using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// Satış / İşlemler modülü — kaynak: apps/api/src/modules/satis/*
//   • Rota: api/v1/islemler (satis, alis, hurda-alis, transfer, takas, iscilik)
//   • Paralel defter: hareketler.tlTutari (TL bacağı) + hareketler.hasGrami (has bacağı)
//   • Her işlem tek EF transaction içinde: hareket + stok + cari bakiye + kasa
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SatisOlusturDto
{
    /// <summary>Müşteri cari ID (opsiyonel, müşteriye bağlanır) — Nest: @IsOptional + @IsNotEmpty (boş string reddedilir)</summary>
    [MinLength(1)] public string? CariId { get; set; }

    /// <summary>Satılan ürün ID</summary>
    [Required] public string UrunId { get; set; } = null!;

    /// <summary>Satış adedi (1 tam ürün = 1)</summary>
    [Range(0.001, double.MaxValue)] public decimal Adet { get; set; }

    /// <summary>Birim satış fiyatı TL</summary>
    [Range(0, double.MaxValue)] public decimal BirimFiyatTl { get; set; }

    /// <summary>İşçilik TL (sıfır da olabilir)</summary>
    [Range(0, double.MaxValue)] public decimal? IscilikTl { get; set; }

    [Required, RegularExpression("NAKIT|KART|HAVALE|ALTIN|VERESIYE")]
    public string OdemeTuru { get; set; } = null!;

    /// <summary>Kasa ID — NAKIT/KART/HAVALE için zorunlu; VERESIYE ise boş bırakılabilir.</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class AlisOlusturDto
{
    /// <summary>Tedarikçi / satıcı cari ID</summary>
    public string? CariId { get; set; }

    /// <summary>Alınan ürün ID (yoksa önce ürün tanımlanmalı)</summary>
    [Required] public string UrunId { get; set; } = null!;

    /// <summary>Alış adedi</summary>
    [Range(0.001, double.MaxValue)] public decimal Adet { get; set; }

    /// <summary>Birim alış fiyatı TL</summary>
    [Range(0, double.MaxValue)] public decimal BirimFiyatTl { get; set; }

    /// <summary>Kasa ID — nakit/havale ödeme yapılıyorsa; cari borçlandırılacaksa boş.</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class HurdaAlisDto
{
    /// <summary>Müşteri / satıcı cari</summary>
    public string? CariId { get; set; }

    /// <summary>Hurdanın ayarı</summary>
    [Required, RegularExpression("K8|K14|K18|K21|K22|K24|G800|G925|G950")]
    public string AyarId { get; set; } = null!;

    /// <summary>Hurda fiziksel gram</summary>
    [Range(0.001, double.MaxValue)] public decimal GramAgirlik { get; set; }

    /// <summary>Ödenen birim fiyat TL/has gram</summary>
    [Range(0, double.MaxValue)] public decimal BirimFiyatTlHasGram { get; set; }

    /// <summary>Ödeme yapılan kasa</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class TransferDto
{
    [Required] public string KaynakSubeId { get; set; } = null!;
    [Required] public string HedefSubeId { get; set; } = null!;
    [Required] public string UrunId { get; set; } = null!;

    [Range(0.001, double.MaxValue)] public decimal Adet { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class TakasDto
{
    /// <summary>Müşteri cari</summary>
    public string? CariId { get; set; }

    /// <summary>Müşteriden alınan eski ürün (takas edilecek)</summary>
    [Required] public string EskiUrunAyarId { get; set; } = null!;
    public decimal EskiUrunGram { get; set; }
    public decimal EskiUrunBirimFiyatTlHasGram { get; set; }

    /// <summary>Müşteriye verilen yeni ürün</summary>
    [Required] public string YeniUrunId { get; set; } = null!;

    [Range(0.001, double.MaxValue)] public decimal YeniUrunAdet { get; set; }

    [Range(0, double.MaxValue)] public decimal YeniUrunBirimFiyatTl { get; set; }

    /// <summary>Fark ödemesi — pozitif = müşteri öder, negatif = biz öderiz</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class IscilikDto
{
    /// <summary>İşçilik talep eden müşteri</summary>
    [Required] public string CariId { get; set; } = null!;

    /// <summary>İşçilik yapılan ürün (varsa)</summary>
    public string? UrunId { get; set; }

    /// <summary>İşçilik ücreti TL (Nest: @IsPositive — sıfırdan büyük)</summary>
    [Range(0d, double.MaxValue, MinimumIsExclusive = true)] public decimal IscilikTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>Nakit tahsilat yapılıyorsa kasa</summary>
    public string? KasaId { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class SatisServisi(KuyumcuDbContext db, ILogger<SatisServisi> logger)
{
    // ─── Satış ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Paralel defter mantığı:
    ///   hareketler.tlTutari   = +toplamTl (satış alacağı / kasa girişi)
    ///   hareketler.hasGrami   = -toplamHas (stoktan çıkış)
    ///   stoklar               -= adet / toplamHasGram
    ///   cari_bakiyeler.tl     += toplamTl  (VERESIYE ise cari borçlanır)
    ///   kasalar.bakiye        += toplamTl  (nakit/kart/havale ise kasaya girer)
    /// </summary>
    public async Task<object> Satis(SatisOlusturDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Satış için şube seçimi gerekli");
        var urun = await UrunBul(dto.UrunId, islem.TenantId);
        var stok = await StokBul(dto.UrunId, subeId, islem.TenantId);

        if (stok.Adet < dto.Adet)
            throw new BadRequestException(
                $"Yetersiz stok. Mevcut: {stok.Adet} adet, Talep: {dto.Adet} adet");

        var iscilikTl = dto.IscilikTl ?? 0m;
        var toplamTl  = dto.Adet * dto.BirimFiyatTl + iscilikTl;
        var toplamHas = dto.Adet * urun.HasGrami;
        var now       = DateTime.UtcNow;

        if (dto.OdemeTuru != "VERESIYE" && string.IsNullOrEmpty(dto.KasaId))
            throw new BadRequestException("Nakit/Kart/Havale satış için kasa ID gerekli");

        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Ana hareket kaydı
        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "SATIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(toplamTl),
            HasGrami      = Yuvarla6(-toplamHas),   // stoktan çıkış
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.UrunId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // 2. Stok azalt
        await StokDegistir(dto.UrunId, subeId, islem.TenantId, -dto.Adet, urun.HasGrami, now);

        // 3a. VERESİYE → cari bakiye artır
        if (dto.OdemeTuru == "VERESIYE" && !string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, toplamTl, 0m, now);

        // 3b. Nakit/Kart/Havale → kasaya ekle + kasa hareketi kaydet
        if (dto.OdemeTuru != "VERESIYE" && !string.IsNullOrEmpty(dto.KasaId))
            await KasaGuncelle(dto.KasaId, islem.TenantId, toplamTl, now, hareketId, dto.OdemeTuru);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] SATIS: ürün={UrunId} adet={Adet} toplam={ToplamTl} TL",
            islem.TenantId, dto.UrunId, dto.Adet, toplamTl);

        return await HareketGetir(dto.UrunId, islem.TenantId);
    }

    // ─── Alış ─────────────────────────────────────────────────────────────────
    /// <summary>
    ///   hareketler.tlTutari   = -toplamTl  (kasadan çıkış / tedarikçi borcu)
    ///   hareketler.hasGrami   = +toplamHas (stoka giriş)
    ///   stoklar               += adet / toplamHasGram
    ///   cari_bakiyeler.tl     -= toplamTl  (tedarikçi bize sattı, biz ödedik → borç bitti)
    ///   kasalar.bakiye        -= toplamTl  (nakit ödeme ise kasadan çıkar)
    /// </summary>
    public async Task<object> Alis(AlisOlusturDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Alış için şube seçimi gerekli");
        var urun      = await UrunBul(dto.UrunId, islem.TenantId);
        var toplamTl  = dto.Adet * dto.BirimFiyatTl;
        var toplamHas = dto.Adet * urun.HasGrami;
        var now       = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "ALIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(-toplamTl),    // kasadan çıkış
            HasGrami      = Yuvarla6(toplamHas),    // stoka giriş
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.UrunId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Stok artır
        await StokDegistir(dto.UrunId, subeId, islem.TenantId, dto.Adet, urun.HasGrami, now);

        // Tedarikçi cari borçlandı (biz ona ödedik → bakiyesi azalır/sıfırlanır)
        if (!string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, -toplamTl, 0m, now);

        // Kasa varsa kasadan düş
        if (!string.IsNullOrEmpty(dto.KasaId))
            await KasaGuncelle(dto.KasaId, islem.TenantId, -toplamTl, now, hareketId, "NAKIT");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] ALIS: ürün={UrunId} adet={Adet} toplam={ToplamTl} TL",
            islem.TenantId, dto.UrunId, dto.Adet, toplamTl);

        return await HareketGetir(dto.UrunId, islem.TenantId);
    }

    // ─── Hurda alış ───────────────────────────────────────────────────────────
    /// <summary>
    /// Hurdayı has gram üzerinden fiyatlandırır, stok girişi yapmaz
    /// (hurda genelde eritilir, ayrı bir ürün sayılmaz).
    /// </summary>
    public async Task<object> HurdaAlis(HurdaAlisDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Hurda alış için şube seçimi gerekli");
        var ayar = Ayarlar.Bul(dto.AyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.AyarId}");

        var hasGrami = HasHesap.ToHasGram(dto.GramAgirlik, ayar);
        var toplamTl = hasGrami * dto.BirimFiyatTlHasGram;
        var now      = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "HURDA_ALIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(-toplamTl),   // kasadan çıkış
            HasGrami      = Yuvarla6(hasGrami),    // has gram girişi (stok değil — genel)
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        if (!string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, -toplamTl, hasGrami, now);

        if (!string.IsNullOrEmpty(dto.KasaId))
            await KasaGuncelle(dto.KasaId, islem.TenantId, -toplamTl, now, hareketId, "NAKIT");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] HURDA_ALIS: {GramAgirlik}g {AyarId} = {HasGrami} has, {ToplamTl} TL",
            islem.TenantId, dto.GramAgirlik, dto.AyarId, Yuvarla6(hasGrami), Yuvarla2(toplamTl));

        return new
        {
            gramAgirlik = dto.GramAgirlik,
            ayarId      = dto.AyarId,
            hasGrami    = Yuvarla6(hasGrami),
            odenenTl    = Yuvarla2(toplamTl),
        };
    }

    // ─── Şubeler arası transfer ───────────────────────────────────────────────
    public async Task<object> Transfer(TransferDto dto, AktifKullanici islem)
    {
        if (dto.KaynakSubeId == dto.HedefSubeId)
            throw new BadRequestException("Kaynak ve hedef şube aynı olamaz");

        var urun       = await UrunBul(dto.UrunId, islem.TenantId);
        var kaynakStok = await StokBul(dto.UrunId, dto.KaynakSubeId, islem.TenantId);

        if (kaynakStok.Adet < dto.Adet)
            throw new BadRequestException(
                $"Kaynak şubede yetersiz stok. Mevcut: {kaynakStok.Adet} adet, Talep: {dto.Adet}");

        var hasGramiPerAdet = urun.HasGrami;
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        db.Hareketler.Add(new Data.Hareket
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            SubeId        = dto.KaynakSubeId,
            Tur           = "TRANSFER",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = Yuvarla6(-dto.Adet * hasGramiPerAdet),
            KurSnapshotId = dto.KurSnapshotId,
            UrunId        = dto.UrunId,
            KarsiSubeId   = dto.HedefSubeId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Kaynak şubeden düş, hedef şubeye ekle
        await StokDegistir(dto.UrunId, dto.KaynakSubeId, islem.TenantId, -dto.Adet, hasGramiPerAdet, now);
        await StokDegistir(dto.UrunId, dto.HedefSubeId,  islem.TenantId,  dto.Adet, hasGramiPerAdet, now);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] TRANSFER: {UrunId} {Adet} adet {KaynakSubeId} → {HedefSubeId}",
            islem.TenantId, dto.UrunId, dto.Adet, dto.KaynakSubeId, dto.HedefSubeId);

        return new { mesaj = "Transfer tamamlandı", urunId = dto.UrunId, adet = dto.Adet };
    }

    // ─── Takas (eski → yeni) ──────────────────────────────────────────────────
    public async Task<object> Takas(TakasDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Takas için şube seçimi gerekli");
        var ayar = Ayarlar.Bul(dto.EskiUrunAyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.EskiUrunAyarId}");

        var yeniUrun = await UrunBul(dto.YeniUrunId, islem.TenantId);
        var yeniStok = await StokBul(dto.YeniUrunId, subeId, islem.TenantId);

        if (yeniStok.Adet < dto.YeniUrunAdet)
            throw new BadRequestException("Yeni ürün stoğu yetersiz");

        var eskiHas     = HasHesap.ToHasGram(dto.EskiUrunGram, ayar);
        var eskiDeger   = eskiHas * dto.EskiUrunBirimFiyatTlHasGram;
        var yeniDeger   = dto.YeniUrunAdet * dto.YeniUrunBirimFiyatTl;
        var farkTl      = yeniDeger - eskiDeger;   // pozitif = müşteri öder
        var yeniUrunHas = dto.YeniUrunAdet * yeniUrun.HasGrami;
        var now         = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        // TAKAS hareketi: net TL farkı + net has gram hareketi
        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "TAKAS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(farkTl),                    // müşteriden alınan fark
            HasGrami      = Yuvarla6(eskiHas - yeniUrunHas),     // net has hareketi
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.YeniUrunId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Yeni ürün stoktan çıkar
        await StokDegistir(dto.YeniUrunId, subeId, islem.TenantId, -dto.YeniUrunAdet, yeniUrun.HasGrami, now);

        // Cari varsa fark borçlandır
        if (!string.IsNullOrEmpty(dto.CariId) && farkTl != 0m)
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, farkTl, -eskiHas, now);

        // Kasaya ekle (müşteri ödemesi var ise)
        if (!string.IsNullOrEmpty(dto.KasaId) && farkTl > 0m)
            await KasaGuncelle(dto.KasaId, islem.TenantId, farkTl, now, hareketId, "NAKIT");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] TAKAS: eskiHas={EskiHas} yeniHas={YeniHas} fark={FarkTl} TL",
            islem.TenantId, eskiHas, yeniUrunHas, Yuvarla2(farkTl));

        return new
        {
            eskiHasGrami = Yuvarla6(eskiHas),
            yeniHasGrami = yeniUrunHas,
            eskiDegerTl  = eskiDeger,
            yeniDegerTl  = yeniDeger,
            farkTl,
        };
    }

    // ─── İşçilik ──────────────────────────────────────────────────────────────
    public async Task<object> Iscilik(IscilikDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "İşçilik için şube seçimi gerekli");
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "ISCILIK",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(dto.IscilikTl),
            HasGrami      = 0m,
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.UrunId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Müşteri işçilik ücretini borçlanır
        await CariBakiyeGuncelle(dto.CariId, islem.TenantId, dto.IscilikTl, 0m, now);

        // Nakit tahsilat
        if (!string.IsNullOrEmpty(dto.KasaId))
        {
            await KasaGuncelle(dto.KasaId, islem.TenantId, dto.IscilikTl, now, hareketId, "NAKIT");
            // Cari bakiyeyi sıfırla (peşin aldık)
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, -dto.IscilikTl, 0m, now);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] ISCILIK: cari={CariId} {IscilikTl} TL",
            islem.TenantId, dto.CariId, dto.IscilikTl);

        return new { cariId = dto.CariId, iscilikTl = dto.IscilikTl };
    }

    // ─── Dahili yardımcılar ───────────────────────────────────────────────────

    /// <summary>Nest ile birebir: token'da şube seçili değilse hata (fallback yok).</summary>
    private static string SubeBelirle(AktifKullanici islem, string hataMesaji)
    {
        if (string.IsNullOrEmpty(islem.SubeId))
            throw new BadRequestException(hataMesaji);
        return islem.SubeId!;
    }

    private async Task<Data.Urun> UrunBul(string urunId, string tenantId) =>
        await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == urunId && u.TenantId == tenantId && u.Aktif)
        ?? throw new NotFoundException("Ürün bulunamadı veya aktif değil");

    private async Task<Data.Stok> StokBul(string urunId, string subeId, string tenantId) =>
        await db.Stoklar.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UrunId == urunId && s.SubeId == subeId && s.TenantId == tenantId)
        ?? throw new BadRequestException("Bu şubede ürün stoğu bulunamadı");

    private async Task StokDegistir(
        string urunId, string subeId, string tenantId,
        decimal adetDelta, decimal hasGramiPerAdet, DateTime now)
    {
        // Aynı transaction içinde henüz kaydedilmemiş satırları da görebilmek için
        // önce EF change tracker'a (Local) bakılır.
        var mevcut = db.Stoklar.Local.FirstOrDefault(s => s.UrunId == urunId && s.SubeId == subeId)
            ?? await db.Stoklar.FirstOrDefaultAsync(s => s.UrunId == urunId && s.SubeId == subeId);

        var yeniAdet = mevcut is not null ? mevcut.Adet + adetDelta : adetDelta;
        var yeniHas  = mevcut is not null
            ? mevcut.ToplamHasGram + adetDelta * hasGramiPerAdet
            : adetDelta * hasGramiPerAdet;

        if (yeniAdet < 0m) throw new BadRequestException("Stok sıfırın altına düşemez");

        if (mevcut is not null)
        {
            mevcut.Adet          = Yuvarla3(yeniAdet);
            mevcut.ToplamHasGram = Yuvarla6(yeniHas);
            mevcut.Guncellendi   = now;
        }
        else
        {
            db.Stoklar.Add(new Data.Stok
            {
                Id            = Kimlik.Ulid(),
                TenantId      = tenantId,
                SubeId        = subeId,
                UrunId        = urunId,
                Adet          = Yuvarla3(yeniAdet),
                ToplamHasGram = Yuvarla6(yeniHas),
                Guncellendi   = now,
            });
        }
    }

    private async Task CariBakiyeGuncelle(
        string cariId, string tenantId,
        decimal tlDelta, decimal hasGramDelta, DateTime now)
    {
        var mevcut = db.CariBakiyeler.Local.FirstOrDefault(b => b.CariId == cariId)
            ?? await db.CariBakiyeler.FirstOrDefaultAsync(b => b.CariId == cariId);

        if (mevcut is not null)
        {
            mevcut.TlBakiye       = Yuvarla2(mevcut.TlBakiye + tlDelta);
            mevcut.HasGramBakiye  = Yuvarla6(mevcut.HasGramBakiye + hasGramDelta);
            mevcut.SonIslemTarihi = now;
            mevcut.Guncellendi    = now;
        }
        else
        {
            db.CariBakiyeler.Add(new Data.CariBakiye
            {
                CariId         = cariId,
                TenantId       = tenantId,
                TlBakiye       = Yuvarla2(tlDelta),
                HasGramBakiye  = Yuvarla6(hasGramDelta),
                SonIslemTarihi = now,
                Guncellendi    = now,
            });
        }
    }

    private async Task KasaGuncelle(
        string kasaId, string tenantId,
        decimal tutarDelta, DateTime now,
        string hareketId, string odemeTuru)
    {
        var kasa = db.Kasalar.Local.FirstOrDefault(k => k.Id == kasaId)
            ?? await db.Kasalar.FirstOrDefaultAsync(k => k.Id == kasaId)
            ?? throw new NotFoundException($"Kasa bulunamadı: {kasaId}");

        kasa.Bakiye      = Yuvarla2(kasa.Bakiye + tutarDelta);
        kasa.Guncellendi = now;

        db.KasaHareketleri.Add(new Data.KasaHareket
        {
            Id          = Kimlik.Ulid(),
            TenantId    = tenantId,
            KasaId      = kasaId,
            HareketId   = hareketId,
            Tutar       = Yuvarla2(Math.Abs(tutarDelta)),
            ParaBirimi  = "TRY",
            OdemeTuru   = odemeTuru,
            Tarih       = now,
            Olusturuldu = now,
        });
    }

    private async Task<object> HareketGetir(string urunId, string tenantId)
    {
        var h = await db.Hareketler.AsNoTracking()
            .Where(x => x.UrunId == urunId && x.TenantId == tenantId)
            .OrderByDescending(x => x.Olusturuldu)
            .FirstOrDefaultAsync();
        return (object?)h ?? new { mesaj = "İşlem tamamlandı" };
    }

    // Nest'teki toFixed(2)/toFixed(3)/toFixed(6) karşılıkları
    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    private static decimal Yuvarla3(decimal d) => Math.Round(d, 3, MidpointRounding.AwayFromZero);
    private static decimal Yuvarla6(decimal d) => Math.Round(d, 6, MidpointRounding.AwayFromZero);
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/islemler")]
[Yetki.ModulYetkisi("islem")]
public sealed class SatisController(SatisServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/islemler/satis — Ürün satışı: stok düşer, kasa/cari güncellenir, deftere yazılır</summary>
    [HttpPost("satis")]
    public Task<object> Satis([FromBody] SatisOlusturDto dto) => servis.Satis(dto, Islem);

    /// <summary>POST /api/v1/islemler/alis — Ürün alışı: stok artar, kasa/cari güncellenir — MUHASEBE+</summary>
    [HttpPost("alis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Alis([FromBody] AlisOlusturDto dto) => servis.Alis(dto, Islem);

    /// <summary>POST /api/v1/islemler/hurda-alis — Hurda altın alışı: has gram hesaplanır, fiyat ödenir — MUHASEBE+</summary>
    [HttpPost("hurda-alis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HurdaAlis([FromBody] HurdaAlisDto dto) => servis.HurdaAlis(dto, Islem);

    /// <summary>POST /api/v1/islemler/transfer — Şubeler arası stok transferi — MUDUR+</summary>
    [HttpPost("transfer")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Transfer([FromBody] TransferDto dto) => servis.Transfer(dto, Islem);

    /// <summary>POST /api/v1/islemler/takas — Eski ürün → yeni ürün takası</summary>
    [HttpPost("takas")]
    public Task<object> Takas([FromBody] TakasDto dto) => servis.Takas(dto, Islem);

    /// <summary>POST /api/v1/islemler/iscilik — İşçilik ücreti kaydet</summary>
    [HttpPost("iscilik")]
    public Task<object> Iscilik([FromBody] IscilikDto dto) => servis.Iscilik(dto, Islem);
}
