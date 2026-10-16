using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Yerlesim;

// ═════════════════════════════════════════════════════════════════════════════
// YERLEŞİM UÇLARI — paket 25 Faz A.
//
// Yol `api/v1/reyonlar` altındadır (reyonun içeriği reyonun işidir), ama reyon
// KARTINI yöneten uçlar KategoriModulu.ReyonController'da kalır — ikinci bir
// reyon yönetim yüzeyi açılmaz.
//
// Her yazma ucu: [ModulYetkisi("urun")] + [RolGerekli] TAM eşleşme + transaction
// + DenetimServisi.YazmaKaydet (DenetimIzi.cs:135 — yeniden yazılmaz, çağrılır).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YerlestirDto
{
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Required, MaxLength(36)] public string ReyonId { get; set; } = null!;

    [Range(0.001, 999999.999, ErrorMessage = "Adet sıfırdan büyük olmalıdır")]
    public decimal Adet { get; set; }
}

public sealed class TasimaDto
{
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Required, MaxLength(36)] public string KaynakReyonId { get; set; } = null!;
    [Required, MaxLength(36)] public string HedefReyonId { get; set; } = null!;

    [Range(0.001, 999999.999, ErrorMessage = "Adet sıfırdan büyük olmalıdır")]
    public decimal Adet { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }
}

public sealed class GeriAlDto
{
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Required, MaxLength(36)] public string ReyonId { get; set; } = null!;

    [Range(0.001, 999999.999, ErrorMessage = "Adet sıfırdan büyük olmalıdır")]
    public decimal Adet { get; set; }
}

[ApiController]
[Route("api/v1/reyonlar")]
[Authorize]
[Yetki.ModulYetkisi("urun")]
public sealed class YerlesimController(YerlesimServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/reyonlar/{id}/stok — Reyonun içeriği (sayfalı).</summary>
    [HttpGet("{id}/stok")]
    public Task<object> ReyonStoku(string id, [FromQuery] int limit = 200, [FromQuery] int ofset = 0)
        => servis.ReyonIcerigi(Islem, id, limit, ofset);

    /// <summary>POST /api/v1/reyonlar/yerlesim — Atanmamış maldan reyona yerleştir.</summary>
    [HttpPost("yerlesim")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public async Task<object> Yerlestir([FromBody] YerlestirDto dto)
    {
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        await servis.Yerlestir(Islem, dto.SubeId, dto.UrunId, dto.ReyonId, dto.Adet, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await Sonuc(dto.SubeId, dto.UrunId, "Mal reyona yerleştirildi");
    }

    /// <summary>POST /api/v1/reyonlar/tasima — Aynı şube içinde reyon → reyon taşı.</summary>
    [HttpPost("tasima")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public async Task<object> Tasi([FromBody] TasimaDto dto)
    {
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        await servis.Tasi(Islem, dto.SubeId, dto.UrunId, dto.KaynakReyonId, dto.HedefReyonId,
                          dto.Adet, dto.Aciklama, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await Sonuc(dto.SubeId, dto.UrunId, "Mal reyonlar arasında taşındı");
    }

    /// <summary>POST /api/v1/reyonlar/geri-al — Reyondan atanmamışa geri çek.</summary>
    [HttpPost("geri-al")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<object> GeriAl([FromBody] GeriAlDto dto)
    {
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        await servis.GeriAl(Islem, dto.SubeId, dto.UrunId, dto.ReyonId, dto.Adet, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await Sonuc(dto.SubeId, dto.UrunId, "Mal atanmamışa geri alındı");
    }

    /// <summary>UX-S23/UX-S27: yazma sonucu AYNI yanıtta görünür — ekran ikinci istek atmaz.</summary>
    private async Task<object> Sonuc(string subeId, string urunId, string mesaj) => new
    {
        mesaj,
        subeId,
        urunId,
        stokAdedi = await servis.StokAdedi(Islem.TenantId, subeId, urunId),
        yerlesikAdet = await servis.YerlesikToplam(Islem.TenantId, subeId, urunId),
        atanmamis = await servis.Atanmamis(Islem.TenantId, subeId, urunId),
    };
}

[ApiController]
[Route("api/v1/urunler")]
[Authorize]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunYerlesimController(YerlesimServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/urunler/{id}/yerlesim — Ürünün şube × reyon dağılımı.</summary>
    [HttpGet("{id}/yerlesim")]
    public Task<object> Yerlesim(string id) => servis.UrunYerlesimi(Islem, id);
}
