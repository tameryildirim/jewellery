namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-7B — para ekranları görünüm kalanları (GOC-A kapanış devri).
// YALNIZ GÖRÜNÜM: hiçbir para/veri/yasal davranış değişmez.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-7B metinleri (bir dilde).</summary>
public sealed record Toplu7BMetinleri(
    // ── /faturalar yeni fatura: yalnız zorunlu görünür; hazır seçili üç seçim grubu bu başlığın altında ──
    string FaturaAyrintiBaslik);

/// <summary>TOPLU-7B sözlüğü (TR + EN).</summary>
public static class Toplu7BSozlugu
{
    public static readonly Toplu7BMetinleri Tr = new(
        FaturaAyrintiBaslik: "Fatura ayrıntıları");

    public static readonly Toplu7BMetinleri En = new(
        FaturaAyrintiBaslik: "Invoice details");

    public static Toplu7BMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
