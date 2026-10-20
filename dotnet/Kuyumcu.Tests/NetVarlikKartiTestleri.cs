using System.Net;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// PAKET-37 (20.10) — altın pozisyonu kartında "Bugünkü servet" bölümü: sunucu sayıları yazılır, eksik girdi sebebi görünür,
// sade yüzde (ayrıntı kapalı) çizilmez.
public sealed class NetVarlikKartiTestleri : BunitContext
{
    private const string Yanit = """
    {"anaMaden":"ALTIN",
     "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":36.4,"cariBorcGram":22.0,
              "netGram":312.8,"alacakliCariSayisi":3,"borcluOlduguCariSayisi":1,"konsinyeGram":0,"emanetGram":0},
     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
     "hesaplanmaAni":"2026-10-20T09:00:00Z","donemdenBagimsiz":true,
     "netVarlik":{"paraBirimleri":[{"paraBirimi":"TRY","kasa":250000,"banka":50000,"toplam":300000,"kur":1,"tlKarsiligi":300000},
                                   {"paraBirimi":"USD","kasa":1000,"banka":200,"toplam":1200,"kur":35,"tlKarsiligi":42000},
                                   {"paraBirimi":"GBP","kasa":10,"banka":0,"toplam":10,"kur":null,"tlKarsiligi":null}],
                  "nakitTl":null,"altinNetHasGram":312.8,"altinTl":2135548.16,"cariTl":1500,"netVarlikTl":null,"hasFiyati":6827.2,
                  "eksikler":["KUR_GBP"]}}
    """;

    public NetVarlikKartiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Yanit, System.Text.Encoding.UTF8, "application/json") });
    }

    [Fact]
    public void Servet_bolumu_sunucu_sayilarini_yazar_eksik_kuru_soyler_toplami_uydurmaz()
    {
        var c = Render<AltinPozisyonuKarti>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-servet")));
        Assert.Contains("2.135.548,16", c.Find("#pozisyon-servet-altin").TextContent);
        Assert.Contains("312,8000", c.Find("#pozisyon-servet-altin").TextContent);
        Assert.Contains("1.500", c.Find("#pozisyon-servet-cari").TextContent);
        Assert.Contains("USD", c.Find("#pozisyon-servet-pb-USD").TextContent);
        Assert.Equal("—", c.Find("#pozisyon-servet-toplam").TextContent.Trim());   // toplam NULL → tire, ekran toplamaz
        Assert.Contains("GBP kuru yok", c.Find("#pozisyon-servet-eksik").TextContent);
    }

    [Fact]
    public void Sade_yuzde_servet_bolumu_ayrinti_acilana_kadar_cizilmez()
    {
        var c = Render<AltinPozisyonuKarti>(p => p.Add(x => x.Sade, true));
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-net")));
        Assert.Empty(c.FindAll("#pozisyon-servet"));
        c.Find("#pozisyon-ayrinti").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-servet")));
    }
}
