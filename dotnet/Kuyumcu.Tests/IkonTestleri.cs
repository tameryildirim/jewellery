using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İKON SETİ — paket UX-5 (brif mührü S6-i · çelişki çözümü Ç7)
//
// SÖZLEŞME: ekrandaki her ikon TEK dosyadan gelir (wwwroot/img/ikon.svg).
// Burada sınanan üç şey:
//   1) SPRITE SAĞLAM MI  — lisans başlığı, benzersiz id, currentColor, 24'lük
//      viewBox. Lisanssız/kaynaksız vendored kopya kapıya takılır (K14).
//   2) HEDEF VAR MI      — ekranda çizilen her <use href="…#ikon-x"> için
//      sprite'ta bir <symbol id="ikon-x"> bulunur. Sembol silinirse ekranda
//      SESSİZCE boş kare çıkardı; burada kırmızı olur.
//   3) EMOJİ GERİ SIZDI MI — Layout/ ve Bilesenler/ altında emoji karakteri
//      sıfırdır (yorumlar hariç; denetim.sh'ın aynı sayacının ikizi).
//
// ⚠ NEDEN DOSYA AYRIŞTIRMA: bUnit CSS/SVG'yi ÇÖZMEZ — <use> hedefini takip
//   etmez. "İkon göründü mü" sorusu ancak hedefin varlığıyla + canlı turdaki
//   ekran görüntüsüyle yanıtlanır. İkisi de bu pakette var.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class IkonTestleri : BunitContext
{
    private static string Sprite() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "img", "ikon.svg");

    /// <summary>Sprite'taki sembol kimlikleri (ikon- öneki atılmış hâlde).</summary>
    private static HashSet<string> Semboller() =>
        Regex.Matches(Sprite(), @"<symbol\s+id=""ikon-([a-z0-9-]+)""")
             .Select(m => m.Groups[1].Value).ToHashSet();

    /// <summary>Ikonlar sınıfındaki sabitlerin DEĞERLERİ (ad → sembol adı).</summary>
    private static Dictionary<string, string> Sabitler() =>
        typeof(Ikonlar).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

    // ── 1) SPRITE SAĞLAMLIĞI ────────────────────────────────────────────────

    [Fact]
    public void Sprite_lisans_surum_ve_kaynak_URL_basligi_tasir()
    {
        var s = Sprite();
        Assert.Contains("ISC License", s);
        Assert.Contains("Lucide", s);
        Assert.Contains("1.47.0", s);
        Assert.Contains("github.com/lucide-icons/lucide", s);
        // Feather türevi semboller MIT'tir — atıf düşerse lisans ihlali olur.
        Assert.Contains("Cole Bemis", s);
    }

    [Fact]
    public void Lisans_tam_metni_kaynagindan_kopyalanmis_olarak_durur()
    {
        var l = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "img", "IKON-LISANS.txt");
        Assert.Contains("ISC License", l);
        Assert.Contains("Lucide Icons and Contributors", l);
        Assert.Contains("The MIT License (MIT)", l);
        Assert.Contains("Cole Bemis", l);
    }

    [Fact]
    public void Her_symbol_benzersiz_24lik_viewBox_ve_currentColor_tasir()
    {
        var s = Sprite();
        var id = Regex.Matches(s, @"<symbol\s+id=""([^""]+)""").Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(id);
        Assert.Equal(id.Count, id.Distinct().Count());          // çift id → <use> yanlış sembolü alır
        Assert.All(id, x => Assert.StartsWith("ikon-", x));

        foreach (Match m in Regex.Matches(s, """<symbol\b[^>]*>"""))
        {
            Assert.Contains("viewBox=\"0 0 24 24\"", m.Value);
            Assert.Contains("stroke=\"currentColor\"", m.Value);
            Assert.Contains("fill=\"none\"", m.Value);
        }
    }

    [Fact]
    public void Sprite_disaridan_hicbir_kaynak_cagirmaz()
    {
        // CDN/web font/npm YASAK (Mehmet K3): dosya kendi kökümüzden gelir,
        // CSP'de yeni istisna gerekmez. Yorumlardaki kaynak URL'si atıftır.
        var kod = YorumsuzSvg(Sprite())
            .Replace("xmlns=\"http://www.w3.org/2000/svg\"", "");   // ad alani, ag istegi degil
        Assert.DoesNotContain("http://", kod);
        Assert.DoesNotContain("https://", kod, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", kod);
        Assert.DoesNotContain("<image", kod);
    }

    // ── 2) HEDEF VAR MI ─────────────────────────────────────────────────────

    [Fact]
    public void Ikonlar_sabitleri_ile_sprite_sembolleri_BIREBIR_ayni()
    {
        var sabit = Sabitler();
        var sembol = Semboller();

        var eksik = sabit.Where(p => !sembol.Contains(p.Value)).ToList();
        Assert.True(eksik.Count == 0,
            "Sprite'ta karşılığı olmayan sabit: " + string.Join(", ", eksik.Select(p => $"{p.Key}={p.Value}")));

        var artik = sembol.Except(sabit.Values).ToList();
        Assert.True(artik.Count == 0,
            "Hiçbir sabitin göstermediği sembol (ölü ağırlık): " + string.Join(", ", artik));
    }

    [Fact]
    public void Ekranda_cizilen_her_use_hedefi_spritete_var()
    {
        var sembol = Semboller();
        var hedefler = new List<string>();

        foreach (var svg in KenarCubuguCiz().FindAll("use")) hedefler.Add(svg.GetAttribute("href")!);
        foreach (var svg in Render<BosDurum>(p => p.Add(x => x.Mesaj, "Kayıt yok")).FindAll("use"))
            hedefler.Add(svg.GetAttribute("href")!);
        foreach (var svg in Render<DegismezIpucu>(p => p
                     .Add(x => x.Varlik, "DenetimKaydi")
                     .Add(x => x.Gerekce, "Denetim kaydı değiştirilemez.")).FindAll("use"))
            hedefler.Add(svg.GetAttribute("href")!);

        Assert.NotEmpty(hedefler);
        foreach (var h in hedefler)
        {
            Assert.StartsWith("/img/ikon.svg#ikon-", h);        // tek dosya, kendi kökümüz
            Assert.Contains(h["/img/ikon.svg#ikon-".Length..], sembol);
        }
    }

    [Fact]
    public void Ikon_varsayilan_olarak_ekran_okuyucuya_gizlidir()
    {
        var e = Render<Ikon>(p => p.Add(x => x.Ad, Ikonlar.Bildirimler));
        var svg = e.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Null(svg.GetAttribute("role"));
        Assert.Null(svg.GetAttribute("aria-label"));
    }

    [Fact]
    public void Etiket_verilince_ikon_role_img_ve_aria_label_alir()
    {
        var e = Render<Ikon>(p => p.Add(x => x.Ad, Ikonlar.Uyari).Add(x => x.Etiket, "Uyarı"));
        var svg = e.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Uyarı", svg.GetAttribute("aria-label"));
        Assert.Null(svg.GetAttribute("aria-hidden"));           // ikisi bir arada olamaz
    }

    [Fact]
    public void Ikon_satir_ici_style_yazmaz_boyut_siniftan_gelir()
    {
        var e = Render<Ikon>(p => p.Add(x => x.Ad, Ikonlar.Kilit).Add(x => x.Sinif, "degismez-kilit"));
        var svg = e.Find("svg");
        Assert.Null(svg.GetAttribute("style"));
        Assert.Contains("ikon", svg.GetAttribute("class")!);
        Assert.Contains("degismez-kilit", svg.GetAttribute("class")!);
    }

    // ── 3) EMOJİ GERİ SIZDI MI ──────────────────────────────────────────────

    [Fact]
    public void Layout_ve_Bilesenler_altinda_emoji_KALMADI()
    {
        var ihlal = new List<string>();
        foreach (var klasor in new[] { "Layout", "Bilesenler" })
        {
            var kok = CssKaynak.Yol("Kuyumcu.Web", klasor);
            foreach (var dosya in Directory.EnumerateFiles(kok, "*.*", SearchOption.AllDirectories)
                         .Where(d => d.EndsWith(".razor") || d.EndsWith(".cs")))
            {
                var kod = YorumsuzRazor(File.ReadAllText(dosya));
                foreach (var (satir, no) in kod.Split('\n').Select((s, i) => (s, i + 1)))
                    if (satir.Any(EmojiMi))
                        ihlal.Add($"{klasor}/{Path.GetFileName(dosya)}:{no}");
            }
        }
        Assert.True(ihlal.Count == 0,
            "Emoji ikon geri sızdı (Ikon bileşeni kullanılmalı): " + string.Join(", ", ihlal));
    }

    /// <summary>
    /// U+1F300–1FAFF (vekil çift D83C-D83E), U+2600–27BF — brif S6-i aralığı.
    /// Ek olarak U+23E0–23FF ve U+2B00–2BFF: kenar çubuğundaki vade simgesi
    /// (U+23F0) dar tanımda SESSİZCE geçmişti; genişletme bilinçlidir.
    /// </summary>
    private static bool EmojiMi(char c) =>
        (c >= '\u2600' && c <= '\u27BF') || (c >= '\uD83C' && c <= '\uD83E')
        || (c >= '\u23E0' && c <= '\u23FF') || (c >= '\u2B00' && c <= '\u2BFF');

    // ── 4) K4 — MENÜ KİMLİK/ROTA ENVANTERİ DEĞİŞMEZ ─────────────────────────

    [Fact]
    public void KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni()
    {
        // K4: yalnız EKLENEN id kabul; silinen/değişen = engel. Menüde id yok,
        // kimliği ROTA taşır — bu yüzden sözleşme rota listesidir.
        string[] beklenen =
        [
            // Paket P-14 (Y-9): sıra artık GRUPLUDUR (MenuTanimi.GrupSirasi).
            // Rota kimlikleri DEĞİŞMEDİ — hiçbiri silinmedi/yeniden adlandırılmadı,
            // yalnız grup sırasına göre yeniden dizildi (K4 "silinen/değişen = engel"
            // kuralı rota kimliğine bakar, satır sırasına değil).
            // ── PAKET S1-d: "Tüm ekranlar" girişi (aramanın altında, grupların
            //    üstünde — tuval Panel.dc.html). S1-b'de KİLİTLİ DÜĞMEYDİ, S1-d
            //    karo sayfasını açınca bağlantı oldu; menü öğesi DEĞİLDİR. ──
            "/ekranlar",
            // ── Günlük İş ──
            "/", "/islemler", "/emanet", "/siparisler", "/bildirimler",
            // ── Müşteri ve Stok ──
            "/cariler", "/urunler", "/urunler/agac", "/urunler/etiketler",
            "/fiyatlandirma", "/sayim",
            // ── Para ve Banka ── (⚠ "evrak" modülü bu fikstürde YOK:
            //    /evraklar ve /evraklar/takvim bu yüzden listede değil)
            "/kasalar", "/vezne", "/pos", "/pos/tahsilatlar", "/banka", "/kur",
            // ── Belge ve Uyum ── (⚠ /uyum ve /gider-pusulalari rol kapısında)
            "/faturalar",
            // ── Dökümler ──
            "/raporlar", "/raporlar/kar-zarar", "/raporlar/vade-takibi",
            "/raporlar/islem-defteri", "/raporlar/satis-analizi", "/raporlar/kasa",
            // ── Yönetim ── (⚠ /isletme · /yetkiler · /denetim-izi · /kurulum ·
            //    /veri-disa-aktarim · /veri-ice-aktarim · /yedekler rol kapısında)
            "/kullanicilar",
            // ── T-K2: kullanıcı kartının dişlisi (tuval: kartta → /ayarlar) ──
            "/ayarlar",
            // ── Alt blok (grup dışı, her modda) — T-K2: kart menüsünde ──
            "/kilavuz", "/ayarlar",
        ];
        var e = KenarCubuguCiz();
        var rotalar = e.FindAll(".kenar-cubugu a").Select(a => a.GetAttribute("href")).ToArray();
        Assert.Equal(beklenen, rotalar);

        // Her menü satırı ikon + METİN taşır (renk/ikon tek başına anlam taşımaz).
        foreach (var a in e.FindAll(".kenar-nav a"))
        {
            Assert.Single(a.QuerySelectorAll("svg.ikon"));
            Assert.False(string.IsNullOrWhiteSpace(a.TextContent.Trim()));
        }
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    private sealed class SahteYetkiApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken t) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","emanet","sayim","uyum","gider","bildirim","siparis","denetim","yetki","isletme","fiyat","kur"]}""",
                    System.Text.Encoding.UTF8, "application/json"),
            });
    }

    private IRenderedComponent<KenarCubugu> KenarCubuguCiz()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteYetkiApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "PATRON", "s1", null, "profesyonel"));

        var e = Render<KenarCubugu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    /// <summary>Razor (@* *@), HTML (&lt;!-- --&gt;) ve C# yorumlarını siler.</summary>
    private static string YorumsuzRazor(string s)
    {
        s = Regex.Replace(s, @"@\*.*?\*@", "", RegexOptions.Singleline);
        s = Regex.Replace(s, @"<!--.*?-->", "", RegexOptions.Singleline);
        s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
        s = Regex.Replace(s, @"(?m)^\s*//.*$", "");
        s = Regex.Replace(s, @"(?m)//[^""'\n]*$", "");
        return s;
    }

    private static string YorumsuzSvg(string s) =>
        Regex.Replace(s, @"<!--.*?-->", "", RegexOptions.Singleline);
}
