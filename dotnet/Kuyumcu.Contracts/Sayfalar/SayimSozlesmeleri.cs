// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Sayim.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Sayim;
public sealed record Oturum(string Id, string Ad, string KapsamTur, string Durum);
public sealed record Satir(string UrunId, string? Kod, string Ad, string? Barkod, string? Birim, decimal SistemAdet, decimal? SayilanAdet, decimal GuncelAdet, decimal? Fark, bool OturumdaHareketVar);
public sealed record Detay(string Id, string Ad, string SubeId, string KapsamTur, string Durum, int ToplamSatir, int Sayilan, int Sayilmayan, int Farkli, decimal EksikAdet, decimal FazlaAdet, decimal NetFarkHas, int Hareketli, List<Satir> Satirlar);
public sealed record KapatYaniti(int Uygulanan, int Atlanan, Detay Detay);
public sealed record Ozet(string Id, string Ad);
