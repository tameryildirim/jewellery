using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Contracts.Sayfalar.Hurda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// D1-15 (18.10, rakip eşitleme: "hurda analizi raporu"). Son N günün hurda defterinden (HurdaStokKapisi
// türleri; storno çiftleri dışarıda) maden başına: alınan has/TL, gönderilen has/TL, ortalama has fiyatları,
// has başına fark, tahlil farkı. Yeni formül YOK: tutar ve has defter satırının kendisidir (işaretsiz toplanır).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class HurdaAnalizServisi(KuyumcuDbContext db)
{
    public async Task<HurdaAnalizi> Analiz(AktifKullanici islem, int gun)
    {
        if (gun is < 1 or > 400) throw new BadRequestException("Gün 1 ile 400 arasında olmalıdır").AlanIle("gun");
        var (bas, _) = Zaman.YerelGunAraligiUtc(Zaman.YerelBugun().AddDays(-(gun - 1)));
        var satirlar = await HurdaStokKapisi.HurdaSatirlari(db, islem.TenantId)
            .Where(h => h.Tarih >= bas)
            .StornoCiftiniDisla(db, islem.TenantId)
            .Select(h => new { h.Tur, h.Maden, h.HasGrami, h.TlTutari })
            .Take(20_000)
            .ToListAsync();

        static decimal? Ort(decimal tl, decimal has) => has > 0m ? Math.Round(tl / has, 2, MidpointRounding.AwayFromZero) : null;
        var madenler = satirlar.GroupBy(s => s.Maden).OrderBy(g => g.Key == Kuyumcu.Core.Domain.Maden.Altin ? 0 : 1).ThenBy(g => g.Key)
            .Select(g =>
            {
                var alis = g.Where(x => x.Tur == "HURDA_ALIS").ToList();
                var satis = g.Where(x => x.Tur == "HURDA_SATIS").ToList();
                var alisHas = alis.Sum(x => Math.Abs(x.HasGrami)); var alisTl = alis.Sum(x => Math.Abs(x.TlTutari));
                var satisHas = satis.Sum(x => Math.Abs(x.HasGrami)); var satisTl = satis.Sum(x => Math.Abs(x.TlTutari));
                var alisOrt = Ort(alisTl, alisHas); var satisOrt = Ort(satisTl, satisHas);
                return new HurdaAnalizMaden(g.Key, alis.Count, alisHas, alisTl, alisOrt, satis.Count, satisHas, satisTl, satisOrt,
                    alisOrt is { } a && satisOrt is { } s ? s - a : null,
                    g.Where(x => x.Tur == TahlilFarkiFabrikasi.Tur).Sum(x => x.HasGrami));
            }).ToList();
        return new HurdaAnalizi(gun, madenler);
    }
}

[ApiController]
[Authorize]
[Route("api/v1/hurda/analiz")]
[Yetki.ModulYetkisi("islem")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class HurdaAnalizController(HurdaAnalizServisi servis) : ControllerBase
{
    /// <summary>GET /api/v1/hurda/analiz?gun=90 — son N günün hurda alış / gönderim özeti (maden başına).</summary>
    [HttpGet]
    public Task<HurdaAnalizi> Analiz([FromQuery] int gun = 90) => servis.Analiz(AktifKullanici.FromClaims(User), gun);
}
