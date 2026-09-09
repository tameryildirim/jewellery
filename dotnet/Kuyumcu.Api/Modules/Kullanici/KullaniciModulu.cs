using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kullanici;

// ═════════════════════════════════════════════════════════════════════════════
// Kullanıcı modülü — NestJS kaynağı: apps/api/src/modules/kullanici/*
//   • Rota: api/v1/kullanicilar (Nest ile birebir)
//   • Rol hiyerarşisi: SAHIP(4) > MUDUR(3) > MUHASEBE(2) > TEZGAHTAR(1)
//   • Şifreler bcrypt (work factor 12) ile hash'lenir — Nest'teki bcryptjs eşleniği
//   • sifreHash hiçbir response'ta dönmez (Temizle)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KullaniciOlusturDto
{
    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2-100 karakter olmalıdır")]
    public string Ad { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2-100 karakter olmalıdır")]
    public string Soyad { get; set; } = null!;

    [Required, EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = null!;

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Şifre 8-72 karakter olmalıdır")]
    [RegularExpression("^(?=.*[0-9])(?=.*[a-zA-Z]).+$", ErrorMessage = "Şifre en az bir harf ve bir rakam içermelidir")]
    public string Sifre { get; set; } = null!;

    [Required, RegularExpression("SAHIP|MUDUR|TEZGAHTAR|MUHASEBE", ErrorMessage = "Geçersiz rol")]
    public string Rol { get; set; } = null!;

    public string? SubeId { get; set; }
}

public sealed class KullaniciGuncelleDto
{
    private string? _subeId;

    [StringLength(100, MinimumLength = 2)] public string? Ad { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? Soyad { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz"), MaxLength(254)]
    public string? Email { get; set; }

    /// <summary>
    /// JSON gövdesinde subeId alanı geldi mi (açık null dahil)?
    /// Nest'te undefined/null ayrımı var: gönderilen null şubeyi temizler,
    /// hiç gönderilmeyen alan ise dokunulmadan bırakılır.
    /// </summary>
    [JsonIgnore] public bool SubeIdGonderildi { get; private set; }

    public string? SubeId
    {
        get => _subeId;
        set { _subeId = value; SubeIdGonderildi = true; }
    }

    public bool? Aktif { get; set; }
}

public sealed class SifreDegistirDto
{
    /// <summary>Kendi şifresini değiştiren kullanıcı için gerekli; admin reset'te opsiyonel.</summary>
    public string? MevcutSifre { get; set; }

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Yeni şifre 8-72 karakter olmalıdır")]
    [RegularExpression("^(?=.*[0-9])(?=.*[a-zA-Z]).+$", ErrorMessage = "Şifre en az bir harf ve bir rakam içermelidir")]
    public string YeniSifre { get; set; } = null!;

    [Required]
    public string YeniSifreTekrar { get; set; } = null!;
}

public sealed class RolDegistirDto
{
    [Required, RegularExpression("SAHIP|MUDUR|TEZGAHTAR|MUHASEBE", ErrorMessage = "Geçersiz rol")]
    public string Rol { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KullaniciServisi(KuyumcuDbContext db)
{
    private const int BcryptRounds = 12;

    // ─── Kullanıcı oluştur ────────────────────────────────────────────────────

    public async Task<object> Olustur(KullaniciOlusturDto dto, AktifKullanici islem)
    {
        RolOlusturmayaIzinVarMi(islem.Rol, dto.Rol);

        var mevcutmu = await db.Kullanicilar.AnyAsync(k =>
            k.Email == dto.Email && k.TenantId == islem.TenantId);
        if (mevcutmu) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");

        var sifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre, BcryptRounds);
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();

        db.Kullanicilar.Add(new Data.Kullanici
        {
            Id = id,
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = dto.Ad,
            Soyad = dto.Soyad,
            Email = dto.Email,
            SifreHash = sifreHash,
            Rol = dto.Rol,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<List<object>> Listele(AktifKullanici islem)
    {
        var liste = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .ToListAsync();
        return liste.Select(Temizle).ToList();
    }

    // ─── Tekil kullanıcı ─────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var kullanici = await TenantKullanicisiBul(id, islem.TenantId);
        return Temizle(kullanici);
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, KullaniciGuncelleDto dto, AktifKullanici islem)
    {
        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        // Yalnızca gönderilen alanlar yazılır (Nest: v !== undefined)
        // subeId açık null gönderilirse şube temizlenir — Nest ile birebir
        if (dto.Ad is not null) hedef.Ad = dto.Ad;
        if (dto.Soyad is not null) hedef.Soyad = dto.Soyad;
        await EpostaAyarla(hedef, dto.Email, islem.TenantId);
        if (dto.SubeIdGonderildi) hedef.SubeId = dto.SubeId;
        if (dto.Aktif is not null) hedef.Aktif = dto.Aktif.Value;
        hedef.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Kendi profilini güncelle ─────────────────────────────────────────────

    public async Task<object> KendinGuncelle(KullaniciGuncelleDto dto, AktifKullanici islem)
    {
        var kullanici = await TenantKullanicisiBul(islem.Id, islem.TenantId);

        // Kendi aktifliğini değiştiremez — aktif alanı yok sayılır
        // subeId açık null gönderilirse şube temizlenir — Nest ile birebir
        if (dto.Ad is not null) kullanici.Ad = dto.Ad;
        if (dto.Soyad is not null) kullanici.Soyad = dto.Soyad;
        await EpostaAyarla(kullanici, dto.Email, islem.TenantId);
        if (dto.SubeIdGonderildi) kullanici.SubeId = dto.SubeId;
        kullanici.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(islem.Id, islem);
    }

    // ─── Rol değiştir ─────────────────────────────────────────────────────────

    public async Task<object> RolDegistir(string id, RolDegistirDto dto, AktifKullanici islem)
    {
        if (id == islem.Id)
            throw new ForbiddenException("Kendi rolünüzü değiştiremezsiniz");

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);
        RolOlusturmayaIzinVarMi(islem.Rol, dto.Rol); // atanacak rol de kontrol edilmeli

        hedef.Rol = dto.Rol;
        hedef.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Kendi şifresini değiştir ─────────────────────────────────────────────

    public async Task<object> KendinSifreDegistir(SifreDegistirDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrEmpty(dto.MevcutSifre))
            throw new BadRequestException("Mevcut şifre gereklidir");
        YeniSifreTekrarKontrol(dto.YeniSifre, dto.YeniSifreTekrar);

        var kullanici = await TenantKullanicisiBul(islem.Id, islem.TenantId);
        var eslesti = BCrypt.Net.BCrypt.Verify(dto.MevcutSifre, kullanici.SifreHash);
        if (!eslesti) throw new BadRequestException("Mevcut şifre hatalı");

        await SifreGuncelle(kullanici, dto.YeniSifre);
        return new { mesaj = "Şifre başarıyla güncellendi" };
    }

    // ─── Admin şifre sıfırlama ────────────────────────────────────────────────

    public async Task<object> AdminSifreSifirla(string id, SifreDegistirDto dto, AktifKullanici islem)
    {
        if (id == islem.Id)
        {
            // Kendi şifresini admin olarak sıfırlamak — kendi endpoint'ini kullansın
            throw new BadRequestException("Kendi şifrenizi /ben/sifre endpoint'inden değiştiriniz");
        }
        YeniSifreTekrarKontrol(dto.YeniSifre, dto.YeniSifreTekrar);

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        await SifreGuncelle(hedef, dto.YeniSifre);
        return new { mesaj = "Şifre başarıyla sıfırlandı" };
    }

    // ─── Deaktif et ───────────────────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        if (id == islem.Id)
            throw new ForbiddenException("Kendi hesabınızı deaktif edemezsiniz");

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        hedef.Aktif = false;
        hedef.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new { mesaj = "Kullanıcı deaktif edildi" };
    }

    // ─── Yardımcı metodlar ───────────────────────────────────────────────────

    internal async Task<Data.Kullanici> TenantKullanicisiBul(string id, string tenantId) =>
        await db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId)
        ?? throw new NotFoundException("Kullanıcı bulunamadı");

    /// <summary>E-posta değişiyorsa tenant içinde benzersizlik doğrular ve atar.</summary>
    private async Task EpostaAyarla(Data.Kullanici hedef, string? yeniEmail, string tenantId)
    {
        if (yeniEmail is null || yeniEmail == hedef.Email) return;
        var cakisan = await db.Kullanicilar.AnyAsync(k =>
            k.Email == yeniEmail && k.TenantId == tenantId && k.Id != hedef.Id);
        if (cakisan) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");
        hedef.Email = yeniEmail;
    }

    private static void HedefUzerindekiYetkiKontrol(AktifKullanici islem, string hedefRol)
    {
        if (AktifKullanici.Seviye(islem.Rol) <= AktifKullanici.Seviye(hedefRol))
            throw new ForbiddenException(
                "Kendi rolünüzden yüksek veya eşit roldeki kullanıcılar üzerinde işlem yapamazsınız");
    }

    private static void RolOlusturmayaIzinVarMi(string islemYapanRol, string hedefRol)
    {
        if (AktifKullanici.Seviye(islemYapanRol) <= AktifKullanici.Seviye(hedefRol))
            throw new ForbiddenException(
                $"{hedefRol} rolü atamak için daha yüksek yetkiye sahip olmanız gerekmektedir");
    }

    private static void YeniSifreTekrarKontrol(string yeni, string tekrar)
    {
        if (yeni != tekrar)
            throw new BadRequestException("Yeni şifre ve tekrarı eşleşmiyor");
    }

    private async Task SifreGuncelle(Data.Kullanici kullanici, string yeniSifre)
    {
        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(yeniSifre, BcryptRounds);
        kullanici.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>sifreHash alanını response'tan düşürür (Nest'teki temizle).</summary>
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
[Route("api/v1/kullanicilar")]
[Yetki.ModulYetkisi("kullanici")]
public sealed class KullaniciController(KullaniciServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/kullanicilar — Yeni kullanıcı oluştur — SAHIP, MUDUR (201)</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> Olustur([FromBody] KullaniciOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>GET /api/v1/kullanicilar — Tenant'taki tüm kullanıcılar — SAHIP, MUDUR</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<List<object>> Listele() => servis.Listele(Islem);

    /// <summary>GET /api/v1/kullanicilar/ben — Kendi profili — herkes</summary>
    [HttpGet("ben")]
    public Task<object> BenimProfilim() => servis.Getir(Islem.Id, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/ben — Kendi profilini güncelle — herkes</summary>
    [HttpPatch("ben")]
    public Task<object> KendinGuncelle([FromBody] KullaniciGuncelleDto dto)
        => servis.KendinGuncelle(dto, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/ben/sifre — Kendi şifresini değiştir — herkes</summary>
    [HttpPatch("ben/sifre")]
    public Task<object> KendinSifreDegistir([FromBody] SifreDegistirDto dto)
        => servis.KendinSifreDegistir(dto, Islem);

    /// <summary>GET /api/v1/kullanicilar/{id} — Kullanıcı detayı — SAHIP, MUDUR</summary>
    [HttpGet("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id} — Kullanıcı güncelle — SAHIP, MUDUR</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] KullaniciGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id}/rol — Rol değiştir — SAHIP, MUDUR</summary>
    [HttpPatch("{id}/rol")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> RolDegistir(string id, [FromBody] RolDegistirDto dto)
        => servis.RolDegistir(id, dto, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id}/sifre — Admin şifre sıfırlama — SAHIP, MUDUR</summary>
    [HttpPatch("{id}/sifre")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AdminSifreSifirla(string id, [FromBody] SifreDegistirDto dto)
        => servis.AdminSifreSifirla(id, dto, Islem);

    /// <summary>DELETE /api/v1/kullanicilar/{id} — Kullanıcıyı deaktif et — SAHIP</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
