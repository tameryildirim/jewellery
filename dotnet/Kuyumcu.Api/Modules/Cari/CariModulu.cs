using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;   // P-05: DogalAnahtar
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Kuyumcu.Api.Modules.Masak;
using GD = Kuyumcu.Api.Common.Dogrulama;   // PAKET GOC-B: ortak girdi öznitelikleri (Core kuralı)
using GS = Kuyumcu.Core.Domain.Girdi.AlanSinirlari;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// CARİ MODÜLÜ — müşteri/tedarikçi/atölye/sarraf hesapları ve paralel defter
// (TL + has gram) bakiye takibi.
// Kaynak: apps/api/src/modules/cari/*
// Desen: Modules/Kasa/KasaModulu.cs (tek dosya: DTO'lar + Servis + Controller)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class CariOlusturDto
{
    [Required(ErrorMessage = "Cari türü zorunludur")]
    [RegularExpression("MUSTERI|TEDARIKCI|HER_IKISI|ATOLYE|SARRAF",
        ErrorMessage = "Cari türü geçersiz — kabul edilen değerler: MUSTERI, TEDARIKCI, HER_IKISI, ATOLYE, SARRAF")]
    public string Tur { get; set; } = null!;

    // PAKET GOC-B: K19/K20 — en az 2 karakter, en az bir harf ("12345" ad olamaz); sınır kolonla TEK sabit.
    [Required(ErrorMessage = "Ad zorunludur"), MaxLength(GS.CariAd), GD.Ad(GS.CariAd)]
    public string Ad { get; set; } = null!;

    // UX-4d madde 8/19: telefon biçimi TEK kuraldan (Kuyumcu.Core Iletisim).
    [MaxLength(20)]
    [Kuyumcu.Api.Common.Dogrulama.Telefon]   // GIRDI-AILESI-2 tur 3: ekranla TEK kural (Core TelefonDogrulayici)
    public string? Telefon { get; set; }

    // PAKET GOC-B: K3 — [EmailAddress] yalnız "@" arıyordu ("a@b" geçiyordu); Core EpostaDogrulayici (ekranla aynı metin).
    [GD.Eposta]
    [MaxLength(GS.Eposta)]
    public string? Email { get; set; }
    [MaxLength(20)] public string? VergiNo { get; set; }
    [MaxLength(500)] public string? Adres { get; set; }
    [MaxLength(1000)] public string? Notlar { get; set; }

    /// <summary>Maksimum veresiye limiti TL — null = sınırsız</summary>
    // UX-4d madde 8: "must be between 0 and 1,79E+308" mesajı kullanıcıya
    // gösteriliyordu; anlamlı Türkçe karşılığı yazıldı.
    [Range(0, double.MaxValue, ErrorMessage = "Kredi limiti 0 veya daha büyük olmalıdır")]
    public decimal? KrediLimitiTl { get; set; }

    /// <summary>
    /// Bu cariye veresiyede varsayılan vade — GÜN. Boş = vade tanınmıyor
    /// (borç doğduğu gün muaccel).
    /// </summary>
    [Range(0, 3650, ErrorMessage = "Varsayılan vade 0 ile 3650 gün arasında olmalıdır")]
    public int? VarsayilanVadeGun { get; set; }
    [MaxLength(36)] public string? FiyatListesiId { get; set; }

    // ── PAKET P-16 (Y-27) — KİMLİK / KYC ALANLARI ───────────────────────────
    // ⚠ UX-S26: bu alanlar OLUŞTURMADA kabul edilir, "sonra düzenlemeden gir"
    //   diye ertelenmez; aynı küme CariGuncelleDto'da da vardır.

    [MaxLength(GS.CariUnvan), GD.Ad(GS.CariUnvan)] public string? Unvan { get; set; }   // PAKET GOC-B: K20

    [MaxLength(4)]
    [RegularExpression(KimlikNo.TurDesen, ErrorMessage = "Kimlik türü geçersiz — kabul edilen değerler: TCKN, VKN")]
    public string? KimlikTuru { get; set; }

    // PAKET GOC-B: K8f — gelecek/0001 yılı/120 yıldan eski doğum tarihi 400 (servis yolunda da elle). Tur 2: 18 yaş altı ENGEL DEĞİL (TMK md.11 yasaklamaz → paket öncesi davranış; ekranda yalnız uyarı).
    [GD.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.KisiDogum)]
    public DateOnly? DogumTarihi { get; set; }

    // KESIF-P2 (#22): uyruk ISO 3166-1 ALPHA-3 (MASAK formuyla tek standart). 2 harfli eski
    // kod 400 alır; kural servis içinde de elle sınanır (CariServisi.UyrukNormalle).
    [MaxLength(3, ErrorMessage = "Uyruk kodu en çok 3 harf olur (örnek: TUR, DEU)")] public string? Uyruk { get; set; }

    [Range(0, 100, ErrorMessage = "İskonto yüzdesi 0 ile 100 arasında olmalıdır")]
    public decimal? IskontoYuzde { get; set; }

    [MaxLength(20)]
    [RegularExpression(CariSegmentleri.Desen,
        ErrorMessage = "Segment geçersiz — kabul edilen değerler: BIREYSEL, KURUMSAL, TOPTAN, VIP")]
    public string? Segment { get; set; }

    /// <summary>D2-12 — yüzük ölçüsü (serbest metin).</summary>
    [MaxLength(GS.CariYuzukOlcusu)] public string? YuzukOlcusu { get; set; }

    /// <summary>
    /// PAKET P-26 (Y-37) — muhasebe hesap kodu (ör. 120.01.0001 / 320.01.0001).
    /// UX-S26: oluşturma formu düzenleme formuyla AYNI alan kümesini taşır —
    /// "sonra düzenlemeden gir" ertelemesi yapılmaz. Boş bırakılabilir; muhasebe
    /// fişinde o satır BOŞ çıkar ve "eksikAlan" sütununda söylenir (uydurulmaz).
    /// </summary>
    [MaxLength(30)] public string? HesapKodu { get; set; }
}

/// <summary>
/// PAKET 40-A — PEP BEYANI gövdesi. GERÇEK PATCH: gönderilmeyen alan
/// "değişmedi" demektir.
///
/// ⚠ NEDEN AYRI DTO (kurul engel 4): CariGuncelleDto'nun tam gövdesiyle cari
///   üzerine yazan bir uç, istemci ad/telefon/adres göndermediğinde onları
///   SESSİZCE SIFIRLARDI. Bu uç YALNIZ pep_* kolonlarına dokunur.
///
/// ⚠ <c>PepBeyanTarihi</c> İSTEMCİDEN ALINMAZ: beyanın ne zaman alındığı
///   denetimde sorulan bir olgudur, istemcinin söylediği değil sunucunun
///   gördüğü andır.
/// </summary>
public sealed class CariPepDto
{
    private bool? _pepBeyani;
    private string? _pepGorev;

    public bool? PepBeyani { get => _pepBeyani; set { _pepBeyani = value; PepBeyaniVerildi = true; } }
    [JsonIgnore] public bool PepBeyaniVerildi { get; private set; }

    [MaxLength(200)]
    public string? PepGorev { get => _pepGorev; set { _pepGorev = value; PepGorevVerildi = true; } }
    [JsonIgnore] public bool PepGorevVerildi { get; private set; }
}

/// <summary>
/// PATCH gövdesinde "hiç gönderilmedi" ile "açıkça null gönderildi" ayrımı
/// korunur (Nest'teki undefined filtresi): yalnızca gövdede geçen alanlar
/// güncellenir; null gönderilen opsiyonel alanlar temizlenir.
/// </summary>
public sealed class CariGuncelleDto
{
    private string? _tur;
    private string? _ad;
    private string? _telefon;
    private string? _email;
    private string? _vergiNo;
    private string? _adres;
    private string? _notlar;
    private decimal? _krediLimitiTl;
    private int? _varsayilanVadeGun;
    private string? _fiyatListesiId;
    private bool? _aktif;

    [RegularExpression("MUSTERI|TEDARIKCI|HER_IKISI|ATOLYE|SARRAF",
        ErrorMessage = "Cari türü geçersiz — kabul edilen değerler: MUSTERI, TEDARIKCI, HER_IKISI, ATOLYE, SARRAF")]
    public string? Tur { get => _tur; set { _tur = value; TurVerildi = true; } }
    [JsonIgnore] public bool TurVerildi { get; private set; }

    [MaxLength(GS.CariAd), GD.Ad(GS.CariAd)]   // PAKET GOC-B: K19/K20
    public string? Ad { get => _ad; set { _ad = value; AdVerildi = true; } }
    [JsonIgnore] public bool AdVerildi { get; private set; }

    [MaxLength(20)]
    [Kuyumcu.Api.Common.Dogrulama.Telefon]   // GIRDI-AILESI-2 tur 3: ekranla TEK kural (Core TelefonDogrulayici)
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonVerildi = true; } }
    [JsonIgnore] public bool TelefonVerildi { get; private set; }

    // PAKET GOC-B: K3 — [EmailAddress] yalnız "@" arıyordu ("a@b" geçiyordu); Core EpostaDogrulayici (ekranla aynı metin).
    [GD.Eposta]
    [MaxLength(GS.Eposta)]
    public string? Email { get => _email; set { _email = value; EmailVerildi = true; } }
    [JsonIgnore] public bool EmailVerildi { get; private set; }

    [MaxLength(20)]
    public string? VergiNo { get => _vergiNo; set { _vergiNo = value; VergiNoVerildi = true; } }
    [JsonIgnore] public bool VergiNoVerildi { get; private set; }

    [MaxLength(500)]
    public string? Adres { get => _adres; set { _adres = value; AdresVerildi = true; } }
    [JsonIgnore] public bool AdresVerildi { get; private set; }

    [MaxLength(1000)]
    public string? Notlar { get => _notlar; set { _notlar = value; NotlarVerildi = true; } }
    [JsonIgnore] public bool NotlarVerildi { get; private set; }

    [Range(0, double.MaxValue, ErrorMessage = "Kredi limiti 0 veya daha büyük olmalıdır")]
    public decimal? KrediLimitiTl { get => _krediLimitiTl; set { _krediLimitiTl = value; KrediLimitiTlVerildi = true; } }
    [JsonIgnore] public bool KrediLimitiTlVerildi { get; private set; }

    [Range(0, 3650, ErrorMessage = "Varsayılan vade 0 ile 3650 gün arasında olmalıdır")]
    public int? VarsayilanVadeGun { get => _varsayilanVadeGun; set { _varsayilanVadeGun = value; VarsayilanVadeGunVerildi = true; } }
    [JsonIgnore] public bool VarsayilanVadeGunVerildi { get; private set; }

    [MaxLength(36)]
    public string? FiyatListesiId { get => _fiyatListesiId; set { _fiyatListesiId = value; FiyatListesiIdVerildi = true; } }
    [JsonIgnore] public bool FiyatListesiIdVerildi { get; private set; }

    public bool? Aktif { get => _aktif; set { _aktif = value; AktifVerildi = true; } }
    [JsonIgnore] public bool AktifVerildi { get; private set; }

    // ── PAKET P-16 (Y-27) — KİMLİK / KYC ALANLARI ───────────────────────────
    // GERÇEK PATCH: gövdede YOKSA korunur, açıkça null gelirse SİLİNİR.
    private string? _unvan;
    private string? _kimlikTuru;
    private DateOnly? _dogumTarihi;
    private string? _uyruk;
    private decimal? _iskontoYuzde;
    private string? _segment;

    [MaxLength(GS.CariUnvan), GD.Ad(GS.CariUnvan)]   // PAKET GOC-B: K20
    public string? Unvan { get => _unvan; set { _unvan = value; UnvanVerildi = true; } }
    [JsonIgnore] public bool UnvanVerildi { get; private set; }

    [MaxLength(4)]
    [RegularExpression(KimlikNo.TurDesen, ErrorMessage = "Kimlik türü geçersiz — kabul edilen değerler: TCKN, VKN")]
    public string? KimlikTuru { get => _kimlikTuru; set { _kimlikTuru = value; KimlikTuruVerildi = true; } }
    [JsonIgnore] public bool KimlikTuruVerildi { get; private set; }

    [GD.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.KisiDogum)]   // PAKET GOC-B: K8f (18 altı engel değil)
    public DateOnly? DogumTarihi { get => _dogumTarihi; set { _dogumTarihi = value; DogumTarihiVerildi = true; } }
    [JsonIgnore] public bool DogumTarihiVerildi { get; private set; }

    [MaxLength(3, ErrorMessage = "Uyruk kodu en çok 3 harf olur (örnek: TUR, DEU)")]   // KESIF-P2 (#22): alpha-3
    public string? Uyruk { get => _uyruk; set { _uyruk = value; UyrukVerildi = true; } }
    [JsonIgnore] public bool UyrukVerildi { get; private set; }

    [Range(0, 100, ErrorMessage = "İskonto yüzdesi 0 ile 100 arasında olmalıdır")]
    public decimal? IskontoYuzde { get => _iskontoYuzde; set { _iskontoYuzde = value; IskontoYuzdeVerildi = true; } }
    [JsonIgnore] public bool IskontoYuzdeVerildi { get; private set; }

    [MaxLength(20)]
    [RegularExpression(CariSegmentleri.Desen,
        ErrorMessage = "Segment geçersiz — kabul edilen değerler: BIREYSEL, KURUMSAL, TOPTAN, VIP")]
    public string? Segment { get => _segment; set { _segment = value; SegmentVerildi = true; } }
    [JsonIgnore] public bool SegmentVerildi { get; private set; }

    private string? _yuzukOlcusu;
    /// <summary>D2-12 — yüzük ölçüsü; gönderilmedi = koru, boş = sil.</summary>
    [MaxLength(GS.CariYuzukOlcusu)]
    public string? YuzukOlcusu { get => _yuzukOlcusu; set { _yuzukOlcusu = value; YuzukOlcusuVerildi = true; } }
    [JsonIgnore] public bool YuzukOlcusuVerildi { get; private set; }

    private string? _hesapKodu;
    /// <summary>PAKET P-26 (Y-37) — ÜÇ DURUMLU: gönderilmedi = koru, "" / null = sil.</summary>
    [MaxLength(30)]
    public string? HesapKodu { get => _hesapKodu; set { _hesapKodu = value; HesapKoduVerildi = true; } }
    [JsonIgnore] public bool HesapKoduVerildi { get; private set; }
}

