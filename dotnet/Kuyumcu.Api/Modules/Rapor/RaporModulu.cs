using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// Rapor modülü — kaynak: apps/api/src/modules/rapor/*
// Salt-okur raporlama uçları; DTO gerektirmez (tüm girdiler query paramı).
// Dönüş şekilleri NestJS RaporService ile birebir aynıdır.
// ═════════════════════════════════════════════════════════════════════════════

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class RaporServisi(KuyumcuDbContext db)
{
    // ─── Günlük özet ──────────────────────────────────────────────────────────
    /// <summary>
    /// Belirli bir gün için:
    /// - Satış adedi + toplam TL
    /// - Alış adedi + toplam TL
    /// - Net has gram hareketi (satış+alış)
    /// - Kasa bakiyeleri
    /// </summary>
    public async Task<object> GunlukOzet(string tarih, AktifKullanici islem)
    {
        // Nest: new Date(tarih) — geçersiz tarih Invalid Date olur ve hiçbir
        // hareket eşleşmez; aynı davranışı korumak için parse başarısızsa
        // boş aralık (min..min) kullanılır.
        var gecerli = DateTime.TryParse(
            tarih, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var gun);
        var gunSonu = gecerli ? gun.AddDays(1) : gun;

        // Tüm hareketleri çek — küçük veri setlerinde yeterli
        var tumHareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= gun && h.Tarih < gunSonu)
            .OrderByDescending(h => h.Tarih)
            .ToListAsync();

        var satislar = tumHareketler.Where(h => h.Tur == "SATIS").ToList();
        var alislar  = tumHareketler.Where(h => h.Tur == "ALIS").ToList();
        var hurdalar = tumHareketler.Where(h => h.Tur == "HURDA_ALIS").ToList();

        // Kasa bakiyeleri
        var kasaBakiyeleri = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .ToListAsync();

        return new
        {
            tarih,
            satis = new
            {
                adet      = satislar.Count,
                toplamTl  = satislar.Sum(h => h.TlTutari),
                toplamHas = satislar.Sum(h => Math.Abs(h.HasGrami)),
            },
            alis = new
            {
                adet      = alislar.Count,
                toplamTl  = alislar.Sum(h => Math.Abs(h.TlTutari)),
                toplamHas = alislar.Sum(h => h.HasGrami),
            },
            hurda = new
            {
                toplamHas = hurdalar.Sum(h => h.HasGrami),
                odenenTl  = hurdalar.Sum(h => Math.Abs(h.TlTutari)),
            },
            netHasGramHareketi = tumHareketler.Sum(h => h.HasGrami),
            hareketSayisi      = tumHareketler.Count,
            kasalar = kasaBakiyeleri.Select(k => new
            {
                id         = k.Id,
                ad         = k.Ad,
                paraBirimi = k.ParaBirimi,
                bakiye     = k.Bakiye,
            }).ToList(),
        };
    }

    // ─── Aylık özet ───────────────────────────────────────────────────────────

    public async Task<object> AylikOzet(int yil, int ay, AktifKullanici islem)
    {
        // Nest: new Date(yil, ay-1, 1) — JS ay taşmasını (ay=13 → ertesi yıl Ocak)
        // AddMonths ile aynen koruyoruz.
        var baslangic = new DateTime(yil, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(ay - 1);
        var bitis     = baslangic.AddMonths(1);

        var tumHareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= baslangic && h.Tarih < bitis)
            .ToListAsync();

        var satislar    = tumHareketler.Where(h => h.Tur == "SATIS").ToList();
        var alislar     = tumHareketler.Where(h => h.Tur == "ALIS").ToList();
        var tahsilatlar = tumHareketler.Where(h => h.Tur == "TAHSILAT").ToList();
        var veresiyeler = tumHareketler.Where(h => h.Tur == "VERESIYE").ToList();

        var satisTl = satislar.Sum(h => h.TlTutari);
        var alisTl  = alislar.Sum(h => Math.Abs(h.TlTutari));

        return new
        {
            yil, ay,
            satis    = new { sayisi = satislar.Count, toplamTl = satisTl, toplamHas = satislar.Sum(h => Math.Abs(h.HasGrami)) },
            alis     = new { sayisi = alislar.Count,  toplamTl = alisTl,  toplamHas = alislar.Sum(h => h.HasGrami) },
            tahsilat = new { toplamTl = tahsilatlar.Sum(h => Math.Abs(h.TlTutari)) },
            veresiye = new { toplamTl = veresiyeler.Sum(h => h.TlTutari) },
            karTl    = satisTl - alisTl,
        };
    }

    // ─── Cari yaşlandırma (aging) ─────────────────────────────────────────────
    /// <summary>
    /// Açık bakiyeli cariler:
    /// 0-30 gün, 31-60 gün, 61-90 gün, 90+ gün dilimlerine göre gruplandırır.
    /// </summary>
    public async Task<object> CariYaslandirma(AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;

        var bakiyeler = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId)
            .ToListAsync();

        // Sadece bakiyesi olan cariler
        var aktifBakiyeler = bakiyeler
            .Where(b => Math.Abs(b.TlBakiye) > 0.01m || Math.Abs(b.HasGramBakiye) > 0.000001m)
            .ToList();

        // Cari adları
        var cariMap = aktifBakiyeler.Count > 0
            ? await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == islem.TenantId)
                .ToDictionaryAsync(c => c.Id)
            : new Dictionary<string, Data.Cari>();

        var yaslandirma = aktifBakiyeler.Select(b =>
        {
            var gun = b.SonIslemTarihi is { } sonIslem
                ? (int?)(int)Math.Floor((simdi - sonIslem).TotalDays)
                : null;

            var cari = cariMap.GetValueOrDefault(b.CariId);

            return new
            {
                cariId        = b.CariId,
                cariAd        = cari?.Ad ?? "Bilinmiyor",
                cariTur       = cari?.Tur ?? "MUSTERI",
                tlBakiye      = b.TlBakiye,
                hasGramBakiye = b.HasGramBakiye,
                gunSayisi     = gun,
                dilim         = gun is not int g ? "BELIRSIZ"
                              : g <= 30          ? "0-30"
                              : g <= 60          ? "31-60"
                              : g <= 90          ? "61-90"
                                                 : "90+",
            };
        }).ToList();

        // Özet — sözlük anahtarları JSON'a aynen geçer ("0-30", ..., "BELIRSIZ")
        var ozetTl = new Dictionary<string, decimal>
        {
            ["0-30"] = 0m, ["31-60"] = 0m, ["61-90"] = 0m, ["90+"] = 0m, ["BELIRSIZ"] = 0m,
        };
        foreach (var y in yaslandirma) ozetTl[y.dilim] += y.tlBakiye;

        return new { yaslananCariler = yaslandirma, ozetTl };
    }

    // ─── Stok değer raporu ────────────────────────────────────────────────────
    /// <summary>
    /// Tüm ürünler için güncel stok adeti + toplam has gram.
    /// </summary>
    public async Task<object> StokDegeri(AktifKullanici islem, string? subeId)
    {
        var q = db.Stoklar.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        if (!string.IsNullOrEmpty(subeId)) q = q.Where(s => s.SubeId == subeId);
        var stokListesi = await q.ToListAsync();

        if (stokListesi.Count == 0) return new { urunler = Array.Empty<object>(), toplamHas = 0m };

        var urunMap = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId)
            .ToDictionaryAsync(u => u.Id);

        // Ürün bazlı topla — grup sırası ilk görülme sırasını korur (Nest'teki Map gibi)
        var satirlar = stokListesi
            .GroupBy(s => s.UrunId)
            .Select(g =>
            {
                var urun = urunMap.GetValueOrDefault(g.Key);
                return new
                {
                    urunId    = g.Key,
                    kod       = urun?.Kod ?? "",
                    ad        = urun?.Ad ?? "",
                    ayarId    = urun?.AyarId ?? "",
                    adet      = g.Sum(s => s.Adet),
                    toplamHas = g.Sum(s => s.ToplamHasGram),
                };
            })
            .ToList();

        return new { urunler = satirlar, toplamHas = satirlar.Sum(s => s.toplamHas) };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/raporlar")]
