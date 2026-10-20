namespace Kuyumcu.Core.I18n;

// D1-20 "dene-gör" (20.10, rakip Dezy) — prim simülasyonu: "bu oranla bu dönemde ne öderdim?" (TR + EN).
public static class PrimSimSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Try a rate: what would I pay?" : "Deneyin: bu oranla ne öderdim?";
    public static string Oran(string dil) => En(dil) ? "Commission rate to try" : "Denenecek prim oranı";
    public static string Hesapla(string dil) => En(dil) ? "Calculate" : "Hesapla";
    public static string Geri(string dil) => En(dil) ? "Back to saved rates" : "Kayıtlı oranlara dön";
    public static string Not(string oran, string dil) => En(dil)
        ? $"Simulation — commissions above are calculated at {oran} for everyone; nothing was saved."
        : $"Deneme — yukarıdaki primler herkes için {oran} ile hesaplandı; hiçbir şey kaydedilmedi.";
}
