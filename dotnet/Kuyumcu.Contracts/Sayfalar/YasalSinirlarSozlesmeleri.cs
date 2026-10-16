// PAKET YASAL-1 — "Yasal sınırlar" ekranı ve Panel bandı istemci sözleşmeleri.
// Kural (PLAN § 1 md.9): istemci record'u razor içine yazılmaz, buraya yazılır.
// Tarihler sunucudan "yyyy-MM-dd" metni gelir (yerel gün); ekran Bicim ile yazar.

namespace Kuyumcu.Contracts.Sayfalar.YasalSinirlar;

/// <summary>GET /api/v1/yasal-parametreler satırı.</summary>
public sealed record YasalSatirVeri(
    string Id, string Anahtar, decimal? DegerSayi, string? DegerMetin, string Birim,
    string Baslangic, string? Bitis, string Dayanak, string KaynakUrl, string Teyit, string AciklamaSade,
    bool Kullaniliyor = true,
    // Tur 3: eşik ifadesi (USTU → "X üzeri", VE_USTU → "X ve üzeri"); null → değer olduğu gibi.
    string? EsikTuru = null, decimal? EsikTutari = null,
    // PAKET YASAL-PARAM-EKRAN: kaynak künyesi (Resmî Gazete tarihi YYYY-MM-DD + sayısı); eski satırda null.
    string? ResmiGazeteTarihi = null, string? ResmiGazeteSayisi = null,
    // PAKET YASAL-PARAM-2: bugünden geçerli satırın yürürlüğe girdiği yerel saat (HH:mm; anı yoksa null)
    // ve satır bugün AYNI GÜN düzeltilebilir mi (bugün, anlı yazılmış, hâlâ yürürlükte).
    string? BaslangicSaati = null, bool AyniGunDuzeltilebilir = false);

/// <summary>GET /api/v1/yasal-parametreler yanıtı.</summary>
public sealed record YasalListeVeri(
    string Tarih, string Bugun, List<YasalSatirVeri>? Satirlar, List<string>? Tatiller, bool BayramEksik,
    // Tur 3: o günden SONRA başlayacak satırlar (ufuksuz, başlangıca göre artan) — kartta
    // "Yaklaşan değişiklik" satırı; geçmiş listesine GİRMEZ.
    List<YasalSatirVeri>? Sonrakiler = null,
    // PAKET YASAL-PARAM-EKRAN tur 3: bu oturum ByGatech yönetim kiracısında mı (Sistem:YonetimKiracilari).
    // Yalnız GÖRÜNÜRLÜK içindir; yazma yetkisi sistem anahtarıdır. Varsayılan false (fail-closed).
    bool YonetimAcik = false);

/// <summary>GET /api/v1/yasal-parametreler/gecmis yanıtı.</summary>
public sealed record YasalGecmisVeri(string Anahtar, List<YasalSatirVeri>? Satirlar);

/// <summary>GET /api/v1/yasal-parametreler/yaklasan — tek değişiklik.</summary>
public sealed record YasalDegisiklikVeri(
    string Anahtar, string AciklamaSade, string Birim, string Baslangic, int KalanGun,
    decimal? YeniSayi, string? YeniMetin, decimal? EskiSayi, string? EskiMetin, string Teyit,
    // Tur 3: eşik ifadesi — bantta da iç karşılaştırma değeri kuruşla basılmaz.
    string? EsikTuru = null, decimal? YeniEsik = null, decimal? EskiEsik = null, string? EskiEsikTuru = null);

/// <summary>GET /api/v1/yasal-parametreler/yaklasan yanıtı.</summary>
public sealed record YasalYaklasanVeri(
    string Bugun, int UfukGun, int BantUfkuGun, List<YasalDegisiklikVeri>? Degisiklikler, int Yil, bool BayramEksik);

/// <summary>
/// PAKET YASAL-PARAM-EKRAN — POST /api/v1/sistem/yasal-parametreler/onizleme yanıtı (onay penceresi).
/// Yon: SIKILASTIRMA · GEVSETME · AYNI · DEGISIKLIK. AcikIslemSayisi null = bu sınır için sayılmaz.
/// </summary>
public sealed record YasalOnizlemeVeri(
    string Anahtar, string Birim, decimal? EskiDeger, string? EskiMetin, string? EskiBaslangic,
    decimal? YeniDeger, string? YeniMetin, string Baslangic, bool BugunMu, string Yon, int? AcikIslemSayisi,
    string ResmiGazeteTarihi, string ResmiGazeteSayisi, string Teyit, string Dayanak, string OnayKodu,
    bool BuyukDegisim = false);

/// <summary>PAKET YASAL-PARAM-EKRAN — POST /api/v1/sistem/yasal-parametreler yanıtı (başarı bandı).</summary>
public sealed record YasalEkleSonucVeri(
    string Id, string Anahtar, decimal? DegerSayi, string? DegerMetin, decimal? EskiDeger, string Birim,
    string Baslangic, bool BugunMu, string Yon, string Teyit, string ResmiGazeteTarihi, string ResmiGazeteSayisi,
    string? KapananSatirId);

/// <summary>PAKET YASAL-PARAM-2 — POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme/onizleme yanıtı.</summary>
public sealed record YasalDuzeltOnizlemeVeri(
    string Id, string Anahtar, string Birim, decimal? YanlisDeger, decimal YeniDeger, decimal? OncekiDeger,
    string YanlisSaati, string OnayKodu, bool BuyukDegisim = false);

/// <summary>PAKET YASAL-PARAM-2 — POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme yanıtı (başarı bandı).</summary>
public sealed record YasalDuzeltSonucVeri(
    string Id, string Anahtar, string Birim, decimal? DegerSayi, decimal? YanlisDeger,
    string YanlisSaati, string DuzeltmeSaati, string DuzeltilenSatirId);
