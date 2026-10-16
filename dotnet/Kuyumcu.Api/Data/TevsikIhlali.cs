using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-16) — GEÇ FARK EDİLEN NAKİT SINIRI AŞIMI
//
// Kapı (TevsikKapisi) her şeye rağmen bir aşımı kaçırabilir: kural kapatılmış,
// eski kayıt, eşzamanlı iki fiş (TOCTOU), veri göçü. TEVSIK_KURTARMA işi bu
// aşımları ertesi tur kendisi bulur ve bu tabloya yazar.
//
// DAYANAK (şartname § 3.1 M-6, birincil): VUK mük. md.355 — ödemeyi YAPAN,
// ödemeyi takip eden beş iş günü içinde kendiliğinden idareye bildirirse ödeme
// yapan adına ceza kesilmez. Muafiyet ÖDEYEN taraf içindir → `yon` kolonu.
//
// ⚠ ADR-0004: satır SİLİNMEZ. Kapatma (BILDIRILDI / GECERSIZ) BİR KEZ yazılır;
//   SURE_DOLDU iş tarafından otomatik konur ve kayıt kalır.
// ⚠ ŞUBE TAŞIMAZ: ihlal mükellef (kiracı) genelidir (ŞK-A § 2.6).
// ⚠ KİŞİSEL VERİ TAŞIMAZ: yalnız cari kimliği ve hareket kimlikleri (kanıt).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İhlal türleri.</summary>
public static class TevsikIhlalTuru
{
    public const string GunlukKumulatif = "GUNLUK_KUMULATIF";
    public const string IslemTutari = "ISLEM_TUTARI";
    public const string Taksit = "TAKSIT";
}

/// <summary>Paranın yönü — muafiyet (md.355) yalnız ÖDEYEN taraf içindir.</summary>
public static class TevsikIhlalYonu
{
    /// <summary>Kuyumcu tahsil etti (müşteriye satış) — kuyumcu için bu yolla muafiyet yok.</summary>
    public const string Tahsilat = "TAHSILAT";
    /// <summary>Kuyumcu ödedi (vatandaştan alış, tedarikçiye tediye) — son güne kadar bildirirse ceza yok.</summary>
    public const string Odeme = "ODEME";
}

/// <summary>İhlal durumu.</summary>
public static class TevsikIhlalDurumu
{
    public const string Acik = "ACIK";
    public const string Bildirildi = "BILDIRILDI";
    public const string SureDoldu = "SURE_DOLDU";
    public const string Gecersiz = "GECERSIZ";
}

[Table("tevsik_ihlalleri")]
public class TevsikIhlali : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary><see cref="TevsikIhlalTuru"/>.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;

    /// <summary><see cref="TevsikIhlalYonu"/>.</summary>
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = null!;

    /// <summary>Karşı taraf — NULL olamaz (kimliksiz satışta kişi bazlı ihlal ölçülemez).</summary>
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;

    /// <summary>İhlalin YEREL günü (Europe/Istanbul).</summary>
    [Column("ihlal_gunu")] public DateOnly IhlalGunu { get; set; }

    [Column("nakit_toplam_tl", TypeName = "decimal(15,2)")] public decimal NakitToplamTl { get; set; }

    /// <summary>O günün haddi (YASAL-1 TEVSIK_HADDI_TL).</summary>
    [Column("had_tl", TypeName = "decimal(15,2)")] public decimal HadTl { get; set; }

    /// <summary>Kanıt: hareket kimlikleri, JSON dizi. Kişisel veri YOK.</summary>
    [Column("hareket_idleri", TypeName = "text")] public string HareketIdleri { get; set; } = "[]";

    /// <summary>ihlal_gunu + TEVSIK_BILDIRIM_IS_GUNU iş günü (ResmiTatiller ile).</summary>
    [Column("bildirim_son_gunu")] public DateOnly BildirimSonGunu { get; set; }

    /// <summary><see cref="TevsikIhlalDurumu"/>.</summary>
    [Column("durum"), MaxLength(12)] public string Durum { get; set; } = TevsikIhlalDurumu.Acik;

    // ── Kapatma — BİR KEZ yazılır ──
    [Column("kapatma_tarihi")] public DateTime? KapatmaTarihi { get; set; }
    [Column("kapatan_kullanici_id"), MaxLength(36)] public string? KapatanKullaniciId { get; set; }
    [Column("kapatma_gerekcesi"), MaxLength(500)] public string? KapatmaGerekcesi { get; set; }
    [Column("bildirim_evrak_no"), MaxLength(60)] public string? BildirimEvrakNo { get; set; }

    /// <summary>Beyan edilen bildirim günü (yerel).</summary>
    [Column("bildirim_tarihi")] public DateOnly? BildirimTarihi { get; set; }

    /// <summary>Bildirim son günden SONRA yapıldıysa true — yalan beyan üretilmez.</summary>
    [Column("gec")] public bool Gec { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
