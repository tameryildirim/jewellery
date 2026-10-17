namespace Kuyumcu.Core.I18n;

// 18.10 — toplu fatura (Faturalog eşitliği: "tek tuşla"; kuyumcu için gün sonu) — TR + EN.
public static class FaturaTopluSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ac(string dil) => En(dil) ? "Bulk invoice" : "Toplu fatura";
    public static string Baslik(string dil) => En(dil) ? "Invoice today's receipts" : "Bugünün fişlerine fatura kes";
    public static string BaslikGun(int gun, string dil) => gun <= 1 ? Baslik(dil) : En(dil) ? $"Receipts of the last {gun} days" : $"Son {gun} günün faturasız fişleri";
    public static string Bos(string dil) => En(dil) ? "No receipt without an invoice." : "Faturası kesilmemiş fiş yok.";
    public static string SonYediGun(string dil) => En(dil) ? "Show the last 7 days" : "Son 7 günü göster";
    public static string Kes(int adet, string dil) => En(dil) ? $"Invoice {adet} receipt(s)" : $"{adet} fişe fatura kes";
    public static string Sonuc(int kesilen, int hatali, string dil) => En(dil)
        ? $"{kesilen} invoice(s) issued" + (hatali > 0 ? $", {hatali} could not be issued (reason on the line)." : ".")
        : $"{kesilen} fatura kesildi" + (hatali > 0 ? $", {hatali} kesilemedi (sebebi satırında)." : ".");
    public static string Kalem(int adet, string dil) => En(dil) ? $"{adet} item(s)" : $"{adet} kalem";
}
