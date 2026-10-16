using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — 29.09 DEVAM 6 (tasarım hakemi KRİTİK): İşlem defteri "Yazdır"
// yalnız ekrandaki 6 satırı basıyordu, kâğıtta başlık ve tarih aralığı yoktu.
// Artık seçili aralığın TAMAMI sunucudan sayfa sayfa (500'er) çekilir; kâğıda
// başlık + tarih aralığı + tüm satırlar basılır; window.print TEK noktadan
// (YazdirDugmesi, Otomatik kip). Sahte API sayfalamayı gerçek gibi yapar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZoBelgeYazdirTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private int toplam = 612;

    public ZoBelgeYazdirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    private string Defter(int ofset, int limit)
    {
        var adet = Math.Max(0, Math.Min(limit, toplam - ofset));
        var satirlar = string.Join(",", Enumerable.Range(ofset, adet).Select(i =>
            $$"""{"id":"h{{i}}","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":{{i + 1}},"hasGrami":0,"cariAd":"Müşteri {{i}}"}"""));
        var daha = ofset + adet < toplam ? "true" : "false";
        return $$"""
            {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":{{toplam}},"ofset":{{ofset}},"limit":{{limit}},"dahaFazlasiVar":{{daha}},
             "ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},"storno":{"adet":0,"tutarTl":0},"satirlar":[{{satirlar}}]}
            """;
    }

    private sealed class Sahte(ZoBelgeYazdirTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var q = System.Web.HttpUtility.ParseQueryString(istek.RequestUri.Query);
            t.istekler.Add(istek.RequestUri.PathAndQuery);
            var g = yol switch
            {
                var x when x.EndsWith("/raporlar/islem-defteri") =>
                    t.Defter(int.Parse(q["ofset"] ?? "0"), int.Parse(q["limit"] ?? "100")),
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void ZB47_Yazdir_secili_araligin_TAMAMINI_sayfa_sayfa_ceker_baslik_aralik_ve_tum_satirlari_basar()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        Assert.Equal(GorselSigmaSozlugu.IslemDefteriSayfaBoyu, c.FindAll("#id-tablo tbody tr").Count);
        Assert.True(GorselSigmaSozlugu.IslemDefteriSayfaBoyu >= 25, "ekran sayfası en az 25 satır");
        Assert.Empty(c.FindAll("#id-yazdir-belge"));
        Assert.Empty(JSInterop.Invocations["window.print"]);

        c.Find("#id-yazdir").Click();
        c.WaitForElement("#id-yazdir-belge");

        // İki ayrı sayfa isteği (500 + 112) — ekran sayfası (25) DEĞİL.
        Assert.Contains(istekler, i => i.Contains("/raporlar/islem-defteri?") && i.Contains("limit=500") && i.Contains("ofset=0"));
        Assert.Contains(istekler, i => i.Contains("/raporlar/islem-defteri?") && i.Contains("limit=500") && i.Contains("ofset=500"));
        Assert.Equal(612, c.FindAll("#id-yazdir-tablo tbody tr").Count);
        Assert.Contains("Müşteri 611", c.Find("#id-yazdir-tablo").TextContent);

        // Başlık = menü adı, tarih aralığı ve kayıt sayısı kâğıtta.
        var ZB = ZoBelgeSozlugu.Tr;
        Assert.Equal(MenuAdlari.Icin("tr").IslemDefteri.Ad, c.Find("#id-yazdir-baslik").TextContent.Trim());
        var bas = Bicim.Tarih(DateTime.SpecifyKind(DateTime.Today.AddDays(-6), DateTimeKind.Local), "tr-TR");
        var bit = Bicim.Tarih(DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local), "tr-TR");
        Assert.Equal(string.Format(ZB.YazdirAralik, bas, bit), c.Find("#id-yazdir-aralik").TextContent.Trim());
        Assert.Equal(string.Format(ZB.YazdirKayit, Bicim.Girdi(612, "N0"), Bicim.Girdi(612, "N0")), c.Find("#id-yazdir-sayi").TextContent.Trim());
        Assert.Empty(c.FindAll("#id-yazdir-sinir"));   // hepsi basıldı → sınır uyarısı yok

        // Ekrandaki sayfa kâğıda çıkmaz (yerine belge basılır); window.print TEK noktadan bir kez.
        Assert.Contains("yazdirma-gizle", c.Find("#id-tablo").ParentElement!.ParentElement!.ClassList);
        Assert.Contains("yazdirma-gizle", c.Find("#id-ozet").ClassList);
        Assert.NotNull(c.Find("#id-yazdir-oto"));
        c.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["window.print"]));
        Assert.Contains("zo-yazdir-belge", c.Find("#id-yazdir-belge").ClassList);
    }

    [Fact]
    public void ZB48_Yazdir_bos_listede_kilitli_sebep_yazar_tiklansa_da_istek_atmaz()
    {
        toplam = 0;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-yazdir-kilit");
        Assert.True(c.Find("#id-yazdir").HasAttribute("disabled"));
        // 29.09 doğrulayıcı DÜŞÜK: önce/sonra sayımı arada EYLEM olmadan yapılıyordu. Artık
        // kilitli düğmeye tıklama DENENİR (tarayıcı disabled düğmede olayı yutar; bUnit yutmaz —
        // bu yüzden işleyicinin kendi koruması da sınanır): istek gitmez, belge oluşmaz.
        var once = istekler.Count;
        c.Find("#id-yazdir").Click();
        Assert.Equal(once, istekler.Count);
        Assert.Empty(c.FindAll("#id-yazdir-belge"));
        Assert.Empty(JSInterop.Invocations["window.print"]);
    }
}
