using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kuyumcu.Api.Modules.Yetki;

// ═════════════════════════════════════════════════════════════════════════════
// Yetki modülü — rol bazlı modül (ekran) erişimi. Tenant başına yapılandırılır.
//   • Bir rol_yetkileri satırı = o rolün o modüle erişimi var.
//   • SAHIP her zaman tam yetkilidir (tabloda tutulmaz).
//   • İlk erişimde varsayılanlar tohumlanır (mevcut davranışı korur).
// ═════════════════════════════════════════════════════════════════════════════

public static class Moduller
{
    /// <summary>Yetkilendirilebilir ekranlar (menüdeki modüller).</summary>
    public static readonly string[] Tumu =
        ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"];

    /// <summary>Yapılandırılabilir roller — SAHIP her zaman tam yetkili olduğundan listede yok.</summary>
    public static readonly string[] Roller = ["MUDUR", "MUHASEBE", "TEZGAHTAR"];

    /// <summary>Varsayılan yetkiler — mevcut davranışı korur (rapor: MUHASEBE+, kullanıcı: MUDUR+).</summary>
    public static readonly Dictionary<string, string[]> Varsayilan = new()
    {
        ["MUDUR"]     = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"],
        ["MUHASEBE"]  = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor"],
        ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka", "fatura"],
    };
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class YetkiServisi(KuyumcuDbContext db, IMemoryCache cache)
{
    private const string MarkerRol = "__SISTEM__", MarkerModul = "__yapilandirildi__";
    private static string CacheKey(string tenantId) => $"yetki:{tenantId}";

    /// <summary>Tenant'ın rol→modül matrisi (SAHIP hariç). İlk erişimde varsayılanları tohumlar.</summary>
    public async Task<Dictionary<string, HashSet<string>>> Matris(string tenantId)
    {
        if (cache.TryGetValue(CacheKey(tenantId), out Dictionary<string, HashSet<string>>? onbellek) && onbellek is not null)
            return onbellek;

        await TohumlaGerekiyorsa(tenantId);

        var satirlar = await db.RolYetkileri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.Rol != MarkerRol)
            .ToListAsync();

        var matris = Moduller.Roller.ToDictionary(r => r, _ => new HashSet<string>());
        foreach (var s in satirlar)
            if (matris.TryGetValue(s.Rol, out var set)) set.Add(s.Modul);

        cache.Set(CacheKey(tenantId), matris, TimeSpan.FromMinutes(30));
        return matris;
    }

    /// <summary>Belirli rolün erişebildiği modüller (SAHIP → tümü).</summary>
    public async Task<HashSet<string>> RolModulleri(string tenantId, string rol)
    {
        if (rol == "SAHIP") return [.. Moduller.Tumu];
        var matris = await Matris(tenantId);
        return matris.TryGetValue(rol, out var set) ? set : [];
    }

    public async Task<bool> ErisebilirMi(string tenantId, string rol, string modul)
    {
        if (rol == "SAHIP") return true;
        return (await RolModulleri(tenantId, rol)).Contains(modul);
    }

    /// <summary>Matrisi kaydet (SAHIP) — tenant satırları silinip yeniden yazılır (+ marker).</summary>
    public async Task Kaydet(string tenantId, Dictionary<string, string[]> matris)
    {
        var mevcut = await db.RolYetkileri.Where(y => y.TenantId == tenantId).ToListAsync();
        db.RolYetkileri.RemoveRange(mevcut);

        var eklenecek = new List<RolYetki>
        {
            new() { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = MarkerModul },
        };
        foreach (var rol in Moduller.Roller)
        {
            if (!matris.TryGetValue(rol, out var modeller) || modeller is null) continue;
            foreach (var m in modeller.Where(Moduller.Tumu.Contains).Distinct())
                eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = rol, Modul = m });
        }
        db.RolYetkileri.AddRange(eklenecek);
        await db.SaveChangesAsync();
        cache.Remove(CacheKey(tenantId));
    }

    private async Task TohumlaGerekiyorsa(string tenantId)
    {
        var yapilandirilmis = await db.RolYetkileri.AnyAsync(y => y.TenantId == tenantId && y.Rol == MarkerRol);
        if (yapilandirilmis) return;

        var eklenecek = new List<RolYetki>
        {
            new() { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = MarkerModul },
        };
        foreach (var (rol, modeller) in Moduller.Varsayilan)
            foreach (var m in modeller)
                eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = rol, Modul = m });

        db.RolYetkileri.AddRange(eklenecek);
        // Eşzamanlı ilk isteklerde çift tohumlama olabilir — benzersizlik ihlalini yut.
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); }
    }
}

// ── Yetki filtresi (controller/action seviyesinde modül erişimi) ─────────────

/// <summary>
/// Modül erişim kontrolü. SAHIP daima geçer; diğer roller için tenant yapılandırmasına bakılır.
/// RolGerekli ile aynı desende ForbiddenException fırlatır (HataYakalayici 403'e çevirir).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ModulYetkisiAttribute(string modul) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var kullanici = AktifKullanici.FromClaims(ctx.HttpContext.User);
        if (kullanici.Rol == "SAHIP") return;

        var servis = ctx.HttpContext.RequestServices.GetRequiredService<YetkiServisi>();
        if (!await servis.ErisebilirMi(kullanici.TenantId, kullanici.Rol, modul))
            throw new ForbiddenException($"Bu ekran için yetkiniz yok ({modul})");
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

public sealed class YetkiKaydetDto
{
    /// <summary>rol → erişebileceği modüller. Yalnız yapılandırılabilir roller dikkate alınır.</summary>
    public Dictionary<string, string[]>? Matris { get; set; }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/yetkiler")]
public sealed class YetkiController(YetkiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/yetkiler/benim — Giriş yapanın erişebildiği modüller (menü için) — herkes</summary>
    [HttpGet("benim")]
    public async Task<object> Benim()
    {
        var moduller = await servis.RolModulleri(Islem.TenantId, Islem.Rol);
        return new { moduller = moduller.OrderBy(x => x).ToArray() };
    }

    /// <summary>GET /api/v1/yetkiler — Tüm rol/modül matrisi (yapılandırma ekranı) — SAHIP</summary>
    [HttpGet]
    [RolGerekli("SAHIP")]
    public async Task<object> Getir()
    {
        var matris = await servis.Matris(Islem.TenantId);
        return new
        {
            moduller = Moduller.Tumu,
            roller = Moduller.Roller,
            matris = Moduller.Roller.ToDictionary(r => r, r => matris[r].OrderBy(x => x).ToArray()),
        };
    }

    /// <summary>PUT /api/v1/yetkiler — Matrisi kaydet — SAHIP</summary>
    [HttpPut]
    [RolGerekli("SAHIP")]
    public async Task<object> Kaydet([FromBody] YetkiKaydetDto dto)
    {
        await servis.Kaydet(Islem.TenantId, dto.Matris ?? new());
        return new { mesaj = "Yetkiler güncellendi" };
    }
}
