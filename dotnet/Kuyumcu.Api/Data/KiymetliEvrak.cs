using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// KIYMETLİ EVRAK — müşteriden ALINAN çek / senet portföyü (paket 29 Faz A).
///
/// ⚠ NEDEN VAR: perakende kuyumcu veresiye bakiyesini kapatmak için müşteriden
/// çek ya da senet alır. Bu varlık sistemde hiç görünmüyordu: ya cari bakiye
/// elle düzeltiliyor ya da borç açık kalıyordu. Evrak artık ayrı bir kayıt
/// olarak durur, vadeye göre takvime dizilir ve karşılıksız çıktığında borcu
/// geri doğurur.
///
/// ⚠ BU TABLO BAKİYENİN KAYNAĞI DEĞİLDİR — PORTFÖYÜN AYNASIDIR (K4-B).
/// Cari bakiyesini YALNIZ <c>hareketler</c> defteri değiştirir
/// (<see cref="HareketFabrikasi"/> tek kapıdır) ve bakiye yalnız
/// <c>CariModulu.BakiyeGuncelle</c> ile yazılır. Buradaki tutar bir İDDİA
/// değil, o defter satırının portföydeki karşılığıdır. Bu tablodan bakiye
/// TÜRETİLMEZ; iki kaynak olduğu an ikisi ayrışır ve hangisinin doğru olduğu
/// bir daha bilinemez (VadeKapisi.cs'deki "defter tek doğruluk kaynağıdır"
/// kararının aynısı).
///
/// ⚠ SATIR SİLİNMEZ. Yaşam döngüsü yalnız <see cref="Durum"/> geçişleriyle
/// yürür; geçiş kapısı <c>EvrakDurumKapisi.Dogrula</c>'dır. Yanlış girilen bir
/// evrakın geri alınma yolu karşılıksız işaretlemek ya da storno'dur.
/// </summary>
[Table("kiymetli_evraklar")]
public class KiymetliEvrak : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    /// <summary>CEK | SENET — <see cref="EvrakTuru"/>.</summary>
    [Column("evrak_turu"), MaxLength(10)] public string EvrakTuru { get; set; } = null!;

    /// <summary>ALINAN | VERILEN. Faz A'da yalnız ALINAN yazılır (VERİLEN evrak Faz B).</summary>
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = EvrakYonu.Alinan;

    /// <summary><see cref="EvrakDurumu"/> sabitlerinden biri.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = EvrakDurumu.Portfoyde;

    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }

    /// <summary>Faz A'da TRY dışı REDDEDİLİR (K4-G — döviz evrak kapsam dışı).</summary>
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";

    /// <summary>
    /// Vade GÜNÜ — yerel (Europe/Istanbul) takvim günü.
    /// ⚠ UTC damga DEĞİL: <c>Hareket.VadeTarihi</c> ile aynı K5 gerekçesi —
    /// 00:00–03:00 arasındaki bir damga UTC'de bir önceki güne düşer ve vade
    /// bir gün erken görünürdü.
    /// </summary>
    [Column("vade_gunu")] public DateOnly VadeGunu { get; set; }

    [Column("evrak_no"), MaxLength(40)] public string EvrakNo { get; set; } = null!;
    [Column("banka_adi"), MaxLength(100)] public string? BankaAdi { get; set; }

    /// <summary>
    /// ÇİFT GİRİŞ AYIRT EDİCİSİ — asla null (Bertan'ın birleşik koşulu).
    ///
    /// ⚠ NEDEN AYRI KOLON: benzersizlik (tenant, tür, banka, evrakNo) üzerinden
    /// kurulacaktı; ama MySQL'de UNIQUE indeks birden çok NULL'a izin verir ve
    /// SENET'te <see cref="BankaAdi"/> NULL'dur — aynı senet ikinci kez
    /// girilebilirdi ve veritabanı bunu YAKALAMAZDI. Banka adının kendisine
    /// "SENET" yazmak ekranda yanlış bilgi gösterirdi; ayırt edici buraya
    /// yazılır: <c>BankaAdi ?? "SENET"</c>. Elle kontrol (EvrakServisi) bu
    /// indeksin TOCTOU boşluğuna karşı İKİNCİ katman olarak kalır.
    /// </summary>
    [Column("banka_ayirt_edici"), MaxLength(100)] public string BankaAyirtEdici { get; set; } = null!;

    [Column("banka_sube"), MaxLength(100)] public string? BankaSube { get; set; }
    [Column("keside_yeri"), MaxLength(100)] public string? KesideYeri { get; set; }
    [Column("keside_tarihi")] public DateOnly? KesideTarihi { get; set; }

    /// <summary>Evrakın BORÇLUSU (keşideci) — carilerde kayıtlıysa.</summary>
    [Column("borclu_cari_id"), MaxLength(36)] public string? BorcluCariId { get; set; }
    [Column("borclu_ad"), MaxLength(200)] public string? BorcluAd { get; set; }

    /// <summary>
    /// Borçlunun TCKN/VKN — DİSKE ŞİFRELİ İNER (ADR-0003).
    /// Converter <c>KuyumcuDbContext.OnModelCreating</c>'de kurulur; bellekte
    /// düz metin görünür, veritabanında değil.
    /// </summary>
    [Column("borclu_kimlik_no"), MaxLength(255)] public string? BorcluKimlikNo { get; set; }

    /// <summary>
    /// <see cref="BorcluKimlikNo"/>'nun HMAC'i — ARAMA YALNIZ BUNUN ÜZERİNDEN.
    /// Şifreli sütunda eşitlik araması yapılamaz. Doldurmayı
    /// <c>MasakHashDinleyicisi</c> üstlenir (unutulamaz olsun diye).
    /// </summary>
    [Column("borclu_kimlik_no_hash"), MaxLength(64)] public string? BorcluKimlikNoHash { get; set; }

    /// <summary>Alan şifreleme sürüm damgası (anahtar döndürmede gerekir).</summary>
    [Column("sifre_surumu")] public int SifreSurumu { get; set; }

    /// <summary>Evrakı BİZE VEREN cari — borcu kapanan cari budur.</summary>
    [Column("kaynak_cari_id"), MaxLength(36)] public string KaynakCariId { get; set; } = null!;

    /// <summary>Borcu kapatan EVRAK_GIRIS defter satırı.</summary>
    [Column("giris_hareket_id"), MaxLength(36)] public string GirisHareketId { get; set; } = null!;

    [Column("tahsile_veren_banka_hesap_id"), MaxLength(36)] public string? TahsileVerenBankaHesapId { get; set; }
    [Column("tahsile_verme_tarihi")] public DateTime? TahsileVermeTarihi { get; set; }

    [Column("tahsil_tarihi")] public DateTime? TahsilTarihi { get; set; }
    [Column("tahsil_kasa_id"), MaxLength(36)] public string? TahsilKasaId { get; set; }
    [Column("tahsil_banka_hesap_id"), MaxLength(36)] public string? TahsilBankaHesapId { get; set; }
    [Column("tahsil_kasa_hareket_id"), MaxLength(36)] public string? TahsilKasaHareketId { get; set; }
    [Column("tahsil_banka_hareket_id"), MaxLength(36)] public string? TahsilBankaHareketId { get; set; }

    [Column("karsiliksiz_tarihi")] public DateTime? KarsiliksizTarihi { get; set; }
    [Column("karsiliksiz_hareket_id"), MaxLength(36)] public string? KarsiliksizHareketId { get; set; }
    [Column("karsiliksiz_aciklama"), MaxLength(500)] public string? KarsiliksizAciklama { get; set; }

    /// <summary>
    /// CİRO (PAKET P-21 / Y-21 — Faz B AÇILDI): evrakın ÜÇÜNCÜ BİR CARİYE
    /// devredildiği kayıt. Ciro bir ÖDEMEDİR: karşı tarafa olan borcumuz kapanır.
    /// </summary>
    [Column("ciro_cari_id"), MaxLength(36)] public string? CiroCariId { get; set; }
    [Column("ciro_tarihi")] public DateTime? CiroTarihi { get; set; }
    [Column("ciro_hareket_id"), MaxLength(36)] public string? CiroHareketId { get; set; }

    // ── PAKET P-21 (Y-21) — RÜCU ALANLARI ───────────────────────────────────
    //
    // ⚠ NEDEN AYRI KOLONLAR, NEDEN KARSILIKSIZ_* YETMEZ: ciro edilen bir evrak
    // karşılıksız çıktığında İKİ AYRI borç doğar ve ikisi farklı taraflaradır:
    //   (1) evrakı BİZE veren carinin borcu geri doğar (kaynak cari) —
    //       mevcut karsiliksiz_* alanları bunu taşır;
    //   (2) evrakı BİZDEN alan cariye (ciro ettiğimiz taraf) borcumuz geri
    //       doğar: TTK md.724 anlamında bize RÜCU edilir.
    // Tek alan kümesiyle izlenseydi ikinci borç kaydı hiç görünmezdi — ciro
    // ettiğimiz kuyumcuya olan borcumuz defterde yok sayılırdı.
    //
    // ⚠ TERMİNAL DEĞİLDİR (plan DoD): rücu edilen evrak KARSILIKSIZ durumuna
    // gelir ve oradan TAHSIL_EDILDI'ye geçebilir (EvrakGecisleri). "Ciro ettim,
    // benden çıktı" diye kapatılsaydı geri dönen evrak sisteme giremezdi.

    /// <summary>Rücu (ciro edilen evrakın bize geri dönmesi) tarihi.</summary>
    [Column("rucu_tarihi")] public DateTime? RucuTarihi { get; set; }

    /// <summary>Ciro ettiğimiz cariye olan borcumuzu GERİ DOĞURAN defter satırı.</summary>
    [Column("rucu_hareket_id"), MaxLength(36)] public string? RucuHareketId { get; set; }

    /// <summary>Rücu ile geri doğan tutar (kısmi rücu bu sürümde YOKTUR: daima tam tutar).</summary>
    [Column("rucu_tutar", TypeName = "decimal(15,2)")] public decimal? RucuTutar { get; set; }

    [Column("notlar"), MaxLength(500)] public string? Notlar { get; set; }
    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>Evrak türü — tek kaynak (UX-S31).</summary>
