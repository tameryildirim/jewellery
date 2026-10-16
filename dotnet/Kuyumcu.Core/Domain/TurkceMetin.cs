using System.Globalization;

namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// TÜRKÇE METİN — büyük/küçük harf kapanı
//
// İki kez ısırdı (2026-09-14):
//   1. `OrdinalIgnoreCase` "SENTETİK" ile "sentetik"i EŞLEŞTİRMEDİ → kuyumcunun
//      Türkçe büyük harfle yazdığı etiket ibaresi reddediliyordu.
//   2. `ToUpperInvariant("sentetik")` → "SENTETIK" (noktasız I) → etikete
//      yasal ibare YANLIŞ basılıyordu.
//
// Yönetmeliğin dili Türkçedir; harf dönüşümü de Türkçe kültürle yapılır.
// ═════════════════════════════════════════════════════════════════════════════

public static class TurkceMetin
{
    public static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Türkçe büyük harf: i → İ, ı → I.</summary>
    public static string Buyut(string? s) => (s ?? "").ToUpper(Kultur);

    /// <summary>Türkçe küçük harf: İ → i, I → ı.</summary>
    public static string Kucult(string? s) => (s ?? "").ToLower(Kultur);

    /// <summary>Türkçe harf kurallarına göre içerir mi?</summary>
    public static bool Icerir(string? metin, string? aranan) =>
        metin is not null && aranan is not null
        && Kucult(metin).Contains(Kucult(aranan), StringComparison.Ordinal);
}
