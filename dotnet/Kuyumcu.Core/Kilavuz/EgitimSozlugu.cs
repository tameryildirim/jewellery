namespace Kuyumcu.Core.Kilavuz;

// Y-01 (21.10, rakip Hasfiyat/Pusula/KuyumSmart) — Kılavuz ekranındaki eğitim videoları listesi (TR + EN).
public static class EgitimSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Baslik(string dil) => En(dil) ? "Training videos" : "Eğitim videoları";
    public static string Aciklama(string dil, int adet) => En(dil)
        ? $"{adet} short videos, one or two minutes each, one per screen. Chapters are listed under each video."
        : $"Ekran başına bir–iki dakikalık {adet} sesli video. Bölüm başlangıçları her videonun altında yazar.";
    public static string Bolumler(string dil) => En(dil) ? "Chapters" : "Bölümler";
    public static string Desteklenmiyor(string dil) => En(dil) ? "Your browser cannot play this video." : "Tarayıcınız bu videoyu oynatamıyor.";
}
