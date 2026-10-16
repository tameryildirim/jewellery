using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MCari = Kuyumcu.Api.Modules.Cari;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-16 — SATIŞ KALEMİ + FORM ALANI PARİTESİ
// Kimlikler: Y-4 (satır/fiş iskontosu) · Y-26 (ürün kartı) · Y-27 (cari kartı)
//            B-31 (pasif cari satışta) · B-69 (sistemin bildiği alan)
//
// Her iş kuralı için HATAYI YENİDEN ÜRETEN bir test vardır: kural bozulduğunda
// (mutasyon) testin ADIYLA kırmızıya dönmesi raporda kanıtlanmıştır.
// ═════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// 1) SAF — İSKONTO HESABI (veritabanı yok)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class P16IskontoHesabiTestleri
{
    [Fact]
    public void Yuzde_TL_karsiligina_kurusa_yuvarlanarak_cevrilir()
    {
        // 1.234,56 × %10 = 123,456 → 123,46 (AwayFromZero, sunucu ile aynı kural).
        Assert.Equal(123.46m, MSatis.IskontoHesabi.Tutar(1_234.56m, 10m));
    }

    [Fact]
    public void Iskonto_brutu_ASAMAZ()
    {
        // %100'de tam brüt düşer; satır NEGATİFE inemez.
        Assert.Equal(500m, MSatis.IskontoHesabi.Tutar(500m, 100m));

        // ⚠ KELEPÇE BURADA GÖRÜNÜR: kuruş yuvarlaması (AwayFromZero) brütün
        //   ÜSTÜNE çıkabilir. 10,005 × %100 = 10,005 → yuvarlanınca 10,01 olur
        //   ve kelepçe olmasaydı satır −0,005 TL'ye düşerdi. Kelepçesiz bir
        //   sürümde bu satır KIRMIZI olur (mutasyon M16-2).
        Assert.Equal(10.005m, MSatis.IskontoHesabi.Tutar(10.005m, 100m));
    }

    [Fact]
    public void Bos_ve_sifir_yuzde_iskonto_URETMEZ()
    {
        Assert.Equal(0m, MSatis.IskontoHesabi.Tutar(1_000m, null));
        Assert.Equal(0m, MSatis.IskontoHesabi.Tutar(1_000m, 0m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Aralik_disi_yuzde_REDDEDILIR_sessizce_kelepcelenmez(double y)
        => Assert.Throws<BadRequestException>(
            () => MSatis.IskontoHesabi.YuzdeDogrula((decimal)y, "Fiş iskontosu"));

    [Fact]
    public void Fis_iskontosunun_satir_paylari_TOPLAMI_TAM_tutar()
    {
        // 3 satır, kuruş artığı doğuran oran: paylar toplamı iskonto tutarına
        // BİREBİR eşit olmalı — aksi hâlde Σ satır ≠ fiş toplamı olur ve ödeme
        // bacağı doğrulaması 400 verir (ya da para kaybolur).
        decimal[] satirlar = [33.33m, 33.33m, 33.34m];
        var paylar = MSatis.IskontoHesabi.Dagit(satirlar, 10m);
        Assert.Equal(10m, paylar.Sum());
        Assert.Equal(3, paylar.Length);
    }

    [Fact]
    public void Dagitim_hicbir_satiri_NEGATIFE_dusurmez()
    {
        decimal[] satirlar = [1m, 999m];
        var paylar = MSatis.IskontoHesabi.Dagit(satirlar, 1_000m);
        Assert.All(paylar, p => Assert.True(p >= 0m));
        for (var i = 0; i < satirlar.Length; i++) Assert.True(paylar[i] <= satirlar[i]);
    }

    [Fact]
    public void Kalem_yoksa_ya_da_toplam_sifirsa_dagitim_BOS_doner()
    {
        Assert.Empty(MSatis.IskontoHesabi.Dagit([], 100m));
        Assert.All(MSatis.IskontoHesabi.Dagit([0m, 0m], 100m), p => Assert.Equal(0m, p));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 2) SAF — TCKN / VKN HANE KURALI (Y-27)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class P16KimlikNoTestleri
{
    [Fact]
    public void Bos_numara_GECERLIDIR_beyan_yok_demektir()
    {
        Assert.Null(KimlikNo.Dogrula(null, null));
        Assert.Null(KimlikNo.Dogrula("", KimlikNo.Tckn));
    }

    [Fact]
    public void TCKN_11_haneden_kisa_ya_da_uzunsa_REDDEDILIR()
    {
        Assert.NotNull(KimlikNo.Dogrula("1234567890", KimlikNo.Tckn));   // 10
        Assert.NotNull(KimlikNo.Dogrula("123456789012", KimlikNo.Tckn)); // 12
        Assert.Null(KimlikNo.Dogrula("12345678950", KimlikNo.Tckn));     // 11
    }

    [Fact]
    public void VKN_10_hane_olmali()
    {
        Assert.NotNull(KimlikNo.Dogrula("12345678901", KimlikNo.Vkn));   // 11
        Assert.Null(KimlikNo.Dogrula("1234567890", KimlikNo.Vkn));       // 10
    }

    [Fact]
    public void Rakam_disi_karakter_REDDEDILIR()
        => Assert.NotNull(KimlikNo.Dogrula("1234567890A", KimlikNo.Tckn));

    [Fact]
    public void Tur_beyan_edilmemisse_uzunluktan_TURETILIR_uydurulmaz()
    {
        Assert.Equal(KimlikNo.Tckn, KimlikNo.TurTuret("12345678901"));
        Assert.Equal(KimlikNo.Vkn, KimlikNo.TurTuret("1234567890"));
        Assert.Null(KimlikNo.TurTuret("123"));
        // Türsüz + 9 haneli numara: hangi tür olduğu BİLİNEMEZ → hata.
        Assert.NotNull(KimlikNo.Dogrula("123456789", null));
    }

    [Fact]
    public void Segment_kapali_listedir()
    {
        Assert.True(CariSegmentleri.Gecerli(null));
        Assert.True(CariSegmentleri.Gecerli(CariSegmentleri.Vip));
        Assert.False(CariSegmentleri.Gecerli("ALTIN_UYE"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 3) VERİTABANI — SATIŞ FİŞİ İSKONTOSU VE PASİF CARİ KAPISI
// ─────────────────────────────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P16SatisFisiIskontoTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.OdemeBacagiDto B(Kiraci k, string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId };

    private static MSatis.SatisFisiDto Fis(
        Kiraci k, decimal birimFiyat, decimal odenecek,
        decimal? satirIskonto = null, decimal? fisIskonto = null, int kalemAdet = 1) => new()
    {
        CariId = k.CariId,
        Kalemler = Enumerable.Range(0, kalemAdet)
            .Select(_ => new MSatis.SatisFisKalemDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat,
                SatirIskontoYuzde = satirIskonto,
            }).ToList(),
        Odemeler = [B(k, "NAKIT", odenecek)],
        KurSnapshotId = k.KurId,
        FisIskontoYuzde = fisIskonto,
    };

    private static decimal Alan(object y, string ad) =>
        Convert.ToDecimal(y.GetType().GetProperty(ad)!.GetValue(y));

    [Fact]
    public async Task Satir_iskontosu_SUNUCUDA_dusulur_defter_toplami_netdir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 satır iskontosu");
        using var db = vt.Olustur(k);

        // 10.000 TL kalem, %10 satır iskontosu → net 9.000 TL.
        var y = await Satis(db).SatisFisi(Fis(k, 10_000m, 9_000m, satirIskonto: 10m), k.Islem);

        Assert.Equal(9_000m, Alan(y, "toplamTl"));
        Assert.Equal(1_000m, Alan(y, "satirIskontoToplam"));

        using var kontrol = vt.OlusturSistem();
        var satirlar = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).ToList();
        Assert.Equal(9_000m, satirlar.Sum(h => h.TlTutari));
    }

    [Fact]
    public async Task Fis_iskontosu_satirlara_ORANSAL_dagitilir_toplam_TUTAR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 fiş iskontosu");
        using var db = vt.Olustur(k);

        // 3 × 1.000 = 3.000, %10 fiş iskontosu → 2.700.
        var y = await Satis(db).SatisFisi(
            Fis(k, 1_000m, 2_700m, fisIskonto: 10m, kalemAdet: 3), k.Islem);

        Assert.Equal(2_700m, Alan(y, "toplamTl"));
        Assert.Equal(300m, Alan(y, "fisIskontoTutari"));

        using var kontrol = vt.OlusturSistem();
        var satirlar = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).ToList();
        // Σ satır = fiş toplamı: kuruş bile kaçmaz.
        Assert.Equal(2_700m, satirlar.Sum(h => h.TlTutari));
    }

    [Fact]
    public void Iskonto_ISTEMCININ_gonderdigi_TUTARDAN_degil_YUZDEDEN_uretilir()
    {
        // Sözleşme kanıtı: DTO'da iskonto TUTARI alanı YOKTUR. İstemci yüzde
        // gönderir; tutarı sunucu üretir. Bu test, tutar alanı eklenirse
        // (ekran ile defter ayrışması) derlemeyle birlikte düşünülmesini sağlar.
        var alanlar = typeof(MSatis.SatisFisKalemDto).GetProperties().Select(p => p.Name).ToList();
        Assert.Contains("SatirIskontoYuzde", alanlar);
        Assert.DoesNotContain("SatirIskontoTl", alanlar);
        var fisAlanlar = typeof(MSatis.SatisFisiDto).GetProperties().Select(p => p.Name).ToList();
        Assert.Contains("FisIskontoYuzde", fisAlanlar);
        Assert.DoesNotContain("FisIskontoTl", fisAlanlar);
    }

    [Fact]
    public async Task Aralik_disi_fis_iskontosu_400_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 iskonto aralığı");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(Fis(k, 1_000m, 1_000m, fisIskonto: 120m), k.Islem));
    }

    /// <summary>
    /// UX-S21 / B-69 — sistem carinin iskonto oranını BİLİR; kasiyerden
    /// tekrar istenmez. Gövdede yüzde YOKSA cari oranı uygulanır.
    /// </summary>
    [Fact]
    public async Task Fis_iskontosu_bossa_CARININ_orani_uygulanir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P16 cari iskontosu");
        var cari = sistem.Cariler.Single(c => c.Id == k.CariId);
        cari.IskontoYuzde = 20m;
        sistem.SaveChanges();

        using var db = vt.Olustur(k);
        // 1.000 TL kalem, gövdede iskonto YOK → carinin %20'si uygulanır → 800.
        var y = await Satis(db).SatisFisi(Fis(k, 1_000m, 800m), k.Islem);

        Assert.Equal(800m, Alan(y, "toplamTl"));
        Assert.Equal(200m, Alan(y, "fisIskontoTutari"));
        Assert.True((bool)y.GetType().GetProperty("fisIskontoCaridenMi")!.GetValue(y)!);
    }

    /// <summary>
    /// "0" ile "boş" AYRI anlamlardır: açıkça 0 gönderildiğinde carinin oranı
    /// UYGULANMAZ — aksi hâlde kasiyerin "indirim yok" kararı yok sayılırdı.
    /// </summary>
    [Fact]
    public async Task Acikca_sifir_gonderilirse_CARININ_orani_UYGULANMAZ()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P16 sıfır iskonto");
        var cari = sistem.Cariler.Single(c => c.Id == k.CariId);
        cari.IskontoYuzde = 20m;
        sistem.SaveChanges();

        using var db = vt.Olustur(k);
        var y = await Satis(db).SatisFisi(Fis(k, 1_000m, 1_000m, fisIskonto: 0m), k.Islem);

        Assert.Equal(1_000m, Alan(y, "toplamTl"));
        Assert.Equal(0m, Alan(y, "fisIskontoTutari"));
    }

    // ── B-31 — PASİF CARİ SATIŞTA ───────────────────────────────────────────

    [Fact]
    public async Task Pasif_cariyle_SATIS_FISI_REDDEDILIR()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P16 pasif cari fiş");
        var cari = sistem.Cariler.Single(c => c.Id == k.CariId);
        cari.Aktif = false;
        sistem.SaveChanges();

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(Fis(k, 1_000m, 1_000m), k.Islem));
        Assert.Contains("Pasif cariyle", ex.Message);
    }

    [Fact]
    public async Task Pasif_cariyle_TEK_KALEMLI_SATIS_da_REDDEDILIR()
    {
        // Ekran gizlemesine güvenilmez: doğrudan API çağrısı da kapalıdır.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P16 pasif cari satış");
        var cari = sistem.Cariler.Single(c => c.Id == k.CariId);
        cari.Aktif = false;
        sistem.SaveChanges();

        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 100m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
    }

    [Fact]
    public async Task Aktif_cariyle_satis_ETKILENMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 aktif cari satış");
        using var db = vt.Olustur(k);
        var y = await Satis(db).SatisFisi(Fis(k, 1_000m, 1_000m), k.Islem);
        Assert.Equal(1_000m, Alan(y, "toplamTl"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 4) VERİTABANI — CARİ KARTI (Y-27) VE ÜRÜN KARTI (Y-26) ALANLARI
// ─────────────────────────────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P16KartAlanlariTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));
    private static MUrun.UrunServisi Urun(KuyumcuDbContext db) => new(db);

    // Cari servisi anonim nesne ("id"), ürün servisi VARLIĞIN KENDİSİNİ ("Id")
    // döndürüyor — iki yol da desteklenir, test sözleşmeyi zorlamaz.
    private static string IdOku(object y) =>
        (string)(y.GetType().GetProperty("id") ?? y.GetType().GetProperty("Id")!).GetValue(y)!;

    [Fact]
    public async Task Cari_kimlik_alanlari_YAZILIR_ve_geri_OKUNUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 cari kart");
        using var db = vt.Olustur(k);

        var y = await Cari(db).Olustur(new MCari.CariOlusturDto
        {
            Ad = "Ayşe Yılmaz", Tur = "MUSTERI",
            VergiNo = "12345678950", KimlikTuru = KimlikNo.Tckn,
            Unvan = "Ayşe Kuyumculuk Ltd. Şti.",
            DogumTarihi = new DateOnly(1985, 4, 12),
            Uyruk = "TUR", IskontoYuzde = 7.5m, Segment = CariSegmentleri.Vip,   // KESIF-P2 (#22): alpha-3
        }, k.Islem);

        var id = IdOku(y);
        using var kontrol = vt.OlusturSistem();
        var c = kontrol.Cariler.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal("Ayşe Kuyumculuk Ltd. Şti.", c.Unvan);
        Assert.Equal(KimlikNo.Tckn, c.KimlikTuru);
        Assert.Equal(new DateOnly(1985, 4, 12), c.DogumTarihi);
        Assert.Equal("TUR", c.Uyruk);
        Assert.Equal(7.5m, c.IskontoYuzde);
        Assert.Equal(CariSegmentleri.Vip, c.Segment);
    }

    [Fact]
    public async Task Cari_olustururken_TCKN_hane_kurali_SERVISTE_de_isler()
    {
        // ⚠ DataAnnotations yalnız controller yolunda koşar; servis doğrudan
        //   çağrıldığında kural atlanırdı. Bu test o boşluğu kapatır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 tckn kapısı");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).Olustur(new MCari.CariOlusturDto
            {
                Ad = "Kısa TCKN", Tur = "MUSTERI",
                VergiNo = "1234567890", KimlikTuru = KimlikNo.Tckn,
            }, k.Islem));
        Assert.Contains("11 hane", ex.Message);
    }

    [Fact]
    public async Task Cari_kimlik_turu_beyan_edilmezse_uzunluktan_TURETILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 tür türetme");
        using var db = vt.Olustur(k);

        var y = await Cari(db).Olustur(new MCari.CariOlusturDto
        {
            Ad = "Türsüz Kart", Tur = "MUSTERI", VergiNo = "1234567890",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(KimlikNo.Vkn, kontrol.Cariler.AsNoTracking().Single(x => x.Id == IdOku(y)).KimlikTuru);
    }

    [Fact]
    public async Task Cari_PATCH_gonderilmeyen_alani_KORUR_null_geleni_SILER()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 cari patch");
        using var db = vt.Olustur(k);

        var y = await Cari(db).Olustur(new MCari.CariOlusturDto
        {
            Ad = "Patch Testi", Tur = "MUSTERI",
            Unvan = "Eski Ünvan", Segment = CariSegmentleri.Toptan, IskontoYuzde = 5m,
        }, k.Islem);
        var id = IdOku(y);

        // Yalnız segment gönderilir: ünvan ve iskonto KORUNMALI.
        await Cari(db).Guncelle(id, new MCari.CariGuncelleDto { Segment = CariSegmentleri.Vip }, k.Islem);
        using (var kontrol = vt.OlusturSistem())
        {
            var c = kontrol.Cariler.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal("Eski Ünvan", c.Unvan);
            Assert.Equal(5m, c.IskontoYuzde);
            Assert.Equal(CariSegmentleri.Vip, c.Segment);
        }

        // Açıkça null gönderilir: SİLİNMELİ.
        await Cari(db).Guncelle(id, new MCari.CariGuncelleDto { Unvan = null, IskontoYuzde = null }, k.Islem);
        using (var kontrol = vt.OlusturSistem())
        {
            var c = kontrol.Cariler.AsNoTracking().Single(x => x.Id == id);
            Assert.Null(c.Unvan);
            Assert.Null(c.IskontoYuzde);
            Assert.Equal(CariSegmentleri.Vip, c.Segment);   // dokunulmayan alan yerinde
        }
    }

    [Fact]
    public async Task Urun_kart_alanlari_YAZILIR_ve_PATCH_ile_DUZELTILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 ürün kart");
        using var db = vt.Olustur(k);

        var y = await Urun(db).Olustur(new MUrun.UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "P16-U1", Ad = "Tek taş yüzük", AyarId = "K22",
            GramAgirlik = 5m,
            TedarikciId = k.CariId, AlisMaliyeti = 12_500.50m, HedefMarj = 22.5m,
            KdvOrani = 20m, SeriNo = "SN-001", Olcu = "17", MinStok = 3m,
        }, k.Islem);
        var id = IdOku(y);

        using (var kontrol = vt.OlusturSistem())
        {
            var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal(k.CariId, u.TedarikciId);
            Assert.Equal(12_500.50m, u.AlisMaliyeti);
            Assert.Equal(22.5m, u.HedefMarj);
            Assert.Equal(20m, u.KdvOrani);
            Assert.Equal("SN-001", u.SeriNo);
            Assert.Equal("17", u.Olcu);
            Assert.Equal(3m, u.MinStok);
        }

        // ÜÇ DURUMLU PATCH: gönderilmeyen korunur, null silinir.
        await Urun(db).Guncelle(id, new MUrun.UrunGuncelleDto { AlisMaliyeti = 9_000m }, k.Islem);
        using (var kontrol = vt.OlusturSistem())
        {
            var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal(9_000m, u.AlisMaliyeti);
            Assert.Equal("SN-001", u.SeriNo);       // gönderilmedi → korundu
        }

        await Urun(db).Guncelle(id, new MUrun.UrunGuncelleDto { SeriNo = null, TedarikciId = null }, k.Islem);
        using (var kontrol = vt.OlusturSistem())
        {
            var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == id);
            Assert.Null(u.SeriNo);
            Assert.Null(u.TedarikciId);
            Assert.Equal(9_000m, u.AlisMaliyeti);   // dokunulmayan alan yerinde
        }
    }

    [Fact]
    public async Task Baska_kiracinin_carisi_TEDARIKCI_olarak_yazilamaz()
    {
        // K1 kapısı: yabancı kimlik doğrulanmadan kolona yazılamaz.
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P16 tedarikçi A");
        var b = TestKurulum.KiraciKur(sistem, "P16 tedarikçi B");

        using var db = vt.Olustur(a);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Urun(db).Olustur(new MUrun.UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = "P16-U2", Ad = "Sızıntı denemesi", AyarId = "K22",
                GramAgirlik = 5m, TedarikciId = b.CariId,
            }, a.Islem));
    }

    [Fact]
    public async Task Alis_maliyeti_girilmeyen_urun_SIFIR_MALIYET_sayilmaz()
    {
        // Anayasa md.1: bilinmeyen tahmin edilmez. Kolon NULL kalır; kârlılık
        // raporu (P-25) bu satırı "maliyeti bilinmiyor" diye ayırabilsin.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P16 maliyet yok");
        using var db = vt.Olustur(k);

        var y = await Urun(db).Olustur(new MUrun.UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "P16-U3", Ad = "Maliyetsiz", AyarId = "K22", GramAgirlik = 5m,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Null(kontrol.Urunler.AsNoTracking().Single(x => x.Id == IdOku(y)).AlisMaliyeti);
    }
}
