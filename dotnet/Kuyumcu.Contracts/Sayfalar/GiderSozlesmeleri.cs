// PAKET P-19 (Y-11/Y-12) — gider + virman ekranının istemci tipleri.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine YAZILMAZ, buraya yazılır.
// Alan adları VE TİPLERİ sunucu yanıtıyla birebir doğrulandı:
//   Modules/Gider/GiderModulu.cs   → Kategoriye(), Kaydet(), Listele()
//   Modules/Gider/VirmanModulu.cs  → Olustur(), Listele()

namespace Kuyumcu.Contracts.Sayfalar.Gider;

public sealed record GiderKategoriVeri(
    string Id, string Ad, string? Aciklama, bool Aktif,
    DateTime Olusturuldu, DateTime Guncellendi, int GiderAdedi, decimal GiderToplamTl);

public sealed record GiderKategoriYaniti(List<GiderKategoriVeri> Kategoriler, int Toplam);

public sealed record GiderSatiri(
    string Id, string KasaId, string? KasaAdi, string? KategoriId, string? KategoriAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    bool StornoSatiri, bool Stornolandi, string? PersonelAd = null);   // Y-05: avans ise personel

/// <summary>
/// PAKET Q10-1 — sunucunun hesapladığı NET gider toplamı (para birimi başına).
/// Sunucu: GiderModulu.cs → GiderParaToplami. İstemci toplamı YENİDEN HESAPLAMAZ.
/// </summary>
public sealed record GiderToplamVeri(string ParaBirimi, decimal Tutar, int Adet);

public sealed record GiderYaniti(
    List<GiderSatiri> Satirlar, int ToplamAdet, int Limit, bool DahaFazlasiVar, string? SinirNotu,
    List<GiderToplamVeri>? Toplamlar = null);

public sealed record GiderSonucu(
    string Id, string KasaId, string? KasaAdi, string KategoriId, string KategoriAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    decimal KasaBakiyesi, string StornoYolu);

public sealed record VirmanSatiri(
    string CikisBacakId, string? GirisBacakId, string KaynakTur, string? KaynakAdi,
    string HedefTur, string? HedefAdi, decimal Tutar, string ParaBirimi,
    string? Aciklama, DateTime Tarih, bool StornoSatiri);

public sealed record VirmanYaniti(
    List<VirmanSatiri> Satirlar, int Toplam, int Limit, bool DahaFazlasiVar, string? SinirNotu);

public sealed record VirmanSonucu(
    string CikisBacakId, string GirisBacakId,
    string KaynakTur, string KaynakId, string? KaynakAdi,
    string HedefTur, string HedefId, string? HedefAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    bool SubelerArasi, string StornoYolu);

public sealed record VirmanStornoSonucu(
    string[] KaynakBacakIdleri, string[] StornoBacakIdleri, int BacakSayisi,
    string Gerekce, DateTime Tarih);

/// <summary>Virman seçicisinin tek dizisi (UX-S26: iki formda tek kaynak).</summary>
public sealed record VirmanTarafSecenegi(string Tur, string Id, string Ad, string ParaBirimi, decimal Bakiye);
