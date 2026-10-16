using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 3 — Sipariş çatısı (Faz A).
//
// Perakende kuyumcunun bugün deftere hiç girmeyen üç sözü: vitrinde olmayan malı
// müşteriye söz vermek, DIŞ atölyeye mal ısmarlamak, tamire alınan eşyayı takip
// etmek. Bu üçü bir DURUM MAKİNESİ ve APPEND-ONLY geçiş kaydıyla kayda geçer.
//
// ⚠ DEFTERE YAZMAZ: sipariş modülü HareketFabrikasi.Ekle çağırmaz, hareketler /
//   kasa_hareketleri satırı üretmez, stok gerçeğini değiştirmez. Para ve teslim
//   HÂLÂ mevcut defter uçlarından geçer (denetim.sh "Hareketler.Add = 1" sayacı
//   bu paketle DEĞİŞMEZ).
//
// ⚠ KAPSAM SINIRI (Katman 1 = imalat/atölye KAPSAM DIŞI): kalem tablosunda
//   fire, usta payı, ayar düşürme YOKTUR. "Atölye siparişi" yalnız DIŞ atölyeye
//   "verildi / geldi" takibidir; iş emri değildir.
//
// ⚠ Entities.cs'e DOKUNULMADI (çakışma azaltma — GunKapanisi.cs, UrunKategorisi.cs
//   deseniyle aynı: varlık + kapalı listeler aynı dosyada).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Sipariş başlığı — müşteri siparişi, (dış) atölye siparişi ya da tamir-servis kaydı.
///
/// <para>SİLİNMEZ: hard DELETE ucu YOKTUR. Vazgeçilen sipariş
/// <see cref="SiparisDurumu.Iptal"/> durumuna GEÇİRİLİR (gerekçe zorunlu) ve
/// geçiş kaydı kalır — "neden iptal edildi" sorusu sonradan sorulur.</para>
/// </summary>
[Table("siparisler")]
public class Siparis : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>İnsan okunur numara (S-YYYY-NNNNNN). (tenant_id, no) UNIQUE.</summary>
    [Column("no"), MaxLength(20)] public string No { get; set; } = null!;

    /// <summary>Kapalı liste — <see cref="SiparisTuru"/>. Fail-closed doğrulanır.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = SiparisTuru.MusteriSiparisi;

    /// <summary>Kapalı liste — <see cref="SiparisDurumu"/>. TASLAK doğar.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = SiparisDurumu.Taslak;

    /// <summary>Müşteri ya da (atölye siparişinde) atölye carisi. Aynı kiracıda olmalı.</summary>
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;

    [Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }

    /// <summary>Müşteriye verilen söz. Geçmişse ve sipariş açıksa "geciken" sayılır.</summary>
    [Column("soz_verilen_gun")] public DateOnly? SozVerilenGun { get; set; }

    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>
    /// Söz verilen tahmini tutar. Doluysa <see cref="KurSnapshotId"/> ZORUNLUDUR —
    /// "hangi kurdan söz verildi" sorusu sonradan sorulur.
    /// ⚠ Bu bir TAHMİNDİR; deftere hiçbir etkisi yoktur.
    /// </summary>
    [Column("tahmini_tutar_tl", TypeName = "decimal(14,2)")] public decimal? TahminiTutarTl { get; set; }

    [Column("kur_snapshot_id"), MaxLength(36)] public string? KurSnapshotId { get; set; }

    /// <summary>
    /// TAMIR türünde müşteri eşyası deftere alındıysa EMANET_GIRIS hareketinin kimliği.
    /// ⚠ OPSİYONEL (Bertan/Mehmet koşulu: zorunluluk kararı bekliyor). Boşsa ekran
    /// "bu eşya defterde görünmez" uyarısı basar. Sipariş modülü has bacağına YAZMAZ.
    /// </summary>
    [Column("emanet_hareket_id"), MaxLength(36)] public string? EmanetHareketId { get; set; }

    // ── PAKET P-26 (Y-36) — DIŞ USTA / ATÖLYE CARİSİ ────────────────────────
    /// <summary>
    /// ATOLYE_SIPARISI ve TAMIR türünde işi yapan DIŞ ustanın carisi.
    ///
    /// <para>
    /// ⚠ <b>BU KOLON METAL ÇIKIŞI YAZMAZ ve K-13 mührü gereği YAZMAYACAKTIR.</b>
    /// Kurucu kalemi K-13 (2026-09-21, <c>KARARLAR-URUN-2026-09-21.md</c>)
    /// eritme firesini ve imalat/atölye katmanını 1.0 KAPSAMI DIŞINDA bıraktı.
    /// Ustaya metal çıkışı bir İMALAT hareketidir (has emanet + fire + ayar
    /// düşümü birlikte yürür); yarısını yazmak has-cariyi denk gelmez hâle
    /// getirirdi. Burada tutulan tek şey <b>kimin yaptığıdır</b> — defter
    /// bacağı yoktur. Metal fiilen veriliyorsa mevcut EMANET_CIKIS ucu
    /// kullanılır ve ekranda bu yazar.
    /// </para>
    /// </summary>
    [Column("usta_cari_id"), MaxLength(36)] public string? UstaCariId { get; set; }

    // ── PAKET P-26 (Y-30) — TAMİR KAPORASI DEFTERE BAĞLANIR ─────────────────
    /// <summary>
    /// Alınan kaporanın deftere düşen GERÇEK tahsilat satırı
    /// (<c>CariServisi.TahsilatGirHareket</c> → <c>HareketFabrikasi.Ekle</c>).
    ///
    /// <para>
    /// ⚠ Sipariş modülü kaporayı KENDİ yazmaz: ikinci bir para yolu açılsaydı
    /// kapora kasaya girer ama MASAK/tevsik kümülatifine girmezdi. Burada
    /// yalnız doğan hareketin kimliği saklanır — <b>sipariş bir defter satırı
    /// ÜRETİR ama bir defter satırı DEĞİLDİR</b>.
    /// </para>
    /// <para>NULL = kapora alınmadı. Storno mevcut defter yolundan yürür.</para>
    /// </summary>
    [Column("kapora_hareket_id"), MaxLength(36)] public string? KaporaHareketId { get; set; }

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>
    /// İYİMSER KİLİT damgası (Bertan koşulu). Geçiş/PATCH isteği bu damgayı
    /// gönderirse ve damga eskimişse 409 döner — iki kullanıcı aynı siparişte
    /// aynı anda geçiş yaparsa ikincisi sessizce kazanmaz.
    /// </summary>
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    /// <summary>Liste ucunun azami satır sayısı (TB-19 — sınırsız ToListAsync yasak).</summary>
    public const int AzamiLimit = 200;

    /// <summary>Liste ucunun varsayılan limiti.</summary>
    public const int VarsayilanLimit = 50;

    /// <summary>İptal / geri geçiş gerekçesinin asgari uzunluğu.</summary>
    public const int GerekceAsgari = 10;
}

