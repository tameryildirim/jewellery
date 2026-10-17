using System.Net;
using System.Text.Json.Nodes;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GA = Kuyumcu.Tests.TemaTemizligiGDYardimcisi;

namespace Kuyumcu.Tests;

// 17.10 kurucu: "fiş her zaman oluşacağından seçenek kalksın; fatura da her zaman oluşsun ama müşteriye sorsun
// fatura kesilsin mi diye; zorunlu durumlarda hayır işaretlenirse emin misiniz sınırı aştınız uyarısı verilsin,
// evet derse fatura da kesilir". Ürün 10.000 TL; had parametreyle (altında/üstünde).
public sealed class FaturaSorusuEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal had = 100_000m;

    private sealed class SahteApi(FaturaSorusuEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/islemler/havale-bekleyen") => "[]",
                var v when v.Contains("/satis-belge") => $$"""{"varsayilan":"FATURA","kuyumcuFaturaHaddi":{{t.had}}}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar/nakit-ayrimi") => "[]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","kur","emanet"]}""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"p1","ad":"Peşin Müşteri","sistemCarisi":true},{"id":"c1","ad":"Selin Ergüven"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    public FaturaSorusuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<HavaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Tezgah(string? musteri)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        if (musteri is not null) GA.MusteriSec(e, musteri);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        return e;
    }

    private int SatisPostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")); }

    private JsonNode SonSatis()
    {
        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), "satış POST'u gelmedi");
        return JsonNode.Parse(g!)!;
    }

    [Fact]
    public void FS_01_Fis_Fatura_secimi_yok_pesin_musteride_soru_sorulmaz_faturasiz_gider()
    {
        var e = Tezgah(null);
        Assert.Empty(e.FindAll("#yildiz-belge-fis, #yildiz-belge-fatura"));
        e.Find("#yildiz-tezgah-kaydet").Click();
        Assert.Empty(e.FindAll("#yildiz-belge-soru"));
        Assert.Null((string?)SonSatis()["belgeTuru"]);
    }

    [Fact]
    public void FS_02_musterili_satista_once_soru_Evet_birincil_ve_faturali_gider()
    {
        var e = Tezgah("c1");
        e.Find("#yildiz-tezgah-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("Fatura kesilsin mi?", e.Find("#yildiz-belge-soru-metin").TextContent.Trim()));
        Assert.Equal(0, SatisPostSayisi());                                     // soru cevaplanmadan kayıt yok
        Assert.Empty(e.FindAll("#yildiz-tezgah-kaydet"));                       // tek birincil: cevap
        Assert.Contains("yz-b1", e.Find("#yildiz-belge-evet").ClassList);       // ayar FATURA → Evet önerilir
        Assert.Contains("yz-b2", e.Find("#yildiz-belge-hayir").ClassList);
        e.Find("#yildiz-belge-evet").Click();
        var n = SonSatis();
        Assert.Equal("FATURA", (string?)n["belgeTuru"]);
        Assert.Equal("c1", (string?)n["cariId"]);
    }

    [Fact]
    public void FS_03_musterili_satista_Hayir_faturasiz_gider()
    {
        var e = Tezgah("c1");
        e.Find("#yildiz-tezgah-kaydet").Click();
        e.WaitForElement("#yildiz-belge-hayir").Click();
        Assert.Null((string?)SonSatis()["belgeTuru"]);
    }

    [Fact]
    public void FS_04_sinir_asildi_Hayir_uyari_verir_Geri_don_kapatir_Evet_fatura_keser()
    {
        had = 5_000m;   // 10.000 TL satış sınırın üstünde
        var e = Tezgah("c1");
        e.Find("#yildiz-tezgah-kaydet").Click();
        e.WaitForElement("#yildiz-belge-hayir").Click();
        e.WaitForAssertion(() => Assert.Contains("fatura sınırını", e.Find("#yildiz-belge-soru-metin").TextContent));
        Assert.Contains("Emin misiniz?", e.Find("#yildiz-belge-soru-metin").TextContent);
        Assert.Equal(0, SatisPostSayisi());
        e.Find("#yildiz-belge-geri").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-belge-soru")));
        Assert.Equal(0, SatisPostSayisi());
        e.Find("#yildiz-tezgah-kaydet").Click();
        e.WaitForElement("#yildiz-belge-hayir").Click();
        e.WaitForElement("#yildiz-belge-geri");
        e.Find("#yildiz-belge-evet").Click();                                   // "Evet, fatura kes"
        Assert.Equal("FATURA", (string?)SonSatis()["belgeTuru"]);
    }
}
