using Kuyumcu.Api.Modules.Arama;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET KATMAN-1b sınır testleri.
// Paket şu gösterim yolunu değiştirdi: kayıt paletinde ürün alt bilgisi gram biçimi
// (AramaServisi.EkranGram: eskiden InvariantCulture "0.####", şimdi tr-TR "#,##0.###").
// Ürün gramı decimal(12,4) saklanır; ekran kuralı ürün listesindeki Bicim.Gram (F3) ile
// aynı sayısal değeri vermelidir (3 hane, yarım uzağa yuvarlama), yalnız sondaki sıfırlar atılır.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public class KATMAN1bSinirTestleri
{
    [Fact]
    public void Gram_sifir()
        => Assert.Equal("0 g", AramaServisi.EkranGram(0m));

    [Fact]
    public void Gram_eksi_isaret_korunur()
        => Assert.Equal("-4,1 g", AramaServisi.EkranGram(-4.1m));

    [Fact]
    public void Gram_tr_TR_ondalik_virgul_binlik_nokta()
    {
        Assert.Equal("4,1 g", AramaServisi.EkranGram(4.1m));
        Assert.Equal("1.250 g", AramaServisi.EkranGram(1250m));
        Assert.Equal("1.250,5 g", AramaServisi.EkranGram(1250.5m));
    }

    [Fact]
    public void Gram_dort_hane_ucuncu_haneye_yarim_uzaga_yuvarlanir_urun_listesiyle_ayni()
    {
        // 1,2345 → 1,235 (Bicim.Gram "1,235 g" ile aynı sayı); 1,2344 → 1,234.
        Assert.Equal("1,235 g", AramaServisi.EkranGram(1.2345m));
        Assert.Equal("1,234 g", AramaServisi.EkranGram(1.2344m));
        Assert.Equal("1,235 g", Bicim.Gram(1.2345m));
    }

    [Fact]
    public void Gram_0_0005_sinirinda_yukari_0_0004_asagi()
    {
        Assert.Equal("0,001 g", AramaServisi.EkranGram(0.0005m));
        Assert.Equal("0 g", AramaServisi.EkranGram(0.0004m));
    }

    [Fact]
    public void Gram_cok_buyuk_decimal_12_4_ust_siniri()
        => Assert.Equal("99.999.999,999 g", AramaServisi.EkranGram(99999999.999m));

    [Fact]
    public void Gram_InvariantCulture_nokta_ondaligi_SIZMAZ()
        => Assert.DoesNotContain("4.1", AramaServisi.EkranGram(4.1m), StringComparison.Ordinal);

    // ── Doğrulayıcı tur 30.09 02:30 (Aşkın + Mehmet) — ek sınırlar ─────────────────
    [Fact]
    public void Gram_cok_buyuk_999_999_999_99_tr_TR()
        => Assert.Equal("999.999.999,99 g", AramaServisi.EkranGram(999999999.99m));

    [Fact]
    public void Gram_0_0001_proje_kurali_3_hane_P57_sifira_iner_eksi_isaretsiz()
    {
        // Ekran gram kuralı 3 hanedir (Bicim.Gram F3 → "0,000 g"); 0,0001 g palette "0 g".
        Assert.Equal("0 g", AramaServisi.EkranGram(0.0001m));
        Assert.Equal("0,000 g", Bicim.Gram(0.0001m));
        // -0,0001 → "-0 g" gibi işaretli sıfır ÇIKMAMALI
        Assert.Equal("0 g", AramaServisi.EkranGram(-0.0001m));
    }

    [Fact]
    public void Telefon_bos_null_ve_bosluk_degismeden_doner()
    {
        Assert.Null(AramaServisi.EkranTelefon(null));
        Assert.Equal("", AramaServisi.EkranTelefon(""));
        Assert.Equal("   ", AramaServisi.EkranTelefon("   "));
    }

    [Theory]
    [InlineData("05320000104", "0532 000 01 04")]
    [InlineData("+90 532 000 01 04", "0532 000 01 04")]
    [InlineData("5320000104", "0532 000 01 04")]
    [InlineData("0532-000-01-04", "0532 000 01 04")]
    [InlineData("+49 30 1234567", "+49 30 1234567")]   // yurt dışı: tahminle biçimlenmez
    [InlineData("1234", "1234")]                        // dahili: değişmez
    public void Telefon_TR_kalibi_ve_kalip_disi(string ham, string beklenen)
        => Assert.Equal(beklenen, AramaServisi.EkranTelefon(ham));

    [Theory]
    [InlineData("45.000", 45000)]
    [InlineData("1,5", 1.5)]
    [InlineData("0,005", 0.005)]
    [InlineData("0,0001", 0.0001)]
    [InlineData("999.999.999,99", 999999999.99)]
    [InlineData("-4,1", -4.1)]
    public void SayiCoz_Turkce_giris_Tamam(string ham, double beklenen)
    {
        var s = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(ham);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal((decimal)beklenen, s.Deger);
    }

    [Fact]
    public void SayiCoz_belirsiz_1_500_REDDEDILIR_bos_BOS()
    {
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Belirsiz, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Bos, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Bos, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("  ").Sonuc);
    }
}
