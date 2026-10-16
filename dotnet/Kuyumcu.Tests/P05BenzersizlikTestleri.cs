using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-05 (Y-2) — DOĞAL ANAHTAR BENZERSİZLİK KISITLARI [gerçek MySQL]
//
// BULUNAN AÇIK: stoklar(tenant,şube,ürün) · urunler(tenant,kod) ·
// urunler(tenant,barkod) · cariler(tenant,vergi_no) benzersizliği YALNIZ
// uygulama katmanındaki `AnyAsync` ön kontrolüyle korunuyordu. Ön kontrol ile
// INSERT arasındaki pencerede ikinci istek sessizce ikinci satırı yazıyordu;
// cariler.vergi_no için ön kontrol HİÇ YOKTU.
//
// Bu dosya kısıtı ÜÇ katmanda birden kilitler:
//   1. veritabanı  — ikinci INSERT 1062 ile reddedilir (ön kontrol devre dışı)
//   2. boru hattı  — 1062 → 409 + Türkçe ALAN ALTI mesaj (HataYakalayici)
//   3. uç          — eşzamanlı iki POST'tan biri 409 alır, satır sayısı 1 kalır
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P05VeritabaniKisitTestleri(TestVeritabani vt)
{
    private static string Ulid() => Kimlik.Ulid();

    /// <summary>Kısıt ihlalinin MySQL hata numarası ve indeks adı.</summary>
    private static (int? Numara, string? Indeks) Ihlal(DbUpdateException ex)
    {
        var ic = ex.InnerException;
        var numara = ic?.GetType().GetProperty("Number")?.GetValue(ic) as int?;
        return (numara, BenzersizlikSozlugu.IndeksAdiniAyikla(ic?.Message));
    }

    [Fact]
    public void Stok_ayni_sube_ve_urun_icin_IKINCI_satir_VERITABANINDA_reddedilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 stok ikiz");

        // KiraciKur zaten bir stok satırı açtı; ikincisi kısıta çarpmalı.
        db.Stoklar.Add(new Stok
        {
            Id = Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = k.UrunId,
            Adet = 5m, ToplamHasGram = 5m, Guncellendi = DateTime.UtcNow,
        });

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        var (numara, indeks) = Ihlal(ex);
        Assert.Equal(1062, numara);
        // Ayikla ham adı döndürür (tablo öneki dâhil); sözlük son parçayı çözer.
        Assert.EndsWith("UQ_stoklar_tenant_sube_urun", indeks);
    }

    [Fact]
    public void Urun_ayni_kod_icin_IKINCI_satir_VERITABANINDA_reddedilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 kod ikiz");
        var mevcut = db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId);

        db.Urunler.Add(YeniUrun(k.TenantId, kod: mevcut.Kod, barkod: Ulid()));

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        var (numara, indeks) = Ihlal(ex);
        Assert.Equal(1062, numara);
        // Ayikla ham adı döndürür (tablo öneki dâhil); sözlük son parçayı çözer.
        Assert.EndsWith("UQ_urunler_tenant_kod", indeks);
    }

    [Fact]
    public void Urun_ayni_barkod_icin_IKINCI_satir_VERITABANINDA_reddedilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 barkod ikiz");
        var barkod = "P05-" + Ulid()[..10];

        db.Urunler.Add(YeniUrun(k.TenantId, kod: "P05A-" + Ulid()[..8], barkod: barkod));
        db.SaveChanges();
        db.Urunler.Add(YeniUrun(k.TenantId, kod: "P05B-" + Ulid()[..8], barkod: barkod));

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        var (numara, indeks) = Ihlal(ex);
        Assert.Equal(1062, numara);
        // Ayikla ham adı döndürür (tablo öneki dâhil); sözlük son parçayı çözer.
        Assert.EndsWith("UQ_urunler_tenant_barkod", indeks);
    }

    /// <summary>
    /// MySQL sözleşmesi: UNIQUE indeks BİRDEN ÇOK NULL kabul eder. Barkodsuz
    /// ürün sayısı sınırsızdır — kısıt "barkod zorunlu" ANLAMINA GELMEZ.
    /// </summary>
    [Fact]
    public void Barkodu_BOS_olan_uc_urun_yan_yana_YASAR_NULL_coklu_kabul()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 barkod null");

        var once = db.Urunler.Count(u => u.TenantId == k.TenantId && u.Barkod == null);
        for (var i = 0; i < 3; i++)
            db.Urunler.Add(YeniUrun(k.TenantId, kod: $"P05N{i}-" + Ulid()[..8], barkod: null));
        db.SaveChanges();   // üç NULL yan yana — kısıt patlamamalı

        Assert.Equal(once + 3, db.Urunler.Count(u => u.TenantId == k.TenantId && u.Barkod == null));
    }

    [Fact]
    public void Cari_ayni_vergi_no_icin_IKINCI_satir_VERITABANINDA_reddedilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 vergi ikiz");
        var vergiNo = "1234567890";

        db.Cariler.Add(YeniCari(k.TenantId, vergiNo));
        db.SaveChanges();
        db.Cariler.Add(YeniCari(k.TenantId, vergiNo));

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        var (numara, indeks) = Ihlal(ex);
        Assert.Equal(1062, numara);
        // Ayikla ham adı döndürür (tablo öneki dâhil); sözlük son parçayı çözer.
        Assert.EndsWith("UQ_cariler_tenant_vergi_no", indeks);
    }

    /// <summary>Kısıt KİRACI İÇİNDEDİR: başka işletme aynı vergi numarasını kullanabilir.</summary>
    [Fact]
    public void Ayni_vergi_no_BASKA_kiracida_KABUL_edilir()
    {
        using var db = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(db, "P05 vergi A");
        var b = TestKurulum.KiraciKur(db, "P05 vergi B");
        var vergiNo = "9876543210";

        db.Cariler.Add(YeniCari(a.TenantId, vergiNo));
        db.Cariler.Add(YeniCari(b.TenantId, vergiNo));
        db.SaveChanges();   // patlamamalı

        Assert.Equal(2, db.Cariler.Count(c =>
            c.VergiNo == vergiNo && (c.TenantId == a.TenantId || c.TenantId == b.TenantId)));
    }

    /// <summary>
    /// K-1 ÇIKIŞI (2026-09-18): cariler karısıtı FİLTRELİDİR — benzersizlik YALNIZ
    /// AKTİF cariler arasında geçerlidir. Arşivlenmiş (aktif=0) bir kart aynı
    /// numarayı taşıyor diye yeni kart açılamaması iş kuralına AYKIRIYDI ve canlıda
    /// QA-TEST-1 fiksturunu birleştirmeye zorluyordu.
    /// </summary>
    [Fact]
    public void Pasif_cari_ayni_vergi_no_ile_AKTIF_carinin_yaninda_YASAR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 vergi pasif");
        var vergiNo = "7010000001";

        var pasif = YeniCari(k.TenantId, vergiNo);
        pasif.Aktif = false;
        db.Cariler.Add(pasif);
        db.Cariler.Add(YeniCari(k.TenantId, vergiNo));   // aktif
        db.SaveChanges();                                 // patlamamalı

        Assert.Equal(2, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo));
        Assert.Equal(1, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo && c.Aktif));
    }

    /// <summary>İki ARŞİV kaydı aynı numarayı taşıyabilir (ifade NULL'a düşer).</summary>
    [Fact]
    public void Iki_PASIF_cari_ayni_vergi_no_ile_YASAR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 vergi iki pasif");
        var vergiNo = "7010000002";

        foreach (var _ in new[] { 1, 2 })
        {
            var c = YeniCari(k.TenantId, vergiNo);
            c.Aktif = false;
            db.Cariler.Add(c);
        }
        db.SaveChanges();   // patlamamalı

        Assert.Equal(2, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo && !c.Aktif));
    }

    /// <summary>
    /// Kurul bulgusu (ORTA): stok üst satırını aynı anda iki işlem açarsa
    /// İKİNCİ satır SESSİZCE oluşmaz — veritabanı 1062 verir. Kuralın kanıtı.
    /// </summary>
    [Fact]
    public void Ayni_urun_sube_icin_IKINCI_stok_satiri_YARISI_KAYBEDEN_tarafta_1062()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 stok yarış");
        var urun = YeniUrun(k.TenantId, kod: "P05S-" + Ulid()[..8], barkod: null);
        db.Urunler.Add(urun);
        db.SaveChanges();

        db.Stoklar.Add(YeniStok(k.TenantId, k.SubeId, urun.Id));
        db.SaveChanges();

        // "Yarışı kaybeden" taraf: ilk satırı GÖRMEDEN kendi satırını yazmaya çalışır.
        using var digeri = vt.OlusturSistem();
        digeri.Stoklar.Add(YeniStok(k.TenantId, k.SubeId, urun.Id));

        var ex = Assert.Throws<DbUpdateException>(() => digeri.SaveChanges());
        var (numara, indeks) = Ihlal(ex);
        Assert.Equal(1062, numara);
        Assert.EndsWith("UQ_stoklar_tenant_sube_urun", indeks);
        Assert.Equal(1, db.Stoklar.Count(s => s.TenantId == k.TenantId && s.UrunId == urun.Id));
    }

    /// <summary>PLAN: CariIban(tenant, iban_hash) zaten vardı → yalnız DOĞRULANIR.</summary>
    [Fact]
    public void CariIban_benzersizligi_HALA_YERINDE()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P05 iban");
        var iban = "TR330006100519786457841326";
        var now = DateTime.UtcNow;

        // iban_hash'i MasakHashDinleyicisi doldurur — aynı IBAN aynı hash'i verir.
        db.CariIbanlari.Add(new CariIban
        {
            Id = Ulid(), TenantId = k.TenantId, CariId = k.CariId,
            Iban = iban, Unvan = "P05", Olusturuldu = now,
        });
        db.SaveChanges();
        db.CariIbanlari.Add(new CariIban
        {
            Id = Ulid(), TenantId = k.TenantId, CariId = k.CariId,
            Iban = iban, Unvan = "P05", Olusturuldu = now,
        });

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.Equal(1062, Ihlal(ex).Numara);
    }

    private static Urun YeniUrun(string tenantId, string kod, string? barkod)
    {
        var now = DateTime.UtcNow;
        return new Urun
        {
            Id = Ulid(), TenantId = tenantId, Tip = "ZIYNET", Kod = kod, Ad = "P05 ürün",
            AyarId = "K22", Milyem = 916m, GramAgirlik = 1m, TasAgirlik = 0m,
            HasGrami = 0.916m, IscilikTl = 0m, Barkod = barkod, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        };
    }

    private static Stok YeniStok(string tenantId, string subeId, string urunId) => new()
    {
        Id = Ulid(), TenantId = tenantId, SubeId = subeId, UrunId = urunId,
        Adet = 1m, ToplamHasGram = 1m, Guncellendi = DateTime.UtcNow,
    };

    private static Cari YeniCari(string tenantId, string? vergiNo)
    {
        var now = DateTime.UtcNow;
        return new Cari
        {
            Id = Ulid(), TenantId = tenantId, Tur = "MUSTERI", Ad = "P05 cari",
            VergiNo = vergiNo, Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SAF TESTLER — sözlük ve normalleştirme (veritabanı gerekmez)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P05SozlukTestleri
{
    [Theory]
    [InlineData("urunler.UQ_urunler_tenant_kod", "kod")]
    [InlineData("UQ_urunler_tenant_barkod", "barkod")]
    [InlineData("cariler.UQ_cariler_tenant_vergi_no", "vergiNo")]
    [InlineData("stoklar.UQ_stoklar_tenant_sube_urun", "urunId")]
    [InlineData("cari_ibanlari.IX_cari_ibanlari_tenant_id_iban_hash", "iban")]
    public void Indeks_adi_DOGRU_alana_cozulur(string indeks, string beklenenAlan)
    {
        var ihlal = BenzersizlikSozlugu.Coz(indeks);
        Assert.Equal(beklenenAlan, ihlal.Alan);
        Assert.NotEqual(BenzersizlikSozlugu.GenelMesaj, ihlal.Mesaj);
    }

    /// <summary>
    /// Kurul bulgusu (ORTA): stok üst satırı yarışı SATIŞ ekranında görülür.
    /// Kasiyere "ikinci satır açılamaz" demek anlamsızdı; mesaj YAPILACAK İŞİ
    /// (tekrar dene) ve verinin bozulmadığını söylemek ZORUNDADIR.
    /// </summary>
    [Fact]
    public void Stok_yarisi_mesaji_kasiyere_TEKRAR_DENE_der_ve_veri_bozulmadi_bilgisi_verir()
    {
        var ihlal = BenzersizlikSozlugu.Coz("stoklar.UQ_stoklar_tenant_sube_urun");
        Assert.Equal("urunId", ihlal.Alan);
        Assert.Contains("tekrarlayın", ihlal.Mesaj);
        Assert.Contains("stok bozulmadı", ihlal.Mesaj);
        Assert.DoesNotContain("ikinci satır açılamaz", ihlal.Mesaj);
    }

    /// <summary>Cari mesajı kuralın FİLTRELİ olduğunu (AKTİF) söyler.</summary>
    [Fact]
    public void Cari_vergi_no_mesaji_kisitin_yalniz_AKTIF_kartlar_icin_oldugunu_SOYLER()
    {
        var ihlal = BenzersizlikSozlugu.Coz("cariler.UQ_cariler_tenant_vergi_no");
        Assert.Equal("vergiNo", ihlal.Alan);
        Assert.Contains("AKTİF", ihlal.Mesaj);
    }

    /// <summary>Bilinmeyen indeks: genel mesaj — indeks/tablo adı SIZDIRILMAZ.</summary>
    [Fact]
    public void Bilinmeyen_indeks_GENEL_mesaja_duser_ve_ic_kod_SIZMAZ()
    {
        var ihlal = BenzersizlikSozlugu.Coz("siparisler.IX_gizli_ic_indeks");
        Assert.Equal(BenzersizlikSozlugu.GenelAlan, ihlal.Alan);
        Assert.Equal(BenzersizlikSozlugu.GenelMesaj, ihlal.Mesaj);
        Assert.DoesNotContain("IX_gizli", ihlal.Mesaj);
    }

    [Theory]
    [InlineData("Duplicate entry 'T1-ABC' for key 'urunler.UQ_urunler_tenant_kod'",
                "urunler.UQ_urunler_tenant_kod")]
    [InlineData("Duplicate entry 'x' for key `cariler.UQ_cariler_tenant_vergi_no`",
                "cariler.UQ_cariler_tenant_vergi_no")]
    public void MySQL_1062_metninden_indeks_adi_AYIKLANIR(string metin, string beklenen)
        => Assert.Equal(beklenen, BenzersizlikSozlugu.IndeksAdiniAyikla(metin));

    [Fact]
    public void Ilgisiz_hata_metninden_indeks_adi_CIKMAZ()
        => Assert.Null(BenzersizlikSozlugu.IndeksAdiniAyikla("Deadlock found when trying to get lock"));

    /// <summary>
    /// Boş dizge NULL DEĞİLDİR ve ikinci boş dizge kısıtı ihlal ederdi —
    /// normalleştirme bu tuzağı tek yerde kapatır.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  1234567890 ", "1234567890")]
    public void Dogal_anahtar_normallestirmesi_BOS_degeri_NULL_yapar(string? girdi, string? beklenen)
        => Assert.Equal(beklenen, DogalAnahtar.Normalle(girdi));
}

// ═════════════════════════════════════════════════════════════════════════════
// BORU HATTI TESTİ — 1062 → 409 + alan altı Türkçe mesaj
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P05HataYakalayiciTestleri
{
    /// <summary>MySqlException taklidi: yansımayla okunan `Number` alanını taşır.</summary>
    private sealed class SahteVeritabaniHatasi(string mesaj, int numara) : Exception(mesaj)
    {
        public int Number { get; } = numara;
    }

    private static async Task<(int Kod, JsonElement Govde)> Calistir(Exception firlatilan)
    {
        var yakalayici = new HataYakalayici(
            _ => throw firlatilan, NullLogger<HataYakalayici>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/v1/urunler";
        var akis = new MemoryStream();
        ctx.Response.Body = akis;

        await yakalayici.InvokeAsync(ctx);

        akis.Position = 0;
        var metin = await new StreamReader(akis).ReadToEndAsync();
        return (ctx.Response.StatusCode, JsonDocument.Parse(metin).RootElement);
    }

    [Fact]
    public async Task Veritabani_1062_hatasi_409_ve_ALAN_ALTI_mesaja_cevrilir()
    {
        var (kod, govde) = await Calistir(new DbUpdateException(
            "An error occurred while saving",
            new SahteVeritabaniHatasi(
                "Duplicate entry 'T1-ABC' for key 'urunler.UQ_urunler_tenant_kod'", 1062)));

        Assert.Equal(409, kod);
        Assert.Equal("Conflict", govde.GetProperty("error").GetString());
        var alanlar = govde.GetProperty("alanlar");
        var mesaj = alanlar.GetProperty("kod")[0].GetString()!;
        Assert.Contains("stok kodu", mesaj);
        // İç kod SIZMAZ: indeks/tablo adı kullanıcı gövdesinde geçmez.
        Assert.DoesNotContain("UQ_urunler", govde.ToString());
    }

    /// <summary>1062 DIŞINDAKİ yazma hatası 409'a çevrilmez — 500 kalır.</summary>
    [Fact]
    public async Task Benzersizlik_DISI_yazma_hatasi_409_OLMAZ()
    {
        var (kod, govde) = await Calistir(new DbUpdateException(
            "An error occurred while saving",
            new SahteVeritabaniHatasi("Deadlock found when trying to get lock", 1213)));

        Assert.Equal(500, kod);
        Assert.False(govde.TryGetProperty("alanlar", out _));
    }

    /// <summary>Alan altı 409 servis yolundan da doğar (ön kontrol dalı).</summary>
    [Fact]
    public async Task Servisin_alan_cakismasi_409_alanlar_sozlugunu_TASIR()
    {
        var (kod, govde) = await Calistir(
            new AlanCakismasiException("vergiNo", "'123' vergi/TC numarası zaten kayıtlı"));

        Assert.Equal(409, kod);
        Assert.Equal("'123' vergi/TC numarası zaten kayıtlı",
            govde.GetProperty("alanlar").GetProperty("vergiNo")[0].GetString());
    }

    /// <summary>GERİLEME KAPISI: alan taşımayan hatanın gövdesi DEĞİŞMEDİ.</summary>
    [Fact]
    public async Task Alansiz_hatada_govde_ESKISI_GIBI_kalir_alanlar_BASILMAZ()
    {
        var (kod, govde) = await Calistir(new NotFoundException("Ürün bulunamadı"));

        Assert.Equal(404, kod);
        Assert.False(govde.TryGetProperty("alanlar", out _));
        Assert.Equal("Ürün bulunamadı", govde.GetProperty("message").GetString());
        Assert.Equal("Not Found", govde.GetProperty("error").GetString());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UÇ TESTLERİ — gerçek HTTP boru hattı
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P05UcTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object UrunGovde(string kod, string? barkod = null) => new
    {
        tip = "ZIYNET", kod, ad = "P05 bilezik", ayarId = "K22",
        gramAgirlik = 10m, tasAgirlik = 0m, barkod,
    };

    [Fact]
    public async Task Ayni_kodla_IKINCI_urun_409_ve_kod_alani_altinda_TURKCE_mesaj()
    {
        var k = YeniKiraci("P05 uç kod");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var kod = "P05-" + Guid.NewGuid().ToString("N")[..8];

        var ilk = await istemci.PostJson("/api/v1/urunler", UrunGovde(kod));
        var ikinci = await istemci.PostJson("/api/v1/urunler", UrunGovde(kod));

        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        var govde = await ikinci.Json();
        var mesaj = govde.GetProperty("alanlar").GetProperty("kod")[0].GetString()!;
        Assert.Contains("kodu zaten kullanımda", mesaj);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.Urunler.Count(u => u.TenantId == k.TenantId && u.Kod == kod));
    }

    /// <summary>
    /// EŞZAMANLI iki istek: biri 201, biri 409 — ve satır sayısı 1. Ön kontrol
    /// yarışı kaybetse bile veritabanı kısıtı ikinci satırı SESSİZCE yaratmaz.
    /// </summary>
    [Fact]
    public async Task Eszamanli_iki_istek_ayni_kodu_yazamaz_satir_sayisi_BIR()
    {
        var k = YeniKiraci("P05 yarış");
        var kod = "P05Y-" + Guid.NewGuid().ToString("N")[..8];

        var a = f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/urunler", UrunGovde(kod));
        var b = f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/urunler", UrunGovde(kod));
        var yanitlar = await Task.WhenAll(a, b);

        Assert.Equal(1, yanitlar.Count(y => y.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, yanitlar.Count(y => y.StatusCode == HttpStatusCode.Conflict));

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.Urunler.Count(u => u.TenantId == k.TenantId && u.Kod == kod));
    }

    [Fact]
    public async Task Ayni_vergi_no_ile_IKINCI_cari_409_ve_vergiNo_alani_altinda()
    {
        var k = YeniKiraci("P05 uç vergi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var vergiNo = "4440" + Random.Shared.Next(100000, 999999);

        var ilk = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ali", vergiNo });
        var ikinci = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Veli", vergiNo });

        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        var mesaj = (await ikinci.Json()).GetProperty("alanlar").GetProperty("vergiNo")[0].GetString()!;
        Assert.Contains("vergi/TC numarası", mesaj);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo));
    }

    /// <summary>
    /// Vergi numarası VERİLMEYEN cari sayısı sınırsızdır; boş dizge NULL'a
    /// çevrilir, ikinci kayıt 409 ALMAZ.
    /// </summary>
    [Fact]
    public async Task Vergi_numarasi_BOS_iki_cari_ARKA_ARKAYA_acilir_ve_NULL_yazilir()
    {
        var k = YeniKiraci("P05 vergi boş");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var ilk = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Boş 1", vergiNo = "" });
        var ikinci = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Boş 2", vergiNo = "   " });

        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ikinci.StatusCode);

        using var db = f.Vt.OlusturSistem();
        // KiraciKur'un açtığı cari de vergi numarasız — bu yüzden 3 satır beklenir.
        Assert.Equal(3, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == null));
        Assert.Equal(0, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == ""));
    }

    /// <summary>PATCH sözleşmesi: gönderilmeyen alan KORUNUR, çakışan değer 409 alır.</summary>
    [Fact]
    public async Task Cari_PATCH_vergi_no_gonderilmezse_KORUNUR_cakisirsa_409()
    {
        var k = YeniKiraci("P05 patch vergi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var vergiA = "5550" + Random.Shared.Next(100000, 999999);
        var vergiB = "5551" + Random.Shared.Next(100000, 999999);

        var a = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Cari A", vergiNo = vergiA });
        var b = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Cari B", vergiNo = vergiB });
        var bId = (await b.Json()).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Created, a.StatusCode);

        // 1) vergiNo GÖNDERİLMEDİ → değişmemeli
        var korundu = await istemci.PatchJson($"/api/v1/cari/{bId}", new { ad = "B düzeltildi" });
        Assert.Equal(HttpStatusCode.OK, korundu.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(vergiB, db.Cariler.Single(c => c.Id == bId).VergiNo);

        // 2) A'nın numarası B'ye yazılamaz
        var cakisma = await istemci.PatchJson($"/api/v1/cari/{bId}", new { vergiNo = vergiA });
        Assert.Equal(HttpStatusCode.Conflict, cakisma.StatusCode);
        Assert.Equal("vergiNo",
            (await cakisma.Json()).GetProperty("alanlar").EnumerateObject().First().Name);

        // 3) null gönderilirse SİLİNİR (PATCH sözleşmesi)
        var silindi = await istemci.PatchJson($"/api/v1/cari/{bId}", new { vergiNo = (string?)null });
        Assert.Equal(HttpStatusCode.OK, silindi.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Null(db.Cariler.Single(c => c.Id == bId).VergiNo);
    }

    /// <summary>
    /// K-1 ÇIKIŞI: kart PASİFE alındıktan sonra aynı vergi numarasıyla YENİ kart
    /// açılabilir (201) — hem ekran ön kontrolü hem veritabanı kabul eder.
    /// </summary>
    [Fact]
    public async Task Pasife_alinan_carinin_vergi_numarasi_YENI_karta_VERILEBILIR()
    {
        var k = YeniKiraci("P05 pasif vergi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var vergiNo = "7770" + Random.Shared.Next(100000, 999999);

        var eski = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Eski", vergiNo });
        Assert.Equal(HttpStatusCode.Created, eski.StatusCode);
        var eskiId = (await eski.Json()).GetProperty("id").GetString()!;

        var pasif = await istemci.DeleteAsync($"/api/v1/cari/{eskiId}");
        Assert.Equal(HttpStatusCode.OK, pasif.StatusCode);

        var yeni = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Yeni", vergiNo });
        Assert.Equal(HttpStatusCode.Created, yeni.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(2, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo));
        Assert.Equal(1, db.Cariler.Count(c => c.TenantId == k.TenantId && c.VergiNo == vergiNo && c.Aktif));
    }

    /// <summary>
    /// Arşiv kartı yeniden AKTİFE alınırken aynı numara başka bir aktif kartta ise
    /// istek 409 alır; 1062 kullanıcıya 500 olarak SIZMAZ.
    /// </summary>
    [Fact]
    public async Task Pasif_cariyi_AKTIFE_alirken_cakisan_vergi_no_409_verir()
    {
        var k = YeniKiraci("P05 aktife alma");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var vergiNo = "7771" + Random.Shared.Next(100000, 999999);

        var eski = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Eski", vergiNo });
        var eskiId = (await eski.Json()).GetProperty("id").GetString()!;
        await istemci.DeleteAsync($"/api/v1/cari/{eskiId}");
        var yeni = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Yeni", vergiNo });
        Assert.Equal(HttpStatusCode.Created, yeni.StatusCode);

        var geriAl = await istemci.PatchJson($"/api/v1/cari/{eskiId}", new { aktif = true });

        Assert.Equal(HttpStatusCode.Conflict, geriAl.StatusCode);
        var alanlar = (await geriAl.Json()).GetProperty("alanlar");
        Assert.Equal("vergiNo", alanlar.EnumerateObject().First().Name);
        // ⚠ Çakışma UYGULAMA ÖN KONTROLÜNDEN gelmeli, veritabanının 1062'sinden
        //   DEĞİL: 1062 yolu işlemi zehirler, günlüğe uyarı basar ve indeks bir
        //   gün düşse sessizce ikinci aktif kart doğar. Ayırt edici: ön kontrolün
        //   mesajı ÇAKIŞAN NUMARAYI içerir, sözlük mesajı içermez.
        Assert.Contains(vergiNo, alanlar.GetProperty("vergiNo")[0].GetString()!);
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.Cariler.Single(c => c.Id == eskiId).Aktif);
    }

    /// <summary>Kendi numarasını yeniden gönderen PATCH 409 ALMAZ (kendiyle çakışma yok).</summary>
    [Fact]
    public async Task Cari_PATCH_kendi_vergi_numarasini_tekrar_gonderince_409_ALMAZ()
    {
        var k = YeniKiraci("P05 patch kendi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var vergiNo = "5552" + Random.Shared.Next(100000, 999999);

        var c = await istemci.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Cari C", vergiNo });
        var id = (await c.Json()).GetProperty("id").GetString()!;

        var tekrar = await istemci.PatchJson($"/api/v1/cari/{id}", new { vergiNo });

        Assert.Equal(HttpStatusCode.OK, tekrar.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(vergiNo, db.Cariler.Single(x => x.Id == id).VergiNo);
    }

    [Fact]
    public async Task Ayni_barkodla_IKINCI_urun_409_ve_barkod_alani_altinda()
    {
        var k = YeniKiraci("P05 uç barkod");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var barkod = "P05B" + Random.Shared.Next(100000, 999999);

        var ilk = await istemci.PostJson("/api/v1/urunler",
            UrunGovde("P05B1-" + Guid.NewGuid().ToString("N")[..8], barkod));
        var ikinci = await istemci.PostJson("/api/v1/urunler",
            UrunGovde("P05B2-" + Guid.NewGuid().ToString("N")[..8], barkod));

        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        Assert.Contains("barkodu zaten kullanımda",
            (await ikinci.Json()).GetProperty("alanlar").GetProperty("barkod")[0].GetString());
    }
}
