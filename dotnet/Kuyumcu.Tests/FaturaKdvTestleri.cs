using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KALEM BAZLI KDV / ÖZEL MATRAH — paket 1 Faz A [saf, DB'siz]
//
// Bertan'ın kısmi vetosunun çözümü: has bedeli türetimi TEK saf sınıftadır
// (OzelMatrahMotoru). Paket 1 onu kalem bazına yayar, paket 38 hareket bazına.
// Bu dosya motorun kalem sözleşmesini sınar.
//
// ⚠ Sabitler ÖNCEDEN perl ile hesaplanıp rakam olarak yazılmıştır (Aşkın'ın
//   koşulu: "farklı çıkan bir örnek seçilir" kabul ölçütü değildir).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FaturaKdvTestleri
{
    private static readonly HasBacagi[] HasYok = [];

    [Fact]
    public void Standart_yuzde20_12000_TL_kalemde_matrah_10000_kdv_2000()
    {
        var s = OzelMatrahMotoru.Hesapla(HasYok, 12_000m, 0.20m);

        Assert.Equal(10_000.00m, s.Matrah);
        Assert.Equal(2_000.00m, s.KdvTutar);
        Assert.Equal(0m, s.HasDeger);
        Assert.Equal(10_000.00m, s.AraToplam);
    }

    /// <summary>
    /// 65.000 TL'lik kalem, has bedeli 61.542,93 → KDV yalnız 3.457,07 üzerinden.
    /// (perl: 3457,07 / 1,20 = 2880,89 matrah · 576,18 KDV)
    /// </summary>
    [Fact]
    public void Kuyum_kaleminde_KDV_yalnizca_has_disi_tutardan_hesaplanir()
    {
        // 9,0158 gram has × 6.825,00 TL = 61.532,84 değil — bacak doğrudan verilir:
        var s = OzelMatrahMotoru.Hesapla(
            [new HasBacagi(9m, 6_838.1033333333m)], 65_000m, 0.20m);

        Assert.Equal(61_542.93m, s.HasDeger);
        Assert.Equal(2_880.89m, s.Matrah);
        Assert.Equal(576.18m, s.KdvTutar);
        Assert.Equal(64_423.82m, s.AraToplam);
    }

    [Fact]
    public void Has_bedeli_satir_toplamina_esit_veya_buyukse_BadRequest()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            OzelMatrahMotoru.Hesapla([new HasBacagi(10m, 1_000m)], 10_000m, 0.20m));

        Assert.Contains("küçük olmalıdır", hata.Message);
    }

    /// <summary>
    /// ÜÇ KALEM AYRI AYRI YUVARLANIR: 3 × 10,01 TL kalemde satır başına KDV 1,67
    /// → toplam 5,01. Önce toplanıp (30,03) sonra yuvarlansaydı 5,00 çıkardı ve
    /// kalem toplamı belge toplamından 1 kuruş saparadı.
    /// </summary>
    [Fact]
    public void Uc_kalemin_KDV_si_AYRI_AYRI_yuvarlanir_ve_toplanir()
    {
        var kalemBasina = Enumerable.Range(0, 3)
            .Select(_ => OzelMatrahMotoru.Hesapla(HasYok, 10.01m, 0.20m).KdvTutar)
            .Sum();
        var oncedenToplanmis = OzelMatrahMotoru.Hesapla(HasYok, 30.03m, 0.20m).KdvTutar;

        Assert.Equal(5.01m, kalemBasina);
        Assert.Equal(5.00m, oncedenToplanmis);
        Assert.NotEqual(kalemBasina, oncedenToplanmis);   // örnek gerçekten ayırıyor
    }

    [Fact]
    public void Sifir_oranli_kalemde_KDV_sifir_matrah_satir_toplamidir()
    {
        var s = OzelMatrahMotoru.Hesapla(HasYok, 1_234.56m, 0m);

        Assert.Equal(0m, s.KdvTutar);
        Assert.Equal(1_234.56m, s.Matrah);
        Assert.Equal(1_234.56m, s.AraToplam);
    }

    [Fact]
    public void Kur_kaydi_yoksa_has_bedeli_SIFIR_SAYILMAZ_BadRequest()
    {
        // FAIL-CLOSED: 0 yazmak tüm tutarı matraha sokar ve fazla KDV ödetir.
        var hata = Assert.Throws<BadRequestException>(() =>
            OzelMatrahMotoru.Hesapla([new HasBacagi(9m, 0m)], 65_000m, 0.20m));

        Assert.Contains("kur kaydı", hata.Message);
    }

    [Fact]
    public void Sifir_veya_negatif_toplam_BadRequest()
    {
        Assert.Throws<BadRequestException>(() => OzelMatrahMotoru.Hesapla(HasYok, 0m, 0.20m));
        Assert.Throws<BadRequestException>(() => OzelMatrahMotoru.Hesapla(HasYok, -5m, 0.20m));
    }

    [Fact]
    public void Gecersiz_oran_BadRequest()
    {
        Assert.Throws<BadRequestException>(() => OzelMatrahMotoru.Hesapla(HasYok, 100m, -0.01m));
        Assert.Throws<BadRequestException>(() => OzelMatrahMotoru.Hesapla(HasYok, 100m, 1.5m));
    }

    /// <summary>Her bacak KENDİ kur fiyatıyla çarpılır; tek kur VARSAYILMAZ.</summary>
    [Fact]
    public void Iki_bacak_KENDI_kur_fiyatiyla_carpilir()
    {
        var s = OzelMatrahMotoru.Hesapla(
            [new HasBacagi(1m, 6_800m), new HasBacagi(1m, 6_900m)], 20_000m, 0.20m);

        Assert.Equal(13_700.00m, s.HasDeger);
        Assert.Equal(2m, s.HasGram);
    }

    [Fact]
    public void Has_bacagi_yoksa_UYARI_doner_ama_hesap_standarttir()
    {
        var s = OzelMatrahMotoru.Hesapla(HasYok, 120m, 0.20m);
        Assert.NotNull(s.Uyari);
        Assert.Equal(20.00m, s.KdvTutar);
    }
}
