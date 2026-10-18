using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Sistem;

// ═════════════════════════════════════════════════════════════════════════════
// SİSTEM YÖNETİMİ MODÜLÜ — paket F-2-min Faz A.
//
// "Bedava sınırsız kiracı açma" kapatıldı: kiracı yalnız burada üretilen TEK
// KULLANIMLIK bir davetle doğar. Yönetim JWT ile DEĞİL, yapılandırma sırrıyla
// yapılır (MÜHÜR 4 / K1) — 'SISTEM' rolü açılmadı.
//
// ⚠ FAZ B (Blazor yönetici konsolu /sistem/kiracilar) BU DALGADA YOK (K5):
//   dalga 6'ya, T-4b yanına alındı. Pilot davetleri curl + X-Sistem-Anahtari
//   ile üretilir; runbook docs/SISTEM-DAVET-RUNBOOK.md (K6).
//
// ⚠ /api/v1/sistem/* uçları 45-API (MFA) tamamlanmadan ÜRETİME AÇILMAZ (K4);
//   bu bir TAKVİM kapısıdır. Teknik koruma: T-1 nginx şablonunda IP kısıtı.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

/// <summary>POST /sistem/davetler gövdesi. Hiçbir alan zorunlu değildir; boş gövde de geçerli bir davet üretir.</summary>
public sealed class DavetUretDto
{
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }
    [MaxLength(200)] public string? IsletmeAdi { get; set; }

    /// <summary>1-90 gün. Boşsa yapılandırmadaki Sistem:VarsayilanDenemeGun.</summary>
    [Range(1, 90)] public int? DenemeGun { get; set; }

    /// <summary>1-720 saat. Boşsa yapılandırmadaki Sistem:DavetGecerlilikSaat.</summary>
    [Range(1, 720)] public int? GecerlilikSaat { get; set; }

    [MaxLength(200)] public string? Not { get; set; }

    /// <summary>
    /// NEDEN — en az 20 karakter (MÜHÜR 4 / K3). Denetim izine bu yazılır;
    /// yeni kiracı açma yetkisi üreten ayrıcalıklı eylem gerekçesiz yapılamaz.
    /// </summary>
    [Required(ErrorMessage = "Gerekçe zorunludur")]
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    [MaxLength(500)]
    public string Gerekce { get; set; } = null!;
}

/// <summary>
/// PATCH /sistem/davetler/{id} gövdesi — ÜÇ DURUMLU (UX-S3 · mevcut
/// <c>…Gonderildi</c> deseni; yeni desen AÇILMAZ): alan gövdede YOK → korunur ·
/// null → silinir · dolu → yazılır.
/// </summary>
public sealed class DavetGuncelleDto
{
    private bool? _iptal;
    private string? _not;

    [JsonIgnore] public bool IptalGonderildi { get; private set; }
    [JsonIgnore] public bool NotGonderildi { get; private set; }

    /// <summary>true = daveti iptal et · false = iptali geri al (DoD: geri dönüş yolu).</summary>
    public bool? Iptal { get => _iptal; set { _iptal = value; IptalGonderildi = true; } }

    [MaxLength(200)]
    public string? Not { get => _not; set { _not = value; NotGonderildi = true; } }

    /// <summary>
    /// NEDEN — en az 20 karakter (MÜHÜR 4 / K3). Denetim izine bu yazılır;
    /// yeni kiracı açma yetkisi üreten ayrıcalıklı eylem gerekçesiz yapılamaz.
    /// </summary>
    [Required(ErrorMessage = "Gerekçe zorunludur")]
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    [MaxLength(500)]
    public string Gerekce { get; set; } = null!;
}

/// <summary>PATCH /sistem/kiracilar/{id} gövdesi — süre uzatma / durum değişimi.</summary>
public sealed class KiraciGuncelleDto
{
    private DateTime? _denemeBitis;
    private string? _durum;
    private bool? _aktif;

    [JsonIgnore] public bool DenemeBitisGonderildi { get; private set; }
    [JsonIgnore] public bool DurumGonderildi { get; private set; }
    [JsonIgnore] public bool AktifGonderildi { get; private set; }

    /// <summary>Yeni deneme bitişi (UTC). Gönderilirse İLERİ tarihli olmalı; null = süre kısıtını kaldır.</summary>
    public DateTime? DenemeBitis
    {
        get => _denemeBitis;
        set { _denemeBitis = value; DenemeBitisGonderildi = true; }
    }

