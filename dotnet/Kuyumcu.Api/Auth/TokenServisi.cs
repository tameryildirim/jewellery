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

/// <summary>Access token üretimi + refresh token üretim/hash (SHA-256).</summary>
public sealed class TokenServisi(JwtAyarlar ayarlar)
{
    public string AccessUret(string kullaniciId, string tenantId, string? subeId, string rol)
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
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(ayarlar.AccessDakika),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
