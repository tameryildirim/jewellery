using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Masak;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÇOKLU BİRİM CARİ BAKİYE — paket 27 Faz A.
//
// Üçüncü paralel defter: borç hangi para biriminde doğduysa o para biriminde
// durur, aynı para biriminde kapanır, TL ve has defterlerine KARIŞMAZ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CariDovizBakiyeTestleri(TestVeritabani vt)
{
    private const decimal UsdKur = 40m;
    private const decimal EurKur = 44m;

    private static MCari.CariServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    /// <summary>Kurulu kiracı + USD/EUR kuru (GBP BİLİNÇLİ olarak boş — fail-closed sınanır).</summary>
    private Kiraci Kur(string ad, decimal? gbp = null)
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        TestKurulum.KurDoviz(db, k.KurId, UsdKur, EurKur, gbp);
        return k;
    }

    private static MCari.DovizVeresiyeDto Veresiye(string pb, decimal tutar, Kiraci k, MasakBilgiDto? masak = null)
        => new() { ParaBirimi = pb, Tutar = tutar, KurSnapshotId = k.KurId, Masak = masak };

    private static MCari.DovizTahsilatDto Tahsilat(string pb, decimal tutar, string kasaId, Kiraci k)
        => new() { ParaBirimi = pb, Tutar = tutar, KasaId = kasaId, KurSnapshotId = k.KurId };

    /// <summary>Anonim yanıtın ham JSON metni — alan adları camelCase gelir.</summary>
    private static string Json(object cevap) =>
        System.Text.Json.JsonSerializer.Serialize(cevap);

    private static CariBakiye Bakiye(KuyumcuDbContext db, string cariId) =>
        db.CariBakiyeler.AsNoTracking().Single(x => x.CariId == cariId);

    // ── 1. Döviz veresiye ───────────────────────────────────────────────────

    [Fact]
    public async Task Doviz_veresiye_usd_kolonunu_artirir_TL_ve_has_DEGISMEZ()
    {
        var k = Kur("Döviz veresiye USD");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 1_000m, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var b = Bakiye(kontrol, k.CariId);
        Assert.Equal(1_000m, b.UsdBakiye);
        Assert.Equal(0m, b.TlBakiye);            // TL defteri KİRLENMEDİ
        Assert.Equal(0m, b.HasGramBakiye);
        Assert.Equal(0m, b.EurBakiye);
        Assert.Equal(0m, b.GbpBakiye);

        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_VERESIYE");
        Assert.Equal("USD", h.ParaBirimi);
        Assert.Equal(1_000m, h.DovizTutar);                       // İŞARETLİ +
        Assert.Equal(1_000m * UsdKur, h.TlTutari);                // TL karşılığı sunucuda
        // K27-E DEĞİŞMEZİ: döviz satırı TL BORCU DEĞİLDİR.
        Assert.Equal(0m, h.VeresiyeTutar);
        Assert.Equal(0m, h.NakitTutar);
        Assert.Null(h.VadeTarihi);
    }

    /// <summary>TL karşılığı 2 haneye AwayFromZero yuvarlanır ve kur ×  miktar'dır.</summary>
    [Fact]
    public async Task Doviz_hareketinde_TlTutari_kur_carpimi_ve_iki_hanedir()
    {
        var k = Kur("Döviz yuvarlama");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("EUR", 123.45m, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_VERESIYE");
        Assert.Equal(Math.Round(123.45m * EurKur, 2, MidpointRounding.AwayFromZero), h.TlTutari);
        Assert.Equal(123.45m, h.DovizTutar);
        Assert.Equal("EUR", h.ParaBirimi);
    }

    // ── 2. Döviz tahsilat ───────────────────────────────────────────────────

    [Fact]
    public async Task Doviz_tahsilat_bakiyeyi_azaltir_ve_kasaya_EFEKTIF_yazar()
    {
        var k = Kur("Döviz tahsilat");
        string usdKasa;
        using (var s = vt.OlusturSistem())
            usdKasa = TestKurulum.KasaEkle(s, k.TenantId, k.SubeId, "USD", 0m, "USD Kasa");

        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 1_000m, k), k.Islem);
        await Servis(db).DovizTahsilatGir(k.CariId, Tahsilat("USD", 400m, usdKasa, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(600m, Bakiye(kontrol, k.CariId).UsdBakiye);

        // ⚠ Kasaya EFEKTİF yazılır — TL karşılığı (16.000) DEĞİL (M84).
        Assert.Equal(400m, TestKurulum.KasaBakiye(kontrol, usdKasa));
        // TL kasası DOKUNULMADI.
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));

        var kh = kontrol.KasaHareketleri.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.KasaId == usdKasa);
        Assert.Equal("USD", kh.ParaBirimi);
        Assert.Equal(400m, kh.Tutar);
        Assert.Equal("DOVIZ", kh.OdemeTuru);

        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_TAHSILAT");
        Assert.Equal(-400m, h.DovizTutar);                       // İŞARETLİ −
        Assert.Equal(-400m * UsdKur, h.TlTutari);
        // Efektif döviz NAKİTTİR (A12): nakit payı TL karşılığıdır (M82).
        Assert.Equal(400m * UsdKur, h.NakitTutar);
        Assert.Equal(0m, h.VeresiyeTutar);
    }

    /// <summary>K6: efektif YALNIZ o para birimindeki kasaya girer (M83).</summary>
    [Fact]
    public async Task Doviz_tahsilatinda_TL_kasasi_REDDEDILIR()
    {
        var k = Kur("Döviz kasa kapısı");
        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).DovizTahsilatGir(k.CariId, Tahsilat("USD", 100m, k.KasaId, k), k.Islem));
        Assert.Contains("USD", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_TAHSILAT"));
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    /// <summary>Gün kapanışı kilidi döviz tahsilatında da işler.</summary>
    [Fact]
    public async Task Gun_kapanmis_kasaya_doviz_tahsilati_REDDEDILIR()
    {
        var k = Kur("Döviz gün kilidi");
        string usdKasa;
        using (var s = vt.OlusturSistem())
        {
            usdKasa = TestKurulum.KasaEkle(s, k.TenantId, k.SubeId, "USD");
            s.GunKapanislari.Add(new GunKapanisi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, KasaId = usdKasa,
                YerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow)),
                DefterBakiye = 0m, SayilanNakit = 0m, Fark = 0m,
                KapatanKullaniciId = k.KullaniciId, KapanisZamani = DateTime.UtcNow,
                Olusturuldu = DateTime.UtcNow,
            });
            s.SaveChanges();
        }

        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAnyAsync<Exception>(
            () => Servis(db).DovizTahsilatGir(k.CariId, Tahsilat("USD", 100m, usdKasa, k), k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_TAHSILAT"));
    }

    // ── 3. K27-E: döviz satırı TL borcu SAYILMAZ ────────────────────────────

    /// <summary>
    /// ⚠ BU TESTİN ÖLÇTÜĞÜ ŞEY (M78/M79): döviz satırının TlTutari'si doludur;
    /// türü "TAHSILAT" olsaydı ya da veresiye payı yazılsaydı, aynı borç HEM
    /// döviz kolonunda HEM TL vade defterinde sayılırdı (çifte sayma).
    /// Cariye ÖNCE gerçek bir TL borcu yazılır ki AcikBorclar erken dönmesin.
    /// </summary>
    [Fact]
    public async Task Doviz_satiri_AcikBorclara_HIC_girmez_TL_partisi_bozulmaz()
    {
        var k = Kur("Döviz vade");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = 1_000m, VadeGun = 30, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 500m, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1_000m, Bakiye(kontrol, k.CariId).TlBakiye);   // TL bakiyesi DEĞİŞMEDİ
        Assert.Equal(500m, Bakiye(kontrol, k.CariId).UsdBakiye);

        var (borclar, vadesiz) = await Kuyumcu.Api.Modules.Vade.VadeKapisi.AcikBorclar(
            kontrol, k.TenantId, k.CariId, DateTime.UtcNow);

        // TEK parti, TAM 1.000 TL; döviz satırı ne parti açtı ne parti tüketti.
        var tek = Assert.Single(borclar);
        Assert.Equal(1_000m, tek.AcikTutar);
        Assert.Equal(0m, vadesiz);
    }

    /// <summary>Fabrika kapısı: döviz satırına veresiye payı yazılamaz (K27-E).</summary>
    [Fact]
    public void HareketFabrikasi_doviz_satirinda_veresiye_payini_REDDEDER()
    {
        var k = Kur("Fabrika döviz kapısı");
        using var db = vt.Olustur(k.TenantId);
        var hata = Assert.Throws<InvalidOperationException>(() => HareketFabrikasi.Ekle(db,
            OdemeIzi.Veresiyeden(100m, DateOnly.FromDateTime(DateTime.UtcNow)),
            new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
                Tur = "DOVIZ_VERESIYE", Tarih = DateTime.UtcNow, TlTutari = 100m,
                ParaBirimi = "USD", DovizTutar = 100m, KurSnapshotId = k.KurId,
                CariId = k.CariId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
            }));
        Assert.Contains("K27-E", hata.Message);
    }

    // ── 4. Para birimleri bağımsız · çapraz kapatma yok ─────────────────────

    [Fact]
    public async Task Iki_para_birimi_ayni_caride_BAGIMSIZ_kolonlar_karismaz()
    {
        var k = Kur("İki para birimi");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 1_000m, k), k.Islem);
        // EUR tarafında BİZ borçluyuz: düzeltme fişiyle negatif delta.
        await Servis(db).Duzeltme(k.CariId, new MCari.CariDuzeltmeDto
        {
            Gerekce = "Eski sistemden devreden EUR borcumuz — mutabakat 2026-09",
            DovizParaBirimi = "EUR", DovizDelta = -300m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var b = Bakiye(kontrol, k.CariId);
        Assert.Equal(1_000m, b.UsdBakiye);
        Assert.Equal(-300m, b.EurBakiye);
        Assert.Equal(0m, b.GbpBakiye);
        Assert.Equal(0m, b.TlBakiye);
    }

    /// <summary>
    /// K27-G: USD borcuna EUR tahsilatı çapraz kapatma YAPMAZ. EUR kolonu
    /// eksiye düşer (biz o kadar EUR aldık), USD borcu OLDUĞU GİBİ kalır.
    /// </summary>
    [Fact]
    public async Task Capraz_kapatma_denemesi_USD_borcunu_DEGISTIRMEZ()
    {
        var k = Kur("Çapraz kapatma");
        string eurKasa;
        using (var s = vt.OlusturSistem())
            eurKasa = TestKurulum.KasaEkle(s, k.TenantId, k.SubeId, "EUR");

        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 1_000m, k), k.Islem);
        await Servis(db).DovizTahsilatGir(k.CariId, Tahsilat("EUR", 200m, eurKasa, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var b = Bakiye(kontrol, k.CariId);
        Assert.Equal(1_000m, b.UsdBakiye);    // AYNEN duruyor
        Assert.Equal(-200m, b.EurBakiye);
    }

    // ── 5. Fail-closed kapılar ──────────────────────────────────────────────

    /// <summary>Kur kaydında GBP yoksa GBP veresiye REDDEDİLİR; hiçbir satır yazılmaz (M77).</summary>
    [Fact]
    public async Task GBP_kuru_yokken_GBP_veresiye_REDDEDILIR_ve_hicbir_satir_yazilmaz()
    {
        var k = Kur("GBP kur yok");     // gbp = null
        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("GBP", 100m, k), k.Islem));
        Assert.Contains("GBP", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
        Assert.Equal(0m, Bakiye(kontrol, k.CariId).GbpBakiye);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("CHF")]
    [InlineData("usd")]
    public async Task Gecersiz_para_birimiyle_doviz_ucu_REDDEDILIR(string pb)
    {
        var k = Kur($"Geçersiz PB {pb}");
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).DovizVeresiyeGir(k.CariId, Veresiye(pb, 100m, k), k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
    }

    // ── 6. Kredi limiti (K27-I) ─────────────────────────────────────────────

    /// <summary>
    /// Limit 10.000 TL; cari 500 USD borçlu (kur 40 → 20.000 TL karşılığı).
    /// Yeni TL veresiye limitten REDDEDİLİR (M80).
    /// </summary>
    [Fact]
    public async Task Kredi_limiti_doviz_borcunun_TL_karsiligini_GORUR()
    {
        var k = Kur("Limit döviz");
        using var db = vt.Olustur(k.TenantId);
        // Önce döviz borcu doğar (limit henüz yok — devir bakiyesi senaryosu),
        // SONRA limit konur. Limit, doğmuş döviz borcunu da GÖRMEK zorundadır.
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 500m, k), k.Islem);
        using (var s = vt.OlusturSistem()) { s.Cariler.Single(c => c.Id == k.CariId).KrediLimitiTl = 10_000m; s.SaveChanges(); }

        // Yeni bağlam: limitin güncel hâli okunsun (önceki bağlam cariyi izliyordu).
        using var db2 = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db2).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
            {
                TlTutari = 100m, KurSnapshotId = k.KurId,
            }, k.Islem));
        // Mesaj döviz karşılığını AÇIKÇA söyler — kullanıcı neden reddedildiğini bilir.
        Assert.Contains("20000", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, Bakiye(kontrol, k.CariId).TlBakiye);
    }

    /// <summary>
    /// Döviz bakiyesi VAR ama o para biriminin kuru YOK → limit kapısı REDDEDER,
    /// sessizce "döviz borcu 0" saymaz (M81).
    /// </summary>
    [Fact]
    public async Task Doviz_bakiyesi_varken_kuru_olmayan_kurla_limit_kapisi_REDDEDER()
    {
        var k = Kur("Limit kur yok");
        using (var s = vt.OlusturSistem()) { s.Cariler.Single(c => c.Id == k.CariId).KrediLimitiTl = 1_000_000m; s.SaveChanges(); }

        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 10m, k), k.Islem);

        // Kur kaydından USD kuru KALDIRILIR (ör. eksik beslenen yeni snapshot).
        using (var s = vt.OlusturSistem()) TestKurulum.KurDoviz(s, k.KurId, usd: null, eur: EurKur);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
            {
                TlTutari = 100m, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("USD", hata.Message);
    }

    // ── 7. Düzeltme fişi (açılış bakiyesi) ──────────────────────────────────

    [Fact]
    public async Task Duzeltme_fisiyle_acilis_USD_bakiyesi_girilir_ve_denetim_izinde_para_birimi_gorunur()
    {
        const string gerekce = "Eski programdan devreden USD açılış bakiyesi — mutabakat";
        var k = Kur("Açılış USD");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Duzeltme(k.CariId, new MCari.CariDuzeltmeDto
        {
            Gerekce = gerekce, DovizParaBirimi = "USD", DovizDelta = 2_500m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2_500m, Bakiye(kontrol, k.CariId).UsdBakiye);
        Assert.Equal(0m, Bakiye(kontrol, k.CariId).TlBakiye);   // TL kirlenmedi

        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "CARI_DUZELTME");
        Assert.Equal("USD", h.ParaBirimi);
        Assert.Equal(2_500m, h.DovizTutar);
        Assert.Equal(0m, h.TlTutari);          // düzeltmede TL karşılığı YAZILMAZ

        var iz = Assert.Single(kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "CariDuzeltme").ToList());
        Assert.True(iz.Hassas);
        Assert.Contains("USD=+2500", iz.Detay);
        Assert.Contains(gerekce, iz.Detay);
    }

    /// <summary>
    /// Üç deltadan biri doluyken "sıfır delta" reddi ARTIK TETİKLENMEZ (M88);
    /// ama kısa gerekçe hâlâ reddedilir ve yarım döviz gövdesi de reddedilir.
    /// </summary>
    [Fact]
    public async Task Duzeltmede_kisa_gerekce_ve_yarim_doviz_govdesi_REDDEDILIR()
    {
        var k = Kur("Düzeltme kapıları");
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Duzeltme(k.CariId, new MCari.CariDuzeltmeDto
        {
            Gerekce = "kısa", DovizParaBirimi = "USD", DovizDelta = 100m, KurSnapshotId = k.KurId,
        }, k.Islem));

        // Para birimi verildi ama delta yok → sessizce yok sayılmaz.
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Duzeltme(k.CariId, new MCari.CariDuzeltmeDto
        {
            Gerekce = "Yeterince uzun bir gerekçe metni buraya yazıldı",
            DovizParaBirimi = "USD", KurSnapshotId = k.KurId,
        }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
    }

    // ── 8. Kiracı izolasyonu ────────────────────────────────────────────────

    [Fact]
    public async Task Kiraci_A_nin_doviz_bakiyesi_B_den_okunamaz_ve_yazilamaz()
    {
        var a = Kur("Döviz kiracı A");
        var b = Kur("Döviz kiracı B");

        using (var dbA = vt.Olustur(a.TenantId))
            await Servis(dbA).DovizVeresiyeGir(a.CariId, Veresiye("USD", 750m, a), a.Islem);

        using var dbB = vt.Olustur(b.TenantId);
        // B, A'nın cari kimliğiyle yazmayı denerse cari BULUNAMAZ (K1 kapısı).
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(dbB).DovizVeresiyeGir(a.CariId, Veresiye("USD", 100m, b), b.Islem));
        // B'nin bağlamında A'nın bakiye satırı GÖRÜNMEZ (global sorgu filtresi).
        Assert.Empty(dbB.CariBakiyeler.AsNoTracking().Where(x => x.CariId == a.CariId).ToList());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(750m, Bakiye(kontrol, a.CariId).UsdBakiye);   // bozulmadı
    }

    // ── 9. MASAK ve tevsik (TL karşılığı üzerinden) ─────────────────────────

    /// <summary>
    /// MASAK eşiği TL karşılığından işler (M85): 5.000 USD × 40 = 200.000 TL >
    /// 185.000 TL eşiği → kimlik tespiti olmadan REDDEDİLİR. Efektif miktarla
    /// (5.000) karşılaştırılsaydı eşik hiç tetiklenmezdi.
    /// </summary>
    [Fact]
    public async Task MASAK_esigini_asan_doviz_veresiye_kimlik_tespiti_olmadan_REDDEDILIR()
    {
        var k = Kur("MASAK döviz");
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 5_000m, k), k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
        Assert.Equal(0m, Bakiye(kontrol, k.CariId).UsdBakiye);
    }

    /// <summary>
    /// 459 s. VUK tevsik haddi: efektif döviz NAKİTTİR ve TL karşılığı hadde
    /// sayılır (M82). 1.000 USD × 40 = 40.000 TL > 30.000 TL → red.
    /// </summary>
    [Fact]
    public async Task Tevsik_haddini_asan_efektif_doviz_tahsilati_REDDEDILIR()
    {
        var k = Kur("Tevsik döviz");
        string usdKasa;
        using (var s = vt.OlusturSistem())
            usdKasa = TestKurulum.KasaEkle(s, k.TenantId, k.SubeId, "USD");

        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).DovizTahsilatGir(k.CariId, Tahsilat("USD", 1_000m, usdKasa, k), k.Islem));
        Assert.Contains("30", hata.Message);   // haddi söyleyen mesaj

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_TAHSILAT"));
        Assert.Equal(0m, TestKurulum.KasaBakiye(kontrol, usdKasa));
    }

    // ── 10. Raporlar: bilgi SAKLANMAZ ───────────────────────────────────────

    /// <summary>
    /// Yaşlandırmada döviz bakiyeli cari GÖRÜNÜR ve rozet alanı doludur;
    /// vade takibinde ise GÖRÜNMEZ (TL borcu yok — Faz A'da döviz vadesizdir).
    /// </summary>
    [Fact]
    public async Task Doviz_bakiyeli_cari_yaslandirmada_GORUNUR_vade_takibinde_GORUNMEZ()
    {
        var k = Kur("Rapor döviz");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).DovizVeresiyeGir(k.CariId, Veresiye("USD", 900m, k), k.Islem);

        using var kontrol = vt.Olustur(k.TenantId);
        var rapor = TestKurulum.RaporServisi(kontrol);

        var yaslandirma = await rapor.CariYaslandirma(k.Islem);
        var satirlar = Json(yaslandirma);
        Assert.Contains(k.CariId, satirlar);
        Assert.Contains("\"dovizBakiyesiVar\":true", satirlar);

        var vade = await rapor.VadeTakibi(k.Islem, 30);
        Assert.DoesNotContain(k.CariId, Json(vade));
    }
}
