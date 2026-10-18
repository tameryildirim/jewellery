namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// D1 — RAKİP EŞİTLEME DALGA 1 (18.10) ekran metinleri · TR + EN
//
// ⚠ Sozlukler.cs ORTAK HUB'dır → yeni metin ayrı dosyada (CiktiBelgeSozlugu deseni).
// ⚠ Konumsal record: Tr ve En aynı kurucudan geçer, eksik anahtar derleme hatasıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>D1 paketlerinin ekran metinleri.</summary>
public sealed record D1Metinleri(
    string KagitSecimi,
    string KagitA4,
    string Kagit80,
    string FaturaYazdir);

/// <summary>D1 metinleri — TR + EN.</summary>
public static class D1Sozlugu
{
    public static readonly D1Metinleri Tr = new(
        KagitSecimi: "Kâğıt",
        KagitA4: "A4",
        Kagit80: "80 mm fiş",
        FaturaYazdir: "Yazdır");

    public static readonly D1Metinleri En = new(
        KagitSecimi: "Paper",
        KagitA4: "A4",
        Kagit80: "80 mm receipt",
        FaturaYazdir: "Print");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static D1Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
