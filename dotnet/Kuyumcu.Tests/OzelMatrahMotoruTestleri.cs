using Kuyumcu.Api.Common;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÖZEL MATRAH MOTORU — paket 38 [saf]  (DB/ağ/saat GÖRMEZ)
//
// TEK MOTOR (ADR-0016): has bedelini hesaplayan sınıf projede tektir. Paket 1
// onu kalem bazına, paket 38 hareket bazına uygular. Bu dosya motorun KENDİ
// sözleşmesini sınar; sabitler elle (bağımsız) hesaplanmıştır.
//
// ⚠ Sabitler kültürden bağımsızdır (decimal literal), GrafikKulturTestleri
//   tuzağına düşmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class OzelMatrahMotoruTestleri
{
    private static MFatura.HasBacagi B(decimal gram, decimal fiyat) => new(gram, fiyat);

    // ── 1. Tek bacak: elle hesaplanmış sabitler ──────────────────────────────

    [Fact]
    public void Tek_bacak_has_bedeli_matrah_disi_kalir_ve_KDV_yalniz_iscilige_uygulanir()
    {
        // 10 g × 6.800 = 68.000 TL has bedeli; toplam 75.000 → matraha giren 7.000
        // (KDV dahil). 7.000 / 1,20 = 5.833,33 → KDV 1.166,67; ara toplam 73.833,33.
        var s = MFatura.OzelMatrahMotoru.Hesapla([B(10m, 6_800m)], 75_000m, 0.20m);

        Assert.Equal(10.000000m, s.HasGram);
        Assert.Equal(68_000.00m, s.HasDeger);
        Assert.Equal(5_833.33m, s.Matrah);
        Assert.Equal(1_166.67m, s.KdvTutar);
        Assert.Equal(73_833.33m, s.AraToplam);
        Assert.Null(s.Uyari);
    }

    // ── 2. İKİ BACAK, İKİ FARKLI KUR (tek kur VARSAYILMAZ) ───────────────────

    [Fact]
    public void Iki_bacak_FARKLI_kurla_carpilir_ortalama_kur_KULLANILMAZ()
    {
        // 5 × 6.800 = 34.000 ; 5 × 7.000 = 35.000 → 69.000.
        // Ortalama kurla (6.900 × 10) da 69.000 çıkardı; ayrıştırmak için
        // gramlar FARKLI seçilir: 4 × 6.800 = 27.200 ; 6 × 7.000 = 42.000 → 69.200.
        // Ortalama kur (6.900 × 10 = 69.000) kullanılsaydı 200 TL saparadı.
        var s = MFatura.OzelMatrahMotoru.Hesapla(
            [B(4m, 6_800m), B(6m, 7_000m)], 100_000m, 0.20m);

        Assert.Equal(69_200.00m, s.HasDeger);
        Assert.NotEqual(69_000.00m, s.HasDeger);
        Assert.Equal(10.000000m, s.HasGram);
    }

    // ── 3. Has bedeli toplamı AŞIYOR → hesap YAPILMAZ ────────────────────────

    /// <summary>
    /// ⚠ SAPMA (raporda yazılı): paket 38 spesifikasyonu "sonuç Uyari taşır"
    /// diyordu; motor paket 1'de FAIL-CLOSED olarak (BadRequestException)
    /// doğduğu ve paket 1'in testleri bu sözleşmeye bağlı olduğu için davranış
    /// DEĞİŞTİRİLMEDİ. Ortak olan şart korunur: HESAP YAPILMAZ.
    /// </summary>
    [Fact]
    public void Has_bedeli_toplami_asinca_hesap_YAPILMAZ()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            MFatura.OzelMatrahMotoru.Hesapla([B(10m, 6_800m)], 68_000m, 0.20m));

        Assert.Contains("küçük olmalıdır", hata.Message);
    }

    // ── 4. Kur kaydı yok / sıfır → FAIL-CLOSED ───────────────────────────────

    [Fact]
    public void Has_fiyati_sifir_olan_bacak_400_verir_sifir_TL_VARSAYILMAZ()
    {
        // 0 kabul edilseydi has bedeli 0 çıkar, TÜM bedel matraha girer ve
        // mükellefe FAZLA KDV ödetirdi — sessiz, mükellef aleyhine hata.
        var hata = Assert.Throws<BadRequestException>(() =>
            MFatura.OzelMatrahMotoru.Hesapla([B(10m, 0m)], 75_000m, 0.20m));

        Assert.Contains("Has altın fiyatı bulunamadı", hata.Message);
    }

    // ── 5. Bacak yoksa: standart hesabın ta kendisi (iki formül yazılmaz) ────

    [Fact]
    public void Bacak_yoksa_sonuc_STANDART_hesabin_aynisidir_ve_uyari_tasir()
    {
        var s = MFatura.OzelMatrahMotoru.Hesapla([], 1_200m, 0.20m);

        Assert.Equal(0m, s.HasDeger);
        Assert.Equal(200.00m, s.KdvTutar);          // 1200 − 1200/1,2
        Assert.Equal(1_000.00m, s.Matrah);
        Assert.NotNull(s.Uyari);
    }

    // ── 6. Yuvarlama: AwayFromZero, bacak bacak ──────────────────────────────

    [Fact]
    public void Yuvarlama_AwayFromZero_ve_bacak_bacak_yapilir()
    {
        // 1 g × 0,005 = 0,005 → 0,01 (AwayFromZero; ToEven olsaydı 0,00 olurdu).
        var s = MFatura.OzelMatrahMotoru.Hesapla([B(1m, 0.005m)], 100m, 0.20m);
        Assert.Equal(0.01m, s.HasDeger);

        // Üç bacak ayrı ayrı yuvarlanır: 3 × 0,005 → 3 × 0,01 = 0,03.
        // Önce toplanıp (0,015) sonra yuvarlansaydı 0,02 çıkardı.
        var c = MFatura.OzelMatrahMotoru.Hesapla(
            [B(1m, 0.005m), B(1m, 0.005m), B(1m, 0.005m)], 100m, 0.20m);
        Assert.Equal(0.03m, c.HasDeger);
    }

    // ── 7. Negatif has gramı motora GİRMEZ (işaret çağıranda temizlenir) ─────

    [Fact]
    public void Negatif_has_grami_reddedilir()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            MFatura.OzelMatrahMotoru.Hesapla([B(-10m, 6_800m)], 75_000m, 0.20m));

        Assert.Contains("negatif", hata.Message);
    }

    // ── 8. HasBedeli, Hesapla ile AYNI rakamı verir (tek motor kanıtı) ───────

    [Fact]
    public void HasBedeli_ve_Hesapla_ayni_has_bedelini_verir()
    {
        MFatura.HasBacagi[] bacaklar = [B(4m, 6_800m), B(6m, 7_000m)];

        var (gram, deger) = MFatura.OzelMatrahMotoru.HasBedeli(bacaklar);
        var s = MFatura.OzelMatrahMotoru.Hesapla(bacaklar, 100_000m, 0.20m);

        Assert.Equal(s.HasGram, gram);
        Assert.Equal(s.HasDeger, deger);
    }

    // ── 9. KdvAyarlari varsayılanı: oran 0.20, KOD BOŞ ───────────────────────

    /// <summary>
    /// Mehmet K38-1: doğrulanmamış '805' kodu koda GİRMEZ. Varsayılan boştur;
    /// kod ancak yapılandırmadan açılır.
    /// </summary>
    [Fact]
    // ⚠ PAKET P-26 (Y-40) İLE DEĞİŞTİ: varsayılan kod artık BOŞ DEĞİL 805'tir
    //   (ekip kararı K-10, 21.09.2026 — KDVK md.23/e + GİB özel matrah kod
    //   listesi 801-812). Testin ADI ve İDDİASI bilerek güncellendi; eski
    //   iddia ("BOŞ") artık ürünün davranışı değildir. Kodu KAPATMA yolu
    //   (yapılandırmada boş dizge) hâlâ sınanıyor — kilitlenmedik.
    public void KdvAyarlari_varsayilani_oran_020_ve_ozel_matrah_kodu_KAPALI()
    {
        var a = new MFatura.KdvAyarlari();

        Assert.Equal(0.20m, a.VarsayilanOran);
        // P-26 hasım bulgusu: K-10 teyidi gelmeden kod BOŞ doğar (PLAN:578/778).
        Assert.Null(a.OzelMatrahKodu);
        Assert.Null(a.KodYaDaNull);
        Assert.Null(new MFatura.KdvAyarlari { OzelMatrahKodu = "   " }.KodYaDaNull);
        Assert.Equal("805", new MFatura.KdvAyarlari { OzelMatrahKodu = " 805 " }.KodYaDaNull);
    }
}
