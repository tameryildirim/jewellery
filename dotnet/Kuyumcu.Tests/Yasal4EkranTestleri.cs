using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 — ekran testleri: Gider pusulaları (Y4-A yeniden düzenle,
// Y4-B stopaj sekmesi) ve Ürün formu KMTS alanı (Y4-C, K-13).
// Sahte API yeni uçlara GERÇEKÇİ yanıt verir ('{}' varsayılanı değil).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Yasal4PusulaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string stopajYaniti = Stopaj;
    private string sureYaniti = Sure;

    private const string Liste = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":"h1","saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Yanlış Ad","saticiTckn":"*******0146","ayarId":"K22","gramAgirlik":1.0,
          "hasGrami":0.916,"brutTutar":2564.80,"stopajOran":0.0,"stopajTutar":0.00,"netOdenen":2564.80,
          "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
          "iptalEdildi":true,"iptalGerekce":"yanlış ad","iptalTarihi":"2099-09-02T00:00:00Z",
          "duzeltilenId":null,"gecikti":false,"kalanGun":5,"sonGun":"2099-09-08","yenidenDuzenlenebilir":true},
         {"id":"gp2","pusulaNo":"GP-2026-000002","hareketId":"h2","saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Ali Veli","saticiTckn":"*******0146","ayarId":"K22","gramAgirlik":1.0,
          "hasGrami":0.916,"brutTutar":2564.80,"stopajOran":0.0,"stopajTutar":0.00,"netOdenen":2564.80,
          "teslimTarihi":"2020-09-01T00:00:00Z","duzenlemeSonTarihi":"2020-09-08T00:00:00Z",
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null,
          "duzeltilenId":null,"gecikti":null,"kalanGun":null,"sonGun":null,"yenidenDuzenlenebilir":false}]
        """;

    private const string Sure = """
        {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":1,"gecikmis":[],
         "yaklasan":[{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":"h1","teslimTarihi":"2099-09-01T00:00:00Z",
           "sonGun":"2099-09-08","kalanGun":5,"brutTutar":2564.80,"stopajTutar":0}],
         "enYakinSonGun":"2099-09-08","sinir":500,"sinirdaMi":false,"uyarilar":[]}
        """;

    private const string Detay = """
        {"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":"h1","subeId":"sb1","cariId":null,
         "saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Yanlış Ad","saticiTckn":"10000000146",
         "saticiAdres":"Ulus","ayarId":"K22","gramAgirlik":1.0,"hasGrami":0.916,"malAciklamasi":"bilezik",
         "brutTutar":2564.80,"stopajOran":0.0,"stopajTutar":0.00,"netOdenen":2564.80,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "duzeltilenId":null,"olusturuldu":"2099-09-01T00:00:00Z","iptalEdildi":true,
         "iptalGerekce":"yanlış ad","iptalTarihi":"2099-09-02T00:00:00Z"}
        """;

    private const string Yeni = """
        {"id":"gp3","pusulaNo":"GP-2026-000003","hareketId":"h1","subeId":"sb1","cariId":null,
         "saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Doğru Ad","saticiTckn":"10000000146",
         "saticiAdres":"Ulus","ayarId":"K22","gramAgirlik":1.0,"hasGrami":0.916,"malAciklamasi":"bilezik",
         "brutTutar":2564.80,"stopajOran":0.0,"stopajTutar":0.00,"netOdenen":2564.80,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "duzeltilenId":"gp1","olusturuldu":"2099-09-03T00:00:00Z","iptalEdildi":false,
         "iptalGerekce":null,"iptalTarihi":null}
        """;

    private const string Stopaj = """
        {"yil":2026,"donemler":[
          {"donem":"2026-08","kesilenTl":1340.00,"odenenTl":0,"kalanTl":1340.00,"sonGun":"2026-09-26",
           "sonGunTeyit":"TEYITSIZ","sonGunGecti":true,"pusulaSayisi":4,"odemeler":[]},
          {"donem":"2026-07","kesilenTl":200.00,"odenenTl":200.00,"kalanTl":0,"sonGun":"2026-08-26",
           "sonGunTeyit":"TEYITSIZ","sonGunGecti":false,"pusulaSayisi":1,
           "odemeler":[{"id":"so1","donem":"2026-07","tutarTl":200.00,"odemeTarihi":"2026-08-20T09:00:00Z",
             "kasaId":"k1","kasaAdi":"Ana kasa","hareketId":"kh1","tahakkukFisNo":null,"geriAlindi":false}]}]}
        """;

    public Yasal4PusulaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi(Yasal4PusulaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => t.sureYaniti,
                var y when y.EndsWith("/yeniden-duzenle") => Yeni,
                var y when y.Contains("/gider-pusulalari/") => Detay,
                var y when y.EndsWith("/gider-pusulalari") => Liste,
                var y when y.Contains("/stopaj/donemler") => t.stopajYaniti,
                var y when y.EndsWith("/stopaj/odemeler") =>
                    """{"id":"so9","donem":"2026-08","tutarTl":1340.00,"kasaId":"k1","kasaAdi":"Ana kasa","kasaBakiyesi":100000,"kalanTl":0}""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana kasa","paraBirimi":"TRY","bakiye":101340.00,"aktif":true,"subeId":"sb1"}]""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                _ => "[]",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> Ciz(string? sekme = null, string? ek = null)
    {
        if (sekme is not null)
            Services.GetRequiredService<NavigationManager>().NavigateTo($"/gider-pusulalari?sekme={sekme}{ek}");
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 2);
        return c;
    }

    // ── Y4-A ────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4A_iptal_edilmis_yenisiz_satirda_BIRINCIL_eylem_Yeniden_duzenle_digerinde_YOK()
    {
        var c = Ciz();
        var kap = c.Find("#gp-eylem-gp1");
        var ilk = kap.QuerySelectorAll("button").First();
        Assert.Equal("gp-yeniden-gp1", ilk.Id);
        Assert.Equal(Yasal4Sozlugu.Tr.SatirYenidenDuzenle, ilk.TextContent.Trim());
        Assert.Empty(c.FindAll("#gp-yeniden-gp2"));
        // Geri sayım YALNIZ bekleyen belgede; eski (süresinde) belgede rozet yok.
        Assert.NotNull(c.Find("#gp-kalan-gp1"));
        Assert.Empty(c.FindAll("#gp-kalan-gp2"));
        Assert.Empty(c.FindAll("#gp-sure-gecti-gp2"));
        // Kart: "yeniden düzenlenecek" 1 (eski kırmızı "gecikmiş" kartı yok).
        Assert.Contains(Yasal4Sozlugu.Tr.KartBekleyen, c.Find("#gp-sure-kartlari").TextContent);
    }

    [Fact]
    public void Y4A_yeniden_duzenle_cekmecesi_dolu_acilir_kaydet_POST_atar_ve_bant_yeni_numarayi_yazar()
    {
        var c = Ciz();
        c.Find("#gp-yeniden-gp1").Click();
        c.WaitForElement("#gp-yd-form");
        // UX-S21: bilinen değerler dolu gelir.
        Assert.Equal("Yanlış Ad", c.Find("#gp-yd-ad").GetAttribute("value"));
        c.Find("#gp-yd-ad").Input("Doğru Ad");
        c.Find("#cekmece-gider-pusulasi-yeniden-kaydet").Click();
        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.EndsWith("/gider-pusulalari/gp1/yeniden-duzenle")
                 && System.Text.Json.JsonDocument.Parse(i.Govde).RootElement.GetProperty("adSoyad").GetString() == "Doğru Ad"));
        c.WaitForAssertion(() => Assert.Contains("GP-2026-000003", c.Find("#gp-yd-basari").TextContent));
    }

    [Fact]
    public void Y4A_yeniden_duzenle_TCKN_eksikse_Kaydet_kilitli_ve_sebebi_yazar()
    {
        var c = Ciz();
        c.Find("#gp-yeniden-gp1").Click();
        c.WaitForElement("#gp-yd-form");
        c.Find("#gp-yd-tckn").Input("123");
        Assert.True(c.Find("#cekmece-gider-pusulasi-yeniden-kaydet").HasAttribute("disabled"));
        Assert.Contains(Yasal4Sozlugu.Tr.YdKilit, c.Markup);
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/yeniden-duzenle"));
    }

    // ── Y4-B ────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4B_stopaj_sekmesi_acik_donem_tabloda_odenmis_ay_KAPALI_katlanirda_Odendi_POST()
    {
        var c = Ciz("stopaj");
        c.WaitForElement("#sp-tablo");
        Assert.Contains(istekler, i => i.Yol.EndsWith("/stopaj/donemler"));
        Assert.NotNull(c.Find("#sp-satir-2026-08"));
        Assert.Contains("satir-kirmizi", c.Find("#sp-satir-2026-08").ClassName);
        Assert.Empty(c.FindAll("#sp-satir-2026-07"));            // ödenmiş ay kapalı akordeonda
        Assert.NotNull(c.Find("#sp-odenmis"));
        Assert.Contains(Yasal4Sozlugu.Tr.StopajTeyitBekliyor, c.Find("#sp-satir-2026-08").TextContent);

        c.Find("#sp-ode-2026-08").Click();
        c.WaitForElement("#sp-od-form");
        Assert.NotNull(c.Find("#sp-od-kasa-tek"));               // UX-S22: tek kasa → seçici yok
        c.Find("#cekmece-stopaj-odemesi-kaydet").Click();
        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.EndsWith("/stopaj/odemeler") && i.Govde.Contains("\"kasaId\":\"k1\"") && i.Govde.Contains("2026-08")));
        c.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(c.Find("#sp-basari").TextContent)));
    }

    [Fact]
    public void Y4B_stopaj_okunamazsa_hata_bandi_bos_durum_DEGIL()
    {
        stopajYaniti = "geçersiz";
        var c = Ciz("stopaj");
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#sp-hata")));
        Assert.Empty(c.FindAll("#sp-tablo"));
        Assert.DoesNotContain(Yasal4Sozlugu.Tr.StopajBos, c.Markup);
    }

    // ── 2. tur hasım bulguları (01.10) ──────────────────────────────────────

    [Fact]
    public void Y4B_Panel_tek_dokunus_ode_parametresi_o_donemin_odeme_cekmecesini_ACIK_getirir()
    {
        var c = Ciz("stopaj", "&ode=2026-08");
        c.WaitForElement("#sp-od-form");
        Assert.Contains(Bicim.Para(1340m, "TRY"), c.Find("#sp-od-kalan").TextContent);
        c.Find("#cekmece-stopaj-odemesi-kaydet").Click();
        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.EndsWith("/stopaj/odemeler") && i.Govde.Contains("2026-08")));
    }

    [Fact]
    public void Y4B_ode_parametresi_kalani_olmayan_donemde_cekmece_ACILMAZ()
    {
        var c = Ciz("stopaj", "&ode=2026-07");
        c.WaitForElement("#sp-tablo");
        Assert.Empty(c.FindAll("#sp-od-form"));
    }

    [Fact]
    public void Y4B_fazla_odeme_EKSI_kalan_acik_tabloda_rozetle_gorunur_Odendi_dugmesi_YOK()
    {
        stopajYaniti = """
            {"yil":2026,"donemler":[
              {"donem":"2026-06","kesilenTl":100.00,"odenenTl":150.00,"kalanTl":-50.00,"sonGun":"2026-07-26",
               "sonGunTeyit":"TEYITSIZ","sonGunGecti":false,"pusulaSayisi":1,"odemeler":[]}]}
            """;
        var c = Ciz("stopaj");
        c.WaitForElement("#sp-tablo");
        Assert.Equal(Yasal4Sozlugu.Tr.StopajFazlaOdeme, c.Find("#sp-fazla-2026-06").TextContent.Trim());
        Assert.Contains(Bicim.Para(-50m, "TRY"), c.Find("#sp-kalan-2026-06").TextContent);
        Assert.Empty(c.FindAll("#sp-ode-2026-06"));
    }

    [Fact]
    public void Y4A_mukellef_fatura_bekleyen_BILGI_satiri_sayiyla_geri_sayimsiz()
    {
        sureYaniti = Sure.Replace("\"uyarilar\":[]", "\"uyarilar\":[],\"faturaBekleyenSayisi\":2", StringComparison.Ordinal);
        var c = Ciz();
        var s = c.Find("#gp-fatura-bekleyen");
        Assert.Contains(string.Format(Yasal4Sozlugu.Tr.FaturaBekleyenSatir, 2), s.TextContent);
        Assert.Empty(s.QuerySelectorAll(".rozet"));
    }

    [Fact]
    public void Y4A_fatura_bekleyen_0_iken_satir_YOK()
    {
        var c = Ciz();
        Assert.Empty(c.FindAll("#gp-fatura-bekleyen"));
    }

    [Fact]
    public void Y4B_stopaj_sekmesi_acilmadikca_donem_ucu_CAGRILMAZ()
    {
        Ciz();
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/stopaj/"));
    }
}

