using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// 17.10 — ÖDEMENİN HANGİ BORÇLU SATIŞA SAYILDIĞI (mahsup). Kurucu: "müşterinin borcu olan satışlar liste
/// olarak gelsin, oradan seçim yapabilsin". Araştırma (KISMI-ODEME-ARASTIRMASI-2026-10-17 § 4.3): en küçük biçim.
/// <para>
/// ⚠ BAKİYEYİ DEĞİŞTİRMEZ. Tek doğruluk kaynağı defterdir (hareketler + cari_bakiyeler). Bu satır yalnız
/// <see cref="Modules.Vade.VadeKapisi"/> defter yürüyüşünde azaltan satırın ÖNCE hangi açık partiyi tüketeceğini
/// söyler; kalan tutar bugünkü FIFO'ya devam eder. Satır yazmayan her ödeme yolu bugünkü gibi davranır (sapma yok).
/// </para>
/// <para>
/// ⚠ APPEND-ONLY: güncellenmez, silinmez. Seçimi değiştirmenin yolu ödemenin stornosu ve yeniden işlenmesidir;
/// stornolanmış ödemenin satırları yürüyüşte yok sayılır. TBK 101/1: paranın hangi borca sayılacağını borçlu
/// bildirir — bu satır o beyanın kaydıdır.
/// </para>
/// </summary>
[Table("cari_mahsuplar")]
public class CariMahsup : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    /// <summary>Borcu azaltan hareket (TAHSILAT).</summary>
    [Column("odeme_hareket_id"), MaxLength(36)] public string OdemeHareketId { get; set; } = null!;
    /// <summary>Açık partiyi doğuran hareket (SATIS — veresiye payı).</summary>
    [Column("borc_hareket_id"), MaxLength(36)] public string BorcHareketId { get; set; } = null!;
    [Column("tutar_tl", TypeName = "decimal(15,2)")] public decimal TutarTl { get; set; }
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
