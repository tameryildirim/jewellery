using System.Net;
using System.Text;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatinalma = Kuyumcu.Api.Modules.Satinalma;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET URUN-ACILIS-ATOMIK — çok adımlı yazma uçlarında ATOMİKLİK
//
// Bulgu (08.10): yeni kiracıda kur yokken POST /urunler {acilisStogu} → 400
// "Sayım kaydı için en az bir kur girişi gerekir", ama ürün kartı veritabanında
// KALIYORDU; aynı kodla tekrar deneme 409, satış aramasında 'Stokta yok' ikizi.
// Değişmez: bir adım reddederse HİÇBİR satır kalmaz; tekrar deneme çift üretmez.
//   A1  ürün + açılış stoğu (kur yok)       → 0 ürün/stok/parti/hareket; düzeltip tekrar = 1 ürün
//   A2  ürün + açılış stoğu (şube belirsiz) → 0 ürün
//   A3  satın alma mal kabul (alış reddi)   → 0 irsaliye/kalem/hareket, sipariş kalemi değişmez
//   A4  ekstre toplu onay (mükerrer reddi)  → reddedilen satırın Add'i SIZMAZ, bakiye değişmez
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UrunAcilisAtomikTestleri(TestVeritabani vt)
{
    private static async Task KurlariSil(KuyumcuDbContext sistem, string tenantId)
    {
        sistem.KurSnapshots.RemoveRange(
            await sistem.KurSnapshots.Where(x => x.TenantId == tenantId).ToListAsync());
        await sistem.SaveChangesAsync();
    }

    private static async Task<(int Urun, int Stok, int Parti, int Hareket)> Sayim(
        KuyumcuDbContext sistem, string tenantId, string kod)
    {
        var urunIdler = await sistem.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Kod == kod).Select(u => u.Id).ToListAsync();
        return (
            urunIdler.Count,
            await sistem.Stoklar.AsNoTracking().CountAsync(s => s.TenantId == tenantId && urunIdler.Contains(s.UrunId)),
            await sistem.StokPartileri.AsNoTracking().CountAsync(p => p.TenantId == tenantId && urunIdler.Contains(p.UrunId)),
            await sistem.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == tenantId && h.UrunId != null && urunIdler.Contains(h.UrunId)));
    }

    private static UrunOlusturDto Dto(string kod, decimal adet) => new()
    {
        Tip = "ZIYNET", Kod = kod, Ad = "Atomik bilezik", AyarId = "K22",
        GramAgirlik = 10m, Birim = "ADET",
        AcilisStogu = new AcilisStoguDto { Adet = adet },
    };

    [Fact]
    public async Task A1_Kur_yokken_acilis_stoklu_urun_REDDEDILIR_ve_HICBIR_satir_kalmaz_tekrar_deneme_cift_uretmez()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETURUN-ACILIS-ATOMIK-TEST A1");
        await KurlariSil(sistem, k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(Dto("ATM-1", 5m), k.Islem));
        // Tur 2 (tasarım hakemi KRİTİK): sayım jargonu değil, tezgâhtar dili + açılış alanı.
        Assert.Equal(Kuyumcu.Core.I18n.UrunAcilisSozlugu.Tr.AcilisKurYok, hata.Message);
        Assert.Equal("acilisStogu.adet", AlanIsareti.Alan(hata));

        // ATOMİKLİK: kart dahil hiçbir satır yok (eskiden ürün kartı aktif=1 kalıyordu).
        Assert.Equal((0, 0, 0, 0), await Sayim(vt.OlusturSistem(), k.TenantId, "ATM-1"));

        // Kur girilip AYNI kodla tekrar denenince 409 DEĞİL başarı, tek ürün.
        sistem.KurSnapshots.Add(new KurSnapshot
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, AltinHasFiyati = 6_827.20m,
            GramAltinFiyati = 6_861.51m, Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow,
        });
        await sistem.SaveChangesAsync();

        await new UrunServisi(vt.Olustur(k)).Olustur(Dto("ATM-1", 5m), k.Islem);
        var (urun, stok, parti, hareket) = await Sayim(vt.OlusturSistem(), k.TenantId, "ATM-1");
        Assert.Equal(1, urun);
        Assert.Equal(1, stok);
        Assert.Equal(1, parti);
        Assert.Equal(1, hareket);

        // Üçüncü deneme: kod artık gerçekten kullanımda → 409 alan:kod, ikinci kart yok.
        await Assert.ThrowsAsync<AlanCakismasiException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(Dto("ATM-1", 5m), k.Islem));
        Assert.Equal(1, (await Sayim(vt.OlusturSistem(), k.TenantId, "ATM-1")).Urun);
    }

    [Fact]
    public async Task A2_Sube_belirsizken_acilis_stoklu_urun_REDDEDILIR_ve_kart_YAZILMAZ()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETURUN-ACILIS-ATOMIK-TEST A2");
        sistem.Subeler.Add(new Sube
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Şube 2", Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await sistem.SaveChangesAsync();
        var subesiz = new Kuyumcu.Api.Auth.AktifKullanici(k.KullaniciId, k.TenantId, null, "SAHIP");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(Dto("ATM-2", 3m), subesiz));

        Assert.Equal((0, 0, 0, 0), await Sayim(vt.OlusturSistem(), k.TenantId, "ATM-2"));
    }

    [Fact]
    public async Task A1b_Acilis_stogu_basariliysa_kart_stok_ve_defter_TEK_islemde_dogar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST A1b");
        await new UrunServisi(vt.Olustur(k)).Olustur(Dto("ATM-3", 4m), k.Islem);

        var sistem = vt.OlusturSistem();
        Assert.Equal((1, 1, 1, 1), await Sayim(sistem, k.TenantId, "ATM-3"));
        var urunId = await sistem.Urunler.AsNoTracking()
            .Where(u => u.TenantId == k.TenantId && u.Kod == "ATM-3").Select(u => u.Id).SingleAsync();
        var stok = await sistem.Stoklar.AsNoTracking()
            .FirstAsync(s => s.TenantId == k.TenantId && s.UrunId == urunId);
        Assert.Equal(4m, stok.Adet);
    }

    // ── A3 — satın alma mal kabul ──────────────────────────────────────────

    private static MSatinalma.SatinalmaServisi Satinalma(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db),
            new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance),
            NullLogger<MSatinalma.SatinalmaServisi>.Instance);

    [Fact]
    public async Task A3_Mal_kabulde_alis_reddedilince_irsaliye_kalem_ve_hareket_YAZILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST A3");
        using var db = vt.OlusturSistem();
        var sonuc = await Satinalma(db).Olustur(new MSatinalma.SatinalmaOlusturDto
        {
            TedarikciCariId = k.CariId, SubeId = k.SubeId, Aciklama = "atomik mal kabul",
            Kalemler = [new MSatinalma.SatinalmaKalemDto { UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 1_000m }],
        }, k.Islem);
        var id = System.Text.Json.JsonSerializer.SerializeToNode(sonuc, sonuc.GetType(),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!["id"]!.GetValue<string>();
        await Satinalma(db).Onayla(id, k.Islem);
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;
        var oncekiHareket = await db.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == k.TenantId);

        // İrsaliye adımı GEÇER, kalem alışı (SatisServisi.Alis) kur bulunamadığı için REDDEDER.
        await Assert.ThrowsAnyAsync<ApiException>(() =>
            Satinalma(vt.OlusturSistem()).MalKabul(id, new MSatinalma.MalKabulDto
            {
                KurSnapshotId = Kimlik.Ulid(),   // kiracıda olmayan kur
                Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 2m }],
            }, k.Islem));

        await using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Irsaliyeler.CountAsync(i => i.TenantId == k.TenantId));
        Assert.Equal(0, await kontrol.IrsaliyeKalemleri.CountAsync(i => i.TenantId == k.TenantId));
        Assert.Equal(oncekiHareket, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId));
        Assert.Equal(0m, (await kontrol.SatinalmaKalemleri.AsNoTracking().FirstAsync(x => x.Id == kalemId)).KabulEdilenAdet);
        Assert.Equal(SatinalmaDurumu.Onaylandi,
            (await kontrol.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id)).Durum);

        // Düzeltilip tekrar denenince TEK irsaliye doğar (çift yok).
        await Satinalma(vt.OlusturSistem()).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 2m }],
        }, k.Islem);
        await using var kontrol2 = vt.OlusturSistem();
        Assert.Equal(1, await kontrol2.Irsaliyeler.CountAsync(i => i.TenantId == k.TenantId));
        Assert.Equal(0, await kontrol2.IrsaliyeKalemleri.CountAsync(i => i.TenantId == k.TenantId && i.HareketId == null));
    }

    /// <summary>
    /// Tur 2 (tasarım hakemi KRİTİK): gram × adet has tavanını aşan açılış stoğu
    /// "Sayılan adet çok büyük" (sayım dili) DEĞİL, ürün kartı dilinde ve açılış
    /// alanıyla reddedilir; kart YAZILMAZ.
    /// </summary>
    [Fact]
    public async Task A6_Acilis_stogu_has_tavanini_asinca_kart_dilinde_REDDEDILIR_ve_kart_YAZILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST A6");
        var dto = Dto("ATM-6", 1_000_000m);
        dto.GramAgirlik = 99_999m;

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(dto, k.Islem));
        Assert.Equal(Kuyumcu.Core.I18n.UrunAcilisSozlugu.Tr.AcilisCokBuyuk, hata.Message);
        Assert.Equal("acilisStogu.adet", AlanIsareti.Alan(hata));
        Assert.Equal((0, 0, 0, 0), await Sayim(vt.OlusturSistem(), k.TenantId, "ATM-6"));
    }

    /// <summary>
    /// Tur 2 (doğrulayıcı ORTA): sayım oturumu kapanışı TEK işlem. İlk satır (fark −2)
    /// uygulanır, ikinci satır has tavanında reddedilir → ilk satırın defteri de
    /// YAZILMAZ, stok değişmez, oturum AÇIK kalır. Düzeltip tekrar kapatınca tek düzeltme.
    /// </summary>
    // DI'deki gibi TEK DbContext: sayım servisi ve içerdiği ürün servisi aynı bağlantıyı paylaşır.
    private static Kuyumcu.Api.Modules.Sayim.SayimServisi SayimServisi(KuyumcuDbContext db) => new(db, new UrunServisi(db));

    [Fact]
    public async Task A5_Sayim_kapanisinda_ikinci_satir_reddedilince_ilk_satirin_defteri_YAZILMAZ_oturum_ACIK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST A5", stokAdet: 5m);
        await new UrunServisi(vt.Olustur(k)).Olustur(
            new UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = "ATM-5B", Ad = "Atomik sayım", AyarId = "K22",
                GramAgirlik = 10m, Birim = "ADET", AcilisStogu = new AcilisStoguDto { Adet = 5m },
            }, k.Islem);

        var sayim = SayimServisi(vt.Olustur(k));
        var ac = await sayim.Ac(new Kuyumcu.Api.Modules.Sayim.SayimAcDto { Ad = "Atomik", SubeId = k.SubeId }, k.Islem);
        var oturumId = (string)ac.GetType().GetProperty("Id")!.GetValue(ac)!;

        List<string> sira;
        await using (var o = vt.OlusturSistem())
            sira = await o.SayimSatirlari.AsNoTracking().Where(x => x.OturumId == oturumId)
                .OrderBy(x => x.Id).Select(x => x.UrunId).ToListAsync();
        Assert.Equal(2, sira.Count);
        // Sıra kimliğe göre: İLK satır geçerli fark (5 → 3), İKİNCİ satır has tavanını aşar.
        await sayim.Say(oturumId, new Kuyumcu.Api.Modules.Sayim.SayimSayDto { UrunId = sira[0], SayilanAdet = 3m }, k.Islem);
        await sayim.Say(oturumId, new Kuyumcu.Api.Modules.Sayim.SayimSayDto { UrunId = sira[1], SayilanAdet = 200_000_000m }, k.Islem);

        int HareketSay(KuyumcuDbContext d) => d.Hareketler.AsNoTracking()
            .Count(h => h.TenantId == k.TenantId && h.Tur == "SAYIM_DUZELTME");
        int once; await using (var o = vt.OlusturSistem()) once = HareketSay(o);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            SayimServisi(vt.Olustur(k))
                .Kapat(oturumId, new Kuyumcu.Api.Modules.Sayim.SayimKapatDto(), k.Islem));

        await using (var kontrol = vt.OlusturSistem())
        {
            Assert.Equal(once, HareketSay(kontrol));   // ilk satırın düzeltmesi SIZMADI
            Assert.Equal(5m, (await kontrol.Stoklar.AsNoTracking()
                .FirstAsync(s => s.UrunId == sira[0] && s.SubeId == k.SubeId)).Adet);
            Assert.Equal("ACIK", (await kontrol.SayimOturumlari.AsNoTracking().FirstAsync(x => x.Id == oturumId)).Durum);
            Assert.Null((await kontrol.SayimSatirlari.AsNoTracking()
                .FirstAsync(x => x.OturumId == oturumId && x.UrunId == sira[0])).DuzeltmeHareketId);
        }

        // Düzeltip tekrar kapatınca: tek düzeltme, oturum KAPANDI.
        await sayim.Say(oturumId, new Kuyumcu.Api.Modules.Sayim.SayimSayDto { UrunId = sira[1], SayilanAdet = 5m }, k.Islem);
        await SayimServisi(vt.Olustur(k))
            .Kapat(oturumId, new Kuyumcu.Api.Modules.Sayim.SayimKapatDto(), k.Islem);
        await using var son = vt.OlusturSistem();
        Assert.Equal(once + 1, HareketSay(son));
        Assert.Equal(3m, (await son.Stoklar.AsNoTracking().FirstAsync(s => s.UrunId == sira[0] && s.SubeId == k.SubeId)).Adet);
        Assert.Equal("KAPANDI", (await son.SayimOturumlari.AsNoTracking().FirstAsync(x => x.Id == oturumId)).Durum);
    }
}

