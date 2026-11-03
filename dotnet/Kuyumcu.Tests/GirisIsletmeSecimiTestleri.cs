using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Kisit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 02.11 — GİRİŞ EKRANI: AYNI E-POSTA BİRDEN FAZLA İŞLETMEDE → İŞLETME SEÇİMİ
//
// Kurucu canlı sunucuda yakaladı: e-postası iki işletmede kayıtlı olunca ekran
// "tenantId belirtiniz" diye teknik bir hata basıyor, seçim sunmuyordu.
// Sözleşme:
//   (1) Sunucu isletmeSecimi döndürünce e-posta/parola formu yerine "Hangi işletme?"
//       seçimi gelir; işletme adları radyo olarak listelenir; jargon (tenantId) YOK.
//   (2) Seçim yapılmadan "Devam et" kilitli ve kilit sebebi yazılı; seçince açılır.
//   (3) Devam et → aynı e-posta + parola + SEÇİLEN tenantId ile /auth/giris yinelenir,
//       jeton gelince ana sayfaya gidilir.
//   (4) Vazgeç → seçim kapanır, parola bellekten silinir, giriş formu geri gelir.
//   (5) Tek birincil düğme; "Yeni işletme kur" bağlantısı seçim sırasında gizli.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirisIsletmeSecimiTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
                return Yanit(HttpStatusCode.OK, JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            if (yol.EndsWith("/auth/giris", StringComparison.Ordinal))
            {
                using var g = JsonDocument.Parse(govde);
                var tenantVar = g.RootElement.TryGetProperty("tenantId", out var t) && t.ValueKind == JsonValueKind.String;
                if (!tenantVar)
                    return Yanit(HttpStatusCode.OK,
                        """{"isletmeSecimi":true,"isletmeler":[{"tenantId":"t-a","ad":"Altın Çarşı Kuyumculuk","rol":"MUDUR"},{"tenantId":"t-b","ad":"Ramazan Deneme","rol":"SAHIP"}]}""");
                return Yanit(HttpStatusCode.OK,
                    """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t-b","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""");
            }
            return Yanit(HttpStatusCode.NotFound, """{"hata":"beklenmeyen uç"}""");
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    public GirisIsletmeSecimiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Giris> SecimeKadar()
    {
        Nav.NavigateTo("/giris");
        var e = Render<Giris>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#giris-kisit-yukleniyor")));
        e.Find("#giris-eposta").Input("ramazan@ornek.com");
        e.Find("#giris-sifre").Input("GucluSifre123");
        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#giris-isletme-form")));
        return e;
    }

    [Fact]
    public void IS01_Liste_gelince_isletme_secimi_acilir_adlar_radyo_jargon_yok_tek_birincil()
    {
        var e = SecimeKadar();

        Assert.Equal(Sozlukler.Tr.Auth.IsletmeSec, e.Find("#giris-isletme-baslik").TextContent.Trim());
        var secenekler = e.FindAll("#giris-isletme input[type=radio]");
        Assert.Equal(2, secenekler.Count);
        Assert.Contains("Altın Çarşı Kuyumculuk", e.Find("#giris-isletme").TextContent);
        Assert.Contains("Ramazan Deneme", e.Find("#giris-isletme").TextContent);
        Assert.DoesNotContain("tenantId", e.Markup);
        Assert.Single(e.FindAll(".btn-birincil"));
        Assert.Empty(e.FindAll("#giris-eposta"));          // e-posta/parola formu kapandı
        Assert.Empty(e.FindAll("#giris-yeni-dukkan"));     // seçim sırasında kayıt bağlantısı yok
    }

    [Fact]
    public void IS02_Secim_yapilmadan_Devam_kilitli_sebep_yazili_secince_acilir()
    {
        var e = SecimeKadar();

        Assert.True(e.Find("#giris-isletme-devam").HasAttribute("disabled"));
        Assert.Contains(Sozlukler.Tr.Auth.IsletmeSecEksik, e.Find("#giris-isletme-kilit").TextContent);

        e.Find("#giris-isletme-secenek-1").Change("t-b");

        e.WaitForAssertion(() => Assert.False(e.Find("#giris-isletme-devam").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#giris-isletme-kilit"));
    }

    [Fact]
    public void IS03_Devam_et_ayni_eposta_parola_ve_SECILEN_tenantId_ile_yeniden_ister_jetonla_ana_sayfaya_gider()
    {
        var e = SecimeKadar();
        e.Find("#giris-isletme-secenek-1").Change("t-b");
        e.WaitForAssertion(() => Assert.False(e.Find("#giris-isletme-devam").HasAttribute("disabled")));

        e.Find("#giris-isletme-form").Submit();

        e.WaitForAssertion(() => Assert.Equal(2, istekler.Count(i => i.Yol.EndsWith("/auth/giris"))));
        using var govde = JsonDocument.Parse(istekler.Last(i => i.Yol.EndsWith("/auth/giris")).Govde);
        Assert.Equal("ramazan@ornek.com", govde.RootElement.GetProperty("email").GetString());
        Assert.Equal("GucluSifre123", govde.RootElement.GetProperty("sifre").GetString());
        Assert.Equal("t-b", govde.RootElement.GetProperty("tenantId").GetString());
        e.WaitForAssertion(() => Assert.EndsWith("/", Nav.Uri));
    }

    [Fact]
    public void IS04_Vazgec_secimi_kapatir_parolayi_siler_giris_formu_geri_gelir()
    {
        var e = SecimeKadar();

        e.Find("#giris-isletme-vazgec").Click();

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#giris-eposta")));
        Assert.Empty(e.FindAll("#giris-isletme-form"));
        Assert.Equal("", e.Find("#giris-sifre").GetAttribute("value") ?? "");
        Assert.Equal("ramazan@ornek.com", e.Find("#giris-eposta").GetAttribute("value"));
        Assert.NotEmpty(e.FindAll("#giris-yeni-dukkan"));
    }
}
