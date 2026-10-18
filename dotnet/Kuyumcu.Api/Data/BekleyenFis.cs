using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D1-03 (18.10) — tezgâhta BEKLETİLEN satış fişi (park). Defter değildir: stok, kasa ve cari
/// değişmez. Geri alınınca kayıt silinir ve içerik tezgâha yüklenir; satış normal Kaydet'ten geçer.
/// </summary>
[Table("bekleyen_fisler")]
public class BekleyenFis : IKiraciVarlik
{
    /// <summary>Kiracı başına en çok bekleyen fiş (tezgâh kuyruğu; fazlası 409).</summary>
    public const int AzamiAdet = 50;
    /// <summary>Bir bekleyen fişte en çok kalem (tezgâh fişiyle aynı mertebe).</summary>
    public const int AzamiKalem = 200;

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    /// <summary>Listede görünen ad: müşteri adı ya da ilk kalem (sunucu üretir).</summary>
    [Column("etiket"), MaxLength(120)] public string Etiket { get; set; } = null!;
    /// <summary>BekleyenFisIcerik JSON'u.</summary>
    [Column("icerik", TypeName = "text")] public string Icerik { get; set; } = null!;
    [Column("kalem_sayisi")] public int KalemSayisi { get; set; }
    /// <summary>Kalemlerden hesaplanan yaklaşık TL (liste için); satışın tutarı Kaydet'te yeniden hesaplanır.</summary>
    [Column("toplam_tl", TypeName = "decimal(15,2)")] public decimal ToplamTl { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
