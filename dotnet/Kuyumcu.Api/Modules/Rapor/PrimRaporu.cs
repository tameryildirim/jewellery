using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Contracts.Sayfalar.Raporlar;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// D1-20 (18.10, rakip eşitleme: "personel prim / komisyon, hedef") — hareketler.personel_id = SATAN (anlam kilidi,
// Entities.cs; R13 "kaydeden" raporu prim dayanağı DEĞİLDİR).
//   • Net satış = satanın SATIS tutarları − bu satışlardan yapılan iadeler (iade, ASIL satışın satanından düşer;
//     iade_kaynak_hareket_id bağı). Storno çiftleri dışarıda.
//   • Hedef: kullanıcının aylık hedefi dönemin her gününe ayın gün sayısıyla orantılanır (ay ortası dönem doğru ölçülür).
//   • Prim = net satış × prim yüzdesi (net ≤ 0 ise 0). Satanı bilinmeyen satış ayrı satırda; prime girmez.
// ═════════════════════════════════════════════════════════════════════════════
public static class PrimRaporu
{
    public static async Task<PrimRaporYaniti> Hesapla(KuyumcuDbContext db, DenetimServisi denetim, AktifKullanici islem,
        DateOnly? baslangic, DateOnly? bitis, string? subeId, decimal? simulasyonOran = null)
    {
        // D1-20 "dene-gör" (20.10, rakip Dezy): "bu oranla geçen ay ne öderdim?" — kayıtlı yüzde yerine verilen oranla hesaplanır;
        // hiçbir şey yazılmaz, yanıt SimulasyonOran taşır (ekran bunu açıkça söyler).
        if (simulasyonOran is < 0m or > 100m) throw new BadRequestException("Prim oranı 0 ile 100 arasında olmalıdır").AlanIle("oran");
        var d = RaporAraligi.Coz(baslangic, bitis);
        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit).Daralt(subeId)
            .StornoCiftiniDisla(db, islem.TenantId);
        var satislar = await temel.Where(h => h.Tur == "SATIS")
            .GroupBy(h => h.PersonelId)
            .Select(g => new { PersonelId = g.Key, Adet = g.Count(), Tl = g.Sum(x => (decimal?)x.TlTutari) ?? 0m })
            .ToListAsync();
        var iadeler = await (from i in temel.Where(h => h.Tur == "SATIS_IADE" && h.IadeKaynakHareketId != null)
                             join s in db.Hareketler.AsNoTracking().Where(x => x.TenantId == islem.TenantId) on i.IadeKaynakHareketId equals s.Id
                             group i by s.PersonelId into g
                             select new { PersonelId = g.Key, Tl = g.Sum(x => (decimal?)x.TlTutari) ?? 0m }).ToListAsync();

        // Y-05: dönemde personele ödenen avanslar (gider satırı, storno çiftleri dışarıda) — prim − avans = net ödenecek.
        var avanslar = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.PersonelId != null && h.GiderKategoriId != null && h.StornoKaynakId == null
                     && h.Tarih >= d.Bas && h.Tarih < d.Bit
                     && !db.KasaHareketleri.Any(s => s.TenantId == islem.TenantId && s.StornoKaynakId == h.Id))
            .GroupBy(h => h.PersonelId!).Select(g => new { PersonelId = g.Key, Tl = g.Sum(x => -x.Tutar) }).ToListAsync();
        var kisiIdler = satislar.Select(s => s.PersonelId).Concat(iadeler.Select(i => i.PersonelId)).Concat(avanslar.Select(a => a.PersonelId))
            .Where(x => x != null).Select(x => x!).Distinct().ToList();
        var kisiler = await db.Kullanicilar.AsNoTracking().Where(u => u.TenantId == islem.TenantId && kisiIdler.Contains(u.Id))
            .Take(kisiIdler.Count + 1)
            .ToDictionaryAsync(u => u.Id, u => new { Ad = (u.Ad + " " + u.Soyad).Trim(), u.PrimYuzde, u.AylikHedefTl });

        decimal Orantili(decimal aylik)
        {
            decimal t = 0m;
            for (var g = d.YerelBas; g <= d.YerelBit; g = g.AddDays(1)) t += aylik / DateTime.DaysInMonth(g.Year, g.Month);
            return Math.Round(t, 2, MidpointRounding.AwayFromZero);
        }

        var satirlar = new List<PrimSatiri>();
        foreach (var id in kisiIdler.Append(null).Distinct())
        {
            var s = satislar.FirstOrDefault(x => x.PersonelId == id);
            var i = iadeler.FirstOrDefault(x => x.PersonelId == id);
            var av = id is null ? null : avanslar.FirstOrDefault(x => x.PersonelId == id);
            if (s is null && i is null && av is null) continue;
            var satisTl = Math.Round(Math.Abs(s?.Tl ?? 0m), 2);
            var iadeTl = Math.Round(Math.Abs(i?.Tl ?? 0m), 2);
            var net = satisTl - iadeTl;
            var k = id is null ? null : kisiler.GetValueOrDefault(id);
            var hedef = k?.AylikHedefTl is { } ah && ah > 0m ? Orantili(ah) : (decimal?)null;
            var yuzde = id is null ? null : simulasyonOran ?? k?.PrimYuzde;
            var prim = yuzde is { } py && net > 0m ? Math.Round(net * py / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
            var avans = Math.Round(Math.Abs(av?.Tl ?? 0m), 2);
            satirlar.Add(new PrimSatiri(id, k?.Ad, s?.Adet ?? 0, satisTl, iadeTl, net, hedef,
                hedef is > 0m ? Math.Round(net / hedef.Value * 100m, 1, MidpointRounding.AwayFromZero) : null, yuzde, prim,
                avans, Math.Round(prim - avans, 2)));
        }
        await denetim.ListeOkumaKaydet(islem, "PrimRaporu", satirlar.Count, simulasyonOran is { } so ? $"prim simülasyonu görüntülendi (%{so})" : "prim raporu görüntülendi");
        return new PrimRaporYaniti(d.YerelBas, d.YerelBit, [.. satirlar.OrderBy(x => x.PersonelId is null).ThenByDescending(x => x.NetTl)],
            satirlar.Sum(x => x.PrimTl), simulasyonOran);
    }
}
