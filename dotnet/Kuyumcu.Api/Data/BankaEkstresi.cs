using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// BANKA EKSTRESİ İÇE AKTARMA — paket 32 Faz A (CSV, sahne tablolu, insan onaylı)
//
// ⚠ NEDEN SAHNE (staging) TABLOSU VAR:
//   Yüklenen ekstrenin satırları banka_hareketleri'ne DOĞRUDAN yazılsaydı,
//   150-400 satırlık bir dosya tek istekte defteri şişirir ve MASAK kümülatifi
//   (paket 41) insan onayı görmeden tetiklenirdi. Bu yüzden yükleme HİÇBİR
//   banka hareketi üretmez: satırlar burada bekler, kullanıcı tek tek/toplu
//   seçer ve YALNIZ POST /onayla üretim yapar.
//
// ⚠ TEK YAZMA YOLU (K4-OK-2): onay, BankaServisi.HareketEkleIc'i çağırır.
//   Bu sınıfların HİÇBİRİ banka_hareketleri'ne Add yapmaz; denetim.sh
//   "BankaHareketleri.Add" sayacı bunu CI'da zorlar.
//
// ⚠ KİŞİSEL VERİ (Mehmet AS-2 kararı, ADR-0003):
//   • CariIban.Iban ve EkstreSatiri.KarsiIban DİSKE ŞİFRELİ iner (AES-GCM,
//     KuyumcuDbContext HasConversion). Aranabilirlik HMAC hash kolonundan
//     gelir (MasakHashDinleyicisi deseni) — şifreli sütun ARANMAZ.
//   • EkstreSatiri.HamSatir ham dosyanın satırıdır ve IBAN/ad taşır; o da
//     diske ŞİFRELİ iner.
//   • Mevcut BankaHareket.KarsiIban DÜZ kalır — geriye dönük şifreleme bu
//     pakette YAPILMADI (teknik borç TB-32-2).
//
// SAKLANMAYAN: ham CSV DOSYASI diske yazılmaz. sha256 mükerrer dosya tespitine
// yeter; satırların kendisi zaten ekstre_satirlari'nda durur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ekstre dosyası formatı — kapalı liste.</summary>
public static class EkstreFormati
{
    /// <summary>Faz A: şablonunu BİZ yayımlıyoruz, lehçe riski yok.</summary>
    public const string Csv = "CSV";

    /// <summary>
    /// Faz B. ⚠ Bu pakette AYRIŞTIRICI YAZILMADI: elimizde gerçek banka MT940
    /// çıktısı yok ve "kamuya açık dokümana göre yazıldı" bir kanıt boşluğudur
    /// (Anayasa md.1). Uç bu değeri 400 ile reddeder — sessizce CSV gibi
    /// okumaz.
    /// </summary>
    public const string Mt940 = "MT940";

    public static readonly string[] Tumu = [Csv, Mt940];
}

/// <summary>İthalat oturumunun yaşam döngüsü.</summary>
public static class EkstreIthalatDurumu
{
    /// <summary>Satırlar sahnede bekliyor; banka_hareketleri'ne SIFIR satır yazıldı.</summary>
    public const string Prova = "PROVA";

    /// <summary>En az bir satır onaylandı ve banka hareketine dönüştü.</summary>
    public const string Onaylandi = "ONAYLANDI";

    /// <summary>Kullanıcı vazgeçti. ⚠ SİLME DEĞİLDİR: satırlar geçmişte kalır.</summary>
    public const string Iptal = "IPTAL";

    public static readonly string[] Tumu = [Prova, Onaylandi, Iptal];
}

/// <summary>Sahne satırının durumu.</summary>
public static class EkstreSatirDurumu
{
    /// <summary>Ayrıştı, mükerrer değil, onaya hazır.</summary>
    public const string Yeni = "YENI";

