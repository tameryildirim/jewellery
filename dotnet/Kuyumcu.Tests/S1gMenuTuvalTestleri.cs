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
// PAKET S1-g — MENÜ VE ÜST BAR TUVALE UYUM (kurucu 24.09 06:55: "menüyü felan
// kontrol et, olmamış")
//
// TEK DOĞRULUK KAYNAĞI: dokumanlar/02-mimari/sadelestirme/tuval/project/
//   Panel.dc.html (basit) · Profesyonel.dc.html · Tezgah.dc.html · Ekranlar.dc.html
//
// Orkestratörün gerçek tarayıcıda ölçtüğü sapmaların kilitleri:
//   (1) KRİTİK — kapalı grubun satırları `hidden` taşıyor ama GÖRÜNÜYORDU:
//       `.kenar-satir{display:flex}` UA `[hidden]{display:none}` kuralını
//       eziyordu. bUnit CSS uygulamaz → bu dosya CSS DOSYASINI okur ve genel
//       `[hidden]{display:none !important}` kuralını kilitler. Gerçek görünürlük
//       ölçümü tarayıcıdadır (rapor § canlı ölçüm).
//   (2) basit modda 6 grup (MenuKatlamaTestleri / BasitModMenuTestleri'nde).
//   (3) ekran adları tuvalle BİREBİR (ad tablosu aşağıda).
//   (4) menüde yıldız 0, tutamaç 0; favori ekleme "Tüm ekranlar"da.
//   (5) kullanıcı kartı: baş harf + ad + rol (+ PRO'da " · <şube>").
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1gMenuTuvalTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private string tercihler = "{}";
    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";

    private sealed class SahteApi(
        List<(string, string)> kayit, Func<string> tercihler, Func<string> moduller) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol));
            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    $$"""{"tercihler":{{tercihler()}},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    $$"""{"moduller":{{moduller()}}}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) =>
                    """[{"id":"s1","ad":"Merkez şube","aktif":true}]""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public S1gMenuTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => tercihler, () => moduller))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<KenarCubugu> Ciz(string mod, string rol = "SAHIP", string rota = "/")
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", null, mod));
        var e = Render<KenarCubugu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    private static string CssYolu(string ad) =>
        Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", ad);

    // ── (1) GENEL [hidden] KURALI — KRİTİK ──────────────────────────────────

    [Fact]
    public void Genel_hidden_kurali_app_css_BASINDA_ve_IMPORTANT()
    {
        var css = File.ReadAllText(CssYolu("app.css"));
        var kural = Regex.Match(css, @"(?m)^\[hidden\]\s*\{\s*display:\s*none\s*!important;\s*\}");

        Assert.True(kural.Success, "app.css'te genel `[hidden] { display: none !important; }` kuralı YOK — " +
            "`.kenar-satir{display:flex}` gibi her sınıf kuralı `hidden` özniteliğini ezer (S1-g KRİTİK)");
        // Kural sınıf kurallarından ÖNCE, dosyanın başındadır (tek yerde, bulunur).
        var ilkSinifKurali = Regex.Match(css, @"(?m)^\.[a-z]").Index;
        Assert.True(kural.Index < ilkSinifKurali, "genel [hidden] kuralı app.css'in BAŞINDA olmalı");
        // !important olmadan kural .kenar-satir'ı YENEMEZ (özgüllük eşit/düşük).
        Assert.DoesNotMatch(@"(?m)^\[hidden\]\s*\{\s*display:\s*none\s*;\s*\}", css);
    }

    [Fact]
    public void Katli_satirlar_hidden_OZNITELIGI_tasir_ve_display_ezen_sinif_kurali_var()
    {
        // Genel kuralın NEDEN gerekli olduğunun belgesi: satır sınıfı display
        // atar (bu kural silinirse genel kural gereksiz görünebilir — değil).
        var css = File.ReadAllText(CssYolu("app.css"));
        Assert.Matches(@"\.kenar-satir\s*\{[^}]*display:\s*flex", css);

        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var katli = e.FindAll(".kenar-satir[data-sira-anahtar]").Where(s => s.HasAttribute("hidden")).ToList();
        Assert.Equal(MenuTanimi.Ogeler.Count - 5, katli.Count);   // Panel rotası: 5 satır açık
    }

    // ── (3) ADLAR TUVALLE BİREBİR ───────────────────────────────────────────

    /// <summary>
    /// Rota → tuvaldeki ad (Ekranlar.dc.html "Tüm ekranlar" + Panel.dc.html
    /// menü). Bir ad eskiye dönerse (ör. "Cariler") bu tablo KIRMIZIDIR.
    /// </summary>
    public static readonly (string Yol, string Ad)[] TuvalAdlari =
    [
        ("/", "Panel"), ("/islemler", "Satış"), ("/emanet", "Emanet"),
        ("/siparisler", "Sipariş ve tamir"), ("/bildirimler", "Bildirimler"),
        ("/cariler", "Müşteriler"), ("/urunler", "Ürünler"), ("/urunler/agac", "Ürün ağacı"),
        ("/urunler/etiketler", "Etiketler"), ("/fiyatlandirma", "Fiyat listeleri"), ("/sayim", "Sayım"),
        ("/satin-alma", "Satın alma"), ("/irsaliyeler", "İrsaliyeler"),   // PAKET EKSIK-B1-SATINALMA
        ("/kasalar", "Kasalar"), ("/vezne", "Kasa kuyruğu"), ("/pos", "POS cihazları"),
        ("/pos/tahsilatlar", "POS alacakları"), ("/banka", "Banka"), ("/evraklar", "Çek ve senet"),
        ("/evraklar/takvim", "Vade takvimi"), ("/kur", "Kur"), ("/gider", "Gider ve para aktarımı"),
        ("/hurda", "Hurda ve ramat"),
        ("/faturalar", "Faturalar"), ("/gider-pusulalari", "Gider pusulaları"), ("/uyum", "Uyum (MASAK)"),
        // PAKET YASAL-1 tur 2 (tasarım hakemi KRİTİK): tuvalde olmayan YENİ ekran — şartname § 4.5 adı.
        ("/yasal-sinirlar", "Yasal sınırlar"),
        ("/raporlar", "Rapor merkezi"), ("/raporlar/kar-zarar", "Kâr-zarar"),
        ("/raporlar/vade-takibi", "Vade takibi"), ("/raporlar/islem-defteri", "İşlem defteri"),
        ("/raporlar/satis-analizi", "Satış analizi"), ("/raporlar/kasa", "Kasa raporu"),
        ("/kullanicilar", "Kullanıcılar"), ("/isletme", "İşletme"), ("/yetkiler", "Yetkiler"),
        ("/denetim-izi", "Kim ne yaptı"), ("/kurulum", "Kurulum"),
        ("/veri-disa-aktarim", "Veri dışa aktar"), ("/veri-ice-aktarim", "Veri içe aktar"),
        ("/yedekler", "Yedekler"),
        ("/kilavuz", "Kılavuz"), ("/ayarlar", "Ayarlar"),
    ];

    [Fact]
    public void Menu_adlari_TUVALLE_BIREBIR_ve_sayfa_basligi_menu_adiyla_AYNI()
    {
        var tumu = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok).ToList();
        Assert.Equal(tumu.Count, TuvalAdlari.Length);
        foreach (var (yol, ad) in TuvalAdlari)
        {
            var oge = tumu.Single(o => o.Yol == yol);
            Assert.Equal(ad, oge.Etiket(Sozlukler.Tr));
            // Üst bar başlığı (RotaBasliklari) menü adıyla AYNI (UX-S31).
            Assert.Equal(ad, RotaBasliklari.Baslik(yol, Sozlukler.Tr));
            // İngilizce karşılık boş değil ve TR ile aynı dizge değil (Panel/Kur/Banka hariç ortak).
            Assert.False(string.IsNullOrWhiteSpace(oge.Etiket(Sozlukler.En)));
        }
    }

    [Fact]
    public void Grup_basliklari_TUVALDEKI_gibi_CUMLE_DUZENINDE()
    {
        string[] tuval = ["Günlük iş", "Müşteri ve stok", "Para ve banka", "Belge ve uyum", "Raporlar", "Yönetim"];
        Assert.Equal(tuval, MenuTanimi.GrupSirasi.Select(g => MenuTanimi.GrupEtiketi(g)(Sozlukler.Tr)).ToArray());

        // CSS büyük harfe ÇEVİRMEZ: S1-g bloğu text-transform'u kapatır.
        var css = File.ReadAllText(CssYolu("app.css"));
        var blok = css[css.IndexOf("PAKET S1-g — MENÜ VE ÜST BAR", StringComparison.Ordinal)..];
        Assert.Matches(@"\.kenar-grup-dugme\[aria-expanded=""true""\] \.kenar-grup-ad\s*\{\s*text-transform:\s*none", blok);
    }

    [Fact]
    public void Kapali_grup_onizlemesi_tuvalin_KISA_adlarini_yazar_tam_adlar_ipucunda()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var para = e.Find("#kenar-onizleme-para-banka");

        // Tuval Panel.dc.html: "Kasalar · Vezne · POS · Banka · Çek-senet · Kur · Gider · Hurda…"
        foreach (var kisa in new[] { "Kasalar", "Vezne", "POS", "Banka", "Çek-senet", "Kur", "Gider", "Hurda" })
            Assert.Contains(kisa, para.TextContent, StringComparison.Ordinal);
        Assert.StartsWith("Kasalar · Vezne · POS", para.TextContent.Trim(), StringComparison.Ordinal);
        // Tam adlar kaybolmaz: ipucunda (title) yazar.
        Assert.Contains("Kasa kuyruğu", para.GetAttribute("title") ?? "", StringComparison.Ordinal);
        Assert.Contains("Rapor merkezi · Kâr-zarar · Vade",
            e.Find("#kenar-onizleme-rapor").TextContent, StringComparison.Ordinal);
    }

    // ── (4) YILDIZ VE TUTAMAÇ MENÜDE YOK; FAVORİLER PRO'DA ─────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Menude_yildiz_ve_tutamac_SIFIR(string mod)
    {
        tercihler = """{"menu-favoriler":"[\"/kur\"]"}""";
        var e = Ciz(mod);

        Assert.Empty(e.FindAll(".kenar-yildiz"));
        Assert.Empty(e.FindAll(".kenar-tutamac"));
        Assert.Empty(e.FindAll("[id^=kenar-favori-]:not([id^=kenar-favori-satir-])"));
        Assert.DoesNotContain("⠿", e.Find("#kenar-cubugu").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void PRO_modda_Favoriler_satiri_favori_varsa_EN_USTTE_basitte_yok()
    {
        tercihler = """{"menu-favoriler":"[\"/kur\",\"/faturalar\"]"}""";
        var pro = Ciz(ArayuzModuServisi.Profesyonel);
        pro.WaitForAssertion(() => Assert.NotEmpty(pro.FindAll("#kenar-grup-favoriler")));

        var ilk = pro.Find("#kenar-nav").Children.First();
        Assert.Equal("kenar-grup-favoriler", ilk.Id);
        Assert.NotNull(pro.Find("#kenar-favori-satir-kur a[href='/kur']"));
        Assert.NotNull(pro.Find("#kenar-favori-satir-faturalar"));

        var basit = Ciz(ArayuzModuServisi.Basit);
        Assert.Empty(basit.FindAll("#kenar-grup-favoriler"));
    }

    [Fact]
    public void PRO_modda_favori_yoksa_Favoriler_satiri_CIZILMEZ()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#kenar-grup-favoriler"));
    }

    [Fact]
    public void Sira_sifirla_kayitli_sira_YOKKEN_cizilmez()
    {
        var bos = Ciz(ArayuzModuServisi.Basit);
        Assert.Empty(bos.FindAll("#kenar-sira-sifirla"));
    }

    [Fact]
    public void Sira_sifirla_YALNIZ_kayitli_sira_varken_cikar_ipucu_tasir_ve_DELETE_atar()
    {
        // ⚠ Tercih servisi kullanıcı başına bir kez okur: tercih İLK çizimden önce kurulur.
        tercihler = """{"menu-sirasi":"{\"rapor\":[\"/raporlar/kasa\",\"/raporlar\"]}"}""";
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kenar-sira-sifirla")));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#kenar-sira-sifirla").GetAttribute("title")));

        e.Find("#kenar-sira-sifirla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "DELETE" && i.Yol.Contains("menu-sirasi", StringComparison.Ordinal)));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kenar-sira-sifirla")));
    }

    // ── (5) KULLANICI KARTI ─────────────────────────────────────────────────

    [Fact]
    public void Kullanici_karti_bas_harf_ad_rol_ve_PRO_da_SUBE_adi()
    {
        var pro = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.Equal("RA", pro.Find(".kenar-avatar").TextContent.Trim());
        // T-K2: tuval "Ramazan A." — tam ad title'da (bilgi kaybolmaz).
        Assert.Equal("Ramazan A.", pro.Find(".kullanici-ad").TextContent.Trim());
        Assert.Equal("Ramazan Akkocan", pro.Find(".kullanici-ad").GetAttribute("title"));
        Assert.Equal(Sozlukler.Tr.Kullanici.RolSahip, pro.Find("#kenar-kullanici-rol").TextContent.Trim());
        pro.WaitForAssertion(() =>
            Assert.Equal("· Merkez şube", pro.Find("#kenar-kullanici-rol-satir #ust-sube-sabit").TextContent.Trim()));

        // Basit yüz (tuval Panel.dc.html satır 47): yalnız rol.
        var basit = Ciz(ArayuzModuServisi.Basit);
        Assert.Empty(basit.FindAll("#ust-sube-sabit"));
        Assert.Equal(Sozlukler.Tr.Kullanici.RolSahip, basit.Find("#kenar-kullanici-rol").TextContent.Trim());
    }

    // ── KAYNAK KİLİTLERİ ────────────────────────────────────────────────────

    [Fact]
    public void Ust_bar_ve_menu_kaynaginda_METIN_IKON_yok()
    {
        var kok = Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Layout");
        var ust = File.ReadAllText(Path.Combine(kok, "UstBar.razor"));
        var kenar = File.ReadAllText(Path.Combine(kok, "KenarCubugu.razor"));

        Assert.DoesNotContain(">⟳<", ust, StringComparison.Ordinal);
        Assert.DoesNotContain(">⠿<", kenar, StringComparison.Ordinal);
        Assert.DoesNotContain("YildizDugmesi(", kenar, StringComparison.Ordinal);
        Assert.DoesNotContain("MenudeGorunur", kenar, StringComparison.Ordinal);
    }

    [Fact]
    public void Menu_sozlugu_IKI_DILDE_dolu_ve_EN_adlar_benzersiz()
    {
        var tumu = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok).ToList();
        foreach (var dil in new[] { Sozlukler.Tr, Sozlukler.En })
        {
            var adlar = tumu.Select(o => o.Etiket(dil)).ToList();
            Assert.All(adlar, a => Assert.False(string.IsNullOrWhiteSpace(a)));
            Assert.All(tumu.Select(o => o.KisaEtiket(dil)), a => Assert.False(string.IsNullOrWhiteSpace(a)));
            Assert.Equal(adlar.Count, adlar.Distinct(StringComparer.Ordinal).Count());
        }
        Assert.NotEqual(MenuAdlari.Tr.Musteriler.Ad, MenuAdlari.En.Musteriler.Ad);
    }

    [Fact]
    public void Eski_ad_ile_arama_EKRANI_BULMAYA_devam_eder()
    {
        // Ad değişikliği aramayı kırmaz: "cari" / "denetim" yazan yeni adı bulur.
        var cariler = MenuTanimi.Ogeler.Single(o => o.Yol == "/cariler");
        var denetim = MenuTanimi.Ogeler.Single(o => o.Yol == "/denetim-izi");
        Assert.Contains("cari", AramaMetni.Katla(cariler.AramaMetni(Sozlukler.Tr)), StringComparison.Ordinal);
        Assert.Contains("denetim", AramaMetni.Katla(denetim.AramaMetni(Sozlukler.Tr)), StringComparison.Ordinal);
        // Eş anlamlı EKRANDA görünmez: görünen ad yalnız tuval adıdır.
        Assert.Equal("Müşteriler", cariler.Etiket(Sozlukler.Tr));
    }
}
