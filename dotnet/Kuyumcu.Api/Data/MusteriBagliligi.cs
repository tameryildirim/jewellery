using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>D2-12 (18.10) — müşterinin dilek listesi (istediği ürün/model; isteğe bağlı katalog ürünü). Kapatılır, silinmez.</summary>
[Table("cari_dilekleri")]
public class CariDilek : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }
    [Column("aciklama"), MaxLength(300)] public string Aciklama { get; set; } = null!;
    [Column("olusturan_id"), MaxLength(36)] public string OlusturanId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("kapatildi")] public DateTime? Kapatildi { get; set; }
}

/// <summary>
/// D2-12 — sadakat puanı KULLANIMI. Puan mali kayıt değildir: kazanılan puan net alışverişten hesaplanır, kullanım
/// bu tabloya düşülür; karşılığında verilen indirim/hediye tezgâhta normal yoldan (iskonto/hediye) işlenir.
/// </summary>
[Table("sadakat_puan_kullanimlari")]
public class SadakatPuanKullanimi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("puan")] public int Puan { get; set; }
    [Column("aciklama"), MaxLength(300)] public string Aciklama { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
}
