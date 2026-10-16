// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/UrunEtiketleri.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Contracts.Sayfalar.UrunEtiketleri;
public sealed record UrunVeri(
    string Id, string Tip, string Kod, string Ad, string AyarId,
    decimal GramAgirlik, decimal TasAgirlik, decimal HasGrami, string? Barkod, bool Aktif,
    string? TasKokeni = null, string? TasIbaresi = null,
    // PAKET 20 — etikette maden + milyem ("Platin 950") ve rodyum işareti.
    // Sunucunun ZATEN döndürdüğü alanlar; yeni uç açılmadı.
    decimal Milyem = 0m, string Maden = "ALTIN", string MadenAdi = "Altın",
    bool RodyumKaplama = false);
