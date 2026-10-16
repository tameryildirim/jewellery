using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-64 — SATIŞ ANALİZİ: GRAFİK + ÖZET KARTINDA ÇIKIŞ [bUnit]
//
// Tur 8 ölçümü: /raporlar/satis-analizi'nde HİÇ grafik yoktu ve özet kartı
// yalnız GİRİŞ tutarını basıyordu (çıkış yalnız tabloda). Bu dosya:
//   • ödeme sekmesinde parça sayısı > 0 olan bir çubuk grafiğin çizildiğini,
//   • özet kartının hem GİRİŞ hem ÇIKIŞ TL tutarını gösterdiğini,
//   • çıkış TL toplamının DÖVİZİ TL'ye katmadığını (sunucunun girisTlToplam
//     kuralıyla aynı: yalnız TRY satırları — UX-S32)
// ölçer.
//
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: gövde, SatisRaporlari.OdemeAraci'nin
//   gerçek şeklini taşır (RaporEkranTestleri fikstürüyle aynı alanlar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SatisAnaliziEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];

    private const string OdemeAraci = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","kaynak":"KASA_HAREKETLERI",
         "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":12000.00,"cikisTutar":4500.00,"adet":5},
                                                     {"paraBirimi":"USD","girisTutar":300.00,"cikisTutar":999.00,"adet":2}],
                     "girisTlToplam":12000.00,"adet":7},
                    {"arac":"KART","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":8000.00,"cikisTutar":0,"adet":2}],
                     "girisTlToplam":8000.00,"adet":2}],
         "veresiye":{"doganTl":3000.00,"kapananTl":0},
         "bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":4},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"YUKSEK"}
        """;

    public SatisAnaliziEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol, istek.RequestUri.Query));
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/odeme-araci") => OdemeAraci,
                var y when y.EndsWith("/raporlar/personel") =>
                    """{"baslangic":"2026-08-19","bitis":"2026-09-17","olcut":"KAYDEDEN_KULLANICI","uyari":"SATAN_DEGIL_KAYDEDEN","toplamSatir":0,"satirlar":[]}""",
                var y when y.EndsWith("/raporlar/sube-karsilastirma") =>
                    """{"baslangic":"2026-08-19","bitis":"2026-09-17","transferCiroyaKatilir":false,"toplamSatir":0,"toplamCiroTl":0,"satirlar":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SatisAnalizi> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-ozet")));
        return e;
    }

    [Fact]
    public void Sayfa_EnAzBirVeriGrafigiCizer()
    {
        var e = Ekran();
        var cubuklar = e.FindAll("#sa-grafik .grafik-sar svg rect");
        // 2 araç × 2 seri (giriş, çıkış) = 4 çubuk.
        Assert.Equal(4, cubuklar.Count);
        Assert.Contains(cubuklar, r => double.Parse(r.GetAttribute("height")!,
            System.Globalization.CultureInfo.InvariantCulture) > 0);
        // Lejant iki seriyi de adlandırır.
        var lejant = e.Find("#sa-grafik .grafik-lejant").TextContent;
        Assert.Contains("Giriş", lejant);
        Assert.Contains("Çıkış", lejant);
    }

    [Fact]
    public void OzetKarti_CikisTutariniDaGosterir()
    {
        var e = Ekran();
        var nakit = e.FindAll("#sa-ozet .stat-kart")
            .First(k => k.QuerySelector(".stat-etiket")!.TextContent.Trim() == "Nakit");   // KAT-1: ham "NAKIT" kodu basılmaz (UX-S31)

        var deger = nakit.QuerySelector(".stat-deger")!.TextContent;
        var alt = nakit.QuerySelector(".stat-alt")!.TextContent;
        Assert.Contains("Giriş", deger);
        Assert.Contains(Bicim.Tl(12000m), deger);
        Assert.Contains("Çıkış", alt);
        // Yalnız TRY çıkışı: 4.500 — USD'nin 999'u TL toplamına KATILMAZ (UX-S32).
        Assert.Contains(Bicim.Tl(4500m), alt);
        Assert.DoesNotContain("5.499", alt);
        Assert.Contains("7", alt);   // hareket sayısı korunur
    }
}
