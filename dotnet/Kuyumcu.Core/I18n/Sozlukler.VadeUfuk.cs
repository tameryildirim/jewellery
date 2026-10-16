namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-75 — VADE TAKİBİ: geçersiz ufukta kartların kaybolmaması (TR + EN).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin AYRI dosyaya — Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs).
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ Sınırlar (0–365) METNE GÖMÜLMEZ: ekran EnAz/EnCok değerini {0}/{1}
//   yer tutucularına yazar — sınır tek yerde (VadeTakibi.razor sabitleri).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Vade takibi ufuk girdisi ve korunan rapor uyarısı metinleri (bir dilde).</summary>
public sealed record VadeUfukMetinleri(
    string UfukBos,
    string UfukAralikDisi,
    string UfukTamSayiDegil,
    string YuklemeBasarisiz,
    string SonGecerliRapor);

public static class VadeUfukSozlugu
{
    public static readonly VadeUfukMetinleri Tr = new(
        UfukBos: "Gün sayısı boş bırakıldı; rapor yenilenmedi. {0} ile {1} arasında bir gün sayısı yazın.",
        UfukAralikDisi: "Gün sayısı {0} ile {1} arasında olmalı; rapor yenilenmedi.",
        UfukTamSayiDegil: "Gün sayısı tam sayı olmalı (ör. 7, 30); rapor yenilenmedi.",
        YuklemeBasarisiz: "Rapor yenilenemedi: {0}",
        SonGecerliRapor: "Ekrandaki rakamlar son geçerli rapora aittir ({0} günlük ufuk).");

    public static readonly VadeUfukMetinleri En = new(
        UfukBos: "The number of days is empty; the report was not refreshed. Enter a number of days between {0} and {1}.",
        UfukAralikDisi: "The number of days must be between {0} and {1}; the report was not refreshed.",
        UfukTamSayiDegil: "The number of days must be a whole number (e.g. 7, 30); the report was not refreshed.",
        YuklemeBasarisiz: "The report could not be refreshed: {0}",
        SonGecerliRapor: "The figures on screen belong to the last valid report ({0}-day horizon).");

    public static VadeUfukMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
