namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K3 — E-POSTA. KAYNAK (STANDART): RFC 5321 §4.5.3.1 (yerel ≤ 64, alan ≤ 255,
// yol ≤ 256 → adres ≤ 254), RFC 5322 §3.4.1 (yerel@alan).
// İŞ KURALI: alan adında en az bir nokta ("a@b" dükkân müşterisi için kabul
// edilmez); boş etiket ("a@b..com", "a@.com") yok; boşluk yok.
// Kayıt biçimi: kırpılmış + küçük harf.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>E-posta kuralının tek kaynağı.</summary>
public static class EpostaDogrulayici
{
    /// <summary>Kırpar, küçük harfe çevirir.</summary>
    public static string Normallestir(string? ham) => (ham ?? "").Trim().ToLowerInvariant();

    /// <summary>E-posta denetimi. Boş → null.</summary>
    public static GirdiHatasi? Denetle(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        var t = ham.Trim();
        if (t.Length > AlanSinirlari.Eposta) return new(GirdiKodu.EpostaUzun, AlanSinirlari.Eposta);
        foreach (var c in t)
            if (char.IsWhiteSpace(c) || char.IsControl(c)) return new(GirdiKodu.EpostaBicim);
        var at = t.IndexOf('@');
        if (at <= 0 || at != t.LastIndexOf('@') || at == t.Length - 1) return new(GirdiKodu.EpostaBicim);
        var yerel = t[..at];
        var alan = t[(at + 1)..];
        if (yerel.Length > AlanSinirlari.EpostaYerel || alan.Length > AlanSinirlari.EpostaAlan)
            return new(GirdiKodu.EpostaUzun, AlanSinirlari.Eposta);
        if (yerel.StartsWith('.') || yerel.EndsWith('.') || yerel.Contains("..", StringComparison.Ordinal))
            return new(GirdiKodu.EpostaBicim);
        if (!alan.Contains('.')) return new(GirdiKodu.EpostaBicim);
        foreach (var etiket in alan.Split('.'))
        {
            if (etiket.Length == 0 || etiket.StartsWith('-') || etiket.EndsWith('-')) return new(GirdiKodu.EpostaBicim);
            foreach (var c in etiket)
                if (!(char.IsLetterOrDigit(c) || c == '-')) return new(GirdiKodu.EpostaBicim);
        }
        return null;
    }

    public static bool Gecerli(string? ham) => !string.IsNullOrWhiteSpace(ham) && Denetle(ham) is null;
}
