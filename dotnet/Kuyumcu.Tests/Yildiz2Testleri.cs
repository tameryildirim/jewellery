using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-2 — "Yıldız" KABUĞU: üst 5 kelime menü + "Tüm ekranlar" kareleri
// Kaynak: TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md § 4 D · tuval Bugun4 / Tum4.
//
// Kilitlenen sözleşmeler:
//   • Beş kelime, sıra ve hedefler; seçili öğe aria-current + altı çizili sınıf.
//   • YETKİ: üst menü sol menüyle AYNI kaynaktan süzülür (TEZGAHTAR → kısa menü).
//   • 375 px: tek "Menü" düğmesi (aria-expanded/controls), Esc kapatır.
//   • "Tüm ekranlar": 39 kare, her karede ikon + açıklama (yalnız Yıldız'da);
//     başka şemada eski markup (Günlük iş dışında ikon yok, açıklama yok).
//   • Hesap menüsünde dil/şube/Kılavuz/Ayarlar/Çıkış KAYBOLMAZ.
//   • CSS: yeni kurallar Yıldız kapsamında; gizli menü [hidden] ile görünmez;
//     hover açıklaması visibility ile saklanır; 200 ağırlık yok.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Yildiz2Testleri : BunitContext
{
    private string moduller = "[]";
    private string sema = "yildiz";

    private sealed class SahteApi(Func<string> moduller, Func<string> sema) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/yetkiler/benim") => $$"""{"moduller":{{moduller()}}}""",
                var y when y.EndsWith("/tercihler/benim") => """{"tercihler":{},"mesaj":"ok"}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","tenantId":"t1","altinHasFiyati":6877,"usdTry":41.95,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-08T09:02:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22","milyem":916,"gramFiyati":5555,"hasFiyati":6877}]}""",
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{sema()}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.Contains("/subeler") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public Yildiz2Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => moduller, () => sema))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private void Oturum(string rol) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", rol, "s1", null, ArayuzModuServisi.Profesyonel));

    private IRenderedComponent<YildizUstMenu> Menu(string rol = "SAHIP", string rota = "/")
    {
        Oturum(rol);
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        return Render<YildizUstMenu>();
    }

    private static string CssOku(string ad) => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", ad);

    // ── Saf kurallar ───────────────────────────────────────────────────────

    [Fact]
    public void Y2_01_bes_kelime_sirasi_ve_hedefleri_tuvaldeki_gibidir()
    {
        Assert.Equal(["Bugün", "Tezgâh", "Müşteriler", "Kasa", "Tüm ekranlar"],
            YildizKabuk.UstMenu.Select(o => o.Ad("tr")).ToList());
        Assert.Equal(["/", "/islemler", "/cariler", "/kasalar", "/ekranlar"],
            YildizKabuk.UstMenu.Select(o => o.Yol).ToList());
        Assert.Equal(["Today", "Counter", "Customers", "Cash", "All screens"],
            YildizKabuk.UstMenu.Select(o => o.Ad("en")).ToList());
    }

    [Fact]
    public void Y2_02_her_menu_ekraninin_TR_ve_EN_aciklamasi_vardir_ve_sayi_39dur()
    {
        // 39 → 41: PAKET EKSIK-B1-SATINALMA (/satin-alma + /irsaliyeler; TR+EN açıklama eklendi).
        Assert.Equal(41, MenuTanimi.Ogeler.Count);
        foreach (var o in MenuTanimi.Ogeler)
        {
            Assert.False(string.IsNullOrWhiteSpace(YildizSozlugu.Aciklama(o.Yol, "tr")), o.Yol);
            Assert.False(string.IsNullOrWhiteSpace(YildizSozlugu.Aciklama(o.Yol, "en")), o.Yol);
        }
        Assert.Equal(MenuTanimi.Ogeler.Count, YildizSozlugu.AciklamaYollari.Count);
    }

    [Fact]
    public void Y2_03_yetki_suzgeci_sol_menuyle_ayni_kaynaktan_Tum_ekranlar_hep_var()
    {
        var hicbiri = YildizKabuk.UstMenu.Where(o => YildizKabuk.Gorunur(o, _ => false, "TEZGAHTAR")).Select(o => o.Yol).ToList();
        Assert.Equal(["/", "/ekranlar"], hicbiri);
        var hepsi = YildizKabuk.UstMenu.Where(o => YildizKabuk.Gorunur(o, _ => true, "SAHIP")).ToList();
        Assert.Equal(5, hepsi.Count);
    }

    [Fact]
    public void Y2_04_aktif_oge_tam_ya_da_onek_eslesmesiyle_bulunur()
    {
        var bugun = YildizKabuk.UstMenu[0];
        var tezgah = YildizKabuk.UstMenu[1];
        Assert.True(YildizKabuk.Aktif(bugun, "/"));
        Assert.False(YildizKabuk.Aktif(bugun, "/islemler"));
        Assert.True(YildizKabuk.Aktif(tezgah, "/islemler"));
        Assert.True(YildizKabuk.Aktif(tezgah, "/islemler/abc"));
        Assert.False(YildizKabuk.Aktif(tezgah, "/islemlerx"));
    }

    // ── bUnit: üst menü ────────────────────────────────────────────────────

    [Fact]
    public void Y2_10_ust_menu_bes_baglanti_secili_olan_aria_current_tasir()
    {
        var e = Menu("SAHIP", "/cariler");
        var l = e.FindAll("#yildiz-menu a.yildiz-menu-oge");
        Assert.Equal(5, l.Count);
        Assert.Equal(["/", "/islemler", "/cariler", "/kasalar", "/ekranlar"], l.Select(a => a.GetAttribute("href")).ToList());
        Assert.Equal(["Bugün", "Tezgâh", "Müşteriler", "Kasa", "Tüm ekranlar"], l.Select(a => a.TextContent.Trim()).ToList());
        var secili = e.FindAll("#yildiz-menu a[aria-current=page]");
        Assert.Single(secili);
        Assert.Equal("yildiz-menu-musteriler", secili[0].Id);
        Assert.Contains("aktif", secili[0].ClassName);
        Assert.Empty(e.FindAll("#kenar-cubugu"));
    }

    [Fact]
    public void Y2_11_TEZGAHTAR_modulsuz_yalniz_Bugun_ve_Tum_ekranlar_gorur()
    {
        moduller = "[]";
        var e = Menu("TEZGAHTAR", "/");
        e.WaitForAssertion(() => Assert.Equal(["Bugün", "Tüm ekranlar"],
            e.FindAll("#yildiz-menu a.yildiz-menu-oge").Select(a => a.TextContent.Trim()).ToList()));
    }

    [Fact]
    public void Y2_12_menu_dugmesi_aria_expanded_degistirir_Esc_kapatir()
    {
        var e = Menu();
        var d = e.Find("#yildiz-menu-ac");
        Assert.Equal("false", d.GetAttribute("aria-expanded"));
        Assert.Equal("yildiz-menu", d.GetAttribute("aria-controls"));
        Assert.DoesNotContain("acik", e.Find("#yildiz-menu").ClassName);
        d.Click();
        Assert.Equal("true", e.Find("#yildiz-menu-ac").GetAttribute("aria-expanded"));
        Assert.Contains("acik", e.Find("#yildiz-menu").ClassName);
        e.Find("#yildiz-ust").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("false", e.Find("#yildiz-menu-ac").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Y2_13_hesap_menusu_dil_Kilavuz_Ayarlar_ve_Cikis_yollarini_korur()
    {
        var e = Menu();
        Assert.NotNull(e.Find("#yildiz-menu-dil"));
        Assert.NotNull(e.Find("#yildiz-alt-kilavuz"));
        Assert.NotNull(e.Find("#yildiz-alt-ayarlar"));
        Assert.NotNull(e.Find("#yildiz-cikis"));
        // kapalıyken DOM'da hidden ile durur; düğme açar
        Assert.NotNull(e.Find("#yildiz-kullanici-menu").GetAttribute("hidden"));
        e.Find("#yildiz-kullanici-ac").Click();
        Assert.Null(e.Find("#yildiz-kullanici-menu").GetAttribute("hidden"));
        Assert.Equal("true", e.Find("#yildiz-kullanici-ac").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Y2_14_kur_sagda_okunur_HAS_ve_22_AYAR_sunucudan()
    {
        // Kur yoklamasını UstBar başlatır (kardeş bileşen); burada o adım elle yapılır.
        Services.GetRequiredService<CanliKurServisi>().BaslatAsync().GetAwaiter().GetResult();
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-kur-has")));
        Assert.Contains("6.877", e.Find("#yildiz-kur-has").TextContent);
        Assert.Contains("5.555", e.Find("#yildiz-kur-22").TextContent);   // has×0,916 DEĞİL: sunucu değeri
        Assert.DoesNotContain("style=", e.Markup);
    }

    // ── bUnit: Tüm ekranlar kareleri ───────────────────────────────────────

    private IRenderedComponent<Ekranlar> Karolar(string rol = "SAHIP")
    {
        Oturum(rol);
        moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim","kur"]""";
        var g = Services.GetRequiredService<GorunumServisi>();
        g.YukleAsync().GetAwaiter().GetResult();
        var e = Render<Ekranlar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(sema == "yildiz" ? "a.yz-kare" : "a.ekran-karo")));   // YILDIZ-4B: Yıldız'da tuval Tum4 karesi
        return e;
    }

    [Fact]
    public void Y2_20_Yildizda_39_karenin_hepsi_ikon_ve_aciklama_tasir()
    {
        var e = Karolar();
        // YILDIZ-4B (kurucu 09.10 "tuval birebir"): Yıldız'da sayfa = tuval Tum4 (YildizEkranlar: a.yz-kare · svg ikon · .yz-kac).
        // 39 → 41: PAKET EKSIK-B1-SATINALMA (/satin-alma + /irsaliyeler menüye girdi).
        Assert.Equal(41, e.FindAll("a.yz-kare").Count);
        Assert.Equal(41, e.FindAll("a.yz-kare svg.yz-ik").Count);
        Assert.Equal(41, e.FindAll("a.yz-kare .yz-kac").Count);
        Assert.Equal(YildizSozlugu.Aciklama("/kasalar", "tr"), e.Find("#ekran-aciklama-kasalar").TextContent);
        Assert.Equal("41 ekran · 6 bölüm · hiçbiri gizli değil", e.Find("#ekranlar-yildiz-ozet").TextContent.Trim());
        Assert.Equal(6, e.FindAll(".yz-tum-nokta").Count);
    }

    [Fact]
    public void Y2_21_Yildizda_yetkiye_gore_suzulur_ozet_hiçbiri_gizli_degil_DEMEZ()
    {
        moduller = "[]";
        Oturum("TEZGAHTAR");
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Ekranlar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("a.yz-kare")));   // YILDIZ-4B: tuval Tum4 karesi
        var adet = e.FindAll("a.yz-kare").Count;
        Assert.True(adet < 39);
        Assert.Empty(e.FindAll("#ekran-karo-yedekler"));       // yönetim kareleri yok
        var ozet = e.Find("#ekranlar-yildiz-ozet").TextContent;
        Assert.DoesNotContain("hiçbiri gizli değil", ozet);
        Assert.StartsWith($"{adet} ekran", ozet);
    }

    [Fact]
    public void Y2_22_baska_semada_karo_markup_AYNEN_eski_kalir()
    {
        sema = "platin";
        var e = Karolar();
        Assert.Empty(e.FindAll(".ekran-karo-aciklama"));
        Assert.Empty(e.FindAll("#ekranlar-yildiz-ozet"));
        Assert.Empty(e.FindAll(".ekranlar-grup-sayi"));
        Assert.Equal(5, e.FindAll("a.ekran-karo .ikon").Count);   // yalnız Günlük iş ikonlu
    }

    // ── Kabuk kaynağı: şema koşullu, varsayılan yol AYNEN ──────────────────

    [Fact]
    public void Y2_30_AnaYerlesim_Yildiz_dali_ayrik_varsayilan_KenarCubugu_yolu_durur()
    {
        var s = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Layout", "AnaYerlesim.razor"));
        Assert.Contains("else if (YildizMi)", s);
        Assert.Contains("<YildizUstMenu />", s);
        Assert.Contains("<KenarCubugu BildirimSayisi=\"bildirimSayisi\" />", s);
        Assert.Contains("else if (TamEkranda && !YildizMi)", s);
        // Yıldız dalında kenar çubuğu çizilmez
        var dal = s[s.IndexOf("else if (YildizMi)", StringComparison.Ordinal)..];
        dal = dal[..dal.IndexOf("else\n{", StringComparison.Ordinal)];
        Assert.DoesNotContain("<KenarCubugu", dal);
    }

    // ── CSS sözleşmesi ─────────────────────────────────────────────────────

    [Fact]
    public void Y2_40_gizli_hesap_menusu_hidden_ile_display_none_olur()
    {
        var c = CssOku("tema-yildiz.css");
        Assert.Contains(".yildiz-kullanici-menu[hidden] { display: none; }", c);
    }

    [Fact]
    public void Y2_41_375te_tek_Menu_dugmesi_acilir_liste_ve_iki_sutun_kare()
    {
        var c = CssOku("tema-yildiz.css");
        Assert.Contains("max-width: 1000px", c);
        Assert.Contains(".yildiz-menu-dugme { display: inline-flex; }", c);
        Assert.Contains(".yildiz-menu.acik { display: flex; }", c);
        Assert.Contains("repeat(2, minmax(0, 1fr))", c);
        Assert.Contains("repeat(8, minmax(0, 1fr))", c);
    }

    [Fact]
    public void Y2_42_hover_aciklamasi_visibility_ile_saklanir_secili_oge_1px_cizgili()
    {
        var c = CssOku("tema-yildiz.css");
        Assert.Contains("visibility: hidden;", c);
        Assert.Contains("border-bottom-color: var(--metin);", c);
        Assert.Contains("translateY(-3px)", c);
    }

    [Fact]
    public void Y2_43_yeni_kabuk_kurallarinda_200_agirlik_ve_sabit_renk_yok()
    {
        var c = CssOku("tema-yildiz.css");
        var bas = c.IndexOf("PAKET YILDIZ-2", StringComparison.Ordinal);
        Assert.True(bas > 0);
        // YILDIZ-4A: yalnız YILDIZ-2 bölümü (sonraki paketlerin tuval-birebir bölümleri kendi testinde).
        var son = c.IndexOf("PAKET YILDIZ-3", bas, StringComparison.Ordinal);
        var blok = c[bas..(son > 0 ? son : c.Length)];
        Assert.DoesNotContain("font-weight: 200", blok);
        Assert.DoesNotMatch(@"#[0-9A-Fa-f]{3,8}\b(?!\s*[,;]?\s*/)", System.Text.RegularExpressions.Regex.Replace(blok, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline));
    }

    [Fact]
    public void Y2_44_Yildiz_kabugu_satir_ici_style_icermez_ve_yeni_id_oneklidir()
    {
        var m = Menu().Markup;
        Assert.DoesNotContain("style=", m);
        foreach (var id in System.Text.RegularExpressions.Regex.Matches(m, "id=\"([^\"]+)\"").Select(x => x.Groups[1].Value))
            Assert.StartsWith("yildiz-", id);
    }
}
