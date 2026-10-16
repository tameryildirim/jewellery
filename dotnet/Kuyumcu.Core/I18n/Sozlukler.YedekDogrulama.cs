namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-77 — YEDEKLER: "ŞİMDİ DOĞRULA" (TR + EN)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin AYRI dosyaya —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs). KasaBildirimSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ JARGON SIFIR: "tatbikat/şema/checksum" yerine "ayrı bir alana geri
//   yükleyip karşılaştırma". Ham kod (DOGRULAMA, YEDEK) ekrana basılmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Yedekler ekranındaki doğrulama talebi metinleri (bir dilde).</summary>
public sealed record YedekDogrulamaMetinleri(
    string SimdiDogrula,
    string Aciklama,
    string TalepAcildi,
    string KilitBekleyen,
    string KilitYedekYok,
    string TurEtiket,
    string TurYedek,
    string TurDogrulama);

public static class YedekDogrulamaSozlugu
{
    public static readonly YedekDogrulamaMetinleri Tr = new(
        SimdiDogrula: "Şimdi doğrula",
        Aciklama: "Son başarılı yedek ayrı bir alana geri yüklenir ve kayıtlar tek tek karşılaştırılır. Yedek dosyası silinmez, değiştirilmez.",
        TalepAcildi: "Doğrulama sıraya alındı; en geç bir dakika içinde başlar",
        KilitBekleyen: "Sırada bekleyen bir doğrulama var ({0} tarihinde istendi). Sonuçlanınca yeniden isteyebilirsiniz; vazgeçmek için aşağıdaki talepler listesinden iptal edin.",
        KilitYedekYok: "Henüz alınmış bir yedek yok; doğrulamadan önce yedek alınmalı.",
        TurEtiket: "Tür",
        TurYedek: "Yedek alma",
        TurDogrulama: "Doğrulama");

    public static readonly YedekDogrulamaMetinleri En = new(
        SimdiDogrula: "Verify now",
        Aciklama: "The latest successful backup is restored to a separate area and compared record by record. The backup file is neither deleted nor changed.",
        TalepAcildi: "Verification queued; it starts within a minute",
        KilitBekleyen: "A verification is already waiting in the queue (requested on {0}). You can request again once it finishes; to withdraw it, cancel it in the request list below.",
        KilitYedekYok: "No backup has been taken yet; take a backup before verifying.",
        TurEtiket: "Type",
        TurYedek: "Backup",
        TurDogrulama: "Verification");

    public static YedekDogrulamaMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Ham tür kodunu ekran adına çevirir (UX-S31).</summary>
    public static string TurAdi(YedekDogrulamaMetinleri m, string? kod) =>
        kod == "DOGRULAMA" ? m.TurDogrulama : m.TurYedek;
}
