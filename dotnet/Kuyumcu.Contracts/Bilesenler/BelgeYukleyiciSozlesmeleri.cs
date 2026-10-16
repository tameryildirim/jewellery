// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Bilesenler/BelgeYukleyici.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Bilesenler.BelgeYukleyici;
public sealed record BelgeSatiri(string Id, string Ad, string Tur, string Mime, int Boyut, string Sha256);
