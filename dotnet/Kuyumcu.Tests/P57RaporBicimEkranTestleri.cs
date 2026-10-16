using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.P57;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-57 — /raporlar HAS VE GRAM HÜCRELERİNDE NOKTA ONDALIK YOK [bUnit]
//
// QA turu 8: /raporlar'da "₺1.500,00" (virgül ondalık) ile "12.500 g" /
// "-16.9460 has" / devir hızı "2.028" (nokta ondalık) yan yana basılıyordu.
// Kök Bicim.Has/Gram/Miktar'ın InvariantCulture kullanmasıydı (P-57).
//
// ⚠ Neden ayrı dosya/ad alanı: RaporEkranTestleri.cs ve RaporlarEkranTestleri.cs
//   paralel koşan SERİT D'nin (Raporlar.razor) alanında. Test adı
//   spesifikasyondaki gibi "RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez";
//   sınıf Kuyumcu.Tests.P57 ad alanındadır, başka dosyaya dokunulmadı.
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: açılışta çağrılan uçlara gerçekçi yanıt.
// ⚠ Sorgu gerçekten ondalık taşır (-16.946, 12.5, 2.0278, 0.5) — tam sayılı
//   fikstür nokta/virgül farkını göstermez ve test "sessizce" yeşil kalırdı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RaporEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];

    private const string Katalog = """
        {"toplam":15,"faz":"A","raporlar":[
          {"kod":"R01","ad":"Günlük Özet","rota":"/raporlar","uc":"/api/v1/raporlar/gunluk","roller":["SAHIP"],"faz":"A","grup":"GUNLUK"}]}
        """;

    private const string CariYaslandirma = """
        {"yaslananCariler":[
           {"cariId":"c1","cariAd":"Has Müşterisi","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":-16.946,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"},
           {"cariId":"c2","cariAd":"TL Müşterisi","cariTur":"MUSTERI","tlBakiye":1500.00,"hasGramBakiye":2.5,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":45,"dilim":"31-60"}],
         "ozetTl":{"0-30":0,"31-60":1500.00,"61-90":0,"90+":0,"BELIRSIZ":0},
         "toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    private const string Yaslandirma = """
        {"olculduAn":"2026-09-22T06:00:00Z","subeId":null,
         "kovalar":[
           {"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":4.000,"hasGram":12.500000,"maliyetTl":50000.00,"maliyetiBilinmeyenAdet":0.000},
           {"kod":"G31_90","altSinirGun":31,"ustSinirGun":90,"adet":2.500,"hasGram":6.250000,"maliyetTl":25000.00,"maliyetiBilinmeyenAdet":1.000}],
         "toplamAdet":6.500,"altinHasGram":18.750000,
         "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":6.500,"toplamGram":18.750000}],
         "yasiBilinmeyenAdet":1.500,
         "devir":{"pencereGun":90,"tuketilenAdet":5.000,"kalanAdet":10.000,"devirHizi":0.5000,
                  "yillikTahmini":2.0278,"ortalamaRafOmruGun":180.5,"yontem":"PARTI_TUKETIMI",
                  "tanim":"devirHizi = pencerede tüketilen adet ÷ bugün rafta kalan adet"},
         "oluStok":{"esikGun":180,"toplamSatir":1,"toplamAdet":1,"ofset":0,"limit":100,
                    "dahaFazlasiVar":false,"urunler":[{"urunId":"u7","kod":"K7","ad":"Eski Kolye","ayarId":"K22","maden":"ALTIN","adet":1,"hasGram":0.000400,"enEskiGiris":"2026-01-02T00:00:00","yasGun":263}]},
         "guvenilirlik":"YUKSEK","uyarilar":[]}
        """;

    private const string Mutabakat = """
        {"tarih":"2026-09-17",
         "tahsilat":{"nakit":150.00,"kart":0,"havale":0,"diger":0,"toplam":150.00},
         "satis":{"sayisi":1,"toplamTl":4000.00,"faturaliSayisi":0,"faturaliTl":0,
                  "faturasizSayisi":1,"faturasizTl":4000.00},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,
                  "stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":4000.00,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],
         "faturaEslesme":null}
        """;

    public RaporEkranTestleri()
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
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol, istek.RequestUri.Query));
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => Katalog,
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => CariYaslandirma,
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    // P-57 ek (doğrulayıcı bulgusu Raporlar.razor:391): KESİRLİ adet —
                    // tam sayı "2" nokta/virgül farkını göstermezdi.
                    """{"urunler":[{"urunId":"u9","kod":"K9","ad":"Kesirli Zincir","ayarId":"K22","adet":2.5,"toplamHas":3.1234}],"toplamHas":3.1234,"madenDagilimi":[],"toplamSatir":1,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => Yaslandirma,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // Nokta ondalıklı sayı: "12.500", "-16.9460", "2.028", "0.5". Tarih ("17.09.2026")
    // ve tr-TR binlik ("₺1.500,00", "250.000,00") eşleşmesin diye iki yanı
    // rakam/nokta/virgül olmayan sayı aranır. T-K1: "1.500 ₺" (NBSP+₺ soneki, Bicim.Tl
    // sıfır kuruşu atar) binlik noktadır, ondalık değildir — hariç tutulur.
    private static readonly Regex NoktaOndalik =
        new(@"(?<![\d.,])-?\d+\.\d+(?![\d.,]|\u00a0₺)", RegexOptions.CultureInvariant);

    [Fact]
    public void HasVeGramHucreleri_NoktaOndalikIcermez()
    {
        // P-57 ek: Blazor WASM'da kültürsüz ToString TARAYICI kültürünü kullanır.
        // en-US tarayıcıyı taklit et; kültürsüz bir hücre "2.5" basar ve yakalanır.
        var onceki = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try { HasVeGramHucreleri_Govde(); }
        finally { System.Globalization.CultureInfo.CurrentCulture = onceki; }
    }

    private void HasVeGramHucreleri_Govde()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-tablo")));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#stok-yaslandirma-kart .stat-izgara")));

        // 1) Cari yaşlandırma has hücreleri (satır + toplam) — birebir metin.
        var hasHucreleri = e.FindAll("#yas-tablo td.yas-has").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("-16,9460 has", hasHucreleri);
        Assert.Contains("2,5000 has", hasHucreleri);
        Assert.Equal("-14,4460 has", e.Find("#yas-toplam-has").TextContent.Trim());

        // 2) Stok yaşlandırma kartı — HAS kovaları (ZO-BELGE 28.09: kova değeri HAS gramıdır →
        //    Bicim.Has, 4 hane; F3 "g" ile 0,0004 has kayboluyordu) + Miktar (kesirli adet, devir hızı).
        var kart = e.Find("#stok-yaslandirma-kart").TextContent;
        Assert.Contains("12,5000 has", kart, StringComparison.Ordinal);
        Assert.Contains("6,2500 has", kart, StringComparison.Ordinal);
        Assert.DoesNotContain("12,500 g", kart, StringComparison.Ordinal);
        Assert.Contains("2,5", kart, StringComparison.Ordinal);     // kova adedi 2.5
        // RAPOR-GORUNUM (veri c): stok yaşı ondalıkları tek kural — oranlar 2 hane, gün tam sayı.
        Assert.Contains("2,03", kart, StringComparison.Ordinal);    // yıllık tahmin 2.0278
        Assert.Contains("181", kart, StringComparison.Ordinal);     // tahmini satış süresi 180.5 → tam gün
        // 2a) Ölü stok tablosu — ürün HAS gramı 4 hane (ZO-BELGE 28.09, P-57): F3 "g" ile
        //     0,0004 has "0,000 g" olup kayboluyordu.
        Assert.Contains("0,0004 has", kart, StringComparison.Ordinal);
        Assert.DoesNotContain("0,000 g", kart, StringComparison.Ordinal);

        // 2b) Stok değeri tablosu — kesirli adet (P-57 ek, Raporlar.razor:391).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-stok-has-baslik")));
        var stokTablo = e.Find("#rapor-stok-has-baslik").Closest("table")!;
        var stokHucreleri = stokTablo.QuerySelectorAll("tbody td").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("2,5", stokHucreleri);
        Assert.Contains("3,1234 has", stokHucreleri);

        // 3) Genel kural: üç bölümde de nokta ondalıklı TEK sayı yok.
        foreach (var metin in new[] { e.Find("#yas-tablo").TextContent,
                                      e.Find("#stok-yaslandirma-kart").TextContent,
                                      stokTablo.TextContent })
        {
            var secici = metin.Length > 40 ? metin[..40] : metin;
            var bulunan = NoktaOndalik.Matches(metin).Select(m => m.Value).ToList();
            Assert.True(bulunan.Count == 0,
                $"{secici} nokta ondalık içeriyor: {string.Join(" | ", bulunan)}");
        }
    }
}
