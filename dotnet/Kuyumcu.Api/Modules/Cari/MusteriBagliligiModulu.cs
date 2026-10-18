using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// D2-12 (18.10, rakip eşitleme: "sadakat/puan, uykuda müşteri, dilek listesi, yüzük ölçüsü").
//   • Sadakat: işletme ayarıyla açılır (her X TL net alışveriş = 1 puan, başlangıç tarihinden). Kazanılan puan HESAPLANIR
//     (satış − iade, storno çiftleri hariç); kullanım kayıt olarak düşülür. Mali etkisi YOK — karşılık tezgâhta
//     iskonto/hediye olarak işlenir (puanın KDV/indirim muhasebesi uydurulmaz).
//   • Uykuda müşteri: en az bir alışverişi olan ve son alışverişi N günden eski müşteriler (Peşin hariç).
//   • Dilek listesi: müşteri başına, isteğe bağlı katalog ürünü; kapatılır (silinmez).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SadakatAyarDto
{
    [Range(1, 10_000_000, ErrorMessage = "Puan başına tutar sıfırdan büyük olmalıdır")] public decimal? TlBasina { get; set; }
    public DateOnly? Baslangic { get; set; }
}

public sealed class PuanKullanDto
{
    [Range(1, 1_000_000, ErrorMessage = "Puan sıfırdan büyük olmalıdır")] public int Puan { get; set; }
    [Required(ErrorMessage = "Açıklama zorunludur"), MaxLength(AlanSinirlari.MusteriNotu)] public string Aciklama { get; set; } = null!;
}

public sealed class DilekDto
{
    [MaxLength(36)] public string? UrunId { get; set; }
    [Required(ErrorMessage = "Açıklama zorunludur"), MaxLength(AlanSinirlari.MusteriNotu)] public string Aciklama { get; set; } = null!;
}

