using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Auth;

/// <summary>
/// D1-14 (18.10, rakip eşitleme: "mali müşavir rolü — yalnız okuma"). Salt okunur işaretli oturum (<see cref="Claim"/>)
/// GET/HEAD/OPTIONS dışında HİÇBİR istek yapamaz — kapı TEK yerdedir, uç uç rol listesi değiştirilmez.
/// İstisna: oturum uçları (çıkış, jeton yenileme, parola değiştirme) — kendi oturumunu yönetebilmeli.
/// </summary>
public sealed class SaltOkunurKapisi(RequestDelegate sonraki)
{
    public const string Claim = "salt";
    public const string Ileti = "Bu hesap yalnız okuma içindir (mali müşavir) — kayıt değiştirilemez";

    private static readonly string[] Serbest = ["/api/v1/auth/"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true
            && ctx.User.FindFirst(Claim)?.Value == "1"
            && !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method) && !HttpMethods.IsOptions(ctx.Request.Method)
            && !Serbest.Any(s => ctx.Request.Path.StartsWithSegments(s.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
            throw new ForbiddenException(Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(Ileti));
        await sonraki(ctx);
    }
}
