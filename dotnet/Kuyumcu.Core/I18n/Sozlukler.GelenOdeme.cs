namespace Kuyumcu.Core.I18n;

// M7a (kurucu 17.10) — gelen banka ödemesi: müşteriyi bul / yoksa aç, "borcundan düşülsün mü?" kararı,
// müşterinin IBAN listesi (TR + EN).
public static class GelenOdemeSozlugu
{
    private static bool En(string dil) => dil == "en";

    // ── Karar kutusu ──
    public static string Baslik(string dil) => En(dil) ? "Incoming payments awaiting your decision" : "Karar bekleyen gelen ödemeler";
    public static string Geldi(string ad, string tutar, string dil) => En(dil) ? $"{ad} sent {tutar}" : $"{ad} {tutar} gönderdi";
    public static string BorcuVar(string borc, string dil) => En(dil)
        ? $"Has a debt of {borc}. Deduct this payment from the debt?"
        : $"Borcu var: {borc}. Bu ödeme borcundan düşülsün mü?";
    public static string BorcuYok(string dil) => En(dil)
        ? "No debt. Credit the account, or is it payment for a new purchase?"
        : "Borcu yok. Hesabına alacak mı yazılsın, yoksa yeni alışverişin ödemesi mi?";
    public static string BorcundanDus(string dil) => En(dil) ? "Deduct from debt" : "Borcundan düş";
    public static string AlacakYaz(string dil) => En(dil) ? "Credit the account" : "Hesabına alacak yaz";
    public static string YeniAlisveris(string dil) => En(dil) ? "Payment for a new purchase" : "Yeni alışverişin ödemesi";
    public static string Dusuldu(string ad, string dil) => En(dil) ? $"Deducted from {ad}'s debt." : $"{ad} adlı müşterinin borcundan düşüldü.";
    public static string SatisSayildi(string dil) => En(dil) ? "Recorded as payment for a new purchase; the debt is unchanged." : "Yeni alışverişin ödemesi sayıldı; borca dokunulmadı.";
    public static string GeriAl(string dil) => En(dil) ? "Undo" : "Geri al";
    public static string GeriAlindi(string dil) => En(dil) ? "Decision undone; the payment is back in the list." : "Karar geri alındı; ödeme yeniden listede.";

    // ── Dekont sonucu ──
    public static string Baglandi(string ad, string dil) => En(dil) ? $"Payment linked to customer {ad}." : $"Ödeme {ad} adlı müşteriye bağlandı.";
    public static string YeniMusteri(string ad, string dil) => En(dil)
        ? $"{ad} was saved as a new customer and the payment was linked."
        : $"{ad} yeni müşteri olarak kaydedildi, ödeme ona bağlandı.";
    public static string Benzer(string adlar, string dil) => En(dil)
        ? $"Similar customers exist ({adlar}); link the payment to the right one from the list."
        : $"Bu ada benzeyen müşteri var ({adlar}); ödemeyi listeden doğru müşteriyle eşleştirin.";
    public static string KimlikNo(string dil) => En(dil) ? "Sender's ID / tax number" : "Gönderenin TC / vergi no";
    public static string KarsiIban(string dil) => En(dil) ? "Sender's IBAN" : "Gönderenin IBAN'ı";
    public static string BulmaIpucu(string dil) => En(dil)
        ? "If written, the customer is found automatically; if there is none, a new one is saved."
        : "Yazılırsa müşteri kendiliğinden bulunur; yoksa yeni müşteri kaydedilir.";

    // ── Müşteri IBAN listesi ──
    public static string Ibanlar(string dil) => En(dil) ? "IBANs" : "IBAN'lar";
    public static string IbanEkle(string dil) => En(dil) ? "Add IBAN" : "IBAN ekle";
    public static string IbanYok(string dil) => En(dil) ? "No IBAN yet. Payments from a listed IBAN are matched automatically." : "Henüz IBAN yok. Listedeki IBAN'dan gelen ödeme kendiliğinden bu müşteriye bağlanır.";
    public static string Kaldir(string dil) => En(dil) ? "Remove" : "Kaldır";
    public static string Ekle(string dil) => En(dil) ? "Add" : "Ekle";
    public static string Ogrenildi(string dil) => En(dil) ? "from bank" : "bankadan";
}
