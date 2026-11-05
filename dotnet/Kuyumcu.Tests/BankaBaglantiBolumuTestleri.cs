using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Kurucu 05.11 — banka bağlantısı bölümü (bUnit):
//   • "Bilgileri değiştir" açılınca bağlı bankanın HER alanı isteğe bağlıdır, yer tutucu
//     "Kayıtlı — değiştirmeyecekseniz boş bırakın"; hiçbir şey yazmadan Kaydet hazırdır ve
//     sunucuya yalnız dolu alanlar gider (boş = eski değer kalır).
//   • "Şimdi dene" bilgi değiştirmeden dener (PUT yok, POST /dene var).
//   • Başarısız deneme "son deneme <zaman>, başarısız" yazar; "henüz denenmedi" yalnız hiç denenmemişse.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class BankaBaglantiBolumuTestleri : BunitContext
{
    private static readonly JsonSerializerOptions J = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private BankaBaglantiOzet durum = new("GARANTI", "Garanti BBVA", "Onay numarası: 2f•••••• · Uygulama kimliği: l7••••••",
        new DateTime(2026, 11, 5, 8, 25, 0, DateTimeKind.Utc), null, "Garanti BBVA: Vermiş olduğunuz clientId-consentId bilgileri hatalıdır. (kod 4)");

    private sealed class SahteApi(BankaBaglantiBolumuTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.EndsWith("/bankalar/baglayicilar", StringComparison.Ordinal))
                return Yanit(JsonSerializer.Serialize(new BankaBaglayiciKatalogOzet("5.10.220.150",
                [new BankaBaglayiciOzet("GARANTI", "Garanti BBVA", "İnternet şubesi › Elektronik Hesap Özeti", true,
                    [new BankaAlaniOzet("consentId", "Onay numarası", false, true, null),
                     new BankaAlaniOzet("clientId", "Uygulama kimliği", false, false, null),
                     new BankaAlaniOzet("clientSecret", "Uygulama şifresi", true, false, null)])]), J));
            if (yol.EndsWith("/baglanti/dene", StringComparison.Ordinal))
                return Yanit(JsonSerializer.Serialize(new BankaBaglantiDeneSonucu(t.durum, 0, 0), J));
            if (yol.EndsWith("/baglanti", StringComparison.Ordinal) && istek.Method == HttpMethod.Put)
                return Yanit(JsonSerializer.Serialize(new BankaBaglantiKayitSonucu(t.durum, new BankaBaglantiDenemesi(false, 0, t.durum.SonHata)), J));
            if (yol.EndsWith("/baglanti", StringComparison.Ordinal))
                return Yanit(JsonSerializer.Serialize(t.durum, J));
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"hata":"beklenmeyen uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }

        private static HttpResponseMessage Yanit(string govde) =>
            new(HttpStatusCode.OK) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    public BankaBaglantiBolumuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<BankaBaglantiBolumu> Ciz()
    {
        var e = Render<BankaBaglantiBolumu>(p => p.Add(x => x.HesapId, "h1"));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#bb-durum")));
        return e;
    }

    [Fact]
    public void Basarisiz_deneme_son_deneme_yazar_henuz_denenmedi_DEMEZ()
    {
        var e = Ciz();
        var d = e.Find("#bb-durum").TextContent;
        Assert.Contains("son deneme", d);
        Assert.Contains("başarısız", d);
        Assert.DoesNotContain("henüz denenmedi", d);
        Assert.Contains("kod 4", e.Find("#bb-son-hata").TextContent);
    }

    [Fact]
    public void Hic_denenmemisse_henuz_denenmedi()
    {
        durum = durum with { SonDeneme = null, SonHata = null };
        var e = Ciz();
        Assert.Contains("henüz denenmedi", e.Find("#bb-durum").TextContent);
    }

    [Fact]
    public async Task Bilgileri_degistir_her_alan_istege_bagli_yer_tutuculu_bos_kaydet_hazir_yalniz_dolu_alan_gider()
    {
        var e = Ciz();
        e.Find("#bb-degistir").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#bb-form")));
        foreach (var ad in new[] { "consentId", "clientId", "clientSecret" })
        {
            var g = e.Find($"#bb-{ad}");
            Assert.Equal("false", g.GetAttribute("aria-required"));
            Assert.Equal("Kayıtlı — değiştirmeyecekseniz boş bırakın", g.GetAttribute("placeholder"));
        }
        Assert.True(e.Instance.KaydetHazir);   // hiçbir şey yazmadan "Kaydet ve dene" basılabilir

        e.Find("#bb-clientId").Input("l7yeni");
        await e.InvokeAsync(() => e.Instance.Kaydet());
        var put = Assert.Single(istekler, i => i.Metot == "PUT" && i.Yol.EndsWith("/bankalar/h1/baglanti"));
        using var g2 = JsonDocument.Parse(put.Govde);
        var alanlar = g2.RootElement.GetProperty("alanlar");
        Assert.Equal("l7yeni", alanlar.GetProperty("clientId").GetString());
        Assert.False(alanlar.TryGetProperty("consentId", out _));
        Assert.False(alanlar.TryGetProperty("clientSecret", out _));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#bb-form")));
    }

    [Fact]
    public void Simdi_dene_bilgi_degistirmeden_dener()
    {
        var e = Ciz();
        e.Find("#bb-dene").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/bankalar/h1/baglanti/dene")));
        Assert.DoesNotContain(istekler, i => i.Metot == "PUT");
        Assert.Empty(e.FindAll("#bb-form"));
    }
}
