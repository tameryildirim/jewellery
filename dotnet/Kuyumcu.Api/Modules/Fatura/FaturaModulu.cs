using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA MODÜLÜ — KasaModulu desenini izler.
// Kaynak: apps/api/src/modules/fatura/*
//   • Durum makinesi: TASLAK→[KESILDI,IPTAL], KESILDI→[ODENDI,IPTAL],
//     ODENDI→[IPTAL], IPTAL→[]
//   • Fatura no: FTR-{yıl}-{sıra:6 hane} (Banka modülündeki dekont no formatıyla aynı desen)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class FaturaOlusturDto
{
    [Required, StringLength(36, MinimumLength = 26)]
    public string CariId { get; set; } = null!;

    [RegularExpression("SATIS|ALIS")]
    public string? Tur { get; set; }

    /// <summary>KDV dahil toplam tutar</summary>
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Toplam sıfırdan büyük olmalıdır")]
    public decimal Toplam { get; set; }

    [Range(0.0, 1.0)]
    public decimal? KdvOran { get; set; }

    /// <summary>STANDART (varsayılan): KDV tüm tutara. KUYUM: has altın KDV'siz, KDV yalnız işçilik+kâra.</summary>
    [RegularExpression("STANDART|KUYUM")]
    public string? KdvTuru { get; set; }

    /// <summary>KUYUM türünde matrahtan düşülen has altın bedeli (KDV'siz)</summary>
    [Range(0.0, double.MaxValue)]
    public decimal? HasDeger { get; set; }

    [RegularExpression("TRY|USD|EUR|GBP")]
    public string? ParaBirimi { get; set; }

    public string? Aciklama { get; set; }

    /// <summary>ISO tarih; verilmezse şimdi</summary>
    public DateTime? Tarih { get; set; }
}

public sealed class DurumGuncelleDto
{
    [Required, RegularExpression("TASLAK|KESILDI|ODENDI|IPTAL")]
    public string Durum { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class FaturaServisi(KuyumcuDbContext db)
{
    /// <summary>İptal edilen fatura başka duruma alınamaz</summary>
    private static readonly Dictionary<string, string[]> GecerliGecisler = new()
    {
        ["TASLAK"]  = ["KESILDI", "IPTAL"],
        ["KESILDI"] = ["ODENDI", "IPTAL"],
        ["ODENDI"]  = ["IPTAL"],
        ["IPTAL"]   = [],
    };

    // ─── Manuel fatura oluştur ────────────────────────────────────────────────

    public async Task<object> Olustur(FaturaOlusturDto dto, AktifKullanici islem)
    {
        var cariVar = await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.Id == dto.CariId && c.TenantId == islem.TenantId);
        if (!cariVar) throw new NotFoundException("Cari bulunamadı");

        var kdvOran = dto.KdvOran ?? 0.20m;
        var kdvTuru = dto.KdvTuru ?? "STANDART";
        var toplam  = dto.Toplam;

        // KDV hesabı türe göre:
        //  STANDART → KDV tüm tutara
        //  KUYUM    → has altın KDV'siz; KDV yalnız (toplam − hasDeger) matrahına
        decimal hasDeger = 0m;
        decimal kdvTutar;
        if (kdvTuru == "KUYUM")
        {
            hasDeger = dto.HasDeger ?? 0m;
            if (hasDeger >= toplam)
                throw new BadRequestException("Has altın değeri toplam tutardan küçük olmalıdır");
            var matrahKdvDahil = toplam - hasDeger;          // işçilik+kâr, KDV dahil
            kdvTutar = matrahKdvDahil - matrahKdvDahil / (1m + kdvOran);
        }
        else
        {
            kdvTutar = toplam - toplam / (1m + kdvOran);
        }
        var araToplam = toplam - kdvTutar;                    // KDV hariç toplam (her iki türde)

        var id    = Kimlik.Ulid();
        var now   = DateTime.UtcNow;
        var tarih = dto.Tarih ?? now;

        db.Faturalar.Add(new Data.Fatura
        {
            Id             = id,
            TenantId       = islem.TenantId,
            FaturaNo       = await SiradakiFaturaNo(islem.TenantId, tarih.Year),
            CariId         = dto.CariId,
            Tur            = dto.Tur ?? "SATIS",
            KdvTuru        = kdvTuru,
            HasDeger       = Math.Round(hasDeger, 2, MidpointRounding.AwayFromZero),
            AraToplam      = Math.Round(araToplam, 2, MidpointRounding.AwayFromZero),
            KdvOran        = Math.Round(kdvOran, 4, MidpointRounding.AwayFromZero),
            KdvTutar       = Math.Round(kdvTutar, 2, MidpointRounding.AwayFromZero),
            Toplam         = Math.Round(toplam, 2, MidpointRounding.AwayFromZero),
            ParaBirimi     = dto.ParaBirimi ?? "TRY",
            Durum          = "TASLAK",
            BankaHareketId = null,
            Aciklama       = dto.Aciklama,
            Tarih          = tarih,
            Olusturuldu    = now,
            Guncellendi    = now,
        });
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        AktifKullanici islem, string? durum, string? cariId, int limit, int offset)
    {
        var q = db.Faturalar.AsNoTracking().Where(f => f.TenantId == islem.TenantId);
        if (durum is "TASLAK" or "KESILDI" or "ODENDI" or "IPTAL")
            q = q.Where(f => f.Durum == durum);
        if (!string.IsNullOrEmpty(cariId)) q = q.Where(f => f.CariId == cariId);

        var liste = await q.OrderByDescending(f => f.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        // Cari adlarını tek sorguda topla
        var cariIdler = liste.Select(f => f.CariId).Distinct().ToList();
        var cariAdMap = cariIdler.Count == 0
            ? new Dictionary<string, string>()
            : await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Ad);

        return new
        {
            faturalar = liste
                .Select(f => CariliYanit(f, cariAdMap.GetValueOrDefault(f.CariId)))
                .ToList(),
        };
    }

    // ─── Tekil ────────────────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == fatura.CariId && c.TenantId == islem.TenantId);
        return CariliYanit(fatura, cari?.Ad);
    }

