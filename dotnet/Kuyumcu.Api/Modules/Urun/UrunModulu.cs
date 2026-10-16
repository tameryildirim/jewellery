using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// Ürün modülü — kaynak: apps/api/src/modules/urun/*
//   • Ürün tanımı (SKU), hasGrami hesabı (gram − taş) × milyem / 1000
//   • Şube bazlı stok görünümü + sayım düzeltmesi
//   • Barkodla hızlı arama (POS)
// Rota ve JSON alan adları NestJS API'siyle birebir aynı.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

/// <summary>
/// Ürün kartı açılırken girilen başlangıç stoğu (UX-4d madde 1).
/// <paramref name="SubeId"/> boşsa oturumun şubesi, o da yoksa işletmenin
/// TEK şubesi kullanılır; birden çok şube varsa şube ZORUNLUDUR (UX-S22).
/// </summary>
public sealed class AcilisStoguDto
{
    [MaxLength(36)] public string? SubeId { get; set; }

    // Range(0,001; 1.000.000) kuralı — tezgâhtar dilli iki cümleyle (AcilisAdediAttribute, aşağıda).
    [AcilisAdedi]
    public decimal Adet { get; set; }
}

/// <summary>
/// URUN-FORM-2: açılış adedi aralığı. Eski Range tek cümleyle iki durumu anlatıyordu;
/// tezgâhtar dili için iki cümle: sıfır/eksi ve çok büyük. Metin tek kaynaktan (UrunAcilisSozlugu).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AcilisAdediAttribute : ValidationAttribute
{
    public const decimal EnAz = 0.001m;
    public const decimal EnCok = Kuyumcu.Core.Domain.Girdi.AlanSinirlari.AcilisStokEnCok;   // GOC-C-2: ekranla TEK sabit

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not decimal d) return ValidationResult.Success;
        var m = Kuyumcu.Core.I18n.UrunAcilisSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli);
        string[]? uyeler = ctx.MemberName is null ? null : [ctx.MemberName];
        if (d < EnAz) return new ValidationResult(m.AdetSifirdanBuyuk, uyeler);
        if (d > EnCok) return new ValidationResult(m.AdetCokBuyuk, uyeler);
        return ValidationResult.Success;
    }
}

// PAKET PATCH-2: oluşturma da aynı kural (UX-S26 — iki form aynı alan kümesi, aynı sözleşme).
[JsonNumberHandling(JsonNumberHandling.Strict)]
public sealed class UrunOlusturDto
{
    [Required, RegularExpression("ZIYNET|KULCE|HURDA|SIKKE|PIRLANTA|SAAT|GUMUS|PLATIN|PALADYUM|CELIK|DEGERLI_TAS")]
    public string Tip { get; set; } = null!;

    /// <summary>Stok kodu / SKU</summary>
    [Required, MaxLength(AlanSinirlari.UrunKod)]
    public string Kod { get; set; } = null!;

    [Required, MaxLength(AlanSinirlari.UrunAd)]
    public string Ad { get; set; } = null!;

    [MaxLength(1000)] public string? Aciklama { get; set; }

    // UX-S31 / UX-4d / K20-8: ayar listesi TEK KAYNAK (Kuyumcu.Core Ayarlar.Tumu).
    // Desen artık ÜRETİLDİĞİ için öznitelik argümanı olarak yazılamaz
    // (const zorunluluğu) — doğrulama AyarIdAttribute ile sözlüğe bağlandı.
    [Required(ErrorMessage = "Ayar zorunludur")]
    [Kuyumcu.Core.Domain.AyarId]
    public string AyarId { get; set; } = null!;

    /// <summary>
    /// Rodyum kaplamalı mı (paket 20, K20-7). Yalnız altın/platin/paladyum
    /// madeninde işaretlenebilir; rodyum bir stok madeni DEĞİLDİR.
    /// </summary>
    public bool RodyumKaplama { get; set; }

    /// <summary>Gerçek milyem (1000 tabanlı); boşsa ayarın standart milyemi. Has hesabı buna göre.</summary>
    // PATCH-4 t2: decimal işlenenli aralık (int işlenenli Range ondalığı tamsayıya yuvarlayarak
    // sınıyordu: 1000,4 ve 0,6 geçiyordu). Asıl kapı servis içinde (GercekMilyemKapisi).
    [Range(typeof(decimal), "1", "1000")] public decimal? GercekMilyem { get; set; }

    /// <summary>Fiziksel brüt gram (taşlar dahil)</summary>
    [Required, Range(0.0001, double.MaxValue)]
    public decimal GramAgirlik { get; set; }
    /// <summary>ADET (varsayılan) · GRAM — GRAM seçilirse gram ağırlık sunucuda 1 olur (1 adet = 1 g).</summary>
    [RegularExpression("ADET|GRAM")] public string Birim { get; set; } = "ADET";

    /// <summary>Taş ağırlığı gram — has hesabından düşülür</summary>
    [Range(0, double.MaxValue)] public decimal? TasAgirlik { get; set; }
    /// <summary>
    /// Kıymetli taş kökeni — Kuyum Ticareti Yönetmeliği md.11/(e)
    /// (RG 24.06.2026/33290). TAS_YOK · DOGAL · SENTETIK · LABORATUVAR_URETIMI ·
    /// YAPAY_URETIM. <b>Taş ağırlığı varsa ZORUNLUDUR.</b>
    /// </summary>
    [RegularExpression("TAS_YOK|DOGAL|SENTETIK|LABORATUVAR_URETIMI|YAPAY_URETIM")]
    public string? TasKokeni { get; set; }

    /// <summary>
    /// Etiket/fatura/vitrinde kullanılacak ibare. Boşsa kökenin varsayılan
    /// ibaresi kullanılır. Yönetmelik "en az birine" dediği için seçilebilir.
    /// </summary>
    [MaxLength(60)] public string? TasIbaresi { get; set; }
    [Range(0.001, 999)] public decimal? PirlantaKarat { get; set; }
    [MaxLength(2)] public string? PirlantaRenk { get; set; }
    [MaxLength(5)] public string? PirlantaBerraklik { get; set; }
    [MaxLength(10)] public string? PirlantaKesim { get; set; }
    [MaxLength(40)] public string? SertifikaKurumu { get; set; }
    [MaxLength(60)] public string? SertifikaNo { get; set; }
    /// <summary>Çok seviyeli ürün ağacındaki kategori.</summary>
    [MaxLength(36)] public string? KategoriId { get; set; }

    /// <summary>Ürünün durduğu vitrin/kasa/depo.</summary>
    [MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>Konsinye mal mı — bize ait değil, satılınca sahibine borçlanılır.</summary>
    public bool Konsinye { get; set; }

    /// <summary>Konsinye malın sahibi. Konsinye ise ZORUNLU.</summary>
    [MaxLength(36)] public string? KonsinyeCariId { get; set; }

    /// <summary>Satılınca sahibine ödenecek birim bedel. Konsinye ise ZORUNLU.</summary>
    [Range(0, double.MaxValue)] public decimal? KonsinyeBirimBedel { get; set; }



    /// <summary>Sabit işçilik TL</summary>
    [Range(0, double.MaxValue)] public decimal? IscilikTl { get; set; }

    /// <summary>Opsiyonel sabit satış fiyatı; boşsa satışta hesaplanır</summary>
    [Range(0, double.MaxValue)] public decimal? SatisFiyati { get; set; }

    /// <summary>
    /// Kanonik parite kodu (CEYREK_YENI, YARIM_YENI, RESAT…). Verilmezse ürün
    /// adından otomatik tahmin edilir; hiç eşleşmezse boş kalır ve fiyat
    /// has gram × kur + işçilik ile hesaplanır.
    /// </summary>
    [MaxLength(30)] public string? PariteKodu { get; set; }

    /// <summary>KMTS benzersiz kodu — opsiyonel, doğrulanmaz (Adım 8).</summary>
    [MaxLength(100)] public string? KmtsKodu { get; set; }

    // PAKET GOC-C: barkod 60/50 açığı — ekran BarkodAlani ile AYNI Core kuralı (K18: ASCII 32–126, 1–50).
    // Uzunluk da BarkodKurali'nda (1–50, AlanSinirlari.Barkod) — ayrı MaxLength ikinci (farklı) ileti üretiyordu.
    [Barkod] public string? Barkod { get; set; }
    [MaxLength(100)] public string? Rfid { get; set; }
    [MaxLength(500)] public string? GorselUrl { get; set; }

    // ── PAKET P-16 (Y-26) — ÜRÜN KARTININ EKSİK ALANLARI ───────────────────
    // UX-S26: oluşturmada kabul edilenler düzenlemeye ERTELENMEZ; aynı küme
    // UrunGuncelleDto'da da vardır ve her ikisi de forma bağlıdır (P-10 sayacı).
    /// <summary>Malı bize satan cari. Konsinye SAHİBİNDEN farklıdır.</summary>
    [MaxLength(36)] public string? TedarikciId { get; set; }
    /// <summary>Birim alış maliyeti TL — kârlılık raporunun tek girdisi (P-25).</summary>
    // PAKET GOC-C (RAPOR-4 devri): K9a — eksi YOK, en çok 2 ondalık; ileti ekranın Para bileşeniyle AYNI sözlük anahtarı.
    [Tutar(TutarTuru.SifirOlabilir)]
    public decimal? AlisMaliyeti { get; set; }
    /// <summary>Hedef kâr marjı yüzdesi (0-500).</summary>
    [Range(0, 500, ErrorMessage = "Hedef marj 0 ile 500 arasında olmalıdır")]
    public decimal? HedefMarj { get; set; }
    /// <summary>KDV oranı yüzde — boş = ürün tipinden türet (oran koda gömülmez).</summary>
    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır")]
    public decimal? KdvOrani { get; set; }
    [MaxLength(60)] public string? SeriNo { get; set; }
    [MaxLength(20)] public string? Olcu { get; set; }
    /// <summary>Kritik stok eşiği; boş = eşik tanımlanmamış.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Kritik stok eşiği 0 veya daha büyük olmalıdır")]
    public decimal? MinStok { get; set; }

    // ═════════════════════════════════════════════════════════════════════════
    // UX-4d madde 1 — AÇILIŞ STOĞU.
    //
    // ⚠ NEDEN VAR: QA turu 1 fikstür ajanı gerçekçi veri tohumlarken ürün
    // kartının stok açma yolu OLMADIĞINI kanıtladı: POST /urunler ürünü
    // 0 stokla açıyor, hemen ardından satış denemesi
    // 400 "Bu şubede ürün stoğu bulunamadı" veriyordu. Kuyumcu, elindeki malı
    // sisteme sokmak için önce ürünü açıp sonra "sayım" yapmak zorunda kalıyordu
    // — hiçbir ekranda yazmayan gizli bir sıra (UX-S25/UX-S26).
    //
    // İKİNCİ YOL AÇILMADI: değer, ürün modülünün ZATEN VAR OLAN stok yazma yolu
    // (StokSayimGuncelle) ile yazılır — deftere HareketFabrikasi üzerinden
    // SAYIM_DUZELTME satırı düşer, maliyet partisi "maliyeti bilinmeyen" olarak
    // açılır (kâr şişmez). Açıklaması "Açılış stoğu"dur.
    //
    // DoD: bu alan SALT_OKUNUR politikasındadır — kaydedildikten sonra ürün
    // kartından değiştirilemez; stok yalnız alış, satış ve sayımla değişir.
    // ═════════════════════════════════════════════════════════════════════════
    public AcilisStoguDto? AcilisStogu { get; set; }
}

// PAKET PATCH-2 [KRİTİK]: sınıf düzeyinde Strict — TÜM sayı üyeleri (GramAgirlik, TasAgirlik,
// SatisFiyati, AlisMaliyeti, KonsinyeBirimBedel, IscilikTl, MinStok, GercekMilyem, PirlantaKarat,
// HedefMarj, KdvOrani ve ileride eklenecekler) tırnaklı dizeyi reddeder: "1.500" → 400, 1,50 DEĞİL.
// Genel serileştirici ayarı (Program.cs) da Strict; bu öznitelik ikinci savunma hattıdır.
[JsonNumberHandling(JsonNumberHandling.Strict)]
public sealed class UrunGuncelleDto
{
    // ═════════════════════════════════════════════════════════════════════════
    // PAKET PATCH-3 — İSTEĞE BAĞLI ALANLAR ÜÇ DURUMLU ("…Verildi" bayrağı).
    // ESKİ: null = koru, "" = sil (P-10 kalıbı). Ekran boşaltılan barkodu/taş ibaresini/
    // 4C alanını null gönderiyordu → sunucu "koru" anlıyordu: SİLME SESSİZCE ETKİSİZDİ.
    // YENİ (tüm API'de tek kural, PATCH-SOZLESME sözleşmesi):
    //   • gövdede YOK            → KORU
    //   • null                   → SİL (isteğe bağlı alan)
    //   • ""/boşluk              → SİL (GERİYE UYUM — bir sürüm boyunca; eski istemci)
    //   • değer                  → doğrula + yaz
    // Zorunlu alanlar (tip, kod, ad, ayarId, birim, gramAgirlik) ve bool'lar: null = KORU
    // (zorunlu alan PATCH ile silinemez; "" → 400 alan). Desen KategoriId/ReyonId ile AYNI.
    // ═════════════════════════════════════════════════════════════════════════
    [RegularExpression("ZIYNET|KULCE|HURDA|SIKKE|PIRLANTA|SAAT|GUMUS|PLATIN|PALADYUM|CELIK|DEGERLI_TAS")]
    public string? Tip { get; set; }

