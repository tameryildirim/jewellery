namespace Kuyumcu.Contracts.Sayfalar.Stopaj;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 / Y4-B — stopaj dönemleri sözleşmesi (sunucu + ekran AYNI tip).
// Para birimi TL'dir (stopaj vergi dairesine TL ödenir); alan adı bunu taşır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir dönemin (YYYY-MM) stopaj durumu.</summary>
public sealed record StopajDonemi(
    string Donem,
    decimal KesilenTl,
    decimal OdenenTl,
    decimal KalanTl,
    // Son ödeme günü; yasal parametre bulunamazsa null (uydurulmaz).
    DateOnly? SonGun,
    // Son gün parametresinin teyit damgası (DOGRULANDI/TEYITSIZ/KAPALI).
    string? SonGunTeyit,
    bool SonGunGecti,
    int PusulaSayisi,
    List<StopajOdemeSatiri> Odemeler);

/// <summary>Kayıtlı ödeme. Geri alınmışsa (kasa satırı stornolu) toplamlara girmez.</summary>
public sealed record StopajOdemeSatiri(
    string Id, string Donem, decimal TutarTl, DateTime OdemeTarihi,
    string KasaId, string? KasaAdi, string HareketId, string? TahakkukFisNo, bool GeriAlindi);

public sealed record StopajDonemleriYaniti(int Yil, List<StopajDonemi> Donemler);

/// <summary>Panel/kasa özeti: bu ay kesilen + ödenmesi gereken dönemler.</summary>
public sealed record StopajOzeti(
    string BuAy,
    decimal BuAyKesilenTl,
    // Son günü geçmiş, kalanı olan dönemler (KRİTİK).
    List<StopajDonemi> Gecikmis,
    // Son güne 5 gün ya da daha az kalmış, kalanı olan dönemler.
    List<StopajDonemi> Yaklasan,
    int UyariGunu);

/// <summary>POST /stopaj/odemeler sonucu — banda yazılır (UX-S27).</summary>
public sealed record StopajOdemeSonucu(
    string Id, string Donem, decimal TutarTl, string KasaId, string? KasaAdi,
    decimal KasaBakiyesi, decimal KalanTl);
