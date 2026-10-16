using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Kilavuz;
using Kuyumcu.Tests.Araclar;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-11) — KILAVUZ TEK KAYNAKTAN ÜRETİLİR
//
// Sınanan sözleşme:
//   • wwwroot/kilavuz.html = Üret(docs/KULLANIM_KILAVUZU.md) BİREBİR; elle
//     düzenleme ya da üretilmeden bırakılan kaynak değişikliği KIRMIZI.
//     Yeniden üretmek için: dotnet/kilavuz-uret.sh (KILAVUZ_YAZ=1 ile bu test).
//   • RotaBasliklari.Harita'daki HER rota kaynakta TAM BİR kez işaretli ve
//     işaretin bağlı olduğu başlık ekranın menü adını taşır (UX-S31).
//   • /kilavuz ekranı her rotaya kılavuzda GERÇEKTEN var olan çıpayla bağlanır.
//   • Kullanıcı belgesinde sabit port / eski başlatıcı adı yok; kısıt beyanı
//     HARİÇ listesinden GÖMÜLÜR; kılavuz betik/çerçeve taşımaz (oturumsuz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KilavuzUretimTestleri : BunitContext
{
    internal static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı.");
    }

    internal static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;
    internal static string KaynakYolu() => Path.Combine(DepoKoku(), "docs", "KULLANIM_KILAVUZU.md");
    internal static string SablonYolu() => Path.Combine(DepoKoku(), "docs", "kilavuz-sablon.html");
    internal static string HaricYolu() => Path.Combine(DepoKoku(), "docs", "HARIC-LISTESI.md");
    internal static string HtmlYolu() => Path.Combine(DotnetKoku(), "Kuyumcu.Web", "wwwroot", "kilavuz.html");

    /// <summary>Diskteki üç kaynaktan üretim (karmalar dosya baytlarından).</summary>
    internal static KilavuzUretici.Sonuc DisktenUret()
    {
        var md = File.ReadAllBytes(KaynakYolu());
        var sablon = File.ReadAllBytes(SablonYolu());
        var haric = File.ReadAllBytes(HaricYolu());
        return KilavuzUretici.Uret(
            Encoding.UTF8.GetString(md), Encoding.UTF8.GetString(sablon), Encoding.UTF8.GetString(haric),
            KilavuzUretici.Sha256(md), KilavuzUretici.Sha256(haric), KilavuzUretici.Sha256(sablon));
    }

    public KilavuzUretimTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── 1) Üretilmiş ve güncel ───────────────────────────────────────────────

    [Fact]
    public void Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL()
    {
        var uretilen = DisktenUret().Html;

        // Yeniden üretme yolu (dotnet/kilavuz-uret.sh): KILAVUZ_YAZ=1 → dosyaya yaz.
        if (Environment.GetEnvironmentVariable("KILAVUZ_YAZ") == "1")
            File.WriteAllText(HtmlYolu(), uretilen, new UTF8Encoding(false));

        var diskte = File.ReadAllText(HtmlYolu()).Replace("\r\n", "\n");
        Assert.True(diskte == uretilen,
            "wwwroot/kilavuz.html docs/KULLANIM_KILAVUZU.md'den üretilenle AYNI DEĞİL — ya HTML elle " +
            "düzenlendi ya da kaynak değişip üretilmedi. Çözüm: dotnet/kilavuz-uret.sh");
    }

    [Fact]
    public void Uretim_DAMGASI_kaynak_karmasini_tasir()
    {
        var html = File.ReadAllText(HtmlYolu());
        var karma = KilavuzUretici.Sha256(File.ReadAllBytes(KaynakYolu()));
        Assert.Contains($"kaynak-sha256: {karma}", html, StringComparison.Ordinal);
        Assert.Contains("ELLE DÜZENLEMEYİN", html, StringComparison.Ordinal);
    }

    // ── 2) Her rota tam bir kez ──────────────────────────────────────────────

    [Fact]
    public void RotaBasliklari_HER_rota_kilavuzda_TAM_BIR_kez_isaretli()
    {
        var isaretler = DisktenUret().Ekranlar.Select(e => RotaBasliklari.Duzgunlestir(e.Rota)).ToList();
        var rotalar = RotaBasliklari.Harita.Keys.Select(RotaBasliklari.Duzgunlestir).ToHashSet(StringComparer.Ordinal);

        var eksik = rotalar.Where(r => !isaretler.Contains(r)).OrderBy(x => x).ToList();
        var hayalet = isaretler.Where(r => !rotalar.Contains(r)).ToList();
        var tekrar = isaretler.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(eksik.Count == 0, "Kılavuzda bölümü OLMAYAN ekran(lar): " + string.Join(", ", eksik));
        Assert.True(hayalet.Count == 0, "Kılavuzda var olmayan rota işaretli: " + string.Join(", ", hayalet));
        Assert.True(tekrar.Count == 0, "Aynı ekran iki kez işaretli: " + string.Join(", ", tekrar));
        Assert.Equal(rotalar.Count, isaretler.Count);
    }

    [Fact]
    public void Ekran_isaretinin_basligi_ekranin_MENU_ADINI_tasir()
    {
        var yanlis = new List<string>();
        foreach (var e in DisktenUret().Ekranlar)
        {
            var ad = RotaBasliklari.Harita[e.Rota](Sozlukler.Tr);
            if (!e.Baslik.Contains(ad, StringComparison.OrdinalIgnoreCase))
                yanlis.Add($"{e.Rota}: başlık '{e.Baslik}' ekran adı '{ad}' içermiyor (satır {e.Satir})");
        }
        Assert.True(yanlis.Count == 0, string.Join("\n", yanlis));
    }

    [Fact]
    public void Her_ekran_capasi_uretilen_HTML_de_gercek_id_olarak_durur()
    {
        var html = File.ReadAllText(HtmlYolu());
        foreach (var rota in RotaBasliklari.Harita.Keys)
            Assert.Contains($"id=\"{KilavuzCapasi.Ekran(rota)}\"", html, StringComparison.Ordinal);
    }

    // ── 3) /kilavuz ekranı ───────────────────────────────────────────────────

    [Fact]
    public void Kilavuz_ekrani_HER_rotaya_kilavuzdaki_GERCEK_capayla_baglanir()
    {
        var html = File.ReadAllText(HtmlYolu());
        var idler = Regex.Matches(html, "id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        var baglar = c.FindAll("#kl-ekranlar a");
        Assert.Equal(RotaBasliklari.Harita.Count, baglar.Count);
        foreach (var a in baglar)
        {
            var href = a.GetAttribute("href") ?? "";
            Assert.StartsWith(KilavuzCapasi.KilavuzAdresi + "#", href);
            Assert.Contains(href.Split('#')[1], idler);
            Assert.Equal("_blank", a.GetAttribute("target"));
            Assert.Contains("noopener", a.GetAttribute("rel") ?? "");
            Assert.False(string.IsNullOrWhiteSpace(a.TextContent));
        }
        // Sayı istemciye gömülmez: açıklama Harita'nın sayısını söyler.
        Assert.Contains(RotaBasliklari.Harita.Count.ToString(), c.Find("#kl-ekran-aciklama").TextContent);
    }

    [Fact]
    public void Kilavuz_ekrani_ekran_adlarini_SOZLUKTEN_alir_kayit_rotasini_belirtir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        // S1-g: kılavuz ekran adı = menü adı (MenuAdlari, tuval) — ESKİ Nav.Cariler.
        Assert.Equal(MenuAdlari.Tr.Musteriler.Ad, c.Find($"#kl-{KilavuzCapasi.Ekran("/cariler")}").TextContent.Trim());
        Assert.Contains(KilavuzSozlugu.KayitAyrintisi("tr"),
            c.Find($"#kl-{KilavuzCapasi.Ekran("/siparisler/{Id}")}").TextContent);
    }

    // ── 4) Kullanıcı belgesi kuralları ───────────────────────────────────────

    [Fact]
    public void Kullanici_belgesinde_SABIT_PORT_ve_eski_baslatici_YOK()
    {
        foreach (var metin in new[] { File.ReadAllText(KaynakYolu()), File.ReadAllText(HtmlYolu()) })
        {
            Assert.DoesNotMatch(new Regex(@"localhost:\d|127\.0\.0\.1:\d|https?://[^\s""<]*:\d{2,5}"), metin);
            Assert.DoesNotContain("start.bat", metin, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stop.bat", metin, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Kisayol_adresi_bolumu_KESIN_yazilir_K26()
    {
        // K-26 kararı geldi: "koşullu" değil, kesin.
        var md = File.ReadAllText(KaynakYolu());
        Assert.Contains("kurulumda sorulan adres kısayola yazılır", md, StringComparison.Ordinal);
        Assert.DoesNotContain("K-26 gelmezse", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Haric_beyani_HARIC_LISTESI_nden_GOMULUR()
    {
        var md = File.ReadAllText(KaynakYolu());
        Assert.Contains(KilavuzUretici.HaricIsareti, md, StringComparison.Ordinal);

        var html = File.ReadAllText(HtmlYolu());
        var satirlar = KilavuzUretici.HaricSatirlari(File.ReadAllText(HaricYolu()));
        Assert.True(satirlar.Count >= 8, $"HARİÇ tablosu {satirlar.Count} satır — ayrıştırıcı bozulmuş olabilir.");
        foreach (var (kalem, _) in satirlar)
            Assert.Contains(KilavuzUretici.Satir(kalem), html, StringComparison.Ordinal);
    }

    [Fact]
    public void Kilavuz_BETIK_CERCEVE_ve_veri_cagrisi_TASIMAZ_oturumsuz_okunur()
    {
        var html = File.ReadAllText(HtmlYolu());
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fetch(", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/", html, StringComparison.Ordinal);
        Assert.Contains("OTURUMSUZ ERİŞİM KARARI", File.ReadAllText(SablonYolu()), StringComparison.Ordinal);
    }

    [Fact]
    public void Kilavuz_mevzuat_ESIK_RAKAMI_tasimaz()
    {
        // Mevzuat kuralı: eşik sayısı kullanıcı belgesine gömülmez.
        var md = File.ReadAllText(KaynakYolu());
        Assert.DoesNotMatch(new Regex(@"185[._ ]?000|30[._]000|\bmd\.\s*\d|madde\s+\d", RegexOptions.IgnoreCase), md);
    }

    // ── 5) Üreticinin kendi kuralları (saf) ──────────────────────────────────

    private const string Sablon = "<title>{{BASLIK}}</title>{{DAMGA}}<nav>{{ICINDEKILER}}</nav>{{KAPAK}}<main>{{ICERIK}}</main>";
    private const string Haric = "## 1. HARİÇ\n\n| # | Kalem | Durum | Kanıt |\n|---|---|---|---|\n| H-1 | **e-Fatura** | **HARİÇ** | x |\n";

    private static KilavuzUretici.Sonuc Uret(string md) => KilavuzUretici.Uret(md, Sablon, Haric, "k", "h", "s");

    [Fact]
    public void Uretici_cipasiz_bolumu_REDDEDER()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Uret("# K\n\n## Cariler\n\nmetin\n"));
        Assert.Contains("çıpası yok", ex.Message);
    }

    [Fact]
    public void Uretici_ayni_cipayi_iki_kez_REDDEDER()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Uret("# K\n\n## A {#a}\n\n## B {#a}\n"));
        Assert.Contains("İKİNCİ", ex.Message);
    }

    [Fact]
    public void Uretici_ayni_ekrani_iki_kez_REDDEDER()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Uret("# K\n\n## A {#a}\n\n<!-- ekran: /cariler -->\n\n## B {#b}\n\n<!-- ekran: /cariler -->\n"));
        Assert.Contains("İKİNCİ", ex.Message);
    }

    [Fact]
    public void Uretici_metni_KACIRIR_kaynaktaki_isaretleme_calismaz()
    {
        var html = Uret("# K\n\n## A {#a}\n\n<script>alert(1)</script> & \"x\"\n").Html;
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; &amp; &quot;x&quot;", html);
    }

    [Fact]
    public void Uretici_ekran_isaretini_capaya_ve_basliga_baglar()
    {
        var s = Uret("# K\n\n## Cariler {#cariler}\n\n<!-- ekran: /cariler -->\n\nmetin\n");
        var e = Assert.Single(s.Ekranlar);
        Assert.Equal("/cariler", e.Rota);
        Assert.Equal("Cariler", e.Baslik);
        Assert.Contains("<span class=\"ekran-capa\" id=\"e-cariler\"></span>", s.Html);
        Assert.Contains("<section id=\"cariler\">", s.Html);
        Assert.Contains("<a href=\"#cariler\">Cariler</a>", s.Html);
    }

    [Fact]
    public void Uretici_HARIC_tablosunu_gomer_kanit_sutununu_BASMAZ()
    {
        var html = Uret("# K\n\n## Olmayanlar {#haric}\n\n" + KilavuzUretici.HaricIsareti + "\n").Html;
        Assert.Contains("<strong>e-Fatura</strong>", html);
        Assert.Contains("<strong>HARİÇ</strong>", html);
        Assert.DoesNotContain("<td>x</td>", html);
    }

    [Fact]
    public void Uretici_dis_baglantiyi_REDDEDER()
    {
        Assert.Throws<InvalidOperationException>(() => Uret("# K\n\n## A {#a}\n\n[tık](https://ornek.test)\n"));
    }

    [Theory]
    [InlineData("/", "e-panel")]
    [InlineData("/urunler/agac", "e-urunler-agac")]
    [InlineData("/siparisler/{Id}", "e-siparisler-kayit")]
    [InlineData("/gider-pusulalari/{Id}/belge", "e-gider-pusulalari-kayit-belge")]
    [InlineData("/cikti/{Tur}/{Id}", "e-cikti-kayit-kayit")]
    public void KilavuzCapasi_rotadan_TEKIL_capa_uretir(string rota, string beklenen)
        => Assert.Equal(beklenen, KilavuzCapasi.Ekran(rota));

    [Fact]
    public void KilavuzCapasi_butun_rotalar_icin_CAKISMASIZDIR()
    {
        var capalar = RotaBasliklari.Harita.Keys.Select(KilavuzCapasi.Ekran).ToList();
        Assert.Equal(capalar.Count, capalar.Distinct(StringComparer.Ordinal).Count());
    }
}
