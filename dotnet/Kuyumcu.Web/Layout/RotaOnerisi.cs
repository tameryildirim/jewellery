using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Layout;

// ═════════════════════════════════════════════════════════════════════════════
// BULUNAMAYAN ROTA İÇİN ÖNERİ — paket P-40.
//
// NEDEN SAF SINIF: tasarım denetçisi `/vade-takvimi`yi 4 genişlikte de "boş"
// ölçtü. Kök neden rota kaydı ya da yetki değil, ROTANIN HİÇ OLMAMASIYDI:
// Router'ın <NotFound> bloğu tek satır ("Sayfa bulunamadı.") çiziyordu —
// başlıksız, adressiz, çıkışsız. Kullanıcı ekranın bozuk mu, yetkisinin mi
// yetmediğini, yoksa adresi mi yanlış yazdığını anlayamıyordu (UX-S28).
//
// Öneri üretimi burada SAF tutulur ki bUnit'siz, hızlı birim testiyle
// sınanabilsin; bileşen yalnız çizer.
//
// ⚠ BU BİR YETKİ KAPISI DEĞİLDİR: öneri listesi MenuTanimi'nden gelir ve
//   yalnız ADRES önerir. Kullanıcı öneriye tıklayınca son sözü yine sunucu
//   söyler (Mehmet K8).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bulunamayan bir adres için "bunu mu aradınız" bağlantısı.</summary>
/// <param name="Yol">Menüdeki rota (baştaki <c>/</c> ile).</param>
/// <param name="Etiket">Sözlükten çözülmüş görünen ad.</param>
public sealed record RotaOnerisiOgesi(string Yol, string Etiket);

public static class RotaOnerisi
{
    /// <summary>Öneri sayısı tavanı — uzun liste "bulunamadı"yı gizlerdi.</summary>
    public const int EnCokOneri = 3;

    /// <summary>Kısa parçalar ("pos", "kur") gürültü yapmasın diye eşleşme eşiği.</summary>
    private const int EnAzParcaUzunlugu = 4;

    /// <summary>
    /// Bulunamayan <paramref name="istenenYol"/> adresine en çok benzeyen menü
    /// rotalarını döndürür. Eşleşme yoksa BOŞ liste döner — uydurma öneri
    /// yazılmaz (Anayasa md.1).
    /// </summary>
    public static IReadOnlyList<RotaOnerisiOgesi> Oner(string? istenenYol, Sozluk sozluk)
    {
        var istenen = Parcala(istenenYol);
        if (istenen.Count == 0) return [];

        var sonuc = new List<(int Puan, RotaOnerisiOgesi Oge)>();
        foreach (var oge in MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok))
        {
            var etiket = oge.Etiket(sozluk);
            var aday = Parcala(oge.Yol).Concat(Parcala(etiket)).ToList();
            var puan = istenen.Count(i => aday.Any(a => Benzer(i, a)));
            if (puan > 0) sonuc.Add((puan, new RotaOnerisiOgesi(oge.Yol, etiket)));
        }

        return sonuc
            .OrderByDescending(s => s.Puan)
            .ThenBy(s => s.Oge.Yol, StringComparer.Ordinal)
            .Select(s => s.Oge)
            .Distinct()
            .Take(EnCokOneri)
            .ToList();
    }

    /// <summary>Adresi/etiketi katlanmış parçalara ayırır ("/vade-takvimi" → vade, takvimi).</summary>
    private static List<string> Parcala(string? metin) =>
        string.IsNullOrWhiteSpace(metin)
            ? []
            : AramaMetni.Katla(metin)
                .Split(new[] { '/', '-', ' ', '\t', '_', '?', '&', '=' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= EnAzParcaUzunlugu)
                .Distinct()
                .ToList();

    /// <summary>
    /// İki parça aynı kökten mi? Türkçe ek yüzünden "takvim" ile "takvimi"
    /// birebir eşleşmez; önek karşılaştırması kullanılır.
    /// </summary>
    private static bool Benzer(string a, string b) =>
        a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal);
}
