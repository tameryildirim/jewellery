using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — tarihli yasal parametreler: saf + veritabanı testleri.
// Kabul: K-1 (tohum değerleri + tanımsız gün 503) · K-2 (yerel gün sınırı) ·
// K-4b (gece tazelemesi, yeniden başlatmasız) · K-6 (açık uçlu çakışma) ·
// K-11 (tohum DOGRULANDI sayısı + kaynak) · K-13 (risk: işlem gününün eşiği).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Test saati — yalnız GetUtcNow ezilir.</summary>
public sealed class SahteSaat(DateTimeOffset baslangic) : TimeProvider
{
    public DateTimeOffset Simdi { get; set; } = baslangic;
    public override DateTimeOffset GetUtcNow() => Simdi;
}

[Collection("veritabani")]
public sealed class YasalParametreTestleri(TestVeritabani vt)
{
    private List<YasalParametreSatiri> TohumSatirlari()
    {
        using var db = vt.OlusturSistem();
        return db.YasalParametreler.AsNoTracking()
            .Where(p => p.Id.StartsWith("yasal1-tohum-") || p.Id.StartsWith("masak370-"))   // MASAK-370 migration satırı
            .AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList();
    }

    private static YasalParametreSatiri Satir(
        string anahtar, decimal deger, DateOnly bas, DateOnly? bit, string id = "t") =>
        new(id + anahtar + bas.DayNumber, anahtar, deger, null, YasalBirim.Tl, bas, bit,
            "test", "https://test", YasalTeyit.Dogrulandi, "test");

    // ── K-1 ──────────────────────────────────────────────────────────────────

    [Fact]
    public void K1_tohum_tevsik_haddi_29_11_2024_7000_30_11_2024_30000_2015_tanimsiz_503()
    {
        var s = YasalParametreServisi.Yukleyicili(TohumSatirlari);

        Assert.Equal(7_000m, s.Sayi(YasalAnahtar.TevsikHaddiTl, new DateOnly(2024, 11, 29)));
        Assert.Equal(30_000m, s.Sayi(YasalAnahtar.TevsikHaddiTl, new DateOnly(2024, 11, 30)));
        Assert.Equal(7_000m, s.Sayi(YasalAnahtar.TevsikHaddiTl, new DateOnly(2016, 1, 1)));

        var ex = Assert.Throws<YasalParametreYokException>(
            () => s.Sayi(YasalAnahtar.TevsikHaddiTl, new DateOnly(2015, 12, 31)));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("Yasal sınır tanımlı değil; destek ile görüşün.", ex.Message);
    }

