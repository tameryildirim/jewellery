namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-DIL-BORC-KASA (Y18) — EMANET "BIRAKILDIĞI GİBİ" ÖZETİ (TEK KAYNAK).
//
// Kurucu/denetim: 12,50 g 22 ayar bilezik bırakan müşteri ekranda "11,4500 has"
// görüyordu. Tezgâhtar emaneti bıraktığı gibi görür: "22 ayar · 12,50 g";
// has yalnız ikincildir. Hesap DEĞİŞMEZ: bakiye ve kapılar yine has üzerinden.
//
// Kural (fail-closed, Anayasa md.1 — uydurma yok):
//   • Ayar başına kalan net gram = Σ(gram − taş) (giriş +, iade −, storno ters).
//   • Bir maden kümesinde ayarı bilinmeyen (eski, bu kolondan önceki) satırların
//     has toplamı 0 değilse ya da bir ayarın kalanı eksiye düştüyse (başka ayarla
//     iade) "bırakıldığı gibi" BİLİNMİYOR sayılır → null döner, ekran has'a düşer.
//   • Has toplamı 0 ise (emanet yok) boş liste döner.
// Sunucu (EmanetModulu, CariModulu) ve istemci aynı fonksiyonu çağırır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Defterdeki bir emanet satırının "bırakıldığı gibi" izi.</summary>
public readonly record struct EmanetIz(string? AyarId, decimal? Gram, decimal? Tas, decimal Has);

/// <summary>Kalan emanetin bir ayarı: net gram (brüt − taş) ve has karşılığı.</summary>
public sealed record EmanetAyarSatiri(string AyarId, string AyarAdi, decimal Gram, decimal Has);

public static class EmanetBirakildigi
{
    /// <summary>
    /// Tek bir madenin emanet satırlarından ayar bazında kalan. null = bilinmiyor (has'a düş);
    /// boş liste = kalan emanet yok.
    /// </summary>
    public static IReadOnlyList<EmanetAyarSatiri>? Ozetle(IEnumerable<EmanetIz> satirlar)
    {
        var liste = satirlar.ToList();
        var netHas = Math.Round(liste.Sum(s => s.Has), 6, MidpointRounding.AwayFromZero);
        if (netHas == 0m) return [];

        var ayarsizHas = Math.Round(liste.Where(s => s.AyarId is null || s.Gram is null).Sum(s => s.Has), 6, MidpointRounding.AwayFromZero);
        if (ayarsizHas != 0m) return null;

        var sonuc = new List<EmanetAyarSatiri>();
        foreach (var g in liste.Where(s => s.AyarId is not null && s.Gram is not null).GroupBy(s => s.AyarId!))
        {
            var gram = Math.Round(g.Sum(s => s.Gram!.Value - (s.Tas ?? 0m)), 3, MidpointRounding.AwayFromZero);
            var has = Math.Round(g.Sum(s => s.Has), 6, MidpointRounding.AwayFromZero);
            if (gram == 0m && has == 0m) continue;
            // Başka ayarla iade ya da tutarsız iz: bilinmiyor (uydurulmaz).
            if (gram <= 0m || has <= 0m) return null;
            var ayar = Ayarlar.Bul(g.Key);
            if (ayar is null) return null;
            sonuc.Add(new EmanetAyarSatiri(ayar.Id, ayar.Ad, gram, has));
        }
        return sonuc.Count == 0 ? null : sonuc.OrderByDescending(s => s.Gram).ToList();
    }
}
