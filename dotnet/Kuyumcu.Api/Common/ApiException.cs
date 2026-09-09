using System.Text.Json;

namespace Kuyumcu.Api.Common;

/// <summary>
/// NestJS hata sözleşmesini koruyan istisnalar:
/// { "message": "...", "error": "Not Found", "statusCode": 404 }
/// </summary>
public class ApiException(int statusCode, string mesaj, string? hataAdi = null)
    : Exception(mesaj)
{
    public int StatusCode { get; } = statusCode;
    public string HataAdi { get; } = hataAdi ?? VarsayilanAd(statusCode);

    private static string VarsayilanAd(int kod) => kod switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        _   => "Internal Server Error",
    };
}

public sealed class NotFoundException(string mesaj) : ApiException(404, mesaj);
public sealed class BadRequestException(string mesaj) : ApiException(400, mesaj);
public sealed class UnauthorizedException(string mesaj) : ApiException(401, mesaj);
public sealed class ForbiddenException(string mesaj) : ApiException(403, mesaj);
public sealed class ConflictException(string mesaj) : ApiException(409, mesaj);

/// <summary>ApiException'ları Nest formatında JSON'a çeviren middleware.</summary>
public sealed class HataYakalayici(RequestDelegate next, ILogger<HataYakalayici> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ApiException ex)
        {
            ctx.Response.StatusCode = ex.StatusCode;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = ex.Message,
                error = ex.HataAdi,
                statusCode = ex.StatusCode,
            }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Beklenmeyen hata: {Yol}", ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Internal server error",
                statusCode = 500,
            }));
        }
    }
}
