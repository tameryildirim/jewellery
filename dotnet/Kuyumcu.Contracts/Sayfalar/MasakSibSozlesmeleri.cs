namespace Kuyumcu.Contracts.Sayfalar.Uyum;

/// <summary>D1-18 — MASAK Online 2.0 ŞİB formuna kopyalanacak alan.</summary>
public sealed record SibAlan(string Etiket, string Deger);

/// <summary>D1-18 — formun bir adımı (kılavuz sırası: gerçek kişi → tüzel kişi → işlem → kategori → açıklama → ekler).</summary>
public sealed record SibAdim(int No, string Baslik, IReadOnlyList<SibAlan> Alanlar, string? Not);

/// <summary>D1-18 — ŞİB gönderime hazırlık: adımlar, sistem adresi, gizlilik uyarısı.</summary>
public sealed record SibHazirlik(string Id, string Adres, string GizlilikNotu, IReadOnlyList<SibAdim> Adimlar);
