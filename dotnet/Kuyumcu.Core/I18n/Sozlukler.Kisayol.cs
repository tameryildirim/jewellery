namespace Kuyumcu.Core.I18n;

// Y-02 (20.10, rakip taraması § 4.1) — tezgâh kısayol yardımı: Profesyonel modda fiş kartının başlığında katlanır
// "Kısayollar" satırı (TR + EN). Tuş eşlemesi Islemler.Pro.cs KisayolHedefi ile AYNI; burada yalnız METİN durur.
public static class KisayolSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Shortcuts" : "Kısayollar";
    public static string F2(string dil) => En(dil) ? "product / barcode" : "ürün / barkod";
    public static string F4(string dil) => En(dil) ? "customer" : "müşteri";
    public static string F8(string dil) => En(dil) ? "payment" : "ödeme";
    public static string F9(string dil) => En(dil) ? "save" : "kaydet";
    public static string CtrlK(string dil) => En(dil) ? "search" : "arama";
}