    // ─── Durum güncelle ───────────────────────────────────────────────────────

    public async Task<object> DurumGuncelle(string id, DurumGuncelleDto dto, AktifKullanici islem)
    {
        var fatura = await db.Faturalar
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        var izinli = GecerliGecisler.GetValueOrDefault(fatura.Durum) ?? [];
        if (!izinli.Contains(dto.Durum))
            throw new BadRequestException($"{fatura.Durum} durumundan {dto.Durum} durumuna geçilemez");

        fatura.Durum = dto.Durum;
        fatura.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Yardımcı ─────────────────────────────────────────────────────────────

    private async Task<string> SiradakiFaturaNo(string tenantId, int yil)
    {
        var onEk = $"FTR-{yil}-";
        var adet = await db.Faturalar.AsNoTracking()
            .CountAsync(f => f.TenantId == tenantId && EF.Functions.Like(f.FaturaNo, onEk + "%"));
        return $"FTR-{yil}-{adet + 1:D6}";
    }

    /// <summary>Nest'teki { ...fatura, cariAd } yanıt şekli.</summary>
    private static object CariliYanit(Data.Fatura f, string? cariAd) => new
    {
        f.Id,
        f.TenantId,
        f.FaturaNo,
        f.CariId,
        f.Tur,
        f.KdvTuru,
        f.HasDeger,
        f.AraToplam,
        f.KdvOran,
        f.KdvTutar,
        f.Toplam,
        f.ParaBirimi,
        f.Durum,
        f.BankaHareketId,
        f.Aciklama,
        f.Tarih,
        f.Olusturuldu,
        f.Guncellendi,
        CariAd = cariAd ?? "—",
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/faturalar")]
[Yetki.ModulYetkisi("fatura")]
public sealed class FaturaController(FaturaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/faturalar — Manuel fatura oluştur — MUHASEBE+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Olustur([FromBody] FaturaOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/faturalar?durum=KESILDI&amp;cariId=... — Fatura listesi</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? durum, [FromQuery] string? cariId,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.Listele(Islem, durum, cariId, limit, offset);

    /// <summary>GET /api/v1/faturalar/{id} — Fatura detayı</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/faturalar/{id}/durum — Durum geçişi — MUHASEBE+</summary>
    [HttpPatch("{id}/durum")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> DurumGuncelle(string id, [FromBody] DurumGuncelleDto dto)
        => servis.DurumGuncelle(id, dto, Islem);
}
