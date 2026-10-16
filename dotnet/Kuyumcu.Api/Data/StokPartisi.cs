using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// STOK MALİYET PARTİSİ (lot) — kâr-zararın tek dayanağı
//
// NEDEN VAR: Bugüne kadar kod tabanında HİÇBİR maliyet alanı yoktu.
// `Urun` satış fiyatını ve işçiliği taşıyor ama ALIŞ MALİYETİNİ taşımıyor;
// `Stok` yalnız adet ve toplam has gram tutuyor. Bu yüzden "satılan malın
// maliyeti" ve dolayısıyla kâr-zarar HESAPLANAMIYORDU — tahmin edilebilirdi,
// ama tahmin bu projede rakam sayılmaz (Anayasa md.1).
//
// ⚠ KUYUMCULUĞUN ÖZÜ: KÂR İKİ BACAKLIDIR.
// Bir kuyumcu TL kârı yazarken has gram KAYBEDEBİLİR. Altın yükselirken
// sattığı malın yerine aynı gramı koyamaz. Bu yüzden her parti iki maliyet
// taşır:
//   • BirimMaliyetTl  — muhasebenin gördüğü maliyet
//   • BirimMaliyetHas — o TL'nin ALIŞ GÜNÜNDEKİ has gram karşılığı
// Kâr da iki bacaklı hesaplanır. Yalnız TL kârı göstermek, kuyumcuya
// kazandığını sanırken gram kaybettirir.
//
// ⚠ MALİYETİ BİLİNMEYEN STOK UYDURULMAZ. Bu özellik gelmeden önce girmiş
// stoklar için açılış (DEVIR) partisi oluşturulur ve maliyeti NULL bırakılır.
// Rapor bunları "maliyeti bilinmeyen" olarak AYRI sayar; sıfır maliyet
// varsaymak kârı olduğundan yüksek gösterirdi.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Parti kaynağı — stoğa nasıl girdi.</summary>
public static class PartiKaynagi
{
    /// <summary>Tedarikçiden alış — maliyet bilinir.</summary>
    public const string Alis = "ALIS";

    /// <summary>Şubeler arası transfer — maliyet kaynak partiden TAŞINIR, yeniden hesaplanmaz.</summary>
    public const string Transfer = "TRANSFER";

    /// <summary>Sayımda fazla çıkan — maliyeti BİLİNMEZ.</summary>
    public const string SayimFazla = "SAYIM_FAZLA";

    /// <summary>Bu özellik gelmeden önce var olan stok — maliyeti BİLİNMEZ.</summary>
    public const string Devir = "DEVIR";

    /// <summary>İade ile geri gelen mal — maliyet orijinal satışın tüketiminden geri alınır.</summary>
    public const string Iade = "IADE";

    public static readonly string[] Hepsi = [Alis, Transfer, SayimFazla, Devir, Iade];

    /// <summary>Bu kaynakta maliyet bilinir mi?</summary>
    public static bool MaliyetBilinir(string kaynak) =>
        kaynak is Alis or Transfer or Iade;
}

/// <summary>Satılan malın maliyeti hangi sırayla düşülür.</summary>
public static class MaliyetYontemi
{
    /// <summary>İlk giren ilk çıkar — varsayılan. Kuyumcuda en yaygın ve VUK'a en uygun olanı.</summary>
    public const string Fifo = "FIFO";

    /// <summary>Son giren ilk çıkar.</summary>
    public const string Lifo = "LIFO";

    /// <summary>Ağırlıklı ortalama — tüm açık partilerin ortalaması.</summary>
    public const string Ortalama = "ORTALAMA";

    public static readonly string[] Hepsi = [Fifo, Lifo, Ortalama];

    public static bool Gecerli(string? y) => y is not null && Hepsi.Contains(y);
}

