namespace Kuyumcu.Web.Bilesenler;

// ═════════════════════════════════════════════════════════════════════════════
// SEÇENEK ÖĞESİ — ortak Secici bileşeninin veri sözleşmesi (paket P-36).
// Kaynak: FRONTEND-KIMLIK-VE-MENU-2026-09-21.md § 6.4.
//
// UX-S31 / B-25: `Deger` ham koddur (ULID, "ACIK", "A22") ve EKRANA BASILMAZ;
// ekranda yalnız `Ad` görünür. Bu ayrım tipin kendisinde durur ki çağıran
// yanlışlıkla kodu basamasın.
// ═════════════════════════════════════════════════════════════════════════════

/// <param name="Deger">Gövdeye giden kod/ULID — ekrana basılmaz.</param>
/// <param name="Ad">Görünen ad (sözlükten çevrilmiş).</param>
/// <param name="AltBilgi">İkinci satır: telefon, kod, bakiye ya da pasiflik sebebi.</param>
/// <param name="RozetMetni">Tür rozeti.</param>
/// <param name="Pasif">Görünür ama seçilemez.</param>
public sealed record SecenekOgesi(
    string Deger,
    string Ad,
    string? AltBilgi = null,
    string? RozetMetni = null,
    bool Pasif = false);
