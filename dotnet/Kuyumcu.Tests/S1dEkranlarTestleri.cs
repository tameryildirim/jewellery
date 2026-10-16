using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-d — TÜM EKRANLAR (KARO MENÜ) · tuval Ekranlar.dc.html
//
// Sınanan sözleşme:
//   (1) KARO SAYISI ÜRETİLİR — MenuTanimi.Ogeler'den; elle yazılmaz, sırası ve
//       grupları kenar çubuğuyla aynıdır; ipucundaki sayı da aynı kaynaktan.
//   (2) YETKİSİZ KARO ÇİZİLMEZ (M-UX3-06) — mod ise süzgeç DEĞİLDİR.
//   (3) ARAMA Türkçe katlar, boş sonuçta boş durum + gerçek "temizle",
//       Enter ilk karoyu açar.
//   (4) HER DÜĞME İŞ YAPAR (DoD EK md.1) — yıldız PATCH atar; hata görünür ve
//       geri alınır (UX-S28); yetki hatasında "yeniden dene" gerçek GET atar.
//   (5) TUVAL — tek birincil karo (Satış), yalnız "Günlük iş" ikonlu, altta
//       Kılavuz/Ayarlar/Vitrin; CSS ölçüleri belirteçle; sayfa tam ekran.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1dEkranlarTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim","kur"]""";
    private bool yazmaPatlasin;
    private bool yetkiPatlasin;

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> moduller, Func<bool> yazmaPatlat, Func<bool> yetkiPatlat) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yetkiPatlat() && yol.Contains("/yetkiler/benim", StringComparison.Ordinal))
                return Hata("""{"hata":"yetki servisi yanıt vermedi"}""");
            if (yazmaPatlat() && istek.Method != HttpMethod.Get)
                return Hata("""{"hata":"tercih yazılamadı"}""");

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    """{"tercihler":{},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    $$"""{"moduller":{{moduller()}}}""",
                _ => """{"hata":"beklenmeyen uç"}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private static HttpResponseMessage Hata(string govde) =>
            new(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            };
    }

    public S1dEkranlarTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => moduller, () => yazmaPatlasin, () => yetkiPatlasin))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<Ekranlar> Ciz(string rol = "SAHIP", string? mod = null, bool karoBekle = true)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", null,
                                mod ?? ArayuzModuServisi.Profesyonel));
        var e = Render<Ekranlar>();
        if (karoBekle)
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("a.ekran-karo")));
        return e;
    }

    private static List<string> Hrefler(IRenderedComponent<Ekranlar> e) =>
        [.. e.FindAll("a.ekran-karo").Select(a => a.GetAttribute("href")!)];

    // ── (1) KARO SAYISI ÜRETİLİR ────────────────────────────────────────────

    [Fact]
    public void Karolar_MenuTanimi_ndan_URETILIR_sayi_ve_sira_birebir()
    {
        var e = Ciz();

        Assert.Equal(MenuTanimi.Ogeler.Select(o => o.Yol).ToList(), Hrefler(e));
        // Tuval 38 ekran der; bugünkü tanım da 38'dir. Sayı elle yazılmadığı
        // için yeni ekran eklenince bu satır kendiliğinden artar (sayım ÜRETİLİR).
        Assert.Equal(MenuTanimi.Ogeler.Count, e.FindAll("a.ekran-karo").Count);
        // Id sözleşmesi: '<önek>-<alan>' — kenar çubuğuyla aynı slug.
        foreach (var oge in MenuTanimi.Ogeler)
            Assert.Single(e.FindAll($"#ekran-karo-{MenuTanimi.Slug(oge.Yol)}"));
    }

    [Fact]
    public void Ipucundaki_sayi_KARO_SAYISIDIR_ve_tuval_metnini_tasir()
    {
        var e = Ciz();
        var adet = e.FindAll("a.ekran-karo").Count;

        Assert.Equal(EkranlarSozlugu.Ipucu("tr", adet), e.Find("#ekranlar-ipucu").TextContent.Trim());
        Assert.Equal($"{adet} ekranın hepsi burada · yıldızla favorine ekle", EkranlarSozlugu.Ipucu("tr", adet));
    }

    [Fact]
    public void Alti_grup_basligi_TUVAL_SIRASIYLA_ve_sozlukten()
    {
        var e = Ciz();
        var bolumler = e.FindAll("section.ekranlar-grup").Select(s => s.Id ?? "").ToList();

        Assert.Equal(6, bolumler.Count);
        Assert.Equal(MenuTanimi.GrupSirasi.Select(g => $"ekranlar-grup-{MenuTanimi.GrupSlug(g)}").ToList(), bolumler);
        foreach (var g in MenuTanimi.GrupSirasi)
        {
            var slug = MenuTanimi.GrupSlug(g);
            Assert.Equal(MenuTanimi.GrupEtiketi(g)(Sozlukler.Tr),
                e.Find($"#ekranlar-grup-baslik-{slug}").TextContent.Trim());
            // Karo grubunda, kenar çubuğundaki grubuyla AYNI öğeler durur.
            var beklenen = MenuTanimi.Ogeler.Where(o => o.Grup == g).Select(o => o.Yol).ToList();
            var gercek = e.FindAll($"#ekranlar-grup-{slug} a.ekran-karo").Select(a => a.GetAttribute("href") ?? "").ToList();
            Assert.Equal(beklenen, gercek);
        }
    }

    [Fact]
    public void Karo_adlari_KENAR_CUBUGUYLA_AYNI_kaynaktan_gelir()
    {
        var e = Ciz();
        foreach (var oge in MenuTanimi.Ogeler)
            Assert.Equal(oge.Etiket(Sozlukler.Tr),
                e.Find($"#ekran-karo-{MenuTanimi.Slug(oge.Yol)} .ekran-karo-ad").TextContent.Trim());
    }

    // ── (2) YETKİ ≠ MOD ─────────────────────────────────────────────────────

    [Fact]
    public void Yetkisiz_ekranin_karosu_CIZILMEZ_ipucu_da_saymaz()
    {
        moduller = """["cari","islem"]""";
        var e = Ciz(rol: "TEZGAHTAR");
        var hrefler = Hrefler(e);
        var beklenen = MenuTanimi.Yetkililer(m => m is "cari" or "islem", "TEZGAHTAR").Select(o => o.Yol).ToList();

        Assert.Equal(beklenen, hrefler);
        // Modülü olmayan ve rol kapılı ekranlar yok:
        foreach (var yasak in new[] { "/urunler", "/kasalar", "/gider", "/hurda", "/uyum", "/gider-pusulalari", "/isletme", "/yedekler" })
            Assert.DoesNotContain(yasak, hrefler);
        Assert.Contains("/cariler", hrefler);
        Assert.Contains("/islemler", hrefler);
        // Boş grup başlığı çizilmez (Belge ve Uyum: fatura modülü yok, iki rol kapılı).
        Assert.Empty(e.FindAll("#ekranlar-grup-belge-uyum"));
        Assert.Equal(EkranlarSozlugu.Ipucu("tr", beklenen.Count), e.Find("#ekranlar-ipucu").TextContent.Trim());
        // Yıldızı da çizilmez: favoriye eklenemez.
        Assert.Empty(e.FindAll("#ekran-favori-gider"));
    }

    [Fact]
    public void Basit_modda_da_YETKILI_her_ekranin_karosu_cizilir_mod_suzgec_degil()
    {
        var e = Ciz(mod: ArayuzModuServisi.Basit);

        Assert.Equal(MenuTanimi.Ogeler.Count, e.FindAll("a.ekran-karo").Count);
        Assert.True(MenuTanimi.Ogeler.Count > BasitMenu.Yollar.Length);
    }

    // ── (3) ARAMA ───────────────────────────────────────────────────────────

    [Fact]
    public void Arama_TURKCE_KATLAR_sayim_Sayim_i_bulur()
    {
        var e = Ciz();
        e.Find("#ekranlar-ara").Input("SAYIM");

        Assert.Equal(["/sayim"], Hrefler(e));
        Assert.Equal(EkranlarSozlugu.SonucSayisi("tr", 1), e.Find("#ekranlar-sonuc-sayisi").TextContent.Trim());
        // Başka grupların başlığı çizilmez; ipucu TOPLAMI söylemeye devam eder.
        Assert.Single(e.FindAll("section.ekranlar-grup"));
        Assert.Equal(EkranlarSozlugu.Ipucu("tr", MenuTanimi.Ogeler.Count), e.Find("#ekranlar-ipucu").TextContent.Trim());
    }

    [Fact]
    public void Arama_sonucu_yoksa_BOS_DURUM_ve_temizle_dugmesi_listeyi_geri_getirir()
    {
        var e = Ciz();
        e.Find("#ekranlar-ara").Input("zzqq");

        Assert.Empty(e.FindAll("a.ekran-karo"));
        Assert.Contains(EkranlarSozlugu.SonucYok("tr", "zzqq"), e.Find("#ekranlar-bos").TextContent, StringComparison.Ordinal);

        e.Find("#ekranlar-aramayi-temizle").Click();

        Assert.Empty(e.FindAll("#ekranlar-bos"));
        Assert.Equal(MenuTanimi.Ogeler.Count, e.FindAll("a.ekran-karo").Count);
        Assert.Equal("", e.Find("#ekranlar-ara").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Arama_bosken_BOS_DURUM_cizilmez()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#ekranlar-bos"));
    }

    [Fact]
    public void Aramada_Enter_ILK_eslesen_karoyu_acar()
    {
        var e = Ciz();
        var nav = Services.GetRequiredService<NavigationManager>();
        e.Find("#ekranlar-ara").Input("kasa");
        var ilk = Hrefler(e)[0];

        e.Find("#ekranlar-ara").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("/kasalar", ilk);
        Assert.EndsWith("/kasalar", nav.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Aramasiz_Enter_hicbir_yere_GITMEZ()
    {
        var e = Ciz();
        var nav = Services.GetRequiredService<NavigationManager>();
        var once = nav.Uri;

        e.Find("#ekranlar-ara").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(once, nav.Uri);
    }

    // ── (4) HER DÜĞME İŞ YAPAR ──────────────────────────────────────────────

    [Fact]
    public void Yildiz_PATCH_atar_govde_favori_listesini_tasir()
    {
        var e = Ciz();
        var yildiz = e.Find("#ekran-favori-cariler");
        Assert.Equal("false", yildiz.GetAttribute("aria-pressed"));

        yildiz.Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.EndsWith("/tercihler/benim", patch.Yol, StringComparison.Ordinal);
        using var j = JsonDocument.Parse(patch.Govde);
        var deger = j.RootElement.GetProperty("tercihler").GetProperty(MenuTercihServisi.AnahtarFavoriler).GetString()!;
        Assert.Equal(["/cariler"], JsonSerializer.Deserialize<string[]>(deger) ?? []);
        e.WaitForAssertion(() =>
            Assert.Equal("true", e.Find("#ekran-favori-cariler").GetAttribute("aria-pressed")));
        Assert.Contains("dolu", e.Find("#ekran-favori-cariler").ClassList);
    }

    [Fact]
    public void Yildiz_yazilamazsa_BANT_cizilir_ve_GERI_ALINIR()
    {
        var e = Ciz();
        yazmaPatlasin = true;

        e.Find("#ekran-favori-urunler").Click();

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ekranlar-tercih-hatasi")));
        Assert.StartsWith(EkranlarSozlugu.FavoriKaydedilemedi("tr", "").TrimEnd(),
            e.Find("#ekranlar-tercih-hatasi").TextContent.Trim(), StringComparison.Ordinal);
        Assert.Equal("false", e.Find("#ekran-favori-urunler").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Yetki_okunamazsa_BANT_ve_yeniden_dene_GERCEK_istek_atar()
    {
        yetkiPatlasin = true;
        var e = Ciz(karoBekle: false);

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ekranlar-yetki-hatasi")));
        var once = istekler.Count(i => i.Yol.EndsWith("/yetkiler/benim", StringComparison.Ordinal));

        yetkiPatlasin = false;
        e.Find("#ekranlar-yetki-yeniden").Click();

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#ekranlar-yetki-hatasi")));
        Assert.True(istekler.Count(i => i.Yol.EndsWith("/yetkiler/benim", StringComparison.Ordinal)) > once);
        e.WaitForAssertion(() => Assert.Equal(MenuTanimi.Ogeler.Count, e.FindAll("a.ekran-karo").Count));
    }

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar()
    {
        // DoD EK md.1: sayfadaki düğme türleri — yıldız (PATCH), temizle (liste
        // geri gelir, ayrı testte), yeniden dene (GET, ayrı testte). Başka düğme YOK.
        var e = Ciz();
        var dugmeler = e.FindAll("button").Select(b => b.Id).ToList();

        Assert.All(dugmeler, id => Assert.StartsWith("ekran-favori-", id, StringComparison.Ordinal));
        Assert.Equal(MenuTanimi.Ogeler.Count, dugmeler.Count);
        Assert.All(e.FindAll("button"), b => Assert.False(b.HasAttribute("disabled")));
    }

    // ── (5) TUVAL ───────────────────────────────────────────────────────────

    [Fact]
    public void TEK_birincil_karo_Satis_tir()
    {
        var e = Ciz();
        var birincil = e.FindAll(".ekran-karo-birincil");

        Assert.Single(birincil);
        Assert.Equal(Ekranlar.BirincilKaroYolu, birincil[0].GetAttribute("href"));
        Assert.Equal("/islemler", Ekranlar.BirincilKaroYolu);
    }

    [Fact]
    public void Yalniz_Gunluk_is_karolari_IKON_tasir()
    {
        var e = Ciz();
        foreach (var oge in MenuTanimi.Ogeler)
        {
            var ikon = e.FindAll($"#ekran-karo-{MenuTanimi.Slug(oge.Yol)} svg.ikon").Count;
            Assert.Equal(oge.Grup == MenuGrubu.Gunluk ? 1 : 0, ikon);
        }
    }

    [Fact]
    public void Ust_serit_marka_PANELE_doner_baslik_h1_arama_yer_tutucusu_tuvalden()
    {
        var e = Ciz();

        Assert.Equal("/", e.Find("#ekranlar-panele-don").GetAttribute("href"));
        Assert.Equal(EkranlarSozlugu.PaneleDon("tr"), e.Find("#ekranlar-panele-don").GetAttribute("aria-label"));
        Assert.Equal(KabukTuvalSozlugu.MarkaYazimi, e.Find("#ekranlar-panele-don .ekranlar-marka-ad").TextContent.Trim());
        Assert.Equal("Tüm ekranlar", e.Find("h1#ekranlar-baslik").TextContent.Trim());
        Assert.Equal("Ekran adı yaz: fatura, kasa, sayım…", e.Find("#ekranlar-ara").GetAttribute("placeholder"));
        Assert.Equal("search", e.Find("#ekranlar-ara").GetAttribute("type"));
    }

    [Fact]
    public void Alt_baglantilar_Kilavuz_Ayarlar_Vitrin_panosu()
    {
        var e = Ciz();
        var alt = e.FindAll("#ekranlar-alt a").Select(a => a.GetAttribute("href")).ToList();

        Assert.Equal(["/kilavuz", "/ayarlar", "/vitrin"], alt);
        Assert.Equal(EkranlarGirisTuvalSozlugu.VitrinPanosu("tr"), e.Find("#ekranlar-alt-vitrin").TextContent.Trim());
    }

    [Fact]
    public void Vitrin_panosu_KUR_yetkisi_yoksa_CIZILMEZ()
    {
        moduller = """["cari","islem"]""";
        var e = Ciz(rol: "TEZGAHTAR");

        Assert.Empty(e.FindAll("#ekranlar-alt-vitrin"));
        Assert.NotEmpty(e.FindAll("#ekranlar-alt-kilavuz"));
        Assert.NotEmpty(e.FindAll("#ekranlar-alt-ayarlar"));
    }

    [Fact]
    public async Task Ingilizce_dilde_metinler_EN_sozlukten()
    {
        var e = Ciz();
        await e.InvokeAsync(() => Services.GetRequiredService<DilServisi>().DegistirAsync("en"));

        e.WaitForAssertion(() => Assert.Equal("All screens", e.Find("#ekranlar-baslik").TextContent.Trim()));
        // S1-g: karo adı MenuAdlari'ndan (tuval) — ESKİ beklenen Nav.Cariler ("Accounts").
        Assert.Equal(MenuAdlari.En.Musteriler.Ad, e.Find("#ekran-karo-cariler .ekran-karo-ad").TextContent.Trim());
        Assert.Equal(EkranlarSozlugu.AramaYerTutucu("en"), e.Find("#ekranlar-ara").GetAttribute("placeholder"));
    }

    [Fact]
    public void Sozluk_IKI_DILDE_dolu()
    {
        foreach (var dil in new[] { "tr", "en" })
        {
            Assert.False(string.IsNullOrWhiteSpace(EkranlarSozlugu.Baslik(dil)));
            Assert.False(string.IsNullOrWhiteSpace(EkranlarSozlugu.AramaYerTutucu(dil)));
            Assert.False(string.IsNullOrWhiteSpace(EkranlarSozlugu.AramaEtiketi(dil)));
            Assert.Contains("38", EkranlarSozlugu.Ipucu(dil, 38), StringComparison.Ordinal);
            Assert.Contains("xyz", EkranlarSozlugu.SonucYok(dil, "xyz"), StringComparison.Ordinal);
            Assert.Contains("E", EkranlarSozlugu.FavoriyeEkle(dil, "E"), StringComparison.Ordinal);
            Assert.Contains("E", EkranlarSozlugu.FavoridenCikar(dil, "E"), StringComparison.Ordinal);
            Assert.Contains("s", EkranlarSozlugu.YetkiYuklenemedi(dil, "s"), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(EkranlarSozlugu.AltBaglantilar(dil)));
        }
        Assert.NotEqual(EkranlarSozlugu.Baslik("tr"), EkranlarSozlugu.Baslik("en"));
    }

    [Fact]
    public void Rota_basligi_ve_yetki_haritasi_ekranlar_icin_TANIMLI()
    {
        Assert.Equal("Tüm ekranlar", RotaBasliklari.Baslik("/ekranlar", Sozlukler.Tr));
        Assert.Equal("All screens", RotaBasliklari.Baslik("/ekranlar", Sozlukler.En));
        Assert.True(Kuyumcu.Core.Erisim.RotaModulleri.Harita.ContainsKey("/ekranlar"));
        Assert.Null(Kuyumcu.Core.Erisim.RotaModulleri.Harita["/ekranlar"]);
        // Kendini listelemez: karo menü bir menü ÖĞESİ değildir.
        Assert.DoesNotContain(MenuTanimi.Ogeler, o => o.Yol == "/ekranlar");
        Assert.Equal("Layout/KenarCubugu.razor", MenuTanimi.MenusuzRotalar["/ekranlar"]);
    }

    [Theory]
    [InlineData("/ekranlar", true)]
    [InlineData("/ekranlar/", true)]
    [InlineData("/", false)]
    [InlineData("/islemler", false)]
    [InlineData("/ekranlarx", false)]
    public void Tam_ekran_YALNIZ_karo_menude(string yol, bool beklenen)
        => Assert.Equal(beklenen, AnaYerlesim.TamEkranRotasi(yol));

    [Fact]
    public void Sayfada_CIPLAK_metin_ve_satir_ici_style_YOK()
    {
        var razor = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "Pages", "Ekranlar.razor"));
        Assert.DoesNotContain("style=\"", razor);
        // Yorumlar atıldıktan sonra Türkçe harf kalmamalı: her metin sözlükten.
        var kod = Regex.Replace(razor, @"@\*.*?\*@", "", RegexOptions.Singleline);
        kod = Regex.Replace(kod, @"//[^\n]*", "");
        Assert.DoesNotMatch("[çğıöşüÇĞİÖŞÜ]", kod);
        // Karo sayısı elle yazılmaz.
        Assert.DoesNotContain("38", kod, StringComparison.Ordinal);
    }

    [Fact]
    public void S1d_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir()
    {
        var css = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "wwwroot", "css", "app.css"));
        var i = css.IndexOf("S1-d — TÜM EKRANLAR (KARO MENÜ)", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-d bloğu yok.");
        var kod = Regex.Replace(css[i..], @"/\*.*?\*/", "", RegexOptions.Singleline);

        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", kod);
        Assert.Contains(".ekranlar-izgara { display: grid; grid-template-columns: repeat(8, minmax(0, 1fr)); gap: 10px; }", kod);
        Assert.Contains("height: 68px; box-sizing: border-box; padding: 0 14px;", kod);
        Assert.Contains("font-size: 17px; font-weight: 500;", kod);
        Assert.Contains("border: 1px solid var(--ayrac); border-radius: 10px;", kod);
        Assert.Contains("background: var(--kenar-yuzey); color: var(--kenar-metin-guclu);", kod);
        Assert.Contains("margin: 0; font-size: 15px; font-weight: 600; color: var(--metin-soluk);", kod);
        Assert.Contains("background: var(--vurgu); color: var(--vurgu-metin); font-weight: 600;", kod);
        Assert.Contains("width: 360px;", kod);
        Assert.Contains("font-family: var(--yazi-marka); font-size: 22px; font-weight: 600;", kod);
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }
}
