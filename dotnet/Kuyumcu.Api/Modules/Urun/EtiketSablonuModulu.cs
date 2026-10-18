using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// D1-02 (18.10, rakip eşitleme: "etiket tasarımcısı") — işletmenin etiket şablonu: hazır boy + alan sırası/görünürlüğü.
// Taş kökeni ibaresi şablondan çıkarılamaz (EtiketSablonu.Normalle). Yazma sahip/müdür; denetim izine düşer.

public sealed class EtiketSablonuDto
{
    [Range(20, 120)] public int GenislikMm { get; set; }
    [Range(15, 100)] public int YukseklikMm { get; set; }
    [Required, MaxLength(8)] public List<string> Alanlar { get; set; } = [];
}

[ApiController]
[Authorize]
[Route("api/v1/urunler/etiket-sablonu")]
[Yetki.ModulYetkisi("urun")]
public sealed class EtiketSablonuController(KuyumcuDbContext db, DenetimServisi denetim) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/urunler/etiket-sablonu</summary>
    [HttpGet]
    public async Task<EtiketSablonu> Getir() =>
        EtiketSablonu.Coz(await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == Islem.TenantId && a.Anahtar == KiraciAyarAnahtari.EtiketSablonu)
            .Select(a => a.Deger).FirstOrDefaultAsync());

    /// <summary>PUT /api/v1/urunler/etiket-sablonu — boy hazır boylardan; alanlar bilinen kodlar.</summary>
    [HttpPut]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<EtiketSablonu> Yaz([FromBody] EtiketSablonuDto dto)
    {
        if (!EtiketSablonu.Boylar.Contains((dto.GenislikMm, dto.YukseklikMm)))
            throw new BadRequestException("Etiket boyu hazır boylardan biri olmalıdır").AlanIle("genislikMm");
        if (dto.Alanlar.Any(a => !EtiketSablonu.TumAlanlar.Contains(a)))
            throw new BadRequestException("Bilinmeyen etiket alanı").AlanIle("alanlar");
        var s = EtiketSablonu.Normalle(new EtiketSablonu(dto.GenislikMm, dto.YukseklikMm, dto.Alanlar));
        var islem = Islem;
        var a = await db.KiraciAyarlari.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.Anahtar == KiraciAyarAnahtari.EtiketSablonu);
        if (a is null)
        {
            a = new KiraciAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = KiraciAyarAnahtari.EtiketSablonu, Deger = "" };
            db.KiraciAyarlari.Add(a);
        }
        a.Deger = s.Yaz();
        a.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, nameof(KiraciAyari), a.Id, $"etiket şablonu: {a.Deger}", hassas: false);
        await db.SaveChangesAsync();
        return s;
    }
}
