using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sistem;
using Kuyumcu.Api.Modules.Yasal;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-PARAM-EKRAN — TUR 3 (önceki doğrulamanın bulguları):
//  • KRİTİK: bağlantılı toplamda kilit metni toplamın AŞTIĞI kademeyi söyler (370.000 kimlik — MASAK
//    / 200.000 kimlik ve işlem amacı); eskiden her zaman en düşük kademe (200.000) basılıyordu.
//  • ORTA: toplam dalında form amaç istiyorsa başlık "Kimlik ve işlem amacı gerekli — …".
//  • DÜŞÜK (Mehmet notu): değişiklik formu yalnız ByGatech yönetim kiracısında görünür
//    (Sistem:YonetimKiracilari; yetki değil görünürlük — yazma yine sistem anahtarıyla).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YasalParamTur3SafTestleri
{
    private static Esikler Esik() =>
        new(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK"]) { KimlikIstenenEnDusukTutar = 200_000m };

    private static MasakGereksinim Toplam(decimal olcu, decimal islem = 185_000m, bool amac = true) =>
        new(amac ? "AMAC_BEYANI" : "YALNIZ_KIMLIK", true, amac, false, false, 20) { OlcuTutarTl = olcu, IslemTutarTl = islem };

    [Theory]
    [InlineData(370_000, "Kimlik")]
    [InlineData(410_000, "Kimlik")]
    [InlineData(369_999.99, "Amac")]
    [InlineData(250_000, "Amac")]
    [InlineData(200_000, "Amac")]
    [InlineData(199_999.99, "Bilinmiyor")]   // sunucu kimlik istedi ama toplam iki kademenin de altında → sayı uydurulmaz
    public void Tur3_ToplamKademesi_MasakKademe_ile_ayni_kural(double olcu, string beklenen) =>
        Assert.Equal(Enum.Parse<MasakTetik>(beklenen), MasakKademe.ToplamKademesi(Esik(), Toplam((decimal)olcu, 150_000m)));

    [Fact]
    public void Tur3_ToplamKademesi_toplamdan_gelmeyen_ya_da_esiksiz_kararda_Bilinmiyor()
    {
        Assert.Equal(MasakTetik.Bilinmiyor, MasakKademe.ToplamKademesi(null, Toplam(400_000m)));
        Assert.Equal(MasakTetik.Bilinmiyor, MasakKademe.ToplamKademesi(Esik(), Toplam(400_000m, 400_000m)));   // tek işlem
        Assert.Equal(MasakTetik.Bilinmiyor, MasakKademe.ToplamKademesi(Esik(), null));
    }

    [Fact]
    public void Tur3_YonetimKiracisiMi_yalniz_listede_ve_anahtar_kullanilabilirken()
    {
        var a = new SistemAyarlari { Anahtari = HttpOrtami.TestSistemAnahtari, YonetimKiracilari = ["T-BYGATECH"] };
        Assert.True(a.YonetimKiracisiMi("T-BYGATECH"));
        Assert.False(a.YonetimKiracisiMi("T-KUYUMCU"));
        Assert.False(a.YonetimKiracisiMi(null));
        Assert.False(a.YonetimKiracisiMi(""));
        Assert.False(a.YonetimKiracisiMi("t-bygatech"));   // ordinal
        // Varsayılan liste boş → hiçbir kiracıda görünmez (fail-closed).
        Assert.False(new SistemAyarlari { Anahtari = HttpOrtami.TestSistemAnahtari }.YonetimKiracisiMi("T-BYGATECH"));
        // Sistem uçları kapalıyken (anahtar yok / yer tutucu) form da açılmaz.
        Assert.False(new SistemAyarlari { Anahtari = "", YonetimKiracilari = ["T-BYGATECH"] }.YonetimKiracisiMi("T-BYGATECH"));
    }

    [Fact]
    public void Tur3_liste_ucu_yonetimAcik_yalniz_yonetim_kiracisinda()
    {
        var servis = YasalParametreServisi.Yukleyicili(() =>
        [
            new("t-m", YasalAnahtar.MasakKimlikEsigiTl, 370_000m, null, "TL", new(2026, 10, 7), null, "d", "https://x", "DOGRULANDI", "a"),
        ], new SahteSaat(new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero)));
        var ayar = new SistemAyarlari { Anahtari = HttpOrtami.TestSistemAnahtari, YonetimKiracilari = ["T-BYGATECH"] };

        bool Acik(string kiraci)
        {
            var c = new YasalParametreController(servis, new TakvimAyarlari(), ayar)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenantId", kiraci)], "test")),
                    },
                },
            };
            return c.Liste(null).YonetimAcik;
        }

        Assert.True(Acik("T-BYGATECH"));
        Assert.False(Acik("T-KUYUMCU"));
    }
}

[Collection("http")]
public sealed class YasalParamTur3HttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Tur3_kuyumcu_SAHIP_liste_yanitinda_yonetimAcik_FALSE()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASALPARAM-TEST-tur3");
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yasal-parametreler");
        Assert.True(c.StatusCode == HttpStatusCode.OK, await c.Content.ReadAsStringAsync());
        var j = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(j.GetProperty("yonetimAcik").GetBoolean());
    }
}

