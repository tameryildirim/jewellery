using System.Text;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Iceaktarim;

/// <summary>
/// İçe aktarım tavanları — ADR-0005: rakam koda GÖMÜLMEZ, yapılandırmadan
/// okunur (<c>appsettings.json → "IceAktarim"</c>). Buradaki varsayılanlar
/// yapılandırmadaki değerlerle AYNIDIR; yapılandırma eksikse davranış değişmez.
/// </summary>
public sealed class IceAktarimAyarlari
{
    /// <summary>Bir oturumda işlenebilecek en fazla VERİ satırı (Mehmet: 5.000 başlangıç).</summary>
    public int MaksSatir { get; init; } = 5_000;

    /// <summary>Ham dosya tavanı — bayt (Mehmet: ≤ 2 MB).</summary>
    public int MaksDosyaBayt { get; init; } = 2 * 1024 * 1024;

    /// <summary>CSV bomb kapısı: bir satırda kabul edilen en fazla alan sayısı.</summary>
    public int MaksAlanSayisi { get; init; } = 60;

    /// <summary>Gerekçe bu uzunluğun altındaysa oturum açılmaz (dışa aktarımla simetrik).</summary>
    public int GerekceAsgari { get; init; } = 20;

    /// <summary>Uygulama adımında BİREBİR yazılması gereken onay metni.</summary>
    public string UygulaOnayMetni { get; init; } = "VERİLERİ İÇE AKTARMAYI ONAYLIYORUM";

    /// <summary>Satır listesi ucunun tavanı (TB-19: limit zorunlu).</summary>
    public int SatirListesiTavan { get; init; } = 200;
}

/// <summary>
/// SAF dosya kapısı — veritabanı GÖRMEZ. <see cref="Urun.BelgeKapisi"/> deseni.
///
/// <para>
/// ⚠ CSV'NİN MAGIC BYTE'I YOKTUR. Bu yüzden kapı POZİTİF imza değil, NEGATİF
/// imza + kodlama sıkılığı uygular (Anayasa md.1: "CSV'dir" diye tahmin
/// edilmez, "ikili dosya DEĞİLDİR" diye kanıtlanır):
///   • Bilinen ikili imzalar (PDF · JPEG · PNG · ZIP/XLSX · OLE2/XLS · GZIP)
///     REDDEDİLİR — kullanıcı Excel'i "CSV UTF-8" olarak kaydetmemiş demektir.
///   • NUL baytı içeren içerik REDDEDİLİR (UTF-16 ve her türlü ikili).
///   • İçerik KATI UTF-8 olarak çözülür; geçersiz bayt dizisi REDDEDİLİR.
/// </para>
/// </summary>
public static class IceAktarimKapisi
{
    private static readonly (byte[] Imza, string Ad)[] IkiliImzalar =
    [
        ([0x25, 0x50, 0x44, 0x46, 0x2D], "PDF"),
        ([0xFF, 0xD8, 0xFF], "JPEG"),
        ([0x89, 0x50, 0x4E, 0x47], "PNG"),
        ([0x50, 0x4B, 0x03, 0x04], "ZIP/XLSX"),
        ([0xD0, 0xCF, 0x11, 0xE0], "Excel 97-2003 (.xls)"),
        ([0x1F, 0x8B], "GZIP"),
    ];

    /// <summary>
    /// Base64 gövdeyi çözer, ikili/kodlama kapısından geçirir ve METİN döndürür.
    /// Geçemezse <see cref="BadRequestException"/> — hiçbir oturum açılmaz.
    /// </summary>
    public static string MetneCevir(string base64, IceAktarimAyarlari ayar, out byte[] ham)
    {
        byte[] icerik;
        try { icerik = Convert.FromBase64String(base64 ?? ""); }
        catch (FormatException) { throw new BadRequestException("Dosya içeriği geçerli base64 değil"); }

        if (icerik.Length == 0) throw new BadRequestException("Dosya boş");

        if (icerik.Length > ayar.MaksDosyaBayt)
            throw new BadRequestException(
                $"Dosya en fazla {ayar.MaksDosyaBayt / 1024} KB olabilir " +
                $"(gelen: {icerik.Length / 1024} KB). Dosyayı bölerek aktarın.");

        foreach (var (imza, ad) in IkiliImzalar)
            if (Baslar(icerik, imza))
                throw new BadRequestException(
                    $"Dosya CSV değil, {ad} görünüyor. Excel'de \"Farklı Kaydet → CSV UTF-8\" seçin.");

        if (Array.IndexOf(icerik, (byte)0) >= 0)
            throw new BadRequestException(
                "Dosya ikili veri içeriyor (NUL baytı). Yalnız UTF-8 kodlu CSV kabul edilir.");

        string metin;
        try
        {
            metin = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(icerik);
        }
        catch (DecoderFallbackException)
        {
            throw new BadRequestException(
                "Dosya UTF-8 değil. Excel'de \"Farklı Kaydet → CSV UTF-8\" seçeneğiyle kaydedin.");
        }

        ham = icerik;
        return metin;
    }

    private static bool Baslar(byte[] icerik, byte[] imza)
    {
        if (icerik.Length < imza.Length) return false;
        for (var i = 0; i < imza.Length; i++)
            if (icerik[i] != imza[i]) return false;
        return true;
    }
}
