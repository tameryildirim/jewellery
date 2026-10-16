using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// İŞLETME KURULUŞU VE AÇILIŞ BAKİYELERİ — paket T-2 Faz A
//
// ⚠ TEK YAZMA YOLU: açılış deftere YALNIZ HareketFabrikasi.Ekle üzerinden,
//   tur="ACILIS" ile düşer. Bu iki tablo DEFTER DEĞİLDİR; deftere yazılanın
//   İZİDİR: "bu bakiye nereden geldi" sorusunun cevabı ve idempotans kapısı.
//
// ⚠ FAIL-CLOSED 1 DİLİ (Bertan'ın birleşik koşulu, 2026-09-16):
//   Sabitler.HareketTurleri dizisine "ACILIS" eklenmesi KOD İÇİNDE ZORLANMAZ
//   (Entities.cs'teki kendi yorumu: "Bu dizi kodda ZORLANMAZ"). O ekleme BELGE
//   değeridir. GERÇEK kapılar şunlardır ve ikisi de ölçülür:
//     (a) denetim.sh — Modules/Kurulum/ altında Hareketler.Add / Cariler.Add /
//         Urunler.Add sayacı 0'dır (ikinci defter yolu açılamaz),
//     (b) DuzeltmePolitikasiTestleri — Sabitler.HareketTurleri ile
//         DuzeltmePolitikasi.HareketTurleri TAM örtüşür (çift yönlü).
//
// ⚠ ACILIS mi CARI_DUZELTME mi (mühür 2 ↔ PAKET-47:75 çelişkisi): ACILIS
//   seçildi ve ikinci yazma yolu AÇILMADI — CariServisi.Duzeltme'ye "tur"
//   parametresi geçirildi. Böylece bakiye satırı, kredi limiti, döviz bacağı
//   ve denetim izi TEK kod yolundan geçer.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Açılış kaleminin hedef türü.</summary>
public static class AcilisTuru
{
    public const string Kasa = "KASA";
    public const string Banka = "BANKA";
    public const string Cari = "CARI";

    /// <summary>Faz B (paket T-2 Faz B) — şema bugünden hazır, motoru orada açılır.</summary>
    public const string Stok = "STOK";

    public static readonly string[] Tumu = [Kasa, Banka, Cari, Stok];

    /// <summary>Faz A'da uygulanabilen türler; STOK bilinçli olarak DIŞARIDADIR.</summary>
    public static readonly string[] FazA = [Kasa, Banka, Cari];
}

/// <summary>Uygulanan bir açılış kaleminin durumu.</summary>
public static class AcilisKalemDurumu
{
    public const string Uygulandi = "UYGULANDI";
    public const string GeriAlindi = "GERI_ALINDI";

    public static readonly string[] Tumu = [Uygulandi, GeriAlindi];
}

/// <summary>Sihirbazın adımları — durum bayraklarının ANAHTAR kümesi.</summary>
public static class KurulumAdimlari
{
    public const string Isletme = "ISLETME";
    public const string Kullanicilar = "KULLANICILAR";
    public const string Kur = "KUR";
    public const string KasaBanka = "KASA_BANKA";
    public const string CariAcilis = "CARI_ACILIS";
    public const string StokAcilis = "STOK_ACILIS";

    /// <summary>Mühür için ZORUNLU adımlar. STOK_ACILIS zorunlu DEĞİLDİR (Faz B).</summary>
    public static readonly string[] Zorunlu = [Isletme, Kullanicilar, Kur, KasaBanka];

    public static readonly string[] Tumu =
        [Isletme, Kullanicilar, Kur, KasaBanka, CariAcilis, StokAcilis];
}

