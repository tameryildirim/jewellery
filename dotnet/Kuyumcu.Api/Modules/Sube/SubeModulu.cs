using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Sube;

// Kaynak: apps/api/src/modules/sube/* — basit şube CRUD'u.
// PAKET PATCH-SOZLESME (02.10): PATCH artık AYRI SubeGuncelleDto ile bağlanır.
// Eski hali (Nest'ten taşınan "PATCH de SubeOlusturDto ile doğrulanır, ad zorunlu")
// yalnız bir alan gönderen istemciye 'Ad zorunludur' 400'ü döndürüyordu (SUBE-KAPSAM-B
// tasarım hakemi bulgusu) — PATCH sözleşmesi ihlali. Sözleşme: gövdede YOKSA KORU,
// null ise SİL; şube adı silinemez → açık null/boş ad 400 (alan altı Türkçe mesaj).

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SubeOlusturDto
{
    private string? _adres;
    private string? _telefon;
    private bool? _aktif;

    [Required, MaxLength(200)]
    public string Ad { get; set; } = null!;

    [MaxLength(500)]
    public string? Adres { get => _adres; set { _adres = value; AdresVerildi = true; } }
    [JsonIgnore] public bool AdresVerildi { get; private set; }

    /// <summary>
    /// Şube telefonu. UX-4d madde 19: "abc" 200 ile kabul ediliyordu — hiçbir
    /// biçim kuralı yoktu. Kural CARİ ile AYNI kaynaktan gelir
    /// (Kuyumcu.Core Iletisim.TelefonDesen); iki ekranda iki kural olmaz (UX-S31).
    /// </summary>
    [MaxLength(20)]
    [Kuyumcu.Api.Common.Dogrulama.Telefon]   // GIRDI-AILESI-2 tur 3: ekranla TEK kural (Core TelefonDogrulayici)
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonVerildi = true; } }
    [JsonIgnore] public bool TelefonVerildi { get; private set; }

    /// <summary>
    /// PAKET S-3b (kurul tur 3 - /isletme): pasife alinan sube arayuzden GERI
    /// ACILAMIYORDU - DELETE vardi, tersi yoktu (UX-S25 ihlali). PATCH sozlesmesi:
    /// govdede YOKSA korunur, gonderilirse uygulanir.
    /// </summary>
    public bool? Aktif { get => _aktif; set { _aktif = value; AktifVerildi = true; } }
    [JsonIgnore] public bool AktifVerildi { get; private set; }
}

/// <summary>
/// PATCH /subeler/{id} gövdesi — KISMİ güncelleme (PAKET PATCH-SOZLESME).
/// Her alanın "gönderildi mi?" bayrağı setter'da kalkar (JSON anahtarının VARLIĞI):
///   • alan YOK            → mevcut değer KORUNUR (zorunlu hatası VERİLMEZ)
///   • alan null           → Adres/Telefon SİLİNİR · Ad için 400 (ad silinemez) · Aktif korunur
///   • alan dolu           → doğrulanır ve yazılır
/// </summary>
public sealed class SubeGuncelleDto
{
    private string? _ad;
    private string? _adres;
    private string? _telefon;
    private bool? _aktif;

    [MaxLength(200)]
    public string? Ad { get => _ad; set { _ad = value; AdVerildi = true; } }
    [JsonIgnore] public bool AdVerildi { get; private set; }

    [MaxLength(500)]
    public string? Adres { get => _adres; set { _adres = value; AdresVerildi = true; } }
    [JsonIgnore] public bool AdresVerildi { get; private set; }

