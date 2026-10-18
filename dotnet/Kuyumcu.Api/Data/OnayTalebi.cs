using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D1-05 (18.10) — SATIŞ ONAY TALEBİ. İşletmenin eşiklerini (indirim yüzdesi, fiş tutarı, zararına satış) ya da
/// çalışanın kişisel iskonto sınırını aşan satış patron/müdür onayı ister. Talep satışın içeriğinden üretilen
/// PARMAK İZİNE bağlıdır: onaylanan talep yalnız AYNI satışa, AYNI çalışan tarafından, onaydan sonraki 30 dakika
/// içinde, BİR KEZ kullanılır. Talep silinmez (iz).
/// </summary>
[Table("onay_talepleri")]
public class OnayTalebi : IKiraciVarlik
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Onaylandi = "ONAYLANDI";
    public const string Reddedildi = "REDDEDILDI";
    /// <summary>Onaydan sonra kullanma süresi (dakika); bekleyen talep de bu süre sonra düşer.</summary>
    public const int GecerlilikDakika = 30;

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("isteyen_id"), MaxLength(36)] public string IsteyenId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    /// <summary>Satış içeriğinin SHA-256 özeti (sunucu üretir).</summary>
    [Column("parmak"), MaxLength(64)] public string Parmak { get; set; } = null!;
    /// <summary>Virgülle ayrılmış sebep kodları (ISKONTO, TUTAR, ZARARINA, KISISEL).</summary>
    [Column("sebepler"), MaxLength(100)] public string Sebepler { get; set; } = null!;
    [Column("toplam_tl", TypeName = "decimal(15,2)")] public decimal ToplamTl { get; set; }
    [Column("liste_tl", TypeName = "decimal(15,2)")] public decimal ListeTl { get; set; }
    [Column("indirim_yuzde", TypeName = "decimal(7,2)")] public decimal IndirimYuzde { get; set; }
    [Column("has_degeri_tl", TypeName = "decimal(15,2)")] public decimal HasDegeriTl { get; set; }
    [Column("kalem_sayisi")] public int KalemSayisi { get; set; }
    [Column("durum"), MaxLength(12)] public string Durum { get; set; } = Bekliyor;
    [Column("karar_veren_id"), MaxLength(36)] public string? KararVerenId { get; set; }
    [Column("karar_zamani")] public DateTime? KararZamani { get; set; }
    [Column("karar_notu"), MaxLength(200)] public string? KararNotu { get; set; }
    [Column("kullanildi")] public DateTime? Kullanildi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
