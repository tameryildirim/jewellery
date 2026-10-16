using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K2 — KABUK TUVALE: kenar çubuğu + üst bar
//
// Tek doğruluk kaynağı: dokumanlar/02-mimari/sadelestirme/tuval/project/
//   Panel.dc.html (BASİT) · Profesyonel.dc.html (PRO) · Ayarlar.dc.html
// Görünüm (px, renk, sığma) GERÇEK TARAYICIDA ölçüldü (rapor § ölçüm tablosu);
// burada kilitlenenler: öğe varlığı, metinler, taşınan özelliklerin YENİ yeri
// (silinmedi), düğmelerin gerçek etkisi ve CSS'teki tuval ölçüleri.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TK2KabukTuvalTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private int subeSayisi = 1;
    private bool subeHata;
    private int bildirimSayisi;
    private string tercihler = "{}";

    private sealed class SahteApi(TK2KabukTuvalTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol));
            if (yol.EndsWith("/subeler", StringComparison.Ordinal) && s.subeHata)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Şube listesi okunamadı"}""", System.Text.Encoding.UTF8, "application/json"),
                });
            }
            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    // ŞUBE-KAPSAM-B: "tüm şubeler" yetkisi sunucudan (gorunumSubesi.tumSubeler).
                    $$"""{"tercihler":{{s.tercihler}},"gorunumSubesi":{"kayitli":null,"subeId":null,"tumSubeler":true,"gecersizTercih":false,"deger":"TUMU"},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) =>
                    "[" + string.Join(",", Enumerable.Range(0, s.subeSayisi)
                        .Select(i => $$"""{"id":"s{{i}}","ad":"Şube {{i}}","aktif":true}""")) + "]",
                var y when y.EndsWith("/bildirimler/sayi", StringComparison.Ordinal) =>
                    $$"""{"sayi":{{s.bildirimSayisi}},"enYuksekOnem":"UYARI"}""",
                var y when y.EndsWith("/kur/guncel", StringComparison.Ordinal) =>
                    """{"id":"kur-1","tenantId":"t1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,"usdTry":41.95,"eurTry":49.10,"gbpTry":56.30,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-23T09:02:00Z","ayarFiyatlari":[]}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public TK2KabukTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private void Oturum(string mod, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s0", null, mod));
    }

    private IRenderedComponent<KenarCubugu> Menu(string mod = ArayuzModuServisi.Basit, string rol = "SAHIP",
        int bildirim = 0, string rota = "/")
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        Oturum(mod, rol);
        var e = Render<KenarCubugu>(p => p.Add(x => x.BildirimSayisi, bildirim));
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    private static string KabukCss() => File.ReadAllText(Path.Combine(
        KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-kabuk.css"));

    private static string Tk2Blogu()
    {
        var css = KabukCss();
        var i = css.IndexOf("PAKET T-K2", StringComparison.Ordinal);
        Assert.True(i >= 0, "tuval-kabuk.css'te T-K2 bloğu YOK");
        // Blok, başlık yorumunun AÇILIŞINDAN başlar (yorum temizliği bozulmasın).
        return css[css.LastIndexOf("/*", i, StringComparison.Ordinal)..];
    }

    // ── (1) Marka ────────────────────────────────────────────────────────────

    [Fact]
    public void TK2_Marka_yazimi_Mihenk_ve_TEK_KAYNAKTAN_turetilir()
    {
        Assert.Equal("Mihenk", KabukTuvalSozlugu.MarkaYazimi);
        Assert.Equal(Marka.Ad, KabukTuvalSozlugu.MarkaYazimi.ToUpper(new System.Globalization.CultureInfo("tr-TR")));
    }

    [Fact]
    public void TK2_Marka_satiri_mavi_kare_icinde_elmas_ve_dil_secici_TEPEDE_YOK()
    {
        var e = Menu();
        var marka = e.Find("#kenar-marka");
        var rozet = marka.QuerySelector(".kenar-marka-rozet");
        Assert.NotNull(rozet);
        Assert.Equal("true", rozet!.GetAttribute("aria-hidden"));
        Assert.Contains("ikon-gem", rozet.InnerHtml, StringComparison.Ordinal);
        Assert.Equal("Mihenk", marka.TextContent.Trim());
        // TR/EN seçici marka satırından KALKTI …
        Assert.Empty(e.FindAll(".kenar-logo .dil-secici"));
        // … ve kullanıcı kartının menüsüne TAŞINDI (özellik silinmedi).
        Assert.Single(e.FindAll("#kenar-kullanici-menu .dil-secici"));
    }

    // ── (2) Ekran ara ────────────────────────────────────────────────────────

    [Fact]
    public void TK2_Arama_yer_tutucusu_Ekran_ara_PRO_da_kisayol_ipucu_BASIT_te_yok()
    {
        var basit = Menu(ArayuzModuServisi.Basit);
        Assert.Equal("Ekran ara", basit.Find("#menu-arama-girdi").GetAttribute("placeholder"));
        Assert.Empty(basit.FindAll("#menu-arama-kisayol"));
        // erişilebilir ad görsel-gizli etikette durur (üst etiket görünmez)
        Assert.Contains("gorsel-gizli", basit.Find("label[for=menu-arama-girdi]").ClassName, StringComparison.Ordinal);

        var pro = Menu(ArayuzModuServisi.Profesyonel);
        var kbd = pro.Find("#menu-arama-kisayol");
        Assert.Equal(Sozlukler.Tr.Kabuk.MenuAramaKisayol, kbd.TextContent.Trim());
        Assert.Equal(KabukTuvalSozlugu.KisayolIpucu("tr"), kbd.GetAttribute("title"));
    }

    [Fact]
    public void TK2_Aramada_kisayol_ipucu_yerini_temizle_dugmesine_birakir()
    {
        var e = Menu(ArayuzModuServisi.Profesyonel);
        e.Find("#menu-arama-girdi").Input("sat");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#menu-arama-temizle")));
        Assert.Empty(e.FindAll("#menu-arama-kisayol"));
    }

    // ── (4) PRO favorileri ───────────────────────────────────────────────────

    [Fact]
    public void TK2_PRO_favori_satiri_SARI_YILDIZ_tasir_ekran_ikonunu_degil()
    {
        tercihler = """{"menu-favoriler":"[\"/islemler\",\"/kur\"]"}""";
        var e = Menu(ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kenar-favori-satir-islemler")));
        var satir = e.Find("#kenar-favori-satir-islemler a");
        Assert.Contains("ikon-star", satir.InnerHtml, StringComparison.Ordinal);
        Assert.Contains("kenar-favori-yildiz", satir.InnerHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("ikon-coins", satir.InnerHtml, StringComparison.Ordinal);
        Assert.Equal(Sozlukler.Tr.Kabuk.Favoriler, e.Find("#kenar-grup-favoriler").TextContent.Trim());
    }

    // ── Bildirimler hapı ─────────────────────────────────────────────────────

    [Fact]
    public void TK2_Bildirim_sayisi_menu_satirinda_hap_sifirda_HIC_cizilmez()
    {
        var e = Menu(bildirim: 3);
        var rozet = e.Find("#kenar-bildirim-rozet");
        Assert.NotNull(rozet.Closest("a[href='/bildirimler']"));
        Assert.Equal("3", rozet.QuerySelector("[aria-hidden=true]")!.TextContent.Trim());
        Assert.Equal(KabukTuvalSozlugu.BildirimRozeti("tr", 3), rozet.GetAttribute("title"));

        var sifir = Menu(bildirim: 0);
        Assert.Empty(sifir.FindAll("#kenar-bildirim-rozet"));
    }

    [Fact]
    public void TK2_UstBar_yokladigi_bildirim_sayisini_kabuga_BILDIRIR()
    {
        bildirimSayisi = 4;
        Oturum(ArayuzModuServisi.Basit);
        var gelen = -1;
        var e = Render<UstBar>(p => p.Add(x => x.BildirimSayisiDegisti, (int n) => gelen = n));
        e.WaitForAssertion(() => Assert.Equal(4, gelen));
        Assert.Contains(istekler, i => i.Yol.EndsWith("/bildirimler/sayi", StringComparison.Ordinal));
        // Üst bardaki rozet DOM'da KALIR (dar ekran + testler); geniş ekranda CSS gizler.
        Assert.NotNull(e.Find("#ust-bildirim-rozet"));
    }

    [Fact]
    public void TK2_PRO_saat_ipucu_KAYNAK_adini_tasir_dar_masaustunde_gizlenen_yazi_kaybolmaz()
    {
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<UstBar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-saat")));
        var title = e.Find("#ust-kur-saat").GetAttribute("title") ?? "";
        Assert.EndsWith("\n" + KabukTuvalSozlugu.KurKaynagi("tr", Sozlukler.Tr.Kur.KaynakAltinkaynak), title, StringComparison.Ordinal);
        // Görünür kaynak yazısı DOM'da durur (1440'ta görünür; 769–1439'da CSS gizler).
        Assert.NotNull(e.Find("#ust-kur-kaynak"));
        Assert.Matches(@"\(max-width: 1439px\)[^@]*#ust-kur-kaynak \{ display: none; \}", KabukCss());
    }

    // ── (6) Kullanıcı kartı + dişli + kart menüsü ────────────────────────────

    [Fact]
    public void TK2_Kullanici_karti_Ramazan_A_ve_disli_AYARLARA_gider()
    {
        var e = Menu();
        Assert.Equal("Ramazan A.", e.Find(".kullanici-ad").TextContent.Trim());
        Assert.Equal("Ramazan Akkocan", e.Find(".kullanici-ad").GetAttribute("title"));
        var disli = e.Find("#kenar-ayarlar-disli");
        Assert.Equal("/ayarlar", disli.GetAttribute("href"));
        Assert.Equal(KabukTuvalSozlugu.AyarlarDisli("tr"), disli.GetAttribute("aria-label"));
        Assert.Contains("ikon-settings", disli.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void TK2_Kart_dugmesi_menuyu_ACAR_KAPATIR_Esc_kapatir()
    {
        var e = Menu();
        var menu = () => e.Find("#kenar-kullanici-menu");
        Assert.True(menu().HasAttribute("hidden"));
        Assert.Equal("false", e.Find("#kenar-kullanici-menu-ac").GetAttribute("aria-expanded"));

        e.Find("#kenar-kullanici-menu-ac").Click();
        Assert.False(menu().HasAttribute("hidden"));
        Assert.Equal("true", e.Find("#kenar-kullanici-menu-ac").GetAttribute("aria-expanded"));

        e.Find("#kenar-kullanici-menu-ac").Click();
        Assert.True(menu().HasAttribute("hidden"));

        e.Find("#kenar-kullanici-menu-ac").Click();
        e.Find(".kenar-alt").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.True(menu().HasAttribute("hidden"));
    }

    [Fact]
    public void TK2_Kart_menusu_TASINANLARI_tasir_kilavuz_ayarlar_cikis_ve_dil_CALISIR()
    {
        var e = Menu();
        var menu = e.Find("#kenar-kullanici-menu");
        Assert.NotNull(menu.QuerySelector("#kenar-alt-kilavuz"));
        Assert.NotNull(menu.QuerySelector("#kenar-alt-ayarlar"));
        Assert.NotNull(menu.QuerySelector("#kenar-cikis"));
        // Dil seçici gerçekten dili değiştirir (DoD EK md.1: etkisiz düğme yok).
        var en = e.FindAll("#kenar-kullanici-menu .dil-secici button").Single(b => b.TextContent.Trim() == "EN");
        en.Click();
        Assert.Equal("en", Services.GetRequiredService<DilServisi>().Dil);
        Assert.Equal("Find screen", e.Find("#menu-arama-girdi").GetAttribute("placeholder"));
    }

    [Fact]
    public void TK2_Cikis_dugmesi_oturumu_kapatir_ve_girise_gider()
    {
        var e = Menu();
        e.Find("#kenar-cikis").Click();
        e.WaitForAssertion(() =>
            Assert.EndsWith("/giris", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal));
    }

    [Fact]
    public void TK2_Katlama_sifirla_menunun_dibinden_KART_MENUSUNE_tasindi()
    {
        tercihler = """{"menu-acik-gruplar":"[\"gunluk\"]"}""";
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kenar-katlama-sifirla")));
        Assert.NotNull(e.Find("#kenar-katlama-sifirla").Closest("#kenar-kullanici-menu"));
        Assert.Null(e.Find("#kenar-katlama-sifirla").Closest("#kenar-nav"));
    }

    [Fact]
    public void TK2_Arama_ALT_BLOKTAN_eslesirse_kart_menusu_acik_gorunur()
    {
        var e = Menu();
        e.Find("#menu-arama-girdi").Input("Ayarlar");
        e.WaitForAssertion(() => Assert.False(e.Find("#kenar-kullanici-menu").HasAttribute("hidden")));
        Assert.False(e.Find("#kenar-alt-ayarlar").HasAttribute("hidden"));
        Assert.True(e.Find("#kenar-alt-kilavuz").HasAttribute("hidden"));
        e.Find("#menu-arama-temizle").Click();
        Assert.True(e.Find("#kenar-kullanici-menu").HasAttribute("hidden"));
    }

    // ── Şube seçici: üst bardan kart menüsüne ────────────────────────────────

    [Fact]
    public void TK2_Coklu_subede_secici_KART_MENUSUNDE_ust_barda_YOK()
    {
        subeSayisi = 3;
        var e = Menu(ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-kullanici-menu #ust-sube-secim")));

        Oturum(ArayuzModuServisi.Profesyonel);
        var ust = Render<UstBar>();
        Assert.Empty(ust.FindAll("#ust-sube-secici"));
        Assert.Empty(ust.FindAll("#ust-sube-secim"));
    }

    [Fact]
    public void TK2_Sube_listesi_okunamazsa_kart_menusu_KENDILIGINDEN_acik_hata_gorunur()
    {
        subeHata = true;
        var e = Menu(ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-kullanici-menu #ust-sube-hata")));
        Assert.False(e.Find("#kenar-kullanici-menu").HasAttribute("hidden"));
        Assert.Equal("true", e.Find("#kenar-kullanici-menu-ac").GetAttribute("aria-expanded"));
    }

    // ── (7) Üst bar: sayfa sekmeleri yuvası ─────────────────────────────────

    [Fact]
    public void TK2_Sayfa_sekmeleri_yuvasi_icerigi_UST_BARDA_cizer_sayfa_kapaninca_kalkar()
    {
        Oturum(ArayuzModuServisi.Basit);
        var yuva = new UstBarSekmeYuvasi();
        var goster = true;
        void Parametre(ComponentParameterCollectionBuilder<CascadingValue<UstBarSekmeYuvasi>> p) => p
            .Add(x => x.Value, yuva)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<UstBar>(0);
                b.CloseComponent();
                if (goster)
                {
                    b.OpenComponent<UstBarSekmeleri>(1);
                    b.AddAttribute(2, "ChildContent", (RenderFragment)(c => c.AddMarkupContent(0, "<span id=\"sekme-deneme\">Görünüm</span>")));
                    b.CloseComponent();
                }
            }));
        var kok = Render<CascadingValue<UstBarSekmeYuvasi>>(Parametre);
        kok.WaitForAssertion(() => Assert.NotNull(kok.Find("#ust-sayfa-sekmeleri #sekme-deneme")));
        // İçerik YALNIZ üst barda (yerinde ikinci kopya yok).
        Assert.Single(kok.FindAll("#sekme-deneme"));
        Assert.NotNull(kok.Find("#sekme-deneme").Closest("header.ust-bar"));

        goster = false;
        kok.Render(Parametre);
        kok.WaitForAssertion(() => Assert.Empty(kok.FindAll("#ust-sayfa-sekmeleri")));
    }

    [Fact]
    public void TK2_Yuva_yoksa_sekmeler_YERINDE_cizilir_ozellik_kaybolmaz()
    {
        var e = Render<UstBarSekmeleri>(p => p.AddChildContent("<span id=\"yerinde\">Kur</span>"));
        Assert.NotNull(e.Find("#yerinde"));
    }

    // ── CSS kilitleri (tuval ölçüleri; görünüm tarayıcıda ölçüldü) ───────────

    [Fact]
    public void TK2_CSS_tuval_olculeri_ve_BELIRTEC_disi_renk_YOK()
    {
        var blok = Tk2Blogu();
        // Tuval Panel.dc.html <nav>: padding 20px 14px · marka kutusu 34 px / 9 px · Sora 21
        Assert.Matches(@"\.kenar-cubugu \{[^}]*padding: 20px 14px", blok);
        Assert.Matches(@"\.kenar-marka-rozet \{[^}]*width: 34px; height: 34px; border-radius: 9px", blok);
        Assert.Matches(@"\.kenar-logo \.marka \{[^}]*font-size: 21px; font-weight: 700", blok);
        // Menü ekran boyunda (kart dipte) — yalnız geniş ekranda.
        Assert.Matches(@"@media \(min-width: 769px\) \{\s*\.kenar-cubugu \{[^}]*position: sticky; top: 0;[^}]*height: 100vh", blok);
        // PRO: satırlar ikonsuz ve önizlemesiz (tuval Profesyonel.dc.html).
        Assert.Contains("html[data-mod=\"profesyonel\"] .kenar-cubugu .kenar-grup-onizleme { display: none; }", blok, StringComparison.Ordinal);
        Assert.Contains("a > .ikon { display: none; }", blok, StringComparison.Ordinal);
        // Sabit renk YOK: hex / rgb( / hsl( yalnız yorumda olabilir.
        var kod = Regex.Replace(blok, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", kod);
        Assert.DoesNotMatch(@"\b(rgb|rgba|hsl|hsla)\(", kod);
    }

    [Fact]
    public void TK2_CSS_ust_barda_kayit_arama_dugmesi_ve_zil_GENIS_ekranda_gizli_HATA_rozeti_kalir()
    {
        var blok = Tk2Blogu();
        var m = Regex.Match(blok, @"@media \(min-width: 769px\) \{(?<ic>(?:[^{}]|\{[^{}]*\})*)\}\s*/\* T-K2 ölçüm", RegexOptions.Singleline);
        Assert.True(m.Success, "üst bar gizleme bloğu bulunamadı");
        var ic = m.Groups["ic"].Value;
        Assert.Contains(".ust-bar .kayit-arama-dugme { display: none; }", ic, StringComparison.Ordinal);
        Assert.Contains(".ust-bar .bildirim-rozet:not(.bildirim-rozet-hata) { display: none; }", ic, StringComparison.Ordinal);
        // Palet kapalıyken boş kap da yer (flex boşluğu) kaplamaz — 1280 PRO ölçümü.
        Assert.Contains(".ust-bar .kayit-arama:not(:has(.kayit-arama-palet)) { display: none; }", blok, StringComparison.Ordinal);
    }
}
