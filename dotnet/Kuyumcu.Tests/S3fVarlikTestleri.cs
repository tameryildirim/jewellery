using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3f — QA turu 5 YÜKSEK bulguları · GRAFİK · CSS · KILAVUZ kapıları
//
// Bu üç bulgu ekran mantığında değil YAYIN VARLIKLARINDA yaşıyordu:
//   • PastaGrafik  — tek dilim %100 iken yozlaşmış yay (getBBox().width == 0)
//   • app.css      — /yetkiler matrisinde ilk sütun yapışkan değildi
//   • kilavuz.html — yanlış başlatma yordamı + 9 ekran hiç anlatılmıyordu
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3fVarlikTestleri : BunitContext
{
    public S3fVarlikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── Kaynak ağacındaki varlıklar (derleme çıktısı değil) ─────────────────

    private static string ProjeKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string Oku(params string[] parcalar) =>
        File.ReadAllText(Path.Combine([ProjeKoku(), .. parcalar]));

    // ═════ /raporlar — tek dilim %100 iken pasta hiç çizilmiyordu ════════════

    [Fact]
    public void Tek_kategori_yuzde_yuz_iken_pasta_TAM_DAIRE_cizer()
    {
        var svg = Render<PastaGrafik>(p => p.Add(x => x.Noktalar,
            new List<GrafikNokta> { new("K22", 12m) })).Markup;

        var d = Regex.Match(svg, @"<path d=""([^""]+)""").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(d));

        // YOZLAŞMA İMZASI: başlangıç ve bitiş noktası AYNI ("M 110.0 20.0 A … 110.0 20.0 Z"
        // tek yayla). Tam daire İKİ yaydan kurulur → path'te iki 'A' komutu olmalı.
        Assert.Equal(2, d.Count(ch => ch == 'A'));
        // Ve tek yaylı yozlaşmış biçim ARTIK ÜRETİLMEZ: 'L' ile merkeze dönüş yok.
        Assert.DoesNotContain("L", d, StringComparison.Ordinal);
    }

    [Fact]
    public void Coklu_dilimde_ESKI_yay_bicimi_KORUNUR()
    {
        var svg = Render<PastaGrafik>(p => p.Add(x => x.Noktalar,
            new List<GrafikNokta> { new("K22", 6m), new("K14", 6m) })).Markup;

        var yollar = Regex.Matches(svg, @"<path d=""([^""]+)""");
        Assert.Equal(2, yollar.Count);
        foreach (Match m in yollar)
        {
            // Normal dilim: merkeze dönüşlü TEK yay.
            Assert.Contains("L", m.Groups[1].Value, StringComparison.Ordinal);
            Assert.Equal(1, m.Groups[1].Value.Count(ch => ch == 'A'));
        }
    }

    // ═════ /yetkiler — ROL sütunu donuk değildi ══════════════════════════════

    [Fact]
    public void app_css_yapiskan_ilk_sutun_kuralini_ICERIR_ve_SABIT_RENK_kullanmaz()
    {
        var css = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var bas = css.IndexOf(".tablo-yapiskan-ilk", StringComparison.Ordinal);
        Assert.True(bas >= 0, "app.css'te .tablo-yapiskan-ilk kuralı yok");

        var blok = css[bas..];
        Assert.Contains("position: sticky", blok, StringComparison.Ordinal);
        Assert.Contains("left: 0", blok, StringComparison.Ordinal);
        // Yeni CSS'te sabit renk YASAK: değer belirteçten gelir.
        var kuralGovdesi = blok[..blok.IndexOf("}", StringComparison.Ordinal)];
        Assert.DoesNotContain("#", kuralGovdesi, StringComparison.Ordinal);
        Assert.Contains("var(--", kuralGovdesi, StringComparison.Ordinal);
    }

    // ═════ /kilavuz — yanlış başlatma yordamı + eksik ekranlar ═══════════════

    [Fact]
    public void Kilavuz_gelistirici_yordamini_ve_SABIT_ADRESI_anlatmaz()
    {
        var k = Oku("Kuyumcu.Web", "wwwroot", "kilavuz.html");

        Assert.DoesNotContain("localhost:5000", k, StringComparison.Ordinal);
        Assert.DoesNotContain("start.bat", k, StringComparison.Ordinal);
        Assert.DoesNotContain("stop.bat", k, StringComparison.Ordinal);
        // Son kullanıcı dili: kısayol anlatılır.
        Assert.Contains("kısayol", k, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Kilavuz_menudeki_HER_ekran_icin_bir_bolum_tasir()
    {
        var k = Oku("Kuyumcu.Web", "wwwroot", "kilavuz.html");

        // QA turu 5 (YÜKSEK): bu dokuz ekran kılavuzda HİÇ anlatılmıyordu.
        string[] bolumler =
        [
            "vezne", "emanet", "siparisler", "sayim", "pos",
            "uyum", "giderpusulasi", "yedekler", "veriaktarim",
        ];
        foreach (var b in bolumler)
        {
            Assert.Contains($"<section id=\"{b}\">", k, StringComparison.Ordinal);
            Assert.Contains($"href=\"#{b}\"", k, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Kilavuz_eski_bolumleri_KAYBOLMADI()
    {
        var k = Oku("Kuyumcu.Web", "wwwroot", "kilavuz.html");
        foreach (var b in new[] { "tanitim", "baslat", "giris", "cariler", "urunler", "raporlar" })
            Assert.Contains($"<section id=\"{b}\">", k, StringComparison.Ordinal);
    }
}
