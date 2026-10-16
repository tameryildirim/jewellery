using Bunit;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET BICIM-2 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metindir (üretim
// kodundan hesaplanmaz). BICIM-2 hesap yapmaz; para/gram/has GÖSTEREN girdi
// kutusu yolunu (Bicim.ParaGirdi/GramGirdi/HasGirdi/SayiGirdi + SayisalAlan
// odak dışı/odakta gösterimi) sınar. Tablo biçimi Bicim.Tl: AwayFromZero
// 2 hane; girdi biçimi YUVARLAMAZ (değeri saklamaz, gösterir).
// ════════════════════════════════════════════════════════════════════════
public sealed class BICIM2SinirTestleri : BunitContext
{
    public BICIM2SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<Para> Kutu(decimal? deger, Action<decimal?> geri) =>
        Render<Para>(p => p
            .Add(x => x.ElemanId, "d-para")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.Deger, deger)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, geri)));

    private static string V(IRenderedComponent<Para> c) => c.Find("#d-para").GetAttribute("value") ?? "";

    // ── sıfır ────────────────────────────────────────────────────────────
    [Fact]
    public void D_B2_sifir_para_gram_sayi_has()
    {
        Assert.Equal("0", Bicim.ParaGirdi(0m));
        Assert.Equal("0", Bicim.ParaGirdi(0.00m));
        Assert.Equal("0,000", Bicim.GramGirdi(0m));
        Assert.Equal("0,0000", Bicim.HasGirdi(0m));
        Assert.Equal("0", Bicim.SayiGirdi(0m));
        var c = Kutu(0m, _ => { });
        Assert.Equal("0", V(c));
    }

    // ── eksi: işaret korunur, geri okunur ─────────────────────────────────
    [Fact]
    public void D_B2_eksi_isaret_dogru_ve_geri_okunur()
    {
        Assert.Equal("-1.500,50", Bicim.ParaGirdi(-1500.5m));
        Assert.Equal(-1500.5m, FormAlani.SayiCoz("-1.500,50").Deger);
        Assert.Equal("-12,500", Bicim.GramGirdi(-12.5m));
        Assert.Equal("−1.500,50 ₺", Bicim.Tl(-1500.5m));   // tablo biçimi değişmedi
    }

    // ── kuruş yuvarlaması: tablo AwayFromZero, girdi yuvarlamaz ────────────
    [Fact]
    public void D_B2_kurus_0_005_siniri()
    {
        Assert.Equal("0,01 ₺", Bicim.Tl(0.005m));     // AwayFromZero
        Assert.Equal("0 ₺", Bicim.Tl(0.004m));
        Assert.Equal("1.000,01 ₺", Bicim.Tl(1000.005m));
        Assert.Equal("0,005", Bicim.ParaGirdi(0.005m));         // girdi değeri saklamaz
        Assert.Equal("1.000,005", Bicim.ParaGirdi(1000.005m));
        Assert.Equal(1000.005m, FormAlani.SayiCoz("1.000,005").Deger);
    }

    // ── has/gram 4 hane ───────────────────────────────────────────────────
    [Fact]
    public void D_B2_has_gram_dort_hane_0_0001()
    {
        Assert.Equal("0,0001", Bicim.HasGirdi(0.0001m));
        Assert.Equal("0,0001", Bicim.GramGirdi(0.0001m));       // 3 haneden fazlası kesilmez
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
    }

    // ── çok büyük tutar ───────────────────────────────────────────────────
    [Fact]
    public void D_B2_cok_buyuk_tutar_gidis_donus()
    {
        Assert.Equal("999.999.999,99", Bicim.ParaGirdi(999999999.99m));
        Assert.Equal(999999999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        decimal? gelen = null;
        var c = Kutu(null, v => gelen = v);
        c.Find("#d-para").Focus();
        c.Find("#d-para").Input("999999999,99");
        c.Find("#d-para").Blur();
        Assert.Equal("999.999.999,99", V(c));
        Assert.Equal(999999999.99m, gelen);
        c.Find("#d-para").Focus();
        Assert.Equal("999999999,99", V(c));
    }

    // ── boş / null ────────────────────────────────────────────────────────
    [Fact]
    public void D_B2_bos_null_kutu_bos_deger_null_hata_yok()
    {
        decimal? gelen = 5m;
        var c = Kutu(5m, v => gelen = v);
        Assert.Equal("5", V(c));
        c.Find("#d-para").Focus();
        c.Find("#d-para").Input("");
        c.Find("#d-para").Blur();
        Assert.Null(gelen);
        Assert.Equal("", V(c));
        Assert.Empty(c.FindAll("#d-para-hata"));
        var n = Kutu(null, _ => { });
        Assert.Equal("", n.Find("#d-para").GetAttribute("value") ?? "");
    }

    // ── Türkçe giriş: '45.000' kabul, '1,5' kabul, '1.500' belirsiz ────────
    [Fact]
    public void D_B2_turkce_giris_45_000_ve_1_5_kabul()
    {
        decimal? gelen = null;
        var c = Kutu(null, v => gelen = v);
        c.Find("#d-para").Focus();
        c.Find("#d-para").Input("45.000");
        Assert.Equal(45000m, gelen);
        c.Find("#d-para").Blur();
        Assert.Equal("45.000", V(c));
        c.Find("#d-para").Focus();
        Assert.Equal("45000", V(c));                            // blur SayiCoz ile normalleştirdi
        c.Find("#d-para").Input("1,5");
        c.Find("#d-para").Blur();
        Assert.Equal(1.5m, gelen);
        Assert.Equal("1,50", V(c));
    }

    [Fact]
    public void D_B2_belirsiz_1_500_reddedilir_deger_degismez()
    {
        decimal? gelen = 9m;
        var c = Kutu(9m, v => gelen = v);
        c.Find("#d-para").Focus();
        c.Find("#d-para").Input("1.500");
        c.Find("#d-para").Blur();
        Assert.Equal(9m, gelen);
        Assert.Equal("1.500", V(c));
        Assert.Equal("true", c.Find("#d-para").GetAttribute("aria-invalid"));
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }

    // ── odak dışı biçimli metin, 1.500 dışında SayiCoz ile birebir geri okunur ──
    [Theory]
    [InlineData("50000")]
    [InlineData("63363.16")]
    [InlineData("1000")]
    [InlineData("1234567.891")]
    [InlineData("0.5")]
    public void D_B2_odak_disi_para_metni_geri_okunur(string ham)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(d, FormAlani.SayiCoz(Bicim.ParaGirdi(d)).Deger);
    }
}
