namespace Kuyumcu.Contracts.Sayfalar.Raporlar;

/// <summary>D1-20 — satan kişi başına prim satırı. PersonelId null = satanı bilinmeyen satış (prime girmez).</summary>
public sealed record PrimSatiri(
    string? PersonelId, string? Ad, int SatisAdet, decimal SatisTl, decimal IadeTl, decimal NetTl,
    decimal? HedefTl, decimal? GerceklesmeYuzde, decimal? PrimYuzde, decimal PrimTl);

/// <summary>D1-20 — dönemin prim raporu.</summary>
public sealed record PrimRaporYaniti(DateOnly Baslangic, DateOnly Bitis, IReadOnlyList<PrimSatiri> Satirlar, decimal ToplamPrimTl);
