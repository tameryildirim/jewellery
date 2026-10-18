using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Isletme;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.DisApi;

// ═════════════════════════════════════════════════════════════════════════════
// D3-11 (19.10, rakip eşitleme: "dış API, WebSocket kur yayını").
//   • Yönetim (yalnız SAHİP, JWT): GET/POST /api/v1/api-anahtarlari · POST …/{id}/iptal. Anahtar bir kez gösterilir.
//   • Dış okuma (anahtar başlığı X-Api-Anahtari, JWT YOK): GET /api/disa/v1/kur · GET /api/disa/v1/urunler.
//     Anahtar sistem kapsamında ÖNEK + SABİT ZAMANLI özet karşılaştırmasıyla bulunur; veri KİRACIYA SABİT kapsamda
//     okunur (JWT yolu ve kiracı süzgeci değişmez). Yalnız OKUMA; kapsam dışı uç 403.
//   • Canlı kur: SignalR /hubs/disa-kur?anahtar=… — KUR kapsamlı anahtar kiracının "disa:" grubuna girer;
//     kur yazıldığında uygulama kanalıyla AYNI veri ("KurGuncellendi") yayınlanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ApiAnahtariDto
{
    [Required(ErrorMessage = "Ad zorunludur"), MaxLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.ApiAnahtariAdi)] public string Ad { get; set; } = null!;
    [Required, MinLength(1, ErrorMessage = "En az bir kapsam seçin")] public List<string> Kapsamlar { get; set; } = [];
}

public static class DisApiAnahtar
{
    public const string Baslik = "X-Api-Anahtari";
    public const string On = "mhk_";

    public static string Uret() => On + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    public static string Ozet(string anahtar) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(anahtar))).ToLowerInvariant();
    public static string Onek(string anahtar) => anahtar.Length >= On.Length + 8 ? anahtar.Substring(On.Length, 8) : "";

    public static string Gecersiz => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("API anahtarı geçersiz ya da iptal edilmiş");
    public static string KapsamDisi => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu API anahtarının bu bilgiye izni yok");

    /// <summary>Anahtarı doğrular (sistem kapsamı — izinli kullanım 14): (kiracı, kapsamlar) ya da null. Son kullanım damgalanır.</summary>
    public static async Task<(string TenantId, string[] Kapsamlar)?> Dogrula(SistemKapsami kapsam, string? anahtar)
    {
        if (string.IsNullOrWhiteSpace(anahtar) || !anahtar.StartsWith(On, StringComparison.Ordinal) || anahtar.Length > 100) return null;
        var onek = Onek(anahtar);
        var ozet = Ozet(anahtar);
        await using var s = kapsam.Sistem();
        var db = s.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var adaylar = await db.ApiAnahtarlari.Where(a => a.Onek == onek && a.IptalZamani == null).Take(5).ToListAsync();
        var a = adaylar.FirstOrDefault(x => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(x.Ozet), Encoding.ASCII.GetBytes(ozet)));
        if (a is null) return null;
        if (a.SonKullanim is null || a.SonKullanim < DateTime.UtcNow.AddMinutes(-5)) { a.SonKullanim = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return (a.TenantId, a.Kapsamlar.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class ApiAnahtariServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public async Task<List<ApiAnahtariOzet>> Liste(AktifKullanici islem) =>
        [.. (await db.ApiAnahtarlari.AsNoTracking().Where(a => a.TenantId == islem.TenantId).OrderByDescending(a => a.Olusturuldu).Take(50).ToListAsync())
            .Select(Ozetle)];

    public async Task<YeniApiAnahtari> Olustur(ApiAnahtariDto dto, AktifKullanici islem)
    {
        var kapsamlar = dto.Kapsamlar.Distinct().ToList();
        if (kapsamlar.Count == 0 || kapsamlar.Any(k => k is not (ApiAnahtari.KapsamKur or ApiAnahtari.KapsamUrun)))
            throw new BadRequestException("Kapsam KUR ya da URUN olmalıdır").AlanIle("kapsamlar");
        if (string.IsNullOrWhiteSpace(dto.Ad)) throw new BadRequestException("Ad zorunludur").AlanIle("ad");
        if (await db.ApiAnahtarlari.CountAsync(a => a.TenantId == islem.TenantId && a.IptalZamani == null) >= 20)
            throw new ConflictException("En çok 20 etkin API anahtarı olabilir — kullanılmayanı iptal edin");
        var anahtar = DisApiAnahtar.Uret();
        var a = new ApiAnahtari
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Ad = dto.Ad.Trim(), Onek = DisApiAnahtar.Onek(anahtar), Ozet = DisApiAnahtar.Ozet(anahtar),
            Kapsamlar = string.Join(',', kapsamlar), OlusturanId = islem.Id, Olusturuldu = DateTime.UtcNow,
        };
        db.ApiAnahtarlari.Add(a);
        denetim.YazmaKaydet(islem, nameof(ApiAnahtari), a.Id, $"dış API anahtarı oluşturuldu · {a.Ad} · {a.Kapsamlar}", hassas: true);
        await db.SaveChangesAsync();
        return new YeniApiAnahtari(Ozetle(a), anahtar);
    }

    public async Task<ApiAnahtariOzet> Iptal(string id, AktifKullanici islem)
    {
        var a = await db.ApiAnahtarlari.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("API anahtarı bulunamadı");
        if (a.IptalZamani is null)
        {
            a.IptalZamani = DateTime.UtcNow;
            denetim.YazmaKaydet(islem, nameof(ApiAnahtari), a.Id, $"dış API anahtarı iptal · {a.Ad}", hassas: true);
            await db.SaveChangesAsync();
        }
        return Ozetle(a);
    }

    private static ApiAnahtariOzet Ozetle(ApiAnahtari a) =>
        new(a.Id, a.Ad, a.Onek, a.Kapsamlar.Split(',', StringSplitOptions.RemoveEmptyEntries), a.Olusturuldu, a.SonKullanim, a.IptalZamani is not null);
}

