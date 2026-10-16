// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

// P-03: GecersizKilmaOzeti / TopluSatir(Ozet) / TopluAtlanan / TopluSonuc bu dosyadan
// KALDIRILDI — sunucunun aynı adlı tipleriyle ikinci tanımdı. Tek tanım: Kuyumcu.Contracts.Fiyat.
using Kuyumcu.Contracts.Fiyat;

namespace Kuyumcu.Contracts.Bilesenler.FiyatListesiUrunPaneli;
public sealed record PanelKategori(string Id, string Ad);
public sealed record Satir(
    string UrunId, string Kod, string Ad, string AyarId, decimal Gram, decimal HasGrami, decimal? IscilikTl,
    bool TeklifVar, string? Sebep, decimal HasDegeri, decimal MarjTl, decimal IscilikTlUygulanan,
    decimal ListeFiyati, decimal IndirimTutari, decimal SatisFiyati, string? KampanyaId, string Kaynak,
    GecersizKilmaOzeti? GecersizKilma);
public sealed record Sonuc(int Toplam, int Limit, int Offset, List<Satir> Satirlar);
