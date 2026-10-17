using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET MASAK-370 finansal sınır testleri.
// Eşikler migration'ın kurduğu GERÇEK yasal_parametreler tablosundan okunur;
// beklenen değerler SABİT sayıdır (üretim kodundan hesaplanmaz).
// Dayanak: Tedbirler Yönetmeliği md.5/1-b (CK 11845, RG 07.10.2026/33393) — 370.000;
// 200.000 amaç beyanı kademesi (TEYİTSİZ) aynen.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MASAK370SinirTestleri(TestVeritabani vt)
{
    private static readonly DateOnly Eski = new(2026, 10, 6);
    private static readonly DateOnly Yeni = new(2026, 10, 7);

    private MMasak.MasakKuralMotoru Motor()
    {
        List<YasalParametreSatiri> Satirlar()
        {
            using var db = vt.OlusturSistem();
            return db.YasalParametreler.AsNoTracking().AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList();
        }
        return new(new MMasak.MasakEsikleri(), YasalParametreServisi.Yukleyicili(Satirlar));
    }

    [Fact]
    public void D1_sifir_tutar_her_iki_donemde_kademe_YOK_dogrulama_uygun()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(0m, Eski).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(0m, Yeni).Kademe);
        Assert.True(m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 0m }, Yeni).Uygun);
    }

    [Fact]
    public void D2_eksi_tutar_mutlak_degerle_olculur_alis_da_esige_tabi()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(-199_999.99m, Yeni).Kademe);
        Assert.Equal(BeyanKademesi.AmacBeyani, m.Gereksinim(-200_000m, Yeni).Kademe);
        Assert.True(m.Gereksinim(-370_000m, Yeni).KimlikGerekli);
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(-185_000m, Eski).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(-185_000m, Yeni).Kademe);
    }

    [Fact]
    public void D3_kurus_siniri_motor_ham_degeri_olcer_199999_99_YOK_200000_00_KIMLIK()
    {
        var m = Motor();
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(199_999.99m, Yeni).Kademe);
        Assert.True(m.Gereksinim(200_000.00m, Yeni).KimlikGerekli);
        Assert.Equal(BeyanKademesi.AmacBeyani, m.Gereksinim(369_999.99m, Yeni).Kademe);
        Assert.Equal(BeyanKademesi.AmacBeyani, m.Gereksinim(370_000.00m, Yeni).Kademe);   // 370k ≥ 200k: amaç kademesi kimliği de ister
        Assert.True(m.Gereksinim(370_000.00m, Yeni).KimlikGerekli);
        // Eski dönem tam sınır: 184.999,99 yok, 185.000,00 yalnız kimlik.
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(184_999.99m, Eski).Kademe);
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(185_000.00m, Eski).Kademe);
    }

    [Fact]
    public void D4_cok_buyuk_tutar_999_999_999_99_en_ust_kademe_tasma_yok()
    {
        var m = Motor();
        var g = m.Gereksinim(999_999_999.99m, Yeni);
        Assert.Equal(BeyanKademesi.BeyanFormuVeBelge, g.Kademe);
        Assert.True(g.KimlikGerekli && g.AmacGerekli && g.BeyanFormuGerekli && g.EkBelgeGerekli);
        var d = m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 999_999_999.99m }, Yeni);
        Assert.False(d.Uygun);
        Assert.Contains(d.Eksikler, x => x == "370.000 TL ve üstü işlemde kimlik tespiti zorunludur");
    }

    [Fact]
    public void D5_bos_null_kimlik_alanlari_370000de_her_eksik_ayri_yazilir()
    {
        var m = Motor();
        var d = m.Dogrula(new MMasak.MasakGirdi
        {
            IslemTutariTl = 370_000m, KimlikTespitiVar = true, BelgeTuru = null, BelgeNo = null, AdSoyad = "  ",
        }, Yeni);
        Assert.False(d.Uygun);
        Assert.Contains("Kimlik belge numarası zorunludur", d.Eksikler);
        Assert.Contains("Ad soyad zorunludur", d.Eksikler);
        Assert.Contains("İşlem amacı belirtilmelidir", d.Eksikler);
    }

    [Fact]
    public void D6_turkce_giris_370_000_ve_1_5_cozulur_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(370_000m, FormAlani.SayiCoz("370.000").Deger);
        Assert.Equal(369_999.99m, FormAlani.SayiCoz("369.999,99").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    [Fact]
    public void D7_donem_gecisi_190000_06_10_kimlik_ister_07_10_istemez_gevseme_yalniz_yeni_gunde()
    {
        var m = Motor();
        Assert.True(m.Gereksinim(190_000m, Eski).KimlikGerekli);
        Assert.False(m.Gereksinim(190_000m, Yeni).KimlikGerekli);
        Assert.Equal(185_000m, m.EsiklerGunu(new DateOnly(2023, 1, 14)).KimlikEsigi);
        Assert.Equal(85_000m, m.EsiklerGunu(new DateOnly(2023, 1, 13)).KimlikEsigi);
        Assert.Equal(370_000m, m.EsiklerGunu(new DateOnly(2030, 1, 1)).KimlikEsigi);
    }
}

