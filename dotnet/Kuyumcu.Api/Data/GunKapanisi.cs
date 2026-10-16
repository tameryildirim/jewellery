using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// KASA GÜN SONU KAPANIŞI — sayılan nakit, defter bakiyesi ve farkın kaydı.
///
/// ⚠ NEDEN VAR: kasa bakiyesi sistemin İDDİASIDIR; çekmecedeki para GERÇEKTİR.
/// İkisi arasındaki fark ancak SAYILIP KAYDEDİLİRSE görülür. Kaydedilmeyen
/// fark ertesi güne devreder ve hangi günden geldiği bir daha bulunamaz.
///
/// Kapanış <b>kasa + YEREL GÜN</b> başına TEKTİR. Gün kapandıktan sonra o
/// kasaya aynı güne ait yeni hareket YAZILAMAZ — yazılabilseydi sayım
/// geçersizleşir, imzalanmış bir tutanağın altı sonradan doldurulmuş olurdu.
/// Yeniden açmak SAHİP yetkisi ister ve gerekçesiyle birlikte kaydedilir.
/// </summary>
[Table("gun_kapanislari")]
public class GunKapanisi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    /// <summary>
    /// Kapatılan YEREL gün (Europe/Istanbul). UTC tarih KULLANILMAZ: İstanbul'da
    /// 00:00–03:00 arası yapılan işlem UTC'de bir önceki güne düşer ve yanlış
    /// günün kapanışına sayılırdı (tevsik kapısındaki K5 kararıyla aynı gerekçe).
    /// </summary>
    [Column("yerel_gun")] public DateOnly YerelGun { get; set; }

    /// <summary>Çekmeceden fiilen sayılan nakit.</summary>
    [Column("sayilan_nakit", TypeName = "decimal(15,2)")] public decimal SayilanNakit { get; set; }

    /// <summary>Kapanış anında sistemin iddia ettiği kasa bakiyesi.</summary>
    [Column("defter_bakiye", TypeName = "decimal(15,2)")] public decimal DefterBakiye { get; set; }

    /// <summary>sayılan − defter. Pozitif = fazla, negatif = eksik.</summary>
    [Column("fark", TypeName = "decimal(15,2)")] public decimal Fark { get; set; }

    /// <summary>Fark sıfır değilse ZORUNLU gerekçe.</summary>
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }

    [Column("kapatan_kullanici_id"), MaxLength(36)] public string KapatanKullaniciId { get; set; } = null!;
    [Column("kapanis_zamani")] public DateTime KapanisZamani { get; set; }

    /// <summary>Gün yeniden açıldıysa dolar; dolu kayıt artık kilit uygulamaz.</summary>
    [Column("acan_kullanici_id"), MaxLength(36)] public string? AcanKullaniciId { get; set; }
    [Column("acilis_zamani")] public DateTime? AcilisZamani { get; set; }
    [Column("acilis_gerekcesi", TypeName = "text")] public string? AcilisGerekcesi { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>Kilit hâlâ geçerli mi — yeniden açılmışsa DEĞİL.</summary>
    [NotMapped] public bool Kilitli => AcilisZamani is null;
}