    /// <summary>DENEME · AKTIF · SALT_OKUNUR.</summary>
    [RegularExpression("DENEME|AKTIF|SALT_OKUNUR", ErrorMessage = "Geçersiz kiracı durumu")]
    public string? Durum { get => _durum; set { _durum = value; DurumGonderildi = true; } }

    /// <summary>Kiracıyı pasife alır/aktifleştirir. Kiracı SİLİNMEZ (DoD: pasife alma).</summary>
    public bool? Aktif { get => _aktif; set { _aktif = value; AktifGonderildi = true; } }

    /// <summary>NEDEN — en az 20 karakter. Denetim izine bu yazılır.</summary>
    [Required(ErrorMessage = "Gerekçe zorunludur")]
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    public string Gerekce { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// Sistem yönetimi servisi. Kiracı-ÜSTÜ çalışır: davetler kiracı-öncesi bir
/// tablodadır, kiracı listesi ise TÜM kiracıları görür — bu yüzden
/// <see cref="SistemKapsami.Sistem"/> kapsamı kullanılır (SistemKapsami.cs
/// izin listesi 7-8. maddeler).
/// </summary>
public sealed class SistemServisi(
    SistemKapsami kapsam, SistemAyarlari ayarlar, KiraciDurumOnbellegi onbellek,
    IHttpContextAccessor erisim)
{
    // ── Davetler ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Tek kullanımlık davet üretir. HAM JETON YALNIZ BU YANITTA bir kez döner;
    /// veritabanına SHA-256 özeti yazılır, loga/denetim kaydına hiç yazılmaz.
    /// </summary>
    public async Task<object> DavetUret(DavetUretDto dto, string operatorAdi)
    {
        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        OperatorVeGerekceDogrula(operatorAdi, dto.Gerekce);
        var gun = dto.DenemeGun ?? ayarlar.VarsayilanDenemeGun;
        if (gun is < 1 or > 90)
            throw new BadRequestException("Deneme süresi 1-90 gün arasında olmalıdır");
        var saat = dto.GecerlilikSaat ?? ayarlar.DavetGecerlilikSaat;
        if (saat is < 1 or > 720)
            throw new BadRequestException("Davet geçerliliği 1-720 saat arasında olmalıdır");

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var now = DateTime.UtcNow;
        var jeton = JetonUret();
        var kayit = new KiraciDaveti
        {
            Id = Kimlik.Ulid(),
            JetonHash = Ozet(jeton),
            Email = Bosa(dto.Email),
            IsletmeAdi = Bosa(dto.IsletmeAdi),
            DenemeGun = gun,
            Gecerlilik = now.AddHours(saat),
            KullanildiMi = false,
            Iptal = false,
            OlusturanNot = Bosa(dto.Not),
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Davetler.Add(kayit);

        // Denetim izi (K3) — davet üretimi YENİ KİRACI AÇMA yetkisi doğuran
        // ayrıcalıklı eylemdir; kim/neden izi olmadan yapılamaz. Otomatik yol
        // KAPALIDIR: KiraciDaveti IKiraciVarlik değildir (kiracı-öncesi tablo),
        // DenetimYazmaDinleyicisi onu kiracısız diye atlar. ⚠ HAM JETON ve
        // sistem anahtarının DEĞERİ ize YAZILMAZ.
        SistemIziYaz(sistem.ServiceProvider, SistemTenantId, DavetDenetimVarligi, kayit.Id,
            DenetimIslemiDavetUret, operatorAdi, dto.Gerekce,
            $"deneme: {gun} gün · geçerlilik: {saat} saat");

        await db.SaveChangesAsync();

        return new
        {
            id = kayit.Id,
            jeton,                       // ← BİR KEZ. Bir daha gösterilemez.
            jetonUyarisi = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu jeton bir daha gösterilmez; güvenli bir kanalla iletin."),
            denemeGun = kayit.DenemeGun,
            gecerlilik = kayit.Gecerlilik,
            email = MaskeliEposta(kayit.Email),
            isletmeAdi = kayit.IsletmeAdi,
            durum = DavetDurumlari.Coz(kayit, now),
            olusturuldu = kayit.Olusturuldu,
        };
    }

    /// <summary>
    /// Davet listesi. JETON HASH'İ DÖNMEZ; e-posta MASKELENİR (Mehmet koşulu —
    /// yönetici konsolu PII deposu değildir, davetin kime gittiği maskeli
    /// biçimde de ayırt edilebilir).
    /// </summary>
    public async Task<object> DavetListe(int limit = 200)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var tavan = Math.Clamp(limit, 1, 500);
        var now = DateTime.UtcNow;
        var satirlar = await db.Davetler.AsNoTracking()
            .OrderByDescending(d => d.Olusturuldu)
            .Take(tavan)
            .ToListAsync();

        return new
        {
            sinir = tavan,
            sinirDegdi = satirlar.Count == tavan,
            adet = satirlar.Count,
            kayitlar = satirlar.Select(d => new
            {
                id = d.Id,
                email = MaskeliEposta(d.Email),
                isletmeAdi = d.IsletmeAdi,
                denemeGun = d.DenemeGun,
                gecerlilik = d.Gecerlilik,
                durum = DavetDurumlari.Coz(d, now),
                kullananTenantId = d.KullananTenantId,
                not = d.OlusturanNot,
                olusturuldu = d.Olusturuldu,
            }).ToList(),
        };
    }

    /// <summary>Daveti iptal eder / iptalini geri alır (UX-S3 üç durumlu PATCH).</summary>
    public async Task<object> DavetGuncelle(string id, DavetGuncelleDto dto, string operatorAdi)
    {
        OperatorVeGerekceDogrula(operatorAdi, dto.Gerekce);

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var davet = await db.Davetler.FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Davet bulunamadı");

        if (davet.KullanildiMi)
            throw new BadRequestException(
                "Kullanılmış davet değiştirilemez; açılan kiracı /sistem/kiracilar ucundan yönetilir");

        var degisen = new List<string>();
        if (dto.IptalGonderildi && dto.Iptal is not null)
        {
            davet.Iptal = dto.Iptal.Value;
            degisen.Add(dto.Iptal.Value ? "iptal" : "iptal_geri_alindi");
        }
        if (dto.NotGonderildi)
        {
            davet.OlusturanNot = Bosa(dto.Not);
            degisen.Add("not");
        }

        var now = DateTime.UtcNow;
        davet.Guncellendi = now;

        // Denetim izi (K3): daveti KİM iptal etti / geri aldı, neden.
        SistemIziYaz(sistem.ServiceProvider, SistemTenantId, DavetDenetimVarligi, davet.Id,
            DenetimIslemiDavetGuncelle, operatorAdi, dto.Gerekce,
            $"alanlar: {(degisen.Count == 0 ? "-" : string.Join(",", degisen))}"
            + $" · yeni durum: {DavetDurumlari.Coz(davet, now)}");

        await db.SaveChangesAsync();

        return new
        {
            id = davet.Id,
            durum = DavetDurumlari.Coz(davet, now),
            not = davet.OlusturanNot,
            gecerlilik = davet.Gecerlilik,
            denemeGun = davet.DenemeGun,
            email = MaskeliEposta(davet.Email),
            isletmeAdi = davet.IsletmeAdi,
            operator_ = operatorAdi,
        };
    }

    // ── Kiracılar ────────────────────────────────────────────────────────────

    /// <summary>Tüm kiracılar: durum, kalan gün, kullanıcı sayısı, son giriş.</summary>
    public async Task<object> KiraciListe()
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var now = DateTime.UtcNow;
        var kiracilar = await db.Tenants.AsNoTracking()
            .OrderByDescending(t => t.Olusturuldu)
            .Select(t => new
            {
                t.Id, t.Ad, t.Plan, t.Durum, t.DenemeBitis, t.Aktif, t.DavetId, t.Olusturuldu,
            })
            .ToListAsync();

        var kimlikler = kiracilar.Select(t => t.Id).ToList();
        var kullanicilar = await db.Kullanicilar.AsNoTracking()
            .Where(k => kimlikler.Contains(k.TenantId))
            .GroupBy(k => k.TenantId)
            .Select(g => new { TenantId = g.Key, Adet = g.Count(), SonGiris = g.Max(x => x.SonGiris) })
            .ToListAsync();
        var harita = kullanicilar.ToDictionary(x => x.TenantId);

        return new
        {
            adet = kiracilar.Count,
            kayitlar = kiracilar.Select(t => new
            {
                id = t.Id,
                ad = t.Ad,
                plan = t.Plan,
                durum = t.Durum,
                denemeBitis = t.DenemeBitis,
                kalanGun = KalanGun(t.DenemeBitis, now),
                yazabilirMi = new KiraciDurumu(t.Durum, t.DenemeBitis).YazabilirMi(now),
                aktif = t.Aktif,
                davetIdVarMi = t.DavetId is not null,
                kullaniciSayisi = harita.TryGetValue(t.Id, out var u) ? u.Adet : 0,
                sonGiris = harita.TryGetValue(t.Id, out var v) ? v.SonGiris : null,
                olusturuldu = t.Olusturuldu,
            }).ToList(),
        };
    }

    /// <summary>
    /// Süre uzatma / durum değiştirme / pasife alma. GEREKÇE ZORUNLU (≥20
    /// karakter) ve her değişiklik append-only denetim kaydı üretir.
    /// Önbellek AÇIKÇA boşaltılır — uzatma gecikmeli etki etmesin.
    /// </summary>
    public async Task<object> KiraciGuncelle(string id, KiraciGuncelleDto dto, string operatorAdi)
    {
        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        OperatorVeGerekceDogrula(operatorAdi, dto.Gerekce);
        if (dto.DurumGonderildi && !KiraciDurumlari.Gecerli(dto.Durum))
            throw new BadRequestException("Geçersiz kiracı durumu: DENEME, AKTIF ya da SALT_OKUNUR olmalıdır");

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var kiraci = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("Kiracı bulunamadı");

        var now = DateTime.UtcNow;
        var degisen = new List<string>();

        if (dto.DenemeBitisGonderildi)
        {
            if (dto.DenemeBitis is not null && dto.DenemeBitis.Value <= now)
                throw new BadRequestException("Deneme bitişi ileri bir tarih olmalıdır");
            if (dto.DenemeBitis != kiraci.DenemeBitis)
            {
                kiraci.DenemeBitis = dto.DenemeBitis;
                degisen.Add("deneme_bitis");
            }
        }

        if (dto.DurumGonderildi && dto.Durum != kiraci.Durum)
        {
            kiraci.Durum = dto.Durum!;
            degisen.Add("durum");
        }

        if (dto.AktifGonderildi && dto.Aktif is not null && dto.Aktif.Value != kiraci.Aktif)
        {
            kiraci.Aktif = dto.Aktif.Value;
            degisen.Add("aktif");
        }

        if (degisen.Count == 0)
            throw new BadRequestException("Değiştirilecek alan gönderilmedi");

        kiraci.Guncellendi = now;

        // Denetim izi (K3): kim · neden · hangi alanlar · istek kimliği · IP.
        // Sistem anahtarının DEĞERİ yazılmaz. Servis yeniden yazılmaz — mevcut
        // DenetimServisi kullanılır.
        SistemIziYaz(sistem.ServiceProvider, kiraci.Id, DenetimVarligi, kiraci.Id,
            DenetimIslemi, operatorAdi, dto.Gerekce,
            $"alanlar: {string.Join(",", degisen)}");

        await db.SaveChangesAsync();
        onbellek.Bosalt(kiraci.Id);

        return new
        {
            id = kiraci.Id,
            ad = kiraci.Ad,
            durum = kiraci.Durum,
            denemeBitis = kiraci.DenemeBitis,
            kalanGun = KalanGun(kiraci.DenemeBitis, now),
            aktif = kiraci.Aktif,
            yazabilirMi = new KiraciDurumu(kiraci.Durum, kiraci.DenemeBitis).YazabilirMi(now),
            degisenAlanlar = degisen,
        };
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>Denetim kaydının varlık türü — mevcut DenetimKaydi.VarlikTuru kolonuna yazılır.</summary>
    public const string DenetimVarligi = "Tenant";

    /// <summary>
    /// Denetim kaydı detayının İLK sözcüğü. Denetim tablosunda ayrı bir "islem"
    /// kolonu YOKTUR (eylem = EKLEME/GUNCELLEME...); işlemin adı detayın başına
    /// yazılır, böylece SQL ile süzülebilir: detay LIKE 'SURE_UZATMA%'.
    /// </summary>
    public const string DenetimIslemi = "SURE_UZATMA";

    /// <summary>Davet kayıtlarının denetim izindeki varlık türü.</summary>
    public const string DavetDenetimVarligi = "KiraciDaveti";

    /// <summary>Davet üretiminin iz damgası — SQL süzgeci: detay LIKE 'DAVET_URETIMI%'.</summary>
    public const string DenetimIslemiDavetUret = "DAVET_URETIMI";

    /// <summary>Davet iptali/geri alınmasının iz damgası.</summary>
    public const string DenetimIslemiDavetGuncelle = "DAVET_GUNCELLEME";

    /// <summary>
    /// KİRACI-ÜSTÜ iz satırlarının kiracı damgası. Davet KİRACI-ÖNCESİ bir
    /// kayıttır: onu üreten/iptal eden işlemin bağlanacağı bir tenants satırı
    /// YOKTUR ve uydurulmaz (Anayasa md.1). Çakışma riski yok: gerçek kiracı
    /// kimlikleri ULID'dir (26 karakter) — "SISTEM" hiçbirine eşit olamaz.
    /// ⚠ Bu satırlar hiçbir kiracının denetim ekranında GÖRÜNMEZ (global
    /// kiracı filtresi); yalnız sistem kapsamından okunur.
    /// </summary>
    public const string SistemTenantId = "SISTEM";

    /// <summary>
    /// Operatör + gerekçe kuralı (MÜHÜR 4 / K3) — TEK YER. DataAnnotations
    /// yalnız controller yolunda koşar; servis doğrudan çağrıldığında da kural
    /// geçerlidir.
    /// </summary>
    private static void OperatorVeGerekceDogrula(string operatorAdi, string? gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce) || gerekce.Trim().Length < 20)
            throw new BadRequestException("Gerekçe en az 20 karakter olmalıdır");
        if (string.IsNullOrWhiteSpace(operatorAdi)
            || operatorAdi.Trim().Length < SistemAnahtariKapisi.OperatorAsgariUzunluk)
            throw new BadRequestException($"{SistemAnahtariKapisi.OperatorBasligi} başlığı zorunludur");
    }

