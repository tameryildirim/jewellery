using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET YASAL-PARAM-EKRAN finansal sınır testleri.
// Beklenen değerler SABİT (üretim kodundan hesaplanmaz). Saat 08.10.2026 09:00 UTC;
// tablo: kimlik 185.000 (14.01.2023–07.10.2026) → 370.000 (07.10.2026–), amaç 200.000.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASALPARAMEKRANSinirTestleri
{
    private static readonly DateOnly Gecis = new(2026, 10, 7);

    private static YasalParametreSatiri S(string anahtar, decimal deger, DateOnly bas, DateOnly? bit = null) =>
        new("d-" + anahtar + bas.DayNumber, anahtar, deger, null, "TL", bas, bit, "d", "https://x", "DOGRULANDI", "a");

    private static MMasak.MasakKapisi Kapi() =>
        new(new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri(), YasalParametreServisi.Yukleyicili(() =>
        [
            S(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new(2023, 1, 14), Gecis),
            S(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, Gecis),
            S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m, new(2008, 4, 1)),
            S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new(2008, 4, 1)),
            S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new(2008, 4, 1)),
            S(YasalAnahtar.MasakSibIsGunu, 10m, new(2008, 4, 1)),
            S(YasalAnahtar.MasakSaklamaYil, 8m, new(2008, 4, 1)),
        ], new SahteSaat(new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero)))));

    // ── sıfır ──
    [Fact]
    public void Sifir_tutar_kademe_YOK_sifir_esige_inis_SIKILASTIRMA()
    {
        Assert.Equal(BeyanKademesi.Yok, Kapi().Dogrula(0m, null).Kademe);
        Assert.Equal("SIKILASTIRMA", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, 0m));
    }

    // ── eksi (iade/alış): işaret eşiği gevşetmez ──
    [Fact]
    public void Eksi_tutar_mutlak_degerle_esige_tabi()
    {
        Assert.Equal(BeyanKademesi.Yok, Kapi().Dogrula(-199_999.99m, null).Kademe);
        var ex = Assert.Throws<BadRequestException>(() => Kapi().Dogrula(-200_000m, null));
        Assert.Contains("200.000", ex.Message);
    }

    // ── kuruş / 0,005 sınırı: eşik >= karşılaştırması yuvarlamasız ──
    [Fact]
    public void Kurus_sinirlari_esik_alti_ve_ustu()
    {
        var k = Kapi();
        Assert.Equal(BeyanKademesi.Yok, k.Dogrula(199_999.99m, null).Kademe);
        Assert.Equal(BeyanKademesi.Yok, k.Dogrula(199_999.995m, null).Kademe);   // 0,005 yukarı yuvarlanıp eşiğe ÇIKARILMAZ
        Assert.Throws<BadRequestException>(() => k.Dogrula(200_000m, null));     // eşik dahil
        // Ekran biçimi: kuruş AwayFromZero (Bicim.Tl) — 0,005 → 0,01.
        var b = Kuyumcu.Web.Servisler.Bicim.Tl(199_999.985m);
        Assert.Contains("199.999,99", b);
        Assert.DoesNotContain("TRY", b);
    }

    // ── dönem geçişi: işlem günü verilirse o günün eşiği ──
    [Fact]
    public void Donem_gecisi_06_10_185000_08_10_200000()
    {
        var k = Kapi();
        Assert.Throws<BadRequestException>(() => k.Dogrula(185_000m, null, esikGunu: new DateOnly(2026, 10, 6)));
        Assert.Equal(BeyanKademesi.Yok, k.Dogrula(184_999.99m, null, esikGunu: new DateOnly(2026, 10, 6)).Kademe);
        Assert.Equal(BeyanKademesi.Yok, k.Dogrula(185_000m, null).Kademe);
    }

    // ── çok büyük tutar ──
    [Fact]
    public void Cok_buyuk_tutar_en_ust_kademe_ve_yon_gevsetme()
    {
        var g = Assert.Throws<BadRequestException>(() => Kapi().Dogrula(999_999_999.99m, null));
        Assert.StartsWith("MASAK:", g.Message);
        Assert.Equal("GEVSETME", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, 999_999_999.99m));
    }

    // ── boş / null ──
    [Fact]
    public void Bos_null_degerlerde_yon_DEGISIKLIK_kural_tek_cumle()
    {
        Assert.Equal("DEGISIKLIK", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, null, 370_000m));
        Assert.Equal("DEGISIKLIK", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, null));
        Assert.Equal("AYNI", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, 370_000m));
        // Amaç kademesi yok (0) → yalnız kimlik cümlesi.
        Assert.Equal("Kimlik 370000 ve üstünde zorunlu (MASAK).",
            YasalParamSozlugu.KimlikKuraliMetni("tr", 370_000m, 0m, t => t.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal("Kimlik 370000 ve üstünde zorunlu (MASAK). 200000 ve üstünde işlem amacı beyanıyla birlikte de kimlik istenir (mali müşavir teyidi bekleniyor).",
            YasalParamSozlugu.KimlikKuraliMetni("tr", 370_000m, 200_000m, t => t.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    // ── Türkçe giriş (formun Sayı alanı) ──
    [Fact]
    public void Turkce_giris_45000_1virgul5_belirsiz_1500()
    {
        Assert.Equal(370_000m, FormAlani.SayiCoz("370.000").Deger);
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }

    // ── açılış: tablodan okunan anahtar farkı engel DEĞİL ──
    [Fact]
    public void Acilis_tevsik_haddi_farki_uyari_stopaj_farki_engel()
    {
        var s = YasalParametreServisi.Yukleyicili(() =>
        [
            S(YasalAnahtar.TevsikHaddiTl, 30_000m, new(2024, 12, 1)),
            S(YasalAnahtar.StopajNihaiTuketiciOran, 0.01m, new(2008, 4, 1)),
        ], new SahteSaat(new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero)));
        var (engel, uyari) = s.AcilisDegerlendir(new Dictionary<string, decimal>
        {
            [YasalAnahtar.TevsikHaddiTl] = 40_000m,
            [YasalAnahtar.StopajNihaiTuketiciOran] = 0.02m,
        });
        Assert.Single(uyari);
        Assert.Contains("TEVSIK_HADDI_TL", uyari[0]);
        Assert.Single(engel);
        Assert.Contains("STOPAJ_NIHAI_TUKETICI_ORAN", engel[0]);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — TUR 2 (08.10.2026 05:xx). Bugünden geçerli geçiş + akla yatkınlık
// bandı (tur 2'nin yeni kuralı). Tablo: kimlik 370.000 (07.10–08.10) → 300.000 (08.10–);
// amaç kademesi kimliğin ÜSTÜNDE (500.000) ki YalnizKimlik kademesi ölçülebilsin.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASALPARAMEKRANTur2SinirTestleri
{
    private static readonly DateOnly Bugun = new(2026, 10, 8);

    private static YasalParametreSatiri S(string anahtar, decimal deger, DateOnly bas, DateOnly? bit = null) =>
        new("t2-" + anahtar + bas.DayNumber, anahtar, deger, null, "TL", bas, bit, "d", "https://x", "DOGRULANDI", "a");

    private static MMasak.MasakKuralMotoru Motor() => new(new MMasak.MasakEsikleri(), YasalParametreServisi.Yukleyicili(() =>
    [
        S(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, new(2026, 10, 7), Bugun),
        S(YasalAnahtar.MasakKimlikEsigiTl, 300_000m, Bugun),
        S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 500_000m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakSibIsGunu, 10m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakSaklamaYil, 8m, new(2008, 4, 1)),
    ], new SahteSaat(new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero))));

    [Fact]
    public void Sifir_ve_eksi_yeni_deger_akla_yatkin_DEGIL()
    {
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, 0m));
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, -1m));
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, -370_000m));
    }

    [Fact]
    public void Eksi_tutar_bugunku_yeni_esige_mutlak_degerle_tabi()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(-300_000m, Bugun).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(0m, Bugun).Kademe);
    }

    [Fact]
    public void Kurus_siniri_yeni_esigin_bir_kurus_alti_serbest_tam_esik_kimlik()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(299_999.99m, Bugun).Kademe);
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(300_000.00m, Bugun).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(299_999.995m, Bugun).Kademe);   // motor yuvarlamaz
    }

    [Fact]
    public void Bugunden_gecerli_degisiklik_bugun_yeni_dun_eski_esikle()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(350_000m, Bugun).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(350_000m, Bugun.AddDays(-1)).Kademe);
        Assert.Equal(300_000m, m.EsiklerGunu(Bugun).KimlikEsigi);
        Assert.Equal(370_000m, m.EsiklerGunu(Bugun.AddDays(-1)).KimlikEsigi);
    }

    [Fact]
    public void Cok_buyuk_tutar_en_ust_kademe_ve_akla_yatkinlik_disi()
    {
        Assert.Equal(BeyanKademesi.BeyanFormuVeBelge, Motor().Gereksinim(999_999_999.99m, Bugun).Kademe);
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, 999_999_999.99m));
        Assert.True(YasalParametreServisi.BuyukDegisim(370_000m, 999_999_999.99m));
    }

    [Fact]
    public void Null_eski_ya_da_yeni_deger_buyuk_degisim_isaretlenmez()
    {
        Assert.False(YasalParametreServisi.BuyukDegisim(null, 300_000m));
        Assert.False(YasalParametreServisi.BuyukDegisim(370_000m, null));
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    [Theory]
    [InlineData("3700000", true)]
    [InlineData("3700000.01", false)]
    [InlineData("37000", true)]
    [InlineData("36999.99", false)]
    public void Akla_yatkinlik_bandi_kurus_hassasiyetinde(string yeni, bool beklenen) =>
        Assert.Equal(beklenen, YasalParametreServisi.AklaYatkin(370_000m,
            decimal.Parse(yeni, System.Globalization.CultureInfo.InvariantCulture)));
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — TUR 3 (08.10.2026 07:xx). İstemci kademe seçicisi (MasakKademe) ile
// sunucu kuralı (MasakKuralMotoru) AYNI sınırda aynı kararı verir mi; Türkçe giriş; ve
// sistem ucunun KAPALI damgası. Beklenen değerler SABİT. Eşikler: kimlik 370.000, amaç 200.000.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASALPARAMEKRANTur3SinirTestleri
{
    private static readonly Kuyumcu.Contracts.Bilesenler.MasakFormu.Esikler E =
        new(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 10, []);

    private static MMasak.MasakKuralMotoru Motor() => new(new MMasak.MasakEsikleri
    {
        KimlikEsigi = 370_000m, AmacBeyaniEsigi = 200_000m,
        BeyanFormuEsigi = 2_000_000.01m, EkBelgeEsigi = 20_000_000.01m,
    });

    [Theory]
    [InlineData("0", "Yok", "YOK")]
    [InlineData("199999.99", "Yok", "YOK")]
    [InlineData("200000", "Amac", "AMAC_BEYANI")]
    [InlineData("369999.99", "Amac", "AMAC_BEYANI")]
    [InlineData("369999.995", "Amac", "AMAC_BEYANI")]
    [InlineData("370000", "Kimlik", "AMAC_BEYANI")]
    [InlineData("-370000", "Kimlik", "AMAC_BEYANI")]
    [InlineData("-369999.99", "Amac", "AMAC_BEYANI")]
    [InlineData("999999999.99", "Kimlik", "BEYAN_FORMU_VE_BELGE")]
    public void Istemci_kademesi_ve_sunucu_kademesi_sinirlarda(string tutar, string istemci, string sunucu)
    {
        var t = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(istemci, Kuyumcu.Web.Servisler.MasakKademe.Sec(E, null, t).ToString());
        var g = Motor().Gereksinim(t);
        Assert.Equal(sunucu, g.Kademe);
        // Kimliği istemci "gerekmez" diyorsa sunucu da istememeli (aynı sınır).
        Assert.Equal(istemci != "Yok", g.KimlikGerekli);
    }

    [Fact]
    public void Kurus_yuvarlamasi_AwayFromZero_369999_995_fis_tutari_370000_olur_kimlik_istenir()
    {
        // Fiş tutarı projede AwayFromZero ile 2 haneye yuvarlanır: 369.999,995 → 370.000,00 → kimlik.
        var fis = Math.Round(369_999.995m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(370_000.00m, fis);
        Assert.Equal("Kimlik", Kuyumcu.Web.Servisler.MasakKademe.Sec(E, null, fis).ToString());
        Assert.True(Motor().Gereksinim(fis).KimlikGerekli);
    }

    [Fact]
    public void Bos_null_esik_ya_da_gereksinim_sayi_uydurmaz()
    {
        Assert.Equal("Bilinmiyor", Kuyumcu.Web.Servisler.MasakKademe.Sec(null, null, 500_000m).ToString());
        Assert.Equal("Bilinmiyor", Kuyumcu.Web.Servisler.MasakKademe.ToplamKademesi(E, null).ToString());
        // Eski sunucu ölçüyü göndermez (0) → toplamdan sayılmaz.
        var eski = new Kuyumcu.Web.Servisler.MasakGereksinim("AMAC_BEYANI", true, true, false, false, 10);
        Assert.False(eski.ToplamdanMi);
        Assert.Equal(250_000m, Kuyumcu.Web.Servisler.MasakKademe.Olcu(eski, -250_000m));
        // Toplamdan: 150.000 işlem + bugünkü toplam 380.000 → kimlik kademesi (370.000), amaç değil.
        var top = new Kuyumcu.Web.Servisler.MasakGereksinim("AMAC_BEYANI", true, true, false, false, 10) { OlcuTutarTl = 380_000m, IslemTutarTl = 150_000m };
        Assert.Equal("Toplam", Kuyumcu.Web.Servisler.MasakKademe.Sec(E, top, 150_000m).ToString());
        Assert.Equal("Kimlik", Kuyumcu.Web.Servisler.MasakKademe.ToplamKademesi(E, top).ToString());
    }

    [Fact]
    public void Turkce_giris_esik_formu()
    {
        Assert.Equal(370_000.5m, FormAlani.SayiCoz("370.000,50").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(0m, FormAlani.SayiCoz("0").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }

    [Fact]
    public void Akla_yatkinlik_sifir_eksi_ve_cok_buyuk()
    {
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, 0m));
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, -370_000m));
        Assert.False(YasalParametreServisi.AklaYatkin(370_000m, 999_999_999.99m));
        Assert.True(YasalParametreServisi.AklaYatkin(370_000m, 300_000m));
        Assert.Equal("SIKILASTIRMA", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, 300_000m));
        Assert.Equal("GEVSETME", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, 400_000m));
        Assert.Equal("DEGISIKLIK", YasalParametreServisi.Yon(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, null));
    }
}

