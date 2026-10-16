using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET P-37b — karşılama ekranının "pencere pencere" kutucuk kataloğu.
/// ⚠ TEK KAYNAK (UX-S31): kutucuk kodu, başlığı ve sırası YALNIZ burada tanımlıdır;
///   ekran da, testler de, kullanıcı tercihi de bu listeden beslenir.
/// Kod dizgeleri KULLANICI TERCİHİNE yazılır → asla değiştirilmez, yalnız eklenir.
/// </summary>
public sealed record PanelKutucukTanimi(string Kod, Func<Sozluk, string> Baslik);

public static class PanelKutucuklari
{
    public const string GunOzeti    = "gun-ozeti";
    public const string Kur         = "kur";
    public const string Bekleyen    = "bekleyen";
    public const string Kasa        = "kasa";
    public const string SonIslemler = "son-islemler";
    public const string HizliEylem  = "hizli-eylem";
    public const string Grafik      = "grafik";

    /// <summary>
    /// VARSAYILAN SIRA = sabahın beş sorusunun cevabı ÖNCE (kaydırmadan görünür):
    /// bugün ne sattım · altın kaç · kim borçlu / bekleyen iş · kasada ne var.
    /// Grafik ve son işlemler sonra gelir.
    /// </summary>
    public static readonly List<PanelKutucukTanimi> Tumu =
    [
        new(GunOzeti,    t => t.Panel.BugunOzeti),
        new(Kur,         t => t.Panel.Kur24k),
        new(Bekleyen,    t => t.Panel.BekleyenIsler),
        new(Kasa,        t => t.Panel.KasaOzeti),
        new(HizliEylem,  t => t.Panel.HizliEylemler),
        new(SonIslemler, t => t.Panel.SonIslemler),
        new(Grafik,      t => t.Panel.Son7Gun),
    ];
}
