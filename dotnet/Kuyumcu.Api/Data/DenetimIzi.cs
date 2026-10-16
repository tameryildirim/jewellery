using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuyumcu.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// DENETİM İZİ — Adım 3'ün ÖN ŞARTI (Mehmet, 06 Güvenlik).
//
// Gerekçesi (aynen): "MASAK verisi devreye girmeden bu ÖNCE kapanmalı, çünkü
// kişisel veri erişim logu KVKK madde 12 gereğidir ve bu kod tabanında hiç yok."
// Denetim raporundaki T4 teknik borcu da buydu.
//
// İKİ ŞEYİ BİRDEN KAYDEDER:
//   • YAZMA — otomatik, SaveChanges sırasında yakalanır (unutulamaz)
//   • OKUMA — açık çağrı gerektirir; KVKK'nın istediği "kim hangi kimliği
//     GÖRDÜ" sorusunun cevabı yalnız böyle tutulabilir (SELECT'i EF yakalamaz)
//
// APPEND-ONLY: bu tablo güncellenmez, silinmez. Denetim izi değiştirilebiliyorsa
// denetim izi değildir.
// ═════════════════════════════════════════════════════════════════════════════

[Table("denetim_kayitlari")]
public class DenetimKaydi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>İşlemi yapan kullanıcı; sistem işlemlerinde null.</summary>
    [Column("kullanici_id"), MaxLength(36)] public string? KullaniciId { get; set; }

    /// <summary>OKUMA · EKLEME · GUNCELLEME · SILME · REDDEDILDI</summary>
    [Column("eylem"), MaxLength(20)] public string Eylem { get; set; } = null!;

    /// <summary>Varlık türü (Cari, MasakKimlikTespiti…).</summary>
    [Column("varlik_turu"), MaxLength(60)] public string VarlikTuru { get; set; } = null!;

    [Column("varlik_id"), MaxLength(36)] public string? VarlikId { get; set; }

    /// <summary>Hassas mı? KVKK kapsamındaki kişisel veriye dokunan kayıtlar ayrı süzülebilsin.</summary>
    [Column("hassas")] public bool Hassas { get; set; }

    /// <summary>Değişen alan adları ya da okuma gerekçesi. DEĞERLER YAZILMAZ.</summary>
    [Column("detay"), MaxLength(1000)] public string? Detay { get; set; }

    /// <summary>
    /// BAĞLAM DAMGASI — işlemin yapıldığı şube. <c>null</c> = BİLİNMİYOR
    /// (bu kolon eklenmeden önce yazılmış satır, şubesiz sistem işi ya da
    /// şube seçilmemiş oturum). ⚠ "Varsayılan şube" ASLA atanmaz: uydurulmuş
    /// bir damga, damgasızlıktan daha kötüdür (Anayasa md.1).
    /// </summary>
    [Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }

    /// <summary>
    /// BAĞLAM DAMGASI — işlem bir kasaya dokunuyorsa o kasa; değilse <c>null</c>.
    /// </summary>
    [Column("kasa_id"), MaxLength(36)] public string? KasaId { get; set; }

    [Column("ip_adresi"), MaxLength(64)] public string? IpAdresi { get; set; }
    [Column("user_agent"), MaxLength(300)] public string? UserAgent { get; set; }
    [Column("tarih")] public DateTime Tarih { get; set; }
}

public static class DenetimEylemi
{
    public const string Okuma = "OKUMA";
    public const string Ekleme = "EKLEME";
    public const string Guncelleme = "GUNCELLEME";
    public const string Silme = "SILME";
    /// <summary>Kiracı ihlali gibi reddedilen girişimler — saldırı sinyali.</summary>
    public const string Reddedildi = "REDDEDILDI";
}

