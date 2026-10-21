namespace Kuyumcu.Core.Domain;

/// <summary>
/// FİYAT KALEMLERİ — TEK KAYNAK (kurucu 21.10: "ürünler birkaç yerde farklı liste geliyor; eski yeni tam liste gelmesi lazım").
/// Kur ekranı, vitrin/pano, ürün kartının kur bağı ve EN adlar bu listeden okunur: kod, ekran sırası, grup ve TR/EN ad
/// TEK yerde. Kodlar kaynaktan bağımsızdır (her sağlayıcı kendi kodunu bu kodlara eşler — KurKaynaklari.cs).
/// Sıra = ekran sırası: has/gram → ayar → sarrafiye (her kalemde YENİ hemen ardından ESKİ) → gümüş → ons.
/// </summary>
public static class FiyatKalemleri
{
    public enum Grup { Has, Ayar, Sarrafiye, Maden }

    /// <param name="Para">Fiyatın para birimi: ONS dünya piyasası kotasyonudur, ons başına DOLAR (kaynaklar TL vermez).</param>
    public sealed record Kalem(string Kod, Grup Grup, string Tr, string En, string Para = "TRY");

    public static readonly IReadOnlyList<Kalem> Tumu =
    [
        new("HAS", Grup.Has, "Has Altın (gram)", "Pure gold (gram)"),
        new("GRAM", Grup.Has, "Gram Altın", "Gold (gram)"),
        new("KULCE", Grup.Has, "Külçe Altın", "Gold bar"),
        new("BILEZIK_22", Grup.Ayar, "22 Ayar Bilezik", "22-carat bracelet"),
        new("HURDA_22", Grup.Ayar, "22 Ayar Hurda", "22-carat scrap"),
        new("AYAR_18", Grup.Ayar, "18 Ayar Altın", "18-carat gold"),
        new("AYAR_14", Grup.Ayar, "14 Ayar Altın", "14-carat gold"),
        new("CEYREK_YENI", Grup.Sarrafiye, "Yeni Çeyrek", "New quarter coin"),
        new("CEYREK_ESKI", Grup.Sarrafiye, "Eski Çeyrek", "Old quarter coin"),
        new("YARIM_YENI", Grup.Sarrafiye, "Yeni Yarım", "New half coin"),
        new("YARIM_ESKI", Grup.Sarrafiye, "Eski Yarım", "Old half coin"),
        new("TAM_YENI", Grup.Sarrafiye, "Yeni Tam (Teklik)", "New full coin"),
        new("TAM_ESKI", Grup.Sarrafiye, "Eski Tam (Teklik)", "Old full coin"),
        new("ATA_YENI", Grup.Sarrafiye, "Yeni Ata Lira", "New Ata coin"),
        new("ATA_ESKI", Grup.Sarrafiye, "Eski Ata Lira", "Old Ata coin"),
        new("GREMSE_YENI", Grup.Sarrafiye, "Yeni Gremse", "New Gremse coin"),
        new("GREMSE_ESKI", Grup.Sarrafiye, "Eski Gremse", "Old Gremse coin"),
        new("BESLI", Grup.Sarrafiye, "Ata Beşli", "Ata five-coin"),
        new("RESAT", Grup.Sarrafiye, "Reşat Altın", "Resat gold coin"),
        new("HAMIT", Grup.Sarrafiye, "Hamit Altın", "Hamit gold coin"),
        new("GUMUS", Grup.Maden, "Gümüş (gram)", "Silver (gram)"),
        new("ONS", Grup.Maden, "ONS Altın", "Gold ounce (ONS)", "USD"),
    ];

    private static readonly IReadOnlyDictionary<string, (int Sira, Kalem Kalem)> Dizin =
        Tumu.Select((k, i) => (k, i)).ToDictionary(x => x.k.Kod, x => (x.i, x.k), StringComparer.Ordinal);

    /// <summary>Kod katalogda mı?</summary>
    public static bool Var(string? kod) => kod is not null && Dizin.ContainsKey(kod);

    /// <summary>Ekran sırası; katalog dışı kod listenin SONUNA (int.MaxValue).</summary>
    public static int Sira(string? kod) => kod is not null && Dizin.TryGetValue(kod, out var d) ? d.Sira : int.MaxValue;

    /// <summary>Fiyatın para birimi (katalog dışı kodda TRY).</summary>
    public static string Para(string? kod) => kod is not null && Dizin.TryGetValue(kod, out var d) ? d.Kalem.Para : "TRY";

    /// <summary>Görünen ad; katalog dışı kodda <paramref name="yedek"/> (yoksa kodun kendisi) AYNEN döner.</summary>
    public static string Ad(string? kod, string dil, string? yedek = null) =>
        kod is not null && Dizin.TryGetValue(kod, out var d) ? (dil == "en" ? d.Kalem.En : d.Kalem.Tr) : yedek ?? kod ?? "";
}