public sealed class VeresiyeGirisDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Verilen kredi miktarı TL (bize borçlanıyor)</summary>
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal TlTutari { get; set; }

    /// <summary>
    /// Has gram cinsinden aynı anda verilen altın (varsa).
    /// Sıfır veya boş: sadece TL kredisi.
    /// </summary>
    [Range(0, double.MaxValue)] public decimal? HasGrami { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID (zorunlu: paralel defterde kur bağlantısı)</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş.</summary>
    public MasakBilgiDto? Masak { get; set; }
}

public sealed class TahsilatGirisDto : IValidatableObject
{
    /// <summary>TL ödeme tutarı (nakit / havale / EFT)</summary>
    [Range(0, double.MaxValue)] public decimal? TlTutari { get; set; }

    /// <summary>
    /// Ödemenin şekli: NAKIT · KART · HAVALE.
    /// <para>
    /// ⚠ <b>Varsayılan NAKİT'tir ve bu bilinçlidir.</b> 459 s. VUK Genel Tebliği tevsik
    /// zorunluluğu yalnız nakit ödemeye uygulanır; ödeme şekli bilinmiyorsa
    /// en sıkı varsayım seçilir (fail-closed). Banka üzerinden yapılan bir
    /// tahsilat hadde takılırsa, kullanıcı türü belirterek geçer — tersi
    /// durumda (bilinmiyor → serbest) mevzuat ihlali sessizce geçerdi.
    /// </para>
    /// <para>
    /// Bu alan 2026-09-14'te eklendi: tahsilat hareketinde ödeme türü HİÇ
    /// tutulmuyordu, dolayısıyla nakit kümülatif toplamı eksik hesaplanıyordu.
    /// </para>
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    /// <summary>Has gram ile ödeme (altın teslim)</summary>
    [Range(0, double.MaxValue)] public decimal? HasGrami { get; set; }

    /// <summary>
    /// Paranın gireceği kasa — <b>TL tutarı varsa ZORUNLU</b> (paket UX-4b madde 9,
    /// Q1 kararı B, Tamer Bey mührü 2026-09-16 09:30).
    /// <para>
    /// Satış tarafındaki kuralın AYNISI (SatisModulu.SatisDto.KasaId): NAKIT ·
    /// KART · HAVALE için kasa ister. <b>HAVALE de kasaya yazılır</b> — banka
    /// bacağı bu pakette AÇILMAZ; tek modülde satıştan farklı davranmak
    /// Mehmet'in (06 Güvenlik) veto ettiği durumdur.
    /// </para>
    /// <para>
    /// Yalnız has gram ile yapılan tahsilatta (TlTutari = 0) kasa İSTENMEZ:
    /// para el değiştirmez, altın gelir.
    /// </para>
    /// </summary>
    [MaxLength(36)] public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// Koşullu zorunluluk DataAnnotations ile ifade edilemez; controller yolunda
    /// 400 üretmesi için IValidatableObject kullanılır. ⚠ Bu kapı TEK BAŞINA
    /// yeterli değildir: servis doğrudan çağrıldığında (testler, iç çağrılar)
    /// çalışmaz — <see cref="CariServisi.TahsilatGir"/> içinde elle kontrol de vardır.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext baglam)
    {
        if ((TlTutari ?? 0m) > 0m && string.IsNullOrWhiteSpace(KasaId))
            yield return new ValidationResult(
                "Nakit/Kart/Havale tahsilat için kasa ID gerekli", [nameof(KasaId)]);
    }
}

