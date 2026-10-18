using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-04 (18.10) — tezgâhta kur kilidi (Yıldız yüzü): tezgâhtar yalnız 15 dk / 1 saat seçer, kilitlemek POST eder ve
// "Kur kilitli" özeti çıkar; müşterinin kilidi varsa "Kilitli kur var · Kullan" → tezgâh kilidin kurundan fiyatlar;
// "Kilidi kaldır" …/kaldir POST eder. Harness D1BekleyenEkranTestleri ile aynı sahte uçlar (+ kur-kilitleri).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1KurKilidiEkranTestleri : BunitContext
{
    private string sema = "yildiz";
    private decimal kimlikEsigi = 185000m;
    private bool bekleyenVar = true;
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(D1KurKilidiEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/bekleyen-fisler") && yol.EndsWith("/sil"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            if (yol.EndsWith("/al") && istek.Method == HttpMethod.Post) t.bekleyenVar = false;
            // D1-16: set okutması — ürün barkodu yok (404), set barkodu iki parça döner.
            if (yol.Contains("/urunler/barkod/SET-1"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                { Content = new StringContent("""{"message":"bulunamadı"}""", System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/kur-kilitleri/") && yol.EndsWith("/kaldir"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            var y = yol switch
            {
                var v when v.Contains("/kur-kilitleri") && istek.Method == HttpMethod.Post =>
                    """{"id":"kk2","kurSnapshotId":"kur-1","cariId":null,"cariAd":null,"altinHasFiyati":6800,"usdTry":null,"eurTry":null,"not":null,"bitis":"2099-01-01T10:00:00Z","olusturanAd":"Hakan","olusturuldu":"2026-10-18T11:00:00Z"}""",
                var v when v.Contains("/kur-kilitleri") =>
                    """[{"id":"kk1","kurSnapshotId":"kur-9","cariId":"c1","cariAd":"Selin Ergüven","altinHasFiyati":6500,"usdTry":null,"eurTry":null,"not":null,"bitis":"2099-01-01T10:00:00Z","olusturanAd":"Hakan","olusturuldu":"2026-10-18T11:00:00Z"}]""",
                var v when v.Contains("/urun-setleri/barkod/SET-1") =>
                    """{"id":"set1","ad":"Takım","barkod":"SET-1","aktif":true,"kalemler":[{"urunId":"u1","urunAdi":"Gram altın","urunKodu":"G1","adet":1},{"urunId":"u2","urunAdi":"Küpe","urunKodu":"K1","adet":2}]}""",
                var v when v.EndsWith("/urunler/u2") =>
                    """{"id":"u2","ad":"Küpe","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1,"tip":"ZIYNET","kod":"K1","tasAgirlik":0,"aktif":true}""",
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

    public D1KurKilidiEkranTestleri()
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
    public void Tezgahtar_15dk_ya_da_1saat_kilitler_ozet_cikar_kaldir_POST_eder()
    {
        bekleyenVar = false;
        var e = Satis();
        e.WaitForElement("#yildiz-kur-kilitle").Click();
        var sureler = e.FindAll("#yildiz-kur-kilidi-sureler button").Select(b => b.Id).ToList();
        Assert.Equal(["yildiz-kur-kilitle-15", "yildiz-kur-kilitle-60"], sureler);   // tezgâhtar 60 dk üstünü görmez
        Assert.Empty(e.FindAll("[id^=yildiz-kur-kilidi-kullan-]"));                    // Peşin müşteri: c1'in kilidi önerilmez
        e.Find("#yildiz-kur-kilitle-15").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/kur-kilitleri")));
        string g; lock (istekler) g = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/kur-kilitleri")).Govde;
        var kok = JsonDocument.Parse(g).RootElement;
        Assert.Equal("kur-1", kok.GetProperty("kurSnapshotId").GetString());
        Assert.Equal(15, kok.GetProperty("sureDakika").GetInt32());
        Assert.Equal(JsonValueKind.Null, kok.GetProperty("cariId").ValueKind);
        e.WaitForAssertion(() => Assert.Contains("Kur kilitli", e.Find("#yildiz-kur-kilidi-etkin").TextContent));
        Assert.Contains("6.800", e.Find("#yildiz-kur-kilidi-etkin").TextContent);
        e.Find("#yildiz-kur-kilidi-kaldir").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/kur-kilitleri/kk2/kaldir")));
        e.WaitForElement("#yildiz-kur-kilitle");
    }

    [Fact]
    public void Musterinin_kilidi_Kullan_ile_tezgaha_gelir_fiyat_kilidin_kurundan_istenir()
    {
        var e = Satis();
        e.WaitForElement("#yildiz-tezgah-bekleyenler").Click();
        e.WaitForElement("#yildiz-bekleyen-al-bf1").Click();                           // müşteri c1 tezgâhta
        e.WaitForElement("#yildiz-kur-kilidi-kullan-kk1");
        Assert.Contains("6.500", e.Find("#yildiz-kur-kilidi-uygun-kk1").TextContent);
        e.Find("#yildiz-kur-kilidi-kullan-kk1").Click();
        e.WaitForAssertion(() => Assert.Contains("Kur kilitli", e.Find("#yildiz-kur-kilidi-etkin").TextContent));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.True(Say("GET", "kurSnapshotId=kur-9") > 0), TimeSpan.FromSeconds(5));
    }
}
