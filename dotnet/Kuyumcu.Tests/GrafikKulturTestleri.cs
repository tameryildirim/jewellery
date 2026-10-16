using System.Globalization;
using Bunit;
using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GRAFİK KÜLTÜR KAPANI — paket UX-2 Faz A1 (Aşkın K6)
//
// Belgeli tuzak: `{1.6:F1}` tr-TR kültüründe "1,6" üretir. SVG'de
// `points="12,3 25,4"` ve `viewBox="0 0 600,0 220"` AYRIŞTIRILAMAZ; grafik
// tarayıcıda HİÇ ÇİZİLMEZ — ne hata, ne uyarı, sadece boş bir kutu.
// Barkodda tam olarak bu oldu (BarkodTestleri.cs:~360) ve testler invariant
// kültürde koştuğu için YEŞİLDİ.
//
// Aşkın'ın çapraz bulgusu: BarkodKulturTestleri ZATEN tr-TR'de koşuyordu ama
// GRAFİK bileşenlerinin tr-TR testi YOKTU. M-UX2-07 mutasyonu bu yüzden
// yanlış dosyaya yöneltilmişti. Bu dosya o açığı kapatır — UX-2 Faz B
// grafikleri belirteçlere bağlarken bu kapı zaten kurulu olacak.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GrafikKulturTestleri : BunitContext
{
    /// <summary>Gövdeyi tr-TR kültüründe koşturur, sonra eski kültürü geri verir.</summary>
    private static T TurkceKulturde<T>(Func<T> govde)
    {
        var eskiKultur = CultureInfo.CurrentCulture;
        var eskiUi = CultureInfo.CurrentUICulture;
        try
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentCulture = tr;
            CultureInfo.CurrentUICulture = tr;
            return govde();
        }
        finally
        {
            CultureInfo.CurrentCulture = eskiKultur;
            CultureInfo.CurrentUICulture = eskiUi;
        }
    }

    private static readonly GrafikNokta[] Noktalar =
    [
        new("01.09", 1234.5m), new("02.09", 987.25m), new("03.09", 4321.75m),
        new("04.09", 55.5m),   new("05.09", 7777.125m),
    ];

    private static readonly CiftNokta[] CiftNoktalar =
    [
        new("Oca", 1234.5m, 987.25m), new("Şub", 4321.75m, 55.5m), new("Mar", 77.125m, 3333.3m),
    ];

    /// <summary>SVG'deki sayısal öznitelikleri tek tek ayrıştırılabilir mi diye bakar.</summary>
    private static void SayilarNoktaliOlmali(string svg)
    {
        // points="x,y x,y" → BOŞLUK nokta ayıracı, VİRGÜL x/y ayıracıdır.
        // ⚠ İkisini birden ayıraç saymak yanlış olur: "0,0 168,5" (bozuk) da
        //   "0,0,168,5" gibi ayrıştırılıp SAYI görünür ve mutasyon kaçar.
        //   Bu boşluk M-UX2A1-05'in ilk koşusunda gerçekten yakalandı.
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(svg, @"(points|viewBox)=""([^""]*)"""))
        {
            var ad = m.Groups[1].Value;
            var deger = m.Groups[2].Value;
            foreach (var oge in deger.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var xy = oge.Split(',');
                Assert.True(xy.Length <= 2,
                    $"{ad}=\"{deger}\" içindeki '{oge}' ikiden fazla virgül parçası taşıyor — tr-TR ondalık virgülü kaçmış.");
                Assert.All(xy, p => Assert.True(
                    double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                    $"{ad}=\"{deger}\" içindeki '{p}' invariant kültürde sayı değil — tr-TR virgülü kaçmış."));
            }
        }

        // x/y/width/height gibi tekil sayısal öznitelikler
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(svg, @"\b(x|y|width|height|rx)=""([^""]*)"""))
        {
            var deger = m.Groups[2].Value;
            Assert.DoesNotContain(",", deger);
        }
    }

    [Fact]
    public void TURKCE_kulturde_AlanGrafik_SVG_sinde_ONDALIK_VIRGUL_YOKTUR()
    {
        var svg = TurkceKulturde(() =>
            Render<AlanGrafik>(p => p
                .Add(x => x.Noktalar, Noktalar)
                .Add(x => x.Aciklama, "7 günlük ciro")).Markup);

        Assert.Contains("<polyline", svg);
        SayilarNoktaliOlmali(svg);
    }

    [Fact]
    public void TURKCE_kulturde_CubukGrafik_SVG_sinde_ONDALIK_VIRGUL_YOKTUR()
    {
        var svg = TurkceKulturde(() =>
            Render<CubukGrafik>(p => p
                .Add(x => x.Noktalar, CiftNoktalar)
                .Add(x => x.Aciklama, "Aylık alış/satış")).Markup);

        Assert.Contains("<rect", svg);
        SayilarNoktaliOlmali(svg);
    }

    [Fact]
    public void TURKCE_kulturde_AlanGrafik_polyline_noktalari_AYRISTIRILABILIR()
    {
        var svg = TurkceKulturde(() =>
            Render<AlanGrafik>(p => p.Add(x => x.Noktalar, Noktalar)).Markup);

        var m = System.Text.RegularExpressions.Regex.Match(svg, @"<polyline points=""([^""]+)""");
        Assert.True(m.Success, "polyline points bulunamadı");

        var ciftler = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(Noktalar.Length, ciftler.Length);   // virgül kaçarsa sayı ikiye katlanır
        Assert.All(ciftler, c =>
        {
            var xy = c.Split(',');
            Assert.Equal(2, xy.Length);
            Assert.All(xy, s => Assert.True(
                double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _), $"'{s}' sayı değil"));
        });
    }

    [Fact]
    public void Eksen_etiketi_KULLANICI_GIRDISI_ise_HTML_KACISI_yapilir()
    {
        // Mehmet K4: grafikler MarkupString basıyor; eksen etiketi ürün/cari
        // adı olabilir. Encode edilmezse depolanmış XSS olur.
        var zararli = new GrafikNokta[] { new("<img onerror=alert(1)>", 10m), new("ikinci", 20m) };

        var svg = Render<AlanGrafik>(p => p.Add(x => x.Noktalar, zararli)).Markup;

        Assert.Contains("&lt;img", svg);
        Assert.DoesNotContain("<img onerror", svg);
    }

    // ── PAKET 34 / QA turu 3 (2026-09-17) ────────────────────────────────────
    // ÖLÇÜLEN HATA: /raporlar "Stok Dağılımı" pastası HİÇ ÇİZİLMİYORDU.
    // Tarayıcı konsolu: `<path> attribute d: Expected number, "… 1 1 110,0 20,0 Z"`.
    // PastaGrafik'in dilim yolu `$"M {cx:F1} ..."` ile üretiliyordu; tr-TR'de
    // ondalık ayırıcı VİRGÜL olunca SVG ayrıştırılamıyordu. Bu dosyada pasta
    // için kültür testi YOKTU — açık tam buradaydı.

    [Fact]
    public void PastaGrafik_dilim_yolu_tr_TR_kulturunde_de_SAYI_uretir()
    {
        var svg = TurkceKulturde(() =>
            Render<PastaGrafik>(p => p.Add(x => x.Noktalar, Noktalar)).Markup);

        var eslesmeler = System.Text.RegularExpressions.Regex.Matches(svg, @"<path d=""([^""]+)""");
        Assert.NotEmpty(eslesmeler);

        foreach (System.Text.RegularExpressions.Match m in eslesmeler)
        {
            var d = m.Groups[1].Value;
            // "110,0 20,0" gibi bir koordinat kaçarsa SVG ayrıştırılamaz.
            Assert.DoesNotContain(",", d, StringComparison.Ordinal);
            foreach (var parca in d.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (parca is "M" or "L" or "A" or "Z") continue;
                Assert.True(
                    double.TryParse(parca, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                    $"'{parca}' sayı değil (path: {d})");
            }
        }
    }
}
