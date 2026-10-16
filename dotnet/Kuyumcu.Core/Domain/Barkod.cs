using System.Globalization;
using System.Text;

namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// BARKOD — PLAN-2 Adım 5 · Code 128
//
// NEDEN EAN-13 DEĞİL (bilinçli karar):
// EAN-13 numarası GS1'den alınan bir **firma öneki** gerektirir. Elimizde böyle
// bir önek YOK ve uydurmak, başka bir firmanın numara alanına yazmak demektir.
// "2" ile başlayan mağaza içi (restricted circulation) aralığı da yalnız kapalı
// devrede geçerlidir; ürün bir kez dükkân dışına çıktığında anlamı kalmaz.
//
// Code 128 bir kayıt otoritesi İSTEMEZ, değişken uzunluktadır, alfanümeriktir
// ve her el terminali/okuyucu tarafından desteklenir. Kuyumcunun iç stok
// etiketi için doğru biçim budur. GS1 öneki alınırsa EAN-13 sonradan eklenir;
// bu sınıf o gün için `Code128` adıyla ayrı durur.
//
// ⚠ Bu dosya SAF ALGORİTMADIR: ne I/O yapar ne de çizim. SVG üretimi istemcide
// (ADR-0007), genişlik hesabı burada. Aynı kod hem sunucuda hem WASM'da çalışır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Code 128-B kodlayıcı (ASCII 32–126) ve sağlama hesabı.</summary>
public static class Code128
{
    /// <summary>Code 128 sembol tablosu — her değer 11 modül (bar/boşluk) genişliğinde.</summary>
    private static readonly string[] Desenler =
    [
        "11011001100", "11001101100", "11001100110", "10010011000", "10010001100",
        "10001001100", "10011001000", "10011000100", "10001100100", "11001001000",
        "11001000100", "11000100100", "10110011100", "10011011100", "10011001110",
        "10111001100", "10011101100", "10011100110", "11001110010", "11001011100",
        "11001001110", "11011100100", "11001110100", "11101101110", "11101001100",
        "11100101100", "11100100110", "11101100100", "11100110100", "11100110010",
        "11011011000", "11011000110", "11000110110", "10100011000", "10001011000",
        "10001000110", "10110001000", "10001101000", "10001100010", "11010001000",
        "11000101000", "11000100010", "10110111000", "10110001110", "10001101110",
        "10111011000", "10111000110", "10001110110", "11101110110", "11010001110",
        "11000101110", "11011101000", "11011100010", "11011101110", "11101011000",
        "11101000110", "11100010110", "11101101000", "11101100010", "11100011010",
        "11101111010", "11001000010", "11110001010", "10100110000", "10100001100",
        "10010110000", "10010000110", "10000101100", "10000100110", "10110010000",
        "10110000100", "10011010000", "10011000010", "10000110100", "10000110010",
        "11000010010", "11001010000", "11110111010", "11000010100", "10001111010",
        "10100111100", "10010111100", "10010011110", "10111100100", "10011110100",
        "10011110010", "11110100100", "11110010100", "11110010010", "11011011110",
        "11011110110", "11110110110", "10101111000", "10100011110", "10001011110",
        "10111101000", "10111100010", "11110101000", "11110100010", "10111011110",
        "10111101110", "11101011110", "11110101110", "11010000100", "11010010000",
        "11010011100", "1100011101011",   // 106: STOP (13 modül — tek istisna)
    ];

    /// <summary>Code 128-B başlangıç sembolü.</summary>
    private const int BaslangicB = 104;
    private const int Stop = 106;

    /// <summary>Code 128-B'nin kodlayabildiği karakter aralığı: ASCII 32–126.</summary>
    public static bool Kodlanabilir(string? veri) =>
        !string.IsNullOrEmpty(veri) && veri.All(c => c is >= ' ' and <= '~');

    /// <summary>
    /// Sağlama (checksum) değeri: başlangıç + Σ(değer × konum), mod 103.
    /// Okuyucu bunu doğrular; yanlış hesaplanan barkod hiç okunmaz.
    /// </summary>
    public static int Saglama(string veri)
    {
        var toplam = BaslangicB;
        for (var i = 0; i < veri.Length; i++)
            toplam += (veri[i] - 32) * (i + 1);
        return toplam % 103;
    }

