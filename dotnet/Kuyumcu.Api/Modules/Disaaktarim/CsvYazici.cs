using System.Globalization;
using System.Text;

namespace Kuyumcu.Api.Modules.Disaaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// CSV YAZICI — RFC 4180
//
// Neden elle yazıldı: tek ihtiyaç kaçış kuralı ve Türkçe biçimleme. Bir kütüphane
// eklemek SBOM'a yeni bir bağımlılık ve lisans yükü getirirdi (ADR-0007 ile aynı
// gerekçe). Kaçış kuralı 20 satır ve testle sabitlendi.
//
// ⚠ CSV ENJEKSİYONU: "=", "+", "-", "@" ile başlayan bir hücre Excel'de FORMÜL
// olarak çalışır. Dışa aktarılan veri müşteri adı içerir; bir müşteri adı
// "=cmd|..." olabilir. Bu yüzden bu karakterlerle başlayan hücrelerin başına
// tek tırnak konur — Excel'in kendi kaçış yöntemi.
// ═════════════════════════════════════════════════════════════════════════════

public static class CsvYazici
{
    /// <summary>Excel'in UTF-8'i tanıması için BOM. Olmadan Türkçe karakterler bozulur.</summary>
    public static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    private const string Tehlikeli = "=+-@\t\r";

    public static string Hucre(string? deger)
    {
        if (string.IsNullOrEmpty(deger)) return "";

        var s = deger;
        if (Tehlikeli.Contains(s[0])) s = "'" + s;

        return s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
    }

    public static string Satir(IEnumerable<string?> hucreler) =>
        string.Join(",", hucreler.Select(Hucre)) + "\r\n";

    // ── Biçimleme: makine okunur, kültürden bağımsız ─────────────────────────
    // Nokta ayraçlı ondalık ve ISO-8601 tarih seçildi; başka bir programa
    // aktarılacak veri Türkçe virgüllü ondalıkla taşınmaz.

    public static string Sayi(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    public static string Sayi(decimal? d) => d?.ToString(CultureInfo.InvariantCulture) ?? "";
    public static string Tarih(DateTime t) => t.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    public static string Tarih(DateTime? t) => t is null ? "" : Tarih(t.Value);
    public static string Evet(bool b) => b ? "EVET" : "HAYIR";

    public static byte[] Bayt(string s) => Encoding.UTF8.GetBytes(s);
}
