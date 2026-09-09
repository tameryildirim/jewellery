using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// CARİ MODÜLÜ — müşteri/tedarikçi/atölye/sarraf hesapları ve paralel defter
// (TL + has gram) bakiye takibi.
// Kaynak: apps/api/src/modules/cari/*
// Desen: Modules/Kasa/KasaModulu.cs (tek dosya: DTO'lar + Servis + Controller)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class CariOlusturDto
{
    [Required, RegularExpression("MUSTERI|TEDARIKCI|HER_IKISI|ATOLYE|SARRAF")]
    public string Tur { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Ad { get; set; } = null!;

    [MaxLength(20)] public string? Telefon { get; set; }
    [EmailAddress, MaxLength(254)] public string? Email { get; set; }
    [MaxLength(20)] public string? VergiNo { get; set; }
    [MaxLength(500)] public string? Adres { get; set; }
    [MaxLength(1000)] public string? Notlar { get; set; }

    /// <summary>Maksimum veresiye limiti TL — null = sınırsız</summary>
    [Range(0, double.MaxValue)] public decimal? KrediLimitiTl { get; set; }
}

/// <summary>
/// PATCH gövdesinde "hiç gönderilmedi" ile "açıkça null gönderildi" ayrımı
/// korunur (Nest'teki undefined filtresi): yalnızca gövdede geçen alanlar
/// güncellenir; null gönderilen opsiyonel alanlar temizlenir.
/// </summary>
public sealed class CariGuncelleDto
{
    private string? _tur;
    private string? _ad;
    private string? _telefon;
    private string? _email;
    private string? _vergiNo;
    private string? _adres;
    private string? _notlar;
    private decimal? _krediLimitiTl;
    private bool? _aktif;

    [RegularExpression("MUSTERI|TEDARIKCI|HER_IKISI|ATOLYE|SARRAF")]
    public string? Tur { get => _tur; set { _tur = value; TurVerildi = true; } }
    [JsonIgnore] public bool TurVerildi { get; private set; }

    [MaxLength(200)]
    public string? Ad { get => _ad; set { _ad = value; AdVerildi = true; } }
    [JsonIgnore] public bool AdVerildi { get; private set; }

    [MaxLength(20)]
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonVerildi = true; } }
    [JsonIgnore] public bool TelefonVerildi { get; private set; }

    [EmailAddress, MaxLength(254)]
    public string? Email { get => _email; set { _email = value; EmailVerildi = true; } }
    [JsonIgnore] public bool EmailVerildi { get; private set; }

    [MaxLength(20)]
    public string? VergiNo { get => _vergiNo; set { _vergiNo = value; VergiNoVerildi = true; } }
    [JsonIgnore] public bool VergiNoVerildi { get; private set; }

    [MaxLength(500)]
    public string? Adres { get => _adres; set { _adres = value; AdresVerildi = true; } }
    [JsonIgnore] public bool AdresVerildi { get; private set; }

    [MaxLength(1000)]
    public string? Notlar { get => _notlar; set { _notlar = value; NotlarVerildi = true; } }
    [JsonIgnore] public bool NotlarVerildi { get; private set; }

    [Range(0, double.MaxValue)]
    public decimal? KrediLimitiTl { get => _krediLimitiTl; set { _krediLimitiTl = value; KrediLimitiTlVerildi = true; } }
    [JsonIgnore] public bool KrediLimitiTlVerildi { get; private set; }

    public bool? Aktif { get => _aktif; set { _aktif = value; AktifVerildi = true; } }
    [JsonIgnore] public bool AktifVerildi { get; private set; }
}

public sealed class VeresiyeGirisDto
{
    /// <summary>Verilen kredi miktarı TL (bize borçlanıyor)</summary>
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
    public decimal TlTutari { get; set; }

