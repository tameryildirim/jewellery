using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// E-1/E-2 (20.10, kurucu: "satış fişini kaydetsin, arka planda fatura gönderilsin") — işletmenin e-Fatura /
/// e-Arşiv entegratörü bağlantısı. Kuyumcu entegratörle KENDİSİ sözleşme yapar; entegratör panelinden ürettiği
/// API anahtarı AES-GCM ile şifreli saklanır, ekrana bir daha dönmez. Kaynak: ARASTIRMA-E-FATURA-ENTEGRASYON-2026-10-19.md § 5.
/// </summary>
[Table("ebelge_ayarlari")]
public class EBelgeAyari : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    /// <summary>Entegratör kodu (<see cref="Modules.EBelge.EBelgeSaglayicilari"/>), ör. NILVERA.</summary>
    [Column("saglayici"), MaxLength(20)] public string Saglayici { get; set; } = null!;
    /// <summary>TEST (entegratörün deneme ortamı) | CANLI.</summary>
    [Column("ortam"), MaxLength(10)] public string Ortam { get; set; } = "TEST";
    [Column("anahtar_sifreli"), MaxLength(2000)] public string AnahtarSifreli { get; set; } = null!;
    /// <summary>Entegratörün verdiği fatura seri ön eki (3 harf); boşsa entegratörün varsayılanı.</summary>
    [Column("seri_on_eki"), MaxLength(3)] public string? SeriOnEki { get; set; }
    /// <summary>Gönderim açık mı? Kapalıyken kuyruğa yeni fatura alınmaz (var olanlar durur).</summary>
    [Column("etkin")] public bool Etkin { get; set; } = true;
    /// <summary>Bu andan SONRA kesilen faturalar gönderilir; geçmiş faturalar kendiliğinden gönderilmez.</summary>
    [Column("baslangic")] public DateTime Baslangic { get; set; }
    [Column("son_deneme")] public DateTime? SonDeneme { get; set; }
    [Column("son_hata"), MaxLength(300)] public string? SonHata { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// E-5 — bir faturanın e-belge gönderim kaydı (giden kutusu). ETTN kayıt açılırken BİR KEZ üretilir; yeniden
/// denemede aynı ETTN gider (entegratör aynı belgeyi ikinci kez oluşturmaz). Gönderilemeyen fatura "kesildi"
/// sayılmaz: durum ekranda açıkça görünür (GİB'e ulaşmayan e-Arşiv düzenlenmemiş sayılır).
/// </summary>
[Table("ebelge_gonderimleri")]
public class EBelgeGonderimi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("fatura_id"), MaxLength(36)] public string FaturaId { get; set; } = null!;
    [Column("ettn")] public Guid Ettn { get; set; }
    [Column("saglayici"), MaxLength(20)] public string Saglayici { get; set; } = null!;
    /// <summary>EARSIV | EFATURA — gönderim anında alıcının kayıtlı kullanıcı sorgusuyla seçilir; o ana kadar null.</summary>
    [Column("senaryo"), MaxLength(10)] public string? Senaryo { get; set; }
    /// <summary><see cref="Modules.EBelge.EBelgeDurumlari"/>.</summary>
    [Column("durum"), MaxLength(12)] public string Durum { get; set; } = null!;
    /// <summary>Entegratörün/GİB'in verdiği 16 haneli belge numarası.</summary>
    [Column("belge_no"), MaxLength(20)] public string? BelgeNo { get; set; }
    [Column("deneme_sayisi")] public int DenemeSayisi { get; set; }
    [Column("sonraki_deneme")] public DateTime? SonrakiDeneme { get; set; }
    [Column("son_hata"), MaxLength(500)] public string? SonHata { get; set; }
    /// <summary>Entegratöre ilk başarıyla ulaştığı an (iptal süresi bundan sayılır).</summary>
    [Column("gonderildi")] public DateTime? Gonderildi { get; set; }
    [Column("sonuclandi")] public DateTime? Sonuclandi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
