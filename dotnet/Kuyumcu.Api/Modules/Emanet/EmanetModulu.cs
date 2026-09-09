using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Api.Modules.Emanet;

// ═════════════════════════════════════════════════════════════════════════════
// EMANET MODÜLÜ — müşteri altınının teslim alınması (giriş) ve iadesi (çıkış).
// Paralel defterde yalnız has gram bacağı çalışır (tlTutari = 0).
//   • Giriş:  hasGramBakiye += hasGrami (bizde müşterinin altını var)
//   • Çıkış:  hasGramBakiye -= hasGrami (iade — mevcut bakiye yeterli olmalı)
// Kaynak: apps/api/src/modules/emanet/*
// Desen: Modules/Kasa/KasaModulu.cs (tek dosya: DTO'lar + Servis + Controller)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class EmanetGirisDto
{
    /// <summary>Müşteri cari ID</summary>
    [Required] public string CariId { get; set; } = null!;

    /// <summary>Emanet altının ayarı</summary>
    [Required, RegularExpression("K8|K14|K18|K21|K22|K24|G800|G925|G950")]
    public string AyarId { get; set; } = null!;

    /// <summary>Fiziksel gram</summary>
    [Required, Range(0.001, double.MaxValue)]
    public decimal GramAgirlik { get; set; }

    /// <summary>İsteğe bağlı taş ağırlığı</summary>
    [Range(0, double.MaxValue)] public decimal? TasAgirlik { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class EmanetCikisDto
{
    /// <summary>
    /// İade edilen has gram — girişte kayıtlı değerle aynı olmalı,
    /// fark varsa aciklama zorunlu.
    /// </summary>
    [Required, Range(0.001, double.MaxValue)]
    public decimal HasGrami { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class EmanetServisi(KuyumcuDbContext db, ILogger<EmanetServisi> logger)
{
    // ─── Emanet girişi ────────────────────────────────────────────────────────
    /// <summary>
    /// Müşterinin altınını teslim alır.
    /// hasGramBakiye += hasGrami (bizde müşterinin altını var → müşteri bize emanet bıraktı)
    /// hareketler: EMANET_GIRIS, tlTutari=0, hasGrami=+hasGrami
    /// </summary>
    public async Task<object> Giris(EmanetGirisDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Emanet için şube seçimi gerekli");

        var ayar = Ayarlar.Bul(dto.AyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.AyarId}");

        var tasAgirlik = dto.TasAgirlik ?? 0m;
        var netGram    = dto.GramAgirlik - tasAgirlik;
        var hasGrami   = HasHesap.ToHasGram(netGram, ayar);
        var now        = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Hareketler.Add(new Hareket
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            SubeId        = islem.SubeId,
            Tur           = "EMANET_GIRIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = hasGrami,          // pozitif: emanet aldık
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Müşteri has gram bakiyesi artar — bizde altını var
        await BakiyeGuncelle(dto.CariId, islem.TenantId, 0m, hasGrami, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] EMANET_GIRIS: cari={CariId} {Gram}g {AyarId} = {Has} has",
            islem.TenantId, dto.CariId, dto.GramAgirlik, dto.AyarId, hasGrami);

        return new
        {
            cariId      = dto.CariId,
            ayarId      = dto.AyarId,
            gramAgirlik = dto.GramAgirlik,
            hasGrami,
        };
    }

    // ─── Emanet çıkışı ────────────────────────────────────────────────────────
    /// <summary>
    /// Müşteriye altınını iade eder.
    /// hasGramBakiye -= hasGrami
    /// hareketler: EMANET_CIKIS, tlTutari=0, hasGrami=-hasGrami
    /// </summary>
    public async Task<object> Cikis(string cariId, EmanetCikisDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Emanet iadesi için şube seçimi gerekli");

        // Mevcut emanet bakiyesini kontrol et
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == cariId && b.TenantId == islem.TenantId);

        var mevcutHas = bakiye?.HasGramBakiye ?? 0m;
        if (mevcutHas < dto.HasGrami)
        {
            throw new BadRequestException(
                $"Yetersiz emanet bakiyesi. Mevcut: {mevcutHas.ToString("0.0000", CultureInfo.InvariantCulture)} has gram, " +
                $"Talep: {dto.HasGrami.ToString(CultureInfo.InvariantCulture)}");
        }

        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Hareketler.Add(new Hareket
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            SubeId        = islem.SubeId,
            Tur           = "EMANET_CIKIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = -dto.HasGrami,     // negatif: iade ettik
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = cariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Müşteri has gram bakiyesi azalır
        await BakiyeGuncelle(cariId, islem.TenantId, 0m, -dto.HasGrami, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] EMANET_CIKIS: cari={CariId} {Has} has iade",
            islem.TenantId, cariId, dto.HasGrami);

        return new { cariId, iadeEdilenHas = dto.HasGrami };
    }

    // ─── Cari emanet listesi ──────────────────────────────────────────────────

    public async Task<object> CariEmanetListesi(string cariId, AktifKullanici islem)
    {
        // Nest davranışı birebir: son 200 hareket çekilir,
        // EMANET_GIRIS/EMANET_CIKIS filtresi bellekte uygulanır.
        var liste = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.CariId == cariId)
            .OrderByDescending(h => h.Tarih)
            .Take(200)
            .ToListAsync();

        var emanetler = liste
            .Where(h => h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS")
            .ToList();

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == cariId && b.TenantId == islem.TenantId);

        return new
        {
            cariId,
            mevcutHasGramBakiye = bakiye?.HasGramBakiye ?? 0m,
            hareketler          = emanetler,
        };
    }

    // ─── Yardımcı ─────────────────────────────────────────────────────────────

    /// <summary>Bakiye satırını delta ile günceller — çağıranın transaction'ı içinde çalışır.</summary>
    private async Task BakiyeGuncelle(
        string cariId, string tenantId, decimal tlDelta, decimal hasGramDelta, DateTime now)
    {
        var mevcut = await db.CariBakiyeler.FirstOrDefaultAsync(b => b.CariId == cariId);

        if (mevcut is not null)
        {
            mevcut.TlBakiye += tlDelta;
            mevcut.HasGramBakiye += hasGramDelta;
            mevcut.SonIslemTarihi = now;
            mevcut.Guncellendi = now;
        }
        else
        {
            db.CariBakiyeler.Add(new CariBakiye
            {
                CariId         = cariId,
                TenantId       = tenantId,
                TlBakiye       = tlDelta,
                HasGramBakiye  = hasGramDelta,
                SonIslemTarihi = now,
                Guncellendi    = now,
            });
        }
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/emanet")]
public sealed class EmanetController(EmanetServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/emanet/giris — Müşteriden altın teslim al</summary>
    [HttpPost("giris")]
    public Task<object> Giris([FromBody] EmanetGirisDto dto) => servis.Giris(dto, Islem);

    /// <summary>POST /api/v1/emanet/{cariId}/cikis — Müşteriye altını iade et</summary>
    [HttpPost("{cariId}/cikis")]
    public Task<object> Cikis(string cariId, [FromBody] EmanetCikisDto dto)
        => servis.Cikis(cariId, dto, Islem);

    /// <summary>GET /api/v1/emanet/{cariId} — Cari bazlı emanet geçmişi + güncel bakiye</summary>
    [HttpGet("{cariId}")]
    public Task<object> CariEmanetListesi(string cariId)
        => servis.CariEmanetListesi(cariId, Islem);
}
