namespace Kuyumcu.Core.I18n;

// Y-05 (20.10, rakip Dezy) — personel avansı: gider kaydı personele bağlanır, prim raporunda avans düşülür (TR + EN).
public static class PersonelAvansSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Personel(string dil) => En(dil) ? "Staff advance (optional)" : "Personel avansı (isteğe bağlı)";
    public static string Ipucu(string dil) => En(dil)
        ? "Pick a person if this payment is a salary/commission advance; it is deducted on the commission report." : "Bu ödeme maaş/prim avansıysa kişiyi seçin; prim raporunda düşülür.";
    public static string Avans(string dil) => En(dil) ? "Advance" : "Avans";
    public static string NetOdeme(string dil) => En(dil) ? "Net to pay" : "Net ödenecek";
    public static string AvansEtiketi(string ad, string dil) => En(dil) ? $"advance · {ad}" : $"avans · {ad}";
}
