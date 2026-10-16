using System.Net;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
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
        KabukServisi.Ortak.GorunumSubesiYerelAta(null);
    }

    private IRenderedComponent<KenarCubugu> Menu(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null,
                                ArayuzModuServisi.Profesyonel));
        return Render<KenarCubugu>();
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
        // ÖNCE: 40 satır × 2 = 80 görünür ↑/↓ düğmesi.
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));

        Assert.Empty(e.FindAll(".kenar-tasi"));
        Assert.Empty(e.FindAll("[id^=kenar-yukari-]"));
        Assert.Empty(e.FindAll("[id^=kenar-asagi-]"));
        Assert.DoesNotContain("↑", e.Find("#kenar-nav").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("↓", e.Find("#kenar-nav").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Suruklenebilir_her_satir_KLAVYE_yolu_tasir_tutamac_ve_yildiz_GORUNMEZ()
    {
        // Ok düğmeleri kalktı ama erişilebilirlik KALKMADI: satır odaklanabilir
        // ve Alt+↑/↓ sıralamayı hâlâ değiştirir.
        // S1-g YENİDEN YAZIM — ESKİ: Suruklenebilir_her_satirda_tutamac_ve_KLAVYE_yolu_vardir
        // (her satırda '⠿' .kenar-tutamac beklenirdi). YENİ: kurucunun tuvalinde
        // (Panel.dc.html, Profesyonel.dc.html) menü satırında tutamaç ve yıldız
        // YOKTUR; canlı ölçümde 38 + 38 görünüyordu. Sıralama ÖZELLİĞİ duruyor:
        // satırın kendisi sürüklenir (data-sira-anahtar) + Alt+↑/↓.
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));

        var satirlar = e.FindAll(".kenar-satir[data-sira-anahtar]");
        Assert.NotEmpty(satirlar);
        Assert.All(satirlar, s =>
        {
            Assert.Equal("0", s.GetAttribute("tabindex"));
            Assert.False(string.IsNullOrEmpty(s.GetAttribute("data-sira-anahtar")));
            Assert.Null(s.QuerySelector(".kenar-tutamac"));
            Assert.Null(s.QuerySelector(".kenar-yildiz"));
        });
        Assert.DoesNotContain("⠿", e.Find("#kenar-nav").TextContent, StringComparison.Ordinal);
    }

    // ── (4) MENÜDE KALICI PARAGRAF YOK ──────────────────────────────────────

    [Fact]
    public void Menude_kalici_paragraf_SIFIRDIR()
    {
        // ÖNCE: menünün tepesinde "Yıldıza bastığın ekranlar…" 3 satırlık
        // paragraf + altta kapsam kilidi paragrafı duruyordu.
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));

        Assert.Empty(e.FindAll("#kenar-nav p"));
        Assert.Empty(e.FindAll(".kenar-grup-ipucu"));
        // S1-g: "Sırayı sıfırla" yalnız KAYITLI sıra varken çizilir (tuvalde
        // menüde düğme yok; silinecek şey yokken düğme etkisizdir). Kayıtlı sıra
        // yokken düğme de yoktur; kural açıklaması düğmeyle birlikte gelir.
        Assert.Empty(e.FindAll("#kenar-sira-sifirla"));
    }

    // ── (3) MENÜ ARAMASI YENİDEN TASARLANDI ─────────────────────────────────

    // ⚠ S1-j (kurucu 24.09 14:40): kutu artık YALNIZ menüyü süzer; kimliği
    //   #menu-arama-girdi. Kayıt araması (#kabuk-arama-girdi) üst bar paletine
    //   taşındı (KabukAramaEkranTestleri). Kısayol rozeti menü kutusundan kalktı:
    //   Ctrl+K artık KAYIT paletini açar, menü kutusunda göstermek yanıltırdı.

    [Fact]
    public void Menu_aramasi_TEK_PARCA_alandir_ve_ikon_tasir()
    {
        var e = Menu();
        var satir = e.Find("#menu-arama .menu-arama-satir");

        Assert.NotNull(satir.QuerySelector(".menu-arama-ikon"));
        Assert.NotNull(satir.QuerySelector("#menu-arama-girdi"));
        // Kayıt araması menü kutusunda DEĞİL (S1-j).
        Assert.Null(satir.QuerySelector("#kabuk-arama-girdi"));
        // T-K2 (tuval Panel.dc.html): yer tutucu "Ekran ara" — kayıt ifadesi YOK.
        Assert.Equal("Ekran ara", e.Find("#menu-arama-girdi").GetAttribute("placeholder"));
    }

    [Fact]
    public void Menu_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir()
    {
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        Assert.Empty(e.FindAll("#menu-arama-temizle"));

        e.Find("#menu-arama-girdi").Input("kasa");
        // Değerin çizilmesi beklenir (statik KabukServisi olayı paralel sınıflardan
        // yeniden çizim tetikleyebilir — tam süitte ölçülen kararsızlık).
        var temizle = e.WaitForElement("#menu-arama-temizle");   // DoD EK md.1: gerçek iş yapar
        Assert.NotEmpty(e.FindAll(".kenar-satir[hidden]"));

        temizle.Click();

        Assert.Empty(e.FindAll("#menu-arama-temizle"));
        Assert.Equal("", e.Find("#menu-arama-girdi").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Menu_aramasi_menunun_DISINDA_pencere_ACMAZ()
    {
        // S1-j: sonuç menünün İÇİNDE süzülür; kutunun altında/üstünde ayrı
        // sonuç paneli, "KAYITLAR" bölümü ve "En çok 10 kayıt" notu YOKTUR.
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        e.Find("#menu-arama-girdi").Input("sat");
        e.WaitForElement("#menu-arama-temizle");

        Assert.Empty(e.FindAll("#kabuk-arama-paneli"));
        Assert.Empty(e.FindAll(".kabuk-arama-paneli"));
        Assert.Empty(e.FindAll("#kabuk-arama-kayitlar"));
        Assert.DoesNotContain("En çok", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Kayıtlar", e.Markup, StringComparison.Ordinal);
    }

    // ── YETKİ HATASI: MENÜ BOŞALMAZ (sessiz yutma kaldırıldı) ───────────────

    [Fact]
    public async Task Yetki_istegi_hata_verince_menu_BOSALMAZ_ve_bant_cikar()
    {
        // ÖLÇÜLEN KÖK SEBEP (21 Eylül): YetkiServisi.cs:30 `catch { moduller = []; }`
        // → menü 36 bağlantıdan 1'e düşüyor, komut paleti hiçbir ekran bulamıyor
        // ve TEK BİR UYARI bile çıkmıyordu.
        // ⚠ DOĞRULAMA BULGUSU (21 Eylül): bu test ÖNCE hatayı İLK yüklemede
        //   üretiyordu; "son bilinen liste" hiç dolmadığı için mutasyon
        //   (catch'e `moduller = [];` geri konması) testi KIRMIYORDU. Artık
        //   senaryo gerçeğe uygun: önce 200 ile menü DOLAR, sonra tazelemede
        //   500 döner ve bağlantı sayısının DÜŞMEDİĞİ ölçülür.
        yetkiKodu = HttpStatusCode.OK;
        var e = Menu("YONETICI");   // SAHIP DEĞİL: modül süzgeci gerçekten işlesin
        var oturum = Services.GetRequiredService<OturumServisi>();
        var yetki = Services.GetRequiredService<YetkiServisi>();

        e.WaitForAssertion(() => Assert.True(yetki.Yuklendi, "ilk yükleme 200 ile dolmalı"));
        var doluSayi = e.FindAll(".kenar-nav a").Count;
        Assert.True(doluSayi > 1, $"ön koşul: menü dolmalı (ölçülen {doluSayi})");

        // Tazeleme: aynı oturum, BAŞKA kullanıcı → yetkiler yeniden çekilir ve 500 döner.
        yetkiKodu = HttpStatusCode.InternalServerError;
        oturum.KullaniciAta(new OturumKullanici("u2", "Test", "İkinci", "t2@ornek.com",
            "YONETICI", "s1", null, ArayuzModuServisi.Profesyonel));
        await e.InvokeAsync(() => yetki.HazirlaAsync());

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kenar-yetki-hatasi")));
        Assert.NotNull(e.Find("#kenar-yetki-yeniden"));           // gerçek istek atan düğme
        var hataSayi = e.FindAll(".kenar-nav a").Count;
        Assert.True(hataSayi >= doluSayi,
            $"yetki hatasında menü BOŞALMAMALI — son bilinen liste ayakta kalır " +
            $"(önce {doluSayi}, hatadan sonra {hataSayi})");
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