[Collection("http")]
public sealed class MASAK370SinirHttpTestleri(HttpFikstur f)
{
    private async Task<(Kiraci k, string hesap)> Kur(string ad)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, ad);
        var iban = TestIban.Uret();
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        { bankaAdi = "Ziraat", hesapAdi = "Ticari", iban, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (k, (await c.Json()).GetProperty("id").GetString()!);
    }

    private Task<HttpResponseMessage> Dekont(Kiraci k, string h, decimal tutar) =>
        f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bankalar/{h}/hareket", new
        { yon = "GELEN", tutar, karsiTarafAd = "Ali Veli" });

    [Fact]
    public async Task H1_kurus_yuvarlamasi_199999_994_gecer_199999_995_AwayFromZero_200000_olur_MASAK_400()
    {
        // GIRDI-AILESI-2 tur 4 (doğrulayıcı YÜKSEK · K9): banka hareketi 2'den fazla ondalığı artık
        // YUVARLAMAZ, 400 verir (alan: tutar) — "199.999,994" / "199.999,995" kuruş yuvarlamasıyla
        // MASAK eşiğinin altına/üstüne kaymaz, hiç yazılmaz. Eşik sınırı (MASAK 370) 2 ondalıkla AYNEN
        // ölçülür: 199.999,99 geçer, 200.000,00 MASAK 400.
        var (k, h) = await Kur("PAKETMASAK-370-TEST dogrulayici kurus");
        foreach (var t in new[] { 199_999.994m, 199_999.995m })
        {
            var r = await Dekont(k, h, t);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Contains("tutar", await r.Content.ReadAsStringAsync());
        }
        using (var db0 = f.Vt.OlusturSistem()) Assert.Empty(db0.BankaHareketleri.Where(x => x.TenantId == k.TenantId));
        var alt = await Dekont(k, h, 199_999.99m);
        Assert.Equal(HttpStatusCode.OK, alt.StatusCode);

        var (k2, h2) = await Kur("PAKETMASAK-370-TEST dogrulayici kurus2");
        var ust = await Dekont(k2, h2, 200_000.00m);
        Assert.Equal(HttpStatusCode.BadRequest, ust.StatusCode);
        Assert.Contains("200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur",
            (await ust.Json()).GetProperty("message").GetString());
        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.BankaHareketleri.Where(x => x.TenantId == k2.TenantId));
    }

    [Fact]
    public async Task H2_sifir_ve_eksi_dekont_reddedilir_MASAKa_hic_ulasmaz()
    {
        var (k, h) = await Kur("PAKETMASAK-370-TEST dogrulayici sifir");
        Assert.Equal(HttpStatusCode.BadRequest, (await Dekont(k, h, 0m)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Dekont(k, h, -370_000m)).StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.BankaHareketleri.Where(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task H3_gereksinim_ucu_cok_buyuk_tutar_ust_kademe_500_yok()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETMASAK-370-TEST dogrulayici buyuk");
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/masak/gereksinim?tutar=999999999.99");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(BeyanKademesi.BeyanFormuVeBelge, (await r.Json()).GetProperty("kademe").GetString());
    }
}

/// <summary>
/// HASIM DOĞRULAYICI (2. tur, 07.10.2026) — /banka dekont: 07.10.2026 sonrası sunucu kimliği
/// 200.000'de (amaç beyanı kademesi) ister, /masak/esikler kimlikEsigi=370.000 döner. Müşterisiz
/// 250.000 TL dekontta ekran MASAK alanlarını AÇMALIDIR; açmazsa kullanıcı kimliği giremez ve
/// sunucu her denemede 400 döner (çıkmaz). Sahte API sunucunun GERÇEK 07.10 yanıtlarını taklit eder
/// (MasakModulu.EsikYaniti + MasakKuralMotoru.Gereksinim — H1 testi sunucu tarafını kanıtlar).
/// </summary>
public sealed class MASAK370SinirBankaEkranTestleri : BunitContext
{
    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var m = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
            var t = m.Success ? decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
            var gereksinim = t < 200000m
                ? """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}"""
                : """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
            var y = yol switch
            {
                var v when v.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/masak/gereksinim") => gereksinim,
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    public MASAK370SinirBankaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    [Fact]
    public void B1_musterisiz_250000_dekont_07_10_sonrasi_MASAK_alanlari_ACILMALI_cikmaz_olmamali()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-dekont-gir");
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        e.Find("#dekont-tutar").Input("250000");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#masak-belge-no")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void B2_kontrol_musterisiz_370000_dekontta_MASAK_alanlari_acilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-dekont-gir");
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        e.Find("#dekont-tutar").Input("370000");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#masak-belge-no")), TimeSpan.FromSeconds(3));
    }
}

// ═════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 3 (Aşkın + Mehmet, 08.10.2026) — "kimliği FİİLEN isteyen en düşük tutar"
// (MasakEsikleri.KimlikIstenenEnDusukTutar + istemci Esikler.KimlikIstenenTutar()) sınırları.
// Beklenen değerler SABİT sayıdır; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class MASAK370SinirT3Testleri(TestVeritabani vt)
{
    private static readonly DateOnly Eski = new(2026, 10, 6);
    private static readonly DateOnly Yeni = new(2026, 10, 7);
    private static readonly System.Text.Json.JsonSerializerOptions Js = new(System.Text.Json.JsonSerializerDefaults.Web);

    private MMasak.MasakKuralMotoru Motor()
    {
        List<YasalParametreSatiri> Satirlar()
        {
            using var db = vt.OlusturSistem();
            return db.YasalParametreler.AsNoTracking().AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList();
        }
        return new(new MMasak.MasakEsikleri(), YasalParametreServisi.Yukleyicili(Satirlar));
    }

    private static Kuyumcu.Contracts.Bilesenler.MasakFormu.Esikler Coz(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<Kuyumcu.Contracts.Bilesenler.MasakFormu.Esikler>(json, Js)!;

    [Fact]
    public void T3_1_gercek_tabloda_en_dusuk_tutar_06_10_185000_07_10_200000_ve_ileri_tarih_200000()
    {
        var m = Motor();
        Assert.Equal(185_000m, m.EsiklerGunu(Eski).KimlikIstenenEnDusukTutar);
        Assert.Equal(200_000m, m.EsiklerGunu(Yeni).KimlikIstenenEnDusukTutar);
        Assert.Equal(200_000m, m.EsiklerGunu(new DateOnly(2030, 1, 1)).KimlikIstenenEnDusukTutar);
        Assert.Equal(370_000m, m.EsiklerGunu(Yeni).KimlikEsigi);   // md.5/1-b eşiği ayrıca korunur
    }

    [Fact]
    public void T3_2_sifir_istemci_esikleri_hepsi_0_ise_0_doner_sayi_uydurulmaz()
    {
        var e = Coz("""{"kimlikEsigi":0,"kimlikIstenenEnDusukTutar":0,"amacBeyaniEsigi":0,"beyanFormuEsigi":0,"ekBelgeEsigi":0,"asgariAciklamaUzunlugu":0,"belgeTurleri":[]}""");
        Assert.Equal(0m, e.KimlikIstenenTutar());
    }

    [Fact]
    public void T3_3_eksi_bozuk_sunucu_alani_eksi_ise_yok_sayilir_iki_esigin_kucugu_200000()
    {
        var e = Coz("""{"kimlikEsigi":370000,"kimlikIstenenEnDusukTutar":-5,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":[]}""");
        Assert.Equal(200_000m, e.KimlikIstenenTutar());
    }

    [Fact]
    public void T3_4_kurus_sunucunun_6_haneli_ondaligi_birebir_200000_ve_motor_199999_99_YOK_200000_00_AMAC()
    {
        var e = Coz("""{"kimlikEsigi":370000.000000,"kimlikIstenenEnDusukTutar":200000.000000,"amacBeyaniEsigi":200000.000000,"beyanFormuEsigi":2000000.010000,"ekBelgeEsigi":20000000.010000,"asgariAciklamaUzunlugu":20,"belgeTurleri":[]}""");
        Assert.Equal(200_000m, e.KimlikIstenenTutar());
        Assert.True(199_999.99m < e.KimlikIstenenTutar());
        var m = Motor();
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(199_999.99m, Yeni).Kademe);
        Assert.Equal(BeyanKademesi.AmacBeyani, m.Gereksinim(200_000.00m, Yeni).Kademe);
        // AwayFromZero 2 hane: 199.999,995 → 200.000,00 → kimlik kademesi
        Assert.Equal(BeyanKademesi.AmacBeyani, m.Gereksinim(Math.Round(199_999.995m, 2, MidpointRounding.AwayFromZero), Yeni).Kademe);
    }

    [Fact]
    public void T3_5_cok_buyuk_999_999_999_99_en_ust_kademe_ve_istemci_esigi_degismez()
    {
        var m = Motor();
        var g = m.Gereksinim(999_999_999.99m, Yeni);
        Assert.Equal(BeyanKademesi.BeyanFormuVeBelge, g.Kademe);
        Assert.True(g.KimlikGerekli);
        var e = new Kuyumcu.Contracts.Bilesenler.MasakFormu.Esikler(999_999_999.99m, 200_000m, 0m, 0m, 20, []);
        Assert.Equal(200_000m, e.KimlikIstenenTutar());
    }

    [Fact]
    public void T3_6_bos_alan_yok_ve_amac_esigi_kimlikten_buyukse_kimlik_esigi_06_10_davranisi()
    {
        var e = Coz("""{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":[]}""");
        Assert.Equal(0m, e.KimlikIstenenEnDusukTutar);
        Assert.Equal(185_000m, e.KimlikIstenenTutar());
    }

    [Fact]
    public void T3_7_turkce_giris_200_000_ve_199_999_99_cozulur_belirsiz_200_reddedilmez_ama_1_500_belirsiz()
    {
        Assert.Equal(200_000m, FormAlani.SayiCoz("200.000").Deger);
        Assert.Equal(199_999.99m, FormAlani.SayiCoz("199.999,99").Deger);
        Assert.Equal(250_000m, FormAlani.SayiCoz("250000").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }

    [Fact]
    public void T3_8_mesaj_200000_ile_369999_99_arasi_amac_esigini_370000_ve_ustu_kimlik_esigini_soyler()
    {
        var m = Motor();
        var d1 = m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 200_000m }, Yeni);
        var k1 = Assert.Single(d1.Eksikler, x => x.Contains("kimlik tespiti zorunludur"));
        Assert.Contains("200.000", k1);
        var d2 = m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 370_000m }, Yeni);
        var k2 = Assert.Single(d2.Eksikler, x => x.Contains("kimlik tespiti zorunludur"));
        Assert.Contains("370.000", k2);
        Assert.DoesNotContain("200.000", k2);
    }
}
