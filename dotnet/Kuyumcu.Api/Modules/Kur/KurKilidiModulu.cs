using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kur;

// ═════════════════════════════════════════════════════════════════════════════
// D1-04 (18.10, rakip eşitleme: "kur/fiyat kilidi") — müşteriye söylenen kur süresi boyunca geçerli kalır.
//   • Kilit yalnız TAZE kurla açılır (bayat kur kilitlenip satışa taşınamaz).
//   • Satış/sipariş teslimi kilit kimliğiyle gelirse tazelik kapısı yerine kilit kapısı çalışır:
//     kilit kaldırılmamış, süresi dolmamış, aynı kur kaydı ve (bağlıysa) aynı müşteri olmalı.
//   • Tezgâhtar en çok 60 dakika kilitler; daha uzunu (kapora/sipariş, 30 güne kadar) sahip/müdür açar.
//   • Kaldırma silmez, zaman yazar (iz kalır).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KurKilidiDto
{
    [Required(ErrorMessage = "Kur seçilmedi"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(36)] public string? CariId { get; set; }
    [Range(5, KurKilidi.AzamiDakika, ErrorMessage = "Kilit süresi 5 dakika ile 30 gün arasında olmalıdır")] public int SureDakika { get; set; }
    [MaxLength(AlanSinirlari.KurKilidiNotu)] public string? Not { get; set; }
}

public static class KurKilidiKapisi
{
    public static string Bulunamadi => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kur kilidi bulunamadı");
    public static string Doldu => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kur kilidinin süresi doldu; güncel kurla satış yapın.");
    public static string Kaldirildi => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kur kilidi kaldırılmış; güncel kurla satış yapın.");
    public static string BaskaKur => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kur kilidi bu kur kaydına ait değil");
    public static string BaskaMusteri => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu kur kilidi başka bir müşteriye ait");

    /// <summary>Kilit geçerliyse sessizce döner; değilse sebebiyle reddeder. Kur kaydı iptalse kilit de geçmez.</summary>
    public static async Task Dogrula(KuyumcuDbContext db, string kilitId, string kurSnapshotId, string? cariId, string tenantId, DateTime simdi)
    {
        var k = await db.KurKilitleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == kilitId && x.TenantId == tenantId)
            ?? throw new NotFoundException(Bulunamadi);
        if (k.Kaldirildi is not null) throw new ConflictException(Kaldirildi);
        if (k.Bitis <= simdi) throw new ConflictException(Doldu);
        if (k.KurSnapshotId != kurSnapshotId) throw new BadRequestException(BaskaKur).AlanIle("kurKilidiId");
        if (k.CariId is not null && k.CariId != cariId) throw new BadRequestException(BaskaMusteri).AlanIle("kurKilidiId");
        if (await db.KurSnapshots.AsNoTracking().AnyAsync(s => s.Id == kurSnapshotId && s.TenantId == tenantId && s.IptalEdildi))
            throw new BadRequestException("Bu kur kaydı iptal edilmiş; güncel kurla işlem yapın.").AlanIle("kurSnapshotId");
    }
}

public sealed class KurKilidiServisi(KuyumcuDbContext db, DenetimServisi denetim, KurKaynakDefteri? defter = null)
{
    private const int ListeTavani = 50;

