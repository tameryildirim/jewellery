using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// CARİ EŞLEŞTİRİCİ — SAF SINIF (paket 32).
//
// ⚠ EN ÖNEMLİ KURAL (K4-OK-5): ÜNVAN BENZERLİĞİ HİÇBİR KOŞULDA OTOMATİK
//   BAĞLAMAZ. Bu sınıf yalnız ÖNERİ SIRALAR. Otomatik bağın TEK yolu IBAN
//   hash'inin cari_ibanlari'nda TAM eşleşmesidir.
//   Sebep: "Ayşe Demir" ile "Ayşe Demirci" iki ayrı müşteridir ve yanlış
//   eşleşmenin geri alınması, eşleşmemiş satırı elle bağlamaktan ÇOK daha
//   pahalıdır (yanlış müşterinin bakiyesi değişir ve kimse fark etmez).
// ═════════════════════════════════════════════════════════════════════════════

public static class CariEslestirici
{
    /// <summary>
    /// ⚠ AÇIKÇA tr-TR: invariant kültürde "ilker" → "ILKER" olurdu ve "İLKER"
    /// adlı cari ile eşleşmezdi. TestOrtami/HttpOrtami iş parçacığı kültürünü
    /// AYARLAMAZ; bu yüzden kültür burada koda yazılıdır ve makine kültürüne
    /// bırakılmaz (Aşkın koşulu).
    /// </summary>
    public static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Şirket son ekleri — ünvan karşılaştırmasında gürültüdür.</summary>
    private static readonly string[] SirketEkleri =
        ["LTD", "ŞTİ", "STI", "A.Ş.", "AŞ", "A.S.", "AS", "SAN", "TİC", "TIC",
         "SANAYİ", "SANAYI", "TİCARET", "TICARET", "LİMİTED", "LIMITED", "ANONİM", "ANONIM", "ŞİRKETİ", "SIRKETI"];

    /// <summary>
    /// Ünvanı karşılaştırılabilir hâle getirir: tr-TR büyük harf, noktalama ve
    /// çoklu boşluk temizliği, sondaki şirket ekleri atılır.
    /// </summary>
    public static string UnvanNormalle(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return "";

        // ⚠ tr-TR: 'i' → 'İ'. ToUpperInvariant BURADA KULLANILMAZ.
        var buyuk = ham.ToUpper(Turkce);

        var kutu = new StringBuilder(buyuk.Length);
        foreach (var c in buyuk)
            kutu.Append(char.IsLetterOrDigit(c) ? c : ' ');

        var parcalar = kutu.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var ekler = SirketEkleri
            .Select(e => new string(e.Where(char.IsLetterOrDigit).ToArray()).ToUpper(Turkce))
            .ToHashSet(StringComparer.Ordinal);

        // Sondan başlayarak ek temizliği: "AYŞE DEMİR LTD ŞTİ" → "AYŞE DEMİR".
        // ⚠ Baştan temizlenmez: "TİCARET BANKASI" gibi ünvanlarda anlam taşıyan
        //   ilk kelime silinirdi.
        while (parcalar.Count > 1 && ekler.Contains(parcalar[^1])) parcalar.RemoveAt(parcalar.Count - 1);

        return string.Join(' ', parcalar);
    }

    /// <summary>
    /// Jaccard benzerliği: ortak kelime / toplam farklı kelime. 0 ile 1 arası.
    /// <para>⚠ Bu skor YALNIZ öneri sıralamasında kullanılır (K4-OK-5).</para>
    /// </summary>
    public static decimal Skor(string? a, string? b)
    {
        var x = UnvanNormalle(a).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var y = UnvanNormalle(b).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (x.Count == 0 || y.Count == 0) return 0m;

        var kesisim = x.Count(t => y.Contains(t));
        var birlesim = x.Count + y.Count - kesisim;
        return birlesim == 0 ? 0m : decimal.Round((decimal)kesisim / birlesim, 4);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// MÜKERRER ANAHTARI — paket 32.
//
// ⚠ TEK TANIM, İKİ ÇAĞIRAN: elle girilen dekont (BankaServisi.HareketEkleIc) ve
//   ekstre satırı (EkstreServisi). İki ayrı formül yazılsaydı "elle girdiğim
//   havale dosyadan gelince mükerrer görünmüyor" hatası doğardı ve kimse
//   nedenini bulamazdı.
//
// ⚠ DÜRÜST SINIR: banka referansı YOKSA aynı gün aynı tutarda iki MEŞRU havale
//   aynı anahtarı üretir. Tasarım bunu MUKERRER işaretleyip kullanıcıya bırakır
//   (görünür ve düzeltilebilir), sessizce yutmaz.
// ═════════════════════════════════════════════════════════════════════════════

public static class DedupeAnahtari
{
    /// <summary>
    /// SHA-256(hesapId | İŞLETME GÜNÜ(yyyy-MM-dd) | tutar(F2,invariant) | yön |
    /// bankaReferans ?? '' | karsiIban ?? '' | açıklamaNormal)
    /// <para>
    /// ⚠ GÜN, UTC GÜNÜ DEĞİL İŞLETME GÜNÜDÜR (Zaman.UtcdenYerele). Ölçülen hata:
    /// elle girilen dekont "2026-09-01T00:00Z" olarak, aynı havalenin ekstre
    /// satırı ise yerel gün başı olan "2026-08-31T21:00Z" olarak saklanıyor;
    /// UTC gününe bakan bir anahtar bu İKİ AYNI havaleyi FARKLI sanıyor ve
    /// mükerrer koruması sessizce hiç çalışmıyordu.
    /// </para>
    /// </summary>
    public static string Uret(
        string bankaHesapId, DateTime tarih, decimal tutar, string yon,
        string? bankaReferans, string? karsiIban, string? aciklama)
    {
        var parcalar = string.Join('|',
            bankaHesapId,
            Common.Zaman.UtcdenYerele(tarih).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            tutar.ToString("F2", CultureInfo.InvariantCulture),
            yon,
            (bankaReferans ?? "").Trim().ToUpperInvariant(),
            EkstreAyristirici.IbanNormalle(karsiIban) ?? "",
            AciklamaNormalle(aciklama));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(parcalar)));
    }

    /// <summary>Çoklu boşluk ve büyük/küçük harf farkı aynı havaleyi farklı göstermesin.</summary>
    private static string AciklamaNormalle(string? ham) =>
        string.IsNullOrWhiteSpace(ham)
            ? ""
            : string.Join(' ', ham.ToUpper(CariEslestirici.Turkce)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
