using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// FİNANS RAPORLARI — paket 34 Faz A (R11 kasa defteri).
//
// Faz B'ye kalanlar (hurda özet R17, KDV özet R18) BU DOSYAYA gelecek; şimdi
// yazılmıyor çünkü kabul ölçütü olan 19'luk küme henüz oylanmadı (Aşkın'ın
// koşulu — bkz. paket 34 § birleşik koşullar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FinansRaporServisi(KuyumcuDbContext db)
{
    /// <summary>
    /// R11 — kasa bazında GÜN GÜN devir / giriş / çıkış / kapanış + gün kapanışı farkı.
    ///
    /// <para>
    /// DEVİR NASIL BULUNUR: kasa açılış bakiyesi <c>kasa_hareketleri</c>'nde bir
    /// satır DEĞİLDİR (kasa kurulurken doğrudan <c>kasalar.bakiye</c>'ye yazılır).
    /// Bu yüzden devir hareketleri toplayarak bulunamaz; BUGÜNKÜ bakiyeden
    /// geriye doğru çözülür: <c>devir(bas) = bakiye − Σ(tarih ≥ bas)</c>.
    /// Böylece kapanış rakamı kasanın gerçek bakiyesiyle tutar.
    /// </para>
    /// <para>
    /// KAPANMAMIŞ GÜN "AÇIK"tır — sıfır fark YAZILMAZ (Anayasa md.1: sayılmamış
    /// kasanın farkı bilinmiyordur, sıfır değil).
    /// </para>
    /// </summary>
    public async Task<object> KasaDefteri(
        AktifKullanici islem, DateOnly? baslangic, DateOnly? bitis, string? kasaId)
    {
        var d = RaporAraligi.Coz(baslangic, bitis);

        var kasalar = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && (kasaId == null || k.Id == kasaId))
            .OrderBy(k => k.Ad)
            .ToListAsync();

        if (!string.IsNullOrEmpty(kasaId) && kasalar.Count == 0)
            throw new NotFoundException("Kasa bulunamadı");

        var kasaIdleri = kasalar.Select(k => k.Id).ToList();

        // ── K5: yerel gün kovası SUNUCUDA hesaplanır ────────────────────────
        // Saat dilimi farkı Zaman.Isletme'den okunur; sabit yazılmaz.
        var ofset = (int)Zaman.Isletme.GetUtcOffset(d.Bas).TotalHours;
        var yazSaatiRiski = Zaman.Isletme.SupportsDaylightSavingTime
            && Zaman.Isletme.GetUtcOffset(d.Bit) != Zaman.Isletme.GetUtcOffset(d.Bas);

        // TB-19 / MÜHÜR 5: satırlar çekilmez, SUNUCUDA gruplanır.
        var gunluk = await db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && kasaIdleri.Contains(k.KasaId)
                     && k.Tarih >= d.Bas && k.Tarih < d.Bit)
            .GroupBy(k => new
            {
                k.KasaId,
                Yil = k.Tarih.AddHours(ofset).Year,
                Ay = k.Tarih.AddHours(ofset).Month,
                Gun = k.Tarih.AddHours(ofset).Day,
            })
            .Select(g => new
            {
                g.Key.KasaId, g.Key.Yil, g.Key.Ay, g.Key.Gun,
                giris = g.Where(x => x.Tutar > 0m).Sum(x => x.Tutar),
                cikis = g.Where(x => x.Tutar < 0m).Sum(x => x.Tutar),
                adet = g.Count(),
            })
            .ToListAsync();

        var harita = gunluk.ToDictionary(
            x => (x.KasaId, new DateOnly(x.Yil, x.Ay, x.Gun)),
            x => (x.giris, x.cikis, x.adet));

        // Dönem başından SONRAKİ tüm hareketlerin toplamı (devir çözümü için).
        var sonrakiToplam = await db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && kasaIdleri.Contains(k.KasaId) && k.Tarih >= d.Bas)
            .GroupBy(k => k.KasaId)
            .Select(g => new { KasaId = g.Key, Toplam = g.Sum(x => x.Tutar) })
            .ToDictionaryAsync(x => x.KasaId, x => x.Toplam);

        var kapanislar = await db.GunKapanislari.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId && kasaIdleri.Contains(g.KasaId)
                     && g.YerelGun >= d.YerelBas && g.YerelGun <= d.YerelBit)
            .ToListAsync();
        var kapanisHarita = kapanislar.ToDictionary(g => (g.KasaId, g.YerelGun));

        var sonuc = new List<object>();
        foreach (var kasa in kasalar)
        {
            var devir = Yuvarla2(kasa.Bakiye - sonrakiToplam.GetValueOrDefault(kasa.Id, 0m));
            var acilisDeviri = devir;
            var gunler = new List<object>();

            for (var g = d.YerelBas; g <= d.YerelBit; g = g.AddDays(1))
            {
                var v = harita.GetValueOrDefault((kasa.Id, g), (0m, 0m, 0));
                var kapanis = Yuvarla2(devir + v.Item1 + v.Item2);
                var kayit = kapanisHarita.GetValueOrDefault((kasa.Id, g));

                gunler.Add(new
                {
                    tarih = g.ToString("yyyy-MM-dd"),
                    devir,
                    giris = Yuvarla2(v.Item1),
                    cikis = Yuvarla2(-v.Item2),
                    hareketSayisi = v.Item3,
                    kapanis,
                    // Kapanmamış gün: durum AÇIK, fark NULL (sıfır DEĞİL).
                    durum = kayit is null ? "ACIK" : kayit.Kilitli ? "KAPALI" : "YENIDEN_ACILDI",
                    sayilanNakit = kayit?.SayilanNakit,
                    kapanisDefterBakiye = kayit?.DefterBakiye,
                    fark = kayit?.Fark,
                });
                devir = kapanis;
            }

            sonuc.Add(new
            {
                kasaId = kasa.Id,
                kasaAd = kasa.Ad,
                // UX-S32: tutar para birimiyle BİRLİKTE taşınır.
                paraBirimi = kasa.ParaBirimi,
                acilisDeviri,
                kapanisBakiye = devir,
                guncelBakiye = kasa.Bakiye,
                gunler,
            });
        }

        return new
        {
            baslangic = d.YerelBas.ToString("yyyy-MM-dd"),
            bitis = d.YerelBit.ToString("yyyy-MM-dd"),
            saatDilimi = Zaman.Isletme.Id,
            utcOfsetSaat = ofset,
            // Dönem içinde saat farkı değişiyorsa gün kovaları kayabilir — SAKLANMAZ.
            yazSaatiRiski,
            toplamSatir = sonuc.Count,
            kasalar = sonuc,
        };
    }

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}

// ⚠ Kapı nitelikleri AÇIKÇA yazılır (K34-3); miras alınmaz.
[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/raporlar")]
[Yetki.ModulYetkisi("rapor")]
public sealed class FinansRaporController(FinansRaporServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/kasa-defteri — R11 kasa bazlı gün gün defter</summary>
    [HttpGet("kasa-defteri")]
    public Task<object> KasaDefteri(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis, [FromQuery] string? kasaId)
        => servis.KasaDefteri(Islem, baslangic, bitis, kasaId);
}
