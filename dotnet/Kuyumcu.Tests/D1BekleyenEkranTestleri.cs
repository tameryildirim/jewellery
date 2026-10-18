using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-03 (18.10) — tezgâhta işlem bekletme (Yıldız yüzü): kalem varken "Beklet" POST eder ve fişi boşaltır;
// fiş boşken "Bekleyen fişler (n)" açılır, "Al" içeriği tezgâha yükler (kalem + müşteri), "Sil" …/sil POST eder.
// Fiş doluyken bekleyen alınmaz (sebep yazılır). Harness Yildiz4BTestleri ile aynı sahte uçlar.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1BekleyenEkranTestleri : BunitContext
{
    private string sema = "yildiz";
    private decimal kimlikEsigi = 185000m;
    private bool bekleyenVar = true;
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(D1BekleyenEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/bekleyen-fisler") && yol.EndsWith("/sil"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            if (yol.EndsWith("/al") && istek.Method == HttpMethod.Post) t.bekleyenVar = false;
            var y = yol switch
            {
                var v when v.Contains("/bekleyen-fisler") && v.EndsWith("/al") =>
                    """{"cariId":"c1","kalemler":[{"urunId":"u1","urunAdi":"Gram altın","adet":2,"birimFiyat":10000,"hediye":false,"satirIskontoYuzde":0}],"fisIskontoYuzde":null,"aciklama":null}""",
                var v when v.Contains("/bekleyen-fisler") && istek.Method == HttpMethod.Post =>
                    """{"id":"bf2","etiket":"Gram altın","cariAd":null,"kalemSayisi":1,"toplamTl":10000,"kullaniciAd":"Hakan","olusturuldu":"2026-10-18T11:00:00Z"}""",
                var v when v.Contains("/bekleyen-fisler") => t.bekleyenVar
                    ? """[{"id":"bf1","etiket":"Selin Ergüven","cariAd":"Selin Ergüven","kalemSayisi":1,"toplamTl":20000,"kullaniciAd":"Hakan","olusturuldu":"2026-10-18T11:00:00Z"}]"""
                    : "[]",
                var v when v.Contains("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Selin Ergüven"}]""",
                var v when v.Contains("/masak/gereksinim") && t.kimlikEsigi < 185000m => """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => $$"""{"kimlikEsigi":{{t.kimlikEsigi}},"amacBeyaniEsigi":{{t.kimlikEsigi}},"beyanFormuEsigi":{{t.kimlikEsigi}},"ekBelgeEsigi":{{t.kimlikEsigi}},"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public D1BekleyenEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Satis()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "TEZGAHTAR", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        return e;
    }

    private int Say(string metot, string parca) { lock (istekler) return istekler.Count(i => i.Metot == metot && i.Yol.Contains(parca)); }

    [Fact]
    public void Bos_fiste_bekleyenler_acilir_Al_kalem_ve_musteriyi_yukler_Sil_atar()
    {
        var e = Satis();
        Assert.Empty(e.FindAll("#yildiz-tezgah-beklet"));               // kalem yok → Beklet yok
        e.WaitForElement("#yildiz-tezgah-bekleyenler");
        Assert.Contains("(1)", e.Find("#yildiz-tezgah-bekleyenler").TextContent);
        e.Find("#yildiz-tezgah-bekleyenler").Click();
        e.WaitForElement("#yildiz-bekleyen-bf1");
        Assert.Contains("Hakan", e.Find("#yildiz-bekleyen-bf1").TextContent);

        e.Find("#yildiz-bekleyen-al-bf1").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("button.yz-tezgah-kalem")));
        Assert.Contains("Gram altın", e.Find("#yildiz-kalem-0").TextContent);
        Assert.Equal(1, Say("POST", "/bekleyen-fisler/bf1/al"));
        e.WaitForAssertion(() => Assert.Contains("Selin Ergüven", e.Find("#yildiz-tezgah-ust").TextContent));
        Assert.Contains("tezgâha alındı", e.Find("#yildiz-tezgah-bekleyen-not").TextContent);
        Assert.Empty(e.FindAll("#yildiz-tezgah-bekleyenler"));            // fiş doldu → liste düğmesi gizli
        Assert.NotNull(e.Find("#yildiz-tezgah-beklet"));
    }

    [Fact]
    public void Kalem_varken_Beklet_POST_eder_ve_fisi_bosaltir()
    {
        bekleyenVar = false;
        var e = Satis();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForElement("#yildiz-tezgah-beklet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/bekleyen-fisler")));
        string g; lock (istekler) g = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/bekleyen-fisler")).Govde;
        var kok = JsonDocument.Parse(g).RootElement.GetProperty("icerik");
        Assert.Equal("u1", kok.GetProperty("kalemler")[0].GetProperty("urunId").GetString());
        Assert.Equal(JsonValueKind.Null, kok.GetProperty("cariId").ValueKind);          // Peşin Müşteri bekletilmez (sunucuya null)
        e.WaitForAssertion(() => Assert.Contains("bekletildi", e.Find("#yildiz-tezgah-bekleyen-not").TextContent));
        e.WaitForElement("#yildiz-tezgah-bos");                                          // fiş boşaldı
    }
}