    [Fact]
    public void K1_tohum_MASAK_kimlik_esigi_donemleri_ve_bugunku_degerler_yapilandirmayla_ayni()
    {
        var s = YasalParametreServisi.Yukleyicili(TohumSatirlari);
        Assert.Equal(85_000m, s.Sayi(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2023, 1, 13)));
        Assert.Equal(185_000m, s.Sayi(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2023, 1, 14)));
        // MASAK-370: 185.000 dönemi 06.10.2026 dahil biter (bitiş HARİÇ = 07.10.2026); 370.000 07.10.2026'da başlar.
        Assert.Equal(185_000m, s.Sayi(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2026, 10, 6)));
        Assert.Equal(370_000m, s.Sayi(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2026, 10, 7)));

        // K-12 ön koşulu: bugünkü tablo değerleri eski yapılandırma varsayılanlarıyla BİREBİR.
        var bugun = s.Bugun;
        var m = new MMasak.MasakEsikleri();
        Assert.Equal(new MTevsik.TevsikEsikleri().Had, s.Sayi(YasalAnahtar.TevsikHaddiTl, bugun));
        Assert.Equal(m.KimlikEsigi, s.Sayi(YasalAnahtar.MasakKimlikEsigiTl, bugun));
        Assert.Equal(m.AmacBeyaniEsigi, s.Sayi(YasalAnahtar.MasakAmacBeyaniEsigiTl, bugun));
        Assert.Equal(m.BeyanFormuEsigi, s.Sayi(YasalAnahtar.MasakBeyanFormuEsigiTl, bugun));
        Assert.Equal(m.EkBelgeEsigi, s.Sayi(YasalAnahtar.MasakEkBelgeEsigiTl, bugun));
        Assert.Equal(m.SupheliBildirimIsGunu, s.Sayi(YasalAnahtar.MasakSibIsGunu, bugun));
        Assert.Equal(m.SaklamaYili, s.Sayi(YasalAnahtar.MasakSaklamaYil, bugun));
        var h = new Kuyumcu.Api.Modules.Hurda.HurdaAyarlari();
        Assert.Equal(h.PusulaDuzenlemeGunu, s.Sayi(YasalAnahtar.GiderPusulasiGun, bugun));
        Assert.Equal(h.EsnafMuafStopajOran, s.Sayi(YasalAnahtar.StopajEsnafMuafHurdaOran, bugun));
    }

    // ── K-2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public void K2_sinir_29_11_2024_23_59_yerel_7000_ile_30_11_2024_00_00_yerel_30000_ile()
    {
        var s = YasalParametreServisi.Yukleyicili(TohumSatirlari);
        var kural = new MTevsik.TevsikKurali(new MTevsik.TevsikEsikleri(), s);

        // İstanbul UTC+3: yerel 23:59:59 = UTC 20:59:59; yerel 00:00 = UTC 21:00.
        var once = new DateTime(2024, 11, 29, 20, 59, 59, DateTimeKind.Utc);
        var sonra = new DateTime(2024, 11, 29, 21, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2024, 11, 29), YasalParametreServisi.IslemGunu(once));
        Assert.Equal(new DateOnly(2024, 11, 30), YasalParametreServisi.IslemGunu(sonra));

        var r1 = kural.Degerlendir(0m, 8_000m, null, once);
        Assert.True(r1.Gerekli);
        Assert.Equal(7_000m, r1.Had);

        var r2 = kural.Degerlendir(0m, 8_000m, null, sonra);
        Assert.False(r2.Gerekli);
        Assert.Equal(30_000m, r2.Had);
    }

    [Fact]
    public void K2_bitis_gunu_HARIC_baslangic_gunu_DAHIL()
    {
        var a = new DateOnly(2030, 1, 1);
        var s = YasalParametreServisi.Yukleyicili(() =>
        [
            Satir("X_TL", 1m, new DateOnly(2029, 1, 1), a),
            Satir("X_TL", 2m, a, null),
        ]);
        Assert.Equal(1m, s.Sayi("X_TL", a.AddDays(-1)));
        Assert.Equal(2m, s.Sayi("X_TL", a));
    }

    // ── K-4b — gece tazelemesi (yeniden başlatma yok) ───────────────────────

    [Fact]
    public void K4_gun_degisince_onbellek_tazelenir_yeni_satir_yeniden_baslatmasiz_gorunur()
    {
        var bugun = new DateOnly(2031, 3, 10);
        var saat = new SahteSaat(new DateTimeOffset(2031, 3, 10, 20, 0, 0, TimeSpan.Zero)); // yerel 23:00
        var satirlar = new List<YasalParametreSatiri> { Satir("Y_TL", 30_000m, new DateOnly(2030, 1, 1), null) };
        var yukleme = 0;
        var s = YasalParametreServisi.Yukleyicili(() => { yukleme++; return satirlar.ToList(); }, saat);

        Assert.Equal(30_000m, s.Sayi("Y_TL", bugun));
        Assert.Equal(1, yukleme);

        // Başka bir örnek (ya da migration) ileri tarihli satır yazdı; bu örneğin önbelleği bilmiyor.
        satirlar[0] = satirlar[0] with { Bitis = bugun.AddDays(1) };
        satirlar.Add(Satir("Y_TL", 40_000m, bugun.AddDays(1), null));

        // Aynı gün: önbellek yeniden okunmaz.
        Assert.Equal(30_000m, s.Sayi("Y_TL", bugun.AddDays(1)));

        // Yerel gece yarısı geçti (UTC 21:00) → ilk erişimde tablo yeniden okunur.
        saat.Simdi = new DateTimeOffset(2031, 3, 10, 21, 0, 1, TimeSpan.Zero);
        Assert.Equal(bugun.AddDays(1), s.Bugun);
        Assert.Equal(40_000m, s.Sayi("Y_TL", s.Bugun));
        Assert.Equal(2, yukleme);
    }

    // ── K-6 — aynı anahtara çakışan iki açık uçlu satır yazılamaz ──────────

    [Fact]
    public void K6_ayni_anahtarda_iki_ACIK_UCLU_satir_veritabaninda_REDDEDILIR()
    {
        var anahtar = "K6_TEST_" + Kimlik.Ulid()[..10];
        using var db = vt.OlusturSistem();
        YasalParametre Yeni(DateOnly bas) => new()
        {
            Id = Kimlik.Ulid(), Anahtar = anahtar, DegerSayi = 1m, Birim = YasalBirim.Tl,
            GecerlilikBaslangic = bas, GecerlilikBitis = null, Dayanak = "test", KaynakUrl = "https://test",
            Teyit = YasalTeyit.Teyitsiz, AciklamaSade = "test", Olusturan = "test", Olusturuldu = DateTime.UtcNow,
        };
        db.YasalParametreler.Add(Yeni(new DateOnly(2030, 1, 1)));
        db.SaveChanges();

        db.YasalParametreler.Add(Yeni(new DateOnly(2031, 1, 1)));
        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.Contains("ux_yasal_param_acik_uc", ex.InnerException?.Message ?? ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, kontrol.YasalParametreler.Count(p => p.Anahtar == anahtar && p.GecerlilikBitis == null));
        // Test satırı paylaşılan test veritabanında kalmasın.
        kontrol.Database.ExecuteSqlRaw("DELETE FROM yasal_parametreler WHERE anahtar = {0}", anahtar);
    }

    // ── K-11 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void K11_tohum_DOGRULANDI_satir_sayisi_10_ve_her_satirda_birincil_kaynak_https()
    {
        // MASAK-370: 21 tohum + 1 migration satırı (masak370-01, DOGRULANDI, RG 07.10.2026/33393).
        var t = TohumSatirlari();
        Assert.Equal(22, t.Count);
        Assert.Equal(10, t.Count(s => s.Teyit == YasalTeyit.Dogrulandi));
        Assert.All(t, s => Assert.StartsWith("https://", s.KaynakUrl));
        // Tur 2: DOĞRULANDI satırda resmî atıf ZORUNLU; teyitsiz satırda atıf boş kalabilir
        // (ekran "henüz doğrulanmadı" yazar, iç not ic_not'ta).
        Assert.All(t.Where(s => s.Teyit == YasalTeyit.Dogrulandi), s => Assert.False(string.IsNullOrWhiteSpace(s.Dayanak)));
        // Beyaz liste dışı anahtar tohumlanmadı.
        Assert.All(t, s => Assert.True(YasalAnahtar.Birimler.ContainsKey(s.Anahtar), s.Anahtar));
        // Her anahtarda en çok bir açık uçlu satır.
        Assert.All(t.Where(s => s.Bitis is null).GroupBy(s => s.Anahtar), g => Assert.Single(g));
    }

    // ── K-13 — risk: her işlem KENDİ GÜNÜNÜN eşiğiyle ───────────────────────

    [Fact]
    public async Task K13_risk_esige_yakin_sayimi_islemin_GUNUNDEKI_kimlik_esigiyle_yapilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL1-TEST-risk");
        using var db = vt.Olustur(k);

        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var degisim = bugun.AddDays(-10);
        // 20 gün önce eşik 185.000 (band 148.000–185.000) → 150.000 YAKIN.
        // 10 gün önce eşik 400.000'e çıktı (band 320.000–400.000) → 150.000 YAKIN DEĞİL.
        var yasal = YasalParametreServisi.Yukleyicili(() =>
        [
            Satir(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new DateOnly(2000, 1, 1), degisim),
            Satir(YasalAnahtar.MasakKimlikEsigiTl, 400_000m, degisim, null),
        ]);

        var simdi = DateTime.UtcNow;
        void Ekle(DateTime tarih) => HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(150_000m), new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = tarih, TlTutari = 150_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = simdi,
        });
        Ekle(Zaman.YerelGunBasiUtc(bugun.AddDays(-20)).AddHours(12));
        Ekle(Zaman.YerelGunBasiUtc(bugun.AddDays(-5)).AddHours(12));
        await db.SaveChangesAsync();

        var servis = new MMasak.RiskServisi(db, new MMasak.MasakRiskAyarlari(), new MMasak.MasakEsikleri(),
            new DenetimServisi(db, new HttpContextAccessor()), yasal);
        var kok = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(await servis.Hesapla(k.CariId, k.Islem))).RootElement;

        Assert.Equal(1, kok.GetProperty("girdiOzeti").GetProperty("esigeYakinIslem").GetInt32());
    }

    // ── K-13 ek (tur 2, hasım DÜŞÜK-4): geç girilen/içe aktarılan kayıt ────────

    [Fact]
    public async Task K13_ek_pencere_ICINDE_olusturulan_ama_Tarihi_pencereden_ESKI_islem_KENDI_gununun_esigini_alir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL1-TEST-risk2");
        using var db = vt.Olustur(k);

        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        // Eşik PENCERE BAŞINDAN (90 gün) ÖNCE, 100 gün önce 185.000 → 400.000 oldu.
        var degisim = bugun.AddDays(-100);
        var yasal = YasalParametreServisi.Yukleyicili(() =>
        [
            Satir(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new DateOnly(2000, 1, 1), degisim),
            Satir(YasalAnahtar.MasakKimlikEsigiTl, 400_000m, degisim, null),
        ]);

        var simdi = DateTime.UtcNow;
        void Ekle(DateTime tarih) => HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(150_000m), new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = tarih, TlTutari = 150_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = simdi,   // pencere İÇİNDE girildi (içe aktarım / geç giriş)
        });
        // 120 gün önceki işlem: KENDİ gününde eşik 185.000 → 150.000 YAKIN.
        // (Eski davranış: pencere başının eşiği 400.000 → YAKIN DEĞİL sayılıyordu.)
        Ekle(Zaman.YerelGunBasiUtc(bugun.AddDays(-120)).AddHours(12));
        // 5 gün önceki işlem: eşik 400.000 → YAKIN DEĞİL.
        Ekle(Zaman.YerelGunBasiUtc(bugun.AddDays(-5)).AddHours(12));
        await db.SaveChangesAsync();

        var servis = new MMasak.RiskServisi(db, new MMasak.MasakRiskAyarlari { DegerlendirmePencereGunu = 90 },
            new MMasak.MasakEsikleri(), new DenetimServisi(db, new HttpContextAccessor()), yasal);
        var kok = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(await servis.Hesapla(k.CariId, k.Islem))).RootElement;

        Assert.Equal(1, kok.GetProperty("girdiOzeti").GetProperty("esigeYakinIslem").GetInt32());
    }
}
