using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K3 — AYARLAR TUVALE (tuval Ayarlar.dc.html · kurucu 24.09 17:20)
//
// Kilitler:
//   (1) Sayfanın kendi başlık/açıklama/"Profili düzenle" bloğu YOK; başlık üst
//       barda. #ay-profil-duzenle SİLİNMEDİ — profil kartında, ikincil, çekmeceyi açar.
//   (2) Sekme şeridi ÜST BAR YUVASINDA (#ust-sayfa-sekmeleri), sayfada ikinci kopya
//       yok; üst bardaki sekmeye tıklamak sayfadaki paneli değiştirir.
//   (3) Eski derin bağlantı (?sekme=hesap|sistem|destek) Güvenlik'e yönlenir.
//   (4) Dükkân · Kur · Yazıcı: ayarın yaşadığı ekrana bağlantı; yetki yoksa
//       bağlantı YOK, sebep yazılı (UX-S30). Uydurma ayar yok.
//   (5) Sözlük TR+EN tam; (6) CSS: tuval ölçüleri, belirteç dışı renk yok.
// bUnit CSS uygulamaz: görünüm/sığma ölçümü rapordaki tarayıcı tablosudur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TK3AyarlarTuvalTestleri : BunitContext
{
    private readonly List<string> istekler = [];

    public TK3AyarlarTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<SurumServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private sealed class SahteApi(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add($"{istek.Method.Method} {yol}");
            // ⚠ bUnit sözleşmesi: her uca GERÇEKÇİ yanıt.
            var g = yol switch
            {
                var x when x.EndsWith("/destek/paket/gecmis") =>
                    """{"kanalAcik":true,"sinir":50,"dahaFazlasiVar":false,"asgariGerekce":20,"konular":["KURULUM","HATA","DIGER"],"kayitlar":[]}""",
                var x when x.EndsWith("/meta/surum") =>
                    """{"surum":"1.0.0","derlemeZamani":"2026-09-24T06:04:00Z","derlemeZamaniKaynagi":"damga","ortam":"Production","migrationSayisi":71,"sonMigration":"20260924004513_P77","bekleyenMigrationSayisi":0}""",
                var x when x.Contains("/mfa/durum") =>
                    """{"etkin":false,"kurulumBekliyor":false,"kilitli":false,"kalanKurtarma":0}""",
                // TEZGAHTAR: kur ekranının modülü (islem) VAR, İşletme SAHİP'e özel.
                var x when x.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var x when x.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var x when x.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                var x when x.EndsWith("/bildirimler/sayi", StringComparison.Ordinal) => """{"sayi":0,"enYuksekOnem":null}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    private void Oturum(string rol) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1"));

    private IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> AyarCiz(string rol = "SAHIP", string uri = "/ayarlar")
    {
        Nav.NavigateTo(uri);
        Oturum(rol);
        return Render<Kuyumcu.Web.Pages.Ayarlar>();
    }

    /// <summary>Kabuk benzeri: aynı yuvayı alan UstBar + Ayarlar (AnaYerlesim'in cascade'i).</summary>
    private IRenderedComponent<CascadingValue<UstBarSekmeYuvasi>> KabuklaCiz(string rol = "SAHIP", string uri = "/ayarlar")
    {
        Nav.NavigateTo(uri);
        Oturum(rol);
        var yuva = new UstBarSekmeYuvasi();
        return Render<CascadingValue<UstBarSekmeYuvasi>>(p => p
            .Add(x => x.Value, yuva)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<UstBar>(0);
                b.CloseComponent();
                b.OpenComponent<Kuyumcu.Web.Pages.Ayarlar>(1);
                b.CloseComponent();
            })));
    }

    // ── (1) Sayfa başı bloğu kalktı; Profili düzenle profil kartında ─────────

    [Fact]
    public void TK3_01_Sayfa_basi_blogu_YOK_Profili_duzenle_profil_kartinda_ikincil_cekmeceyi_acar()
    {
        var c = AyarCiz();
        // SayfaBasi bileşeni (başlık + açıklama + eylem) çizilmez; sayfada <h1> yok (başlık üst barda).
        Assert.Empty(c.FindAll(".sayfa-basi"));
        Assert.Empty(c.FindAll("h1"));
        Assert.DoesNotContain(Dil().T.Yonetim.AyarlarAciklama, c.Markup);

        var dugme = c.Find("#ay-profil-duzenle");
        Assert.NotNull(dugme.Closest("#ayar-profil"));
        Assert.NotNull(dugme.Closest("#ayar-panel-guvenlik"));
        Assert.Contains("btn-ikincil", dugme.ClassName);
        Assert.DoesNotContain("btn-birincil", dugme.ClassName);
        // Ekran başına TEK birincil düğme: Görünüm'deki "Bunu dükkân varsayılanı yap".
        Assert.Equal(["gk-varsayilan-yap"], c.FindAll("#ayar-panel-gorunum .btn-birincil").Select(b => b.Id ?? "").ToArray());

        Assert.Empty(c.FindAll("#pr-ad"));
        dugme.Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pr-ad")));
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();

    // ── (2) Sekmeler üst bar yuvasında ──────────────────────────────────────

    [Fact]
    public void TK3_02_Sekme_seridi_UST_BARDA_tek_kopya_tiklama_sayfadaki_paneli_degistirir()
    {
        var kok = KabuklaCiz();
        kok.WaitForAssertion(() => Assert.NotNull(kok.Find("#ust-sayfa-sekmeleri #ayar-sekme-seridi")));
        // Şerit YALNIZ üst barda: sayfada ikinci kopya yok.
        Assert.Single(kok.FindAll("#ayar-sekme-seridi"));
        Assert.NotNull(kok.Find("#ayar-sekme-seridi").Closest("header.ust-bar"));
        // Paneller sayfada (üst barda panel yok).
        Assert.Null(kok.Find("#ayar-panel-gorunum").Closest("header"));
        Assert.Equal(["Görünüm", "İşletme", "Kur", "Yazıcı", "Güvenlik"],
            kok.FindAll("#ust-sayfa-sekmeleri [role=tab]").Select(s => s.TextContent.Trim()).ToArray());

        kok.Find("#ayar-sekme-dukkan").Click();
        kok.WaitForAssertion(() =>
        {
            Assert.Contains("sekme=dukkan", Nav.Uri);
            Assert.False(kok.Find("#ayar-panel-dukkan").HasAttribute("hidden"));
            Assert.True(kok.Find("#ayar-panel-gorunum").HasAttribute("hidden"));
            // Üst bardaki seçili sekme de güncellenir (yuva yeniden çizer).
            Assert.Equal("true", kok.Find("#ayar-sekme-dukkan").GetAttribute("aria-selected"));
            Assert.Equal("false", kok.Find("#ayar-sekme-gorunum").GetAttribute("aria-selected"));
        });
    }

    [Fact]
    public void TK3_03_Yuva_yoksa_serit_YERINDE_diger_sekmeli_sayfalar_etkilenmez()
    {
        // Ayarlar kabuk dışında (yuva yok) → şerit sayfanın kendi yerinde, kaybolmaz.
        var c = AyarCiz();
        Assert.Single(c.FindAll("#ayar-sekme-seridi"));
        // UstBarda varsayılanı HAYIR: diğer sayfalar (Uyum, Kur…) şeridi yerinde çizer.
        Assert.False(new Sekmeler().UstBarda);
    }

    // ── (3) Eski derin bağlantılar ──────────────────────────────────────────

    [Theory]
    [InlineData("hesap")]
    [InlineData("sistem")]
    [InlineData("destek")]
    public void TK3_04_Eski_sekme_kodu_GUVENLIK_sekmesine_yonlenir(string eski)
    {
        var c = AyarCiz(uri: $"/ayarlar?sekme={eski}");
        c.WaitForAssertion(() =>
        {
            Assert.Contains("sekme=guvenlik", Nav.Uri);
            Assert.False(c.Find("#ayar-panel-guvenlik").HasAttribute("hidden"));
            Assert.True(c.Find("#ayar-panel-gorunum").HasAttribute("hidden"));
        });
    }

    [Fact]
    public void TK3_04b_Sayfa_ICI_gezinmede_eski_kod_da_GUVENLIGE_yonlenir()
    {
        var c = AyarCiz(uri: "/ayarlar?sekme=dukkan");
        Nav.NavigateTo("/ayarlar?sekme=sistem");
        c.WaitForAssertion(() =>
        {
            Assert.Contains("sekme=guvenlik", Nav.Uri);
            Assert.False(c.Find("#ayar-panel-guvenlik").HasAttribute("hidden"));
        });
    }

    [Fact]
    public void TK3_05_Bilinmeyen_kod_yonlendirilmez_ilk_sekme()
    {
        var c = AyarCiz(uri: "/ayarlar?sekme=uydurma");
        Assert.Contains("sekme=uydurma", Nav.Uri);
        Assert.False(c.Find("#ayar-panel-gorunum").HasAttribute("hidden"));
    }

    // ── (4) Dükkân · Kur · Yazıcı ───────────────────────────────────────────

    [Fact]
    public void TK3_06_SAHIP_Dukkan_Kur_Yazici_kartlari_GERCEK_ekranlara_baglanir()
    {
        var c = AyarCiz();
        var t = AyarlarTuvalSozlugu.Tr;
        Assert.Equal("/isletme", c.Find("#ayar-panel-dukkan #ayar-dukkan-bilgi-git").GetAttribute("href"));
        Assert.Equal("/kur", c.Find("#ayar-panel-kur #ayar-kur-bilgi-git").GetAttribute("href"));
        Assert.Equal("/urunler/etiketler", c.Find("#ayar-panel-yazici #ayar-yazici-bilgi-git").GetAttribute("href"));
        Assert.Equal(t.DukkanAciklama, c.Find("#ayar-dukkan-bilgi-aciklama").TextContent.Trim());
        Assert.Equal(t.KurAciklama, c.Find("#ayar-kur-bilgi-aciklama").TextContent.Trim());
        // Doğrulama bulgusu: Ayarlar üst barında kur şeridi (saat) ≥769 px'te gizli →
        // metin BU ekranın üst barını değil diğer ekranların üst barını gösterir (TR+EN).
        Assert.Contains("diğer ekranların üst barındaki saat", t.KurAciklama);
        Assert.DoesNotContain("üst bardaki saat", t.KurAciklama);
        Assert.Contains("top bar of the other screens", AyarlarTuvalSozlugu.En.KurAciklama);
        Assert.Equal(t.YaziciAciklama, c.Find("#ayar-yazici-bilgi-aciklama").TextContent.Trim());
        // Kâğıt notu Görünüm kartıyla TEK kaynaktan.
        Assert.Equal(GorunumKartiSozlugu.Al("tr").KagitNotu, c.Find("#ayar-yazici-bilgi-not").TextContent.Trim());
        // Dükkânın görünüm varsayılanı kartı (S1-f #ayar-gorunum) Dükkân sekmesinde.
        Assert.NotNull(c.Find("#ayar-gorunum").Closest("#ayar-panel-dukkan"));
        // Bu üç sekmede ölü düğme yok: yalnız bağlantı + dükkân görünüm seçicileri.
        Assert.Empty(c.FindAll("#ayar-panel-kur button, #ayar-panel-yazici button"));
        Assert.Empty(c.FindAll("[id$='-yetki-yok']"));
    }

    [Fact]
    public async Task TK3_07_TEZGAHTAR_Isletme_baglantisi_YOK_sebep_yazili_Kur_baglantisi_modulden()
    {
        Oturum("TEZGAHTAR");
        await Services.GetRequiredService<YetkiServisi>().HazirlaAsync();
        var c = AyarCiz("TEZGAHTAR");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#ayar-kur-bilgi-git")));
        Assert.Empty(c.FindAll("#ayar-dukkan-bilgi-git"));
        Assert.Equal(AyarlarTuvalSozlugu.Tr.DukkanYetkiYok, c.Find("#ayar-dukkan-bilgi-yetki-yok").TextContent.Trim());
        Assert.Equal("/kur", c.Find("#ayar-kur-bilgi-git").GetAttribute("href"));
        Assert.Equal("/urunler/etiketler", c.Find("#ayar-yazici-bilgi-git").GetAttribute("href"));
    }

    [Fact]
    public void TK3_08_Yetki_servisi_YUKLENMEDEN_TEZGAHTAR_baglanti_gormez_sebep_gorur()
    {
        // Modül listesi henüz okunmadıysa yetki VARSAYILMAZ (tutucu).
        var c = AyarCiz("TEZGAHTAR");
        Assert.Empty(c.FindAll("#ayar-kur-bilgi-git"));
        Assert.Equal(AyarlarTuvalSozlugu.Tr.YetkiYok, c.Find("#ayar-kur-bilgi-yetki-yok").TextContent.Trim());
    }

    [Fact]
    public void TK3_09_Guvenlik_alt_bolumleri_SIRASI_profil_sifre_mfa_kunye_destek()
    {
        var c = AyarCiz();
        var sira = c.FindAll("#ayar-panel-guvenlik [id]").Select(e => e.Id ?? "")
            .Where(id => id is "ayar-profil" or "ay-sifre-guncelle" or "ayar-mfa" or "ayar-kunye" or "ayar-destek").ToArray();
        Assert.Equal(["ayar-profil", "ay-sifre-guncelle", "ayar-mfa", "ayar-kunye", "ayar-destek"], sira);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit, false)]
    [InlineData(ArayuzModuServisi.Profesyonel, true)]
    public void TK3_13_Sistem_ve_destek_tuvalde_yok_BASITte_KATLI_PROda_ACIK_silinmez(string mod, bool acik)
    {
        Nav.NavigateTo("/ayarlar?sekme=guvenlik");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", null, mod));
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        var d = c.Find("details#ayar-sistem-destek");
        Assert.Equal(acik, d.HasAttribute("open"));
        Assert.Equal(AyarlarTuvalSozlugu.Tr.SistemDestekBaslik, c.Find("#ayar-sistem-destek-baslik").TextContent.Trim());
        foreach (var id in new[] { "ayar-panel-govde-sistem", "ayar-kunye", "ayar-panel-govde-destek", "ayar-destek" })
            Assert.NotNull(d.QuerySelector($"#{id}"));
        // Dükkân sekmesindeki "tek tek seç" kartı da aynı kuralla (tuvalde yok).
        var dd = c.Find("details#ayar-dukkan-gorunum-ayrinti");
        Assert.Equal(acik, dd.HasAttribute("open"));
        Assert.Equal(AyarlarTuvalSozlugu.Tr.DukkanGorunumBaslik, c.Find("#ayar-dukkan-gorunum-ayrinti-baslik").TextContent.Trim());
        foreach (var id in new[] { "ayar-gorunum", "gorunum-sema", "gorunum-olcek" })
            Assert.NotNull(dd.QuerySelector($"#{id}"));
        Assert.Null(dd.QuerySelector("#ayar-dukkan-bilgi"));
        // Profil, şifre ve iki aşamalı giriş katlı bölümün DIŞINDA (tuvalin Güvenlik'i).
        foreach (var id in new[] { "ayar-profil", "ay-sifre-guncelle", "ayar-mfa" })
            Assert.Null(d.QuerySelector($"#{id}"));
    }

    // ── (5) Sözlük ──────────────────────────────────────────────────────────

    [Fact]
    public void TK3_10_Sozluk_TR_EN_TAM_sekme_adlari_tuvaldeki_gibi()
    {
        foreach (var m in new[] { AyarlarTuvalSozlugu.Tr, AyarlarTuvalSozlugu.En })
            foreach (var p in typeof(AyarlarTuvalMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        foreach (var p in typeof(AyarlarTuvalMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
            Assert.NotEqual(p.GetValue(AyarlarTuvalSozlugu.Tr), p.GetValue(AyarlarTuvalSozlugu.En));

        var tr = AyarlarSekmeSozlugu.Tr;
        Assert.Equal(["Görünüm", "İşletme", "Kur", "Yazıcı", "Güvenlik"], new[] { tr.Gorunum, tr.Dukkan, tr.Kur, tr.Yazici, tr.Guvenlik });
        var en = AyarlarSekmeSozlugu.En;
        Assert.Equal(["Appearance", "Shop", "Rates", "Printer", "Security"], new[] { en.Gorunum, en.Dukkan, en.Kur, en.Yazici, en.Guvenlik });
        Assert.Equal(AyarlarTuvalSozlugu.En, AyarlarTuvalSozlugu.Al("en"));
        Assert.Equal(AyarlarTuvalSozlugu.Tr, AyarlarTuvalSozlugu.Al("tr"));
    }

    // ── (6) CSS kilitleri ───────────────────────────────────────────────────

    private static string Tk3Blogu()
    {
        var css = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-kabuk.css"));
        var i = css.IndexOf("PAKET T-K3", StringComparison.Ordinal);
        Assert.True(i >= 0, "tuval-kabuk.css'te T-K3 bloğu YOK");
        var blok = css[css.LastIndexOf("/*", i, StringComparison.Ordinal)..];
        return Regex.Replace(blok, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    [Fact]
    public void TK3_11_CSS_ust_bar_sekme_hapi_tuval_olculeri_ve_sekmeli_ust_barda_kur_seridi_mod_hapi_gizli()
    {
        var b = Tk3Blogu();
        // Tuval: kap 3 px dolgu, 4 px ara, 1 px ayraç çerçeve, 999 px; sekme 6/14 px, 15 px.
        Assert.Matches(@"\.ust-bar \.ust-sayfa-sekmeleri \.sekmeler-serit \{[^}]*gap: 4px;[^}]*padding: 3px;[^}]*border: 1px solid var\(--ayrac\);[^}]*border-radius: 999px;", b);
        Assert.Matches(@"\.ust-bar \.ust-sayfa-sekmeleri \.sekmeler-sekme \{[^}]*padding: 6px 14px;[^}]*font-size: 15px;", b);
        Assert.Matches(@"\.sekmeler-sekme\.secili \{[^}]*background: var\(--kenar-yuzey\);[^}]*color: var\(--kenar-metin-guclu\);", b);
        // Doğrulama bulgusu (koyu tema): --kenar-yuzey bardan koyu kalıyordu → karanlıkta
        // seçili hap vurgu yüzeyi + vurgu kenarı taşır; HEM sistem karanlığı HEM zorlanmış karanlık.
        Assert.Matches(@"@media \(prefers-color-scheme: dark\) \{\s*:root:not\(\[data-tema=""aydinlik""\]\) \.ust-bar \.ust-sayfa-sekmeleri \.sekmeler-sekme\.secili \{[^}]*background: var\(--vurgu-yuzey\);[^}]*color: var\(--vurgu-guclu\);[^}]*box-shadow: inset 0 0 0 1px var\(--vurgu-kenar\);", b);
        Assert.Matches(@":root\[data-tema=""karanlik""\] \.ust-bar \.ust-sayfa-sekmeleri \.sekmeler-sekme\.secili \{[^}]*background: var\(--vurgu-yuzey\);[^}]*color: var\(--vurgu-guclu\);[^}]*box-shadow: inset 0 0 0 1px var\(--vurgu-kenar\);", b);
        // Tuval Ayarlar üst barında kur şeridi ve mod hapı YOK (yalnız sekmeli üst barda gizli).
        Assert.Matches(@"\.ust-bar:has\(> #ust-sayfa-sekmeleri\) > \.ust-kur-serit,\s*\.ust-bar:has\(> #ust-sayfa-sekmeleri\) #ust-mod-anahtar \{ display: none; \}", b);
        Assert.Contains(".ust-bar > .ust-sayfa-sekmeleri { margin-right: auto; }", b);
        // Tuval <main> dolgusu 26/28 px.
        Assert.Contains(".ana-icerik:has(.ayarlar-sayfa) { padding: 26px 28px; }", b);
        // Kaynak rozeti tuvalde yok → BASİT'te gizli, PRO'da görünür (silinmez).
        Assert.Contains("html:not([data-mod=\"profesyonel\"]) .gk .gk-kaynak { display: none; }", b);
        // Alt satır tek sıra (düğmeler alta düşmez).
        Assert.Contains(".gk .gk-alt { flex-wrap: nowrap; }", b);
        // Dil düğmesi ("TR Türkçe") kısa etikettir — bölünmez (K3 ölçümü 1440: 2 kırık).
        Assert.Matches(@"#ayar-dil \.sekme \{[^}]*white-space: nowrap;", b);
        // 375 px: üst bardaki hap tek sıra, 14 px (S1-h 2 sütun ızgarası hapta bozuk).
        Assert.Matches(@"@media \(max-width: 600px\) \{\s*\.ust-bar \.ust-sayfa-sekmeleri \.sekmeler-serit \{[^}]*display: flex;", b);
    }

    [Fact]
    public void TK3_12_CSS_BELIRTEC_disi_renk_YOK_satir_ici_stil_YOK()
    {
        var b = Tk3Blogu();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", b);
        Assert.DoesNotMatch(@"rgba?\(", b);
        var razor = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Pages", "Ayarlar.razor"));
        Assert.DoesNotContain("style=\"", razor);
        Assert.DoesNotContain("<SayfaBasi", razor);
    }
}
