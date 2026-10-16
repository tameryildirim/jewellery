using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÇIKIŞ — SUNUCU TARAFI TOKEN İPTALİ
//
// TESPİT: `POST /api/v1/auth/cikis` sunucuda VARDI ve refresh token'ı doğru
// biçimde iptal ediyordu; ama web istemcisi bu ucu HİÇ çağırmıyor, tokenleri
// yalnız localStorage'dan siliyordu.
//
// Sonuç: çıkıştan önce kopyalanmış (XSS, ortak bilgisayar, çalınmış yedek) bir
// refresh token çıkıştan SONRA da geçerli kalıyor, /auth/yenile ile yeni
// access token üretmeye devam ediyordu. Kullanıcı "çıktım" sanırken oturum
// fiilen açıktı.
//
// Bu dosya çıkışın gerçekten sunucuya bildirildiğini sınar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CikisGuvenlikTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool sunucuDusuk;

    public CikisGuvenlikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => sunucuDusuk))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        // PAKET P-07: çıkış artık ApiIstemci dayanıklılık bandından geçer
        // (web tarafında ApiIstemci dışında HttpClient tüketicisi kalmadı).
        // Üretimdeki Program.cs kaydının aynısı burada da kurulur.
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<bool> dusukMu) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            if (dusukMu()) throw new HttpRequestException("ağ yok");
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.AbsolutePath, govde));
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        }
    }

    private async Task<OturumServisi> AcikOturum()
    {
        var oturum = Services.GetRequiredService<OturumServisi>();
        await oturum.TokenKaydetAsync("access-abc", "refresh-xyz");
        istekler.Clear();
        return oturum;
    }

    [Fact]
    public async Task Cikis_sunucuya_bildirilir()
    {
        var oturum = await AcikOturum();

        await oturum.CikisAsync();

        var istek = Assert.Single(istekler);
        Assert.Equal("POST", istek.Metot);
        Assert.Equal("/api/v1/auth/cikis", istek.Yol);
    }

    [Fact]
    public async Task Cikis_iptal_edilecek_REFRESH_tokeni_gonderir()
    {
        // Access token göndermek işe yaramaz — iptal edilmesi gereken kayıt
        // refresh token'ın hash'iyle bulunuyor.
        var oturum = await AcikOturum();

        await oturum.CikisAsync();

        var govde = System.Text.Json.JsonDocument.Parse(istekler[0].Govde).RootElement;
        Assert.Equal("refresh-xyz", govde.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Cikis_yerel_tokenleri_de_temizler()
    {
        var oturum = await AcikOturum();

        await oturum.CikisAsync();

        Assert.Null(oturum.AccessToken);
        Assert.Null(oturum.RefreshToken);
        Assert.False(oturum.GirisYapildi);
    }

    [Fact]
    public async Task Sunucuya_ulasilamazsa_kullanici_yine_de_cikar()
    {
        // Ağ hatası yerel çıkışı ENGELLEMEMELİ: aksi hâlde çevrimdışı bir
        // kullanıcı ortak bilgisayarda oturumunu kapatamaz.
        var oturum = await AcikOturum();
        sunucuDusuk = true;

        await oturum.CikisAsync();

        Assert.False(oturum.GirisYapildi);
        Assert.Empty(istekler);
    }

    [Fact]
    public async Task Zaten_kapali_oturumda_sunucuya_bosuna_gidilmez()
    {
        var oturum = Services.GetRequiredService<OturumServisi>();

        await oturum.CikisAsync();

        Assert.Empty(istekler);
    }
}
