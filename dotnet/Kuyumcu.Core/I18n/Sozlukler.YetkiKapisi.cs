namespace Kuyumcu.Core.I18n;

// Görsel denetim Y-7 (21.10) — modül yetkisi kapalı ekranın tek tip karşılaması (ham modül kodu YOK). TR + EN.
public static class YetkiKapisiSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string ModulBaslik(string dil) => En(dil) ? "This screen is not open to your account" : "Bu ekran hesabınıza açık değil";
}
