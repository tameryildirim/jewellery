using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Kisit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-e — GİRİŞ EKRANI: İKİ PANEL · tuval Giris.dc.html
//
// Sınanan sözleşme:
//   (1) TUVAL — sol tek kart (marka h1 + "… hoş geldin" + e-posta/parola +
//       Giriş yap + iki bağlantı + sürüm satırı), sağ menü zemini panel
//       (büyük cümle + alt cümle + üç rakam). Tek birincil düğme.
//   (2) HER DÜĞME İŞ YAPAR (DoD EK md.1) — Göster parolayı gösterir/gizler,
//       "Parolamı unuttum" doğru yolu yazar, "Yeni dükkân kur" kayıt formunu
//       açar, "Girişe dön" geri getirir, Giriş yap POST atar.
//   (3) ÖZELLİK SİLİNMEZ — kayıt, MFA ve P-30 kısıt beyanı yaşar; mevcut
//       id'ler aynı; kısıt beyanı katlanır ama HATA varsa AÇIK başlar (UX-S28).
//   (4) SÜRÜM ELLE YAZILMAZ — istemci derlemesinden türer ve API sürümüyle aynı.
//   (5) CSS — tuval ölçüleri belirteçle, 768 px'te tek sütun, sabit renk yok.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1eGirisTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kisitPatlasin;
    private bool girisReddedilsin;

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<bool> kisitPatlat, Func<bool> girisReddet) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
            {
                if (kisitPatlat()) return Yanit(HttpStatusCode.ServiceUnavailable, """{"hata":"sunucu yok"}""");
                return Yanit(HttpStatusCode.OK, JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            if (yol.EndsWith("/auth/giris", StringComparison.Ordinal) && girisReddet())
                return Yanit(HttpStatusCode.Unauthorized, """{"statusCode":401,"message":"E-posta veya şifre hatalı"}""");
            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — dokunulan uca gerçek şekil.
            if (yol.EndsWith("/auth/giris", StringComparison.Ordinal) || yol.EndsWith("/auth/kayit", StringComparison.Ordinal))
                return Yanit(HttpStatusCode.OK,
                    """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""");
            return Yanit(HttpStatusCode.NotFound, """{"hata":"beklenmeyen uç"}""");
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    public S1eGirisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kisitPatlasin, () => girisReddedilsin))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Giris> Ciz()
    {
        Nav.NavigateTo("/giris");
        var e = Render<Giris>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#giris-kisit-yukleniyor")));
        return e;
    }

    // ── (1) TUVAL ────────────────────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: Tuval_IKI_PANEL_sol_kart_ve_sag_panel kaldırıldı — iki panelli giriş (aside#giris-panel, h1.giris-marka-ad, giriş sekmesi başlığı) tek şema Yıldız ile BİLEREK kalktı; giriş tuval Giris4 düzenidir (ortada amblem, panel/başlık yok): YildizAmblem2Testleri.YA2_13/YA2_14, YildizAmblem3Testleri.YA3_01.
    // TEMA TEMİZLİĞİ 17.10: Sag_panel_TUVAL_metinleri_ve_UC_rakam kaldırıldı — sağ panel (#giris-panel-baslik, #giris-rakamlar) tuval Giris4'te yok (YildizAmblem2Testleri.YA2_13: #giris-panel boş).

    [Fact]
    public void Giris_formu_EPOSTA_PAROLA_ve_TEK_birincil_dugme()
    {
        var e = Ciz();

        Assert.Equal("email", e.Find("#giris-eposta").GetAttribute("type"));
        Assert.Equal("password", e.Find("#giris-sifre").GetAttribute("type"));
        Assert.Single(e.FindAll(".btn-birincil"));
        Assert.Equal(EkranlarGirisTuvalSozlugu.GirisYap("tr"), e.Find("#giris-gonder").TextContent.Trim());
        Assert.Equal("submit", e.Find("#giris-gonder").GetAttribute("type"));
        // Kayıt formu ve sekme hapları ilk bakışta yok (tuval): kayıt bağlantının arkasında.
        Assert.Empty(e.FindAll("#kayit-isletme"));
        Assert.Empty(e.FindAll(".pill"));
    }

    [Fact]
    public void Surum_satiri_DERLEMEDEN_turer_marka_ve_ByGatech_tasir()
    {
        var e = Ciz();
        var bilgi = typeof(Giris).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var satir = e.Find("#giris-surum").TextContent.Trim();
        Assert.Equal($"{KabukTuvalSozlugu.MarkaYazimi} {Giris.SurumMetni(bilgi)} · ByGatech", satir);
        Assert.DoesNotContain("—", satir);
    }

    [Theory]
    [InlineData("1.0.0+abc123", "1.0")]
    [InlineData("2.3.4-rc1", "2.3")]
    [InlineData("1.0.0", "1.0")]
    [InlineData("gelistirme", "gelistirme")]
    [InlineData(null, "—")]
    [InlineData("  ", "—")]
    public void SurumMetni_cekirdek_surumu_Major_Minor_verir(string? bilgi, string beklenen)
        => Assert.Equal(beklenen, Giris.SurumMetni(bilgi));

    [Fact]
    public void Istemci_surumu_API_surumuyle_AYNI()
    {
        // Sürümün tek kaynağı API projesidir (T-4a); istemci aynı yayın
        // parametresiyle derlenir. İkisi ayrışırsa giriş ekranı yalan söyler.
        static string Oku(Assembly a) => Giris.SurumMetni(
            a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        Assert.Equal(Oku(typeof(Kuyumcu.Api.Modules.Meta.SurumController).Assembly), Oku(typeof(Giris).Assembly));
    }

    // ── (2) HER DÜĞME İŞ YAPAR ───────────────────────────────────────────────

    [Fact]
    public void Goster_parolayi_GORUNUR_yapar_ve_GERI_gizler()
    {
        var e = Ciz();
        var goster = e.Find("#giris-sifre-goster");
        Assert.Equal("Göster", goster.TextContent.Trim());
        Assert.Equal("false", goster.GetAttribute("aria-pressed"));
        Assert.Equal("giris-sifre", goster.GetAttribute("aria-controls"));

        goster.Click();
        Assert.Equal("text", e.Find("#giris-sifre").GetAttribute("type"));
        Assert.Equal("Gizle", e.Find("#giris-sifre-goster").TextContent.Trim());
        Assert.Equal("true", e.Find("#giris-sifre-goster").GetAttribute("aria-pressed"));

        e.Find("#giris-sifre-goster").Click();
        Assert.Equal("password", e.Find("#giris-sifre").GetAttribute("type"));
        Assert.Equal("Göster", e.Find("#giris-sifre-goster").TextContent.Trim());
    }

    [Fact]
    public void Goster_yazilan_degeri_KORUR_ve_gonderir()
    {
        var e = Ciz();
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola12345");
        e.Find("#giris-sifre-goster").Click();
        Assert.Equal("Parola12345", e.Find("#giris-sifre").GetAttribute("value"));

        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/auth/giris")));
        using var govde = JsonDocument.Parse(istekler.Last(i => i.Yol.EndsWith("/auth/giris")).Govde);
        Assert.Equal("Parola12345", govde.RootElement.GetProperty("sifre").GetString());
    }

    [Fact]
    public void Parolami_unuttum_DOGRU_YOLU_yazar_istek_atmaz_ve_kapanir()
    {
        var e = Ciz();
        istekler.Clear();
        var bag = e.Find("#giris-parola-unuttum");
        Assert.Equal("false", bag.GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#giris-parola-yardim"));

        bag.Click();
        Assert.Equal("true", e.Find("#giris-parola-unuttum").GetAttribute("aria-expanded"));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#giris-parola-yardim-yukleniyor")));
        var yardim = e.Find("#giris-parola-yardim").TextContent;
        Assert.Contains("Kullanıcılar", yardim);
        Assert.Contains("Parolayı sıfırla", yardim);
        // AUTH-SIFIRLA tur 2: bölme yalnız sıfırlama DURUMUNU sorar (GET); yazma isteği atılmaz.
        Assert.All(istekler, i => Assert.Equal("GET", i.Item1));
        Assert.All(istekler, i => Assert.EndsWith("/auth/parola-sifirlama/durum", i.Item2));

        e.Find("#giris-parola-unuttum").Click();
        Assert.Empty(e.FindAll("#giris-parola-yardim"));
    }

    [Fact]
    public void Yeni_dukkan_kur_KAYIT_formunu_acar_Girise_don_geri_getirir()
    {
        var e = Ciz();
        e.Find("#giris-parola-unuttum").Click();
        e.Find("#giris-yeni-dukkan").Click();

        Assert.NotNull(e.Find("#kayit-isletme"));
        Assert.Empty(e.FindAll("#giris-eposta"));
        // UX-S31: bağlantı, başlık ve gönder düğmesi AYNI ad.
        var ad = GirisSozlugu.YeniDukkanKur("tr");
        Assert.Equal(ad, e.Find("#sayfa-basi-giris-baslik").TextContent.Trim());
        Assert.Equal(ad, e.Find("#kayit-gonder").TextContent.Trim());
        Assert.Single(e.FindAll(".btn-birincil"));

        e.Find("#giris-girise-don").Click();
        Assert.NotNull(e.Find("#giris-eposta"));
        Assert.Empty(e.FindAll("#kayit-isletme"));
        // Sekme değişince açık yardım taşınmaz.
        Assert.Empty(e.FindAll("#giris-parola-yardim"));
    }

    [Fact]
    public void Kayit_formu_POST_auth_kayit_atar_govde_ayristirilir()
    {
        var e = Ciz();
        e.Find("#giris-yeni-dukkan").Click();
        e.Find("#kayit-isletme").Input("Deneme Kuyumculuk");
        e.Find("#kayit-ad").Input("Ayşe");
        e.Find("#kayit-soyad").Input("Öz");
        e.Find("#kayit-eposta").Input("ayse@ornek.com");
        e.Find("#kayit-sifre").Input("Parola12345");
        e.Find("#kayit-davet").Input("  DAVET-1  ");
        e.Find("#kayit-form").Submit();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/auth/kayit")));
        using var govde = JsonDocument.Parse(istekler.Last(i => i.Yol.EndsWith("/auth/kayit")).Govde);
        var k = govde.RootElement;
        Assert.Equal("Deneme Kuyumculuk", k.GetProperty("isletmeAdi").GetString());
        Assert.Equal("Ayşe", k.GetProperty("ad").GetString());
        Assert.Equal("ayse@ornek.com", k.GetProperty("email").GetString());
        Assert.Equal("DAVET-1", k.GetProperty("davetJetonu").GetString());
        Assert.Equal("http://localhost/", Nav.Uri);
    }

    [Fact]
    public void Giris_POST_auth_giris_atar_ve_panele_gider()
    {
        var e = Ciz();
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola12345");
        e.Find("#giris-gonder").Click();   // submit düğmesi formu gönderir

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/auth/giris")));
        using var govde = JsonDocument.Parse(istekler.Last(i => i.Yol.EndsWith("/auth/giris")).Govde);
        Assert.Equal("qa@ornek.com", govde.RootElement.GetProperty("email").GetString());
        Assert.Equal("http://localhost/", Nav.Uri);
    }

    [Fact]
    public void Giris_reddedilirse_HATA_BANDI_cizilir_yonlenmez()
    {
        girisReddedilsin = true;
        var e = Ciz();
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola12345");
        e.Find("#giris-form").Submit();

        e.WaitForAssertion(() => Assert.Contains("hatalı", e.Find("#giris-kart").TextContent));
        Assert.False(e.Find("#giris-gonder").HasAttribute("disabled"));
        Assert.EndsWith("/giris", Nav.Uri);   // yönlenmedi
    }

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar()
    {
        // DoD EK md.1: "tıklanınca hiçbir şey olmayan" düğme yok. DilSecici
        // kendi testleriyle sınanır; geri kalan her düğmenin kimliği bilinir.
        var e = Ciz();
        var kimlikler = e.FindAll("#giris-kart button")
            .Where(b => b.Closest(".dil-secici") is null)
            .Select(b => b.Id).ToHashSet();
        Assert.Equal(new HashSet<string?> { "giris-sifre-goster", "giris-gonder", "giris-parola-unuttum", "giris-yeni-dukkan" },
            kimlikler);

        foreach (var id in kimlikler)
        {
            var taze = Ciz();
            var once = taze.Markup;
            var istekOnce = istekler.Count;
            taze.Find($"#{id}").Click();
            Assert.True(taze.Markup != once || istekler.Count > istekOnce, $"#{id} tıklanınca hiçbir şey olmadı.");
        }
    }

    // ── (3) ÖZELLİK SİLİNMEZ · id sözleşmesi · UX-S28 ────────────────────────

    [Fact]
    public void Kisit_beyani_KATLANIR_ozet_sunucu_sayisini_tasir()
    {
        var e = Ciz();
        var kisit = e.Find("details#giris-kisit");
        Assert.False(kisit.HasAttribute("open"));
        // Beklenen değer süzgeçten BAĞIMSIZ kurulur (M1e-13 dersi: sözlük
        // fonksiyonuyla hesaplanan beklenen, mutasyona uğramış fonksiyonu yakalamaz).
        Assert.Equal($"{Sozlukler.Tr.Auth.KisitBaslik} ({KisitBeyani.Liste.Count})",
            e.Find("#giris-kisit-ozet").TextContent.Trim());
        Assert.Equal("A (8)", GirisSozlugu.KisitOzet("A", 8));
        Assert.Equal("A", GirisSozlugu.KisitOzet("A", null));
        Assert.Equal(KisitBeyani.Liste.Count, e.FindAll("#giris-kisit-liste > li").Count);
    }

    [Fact]
    public void Kisit_ucu_patlarsa_beyan_ACIK_baslar_hata_gorunur()
    {
        kisitPatlasin = true;
        var e = Render<Giris>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#giris-kisit-hata")));
        Assert.True(e.Find("details#giris-kisit").HasAttribute("open"));
        // Sayı uydurulmaz: liste okunamadıysa özette "(n)" yok.
        Assert.Equal(Sozlukler.Tr.Auth.KisitBaslik, e.Find("#giris-kisit-ozet").TextContent.Trim());
    }

    [Fact]
    public void Mevcut_id_envanteri_KORUNUR()
    {
        // TEMA TEMİZLİĞİ 17.10: giriş sekmesinde başlık (SayfaBasi) çizilmez (tuval Giris4); başlık
        // id'leri kayıt sekmesinde yaşar.
        var e = Ciz();
        foreach (var id in new[] { "giris-eposta", "giris-sifre", "giris-kisit", "giris-kisit-liste",
                                   "giris-kisit-kaynak", "giris-amblem" })
            Assert.Single(e.FindAll($"#{id}"));

        e.Find("#giris-yeni-dukkan").Click();
        foreach (var id in new[] { "kayit-isletme", "kayit-ad", "kayit-soyad", "kayit-eposta", "kayit-sifre", "kayit-davet",
                                   "sayfa-basi-giris", "sayfa-basi-giris-baslik" })
            Assert.Single(e.FindAll($"#{id}"));
    }

    [Fact]
    public async Task Ingilizce_dilde_metinler_EN_sozlukten()
    {
        var e = Ciz();
        await e.InvokeAsync(() => Services.GetRequiredService<DilServisi>().DegistirAsync("en"));

        e.WaitForAssertion(() =>
            Assert.Equal(EkranlarGirisTuvalSozlugu.GirisYap("en"), e.Find("#giris-gonder").TextContent.Trim()));
        Assert.Equal("Show", e.Find("#giris-sifre-goster").TextContent.Trim());
        Assert.Equal(GirisSozlugu.ParolamiUnuttum("en"), e.Find("#giris-parola-unuttum").TextContent.Trim());
        // ESKİ: giriş sekmesi başlığı (HosGeldin) ve sağ panel başlığı (PanelBaslik) — Yıldız girişinde
        // ikisi de yok; kayıt sekmesinin başlığı İngilizce sözlükten gelir.
        e.Find("#giris-yeni-dukkan").Click();
        Assert.Equal(GirisSozlugu.YeniDukkanKur("en"), e.Find("#sayfa-basi-giris-baslik").TextContent.Trim());
    }

    [Fact]
    public void Sozluk_IKI_DILDE_dolu_ve_farkli()
    {
        foreach (var dil in new[] { "tr", "en" })
        {
            foreach (var m in new[]
                     {
                         GirisSozlugu.HosGeldin(dil), GirisSozlugu.HesabinlaGir(dil), GirisSozlugu.Goster(dil),
                         GirisSozlugu.Gizle(dil), GirisSozlugu.ParolamiUnuttum(dil), GirisSozlugu.ParolaYardim(dil),
                         GirisSozlugu.YeniDukkanKur(dil), GirisSozlugu.YeniDukkanAlt(dil), GirisSozlugu.GiriseDon(dil),
                         GirisSozlugu.PanelBaslik(dil), GirisSozlugu.PanelAlt(dil),
                     })
                Assert.False(string.IsNullOrWhiteSpace(m));
            Assert.Equal(3, GirisSozlugu.Rakamlar(dil).Count);
            Assert.Contains(KabukTuvalSozlugu.MarkaYazimi, GirisSozlugu.HosGeldin(dil), StringComparison.Ordinal);
        }
        Assert.NotEqual(GirisSozlugu.PanelBaslik("tr"), GirisSozlugu.PanelBaslik("en"));
        Assert.NotEqual(GirisSozlugu.YeniDukkanKur("tr"), GirisSozlugu.YeniDukkanKur("en"));
    }

    // ── Ortak Sifre bileşeni: göster düğmesi İSTEĞE BAĞLI ────────────────────

    [Fact]
    public void Sifre_bileseni_GosterEtiketi_yoksa_ESKISI_GIBI_dugmesiz()
    {
        var c = Render<Sifre>(p => p.Add(x => x.ElemanId, "s-yeni").Add(x => x.Etiket, "Parola"));
        Assert.Empty(c.FindAll("button"));
        Assert.Empty(c.FindAll(".sifre-kutu"));
        Assert.Equal("password", c.Find("#s-yeni").GetAttribute("type"));
    }

    // ── (5) KAYNAK + CSS ─────────────────────────────────────────────────────

    [Fact]
    public void Sayfada_satir_ici_style_ve_SABIT_renk_YOK()
    {
        var razor = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "Pages", "Giris.razor"));
        Assert.DoesNotContain("style=\"", razor);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", razor);
        Assert.DoesNotContain("<script", razor, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S1e_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir()
    {
        var css = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "wwwroot", "css", "app.css"));
        var i = css.IndexOf("S1-e — GİRİŞ EKRANI: İKİ PANEL", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-e bloğu yok.");
        var kod = Regex.Replace(css[i..], @"/\*.*?\*/", "", RegexOptions.Singleline);

        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", kod);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) minmax(0, 1.1fr);", kod);
        Assert.Contains("width: 440px; max-width: 100%;", kod);
        Assert.Contains("height: 54px; min-height: 54px;", kod);
        Assert.Contains("background: var(--kenar-yuzey); color: var(--kenar-metin-guclu);", kod);
        Assert.Contains("font-family: var(--yazi-marka); font-size: 36px; font-weight: 700;", kod);
        Assert.Contains("font-family: var(--yazi-marka); font-size: 34px; font-weight: 700;", kod);
        Assert.Contains("background: var(--vurgu); color: var(--vurgu-metin);", kod);
        Assert.Contains("font-variant-numeric: tabular-nums;", kod);
        // ≤ 768 px tek sütun (şartname 12).
        var mobil = kod.IndexOf("@media (max-width: 768px)", StringComparison.Ordinal);
        Assert.True(mobil > 0, "768 px kırılımı yok.");
        Assert.Contains(".giris-iki-panel { grid-template-columns: minmax(0, 1fr); }", kod[mobil..]);
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }
}
