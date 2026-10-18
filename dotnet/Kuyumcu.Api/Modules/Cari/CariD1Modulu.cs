using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// D1-07 (18.10, rakip eşitleme: "veresiyenin altınla kapanması, fazlasının emanete geçmesi · kara liste").
//   • ALTINLA ÖDE: müşteri ayar + gram getirir. Has karşılığı önce has borcunu kapatır (TAHSILAT — mevcut
//     tahsilat çekirdeği), fazlası AYNI AYARLA emanete girer (EMANET_GIRIS — mevcut emanet çekirdeği). İkisi tek
//     işlemde; borç yoksa tamamı emanete. Yalnız altın (has borcu altın has'ıdır).
//   • KARA LİSTE: sahip/müdür veresiye engeli koyar (sebep zorunlu) ya da kaldırır; veresiye yolları
//     VadeKapisi.KrediLimitiDogrula'da (tek kapı) ve altın veresiyesinde reddedilir. Denetim izine düşer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AltinTahsilatDto
{
    [Required, MaxLength(20)] public string AyarId { get; set; } = null!;
    [Range(0.001, 999_999.999, ErrorMessage = "Gram sıfırdan büyük olmalıdır")] public decimal GramAgirlik { get; set; }
    [Required, MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(500)] public string? Aciklama { get; set; }
}

public sealed class VeresiyeEngeliDto
{
    public bool Engelli { get; set; }
    [MaxLength(AlanSinirlari.VeresiyeEngelSebebi)] public string? Sebep { get; set; }
}

public sealed class CariD1Servisi(KuyumcuDbContext db, CariServisi cari, Emanet.EmanetServisi emanet, DenetimServisi denetim)
{
    public async Task<object> AltinTahsilat(string id, AltinTahsilatDto dto, AktifKullanici islem)
    {
        var c = await db.Cariler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri'ye altınla tahsilat yazılmaz — müşteri seçin").AlanIle("cariId");
        var ayar = Ayarlar.Bul(dto.AyarId) ?? throw new BadRequestException("Geçersiz ayar").AlanIle("ayarId");
        if (ayar.Maden != Maden.Altin || !ayar.Kiymetli) throw new BadRequestException("Altınla ödeme yalnız altın ayarlarıyla yapılır").AlanIle("ayarId");
        var gram = Math.Round(dto.GramAgirlik, 3, MidpointRounding.AwayFromZero);
        if (gram <= 0m) throw new BadRequestException("Gram sıfırdan büyük olmalıdır").AlanIle("gramAgirlik");
        var has = Emanet.EmanetServisi.HasGrami(gram, ayar);

        var kayitli = await db.CariBakiyeler.AsNoTracking().Where(b => b.CariId == id && b.TenantId == islem.TenantId)
            .Select(b => (decimal?)b.HasGramBakiye).FirstOrDefaultAsync() ?? 0m;
        var borc = Math.Max(0m, CariBakiyeKurali.EmanetsizHas(kayitli, await CariBakiyeKurali.EmanetEtkisi(db, islem.TenantId, id)));

        // Fazla, getirilen altının AYNI ayarında gram olarak emanete girer; kapanan has = toplam − emanetin has'ı (kuruşsuz kayıp yok).
        decimal emanetGram = 0m, emanetHas = 0m;
        if (has > borc)
        {
            emanetGram = Math.Round(gram * (has - borc) / has, 3, MidpointRounding.AwayFromZero);
            emanetHas = emanetGram > 0m ? Emanet.EmanetServisi.HasGrami(emanetGram, ayar) : 0m;
        }
        var kapanan = has - emanetHas;

        await using var tx = await db.Database.IsBaslatVeyaDevral();
        if (kapanan > 0m)
            await cari.TahsilatGirHareket(id, new TahsilatGirisDto
            {
                HasGrami = kapanan, OdemeTuru = "NAKIT", KurSnapshotId = dto.KurSnapshotId,
                Aciklama = dto.Aciklama ?? $"Altınla ödeme · {ayar.Ad} {gram:0.###} g",
            }, islem);
        if (emanetGram > 0m)
            await emanet.Giris(new Emanet.EmanetGirisDto
            {
                CariId = id, AyarId = dto.AyarId, GramAgirlik = emanetGram, KurSnapshotId = dto.KurSnapshotId,
                Aciklama = dto.Aciklama ?? "Altınla ödemenin borçtan artanı",
            }, islem);
        if (tx is not null) await tx.CommitAsync();
        return new { hasGrami = has, kapananHas = kapanan, emanetGram, emanetHas, ayarId = dto.AyarId };
    }

    public async Task<object> VeresiyeEngeli(string id, VeresiyeEngeliDto dto, AktifKullanici islem)
    {
        var c = await db.Cariler.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri kara listeye alınamaz").AlanIle("cariId");
        var sebep = string.IsNullOrWhiteSpace(dto.Sebep) ? null : dto.Sebep.Trim();
        if (dto.Engelli && sebep is null) throw new BadRequestException("Kara liste sebebi zorunludur").AlanIle("sebep");
        c.VeresiyeEngeli = dto.Engelli;
        c.VeresiyeEngelSebebi = dto.Engelli ? sebep : null;
        c.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, nameof(Data.Cari), c.Id, dto.Engelli ? "veresiye engeli konuldu" : "veresiye engeli kaldırıldı", hassas: false);
        await db.SaveChangesAsync();
        return new { veresiyeEngeli = c.VeresiyeEngeli, veresiyeEngelSebebi = c.VeresiyeEngelSebebi };
    }
}

[ApiController]
[Authorize]
[Route("api/v1/cari")]
[Yetki.ModulYetkisi("cari")]
public sealed class CariD1Controller(CariD1Servisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/cari/{id}/altin-tahsilat — altınla öde: önce has borcu kapanır, fazlası emanete.</summary>
    [HttpPost("{id}/altin-tahsilat")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> AltinTahsilat(string id, [FromBody] AltinTahsilatDto dto) => servis.AltinTahsilat(id, dto, Islem);

    /// <summary>PUT /api/v1/cari/{id}/veresiye-engeli — kara liste (sahip/müdür).</summary>
    [HttpPut("{id}/veresiye-engeli")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> VeresiyeEngeli(string id, [FromBody] VeresiyeEngeliDto dto) => servis.VeresiyeEngeli(id, dto, Islem);
}
