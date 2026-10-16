namespace Kuyumcu.Web.Bilesenler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-15 — DERİN BAĞLANTI SÖZLEŞMESİ (kimlik B-29)
//
// KURAL (PLAN § 2 P-15 DoD): "sorgu dizesindeki HER süzgeç hedef sayfada
// UYGULANIR ve KUTULARDA GÖRÜNÜR."
//
// NEDEN MEKANİZMA, NEDEN SAYFA SAYFA DEĞİL (PLAN § 1 md.1): sorgu dizesini
// okuyan üç ayrı sayfa üç ayrı ayrıştırıcı yazmıştı; dördüncüsü yazılmadığı
// için `/cariler?cariId=…` bağlantısı SESSİZCE yok sayılıyordu (kullanıcı
// bir müşteriye tıklıyor, tüm listeyi görüyordu). Tek ayrıştırıcı + tek
// sayaç (`SORGU_SUZGECI_OKUMAYAN_SAYFA = 0`) bunu mekanik olarak kapatır.
//
// ⚠ SESSİZ YOK SAYMA YASAK (Anayasa md.1): tanınmayan ya da geçersiz bir
//   süzgeç değeri "yokmuş gibi" davranılmaz — çağıran sayfa bunu kullanıcıya
//   görünür kılar (rozet ya da bant). Bu sınıf yalnız OKUR, karar vermez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Adres çubuğundaki sorgu dizesinden süzgeç okuma — TEK ayrıştırıcı.</summary>
public static class DerinBaglanti
{
    /// <summary>
    /// Mutlak ya da göreli adresteki sorgu dizesini ad→değer sözlüğüne çevirir.
    /// Aynı ad iki kez geçerse İLKİ kazanır (ikincisi sessizce ezmez).
    /// </summary>
    public static Dictionary<string, string> Suzgecler(string uri)
    {
        var sonuc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var soru = uri.IndexOf('?');
        if (soru < 0) return sonuc;

        var sorgu = uri[(soru + 1)..];
        var kare = sorgu.IndexOf('#');
        if (kare >= 0) sorgu = sorgu[..kare];

        foreach (var parca in sorgu.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var esit = parca.IndexOf('=');
            if (esit <= 0) continue;
            var ad = Uri.UnescapeDataString(parca[..esit]);
            var deger = Uri.UnescapeDataString(parca[(esit + 1)..]);
            if (deger.Length == 0) continue;
            sonuc.TryAdd(ad, deger);
        }
        return sonuc;
    }

    /// <summary>Tek süzgeci okur; yoksa null.</summary>
    public static string? Oku(string uri, string ad) =>
        Suzgecler(uri).TryGetValue(ad, out var d) ? d : null;
}
