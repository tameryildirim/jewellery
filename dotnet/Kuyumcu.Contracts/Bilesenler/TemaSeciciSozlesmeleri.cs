// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Bilesenler/TemaSecici.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Bilesenler.TemaSecici;
public sealed record TemaSecenegi(string Kod, string Ad, string Ikon);
