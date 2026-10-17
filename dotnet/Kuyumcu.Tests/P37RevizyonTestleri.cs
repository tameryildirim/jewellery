using System.Net;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37 (REVİZYON) — KURUCU REDDİ ÜZERİNE BEŞ DÜZELTME + DÜZEN DİSİPLİNİ
//
// Kurucu, 21 Eylül 16:30 ve 16:35'te yayındaki ekranı gördü ve reddetti:
//   (1) "dropdownun bazılarında var bazılarında yok arama"
//   (2) "menüde ok işaretleri nedir saçma sapan, sürükle bırakla olacak dedim"
//   (3) "arama kısmı çok eski ve basit"
//   (4) menünün tepesindeki 3 satırlık favori paragrafı
//   (5) "inputlar seçme yerleri dengesiz" · "sağda maden yazan yer ile yanında
//       olması gereken şey aynı hizada mı? Tüm projeyi adam gibi elden geçirsin"
//
// ⚠ Bu dosya GÖRSEL BEĞENİ ölçmez; ölçülebilir sözleşmeleri ölçer. Piksel
//   ölçümleri tarayıcıda koşan ayrı bir betiktedir (rapordaki ölçüm tablosu):
//   dokumanlar/09-uygulama-kayitlari/betikler/p37-hiza-olcum.js
//
// ⚠ Sayaç karşılıkları (dotnet/denetim.sh `# >>> P-37 REVİZYON` bloğu):
//   SECICI_SARTLI_ARAMA = 0 · MENU_OK_DUGMESI = 0 · MENU_KALICI_PARAGRAF = 0
//   MUKERRER_ARAMA_YUZEYI = 0 · SAYFA_BASI_DISI_DUZEN = 0 · MENU_SESSIZ_YUTMA = 0
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P37RevizyonTestleri : BunitContext
{
    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici"]""";
    private HttpStatusCode yetkiKodu = HttpStatusCode.OK;

    private sealed class SahteApi(Func<string> modulJson, Func<HttpStatusCode> kod) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (yol.Contains("/yetkiler/benim", StringComparison.Ordinal) && kod() != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(kod())
                {
                    Content = new StringContent("""{"hata":"yetki servisi yanit vermedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            var govde = yol.Contains("/yetkiler/benim", StringComparison.Ordinal)
                ? $$"""{"moduller":{{modulJson()}}}"""
                // ⚠ bUnit sözleşmesi: "{}" varsayılanı JsonException doğurur;
                //   bilinmeyen uca GERÇEKÇİ boş liste dönülür.
                : """{"sonuclar":[],"limit":10,"dahaVar":false}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public P37RevizyonTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => moduller, () => yetkiKodu))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<CanliKurServisi>();   // YildizUstMenu (kur şeridi yalnız okunur)
        KabukServisi.Ortak.GorunumSubesiYerelAta(null);
    }

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu kaldırıldı. Menü artık üst menüdür (YildizUstMenu);
    // menüdeki ekran araması üst barın Ctrl/Cmd+K paletine (GlobalArama) ve "Tüm ekranlar"
    // sayfasına (Ekranlar) taşındı. Aşağıdaki menü testleri bu üç yüzeyde sınanır.
    private void OturumAc(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null,
                                ArayuzModuServisi.Profesyonel));

    private IRenderedComponent<YildizUstMenu> Menu(string rol = "SAHIP")
    {
        OturumAc(rol);
        var e = Render<YildizUstMenu>();
        e.WaitForAssertion(() => Assert.True(e.FindAll("#yildiz-menu a.yildiz-menu-oge").Count > 2));
        return e;
    }

    /// <summary>Paleti üst bar düğmesiyle açar (gerçek kullanıcı yolu).</summary>
    private IRenderedComponent<GlobalArama> Palet()
    {
        OturumAc();
        var e = Render<GlobalArama>();
        e.Find("#ust-kayit-ara").Click();
        return e;
    }

    // ── (1) DROPDOWN'DA ARAMA HER ZAMAN VAR ─────────────────────────────────

    [Fact]
    public void Secici_arama_kutusu_TEK_secenekte_bile_gorunur()
    {
        // ÖNCE: `AramaGorunur => Tum.Count > AramaEsigi` (8) — bir seçicide kutu
        // vardı, ötekinde yoktu. Kurucu haklı olarak "bazısında var bazısında
        // yok" dedi. ŞİMDİ: eşik YOK.
        var e = Render<Secici>(p => p
            .Add(s => s.ElemanId, "t-secici")
            .Add(s => s.Etiket, "Maden")
            .Add(s => s.Secenekler, new List<SecenekOgesi> { new("ALTIN", "Altın") }));

        e.Find("#t-secici").Click();

        Assert.NotNull(e.Find("#t-secici-ara-girdi"));
    }

    [Fact]
    public void Secici_arama_kutusu_kaynakta_KOSULSUZDUR()
    {
        // Mutasyon kapısı: davranış testi tek seçenekle geçer, ama biri eşiği
        // geri koyarsa ("Tum.Count > 8") çok seçenekli hâlde yine yeşil olurdu.
        // Bu yüzden KAYNAK da denetlenir: koşullu dal KODDA olamaz.
        var kaynak = CssKaynak.YorumsuzKod(
            CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "Secici.razor"));

        Assert.DoesNotContain("AramaEsigi", kaynak, StringComparison.Ordinal);
        Assert.Contains("private const bool AramaGorunur = true;", kaynak, StringComparison.Ordinal);
    }

    [Fact]
    public void Secici_cok_secenekte_de_arama_kutusu_gorunur_ve_suzer()
    {
        var e = Render<Secici>(p => p
            .Add(s => s.ElemanId, "t-cok")
            .Add(s => s.Etiket, "Ayar")
            .Add(s => s.Secenekler, new List<SecenekOgesi>
            {
                new("K8", "8 Ayar"), new("K14", "14 Ayar"), new("K18", "18 Ayar"),
                new("K21", "21 Ayar"), new("K22", "22 Ayar"), new("K24", "24 Ayar"),
            }));

        e.Find("#t-cok").Click();
        e.Find("#t-cok-ara-girdi").Input("22");

        var secenekler = e.FindAll("#t-cok-liste li[role=option]");
        Assert.Single(secenekler);
        Assert.Contains("22", secenekler[0].TextContent, StringComparison.Ordinal);
    }

    // ── (2) MENÜDE GÖRÜNÜR OK DÜĞMESİ YOK ───────────────────────────────────

    [Fact]
    public void Menude_gorunur_ok_dugmesi_SIFIRDIR()
    {
        // ÖNCE: 40 satır × 2 = 80 görünür ↑/↓ düğmesi. TEMA TEMİZLİĞİ 17.10: menü = üst menü
        // (YildizUstMenu #yildiz-menu); içinde yalnız ekran bağlantıları durur, ok düğmesi yok.
        var e = Menu();
        var menu = e.Find("#yildiz-menu");

        Assert.Empty(menu.QuerySelectorAll("button"));
        Assert.Equal(menu.QuerySelectorAll("a").Length, menu.QuerySelectorAll("a.yildiz-menu-oge").Length);
        Assert.Empty(e.FindAll("[id^=kenar-yukari-]"));
        Assert.Empty(e.FindAll("[id^=kenar-asagi-]"));
        Assert.DoesNotContain("↑", menu.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("↓", menu.TextContent, StringComparison.Ordinal);
    }

    // TEMA TEMİZLİĞİ 17.10: Suruklenebilir_her_satir_KLAVYE_yolu_tasir_tutamac_ve_yildiz_GORUNMEZ kaldırıldı — menü sıralama (sürükle-bırak + Alt+↑/↓, data-sira-anahtar) kenar çubuğuyla birlikte kaldırıldı; üst menü tuvalin sabit beş sözcüğüdür (Yildiz2Testleri.Y2_01/Y2_10). Tutamaç ve yıldız da yok (yukarıdaki test: menüde düğme 0).

    // ── (4) MENÜDE KALICI PARAGRAF YOK ──────────────────────────────────────

    [Fact]
    public void Menude_kalici_paragraf_SIFIRDIR()
    {
        // ÖNCE: menünün tepesinde "Yıldıza bastığın ekranlar…" 3 satırlık paragraf + altta kapsam
        // kilidi paragrafı duruyordu. TEMA TEMİZLİĞİ 17.10: Yıldız üst menüsünde (hesap menüsü dahil)
        // kalıcı paragraf yoktur; "Sırayı sıfırla" düğmesi de yoktur (menü sırası kaldırıldı).
        var e = Menu();

        Assert.NotEmpty(e.FindAll("#yildiz-ust a.yildiz-menu-oge"));   // menü çizildi (boş DOM'da yanlış yeşil yok)
        Assert.Empty(e.FindAll("#yildiz-ust p"));
        Assert.Empty(e.FindAll(".kenar-grup-ipucu"));
        Assert.Empty(e.FindAll("#kenar-sira-sifirla"));
    }

    // ── (3) EKRAN ARAMASI ───────────────────────────────────────────────────

    // ⚠ S1-j: kayıt araması (#kabuk-arama-girdi) üst bar paletindedir (KabukAramaEkranTestleri).
    // TEMA TEMİZLİĞİ 17.10: menü kutusu (#menu-arama-girdi) kenar çubuğuyla kaldırıldı; paletin
    // EKRANLAR bölümü aynı eşleşme kuralıyla ekran arar (S1jMenuAramaTestleri).

    [Fact]
    public void Palet_aramasi_TEK_PARCA_alandir_ve_ikon_tasir()
    {
        // ESKİ: Menu_aramasi_TEK_PARCA_alandir_ve_ikon_tasir (menü kutusu). Kutu tek satırdır:
        // ikon + girdi aynı kapta, ayrı bir "Ara" düğmesi yok.
        var e = Palet();
        var satir = e.Find("#kabuk-arama-palet .menu-arama-satir");

        Assert.NotNull(satir.QuerySelector(".menu-arama-ikon"));
        Assert.NotNull(satir.QuerySelector("#kabuk-arama-girdi"));
        Assert.Equal("search", e.Find("#kabuk-arama-girdi").GetAttribute("type"));
        Assert.Empty(satir.QuerySelectorAll("button"));   // yazılmadan temizle düğmesi de yok
    }

    [Fact]
    public void Palet_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir()
    {
        // ESKİ: Menu_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir (menü kutusu).
        var e = Palet();
        Assert.Empty(e.FindAll("#kabuk-arama-temizle"));

        e.Find("#kabuk-arama-girdi").Input("kasa");
        var temizle = e.WaitForElement("#kabuk-arama-temizle");   // DoD EK md.1: gerçek iş yapar
        Assert.NotEmpty(e.FindAll("#kabuk-arama-paneli"));

        temizle.Click();

        Assert.Empty(e.FindAll("#kabuk-arama-temizle"));
        Assert.Empty(e.FindAll("#kabuk-arama-paneli"));
        Assert.Equal("", e.Find("#kabuk-arama-girdi").GetAttribute("value") ?? "");
        Assert.NotNull(e.Find("#kabuk-arama-palet"));   // × yalnız metni siler, palet açık kalır
    }

    // TEMA TEMİZLİĞİ 17.10: Menu_aramasi_menunun_DISINDA_pencere_ACMAZ kaldırıldı — sınadığı menü içi kutu kenar çubuğuyla kaldırıldı; ekran araması artık bilerek perdeli komut paletinde (KabukAramaEkranTestleri.K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal) ve "Tüm ekranlar" sayfasında sayfa içinde süzülür (S1dEkranlarTestleri.Arama_TURKCE_KATLAR_sayim_Sayim_i_bulur).

    // ── YETKİ HATASI: MENÜ BOŞALMAZ (sessiz yutma kaldırıldı) ───────────────

    [Fact]
    public async Task Yetki_istegi_hata_verince_menu_BOSALMAZ_ve_bant_cikar()
    {
        // ÖLÇÜLEN KÖK SEBEP (21 Eylül): YetkiServisi.cs:30 `catch { moduller = []; }`
        // → menü 36 bağlantıdan 1'e düşüyordu ve TEK BİR UYARI bile çıkmıyordu.
        // Senaryo: önce 200 ile menü DOLAR, sonra tazelemede 500 döner ve bağlantı sayısının
        // DÜŞMEDİĞİ ölçülür. TEMA TEMİZLİĞİ 17.10: menünün bütünü = "Tüm ekranlar" sayfası
        // (kenar çubuğu kaldırıldı); bant ve "yeniden dene" o sayfadadır. Üst menünün beş
        // sözcüğü de AYNI son bilinen listeden süzülür (aşağıda ayrıca ölçülür).
        yetkiKodu = HttpStatusCode.OK;
        OturumAc("YONETICI");   // SAHIP DEĞİL: modül süzgeci gerçekten işlesin
        var oturum = Services.GetRequiredService<OturumServisi>();
        var yetki = Services.GetRequiredService<YetkiServisi>();
        var e = Render<Ekranlar>();
        var ust = Render<YildizUstMenu>();

        e.WaitForAssertion(() => Assert.True(yetki.Yuklendi, "ilk yükleme 200 ile dolmalı"));
        e.WaitForAssertion(() => Assert.True(e.FindAll("a.yz-kare").Count > 1));
        var doluSayi = e.FindAll("a.yz-kare").Count;
        var ustDolu = ust.FindAll("#yildiz-menu a.yildiz-menu-oge").Count;
        Assert.True(ustDolu > 2, $"ön koşul: üst menü modül sözcüklerini taşımalı (ölçülen {ustDolu})");

        // Tazeleme: aynı oturum, BAŞKA kullanıcı → yetkiler yeniden çekilir ve 500 döner.
        yetkiKodu = HttpStatusCode.InternalServerError;
        oturum.KullaniciAta(new OturumKullanici("u2", "Test", "İkinci", "t2@ornek.com",
            "YONETICI", "s1", null, ArayuzModuServisi.Profesyonel));
        await e.InvokeAsync(() => yetki.HazirlaAsync());

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ekranlar-yetki-hatasi")));
        Assert.NotNull(e.Find("#ekranlar-yetki-yeniden"));           // gerçek istek atan düğme
        var hataSayi = e.FindAll("a.yz-kare").Count;
        Assert.True(hataSayi >= doluSayi,
            $"yetki hatasında menü BOŞALMAMALI — son bilinen liste ayakta kalır " +
            $"(önce {doluSayi}, hatadan sonra {hataSayi})");
        Assert.True(ust.FindAll("#yildiz-menu a.yildiz-menu-oge").Count >= ustDolu,
            "yetki hatasında üst menü sözcükleri de düşmemeli");
    }

    [Fact]
    public void YetkiServisi_kaynaginda_SESSIZ_YUTMA_yoktur()
    {
        var kaynak = CssKaynak.YorumsuzKod(
            CssKaynak.Metin("Kuyumcu.Web", "Servisler", "YetkiServisi.cs"));

        Assert.DoesNotContain("catch { moduller = []; }", kaynak, StringComparison.Ordinal);
        Assert.DoesNotContain("moduller = [];", kaynak.Split("catch").Last(), StringComparison.Ordinal);
        Assert.Contains("Hata = ex.Message;", kaynak, StringComparison.Ordinal);
    }

    // ── (5) ARAÇ ŞERİDİ DİSİPLİNİ — TEK YER ─────────────────────────────────

    [Fact]
    public void Arac_seridi_hizasi_TEK_yerden_gelir_ve_ortadan_hizalamaz()
    {
        var css = CssKaynak.YorumsuzCss(
            CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));

        // Kusurun kendisi: `align-items: center` farklı yapıdaki çocukları
        // ortadan hizalıyordu → etiketli seçici ile etiketsiz arama kutusunun
        // üst kenarları 20+px kayıyordu.
        Assert.DoesNotContain(
            ".sayfa-basi-filtre, .sayfa-basi-eylem { display: flex; flex-wrap: wrap; gap: var(--bosluk-2); align-items: center; }",
            css, StringComparison.Ordinal);
        // ⚠ TEST AÇIĞI KAPATILDI (M37-3): "css içinde bir yerde flex-end var"
        //   demek YETMİYOR — `.sayfa-basi` başlığı zaten flex-end'di ve şerit
        //   center'a çevrildiğinde test yeşil kalıyordu. Artık ŞERİDİN KENDİ
        //   kuralı ayrıştırılır.
        var serit = System.Text.RegularExpressions.Regex.Match(
            css, @"\.sayfa-basi-filtre,\s*\.sayfa-basi-eylem\s*\{([^}]*)\}");
        Assert.True(serit.Success, ".sayfa-basi-filtre/.sayfa-basi-eylem ortak kuralı bulunamadı");
        Assert.Contains("align-items: flex-end", serit.Groups[1].Value, StringComparison.Ordinal);
        Assert.DoesNotContain("align-items: center", serit.Groups[1].Value, StringComparison.Ordinal);
        // Etiketsiz çocuk, etiket satırı kadar boşluk alır → üst kenarlar eşit.
        Assert.Contains("--arac-etiket-bosluk", css, StringComparison.Ordinal);
        // Etiket KESİLMEZ.
        Assert.Contains("text-overflow: clip", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Hiza_belirtecleri_belirtecler_css_te_TANIMLIDIR()
    {
        // "Tek yer" iddiası ancak belirteçler gerçekten tanımlıysa doğrudur;
        // tanımsız var() sessizce 0'a düşer ve hizayı yine bozardı (md.1).
        var belirtec = CssKaynak.YorumsuzCss(
            CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css"));

        foreach (var ad in new[]
                 { "--arac-etiket-satir", "--arac-etiket-bosluk",
                   "--arac-alan-genislik", "--arac-arama-genislik" })
            Assert.Contains(ad + ":", belirtec, StringComparison.Ordinal);
    }

    [Fact]
    public void Urunler_ekraninda_MUKERRER_arama_yuzeyi_yoktur()
    {
        // ÖNCE: /urunler'de ÜÇ yüzey vardı — #urun-arama, ayrı tam genişlikte
        // #urun-barkod-kutu + "Barkod okut" düğmesi. Kurucu "inputlar seçme
        // yerleri dengesiz" derken bunu görüyordu.
        var razor = CssKaynak.YorumsuzKod(
            CssKaynak.Metin("Kuyumcu.Web", "Pages", "Urunler.razor"));

        Assert.DoesNotContain("urun-barkod-kutu", razor, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(razor, @"<Arama\s"));
        // Barkod okutma kaybolmadı: ana arama kutusunun Enter'ına bağlandı.
        Assert.Contains("Enter=\"BarkodBul\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Serbest_ekranlar_kendi_basliginda_ELLE_duzen_kurmaz()
    {
        // SAYFA_BASI_DISI_DUZEN: bir ekran başlığı+filtresini SayfaBasi dışında
        // elle diziyorsa hizayı tek yerden düzeltmek İMKÂNSIZ olur; ekran başına
        // yama yasağının kod karşılığı budur.
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages");
        var kilitli = new HashSet<string>(StringComparer.Ordinal)
        {
            // Kapsam kilidi (P-37 spesifikasyonu): bu ekranlara DOKUNULMADI.
            "Ayarlar.razor", "Faturalar.razor", "KarZarar.razor", "KasaRaporu.razor",
            "Kilavuz.razor", "Pos.razor", "Raporlar.razor", "SatisAnalizi.razor",
            "Sayim.razor", "Siparisler.razor",
            // Kabuk dışı ekranlar: kenar çubuğu/başlık taşımazlar.
            "Giris.razor", "Kurulum.razor", "PusulaBelgesi.razor", "Cikti.razor",
        };

        var suclular = new List<string>();
        foreach (var yol in Directory.GetFiles(kok, "*.razor"))
        {
            var ad = Path.GetFileName(yol);
            if (kilitli.Contains(ad)) continue;
            var kaynak = CssKaynak.YorumsuzKod(File.ReadAllText(yol));
            if (!kaynak.Contains("<SayfaBasi", StringComparison.Ordinal)) continue;
            // SayfaBasi kullanan ekran, kendi başlık düzenini AYRICA kurmaz.
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    kaynak, @"class=""[^""]*sayfa-basi[^""]*"""))
                suclular.Add(ad);
        }

        Assert.True(suclular.Count == 0,
            "SayfaBasi dışında elle başlık düzeni kuran ekran(lar): " + string.Join(", ", suclular));
    }
}
