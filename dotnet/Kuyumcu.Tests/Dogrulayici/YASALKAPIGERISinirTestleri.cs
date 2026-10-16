using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Tests;

// ════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
// Uygulayıcıdan BAĞIMSIZ; beklenen değerler SABİT sayıdır (üretim kodundan hesaplanmaz).
// Kural (YasalKapiIzi): sunucu reddinin izi, reddin verildiği ölçünün (mutlak tutar) ALTINA inince
// ya da müşteri değişince geçersiz; ölçüde/üstünde ve aynı müşteride geçerli.
// ════════════════════════════════════════════════════════════════════════
public sealed class YASALKAPIGERISinirTestleri
{
    [Fact]
    public void YKGS_sifir_olcu_iz_sifirda_gecerli_negatif_mutlak()
    {
        var iz = YasalKapiIzi.Al(0m, "c1");
        Assert.Equal(0m, iz.Olcu);
        Assert.True(iz.Gecerli(0m, "c1"));
        Assert.True(iz.Gecerli(0.01m, "c1"));
    }

    [Fact]
    public void YKGS_eksi_tutar_mutlak_olculur_isaret_karar_degistirmez()
    {
        var iz = YasalKapiIzi.Al(-200_000m, "c1");
        Assert.Equal(200_000m, iz.Olcu);
        Assert.True(iz.Gecerli(-200_000m, "c1"));
        Assert.True(iz.Gecerli(200_000m, "c1"));
        Assert.False(iz.Gecerli(-199_999.99m, "c1"));
    }

    [Theory]
    [InlineData("199999.99", false)]
    [InlineData("200000.00", true)]
    [InlineData("200000.01", true)]
    [InlineData("369999.99", true)]
    [InlineData("370000", true)]
    public void YKGS_kurus_siniri_200000_reddinde(string tutar, bool gecerli)
    {
        var iz = YasalKapiIzi.Al(200_000.00m, "c1");
        Assert.Equal(gecerli, iz.Gecerli(decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture), "c1"));
    }

    [Fact]
    public void YKGS_kurus_yuvarlama_AwayFromZero_0_005_siniri()
    {
        var iz = YasalKapiIzi.Al(370_000.00m, null);
        // projenin kuralı: Math.Round(x, 2, MidpointRounding.AwayFromZero)
        Assert.True(iz.Gecerli(Math.Round(369_999.995m, 2, MidpointRounding.AwayFromZero), null));    // 370.000,00
        Assert.False(iz.Gecerli(Math.Round(369_999.994m, 2, MidpointRounding.AwayFromZero), null));   // 369.999,99
    }

    [Fact]
    public void YKGS_dort_hane_0_0001_farki_ayirt_edilir()
    {
        var iz = YasalKapiIzi.Al(30_000.0001m, "c1");
        Assert.False(iz.Gecerli(30_000.0000m, "c1"));
        Assert.True(iz.Gecerli(30_000.0001m, "c1"));
    }

    [Fact]
    public void YKGS_cok_buyuk_tutar()
    {
        var iz = YasalKapiIzi.Al(999_999_999.99m, "c1");
        Assert.True(iz.Gecerli(999_999_999.99m, "c1"));
        Assert.False(iz.Gecerli(999_999_999.98m, "c1"));
    }

    [Fact]
    public void YKGS_bos_null_musteri_ayni_deger_iz_yoksa_gecersiz()
    {
        var iz = YasalKapiIzi.Al(200_000m, null);
        Assert.True(iz.Gecerli(200_000m, ""));
        Assert.True(iz.Gecerli(200_000m, null));
        Assert.False(iz.Gecerli(200_000m, "c1"));
        Assert.False(YasalKapiIzi.Gecerli(null, 999_999_999.99m, null));
    }

    [Fact]
    public void YKGS_yukari_asagi_yukari_dizisi_iz_silinmez()
    {
        var iz = YasalKapiIzi.Al(230_000m, "c1");
        Assert.True(iz.Gecerli(230_000m, "c1"));
        Assert.False(iz.Gecerli(199_999.99m, "c1"));
        Assert.True(iz.Gecerli(230_000m, "c1"));
        Assert.False(iz.Gecerli(230_000m, "c2"));
        Assert.True(iz.Gecerli(230_000m, "c1"));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("199.999,99", "199999.99")]
    public void YKGS_turkce_giris_SayiCoz(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact]
    public void YKGS_belirsiz_1_500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── Doğrulayıcı tur 4 ekleri (10.10): üç eşik × iki yön, kuruş yuvarlaması, çoklu kalem silme dizisi ──

    [Theory]
    [InlineData("30000.00", "29999.99", false)]   // nakit sınırı reddi: 1 kuruş aşağı → düşer
    [InlineData("30000.00", "30000.00", true)]
    [InlineData("200000.00", "199999.99", false)] // amaç beyanı reddi
    [InlineData("200000.00", "200000.01", true)]
    [InlineData("370000.00", "369999.99", false)] // kimlik reddi
    [InlineData("370000.00", "370000.00", true)]
    public void YKGS4_uc_esik_red_izi_kurus_alti_dusar_esitte_kalir(string red, string simdi, bool gecerli)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var iz = YasalKapiIzi.Al(decimal.Parse(red, inv), "c1");
        Assert.Equal(gecerli, iz.Gecerli(decimal.Parse(simdi, inv), "c1"));
    }

    [Fact]
    public void YKGS4_yuvarlama_AwayFromZero_199999_995_esitte_kalir_994_duser()
    {
        var iz = YasalKapiIzi.Al(200_000.00m, null);
        Assert.True(iz.Gecerli(Math.Round(199_999.995m, 2, MidpointRounding.AwayFromZero), null));
        Assert.False(iz.Gecerli(Math.Round(199_999.994m, 2, MidpointRounding.AwayFromZero), null));
    }

    [Fact]
    public void YKGS4_coklu_kalem_silme_ve_iskonto_dizisi_her_adimda_yeniden_degerlendirilir()
    {
        // 3 kalem 150.000 + 60.000 + 20.000 = 230.000 → red; sil 60.000 → 170.000; sil 20.000 → 150.000;
        // geri ekle 60.000 → 210.000 (red ölçüsü 230.000 altında: hâlâ düşük); %10 iskonto öncesi 255.555,56 → 230.000,00
        var iz = YasalKapiIzi.Al(230_000m, "c1");
        Assert.False(iz.Gecerli(170_000m, "c1"));
        Assert.False(iz.Gecerli(150_000m, "c1"));
        Assert.False(iz.Gecerli(210_000m, "c1"));
        Assert.True(iz.Gecerli(Math.Round(255_555.56m * 0.9m, 2, MidpointRounding.AwayFromZero), "c1"));   // 230.000,004 → 230.000,00
        Assert.True(iz.Gecerli(Math.Round(255_555.55m * 0.9m, 2, MidpointRounding.AwayFromZero), "c1"));   // 229.999,995 → 230.000,00 (AwayFromZero)
        Assert.False(iz.Gecerli(Math.Round(255_555.54m * 0.9m, 2, MidpointRounding.AwayFromZero), "c1"));  // 229.999,986 → 229.999,99
    }
}
