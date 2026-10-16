using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN AĞACI · REYON · KONSİNYE
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UrunAgaciTestleri(TestVeritabani vt)
{
    private static KategoriServisi Kat(KuyumcuDbContext db) => new(db);

    private static async Task<string> Ekle(KategoriServisi s, AktifKullanici islem, string ad, string? ust = null)
    {
        var k = (UrunKategorisi)await s.Olustur(new KategoriOlusturDto { Ad = ad, UstId = ust }, islem);
        return k.Id;
    }

    [Fact]
    public async Task Cok_seviyeli_agac_ic_ice_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Ağaç");
        using var db = vt.Olustur(k.TenantId);
        var s = Kat(db);

        var ziynet = await Ekle(s, k.Islem, "Ziynet");
        var bilezik = await Ekle(s, k.Islem, "Bilezik", ziynet);
        await Ekle(s, k.Islem, "Ajda", bilezik);        // 3. seviye

        var agac = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(await s.Agac(k.Islem))).RootElement;

        Assert.Equal(1, agac.GetArrayLength());                       // tek kök
        var kok = agac[0];
        Assert.Equal("Ziynet", kok.GetProperty("ad").GetString());
        var seviye2 = kok.GetProperty("altlar")[0];
        Assert.Equal("Bilezik", seviye2.GetProperty("ad").GetString());
        Assert.Equal("Ajda", seviye2.GetProperty("altlar")[0].GetProperty("ad").GetString());
    }

    [Fact]
    public async Task Kategori_KENDI_ustu_olamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döngü 1");
        using var db = vt.Olustur(k.TenantId);
        var s = Kat(db);
        var a = await Ekle(s, k.Islem, "A");

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => s.Guncelle(a, new KategoriGuncelleDto { UstId = a }, k.Islem));
        Assert.Contains("kendi üst kategorisi", hata.Message);
    }

    [Fact]
    public async Task Kategori_kendi_ALTININ_altina_tasinamaz()
    {
        // Taşınabilseydi ağaç yürüyüşü sonsuz döngüye girer, ürün listesi
        // isteği sunucuyu kilitlerdi.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döngü 2");
        using var db = vt.Olustur(k.TenantId);
        var s = Kat(db);
        var a = await Ekle(s, k.Islem, "A");
        var b = await Ekle(s, k.Islem, "B", a);
        var c = await Ekle(s, k.Islem, "C", b);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => s.Guncelle(a, new KategoriGuncelleDto { UstId = c }, k.Islem));
        Assert.Contains("döngü", hata.Message);
    }

    [Fact]
    public async Task Alt_agac_idleri_TUM_torunlari_kapsar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Alt ağaç");
        using var db = vt.Olustur(k.TenantId);
        var s = Kat(db);
        var a = await Ekle(s, k.Islem, "A");
        var b = await Ekle(s, k.Islem, "B", a);
        var c = await Ekle(s, k.Islem, "C", b);
        await Ekle(s, k.Islem, "Alakasız");

        var idler = await s.AltAgacIdleri(a, k.TenantId);
        Assert.Equal(3, idler.Count);
        Assert.Contains(b, idler);
        Assert.Contains(c, idler);
    }

    [Fact]
    public async Task Kategori_filtresi_ALT_kategorileri_de_getirir()
    {
        // Ağacın asıl işi bu: "Ziynet" seçilince altındaki bilezikler de gelmeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kategori filtresi");
        using var db = vt.Olustur(k.TenantId);
        var s = Kat(db);
        var ziynet = await Ekle(s, k.Islem, "Ziynet");
        var bilezik = await Ekle(s, k.Islem, "Bilezik", ziynet);

        var us = new UrunServisi(db);
        var dto = new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "AGC-1", Ad = "Ajda bilezik",
            AyarId = "K22", GramAgirlik = 10m, KategoriId = bilezik,
        };
        await us.Olustur(dto, k.Islem);

        var liste = await us.Listele(k.Islem, null, null, null, null, 50, 0, null, kategoriId: ziynet);
        Assert.Contains(liste, u => u.Kod == "AGC-1");
    }

    [Fact]
    public async Task BASKA_kiracinin_kategorisi_urune_baglanamaz()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "Kat A");
        var b = TestKurulum.KiraciKur(sistem, "Kat B");

        string yabanci;
        using (var dbB = vt.Olustur(b.TenantId))
            yabanci = await Ekle(Kat(dbB), b.Islem, "B kategorisi");

        using var dbA = vt.Olustur(a.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => new UrunServisi(dbA).Olustur(new UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = "X-1", Ad = "Sızan ürün",
                AyarId = "K22", GramAgirlik = 5m, KategoriId = yabanci,
            }, a.Islem));
    }
}

