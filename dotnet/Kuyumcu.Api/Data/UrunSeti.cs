using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D1-16 (18.10, rakip eşitleme: "set") — birden çok ürünün tek barkodla satılan takımı (ör. kolye + küpe).
/// Set kendi stoğunu TUTMAZ: tezgâhta set barkodu okutulunca parçalar kendi fiyatlarıyla fişe eklenir,
/// stok her parçadan düşer (yeni stok kavramı yok, has/fiyat kuralları parça başına aynen).
/// </summary>
[Table("urun_setleri")]
public class UrunSeti : IKiraciVarlik
{
    public const int AzamiKalem = 20;
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    /// <summary>Setin barkodu — ürün barkodları ve toptancı barkodlarıyla da çakışamaz (tek okutma ad alanı).</summary>
    [Column("barkod"), MaxLength(50)] public string Barkod { get; set; } = null!;
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>D1-16 — setin bir parçası (ürün + adet).</summary>
[Table("urun_seti_kalemleri")]
public class UrunSetiKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("set_id"), MaxLength(36)] public string SetId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }
    [Column("sira")] public int Sira { get; set; }
}
