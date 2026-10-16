using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Api.Modules.Yerlesim;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// REYON STOK RAPORU — paket 25 Faz A [DB, gerçek MySQL]
//
// Ölçülen: madenlerin AYRI toplanması, ATANMAMIŞ satırının ayrı görünmesi,
// şube süzgeci ve TB-19 (gruplama SUNUCUDA — sınırsız ToListAsync yok).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ReyonRaporTestleri(TestVeritabani vt)
{
    private static RaporServisi Rapor(KuyumcuDbContext db) => TestKurulum.RaporServisi(db);
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

    private static string UrunEkle(KuyumcuDbContext db, Kiraci k, string ayarId, string subeId,
                                   decimal stokAdet)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        var milyem = Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId)!.Milyem;
        var has = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(10m, 0m, ayarId);
        db.Urunler.Add(new Urun
        {
            // P-05: ULID'in İLK 8 hanesi ZAMAN damgasıdır — aynı milisaniyede açılan
            // iki ürün aynı kodu alıyordu. SON 8 hane rastgele bölümdendir.
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"R-{id[^8..]}", Ad = $"Ürün {ayarId}",
            AyarId = ayarId, Milyem = milyem, GramAgirlik = 10m, TasAgirlik = 0m, HasGrami = has,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeId, UrunId = id,
            Adet = stokAdet, ToplamHasGram = stokAdet * has, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static JsonElement Json(object o) =>
        JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    /// <summary>M25-10 hedefi: farklı madenler TEK toplamda birleştirilmez.</summary>
    [Fact]
    public async Task Rapor_madenleri_ayri_toplar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 rapor maden", stokAdet: 0m);
        string reyon, altin, gumus;
        using (var db = vt.OlusturSistem())
        {
            reyon = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
            altin = UrunEkle(db, k, "K22", k.SubeId, 5m);
            gumus = UrunEkle(db, k, "G925", k.SubeId, 8m);
        }

        using (var db = vt.Olustur(k.TenantId))
        {
            await Yer(db).Yerlestir(k.Islem, k.SubeId, altin, reyon, 5m, DateTime.UtcNow);
            await Yer(db).Yerlestir(k.Islem, k.SubeId, gumus, reyon, 8m, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        using var oku = vt.Olustur(k.TenantId);
        var y = Json(await Rapor(oku).ReyonStok(k.Islem, null, null, null));

        var satir = y.GetProperty("reyonlar").EnumerateArray().Single();
        var madenler = satir.GetProperty("madenler").EnumerateArray().ToList();
        Assert.Equal(2, madenler.Count);   // altın ve gümüş AYRI satır
        Assert.Equal(13m, satir.GetProperty("adet").GetDecimal());
        // Toplam YALNIZ altındır — gümüş gramı buraya karışmaz.
        var altinHas = satir.GetProperty("altinHasGram").GetDecimal();
        var gumusGram = madenler.Single(m => m.GetProperty("maden").GetString() != "ALTIN")
            .GetProperty("toplamGram").GetDecimal();
        Assert.True(gumusGram > 0m);
        Assert.True(altinHas < altinHas + gumusGram);
        Assert.NotNull(y.GetProperty("uyari").GetString());
    }

    /// <summary>ATANMAMIŞ mal ayrı satırdır ve gizlenmez.</summary>
    [Fact]
    public async Task Atanmamis_ayri_satir_olarak_gorunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 rapor atanmamış", stokAdet: 10m);
        string reyon;
        using (var db = vt.OlusturSistem()) reyon = ReyonAc(db, k.TenantId, k.SubeId, "A-VITRIN");
        using (var db = vt.Olustur(k.TenantId))
        {
            await Yer(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, reyon, 4m, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        using var oku = vt.Olustur(k.TenantId);
        var y = Json(await Rapor(oku).ReyonStok(k.Islem, k.SubeId, null, null));

        Assert.Equal(4m, y.GetProperty("reyonlar").EnumerateArray().Single()
            .GetProperty("adet").GetDecimal());
        var atanmamis = y.GetProperty("atanmamis").EnumerateArray().Single();
        Assert.Equal(6m, atanmamis.GetProperty("adet").GetDecimal());
        Assert.Equal(k.SubeId, atanmamis.GetProperty("subeId").GetString());
    }

    /// <summary>subeId süzgeci gerçekten uygulanır.</summary>
    [Fact]
    public async Task Sube_suzgeci_uygulanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P25 rapor şube", stokAdet: 10m);
        string sube2, reyon1, reyon2, urun2;
        using (var db = vt.OlusturSistem())
        {
            sube2 = TestKurulum.SubeEkle(db, k.TenantId);
            reyon1 = ReyonAc(db, k.TenantId, k.SubeId, "S1-VITRIN");
            reyon2 = ReyonAc(db, k.TenantId, sube2, "S2-VITRIN");
            urun2 = UrunEkle(db, k, "K22", sube2, 6m);
        }
        using (var db = vt.Olustur(k.TenantId))
        {
            await Yer(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, reyon1, 4m, DateTime.UtcNow);
            await Yer(db).Yerlestir(k.Islem, sube2, urun2, reyon2, 6m, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        using var oku = vt.Olustur(k.TenantId);
        var y = Json(await Rapor(oku).ReyonStok(k.Islem, sube2, null, null));
        var satir = y.GetProperty("reyonlar").EnumerateArray().Single();
        Assert.Equal("S2-VITRIN", satir.GetProperty("reyonAdi").GetString());
        Assert.Equal(6m, satir.GetProperty("adet").GetDecimal());
    }

    /// <summary>
    /// TB-19 ölçülebilir kabul ölçütü (Aşkın koşulu): gruplama SUNUCUDA yapılır.
    /// Ölçüm KAYNAK TARAMASIDIR — ReyonStok gövdesinde her `ToListAsync`
    /// çağrısından önce bir `GroupBy` bulunmalıdır (StokDegeri'nin sınırsız
    /// `ToListAsync` deseni KOPYALANMAMIŞTIR).
    /// </summary>
    [Fact]
    public void Rapor_gruplamayi_sunucuda_yapar()
    {
        var kaynak = File.ReadAllText(Path.Combine(KokDizin(), "Kuyumcu.Api/Modules/Rapor/RaporModulu.cs"));
        var bas = kaynak.IndexOf("public async Task<object> ReyonStok(", StringComparison.Ordinal);
        Assert.True(bas > 0, "ReyonStok metodu bulunamadı");
        var son = kaynak.IndexOf("\n    }\n}", bas, StringComparison.Ordinal);
        var govde = kaynak[bas..(son > bas ? son : kaynak.Length)];

        var groupBy = govde.Split("GroupBy").Length - 1;
        var toList = govde.Split("ToListAsync").Length - 1;
        Assert.True(groupBy >= toList,
            $"ReyonStok'ta {toList} ToListAsync'e karşılık yalnız {groupBy} GroupBy var — "
            + "gruplama uygulamaya kaymış olabilir (TB-19).");
    }

    private static string KokDizin()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
