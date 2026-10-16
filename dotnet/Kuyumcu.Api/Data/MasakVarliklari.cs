using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK uyum varlıkları — PLAN Adım 3.
//
// Doğrulanmış kurallar (PLAN §1):
//   • Kimlik eşiği ve üstü          → kimlik tespiti zorunlu (md.5/1-b; işlem gününün
//     yasal_parametreler değeri: 14.01.2023–06.10.2026 185.000, 07.10.2026'dan itibaren
//     370.000 TL — CK 11845, RG 07.10.2026/33393)
//   • 200.000 – 2.000.000 TL        → işlem amacı + min 20 karakter açıklama + kimlik (TEYİTSİZ)
//   • 2.000.001 – 20.000.000 TL     → nakit işlem beyan formu
//   • 20.000.001 TL üstü            → beyan formu + ek belge
//   • Şüpheli işlem                 → tutardan bağımsız, 10 İŞ GÜNÜ içinde bildirim
//   • Belge saklama                 → ~8 yıl, denetime hazır
//
// TASARIM KURALLARI:
//   • APPEND-ONLY. Hiçbir tabloda sert silme yok — 8 yıllık saklama bunu gerektirir.
//     Yanlış kayıt DÜZELTİLMEZ, düzeltme kaydı EKLENİR (Duzelten/DuzeltilenId).
//   • Her tablo IKiraciVarlik — Adım 1'in filtresine ve yazma denetçisine tabi.
//   • Kişisel veri asgari: yalnız MASAK'ın istediği alanlar.
//   • Erişim izi: HassasVarliklar listesinde → okuma da loglanır (KVKK md.12).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Kimlik belgesi türleri (MASAK kimlik tespitinde kabul edilenler).
///
/// ⚠ PAKET UX-4d madde 3: liste artık <see cref="Kuyumcu.Core.Domain.KimlikBelgesi"/>
/// içinde TEK KAYNAK olarak durur; DTO düzenli ifadesi, kural motoru ve ekran
/// seçicisi aynı diziden beslenir. Bu tip yalnızca eski çağrı yüzeyini korur.
/// </summary>
public static class KimlikBelgeTuru
{
    public const string TcKimlik = Kuyumcu.Core.Domain.KimlikBelgesi.TcKimlik;
    public const string Pasaport = Kuyumcu.Core.Domain.KimlikBelgesi.Pasaport;
    public const string SurucuBelgesi = Kuyumcu.Core.Domain.KimlikBelgesi.SurucuBelgesi;
    public const string MaviKart = Kuyumcu.Core.Domain.KimlikBelgesi.MaviKart;
    public const string VergiLevhasi = Kuyumcu.Core.Domain.KimlikBelgesi.VergiLevhasi;

    public static readonly IReadOnlyList<string> Hepsi = Kuyumcu.Core.Domain.KimlikBelgesi.Hepsi;

    public static bool Gecerli(string? t) => Kuyumcu.Core.Domain.KimlikBelgesi.Gecerli(t);
}

/// <summary>
/// MASAK kaydının hangi işlem kaynağından doğduğu (paket 41).
/// <para>
/// ⚠ NEDEN VAR: <c>hareket_id</c> kolonu iki farklı tabloyu işaret edebilir ve
/// ADR-0009 gereği yabancı anahtar yoktur. Kaynak yazılmazsa "bu tespit hangi
/// işleme ait" sorusu denetimde cevaplanamaz. Varsayılan <c>HAREKET</c>'tir;
/// paket 41 öncesi yazılmış tüm satırlar anlamını korur.
/// </para>
/// </summary>
public static class MasakKaynakTuru
{
    /// <summary>hareketler defteri (satış, alış, hurda, takas, işçilik, veresiye, tahsilat…).</summary>
    public const string Hareket = "HAREKET";
    /// <summary>banka_hareketleri — elle girilen dekont (paket 41).</summary>
    public const string BankaHareket = "BANKA_HAREKET";

