// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Yetkiler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Yetkiler;
public sealed record YetkiMatrisi(string[] Moduller, string[] Roller, Dictionary<string, string[]> Matris);

/// <summary>PAKET GOC-D — yetki matrisi ızgarasının satırı (rol; SAHİP satırı kilitli).</summary>
public sealed record YetkiSatiri(string Rol, bool Sahip);