// ── A4 — ekstre toplu onayı (HTTP) + ürün ucu HTTP sözleşmesi ───────────────
[Collection("http")]
public sealed class UrunAcilisAtomikHttpTestleri(HttpFikstur f)
{
    private const string Baslik = "tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan";

    /// <summary>Geçerli (MOD 97-10) rastgele TR IBAN — banka ucu IBAN'ı Core kuralıyla doğrular.</summary>
    private static string Iban()
    {
        var bban = "00010" + "0" + Random.Shared.NextInt64(1_000_000_000_000_000L, 9_999_999_999_999_999L);
        var sayisal = System.Numerics.BigInteger.Parse(bban + "292700");
        var kontrol = 98 - (int)(sayisal % 97);
        return "TR" + kontrol.ToString("00") + bban;
    }

    [Fact]
    public async Task A4_Ekstre_onayinda_mukerrer_reddedilen_satirin_yarim_yazimi_SIZMAZ_bakiye_degismez()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETURUN-ACILIS-ATOMIK-TEST A4");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var hesapCevap = await istemci.PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = Iban(), paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, hesapCevap.StatusCode);
        var hesap = (await hesapCevap.Json()).GetProperty("id").GetString()!;

        // (1) Ekstre yüklenir (henüz mükerrer yok).
        var yukle = await istemci.PostJson($"/api/v1/bankalar/{hesap}/ekstre/yukle", new
        {
            dosyaAdi = "atomik.csv", format = "CSV",
            icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                Baslik + "\n01.09.2026;ATOMIK HAVALE;;100,00;1;;;X\n")),
        });
        Assert.Equal(HttpStatusCode.OK, yukle.StatusCode);
        var ithalat = (await yukle.Json()).GetProperty("ithalatId").GetString()!;

        // (2) AYNI havale elle girilir → onay anında mükerrer anahtarı çakışır.
        var elle = await istemci.PostJson($"/api/v1/bankalar/{hesap}/hareket", new
        {
            yon = "GELEN", tutar = 100m, aciklama = "ATOMIK HAVALE", tarih = "2026-09-01",
        });
        Assert.Equal(HttpStatusCode.OK, elle.StatusCode);

        decimal Bakiye()
        {
            using var db = f.Vt.OlusturSistem();
            return db.BankaHesaplari.AsNoTracking().First(h => h.Id == hesap).Bakiye;
        }
        int HareketSayisi()
        {
            using var db = f.Vt.OlusturSistem();
            return db.BankaHareketleri.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.BankaHesapId == hesap);
        }
        var oncekiBakiye = Bakiye();
        Assert.Equal(1, HareketSayisi());

        var satirlar = await istemci.GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=100&offset=0");
        var satirId = (await satirlar.Json()).GetProperty("satirlar").EnumerateArray().Single()
            .GetProperty("id").GetString()!;

        // (3) Onay: satır reddedilir (HATALI), istek 500 DEĞİL 200 döner.
        var onay = await istemci.PostJson($"/api/v1/bankalar/ekstre/{ithalat}/onayla",
            new { satirIdler = new[] { satirId }, carisizOnay = true });
        Assert.Equal(HttpStatusCode.OK, onay.StatusCode);
        Assert.Equal(0, (await onay.Json()).GetProperty("basarili").GetInt32());

        // ATOMİKLİK: reddedilen satırın Add'i ve bakiye artışı sızmadı.
        Assert.Equal(1, HareketSayisi());
        Assert.Equal(oncekiBakiye, Bakiye());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(EkstreSatirDurumu.Hatali,
                db.EkstreSatirlari.AsNoTracking().First(s => s.Id == satirId).Durum);
    }

    [Fact]
    public async Task A1_HTTP_Kur_yokken_POST_urunler_400_alan_mesaji_ve_kart_YOK_tekrar_400_degil_409_degil()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PAKETURUN-ACILIS-ATOMIK-TEST A1H");
            db.KurSnapshots.RemoveRange(db.KurSnapshots.Where(x => x.TenantId == k.TenantId).ToList());
            db.SaveChanges();
        }
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var govde = new
        {
            tip = "ZIYNET", kod = "ATM-H1", ad = "Atomik HTTP", ayarId = "K22",
            gramAgirlik = 10m, birim = "ADET", acilisStogu = new { adet = 5m },
        };

        for (var deneme = 0; deneme < 2; deneme++)
        {
            var cevap = await istemci.PostJson("/api/v1/urunler", govde);
            // İkinci deneme de AYNI 400'ü alır — 409 'kod kullanımda' DEĞİL (yarım kart yok).
            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            // Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır: mesaj tezgâhtar dilinde ve
            // açılış stoğu alanına bağlı (ekran kutuyu kırmızı yapar).
            using var j = System.Text.Json.JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var tr = Kuyumcu.Core.I18n.UrunAcilisSozlugu.Tr.AcilisKurYok;
            Assert.Equal(tr, j.RootElement.GetProperty("message").GetString());
            Assert.Equal(tr, j.RootElement.GetProperty("alanlar").GetProperty("acilisStogu.adet")[0].GetString());
        }

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Urunler.AsNoTracking().Count(u => u.TenantId == k.TenantId && u.Kod == "ATM-H1"));
    }
}

