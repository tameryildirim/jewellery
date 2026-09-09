namespace Kuyumcu.Core.Domain;

/// <summary>Altın/gümüş ayarı — milyem: 1000 üzerinden saflık.</summary>
public sealed record Ayar(string Id, string Ad, decimal Milyem);

/// <summary>
/// Ayar tanımları — TypeScript @kuyum/core ile birebir aynı değerler.
/// Has gram = (gramAğırlık − taşAğırlık) × milyem / 1000
/// </summary>
public static class Ayarlar
{
    public static readonly IReadOnlyDictionary<string, Ayar> Altin = new Dictionary<string, Ayar>
    {
        ["K8"]  = new("K8",  "8 Ayar",  333m),
        ["K14"] = new("K14", "14 Ayar", 585m),
        ["K18"] = new("K18", "18 Ayar", 750m),
        ["K21"] = new("K21", "21 Ayar", 875m),
        ["K22"] = new("K22", "22 Ayar", 916m),
        ["K24"] = new("K24", "24 Ayar", 1000m),
    };

    public static readonly IReadOnlyDictionary<string, Ayar> Gumus = new Dictionary<string, Ayar>
    {
        ["G800"] = new("G800", "800 Gümüş", 800m),
        ["G925"] = new("G925", "925 Gümüş", 925m),
        ["G950"] = new("G950", "950 Gümüş", 950m),
    };

    /// <summary>Altın + gümüş birleşik arama tablosu.</summary>
    public static readonly IReadOnlyDictionary<string, Ayar> Tumu =
        Altin.Concat(Gumus).ToDictionary(kv => kv.Key, kv => kv.Value);

    public static Ayar? Bul(string ayarId) => Tumu.TryGetValue(ayarId, out var a) ? a : null;
}

/// <summary>Has gram hesapları.</summary>
public static class HasHesap
{
    /// <summary>Net gramı (taş düşülmüş) 24K saf altın karşılığına çevirir.</summary>
    public static decimal ToHasGram(decimal netGram, Ayar ayar) =>
        Math.Round(netGram * ayar.Milyem / 1000m, 6);

    /// <summary>Brüt gram + taş ağırlığından has gram.</summary>
    public static decimal HasGramHesapla(decimal gramAgirlik, decimal tasAgirlik, string ayarId)
    {
        var ayar = Bul(ayarId) ?? throw new ArgumentException($"Geçersiz ayar: {ayarId}", nameof(ayarId));
        return ToHasGram(gramAgirlik - tasAgirlik, ayar);
    }

    private static Ayar? Bul(string ayarId) => Ayarlar.Bul(ayarId);
}
