namespace Kuyumcu.Core.I18n;

// Y-11 (20.10, rakip taraması § 4.1) — fiyat teklif fişi: bekletilen fiş + kur kilidi → süreli, numaralı teklif dökümü (TR + EN).
public static class TeklifSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string TeklifVer(string dil) => En(dil) ? "Price quote" : "Fiyat teklifi ver";
    public static string OnceKilit(string dil) => En(dil)
        ? "Lock the rate first — the quote stays valid as long as the lock does."
        : "Önce kuru kilitleyin — teklif, kilit süresi boyunca geçerli kalır.";
    public static string Verildi(string no, string dil) => En(dil) ? $"Quote {no} issued; opening the printout." : $"Teklif {no} verildi; döküm açılıyor.";
    public static string Etiket(string dil) => En(dil) ? "Quote" : "Teklif";
    public static string Gecerli(string zaman, string dil) => En(dil) ? $"valid until {zaman}" : $"{zaman} tarihine kadar geçerli";
    public static string SuresiDoldu(string dil) => En(dil) ? "expired" : "süresi doldu";
    public static string KilitYok(string dil) => En(dil)
        ? "The quote's rate lock is gone; the current rate will be used." : "Teklifin kur kilidi artık yok; güncel kur kullanılacak.";
    public static string Yazdir(string dil) => En(dil) ? "Print quote" : "Teklifi yazdır";
}
