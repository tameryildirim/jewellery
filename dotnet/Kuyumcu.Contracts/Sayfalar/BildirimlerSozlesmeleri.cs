// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Bildirimler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Bildirimler;
public sealed record Satir(string Id, string Tur, string Onem, string Baslik, string Govde,
    string? HedefRota, string? IlgiliVarlikTuru, string? IlgiliVarlikId,
    DateTime Olusturuldu, DateTime? Kapatildi, string? KapatanKullaniciId, string? KapatmaNotu);
public sealed record Yanit(string Durum, int Limit, int AzamiLimit, int KapaliGosterimGun,
    bool DahaFazlasiVar, List<Satir>? Bildirimler)
{
    public List<Satir> Satirlar => Bildirimler ?? [];
}
// PAKET P-11 (B-24) — iş satırı iki AYRI kapı taşır: yapılandırma (ekrandan
// açılamaz) ve kiracı tercihi (ekrandan açılabilir). Tek "etkin" bayrağı
// "neden kapalı?" sorusunu cevaplayamıyordu (UX-S30).
public sealed record IsSatiri(string Kod, string Ad, bool Etkin, DateTime? SonCalisma, string? SonDurum,
    bool YapilandirmaEtkin = true, bool KiraciKapatti = false);

/// <summary>Zamanlanmış iş ayarları + sunucunun bildirdiği sınırlar/varsayılanlar (B-24).</summary>
public sealed record IsAyarlariVeri(
    int BaslangicSaat, int VadeUfukGun, int MasakUyariGun, List<string>? KapaliIsler,
    int SaatEnAz, int SaatEnCok, int VadeEnAz, int VadeEnCok, int MasakEnAz, int MasakEnCok,
    int VarsayilanVadeUfukGun, int VarsayilanMasakUyariGun)
{
    public List<string> Kapalilar => KapaliIsler ?? [];
}
public sealed record CalismaSatiri(string Id, string IsKodu, string CalismaAnahtari,
    DateTime Baslangic, DateTime? Bitis, string Durum,
    int UretilenBildirim, int IncelenenKayit, int? SureMs, string? HataOzeti, bool Elle);
public sealed record IsYaniti(List<IsSatiri>? Isler, List<CalismaSatiri>? Calismalar, int AzamiCalisma,
    IsAyarlariVeri? Ayarlar = null)
{
    public List<IsSatiri> Isler2 => Isler ?? [];
    public List<CalismaSatiri> Calismalar2 => Calismalar ?? [];
}
public sealed record IsGorunumu(List<IsSatiri> Isler, List<CalismaSatiri> Calismalar, IsAyarlariVeri? Ayarlar);
