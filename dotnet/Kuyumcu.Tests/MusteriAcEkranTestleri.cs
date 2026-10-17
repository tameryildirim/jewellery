using Bunit;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// M6b — tezgâh müşteri seçicisinde TC/VKN: kayıtlı numara kendiliğinden seçilir; yoksa ad yazılıp açılır; geçersiz TC uyarılır.
public sealed class MusteriAcEkranTestleri : BunitContext
{
    private const string Tc = "10000000146";     // algoritmaya uygun
    private const string YeniTc = "12345678950"; // algoritmaya uygun, listede yok

    public MusteriAcEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private (IRenderedComponent<YildizMusteriSecici> E, List<string> Secilen, List<(string No, string Ad)> Acilan) Ciz()
    {
        var secilen = new List<string>();
        var acilan = new List<(string No, string Ad)>();
        var b = new YildizTezgahBaglami
        {
            MusteriSecenekleri = [new("p1", "Peşin Müşteri", null), new("c1", "Selin Ergüven", null, Tc), new("c2", "Tuna Akarsu", null, "1234567890")],
            MusteriSeciliId = "p1",
            MusteriSec = EventCallback.Factory.Create<string>(this, id => secilen.Add(id)),
            MusteriAc = EventCallback.Factory.Create<(string No, string Ad)>(this, g => acilan.Add(g)),
        };
        return (Render<YildizMusteriSecici>(p => p.Add(x => x.B, b)), secilen, acilan);
    }

    [Fact]
    public void M6bE_01_kayitli_TC_yazilinca_musteri_kendiliginden_secilir()
    {
        var (e, secilen, _) = Ciz();
        e.Find("#yildiz-musteri-ara").Input(Tc);
        Assert.Equal(["c1"], secilen);
        Assert.Empty(e.FindAll("#yildiz-musteri-yeni"));
        Assert.NotNull(e.Find("#yildiz-musteri-sat-c1"));
    }

    [Fact]
    public void M6bE_02_kayitli_VKN_de_bulunur()
    {
        var (e, secilen, _) = Ciz();
        e.Find("#yildiz-musteri-ara").Input("1234567890");
        Assert.Equal(["c2"], secilen);
    }

    [Fact]
    public void M6bE_03_yeni_TC_ad_yazilip_acilir()
    {
        var (e, secilen, acilan) = Ciz();
        e.Find("#yildiz-musteri-ara").Input(YeniTc);
        Assert.Empty(secilen);
        Assert.NotNull(e.Find("#yildiz-musteri-yeni"));
        Assert.True(e.Find("#yildiz-musteri-yeni-ac").HasAttribute("disabled"));   // ad yokken kilitli
        Assert.Empty(e.FindAll("#yildiz-musteri-yok"));                          // "bulunamadı" ikinci kez yazılmaz
        e.Find("#yildiz-musteri-yeni-ad").Input("  Ayşe Yılmaz ");
        e.Find("#yildiz-musteri-yeni-ac").Click();
        Assert.Equal([(YeniTc, "Ayşe Yılmaz")], acilan);
    }

    [Fact]
    public void M6bE_04_gecersiz_TC_uyarilir_acma_yolu_yok()
    {
        var (e, secilen, acilan) = Ciz();
        e.Find("#yildiz-musteri-ara").Input("11111111111");
        Assert.NotNull(e.Find("#yildiz-musteri-kimlik-hata"));
        Assert.Empty(e.FindAll("#yildiz-musteri-yeni"));
        Assert.Empty(secilen); Assert.Empty(acilan);
    }

    [Fact]
    public void M6bE_05_adla_arama_eskisi_gibi_TC_kutusu_acilmaz()
    {
        var (e, secilen, _) = Ciz();
        e.Find("#yildiz-musteri-ara").Input("Selin");
        Assert.NotNull(e.Find("#yildiz-musteri-sat-c1"));
        Assert.Empty(e.FindAll("#yildiz-musteri-yeni, #yildiz-musteri-kimlik-hata"));
        Assert.Empty(secilen);
    }
}
