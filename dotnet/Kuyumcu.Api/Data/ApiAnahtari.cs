using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D3-11 (19.10) — dış API anahtarı (web sitesi, vitrin ekranı, muhasebe aracı). Anahtarın kendisi SAKLANMAZ:
/// yalnız ilk 8 karakteri (önek, arama için) ve SHA-256 özeti tutulur; oluşturulunca bir kez gösterilir.
/// Kapsam virgüllü: KUR (güncel kur + canlı yayın) · URUN (ürün, stok, liste fiyatı). Yalnız OKUMA.
/// </summary>
[Table("api_anahtarlari")]
public class ApiAnahtari : IKiraciVarlik
{
    public const string KapsamKur = "KUR", KapsamUrun = "URUN";

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("onek"), MaxLength(8)] public string Onek { get; set; } = null!;
    [Column("ozet"), MaxLength(64)] public string Ozet { get; set; } = null!;
    [Column("kapsamlar"), MaxLength(40)] public string Kapsamlar { get; set; } = null!;
    [Column("olusturan_id"), MaxLength(36)] public string OlusturanId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("son_kullanim")] public DateTime? SonKullanim { get; set; }
    [Column("iptal_zamani")] public DateTime? IptalZamani { get; set; }
    /// <summary>Y-14 (20.10): süreli anahtar/bağlantı — bu andan sonra geçersiz; null = süresiz.</summary>
    [Column("bitis")] public DateTime? Bitis { get; set; }
}
