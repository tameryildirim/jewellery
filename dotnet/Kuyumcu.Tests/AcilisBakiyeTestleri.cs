using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kurulum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// AÇILIŞ BAKİYELERİ — paket T-2 Faz A [DB, gerçek MySQL]
//
// Ölçülen değişmezler:
//   FAIL-CLOSED 2  — kuru prova jetonu olmadan uygulama yok
//   FAIL-CLOSED 3  — OdemeIzi.Yok: nakit/veresiye payı 0, tevsik kümülatifi sabit
//   FAIL-CLOSED 7  — aynı hedefe ikinci açılış yok (şube boyutlu UNIQUE)
//   FAIL-CLOSED 9  — bir satır patlarsa oturumun TAMAMI geri alınır
//   İŞARET SÖZLEŞMESİ — HasGrami VERESIYE ile aynı yönde
//   KREDİ LİMİTİ KARARI — reddetmez, UYARIR; onay olmadan uygulanmaz
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class AcilisBakiyeTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Eski programdan devreden acilis bakiyesi";

    private static (AcilisServisi Acilis, KurulumServisi Kurulum) Servisler(KuyumcuDbContext db)
    {
        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
            db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var kurulum = new KurulumServisi(db, denetim, NullLogger<KurulumServisi>.Instance,
            new ConfigurationBuilder().Build());
        var acilis = new AcilisServisi(db, cari, denetim, TestVeritabani.TestSifreleyici,
            kurulum, NullLogger<AcilisServisi>.Instance);
        return (acilis, kurulum);
    }

    /// <summary>Kuru provayı koşturup jetonu alan yardımcı — testler hep bu yoldan geçer.</summary>
    private static async Task<AcilisUygulaDto> ProvadanJeton(
        AcilisServisi acilis, Kiraci k, string tur, params AcilisSatirDto[] satirlar)
    {
        var prova = await acilis.Prova(new AcilisProvaDto { Tur = tur, Satirlar = [.. satirlar] }, k.Islem);
        var jeton = TestKurulum.Metin(prova, "provaJetonu");
        return new AcilisUygulaDto
        {
            Tur = tur,
            Satirlar = [.. satirlar],
            ProvaJetonu = jeton ?? "",
            Gerekce = Gerekce,
            KurSnapshotId = k.KurId,
        };
    }

    // ── Cari açılışı ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Cari_acilisi_ACILIS_hareketi_uretir_ve_bakiyeyi_gunceller()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-cari");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari,
            new AcilisSatirDto { HedefId = k.CariId, TlTutar = 1_500m, HasGram = 4m });
        Assert.NotEmpty(dto.ProvaJetonu);

        await acilis.Uygula(dto, k.Islem);

        var hareket = db.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "ACILIS");
        Assert.Equal(1_500m, hareket.TlTutari);

        var (tl, has) = TestKurulum.CariBakiye(db, k.CariId);
        Assert.Equal(1_500m, tl);
        Assert.Equal(4m, has);

        var kalem = db.AcilisKalemleri.AsNoTracking().Single(a => a.TenantId == k.TenantId);
        Assert.Equal(AcilisTuru.Cari, kalem.Tur);
        Assert.Equal(hareket.Id, kalem.HareketId);
        Assert.Equal(AcilisKalemDurumu.Uygulandi, kalem.Durum);
    }

    [Fact]
    public async Task Acilis_hareketinin_nakit_ve_veresiye_payi_SIFIRDIR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-iz");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari,
            new AcilisSatirDto { HedefId = k.CariId, TlTutar = 9_000m });
        await acilis.Uygula(dto, k.Islem);

        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "ACILIS");
        // Açılışta para EL DEĞİŞTİRMEZ (FAIL-CLOSED 3).
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(0m, h.VeresiyeTutar);
        Assert.Null(h.VadeTarihi);
    }

    [Fact]
    public async Task Acilis_tevsik_gunluk_nakit_toplamini_DEGISTIRMEZ()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-tevsik");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var an = DateTime.UtcNow;
        var once = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi
            .GunlukNakitToplam(db, k.TenantId, k.CariId, an);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari,
            new AcilisSatirDto { HedefId = k.CariId, TlTutar = 400_000m });
        await acilis.Uygula(dto, k.Islem);

        var sonra = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi
            .GunlukNakitToplam(db, k.TenantId, k.CariId, an);

        // 459 s. VUK haddi SAHTE biçimde doldurulmaz.
        Assert.Equal(once, sonra);
    }

    [Fact]
    public async Task Acilis_isaret_sozlesmesi_VERESIYE_ile_ayni_yonde()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-isaret");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari,
            new AcilisSatirDto { HedefId = k.CariId, TlTutar = 100m, HasGram = 7m });
        await acilis.Uygula(dto, k.Islem);

        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "ACILIS");
        var (_, has) = TestKurulum.CariBakiye(db, k.CariId);

        // Bakiye ARTAR (+7), deftere NEGATİF has düşer — VERESIYE sözleşmesi.
        Assert.Equal(7m, has);
        Assert.Equal(-7m, h.HasGrami);
    }

    // ── Kasa açılışı ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Kasa_acilisi_kasa_bakiyesini_ve_bagli_hareket_id_yi_doldurur()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-kasa");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Kasa,
            new AcilisSatirDto { HedefId = k.KasaId, TlTutar = 12_345.67m });
        await acilis.Uygula(dto, k.Islem);

        Assert.Equal(TestKurulum.KasaAcilis + 12_345.67m, TestKurulum.KasaBakiye(db, k.KasaId));

        var hareket = db.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "ACILIS");
        var kh = db.KasaHareketleri.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.KasaId == k.KasaId);

        // Kasa satırı ÜRETİLEN ACILIS hareketine bağlıdır — "bu para nereden geldi".
        Assert.Equal(hareket.Id, kh.HareketId);
        Assert.Equal(12_345.67m, kh.Tutar);
    }

    // ── İdempotans ve UNIQUE ────────────────────────────────────────────────

    [Fact]
    public async Task Ayni_hedefe_ikinci_acilis_REDDEDILIR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-ikinci");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var satir = new AcilisSatirDto { HedefId = k.CariId, TlTutar = 500m };
        await acilis.Uygula(await ProvadanJeton(acilis, k, AcilisTuru.Cari, satir), k.Islem);

        // İkinci deneme: prova artık TEMİZ DEĞİLDİR ve jeton üretilmez.
        var ikinci = await acilis.Prova(
            new AcilisProvaDto { Tur = AcilisTuru.Cari, Satirlar = [satir] }, k.Islem);
        Assert.False(TestKurulum.Bayrak(ikinci, "temiz"));
        Assert.Null(TestKurulum.Metin(ikinci, "provaJetonu"));

        // Jeton doğru olsa bile UYGULAMA yolu 409 ile durur (FAIL-CLOSED 7):
        // "gövdeni düzelt" demek anlamsızdır, bu bir ÇAKIŞMADIR.
        var zorla = new AcilisUygulaDto
        {
            Tur = AcilisTuru.Cari, Satirlar = [satir],
            ProvaJetonu = TestVeritabani.TestSifreleyici.Hashle(
                $"ACILIS|{k.TenantId}|{AcilisTuru.Cari}|" +
                AcilisKaynaklari.Ozet(AcilisKaynaklari.KanonikMetin(AcilisTuru.Cari, [satir]))),
            Gerekce = Gerekce, KurSnapshotId = k.KurId,
        };
        var ex = await Assert.ThrowsAsync<ConflictException>(() => acilis.Uygula(zorla, k.Islem));
        Assert.Equal(409, ex.StatusCode);

        Assert.Single(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
    }

    [Fact]
    public void Ayni_urun_iki_subede_acilabilir_ayni_subede_ikinci_kez_ACILAMAZ()
    {
        // ⚠ Bertan + Mehmet ortak koşulu: STOK benzersizlik anahtarı ŞUBE
        //   boyutunu taşır. Motoru Faz B'de açılıyor, ama ŞEMA KISITI bugün
        //   yazılıdır ve burada VERİTABANI DÜZEYİNDE sınanır.
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-stok-kisit");
        var sube2 = TestKurulum.SubeEkle(db, k.TenantId);

        db.AcilisKalemleri.Add(Kalem(k, AcilisTuru.Stok, k.UrunId, k.SubeId));
        db.AcilisKalemleri.Add(Kalem(k, AcilisTuru.Stok, k.UrunId, sube2));
        db.SaveChanges();   // iki ŞUBE — meşru, kabul edilir

        db.AcilisKalemleri.Add(Kalem(k, AcilisTuru.Stok, k.UrunId, k.SubeId));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        db.ChangeTracker.Clear();

        Assert.Equal(2, db.AcilisKalemleri.AsNoTracking()
            .Count(a => a.TenantId == k.TenantId && a.Tur == AcilisTuru.Stok));
    }

    private static AcilisKalemi Kalem(Kiraci k, string tur, string hedefId, string? subeId) => new()
    {
        Id = Kimlik.Ulid(),
        TenantId = k.TenantId,
        OturumId = Kimlik.Ulid(),
        Tur = tur,
        HedefId = hedefId,
        SubeId = subeId,
        BenzersizAnahtar = AcilisKalemi.AnahtarUret(tur, hedefId, subeId),
        Durum = AcilisKalemDurumu.Uygulandi,
        Olusturuldu = DateTime.UtcNow,
    };

    // ── Tek transaction ─────────────────────────────────────────────────────

    [Fact]
    public async Task Bir_satir_patlarsa_TUM_oturum_geri_alinir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-transaction");
        var kasa2 = TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "TRY", 1_000m, "Kasa-2");

        var dto = await ProvadanJetonAyriBaglam(k, kasa2);

        // İkinci kasanın GÜNÜ prova ile uygula ARASINDA kapatılır: prova temiz
        // dedi ama kapı uygulamada koşar ve satır patlar.
        db.GunKapanislari.Add(new GunKapanisi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = kasa2, SubeId = k.SubeId,
            YerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow)),
            SayilanNakit = 1_000m, DefterBakiye = 1_000m, Fark = 0m,
            KapatanKullaniciId = k.KullaniciId, KapanisZamani = DateTime.UtcNow,
            Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();

        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);
        await Assert.ThrowsAsync<BadRequestException>(() => acilis.Uygula(dto, k.Islem));

        // Hiçbir şey kalmaz: ilk kasanın bakiyesi bile değişmemiştir.
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(db, k.KasaId));
        Assert.Equal(1_000m, TestKurulum.KasaBakiye(db, kasa2));
        Assert.Empty(db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ACILIS"));
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
    }

    private async Task<AcilisUygulaDto> ProvadanJetonAyriBaglam(Kiraci k, string kasa2)
    {
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);
        return await ProvadanJeton(acilis, k, AcilisTuru.Kasa,
            new AcilisSatirDto { HedefId = k.KasaId, TlTutar = 5_000m },
            new AcilisSatirDto { HedefId = kasa2, TlTutar = 3_000m });
    }

    // ── Kredi limiti kararı (Aşkın'ın koşulu) ───────────────────────────────

    [Fact]
    public async Task Kredi_limiti_asan_acilis_ONAYSIZ_reddedilir_ONAYLA_gecer()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-limit");
        var cari = db.Cariler.Single(c => c.Id == k.CariId);
        cari.KrediLimitiTl = 1_000m;
        db.SaveChanges();

        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var satir = new AcilisSatirDto { HedefId = k.CariId, TlTutar = 50_000m };
        var prova = await acilis.Prova(
            new AcilisProvaDto { Tur = AcilisTuru.Cari, Satirlar = [satir] }, k.Islem);

        // KARAR: limit aşımı HATA DEĞİL UYARIDIR — prova TEMİZ kalır ve jeton döner.
        Assert.True(TestKurulum.Bayrak(prova, "temiz"));
        Assert.True(TestKurulum.Bayrak(prova, "limitAsimiOnayiGerekli"));

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari, satir);
        await Assert.ThrowsAsync<BadRequestException>(() => acilis.Uygula(dto, k.Islem));

        dto.LimitAsimiOnayi = true;
        await acilis.Uygula(dto, k.Islem);
        Assert.Equal(50_000m, TestKurulum.CariBakiye(db, k.CariId).Tl);
    }

    // ── Prova hiçbir şey yazmaz + jeton kapısı ──────────────────────────────

    [Fact]
    public async Task Prova_HICBIR_kayit_yazmaz_ve_JETONSUZ_uygulama_REDDEDILIR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-jeton");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var satir = new AcilisSatirDto { HedefId = k.CariId, TlTutar = 250m };
        await acilis.Prova(new AcilisProvaDto { Tur = AcilisTuru.Cari, Satirlar = [satir] }, k.Islem);

        Assert.Empty(db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ACILIS"));
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));

        var sahte = new AcilisUygulaDto
        {
            Tur = AcilisTuru.Cari, Satirlar = [satir],
            ProvaJetonu = "uydurma-jeton", Gerekce = Gerekce, KurSnapshotId = k.KurId,
        };
        await Assert.ThrowsAsync<BadRequestException>(() => acilis.Uygula(sahte, k.Islem));
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Gerekce_asgarinin_altinda_SERVIS_ICINDE_reddedilir()
    {
        // ⚠ FAIL-CLOSED 10: DTO DataAnnotations YALNIZ denetleyici yolunda
        //   çalışır. HTTP testi yeşil kalsa bile servis içi elle kontrol
        //   silinirse iç akışlar (ve gelecekteki çağıranlar) gerekçesiz açılış
        //   yazabilirdi. Bu test o kontrolü DOĞRUDAN servisten sınar.
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-gerekce-servis");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Kasa,
            new AcilisSatirDto { HedefId = k.KasaId, TlTutar = 100m });
        dto.Gerekce = new string('a', AcilisServisi.GerekceAsgari - 1);

        await Assert.ThrowsAsync<BadRequestException>(() => acilis.Uygula(dto, k.Islem));
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
    }

    // ── Geri alma (ters kayıt) ──────────────────────────────────────────────

    [Fact]
    public async Task Geri_alma_TERS_KAYIT_yazar_ve_kalemi_GERI_ALINDI_yapar()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-gerial");
        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var dto = await ProvadanJeton(acilis, k, AcilisTuru.Cari,
            new AcilisSatirDto { HedefId = k.CariId, TlTutar = 800m });
        var sonuc = await acilis.Uygula(dto, k.Islem);
        var oturumId = TestKurulum.Metin(sonuc, "oturumId")!;

        await acilis.GeriAl(new AcilisGeriAlDto
        {
            OturumId = oturumId,
            Gerekce = "Yanlis tutar girildi, acilis geri aliniyor",
            KurSnapshotId = k.KurId,
        }, k.Islem);

        // Defterden satır SİLİNMEZ: iki ACILIS satırı ve net sıfır bakiye.
        var satirlar = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ACILIS").ToList();
        Assert.Equal(2, satirlar.Count);
        Assert.Equal(0m, satirlar.Sum(h => h.TlTutari));
        Assert.Equal(0m, TestKurulum.CariBakiye(db, k.CariId).Tl);

        var kalem = db.AcilisKalemleri.AsNoTracking()
            .Single(a => a.TenantId == k.TenantId && a.OturumId == oturumId);
        Assert.Equal(AcilisKalemDurumu.GeriAlindi, kalem.Durum);
    }

    // ── Açılış penceresi (Mehmet koşulu 2) ──────────────────────────────────

    [Fact]
    public async Task Acilis_penceresi_dolunca_MUHURSUZ_kiracida_bile_409()
    {
        // ⚠ Mehmet koşulu 2: "mühürsüz kalan kiracıda açılış uçları SÜRESİZ
        //   açık kalmasın". Pencere gün sayısı yapılandırmadan okunur; test
        //   rakamı sabitlemez, pencereyi AÇIKÇA geçmiş bir tarihe çeker.
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-pencere");

        db.KurulumDurumlari.Add(new KurulumDurumu
        {
            Id = Kimlik.Ulid(),
            TenantId = k.TenantId,
            Olusturuldu = DateTime.UtcNow.AddYears(-1),
            Guncellendi = DateTime.UtcNow.AddYears(-1),
        });
        db.SaveChanges();

        using var ctx = vt.Olustur(k);
        var (acilis, _) = Servisler(ctx);

        var ex = await Assert.ThrowsAsync<ApiException>(() => acilis.Prova(
            new AcilisProvaDto
            {
                Tur = AcilisTuru.Kasa,
                Satirlar = [new AcilisSatirDto { HedefId = k.KasaId, TlTutar = 10m }],
            }, k.Islem));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(KurulumHataAdi.PencereKapandi, ex.HataAdi);
    }

    // ── Mühür ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Muhurden_sonra_acilis_KAPANIR()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T2-muhur");
        using var ctx = vt.Olustur(k);
        var (acilis, kurulum) = Servisler(ctx);

        await kurulum.Muhurle(new KurulumMuhurDto
        {
            Gerekce = "Kurulum tamamlandi, acilis kapatiliyor",
            Onay = true,
            // P-49: ikinci kapı — kiracının kayıtlı adı birebir.
            IsletmeAdiOnayi = "T2-muhur",
        }, k.Islem);

        var dto = new AcilisUygulaDto
        {
            Tur = AcilisTuru.Cari,
            Satirlar = [new AcilisSatirDto { HedefId = k.CariId, TlTutar = 1m }],
            ProvaJetonu = "x", Gerekce = Gerekce, KurSnapshotId = k.KurId,
        };
        var ex = await Assert.ThrowsAsync<ApiException>(() => acilis.Uygula(dto, k.Islem));
        Assert.Equal(409, ex.StatusCode);
        // T-1'in /saglik/hazir 409'undan AYRI hata adı (Bertan koşulu).
        Assert.Equal(KurulumHataAdi.Muhurlu, ex.HataAdi);
    }
}