    [MaxLength(AlanSinirlari.UrunKod)] public string? Kod { get; set; }
    [MaxLength(AlanSinirlari.UrunAd)] public string? Ad { get; set; }
    private string? _aciklama;
    [MaxLength(1000)]
    public string? Aciklama { get => _aciklama; set { _aciklama = value; AciklamaVerildi = true; } }
    [JsonIgnore] public bool AciklamaVerildi { get; private set; }

    [Kuyumcu.Core.Domain.AyarId]
    public string? AyarId { get; set; }

    /// <summary>Rodyum kaplama işareti (paket 20). Gönderilmezse korunur.</summary>
    public bool? RodyumKaplama { get; set; }

    /// <summary>
    /// Gerçek milyem (1000 tabanlı). PAKET PATCH-4 (VERİ KRİTİK) — ÜÇ DURUMLU:
    ///   • gövdede YOK → KORU (ayar DEĞİŞMEDİYSE kayıtlı milyem; ayar değiştiyse yeni ayarın standardı)
    ///   • null        → ayarın STANDART milyemine DÖN (bilinçli boşaltma)
    ///   • değer       → yaz.
    /// ESKİ: null "verilmedi" sayılıyor, ekran her kayıtta ayarId gönderdiği için
    /// "ayar verildi → standart" dalına düşüyordu: dokunulmayan 920 milyem SESSİZCE 916
    /// oluyor, has gram ve stoktaki has toplamı değişiyordu.
    /// </summary>
    private decimal? _gercekMilyem;
    [Range(typeof(decimal), "1", "1000")]
    public decimal? GercekMilyem { get => _gercekMilyem; set { _gercekMilyem = value; GercekMilyemVerildi = true; } }
    [JsonIgnore] public bool GercekMilyemVerildi { get; private set; }

    [Range(0.0001, double.MaxValue)] public decimal? GramAgirlik { get; set; }
    [RegularExpression("ADET|GRAM")] public string? Birim { get; set; }
    [Range(0, double.MaxValue)] public decimal? TasAgirlik { get; set; }
    /// <summary>
    /// Kıymetli taş kökeni — Kuyum Ticareti Yönetmeliği md.11/(e)
    /// (RG 24.06.2026/33290). TAS_YOK · DOGAL · SENTETIK · LABORATUVAR_URETIMI ·
    /// YAPAY_URETIM. <b>Taş ağırlığı varsa ZORUNLUDUR.</b>
    /// </summary>
    [RegularExpression("TAS_YOK|DOGAL|SENTETIK|LABORATUVAR_URETIMI|YAPAY_URETIM")]
    public string? TasKokeni { get; set; }

    /// <summary>
    /// Etiket/fatura/vitrinde kullanılacak ibare. Boşsa kökenin varsayılan
    /// ibaresi kullanılır. Yönetmelik "en az birine" dediği için seçilebilir.
    /// </summary>
    private string? _tasIbaresi;
    [MaxLength(60)]
    public string? TasIbaresi { get => _tasIbaresi; set { _tasIbaresi = value; TasIbaresiVerildi = true; } }
    [JsonIgnore] public bool TasIbaresiVerildi { get; private set; }
    private decimal? _pirlantaKarat;
    [Range(0.001, 999)]
    public decimal? PirlantaKarat { get => _pirlantaKarat; set { _pirlantaKarat = value; PirlantaKaratVerildi = true; } }
    [JsonIgnore] public bool PirlantaKaratVerildi { get; private set; }
    private string? _pirlantaRenk;
    [MaxLength(2)]
    public string? PirlantaRenk { get => _pirlantaRenk; set { _pirlantaRenk = value; PirlantaRenkVerildi = true; } }
    [JsonIgnore] public bool PirlantaRenkVerildi { get; private set; }
    private string? _pirlantaBerraklik;
    [MaxLength(5)]
    public string? PirlantaBerraklik { get => _pirlantaBerraklik; set { _pirlantaBerraklik = value; PirlantaBerraklikVerildi = true; } }
    [JsonIgnore] public bool PirlantaBerraklikVerildi { get; private set; }
    private string? _pirlantaKesim;
    [MaxLength(10)]
    public string? PirlantaKesim { get => _pirlantaKesim; set { _pirlantaKesim = value; PirlantaKesimVerildi = true; } }
    [JsonIgnore] public bool PirlantaKesimVerildi { get; private set; }
    private string? _sertifikaKurumu;
    [MaxLength(40)]
    public string? SertifikaKurumu { get => _sertifikaKurumu; set { _sertifikaKurumu = value; SertifikaKurumuVerildi = true; } }
    [JsonIgnore] public bool SertifikaKurumuVerildi { get; private set; }
    private string? _sertifikaNo;
    [MaxLength(60)]
    public string? SertifikaNo { get => _sertifikaNo; set { _sertifikaNo = value; SertifikaNoVerildi = true; } }
    [JsonIgnore] public bool SertifikaNoVerildi { get; private set; }
    // ═════════════════════════════════════════════════════════════════════════
    // ÜÇ DURUMLU BAĞ ALANLARI — UX-1-B devri (paket UX-1-C1), UX-S26.
    //
    // ⚠ NEDEN DEĞİŞTİ: eski sözleşme "null = dokunma", "boş dizge = temizle"
    // idi. Ekran ise reyon seçicisi boşaltıldığında gövdeye `reyonId: null`
    // koyuyordu — kullanıcının SİLME kararı sunucuya "dokunma" diye ulaşıyor,
    // ürünün reyon bağı hiç temizlenmiyordu (UX-1-B raporu § açık kalanlar (a)).
    // Sessiz etkisizlik yasaktır. Sözleşme artık üçe ayrılır:
    //   • alan gövdede YOK      → koru  (…Gonderildi = false)
    //   • alan gövdede null/""  → sil   (bağ NULL'a çekilir)
    //   • alan gövdede değer    → ata   (doğrulanır)
    // Desen CariGuncelleDto.Aktif/AktifVerildi ile AYNIDIR; ikinci kalıp yok.
    // ═════════════════════════════════════════════════════════════════════════
    private string? _kategoriId;
    private string? _reyonId;

    /// <summary>Çok seviyeli ürün ağacındaki kategori. null gönderilirse bağ SİLİNİR.</summary>
    [MaxLength(36)]
    public string? KategoriId
    {
        get => _kategoriId;
        set { _kategoriId = value; KategoriIdGonderildi = true; }
    }

    [JsonIgnore] public bool KategoriIdGonderildi { get; private set; }

    /// <summary>Ürünün durduğu vitrin/kasa/depo. null gönderilirse bağ SİLİNİR.</summary>
    [MaxLength(36)]
    public string? ReyonId
    {
        get => _reyonId;
        set { _reyonId = value; ReyonIdGonderildi = true; }
    }

    [JsonIgnore] public bool ReyonIdGonderildi { get; private set; }

    /// <summary>
    /// Konsinye mal mı — bize ait değil, satılınca sahibine borçlanılır.
    /// <para>
    /// ⚠ <b>NULLABLE ve bu bilinçlidir (paket UX-4b madde 6).</b> Eskiden
    /// <c>bool</c> idi: gövdesinde konsinye alanı OLMAYAN bir PATCH
    /// (örn. yalnız <c>{"ad":"yeni"}</c>) sunucuya <c>false</c> olarak ulaşıyor
    /// ve <c>KonsinyeCariId</c> + <c>KonsinyeBirimBedel</c> SESSİZCE
    /// siliniyordu — başkasının malı işletme stoğuna dönüşüyordu.
    /// UX-S26 sözleşmesi gereği gönderilmeyen alan KORUNUR: bu üç alandan
    /// hiçbiri gövdede yoksa konsinye durumu hiç okunmaz.
    /// </para>
    /// </summary>
    public bool? Konsinye { get; set; }

    /// <summary>Konsinye malın sahibi. Konsinye ise ZORUNLU.</summary>
    [MaxLength(36)] public string? KonsinyeCariId { get; set; }

    /// <summary>Satılınca sahibine ödenecek birim bedel. Konsinye ise ZORUNLU.</summary>
    [Range(0, double.MaxValue)] public decimal? KonsinyeBirimBedel { get; set; }


    [Range(0, double.MaxValue)] public decimal? IscilikTl { get; set; }

    /// <summary>Opsiyonel sabit satış fiyatı; null ya da 0 = temizle (hesaplamaya dön)</summary>
    private decimal? _satisFiyati;
    [Range(0, double.MaxValue)]
    public decimal? SatisFiyati { get => _satisFiyati; set { _satisFiyati = value; SatisFiyatiVerildi = true; } }
    [JsonIgnore] public bool SatisFiyatiVerildi { get; private set; }

    /// <summary>Parite kodu. null (ya da geriye uyum: "") → eşleşme kaldırılır.</summary>
    private string? _pariteKodu;
    [MaxLength(30)]
    public string? PariteKodu { get => _pariteKodu; set { _pariteKodu = value; PariteKoduVerildi = true; } }
    [JsonIgnore] public bool PariteKoduVerildi { get; private set; }

