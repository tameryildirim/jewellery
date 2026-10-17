using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kuyumcu.Contracts.Sayfalar.KarZarar;

namespace Kuyumcu.Api.Modules.Maliyet;

// ═════════════════════════════════════════════════════════════════════════════
// M9 — ALIŞ İŞÇİLİK BELGESİ (müşteri 16.10: "aradaki masrafı fatura etmen lazım, yoksa kâr görünür").
// Toptancıdan alınan altın üründe ödenen TL − (has gram × alış günü has fiyatı) = toptancının
// işçiliği. O işçiliğin belgesi (fatura / gider pusulası) yoksa incelemede kâr sayılır. Bu modül
// işçiliği olan alışları listeler, belgeyi kaydettirir, belgesizleri SAYAR (uyarı kapanmaz).
// ⚠ Alış satırı DEĞİŞMEZ; belge kaydı alışın yanındaki bilgidir. Tezgâh akışına dokunulmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AlisIscilikBelgeDto
{
    [Required(ErrorMessage = "Belge türü zorunludur")]
    [RegularExpression("FATURA|GIDER_PUSULASI|BELGESIZ", ErrorMessage = "Belge türü FATURA, GIDER_PUSULASI ya da BELGESIZ olmalıdır")]
    public string BelgeTuru { get; set; } = null!;
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.IscilikBelgeNo)] public string? BelgeNo { get; set; }
    public DateOnly? BelgeTarihi { get; set; }
    [Range(0d, 1_000_000_000d)] public decimal? IscilikTl { get; set; }
}

