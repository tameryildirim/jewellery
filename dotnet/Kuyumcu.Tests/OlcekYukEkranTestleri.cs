using System.Net;
using System.Text;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-YUK t2 — tasarım hakemi KRİTİK (UX-S29 "sessiz kırpma yok"):
// Kâr-zarar > Ürünler listesi sunucu sınırında (50) sessizce kesiliyordu; yanıttaki
// dahaFazlasiVar ekrana hiç gelmiyordu. Ölçülen:
//   • sınıra değince bant: gösterilen sayı + SUNUCUNUN toplam sayısı (ekran türetmez)
//   • "Daha fazla göster" gerçek istek atar (ofset = gösterilen satır), satırlar EKLENİR
//   • son sayfadan sonra bant ve düğme kalkar, "tamamı gösteriliyor" yazar
//   • sınıra değmeyen listede bant/düğme YOK (Pusula sadeliği)
//   • sonraki sayfa hatası listeyi boşaltmaz, hata bandı çıkar (UX-S28)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class OlcekYukEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private int toplam = 65;
    private bool ikinciSayfaHata;

    public OlcekYukEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    /// <summary>Sunucu gibi: limit 50, ofset'ten itibaren; toplam ve dahaFazlasiVar sunucudan.</summary>
    private string UrunSayfasi(int ofset)
    {
        const int limit = 50;
        var adet = Math.Max(0, Math.Min(limit, toplam - ofset));
        var sb = new StringBuilder();
        for (var i = 0; i < adet; i++)
        {
            var n = ofset + i + 1;
            if (i > 0) sb.Append(',');
            sb.Append($$"""{"urunId":"u{{n}}","kod":"K-{{n}}","ad":"OYE Ürün {{n:000}}","satisAdedi":1,"iadeAdedi":0,"brutHasilatTl":1000,"iadeTl":0,"hasilatTl":1000,"maliyetTl":600,"bilinenHasilatTl":1000,"karTl":400,"karDurumu":"TAM","maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"maden":"ALTIN","hasilatHas":0.15,"maliyetHas":0.09,"karHas":0.06,"hasKarDurumu":"TAM","hasMaliyetiEksikSatisAdedi":0,"karMarjiYuzde":40,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"}""");
        }
        var daha = toplam > ofset + adet ? "true" : "false";
        return $$"""{"baslangic":"2026-09-01","bitis":"2026-09-17","subeId":null,"urunSayisi":{{adet}},"limit":50,"ofset":{{ofset}},"toplamUrunSayisi":{{toplam}},"dahaFazlasiVar":{{daha}},"maliyetiBilinenAdet":{{adet}},"maliyetiBilinmeyenAdet":0,"kuruBulunamayanSatis":0,"guvenilirlik":"TAM","satirlar":[{{sb}}]}""";
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> UrunSekmesi()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-sekme-urunler").Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#kz-urun-tablo tbody tr")));
        return c;
    }

    private static int SatirSayisi(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c) =>
        c.FindAll("#kz-urun-tablo tbody tr").Count;

    [Fact]
    public void OYE01_Sinira_degen_liste_bant_ve_daha_fazla_goster_ile_tamamlanir()
    {
        var c = UrunSekmesi();
        Assert.Equal(50, SatirSayisi(c));
        var bant = c.Find("#kz-urun-sinir");
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.UrunListeSiniri, "50", "65"), bant.TextContent.Trim());
        Assert.Equal("status", bant.GetAttribute("role"));
        var dugme = c.Find("#kz-urun-daha");
        Assert.Equal(KarRaporSozlugu.Tr.DahaFazlaGoster, dugme.TextContent.Trim());
        Assert.False(dugme.HasAttribute("disabled"));

        // Düğme GERÇEK istek atar: ofset = gösterilen satır, dönem aynı.
        dugme.Click();
        c.WaitForAssertion(() => Assert.Equal(65, SatirSayisi(c)));
        var urunIstekleri = istekler.Where(y => y.Contains("/raporlar/kar-zarar/urun", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, urunIstekleri.Count);
        Assert.DoesNotContain("ofset=", urunIstekleri[0], StringComparison.Ordinal);
        Assert.Contains("ofset=50", urunIstekleri[1], StringComparison.Ordinal);
        Assert.Contains("baslangic=", urunIstekleri[1], StringComparison.Ordinal);

        // Satırlar eklenir (ilk sayfa korunur, örtüşme yok); son sayfadan sonra bant ve düğme kalkar.
        var adlar = c.FindAll("#kz-urun-tablo tbody tr .kz-urun-ad-metin").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(Enumerable.Range(1, 65).Select(n => $"OYE Ürün {n:000}"), adlar);
        Assert.Empty(c.FindAll("#kz-urun-sinir"));
        Assert.Empty(c.FindAll("#kz-urun-daha"));
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.UrunListeTamami, "65"), c.Find("#kz-urun-tamami").TextContent.Trim());
    }

    [Fact]
    public void OYE02_Sinira_degmeyen_listede_bant_ve_dugme_yok()
    {
        toplam = 12;
        var c = UrunSekmesi();
        Assert.Equal(12, SatirSayisi(c));
        Assert.Empty(c.FindAll("#kz-urun-sinir"));
        Assert.Empty(c.FindAll("#kz-urun-daha"));
        Assert.Empty(c.FindAll("#kz-urun-tamami"));
    }

    [Fact]
    public void OYE03_Sonraki_sayfa_hatasi_listeyi_bosaltmaz_hata_bandi_cikar()
    {
        ikinciSayfaHata = true;
        var c = UrunSekmesi();
        c.Find("#kz-urun-daha").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-urun-daha-hata")));
        Assert.Equal(50, SatirSayisi(c));                       // hata "kayıt yok"a çevrilmez
        Assert.NotNull(c.Find("#kz-urun-sinir"));               // sınır bilgisi durur, yeniden denenebilir
        Assert.False(c.Find("#kz-urun-daha").HasAttribute("disabled"));
    }

    [Fact]
    public async Task OYE04_Ingilizce_bant_sozlukten()
    {
        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var c = UrunSekmesi();
        Assert.Equal(string.Format(KarRaporSozlugu.En.UrunListeSiniri, "50", "65"), c.Find("#kz-urun-sinir").TextContent.Trim());
        Assert.Equal(KarRaporSozlugu.En.DahaFazlaGoster, c.Find("#kz-urun-daha").TextContent.Trim());
    }

    private static string Fikstur(string ad) =>
        (string)(typeof(KarZararEkranTestleri).GetField(ad, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                 ?.GetValue(null) ?? throw new InvalidOperationException($"KarZararEkranTestleri.{ad} fikstürü yok"));

    private sealed class SahteApi(OlcekYukEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            t.istekler.Add(tam);
            var ofset = 0;
            var m = System.Text.RegularExpressions.Regex.Match(tam, @"[?&]ofset=(\d+)");
            if (m.Success) ofset = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

            if (yol.Contains("kar-zarar/urun", StringComparison.Ordinal) && ofset > 0 && t.ikinciSayfaHata)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"Sunucu hatası"}""", Encoding.UTF8, "application/json"),
                });

            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun", StringComparison.Ordinal) => t.UrunSayfasi(ofset),
                var y when y.Contains("kar-zarar", StringComparison.Ordinal) => Fikstur("KarZararYaniti"),
                var y when y.Contains("stok-degerlemesi", StringComparison.Ordinal) => Fikstur("StokYaniti"),
                var y when y.EndsWith("/kur/guncel", StringComparison.Ordinal) =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-17T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, Encoding.UTF8, "application/json"),
            });
        }
    }
}
