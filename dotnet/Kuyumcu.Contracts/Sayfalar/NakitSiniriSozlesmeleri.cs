// PAKET YASAL-2 — nakit sınırı (tevsik) istemci sözleşmeleri: tahsilat taksit
// önizlemesi (TB-15), Uyum "Nakit sınırı aşımları" listesi, dilekçe ve kapatma (TB-16).
// Kural (PLAN § 1 md.9): istemci record'u razor içine yazılmaz, buraya yazılır;
// sunucu `Task<object>` yerine bu tipleri döndürür (sözleşme kilidi).
// Günler yerel gündür (DateOnly → JSON "yyyy-MM-dd").

using System.Text.Json.Serialization;

namespace Kuyumcu.Contracts.Sayfalar.NakitSiniri;

/// <summary>TB-15 önizlemesi: bu cariden en çok ne kadar nakit alınabilir.</summary>
/// <param name="KuralDevrede">Büyük (haddi aşan) işlemden kalan açık borç var ve kural açık.</param>
/// <param name="NakitAlinabilir">En çok alınabilecek nakit; sınır yoksa null.</param>
public sealed record TaksitOnizlemeVeri(
    bool KuralDevrede, decimal? NakitAlinabilir, decimal? BuyukIslemTutari,
    DateOnly? BuyukIslemGunu, decimal? BuyukIslemHaddi, string? Sebep);

/// <summary>GET /api/v1/tevsik/gunluk yanıtı.</summary>
public sealed record TevsikGunlukVeri(
    TaksitOnizlemeVeri? Taksit, decimal Had, bool KuralEtkin, bool KumulatifHesaplandi,
    decimal BugunkuNakitToplam, decimal KalanNakitHakki, bool IslemTutariKuraliDevrede, string? Aciklama);

/// <summary>Bir nakit sınırı aşımı kaydı (GET /api/v1/tevsik/ihlaller satırı).</summary>
public sealed record TevsikIhlalSatiri(
    string Id, string Tur, string Yon, string CariId, string? CariAd, DateOnly IhlalGunu,
    decimal NakitToplamTl, decimal HadTl, DateOnly BildirimSonGunu, string Durum, int KalanIsGunu,
    DateTime? KapatmaTarihi, DateOnly? BildirimTarihi, string? BildirimEvrakNo, bool Gec,
    decimal? IslemToplamTl = null, DateOnly? KaynakGunu = null)
{
    /// <summary>Kapanmamış (işlem bekleyen) mi — AÇIK ya da SÜRE DOLDU.</summary>
    [JsonIgnore] public bool Kapanmamis => Durum is "ACIK" or "SURE_DOLDU";
}

/// <summary>GET /api/v1/tevsik/ihlaller yanıtı — sunucu sınırı (UX-S29) ve bayram notu ile.</summary>
public sealed record TevsikIhlalListesi(
    List<TevsikIhlalSatiri>? Ihlaller, int KapanmamisSayisi, int Sinir, bool DahaVar, bool BayramEksik);

/// <summary>Dilekçedeki ihlal özeti.</summary>
public sealed record TevsikDilekceIhlal(
    string Id, string Tur, string Yon, DateOnly IhlalGunu, decimal NakitToplamTl, decimal HadTl,
    DateOnly BildirimSonGunu, string Durum);

/// <summary>Dilekçeyi veren işletme.</summary>
public sealed record TevsikDilekceIsletme(
    string? Unvan, string? VergiDairesi, string? VergiNo, string? Adres, string? Telefon);

/// <summary>Karşı taraf — TAM kimlik yalnız dilekçe ucunda (okuma izi zorunlu).</summary>
public sealed record TevsikDilekceTaraf(string? Ad, string? VergiNo, string? Adres, string? Telefon);

/// <summary>Kanıt satırı.</summary>
public sealed record TevsikDilekceHareket(string Id, DateTime Tarih, string Tur, decimal TutarTl, decimal NakitTl);

/// <summary>GET /api/v1/tevsik/ihlaller/{id}/dilekce yanıtı.</summary>
public sealed record TevsikDilekceVeri(
    bool Taslak, string? Damga, TevsikDilekceIhlal Ihlal, TevsikDilekceIsletme Isletme,
    TevsikDilekceTaraf KarsiTaraf, List<TevsikDilekceHareket>? Hareketler, string? Dayanak, bool MuafiyetOdeyene);

/// <summary>POST …/bildirildi ve …/gecersiz yanıtı.</summary>
public sealed record TevsikKapatmaYaniti(
    string Id, string Durum, bool Gec, DateOnly? BildirimTarihi, DateOnly? BildirimSonGunu);

/// <summary>
/// Kaynağı bilinmeyen borçtan alınan nakit (K-A, tur 2): kapı GEÇİRDİ ama nakit sınırını
/// kontrol edemedi (açılış/devir/düzeltme bakiyesi). Denetim izinden okunur; ihlal DEĞİLDİR,
/// son gün/geri sayım taşımaz. Salt okunur.
/// </summary>
public sealed record TevsikDogrulanamayanSatiri(
    string Id, DateTime Tarih, string Kaynak, string? CariId, string? CariAd,
    decimal? NakitTl, decimal? DogrulanamayanTl);

/// <summary>GET /api/v1/tevsik/dogrulanamayanlar yanıtı (son 90 gün, sunucu sınırı ile).</summary>
public sealed record TevsikDogrulanamayanListesi(
    List<TevsikDogrulanamayanSatiri>? Satirlar, int Sinir, bool DahaVar, int GunSayisi);
