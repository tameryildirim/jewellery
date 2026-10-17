using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// M7a — MÜŞTERİNİN IBAN LİSTESİ (kurucu 17.10: "müşteri bilgilerine bir IBAN listesi gelsin, birden
// fazla IBAN'la işlem yapabilir"). Tablo paket 32'den beri vardı (cari_ibanlari: şifreli IBAN +
// HMAC hash, bir IBAN tek cariye); eksik olan kartta görmek / eklemek / kaldırmaktı.
// Banka ödemesi bu listedeki IBAN'dan gelirse müşteri kendiliğinden bulunur (GelenOdemeServisi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CariIbanEkleDto
{
    [Required(ErrorMessage = "IBAN zorunludur")]
    [Iban]
    public string Iban { get; set; } = null!;
}

public sealed class CariIbanServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public async Task<List<object>> Liste(string cariId, AktifKullanici islem)
    {
        await CariVarMi(cariId, islem.TenantId);
        var liste = await db.CariIbanlari.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && c.CariId == cariId)
            .OrderBy(c => c.Olusturuldu)
            .Take(50)
            .ToListAsync();
        await denetim.ListeOkumaKaydet(islem, "CariIban", liste.Count, $"cari IBAN listesi · {cariId}");
        return [.. liste.Select(c => (object)new { id = c.Id, iban = c.Iban, kaynak = c.Kaynak, olusturuldu = c.Olusturuldu })];
    }

    public async Task<List<object>> Ekle(string cariId, CariIbanEkleDto dto, AktifKullanici islem)
    {
        var cari = await CariVarMi(cariId, islem.TenantId);
        if (cari.SistemCarisi)
            throw new BadRequestException("Peşin Müşteri kartına IBAN eklenmez.");
        if (Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Denetle(dto.Iban) is { } hata)
            throw new BadRequestException(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(hata, Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("iban");
        var iban = Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Normallestir(dto.Iban);
        var hash = db.Sifreleyici.Hashle(iban);
        var mevcut = await db.CariIbanlari.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == islem.TenantId && c.IbanHash == hash);
        if (mevcut is not null)
        {
            if (mevcut.CariId == cariId)
                throw new ConflictException("Bu IBAN zaten bu müşteride kayıtlı.");
            var sahip = await db.Cariler.AsNoTracking()
                .Where(c => c.Id == mevcut.CariId && c.TenantId == islem.TenantId).Select(c => c.Ad).FirstOrDefaultAsync();
            throw new ConflictException($"Bu IBAN başka bir müşteride kayıtlı: {sahip}. Bir IBAN tek müşteriye bağlanır.");
        }
        var id = Kimlik.Ulid();
        db.CariIbanlari.Add(new CariIban
        {
            Id = id, TenantId = islem.TenantId, CariId = cariId, Iban = iban, IbanHash = hash,
            Kaynak = IbanBagKaynagi.Elle, Olusturuldu = DateTime.UtcNow,
        });
        denetim.YazmaKaydet(islem, "CariIban", id, $"müşteriye IBAN eklendi · cari: {cariId}");
        await db.SaveChangesAsync();
        return await Liste(cariId, islem);
    }

    public async Task<List<object>> Sil(string cariId, string ibanId, AktifKullanici islem)
    {
        await CariVarMi(cariId, islem.TenantId);
        var k = await db.CariIbanlari
            .FirstOrDefaultAsync(c => c.Id == ibanId && c.TenantId == islem.TenantId && c.CariId == cariId)
            ?? throw new NotFoundException("IBAN bulunamadı");
        // Bağ kaldırılır; geçmiş dekontlar etkilenmez (dekont kendi karşı IBAN'ını taşır).
        db.CariIbanlari.Remove(k);
        denetim.YazmaKaydet(islem, "CariIban", ibanId, $"müşteriden IBAN kaldırıldı · cari: {cariId}", eylem: DenetimEylemi.Silme);
        await db.SaveChangesAsync();
        return await Liste(cariId, islem);
    }

    private async Task<Data.Cari> CariVarMi(string cariId, string tenantId) =>
        await db.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == tenantId)
        ?? throw new NotFoundException("Cari bulunamadı");
}

[ApiController]
[Authorize]
[Route("api/v1/cari/{id}/ibanlar")]
[Yetki.ModulYetkisi("cari")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class CariIbanController(CariIbanServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/cari/{id}/ibanlar — müşterinin IBAN listesi.</summary>
    [HttpGet]
    public Task<List<object>> Liste(string id) => servis.Liste(id, Islem);

    /// <summary>POST /api/v1/cari/{id}/ibanlar — IBAN ekle (bir IBAN tek müşteriye).</summary>
    [HttpPost]
    public Task<List<object>> Ekle(string id, [FromBody] CariIbanEkleDto dto) => servis.Ekle(id, dto, Islem);

    /// <summary>DELETE /api/v1/cari/{id}/ibanlar/{ibanId} — IBAN'ı müşteriden kaldır.</summary>
    [HttpDelete("{ibanId}")]
    public Task<List<object>> Sil(string id, string ibanId) => servis.Sil(id, ibanId, Islem);
}
