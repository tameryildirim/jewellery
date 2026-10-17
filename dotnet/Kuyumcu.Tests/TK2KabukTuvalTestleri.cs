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
    private int bildirimSayisi;
    private string tercihler = "{}";

    private sealed class SahteApi(TK2KabukTuvalTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol));
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

    // ── Bildirimler hapı ─────────────────────────────────────────────────────

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

    // ── Şube seçici: üst bardan kart menüsüne ────────────────────────────────

    [Fact]
    public void TK2_Coklu_subede_secici_ust_barda_YOK()
    {
        // Tema temizliği (17.10): kenar çubuğu kart menüsü kabukla kalktı; Yıldız'da şube seçici üst
        // menünün kullanıcı penceresindedir (Yildiz testleri). Burada yalnız üst barda OLMADIĞI kilitlenir.
        subeSayisi = 3;
        Oturum(ArayuzModuServisi.Profesyonel);
        var ust = Render<UstBar>();
        Assert.Empty(ust.FindAll("#ust-sube-secici"));
        Assert.Empty(ust.FindAll("#ust-sube-secim"));
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
    public void TK2_CSS_BELIRTEC_disi_renk_YOK_ve_kenar_cubugu_kurali_KALMADI()
    {
        var blok = Tk2Blogu();
        // Tema temizliği (17.10): eski kenar çubuğu (KenarCubugu) kurallarının hiçbiri kalmadı.
        var kod = Regex.Replace(blok, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"[.#]kenar-[a-z]", kod);
        // Sabit renk YOK: hex / rgb( / hsl( yalnız yorumda olabilir.
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
