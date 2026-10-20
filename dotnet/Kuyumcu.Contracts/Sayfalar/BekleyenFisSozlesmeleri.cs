namespace Kuyumcu.Contracts.Sayfalar.Islemler;

// ═════════════════════════════════════════════════════════════════════════════
// D1-03 (18.10, rakip eşitleme) — İŞLEM BEKLETME (park). Tezgâhta yarım kalan satış fişi
// sunucuda bekletilir; aynı ya da başka kasadan geri alınır. Defter DEĞİLDİR: bekletme hiçbir
// stok/kasa/cari hareketi yazmaz, geri alınan fiş tezgâhta normal Kaydet'ten geçer.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bekletilen fişin bir kalemi (tezgâh FisKalemi'nin kalıcı alanları).</summary>
public sealed record BekleyenFisKalem(
    string UrunId, string UrunAdi, decimal Adet, decimal BirimFiyat, bool Hediye, decimal SatirIskontoYuzde);

/// <summary>Bekletilen fişin içeriği — geri alınınca tezgâha aynen yüklenir.</summary>
public sealed record BekleyenFisIcerik(
    string? CariId, List<BekleyenFisKalem> Kalemler, decimal? FisIskontoYuzde, string? Aciklama,
    string? KurKilidiId = null);   // Y-11: teklifin kur kilidi — geri alınınca tezgâh aynı kuru kullanır

/// <summary>Bekleyenler listesinin satırı.</summary>
public sealed record BekleyenFisOzet(
    string Id, string Etiket, string? CariAd, int KalemSayisi, decimal ToplamTl, string? KullaniciAd, DateTime Olusturuldu,
    string? TeklifNo = null, DateTime? TeklifBitis = null);   // Y-11: fiyat teklifi ise numara + geçerlilik
