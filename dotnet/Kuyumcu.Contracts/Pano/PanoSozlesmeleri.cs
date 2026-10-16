// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 (kimlik Y-7) — VİTRİN / KUR PANOSU SÖZLEŞMESİ.
//
// DURUM TESPİTİ (2026-09-21, kodda ölçüldü): `PanoFiyatServisi` SAF hesap
// olarak vardı ve 20 testi geçiyordu, ama HİÇBİR UÇTAN çağrılmıyordu ve
// hiçbir ekran göstermiyordu ("sunucuda bile uç yok, ekranda düğme yok" —
// UX-S25'in en ağır hâli). Bu paket o hesabı uca ve ekrana bağlar.
//
// ⚠ MARJ VİTRİNE İNMEZ: `PanoGorunum` içinde marj YOKTUR. Hesap sunucuda
//   biter; panoya yalnız sonuç gider (PanoFiyatServisi sınıf notu, Mehmet K).
//   Marj YALNIZ `PanoAyarGorunum` ile ve YÖNETİM ucundan okunur.
// ═════════════════════════════════════════════════════════════════════════════

namespace Kuyumcu.Contracts.Pano;

/// <summary>Vitrine çıkan tek satır. Marj ve ham kotasyon BİLİNÇLİ olarak yok.</summary>
public sealed record PanoSatirGorunum(string Kod, string Etiket, decimal? Alis, decimal? Satis, string Birim);

/// <summary>
/// Panoya ÇIKMAYAN kalem + sebebi. UX-S28: satır sessizce yutulmaz, kuyumcu
/// yönetim panelinde neden kaybolduğunu okur.
/// </summary>
public sealed record PanoAtlananGorunum(string Kod, string Ad, string Sebep);

/// <summary>Vitrin panosunun tam görünümü.</summary>
public sealed record PanoGorunum(
    string Kaynak,
    DateTime Zaman,
    List<PanoSatirGorunum> Satirlar,
    List<PanoAtlananGorunum> Atlananlar,
    int TavanSatir,
    int OkunabilirlikEsigi,
    bool OkunabilirlikUyarisi,
    // PAKET EKSIK-KUCUK (B2-10) — Fiyat Etiketi Yönetmeliği md.6: kıymetli maden
    // birim satış fiyatlarında "işçilik bedelinin dahil olup olmadığı ayrıca
    // belirtilir". Değer: "DAHIL" | "HARIC" | null (dükkân henüz seçmedi → tam ekran
    // pano KİLİTLİ, fail-closed). Geriye dönük uyumlu ek (varsayılan null).
    string? IscilikIbaresi = null);

/// <summary>
/// PAKET EKSIK-KUCUK (B2-10) — işçilik ibaresi PATCH gövdesi. Sözleşme: gövdede
/// alan YOKSA 400 (sessiz etkisizlik yasak); <c>null</c> = ibare silinir (pano kilitlenir).
/// </summary>
public static class IscilikIbaresiDegerleri
{
    public const string Dahil = "DAHIL";
    public const string Haric = "HARIC";
    public static bool Gecerli(string? d) => d is Dahil or Haric;
}

/// <summary>
/// Bir pano kaleminin kiracı ayarı + çözülmüş hâli.
/// <paramref name="Ozel"/> = kiracı bu kalemi GERÇEKTEN ayarlamış (kiraci_ayarlari
/// satırı var). false ise değer varsayılandan gelir ve "Varsayılana dön" kapalıdır.
/// </summary>
public sealed record PanoAyarGorunum(
    string Kod,
    string Ad,
    string? Etiket,
    int Sira,
    decimal AlisMarjTl,
    decimal SatisMarjTl,
    decimal YuvarlamaAdimi,
    bool Aktif,
    bool Ozel);

/// <summary>Yönetim ekranının tek okuması — seçenek listeleri de buradan gelir (UX-S26).</summary>
public sealed record PanoAyarlariGorunum(
    List<PanoAyarGorunum> Ayarlar,
    List<decimal> YuvarlamaAdimlari,
    int TavanSatir,
    int OkunabilirlikEsigi,
    int AktifSayisi);
