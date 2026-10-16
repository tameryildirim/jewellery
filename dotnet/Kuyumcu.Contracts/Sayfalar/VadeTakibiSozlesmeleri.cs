// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/VadeTakibi.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.VadeTakibi;
public sealed record Borc(string HareketId, DateTime Tarih, DateOnly Vade,
    decimal AcikTutar, int GecikmeGun, string? Durum);
public sealed record CariSatir(string CariId, string CariAd, string? CariTur, string? Telefon,
    decimal GecikenTutar, decimal YaklasanTutar, decimal VadesizTutar,
    int? EnEskiGecikmeGun, DateOnly? EnYakinVade, List<Borc>? Borclar)
{
    public List<Borc> Kalemler => Borclar ?? [];
}
public sealed record Yanit(int UfukGun, decimal GecikenToplam, decimal YaklasanToplam,
    decimal VadesizToplam, decimal UfukOtesiToplam, decimal AcikToplam,
    List<CariSatir>? Cariler);
