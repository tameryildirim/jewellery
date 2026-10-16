using System.Net;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// <Secici> UZAK KİPİ [bUnit] — paket P-36b (FRONTEND-KIMLIK § 6.3/6.5)
//
// P-36'da Uzak kip UYGULANMAMIŞTI (UzakTur verilince istisna atıyordu).
// Buradaki testler KayitSecici'den (P-15) taşınan mekanizmayı kilitler:
// sunucuda arama · asgari 2 karakter · 250ms gecikme · YARIŞ KOŞULU koruması ·
// hata ≠ boş durum (UX-S28) · sınır uyarısı (UX-S29) · ULID ekrana basılmaz.
//
// ⚠ Gecikme testte 0'a çekilir (BeklemeMs PARAMETRESİ) — 250ms'yi her testte
//   beklemek süiti saniyelerce uzatırdı. Gecikmenin VARSAYILANI ayrı bir
//   testle (SU8) ölçülür. Parametre statik DEĞİLDİR: statik olsaydı paralel
//   koşan sınıflar birbirinin değerini ezerdi (kararsız test).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SeciciUzakKipTestleri : BunitContext
{
    private readonly SahteArama sahte = new();

    public SeciciUzakKipTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Sorguları kaydeder; yanıtı ya da hatayı çağıran belirler.</summary>
    private sealed class SahteArama : HttpMessageHandler
    {
        public List<string> Yollar { get; } = [];
        public HttpStatusCode Kod { get; set; } = HttpStatusCode.OK;
        public int Sayi { get; set; } = 2;
        public bool DahaVar { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            Yollar.Add(istek.RequestUri!.PathAndQuery);
            if (Kod != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(Kod)
                {
                    Content = new StringContent("""{"mesaj":"sunucu kızdı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            var sonuclar = string.Join(",", Enumerable.Range(1, Sayi).Select(i =>
                $$"""{"kimlik":"01J{{i}}ULIDULIDULIDULIDULID","ad":"Ayşe Yılmaz {{i}}","altBilgi":"0555","tur":"CARI","rota":null}"""));
            var govde = $$"""
                {"sorgu":"ay","tur":"CARI","limit":10,"ofset":0,"sayi":{{Sayi}},
                 "dahaVar":{{(DahaVar ? "true" : "false")}},"tavan":100,"sonuclar":[{{sonuclar}}]}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Secici> Kur() => Render<Secici>(p => p
        .Add(c => c.ElemanId, "uzak-cari")
        .Add(c => c.Etiket, "Cari")
        .Add(c => c.UzakTur, "CARI")
        .Add(c => c.BeklemeMs, 0));

    // ── SU1) Uzak kipte arama kutusu HER ZAMAN görünür (§ 6.5) ──────────────
    [Fact]
    public void SU1_Uzak_kipte_arama_kutusu_her_zaman_gorunur()
    {
        var c = Kur();
        c.Find("#uzak-cari").Click();
        Assert.NotNull(c.Find("#uzak-cari-ara-girdi"));
    }

    // ── SU2) 2 karakterden kısa sorgu İSTEK ATMAZ ve sebebi EKRANDA yazar ───
    [Fact]
    public void SU2_Kisa_sorgu_istek_atmaz_sebebi_yazili()
    {
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("a");

        Assert.Empty(sahte.Yollar);
        Assert.NotEmpty(c.Find("#uzak-cari-asgari").TextContent.Trim());
    }

    // ── SU3) 2 karakterde sunucuda ARANIR ve sonuç listelenir ───────────────
    [Fact]
    public void SU3_Iki_karakterde_sunucuda_aranir()
    {
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");
        c.WaitForAssertion(() => Assert.NotEmpty(sahte.Yollar));

        Assert.Contains("/arama?q=ay", sahte.Yollar[0]);
        Assert.Contains("tur=CARI", sahte.Yollar[0]);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-secenek-0")));
    }

    // ── SU4) ULID EKRANA BASILMAZ; seçilince AD görünür (B-25) ──────────────
    [Fact]
    public void SU4_Secilince_ULID_degil_AD_gorunur()
    {
        string? gelen = null;
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "uzak-cari")
            .Add(x => x.Etiket, "Cari")
            .Add(x => x.UzakTur, "CARI")
            .Add(x => x.BeklemeMs, 0)
            .Add(x => x.DegerChanged, (string? v) => gelen = v));

        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-secenek-0")));
        c.Find("#uzak-cari-secenek-0").Click();

        Assert.StartsWith("01J", gelen);                       // gövdeye ULID gitti
        var metin = c.Find("#uzak-cari .secici-metin").TextContent;
        Assert.Contains("Ayşe", metin);                        // ekranda AD
        Assert.DoesNotContain("01J", metin);                   // ULID YOK
    }

    // ── SU5) HATA "kayıt yok"a ÇEVRİLMEZ (UX-S28) ──────────────────────────
    [Fact]
    public void SU5_Hata_bos_duruma_cevrilmez()
    {
        sahte.Kod = HttpStatusCode.InternalServerError;
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-uzak-hata")));
        Assert.Equal("alert", c.Find("#uzak-cari-uzak-hata").GetAttribute("role"));
        // "Eşleşme yok" boş durumu ÇIKMAZ — hata boş listeye çevrilmedi.
        Assert.Empty(c.FindAll("#uzak-cari-eslesme-yok"));
    }

    // ── SU6) Sonuç GERÇEKTEN boşsa boş durum çıkar (hata değil) ────────────
    [Fact]
    public void SU6_Gercekten_bos_sonucta_bos_durum_cikar()
    {
        sahte.Sayi = 0;
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-eslesme-yok")));
        Assert.Empty(c.FindAll("#uzak-cari-uzak-hata"));
    }

    // ── SU7) SINIR uyarısı (UX-S29): sessiz kırpma yok ─────────────────────
    [Fact]
    public void SU7_Sinira_deginca_daha_fazlasi_var_yazar()
    {
        sahte.DahaVar = true;
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-sinir")));
    }

    // ── SU8) GECİKME: bekleme süresi KayitSecici ile AYNI (250ms) ──────────
    [Fact]
    public void SU8_Varsayilan_bekleme_kayit_secici_ile_ayni()
    {
        // Varsayılan SABİTTİR; testin kendi 0'ı onu etkileyemez (parametre).
        Assert.Equal(250, Secici.VarsayilanBeklemeMs);
        Assert.Equal(KayitSecici.AramaServisiAsgari, Secici.AsgariSorgu);
    }

    // ── SU9) YARIŞ KOŞULU: aynı sorgu iki kez İSTEK ATMAZ ──────────────────
    [Fact]
    public void SU9_Ayni_sorgu_tekrar_istek_atmaz()
    {
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");
        c.WaitForAssertion(() => Assert.Single(sahte.Yollar));

        c.Render();                                   // yeniden çizim
        Assert.Single(sahte.Yollar);                  // ikinci istek YOK
    }

    // ── SU10) Kapanınca eski sonuçlar TUTULMAZ (bayat liste gösterilmez) ───
    [Fact]
    public void SU10_Kapaninca_eski_sonuc_listesi_temizlenir()
    {
        var c = Kur();
        c.Find("#uzak-cari").Click();
        c.Find("#uzak-cari-ara-girdi").Input("ay");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#uzak-cari-secenek-0")));

        c.Find("#uzak-cari-perde").Click();           // dışarı tıkla → kapat
        c.Find("#uzak-cari").Click();                 // yeniden aç

        Assert.Empty(c.FindAll("#uzak-cari-secenek-0"));
        Assert.NotNull(c.Find("#uzak-cari-asgari"));  // yeniden "en az 2 karakter"
    }
}