public sealed class MusteriBagliligiServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public const string AnahtarTlBasina = KiraciAyarAnahtari.SadakatTlBasina, AnahtarBaslangic = KiraciAyarAnahtari.SadakatBaslangic;

    public async Task<SadakatAyari> Ayar(string tenantId)
    {
        var d = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && (a.Anahtar == AnahtarTlBasina || a.Anahtar == AnahtarBaslangic))
            .Take(2).ToDictionaryAsync(a => a.Anahtar, a => a.Deger);
        decimal? tl = decimal.TryParse(d.GetValueOrDefault(AnahtarTlBasina), NumberStyles.Number, CultureInfo.InvariantCulture, out var t) && t > 0m ? t : null;
        DateOnly? bas = DateOnly.TryParseExact(d.GetValueOrDefault(AnahtarBaslangic), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var b) ? b : null;
        return new SadakatAyari(tl, bas);
    }

    public async Task<SadakatAyari> AyarYaz(SadakatAyarDto dto, AktifKullanici islem)
    {
        if (dto.TlBasina is <= 0m) throw new BadRequestException("Puan başına tutar sıfırdan büyük olmalıdır").AlanIle("tlBasina");
        var deger = new Dictionary<string, string>
        {
            [AnahtarTlBasina] = dto.TlBasina?.ToString(CultureInfo.InvariantCulture) ?? "",
            [AnahtarBaslangic] = dto.TlBasina is null ? "" : (dto.Baslangic ?? Zaman.YerelBugun()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        var mevcut = await db.KiraciAyarlari.Where(a => a.TenantId == islem.TenantId && (a.Anahtar == AnahtarTlBasina || a.Anahtar == AnahtarBaslangic)).Take(2).ToListAsync();
        foreach (var (k, v) in deger)
        {
            var a = mevcut.FirstOrDefault(x => x.Anahtar == k);
            if (a is null) { a = new KiraciAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = k, Deger = "" }; db.KiraciAyarlari.Add(a); }
            a.Deger = v; a.Guncellendi = DateTime.UtcNow;
        }
        denetim.YazmaKaydet(islem, nameof(KiraciAyari), islem.TenantId, $"sadakat ayarı: {deger[AnahtarTlBasina]} TL/puan · {deger[AnahtarBaslangic]}", hassas: false);
        await db.SaveChangesAsync();
        return await Ayar(islem.TenantId);
    }

    public async Task<SadakatDurumu> Durum(string cariId, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        var ayar = await Ayar(islem.TenantId);
        var kullanimlar = await db.SadakatPuanKullanimlari.AsNoTracking().Where(x => x.TenantId == islem.TenantId && x.CariId == cariId)
            .OrderByDescending(x => x.Tarih).Take(50).ToListAsync();
        var kullanilan = await db.SadakatPuanKullanimlari.AsNoTracking().Where(x => x.TenantId == islem.TenantId && x.CariId == cariId).SumAsync(x => (int?)x.Puan) ?? 0;
        var idler = kullanimlar.Select(x => x.KullaniciId).Distinct().ToList();
        var adlar = await db.Kullanicilar.AsNoTracking().Where(u => u.TenantId == islem.TenantId && idler.Contains(u.Id)).Take(idler.Count + 1)
            .ToDictionaryAsync(u => u.Id, u => (u.Ad + " " + u.Soyad).Trim());
        var liste = kullanimlar.Select(x => new PuanKullanimOzet(x.Puan, x.Aciklama, x.Tarih, adlar.GetValueOrDefault(x.KullaniciId))).ToList();
        if (ayar.TlBasina is not { } tl || ayar.Baslangic is not { } bas)
            return new SadakatDurumu(false, null, null, 0m, 0, kullanilan, 0, liste);
        var (b0, _) = Zaman.YerelGunAraligiUtc(bas);
        var net = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.CariId == cariId && h.Tarih >= b0 && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"))
            .StornoCiftiniDisla(db, islem.TenantId)
            .SumAsync(h => (decimal?)(h.Tur == "SATIS" ? Math.Abs(h.TlTutari) : -Math.Abs(h.TlTutari))) ?? 0m;
        var kazanilan = net > 0m ? (int)Math.Floor(net / tl) : 0;
        return new SadakatDurumu(true, tl, bas, Math.Round(net, 2), kazanilan, kullanilan, kazanilan - kullanilan, liste);
    }

    public async Task<SadakatDurumu> Kullan(string cariId, PuanKullanDto dto, AktifKullanici islem)
    {
        if (dto.Puan < 1) throw new BadRequestException("Puan sıfırdan büyük olmalıdır").AlanIle("puan");
        if (string.IsNullOrWhiteSpace(dto.Aciklama)) throw new BadRequestException("Açıklama zorunludur").AlanIle("aciklama");
        var d = await Durum(cariId, islem);
        if (!d.Etkin) throw new BadRequestException("Sadakat puanı işletme ayarlarında kapalı").AlanIle("puan");
        if (dto.Puan > d.Bakiye) throw new BadRequestException("Müşterinin puanı yetmiyor").AlanIle("puan");
        db.SadakatPuanKullanimlari.Add(new SadakatPuanKullanimi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, CariId = cariId, Puan = dto.Puan, Aciklama = dto.Aciklama.Trim(),
            KullaniciId = islem.Id, Tarih = DateTime.UtcNow,
        });
        denetim.YazmaKaydet(islem, nameof(SadakatPuanKullanimi), cariId, $"sadakat puanı kullanıldı · {dto.Puan}", hassas: false);
        await db.SaveChangesAsync();
        return await Durum(cariId, islem);
    }

    public async Task<List<CariDilekOzet>> Dilekler(string cariId, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        var l = await db.CariDilekleri.AsNoTracking().Where(x => x.TenantId == islem.TenantId && x.CariId == cariId)
            .OrderBy(x => x.Kapatildi != null).ThenByDescending(x => x.Olusturuldu).Take(100).ToListAsync();
        var urunIdler = l.Where(x => x.UrunId != null).Select(x => x.UrunId!).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id)).Take(urunIdler.Count + 1)
            .ToDictionaryAsync(u => u.Id, u => u.Ad);
        return [.. l.Select(x => new CariDilekOzet(x.Id, x.Aciklama, x.UrunId, x.UrunId is null ? null : urunler.GetValueOrDefault(x.UrunId), x.Olusturuldu, x.Kapatildi != null))];
    }

    public async Task<List<CariDilekOzet>> DilekEkle(string cariId, DilekDto dto, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        if (string.IsNullOrWhiteSpace(dto.Aciklama)) throw new BadRequestException("Açıklama zorunludur").AlanIle("aciklama");
        var urunId = string.IsNullOrEmpty(dto.UrunId) ? null : dto.UrunId;
        if (urunId is not null && !await db.Urunler.AsNoTracking().AnyAsync(u => u.Id == urunId && u.TenantId == islem.TenantId))
            throw new BadRequestException("Ürün bulunamadı").AlanIle("urunId");
        db.CariDilekleri.Add(new CariDilek
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, CariId = cariId, UrunId = urunId, Aciklama = dto.Aciklama.Trim(),
            OlusturanId = islem.Id, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return await Dilekler(cariId, islem);
    }

    public async Task<List<CariDilekOzet>> DilekKapat(string id, AktifKullanici islem)
    {
        var d = await db.CariDilekleri.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Dilek bulunamadı");
        d.Kapatildi ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await Dilekler(d.CariId, islem);
    }

    /// <summary>Son alışverişi <paramref name="gun"/> günden eski (ve en az bir alışverişi olan) müşteriler — en uzun uyuyan önce.</summary>
    public async Task<List<UykudaMusteri>> Uykuda(int gun, AktifKullanici islem)
    {
        if (gun is < 30 or > 3650) throw new BadRequestException("Gün 30 ile 3650 arasında olmalıdır").AlanIle("gun");
        var esik = DateTime.UtcNow.AddDays(-gun);
        var gruplar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == "SATIS" && h.CariId != null)
            .StornoCiftiniDisla(db, islem.TenantId)
            .GroupBy(h => h.CariId!)
            .Select(g => new { CariId = g.Key, Son = g.Max(x => x.Tarih), Toplam = g.Sum(x => (decimal?)Math.Abs(x.TlTutari)) ?? 0m })
            .Where(g => g.Son < esik)
            .OrderBy(g => g.Son).Take(500).ToListAsync();
        var idler = gruplar.Select(g => g.CariId).ToList();
        var cariler = await db.Cariler.AsNoTracking().Where(c => c.TenantId == islem.TenantId && idler.Contains(c.Id) && !c.SistemCarisi && c.Aktif)
            .Take(idler.Count + 1).ToDictionaryAsync(c => c.Id, c => new { c.Ad, c.Telefon });
        var bugun = DateTime.UtcNow;
        return [.. gruplar.Where(g => cariler.ContainsKey(g.CariId)).Select(g => new UykudaMusteri(g.CariId, cariler[g.CariId].Ad, cariler[g.CariId].Telefon,
            g.Son, (int)(bugun - g.Son).TotalDays, Math.Round(g.Toplam, 2)))];
    }
}