    /// <summary>
    /// Has gram cinsinden aynı anda verilen altın (varsa).
    /// Sıfır veya boş: sadece TL kredisi.
    /// </summary>
    [Range(0, double.MaxValue)] public decimal? HasGrami { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID (zorunlu: paralel defterde kur bağlantısı)</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class TahsilatGirisDto
{
    /// <summary>TL ödeme tutarı (nakit / havale / EFT)</summary>
    [Range(0, double.MaxValue)] public decimal? TlTutari { get; set; }

    /// <summary>Has gram ile ödeme (altın teslim)</summary>
    [Range(0, double.MaxValue)] public decimal? HasGrami { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class CariServisi(KuyumcuDbContext db, ILogger<CariServisi> logger)
{
    // ─── Cari oluştur ─────────────────────────────────────────────────────────

    public async Task<object> Olustur(CariOlusturDto dto, AktifKullanici islem)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Cariler.Add(new Data.Cari
        {
            Id = id,
            TenantId = islem.TenantId,
            Tur = dto.Tur,
            Ad = dto.Ad,
            Telefon = dto.Telefon,
            Email = dto.Email,
            VergiNo = dto.VergiNo,
            Adres = dto.Adres,
            Notlar = dto.Notlar,
            KrediLimitiTl = dto.KrediLimitiTl,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });

        db.CariBakiyeler.Add(new CariBakiye
        {
            CariId = id,
            TenantId = islem.TenantId,
            TlBakiye = 0m,
            HasGramBakiye = 0m,
            SonIslemTarihi = null,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Getir(id, islem);
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        AktifKullanici islem, string? tur, bool? aktif, string? q, int limit, int offset)
    {
        var sorgu = db.Cariler.AsNoTracking().Where(c => c.TenantId == islem.TenantId);

        if (!string.IsNullOrEmpty(tur)) sorgu = sorgu.Where(c => c.Tur == tur);
        if (aktif is not null) sorgu = sorgu.Where(c => c.Aktif == aktif);
        if (!string.IsNullOrEmpty(q)) sorgu = sorgu.Where(c => EF.Functions.Like(c.Ad, $"%{q}%"));

        var liste = await sorgu.OrderBy(c => c.Ad)
            .Skip(offset).Take(limit).ToListAsync();

        // Bakiyeleri tek sorguda çekip satırlara işle (liste ekranı bakiye gösterir)
        var idler = liste.Select(c => c.Id).ToList();
        var bakiyeler = idler.Count != 0
            ? await db.CariBakiyeler.AsNoTracking()
                .Where(b => b.TenantId == islem.TenantId && idler.Contains(b.CariId))
                .ToListAsync()
            : [];
        var bakiyeMap = bakiyeler.ToDictionary(b => b.CariId);

        return liste.Select(c =>
        {
            bakiyeMap.TryGetValue(c.Id, out var b);
            return new
            {
                id = c.Id,
                tenantId = c.TenantId,
                tur = c.Tur,
                ad = c.Ad,
                telefon = c.Telefon,
                email = c.Email,
                vergiNo = c.VergiNo,
                adres = c.Adres,
                notlar = c.Notlar,
                krediLimitiTl = c.KrediLimitiTl,
                aktif = c.Aktif,
                olusturuldu = c.Olusturuldu,
                guncellendi = c.Guncellendi,
                tlBakiye = b?.TlBakiye ?? 0m,
                hasGramBakiye = b?.HasGramBakiye ?? 0m,
            };
        }).ToList();
    }

    // ─── Tekil cari (bakiye dahil) ─────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == id);

        return new
        {
            id = cari.Id,
            tenantId = cari.TenantId,
            tur = cari.Tur,
            ad = cari.Ad,
            telefon = cari.Telefon,
            email = cari.Email,
            vergiNo = cari.VergiNo,
            adres = cari.Adres,
            notlar = cari.Notlar,
            krediLimitiTl = cari.KrediLimitiTl,
            aktif = cari.Aktif,
            olusturuldu = cari.Olusturuldu,
            guncellendi = cari.Guncellendi,
            bakiye = bakiye is null ? null : BakiyeFormat(bakiye),
        };
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, CariGuncelleDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // NOT NULL kolonlar (tur/ad/aktif) yalnızca değerle güncellenir;
        // nullable kolonlar açıkça null gönderilerek temizlenebilir.
        if (dto.TurVerildi && dto.Tur is not null) cari.Tur = dto.Tur;
        if (dto.AdVerildi && dto.Ad is not null) cari.Ad = dto.Ad;
        if (dto.TelefonVerildi) cari.Telefon = dto.Telefon;
        if (dto.EmailVerildi) cari.Email = dto.Email;
        if (dto.VergiNoVerildi) cari.VergiNo = dto.VergiNo;
        if (dto.AdresVerildi) cari.Adres = dto.Adres;
        if (dto.NotlarVerildi) cari.Notlar = dto.Notlar;
        if (dto.KrediLimitiTlVerildi) cari.KrediLimitiTl = dto.KrediLimitiTl;
        if (dto.AktifVerildi && dto.Aktif is not null) cari.Aktif = dto.Aktif.Value;
        cari.Guncellendi = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await Getir(id, islem);
    }

    // ─── Deaktif et (soft delete) ─────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        cari.Aktif = false;
        cari.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new { mesaj = "Cari deaktif edildi" };
    }

    // ─── Bakiye getir ─────────────────────────────────────────────────────────

    public async Task<object> BakiyeGetir(string id, AktifKullanici islem)
    {
        await TenantCarisiBul(id, islem.TenantId);

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == id && b.TenantId == islem.TenantId);

        return bakiye is not null
            ? BakiyeFormat(bakiye)
            : new { cariId = id, tlBakiye = 0m, hasGramBakiye = 0m, sonIslemTarihi = (DateTime?)null };
    }

    // ─── Veresiye girişi ──────────────────────────────────────────────────────
    /// <summary>
    /// Müşteri bize TL (ve/veya has gram) borçlanır.
    /// Bakiye: tlBakiye += tlTutari, hasGramBakiye += hasGrami
    /// Hareket: tlTutari = +pozitif (alacak), hasGrami = -hasGrami (altın bizden gitti)
    /// </summary>
    public async Task<object> VeresiyeGir(string id, VeresiyeGirisDto dto, AktifKullanici islem)
    {
        var cari = await TenantCarisiBul(id, islem.TenantId);

        // Kredi limiti kontrolü
        if (cari.KrediLimitiTl is not null)
        {
            var mevcutTl = await db.CariBakiyeler.AsNoTracking()
                .Where(b => b.CariId == id && b.TenantId == islem.TenantId)
                .Select(b => (decimal?)b.TlBakiye)
                .FirstOrDefaultAsync() ?? 0m;

            var yeniBakiye = mevcutTl + dto.TlTutari;
            if (yeniBakiye > cari.KrediLimitiTl)
            {
                // Nest'te bakiye ve tutar JS number'dır (kuyruk sıfırsız: 150.00 → "150"),
                // limit ise DB'den gelen ham string'dir ("1000.00") — G29 bu ayrımı korur.
                throw new BadRequestException(FormattableString.Invariant(
                    $"Kredi limiti aşılıyor. Mevcut bakiye: {mevcutTl:G29} TL, ") +
                    FormattableString.Invariant($"Eklenecek: {dto.TlTutari:G29} TL, Limit: {cari.KrediLimitiTl} TL"));
            }
        }

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var now = DateTime.UtcNow;
        var hasGrami = dto.HasGrami ?? 0m;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = "VERESIYE",
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = dto.TlTutari,   // +pozitif: alacak
            HasGrami = -hasGrami,      // -negatif: altın bizden gitti
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        // Bakiye: tlBakiye += dto.TlTutari, hasGramBakiye += hasGrami
        await BakiyeGuncelle(id, islem.TenantId, dto.TlTutari, hasGrami, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Veresiye: cari={CariId} tl={Tl} has={Has}",
            islem.TenantId, id, dto.TlTutari, hasGrami);

        return await Getir(id, islem);
    }

    // ─── Tahsilat girişi ──────────────────────────────────────────────────────
    /// <summary>
    /// Müşteri borcunu öder (TL ve/veya has gram).
    /// Bakiye: tlBakiye -= tlTutari, hasGramBakiye -= hasGrami
    /// Hareket: tlTutari = -negatif (ödeme), hasGrami = +pozitif (altın bize geldi)
    /// </summary>
    public async Task<object> TahsilatGir(string id, TahsilatGirisDto dto, AktifKullanici islem)
    {
        await TenantCarisiBul(id, islem.TenantId);

        var tlTutari = dto.TlTutari ?? 0m;
        var hasGrami = dto.HasGrami ?? 0m;

        if (tlTutari == 0m && hasGrami == 0m)
            throw new BadRequestException("Tahsilat için en az bir tutar girilmelidir (TL veya has gram)");

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            Tur = "TAHSILAT",
            Tarih = now,
            Aciklama = dto.Aciklama,
            TlTutari = -tlTutari,      // -negatif: ödeme → borç azaldı
            HasGrami = hasGrami,       // +pozitif: altın bize geldi
            KurSnapshotId = dto.KurSnapshotId,
            CariId = id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        // Bakiye: tlBakiye -= tlTutari (borç azalır), hasGramBakiye -= hasGrami (altın borcu azalır)
        await BakiyeGuncelle(id, islem.TenantId, -tlTutari, -hasGrami, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Tahsilat: cari={CariId} tl={Tl} has={Has}",
            islem.TenantId, id, tlTutari, hasGrami);

        return await Getir(id, islem);
    }

    // ─── Ekstre ───────────────────────────────────────────────────────────────

    public async Task<object> Ekstre(
        string id, AktifKullanici islem,
        DateTime? baslangic, DateTime? bitis, int limit, int offset)
    {
        await TenantCarisiBul(id, islem.TenantId);

        var sorgu = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.CariId == id);
        if (baslangic is not null) sorgu = sorgu.Where(h => h.Tarih >= baslangic);
        if (bitis is not null) sorgu = sorgu.Where(h => h.Tarih <= bitis);

        var liste = await sorgu.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        var bakiye = await BakiyeGetir(id, islem);

        return new { bakiye, hareketler = liste };
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<Data.Cari> TenantCarisiBul(string id, string tenantId) =>
        await db.Cariler.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
        ?? throw new NotFoundException("Cari bulunamadı");

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
            // Bakiye satırı yoksa (veri tutarsızlığı — yine de devam et)
            db.CariBakiyeler.Add(new CariBakiye
            {
                CariId = cariId,
                TenantId = tenantId,
                TlBakiye = tlDelta,
                HasGramBakiye = hasGramDelta,
                SonIslemTarihi = now,
                Guncellendi = now,
            });
        }
    }

