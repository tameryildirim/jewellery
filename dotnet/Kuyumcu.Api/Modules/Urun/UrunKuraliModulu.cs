using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// M4 — ÜRÜN KURALLARI (müşteri 16.10: "Ayarlar bölümünde bileziği ben tanımlarım…
// içinde bilezik geçiyorsa binde bir… tek tek yapma"; "bazen 1000 işlem oluyor").
//   • Koşul: kategori ve/veya adda geçen metin (ikisi doluysa ikisi de uymalı).
//   • Değer: alış milyemi, satış milyemi, KDV'ye giren pay (binde), KDV oranı (%).
//   • Yeni üründe BOŞ alanlar uyan İLK kuraldan (sıra) dolar — elle girilen kazanır.
//   • Mevcut ürünlere ÖNİZLEME + UYGULA (varsayılan yalnız boş alanlar); denetim izine yazılır.
//   • Milyem ve binde yalnız ALTIN üründe uygulanır (K20-4); KDV oranı her üründe.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunKuraliDto
{
    [Required, MaxLength(100)] public string Ad { get; set; } = null!;
    [MaxLength(100)] public string? AdIcerir { get; set; }
    [MaxLength(36)] public string? KategoriId { get; set; }
    [Range(1, 2000)] public decimal? AlisMilyemi { get; set; }
    [Range(1, 2000)] public decimal? SatisMilyemi { get; set; }
    [Range(0.001, 1000)] public decimal? KdvMatrahBinde { get; set; }
    [Range(0, 100)] public decimal? KdvOrani { get; set; }
    [Range(0, 10_000)] public int Sira { get; set; }
    public bool Aktif { get; set; } = true;
}

