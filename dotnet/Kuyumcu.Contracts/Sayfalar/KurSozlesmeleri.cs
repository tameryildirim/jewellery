// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Kur.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Kur;
public sealed record AyarFiyatVeri(string AyarId, decimal Milyem, decimal GramFiyati);
// PAKET S-3b — alan adı VE tipi sunucuyla doğrulandı:
//   Modules/Kur/KurModulu.cs KurSnapshotCevap → IptalEdildi bool ·
//   IptalGerekce string? · IptalTarihi DateTime?
public sealed record KurVeri(string Id, decimal AltinHasFiyati, decimal? GramAltinFiyati,
                              decimal? UsdTry, decimal? EurTry, decimal? GbpTry,
                              string Kaynak, DateTime Olusturuldu,
                              List<AyarFiyatVeri>? AyarFiyatlari,
                              bool IptalEdildi = false, string? IptalGerekce = null,
                              DateTime? IptalTarihi = null);
public sealed record PariteVeri(string Kod, string Ad, decimal? Alis, decimal? Satis, string Birim);
public sealed record PariteYaniti(string Kaynak, List<PariteVeri>? Pariteler);
/// <summary>UX-4d madde 24 — sözleşmesiz kaynak bayrağı ve seçili kaynak.</summary>
public sealed record KaynakDurumu(
    List<string> Kaynaklar, string? Varsayilan, string? Secili,
    bool SozlesmesizKaynaklarAcik, bool SeciliSozlesmesiz, string? ManuelKod);