[Collection("veritabani")]
public sealed class KonsinyeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static UrunOlusturDto Dto(string kod, bool konsinye, string? cari = null, decimal? bedel = null) => new()
    {
        Tip = "ZIYNET", Kod = kod, Ad = "Konsinye bilezik",
        AyarId = "K22", GramAgirlik = 10m,
        Konsinye = konsinye, KonsinyeCariId = cari, KonsinyeBirimBedel = bedel,
    };

    [Fact]
    public async Task Konsinye_urun_SAHIPSIZ_kaydedilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye sahipsiz");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new UrunServisi(db).Olustur(Dto("KN-1", true), k.Islem));
        Assert.Contains("sahibi", hata.Message);
    }

    [Fact]
    public async Task Konsinye_urun_BEDELSIZ_kaydedilemez()
    {
        // Bedel yoksa satışta borç doğmaz ve kâr olduğundan yüksek görünür.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye bedelsiz");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new UrunServisi(db).Olustur(Dto("KN-2", true, k.CariId), k.Islem));
        Assert.Contains("birim bedeli", hata.Message);
    }

    [Fact]
    public async Task Konsinye_OLMAYAN_urune_konsinye_alani_girilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tutarsız konsinye");
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => new UrunServisi(db).Olustur(Dto("KN-3", false, k.CariId, 100m), k.Islem));
    }

    [Fact]
    public async Task Konsinye_satisi_bedeli_SAHIBINE_borclanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye satış");
        using var db = vt.Olustur(k.TenantId);

        await new UrunServisi(db).Olustur(Dto("KN-4", true, k.CariId, 4_000m), k.Islem);
        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == "KN-4");

        // Stok ver (konsinye mal vitrine konur, alış yapılmaz)
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = urun.Id,
            Adet = 2m, ToplamHasGram = 2m * urun.HasGrami, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var (oncekiTl, _) = TestKurulum.CariBakiye(db, k.CariId);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urun.Id, Adet = 1m, BirimFiyatTl = 6_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        var (sonrakiTl, _) = TestKurulum.CariBakiye(db, k.CariId);

        // Mal sahibine 4.000 TL borçlanıldı (bakiye negatife gider = biz borçluyuz)
        Assert.Equal(oncekiTl - 4_000m, sonrakiTl);
    }

    [Fact]
    public async Task Eksik_konsinye_bilgisiyle_SATILAMAZ()
    {
        // Göç öncesi ya da elle bozulmuş kayıt satış anında yakalanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye bozuk");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.Konsinye = true;
        urun.KonsinyeCariId = null;      // bozuk kayıt
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("konsinye", hata.Message);
    }

    [Fact]
    public async Task Konsinye_mal_STOK_DEGERINE_katilmaz()
    {
        // Başkasının malı bizim varlığımız değildir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye stok değeri");
        using var db = vt.Olustur(k.TenantId);

        await new UrunServisi(db).Olustur(Dto("KN-5", true, k.CariId, 4_000m), k.Islem);
        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == "KN-5");

        new Kuyumcu.Api.Modules.Maliyet.MaliyetMotoru(db).PartiAc(
            k.TenantId, k.SubeId, urun.Id, PartiKaynagi.Alis, 3m, urun.HasGrami,
            DateTime.UtcNow, birimMaliyetTl: 4_000m, altinHasFiyati: 1_000m);
        await db.SaveChangesAsync();

        var r = await new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db)
            .StokDegerlemesi(k.Islem, null);
        var j = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(r)).RootElement;

        Assert.Equal(0m, j.GetProperty("stokMaliyetiTl").GetDecimal());
        Assert.Equal(1, j.GetProperty("konsinyeUrunSayisi").GetInt32());
        Assert.Contains("konsinye", j.GetProperty("uyari").GetString());
    }
}