[ApiController]
[Authorize]
[Route("api/v1")]
[Yetki.ModulYetkisi("cari")]
public sealed class MusteriBagliligiController(MusteriBagliligiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/sadakat/ayar</summary>
    [HttpGet("sadakat/ayar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<SadakatAyari> Ayar() => servis.Ayar(Islem.TenantId);

    /// <summary>PUT /api/v1/sadakat/ayar — boş tutar = kapalı (sahip/müdür).</summary>
    [HttpPut("sadakat/ayar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<SadakatAyari> AyarYaz([FromBody] SadakatAyarDto dto) => servis.AyarYaz(dto, Islem);

    /// <summary>GET /api/v1/cari/{id}/sadakat — kazanılan/kullanılan/bakiye.</summary>
    [HttpGet("cari/{id}/sadakat")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<SadakatDurumu> Durum(string id) => servis.Durum(id, Islem);

    /// <summary>POST /api/v1/cari/{id}/sadakat/kullan — puan kullanımı (mali etkisi yok).</summary>
    [HttpPost("cari/{id}/sadakat/kullan")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public Task<SadakatDurumu> Kullan(string id, [FromBody] PuanKullanDto dto) => servis.Kullan(id, dto, Islem);

    /// <summary>GET /api/v1/cari/{id}/dilekler</summary>
    [HttpGet("cari/{id}/dilekler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<List<CariDilekOzet>> Dilekler(string id) => servis.Dilekler(id, Islem);

    /// <summary>POST /api/v1/cari/{id}/dilekler</summary>
    [HttpPost("cari/{id}/dilekler")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public Task<List<CariDilekOzet>> DilekEkle(string id, [FromBody] DilekDto dto) => servis.DilekEkle(id, dto, Islem);

    /// <summary>POST /api/v1/cari/dilekler/{id}/kapat</summary>
    [HttpPost("cari/dilekler/{id}/kapat")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public Task<List<CariDilekOzet>> DilekKapat(string id) => servis.DilekKapat(id, Islem);

    /// <summary>GET /api/v1/cari/uykuda?gun=180 — son alışverişi N günden eski müşteriler.</summary>
    [HttpGet("cari/uykuda")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<List<UykudaMusteri>> Uykuda([FromQuery] int gun = 180) => servis.Uykuda(gun, Islem);
}