public sealed class YasalParamTur3MasakFormuTestleri : BunitContext
{
    public YasalParamTur3MasakFormuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<Kuyumcu.Web.Bilesenler.MasakFormu> Ciz(MasakGereksinim g, bool sade) =>
        Render<Kuyumcu.Web.Bilesenler.MasakFormu>(p => p
            .Add(x => x.Model, new Kuyumcu.Web.Bilesenler.MasakFormu.MasakBilgi())
            .Add(x => x.Tutar, 185_000m)
            .Add(x => x.Esik, new Esikler(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK"]) { KimlikIstenenEnDusukTutar = 200_000m })
            .Add(x => x.Gereksinim, g)
            .Add(x => x.Sade, sade));

    [Fact]
    public void Tur3_baglantili_toplamda_form_amac_istiyorsa_baslik_KIMLIK_VE_ISLEM_AMACI_iki_modda()
    {
        var g = new MasakGereksinim("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = 370_000m, IslemTutarTl = 185_000m };
        foreach (var sade in new[] { true, false })
        {
            var e = Ciz(g, sade);
            Assert.Equal($"Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı {Bicim.Tl(370_000m)}",
                e.Find("#masak-baslik").TextContent);
            Assert.NotEmpty(e.FindAll("#masak-amac"));   // başlığın söylediği alan gerçekten istendi
        }
    }
}

/// <summary>Satış ekranının Kaydet kilit metni — bağlantılı toplamın iki dalı (sahte sunucu: 370.000 / 200.000).</summary>
public sealed partial class SATIS3EkranTestleri
{
    private static string Tur3Esik() =>
        """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"],"kimlikIstenenEnDusukTutar":200000}""";

    private static string Tur3Toplam(decimal olcu) =>
        $$"""{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20,"olcuTutarTl":{{olcu.ToString(CultureInfo.InvariantCulture)}},"islemTutarTl":150000}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Tur3Fis(decimal olcu)
    {
        masakEsikJson = Tur3Esik();
        gereksinimOzel = v => v.Contains("tutar=150000&cariId=c1") ? Tur3Toplam(olcu) : null;
        var e = Ekran();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        FiyatliKalem(e, "u1", "150000", 1);
        SeciciYardim.Sec(e, "islem-odeme", "KART");   // nakit (tevsik) kilidi bu testin konusu değil
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);
        Satis4Yardim.MasakBelgeNoSil(e);
        return e;
    }

    [Fact]
    public void Tur3_baglantili_toplam_KIMLIK_esiginde_kilit_370000_MASAK_der_200000_DEMEZ()
    {
        var e = Tur3Fis(370_000m);
        var m = YasalParamSozlugu.Tr;
        // YASAL-KAPI-2 tur 2 (doğrulayıcı KRİTİK "aynı eşik 4 yerde"): PRO kilit sebebi eşik SAYISI söylemez; tur 3'ün
        // "aşılan kademe söylensin" kararı PRO "Yasal kapılar" panelinde (tek yer): kimlik satırı GEREKİR (MASAK).
        e.WaitForAssertion(() => PTezgahTurKimlikYardim.EksikKilit(
            e.Find("#islem-kaydet").GetAttribute("title")), Bekle);   // P-TEZGAH-TUR-KIMLIK K4: eksik alanın adı
        var kilit = e.Find("#islem-kaydet").GetAttribute("title")!;
        Assert.DoesNotContain(Bicim.Tl(200_000m), kilit);
        Assert.DoesNotContain(Bicim.Tl(370_000m), kilit);
        Assert.Contains("MASAK", e.Find("#islem-pro-kimlik-etiket").TextContent);
        Assert.Contains("metin-kirmizi", e.Find("#islem-pro-kimlik").ClassName);
        Assert.Equal($"Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı {Bicim.Tl(370_000m)}",
            e.Find("#masak-baslik").TextContent);
    }

    [Fact]
    public void Tur3_baglantili_toplam_AMAC_kademesinde_kilit_200000_kimlik_ve_islem_amaci_der()
    {
        var e = Tur3Fis(250_000m);
        var m = YasalParamSozlugu.Tr;
        // YASAL-KAPI-2 tur 2: kademe PRO panelinde — amaç satırı GEREKİR, kimlik (MASAK 370.000) satırı altında.
        e.WaitForAssertion(() => PTezgahTurKimlikYardim.EksikKilit(
            e.Find("#islem-kaydet").GetAttribute("title")), Bekle);   // P-TEZGAH-TUR-KIMLIK K4: eksik alanın adı
        var kilit = e.Find("#islem-kaydet").GetAttribute("title")!;
        Assert.DoesNotContain(Bicim.Tl(200_000m), kilit);
        Assert.DoesNotContain(Bicim.Tl(370_000m), kilit);
        Assert.Contains("metin-kirmizi", e.Find("#islem-pro-amac").ClassName);
        Assert.Contains("metin-yesil", e.Find("#islem-pro-kimlik").ClassName);
    }
}