/// <summary>
/// Stoğa giren bir partinin maliyet kaydı. Parti tüketildikçe
/// <see cref="AdetKalan"/> azalır; sıfırlandığında satır SİLİNMEZ
/// (geçmiş maliyet kanıtı olarak durur).
/// </summary>
[Table("stok_partileri")]
public class StokPartisi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;

    /// <summary>Partiyi doğuran hareket. DEVIR ve SAYIM_FAZLA'da boş olabilir.</summary>
    [Column("giris_hareket_id"), MaxLength(36)] public string? GirisHareketId { get; set; }

    [Column("kaynak"), MaxLength(20)] public string Kaynak { get; set; } = null!;

    [Column("adet_giren", TypeName = "decimal(12,3)")] public decimal AdetGiren { get; set; }

    /// <summary>Henüz tüketilmemiş adet. FIFO/LIFO sırası bunun üzerinden yürür.</summary>
    [Column("adet_kalan", TypeName = "decimal(12,3)")] public decimal AdetKalan { get; set; }

    /// <summary>Birim başına has gram — ürün kartı sonradan değişse bile parti kendi anını taşır.</summary>
    [Column("has_gram_birim", TypeName = "decimal(15,6)")] public decimal HasGramBirim { get; set; }

    /// <summary>Birim alış maliyeti (TL). <b>NULL = maliyet bilinmiyor</b>, sıfır DEĞİL.</summary>
    [Column("birim_maliyet_tl", TypeName = "decimal(15,4)")] public decimal? BirimMaliyetTl { get; set; }

    /// <summary>
    /// Birim maliyetin ALIŞ ANINDAKİ has gram karşılığı = <c>BirimMaliyetTl / AltinHasFiyati</c>.
    /// Has bacaklı kâr bunun üzerinden hesaplanır. <b>NULL = bilinmiyor.</b>
    /// </summary>
    [Column("birim_maliyet_has", TypeName = "decimal(15,6)")] public decimal? BirimMaliyetHas { get; set; }

    /// <summary>Maliyetin has karşılığını üreten kur — sonradan denetlenebilsin diye saklanır.</summary>
    [Column("kur_snapshot_id"), MaxLength(36)] public string? KurSnapshotId { get; set; }

    [Column("giris_tarihi")] public DateTime GirisTarihi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>Maliyeti bilinen bir parti mi? Rapor bunu ayırmak zorunda.</summary>
    [NotMapped] public bool MaliyetBilinir => BirimMaliyetTl is not null;
}

/// <summary>
/// Bir çıkış hareketinin hangi partiden ne kadar tükettiği. Satılan malın
/// maliyeti (SMM) bu satırlardan toplanır.
///
/// ⚠ Maliyet partiden KOPYALANIR, referansla okunmaz: parti sonradan
/// düzeltilse bile geçmiş satışın maliyeti değişmez (append-only defter ilkesi).
/// </summary>
[Table("stok_parti_tuketimleri")]
public class StokPartiTuketimi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("parti_id"), MaxLength(36)] public string PartiId { get; set; } = null!;

    /// <summary>Tüketen hareket (satış, takas, transfer çıkışı, sayım eksiği).</summary>
    [Column("cikis_hareket_id"), MaxLength(36)] public string? CikisHareketId { get; set; }

    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }

    /// <summary>Partiden kopyalanan birim maliyet. NULL = o parti maliyetsizdi.</summary>
    [Column("birim_maliyet_tl", TypeName = "decimal(15,4)")] public decimal? BirimMaliyetTl { get; set; }
    [Column("birim_maliyet_has", TypeName = "decimal(15,6)")] public decimal? BirimMaliyetHas { get; set; }

    [Column("tarih")] public DateTime Tarih { get; set; }

    /// <summary>Bu tüketimin toplam TL maliyeti. Maliyet bilinmiyorsa NULL.</summary>
    [NotMapped] public decimal? ToplamMaliyetTl => BirimMaliyetTl * Adet;

    /// <summary>Bu tüketimin toplam has gram maliyeti. Maliyet bilinmiyorsa NULL.</summary>
    [NotMapped] public decimal? ToplamMaliyetHas => BirimMaliyetHas * Adet;
}
