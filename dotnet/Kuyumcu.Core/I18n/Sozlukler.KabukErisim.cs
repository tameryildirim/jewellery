namespace Kuyumcu.Core.I18n;

// Görsel denetim O-14 (21.10, WCAG 2.4.1) — klavye kullanıcısı üst menüyü atlayıp doğrudan ekranın işine geçer. TR + EN.
public static class KabukErisimSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string IcerigeGec(string dil) => En(dil) ? "Skip to content" : "İçeriğe geç";
}
