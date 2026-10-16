using System.Globalization;

namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 — nakit sınırı biçim yardımcıları. Sözleşme tipleri
// Kuyumcu.Contracts.Sayfalar.NakitSiniri'dedir (istemci record'u burada açılmaz).
//
// ⚠ Liste yanıtı NESNE sarmalıdır ({ ihlaller: [...] }) — bUnit sahte API'lerinin
//   "{}" varsayılanı boş liste olarak okunur, ekran hata bandı basmaz.
// ⚠ Rakam/had SUNUCUDAN gelir; istemciye gömülmez (ADR-0005).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Gün biçimi — kültüre bağlı (tr-TR: 07.10.2026).</summary>
public static class NakitSiniriBicim
{
    public static string Gun(DateOnly g, string dil) =>
        g.ToDateTime(TimeOnly.MinValue).ToString("d", CultureInfo.GetCultureInfo(dil == "en" ? "en-GB" : "tr-TR"));

    public static string KisaGun(DateOnly g) =>
        g.ToString("dd.MM", CultureInfo.InvariantCulture);
}