/// <summary>
/// MANUEL DÜZELTME FİŞİ (paket 33) — cari bakiyesini gerekçeli olarak elle
/// düzeltir. Para EL DEĞİŞTİRMEZ: kasa/banka bacağı yoktur, MASAK ve tevsik
/// kapıları çalışmaz (SAYIM_DUZELTME ile aynı gerekçe).
/// </summary>
public sealed class CariDuzeltmeDto
{
    /// <summary>TL bakiye farkı (±). Pozitif = cari bize daha çok borçlanır.</summary>
    // PAKET GOC-B: K9b — işaretli; |tavan| decimal(15,2) controller yolunda (servis KolonTavani PATCH-5 sözleşmesi aynen).
    [Range(typeof(decimal), "-9999999999999.99", "9999999999999.99", ErrorMessage = "Değer çok büyük.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]   // tr-TR: sınır dizgesi değişmez kültürle
    public decimal TlDelta { get; set; }

    /// <summary>Has gram bakiye farkı (±). Pozitif = altın borcu artar.</summary>
    // PAKET GOC-B: K11 — işaretli has; |tavan| decimal(15,6) (servis KolonTavani PATCH-5 aynen).
    [Range(typeof(decimal), "-999999999.999999", "999999999.999999", ErrorMessage = "Değer çok büyük.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]   // tr-TR: sınır dizgesi değişmez kültürle
    public decimal HasDelta { get; set; }

    /// <summary>
    /// Düzeltmenin NEDENİ — en az 20 karakter. Boş/kısa gerekçe kabul edilmez:
    /// elle bakiye müdahalesi denetimde "neden" sorusuna cevap vermek zorundadır.
    /// </summary>
    [Required, MinLength(20), MaxLength(500)] public string Gerekce { get; set; } = null!;

    /// <summary>
    /// Düzeltilecek döviz bakiyesinin para birimi — USD · EUR · GBP.
    /// <see cref="DovizDelta"/> ile BİRLİKTE verilir; biri varsa diğeri zorunludur.
    /// </summary>
    [MaxLength(5)] public string? DovizParaBirimi { get; set; }

    /// <summary>
    /// Döviz bakiye farkı (±). Pozitif = cari bize daha çok borçlanır.
    /// <para>
    /// ⚠ Eski sistemden devreden döviz bakiyesinin <b>TEK meşru giriş yolu</b>
    /// budur (ADR-0014 ham içe aktarımı yasaklıyor): gerekçesi yazılı,
    /// denetim izli, kullanıcısı belli bir düzeltme fişi.
    /// </para>
    /// </summary>
    // PAKET GOC-B: K9b — işaretli döviz tutarı; |tavan| (servis KolonTavani PATCH-5 aynen).
    [Range(typeof(decimal), "-9999999999999.99", "9999999999999.99", ErrorMessage = "Değer çok büyük.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]   // tr-TR: sınır dizgesi değişmez kültürle
    public decimal? DovizDelta { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID (zorunlu: paralel defterde kur bağlantısı)</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;
}

// ── DÖVİZ DTO'ları (paket 27 Faz A) ──────────────────────────────────────────

/// <summary>
/// Döviz cinsinden borç doğurma. <b>TL karşılığı GÖNDERİLEMEZ</b> — sunucu
/// hesaplar (UX-S21 + OdemeSepeti'ndeki aynı sertlik).
/// </summary>
public sealed class DovizVeresiyeDto
{
    [Required(ErrorMessage = "Para birimi zorunludur")]
    [RegularExpression("USD|EUR|GBP",
        ErrorMessage = "Para birimi geçersiz — kabul edilen değerler: USD, EUR, GBP")]
    public string ParaBirimi { get; set; } = null!;

    /// <summary>Efektif döviz miktarı — sıfırdan büyük.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal Tutar { get; set; }

    [Range(0, 3650, ErrorMessage = "Vade 0 ile 3650 gün arasında olmalıdır")]
    public int? VadeGun { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    public MasakBilgiDto? Masak { get; set; }
}

/// <summary>Efektif dövizle tahsilat — para döviz kasasına girer.</summary>
public sealed class DovizTahsilatDto
{
    [Required(ErrorMessage = "Para birimi zorunludur")]
    [RegularExpression("USD|EUR|GBP",
        ErrorMessage = "Para birimi geçersiz — kabul edilen değerler: USD, EUR, GBP")]
    public string ParaBirimi { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal Tutar { get; set; }

    /// <summary>Efektifin gireceği kasa — o para biriminde OLMAK ZORUNDA (K6).</summary>
    [Required(ErrorMessage = "Döviz tahsilatı için kasa seçimi zorunludur")]
    [MaxLength(36)] public string KasaId { get; set; } = null!;

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    public MasakBilgiDto? Masak { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class CariServisi(
    KuyumcuDbContext db, ILogger<CariServisi> logger, DenetimServisi denetim,
    MasakKuralMotoru? masakMotoru = null, Modules.Tevsik.TevsikKurali? tevsikKurali = null,
    IConfiguration? yapilandirma = null)
{
    /// <summary>459 s. VUK Genel Tebliği tevsik kapısı (PLAN-2 Adım 2).</summary>
    private readonly Modules.Tevsik.TevsikKapisi tevsik =
        tevsikKurali is null ? Modules.Tevsik.TevsikKapisi.Varsayilan() : new(tevsikKurali);

    // PAKET B-3 (D2 · K2): Peşin Müşteri sınırı yapılandırılmış tevsik haddini de görür.
    private readonly MasakKapisi masak = new(
        masakMotoru ?? new MasakKuralMotoru(new MasakEsikleri()), tevsikKurali?.Esikler);

    // ── KVKK md.12 okuma izi (PLAN-2 Adım 0c, Mehmet VETO-2) ─────────────────
    // Cari kartı Telefon / VergiNo / Adres tutuyor ve bu modülde HİÇ okuma izi
    // düşmüyordu (ölçüldü 2026-09-13: "Denetim" geçen satır = 0). MASAK ekranında
    // iz tutup cari kartında tutmamak denetimde savunulamaz bir tutarsızlıktı.
    //
    // ⚠ İZ YALNIZ DENETLEYİCİ YOLUNDA DÜŞER. Getir() iç akışlarda da çağrılıyor
    // (Olustur, Guncelle, VeresiyeGir, TahsilatGir sonunda) — oraya iz koysaydık
    // her yazma işlemi sahte bir "okuma" üretir, izi gürültüye boğardı. Bu yüzden
    // denetleyici GetirIzli()'yi çağırır, iç akış Getir()'i.
    private const string Varlik = nameof(Data.Cari);

    /// <summary>
    /// PAKET P-05 (Y-2): vergi/TC numarası kiracı içinde benzersiz mi?
    /// <para><paramref name="vergiNo"/> <c>null</c> ise kontrol YAPILMAZ —
    /// numarası olmayan cari sayısı sınırsızdır (MySQL UNIQUE indeksi birden
    /// çok NULL kabul eder; kural budur, istisna değil).</para>
    /// </summary>
    private async Task VergiNoBenzersizMi(string? vergiNo, string tenantId, string? haricId)
    {
        if (vergiNo is null) return;
        // Kısıt AKTİF cariler arasında geçerlidir (filtreli UNIQUE indeksin birebir
        // karşılığı): arşivlenmiş bir kart aynı numarayı taşıyor diye yeni kayıt
        // engellenmez. Aksi hâlde ekran 409 der, veritabanı kabul ederdi.
        var cakisan = await db.Cariler.AnyAsync(c =>
            c.TenantId == tenantId && c.VergiNo == vergiNo && c.Aktif
            && (haricId == null || c.Id != haricId));
        if (cakisan)
            throw new AlanCakismasiException(
                "vergiNo",
                $"'{vergiNo}' vergi/TC numarası bu işletmede başka bir AKTİF cari kartta kayıtlı");
    }

    // ─── Cari oluştur ─────────────────────────────────────────────────────────

    /// <summary>
    /// PAKET GOC-B (envanter #2, K8f): doğum tarihi gelecek olamaz, 120 yıldan eski olamaz (İŞ KURALI).
    /// Tur 2: 18 yaş altı ENGEL DEĞİL — TMK md.11 erginliği tanımlar, küçüğün müşteri kaydını yasaklamaz
    /// (TEYİTSİZ → paket öncesi davranış; ekranda uyarı). Metin GirdiSozlugu — ekranın alan altı metniyle aynı.
    /// </summary>
    /// <summary>PAKET GOC-B: cari türü beyaz listesi (CariOlusturDto.Tur deseniyle aynı küme).</summary>
    public static readonly string[] CariTurleri = ["MUSTERI", "TEDARIKCI", "HER_IKISI", "ATOLYE", "SARRAF"];

    public static void DogumDenetle(DateOnly? dogum) =>
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(
            Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(dogum, Kuyumcu.Core.Domain.Girdi.TarihTuru.KisiDogum, Zaman.YerelBugun()),
            "dogumTarihi");

    public async Task<object> Olustur(CariOlusturDto dto, AktifKullanici islem)
        => await Getir(await OlusturId(dto, islem), islem);

    /// <summary>
    /// Kartı açar, kimliğini döner (M7a: bankadan gelen ödemeyle otomatik açılış AYNI kapılardan
    /// geçer — TCKN/VKN kuralı, benzersizlik, yaptırım taraması). Dış işlem varsa ona katılır.
    /// </summary>
    public async Task<string> OlusturId(CariOlusturDto dto, AktifKullanici islem)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;

        // ── PAKET P-05 (Y-2) — VERGİ/TC NUMARASI KİRACI İÇİNDE BENZERSİZ ─────
        // Aynı vergi numarasıyla ikinci bir cari kartı, cariyi ikiye bölünmüş
        // bir bakiyeyle bırakır: tahsilat bir karta, borç diğerine yazılır.
        // Kısıt artık veritabanındadır; buradaki ön kontrol yalnız KULLANICIYA
        // anlaşılır cümleyi kurmak içindir (yarışta kısıt devreye girer).
        // Boş dizge NULL'a çevrilir: "vergi numarası yok" hâlinin TEK gösterimi
        // NULL'dır, aksi hâlde ikinci boşluklu kayıt kısıtı ihlal ederdi.
        var vergiNo = DogalAnahtar.Normalle(dto.VergiNo);
        await VergiNoBenzersizMi(vergiNo, islem.TenantId, haricId: null);

        // ── PAKET P-16 (Y-27) — TCKN 11 / VKN 10 hane ────────────────────────
        // ⚠ DataAnnotations YALNIZ controller yolunda koşar; servis doğrudan
        //   çağrıldığında (içe aktarım, testler) kural atlanırdı. Bu yüzden
        //   hane kuralı BURADA, servis içinde de elle sınanır.
        var kimlikHatasi = KimlikNo.Dogrula(vergiNo, dto.KimlikTuru);
        if (kimlikHatasi is not null) throw new BadRequestException(kimlikHatasi).AlanIle("vergiNo");   // TCKN-DOGRULAMA: alan altı
        if (!KimlikNo.TurGecerli(dto.KimlikTuru)) throw new BadRequestException(KimlikNo.TurMesaj);
        if (!CariSegmentleri.Gecerli(dto.Segment)) throw new BadRequestException(CariSegmentleri.Mesaj);
        // PAKET PATCH-5: kredi limiti kolon tavanı decimal(15,2) + 2 haneye yuvarlama; iskonto decimal(5,2)
        // — 2'den fazla ondalık SESSİZCE yuvarlanmaz, reddedilir (PATCH-4 gerçek milyem kuralı).
        if (dto.KrediLimitiTl is { } klHam)
        {
            if (klHam < 0m) throw new BadRequestException("Kredi limiti 0 veya daha büyük olmalıdır").AlanIle("krediLimitiTl");
            dto.KrediLimitiTl = KolonTavani.IsaretliTutar(klHam, "krediLimitiTl", "Kredi limiti");
        }
        KolonTavani.OndalikSiniri(dto.IskontoYuzde, 2, "iskontoYuzde", "İskonto yüzdesi");
        if (dto.IskontoYuzde is < 0m or > 100m)
            throw new BadRequestException("İskonto yüzdesi 0 ile 100 arasında olmalıdır");
        // KESIF-P2 (#22): uyruk alpha-3 — DataAnnotations yalnız controller yolunda; servis içi elle.
        var uyruk = UyrukNormalle(dto.Uyruk, mevcut: null);
        // PAKET GOC-B (envanter #2): doğum tarihi K8f — DataAnnotations yalnız controller yolunda; servis içi elle.
        DogumDenetle(dto.DogumTarihi);

        // ⚠ Paket 47: DIŞ TRANSACTION VARSA DEVRALINIR (Common/VeriIslemi.cs).
        //   Tek başına çağrıldığında davranış AYNIDIR; içe aktarımın tek
        //   transaction değişmezi bu sayede servis yolunu kırmadan tutar.
        await using var tx = await db.Database.IsBaslatVeyaDevral();
        db.Cariler.Add(new Data.Cari
        {
            Id = id,
            TenantId = islem.TenantId,
            Tur = dto.Tur,
            Ad = dto.Ad,
            Telefon = dto.Telefon,
            Email = dto.Email,
            VergiNo = vergiNo,
            Adres = dto.Adres,
            Notlar = dto.Notlar,
            KrediLimitiTl = dto.KrediLimitiTl,
            VarsayilanVadeGun = dto.VarsayilanVadeGun,
            FiyatListesiId = dto.FiyatListesiId,
            // PAKET P-16 (Y-27)
            Unvan = dto.Unvan,
            // Tür beyan edilmediyse numaranın uzunluğundan TÜRETİLİR; numara da
            // yoksa null kalır (uydurulmaz).
            KimlikTuru = dto.KimlikTuru ?? KimlikNo.TurTuret(vergiNo),
            DogumTarihi = dto.DogumTarihi,
            Uyruk = uyruk,
            IskontoYuzde = dto.IskontoYuzde,
            Segment = dto.Segment,
            HesapKodu = string.IsNullOrWhiteSpace(dto.HesapKodu) ? null : dto.HesapKodu.Trim(),
            YuzukOlcusu = string.IsNullOrWhiteSpace(dto.YuzukOlcusu) ? null : dto.YuzukOlcusu.Trim(),   // D2-12
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });

        // PAKET CARI-BAKIYE: sıfır bakiye satırı da tek yazıcıdan (kolon ataması tek dosyada).
        db.CariBakiyeler.Add(CariBakiyeYazici.BosSatir(islem.TenantId, id, now));
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();
        // PAKET YASAL-3 (§ 4.4 nokta 2): kayıt AÇILIR; kimlik no yasal listedeyse cari kilitlenir.
        await Masak.YaptirimServisi.CariTara(db, islem.TenantId, id, vergiNo, dto.Ad, islem);

        return id;
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        AktifKullanici islem, string? tur, bool? aktif, string? q, int limit, int offset,
        string? sirala = null, string? yon = null)
    {
        var sorgu = db.Cariler.AsNoTracking().Where(c => c.TenantId == islem.TenantId);

        // PAKET GOC-B (envanter C satır 87): bilinmeyen tür 400 yerine SESSİZCE boş liste dönüyordu (UX-S28).
        if (!string.IsNullOrEmpty(tur) && !CariTurleri.Contains(tur))
            throw new BadRequestException("Cari türü geçersiz — kabul edilen değerler: MUSTERI, TEDARIKCI, HER_IKISI, ATOLYE, SARRAF").AlanIle("tur");
        if (!string.IsNullOrEmpty(tur)) sorgu = sorgu.Where(c => c.Tur == tur);
        if (aktif is not null) sorgu = sorgu.Where(c => c.Aktif == aktif);
        if (!string.IsNullOrEmpty(q))
        {
            // PAKET Q9-5: arama ad + e-posta + telefon (rakama indirgenmiş, parça
            // eşleşmeli; baştaki 0 / +90 fark etmez) — CariArama.TelefonCekirdegi.
            var desen = $"%{q.Trim()}%";
            var cekirdek = CariArama.TelefonCekirdegi(q);
            if (cekirdek is null)
                sorgu = sorgu.Where(c => EF.Functions.Like(c.Ad, desen)
                    || (c.Email != null && EF.Functions.Like(c.Email, desen)));
            else
            {
                var telDesen = $"%{cekirdek}%";
                sorgu = sorgu.Where(c => EF.Functions.Like(c.Ad, desen)
                    || (c.Email != null && EF.Functions.Like(c.Email, desen))
                    || (c.Telefon != null && EF.Functions.Like(
                        c.Telefon.Replace(" ", "").Replace("-", "").Replace("(", "")
                            .Replace(")", "").Replace("+", "").Replace(".", ""), telDesen)));
            }
        }

        // PAKET GRID-1: sıralama SUNUCUDA (beyaz liste; bilinmeyen ad/yön → 400).
        // Varsayılan (sirala yok) eskisi gibi ada göre; kararlılık için + id.
        var liste = await CariSiralama.Uygula(sorgu, db, islem.TenantId, sirala, yon)
            .Skip(offset).Take(limit).ToListAsync();

        // Liste kişisel veri döndürüyor (telefon, vergiNo, adres) → iz ZORUNLU.
        await denetim.ListeOkumaKaydet(islem, Varlik, liste.Count, "cari listesi");

        // Bakiyeleri tek sorguda çekip satırlara işle (liste ekranı bakiye gösterir)
        var idler = liste.Select(c => c.Id).ToList();
        var bakiyeler = idler.Count != 0
            ? await db.CariBakiyeler.AsNoTracking()
                .Where(b => b.TenantId == islem.TenantId && idler.Contains(b.CariId))
                .ToListAsync()
            : [];
        var bakiyeMap = bakiyeler.ToDictionary(b => b.CariId);
        // PAKET CARI-BAKIYE (tek tanım): listedeki has bakiyesi EMANETSİZDİR — detay ve ekstreyle
        // AYNI fonksiyon (CariBakiyeKurali). Emanet ayrı "emanetteHas" alanıyla döner. Tek sorgu.
        var emanetMap = await CariBakiyeKurali.EmanetEtkileri(db, islem.TenantId, idler);
        // P-DIL-BORC-KASA (Y18): altın emanetinin bırakıldığı gibi kalanı ("22 ayar · 12,50 g"); tek gruplu sorgu.
        var emanetBirakildigiMap = await Kuyumcu.Api.Modules.Emanet.EmanetKurali.BirakildigiGibi(
            db, islem.TenantId, idler, Kuyumcu.Core.Domain.Maden.Altin);

        // PAKET 40-A — SALT OKUNUR risk seviyesi rozeti. Yalnız GÜNCEL satır
        // ("daha yenisi YOK" deseni) ve yalnız LİSTELENEN cariler için tek
        // sorguda okunur (TB-19); sayfa başına bir ek sorgu, N+1 yok.
        // ⚠ Puan ve sebep listeye GİRMEZ: rozet seviyeden ibarettir, gerekçe
        //   Uyum ekranında (okuma izi düşen uçta) görünür.
        var riskMap = idler.Count != 0
            ? (await db.CariRiskProfilleri.AsNoTracking()
                .Where(p => p.TenantId == islem.TenantId && idler.Contains(p.CariId)
                    && !db.CariRiskProfilleri.Any(x => x.TenantId == islem.TenantId
                        && x.CariId == p.CariId && x.Olusturuldu > p.Olusturuldu))
                .Select(p => new { p.CariId, p.Seviye })
                .ToListAsync())
                .ToDictionary(p => p.CariId, p => p.Seviye)
            : [];

        return liste.Select(c =>
        {
            bakiyeMap.TryGetValue(c.Id, out var b);
            return new
            {
                id = c.Id,
                tenantId = c.TenantId,
                tur = c.Tur,
                ad = c.Ad,
                telefon = c.Telefon,
                email = c.Email,
                vergiNo = c.VergiNo,
                adres = c.Adres,
                notlar = c.Notlar,
                krediLimitiTl = c.KrediLimitiTl,
                varsayilanVadeGun = c.VarsayilanVadeGun,
                fiyatListesiId = c.FiyatListesiId,
                aktif = c.Aktif,
                olusturuldu = c.Olusturuldu,
                guncellendi = c.Guncellendi,
                tlBakiye = b?.TlBakiye ?? 0m,
                hasGramBakiye = CariBakiyeKurali.EmanetsizHas(b?.HasGramBakiye ?? 0m, emanetMap.GetValueOrDefault(c.Id)),
                emanetteHas = -emanetMap.GetValueOrDefault(c.Id),
                // null = bilinmiyor (eski satır) → ekran has gösterir; [] = emanet yok.
                emanetBirakildigi = emanetBirakildigiMap.TryGetValue(c.Id, out var eb) ? eb : Array.Empty<Kuyumcu.Core.Domain.EmanetAyarSatiri>(),
                // Döviz bakiyeleri (paket 27): sıfır bakiyede null DEĞİL 0 döner —
                // istemci "alan yok" ile "bakiye yok"u ayırt etmek zorunda kalmasın.
                usdBakiye = b?.UsdBakiye ?? 0m,
                eurBakiye = b?.EurBakiye ?? 0m,
                gbpBakiye = b?.GbpBakiye ?? 0m,
                // Paket 40-A: profili hiç hesaplanmamış caride null — "DUSUK"
                // uydurulmaz (Anayasa md.1); ekran rozeti o zaman çizmez.
                riskSeviye = riskMap.TryGetValue(c.Id, out var sev) ? sev : null,
                pepBeyani = c.PepBeyani,
                // PAKET P-16 (Y-27) — kart alanları listede de döner; ekran
                // "sunucuda var, ekranda yok" durumuna düşmesin (UX-S25).
                unvan = c.Unvan,
                kimlikTuru = c.KimlikTuru,
                dogumTarihi = c.DogumTarihi,
                uyruk = c.Uyruk,
                iskontoYuzde = c.IskontoYuzde,
                segment = c.Segment,
                hesapKodu = c.HesapKodu,
                // PAKET B-3 (D2): "Peşin Müşteri" kartı — ekran silme/pasife alma
                // yerine gerekçe gösterir (UX-S25). Ek alan; mevcut alan değişmedi.
                sistemCarisi = c.SistemCarisi,
                veresiyeEngeli = c.VeresiyeEngeli, veresiyeEngelSebebi = c.VeresiyeEngelSebebi,   // D1-07
                yuzukOlcusu = c.YuzukOlcusu,   // D2-12
            };
        }).ToList();
    }

    // ─── Tekil cari (bakiye dahil) ─────────────────────────────────────────────

    /// <summary>
    /// Denetleyici yolu: kişisel veri görüntülendiği için okuma izi düşer.
    /// İç akışlar (yazma sonrası yanıt üretimi) <see cref="Getir"/> kullanır.
    /// </summary>
    public async Task<object> GetirIzli(string id, AktifKullanici islem)
    {
        var cevap = await Getir(id, islem);
        await denetim.OkumaKaydet(islem, Varlik, id, "cari kartı görüntülendi");
        return cevap;
    }

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == id && b.TenantId == islem.TenantId);
        var emanetEtkisi = bakiye is null ? 0m : await CariBakiyeKurali.EmanetEtkisi(db, islem.TenantId, id);

        return new
        {
            id = cari.Id,
            tenantId = cari.TenantId,
            tur = cari.Tur,
            ad = cari.Ad,
            telefon = cari.Telefon,
            email = cari.Email,
            vergiNo = cari.VergiNo,
            adres = cari.Adres,
            notlar = cari.Notlar,
            krediLimitiTl = cari.KrediLimitiTl,
            varsayilanVadeGun = cari.VarsayilanVadeGun,
            fiyatListesiId = cari.FiyatListesiId,
            aktif = cari.Aktif,
            // Paket 40-A — PEP BEYANI (tespit değil, müşterinin beyanı).
            pepBeyani = cari.PepBeyani,
            pepGorev = cari.PepGorev,
            pepBeyanTarihi = cari.PepBeyanTarihi,
            // PAKET P-16 (Y-27)
            unvan = cari.Unvan,
            kimlikTuru = cari.KimlikTuru,
            dogumTarihi = cari.DogumTarihi,
            uyruk = cari.Uyruk,
            iskontoYuzde = cari.IskontoYuzde,
            segment = cari.Segment,
            hesapKodu = cari.HesapKodu,
            // PAKET B-3 (D2) — ek alan.
            sistemCarisi = cari.SistemCarisi,
            veresiyeEngeli = cari.VeresiyeEngeli, veresiyeEngelSebebi = cari.VeresiyeEngelSebebi,   // D1-07
            yuzukOlcusu = cari.YuzukOlcusu,   // D2-12
            olusturuldu = cari.Olusturuldu,
            guncellendi = cari.Guncellendi,
            bakiye = bakiye is null ? null : BakiyeFormat(bakiye, emanetEtkisi),
        };
    }

    // ─── PAKET B-3 (D2) — Peşin Müşteri sistem carisi ─────────────────────────

    /// <summary>
    /// Kiracının "Peşin Müşteri" kartı; yoksa tohumlanır (ADR-0027 deseni).
    /// Satış ekranı bu kartı ön seçer. Kişisel veri taşımaz → okuma izi düşmez.
    /// </summary>
    public async Task<Kuyumcu.Contracts.Sayfalar.PesinMusteri.PesinMusteriKarti> SistemCarisiGetir(AktifKullanici islem)
    {
        var kart = await SistemCarisi.GetirVeyaOlustur(db, denetim, islem);
        return new(kart.Id, kart.Ad, kart.Tur, kart.Aktif, SistemCarisi: true);
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, CariGuncelleDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // ── PAKET B-3 (D2 · K4) — Peşin Müşteri: ad/not düzenlenir, gerisi kilitli ──
        // Pasife alma YASAK (SaveChanges da reddeder); kimlik alanları YAZILAMAZ —
        // kimliği bilinen müşteri kendi kartını alır, kimliksiz kart kimlik taşımaz.
        if (cari.SistemCarisi)
        {
            if (dto.AktifVerildi && dto.Aktif == false)
                throw new BadRequestException(SistemCarisi.SilinemezMesaji);
            if ((dto.VergiNoVerildi && !string.IsNullOrWhiteSpace(dto.VergiNo))
                || (dto.KimlikTuruVerildi && !string.IsNullOrEmpty(dto.KimlikTuru))
                || (dto.DogumTarihiVerildi && dto.DogumTarihi is not null)
                || (dto.UyrukVerildi && !string.IsNullOrWhiteSpace(dto.Uyruk)))
                throw new BadRequestException(SistemCarisi.KimlikAlaniMesaji);
        }

        // NOT NULL kolonlar (tur/ad/aktif) yalnızca değerle güncellenir;
        // nullable kolonlar açıkça null gönderilerek temizlenebilir.
        // PAKET PATCH-5 (PATCH-3 sözleşmesi): zorunlu alan null = KORU, "" / boşluk = 400 alan.
        // Eskiden "" doğrudan yazılıyordu: RegularExpression ve MaxLength boş dizeyi GEÇERLİ sayar →
        // {"ad":""} adı boş, {"tur":""} türü boş bir cari bırakıyordu.
        if (dto.TurVerildi && dto.Tur is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Tur))
                throw new BadRequestException("Cari türü boşaltılamaz").AlanIle("tur");
            cari.Tur = dto.Tur;
        }
        if (dto.AdVerildi && dto.Ad is not null)
        {
            var yeniAd = dto.Ad.Trim();
            if (yeniAd.Length == 0) throw new BadRequestException("Cari adı boşaltılamaz").AlanIle("ad");
            KolonTavani.MetinSiniri(yeniAd, GS.CariAd, "ad", "Cari adı");
            cari.Ad = yeniAd;
        }
        // PATCH-5: kredi limiti null = SİL (isteğe bağlı), değer → 2 hane + kolon tavanı;
        // iskonto en çok 2 ondalık (decimal(5,2); sessiz yuvarlama yok).
        if (dto.KrediLimitiTlVerildi && dto.KrediLimitiTl is { } klHam)
        {
            if (klHam < 0m) throw new BadRequestException("Kredi limiti 0 veya daha büyük olmalıdır").AlanIle("krediLimitiTl");
            dto.KrediLimitiTl = KolonTavani.IsaretliTutar(klHam, "krediLimitiTl", "Kredi limiti");
        }
        if (dto.IskontoYuzdeVerildi)
            KolonTavani.OndalikSiniri(dto.IskontoYuzde, 2, "iskontoYuzde", "İskonto yüzdesi");
        if (dto.TelefonVerildi) cari.Telefon = dto.Telefon;
        if (dto.EmailVerildi) cari.Email = dto.Email;
        var vergiNoDegisti = false;
        if (dto.VergiNoVerildi)
        {
            // P-05 (Y-2): PATCH sözleşmesi korunur — alan GÖNDERİLDİYSE değişir,
            // null gönderilirse SİLİNİR. Yalnız değer gerçekten değiştiğinde
            // çakışma sorulur; kendi numarasını yeniden gönderen istek 409 almaz.
            var yeniVergiNo = DogalAnahtar.Normalle(dto.VergiNo);
            vergiNoDegisti = yeniVergiNo != cari.VergiNo;
            cari.VergiNo = yeniVergiNo;
        }
        if (dto.AdresVerildi) cari.Adres = dto.Adres;
        if (dto.NotlarVerildi) cari.Notlar = dto.Notlar;
        if (dto.KrediLimitiTlVerildi) cari.KrediLimitiTl = dto.KrediLimitiTl;
        if (dto.VarsayilanVadeGunVerildi) cari.VarsayilanVadeGun = dto.VarsayilanVadeGun;
        if (dto.FiyatListesiIdVerildi) cari.FiyatListesiId = string.IsNullOrEmpty(dto.FiyatListesiId) ? null : dto.FiyatListesiId;
        var pasiftenAktife = false;
        if (dto.AktifVerildi && dto.Aktif is not null)
        {
            pasiftenAktife = dto.Aktif.Value && !cari.Aktif;
            cari.Aktif = dto.Aktif.Value;
        }

        // P-05 (Y-2): kısıt AKTİF cariler arasında geçerli olduğu için çakışma
        // sorusu NİHAİ duruma sorulur — numara değiştiyse DE, kart pasiften
        // aktife alındıysa DA. Aksi hâlde "önce pasifleştir, sonra aynı numarayla
        // aç, sonra eskisini aktifleştir" yolu kısıtı 1062 ile patlatırdı.
        if (cari.Aktif && (vergiNoDegisti || pasiftenAktife))
            await VergiNoBenzersizMi(cari.VergiNo, islem.TenantId, haricId: cari.Id);

        // ── PAKET P-16 (Y-27) — KİMLİK / KYC ALANLARI, GERÇEK PATCH ─────────
        if (dto.UnvanVerildi) cari.Unvan = string.IsNullOrWhiteSpace(dto.Unvan) ? null : dto.Unvan;
        if (dto.KimlikTuruVerildi) cari.KimlikTuru = string.IsNullOrEmpty(dto.KimlikTuru) ? null : dto.KimlikTuru;
        if (dto.DogumTarihiVerildi) { DogumDenetle(dto.DogumTarihi); cari.DogumTarihi = dto.DogumTarihi; }   // PAKET GOC-B: K8f
        // KESIF-P2 (#22): alpha-3 zorunlu; yalnız DEĞİŞMEYEN eski (eşlenemeyen) değer aynen korunur —
        // kart başka bir alan için kaydedilebilsin, ekran "Uyruk kodunu düzeltin" rozetini gösterir.
        if (dto.UyrukVerildi) cari.Uyruk = UyrukNormalle(dto.Uyruk, mevcut: cari.Uyruk);
        if (dto.IskontoYuzdeVerildi) cari.IskontoYuzde = dto.IskontoYuzde;
        if (dto.SegmentVerildi) cari.Segment = string.IsNullOrEmpty(dto.Segment) ? null : dto.Segment;
        if (dto.YuzukOlcusuVerildi) cari.YuzukOlcusu = string.IsNullOrWhiteSpace(dto.YuzukOlcusu) ? null : dto.YuzukOlcusu.Trim();   // D2-12
        if (dto.HesapKoduVerildi)
            cari.HesapKodu = string.IsNullOrWhiteSpace(dto.HesapKodu) ? null : dto.HesapKodu.Trim();

        // Kural NİHAİ duruma uygulanır: numara bu istekte değişmemiş olsa bile
        // tür değişmişse hane kuralı yeniden sınanır (VKN → TCKN çevrilip
        // 10 haneli numara bırakılamaz).
        if (!KimlikNo.TurGecerli(cari.KimlikTuru)) throw new BadRequestException(KimlikNo.TurMesaj);
        if (!CariSegmentleri.Gecerli(cari.Segment)) throw new BadRequestException(CariSegmentleri.Mesaj);
        if (cari.IskontoYuzde is < 0m or > 100m)
            throw new BadRequestException("İskonto yüzdesi 0 ile 100 arasında olmalıdır");
        var kimlikHatasi = KimlikNo.Dogrula(cari.VergiNo, cari.KimlikTuru);
        if (kimlikHatasi is not null) throw new BadRequestException(kimlikHatasi).AlanIle("vergiNo");   // TCKN-DOGRULAMA: alan altı

        cari.Guncellendi = DateTime.UtcNow;

        await db.SaveChangesAsync();
        // PAKET YASAL-3 (§ 4.4 nokta 2): kimlik no ya da ad değiştiyse yeniden taranır.
        if (vergiNoDegisti || dto.AdVerildi)
            await Masak.YaptirimServisi.CariTara(db, islem.TenantId, cari.Id, cari.VergiNo, cari.Ad, islem);
        return await Getir(id, islem);
    }

    // ─── PEP beyanı (paket 40-A) ──────────────────────────────────────────────

    /// <summary>
    /// Siyasi nüfuz sahibi BEYANINI günceller. Yalnız pep_* kolonlarına dokunur;
    /// carinin diğer hiçbir alanı bu yoldan DEĞİŞMEZ.
    ///
    /// İş kuralı: beyan true iken görev BOŞ olamaz (400). Beyan false'a
    /// çekilirse görev ve tarih de TEMİZLENİR — "beyan yok" ile "beyan var ama
    /// görev eski" arasında tutarsız bir üçüncü hâl doğmasın.
    /// </summary>
    public async Task<object> PepGuncelle(string id, CariPepDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // PAKET B-3 (D2): kimliksiz kartta siyasi nüfuz BEYANI olamaz — beyan kişinindir.
        if (cari.SistemCarisi && dto.PepBeyaniVerildi && dto.PepBeyani == true)
            throw new BadRequestException(SistemCarisi.KimlikAlaniMesaji);

        var beyan = dto.PepBeyaniVerildi && dto.PepBeyani is not null ? dto.PepBeyani.Value : cari.PepBeyani;
        var gorev = dto.PepGorevVerildi ? dto.PepGorev?.Trim() : cari.PepGorev;

        // DTO DataAnnotations YALNIZ controller yolunda çalışır — servis kendi
        // kontrolünü yapar (K4 sözleşmesi).
        if (beyan && string.IsNullOrWhiteSpace(gorev))
            throw new BadRequestException(
                "Siyasi nüfuz sahibi beyanı işaretliyse görev alanı zorunludur. " +
                "Beyan müşterinindir; hangi görev nedeniyle beyan verildiği yazılmadan kayıt anlamsızdır.");

        if (beyan)
        {
            cari.PepBeyani = true;
            cari.PepGorev = gorev;
            // Beyanın alındığı an SUNUCUDA damgalanır (istemciden alınmaz).
            cari.PepBeyanTarihi = DateTime.UtcNow;
        }
        else
        {
            cari.PepBeyani = false;
            cari.PepGorev = null;
            cari.PepBeyanTarihi = null;
        }

        cari.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new
        {
            id = cari.Id,
            pepBeyani = cari.PepBeyani,
            pepGorev = cari.PepGorev,
            pepBeyanTarihi = cari.PepBeyanTarihi,
        };
    }

    // ─── Deaktif et (soft delete) ─────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // PAKET B-3 (D2 · K4): Peşin Müşteri SİLİNEMEZ / PASİFE ALINAMAZ.
        // Sunucu kapısı burada; SaveChanges'taki koruma ikinci hattır.
        if (cari.SistemCarisi)
            throw new BadRequestException(SistemCarisi.SilinemezMesaji);

        // ── PAKET P-13 (B-39) — BAĞLI KAYIT ÖZETİ + DENETİM İZİ ──────────────
        // Pasife alma ENGELLENMEZ: borçlu bir müşteri de kartı kapatılabilir
        // olmalıdır (borç kaydı silinmez, pasif carinin bakiyesi durur). Ama
        // "kaç hareketi ve ne kadar bakiyesi olan kartı kapattım" sorusu
        // sessiz kalamaz: özet hem yanıta hem denetim izine yazılır.
        var bagli = await BagliKayitlar(cari, islem.TenantId);
        var ozet = (Kuyumcu.Contracts.Sayfalar.Cariler.CariBagliKayit)bagli;

        cari.Aktif = false;
        cari.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, cari.Id,
            $"cari pasife alındı (hareket: {ozet.HareketSayisi} · TL bakiye: {ozet.TlBakiye} · "
            + $"has bakiye: {ozet.HasGramBakiye})");
        await db.SaveChangesAsync();

        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Cari deaktif edildi"), aktif = false, bagli = ozet };
    }

    /// <summary>
    /// PAKET P-13 (B-39) — pasife almadan ÖNCE ekranın gösterdiği bağlı kayıt
    /// sayıları. UX-S23: soyut kaydın etkisi kaydetmeden önce görünür.
    /// </summary>
    public async Task<object> BagliKayitlariGetir(string id, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);
        return await BagliKayitlar(cari, islem.TenantId);
    }

    /// <summary>Cariye bağlı kayıt sayıları — pasife alma uyarısının kaynağı.</summary>
    private async Task<object> BagliKayitlar(Data.Cari cari, string tenantId)
    {
        var hareket = await db.Hareketler.AsNoTracking()
            .CountAsync(h => h.TenantId == tenantId && h.CariId == cari.Id);
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.CariId == cari.Id);

        var emanetEtkisi = bakiye is null ? 0m : await CariBakiyeKurali.EmanetEtkisi(db, tenantId, cari.Id);
        return new Kuyumcu.Contracts.Sayfalar.Cariler.CariBagliKayit(
            cari.Id, cari.Aktif, hareket,
            bakiye?.TlBakiye ?? 0m, CariBakiyeKurali.EmanetsizHas(bakiye?.HasGramBakiye ?? 0m, emanetEtkisi),
            // PAKET RAPOR-YASLANDIRMA: döviz kolonları elle sayılmaz (yeni birim kaçmasın).
            DovizBirimleri.BakiyeVar(bakiye));
    }

    // ─── Bakiye getir ─────────────────────────────────────────────────────────

    public async Task<object> BakiyeGetir(string id, AktifKullanici islem)
    {
        await TenantCarisiBul(id, islem.TenantId);

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == id && b.TenantId == islem.TenantId);

        return bakiye is not null
            ? BakiyeFormat(bakiye, await CariBakiyeKurali.EmanetEtkisi(db, islem.TenantId, id))
            : BosBakiye(id);
    }

    // ─── Veresiye girişi ──────────────────────────────────────────────────────
    /// <summary>
    /// Müşteri bize TL (ve/veya has gram) borçlanır.
    /// Bakiye: tlBakiye += tlTutari, hasGramBakiye += hasGrami
    /// Hareket: tlTutari = +pozitif (alacak), hasGrami = -hasGrami (altın bizden gitti)
    /// </summary>
    public async Task<object> VeresiyeGir(string id, VeresiyeGirisDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);
        // PAKET PATCH-5 — girdi kapısı (servis içi): TL önce 2 haneye yuvarlanır sonra ≤ 0
        // (0,004 → 0,00 veresiye satırı doğmaz), has 6 haneye (AwayFromZero) + kolon tavanları.
        // Yuvarlanmamış tutar hareket ile bakiyeye FARKLI yuvarlanabiliyordu (eksi bakiyede yarım kuruş).
        dto.TlTutari = KolonTavani.PozitifTutar(dto.TlTutari, "tlTutari", "Tutar");
        if (dto.HasGrami is { } vh)
        {
            dto.HasGrami = KolonTavani.IsaretliHas(vh, "hasGrami", "Has gram");
            if (dto.HasGrami < 0m) throw new BadRequestException("Has gram eksi olamaz").AlanIle("hasGrami");
        }
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        // Kredi limiti + vade — tek kapı (VadeKapisi). Limit metni buradan taşındı;
        // artık veresiye satış ve takas/işçilik farkı da AYNI kontrolden geçiyor.
        await Vade.VadeKapisi.KrediLimitiDogrula(db, cari, dto.TlTutari, dto.KurSnapshotId);
        if (cari.VeresiyeEngeli && dto.HasGrami > 0m)   // D1-07: altın veresiyesi de kara listeye takılır
            throw new BadRequestException(Vade.VadeKapisi.VeresiyeEngelMetni).AlanIle("cariId");

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var now = DateTime.UtcNow;
        var vade = Vade.VadeKapisi.VadeHesapla(cari, dto.VadeGun, now);
        var hasGrami = dto.HasGrami ?? 0m;
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, id, dto.TlTutari, dto.Masak, now);   // MASAK Faz 1

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, id, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ, kümülatif kapılar kilit altında yinelenir
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, id, dto.TlTutari, dto.Masak, now, masakGerek);
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Veresiyeden(dto.TlTutari, vade),
            new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = "VERESIYE",
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = dto.TlTutari,   // +pozitif: alacak
            HasGrami = -hasGrami,      // -negatif: altın bizden gitti
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        masak.Kaydet(db, masakGerek, hareketId, id, dto.TlTutari, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2

        // Bakiye: tlBakiye += dto.TlTutari, hasGramBakiye += hasGrami
        await BakiyeGuncelle(id, islem.TenantId, dto.TlTutari, hasGrami, now);
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Veresiye: cari={CariId} tl={Tl} has={Has}",
            islem.TenantId, id, dto.TlTutari, hasGrami);

        return await Getir(id, islem);
    }

    // ─── Tahsilat girişi ──────────────────────────────────────────────────────
    /// <summary>
    /// Müşteri borcunu öder (TL ve/veya has gram).
    /// Bakiye: tlBakiye -= tlTutari, hasGramBakiye -= hasGrami
    /// Hareket: tlTutari = -negatif (ödeme), hasGrami = +pozitif (altın bize geldi)
    /// </summary>
    public async Task<object> TahsilatGir(string id, TahsilatGirisDto dto, AktifKullanici islem)
    {
        await TahsilatGirHareket(id, dto, islem);
        return await Getir(id, islem);
    }

    /// <summary>
    /// P-23c — tahsilatın ÇEKİRDEĞİ; doğan defter satırının kimliğini döner.
    ///
    /// ⚠ NEDEN AYRILDI: <see cref="TahsilatGir"/> cari detayını döner ve
    /// ekranın sözleşmesi budur — değiştirilmedi. Vezne fişi ise tahsil sonucunda
    /// doğan HAREKETİN kimliğine muhtaçtır (vezne_fisleri.hareket_fis_id).
    /// Kimlik dönmeseydi vezne o satırı zaman damgasından TAHMİN ederdi.
    /// Mantık KOPYALANMADI: iki yol da bu tek gövdeyi çağırır.
    /// </summary>
    public async Task<string> TahsilatGirHareket(string id, TahsilatGirisDto dto, AktifKullanici islem)
    {
        await TenantCarisiBul(id, islem.TenantId);
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        // PAKET PATCH-5 — önce yuvarla (TL 2, has 6 hane AwayFromZero), SONRA sıfır/eksi kontrolü;
        // 0,001 TL "en az bir tutar" kapısından geçip 0,00 tahsilat satırı yazıyordu. + kolon tavanları.
        var tlTutari = KolonTavani.IsaretliTutar(dto.TlTutari ?? 0m, "tlTutari", "Tutar");
        var hasGrami = KolonTavani.IsaretliHas(dto.HasGrami ?? 0m, "hasGrami", "Altın miktarı");
        if (tlTutari < 0m) throw new BadRequestException("Tutar eksi olamaz").AlanIle("tlTutari");
        if (hasGrami < 0m) throw new BadRequestException("Altın miktarı eksi olamaz").AlanIle("hasGrami");

        // PAKET SON-KALANLAR (hasım DÜŞÜK, Pk1440__22): 0,001 yazan kullanıcıya "en az bir tutar girilmelidir"
        // demek yazdığıyla çelişiyordu ve "has gram" jargonu taşıyordu. Kullanıcı bir değer YAZDIYSA ama
        // yuvarlanınca sıfıra düştüyse en küçük kaydedilebilir değer söylenir; hiç yazmadıysa sade istek.
        if (tlTutari == 0m && hasGrami == 0m)
        {
            if ((dto.TlTutari ?? 0m) > 0m)
                throw new BadRequestException(KolonTavani.EnAzTutarMesaji("Tutar")).AlanIle("tlTutari");
            if ((dto.HasGrami ?? 0m) > 0m)
                throw new BadRequestException("Altın miktarı en az 0,000001 gram olmalıdır — daha küçük miktar kaydedilemez").AlanIle("hasGrami");
            throw new BadRequestException("Tutar ya da altın girin.").AlanIle("tlTutari");
        }

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var now = DateTime.UtcNow;
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, id, tlTutari, dto.Masak, now);   // MASAK Faz 1 (nakit tahsilat MASAK'ın göbeği)

        // ── KASA BACAĞI (paket UX-4b madde 9) ────────────────────────────────
        // Satıştaki kuralın AYNISI (SatisModulu.cs "Nakit/Kart/Havale satış için
        // kasa ID gerekli"). DTO'daki IValidatableObject yalnız controller yolunda
        // çalışır; servis doğrudan çağrıldığında bu satır kapıyı kapatır.
        if (tlTutari > 0m && string.IsNullOrWhiteSpace(dto.KasaId))
            throw new BadRequestException("Nakit/Kart/Havale tahsilat için kasa ID gerekli");

        // K6 (TL yalnız TL kasasına) + gün sonu kapanışı kilidi — İKİSİ DE bu tek
        // çağrının içinde. Tahsilat için YENİ bir kasa doğrulama kodu YAZILMAZ.
        Data.Kasa? kasa = tlTutari > 0m
            ? await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now)
            : null;
        // PAKET PATCH-5: kasa bakiyesi decimal(15,2) — sonuç sığmazsa 400 alan:tlTutari, HİÇBİR Add'den önce.
        if (kasa is not null) KolonTavani.BakiyeSigar(kasa.Bakiye + tlTutari, "tlTutari", "Tutar");

        // 459 s. VUK Genel Tebliği — nakit tahsilat haddi. Transaction'dan ÖNCE.
        if (dto.OdemeTuru == "NAKIT" && tlTutari > 0m)
            await tevsik.Dogrula(db, islem.TenantId, id, tlTutari, now);
        // PAKET YASAL-2 (TB-15): büyük işlemden kalan borcun taksiti de nakit alınamaz.
        var taksit = dto.OdemeTuru == "NAKIT" && tlTutari > 0m
            ? await tevsik.TaksitDogrula(db, islem.TenantId, id, tlTutari, tlTutari, now)
            : null;

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, id, now);   // PAKET KAPORA-ESZAMAN: dış işlem varsa ona katılır (sipariş kaporası)
        // PAKET MASAK-ESZAMAN: CARİ SATIRI KİLİDİ alındı; MASAK + tevsik + taksit kapıları kilit altında, güncel toplamla yinelenir.
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, id, tlTutari, dto.Masak, now, masakGerek);
        if (dto.OdemeTuru == "NAKIT" && tlTutari > 0m)
        {
            await tevsik.Dogrula(db, islem.TenantId, id, tlTutari, now);
            taksit = await tevsik.TaksitDogrula(db, islem.TenantId, id, tlTutari, tlTutari, now);
        }
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Etiketten(dto.OdemeTuru, tlTutari, vade: null),
            new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = "TAHSILAT",
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = -tlTutari,      // -negatif: ödeme → borç azaldı
            HasGrami = hasGrami,       // +pozitif: altın bize geldi
            // Ödeme türü ARTIK KAYDEDİLİYOR: kümülatif nakit toplamı bunu okur.
            // PARA-DEMO (madde 2/5): yalnız altınla (TL 0) tahsilatta para el DEĞİŞTİRMEDİ → etiket YOK
            // (P-23 kuralı, takastaki OdemeTutarKapisi.Etiket ile aynı). Eskiden formun varsayılanı
            // "NAKIT" yazılıyordu: ekstrede "Nakit" görünüyor, ödeme kalemsiz NAKIT satırı doğuyordu.
            OdemeTuru = Modules.Satis.OdemeTutarKapisi.Etiket(tlTutari, dto.OdemeTuru),
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        masak.Kaydet(db, masakGerek, hareketId, id, tlTutari, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2
        TaksitDogrulanamadiIzi(taksit, islem, hareketId, tlTutari);   // PAKET YASAL-2 (K-A)

        // Kasa bacağı AYNI transaction içinde: cari bakiyesi düşerken kasa artar.
        // İŞARETLİ (+giriş) yazılır; Math.Abs KULLANILMAZ (SatisModulu'ndaki eski
        // mutlak-değer hatası tekrarlanmasın — defterden bakiye türetilebilir kalsın).
        if (kasa is not null)
        {
            await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            kasa.Bakiye = Math.Round(kasa.Bakiye + tlTutari, 2, MidpointRounding.AwayFromZero);
            kasa.Guncellendi = now;
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id          = Kimlik.Ulid(),
                TenantId    = islem.TenantId,
                KasaId      = kasa.Id,
                HareketId   = hareketId,
                Tutar       = Math.Round(tlTutari, 2, MidpointRounding.AwayFromZero),
                ParaBirimi  = kasa.ParaBirimi,
                OdemeTuru   = dto.OdemeTuru,
                Tarih       = now,
                Olusturuldu = now,
            });
        }

        // Bakiye: tlBakiye -= tlTutari (borç azalır), hasGramBakiye -= hasGrami (altın borcu azalır)
        await BakiyeGuncelle(id, islem.TenantId, -tlTutari, -hasGrami, now);
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();   // devralındıysa commit SAHİBİNDE

        logger.LogInformation(
            "[{TenantId}] Tahsilat: cari={CariId} tl={Tl} has={Has} kasa={KasaId}",
            islem.TenantId, id, tlTutari, hasGrami, kasa?.Id);

        return hareketId;
    }

    /// <summary>
    /// PAKET YASAL-2 (K-A önerisi): kaynağı bilinmeyen (açılış/devir) bakiyeye düşen
    /// nakit GEÇER ama iz bırakır. Detayda kişisel veri YOK — yalnız sayısal özet.
    /// SaveChanges çağırmaz: tahsilatın transaction'ıyla birlikte yazılır.
    /// </summary>
    private void TaksitDogrulanamadiIzi(
        Modules.Tevsik.TaksitSonuc? taksit, AktifKullanici islem, string hareketId, decimal nakit)
    {
        if (taksit is not { Dogrulanamayan: > 0m } t) return;
        denetim.YazmaKaydet(islem, "TEVSIK_TAKSIT_DOGRULANAMADI", hareketId,
            FormattableString.Invariant(
                $"nakit {nakit:0.00} TL; kaynağı bilinmeyen bakiyeden {t.Dogrulanamayan:0.00} TL"),
            hassas: false);
    }

    // ─── "Kasasız tahsilat" raporu (paket UX-4b madde 9 — Mehmet koşul 3) ─────
    /// <summary>
    /// Kasa bacağı ZORUNLU olmadan ÖNCE kaydedilmiş TL tahsilatlarını listeler.
    /// <para>
    /// ⚠ <b>KASA TAHMİNİ YAPILMAZ, GERİ DOLDURMA YAPILMAZ.</b> Hareket tablosunda
    /// kasa_id kolonu hiç olmadı; eski kayıtlar için kasa uydurmak Anayasa
    /// md.1'e aykırıdır. Rapor yalnızca <b>tarih eşiğiyle</b> üretilir:
    /// eşikten önceki TAHSILAT hareketleri ∧ OdemeTuru ∈ {NAKIT, KART, HAVALE}
    /// ∧ TL tutarı var. Eski kayıtlar DEĞİŞTİRİLMEZ (append-only).
    /// </para>
    /// <para>
    /// Eşik: sorgudan gelir; gelmezse yapılandırmadaki
    /// <c>Tahsilat:KasaZorunluBaslangic</c> okunur. İkisi de yoksa tahmin
    /// EDİLMEZ — 400 döner (fail-closed). Bu değeri kurucu/mali müşavir
    /// belirler: kasa bacağının canlıya çıktığı an.
    /// </para>
    /// </summary>
    public async Task<object> KasasizTahsilatlar(
        AktifKullanici islem, DateTime? esik, int limit = 200)
    {
        var yapilandirilan = yapilandirma?["Tahsilat:KasaZorunluBaslangic"];
        DateTime? esikSon = esik
            ?? (DateTime.TryParse(yapilandirilan, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal, out var y)
                ? y : null);

        if (esikSon is not { } sinir)
            throw new BadRequestException(
                "Kasasız tahsilat raporu için tarih eşiği gerekli: ?esik=YYYY-MM-DD " +
                "ya da yapılandırmada Tahsilat:KasaZorunluBaslangic. Eşik tahmin edilmez.");

        if (limit is < 1 or > 1000) limit = 200;

        var satirlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId
                     && h.Tur == "TAHSILAT"
                     && h.Tarih < sinir
                     && h.TlTutari != 0m
                     && (h.OdemeTuru == "NAKIT" || h.OdemeTuru == "KART" || h.OdemeTuru == "HAVALE"))
            .OrderByDescending(h => h.Tarih)
            .Take(limit)
            .Select(h => new
            {
                h.Id,
                h.CariId,
                h.Tarih,
                h.OdemeTuru,
                tlTutari = -h.TlTutari,   // deftere negatif yazılır; raporda pozitif okunur
                h.Aciklama,
            })
            .ToListAsync();

        return new
        {
            esik = sinir,
            adet = satirlar.Count,
            limit,
            sinirDegildi = satirlar.Count < limit,
            aciklama = "Bu tahsilatlar kasa bacağı zorunlu olmadan önce kaydedildi; "
                     + "kasa bilgisi YOKTUR ve geriye dönük üretilmez.",
            satirlar,
        };
    }

    // ─── Manuel düzeltme fişi (paket 33) ──────────────────────────────────────
    /// <summary>
    /// Cari bakiyesini GEREKÇELİ olarak elle düzeltir — devir farkı, eski
    /// sistemden gelen sapma, mutabakat düzeltmesi.
    ///
    /// Bakiye: tlBakiye += TlDelta, hasGramBakiye += HasDelta
    /// Hareket: TlTutari = +TlDelta, HasGrami = -HasDelta (VERESİYE sözleşmesiyle
    /// aynı işaret düzeni: bakiye artışı deftere negatif has olarak düşer).
    ///
    /// ⚠ PARA EL DEĞİŞTİRMEZ: <see cref="OdemeIzi.Yok"/> — nakit/veresiye payı
    /// ve vade YOKTUR. Bu bilinçlidir: düzeltmeyle doğan borcun vadesi
    /// BİLİNMEZ, vade raporunda "vadesi bilinmeyen bakiye" olarak görünür;
    /// bugüne vade yazmak müşteriyi haksız yere gecikmiş gösterirdi. MASAK ve
    /// 459 s. VUK tevsik kapıları da çalışmaz (SAYIM_DUZELTME ile aynı gerekçe).
    ///
    /// ⚠ DENETİM İZİ — İKİ KATMAN (ekip oyu Q1, 2026-09-15):
    ///   1) YAZMA izi OTOMATİKTİR (<see cref="DenetimYazmaDinleyicisi"/>): her
    ///      Added varlık için EKLEME satırı düşer, ama Detay'ı boş kalır.
    ///   2) Bu yüzden ayrıca <c>DenetimServisi.YazmaKaydet</c> çağrılır:
    ///      VarlikTuru "CariDuzeltme", Hassas = true, Detay = delta + GEREKÇE.
    ///      "Kim, ne zaman, NEDEN bakiye düzeltti" sorusu tek tablodan
    ///      cevaplanabilsin diye (Mehmet, 06 Güvenlik şartı). Gerekçe ayrıca
    ///      <c>hareketler.Aciklama</c>'da da durur — iş tablosu değiştirilebilir,
    ///      denetim tablosu append-only'dir.
    /// </summary>
    /// <param name="tur">
    /// Deftere yazılacak hareket türü. Varsayılan CARI_DUZELTME.
    /// <para>
    /// ⚠ PAKET T-2 (Bertan'ın birleşik koşulu): açılış bakiyesi "ACILIS" türüyle
    /// yazılır ama AYRI BİR YAZMA YOLU AÇILMAZ — ikiz defter yolu doğmasın diye
    /// bu metot parametrelendi. Kabul edilen tek alternatif "ACILIS"tir;
    /// başka bir değer <see cref="InvalidOperationException"/> fırlatır ki
    /// gelecekte üçüncü bir tür buradan sessizce sızmasın.
    /// </para>
    /// </param>
    /// <param name="krediLimitiUygula">
    /// PAKET T-2 kararı (uçlar ↔ açık_sorular çelişkisi mühürlendi): açılışta
    /// kredi limiti kapısı REDDETMEZ, UYARIR. Gerekçe: açılış MEVCUT borcun
    /// kaydıdır, yeni risk doğurmaz; limitinin üstünde borcu olan gerçek
    /// müşteri açılışta reddedilirse dükkân hiç kurulamaz. Geçirme kararı
    /// bedavaya değildir: prova raporunda satır UYARI olarak çıkar ve uygula
    /// gövdesinde SAHİP'in ayrı onayı (limitAsimiOnayi) aranır.
    /// </param>
    public async Task<object> Duzeltme(
        string id, CariDuzeltmeDto dto, AktifKullanici islem,
        string tur = "CARI_DUZELTME", bool krediLimitiUygula = true)
    {
        await DuzeltmeYaz(id, dto, islem, tur, krediLimitiUygula);
        return await Getir(id, islem);
    }

    /// <summary>
    /// PAKET T-2 — açılış bakiyesini AYNI yoldan yazar ve üretilen ACILIS
    /// hareketinin kimliğini döner (açılış kalemi izine bağlanır).
    /// <para>
    /// ⚠ Bu metot yeni bir yazma yolu DEĞİLDİR: gövdesi yoktur,
    /// <see cref="DuzeltmeYaz"/>'ı çağırır. Ayrı bir isimle durmasının tek
    /// sebebi çağıranın hareket kimliğine ihtiyaç duymasıdır.
    /// </para>
    /// </summary>
    public Task<string> AcilisYaz(string id, CariDuzeltmeDto dto, AktifKullanici islem) =>
        DuzeltmeYaz(id, dto, islem, "ACILIS", krediLimitiUygula: false);

    private async Task<string> DuzeltmeYaz(
        string id, CariDuzeltmeDto dto, AktifKullanici islem,
        string tur, bool krediLimitiUygula)
    {
        if (tur is not ("CARI_DUZELTME" or "ACILIS"))
            throw new InvalidOperationException(
                $"Cari düzeltme yolu yalnız CARI_DUZELTME ve ACILIS türlerini yazar; gelen: '{tur}'.");

        var cari = await TenantCarisiBul(id, islem.TenantId);

        // ⚠ DataAnnotations yalnız denetleyici yolunda çalışır; servis doğrudan
        // çağrıldığında (testler, iç akışlar) hiçbir şey doğrulamaz. Bu yüzden
        // gerekçe ve sıfır delta kontrolü BURADA elle yapılır.
        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < 20)
            throw new BadRequestException("Düzeltme gerekçesi en az 20 karakter olmalıdır");
        if (gerekce.Length > 500)
            throw new BadRequestException("Düzeltme gerekçesi en çok 500 karakter olabilir");

        // ── Döviz deltası (paket 27): ikisi BİRLİKTE verilir ────────────────
        // Yarım gövde (yalnız para birimi ya da yalnız delta) sessizce
        // yok sayılmaz: kullanıcı "girdim" sanıp bakiyeyi düzeltmemiş olur.
        // PAKET PATCH-5 — önce yuvarla (TL/döviz 2, has 6 hane AwayFromZero), SONRA sıfır kontrolü:
        // 0,001 TL "en az bir fark" kapısından geçip 0,00 düzeltme fişi yazıyordu. + kolon tavanları
        // (hareketler.tl_tutari/doviz_tutar decimal(15,2), has_grami decimal(15,6)).
        // ⚠ AÇILIŞ (T-2, toplu devir) İSTİSNASI: sıfır kontrolü açılışta HAM değere yapılır (eski davranış).
        //   Eski sistemden gelen 0,004 TL'lik artık 0,00'a yuvarlanıp listede görünmez (RAPORYASLANDIRMA
        //   sınır testi bu sözleşmeyi tutar); yuvarlanınca sıfırlanan tek satır yüzünden TÜM açılış
        //   (tek transaction) geri alınmaz. Yuvarlama ve kolon tavanı açılışta da uygulanır.
        var hamSifir = dto.TlDelta == 0m && dto.HasDelta == 0m;
        var hamDovizSifir = (dto.DovizDelta ?? 0m) == 0m;
        var yuvarlanmisSifirKontrolu = tur != "ACILIS";
        dto.TlDelta = KolonTavani.IsaretliTutar(dto.TlDelta, "tlDelta", "TL farkı");
        dto.HasDelta = KolonTavani.IsaretliHas(dto.HasDelta, "hasDelta", "Has gram farkı");
        if (dto.DovizDelta is { } ddHam)
            dto.DovizDelta = KolonTavani.IsaretliTutar(ddHam, "dovizDelta", "Döviz farkı");
        string? dovizPb = null;
        var dovizDelta = dto.DovizDelta ?? 0m;
        if (dto.DovizParaBirimi is not null || dto.DovizDelta is not null)
        {
            dovizPb = DovizBakiye.Dogrula(dto.DovizParaBirimi);
            if (yuvarlanmisSifirKontrolu ? dovizDelta == 0m : hamDovizSifir)
                throw new BadRequestException("Döviz düzeltmesinde fark sıfır olamaz").AlanIle("dovizDelta");
        }

        if ((yuvarlanmisSifirKontrolu ? dto.TlDelta == 0m && dto.HasDelta == 0m : hamSifir) && dovizPb is null)
            throw new BadRequestException("Düzeltme için en az bir fark girilmelidir (TL, has gram veya döviz)").AlanIle("tlDelta");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        // Borcu ARTIRAN düzeltme kredi limitine tabidir; azaltan değildir.
        // (KrediLimitiDogrula ≤ 0 deltada kendiliğinden atlar.)
        if (krediLimitiUygula)
            await Vade.VadeKapisi.KrediLimitiDogrula(db, cari, dto.TlDelta, dto.KurSnapshotId);

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        // ⚠ Buraya kadarki 400'ler HİÇBİR Add'den ÖNCE fırlar: aynı DbContext
        // ile ikinci bir çağrı yapılırsa yarım kalmış değişiklik izlenmez.
        var now = DateTime.UtcNow;

        // PAKET T-2: dış bir transaction VARSA ona katılır, commit'i sahibine
        // bırakır (Common/VeriIslemi.cs). Tek başına çağrıldığında davranış
        // AYNIDIR. Açılışın "bir satır patlarsa hepsi geri alınır" değişmezi
        // ancak böyle sağlanır.
        await using var tx = await db.Database.IsBaslatVeyaDevral();
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,
            new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = tur,
            Tarih = now,
            Aciklama = tur == "ACILIS" ? $"Açılış devri: {gerekce}" : $"Düzeltme fişi: {gerekce}",
            TlTutari = dto.TlDelta,     // +pozitif: alacak arttı
            HasGrami = -dto.HasDelta,   // işaret VERESİYE sözleşmesiyle aynı
            // Döviz bacağı (paket 27): TL bacağı KİRLETİLMEZ — düzeltmede
            // TL karşılığı YAZILMAZ, çünkü düzeltme fişinde para el değiştirmez
            // ve MASAK/tevsik kapıları zaten çalışmaz (SAYIM_DUZELTME gerekçesi).
            ParaBirimi = dovizPb ?? "TRY",
            DovizTutar = dovizPb is null ? null : dovizDelta,
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        await BakiyeGuncelle(id, islem.TenantId, dto.TlDelta, dto.HasDelta, now, dovizPb, dovizDelta);

        denetim.YazmaKaydet(islem, tur == "ACILIS" ? "ACILIS" : "CariDuzeltme", hareketId,
            FormattableString.Invariant(
            $"tl={dto.TlDelta:+0.##;-0.##;0} has={dto.HasDelta:+0.######;-0.######;0}") +
            (dovizPb is null ? "" : FormattableString.Invariant($" {dovizPb}={dovizDelta:+0.##;-0.##;0}")) +
            $" · {gerekce}");

        await db.SaveChangesAsync();
        // tx null ise transaction DIŞARININ: commit onun sorumluluğudur.
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Cari {Tur}: cari={CariId} tl={Tl} has={Has} hareket={HareketId}",
            islem.TenantId, tur, id, dto.TlDelta, dto.HasDelta, hareketId);

        return hareketId;
    }

    // ═══ DÖVİZ DEFTERİ (paket 27 Faz A) ══════════════════════════════════════

    /// <summary>Kur fotoğrafını kiracı sınırıyla birlikte getirir (K1).</summary>
    private async Task<KurSnapshot> KurGetir(string kurSnapshotId, string tenantId) =>
        await db.KurSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == kurSnapshotId && s.TenantId == tenantId)
        ?? throw new NotFoundException("Kur kaydı bulunamadı");

    /// <summary>
    /// DÖVİZ VERESİYE — borç, doğduğu para biriminde durur.
    ///
    /// <para>
    /// Bakiye: ilgili döviz kolonu += tutar. Defter satırı:
    /// Tur=DOVIZ_VERESIYE, ParaBirimi=seçilen, DovizTutar=+tutar,
    /// TlTutari=+TL karşılığı (MASAK eşiği TL üzerinden işlediği için),
    /// <b>VeresiyeTutar=0 ve vade YOK</b> (K27-E) — böylece
    /// <c>VadeKapisi.AcikBorclar</c> bu satırı TL borcu SAYMAZ.
    /// </para>
    /// <para>
    /// ⚠ VADE: Faz A'da döviz borcunun vadesi YOKTUR. <paramref name="dto"/>
    /// içindeki <c>VadeGun</c> yalnız DOĞRULANIR (aralık dışı değer sessizce
    /// yutulmasın) ve deftere YAZILMAZ; ekran bunu "vadelendirilmedi" diye
    /// açıkça söyler. Bugüne vade yazmak müşteriyi haksız yere gecikmiş
    /// gösterirdi (paket 33 SAYIM_DUZELTME gerekçesiyle aynı).
    /// </para>
    /// <para>
    /// ⚠ TEVSİK ÇALIŞMAZ: nakit el değiştirmiyor, <see cref="OdemeIzi.Yok"/>.
    /// </para>
    /// </summary>
    public async Task<object> DovizVeresiyeGir(string id, DovizVeresiyeDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // ⚠ DataAnnotations yalnız denetleyici yolunda çalışır — servis
        //   doğrudan çağrıldığında hiçbir şey doğrulamaz (proje değişmezi).
        var paraBirimi = DovizBakiye.Dogrula(dto.ParaBirimi);
        // PAKET PATCH-5: önce yuvarla (doviz_tutar decimal(15,2)), sonra ≤ 0 + kolon tavanı.
        dto.Tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Tutar");
        if (dto.VadeGun is < 0 or > 3650)
            throw new BadRequestException("Vade 0 ile 3650 gün arasında olmalıdır");

        var kur = await KurGetir(dto.KurSnapshotId, islem.TenantId);
        var oran = DovizBakiye.KurZorunlu(kur, paraBirimi);      // fail-closed
        var tlKarsiligi = DovizBakiye.TlKarsiligi(dto.Tutar, oran);
        // PATCH-5: TL karşılığı hareketler.tl_tutari decimal(15,2) kolonuna yazılır — sığmazsa 400 alan:tutar.
        KolonTavani.Denetle(tlKarsiligi, 2, KolonTavani.D15_2, "tutar", "Tutarın TL karşılığı");

        // Kredi limiti: döviz borcunun TL karşılığı da limite sayılır (K27-I).
        await Vade.VadeKapisi.KrediLimitiDogrula(db, cari, tlKarsiligi, dto.KurSnapshotId);

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var now = DateTime.UtcNow;
        // MASAK eşiği TL üzerinden işler — efektif miktarla DEĞİL (M85).
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, id, tlKarsiligi, dto.Masak, now);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, id, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ, kümülatif kapılar kilit altında yinelenir
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, id, tlKarsiligi, dto.Masak, now, masakGerek);
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,          // K27-E: veresiye payı YOK, nakit payı YOK
            new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = DovizBakiye.TurVeresiye,
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = tlKarsiligi,       // +pozitif: alacak (yalnız TL karşılığı)
            ParaBirimi = paraBirimi,
            DovizTutar = dto.Tutar,       // +pozitif: efektif alacak
            HasGrami = 0m,
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        masak.Kaydet(db, masakGerek, hareketId, id, tlKarsiligi, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        await BakiyeGuncelle(id, islem.TenantId, 0m, 0m, now, paraBirimi, dto.Tutar);
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Döviz veresiye: cari={CariId} {ParaBirimi}={Tutar} tl={Tl}",
            islem.TenantId, id, paraBirimi, dto.Tutar, tlKarsiligi);

        return await Getir(id, islem);
    }

    /// <summary>
    /// DÖVİZ TAHSİLAT — efektif döviz kasaya girer, borç aynı para biriminde azalır.
    ///
    /// <para>
    /// ⚠ <b>ÇAPRAZ KAPATMA YOKTUR (K27-G):</b> USD borcu yalnız USD ile azalır.
    /// USD borcunu TL ile kapatmak kur farkıyla birlikte fiilen döviz satışıdır
    /// ve 32 Sayılı Karar uyarınca yetki belgesi ister — Faz A bunu AÇMAZ.
    /// Farklı para biriminde tahsilat gelirse o para biriminin kolonu eksiye
    /// düşer; USD borcu OLDUĞU GİBİ kalır (bakiyeler karışmaz).
    /// </para>
    /// <para>
    /// ⚠ <b>EFEKTİF NAKİTTİR (A12):</b> elden alınır, hiçbir finansal kurum
    /// kanalından geçmez → <see cref="OdemeIzi.Nakitten"/> ve
    /// <c>TevsikKapisi.Dogrula</c> TL karşılığıyla çağrılır (459 s. VUK).
    /// Kasaya ise <b>EFEKTİF</b> miktar yazılır, TL karşılığı DEĞİL.
    /// </para>
    /// </summary>
    public async Task<object> DovizTahsilatGir(string id, DovizTahsilatDto dto, AktifKullanici islem)
    {
        await TenantCarisiBul(id, islem.TenantId);

        var paraBirimi = DovizBakiye.Dogrula(dto.ParaBirimi);
        // PAKET PATCH-5: önce yuvarla (doviz_tutar decimal(15,2)), sonra ≤ 0 + kolon tavanı.
        dto.Tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Tutar");
        if (string.IsNullOrWhiteSpace(dto.KasaId))
            throw new BadRequestException("Döviz tahsilatı için kasa ID gerekli");
        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var kur = await KurGetir(dto.KurSnapshotId, islem.TenantId);
        var oran = DovizBakiye.KurZorunlu(kur, paraBirimi);
        var tlKarsiligi = DovizBakiye.TlKarsiligi(dto.Tutar, oran);
        // PATCH-5: TL karşılığı hareketler.tl_tutari decimal(15,2) kolonuna yazılır — sığmazsa 400 alan:tutar.
        KolonTavani.Denetle(tlKarsiligi, 2, KolonTavani.D15_2, "tutar", "Tutarın TL karşılığı");

        var now = DateTime.UtcNow;

        // K6 (para birimi) + gün kapanışı kilidi — İKİSİ de bu tek çağrıda.
        // Beklenen para birimi TAHSİL EDİLEN dövizdir, "TRY" DEĞİL (M83).
        var kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId, islem.TenantId, paraBirimi, now);
        KolonTavani.BakiyeSigar(kasa.Bakiye + dto.Tutar, "tutar", "Tutar");   // PATCH-5: Add'den önce

        // Üç kapı da transaction AÇILMADAN önce (Mehmet koşulu).
        await tevsik.Dogrula(db, islem.TenantId, id, tlKarsiligi, now);
        var taksit = await tevsik.TaksitDogrula(db, islem.TenantId, id, tlKarsiligi, tlKarsiligi, now, dovizMi: true);   // PAKET YASAL-2 (rakam TL karşılığı)
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, id, tlKarsiligi, dto.Masak, now);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, id, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ, kümülatif kapılar kilit altında yinelenir
        await tevsik.Dogrula(db, islem.TenantId, id, tlKarsiligi, now);
        taksit = await tevsik.TaksitDogrula(db, islem.TenantId, id, tlKarsiligi, tlKarsiligi, now, dovizMi: true);
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, id, tlKarsiligi, dto.Masak, now, masakGerek);
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Nakitten(tlKarsiligi),   // efektif döviz NAKİTTİR (A12)
            new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = DovizBakiye.TurTahsilat,
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = -tlKarsiligi,     // -negatif: ödeme
            ParaBirimi = paraBirimi,
            DovizTutar = -dto.Tutar,     // -negatif: efektif borç kapattı
            HasGrami = 0m,
            OdemeTuru = "NAKIT",
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        masak.Kaydet(db, masakGerek, hareketId, id, tlKarsiligi, dto.Masak, islem, now, MasakKaynakTuru.Hareket);
        TaksitDogrulanamadiIzi(taksit, islem, hareketId, tlKarsiligi);   // PAKET YASAL-2 (K-A)

        // ⚠ KASAYA EFEKTİF YAZILIR (M84): USD kasasının bakiyesi USD'dir.
        //   TL karşılığını yazmak kasa bakiyesini para birimi karışığı yapardı.
        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye = Math.Round(kasa.Bakiye + dto.Tutar, 2, MidpointRounding.AwayFromZero);
        kasa.Guncellendi = now;
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id          = Kimlik.Ulid(),
            TenantId    = islem.TenantId,
            KasaId      = kasa.Id,
            HareketId   = hareketId,
            Tutar       = Math.Round(dto.Tutar, 2, MidpointRounding.AwayFromZero),
            ParaBirimi  = kasa.ParaBirimi,
            OdemeTuru   = "DOVIZ",
            Tarih       = now,
            Olusturuldu = now,
        });

        await BakiyeGuncelle(id, islem.TenantId, 0m, 0m, now, paraBirimi, -dto.Tutar);
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Döviz tahsilat: cari={CariId} {ParaBirimi}={Tutar} kasa={KasaId}",
            islem.TenantId, id, paraBirimi, dto.Tutar, kasa.Id);

        return await Getir(id, islem);
    }

    // ─── Ekstre ───────────────────────────────────────────────────────────────
    //
    // KESIF-P2 (#8) — SATIR BAŞINA CARİ ETKİSİ. Eski yanıt satırın DEFTER has'ını
    // (hareket.hasGrami: stoktan çıkan altın, emanet girişi …) veriyordu; ekran onu
    // bakiye etkisi gibi basıyordu ve satırlar toplamı üstteki "Has Gram" bakiyesini
    // vermiyordu (keşif: −250 / +114,958 / −13,9232 ↔ bakiye −114,958). Artık her satır
    // `cariHasEtkisi` taşır: cari_bakiyeler.has_gram_bakiye'yi O SATIRIN yazdığı delta.
    // Kural, bakiyeyi yazan kodun AYNASIDIR (tek yerde, CariHasEtkisi):
    //   • VERESIYE · TAHSILAT · CARI_DUZELTME · ACILIS (cari) → −hasGrami (K4 sözleşmesi)
    //   • EMANET_GIRIS · EMANET_CIKIS → −hasGrami YALNIZ ALTIN (K20-2: EmanetModulu
    //     altın dışı emaneti bakiyeye yazmaz); storno satırı da AYNI kural (CIFT-BACAK:
    //     tek kaynak EmanetKurali — yazan, tersleyen ve ayna aynı fonksiyonu çağırır)
    //   • geri kalan her tür (SATIS, ALIS, iade, evrak, döviz …) → 0: has bakiyesine dokunmaz.
    // Sayfa dışı kalan satırların (limit/offset/tarih) toplam etkisi `sayfaDisiHasEtkisi`
    // ile söylenir: ekranda satırlar + sayfa dışı = bakiye (±0,0001), sessiz fark yok.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>KESIF-P2 (#8): hareketin cari has bakiyesine yazdığı delta (bakiyeyi yazan kodun aynası).</summary>
    /// <remarks>PAKET CARI-BAKIYE: kural tek yerde (CariBakiyeKurali.HasEtkisi); bu ad testler ve ekstre için korunur.</remarks>
    public static decimal CariHasEtkisi(string tur, decimal hasGrami, string? maden, bool storno) =>
        CariBakiyeKurali.HasEtkisi(tur, hasGrami, maden);

    /// <summary>KESIF-P2 (#8): satır has bakiyesine giren türde mi (emanet dışı madende hayır).</summary>
    public static bool HasBakiyesineGirer(string tur, string? maden, bool storno) => tur switch
    {
        "VERESIYE" or "TAHSILAT" or "CARI_DUZELTME" or "ACILIS" => true,
        "EMANET_GIRIS" or "EMANET_CIKIS" => Modules.Emanet.EmanetKurali.CariHasKolonunaGirer(maden),   // CIFT-BACAK
        _ => false,
    };

    /// <summary>
    /// KESIF-P2 tur 3 (ekip kararı 3): satırın EKRANDAKİ bakiye etkisi. Emanet (müşterinin bıraktığı
    /// maden) bakiyeye girmez → 0; kayıtlı bakiyedeki emanet payı ayrı `emanetteHas` ile söylenir.
    /// Kayıtlı bakiyenin TANIMI değişmez (CariHasEtkisi aynası aynen): kayıtlı = emanetsiz + emanet etkisi.
    /// </summary>
    public static decimal BakiyeHasEtkisi(string tur, decimal hasGrami, string? maden, bool storno) =>
        EmanetTuru(tur) ? 0m : CariHasEtkisi(tur, hasGrami, maden, storno);

    /// <summary>KESIF-P2 tur 3: emanet hareketi mi (EMANET_GIRIS · EMANET_CIKIS).</summary>
    public static bool EmanetTuru(string tur) => tur is "EMANET_GIRIS" or "EMANET_CIKIS";

    public async Task<object> Ekstre(
        string id, AktifKullanici islem,
        DateTime? baslangic, DateTime? bitis, int limit, int offset)
    {
        limit = Math.Clamp(limit, 1, 1000);   // UX-S29: sunucu sınırı; aşan istek kırpılır ve dahaFazlaVar söyler
        await TenantCarisiBul(id, islem.TenantId);

        var sorgu = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.CariId == id);
        if (baslangic is not null) sorgu = sorgu.Where(h => h.Tarih >= baslangic);
        if (bitis is not null) sorgu = sorgu.Where(h => h.Tarih <= bitis);

        var liste = await sorgu.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit + 1).ToListAsync();
        // UX-S29: sınıra değdiyse söylenir (sessiz kırpma yok).
        var dahaFazlaVar = liste.Count > limit;
        if (dahaFazlaVar) liste.RemoveAt(liste.Count - 1);

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == id && b.TenantId == islem.TenantId);

        // KESIF-P2 tur 2 (#8, hakem) + PAKET CARI-BAKIYE: carinin TÜM hareketlerinin (sayfa/tarih
        // süzgecinden bağımsız) kural aynası — has VE TL — TEK fonksiyondan (CariBakiyeKurali.Hesapla).
        // Uyuşmazlık "eski işlem" diye açıklanmaz; günlüğe yazılır ve ekranda uyarı olarak görünür.
        // Ekip kararı 3: emanet bakiyeye girmez → üstteki bakiye = kayıtlı − emanet etkisi.
        var ayna = await CariBakiyeKurali.Hesapla(db, islem.TenantId, id);
        var bakiyeHas = ayna.KayitliHas;
        var tumEtki = ayna.HasAyna;
        var emanetEtkisi = ayna.EmanetEtkisi;
        var tumEtkiEmanetsiz = tumEtki - emanetEtkisi;
        var uyusmazlik = ayna.HasUyusmazligi;
        if (Math.Abs(uyusmazlik) > CariBakiyeKurali.HasEsik)
            logger.LogWarning(
                "KESIF-P2 #8: cari {CariId} (kiracı {TenantId}) kayıtlı has bakiyesi {Bakiye} işlemlerin toplamı {Toplam} ile uyuşmuyor; fark {Fark}",
                id, islem.TenantId, bakiyeHas, tumEtki, uyusmazlik);
        if (ayna.TlUyusmazligi is { } tlFark && Math.Abs(tlFark) > CariBakiyeKurali.TlEsik)
            logger.LogWarning(
                "CARI-BAKIYE: cari {CariId} (kiracı {TenantId}) kayıtlı TL bakiyesi {Bakiye} işlemlerin TL toplamı {Toplam} ile uyuşmuyor; fark {Fark}",
                id, islem.TenantId, ayna.KayitliTl, ayna.TlAyna, tlFark);

        // PARA-DEMO-2 (madde 1): gerçek karma fişte ekstre "Karma ödeme" demez, BACAK TÜRLERİNİ yazar
        // ("Kart + Havale"). Kaynak odeme_kalemleri (fişin bacakları, sıra korunur); bilinmeyen
        // (geri doldurulmuş "BILINMIYOR") kalem araç sayılmaz — uydurulmaz.
        var karmaFisler = liste.Where(h => h.OdemeTuru == "KARMA").Select(h => h.FisId ?? h.Id)
            .Distinct(StringComparer.Ordinal).ToList();
        var karmaAraclari = karmaFisler.Count == 0
            ? new Dictionary<string, List<string>>(StringComparer.Ordinal)
            : (await db.OdemeKalemleri.AsNoTracking()
                    .Where(k => k.TenantId == islem.TenantId && karmaFisler.Contains(k.FisId) && !k.Bilinmiyor)
                    .Select(k => new { k.FisId, k.Arac, k.Sira })
                    .ToListAsync())
                .GroupBy(k => k.FisId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Sira).Select(x => x.Arac).Distinct(StringComparer.Ordinal).ToList(),
                    StringComparer.Ordinal);

        var satirlar = liste.Select(h =>
        {
            var storno = h.StornoKaynakId is not null;
            return new
            {
                id = h.Id,
                tur = h.Tur,
                tarih = h.Tarih,
                aciklama = h.Aciklama,
                tlTutari = h.TlTutari,
                paraBirimi = h.ParaBirimi,
                dovizTutar = h.DovizTutar,
                hasGrami = h.HasGrami,
                maden = h.Maden,
                odemeTuru = h.OdemeTuru,
                // PARA-DEMO-2: karma satırın bacak araçları (yalnız KARMA satırda dolu; diğerinde null).
                odemeAraclari = h.OdemeTuru == "KARMA" && karmaAraclari.TryGetValue(h.FisId ?? h.Id, out var ka) ? ka : null,
                nakitTutar = h.NakitTutar,
                veresiyeTutar = h.VeresiyeTutar,
                fisId = h.FisId,
                iadeKaynakHareketId = h.IadeKaynakHareketId,
                stornoKaynakId = h.StornoKaynakId,
                vadeTarihi = h.VadeTarihi,
                subeId = h.SubeId,
                olusturuldu = h.Olusturuldu,
                // KESIF-P2 (#8)
                cariHasEtkisi = CariHasEtkisi(h.Tur, h.HasGrami, h.Maden, storno),
                // Tur 3 (karar 3): ekrandaki bakiye etkisi — emanet satırı 0, "girmez".
                bakiyeHasEtkisi = BakiyeHasEtkisi(h.Tur, h.HasGrami, h.Maden, storno),
                hasBakiyesineGirer = !EmanetTuru(h.Tur) && HasBakiyesineGirer(h.Tur, h.Maden, storno),
                emanet = EmanetTuru(h.Tur),
                // Emanet satırının kayıtlı bakiyedeki payı, "bizde duran" yönünde (giriş artı).
                emanetHas = EmanetTuru(h.Tur) ? -CariHasEtkisi(h.Tur, h.HasGrami, h.Maden, storno) : 0m,
                // PAKET CARI-BAKIYE: satırın cari TL bakiyesine etkisi (TL aynasının satır yüzü; null = bilinmiyor).
                cariTlEtkisi = CariBakiyeKurali.TlEtkisi(h.Tur, h.TlTutari, h.VeresiyeTutar, storno),
            };
        }).ToList();

        return new
        {
            bakiye = bakiye is not null ? BakiyeFormat(bakiye, emanetEtkisi) : BosBakiye(id),
            hareketler = satirlar,
            // Satırlar + sayfa dışı + uyuşmazlık = bakiye. Tüm hareketler sayfadaysa sayfa dışı 0'dır
            // (CariEkstre_satir_cari_has_toplami_bakiyeye_esit bunu her cari için sınar);
            // uyuşmazlık yalnız kayıtlı bakiye işlemlerle tutmuyorsa ≠ 0 (CariEkstre_bakiye_uyusmazligi_ayri_soylenir).
            // Tur 3 (karar 3): satırların bakiye etkisi + sayfa dışı + uyuşmazlık = bakiyeEmanetsizHas.
            sayfaDisiHasEtkisi = tumEtkiEmanetsiz - satirlar.Sum(s => s.bakiyeHasEtkisi),
            bakiyeUyusmazligiHas = uyusmazlik,
            bakiyeEmanetsizHas = bakiyeHas - emanetEtkisi,
            emanetteHas = -emanetEtkisi,
            // PAKET CARI-BAKIYE (TL aynası): kayıtlı TL − kural toplamı. null = ölçülemez; sebebi
            // tlAynaOlculemez (VERESIYE_PAYI_YOK: Faz 0 öncesi satır · KONSINYE_SAHIBI: borcun defter satırı yok).
            bakiyeUyusmazligiTl = ayna.TlUyusmazligi,
            tlAynaOlculemez = ayna.TlOlculemezSebep,
            dahaFazlaVar,
            limit,
        };
    }

    /// <summary>
    /// KESIF-P2 (#22) — uyruk tek standart: ISO 3166-1 ALPHA-3 (+ pasaport ICAO kodları),
    /// MASAK formunun tek kaynağı <see cref="Kuyumcu.Core.I18n.UlkeListesi.KodGecerli"/>.
    /// Boş → null. Gönderilen değer kayıttaki eski (eşlenemeyen) değerle AYNIYSA dokunulmaz:
    /// değer uydurulmaz, kart başka alanlar için kaydedilebilir (ekip kararı 4).
    /// </summary>
    public static string? UyrukNormalle(string? deger, string? mevcut)
    {
        if (string.IsNullOrWhiteSpace(deger)) return null;
        var v = deger.Trim().ToUpperInvariant();
        if (mevcut is not null && v == mevcut.Trim().ToUpperInvariant()) return mevcut;
        if (v.Length != 3 || !Kuyumcu.Core.I18n.UlkeListesi.KodGecerli(v))
            throw new BadRequestException(
                $"Uyruk kodu geçersiz ('{v}'): pasaporttaki 3 harfli ülke kodunu yazın (örnek: TUR, DEU). "
                + "İki harfli eski kodlar (TR, DE) artık kabul edilmez.");
        return v;
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<Data.Cari> TenantCarisiBul(string id, string tenantId) =>
        await db.Cariler.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
        ?? throw new NotFoundException("Cari bulunamadı");

    /// <summary>
    /// Bakiye satırını delta ile günceller — çağıranın transaction'ı içinde çalışır.
    /// </summary>
    /// <param name="dovizParaBirimi">
    /// Verilirse üçüncü paralel defter (döviz) de güncellenir. TL/has imzası
    /// BOZULMADI: mevcut çağıranlar bu iki parametreyi hiç görmez.
    /// </param>
    private async Task BakiyeGuncelle(
        string cariId, string tenantId, decimal tlDelta, decimal hasGramDelta, DateTime now,
        string? dovizParaBirimi = null, decimal dovizDelta = 0m)
    {
        // PAKET CARI-BAKIYE: TEK YAZICI (Common/CariBakiyeServisi.cs). Döviz kolonu yine yalnız
        // DovizBakiye.Ekle'den (yazıcı onu çağırır).
        await CariBakiyeYazici.Uygula(db, tenantId, cariId, tlDelta, hasGramDelta, now,
            dovizParaBirimi, dovizDelta);
    }

    /// <summary>
    /// Bakiye yanıtının TEK biçimleyicisi — Getir · BakiyeGetir · Ekstre
    /// üçü de buradan geçer, böylece yeni bir bakiye alanı bir uçta unutulamaz.
    /// ⚠ TL · has · döviz ASLA tek sayıya TOPLANMAZ (K27 toplama yasağı).
    /// </summary>
    /// <remarks>
    /// PAKET CARI-BAKIYE (tek tanım): <c>hasGramBakiye</c> EMANETSİZ bakiyedir (liste, detay, ekstre,
    /// bakiye ucu aynı değeri verir). Kayıtlı kolon (emanet dahil, K4) <c>kayitliHasGramBakiye</c>,
    /// emanet payı <c>emanetteHas</c> (bizde duran, artı) olarak ayrıca döner.
    /// </remarks>
    private static object BakiyeFormat(CariBakiye b, decimal emanetEtkisi) => new
    {
        cariId = b.CariId,
        tlBakiye = b.TlBakiye,
        hasGramBakiye = CariBakiyeKurali.EmanetsizHas(b.HasGramBakiye, emanetEtkisi),
        emanetteHas = -emanetEtkisi,
        kayitliHasGramBakiye = b.HasGramBakiye,
        usdBakiye = b.UsdBakiye,
        eurBakiye = b.EurBakiye,
        gbpBakiye = b.GbpBakiye,
        sonIslemTarihi = b.SonIslemTarihi,
    };

    /// <summary>Bakiye satırı olmayan cari — BakiyeFormat ile AYNI alan kümesi.</summary>
    private static object BosBakiye(string cariId) => new
    {
        cariId, tlBakiye = 0m, hasGramBakiye = 0m, emanetteHas = 0m, kayitliHasGramBakiye = 0m,
        usdBakiye = 0m, eurBakiye = 0m, gbpBakiye = 0m, sonIslemTarihi = (DateTime?)null,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/cari")]
[Yetki.ModulYetkisi("cari")]
public sealed class CariController(CariServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/cari — Yeni cari (müşteri/tedarikçi/atölye/sarraf) oluştur — MUHASEBE+</summary>
    /// <remarks>Nest'te @HttpCode override'ı yok: POST varsayılanı 201 Created döner.</remarks>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Olustur([FromBody] CariOlusturDto dto)
        => StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>GET /api/v1/cari?tur=&amp;aktif=&amp;q=&amp;limit=&amp;offset=&amp;sirala=&amp;yon= — Cari listesi (bakiyeler dahil).
    /// PAKET GRID-1: sirala ∈ {ad, tur, telefon, tl-bakiye}, yon ∈ {artan, azalan}; bilinmeyen → 400.</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? tur, [FromQuery] bool? aktif, [FromQuery] string? q,
        [FromQuery] int limit = 50, [FromQuery] int offset = 0,
        [FromQuery] string? sirala = null, [FromQuery] string? yon = null)
        => servis.Listele(Islem, tur, aktif, q, limit, offset, sirala, yon);

    /// <summary>
    /// GET /api/v1/cari/sistem-carisi — "Peşin Müşteri" kartı (yoksa tohumlanır).
    /// PAKET B-3 (D2). Satış ekranı ön seçim için okur; cari listesiyle AYNI rol
    /// yüzeyi (tezgâhtar satış yapar). Sabit segment "{id}"den önceliklidir.
    /// </summary>
    [HttpGet("sistem-carisi")]
    public Task<Kuyumcu.Contracts.Sayfalar.PesinMusteri.PesinMusteriKarti> SistemCarisiGetir()
        => servis.SistemCarisiGetir(Islem);

    /// <summary>GET /api/v1/cari/{id} — Cari detayı + anlık bakiye (okuma izi düşer)</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.GetirIzli(id, Islem);

    /// <summary>GET /api/v1/cari/{id}/bakiye — Sadece bakiye bilgisi (TL + has gram paralel)</summary>
    [HttpGet("{id}/bakiye")]
    public Task<object> BakiyeGetir(string id) => servis.BakiyeGetir(id, Islem);

    /// <summary>GET /api/v1/cari/{id}/ekstre — Hareket listesi (ekstre) — tarih filtrelemeli</summary>
    [HttpGet("{id}/ekstre")]
    public Task<object> Ekstre(
        string id,
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.Ekstre(id, Islem, baslangic, bitis, limit, offset);

    /// <summary>PATCH /api/v1/cari/{id} — Cari bilgilerini güncelle — MUHASEBE+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Guncelle(string id, [FromBody] CariGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>
    /// PATCH /api/v1/cari/{id}/pep — PEP BEYANI (bool + görev). Tarih sunucuda
    /// damgalanır. Yalnız pep_* kolonlarına dokunur — MUHASEBE+
    /// </summary>
    [HttpPatch("{id}/pep")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> PepGuncelle(string id, [FromBody] CariPepDto dto)
        => servis.PepGuncelle(id, dto, Islem);

    /// <summary>POST /api/v1/cari/{id}/veresiye — Veresiye girişi — TEZGAHTAR+</summary>
    [HttpPost("{id}/veresiye")]
    public Task<object> VeresiyeGir(string id, [FromBody] VeresiyeGirisDto dto)
        => servis.VeresiyeGir(id, dto, Islem);

    /// <summary>POST /api/v1/cari/{id}/tahsilat — Tahsilat girişi — TEZGAHTAR+</summary>
    [HttpPost("{id}/tahsilat")]
    public Task<object> TahsilatGir(string id, [FromBody] TahsilatGirisDto dto)
        => servis.TahsilatGir(id, dto, Islem);

    /// <summary>
    /// GET /api/v1/cari/kasasiz-tahsilatlar?esik=&amp;limit= — kasa bacağı
    /// zorunlu olmadan önce kaydedilmiş TL tahsilatlar — MUHASEBE+.
    /// <remarks>
    /// Sabit segment ("kasasiz-tahsilatlar") ASP.NET Core yönlendirmesinde
    /// parametreli segmentten ("{id}") daha yüksek önceliklidir; bildirim
    /// sırasından bağımsız olarak bu uç kazanır.
    /// </remarks>
    /// </summary>
    [HttpGet("kasasiz-tahsilatlar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> KasasizTahsilatlar([FromQuery] DateTime? esik, [FromQuery] int limit = 200)
        => servis.KasasizTahsilatlar(Islem, esik, limit);

    /// <summary>
    /// POST /api/v1/cari/{id}/doviz-veresiye — Döviz cinsinden borç — MUHASEBE+.
    /// <remarks>
    /// ⚠ TEZGÂHTAR'a KAPALI ve bu bilinçli bir SIKILAŞTIRMADIR: 2008-32/34
    /// Tebliği bedelin dövizle KARARLAŞTIRILMASINI ve TAHSİLİNİ serbest bırakır
    /// (doğrulandı, A12); alacağın döviz cinsinden AÇIK bırakılmasına dair
    /// birincil metin teyidi ise YOKTUR. Hüküm koda gömülmedi — yalnız yetki
    /// kısıtı olarak uygulandı (bkz. rapor § kurucuya/mali müşavire notlar).
    /// </remarks>
    /// </summary>
    [HttpPost("{id}/doviz-veresiye")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> DovizVeresiyeGir(string id, [FromBody] DovizVeresiyeDto dto)
        => servis.DovizVeresiyeGir(id, dto, Islem);

    /// <summary>
    /// POST /api/v1/cari/{id}/doviz-tahsilat — Efektif dövizle tahsilat — TEZGAHTAR+.
    /// <remarks>Mevcut tahsilat ucuyla AYNI rol yüzeyi: tezgâhtar para tahsil eder.</remarks>
    /// </summary>
    [HttpPost("{id}/doviz-tahsilat")]
    public Task<object> DovizTahsilatGir(string id, [FromBody] DovizTahsilatDto dto)
        => servis.DovizTahsilatGir(id, dto, Islem);

    /// <summary>POST /api/v1/cari/{id}/duzeltme — Manuel düzeltme fişi — MUHASEBE+</summary>
    /// <remarks>
    /// Elle bakiye müdahalesi: TEZGÂHTAR'a kapalıdır. Ekranda düğme de rolle
    /// gizlenir, ama son sözü BU kapı söyler — ekran gizlemesi kapı sayılmaz.
    /// </remarks>
    [HttpPost("{id}/duzeltme")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Duzeltme(string id, [FromBody] CariDuzeltmeDto dto)
        => servis.Duzeltme(id, dto, Islem);

    /// <summary>
    /// GET /api/v1/cari/{id}/bagli-kayitlar — pasife alma onayında gösterilen
    /// bağlı kayıt sayıları (PAKET P-13 · B-39 · UX-S23) — MUDUR+
    /// </summary>
    [HttpGet("{id}/bagli-kayitlar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> BagliKayitlar(string id) => servis.BagliKayitlariGetir(id, Islem);

    /// <summary>DELETE /api/v1/cari/{id} — Cariyi deaktif et (soft delete) — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
