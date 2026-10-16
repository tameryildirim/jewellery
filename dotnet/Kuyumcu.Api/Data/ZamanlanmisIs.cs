using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 6 — BİLDİRİM + ZAMANLANMIŞ İŞ ÇATISI (Faz A).
//
// ⚠ Entities.cs'e DOKUNULMADI (GunKapanisi.cs deseni): paket 1/38 aynı dosyayı
//   değiştiriyor; iki tabloyu ayrı dosyada tutmak yaz-seri çakışmasını keser.
//
// ⚠ is_calismalari APPEND-ONLY DEĞİLDİR — "ekle + BİR KEZ tamamla"dır:
//   satır BASLADI ile eklenir (hak talebi/claim; eşzamanlılığı veritabanı
//   UNIQUE indeksiyle kapatmanın tek yolu budur), bitişte bitis/durum/sayaçlar
//   TEK seferde yazılır ve bir daha DEĞİŞMEZ. Bu tablo ADR-0004 denetim izi
//   DEĞİLDİR; denetim izi hâlâ denetim_kayitlari'dır ve gerçekten append-only'dur.
//   (Bertan'ın koşulu: bu not koda da düşsün ki sonraki oturum karıştırmasın.)
//
// ⚠ bildirimler SİLİNMEZ. "Kapatma" yalnız bir damgadır (kapatildi +
//   kapatan_kullanici_id + kapatma_notu). Remove/ExecuteDelete yoktur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İş çalışmasının durumu.</summary>
public static class IsDurum
{
    public const string Basladi = "BASLADI";
    public const string Basarili = "BASARILI";
    public const string Hatali = "HATALI";

    public static readonly IReadOnlyList<string> Hepsi = [Basladi, Basarili, Hatali];
}

/// <summary>Bildirimin önem düzeyi — ekranda rozet rengi, listede sıralama anahtarı.</summary>
public static class BildirimOnem
{
    public const string Bilgi = "BILGI";
    public const string Uyari = "UYARI";
    public const string Kritik = "KRITIK";

    public static readonly IReadOnlyList<string> Hepsi = [Kritik, Uyari, Bilgi];

    /// <summary>Sıralama ağırlığı — KRITIK en üstte.</summary>
    public static int Agirlik(string onem) => onem switch
    {
        Kritik => 0, Uyari => 1, _ => 2,
    };
}

/// <summary>
/// Bir zamanlanmış işin TEK bir koşumu. (tenant_id, is_kodu, calisma_anahtari)
/// benzersizdir: aynı yerel gün ikinci kez koşulamaz ve bu kararı VERİTABANI
/// verir — uygulama "önce SELECT et" yapmaz (yarış koşulu kapalı).
/// </summary>
[Table("is_calismalari")]
public class IsCalismasi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    [Column("is_kodu"), MaxLength(40)] public string IsKodu { get; set; } = null!;

    /// <summary>
    /// Yerel gün ("2026-09-16") ya da elle koşum anahtarı ("2026-09-16-E1435").
    /// YEREL gündür: İstanbul'da 00:30'da koşan tur ile 23:30'daki aynı güne düşer.
    /// </summary>
    [Column("calisma_anahtari"), MaxLength(40)] public string CalismaAnahtari { get; set; } = null!;

    [Column("baslangic")] public DateTime Baslangic { get; set; }
    [Column("bitis")] public DateTime? Bitis { get; set; }

    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = IsDurum.Basladi;

    [Column("uretilen_bildirim")] public int UretilenBildirim { get; set; }
    [Column("incelenen_kayit")] public int IncelenenKayit { get; set; }
    [Column("sure_ms")] public int? SureMs { get; set; }

    /// <summary>
    /// SINIFLANDIRILMIŞ hata özeti (≤500). Ham istisna mesajı YAZILMAZ:
    /// bağlantı dizesi, sır ve kişisel veri buraya sızabilirdi (Mehmet'in koşulu).
    /// </summary>
    [Column("hata_ozeti"), MaxLength(500)] public string? HataOzeti { get; set; }

    /// <summary>Elle tetiklendiyse tetikleyen kullanıcı; zamanlayıcı turunda null.</summary>
    [Column("tetikleyen_kullanici_id"), MaxLength(36)] public string? TetikleyenKullaniciId { get; set; }
}

