using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Evrak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MVade = Kuyumcu.Api.Modules.Vade;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KIYMETLİ EVRAK — çek/senet portföyü (paket 29 Faz A).
//
// ⚠ İKİ AYRI TEST, TEK OLGU DEĞİL: "bakiye doğru" ile "vade dağılımı doğru"
//   AYRI ölçülür. Evrak girişinde ödeme izi yanlış verilirse bakiye yine
//   doğru çıkar ama müşteri kapattığı borç yüzünden "gecikmiş" görünmeye
//   devam eder — bakiye testi bunu YAKALAMAZ (M29-03).
//
// ⚠ KÜLTÜR: bu dosya tarih/sayı biçimlendirmesi YAPMAZ; karşılaştırmalar
//   DateOnly ve decimal üzerinden gider (tr-TR / invariant tuzağı kapalı).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class EvrakTestleri(TestVeritabani vt)
{
    private static EvrakServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<EvrakServisi>.Instance, TestKurulum.Denetim(db),
            Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan());

    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static EvrakGirisDto Giris(Kiraci k, decimal tutar, string no = "CEK-001",
        string tur = "CEK", string? banka = "Ziraat", int vadeGun = 30) => new()
    {
        EvrakTuru = tur,
        Tutar = tutar,
        VadeGunu = Bugun.AddDays(vadeGun),
        EvrakNo = no,
        BankaAdi = banka,
        KaynakCariId = k.CariId,
        KurSnapshotId = k.KurId,
    };

    /// <summary>Müşteriye önce veresiye borç yazar (evrak o borcu kapatacak).</summary>
    private static async Task VeresiyeYaz(KuyumcuDbContext db, Kiraci k, decimal tutar, int vadeGun = 30)
    {
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
            db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
            TestKurulum.Denetim(db));
        await cari.VeresiyeGir(k.CariId,
            new Kuyumcu.Api.Modules.Cari.VeresiyeGirisDto
            {
                TlTutari = tutar, KurSnapshotId = k.KurId, VadeGun = vadeGun,
            }, k.Islem);
    }

    private static decimal Bakiye(KuyumcuDbContext db, Kiraci k) =>
        db.CariBakiyeler.AsNoTracking()
            .Where(b => b.CariId == k.CariId && b.TenantId == k.TenantId)
            .Select(b => b.TlBakiye).FirstOrDefault();

    // ── Giriş: borç kapatır ──────────────────────────────────────────────────

    [Fact]
    public async Task Giris_cari_bakiyesini_azaltir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-giris");
        using var db = vt.Olustur(k);
        await VeresiyeYaz(db, k, 50_000m);
        Assert.Equal(50_000m, Bakiye(db, k));

        var sonuc = await Servis(db).Gir(Giris(k, 30_000m), k.Islem);

        Assert.Equal("PORTFOYDE", sonuc.Evrak.Durum);
        Assert.Equal(20_000m, sonuc.CariTlBakiye);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_GIRIS");
        Assert.Equal(-30_000m, h.TlTutari);           // borç KAPATAN yön
        Assert.Equal(30_000m, h.VeresiyeTutar);       // veresiye payı DOLU
        Assert.Equal(0m, h.NakitTutar);               // nakit el değiştirmedi
        Assert.Null(h.VadeTarihi);                    // borç doğurmaz → vade YOK
    }

    [Fact]
    public async Task Giris_sonrasi_acik_borc_duser()
    {
        // ⚠ Bakiye testinden AYRI ve TOPLAM DEĞİL, VADE DAĞILIMI ölçülür.
        //   NEDEN: yalnız toplam ölçülseydi ödeme izi hiç verilmese de (M29-03)
        //   toplam 20.000 çıkardı — AcikBorclar bakiyeyi tavan kabul eder.
        //   Ayrımı görmek için İKİ FARKLI VADELİ parti kurulur: evrak girişi
        //   FIFO ile ESKİ partiyi tüketmeli, geriye YENİ vadeli borç kalmalıdır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-vade");
        using var db = vt.Olustur(k);
        await VeresiyeYaz(db, k, 20_000m, vadeGun: 10);   // eski parti
        await VeresiyeYaz(db, k, 30_000m, vadeGun: 60);   // yeni parti

        var (oncekiBorclar, _) = await MVade.VadeKapisi.AcikBorclar(db, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(50_000m, oncekiBorclar.Sum(b => b.AcikTutar));
        Assert.Equal(2, oncekiBorclar.Count);

        await Servis(db).Gir(Giris(k, 30_000m), k.Islem);

        using var kontrol = vt.Olustur(k);
        var (borclar, vadesiz) = await MVade.VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(20_000m, borclar.Sum(b => b.AcikTutar) + vadesiz);
        // 'Vadesi bilinmeyen' kalemi ARTMAMALI: giriş vadesiz BORÇ yazmaz.
        Assert.Equal(0m, vadesiz);
        // ⚠ AYIRT EDİCİ ÖLÇÜM: 20.000'lik ESKİ parti TÜKENMİŞ olmalı; kalan
        //   borcun tamamı YENİ vadeye (+60 gün) ait. İz verilmezse eski parti
        //   açık kalır ve burası +10 günü görür.
        var kalan = Assert.Single(borclar);
        Assert.Equal(Bugun.AddDays(60), kalan.Vade);
        Assert.Equal(20_000m, kalan.AcikTutar);
    }

    [Fact]
    public async Task Vadesi_gecmis_tarihle_giris_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-gecmisvade");
        using var db = vt.Olustur(k);
        var dto = Giris(k, 1_000m);
        dto.VadeGunu = Bugun.AddDays(-1);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Gir(dto, k.Islem));
    }

    [Fact]
    public async Task TRY_disi_evrak_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-doviz");
        using var db = vt.Olustur(k);
        var dto = Giris(k, 1_000m);
        dto.ParaBirimi = "USD";
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Gir(dto, k.Islem));
    }

    [Fact]
    public async Task Sube_yoksa_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-sube");
        using var db = vt.Olustur(k);
        var subesiz = k.Islem with { SubeId = null };
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Gir(Giris(k, 1_000m), subesiz));
    }

    // ── Çift giriş ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Ayni_banka_ve_cek_no_iki_kez_girilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-cift-cek");
        using var db = vt.Olustur(k);
        await Servis(db).Gir(Giris(k, 5_000m, "CEK-777", "CEK", "Ziraat"), k.Islem);
        await Assert.ThrowsAsync<ConflictException>(
            () => Servis(db).Gir(Giris(k, 5_000m, "CEK-777", "CEK", "Ziraat"), k.Islem));
    }

    [Fact]
    public async Task Ayni_senet_iki_kez_girilemez()
    {
        // ⚠ MySQL UNIQUE indeksi ÇOKLU NULL'a izin verir; senette banka_adi
        //   NULL'dur. Koruma, asla null olmayan ayırt edici kolon + servis içi
        //   elle kontroldür (M29-07 bu iki katmanı da ölçer).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-cift-senet");
        using var db = vt.Olustur(k);
        await Servis(db).Gir(Giris(k, 5_000m, "SNT-1", "SENET", banka: null), k.Islem);
        await Assert.ThrowsAsync<ConflictException>(
            () => Servis(db).Gir(Giris(k, 5_000m, "SNT-1", "SENET", banka: null), k.Islem));
    }

    // ── Durum makinesi ───────────────────────────────────────────────────────

    [Fact]
    public async Task Gecersiz_gecis_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-gecis");
        using var db = vt.Olustur(k);
        var e = await Servis(db).Gir(Giris(k, 4_000m), k.Islem);

        // TAHSIL_EDILDI son durumdur → tahsile verilemez.
        await Servis(db).TahsilEdildi(e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).TahsileVer(
            e.Evrak.Id, new TahsileVerDto { BankaHesapId = "x" }, k.Islem));
    }

    [Fact]
    public async Task Ikinci_kez_tahsile_verilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-iki-tahsile");
        using var db = vt.Olustur(k);
        var hesapId = BankaHesabiKur(db, k);
        var e = await Servis(db).Gir(Giris(k, 4_000m), k.Islem);
        await Servis(db).TahsileVer(e.Evrak.Id, new TahsileVerDto { BankaHesapId = hesapId }, k.Islem);

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).TahsileVer(
            e.Evrak.Id, new TahsileVerDto { BankaHesapId = hesapId }, k.Islem));
    }

    // ── Karşılıksız ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Karsiliksiz_borcu_geri_dogurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-karsiliksiz");
        using var db = vt.Olustur(k);
        await VeresiyeYaz(db, k, 50_000m);
        var e = await Servis(db).Gir(Giris(k, 30_000m), k.Islem);
        Assert.Equal(20_000m, e.CariTlBakiye);

        var sonuc = await Servis(db).Karsiliksiz(e.Evrak.Id,
            new KarsiliksizDto
            {
                Aciklama = "Banka karşılıksız işlemi bildirdi, müşteri arandı.",
                KurSnapshotId = k.KurId,
            }, k.Islem);

        Assert.Equal("KARSILIKSIZ", sonuc.Evrak.Durum);
        Assert.Equal(50_000m, sonuc.CariTlBakiye);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_KARSILIKSIZ");
        Assert.Equal(30_000m, h.TlTutari);           // borç DOĞURAN yön
        Assert.Equal(30_000m, h.VeresiyeTutar);
        Assert.NotNull(h.VadeTarihi);                // borç doğurdu → vade DOLU
    }

    [Fact]
    public async Task Karsiliksiz_kredi_limitini_asamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-limit");
        using var db = vt.Olustur(k);
        await VeresiyeYaz(db, k, 50_000m);
        var e = await Servis(db).Gir(Giris(k, 30_000m), k.Islem);

        // Limit, evrak girişinden SONRAKİ bakiyenin (20.000) hemen üstüne çekilir:
        // borç geri doğunca 50.000 olacak ve limiti aşacak.
        var cari = db.Cariler.Single(c => c.Id == k.CariId);
        cari.KrediLimitiTl = 25_000m;
        await db.SaveChangesAsync();

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).Karsiliksiz(
            e.Evrak.Id,
            new KarsiliksizDto
            {
                Aciklama = "Karşılıksız çıktı, limit denetimi bekleniyor.",
                KurSnapshotId = k.KurId,
            }, k.Islem));
    }

    [Fact]
    public async Task Karsiliksiz_gerekcesi_20_karakterden_kisa_olamaz()
    {
        // DataAnnotations yalnız controller yolunda çalışır; servis içi kapı bu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-gerekce");
        using var db = vt.Olustur(k);
        var e = await Servis(db).Gir(Giris(k, 3_000m), k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Karsiliksiz(
            e.Evrak.Id, new KarsiliksizDto { Aciklama = "kısa", KurSnapshotId = k.KurId }, k.Islem));
    }

    // ── Tahsil ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Nakit_tahsil_kasa_bakiyesini_artirir_ve_borcu_IKINCI_KEZ_kapatmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-nakit-tahsil");
        using var db = vt.Olustur(k);
        // PAKET YASAL-2 (TB-15): tek kaynak 50.000 TL olsaydı (haddi aşan işlem) senedin
        // nakde çevrilmesi büyük işlemin taksitini nakit almak olurdu ve kapı 400 verirdi
        // (TevsikTaksitDbTestleri.Evrak_buyuk_veresiyeden_senedin_NAKIT_tahsili_400).
        // Bu testin konusu kasa/çift sayma olduğu için borç iki had-altı kaynağa bölündü.
        await VeresiyeYaz(db, k, 25_000m);
        await VeresiyeYaz(db, k, 25_000m);
        var e = await Servis(db).Gir(Giris(k, 30_000m), k.Islem);

        var sonuc = await Servis(db).TahsilEdildi(e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);

        Assert.Equal("TAHSIL_EDILDI", sonuc.Evrak.Durum);
        // ⚠ ÇİFT SAYMA YASAĞI: borç girişte kapandı; tahsil onu tekrar kapatmaz.
        Assert.Equal(20_000m, sonuc.CariTlBakiye);

        using var kontrol = vt.OlusturSistem();
        var kasa = kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId);
        Assert.Equal(TestKurulum.KasaAcilis + 30_000m, kasa.Bakiye);
        Assert.Single(kontrol.KasaHareketleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Tutar == 30_000m));

        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "TAHSILAT");
        Assert.Equal(30_000m, h.NakitTutar);
        Assert.Equal(0m, h.VeresiyeTutar);   // veresiye payı 0 → VadeKapisi etkisi 0
    }

    [Fact]
    public async Task Kapanmis_gune_tahsil_yazilamaz()
    {
        // ⚠ İKİ KASA / İKİ GÜN fikstürü: kapanan gün kilidi KasaDogrula'ya
        //   verilen `an` parametresiyle çalışır. `an` düşerse bu test kırmızıdır
        //   (M29-05) — gün kilidi başka hiçbir katmanda tekrarlanmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-gun-kilidi");
        using var db = vt.Olustur(k);

        var acikKasaId = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = acikKasaId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Açık kasa",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var e1 = await Servis(db).Gir(Giris(k, 1_000m, "CEK-A"), k.Islem);
        var e2 = await Servis(db).Gir(Giris(k, 2_000m, "CEK-B"), k.Islem);

        // k.KasaId'nin BUGÜNÜ kapatılır; acikKasaId açık kalır.
        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).TahsilEdildi(
            e1.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem));

        // Açık kasaya YAZILABİLİR — test "her tahsil 400" demiyor, KİLİDİ ölçüyor.
        using var db3 = vt.Olustur(k);
        var ok = await Servis(db3).TahsilEdildi(e2.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = acikKasaId, KurSnapshotId = k.KurId }, k.Islem);
        Assert.Equal("TAHSIL_EDILDI", ok.Evrak.Durum);
    }

    [Fact]
    public async Task Nakit_tahsil_USD_kasasina_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-usd-kasa", kasaDoviz: "USD");
        using var db = vt.Olustur(k);
        var e = await Servis(db).Gir(Giris(k, 1_000m), k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).TahsilEdildi(
            e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem));
    }

    [Fact]
    public async Task Tevsik_haddi_asan_nakit_tahsil_400()
    {
        // 459 s. VUK — 30.000 TL nakit haddi. Kapı transaction'dan ÖNCE koşar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-tevsik");
        using var db = vt.Olustur(k);
        var e = await Servis(db).Gir(Giris(k, 120_000m), k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).TahsilEdildi(
            e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem));

        // Fail-closed: durum DEĞİŞMEMİŞ olmalı (transaction hiç açılmadı).
        using var kontrol = vt.OlusturSistem();
        var evrak = kontrol.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == e.Evrak.Id);
        Assert.Equal("PORTFOYDE", evrak.Durum);
    }

    [Fact]
    public async Task Banka_tahsili_hesap_bakiyesini_artirir_ve_GELEN_hareket_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-banka-tahsil");
        using var db = vt.Olustur(k);
        var hesapId = BankaHesabiKur(db, k);
        var e = await Servis(db).Gir(Giris(k, 40_000m), k.Islem);

        await Servis(db).TahsilEdildi(e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "BANKA", BankaHesapId = hesapId, KurSnapshotId = k.KurId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var hesap = kontrol.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesapId);
        Assert.Equal(40_000m, hesap.Bakiye);
        var bh = kontrol.BankaHareketleri.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.BankaHesapId == hesapId);
        Assert.Equal("GELEN", bh.Yon);
        Assert.Equal(40_000m, bh.Tutar);
        // M7a: borç evrak GİRİŞİNDE düşüldü → tahsil satırı "borcundan düşülsün mü?" listesine DÜŞMEZ (çift düşme yok).
        Assert.Equal(Kuyumcu.Api.Modules.Banka.CariKarari.Kapali, bh.CariKarari);
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_evraki_gorulemez_ve_degistirilemez()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-kiraci-A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-kiraci-B");

        using var dbA = vt.Olustur(a);
        var e = await Servis(dbA).Gir(Giris(a, 5_000m, "CEK-IZOLE"), a.Islem);

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbB).Getir(e.Evrak.Id, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbB).PortfoyeAl(e.Evrak.Id, b.Islem));
    }

    [Fact]
    public async Task Baska_kiracinin_banka_hesabiyla_tahsile_verilemez()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-hesap-A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-hesap-B");

        using var dbB = vt.Olustur(b);
        var yabanciHesap = BankaHesabiKur(dbB, b);

        using var dbA = vt.Olustur(a);
        var e = await Servis(dbA).Gir(Giris(a, 5_000m, "CEK-HESAP"), a.Islem);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbA).TahsileVer(
            e.Evrak.Id, new TahsileVerDto { BankaHesapId = yabanciHesap }, a.Islem));
    }

    // ── Alan şifreleme ───────────────────────────────────────────────────────

    [Fact]
    public async Task Kimlik_no_diske_sifreli_iner()
    {
        const string Tckn = "12345678950";
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-sifre");
        using var db = vt.Olustur(k);
        var dto = Giris(k, 9_000m, "CEK-SIFRE");
        dto.BorcluKimlikNo = Tckn;
        var e = await Servis(db).Gir(dto, k.Islem);

        // HAM SQL — EF converter'ını ATLAYARAK diskteki gerçek değeri oku.
        using var kontrol = vt.OlusturSistem();
        var ham = await kontrol.Database.SqlQuery<string>(
            $"SELECT borclu_kimlik_no AS Value FROM kiymetli_evraklar WHERE id = {e.Evrak.Id}").SingleAsync();

        Assert.NotEqual(Tckn, ham);
        Assert.DoesNotContain(Tckn, ham);
        Assert.StartsWith("v1:", ham);

        // Bellekte (EF üzerinden) DÜZ metin görünür.
        using var okuma = vt.Olustur(k);
        var varlik = okuma.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == e.Evrak.Id);
        Assert.Equal(Tckn, varlik.BorcluKimlikNo);
    }

    [Fact]
    public async Task Hash_ile_arama_bulur_duz_metinle_bulmaz()
    {
        const string Tckn = "98765432228";
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-hash-ara");
        using var db = vt.Olustur(k);
        var dto = Giris(k, 7_000m, "CEK-HASH");
        dto.BorcluKimlikNo = Tckn;
        await Servis(db).Gir(dto, k.Islem);

        using var okuma = vt.Olustur(k);
        var bulunan = await Servis(okuma).KimlikIleAra(Tckn, k.Islem);
        Assert.Single(bulunan.Kalemler);

        // Şifreli sütunda DÜZ METİN araması hiçbir şey bulmaz (ham SQL).
        using var kontrol = vt.OlusturSistem();
        var duzEslesme = await kontrol.Database.SqlQuery<int>(
            $"SELECT COUNT(*) AS Value FROM kiymetli_evraklar WHERE borclu_kimlik_no = {Tckn}").SingleAsync();
        Assert.Equal(0, duzEslesme);
    }

    // ── Liste ve takvim ──────────────────────────────────────────────────────

    [Fact]
    public async Task Limit_200_uzeri_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-limit-kiskaci");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Listele(
            k.Islem, null, null, null, null, null, null, limit: 5000, offset: 0));
    }

    [Fact]
    public async Task Takvim_gun_basina_toplam_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-takvim");
        using var db = vt.Olustur(k);
        await Servis(db).Gir(Giris(k, 1_000m, "CEK-T1", vadeGun: 5), k.Islem);
        await Servis(db).Gir(Giris(k, 2_000m, "CEK-T2", vadeGun: 5), k.Islem);
        await Servis(db).Gir(Giris(k, 4_000m, "CEK-T3", vadeGun: 9), k.Islem);

        using var okuma = vt.Olustur(k);
        var t = await Servis(okuma).Takvim(k.Islem, Bugun, Bugun.AddDays(30), null);

        Assert.Equal(2, t.Kalemler.Count);        // SATIR değil, GÜN döner
        var besinciGun = t.Kalemler.Single(g => g.Gun == Bugun.AddDays(5));
        Assert.Equal(2m, besinciGun.Adet);
        Assert.Equal(3_000m, besinciGun.Tutar);
        Assert.Equal(7_000m, t.ToplamTutar);
    }

    [Fact]
    public async Task Takvim_366_gunden_uzun_araligi_reddeder()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-takvim-aralik");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Takvim(
            k.Islem, Bugun, Bugun.AddDays(400), null));
    }

    // ── Düzeltme (UX-S25) ────────────────────────────────────────────────────

    [Fact]
    public async Task Gonderilmeyen_alan_KORUNUR_null_lanmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-patch");
        using var db = vt.Olustur(k);
        var dto = Giris(k, 6_000m, "CEK-PATCH");
        dto.Notlar = "ilk not";
        dto.BorcluAd = "Ahmet Yılmaz";
        var e = await Servis(db).Gir(dto, k.Islem);

        // YALNIZ evrak no gönderilir; diğer alanlar gövdede YOKTUR.
        using var db2 = vt.Olustur(k);
        await Servis(db2).Duzelt(e.Evrak.Id, new EvrakDuzeltDto { EvrakNo = "CEK-PATCH-2" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var v = kontrol.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == e.Evrak.Id);
        Assert.Equal("CEK-PATCH-2", v.EvrakNo);
        Assert.Equal("ilk not", v.Notlar);          // KORUNDU
        Assert.Equal("Ahmet Yılmaz", v.BorcluAd);   // KORUNDU
        Assert.Equal(6_000m, v.Tutar);              // düzeltilemez alan dokunulmadı
    }

    [Fact]
    public async Task Portfoyde_olmayan_evrak_duzeltilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-patch-kilit");
        using var db = vt.Olustur(k);
        var e = await Servis(db).Gir(Giris(k, 2_500m, "CEK-KILIT"), k.Islem);
        await Servis(db).TahsilEdildi(e.Evrak.Id,
            new TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).Duzelt(
            e.Evrak.Id, new EvrakDuzeltDto { EvrakNo = "CEK-YENI" }, k.Islem));
    }

    // ── Denetim izi ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Her_durum_degisimi_denetim_kaydi_birakir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "EVRAK-denetim");
        using var db = vt.Olustur(k);
        var hesapId = BankaHesabiKur(db, k);
        var e = await Servis(db).Gir(Giris(k, 3_500m, "CEK-DEN"), k.Islem);
        await Servis(db).TahsileVer(e.Evrak.Id, new TahsileVerDto { BankaHesapId = hesapId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kayitlar = kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "KiymetliEvrak").ToList();
        Assert.True(kayitlar.Count >= 2, $"En az 2 denetim kaydı bekleniyordu, {kayitlar.Count} bulundu.");
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private static string BankaHesabiKur(KuyumcuDbContext db, Kiraci k)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.BankaHesaplari.Add(new BankaHesap
        {
            Id = id, TenantId = k.TenantId, BankaAdi = "Ziraat", HesapAdi = "Vadesiz",
            Iban = TestIban.Tamamla(new string('1', 22)), ParaBirimi = "TRY", Bakiye = 0m,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }
}
