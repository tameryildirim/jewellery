using System.Globalization;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET YASAL-1 — yasal sınır değerlerinin ekran biçimi (tek yer: sayfa + Panel bandı).
/// Para <see cref="Bicim.Tl"/>, oran <see cref="Bicim.Yuzde"/>; süre birimi sözlükten.
/// Tarih sunucudan "yyyy-MM-dd" (yerel gün) gelir, dilin uzun biçimiyle yazılır.
/// </summary>
public static class YasalBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Deger(decimal? sayi, string? metin, string birim, YasalSinirMetinleri m) =>
        birim switch
        {
            "TL" when sayi is { } n => Bicim.Tl(n),
            "ORAN" when sayi is { } n => Bicim.Yuzde(n * 100m),
            "GUN" when sayi is { } n => $"{n.ToString("0", Tr)} {m.BirimGun}",
            "IS_GUNU" when sayi is { } n => $"{n.ToString("0", Tr)} {m.BirimIsGunu}",
            "YIL" when sayi is { } n => $"{n.ToString("0", Tr)} {m.BirimYil}",
            // Ayın günü bir SÜRE değildir ("26 gün" yanlış) — "İzleyen ayın 26. günü".
            "AYIN_GUNU" when sayi is { } n => string.Format(m.AyinGunu, n.ToString("0", Tr)),
            _ => string.IsNullOrWhiteSpace(metin) ? m.DegerYok : metin!,
        };

    /// <summary>
    /// YASAL-1 tur 3: eşik ifadesi — sistemin iç karşılaştırma değeri (ör. ≥ 2.000.000,01)
    /// kullanıcıya KURUŞLA gösterilmez: sunucu "USTU"/"VE_USTU" + tam tutar döner,
    /// ekran "X ₺ üzeri" / "X ₺ ve üzeri" yazar (rakam sunucudan). Eşik değilse <see cref="Deger"/>.
    /// </summary>
    public static string Esikli(decimal? sayi, string? metin, string birim, string? esikTuru, decimal? esikTutari,
        YasalSinirMetinleri m) =>
        esikTuru switch
        {
            "USTU" when esikTutari is { } t => string.Format(m.EsikUstu, Bicim.Tl(t)),
            "VE_USTU" when esikTutari is { } t => string.Format(m.EsikVeUstu, Bicim.Tl(t)),
            _ => Deger(sayi, metin, birim, m),
        };

    /// <summary>Bir satırın ekran değeri (eşik dili dahil).</summary>
    public static string SatirDegeri(Kuyumcu.Contracts.Sayfalar.YasalSinirlar.YasalSatirVeri s, YasalSinirMetinleri m) =>
        Esikli(s.DegerSayi, s.DegerMetin, s.Birim, s.EsikTuru, s.EsikTutari, m);

    /// <summary>"yyyy-MM-dd" → DateOnly (geçersizse null).</summary>
    public static DateOnly? Gun(string? yyyyMmGg) =>
        DateOnly.TryParseExact(yyyyMmGg, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)
            ? g : null;

    public static string Tarih(string? yyyyMmGg, string dil) =>
        DateOnly.TryParseExact(yyyyMmGg, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)
            ? g.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo(dil == "en" ? "en-GB" : "tr-TR"))
            : "";

    /// <summary>
    /// Geçmiş dönemin SON GÜNÜ: sunucudaki bitiş HARİÇTİR (o gün yeni değer başlar),
    /// ekrana dahil olan son gün (bitiş − 1) yazılır — "1 Ocak 2016 – 29 Kasım 2024".
    /// </summary>
    public static string SonGun(string? bitisHaric, string dil) =>
        DateOnly.TryParseExact(bitisHaric, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)
            ? Tarih(g.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dil)
            : "";

    /// <summary>Sade ad: sözlükten (TR+EN); anahtar sözlükte yoksa sunucunun sade açıklaması.</summary>
    public static string Ad(string anahtar, string? sunucuAciklamasi, YasalSinirMetinleri m) =>
        m.Adlar.TryGetValue(anahtar, out var ad) ? ad : sunucuAciklamasi ?? "";

    /// <summary>id eki: TEVSIK_HADDI_TL → tevsik-haddi-tl.</summary>
    public static string Slug(string anahtar) => anahtar.ToLowerInvariant().Replace('_', '-');
}
