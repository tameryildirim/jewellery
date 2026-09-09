using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// Ürün modülü — kaynak: apps/api/src/modules/urun/*
//   • Ürün tanımı (SKU), hasGrami hesabı (gram − taş) × milyem / 1000
//   • Şube bazlı stok görünümü + sayım düzeltmesi
//   • Barkodla hızlı arama (POS)
// Rota ve JSON alan adları NestJS API'siyle birebir aynı.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class UrunOlusturDto
{
    [Required, RegularExpression("ZIYNET|KULCE|HURDA|SIKKE|PIRLANTA|SAAT|GUMUS")]
    public string Tip { get; set; } = null!;

    /// <summary>Stok kodu / SKU</summary>
    [Required, MaxLength(100)]
    public string Kod { get; set; } = null!;

    [Required, MaxLength(300)]
    public string Ad { get; set; } = null!;

    [MaxLength(1000)] public string? Aciklama { get; set; }

    [Required, RegularExpression("K8|K14|K18|K21|K22|K24|G800|G925|G950")]
    public string AyarId { get; set; } = null!;

    /// <summary>Gerçek milyem (1000 tabanlı); boşsa ayarın standart milyemi. Has hesabı buna göre.</summary>
    [Range(1, 1000)] public decimal? GercekMilyem { get; set; }

    /// <summary>Fiziksel brüt gram (taşlar dahil)</summary>
    [Required, Range(0.0001, double.MaxValue)]
    public decimal GramAgirlik { get; set; }

    /// <summary>Taş ağırlığı gram — has hesabından düşülür</summary>
    [Range(0, double.MaxValue)] public decimal? TasAgirlik { get; set; }

    /// <summary>Sabit işçilik TL</summary>
    [Range(0, double.MaxValue)] public decimal? IscilikTl { get; set; }

    /// <summary>Opsiyonel sabit satış fiyatı; boşsa satışta hesaplanır</summary>
    [Range(0, double.MaxValue)] public decimal? SatisFiyati { get; set; }

    [MaxLength(50)] public string? Barkod { get; set; }
    [MaxLength(100)] public string? Rfid { get; set; }
    [MaxLength(500)] public string? GorselUrl { get; set; }
}

public sealed class UrunGuncelleDto
{
    [RegularExpression("ZIYNET|KULCE|HURDA|SIKKE|PIRLANTA|SAAT|GUMUS")]
    public string? Tip { get; set; }

    [MaxLength(100)] public string? Kod { get; set; }
    [MaxLength(300)] public string? Ad { get; set; }
    [MaxLength(1000)] public string? Aciklama { get; set; }

    [RegularExpression("K8|K14|K18|K21|K22|K24|G800|G925|G950")]
    public string? AyarId { get; set; }

    /// <summary>Gerçek milyem (1000 tabanlı); boşsa ayarın standart milyemi</summary>
    [Range(1, 1000)] public decimal? GercekMilyem { get; set; }

    [Range(0.0001, double.MaxValue)] public decimal? GramAgirlik { get; set; }
    [Range(0, double.MaxValue)] public decimal? TasAgirlik { get; set; }
    [Range(0, double.MaxValue)] public decimal? IscilikTl { get; set; }

    /// <summary>Opsiyonel sabit satış fiyatı; 0 = temizle (hesaplamaya dön)</summary>
    [Range(0, double.MaxValue)] public decimal? SatisFiyati { get; set; }

    [MaxLength(50)] public string? Barkod { get; set; }
    [MaxLength(100)] public string? Rfid { get; set; }
    [MaxLength(500)] public string? GorselUrl { get; set; }
    public bool? Aktif { get; set; }
}

