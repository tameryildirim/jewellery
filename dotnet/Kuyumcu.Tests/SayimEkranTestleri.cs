using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SAYIM EKRANI — paket 23. Kapılar: hareketli satır uyarısı + onay kutusu ·
// sayılmayan uyarısı · hiç sayım yokken Kapat kapalı · kapat gövdesi onayı taşır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SayimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool hareketli;

    public SayimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => hareketli)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> hareketliMi) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var h = hareketliMi() ? "true" : "false";
            var detay = $$"""
                {"id":"o1","ad":"Vitrin","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":2,"sayilan":1,"sayilmayan":1,
                 "farkli":1,"eksikAdet":2,"fazlaAdet":0,"netFarkHas":-28.38,"hareketli":{{(hareketliMi() ? 1 : 0)}},
                 "satirlar":[
                  {"urunId":"u1","kod":"BB1","ad":"Burma Bilezik","barkod":"869001","sistemAdet":5,"sayilanAdet":3,"guncelAdet":5,"fark":-2,"oturumdaHareketVar":{{h}}},
                  {"urunId":"u2","kod":"KL1","ad":"Kolye","barkod":null,"sistemAdet":1,"sayilanAdet":null,"guncelAdet":1,"fark":null,"oturumdaHareketVar":false}]}
                """;
            var y = yol switch
            {
                var v when v.EndsWith("/sayim") && istek.Method == HttpMethod.Get => """[{"id":"o1","ad":"Vitrin","kapsamTur":"TUMU","durum":"ACIK"}]""",
                var v when v.EndsWith("/sayim/o1/kapat") => $$"""{"uygulanan":1,"atlanan":0,"detay":{{detay.Replace("\"ACIK\"", "\"KAPANDI\"")}}}""",
                var v when v.Contains("/sayim/o1") => detay,
                var v when v.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var v when v.Contains("/reyonlar") => "[]",
                var v when v.Contains("/urun-kategorileri/duz") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> OturumAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        e.WaitForElement("#sy-tablo tbody tr");
        e.Find("#sy-tablo tbody tr button").Click();
        e.WaitForElement("#sy-satirlar");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Fark_raporu_sistem_sayilan_guncel_ve_sayilmadi_gosterir()
    {
        var e = OturumAc();
        var satirlar = e.FindAll("#sy-satirlar tbody tr");
        Assert.Equal(2, satirlar.Count);
        Assert.Contains("-2", satirlar[0].TextContent);
        Assert.Contains("sayılmadı", satirlar[1].TextContent);
        Assert.Contains("SIFIRLANMAZ", e.Markup);           // sayılmayan uyarısı
    }

    /// <summary>Hareketli satır varsa uyarı + onay kutusu çizilir; onaysız kapatma gövdesi false taşır.</summary>
    [Fact]
    public void Hareketli_satirda_uyari_ve_onay_kutusu_vardir_govde_onayi_tasir()
    {
        hareketli = true;
        var e = OturumAc();
        Assert.NotNull(e.Find("#sy-hareketli-uyari"));
        e.Find("#sy-kapat").Click();
        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.EndsWith("/sayim/o1/kapat"));
            Assert.Contains("\"hareketliSatirlariUygula\":false", istek.Govde);
        });
        istekler.Clear();
    }

    [Fact]
    public void Onay_kutusu_isaretlenince_govde_TRUE_tasir()
    {
        hareketli = true;
        var e = OturumAc();
        e.Find("#sy-hareketli-onay").Change(true);
        e.Find("#sy-kapat").Click();
        e.WaitForAssertion(() =>
            Assert.Contains("\"hareketliSatirlariUygula\":true", Assert.Single(istekler, i => i.Yol.EndsWith("/sayim/o1/kapat")).Govde));
    }

    [Fact]
    public void Hareketsiz_oturumda_onay_kutusu_YOKTUR()
    {
        var e = OturumAc();
        Assert.Empty(e.FindAll("#sy-hareketli-onay"));
    }

    [Fact]
    public void Say_gonderisi_barkod_ve_adet_tasir()
    {
        var e = OturumAc();
        e.Find("#sy-barkod").Input("869001");   // PAKET GOC-C: ortak Arama
        e.Find("#sy-adet").Input("3");   // PAKET GOC-C: ortak Sayi
        e.Find("#sy-say").Click();
        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.EndsWith("/sayim/o1/say"));
            Assert.Contains("\"barkod\":\"869001\"", istek.Govde);
            Assert.Contains("\"sayilanAdet\":3", istek.Govde);
        });
    }
}
