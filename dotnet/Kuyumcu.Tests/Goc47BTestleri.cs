using System.Diagnostics;
using System.Net.Http.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-47B — veri göçü Faz B: açılış bakiyesi + açılış stoku [DB]
//
// Her testin adı hangi kapıyı sınadığını söyler; kapı gevşetilirse kırmızıya döner.
//   • Cari açılışı TEK YAZICIDAN geçer, ayna 0     → Acilis_bakiyesi_TEK_YAZICI…
//   • Stok açılışı DEVIR partisi, maliyet NULL       → Acilis_stoku_DEVIR_partisi…
//   • Kuru prova deftere/stoğa yazmaz               → PROVA_acilis_defter_ve_stoga_YAZMAZ
//   • Tek transaction (dış işlem geri alınır)       → Bir_satir_patlarsa_TUM_acilis…
//   • Geri alma TERS KAYIT, önceki hâle dönüş        → Geri_al_TERS_KAYITLA…
//   • Satılmış açılış stoğu geri alınmaz            → Acilis_stogu_tuketilmisse…
//   • Aynı dosya iki kez uygulanmaz (SHA-256)        → Ayni_dosya_IKINCI_kez…
//   • Emanet bakiyeye girmez (karar 3)               → Emanet_kolonu…
//   • Has 4 ondalık, yuvarlanmaz (karar 5)           → Has_4_ondaligi_asan…
//   • Mühürlü kurulumun arka kapısı olunmaz          → Muhurlu_kurulumda…
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class Goc47BTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Eski programdan açılış bakiyeleri ve vitrin stoğu taşınıyor";

    private static IceAktarimServisi Servis(KuyumcuDbContext db)
    {
        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
            db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var urun = new Kuyumcu.Api.Modules.Urun.UrunServisi(db, denetim);
        var kurulum = new Kuyumcu.Api.Modules.Kurulum.KurulumServisi(db, denetim,
            NullLogger<Kuyumcu.Api.Modules.Kurulum.KurulumServisi>.Instance, new ConfigurationBuilder().Build());
        return new IceAktarimServisi(db, denetim, cari, urun, null, kurulum);
    }

    private static IceAktarimProvaDto Dto(string kaynak, string csv, string? politika = null,
        Dictionary<string, string>? esleme = null) => new()
    {
        Kaynak = kaynak,
        DosyaAdi = $"{kaynak}.csv",
        IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        Gerekce = Gerekce,
        YinelenenPolitikasi = politika,
        KolonEslemesi = esleme,
    };

    private static string Alan(object cevap, params string[] yol)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol) oge = oge.GetProperty(a);
        return oge.ToString();
    }

    private static IceAktarimUygulaDto Onay(IceAktarimServisi s) => new() { Onay = s.Ayarlar.UygulaOnayMetni };

    private static string CariEkle(KuyumcuDbContext db, string tenantId, string ad, string? tel, string? vergi = null)
    {
        var id = Kimlik.Ulid(); var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = tenantId, Tur = "MUSTERI", Ad = ad, Telefon = tel, VergiNo = vergi,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.CariBakiyeler.Add(new CariBakiye
        {
            CariId = id, TenantId = tenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        return id;
    }

    private static string UrunEkle(KuyumcuDbContext db, string tenantId, string kod, decimal gram = 10m)
    {
        var id = Kimlik.Ulid(); var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = tenantId, Tip = "ZIYNET", Kod = kod, Ad = $"Ürün {kod}", AyarId = "K22",
            Milyem = Kuyumcu.Core.Domain.Ayarlar.Bul("K22")!.Milyem, GramAgirlik = gram, TasAgirlik = 0m,
            HasGrami = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(gram, 0m, "K22"),
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        return id;
    }

    private static async Task AynaSifir(KuyumcuDbContext db, string tenantId, string cariId)
    {
        var a = await CariBakiyeKurali.Hesapla(db, tenantId, cariId);
        Assert.Equal(0m, a.HasUyusmazligi);
        Assert.Equal(0m, a.TlUyusmazligi);
    }

    // ── 1) Cari açılışı ──────────────────────────────────────────────────────

    [Fact]
    public async Task Acilis_bakiyesi_TEK_YAZICIDAN_ACILIS_hareketi_uretir_bakiye_ve_ayna_tutarli()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST bakiye");
        using var db = vt.Olustur(k.TenantId);
        var c1 = CariEkle(db, k.TenantId, "Ayşe Yılmaz", "05321112233");
        var c2 = CariEkle(db, k.TenantId, "İsmail Çelik", null, "1234567890");
        await db.SaveChangesAsync();

        var csv = """
            ad,telefon,vergiNo,tlBakiye,hasGramBakiye,usdBakiye
            Ayşe Yılmaz,0532 111 22 33,,1500.50,12.5000,
            ismail çelik,,1234567890,-250,0,100
            """;
        var s = Servis(db);
        var prova = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        Assert.Equal("2", Alan(prova, "gecerliSatir"));
        Assert.Equal("1250.50", Alan(prova, "ozet", "TlToplam"));

        await s.Uygula(Alan(prova, "oturumId"), Onay(s), k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var b1 = await oku.CariBakiyeler.AsNoTracking().SingleAsync(b => b.CariId == c1);
        Assert.Equal(1500.50m, b1.TlBakiye);
        Assert.Equal(12.5m, b1.HasGramBakiye);
        var b2 = await oku.CariBakiyeler.AsNoTracking().SingleAsync(b => b.CariId == c2);
        Assert.Equal(-250m, b2.TlBakiye);

        // Defter: ACILIS türü, cariye bağlı, has işareti VERESİYE sözleşmesiyle aynı (borç → eksi has).
        var h1 = await oku.Hareketler.AsNoTracking().SingleAsync(h => h.CariId == c1);
        Assert.Equal("ACILIS", h1.Tur);
        Assert.Equal(-12.5m, h1.HasGrami);
        Assert.Equal(2, await oku.Hareketler.CountAsync(h => h.CariId == c2));   // TL + USD bacağı

        // Ekstre aynası: kayıtlı bakiye = kural toplamı (TEK YAZICI kanıtı).
        await AynaSifir(oku, k.TenantId, c1);
        await AynaSifir(oku, k.TenantId, c2);

        // T-2 ile ortak şema: açılış kalemi doğdu → sihirbaz aynı cariye ikinci açılış veremez.
        Assert.Equal(2, await oku.AcilisKalemleri.CountAsync(x => x.Tur == AcilisTuru.Cari
                                                              && x.Durum == AcilisKalemDurumu.Uygulandi));
    }

    [Fact]
    public async Task PROVA_acilis_defter_ve_stoga_YAZMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST prova");
        using var db = vt.Olustur(k.TenantId);
        CariEkle(db, k.TenantId, "Ali Demir", "05320000001");
        UrunEkle(db, k.TenantId, "G47B-P1");
        await db.SaveChangesAsync();

        var hOnce = await db.Hareketler.CountAsync();
        var pOnce = await db.Set<StokPartisi>().CountAsync();
        var s = Servis(db);
        await s.Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye\nAli Demir,05320000001,100\n"), k.Islem);
        var stok = await s.Prova(Dto("acilis-stok", "kod,adet\nG47B-P1,3\n"), k.Islem);

        Assert.Equal("0", Alan(stok, "yazilanKayit"));
        Assert.Equal(hOnce, await db.Hareketler.CountAsync());
        Assert.Equal(pOnce, await db.Set<StokPartisi>().CountAsync());
        Assert.Equal(0, await db.AcilisKalemleri.CountAsync());
    }

    [Fact]
    public async Task Has_4_ondaligi_asan_satir_HATA_yuvarlanmaz_karar5()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST has");
        using var db = vt.Olustur(k.TenantId);
        CariEkle(db, k.TenantId, "Ali Demir", "05320000001");
        CariEkle(db, k.TenantId, "Veli Kaya", "05320000002");
        await db.SaveChangesAsync();

        var csv = "ad,telefon,hasGramBakiye\nAli Demir,05320000001,1.12345\nVeli Kaya,05320000002,1.1234\n";
        var p = await Servis(db).Prova(Dto("acilis-bakiye", csv), k.Islem);

        Assert.Equal("1", Alan(p, "gecerliSatir"));
        var hata = await db.Set<IceAktarimSatiri>()
            .SingleAsync(x => x.OturumId == Alan(p, "oturumId") && x.Sonuc == IceAktarimSatirSonucu.Hata);
        Assert.Equal(DefterHataKodu.HasOndalik, hata.HataKodu);
        Assert.Equal(2, hata.SatirNo);
    }

    [Fact]
    public async Task Virgullu_sayi_iceren_satir_HATA_deftere_ON_KAT_yazilmaz()
    {
        // Hasım bulgusu (KRİTİK): TR tablolama programı "1,5" ve "1500,50" verir; eski okuyucu
        // virgülü binlik sayıp 15 / 150050 yazıyordu. Satır HATA olmalı, bakiye DEĞİŞMEMELİ.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST virgul");
        using var db = vt.Olustur(k.TenantId);
        var c1 = CariEkle(db, k.TenantId, "Ali Demir", "05320000011");
        var c2 = CariEkle(db, k.TenantId, "Veli Kaya", "05320000012");
        await db.SaveChangesAsync();

        var csv = "ad,telefon,tlBakiye,hasGramBakiye\n" +
                  "Ali Demir,05320000011,,\"1,5\"\n" +
                  "Veli Kaya,05320000012,\"1500,50\",\n";
        var s = Servis(db);
        var p = await s.Prova(Dto("acilis-bakiye", csv, politika: "ATLA"), k.Islem);
        Assert.Equal("0", Alan(p, "gecerliSatir"));
        Assert.Equal("2", Alan(p, "hataliSatir"));
        var hatalar = await db.Set<IceAktarimSatiri>()
            .Where(x => x.OturumId == Alan(p, "oturumId")).OrderBy(x => x.SatirNo).ToListAsync();
        Assert.All(hatalar, h => Assert.Equal(DefterHataKodu.SayiGecersiz, h.HataKodu));
        Assert.All(hatalar, h => Assert.Contains("1.5", h.HataMesaji));   // doğru yazım söylenir

        foreach (var id in new[] { c1, c2 })
        {
            var b = await db.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == id);
            Assert.Equal(0m, b.TlBakiye);
            Assert.Equal(0m, b.HasGramBakiye);
        }
    }

    [Fact]
    public async Task Binlik_benzeri_45_000_PROVADA_SAYI_BELIRSIZ_deftere_yazilmaz()
    {
        // Tur 3 ilkesi: '45.000' "kırk beş bin" mi "kırk beş" mi? Sessizce seçilmez → satır HATA.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST belirsiz");
        using var db = vt.Olustur(k.TenantId);
        var c1 = CariEkle(db, k.TenantId, "Ali Demir", "05320000021");
        var c2 = CariEkle(db, k.TenantId, "Veli Kaya", "05320000022");
        var c3 = CariEkle(db, k.TenantId, "Can Ak", "05320000023");
        await db.SaveChangesAsync();

        var csv = "ad,telefon,tlBakiye,hasGramBakiye\n" +
                  "Ali Demir,05320000021,45.000,\n" +
                  "Veli Kaya,05320000022,,1.500\n" +
                  "Can Ak,05320000023,45000,0.916\n";
        var s = Servis(db);
        var p = await s.Prova(Dto("acilis-bakiye", csv, politika: "ATLA"), k.Islem);
        Assert.Equal("1", Alan(p, "gecerliSatir"));
        Assert.Equal("2", Alan(p, "hataliSatir"));
        Assert.Equal("0", Alan(p, "yazilanKayit"));
        var hatalar = await db.Set<IceAktarimSatiri>()
            .Where(x => x.OturumId == Alan(p, "oturumId") && x.HataKodu != null)
            .OrderBy(x => x.SatirNo).ToListAsync();
        Assert.Equal(2, hatalar.Count);
        Assert.All(hatalar, h => Assert.Equal("SAYI_BELIRSIZ", h.HataKodu));
        Assert.All(hatalar, h => Assert.Contains("45000 ya da 45.5", h.HataMesaji));

        foreach (var id in new[] { c1, c2, c3 })
        {
            var b = await db.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == id);
            Assert.Equal(0m, b.TlBakiye);
            Assert.Equal(0m, b.HasGramBakiye);
        }
    }

    [Fact]
    public async Task Emanet_kolonu_olan_dosya_REDDEDILIR_bakiyeye_girmez_karar3()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST emanet");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Prova(
            Dto("acilis-bakiye", "ad,tlBakiye,emanetHasGram\nAli,10,5\n"), k.Islem));
        Assert.Contains("Emanet", hata.Message);
        Assert.Equal(0, await db.Set<IceAktarimOturumu>().CountAsync());   // oturum AÇILMADI
    }

    [Fact]
    public async Task Cari_bulunamazsa_kart_URETILMEZ_satir_HATA()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST yok");
        using var db = vt.Olustur(k.TenantId);
        var once = await db.Cariler.CountAsync();

        var p = await Servis(db).Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye\nHayalet Müşteri,0500,10\n"), k.Islem);

        Assert.Equal("0", Alan(p, "gecerliSatir"));
        Assert.Contains(DefterHataKodu.CariYok, Alan(p, "ilkHatalar"));
        Assert.Equal(once, await db.Cariler.CountAsync());
    }

    [Fact]
    public async Task Sutun_eslestirme_dosya_basligini_bizim_alana_cevirir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST eşleme");
        using var db = vt.Olustur(k.TenantId);
        UrunEkle(db, k.TenantId, "G47B-E1");
        await db.SaveChangesAsync();
        var s = Servis(db);

        var csv = "Stok Kodu,Miktar\nG47B-E1,4\n";
        // Eşleme yoksa zorunlu kolon eksik → 400.
        await Assert.ThrowsAsync<BadRequestException>(() => s.Prova(Dto("acilis-stok", csv), k.Islem));

        var p = await s.Prova(Dto("acilis-stok", csv, esleme: new() { ["kod"] = "Stok Kodu", ["adet"] = "Miktar" }), k.Islem);
        Assert.Equal("1", Alan(p, "gecerliSatir"));

        // Bilinmeyen hedef alan ve dosyada olmayan başlık 400.
        await Assert.ThrowsAsync<BadRequestException>(() => s.Prova(
            Dto("acilis-stok", csv, esleme: new() { ["yokAlan"] = "Miktar", ["kod"] = "Stok Kodu" }), k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() => s.Prova(
            Dto("acilis-stok", csv, esleme: new() { ["kod"] = "Olmayan", ["adet"] = "Miktar" }), k.Islem));
    }

    // ── 2) Stok açılışı ──────────────────────────────────────────────────────

    [Fact]
    public async Task Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST devir");
        using var db = vt.Olustur(k.TenantId);
        var u1 = UrunEkle(db, k.TenantId, "G47B-D1");
        var u2 = UrunEkle(db, k.TenantId, "G47B-D2");
        await db.SaveChangesAsync();

        var s = Servis(db);
        var p = await s.Prova(Dto("acilis-stok", "kod,adet,birimMaliyetTl\nG47B-D1,5,\nG47B-D2,2,1250.75\n"), k.Islem);
        Assert.Equal("2", Alan(p, "gecerliSatir"));
        Assert.Equal("5", Alan(p, "ozet", "MaliyetiBilinmeyenAdet"));
        await s.Uygula(Alan(p, "oturumId"), Onay(s), k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var p1 = await oku.Set<StokPartisi>().AsNoTracking().SingleAsync(x => x.UrunId == u1);
        Assert.Equal(PartiKaynagi.Devir, p1.Kaynak);
        Assert.Null(p1.BirimMaliyetTl);                 // ⛔ SIFIR DEĞİL — bilinmiyor
        Assert.Equal(5m, p1.AdetGiren);
        var p2 = await oku.Set<StokPartisi>().AsNoTracking().SingleAsync(x => x.UrunId == u2);
        Assert.Equal(1250.75m, p2.BirimMaliyetTl);

        var stok = await oku.Stoklar.AsNoTracking().SingleAsync(x => x.UrunId == u1 && x.SubeId == k.SubeId);
        Assert.Equal(5m, stok.Adet);
        var h = await oku.Hareketler.AsNoTracking().SingleAsync(x => x.UrunId == u1);
        Assert.Equal("SAYIM_DUZELTME", h.Tur);
        Assert.Equal(0m, h.TlTutari);                   // para el değiştirmez
    }

    [Fact]
    public async Task Acilis_stoku_maliyet_SIFIR_satir_HATA_stok_var_satir_HATA()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST sıfır");
        using var db = vt.Olustur(k.TenantId);
        UrunEkle(db, k.TenantId, "G47B-S1");
        await db.SaveChangesAsync();
        var mevcutKod = await db.Urunler.Where(u => u.Id == k.UrunId).Select(u => u.Kod).SingleAsync();

        var p = await Servis(db).Prova(
            Dto("acilis-stok", $"kod,adet,birimMaliyetTl\nG47B-S1,1,0\n{mevcutKod},1,\nYOK-KOD,1,\n"), k.Islem);

        Assert.Equal("0", Alan(p, "gecerliSatir"));
        var kodlar = await db.Set<IceAktarimSatiri>().Where(x => x.OturumId == Alan(p, "oturumId"))
            .OrderBy(x => x.SatirNo).Select(x => x.HataKodu).ToListAsync();
        Assert.Equal(new string?[] { DefterHataKodu.MaliyetSifir, DefterHataKodu.StokVar, DefterHataKodu.UrunYok }, kodlar);
    }

    // ── 3) Tek transaction ───────────────────────────────────────────────────

    [Fact]
    public async Task Bir_satir_patlarsa_TUM_acilis_geri_alinir_kismi_goc_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST tx");
        using var db = vt.Olustur(k.TenantId);
        var c1 = CariEkle(db, k.TenantId, "Bir Müşteri", "05320000011");
        CariEkle(db, k.TenantId, "İki Müşteri", "05320000012");
        var c3 = CariEkle(db, k.TenantId, "Üç Müşteri", "05320000013");
        await db.SaveChangesAsync();

        var s = Servis(db);
        var csv = "ad,telefon,tlBakiye\nBir Müşteri,05320000011,100\nİki Müşteri,05320000012,200\nÜç Müşteri,05320000013,300\n";
        var p = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        Assert.Equal("3", Alan(p, "gecerliSatir"));

        // Yeniden doğrulamayı GEÇEN ama yazımda patlayan durum: 3. carinin anahtarını tutan
        // GERI_ALINDI bir kalem (eski sürüm kalıntısı) → UNIQUE ihlali, 1-2. satır yazıldıktan SONRA.
        db.AcilisKalemleri.Add(new AcilisKalemi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, OturumId = "eski", Tur = AcilisTuru.Cari, HedefId = c3,
            BenzersizAnahtar = AcilisKalemi.AnahtarUret(AcilisTuru.Cari, c3, null),
            Durum = AcilisKalemDurumu.GeriAlindi, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var hOnce = await db.Hareketler.CountAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => s.Uygula(Alan(p, "oturumId"), Onay(s), k.Islem));

        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(hOnce, await oku.Hareketler.CountAsync());                       // defterde iz YOK
        Assert.Equal(0m, (await oku.CariBakiyeler.AsNoTracking().SingleAsync(b => b.CariId == c1)).TlBakiye);
        var o = await oku.Set<IceAktarimOturumu>().AsNoTracking().SingleAsync(x => x.Id == Alan(p, "oturumId"));
        Assert.Equal(IceAktarimDurumu.Iptal, o.Durum);
    }

    // ── 4) Geri alma — ters kayıt ────────────────────────────────────────────

    [Fact]
    public async Task Geri_al_TERS_KAYITLA_bakiye_ve_stok_onceki_haline_doner_kayit_silinmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST geri");
        using var db = vt.Olustur(k.TenantId);
        var c1 = CariEkle(db, k.TenantId, "Ayşe Yılmaz", "05321112233");
        var u1 = UrunEkle(db, k.TenantId, "G47B-G1");
        await db.SaveChangesAsync();
        var s = Servis(db);

        var pb = await s.Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye,hasGramBakiye,eurBakiye\nAyşe Yılmaz,05321112233,700,3.25,40\n"), k.Islem);
        await s.Uygula(Alan(pb, "oturumId"), Onay(s), k.Islem);
        var ps = await s.Prova(Dto("acilis-stok", "kod,adet\nG47B-G1,6\n"), k.Islem);
        await s.Uygula(Alan(ps, "oturumId"), Onay(s), k.Islem);
        var hUygulandi = await db.Hareketler.CountAsync();

        var g1 = await s.GeriAl(Alan(pb, "oturumId"), k.Islem);
        var g2 = await s.GeriAl(Alan(ps, "oturumId"), k.Islem);
        Assert.Equal("1", Alan(g1, "geriAlinan"));
        Assert.Equal("1", Alan(g2, "geriAlinan"));

        using var oku = vt.Olustur(k.TenantId);
        var b = await oku.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == c1);
        Assert.Equal(0m, b.TlBakiye);
        Assert.Equal(0m, b.HasGramBakiye);
        Assert.Equal(0m, Kuyumcu.Api.Modules.Cari.DovizBakiye.Oku(b, "EUR"));
        await AynaSifir(oku, k.TenantId, c1);
        var stok = await oku.Stoklar.AsNoTracking().SingleAsync(x => x.UrunId == u1 && x.SubeId == k.SubeId);
        Assert.Equal(0m, stok.Adet);
        var parti = await oku.Set<StokPartisi>().AsNoTracking().SingleAsync(x => x.UrunId == u1);
        Assert.Equal(0m, parti.AdetKalan);

        // ADR-0004: defter satırı SİLİNMEDİ, ters satır EKLENDİ.
        Assert.True(await oku.Hareketler.CountAsync() > hUygulandi);
        Assert.Equal(2, await oku.AcilisKalemleri.CountAsync(x => x.Durum == AcilisKalemDurumu.GeriAlindi));

        // Anahtar serbest: düzeltilmiş açılış AYNI cariye yeniden girilebilir.
        var yeni = await s.Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye\nAyşe Yılmaz,05321112233,650\n"), k.Islem);
        Assert.Equal("1", Alan(yeni, "gecerliSatir"));
        await s.Uygula(Alan(yeni, "oturumId"), Onay(s), k.Islem);
        using var son = vt.Olustur(k.TenantId);
        Assert.Equal(650m, (await son.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == c1)).TlBakiye);
        await AynaSifir(son, k.TenantId, c1);
    }

    [Fact]
    public async Task Acilis_stogu_tuketilmisse_geri_al_REDDEDILIR_kismi_geri_alma_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST tüketim");
        using var db = vt.Olustur(k.TenantId);
        var u1 = UrunEkle(db, k.TenantId, "G47B-T1");
        await db.SaveChangesAsync();
        var s = Servis(db);
        var p = await s.Prova(Dto("acilis-stok", "kod,adet\nG47B-T1,6\n"), k.Islem);
        await s.Uygula(Alan(p, "oturumId"), Onay(s), k.Islem);

        // Satış simülasyonu: partiden 1 adet tüketildi (maliyet motoru yolu).
        var motor = new Kuyumcu.Api.Modules.Maliyet.MaliyetMotoru(db);
        await motor.PartiTuket(k.TenantId, k.SubeId, u1, 1m, DateTime.UtcNow, "FIFO");
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => s.GeriAl(Alan(p, "oturumId"), k.Islem));

        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(6m, (await oku.Stoklar.AsNoTracking().SingleAsync(x => x.UrunId == u1 && x.SubeId == k.SubeId)).Adet);
        var o = await oku.Set<IceAktarimOturumu>().AsNoTracking().SingleAsync(x => x.Id == Alan(p, "oturumId"));
        Assert.Equal(IceAktarimDurumu.Uygulandi, o.Durum);
    }

    // ── 5) Aynı dosya iki kez · ATLA · mühür ─────────────────────────────────

    [Fact]
    public async Task Ayni_dosya_IKINCI_kez_uygulanamaz_409_SHA256()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST sha");
        using var db = vt.Olustur(k.TenantId);
        CariEkle(db, k.TenantId, "Ali Demir", "05320000001");
        await db.SaveChangesAsync();
        var s = Servis(db);
        const string csv = "ad,telefon,tlBakiye\nAli Demir,05320000001,100\n";

        var p1 = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        var p2 = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);   // aynı dosya, ikinci oturum
        await s.Uygula(Alan(p1, "oturumId"), Onay(s), k.Islem);

        var hata = await Assert.ThrowsAsync<ConflictException>(() => s.Uygula(Alan(p2, "oturumId"), Onay(s), k.Islem));
        Assert.Contains("daha önce uygulandı", hata.Message);

        // Üçüncü prova uyarıyı kendisi taşır.
        var p3 = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        Assert.Equal(Alan(p1, "oturumId"), Alan(p3, "ayniDosyaOturumId"));

        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(1, await oku.Hareketler.CountAsync(h => h.Tur == "ACILIS"));     // İKİ KEZ yazılmadı
    }

    [Fact]
    public async Task ATLA_politikasi_zaten_acilisi_olan_satiri_atlar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST atla");
        using var db = vt.Olustur(k.TenantId);
        CariEkle(db, k.TenantId, "Ali Demir", "05320000001");
        CariEkle(db, k.TenantId, "Veli Kaya", "05320000002");
        await db.SaveChangesAsync();
        var s = Servis(db);
        var p1 = await s.Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye\nAli Demir,05320000001,100\n"), k.Islem);
        await s.Uygula(Alan(p1, "oturumId"), Onay(s), k.Islem);

        var csv = "ad,telefon,tlBakiye\nAli Demir,05320000001,100\nVeli Kaya,05320000002,50\n";
        var hataP = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        Assert.Equal("1", Alan(hataP, "hataliSatir"));
        var atlaP = await s.Prova(Dto("acilis-bakiye", csv, politika: "ATLA"), k.Islem);
        Assert.Equal("1", Alan(atlaP, "atlananSatir"));
        Assert.Equal("1", Alan(atlaP, "gecerliSatir"));
    }

    [Fact]
    public async Task Muhurlu_kurulumda_acilis_icee_aktarimi_409_arka_kapi_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST mühür");
        using var db = vt.Olustur(k.TenantId);
        CariEkle(db, k.TenantId, "Ali Demir", "05320000001");
        db.KurulumDurumlari.Add(new KurulumDurumu
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Muhurlendi = true, MuhurTarihi = DateTime.UtcNow,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<ApiException>(() => Servis(db).Prova(
            Dto("acilis-bakiye", "ad,telefon,tlBakiye\nAli Demir,05320000001,100\n"), k.Islem));
        Assert.Equal(409, hata.StatusCode);
    }

    [Fact]
    public async Task Baska_kiracinin_acilis_oturumu_geri_alinamaz_404()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST kiracı B");
        using var dbA = vt.Olustur(a.TenantId);
        CariEkle(dbA, a.TenantId, "Ali Demir", "05320000001");
        await dbA.SaveChangesAsync();
        var sA = Servis(dbA);
        var p = await sA.Prova(Dto("acilis-bakiye", "ad,telefon,tlBakiye\nAli Demir,05320000001,100\n"), a.Islem);
        await sA.Uygula(Alan(p, "oturumId"), Onay(sA), a.Islem);

        using var dbB = vt.Olustur(b.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbB).GeriAl(Alan(p, "oturumId"), b.Islem));
    }

    // ── 6) Ölçüm: 2.000 satırlık açılış stoku provası ≤ 60 sn ────────────────

    [Fact]
    public async Task Iki_bin_satirlik_acilis_stoku_provasi_60_saniyede_biter()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST 2000");
        using var db = vt.Olustur(k.TenantId);
        var sb = new StringBuilder("kod,adet,birimMaliyetTl\n");
        for (var i = 0; i < 2000; i++)
        {
            UrunEkle(db, k.TenantId, $"V{i:D5}");
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"V{i:D5},{1 + i % 3},{(i % 2 == 0 ? "" : "1500.5")}\n");
        }
        await db.SaveChangesAsync();

        var sure = Stopwatch.StartNew();
        var p = await Servis(db).Prova(Dto("acilis-stok", sb.ToString()), k.Islem);
        sure.Stop();

        Assert.Equal("2000", Alan(p, "gecerliSatir"));
        Assert.True(sure.Elapsed < TimeSpan.FromSeconds(60), $"prova {sure.Elapsed.TotalSeconds:F1} sn sürdü");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GOC-47B [HTTP] — model bağlama, camelCase sözleşme, 409 kodu (servis testi göremez).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Goc47BHttpTestleri(HttpFikstur f)
{
    private static object Govde(string csv, string kaynak = "acilis-bakiye", object? esleme = null) => new
    {
        kaynak,
        dosyaAdi = "acilis.csv",
        icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        gerekce = "Eski programdan açılış bakiyeleri taşınıyor",
        yinelenenPolitikasi = "HATA",
        kolonEslemesi = esleme,
    };

    [Fact]
    public async Task Ayni_dosya_ikinci_uygulama_HTTP_409_ozet_camelCase()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKETGOC-47B-TEST http sha");
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var now = DateTime.UtcNow; var id = Kimlik.Ulid();
            db.Cariler.Add(new Cari { Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Ali Demir",
                Telefon = "05320000001", Aktif = true, Olusturuldu = now, Guncellendi = now });
            db.CariBakiyeler.Add(new CariBakiye { CariId = id, TenantId = k.TenantId, Guncellendi = now });
            await db.SaveChangesAsync();
        }
        var c = f.Ortam.Istemci(k);
        var ayar = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<IceAktarimAyarlari>(f.Ortam.Services);
        const string csv = "Müşteri,Tel,tlBakiye\nAli Demir,05320000001,125.40\n";
        var esleme = new { ad = "Müşteri", telefon = "Tel" };

        var r1 = await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde(csv, esleme: esleme));
        Assert.Equal(System.Net.HttpStatusCode.OK, r1.StatusCode);
        var j1 = await r1.Json();
        Assert.Equal(125.40m, j1.GetProperty("ozet").GetProperty("tlToplam").GetDecimal());
        var r2 = await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde(csv, esleme: esleme));
        var o2 = (await r2.Json()).GetProperty("oturumId").GetString();

        var u1 = await c.PostAsJsonAsync($"/api/v1/ice-aktarim/{j1.GetProperty("oturumId").GetString()}/uygula",
            new { onay = ayar.UygulaOnayMetni });
        Assert.Equal(System.Net.HttpStatusCode.OK, u1.StatusCode);
        var u2 = await c.PostAsJsonAsync($"/api/v1/ice-aktarim/{o2}/uygula", new { onay = ayar.UygulaOnayMetni });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, u2.StatusCode);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GOC-47B [bUnit] — sütun eşleştirme kilidi, özet, hata kodu çevirisi, geri alma metni.
