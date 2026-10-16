namespace Kuyumcu.Api.Common;

/// <summary>
/// Tutarı Türkçe yazıyla yazar — "62.288,50" → "ALTMIŞİKİBİNİKİYÜZSEKSENSEKİZ TL ELLİ KR".
///
/// Gider pusulası, fatura gibi resmî belgelerde tutarın yazıyla da yer alması
/// beklenir (rakam tahrifatına karşı). Belge çıktısında kullanılır.
///
/// Sınır: 999.999.999.999,99 TL'ye kadar. Üstünde rakamla yazılır.
/// </summary>
public static class TutarYazi
{
    private static readonly string[] Birler =
        ["", "BİR", "İKİ", "ÜÇ", "DÖRT", "BEŞ", "ALTI", "YEDİ", "SEKİZ", "DOKUZ"];

    private static readonly string[] Onlar =
        ["", "ON", "YİRMİ", "OTUZ", "KIRK", "ELLİ", "ALTMIŞ", "YETMİŞ", "SEKSEN", "DOKSAN"];

    private static readonly string[] Basamaklar = ["", "BİN", "MİLYON", "MİLYAR"];

    public static string Yaz(decimal tutar, string paraBirimi = "TL", string kurusAdi = "KR")
    {
        if (tutar < 0m) return "EKSİ " + Yaz(-tutar, paraBirimi, kurusAdi);

        var yuvarlanmis = Math.Round(tutar, 2, MidpointRounding.AwayFromZero);
        var tam = (long)decimal.Truncate(yuvarlanmis);
        var kurus = (int)Math.Round((yuvarlanmis - tam) * 100m, 0, MidpointRounding.AwayFromZero);

        if (tam > 999_999_999_999L)
            return $"{yuvarlanmis:N2} {paraBirimi}";   // sınır dışı: rakamla

        var lira = tam == 0 ? "SIFIR" : UcluGruplar(tam);
        var sonuc = $"{lira} {paraBirimi}";

        if (kurus > 0) sonuc += $" {UcluGruplar(kurus)} {kurusAdi}";
        return sonuc;
    }

    /// <summary>Sayıyı üçlü gruplara bölüp basamak adlarıyla birleştirir.</summary>
    private static string UcluGruplar(long sayi)
    {
        var parcalar = new List<string>();
        var basamak = 0;

        while (sayi > 0)
        {
            var grup = (int)(sayi % 1000);
            sayi /= 1000;

            if (grup > 0)
            {
                var metin = UcBasamak(grup);

                // Türkçe kural: "BİRBİN" denmez, "BİN" denir. Ama "BİRMİLYON" denir.
                if (basamak == 1 && grup == 1) metin = "";

                parcalar.Insert(0, metin + Basamaklar[basamak]);
            }
            basamak++;
        }

        return string.Concat(parcalar);
    }

    /// <summary>0-999 arası sayıyı yazar. "YÜZ" öncesi "BİR" yazılmaz.</summary>
    private static string UcBasamak(int sayi)
    {
        var yuz = sayi / 100;
        var kalan = sayi % 100;

        var metin = yuz switch
        {
            0 => "",
            1 => "YÜZ",            // "BİRYÜZ" değil
            _ => Birler[yuz] + "YÜZ",
        };

        metin += Onlar[kalan / 10] + Birler[kalan % 10];
        return metin;
    }
}