/// <summary>PATCH {id}/stok/{subeId} gövdesi — Nest'teki @Body('adet').</summary>
public sealed class StokSayimDto
{
    public decimal Adet { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class UrunServisi(KuyumcuDbContext db)
{
    // ─── Yardımcı: ayarId → hasGrami hesabı ──────────────────────────────────

    private static Ayar AyarBul(string ayarId) =>
        Ayarlar.Bul(ayarId) ?? throw new BadRequestException($"Geçersiz ayar: {ayarId}");

    /// <summary>
    /// Has gram = (gram − taş) × milyem/1000. gercekMilyem verilirse imalattaki
    /// fiili saflık kullanılır, yoksa ayar standardı.
    /// </summary>
    private static decimal HesaplaHasGrami(decimal gramAgirlik, decimal tasAgirlik, string ayarId, decimal? gercekMilyem = null)
    {
        var ayar = AyarBul(ayarId); // geçersiz ayar → 400
        var efektifAyar = gercekMilyem.HasValue ? ayar with { Milyem = gercekMilyem.Value } : ayar;
        return HasHesap.ToHasGram(gramAgirlik - tasAgirlik, efektifAyar);
    }

    // ─── Ürün oluştur ─────────────────────────────────────────────────────────

    public async Task<object> Olustur(UrunOlusturDto dto, AktifKullanici islem)
    {
        var ayar = AyarBul(dto.AyarId);

        // Stok kodu tenant içinde benzersiz olmalı
        var mevcutKod = await db.Urunler.AnyAsync(u =>
            u.TenantId == islem.TenantId && u.Kod == dto.Kod);
        if (mevcutKod) throw new ConflictException($"'{dto.Kod}' kodu zaten kullanımda");

        var tasAgirlik    = dto.TasAgirlik ?? 0m;
        var efektifMilyem = dto.GercekMilyem ?? ayar.Milyem;
        var hasGrami      = HesaplaHasGrami(dto.GramAgirlik, tasAgirlik, dto.AyarId, dto.GercekMilyem);
        var now           = DateTime.UtcNow;
        var id            = Kimlik.Ulid();

        db.Urunler.Add(new Data.Urun
        {
            Id          = id,
            TenantId    = islem.TenantId,
            Tip         = dto.Tip,
            Kod         = dto.Kod,
            Ad          = dto.Ad,
            Aciklama    = dto.Aciklama,
            AyarId      = dto.AyarId,
            Milyem      = efektifMilyem,
            GramAgirlik = dto.GramAgirlik,
            TasAgirlik  = tasAgirlik,
            HasGrami    = hasGrami,
            IscilikTl   = dto.IscilikTl ?? 0m,
            SatisFiyati = dto.SatisFiyati,
            Barkod      = dto.Barkod,
            Rfid        = dto.Rfid,
            GorselUrl   = dto.GorselUrl,
            Aktif       = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<List<Data.Urun>> Listele(
        AktifKullanici islem,
        string? tip, string? ayarId, bool? aktif, string? q,
        int limit, int offset)
    {
        var sorgu = db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId);

        if (!string.IsNullOrEmpty(tip))    sorgu = sorgu.Where(u => u.Tip == tip);
        if (!string.IsNullOrEmpty(ayarId)) sorgu = sorgu.Where(u => u.AyarId == ayarId);
        if (aktif is not null)             sorgu = sorgu.Where(u => u.Aktif == aktif);
        if (!string.IsNullOrEmpty(q))
            sorgu = sorgu.Where(u =>
                EF.Functions.Like(u.Ad, $"%{q}%") || EF.Functions.Like(u.Kod, $"%{q}%"));

        return await sorgu.OrderBy(u => u.Kod).Skip(offset).Take(limit).ToListAsync();
    }

    // ─── Tekil ürün ───────────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");
        return urun;
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, UrunGuncelleDto dto, AktifKullanici islem)
    {
        var mevcut = await TenantUrunuBul(id, islem.TenantId);

        // Stok kodu çakışma kontrolü
        if (dto.Kod is not null && dto.Kod != mevcut.Kod)
        {
            var cakisan = await db.Urunler.AnyAsync(u =>
                u.TenantId == islem.TenantId && u.Kod == dto.Kod);
            if (cakisan) throw new ConflictException($"'{dto.Kod}' kodu zaten kullanımda");
        }

        var now = DateTime.UtcNow;
        var hasEtkileyen =
            dto.GramAgirlik is not null || dto.TasAgirlik is not null ||
            dto.AyarId is not null || dto.GercekMilyem is not null;

        await using var tx = await db.Database.BeginTransactionAsync();

        if (dto.Tip is not null)       mevcut.Tip = dto.Tip;
        if (dto.Kod is not null)       mevcut.Kod = dto.Kod;
        if (dto.Ad is not null)        mevcut.Ad = dto.Ad;
        if (dto.Aciklama is not null)  mevcut.Aciklama = dto.Aciklama;
        if (dto.Barkod is not null)    mevcut.Barkod = dto.Barkod;
        if (dto.Rfid is not null)      mevcut.Rfid = dto.Rfid;
        if (dto.GorselUrl is not null) mevcut.GorselUrl = dto.GorselUrl;
        if (dto.IscilikTl is not null) mevcut.IscilikTl = dto.IscilikTl.Value;
        // 0 gönderilirse sabit fiyat temizlenir (satışta hesaplamaya dönülür)
        if (dto.SatisFiyati is not null) mevcut.SatisFiyati = dto.SatisFiyati.Value > 0 ? dto.SatisFiyati.Value : null;
        if (dto.Aktif is not null)     mevcut.Aktif = dto.Aktif.Value;

        // Gram, taş, ayar veya gerçek milyem değiştiyse hasGrami yeniden hesapla
        if (hasEtkileyen)
        {
            var yeniGram   = dto.GramAgirlik ?? mevcut.GramAgirlik;
            var yeniTas    = dto.TasAgirlik ?? mevcut.TasAgirlik ?? 0m;
            var yeniAyarId = dto.AyarId ?? mevcut.AyarId;

            if (dto.AyarId is not null) mevcut.AyarId = yeniAyarId;

            // Efektif milyem: verilirse gerçek milyem; yoksa ayar değiştiyse standardı; yoksa mevcut
            var efektifMilyem = dto.GercekMilyem
                ?? (dto.AyarId is not null ? AyarBul(yeniAyarId).Milyem : mevcut.Milyem);

            mevcut.Milyem   = efektifMilyem;
            mevcut.HasGrami = HesaplaHasGrami(yeniGram, yeniTas, yeniAyarId, efektifMilyem);
            if (dto.GramAgirlik is not null) mevcut.GramAgirlik = yeniGram;
            if (dto.TasAgirlik is not null)  mevcut.TasAgirlik = dto.TasAgirlik.Value;

            // Stokların toplamHasGram'ını yeni has gramına göre güncelle
            var subeStoklar = await db.Stoklar.Where(s => s.UrunId == id).ToListAsync();
            foreach (var s in subeStoklar)
            {
                s.ToplamHasGram = Math.Round(s.Adet * mevcut.HasGrami, 6);
                s.Guncellendi = now;
            }
        }

        mevcut.Guncellendi = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Getir(id, islem);
    }

    // ─── Deaktif et ───────────────────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var urun = await TenantUrunuBul(id, islem.TenantId);

        urun.Aktif = false;
        urun.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new { mesaj = "Ürün deaktif edildi" };
    }

    // ─── Stok listesi (ürün bazlı, tüm şubeler) ──────────────────────────────

    public async Task<object> StokListele(string id, AktifKullanici islem)
    {
        await TenantUrunuBul(id, islem.TenantId);

        var stokListesi = await db.Stoklar.AsNoTracking()
            .Where(s => s.UrunId == id && s.TenantId == islem.TenantId)
            .ToListAsync();

        return new
        {
            urunId        = id,
            toplamAdet    = stokListesi.Sum(s => s.Adet),
            toplamHasGram = stokListesi.Sum(s => s.ToplamHasGram),
            subeStoklar   = stokListesi.Select(s => new
            {
                subeId        = s.SubeId,
                adet          = s.Adet,
                toplamHasGram = s.ToplamHasGram,
                guncellendi   = s.Guncellendi,
            }).ToList(),
        };
    }

    // ─── Şube stoku sayım güncelleme ──────────────────────────────────────────
    /// <summary>
    /// Sayım düzeltmesi — mevcut adet üzerine yazan (replace) işlem.
    /// Genellikle sayım modülü kullanır; satış/alış kendi stok güncellemesini yapar.
    /// </summary>
    public async Task<object> StokSayimGuncelle(
        string id, string subeId, decimal yeniAdet, AktifKullanici islem)
    {
        var urun = await TenantUrunuBul(id, islem.TenantId);
        var toplamHasGram = Math.Round(yeniAdet * urun.HasGrami, 6);
        var now = DateTime.UtcNow;

        var mevcut = await db.Stoklar
            .FirstOrDefaultAsync(s => s.UrunId == id && s.SubeId == subeId);

        if (mevcut is not null)
        {
            mevcut.Adet          = Math.Round(yeniAdet, 3);
            mevcut.ToplamHasGram = toplamHasGram;
            mevcut.Guncellendi   = now;
        }
        else
        {
            db.Stoklar.Add(new Stok
            {
                Id            = Kimlik.Ulid(),
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                UrunId        = id,
                Adet          = Math.Round(yeniAdet, 3),
                ToplamHasGram = toplamHasGram,
                Guncellendi   = now,
            });
        }
        await db.SaveChangesAsync();

        return await StokListele(id, islem);
    }

    // ─── Barkod ile bul ───────────────────────────────────────────────────────

    public async Task<object> BarkodBul(string barkod, AktifKullanici islem)
    {
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.TenantId == islem.TenantId && u.Barkod == barkod && u.Aktif)
            ?? throw new NotFoundException($"'{barkod}' barkodlu ürün bulunamadı");
        return urun;
    }

    // ─── Dahili yardımcı ──────────────────────────────────────────────────────

    internal async Task<Data.Urun> TenantUrunuBul(string id, string tenantId) =>
        await db.Urunler.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
        ?? throw new NotFoundException("Ürün bulunamadı");
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/urunler")]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunController(UrunServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/urunler — Yeni ürün tanımla (201) — MUHASEBE+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Olustur([FromBody] UrunOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>GET /api/v1/urunler — Filtreli ürün listesi</summary>
    [HttpGet]
    public Task<List<Data.Urun>> Listele(
        [FromQuery] string? tip, [FromQuery] string? ayarId,
        [FromQuery] bool? aktif, [FromQuery] string? q,
        [FromQuery, Range(1, 500)] int limit = 50,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0)
        => servis.Listele(Islem, tip, ayarId, aktif, q, limit, offset);

    /// <summary>GET /api/v1/urunler/barkod/{kod} — Barkodla hızlı arama (POS)</summary>
    [HttpGet("barkod/{kod}")]
    public Task<object> BarkodBul(string kod) => servis.BarkodBul(kod, Islem);

    /// <summary>GET /api/v1/urunler/{id} — Ürün detayı</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>GET /api/v1/urunler/{id}/stok — Tüm şubelerdeki stok durumu</summary>
    [HttpGet("{id}/stok")]
    public Task<object> StokListele(string id) => servis.StokListele(id, Islem);

    /// <summary>PATCH /api/v1/urunler/{id} — Ürün bilgilerini güncelle — MUHASEBE+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Guncelle(string id, [FromBody] UrunGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>PATCH /api/v1/urunler/{id}/stok/{subeId} — Sayım düzeltme — MUDUR+</summary>
    [HttpPatch("{id}/stok/{subeId}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> StokSayimGuncelle(string id, string subeId, [FromBody] StokSayimDto dto)
        => servis.StokSayimGuncelle(id, subeId, dto.Adet, Islem);

    /// <summary>DELETE /api/v1/urunler/{id} — Soft delete — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