// ⚠ Her olay sonrası bekleme WaitForAssertion ile (sabit uyku YASAK).
// ═════════════════════════════════════════════════════════════════════════════

// Saf (DB'siz) sayı/tavan sınır testleri — hasım bulguları KRİTİK + DÜŞÜK.
public sealed class Goc47BSayiTestleri
{
    private static readonly AcilisBakiyeKaynagi Bakiye = new();
    private static readonly AcilisStokKaynagi Stok = new();

    private static SatirSonucu B(string tl = "", string has = "", string usd = "") =>
        Bakiye.Dogrula(new Dictionary<string, string>
        {
            ["ad"] = "Ayşe", ["tlBakiye"] = tl, ["hasGramBakiye"] = has, ["usdBakiye"] = usd,
        });

    private static SatirSonucu S(string adet, string maliyet = "") =>
        Stok.Dogrula(new Dictionary<string, string> { ["kod"] = "K1", ["adet"] = adet, ["birimMaliyetTl"] = maliyet });

    [Theory]
    [InlineData("1,5")]
    [InlineData("1500,50")]
    [InlineData("1,000")]
    [InlineData("1.500,50")]
    [InlineData("1,500.50")]
    public void Virgullu_sayi_hucresi_her_defter_kolonunda_SAYI_GECERSIZ(string hucre)
    {
        foreach (var s in new[] { B(tl: hucre), B(has: hucre), B(tl: "5", usd: hucre), S(hucre), S("1", hucre) })
        {
            Assert.Null(s.Dto);
            Assert.Equal(DefterHataKodu.SayiGecersiz, s.HataKodu);
            Assert.Contains("NOKTA", s.HataMesaji);
        }
    }

