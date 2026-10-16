using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yerlesim;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// STOK YERLEŞİMİ — paket 25 Faz A [DB, gerçek MySQL]
//
// Ölçülen tek şey: I1 değişmezi (Σ yerleşim ≤ stok) ve reyon kapıları.
// Her test TEK katmanı ölçer — mutasyon kanıtı ancak böyle tek teste düşer.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class StokYerlesimiTestleri(TestVeritabani vt)
{
    private static YerlesimServisi Servis(KuyumcuDbContext db) => new(db);

    private static string ReyonAc(KuyumcuDbContext db, Kiraci k, string ad,
                                  string tip = ReyonTipi.Vitrin, bool aktif = true,
                                  string? subeId = null)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Set<Reyon>().Add(new Reyon
        {
            Id = id, TenantId = k.TenantId, SubeId = subeId ?? k.SubeId, Ad = ad, Tip = tip,
            Aktif = aktif, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task Yerlestir(TestVeritabani vt, Kiraci k, string reyonId, decimal adet)
    {
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, reyonId, adet, DateTime.UtcNow);
        await db.SaveChangesAsync();
    }

    private static decimal ReyonAdedi(TestVeritabani vt, string tenantId, string reyonId) =>
        vt.OlusturSistem().StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.ReyonId == reyonId)
            .Sum(y => (decimal?)y.Adet) ?? 0m;

    /// <summary>Temel yol: 10 adetten 4'ü vitrine → vitrin 4, atanmamış 6.</summary>
    [Fact]
    public async Task Yerlestirme_atanmamisi_dusurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 yerleştir", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k, "VITRIN-A");

        await Yerlestir(vt, k, vitrin, 4m);

        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(4m, ReyonAdedi(vt, k.TenantId, vitrin));
        Assert.Equal(6m, await Servis(oku).Atanmamis(k.TenantId, k.SubeId, k.UrunId));
        // `stoklar` DEĞİŞMEZ — yerleşim alt bölümlemedir.
        Assert.Equal(10m, await Servis(oku).StokAdedi(k.TenantId, k.SubeId, k.UrunId));
    }

    /// <summary>M25-1 hedefi: I1 kapısı (Σ yerleşim ≤ stok).</summary>
    [Fact]
    public async Task Atanmamistan_fazlasi_yerlestirilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 I1", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k, "VITRIN-A");

        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db2).Yerlestir(k.Islem, k.SubeId, k.UrunId, vitrin, 11m, DateTime.UtcNow));
        Assert.Equal(400, hata.StatusCode);
        Assert.Equal(0m, ReyonAdedi(vt, k.TenantId, vitrin));
    }

    /// <summary>Reyon içi taşıma toplam stoğa DOKUNMAZ.</summary>
    [Fact]
    public async Task Tasima_toplam_stogu_degistirmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 taşıma", stokAdet: 10m);
        string vitrin, kasa;
        using (var db = vt.OlusturSistem())
        {
            vitrin = ReyonAc(db, k, "VITRIN-A");
            kasa = ReyonAc(db, k, "KASA", ReyonTipi.Kasa);
        }
        await Yerlestir(vt, k, vitrin, 4m);

        using (var db = vt.Olustur(k.TenantId))
        {
            await Servis(db).Tasi(k.Islem, k.SubeId, k.UrunId, vitrin, kasa, 2m, null, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        Assert.Equal(2m, ReyonAdedi(vt, k.TenantId, vitrin));
        Assert.Equal(2m, ReyonAdedi(vt, k.TenantId, kasa));
        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(10m, await Servis(oku).StokAdedi(k.TenantId, k.SubeId, k.UrunId));
        Assert.Equal(6m, await Servis(oku).Atanmamis(k.TenantId, k.SubeId, k.UrunId));
    }

    [Fact]
    public async Task Kaynak_reyonda_yetersizse_tasinamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 yetersiz taşıma", stokAdet: 10m);
        string vitrin, kasa;
        using (var db = vt.OlusturSistem())
        {
            vitrin = ReyonAc(db, k, "VITRIN-A");
            kasa = ReyonAc(db, k, "KASA", ReyonTipi.Kasa);
        }
        await Yerlestir(vt, k, vitrin, 1m);

        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db2).Tasi(k.Islem, k.SubeId, k.UrunId, vitrin, kasa, 3m, null, DateTime.UtcNow));
        Assert.Equal(400, hata.StatusCode);
    }

    [Fact]
    public async Task Kaynak_ve_hedef_ayni_reyon_olamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 aynı reyon", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k, "VITRIN-A");
        await Yerlestir(vt, k, vitrin, 4m);

        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db2).Tasi(k.Islem, k.SubeId, k.UrunId, vitrin, vitrin, 1m, null, DateTime.UtcNow));
        Assert.Equal(400, hata.StatusCode);
    }

    /// <summary>M25-4 hedefi: reyon ŞUBESİNE kilitlidir.</summary>
    [Fact]
    public async Task Baska_subenin_reyonuna_yerlestirilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 şube kilidi", stokAdet: 10m);
        string yabanciReyon;
        using (var db = vt.OlusturSistem())
        {
            var sube2 = TestKurulum.SubeEkle(db, k.TenantId);
            yabanciReyon = ReyonAc(db, k, "SUBE2-VITRIN", subeId: sube2);
        }

        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db2).Yerlestir(k.Islem, k.SubeId, k.UrunId, yabanciReyon, 1m, DateTime.UtcNow));
        Assert.Equal(400, hata.StatusCode);
    }

    [Fact]
    public async Task Pasif_reyona_yerlestirilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 pasif reyon", stokAdet: 10m);
        string pasif;
        using (var db = vt.OlusturSistem()) pasif = ReyonAc(db, k, "ESKI-VITRIN", aktif: false);

        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db2).Yerlestir(k.Islem, k.SubeId, k.UrunId, pasif, 1m, DateTime.UtcNow));
        Assert.Equal(400, hata.StatusCode);
    }

    /// <summary>I2: sıfıra düşen yerleşim satırı SİLİNİR — çöp satır kalmaz.</summary>
    [Fact]
    public async Task Sifira_dusen_yerlesim_satiri_silinir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 sıfır satır", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k, "VITRIN-A");
        await Yerlestir(vt, k, vitrin, 3m);

        using (var db = vt.Olustur(k.TenantId))
        {
            await Servis(db).GeriAl(k.Islem, k.SubeId, k.UrunId, vitrin, 3m, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        using var oku = vt.OlusturSistem();
        Assert.False(await oku.StokYerlesimleri.AsNoTracking()
            .AnyAsync(y => y.TenantId == k.TenantId && y.ReyonId == vitrin));
    }

    /// <summary>Kiracı sızıntısı yok: yabancı reyon 404 (400 değil — varlık sızdırılmaz).</summary>
    [Fact]
    public async Task Yabanci_kiracinin_reyonuna_yerlestirilemez()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 kiracı A", stokAdet: 10m);
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 kiracı B", stokAdet: 10m);
        string yabanci;
        using (var db = vt.OlusturSistem()) yabanci = ReyonAc(db, b, "B-VITRIN");

        using var db2 = vt.Olustur(a.TenantId);
        var hata = await Assert.ThrowsAsync<NotFoundException>(() =>
            Servis(db2).Yerlestir(a.Islem, a.SubeId, a.UrunId, yabanci, 1m, DateTime.UtcNow));
        Assert.Equal(404, hata.StatusCode);
    }

    /// <summary>
    /// EŞZAMANLILIK (Mehmet + Aşkın koşulu): iki paralel bağlam aynı yerleşim
    /// satırını düşürürse İKİNCİSİ REDDEDİLİR. İyimser kilit `guncellendi`
    /// damgasıdır ([ConcurrencyCheck]); ExecuteUpdate KULLANILMAZ.
    /// </summary>
    [Fact]
    public async Task Esszamanli_iki_dusum_ikincisini_reddeder()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 eşzamanlı", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k, "VITRIN-A");
        await Yerlestir(vt, k, vitrin, 6m);

        using var bir = vt.Olustur(k.TenantId);
        using var iki = vt.Olustur(k.TenantId);

        var satirBir = await bir.StokYerlesimleri.FirstAsync(y => y.ReyonId == vitrin);
        var satirIki = await iki.StokYerlesimleri.FirstAsync(y => y.ReyonId == vitrin);

        satirBir.Adet -= 4m;
        satirBir.Guncellendi = DateTime.UtcNow;
        await bir.SaveChangesAsync();

        satirIki.Adet -= 4m;
        satirIki.Guncellendi = DateTime.UtcNow.AddMilliseconds(1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => iki.SaveChangesAsync());

        // Aşırı-düşüm OLMADI: 6 − 4 = 2 (2 değil −2).
        Assert.Equal(2m, ReyonAdedi(vt, k.TenantId, vitrin));
    }

    /// <summary>GRAM ürün (A15): 1 adet = 1 gram, 3 ondalık bozulmaz.</summary>
    [Fact]
    public async Task Gramli_uruncte_ondalik_bozulmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 gram", stokAdet: 7.5m);
        using (var g = vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): testin adı "gramlı ürün" — birim artık açıkça GRAM
        string vitrin, kasa;
        using (var db = vt.OlusturSistem())
        {
            vitrin = ReyonAc(db, k, "VITRIN-A");
            kasa = ReyonAc(db, k, "KASA", ReyonTipi.Kasa);
        }
        await Yerlestir(vt, k, vitrin, 2.5m);

        using (var db = vt.Olustur(k.TenantId))
        {
            await Servis(db).Tasi(k.Islem, k.SubeId, k.UrunId, vitrin, kasa, 0.3m, null, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        Assert.Equal(2.2m, ReyonAdedi(vt, k.TenantId, vitrin));
        Assert.Equal(0.3m, ReyonAdedi(vt, k.TenantId, kasa));
    }
}
