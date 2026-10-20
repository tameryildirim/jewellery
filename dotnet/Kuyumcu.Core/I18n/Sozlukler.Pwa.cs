namespace Kuyumcu.Core.I18n;

// PAKET-44 / PWA-1 (20.10) — yeni sürüm bandı, çevrimdışı göstergesi, Ayarlar › Uygulama kartı (TR + EN).
public static class PwaSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string YeniSurum(string dil) => En(dil) ? "A new version is ready." : "Yeni sürüm hazır.";
    public static string Yenile(string dil) => En(dil) ? "Update now" : "Şimdi yenile";
    public static string Cevrimdisi(string dil) => En(dil)
        ? "You are offline — nothing can be saved; what you see is from before the connection dropped."
        : "Çevrimdışısınız — kayıt yapılamaz; görünen veriler bağlantı kesilmeden öncesine aittir.";
    public static string Uygulama(string dil) => En(dil) ? "App" : "Uygulama";
    public static string AnaEkranaEkle(string dil) => En(dil) ? "Add to home screen" : "Ana ekrana ekle";
    public static string KurulamazSebep(string dil) => En(dil)
        ? "This browser does not offer installation, or the app is already installed."
        : "Bu tarayıcı kurulum sunmuyor ya da uygulama zaten kurulu.";
    public static string IosYonerge(string dil) => En(dil)
        ? "On iPhone/iPad: Safari › Share › Add to Home Screen." : "iPhone/iPad'de: Safari › Paylaş › Ana Ekrana Ekle.";
    public static string Kuruldu(string dil) => En(dil) ? "Added to the home screen." : "Ana ekrana eklendi.";
}
