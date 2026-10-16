using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT sayıdır (üretim
// kodundan hesaplanmaz). Tablo veritabanından değil, şartname § 3.1'deki
// birincil kaynak değerleriyle elle kurulur — tohum yanlışsa bu dosya değil
// K-1 testleri kırılır; bu dosya KURALIN tarih/sınır davranışını sınar.
// Kapsam: tevsik haddi (7.000 → 30.000 geçişi, 30.11.2024 yerel), MASAK
// kimlik eşiği (85.000 → 185.000, 14.01.2023), fail-closed, ekran biçimi.
// ════════════════════════════════════════════════════════════════════════
public sealed class YASAL1SinirTestleri
{
    private static YasalParametreSatiri S(string anahtar, decimal? deger, DateOnly bas, DateOnly? bit,
        string birim = YasalBirim.Tl) =>
        new($"d-{anahtar}-{bas.DayNumber}", anahtar, deger, null, birim, bas, bit,
            "dogrulayici", "https://dogrulayici", YasalTeyit.Dogrulandi, "dogrulayici");

    private static YasalParametreServisi Servis() => YasalParametreServisi.Yukleyicili(() =>
    [
        S(YasalAnahtar.TevsikHaddiTl, 7_000m, new DateOnly(2016, 1, 1), new DateOnly(2024, 11, 30)),
        S(YasalAnahtar.TevsikHaddiTl, 30_000m, new DateOnly(2024, 11, 30), null),
        S(YasalAnahtar.MasakKimlikEsigiTl, 85_000m, new DateOnly(2022, 6, 16), new DateOnly(2023, 1, 14)),
        S(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new DateOnly(2023, 1, 14), null),
        S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakSibIsGunu, 10m, new DateOnly(2008, 4, 1), null, YasalBirim.IsGunu),
        S(YasalAnahtar.MasakSaklamaYil, 8m, new DateOnly(2008, 4, 1), null, YasalBirim.Yil),
        S(YasalAnahtar.StopajEsnafMuafHurdaOran, 0.02m, new DateOnly(2009, 2, 3), null, YasalBirim.Oran),
        S(YasalAnahtar.KmtsKodDeseni, null, new DateOnly(2026, 9, 30), null, YasalBirim.Kod),
    ]);

    private static MTevsik.TevsikKurali Tevsik() => new(new MTevsik.TevsikEsikleri(), Servis());
    private static MMasak.MasakKuralMotoru Masak() => new(new MMasak.MasakEsikleri(), Servis());

    // 2024-11-29 yerel öğlen (UTC+3) ve 2024-11-30 yerel öğlen.
    private static readonly DateTime EskiGun = new(2024, 11, 29, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YeniGun = new(2024, 11, 30, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Sifir_nakit_her_iki_donemde_tevsik_GEREKMEZ()
    {
        Assert.False(Tevsik().Degerlendir(0m, 0m, null, EskiGun).Gerekli);
        Assert.False(Tevsik().Degerlendir(0m, 0m, null, YeniGun).Gerekli);
    }

    [Fact]
    public void Eksi_nakit_tediye_de_hadde_tabi_isaret_mutlak_deger_7000_donemi()
    {
        var r = Tevsik().Degerlendir(0m, -7_000.01m, null, EskiGun);
        Assert.True(r.Gerekli);
        Assert.Equal(7_000m, r.Had);
        Assert.Equal(0.01m, r.Asim);
        Assert.False(Tevsik().Degerlendir(0m, -7_000m, null, EskiGun).Gerekli);
    }

    [Fact]
    public void Kurus_siniri_eski_donem_7000_00_gecer_7000_01_takilir_yeni_donemde_ikisi_de_gecer()
    {
        Assert.False(Tevsik().Degerlendir(0m, 7_000.00m, null, EskiGun).Gerekli);
        Assert.True(Tevsik().Degerlendir(0m, 7_000.01m, null, EskiGun).Gerekli);
        Assert.False(Tevsik().Degerlendir(0m, 7_000.01m, null, YeniGun).Gerekli);
        Assert.False(Tevsik().Degerlendir(0m, 30_000.00m, null, YeniGun).Gerekli);
        var r = Tevsik().Degerlendir(0m, 30_000.01m, null, YeniGun);
        Assert.True(r.Gerekli);
        Assert.Equal(30_000m, r.Had);
        Assert.Equal(0.01m, r.Asim);
    }

    [Fact]
    public void Kumulatif_kurus_siniri_29_999_99_arti_0_01_gecer_arti_0_02_takilir()
    {
        Assert.False(Tevsik().Degerlendir(29_999.99m, 0.01m, null, YeniGun).Gerekli);
        Assert.True(Tevsik().Degerlendir(29_999.99m, 0.02m, null, YeniGun).Gerekli);
    }

    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99_asim_kurusu_kurusuna()
    {
        var r = Tevsik().Degerlendir(0m, 999_999_999.99m, null, YeniGun);
        Assert.True(r.Gerekli);
        Assert.Equal(999_969_999.99m, r.Asim);

        var m = Masak().Gereksinim(999_999_999.99m, new DateOnly(2026, 9, 30));
        Assert.True(m.EkBelgeGerekli);
    }

    [Fact]
    public void MASAK_kimlik_esigi_islemin_gunuyle_184_999_99_ve_85_000()
    {
        var yeni = new DateOnly(2023, 1, 14);
        var eski = new DateOnly(2023, 1, 13);
        Assert.False(Masak().Gereksinim(184_999.99m, yeni).KimlikGerekli);
        Assert.True(Masak().Gereksinim(185_000m, yeni).KimlikGerekli);
        Assert.True(Masak().Gereksinim(85_000m, eski).KimlikGerekli);
        Assert.False(Masak().Gereksinim(84_999.99m, eski).KimlikGerekli);
        Assert.True(Masak().Gereksinim(-185_000m, yeni).KimlikGerekli);   // alış da eşiğe tabi
    }

    [Fact]
    public void Bos_null_tanimsiz_gun_ve_degersiz_satir_FAIL_CLOSED_503()
    {
        var s = Servis();
        Assert.Equal(503, Assert.Throws<YasalParametreYokException>(
            () => s.Sayi(YasalAnahtar.TevsikHaddiTl, new DateOnly(2015, 12, 31))).StatusCode);
        Assert.Equal(503, Assert.Throws<YasalParametreYokException>(
            () => s.Sayi(YasalAnahtar.KmtsKodDeseni, new DateOnly(2026, 9, 30))).StatusCode);
        Assert.Equal(503, Assert.Throws<YasalParametreYokException>(
            () => s.Sayi("", new DateOnly(2026, 9, 30))).StatusCode);
        // Tevsik kuralı tanımsız günde varsayılana (30.000) DÜŞMEZ.
        Assert.Throws<YasalParametreYokException>(() => Tevsik().Degerlendir(0m, 1m, null,
            new DateTime(2015, 12, 31, 9, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Oran_alti_hane_hassasiyet_kaybi_yok_0_02()
    {
        Assert.Equal(0.02m, Servis().Sayi(YasalAnahtar.StopajEsnafMuafHurdaOran, new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public void Ekran_bicimi_tr_TR_kurus_ve_oran()
    {
        var m = YasalSinirSozlugu.Al("tr");
        Assert.Equal("2.000.000,01\u00A0₺", YasalBicim.Deger(2_000_000.01m, null, YasalBirim.Tl, m));
        Assert.Equal("999.999.999,99\u00A0₺", YasalBicim.Deger(999_999_999.99m, null, YasalBirim.Tl, m));
        Assert.Equal("0\u00A0₺", YasalBicim.Deger(0m, null, YasalBirim.Tl, m));
        Assert.Equal("0,01\u00A0₺", YasalBicim.Deger(0.005m, null, YasalBirim.Tl, m));   // AwayFromZero
        Assert.Equal("0\u00A0₺", YasalBicim.Deger(0.004m, null, YasalBirim.Tl, m));
        Assert.Equal("%2", YasalBicim.Deger(0.02m, null, YasalBirim.Oran, m));
        Assert.DoesNotContain("TRY", YasalBicim.Deger(30_000m, null, YasalBirim.Tl, m));
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — HASIM DOĞRULAYICI TUR 2 SINIR TESTLERİ (Aşkın + Mehmet,
// 30.09 ikinci doğrulama). Beklenen değerler SABİT; üretim kodundan
// hesaplanmaz. Kapsam: sıfır değer (0 geçerli değerdir, 503 değil), eksi
// tutarın mutlak değerle kademelenmesi, gün sınırında kuruş (>= / >),
// 999.999.999,99, dönem bölme, saklama yılının kayıt günü, sistem ucunda
// eksi/kesirli değer.
// ════════════════════════════════════════════════════════════════════════
public sealed class YASAL1SinirTestleriTur2
{
    private static YasalParametreSatiri S(string anahtar, decimal? deger, DateOnly bas, DateOnly? bit,
        string birim = YasalBirim.Tl) =>
        new($"d2-{anahtar}-{bas.DayNumber}", anahtar, deger, null, birim, bas, bit,
            "dogrulayici2", "https://dogrulayici2", YasalTeyit.Dogrulandi, "dogrulayici2");

    private static YasalParametreServisi Servis() => YasalParametreServisi.Yukleyicili(() =>
    [
        S(YasalAnahtar.TevsikHaddiTl, 7_000m, new DateOnly(2016, 1, 1), new DateOnly(2024, 11, 30)),
        S(YasalAnahtar.TevsikHaddiTl, 30_000m, new DateOnly(2024, 11, 30), null),
        S(YasalAnahtar.MasakKimlikEsigiTl, 85_000m, new DateOnly(2022, 6, 16), new DateOnly(2023, 1, 14)),
        S(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new DateOnly(2023, 1, 14), null),
        S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakSibIsGunu, 10m, new DateOnly(2008, 4, 1), null, YasalBirim.IsGunu),
        S(YasalAnahtar.MasakSaklamaYil, 8m, new DateOnly(2008, 4, 1), null, YasalBirim.Yil),
        S(YasalAnahtar.StopajNihaiTuketiciOran, 0m, new DateOnly(2009, 2, 3), null, YasalBirim.Oran),
    ]);

    private static MMasak.MasakKuralMotoru Masak() => new(new MMasak.MasakEsikleri(), Servis());

    [Fact]
    public void Sifir_deger_GECERLI_degerdir_503_degil()
    {
        Assert.Equal(0m, Servis().Sayi(YasalAnahtar.StopajNihaiTuketiciOran, new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public void Eksi_tutar_mutlak_degerle_kademelenir_alis_da_esige_tabi()
    {
        var m = Masak();
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(-185_000m, new DateOnly(2026, 9, 30)).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(-184_999.99m, new DateOnly(2026, 9, 30)).Kademe);
    }

    [Fact]
    public void Gun_sinirinda_kurus_eski_gun_85_000_00_kimlik_ister_yeni_gun_184_999_99_istemez()
    {
        var m = Masak();
        // 2023-01-13 yerel son gün: eşik 85.000 (>=)
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(85_000.00m, new DateOnly(2023, 1, 13)).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(84_999.99m, new DateOnly(2023, 1, 13)).Kademe);
        // 2023-01-14 yerel ilk gün: eşik 185.000
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(184_999.99m, new DateOnly(2023, 1, 14)).Kademe);
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(185_000.00m, new DateOnly(2023, 1, 14)).Kademe);
    }

    [Fact]
    public void Tevsik_yerel_gece_yarisi_30_000_00_gecer_30_000_01_takilir()
    {
        var k = new MTevsik.TevsikKurali(new MTevsik.TevsikEsikleri(), Servis());
        var yerelGeceYarisi = new DateTime(2024, 11, 29, 21, 0, 0, DateTimeKind.Utc); // 30.11 00:00 yerel
        Assert.False(k.Degerlendir(0m, 30_000.00m, null, yerelGeceYarisi).Gerekli);
        var r = k.Degerlendir(0m, 30_000.01m, null, yerelGeceYarisi);
        Assert.True(r.Gerekli);
        Assert.Equal(0.01m, r.Asim);
        // Bir saniye önce (29.11 23:59:59 yerel) 7.000 haddi: 7.000,01 takılır.
        var r2 = k.Degerlendir(0m, 7_000.01m, null, yerelGeceYarisi.AddSeconds(-1));
        Assert.True(r2.Gerekli);
        Assert.Equal(7_000m, r2.Had);
    }

    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99_en_ust_kademe()
    {
        Assert.Equal(BeyanKademesi.BeyanFormuVeBelge,
            Masak().Gereksinim(999_999_999.99m, new DateOnly(2026, 9, 30)).Kademe);
    }

    [Fact]
    public void Donemler_gun_sinirinda_bolunur_bitis_HARIC()
    {
        var d = Servis().Donemler(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 20));
        Assert.Equal(2, d.Count);
        Assert.Equal((new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 13), 85_000m), d[0]);
        Assert.Equal((new DateOnly(2023, 1, 14), new DateOnly(2023, 1, 20), 185_000m), d[1]);
        // Aralığın bir günü bile tanımsızsa 503 (boşluk bugünkü değerle doldurulmaz).
        Assert.Throws<YasalParametreYokException>(() =>
            Servis().Donemler(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2022, 6, 15), new DateOnly(2022, 6, 20)));
    }

    [Fact]
    public void Null_bos_anahtar_503()
    {
        Assert.Throws<YasalParametreYokException>(() => Servis().Sayi("", new DateOnly(2026, 9, 30)));
        Assert.Throws<YasalParametreYokException>(() => Servis().Sayi(YasalAnahtar.KmtsKodDeseni, new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public void Saklama_bitisi_kayit_gununun_yiliyla_8_yil()
    {
        var t = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2032, 3, 1, 10, 0, 0, DateTimeKind.Utc), Masak().SaklamaBitisi(t));
    }
}

[Collection("http")]
public sealed class YASAL1SinirHttpTestleri(HttpFikstur f)
{
    private static object Govde(string anahtar, decimal deger, DateOnly bas) => new
    {
        anahtar, degerSayi = deger, baslangic = bas.ToString("yyyy-MM-dd"),
        dayanak = "dogrulayici tur 2", kaynakUrl = "https://www.resmigazete.gov.tr/dogrulayici",
        teyit = "TEYITSIZ", aciklamaSade = "dogrulayici", gerekce = "Hasım doğrulayıcı eksi değer sınır denemesi",
    };

    [Fact]
    public async Task Sistem_ucu_EKSI_had_400_alan_isaretli_ve_satir_yazilmaz()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var yarin = YasalParametreServisi.IslemGunu(DateTime.UtcNow).AddDays(9);
        var c = await sis.PostAsJsonAsync("/api/v1/sistem/yasal-parametreler",
            Govde(YasalAnahtar.TevsikHaddiTl, -1m, yarin));
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, c.StatusCode);
        var g = await c.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(g.GetProperty("alanlar").TryGetProperty("degerSayi", out _));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.YasalParametreler.Count(p => p.Anahtar == YasalAnahtar.TevsikHaddiTl && p.DegerSayi == -1m));
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — HASIM DOĞRULAYICI TUR 3 (2026-09-30, Aşkın + Mehmet).
// Beklenenler SABİT sayılar; tablo şartname § 3.1 zincirinden elle kurulur.
// Kapsam: MASAK kimlik eşiği zinciri (20k→75k→85k→185k) dönem bölme,
// ilk satırdan önceki gün fail-closed, açılış eşitlik denetiminde ölçek,
// eşik ekran dili sınırları, tevsik 7.000 döneminde kuruş sınırı,
// 999.999.999,99 tutar, yaklaşan ufkunun 7/8 gün sınırı.
// ════════════════════════════════════════════════════════════════════════
public sealed class YASAL1SinirTestleriTur3
{
    private sealed class SabitSaat(DateTimeOffset an) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => an;
    }

    private static YasalParametreSatiri S(string anahtar, decimal? deger, DateOnly bas, DateOnly? bit) =>
        new($"d3-{anahtar}-{bas.DayNumber}", anahtar, deger, null, YasalBirim.Tl, bas, bit,
            "dogrulayici", "https://dogrulayici", YasalTeyit.Dogrulandi, "dogrulayici");

    private static List<YasalParametreSatiri> Zincir() =>
    [
        S(YasalAnahtar.MasakKimlikEsigiTl, 20_000m, new DateOnly(2008, 4, 1), new DateOnly(2021, 2, 24)),
        S(YasalAnahtar.MasakKimlikEsigiTl, 75_000m, new DateOnly(2021, 2, 24), new DateOnly(2022, 6, 16)),
        S(YasalAnahtar.MasakKimlikEsigiTl, 85_000m, new DateOnly(2022, 6, 16), new DateOnly(2023, 1, 14)),
        S(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new DateOnly(2023, 1, 14), null),
        S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakSibIsGunu, 10m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.MasakSaklamaYil, 8m, new DateOnly(2008, 4, 1), null),
        S(YasalAnahtar.TevsikHaddiTl, 7_000m, new DateOnly(2016, 1, 1), new DateOnly(2024, 11, 30)),
        S(YasalAnahtar.TevsikHaddiTl, 30_000m, new DateOnly(2024, 11, 30), null),
    ];

    // 2026-09-30 yerel 12:00 (UTC 09:00).
    private static YasalParametreServisi Servis(List<YasalParametreSatiri>? ek = null) =>
        YasalParametreServisi.Yukleyicili(() => [.. Zincir(), .. ek ?? []],
            new SabitSaat(new DateTimeOffset(2026, 9, 30, 9, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Donemler_MASAK_zinciri_dort_doneme_bolunur_sinirlar_bitis_HARIC()
    {
        var d = Servis().Donemler(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2021, 1, 1), new DateOnly(2023, 12, 31));
        Assert.Equal(4, d.Count);
        Assert.Equal((new DateOnly(2021, 1, 1), new DateOnly(2021, 2, 23), 20_000m), d[0]);
        Assert.Equal((new DateOnly(2021, 2, 24), new DateOnly(2022, 6, 15), 75_000m), d[1]);
        Assert.Equal((new DateOnly(2022, 6, 16), new DateOnly(2023, 1, 13), 85_000m), d[2]);
        Assert.Equal((new DateOnly(2023, 1, 14), new DateOnly(2023, 12, 31), 185_000m), d[3]);
    }

    [Fact]
    public void Donemler_ilk_satirdan_ONCEKI_gunu_kapsarsa_FAIL_CLOSED_503_bugunku_degerle_doldurmaz()
    {
        var ex = Assert.Throws<YasalParametreYokException>(() =>
            Servis().Donemler(YasalAnahtar.MasakKimlikEsigiTl, new DateOnly(2008, 3, 31), new DateOnly(2008, 4, 2)));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal(new DateOnly(2008, 3, 31), ex.Gun);
    }

    [Fact]
    public void MASAK_80_000_gecis_gununde_75_000_doneminde_kimlik_ISTER_85_000_doneminde_ISTEMEZ()
    {
        var m = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri(), Servis());
        Assert.True(m.Gereksinim(80_000m, new DateOnly(2022, 6, 15)).KimlikGerekli);
        Assert.False(m.Gereksinim(80_000m, new DateOnly(2022, 6, 16)).KimlikGerekli);
        Assert.True(m.Gereksinim(75_000m, new DateOnly(2021, 2, 24)).KimlikGerekli);
        Assert.False(m.Gereksinim(74_999.99m, new DateOnly(2021, 2, 24)).KimlikGerekli);
        Assert.True(m.Gereksinim(20_000m, new DateOnly(2021, 2, 23)).KimlikGerekli);
    }

    [Fact]
    public void Acilis_esitlik_denetimi_olcek_farkini_fark_saymaz_kurus_farkini_sayar()
    {
        var s = Servis();
        Assert.Empty(s.AcilisFarklari(new Dictionary<string, decimal>
        {
            [YasalAnahtar.TevsikHaddiTl] = 30000.000000m,
            [YasalAnahtar.MasakBeyanFormuEsigiTl] = 2000000.010m,
        }));
        var f = s.AcilisFarklari(new Dictionary<string, decimal> { [YasalAnahtar.TevsikHaddiTl] = 30_000.01m });
        Assert.Single(f);
        Assert.Contains("TEVSIK_HADDI_TL", f[0]);
        // Tabloda bugün satırı olmayan anahtar fark sayılmaz (işlem anında 503 verir).
        Assert.Empty(s.AcilisFarklari(new Dictionary<string, decimal> { [YasalAnahtar.GiderPusulasiGun] = 7m }));
    }

    [Fact]
    public void Esik_gosterimi_sinirlari_sifir_kurus_yarim_kurus_ve_esik_olmayan()
    {
        Assert.Equal(("USTU", 2_000_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m));
        Assert.Equal(("VE_USTU", 185_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 185_000m));
        Assert.Equal(("VE_USTU", 0m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 0m));
        // Yarım kuruş (0,005) ve 0,02 ne "üzeri" ne "ve üzeri": ham değer basılır.
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 185_000.005m));
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 185_000.02m));
        // Tevsik > had ile karşılaştırılır → eşik dili UYGULANMAZ.
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.TevsikHaddiTl, 30_000m));
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, null));
    }

    [Fact]
    public void Tevsik_7000_doneminde_kumulatif_kurus_siniri_ve_999_999_999_99()
    {
        var k = new MTevsik.TevsikKurali(new MTevsik.TevsikEsikleri(), Servis());
        var eski = new DateTime(2024, 11, 29, 20, 59, 59, DateTimeKind.Utc); // yerel 23:59:59, 7.000 dönemi
        Assert.False(k.Degerlendir(6_999.99m, 0.01m, null, eski).Gerekli);
        var r = k.Degerlendir(6_999.99m, 0.02m, null, eski);
        Assert.True(r.Gerekli);
        Assert.Equal(7_000m, r.Had);
        Assert.Equal(0.01m, r.Asim);
        var b = k.Degerlendir(0m, 999_999_999.99m, null, eski);
        Assert.Equal(999_992_999.99m, b.Asim);
        var y = k.Degerlendir(0m, -999_999_999.99m, null, new DateTime(2024, 11, 29, 21, 0, 0, DateTimeKind.Utc));
        Assert.Equal(30_000m, y.Had);
        Assert.Equal(999_969_999.99m, y.Asim);
    }

    [Fact]
    public void Yaklasan_ufku_7_gun_DAHIL_8_gun_HARIC_bugun_baslayan_HARIC()
    {
        var bugun = new DateOnly(2026, 9, 30);
        var s = Servis(
        [
            S(YasalAnahtar.MasakSibIsGunu + "_X7", 1m, bugun.AddDays(7), null),
            S(YasalAnahtar.MasakSibIsGunu + "_X8", 1m, bugun.AddDays(8), null),
            S(YasalAnahtar.MasakSibIsGunu + "_X0", 1m, bugun, null),
        ]);
        Assert.Equal(bugun, s.Bugun);
        var y = s.Yaklasan(bugun, 7).Select(x => x.Yeni.Anahtar).ToList();
        Assert.Equal(["MASAK_SIB_IS_GUNU_X7"], y);
    }
}
