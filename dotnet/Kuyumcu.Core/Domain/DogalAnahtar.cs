namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-05 (Y-2) — DOĞAL ANAHTAR NORMALLEŞTİRME
//
// NEDEN GEREKLİ:
// MySQL'de UNIQUE indeks birden çok NULL kabul eder ama BOŞ DİZGE ('') NULL
// DEĞİLDİR. Barkodsuz iki ürün '' ile kaydedilirse ikincisi 1062 ile reddedilir
// ve kullanıcı "barkod zaten kullanımda" gibi ANLAMSIZ bir hata görür — oysa
// barkod hiç girilmemiştir. Aynı tuzak cariler.vergi_no için de geçerlidir.
//
// Bu yüzden opsiyonel doğal anahtarlar veritabanına yazılmadan ÖNCE tek bir
// yerden geçer: baştaki/sondaki boşluk atılır, geriye boş dizge kalıyorsa NULL
// olur. "Değer yok" hâlinin TEK gösterimi NULL'dır.
//
// ⚠ ZORUNLU doğal anahtar (urunler.kod) için Normalle() KULLANILMAZ — orada
//   boşluk kırpma ayrı bir karardır ve alan NOT NULL'dır; bkz. Kirp().
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Benzersizlik kısıtı taşıyan opsiyonel alanların tek normalleştirme yolu.</summary>
public static class DogalAnahtar
{
    /// <summary>
    /// Opsiyonel doğal anahtarı normalleştirir: kırpar, boş kalırsa <c>null</c> döner.
    /// "Boş dizge" ile "değer yok" arasındaki ikinci hâli ortadan kaldırır.
    /// </summary>
    public static string? Normalle(string? deger)
    {
        if (deger is null) return null;
        var kirpik = deger.Trim();
        return kirpik.Length == 0 ? null : kirpik;
    }

    /// <summary>
    /// ZORUNLU doğal anahtarı kırpar (NULL'a çevirmez). Boş/boşluk verilirse
    /// olduğu gibi döner — zorunluluk denetimi DTO doğrulamasının işidir,
    /// bu yardımcının değil.
    /// </summary>
    public static string Kirp(string deger) => deger.Trim();
}
