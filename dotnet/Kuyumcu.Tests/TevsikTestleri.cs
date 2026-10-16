using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tevsik;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEVSİK ZORUNLULUĞU TESTLERİ — 459 s. VUK Genel Tebliği · PLAN-2 Adım 2
//
// Dayanak: 575 s. Genel Tebliğ, RG 30.11.2024/32738 — had 7.000 → 30.000 TL.
//
// Aşkın'ın şartı: eşiğin TAM SINIRI sınanır (29.999 / 30.000 / 30.001) ve
// "aynı gün" tanımı Europe/Istanbul gün sınırıyla doğrulanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TevsikKuralTestleri
{
    private static readonly TevsikKurali K = new(new TevsikEsikleri());

    // ── Tek işlem, tam sınır ────────────────────────────────────────────────

    [Theory]
    [InlineData(29_999.99, false)]   // haddin bir kuruş altı → serbest
    [InlineData(30_000.00, false)]   // TAM had → hâlâ serbest ("aşması hâlinde")
    [InlineData(30_000.01, true)]    // bir kuruş aşım → tevsik ZORUNLU
    [InlineData(50_000.00, true)]
    public void Tek_islemde_had_asilirsa_tevsik_gerekir(decimal tutar, bool gerekli)
        => Assert.Equal(gerekli, K.Degerlendir(0m, tutar).Gerekli);

    [Fact]
    public void Tam_had_serbesttir_kanun_asmasi_halinde_der()
    {
        // 459 s. VUK Genel Tebliği "aşması hâlinde" der; 30.000 TL'nin KENDİSİ aşım değildir.
        // Bu bir kuruşluk fark, bir ceza ile ceza olmaması arasındaki farktır.
        Assert.False(K.Degerlendir(0m, 30_000m).Gerekli);
        Assert.True(K.Degerlendir(0m, 30_000.01m).Gerekli);
    }

    // ── KÜMÜLATİF — kuralın asıl amacı ──────────────────────────────────────

    [Fact]
    public void Islemi_bolerek_had_atlatilamaz()
    {
        // 3 × 12.000 = 36.000 → tek 36.000 TL kadar ihlaldir.
        // Bu kural olmasa sistem bölerek kaçmaya sessizce izin verirdi.
        Assert.False(K.Degerlendir(0m, 12_000m).Gerekli);          // 1. işlem
        Assert.False(K.Degerlendir(12_000m, 12_000m).Gerekli);     // 2. → 24.000
        Assert.True(K.Degerlendir(24_000m, 12_000m).Gerekli);      // 3. → 36.000 ⛔
    }

    [Theory]
    [InlineData(25_000, 4_999.99, false)]   // toplam 29.999,99
    [InlineData(25_000, 5_000.00, false)]   // toplam tam 30.000
    [InlineData(25_000, 5_000.01, true)]    // toplam 30.000,01 ⛔
    public void Kumulatif_toplam_tam_sinirda_degerlendirilir(
        decimal onceki, decimal bu, bool gerekli)
        => Assert.Equal(gerekli, K.Degerlendir(onceki, bu).Gerekli);

    [Fact]
    public void Kumulatif_ihlalde_mesaj_gunun_onceki_toplamini_da_soyler()
    {
        var s = K.Degerlendir(24_000m, 12_000m);

        Assert.Contains("bugün", s.Mesaj);
        Assert.Contains("nakit sınırı", s.Mesaj);
        Assert.Equal(36_000m, s.GunlukNakitToplam);
        Assert.Equal(6_000m, s.Asim);
    }

    [Fact]
    public void Tek_islem_ihlalinde_mesaj_kumulatif_demez()
    {
        // Günün ilk işlemi tek başına aşıyorsa "toplamı" demek yanıltıcı olur.
        var s = K.Degerlendir(0m, 50_000m);
        Assert.DoesNotContain("aynı gün", s.Mesaj);
        Assert.Contains("nakit sınırı", s.Mesaj);
    }

    // ── İşaret ve sıfır ─────────────────────────────────────────────────────

    [Fact]
    public void Isaret_onemsizdir_tahsilat_da_tediye_de_hadde_tabidir()
    {
        // Kasadan çıkış negatif yazılır; had her iki yönde de geçerlidir.
        Assert.True(K.Degerlendir(0m, -50_000m).Gerekli);
        Assert.True(K.Degerlendir(-24_000m, -12_000m).Gerekli);
    }

    [Fact]
    public void Nakit_yoksa_kural_isletilmez()
    {
        // Tamamı karta/havaleye giden işlem zaten tevsik edilmiştir.
        Assert.False(K.Degerlendir(100_000m, 0m).Gerekli);
    }

    // ── Yapılandırma ────────────────────────────────────────────────────────

    [Fact]
    public void Had_yapilandirmadan_okunur_kod_degismez()
    {
        // ADR-0005: mevzuat rakamı değişirse kod değişmemeli.
        // Had 7.000'e dönerse (eski değer) kural o rakama göre çalışmalı.
        var eski = new TevsikKurali(new TevsikEsikleri { Had = 7_000m });

        Assert.True(eski.Degerlendir(0m, 7_000.01m).Gerekli);
        Assert.False(eski.Degerlendir(0m, 7_000m).Gerekli);
    }

    [Fact]
    public void Varsayilan_had_30000_TL_dir()
    {
        // RG 30.11.2024/32738 — 575 s. Tebliğ ile 7.000'den yükseltildi.
        Assert.Equal(30_000m, new TevsikEsikleri().Had);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KAPI — veritabanına karşı: kümülatif toplam ve GÜN SINIRI
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class TevsikKapiTestleri(TestVeritabani vt)
{
    private static readonly TevsikKapisi Kapi = TevsikKapisi.Varsayilan();

    private static void NakitHareket(
        KuyumcuDbContext db, Kiraci k, decimal tutar, DateTime tarihUtc, string odemeTuru = "NAKIT")
    {
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = tarihUtc, TlTutari = tutar, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = odemeTuru,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = tarihUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Gun_icinde_biriken_nakit_haddi_asinca_islem_DURUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik kümülatif");
        var an = DateTime.UtcNow;

        using var db = vt.Olustur(k.TenantId);
        NakitHareket(db, k, 25_000m, an);

        // 25.000 + 6.000 = 31.000 > 30.000 → fail-closed
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Kapi.Dogrula(db, k.TenantId, k.CariId, 6_000m, an));

        Assert.Contains("nakit sınırı", hata.Message);
        Assert.Contains("bugün", hata.Message);
    }

    // ── KARMA / kısmi ödeme — nakit payı ──────────────────────────────────
    //
    // TevsikKapisi.cs sonundaki "BİLİNEN SINIR" notu bu açığı belgeliyordu:
    // OdemeTuru tek etiket olduğu için "KARMA" hareketin nakit payı kümülatife
    // giremiyordu. 20.000 nakit + 20.000 kart → had sessizce deliniyordu.
    // Aşağıdaki iki test bu açığın KAPANDIĞINI ve kapatırken aşırıya
    // KAÇILMADIĞINI (kart payı nakit sayılmaz) sabitler.

    [Fact]
    public async Task KARMA_hareketin_NAKIT_PAYI_kumulatife_KATILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik karma nakit payı");
        var an = DateTime.UtcNow;
        using var db = vt.Olustur(k.TenantId);

        // 40.000 TL'lik satış: 20.000 nakit + 20.000 kart → etiket KARMA, nakit payı 20.000
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = an, TlTutari = 40_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "KARMA",
            NakitTutar = 20_000m, VeresiyeTutar = 0m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = an,
        });
        db.SaveChanges();

        // Ardından 15.000 TL saf nakit: 20.000 + 15.000 = 35.000 > 30.000 → DURMALI.
        // Nakit payı görülmezse kapı yalnız 15.000 görür ve işlem geçer — açık budur.
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Kapi.Dogrula(db, k.TenantId, k.CariId, 15_000m, an));
        Assert.Contains("35.000,00", hata.Message);
    }

    [Fact]
    public async Task KARMA_hareketin_KART_PAYI_nakit_SAYILMAZ()
    {
        // Açığı kapatmanın kolay ama YANLIŞ yolu: KARMA satırın TlTutari'sinin
        // tamamını nakit saymak. 40.000'in 20.000'i karttır; kart tevsik
        // edilmiş sayılır. Aksi hâlde yasal bir satış haksız yere durdurulur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik karma kart payı");
        var an = DateTime.UtcNow;
        using var db = vt.Olustur(k.TenantId);

        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = an, TlTutari = 40_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "KARMA",
            NakitTutar = 20_000m, VeresiyeTutar = 0m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = an,
        });
        db.SaveChanges();

        // 20.000 (nakit payı) + 9.000 = 29.000 ≤ 30.000 → GEÇMELİ.
        // TlTutari'nin tamamı sayılsaydı 49.000 olur ve haksız yere dururdu.
        var sonuc = await Kapi.Dogrula(db, k.TenantId, k.CariId, 9_000m, an);
        Assert.False(sonuc.Gerekli);
        Assert.Equal(29_000m, sonuc.GunlukNakitToplam);
    }

    [Fact]
    public async Task Had_altinda_kalan_birikim_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik had altı");
        var an = DateTime.UtcNow;

        using var db = vt.Olustur(k.TenantId);
        NakitHareket(db, k, 25_000m, an);

        var sonuc = await Kapi.Dogrula(db, k.TenantId, k.CariId, 5_000m, an);   // tam 30.000

        Assert.False(sonuc.Gerekli);
        Assert.Equal(30_000m, sonuc.GunlukNakitToplam);
    }

    [Fact]
    public async Task DUNKU_nakit_bugunku_toplama_KATILMAZ()
    {
        // "Aynı gün" kuralı: dünün nakiti bugünü kilitlememeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik dünkü");
        var an = DateTime.UtcNow;

        using var db = vt.Olustur(k.TenantId);
        NakitHareket(db, k, 29_000m, an.AddDays(-1));

        var sonuc = await Kapi.Dogrula(db, k.TenantId, k.CariId, 29_000m, an);
        Assert.False(sonuc.Gerekli);
    }

    [Fact]
    public async Task Gun_siniri_EUROPE_ISTANBUL_yerel_gunudur()
    {
        // ⚠ K5: UTC gün sınırı kullanılsaydı, İstanbul'da 01:00'de yapılan
        // nakit bir ÖNCEKİ UTC gününe düşer ve kümülatif toplam eksik
        // hesaplanırdı — yani had sessizce atlatılabilirdi.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik gün sınırı");

        // İstanbul'da 01:00 → UTC'de bir önceki günün 22:00'si.
        var yerelGun = new DateOnly(2026, 6, 15);
        var istanbulSaat01 = Zaman.YerelGunBasiUtc(yerelGun).AddHours(1);
        var istanbulSaat23 = Zaman.YerelGunBasiUtc(yerelGun).AddHours(23);

        // İkisi de AYNI yerel gün; UTC'de farklı günlerde görünürler.
        Assert.NotEqual(istanbulSaat01.Date, istanbulSaat23.Date);

        using var db = vt.Olustur(k.TenantId);
        NakitHareket(db, k, 20_000m, istanbulSaat01);

        // Aynı yerel günün sonunda 15.000 daha → toplam 35.000 ⛔
        await Assert.ThrowsAsync<BadRequestException>(
            () => Kapi.Dogrula(db, k.TenantId, k.CariId, 15_000m, istanbulSaat23));
    }

    [Fact]
    public async Task Nakit_olmayan_odeme_toplama_katilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik kart");
        var an = DateTime.UtcNow;

        using var db = vt.Olustur(k.TenantId);
        NakitHareket(db, k, 100_000m, an, odemeTuru: "KART");

        // Kart zaten tevsik edilmiştir; kümülatif nakde katılmaz.
        var sonuc = await Kapi.Dogrula(db, k.TenantId, k.CariId, 5_000m, an);
        Assert.False(sonuc.Gerekli);
        Assert.Equal(5_000m, sonuc.GunlukNakitToplam);
    }

    [Fact]
    public async Task Baska_kiracinin_nakiti_toplama_KATILMAZ()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik kiracı A");
        var an = DateTime.UtcNow;

        using (var dbA = vt.Olustur(a.TenantId)) NakitHareket(dbA, a, 29_000m, an);

        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik kiracı B");
        using var dbB = vt.Olustur(b.TenantId);

        var sonuc = await Kapi.Dogrula(dbB, b.TenantId, b.CariId, 29_000m, an);
        Assert.False(sonuc.Gerekli);
    }

    [Fact]
    public async Task Cari_bilinmiyorsa_yalniz_bu_islem_degerlendirilir()
    {
        // Kişi bilinmiyorsa kümülatif toplam hesaplanamaz — ama tek işlem
        // haddi aşıyorsa yine durdurulur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik carisiz");
        using var db = vt.Olustur(k.TenantId);

        Assert.False((await Kapi.Dogrula(db, k.TenantId, null, 30_000m, DateTime.UtcNow)).Gerekli);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Kapi.Dogrula(db, k.TenantId, null, 30_000.01m, DateTime.UtcNow));
    }

    [Fact]
    public async Task Kural_kapatilirsa_islem_durmaz_ama_karar_yine_hesaplanir()
    {
        // Kaçış kapısı bilinçlidir: kapatmak mevzuata aykırı davranmayı
        // seçmektir. Ama rapor/denetim doğru veriyi görmeye devam etmeli.
        var kapali = new TevsikKapisi(new TevsikKurali(new TevsikEsikleri { Etkin = false }));
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tevsik kapalı");

        using var db = vt.Olustur(k.TenantId);
        var sonuc = await kapali.Dogrula(db, k.TenantId, k.CariId, 50_000m, DateTime.UtcNow);

        Assert.True(sonuc.Gerekli);        // karar doğru
        Assert.NotNull(sonuc.Mesaj);       // gerekçe duruyor
        // ...ama istisna atılmadı.
    }
}
