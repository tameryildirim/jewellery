namespace Kuyumcu.Core.I18n;

// Y-17 (20.10, rakip Dezy "gider fotoğrafı") — gider kaydına fiş/fatura fotoğrafı (TR + EN).
public static class GiderBelgeSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Baslik(string dil) => En(dil) ? "Receipt photo" : "Fiş fotoğrafı";
    public static string Ekle(string dil) => En(dil) ? "Add receipt photo" : "Fiş fotoğrafı ekle";
    public static string Belge(string dil) => En(dil) ? "Receipt" : "Belge";
    public static string Aciklama(string dil) => En(dil)
        ? "Photograph the receipt or invoice; it stays with this expense record." : "Fişin ya da faturanın fotoğrafını çekin; bu gider kaydının yanında durur.";
}
