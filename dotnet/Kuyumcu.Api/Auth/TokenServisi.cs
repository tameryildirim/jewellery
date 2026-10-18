using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kuyumcu.Api.Auth;

public sealed class JwtAyarlar
{
    public string Secret { get; set; } = null!;
    public int AccessDakika { get; set; } = 15;
    public int RefreshGun { get; set; } = 7;
}

/// <summary>
/// Jeton hedef kitleleri (audience). PAKET 45 / Mehmet VETO:
/// MFA meydan okuma jetonu access token ile AYNI Secret'la imzalanır; JwtBearer
/// onu bearer sayarsa MFA fiilen ATLANIR. Tek gerçek ayrım audience'tır ve
/// JwtBearer <c>ValidateAudience = true</c> ile bunu doğrular (Program.cs).
/// </summary>
public static class Jetonlar
{
    public const string ApiAudience = "kuyumcu-api";
    public const string MfaAudience = "kuyumcu-mfa";
    /// <summary>Meydan okuma jetonunun amacı; audience'ın yanında İKİNCİ kapı.</summary>
    public const string MfaAmac = "mfa";
}

/// <summary>Access token üretimi + refresh token üretim/hash (SHA-256).</summary>
public sealed class TokenServisi(JwtAyarlar ayarlar)
{
    public string AccessUret(string kullaniciId, string tenantId, string? subeId, string rol, bool saltOkunur = false)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ayarlar.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, kullaniciId),
            new("tenantId", tenantId),
            new("subeId", subeId ?? ""),
            new("rol", rol),
        };
        // D1-14: salt okunur (mali müşavir) hesap — SaltOkunurKapisi yazma isteklerini bu işaretle reddeder.
        if (saltOkunur) claims.Add(new(SaltOkunurKapisi.Claim, "1"));
        var token = new JwtSecurityToken(
            audience: Jetonlar.ApiAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(ayarlar.AccessDakika),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── PAKET 45: MFA meydan okuma jetonu ────────────────────────────────────

    /// <summary>
    /// Parola doğrulandı ama MFA geçilmedi: kısa ömürlü, AYRI AUDIENCE'lı jeton.
    /// <para>
    /// <b>pv</b> = parola hash'inin SHA-256'sının ilk 16 hex'i. Parola
    /// değiştirilince elde kalan meydan okuma jetonu GEÇERSİZLEŞİR (çalınan
    /// parolayla başlatılmış yarım giriş, parola değişince devam edemez).
    /// </para>
    /// </summary>
    public string MfaMeydanOkumaUret(string kullaniciId, string tenantId, string sifreHash, int dakika)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ayarlar.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            audience: Jetonlar.MfaAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, kullaniciId),
                new Claim("tenantId", tenantId),
                new Claim("amac", Jetonlar.MfaAmac),
                new Claim("pv", ParolaSurumu(sifreHash)),
            ],
            expires: DateTime.UtcNow.AddMinutes(dakika),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Meydan okuma jetonunu İMZA DOĞRULAYARAK çözer. Audience, amaç ya da ömür
    /// tutmuyorsa <c>null</c> döner — access token buradan GEÇEMEZ.
    /// (Oran sınırı bölütü de bunu kullanır: kota sahteciliği olmasın.)
    /// </summary>
    public (string KullaniciId, string TenantId, string ParolaSurumu)? MfaMeydanOkumaCoz(string? jeton)
    {
        if (string.IsNullOrWhiteSpace(jeton)) return null;
        try
        {
            var ilke = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidAudience = Jetonlar.MfaAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ayarlar.Secret)),
                ClockSkew = TimeSpan.Zero,
            };
            var kimlik = new JwtSecurityTokenHandler().ValidateToken(jeton, ilke, out _);
            if (kimlik.FindFirst("amac")?.Value != Jetonlar.MfaAmac) return null;

            var kullaniciId = kimlik.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? kimlik.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var tenantId = kimlik.FindFirst("tenantId")?.Value;
            var pv = kimlik.FindFirst("pv")?.Value;
            if (string.IsNullOrEmpty(kullaniciId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(pv))
                return null;
            return (kullaniciId, tenantId, pv);
        }
        catch (Exception)
        {
            // Bozuk/süresi dolmuş/yanlış audience — hepsi aynı cevabı verir: geçersiz.
            return null;
        }
    }

    /// <summary>Parola sürümü damgası — parola hash'inin SHA-256'sının ilk 16 hex'i.</summary>
    public static string ParolaSurumu(string sifreHash) => Hashle(sifreHash)[..16];

    /// <summary>Rastgele refresh token — düz metin istemciye, SHA-256 hash'i veritabanına.</summary>
    public (string DuzMetin, string Hash) RefreshUret()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var duz = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (duz, Hashle(duz));
    }

    public static string Hashle(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public int RefreshGun => ayarlar.RefreshGun;
}
