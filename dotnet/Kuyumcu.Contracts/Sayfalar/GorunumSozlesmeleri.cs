// PAKET P-36b — /ayarlar → Görünüm bölümünün istemci sözleşmesi.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Gorunum;

/// <summary>
/// Sunucudan gelen DÜKKÂN görünüm varsayılanı (GET/PATCH /api/v1/gorunum).
/// ⚠ PAKET B-1: <paramref name="TemaModu"/> eklendi, varsayılanı olan bir alandır
/// (eski yanıtları ayrıştıran kod kırılmaz).
/// </summary>
public sealed record GorunumAyari(string Sema, string YaziOlcegi, string? TemaModu = null);

/// <summary>
/// PAKET B-1 — kullanıcının ÇÖZÜLMÜŞ görünümü (GET/PATCH /api/v1/kullanici/gorunum).
/// Katman çözümü SUNUCUDADIR; ekran <c>Kaynagi</c> alanlarını yalnız rozet
/// olarak gösterir (UX-S21), yeniden hesaplamaz.
/// </summary>
public sealed record KullaniciGorunumAyari(
    string TemaModu,
    string Sema,
    int YaziBoyutu,
    string TemaModuKaynagi,
    string SemaKaynagi,
    string YaziBoyutuKaynagi,
    string? OzelTemaModu,
    string? OzelSema,
    int? OzelYaziBoyutu,
    string KiraciTemaModu,
    string KiraciSema,
    int KiraciYaziBoyutu,
    IReadOnlyList<string> Semalar,
    IReadOnlyList<string> TemaModlari,
    IReadOnlyList<int> YaziBoyutlari);

/// <summary>Ekranda çizilen bir seçenek — şema ya da ölçek.</summary>
/// <param name="Kod">Sunucuya giden kod (altin · kurumsal · zumrut · yogun · okunakli).</param>
/// <param name="Ad">Sözlükten çevrilmiş görünen ad — ham kod EKRANA BASILMAZ (UX-S31).</param>
/// <param name="Aciklama">Ne değişeceğini söyleyen tek cümle.</param>
public sealed record GorunumSecenegi(string Kod, string Ad, string Aciklama);
