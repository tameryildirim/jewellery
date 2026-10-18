using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D1-06 (18.10) — KUYUMCUNUN KENDİ TAKSİTLİ SATIŞI: veresiye satış borcu taksitlere bölünür. Defter DEĞİŞMEZ
/// (borç cari defterinde tek satır kalır; tahsilatlar FIFO düşer). Taksit durumu o borcun açık tutarından sırayla
/// hesaplanır. Senetli planda her taksit için ALINAN SENET açılır (borç senede geçer); durum senetten okunur.
/// Vade farkı EKLENMEZ (vade farkı KDV'si mali müşavir sorusu — uydurulmaz).
/// </summary>
[Table("odeme_planlari")]
public class OdemePlani : IKiraciVarlik
{
    public const string Aktif = "AKTIF";
    public const string Iptal = "IPTAL";

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    /// <summary>Taksitlendirilen veresiye satış hareketi.</summary>
    [Column("kaynak_hareket_id"), MaxLength(36)] public string KaynakHareketId { get; set; } = null!;
    [Column("toplam_tl", TypeName = "decimal(15,2)")] public decimal ToplamTl { get; set; }
    [Column("senetli")] public bool Senetli { get; set; }
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = Aktif;
    [Column("olusturan_id"), MaxLength(36)] public string OlusturanId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("iptal_zamani")] public DateTime? IptalZamani { get; set; }
}

[Table("odeme_plani_taksitleri")]
public class OdemePlaniTaksiti : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("plan_id"), MaxLength(36)] public string PlanId { get; set; } = null!;
    [Column("sira")] public int Sira { get; set; }
    [Column("vade")] public DateOnly Vade { get; set; }
    [Column("tutar_tl", TypeName = "decimal(15,2)")] public decimal TutarTl { get; set; }
    /// <summary>Senetli planda taksidin senedi (kiymetli_evraklar).</summary>
    [Column("evrak_id"), MaxLength(36)] public string? EvrakId { get; set; }
}
