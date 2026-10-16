using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-21 — AVANS (Y-17) · SİPARİŞ→SATIŞ (Y-18) · DEĞİŞİM (Y-19) ·
//              KONSİNYE ÇIKIŞ/İADE (Y-20) · REZERV/BLOKE (Y-23)   [gerçek MySQL]
//
// HER TEST BİR İŞ KURALINI YENİDEN ÜRETİR:
//   • Bloke adet satılabilir stoktan düşer, ADETTEN düşmez (sayım bozulmaz).
//   • bloke_adet > adet İMKÂNSIZDIR (BLOKE_TUTARSIZ = 0).
//   • Avans satışa mahsup edilince cari bakiye ÇİFT SAYMAZ.
//   • Sipariş → satış TEK transaction; yarım dönüşüm imkânsız.
//   • Kapanmış (terminal) kayıt deftere yazdıramaz (TERMINAL_SONRASI_YAZAN_YOL = 0).
//   • Değişim iki bacağı TEK fişte yazar ve iade hakkını tüketir (çift iade yok).
//   • Konsinye çıkış mülkiyet devri değildir: TL bacağı 0, cari bakiye sabit.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P21TicaretTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance);

    private static Stok StokOku(KuyumcuDbContext db, Kiraci k) =>
        db.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId && s.UrunId == k.UrunId);

    private static string Alan(object cevap, string ad) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cevap))
            .RootElement.GetProperty(ad).GetString()!;

    private static decimal Sayi(object cevap, string ad) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cevap))
            .RootElement.GetProperty(ad).GetDecimal();

    /// <summary>İkinci bir cari (ciro / konsinye karşı tarafı).</summary>
    private static string CariEkle(KuyumcuDbContext sistem, Kiraci k, string ad = "Karşı cari")
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        sistem.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "TEDARIKCI", Ad = ad,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        sistem.CariBakiyeler.Add(new CariBakiye
        {
            CariId = id, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        sistem.SaveChanges();
        return id;
    }

    // ═══════════ Y-23 · REZERVASYON / BLOKE ADET ═════════════════════════════

    [Fact]
    public async Task Rezervasyon_blokeyi_artirir_ADEDI_DEGISTIRMEZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rez-1", stokAdet: 10m);
        using var db = vt.Olustur(k);

        var cevap = await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 3m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var stok = StokOku(kontrol, k);
        Assert.Equal(10m, stok.Adet);          // FİZİKSEL adet değişmez — sayım bozulmaz
        Assert.Equal(3m, stok.BlokeAdet);
        Assert.Equal(7m, BlokeKapisi.Satilabilir(stok));
        Assert.Equal(7m, Sayi(cevap, "satilabilirAdet"));
    }

    [Fact]
    public async Task Rezerve_mal_SATILAMAZ_satilabilir_stok_kapisi_calisir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rez-2", stokAdet: 5m);
        using var db = vt.Olustur(k);

        await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 4m }, k.Islem);

        // 5 adet var, 4'ü rezerve → 2 adet satılamaz.
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(
            new MSatis.SatisOlusturDto
            {
                UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("satılabilir", ex.Message);
        Assert.Contains("rezerve 4", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Equal(5m, StokOku(kontrol, k).Adet);
    }

    [Fact]
    public async Task Bloke_adet_stok_adedini_ASAMAZ_BLOKE_TUTARSIZ_sifir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rez-3", stokAdet: 2m);
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 3m }, k.Islem));
        Assert.Contains("Bloke adet stok adedini aşamaz", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, StokOku(kontrol, k).BlokeAdet);
        Assert.Empty(kontrol.Rezervasyonlar.Where(r => r.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Rezervasyon_iptali_blokeyi_birakir_ikinci_iptal_409_doner()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rez-4", stokAdet: 10m);
        using var db = vt.Olustur(k);

        var r = await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 3m }, k.Islem);
        var id = Alan(r, "id");

        await Satis(db).RezervIptal(id, new MSatis.RezervIptalDto
        {
            Gerekce = "Müşteri vazgeçti, mal vitrine döndü",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, StokOku(kontrol, k).BlokeAdet);
        Assert.Equal(RezervasyonDurumu.Iptal,
            kontrol.Rezervasyonlar.Single(x => x.Id == id).Durum);

        // TERMINAL_SONRASI_YAZAN_YOL: ikinci iptal blokeyi TEKRAR düşüremez.
        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<ConflictException>(() => Satis(db2).RezervIptal(id,
            new MSatis.RezervIptalDto { Gerekce = "İkinci kez iptal denemesi" }, k.Islem));
    }

    [Fact]
    public async Task Rezervasyon_iptal_gerekcesi_servis_icinde_de_zorunludur()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rez-5", stokAdet: 4m);
        using var db = vt.Olustur(k);

        var r = await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 1m }, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).RezervIptal(
            Alan(r, "id"), new MSatis.RezervIptalDto { Gerekce = "kısa" }, k.Islem));
        Assert.Contains("en az 10 karakter", ex.Message);
    }

    [Fact]
    public async Task Stok_raporu_blokeyi_AYRI_gosterir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rapor", stokAdet: 8m);
        using var db = vt.Olustur(k);

        await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 3m }, k.Islem);

        using var okuma = vt.Olustur(k);
        var rapor = await TestKurulum.RaporServisi(okuma).StokDegeri(k.Islem, null, 50, 0);
        var kok = System.Text.Json.JsonDocument
            .Parse(System.Text.Json.JsonSerializer.Serialize(rapor)).RootElement;
        var satir = kok.GetProperty("urunler")[0];

        Assert.Equal(8m, satir.GetProperty("adet").GetDecimal());
        Assert.Equal(3m, satir.GetProperty("blokeAdet").GetDecimal());
        Assert.Equal(5m, satir.GetProperty("satilabilirAdet").GetDecimal());
        Assert.Equal(3m, kok.GetProperty("toplamBlokeAdet").GetDecimal());
    }

    // ═══════════ Y-17 · AVANS ════════════════════════════════════════════════

    [Fact]
    public async Task Avans_kasaya_girer_ve_musteri_ALACAKLI_olur()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-avans-1");
        using var db = vt.Olustur(k);
        var kasaOnce = TestKurulum.KasaBakiye(sistem, k.KasaId);

        await Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = 5_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "AVANS_ALINDI");
        Assert.Equal(5_000m, h.TlTutari);
        Assert.Equal(5_000m, h.NakitTutar);     // 459 s. VUK kümülatifi bu satırı GÖRÜR
        Assert.Equal(0m, h.HasGrami);
        Assert.Equal(kasaOnce + 5_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-5_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    /// <summary>
    /// ⚠ PLANIN DoD MADDESİ: "Avans satışa mahsup edilince cari bakiye ÇİFT SAYMAZ."
    /// Mahsup satırı cari bakiyeye DOKUNMAZ; bakiye yalnız AVANS_ALINDI ile azalmıştır.
    /// </summary>
    [Fact]
    public async Task Avans_mahsubu_cari_bakiyeyi_CIFT_SAYMAZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-avans-2");
        using var db = vt.Olustur(k);

        await Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = 5_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        // Veresiye satış: 12.000 TL borç doğar.
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 12_000m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, VadeGun = 30,
        }, k.Islem);

        using var ara = vt.OlusturSistem();
        var satisId = ara.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;
        var bakiyeMahsupOncesi = TestKurulum.CariBakiye(ara, k.CariId).Tl;
        Assert.Equal(7_000m, bakiyeMahsupOncesi);      // −5.000 avans + 12.000 satış

        using var db2 = vt.Olustur(k);
        var sonuc = await Satis(db2).AvansMahsup(new MSatis.AvansMahsupDto
        {
            SatisHareketId = satisId, Tutar = 5_000m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        // ⚠ BAKİYE DEĞİŞMEDİ — mahsup ikinci kez alacak yazmaz.
        Assert.Equal(bakiyeMahsupOncesi, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        // Havuz eridi.
        Assert.Equal(0m, Sayi(sonuc, "acikAvans"));

        var mahsup = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "AVANS_MAHSUP");
        Assert.Equal(-5_000m, mahsup.TlTutari);
        Assert.Equal(0m, mahsup.NakitTutar);        // para el değiştirmez
        Assert.Equal(0m, mahsup.VeresiyeTutar);
        Assert.Equal(satisId, mahsup.FisId);        // hangi satışa sayıldığı yazılı
    }

    [Fact]
    public async Task Avans_mahsubu_acik_avansi_ASAMAZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-avans-3");
        using var db = vt.Olustur(k);

        await Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = 1_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 9_000m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, VadeGun = 30,
        }, k.Islem);

        using var ara = vt.OlusturSistem();
        var satisId = ara.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db2).AvansMahsup(
            new MSatis.AvansMahsupDto
            {
                SatisHareketId = satisId, Tutar = 5_000m, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("açık avansı aşıyor", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "AVANS_MAHSUP"));
    }

    [Fact]
    public async Task Avans_iadesi_kasadan_cikar_ve_borcumuzu_kapatir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-avans-4");
        using var db = vt.Olustur(k);

        await Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = 2_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var db2 = vt.Olustur(k);
        var sonuc = await Satis(db2).AvansIade(new MSatis.AvansIadeDto
        {
            CariId = k.CariId, Tutar = 2_000m, KasaId = k.KasaId,
            KurSnapshotId = k.KurId, Gerekce = "Müşteri siparişten vazgeçti",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, Sayi(sonuc, "acikAvans"));
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ═══════════ Y-18 · SİPARİŞ → SATIŞ ══════════════════════════════════════

    /// <summary>HAZIR durumuna kadar siparişi ilerletir ve kimliğini döndürür.</summary>
    private static async Task<(string Id, string KalemId)> HazirSiparis(
        KuyumcuDbContext db, Kiraci k)
    {
        var cevap = await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.MusteriSiparisi,
            CariId = k.CariId,
            SozVerilenGun = Zaman.YerelBugun().AddDays(7),
            Kalemler = [new MSiparis.SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }],
        }, k.Islem);
        var id = Alan(cevap, "id");

        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);

        var kalemId = await db.SiparisKalemleri.AsNoTracking()
            .Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();
        return (id, kalemId);
    }

    [Fact]
    public async Task Siparis_satisa_cevrilir_hareketler_siparis_id_tasir_ve_siparis_TESLIM_EDILDI_olur()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-1", stokAdet: 5m);
        using var db = vt.Olustur(k);
        var (id, kalemId) = await HazirSiparis(db, k);

        using var db2 = vt.Olustur(k);
        var sonuc = await Satis(db2).SiparisiSatisaCevir(id, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 10_000m, KasaId = k.KasaId }],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(SiparisDurumu.TeslimEdildi, kontrol.Siparisler.Single(s => s.Id == id).Durum);

        var satis = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
        Assert.Equal(id, satis.SiparisId);        // Y-18 bağı
        Assert.Equal(Alan(sonuc, "fisId"), satis.FisId);
        Assert.Equal(4m, StokOku(kontrol, k).Adet);

        // Append-only geçiş izi yazıldı.
        Assert.Contains(kontrol.SiparisGecisleri.Where(g => g.SiparisId == id),
            g => g.YeniDurum == SiparisDurumu.TeslimEdildi);
    }

    [Fact]
    public async Task Ayni_siparis_IKINCI_kez_satisa_cevrilemez()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-2", stokAdet: 5m);
        using var db = vt.Olustur(k);
        var (id, kalemId) = await HazirSiparis(db, k);

        MSatis.SiparisSatisaCevirDto Dto() => new()
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 10_000m, KasaId = k.KasaId }],
        };

        using var db2 = vt.Olustur(k);
        await Satis(db2).SiparisiSatisaCevir(id, Dto(), k.Islem);

        // 1. kapı: sipariş artık TERMİNAL (TESLIM_EDILDI) — yazan yol kapalı.
        using var db3 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Satis(db3).SiparisiSatisaCevir(id, Dto(), k.Islem));
        Assert.Contains("kapanmıştır", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }

    /// <summary>
    /// ⚠ İKİNCİ KAPI, AYRI TEST: durum makinesi bir şekilde geri alınsa bile
    /// (elle düzeltme, göç, hata) <c>hareketler.siparis_id</c> bağı ikinci
    /// dönüşümü REDDEDER. Tek kapıya güvenilseydi geri alınan bir sipariş aynı
    /// malı ikinci kez satardı.
    /// </summary>
    [Fact]
    public async Task Durum_geri_alinsa_bile_siparis_id_bagi_ikinci_donusumu_reddeder()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-2b", stokAdet: 5m);
        using var db = vt.Olustur(k);
        var (id, kalemId) = await HazirSiparis(db, k);

        MSatis.SiparisSatisaCevirDto Dto() => new()
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 10_000m, KasaId = k.KasaId }],
        };

        using var db2 = vt.Olustur(k);
        await Satis(db2).SiparisiSatisaCevir(id, Dto(), k.Islem);

        // Durumu ELLE geri al (üretimde olmaması gereken hâl).
        using var elle = vt.OlusturSistem();
        elle.Siparisler.Single(x => x.Id == id).Durum = SiparisDurumu.Hazir;
        elle.SaveChanges();

        using var db3 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Satis(db3).SiparisiSatisaCevir(id, Dto(), k.Islem));
        Assert.Contains("zaten satışa çevrilmiş", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }

    /// <summary>
    /// ⚠ PLANIN DoD MADDESİ: "tek transaction; YARIM DÖNÜŞÜM İMKÂNSIZ".
    /// Stok yetmediği için satış bacağı patlar; sipariş durumu ve geçiş kaydı
    /// GERİ ALINIR — sipariş HAZIR kalır.
    /// </summary>
    [Fact]
    public async Task Yarim_donusum_imkansiz_satis_patlarsa_siparis_HAZIR_kalir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-3", stokAdet: 0m);
        using var db = vt.Olustur(k);
        var (id, kalemId) = await HazirSiparis(db, k);

        using var db2 = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db2).SiparisiSatisaCevir(
            id, new MSatis.SiparisSatisaCevirDto
            {
                KurSnapshotId = k.KurId,
                Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
                Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 10_000m, KasaId = k.KasaId }],
            }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(SiparisDurumu.Hazir, kontrol.Siparisler.Single(s => s.Id == id).Durum);
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.DoesNotContain(kontrol.SiparisGecisleri.Where(g => g.SiparisId == id),
            g => g.YeniDurum == SiparisDurumu.TeslimEdildi);
    }

    [Fact]
    public async Task Kalem_fiyati_eksikse_donusum_KISMI_yapilmaz()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-4", stokAdet: 5m);
        using var db = vt.Olustur(k);
        var (id, _) = await HazirSiparis(db, k);

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db2).SiparisiSatisaCevir(
            id, new MSatis.SiparisSatisaCevirDto
            {
                KurSnapshotId = k.KurId,
                Kalemler = [new MSatis.SiparisSatisaCevirKalemDto
                {
                    SiparisKalemId = "OLMAYAN-KALEM", BirimFiyatTl = 10_000m,
                }],
                Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 10_000m, KasaId = k.KasaId }],
            }, k.Islem));
        Assert.Contains("kısmi yapılamaz", ex.Message);
    }

    /// <summary>TERMINAL_SONRASI_YAZAN_YOL = 0 — iptal edilmiş sipariş deftere yazdıramaz.</summary>
    [Fact]
    public async Task Iptal_edilmis_siparis_satisa_cevrilemez()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-5", stokAdet: 5m);
        using var db = vt.Olustur(k);

        var cevap = await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId,
            Kalemler = [new MSiparis.SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }],
        }, k.Islem);
        var id = Alan(cevap, "id");
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto
        {
            YeniDurum = SiparisDurumu.Iptal, Gerekce = "Müşteri vazgeçti",
        }, k.Islem);

        var kalemId = await db.SiparisKalemleri.AsNoTracking()
            .Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Satis(db2).SiparisiSatisaCevir(
            id, new MSatis.SiparisSatisaCevirDto
            {
                KurSnapshotId = k.KurId,
                Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 100m }],
                Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 100m, KasaId = k.KasaId }],
            }, k.Islem));
        Assert.Contains("kapanmıştır", ex.Message);
    }

    /// <summary>Y-17 ∩ Y-18 ∩ Y-23: rezervli + avanslı sipariş tek çağrıda kapanır.</summary>
    [Fact]
    public async Task Donusumde_rezerv_kapanir_avans_mahsup_edilir_ve_bakiye_cift_saymaz()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-sip-6", stokAdet: 5m);
        using var db = vt.Olustur(k);
        var (id, kalemId) = await HazirSiparis(db, k);

        await Satis(db).RezervOlustur(new MSatis.RezervOlusturDto
        {
            UrunId = k.UrunId, CariId = k.CariId, Adet = 1m, SiparisId = id,
        }, k.Islem);
        await Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = 3_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, SiparisId = id,
        }, k.Islem);

        using var db2 = vt.Olustur(k);
        var sonuc = await Satis(db2).SiparisiSatisaCevir(id, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId,
            AvansMahsupTl = 3_000m,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 10_000m }],
        }, k.Islem);

        Assert.Equal(1m, Sayi(sonuc, "kapananRezervSayisi"));
        Assert.Equal(3_000m, Sayi(sonuc, "avansMahsupTl"));
        Assert.Equal(0m, Sayi(sonuc, "acikAvans"));

        using var kontrol = vt.OlusturSistem();
        var stok = StokOku(kontrol, k);
        Assert.Equal(4m, stok.Adet);
        Assert.Equal(0m, stok.BlokeAdet);          // bloke bırakıldı, sızmadı
        Assert.Equal(RezervasyonDurumu.Satildi,
            kontrol.Rezervasyonlar.Single(r => r.SiparisId == id).Durum);

        // −3.000 avans + 10.000 veresiye satış = 7.000 (mahsup ikinci kez yazmaz)
        Assert.Equal(7_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ═══════════ Y-19 · DEĞİŞİM ══════════════════════════════════════════════

    [Fact]
    public async Task Degisim_iki_bacagi_TEK_fiste_yazar_ve_farki_BIR_KEZ_tahsil_eder()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-deg-1", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var ara = vt.OlusturSistem();
        var satisId = ara.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;
        var kasaOnce = TestKurulum.KasaBakiye(ara, k.KasaId);
        var stokOnce = StokOku(ara, k).Adet;   // 9

        using var db2 = vt.Olustur(k);
        var sonuc = await Satis(db2).Degisim(new MSatis.DegisimDto
        {
            KaynakHareketId = satisId, GeriAlinanAdet = 1m,
            YeniUrunId = k.UrunId, YeniAdet = 1m, YeniBirimFiyatTl = 13_000m,
            FarkOdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        Assert.Equal(3_000m, Sayi(sonuc, "farkTl"));

        using var kontrol = vt.OlusturSistem();
        var fisId = Alan(sonuc, "fisId");
        var bacaklar = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId == fisId).ToList();
        Assert.Equal(2, bacaklar.Count);                       // TEK FİŞ, İKİ BACAK
        Assert.Contains(bacaklar, h => h.Tur == "SATIS_IADE" && h.IadeKaynakHareketId == satisId);
        Assert.Contains(bacaklar, h => h.Tur == "SATIS");

        // Fark TEK KEZ kasaya girdi (3.000), çifte yazılmadı.
        Assert.Equal(kasaOnce + 3_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        // 1 geri geldi, 1 çıktı → adet AYNI kalır.
        Assert.Equal(stokOnce, StokOku(kontrol, k).Adet);
    }

    /// <summary>
    /// ⚠ ÇİFT İADE KAPISI (plan risk notu): değişimde geri alınan adet, iade
    /// hakkını TÜKETİR. Aksi hâlde aynı mal önce değişimle, sonra iadeyle iki
    /// kez geri alınır ve para iki kez çıkardı.
    /// </summary>
    [Fact]
    public async Task Degisimden_sonra_ayni_mal_IKINCI_kez_iade_edilemez()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-deg-2", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var ara = vt.OlusturSistem();
        var satisId = ara.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;

        using var db2 = vt.Olustur(k);
        await Satis(db2).Degisim(new MSatis.DegisimDto
        {
            KaynakHareketId = satisId, GeriAlinanAdet = 1m,
            YeniUrunId = k.UrunId, YeniAdet = 1m, YeniBirimFiyatTl = 10_000m,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        using var db3 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db3).SatisIade(
            new MSatis.IadeDto
            {
                KaynakHareketId = satisId, Adet = 1m, OdemeTuru = "NAKIT",
                KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        // Mesaj metni IadeKaynagi kapısından gelir; kural: hak TÜKENMİŞTİR.
        Assert.Contains("zaten iade edilmiş", ex.Message);
    }

    [Fact]
    public async Task Degisimde_KART_farki_kapalidir_ve_sebebini_soyler()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-deg-3", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var ara = vt.OlusturSistem();
        var satisId = ara.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db2).Degisim(
            new MSatis.DegisimDto
            {
                KaynakHareketId = satisId, GeriAlinanAdet = 1m,
                YeniUrunId = k.UrunId, YeniAdet = 1m, YeniBirimFiyatTl = 13_000m,
                FarkOdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("KART farkı", ex.Message);
    }

    // ═══════════ Y-20 · KONSİNYE ÇIKIŞ / İADE ════════════════════════════════

    [Fact]
    public async Task Konsinye_cikis_stoktan_duser_ama_TL_ve_cari_bakiyeye_DOKUNMAZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-kons-1", stokAdet: 10m);
        var karsi = CariEkle(sistem, k, "Komşu kuyumcu");
        using var db = vt.Olustur(k);

        await Satis(db).KonsinyeCikis(new MSatis.KonsinyeCikisDto
        {
            CariId = karsi, UrunId = k.UrunId, Adet = 4m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "KONSINYE_CIKIS");
        Assert.Equal(0m, h.TlTutari);                        // SATIŞ DEĞİLDİR
        Assert.True(h.HasGrami < 0m);                        // mal fiziken çıktı
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(6m, StokOku(kontrol, k).Adet);
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, karsi).Tl);
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task Konsinye_iadesi_stogu_geri_getirir_fazla_iade_REDDEDILIR()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-kons-2", stokAdet: 10m);
        var karsi = CariEkle(sistem, k, "Komşu kuyumcu");
        using var db = vt.Olustur(k);

        var cikis = await Satis(db).KonsinyeCikis(new MSatis.KonsinyeCikisDto
        {
            CariId = karsi, UrunId = k.UrunId, Adet = 4m, KurSnapshotId = k.KurId,
        }, k.Islem);
        var cikisId = Alan(cikis, "hareketId");

        using var db2 = vt.Olustur(k);
        await Satis(db2).KonsinyeIade(new MSatis.KonsinyeIadeDto
        {
            CikisHareketId = cikisId, Adet = 3m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(9m, StokOku(kontrol, k).Adet);

        // Açık 1 adet kaldı; 2 adet iade edilemez.
        using var db3 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db3).KonsinyeIade(
            new MSatis.KonsinyeIadeDto
            {
                CikisHareketId = cikisId, Adet = 2m, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("açık konsinye adedini aşıyor", ex.Message);

        using var kontrol2 = vt.OlusturSistem();
        Assert.Equal(9m, StokOku(kontrol2, k).Adet);   // hiçbir şey yazılmadı
    }

    [Fact]
    public async Task Rezerve_mal_konsinyeye_CIKARILAMAZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-kons-3", stokAdet: 3m);
        var karsi = CariEkle(sistem, k);
        using var db = vt.Olustur(k);

        await Satis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 3m }, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).KonsinyeCikis(
            new MSatis.KonsinyeCikisDto
            {
                CariId = karsi, UrunId = k.UrunId, Adet = 1m, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("satılabilir", ex.Message);
    }

    // ═══════════ KİRACI İZOLASYONU ═══════════════════════════════════════════

    [Fact]
    public async Task Baska_kiracinin_rezervasyonu_iptal_edilemez()
    {
        using var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P21-izo-A", stokAdet: 5m);
        var b = TestKurulum.KiraciKur(sistem, "P21-izo-B", stokAdet: 5m);

        using var dbA = vt.Olustur(a);
        var r = await Satis(dbA).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = a.UrunId, CariId = a.CariId, Adet = 1m }, a.Islem);

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Satis(dbB).RezervIptal(
            Alan(r, "id"), new MSatis.RezervIptalDto { Gerekce = "Başka kiracıdan iptal" }, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(RezervasyonDurumu.Aktif,
            kontrol.Rezervasyonlar.Single(x => x.Id == Alan(r, "id")).Durum);
    }
}
