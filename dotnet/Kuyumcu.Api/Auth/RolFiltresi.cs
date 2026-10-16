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
            // KESIF-P1 (#72 · UX-S31): ham rol kodu (SAHIP · MUDUR) metne BASILMAZ;
            // ad tek kaynaktan (KesifP1Sozlugu.RolAdi): "… gerekli rol: dükkân sahibi ya da müdür".
            throw new ForbiddenException(string.Format(
                Kuyumcu.Core.I18n.KesifP1Sozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).SunucuGerekliRol,   // PAKET DIL-SUNUCU: istek dili
                Kuyumcu.Core.I18n.KesifP1Sozlugu.RolListesi(roller, Kuyumcu.Core.I18n.IstekDili.Gecerli)));
    }
}
