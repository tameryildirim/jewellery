namespace Kuyumcu.Core.I18n;

// M6b (kurucu: "TC'yi yazın… müşteri listesinden otomatik bulsun, yoksa otomatik kaydetsin") — tezgâh müşteri aramasında
// TC/VKN ile bul ya da aç (TR + EN).
public static class MusteriAcSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Yok(string dil) => En(dil) ? "No customer with this number. Enter the name to open one." : "Bu numarayla kayıtlı müşteri yok. Adını yazıp açın.";
    public static string AdSoyad(string dil) => En(dil) ? "Name and surname" : "Ad soyad";
    public static string AcVeSec(string dil) => En(dil) ? "Open customer and select" : "Müşteriyi aç ve seç";
    public static string Gecersiz(string dil) => En(dil) ? "This ID number is not valid; check the digits." : "Bu TC kimlik numarası geçerli değil; rakamları kontrol edin.";
}
