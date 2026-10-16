using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
// PAKET T-K4 — TÜM EKRANLAR + GİRİŞ TUVALE (2026-09-25)
//
// Tek doğruluk kaynağı: tuval/project/Ekranlar.dc.html · Giris.dc.html.
// Sınanan sözleşme:
//   (1) Yazımlar tuvalden: marka "Mihenk" (Marka.Ad'dan TÜRER, ikinci kaynak yok),
//       "Vitrin panosu", "Parola", "Giriş yap", sürüm satırı "Mihenk x.y · ByGatech".
//   (2) Günlük iş satırı ikonlu ızgara sınıfını taşır (K3 — kısa etiket bölünmez).
//   (3) Mevcut id'ler ve giriş davranışı (POST /auth/giris {email, sifre}) DEĞİŞMEZ.
//   (4) css/tuval-kabuk.css T-K4 bloğu tuval ölçülerini BELİRTEÇLE taşır
//       (8 sütun ≥1400, yıldız yer ayırmaz, arama 24 px satır, giriş kartının
//       akışından çıkan dil seçici + katlı kısıt beyanı, boş alt satır arası yok).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TK4EkranlarGirisTuvalTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı yok — dokunulan her uca gerçekçi yanıt.
            string cevap;
            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
                cevap = JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            else if (yol.EndsWith("/auth/giris", StringComparison.Ordinal))
                cevap = """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""";
            else if (yol.Contains("/tercihler/benim", StringComparison.Ordinal))
                cevap = """{"tercihler":{},"mesaj":"Tercihler kaydedildi"}""";
            else if (yol.Contains("/yetkiler/benim", StringComparison.Ordinal))
                cevap = """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim","kur"]}""";
            else
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"hata":"beklenmeyen uç"}""", System.Text.Encoding.UTF8, "application/json"),
                };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public TK4EkranlarGirisTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<Ekranlar> EkranlarCiz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", null,
                                ArayuzModuServisi.Basit));
        var e = Render<Ekranlar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("a.ekran-karo")));
        return e;
    }

    private IRenderedComponent<Giris> GirisCiz()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/giris");
        var e = Render<Giris>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#giris-kisit-yukleniyor")));
        return e;
    }

    // ── (1) Yazımlar ─────────────────────────────────────────────────────────

    [Fact]
    public void TK4_01_Marka_tuval_yazimi_Mihenk_ve_Marka_Ad_dan_TURER()
    {
        Assert.Equal("Mihenk", KabukTuvalSozlugu.MarkaYazimi);
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var kucuk = Marka.Ad.ToLower(tr);
        Assert.Equal(char.ToUpper(kucuk[0], tr) + kucuk[1..], KabukTuvalSozlugu.MarkaYazimi);

        var e = EkranlarCiz();
        Assert.Equal("Mihenk", e.Find("#ekranlar-panele-don .ekranlar-marka-ad").TextContent.Trim());
    }

    [Fact]
    public void TK4_02_Vitrin_panosu_tuval_yazimi_TR_EN()
    {
        Assert.Equal("Vitrin panosu", EkranlarGirisTuvalSozlugu.VitrinPanosu("tr"));
        Assert.Equal("Showcase board", EkranlarGirisTuvalSozlugu.VitrinPanosu("en"));
        var e = EkranlarCiz();
        Assert.Equal("Vitrin panosu", e.Find("#ekranlar-alt-vitrin").TextContent.Trim());
        Assert.Equal("/vitrin", e.Find("#ekranlar-alt-vitrin").GetAttribute("href"));
    }

    [Fact]
    public void TK4_03_Yalniz_Gunluk_is_izgarasi_IKONLU_sinifi_tasir()
    {
        var e = EkranlarCiz();
        var gunluk = e.Find($"#ekranlar-grup-{MenuTanimi.GrupSlug(MenuGrubu.Gunluk)} .ekranlar-izgara");
        Assert.Contains("ekranlar-izgara-ikonlu", gunluk.ClassList);
        var ikonlu = e.FindAll(".ekranlar-izgara-ikonlu");
        Assert.Single(ikonlu);
        // Diğer grupların ızgarası düz kalır (tuval: ikonsuz karolar, 8 eşit sütun).
        Assert.True(e.FindAll(".ekranlar-izgara").Count > 1);
    }

    [Fact]
    public void TK4_04_Giris_tuval_yazimlari_Mihenk_Parola_Giris_yap_surum()
    {
        var e = GirisCiz();
        Assert.Equal("Mihenk", e.Find("h1.giris-marka-ad").TextContent.Trim());
        Assert.Equal("Mihenk’e hoş geldin", e.Find("#sayfa-basi-giris-baslik").TextContent.Trim());
        Assert.Equal("Parola", e.Find("label[for='giris-sifre'] .alan-etiket-metin").TextContent.Trim());
        Assert.Equal("Giriş yap", e.Find("#giris-gonder").TextContent.Trim());
        Assert.StartsWith("Mihenk ", e.Find("#giris-surum").TextContent.Trim(), StringComparison.Ordinal);
        Assert.EndsWith(" · ByGatech", e.Find("#giris-surum").TextContent.Trim(), StringComparison.Ordinal);
        // Parolamı unuttum ile aynı kelime (tek kavram, tek ad).
        Assert.StartsWith(EkranlarGirisTuvalSozlugu.Parola("tr"), GirisSozlugu.ParolamiUnuttum("tr"), StringComparison.Ordinal);

        // Kayıt formu da aynı etiketi kullanır.
        e.Find("#giris-yeni-dukkan").Click();
        Assert.Equal("Parola", e.Find("label[for='kayit-sifre'] .alan-etiket-metin").TextContent.Trim());
    }

    [Fact]
    public async Task TK4_05_Giris_ingilizce_yazimlar()
    {
        var e = GirisCiz();
        await e.InvokeAsync(() => Services.GetRequiredService<DilServisi>().DegistirAsync("en"));
        e.WaitForAssertion(() => Assert.Equal("Sign in", e.Find("#giris-gonder").TextContent.Trim()));
        Assert.Equal("Password", e.Find("label[for='giris-sifre'] .alan-etiket-metin").TextContent.Trim());
        Assert.Equal("Welcome to Mihenk", e.Find("#sayfa-basi-giris-baslik").TextContent.Trim());
    }

    // ── (3) Davranış ve id'ler değişmez ──────────────────────────────────────

    [Fact]
    public void TK4_06_Giris_mevcut_idler_KORUNUR()
    {
        var e = GirisCiz();
        foreach (var id in new[]
                 {
                     "giris-sayfa", "giris-sol", "giris-kart", "giris-marka", "giris-dil", "giris-form",
                     "giris-eposta", "giris-sifre", "giris-sifre-goster", "giris-gonder", "giris-baglar",
                     "giris-parola-unuttum", "giris-yeni-dukkan", "giris-surum", "giris-kisit", "giris-kisit-ozet",
                     "giris-panel", "giris-panel-baslik", "giris-panel-alt", "giris-rakamlar",
                     "sayfa-basi-giris-baslik", "sayfa-basi-giris-aciklama",
                 })
            Assert.NotNull(e.Find("#" + id));
    }

    [Fact]
    public void TK4_07_Giris_POST_yolu_DEGISMEZ_email_sifre_govdesi()
    {
        var e = GirisCiz();
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola1234");
        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/auth/giris", StringComparison.Ordinal)));
        var govde = istekler.Last(i => i.Yol.EndsWith("/auth/giris", StringComparison.Ordinal)).Govde;
        using var j = JsonDocument.Parse(govde);
        Assert.Equal("qa@ornek.com", j.RootElement.GetProperty("email").GetString());
        Assert.Equal("Parola1234", j.RootElement.GetProperty("sifre").GetString());
    }

    [Fact]
    public void TK4_08_Giris_bos_gonderimde_alan_alti_mesaj_ve_istek_YOK()
    {
        var e = GirisCiz();
        e.Find("#giris-form").Submit();
        Assert.NotEmpty(e.FindAll("#giris-eposta-sar .alan-hata"));
        Assert.NotEmpty(e.FindAll("#giris-sifre-sar .alan-hata"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/auth/giris", StringComparison.Ordinal));
    }

    // ── (4) CSS: tuval ölçüleri, yalnız belirteç ─────────────────────────────

    private static string Blok()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-kabuk.css");
        var i = css.IndexOf("PAKET T-K4 — TÜM EKRANLAR + GİRİŞ TUVALE", StringComparison.Ordinal);
        Assert.True(i > 0, "tuval-kabuk.css'te T-K4 bloğu yok.");
        return Regex.Replace(css[css.LastIndexOf("/*", i, StringComparison.Ordinal)..], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    [Fact]
    public void TK4_09_CSS_sabit_renk_YOK_yalniz_belirtec()
    {
        var kod = Blok();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", kod);
        Assert.Contains("--tk-soluk-acik: color-mix(in srgb, var(--kenar-metin) 50%, var(--menu-baslik));", kod);
        Assert.Contains(".ekranlar-sayfa .ekranlar-ipucu { color: var(--tk-soluk-acik); white-space: nowrap; }", kod);
    }

    [Fact]
    public void TK4_10_CSS_izgara_1400_ustu_8_sutun_1024_1399_6_sutun()
    {
        var kod = Blok();
        Assert.Matches(@"@media \(min-width: 1400px\) \{\s*\.ekranlar-sayfa \.ekranlar-izgara \{ --tk4-sutun: 8; grid-template-columns: repeat\(8, minmax\(0, 1fr\)\); \}", kod);
        Assert.Matches(@"@media \(min-width: 1024px\) and \(max-width: 1399px\) \{\s*\.ekranlar-sayfa \.ekranlar-izgara \{ --tk4-sutun: 6; grid-template-columns: repeat\(6, minmax\(0, 1fr\)\); \}", kod);
    }

    [Fact]
    public void TK4_11_CSS_ikonlu_satir_yaziya_gore_buyur_kisa_etiket_bolunmez()
    {
        var kod = Blok();
        Assert.Contains(".ekranlar-sayfa .ekranlar-izgara-ikonlu { display: flex; flex-wrap: wrap; gap: 10px; }", kod);
        Assert.Contains("min-width: calc((100% - (var(--tk4-sutun) - 1) * 10px) / var(--tk4-sutun));", kod);
        Assert.Contains(".ekranlar-sayfa .ekranlar-izgara-ikonlu .ekran-karo-ad { white-space: nowrap; }", kod);
    }

    [Fact]
    public void TK4_12_CSS_yildiz_fareli_ekranda_YER_AYIRMAZ_dokunmatikte_dokunulmaz()
    {
        var kod = Blok();
        var i = kod.IndexOf("@media (hover: hover) and (min-width: 769px)", StringComparison.Ordinal);
        Assert.True(i > 0);
        var parca = kod[i..kod.IndexOf("\n}", i, StringComparison.Ordinal)];
        Assert.Contains(".ekranlar-sayfa .ekran-karo-kap > .ekran-karo { padding-right: 14px; }", parca);
        Assert.Contains(".ekranlar-sayfa .ekran-yildiz { top: 2px; right: 2px; width: 24px; height: 24px; }", parca);
        Assert.DoesNotContain("hover: none", kod);
    }

    [Fact]
    public void TK4_13_CSS_arama_kutusu_24px_satir_kabi_44px()
    {
        var kod = Blok();
        Assert.Contains(".ekranlar-sayfa .ekranlar-arama .arama-kutu > input.girdi-arama:focus-visible {", kod);
        Assert.Contains("min-height: 0; height: 24px; line-height: 24px; font-size: 16px;", kod);
    }

    [Fact]
    public void TK4_14_CSS_giris_kart_akisi_tuvaldeki_gibi()
    {
        var kod = Blok();
        Assert.Contains(".giris-iki-panel .giris-form-alan .zorunlu-isaret { display: none; }", kod);
        Assert.Contains(".giris-iki-panel .giris-baglar .giris-bag { margin: -11px 0; }", kod);
        Assert.Contains(".giris-iki-panel .giris-form-alan .alan:not(:has(> .alan-alt > *)) { grid-template-rows: auto auto; }", kod);
        Assert.Contains(".giris-iki-panel .giris-form-alan .alan-etiket-metin { font-weight: 400; }", kod);
        Assert.Contains(".giris-iki-panel .giris-marka-ad { line-height: 1.45; }", kod);
        var i = kod.IndexOf("@media (min-width: 769px) and (min-height: 700px)", StringComparison.Ordinal);
        Assert.True(i > 0, "dil seçici / kısıt beyanı yerleşimi dar ya da alçak ekranda uygulanmamalı.");
        var parca = kod[i..];
        Assert.Contains(".giris-iki-panel .giris-dil { position: absolute; top: 24px; right: 24px; }", parca);
        Assert.Contains(".giris-iki-panel .giris-kisit:not([open]) {", parca);
        Assert.Contains("position: absolute; bottom: 16px; left: calc(50% - 220px);", parca);
    }

    [Fact]
    public void TK4_15_Razor_satir_ici_stil_ve_sabit_marka_metni_YOK()
    {
        foreach (var sayfa in new[] { "Ekranlar.razor", "Giris.razor" })
        {
            var kod = CssKaynak.Metin("Kuyumcu.Web", "Pages", sayfa);
            Assert.DoesNotContain("style=\"", kod, StringComparison.Ordinal);
            Assert.DoesNotContain(">Mihenk<", kod, StringComparison.Ordinal);
            Assert.Contains("KabukTuvalSozlugu.MarkaYazimi", kod, StringComparison.Ordinal);
        }
    }
}
