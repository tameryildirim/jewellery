using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Routing;

namespace Kuyumcu.Web.Layout;

/// <summary>
/// PAKET YILDIZ-2 — "Yıldız" kabuğunun karar ve veri noktası (tek yer).
///
/// <para>
/// ⚠ TEK KABUK (tema temizliği, kurucu 17.10): Yıldız tek şemadır; eski "Mi()"
/// şema sorusu ve kenar çubuklu kabuk (KenarCubugu) kaldırıldı. AnaYerlesim
/// her oturumlu ekranı üst menü kabuğuyla çizer.
/// </para>
/// </summary>
public static class YildizKabuk
{
    /// <summary>Üst menüdeki beş kelime ve hedefleri (tuval Bugun4 &lt;nav&gt;).</summary>
    public sealed record UstMenuOgesi(string Yol, string Kimlik, bool TamEslesme, Func<string, string> Ad);

    /// <summary>Sıra = ekrandaki sıra. Son öğe her zaman "Tüm ekranlar"dır (yetki kapısı yok).</summary>
    public static readonly IReadOnlyList<UstMenuOgesi> UstMenu =
    [
        new("/", "bugun", true, YildizSozlugu.Bugun),
        new("/islemler", "tezgah", false, YildizSozlugu.Tezgah),
        new("/cariler", "musteriler", false, YildizSozlugu.Musteriler),
        new("/kasalar", "kasa", false, YildizSozlugu.Kasa),
        new(MenuTanimi.TumEkranlarYolu, "tum-ekranlar", true, YildizSozlugu.TumEkranlar),
    ];

    /// <summary>
    /// Öğe bu kullanıcıya görünür mü? "Tüm ekranlar" her zaman; öteki dördü
    /// sol menüyle AYNI kaynaktan (<see cref="MenuTanimi.YetkiliMi"/>) süzülür.
    /// </summary>
    public static bool Gorunur(UstMenuOgesi oge, Func<string, bool> erisebilir, string? rol)
    {
        if (oge.Yol == MenuTanimi.TumEkranlarYolu) return true;
        var m = MenuTanimi.Ogeler.FirstOrDefault(o => o.Yol == oge.Yol);
        return m is null || MenuTanimi.YetkiliMi(m, erisebilir, rol);
    }

    /// <summary>Konum bu öğenin ekranı mı (altı çizili öğe)?</summary>
    public static bool Aktif(UstMenuOgesi oge, string mutlakYol)
    {
        var y = mutlakYol.Length > 1 ? mutlakYol.TrimEnd('/') : mutlakYol;
        if (oge.TamEslesme) return y == oge.Yol;
        return y == oge.Yol || y.StartsWith(oge.Yol + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// PAKET YILDIZ-KATMAN (5A kalanı "üst menüde seçili öğe yok"): tuvalde HER ekranın üst menüsünde
    /// bir sözcük seçilidir ve seçim ekranın MENÜ GRUBUNDAN gelir (Y_*.dc.html <c>a.on</c> — ölçüldü:
    /// Günlük iş → Bugün · Müşteri ve stok → Müşteriler · Para ve banka → Kasa · Belge ve uyum,
    /// Raporlar, Yönetim → Tüm ekranlar). Önce doğrudan eşleşme (<see cref="Aktif"/>: Tezgâh kendi
    /// sözcüğüdür), yoksa en uzun önekle eşleşen menü öğesinin grubu; hiçbiri değilse "Tüm ekranlar"
    /// (bütün ekranlar oradadır). Seçilen sözcük kullanıcıya görünmüyorsa (yetki) yine "Tüm ekranlar".
    /// </summary>
    public static string SeciliKimlik(string mutlakYol, Func<UstMenuOgesi, bool> gorunur)
    {
        var y = mutlakYol.Length > 1 ? mutlakYol.TrimEnd('/') : mutlakYol;
        var dogrudan = UstMenu.FirstOrDefault(o => o.Yol != MenuTanimi.TumEkranlarYolu && Aktif(o, y));
        var secilen = dogrudan ?? GrupOgesi(y);
        var tum = UstMenu[^1];
        return secilen is not null && gorunur(secilen) ? secilen.Kimlik : tum.Kimlik;
    }

    /// <summary>
    /// PAKET YILDIZ-KATMAN (denetim Y17/O16): kendi Yıldız yüzü olmayan ekranın başlık boyu — tuvalin
    /// kendisi (Y_*.dc.html h1): özet kalıbı (Kâr-zarar, Satış analizi, Uyum) 40 px / 300, liste ve
    /// form kalıbı 64 px / 200. Ölçüldü; O16'daki "40 px olanlar 64 olsun" tuvale aykırı olduğu için uygulanmaz.
    /// </summary>
    public static int BaslikBoyu(string mutlakYol) =>
        mutlakYol.TrimEnd('/') is "/uyum" or "/raporlar/kar-zarar" or "/raporlar/satis-analizi" ? 40 : 64;

    private static UstMenuOgesi? GrupOgesi(string y)
    {
        var m = MenuTanimi.Ogeler
            .Where(o => o.Yol != "/" && (y == o.Yol || y.StartsWith(o.Yol + "/", StringComparison.Ordinal)))
            .OrderByDescending(o => o.Yol.Length)
            .FirstOrDefault();
        if (m is null) return null;
        var hedef = m.Grup switch
        {
            MenuGrubu.Gunluk => "bugun",
            MenuGrubu.MusteriStok => "musteriler",
            MenuGrubu.ParaBanka => "kasa",
            _ => "tum-ekranlar",
        };
        return UstMenu.First(o => o.Kimlik == hedef);
    }
}
