namespace Kuyumcu.Web.Bilesenler;

/// <summary>M7a — GET /bankalar/karar-bekleyen satırı.</summary>
public sealed record GelenOdemeSatiri(
    string Id, string BankaHesapId, DateTime Tarih, decimal Tutar, string? KarsiTarafAd,
    string? Aciklama, string CariId, string? CariAd, decimal BorcTl);

/// <summary>M7a — GET /cari/{id}/ibanlar satırı.</summary>
public sealed record CariIbanSatiri(string Id, string Iban, string Kaynak, DateTime Olusturuldu);
