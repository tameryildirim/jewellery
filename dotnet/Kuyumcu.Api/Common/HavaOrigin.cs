namespace Kuyumcu.Api.Common;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-37b — HAVA KAYNAĞI İÇİN CSP KAPISI
//
// BU DOSYA BİR CANLI BULGUDAN DOĞDU (md.1, 2026-09-21): karşılama ekranının
// hava kutucuğu kodda ve testlerde çalışıyordu ama TARAYICIDA hiçbir zaman
// veri alamıyordu — `Content-Security-Policy: connect-src 'self'` dış isteği
// kesiyordu. Yani "sessizce kaybol" yolu her zaman devreye giriyordu.
//
// ÇÖZÜM FAIL-CLOSED'DUR: varsayılan olarak HİÇBİR dış origin açılmaz. Yalnız
// `Hava:KaynakOrigin` yapılandırmasında AÇIKÇA bir origin verilirse connect-src
// o tek origin ile genişler. Kaynağın seçimi (lisans/sözleşme) kurucu kalemi,
// origin'in açılması güvenlik kalemidir; ikisi de sessizce yapılmaz.
//
// ⚠ BAŞLIK ENJEKSİYONU: yapılandırma değeri başlığa HAM konmaz. Yalnız
//   https şemalı, mutlak bir URL'nin AUTHORITY parçası (şema + ana makine +
//   port) alınır; yol, sorgu, boşluk, ';' ve ',' taşıyan değer REDDEDİLİR.
// ═══════════════════════════════════════════════════════════════════════════
public static class HavaOrigin
{
    /// <summary>
    /// Yapılandırılan hava kaynağını CSP <c>connect-src</c> ekine çevirir.
    /// Geçersiz / boş / https olmayan her değer için <b>boş dizge</b> döner
    /// (fail-closed: şüphede kapı KAPALI kalır).
    /// </summary>
    public static string CspEki(string? yapilandirilan)
    {
        if (string.IsNullOrWhiteSpace(yapilandirilan)) return "";

        var deger = yapilandirilan.Trim();
        if (deger.Any(c => c is ';' or ',' or '\'' or '"' or '\n' or '\r' || char.IsWhiteSpace(c)))
            return "";

        if (!Uri.TryCreate(deger, UriKind.Absolute, out var u)) return "";
        if (u.Scheme != Uri.UriSchemeHttps) return "";   // düz http açılmaz
        if (!string.IsNullOrEmpty(u.Query) || !string.IsNullOrEmpty(u.Fragment)) return "";
        if (u.AbsolutePath.Trim('/').Length > 0) return "";   // yol taşıyan değer origin değildir

        return " " + u.GetLeftPart(UriPartial.Authority);
    }
}
