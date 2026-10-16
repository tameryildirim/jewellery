namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-76 — YEDEKLER: RPO AŞIMI (TR + EN)
//
// QA tur 8: RPO taahhüdü 22 kat aşılmıştı (7985 dk / 360 dk) ve kart bunu
// NÖTR renkte, ham dakikayla gösteriyordu. Bu metinler kartın altındaki
// uyarıyı ve insan okunur süre birimlerini taşır.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr/En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Yedekler ekranındaki RPO aşımı metinleri (bir dilde).</summary>
public sealed record YedekRpoMetinleri(
    string BirimGun,
    string BirimSaat,
    string BirimDakika,
    // {0} = son başarılı yedekten bu yana geçen süre, {1} = taahhüt, {2} = kaç kat.
    string AsimUyari,
    string OlculmediUyari,
    // İpucu (title) başlığı: ham dakikalar.
    string HamIpucu,
    string Olculen);

public static class YedekRpoSozlugu
{
    public static readonly YedekRpoMetinleri Tr = new(
        BirimGun: "gün",
        BirimSaat: "saat",
        BirimDakika: "dk",
        AsimUyari: "Veri kaybı riski: son başarılı yedek {0} önce alındı. Söz verilen en uzun aralık {1}; bu süre {2} kat aşıldı. Bir arıza olursa bu aralıkta girilen kayıtlar kaybolabilir.",
        OlculmediUyari: "Hiç başarılı yedek alınmadı; bir arızada ne kadar veri kaybedileceği bilinmiyor.",
        HamIpucu: "Dakika olarak",
        Olculen: "Son başarılı yedekten bu yana");

    public static readonly YedekRpoMetinleri En = new(
        BirimGun: "d",
        BirimSaat: "h",
        BirimDakika: "min",
        AsimUyari: "Data loss risk: the last successful backup was taken {0} ago. The promised maximum gap is {1}; it has been exceeded {2} times over. If a failure occurs, records entered in this gap could be lost.",
        OlculmediUyari: "No successful backup has ever been taken; how much data a failure would lose is unknown.",
        HamIpucu: "In minutes",
        Olculen: "Since the last successful backup");

    public static YedekRpoMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// Kart rengi — ÜÇ BANT (P-76 kabul koşulu): taahhüt içinde "yesil",
    /// taahhüdün 1–2 katı "sari", 2 katın üzerinde "kirmizi". Ölçüm yoksa
    /// (hiç başarılı yedek yok) YEŞİL DEĞİL, "sari" (uyarı) — bilinmeyen iyi
    /// sayılmaz (Anayasa md.1).
    /// </summary>
    public static string Renk(int? olculen, int taahhut) => olculen switch
    {
        null => "sari",
        { } d when d <= taahhut => "yesil",
        { } d when d <= 2L * taahhut => "sari",
        _ => "kirmizi",
    };

    /// <summary>Taahhüt aşıldı mı (ölçüm yoksa aşım denmez; ayrı uyarı basılır).</summary>
    public static bool Asildi(int? olculen, int taahhut) => olculen is { } d && d > taahhut;
}
