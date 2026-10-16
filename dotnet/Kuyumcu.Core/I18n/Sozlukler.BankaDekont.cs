namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q9-1 — BANKA DEKONTU: TARİH GİRİŞİ/DÜZELTMESİ + MÜŞTERİYLE EŞLEŞTİRME
// (QA turu 9 · 2 KRİTİK + 1 YÜKSEK): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve bu dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin Sozlukler.<Alan>.cs").
//   KasaBildirimSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ JARGON SIFIR: "cari" yerine "müşteri", "MASAK eşiği" yerine "yasal kimlik
//   bildirimi"; ham kod (BEKLIYOR, ESLESTI) ekrana basılmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Banka ekranına Q9-1 ile eklenen dekont metinleri (bir dilde).</summary>
public sealed record BankaDekontMetinleri(
    // ── dekont girişi: tarih ──
    string Tarih,
    string TarihIpucu,
    string TarihSecin,
    string TarihIleri,
    // ── satırda tarih düzeltme ──
    string TarihDuzeltBaslik,
    string TarihDuzeltEtiket,
    string TarihDuzeltAria,
    string TarihDuzeltildi,
    string TarihDegismedi,
    string EkstreTarihGerekce,
    string EkstreTarihYolu,
    // ── müşteriyle eşleştirme (Bekliyor → Eşleşti) ──
    string Eslestir,
    string EslestirBaslik,
    string EslestirMusteri,
    string EslestirIpucu,
    string EslestirMusteriSecin,
    string Eslestirildi,
    string EslesmisGerekce,
    string EslesmisYolu,
    string BekliyorAnlami);

public static class BankaDekontSozlugu
{
    public static readonly BankaDekontMetinleri Tr = new(
        Tarih: "Dekont tarihi",
        TarihIpucu: "Paranın bankada hareket ettiği gün. Bugünden ileri olamaz.",
        TarihSecin: "Dekont tarihini seçin.",
        TarihIleri: "Dekont tarihi bugünden ileri olamaz.",
        TarihDuzeltBaslik: "Dekont tarihini düzelt",
        TarihDuzeltEtiket: "Doğru tarih",
        TarihDuzeltAria: "Tarih: {0} — düzelt",
        TarihDuzeltildi: "Dekont tarihi düzeltildi: {0}",
        TarihDegismedi: "Tarih aynı; değişiklik yapılmadı.",
        EkstreTarihGerekce: "Banka ekstresinden gelen satırın tarihi ve karşı tarafı bankanın kendi kaydıdır; buradan değiştirilmez.",
        EkstreTarihYolu: "Bu satırda yalnız açıklama notunu düzeltebilirsiniz.",
        Eslestir: "Müşteriyle eşleştir",
        EslestirBaslik: "Dekontu müşteriyle eşleştir",
        EslestirMusteri: "Müşteri",
        EslestirIpucu: "Eşleşen dekontun durumu 'Eşleşti' olur. Müşteri sonradan değiştirilemez.",
        EslestirMusteriSecin: "Eşleştirmek için müşteri seçin.",
        Eslestirildi: "Dekont eşleşti: {0}",
        EslesmisGerekce: "Eşleşmiş dekontun müşterisi değiştirilemez; yasal bildirim toplamı bu müşteriye yazıldı.",
        EslesmisYolu: "Müşteri yanlışsa dekontu Storno edin ve doğru müşteriyi seçerek yeni dekont girin.",
        BekliyorAnlami: "'Bekliyor' = dekont henüz bir müşteriyle eşleşmedi. Satırdaki 'Müşteriyle eşleştir' düğmesiyle eşleştirin.");

    public static readonly BankaDekontMetinleri En = new(
        Tarih: "Entry date",
        TarihIpucu: "The day the money moved at the bank. Cannot be in the future.",
        TarihSecin: "Choose the entry date.",
        TarihIleri: "The entry date cannot be in the future.",
        TarihDuzeltBaslik: "Correct entry date",
        TarihDuzeltEtiket: "Correct date",
        TarihDuzeltAria: "Date: {0} — correct",
        TarihDuzeltildi: "Entry date corrected: {0}",
        TarihDegismedi: "Same date; nothing changed.",
        EkstreTarihGerekce: "For a line imported from the bank statement, the date and counterparty are the bank's own record and cannot be changed here.",
        EkstreTarihYolu: "On this line you can correct only the description note.",
        Eslestir: "Match to customer",
        EslestirBaslik: "Match entry to a customer",
        EslestirMusteri: "Customer",
        EslestirIpucu: "A matched entry becomes 'Matched'. The customer cannot be changed later.",
        EslestirMusteriSecin: "Choose a customer to match.",
        Eslestirildi: "Entry matched: {0}",
        EslesmisGerekce: "The customer of a matched entry cannot be changed; the legal reporting total was recorded for this customer.",
        EslesmisYolu: "If the customer is wrong, reverse the entry and enter a new one with the correct customer.",
        BekliyorAnlami: "'Pending' = the entry is not yet matched to a customer. Use 'Match to customer' on the row.");

    public static BankaDekontMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
