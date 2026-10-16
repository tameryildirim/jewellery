namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// KÂR-ZARAR İSTEMCİ MODELLERİ — paket 35 Faz A.
//
// ⚠ NEDEN SAYFADA DEĞİL: denetim.sh, Pages/*.razor içinde YENİ istemci record
// tanımını saymayı reddeder (SÖZLEŞME-1: yanıt modelleri sayfaya dağılmaz).
// Kâr köprüsü, storno bloğu ve karşılaştırma zarfı birden çok ekranda (paket
// 36 patron paneli, Faz B kırılım sekmeleri) tekrar kullanılacağı için tipler
// baştan sayfa dışında doğar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kâr köprüsü satırı. <paramref name="Kod"/> EKRANA BASILMAZ, sözlükten çevrilir.</summary>
/// PAKET KAR-RAPOR: kâr hesaplanamadığında BRUT_KAR / DONEM_KARI tutarı NULL gelir.
public sealed record KarKoprusuSatiri(string Kod, decimal? TutarTl, bool AraToplam);

/// <summary>Storno bloğu — çift toplamlardan dışlandı, ayrıca sayılır (paket 48-A).</summary>
public sealed record KarZararStorno(int Adet, decimal TutarTl);

/// <summary>Karşılaştırma dönemi — gün sayısı CARİ dönemle EŞİTTİR.</summary>
public sealed record KarZararOnceki(
    string Baslangic, string Bitis, int GunSayisi,
    decimal HasilatTl, decimal? MaliyetTl, decimal? KarTl, decimal? DonemKariTl,
    // RAPOR-GORUNUM (veri d): grafik hasılat çubuğu kârla aynı kümeden (maliyeti bilinen satış).
    decimal? BilinenHasilatTl = null);

/// <summary>Değişim yüzdeleri. Önceki dönem sıfırsa yüzde ANLAMSIZDIR → null.</summary>
public sealed record KarZararDegisim(
    decimal? HasilatTlYuzde, decimal? KarTlYuzde,
    decimal? DonemKariTlYuzde, decimal? KarHasYuzde);

/// <summary>
/// Defter has gramının MADEN kırılımı (paket P-45).
/// <para>
/// ⚠ NEDEN VAR: ekran tek bir "defter has gramı" rakamı basıyordu ve bu rakam
/// altın + gümüş gramlarının TOPLAMIYDI — hiçbir şeyin karşılığı olmayan bir
/// sayı. Gramlar madene göre ayrı durur, toplanmaz.
/// </para>
/// </summary>
public sealed record MadenHasGrami(string Maden, string MadenAdi, decimal ToplamHas, int SatirSayisi);
