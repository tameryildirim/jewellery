using System.Security;

namespace Kuyumcu.Api.Modules.EBelge;

/// <summary>
/// Mihenk e-belge görüntü şablonu (Sablon/mihenk-fatura.xslt, gömülü kaynak). Yapılandırma:
///   EBelge:MihenkSablonu (varsayılan true) — false: entegratörün varsayılan şablonu kullanılır.
///   EBelge:GibLogoAdresi — doluysa GİB logosu resim olarak basılır; boşsa idarenin adı yazıyla.
/// </summary>
public static class EBelgeSablonu
{
    private static readonly Lazy<string> Ham = new(() =>
    {
        using var s = typeof(EBelgeSablonu).Assembly.GetManifestResourceStream("Kuyumcu.Api.EBelge.mihenk-fatura.xslt")
            ?? throw new InvalidOperationException("e-belge şablonu gömülü kaynakta yok");
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    });

    /// <summary>Şablon metni; logo adresi verilirse parametrenin varsayılanına gömülür (entegratör parametre geçirmez).</summary>
    public static string Metin(string? gibLogoAdresi = null) =>
        string.IsNullOrWhiteSpace(gibLogoAdresi) ? Ham.Value
            : Ham.Value.Replace("<xsl:param name=\"gibLogo\" select=\"''\"/>",
                $"<xsl:param name=\"gibLogo\" select=\"'{SecurityElement.Escape(gibLogoAdresi.Trim().Replace("'", ""))}'\"/>");

    public static string? Yapilandirmadan(IConfiguration y) =>
        y.GetValue("EBelge:MihenkSablonu", true) ? Metin(y["EBelge:GibLogoAdresi"]) : null;
}
