namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-68 — /kurulum mühür kilidi gerekçesi + döviz kasası süzgeci (TR + EN).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin AYRI dosyaya — Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs).
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ Kabul edilen para birimleri METNE GÖMÜLMEZ: ekran sunucunun
//   acilisParaBirimleri alanını {2} yer tutucusuna yazar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kurulum ekranı mühür kilidi ve açılış hedef süzgeci metinleri (bir dilde).</summary>
public sealed record KurulumKilitMetinleri(
    string Kaydediliyor,
    string EksikAdimlarBaslik,
    string AdimiTamamla,
    string DovizDisaridaBaslik,
    string DovizDisaridaSatir);

public static class KurulumKilitSozlugu
{
    public static readonly KurulumKilitMetinleri Tr = new(
        Kaydediliyor: "Kaydediliyor… İşlem bitince mühür düğmesi yeniden değerlendirilir.",
        EksikAdimlarBaslik: "Mühürden önce tamamlanması gereken adımlar (tıklayınca ilgili ekran açılır):",
        AdimiTamamla: "{0} — tamamlamak için tıklayın",
        DovizDisaridaBaslik: "Aşağıdaki kasalar/hesaplar listede yok, çünkü açılış bu para birimini kabul etmiyor:",
        DovizDisaridaSatir: "{0} ({1}) — açılış yalnız şu para birimlerini kabul ediyor: {2}");

    public static readonly KurulumKilitMetinleri En = new(
        Kaydediliyor: "Saving… The seal button will be re-evaluated when this finishes.",
        EksikAdimlarBaslik: "Steps to complete before sealing (click to open the related screen):",
        AdimiTamamla: "{0} — click to complete",
        DovizDisaridaBaslik: "The following tills/accounts are not listed because opening balances do not accept their currency:",
        DovizDisaridaSatir: "{0} ({1}) — opening balances only accept: {2}");

    public static KurulumKilitMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
