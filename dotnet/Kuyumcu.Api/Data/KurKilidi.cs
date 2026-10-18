using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D1-04 (18.10) — KUR KİLİDİ: müşteriye söylenen kur, süresi boyunca satışta geçerli kalır (kur değişse de).
/// Kilit bir kur kaydını (KurSnapshot) işaret eder; satış o kayıtla yapılır, tazelik kapısı kilit süresince aşılır.
/// Müşteriye bağlıysa yalnız o müşterinin satışında kullanılır. Kaldırılan kilit silinmez (iz kalır).
/// </summary>
[Table("kur_kilitleri")]
public class KurKilidi : IKiraciVarlik
{
    /// <summary>Tezgâhtarın açabileceği en uzun kilit (dakika); daha uzunu (sipariş/kapora) yönetici açar.</summary>
    public const int TezgahtarAzamiDakika = 60;
    /// <summary>En uzun kilit: 30 gün (sipariş teslimine kadar).</summary>
    public const int AzamiDakika = 43200;

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kur_snapshot_id"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("not_metni"), MaxLength(200)] public string? Not { get; set; }
    [Column("bitis")] public DateTime Bitis { get; set; }
    [Column("olusturan_id"), MaxLength(36)] public string OlusturanId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("kaldirildi")] public DateTime? Kaldirildi { get; set; }
}
