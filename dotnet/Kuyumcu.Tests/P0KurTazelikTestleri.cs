using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kur;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 17.10 P0-3 (eksik/fazla incelemesi) — kur devre kesici + satış/alış kur tazeliği kapısı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P0KurTazelikTestleri(TestVeritabani vt)
{
    private static KurServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<KurServisi>.Instance, null!, null!,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

    private static string KurEkle(KuyumcuDbContext db, Kiraci k, string kaynak, DateTime olusturuldu, bool iptal = false)
    {
        var id = Kimlik.Ulid();
        db.KurSnapshots.Add(new KurSnapshot
        {
            Id = id, TenantId = k.TenantId, AltinHasFiyati = 3_000m, Kaynak = kaynak,
            Olusturuldu = olusturuldu, IptalEdildi = iptal,
        });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task Devre_kesici_bant_disi_sicramayi_YAZMAZ_ve_tek_bildirim_birakir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P0-kur-sicrama");
        using var db = vt.Olustur(k);
        var son = await db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == k.TenantId)
            .OrderByDescending(s => s.Olusturuldu).FirstAsync();
        var once = await db.KurSnapshots.CountAsync(s => s.TenantId == k.TenantId);

        var (cevap, yazildi) = await Servis(db).KaydetVeyaMevcut(k.TenantId,
            new DisKurVerisi(son.AltinHasFiyati * 3m, null, null, null, null, "ALTINKAYNAK"));
        Assert.False(yazildi);
        Assert.Equal(son.AltinHasFiyati, cevap.AltinHasFiyati);
        // aynı gün ikinci sıçrama → ikinci bildirim YOK
        await Servis(db).KaydetVeyaMevcut(k.TenantId,
            new DisKurVerisi(son.AltinHasFiyati * 3m, null, null, null, null, "ALTINKAYNAK"));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(once, await kontrol.KurSnapshots.CountAsync(s => s.TenantId == k.TenantId));
        Assert.Equal(1, await kontrol.Bildirimler.CountAsync(b => b.TenantId == k.TenantId && b.Tur == "KUR_SICRAMA"));
    }

    [Fact]
    public async Task Devre_kesici_bant_icindeki_degisimi_yazar_kullanici_isteyince_sicramada_409()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P0-kur-normal");
        using var db = vt.Olustur(k);
        var son = await db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == k.TenantId)
            .OrderByDescending(s => s.Olusturuldu).FirstAsync();

        var (_, yazildi) = await Servis(db).KaydetVeyaMevcut(k.TenantId,
            new DisKurVerisi(Math.Round(son.AltinHasFiyati * 1.01m, 2), null, null, null, null, "ALTINKAYNAK"));
        Assert.True(yazildi);

        await Assert.ThrowsAsync<ConflictException>(() => Servis(db).KaydetVeyaMevcut(k.TenantId,
            new DisKurVerisi(son.AltinHasFiyati * 3m, null, null, null, null, "ALTINKAYNAK"), kullaniciIstedi: true));
    }

    [Fact]
    public async Task Tazelik_kapisi_dunku_elle_kur_iptal_kur_ve_bayat_dis_kaynak_409_bugunku_elle_kur_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P0-kur-taze");
        using var db = vt.Olustur(k);
        var simdi = DateTime.UtcNow;

        var dun = KurEkle(db, k, KurKaynakDefteri.ManuelKod, simdi.AddDays(-1).AddHours(-1));
        var iptal = KurEkle(db, k, KurKaynakDefteri.ManuelKod, simdi, iptal: true);
        var bayat = KurEkle(db, k, "ALTINKAYNAK", simdi.AddMinutes(-10));
        var taze = KurEkle(db, k, "ALTINKAYNAK", simdi.AddSeconds(-30));
        var bugun = KurEkle(db, k, KurKaynakDefteri.ManuelKod, simdi);

        foreach (var id in new[] { dun, bayat })
            await Assert.ThrowsAsync<ConflictException>(() =>
                IslemDogrulayici.KurTazeDogrula(db, id, k.TenantId, null, simdi));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            IslemDogrulayici.KurTazeDogrula(db, iptal, k.TenantId, null, simdi));
        await IslemDogrulayici.KurTazeDogrula(db, taze, k.TenantId, null, simdi);
        await IslemDogrulayici.KurTazeDogrula(db, bugun, k.TenantId, null, simdi);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            IslemDogrulayici.KurTazeDogrula(db, "yok", k.TenantId, null, simdi));
    }
}

[Collection("veritabani")]
public sealed class P0KurTemizlikTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Eski_kur_temizligi_gunun_son_kurunu_ve_bagli_kuru_korur_ara_kurlari_siler()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P0-kur-temizlik");
        using var db = vt.Olustur(k);
        // 120 gün önce, İstanbul öğlen (09:00 UTC) civarı: üç ara kur + bir harekete bağlanacak kur
        var gun = DateTime.UtcNow.Date.AddDays(-120).AddHours(9);
        string Ekle(DateTime an)
        {
            var id = Kimlik.Ulid();
            db.KurSnapshots.Add(new KurSnapshot { Id = id, TenantId = k.TenantId, AltinHasFiyati = 3_000m, Kaynak = "ALTINKAYNAK", Olusturuldu = an });
            return id;
        }
        var ara1 = Ekle(gun);
        var bagli = Ekle(gun.AddMinutes(1));
        var ara2 = Ekle(gun.AddMinutes(2));
        var sonGun = Ekle(gun.AddMinutes(3));       // günün son kuru
        var ertesi = Ekle(gun.AddDays(1));          // ertesi günün tek kuru
        await db.SaveChangesAsync();

        // "bagli" kur bir harekete bağlanır (veresiye yalnız kaydın varlığını denetler)
        await new Kuyumcu.Api.Modules.Cari.CariServisi(db,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
                TestKurulum.Denetim(db))
            .VeresiyeGir(k.CariId, new Kuyumcu.Api.Modules.Cari.VeresiyeGirisDto { TlTutari = 100m, KurSnapshotId = bagli, VadeGun = 30 }, k.Islem);

        await KurZamanlayici.EskiKurlariSil(vt.OlusturSistem(), DateTime.UtcNow.AddDays(-90), CancellationToken.None);

        using var kontrol = vt.OlusturSistem();
        var kalan = kontrol.KurSnapshots.AsNoTracking().Where(s => s.TenantId == k.TenantId).Select(s => s.Id).ToHashSet();
        Assert.DoesNotContain(ara1, kalan);
        Assert.DoesNotContain(ara2, kalan);
        Assert.Contains(bagli, kalan);
        Assert.Contains(sonGun, kalan);
        Assert.Contains(ertesi, kalan);
        Assert.Contains(k.KurId, kalan);            // yeni (90 günden genç) dokunulmaz
    }
}