// ── Tur 2 (tasarım hakemi KRİTİK) — EKRAN: açılış stoğu reddi GÖRÜNÜR ────────
// Eskiden BASİT modda sunucunun açılış hatası hiçbir alana eşlenmiyordu: 'Daha
// fazla' boşuna açılıyor, form uzuyor, bant ve Kaydet ekran dışında kalıyordu.
// Şimdi: (1) alanlı hata u-acilis-stok kutusunun ALTINDA, grup AÇILMAZ, odak kutuya;
// (2) alansız hata bandı (u-urun-hata) pencerenin SABİT altlığında, odak banda.
public sealed class UrunAcilisAtomikEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string postYaniti = "";

    public UrunAcilisAtomikEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(UrunAcilisAtomikEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/urunler"))
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(t.postYaniti, Encoding.UTF8, "application/json"),
                };
            var g = yol switch
            {
                var y when y.Contains("/urunler/stok-adetleri") => """{"urunler":[]}""",
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                      "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/reyonlar") => "[]",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var y when y.Contains("/cari") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"kaynak":"TEST","pariteler":[]}""",
                var y when y.Contains("/urunler") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }

    private static string Yanit(string mesaj, string? alan) => System.Text.Json.JsonSerializer.Serialize(
        alan is null
            ? (object)new { message = mesaj, error = "Bad Request", statusCode = 400 }
            : new { message = mesaj, alanlar = new Dictionary<string, string[]> { [alan] = [mesaj] }, error = "Bad Request", statusCode = 400 });

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> FormuDoldurKaydet(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-ad").Input("Atomik bilezik");
        e.Find("#u-kod").Input("ATM-E1");
        e.Find("#u-gram").Input("10");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-acilis-stok").Input("5");
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Liste(), i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")));
        return e;
    }

    private List<(string Metot, string Yol, string Govde)> Liste() { lock (istekler) return istekler.ToList(); }

    private bool OdakGitti(string id) => JSInterop.Invocations
        .Any(i => i.Identifier == "kuyumcuOdak.alana" && i.Arguments.Count > 0 && (string?)i.Arguments[0] == id);

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void E1_Acilis_stogu_reddi_kutunun_ALTINDA_gorunur_Daha_fazla_ACILMAZ_odak_kutuya(string mod)
    {
        var tr = Kuyumcu.Core.I18n.UrunAcilisSozlugu.Tr.AcilisKurYok;
        postYaniti = Yanit(tr, "acilisStogu.adet");
        var e = FormuDoldurKaydet(mod);

        e.WaitForAssertion(() => Assert.Contains(tr, e.Find("#alan-hata-u-acilis-stok").TextContent));
        if (mod == ArayuzModuServisi.Basit)
            Assert.Equal("false", e.Find("#u-daha-fazla").GetAttribute("aria-expanded"));   // form UZAMAZ
        Assert.True(OdakGitti("u-acilis-stok"), "odak açılış stoğu kutusuna gitmedi");
        // Bant ve Kaydet SABİT altlıkta: gövde kaysa da görünür kalır.
        var altlik = e.Find(".modal-alt");
        Assert.NotNull(altlik.QuerySelector("#u-urun-hata"));
        // URUN-FORM-2: alana bağlı retde bant YALNIZ kısa genel cümle (aynı cümle iki kez yazılmaz).
        Assert.DoesNotContain(tr, altlik.QuerySelector("#u-urun-hata")!.TextContent);
        Assert.NotNull(altlik.QuerySelector("#u-kaydet"));
        Assert.Single(e.FindAll("#u-kaydet"));
    }

    [Fact]
    public void E2_Alansiz_sunucu_hatasinda_yeni_urun_penceresi_BANDA_kaydirir()
    {
        postYaniti = Yanit("Beklenmeyen bir ret", null);
        var e = FormuDoldurKaydet(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Contains("Beklenmeyen bir ret", e.Find(".modal-alt #u-urun-hata").TextContent));
        Assert.True(OdakGitti("u-urun-hata"), "alansız hatada bant görünür alana kaydırılmadı");
    }
}
