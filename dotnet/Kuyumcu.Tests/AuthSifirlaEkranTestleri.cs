using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Kisit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET AUTH-SIFIRLA — /sifre-sifirla ekranı + giriş ekranındaki bağlantı (bUnit).
//
// Sınanan sözleşme:
//   • Kanal kapalıysa (durum.acik=false ya da istek 503) form ÇİZİLMEZ, doğru yol
//     yazılır, "gönderildi" bandı ÇIKMAZ (madde 6).
//   • Durum okunamazsa ayrı hata metni (UX-S28) — "kapalı" gibi görünmez.
//   • Boş/geçersiz gönderimde alan altı mesaj, istek GİTMEZ (DoD EK md.3).
//   • İstek sonucu SABİT metin (madde 1), gövde ayrıştırılarak sınanır.
//   • #jeton parçası → yeni parola adımı; durum SORULMAZ; tamamla gövdesi doğru;
//     red → "Yeni bağlantı iste" yolu; başarı → girişe dön (oturum açılmaz).
//   • Her düğme iş yapar (DoD EK md.1), tek birincil düğme.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AuthSifirlaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kanalAcik = true;
    private HttpStatusCode durumKodu = HttpStatusCode.OK;
    private HttpStatusCode istekKodu = HttpStatusCode.OK;
    private HttpStatusCode tamamlaKodu = HttpStatusCode.OK;
    private string tamamlaHataGovdesi =
        """{"statusCode":400,"message":"Bu bağlantı geçersiz, kullanılmış ya da süresi dolmuş. Giriş ekranından yeni bir bağlantı isteyin."}""";

    private sealed class SahteApi(AuthSifirlaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/auth/parola-sifirlama/durum", StringComparison.Ordinal))
                return t.durumKodu != HttpStatusCode.OK
                    ? Yanit(t.durumKodu, """{"statusCode":500,"message":"Sunucu hatası"}""")
                    : Yanit(HttpStatusCode.OK, t.kanalAcik
                        ? """{"acik":true,"mesaj":null,"gecerlilikDakika":30}"""
                        : """{"acik":false,"mesaj":"Parola sıfırlama e-postası bu kurulumda açık değil.","gecerlilikDakika":30}""");
            if (yol.EndsWith("/auth/parola-sifirlama/istek", StringComparison.Ordinal))
                return t.istekKodu switch
                {
                    HttpStatusCode.OK => Yanit(HttpStatusCode.OK,
                        """{"mesaj":"Bu e-posta adresi kayıtlıysa parola sıfırlama bağlantısı gönderildi."}"""),
                    HttpStatusCode.ServiceUnavailable => Yanit(HttpStatusCode.ServiceUnavailable,
                        """{"statusCode":503,"message":"Parola sıfırlama e-postası bu kurulumda açık değil."}"""),
                    HttpStatusCode.TooManyRequests => CokIstek(),
                    _ => Yanit(t.istekKodu, """{"statusCode":400,"message":"Geçerli bir e-posta adresi giriniz","alanlar":{"email":["Geçerli bir e-posta adresi giriniz"]}}"""),
                };
            if (yol.EndsWith("/auth/parola-sifirlama/tamamla", StringComparison.Ordinal))
                return t.tamamlaKodu == HttpStatusCode.OK
                    ? Yanit(HttpStatusCode.OK, """{"mesaj":"Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz."}""")
                    : Yanit(t.tamamlaKodu, t.tamamlaHataGovdesi);
            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
                return Yanit(HttpStatusCode.OK, JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return Yanit(HttpStatusCode.NotFound, """{"hata":"beklenmeyen uç"}""");
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };

        /// <summary>Sunucunun gerçek 429 yanıtı: Retry-After 900 sn (15 dk penceresi).</summary>
        private static HttpResponseMessage CokIstek()
        {
            var y = Yanit(HttpStatusCode.TooManyRequests,
                """{"statusCode":429,"message":"Çok fazla parola sıfırlama isteği. Lütfen daha sonra tekrar deneyin."}""");
            y.Headers.TryAddWithoutValidation("Retry-After", "900");
            return y;
        }
    }

    public AuthSifirlaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();
    private static readonly AuthSifirlaMetinleri M = AuthSifirlaSozlugu.Tr;

    private IRenderedComponent<SifreSifirla> Ciz(string adres = "/sifre-sifirla")
    {
        Nav.NavigateTo(adres);
        var e = Render<SifreSifirla>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#sifirla-yukleniyor")));
        return e;
    }

    // ── Kapalı kanal (madde 6) ───────────────────────────────────────────────

    [Fact]
    public void Kanal_kapaliysa_form_YOK_dogru_yol_yazili()
    {
        kanalAcik = false;
        var e = Ciz();
        Assert.Empty(e.FindAll("#sifirla-istek-form"));
        Assert.Empty(e.FindAll("#sifirla-gonder"));
        Assert.Equal(M.KapaliMesaj, e.Find("#sifirla-kapali-mesaj").TextContent.Trim());
        Assert.Contains("Kullanıcılar", e.Find("#sifirla-kapali-yonetici").TextContent);
        Assert.Equal("/giris", e.Find("#sifirla-girise-don").GetAttribute("href"));
        Assert.Empty(e.FindAll("#sifirla-basari"));
    }

    [Fact]
    public void Istek_503_donerse_KAPALI_gorunur_gonderildi_DEMEZ()
    {
        istekKodu = HttpStatusCode.ServiceUnavailable;
        var e = Ciz();
        e.Find("#sifirla-eposta").Input("patron@dukkan.com");
        e.Find("#sifirla-istek-form").Submit();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sifirla-kapali")));
        Assert.Empty(e.FindAll("#sifirla-basari"));
        Assert.Empty(e.FindAll("#sifirla-istek-form"));
    }

    [Fact]
    public void Durum_okunamazsa_AYRI_hata_metni_kapali_gibi_gorunmez()
    {
        durumKodu = HttpStatusCode.InternalServerError;
        var e = Ciz();
        Assert.StartsWith(M.DurumHatasi, e.Find("#sifirla-durum-hata").TextContent.Trim());
        Assert.Empty(e.FindAll("#sifirla-kapali"));
        Assert.Empty(e.FindAll("#sifirla-istek-form"));
    }

    // ── İstek adımı (madde 1) ────────────────────────────────────────────────

    [Fact]
    public void Bos_ve_gecersiz_eposta_ALAN_ALTI_mesaj_istek_gitmez()
    {
        var e = Ciz();
        istekler.Clear();
        e.Find("#sifirla-istek-form").Submit();
        Assert.NotNull(e.Find("#sifirla-hata"));
        Assert.NotEmpty(e.FindAll("[id^='sifirla-eposta-hata'], .alan-hata"));
        e.Find("#sifirla-eposta").Input("eposta-degil");
        e.Find("#sifirla-istek-form").Submit();
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/istek"));
    }

    [Fact]
    public void Gecerli_eposta_POST_atar_govde_ayristirilir_SABIT_sonuc_metni()
    {
        var e = Ciz();
        e.Find("#sifirla-eposta").Input("  patron@dukkan.com ");
        e.Find("#sifirla-istek-form").Submit();
        e.WaitForAssertion(() => Assert.Equal(M.IstekBasari, e.Find("#sifirla-basari").TextContent.Trim()));
        var istek = istekler.Single(i => i.Yol.EndsWith("/auth/parola-sifirlama/istek"));
        Assert.Equal("POST", istek.Metot);
        using var g = JsonDocument.Parse(istek.Govde);
        Assert.Equal("patron@dukkan.com", g.RootElement.GetProperty("email").GetString());
        Assert.Single(g.RootElement.EnumerateObject());
        Assert.Empty(e.FindAll("#sifirla-istek-form"));   // ikinci gönderim yok
    }

    [Fact]
    public void Istek_adiminda_TEK_birincil_dugme_ve_her_dugme_is_yapar()
    {
        var e = Ciz();
        Assert.Single(e.FindAll(".btn-birincil"));
        var dugmeler = e.FindAll("#sifirla-kart button").Where(b => b.Closest(".dil-secici") is null)
            .Select(b => b.Id).ToHashSet();
        Assert.Equal(new HashSet<string?> { "sifirla-gonder" }, dugmeler);
    }

    // ── Yeni parola adımı (madde 2, 4, 5) ────────────────────────────────────

    [Fact]
    public void Jeton_parcasi_YENI_PAROLA_adimini_acar_durum_sorulmaz()
    {
        var e = Ciz("/sifre-sifirla#jeton=AbC_123-xyz");
        Assert.NotNull(e.Find("#sifirla-yeni-form"));
        Assert.Equal(M.YeniBaslik, e.Find("#sayfa-basi-sifirla-baslik").TextContent.Trim());
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/durum"));
        Assert.Single(e.FindAll(".btn-birincil"));
    }

    [Fact]
    public void Acik_sekmede_YALNIZ_parca_degisince_de_yeni_parola_adimi_acilir()
    {
        var e = Ciz();
        Assert.NotNull(e.Find("#sifirla-istek-form"));
        Nav.NavigateTo("/sifre-sifirla#jeton=Taze_123");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sifirla-yeni-form")));
        Assert.Empty(e.FindAll("#sifirla-istek-form"));
    }

    [Fact]
    public void Sayfa_basligi_UST_BAR_adi_ile_AYNI_degil_gorunur_kalir()
    {
        // Kabuksuz sayfada üst bar yok: şerit başlığı rota adıyla aynı olsaydı
        // SayfaBasi onu görsel gizlerdi (canlı ölçümde yakalandı).
        var e = Ciz();
        Assert.DoesNotContain("sayfa-basi--baslik-ustte", e.Find("#sayfa-basi-sifirla").ClassName ?? "");
        var e2 = Ciz("/sifre-sifirla#jeton=abc");
        Assert.DoesNotContain("sayfa-basi--baslik-ustte", e2.Find("#sayfa-basi-sifirla").ClassName ?? "");
    }

    [Fact]
    public void Yeni_parola_kurala_uymaz_ya_da_tekrar_tutmazsa_ALAN_mesaji_istek_gitmez()
    {
        var e = Ciz("/sifre-sifirla#jeton=abc");
        e.Find("#sifirla-yeni-sifre").Input("kisa");
        e.Find("#sifirla-yeni-tekrar").Input("kisa");
        e.Find("#sifirla-yeni-form").Submit();
        Assert.Contains(M.ParolaKurali, e.Find("#sifirla-kart").TextContent);

        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola457");
        e.Find("#sifirla-yeni-form").Submit();
        Assert.Contains(M.ParolalarAyniDegil, e.Find("#sifirla-kart").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/tamamla"));
    }

    [Fact]
    public void Tamamla_govdesi_DOGRU_basari_bandi_ve_girise_don_oturum_acilmaz()
    {
        var e = Ciz("/sifre-sifirla#jeton=AbC_123-xyz");
        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola456");
        e.Find("#sifirla-yeni-form").Submit();
        e.WaitForAssertion(() => Assert.Equal(M.TamamBasari, e.Find("#sifirla-basari").TextContent.Trim()));

        var istek = istekler.Single(i => i.Yol.EndsWith("/auth/parola-sifirlama/tamamla"));
        using var g = JsonDocument.Parse(istek.Govde);
        Assert.Equal("AbC_123-xyz", g.RootElement.GetProperty("jeton").GetString());
        Assert.Equal("YeniParola456", g.RootElement.GetProperty("yeniSifre").GetString());
        Assert.Equal("YeniParola456", g.RootElement.GetProperty("yeniSifreTekrar").GetString());
        Assert.Empty(e.FindAll("#sifirla-yeni-form"));
        Assert.Equal("/giris", e.Find("#sifirla-girise-don").GetAttribute("href"));
        Assert.False(Services.GetRequiredService<OturumServisi>().GirisYapildi);
    }

    [Fact]
    public void Jeton_reddedilirse_HATA_bandi_ve_YENI_BAGLANTI_yolu()
    {
        tamamlaKodu = HttpStatusCode.BadRequest;
        var e = Ciz("/sifre-sifirla#jeton=eski");
        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola456");
        e.Find("#sifirla-yeni-form").Submit();
        // Tur 2 (tasarım hakemi): bant AYNI ekrandaki eylemi gösterir; işe yaramayan form KALMAZ;
        // tek birincil düğme "Yeni bağlantı iste".
        e.WaitForAssertion(() => Assert.Equal(M.BaglantiGecersiz, e.Find("#sifirla-hata").TextContent.Trim()));
        Assert.DoesNotContain("Giriş ekranından", e.Find("#sifirla-kart").TextContent);
        Assert.Empty(e.FindAll("#sifirla-yeni-form"));
        Assert.Empty(e.FindAll("#sifirla-kaydet"));
        var birincil = Assert.Single(e.FindAll(".btn-birincil"));
        Assert.Equal("sifirla-yeni-baglanti", birincil.Id);
        Assert.Equal(M.YeniBaglantiIste, birincil.TextContent.Trim());
        birincil.Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sifirla-istek-form")));
        Assert.EndsWith("/sifre-sifirla", Nav.Uri);
    }

    [Fact]
    public void Jeton_okununca_PARCA_adres_cubugundan_silinir_jeton_bellekte_kalir()
    {
        var e = Ciz("/sifre-sifirla#jeton=Gizli_Jeton-123");
        Assert.DoesNotContain("#jeton", Nav.Uri);
        Assert.EndsWith("/sifre-sifirla", Nav.Uri);
        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola456");
        e.Find("#sifirla-yeni-form").Submit();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sifirla-basari")));
        using var g = JsonDocument.Parse(istekler.Single(i => i.Yol.EndsWith("/tamamla")).Govde);
        Assert.Equal("Gizli_Jeton-123", g.RootElement.GetProperty("jeton").GetString());
    }

    [Fact]
    public void Acik_sekmede_gelen_parca_da_adres_cubugundan_silinir()
    {
        var e = Ciz();
        Nav.NavigateTo("/sifre-sifirla#jeton=Taze_456");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sifirla-yeni-form")));
        e.WaitForAssertion(() => Assert.DoesNotContain("#jeton", Nav.Uri));
    }

    [Fact]
    public void Hiz_siniri_429_KENDI_metni_DAKIKA_yazar_olmayan_dugmeyi_isaret_etmez()
    {
        istekKodu = HttpStatusCode.TooManyRequests;
        var e = Ciz();
        e.Find("#sifirla-eposta").Input("patron@ornek.com");
        e.Find("#sifirla-istek-form").Submit();
        e.WaitForAssertion(() => Assert.Equal(M.HizSiniri(15), e.Find("#sifirla-hata").TextContent.Trim()));
        var bant = e.Find("#sifirla-hata").TextContent;
        Assert.Contains("15 dakika", bant);
        Assert.DoesNotContain("saniye", bant);
        Assert.DoesNotContain("Yeniden dene”", bant);
        Assert.DoesNotContain("yoğunluk", bant, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(e.FindAll("#sifirla-basari"));
    }

    [Fact]
    public void Ekrana_TEKNIK_istisna_adi_yazilmaz()
    {
        durumKodu = HttpStatusCode.InternalServerError;
        var e = Ciz();
        var metin = e.Find("#sifirla-durum-hata").TextContent.Trim();
        Assert.Equal(M.DurumHatasi, metin);
        Assert.DoesNotContain("Exception", e.Find("#sifirla-kart").TextContent);
        Assert.DoesNotContain("ApiHata", e.Find("#sifirla-kart").TextContent);
    }

    [Fact]
    public void Sunucu_alan_hatasi_ALANIN_altina_duser_yeni_baglanti_cikmaz()
    {
        tamamlaKodu = HttpStatusCode.BadRequest;
        tamamlaHataGovdesi = """{"statusCode":400,"message":"Şifre en az bir harf ve bir rakam içermelidir","alanlar":{"yeniSifre":["Şifre en az bir harf ve bir rakam içermelidir"]}}""";
        var e = Ciz("/sifre-sifirla#jeton=abc");
        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola456");
        e.Find("#sifirla-yeni-form").Submit();
        e.WaitForAssertion(() => Assert.Contains("bir rakam", e.Find("#sifirla-kart").TextContent));
        Assert.Empty(e.FindAll("#sifirla-yeni-baglanti"));
    }

    // ── Saf ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("http://x/sifre-sifirla#jeton=abc", "abc")]
    [InlineData("http://x/sifre-sifirla#a=1&jeton=Q_w-1", "Q_w-1")]
    [InlineData("http://x/sifre-sifirla", null)]
    [InlineData("http://x/sifre-sifirla#jeton=", null)]
    [InlineData("http://x/sifre-sifirla?jeton=abc", null)]   // sorgu dizgesi KABUL EDİLMEZ (günlüğe düşer)
    public void Jeton_yalniz_PARCADAN_okunur(string uri, string? beklenen) =>
        Assert.Equal(beklenen, SifreSifirla.JetonuOku(uri));

    [Theory]
    [InlineData("YeniParola456", true)]
    [InlineData("kisa1", false)]
    [InlineData("sadeceharf", false)]
    [InlineData("12345678", false)]
    public void Istemci_kural_aynasi(string p, bool beklenen) =>
        Assert.Equal(beklenen, SifreSifirla.ParolaKuralinaUyar(p));

    [Theory]
    [InlineData("/giris", true)]
    [InlineData("/sifre-sifirla", true)]
    [InlineData("/sifre-sifirla/", true)]
    [InlineData("/", false)]
    [InlineData("/kullanicilar", false)]
    public void Kabuk_GIRIS_ONCESI_rotalar(string yol, bool beklenen) =>
        Assert.Equal(beklenen, AnaYerlesim.GirisOncesiRota(yol));

    [Fact]
    public void Sozluk_TR_EN_dolu_ve_jargon_yok()
    {
        foreach (var m in new[] { AuthSifirlaSozlugu.Tr, AuthSifirlaSozlugu.En })
            foreach (var p in typeof(AuthSifirlaMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotContain("token", v, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("jeton", v, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("SMTP", v, StringComparison.OrdinalIgnoreCase);
            }
        Assert.NotEqual(AuthSifirlaSozlugu.Tr.BaglantiGonder, AuthSifirlaSozlugu.En.BaglantiGonder);
    }

    // ── Giriş ekranındaki bağlantı ───────────────────────────────────────────

    private IRenderedComponent<Giris> GirisYardimAc()
    {
        Nav.NavigateTo("/giris");
        var g = Render<Giris>();
        g.WaitForAssertion(() => Assert.Empty(g.FindAll("#giris-kisit-yukleniyor")));
        istekler.Clear();
        g.Find("#giris-parola-unuttum").Click();
        g.WaitForAssertion(() => Assert.Empty(g.FindAll("#giris-parola-yardim-yukleniyor")));
        return g;
    }

    [Fact]
    public void Giris_kanal_ACIKKEN_sifirlama_baglantisini_ve_yonetici_yolunu_gosterir()
    {
        var g = GirisYardimAc();
        var bag = g.Find("#giris-parola-sifirla-bag");
        Assert.Equal("/sifre-sifirla", bag.GetAttribute("href"));
        Assert.Equal(M.GiristekiBaglanti, bag.TextContent.Trim());
        Assert.Contains("Şifre Sıfırla", g.Find("#giris-parola-yardim").TextContent);
        // Yalnız durum SORULUR (GET); hiçbir yazma isteği atılmaz.
        var tek = Assert.Single(istekler);
        Assert.Equal("GET", tek.Metot);
        Assert.EndsWith("/auth/parola-sifirlama/durum", tek.Yol);
    }

    [Fact]
    public void Giris_kanal_KAPALIYKEN_eposta_baglantisi_CIZILMEZ_tek_dogru_yol_yazilir()
    {
        kanalAcik = false;
        var g = GirisYardimAc();
        Assert.Empty(g.FindAll("#giris-parola-sifirla-bag"));
        Assert.DoesNotContain(M.GiristekiBaglanti, g.Markup);
        Assert.Equal(M.GirisKapaliYardim, g.Find("#giris-parola-yardim-yonetici").TextContent.Trim());
        Assert.Contains("destek hattı", g.Find("#giris-parola-yardim").TextContent);
    }

    [Fact]
    public void Giris_durum_OKUNAMAZSA_baglanti_CIZILMEZ_kapali_da_denmez_teknik_ad_yok()
    {
        durumKodu = HttpStatusCode.InternalServerError;
        var g = GirisYardimAc();
        Assert.Empty(g.FindAll("#giris-parola-sifirla-bag"));
        var yardim = g.Find("#giris-parola-yardim").TextContent;
        Assert.Contains("Şifre Sıfırla", yardim);
        Assert.DoesNotContain(M.KapaliBaslik, yardim);
        Assert.DoesNotContain("Exception", yardim);
    }
}