/// <summary>
/// KVKK kapsamında sayılan, erişimi OKUMA olarak da loglanan varlıklar.
/// Buraya bir tür eklemek, o türün okunmasının da iz bırakacağı anlamına gelir.
/// </summary>
public static class HassasVarliklar
{
    private static readonly HashSet<string> Liste =
    [
        nameof(Cari),           // TCKN, doğum tarihi, adres — Adım 3'te dolacak
        "MasakKimlikTespiti",
        "MasakBeyani",
        "MasakSupheliBildirim",
        // Dışa aktarım kişisel veriyi TOPLU olarak dışarı çıkarır — tek bir kayda
        // bakmaktan daha hassastır (PLAN-2 Adım 4, Mehmet'in şartı 3).
        "DisaAktarim",
        // PAKET S-3b (kurul tur 3 · /denetim-izi, YÜKSEK): gider pusulası detayı
        // ve belgesi TAM TCKN gösterir; bu okumalar KVKK md.12 raporlamasında
        // "kişisel veri erişimi" süzgecinde GÖRÜNMÜYORDU. Tür listeye alındı.
        nameof(GiderPusulasi),
    ];

    public static bool Mi(string varlikTuru) => Liste.Contains(varlikTuru);
}

/// <summary>
/// Okuma izini yazan servis. Yazma izi otomatiktir (<see cref="DenetimYazmaDinleyicisi"/>),
/// okuma izi açık çağrı ister — EF SELECT'leri yakalayamaz.
/// </summary>
public sealed class DenetimServisi(KuyumcuDbContext db, IHttpContextAccessor erisim)
{
    /// <summary>Hassas bir kaydın GÖRÜNTÜLENDİĞİNİ kaydeder (KVKK md.12).</summary>
    public async Task OkumaKaydet(
        AktifKullanici islem, string varlikTuru, string? varlikId, string? gerekce = null)
    {
        db.DenetimKayitlari.Add(Olustur(
            islem.TenantId, islem.Id, DenetimEylemi.Okuma, varlikTuru, varlikId, gerekce, erisim,
            subeId: islem.SubeId));
        await db.SaveChangesAsync();
    }

