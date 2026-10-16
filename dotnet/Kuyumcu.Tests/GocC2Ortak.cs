using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C-2 — test ortak yardımcıları.
// (1) Satış tuvalinin tabloları ortak ızgaraya (Bilesenler/Grid/Izgara) taşındı; ızgara ApiIstemci ister
//     (tercih okuma: GET /tercihler/benim). Bileşeni TEK BAŞINA çizen eski testler bu bağımlılığı kaydetmiyordu →
//     IzgaraServisleri: boş tercih yanıtı veren sahte HTTP + ApiIstemci (ızgara "kayıtlı tercih yok" = varsayılan düzen).
// (2) /vitrin ayar tablosu PRO'da da KAPALI katlanırla gelir (K4: 375'te 10 ekran) → AyarlariAc: açıcıya basar.
// ═════════════════════════════════════════════════════════════════════════════
public static class GocC2Ortak
{
    /// <summary>Ortak ızgaranın bağımlılıkları (yalnız eksikse eklenir — mevcut kayıtları ezmez).</summary>
    public static void IzgaraServisleri(BunitServiceProvider s)
    {
        if (!s.Any(d => d.ServiceType == typeof(OturumServisi))) s.AddScoped<OturumServisi>();
        if (!s.Any(d => d.ServiceType == typeof(HttpClient)))
            s.AddScoped(_ => new HttpClient(new BosTercihIsleyici()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        if (!s.Any(d => d.ServiceType == typeof(ApiIstemci))) s.AddScoped<ApiIstemci>();
    }

    /// <summary>Her isteğe 200 + boş tercih kümesi (ızgaranın tek isteği tercih okumasıdır).</summary>
    private sealed class BosTercihIsleyici : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"tercihler":{}}""", System.Text.Encoding.UTF8, "application/json"),
            });
    }

    /// <summary>/vitrin ayar katlanırı kapalıysa açar ve ayar ızgarasını bekler.</summary>
    public static void AyarlariAc<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent
    {
        var ac = e.WaitForElement("#vitrin-ayar-kat-ac", TimeSpan.FromSeconds(10));
        if (ac.GetAttribute("aria-expanded") != "true") ac.Click();
        e.WaitForElement("#vitrin-ayarlar", TimeSpan.FromSeconds(10));
    }
}
