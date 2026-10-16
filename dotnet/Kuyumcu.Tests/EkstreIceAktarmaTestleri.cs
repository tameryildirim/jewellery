using System.Net;
using System.Text;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 32 — BANKA EKSTRESİ İÇE AKTARMA [HTTP + gerçek MySQL]
//
// Kanıtlanan üç cümle:
//   1. YÜKLEME HİÇBİR ŞEY YAZMAZ (banka_hareketleri sayısı değişmez)
//   2. ONAY OLMADAN HAREKET YOK ve boş liste kabul edilmez
//   3. TEK YAZMA YOLU (onay HareketEkleIc'ten geçer → MASAK kapısı çalışır)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class EkstreIceAktarmaTestleri(HttpFikstur f)
{
    private const string Baslik = "tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static string Iban() =>
        TestIban.Uret();

    private async Task<string> HesapAc(Kiraci k, string paraBirimi = "TRY")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = Iban(), paraBirimi,
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private static string B64(string metin) => Convert.ToBase64String(Encoding.UTF8.GetBytes(metin));

    private Task<HttpResponseMessage> Yukle(
        Kiraci k, string hesapId, string icerik, string rol = "SAHIP",
        bool tekrar = false, string dosya = "ekstre.csv", string format = "CSV")
        => f.Ortam.Istemci(k, rol).PostJson(
            $"/api/v1/bankalar/{hesapId}/ekstre/yukle" + (tekrar ? "?tekrar=true" : ""),
            new { dosyaAdi = dosya, format, icerikBase64 = B64(icerik) });

    /// <summary>
    /// ⚠ P-38 · B-18: sunucu artık cari eşleşmesi OLMAYAN satırı varsayılan
    /// olarak onaylamaz. Bu dosyanın senaryoları (mükerrer, MASAK, tek yazma
    /// yolu) carinin varlığıyla ilgili DEĞİLDİR ve eskiden de carisiz
    /// onaylıyordu; niyet artık AÇIKÇA yazılır. Carisiz reddi kendi testinde
    /// sınanır (P38SahipsizBulgularTestleri).
    /// </summary>
    private Task<HttpResponseMessage> Onayla(Kiraci k, string ithalatId, IEnumerable<string> idler,
        string rol = "SAHIP", bool carisizOnay = true)
        => f.Ortam.Istemci(k, rol).PostJson(
            $"/api/v1/bankalar/ekstre/{ithalatId}/onayla",
            new { satirIdler = idler.ToList(), carisizOnay });

    private async Task<HttpResponseMessage> SatirPatch(Kiraci k, string satirId, object govde)
        => await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/bankalar/ekstre/satir/{satirId}",
            System.Net.Http.Json.JsonContent.Create(govde));

    private async Task<List<(string Id, int SatirNo, string Durum)>> Satirlar(Kiraci k, string ithalatId)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalatId}?limit=100&offset=0");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("satirlar").EnumerateArray()
            .Select(s => (s.GetProperty("id").GetString()!, s.GetProperty("satirNo").GetInt32(),
                          s.GetProperty("durum").GetString()!))
            .ToList();
    }

    private async Task<int> HareketSayisi(Kiraci k, string hesapId)
    {
        await using var db = f.Vt.OlusturSistem();
        return await db.BankaHareketleri
            .CountAsync(h => h.TenantId == k.TenantId && h.BankaHesapId == hesapId);
    }

    // ── 1) Yükleme deftere SIFIR satır yazar ────────────────────────────────

    [Fact]
    public async Task Yukleme_sonrasi_banka_hareketleri_SIFIR_ekstre_satirlari_N_olur()
    {
        var k = YeniKiraci("P32-YUKLE");
        var hesap = await HesapAc(k);

        var icerik = Baslik
            + "\n01.09.2026;A;;100,00;1;R1;;X"
            + "\n02.09.2026;B;;200,00;2;R2;;Y"
            + "\n03.09.2026;C;300,00;;3;R3;;Z\n";

        var cevap = await Yukle(k, hesap, icerik);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();

        Assert.Equal(3, govde.GetProperty("satirSayisi").GetInt32());
        Assert.Equal(3, govde.GetProperty("yeniSayisi").GetInt32());
        // Sunucunun kendi iddiası:
        Assert.Equal(0, govde.GetProperty("uretilenHareketSayisi").GetInt32());
        // Ve gerçeği:
        Assert.Equal(0, await HareketSayisi(k, hesap));
    }

    [Fact]
    public async Task Ayni_dosya_ikinci_kez_yuklenince_409_ve_IKINCI_ithalat_olusmaz()
    {
        var k = YeniKiraci("P32-SHA");
        var hesap = await HesapAc(k);
        var icerik = Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n";

        Assert.Equal(HttpStatusCode.OK, (await Yukle(k, hesap, icerik)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Yukle(k, hesap, icerik)).StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, await db.EkstreIthalatlari.CountAsync(i => i.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Tekrar_true_ile_ayni_dosya_yuklenir_ve_TUM_satirlar_MUKERRER_olur()
    {
        var k = YeniKiraci("P32-TEKRAR");
        var hesap = await HesapAc(k);
        var icerik = Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n";

        var ilk = await Yukle(k, hesap, icerik);
        var ithalat1 = (await ilk.Json()).GetProperty("ithalatId").GetString()!;
        var satir1 = (await Satirlar(k, ithalat1)).Single();
        Assert.Equal(HttpStatusCode.OK, (await Onayla(k, ithalat1, [satir1.Id])).StatusCode);

        var ikinci = await Yukle(k, hesap, icerik, tekrar: true);
        Assert.Equal(HttpStatusCode.OK, ikinci.StatusCode);
        Assert.Equal(1, (await ikinci.Json()).GetProperty("mukerrerSayisi").GetInt32());
        // Onaydan gelen 1 hareket dışında yeni hareket YOK.
        Assert.Equal(1, await HareketSayisi(k, hesap));
    }

    [Fact]
    public async Task Elle_girilmis_dekontun_aynisi_ekstrede_MUKERRER_isaretlenir()
    {
        var k = YeniKiraci("P32-ELLE");
        var hesap = await HesapAc(k);

        var dekont = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bankalar/{hesap}/hareket", new
        {
            yon = "GELEN", tutar = 5000m, karsiTarafAd = "ALI VELI",
            aciklama = "HAVALE", tarih = "2026-09-01T00:00:00Z",
        });
        Assert.Equal(HttpStatusCode.OK, dekont.StatusCode);

        // ⚠ Aynı anahtar: hesap + tarih + tutar + yön + referans('') + IBAN('') + açıklama
        var icerik = Baslik
            + "\n01.09.2026;HAVALE;;5.000,00;1;;;ALI VELI"
            + "\n02.09.2026;BASKA;;250,00;2;;;BASKA\n";

        var cevap = await Yukle(k, hesap, icerik);
        var govde = await cevap.Json();

        Assert.Equal(1, govde.GetProperty("mukerrerSayisi").GetInt32());
        Assert.Equal(1, govde.GetProperty("yeniSayisi").GetInt32());
    }

    // ── 2) Onay ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Onay_secili_satirlari_hareket_yapar_ve_bakiye_TAM_net_toplam_kadar_degisir()
    {
        var k = YeniKiraci("P32-ONAY");
        var hesap = await HesapAc(k);

        var icerik = Baslik
            + "\n01.09.2026;A;;100,00;1;R1;;X"
            + "\n02.09.2026;B;;200,00;2;R2;;Y"
            + "\n03.09.2026;C;50,00;;3;R3;;Z"
            + "\n04.09.2026;D;;999,00;4;R4;;W\n";

        var ithalat = (await (await Yukle(k, hesap, icerik)).Json()).GetProperty("ithalatId").GetString()!;
        var satirlar = await Satirlar(k, ithalat);
        var secim = satirlar.Where(s => s.SatirNo <= 3).Select(s => s.Id).ToList();

        var cevap = await Onayla(k, ithalat, secim);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        Assert.Equal(3, govde.GetProperty("basarili").GetInt32());
        Assert.Equal(3, govde.GetProperty("uretilenHareketSayisi").GetInt32());

        Assert.Equal(3, await HareketSayisi(k, hesap));

        await using var db = f.Vt.OlusturSistem();
        var bakiye = (await db.BankaHesaplari.FirstAsync(h => h.Id == hesap)).Bakiye;
        Assert.Equal(250m, bakiye);   // +100 +200 -50

        var onaylananlar = await db.EkstreSatirlari
            .Where(s => s.IthalatId == ithalat && s.Durum == EkstreSatirDurumu.Onaylandi)
            .ToListAsync();
        Assert.Equal(3, onaylananlar.Count);
        Assert.All(onaylananlar, s => Assert.False(string.IsNullOrEmpty(s.BankaHareketId)));
    }

    [Fact]
    public async Task Ayni_satir_ikinci_kez_onaylanamaz_ve_IKINCI_hareket_olusmaz()
    {
        var k = YeniKiraci("P32-IKINCI");
        var hesap = await HesapAc(k);
        var icerik = Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n";

        var ithalat = (await (await Yukle(k, hesap, icerik)).Json()).GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();

        Assert.Equal(HttpStatusCode.OK, (await Onayla(k, ithalat, [satir.Id])).StatusCode);

        // ⚠ KATMAN 1: durum kontrolü. Yanıt 200'dür ama satır bazında HATA döner
        //   ve İKİNCİ HAREKET OLUŞMAZ — asıl iddia budur.
        var ikinci = await Onayla(k, ithalat, [satir.Id]);
        var govde = await ikinci.Json();
        Assert.Equal(0, govde.GetProperty("basarili").GetInt32());
        Assert.Equal(1, govde.GetProperty("basarisiz").GetInt32());
        Assert.Equal(1, await HareketSayisi(k, hesap));
    }

    [Fact]
    public async Task Benzersiz_indeks_ayni_anahtarli_IKINCI_hareketi_VERITABANINDAN_reddeder()
    {
        // ⚠ KATMAN 2 AYRI ÖLÇÜLÜR (Aşkın koşulu): durum kontrolünü hiç
        //   kullanmadan, doğrudan ikinci kez ELLE aynı dekontu girerek.
        var k = YeniKiraci("P32-INDEKS");
        var hesap = await HesapAc(k);

        object Dekont() => new
        {
            yon = "GELEN", tutar = 1234m, karsiTarafAd = "AYNI",
            aciklama = "AYNI ACIKLAMA", tarih = "2026-09-01T00:00:00Z",
        };

        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bankalar/{hesap}/hareket", Dekont())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bankalar/{hesap}/hareket", Dekont())).StatusCode);

        Assert.Equal(1, await HareketSayisi(k, hesap));
    }

    [Fact]
    public async Task Bos_satirIdler_listesi_400_doner()
    {
        var k = YeniKiraci("P32-BOS");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;1,00;1;R;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        Assert.Equal(HttpStatusCode.BadRequest, (await Onayla(k, ithalat, [])).StatusCode);
    }

    [Fact]
    public async Task Bes_yuzden_fazla_satir_onaylanmaya_calisilirsa_400_doner()
    {
        var k = YeniKiraci("P32-TAVAN");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;1,00;1;R;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        var sahte = Enumerable.Range(0, 501).Select(i => $"id-{i}");

        Assert.Equal(HttpStatusCode.BadRequest, (await Onayla(k, ithalat, sahte)).StatusCode);
    }

    [Fact]
    public async Task Onaylanmis_satirda_PATCH_409_doner()
    {
        var k = YeniKiraci("P32-PATCH409");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;1,00;1;R;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();
        await Onayla(k, ithalat, [satir.Id]);

        var cevap = await SatirPatch(k, satir.Id, new { durum = "ATLANDI" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    [Fact]
    public async Task MASAK_kapisi_toplu_onayda_da_calisir_esik_ustu_satir_kimliksiz_GECMEZ()
    {
        // ⚠ TEK YAZMA YOLUNUN ASIL KANITI: onay HareketEkleIc'ten geçtiği için
        //   paket 41'in MASAK kapısı toplu üretimde de çalışır. Kendi Add'ini
        //   yazan bir uygulama bu testte YEŞİL KALAMAZ.
        var k = YeniKiraci("P32-MASAK");
        var hesap = await HesapAc(k);
        var icerik = Baslik + "\n01.09.2026;BUYUK HAVALE;;200.000,00;1;R1;;BUYUK\n";

        var ithalat = (await (await Yukle(k, hesap, icerik)).Json()).GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();

        // Satırın carisi yok → MASAK kapısı kimlik tespiti olmadan geçirmez.
        await SatirPatch(k, satir.Id, new { cariId = k.CariId });
        var cevap = await Onayla(k, ithalat, [satir.Id]);

        var govde = await cevap.Json();
        Assert.Equal(0, govde.GetProperty("basarili").GetInt32());
        Assert.Equal(0, await HareketSayisi(k, hesap));
    }

    // ── 3) Eşleştirme ve öğrenme ────────────────────────────────────────────

    [Fact]
    public async Task Elle_cari_baglaninca_IBAN_ogrenilir_ve_SONRAKI_ekstrede_otomatik_eslesir()
    {
        var k = YeniKiraci("P32-OGREN");
        var hesap = await HesapAc(k);
        var iban = "TR330006100519786457841326";

        var ithalat1 = (await (await Yukle(k, hesap,
            Baslik + $"\n01.09.2026;A;;100,00;1;R1;{iban};X\n", dosya: "a.csv")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir1 = (await Satirlar(k, ithalat1)).Single();

        Assert.Equal(HttpStatusCode.OK, (await SatirPatch(k, satir1.Id, new { cariId = k.CariId })).StatusCode);

        await using (var db = f.Vt.OlusturSistem())
        {
            var bag = await db.CariIbanlari.FirstOrDefaultAsync(c => c.TenantId == k.TenantId);
            Assert.NotNull(bag);
            Assert.Equal(IbanBagKaynagi.Ogrenildi, bag.Kaynak);
            Assert.Equal(k.CariId, bag.CariId);
        }

        // İKİNCİ ekstre: aynı IBAN, farklı satır → OTOMATİK eşleşir.
        var cevap2 = await Yukle(k, hesap, Baslik + $"\n05.09.2026;B;;777,00;2;R9;{iban};X\n", dosya: "b.csv");
        var ithalat2 = (await cevap2.Json()).GetProperty("ithalatId").GetString()!;

        await using var db2 = f.Vt.OlusturSistem();
        var satir2 = await db2.EkstreSatirlari.FirstAsync(s => s.IthalatId == ithalat2);
        Assert.Equal(k.CariId, satir2.EslesenCariId);
        Assert.Equal(EslesmeKaynagi.Iban, satir2.EslesmeKaynagi);
    }

    [Fact]
    public async Task Ayni_IBANi_ikinci_bir_cariye_baglama_denemesi_409_doner()
    {
        var k = YeniKiraci("P32-IBAN409");
        var hesap = await HesapAc(k);
        var iban = "TR020006100519786457841399";

        string ikinciCariId;
        await using (var db = f.Vt.OlusturSistem())
        {
            ikinciCariId = Guid.NewGuid().ToString();
            db.Cariler.Add(new Cari
            {
                Id = ikinciCariId, TenantId = k.TenantId, Ad = "İKİNCİ MÜŞTERİ",
                Tur = "MUSTERI", Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var ithalat = (await (await Yukle(k, hesap,
            Baslik + $"\n01.09.2026;A;;100,00;1;R1;{iban};X\n02.09.2026;B;;200,00;2;R2;{iban};X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satirlar = await Satirlar(k, ithalat);

        Assert.Equal(HttpStatusCode.OK, (await SatirPatch(k, satirlar[0].Id, new { cariId = k.CariId })).StatusCode);
        var cevap = await SatirPatch(k, satirlar[1].Id, new { cariId = ikinciCariId });

        // ⚠ 409 mesajı MEVCUT SAHİBİN adını söyler — sessizce taşınmaz.
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        string ilkSahipAd;
        await using (var db2 = f.Vt.OlusturSistem())
            ilkSahipAd = (await db2.Cariler.FirstAsync(c => c.Id == k.CariId)).Ad;
        // ⚠ JSON Türkçe harfleri \uXXXX kaçışıyla yazar; ham metin değil
        //   AYRIŞTIRILMIŞ gövde sınanır (Türkçe gövde ayrıştırılarak sınanır kuralı).
        Assert.Contains(ilkSahipAd, (await cevap.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Unvan_benzerligi_ASLA_otomatik_baglamaz_oneri_ucu_yalnizca_SIRALAR()
    {
        var k = YeniKiraci("P32-UNVAN");
        var hesap = await HesapAc(k);

        // Kiracının carisinin adıyla NEREDEYSE aynı bir ünvan.
        string cariAd;
        await using (var db = f.Vt.OlusturSistem())
            cariAd = (await db.Cariler.FirstAsync(c => c.Id == k.CariId)).Ad;

        var ithalat = (await (await Yukle(k, hesap,
            Baslik + $"\n01.09.2026;A;;100,00;1;R1;;{cariAd}\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        await using (var db = f.Vt.OlusturSistem())
        {
            var satir = await db.EkstreSatirlari.FirstAsync(s => s.IthalatId == ithalat);
            // ⚠ İDDİA: ad birebir aynı olsa bile OTOMATİK BAĞ YOK.
            Assert.Null(satir.EslesenCariId);
            Assert.Null(satir.EslesmeKaynagi);
        }

        var satirId = (await Satirlar(k, ithalat)).Single().Id;
        var oneri = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/bankalar/ekstre/satir/{satirId}/oneriler");
        Assert.Equal(HttpStatusCode.OK, oneri.StatusCode);
        var govde = await oneri.Json();
        Assert.Equal(0, govde.GetProperty("otomatikUygulanan").GetInt32());
        Assert.True(govde.GetProperty("oneriler").GetArrayLength() > 0);
    }

    [Fact]
    public async Task PATCH_gonderilmeyen_alani_DEGISTIRMEZ()
    {
        var k = YeniKiraci("P32-PATCHSOZ");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();

        await SatirPatch(k, satir.Id, new { cariId = k.CariId });
        // Yalnız durum gönderilir: cari bağı KORUNMALI.
        await SatirPatch(k, satir.Id, new { durum = "ATLANDI" });

        await using var db = f.Vt.OlusturSistem();
        var son = await db.EkstreSatirlari.FirstAsync(s => s.Id == satir.Id);
        Assert.Equal(k.CariId, son.EslesenCariId);
        Assert.Equal(EkstreSatirDurumu.Atlandi, son.Durum);
    }

    [Fact]
    public async Task Atlanan_satir_GERI_ALINABILIR()
    {
        var k = YeniKiraci("P32-GERIAL");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();

        await SatirPatch(k, satir.Id, new { durum = "ATLANDI" });
        Assert.Equal(HttpStatusCode.OK, (await SatirPatch(k, satir.Id, new { durum = "YENI" })).StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(EkstreSatirDurumu.Yeni, (await db.EkstreSatirlari.FirstAsync(s => s.Id == satir.Id)).Durum);
    }

    [Fact]
    public async Task Bos_cariId_ile_bag_KALDIRILIR()
    {
        var k = YeniKiraci("P32-BAGKALDIR");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();

        await SatirPatch(k, satir.Id, new { cariId = k.CariId });
        Assert.Equal(HttpStatusCode.OK, (await SatirPatch(k, satir.Id, new { cariId = "" })).StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Null((await db.EkstreSatirlari.FirstAsync(s => s.Id == satir.Id)).EslesenCariId);
    }

    // ── 4) Fail-closed kapılar ──────────────────────────────────────────────

    [Fact]
    public async Task Gelecek_tarihli_satir_HATALI_olur_ve_onaya_SECILEMEZ()
    {
        var k = YeniKiraci("P32-GELECEK");
        var hesap = await HesapAc(k);
        var gelecek = DateTime.UtcNow.AddDays(30).ToString("dd.MM.yyyy");

        var cevap = await Yukle(k, hesap, Baslik + $"\n{gelecek};ILERI;;100,00;1;R1;;X\n");
        Assert.Equal(1, (await cevap.Json()).GetProperty("hataliSayisi").GetInt32());

        var ithalat = (await cevap.Json()).GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();
        Assert.Equal(EkstreSatirDurumu.Hatali, satir.Durum);

        var onay = await Onayla(k, ithalat, [satir.Id]);
        Assert.Equal(0, (await onay.Json()).GetProperty("basarili").GetInt32());
        Assert.Equal(0, await HareketSayisi(k, hesap));
    }

    [Fact]
    public async Task On_bin_bir_satirlik_dosya_400_doner_ve_HICBIR_TABLOYA_yazilmaz()
    {
        var k = YeniKiraci("P32-10001");
        var hesap = await HesapAc(k);

        var kutu = new StringBuilder(Baslik + "\n");
        for (var i = 0; i < 10_001; i++) kutu.Append("01.09.2026;X;;1,00;1;R;;A\n");

        var cevap = await Yukle(k, hesap, kutu.ToString());

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.EkstreIthalatlari.CountAsync(i => i.TenantId == k.TenantId));
        Assert.Equal(0, await db.EkstreSatirlari.CountAsync(s => s.TenantId == k.TenantId));
    }

    [Fact]
    public async Task MT940_formati_400_ile_reddedilir_sessizce_CSV_gibi_okunmaz()
    {
        var k = YeniKiraci("P32-MT940");
        var hesap = await HesapAc(k);

        var cevap = await Yukle(k, hesap, ":20:REF\n:61:2609010901C1234,56NTRF\n", format: "MT940");

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("MT940", await cevap.Metin());
    }

    // ── 5) Kişisel veri (AS-2) ──────────────────────────────────────────────

    [Fact]
    public async Task Karsi_IBAN_ve_ham_satir_DISKTE_SIFRELI_saklanir()
    {
        var k = YeniKiraci("P32-SIFRE");
        var hesap = await HesapAc(k);
        var iban = "TR560006100519786457841300";

        var ithalat = (await (await Yukle(k, hesap,
            Baslik + $"\n01.09.2026;A;;100,00;1;R1;{iban};GIZLI AD\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        await using var db = f.Vt.OlusturSistem();
        // ⚠ HAM SQL: değer dönüştürücü devreye GİRMEZ, diskteki hâli okunur.
        var ham = await db.Database.SqlQueryRaw<string>(
            $"SELECT CONCAT(COALESCE(karsi_iban,''), '|', ham_satir) AS Value FROM ekstre_satirlari WHERE ithalat_id = {{0}}",
            ithalat).ToListAsync();

        var satir = Assert.Single(ham);
        Assert.DoesNotContain(iban, satir);
        Assert.DoesNotContain("GIZLI AD", satir);

        // Uygulama tarafında düz metin görünür (converter çalışıyor).
        var varlik = await db.EkstreSatirlari.FirstAsync(s => s.IthalatId == ithalat);
        Assert.Equal(iban, varlik.KarsiIban);
        Assert.False(string.IsNullOrEmpty(varlik.KarsiIbanHash));
    }

    [Fact]
    public async Task Ekranin_gordugu_IBAN_MASKELIDIR()
    {
        var k = YeniKiraci("P32-MASKE");
        var hesap = await HesapAc(k);
        var iban = "TR500006100519786457841311";

        var ithalat = (await (await Yukle(k, hesap,
            Baslik + $"\n01.09.2026;A;;100,00;1;R1;{iban};X\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=100&offset=0");
        var metin = await cevap.Metin();

        Assert.DoesNotContain(iban, metin);
        Assert.Contains("TR50 **** 1311", metin);
    }

    // ── 6) Denetim izi ──────────────────────────────────────────────────────

    [Fact]
    public async Task Denetim_izinde_yukleme_ve_onay_AYRI_kayitlardir()
    {
        var k = YeniKiraci("P32-DENETIM");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;
        var satir = (await Satirlar(k, ithalat)).Single();
        await Onayla(k, ithalat, [satir.Id]);

        await using var db = f.Vt.OlusturSistem();
        var kayitlar = await db.DenetimKayitlari
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "EkstreIthalati")
            .ToListAsync();

        // ⚠ İDDİA "toplam 2 kayıt" DEĞİL, "yükleme ve onay AYRI kayıtlardır":
        //   DenetimYazmaDinleyicisi ayrıca kendi otomatik izlerini de yazar.
        Assert.Single(kayitlar, d => d.Detay is not null && d.Detay.Contains("ekstre yüklendi"));
        Assert.Single(kayitlar, d => d.Detay is not null && d.Detay.Contains("ekstre onaylandı"));
        Assert.Contains(kayitlar, d => d.Detay is not null && d.Detay.Contains("üretilen banka hareketi: 0"));
        Assert.Contains(kayitlar, d => d.Detay is not null && d.Detay.Contains("üretilen banka hareketi: 1"));
    }

    // ── 7) Sayfalama (TB-19) ────────────────────────────────────────────────

    [Fact]
    public async Task Servis_katmani_da_limit_tavanini_kendisi_uygular()
    {
        // ⚠ İKİ KATMAN AYRI ÖLÇÜLÜR: HTTP testi controller'daki [Range(1,100)]
        //   niteliğini ölçer. Nitelik silinirse servis hâlâ tutmalıdır; aksi
        //   hâlde "sınırsız liste yok" kuralı tek bir satıra bağlı kalırdı.
        var k = YeniKiraci("P32-LIMIT-S");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        await using var db = f.Vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Banka.EkstreServisi(
            db, new Kuyumcu.Api.Modules.Banka.BankaServisi(db), TestKurulum.Denetim(db));

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(
            () => servis.Getir(ithalat, k.Islem, null, 5000, 0));
    }

    [Fact]
    public async Task Limit_tavani_asilirsa_400_doner_sinirsiz_liste_YOK()
    {
        var k = YeniKiraci("P32-LIMIT");
        var hesap = await HesapAc(k);
        var ithalat = (await (await Yukle(k, hesap, Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n")).Json())
            .GetProperty("ithalatId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=5000&offset=0");

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }
}
