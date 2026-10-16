namespace Kuyumcu.Web.Layout;

/// <summary>
/// PAKET S1-c — üst bardaki "Yardım" bağlantısının hedefi.
/// <para>
/// Tuvalde bağlantı tek bir "Yardım"dır. Kılavuz (<c>wwwroot/kilavuz.html</c>)
/// zaten ekran başına <c>&lt;section id="..."&gt;</c> çıpaları taşıyor; bağlantı
/// bulunduğun ekranın bölümüne gider, eşleşme yoksa kılavuzun başına.
/// Bağlantı YENİ SEKMEDE açılır: tezgâhta yarım kalmış bir fiş Yardım'a
/// basıldı diye kaybolmaz.
/// </para>
/// <para>
/// ⚠ Harita yalnız kılavuzda GERÇEKTEN bulunan çıpalara gider; tahminle çıpa
///   yazılmaz. <c>S1cUstBarTestleri</c> her değerin kilavuz.html'de bir
///   <c>section id</c> olarak durduğunu ve her anahtarın gerçek bir @page
///   rotasının ilk kesimi olduğunu İKİ YÖNLÜ doğrular.
/// </para>
/// </summary>
public static class YardimBaglantisi
{
    /// <summary>Kılavuzun kendisi (statik dosya — CSP 'self').</summary>
    public const string Kilavuz = "/kilavuz.html";

    /// <summary>Rotanın İLK kesimi → kilavuz.html çıpası.</summary>
    public static readonly IReadOnlyDictionary<string, string> Harita =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [""] = "panel",
            ["giris"] = "giris",
            ["cariler"] = "cariler",
            ["urunler"] = "urunler",
            ["islemler"] = "islemler",
            ["kur"] = "kur",
            ["kasalar"] = "kasalar",
            ["banka"] = "banka",
            ["faturalar"] = "faturalar",
            ["raporlar"] = "raporlar",
            ["kullanicilar"] = "kullanicilar",
            ["ayarlar"] = "ayarlar",
            ["vezne"] = "vezne",
            ["emanet"] = "emanet",
            ["siparisler"] = "siparisler",
            ["sayim"] = "sayim",
            ["pos"] = "pos",
            ["uyum"] = "uyum",
            ["gider-pusulalari"] = "giderpusulasi",
            ["yedekler"] = "yedekler",
            ["veri-ice-aktarim"] = "veriaktarim",
            ["veri-disa-aktarim"] = "veriaktarim",
        };

    /// <summary>
    /// Mutlak yol → kılavuz adresi. Tanınmayan ekran kılavuzun BAŞINA gider
    /// (boş bağlantı ya da uydurma çıpa yok).
    /// </summary>
    public static string Adres(string yol)
    {
        var temiz = (yol ?? "").Split('?', '#')[0].Trim('/');
        var ilk = temiz.Split('/')[0];
        return Harita.TryGetValue(ilk, out var capa) ? $"{Kilavuz}#{capa}" : Kilavuz;
    }
}