/// <summary>
/// Kiracı başına TEK satır: kurulum nerede kaldı, mühürlendi mi, kim mühürledi.
/// </summary>
[Table("kurulum_durumu")]
public class KurulumDurumu : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;

    /// <summary>UNIQUE — kiracı başına tek satır (veritabanı düzeyinde zorlanır).</summary>
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>
    /// Kullanıcının ATLADIĞINI söylediği adımlar (virgülle ayrık anahtar listesi).
    /// Bitmiş adımlar buradan değil, GERÇEK VERİDEN türetilir (UX-S21): şube
    /// sayısı, kullanıcı sayısı, kasa sayısı… "bitti" bayrağı elle yazılsaydı
    /// veriyle çelişebilirdi.
    /// </summary>
    [Column("atlanan_adimlar"), MaxLength(255)] public string? AtlananAdimlar { get; set; }

    [Column("muhurlendi")] public bool Muhurlendi { get; set; }
    [Column("muhur_tarihi")] public DateTime? MuhurTarihi { get; set; }
    [Column("muhurleyen_kullanici_id"), MaxLength(36)] public string? MuhurleyenKullaniciId { get; set; }

    /// <summary>Mühür gerekçesi — geri dönülemez işlemin NEDENİ (≥20 karakter).</summary>
    [Column("muhur_gerekcesi"), MaxLength(500)] public string? MuhurGerekcesi { get; set; }

    /// <summary>
    /// Açılış penceresinin başladığı an. Pencere (varsayılan 30 gün, yapılandırmadan)
    /// dolduğunda açılış uçları mühürlenmemiş olsa bile 409 verir (Mehmet koşulu 2:
    /// "mühürsüz kiracıda açılış uçları süresiz açık kalmasın").
    /// </summary>
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Uygulanan HER açılış kaleminin izi: idempotans (aynı hedefe ikinci açılış
/// yok), oturum geri alma ve "bu bakiye nereden geldi" sorusunun cevabı.
/// </summary>
[Table("acilis_kalemleri")]
public class AcilisKalemi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Bir "uygula" çağrısının tüm kalemlerini bağlayan yerel kimlik.</summary>
    [Column("oturum_id"), MaxLength(36)] public string OturumId { get; set; } = null!;

    /// <summary>KASA | BANKA | CARI | STOK.</summary>
    [Column("tur"), MaxLength(10)] public string Tur { get; set; } = null!;

    /// <summary>Açılışı yapılan kaydın kimliği (kasa / banka hesabı / cari / ürün).</summary>
    [Column("hedef_id"), MaxLength(36)] public string HedefId { get; set; } = null!;

    /// <summary>
    /// STOK kaleminin şubesi; diğer türlerde null.
    /// <para>
    /// ⚠ Bertan + Mehmet ortak koşulu: aynı ürünün İKİNCİ ŞUBEDEKİ meşru
    /// açılışı reddedilmemelidir. Bu yüzden benzersizlik anahtarı hedef_id
    /// DEĞİL, <see cref="BenzersizAnahtar"/>'dır ve STOK'ta şube boyutunu
    /// içerir.
    /// </para>
    /// </summary>
    [Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }

    /// <summary>
    /// UNIQUE(tenant_id, tur, benzersiz_anahtar) — <see cref="AnahtarUret"/> ile
    /// üretilir. Kolon olarak TUTULUR (hesaplanmış kolon değil): kısıt
    /// veritabanı düzeyinde zorlanmalı ki eşzamanlı iki istek de takılsın.
    /// </summary>
    [Column("benzersiz_anahtar"), MaxLength(80)] public string BenzersizAnahtar { get; set; } = null!;

    /// <summary>Üretilen ACILIS hareketinin kimliği (defter bacağı).</summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>Kasa açılışında üretilen kasa hareketi.</summary>
    [Column("kasa_hareket_id"), MaxLength(36)] public string? KasaHareketId { get; set; }

    /// <summary>Stok açılışında (Faz B) üretilen DEVIR partisi.</summary>
    [Column("parti_id"), MaxLength(36)] public string? PartiId { get; set; }

    [Column("tl_tutar", TypeName = "decimal(15,2)")] public decimal TlTutar { get; set; }
    [Column("has_gram", TypeName = "decimal(15,6)")] public decimal HasGram { get; set; }

    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = AcilisKalemDurumu.Uygulandi;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>
    /// Benzersizlik anahtarı. STOK'ta şube boyutu vardır — aynı ürün iki şubede
    /// meşru olarak açılabilir; aynı şubede İKİNCİ KEZ açılamaz.
    /// </summary>
    public static string AnahtarUret(string tur, string hedefId, string? subeId) =>
        tur == AcilisTuru.Stok
            ? $"{subeId ?? "-"}|{hedefId}"
            : hedefId;
}