    [Fact]
    public void Nokta_ondalik_ve_isaret_okunur_us_ve_bosluk_red()
    {
        var d = Assert.IsType<AcilisBakiyeSatiri>(B(tl: " -1500.50 ", has: "+1.5").Dto);
        Assert.Equal(-1500.50m, d.Tl);
        Assert.Equal(1.5m, d.Has);
        Assert.Equal(DefterHataKodu.SayiGecersiz, B(tl: "1e3").HataKodu);
        Assert.Equal(DefterHataKodu.SayiGecersiz, B(tl: "1 500").HataKodu);
    }

    // ── Tur 3 (orkestratör ilkesi): binlik ayracına benzeyen NOKTA belirsizdir ──
    private static SatirSonucu CariKart(string limit) =>
        new CariKaynagi().Dogrula(new Dictionary<string, string>
        {
            ["tur"] = "MUSTERI", ["ad"] = "Ayşe", ["krediLimitiTl"] = limit,
        });

    private static SatirSonucu UrunKart(string kolon, string hucre)
    {
        var ham = new Dictionary<string, string>
        {
            ["tip"] = "ZIYNET", ["kod"] = "K1", ["ad"] = "Bilezik", ["ayarId"] = "K22", ["gramAgirlik"] = "12.5",
        };
        ham[kolon] = hucre;
        return new UrunKaynagi().Dogrula(ham);
    }

