using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// E-8 (20.10) — İşletme › e-Fatura bağlantısı çekmecesi: tek birincil düğme "Kaydet ve dene", anahtar girilmeden kilitli,
// anahtar alanı parola tipinde; kayıtlıyken anahtar boş bırakılabilir (yer tutucu söyler); sonuç aynı çekmecede.
public sealed class EBelgeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string ayarJson = """{"saglayici":null,"ortam":null,"seriOnEki":null,"etkin":false,"kayitli":false,"baslangic":null,"sonDeneme":null,"sonHata":null,"saglayicilar":[{"kod":"NILVERA","ad":"Nilvera"}]}""";
    private string kayitYaniti = "";

    private sealed class SahteApi(EBelgeEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            var govde = r.Content is null ? "" : await r.Content.ReadAsStringAsync(c);
            lock (t.istekler) t.istekler.Add((r.Method.Method, r.RequestUri!.PathAndQuery, govde));
            var y = r.Method == HttpMethod.Put ? t.kayitYaniti : t.ayarJson;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    public EBelgeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    [Fact]
    public void Ilk_baglanti_anahtarsiz_kilitli_anahtarla_kaydet_ve_dene_basari_ayni_cekmecede()
    {
        kayitYaniti = """{"ayar":{"saglayici":"NILVERA","ortam":"TEST","seriOnEki":null,"etkin":true,"kayitli":true,"baslangic":null,"sonDeneme":null,"sonHata":null,"saglayicilar":[{"kod":"NILVERA","ad":"Nilvera"}]},"basarili":true,"hata":null}""";
        var e = Render<Kuyumcu.Web.Bilesenler.EBelgeCekmecesi>(p => p.Add(x => x.Acik, true));
        e.WaitForElement("#eb-anahtar");
        var dugme = e.Find("#eb-kaydet");
        Assert.Equal("Kaydet ve dene", dugme.TextContent.Trim());
        Assert.True(dugme.HasAttribute("disabled"));
        Assert.Equal("password", e.Find("#eb-anahtar").GetAttribute("type"));
        e.Find("#eb-anahtar").Input("anahtar-123");
        e.WaitForAssertion(() => Assert.False(e.Find("#eb-kaydet").HasAttribute("disabled")));
        e.Find("#eb-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("Bağlantı çalışıyor.", e.Find("#eb-basari").TextContent.Trim()));
        var put = Assert.Single(istekler, i => i.Metot == "PUT");
        Assert.Contains("\"saglayici\":\"NILVERA\"", put.Govde);
        Assert.Contains("\"ortam\":\"TEST\"", put.Govde);
        Assert.Contains("\"anahtar\":\"anahtar-123\"", put.Govde);
        Assert.Equal("", e.Find("#eb-anahtar").GetAttribute("value") ?? "");   // anahtar ekranda kalmaz
    }

    [Fact]
    public void Kayitliyken_anahtar_bos_birakilabilir_son_hata_gosterilir()
    {
        ayarJson = """{"saglayici":"NILVERA","ortam":"CANLI","seriOnEki":"MHK","etkin":true,"kayitli":true,"baslangic":null,"sonDeneme":null,"sonHata":"Nilvera API anahtarını kabul etmedi (yetkisi yok ya da süresi dolmuş).","saglayicilar":[{"kod":"NILVERA","ad":"Nilvera"}]}""";
        var e = Render<Kuyumcu.Web.Bilesenler.EBelgeCekmecesi>(p => p.Add(x => x.Acik, true));
        e.WaitForElement("#eb-anahtar");
        Assert.False(e.Find("#eb-kaydet").HasAttribute("disabled"));
        Assert.Equal("Kayıtlı — değiştirmeyecekseniz boş bırakın", e.Find("#eb-anahtar").GetAttribute("placeholder"));
        Assert.Contains("kabul etmedi", e.Find("#eb-son-hata").TextContent);
        Assert.NotEmpty(e.FindAll("#eb-kaldir"));
    }
}