    public async Task<List<KurKilidiOzet>> Etkinler(string? cariId, AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;
        var q = db.KurKilitleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Kaldirildi == null && k.Bitis > simdi);
        if (!string.IsNullOrEmpty(cariId)) q = q.Where(k => k.CariId == cariId);
        var l = await q.OrderBy(k => k.Bitis).ThenBy(k => k.Id).Take(ListeTavani).ToListAsync();
        return await Ozetle(l, islem.TenantId);
    }

    public async Task<KurKilidiOzet> Ac(KurKilidiDto dto, AktifKullanici islem)
    {
        if (dto.SureDakika is < 5 or > KurKilidi.AzamiDakika)
            throw new BadRequestException("Kilit süresi 5 dakika ile 30 gün arasında olmalıdır").AlanIle("sureDakika");
        if (dto.SureDakika > KurKilidi.TezgahtarAzamiDakika && islem.Rol is not ("SAHIP" or "MUDUR"))
            throw new ForbiddenException(Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("60 dakikadan uzun kilidi sahip ya da müdür açar"));
        if (dto.Not is { Length: > AlanSinirlari.KurKilidiNotu })
            throw new BadRequestException("Not en çok 200 karakter olabilir").AlanIle("not");
        var simdi = DateTime.UtcNow;
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.KurTazeDogrula(db, dto.KurSnapshotId, islem.TenantId, defter, simdi);   // bayat kur kilitlenmez
        var cariId = string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId;
        if (cariId is not null)
        {
            var sistem = await db.Cariler.AsNoTracking().Where(c => c.Id == cariId && c.TenantId == islem.TenantId)
                .Select(c => (bool?)c.SistemCarisi).FirstOrDefaultAsync()
                ?? throw new BadRequestException("Müşteri bulunamadı").AlanIle("cariId");
            if (sistem) cariId = null;   // Peşin Müşteri = müşterisiz kilit
        }
        var k = new KurKilidi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, KurSnapshotId = dto.KurSnapshotId, CariId = cariId,
            Not = string.IsNullOrWhiteSpace(dto.Not) ? null : dto.Not.Trim(),
            Bitis = simdi.AddMinutes(dto.SureDakika), OlusturanId = islem.Id, Olusturuldu = simdi,
        };
        db.KurKilitleri.Add(k);
        denetim.YazmaKaydet(islem, nameof(KurKilidi), k.Id, $"kur kilitlendi · {dto.SureDakika} dk", hassas: false);
        await db.SaveChangesAsync();
        return (await Ozetle([k], islem.TenantId))[0];
    }

    public async Task Kaldir(string id, AktifKullanici islem)
    {
        var k = await db.KurKilitleri.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(KurKilidiKapisi.Bulunamadi);
        if (k.Kaldirildi is not null) return;
        k.Kaldirildi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, nameof(KurKilidi), id, "kur kilidi kaldırıldı", hassas: false);
        await db.SaveChangesAsync();
    }

    private async Task<List<KurKilidiOzet>> Ozetle(List<KurKilidi> l, string tenantId)
    {
        var kurIdler = l.Select(k => k.KurSnapshotId).Distinct().ToList();
        var kurlar = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && kurIdler.Contains(s.Id))
            .Take(kurIdler.Count + 1)
            .ToDictionaryAsync(s => s.Id, s => new { s.AltinHasFiyati, s.UsdTry, s.EurTry });
        var cariIdler = l.Where(k => k.CariId != null).Select(k => k.CariId!).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == tenantId && cariIdler.Contains(c.Id))
            .Take(cariIdler.Count + 1)
            .ToDictionaryAsync(c => c.Id, c => c.Ad);
        var kisiIdler = l.Select(k => k.OlusturanId).Distinct().ToList();
        var kisiler = await db.Kullanicilar.AsNoTracking()
            .Where(u => u.TenantId == tenantId && kisiIdler.Contains(u.Id))
            .Take(kisiIdler.Count + 1)
            .ToDictionaryAsync(u => u.Id, u => u.Ad);
        return [.. l.Select(k =>
        {
            var kur = kurlar.GetValueOrDefault(k.KurSnapshotId);
            return new KurKilidiOzet(k.Id, k.KurSnapshotId, k.CariId, k.CariId is null ? null : cariler.GetValueOrDefault(k.CariId),
                kur?.AltinHasFiyati ?? 0m, kur?.UsdTry, kur?.EurTry, k.Not, k.Bitis, kisiler.GetValueOrDefault(k.OlusturanId), k.Olusturuldu);
        })];
    }
}

[ApiController]
[Authorize]
[Route("api/v1/kur-kilitleri")]
[Yetki.ModulYetkisi("islem")]
[RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
public sealed class KurKilidiController(KurKilidiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kur-kilitleri?cariId= — süresi dolmamış kilitler (önce bitecek olan).</summary>
    [HttpGet]
    public Task<List<KurKilidiOzet>> Etkinler([FromQuery] string? cariId) => servis.Etkinler(cariId, Islem);

    /// <summary>POST /api/v1/kur-kilitleri — güncel kuru kilitle.</summary>
    [HttpPost]
    public Task<KurKilidiOzet> Ac([FromBody] KurKilidiDto dto) => servis.Ac(dto, Islem);

    /// <summary>POST /api/v1/kur-kilitleri/{id}/kaldir — kilidi bitir (kayıt kalır).</summary>
    [HttpPost("{id}/kaldir")]
    public async Task<IActionResult> Kaldir(string id) { await servis.Kaldir(id, Islem); return NoContent(); }
}