    /// <summary>KMTS benzersiz kodu — opsiyonel. null (ya da "") → temizlenir.</summary>
    private string? _kmtsKodu;
    [MaxLength(100)]
    public string? KmtsKodu { get => _kmtsKodu; set { _kmtsKodu = value; KmtsKoduVerildi = true; } }
    [JsonIgnore] public bool KmtsKoduVerildi { get; private set; }
    private string? _barkod;
    [Barkod]   // PAKET GOC-C: K18 (uzunluk dahil) — POST ile aynı tek ileti
    public string? Barkod { get => _barkod; set { _barkod = value; BarkodVerildi = true; } }
    [JsonIgnore] public bool BarkodVerildi { get; private set; }
    private string? _rfid;
    [MaxLength(100)]
    public string? Rfid { get => _rfid; set { _rfid = value; RfidVerildi = true; } }
    [JsonIgnore] public bool RfidVerildi { get; private set; }
    private string? _gorselUrl;
    [MaxLength(500)]
    public string? GorselUrl { get => _gorselUrl; set { _gorselUrl = value; GorselUrlVerildi = true; } }
    [JsonIgnore] public bool GorselUrlVerildi { get; private set; }
    public bool? Aktif { get; set; }

    // ── PAKET P-16 (Y-26) — ÜRÜN KARTININ EKSİK ALANLARI ───────────────────
    // ÜÇ DURUMLU: gövdede yok → koru · null → sil · değer → ata. Desen
    // KategoriId/ReyonId ile AYNIDIR; ikinci bir kalıp açılmadı.
    private string? _tedarikciId;
    private decimal? _alisMaliyeti;
    private decimal? _hedefMarj;
    private decimal? _kdvOrani;
    private string? _seriNo;
    private string? _olcu;
    private decimal? _minStok;

    /// <summary>Malı bize satan cari. null gönderilirse bağ SİLİNİR.</summary>
    [MaxLength(36)]
    public string? TedarikciId { get => _tedarikciId; set { _tedarikciId = value; TedarikciIdGonderildi = true; } }
    [JsonIgnore] public bool TedarikciIdGonderildi { get; private set; }

    [Tutar(TutarTuru.SifirOlabilir)]   // PAKET GOC-C (RAPOR-4 devri): POST ile aynı K9a kuralı
    public decimal? AlisMaliyeti { get => _alisMaliyeti; set { _alisMaliyeti = value; AlisMaliyetiGonderildi = true; } }
    [JsonIgnore] public bool AlisMaliyetiGonderildi { get; private set; }

