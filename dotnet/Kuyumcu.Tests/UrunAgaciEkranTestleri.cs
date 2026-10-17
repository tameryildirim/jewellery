using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN AĞACI VE REYON EKRANI
//
// Çok seviyeli kategori ağacı ve reyon API'de vardı, GİRİŞİ YOKTU: kullanıcı
// ne dal açabiliyor ne malın hangi vitrinde durduğunu söyleyebiliyordu.
//
// ⚠ DÖNGÜ: bir kategori kendi alt ağacına taşınırsa ağaç yürüyüşü sonsuz
// döngüye girer ve ürün listesi isteği sunucuyu kilitler. Sunucu bunu reddeder;
// ekran adayı listeden hiç göstermeyerek kullanıcıyı oraya sokmaz. Aşağıdaki
// test bu ikinci kapıyı sınar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunAgaciEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Altın › Bilezik › Burma  — üç seviye, döngü sınavı için gerekli
    private const string Agac = """
        [{"id":"k1","ad":"Altın","sira":0,"aktif":true,"urunSayisi":5,"altlar":[
            {"id":"k2","ad":"Bilezik","sira":0,"aktif":true,"urunSayisi":3,"altlar":[
                {"id":"k3","ad":"Burma","sira":0,"aktif":true,"urunSayisi":1,"altlar":[]}]}]},
         {"id":"k9","ad":"Gümüş","sira":1,"aktif":false,"urunSayisi":0,"altlar":[]}]
        """;

    private const string Reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"Ön Vitrin","tip":"VITRIN","aktif":true,"urunSayisi":3,"yerlesikAdet":7}]
        """;

    public UrunAgaciEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.Contains("/urun-kategorileri") => Agac,
                var y when y.Contains("/reyonlar") => Reyonlar,
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunAgaci> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        istekler.Clear();
        return e;
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static AngleSharp.Dom.IElement Dugme(
        IRenderedComponent<Kuyumcu.Web.Pages.UrunAgaci> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    // ── Yükleme ──────────────────────────────────────────────────────────────

    [Fact]
    public void Acilista_agac_reyon_ve_subeler_cekilir()
    {
        Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();

        Assert.Contains(istekler, i => i.Yol.Contains("/urun-kategorileri"));
        Assert.Contains(istekler, i => i.Yol.Contains("/reyonlar"));
        Assert.Contains(istekler, i => i.Yol.Contains("/subeler"));
    }

    [Fact]
    public void PASIF_kategoriler_de_cekilir()
    {
        // sadeceAktif=false olmazsa, yanlışlıkla pasife alınan bir dal ekrandan
        // tümüyle kaybolur ve kullanıcı onu bir daha geri açamazdı.
        Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();

        Assert.Contains(istekler, i => i.Yol.Contains("/urun-kategorileri?sadeceAktif=false"));
    }

    [Fact]
    public void Agac_ic_ice_cizilir_ve_pasif_dal_isaretlenir()
    {
        var e = Ciz();

        Assert.Contains("Burma", e.Markup);      // 3. seviye görünür
        Assert.Contains("Pasif", e.Markup);      // k9 pasif rozeti
    }

    // ── Kategori oluşturma ───────────────────────────────────────────────────

    [Fact]
    public void Yeni_kategori_POST_ile_kaydedilir()
    {
        var e = Ciz();
        Dugme(e, "Yeni kategori").Click();
        e.Find("#k-ad").Input("Kolye");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/urun-kategorileri", istek.Yol);
        Assert.Equal("Kolye", Alan(istek.Govde, "ad"));
        Assert.Null(Alan(istek.Govde, "ustId"));   // kök seviye
    }

    [Fact]
    public void Dal_altina_ekleyince_UST_ID_dolu_gider()
    {
        var e = Ciz();
        // "Altın" satırındaki "+ Yeni Kategori" düğmesi
        e.FindAll("button").Where(b => b.TextContent.Contains("Yeni kategori")).Skip(1).First().Click();
        e.Find("#k-ad").Input("Külçe");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.Equal("k1", Alan(istek.Govde, "ustId"));
    }

    // ── Kategori düzenleme ───────────────────────────────────────────────────

    [Fact]
    public void Duzenlemede_mevcut_UST_secili_gelir_ve_PATCH_gonderilir()
    {
        var e = Ciz();
        // "Bilezik" satırındaki Düzenle (ağaç sırası: Altın, Bilezik, Burma, Gümüş)
        e.FindAll("button").Where(b => b.TextContent.Contains("Düzenle")).Skip(1).First().Click();
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/urun-kategorileri/k2", istek.Yol);
        Assert.Equal("Bilezik", Alan(istek.Govde, "ad"));
        Assert.Equal("k1", Alan(istek.Govde, "ustId"));   // mevcut üst korunur
    }

    [Fact]
    public void Kategori_KENDI_ALT_AGACINA_ust_olarak_secilemez()
    {
        // Döngü koruması. "Bilezik" düzenlenirken üst adayları arasında
        // ne kendisi ne de torunu "Burma" bulunabilir.
        var e = Ciz();
        e.FindAll("button").Where(b => b.TextContent.Contains("Düzenle")).Skip(1).First().Click();

        var degerler = SeciciYardim.Secenekler(e, "k-ust")
            .Select(o => o.Deger()).ToList();
        Assert.DoesNotContain("k2", degerler);   // kendisi
        Assert.DoesNotContain("k3", degerler);   // torunu
        Assert.Contains("k1", degerler);         // kardeş/ata seçilebilir
        Assert.Contains("k9", degerler);
    }

    // ── Reyon ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reyon_listesi_tipi_ve_urun_sayisiyla_gosterilir()
    {
        var e = Ciz();

        Assert.Contains("Ön Vitrin", e.Markup);
        Assert.Contains("Vitrin", e.Markup);
    }

    [Fact]
    public void Yeni_reyon_POST_ile_kaydedilir()
    {
        var e = Ciz();
        Dugme(e, "Yeni reyon").Click();
        e.Find("#r-ad").Input("Arka Kasa");
        SeciciYardim.Sec(e, "r-tip", "KASA");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/reyonlar", istek.Yol);
        Assert.Equal("Arka Kasa", Alan(istek.Govde, "ad"));
        Assert.Equal("KASA", Alan(istek.Govde, "tip"));
        Assert.Equal("s1", Alan(istek.Govde, "subeId"));   // tek şube varsa seçili gelir
    }

    [Fact]
    public void Kapsam_sinirinin_YENI_gercegi_ekranda_yazar()
    {
        // ⚠ PAKET 25 Faz A — METİN SİLİNMEDİ, DARALTILDI (Bertan koşulu).
        // Eski gerçek: "reyon ürün kartına bağlıdır, stok satırına değil."
        // Yeni gerçek: reyon artık stok ADEDİNİ de bölüyor (yerleşik adet),
        // AMA sayım kapsamı hâlâ ürün kartından okunuyor (Faz B'ye kaldı).
        // Yarı doğru kalan uyarı, yalan söyleyen uyarıdır.
        // ORTAK-1 hakem turu 29.09 (tasarım hakemi): HUB'daki geliştirici değişiklik notu ("Reyon artık …
        // SAYIM KAPSAMI hâlâ …") ekrandan kalktı. AYNI GERÇEK PRO'da tek sade cümleyle (#agac-reyon-sayim-notu)
        // söylenir: sayım, varsayılan reyonu bu reyon olan ürünleri (ürün kartı) sayar, reyondaki fiilî malı değil.
        var e = Ciz();

        var not = e.Find("#agac-reyon-sayim-notu").TextContent;
        Assert.Contains(Kuyumcu.Core.I18n.OrtakGorunumSozlugu.Tr.UrunAgaciReyonSayimNotu, not);
        Assert.Contains("varsayılan reyonu", not);
        Assert.Contains("fiilen duran malı değil", not);
        Assert.DoesNotContain("SAYIM KAPSAMI", e.Markup);
        Assert.DoesNotContain("Reyon ürün KARTINA bağlıdır, şube stok satırına değil", e.Markup);
    }
}
