using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// M7 — DEMO BANKASI: "Havale gönder" ekranının yazdığı, sahte sağlayıcının (SAHTE) çekiciye sunduğu gelen
/// havale. Gerçek bankanın hesap hareketi servisinin yerini tutar; çekici bunu gerçek bir banka hareketi gibi
/// içeri alır (müşteri bul/aç, karar kutusu, canlı yenileme). Gerçek parayı TEMSİL ETMEZ; yalnız sağlayıcı kodu
/// SAHTE olan hesapta kullanılır.
/// </summary>
[Table("sahte_banka_hareketleri")]
public class SahteBankaHareketi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_hesap_id"), MaxLength(36)] public string BankaHesapId { get; set; } = null!;
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("gonderen_ad"), MaxLength(150)] public string GonderenAd { get; set; } = null!;
    [Column("gonderen_iban"), MaxLength(34)] public string? GonderenIban { get; set; }
    /// <summary>Gönderenin TCKN/VKN'si — bankanın hareket kaydında gelen bilgi (müşteri bulmak için).</summary>
    [Column("gonderen_kimlik_no"), MaxLength(11)] public string? GonderenKimlikNo { get; set; }
    [Column("aciklama"), MaxLength(200)] public string? Aciklama { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