public static class EvrakTuru
{
    public const string Cek = "CEK";
    public const string Senet = "SENET";
    public static readonly string[] Tumu = [Cek, Senet];
}

/// <summary>Evrak yönü. Faz A yalnız ALINAN üretir.</summary>
public static class EvrakYonu
{
    public const string Alinan = "ALINAN";
    public const string Verilen = "VERILEN";
    public static readonly string[] Tumu = [Alinan, Verilen];
}

/// <summary>Evrak durumları — tek kaynak (UX-S31).</summary>
public static class EvrakDurumu
{
    public const string Portfoyde = "PORTFOYDE";
    public const string TahsileVerildi = "TAHSILE_VERILDI";
    public const string TahsilEdildi = "TAHSIL_EDILDI";
    public const string Karsiliksiz = "KARSILIKSIZ";
    public const string CiroEdildi = "CIRO_EDILDI";

    public static readonly string[] Tumu =
        [Portfoyde, TahsileVerildi, TahsilEdildi, Karsiliksiz, CiroEdildi];
}

/// <summary>
/// İZİNLİ DURUM GEÇİŞLERİ — tek tablo, tek kaynak.
///
/// ⚠ Bu sözlük dışında hiçbir yerde geçiş kontrolü YAZILMAZ. Geçiş kuralını
/// çağrı noktalarına serpiştirmek <c>GunKapanisiKapisi</c>'ndeki aynı gerekçeyle
/// reddedildi: "biri unutulur". Kapı <c>EvrakDurumKapisi.Dogrula</c>'dır.
/// </summary>
public static class EvrakGecisleri
{
    public static readonly IReadOnlyDictionary<string, string[]> Izinli =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [EvrakDurumu.Portfoyde] =
                [EvrakDurumu.TahsileVerildi, EvrakDurumu.TahsilEdildi,
                 EvrakDurumu.Karsiliksiz, EvrakDurumu.CiroEdildi],
            // Banka evrakı iade edebilir (PORTFOYDE'ye dönüş) ya da tahsil eder.
            [EvrakDurumu.TahsileVerildi] =
                [EvrakDurumu.Portfoyde, EvrakDurumu.TahsilEdildi, EvrakDurumu.Karsiliksiz],
            // Ciro edilen evrak bizde değildir; yalnız karşılıksız haberi gelir (rücu).
            [EvrakDurumu.CiroEdildi] = [EvrakDurumu.Karsiliksiz],
            // Karşılıksız çıkan evrak sonradan ödenebilir.
            [EvrakDurumu.Karsiliksiz] = [EvrakDurumu.TahsilEdildi],
            // SON DURUM — hiçbir geçişi yoktur.
            [EvrakDurumu.TahsilEdildi] = [],
        };
}
