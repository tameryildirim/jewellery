using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Abonelik;

// ═════════════════════════════════════════════════════════════════════════════
// ABONELİK VARLIKLARI — PAKET P-27 (F-1 abonelik + F-3 plan kapısı · R-1).
//
// NEDEN YENİ TABLO: bugüne dek abonelik hakkının TEK izi `tenants.plan`
// kolonuydu. Bir kolon şunları söyleyemez: plan NE ZAMAN başladı, NE ZAMAN
// bitiyor, KİM değiştirdi, ÖNCEKİ dönem neydi. "Ücretli sözleşme" bu üç
// sorunun cevabı olmadan imzalanamaz (kurul md.2).
//
// ⚠ İKİ TABLO, İKİ FARKLI KAPSAM:
//   • abonelik_planlari    → KİRACI-ÜSTÜ katalog (kiracı DOĞMADAN vardır ve
//     tüm kiracılar aynı katalogdan beslenir). KuyumcuDbContext kapsam
//     denetiminde GEREKÇELİ beyaz liste maddesi vardır — KiraciDaveti /
//     YedekKaydi kalıbının AYNISI, yeni desen açılmadı.
//   • kiraci_abonelikleri  → IKiraciVarlik. Global kiracı süzgeci
//     OnModelCreating'de OTOMATİK kurulur.
//
// ⚠ FK YOK (bilerek): depoda `HasOne`/`WithMany` sayısı 0'dır ve FK borcu
//   TEK paketle (P-34) toplu kapatılacaktır. Bu tabloların şema dilimi P-34'ten
//   ÖNCE bitirilmiştir ki FK grubu bunları da kapsasın (plan § 1 md.11 ·
//   hasım bulgusu 21). Burada tek taraflı FK atmak o kararı delerdi.
//
// ⚠ FİYAT YOK (K-16): rakamlı fiyat ilanı KURUCU kalemidir. Katalogda tutar
//   kolonu AÇILMADI — açılsaydı ürün "fiyatı var" izlenimi verirdi ve ekip
//   uydurma bir rakam yazmak zorunda kalırdı (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Satılabilir plan kataloğu. KİRACI-ÜSTÜdür: aynı satır tüm kiracılar için
/// geçerlidir, yazma yolu YALNIZ sistem anahtarıyla korunan uçtur.
/// </summary>
[Table("abonelik_planlari")]
public class AbonelikPlani
{
    /// <summary>Plan kodu — BASLANGIC · PRO · KURUMSAL. Birincil anahtar.</summary>
    [Key, Column("kod"), MaxLength(20)] public string Kod { get; set; } = null!;

    [Column("ad"), MaxLength(80)] public string Ad { get; set; } = null!;
    [Column("aciklama"), MaxLength(300)] public string? Aciklama { get; set; }

    /// <summary>
    /// Yükseltme/indirme SIRASI. Büyük seviye = daha geniş hak.
    /// Plan kapısının "yükseltme mi?" kararı BU sayıyla verilir; ad
    /// karşılaştırmasıyla değil (yeni plan eklenince kapı sessizce açılırdı).
    /// </summary>
    [Column("seviye")] public int Seviye { get; set; }

    /// <summary>NULL = sınırsız. Sınır ürün davranışına henüz BAĞLANMADI (devredildi: P-28 ölçümü).</summary>
    [Column("kullanici_tavan")] public int? KullaniciTavan { get; set; }
    [Column("sube_tavan")] public int? SubeTavan { get; set; }

    /// <summary>Pasif plan yeni abonelikte SEÇİLEMEZ; mevcut abonelikler etkilenmez (veri kaybı yok).</summary>
    [Column("aktif")] public bool Aktif { get; set; } = true;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Kiracının abonelik DÖNEMİ. Her plan değişimi yeni bir satır doğurur; önceki
/// satır SONLANDI olarak kapatılır — geçmiş SİLİNMEZ (append-only tarihçe).
/// </summary>
[Table("kiraci_abonelikleri")]
public class KiraciAbonelik : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("plan_kodu"), MaxLength(20)] public string PlanKodu { get; set; } = null!;

    [Column("baslangic")] public DateTime Baslangic { get; set; }

    /// <summary>Dönem bitişi (UTC). NULL = süresiz. Geçmişse kiracı SALT OKUNUR olur (fail-closed).</summary>
    [Column("bitis")] public DateTime? Bitis { get; set; }

    /// <summary>AKTIF · SONLANDI — <see cref="AbonelikDurumlari"/>.</summary>
    [Column("durum"), MaxLength(16)] public string Durum { get; set; } = AbonelikDurumlari.Aktif;

    /// <summary>SISTEM (operatör) · KIRACI (kendi indirdiği plan) — <see cref="AbonelikKaynaklari"/>.</summary>
    [Column("kaynak"), MaxLength(16)] public string Kaynak { get; set; } = AbonelikKaynaklari.Sistem;

    /// <summary>Değişimin gerekçesi. Sistem yolunda ZORUNLU (≥20 karakter).</summary>
    [Column("gerekce"), MaxLength(500)] public string? Gerekce { get; set; }

    /// <summary>Sistem operatörünün adı. Kiracı yolunda NULL (kullanıcı kimliği denetim izindedir).</summary>
    [Column("operator_adi"), MaxLength(120)] public string? OperatorAdi { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Abonelik dönemi durumları. BEYAZ LİSTE: bilinmeyen değer "aktif dönem"
/// sayılmaz — okuyucu tarafta AKTIF ile TAM eşleşme aranır.
/// </summary>
public static class AbonelikDurumlari
{
    public const string Aktif = "AKTIF";
    public const string Sonlandi = "SONLANDI";

    public static bool Gecerli(string? d) => d is Aktif or Sonlandi;
}

/// <summary>Abonelik satırını kimin doğurduğu.</summary>
public static class AbonelikKaynaklari
{
    public const string Sistem = "SISTEM";
    public const string Kiraci = "KIRACI";

    public static bool Gecerli(string? k) => k is Sistem or Kiraci;
}
