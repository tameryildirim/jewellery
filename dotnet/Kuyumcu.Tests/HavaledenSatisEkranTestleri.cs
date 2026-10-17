using System.Net;
using System.Text.Json.Nodes;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// 17.10 HAVALEDEN SATIŞ — kurucu: "hesaba para geldiğinde listeye düşer, tezgâhtar ne sattıysa kendisi seçer ve direkt
// fatura kesilir". Tezgâhta bekleyen havaleler katlı gelir; seçilince belge FATURA, müşteri havalenin müşterisi,
// ödeme tek HAVALE bacağı (kasa yok, bankaHareketId dolu). Görünürlük kanıtı gerçek tarayıcıdadır.
public sealed class HavaledenSatisEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Doluysa bekleyen havale listesi bu kapı açılana dek DÖNMEZ (açılış yarışını sahnelemek için).</summary>
    private TaskCompletionSource? havaleKapisi;

    private sealed class SahteApi(HavaledenSatisEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/islemler/havale-bekleyen") && t.havaleKapisi is { } kapi) await kapi.Task;
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/islemler/havale-bekleyen") =>
                    """[{"id":"bh1","tarih":"2026-10-17T09:00:00","tutar":10000,"karsiTarafAd":"SELIN ERGUVEN","cariId":"c1","cariAd":"Selin Ergüven"}]""",
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

    public HavaledenSatisEkranTestleri()
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

    [Fact]
    public void HS_E1_bekleyen_havale_katli_gelir_secilince_fatura_ve_tek_HAVALE_bacagi_kasasiz_gider()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "TEZGAHTAR", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));

        // Katlı: yalnız "Gelen havaleler (1)" düğmesi; liste kapalı.
        var ac = e.WaitForElement("#yildiz-havaleler-ac", TimeSpan.FromSeconds(5));
        Assert.Equal("Gelen havaleler (1)", ac.TextContent.Trim());
        Assert.Empty(e.FindAll("#yildiz-havaleler"));
        ac.Click();
        var satir = e.WaitForElement("#yildiz-havale-bh1", TimeSpan.FromSeconds(5));
        Assert.Contains("Selin Ergüven", satir.TextContent);
        Assert.DoesNotContain("TR", satir.TextContent.Replace("Ergüven", ""));   // IBAN yok
        satir.Click();

        e.WaitForAssertion(() =>
        {
            Assert.Equal("true", e.Find("#yildiz-belge-fatura").GetAttribute("aria-pressed"));
            Assert.Contains("Selin Ergüven havalesi", e.Find("#yildiz-belge-not").TextContent);
            Assert.NotNull(e.Find("#yildiz-havale-birak"));
            Assert.Empty(e.FindAll("#yildiz-havaleler-ac"));
            Assert.Empty(e.FindAll("#yildiz-tezgah-baska-odeme, #yildiz-tezgah-para-ustu"));   // ödeme yalnız havale
            Assert.DoesNotContain("Ana Kasa", e.Find("#yildiz-odeme-0").TextContent);       // havale kasaya girmez
        }, TimeSpan.FromSeconds(5));

        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-kaydet").Click();

        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), "satış POST'u gelmedi");
        var n = JsonNode.Parse(g!)!;
        var odemeler = n["odemeler"]!.AsArray();
        var bacak = Assert.Single(odemeler)!;
        Assert.Equal("HAVALE", (string?)bacak["arac"]);
        Assert.Equal("bh1", (string?)bacak["bankaHareketId"]);
        Assert.Null((string?)bacak["kasaId"]);
        Assert.Equal(10000m, (decimal)bacak["tutarTl"]!);
        Assert.Equal("FATURA", (string?)n["belgeTuru"]);
        Assert.Equal("c1", (string?)n["cariId"]);
    }

    [Fact]
    public void HS_E2_havaleden_degil_normal_satisa_doner()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "TEZGAHTAR", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-havaleler-ac", TimeSpan.FromSeconds(10)).Click();
        e.WaitForElement("#yildiz-havale-bh1", TimeSpan.FromSeconds(5)).Click();
        e.WaitForElement("#yildiz-havale-birak", TimeSpan.FromSeconds(5)).Click();
        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("#yildiz-havale-birak"));
            Assert.NotNull(e.Find("#yildiz-havaleler-ac"));
            Assert.DoesNotContain("havalesi", e.FindAll("#yildiz-belge-not").FirstOrDefault()?.TextContent ?? "");
        }, TimeSpan.FromSeconds(5));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> HavaleAdresiyleAc()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "TEZGAHTAR", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("islemler?havale=bh1");
        return Render<Kuyumcu.Web.Pages.Islemler>();
    }

    [Fact]
    public void HS_E3_havale_adresi_acilista_havaleyi_secer()
    {
        var e = HavaleAdresiyleAc().TezgahBekle();
        e.WaitForElement("#yildiz-havale-birak", TimeSpan.FromSeconds(5));
        Assert.Equal("true", e.Find("#yildiz-belge-fatura").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void HS_E4_havale_listesi_gelirken_baska_ture_gecen_tezgahtarin_secimi_EZILMEZ()
    {
        // Açılış yarışı (17.10): tezgâh çizildi, havale listesi henüz gelmedi; tezgâhtar Hurda alışa geçti.
        // Liste gelince ?havale= türü SATIŞ'a geri ÇEVİRMEZ (HavaleSec kalemleri temizlemeden taşırdı).
        havaleKapisi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = HavaleAdresiyleAc();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        Assert.Equal("suruyor", e.Find("[data-acilis]").GetAttribute("data-acilis"));
        TemaTemizligiGDYardimcisi.TurSec(e, "HURDA_ALIS");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah")), TimeSpan.FromSeconds(5));

        havaleKapisi.SetResult();
        e.TezgahBekle();
        Assert.Empty(e.FindAll("#yildiz-tezgah"));        // hâlâ Hurda alış — satış tezgâhına dönülmedi
        Assert.Empty(e.FindAll("#yildiz-havale-birak"));  // havale bu fişe zorlanmadı
    }
}