    [MaxLength(20)]
    [Kuyumcu.Api.Common.Dogrulama.Telefon]   // GIRDI-AILESI-2 tur 3: ekranla TEK kural (Core TelefonDogrulayici)
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonVerildi = true; } }
    [JsonIgnore] public bool TelefonVerildi { get; private set; }

    public bool? Aktif { get => _aktif; set { _aktif = value; AktifVerildi = true; } }
    [JsonIgnore] public bool AktifVerildi { get; private set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class SubeServisi(KuyumcuDbContext db)
{
    public async Task<object> Olustur(SubeOlusturDto dto, AktifKullanici islem)
    {
        // PAKET GOC-D (envanter E · "şube telefonu hane sayısı"): [Telefon] yalnız controller yolunda
        // koşar; servis yolunda 9 haneli numara kaydediliyordu. AYNI Core kuralı (TelefonDogrulayici).
        TelefonDenetle(dto.Telefon);
        var mevcutAd = await db.Subeler.AnyAsync(s =>
            s.TenantId == islem.TenantId && s.Ad == dto.Ad);
        if (mevcutAd) throw new ConflictException($"'{dto.Ad}' adlı şube zaten mevcut");

        var now = DateTime.UtcNow;
        var sube = new Data.Sube
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            Ad = dto.Ad,
            Adres = dto.Adres,
            Telefon = dto.Telefon,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Subeler.Add(sube);
        await db.SaveChangesAsync();
        return await Getir(sube.Id, islem);
    }

    public async Task<List<Data.Sube>> Listele(AktifKullanici islem) =>
        await db.Subeler.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId)
            .ToListAsync();

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var sube = await db.Subeler.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Şube bulunamadı");
        return sube;
    }

    /// <summary>PAKET GOC-D — K2 telefon (10 ulusal hane + NDC aralığı) servis yolunda; boş = alan silinir/yok.</summary>
    private static void TelefonDenetle(string? telefon)
    {
        if (!string.IsNullOrWhiteSpace(telefon))
            Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TelefonDogrulayici.Denetle(telefon), "telefon");
    }

    public async Task<object> Guncelle(string id, SubeGuncelleDto dto, AktifKullanici islem)
    {
        if (dto.TelefonVerildi) TelefonDenetle(dto.Telefon);   // PAKET GOC-D: servis yolu da aynı kural
        var sube = await TenantSubesiBul(id, islem.TenantId);

        // PATCH-SOZLESME: ad YALNIZ gönderildiyse dokunulur. DataAnnotations yalnız
        // controller yolunda koşar → kurallar servis içinde ELLE de denetlenir.
        // PAKET PATCH-3 — TUTARLILIK KURALI (tüm PATCH uçları): ZORUNLU alanda null = KORU
        // (ürün adı/kodu, kategori adı, rezerv adedi ile AYNI); boş/boşluk dize → 400 alan.
        // ESKİ: şubede {ad:null} → 400, üründe {ad:null} → 200 idi (iki farklı cevap).
        if (dto.AdVerildi && dto.Ad is not null)
        {
            var ad = dto.Ad.Trim();
            if (ad.Length == 0)
                throw new BadRequestException("Şube adı boş bırakılamaz").AlanIle("ad");
            if (ad.Length > 200)
                throw new BadRequestException("Şube adı en fazla 200 karakter olabilir").AlanIle("ad");
            if (ad != sube.Ad && await db.Subeler.AnyAsync(s =>
                    s.TenantId == islem.TenantId && s.Ad == ad && s.Id != sube.Id))
                throw new AlanCakismasiException("ad", $"'{ad}' adlı şube zaten mevcut");
            sube.Ad = ad;
        }
        // İsteğe bağlı alan: null ya da "" (geriye uyum) → SİL; kolona "" yazılmaz.
        if (dto.AdresVerildi) sube.Adres = string.IsNullOrWhiteSpace(dto.Adres) ? null : dto.Adres.Trim();
        if (dto.TelefonVerildi) sube.Telefon = string.IsNullOrWhiteSpace(dto.Telefon) ? null : dto.Telefon.Trim();
        // UX-S25: aktife alma DELETE'in tersidir; ayri uc acilmaz, ayni PATCH tasir.
        if (dto.AktifVerildi && dto.Aktif is { } aktif) sube.Aktif = aktif;
        sube.Guncellendi = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await Getir(id, islem);
    }

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var sube = await TenantSubesiBul(id, islem.TenantId);
        sube.Aktif = false;
        sube.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Şube deaktif edildi") };
    }

    internal async Task<Data.Sube> TenantSubesiBul(string id, string tenantId) =>
        await db.Subeler.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId)
        ?? throw new NotFoundException("Şube bulunamadı");
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/subeler")]
public sealed class SubeController(SubeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/subeler — Şube oluştur — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] SubeOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/subeler — Tenant'ın tüm şubeleri</summary>
    [HttpGet]
    [HerkeseAcik("Şube listesi her ekranın süzgecinde gerekir; kiracı filtresi zaten sınırlar")]
    public Task<List<Data.Sube>> Listele() => servis.Listele(Islem);

    /// <summary>GET /api/v1/subeler/{id} — Şube detayı</summary>
    [HttpGet("{id}")]
    [HerkeseAcik("Tek şube okuması — kiracı filtresi sınırlar, yazma uçları [RolGerekli] ile kapalı")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/subeler/{id} — Şube güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] SubeGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>DELETE /api/v1/subeler/{id} — Şubeyi deaktif et — SAHIP</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
