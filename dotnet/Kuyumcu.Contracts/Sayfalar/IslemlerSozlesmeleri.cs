// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Islemler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;
using Kuyumcu.Core.Sozlesmeler;

namespace Kuyumcu.Contracts.Sayfalar.Islemler;
// ── veri modelleri (JSON camelCase otomatik eşleşir) ──
// PAKET B-3 (D2): SistemCarisi — "Peşin Müşteri" kartı; GET /cari/sistem-carisi
// ve GET /cari yanıtlarındaki ek alan. Yeni istemci tipi AÇILMADI (tip tavanı).
// PAKET S2-3 (bakiye rozeti, Pusula F4): GET /cari yanıtının ZATEN taşıdığı
// bakiye alanları (sunucu adı + tipi birebir: decimal). Yeni tip AÇILMADI.
public sealed record CariVeri(string Id, string Ad, bool SistemCarisi = false,
    decimal TlBakiye = 0m, decimal HasGramBakiye = 0m,
    decimal UsdBakiye = 0m, decimal EurBakiye = 0m, decimal GbpBakiye = 0m,
    // PAKET T-S2 (tuval Profesyonel "Fiyat listesi" kutusu): GET /cari yanıtının ZATEN
    // taşıdığı alan (sunucu adı + tipi birebir). Yeni tip AÇILMADI.
    string? FiyatListesiId = null,
    // SATIS-3 k2 (tasarım hakemi): eski altınla ödemede satıcı kimliği seçili müşteriden dolar — GET /cari yanıtının
    // ZATEN taşıdığı alanlar (CariModulu.Listele: vergiNo, adres, kimlikTuru; ad/tip birebir). Yeni tip AÇILMADI.
    string? VergiNo = null, string? Adres = null, string? KimlikTuru = null);
public sealed record UrunVeri(
    string Id, string Ad, string? AyarId, string? Ayar,
    decimal HasGrami, decimal? IscilikTl, decimal? SatisFiyati, string? Birim = "ADET",
    // ── PAKET P-16 (Y-4) — satış kalem satırında ayar/milyem/gram/has ──────
    // Yeni istemci record AÇILMADI; sunucunun ZATEN döndürdüğü alanlar
    // (GET /urunler → List<Data.Urun>) mevcut tipe eklendi. Tip sözleşmesi
    // sunucuyla birebir: milyem decimal(7,2), gram/taş decimal(12,4).
    decimal Milyem = 0m, decimal GramAgirlik = 0m, decimal? TasAgirlik = null,
    // PAKET T-S2 (tuval Profesyonel kalem tablosu "Barkod" kolonu): GET /urunler
    // yanıtının ZATEN taşıdığı alanlar (Data.Urun.Barkod / Kod). Yeni tip AÇILMADI.
    string? Barkod = null, string? Kod = null);
/// <summary>SATIS-3 t6: SubeId — /kasalar yanıtının ZATEN taşıdığı alan; varsayılan kasa oturumun şubesinden seçilir
/// (başka şubenin kasasına nakit yazılmasın). Eski gövdelerde yoksa null = bilinmiyor.</summary>
/// <param name="SorumluKullaniciId">D1-11: kasanın sorumlu çalışanı — tezgâhta o çalışanın varsayılan kasası.</param>
public sealed record KasaVeri(string Id, string Ad, string ParaBirimi, bool Aktif, string? SubeId = null, string? SorumluKullaniciId = null);
public sealed record SubeVeri(string Id, string Ad, bool Aktif);
/// <summary>Sunucu fiyat teklifi — kırılımıyla. Formül EKRANDA YOK (PANO-KARAR §88).</summary>
public sealed record Teklif(bool TeklifVar, string? Sebep, decimal? ListeFiyati, decimal? HasDegeri,
    decimal? MarjTl, decimal? IscilikTl, decimal? IndirimTutari, decimal? SatisFiyati, string? KampanyaId,
    bool SabitFiyat, decimal? IndirimOncesiFiyat);
public sealed record TevsikOnizleme(
    decimal Had, bool KuralEtkin, bool KumulatifHesaplandi,
    decimal BugunkuNakitToplam, decimal KalanNakitHakki, bool IslemTutariKuraliDevrede);
// ── İade ──
// ── P-23 FAZ 2 (R-17) — fişin KAYITLI ödeme kırılımı ────────────────────────
// ⚠ Sayfa içinde yeni istemci record AÇILMADI (denetim.sh tavanı): ortak tip
//   sözleşme tarafındadır ve sunucunun `odemeKalemleri` alanıyla ad+tip olarak
//   birebir eşleşir (tutar decimal→decimal).
/// <summary>Bir fişin tek ödeme bacağının ekrana dönen hâli.</summary>
public sealed record OdemeKalemiSatiri(
    string Arac, decimal Tutar, string ParaBirimi,
    bool Bilinmiyor, bool GeriDolduruldu, bool Aktif);

public sealed record IadeAdayi(
    string Id, DateTime Tarih, decimal TlTutari, string UrunId, string? UrunAdi,
    decimal ToplamAdet, decimal IadeEdilenAdet, decimal KalanAdet, string? CariId);

/// <summary>17.10 havaleden satış — GET /islemler/havale-bekleyen satırı (IBAN, açıklama, banka hesabı dönmez).</summary>
public sealed record HavaleSatiri(string Id, DateTime Tarih, decimal Tutar, string? KarsiTarafAd, string? CariId, string? CariAd);
