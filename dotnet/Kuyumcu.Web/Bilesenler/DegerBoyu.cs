namespace Kuyumcu.Web.Bilesenler;

/// <summary>
/// PAKET ORTAK-1 (B) — büyük değer (tutar/gram) yazısının UZUNLUK KADEMESİ.
/// <para>
/// Para kartındaki değer kırılamaz bir dizgedir ("−728.188,36 ₺"). Kart
/// daraldığında (960/1024 px) değer kutudan taşıyordu. css/ortak.css değerin
/// yazı boyunu kart içeriğinin genişliğine (kap sorgusu, cqi) bağlar; KAÇ
/// cqi olacağı değerin karakter sayısına bağlıdır — kısa değer ("3", "12 ₺")
/// büyük kalır, uzun değer tek satıra sığacak kadar küçülür. Özgün (tuval)
/// boyu hiçbir kademede AŞILMAZ: geniş kartta görünüm değişmez.
/// </para>
/// Kademe yalnız <c>data-deger-boy</c> özniteliğine yazılır; değer, biçim ve
/// id DEĞİŞMEZ (satır içi style yok).
/// </summary>
public static class DegerBoyu
{
    /// <summary>Kademe sınırları (karakter): ≤9 · ≤12 · ≤15 · ≤20 · 21+.</summary>
    public const int Kisa = 9, Orta = 12, Uzun = 15, CokUzun = 20;

    /// <summary>Değerin uzunluk kademesi: kisa · orta · uzun · cok-uzun · en-uzun.</summary>
    public static string Kademe(string? deger) => (deger?.Length ?? 0) switch
    {
        <= Kisa => "kisa",
        <= Orta => "orta",
        <= Uzun => "uzun",
        <= CokUzun => "cok-uzun",
        _ => "en-uzun",
    };
}
