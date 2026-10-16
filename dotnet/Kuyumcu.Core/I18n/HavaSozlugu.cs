namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-37b — WMO HAVA KODU → İNSAN METNİ (TR + EN)
//
// UX-S31 (aynı kavram tek ad, tek dil, tek kaynak): ham veritabanı/servis
// kodu (burada WMO "weather_code" sayısı: 0, 45, 61, 95…) ekrana BASILMAZ;
// sözlükten çevrilir. Metin İKİ DİLDE de vardır — İngilizce arayüzde Türkçe
// metin kalmaz.
//
// ⚠ TEK KAYNAK: bu eşleme yalnız burada durur. HavaServisi (istemci) ve
//   ekran aynı diziden beslenir; yeni bir kod eklendiğinde iki dil BİRLİKTE
//   eklenir (dizi çiftleri yan yana durduğu için biri unutulamaz).
// Kaynak tanımı: WMO Code Table 4677 (Open-Meteo "weather_code" alanı).
// ═══════════════════════════════════════════════════════════════════════════
public static class HavaSozlugu
{
    private static readonly (int[] Kodlar, string Tr, string En)[] Tablo =
    [
        ([0],                       "Açık",            "Clear"),
        ([1, 2],                    "Az bulutlu",      "Partly cloudy"),
        ([3],                       "Bulutlu",         "Cloudy"),
        ([45, 48],                  "Sisli",           "Foggy"),
        ([51, 53, 55, 56, 57],      "Çiseli",          "Drizzle"),
        ([61, 63, 65, 66, 67],      "Yağmurlu",        "Rainy"),
        ([71, 73, 75, 77],          "Karlı",           "Snowy"),
        ([80, 81, 82],              "Sağanak",         "Showers"),
        ([85, 86],                  "Kar sağanağı",    "Snow showers"),
        ([95, 96, 99],              "Gök gürültülü",   "Thunderstorm"),
    ];

    /// <summary>
    /// WMO kodunu seçili dildeki duruma çevirir. Bilinmeyen/boş kod için
    /// BOŞ dizge döner (uydurma metin yazılmaz — md.1).
    /// </summary>
    public static string Durum(int? kod, string dil)
    {
        if (kod is not { } k) return "";
        foreach (var (kodlar, tr, en) in Tablo)
            if (Array.IndexOf(kodlar, k) >= 0)
                return dil == "en" ? en : tr;
        return "";
    }
}
