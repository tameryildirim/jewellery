using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Fiyat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Fiyat;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P5 · SATIS-4 (c) (Kurul 5): POS ürün arama satırında FİYAT.
// Arama listesi (GET /urunler) altın ürünlerinde satış fiyatı taşımaz — fiyat has
// kuru + liste marjı + işçilik + kampanyadan /fiyat/teklif ile hesaplanır. Satır başına
// ayrı istek (20 öneri = 20 istek) yerine TEK istek: aynı motor (FiyatServisi.TeklifListeyle
// — tek motor ilkesi), aynı liste çözümü (cari → liste, yoksa varsayılan), aynı kur kaydı.
// SALT OKUMA: hiçbir satır yazmaz, marj/formül dönmez (yalnız satış fiyatı).
// Yetki: /fiyat/teklif ile AYNI sınıf kapısı ("fiyat-listesi" modül yetkisi, tüm roller okur).
// Sınır: tek istekte en çok 25 ürün (arama listesi 20 gösterir) — aşımı 400 (sessiz kırpma yok).
// ═════════════════════════════════════════════════════════════════════════════
[ApiController, Authorize]
[Yetki.ModulYetkisi("fiyat-listesi")]
[Route("api/v1/fiyat")]
public sealed class FiyatTopluTeklifController(FiyatServisi servis, KuyumcuDbContext db) : ControllerBase
{
    /// <summary>Tek istekte sorulabilecek azami ürün.</summary>
    public const int EnFazlaUrun = 25;

    /// <summary>GET /api/v1/fiyat/teklifler?urunIds=a,b&amp;kurSnapshotId=&amp;cariId= — ürün başına satış fiyatı.</summary>
    [HttpGet("teklifler")]
    public async Task<TopluTeklifYaniti> Teklifler([FromQuery] string? urunIds, [FromQuery] string? kurSnapshotId,
                                                   [FromQuery] string? cariId)
    {
        var islem = AktifKullanici.FromClaims(User);
        var kimlikler = (urunIds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (kimlikler.Count == 0)
            throw new BadRequestException("En az bir ürün kimliği verilmelidir (urunIds)");
        if (kimlikler.Count > EnFazlaUrun)
            throw new BadRequestException($"Tek istekte en fazla {EnFazlaUrun} ürün sorulabilir");
        if (string.IsNullOrWhiteSpace(kurSnapshotId))
            throw new BadRequestException("Kur kaydı gerekli (kurSnapshotId)");

        var kur = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == kurSnapshotId && s.TenantId == islem.TenantId)
            .Select(s => (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Kur kaydı bulunamadı");
        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && kimlikler.Contains(u.Id))
            .Take(EnFazlaUrun)
            .ToListAsync();
        var liste = await servis.ListeCoz(islem.TenantId, string.IsNullOrWhiteSpace(cariId) ? null : cariId);
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

        var sozluk = urunler.ToDictionary(u => u.Id, StringComparer.Ordinal);
        var satirlar = new List<TopluTeklifSatiri>(kimlikler.Count);
        foreach (var id in kimlikler)
        {
            if (!sozluk.TryGetValue(id, out var urun)) continue;
            if (liste is null) { satirlar.Add(new TopluTeklifSatiri(id, false, null, "LISTE_YOK")); continue; }
            var t = await servis.TeklifListeyle(urun, liste, kur, bugun);
            satirlar.Add(t is null
                ? new TopluTeklifSatiri(id, false, null, AtlamaSebebi.FiyatYok)
                : new TopluTeklifSatiri(id, true, t.SatisFiyati, null));
        }
        return new TopluTeklifYaniti(satirlar);
    }
}