/// <summary>
/// Sipariş kalemi — stoktan seçilmiş ürün ya da serbest tanım.
///
/// ⚠ KAPSAM: fire / usta payı / ayar düşürme alanı YOKTUR (Katman 1 kapsam dışı).
/// ⚠ Kalem kimliği DIŞARIDAN gelmez: PATCH kalemleri tümüyle değiştirir (sil + yaz).
/// </summary>
[Table("siparis_kalemleri")]
public class SiparisKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("siparis_id"), MaxLength(36)] public string SiparisId { get; set; } = null!;

    /// <summary>Katalogdaki ürün. Boşsa <see cref="Tanim"/> zorunludur.</summary>
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }

    /// <summary>Serbest tanım ("18 ayar tektaş, 0.30 pırlanta"). UrunId boşsa zorunlu.</summary>
    [Column("tanim"), MaxLength(200)] public string? Tanim { get; set; }

    /// <summary>Ayar kodu — verilirse Ayarlar.Bul ile doğrulanır.</summary>
    [Column("ayar_id"), MaxLength(10)] public string? AyarId { get; set; }

    [Column("gram_agirlik", TypeName = "decimal(12,3)")] public decimal? GramAgirlik { get; set; }
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; } = 1m;

    /// <summary>Ekrandaki satır sırası (0'dan).</summary>
    [Column("sira")] public int Sira { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>
/// Sipariş durum geçişi — APPEND-ONLY.
///
/// <para>⚠ KOD TARAFINDA ZORLANIR (Mehmet koşulu): "ADR-0004 ruhu" demek zorlama
/// değildir. <see cref="KuyumcuDbContext"/> SaveChanges sırasında bu varlık için
/// EntityState.Modified / Deleted görürse <see cref="SiparisGecisiDegistirilemez"/>
/// fırlatır — tıpkı denetim izinde olduğu gibi.</para>
///
/// <para>⚠ Faz A'da hareket bağı (teslimin defter karşılığı) YOKTUR; o alan ve
/// doğrulaması Faz B'de kendi migration'ıyla gelir. Kullanılmayan kolon
/// açılmadı.</para>
/// </summary>
[Table("siparis_gecisleri")]
public class SiparisGecisi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("siparis_id"), MaxLength(36)] public string SiparisId { get; set; } = null!;

    /// <summary>İlk (doğuş) satırında NULL — sipariş TASLAK olarak doğar.</summary>
    [Column("onceki_durum"), MaxLength(20)] public string? OncekiDurum { get; set; }

    [Column("yeni_durum"), MaxLength(20)] public string YeniDurum { get; set; } = null!;

    /// <summary>İptal ve geri geçişte ZORUNLU (en az 10 karakter).</summary>
    [Column("gerekce"), MaxLength(500)] public string? Gerekce { get; set; }

    [Column("kullanici_id"), MaxLength(36)] public string? KullaniciId { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Sipariş türü — KAPALI liste (fail-closed).</summary>
public static class SiparisTuru
{
    /// <summary>Vitrinde olmayan malın müşteriye söz verilmesi.</summary>
    public const string MusteriSiparisi = "MUSTERI_SIPARISI";

    /// <summary>DIŞ atölyeye ısmarlama — "verildi / geldi" takibi (iş emri DEĞİL).</summary>
    public const string AtolyeSiparisi = "ATOLYE_SIPARISI";

    /// <summary>Müşteri eşyasının tamir-servis takibi.</summary>
    public const string Tamir = "TAMIR";

    public static readonly string[] Tumu = [MusteriSiparisi, AtolyeSiparisi, Tamir];
}

/// <summary>
/// Sipariş durumu — KAPALI, ALTI durum (fail-closed).
/// ⚠ DURUM ENFLASYONU YASAĞI: "cila bitti" gibi ara durumlar tek tek eklenmez;
/// genişletme yalnız ekip oyuyla ve TEK SEFERDE yapılır.
/// </summary>
public static class SiparisDurumu
{
    public const string Taslak = "TASLAK";
    public const string Onaylandi = "ONAYLANDI";
    public const string Atolyede = "ATOLYEDE";
    public const string Hazir = "HAZIR";
    public const string TeslimEdildi = "TESLIM_EDILDI";
    public const string Iptal = "IPTAL";

    public static readonly string[] Tumu = [Taslak, Onaylandi, Atolyede, Hazir, TeslimEdildi, Iptal];

    /// <summary>Terminal durumlar — hiçbir çıkış yok, hiçbir alan değişmez.</summary>
    public static readonly string[] Terminal = [TeslimEdildi, Iptal];

    /// <summary>Hâlâ açık (geciken sayımına giren) durumlar.</summary>
    public static readonly string[] Acik = [Taslak, Onaylandi, Atolyede, Hazir];
}

/// <summary>
/// APPEND-ONLY İHLALİ — sipariş geçiş kaydı değiştirilemez.
/// Denetim izindeki <c>DenetimIziDegistirilemez</c> ile aynı desen.
/// </summary>
public sealed class SiparisGecisiDegistirilemez(Microsoft.EntityFrameworkCore.EntityState durum)
    : InvalidOperationException(
        $"Sipariş geçiş kaydı append-only'dir: {nameof(SiparisGecisi)} üzerinde " +
        $"{(durum == Microsoft.EntityFrameworkCore.EntityState.Deleted ? "SİLME" : "GÜNCELLEME")} yapılamaz. " +
        "Değiştirilebilen geçiş izi, iz değildir.");
