using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 / Y4-B — STOPAJ (GELİR VERGİSİ TEVKİFATI) ÖDEMESİ.
//
// Kesilen stopaj dükkânın parası değil, vergi dairesinin alacağıdır (GVK md.94/13;
// beyan GVK md.98, ödeme md.119). DÖNEM BORCU YENİ TABLODA TUTULMAZ, gider
// pusulalarından TÜRETİLİR (Σ stopaj_tutar, iptal edilmemiş, teslim yerel ayı).
// Bu tablo yalnız ÖDEMEYİ tutar.
//
// APPEND-ONLY: satır düzenlenmez, silinmez. Ödemenin kasa çıkışı mevcut GİDER
// yolundan (GiderServisi.Kaydet) yazılır; DÜZELTME = o kasa satırının STORNOSU
// (48-A motoru, StornoServisi.KasaHareketiStorno). Stornolanan kasa satırına bağlı
// ödeme "ödenen" toplamına girmez (türetme StopajServisi'nde).
// ═════════════════════════════════════════════════════════════════════════════

[Table("stopaj_odemeleri")]
public class StopajOdemesi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Vergi dönemi — "YYYY-MM" (teslimin yerel ayı).</summary>
    [Column("donem"), MaxLength(7)] public string Donem { get; set; } = null!;

    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }

    [Column("odeme_tarihi")] public DateTime OdemeTarihi { get; set; }

    /// <summary>Ödemenin kasa çıkışı (kasa_hareketleri.id) — mevcut GİDER yolunun satırı.</summary>
    [Column("hareket_id"), MaxLength(36)] public string HareketId { get; set; } = null!;

    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;

    /// <summary>Vergi dairesi tahakkuk fişi numarası — isteğe bağlı.</summary>
    [Column("tahakkuk_fis_no"), MaxLength(50)] public string? TahakkukFisNo { get; set; }

    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>
    /// Dönem içindeki SIRA (1, 2, 3 …) — EŞZAMANLILIK KİLİDİ (hasım bulgusu, 01.10).
    /// UNIQUE (tenant_id, donem, sira_no): iki ödeme aynı "son sıra"yı okuyup aynı
    /// numarayı yazmaya kalkarsa veritabanı ikincisini reddeder (1062 → 409); kalan
    /// kontrolü kilitsiz okunsa bile kesilenin üstüne ödeme YAZILAMAZ.
    /// </summary>
    [Column("sira_no")] public int SiraNo { get; set; }
}