    /// <summary>Birden çok kaydın listelendiğini tek satırda kaydeder.</summary>
    public async Task ListeOkumaKaydet(
        AktifKullanici islem, string varlikTuru, int adet, string? gerekce = null)
    {
        db.DenetimKayitlari.Add(Olustur(
            islem.TenantId, islem.Id, DenetimEylemi.Okuma, varlikTuru, null,
            $"liste: {adet} kayıt{(gerekce is null ? "" : $" · {gerekce}")}", erisim,
            subeId: islem.SubeId));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// GEREKÇELİ yazma izi — otomatik yazma izinin (<see cref="DenetimYazmaDinleyicisi"/>)
    /// yetmediği işlemler için. Dinleyici Added varlıklarda Detay'ı BOŞ bırakır;
    /// elle bakiye düzeltmesi gibi işlemlerde "NEDEN" sorusu cevapsız kalırdı.
    ///
    /// ⚠ SaveChanges ÇAĞIRMAZ: çağıranın transaction'ı içinde, onun tek
    /// SaveChanges'i ile yazılır. Böylece iş kaydı yazılıp iz yazılamazsa
    /// ikisi birden geri alınır (Mehmet, 06 Güvenlik şartı 3).
    ///
    /// ⚠ <paramref name="detay"/>'a KİŞİSEL VERİ (TCKN, adres, telefon) değil,
    /// kullanıcının yazdığı gerekçe ve sayısal özet girer.
    /// <para><paramref name="eylem"/>: verilmezse EKLEME (geriye uyumlu). Mevcut kaydı
    /// değiştiren işlem (ör. parola sıfırlama) <see cref="DenetimEylemi.Guncelleme"/> verir.</para>
    /// </summary>
    public void YazmaKaydet(
        AktifKullanici islem, string varlikTuru, string? varlikId, string detay, bool hassas = true,
        string? kasaId = null, string? eylem = null)
    {
        var kayit = Olustur(
            islem.TenantId, islem.Id, eylem ?? DenetimEylemi.Ekleme, varlikTuru, varlikId, detay, erisim,
            subeId: islem.SubeId, kasaId: kasaId);
        // Hassas bayrağı burada PARAMETREDEN gelir; varlık türü HassasVarliklar
        // listesinde OLMAYABİLİR (ör. "CariDuzeltme" kişisel veri değil ama
        // denetimde hassas süzgeçte görünmesi gerekir). Türü listeye de eklemek
        // okumaları da izlemeye başlardı — istenen bu değil.
        kayit.Hassas = hassas || kayit.Hassas;
        db.DenetimKayitlari.Add(kayit);
    }

    /// <summary>
    /// KULLANICISIZ sistem işleminin yazma izi — paket F-2-min (MÜHÜR 4 / K3).
    /// <para>
    /// Sistem yönetimi uçlarında JWT yoktur: işlemi yapan bir kullanıcı satırı
    /// değil, sistem anahtarını taşıyan operatördür. <c>kullanici_id</c> bu
    /// yüzden NULL kalır ("sistem işlemlerinde null" — kolon açıklaması) ve
    /// operatörün adı <paramref name="detay"/>'a yazılır.
    /// </para>
    /// <para>
    /// ⚠ Kayıt üretimi YENİDEN YAZILMAZ: aynı <see cref="Olustur"/> fabrikası
    /// kullanılır (IP ve istek damgası oradan gelir). SaveChanges ÇAĞIRMAZ —
    /// çağıranın transaction'ı ile yazılır.
    /// </para>
    /// <para>⚠ Sistem anahtarının DEĞERİ hiçbir koşulda detaya yazılmaz.</para>
    /// </summary>
    public void SistemYazmaKaydet(
        string tenantId, string varlikTuru, string? varlikId, string detay)
    {
        var kayit = Olustur(
            tenantId, kullaniciId: null, DenetimEylemi.Guncelleme, varlikTuru, varlikId,
            detay, erisim);
        kayit.Hassas = true;
        db.DenetimKayitlari.Add(kayit);
    }

    /// <param name="subeId">Bağlam damgası — bilinmiyorsa null (uydurulmaz).</param>
    /// <param name="kasaId">Bağlam damgası — işlem kasaya dokunmuyorsa null.</param>
    /// <summary>
    /// REDDEDİLEN bir girişimin izi (saldırı/hata sinyali). Çağıranın
    /// transaction'ından BAĞIMSIZDIR ve kendi SaveChanges'ini çağırır:
    /// aynı transaction'da yazılsaydı, reddedilen işlem geri alınırken
    /// denemenin izi de kaybolurdu (ADR-0004 ruhu, Mehmet koşulu).
    /// </summary>
    public async Task RedKaydet(
        AktifKullanici islem, string varlikTuru, string? varlikId, string detay)
    {
        var kayit = Olustur(
            islem.TenantId, islem.Id, DenetimEylemi.Reddedildi, varlikTuru, varlikId, detay, erisim,
            subeId: islem.SubeId);
        kayit.Hassas = true;
        db.DenetimKayitlari.Add(kayit);
        await db.SaveChangesAsync();
    }

    public static DenetimKaydi Olustur(
        string tenantId, string? kullaniciId, string eylem, string varlikTuru,
        string? varlikId, string? detay, IHttpContextAccessor? erisim,
        string? subeId = null, string? kasaId = null)
    {
        var ctx = erisim?.HttpContext;
        return new DenetimKaydi
        {
            Id = Common.Kimlik.Ulid(),
            TenantId = tenantId,
            KullaniciId = kullaniciId,
            Eylem = eylem,
            VarlikTuru = varlikTuru,
            VarlikId = varlikId,
            Hassas = HassasVarliklar.Mi(varlikTuru),
            SubeId = subeId,
            KasaId = kasaId,
            Detay = detay is { Length: > 1000 } ? detay[..1000] : detay,
            IpAdresi = ctx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ctx?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua
                ? (ua.Length > 300 ? ua[..300] : ua)
                : null,
            Tarih = DateTime.UtcNow,
        };
    }
}
