namespace Kuyumcu.Web.Bilesenler;

/// <summary>M7a — GET /bankalar/karar-bekleyen satırı.</summary>
public sealed record GelenOdemeSatiri(
    string Id, string BankaHesapId, DateTime Tarih, decimal Tutar, string? KarsiTarafAd,
    string? Aciklama, string CariId, string? CariAd, decimal BorcTl,
    decimal AlacakTl = 0m, decimal HasBorcGram = 0m, List<BorcluSatisSatiri>? BorcluSatislar = null);

/// <summary>17.10 — müşterinin borcu olan satışı (TBK 102 sırasıyla, sunucu öneri tutarıyla).</summary>
public sealed record BorcluSatisSatiri(
    string HareketId, DateTime Tarih, DateOnly Vade, int GecikmeGun, decimal KalanTl, string? FisId,
    bool Faturali, bool FaturaSuresiGecti, decimal OneriTl, string? FaturaNo = null);

/// <summary>M7a — GET /cari/{id}/ibanlar satırı.</summary>
public sealed record CariIbanSatiri(string Id, string Iban, string Kaynak, DateTime Olusturuldu);