    /// <summary>
    /// Sistem uçlarının ORTAK denetim izi (K3): işlem · kim · ne · neden ·
    /// istek kimliği (T-4a gelince X-Istek-Kimligi buraya bağlanır). IP ve
    /// kullanıcı-aracısı damgasını DenetimServisi.Olustur ekler.
    /// <para>⚠ SaveChanges ÇAĞIRMAZ: çağıranın tek transaction'ı ile yazılır —
    /// iş kaydı yazılıp iz yazılamazsa ikisi birden geri alınır.</para>
    /// <para>⚠ Ham jeton ve sistem anahtarının DEĞERİ buraya hiçbir koşulda
    /// yazılmaz.</para>
    /// </summary>
    private void SistemIziYaz(
        IServiceProvider saglayici, string tenantId, string varlikTuru, string? varlikId,
        string islem, string operatorAdi, string gerekce, string? ek = null)
    {
        var ctx = erisim.HttpContext;
        var denetim = saglayici.GetRequiredService<DenetimServisi>();
        denetim.SistemYazmaKaydet(
            tenantId, varlikTuru, varlikId,
            $"{islem} · operatör: {operatorAdi.Trim()}"
            + (string.IsNullOrEmpty(ek) ? "" : $" · {ek}")
            + $" · gerekçe: {gerekce.Trim()}"
            + $" · istek: {ctx?.TraceIdentifier ?? "-"}");
    }

