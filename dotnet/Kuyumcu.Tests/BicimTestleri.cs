using System.Globalization;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-57 (QA turu 8 triyajı, YÜKSEK) — Bicim tr-TR BİRLEŞTİRMESİ [saf]
//
// ÖLÇÜLEN HATA: Bicim.Has() ve Bicim.Gram() CultureInfo.InvariantCulture
// kullanıyordu, Bicim.Tl() tr-TR. Aynı ekranda "₺1.234,56" ile "12.5000 has"
// yan yana duruyordu — ondalık ayırıcı bir yerde virgül, bir yerde nokta.
//
// Bu dosya dört kuralı sınar:
//   Has/Gram  — insan biçimi VİRGÜL ondalık (tr-TR), basamak sayısı korunur.
//   Makine    — API/SVG/input biçimi NOKTA ondalık kalır (sözleşme bozulmaz).
//   Yuzde     — "%12,5" (Türkçe: işaret önde, virgül ondalık).
//
// ⚠ Testler iş parçacığı kültüründen BAĞIMSIZ olmalı: kültür bilinçli olarak
//   en-US'e (nokta ondalık) çekilir; Bicim kendi kültürünü taşımıyorsa kırmızı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BicimTestleri
{
    private static T KulturIle<T>(string kultur, Func<T> f)
    {
        var onceki = CultureInfo.CurrentCulture;
        var oncekiUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(kultur);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(kultur);
            return f();
        }
        finally
        {
            CultureInfo.CurrentCulture = onceki;
            CultureInfo.CurrentUICulture = oncekiUi;
        }
    }

    [Fact]
    public void Has_TurkceVirgulOndalikBasar()
    {
        // Kabul koşulu birebir: Bicim.Has(77.6014m) → "77,6014 has".
        Assert.Equal("77,6014 has", KulturIle("en-US", () => Bicim.Has(77.6014m)));
        Assert.Equal("77,6014 has", KulturIle("tr-TR", () => Bicim.Has(77.6014m)));
        // Negatif ve basamak tamamlama (F4) korunur.
        Assert.Equal("-16,9460 has", KulturIle("en-US", () => Bicim.Has(-16.946m)));
        Assert.Equal("12,5000 has", KulturIle("en-US", () => Bicim.Has(12.5m)));
        // MADEN-AYRIMI tur 2 (tasarım hakemi KRİTİK + ekip kararı 5): binlik gruplu, 4 ondalık —
        // kültürden bağımsız tr-TR; ondalık hep virgülden sonra 4 hane, nokta yalnız binlik.
        Assert.Equal("1.234,5670 has", KulturIle("en-US", () => Bicim.Has(1234.567m)));
        Assert.Equal("1.234,5670 has", KulturIle("tr-TR", () => Bicim.Has(1234.567m)));
        Assert.Equal("999,9999 has", Bicim.Has(999.9999m));
    }

    [Fact]
    public void Gram_TurkceVirgulOndalikBasar()
    {
        // Kabul koşulu birebir: Bicim.Gram(56.991m) → "56,991 g".
        Assert.Equal("56,991 g", KulturIle("en-US", () => Bicim.Gram(56.991m)));
        Assert.Equal("56,991 g", KulturIle("tr-TR", () => Bicim.Gram(56.991m)));
        Assert.Equal("4,160 g", KulturIle("en-US", () => Bicim.Gram(4.16m)));
        Assert.Equal("-5,496 g", KulturIle("en-US", () => Bicim.Gram(-5.496m)));
        Assert.Equal("10250,000 g", KulturIle("en-US", () => Bicim.Gram(10250m)));
        // Miktar(GRAM) Gram'a devreder; ADET ve birimsiz de virgül ondalık.
        Assert.Equal("1,000 g", KulturIle("en-US", () => Bicim.Miktar(1m, "GRAM")));
        Assert.Equal("4,25 adet", KulturIle("en-US", () => Bicim.Miktar(4.25m, "ADET")));
        Assert.Equal("2,028", KulturIle("en-US", () => Bicim.Miktar(2.0278m, null)));
        Assert.Equal("3 adet", KulturIle("en-US", () => Bicim.Miktar(3m, "ADET")));
    }

    [Fact]
    public void Makine_InvariantKalir_ApiSozlesmesiBozulmaz()
    {
        // API gövdesi, sorgu dizesi, SVG koordinatı ve input type=number değeri
        // NOKTA ondalık ister. tr-TR iş parçacığında bile değişmez.
        Assert.Equal("77.6014", KulturIle("tr-TR", () => Bicim.Makine(77.6014m)));
        Assert.Equal("56.991", KulturIle("tr-TR", () => Bicim.Makine(56.991m)));
        Assert.Equal("1234.5", KulturIle("tr-TR", () => Bicim.Makine(1234.5m)));
        Assert.Equal("-16.946", KulturIle("tr-TR", () => Bicim.Makine(-16.946m)));
        Assert.Equal("12.5", KulturIle("tr-TR", () => Bicim.Makine(12.5d)));
        Assert.Equal("1234", KulturIle("tr-TR", () => Bicim.Makine(1234)));
        Assert.Equal("2026-09-24", KulturIle("tr-TR", () => Bicim.Makine(new DateOnly(2026, 9, 24))));
        // Makine çıktısı InvariantCulture ile geri ayrıştırılabilir (sözleşme).
        Assert.Equal(77.6014m, decimal.Parse(KulturIle("tr-TR", () => Bicim.Makine(77.6014m)), CultureInfo.InvariantCulture));
        Assert.DoesNotContain(",", KulturIle("tr-TR", () => Bicim.Makine(77.6014m)), StringComparison.Ordinal);
    }

    [Fact]
    public void Yuzde_TurkceVirgulOndalikBasar()
    {
        Assert.Equal("%12,5", KulturIle("en-US", () => Bicim.Yuzde(12.5m)));
        Assert.Equal("%25", KulturIle("en-US", () => Bicim.Yuzde(25m)));
        Assert.Equal("%33,33", KulturIle("en-US", () => Bicim.Yuzde(33.3333m)));
        Assert.Equal("%-4,2", KulturIle("en-US", () => Bicim.Yuzde(-4.2m)));
        Assert.Equal("%0", KulturIle("tr-TR", () => Bicim.Yuzde(0m)));
    }
}
