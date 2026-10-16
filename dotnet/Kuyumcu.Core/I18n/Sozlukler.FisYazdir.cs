namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5 — KAYDET → YAZDIR + PARA ÜSTÜ (tezgâh fişi) · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: "yeni
//   metin için AYRI dosya aç"). EskiAltinSozlugu / TezgahFisiSozlugu deseni.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı
//   kurucudan geçer.
// ⚠ Etiket ≤ 3 kelime, jargon yok (PLAN § 5B): "para üstü", "alınan", "fiş
//   yazdır" tezgâhın kendi sözcükleridir. Ham kimlik (ULID) hiçbir metinde yok.
// ⚠ EŞİK SAYISI / ORAN YOK: para üstü bir istemci hesabıdır (KARARLAR D7:
//   "deftere girmez"); hiçbir metin tutar sınırı söylemez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kaydet sonrası yazdırma bağlantısı ve nakit para üstü metinleri.</summary>
public sealed record FisYazdirMetinleri(
    string FisYazdir,
    string FisYazdirSira,
    string FisYazdirIpucu,
    string YazdirHata,
    string Alinan,
    string AlinanIpucu,
    string ParaUstu,
    string Eksik,
    string AlinanBekleniyor);

/// <summary>Yazdırma + para üstü metinleri — TR + EN.</summary>
public static class FisYazdirSozlugu
{
    public static readonly FisYazdirMetinleri Tr = new(
        FisYazdir: "Fiş yazdır",
        FisYazdirSira: "Fiş yazdır ({0}/{1})",
        FisYazdirIpucu: "Fiş yeni sekmede açılır, yazdırma penceresi kendiliğinden gelir.",
        YazdirHata: "Fiş yazdırmaya hazırlanamadı: {0}",
        Alinan: "Alınan",
        AlinanIpucu: "Müşterinin verdiği nakit. Yalnız para üstü için; kayda girmez.",
        ParaUstu: "Para üstü",
        Eksik: "Eksik",
        AlinanBekleniyor: "Alınanı yazın");

    public static readonly FisYazdirMetinleri En = new(
        FisYazdir: "Print receipt",
        FisYazdirSira: "Print receipt ({0}/{1})",
        FisYazdirIpucu: "The receipt opens in a new tab and the print dialog appears by itself.",
        YazdirHata: "Receipt could not be prepared for printing: {0}",
        Alinan: "Received",
        AlinanIpucu: "Cash handed over by the customer. Used only for change; not recorded.",
        ParaUstu: "Change",
        Eksik: "Short",
        AlinanBekleniyor: "Enter the amount received");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static FisYazdirMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
