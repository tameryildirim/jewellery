using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Bilesenler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-5 — KUR KAYNAĞI VE AYAR ADI: TEK ÇEVİRİCİ (UX-S31)
//
// NEDEN VAR: kur kaynağı kodu (ALTINKAYNAK · MANUAL) Kur ekranının geçmiş
// tablosunda ve Vitrin panosunun başlığında HAM basılıyordu; çevirici yalnız
// Kur.razor'un seçicisinde vardı. Aynı kavram iki ekranda iki ad taşımasın
// diye çevirici TEK yere alındı (UX-S31 "aynı kavram tek ad, tek kaynak").
//
// ⚠ Bilinmeyen kod (yeni adaptör) kodun KENDİSİYLE görünür: uydurma ad yok
//   (Anayasa md.1 — bilinmeyen tahmin edilmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ham kur kaynağı / ayar kodunu ekranda okunur ada çevirir.</summary>
public static class KurKaynakAdi
{
    /// <summary>Kur kaynağı kodu → sözlükteki ad.</summary>
    public static string Ad(string? kod, Sozluk t) => kod switch
    {
        "ALTINKAYNAK" => t.Kur.KaynakAltinkaynak,
        "ANKARA" => t.Kur.KaynakAnkara,
        "TRUNCGIL" => t.Kur.KaynakTruncgil,
        "MANUAL" => t.Kur.KaynakManuel,
        _ => kod ?? "",
    };

    /// <summary>
    /// Ayar kodu (K22 · G925) → görünen ad ("22 Ayar" · "925 Gümüş").
    /// Ad TEK kaynaktan gelir: <see cref="Kuyumcu.Core.Domain.Ayarlar"/>.
    /// </summary>
    public static string AyarAdi(string? ayarId) =>
        ayarId is not null && Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId) is { } a
            ? DilEkranSozlugu.AyarAdi(Servisler.Bicim.Dil, a.Id, a.Ad) : ayarId ?? "";

    /// <summary>Maden kodu → etkin dilde ad (TR "Altın", EN "Gold") — tek çevirici.</summary>
    public static string MadenAdi(string maden) =>
        DilEkranSozlugu.MadenAdi(Servisler.Bicim.Dil, maden, Kuyumcu.Core.Domain.Maden.Ad(maden));

    /// <summary>
    /// Sunucunun gönderdiği maden adı (Türkçe) → etkin dilde ad. Kod tanınmıyorsa sunucu adı AYNEN
    /// (ad uydurulmaz); ikisi de yoksa boş.
    /// </summary>
    public static string MadenAdi(string? kod, string? sunucuAdi) =>
        kod is null ? sunucuAdi ?? ""
            : DilEkranSozlugu.MadenAdi(Servisler.Bicim.Dil, kod, sunucuAdi ?? kod);
}
