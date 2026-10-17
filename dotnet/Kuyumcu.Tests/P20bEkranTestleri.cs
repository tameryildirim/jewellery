using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20b (K-13) — /hurda EKRANININ TAHLİL FARKI DİLİMİ [bUnit]
//
// UX-S21 fark TÜRETİLİR (boş alan olarak sorulmaz) ve yönü yazılır.
// UX-S24 bağımlı alanın bağını EKRAN kurar: gerekçe yalnız fark varken açılır.
// UX-S25 oluşturulan kayıt DÜZELTİLEBİLİR — defterde "Düzelt" düğmesi vardır.
// UX-S27 sunucunun döndürdüğü FARK banda yazılır.
// UX-S30 kilitli Kaydet'in sebebi (gerekçe / sıfır fark) ekranda yazar.
// UX-S31 ham tür kodu (TAHLIL_FARKI) ekrana basılmaz, sözlükten çözülür.
// DoD EK 1/2 çok alanlı düzenleme ÇEKMECEDE yapılır (ModalPencere değil).
// DoD EK 1/3 gerekçesiz gönderimde alan altı Türkçe mesaj çıkar, POST ATILMAZ.
// K-13/1 ekranda "fire" adıyla bir GİRDİ ALANI YOKTUR (yalnız gerekçe notu).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P20bEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private string satisYaniti = """
        {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":4.8,
         "birimFiyatTlHasGram":6800,"toplamTl":32640,"tahsilEdilenTl":32640,"kalanTl":0,
         "kalanHurdaHasGram":4.16,"kasaBakiyesi":284000,"paraBirimi":"TRY",
         "tarih":"2026-09-21T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,
         "bizimKaydimizHasGram":5,"aliciKabulHasGram":4.8,"tahlilFarkiHasGram":-0.2,
         "tahlilFarkiHareketId":"f1"}
        """;

    public P20bEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => satisYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> satis) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            // ⚠ "{}" varsayılanı YETMEZ: yeni uçlara GERÇEKÇİ yanıt verilir,
            //   yoksa ekran boş modelle yeşil kalır ve sözleşme kayması görünmez.
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") => """
                    {"subeId":"s1",
                     "satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0}],
                     "fireKaydiAcik":false,
                     "fireNotu":"Bu sürümde eritme firesi kaydedilmez; imalat kapsam dışıdır (K-13 kararı)."}
                    """,
                var y when y.EndsWith("/hurda/defter") => """
                    {"satirlar":[
                       {"id":"h1","tur":"HURDA_ALIS","tarih":"2026-09-20T08:00:00Z","tlTutari":-25648,
                        "hasGrami":9.16,"maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,
                        "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false,
                        "tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0},
                       {"id":"h8","tur":"HURDA_SATIS","tarih":"2026-09-21T08:00:00Z","tlTutari":13920,
                        "hasGrami":-4.8,"maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,
                        "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false,
                        "tahlilKaynakHareketId":null,"tahlilFarkiHasGram":-0.2},
                       {"id":"f0","tur":"TAHLIL_FARKI","tarih":"2026-09-21T08:00:00Z","tlTutari":0,
                        "hasGrami":-0.2,"maden":"ALTIN","madenAdi":"Altın","cariId":null,
                        "aciklama":"Rafineri tahlil raporu 2026/144.",
                        "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false,
                        "tahlilKaynakHareketId":"h8","tahlilFarkiHasGram":0}],
                     "toplamAdet":3,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
                    """,
                var y when y.EndsWith("/islemler/hurda-satis") => satis(),
                var y when y.Contains("/tahlil-duzeltme") => """
                    {"hurdaSatisId":"h8","duzeltmeHareketId":"f2","maden":"ALTIN","madenAdi":"Altın",
                     "ekFarkHasGram":-0.05,"toplamTahlilFarkiHasGram":-0.25,
                     "kalanHurdaHasGram":4.11,"tarih":"2026-09-21T10:00:00Z"}
                    """,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Anadolu Rafineri"}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-21T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/defter")));
        // ZO-ISLEM-A: isteğe bağlı alanlar "+ Detay ekle", defter kapalı akordeon
        // arkasında. Bu dosyanın testleri o alanların KURALLARINI sınar; açılış
        // davranışı ZOISLEMAEkranTestleri'nde ayrıca sınanır.
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        return e;
    }

    private static void FormuDoldur(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e,
        string bizim, string? kabul = null)
    {
        e.Find("#hrd-has").Input(bizim);
        if (kabul is not null) e.Find("#hrd-kabul-has").Input(kabul);
    }

    // ── UX-S21: iki has ayrı durur, fark TÜRETİLİR ve YÖNÜ yazılır ──────────

    [Fact]
    public void Iki_has_alani_AYRI_durur_ve_fark_yonuyle_gorunur()
    {
        var e = Ekran();
        Assert.Single(e.FindAll("#hrd-has"));
        Assert.Single(e.FindAll("#hrd-kabul-has"));

        FormuDoldur(e, "5", "4.8");
        e.WaitForAssertion(() =>
        {
            var metin = e.Find("#hrd-tahlil-fark").TextContent;
            Assert.Contains("Tahlil", metin);
            Assert.Contains("Aleyhimize", metin);
        });

        // LEHTE yön de yazılır.
        e.Find("#hrd-kabul-has").Input("5.1");
        e.WaitForAssertion(() => Assert.Contains("Lehimize", e.Find("#hrd-tahlil-fark").TextContent));
    }

    [Fact]
    public void Fark_yokken_FARK_YOK_yazar_ve_gerekce_alani_ACILMAZ()
    {
        var e = Ekran();
        FormuDoldur(e, "5");
        e.WaitForAssertion(() => Assert.Contains("Fark yok", e.Find("#hrd-tahlil-fark").TextContent));
        Assert.Empty(e.FindAll("#hrd-tahlil-gerekce"));
    }

    // ── UX-S24 + DoD EK 1/3: gerekçe fark varken açılır, boşsa POST ATILMAZ ──

    [Fact]
    public void Fark_varken_gerekcesiz_kaydet_POST_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = Ekran();
        FormuDoldur(e, "5", "4.8");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-tahlil-gerekce")));

        e.Find("#hrd-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
        e.WaitForAssertion(() =>
            Assert.Contains("20 karakter", e.Find("#alan-hata-hrd-tahlil-gerekce").TextContent));
    }

    [Fact]
    public void Gerekce_dolunca_POST_gider_ve_govdede_kabul_ile_gerekce_bulunur()
    {
        var e = Ekran();
        FormuDoldur(e, "5", "4.8");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-tahlil-gerekce")));
        e.Find("#hrd-tahlil-gerekce").Input("Rafineri tahlil raporu 2026/144 ile mutabakat farkı.");

        e.Find("#hrd-kaydet").Click();
        // Q10-2: geri alınamaz kayıt önce onaya gider; POST onaydan sonra.
        e.WaitForElement("#hrd-onay-onayla").Click();

        // Kararsızlık kapısı: POST asenkron gider, okumadan ÖNCE varlığı beklenir.
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")));
        var istek = istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
        Assert.Contains("\"aliciKabulHasGrami\":4.8", istek.Govde);
        Assert.Contains("tahlilFarkiGerekcesi", istek.Govde);

        // UX-S27: sunucunun DÖNDÜRDÜĞÜ fark banda yazılır.
        e.WaitForAssertion(() =>
        {
            var bant = e.Find("#hrd-basari").TextContent;
            Assert.Contains("Tahlil", bant);
            Assert.Contains("Aleyhimize", bant);
        });
    }

    // ── UX-S25 + DoD EK 1/2: düzeltme ÇEKMECEDE yapılır ─────────────────────

    [Fact]
    public void Defterde_duzelt_dugmesi_CEKMECEYI_acar_ve_kaydet_POST_atar()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-duzelt-h8")));
        // Fark satırının kendisinde düzelt düğmesi YOKTUR (düzeltme satışa yazılır).
        Assert.Empty(e.FindAll("#hrd-duzelt-f0"));

        e.Find("#hrd-duzelt-h8").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#cekmece-tahlilfarki")));

        // UX-S30: eksik koşulda Kaydet kilitli ve SEBEBİ yazılı.
        Assert.Contains("sıfır olamaz", e.Find("#cekmece-tahlilfarki-kilit").TextContent);

        e.Find("#hrd-ek-fark").Input("-0.05");
        e.Find("#hrd-duzelt-gerekce").Input("İkinci tahlil raporu geldi: eksilme 0,25 has.");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-tahlilfarki-kilit")));

        e.Find("#cekmece-tahlilfarki-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
        var istek = istekler.Last(i => i.Metot == "POST");
        Assert.Contains("/islemler/hurda-satis/h8/tahlil-duzeltme", istek.Yol);
        Assert.Contains("ekFarkHasGrami", istek.Govde);
        e.WaitForAssertion(() =>
            Assert.Contains("düzeltmesi kaydedildi", e.Find("#hrd-basari").TextContent));
    }

    // ── UX-S31 + K-13/1 ─────────────────────────────────────────────────────

    [Fact]
    public void Defterde_ham_tur_kodu_BASILMAZ_sozlukten_cozulur()
    {
        var e = Ekran();
        // ZO-ISLEM-A 28.09 7. tur: defter verisi eşzamansız gelir; tam süit yükünde Find yarışıyordu.
        var tablo = e.WaitForElement("#hrd-defter").TextContent;
        Assert.Contains("Tahlil farkı", tablo);
        Assert.DoesNotContain("TAHLIL_FARKI", tablo);
        Assert.DoesNotContain("HURDA_SATIS", tablo);
    }

    [Fact]
    public void Ekranda_FIRE_adiyla_bir_GIRDI_alani_YOKTUR_gelistirici_notu_da_YOKTUR()
    {
        var e = Ekran();
        foreach (var g in e.FindAll("input, select, textarea"))
            Assert.DoesNotContain("fire", (g.Id ?? "").ToLowerInvariant());

        // ZO-ISLEM-A 28.09 (tasarım hakemi): K-13 gerekçesi sunucuda durur; ekran onu
        // BASMAZ ("imalat kapsam dışıdır … K-13 kararı" geliştirici notuydu).
        Assert.Empty(e.FindAll("#hrd-fire-notu"));
        Assert.DoesNotContain("imalat kapsam dışıdır", e.Markup);
    }

    // ── DoD EK 1/1 — ölü düğme yok (çekmece açıkken de) ─────────────────────

    [Fact]
    public void Cekmece_acikken_de_her_dugme_ya_istek_atar_ya_DISABLED()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-duzelt-h8")));
        e.Find("#hrd-duzelt-h8").Click();

        foreach (var d in e.FindAll("button"))
        {
            var kilitli = d.HasAttribute("disabled");
            var tiklanabilir = d.Attributes.Any(a => a.Name.Contains("onclick", StringComparison.OrdinalIgnoreCase));
            Assert.True(kilitli || tiklanabilir, $"Ölü düğme: #{d.Id}");
        }
    }
}