    [Range(0, 500, ErrorMessage = "Hedef marj 0 ile 500 arasında olmalıdır")]
    public decimal? HedefMarj { get => _hedefMarj; set { _hedefMarj = value; HedefMarjGonderildi = true; } }
    [JsonIgnore] public bool HedefMarjGonderildi { get; private set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır")]
    public decimal? KdvOrani { get => _kdvOrani; set { _kdvOrani = value; KdvOraniGonderildi = true; } }
    [JsonIgnore] public bool KdvOraniGonderildi { get; private set; }

    [MaxLength(60)]
    public string? SeriNo { get => _seriNo; set { _seriNo = value; SeriNoGonderildi = true; } }
    [JsonIgnore] public bool SeriNoGonderildi { get; private set; }

    [MaxLength(20)]
    public string? Olcu { get => _olcu; set { _olcu = value; OlcuGonderildi = true; } }
    [JsonIgnore] public bool OlcuGonderildi { get; private set; }

    [Range(0, double.MaxValue, ErrorMessage = "Kritik stok eşiği 0 veya daha büyük olmalıdır")]
    public decimal? MinStok { get => _minStok; set { _minStok = value; MinStokGonderildi = true; } }
    [JsonIgnore] public bool MinStokGonderildi { get; private set; }
}

/// <summary>PATCH {id}/stok/{subeId} gövdesi — Nest'teki @Body('adet').
/// PAKET PATCH-SOZLESME (02.10): Adet eskiden `decimal` idi → gövdede adet YOKSA 0
/// bağlanıyor ve stok SESSİZCE SIFIRLANIP deftere sayım düzeltmesi yazılıyordu.
/// Bu tek alanlı bir EYLEM ucudur (sayılan adet); adet gelmezse 400 + alan mesajı.</summary>
public sealed class StokSayimDto
{
    // PATCH-SOZLESME tur 2: tırnaklı sayı ("45.000", "1.500") REDDEDİLİR (400) —
    // Web varsayılanı dizeyi invariant okuyup 45 bini 45 yapıyordu. Eksi/üst
    // sınır servis içinde elle denetlenir (SayimModulu da aynı servisi çağırır).
    // PAKET GOC-C: aralık ekranla aynı (≥ 0, decimal(12,3) tavanı); ADET birimli üründe tam sayı servis içinde (UrunAdetKapisi).
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Adet { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class UrunServisi(KuyumcuDbContext db, DenetimServisi? denetim = null, EtiketAyarlari? etiketAyarlari = null,
    YasalParametreServisi? yasal = null)
{
    /// <summary>
    /// Paket 25 — sayım düzeltmesinin reyon bacağı. Sayım `stoklar`ı üzerine
    /// yazar; yerleşim kırılımı da aynı yönde düzeltilmezse I1 (Σ yerleşim ≤
    /// stok) sayım anında bozulurdu.
    /// </summary>
    private readonly Modules.Yerlesim.YerlesimServisi yerlesim = new(db);

    // ─── Yardımcı: ayarId → hasGrami hesabı ──────────────────────────────────

    private static Ayar AyarBul(string ayarId) =>
        Ayarlar.Bul(ayarId) ?? throw new BadRequestException($"Geçersiz ayar: {ayarId}");

    /// <summary>
    /// Has gram = (gram − taş) × milyem/1000. gercekMilyem verilirse imalattaki
    /// fiili saflık kullanılır, yoksa ayar standardı.
    /// </summary>
    private static decimal HesaplaHasGrami(decimal gramAgirlik, decimal tasAgirlik, string ayarId, decimal? gercekMilyem = null)
    {
        var ayar = AyarBul(ayarId); // geçersiz ayar → 400
        // K20-5: kıymetsiz ayarda (çelik) has hesaplanmaz — elle girilen "gerçek
        // milyem" bile sahte has üretemez; ToHasGram HasHesaplanir'a bakar.
        var efektifAyar = gercekMilyem.HasValue && ayar.HasHesaplanir
            ? ayar with { Milyem = gercekMilyem.Value }
            : ayar;
        return HasHesap.ToHasGram(gramAgirlik - tasAgirlik, efektifAyar);
    }

    /// <summary>
    /// Parite kodunu çözer. Verildiyse geçerliliği doğrulanır, verilmediyse BOŞ kalır.
    ///
    /// GERİ ALINDI (Kurucu kararı, 2026-09-11): ürün adından OTOMATİK tahmin
    /// kapatıldı. Sebep: kaynağın kotasyonu toptancının fiyatıdır, dükkânın satış
    /// fiyatı değildir; marj alanı olmadan ürüne fiyat olarak bağlanması yanlış
    /// vaat üretiyordu. Ayrıca ada dayalı tahmin ("Ata Lira Kolye") sessizce
    /// yanlış etiketleyebiliyordu. Alan ve doğrulama uykuda bırakıldı; marj ve
    /// alış/satış modeli gelince (Faz 1) yeniden açılacak.
    /// Tahmin fonksiyonu (Kur.PariteKodlari.Tahmin) korunuyor, çağrılmıyor.
    /// </summary>
    private static string? PariteCoz(string? istenen, string ad, string? tip)
    {
        if (string.IsNullOrWhiteSpace(istenen)) return null;
        if (!Kur.PariteKodlari.Gecerli(istenen))
            throw new BadRequestException($"Geçersiz parite kodu: {istenen}");
        return istenen;
    }

    // ─── Ürün oluştur ─────────────────────────────────────────────────────────

    /// <summary>
    /// PATCH-SOZLESME tur 3 (PATCH) + PAKET PATCH-3 (POST): ürün sayı kolonlarının kapasite
    /// kapısı — oluşturma ve düzenleme AYNI kapıdan geçer (UX-S26). DTO Range üst sınırı
    /// double.MaxValue → taşan değer veritabanında 1264 → 500 dönüyordu.
    /// gram_agirlik / tas_agirlik decimal(12,4) · satis_fiyati / alis_maliyeti /
    /// konsinye_birim_bedel decimal(15,2) · iscilik_tl decimal(12,2) · min_stok decimal(12,3).
    /// </summary>
    internal static void UrunKolonKapisi(decimal? gram, decimal? tas, decimal? satisFiyati,
        decimal? alisMaliyeti, decimal? konsinyeBedel, decimal? iscilik, decimal? minStok)
    {
        KolonTavani.Denetle(gram, 4, KolonTavani.D12_4, "gramAgirlik", "Gram ağırlık");
        KolonTavani.Denetle(tas, 4, KolonTavani.D12_4, "tasAgirlik", "Taş ağırlığı");
        KolonTavani.Denetle(satisFiyati, 2, KolonTavani.D15_2, "satisFiyati", "Satış fiyatı");
        KolonTavani.Denetle(alisMaliyeti, 2, KolonTavani.D15_2, "alisMaliyeti", "Alış maliyeti");
        KolonTavani.Denetle(konsinyeBedel, 2, KolonTavani.D15_2, "konsinyeBirimBedel", "Konsinye birim bedeli");
        KolonTavani.Denetle(iscilik, 2, KolonTavani.D12_2, "iscilikTl", "İşçilik");
        KolonTavani.Denetle(minStok, 3, KolonTavani.D12_3, "minStok", "Kritik stok eşiği");
    }

    /// <summary>
    /// PAKET PATCH-4 t2 (hasım KRİTİK): gerçek milyem SERVİS İÇİ kapısı — DTO DataAnnotations
    /// yalnız controller yolunda koşar ve int işlenenli Range ondalığı yuvarlayarak sınıyordu
    /// (1000,4 → has brüt gramı aşıyordu = sahte has). Verildiyse 1 ≤ m ≤ 1000 ve en çok
    /// 2 ondalık (kolon decimal(7,2)); değilse 400 alan:gercekMilyem. null/verilmedi serbest.
    /// </summary>
    public static void GercekMilyemKapisi(decimal? milyem)
    {
        if (milyem is not { } m) return;
        if (m < 1m || m > 1000m)
            throw new BadRequestException("Gerçek milyem 1 ile 1000 arasında olmalı").AlanIle("gercekMilyem");
        if (decimal.Round(m, 2) != m)
            throw new BadRequestException("Gerçek milyem en çok 2 ondalık basamak taşıyabilir").AlanIle("gercekMilyem");
    }

    /// <summary>
    /// PAKET GOC-C — ürün kartı GİRDİ kapısı (servis yolu; DTO öznitelikleri yalnız controller yolunda koşar).
    /// POST ve PATCH AYNI kapıdan geçer (UX-S26). Değerler PATCH'te kayıtlı + gelen BİRLEŞİMİDİR.
    /// <list type="bullet">
    /// <item>#5 taş ağırlığı brüt gramdan KÜÇÜK (K10) — eşit/büyükse has gram eksi ya da sıfır hesaplanırdı.</item>
    /// <item>RAPOR-4 devri: alış maliyeti, satış fiyatı, işçilik, konsinye bedeli eksi olamaz (K9a) — servis yolunda da.</item>
    /// <item>#6 ADET birimli üründe kritik stok eşiği tam sayı (K14).</item>
    /// <item>Barkod K18 (ASCII 32–126, 1–50) — servis yolunda da.</item>
    /// </list>
    /// </summary>
    public static void GocCKapisi(string? birim, decimal gram, decimal tas, decimal? satisFiyati,
        decimal? alisMaliyeti, decimal? konsinyeBedel, decimal? iscilik, decimal? minStok, string? barkod)
    {
        if (!string.Equals(birim, UrunAdetKapisi.GramBirimi, StringComparison.Ordinal) && tas > 0m && tas >= gram)
            GirdiKapisi.Denetle(new GirdiHatasi(GirdiKodu.TasBrut), "tasAgirlik");
        GirdiKapisi.Tutar(alisMaliyeti, "alisMaliyeti", TutarTuru.SifirOlabilir);
        GirdiKapisi.Tutar(satisFiyati, "satisFiyati", TutarTuru.SifirOlabilir);
        GirdiKapisi.Tutar(iscilik, "iscilikTl", TutarTuru.SifirOlabilir);
        GirdiKapisi.Tutar(konsinyeBedel, "konsinyeBirimBedel", TutarTuru.SifirOlabilir);
        UrunAdetKapisi.SifirOlabilirDogrula(birim, minStok, "minStok");
        if (!string.IsNullOrWhiteSpace(barkod))
            GirdiKapisi.Denetle(BarkodKurali.Denetle(barkod.Trim()), "barkod");
    }

    public async Task<object> Olustur(UrunOlusturDto dto, AktifKullanici islem)
    {
        GercekMilyemKapisi(dto.GercekMilyem);   // PATCH-4 t2: servis içi aralık kapısı
        // PAKET PATCH-3: POST kolon tavanı (PATCH ile ortak kapı) — 500 değil 400 alan.
        UrunKolonKapisi(dto.GramAgirlik, dto.TasAgirlik, dto.SatisFiyati, dto.AlisMaliyeti,
            dto.KonsinyeBirimBedel, dto.IscilikTl, dto.MinStok);
        // PAKET GOC-C: taş < brüt (#5), eksi tutar (RAPOR-4), ADET eşiği (#6), barkod (K18).
        GocCKapisi(dto.Birim, dto.GramAgirlik, dto.TasAgirlik ?? 0m, dto.SatisFiyati, dto.AlisMaliyeti,
            dto.KonsinyeBirimBedel, dto.IscilikTl, dto.MinStok, dto.Barkod);
        if (dto.AcilisStogu is { Adet: > 0m } acilisGirdi)
            UrunAdetKapisi.Dogrula(dto.Birim, acilisGirdi.Adet, AcilisAlani);
        // PAKET P-URUN (K6, fail-closed): ayarın varsayılanı YOK. DataAnnotations yalnız
        // controller yolunda işler; servis içi yol da ayarsız ürünü 400 + alan ile reddeder
        // (Ayarlar.Bul(null) 500'e düşüyordu).
        if (string.IsNullOrWhiteSpace(dto.AyarId))
            throw new BadRequestException(Kuyumcu.Core.I18n.PUrunSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).AyarZorunlu)
                .AlanIle("ayarId");
        var ayar = AyarBul(dto.AyarId);

        // Stok kodu tenant içinde benzersiz olmalı
        var mevcutKod = await db.Urunler.AnyAsync(u =>
            u.TenantId == islem.TenantId && u.Kod == dto.Kod);
        if (mevcutKod) throw new AlanCakismasiException("kod", $"'{dto.Kod}' kodu zaten kullanımda");

        var tasAgirlik    = dto.TasAgirlik ?? 0m;

        // Kuyum Ticareti Yönetmeliği md.11/(e) — taşlı ürünün kökeni ZORUNLU.
        TasKapisi.Dogrula(tasAgirlik, dto.TasKokeni, dto.TasIbaresi);
        PirlantaKapisi.Dogrula(dto.Tip, dto.PirlantaKarat, dto.PirlantaRenk, dto.PirlantaBerraklik, dto.PirlantaKesim, dto.SertifikaKurumu, dto.SertifikaNo);

        // Konsinye mal bize ait değildir; sahibi ve bedeli olmadan kaydedilemez.
        // K20-5/6/7 — tip ile ayarın madeni tutarlı mı, çelikte fiyat var mı,
        // rodyum işareti anlamlı mı? Fail-closed.
        MadenKapisi.UrunDogrula(dto.Tip, dto.AyarId, dto.RodyumKaplama, dto.SatisFiyati);
        KonsinyeKapisi.Dogrula(dto.Konsinye, dto.KonsinyeCariId, dto.KonsinyeBirimBedel);
        if (dto.Konsinye)
            await IslemDogrulayici.CariDogrula(db, dto.KonsinyeCariId, islem.TenantId);
        await KategoriDogrula(dto.KategoriId, islem.TenantId);
        await ReyonDogrula(dto.ReyonId, islem.TenantId);
        // PAKET P-16 (Y-26): tedarikçi de yabancı kimliktir — K1 kapısından geçer.
        await IslemDogrulayici.CariDogrula(db, dto.TedarikciId, islem.TenantId);
        var efektifMilyem = dto.GercekMilyem ?? ayar.Milyem;
        // GRAM birimi: 1 adet = 1 gram sözleşmesi — istemcinin gönderdiği gram/taş yoksayılır.
        var gramAgirlik   = dto.Birim == "GRAM" ? 1m : dto.GramAgirlik;
        if (dto.Birim == "GRAM") tasAgirlik = 0m;
        var hasGrami      = HesaplaHasGrami(gramAgirlik, tasAgirlik, dto.AyarId, dto.GercekMilyem);
        var now           = DateTime.UtcNow;
        var id            = Kimlik.Ulid();

        // Elle girilen barkod başka bir ürüne aitse okuyucu yanlış ürünü getirir.
        if (!string.IsNullOrWhiteSpace(dto.Barkod))
        {
            var barkodCakisan = await db.Urunler.AnyAsync(u =>
                u.TenantId == islem.TenantId && u.Barkod == dto.Barkod.Trim());
            if (barkodCakisan)
                // P-05 (Y-2): aynı cümle, artık ALAN ALTI — hangi alanı düzelteceği belli.
                throw new AlanCakismasiException("barkod", $"'{dto.Barkod.Trim()}' barkodu zaten kullanımda");
        }

        // ── PAKET URUN-ACILIS-ATOMIK — ürün kartı + açılış stoğu TEK DB İŞLEMİ ──
        // KÖK NEDEN (08.10): kart burada SaveChanges ile commit ediliyor, açılış
        // stoğu ardından AYRI bir transaction'da yazılıyordu. İkinci adım reddedince
        // (ör. yeni kiracıda kur yok → 400) kart veritabanında KALIYOR, tekrar deneme
        // 409 'kod kullanımda' alıyor, satış aramasında 'Stokta yok' ikizi çıkıyordu.
        // Şimdi: işlem burada açılır (dış işlem varsa ona katılır — VeriIslemi),
        // herhangi bir adım reddederse `await using` geri alır ve HİÇBİR satır kalmaz.
        await using var tx = await db.Database.IsBaslatVeyaDevral();

        db.Urunler.Add(new Data.Urun
        {
            Id          = id,
            TenantId    = islem.TenantId,
            Tip         = dto.Tip,
            Kod         = dto.Kod,
            Ad          = dto.Ad,
            Aciklama    = GorselKapisi.Bosla(dto.Aciklama),
            AyarId      = dto.AyarId,
            Milyem      = efektifMilyem,
            GramAgirlik = gramAgirlik,
            Birim       = dto.Birim,
            TasAgirlik  = tasAgirlik,
            TasKokeni   = dto.TasKokeni ?? TasKokeni.TasYok,
            RodyumKaplama = dto.RodyumKaplama,
            TasIbaresi  = dto.TasIbaresi?.Trim(),
            PirlantaKarat = dto.PirlantaKarat, PirlantaRenk = dto.PirlantaRenk, PirlantaBerraklik = dto.PirlantaBerraklik, PirlantaKesim = dto.PirlantaKesim, SertifikaKurumu = dto.SertifikaKurumu, SertifikaNo = dto.SertifikaNo?.Trim(),
            KategoriId  = dto.KategoriId,
            ReyonId     = dto.ReyonId,
            Konsinye    = dto.Konsinye,
            KonsinyeCariId = dto.KonsinyeCariId,
            KonsinyeBirimBedel = dto.KonsinyeBirimBedel,
            HasGrami    = hasGrami,
            IscilikTl   = dto.IscilikTl ?? 0m,
            SatisFiyati = dto.SatisFiyati,
            PariteKodu  = PariteCoz(dto.PariteKodu, dto.Ad, dto.Tip),
            KmtsKodu    = KmtsKodu.Dogrula(dto.KmtsKodu, yasal),   // YASAL-4 Y4-C: tek doğrulama yeri
            // Barkod verilmediyse ÜRETİLİR (PLAN-2 Adım 5). Etiket basmak için
            // her ürünün bir barkodu olmalı; kullanıcıdan istemek yerine ULID'den
            // türetiyoruz — çakışma riski ULID'in rastgele kısmı kadar düşük.
            Barkod      = string.IsNullOrWhiteSpace(dto.Barkod)
                ? Kuyumcu.Core.Domain.BarkodUretici.Uret(id)
                : dto.Barkod.Trim(),
            // P-10 (Y-3): boş dizge = "yok" demektir; kolona "" yazılmaz.
            Rfid        = GorselKapisi.Bosla(dto.Rfid),
            // P-10 (Y-3): adres kapalı listeden geçer (http/https) — bkz. GorselKapisi.
            GorselUrl   = GorselKapisi.Dogrula(dto.GorselUrl),
            // ── PAKET P-16 (Y-26) ────────────────────────────────────────────
            TedarikciId   = string.IsNullOrWhiteSpace(dto.TedarikciId) ? null : dto.TedarikciId,
            AlisMaliyeti  = dto.AlisMaliyeti,
            HedefMarj     = dto.HedefMarj,
            KdvOrani      = dto.KdvOrani,
            SeriNo        = GorselKapisi.Bosla(dto.SeriNo),
            Olcu          = GorselKapisi.Bosla(dto.Olcu),
            MinStok       = dto.MinStok,
            Aktif       = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (TekillikIhlaliMi(ex))
        {
            // Eşzamanlı çift gönderim: ön kontrolü iki istek birden geçtiyse
            // UQ_urunler_tenant_kod / _barkod son savunmadır — 500 değil 409 alan.
            throw new AlanCakismasiException("kod", $"'{dto.Kod}' kodu zaten kullanımda");
        }

        // ── UX-4d madde 1: açılış stoğu ──────────────────────────────────────
        if (dto.AcilisStogu is { Adet: > 0m } acilis)
        {
            var subeId = await AcilisSubesiCoz(acilis.SubeId, islem);
            // URUN-ACILIS-ATOMIK tur 2 (tasarım hakemi KRİTİK): ürün kartı yolunun
            // reddi SAYIM diliyle değil TEZGÂHTAR diliyle ve açılış stoğu ALANIYLA
            // döner — ekran kutuyu kırmızı yapar, odağı oraya götürür. Kapılar
            // StokSayimGuncelle'nin kapılarıyla AYNI ölçüdür (kopya değil, ön söz):
            // son söz yine oradadır. Kart bu noktada yazılmış ama COMMIT EDİLMEMİŞTİR
            // — red işlemi geri alır (0 satır).
            var acilisMetin = Kuyumcu.Core.I18n.UrunAcilisSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli);
            var acilisAdet  = Math.Round(acilis.Adet, 3, MidpointRounding.AwayFromZero);
            if (acilisAdet > StokAdetTavani || Math.Abs(acilisAdet * hasGrami) > HasGramTavani)
                throw new BadRequestException(acilisMetin.AcilisCokBuyuk).AlanIle(AcilisAlani);
            if (!await db.KurSnapshots.AsNoTracking().AnyAsync(k => k.TenantId == islem.TenantId))
                throw new BadRequestException(acilisMetin.AcilisKurYok).AlanIle(AcilisAlani);
            await StokSayimGuncelle(id, subeId, acilis.Adet, islem,
                aciklama: $"Açılış stoğu: {acilis.Adet:0.###} adet (ürün kartı)");
        }

        if (tx is not null) await tx.CommitAsync();   // URUN-ACILIS-ATOMIK: tek commit
        return await Getir(id, islem);
    }

    /// <summary>
    /// Açılış stoğunun yazılacağı şube (UX-S21 + UX-S22): sistemin bildiği alan
    /// kullanıcıdan istenmez. Sıra: gövdedeki şube → oturumun şubesi → işletmenin
    /// TEK aktif şubesi. Birden çok şube varsa ve hiçbiri belli değilse TAHMİN
    /// EDİLMEZ, açık hata verilir (Anayasa md.1).
    /// </summary>
    /// <summary>MySQL benzersizlik ihlali (1062) mi — başka bir DB hatası 409'a çevrilmez.</summary>
    internal static bool TekillikIhlaliMi(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true;

    private async Task<string> AcilisSubesiCoz(string? subeId, AktifKullanici islem)
    {
        if (!string.IsNullOrWhiteSpace(subeId)) return subeId!;
        if (!string.IsNullOrWhiteSpace(islem.SubeId)) return islem.SubeId!;

        var aktifSubeler = await db.Subeler.AsNoTracking()
            .Where(x => x.TenantId == islem.TenantId && x.Aktif)
            .Select(x => x.Id)
            .Take(2)
            .ToListAsync();

        if (aktifSubeler.Count == 1) return aktifSubeler[0];
        if (aktifSubeler.Count == 0)
            throw new BadRequestException(
                "Açılış stoğu yazılamadı: işletmede aktif şube yok. Önce İşletme ekranından şube açın.")
                .AlanIle(AcilisAlani);
        throw new BadRequestException(
            "Açılış stoğu için şube seçilmelidir — işletmede birden çok aktif şube var.")
            .AlanIle(AcilisAlani);
    }

    /// <summary>
    /// URUN-ACILIS-ATOMIK tur 2: ürün kartı yolunda açılış stoğu reddinin TEK alan
    /// adı (ekran: u-acilis-stok). Sayım ucunun "adet" alanıyla karışmaz.
    /// </summary>
    internal const string AcilisAlani = "acilisStogu.adet";

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<List<Data.Urun>> Listele(
        AktifKullanici islem,
        string? tip, string? ayarId, bool? aktif, string? q,
        int limit, int offset, string? tasKategori = null,
        string? kategoriId = null, string? reyonId = null, bool? konsinye = null,
        string? maden = null, string? siralama = null)
    {
        // PAKET SATIS-3 (doğrulama bulgusu): işlem ekranının boş odak listesi "en çok satılan"
        // ürünleri ister. Geçersiz sıralama sessizce koda düşmez — 400 (Anayasa md.1).
        if (!string.IsNullOrEmpty(siralama) && siralama != SiralamaCokSatan)
            throw new BadRequestException($"Geçersiz sıralama: '{siralama}'. Geçerli: {SiralamaCokSatan}");
        var sorgu = db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId);

        if (!string.IsNullOrEmpty(tip))    sorgu = sorgu.Where(u => u.Tip == tip);
        if (!string.IsNullOrEmpty(ayarId)) sorgu = sorgu.Where(u => u.AyarId == ayarId);
        // PAKET 20 — maden filtresi. Maden ürün kartında SAKLANMAZ (ayardan
        // türetilir, K20-8); filtre o madenin ayar kümesine çevrilir ve
        // sunucuda süzülür — liste belleğe çekilip orada filtrelenmez (TB-19).
        if (!string.IsNullOrEmpty(maden))
        {
            var madeninAyarlari = Ayarlar.MadeninAyarlari(maden).Select(a => a.Id).ToList();
            sorgu = sorgu.Where(u => madeninAyarlari.Contains(u.AyarId));
        }
        if (aktif is not null)             sorgu = sorgu.Where(u => u.Aktif == aktif);
        if (!string.IsNullOrEmpty(q))
            sorgu = sorgu.Where(u =>
                EF.Functions.Like(u.Ad, $"%{q}%") || EF.Functions.Like(u.Kod, $"%{q}%")
                // PAKET SATIS-3 (KAYNAK 0): işlem ekranının TEK arama kutusu barkodla da bulur
                // (öneki yeter — okuyucu dizisi tamamlanmadan liste daralır).
                || (u.Barkod != null && EF.Functions.Like(u.Barkod, $"{q}%")));

        // Kategori filtresi ALT AĞACI DA kapsar: "Ziynet" seçilince altındaki
        // bütün alt kategoriler gelir — yoksa ağaç işe yaramaz.
        if (!string.IsNullOrEmpty(kategoriId))
        {
            var altAgac = await new KategoriServisi(db).AltAgacIdleri(kategoriId, islem.TenantId);
            sorgu = sorgu.Where(u => u.KategoriId != null && altAgac.Contains(u.KategoriId));
        }
        if (!string.IsNullOrEmpty(reyonId)) sorgu = sorgu.Where(u => u.ReyonId == reyonId);
        if (konsinye is not null)           sorgu = sorgu.Where(u => u.Konsinye == konsinye.Value);

        // md.11/(e): doğal ve yapay taşlar internet ortamında AYRI KATEGORİLERDE
        // sunulmak zorunda. Vitrin/e-ticaret bu filtreyle ayrılır.
        if (!string.IsNullOrEmpty(tasKategori))
        {
            // ⚠ KÖKENİ BOŞ kayıt "taşsız" DEĞİLDİR. Göç öncesi taşlı ürünlerin
            // kökeni NULL'dır; bunları TASSIZ kategorisine karıştırmak yönetmeliğin
            // ayrı-kategori kuralını sessizce delerdi. Ayrı bir kova verilir.
            sorgu = tasKategori switch
            {
                "DOGAL_TAS" => sorgu.Where(u => u.TasKokeni == TasKokeni.Dogal),
                "YAPAY_TAS" => sorgu.Where(u =>
                    u.TasKokeni == TasKokeni.Sentetik ||
                    u.TasKokeni == TasKokeni.LaboratuvarUretimi ||
                    u.TasKokeni == TasKokeni.YapayUretim),
                "TASSIZ" => sorgu.Where(u =>
                    u.TasKokeni == TasKokeni.TasYok ||
                    (u.TasKokeni == null && (u.TasAgirlik == null || u.TasAgirlik == 0m))),
                // Eksik veri kovası: satışa çıkamayacak ürünler. İşletme bunları
                // görüp kökenini girmek zorunda.
                "KOKEN_EKSIK" => sorgu.Where(u =>
                    u.TasAgirlik > 0m &&
                    (u.TasKokeni == null || u.TasKokeni == TasKokeni.TasYok)),
                _ => throw new BadRequestException(
                    $"Geçersiz taş kategorisi: '{tasKategori}'. " +
                    "DOGAL_TAS / YAPAY_TAS / TASSIZ / KOKEN_EKSIK"),
            };
        }

        if (siralama == SiralamaCokSatan)
        {
            // Son CokSatanGun gündeki SATIS hareketi sayısına göre; eşitlikte koda göre (kararlı).
            var sinir = DateTime.UtcNow.AddDays(-CokSatanGun);
            var tenantId = islem.TenantId;
            return await sorgu
                .OrderByDescending(u => db.Hareketler.Count(h =>
                    h.TenantId == tenantId && h.UrunId == u.Id && h.Tur == "SATIS" && h.Tarih >= sinir))
                .ThenBy(u => u.Kod)
                .Skip(offset).Take(limit).ToListAsync();
        }
        return await sorgu.OrderBy(u => u.Kod).Skip(offset).Take(limit).ToListAsync();
    }

    /// <summary>GET /urunler?siralama=cok-satan — son <see cref="CokSatanGun"/> günün satış sayısına göre.</summary>
    public const string SiralamaCokSatan = "cok-satan";
    public const int CokSatanGun = 90;

    /// <summary>Kategori bu kiracıya ait ve aktif mi?</summary>
    private async Task KategoriDogrula(string? kategoriId, string tenantId)
    {
        if (string.IsNullOrEmpty(kategoriId)) return;
        var var_ = await db.Set<Data.UrunKategorisi>().AsNoTracking()
            .AnyAsync(k => k.Id == kategoriId && k.TenantId == tenantId && k.Aktif);
        if (!var_) throw new NotFoundException("Ürün kategorisi bulunamadı");
    }

    /// <summary>Reyon bu kiracıya ait ve aktif mi?</summary>
    private async Task ReyonDogrula(string? reyonId, string tenantId)
    {
        if (string.IsNullOrEmpty(reyonId)) return;
        var var_ = await db.Set<Data.Reyon>().AsNoTracking()
            .AnyAsync(r => r.Id == reyonId && r.TenantId == tenantId && r.Aktif);
        if (!var_) throw new NotFoundException("Reyon bulunamadı");
    }

    // ─── Etiket / ibare ───────────────────────────────────────────────────────
    /// <summary>
    /// Etiket, ürün sertifikası, fatura, internet sayfası ve tanıtım
    /// materyallerinde kullanılacak zorunlu metin — md.11/(e).
    /// Doğal taş ve taşsız üründe <c>ibare</c> boştur.
    /// </summary>
    public async Task<object> Etiket(string id, AktifKullanici islem)
    {
        var urun = await TenantUrunuBul(id, islem.TenantId);
        var ibare = TasKokeni.Ibare(urun.TasKokeni, urun.TasIbaresi);
        return new
        {
            urunId       = urun.Id,
            kod          = urun.Kod,
            ad           = urun.Ad,
            tasAgirlik   = urun.TasAgirlik,
            tasKokeni    = urun.TasKokeni,
            pirlantaKarat = urun.PirlantaKarat, pirlantaRenk = urun.PirlantaRenk, pirlantaBerraklik = urun.PirlantaBerraklik, pirlantaKesim = urun.PirlantaKesim,
            sertifikaKurumu = urun.SertifikaKurumu, sertifikaNo = urun.SertifikaNo, pirlantaOzet = Kuyumcu.Core.Domain.Pirlanta.Ozet(urun.PirlantaKarat, urun.PirlantaRenk, urun.PirlantaBerraklik, urun.PirlantaKesim),
            tasKategori  = TasKokeni.Kategori(urun.TasKokeni),
            ibare        = ibare,
            ibareZorunlu = TasKokeni.IbareGerekir(urun.TasKokeni),
            // Etikete basılacak tam satır. İbare zorunluysa ürün adının yanında
            // açıkça görünür — yönetmelik "açıkça yer vermeksizin satışa sunamaz".
            etiketMetni  = ibare.Length == 0 ? urun.Ad : $"{urun.Ad} ({ibare})",
            // PLAN-2 Adım 5 — etiket basımı için gereken alanlar
            barkod       = urun.Barkod,
            ayarId       = urun.AyarId,
            // PAKET 20 — etikette maden + milyem ("Platin 950") ve rodyum ibaresi.
            maden        = urun.Maden,
            madenAdi     = urun.MadenAdi,
            madenIbaresi = $"{urun.MadenAdi} {urun.Milyem:0.##}",
            rodyumKaplama = urun.RodyumKaplama,
            // ⚠ İbarenin ZORUNLU olduğu DOĞRULANMADI → yapılandırmadan okunur,
            // varsayılan KAPALI (ADR-0005). Kapalıyken etikete BASILMAZ.
            rodyumIbaresi = urun.RodyumKaplama && (etiketAyarlari?.RodyumIbaresiGoster ?? false)
                ? (etiketAyarlari!.RodyumIbaresi) : null,
            milyem       = urun.Milyem,
            gramAgirlik  = urun.GramAgirlik,
            birim        = urun.Birim,
            hasGrami     = urun.HasGrami,
            iscilikTl    = urun.IscilikTl,
            satisFiyati  = urun.SatisFiyati,
            dayanak      = "Kuyum Ticareti Yönetmeliği md.11/(e) — RG 24.06.2026/33290",
        };
    }

    /// <summary>
    /// Maden + ayar (saflık/milyem) tablosu — <c>Ayarlar.Tumu</c>'den ÜRETİLİR.
    /// Ekranlar ve harici istemciler bunu kullanır; sabit liste kopyalanmaz.
    /// </summary>
    public object MadenTablosu() => new
    {
        madenler = MadenListesi(),
        // Etiket ibare politikası — ekran kendi başına karar vermesin (ADR-0005:
        // doğrulanmamış mevzuat iddiası koda gömülmez; varsayılan KAPALI).
        etiket = new
        {
            rodyumIbaresiGoster = etiketAyarlari?.RodyumIbaresiGoster ?? false,
            rodyumIbaresi       = etiketAyarlari?.RodyumIbaresi ?? "rodyum kaplama",
        },
    };

    private static IReadOnlyList<object> MadenListesi() =>
        [.. Maden.Hepsi.Select(m => new
        {
            maden    = m,
            madenAdi = Maden.Ad(m),
            // Rodyum kaplama işareti bu madende anlamlı mı (K20-7)?
            rodyumKaplanabilir = Array.IndexOf(Maden.RodyumKaplanabilir, m) >= 0,
            // Ürün grubu (tip) kısıtı ekranda önden süzülebilsin.
            ayarlar = Ayarlar.MadeninAyarlari(m).Select(a => new
            {
                id            = a.Id,
                ad            = a.Ad,
                milyem        = a.Milyem,
                kiymetli      = a.Kiymetli,
                hasHesaplanir = a.HasHesaplanir,
            }).ToList(),
        })];

    // ─── Tekil ürün ───────────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");
        return urun;   // servis içi çağıranlar VARLIĞI alır; HTTP yanıtı controller'da YanitJson ile zenginleşir
    }

    // ═══ PAKET PATCH-4 — YANITTA gercekMilyem ═══════════════════════════════════
    // Varlıkta yalnız "milyem" kolonu var; düzenleme çekmecesi "gercekMilyem" okur ve
    // bu alan yanıtta HİÇ yoktu → çekmece her açılışta boş geliyor, Kaydet kayıtlı
    // gerçek milyemi siliyordu. Yanıt = varlığın TAMAMI (serileştirme aynı: Web
    // varsayılanı + Strict sayı) + gercekMilyem: milyem ayarın standardından
    // FARKLIYSA o değer, standartsa null (çekmece boş = "standart kullanılıyor").

    /// <summary>Kayıtlı milyem ayarın standardından farklıysa o değer; değilse null.</summary>
    public static decimal? GercekMilyemi(Data.Urun u) =>
        Ayarlar.Bul(u.AyarId) is { } a && a.Milyem != u.Milyem ? u.Milyem : null;

    private static readonly System.Text.Json.JsonSerializerOptions YanitSecenekleri =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict };

