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
// Nest'te PATCH de aynı SubeOlusturDto ile doğrulanır (ad zorunlu);
// adres/telefon için "gönderildi mi?" ayrımı Verildi bayraklarıyla korunur.

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SubeOlusturDto
{
    private string? _adres;
    private string? _telefon;

    [Required, MaxLength(200)]
    public string Ad { get; set; } = null!;

    [MaxLength(500)]
    public string? Adres { get => _adres; set { _adres = value; AdresVerildi = true; } }
    [JsonIgnore] public bool AdresVerildi { get; private set; }

    [MaxLength(20)]
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonVerildi = true; } }
    [JsonIgnore] public bool TelefonVerildi { get; private set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class SubeServisi(KuyumcuDbContext db)
{
    public async Task<object> Olustur(SubeOlusturDto dto, AktifKullanici islem)
    {
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

    public async Task<object> Guncelle(string id, SubeOlusturDto dto, AktifKullanici islem)
    {
        var sube = await TenantSubesiBul(id, islem.TenantId);

        sube.Ad = dto.Ad;
        if (dto.AdresVerildi) sube.Adres = dto.Adres;
        if (dto.TelefonVerildi) sube.Telefon = dto.Telefon;
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
        return new { mesaj = "Şube deaktif edildi" };
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
    public Task<List<Data.Sube>> Listele() => servis.Listele(Islem);

    /// <summary>GET /api/v1/subeler/{id} — Şube detayı</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/subeler/{id} — Şube güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] SubeOlusturDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>DELETE /api/v1/subeler/{id} — Şubeyi deaktif et — SAHIP</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