public sealed class Yasal4UrunKmtsEkranTestleri : BunitContext
{
    private bool kmtsAcik;

    public Yasal4UrunKmtsEkranTestleri()
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
    }

    private sealed class SahteApi(Yasal4UrunKmtsEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kmts/ayar") => t.kmtsAcik ? """{"acik":true,"bicimDenetimi":false}""" : """{"acik":false,"bicimDenetimi":false}""",
                var y when y.EndsWith("/madenler") => """{"madenler":[]}""",
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> YeniFormAc(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1", mod, mod));
        var c = Render<Kuyumcu.Web.Pages.Urunler>();
        c.WaitForElement("#urun-yeni-ac");
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#urun-yeni-ac")));
        c.Find("#urun-yeni-ac").Click();
        c.WaitForElement("#u-ad");
        return c;
    }

    [Fact]
    public void Y4C_K13_ayar_kapali_PRO_formda_KMTS_alani_YOK()
    {
        var c = YeniFormAc(ArayuzModuServisi.Profesyonel);
        Assert.Empty(c.FindAll("#u-kmts"));
        Assert.Empty(c.FindAll("#u-ek-bilgiler"));
    }

    [Fact]
    public void Y4C_K13_ayar_acik_PRO_katlanirda_KMTS_alani_1_ve_ipucu_jargonsuz()
    {
        kmtsAcik = true;
        var c = YeniFormAc(ArayuzModuServisi.Profesyonel);
        c.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        c.WaitForElement("#u-ek-bilgiler");
        c.Find("#u-ek-bilgiler-ac").Click();
        Assert.Single(c.FindAll("#u-kmts"));
        Assert.DoesNotContain("sunucu doğrulamaz", c.Markup);
        Assert.Contains(Yasal4Sozlugu.Tr.KmtsIpucu, c.Markup);
    }

    [Fact]
    public void Y4C_K13_ayar_acik_BASIT_modda_KMTS_alani_YOK()
    {
        kmtsAcik = true;
        var c = YeniFormAc(ArayuzModuServisi.Basit);
        Assert.Empty(c.FindAll("#u-kmts"));
        Assert.Empty(c.FindAll("#u-ek-bilgiler"));
    }
}

/// <summary>Hasım bulgusu 01.10 (§ 4.4): desen yazılınca mevcut uymayan KMTS kodları İşletme → Ayarlar'da sayıyla görünür.</summary>
public sealed class Yasal4IsletmeKmtsEkranTestleri : BunitContext
{
    private string kmtsYaniti = """{"acik":true,"bicimDenetimi":false,"uymayanSayisi":0}""";

    public Yasal4IsletmeKmtsEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(Yasal4IsletmeKmtsEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kmts/ayar") => t.kmtsYaniti,
                var y when y.EndsWith("/admin/tenant/kimlik") || y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"Test Kuyumculuk","plan":"BASLANGIC","aiEtkin":false,"varsayilanArayuzModu":"PROFESYONEL","adres":null,"telefon":null,"vergiDairesi":null,"vergiNo":"1234567890","kurKaynagi":"ALTINKAYNAK","maliyetYontemi":"FIFO"}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true}]""",
                var y when y.EndsWith("/abonelik") => """{"planKodu":"BASLANGIC","planAdi":"Başlangıç","yazabilirMi":true,"planlar":[],"gecmis":[]}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> AyarlarAc()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement($"#isl-sekme-{GorselSigmaSozlugu.IsAyarlar}").Click();
        c.WaitForElement("#i-kmts-kullanimi");
        return c;
    }

    [Fact]
    public void Y4C_uymayan_KMTS_kodu_varsa_sayi_ve_urunlere_baglanti_gorunur()
    {
        kmtsYaniti = """{"acik":true,"bicimDenetimi":true,"uymayanSayisi":3}""";
        var c = AyarlarAc();
        c.WaitForAssertion(() => Assert.Contains(string.Format(Yasal4Sozlugu.Tr.KmtsUymayan, 3), c.Find("#i-kmts-uymayan").TextContent));
        Assert.Equal("/urunler", c.Find("#i-kmts-uymayan").GetAttribute("href"));
    }

    [Fact]
    public void Y4C_uymayan_yokken_satir_YOK()
    {
        var c = AyarlarAc();
        Assert.Empty(c.FindAll("#i-kmts-uymayan"));
    }
}
