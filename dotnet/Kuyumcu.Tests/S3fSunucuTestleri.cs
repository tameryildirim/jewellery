using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Admin;
using Kuyumcu.Api.Modules.Kurulum;
using Kuyumcu.Api.Modules.Siparis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3f — QA turu 5 YÜKSEK bulguları (2. dilim) · SUNUCU TARAFI
//
// Her test QA turu 5'te KANITLANMIŞ bir davranışı yeniden üretir ve düzeltmeyi
// ölçer. Mutasyon kanıtı raporda (PAKET-S-3f-uygulama-raporu.md § mutasyonlar).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class S3fSunucuTestleri(TestVeritabani vt)
{
    // ── /isletme — işletme kimliği telefonu doğrulanmıyordu ─────────────────

    private static AdminServisi Admin(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db));

    [Fact]
    public async Task Isletme_kimliginde_gecersiz_telefon_400_dondurur()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-tel");
        using var ctx = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Admin(ctx).KimlikGuncelle(new TenantKimlikGuncelleDto { Telefon = "abc" }, k.Islem));
        Assert.Contains("Telefon", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Kayıt YAZILMADI (ölçüm veritabanından, yanıttan değil).
        using var oku = vt.OlusturSistem();
        Assert.Null(oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).Telefon);
    }

    [Fact]
    public async Task Isletme_kimliginde_gecerli_telefon_KABUL_edilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-tel-ok");
        using var ctx = vt.Olustur(k);

        await Admin(ctx).KimlikGuncelle(
            new TenantKimlikGuncelleDto { Telefon = "+90 212 555 44 33" }, k.Islem);

        using var oku = vt.OlusturSistem();
        Assert.Equal("+90 212 555 44 33",
            oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).Telefon);
    }

    [Fact]
    public async Task Telefon_GONDERILMEZSE_mevcut_deger_KORUNUR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-tel-patch");
        using var ctx = vt.Olustur(k);
        await Admin(ctx).KimlikGuncelle(
            new TenantKimlikGuncelleDto { Telefon = "05551112233" }, k.Islem);

        // PATCH sözleşmesi: gövdede YOKSA korunur — yeni kural bunu bozmamalı.
        using var ctx2 = vt.Olustur(k);
        await Admin(ctx2).KimlikGuncelle(new TenantKimlikGuncelleDto { Ad = "Yeni Unvan" }, k.Islem);

        using var oku = vt.OlusturSistem();
        var t = oku.Tenants.AsNoTracking().Single(x => x.Id == k.TenantId);
        Assert.Equal("05551112233", t.Telefon);
        Assert.Equal("Yeni Unvan", t.Ad);
    }

    // ── /kurulum — kuru prova negatif açılışı geçerli sayıyordu ─────────────

    private static AcilisServisi Acilis(KuyumcuDbContext db)
    {
        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
            db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var kurulum = new KurulumServisi(db, denetim, NullLogger<KurulumServisi>.Instance,
            new ConfigurationBuilder().Build());
        return new AcilisServisi(db, cari, denetim, TestVeritabani.TestSifreleyici,
            kurulum, NullLogger<AcilisServisi>.Instance);
    }

    private static List<string> HataKodlari(object prova)
    {
        var kok = JsonDocument.Parse(JsonSerializer.Serialize(prova)).RootElement;
        // ⚠ Anonim nesne varsayılan seçeneklerle serileştirilir: record alanları
        //   PascalCase kalır. Ad araması BÜYÜK-KÜÇÜK HARF DUYARSIZ yapılır.
        return [.. kok.GetProperty("hatalar").EnumerateArray()
            .Select(h => h.EnumerateObject()
                .FirstOrDefault(pr => string.Equals(pr.Name, "hataKodu",
                    StringComparison.OrdinalIgnoreCase)).Value.GetString() ?? "")];
    }

    [Fact]
    public async Task Negatif_KASA_acilisi_provada_SATIR_HATASI_olur_ve_jeton_URETILMEZ()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-neg-kasa");
        using var ctx = vt.Olustur(k);

        var prova = await Acilis(ctx).Prova(new AcilisProvaDto
        {
            Tur = AcilisTuru.Kasa,
            Satirlar = [new AcilisSatirDto { HedefId = k.KasaId, TlTutar = -250m }],
        }, k.Islem);

        Assert.False(TestKurulum.Bayrak(prova, "temiz"));
        Assert.Contains(AcilisHataKodu.NegatifAcilis, HataKodlari(prova));
        Assert.Null(TestKurulum.Metin(prova, "provaJetonu"));
    }

    [Fact]
    public async Task Negatif_CARI_acilisi_GECERLIDIR_musteri_alacakli_olabilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-neg-cari");
        using var ctx = vt.Olustur(k);

        var prova = await Acilis(ctx).Prova(new AcilisProvaDto
        {
            Tur = AcilisTuru.Cari,
            Satirlar = [new AcilisSatirDto { HedefId = k.CariId, TlTutar = -250m }],
        }, k.Islem);

        Assert.True(TestKurulum.Bayrak(prova, "temiz"));
        Assert.DoesNotContain(AcilisHataKodu.NegatifAcilis, HataKodlari(prova));
    }

    [Fact]
    public async Task Pozitif_kasa_acilisi_HALA_gecerlidir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3f-poz-kasa");
        using var ctx = vt.Olustur(k);

        var prova = await Acilis(ctx).Prova(new AcilisProvaDto
        {
            Tur = AcilisTuru.Kasa,
            Satirlar = [new AcilisSatirDto { HedefId = k.KasaId, TlTutar = 250m }],
        }, k.Islem);

        Assert.True(TestKurulum.Bayrak(prova, "temiz"));
        Assert.NotNull(TestKurulum.Metin(prova, "provaJetonu"));
    }

    // ── /evraklar/takvim — boş durum yanlış bilgi veriyordu ─────────────────

    private static Kuyumcu.Api.Modules.Evrak.EvrakServisi Evrak(KuyumcuDbContext db) =>
        new(db, NullLogger<Kuyumcu.Api.Modules.Evrak.EvrakServisi>.Instance,
            TestKurulum.Denetim(db),
            Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan());

    [Fact]
    public async Task Takvim_ARALIK_bos_olsa_bile_PORTFOY_ozetini_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "S3f-takvim");
        var bugun = DateOnly.FromDateTime(Kuyumcu.Api.Common.Zaman.UtcdenYerele(DateTime.UtcNow));

        using (var yaz = vt.Olustur(k))
        {
            await Evrak(yaz).Gir(new Kuyumcu.Api.Modules.Evrak.EvrakGirisDto
            {
                EvrakTuru = "CEK", Tutar = 1_250.50m, VadeGunu = bugun.AddDays(120),
                EvrakNo = "S3F-CEK-1", BankaAdi = "Ziraat",
                KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
            }, k.Islem);
        }

        using var db = vt.Olustur(k);
        // Aralık BİLEREK evrakın vadesini KAPSAMAZ.
        var takvim = await Evrak(db).Takvim(k.Islem, bugun, bugun.AddDays(10), null);

        Assert.Empty(takvim.Kalemler);          // aralıkta gün yok — DOĞRU
        Assert.Equal(0m, takvim.ToplamAdet);
        // …ama portföy BOŞ DEĞİL; ekran "Portföyde evrak yok" diyemez.
        Assert.Equal(1m, takvim.PortfoyAdet);
        Assert.Equal(1_250.50m, takvim.PortfoyTutar);
        Assert.Equal(bugun.AddDays(120), takvim.PortfoyIlkVade);
        Assert.Equal(bugun.AddDays(120), takvim.PortfoySonVade);
        Assert.True(takvim.AzamiGun > 0);
    }

    [Fact]
    public async Task Takvim_portfoy_GERCEKTEN_bossa_sifir_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "S3f-takvim-bos");
        var bugun = DateOnly.FromDateTime(Kuyumcu.Api.Common.Zaman.UtcdenYerele(DateTime.UtcNow));

        using var db = vt.Olustur(k);
        var takvim = await Evrak(db).Takvim(k.Islem, bugun, bugun.AddDays(30), null);

        Assert.Equal(0m, takvim.PortfoyAdet);
        Assert.Null(takvim.PortfoyIlkVade);
    }

    // ── /pos — bağlı kasa oluşturmadan sonra düzenlenemiyordu ───────────────

    [Fact]
    public async Task Tahsilati_olmayan_cihazda_TahsilatVar_FALSE_ve_kasa_DEGISTIRILEBILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "S3f-pos-bos");
        var cihazId = PosYardim.CihazYaz(vt, k);
        var ikinciKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        using var db = vt.Olustur(k);
        var once = await PosYardim.Servis(db).CihazListele(k.Islem, null, null);
        Assert.False(Assert.Single(once).TahsilatVar);

        await PosYardim.Servis(db).CihazGuncelle(
            cihazId, new Kuyumcu.Api.Modules.Pos.PosCihazGuncelleDto { KasaId = ikinciKasa }, k.Islem);

        using var oku = vt.OlusturSistem();
        Assert.Equal(ikinciKasa, oku.PosCihazlari.AsNoTracking().Single(c => c.Id == cihazId).KasaId);
    }

    [Fact]
    public async Task Tahsilati_OLAN_cihazda_TahsilatVar_TRUE_ve_kasa_degisimi_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "S3f-pos-dolu");
        var cihazId = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihazId, 1_000m);
        var ikinciKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        using var db = vt.Olustur(k);
        var liste = await PosYardim.Servis(db).CihazListele(k.Islem, null, null);
        Assert.True(Assert.Single(liste).TahsilatVar);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            PosYardim.Servis(db).CihazGuncelle(
                cihazId, new Kuyumcu.Api.Modules.Pos.PosCihazGuncelleDto { KasaId = ikinciKasa }, k.Islem));
        // ⚠ PAKET P-22 / B-55: 409 KORUNDU (sessiz alan güncellemesiyle kasa
        //   taşınmaz) ama mesaj artık ÇIKMAZ DEĞİL, GÖÇ UCUNU gösterir.
        Assert.Contains("kasa-goc", ex.Message, StringComparison.OrdinalIgnoreCase);

        using var oku = vt.OlusturSistem();
        Assert.NotEqual(ikinciKasa, oku.PosCihazlari.AsNoTracking().Single(c => c.Id == cihazId).KasaId);
    }

    [Fact]
    public async Task Kasa_GONDERILMEZSE_cihazin_kasasi_KORUNUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "S3f-pos-patch");
        var cihazId = PosYardim.CihazYaz(vt, k);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).CihazGuncelle(
            cihazId, new Kuyumcu.Api.Modules.Pos.PosCihazGuncelleDto { Ad = "Yeni Ad" }, k.Islem);

        using var oku = vt.OlusturSistem();
        var c = oku.PosCihazlari.AsNoTracking().Single(x => x.Id == cihazId);
        Assert.Equal(k.KasaId, c.KasaId);
        Assert.Equal("Yeni Ad", c.Ad);
    }

    // ── /siparisler — ONAYLANDI geri alınamıyordu (saf durum makinesi) ──────

    [Fact]
    public void Onaylandi_durumundan_TASLAK_a_geri_donus_IZINLIDIR()
    {
        foreach (var tur in new[]
                 {
                     SiparisTuru.MusteriSiparisi, SiparisTuru.AtolyeSiparisi, SiparisTuru.Tamir,
                 })
        {
            Assert.True(SiparisDurumMakinesi.Izinli(tur, SiparisDurumu.Onaylandi, SiparisDurumu.Taslak),
                $"{tur}: ONAYLANDI → TASLAK izinli olmalı");
            Assert.Contains(SiparisDurumu.Taslak,
                SiparisDurumMakinesi.Hedefler(tur, SiparisDurumu.Onaylandi));
        }
    }

    [Fact]
    public void Geri_donus_GEREKCE_ister_ileri_gecis_ISTEMEZ()
    {
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Onaylandi, SiparisDurumu.Taslak));
        Assert.False(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Taslak, SiparisDurumu.Onaylandi));
    }

    // ⚠ KARAR DEĞİŞTİ — K-23 (2026-09-21, KARARLAR-URUN-2026-09-21.md § K-23):
    //   "ATOLYEDE/HAZIR geri dönüşü açılmadı" kilidi KALDIRILDI. Yeni kural:
    //   GERİ DÖNÜŞ SERBEST AMA DENETİMLİ. Eski testin yerine, kararın DÖRT
    //   ŞARTINI ölçen testler geçti (PAKET P-22).
    [Fact]
    public void K23_Atolyede_ve_Hazir_durumlarindan_geri_donus_ACIK()
    {
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.MusteriSiparisi, SiparisDurumu.Atolyede, SiparisDurumu.Onaylandi));
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.MusteriSiparisi, SiparisDurumu.Hazir, SiparisDurumu.Atolyede));
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.MusteriSiparisi, SiparisDurumu.Hazir, SiparisDurumu.Onaylandi));
    }

    [Fact]
    public void K23_geri_donus_GEREKCE_ister_ve_GERI_YONLU_isaretlenir()
    {
        Assert.True(SiparisDurumMakinesi.GeriYonlu(SiparisDurumu.Hazir, SiparisDurumu.Atolyede));
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Hazir, SiparisDurumu.Atolyede));
        Assert.False(SiparisDurumMakinesi.GeriYonlu(SiparisDurumu.Atolyede, SiparisDurumu.Hazir));
        // İPTAL geri yönlü SAYILMAZ: kendi rol kapısı vardır, gerekçesi ayrı kuraldır.
        Assert.False(SiparisDurumMakinesi.GeriYonlu(SiparisDurumu.Hazir, SiparisDurumu.Iptal));
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Hazir, SiparisDurumu.Iptal));
    }

    [Fact]
    public void K23_atolye_siparisinde_HAZIR_yalniz_ATOLYEDE_ye_doner()
    {
        // Malın atölyeden çıktığı iddiası bu ekrandan doğrulanamaz (md.1):
        // iki adım geri gerekiyorsa İKİ geçiş yazılır, ikisi de izde görünür.
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.AtolyeSiparisi, SiparisDurumu.Hazir, SiparisDurumu.Atolyede));
        Assert.False(SiparisDurumMakinesi.Izinli(
            SiparisTuru.AtolyeSiparisi, SiparisDurumu.Hazir, SiparisDurumu.Onaylandi));
    }

    [Fact]
    public void Terminal_durumlarin_HICBIR_cikisi_yoktur()
    {
        Assert.Empty(SiparisDurumMakinesi.Hedefler(SiparisTuru.MusteriSiparisi, SiparisDurumu.TeslimEdildi));
        Assert.Empty(SiparisDurumMakinesi.Hedefler(SiparisTuru.MusteriSiparisi, SiparisDurumu.Iptal));
    }
}
