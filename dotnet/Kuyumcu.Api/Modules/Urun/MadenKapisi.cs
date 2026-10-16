using Kuyumcu.Api.Common;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// MADEN KAPISI — paket 20 Faz A (fail-closed).
//
// ⚠ NEDEN VAR: "has gram" bir ALTIN kavramıdır (K20-2). Sistem bugüne kadar
// gümüşün, platinin ve paladyumun has gramını ALTININKİYLE AYNI kolona yazıyor,
// raporlar ikisini AYIRMADAN topluyordu: 100 g gümüş emaneti defterde 100 g
// altın emaneti gibi görünüyordu. Çelik saat ise K18 gibi bir ALTIN ayarıyla
// kaydedilmek zorundaydı — 40 g çelik, 30 g has altına dönüşüyordu.
//
// Bu kapı iki soruyu fail-closed cevaplar:
//   • ÜRÜN: tip ile ayarın madeni tutarlı mı, çelikte fiyat var mı, rodyum
//     işareti anlamlı mı?
//   • DEFTER: has bacağı sıfırdan farklı her satır GEÇERLİ bir maden taşıyor mu?
//
// İkinci soru <see cref="Data.HareketFabrikasi.Ekle"/> içinde — deftere yazan
// TEK yol orası olduğu için maden işaretinin boş kalması imkânsızdır (K20-1).
// denetim.sh bu çağrının yalnız orada olduğunu sayar.
// ═════════════════════════════════════════════════════════════════════════════

public static class MadenKapisi
{
    /// <summary>
    /// Ürün tipinin BEKLEDİĞİ maden. <c>null</c> = tip maden dayatmaz
    /// (PIRLANTA/SAAT/DEGERLI_TAS taşı ya da mekanizması taşıyabilir; kasa
    /// hangi madene monte edildiyse odur).
    /// </summary>
    private static string? TipinMadeni(string? tip) => tip switch
    {
        "GUMUS"    => Maden.Gumus,
        "PLATIN"   => Maden.Platin,
        "PALADYUM" => Maden.Paladyum,
        "CELIK"    => Maden.Celik,
        // Ziynet, külçe, hurda ve sikke Türk kuyumculuğunda ALTIN kalemleridir;
        // gümüş ziynet "GUMUS" tipiyle kaydedilir.
        "ZIYNET" or "KULCE" or "HURDA" or "SIKKE" => Maden.Altin,
        _ => null,
    };

    /// <summary>
    /// Ürün kartı kaydedilirken/güncellenirken maden bütünlüğü (K20-5, K20-6, K20-7).
    /// </summary>
    /// <param name="tip">Ürün tipi (ZIYNET, GUMUS, CELIK…).</param>
    /// <param name="ayarId">Ürünün ayarı.</param>
    /// <param name="rodyumKaplama">Rodyum kaplama işareti.</param>
    /// <param name="satisFiyati">Ürün kartındaki sabit satış fiyatı (varsa).</param>
    public static void UrunDogrula(string? tip, string? ayarId, bool rodyumKaplama, decimal? satisFiyati)
    {
        var ayar = Ayarlar.Bul(ayarId ?? string.Empty)
            ?? throw new BadRequestException(
                $"Geçersiz ayar: {ayarId}. Kabul edilen değerler: {string.Join(", ", Ayarlar.GecerliIdler)}");

        // (b) tip ↔ maden tutarlılığı
        var beklenen = TipinMadeni(tip);
        if (beklenen is not null && ayar.Maden != beklenen)
            throw new BadRequestException(
                $"Ürün grubu '{tip}' {Maden.Ad(beklenen).ToLowerInvariant()} bekler, " +
                $"seçilen ayar ({ayar.Id} — {ayar.Ad}) {Maden.Ad(ayar.Maden).ToLowerInvariant()} ayarıdır. " +
                "Ürün grubunu ya da ayarı düzeltin: farklı madenlerin gramları toplanamaz.");

        // (c) kıymetsiz maden has×kur ile fiyatlanamaz → sabit fiyat ZORUNLU
        if (!ayar.Kiymetli && satisFiyati is null or <= 0m)
            throw new BadRequestException(
                $"{Maden.Ad(ayar.Maden)} ürününde satış fiyatı zorunludur: bu maden kıymetli " +
                "değildir, has gramı hesaplanmaz ve fiyatı has kurundan türetilemez.");

        // (d) rodyum kaplama yalnız altın/platin/paladyumda anlamlıdır (K20-7)
        if (rodyumKaplama && !Ayarlar_RodyumKaplanabilir(ayar.Maden))
            throw new BadRequestException(
                $"Rodyum kaplama işareti {Maden.Ad(ayar.Maden).ToLowerInvariant()} üründe kullanılamaz. " +
                "Rodyum beyaz altın, platin ve paladyum yüzeye vurulan bir kaplamadır.");
    }

    private static bool Ayarlar_RodyumKaplanabilir(string maden) =>
        Array.IndexOf(Maden.RodyumKaplanabilir, maden) >= 0;

    /// <summary>
    /// DEFTER KAPISI (K20-1) — <see cref="Data.HareketFabrikasi.Ekle"/> içinden
    /// ÇAĞRILIR, başka hiçbir yerden çağrılmaz (denetim.sh sayar).
    ///
    /// <para>
    /// Has bacağı sıfırdan farklı her satır geçerli bir maden taşımak
    /// ZORUNDADIR. "Yazan doldursun" bir âdet olarak bırakılsaydı, eklenen
    /// sonraki yazma noktası işareti unutur ve o gram sessizce ALTIN toplamına
    /// karışırdı. TL-only satırlarda (tahsilat, işçilik) maden bacağı yoktur;
    /// oradaki varsayılan 'ALTIN' bir iddia değil, doldurulmamış kolondur.
    /// </para>
    /// </summary>
    public static void HareketDogrula(Data.Hareket hareket)
    {
        if (hareket.HasGrami == 0m) return;

        if (string.IsNullOrWhiteSpace(hareket.Maden))
            throw new InvalidOperationException(
                $"Has bacağı olan hareket madensiz yazılamaz (tür: {hareket.Tur}, " +
                $"has: {hareket.HasGrami}). Maden işareti olmadan bu gram hangi " +
                "madene ait olduğu bilinmeden ALTIN toplamına karışırdı (K20-1).");

        if (Array.IndexOf(Maden.Hepsi, hareket.Maden) < 0)
            throw new InvalidOperationException(
                $"Bilinmeyen maden: '{hareket.Maden}' (tür: {hareket.Tur}). " +
                $"Kabul edilenler: {string.Join(", ", Maden.Hepsi)}.");
    }
}
