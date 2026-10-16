// PAKET EKSIK-B1-SATINALMA — Satın alma (B1-1) ve İrsaliyeler (B1-2) ekranlarının istemci tipleri.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine YAZILMAZ, buraya yazılır.
// Alan adları VE TİPLERİ sunucu yanıtıyla birebir doğrulandı:
//   Modules/Satinalma/SatinalmaModulu.cs → Listele(), Detay(), MalKabul()
//   Modules/Irsaliye/IrsaliyeModulu.cs   → Listele(), Detay()

namespace Kuyumcu.Contracts.Sayfalar.SatinAlma;

/// <summary>Durum kırılımı satırı (liste özetindeki sayaç).</summary>
public sealed record SatinAlmaKirilim(string Durum, int Adet);

public sealed record SatinAlmaSatiri(
    string Id, string No, string Durum, string SubeId, string TedarikciCariId, string? TedarikciAdi,
    DateOnly? BeklenenTarih, string? Aciklama, string? HesapKodu,
    DateTime Olusturuldu, DateTime Guncellendi, int KalemSayisi);

public sealed record SatinAlmaListesi(
    List<SatinAlmaSatiri> Satirlar, int Toplam, List<SatinAlmaKirilim> Kirilim,
    int Limit, int AzamiLimit, bool SinirDegdi);

public sealed record SatinAlmaKalemVeri(
    string Id, string? UrunId, string? UrunAdi, string? Birim, string? Tanim,
    decimal Adet, decimal? BirimFiyatTl, decimal KabulEdilenAdet, decimal KalanAdet, int Sira);

public sealed record SatinAlmaIrsaliyeOzeti(string Id, string No, DateTime Tarih, string Durum, string? FaturaId);

public sealed record SatinAlmaDetay(
    string Id, string No, string Durum, string SubeId, string TedarikciCariId, string? TedarikciAdi,
    DateOnly? BeklenenTarih, string? Aciklama, string? HesapKodu, string? IptalGerekce,
    DateTime Olusturuldu, DateTime Guncellendi,
    List<SatinAlmaKalemVeri> Kalemler, List<SatinAlmaIrsaliyeOzeti> Irsaliyeler,
    bool Duzenlenebilir, bool MalKabulEdilebilir, string? KilitSebebi);

/// <summary>Mal kabul yanıtı — UX-S27: irsaliye no ve yeni durum BANDA yazılır.</summary>
public sealed record MalKabulSonucu(
    SatinAlmaDetay Siparis, string IrsaliyeId, string IrsaliyeNo, int KabulEdilenKalem, string YeniDurum);

public sealed record IrsaliyeSatiri(
    string Id, string No, string Yon, string Durum, string CariId, string? CariAdi, string SubeId,
    DateTime Tarih, string? FaturaId, string? SatinalmaSiparisId, string? Aciklama, string? HesapKodu,
    bool Bagsiz, int KalemSayisi);

public sealed record IrsaliyeListesi(
    List<IrsaliyeSatiri> Satirlar, int Toplam, int BagsizToplam, int Limit, int AzamiLimit, bool SinirDegdi);

public sealed record IrsaliyeKalemVeri(
    string Id, string? UrunId, string? UrunAdi, string? Birim, string? Tanim, decimal Adet, string? HareketId, int Sira);

public sealed record IrsaliyeDetay(
    string Id, string No, string Yon, string Durum, string CariId, string? CariAdi, string SubeId, DateTime Tarih,
    string? SatinalmaSiparisId, string? FaturaId, string? Aciklama, string? HesapKodu, string? IptalGerekce,
    DateTime Olusturuldu, DateTime Guncellendi, List<IrsaliyeKalemVeri> Kalemler,
    bool Bagsiz, bool Duzenlenebilir, string? KilitSebebi);

/// <summary>
/// Sipariş / sevk formunun bir satırı (ekranda düzenlenen DEĞİŞKEN model — sunucuya
/// <c>{urunId, adet, birimFiyatTl, tanim}</c> olarak gider). Oluşturma ve düzenleme AYNI
/// satır modelini kullanır (UX-S26).
/// </summary>
public sealed class SatinAlmaFormSatiri
{
    public string Anahtar { get; } = Guid.NewGuid().ToString("N")[..8];
    public string? UrunId { get; set; }
    /// <summary>Düzenlemede mevcut ürünün adı (seçici yeniden açılmadan gösterilir).</summary>
    public string? UrunAdi { get; set; }
    public decimal? Adet { get; set; }
    public decimal? BirimFiyatTl { get; set; }
    public string? Tanim { get; set; }
}
