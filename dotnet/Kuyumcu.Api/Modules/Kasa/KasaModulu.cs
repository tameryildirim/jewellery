using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kasa;

// ═════════════════════════════════════════════════════════════════════════════
// ÖRNEK MODÜL — diğer modüller bu deseni birebir izler:
//   • Namespace: Kuyumcu.Api.Modules.{Modul}
//   • Tek dosya: DTOs + Servis + Controller
//   • Rota ve JSON alan adları NestJS API'siyle birebir aynı (Türkçe camelCase)
//   • Hatalar Common.ApiException türevleriyle (NotFound/BadRequest/Conflict...)
//   • Çok tablolu yazmalar tek EF transaction içinde
// Kaynak: apps/api/src/modules/kasa/*
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KasaOlusturDto
{
    [Required] public string Ad { get; set; } = null!;
    [Required] public string SubeId { get; set; } = null!;
    public string? ParaBirimi { get; set; }
}

public sealed class KasaGuncelleDto
{
    [Required(ErrorMessage = "Kasa adı gereklidir")]
    [MinLength(2, ErrorMessage = "Kasa adı en az 2 karakter olmalıdır")]
    [MaxLength(100, ErrorMessage = "Kasa adı en fazla 100 karakter olabilir")]
    public string Ad { get; set; } = null!;
}

public sealed class KasaHareketDto
{
    [Required, RegularExpression("GIRIS|CIKIS", ErrorMessage = "Yön GIRIS veya CIKIS olmalıdır")]
    public string Yon { get; set; } = null!;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal Tutar { get; set; }

    [Required, RegularExpression("NAKIT|KART|HAVALE|ALTIN|DIGER")]
    public string OdemeTuru { get; set; } = null!;

    [Range(0, 1)] public decimal? KomisyonOran { get; set; }
    public string? Aciklama { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KasaServisi(KuyumcuDbContext db)
{
    public async Task<object> Olustur(KasaOlusturDto dto, AktifKullanici islem)
    {
        var mevcutAd = await db.Kasalar.AnyAsync(k =>
            k.TenantId == islem.TenantId && k.SubeId == dto.SubeId && k.Ad == dto.Ad);
        if (mevcutAd) throw new ConflictException($"'{dto.Ad}' adlı kasa bu şubede zaten mevcut");

        var kasa = new Data.Kasa
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = dto.Ad,
            ParaBirimi = dto.ParaBirimi ?? "TRY",
            Bakiye = 0m,
            Aktif = true,
            Guncellendi = DateTime.UtcNow,
        };
        db.Kasalar.Add(kasa);
        await db.SaveChangesAsync();
        return await Getir(kasa.Id, islem);
    }

    public async Task<List<Data.Kasa>> Listele(AktifKullanici islem, string? subeId)
    {
        var q = db.Kasalar.AsNoTracking().Where(k => k.TenantId == islem.TenantId);
        if (!string.IsNullOrEmpty(subeId)) q = q.Where(k => k.SubeId == subeId);
        return await q.ToListAsync();
    }

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var kasa = await db.Kasalar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        return kasa;
    }

    public async Task<object> Guncelle(string id, KasaGuncelleDto dto, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);

        // Aynı şubede aynı ada sahip başka kasa varsa çakışma
        var mevcutAd = await db.Kasalar.AnyAsync(k =>
            k.TenantId == islem.TenantId && k.SubeId == kasa.SubeId && k.Ad == dto.Ad && k.Id != id);
        if (mevcutAd) throw new ConflictException($"'{dto.Ad}' adlı kasa bu şubede zaten mevcut");

        kasa.Ad = dto.Ad;
        kasa.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    /// <summary>Satış/alış dışı nakit hareketleri: gider, transfer, sayım farkı vb.</summary>
    public async Task<object> HareketGir(string id, KasaHareketDto dto, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);

        var yonKatsayi = dto.Yon == "GIRIS" ? 1m : -1m;
        var komisyon = dto.KomisyonOran ?? 0m;
        var netTutar = dto.Tutar * yonKatsayi * (1m - komisyon);
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        kasa.Bakiye += netTutar;
        kasa.Guncellendi = now;

        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            KasaId = id,
            Tutar = dto.Tutar * yonKatsayi,
            ParaBirimi = kasa.ParaBirimi,
            OdemeTuru = dto.OdemeTuru,
            KomisyonOran = komisyon,
            Tarih = now,
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Getir(id, islem);
    }

    public async Task<object> HareketListele(
        string id, AktifKullanici islem,
        DateTime? baslangic, DateTime? bitis, int limit, int offset)
    {
        await TenantKasasiBul(id, islem.TenantId);

        var q = db.KasaHareketleri.AsNoTracking()
            .Where(h => h.KasaId == id && h.TenantId == islem.TenantId);
        if (baslangic is not null) q = q.Where(h => h.Tarih >= baslangic);
        if (bitis is not null) q = q.Where(h => h.Tarih <= bitis);

        var liste = await q.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        var kasa = await Getir(id, islem);
        return new { kasa, hareketler = liste };
    }

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        if (kasa.Bakiye != 0m)
            throw new BadRequestException("Bakiyesi sıfır olmayan kasa deaktif edilemez");

        kasa.Aktif = false;
        kasa.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new { mesaj = "Kasa deaktif edildi" };
    }

    internal async Task<Data.Kasa> TenantKasasiBul(string id, string tenantId) =>
        await db.Kasalar.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId)
        ?? throw new NotFoundException("Kasa bulunamadı");
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kasalar")]
[Yetki.ModulYetkisi("kasa")]
public sealed class KasaController(KasaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/kasalar — Kasa oluştur — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] KasaOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/kasalar?subeId=...</summary>
    [HttpGet]
    public Task<List<Data.Kasa>> Listele([FromQuery] string? subeId) => servis.Listele(Islem, subeId);

    /// <summary>GET /api/v1/kasalar/{id}</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>GET /api/v1/kasalar/{id}/hareketler</summary>
    [HttpGet("{id}/hareketler")]
    public Task<object> HareketListele(
        string id,
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.HareketListele(id, Islem, baslangic, bitis, limit, offset);

    /// <summary>POST /api/v1/kasalar/{id}/hareket — MUHASEBE+</summary>
    [HttpPost("{id}/hareket")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketGir(string id, [FromBody] KasaHareketDto dto)
        => servis.HareketGir(id, dto, Islem);

    /// <summary>PATCH /api/v1/kasalar/{id} — Kasa adını güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] KasaGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>DELETE /api/v1/kasalar/{id} — SAHIP</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