public sealed class AlisIscilikServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public const int VarsayilanGun = 90;

    /// <summary>İşçiliği olan altın alışları (cari'li, stornosuz) + belge durumu + belgesiz özeti.</summary>
    public async Task<AlisIscilikListe> Liste(AktifKullanici islem, int gun = VarsayilanGun)
    {
        if (gun is < 1 or > 400) throw new BadRequestException("Gün 1 ile 400 arasında olmalıdır").AlanIle("gun");
        var (bas, _) = Zaman.YerelGunAraligiUtc(Zaman.YerelBugun().AddDays(-(gun - 1)));

        var alislar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == "ALIS" && h.CariId != null && h.Tarih >= bas)
            .StornoCiftiniDisla(db, islem.TenantId)
            .OrderByDescending(h => h.Tarih).ThenBy(h => h.Id)
            .Take(500)
            .Select(h => new { h.Id, h.Tarih, h.CariId, h.UrunId, h.Adet, h.TlTutari, h.HasGrami, h.Maden, h.KurSnapshotId })
            .ToListAsync();
        alislar = [.. alislar.Where(a => KarZararServisi.AltinMaden(a.Maden))];

        var kurIdler = alislar.Select(a => a.KurSnapshotId).Distinct().ToList();
        var kurlar = await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && kurIdler.Contains(k.Id))
            .Take(kurIdler.Count)
            .ToDictionaryAsync(k => k.Id, k => k.AltinHasFiyati);
        var idler = alislar.Select(a => a.Id).ToList();
        var belgeler = await db.AlisIscilikBelgeleri.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId && idler.Contains(b.AlisHareketId))
            .Take(idler.Count)
            .ToDictionaryAsync(b => b.AlisHareketId);
        var cariIdler = alislar.Select(a => a.CariId!).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id) && !c.SistemCarisi)
            .Take(cariIdler.Count)
            .ToDictionaryAsync(c => c.Id, c => c.Ad);
        var urunIdler = alislar.Where(a => a.UrunId != null).Select(a => a.UrunId!).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            .Take(urunIdler.Count)
            .ToDictionaryAsync(u => u.Id, u => u.Ad);

        var satirlar = alislar
            .Where(a => cariler.ContainsKey(a.CariId!) && kurlar.TryGetValue(a.KurSnapshotId, out var k) && k > 0m)
            .Select(a =>
            {
                var odenen = Math.Abs(a.TlTutari);
                var hasBedeli = Math.Round(a.HasGrami * kurlar[a.KurSnapshotId], 2, MidpointRounding.AwayFromZero);
                var b = belgeler.GetValueOrDefault(a.Id);
                return new AlisIscilikSatiri(
                    a.Id, a.Tarih, a.CariId!, cariler[a.CariId!],
                    a.UrunId is null ? null : urunler.GetValueOrDefault(a.UrunId),
                    a.Adet, odenen, a.HasGrami, hasBedeli, odenen - hasBedeli,
                    b is null ? null : new AlisIscilikBelge(b.BelgeTuru, b.BelgeNo, b.BelgeTarihi, b.IscilikTl));
            })
            .Where(x => x.IscilikTahminiTl >= 0.01m)
            .Take(200)
            .ToList();

        var belgesiz = satirlar.Where(x => x.Belge is null || x.Belge.BelgeTuru == IscilikBelgeTuru.Belgesiz).ToList();
        return new AlisIscilikListe(gun, belgesiz.Count, belgesiz.Sum(x => x.Belge?.IscilikTl ?? x.IscilikTahminiTl), satirlar);
    }

    /// <summary>Alışın işçilik belgesini kaydeder ya da düzeltir.</summary>
    public async Task<AlisIscilikBelge> Kaydet(string alisHareketId, AlisIscilikBelgeDto dto, AktifKullanici islem)
    {
        if (!IscilikBelgeTuru.Tumu.Contains(dto.BelgeTuru))
            throw new BadRequestException("Belge türü FATURA, GIDER_PUSULASI ya da BELGESIZ olmalıdır").AlanIle("belgeTuru");

        var alis = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == alisHareketId && h.TenantId == islem.TenantId && h.Tur == "ALIS")
            ?? throw new NotFoundException("Alış bulunamadı");
        if (await db.Hareketler.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == alis.Id))
            throw new ConflictException("Bu alış stornolanmıştır; işçilik belgesi girilmez.");

        var belgeli = dto.BelgeTuru != IscilikBelgeTuru.Belgesiz;
        var no = string.IsNullOrWhiteSpace(dto.BelgeNo) ? null : dto.BelgeNo.Trim();
        if (belgeli && no is null)
            throw new BadRequestException("Belge numarası zorunludur").AlanIle("belgeNo");
        if (belgeli && dto.BelgeTarihi is null)
            throw new BadRequestException("Belge tarihi zorunludur").AlanIle("belgeTarihi");
        if (dto.BelgeTarihi is { } bt && bt > Zaman.YerelBugun())
            throw new BadRequestException("Belge tarihi bugünden ileri olamaz").AlanIle("belgeTarihi");
        var odenen = Math.Abs(alis.TlTutari);
        var tutar = dto.IscilikTl ?? 0m;
        if (Kuyumcu.Core.Domain.Girdi.SayiKurali.Ondalik(tutar, 2) is not null)
            throw new BadRequestException("İşçilik tutarı en çok 2 ondalık olmalıdır").AlanIle("iscilikTl");
        if (tutar <= 0m) throw new BadRequestException("İşçilik tutarı sıfırdan büyük olmalıdır").AlanIle("iscilikTl");
        if (tutar > odenen) throw new BadRequestException("İşçilik tutarı alışın tutarını aşamaz").AlanIle("iscilikTl");

        var now = DateTime.UtcNow;
        var kayit = await db.AlisIscilikBelgeleri
            .FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.AlisHareketId == alis.Id);
        if (kayit is null)
        {
            kayit = new AlisIscilikBelgesi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, AlisHareketId = alis.Id, CariId = alis.CariId,
                Olusturuldu = now,
            };
            db.AlisIscilikBelgeleri.Add(kayit);
        }
        kayit.BelgeTuru = dto.BelgeTuru;
        kayit.BelgeNo = belgeli ? no : null;
        kayit.BelgeTarihi = belgeli ? dto.BelgeTarihi : null;
        kayit.IscilikTl = tutar;
        kayit.KullaniciId = islem.Id;
        kayit.Guncellendi = now;
        denetim.YazmaKaydet(islem, nameof(AlisIscilikBelgesi), kayit.Id,
            $"alış işçilik belgesi · alış: {alis.Id} · tür: {dto.BelgeTuru} · tutar: {tutar:0.00}", hassas: false);
        await db.SaveChangesAsync();
        return new AlisIscilikBelge(kayit.BelgeTuru, kayit.BelgeNo, kayit.BelgeTarihi, kayit.IscilikTl);
    }
}

[ApiController]
[Authorize]
[Route("api/v1/alis-iscilik")]
[Yetki.ModulYetkisi("rapor")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class AlisIscilikController(AlisIscilikServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/alis-iscilik?gun=90 — işçiliği olan alışlar + belge durumu + belgesiz özeti.</summary>
    [HttpGet]
    public Task<AlisIscilikListe> Liste([FromQuery] int gun = AlisIscilikServisi.VarsayilanGun) => servis.Liste(Islem, gun);

    /// <summary>PUT /api/v1/alis-iscilik/{alisHareketId} — belgeyi kaydet / düzelt.</summary>
    [HttpPut("{alisHareketId}")]
    public Task<AlisIscilikBelge> Kaydet(string alisHareketId, [FromBody] AlisIscilikBelgeDto dto) => servis.Kaydet(alisHareketId, dto, Islem);
}