    [Theory]
    [InlineData("45.000")]
    [InlineData("1.500")]
    [InlineData("2.500.000")]
    [InlineData("-1.500")]
    public void Binlik_benzeri_noktali_sayi_her_kolonda_SAYI_BELIRSIZ(string hucre)
    {
        // TL, döviz, has, adet, maliyet (Faz B) + kredi limiti TL, gram, iscilik, fiyat (Faz A)
        var sonuclar = new[]
        {
            B(tl: hucre), B(has: hucre), B(tl: "5", usd: hucre), S(hucre), S("1", hucre),
            CariKart(hucre), UrunKart("gramAgirlik", hucre), UrunKart("tasAgirlik", hucre),
            UrunKart("iscilikTl", hucre), UrunKart("satisFiyati", hucre),
        };
        foreach (var s in sonuclar)
        {
            Assert.Null(s.Dto);
            Assert.Equal("SAYI_BELIRSIZ", s.HataKodu);
            Assert.Contains("Binlik ayracı kullanmayın; 45000 ya da 45.5 yazın.", s.HataMesaji);
            Assert.Contains($"'{hucre}'", s.HataMesaji);
        }
        Assert.True(CsvOkuyucu.BinlikBelirsizMi(" " + hucre + " "));
    }

    [Theory]
    [InlineData("45.5", "45.5")]
    [InlineData("0.916", "0.916")]
    [InlineData("13.9232", "13.9232")]
    [InlineData("45000", "45000")]
    [InlineData("1.25", "1.25")]
    [InlineData("1500.50", "1500.50")]
    public void Belirsiz_olmayan_nokta_ondalik_GECERLI(string hucre, string beklenen)
    {
        var deger = decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(CsvOkuyucu.BinlikBelirsizMi(hucre));
        Assert.Equal(deger, Assert.IsType<AcilisBakiyeSatiri>(B(has: hucre).Dto).Has);
        Assert.NotNull(UrunKart("gramAgirlik", hucre).Dto);
        // Diğer kolonlarda ondalık sayısı/tam sayı kuralı ayrıca reddedebilir; ama ASLA belirsiz değil.
        foreach (var s in new[] { B(tl: hucre), B(tl: "1", usd: hucre), S(hucre), S("1", hucre), CariKart(hucre),
                                  UrunKart("iscilikTl", hucre), UrunKart("satisFiyati", hucre) })
        {
            Assert.NotEqual("SAYI_BELIRSIZ", s.HataKodu);
            Assert.NotEqual(DefterHataKodu.SayiGecersiz, s.HataKodu);
        }
    }

