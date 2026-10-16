using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kur;
using Kuyumcu.Api.Modules.Masak;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d — SERVİS / VERİTABANI TESTLERİ
//   madde 1  açılış stoğu ürün kartından yazılır (mevcut stok yolu, ikinci yol yok)
//   madde 2  MASAK açıklama mesajı HANGİ ALANI kastettiğini söyler
//   madde 4  kümülatif eşik mesajı TEK BİÇİM (ilk denemede de toplamı verir)
//   madde 9  kur geçmişi bitiş GÜNÜNÜN TAMAMINI kapsar
//   madde 11 gün sonu: fişe bağlanmamış fatura görünür, FARK tek tanım
//   madde 12 aylık net has gram günlükle AYNI tanım
//   madde 13 kasa PATCH ile aktifleştirilebilir
//   madde 16 vade takibi: ufuk ötesi + toplam açık alacak
//   madde 20 pusula GÖRÜNTÜLEME ile YAZDIRMA ayrı denetim olayı
//   madde 21 aylık kâr TEK TANIM (KarZararServisi)
//   madde 23 denetim izi ters tarih aralığı 400
//   madde 24 sözleşmesiz kur kaynakları yapılandırmayla kapanır
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UX4dServisTestleri(TestVeritabani vt)
{
    private static DenetimServisi Denetim(KuyumcuDbContext db) => new(db, new HttpContextAccessor());

    // ── madde 1 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M1_Urun_kartindan_ACILIS_STOGU_girilince_stok_ve_DEFTER_satiri_dogar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d acilis stok");
        var db = vt.Olustur(k);
        var s = new UrunServisi(db);

        var sonuc = await s.Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "ACL-1", Ad = "Açılış bileziği", AyarId = "K22",
            GramAgirlik = 10m, Birim = "ADET",
            AcilisStogu = new AcilisStoguDto { Adet = 7m },
        }, k.Islem);

        var urunId = (string)sonuc.GetType().GetProperty("Id")!.GetValue(sonuc)!;

        var sistem = vt.OlusturSistem();
        var stok = await sistem.Stoklar.FirstOrDefaultAsync(x => x.UrunId == urunId);
        Assert.NotNull(stok);
        Assert.Equal(7m, stok!.Adet);

        // Deftere YAZILDI: stok sessizce doğmadı (HareketFabrikasi tek yol).
        var hareket = await sistem.Hareketler.FirstOrDefaultAsync(h => h.UrunId == urunId);
        Assert.NotNull(hareket);
        Assert.Equal("SAYIM_DUZELTME", hareket!.Tur);
        Assert.Contains("Açılış stoğu", hareket.Aciklama);
    }

    [Fact]
    public async Task M1_Acilis_stogu_VERILMEZSE_davranis_AYNEN_korunur_stok_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d acilis yok");
        var db = vt.Olustur(k);
        var sonuc = await new UrunServisi(db).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "ACL-2", Ad = "Stoksuz", AyarId = "K22",
            GramAgirlik = 10m, Birim = "ADET",
        }, k.Islem);

        var urunId = (string)sonuc.GetType().GetProperty("Id")!.GetValue(sonuc)!;
        Assert.Null(await vt.OlusturSistem().Stoklar.FirstOrDefaultAsync(x => x.UrunId == urunId));
    }

    [Fact]
    public async Task M1_Birden_cok_aktif_sube_varsa_sube_TAHMIN_EDILMEZ_400_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d iki sube");
        sistem.Subeler.Add(new Sube
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Şube 2", Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var islemSubesiz = new Kuyumcu.Api.Auth.AktifKullanici(k.KullaniciId, k.TenantId, null, "SAHIP");

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(db).Olustur(new UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = "ACL-3", Ad = "İki şube", AyarId = "K22",
                GramAgirlik = 10m, Birim = "ADET",
                AcilisStogu = new AcilisStoguDto { Adet = 3m },
            }, islemSubesiz));

        Assert.Contains("şube seçilmelidir", hata.Message);
    }

    // ── madde 2 ve 4 ─────────────────────────────────────────────────────────

    [Fact]
    public void M2_MASAK_aciklama_mesaji_HANGI_ALANI_kastettigini_soyler()
    {
        var kapi = MasakKapisi.Varsayilan();
        var hata = Assert.Throws<BadRequestException>(() =>
            kapi.Dogrula(250_000m, new MasakBilgiDto
            {
                BelgeTuru = "TC_KIMLIK", BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
                IslemAmaci = "Takı alımı", Aciklama = "kısa",
            }));

        Assert.Contains("MASAK beyan açıklaması", hata.Message);
        Assert.Contains("masak.aciklama", hata.Message);
    }

    [Fact]
    public void M3_Gecersiz_belge_turunde_mesaj_KABUL_EDILEN_DEGERLERI_listeler()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            MasakKapisi.Varsayilan().Dogrula(200_000m, new MasakBilgiDto
            {
                BelgeTuru = "EHLIYET", BelgeNo = "1", AdSoyad = "X",
                IslemAmaci = "a", Aciklama = new string('a', 30),
            }));

        Assert.Contains("TC_KIMLIK", hata.Message);
        Assert.Contains("PASAPORT", hata.Message);
    }

    [Fact]
    public void M4_Kumulatif_mesaj_ILK_denemede_de_toplami_ve_esigi_verir()
    {
        var kapi = MasakKapisi.Varsayilan();

        // İlk deneme: kümülatif == işlem tutarı (önceki işlem yok).
        var ilk = Assert.Throws<BadRequestException>(() =>
            kapi.Dogrula(200_000m, null, kumulatifTutarTl: 200_000m));
        // İkinci deneme: aynı gün önceki işlemlerle birlikte.
        var ikinci = Assert.Throws<BadRequestException>(() =>
            kapi.Dogrula(100_000m, null, kumulatifTutarTl: 300_000m));

        foreach (var m in new[] { ilk.Message, ikinci.Message })
        {
            Assert.Contains("Bu cariyle bugün kümülatif", m);
            Assert.Contains("eşik 200.000", m);   // MASAK-370: kimlik isteyen en düşük tutar (amaç beyanı 200.000; md.5/1-b 370.000)
        }
    }

    // ── madde 9 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M9_Kur_gecmisi_BITIS_gununde_olusan_kaydi_da_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d kur gecmis");
        var db = vt.Olustur(k);
        var servis = KurServisiKur(db, sozlesmesizAcik: false);

        await servis.ManuelKurKaydet(new ManuelKurDto { AltinHasFiyati = 6_750m }, k.TenantId);

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun().ToDateTime(TimeOnly.MinValue);
        var liste = await servis.KurGecmisi(k.TenantId, new KurFiltreDto
        {
            Baslangic = bugun.AddDays(-30),
            Bitis = bugun,                 // ekranın gönderdiği biçim: gün 00:00
        });

        Assert.Contains(liste, s => s.Kaynak == "MANUAL" && s.AltinHasFiyati == 6_750m);
    }

    // ── madde 24 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M24_Sozlesmesiz_kaynaklar_KAPALIYKEN_hic_adaptor_kurulmaz()
    {
        var defter = Defter(acik: false);
        Assert.False(defter.SozlesmesizKaynaklarAcik);
        Assert.Empty(defter.Kodlar);
        Assert.Equal("MANUAL", defter.VarsayilanKod);
    }

    [Fact]
    public void M24_Kapali_bayrakta_dis_cekim_denenmez_503_ile_manuel_yola_yonlendirir()
    {
        var hata = Assert.Throws<ApiException>(() => Defter(acik: false).Bul("TRUNCGIL"));
        Assert.Equal(503, hata.StatusCode);
        Assert.Contains("Manuel kur", hata.Message);
    }

    [Fact]
    public void M24_Development_te_bayrak_ACIK_ve_uc_adaptor_kurulur()
    {
        var defter = Defter(acik: true);
        Assert.True(defter.SozlesmesizKaynaklarAcik);
        Assert.Equal(3, defter.Kodlar.Count);
        Assert.All(defter.Kodlar, kod => Assert.True(KurKaynakDefteri.Sozlesmesiz(kod)));
    }

    [Fact]
    public async Task M24_Bayrak_kapaliyken_MANUEL_kur_akisi_calisir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d kur manuel");
        var db = vt.Olustur(k);
        var cevap = await KurServisiKur(db, sozlesmesizAcik: false)
            .ManuelKurKaydet(new ManuelKurDto { AltinHasFiyati = 7_100m }, k.TenantId);

        Assert.Equal("MANUAL", cevap.Kaynak);
        Assert.Equal(7_100m, cevap.AltinHasFiyati);
    }

    // ── madde 11 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M11_Fise_baglanmamis_fatura_GORUNUR_ve_FARK_tek_tanimla_hesaplanir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d gun sonu");
        var now = DateTime.UtcNow;

        // Faturalandırılmamış satış: 100.000 TL
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = now, TlTutari = 100_000m, HasGrami = -10m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        // Hiçbir fişe bağlı OLMAYAN satış faturası: 40.000 TL
        sistem.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaNo = "FTR-TEK-1", CariId = k.CariId,
            Tur = "SATIS", AraToplam = 40_000m, Toplam = 40_000m, Durum = "KESILDI",
            Tarih = now, Olusturuldu = now, Guncellendi = now,
        });
        await sistem.SaveChangesAsync();

        var rapor = await TestKurulum.RaporServisi(vt.Olustur(k))
            .PosFaturaMutabakati(DateOnly.FromDateTime(Zaman.UtcdenYerele(now)).ToString("yyyy-MM-dd"), k.Islem);

        var esleme = Alan(rapor, "faturaEslesme")!;
        Assert.Equal(1, Alan(esleme, "bagimsizSayisi"));
        Assert.Equal(40_000m, Alan(esleme, "bagimsizTl"));
        // FARK = faturalanmamış satış (100.000) − fişe bağlanmamış fatura (40.000)
        Assert.Equal(60_000m, Alan(rapor, "fark"));
        Assert.Equal(false, Alan(rapor, "mutabik"));
    }

    [Fact]
    public async Task M11_IPTAL_edilmis_faturanin_damgasi_satisi_FATURALI_yapmaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d iptal damga");
        var now = DateTime.UtcNow;

        // Satış damgalı ama faturası İPTAL: beyana giren bir belgesi yok.
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = now, TlTutari = 30_000m, HasGrami = -4m, ParaBirimi = "TRY",
            FaturaNo = "FTR-IPTAL-1",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        sistem.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaNo = "FTR-IPTAL-1", CariId = k.CariId,
            Tur = "SATIS", AraToplam = 30_000m, Toplam = 30_000m, Durum = "IPTAL",
            Tarih = now, Olusturuldu = now, Guncellendi = now,
        });
        await sistem.SaveChangesAsync();

        var rapor = await TestKurulum.RaporServisi(vt.Olustur(k))
            .PosFaturaMutabakati(DateOnly.FromDateTime(Zaman.UtcdenYerele(now)).ToString("yyyy-MM-dd"), k.Islem);

        var satis = Alan(rapor, "satis")!;
        Assert.Equal(1, Alan(satis, "faturasizSayisi"));
        Assert.Equal(0, Alan(satis, "faturaliSayisi"));
        Assert.Equal(30_000m, Alan(rapor, "fark"));
    }

    // ── madde 12 ve 21 ───────────────────────────────────────────────────────

    [Fact]
    public async Task M12_Aylik_net_has_gram_GUNLUK_ile_AYNI_tanimdir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d aylik has");
        var now = DateTime.UtcNow;
        var yerel = Zaman.UtcdenYerele(now);

        // Hurda alımı: has GİRER — günlük bunu sayıyordu, aylık saymıyordu.
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "HURDA_ALIS",
            Tarih = now, TlTutari = -50_000m, HasGrami = 45m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var rapor = TestKurulum.RaporServisi(vt.Olustur(k));
        var gunluk = await rapor.GunlukOzet(DateOnly.FromDateTime(yerel).ToString("yyyy-MM-dd"), k.Islem);
        var aylik = await rapor.AylikOzet(yerel.Year, yerel.Month, k.Islem);

        var g = (decimal)Alan(gunluk, "netHasGramHareketi")!;
        var a = (decimal)Alan(aylik, "netHasGramHareketi")!;
        Assert.Equal(45m, g);
        Assert.Equal(g, a);   // aylık, o ayın günlüklerinden KÜÇÜK olamaz
    }

    [Fact]
    public async Task M21_Aylik_kar_KAR_ZARAR_SERVISINDEN_gelir_satis_eksi_alis_DEGIL()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d aylik kar");
        var now = DateTime.UtcNow;
        var yerel = Zaman.UtcdenYerele(now);

        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = now, TlTutari = 80_000m, HasGrami = -10m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "ALIS",
            Tarih = now, TlTutari = -120_000m, HasGrami = 18m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var aylik = await TestKurulum.RaporServisi(db, new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db))
            .AylikOzet(yerel.Year, yerel.Month, k.Islem);

        // PAKET KAR-RAPOR (02.10): bu fikstürdeki satışın maliyet partisi YOK. Eski hesap
        // onu sıfır maliyetle kâra katıp "kâr = 80.000 = satış" diyordu (fail-open, demo
        // bulgusunun kök nedeni). Artık kâr HESAPLANAMAZ: null + durum HESAPLANAMADI.
        Assert.Equal("HESAPLANAMADI", Alan(aylik, "karKaynagi"));
        Assert.Equal("HESAPLANAMADI", Alan(aylik, "karDurumu"));
        Assert.Null(Alan(aylik, "karTl"));
        Assert.Equal(1, Alan(aylik, "maliyetiEksikSatisAdedi"));
        Assert.Equal(80_000m, Alan(aylik, "maliyetiEksikTutarTl"));
        // Eski (yanlış) tanım ADIYLA korunuyor ve kâr ile AYNI DEĞİL.
        Assert.Equal(-40_000m, Alan(aylik, "satisAlisFarkiTl"));
        Assert.NotEqual(Alan(aylik, "satisAlisFarkiTl"), Alan(aylik, "karTl"));
    }

    [Fact]
    public async Task M21_KarZarar_servisi_YOKSA_kar_alani_NULL_doner_sifir_YAZILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d kar null");
        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var aylik = await TestKurulum.RaporServisi(vt.Olustur(k)).AylikOzet(yerel.Year, yerel.Month, k.Islem);

        Assert.Null(Alan(aylik, "karTl"));
        Assert.Equal("HESAPLANAMADI", Alan(aylik, "karKaynagi"));
    }

    // ── madde 16 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M16_Ufkun_OTESINDEKI_acik_alacak_toplamda_GORUNUR()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d vade");
        var now = DateTime.UtcNow;

        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "VERESIYE",
            CariId = k.CariId, Tarih = now, TlTutari = 76_000m, HasGrami = 0m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            VeresiyeTutar = 76_000m,
            VadeTarihi = DateOnly.FromDateTime(Zaman.UtcdenYerele(now).AddDays(60)),
        });
        var bakiye = await sistem.CariBakiyeler.FirstAsync(b => b.CariId == k.CariId);
        bakiye.TlBakiye = 76_000m;
        await sistem.SaveChangesAsync();

        var r = await TestKurulum.RaporServisi(vt.Olustur(k)).VadeTakibi(k.Islem, ufukGun: 7);

        // Üç eski kart SIFIR (bu doğrudur: vadesi 60 gün sonra)…
        Assert.Equal(0m, Alan(r, "gecikenToplam"));
        Assert.Equal(0m, Alan(r, "yaklasanToplam"));
        // …ama para KAYBOLMAZ: ufuk ötesi ve toplam açık alacak gösterilir.
        Assert.Equal(76_000m, Alan(r, "ufukOtesiToplam"));
        Assert.Equal(76_000m, Alan(r, "acikToplam"));
    }

    // ── madde 13 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M13_Pasife_alinan_kasa_PATCH_ile_AKTIFLESTIRILEBILIR()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d kasa aktif");
        var kasa = await sistem.Kasalar.FirstAsync(x => x.Id == k.KasaId);
        kasa.Bakiye = 0m; kasa.Aktif = false;
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Kasa.KasaServisi(db, NullLogger<Kuyumcu.Api.Modules.Kasa.KasaServisi>.Instance, Denetim(db))
            .Guncelle(k.KasaId, new Kuyumcu.Api.Modules.Kasa.KasaGuncelleDto { Ad = kasa.Ad, Aktif = true }, k.Islem);

        Assert.True((await vt.OlusturSistem().Kasalar.FirstAsync(x => x.Id == k.KasaId)).Aktif);
    }

    [Fact]
    public async Task M13_Bakiyeli_kasa_PATCH_ile_de_pasife_ALINAMAZ_kapi_ayni()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d kasa kapi");
        var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new Kuyumcu.Api.Modules.Kasa.KasaServisi(db, NullLogger<Kuyumcu.Api.Modules.Kasa.KasaServisi>.Instance, Denetim(db))
                .Guncelle(k.KasaId, new Kuyumcu.Api.Modules.Kasa.KasaGuncelleDto { Ad = "Kasa", Aktif = false }, k.Islem));

        Assert.Contains("Bakiyesi sıfır olmayan", hata.Message);
    }

    // ── madde 23 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M23_Denetim_izinde_TERS_tarih_araligi_400_doner_bos_liste_DEGIL()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4d denetim aralik");
        var db = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Denetim.DenetimOkumaServisi(db, Denetim(db));

        var bas = DateTime.UtcNow;
        var bit = bas.AddDays(-5);

        var h1 = await Assert.ThrowsAsync<BadRequestException>(
            () => servis.Listele(k.Islem, bas, bit, null, null, null, null, null, 50, 0));
        var h2 = await Assert.ThrowsAsync<BadRequestException>(() => servis.Ozet(k.Islem, bas, bit));

        // PAKET GOC-D: sunucu metni ekranın alan altı metniyle AYNI sözlük anahtarı (Core RaporDonemKurali → Kabuk.TarihAraligi).
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.TarihAraligi, h1.Message);
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.TarihAraligi, h2.Message);
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────

    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    private static KurKaynakDefteri Defter(bool acik)
    {
        var yapilandirma = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kur:SozlesmesizKaynaklarAcik"] = acik ? "true" : "false",
            })
            .Build();
        return new KurKaynakDefteri(new SahteHttpFabrikasi(), NullLogger<KurKaynakDefteri>.Instance, yapilandirma);
    }

    private static KurServisi KurServisiKur(KuyumcuDbContext db, bool sozlesmesizAcik) =>
        new(db, NullLogger<KurServisi>.Instance, new SahteHub(), Defter(sozlesmesizAcik),
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

    /// <summary>
    /// Dış ağa ÇIKMAYAN HttpClient fabrikası — bayrak kapalıyken hiç
    /// kullanılmadığı, açıkken de testin ağ istemediği kanıtlanır.
    /// </summary>
    private sealed class SahteHttpFabrikasi : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Engel());

        private sealed class Engel : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage istek, CancellationToken iptal) =>
                throw new InvalidOperationException(
                    $"Testte dış HTTP çağrısı yapılmamalıydı: {istek.RequestUri}");
        }
    }

    private sealed class SahteHub : Microsoft.AspNetCore.SignalR.IHubContext<KurHub>
    {
        public Microsoft.AspNetCore.SignalR.IHubClients Clients { get; } = new BosIstemciler();
        public Microsoft.AspNetCore.SignalR.IGroupManager Groups { get; } = new BosGrup();

        private sealed class BosIstemciler : Microsoft.AspNetCore.SignalR.IHubClients
        {
            public Microsoft.AspNetCore.SignalR.IClientProxy All { get; } = new BosProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy AllExcept(IReadOnlyList<string> x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy Client(string x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy Clients(IReadOnlyList<string> x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy Group(string x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy GroupExcept(string x, IReadOnlyList<string> y) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy Groups(IReadOnlyList<string> x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy User(string x) => All;
            public Microsoft.AspNetCore.SignalR.IClientProxy Users(IReadOnlyList<string> x) => All;
        }

        private sealed class BosProxy : Microsoft.AspNetCore.SignalR.IClientProxy
        {
            public Task SendCoreAsync(string metot, object?[] argumanlar, CancellationToken iptal = default)
                => Task.CompletedTask;
        }

        private sealed class BosGrup : Microsoft.AspNetCore.SignalR.IGroupManager
        {
            public Task AddToGroupAsync(string c, string g, CancellationToken i = default) => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string c, string g, CancellationToken i = default) => Task.CompletedTask;
        }
    }
}
