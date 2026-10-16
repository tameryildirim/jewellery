namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// KIYMETLİ TAŞ KÖKENİ — Kuyum Ticareti Yönetmeliği md.11/(e) · PLAN-2 Adım 3
//
// DAYANAK (birincil metin okundu): Resmî Gazete 24.06.2026 / 33290 ile
// Kuyum Ticareti Yönetmeliği md.11'e (e) bendi eklendi:
//
//   "Laboratuvar ortamında üretilen veya insan müdahalesiyle oluşturulan
//    kıymetli taş ürünlerini 'sentetik', 'laboratuvar üretimi', 'yapay üretim'
//    … ibarelerinden en az birine ETİKET, ÜRÜN SERTİFİKASI, FATURA, İNTERNET
//    SAYFASI, REKLAM VE TANITIM MATERYALLERİNDE … açıkça yer vermeksizin
//    satışa sunamaz. Bu ürünler ile doğal kıymetli taşları … AYRI BÖLÜMLERDE,
//    internet ortamında AYRI KATEGORİLERDE satışa sunar."
//
// Bu bir yazılım yükümlülüğüdür: ürün kartında zorunlu alan, etiket metni,
// fatura satırı ve kategori ayrımı gerektirir.
//
// ⚠ ÜÇ İBAREDEN EN AZ BİRİ — yönetmelik alternatif sunar, hangisinin
// kullanılacağını işletmeye bırakır. Bu yüzden kökene karşılık gelen ibare
// SABİT değil, seçilebilir tutulur.
//
// ⚠ "BİLİNMİYOR" DİYE BİR SEÇENEK YOK. Taşlı bir ürünün kökeni belirtilmeden
// satışa sunulamaz; belirsizlik, doğal sayılması anlamına gelmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kıymetli taşın kökeni — Kuyum Ticareti Yönetmeliği md.11/(e).</summary>
public static class TasKokeni
{
    /// <summary>Doğada oluşmuş taş. Zorunlu ibare gerektirmez.</summary>
    public const string Dogal = "DOGAL";

    /// <summary>Laboratuvar ortamında üretilmiş. İbare ZORUNLU.</summary>
    public const string LaboratuvarUretimi = "LABORATUVAR_URETIMI";

    /// <summary>Sentetik. İbare ZORUNLU.</summary>
    public const string Sentetik = "SENTETIK";

    /// <summary>İnsan müdahalesiyle oluşturulmuş / yapay üretim. İbare ZORUNLU.</summary>
    public const string YapayUretim = "YAPAY_URETIM";

    /// <summary>Üründe kıymetli taş yok — yönetmelik kapsamı dışı.</summary>
    public const string TasYok = "TAS_YOK";

    public static readonly string[] Hepsi =
        [TasYok, Dogal, LaboratuvarUretimi, Sentetik, YapayUretim];

    /// <summary>
    /// Bu köken, md.11/(e) gereği etiket/sertifika/fatura/internette ibare
    /// zorunluluğu doğurur mu?
    /// </summary>
    public static bool IbareGerekir(string? koken) =>
        koken is LaboratuvarUretimi or Sentetik or YapayUretim;

    /// <summary>Geçerli bir köken değeri mi?</summary>
    public static bool Gecerli(string? koken) =>
        koken is not null && Hepsi.Contains(koken);

    /// <summary>
    /// Yönetmeliğin saydığı ibarelerden köken için varsayılan olanı.
    /// İşletme başka bir ibare seçebilir (md.11/(e) "en az birine" der).
    /// </summary>
    public static string VarsayilanIbare(string koken) => koken switch
    {
        Sentetik => "sentetik",
        LaboratuvarUretimi => "laboratuvar üretimi",
        YapayUretim => "yapay üretim",
        _ => "",
    };

    /// <summary>
    /// Ürünün etiket / fatura / vitrin metninde görünmesi gereken ibare.
    /// Doğal taş ve taşsız ürün için boştur.
    /// </summary>
    public static string Ibare(string? koken, string? ozelIbare = null)
    {
        if (!IbareGerekir(koken)) return "";
        return string.IsNullOrWhiteSpace(ozelIbare)
            ? VarsayilanIbare(koken!)
            : ozelIbare.Trim();
    }

    /// <summary>
    /// Vitrin ve internet kategorisi. md.11/(e) doğal ve yapay taşların
    /// <b>ayrı bölümlerde / ayrı kategorilerde</b> sunulmasını ister.
    /// </summary>
    public static string Kategori(string? koken) => koken switch
    {
        Dogal => "DOGAL_TAS",
        LaboratuvarUretimi or Sentetik or YapayUretim => "YAPAY_TAS",
        _ => "TASSIZ",
    };
}
