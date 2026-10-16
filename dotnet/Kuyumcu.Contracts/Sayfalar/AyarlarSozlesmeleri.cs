// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Ayarlar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Ayarlar;
// ── dil kartları (React: DILLER sabiti) ──
public sealed record DilSecenegi(string Kod, string Ad, string Bayrak);
