using Bunit;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 03.11 KURUCU — "Fatura kesilsin mi? Evet diyorum, satış tamamlandı vs uyarısı gelmiyor, sanki kaydetmemiş gibi;
// tam ekran loading + giriş/sol üstteki gibi amblem animasyonu". Aynı satışı iki kez yapmıştı (FTR-2026-000010 + 000011).
//   • Kayıt boyunca tam ekran perde (role=status, aria-busy, amblem, "Kaydediliyor…"), düğme YOK.
//   • Fişli satış bitince aynı perde "Satış tamamlandı" (role=dialog) + fiş satırı + yazdırma bağlantıları + TEK birincil
//     düğme "Yeni satış"; düğme ve Escape perdeyi kapatır.
//   • İkisi de yokken perde çizilmez. Faturalı satışın fatura kâğıdına geçişi FaturaSorusuEkranTestleri.FS_06'da.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizTezgahPerdeTestleri : BunitContext
{
    private sealed class BosApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });
    }

    public YildizTezgahPerdeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new BosApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private YildizTezgahBaglami Baglam(bool kaydediliyor = false, string? tamamBaslik = null, Action? kapat = null) => new()
    {
        TurAdi = "Satış", MusteriAdi = "Selin Ergüven", MusteriDurumu = "Borç yok",
        Kalemler = [], ToplamAyrinti = "0,00 g · 0,0000 g has", ToplamMetni = "0,00 ₺",
        Odemeler = [new(0, "Nakit", "Ana Kasa", "0,00 ₺")], FarkMetni = "0 ₺", FarkTamam = true,
        KaydetEtiketi = "Kaydet ve yazdır", KaydetKilitli = true, KaydetKilitSebebi = "Önce ürün seçin",
        Kaydediliyor = kaydediliyor,
        SonFis = "Son fiş: SF-2026-000012 · Fatura kesilmedi",
        YazdirAdresleri = ["/cikti/SATIS_FISI/h1?yazdir=1"],
        UrunBelgesiAdresi = "/cikti/GARANTI_KARTI/h1",
        TamamBaslik = tamamBaslik,
        TamamKapatEtiketi = "Yeni satış",
        TamamKapat = EventCallback.Factory.Create(this, () => kapat?.Invoke()),
    };

    [Fact]
    public void P01_kayit_sirasinda_tam_ekran_perde_amblem_ve_Kaydediliyor_dugme_yok()
    {
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, Baglam(kaydediliyor: true)));
        var perde = e.Find("#yildiz-tezgah-perde");
        Assert.Equal("status", perde.GetAttribute("role"));
        Assert.Equal("true", perde.GetAttribute("aria-busy"));
        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-perde .amb-parla .tas3d"));          // giriş amblemi (3B taş)
        Assert.Equal("Kaydediliyor…", e.Find("#yildiz-tezgah-perde-baslik").TextContent.Trim());
        Assert.Empty(e.FindAll("#yildiz-tezgah-perde button, #yildiz-tezgah-perde a"));   // bekleme ekranında eylem yok
    }

    [Fact]
    public void P02_kayit_bitince_Satis_tamamlandi_paneli_fis_satiri_yazdirma_ve_tek_birincil_Yeni_satis()
    {
        var kapandi = 0;
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, Baglam(tamamBaslik: "Satış tamamlandı", kapat: () => kapandi++)));
        var perde = e.Find("#yildiz-tezgah-perde");
        Assert.Equal("dialog", perde.GetAttribute("role"));
        Assert.Equal("true", perde.GetAttribute("aria-modal"));
        Assert.Equal("Satış tamamlandı", e.Find("#yildiz-tezgah-perde-baslik").TextContent.Trim());
        Assert.Equal("Son fiş: SF-2026-000012 · Fatura kesilmedi", e.Find("#yildiz-tezgah-perde-satir").TextContent.Trim());
        Assert.Equal("/cikti/SATIS_FISI/h1?yazdir=1", e.Find("#yildiz-tezgah-perde-yazdir").GetAttribute("href"));
        Assert.Equal("/cikti/GARANTI_KARTI/h1", e.Find("#yildiz-tezgah-perde-urun-belgesi").GetAttribute("href"));
        Assert.Single(e.FindAll("#yildiz-tezgah-perde .yz-b1"));                          // tek birincil
        var kapat = e.Find("#yildiz-tezgah-perde-kapat");
        Assert.Equal("Yeni satış", kapat.TextContent.Trim());
        // odak tek birincil düğmede (sayfanın odak yardımcısı; kayıt sonrası barkod odağı perdeyi geçmesin)
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "kuyumcuOdak.alana" && Equals(i.Arguments[0], "yildiz-tezgah-perde-kapat"));
        kapat.Click();
        Assert.Equal(1, kapandi);
        perde.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, kapandi);
    }

    [Fact]
    public void P03_kayit_yokken_perde_cizilmez_ve_Kaydediliyorken_Escape_kapatmaz()
    {
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, Baglam()));
        Assert.Empty(e.FindAll("#yildiz-tezgah-perde"));
        var kapandi = 0;
        var k = Render<YildizTezgah>(p => p.Add(x => x.B, Baglam(kaydediliyor: true, kapat: () => kapandi++)));
        k.Find("#yildiz-tezgah-perde").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(0, kapandi);
    }
}
