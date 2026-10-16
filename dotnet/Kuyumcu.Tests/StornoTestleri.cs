using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// STORNO MOTORU — paket 48 Faz A.
//
// Sınanan çekirdek: defter YERİNDE DEĞİŞMEZ, ters satır işaretiyle doğar,
// bir hareket en fazla BİR kez stornolanır, liste dışı tür REDDEDİLİR ve
// storno çifti kümülatif eşiklerde İKİNCİ KEZ sayılmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class StornoTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Kasiyer yanlış cariye işledi — mutabakatta düzeltildi";

    private static StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), NullLogger<StornoServisi>.Instance);

    private static SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance,
            new DenetimServisi(db, new HttpContextAccessor()));

    private static StornoDto Dto(Kiraci k, string? kasaId = null, string? gerekce = null) => new()
    {
        Gerekce = gerekce ?? Gerekce,
        KurSnapshotId = k.KurId,
        KasaId = kasaId,
    };

    /// <summary>Kasadan tahsil edilen bir işçilik fişi keser, hareket kimliğini döner.</summary>
    private static async Task<string> IscilikKes(KuyumcuDbContext db, Kiraci k, decimal tutar)
    {
        await Satis(db).Iscilik(new IscilikDto
        {
            CariId = k.CariId, IscilikTl = tutar, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, Aciklama = "İşçilik",
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ISCILIK" && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();
    }

    [Fact]
    public async Task Iscilik_stornosu_kasa_bakiyesini_geri_alir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-işçilik");

        using var db = vt.Olustur(k);
        var oncekiKasa = TestKurulum.KasaBakiye(db, k.KasaId);
        var kaynakId = await IscilikKes(db, k, 1_500m);
        Assert.Equal(oncekiKasa + 1_500m, TestKurulum.KasaBakiye(db, k.KasaId));

        using var db2 = vt.Olustur(k);
        var sonuc = await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(oncekiKasa, TestKurulum.KasaBakiye(kontrol, k.KasaId));

        var ters = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.StornoKaynakId == kaynakId);
        Assert.Equal(-1_500m, ters.TlTutari);
        Assert.Equal("ISCILIK", ters.Tur);
        Assert.Null(ters.IadeKaynakHareketId);   // storno iade DEĞİLDİR
        // Yanıt sözleşmesi: dönen kimlik deftere yazılan ters satırın kimliğidir.
        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(sonuc)).RootElement;
        Assert.Equal(ters.Id, json.GetProperty("stornoHareketId").GetString());
        Assert.Equal(kaynakId, json.GetProperty("kaynakHareketId").GetString());
    }

    [Fact]
    public async Task Kaynak_satir_degismez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-kaynak");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 900m);

        using var oku = vt.OlusturSistem();
        var once = oku.Hareketler.AsNoTracking().Single(h => h.Id == kaynakId);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var sonra = kontrol.Hareketler.AsNoTracking().Single(h => h.Id == kaynakId);
        Assert.Equal(once.TlTutari, sonra.TlTutari);
        Assert.Equal(once.HasGrami, sonra.HasGrami);
        Assert.Equal(once.Aciklama, sonra.Aciklama);
        Assert.Null(sonra.StornoKaynakId);        // kaynak satır storno DEĞİLDİR
        Assert.Equal(once.Olusturuldu, sonra.Olusturuldu);
    }

    [Fact]
    public async Task Nakit_tahsilat_stornosu_cari_bakiyeyi_geri_alir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-tahsilat");

        using var db = vt.Olustur(k);
        // Önce borç doğsun (veresiye), sonra tahsilat yapılsın.
        await Cari(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        { TlTutari = 5_000m, KurSnapshotId = k.KurId, Aciklama = "Veresiye" }, k.Islem);
        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 2_000m, OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, Aciklama = "Tahsilat",
        }, k.Islem);

        Assert.Equal(3_000m, TestKurulum.CariBakiye(db, k.CariId).Tl);

        var tahsilatId = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT")
            .Select(h => h.Id).SingleAsync();

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(tahsilatId, Dto(k, k.KasaId), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(5_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    [Fact]
    public async Task Emanet_giris_stornosu_has_bakiyeyi_geri_alir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-emanet");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "EMANET_GIRIS",
            Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = 4.5m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        });
        var bakiye = db.CariBakiyeler.Single(b => b.CariId == k.CariId);
        bakiye.HasGramBakiye -= 4.5m;
        await db.SaveChangesAsync();
        Assert.Equal(-4.5m, TestKurulum.CariBakiye(db, k.CariId).Has);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(id, Dto(k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
        var ters = kontrol.Hareketler.AsNoTracking().Single(h => h.StornoKaynakId == id);
        Assert.Equal(-4.5m, ters.HasGrami);
    }

    [Fact]
    public async Task Ayni_hareket_iki_kez_stornolanamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-cift");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 700m);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var db3 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db3).Storno(kaynakId, Dto(k, k.KasaId), k.Islem));
        Assert.Contains("zaten stornolanmıştır", hata.Message);

        // Defterde ÜÇÜNCÜ satır oluşmadı.
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Hareketler.AsNoTracking()
            .Count(h => h.TenantId == k.TenantId && h.Tur == "ISCILIK"));
    }

    [Fact]
    public async Task Storno_satirinin_kendisi_stornolanamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-storno");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 400m);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var oku = vt.OlusturSistem();
        var tersId = oku.Hareketler.AsNoTracking().Single(h => h.StornoKaynakId == kaynakId).Id;

        using var db3 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db3).Storno(tersId, Dto(k, k.KasaId), k.Islem));
        Assert.Contains("STORNO kaydıdır", hata.Message);
    }

    [Fact]
    public async Task SATIS_storno_istegi_409_ve_mesaj_iade_yolunu_soyler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-satis");

        using var db = vt.Olustur(k);
        await Satis(db).Satis(new SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        var satisId = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Select(h => h.Id).FirstAsync();

        using var db2 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db2).Storno(satisId, Dto(k, k.KasaId), k.Islem));
        Assert.Contains("satis-iade", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
    }

    [Fact]
    public async Task CARI_DUZELTME_storno_istegi_409_ve_duzeltme_fisi_yolunu_soyler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-duzeltme");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "CARI_DUZELTME",
            Tarih = DateTime.UtcNow, TlTutari = 100m, HasGrami = 0m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var db2 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db2).Storno(id, Dto(k), k.Islem));
        Assert.Contains("duzeltme", hata.Message);
    }

    [Fact]
    public async Task Stok_bacakli_turlerin_stornosu_KAPALIDIR_409()
    {
        // TAKAS / HURDA_ALIS / TRANSFER: maliyet partisi geri alması gerektiği
        // için beyaz listede DEĞİL. Fail-closed: kısmi geri alma yapılmaz.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-takas");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAKAS",
            Tarih = DateTime.UtcNow, TlTutari = 250m, HasGrami = 1m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var db2 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db2).Storno(id, Dto(k), k.Islem));
        Assert.Contains("maliyet partisi", hata.Message);
    }

    [Fact]
    public async Task Gerekce_19_karakter_400_20_karakter_gecer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-gerekçe");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 300m);

        using var db2 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Storno(db2).Storno(kaynakId, Dto(k, k.KasaId, new string('a', 19)), k.Islem));
        Assert.Contains("20 karakter", hata.Message);

        using var db3 = vt.Olustur(k);
        await Storno(db3).Storno(kaynakId, Dto(k, k.KasaId, new string('a', 20)), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.StornoKaynakId == kaynakId));
    }

    [Fact]
    public async Task Storno_denetim_izine_gerekceli_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-iz");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 250m);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.VarlikTuru == "Storno"
                      && d.Eylem == DenetimEylemi.Ekleme);
        Assert.True(iz.Hassas);
        Assert.Contains(Gerekce, iz.Detay);
        Assert.Equal(k.SubeId, iz.SubeId);
        Assert.Equal(k.KasaId, iz.KasaId);
    }

    [Fact]
    public async Task Reddedilen_storno_denemesi_de_ize_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-red");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TRANSFER",
            Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = 0m,
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<ConflictException>(
            () => Storno(db2).Storno(id, Dto(k), k.Islem));

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Reddedildi
                      && d.VarlikTuru == "Storno");
        Assert.Equal(id, iz.VarlikId);
        Assert.Contains("REDDEDİLDİ", iz.Detay);
    }

    [Fact]
    public async Task Storno_cifti_tevsik_haddini_ikinci_kez_doldurmaz()
    {
        // ASİMETRİK kurgu (Aşkın koşulu): 25.000 nakit → storno → 10.000 nakit.
        // Süzgeç varsa kümülatif 10.000'dir ve kapı AÇIKTIR; süzgeç kaldırılırsa
        // 35.000 sayılır ve kapı 30.000 haddinde KAPANIR.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-tevsik");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 25_000m);

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var db3 = vt.Olustur(k);
        var toplam = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi
            .GunlukNakitToplam(db3, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(0m, toplam);

        // Yeni 10.000 TL nakit işlem kapıdan GEÇER.
        await IscilikKes(db3, k, 10_000m);

        using var db4 = vt.Olustur(k);
        Assert.Equal(10_000m, await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi
            .GunlukNakitToplam(db4, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    [Fact]
    public async Task Storno_cifti_MASAK_kumulatifinde_cift_saymaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Storno-masak");

        using var db = vt.Olustur(k);
        var kaynakId = await IscilikKes(db, k, 20_000m);

        using var once = vt.Olustur(k);
        Assert.Equal(20_000m, await Kuyumcu.Api.Modules.Masak.MasakKapisi
            .GunlukIslemToplam(once, k.TenantId, k.CariId, DateTime.UtcNow));

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(kaynakId, Dto(k, k.KasaId), k.Islem);

        using var sonra = vt.Olustur(k);
        // Çift dışlanır: ne şişer (40.000) ne tek taraflı azalır (20.000) — 0.
        Assert.Equal(0m, await Kuyumcu.Api.Modules.Masak.MasakKapisi
            .GunlukIslemToplam(sonra, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    [Fact]
    public async Task Yabanci_kiracinin_hareketi_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Storno-A");
        var b = TestKurulum.KiraciKur(kurulum, "Storno-B");

        using var dbA = vt.Olustur(a);
        var kaynakId = await IscilikKes(dbA, a, 100m);

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Storno(dbB).Storno(kaynakId, Dto(b, b.KasaId), b.Islem));
    }
}
