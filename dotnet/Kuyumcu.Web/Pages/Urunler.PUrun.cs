using System.Text.RegularExpressions;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-URUN (ekran denetimi 10.10 · K6, Y19, O13)
//
// K6 — yeni ürün formu AYAR'ı "18 Ayar" dolu açıyordu; "22 Ayar Halka Küpe" 18 ayar
//      fiyatıyla kaydedildi. Artık ayarın VARSAYILANI YOK (fail-closed): seçilmeden
//      Kaydet kilitli + sebebi yazılı; ad ile seçim çelişirse uyarı (kaydı engellemez).
//      Maden değişince ayar o madenin İLK ayarına ATLAMAZ — tek ayarlı maden (çelik)
//      hariç boşalır (UX-S22: tek seçenek varsa kendiliğinden konur).
// O13 — Yıldız listesi AYAR sütunu milyem (916/750) basıyordu, form "22 Ayar" diyor;
//      liste de aynı dili konuşur: "22" / "18" (gümüş/platinde ad zaten milyemle
//      başlar: "925").
// ═════════════════════════════════════════════════════════════════════════════
public partial class Urunler
{
    private PUrunMetinleri PU => PUrunSozlugu.Al(Dil.Dil);

    // "22 Ayar", "14 ayar", "24K", "18 k", "22 krt", "21 ayr" — yalnız altın ayarları.
    // "karat"/"ct" BİLEREK yok: pırlantada karat ağırlıktır ("0,50 karat"), ayar değil.
    private static readonly Regex AdAyarDeseni = new(
        @"(?<![\d.,])(8|14|18|21|22|24)\s*(?:ayar|ayr|krt|kt|k)(?![\p{L}\d])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Ürün adında yazılı altın ayarı (ör. "22 Ayar Halka Küpe" → "K22"). Yoksa ya da
    /// adda birbirinden farklı iki ayar geçiyorsa null (tahmin edilmez).
    /// </summary>
    public static string? AdindakiAyar(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return null;
        var bulunan = AdAyarDeseni.Matches(ad).Select(m => "K" + m.Groups[1].Value).Distinct().ToList();
        return bulunan.Count == 1 ? bulunan[0] : null;
    }

    /// <summary>
    /// Ad ile seçilen ayar çelişiyorsa uyarı metni; seçim yoksa ya da adda ayar yoksa null.
    /// Kaydı ENGELLEMEZ — yalnız görünür kılar.
    /// </summary>
    public static string? AdAyarCelismesi(string? ad, string? seciliAyar, string dil)
    {
        if (string.IsNullOrEmpty(seciliAyar)) return null;
        if (AdindakiAyar(ad) is not { } addaki || addaki == seciliAyar) return null;
        return string.Format(PUrunSozlugu.Al(dil).AdAyarCelisiyor,
            Kuyumcu.Web.Bilesenler.KurKaynakAdi.AyarAdi(addaki),
            Kuyumcu.Web.Bilesenler.KurKaynakAdi.AyarAdi(seciliAyar));
    }

    private string? YeniAdAyarUyarisi => AdAyarCelismesi(yeniAd, yeniAyar, Dil.Dil);
    private string? DuzAdAyarUyarisi => AdAyarCelismesi(dAd, dAyar, Dil.Dil);

    /// <summary>
    /// Yeni ürün formunda maden değişince ayar: seçili ayar o madende varsa korunur;
    /// madenin TEK ayarı varsa o konur (UX-S22); değilse BOŞ kalır — ilk ayara atlamaz (K6).
    /// </summary>
    public static string MadeninAyariVarsayilansiz(string ayar, string maden)
    {
        var ayarlar = Kuyumcu.Core.Domain.Ayarlar.MadeninAyarlari(maden).ToList();
        if (ayarlar.Any(a => a.Id == ayar)) return ayar;
        return ayarlar.Count == 1 ? ayarlar[0].Id : "";
    }

    /// <summary>
    /// O13 — Yıldız listesi AYAR sütunu: formla aynı dil, kısa biçim ("22 Ayar" → "22",
    /// "925 Gümüş" → "925"). Adı sayıyla başlamayan (çelik) ya da bilinmeyen ayarda ad/kod.
    /// </summary>
    public static string YildizAyarKisa(string ayarId)
    {
        var ad = Kuyumcu.Web.Bilesenler.KurKaynakAdi.AyarAdi(ayarId);
        var ilk = ad.Split(' ', 2)[0];
        return ilk.Length > 0 && ilk.All(char.IsDigit) ? ilk : ad;
    }
}
