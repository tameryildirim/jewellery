using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Vade;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MRapor = Kuyumcu.Api.Modules.Rapor;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VADE TAKİBİ — paket 28
//
// İki ayrı açığı birden kapatır:
//  1. VADE: borcun ne zaman ödeneceği hiçbir yerde yazmıyordu. Yaşlandırma
//     raporu borcun KAÇ GÜNDÜR durduğunu gösteriyordu; 90 gün vade verilen
//     60 günlük borç orada "61-90" diliminde, yani geciken gibi görünüyordu.
//  2. KREDİ LİMİTİ: sunucuda vardı ama YALNIZ CariModulu.VeresiyeGir yolunda
//     işliyordu. Veresiye SATIŞ, karma fişin veresiye bacağı ve takas/işçilik
//     farkı limiti hiç görmüyordu — limit, kasiyerin hangi ekranı kullandığına
//     bağlı bir tavsiyeydi.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class VadeTakibiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

    private static MRapor.RaporServisi Rapor(KuyumcuDbContext db) => TestKurulum.RaporServisi(db);

    private static MSatis.SatisOlusturDto VeresiyeSatis(Kiraci k, decimal adet, decimal birim, int? vadeGun) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
        OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId, VadeGun = vadeGun,
    };

    private Hareket SonHareket(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == tur)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .First();
    }

    // ── Vade yazımı ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Veresiye_satis_vade_gununu_HAREKETE_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade yazım", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 5_000m, vadeGun: 30), k.Islem);

        var h = SonHareket(k, "SATIS");
        Assert.Equal(Zaman.YerelBugun().AddDays(30), h.VadeTarihi);
        Assert.Equal(5_000m, h.VeresiyeTutar);
    }

    /// <summary>
    /// PEŞİN satışa vade YAZILMAZ. Yazılsaydı vade takibi raporu, parası çoktan
    /// alınmış bir satışı "vadesi gelen borç" diye listeler ve kasiyer ödemiş
    /// müşteriyi arardı.
    /// </summary>
    [Fact]
    public async Task Pesin_satis_vade_YAZMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade peşin", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        Assert.Null(SonHareket(k, "SATIS").VadeTarihi);
    }

    [Fact]
    public async Task Vade_verilmezse_CARININ_varsayilani_uygulanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade varsayılan", stokAdet: 10m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.VarsayilanVadeGun = 45;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 5_000m, vadeGun: null), k.Islem);

        Assert.Equal(Zaman.YerelBugun().AddDays(45), SonHareket(k, "SATIS").VadeTarihi);
    }

    [Fact]
    public async Task Islemdeki_vade_carinin_varsayilanini_EZER()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade ezme", stokAdet: 10m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.VarsayilanVadeGun = 45;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 5_000m, vadeGun: 7), k.Islem);

        Assert.Equal(Zaman.YerelBugun().AddDays(7), SonHareket(k, "SATIS").VadeTarihi);
    }

    // ── Kredi limiti — artık HER veresiye yolunda ─────────────────────────────

    /// <summary>
    /// ⚠ BU TESTİN VARLIK SEBEBİ: limit bu pakete kadar veresiye satışta HİÇ
    /// bakılmıyordu. 1.000 TL limitli müşteriye ekrandan 50.000 TL'lik veresiye
    /// satış yapılabiliyordu.
    /// </summary>
    [Fact]
    public async Task Veresiye_SATIS_kredi_limitini_asamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Limit satış", stokAdet: 10m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.KrediLimitiTl = 4_000m;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(VeresiyeSatis(k, 1m, 5_000m, vadeGun: 30), k.Islem));
        Assert.Contains("Kredi limiti", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.False(await kontrol.Hareketler.AnyAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }

    [Fact]
    public async Task Limit_icindeki_veresiye_satis_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Limit içi", stokAdet: 10m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.KrediLimitiTl = 10_000m;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 5_000m, vadeGun: 30), k.Islem);

        Assert.Equal(5_000m, SonHareket(k, "SATIS").VeresiyeTutar);
    }

    /// <summary>
    /// Limit BİRİKİMLİDİR: tek başına limitin altındaki ikinci satış, mevcut
    /// bakiyeyle birlikte limiti aşıyorsa reddedilir. Aksi halde 4.000 limitli
    /// müşteriye 3.000'lik satış istendiği kadar tekrarlanabilirdi.
    /// </summary>
    [Fact]
    public async Task Limit_MEVCUT_BAKIYE_ile_birlikte_degerlendirilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Limit birikim", stokAdet: 10m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.KrediLimitiTl = 5_000m;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 30), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 30), k.Islem));
        Assert.Contains("Kredi limiti", ex.Message);
    }

    /// <summary>
    /// ALIŞ bizim borcumuzdur; müşteriye tanıdığımız kredi limiti kendi
    /// borcumuzu sınırlamaz. Sınırlasaydı limitli bir tedarikçiden mal almak
    /// imkânsız hale gelirdi.
    /// </summary>
    [Fact]
    public async Task Alista_kredi_limiti_UYGULANMAZ_ama_vade_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Alış vade", stokAdet: 0m);
        using (var ayar = vt.Olustur(k.TenantId))
        {
            var c = await ayar.Cariler.SingleAsync(x => x.Id == k.CariId);
            c.KrediLimitiTl = 100m;
            await ayar.SaveChangesAsync();
        }

        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 9_000m,
            OdemeTuru = "HAVALE", OdenenTl = 0m, KasaId = k.KasaId,
            KurSnapshotId = k.KurId, VadeGun = 14,
        }, k.Islem);

        var h = SonHareket(k, "ALIS");
        Assert.Equal(Zaman.YerelBugun().AddDays(14), h.VadeTarihi);
        Assert.Equal(9_000m, h.VeresiyeTutar);
    }

    // ── Açık borç türetimi (FIFO) ────────────────────────────────────────────

    /// <summary>
    /// Tahsilat EN ESKİ borçtan mahsup edilir. Aksi halde müşteri ödeme yaptıkça
    /// en eski (ve en çok gecikmiş) borcu listede kalır, gecikme yapay olarak büyürdü.
    /// </summary>
    [Fact]
    public async Task Tahsilat_EN_ESKI_borctan_mahsup_edilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "FIFO mahsup", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);   // eski
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 2_000m, vadeGun: 60), k.Islem);   // yeni

        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 3_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Equal(0m, vadesiz);
        var tek = Assert.Single(borclar);
        Assert.Equal(2_000m, tek.AcikTutar);                          // kalan YENİ borç
        Assert.Equal(Zaman.YerelBugun().AddDays(60), tek.Vade);
    }

    [Fact]
    public async Task Kismi_tahsilat_en_eski_borcu_KISMEN_kapatir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "FIFO kısmi", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 2_000m, vadeGun: 60), k.Islem);

        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 1_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (borclar, _) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Equal(2, borclar.Count);
        Assert.Equal(2_000m, borclar[0].AcikTutar);   // eski borcun kalanı
        Assert.Equal(2_000m, borclar[1].AcikTutar);
    }

    /// <summary>
    /// Vadesi BİLİNMEYEN bakiye (bu alan eklenmeden önceki kayıtlar) ayrı
    /// döner — bugüne yazılıp "gecikti" sayılmaz. Sayılsaydı sistem, hakkında
    /// hiçbir vade sözü verilmemiş her eski borcu gecikmiş ilan ederdi.
    /// </summary>
    [Fact]
    public async Task Vadesi_bilinmeyen_bakiye_AYRI_raporlanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vadesiz bakiye", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);

        // Eski kayıt taklidi: vade sütunu doldurulmadan bakiye şişirilir.
        using (var eski = vt.OlusturSistem())
        {
            var b = await eski.CariBakiyeler.SingleAsync(x => x.CariId == k.CariId);
            b.TlBakiye += 1_500m;
            await eski.SaveChangesAsync();
        }

        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Equal(3_000m, Assert.Single(borclar).AcikTutar);
        Assert.Equal(1_500m, vadesiz);
    }

    [Fact]
    public async Task Borcu_kapanan_cari_listede_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Borç kapandı", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);
        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 3_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Empty(borclar);
        Assert.Equal(0m, vadesiz);
    }

    // ── Gecikme hesabı ───────────────────────────────────────────────────────

    [Fact]
    public async Task Vadesi_gecen_borc_GECIKTI_sayilir_vadesi_gelmeyen_sayilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gecikme", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 0), k.Islem);

        using var kontrol = vt.OlusturSistem();

        // Bugün: vadesi BUGÜN olan borç henüz gecikmemiştir (gecikme = 0).
        var (bugun, _) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(0, Assert.Single(bugun).GecikmeGun);

        // 5 gün sonra bakıldığında 5 gün gecikmiştir.
        var (sonra, _) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow.AddDays(5));
        Assert.Equal(5, Assert.Single(sonra).GecikmeGun);
    }

    // ── Rapor ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rapor_geciken_ve_yaklasan_borcu_AYIRIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade raporu", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        // Vadesi bugün olan borç (gecikme 0 → yaklaşan), ufuk 7 gün.
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 0), k.Islem);
        // Vadesi 90 gün sonra — ufkun dışında, hiçbir toplama girmez.
        await Satis(db).Satis(VeresiyeSatis(k, 1m, 2_000m, vadeGun: 90), k.Islem);

        using var kontrol = vt.Olustur(k.TenantId);
        var yanit = await Rapor(kontrol).VadeTakibi(k.Islem, ufukGun: 7);
        var tip = yanit.GetType();

        Assert.Equal(0m, (decimal)tip.GetProperty("gecikenToplam")!.GetValue(yanit)!);
        Assert.Equal(3_000m, (decimal)tip.GetProperty("yaklasanToplam")!.GetValue(yanit)!);
        Assert.Equal(0m, (decimal)tip.GetProperty("vadesizToplam")!.GetValue(yanit)!);
    }

    [Fact]
    public async Task Rapor_bakiyesiz_cariyi_LISTELEMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Vade boş rapor", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 3_000m,
            OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.Olustur(k.TenantId);
        var yanit = await Rapor(kontrol).VadeTakibi(k.Islem, ufukGun: 7);
        var cariler = (System.Collections.IList)yanit.GetType()
            .GetProperty("cariler")!.GetValue(yanit)!;

        Assert.Empty(cariler);
    }

    /// <summary>
    /// Tedarikçiye olan BİZİM borcumuz, "kimden alacağım var" raporunda YER ALMAZ.
    /// Alsaydı kasiyer, para vermesi gereken tedarikçiyi tahsilat için arardı.
    /// </summary>
    [Fact]
    public async Task Rapor_BIZIM_tedarikciye_borcumuzu_LISTELEMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tedarikçi borcu", stokAdet: 0m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 9_000m,
            OdemeTuru = "HAVALE", OdenenTl = 0m, KasaId = k.KasaId,
            KurSnapshotId = k.KurId, VadeGun = 14,
        }, k.Islem);

        using var kontrol = vt.Olustur(k.TenantId);
        var yanit = await Rapor(kontrol).VadeTakibi(k.Islem, ufukGun: 30);
        var cariler = (System.Collections.IList)yanit.GetType()
            .GetProperty("cariler")!.GetValue(yanit)!;

        Assert.Empty(cariler);
    }

    // ── Fabrika değişmezi ────────────────────────────────────────────────

    /// <summary>
    /// Veresiye payı OLMAYAN harekete vade yazılamaz. Yazılabilseydi peşin bir
    /// satış vade takibi raporunda "ödenmemiş borç" olarak görünürdü.
    /// </summary>
    [Fact]
    public void Veresiye_payi_olmayan_hareket_vade_TASIYAMAZ()
    {
        using var db = vt.OlusturSistem();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            HareketFabrikasi.Ekle(db, new OdemeIzi(0m, 0m, Zaman.YerelBugun()),
                new Hareket { Tur = "SATIS", TlTutari = 100m }));
        Assert.Contains("Vade yalnız veresiye", ex.Message);
    }

    [Fact]
    public void Taslakta_doldurulan_vade_REDDEDILIR()
    {
        using var db = vt.OlusturSistem();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            HareketFabrikasi.Ekle(db, OdemeIzi.Veresiyeden(100m, Zaman.YerelBugun()),
                new Hareket { Tur = "SATIS", TlTutari = 100m, VadeTarihi = Zaman.YerelBugun() }));
        Assert.Contains("VadeTarihi taslakta doldurulmaz", ex.Message);
    }

    [Fact]
    public void Negatif_vade_gunu_REDDEDILIR()
    {
        var cari = new Kuyumcu.Api.Data.Cari { Id = "c", TenantId = "t", Ad = "x", Tur = "MUSTERI" };
        Assert.Throws<BadRequestException>(() => VadeKapisi.VadeHesapla(cari, -1, DateTime.UtcNow));
    }
    // ── Kapanmış borcun geri açılmaması (canlı doğrulamada bulunan kusur) ────

    /// <summary>
    /// ⚠ BU TESTİN VARLIK SEBEBİ — CANLI DOĞRULAMA (2026-09-15): ilk tasarım
    /// bakiyeyi vadeli satırlara dağıtıyordu. Kapanmış bir borç aday havuzunda
    /// kaldığı için, SONRADAN doğan vadesiz bir bakiye artışı çoktan ödenmiş
    /// ESKİ borca yazılıyor ve müşteri hiç gecikmediği hâlde "12 gün gecikti"
    /// görünüyordu. Defteri yürüyen algoritma bu kapanmayı hatırlar.
    /// </summary>
    [Fact]
    public async Task Odenmis_borc_sonraki_vadesiz_bakiyeyi_UZERINE_ALMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kapanan borç", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);
        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 3_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        // Borç kapandıktan SONRA vadesi bilinmeyen bir bakiye doğuyor
        // (bu alan eklenmeden önce yazılmış kayıt ya da elle düzeltme).
        using (var eski = vt.OlusturSistem())
        {
            var b = await eski.CariBakiyeler.SingleAsync(x => x.CariId == k.CariId);
            b.TlBakiye += 1_500m;
            await eski.SaveChangesAsync();
        }

        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Empty(borclar);          // kapanan borç GERİ AÇILMAZ
        Assert.Equal(1_500m, vadesiz);  // yeni bakiye vadesi bilinmeyen olarak görünür
    }

    /// <summary>
    /// Fazla tahsilat müşteriye ALACAK doğurur: artık BİZİM ona borcumuz vardır.
    /// Bu, vadesi bilinmeyen bir borç olarak döner; müşterinin gecikmiş borcu
    /// olarak DEĞİL. Karışsaydı parasını fazla ödemiş müşteri "borçlu" diye aranırdı.
    /// </summary>
    [Fact]
    public async Task Fazla_tahsilat_acik_borc_BIRAKMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fazla tahsilat", stokAdet: 10m);
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(VeresiyeSatis(k, 1m, 3_000m, vadeGun: 10), k.Islem);
        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 4_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        Assert.Empty(borclar);          // müşterinin GECİKMİŞ borcu yok
        Assert.Equal(1_000m, vadesiz);  // bizim ona 1.000 TL borcumuz var, vadesi yok
    }

}
