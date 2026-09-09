using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Admin;

// ═════════════════════════════════════════════════════════════════════════════
// Kaynak: apps/api/src/modules/admin/*
// Tenant kendi bilgilerini görür; planını ve AI modülünü günceller.
// Tüm uç noktalar SAHIP rolü gerektirir.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class PlanGuncelleDto
{
    [Required, RegularExpression("BASLANGIC|PRO|KURUMSAL")]
    public string Plan { get; set; } = null!;
}

public sealed class AiToggleDto
{
    [Required] public bool? Etkin { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class AdminServisi(KuyumcuDbContext db)
{
    /// <summary>Tenant kendi bilgilerini görür + planını günceller</summary>
    public async Task<Tenant> TenantGetir(AktifKullanici islem)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");
        return tenant;
    }

    public async Task<Tenant> PlanGuncelle(string plan, AktifKullanici islem)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        tenant.Plan = plan;
        tenant.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await TenantGetir(islem);
    }

    public async Task<Tenant> AiToggle(bool etkin, AktifKullanici islem)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        tenant.AiEtkin = etkin;
        tenant.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await TenantGetir(islem);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[RolGerekli("SAHIP")]
[Route("api/v1/admin")]
public sealed class AdminController(AdminServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/admin/tenant — Tenant bilgisi + plan</summary>
    [HttpGet("tenant")]
    public Task<Tenant> TenantGetir() => servis.TenantGetir(Islem);

    /// <summary>PATCH /api/v1/admin/plan — Plan değiştir</summary>
    [HttpPatch("plan")]
    public Task<Tenant> PlanGuncelle([FromBody] PlanGuncelleDto dto)
        => servis.PlanGuncelle(dto.Plan, Islem);

    /// <summary>PATCH /api/v1/admin/ai — AI modülünü aç/kapat</summary>
    [HttpPatch("ai")]
    public Task<Tenant> AiToggle([FromBody] AiToggleDto dto)
        => servis.AiToggle(dto.Etkin!.Value, Islem);
}
