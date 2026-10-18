using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Urunler;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// D1-16 (18.10, rakip eşitleme: "set") — ürün setleri. Set barkodu okutma ad alanını ürünlerle paylaşır
// (ürün barkodu · toptancı barkodu · set barkodu birbirine çakışamaz). PUT setin TAMAMINI yazar (ad, barkod,
// aktiflik, parçalar); parçalar aynı işletmenin AKTİF ürünleri olmalıdır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunSetiKalemDto
{
    [Required] public string UrunId { get; set; } = null!;
    [Range(typeof(decimal), "0.001", "100000", ErrorMessage = "Adet 0,001 ile 100000 arasında olmalıdır", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Adet { get; set; } = 1m;
}

public sealed class UrunSetiDto
{
    [Required(ErrorMessage = "Set adı zorunludur"), MaxLength(AlanSinirlari.UrunSetiAdi)] public string Ad { get; set; } = null!;
    [Required(ErrorMessage = "Set barkodu zorunludur"), Barkod] public string Barkod { get; set; } = null!;
    public bool Aktif { get; set; } = true;
    [Required] public List<UrunSetiKalemDto> Kalemler { get; set; } = [];
}

public sealed class UrunSetiServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public async Task<List<UrunSetiVeri>> Liste(AktifKullanici islem)
    {
        var setler = await db.UrunSetleri.AsNoTracking().Where(s => s.TenantId == islem.TenantId)
            .OrderBy(s => s.Ad).Take(500).ToListAsync();
        var idler = setler.Select(s => s.Id).ToList();
        var kalemler = await db.UrunSetiKalemleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && idler.Contains(k.SetId))
            .OrderBy(k => k.Sira).Take(500 * UrunSeti.AzamiKalem).ToListAsync();
        var urunIdler = kalemler.Select(k => k.UrunId).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Ad, u.Kod }).Take(urunIdler.Count + 1).ToDictionaryAsync(u => u.Id);
        return [.. setler.Select(s => new UrunSetiVeri(s.Id, s.Ad, s.Barkod, s.Aktif,
            [.. kalemler.Where(k => k.SetId == s.Id).Select(k => new UrunSetiKalemVeri(k.UrunId,
                urunler.GetValueOrDefault(k.UrunId)?.Ad ?? "—", urunler.GetValueOrDefault(k.UrunId)?.Kod ?? "", k.Adet))]))];
    }

    public async Task<UrunSetiVeri> BarkodBul(string barkod, AktifKullanici islem)
    {
        var s = await db.UrunSetleri.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.Barkod == barkod && x.Aktif)
            ?? throw new NotFoundException($"'{barkod}' barkodlu set bulunamadı");
        return (await Liste(islem)).Single(x => x.Id == s.Id);
    }

    public Task<UrunSetiVeri> Olustur(UrunSetiDto dto, AktifKullanici islem) => Yaz(null, dto, islem);
    public Task<UrunSetiVeri> Guncelle(string id, UrunSetiDto dto, AktifKullanici islem) => Yaz(id, dto, islem);

    private async Task<UrunSetiVeri> Yaz(string? id, UrunSetiDto dto, AktifKullanici islem)
    {
        var ad = (dto.Ad ?? "").Trim();
        if (ad.Length == 0) throw new BadRequestException("Set adı zorunludur").AlanIle("ad");
        if (ad.Length > AlanSinirlari.UrunSetiAdi) throw new BadRequestException("Set adı en çok 200 karakter olabilir").AlanIle("ad");
        var barkod = (dto.Barkod ?? "").Trim();
        GirdiKapisi.Denetle(BarkodKurali.Denetle(barkod), "barkod");
        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count < 2) throw new BadRequestException("Sette en az iki parça olmalıdır").AlanIle("kalemler");
        if (kalemler.Count > UrunSeti.AzamiKalem) throw new BadRequestException("Sette en çok 20 parça olabilir").AlanIle("kalemler");
        if (kalemler.Any(k => k.Adet <= 0m)) throw new BadRequestException("Adet sıfırdan büyük olmalıdır").AlanIle("kalemler");
        var urunIdler = kalemler.Select(k => k.UrunId).Distinct().ToList();
        if (urunIdler.Count != kalemler.Count) throw new BadRequestException("Aynı ürün sette iki kez olamaz").AlanIle("kalemler");
        var gecerli = await db.Urunler.CountAsync(u => u.TenantId == islem.TenantId && u.Aktif && urunIdler.Contains(u.Id));
        if (gecerli != urunIdler.Count) throw new BadRequestException("Setteki bir ürün bulunamadı ya da pasif").AlanIle("kalemler");
        // Okutma ad alanı TEK: ürün barkodu, toptancı barkodu ve başka setin barkodu çakışamaz.
        if (await db.Urunler.AnyAsync(u => u.TenantId == islem.TenantId && (u.Barkod == barkod || u.TedarikciBarkodu == barkod))
            || await db.UrunSetleri.AnyAsync(s => s.TenantId == islem.TenantId && s.Barkod == barkod && s.Id != id))
            throw new AlanCakismasiException("barkod", $"'{barkod}' barkodu zaten kullanımda");

        var now = DateTime.UtcNow;
        UrunSeti set;
        if (id is null)
        {
            set = new UrunSeti { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Olusturuldu = now };
            db.UrunSetleri.Add(set);
        }
        else
        {
            set = await db.UrunSetleri.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Set bulunamadı");
            db.UrunSetiKalemleri.RemoveRange(await db.UrunSetiKalemleri.Where(k => k.SetId == id && k.TenantId == islem.TenantId)
                .Take(UrunSeti.AzamiKalem + 1).ToListAsync());   // set en çok 20 parça (Yaz kapısı)
        }
        set.Ad = ad; set.Barkod = barkod; set.Aktif = dto.Aktif; set.Guncellendi = now;
        var sira = 0;
        foreach (var k in kalemler)
            db.UrunSetiKalemleri.Add(new UrunSetiKalemi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, SetId = set.Id, UrunId = k.UrunId,
                Adet = Math.Round(k.Adet, 3, MidpointRounding.AwayFromZero), Sira = sira++,
            });
        denetim.YazmaKaydet(islem, nameof(UrunSeti), set.Id, $"ürün seti {(id is null ? "eklendi" : "güncellendi")} · {kalemler.Count} parça", hassas: false);
        await db.SaveChangesAsync();
        return (await Liste(islem)).Single(x => x.Id == set.Id);
    }
}

[ApiController]
[Authorize]
[Route("api/v1/urun-setleri")]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunSetiController(UrunSetiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/urun-setleri — işletmenin setleri ve parçaları.</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<List<UrunSetiVeri>> Liste() => servis.Liste(Islem);

    /// <summary>GET /api/v1/urun-setleri/barkod/{barkod} — tezgâh okutması (aktif set).</summary>
    [HttpGet("barkod/{barkod}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<UrunSetiVeri> Barkod(string barkod) => servis.BarkodBul(barkod, Islem);

    /// <summary>POST /api/v1/urun-setleri — yeni set (SAHİP/MÜDÜR).</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<UrunSetiVeri> Olustur([FromBody] UrunSetiDto dto) => servis.Olustur(dto, Islem);

    /// <summary>PUT /api/v1/urun-setleri/{id} — seti tamamen yaz (ad, barkod, aktiflik, parçalar).</summary>
    [HttpPut("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<UrunSetiVeri> Guncelle(string id, [FromBody] UrunSetiDto dto) => servis.Guncelle(id, dto, Islem);
}