public sealed class UrunKuraliServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    private static readonly CompareInfo Tr = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
    private const int OnizlemeSiniri = 500;
    private const int KuralSiniri = 200;

    /// <summary>Kural ürüne uyar mı? Ad: Türkçe büyük/küçük harf duyarsız "içerir".</summary>
    public static bool Uyar(UrunKurali k, string? urunAd, string? kategoriId)
    {
        if (string.IsNullOrWhiteSpace(k.AdIcerir) && string.IsNullOrEmpty(k.KategoriId)) return false;
        if (!string.IsNullOrEmpty(k.KategoriId) && k.KategoriId != kategoriId) return false;
        if (!string.IsNullOrWhiteSpace(k.AdIcerir)
            && Tr.IndexOf(urunAd ?? "", k.AdIcerir.Trim(), CompareOptions.IgnoreCase) < 0) return false;
        return true;
    }

    private static bool Altin(string? ayarId) =>
        (Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayarId ?? "") ?? Kuyumcu.Core.Domain.Maden.Altin) == Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>Ürün oluşturmada: uyan ilk kural (yoksa null). Sıra küçük olan önce.</summary>
    public static async Task<UrunKurali?> IlkUyan(KuyumcuDbContext db, string tenantId, string? ad, string? kategoriId)
    {
        var kurallar = await db.Set<UrunKurali>().AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.Aktif)
            .OrderBy(k => k.Sira).ThenBy(k => k.Olusturuldu)
            .Take(KuralSiniri)
            .ToListAsync();
        return kurallar.FirstOrDefault(k => Uyar(k, ad, kategoriId));
    }

    /// <summary>Boş alanları kuraldan doldurur; değişen alan adlarını döner.</summary>
    public static List<string> Doldur(Data.Urun u, UrunKurali k, bool yalnizBos = true)
    {
        var degisen = new List<string>();
        var altin = Altin(u.AyarId);
        if (altin && k.AlisMilyemi is { } am && (!yalnizBos || u.AlisMilyemi is null) && u.AlisMilyemi != am)
        { u.AlisMilyemi = am; degisen.Add("alisMilyemi"); }
        if (altin && k.SatisMilyemi is { } sm && (!yalnizBos || u.SatisMilyemi is null) && u.SatisMilyemi != sm)
        { u.SatisMilyemi = sm; degisen.Add("satisMilyemi"); }
        if (altin && k.KdvMatrahBinde is { } kb && (!yalnizBos || u.KdvMatrahBinde is null) && u.KdvMatrahBinde != kb)
        { u.KdvMatrahBinde = kb; degisen.Add("kdvMatrahBinde"); }
        if (k.KdvOrani is { } ko && (!yalnizBos || u.KdvOrani is null) && u.KdvOrani != ko)
        { u.KdvOrani = ko; degisen.Add("kdvOrani"); }
        return degisen;
    }

    private static void Denetle(UrunKuraliDto dto)
    {
        var ad = (dto.Ad ?? "").Trim();
        if (ad.Length == 0) throw new BadRequestException("Kural adı boş olamaz").AlanIle("ad");
        if (string.IsNullOrWhiteSpace(dto.AdIcerir) && string.IsNullOrWhiteSpace(dto.KategoriId))
            throw new BadRequestException("Kural için kategori ya da adda geçen metin seçin").AlanIle("adIcerir");
        if (dto.AlisMilyemi is null && dto.SatisMilyemi is null && dto.KdvMatrahBinde is null && dto.KdvOrani is null)
            throw new BadRequestException("Kural en az bir değer taşımalı").AlanIle("alisMilyemi");
        GirdiKapisi.Denetle(SatisMilyemiKurali.Denetle(dto.AlisMilyemi), "alisMilyemi");
        GirdiKapisi.Denetle(SatisMilyemiKurali.Denetle(dto.SatisMilyemi), "satisMilyemi");
        GirdiKapisi.Denetle(KdvMatrahPayiKurali.Denetle(dto.KdvMatrahBinde), "kdvMatrahBinde");
        if (dto.KdvOrani is { } ko && (ko < 0m || ko > 100m || decimal.Round(ko, 2) != ko))
            throw new BadRequestException("KDV oranı 0 ile 100 arasında olmalıdır").AlanIle("kdvOrani");
    }

    private async Task KategoriVarMi(string? kategoriId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(kategoriId)) return;
        if (!await db.UrunKategorileri.AnyAsync(k => k.Id == kategoriId && k.TenantId == tenantId))
            throw new NotFoundException("Kategori bulunamadı");
    }

    private static object Yanit(UrunKurali k) => new
    {
        k.Id, k.Ad, k.AdIcerir, k.KategoriId, k.AlisMilyemi, k.SatisMilyemi, k.KdvMatrahBinde, k.KdvOrani, k.Sira, k.Aktif,
    };

    public async Task<object> Liste(AktifKullanici islem) =>
        (await db.Set<UrunKurali>().AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira).ThenBy(k => k.Olusturuldu)
            .Take(KuralSiniri)
            .ToListAsync()).Select(Yanit).ToList();

    public async Task<object> Olustur(UrunKuraliDto dto, AktifKullanici islem)
    {
        Denetle(dto);
        await KategoriVarMi(dto.KategoriId, islem.TenantId);
        var now = DateTime.UtcNow;
        var k = new UrunKurali
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Ad = dto.Ad.Trim(),
            AdIcerir = string.IsNullOrWhiteSpace(dto.AdIcerir) ? null : dto.AdIcerir.Trim(),
            KategoriId = string.IsNullOrWhiteSpace(dto.KategoriId) ? null : dto.KategoriId,
            AlisMilyemi = dto.AlisMilyemi, SatisMilyemi = dto.SatisMilyemi, KdvMatrahBinde = dto.KdvMatrahBinde,
            KdvOrani = dto.KdvOrani, Sira = dto.Sira, Aktif = dto.Aktif, Olusturuldu = now, Guncellendi = now,
        };
        db.Add(k);
        await db.SaveChangesAsync();
        return Yanit(k);
    }

    public async Task<object> Guncelle(string id, UrunKuraliDto dto, AktifKullanici islem)
    {
        Denetle(dto);
        await KategoriVarMi(dto.KategoriId, islem.TenantId);
        var k = await db.Set<UrunKurali>().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kural bulunamadı");
        k.Ad = dto.Ad.Trim();
        k.AdIcerir = string.IsNullOrWhiteSpace(dto.AdIcerir) ? null : dto.AdIcerir.Trim();
        k.KategoriId = string.IsNullOrWhiteSpace(dto.KategoriId) ? null : dto.KategoriId;
        k.AlisMilyemi = dto.AlisMilyemi; k.SatisMilyemi = dto.SatisMilyemi;
        k.KdvMatrahBinde = dto.KdvMatrahBinde; k.KdvOrani = dto.KdvOrani;
        k.Sira = dto.Sira; k.Aktif = dto.Aktif; k.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Yanit(k);
    }

    private async Task<(UrunKurali Kural, List<Data.Urun> Uyanlar)> Uyanlari(string id, AktifKullanici islem, bool izle)
    {
        var k = await db.Set<UrunKurali>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kural bulunamadı");
        var q = db.Urunler.Where(u => u.TenantId == islem.TenantId && u.Aktif);
        if (!izle) q = q.AsNoTracking();
        if (!string.IsNullOrEmpty(k.KategoriId)) q = q.Where(u => u.KategoriId == k.KategoriId);
        // Ad süzgeci Türkçe harf duyarsız olduğu için bellekte uygulanır; aday kümesi kiracıyla sınırlı.
        var adaylar = await q.OrderBy(u => u.Kod).Take(20_000).ToListAsync();
        return (k, adaylar.Where(u => Uyar(k, u.Ad, u.KategoriId)).ToList());
    }

    public async Task<object> Onizleme(string id, bool yalnizBos, AktifKullanici islem)
    {
        var (k, uyanlar) = await Uyanlari(id, islem, izle: false);
        var satirlar = new List<object>();
        var degisecek = 0;
        foreach (var u in uyanlar)
        {
            var kopya = new Data.Urun
            {
                Id = u.Id, TenantId = u.TenantId, Tip = u.Tip, Kod = u.Kod, Ad = u.Ad, AyarId = u.AyarId,
                AlisMilyemi = u.AlisMilyemi, SatisMilyemi = u.SatisMilyemi, KdvMatrahBinde = u.KdvMatrahBinde, KdvOrani = u.KdvOrani,
            };
            var alanlar = Doldur(kopya, k, yalnizBos);
            if (alanlar.Count > 0) degisecek++;
            if (satirlar.Count < OnizlemeSiniri)
                satirlar.Add(new { u.Id, u.Kod, u.Ad, degisenAlanlar = alanlar,
                    u.AlisMilyemi, u.SatisMilyemi, u.KdvMatrahBinde, u.KdvOrani });
        }
        return new { kuralId = k.Id, uyanSayisi = uyanlar.Count, degisecekSayisi = degisecek, satirlar };
    }

    public async Task<object> Uygula(string id, bool yalnizBos, AktifKullanici islem)
    {
        var (k, uyanlar) = await Uyanlari(id, islem, izle: true);
        var degisen = 0;
        var now = DateTime.UtcNow;
        foreach (var u in uyanlar)
            if (Doldur(u, k, yalnizBos).Count > 0) { u.Guncellendi = now; degisen++; }
        if (degisen > 0)
        {
            denetim?.YazmaKaydet(islem, nameof(UrunKurali), k.Id,
                $"Ürün kuralı \"{k.Ad}\" {degisen} ürüne uygulandı ({(yalnizBos ? "yalnız boş alanlar" : "üzerine yazarak")}).",
                hassas: false);
            await db.SaveChangesAsync();
        }
        return new { kuralId = k.Id, uyanSayisi = uyanlar.Count, degisenSayisi = degisen };
    }
}

[ApiController]
[Authorize]
[Route("api/v1/urun-kurallari")]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunKuraliController(UrunKuraliServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpGet]
    public Task<object> Liste() => servis.Liste(Islem);

    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] UrunKuraliDto dto) => servis.Olustur(dto, Islem);

    [HttpPut("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] UrunKuraliDto dto) => servis.Guncelle(id, dto, Islem);

    [HttpGet("{id}/onizleme")]
    public Task<object> Onizleme(string id, [FromQuery] bool yalnizBos = true) => servis.Onizleme(id, yalnizBos, Islem);

    [HttpPost("{id}/uygula")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Uygula(string id, [FromQuery] bool yalnizBos = true) => servis.Uygula(id, yalnizBos, Islem);
}
