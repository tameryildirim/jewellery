// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Siparisler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Siparisler;
public sealed record Kirilim(string Durum, int Adet);
public sealed record Satir(string Id, string No, string Tur, string Durum, string? CariId,
    string? CariAd, string? SubeId, DateOnly? SozVerilenGun, decimal? TahminiTutarTl,
    string? Aciklama, int KalemSayisi, bool Geciken, List<string>? IzinliGecisler,
    bool Duzenlenebilir, DateTime Olusturuldu, DateTime Guncellendi);
public sealed record Yanit(int Limit, int AzamiLimit, int Toplam, bool DahaFazlasiVar,
    int GecikenSayisi, List<Kirilim>? DurumKirilimi, List<Satir>? Siparisler)
{
    public List<Satir> Satirlar => Siparisler ?? [];
    public List<Kirilim> Kirilim => DurumKirilimi ?? [];
}
public sealed record CariSatiri(string Id, string Ad, string Tur);
public sealed record UrunSatiri(string Id, string Ad, string? AyarId, decimal GramAgirlik);
public sealed record KurSatiri(string? Id);