    /// <summary>HTTP yanıtı: varlıksa gercekMilyem eklenir; başka şekil aynen döner.</summary>
    public static object Yanit(object sonuc) => sonuc is Data.Urun u ? YanitJson(u) : sonuc;

    /// <summary>Ürün yanıtı: varlığın tüm alanları + <c>gercekMilyem</c>.</summary>
    public static System.Text.Json.Nodes.JsonObject YanitJson(Data.Urun u)
    {
        var o = System.Text.Json.JsonSerializer.SerializeToNode(u, YanitSecenekleri)!.AsObject();
        o["gercekMilyem"] = GercekMilyemi(u);
        return o;
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, UrunGuncelleDto dto, AktifKullanici islem)
    {
        var mevcut = await TenantUrunuBul(id, islem.TenantId);
        var eskiMaden = mevcut.Maden;

        // PAKET PATCH-SOZLESME (02.10): ad ve stok kodu OLUŞTURMADA zorunludur; PATCH'te
        // gönderilmezse KORUNUR (null = dokunma), ama BOŞ DİZE gelirse eskiden kolona ""
        // yazılıyordu (adsız/kodsuz ürün). Boş/boşluk → 400 alan mesajı.
        if (dto.Ad is not null && dto.Ad.Trim().Length == 0)
            throw new BadRequestException("Ürün adı boş bırakılamaz").AlanIle("ad");
        if (dto.Kod is not null && dto.Kod.Trim().Length == 0)
            throw new BadRequestException("Stok kodu boş bırakılamaz").AlanIle("kod");

        GercekMilyemKapisi(dto.GercekMilyem);   // PATCH-4 t2: servis içi aralık kapısı (POST ile ortak)
        // PATCH-SOZLESME tur 3 + PATCH-3: kolon kapasitesi — POST ile ORTAK kapı.
        UrunKolonKapisi(dto.GramAgirlik, dto.TasAgirlik, dto.SatisFiyati, dto.AlisMaliyeti,
            dto.KonsinyeBirimBedel, dto.IscilikTl, dto.MinStok);

        // PATCH-SOZLESME tur 2 (hasım DÜŞÜK): ad/kod KIRPILMIŞ yazılır ve kırpılmış
        // karşılaştırılır — " ABC " ile "ABC" iki ayrı stok kodu olamaz.
        var yeniKod = dto.Kod?.Trim();
        var yeniAd  = dto.Ad?.Trim();

        // Stok kodu çakışma kontrolü
        if (yeniKod is not null && yeniKod != mevcut.Kod)
        {
            var cakisan = await db.Urunler.AnyAsync(u =>
                u.TenantId == islem.TenantId && u.Kod == yeniKod && u.Id != id);
            if (cakisan) throw new AlanCakismasiException("kod", $"'{yeniKod}' kodu zaten kullanımda");
        }

        var now = DateTime.UtcNow;
        // ═══ PAKET PATCH-4 (VERİ KRİTİK) — "GÖNDERİLDİ" DEĞİL "DEĞİŞTİ" ════════════
        // Ekran her kayıtta ayarId/birim/gram/taş/gerçek milyemi gönderir. Eskiden
        // "ayarId gövdede var" = "ayar değişti" sayılıyordu: barkodu düzeltilen 920
        // milyemli ürün SESSİZCE ayarın standardına (916) dönüyor, has gram ve
        // stoktaki has toplamı yeniden hesaplanıyordu. Artık:
        //   • ayar yalnız GERÇEKTEN farklıysa "değişti" sayılır;
        //   • gerçek milyem üç durumlu (yok = koru · null = standarda dön · değer = yaz);
        //   • has gram ve stok has toplamı yalnız has'ı etkileyen bir değer
        //     GERÇEKTEN değiştiyse yeniden hesaplanır (aynı değer = dokunma).
        var hedefBirim  = dto.Birim ?? mevcut.Birim;
        var hedefAyarId = dto.AyarId ?? mevcut.AyarId;
        var ayarDegisti = hedefAyarId != mevcut.AyarId;
        var hedefMilyem = dto.GercekMilyemVerildi
            ? (dto.GercekMilyem ?? AyarBul(hedefAyarId).Milyem)
            : (ayarDegisti ? AyarBul(hedefAyarId).Milyem : mevcut.Milyem);
        var hedefGram   = hedefBirim == "GRAM" ? 1m : (dto.GramAgirlik ?? mevcut.GramAgirlik);
        var hedefTas    = hedefBirim == "GRAM" ? 0m : (dto.TasAgirlik ?? mevcut.TasAgirlik ?? 0m);
        var hasEtkileyen =
            hedefBirim != mevcut.Birim || hedefGram != mevcut.GramAgirlik ||
            hedefTas != (mevcut.TasAgirlik ?? 0m) || ayarDegisti || hedefMilyem != mevcut.Milyem;

        // PAKET GOC-C: POST ile AYNI girdi kapısı — taş < brüt KAYITLI + GELEN birleşimiyle sınanır
        // (yalnız gramı düşüren PATCH de taşı brütün üstünde bırakamaz); tutar/eşik/barkod gelen değerle.
        GocCKapisi(hedefBirim, hedefGram, hedefTas, dto.SatisFiyati, dto.AlisMaliyeti,
            dto.KonsinyeBirimBedel, dto.IscilikTl, dto.MinStok, dto.Barkod);

        await using var tx = await db.Database.BeginTransactionAsync();

        if (dto.Tip is not null)       mevcut.Tip = dto.Tip;
        if (yeniKod is not null)       mevcut.Kod = yeniKod;
        if (yeniAd is not null)        mevcut.Ad = yeniAd;
        // PAKET PATCH-3 — isteğe bağlı alanlar ÜÇ DURUMLU: gövdede yok → koru ·
        // null ya da ""(geriye uyum) → SİL · değer → doğrula+yaz. (P-10: kolona "" yazılmaz.)
        if (dto.AciklamaVerildi)  mevcut.Aciklama = GorselKapisi.Bosla(dto.Aciklama);
        if (dto.KmtsKoduVerildi)
            mevcut.KmtsKodu = KmtsKodu.Dogrula(dto.KmtsKodu, yasal);   // YASAL-4 Y4-C: boş → temizle

        if (dto.PariteKoduVerildi)
            mevcut.PariteKodu = string.IsNullOrWhiteSpace(dto.PariteKodu)
                ? null
                : PariteCoz(dto.PariteKodu, mevcut.Ad, mevcut.Tip);
        if (dto.BarkodVerildi && dto.Barkod != mevcut.Barkod)
        {
            var yeni = (dto.Barkod ?? "").Trim();
            if (yeni.Length > 0 && await db.Urunler.AnyAsync(u =>
                    u.TenantId == islem.TenantId && u.Barkod == yeni && u.Id != id))
                throw new AlanCakismasiException("barkod", $"'{yeni}' barkodu zaten kullanımda");
            mevcut.Barkod = yeni.Length == 0 ? null : yeni;
        }
        if (dto.RfidVerildi)      mevcut.Rfid = GorselKapisi.Bosla(dto.Rfid);
        if (dto.GorselUrlVerildi) mevcut.GorselUrl = GorselKapisi.Dogrula(dto.GorselUrl);
        if (dto.IscilikTl is not null) mevcut.IscilikTl = dto.IscilikTl.Value;
        // null ya da 0 gönderilirse sabit fiyat temizlenir (satışta hesaplamaya dönülür)
        if (dto.SatisFiyatiVerildi) mevcut.SatisFiyati = dto.SatisFiyati is > 0m ? dto.SatisFiyati.Value : null;
        if (dto.Aktif is not null)     mevcut.Aktif = dto.Aktif.Value;

        // Kategori / reyon / konsinye — üçü de doğrulanır, sessizce yazılmaz.
        // GÖNDERİLDİ mi diye bakılır, değeri null mu diye DEĞİL: null = SİL.
        if (dto.KategoriIdGonderildi)
        {
            var k = string.IsNullOrWhiteSpace(dto.KategoriId) ? null : dto.KategoriId.Trim();
            await KategoriDogrula(k, islem.TenantId);
            mevcut.KategoriId = k;
        }
        if (dto.ReyonIdGonderildi)
        {
            var r = string.IsNullOrWhiteSpace(dto.ReyonId) ? null : dto.ReyonId.Trim();
            await ReyonDogrula(r, islem.TenantId);
            mevcut.ReyonId = r;
        }

        // ── PAKET P-16 (Y-26) — kart alanları, ÜÇ DURUMLU PATCH ─────────────
        if (dto.TedarikciIdGonderildi)
        {
            var t = string.IsNullOrWhiteSpace(dto.TedarikciId) ? null : dto.TedarikciId.Trim();
            await IslemDogrulayici.CariDogrula(db, t, islem.TenantId);
            mevcut.TedarikciId = t;
        }
        if (dto.AlisMaliyetiGonderildi) mevcut.AlisMaliyeti = dto.AlisMaliyeti;
        if (dto.HedefMarjGonderildi)    mevcut.HedefMarj    = dto.HedefMarj;
        if (dto.KdvOraniGonderildi)     mevcut.KdvOrani     = dto.KdvOrani;
        if (dto.SeriNoGonderildi)       mevcut.SeriNo       = GorselKapisi.Bosla(dto.SeriNo);
        if (dto.OlcuGonderildi)         mevcut.Olcu         = GorselKapisi.Bosla(dto.Olcu);
        if (dto.MinStokGonderildi)      mevcut.MinStok      = dto.MinStok;
        {
            // UX-4b madde 6 / UX-S26: konsinye üçlüsü YALNIZ gövdede geldiyse
            // değerlendirilir. Hiçbiri gönderilmediyse mevcut kayıt olduğu gibi
            // korunur — "gönderilmeyen alan = koru".
            if (dto.Konsinye is not null || dto.KonsinyeCariId is not null
                || dto.KonsinyeBirimBedel is not null)
            {
                var konsinye = dto.Konsinye ?? mevcut.Konsinye;
                var kCari    = dto.KonsinyeCariId ?? mevcut.KonsinyeCariId;
                var kBedel   = dto.KonsinyeBirimBedel ?? mevcut.KonsinyeBirimBedel;
                KonsinyeKapisi.Dogrula(konsinye, konsinye ? kCari : null, konsinye ? kBedel : null);
                if (konsinye) await IslemDogrulayici.CariDogrula(db, kCari, islem.TenantId);
                mevcut.Konsinye = konsinye;
                mevcut.KonsinyeCariId = konsinye ? kCari : null;
                mevcut.KonsinyeBirimBedel = konsinye ? kBedel : null;
            }
        }

        // Taş kökeni — md.11/(e). hasEtkileyen bloğunun DIŞINDA: yalnızca köken
        // güncellense de (taş ağırlığına dokunulmasa da) kapı çalışmak zorunda.
        // Taş ağırlığı bu noktada henüz eski değerinde; aşağıda hasEtkileyen
        // bloğu güncelliyor. Bu yüzden efektif ağırlığı burada hesaplıyoruz.
        var efektifTas  = dto.TasAgirlik ?? mevcut.TasAgirlik;
        var kokenGirdi  = dto.TasKokeni ?? mevcut.TasKokeni;
        var ibareGirdi  = dto.TasIbaresiVerildi ? GorselKapisi.Bosla(dto.TasIbaresi) : mevcut.TasIbaresi;
        TasKapisi.Dogrula(efektifTas, kokenGirdi, ibareGirdi);
        // 4C: verilen alan güncellenir, verilmeyen korunur; sonuç tip ile birlikte doğrulanır.
        // PAKET PATCH-3: üç durumlu (null/"" → SİL). Sonuç tip ile birlikte PirlantaKapisi'nda doğrulanır.
        if (dto.PirlantaKaratVerildi) mevcut.PirlantaKarat = dto.PirlantaKarat;
        if (dto.PirlantaRenkVerildi) mevcut.PirlantaRenk = GorselKapisi.Bosla(dto.PirlantaRenk);
        if (dto.PirlantaBerraklikVerildi) mevcut.PirlantaBerraklik = GorselKapisi.Bosla(dto.PirlantaBerraklik);
        if (dto.PirlantaKesimVerildi) mevcut.PirlantaKesim = GorselKapisi.Bosla(dto.PirlantaKesim);
        if (dto.SertifikaKurumuVerildi) mevcut.SertifikaKurumu = GorselKapisi.Bosla(dto.SertifikaKurumu);
        if (dto.SertifikaNoVerildi) mevcut.SertifikaNo = GorselKapisi.Bosla(dto.SertifikaNo);
        PirlantaKapisi.Dogrula(dto.Tip ?? mevcut.Tip, mevcut.PirlantaKarat, mevcut.PirlantaRenk, mevcut.PirlantaBerraklik, mevcut.PirlantaKesim, mevcut.SertifikaKurumu, mevcut.SertifikaNo);
        mevcut.TasKokeni = kokenGirdi;
        if (dto.TasIbaresiVerildi) mevcut.TasIbaresi = ibareGirdi;

        // Gram, taş, ayar veya gerçek milyem değiştiyse hasGrami yeniden hesapla
        if (hasEtkileyen)
        {
            if (dto.Birim is not null) mevcut.Birim = dto.Birim;
            var yeniGram   = mevcut.Birim == "GRAM" ? 1m : (dto.GramAgirlik ?? mevcut.GramAgirlik);
            if (mevcut.Birim == "GRAM") { dto.TasAgirlik = 0m; mevcut.GramAgirlik = 1m; }
            var yeniTas    = dto.TasAgirlik ?? mevcut.TasAgirlik ?? 0m;
            var yeniAyarId = dto.AyarId ?? mevcut.AyarId;

            if (dto.AyarId is not null) mevcut.AyarId = yeniAyarId;

            // Efektif milyem (PATCH-4): yukarıdaki üç durumlu karar — ayar yalnız
            // GERÇEKTEN değiştiyse standarda döner; aksi hâlde kayıtlı milyem korunur.
            var efektifMilyem = hedefMilyem;

            mevcut.Milyem   = efektifMilyem;
            mevcut.HasGrami = HesaplaHasGrami(yeniGram, yeniTas, yeniAyarId, efektifMilyem);
            if (dto.GramAgirlik is not null) mevcut.GramAgirlik = yeniGram;
            if (dto.TasAgirlik is not null)  mevcut.TasAgirlik = dto.TasAgirlik.Value;

            // Stokların toplamHasGram'ını yeni has gramına göre güncelle
            var subeStoklar = await db.Stoklar.Where(s => s.UrunId == id).ToListAsync();
            foreach (var s in subeStoklar)
            {
                // PATCH-SOZLESME tur 3: has yuvarlaması fatura kuralıyla AYNI (Yuvarla6 = AwayFromZero)
                // ve stok.toplam_has_gram decimal(15,6) kapasitesi — taşarsa 500 değil 400.
                s.ToplamHasGram = KolonTavani.Denetle(
                    Kuyumcu.Api.Modules.Fatura.OzelMatrahMotoru.Yuvarla6(s.Adet * mevcut.HasGrami),
                    6, KolonTavani.D15_6, "gramAgirlik", "Stoktaki toplam has gram");
                s.Guncellendi = now;
            }
        }

        // ── PAKET 20 — MADEN KAPISI (tip/ayar/rodyum/fiyat tutarlılığı) ──────
        // Ayar ve tip yukarıda yazıldı; kapı SON hâl üzerinde çalışır ki
        // "tipi GUMUS yap, ayarı K22 bırak" gibi kısmi PATCH'ler de yakalansın.
        if (dto.RodyumKaplama is not null) mevcut.RodyumKaplama = dto.RodyumKaplama.Value;
        var oncekiMaden = eskiMaden;
        MadenKapisi.UrunDogrula(mevcut.Tip, mevcut.AyarId, mevcut.RodyumKaplama, mevcut.SatisFiyati);

        // Maden DEĞİŞTİYSE bu ürünün geçmiş defter satırları eski madenle
        // etiketli kalır (append-only) — sessiz geçilmez, denetim izine yazılır.
        if (mevcut.Maden != oncekiMaden)
            denetim?.YazmaKaydet(
                islem, nameof(Data.Urun), mevcut.Id,
                $"Ürün madeni {Kuyumcu.Core.Domain.Maden.Ad(oncekiMaden)} → " +
                $"{mevcut.MadenAdi} (ayar {mevcut.AyarId}). Geçmiş hareket satırları " +
                "yazıldıkları madenle etiketli KALIR; defter append-only'dir.",
                hassas: false);

        mevcut.Guncellendi = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Getir(id, islem);
    }

