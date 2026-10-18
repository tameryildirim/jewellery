using System.Globalization;
using System.Text;

namespace Kuyumcu.Core.Domain.Girdi;

/// <summary>
/// D1-16 (18.10, rakip eşitleme: "akıllı içe aktarım — sütunların kendiliğinden tanınması"). Rakip programların
/// ve Excel'in yaygın başlık adlarından (ör. "Ürün Adı", "urun_adi", "Product Name") bizim alana ÖNERİ çıkarır.
/// ⚠ Yerel ve deterministiktir: sabit eş anlamlı listesi + harf düzleştirme. Yapay zekâ / dış servis YOK (KVKK —
///   veri cihazdan çıkmaz). Öneri yalnız ekranda seçili gelir; kullanıcı görür ve değiştirir, sunucu tahmin etmez.
/// </summary>
public static class KolonTanima
{
    private static readonly IReadOnlyDictionary<string, string[]> EsAnlamlar = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["kod"] = ["kod", "urunkodu", "stokkodu", "malkodu", "kodno", "code", "sku", "productcode", "itemcode"],
        ["ad"] = ["ad", "adi", "adsoyad", "adisoyadi", "musteriadi", "musteri", "cariadi", "cari", "unvan", "urunadi", "urun", "stokadi", "maladi", "name", "fullname", "productname", "customername"],
        ["tur"] = ["tur", "turu", "caritipi", "caritur", "type"],
        ["tip"] = ["tip", "tipi", "urunturu", "uruntipi", "kategori", "category"],
        ["telefon"] = ["telefon", "tel", "telno", "gsm", "cep", "ceptelefonu", "phone", "mobile"],
        ["email"] = ["email", "eposta", "mail", "epostaadresi"],
        ["vergiNo"] = ["vergino", "vkn", "tckn", "tcno", "tckimlikno", "kimlikno", "vergikimlikno", "taxno", "taxid"],
        ["adres"] = ["adres", "adresi", "address"],
        ["notlar"] = ["not", "notlar", "aciklama2", "note", "notes"],
        ["aciklama"] = ["aciklama", "description", "desc"],
        ["ayarId"] = ["ayar", "ayarid", "karat", "milyem", "purity"],
        ["barkod"] = ["barkod", "barcode", "ean", "ean13"],
        ["birim"] = ["birim", "olcubirimi", "unit"],
        ["gramAgirlik"] = ["gram", "gramaj", "agirlik", "gramagirlik", "brutgram", "weight", "grams"],
        ["iscilikTl"] = ["iscilik", "iscilikt", "isciliktl", "iscilikucreti", "labor", "labour"],
        ["kmtsKodu"] = ["kmts", "kmtskodu"],
        ["krediLimitiTl"] = ["kredilimiti", "limit", "kredilimitit", "creditlimit"],
        ["satisFiyati"] = ["satisfiyati", "fiyat", "satis", "etiketfiyati", "price", "saleprice"],
        ["tasAgirlik"] = ["tas", "tasagirlik", "tasgram", "stoneweight"],
        ["tasIbaresi"] = ["tasibaresi", "tasturu", "tascinsi"],
        ["tasKokeni"] = ["taskokeni", "tasmenseii", "dogalyapay"],
        ["varsayilanVadeGun"] = ["vade", "vadegun", "vadegunu", "varsayilanvade"],
        ["adet"] = ["adet", "miktar", "stok", "stokadedi", "quantity", "qty"],
        ["birimMaliyetTl"] = ["maliyet", "birimmaliyet", "alisfiyati", "alis", "cost", "unitcost"],
        ["tlBakiye"] = ["tlbakiye", "bakiye", "bakiyetl", "borc", "tlborc", "balance"],
        ["hasGramBakiye"] = ["hasbakiye", "hasgrambakiye", "hasgram", "altinbakiye", "has"],
        ["usdBakiye"] = ["usdbakiye", "dolarbakiye", "usd", "dolar"],
        ["eurBakiye"] = ["eurbakiye", "eurobakiye", "eur", "euro"],
        ["gbpBakiye"] = ["gbpbakiye", "sterlinbakiye", "gbp", "sterlin"],
    };

    /// <summary>Başlığı karşılaştırma biçimine indirger: küçük harf, Türkçe harfler düz, harf/rakam dışı atılır.</summary>
    public static string Duzlestir(string? baslik)
    {
        var s = (baslik ?? "").Trim().ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        var b = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            var d = c switch { 'ı' => 'i', 'ğ' => 'g', 'ü' => 'u', 'ş' => 's', 'ö' => 'o', 'ç' => 'c', 'â' => 'a', 'î' => 'i', 'û' => 'u', _ => c };
            if (char.IsAsciiLetterOrDigit(d)) b.Append(d);
        }
        return b.ToString();
    }

    /// <summary>
    /// Bizim <paramref name="kolon"/> için dosya başlıklarından ÖNERİ: önce birebir ad, sonra eş anlamlı listesindeki
    /// sırayla ilk eşleşen başlık. <paramref name="kullanilmis"/> başka kolona önerilmiş başlıkları dışarıda bırakır
    /// (aynı başlık iki alana önerilmez). Bulunamazsa null.
    /// </summary>
    public static string? Oner(string kolon, IReadOnlyList<string> basliklar, ISet<string>? kullanilmis = null)
    {
        var aday = basliklar.Where(b => kullanilmis is null || !kullanilmis.Contains(b)).ToList();
        var birebir = aday.FirstOrDefault(b => b.Equals(kolon, StringComparison.OrdinalIgnoreCase));
        if (birebir is not null) return birebir;
        if (!EsAnlamlar.TryGetValue(kolon, out var liste)) return null;
        foreach (var es in liste)
            if (aday.FirstOrDefault(b => Duzlestir(b) == es) is { } bulundu) return bulundu;
        return null;
    }

    /// <summary>Bütün kolonlar için öneriler (zorunlular önce önerilir; bir başlık tek kolona gider).</summary>
    public static Dictionary<string, string> OnerHepsi(IEnumerable<(string Ad, bool Zorunlu)> kolonlar, IReadOnlyList<string> basliklar)
    {
        var sonuc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var kullanilmis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (ad, _) in kolonlar.OrderByDescending(k => k.Zorunlu))
            if (Oner(ad, basliklar, kullanilmis) is { } b) { sonuc[ad] = b; kullanilmis.Add(b); }
        return sonuc;
    }
}
