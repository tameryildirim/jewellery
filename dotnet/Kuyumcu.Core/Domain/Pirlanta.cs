namespace Kuyumcu.Core.Domain;

/// <summary>Pırlanta 4C sözlükleri — kapalı listeler (serbest metin yok).</summary>
public static class Pirlanta
{
    public static readonly string[] Renkler =
        ["D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"];
    public static readonly string[] Berrakliklar = ["FL", "IF", "VVS1", "VVS2", "VS1", "VS2", "SI1", "SI2", "I1", "I2", "I3"];
    public static readonly string[] Kesimler = ["EXCELLENT", "VERY_GOOD", "GOOD", "FAIR", "POOR"];
    public static readonly string[] SertifikaKurumlari = ["GIA", "HRD", "IGI", "EGL", "DIGER"];

    /// <summary>Etiket/fiş özeti: "0,52 ct · G · VS1 · EX".</summary>
    public static string Ozet(decimal? karat, string? renk, string? berraklik, string? kesim)
    {
        if (karat is null) return "";
        var p = new List<string> { karat.Value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " ct" };
        if (renk is not null) p.Add(renk);
        if (berraklik is not null) p.Add(berraklik);
        if (kesim is not null) p.Add(kesim switch { "EXCELLENT" => "EX", "VERY_GOOD" => "VG", "GOOD" => "G", "FAIR" => "F", "POOR" => "P", _ => kesim });
        return string.Join(" · ", p);
    }
}