    private static object BakiyeFormat(CariBakiye b) => new
    {
        cariId = b.CariId,
        tlBakiye = b.TlBakiye,
        hasGramBakiye = b.HasGramBakiye,
        sonIslemTarihi = b.SonIslemTarihi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/cari")]
[Yetki.ModulYetkisi("cari")]
public sealed class CariController(CariServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/cari — Yeni cari (müşteri/tedarikçi/atölye/sarraf) oluştur — MUHASEBE+</summary>
    /// <remarks>Nest'te @HttpCode override'ı yok: POST varsayılanı 201 Created döner.</remarks>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Olustur([FromBody] CariOlusturDto dto)
        => StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>GET /api/v1/cari?tur=&amp;aktif=&amp;q=&amp;limit=&amp;offset= — Cari listesi (bakiyeler dahil)</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? tur, [FromQuery] bool? aktif, [FromQuery] string? q,
        [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        => servis.Listele(Islem, tur, aktif, q, limit, offset);

    /// <summary>GET /api/v1/cari/{id} — Cari detayı + anlık bakiye</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>GET /api/v1/cari/{id}/bakiye — Sadece bakiye bilgisi (TL + has gram paralel)</summary>
    [HttpGet("{id}/bakiye")]
    public Task<object> BakiyeGetir(string id) => servis.BakiyeGetir(id, Islem);

    /// <summary>GET /api/v1/cari/{id}/ekstre — Hareket listesi (ekstre) — tarih filtrelemeli</summary>
    [HttpGet("{id}/ekstre")]
    public Task<object> Ekstre(
        string id,
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.Ekstre(id, Islem, baslangic, bitis, limit, offset);

    /// <summary>PATCH /api/v1/cari/{id} — Cari bilgilerini güncelle — MUHASEBE+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Guncelle(string id, [FromBody] CariGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>POST /api/v1/cari/{id}/veresiye — Veresiye girişi — TEZGAHTAR+</summary>
    [HttpPost("{id}/veresiye")]
    public Task<object> VeresiyeGir(string id, [FromBody] VeresiyeGirisDto dto)
        => servis.VeresiyeGir(id, dto, Islem);

    /// <summary>POST /api/v1/cari/{id}/tahsilat — Tahsilat girişi — TEZGAHTAR+</summary>
    [HttpPost("{id}/tahsilat")]
    public Task<object> TahsilatGir(string id, [FromBody] TahsilatGirisDto dto)
        => servis.TahsilatGir(id, dto, Islem);

    /// <summary>DELETE /api/v1/cari/{id} — Cariyi deaktif et (soft delete) — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
