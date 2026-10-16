using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// SAYIM OTURUMU — bir şubede, bir kapsamda (tümü / reyon / kategori) fiziksel sayım.
///
/// Açılışta kapsamdaki her ürünün SİSTEM adedi satırlara DONDURULUR; sayılan adet
/// sonra girilir. Kapanışta fark ≠ 0 olan sayılmış satırlar deftere
/// <c>SAYIM_DUZELTME</c> olarak yazılır (TB-18 yolu). VUK md.186 envanter kaydı ve
/// md.253 saklama yükümlülüğü için oturum ve satırları silinmez, İPTAL edilir.
/// </summary>
[Table("sayim_oturumlari")]
public class SayimOturumu : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("ad"), MaxLength(150)] public string Ad { get; set; } = null!;
    /// <summary>TUMU · REYON · KATEGORI</summary>
    [Column("kapsam_tur"), MaxLength(20)] public string KapsamTur { get; set; } = "TUMU";
    [Column("kapsam_id"), MaxLength(36)] public string? KapsamId { get; set; }
    /// <summary>ACIK · KAPANDI · IPTAL</summary>
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = "ACIK";
    [Column("acildi")] public DateTime Acildi { get; set; }
    [Column("kapandi")] public DateTime? Kapandi { get; set; }
    [Column("acan_kullanici_id"), MaxLength(36)] public string AcanKullaniciId { get; set; } = null!;
    [Column("kapatan_kullanici_id"), MaxLength(36)] public string? KapatanKullaniciId { get; set; }
    [Column("notlar"), MaxLength(1000)] public string? Notlar { get; set; }

    public static readonly string[] KapsamTurleri = ["TUMU", "REYON", "KATEGORI"];
}

/// <summary>Oturumun bir ürün satırı. <see cref="SayilanAdet"/> null = HENÜZ SAYILMADI (sıfır değil).</summary>
[Table("sayim_satirlari")]
public class SayimSatiri : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("oturum_id"), MaxLength(36)] public string OturumId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    /// <summary>Açılış anında dondurulan sistem adedi.</summary>
    [Column("sistem_adet", TypeName = "decimal(12,3)")] public decimal SistemAdet { get; set; }
    [Column("sayilan_adet", TypeName = "decimal(12,3)")] public decimal? SayilanAdet { get; set; }
    [Column("sayildi")] public DateTime? Sayildi { get; set; }
    [Column("sayan_kullanici_id"), MaxLength(36)] public string? SayanKullaniciId { get; set; }
    /// <summary>Kapanışta deftere yazılan düzeltme hareketi; fark 0 ya da uygulanmadıysa boş.</summary>
    [Column("duzeltme_hareket_id"), MaxLength(36)] public string? DuzeltmeHareketId { get; set; }
}
