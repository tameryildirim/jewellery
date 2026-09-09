using System.Globalization;

namespace Kuyumcu.Web.Servisler;

/// <summary>tlFormat / hasFormat / gramFormat karşılıkları.</summary>
public static class Bicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Tl(decimal n) => n.ToString("C2", Tr);
    public static string Has(decimal n) => $"{n.ToString("F4", CultureInfo.InvariantCulture)} has";
    public static string Gram(decimal n) => $"{n.ToString("F3", CultureInfo.InvariantCulture)} g";
    public static string Tarih(DateTime t, string locale) =>
        t.ToLocalTime().ToString("d", CultureInfo.GetCultureInfo(locale));
}