    // ─── Deaktif et ───────────────────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var urun = await TenantUrunuBul(id, islem.TenantId);

        urun.Aktif = false;
        urun.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Ürün deaktif edildi") };
    }

    // ─── Stok listesi (ürün bazlı, tüm şubeler) ──────────────────────────────

    public async Task<object> StokListele(string id, AktifKullanici islem)
    {
        await TenantUrunuBul(id, islem.TenantId);

        var stokListesi = await db.Stoklar.AsNoTracking()
            .Where(s => s.UrunId == id && s.TenantId == islem.TenantId)
            .ToListAsync();

        return new
        {
            urunId        = id,
            toplamAdet    = stokListesi.Sum(s => s.Adet),
            toplamHasGram = stokListesi.Sum(s => s.ToplamHasGram),
            subeStoklar   = stokListesi.Select(s => new
            {
                subeId        = s.SubeId,
                adet          = s.Adet,
                toplamHasGram = s.ToplamHasGram,
                guncellendi   = s.Guncellendi,
            }).ToList(),
        };
    }

    /// <summary>stok.adet kolonu decimal(12,3) — en büyük yazılabilir değer.</summary>
    internal const decimal StokAdetTavani = 999_999_999.999m;
    /// <summary>has kolonları (stok.toplam_has_gram, hareketler.has_grami) decimal(15,6).</summary>
    internal const decimal HasGramTavani = 999_999_999.999999m;

