using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). Kapsam: /hurda ramat çıkışı — ekran (bUnit: onay penceresi,
// bakiye sınırı, mesajın gösterdiği bakiye, özetin gösterdiği gram, tutar
// yuvarlaması, Türkçe giriş) ve sunucu çıkış kapısı (gerçek MySQL: 0 · eksi ·
// = bakiye · bakiye + 0,0001 · 4 hane · çok büyük).
// Projedeki para yuvarlama kuralı: MidpointRounding.AwayFromZero (SatisServisi.Yuvarla2).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q102EkranSinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal bakiye = 9.16m;

    public Q102EkranSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => bakiye))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<decimal> bakiye) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var b = bakiye().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    "{\"subeId\":\"s1\",\"satirlar\":[{\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGram\":" + b
                    + ",\"girenHas\":" + b + ",\"cikanHas\":0,\"tahlilFarkiHas\":0}],\"fireKaydiAcik\":false,\"fireNotu\":null}",
                var y when y.EndsWith("/hurda/defter") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/islemler/hurda-satis") => """
                    {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":0.5,
                     "birimFiyatTlHasGram":6800,"toplamTl":3400,"tahsilEdilenTl":3400,"kalanTl":0,
                     "kalanHurdaHasGram":8.66,"kasaBakiyesi":253400,"paraBirimi":"TRY",
                     "tarih":"2026-09-24T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,
                     "aliciKabulHasGrami":0.5,"tahlilFarkiHasGram":0}
                    """,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-24T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/defter")));
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        // ZO-ISLEM-A: isteğe bağlı alanlar "+ Detay ekle", defter kapalı akordeon
        // arkasında. Bu dosyanın testleri o alanların KURALLARINI sınar; açılış
        // davranışı ZOISLEMAEkranTestleri'nde ayrıca sınanır.
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        return e;
    }

    private bool PostVar() => istekler.Any(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));

    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    [Theory]
    [InlineData("0")]
    [InlineData("-0,5")]
    [InlineData("")]
    public void DQ102_sifir_eksi_bos_gram_onaya_gitmez(string ham)
    {
        var e = Ekran();
        e.Find("#hrd-has").Input(ham);
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#alan-hata-hrd-has");
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.False(PostVar());
    }

    [Fact]
    public void DQ102_bakiyeye_esit_4_haneli_cikis_gecer_ve_govdede_aynen_gider()
    {
        bakiye = 9.1646m;
        var e = Ekran();
        e.Find("#hrd-has").Input("9,1646");
        Assert.Empty(e.FindAll("#alan-hata-hrd-has"));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        Assert.Contains("\"hasGrami\":9.1646", istekler.Single(i => i.Metot == "POST").Govde);
    }

    [Fact]
    public void DQ102_bakiye_arti_0_0001_aninda_reddedilir()
    {
        bakiye = 9.1646m;
        var e = Ekran();
        e.Find("#hrd-has").Input("9,1647");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-hrd-has")));
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.False(PostVar());
    }

    /// <summary>
    /// QA 10 bulgusunun ÖZÜ: mesaj gerçek durumu söylemeli. Bakiye 9,1646 iken
    /// kullanıcı "9,165" yazarsa (aşım 0,0004) mesajdaki "mevcut" değer, girilen
    /// değerden KÜÇÜK görünmeli; "9,165 g mevcut" yazıp 9,165'i reddetmek çelişkidir.
    /// </summary>
    [Fact]
    public void DQ102_asim_mesajindaki_mevcut_bakiye_girilen_degerle_celismez()
    {
        bakiye = 9.1646m;
        var e = Ekran();
        e.Find("#hrd-has").Input("9,165");
        var mesaj = e.WaitForElement("#alan-hata-hrd-has").TextContent;
        Assert.DoesNotContain("9,165 g", mesaj);
    }

    /// <summary>Geri alınamaz kaydın onay özeti gönderilecek gramı AYNEN göstermeli.</summary>
    [Fact]
    public void DQ102_onay_ozeti_4_haneli_grami_aynen_gosterir()
    {
        bakiye = 9.1646m;
        var e = Ekran();
        e.Find("#hrd-has").Input("9,1604");
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("9,1604", ozet);
    }

    /// <summary>Kuruş: 0,5 has × 0,01 TL = 0,005 → AwayFromZero → 0,01 ₺.</summary>
    [Fact]
    public void DQ102_kurus_yuvarlamasi_0_005_yukari()
    {
        var e = Ekran();
        e.Find("#hrd-has").Input("0,5");
        e.Find("#hrd-fiyat").Input("0,01");
        e.WaitForAssertion(() => Assert.Contains("0,01", e.Find("#hrd-tutar").TextContent));
    }

    [Fact]
    public void DQ102_cok_buyuk_tutar_ozette_tr_bicimli()
    {
        bakiye = 1m;
        var e = Ekran();
        e.Find("#hrd-has").Input("1");
        e.Find("#hrd-fiyat").Input("999.999.999,99");
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("999.999.999,99", ozet);
        Assert.DoesNotContain("TRY", ozet);
    }

    [Fact]
    public void DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red()
    {
        var e = Ekran();
        // Q10-2 v2 (DÜŞÜK bulgu): bUnit girdi yarışı — değer yerleşene dek yeniden gir,
        // mesajı WaitForAssertion ile bekle (beklenti GEVŞETİLMEDİ: mesaj yine zorunlu).
        Gir(e, "#hrd-has", "1.500");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-has-hata, #alan-hata-hrd-has")));
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));

        Gir(e, "#hrd-has", "1,5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        Assert.Contains("\"hasGrami\":1.5", istekler.Single(i => i.Metot == "POST").Govde);
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
    }

    // ── DOĞRULAYICI TUR 2 (v2, 24.09) ──────────────────────────────────────
    /// <summary>
    /// Onay penceresi AÇIKKEN canlı kur güncellenirse (CanliKurServisi 10 sn
    /// yoklar) kullanıcının onayladığı özet ile sunucuya giden istek AYNI
    /// olmalıdır. Beklenen sabit: özet 6.800,00 ₺ gösterdi → gövde 6800 taşır.
    /// </summary>
    [Fact]
    public void DQ102v2_onay_acikken_kur_degisirse_POST_onaylanan_fiyati_tasir()
    {
        var e = Ekran();
        e.Find("#hrd-has").Input("0,5");
        e.Find("#hrd-kaydet").Click();
        var hedef = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("6.800", hedef);
        Assert.Contains("3.400", hedef);

        var canli = Services.GetRequiredService<CanliKurServisi>();
        e.InvokeAsync(() => canli.Uygula(new CanliKurServisi.KurAnlik("kur-2", 7_000m, 7_050m, 34m, 37m, 43.5m)));

        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var govde = istekler.Single(i => i.Metot == "POST").Govde;
        Assert.True(govde.Contains("\"birimFiyatTlHasGram\":6800"), "Onaylanan 6.800, giden gövde: " + govde);
        Assert.Contains("\"tahsilEdilenTl\":3400", govde);
    }

}

[Collection("veritabani")]
public sealed class Q102SunucuSinirTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    /// <summary>10,005 g K22 → has 10,005 × 0,916 = 9,16458.</summary>
    private static MSatis.HurdaAlisDto Alis(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10.005m,
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private static MSatis.HurdaSatisDto Cikis(Kiraci k, decimal has, decimal fiyat = 2_900m) => new()
    {
        Maden = "ALTIN", HasGrami = has, BirimFiyatTlHasGram = fiyat,
        KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    [Fact]
    public async Task DQ102S_bakiye_alti_haneli_esit_cikis_gecer_arti_0_0001_gecmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "DQ102-esit"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        using (var o = vt.Olustur(k))
            Assert.Equal(9.16458m, await MHurda.HurdaStokKapisi.Bakiye(o, k.Islem, "ALTIN"));

        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 9.16468m), k.Islem));
        var s = await Satis(db).HurdaSatis(Cikis(k, 9.16458m), k.Islem);
        Assert.Equal(0m, s.KalanHurdaHasGram);
    }

    [Fact]
    public async Task DQ102S_kurus_yuvarlamasi_AwayFromZero()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "DQ102-kurus"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        // ISLEM-BANT: 0,01 TL/has g bant dışı; aynı yarım kuruş geçerli fiyatla: 0,000002 × 2.500 = 0,005 → 0,01.
        var s = await Satis(db).HurdaSatis(Cikis(k, 0.000002m, 2_500m), k.Islem);
        Assert.Equal(0.01m, s.ToplamTl);
    }

    [Fact]
    public async Task DQ102S_cok_buyuk_cikis_bakiyeyi_asar_reddedilir_hicbir_sey_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "DQ102-buyuk"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 999_999_999.99m), k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.0001")]
    public async Task DQ102S_sifir_ve_eksi_reddedilir(string has)
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"DQ102-s{has}"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(
            Cikis(k, decimal.Parse(has, System.Globalization.CultureInfo.InvariantCulture)), k.Islem));
        using var o = vt.Olustur(k);
        Assert.Equal(9.16458m, await MHurda.HurdaStokKapisi.Bakiye(o, k.Islem, "ALTIN"));
    }

    // ── DOĞRULAYICI TUR 2 (v2, 24.09) — 6 haneden ince çıkış sınırı ─────────
    /// <summary>9,16458 + 0,0000004 → 6 haneye 9,16458 (= bakiye): geçer, bakiye TAM 0 (eksiye düşmez).</summary>
    [Fact]
    public async Task DQ102v2S_bakiye_arti_0_0000004_yuvarlanir_gecer_defter_eksiye_dusmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "DQ102v2-ince"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Satis(db).HurdaSatis(Cikis(k, 9.1645804m), k.Islem);
        using var o = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(o, k.Islem, "ALTIN"));
    }

    /// <summary>9,16458 + 0,0000005 → AwayFromZero 9,164581 &gt; bakiye: REDDEDİLİR, hiçbir şey yazılmaz.</summary>
    [Fact]
    public async Task DQ102v2S_bakiye_arti_0_0000005_yarim_yukari_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "DQ102v2-yarim"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 9.1645805m), k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS"));
    }

}

// ── DOĞRULAYICI TUR 3 (25.09) — onay yarışları, sunucu reddi, canlı mesaj döngüsü, kuruş/0,0001 ──
// Beklenen değerler SABİT (elle hesaplandı); üretim kodundan türetilmez.
public sealed class Q102T3EkranSinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal bakiye = 2.664m;
    private TaskCompletionSource<bool>? postBekle;
    private bool postReddet;

    public Q102T3EkranSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(Q102T3EkranSinirTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var b = t.bakiye.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (yol.EndsWith("/islemler/hurda-satis"))
            {
                if (t.postBekle is { } tcs) await tcs.Task;
                if (t.postReddet)
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("""{"message":"SUNUCU-RED-T3: hurda bakiyesi yetersiz"}""",
                            System.Text.Encoding.UTF8, "application/json"),
                    };
            }
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    "{\"subeId\":\"s1\",\"satirlar\":[{\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGram\":" + b
                    + ",\"girenHas\":" + b + ",\"cikanHas\":0,\"tahlilFarkiHas\":0}],\"fireKaydiAcik\":false,\"fireNotu\":null}",
                var y when y.EndsWith("/hurda/defter") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/islemler/hurda-satis") => """
                    {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":0.5,
                     "birimFiyatTlHasGram":6800,"toplamTl":3400,"tahsilEdilenTl":3400,"kalanTl":0,
                     "kalanHurdaHasGram":2.164,"kasaBakiyesi":253400,"paraBirimi":"TRY",
                     "tarih":"2026-09-24T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,
                     "aliciKabulHasGrami":0.5,"tahlilFarkiHasGram":0}
                    """,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-24T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/defter")));
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        // ZO-ISLEM-A: isteğe bağlı alanlar "+ Detay ekle", defter kapalı akordeon
        // arkasında. Bu dosyanın testleri o alanların KURALLARINI sınar; açılış
        // davranışı ZOISLEMAEkranTestleri'nde ayrıca sınanır.
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        return e;
    }

    private int PostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")); }

    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    /// <summary>QA 10 senaryosu birebir: bakiye 2,664 · 5 → mesaj · 0,5 → mesaj KALKAR · 3 → mesaj GERİ gelir.</summary>
    [Fact]
    public void DQ102T3_qa_senaryosu_asim_duzelt_yeniden_as_mesaj_canli_izler()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "5");
        var m1 = e.WaitForElement("#alan-hata-hrd-has").TextContent;
        Assert.Contains("Kalan hurdayı aşıyorsunuz (2,664 g mevcut)", m1);
        Assert.DoesNotContain("hurda bakiyesi yok", m1);

        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-has")));

        Gir(e, "#hrd-has", "3");
        e.WaitForAssertion(() => Assert.Contains("2,664 g mevcut", e.Find("#alan-hata-hrd-has").TextContent));
        Assert.Equal(0, PostSayisi());
    }

    /// <summary>Onayla'ya istek sürerken ikinci kez basmak İKİNCİ geri alınamaz kayıt üretmemeli.</summary>
    [Fact]
    public void DQ102T3_onayla_cift_tik_tek_POST()
    {
        postBekle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSayisi()));
        // İstek askıdayken ikinci tık (düğme kilitli olmalı; kilitli değilse de koruma POST atmamalı).
        var d = e.Find("#hrd-onay-onayla");
        Assert.True(d.HasAttribute("disabled"), "İstek sürerken Onayla düğmesi kilitli değil");
        try { d.Click(); } catch (Exception) { /* bUnit kilitli düğmeye tıklamayı reddedebilir */ }
        postBekle.SetResult(true);
        e.WaitForAssertion(() => Assert.Contains("2,164 g", e.Find("#hrd-basari").TextContent));
        Assert.Equal(1, PostSayisi());
    }

    /// <summary>Sunucu reddederse: pencere AÇIK kalır, hata pencerede görünür, başarı yazılmaz, girilen gram KORUNUR.</summary>
    [Fact]
    public void DQ102T3_sunucu_reddi_pencerede_gorunur_basari_yok_deger_korunur()
    {
        postReddet = true;
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains("SUNUCU-RED-T3", e.Find("#hrd-onay-hata").TextContent));
        Assert.NotEmpty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.Empty(e.FindAll("#hrd-basari .bant, #hrd-basari"));
        Assert.Equal(1, PostSayisi());
        e.Find("#hrd-onay-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-onay-kipi")));
        Assert.Equal("0,5", e.Find("#hrd-has").GetAttribute("value"));
        Assert.Equal(1, PostSayisi());
    }

    /// <summary>Kuruş sınırı: 0,0001 has × 45 = 0,0045 → 0,00 ₺; × 50 = 0,005 → AwayFromZero 0,01 ₺. 4 hane gram özette aynen.</summary>
    [Fact]
    public void DQ102T3_0_0001_gram_kurus_sinirlari_ve_ozet()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,0001");
        Gir(e, "#hrd-fiyat", "45");
        e.WaitForAssertion(() => Assert.Matches(@"Tutar:\s*0\s*₺", e.Find("#hrd-tutar").TextContent)); // Bicim.Para tam sayıyı küsuratsız yazar (ölçüldü)
        Assert.DoesNotContain("0,01", e.Find("#hrd-tutar").TextContent);
        Gir(e, "#hrd-fiyat", "50");
        e.WaitForAssertion(() => Assert.Contains("0,01", e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("0,0001 g", ozet);
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSayisi()));
        string govde; lock (istekler) govde = istekler.Single(i => i.Metot == "POST").Govde;
        Assert.Contains("\"hasGrami\":0.0001", govde);
        Assert.Contains("\"birimFiyatTlHasGram\":50", govde);
    }

    /// <summary>Çok büyük: 0,5 × 1.999.999.999,98 = 999.999.999,99 ₺ — tutar ve özet tr-TR, 'TRY' yok.</summary>
    [Fact]
    public void DQ102T3_cok_buyuk_tutar_999_999_999_99()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-fiyat", "1.999.999.999,98");
        e.WaitForAssertion(() => Assert.Contains("999.999.999,99", e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("999.999.999,99", ozet);
        Assert.DoesNotContain("TRY", ozet);
    }

    /// <summary>Belirsiz Türkçe giriş '1.500' onaya GİTMEZ; '45.000' → 45000, '1,5' → 1,5 (FormAlani.SayiCoz).</summary>
    [Fact]
    public void DQ102T3_turkce_giris_sayicoz()
    {
        Assert.Equal(45000m, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1,5").Deger);
        Assert.Null(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500").Deger);
        var e = Ekran();
        Gir(e, "#hrd-has", "1.500");
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.Equal(0, PostSayisi());
    }
}
