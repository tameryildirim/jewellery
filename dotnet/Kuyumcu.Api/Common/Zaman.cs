namespace Kuyumcu.Api.Common;

/// <summary>
/// İşletme saat dilimi (Europe/Istanbul). Kayıtlar UTC tutulur; raporların
/// gün/ay sınırı işletmenin yerel gününe göre hesaplanır — aksi halde
/// İstanbul'da gece 00:00–03:00 arasındaki ciro "yarına" düşer (K5).
/// </summary>
public static class Zaman
{
    public static TimeZoneInfo Isletme { get; } = SaatDilimiBul();

    private static TimeZoneInfo SaatDilimiBul()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Türkiye 2016'dan beri kalıcı UTC+3 (yaz saati uygulaması yok)
        return TimeZoneInfo.CreateCustomTimeZone(
            "Europe/Istanbul", TimeSpan.FromHours(3), "Europe/Istanbul", "Europe/Istanbul");
    }

    /// <summary>Yerel günün 00:00'ının UTC karşılığı.</summary>
    public static DateTime YerelGunBasiUtc(DateOnly gun) =>
        TimeZoneInfo.ConvertTimeToUtc(
            gun.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Isletme);

    /// <summary>Yerel gün [başlangıç, bitiş) UTC aralığı.</summary>
    public static (DateTime Bas, DateTime Bit) YerelGunAraligiUtc(DateOnly gun) =>
        (YerelGunBasiUtc(gun), YerelGunBasiUtc(gun.AddDays(1)));

    /// <summary>Yerel ay [başlangıç, bitiş) UTC aralığı. Ay taşması korunur (ay=13 → ertesi yıl Ocak).</summary>
    public static (DateTime Bas, DateTime Bit) YerelAyAraligiUtc(int yil, int ay)
    {
        var ilkGun = new DateOnly(yil, 1, 1).AddMonths(ay - 1);
        return (YerelGunBasiUtc(ilkGun), YerelGunBasiUtc(ilkGun.AddMonths(1)));
    }

    /// <summary>UTC anı işletme saatine çevirir.</summary>
    public static DateTime UtcdenYerele(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc), Isletme);

    /// <summary>Yerel günün SON anının UTC karşılığı (ertesi günün başından 1 tik geri).</summary>
    public static DateTime YerelGunSonuUtc(DateOnly gun) =>
        YerelGunBasiUtc(gun.AddDays(1)).AddTicks(-1);

    /// <summary>İşletme saatine göre bugün.</summary>
    public static DateOnly YerelBugun() => YerelBugun(DateTime.UtcNow);

    /// <summary>Verilen UTC anında işletme saatine göre gün (30.09 22:30 UTC → 01.10).</summary>
    public static DateOnly YerelBugun(DateTime utc) => DateOnly.FromDateTime(UtcdenYerele(utc));
}