    // ─── Şube stoku sayım güncelleme ──────────────────────────────────────────
    /// <summary>
    /// Sayım düzeltmesi — mevcut adet üzerine yazan (replace) işlem.
    /// Genellikle sayım modülü kullanır; satış/alış kendi stok güncellemesini yapar.
    /// </summary>
    public async Task<object> StokSayimGuncelle(
        string id, string subeId, decimal yeniAdet, AktifKullanici islem,
        string? aciklama = null)
    {
        var urun = await TenantUrunuBul(id, islem.TenantId);
        // Mehmet (06 Güvenlik) bulgusu, ADIM1-KARAR §1/4: subeId doğrulanmıyordu.
        // Yabancı bir subeId ile kendi kiracımızda ama geçersiz şube referanslı
        // yeni bir Stok satırı açılabiliyordu — veri bütünlüğü kusuru.
        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);

        // PATCH-SOZLESME tur 2 (hasım KRİTİK ×3): sayılan adet kapısı.
        // (a) Yuvarlama fatura kalemiyle AYNI kural: yarım binde AwayFromZero
        //     (eskiden ToEven — 1,0005 → 1,000; fatura miktarı 1,001 yapıyordu).
        // (b) Eksi adet YOK — stok sıfırın altına düşemez (SatisModulu değişmezi).
        // (c) Üst sınır KOLON KAPASİTESİNDEN türetilir (iş eşiği uydurulmaz):
        //     stok.adet decimal(12,3) ve has kolonları decimal(15,6) — taşarsa
        //     eskiden veritabanı hatası 500 dönüyordu; şimdi 400 alan:adet.
        yeniAdet = Math.Round(yeniAdet, 3, MidpointRounding.AwayFromZero);
        if (yeniAdet < 0m)
            throw new BadRequestException("Sayılan adet eksi olamaz").AlanIle("adet");
        // PAKET GOC-C (#6 · K14): ADET birimli üründe sayılan miktar tam sayıdır (1,5 yüzük sayılmaz).
        UrunAdetKapisi.SifirOlabilirDogrula(urun.Birim, yeniAdet, "adet");
        if (yeniAdet > StokAdetTavani
            || Math.Abs(yeniAdet * urun.HasGrami) > HasGramTavani)
            throw new BadRequestException("Sayılan adet çok büyük — kaydedilebilecek en büyük değeri aşıyor").AlanIle("adet");
        // PATCH-SOZLESME tur 3 (hasım ORTA): has yuvarlaması fatura kalemiyle AYNI kural
        // (OzelMatrahMotoru.Yuvarla6 = yarımda AwayFromZero); eskiden ToEven — 1,3750005 → 1,375000
        // (fatura 1,375001). Defter ile belge aynı has'ı yazar.
        var toplamHasGram = Kuyumcu.Api.Modules.Fatura.OzelMatrahMotoru.Yuvarla6(yeniAdet * urun.HasGrami);
        var now = DateTime.UtcNow;