[Yetki.ModulYetkisi("rapor")]
public sealed class RaporController(RaporServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/gunluk?tarih=2026-07-02 — Günlük satış/alış/kasa özeti</summary>
    [HttpGet("gunluk")]
    public Task<object> GunlukOzet([FromQuery] string? tarih)
        => servis.GunlukOzet(
            tarih ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Islem);

    /// <summary>GET /api/v1/raporlar/aylik?yil=2026&amp;ay=7 — Aylık toplamlar ve kâr özeti</summary>
    [HttpGet("aylik")]
    public Task<object> AylikOzet([FromQuery] int? yil, [FromQuery] int? ay)
    {
        var simdi = DateTime.UtcNow;
        return servis.AylikOzet(yil ?? simdi.Year, ay ?? simdi.Month, Islem);
    }

    /// <summary>GET /api/v1/raporlar/cari-yaslandirma — Açık cari bakiyeleri (0-30, 31-60, 61-90, 90+ gün)</summary>
    [HttpGet("cari-yaslandirma")]
    public Task<object> CariYaslandirma() => servis.CariYaslandirma(Islem);

    /// <summary>GET /api/v1/raporlar/stok-degeri?subeId=... — Stok envanteri ve toplam has gram</summary>
    [HttpGet("stok-degeri")]
    public Task<object> StokDegeri([FromQuery] string? subeId) => servis.StokDegeri(Islem, subeId);
}
