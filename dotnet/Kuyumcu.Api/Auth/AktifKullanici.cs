using System.Security.Claims;

namespace Kuyumcu.Api.Auth;

/// <summary>JWT payload'ının servis katmanına taşınan hâli (Nest'teki AktifKullanici).</summary>
public sealed record AktifKullanici(string Id, string TenantId, string? SubeId, string Rol)
{
    /// <summary>Rol hiyerarşisi: SAHIP(4) &gt; MUDUR(3) &gt; MUHASEBE(2) &gt; TEZGAHTAR(1)</summary>
    public static int Seviye(string rol) => rol switch
    {
        "SAHIP" => 4, "MUDUR" => 3, "MUHASEBE" => 2, "TEZGAHTAR" => 1, _ => 0,
    };

    public static AktifKullanici FromClaims(ClaimsPrincipal user)
    {
        var subeId = user.FindFirstValue("subeId");
        return new AktifKullanici(
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "",
            user.FindFirstValue("tenantId") ?? "",
            string.IsNullOrEmpty(subeId) ? null : subeId,
            user.FindFirstValue("rol") ?? "");
    }
}
