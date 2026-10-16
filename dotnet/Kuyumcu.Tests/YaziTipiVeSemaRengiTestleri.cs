using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-b — (A) VENDORED YAZI TİPİ · (B) ŞEMA ↔ DURUM RENGİ AYRIMI
//
// (A) Kurucunun tuvali Manrope + Sora'yı Google Fonts <link> ile çekiyor.
//     CSP `font-src 'self'` bunu YASAKLIYOR (plan § 3 md.8). Dosyalar
//     wwwroot/font/ altına indirildi; bu testler "indirildi" iddiasını
//     KANITA bağlar: dosya var, woff2 imzası doğru, OFL metni yanında,
//     belirtecler.css onları KENDİ KÖKÜMÜZDEN çağırıyor ve dış alan adı yok.
//
// (B) TEMA-SISTEMI § 6.2 kuralı: vurgu ile her durum rengi arasında OKLCH hue
//     farkı ≥30° VEYA chroma oranı ≥2,5× olmalı. S1-a bu kuralın 64 çiftin
//     10'unda tutmadığını ÖLÇTÜ ve karar vermedi. Karar D-S1b-1 bu pakette
//     verildi (gerekçesi belirtecler.css başlığında): başarı/bilgi/uyarı
//     kaydırıldı, BORDO ↔ TEHLİKE çifti ise güvenlik uzlaşımı (kırmızı =
//     tehlike) gereği MUAF bırakıldı. Bu dosya hem kuralı hem de MUAFİYETİN
//     TEK OLDUĞUNU kilitler: ikinci bir sessiz istisna açılırsa test kırmızıya
//     döner.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YaziTipiVeSemaRengiTestleri
{
    private static string Belirtecler() =>
        CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");

    private static string FontYolu(string ad) =>
        CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "font", ad);

    // ── (A) VENDORED YAZI TİPİ ──────────────────────────────────────────────

    [Theory]
    [InlineData("manrope-latin.woff2")]
    [InlineData("manrope-latin-ext.woff2")]
    [InlineData("sora-latin.woff2")]
    [InlineData("sora-latin-ext.woff2")]
    public void Yazi_tipi_dosyasi_VARDIR_ve_gercek_woff2dir(string ad)
    {
        var yol = FontYolu(ad);
        Assert.True(File.Exists(yol), $"vendored yazı tipi yok: {ad}");

        var bayt = File.ReadAllBytes(yol);
        Assert.True(bayt.Length > 4000, $"{ad} şüpheli biçimde küçük: {bayt.Length} bayt");
        // woff2 sihirli imzası: "wOF2"
        Assert.Equal("wOF2", System.Text.Encoding.ASCII.GetString(bayt, 0, 4));
    }

    [Theory]
    [InlineData("OFL-Manrope.txt", "Manrope Project Authors")]
    [InlineData("OFL-Sora.txt", "Sora Project Authors")]
    public void Yazi_tipinin_OFL_LISANSI_yanindadir(string ad, string sahip)
    {
        var metin = File.ReadAllText(FontYolu(ad));

        Assert.Contains("SIL OPEN FONT LICENSE", metin.ToUpperInvariant(), StringComparison.Ordinal);
        Assert.Contains("Copyright", metin, StringComparison.Ordinal);
        Assert.Contains(sahip, metin, StringComparison.Ordinal);
    }

    [Fact]
    public void Belirtecler_yazi_tipini_KENDI_KOKUMUZDEN_cagirir_CDN_yoktur()
    {
        var css = Belirtecler();

        foreach (var dosya in new[]
        {
            "/font/manrope-latin.woff2", "/font/manrope-latin-ext.woff2",
            "/font/sora-latin.woff2", "/font/sora-latin-ext.woff2",
        })
            Assert.Contains(dosya, css, StringComparison.Ordinal);

        // CSP font-src 'self' — dış alan adı HİÇBİR biçimde geçmez.
        foreach (var disari in new[] { "fonts.googleapis.com", "fonts.gstatic.com", "//cdn", "http://", "https://" })
            Assert.DoesNotContain(disari, Regex.Replace(css, @"(?s)/\*.*?\*/", ""), StringComparison.Ordinal);
    }

    [Fact]
    public void Govde_Manrope_marka_Sora_ve_YEDEK_YIGIN_durur()
    {
        var css = Belirtecler();

        var ailesi = Regex.Match(css, @"--yazi-ailesi:\s*([^;]+);").Groups[1].Value;
        var marka = Regex.Match(css, @"--yazi-marka:\s*([^;]+);").Groups[1].Value;

        Assert.Contains("Manrope", ailesi, StringComparison.Ordinal);
        Assert.Contains("Sora", marka, StringComparison.Ordinal);
        // Yazı tipi servis edilemezse ekran boş kalmaz (md.1): sistem yığını durur.
        Assert.Contains("system-ui", ailesi, StringComparison.Ordinal);
        Assert.Contains("system-ui", marka, StringComparison.Ordinal);
    }

    // ── (B) ŞEMA ↔ DURUM RENGİ AYRIMI ───────────────────────────────────────

    /// <summary>Şema bloklarını çıkarır: (şema adı, blok tipi) → belirteç sözlüğü.</summary>
    private static List<(string Sema, string Tip, Dictionary<string, string> Belirtec)> Bloklar()
    {
        var css = Belirtecler();
        var sonuc = new List<(string, string, Dictionary<string, string>)>();
        foreach (Match m in Regex.Matches(css, @"(?s)(\[data-sema=""[a-z]+""\][^{]*)\{(.*?)\n\s*\}"))
        {
            var sel = m.Groups[1].Value;
            var govde = m.Groups[2].Value;
            var tip = sel.Contains("data-tema=\"karanlik\"", StringComparison.Ordinal) ? "karanlik"
                    : govde.Contains("color-scheme: light", StringComparison.Ordinal) ? "aydinlik" : "media";
            var belirtec = Regex.Matches(govde, @"--([a-z0-9-]+):\s*(#[0-9a-fA-F]{6});")
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);
            if (!belirtec.ContainsKey("vurgu")) continue;
            // ⚠ Aynı blok seçicide şema adı BİRDEN ÇOK geçebilir
            //   (":root[...][data-sema=\"platin\"], .tema-onizleme[...][data-sema=\"platin\"]").
            //   Blok tektir; şema adı TEKİLLEŞTİRİLİR, yoksa ölçüm iki kez sayardı.
            foreach (var ad in Regex.Matches(sel, @"data-sema=""([a-z]+)""")
                                    .Select(x => x.Groups[1].Value).Distinct())
                sonuc.Add((ad, tip, belirtec));
        }
        return sonuc;
    }

    /// <summary>Karar D-S1b-1 (b): kuraldan MUAF olan TEK çift.</summary>
    private static readonly (string Sema, string Durum)[] Muaflar = [("bordo", "tehlike")];

    [Fact]
    public void Semalar_ve_bloklar_EKSIKSIZ_okunuyor()
    {
        var bloklar = Bloklar();
        var semalar = bloklar.Select(b => b.Sema).Distinct().ToList();

        // 8 şema + "kurumsal" takma adı; her biri 3 blokta (aydınlık/media/karanlık).
        foreach (var s in new[] { "platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi" })
            Assert.Contains(s, semalar);
        Assert.Equal(3, bloklar.Count(b => b.Sema == "platin"));
    }

    [Fact]
    public void Vurgu_ile_durum_rengi_AYIRT_EDILEBILIR_tek_muafiyet_bordo_tehlikedir()
    {
        var kalan = new List<string>();
        foreach (var (sema, tip, b) in Bloklar())
        {
            var vurgu = Renk.Oklch(b["vurgu"]);
            foreach (var durum in new[] { "basari", "uyari", "tehlike", "bilgi" })
            {
                if (!b.TryGetValue(durum, out var deger)) continue;
                var d = Renk.Oklch(deger);
                var hueFarki = Renk.HueFarki(vurgu.H, d.H);
                var chromaOrani = Math.Max(vurgu.C, d.C) / Math.Max(Math.Min(vurgu.C, d.C), 1e-6);
                var gecti = hueFarki >= 30 || chromaOrani >= 2.5;
                if (!gecti) kalan.Add($"{sema}/{tip} vurgu↔{durum}: hue {hueFarki:F1}°, chroma {chromaOrani:F2}×");
            }
        }

        // Muaf çiftin 3 bloğu (aydınlık + media + karanlık) DIŞINDA kalan olmamalı.
        var muafDisi = kalan.Where(k => !Muaflar.Any(m =>
            k.StartsWith(m.Sema + "/", StringComparison.Ordinal)
            && k.Contains("↔" + m.Durum, StringComparison.Ordinal))).ToList();

        Assert.True(muafDisi.Count == 0,
            "kuraldan geçmeyen YENİ çift: " + string.Join(" · ", muafDisi));
        // Muafiyet GERÇEKTEN kullanılıyor: sessizce ölmüş bir istisna bırakılmaz.
        Assert.Equal(3, kalan.Count);
    }

    [Fact]
    public void Zeytin_basarisi_TUVALDEN_gelen_Zumrut_ailesiyle_AYNIDIR()
    {
        // Karar D-S1b-1 (a): yeni renk UYDURULMADI; kurucunun onayladığı tuvalin
        // Zümrüt'te yaptığı çözüm birebir kopyalandı.
        var bloklar = Bloklar();
        foreach (var tip in new[] { "aydinlik", "media", "karanlik" })
        {
            var zeytin = bloklar.First(b => b.Sema == "zeytin" && b.Tip == tip).Belirtec;
            var zumrut = bloklar.First(b => b.Sema == "zumrut" && b.Tip == tip).Belirtec;
            foreach (var ad in new[] { "basari", "basari-vurgu", "basari-yuzey", "basari-kenar" })
                Assert.Equal(zumrut[ad], zeytin[ad]);
        }
    }
}

/// <summary>Testlerin ortak OKLCH/kontrast hesabı (sRGB → OKLab → OKLCH).</summary>
internal static class Renk
{
    private static double Dogrusal(int b)
    {
        var c = b / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    public static (double L, double C, double H) Oklch(string onalti)
    {
        var h = onalti.TrimStart('#');
        var r = Dogrusal(Convert.ToInt32(h[..2], 16));
        var g = Dogrusal(Convert.ToInt32(h.Substring(2, 2), 16));
        var b = Dogrusal(Convert.ToInt32(h.Substring(4, 2), 16));

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        var ll = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
        var aa = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var bb = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        var hue = (Math.Atan2(bb, aa) * 180 / Math.PI + 360) % 360;
        return (ll, Math.Sqrt(aa * aa + bb * bb), hue);
    }

    public static double HueFarki(double a, double b)
    {
        var d = Math.Abs(a - b) % 360;
        return Math.Min(d, 360 - d);
    }
}
