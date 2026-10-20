using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// Y-04 (20.10, rakip Dezy "öz varlık · dünle fark") — gün kapanışında alınan servet anlık görüntüsü (kiracı × gün, tek satır).
/// Kaynak: PozisyonServisi.NetVarlik (kasa+banka, altın has değeri, cari). Kapanış yapılmayan gün için satır yoktur;
/// karşılaştırma "son kapanış"a göre yapılır ve tarihi yazılır (uydurma "dün" yok).
/// </summary>
[Table("servet_gunluk")]
public class ServetSnapshot : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("gun")] public DateOnly Gun { get; set; }
    [Column("nakit_tl", TypeName = "decimal(15,2)")] public decimal? NakitTl { get; set; }
    [Column("altin_tl", TypeName = "decimal(15,2)")] public decimal? AltinTl { get; set; }
    [Column("cari_tl", TypeName = "decimal(15,2)")] public decimal? CariTl { get; set; }
    [Column("net_tl", TypeName = "decimal(15,2)")] public decimal? NetTl { get; set; }
    [Column("altin_net_has", TypeName = "decimal(15,6)")] public decimal AltinNetHasGram { get; set; }
    [Column("has_fiyati", TypeName = "decimal(15,4)")] public decimal? HasFiyati { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
