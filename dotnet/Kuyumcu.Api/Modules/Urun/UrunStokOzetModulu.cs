using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Urunler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ISTEK-1 — TOPLU STOK ADEDİ (ürün listesinin "stok" sütunu için TEK istek)
//
// ÖLÇÜM (başsız Chrome, tek kullanıcı, 10 ekran 30 sn): /urunler ekranı
// görünen HER satır için ayrı GET /urunler/{id}/stok atıyordu (N+1) — 12
// ürünlük demo kiracısında 54 isteğin 12'si buydu; 200 ürünlük gerçek bir
// dükkânda tek ekran açılışı 200 istek eder ve genel sınırı tek başına yer.
// Bu uç aynı bilgiyi (şubeler toplamı adet + has gram) TEK istekte verir.
//
// ⚠ AYRI DOSYA: UrunModulu.cs'e dokunulmadı (paralel şerit güvenliği);
// yalnız OKUMA ucudur, yeni varlık/kayıt türü doğurmaz (SÖZLEŞME-DoD'un
// PATCH/DELETE maddesi uygulanmaz). Tek tek uçla (UrunServisi.StokListele)
// AYNI hesap: kiracının stok satırları ürün başına toplanır, tüm şubeler.
// Kiracıya ait olmayan ya da olmayan kimlik yanıtta YER ALMAZ (varlık
// sızdırılmaz); ürünü olan ama stok satırı olmayan kimlik 0 döner.
// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Authorize]
[Route("api/v1/urunler")]
[Yetki.ModulYetkisi("urun")]
public sealed class UrunStokOzetController(KuyumcuDbContext db) : ControllerBase
{
    /// <summary>Tek istekte sorulabilecek azami ürün (liste ekranının sınırıyla aynı).</summary>
    public const int EnFazlaKimlik = 500;

    /// <summary>GET /api/v1/urunler/stok-adetleri?ids=a,b,c — ürün başına toplam stok (tüm şubeler)</summary>
    [HttpGet("stok-adetleri")]
    public async Task<StokAdetleriYaniti> StokAdetleri([FromQuery] string? ids, [FromQuery] string? subeId)
    {
        var islem = AktifKullanici.FromClaims(User);
        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi (null = tüm şubeler toplamı).
        var sube = await GorunumSubesi.Coz(db, islem, subeId);
        var kimlikler = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (kimlikler.Count == 0)
            throw new BadRequestException("En az bir ürün kimliği verilmelidir (ids)");
        if (kimlikler.Count > EnFazlaKimlik)
            throw new BadRequestException($"Tek istekte en fazla {EnFazlaKimlik} ürün sorulabilir");

        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && kimlikler.Contains(u.Id))
            .Select(u => u.Id)
            .Take(EnFazlaKimlik)   // kimlik sayısı girişte en fazla 500 (B-8 sayacı)
            .ToListAsync();

        var toplamlar = await db.Stoklar.AsNoTracking().Daralt(sube)
            .Where(s => s.TenantId == islem.TenantId && kimlikler.Contains(s.UrunId))
            .GroupBy(s => s.UrunId)
            .Select(g => new { UrunId = g.Key, Adet = g.Sum(x => x.Adet), Has = g.Sum(x => x.ToplamHasGram) })
            .ToListAsync();
        var sozluk = toplamlar.ToDictionary(x => x.UrunId, x => x);

        return new StokAdetleriYaniti(urunler
            .Select(id => sozluk.TryGetValue(id, out var t)
                ? new StokAdetSatiri(id, t.Adet, t.Has)
                : new StokAdetSatiri(id, 0m, 0m))
            .ToList());
    }
}
