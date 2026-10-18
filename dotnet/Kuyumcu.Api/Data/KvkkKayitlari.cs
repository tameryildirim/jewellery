using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// D1-19 (18.10, rakip eşitleme) — KVKK aydınlatma ve açık rıza kaydı.
//   • Aydınlatma metnini İŞLETME girer (hukukçusundan aldığı metin) — sistem metin UYDURMAZ.
//     Her değişiklik YENİ SÜRÜM (eski sürüm silinmez: hangi müşteriye hangi metnin okunduğu ispatı).
//   • Müşteri kaydı EKLENEREK tutulur: aydınlatma yapıldı · pazarlama izni verildi / geri alındı.
//     Geri alma da yeni satırdır; geçmiş silinmez (KVKK md.5/md.10 ispat yükü veri sorumlusundadır).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İşletmenin aydınlatma metninin bir sürümü (en büyük sürüm geçerlidir).</summary>
[Table("kvkk_metinleri")]
public class KvkkMetni : IKiraciVarlik
{
    public const int EnFazlaKarakter = Kuyumcu.Core.Domain.Girdi.AlanSinirlari.KvkkMetni;

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("surum")] public int Surum { get; set; }
    [Column("metin", TypeName = "text")] public string Metin { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Bir müşterinin KVKK beyanı (ekleme kaydı; geri alma da yeni satır).</summary>
[Table("kvkk_kayitlari")]
public class KvkkKaydi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    /// <summary><see cref="KvkkKayitTuru"/>.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    /// <summary>true = yapıldı / izin verildi · false = izin geri alındı.</summary>
    [Column("deger")] public bool Deger { get; set; }
    /// <summary>Okunan aydınlatma metninin sürümü (aydınlatma kaydında zorunlu).</summary>
    [Column("metin_surumu")] public int? MetinSurumu { get; set; }
    /// <summary><see cref="KvkkKanal"/>.</summary>
    [Column("kanal"), MaxLength(15)] public string Kanal { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
}

public static class KvkkKayitTuru
{
    public const string Aydinlatma = "AYDINLATMA";
    public const string PazarlamaIzni = "PAZARLAMA_IZNI";
    public static readonly string[] Tumu = [Aydinlatma, PazarlamaIzni];
}

public static class KvkkKanal
{
    /// <summary>Tezgâhta sözlü / ekranda gösterilerek.</summary>
    public const string Tezgah = "TEZGAH";
    /// <summary>Basılı form imzalatıldı.</summary>
    public const string ImzaliForm = "IMZALI_FORM";
    public static readonly string[] Tumu = [Tezgah, ImzaliForm];
}
