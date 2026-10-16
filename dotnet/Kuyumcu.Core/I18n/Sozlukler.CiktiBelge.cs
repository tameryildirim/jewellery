namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-4 — ÇIKTI BELGESİ (fiş / makbuz / ekstre) ek metinleri · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: "yeni
//   metin için AYRI dosya aç"). FisYazdirSozlugu deseni.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı
//   kurucudan geçer.
// ⚠ Jargon yok (PLAN § 5B): "ULID", "kimlik" gibi iç terimler metinde geçmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Çıktı sayfasının Q9-4 ile eklenen metinleri.</summary>
public sealed record CiktiBelgeMetinleri(
    string OnizlemeBasligi,
    string BelgeNoKayitNumarasi);

/// <summary>Çıktı sayfası ek metinleri — TR + EN.</summary>
public static class CiktiBelgeSozlugu
{
    public static readonly CiktiBelgeMetinleri Tr = new(
        OnizlemeBasligi: "Yazdırma önizlemesi",
        BelgeNoKayitNumarasi: "Bu numara sistemdeki kayıt numarasıdır; ayrı bir fiş sıra numarası tutulmuyor.");

    public static readonly CiktiBelgeMetinleri En = new(
        OnizlemeBasligi: "Print preview",
        BelgeNoKayitNumarasi: "This is the record number in the system; no separate receipt sequence number is kept.");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static CiktiBelgeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
