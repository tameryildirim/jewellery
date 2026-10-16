using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — "Yasal sınırlar" ekranı + Panel yasal bandı (bUnit).
// K-9: bant 7 gün kala çizilir, 8 gün kala çizilmez (ufuk SUNUCUDAN).
// UX-S28 (hata ≠ boş) · ham kod basılmaz (UX-S31) · yetkisiz rolde istek yok ·
// her düğme istek atar ya da açılır/kapanır (katlanır başlığı).
// ⚠ Sahte API '{}' varsayılanı yerine GERÇEKÇİ yanıtlar verir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YasalSinirlarEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private string yaklasan = Yaklasan(7);
    private string liste = Liste;
    private int listeKodu = 200;
    private string gecmisGovde = Gecmis;

    private static string Yaklasan(int kalanGun, bool bayramEksik = false) => $$"""
        {"bugun":"2026-09-30","ufukGun":30,"bantUfkuGun":7,"yil":2026,"bayramEksik":{{(bayramEksik ? "true" : "false")}},
         "degisiklikler":[{"anahtar":"TEVSIK_HADDI_TL","aciklamaSade":"Nakitte günlük sınır","birim":"TL",
           "baslangic":"2026-10-07","kalanGun":{{kalanGun}},"yeniSayi":40000,"yeniMetin":null,
           "eskiSayi":30000,"eskiMetin":null,"teyit":"DOGRULANDI"}]}
        """;

    private const string Liste = """
        {"tarih":"2026-09-30","bugun":"2026-09-30","bayramEksik":true,"tatiller":[],
         "satirlar":[
          {"id":"a","anahtar":"TEVSIK_HADDI_TL","degerSayi":30000,"degerMetin":null,"birim":"TL",
           "baslangic":"2024-11-30","bitis":null,"dayanak":"575 s. VUK Genel Tebliği, RG 30.11.2024/32738",
           "kaynakUrl":"https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm","teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true},
          {"id":"b","anahtar":"STOPAJ_ESNAF_MUAF_HURDA_ORAN","degerSayi":0.02,"degerMetin":null,"birim":"ORAN",
           "baslangic":"2009-02-03","bitis":null,"dayanak":"GVK md.94/13","kaynakUrl":"https://www.resmigazete.gov.tr/x",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true},
          {"id":"c","anahtar":"MASAK_SIB_IS_GUNU","degerSayi":10,"degerMetin":null,"birim":"IS_GUNU",
           "baslangic":"2008-04-01","bitis":null,"dayanak":"md.28/2","kaynakUrl":"https://www.mevzuat.gov.tr/x",
           "teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true},
          {"id":"d","anahtar":"KMTS_KOD_DESENI","degerSayi":null,"degerMetin":null,"birim":"KOD",
           "baslangic":"2026-09-30","bitis":null,"dayanak":"","kaynakUrl":"https://www.mevzuat.gov.tr/",
           "teyit":"KAPALI","aciklamaSade":"x","kullaniliyor":false},
          {"id":"e","anahtar":"FATURA_HADDI_TL","degerSayi":12000,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-01-01","bitis":null,"dayanak":"VUK md.232","kaynakUrl":"https://www.resmigazete.gov.tr/y",
           "teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":false},
          {"id":"f","anahtar":"STOPAJ_BEYAN_GUNU","degerSayi":26,"degerMetin":null,"birim":"AYIN_GUNU",
           "baslangic":"2026-09-30","bitis":null,"dayanak":"","kaynakUrl":"https://cdn.gib.gov.tr/z",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":false},
          {"id":"g","anahtar":"KDV_GENEL_ORAN","degerSayi":0.20,"degerMetin":null,"birim":"ORAN",
           "baslangic":"2026-09-30","bitis":null,"dayanak":"","kaynakUrl":"https://www.mevzuat.gov.tr/k",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true}]}
        """;

    private const string Gecmis = """
        {"anahtar":"TEVSIK_HADDI_TL","satirlar":[
          {"id":"a","anahtar":"TEVSIK_HADDI_TL","degerSayi":30000,"degerMetin":null,"birim":"TL","baslangic":"2024-11-30","bitis":null,"dayanak":"d","kaynakUrl":"https://x","teyit":"DOGRULANDI","aciklamaSade":"x"},
          {"id":"b","anahtar":"TEVSIK_HADDI_TL","degerSayi":7000,"degerMetin":null,"birim":"TL","baslangic":"2016-01-01","bitis":"2024-11-30","dayanak":"d","kaynakUrl":"https://x","teyit":"DOGRULANDI","aciklamaSade":"x"}]}
        """;

    private const string GecmisKdv = """
        {"anahtar":"KDV_GENEL_ORAN","satirlar":[
          {"id":"g","anahtar":"KDV_GENEL_ORAN","degerSayi":0.20,"degerMetin":null,"birim":"ORAN","baslangic":"2026-09-30","bitis":null,"dayanak":"","kaynakUrl":"https://x","teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true}]}
        """;

    public YasalSinirlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(YasalSinirlarEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            t.istekler.Add(yol);
            var (kod, govde) = yol switch
            {
                var y when y.Contains("/yasal-parametreler/yaklasan") => (200, t.yaklasan),
                var y when y.Contains("/yasal-parametreler/gecmis?anahtar=KDV_GENEL_ORAN") => (200, GecmisKdv),
                var y when y.Contains("/yasal-parametreler/gecmis") => (200, t.gecmisGovde),
                var y when y.Contains("/yasal-parametreler") => (t.listeKodu, t.listeKodu == 200 ? t.liste
                    : """{"message":"Sunucu hatası","error":"Internal Server Error","statusCode":500}"""),
                _ => (200, "{}"),
            };
            return Task.FromResult(new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void Oturum(string rol) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    // ── Panel bandı ─────────────────────────────────────────────────────────

    [Fact]
    public void K9_bant_7_gun_kala_SAHIP_icin_CIZILIR_eski_ve_yeni_deger_tr_biciminde()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yasal-bant-tevsik-haddi-tl")));
        var metin = e.Find("#yasal-bant-tevsik-haddi-tl").TextContent;
        Assert.Contains("7 Ekim 2026 tarihinden itibaren", metin);
        Assert.Contains($"{Bicim.Tl(30_000m)} yerine {Bicim.Tl(40_000m)}", metin);
        Assert.DoesNotContain("TEVSIK", metin);
        Assert.Equal("/yasal-sinirlar", e.Find("#yasal-bant-tevsik-haddi-tl a").GetAttribute("href"));
    }

    [Fact]
    public void K9_bant_8_gun_kala_CIZILMEZ()
    {
        yaklasan = Yaklasan(8);
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        e.WaitForAssertion(() => Assert.Contains(istekler, y => y.Contains("/yaklasan")));
        Assert.Empty(e.FindAll("#yasal-bant-tevsik-haddi-tl"));
        Assert.Empty(e.FindAll("#yasal-bantlar"));
    }

    [Fact]
    public void Bant_TEZGAHTAR_istek_ATMAZ_ve_hicbir_sey_cizmez()
    {
        yaklasan = Yaklasan(1, bayramEksik: true);
        Oturum("TEZGAHTAR");
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        Assert.Empty(istekler);
        Assert.Empty(e.FindAll(".yasal-bant"));
    }

    [Fact]
    public void Tur2_bayram_eksik_SARI_BANT_Panelde_HIC_cizilmez_SAHIP_icin_de()
    {
        yaklasan = Yaklasan(3, bayramEksik: true);
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yasal-bant-tevsik-haddi-tl")));
        Assert.Empty(e.FindAll("#yasal-bant-bayram"));
        Assert.Empty(e.FindAll(".bant-uyari"));
        Assert.DoesNotContain("bayram", e.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tur2_bant_oturum_BASTA_NULL_sonra_SAHIP_olunca_istek_atar_ve_cizer()
    {
        // Doğrudan açılış / tam yenileme: bileşen çizildiğinde Kullanici henüz yok.
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        Assert.Empty(istekler);
        Assert.Empty(e.FindAll(".yasal-bant"));

        Oturum("SAHIP");   // OturumServisi.Degisti tetiklenir
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yasal-bant-tevsik-haddi-tl")));
        Assert.Single(istekler, y => y.Contains("/yasal-parametreler/yaklasan"));

        // İkinci oturum olayı (jeton tazeleme) istek ÇOĞALTMAZ.
        Oturum("SAHIP");
        Assert.Single(istekler, y => y.Contains("/yasal-parametreler/yaklasan"));
    }

    [Fact]
    public void Tur2_ekran_oturum_BASTA_NULL_sonra_MUHASEBE_olunca_yukler_yetki_notu_gostermez()
    {
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        Assert.Empty(istekler);
        Assert.Empty(e.FindAll("#ys-yetki-yok"));
        Oturum("MUHASEBE");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        Assert.Single(istekler, y => y.EndsWith("/yasal-parametreler", StringComparison.Ordinal));
    }

    // ── Ekran ───────────────────────────────────────────────────────────────

    [Fact]
    public void Ekran_SAHIP_kartlar_sade_ad_tr_deger_rozet_sozlukten_ham_kod_yok()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));

        Assert.Equal(Bicim.Tl(30_000m), e.Find("#ys-deger-tevsik-haddi-tl").TextContent.Trim());
        // Süre ve oranlar KAPALI katlanırda (zorunlu önce); açınca görünür.
        Assert.Empty(e.FindAll("#ys-deger-masak-sib-is-gunu"));
        Assert.Equal("3", e.Find("#ys-sureler-sayi").TextContent.Trim());
        e.Find("#ys-sureler-ac").Click();
        Assert.Equal("%2", e.Find("#ys-deger-stopaj-esnaf-muaf-hurda-oran").TextContent.Trim());
        Assert.Equal("10 iş günü", e.Find("#ys-deger-masak-sib-is-gunu").TextContent.Trim());
        // Sıra: nakit günlük sınır EN ÜSTTE; kuralı kapalı rakam kapalı katlanırda.
        Assert.Equal("ys-satir-tevsik-haddi-tl", e.FindAll("#ys-liste > li")[0].Id);
        Assert.Empty(e.FindAll("#ys-deger-kmts-kod-deseni"));
        // Tur 2: programın denetlemediği (FATURA_HADDI_TL, STOPAJ_BEYAN_GUNU) + KAPALI → aynı grupta.
        Assert.Equal("3", e.Find("#ys-kapali-sayi").TextContent.Trim());
        Assert.Empty(e.FindAll("#ys-deger-fatura-haddi-tl"));
        Assert.Contains("30 Kasım 2024", e.Find("#ys-baslangic-tevsik-haddi-tl").TextContent);
        // Tur 2: başlangıcı DOĞRULANMAMIŞ satır migration gününü yazmaz. YASAL-PARAM-EKRAN tur 3 (hasım DÜŞÜK):
        // "Ne zamandan beri: doğrulanmadı" satırı hiç basılmaz (teyit durumunu rozet söylüyor).
        Assert.Empty(e.FindAll("#ys-baslangic-kdv-genel-oran"));
        Assert.DoesNotContain("2026", e.Find("#ys-satir-kdv-genel-oran").TextContent);

        var govde = e.Find("#ys-liste").TextContent + e.Find("#ys-sure-liste").TextContent;
        Assert.Contains("Mali müşavir teyidi bekliyor", govde);
        Assert.Contains("Doğrulandı", govde);
        e.Find("#ys-kapali-ac").Click();
        Assert.Equal("Değer girilmedi", e.Find("#ys-deger-kmts-kod-deseni").TextContent.Trim());
        Assert.Contains("Henüz kullanılmıyor", e.Find("#ys-kapali-liste").TextContent);
        Assert.Contains("henüz denetlemiyor", e.Find("#ys-kapali-not").TextContent);
        Assert.NotNull(e.Find("#ys-deger-fatura-haddi-tl"));
        // Tur 2: ayın günü SÜRE değildir.
        var beyan = e.Find("#ys-deger-stopaj-beyan-gunu").TextContent.Trim();
        Assert.Equal("İzleyen ayın 26. günü", beyan);
        Assert.DoesNotContain("26 gün", beyan);
        foreach (var ham in new[] { "DOGRULANDI", "TEYITSIZ", "KAPALI", "TEVSIK_HADDI_TL", "IS_GUNU", "AYIN_GUNU" })
            Assert.DoesNotContain(ham, govde);

        // Tur 2: açılışta açıklama satırı ve gerekçe paragrafı YOK; değişmezlik tek satır.
        Assert.Empty(e.FindAll("#sayfa-basi-yasal-sinirlar-aciklama"));
        Assert.Empty(e.FindAll("#degismez-ipucu-YasalParametre"));
        Assert.Equal("Devletin rakamları — mevzuat değişince ByGatech günceller", e.Find("#ys-degismez-ac .katlanir-ad").TextContent.Trim());   // YASAL-PARAM-EKRAN
        e.Find("#ys-degismez-ac").Click();
        Assert.Contains("ulusal sınırlardır", e.Find("#degismez-ipucu-YasalParametre").TextContent);
        // Tur 2: bayram uyarısı açılışta bant DEĞİL — katlanır içinde, yapılacak yolla.
        Assert.Empty(e.FindAll("#ys-bayram-uyari"));
        Assert.Empty(e.FindAll(".bant-uyari"));
        e.Find("#ys-tatiller-ac").Click();
        Assert.Contains("program sağlayıcısı girer", e.Find("#ys-bayram-uyari").TextContent);
        // Dayanak ve geçmiş KAPALI gelir (zorunlu önce).
        Assert.Empty(e.FindAll("#ys-ayrinti-tevsik-haddi-tl-govde"));
        // Her düğme katlanır başlığıdır (açılır/kapanır) — ölü düğme yok.
        Assert.All(e.FindAll("button"), b => Assert.Contains("katlanir-baslik", b.ClassName));
    }

    [Fact]
    public void Ekran_gecmis_katlanir_ilk_acilista_istek_atar_ve_donemleri_yazar()
    {
        Oturum("MUHASEBE");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-ayrinti-tevsik-haddi-tl-ac")));
        Assert.DoesNotContain(istekler, y => y.Contains("/gecmis"));

        e.Find("#ys-ayrinti-tevsik-haddi-tl-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-gecmis-liste-tevsik-haddi-tl")));
        Assert.Contains(istekler, y => y.Contains("/yasal-parametreler/gecmis?anahtar=TEVSIK_HADDI_TL"));
        var metin = e.Find("#ys-gecmis-liste-tevsik-haddi-tl").TextContent;
        // Tur 2 (hasım ORTA-1): bitiş HARİÇ → ekrana SON GÜN (29 Kasım) yazılır.
        Assert.Contains($"1 Ocak 2016 – 29 Kasım 2024: {Bicim.Tl(7_000m)}", metin);
        Assert.DoesNotContain("– 30 Kasım 2024", metin);
        Assert.Contains($"30 Kasım 2024 – sürüyor: {Bicim.Tl(30_000m)}", metin);
    }

    // ── Tur 3 (orkestratör 14:50) ────────────────────────────────────────────

    private const string ListeIleri = """
        {"tarih":"2026-09-30","bugun":"2026-09-30","bayramEksik":false,"tatiller":[],
         "satirlar":[
          {"id":"a","anahtar":"TEVSIK_HADDI_TL","degerSayi":30000,"degerMetin":null,"birim":"TL",
           "baslangic":"2024-11-30","bitis":"2026-10-03","dayanak":"575 s. VUK Genel Tebliği","kaynakUrl":"https://x",
           "teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true,"esikTuru":null,"esikTutari":null},
          {"id":"k","anahtar":"MASAK_KIMLIK_ESIGI_TL","degerSayi":185000,"degerMetin":null,"birim":"TL",
           "baslangic":"2023-01-14","bitis":null,"dayanak":"md.5/1-b","kaynakUrl":"https://x",
           "teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true,"esikTuru":"VE_USTU","esikTutari":185000},
          {"id":"b","anahtar":"MASAK_BEYAN_FORMU_ESIGI_TL","degerSayi":2000000.01,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-09-30","bitis":null,"dayanak":"","kaynakUrl":"https://x",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true,"esikTuru":"USTU","esikTutari":2000000}],
         "sonrakiler":[
          {"id":"n","anahtar":"TEVSIK_HADDI_TL","degerSayi":40000,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-10-03","bitis":null,"dayanak":"d","kaynakUrl":"https://x",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true}]}
        """;

    private const string GecmisIleri = """
        {"anahtar":"TEVSIK_HADDI_TL","satirlar":[
          {"id":"n","anahtar":"TEVSIK_HADDI_TL","degerSayi":40000,"degerMetin":null,"birim":"TL","baslangic":"2026-10-03","bitis":null,"dayanak":"d","kaynakUrl":"https://x","teyit":"TEYITSIZ","aciklamaSade":"x"},
          {"id":"a","anahtar":"TEVSIK_HADDI_TL","degerSayi":30000,"degerMetin":null,"birim":"TL","baslangic":"2024-11-30","bitis":"2026-10-03","dayanak":"d","kaynakUrl":"https://x","teyit":"DOGRULANDI","aciklamaSade":"x"},
          {"id":"b","anahtar":"TEVSIK_HADDI_TL","degerSayi":7000,"degerMetin":null,"birim":"TL","baslangic":"2016-01-01","bitis":"2024-11-30","dayanak":"d","kaynakUrl":"https://x","teyit":"DOGRULANDI","aciklamaSade":"x"}]}
        """;

    [Fact]
    public void Tur3_ileri_tarihli_satir_GECMISTE_degil_kartta_yaklasan_bugunku_satir_suruyor()
    {
        liste = ListeIleri;
        gecmisGovde = GecmisIleri;
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));

        // Kartta (açılışta görünür) yaklaşan değişiklik satırı.
        var yak = e.Find("#ys-yaklasan-tevsik-haddi-tl").TextContent;
        Assert.Contains("Yaklaşan değişiklik:", yak);
        Assert.Contains($"3 Ekim 2026 tarihinden itibaren {Bicim.Tl(40_000m)}", yak);
        // Bugünkü değer değişmez; yaklaşan satırı olmayan kartta satır yok.
        Assert.Equal(Bicim.Tl(30_000m), e.Find("#ys-deger-tevsik-haddi-tl").TextContent.Trim());
        Assert.Empty(e.FindAll("#ys-yaklasan-masak-kimlik-esigi-tl"));

        e.Find("#ys-ayrinti-tevsik-haddi-tl-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-gecmis-liste-tevsik-haddi-tl")));
        var g = e.Find("#ys-gecmis-liste-tevsik-haddi-tl");
        var metin = g.TextContent;
        Assert.Equal(2, g.QuerySelectorAll("li").Length);
        Assert.DoesNotContain(Bicim.Tl(40_000m), metin);
        Assert.DoesNotContain("3 Ekim 2026", metin);
        Assert.DoesNotContain("2 Ekim 2026", metin);
        Assert.Contains($"30 Kasım 2024 – sürüyor: {Bicim.Tl(30_000m)}", metin);
        Assert.Contains($"1 Ocak 2016 – 29 Kasım 2024: {Bicim.Tl(7_000m)}", metin);
    }

    [Fact]
    public void Tur3_esik_kurussuz_uzeri_ve_ve_uzeri_yazilir_ic_karsilastirma_degeri_basilmaz()
    {
        liste = ListeIleri;
        Oturum("MUDUR");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        var beyan = e.Find("#ys-deger-masak-beyan-formu-esigi-tl").TextContent.Trim();
        Assert.Equal($"{Bicim.Tl(2_000_000m)} üzeri", beyan);
        Assert.DoesNotContain(",01", beyan);
        Assert.Equal($"{Bicim.Tl(185_000m)} ve üzeri", e.Find("#ys-deger-masak-kimlik-esigi-tl").TextContent.Trim());
        Assert.DoesNotContain(",01", e.Find("#ys-liste").TextContent);
    }

    [Fact]
    public void Tur3_bant_esik_degisikligi_kurussuz_yazar()
    {
        yaklasan = """
            {"bugun":"2026-09-30","ufukGun":30,"bantUfkuGun":7,"yil":2026,"bayramEksik":false,
             "degisiklikler":[{"anahtar":"MASAK_BEYAN_FORMU_ESIGI_TL","aciklamaSade":"x","birim":"TL",
               "baslangic":"2026-10-03","kalanGun":3,"yeniSayi":3000000.01,"yeniMetin":null,
               "eskiSayi":2000000.01,"eskiMetin":null,"teyit":"TEYITSIZ",
               "esikTuru":"USTU","yeniEsik":3000000,"eskiEsik":2000000,"eskiEsikTuru":"USTU"}]}
            """;
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.YasalDegisiklikBandi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yasal-bant-masak-beyan-formu-esigi-tl")));
        var m = e.Find("#yasal-bant-masak-beyan-formu-esigi-tl").TextContent;
        Assert.Contains($"{Bicim.Tl(2_000_000m)} üzeri yerine {Bicim.Tl(3_000_000m)} üzeri", m);
        Assert.DoesNotContain(",01", m);
    }

    [Fact]
    public void Tur2_dayanagi_bos_satir_ic_not_yerine_sade_cumle_yazar()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        e.Find("#ys-sureler-ac").Click();
        e.Find("#ys-ayrinti-kdv-genel-oran-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-dayanak-kdv-genel-oran")));
        Assert.Equal("Resmî dayanağı henüz doğrulanmadı.", e.Find("#ys-dayanak-kdv-genel-oran").TextContent.Trim());
        // Başlangıcı doğrulanmamış satırın GEÇMİŞİ de migration gününü basmaz.
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-gecmis-liste-kdv-genel-oran")));
        var g = e.Find("#ys-gecmis-liste-kdv-genel-oran").TextContent;
        Assert.Contains("Sürüyor: %20 (başlangıç tarihi doğrulanmadı)", g);
        Assert.DoesNotContain("2026", g);
    }

    [Fact]
    public void Ekran_TEZGAHTAR_istek_atmaz_sade_not_ham_rol_kodu_yok()
    {
        Oturum("TEZGAHTAR");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        Assert.Empty(istekler);
        var not = e.Find("#ys-yetki-yok").TextContent;
        Assert.Contains("işletme sahibi, müdür ve muhasebe", not);
        Assert.DoesNotContain("SAHIP", e.Markup);
    }

    [Fact]
    public void Ekran_hata_BOS_DURUMA_cevrilmez_hata_bandi_cizilir()
    {
        listeKodu = 500;
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.Contains("Yasal sınırlar yüklenemedi", e.Find("#ys-hata-bandi").TextContent));
        Assert.Empty(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll("#ys-liste"));
    }

    [Fact]
    public void Ekran_basarili_ve_bos_liste_BOS_DURUM_cizer()
    {
        liste = """{"tarih":"2026-09-30","bugun":"2026-09-30","bayramEksik":false,"tatiller":[],"satirlar":[]}""";
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".bos-durum")));
        Assert.Contains("Tanımlı yasal sınır bulunmuyor.", e.Find(".bos-durum").TextContent);
    }

    /// <summary>
    /// PAKET YASAL-3 tur 2 (§ 4.6 T-1): MASAK kimlik eşiği kartında "bağlantılı işlem = aynı gün
    /// aynı müşteri" okumasının teyit beklediği yazılır (tek sade cümle, mevzuat kodu yok);
    /// başka kartta bu not yok.
    /// </summary>
    [Fact]
    public void Yasal3_T1_yorum_notu_yalniz_MASAK_kimlik_esigi_kartinda()
    {
        liste = ListeIleri;
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        // YASAL-PARAM-EKRAN (MASAK-370 hakemi DÜŞÜK): not "Doğrulandı" rozetinin yanında DEĞİL —
        // kapalı "Dayanak ve geçmiş" katlanırında; açılınca görünür.
        Assert.Empty(e.FindAll("#ys-yorum-masak-kimlik-esigi-tl"));
        e.Find("#ys-ayrinti-masak-kimlik-esigi-tl-ac").Click();
        var not = e.Find("#ys-yorum-masak-kimlik-esigi-tl").TextContent.Trim();
        // YASAL-PARAM-EKRAN tur 3 (hasım DÜŞÜK): "okuma" iç terimi yok — "Toplama kuralı: mali müşavir teyidi bekleniyor".
        Assert.Equal(Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.ToplamaKurali, not);
        Assert.Contains("Toplama kuralı: mali müşavir teyidi bekleniyor", not);
        Assert.DoesNotContain("okuma", not);
        Assert.DoesNotContain("md.", not, StringComparison.Ordinal);
        Assert.Single(e.FindAll("[id^='ys-yorum-']"));
    }
}