    public static readonly IReadOnlyList<string> Hepsi = [Hareket, BankaHareket];
}

/// <summary>
/// Bir işlem için yapılan kimlik tespiti. İŞLEME bağlıdır, cariye değil:
/// MASAK "işlem anında tespit" ister, carinin eski kaydı yeterli sayılmaz.
/// </summary>
[Table("masak_kimlik_tespitleri")]
public class MasakKimlikTespiti : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>
    /// Tespitin yapıldığı işlemin kimliği. Null ise işlem henüz kapanmamıştır.
    /// <para>
    /// ⚠ PAKET 41: bu kolon <b>kaynak_tur'e göre</b> ya <c>hareketler</c> ya da
    /// <c>banka_hareketleri</c> satırını işaret eder. ADR-0009 gereği yabancı
    /// anahtar YOKTUR; anlam çakışmasını veritabanı yakalamaz, okuyan kod
    /// <see cref="KaynakTur"/> alanına BAKMAK ZORUNDADIR. Kolonun adını ve
    /// anlamını değiştirmek (yeni kaynak_id açıp bunu boşaltmak) 8 yıl saklanan
    /// append-only bir tabloda geçmiş satırların anlamını değiştirirdi —
    /// denetimde açıklanamaz, bu yüzden REDDEDİLDİ.
    /// </para>
    /// </summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>
    /// <see cref="HareketId"/> hangi tabloyu işaret ediyor (paket 41).
    /// <c>HAREKET</c> → hareketler defteri (varsayılan, tüm eski satırlar),
    /// <c>BANKA_HAREKET</c> → elle girilen banka dekontu.
    /// </summary>
    [Column("kaynak_tur"), MaxLength(20)] public string KaynakTur { get; set; } = MasakKaynakTuru.Hareket;

    /// <summary>Cari kaydı varsa. Null olabilir: kimlik KİŞİNİN kimliğidir, cari kartı şart değil.</summary>
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }

    /// <summary>Tespit anındaki işlem tutarı — eşik kararının dayanağı, sonradan değişmez.</summary>
    [Column("islem_tutari_tl", TypeName = "decimal(15,2)")] public decimal IslemTutariTl { get; set; }

    [Column("belge_turu"), MaxLength(20)] public string BelgeTuru { get; set; } = null!;

    /// <summary>
    /// Belge numarası (TCKN dahil). Veritabanında AES-256-GCM ŞİFRELİ durur
    /// (value converter, KuyumcuDbContext). Bu sütun ARANMAZ — arama için BelgeNoHash.
    /// Uzunluk 200: "v1:" + base64(nonce ‖ şifreli ‖ etiket).
    /// </summary>
    [Column("belge_no"), MaxLength(200)] public string BelgeNo { get; set; } = null!;

    /// <summary>
    /// HMAC-SHA256(normalize(BelgeNo)) — deterministik parmak izi. "Bu TCKN daha
    /// önce işlem yaptı mı" sorusu buradan. MasakHashDinleyicisi kayıt anında doldurur.
    /// </summary>
    [Column("belge_no_hash"), MaxLength(64)] public string BelgeNoHash { get; set; } = null!;

    /// <summary>Şifreleme anahtarı sürümü — rotasyon (Faz B) göç gerektirmesin diye bugünden.</summary>
    [Column("sifre_surumu")] public byte SifreSurumu { get; set; }

    [Column("ad_soyad"), MaxLength(200)] public string AdSoyad { get; set; } = null!;
    [Column("dogum_tarihi")] public DateOnly? DogumTarihi { get; set; }
    [Column("uyruk"), MaxLength(3)] public string? Uyruk { get; set; }        // ISO 3166-1 alpha-3
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }

    /// <summary>Tespiti yapan personel — "kim baktı" sorusunun cevabı.</summary>
    [Column("tespit_eden_kullanici_id"), MaxLength(36)] public string TespitEdenKullaniciId { get; set; } = null!;

    [Column("tarih")] public DateTime Tarih { get; set; }

    /// <summary>Saklama bitişi (tarih + 8 yıl). Süre dolunca maskeleme/arşiv işi buna bakar.</summary>
    [Column("saklama_bitis")] public DateTime SaklamaBitis { get; set; }

    /// <summary>Bu kayıt bir düzeltme ise, düzelttiği kaydın kimliği (append-only).</summary>
    [Column("duzeltilen_id"), MaxLength(36)] public string? DuzeltilenId { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Beyan kademeleri — tutara göre hangi belge/açıklama gerekiyor.</summary>
public static class BeyanKademesi
{
    /// <summary>Kimlik isteyen en düşük tutarın altı — yükümlülük yok (07.10.2026'dan itibaren 200.000 altı; öncesi 185.000 altı).</summary>
    public const string Yok = "YOK";
    /// <summary>Kimlik eşiği – 199.999,99 → yalnız kimlik. 06.10.2026'ya dek 185.000–199.999,99;
    /// 07.10.2026'dan itibaren kimlik eşiği (370.000) amaç beyanı eşiğinin üstünde olduğundan BU KADEME OLUŞMAZ.</summary>
    public const string YalnizKimlik = "YALNIZ_KIMLIK";
    /// <summary>200.000 – 2.000.000 → amaç + min 20 karakter açıklama.</summary>
    public const string AmacBeyani = "AMAC_BEYANI";
    /// <summary>2.000.001 – 20.000.000 → nakit işlem beyan formu.</summary>
    public const string BeyanFormu = "BEYAN_FORMU";
    /// <summary>20.000.001+ → beyan formu + ek belge.</summary>
    public const string BeyanFormuVeBelge = "BEYAN_FORMU_VE_BELGE";

    public static readonly IReadOnlyList<string> Hepsi =
        [Yok, YalnizKimlik, AmacBeyani, BeyanFormu, BeyanFormuVeBelge];
}

/// <summary>Eşik üstü işlem için alınan beyan.</summary>
[Table("masak_beyanlari")]
public class MasakBeyani : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    [Column("kimlik_tespiti_id"), MaxLength(36)] public string KimlikTespitiId { get; set; } = null!;

    /// <summary>
    /// ⚠ PAKET 41: kimlik tespitindekiyle aynı sözleşme — <see cref="KaynakTur"/>
    /// hangi tabloyu işaret ettiğini söyler (hareketler / banka_hareketleri).
    /// </summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>
    /// Kaynak tablo (paket 41) — simetri için beyanda da tutulur. Tespit kaynak
    /// taşıyıp beyan taşımasaydı beyanın hangi işleme ait olduğu yalnız
    /// kimlik_tespiti_id üzerinden DOLAYLI çözülürdü.
    /// </summary>
    [Column("kaynak_tur"), MaxLength(20)] public string KaynakTur { get; set; } = MasakKaynakTuru.Hareket;

    [Column("islem_tutari_tl", TypeName = "decimal(15,2)")] public decimal IslemTutariTl { get; set; }

    /// <summary>Hangi kademeye girdi — tutar sonradan değişse bile karar kaydı kalır.</summary>
    [Column("kademe"), MaxLength(30)] public string Kademe { get; set; } = null!;

    /// <summary>İşlemin amacı (yatırım, hediye, ticari…).</summary>
    [Column("islem_amaci"), MaxLength(100)] public string IslemAmaci { get; set; } = null!;

    /// <summary>Serbest açıklama — AMAC_BEYANI ve üstünde en az 20 karakter.</summary>
    [Column("aciklama"), MaxLength(2000)] public string Aciklama { get; set; } = null!;

    /// <summary>Fonun kaynağı (BEYAN_FORMU ve üstünde beklenir).</summary>
    [Column("fon_kaynagi"), MaxLength(300)] public string? FonKaynagi { get; set; }

    /// <summary>Ek belge referansı (20 M üstü).</summary>
    [Column("ek_belge_referansi"), MaxLength(300)] public string? EkBelgeReferansi { get; set; }

    [Column("alan_kullanici_id"), MaxLength(36)] public string AlanKullaniciId { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("saklama_bitis")] public DateTime SaklamaBitis { get; set; }
    [Column("duzeltilen_id"), MaxLength(36)] public string? DuzeltilenId { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Şüpheli işlem bildiriminin yaşam döngüsü.</summary>
public static class SupheliDurum
{
    /// <summary>İşaretlendi, henüz gönderilmedi — 10 iş günü sayacı işliyor.</summary>
    public const string Acik = "ACIK";
    public const string Gonderildi = "GONDERILDI";
    /// <summary>İnceleme sonucu şüphe kalkmış; kayıt SİLİNMEZ, kapatılır.</summary>
    public const string Kapatildi = "KAPATILDI";

    public static readonly IReadOnlyList<string> Hepsi = [Acik, Gonderildi, Kapatildi];
}

/// <summary>
/// Şüpheli işlem bildirimi. Tutardan BAĞIMSIZ — kimlik eşiği altı bir işlem de
/// şüpheli olabilir. Yasal süre: tespitten itibaren 10 İŞ GÜNÜ.
/// </summary>
[Table("masak_supheli_bildirimler")]
public class MasakSupheliBildirim : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("kimlik_tespiti_id"), MaxLength(36)] public string? KimlikTespitiId { get; set; }

    [Column("islem_tutari_tl", TypeName = "decimal(15,2)")] public decimal? IslemTutariTl { get; set; }

    /// <summary>
    /// Şüphenin gerekçesi — MASAK ŞİB formunun (G) Açıklama alanına gider.
    /// Kuyumcu ŞİB Rehberi 2.0 §4.2.7: ASGARİ 300, AZAMİ 50.000 karakter; ekte değil
    /// doğrudan bu alanda; her bildirim için ÖZGÜN metin (şablon tekrarı yasak).
    /// </summary>
    [Column("gerekce", TypeName = "text")] public string Gerekce { get; set; } = null!;

    /// <summary>Şüphenin tespit edildiği an — 10 iş günü buradan sayılır.</summary>
    [Column("tespit_tarihi")] public DateTime TespitTarihi { get; set; }

    /// <summary>Hesaplanmış son bildirim tarihi (10 iş günü, resmî tatiller hariç).</summary>
    [Column("son_bildirim_tarihi")] public DateTime SonBildirimTarihi { get; set; }

    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = SupheliDurum.Acik;

    [Column("gonderim_tarihi")] public DateTime? GonderimTarihi { get; set; }
    /// <summary>MASAK'tan dönen referans numarası.</summary>
    [Column("gonderim_referansi"), MaxLength(100)] public string? GonderimReferansi { get; set; }

    /// <summary>Kapatıldıysa gerekçesi. Kayıt silinmez.</summary>
    [Column("kapatma_gerekcesi"), MaxLength(1000)] public string? KapatmaGerekcesi { get; set; }

    /// <summary>
    /// PAKET P-13 (B-12) — EK BEYAN BAĞI. Bu kayıt, MASAK'a GÖNDERİLMİŞ bir
    /// bildirimin düzeltmesi/ekiyse, düzelttiği bildirimin kimliği.
    /// <para>
    /// ⚠ Gönderilmiş bildirim GERİ ALINMAZ ve ÜZERİNE YAZILMAZ (append-only,
    /// 8 yıl saklama). Yanlış ya da eksik gönderimin tek düzeltme yolu, aslına
    /// BAĞLI yeni bir bildirim (ek beyan) göndermektir; ikisi de defterde durur.
    /// </para>
    /// </summary>
    [Column("duzeltilen_bildirim_id"), MaxLength(36)] public string? DuzeltilenBildirimId { get; set; }

    [Column("bildiren_kullanici_id"), MaxLength(36)] public string BildirenKullaniciId { get; set; } = null!;
    [Column("saklama_bitis")] public DateTime SaklamaBitis { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK PROFİLİ TARİHÇESİ.
//
// APPEND-ONLY: her hesaplama ve her elle müdahale YENİ SATIRdır. Güncel profil
// en son `olusturuldu` satırıdır. Üzerine yazılsaydı "o gün hangi skorla işlem
// yaptın" sorusu denetimde cevapsız kalırdı. Kural yalnız yorumda DEĞİL,
// KuyumcuDbContext.RiskProfiliniKoru() içinde KODA DÖKÜLMÜŞTÜR.
//
// ⚠ KİŞİSEL VERİ YOK: `sebepler` yalnız KOD SABİTİ taşır (PEP_BEYANI,
//   NAKIT_AGIRLIK, …). Belge no / TCKN / uyruk / adres bu tabloya yazılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Profil satırının nereden doğduğu.</summary>
public static class RiskKaynagi
{
    /// <summary>Motorun hesabı.</summary>
    public const string Hesap = "HESAP";
    /// <summary>Kullanıcının gerekçeli elle ezmesi.</summary>
    public const string Elle = "ELLE";

    public static readonly IReadOnlyList<string> Hepsi = [Hesap, Elle];
}

/// <summary>Risk seviyesi bandı. Bandın SINIRI kodda değil yapılandırmadadır.</summary>
public static class RiskSeviyesi
{
    public const string Dusuk = "DUSUK";
    public const string Orta = "ORTA";
    public const string Yuksek = "YUKSEK";

    public static readonly IReadOnlyList<string> Hepsi = [Dusuk, Orta, Yuksek];

    public static bool Gecerli(string? s) => s is not null && Hepsi.Contains(s);
}

/// <summary>Bir carinin bir andaki risk profili. APPEND-ONLY tarihçe satırı.</summary>
[Table("cari_risk_profilleri")]
public class CariRiskProfili : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;

    /// <summary><see cref="RiskKaynagi"/> — HESAP ya da ELLE.</summary>
    [Column("kaynak"), MaxLength(10)] public string Kaynak { get; set; } = RiskKaynagi.Hesap;

    [Column("puan")] public int Puan { get; set; }

    /// <summary><see cref="RiskSeviyesi"/>.</summary>
    [Column("seviye"), MaxLength(10)] public string Seviye { get; set; } = RiskSeviyesi.Dusuk;

    /// <summary>Motorun ürettiği gerekçe KODLARI — JSON dizi. Kişisel veri İÇERMEZ.</summary>
    [Column("sebepler", TypeName = "text")] public string Sebepler { get; set; } = "[]";

    /// <summary>ELLE ezmede ZORUNLU ve en az yapılandırmadaki karakter sayısı kadar.</summary>
    [Column("gerekce"), MaxLength(500)] public string? Gerekce { get; set; }

    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("hesaplanma_tarihi")] public DateTime HesaplanmaTarihi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>
/// APPEND-ONLY İHLALİ — risk profili satırı değiştirilemez.
/// <c>DenetimIziDegistirilemez</c> ve <c>SiparisGecisiDegistirilemez</c> ile aynı desen.
/// </summary>
public sealed class RiskProfiliDegistirilemez(Microsoft.EntityFrameworkCore.EntityState durum)
    : InvalidOperationException(
        $"Risk profili append-only'dir: {nameof(CariRiskProfili)} üzerinde " +
        $"{(durum == Microsoft.EntityFrameworkCore.EntityState.Deleted ? "SİLME" : "GÜNCELLEME")} yapılamaz. " +
        "Düzeltme yolu: yeniden hesapla ya da gerekçeli elle ezme (yeni satır).");
