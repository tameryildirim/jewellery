namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-YASLANDIRMA — cari yaşlandırma "vadesi bilinmiyor" dilimi ve döviz
// bakiye özeti (TR + EN).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin AYRI dosyaya).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı kurucudan.
// ⚠ JARGON YOK: "ACILIS", "BELIRSIZ" gibi ham kodlar ekrana basılmaz; gündelik dil.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Cari yaşlandırma ek metinleri (bir dilde).</summary>
public sealed record RaporYaslandirmaMetinleri(
    string BelirsizBaslik,
    string AcilisRozet,
    string AcilisDahilRozet,
    string AcilisIpucu,
    string AcilisDahilIpucu,
    string TarihYokRozet,
    string TarihYokIpucu,
    string BelirsizIpucu,
    string BelirsizNotu,
    string DovizOzetBaslik,
    string DovizSatirIpucu);

/// <summary>Cari yaşlandırma ek metinleri — TR ve EN.</summary>
public static class RaporYaslandirmaSozlugu
{
    public static readonly RaporYaslandirmaMetinleri Tr = new(
        BelirsizBaslik: "Tarihsiz",
        AcilisRozet: "Açılış bakiyesi",
        AcilisDahilRozet: "Açılış bakiyesi de var",
        AcilisIpucu: "Önceki programdan devralınan bakiye. Ne zamandan beri beklediği bilinmiyor, bu yüzden yeni borç gibi gösterilmez.",
        AcilisDahilIpucu: "Bu bakiyenin bir kısmı önceki programdan devralınan açılış bakiyesidir. Satır, son işlem tarihine göre güne bölünür.",
        TarihYokRozet: "İşlem tarihi yok",
        TarihYokIpucu: "Bu carinin bakiyesinde işlem tarihi kayıtlı değil. Borcun ne zamandan beri beklediği hesaplanamıyor.",
        BelirsizIpucu: "Ne zamandan beri beklediği bilinmeyen bakiyeler: önceki programdan devralınan açılış bakiyeleri ve işlem tarihi olmayan bakiyeler.",
        BelirsizNotu: "\"Tarihsiz\" sütunu, ne zamandan beri beklediği bilinmeyen bakiyeleri gösterir: önceki programdan devralınan açılış bakiyeleri ve işlem tarihi kayıtlı olmayan bakiyeler. Bunlar yeni borç sayılmaz.",
        DovizOzetBaslik: "Döviz bakiyeleri (güne bölünmez, TL toplamına eklenmez):",
        DovizSatirIpucu: "Döviz bakiyesi güne bölünmez ve TL toplamına eklenmez.");

    public static readonly RaporYaslandirmaMetinleri En = new(
        BelirsizBaslik: "Undated",
        AcilisRozet: "Opening balance",
        AcilisDahilRozet: "Includes opening balance",
        AcilisIpucu: "Balance carried over from the previous program. How long it has been outstanding is unknown, so it is not shown as new debt.",
        AcilisDahilIpucu: "Part of this balance is an opening balance carried over from the previous program. The row is aged by its last transaction date.",
        TarihYokRozet: "No transaction date",
        TarihYokIpucu: "No transaction date is recorded for this balance. How long it has been outstanding cannot be calculated.",
        BelirsizIpucu: "Balances of unknown age: opening balances carried over from the previous program and balances without a transaction date.",
        BelirsizNotu: "The \"Undated\" column shows balances of unknown age: opening balances carried over from the previous program and balances with no recorded transaction date. They are not counted as new debt.",
        DovizOzetBaslik: "Foreign currency balances (not aged, not added to the TL total):",
        DovizSatirIpucu: "Foreign currency balances are not aged and are not added to the TL total.");

    public static RaporYaslandirmaMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
