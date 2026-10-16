namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-3 — FİŞ ÜST SATIRI: MÜŞTERİ BAKİYE ROZETİ + "GERÇEK MÜŞTERİ SEÇİN"
// BANDI · TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni metin
//   için Sozlukler.<Alan>.cs). TezgahFisiSozlugu / PesinMusteriSozlugu deseni.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ EŞİK SAYISI YOK (PLAN § 3 md.6, D2 K2): hangi tutarda kimlik gerektiğini
//   sunucu söyler (400 mesajı). Sözlük testi rakam dizisini yasaklar.
// ⚠ Jargon sıfır (PLAN § 5B): "bakiye", "cari", "tevsik", "MASAK" ekrana çıkmaz;
//   etiketler en çok 3 kelime.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Fiş üst satırının (müşteri) bir dildeki metinleri.</summary>
public sealed record FisMusteriMetinleri(
    string BorcuYok,
    string Borcu,
    string Alacagi,
    string Has,
    string RozetBaslik,
    string KimlikBandi,
    string GercekMusteriSec,
    string YeniMusteri);

public static class FisMusteriSozlugu
{
    public static readonly FisMusteriMetinleri Tr = new(
        BorcuYok: BorcDiliSozlugu.BorcYok("tr"), // P-DIL-BORC-KASA: tek kalıp
        Borcu: BorcDiliSozlugu.BizeBorcluEtiket("tr"),
        Alacagi: BorcDiliSozlugu.BizBorcluyuzEtiket("tr"),
        Has: "has",
        RozetBaslik: "Müşterinin bugünkü durumu",
        KimlikBandi: "Bu tutarda satış için müşterinin kimliği gerekiyor. "
                   + "Peşin Müşteri ile kaydedilemez: listeden gerçek müşteriyi seçin "
                   + "ya da yeni müşteri kartı açın.",
        GercekMusteriSec: "Müşteri seç",
        YeniMusteri: "Yeni müşteri");

    public static readonly FisMusteriMetinleri En = new(
        BorcuYok: BorcDiliSozlugu.BorcYok("en"),
        Borcu: BorcDiliSozlugu.BizeBorcluEtiket("en"),
        Alacagi: BorcDiliSozlugu.BizBorcluyuzEtiket("en"),
        Has: "fine gold",
        RozetBaslik: "Customer's position today",
        KimlikBandi: "A sale of this amount needs the customer's identity. "
                   + "It cannot be saved to the Walk-in Customer: pick the real customer from the list "
                   + "or open a new customer account.",
        GercekMusteriSec: "Pick customer",
        YeniMusteri: "New customer");

    /// <summary>Seçili dilin metinleri.</summary>
    public static FisMusteriMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
