namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-4 — KARMA ÖDEMEDE ALTIN BACAĞI (EKRAN) · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: "yeni
//   metin için AYRI dosya aç"). TezgahFisiSozlugu / AltinBacagiSozlugu deseni.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı
//   kurucudan geçer.
// ⚠ Taslak (tuval Tezgah.dc.html) ALDIM sütununda bacağı "Eski altın" diye
//   adlandırır: bacak aracının etiketi, tezgâh düğmesi ve ipucu AYNI addır —
//   aynı kavram iki ad taşımaz (UX-S31). Etiket ≤ 3 kelime, jargon yok (§ 5B).
// ⚠ EŞİK SAYISI / ORAN YOK: hiçbir metin tutar sınırı ya da kesinti oranı söylemez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Eski altınla ödemenin (ALTIN bacağı) fiş ekranındaki metinleri.</summary>
public sealed record EskiAltinMetinleri(
    string EskiAltin,
    string EskiAltinIpucu,
    string BasitIpucu,
    string KaydetKilit);

/// <summary>Eski altın metinleri — TR + EN.</summary>
public static class EskiAltinSozlugu
{
    public static readonly EskiAltinMetinleri Tr = new(
        EskiAltin: "Eski altın",
        EskiAltinIpucu: "Müşteri eski altın verdiyse basın. Ayar, gram ve fiyatı yazarsınız; tutarı program hesaplar.",
        BasitIpucu: "Eski altınla ödeme için '{0}' düğmesine basın.",
        KaydetKilit: "Eski altın: {0}");

    public static readonly EskiAltinMetinleri En = new(
        EskiAltin: "Old gold",
        EskiAltinIpucu: "Press if the customer gave old gold. You enter purity, grams and price; the amount is calculated.",
        BasitIpucu: "To take old gold as payment, press '{0}'.",
        KaydetKilit: "Old gold: {0}");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static EskiAltinMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
