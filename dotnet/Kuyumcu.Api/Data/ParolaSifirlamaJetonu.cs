using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET AUTH-SIFIRLA — "Parolamı unuttum" bağlantısının jetonu (kurul 5 engeli).
//
// Ekip oyu (Mehmet güvenlik + Taşkın + Ertuğ), madde (2):
//   • Jeton EN AZ 32 bayt rastgeledir; istemciye (e-postaya) düz metin gider,
//     veritabanına YALNIZ SHA-256 özeti (64 hex) yazılır — düz metin kolon YOK.
//   • Tek kullanımlıktır (kullanildi damgası) ve 30 dk geçerlidir.
//   • Yeni istek gelince ve parola sıfırlanınca kullanıcının AÇIK jetonları
//     iptal_edildi = 1 olur.
//
// DoD sözleşmesi (Bertan, dalga 2b): bu kayıt KULLANICI TARAFINDAN DÜZENLENEMEZ
// ve SİLİNMEZ — güvenlik jetonudur, sistem üretir, sistem tüketir. Düzeltme yolu
// "yeni bağlantı iste"dir (eskisini otomatik iptal eder). Gerekçe raporda.
//
// ⚠ Kullanildi [ConcurrencyCheck]: iki eşzamanlı "tamamla" isteğinde EF
//   UPDATE … WHERE kullanildi IS NULL üretir; ikinci istek 0 satır günceller ve
//   DbUpdateConcurrencyException → 400 alır. Tek kullanım yarışla delinemez.
// ═════════════════════════════════════════════════════════════════════════════

[Table("parola_sifirlama_jetonlari")]
public class ParolaSifirlamaJetonu : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;

    /// <summary>SHA-256(jeton) — 64 küçük hex. Düz metin jeton HİÇBİR kolonda tutulmaz.</summary>
    [Column("jeton_hash"), MaxLength(64)] public string JetonHash { get; set; } = null!;

    /// <summary>Son geçerlilik anı (UTC) — oluşturma + 30 dk.</summary>
    [Column("gecerlilik")] public DateTime Gecerlilik { get; set; }

    /// <summary>Kullanıldığı an (UTC); null = kullanılmadı. Eşzamanlılık belirteci.</summary>
    [Column("kullanildi"), ConcurrencyCheck] public DateTime? Kullanildi { get; set; }

    /// <summary>Yeni istek ya da başarılı sıfırlama ile geçersiz kılındı.</summary>
    [Column("iptal_edildi")] public bool IptalEdildi { get; set; }

    /// <summary>İsteği yapan istemcinin IP'si (denetim için; jeton DEĞİLDİR).</summary>
    [Column("ip_adresi"), MaxLength(45)] public string? IpAdresi { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
