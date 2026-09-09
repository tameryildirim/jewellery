using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// Banka modülü — KasaModulu.cs desenini birebir izler.
// Kaynak: apps/api/src/modules/banka/*
//   • Manuel dekont girişi (canlı banka entegrasyonu gelene kadar)
//   • GELEN hareketleri satış faturasına dönüştürme (FTR-{yıl}-{sıra})
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class HesapOlusturDto
{
    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Banka adı 2-100 karakter olmalıdır")]
    public string BankaAdi { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Hesap adı 2-100 karakter olmalıdır")]
    public string HesapAdi { get; set; } = null!;

    /// <summary>Hesap sahibinin adı/ünvanı (kişi ya da firma) — opsiyonel</summary>
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Hesap sahibi 2-150 karakter olmalıdır")]
    public string? HesapSahibi { get; set; }

    [Required, RegularExpression(@"^TR\d{24}$|^[A-Z]{2}\d{2}[A-Z0-9]{10,30}$",
        ErrorMessage = "Geçerli bir IBAN giriniz (TR + 24 rakam)")]
    public string Iban { get; set; } = null!;

    [RegularExpression("TRY|USD|EUR|GBP")]
    public string? ParaBirimi { get; set; }
}

public sealed class HesapGuncelleDto
{
    [StringLength(100, MinimumLength = 2)] public string? BankaAdi { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? HesapAdi { get; set; }
    /// <summary>Boş string gönderilirse sahip temizlenir</summary>
    [StringLength(150)] public string? HesapSahibi { get; set; }

    [RegularExpression(@"^TR\d{24}$|^[A-Z]{2}\d{2}[A-Z0-9]{10,30}$",
        ErrorMessage = "Geçerli bir IBAN giriniz (TR + 24 rakam)")]
    public string? Iban { get; set; }

    [RegularExpression("TRY|USD|EUR|GBP")] public string? ParaBirimi { get; set; }
    public bool? Aktif { get; set; }
}

public sealed class HareketEkleDto
{
    [Required, RegularExpression("GELEN|GIDEN", ErrorMessage = "Yön GELEN veya GIDEN olmalıdır")]
    public string Yon { get; set; } = null!;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal Tutar { get; set; }

    [StringLength(150, MinimumLength = 1)]
    public string? KarsiTarafAd { get; set; }

    [StringLength(34, MinimumLength = 15)]
    public string? KarsiIban { get; set; }

    public string? Aciklama { get; set; }

    /// <summary>Bilinen bir cariye doğrudan eşleştir</summary>
    [StringLength(36, MinimumLength = 26)]
    public string? CariId { get; set; }

    /// <summary>ISO tarih; verilmezse şimdi</summary>
    public string? Tarih { get; set; }
}

public sealed class FaturayaDonusturDto
{
    /// <summary>Fatura kesilecek cari — hareket zaten eşleşmişse opsiyonel</summary>
    [StringLength(36, MinimumLength = 26)]
    public string? CariId { get; set; }

    /// <summary>KDV oranı (0, 0.01, 0.10, 0.20) — tutar KDV dahil kabul edilir</summary>
    [Range(0, 1)]
    public decimal? KdvOran { get; set; }

    [RegularExpression("SATIS|ALIS")]
    public string? Tur { get; set; }

    public string? Aciklama { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class BankaServisi(KuyumcuDbContext db)
{
    // ─── Hesap oluştur ────────────────────────────────────────────────────────

    public async Task<object> HesapOlustur(HesapOlusturDto dto, AktifKullanici islem)
    {
        var mevcut = await db.BankaHesaplari.AnyAsync(h =>
            h.TenantId == islem.TenantId && h.Iban == dto.Iban);
        if (mevcut) throw new ConflictException("Bu IBAN zaten kayıtlı");

        var now = DateTime.UtcNow;
        var hesap = new BankaHesap
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            BankaAdi = dto.BankaAdi,
            HesapAdi = dto.HesapAdi,
            HesapSahibi = string.IsNullOrWhiteSpace(dto.HesapSahibi) ? null : dto.HesapSahibi.Trim(),
            Iban = dto.Iban,
            ParaBirimi = dto.ParaBirimi ?? "TRY",
            Bakiye = 0m,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.BankaHesaplari.Add(hesap);
        await db.SaveChangesAsync();

        return await HesapGetir(hesap.Id, islem);
    }

    // ─── Hesap güncelle ───────────────────────────────────────────────────────
    /// <summary>Yalnız gönderilen alanlar güncellenir. IBAN değişirse benzersizlik doğrulanır.</summary>
    public async Task<object> HesapGuncelle(string id, HesapGuncelleDto dto, AktifKullanici islem)
    {
        var hesap = await TenantHesabiBul(id, islem.TenantId);

        if (!string.IsNullOrEmpty(dto.Iban) && dto.Iban != hesap.Iban)
        {
            var cakisan = await db.BankaHesaplari.AnyAsync(h =>
                h.TenantId == islem.TenantId && h.Iban == dto.Iban && h.Id != id);
            if (cakisan) throw new ConflictException("Bu IBAN zaten kayıtlı");
            hesap.Iban = dto.Iban;
        }

        if (dto.BankaAdi is not null) hesap.BankaAdi = dto.BankaAdi;
        if (dto.HesapAdi is not null) hesap.HesapAdi = dto.HesapAdi;
        // Boş string → sahip temizlenir; null → dokunulmaz
        if (dto.HesapSahibi is not null)
            hesap.HesapSahibi = string.IsNullOrWhiteSpace(dto.HesapSahibi) ? null : dto.HesapSahibi.Trim();
        if (dto.ParaBirimi is not null) hesap.ParaBirimi = dto.ParaBirimi;
        if (dto.Aktif is not null) hesap.Aktif = dto.Aktif.Value;
        hesap.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await HesapGetir(id, islem);
    }

    // ─── Hesap listesi ────────────────────────────────────────────────────────

    public async Task<object> HesapListele(AktifKullanici islem)
    {
        var hesaplar = await db.BankaHesaplari.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId)
            .OrderByDescending(h => h.Olusturuldu)
            .ToListAsync();
        return new { hesaplar };
    }

    public async Task<object> HesapGetir(string id, AktifKullanici islem)
    {
        var hesap = await db.BankaHesaplari.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");
        return hesap;
    }

    // ─── Hareket ekle ─────────────────────────────────────────────────────────
    /// <summary>
    /// Manuel dekont girişi (canlı banka entegrasyonu gelene kadar).
    /// GELEN → bakiye artar, GIDEN → bakiye azalır.
    /// </summary>
    public async Task<object> HareketEkle(string hesapId, HareketEkleDto dto, AktifKullanici islem)
    {
        var hesap = await TenantHesabiBul(hesapId, islem.TenantId);

        if (!string.IsNullOrEmpty(dto.CariId))
        {
            var cariVar = await db.Cariler.AnyAsync(c =>
                c.Id == dto.CariId && c.TenantId == islem.TenantId);
            if (!cariVar) throw new NotFoundException("Cari bulunamadı");
        }

        var yonKatsayi = dto.Yon == "GELEN" ? 1m : -1m;
        var now = DateTime.UtcNow;
        var hareketId = Kimlik.Ulid();
        var tarih = string.IsNullOrEmpty(dto.Tarih)
            ? now
            : DateTime.Parse(dto.Tarih, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        await using var tx = await db.Database.BeginTransactionAsync();
        hesap.Bakiye += dto.Tutar * yonKatsayi;
        hesap.Guncellendi = now;

        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            BankaHesapId = hesapId,
            Yon = dto.Yon,
            Tutar = dto.Tutar,
            ParaBirimi = hesap.ParaBirimi,
            KarsiTarafAd = dto.KarsiTarafAd,
            KarsiIban = dto.KarsiIban,
            Aciklama = dto.Aciklama,
            CariId = string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId,
            Durum = string.IsNullOrEmpty(dto.CariId) ? "BEKLIYOR" : "ESLESTI",
            Tarih = tarih,
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new { hareketId, hesap = await HesapGetir(hesapId, islem) };
    }

    // ─── Hareket listesi ──────────────────────────────────────────────────────

    public async Task<object> HareketListele(
        string hesapId, AktifKullanici islem, string? durum, int limit, int offset)
    {
        var hesap = await HesapGetir(hesapId, islem);

        var q = db.BankaHareketleri.AsNoTracking()
            .Where(h => h.BankaHesapId == hesapId && h.TenantId == islem.TenantId);
        if (durum is "BEKLIYOR" or "ESLESTI" or "FATURALANDI")
            q = q.Where(h => h.Durum == durum);

        var hareketler = await q.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        return new { hesap, hareketler };
    }

    // ─── Faturaya dönüştür ────────────────────────────────────────────────────
    /// <summary>
    /// GELEN banka hareketini satış faturasına çevirir:
    ///  - Tutar KDV dahil kabul edilir: araToplam = toplam / (1 + kdvOran)
    ///  - Fatura no: FTR-&lt;yıl&gt;-&lt;sıra&gt; (tenant bazında)
    ///  - Hareket FATURALANDI durumuna geçer ve faturaya bağlanır
    /// </summary>
    public async Task<object> FaturayaDonustur(string hareketId, FaturayaDonusturDto dto, AktifKullanici islem)
    {
        var hareket = await db.BankaHareketleri
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        if (hareket.Durum == "FATURALANDI")
            throw new ConflictException("Bu hareket zaten faturalandı");
        if (hareket.Yon != "GELEN")
            throw new BadRequestException("Yalnızca GELEN hareketler faturaya dönüştürülebilir");

        var cariId = dto.CariId ?? hareket.CariId;
        if (string.IsNullOrEmpty(cariId))
            throw new BadRequestException("Hareket bir cariye eşleşmemiş; cariId gönderiniz");

        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");

        var kdvOran = dto.KdvOran ?? 0.20m;
        var toplam = hareket.Tutar;
        var araToplam = toplam / (1m + kdvOran);
        var kdvTutar = toplam - araToplam;

        var faturaId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var faturaNo = await SiradakiFaturaNo(islem.TenantId, now.Year);

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Faturalar.Add(new Data.Fatura
        {
            Id = faturaId,
            TenantId = islem.TenantId,
            FaturaNo = faturaNo,
            CariId = cariId,
            Tur = dto.Tur ?? "SATIS",
            AraToplam = Math.Round(araToplam, 2, MidpointRounding.AwayFromZero),
            KdvOran = Math.Round(kdvOran, 4, MidpointRounding.AwayFromZero),
            KdvTutar = Math.Round(kdvTutar, 2, MidpointRounding.AwayFromZero),
            Toplam = toplam,
            ParaBirimi = hareket.ParaBirimi,
            Durum = "KESILDI",
            BankaHareketId = hareketId,
            Aciklama = dto.Aciklama ?? hareket.Aciklama,
            Tarih = hareket.Tarih,
            Olusturuldu = now,
            Guncellendi = now,
        });

        hareket.Durum = "FATURALANDI";
        hareket.FaturaId = faturaId;
        hareket.CariId = cariId;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faturaId);
        return new { fatura, cariAd = cari.Ad };
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    internal async Task<BankaHesap> TenantHesabiBul(string id, string tenantId) =>
        await db.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId)
        ?? throw new NotFoundException("Banka hesabı bulunamadı");

    private async Task<string> SiradakiFaturaNo(string tenantId, int yil)
    {
        var onEk = $"FTR-{yil}-";
        var adet = await db.Faturalar
            .CountAsync(f => f.TenantId == tenantId && f.FaturaNo.StartsWith(onEk));
        return $"{onEk}{adet + 1:D6}";
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
public sealed class BankaController(BankaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/bankalar — Banka hesabı ekle — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> HesapOlustur([FromBody] HesapOlusturDto dto) => servis.HesapOlustur(dto, Islem);

    /// <summary>GET /api/v1/bankalar — Hesap listesi</summary>
    [HttpGet]
    public Task<object> HesapListele() => servis.HesapListele(Islem);

    /// <summary>GET /api/v1/bankalar/{id} — Hesap detayı</summary>
    [HttpGet("{id}")]
    public Task<object> HesapGetir(string id) => servis.HesapGetir(id, Islem);

    /// <summary>PATCH /api/v1/bankalar/{id} — Hesap bilgilerini güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> HesapGuncelle(string id, [FromBody] HesapGuncelleDto dto)
        => servis.HesapGuncelle(id, dto, Islem);

    /// <summary>GET /api/v1/bankalar/{id}/hareketler?durum=BEKLIYOR — Hareket listesi</summary>
    [HttpGet("{id}/hareketler")]
    public Task<object> HareketListele(
        string id,
        [FromQuery] string? durum,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.HareketListele(id, Islem, durum, limit, offset);

    /// <summary>POST /api/v1/bankalar/{id}/hareket — Manuel dekont girişi — MUHASEBE+</summary>
    [HttpPost("{id}/hareket")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketEkle(string id, [FromBody] HareketEkleDto dto)
        => servis.HareketEkle(id, dto, Islem);

    /// <summary>POST /api/v1/bankalar/hareketler/{hareketId}/faturaya-donustur — MUHASEBE+</summary>
    [HttpPost("hareketler/{hareketId}/faturaya-donustur")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> FaturayaDonustur(string hareketId, [FromBody] FaturayaDonusturDto dto)
        => servis.FaturayaDonustur(hareketId, dto, Islem);
}