[Collection("http")]
public sealed class YASALPARAMEKRANTur3HttpSinirTestleri(HttpFikstur f)
{
    private const string Uc = "/api/v1/sistem/yasal-parametreler";
    private static DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);
    private static string G(DateOnly g) => g.ToString("yyyy-MM-dd");

    /// <summary>
    /// KAPALI damgası + DEĞERSİZ satır, programın işlem anında tablodan okuduğu bir EŞİĞE bugünden
    /// yazılırsa o eşik o gün okunamaz (Sayi → 503) ve tüm kiracılarda MASAK kapısı durur.
    /// Beklenen: sistem ucu bunu ÖNİZLEMEDE reddeder (400) — tek kayıtla ülke çapında satış durmasın.
    /// </summary>
    [Fact]
    public async Task KAPALI_degersiz_satir_tablodan_okunan_esige_bugunden_yazilamaz()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var govde = new Dictionary<string, object?>
        {
            ["anahtar"] = YasalAnahtar.MasakEkBelgeEsigiTl, ["degerSayi"] = null, ["teyit"] = "KAPALI",
            ["baslangic"] = G(Bugun.AddDays(2)), ["kaynakUrl"] = "https://www.resmigazete.gov.tr/test",
            ["dayanak"] = "DOGRULAYICI tur 3 test satırı", ["gerekce"] = "Doğrulayıcı KAPALI değersiz satır sınaması",
            ["resmiGazeteTarihi"] = G(Bugun), ["resmiGazeteSayisi"] = "33393",
        };
        govde["baslangic"] = G(Bugun);
        var servis = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        var onceki = servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!;
        System.Net.HttpStatusCode onizleme, kayit = 0, gereksinim = 0;
        try
        {
            var on = await sis.PostAsJsonAsync(Uc + "/onizleme", govde);
            onizleme = on.StatusCode;
            if (on.IsSuccessStatusCode)
            {
                var kod = (await on.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("onayKodu").GetString();
                kayit = (await sis.PostAsJsonAsync(Uc, new Dictionary<string, object?>(govde) { ["onayKodu"] = kod })).StatusCode;
                using var db0 = f.Vt.OlusturSistem();
                var k = TestKurulum.KiraciKur(db0, "PAKETYASAL-PARAM-EKRAN-TEST-dg3");
                gereksinim = (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/masak/gereksinim?tutar=1000")).StatusCode;
            }
        }
        finally
        {
            using var db = f.Vt.OlusturSistem();
            db.Database.ExecuteSqlRaw(
                "DELETE FROM yasal_parametreler WHERE anahtar = 'MASAK_EK_BELGE_ESIGI_TL' AND olusturan LIKE 'sistem:%'");
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET gecerlilik_bitis = NULL WHERE id = {0}", onceki.Id);
            servis.Tazele();
        }
        // Gerçekleşen (doğrulayıcı tur 3): önizleme 200, kayıt 201, ardından 1.000 TL'lik gereksinim sorgusu 503.
        Assert.True(onizleme == System.Net.HttpStatusCode.BadRequest,
            $"onizleme={(int)onizleme} kayit={(int)kayit} gereksinim(1.000 TL)={(int)gereksinim}");
    }
}