    /// <summary>
    /// Aynı dedupe anahtarı banka_hareketleri'nde ZATEN VAR. Satır silinmez,
    /// varsayılan olarak seçilmez; kullanıcı "yine de al" diyebilir (fail-closed
    /// ama çıkmaz sokak değil — aynı gün aynı tutarda iki meşru havale olabilir).
    /// </summary>
    public const string Mukerrer = "MUKERRER";

    /// <summary>Onaylandı; banka hareketi üretildi. ⚠ DEĞİŞMEZ (PATCH 409).</summary>
    public const string Onaylandi = "ONAYLANDI";

    /// <summary>Kullanıcı bu satırı almak istemedi. Geri alınabilir (PATCH durum=YENI).</summary>
    public const string Atlandi = "ATLANDI";

    /// <summary>
    /// Çözülemedi (bozuk tarih, tutar yok, gelecek tarih...). ⚠ ATLANMAZ:
    /// "sessizce eksik içe aktarım" bu projedeki en pahalı hata sınıfıdır.
    /// </summary>
    public const string Hatali = "HATALI";

    public static readonly string[] Tumu = [Yeni, Mukerrer, Onaylandi, Atlandi, Hatali];
}

/// <summary>Sahne satırının cariye nasıl bağlandığı.</summary>
public static class EslesmeKaynagi
{
    /// <summary>IBAN hash'i cari_ibanlari'nda TAM eşleşti — OTOMATİK bağın TEK yolu.</summary>
    public const string Iban = "IBAN";

    /// <summary>Kullanıcı elle bağladı. Ünvan benzerliği ASLA otomatik bağlamaz (K4-OK-5).</summary>
    public const string Elle = "ELLE";
}

/// <summary>Bir cari-IBAN bağının nereden geldiği.</summary>
public static class IbanBagKaynagi
{
    /// <summary>Kullanıcı cari kartından/ekstre ekranından açıkça girdi.</summary>
    public const string Elle = "ELLE";

    /// <summary>Ekstre satırı elle bağlanırken ÖĞRENİLDİ (K4-OK-6).</summary>
    public const string Ogrenildi = "OGRENILDI";
}

/// <summary>Yüklenen bir ekstre dosyasının oturum başlığı.</summary>
[Table("ekstre_ithalatlari")]
public class EkstreIthalati : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_hesap_id"), MaxLength(36)] public string BankaHesapId { get; set; } = null!;
    [Column("dosya_adi"), MaxLength(255)] public string DosyaAdi { get; set; } = null!;
    [Column("format"), MaxLength(10)] public string Format { get; set; } = EkstreFormati.Csv;

    /// <summary>Dosyanın SHA-256'sı — "bu dosyayı zaten yüklemiştim" tespiti.</summary>
    [Column("sha256"), MaxLength(64)] public string Sha256 { get; set; } = null!;

    [Column("satir_sayisi")] public int SatirSayisi { get; set; }
    [Column("yeni_sayisi")] public int YeniSayisi { get; set; }
    [Column("mukerrer_sayisi")] public int MukerrerSayisi { get; set; }
    [Column("hatali_sayisi")] public int HataliSayisi { get; set; }
    [Column("onaylanan_sayisi")] public int OnaylananSayisi { get; set; }
    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = EkstreIthalatDurumu.Prova;

    /// <summary>MT940 :60F: — CSV'de daima null (Faz B).</summary>
    [Column("acilis_bakiye", TypeName = "decimal(15,2)")] public decimal? AcilisBakiye { get; set; }

    /// <summary>MT940 :62F: — CSV'de daima null (Faz B).</summary>
    [Column("kapanis_bakiye", TypeName = "decimal(15,2)")] public decimal? KapanisBakiye { get; set; }

    [Column("yukleyen_kullanici_id"), MaxLength(36)] public string YukleyenKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Ekstrenin TEK satırı — SAHNE kaydı (K4-OK-3). Banka hareketi DEĞİLDİR;
