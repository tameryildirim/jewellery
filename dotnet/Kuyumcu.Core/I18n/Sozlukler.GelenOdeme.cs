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

    // ── Borçlu satış seçimi (17.10) ──
    public static string BorcluSatislar(string dil) => En(dil) ? "Sales with debt" : "Borcu olan satışlar";
    public static string Kalan(string tutar, string dil) => En(dil) ? $"remaining {tutar}" : $"kalan {tutar}";
    public static string Dusulecek(string dusulecek, string kalanBorc, string dil) => En(dil)
        ? $"To deduct {dusulecek} · Remaining debt {kalanBorc}" : $"Düşülecek {dusulecek} · Kalan borç {kalanBorc}";
    public static string HesabindaKalacak(string tutar, string dil) => En(dil)
        ? $"{tutar} will stay on the customer's account" : $"Müşterinin hesabında {tutar} kalacak";
    public static string HesabindaVar(string tutar, string dil) => En(dil)
        ? $"Customer already has {tutar} on account" : $"Müşterinin hesabında zaten {tutar} var";
    public static string AltinBorcu(string miktar, string dil) => En(dil) ? $"Gold debt {miktar} (not deducted here)" : $"Altın borcu {miktar} (burada düşülmez)";
    public static string FaturaGecti(string dil) => En(dil) ? "No invoice — 7 days passed" : "Faturası yok — 7 gün geçti";
    public static string Fis(string dil) => En(dil) ? "Slip" : "Fiş";
    public static string DusulecekTutar(string dil) => En(dil) ? "Amount to deduct" : "Düşülecek tutar";

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

    // ── M7: banka sağlayıcısı / demo bankası ──
    public static string DemoBanka(string dil) => En(dil) ? "Demo bank" : "Demo banka";
    public static string OtomatikAl(string dil) => En(dil) ? "Fetch transactions from the bank automatically (demo bank)" : "Hareketleri bankadan kendiliğinden al (demo banka)";
    public static string OtomatikAlIpucu(string dil) => En(dil)
        ? "Incoming transfers appear on screen within seconds. The demo bank is for trying it out; a real bank connection uses the same flow."
        : "Gelen havale birkaç saniye içinde ekrana düşer. Demo banka denemek içindir; gerçek banka bağlantısı aynı yolu kullanır.";
    public static string SonCekim(string saat, string dil) => En(dil) ? $"last fetched {saat}" : $"son çekim {saat}";
    public static string HavaleGonder(string dil) => En(dil) ? "Send transfer (demo)" : "Havale gönder (demo)";
    public static string Hesap(string dil) => En(dil) ? "Account" : "Hesap";
    public static string Tutar(string dil) => En(dil) ? "Amount" : "Tutar";
    public static string GonderenAd(string dil) => En(dil) ? "Sender name" : "Gönderenin adı soyadı";
    public static string Gonder(string dil) => En(dil) ? "Send" : "Gönder";
    public static string Gonderildi(int yeni, string dil) => En(dil)
        ? (yeni > 0 ? "Transfer arrived in the account." : "Transfer sent; it will appear on the next fetch.")
        : (yeni > 0 ? "Havale hesaba düştü." : "Havale gönderildi; bir sonraki çekimde düşecek.");
}
