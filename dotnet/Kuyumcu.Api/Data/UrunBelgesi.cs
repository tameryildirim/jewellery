using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 4 — Belge deposu (Faz A).
//
// ⚠ DOSYA ADI HÂLÂ UrunBelgesi.cs: sınıf `Belge` oldu ama dosyayı yeniden
// adlandırmak (git mv / rm) TurkReport hook'u tarafından bloklanıyor. Bertan'ın
// koşulu gereği İKİNCİ bir Belge.cs AÇILMADI (boş .cs dosyası bırakmak yerine
// sınıf aynı dosyada yeniden adlandırıldı). Dosya adının düzeltilmesi kurucuya
// bırakıldı — davranış üzerinde etkisi yoktur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Belge (sertifika PDF'i, ürün görseli, fotoğraf, ek) — VERİTABANINDA saklanır.
///
/// ⚠ NEDEN DİSK DEĞİL: projede dosya depolama yok; dışa aktarım "diske yazılmaz"
/// ilkesini koydu; yedek betiği yalnız tabloları doğrular — diskteki dosya yedeğin
/// dışında kalır ve kiracı izolasyonu ayrıca kurulmak zorunda kalırdı. DB'de
/// aynı global filtre, aynı yedek. İçerik türü BEYANA GÜVENİLMEZ: karar magic
/// byte'a göre verilir (<see cref="Modules.Urun.BelgeKapisi"/>).
/// Silme yok — <see cref="Aktif"/> ile pasife alınır (tüketici ispatı korunur).
/// </summary>
[Table("belgeler")]
public class Belge : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Kapalı liste — <see cref="BelgeSahipTuru"/>. Fail-closed doğrulanır.</summary>
    [Column("sahip_turu"), MaxLength(20)] public string SahipTuru { get; set; } = BelgeSahipTuru.Urun;

    /// <summary>Sahip kaydın kimliği. Sahip AYNI KİRACIDA var olmalıdır; yoksa 404.</summary>
    [Column("sahip_id"), MaxLength(36)] public string SahipId { get; set; } = null!;

    /// <summary>
    /// ⚠ ESKİ KOLON — değeri <see cref="SahipId"/>'ye taşındı (paket 4 geri doldurma).
    /// Faz A'da DÜŞÜRÜLMEDİ: geri-doldurma testi "eski urun_id'li satır" fikstürünü
    /// ancak bu kolon dururken kurabilir (Aşkın'ın koşulu) ve Down böylece geri
    /// dönülemez veri üretmez. Sonraki bir migration'da düşecek. Yeni kod YAZMAZ.
    /// </summary>
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }

    /// <summary>Kapalı liste — <see cref="BelgeTuru"/>.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = BelgeTuru.Sertifika;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("mime"), MaxLength(60)] public string Mime { get; set; } = "application/pdf";
    [Column("boyut")] public int Boyut { get; set; }
    [Column("sha256"), MaxLength(64)] public string Sha256 { get; set; } = null!;
    [Column("icerik", TypeName = "longblob")] public byte[] Icerik { get; set; } = null!;
    [Column("yukleyen_kullanici_id"), MaxLength(36)] public string YukleyenKullaniciId { get; set; } = null!;
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>PDF tavanı (ham bayt) — yapılandırma varsayılanıyla AYNI değer (ADR-0005).</summary>
    public const int EnFazlaPdfBayt = 5 * 1024 * 1024;

    /// <summary>JPEG/PNG tavanı (ham bayt) — yapılandırma varsayılanıyla AYNI değer.</summary>
    public const int EnFazlaGorselBayt = 2 * 1024 * 1024;
}

/// <summary>Belgenin bağlandığı kayıt türü — KAPALI liste (fail-closed).</summary>
public static class BelgeSahipTuru
{
    public const string Urun = "URUN";
    public const string Siparis = "SIPARIS";
    public const string GiderPusulasi = "GIDER_PUSULASI";
    public const string Isletme = "ISLETME";
    /// <summary>D1-12 (18.10): hurda alımının fotoğrafı — sahip HURDA_ALIS türündeki defter satırıdır.</summary>
    public const string HurdaAlis = "HURDA_ALIS";
    /// <summary>D1-13: kimlik tespit formunun imzası — sahip MASAK kimlik tespiti kaydı.</summary>
    public const string KimlikTespit = "KIMLIK_TESPIT";
    /// <summary>D1-13: garanti kartının imzası — sahip SATIS türündeki defter satırı.</summary>
    public const string Satis = "SATIS";

    public static readonly string[] Tumu = [Urun, Siparis, GiderPusulasi, Isletme, HurdaAlis, KimlikTespit, Satis];
}

/// <summary>Belge türü — KAPALI liste (fail-closed).</summary>
public static class BelgeTuru
{
    public const string Sertifika = "SERTIFIKA";
    public const string Gorsel = "GORSEL";
    public const string Fotograf = "FOTOGRAF";
    public const string Ek = "EK";
    /// <summary>D1-13 (18.10): ekranda atılan imza — yalnız PNG (tuval çıktısı).</summary>
    public const string Imza = "IMZA";

    public static readonly string[] Tumu = [Sertifika, Gorsel, Fotograf, Ek, Imza];
}
