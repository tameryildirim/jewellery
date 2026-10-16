using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Api.Modules.Yerlesim;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YERLEŞİM ÇIKIŞ/GİRİŞ BACAĞI — paket 25 Faz A [DB, gerçek MySQL]
//
// KARAR 3 sırası: reyon verildiyse YALNIZ o reyondan (fail-closed); verilmediyse
// önce ATANMAMIŞ, sonra reyonlar AD ARTAN sırada + uyarı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class YerlesimCikisTestleri(TestVeritabani vt)
{
    private static SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<SatisServisi>.Instance);

    private static YerlesimServisi Yer(KuyumcuDbContext db) => new(db);

    private static string ReyonAc(KuyumcuDbContext db, string tenantId, string subeId, string ad)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Set<Reyon>().Add(new Reyon
        {
            Id = id, TenantId = tenantId, SubeId = subeId, Ad = ad, Tip = ReyonTipi.Vitrin,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task Yerlestir(TestVeritabani vt, Kiraci k, string reyonId, decimal adet)
    {
        using var db = vt.Olustur(k.TenantId);
        await Yer(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, reyonId, adet, DateTime.UtcNow);
        await db.SaveChangesAsync();
    }

    private static decimal ReyonAdedi(TestVeritabani vt, string tenantId, string reyonId) =>
        vt.OlusturSistem().StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.ReyonId == reyonId)
            .Sum(y => (decimal?)y.Adet) ?? 0m;

    private static SatisOlusturDto SatisDto(Kiraci k, decimal adet, string? reyonId = null) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = adet, BirimFiyatTl = 100m,
        OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId, ReyonId = reyonId,
    };

    /// <summary>reyonId verilmişse YALNIZ o reyondan düşer.</summary>
    [Fact]
    public async Task ReyonId_ile_satis_yalniz_o_reyondan_duser()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 çıkış reyon", stokAdet: 10m);
        string vitrin, kasa;
        using (var db = vt.OlusturSistem())
        {
            vitrin = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            kasa = ReyonAc(db, k.TenantId, k.SubeId, "B-KASA");
        }
        await Yerlestir(vt, k, vitrin, 4m);
        await Yerlestir(vt, k, kasa, 3m);

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Satis(SatisDto(k, 2m, kasa), k.Islem);

        Assert.Equal(4m, ReyonAdedi(vt, k.TenantId, vitrin));
        Assert.Equal(1m, ReyonAdedi(vt, k.TenantId, kasa));
    }

    /// <summary>M25-2 hedefi: o reyonda yetersizse 400 — başka reyondan alınmaz.</summary>
    [Fact]
    public async Task ReyonId_ile_satis_o_reyonda_yetersizse_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 çıkış yetersiz", stokAdet: 10m);
        string vitrin, kasa;
        using (var db = vt.OlusturSistem())
        {
            vitrin = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            kasa = ReyonAc(db, k.TenantId, k.SubeId, "B-KASA");
        }
        await Yerlestir(vt, k, vitrin, 8m);
        await Yerlestir(vt, k, kasa, 2m);

        using (var db = vt.Olustur(k.TenantId))
        {
            // Şube stoğu 10, yani stok YETERLİ; buna rağmen KASA'da 2 varken 3 istenirse red.
            var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
                Satis(db).Satis(SatisDto(k, 3m, kasa), k.Islem));
            Assert.Equal(400, hata.StatusCode);
        }

        Assert.Equal(2m, ReyonAdedi(vt, k.TenantId, kasa));
        Assert.Equal(8m, ReyonAdedi(vt, k.TenantId, vitrin));
    }

    /// <summary>M25-3 hedefi: reyonsuz çıkışta ÖNCE atanmamış tükenir.</summary>
    [Fact]
    public async Task Reyonsuz_satis_once_atanmamisi_tuketir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 atanmamış önce", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
        await Yerlestir(vt, k, vitrin, 4m);   // atanmamış 6

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Satis(SatisDto(k, 5m), k.Islem);

        // Yerleşim satırına DOKUNULMADI; düşen mal atanmamıştandır.
        Assert.Equal(4m, ReyonAdedi(vt, k.TenantId, vitrin));
        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(5m, await Yer(oku).StokAdedi(k.TenantId, k.SubeId, k.UrunId));
        Assert.Equal(1m, await Yer(oku).Atanmamis(k.TenantId, k.SubeId, k.UrunId));
    }

    /// <summary>Atanmamış bitince reyonlar AD SIRASIYLA tüketilir ve UYARI döner.</summary>
    [Fact]
    public async Task Atanmamis_bitince_reyonlar_ad_sirasiyla_tuketilir_ve_uyari_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 ad sırası", stokAdet: 10m);
        string a, b;
        using (var db = vt.OlusturSistem())
        {
            b = ReyonAc(db, k.TenantId, k.SubeId, "B-KASA");
            a = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
        }
        await Yerlestir(vt, k, a, 5m);
        await Yerlestir(vt, k, b, 5m);   // atanmamış 0

        object sonuc;
        using (var db = vt.Olustur(k.TenantId))
            sonuc = await Satis(db).Satis(SatisDto(k, 4m), k.Islem);

        // A-VITRIN adça önce gelir → önce o tükenir.
        Assert.Equal(1m, ReyonAdedi(vt, k.TenantId, a));
        Assert.Equal(5m, ReyonAdedi(vt, k.TenantId, b));

        var govde = System.Text.Json.JsonSerializer.Serialize(sonuc);
        Assert.Contains("yerlesimUyarisi", govde);
        Assert.Contains("A-VITRIN", govde);
    }

    /// <summary>Şubeler arası transfer: kaynakta reyondan düşer, hedefte reyona girer.</summary>
    [Fact]
    public async Task Transfer_kaynak_reyondan_duser_hedef_reyona_girer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 transfer", stokAdet: 10m);
        string sube2, kaynakReyon, hedefReyon;
        using (var db = vt.OlusturSistem())
        {
            sube2 = TestKurulum.SubeEkle(db, k.TenantId);
            kaynakReyon = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            hedefReyon = ReyonAc(db, k.TenantId, sube2, "S2-VITRIN");
        }
        await Yerlestir(vt, k, kaynakReyon, 6m);

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Transfer(new TransferDto
            {
                KaynakSubeId = k.SubeId, HedefSubeId = sube2, UrunId = k.UrunId, Adet = 3m,
                KurSnapshotId = k.KurId, ReyonId = kaynakReyon, HedefReyonId = hedefReyon,
            }, k.Islem);

        Assert.Equal(3m, ReyonAdedi(vt, k.TenantId, kaynakReyon));
        Assert.Equal(3m, ReyonAdedi(vt, k.TenantId, hedefReyon));
    }

    /// <summary>hedefReyonId yoksa mal hedef şubede ATANMAMIŞ olur.</summary>
    [Fact]
    public async Task Hedef_reyon_yoksa_hedef_subede_atanmamis_olur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 transfer atanmamış", stokAdet: 10m);
        string sube2, hedefReyon;
        using (var db = vt.OlusturSistem())
        {
            sube2 = TestKurulum.SubeEkle(db, k.TenantId);
            hedefReyon = ReyonAc(db, k.TenantId, sube2, "S2-VITRIN");
        }

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Transfer(new TransferDto
            {
                KaynakSubeId = k.SubeId, HedefSubeId = sube2, UrunId = k.UrunId, Adet = 3m,
                KurSnapshotId = k.KurId,
            }, k.Islem);

        Assert.Equal(0m, ReyonAdedi(vt, k.TenantId, hedefReyon));
        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(3m, await Yer(oku).Atanmamis(k.TenantId, sube2, k.UrunId));
    }

    /// <summary>Alışta ürünün VARSAYILAN reyonu doğru şubedeyse oraya girer.</summary>
    [Fact]
    public async Task Aliste_varsayilan_reyon_dogru_subedeyse_oraya_girer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 varsayılan reyon", stokAdet: 0m);
        string varsayilan;
        using (var db = vt.OlusturSistem())
        {
            varsayilan = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.ReyonId = varsayilan;
            db.SaveChanges();
        }

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Alis(new AlisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 4m, BirimFiyatTl = 100m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        Assert.Equal(4m, ReyonAdedi(vt, k.TenantId, varsayilan));
    }

    /// <summary>M25-8 hedefi: varsayılan reyon BAŞKA şubedeyse sessizce kullanılmaz.</summary>
    [Fact]
    public async Task Varsayilan_reyon_baska_subedeyse_atanmamisa_girer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 yabancı varsayılan", stokAdet: 0m);
        string yabanciReyon;
        using (var db = vt.OlusturSistem())
        {
            var sube2 = TestKurulum.SubeEkle(db, k.TenantId);
            yabanciReyon = ReyonAc(db, k.TenantId, sube2, "S2-VITRIN");
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.ReyonId = yabanciReyon;
            db.SaveChanges();
        }

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Alis(new AlisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 4m, BirimFiyatTl = 100m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        Assert.Equal(0m, ReyonAdedi(vt, k.TenantId, yabanciReyon));
        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(4m, await Yer(oku).Atanmamis(k.TenantId, k.SubeId, k.UrunId));
    }

    /// <summary>Satış sonrası I1 korunur: Σ yerleşim ≤ stok.</summary>
    [Fact]
    public async Task Satis_sonrasi_I1_korunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 I1 satış", stokAdet: 10m);
        string a, b;
        using (var db = vt.OlusturSistem())
        {
            a = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            b = ReyonAc(db, k.TenantId, k.SubeId, "B-KASA");
        }
        await Yerlestir(vt, k, a, 5m);
        await Yerlestir(vt, k, b, 5m);

        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Satis(SatisDto(k, 7m), k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var stok = await Yer(oku).StokAdedi(k.TenantId, k.SubeId, k.UrunId);
        var yerlesik = await Yer(oku).YerlesikToplam(k.TenantId, k.SubeId, k.UrunId);
        Assert.Equal(3m, stok);
        Assert.True(yerlesik <= stok, $"I1 bozuldu: yerleşim {yerlesik} > stok {stok}");
    }

    /// <summary>Sayım düzeltmesi de yerleşimi düzeltir ve I1'i korur (TB-18 yolu).</summary>
    [Fact]
    public async Task Sayim_dusumu_yerlesimi_de_duzeltir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 sayım", stokAdet: 10m);
        string vitrin;
        using (var db = vt.OlusturSistem()) vitrin = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
        await Yerlestir(vt, k, vitrin, 10m);   // atanmamış 0

        using (var db = vt.Olustur(k.TenantId))
            await new Kuyumcu.Api.Modules.Urun.UrunServisi(db)
                .StokSayimGuncelle(k.UrunId, k.SubeId, 6m, k.Islem);

        Assert.Equal(6m, ReyonAdedi(vt, k.TenantId, vitrin));
        using var oku = vt.Olustur(k.TenantId);
        Assert.Equal(0m, await Yer(oku).Atanmamis(k.TenantId, k.SubeId, k.UrunId));
    }
}
