namespace Kuyumcu.Contracts.Sayfalar.Islemler;

/// <summary>D1-05 — işletmenin satış onayı eşikleri (boş = kapalı) ve mesai saatleri.</summary>
public sealed record SatisOnayAyarlari(decimal? IskontoYuzde, decimal? TutarTl, bool Zararina, string? MesaiBaslangic, string? MesaiBitis);

/// <summary>D1-05 — satış onay talebi. Durum: BEKLIYOR · ONAYLANDI · REDDEDILDI · KULLANILDI · SURESI_DOLDU.
/// Sebepler: ISKONTO · KISISEL · TUTAR · ZARARINA.</summary>
public sealed record OnayTalebiOzet(
    string Id,
    string Durum,
    IReadOnlyList<string> Sebepler,
    decimal ToplamTl,
    decimal ListeTl,
    decimal IndirimYuzde,
    decimal HasDegeriTl,
    int KalemSayisi,
    string? IsteyenAd,
    string? CariAd,
    string? KararVerenAd,
    string? KararNotu,
    DateTime Olusturuldu,
    DateTime? KararZamani);