        // ADR-0010: global filtre var, elle kiracı şartı da yazılır — tek katman bırakılmaz.
        var mevcut = await db.Stoklar
            .FirstOrDefaultAsync(s => s.UrunId == id && s.SubeId == subeId
                                   && s.TenantId == islem.TenantId);

        var eskiAdet = mevcut?.Adet ?? 0m;
        var fark = Math.Round(yeniAdet - eskiAdet, 3);

        // URUN-ACILIS-ATOMIK: dış işlem (ürün kartı oluşturma, içe aktarım) varsa ona
        // KATILIR ve commit'i sahibine bırakır; tek başına çağrıldığında davranış aynıdır.
        await using var tx = await db.Database.IsBaslatVeyaDevral();

        // ── TB-18 KAPANIŞI — sayım artık DEFTERE yazılır ────────────────────
        // Devir denetiminin T3 bulgusu: `SAYIM_DUZELTME` hareket türü tanımlıydı
        // ama hiç kullanılmıyordu; sayım stoğu sessizce üzerine yazıyordu.
        // Sonuç: append-only defter, stok gerçeğinin tek kaynağı olma özelliğini
        // sayım anında kaybediyordu — kaybolan 3 gramı kimin ne zaman düştüğü
        // hiçbir yerde görünmüyordu.
        if (fark != 0m)
        {
            var hareketId = Kimlik.Ulid();
            HareketFabrikasi.Ekle(db,
                OdemeIzi.Yok,
                new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = "SAYIM_DUZELTME",
                Tarih         = now,
                Aciklama      = aciklama
                                ?? $"Sayım düzeltmesi: {eskiAdet:0.###} → {yeniAdet:0.###} adet",
                TlTutari      = 0m,                                  // sayım para hareketi DEĞİLDİR
                HasGrami      = Kuyumcu.Api.Modules.Fatura.OzelMatrahMotoru.Yuvarla6(fark * urun.HasGrami), // + fazla, − eksik (fatura kuralı)
                // K20-1: sayım farkının madeni ürünün ayarından gelir.
                Maden         = urun.Maden,
                ParaBirimi    = "TRY",
                KurSnapshotId = await SonKurId(islem.TenantId),
                UrunId        = id,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

            // Maliyet defteri de sayımı görmeli.
            // Fazla çıkan malın maliyeti BİLİNMEZ — sıfır yazmak kârı şişirirdi.
            var motor = new Modules.Maliyet.MaliyetMotoru(db);
            if (fark > 0m)
                motor.PartiAc(islem.TenantId, subeId, id, PartiKaynagi.SayimFazla,
                    fark, urun.HasGrami, now, girisHareketId: hareketId);
            else
                await motor.PartiTuket(islem.TenantId, subeId, id, -fark, now,
                    await motor.Yontem(islem.TenantId), hareketId);
        }

        if (mevcut is not null)
        {
            mevcut.Adet          = yeniAdet;
            mevcut.ToplamHasGram = toplamHasGram;
            mevcut.Guncellendi   = now;
        }
        else
        {
            db.Stoklar.Add(new Stok
            {
                Id            = Kimlik.Ulid(),
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                UrunId        = id,
                Adet          = yeniAdet,
                ToplamHasGram = toplamHasGram,
                Guncellendi   = now,
            });
        }

        // ── PAKET 25 — SAYIMIN YERLEŞİM BACAĞI ──────────────────────────────
        // Fazla çıkan mal ürünün varsayılan reyonuna (aynı şube + aktif ise)
        // yazılır, değilse ATANMAMIŞ kalır. Eksik çıkan mal önce ATANMAMIŞ'tan,
        // yetmezse reyonlardan (ad sırasıyla) düşülür — Faz A'da sayım satırı
        // reyon taşımadığı için tek makul sıra budur (Faz B kapsamı).
        if (fark > 0m)
            await yerlesim.GirisEkle(islem.TenantId, subeId, id, fark, null, urun.ReyonId, now);
        else if (fark < 0m)
            await yerlesim.CikisDus(islem.TenantId, subeId, id, -fark, null, yeniAdet, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        return await StokListele(id, islem);
    }

    /// <summary>
    /// Sayım hareketine bağlanacak kur fotoğrafı. `hareketler.kur_snapshot_id`
    /// NOT NULL olduğu için gereklidir; sayım para hareketi olmadığından
    /// en son kur yeterlidir. Hiç kur yoksa sayım yapılamaz — uydurma kur yazılmaz.
    /// </summary>
    private async Task<string> SonKurId(string tenantId) =>
        await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.Olusturuldu)
            .Select(k => k.Id)
            .FirstOrDefaultAsync()
        ?? throw new BadRequestException(
            "Sayım kaydı için en az bir kur girişi gerekir — hareket kur fotoğrafına bağlanır.");

    // ─── Barkod ile bul ───────────────────────────────────────────────────────

    public async Task<object> BarkodBul(string barkod, AktifKullanici islem)
    {
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.TenantId == islem.TenantId && u.Barkod == barkod && u.Aktif)
            ?? throw new NotFoundException($"'{barkod}' barkodlu ürün bulunamadı");
        return urun;
    }

    // ─── Dahili yardımcı ──────────────────────────────────────────────────────

    internal async Task<Data.Urun> TenantUrunuBul(string id, string tenantId) =>
        await db.Urunler.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
        ?? throw new NotFoundException("Ürün bulunamadı");
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/urunler")]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunController(UrunServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/urunler — Yeni ürün tanımla (201) — MUHASEBE+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Olustur([FromBody] UrunOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, UrunServisi.Yanit(await servis.Olustur(dto, Islem)));   // PATCH-4: + gercekMilyem

    /// <summary>GET /api/v1/urunler — Filtreli ürün listesi</summary>
    [HttpGet]
    public async Task<List<System.Text.Json.Nodes.JsonObject>> Listele(
        [FromQuery] string? tip, [FromQuery] string? ayarId,
        [FromQuery] bool? aktif, [FromQuery] string? q,
        [FromQuery, Range(1, 500)] int limit = 50,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery] string? tasKategori = null,
        [FromQuery] string? kategoriId = null,
        [FromQuery] string? reyonId = null,
        [FromQuery] bool? konsinye = null,
        [FromQuery] string? maden = null,
        [FromQuery] string? siralama = null)
        // PAKET PATCH-4: liste satırı da gercekMilyem taşır (varlık + tek alan, UrunServisi.YanitJson).
        => (await servis.Listele(Islem, tip, ayarId, aktif, q, limit, offset, tasKategori,
                          kategoriId, reyonId, konsinye, maden, siralama))
           .Select(UrunServisi.YanitJson).ToList();

    /// <summary>
    /// GET /api/v1/urunler/madenler — MADEN + SAFLIK (MİLYEM) TABLOSU (paket 20).
    ///
    /// <para>
    /// Seçim listelerinin TEK KAYNAĞI (K20-8, UX-S31): ürün kartı, POS ve
    /// harici istemciler ayar listesini buradan kurar; sabit liste kopyalanmaz.
    /// Kiracıya bağlı veri yoktur ama uç <b>anonim DEĞİLDİR</b> (Mehmet koşulu):
    /// sınıf düzeyindeki [Authorize] geçerlidir.
    /// </para>
    /// </summary>
    [HttpGet("madenler")]
    public object Madenler() => servis.MadenTablosu();

    /// <summary>GET /api/v1/urunler/barkod/{kod} — Barkodla hızlı arama (POS)</summary>
    [HttpGet("barkod/{kod}")]
    public Task<object> BarkodBul(string kod) => servis.BarkodBul(kod, Islem);

    /// <summary>GET /api/v1/urunler/{id} — Ürün detayı</summary>
    [HttpGet("{id}")]
    public async Task<object> Getir(string id) => UrunServisi.Yanit(await servis.Getir(id, Islem));   // PATCH-4: + gercekMilyem

    /// <summary>GET /api/v1/urunler/{id}/etiket — Zorunlu taş ibaresi (md.11/(e))</summary>
    [HttpGet("{id}/etiket")]
    public Task<object> Etiket(string id) => servis.Etiket(id, Islem);

    /// <summary>GET /api/v1/urunler/{id}/stok — Tüm şubelerdeki stok durumu</summary>
    [HttpGet("{id}/stok")]
    public Task<object> StokListele(string id) => servis.StokListele(id, Islem);

    /// <summary>PATCH /api/v1/urunler/{id} — Ürün bilgilerini güncelle — MUHASEBE+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<object> Guncelle(string id, [FromBody] UrunGuncelleDto dto)
        => UrunServisi.Yanit(await servis.Guncelle(id, dto, Islem));   // PATCH-4: + gercekMilyem

    /// <summary>PATCH /api/v1/urunler/{id}/stok/{subeId} — Sayım düzeltme — MUDUR+</summary>
    [HttpPatch("{id}/stok/{subeId}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> StokSayimGuncelle(string id, string subeId, [FromBody] StokSayimDto dto)
        => servis.StokSayimGuncelle(id, subeId,
            dto.Adet ?? throw new BadRequestException("Sayılan adet girilmedi").AlanIle("adet"), Islem);

    /// <summary>DELETE /api/v1/urunler/{id} — Soft delete — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
