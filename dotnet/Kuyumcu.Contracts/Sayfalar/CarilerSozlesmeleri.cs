// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Cariler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Cariler;
// ── veri modelleri (JSON camelCase otomatik eşleşir) ──
// ⚠ Yeni istemci tipi AÇILMADI (MÜHÜR 5 KOŞUL 3): mevcut tipe sunucunun
//   zaten döndürdüğü `aktif` alanı eklendi (CariModulu.Listele satır ~351).
// ⚠ Paket 27: yeni istemci tipi AÇILMADI — mevcut iki tipe sunucunun
//   döndürdüğü üç döviz alanı eklendi (denetim.sh tavanı korunur).
// ⚠ Paket 40-A: yine YENİ istemci tipi AÇILMADI — mevcut iki tipe
//   sunucunun döndürdüğü risk/PEP alanları eklendi (denetim.sh istemci tipi tavanı
//   korunur). Tip sözleşmesi: sunucuda `puan` INT, `riskSeviye` string?,
//   `pepBeyani` bool — burada da AYNI (QA tur 3 KRİTİK: int ↔ ondalık
//   uyuşmazlığı Blazor render ağacını öldürüyordu).
public sealed record CariVeri(
    string Id, string Ad, string Tur, string? Telefon, string? Email,
    decimal TlBakiye, decimal HasGramBakiye, bool Aktif = true,
    decimal UsdBakiye = 0m, decimal EurBakiye = 0m, decimal GbpBakiye = 0m,
    string? RiskSeviye = null, bool PepBeyani = false,
    // PAKET B-3 (D2): "Peşin Müşteri" sistem kartı — satırda pasife alma
    // yerine DegismezIpucu çizilir. Ek alan (tip tavanı korunur).
    bool SistemCarisi = false,
    // PAKET T-M1 (tuval Musteriler.dc.html): "Toptancı" süzgeci/rozeti ve panelin
    // "Son değişiklik" satırı. Sunucu (CariServisi.Listele) iki alanı ZATEN
    // döndürüyordu; yeni istemci tipi AÇILMADI, mevcut tip genişletildi.
    // `guncellendi` sunucuda UTC DateTime (Kind belirsiz serileşir) → ekran UTC sayar.
    string? Segment = null, DateTime? Guncellendi = null,
    // PAKET CARI-BAKIYE (tek tanım): HasGramBakiye sunucuda EMANETSİZ üretilir; emanet payı
    // ayrı döner (bizde duran, artı). İstemci hesaplamaz, yalnız rozet çizer.
    decimal EmanetteHas = 0m,
    // P-DIL-BORC-KASA (Y18): altın emanetinin bırakıldığı gibi kalanı ("22 ayar · 12,50 g").
    // null = bilinmiyor (eski satır) → ekran has gösterir; boş = emanet yok.
    IReadOnlyList<Kuyumcu.Core.Domain.EmanetAyarSatiri>? EmanetBirakildigi = null,
    // D1-07 — kara liste (veresiye engeli) ve risk rengi için kredi limiti.
    bool VeresiyeEngeli = false, string? VeresiyeEngelSebebi = null, decimal? KrediLimitiTl = null,
    string? YuzukOlcusu = null);
public sealed record BakiyeVeri(
    decimal TlBakiye, decimal HasGramBakiye,
    decimal UsdBakiye = 0m, decimal EurBakiye = 0m, decimal GbpBakiye = 0m,
    // PAKET CARI-BAKIYE: HasGramBakiye emanetsiz; kayıtlı kolon (emanet dahil) ve emanet payı ayrıca.
    decimal EmanetteHas = 0m, decimal? KayitliHasGramBakiye = null);
public sealed record CariDetayVeri(
    string Id, string Ad, string Tur, string? Telefon, string? Email,
    string? VergiNo, string? Adres, decimal? KrediLimitiTl, int? VarsayilanVadeGun, string? FiyatListesiId,
    bool PepBeyani = false, string? PepGorev = null, DateTime? PepBeyanTarihi = null,
    // P-10 (Y-3): sunucu GET /cari/{id} yanıtında `notlar` alanını ZATEN
    // döndürüyordu (CariModulu.cs), istemci okumuyordu — bu yüzden düzenleme
    // çekmecesi notu gösteremiyor, PATCH gövdesi de taşıyamıyordu.
    string? Notlar = null,
    // ═══ PAKET P-16 (Y-27) — CARİ KARTI KİMLİK / KYC ALANLARI ═══════════════
    // Yeni istemci record AÇILMADI; mevcut tip genişletildi (PLAN § 1 md.9).
    // Tip sözleşmesi sunucuyla birebir: iskontoYuzde SUNUCUDA decimal(5,2),
    // dogumTarihi DateOnly? — istemcide de AYNI tip.
    string? Unvan = null, string? KimlikTuru = null, DateOnly? DogumTarihi = null,
    string? Uyruk = null, decimal? IskontoYuzde = null, string? Segment = null,
    // ═══ PAKET P-26 (Y-37) — MUHASEBE HESAP KODU ═══════════════════════════
    // UX-S26 / denetim.sh DTO_FORM_ESLESMEZ: sunucu bu alanı oluşturmada DA
    // düzenlemede DE kabul eder; istemci tipi sunucuyla birebir (string?).
    string? HesapKodu = null,
    // D1-07 — kara liste (veresiye engeli).
    bool VeresiyeEngeli = false, string? VeresiyeEngelSebebi = null,
    // D2-12 — yüzük ölçüsü.
    string? YuzukOlcusu = null);