/// yalnız <see cref="BankaHareketId"/> doldurulduğunda deftere karşılığı doğar.
/// </summary>
[Table("ekstre_satirlari")]
public class EkstreSatiri : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ithalat_id"), MaxLength(36)] public string IthalatId { get; set; } = null!;

    /// <summary>Dosyadaki sıra — denetimde "kaçıncı satır" sorusunun cevabı.</summary>
    [Column("satir_no")] public int SatirNo { get; set; }

    /// <summary>
    /// Ham dosya satırı (500 karaktere kırpılır). ⚠ DİSKE ŞİFRELİ İNER
    /// (KuyumcuDbContext HasConversion): IBAN ve karşı taraf adı taşır.
    /// Kolon tipi bu yüzden TEXT'tir — şifreli metin düz metinden uzundur.
    /// </summary>
    [Column("ham_satir", TypeName = "text")] public string HamSatir { get; set; } = null!;

    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = null!;
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("karsi_taraf_ad"), MaxLength(150)] public string? KarsiTarafAd { get; set; }

    /// <summary>⚠ DİSKE ŞİFRELİ İNER (AS-2). Arama <see cref="KarsiIbanHash"/> üzerinden.</summary>
    [Column("karsi_iban"), MaxLength(255)] public string? KarsiIban { get; set; }

    /// <summary>HMAC — <see cref="MasakHashDinleyicisi"/> doldurur, servis elle yazmaz.</summary>
    [Column("karsi_iban_hash"), MaxLength(64)] public string? KarsiIbanHash { get; set; }

    [Column("banka_referans"), MaxLength(50)] public string? BankaReferans { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }

    /// <summary>SHA-256 mükerrer anahtarı — <see cref="Modules.Banka.DedupeAnahtari"/> üretir.</summary>
    [Column("dedupe_anahtar"), MaxLength(64)] public string DedupeAnahtar { get; set; } = null!;

    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = EkstreSatirDurumu.Yeni;
    [Column("hata_mesaji"), MaxLength(200)] public string? HataMesaji { get; set; }
    [Column("eslesen_cari_id"), MaxLength(36)] public string? EslesenCariId { get; set; }
    [Column("eslesme_kaynagi"), MaxLength(15)] public string? EslesmeKaynagi { get; set; }

    /// <summary>Onay sonrası üretilen banka hareketi — null ise deftere karşılığı YOK.</summary>
    [Column("banka_hareket_id"), MaxLength(36)] public string? BankaHareketId { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Cari ↔ IBAN bağı (öğrenen eşleştirme, K4-OK-6).
/// <para>
/// ⚠ NEDEN CARİ'YE KOLON DEĞİL: bir müşterinin birden çok hesabı olabilir ve
/// eşi/şirketi adına havale gelebilir. Tek kolon, ikinci IBAN görüldüğünde ya
/// birincisini EZER ya da eşleşmeyi kaybeder.
/// </para>
/// </summary>
[Table("cari_ibanlari")]
public class CariIban : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;

    /// <summary>⚠ DİSKE ŞİFRELİ İNER (AS-2). Benzersizlik ve arama hash üzerinden.</summary>
    [Column("iban"), MaxLength(255)] public string Iban { get; set; } = null!;

    /// <summary>
    /// HMAC — <see cref="MasakHashDinleyicisi"/> doldurur. (tenant_id, iban_hash)
    /// BENZERSİZDİR: bir IBAN iki cariye bağlanamaz, belirsiz eşleşme yapısal
    /// olarak imkânsızdır.
    /// </summary>
    [Column("iban_hash"), MaxLength(64)] public string IbanHash { get; set; } = null!;

    /// <summary>Ekstrede görülen ad — denetim için; eşleştirmede KULLANILMAZ.</summary>
    [Column("unvan"), MaxLength(150)] public string? Unvan { get; set; }

    [Column("kaynak"), MaxLength(15)] public string Kaynak { get; set; } = IbanBagKaynagi.Elle;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
