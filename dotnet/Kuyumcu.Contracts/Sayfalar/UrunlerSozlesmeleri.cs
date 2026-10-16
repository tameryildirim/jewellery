// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Urunler.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using Kuyumcu.Core.Sozlesmeler;

namespace Kuyumcu.Contracts.Sayfalar.Urunler;
// ── veri modelleri (sayfaya özel, JSON camelCase otomatik eşleşir) ──
public sealed record UrunVeri(
    string Id, string Tip, string Kod, string Ad, string AyarId,
    decimal GramAgirlik, decimal TasAgirlik, decimal HasGrami, string? Barkod, bool Aktif,
    string? TasKokeni = null, string? TasIbaresi = null,
    // P-10 (Y-8): kart galerisinin kapak görseli listede de okunur — liste
    // yanıtı varlığın tamamıdır (UrunModulu.Listele → List<Data.Urun>).
    string? GorselUrl = null,
    // S3-3: stok adedi sütunu birimi (ADET/GRAM) — sunucunun ZATEN döndürdüğü alan.
    string? Birim = null);
// S3-3: stok penceresinde ham şube kimliği yerine ŞUBE ADI (GET /subeler).
public sealed record UrunSubeVeri(string Id, string Ad);
// Adet SUNUCUDA decimal(12,3) (Entities.cs:305: [Column("adet", TypeName = "decimal(12,3)")]).
// S-2'de alan ADI düzeltilmişti ama TİP int bırakılmıştı: sunucu 5.000 döndürünce
// System.Text.Json çözümlemesi patlıyor, modal "stok yok" gösteriyor ve hata render
// ağacını öldürdüğü için sayfadaki sonraki kaydetmeler de sessizce çalışmıyordu
// (QA turu 3, /urunler, iki KRİTİK bulgu). Tip sözleşmesi: ad + TİP birlikte eşleşir.
public sealed record StokVeri(string SubeId, decimal Adet, decimal ToplamHasGram);
// Sunucu alan adı "subeStoklar" (UrunModulu.StokListele). Eskiden "Stok" bekleniyordu:
// alan null kalıyor ve modal açılışında NullReferenceException fırlıyordu (QA turu 2, S-2).
public sealed record StokYaniti(List<StokVeri>? SubeStoklar);
// PAKET ISTEK-1 — GET /urunler/stok-adetleri?ids=… (UrunStokOzetController):
// liste sütunu için ürün başına toplam stok, TEK istekte (N+1 kalktı).
// Sunucu BU tipi döndürür (derleyici kilidi — SozlesmeKapsamTestleri).
public sealed record StokAdetSatiri(string UrunId, decimal ToplamAdet, decimal ToplamHasGram);
public sealed record StokAdetleriYaniti(List<StokAdetSatiri>? Urunler);
// ── Kategori / reyon / cari kaynakları (süzgeç + form seçimleri) ──
/// <summary>
/// Kategori ağacı düğümü.
///
/// ⚠ <c>Altlar</c> NULL OLABİLİR: yaprak düğümde alan hiç gönderilmeyebilir
/// ve JSON'dan null gelir. Null gelince ağaç yürüyüşü NullReference atıp
/// EKRANIN TAMAMINI çökertiyordu — kategori isteğe bağlı bir yardımcıyken
/// ürün listesi bile açılmıyordu. Bu yüzden her erişim <c>Cocuklar</c>
/// üzerinden yapılır.
/// </summary>
public sealed record KategoriDugum(string Id, string Ad, int Sira, bool Aktif,
                                    int UrunSayisi, List<KategoriDugum>? Altlar)
{
    public List<KategoriDugum> Cocuklar => Altlar ?? [];
}
public sealed record DuzKategori(string Id, string Etiket);
public sealed record ReyonVeri(string Id, string SubeId, string Ad, string Tip, bool Aktif);
public sealed record CariOzet(string Id, string Ad);
// ── Etiket ──
public sealed record EtiketVeri(
    string UrunId, string Kod, string Ad, string Ibare, bool IbareZorunlu,
    string EtiketMetni, string? Barkod, string AyarId, decimal Milyem,
    decimal GramAgirlik, decimal HasGrami, string Dayanak);
/// <summary>Düzenleme için çekilen tam kart — listede olmayan alanlar dâhil.</summary>
public sealed record UrunTamKart(
    string Id, string Tip, string Kod, string Ad, string AyarId,
    decimal GramAgirlik, decimal TasAgirlik, decimal IscilikTl,
    string? Barkod, string? TasKokeni, string? TasIbaresi,
    string? KategoriId, string? ReyonId, decimal? SatisFiyati,
    // ⚠ Yeni istemci tipi AÇILMADI: sunucunun ZATEN döndürdüğü alanlar
    //   eklendi (GET /urunler/{id} varlığın tamamını döner).
    decimal? GercekMilyem = null, string Birim = "ADET", bool Aktif = true,
    decimal? PirlantaKarat = null, string? PirlantaRenk = null, string? PirlantaBerraklik = null,
    string? PirlantaKesim = null, string? SertifikaKurumu = null, string? SertifikaNo = null,
    // paket UX-4b madde 6: konsinye üçlüsü artık OKUNUYOR — okunmadığı sürece
    // düzenleme modalı bu alanları gösteremiyor, PATCH gövdesi de taşıyamıyordu.
    bool Konsinye = false, string? KonsinyeCariId = null, decimal? KonsinyeBirimBedel = null,
    // Paket 20 — maden ve rodyum işareti sunucunun ZATEN döndürdüğü alanlar.
    bool RodyumKaplama = false, string Maden = "ALTIN", string MadenAdi = "Altın",
    // ═══ PAKET P-10 (Y-3) — SUNUCUNUN KABUL ETTİĞİ AMA EKRANIN OKUMADIĞI ALANLAR ═══
    // GET /urunler/{id} varlığın TAMAMINI döndürüyor (UrunModulu.Getir); bu beş
    // alan kolonda ve DTO,da vardı, yalnız istemci tipi okumuyordu. Yeni istemci
    // record AÇILMADI — mevcut tip genişletildi (PLAN § 1 md.9).
    string? Aciklama = null, string? Rfid = null, string? KmtsKodu = null,
    string? GorselUrl = null, string? PariteKodu = null,
    // ═══ PAKET P-16 (Y-26) — ÜRÜN KARTININ EKSİK ALANLARI ═══════════════════
    // Yeni istemci record AÇILMADI; mevcut tip genişletildi (PLAN § 1 md.9).
    // Tip sözleşmesi sunucuyla birebir: alis_maliyeti/hedef_marj/kdv_orani/
    // min_stok SUNUCUDA decimal (Entities.cs), burada da decimal? — int
    // yazılsaydı Blazor çözümlemesi ondalık değerde patlardı (QA tur 3 dersi).
    string? TedarikciId = null, decimal? AlisMaliyeti = null, decimal? HedefMarj = null,
    decimal? KdvOrani = null, string? SeriNo = null, string? Olcu = null,
    decimal? MinStok = null,
    // M1 (16.10) — satış milyemi + fiyat ofseti (GET /urunler/{id} varlığın tamamını döner).
    decimal? SatisMilyemi = null, decimal? FiyatOfsetTl = null);
