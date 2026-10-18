using Bunit;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// D1-07 — altınla öde ön izlemesi: 22 ayar 5 g = 4,58 has; 2 has borç düşer, 2,58 has emanete (milyem BİNDE — 18.10'da
// ekran milyemi oran sanıp 4.580 has yazmıştı; tarayıcıda görüldü).
public sealed class D1AltinOdeEkranTestleri : BunitContext
{
    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""", System.Text.Encoding.UTF8, "application/json"),
            });
    }

    public D1AltinOdeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    [Fact]
    public void Onizleme_borcu_ve_emaneti_has_olarak_dogru_yazar()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.AltinOdeCekmecesi>(p => p.Add(x => x.Cari, new CariVeri("c1", "Selin", "BIREYSEL", null, null, 0m, 2m)));
        e.WaitForElement("#ao-gram");
        e.Find("#ao-gram").Input("5"); e.Find("#ao-gram").Blur();
        e.WaitForAssertion(() =>
        {
            var o = e.Find("#ao-onizleme").TextContent;
            Assert.Contains("4,5800", o);
            Assert.Contains("borçtan düşen 2,0000", o);
            Assert.Contains("emanete giren 2,5800", o);
            Assert.DoesNotContain("has has", o);
        });
    }
}