[ApiController]
[Authorize]
[Route("api/v1/api-anahtarlari")]
[RolGerekli("SAHIP")]
public sealed class ApiAnahtariController(ApiAnahtariServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/api-anahtarlari</summary>
    [HttpGet] public Task<List<ApiAnahtariOzet>> Liste() => servis.Liste(Islem);

    /// <summary>POST /api/v1/api-anahtarlari — tam anahtar yalnız bu yanıtta.</summary>
    [HttpPost] public Task<YeniApiAnahtari> Olustur([FromBody] ApiAnahtariDto dto) => servis.Olustur(dto, Islem);

    /// <summary>POST /api/v1/api-anahtarlari/{id}/iptal</summary>
    [HttpPost("{id}/iptal")] public Task<ApiAnahtariOzet> Iptal(string id) => servis.Iptal(id, Islem);
}

[ApiController]
[AllowAnonymous]
[Route("api/disa/v1")]
public sealed class DisApiController(SistemKapsami kapsam) : ControllerBase
{
    private async Task<string> Kiraci(string gereken)
    {
        var d = await DisApiAnahtar.Dogrula(kapsam, Request.Headers[DisApiAnahtar.Baslik].FirstOrDefault())
            ?? throw new UnauthorizedException(DisApiAnahtar.Gecersiz);
        if (!d.Kapsamlar.Contains(gereken)) throw new ForbiddenException(DisApiAnahtar.KapsamDisi);
        return d.TenantId;
    }

    /// <summary>GET /api/disa/v1/kur — güncel kur (uygulamanın gördüğü veriyle aynı).</summary>
    [HttpGet("kur")]
    public async Task<object> Kur()
    {
        var t = await Kiraci(ApiAnahtari.KapsamKur);
        // SistemKapsami izinli kullanım 15: kiracıya SABİT kapsam (dış API okuması).
        await using var k = kapsam.Kiraci(t);
        return await k.ServiceProvider.GetRequiredService<Kur.KurServisi>().GuncelKur(t);
    }

    /// <summary>GET /api/disa/v1/urunler?limit=&amp;offset= — aktif ürünler, toplam stok ve güncel liste fiyatı (varsa).</summary>
    [HttpGet("urunler")]
    public async Task<object> Urunler([FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        var t = await Kiraci(ApiAnahtari.KapsamUrun);
        limit = Math.Clamp(limit, 1, 500); offset = Math.Max(0, offset);
        await using var k = kapsam.Kiraci(t);
        var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var toplam = await db.Urunler.AsNoTracking().CountAsync(u => u.TenantId == t && u.Aktif);
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == t && u.Aktif)
            .OrderBy(u => u.Kod).ThenBy(u => u.Id).Skip(offset).Take(limit).ToListAsync();
        var idler = urunler.Select(u => u.Id).ToList();
        var stok = await db.Stoklar.AsNoTracking().Where(s => s.TenantId == t && idler.Contains(s.UrunId))
            .GroupBy(s => s.UrunId).Select(g => new { g.Key, Adet = g.Sum(x => x.Adet) }).ToDictionaryAsync(x => x.Key, x => x.Adet);
        var kur = await db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == t && !s.IptalEdildi)
            .OrderByDescending(s => s.Olusturuldu).Select(s => (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync();
        var fiyat = new Fiyat.FiyatServisi(db);
        var bugun = Zaman.YerelBugun();
        var satirlar = new List<DisUrun>(urunler.Count);
        foreach (var u in urunler)
        {
            decimal? liste = null;
            if (kur is { } hk) liste = (await fiyat.Teklif(u, null, hk, bugun))?.SatisFiyati;
            satirlar.Add(new DisUrun(u.Kod, u.Ad, u.Barkod, u.AyarId, u.GramAgirlik, u.HasGrami, stok.GetValueOrDefault(u.Id), liste));
        }
        return new { toplam, limit, offset, urunler = satirlar };
    }
}

/// <summary>D3-11 — dış canlı kur kanalı: /hubs/disa-kur?anahtar=… (KUR kapsamlı anahtar). Yalnız dinler.</summary>
[AllowAnonymous]
public sealed class DisaKurHub(SistemKapsami kapsam) : Hub
{
    public static string Grup(string tenantId) => "disa:" + tenantId;

    public override async Task OnConnectedAsync()
    {
        var anahtar = Context.GetHttpContext()?.Request.Query["anahtar"].FirstOrDefault();
        var d = await DisApiAnahtar.Dogrula(kapsam, anahtar);
        if (d is null || !d.Value.Kapsamlar.Contains(ApiAnahtari.KapsamKur)) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, Grup(d.Value.TenantId));
        await base.OnConnectedAsync();
    }
}
