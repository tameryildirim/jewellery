using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Urun;

/// <summary>
/// Belge tavanları — ADR-0005: rakam koda GÖMÜLMEZ, yapılandırmadan okunur
/// (<c>appsettings.json → "Belge"</c>). Buradaki varsayılanlar yapılandırmadaki
/// değerlerle AYNIDIR; yapılandırma eksikse davranış değişmez.
/// </summary>
public sealed class BelgeAyarlari
{
    /// <summary>PDF tavanı (MB).</summary>
    public int EnFazlaPdfMb { get; init; } = Belge.EnFazlaPdfBayt / 1024 / 1024;

    /// <summary>JPEG/PNG tavanı (MB).</summary>
    public int EnFazlaGorselMb { get; init; } = Belge.EnFazlaGorselBayt / 1024 / 1024;

    /// <summary>Kiracı başına toplam aktif blob tavanı (MB).</summary>
    public int KiraciToplamMb { get; init; } = 500;

    /// <summary>Bir sahibe bağlanabilecek en fazla AKTİF belge sayısı.</summary>
    public int SahipBasinaAzami { get; init; } = 20;

    public long EnFazlaPdfBayt => (long)EnFazlaPdfMb * 1024 * 1024;
    public long EnFazlaGorselBayt => (long)EnFazlaGorselMb * 1024 * 1024;
    public long KiraciToplamBayt => (long)KiraciToplamMb * 1024 * 1024;
}

/// <summary>
/// SAF içerik kapısı — veritabanı GÖRMEZ.
///
/// İLKE: içerik türü BEYANA GÜVENİLMEZ. Karar yalnız magic byte'a göre verilir.
/// Kabul edilen tek liste: PDF · JPEG · PNG. SVG/WebP/GIF KABUL EDİLMEZ
/// (Mehmet'in K4-4 koşulu: SVG betik taşır, sunucuya görüntü kütüphanesi
/// eklenmez — tanınmayan her imza reddedilir).
/// </summary>
public static class BelgeKapisi
{
    public const string MimePdf = "application/pdf";
    public const string MimeJpeg = "image/jpeg";
    public const string MimePng = "image/png";

    private static readonly BelgeAyarlari Varsayilan = new();

    /// <summary>PDF: <c>25 50 44 46 2D</c> (%PDF-)</summary>
    private static readonly byte[] PdfImza = [0x25, 0x50, 0x44, 0x46, 0x2D];

    /// <summary>JPEG: <c>FF D8 FF</c></summary>
    private static readonly byte[] JpegImza = [0xFF, 0xD8, 0xFF];

    /// <summary>PNG: <c>89 50 4E 47 0D 0A 1A 0A</c></summary>
    private static readonly byte[] PngImza = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Tanınan imza yoksa null döner — "bilinmeyen" tahmin edilmez, reddedilir.</summary>
    public static string? MimeTani(byte[] icerik)
    {
        if (Baslar(icerik, PdfImza)) return MimePdf;
        if (Baslar(icerik, JpegImza)) return MimeJpeg;
        if (Baslar(icerik, PngImza)) return MimePng;
        return null;
    }

    private static bool Baslar(byte[] icerik, byte[] imza)
    {
        if (icerik.Length < imza.Length) return false;
        for (var i = 0; i < imza.Length; i++)
            if (icerik[i] != imza[i]) return false;
        return true;
    }

    /// <summary>
    /// Magic byte + belge türü + boyut kapısı. Geçerse <paramref name="mime"/> dolar.
    ///
    /// SERTIFIKA YALNIZ PDF kabul eder (bir sertifikanın ekran görüntüsü sertifika
    /// değildir); GORSEL/FOTOGRAF yalnız JPEG/PNG; EK her ikisi.
    /// </summary>
    public static void IcerikDogrula(byte[] icerik, string tur, out string mime, BelgeAyarlari? ayarlar = null)
    {
        var a = ayarlar ?? Varsayilan;

        if (icerik.Length == 0) throw new BadRequestException("Belge boş");

        // Mutlak tavan ÖNCE: hiçbir türde aşılamayacak sınır. (Eski PdfDogrula
        // davranışı korunur — boyut denetimi imzadan önce gelir.)
        if (icerik.Length > a.EnFazlaPdfBayt)
            throw new BadRequestException($"Belge en fazla {a.EnFazlaPdfMb} MB olabilir");

        if (!BelgeTuru.Tumu.Contains(tur))
            throw new BadRequestException($"Geçersiz belge türü: {tur} ({string.Join(", ", BelgeTuru.Tumu)})");

        var taninan = MimeTani(icerik);

        if (tur == BelgeTuru.Sertifika)
        {
            // ⚠ MESAJ KORUNUR: A16 canlı kanıtı ve PirlantaKapisiTestleri bu metne bakar.
            if (taninan != MimePdf)
                throw new BadRequestException("Belge PDF değil (imza %PDF- bulunamadı)");
        }
        else if (taninan is null)
        {
            throw new BadRequestException("Dosya imzası tanınmadı: yalnız PDF, JPEG ve PNG kabul edilir");
        }
        else if (tur is BelgeTuru.Gorsel or BelgeTuru.Fotograf && taninan == MimePdf)
        {
            throw new BadRequestException($"{tur} türü yalnız JPEG/PNG kabul eder (dosya imzası: PDF)");
        }

        mime = taninan!;

        if (mime != MimePdf && icerik.Length > a.EnFazlaGorselBayt)
            throw new BadRequestException($"Görsel en fazla {a.EnFazlaGorselMb} MB olabilir");
    }
}
