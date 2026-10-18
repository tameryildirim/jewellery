namespace Kuyumcu.Contracts.Sayfalar.Isletme;

/// <summary>D3-11 — dış API anahtarı (anahtarın kendisi yok; yalnız önek).</summary>
public sealed record ApiAnahtariOzet(string Id, string Ad, string Onek, IReadOnlyList<string> Kapsamlar, DateTime Olusturuldu, DateTime? SonKullanim, bool Iptal);

/// <summary>D3-11 — yeni anahtar: tam anahtar YALNIZ bu yanıtta döner.</summary>
public sealed record YeniApiAnahtari(ApiAnahtariOzet Ozet, string Anahtar);

/// <summary>D3-11 — dış API ürün satırı.</summary>
public sealed record DisUrun(string Kod, string Ad, string? Barkod, string? Ayar, decimal Gram, decimal HasGrami, decimal Stok, decimal? ListeFiyatiTl);