    /// <summary>
    /// Barkodun modül dizisi: '1' = bar, '0' = boşluk.
    /// Sessiz alan (quiet zone) DAHİL DEĞİLDİR — çizim katmanı ekler.
    /// </summary>
    public static string Moduller(string veri)
    {
        if (!Kodlanabilir(veri))
            throw new ArgumentException(
                "Code 128-B yalnız ASCII 32–126 arası karakterleri kodlar. " +
                $"Geçersiz veri: '{veri}'", nameof(veri));

        var sb = new StringBuilder(Desenler[BaslangicB]);
        foreach (var c in veri) sb.Append(Desenler[c - 32]);
        sb.Append(Desenler[Saglama(veri)]);
        sb.Append(Desenler[Stop]);
        return sb.ToString();
    }

    /// <summary>
    /// Etiket için SVG. Metin ÜRETİLMEZ; çağıran ürün adını/fiyatı kendi
    /// düzeninde yazar. <paramref name="modulGenislik"/> tek modülün px genişliği.
    /// </summary>
    public static string Svg(string veri, int yukseklik = 40, double modulGenislik = 1.6,
                             int sessizAlanModul = 10)
    {
        var m = Moduller(veri);
        var toplamModul = m.Length + sessizAlanModul * 2;
        var genislik = toplamModul * modulGenislik;

        // ⚠ SVG SAYILARI KÜLTÜRDEN BAĞIMSIZ YAZILIR.
        // Türkçe kültürde `{1.6:0.##}` → "1,6" üretir; SVG bunu ayrıştıramaz ve
        // barkod tarayıcıda HİÇ ÇİZİLMEZ. Testler invariant kültürde koştuğu için
        // yeşildi; hata yalnız canlı Türkçe arayüzde göründü (2026-09-14).
        var iv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {genislik.ToString("0.##", iv)} {yukseklik.ToString(iv)}\" ")
          .Append($"width=\"{genislik.ToString("0.##", iv)}\" height=\"{yukseklik.ToString(iv)}\" role=\"img\" ")
          .Append($"aria-label=\"barkod {System.Net.WebUtility.HtmlEncode(veri)}\">")
          .Append($"<rect width=\"{genislik.ToString("0.##", iv)}\" height=\"{yukseklik.ToString(iv)}\" fill=\"#fff\"/>");

        // Ardışık barları tek dikdörtgende birleştir — DOM'u küçük tutar.
        var i = 0;
        while (i < m.Length)
        {
            if (m[i] == '0') { i++; continue; }
            var bas = i;
            while (i < m.Length && m[i] == '1') i++;
            var x = (sessizAlanModul + bas) * modulGenislik;
            var w = (i - bas) * modulGenislik;
            sb.Append($"<rect x=\"{x.ToString("0.##", iv)}\" y=\"0\" width=\"{w.ToString("0.##", iv)}\" height=\"{yukseklik.ToString(iv)}\" fill=\"#000\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}

/// <summary>
/// İç stok barkodu üretimi. Biçim: <c>K</c> + 10 haneli baz32 gövde.
/// GS1 numarası DEĞİLDİR ve öyleymiş gibi gösterilmez.
/// </summary>
public static class BarkodUretici
{
    /// <summary>Karıştırılabilecek harfler (I, L, O, U) elenmiştir — elle okunabilirlik.</summary>
    private const string Abece = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const string Onek = "K";
    public const int GovdeUzunluk = 10;

    /// <summary>
    /// ULID'den türetilmiş barkod. ULID'in kendisi 26 karakterdir; etikete
    /// sığmaz ve elle okunamaz. Son 10 karakteri (rastgele kısmı) kullanılır.
    /// </summary>
    public static string Uret(string ulid)
    {
        if (string.IsNullOrWhiteSpace(ulid) || ulid.Length < GovdeUzunluk)
            throw new ArgumentException("Barkod üretimi için geçerli bir ULID gerekir", nameof(ulid));

        var govde = ulid[^GovdeUzunluk..].ToUpperInvariant();
        // ULID zaten Crockford baz32 — yine de abecemize düşmeyen bir karakter
        // gelirse sessizce geçmesin.
        foreach (var c in govde)
            if (!Abece.Contains(c))
                throw new ArgumentException($"Barkod gövdesinde beklenmeyen karakter: '{c}'", nameof(ulid));

        return Onek + govde;
    }

    /// <summary>Bizim ürettiğimiz biçime uyuyor mu? (Elle girilen barkodlar da geçerlidir.)</summary>
    public static bool BizimBicim(string? barkod) =>
        barkod is not null
        && barkod.Length == Onek.Length + GovdeUzunluk
        && barkod.StartsWith(Onek, StringComparison.Ordinal)
        && barkod[Onek.Length..].All(Abece.Contains);
}