public sealed record FiyatListesiOzet(string Id, string Ad, string Tur, bool Aktif);
// KESIF-P2 (#8): yeni tip AÇILMADI — sunucunun (CariServisi.Ekstre) döndürdüğü satır başına
// cari has etkisi ve sayfa dışı etki mevcut iki tipe eklendi. Satır has'ı (HasGrami) DEFTER
// değeridir; bakiyeye yazılan delta CariHasEtkisi'dir (satırlar + sayfa dışı = bakiye).
public sealed record EkstreHareket(
    string Id, string Tur, decimal TlTutari, decimal HasGrami,
    string? Aciklama, DateTime Olusturuldu,
    decimal CariHasEtkisi = 0m, bool HasBakiyesineGirer = false, bool Emanet = false, string? Maden = null,
    // KESIF-P2 tur 3 (karar 3): ekrandaki bakiye etkisi (emanet 0) ve emanet payı (bizde duran, artı).
    decimal BakiyeHasEtkisi = 0m, decimal EmanetHas = 0m,
    // PAKET CARI-BAKIYE: satırın cari TL bakiyesine etkisi (null = bilinmiyor, Faz 0 öncesi satır).
    decimal? CariTlEtkisi = null,
    // TOPLU-1: iptal (storno) ters kaydının kaynağı — sunucu yanıtında zaten vardı (stornoKaynakId);
    // doluysa satır "… iptali" adını taşır ve "STORNO: " ön eki ekrana çıkmaz.
    string? StornoKaynakId = null,
    // PAKET PARA-DEMO (madde 5): satırın ödeme türü — sunucu yanıtında zaten vardı (odemeTuru);
    // ekstre satırında "Nakit / Kart / Havale / Altın" yazar (ham kod basılmaz, sözlükten).
    string? OdemeTuru = null,
    // PARA-DEMO-2 (madde 1): KARMA satırın bacak araçları (sıra korunur) — "Kart + Havale".
    List<string>? OdemeAraclari = null);
public sealed record EkstreYaniti(BakiyeVeri Bakiye, List<EkstreHareket> Hareketler,
    decimal SayfaDisiHasEtkisi = 0m, bool DahaFazlaVar = false, int Limit = 100,
    // KESIF-P2 tur 2: kayıtlı bakiye − tüm işlemlerin etkisi (0 değilse ekranda uyarı; "eski işlem" DEĞİL).
    decimal BakiyeUyusmazligiHas = 0m,
    // KESIF-P2 tur 3 (karar 3): emanetsiz bakiye (üstteki kutu) ve "Emanette" kutusu. Null = eski sunucu.
    decimal? BakiyeEmanetsizHas = null, decimal EmanetteHas = 0m,
    // PAKET CARI-BAKIYE (TL aynası): kayıtlı TL − kural toplamı; null = ölçülemez (sebep TlAynaOlculemez).
    decimal? BakiyeUyusmazligiTl = null, string? TlAynaOlculemez = null);
public sealed record KasaOzet(string Id, string Ad, string ParaBirimi, bool Aktif);
/// <summary>Kur fotoğrafı — paket 27 ile döviz alanları da okunur (camelCase).</summary>
public sealed record KurVeri(string Id, decimal? UsdTry = null, decimal? EurTry = null, decimal? GbpTry = null);

// ── PAKET P-13 (B-39) — pasife alma öncesi bağlı kayıt özeti ─────────────────
// TEK TİP: sunucu (CariServisi.BagliKayitlar) ve ekran (Cariler.razor) AYNI
// record'u kullanır; ad ya da tip sapması derleme hatası verir (PLAN § 1 md.9).
/// <summary>Cariye bağlı kayıt sayıları — "pasife al" onayında gösterilir (UX-S23).</summary>
public sealed record CariBagliKayit(
    string CariId, bool Aktif, int HareketSayisi,
    decimal TlBakiye, decimal HasGramBakiye, bool DovizBakiyeVar);
