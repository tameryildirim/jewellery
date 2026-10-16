// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Yedekler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Yedekler;
public sealed record OzetVeri(
    SonYedekVeri? SonYedek, string Dogrulama, DateTime? DogrulamaTarih,
    DateTime? SonDogrulanmisYedek, string DisKopya, int SaklananKosu, int SaklamaGun,
    int? RpoDakikaOlculen, int RpoDakikaTaahhut, int RtoDakikaTaahhut,
    int? RtoDakikaOlculen, DateTime? SonTatbikat, string? SonTatbikatSonuc, int AzamiLimit,
    // PAKET P-33 (BK-30): son BAŞARILI tatbikat + tazelik — hesap SUNUCUDA.
    DateTime? SonBasariliTatbikat = null, int? TatbikatYasGun = null,
    int TatbikatAzamiGun = 0, bool TatbikatBayat = true,
    // PAKET P-77: bu işletmenin BEKLEYEN doğrulama talebi (yoksa null).
    BekleyenDogrulamaVeri? BekleyenDogrulamaTalebi = null);
/// <summary>PAKET P-77 — ozet.bekleyenDogrulamaTalebi.</summary>
public sealed record BekleyenDogrulamaVeri(string Id, DateTime Olusturuldu);
public sealed record SonYedekVeri(
    string Tur, DateTime Zaman, DateTime? Bitis, long? BoyutBayt, string Durum,
    bool Sifreli, int? TabloSayisi, string? HataOzeti);
public sealed record KosuVeri(
    string Id, string Tur, DateTime Baslangic, DateTime? Bitis, string Durum,
    long? BoyutBayt, bool Sifreli, string DisKopya, string Dogrulama,
    int? TabloSayisi, string? HataOzeti);
public sealed record KosuYanit(int Limit, int AzamiLimit, bool DahaFazlasiVar, List<KosuVeri> Kayitlar);
public sealed record TatbikatVeri(
    string Id, DateTime Tarih, int TabloSayisi, int Uyusan, int Uyusmayan,
    string Sonuc, int SureSn, bool SemaDusuruldu, int? RtoDakikaOlculen, string? Rapor);
public sealed record TatbikatYanit(int Limit, int AzamiLimit, List<TatbikatVeri> Tatbikatlar);
public sealed record TalepVeri(
    string Id, string Gerekce, string Durum, DateTime Olusturuldu,
    DateTime? Islendi, string? YedekKaydiId, string? IptalGerekce,
    // PAKET P-77: YEDEK | DOGRULAMA (eski yanıtlar için varsayılan YEDEK).
    string Tur = "YEDEK");
public sealed record TalepYanit(int Limit, int AzamiLimit, List<TalepVeri> Talepler);
