namespace Kuyumcu.Core.Domain;

/// <summary>
/// ÜRÜN GRUBU (tip) listesinin TEK KAYNAĞI (UX-S31, paket 20).
///
/// <para>
/// ⚠ NEDEN VAR: liste API'de (<c>Sabitler.UrunTipleri</c>), DTO deseninde ve
/// ürün ekranında ÜÇ kez yazılmıştı ve içerikleri FARKLIYDI: API PLATIN,
/// PALADYUM, CELIK ve DEGERLI_TAS gruplarını kabul ediyordu, ekrandaki dizi
/// bunları HİÇ göstermiyordu — kuyumcu vitrinindeki platin alyansı sisteme
/// ekran üzerinden giremiyordu. Artık ekran ve API aynı diziyi okur.
/// </para>
/// </summary>
public static class UrunTipleri
{
    public const string Ziynet = "ZIYNET";
    public const string Kulce = "KULCE";
    public const string Hurda = "HURDA";
    public const string Sikke = "SIKKE";
    public const string Pirlanta = "PIRLANTA";
    public const string Saat = "SAAT";
    public const string Gumus = "GUMUS";
    public const string Platin = "PLATIN";
    public const string Paladyum = "PALADYUM";
    public const string Celik = "CELIK";
    public const string DegerliTas = "DEGERLI_TAS";

    public static readonly string[] Hepsi =
        [Ziynet, Kulce, Hurda, Sikke, Pirlanta, Saat, Gumus, Platin, Paladyum, Celik, DegerliTas];

    /// <summary>DTO doğrulaması için desen — dizinin kendisinden üretilir.</summary>
    public static readonly string Desen = string.Join('|', Hepsi);
}
