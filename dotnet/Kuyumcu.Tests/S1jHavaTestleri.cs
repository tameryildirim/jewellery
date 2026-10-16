using System.Net;
using Kuyumcu.Api.Modules.Hava;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-j — HAVA SUNUCU ÜZERİNDEN (GET /api/v1/hava)
//
// ÖNCE: istemci api.open-meteo.com'a doğrudan fetch ediyordu; CSP
// `connect-src 'self'` her Panel açılışında konsola ihlal düşürüyordu.
// ŞİMDİ: dış çağrıyı SUNUCU yapar (30 dk önbellek); kapalı/hata/zaman aşımında
// uç 204 döner ve ekranda hava HİÇ gösterilmez — UYDURMA SICAKLIK YOK.
// CSP GEVŞETİLMEDİ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1jHavaSaftestleri
{
    private sealed class SahteKaynak(Func<HttpRequestMessage, HttpResponseMessage> cevap) : HttpMessageHandler
    {
        public int Istek { get; private set; }
        public string? SonAdres { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            Istek++;
            SonAdres = istek.RequestUri!.ToString();
            return Task.FromResult(cevap(istek));
        }
    }

    private sealed class Fabrika(HttpMessageHandler h) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(h, disposeHandler: false);
    }

    private static (HavaServisi Servis, SahteKaynak Kaynak) Kur(
        Func<HttpRequestMessage, HttpResponseMessage> cevap, bool etkin = true)
    {
        var yap = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hava:Etkin"] = etkin ? "true" : "false",
            ["Hava:Sehir"] = "İstanbul",
            ["Hava:Enlem"] = "41.0138",
            ["Hava:Boylam"] = "28.9497",
        }).Build();
        var kaynak = new SahteKaynak(cevap);
        return (new HavaServisi(new Fabrika(kaynak), yap, new MemoryCache(new MemoryCacheOptions())), kaynak);
    }

    private static HttpResponseMessage Json(string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };

    private const string Iyi = """{"current":{"temperature_2m":21.4,"weather_code":0}}""";

    [Fact]
    public async Task Varsayilan_KAPALI_dis_istek_CIKMAZ_null_doner()
    {
        var (s, k) = Kur(_ => Json(Iyi), etkin: false);

        Assert.Null(await s.Getir());
        Assert.Equal(0, k.Istek);
    }

    [Fact]
    public async Task Acikken_sunucu_kaynagi_cagirir_derece_sehir_kod_doner_ve_30_dk_onbellekler()
    {
        var (s, k) = Kur(_ => Json(Iyi));

        var a = await s.Getir();
        var b = await s.Getir();

        Assert.NotNull(a);
        Assert.Equal(21.4m, a!.Derece);
        Assert.Equal(0, a.Kod);
        Assert.Equal("İstanbul", a.Sehir);
        Assert.Same(a, b);                       // ikinci çağrı önbellekten
        Assert.Equal(1, k.Istek);
        Assert.Contains("latitude=41.0138", k.SonAdres, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(30), HavaServisi.OnbellekSuresi);
    }

    [Fact]
    public async Task Kaynak_HATA_donerse_UYDURMA_sicaklik_yok_null()
    {
        var (s, _) = Kur(_ => Json("""{"hata":"x"}""", HttpStatusCode.InternalServerError));
        Assert.Null(await s.Getir());
    }

    [Fact]
    public async Task Kaynaga_ULASILAMAZSA_UYDURMA_sicaklik_yok_null()
    {
        var (s, _) = Kur(_ => throw new HttpRequestException("ağ yok"));
        Assert.Null(await s.Getir());
    }

    [Fact]
    public async Task Kaynak_DERECESIZ_yanit_verirse_UYDURMA_sicaklik_yok_null()
    {
        var (s, _) = Kur(_ => Json("""{"current":{"weather_code":3}}"""));
        Assert.Null(await s.Getir());
    }
}

[Collection("http")]
public sealed class S1jHavaHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Hava_ucu_yapilandirma_yokken_204_doner_govde_bos()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S1J-HAVA-" + Guid.NewGuid().ToString("N")[..6]);
        using var c = f.Ortam.Istemci(k);

        var r = await c.GetAsync("/api/v1/hava");

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Equal("", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hava_ucu_oturumsuz_401_doner()
    {
        using var c = f.Ortam.AnonimIstemci();
        var r = await c.GetAsync("/api/v1/hava");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
