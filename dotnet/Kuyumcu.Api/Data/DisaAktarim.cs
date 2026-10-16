using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// DIŞA AKTARIM TALEBİ — PLAN-2 Adım 4 · Mehmet'in VETO-1 şartları
//
// Neden bir TALEP kaydı var da doğrudan indirme ucu yok:
//   • Şart 3: her aktarım denetim izine girer — kim, ne zaman, kaç satır,
//     hangi kapsam, hangi GEREKÇE. Gerekçesiz aktarım kabul edilmez.
//   • Şart 6: indirme bağlantısı KISA ÖMÜRLÜ ve TEK KULLANIMLIK olmalı.
//     Bu, jetonun sunucu tarafında durum tutmasını zorunlu kılar.
//   • Şart 5: dosya şifreli üretilir; parola talep yanıtında bir kez gösterilir,
//     indirme sırasında GERİ İSTENİR. Böylece bağlantı tek başına dosyayı açmaz.
//
// SAKLANMAYANLAR (bilinçli):
//   • Parolanın kendisi saklanmaz — yalnız SHA-256 özeti tutulur, doğrulamak için.
//   • Üretilen dosya diske YAZILMAZ. İndirme anında akışa (stream) üretilir.
//     Mehmet: "Diskteki AES-256-GCM koruması, masaüstüne düşen düz metin CSV
//     karşısında hükümsüzdür." — sunucuda da düz metin bırakmıyoruz.
// ═════════════════════════════════════════════════════════════════════════════

[Table("disa_aktarim_talepleri")]
public class DisaAktarimTalebi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;

    /// <summary>Şart 1: tek tıkla değil — işletmenin yazılı gerekçesi.</summary>
    [Column("gerekce"), MaxLength(500)] public string Gerekce { get; set; } = null!;

    /// <summary>Virgülle ayrılmış kaynak adları: cari,urun,satis,fatura,kasa,gider-pusulasi.</summary>
    [Column("kapsam"), MaxLength(300)] public string Kapsam { get; set; } = null!;

    /// <summary>Şart 2: varsayılan MASKELİ. false ise ayrı onay metni alınmıştır.</summary>
    [Column("maskeli")] public bool Maskeli { get; set; } = true;

    /// <summary>Jetonun SHA-256 özeti. Jetonun kendisi yalnız yanıtla gider.</summary>
    [Column("jeton_hash"), MaxLength(64)] public string JetonHash { get; set; } = null!;

    /// <summary>Parolanın SHA-256 özeti. Parola saklanmaz.</summary>
    [Column("parola_hash"), MaxLength(64)] public string ParolaHash { get; set; } = null!;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>Şart 6: kısa ömür. Bu andan sonra jeton ölüdür.</summary>
    [Column("son_kullanma")] public DateTime SonKullanma { get; set; }

    /// <summary>Şart 6: tek kullanım. Dolduğunda jeton bir daha çalışmaz.</summary>
    [Column("kullanildi")] public DateTime? Kullanildi { get; set; }

    /// <summary>İndirme tamamlandığında yazılır — denetim izinde "kaç satır" cevabı.</summary>
    [Column("satir_sayisi")] public int? SatirSayisi { get; set; }

    [Column("ip_adresi"), MaxLength(45)] public string? IpAdresi { get; set; }
}
