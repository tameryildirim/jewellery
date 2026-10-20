namespace Kuyumcu.Contracts.Sayfalar.Gise;

// Y-03 (20.10) — gişe zimmeti: GET /gise · POST /gise · GET /gise/{id} · POST /gise/{id}/kabul · /takviye ·
// /takviye/{kalemId}/karar · /kapat · /iptal. Sunucu (GiseServisi) AYNI tipleri döndürür (sözleşme kilidi).
public sealed record GiseZimmetOzet(string Id, string KasaId, string KasaAd, string PersonelId, string PersonelAd, string Durum,
    decimal AcilisNakit, string ParaBirimi, int UrunSayisi, int BekleyenTakviye, DateTime Olusturuldu, DateTime? KabulZamani,
    DateTime? KapanisZamani, decimal? NakitFark);
/// <summary>Zimmetteki bir ürünün gün sonu durumu: zimmet (sabah + kabul edilen takviye) − satılan = beklenen.</summary>
public sealed record GiseUrunDurumu(string UrunId, string Kod, string Ad, decimal Zimmet, decimal Satilan, decimal Beklenen,
    decimal? Sayilan, decimal? Fark);
public sealed record GiseTakviyeOzet(string Id, string? UrunId, string? UrunKod, string? UrunAd, decimal Adet, decimal? NakitTutar,
    string Durum, string IsteyenId, string IsteyenAd, string? OnaylayanAd, DateTime Olusturuldu);
/// <param name="NakitGizli">Kör sayım açık ve bakan kişi tezgâhtar: beklenen nakit GÖNDERİLMEDİ (0).</param>
public sealed record GiseZimmetDetay(GiseZimmetOzet Ozet, string? KaynakKasaId, string? KaynakKasaAd, decimal BeklenenNakit,
    bool NakitGizli, decimal? SayilanNakit, decimal? NakitFark, string? Aciklama, string? DevirKasaAd,
    IReadOnlyList<GiseUrunDurumu> Urunler, IReadOnlyList<GiseTakviyeOzet> Takviyeler,
    bool KabulEdebilir, bool KapatabilirMi, bool KararVerebilirMi, bool IptalEdebilir);
/// <summary>Yeni zimmet formundaki taslak ürün satırı (yalnız ekran; sunucuya kalemler[] olarak gider).</summary>
public sealed record GiseYeniKalem(string UrunId, string Ad, decimal Adet);
public sealed record GiseListesi(IReadOnlyList<GiseZimmetOzet> Acik, IReadOnlyList<GiseZimmetOzet> Gecmis, bool YonetimMi);
public sealed record GiseKapatSonucu(GiseZimmetDetay Detay, decimal DevredilenNakit, string? DevirKasaAd, decimal NakitFark, int FarkliUrun);
