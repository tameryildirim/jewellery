using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Auth;

// ═════════════════════════════════════════════════════════════════════════════
// AUTH MODÜLÜ — kayıt (tenant bootstrap), giriş, refresh rotasyonu, çıkış, ben.
// Kaynak: apps/api/src/modules/auth/*
//   • Refresh token: düz metin istemciye, SHA-256 hash'i veritabanına
//   • Rotasyon: her yenilemede eski token iptal, yenisi üretilir
//   • Çalıntı tespiti: iptal edilmiş token yeniden kullanılırsa kullanıcının
//     TÜM refresh tokenleri iptal edilir
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KayitDto
{
    /// <summary>İşletme / mağaza adı</summary>
    [Required, MaxLength(200)]
    public string IsletmeAdi { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Ad { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Soyad { get; set; } = null!;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = null!;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Sifre { get; set; } = null!;
}

public sealed class GirisDto
{
    [Required, EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = null!;

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Şifre 8-72 karakter olmalıdır")]
    public string Sifre { get; set; } = null!;

    /// <summary>Aynı e-posta birden fazla işletmede kayıtlıysa zorunlu</summary>
    [StringLength(36, MinimumLength = 26, ErrorMessage = "Geçersiz tenant ID")]
    public string? TenantId { get; set; }
}

public sealed class RefreshDto
{
    // Nest 43 karakter (32 byte) üretirdi; TokenServisi 64 karakter (48 byte)
    // üretir — geçiş dönemi için her iki uzunluk da kabul edilir.
    [Required, StringLength(64, MinimumLength = 43, ErrorMessage = "Geçersiz refresh token")]
    public string RefreshToken { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class AuthServisi(KuyumcuDbContext db, TokenServisi tokenServisi)
{
    private const int BcryptRounds = 12;

    // ─── Tenant kayıt (bootstrap) ────────────────────────────────────────────
    /// <summary>Yeni tenant + varsayılan şube + SAHIP kullanıcı oluşturur.</summary>
    public async Task<object> Kayit(KayitDto dto, string ipAdresi, string userAgent)
    {
        var mevcutKullanici = await db.Kullanicilar.AsNoTracking()
            .AnyAsync(k => k.Email == dto.Email);
        if (mevcutKullanici) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");

        var tenantId = Kimlik.Ulid();
        var subeId = Kimlik.Ulid();
        var userId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var sifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre, BcryptRounds);

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Ad = dto.IsletmeAdi,
            Plan = "BASLANGIC",
            AiEtkin = false,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        db.Subeler.Add(new Data.Sube
        {
            Id = subeId,
            TenantId = tenantId,
            Ad = "Merkez Şube",
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        db.Kullanicilar.Add(new Data.Kullanici
        {
            Id = userId,
            TenantId = tenantId,
            SubeId = subeId,
            Ad = dto.Ad,
            Soyad = dto.Soyad,
            Email = dto.Email,
            SifreHash = sifreHash,
            Rol = "SAHIP",
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var accessToken = tokenServisi.AccessUret(userId, tenantId, subeId, "SAHIP");
        var refreshToken = await RefreshUretVeKaydet(userId, tenantId, ipAdresi, userAgent);

        return new { tenantId, accessToken, refreshToken };
    }

    // ─── Giriş ───────────────────────────────────────────────────────────────

    public async Task<object> Giris(GirisDto dto, string ipAdresi, string userAgent)
    {
        // tenantId verilmediyse yalnızca e-posta ile ara; aynı e-posta birden
        // fazla tenant'ta kayıtlıysa (composite unique buna izin verir) tenantId iste
        var q = db.Kullanicilar.AsNoTracking()
            .Where(k => k.Email == dto.Email && k.Aktif);
        if (!string.IsNullOrEmpty(dto.TenantId))
            q = q.Where(k => k.TenantId == dto.TenantId);

        var adaylar = await q.Take(2).ToListAsync();

        if (adaylar.Count == 0) throw new NotFoundException("Kullanıcı bulunamadı");
        if (adaylar.Count > 1)
            throw new UnauthorizedException("Bu e-posta birden fazla işletmede kayıtlı, tenantId belirtiniz");
        var kullanici = adaylar[0];

        var sifreEslesti = BCrypt.Net.BCrypt.Verify(dto.Sifre, kullanici.SifreHash);
        if (!sifreEslesti) throw new UnauthorizedException("E-posta veya şifre hatalı");

        var accessToken = tokenServisi.AccessUret(
            kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol);
        var refreshToken = await RefreshUretVeKaydet(
            kullanici.Id, kullanici.TenantId, ipAdresi, userAgent);

        await db.Kullanicilar.Where(k => k.Id == kullanici.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.SonGiris, DateTime.UtcNow));

        return new { accessToken, refreshToken, kullanici = Temizle(kullanici) };
    }

    // ─── Token yenileme (rotasyon) ───────────────────────────────────────────

    public async Task<object> Yenile(string rawRefreshToken, string ipAdresi, string userAgent)
    {
        var tokenHash = TokenServisi.Hashle(rawRefreshToken);
        var now = DateTime.UtcNow;

        var kayit = await db.RefreshTokenler
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        // Çalıntı tespiti: iptal edilmiş token yeniden kullanıldıysa
        // kullanıcının TÜM tokenlerini iptal et
        if (kayit is not null && kayit.IptalEdildi)
        {
            await TumTokenlerIptalEt(kayit.KullaniciId);
            throw new UnauthorizedException("Refresh token geçersiz veya süresi dolmuş");
        }

        if (kayit is null || kayit.Gecerlilik < now)
        {
            // Süresi dolmuşsa güvenlik önlemi: tüm tokenlerini iptal et
            if (kayit is not null) await TumTokenlerIptalEt(kayit.KullaniciId);
            throw new UnauthorizedException("Refresh token geçersiz veya süresi dolmuş");
        }

        // Token rotate: eskiyi iptal et
        kayit.IptalEdildi = true;
        await db.SaveChangesAsync();

        var kullanici = await db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == kayit.KullaniciId);
        if (kullanici is null || !kullanici.Aktif)
            throw new UnauthorizedException("Kullanıcı aktif değil");

        var accessToken = tokenServisi.AccessUret(
            kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol);
        var refreshToken = await RefreshUretVeKaydet(
            kullanici.Id, kullanici.TenantId, ipAdresi, userAgent);

        return new { accessToken, refreshToken };
    }

    // ─── Çıkış ───────────────────────────────────────────────────────────────

    public async Task Cikis(string rawRefreshToken)
    {
        var tokenHash = TokenServisi.Hashle(rawRefreshToken);
        await db.RefreshTokenler.Where(t => t.TokenHash == tokenHash)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IptalEdildi, true));
    }

    // ─── Mevcut kullanıcı ────────────────────────────────────────────────────

    public async Task<object> Ben(AktifKullanici islem)
    {
        var kullanici = await db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == islem.Id)
            ?? throw new NotFoundException("Kullanıcı bulunamadı");
        return Temizle(kullanici);
    }

    // ─── Yardımcı metodlar ───────────────────────────────────────────────────

    /// <summary>Rastgele refresh token üret, hash'ini kaydet, düz metni döndür.</summary>
    private async Task<string> RefreshUretVeKaydet(
        string kullaniciId, string tenantId, string ipAdresi, string userAgent)
    {
        var (duzMetin, hash) = tokenServisi.RefreshUret();
        var now = DateTime.UtcNow;

        db.RefreshTokenler.Add(new RefreshToken
        {
            Id = Kimlik.Ulid(),
            KullaniciId = kullaniciId,
            TenantId = tenantId,
            TokenHash = hash,
            IpAdresi = ipAdresi,
            UserAgent = userAgent,
            Gecerlilik = now.AddDays(tokenServisi.RefreshGun),
            IptalEdildi = false,
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();

        return duzMetin;
    }

    private async Task TumTokenlerIptalEt(string kullaniciId) =>
        await db.RefreshTokenler.Where(t => t.KullaniciId == kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IptalEdildi, true));

    /// <summary>sifreHash hariç tüm kullanıcı alanları (Nest kullaniciTemizle).</summary>
    private static object Temizle(Data.Kullanici k) => new
    {
        id = k.Id,
        tenantId = k.TenantId,
        subeId = k.SubeId,
        ad = k.Ad,
        soyad = k.Soyad,
        email = k.Email,
        rol = k.Rol,
        aktif = k.Aktif,
        sonGiris = k.SonGiris,
        olusturuldu = k.Olusturuldu,
        guncellendi = k.Guncellendi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/auth")]
public sealed class AuthController(AuthServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    private string Ua => Request.Headers.UserAgent.ToString();

    /// <summary>POST /api/v1/auth/kayit — Yeni tenant + SAHIP kullanıcı (bootstrap)</summary>
    [HttpPost("kayit")]
    [AllowAnonymous]
    public async Task<IActionResult> Kayit([FromBody] KayitDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Kayit(dto, Ip, Ua));

    /// <summary>POST /api/v1/auth/giris — E-posta + şifre ile oturum aç</summary>
    [HttpPost("giris")]
    [AllowAnonymous]
    public Task<object> Giris([FromBody] GirisDto dto) => servis.Giris(dto, Ip, Ua);

    /// <summary>POST /api/v1/auth/yenile — Refresh token ile yeni access token (rotasyon)</summary>
    [HttpPost("yenile")]
    [AllowAnonymous]
    public Task<object> Yenile([FromBody] RefreshDto dto) => servis.Yenile(dto.RefreshToken, Ip, Ua);

    /// <summary>POST /api/v1/auth/cikis — Refresh token'ı geçersiz kıl (204)</summary>
    [HttpPost("cikis")]
    [AllowAnonymous]
    public async Task<IActionResult> Cikis([FromBody] RefreshDto dto)
    {
        await servis.Cikis(dto.RefreshToken);
        return NoContent();
    }

    /// <summary>GET /api/v1/auth/ben — Oturum açmış kullanıcının bilgileri</summary>
    [HttpGet("ben")]
    public Task<object> Ben() => servis.Ben(Islem);
}
