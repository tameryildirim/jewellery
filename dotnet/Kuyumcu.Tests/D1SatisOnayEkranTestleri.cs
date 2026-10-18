using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-05 (18.10) — tezgâhta satış onayı: sunucu 409 ONAY_GEREKLI → bant "Onay iste"; talep aynı satış içeriğiyle açılır,
// bant "Onay bekleniyor"; durum ONAYLANDI olunca bant "Kaydet'e basın" ve yeniden Kaydet onayTalebiId taşır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1SatisOnayEkranTestleri : BunitContext
{
    private string sema = "yildiz";
    private decimal kimlikEsigi = 185000m;
    private bool bekleyenVar = false;
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(D1SatisOnayEkranTestleri t) : HttpMessageHandler
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
            if (yol.Contains("/islemler/satis") && istek.Method == HttpMethod.Post && !govde.Contains("onayTalebiId"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.Conflict)
                { Content = new StringContent("""{"message":"Bu satış patron onayı ister — tezgâhtan onay isteyin.","kod":"ONAY_GEREKLI","error":"Conflict","statusCode":409}""", System.Text.Encoding.UTF8, "application/json") };
            var y = yol switch
            {
                var v when v.Contains("/satis-onayi/talepler") && istek.Method == HttpMethod.Post =>
                    """{"id":"ot1","durum":"BEKLIYOR","sebepler":["ISKONTO"],"toplamTl":9000,"listeTl":10000,"indirimYuzde":10,"hasDegeriTl":6700,"kalemSayisi":1,"isteyenAd":"Hakan","cariAd":null,"kararVerenAd":null,"kararNotu":null,"olusturuldu":"2026-10-18T11:00:00Z","kararZamani":null}""",
                var v when v.Contains("/satis-onayi/talepler/ot1") =>
                    """{"id":"ot1","durum":"ONAYLANDI","sebepler":["ISKONTO"],"toplamTl":9000,"listeTl":10000,"indirimYuzde":10,"hasDegeriTl":6700,"kalemSayisi":1,"isteyenAd":"Hakan","cariAd":null,"kararVerenAd":"Patron","kararNotu":null,"olusturuldu":"2026-10-18T11:00:00Z","kararZamani":"2026-10-18T11:01:00Z"}""",
                var v when v.Contains("/kur-kilitleri") => "[]",
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

    public D1SatisOnayEkranTestleri()
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
    public void Onay_gerekli_bant_talep_acar_onaylaninca_Kaydet_onay_kimligini_tasir()
    {
        var e = Satis();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForElement("#yildiz-tezgah-kaydet").Click();
        e.WaitForElement("#satis-onay-iste", TimeSpan.FromSeconds(5));
        Assert.Contains("patron onayı", e.Find("#satis-onay-metin").TextContent);
        e.Find("#satis-onay-iste").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/satis-onayi/talepler")));
        string g; lock (istekler) g = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/satis-onayi/talepler")).Govde;
        var kok = JsonDocument.Parse(g).RootElement;
        Assert.Equal("u1", kok.GetProperty("kalemler")[0].GetProperty("urunId").GetString());
        Assert.Equal("kur-1", kok.GetProperty("kurSnapshotId").GetString());
        e.WaitForAssertion(() => Assert.Contains("Onaylandı", e.Find("#satis-onay-metin").TextContent), TimeSpan.FromSeconds(8));
        e.Find("#yildiz-tezgah-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis") && i.Govde.Contains("\"onayTalebiId\":\"ot1\"")); }, TimeSpan.FromSeconds(5));
    }
}
