namespace Kuyumcu.Contracts.Sayfalar.Islemler;

/// <summary>D1-04 — etkin kur kilidi (tezgâhta "kilitli kur" olarak gösterilir).</summary>
public sealed record KurKilidiOzet(
    string Id,
    string KurSnapshotId,
    string? CariId,
    string? CariAd,
    decimal AltinHasFiyati,
    decimal? UsdTry,
    decimal? EurTry,
    string? Not,
    DateTime Bitis,
    string? OlusturanAd,
    DateTime Olusturuldu);
