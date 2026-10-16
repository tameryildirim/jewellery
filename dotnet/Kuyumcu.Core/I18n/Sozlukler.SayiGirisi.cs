namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-78 — SAYI GİRİŞİ tr-TR AYRIŞTIRMA: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; orkestratör koşulu
//   "yeni metin AYRI dosyaya" (SatirEylemSozlugu ile aynı desen).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ Hedef kitle tezgâhtar: jargon yok, örnekle anlatılır.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Sayı girişi ayrıştırma mesajları (bir dilde).</summary>
/// <param name="Belirsiz">"1.500" gibi hem 1,5 hem 1500 okunabilecek girdi (fail-closed ret).</param>
/// <param name="BelirsizTam">TOPLU-7A: aynı girdi TAM SAYI alanında ("1.500" adet) — ondalık seçeneği yoktur, tek yol "1500" yazmaktır.</param>
/// <param name="Gecersiz">Sayıya hiç çevrilemeyen girdi ("abc", "1,500.25", "1,2,3").</param>
public sealed record SayiGirisiMetinleri(
    string Belirsiz,
    string BelirsizTam,
    string Gecersiz);

public static class SayiGirisiSozlugu
{
    public static readonly SayiGirisiMetinleri Tr = new(
        Belirsiz: "Ondalık için virgül kullanın: 1,5 ya da 1500",
        BelirsizTam: "Tam sayı girin, nokta kullanmayın: 1500",
        Gecersiz: "Sayı anlaşılmadı. Örnek: 1,5 · 45.000 · 1500");

    public static readonly SayiGirisiMetinleri En = new(
        Belirsiz: "Unclear number. Use a comma for decimals: 1,5 — or write 1500 without a dot",
        BelirsizTam: "Whole numbers only. Write 1500 without a dot",
        Gecersiz: "That is not a number. Examples: 1,5 · 45.000 · 1500");

    public static SayiGirisiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