/// <summary>
/// Kullanıcıya görünen hatırlatma. (tenant_id, tekillik_anahtari) benzersizdir:
/// iş günde 96 kez koşsa da kullanıcı TEK satır görür.
///
/// ⚠ KİŞİSEL VERİ TAŞIMAZ: başlık/gövde TCKN, belge no, ad-soyad içermez
/// (IsSenaryoTestleri zorlar). Kişiye ulaşmak için hedef_rota izlenir.
/// </summary>
[Table("bildirimler")]
public class Bildirim : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Üreten işin kodu (VADE_HATIRLATMA, MASAK_SIB_SURESI, …).</summary>
    [Column("tur"), MaxLength(40)] public string Tur { get; set; } = null!;

    [Column("onem"), MaxLength(10)] public string Onem { get; set; } = BildirimOnem.Bilgi;

    [Column("baslik"), MaxLength(200)] public string Baslik { get; set; } = null!;
    [Column("govde"), MaxLength(1000)] public string Govde { get; set; } = null!;

    /// <summary>Tıklanınca gidilecek ekran yolu — uygulama içi, dış bağlantı değil.</summary>
    [Column("hedef_rota"), MaxLength(200)] public string? HedefRota { get; set; }

    /// <summary>
    /// Virgülle ayrılmış rol listesi. BOŞ ise modül yetkisi olan herkes görür.
    /// Süzme SUNUCUDA yapılır — ekran gizlemesi kapı sayılmaz.
    /// </summary>
    [Column("gorunur_roller"), MaxLength(100)] public string GorunurRoller { get; set; } = "";

    [Column("ilgili_varlik_turu"), MaxLength(30)] public string? IlgiliVarlikTuru { get; set; }
    [Column("ilgili_varlik_id"), MaxLength(36)] public string? IlgiliVarlikId { get; set; }

    /// <summary>Tekrarsızlık anahtarı — kiracı içinde BENZERSİZ (veritabanı zorlar).</summary>
    [Column("tekillik_anahtari"), MaxLength(160)] public string TekillikAnahtari { get; set; } = null!;

    [Column("is_calismasi_id"), MaxLength(36)] public string? IsCalismasiId { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    [Column("kapatildi")] public DateTime? Kapatildi { get; set; }
    [Column("kapatan_kullanici_id"), MaxLength(36)] public string? KapatanKullaniciId { get; set; }
    [Column("kapatma_notu"), MaxLength(500)] public string? KapatmaNotu { get; set; }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-11 (B-24) — ZAMANLANMIŞ İŞ AYARLARI.
//
// ⚠ MIGRATION YOK (plan: "Migration: HAYIR"). Ayarlar MEVCUT `kiraci_ayarlari`
//   anahtar/değer tablosunda durur; o tablo IKiraciVarlik'tir, global kiracı
//   filtresinden, yazma denetçisinden ve denetim izinden geçer. Yeni tablo
//   açmak bu üç mekanizmayı yeniden kurmak demekti.
//
// ⚠ AYAR YALNIZ DARALTIR (fail-closed): yapılandırmada (appsettings) KAPALI bir
//   iş kiracı ayarıyla AÇILAMAZ. Kiracı yalnız açık bir işi kapatabilir.
//   Gerekçe: Isler:*:Etkin kurulum/mevzuat kapısıdır (ör. TevsikKurtarma
//   dayanağı DOĞRULANMADIĞI için KAPALI); ekrandan delinemez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Zamanlanmış iş ayarlarının kiraci_ayarlari anahtarları — sihirli dize yazılmaz.</summary>
public static class IsAyarAnahtari
{
    /// <summary>Zamanlanmış turun işletme saatinden ÖNCE koşmaması için alt sınır (0-23).</summary>
    public const string BaslangicSaat = "is_baslangic_saat";

    /// <summary>Vade hatırlatma ufku (gün) — Isler:Vade:UfukGun değerini EZER.</summary>
    public const string VadeUfukGun = "is_vade_ufuk_gun";

    /// <summary>MASAK ŞİB süre uyarı eşiği (gün) — Isler:Masak:UyariGun değerini EZER.</summary>
    public const string MasakUyariGun = "is_masak_uyari_gun";

    /// <summary>Kiracının KAPATTIĞI iş kodları — "VADE_HATIRLATMA,MASAK_SIB_SURESI".</summary>
    public const string KapaliIsler = "is_kapali_isler";

    public static readonly string[] Tumu =
        [BaslangicSaat, VadeUfukGun, MasakUyariGun, KapaliIsler];

    /// <summary>Eşiklerin ve saatin kabul aralığı — servis ve ekran AYNI diziden okur.</summary>
    public const int SaatEnAz = 0, SaatEnCok = 23;
    public const int VadeEnAz = 1, VadeEnCok = 365;
    public const int MasakEnAz = 0, MasakEnCok = 60;
}

/// <summary>
/// Bir kiracının çözülmüş iş ayarları (yapılandırma varsayılanı + kiracı ezmesi).
/// Varlık DEĞİLDİR: tabloya yazılmaz, her okumada kiraci_ayarlari'ndan kurulur.
/// </summary>
public sealed record IsAyarlari(
    int BaslangicSaat,
    int VadeUfukGun,
    int MasakUyariGun,
    IReadOnlyList<string> KapaliIsler);
