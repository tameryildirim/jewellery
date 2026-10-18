namespace Kuyumcu.Contracts.Sayfalar.Cariler;

/// <summary>D1-06 — taksitlendirilebilir veresiye satış (açık borç).</summary>
public sealed record OdemePlaniAdayi(string HareketId, DateTime Tarih, decimal AcikTutar, string? Aciklama);

/// <summary>D1-06 — taksit. Durum: ODENDI · KISMI · GECIKTI · BEKLIYOR · KARSILIKSIZ.</summary>
public sealed record OdemePlaniTaksitOzet(int Sira, DateOnly Vade, decimal TutarTl, decimal OdenenTl, string Durum, string? EvrakId);

/// <summary>D1-06 — ödeme planı. Durum: AKTIF · TAMAMLANDI · IPTAL.</summary>
public sealed record OdemePlaniOzet(
    string Id, string CariId, string? CariAd, string KaynakHareketId, DateTime KaynakTarih, decimal ToplamTl, decimal OdenenTl,
    bool Senetli, string Durum, DateTime Olusturuldu, IReadOnlyList<OdemePlaniTaksitOzet> Taksitler);