    public static int? KalanGun(DateTime? bitis, DateTime now) =>
        bitis is null ? null : (int)Math.Ceiling((bitis.Value - now).TotalDays);

    private static string? Bosa(string? d) => string.IsNullOrWhiteSpace(d) ? null : d.Trim();

    /// <summary>256 bit entropi, URL güvenli. Ham jeton yalnız üretim yanıtında yaşar.</summary>
    public static string JetonUret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>SHA-256 hex — DisaAktarim jeton özetiyle aynı yöntem.</summary>
    public static string Ozet(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    /// <summary>a***@e***.com — davetin kime gittiği ayırt edilir, adres okunmaz (KVKK).</summary>
    public static string? MaskeliEposta(string? e)
    {
        if (string.IsNullOrWhiteSpace(e)) return null;
        var at = e.IndexOf('@');
        if (at <= 0 || at == e.Length - 1) return "***";
        var ad = e[..at];
        var alan = e[(at + 1)..];
        var nokta = alan.LastIndexOf('.');
        var uzanti = nokta > 0 ? alan[nokta..] : "";
        return $"{ad[0]}***@{alan[0]}***{uzanti}";
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/sistem/*</c> — JWT ile DEĞİL, sistem anahtarıyla korunur.
/// <para>
/// ⚠ [AllowAnonymous]: bu uçların kimliği <see cref="SistemAnahtariKapisi"/>'dır.
/// Geçerli bir SAHIP token'ı bu uçları AÇMAZ (test: SAHIP_Jwt_Ile_Sistem_Ucuna_Erisilemez).
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[TypeFilter(typeof(SistemAnahtariKapisi))]
[Route("api/v1/sistem")]
public sealed class SistemController(SistemServisi servis) : ControllerBase
{
    private string Operator =>
        SistemAnahtariKapisi.OperatorCoz(Request.Headers[SistemAnahtariKapisi.OperatorBasligi].ToString());

    /// <summary>POST /api/v1/sistem/davetler — tek kullanımlık davet üretir.</summary>
    [HttpPost("davetler")]
    [EnableRateLimiting(OranSiniri.SistemDavet)]
    public async Task<IActionResult> DavetUret([FromBody] DavetUretDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.DavetUret(dto, Operator));

    /// <summary>GET /api/v1/sistem/davetler — davet listesi (jeton hash'i DÖNMEZ).</summary>
    [HttpGet("davetler")]
    public Task<object> DavetListe([FromQuery] int limit = 200) => servis.DavetListe(limit);

    /// <summary>PATCH /api/v1/sistem/davetler/{id} — daveti iptal eder / geri alır.</summary>
    [HttpPatch("davetler/{id}")]
    public Task<object> DavetGuncelle(string id, [FromBody] DavetGuncelleDto dto) =>
        servis.DavetGuncelle(id, dto, Operator);

    /// <summary>GET /api/v1/sistem/kiracilar — kiracı listesi.</summary>
    [HttpGet("kiracilar")]
    public Task<object> KiraciListe() => servis.KiraciListe();

    /// <summary>PATCH /api/v1/sistem/kiracilar/{id} — süre uzatma / durum değişimi.</summary>
    [HttpPatch("kiracilar/{id}")]
    public Task<object> KiraciGuncelle(string id, [FromBody] KiraciGuncelleDto dto) =>
        servis.KiraciGuncelle(id, dto, Operator);

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — POST /api/v1/sistem/yasal-parametreler/onizleme: İKİNCİ ONAY
    /// ADIMININ içeriği (eski → yeni, geçerlilik günü, yön, etkilenecek açık işlem sayısı, onay
    /// kodu). HİÇBİR ŞEY YAZMAZ. Sistem anahtarı ister (SistemAnahtariKapisi); JWT (SAHİP dahil)
    /// ile erişilemez. Kiracı ekranından yazma YOKTUR: ulusal rakamdır, yanlış giriş ceza doğurur.
    /// </summary>
    [HttpPost("yasal-parametreler/onizleme")]
    public Task<object> YasalParametreOnizle(
        [FromBody] Kuyumcu.Api.Common.YasalParametreEkleDto dto,
        [FromServices] Kuyumcu.Api.Common.YasalParametreServisi yasal) =>
        yasal.Onizle(dto, Operator);

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — POST /api/v1/sistem/yasal-parametreler: BUGÜN ya da ileri
    /// tarihli değer (geçmiş 400); Resmî Gazete künyesi + ≥20 karakter gerekçe + önizleme
    /// onay kodu zorunlu. Denetim izi DenetimServisi.SistemYazmaKaydet.
    /// </summary>
    [HttpPost("yasal-parametreler")]
    public async Task<IActionResult> YasalParametreEkle(
        [FromBody] Kuyumcu.Api.Common.YasalParametreEkleDto dto,
        [FromServices] Kuyumcu.Api.Common.YasalParametreServisi yasal) =>
        StatusCode(StatusCodes.Status201Created, await yasal.Ekle(dto, Operator));

    /// <summary>
    /// PAKET YASAL-PARAM-2 — POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme/onizleme: bugünden
    /// geçerli yanlış girişin AYNI GÜN düzeltmesinin onay içeriği. HİÇBİR ŞEY YAZMAZ.
    /// </summary>
    [HttpPost("yasal-parametreler/{id}/duzeltme/onizleme")]
    public Task<object> YasalParametreDuzeltOnizle(
        string id, [FromBody] Kuyumcu.Api.Common.YasalParametreDuzeltDto dto,
        [FromServices] Kuyumcu.Api.Common.YasalParametreServisi yasal) =>
        yasal.DuzeltOnizle(id, dto, Operator);

    /// <summary>
    /// PAKET YASAL-PARAM-2 — POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme: yalnız değeri giren
    /// operatör (başkası 403), gerekçe ≥ 20, önizleme onay kodu; düzeltme anından ÖNCEKİ işlemler değişmez.
    /// </summary>
    [HttpPost("yasal-parametreler/{id}/duzeltme")]
    public async Task<IActionResult> YasalParametreDuzelt(
        string id, [FromBody] Kuyumcu.Api.Common.YasalParametreDuzeltDto dto,
        [FromServices] Kuyumcu.Api.Common.YasalParametreServisi yasal) =>
        StatusCode(StatusCodes.Status201Created, await yasal.Duzelt(id, dto, Operator));

    /// <summary>
    /// PAKET YASAL-3 (§ 4.4) — POST /api/v1/sistem/yaptirim-listesi: resmî yaptırım
    /// (malvarlığı dondurma) listesini ByGatech merkezî günceller (kuyumcu liste
    /// YÜKLEMEZ). JWT ile erişilemez (SistemAnahtariKapisi). Satır silinmez;
    /// kaldırılan karar damgalanır. Yeni satır mevcut bir cariyle TAM eşleşirse o
    /// kiracıda cari kilitlenir (+ açık bakiye/emanet/siparişte 30 günlük görev).
    /// </summary>
    [HttpPost("yaptirim-listesi")]
    public async Task<object> YaptirimListesi(
        [FromBody] Kuyumcu.Api.Modules.Masak.YaptirimListesiDto dto,
        [FromServices] SistemKapsami kapsam)
    {
        if (string.IsNullOrWhiteSpace(Operator) || Operator.Length < SistemAnahtariKapisi.OperatorAsgariUzunluk)
            throw new BadRequestException($"{SistemAnahtariKapisi.OperatorBasligi} başlığı zorunludur");
        await using var k = kapsam.Sistem();
        var sdb = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var denetim = k.ServiceProvider.GetRequiredService<DenetimServisi>();
        return await Kuyumcu.Api.Modules.Masak.YaptirimServisi.ListeGuncelle(sdb, denetim, dto, Operator);
    }

    /// <summary>D1-18 — POST /api/v1/sistem/yaptirim-listesi/bm-cek: BM listesini şimdi çek (günlük turu beklemeden).</summary>
    [HttpPost("yaptirim-listesi/bm-cek")]
    public async Task<object> BmYaptirimCek([FromServices] Kuyumcu.Api.Modules.Masak.BmYaptirimGuncelleyici guncelleyici, CancellationToken iptal)
    {
        if (string.IsNullOrWhiteSpace(Operator) || Operator.Length < SistemAnahtariKapisi.OperatorAsgariUzunluk)
            throw new BadRequestException($"{SistemAnahtariKapisi.OperatorBasligi} başlığı zorunludur");
        return await guncelleyici.Tur(iptal);
    }

    /// <summary>
    /// PAKET YASAL-1 — GET /api/v1/sistem/yasal-parametreler/saglik: önbellek ile
    /// veritabanı aynı mı (satır sayısı). Çok örnekli kurulumda ayrışmayı ölçer.
    /// </summary>
    [HttpGet("yasal-parametreler/saglik")]
    public async Task<object> YasalParametreSaglik(
        [FromServices] Kuyumcu.Api.Common.YasalParametreServisi yasal,
        [FromServices] SistemKapsami kapsam,
        [FromServices] Kuyumcu.Api.Common.TakvimAyarlari takvim)
    {
        var (onbellek, yuklenme, gun) = yasal.OnbellekOlcusu();
        await using var k = kapsam.Sistem();
        var vt = await k.ServiceProvider.GetRequiredService<KuyumcuDbContext>().YasalParametreler.CountAsync();
        // YASAL-1 tur 2: bayram eksikliği Panel bandı yerine OPERATÖRE (ByGatech) burada görünür.
        var yil = yasal.Bugun.Year;
        return new
        {
            onbellekSatir = onbellek, veritabaniSatir = vt, esit = onbellek == vt, yuklenmeUtc = yuklenme, yuklenenGun = gun,
            bayramYili = yil, bayramEksik = yasal.BayramEksik(yil, takvim),
        };
    }
}
