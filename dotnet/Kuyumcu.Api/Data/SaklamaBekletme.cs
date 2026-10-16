using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 (§ 4.3) — SAKLAMA BEKLETMESİ.
//
// Kendiliğinden maskeleme (SAKLAMA_UYGULA işi) yasal saklama/uyuşmazlık hâlinde
// SAHİP'in gerekçeli kararıyla BEKLETİLİR. Kapsam ya kiracının TÜM kayıtları ya
// da TEK bir kimlik tespitidir. Bitiş en çok 1 yıl sonradır; yenilenebilir.
//
// ⚠ SİLİNMEZ (Remove/ExecuteDelete yok). Düzeltme: PATCH (bitiş/gerekçe — her
//   değişiklik denetim izine yazılır); erken bitirme: "kaldır" damgası
//   (kaldirildi + kaldiran_kullanici_id). Satır tarihçe olarak kalır.
// ⚠ Kişisel veri YOKTUR: gerekçe serbest metindir ama ekranda "kimlik yazmayın"
//   uyarısı durur; tespit kimliği yalnız ULID'dir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bekletme kapsamı.</summary>
public static class SaklamaBekletmeKapsami
{
    /// <summary>Kiracının bütün MASAK kimlik kayıtları.</summary>
    public const string Tumu = "TUMU";
    /// <summary>Tek bir kimlik tespiti (ve ona bağlı beyan).</summary>
    public const string Tespit = "TESPIT";
    public static readonly string[] Hepsi = [Tumu, Tespit];
}

[Table("saklama_bekletmeleri")]
public class SaklamaBekletme : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary><see cref="SaklamaBekletmeKapsami"/>.</summary>
    [Column("kapsam"), MaxLength(10)] public string Kapsam { get; set; } = SaklamaBekletmeKapsami.Tumu;

    /// <summary>Kapsam TESPIT ise bekletilen kimlik tespiti; TUMU'de NULL.</summary>
    [Column("kimlik_tespiti_id"), MaxLength(36)] public string? KimlikTespitiId { get; set; }

    [Column("baslangic")] public DateTime Baslangic { get; set; }

    /// <summary>UTC; bu andan sonra bekletme kendiliğinden biter (iş maskelemeye devam eder).</summary>
    [Column("bitis")] public DateTime Bitis { get; set; }

    [Column("gerekce"), MaxLength(500)] public string Gerekce { get; set; } = null!;

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    /// <summary>Erken bitirildiyse damga; NULL = kaldırılmadı.</summary>
    [Column("kaldirildi")] public DateTime? Kaldirildi { get; set; }
    [Column("kaldiran_kullanici_id"), MaxLength(36)] public string? KaldiranKullaniciId { get; set; }
}
