using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d madde 15 — SÜRESİ DOLMUŞ OTURUM
//
// QA turu 1: sayfa açılışında 3 paralel istek 401 alıyor; 3 × /auth/yenile 401,
// 3 × /auth/cikis ve Blazor'un "Beklenmeyen bir hata oluştu" bandı çıkıyordu.
// Beklenen: TEK yenileme denemesi, TEK çıkış, /giris'e yönlendirme ve
// kullanıcıya Türkçe "Oturum süresi doldu" mesajı (genel hata bandı DEĞİL).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX4dOturumTestleri : BunitContext
{
    private readonly List<string> yollar = [];

    private ApiIstemci Kur()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new HepUnauthorized(yollar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<ApiIstemci>();

        var oturum = Services.GetRequiredService<OturumServisi>();
        oturum.TokenKaydetAsync("eski-access", "eski-refresh").GetAwaiter().GetResult();
        return Services.GetRequiredService<ApiIstemci>();
    }

    [Fact]
    public async Task M15_Ucu_paralel_401_TEK_yenileme_ve_TEK_cikis_uretir()
    {
        var api = Kur();

        var gorevler = new[] { "/kur/guncel", "/cari", "/urunler" }
            .Select(yol => Task.Run(async () =>
            {
                try { await api.GetAsync(yol); }
                catch (ApiHata) { /* beklenen */ }
            }));
        await Task.WhenAll(gorevler);

        Assert.Equal(1, yollar.Count(y => y.Contains("auth/yenile")));
        Assert.True(yollar.Count(y => y.Contains("auth/cikis")) <= 1);
    }

    [Fact]
    public async Task M15_Oturum_dustugunde_TURKCE_mesaj_ve_giris_yonlendirmesi_olur()
    {
        var api = Kur();
        var nav = Services.GetRequiredService<NavigationManager>();

        var hata = await Assert.ThrowsAsync<OturumDustuHatasi>(() => api.GetAsync("/kur/guncel"));

        Assert.Equal("Oturum süresi doldu — lütfen tekrar giriş yapın.", hata.Message);
        Assert.Equal(401, hata.StatusCode);
        Assert.EndsWith("/giris", nav.Uri);
    }

    [Fact]
    public async Task M15_Oturum_dustukten_SONRAKI_istekler_yeniden_yenileme_DENEMEZ()
    {
        var api = Kur();

        await Assert.ThrowsAsync<OturumDustuHatasi>(() => api.GetAsync("/kur/guncel"));
        var ilkDeneme = yollar.Count(y => y.Contains("auth/yenile"));

        await Assert.ThrowsAsync<OturumDustuHatasi>(() => api.GetAsync("/cari"));

        Assert.Equal(ilkDeneme, yollar.Count(y => y.Contains("auth/yenile")));
    }

    /// <summary>Her isteğe 401 döner — süresi dolmuş oturumun aynısı.</summary>
    private sealed class HepUnauthorized(List<string> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            lock (kayit) kayit.Add(istek.RequestUri!.PathAndQuery);
            // ⚠ GERÇEK EŞZAMANLILIK: yanıt anında dönerse üç istek sırayla
            // çalışır ve tek-uçuş kilidi sınanmamış olur. Gecikme, üç isteğin
            // 401'i AYNI ANDA görmesini sağlar — QA'nın gördüğü tablo budur.
            await Task.Delay(40, iptal);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"message":"Yetkisiz","statusCode":401}""",
                    System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
