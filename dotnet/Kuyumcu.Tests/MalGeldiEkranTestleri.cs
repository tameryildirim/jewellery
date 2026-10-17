using System.Net;
using System.Text.Json.Nodes;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// 17.10 kurucu: stok girişi tek adımlı "Mal geldi" (Satın alma ekranı). Sipariş formunun aynısı "Mal geldi"
// kipinde açılır; Stoğa al → POST /satinalma/mal-geldi; eşik üstünde MASAK beyanı aynı çekmecede istenir.
public sealed class MalGeldiEkranTestleri : BunitContext
{
    private const string Detay = """
        {"id":"s1","no":"SAS-2026-00001","durum":"KAPANDI","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.",
         "beklenenTarih":"2026-10-20","aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":5,"birimFiyatTl":2000,"kabulEdilenAdet":2,"kalanAdet":3,"sira":0}],
         "irsaliyeler":[{"id":"i1","no":"IRS-2026-00001","tarih":"2026-10-09T08:10:00Z","durum":"ACIK","faturaId":null}],
         "duzenlenebilir":false,"malKabulEdilebilir":false,"kilitSebebi":"Onaylanmış siparişin kalemleri değiştirilemez; kalan kalem mal kabulüyle kapanır."}
        """;

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Metot, string Parca, HttpStatusCode Kod, string Govde)> yollar = [];

    public MalGeldiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();      // tur 3: mal kabul MASAK gereksinimini sorar
    }

    private sealed class SahteApi(MalGeldiEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            foreach (var (m, p, k, g) in t.yollar)
                if (m == istek.Method.Method && yol.Contains(p))
                    return new HttpResponseMessage(k) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
            // TUR 3 — gerçekçi MASAK yanıtları (sunucu MasakKuralMotoru şekli; eşik 370.000 kimlik):
            // canlı 5166 /masak/esikler ile aynı: 200.000 altı YOK, 200.000+ kimlik + amaç, 2.000.000,01+ ayrıca fon kaynağı (canlı 5166 yanıtıyla aynı kademe).
            if (yol.Contains("/masak/esikler"))
                return Json("""{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""");
            if (yol.Contains("/masak/gereksinim"))
            {
                var m2 = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
                var tutar = m2.Success ? decimal.Parse(m2.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
                return Json(tutar < 200000m ? """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}"""
                    : tutar < 2000000.01m
                    ? """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}"""
                    : """{"kademe":"BEYAN_FORMU","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":true,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""");
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static HttpResponseMessage Json(string g) =>
        new(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };

    private void Yol(string metot, string parca, string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        yollar.Add((metot, parca, kod, govde));

    private void Oturum(string rol, string mod = ArayuzModuServisi.Profesyonel) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb", null, mod));

    private List<(string Metot, string Yol, string Govde)> Istekler(string metot)
    {
        lock (istekler) return istekler.Where(i => i.Metot == metot).ToList();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SatinAlma> FormuDoldur(decimal fiyat)
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", """{"satirlar":[],"toplam":0,"kirilim":[],"limit":200,"azamiLimit":200,"sinirDegdi":false}""");
        Yol("GET", "/arama?q=Has&tur=CARI", """{"sorgu":"Has","tur":"CARI","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"c1","ad":"Has Toptan A.Ş.","altBilgi":null,"tur":"CARI","rota":"/cariler"}],"gozatma":false}""");
        Yol("GET", "/arama?q=bilezik&tur=URUN", """{"sorgu":"bilezik","tur":"URUN","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"u1","ad":"22 ayar bilezik","altBilgi":null,"tur":"URUN","rota":"/urunler"}],"gozatma":false}""");
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        Yol("POST", "/satinalma/mal-geldi", "{\"siparis\":" + Detay + ",\"irsaliyeId\":\"i9\",\"irsaliyeNo\":\"IRS-2026-00009\",\"kabulEdilenKalem\":1,\"yeniDurum\":\"KAPANDI\"}");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForElement("#sa-mal-geldi").Click();
        e.WaitForElement("#sa-form-tedarikci");
        UzakSec(e, "sa-form-tedarikci", "Has");
        UzakSec(e, "sa-form-urun-0", "bilezik");
        e.Find("#sa-form-fiyat-0").Focus();
        e.Find("#sa-form-fiyat-0").Input(fiyat.ToString(System.Globalization.CultureInfo.InvariantCulture));
        e.Find("#sa-form-fiyat-0").Blur();
        return e;
    }

    private static void UzakSec(IRenderedComponent<Kuyumcu.Web.Pages.SatinAlma> e, string id, string sorgu)
    {
        e.Find("#" + id).Click();
        e.WaitForElement($"#{id}-ara-girdi").Input(sorgu);
        e.WaitForElement($"#{id}-secenek-0", TimeSpan.FromSeconds(5)).Click();
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, id))));
    }

    private static string KaydetId => "cekmece-sa-form-kaydet";

    [Fact]
    public void MGE_01_mal_geldi_birincil_tek_istekte_stoga_alir_irsaliye_bantta()
    {
        var e = FormuDoldur(1_000m);
        Assert.Contains("dugme-birincil", e.Find("#sa-mal-geldi").ClassList);
        Assert.Contains("dugme-ikincil", e.Find("#sa-yeni").ClassList);   // tek birincil
        Assert.Equal(MalGeldiSozlugu.Kaydet("tr"), e.Find("#" + KaydetId).TextContent.Trim());
        e.WaitForAssertion(() => Assert.False(e.Find("#" + KaydetId).HasAttribute("disabled")));
        e.Find("#" + KaydetId).Click();
        e.WaitForAssertion(() => Assert.Single(Istekler("POST"), i => i.Yol.Contains("/satinalma/mal-geldi")));
        var g = JsonNode.Parse(Istekler("POST").Single(i => i.Yol.Contains("/satinalma/mal-geldi")).Govde)!;
        Assert.Equal("c1", (string?)g["tedarikciCariId"]);
        Assert.Equal("kur1", (string?)g["kurSnapshotId"]);
        Assert.Equal("u1", (string?)g["kalemler"]![0]!["urunId"]);
        Assert.Equal(1m, (decimal)g["kalemler"]![0]!["adet"]!);
        Assert.Equal(1_000m, (decimal)g["kalemler"]![0]!["birimFiyatTl"]!);
        Assert.Null(g["masak"]);
        Assert.Empty(Istekler("POST").Where(i => i.Yol.EndsWith("/satinalma") || i.Yol.Contains("/onayla")));   // üç adım istemcide YOK
        e.WaitForAssertion(() => Assert.Contains("IRS-2026-00009", e.Markup));
    }

    [Fact]
    public void MGE_02_esik_ustunde_once_MASAK_beyani_istenir_istek_gitmez()
    {
        var e = FormuDoldur(400_000m);
        e.WaitForAssertion(() => Assert.False(e.Find("#" + KaydetId).HasAttribute("disabled")));
        e.Find("#" + KaydetId).Click();
        e.WaitForElement("#sa-form-masak");
        Assert.Contains(MalGeldiSozlugu.MasakDoldurun("tr"), e.Find("#sa-form-hata").TextContent);
        Assert.Empty(Istekler("POST").Where(i => i.Yol.Contains("/satinalma/mal-geldi")));
        e.WaitForAssertion(() => Assert.True(e.Find("#" + KaydetId).HasAttribute("disabled")));   // beyan eksik → kilit
    }
}
