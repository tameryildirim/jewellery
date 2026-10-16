namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5b — SATICI PERSONEL ("Satan") · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin ayrı dosyada).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record.
// ⚠ Etiket ≤ 3 kelime, jargon yok (PLAN § 5B). Rol kodu (SAHIP/MUDUR) ekrana
//   basılmaz; "işletme sahibi ya da müdür" diye yazılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satış ekranının "Satan" satırı / seçicisi metinleri.</summary>
public sealed record FisSaticiMetinleri(
    string Satan,
    string SatanIpucu,
    string KendinizIpucu,
    string YuklemeHatasi,
    string Bilinmiyor,
    string DahaFazlasiVar);

/// <summary>Satıcı personel metinleri — TR + EN.</summary>
public static class FisSaticiSozlugu
{
    public static readonly FisSaticiMetinleri Tr = new(
        Satan: "Satan",
        SatanIpucu: "Satışı yapan kişi. Kaydı giren siz olsanız da satan başkası olabilir.",
        KendinizIpucu: "Satış sizin adınıza yazılır. Başkasını yalnız işletme sahibi ya da müdür seçebilir.",
        YuklemeHatasi: "Satıcı listesi alınamadı; satış sizin adınıza yazılır: {0}",
        Bilinmiyor: "Siz",
        DahaFazlasiVar: "Liste uzun; yalnız ilk kişiler gösteriliyor. Aradığınız yoksa kullanıcılar ekranından bakın.");

    public static readonly FisSaticiMetinleri En = new(
        Satan: "Sold by",
        SatanIpucu: "The person who made the sale. You may enter it for someone else.",
        KendinizIpucu: "The sale is recorded in your name. Only the owner or a manager can pick someone else.",
        YuklemeHatasi: "Seller list could not be loaded; the sale is recorded in your name: {0}",
        Bilinmiyor: "You",
        DahaFazlasiVar: "The list is long; only the first people are shown. Check the users screen if someone is missing.");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static FisSaticiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
