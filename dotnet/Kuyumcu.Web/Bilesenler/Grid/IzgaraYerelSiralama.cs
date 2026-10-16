using System.Globalization;

namespace Kuyumcu.Web.Bilesenler.Grid;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-A — TAM LİSTENİN YEREL SIRALAMASI (en küçük geriye uyumlu ekleme).
//
// Ortak ızgara sıralamayı KENDİSİ yapmaz; SiralamaDegisti ile sayfaya haber verir
// (envanter § G1: "sayfalı listede istemci sıralaması yanıltır"). Para ekranlarının
// çoğu listesi TAMDIR (sunucu sınırına değmez: kur ayar fiyatları, pariteler, POS
// cihazları, kasa raporu satırları…) — tam listede yerel sıralama yanıltmaz.
// Bu yardımcı yalnız o durumda kullanılır: sayfa, liste sınıra DEĞDİYSE kolonlara
// SunucuSiralamaAdi VERMEZ (başlıkta sıralama düğmesi çıkmaz — yanıltıcı sıra yok).
// Sıralama anahtarı kolonun Deger'idir (görünen hücre şablonundan bağımsız, tek kaynak).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tam (kırpılmamış) listeyi kolonun <see cref="IzgaraKolonu{T}.Deger"/>'ine göre sıralar.</summary>
public static class IzgaraYerelSiralama
{
    private static readonly CompareInfo Tr = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    /// <summary>
    /// Sıralama yoksa ya da kolonun Deger'i yoksa liste AYNEN döner. Eşitlerde ilk sıra korunur
    /// (OrderBy kararlıdır). Boş değer (null) artanda sona, azalanda başa gider.
    /// </summary>
    public static IReadOnlyList<T> Uygula<T>(IReadOnlyList<T> ogeler, IzgaraSiralama? siralama, IReadOnlyList<IzgaraKolonu<T>> kolonlar)
    {
        if (siralama is null || ogeler.Count < 2) return ogeler;
        var kolon = kolonlar.FirstOrDefault(k => k.Anahtar == siralama.KolonAnahtari);
        if (kolon?.Deger is not { } deger) return ogeler;
        var karsilastirici = Comparer<object?>.Create(Karsilastir);
        return siralama.Azalan
            ? [.. ogeler.OrderByDescending(deger, karsilastirici)]
            : [.. ogeler.OrderBy(deger, karsilastirici)];
    }

    /// <summary>Tür bilen karşılaştırma: sayı sayıyla, tarih tarihle, metin tr-TR kurallarıyla.</summary>
    internal static int Karsilastir(object? a, object? b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return 1;
        if (b is null) return -1;
        if (Sayi(a) is { } x && Sayi(b) is { } y) return x.CompareTo(y);
        if (a is DateTime ta && b is DateTime tb) return ta.CompareTo(tb);
        if (a is DateOnly da && b is DateOnly db) return da.CompareTo(db);
        return Tr.Compare(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture),
            CompareOptions.IgnoreCase);
    }

    private static decimal? Sayi(object v) => v switch
    {
        decimal d => d,
        int i => i,
        long l => l,
        double db => (decimal)db,
        _ => null,
    };
}