    [Fact]
    public void Has_tavani_DB_kolonuna_sigar_999999999_9999_kabul_ustu_TUTAR_ASIRI()
    {
        var d = Assert.IsType<AcilisBakiyeSatiri>(B(has: "999999999.9999").Dto);
        Assert.Equal(999_999_999.9999m, d.Has);
        Assert.Equal(-999_999_999.9999m, Assert.IsType<AcilisBakiyeSatiri>(B(has: "-999999999.9999").Dto).Has);
        Assert.Equal(DefterHataKodu.TutarAsiri, B(has: "1000000000").HataKodu);
        Assert.Equal(DefterHataKodu.TutarAsiri, B(has: "-1000000000").HataKodu);
    }
}

public sealed class Goc47BEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Sablonlar = """
        {"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM","maksSatir":5000,
         "maksDosyaBayt":2097152,"gerekceAsgari":20,"politikalar":["HATA","ATLA"],
         "kaynaklar":[
           {"ad":"acilis-stok","defter":true,"kolonlar":[{"ad":"kod","zorunlu":true,"ornek":"BLZ-1"},
                                     {"ad":"adet","zorunlu":true,"ornek":"3"},
                                     {"ad":"birimMaliyetTl","zorunlu":false,"ornek":""}]},
           {"ad":"cari","defter":false,"kolonlar":[{"ad":"tur","zorunlu":true,"ornek":"MUSTERI"}]}]}
        """;

    private const string ProvaYaniti = """
        {"oturumId":"o1","kaynak":"acilis-stok","durum":"PROVA","yinelenenPolitikasi":"HATA",
         "toplamSatir":3,"gecerliSatir":2,"hataliSatir":1,"atlananSatir":0,
         "yazilanKayit":0,"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM",
         "bilinmeyenKolonlar":[],"ayniDosyaOturumId":"o0",
         "ozet":{"satirSayisi":2,"tlToplam":0,"hasToplam":0,"dovizToplam":{},
                 "toplamAdet":7,"maliyetiBilinmeyenAdet":5,"toplamMaliyetTl":2501.5,"toplamHasGram":64.1667},
         "ilkHatalar":[{"satirNo":4,"hataKodu":"MALIYET_SIFIR","hataMesaji":"birimMaliyetTl sıfır olamaz"}]}
        """;

    public Goc47BEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/ice-aktarim/sablonlar") => Sablonlar,
                var y when y.Contains("/ice-aktarim/gecmis") => """{"toplam":0,"limit":20,"kayitlar":[]}""",
                var y when y.EndsWith("/ice-aktarim/prova") => ProvaYaniti,
                var y when y.EndsWith("/uygula") =>
                    """{"oturumId":"o1","durum":"UYGULANDI","uygulananSatir":2,"atlananSatir":0,"hataliSatir":1,"geriAlinabilir":true}""",
                var y when y.EndsWith("/geri-al") =>
                    """{"oturumId":"o1","durum":"GERI_ALINDI","pasifeAlinan":0,"geriAlinan":2,"mesaj":"Ters kayıt"}""",
                var y when y.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VeriIceAktarim> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-prova")));
        istekler.Clear();
        return e;
    }

    private static void Sec(IRenderedComponent<Kuyumcu.Web.Pages.VeriIceAktarim> e, string secici, string deger)
    {
        e.Find($"#{secici}").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll($"#{secici}-liste li.secici-secenek")));
        e.FindAll($"#{secici}-liste li.secici-secenek").First(l => l.GetAttribute("data-deger") == deger).Click();
    }

    [Fact]
    public void Sutun_eslestirme_zorunlu_eksikse_PROVA_kilitli_sebep_yazar_secince_govdeye_gider()
    {
        var e = Ciz();
        Sec(e, "ia-kaynak", "acilis-stok");
        // Dosya başlıkları BİZİMKİ DEĞİL — kullanıcı eşler.
        e.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("Stok Kodu,Miktar\nBLZ-1,3\n", "vitrin.csv"));
        e.Find("#ia-gerekce").Input("Eski programdan vitrin stoğu taşınıyor");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-esle-kod")));
        Assert.True(e.Find("#ia-prova").HasAttribute("disabled"));
        e.Find("#ia-nasil-ac").Click();   // kural katlanırda (kapalı gelir)
        Assert.Equal(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.OndalikNotu, e.Find("#ia-ondalik-notu").TextContent);
        Assert.Equal(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.BinlikNotu, e.Find("#ia-binlik-notu").TextContent);
        Assert.Contains("45.000", e.Find("#ia-binlik-notu").TextContent);
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.EslestirmeEksik, 2),
            e.Find("#ia-kilit-sebep").TextContent);

        Sec(e, "ia-esle-kod", "Stok Kodu");
        Sec(e, "ia-esle-adet", "Miktar");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));

        e.Find("#ia-prova").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/ice-aktarim/prova")));
        var govde = istekler.First(i => i.Yol.EndsWith("/ice-aktarim/prova")).Govde;
        using var j = JsonDocument.Parse(govde);
        var es = j.RootElement.GetProperty("kolonEslemesi");
        Assert.Equal("Stok Kodu", es.GetProperty("kod").GetString());
        Assert.Equal("Miktar", es.GetProperty("adet").GetString());
        Assert.Equal("acilis-stok", j.RootElement.GetProperty("kaynak").GetString());
    }

    [Fact]
    public void Prova_ozeti_maliyetsiz_adet_baglantisi_ayni_dosya_uyarisi_ve_cevrilmis_hata_kodu()
    {
        var e = Ciz();
        Sec(e, "ia-kaynak", "acilis-stok");
        e.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("kod,adet\nBLZ-1,3\n", "vitrin.csv"));
        e.Find("#ia-gerekce").Input("Eski programdan vitrin stoğu taşınıyor");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));
        e.Find("#ia-prova").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-defter-ozet")));
        Assert.Equal("/raporlar/kar-zarar", e.Find("#ia-ozet-maliyetsiz").GetAttribute("href"));
        Assert.Contains("5", e.Find("#ia-ozet-maliyetsiz").TextContent);
        Assert.NotNull(e.Find("#ia-ayni-dosya"));
        var tablo = e.Find("#ia-hata-tablosu").TextContent;
        Assert.Contains(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.HkMaliyetSifir, tablo);
        Assert.DoesNotContain("MALIYET_SIFIR", tablo);   // UX-S31: ham kod ekrana basılmaz
    }

    [Fact]
    public void Defter_kaynaginda_geri_al_TERS_KAYIT_onayi_ister_ve_sonucu_banda_yazar()
    {
        var e = Ciz();
        Sec(e, "ia-kaynak", "acilis-stok");
        e.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("kod,adet\nBLZ-1,3\n", "vitrin.csv"));
        e.Find("#ia-gerekce").Input("Eski programdan vitrin stoğu taşınıyor");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));
        e.Find("#ia-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-onay")));
        e.Find("#ia-onay").Input("VERİLERİ İÇE AKTARMAYI ONAYLIYORUM");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-uygula").HasAttribute("disabled")));
        e.Find("#ia-uygula").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-geri-al")));
        Assert.NotNull(e.Find("#ia-geri-al-ipucu"));
        e.Find("#ia-geri-al").Click();
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.GeriAlOnayDefter, e.Markup));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/geri-al"));   // onaysız istek YOK

        e.Find("#ia-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/ice-aktarim/o1/geri-al")));
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.Goc47BSozlugu.Tr.GeriAlindiDefter + " — 2", e.Markup));
    }
}
