using Kuyumcu.Api.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kuyumcu.Api.Auth;

/// <summary>
/// Nest'teki @Roles(...) dekoratörünün karşılığı — sayılan rollerden biri gerekir.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RolGerekliAttribute(params string[] roller) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var kullanici = AktifKullanici.FromClaims(ctx.HttpContext.User);
        if (!roller.Contains(kullanici.Rol))
            // Nest RolesGuard ile birebir: "Bu işlem için gerekli rol: SAHIP veya MUDUR"
            throw new ForbiddenException($"Bu işlem için gerekli rol: {string.Join(" veya ", roller)}");
    }
}
