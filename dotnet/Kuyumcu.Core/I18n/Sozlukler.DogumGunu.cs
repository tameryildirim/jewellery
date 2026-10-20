namespace Kuyumcu.Core.I18n;

// DG-1 (20.10, rakip taraması) — yaklaşan doğum günleri (Müşteriler ekranı, katlanır kapalı) (TR + EN).
public static class DogumGunuSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(int gun, string dil) => En(dil) ? $"Upcoming birthdays ({gun} days)" : $"Yaklaşan doğum günleri ({gun} gün)";
    public static string Bos(string dil) => En(dil) ? "No birthdays in this period." : "Bu dönemde doğum günü yok.";
    public static string NeZaman(int gunSonra, string dil) => gunSonra switch
    {
        0 => En(dil) ? "today" : "bugün",
        1 => En(dil) ? "tomorrow" : "yarın",
        _ => En(dil) ? $"in {gunSonra} days" : $"{gunSonra} gün sonra",
    };
    public static string Yas(int yas, string dil) => En(dil) ? $"turns {yas}" : $"{yas} yaşına girecek";
    public static string Kutla(string ad, string dil) => En(dil) ? $"Happy birthday, {ad}!" : $"Doğum gününüz kutlu olsun {ad}!";
}
