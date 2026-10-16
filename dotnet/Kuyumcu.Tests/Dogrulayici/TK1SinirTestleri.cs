using System.Globalization;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR (üretim
// kodundan hesaplanmaz). T-K1 ekran TL biçimini ("57.550 ₺", sıfır kuruş
// yazılmaz, eksi U+2212, NBSP), kâğıt biçimini (kuruş hep 2 hane) ve kur
// şeridini (has/gram tam lira) değiştirdi. Sınırlar:
//   sıfır · eksi · 0,005 yuvarlama (AwayFromZero) · has 4 hane · çok büyük
//   tutar · boş/null para birimi · tarayıcı kültüründen bağımsızlık ·
//   Türkçe giriş (FormAlani.SayiCoz) ve Girdi gidiş-dönüşü.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TK1SinirTestleri
{
    private const string B = " ₺";   // NBSP + ₺
    private const string E = "−";         // eksi işareti

    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    // ── Ekran (Tl) ───────────────────────────────────────────────────────────
    [Theory]
    [InlineData("0", "0" + B)]
    [InlineData("0.00", "0" + B)]
    [InlineData("0.004", "0" + B)]
    [InlineData("0.005", "0,01" + B)]
    [InlineData("2.675", "2,68" + B)]
    [InlineData("-0.004", "0" + B)]
    [InlineData("-0.005", E + "0,01" + B)]
    [InlineData("-1", E + "1" + B)]
    [InlineData("999999999.99", "999.999.999,99" + B)]
    [InlineData("999999999.995", "1.000.000.000" + B)]
    [InlineData("45000.10", "45.000,10" + B)]
    [InlineData("0.999", "1" + B)]
    public void DT_Tl_sinirlari(string girdi, string beklenen) =>
        Assert.Equal(beklenen, Bicim.Tl(D(girdi)));

    // ── Kâğıt (TlKurus / BelgePara) ─────────────────────────────────────────
    [Theory]
    [InlineData("0", "0,00" + B)]
    [InlineData("-0.004", "0,00" + B)]
    [InlineData("0.005", "0,01" + B)]
    [InlineData("999999999.99", "999.999.999,99" + B)]
    [InlineData("-45000", E + "45.000,00" + B)]
    public void DT_BelgePara_TRY_ve_bos_birim_sinirlari(string girdi, string beklenen)
    {
        Assert.Equal(beklenen, Bicim.TlKurus(D(girdi)));
        Assert.Equal(beklenen, Bicim.BelgePara(D(girdi), null));
        Assert.Equal(beklenen, Bicim.BelgePara(D(girdi), ""));
        Assert.Equal(beklenen, Bicim.BelgePara(D(girdi), "TRY"));
    }

    // ── Kur şeridi (TlTam) ───────────────────────────────────────────────────
    [Theory]
    [InlineData("0", "0" + B)]
    [InlineData("0.5", "1" + B)]
    [InlineData("-0.5", E + "1" + B)]
    [InlineData("0.49", "0" + B)]
    [InlineData("6876.5", "6.877" + B)]
    public void DT_TlTam_sinirlari(string girdi, string beklenen) =>
        Assert.Equal(beklenen, Bicim.TlTam(D(girdi)));

    // ── Null / boş para birimi (ekran) ───────────────────────────────────────
    [Fact]
    public void DT_Para_bos_ve_null_birim_TL_ekran_bicimi()
    {
        Assert.Equal("0" + B, Bicim.Para(0m, null));
        Assert.Equal("0" + B, Bicim.Para(0m, ""));
        Assert.Equal("12,50" + B, Bicim.Para(12.5m, "TRY"));
    }

    // ── Has 4 hane ───────────────────────────────────────────────────────────
    [Fact]
    public void DT_Has_dort_hane_ve_gram_uc_hane()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("0,0000 has", Bicim.Has(0m));
        Assert.Equal("13,9232 has", Bicim.Has(13.9232m));
        Assert.Equal("0,001 g", Bicim.Gram(0.001m));
    }

    // ── Tarayıcı kültüründen bağımsızlık (24.09 kusuru: "TRY6,754.91") ─────────
    [Fact]
    public void DT_Tl_en_US_kulturunde_de_tr_TR_basar_TRY_yok()
    {
        var onceki = CultureInfo.CurrentCulture;
        var oncekiUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("6.754,91" + B, Bicim.Tl(6754.91m));
            Assert.Equal("6.755" + B, Bicim.TlTam(6754.91m));
            Assert.Equal("6.754,91" + B, Bicim.TlKurus(6754.91m));
            Assert.DoesNotContain("TRY", Bicim.Tl(6754.91m));
        }
        finally
        {
            CultureInfo.CurrentCulture = onceki;
            CultureInfo.CurrentUICulture = oncekiUi;
        }
    }

    // ── Satır kırılmaz: tutar içinde düz boşluk yok ─────────────────────────
    [Fact]
    public void DT_Tl_duz_bosluk_icermez_NBSP_kullanir()
    {
        var s = Bicim.Tl(1234567.5m);
        Assert.Equal("1.234.567,50" + B, s);
        Assert.DoesNotContain(" ", s);
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) + Girdi gidiş-dönüşü ─────────────────
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,005", "0.005")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("-22.000", "-22000")]
    public void DT_SayiCoz_turkce_girdi(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(D(beklenen), c.Deger);
    }

    [Fact]
    public void DT_SayiCoz_belirsiz_ve_bos()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("   ").Sonuc);
    }

    [Fact]
    public void DT_Girdi_gidis_donus_ekran_bicimi_girdiye_sizmaz()
    {
        var g = Bicim.Girdi(45000.5m, "0.00");
        Assert.Equal("45000,50", g);
        Assert.Equal(45000.50m, FormAlani.SayiCoz(g).Deger);
        // Ekran biçimi ("45.000,50 ₺") girdi kutusuna konmaz: ₺ ile çözülemez.
        Assert.Equal(FormAlani.SayiSonucu.Gecersiz, FormAlani.SayiCoz(Bicim.Tl(45000.5m)).Sonuc);
    }
}
