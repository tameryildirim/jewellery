// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Fiyatlandirma.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Fiyatlandirma;
public sealed record Liste(string Id, string Ad, string Tur, decimal HasMarjYuzde, decimal IscilikCarpanYuzde, bool Varsayilan, bool Aktif);
public sealed record Kamp(string Id, string Ad, DateOnly Baslangic, DateOnly Bitis, string HedefTur, string? HedefId, string IndirimTuru, decimal Deger, bool Aktif);
public sealed record UrunOzet(string Id, string Ad, string AyarId, decimal HasGrami);
public sealed record KategoriOzet(string Id, string Ad);
public sealed record KurOzet(string? Id);
