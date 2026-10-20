namespace Kuyumcu.Core.I18n;

// Y-15 (20.10, rakip taraması) — mali müşavir hesap planı kodları (İşletme › Ayarlar). Jargon sıfır: "hesap planı" yerine
// "mali müşavirin hesap kodları"; 600/391 varsayılanı açıkça yazılır (TR + EN).
public static class MuhasebeSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Accountant's account codes" : "Mali müşavirin hesap kodları";
    public static string Aciklama(string dil) => En(dil)
        ? "Used in the Luca voucher export: sales account and VAT account. Customer codes are on each customer card."
        : "Luca fiş dosyasında kullanılır: satış hesabı ve KDV hesabı. Müşteri kodu her müşterinin kartında.";
    public static string SatisHesabi(string dil) => En(dil) ? "Sales account" : "Satış hesabı";
    public static string KdvHesabi(string dil) => En(dil) ? "VAT account" : "KDV hesabı";
    public static string Varsayilan(string kod, string dil) => En(dil) ? $"default {kod}" : $"boşsa {kod}";
    public static string Kaydet(string dil) => En(dil) ? "Save codes" : "Kodları kaydet";
    public static string Kaydedildi(string dil) => En(dil) ? "Account codes saved." : "Hesap kodları kaydedildi.";
}
