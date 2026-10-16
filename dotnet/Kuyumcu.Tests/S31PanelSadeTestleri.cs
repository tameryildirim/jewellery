using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-1 — PANEL "SABAHIN BEŞ SORUSU" (basit yüz) · tuval Panel.dc.html
//
// İKİ YÜZ, TEK GÖVDE (PLAN § 5):
//   • BASİT  → selam + tarih (+hava) tek satır · "Yeni satış" TEK birincil ·
//              Bugün ne sattım? · Kasada ne var? · Altın pozisyonum (B-2) ·
//              Kim borçlu? (satırda tek eylem) · Bekleyen iş.
//   • PROFESYONEL → P-37b kutucuk ızgarası AYNEN (P37bKarsilamaTestleri korur).
//
// Sınanan sözleşmeler: UX-S23 (sayı → kayıt bağlantısı) · UX-S28 (hata / 403
// "kayıt yok"a çevrilmez) · UX-S29 (sınır yazılır) · UX-S31 (ham kod yok) ·
// UX-S32 (para birimleri toplanmaz) · yetki ≠ mod · tek birincil düğme ·
// yüze göre yükleme (gereksiz istek yok) · mod değişince eksik yüz yüklenir.
// ⚠ bUnit sözleşmesi: sahte API "{}" varsayılanı bırakmaz — her uca GERÇEKÇİ yanıt.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S31PanelSadeTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    /// <summary>Yol parçası → (durum kodu, gövde). Verilmeyen uç varsayılan gerçekçi yanıtı alır.</summary>
    private readonly Dictionary<string, (HttpStatusCode Kod, string Govde)> ozel = [];

    private const string GunlukVarsayilan = """
        {"tarih":"2026-09-23","satis":{"adet":3,"toplamTl":57550.00,"toplamHas":10.4},
         "alis":{"adet":1,"toplamTl":3000.00,"toplamHas":1.0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":9.4,"hareketSayisi":4,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":44200.00,"paraBirimi":"TRY"},
                    {"id":"k2","ad":"Banka","bakiye":40000.00,"paraBirimi":"TRY"},
                    {"id":"k3","ad":"Dolar Kasa","bakiye":500.00,"paraBirimi":"USD"}]}
        """;

    private const string YaslandirmaVarsayilan = """
        {"yaslananCariler":[
           {"cariId":"c1","cariAd":"Hasan Kaya","cariTur":"MUSTERI","tlBakiye":38900.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":40,"dilim":"31-60"},
           {"cariId":"c2","cariAd":"Zeynep Demir","cariTur":"MUSTERI","tlBakiye":12000.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":5,"dilim":"0-30"},
           {"cariId":"c3","cariAd":"Mehmet Öz","cariTur":"TEDARIKCI","tlBakiye":0,"hasGramBakiye":-22.0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"}],
         "ozetTl":{"0-30":12000.00,"31-60":38900.00,"61-90":0,"90+":0,"BELIRSIZ":0},
         "toplamSatir":3,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    private static string VadeVarsayilan()
    {
        var yaklasan = DateOnly.FromDateTime(DateTime.Today).AddDays(3).ToString("yyyy-MM-dd");
        return $$"""
            {"ufukGun":7,"gecikenToplam":38900.00,"yaklasanToplam":12000.00,"vadesizToplam":0,
             "ufukOtesiToplam":0,"acikToplam":50900.00,
             "cariler":[
               {"cariId":"c1","cariAd":"Hasan Kaya","cariTur":"MUSTERI","telefon":null,
                "gecikenTutar":38900.00,"yaklasanTutar":0,"ufukOtesiTutar":0,"acikToplamTutar":38900.00,
                "vadesizTutar":0,"enEskiGecikmeGun":12,"enYakinVade":"2026-09-11","borclar":[]},
               {"cariId":"c2","cariAd":"Zeynep Demir","cariTur":"MUSTERI","telefon":null,
                "gecikenTutar":0,"yaklasanTutar":12000.00,"ufukOtesiTutar":0,"acikToplamTutar":12000.00,
                "vadesizTutar":0,"enEskiGecikmeGun":null,"enYakinVade":"{{yaklasan}}","borclar":[]}]}
            """;
    }

    private const string BildirimVarsayilan = """
        {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,
         "bildirimler":[
           {"id":"b1","tur":"VADE_GECIKTI","onem":"KRITIK","baslik":"Hasan Kaya'nın vadesi geçti",
            "govde":"38.900 TL","hedefRota":"/vade-takibi","ilgiliVarlikTuru":"Cari","ilgiliVarlikId":"c1",
            "olusturuldu":"2026-09-23T06:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
           {"id":"b2","tur":"DIS","onem":"UYARI","baslik":"Dış bağlantılı iş",
            "govde":"","hedefRota":"//kotu.ornek/yol","ilgiliVarlikTuru":null,"ilgiliVarlikId":null,
            "olusturuldu":"2026-09-23T05:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
        """;

    private const string PozisyonVarsayilan = """
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":14.4,"cariBorcGram":0,
                  "netGram":312.8,"alacakliCariSayisi":1,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-09-23T09:00:00Z","donemdenBagimsiz":true}
        """;

    private const string KarVarsayilan = """
        {"hasilatTl":57550.00,"maliyetTl":53430.00,"karTl":4120.00,"karMarjiYuzde":7.16,
         "hasilatHas":10.4,"maliyetHas":10.0,"karHas":0.4,"satisAdedi":3,
         "maliyetiBilinenAdet":3,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","uyari":null}
        """;

    private sealed class SahteApi(
        List<(string, string)> kayit,
        Dictionary<string, (HttpStatusCode, string)> ozel) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add((istek.Method.Method, tam));

            foreach (var (parca, (kod, govde)) in ozel)
            {
                if (yol.Contains(parca, StringComparison.Ordinal))
                    return Task.FromResult(Yanit(kod, govde));
            }

            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => GunlukVarsayilan,
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) => YaslandirmaVarsayilan,
                var y when y.EndsWith("/raporlar/vade-takibi", StringComparison.Ordinal) => VadeVarsayilan(),
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) =>
                    """{"gunler":[{"tarih":"2026-09-22","satisTl":1000.00}]}""",
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-09-23","bitis":"2026-09-23","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) => BildirimVarsayilan,
                // T-P1: kâr (bugün..bugün) ve banka hesapları — gerçekçi şekil (KarZararServisi.Donem / BankaServisi.HesapListele).
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) => KarVarsayilan,
                var y when y.EndsWith("/bankalar", StringComparison.Ordinal) => """{"hesaplar":[]}""",
                // PARA-DEMO-2 tur 5: "Kasada ne var?" alt satırı nakit/kart ayrımını bu uçtan okur — gerçekçi şekil
                // (KasaNakitAyrimiOzeti); bu fikstürde kasalara kart/havale yazılmamış: nakit = bakiye.
                var y when y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal) => """
                    [{"kasaId":"k1","paraBirimi":"TRY","toplam":44200.00,"nakit":44200.00,"kart":0,"havale":0},
                     {"kasaId":"k2","paraBirimi":"TRY","toplam":40000.00,"nakit":40000.00,"kart":0,"havale":0},
                     {"kasaId":"k3","paraBirimi":"USD","toplam":500.00,"nakit":500.00,"kart":0,"havale":0}]
                    """,
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => PozisyonVarsayilan,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(Yanit(HttpStatusCode.OK, cevap));
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) => new(kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public S31PanelSadeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, ozel))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        // Hava KAPALI: boş yapılandırma = istek atılmaz (HavaServisi sözleşmesi).
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private void OturumAc(string mod, string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit(string rol = "SAHIP")
    {
        OturumAc(ArayuzModuServisi.Basit, rol);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-sade");
        return e;
    }

    /// <summary>Her kutucuk kendi iskeletini taşır; iskelet kalmadıysa yükleyiciler bitti.</summary>
    private static void Bekle(IRenderedComponent<Kuyumcu.Web.Pages.Panel> e)
        => e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll(".karsilama-iskelet"));
            Assert.Empty(e.FindAll(".yukleniyor-kutu"));
        }, TimeSpan.FromSeconds(10));

    private List<string> Yollar() { lock (istekler) return [.. istekler.Select(i => i.Yol)]; }

    // ── İKİ YÜZ ─────────────────────────────────────────────────────────────

    [Fact]
    public void S31_01_Basit_yuz_SADE_duzeni_cizer_izgara_YOK()
    {
        var e = Basit();
        Bekle(e);

        Assert.NotNull(e.Find("#panel-soru-satis"));
        Assert.NotNull(e.Find("#panel-soru-kasa"));
        Assert.NotNull(e.Find("#pozisyon-kart"));          // B-2 kartı yerleşti
        Assert.NotNull(e.Find("#panel-soru-borclu"));
        Assert.NotNull(e.Find("#panel-soru-bekleyen"));
        Assert.Empty(e.FindAll("#panel-izgara"));
        Assert.Empty(e.FindAll("#panel-duzen-modu"));
        // Selam satırı iki yüzde de aynı id ile durur (bUnit/QA sözleşmesi).
        Assert.Contains("Ramazan", e.Find("#panel-selam").TextContent, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#panel-tarih").TextContent));
    }

    [Fact]
    public void S31_02_Basit_yuzde_TEK_birincil_dugme_Yeni_satis()
    {
        var e = Basit();
        Bekle(e);

        var birincil = e.FindAll(".dugme-birincil, .btn-birincil");
        Assert.Single(birincil);
        Assert.Equal("panel-yeni-satis", birincil[0].Id);
        Assert.Equal("/islemler", birincil[0].GetAttribute("href"));
        Assert.Contains("Yeni satış", birincil[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_03_Profesyonel_yuz_P37b_izgarasini_AYNEN_cizer()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Bekle(e);

        foreach (var k in PanelKutucuklari.Tumu)
            Assert.NotNull(e.Find($"#panel-kutucuk-{k.Kod}"));
        Assert.NotNull(e.Find("#panel-duzen-modu"));
        Assert.NotNull(e.Find("#panel-satis-yap"));
        Assert.Empty(e.FindAll("#panel-sade"));
        Assert.Empty(e.FindAll("#panel-yeni-satis"));
        Assert.Empty(e.FindAll("#pozisyon-kart"));
    }

    // ── YÜZE GÖRE YÜKLEME ───────────────────────────────────────────────────

    [Fact]
    public void S31_04_Her_yuz_YALNIZ_kendi_isteklerini_atar()
    {
        var e = Basit();
        Bekle(e);
        var basitYollar = Yollar();
        Assert.Contains(basitYollar, y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
        Assert.Contains(basitYollar, y => y.Contains("/bildirimler", StringComparison.Ordinal));
        Assert.DoesNotContain(basitYollar, y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));
        Assert.DoesNotContain(basitYollar, y => y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal));
        Assert.DoesNotContain(basitYollar, y => y.Contains("/kur/kaynaklar", StringComparison.Ordinal));
    }

    [Fact]
    public void S31_05_Profesyonel_yuz_basit_yuzun_isteklerini_ATMAZ()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Bekle(e);
        var yollar = Yollar();
        Assert.Contains(yollar, y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/bildirimler", StringComparison.Ordinal));
    }

    [Fact]
    public void S31_06_Mod_calisirken_degisince_eksik_yuz_YUKLENIR()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Bekle(e);

        Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit);
        e.WaitForElement("#panel-sade");
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-borclu-c1")));
        Assert.Contains(Yollar(), y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
    }

    // ── (1) BUGÜN NE SATTIM? ────────────────────────────────────────────────

    [Fact]
    public void S31_07_Satis_karti_TUTAR_ADET_HAS_yazar_ve_kayitlara_baglanir()
    {
        var e = Basit();
        Bekle(e);

        var deger = e.Find("#panel-soru-satis-deger");
        Assert.Equal(Bicim.TlKurus(57550m), deger.TextContent.Trim());
        Assert.Equal("/raporlar/islem-defteri", deger.GetAttribute("href"));   // UX-S23
        Assert.Contains("3 satış", e.Find("#panel-soru-satis-adet").TextContent, StringComparison.Ordinal);
        // RAPOR-GORUNUM (veri a): Panel has dili Rapor merkezi ve pozisyon kartıyla TEK (Bicim.Has, N4).
        Assert.Contains("10,4000 has", e.Find("#panel-soru-satis-has").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_08_Satis_yoksa_SADE_cumle_yazar()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.OK, """
            {"tarih":"2026-09-23","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
             "netHasGramHareketi":0,"hareketSayisi":0,
             "kasalar":[{"id":"k1","ad":"Nakit","bakiye":100.00,"paraBirimi":"TRY"}]}
            """);
        var e = Basit();
        Bekle(e);

        Assert.Equal("Bugün henüz satış yok.", e.Find("#panel-soru-satis-yok").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-soru-satis-adet"));
    }

    [Fact]
    public void S31_09_Ozet_ucu_PATLARSA_sayi_cizilmez_bant_durur()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.InternalServerError, """{"hata":"rapor ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        // UX-S28: hata "0 ₺"a çevrilmez.
        Assert.NotNull(e.Find("#panel-soru-satis-hata"));
        Assert.NotNull(e.Find("#panel-soru-kasa-hata"));
        Assert.Empty(e.FindAll("#panel-soru-satis-deger"));
        Assert.Empty(e.FindAll("#panel-soru-kasa-deger"));
        Assert.Empty(e.FindAll("#panel-soru-satis-yetki"));
    }

    [Fact]
    public void S31_10_Ozet_403_ise_YETKI_notu_hata_bandi_degil()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.Forbidden, """{"hata":"Bu işlem için yetkiniz yok"}""");
        var e = Basit("TEZGAHTAR");
        Bekle(e);

        Assert.Equal("Bu bilgiyi yalnız yönetici görür.", e.Find("#panel-soru-satis-yetki").TextContent.Trim());
        Assert.NotNull(e.Find("#panel-soru-kasa-yetki"));
        Assert.Empty(e.FindAll("#panel-soru-satis-hata"));
        Assert.Empty(e.FindAll("#panel-soru-satis-deger"));
    }

    // ── (2) KASADA NE VAR? — UX-S32 ─────────────────────────────────────────

    [Fact]
    public void S31_11_Kasa_karti_PARA_BIRIMLERINI_TOPLAMAZ()
    {
        var e = Basit();
        Bekle(e);

        // TL kasaları: 44.200 + 40.000 = 84.200 ₺ — Dolar kasası (500 $) EKLENMEZ.
        var deger = e.Find("#panel-soru-kasa-deger");
        Assert.Equal(Bicim.BelgePara(84200m, "TRY"), deger.TextContent.Trim());
        Assert.Equal("/kasalar", deger.GetAttribute("href"));
        // T-P1 (tuval): alt satır "Nakit X ₺" — TL kasalarının toplamı (banka hesabı yoksa banka yazılmaz).
        Assert.Equal($"Nakit {Bicim.BelgePara(84200m, "TRY")}", e.Find("#panel-soru-kasa-grup").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-soru-kasa-banka"));

        var dolar = e.Find("#panel-soru-kasa-grup-1").TextContent;
        Assert.Contains("Dolar kasalar", dolar, StringComparison.Ordinal);
        Assert.Contains(Bicim.BelgePara(500m, "USD"), dolar, StringComparison.Ordinal);
        Assert.DoesNotContain("84.700", e.Markup, StringComparison.Ordinal);
        // Ham para kodu ekrana basılmaz (UX-S31).
        Assert.DoesNotContain("USD", e.Find("#panel-soru-kasa").TextContent, StringComparison.Ordinal);
    }

    // ── (4) KİM BORÇLU? ─────────────────────────────────────────────────────

    [Fact]
    public void S31_12_Kim_borclu_satirda_TEK_eylem_yon_dogru()
    {
        var e = Basit();
        Bekle(e);

        var c1 = e.Find("#panel-borclu-c1");
        Assert.Single(c1.QuerySelectorAll("a, button"));
        var tahsil = e.Find("#panel-borclu-eylem-c1");
        Assert.Equal("Tahsil et", tahsil.TextContent.Trim());
        Assert.Equal("/cariler?cariId=c1&islem=tahsilat", tahsil.GetAttribute("href"));   // sunum 14.10: Tahsil et → tahsilat penceresi

        var ode = e.Find("#panel-borclu-eylem-c3");
        Assert.Equal("Öde", ode.TextContent.Trim());
        Assert.Equal("/cariler?cariId=c3", ode.GetAttribute("href"));
        Assert.Contains("panel-liste-tutar--eksi", e.Find("#panel-borclu-tutar-c3").ClassName, StringComparison.Ordinal);
        Assert.Contains("−22,0000 has", e.Find("#panel-borclu-tutar-c3").TextContent, StringComparison.Ordinal);   // RAPOR-GORUNUM: tek has dili
        Assert.Equal("Biz müşteriye borçluyuz", e.Find("#panel-borclu-durum-c3").TextContent.Trim());
    }

    [Fact]
    public void S31_13_Kim_borclu_VADE_durumunu_sade_yazar()
    {
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() =>
            Assert.Equal("Vade geçti", e.Find("#panel-borclu-durum-c1").TextContent.Trim()));
        // T-P1: tuvaldeki kısa metin; gecikme gün sayısı SİLİNMEDİ, title'da okunur.
        Assert.Equal("12 gündür gecikmede", e.Find("#panel-borclu-durum-c1").GetAttribute("title"));
        Assert.Contains("panel-liste-durum--gecti", e.Find("#panel-borclu-durum-c1").ClassName, StringComparison.Ordinal);
        Assert.Equal("3 gün kaldı", e.Find("#panel-borclu-durum-c2").TextContent.Trim());
    }

    [Fact]
    public void S31_14_Kim_borclu_SIRA_once_alacak_sonra_borc()
    {
        var e = Basit();
        Bekle(e);

        var sira = e.FindAll("#panel-soru-borclu-liste > li").Select(li => li.Id).ToList();
        Assert.Equal(["panel-borclu-c1", "panel-borclu-c2", "panel-borclu-c3"], sira);
    }

    [Fact]
    public void S31_15_Kim_borclu_SINIRI_ister_ve_daha_fazlasini_soyler()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK, YaslandirmaVarsayilan
            .Replace("\"toplamSatir\":3", "\"toplamSatir\":812", StringComparison.Ordinal)
            .Replace("\"dahaFazlasiVar\":false", "\"dahaFazlasiVar\":true", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        Assert.Contains(Yollar(), y => y.Contains("cari-yaslandirma?limit=500", StringComparison.Ordinal));
        Assert.Contains("500", e.Find("#panel-soru-borclu-sinir").TextContent, StringComparison.Ordinal);
        Assert.Contains("812 açık hesap", e.Find("#panel-soru-borclu-sayi").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_16_Kim_borclu_403_YETKI_notu_500_BANT_bos_liste_BOS_DURUM()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        var e = Basit("TEZGAHTAR");
        Bekle(e);
        Assert.NotNull(e.Find("#panel-soru-borclu-yetki"));
        Assert.Empty(e.FindAll("#panel-soru-borclu-hata"));
        Assert.DoesNotContain("Açık hesap yok.", e.Find("#panel-soru-borclu").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_17_Kim_borclu_HATA_bos_duruma_cevrilmez()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.InternalServerError, """{"hata":"rapor ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        Assert.NotNull(e.Find("#panel-soru-borclu-hata"));
        Assert.DoesNotContain("Açık hesap yok.", e.Find("#panel-soru-borclu").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_18_Kim_borclu_GERCEKTEN_bossa_bos_durum()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);

        Assert.Contains("Açık hesap yok.", e.Find("#panel-soru-borclu").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#panel-soru-borclu-liste"));
    }

    [Fact]
    public void S31_19_Vade_ucu_patlarsa_LISTE_KALIR_bant_cikar()
    {
        ozel["/raporlar/vade-takibi"] = (HttpStatusCode.InternalServerError, """{"hata":"vade ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-borclu-vade-hata")));
        Assert.NotNull(e.Find("#panel-borclu-c1"));
        Assert.Equal("Açık hesap", e.Find("#panel-borclu-durum-c1").TextContent.Trim());
    }

    // ── (5) BEKLEYEN İŞ ─────────────────────────────────────────────────────

    [Fact]
    public void S31_20_Bekleyen_is_bildirimleri_BAGLANTIYLA_ham_kodsuz_listeler()
    {
        var e = Basit();
        Bekle(e);

        Assert.Contains(Yollar(), y => y.Contains("/bildirimler?durum=ACIK&limit=5", StringComparison.Ordinal));
        var git = e.Find("#panel-is-git-b1");
        Assert.Equal("/vade-takibi", git.GetAttribute("href"));
        Assert.Contains("vadesi geçti", git.TextContent, StringComparison.Ordinal);
        // "//" ile başlayan rota uygulama dışıdır → bağlantı OLMAZ.
        Assert.Empty(e.FindAll("#panel-is-git-b2"));
        Assert.NotNull(e.Find("#panel-is-b2"));
        // Ham önem/tür kodu ekrana basılmaz (UX-S31); önem sade adla okunur.
        var metin = e.Find("#panel-soru-bekleyen").OuterHtml;
        Assert.DoesNotContain("KRITIK", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("VADE_GECIKTI", metin, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Acil\"", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_21_Bekleyen_is_SINIRA_degdiyse_soyler()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            BildirimVarsayilan.Replace("\"dahaFazlasiVar\":false", "\"dahaFazlasiVar\":true", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        Assert.Contains("İlk 5 iş", e.Find("#panel-soru-bekleyen-sinir").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_22_Bekleyen_is_GERCEKTEN_bossa_bos_durum_hatada_DEGIL()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""");
        var e = Basit();
        Bekle(e);
        Assert.Contains("Bekleyen iş yok.", e.Find("#panel-soru-bekleyen").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_23_Bekleyen_is_HATASI_bos_duruma_cevrilmez()
    {
        ozel["/bildirimler"] = (HttpStatusCode.InternalServerError, """{"hata":"bildirim ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        Assert.NotNull(e.Find("#panel-soru-bekleyen-hata"));
        Assert.DoesNotContain("Bekleyen iş yok.", e.Find("#panel-soru-bekleyen").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_24_Bekleyen_is_UYUM_sayaclarini_yetkili_role_gosterir()
    {
        ozel["/masak/ozet"] = (HttpStatusCode.OK, """{"suresiGecmis":2,"yaklasan":0}""");
        var e = Basit();
        Bekle(e);

        var metin = e.Find("#panel-soru-bekleyen").TextContent;
        Assert.Contains("2", metin, StringComparison.Ordinal);
        Assert.NotEmpty(e.Find("#panel-soru-bekleyen").QuerySelectorAll("a[href='/uyum']"));
    }

    // ── KAYNAK SÖZLEŞMESİ ───────────────────────────────────────────────────

    [Fact]
    public void S31_25_Panel_basit_yuzde_POST_etmez_ve_esik_sayisi_gommez()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Panel.razor");
        var bas = kaynak.IndexOf("PAKET S3-1 — BASİT YÜZ", StringComparison.Ordinal);
        var son = kaynak.IndexOf("// ── Karşılama başlığı", StringComparison.Ordinal);
        Assert.True(bas > 0 && son > bas);
        var sade = kaynak[bas..son];
        Assert.DoesNotContain("PostAsync", sade, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchAsync", sade, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteAsync", sade, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", sade, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_26_Sozluk_TR_ve_EN_DOLU_ve_farkli()
    {
        var tr = Kuyumcu.Core.I18n.PanelSadeSozlugu.Tr;
        var en = Kuyumcu.Core.I18n.PanelSadeSozlugu.En;
        foreach (var p in typeof(Kuyumcu.Core.I18n.PanelSadeMetinleri).GetProperties())
        {
            if (p.PropertyType != typeof(string)) continue;
            var t = (string)p.GetValue(tr)!;
            var n = (string)p.GetValue(en)!;
            Assert.False(string.IsNullOrWhiteSpace(t), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(n), p.Name);
        }
        Assert.NotEqual(tr.KimBorclu, en.KimBorclu);
        Assert.Equal("Diğer kasalar", Kuyumcu.Core.I18n.PanelSadeSozlugu.KasaGrubu("XAU", "tr"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAKET T-P1 — PANEL TUVALE (tuval/project/Panel.dc.html birebir)
    // Sözleşmeler: kâr uçtan gelir, yoksa yazılmaz (uydurma yok) · kasa "Nakit ·
    // Banka" TL, döviz toplanmaz · Kim borçlu en çok 3 satır + Tümü (sayı
    // silinmez) · Bekleyen iş satırında kaynak etiketi (ham kod yok) · uyum
    // sayaçları nokta satırı (emoji yok) · pozisyon kartı sade + Ayrıntı ·
    // hava "İstanbul 21°" (durum title'da) · CSS belirteçli, tuval ölçülü.
    // ═══════════════════════════════════════════════════════════════════════

    private static string Bugun => DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void TP1_01_Satis_karti_KAR_ucundan_gelir_ve_kayda_baglanir()
    {
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-kar")));
        var kar = e.Find("#panel-soru-satis-kar");
        Assert.Equal($"Kâr {Bicim.TlKurus(4120m)}", kar.TextContent.Trim());
        Assert.Equal("/raporlar/kar-zarar", kar.GetAttribute("href"));        // UX-S23
        Assert.DoesNotContain("panel-soru-kar--eksi", kar.ClassName, StringComparison.Ordinal);
        Assert.Contains(Yollar(), y => y.Contains($"/raporlar/kar-zarar?baslangic={Bugun}&bitis={Bugun}", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#panel-soru-satis-kar-hata"));
    }

    [Fact]
    public void TP1_02a_Kar_403_ise_KAR_YAZILMAZ_uydurma_sifir_yok_bant_yok()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        var e = Basit("TEZGAHTAR");
        Bekle(e);

        Assert.NotNull(e.Find("#panel-soru-satis-deger"));
        Assert.Empty(e.FindAll("#panel-soru-satis-kar"));
        Assert.Empty(e.FindAll("#panel-soru-satis-kar-hata"));
        Assert.DoesNotContain("Kâr", e.Find("#panel-soru-satis").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_02b_Kar_ucu_PATLARSA_bant_cikar_satis_kalir()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.InternalServerError, """{"hata":"kâr ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-kar-hata")));
        Assert.Empty(e.FindAll("#panel-soru-satis-kar"));
        Assert.Equal(Bicim.TlKurus(57550m), e.Find("#panel-soru-satis-deger").TextContent.Trim());
    }

    [Fact]
    public void TP1_03_Maliyeti_bilinmeyen_varsa_RAKAM_basilmaz_eksik_der()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.OK,
            KarVarsayilan.Replace("\"karTl\":4120.00", "\"karTl\":-350.00", StringComparison.Ordinal)
                         .Replace("\"maliyetiBilinmeyenAdet\":0", "\"maliyetiBilinmeyenAdet\":2", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-kar")));
        var kar = e.Find("#panel-soru-satis-kar");
        // Maliyeti bilinmeyen satış varken rakam BASILMAZ (yanıltıcı olurdu); eksik olduğu söylenir.
        Assert.Contains("2 ürünün maliyeti bilinmiyor", kar.GetAttribute("title"), StringComparison.Ordinal);
        Assert.Equal("Kâr: maliyet eksik", kar.TextContent.Trim());
        Assert.Contains("panel-soru-kar--eksik", kar.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.Tl(-350m), e.Find("#panel-soru-satis").TextContent, StringComparison.Ordinal);
        Assert.Equal("/raporlar/kar-zarar", kar.GetAttribute("href"));
    }

    [Fact]
    public void TP1_03b_Zarar_KIRMIZI_yazilir_maliyet_tamsa()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.OK,
            KarVarsayilan.Replace("\"karTl\":4120.00", "\"karTl\":-350.00", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-kar")));
        var kar = e.Find("#panel-soru-satis-kar");
        Assert.Contains("panel-soru-kar--eksi", kar.ClassName, StringComparison.Ordinal);
        Assert.Equal($"Kâr {Bicim.TlKurus(-350m)}", kar.TextContent.Trim());
    }

    [Fact]
    public void TP1_04_Kasa_karti_NAKIT_ve_BANKA_TL_toplar_doviz_ve_pasif_banka_EKLENMEZ()
    {
        ozel["/bankalar"] = (HttpStatusCode.OK, """
            {"hesaplar":[
              {"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","iban":"TR00","paraBirimi":"TRY","bakiye":40000.00,"aktif":true},
              {"id":"b2","bankaAdi":"Ziraat","hesapAdi":"Eski","iban":"TR01","paraBirimi":"TRY","bakiye":999.00,"aktif":false},
              {"id":"b3","bankaAdi":"Garanti","hesapAdi":"Dolar","iban":"TR02","paraBirimi":"USD","bakiye":100.00,"aktif":true}]}
            """);
        var e = Basit();
        Bekle(e);

        // Kasalar TL 84.200 + banka TL 40.000 = 124.200 ₺ (pasif 999 ₺ ve 100 $ YOK).
        Assert.Equal(Bicim.BelgePara(124200m, "TRY"), e.Find("#panel-soru-kasa-deger").TextContent.Trim());
        Assert.Equal($"Nakit {Bicim.BelgePara(84200m, "TRY")}", e.Find("#panel-soru-kasa-grup").TextContent.Trim());
        var banka = e.Find("#panel-soru-kasa-banka");
        Assert.Equal($"Banka {Bicim.BelgePara(40000m, "TRY")}", banka.TextContent.Trim());
        Assert.Equal("/banka", banka.GetAttribute("href"));
        Assert.Contains(Yollar(), y => y.EndsWith("/bankalar", StringComparison.Ordinal));
        // Kasalar tek tek silinmedi: üzerine gelince okunur.
        Assert.Contains("Nakit", e.Find("#panel-soru-kasa-grup").GetAttribute("title"), StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_05_Banka_403_banka_bacagi_YAZILMAZ_500_ise_BANT()
    {
        ozel["/bankalar"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        var e = Basit();
        Bekle(e);
        Assert.Equal(Bicim.BelgePara(84200m, "TRY"), e.Find("#panel-soru-kasa-deger").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-soru-kasa-banka"));
        Assert.Empty(e.FindAll("#panel-soru-kasa-banka-hata"));

        ozel["/bankalar"] = (HttpStatusCode.InternalServerError, """{"hata":"banka ucu cevap vermedi"}""");
        var e2 = Render<Kuyumcu.Web.Pages.Panel>();
        e2.WaitForElement("#panel-sade");
        Bekle(e2);
        Assert.NotNull(e2.Find("#panel-soru-kasa-banka-hata"));
        Assert.Empty(e2.FindAll("#panel-soru-kasa-banka"));
    }

    [Fact]
    public void TP1_06_Kim_borclu_EN_COK_3_satir_2_tahsil_1_ode_sayi_SILINMEZ()
    {
        static string Cari(string id, decimal tl) =>
            $$"""{"cariId":"{{id}}","cariAd":"Ad {{id}}","cariTur":"MUSTERI","tlBakiye":{{tl.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"}""";
        var satirlar = string.Join(",", new[]
        {
            Cari("a1", 5000m), Cari("a2", 4000m), Cari("a3", 3000m), Cari("a4", 2000m), Cari("a5", 1000m),
            Cari("z1", -700m), Cari("z2", -900m),
        });
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            $$"""{"yaslananCariler":[{{satirlar}}],"ozetTl":{},"toplamSatir":7,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);

        var sira = e.FindAll("#panel-soru-borclu-liste > li").Select(li => li.Id).ToList();
        Assert.Equal(["panel-borclu-a1", "panel-borclu-a2", "panel-borclu-z2"], sira);
        Assert.Equal("Tümü", e.Find("#panel-soru-borclu-tumu").FirstChild!.TextContent.Trim());
        Assert.Contains("7 açık hesap", e.Find("#panel-soru-borclu-sayi").TextContent, StringComparison.Ordinal);
        Assert.Contains("gorsel-gizli", e.Find("#panel-soru-borclu-sayi").ClassName, StringComparison.Ordinal);
        Assert.Equal("7 açık hesap", e.Find("#panel-soru-borclu-tumu").GetAttribute("title"));
    }

    [Fact]
    public void TP1_07_Bekleyen_is_satirinda_KAYNAK_etiketi_ham_kod_basilmaz()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            BildirimVarsayilan.Replace("\"tur\":\"VADE_GECIKTI\"", "\"tur\":\"VADE_HATIRLATMA\"", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        Assert.Equal("Vade", e.Find("#panel-is-kaynak-b1").TextContent.Trim());
        Assert.Equal("Bildirim", e.Find("#panel-is-kaynak-b2").TextContent.Trim());   // bilinmeyen kod uydurulmaz
        var metin = e.Find("#panel-soru-bekleyen").TextContent;
        Assert.DoesNotContain("VADE_HATIRLATMA", metin, StringComparison.Ordinal);
        Assert.Equal("Uyum", Kuyumcu.Core.I18n.PanelTuvalSozlugu.Kaynak("MASAK_SIB_SURESI", "tr"));
        Assert.Equal("System", Kuyumcu.Core.I18n.PanelTuvalSozlugu.Kaynak("SAKLAMA_ONIZLEME", "en"));
    }

    [Fact]
    public void TP1_08_Uyum_sayaclari_NOKTA_satiri_emoji_yok_baglanti_ve_kaynak_var()
    {
        ozel["/masak/ozet"] = (HttpStatusCode.OK, """{"suresiGecmis":2,"yaklasan":0}""");
        var e = Basit();
        Bekle(e);

        var satir = e.Find("#panel-uyum-masak-gecti");
        Assert.Equal("Acil", satir.QuerySelector(".panel-nokta")!.GetAttribute("aria-label"));
        Assert.Contains("panel-nokta--acil", satir.QuerySelector(".panel-nokta")!.ClassName, StringComparison.Ordinal);
        Assert.Equal("/uyum", e.Find("#panel-uyum-masak-gecti-git").GetAttribute("href"));
        Assert.StartsWith("2 ", e.Find("#panel-uyum-masak-gecti-git").TextContent.Trim(), StringComparison.Ordinal);
        Assert.Equal("Uyum", satir.QuerySelector(".panel-is-kaynak")!.TextContent.Trim());
        var bekleyen = e.Find("#panel-soru-bekleyen").TextContent;
        Assert.DoesNotContain("⛔", bekleyen, StringComparison.Ordinal);
        Assert.DoesNotContain("⚠", bekleyen, StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_09_Pozisyon_karti_SADE_kisa_Ayrinti_dugmesi_kalanini_acar()
    {
        ozel["/pozisyon"] = (HttpStatusCode.OK, PozisyonVarsayilan
            .Replace("\"cariBorcGram\":0", "\"cariBorcGram\":22.0", StringComparison.Ordinal)
            .Replace("\"hurdaGram\":0", "\"hurdaGram\":5.5", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        // MADEN-2 (ilke "tek has biçimi"): net pozisyon Bicim.Has ile AYNI — binlik + 4 ondalık.
        Assert.Equal("+312,8000 has", e.Find("#pozisyon-net").TextContent.Trim());
        Assert.Contains("Vitrin 298,4000 has", e.Find("#pozisyon-vitrin").TextContent, StringComparison.Ordinal);
        Assert.Contains("Alacak 14,4000 has", e.Find("#pozisyon-alacak").TextContent, StringComparison.Ordinal);
        // PAKET RAPOR-2 (tutarlılık): sıfır olmayan Borç ve Hurda sade yüzde de kırılımdadır — gösterilen
        // kalemlerin toplamı ana değeri tutar (eskiden yalnız Vitrin + Alacak görünüyordu).
        Assert.NotNull(e.Find("#pozisyon-borc"));
        Assert.NotNull(e.Find("#pozisyon-hurda"));
        Assert.Empty(e.FindAll("#pozisyon-donem"));
        Assert.Empty(e.FindAll("#degismez-ipucu-AltinPozisyonu"));

        var dugme = e.Find("#pozisyon-ayrinti");
        Assert.Equal("false", dugme.GetAttribute("aria-expanded"));
        Assert.Equal("Ayrıntı", dugme.TextContent.Trim());
        dugme.Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#pozisyon-donem")));
        Assert.NotNull(e.Find("#pozisyon-borc"));
        Assert.NotNull(e.Find("#pozisyon-hurda"));
        Assert.NotNull(e.Find("#degismez-ipucu-AltinPozisyonu"));
        Assert.Equal("true", e.Find("#pozisyon-ayrinti").GetAttribute("aria-expanded"));
        Assert.Equal("Kapat", e.Find("#pozisyon-ayrinti").TextContent.Trim());
    }

    [Fact]
    public void TP1_10_Hava_basit_yuzde_tuval_bicimi_derece_kisa_durum_titleda()
    {
        ozel["/hava"] = (HttpStatusCode.OK, """{"derece":21.4,"kod":0,"sehir":"İstanbul","kaynak":"Open-Meteo (CC BY 4.0)"}""");
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.Equal("21°", e.Find("#panel-hava-derece").TextContent.Trim()));
        Assert.Equal("İstanbul", e.Find("#panel-hava-sehir").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-hava-durum"));
        Assert.Equal("Açık · Open-Meteo (CC BY 4.0)", e.Find("#panel-hava").GetAttribute("title"));
        Assert.DoesNotContain("°C", e.Find("#panel-tarih").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_11_Yeni_satis_ARTI_isareti_CSS_ile_ikon_sikkesi_yok()
    {
        var e = Basit();
        var dugme = e.Find("#panel-yeni-satis");
        Assert.Empty(dugme.QuerySelectorAll("svg"));
        Assert.Equal("Yeni satış", dugme.TextContent.Trim());
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-panel-musteri.css");
        Assert.Matches(@"\.panel-yeni-satis::before\s*\{[^}]*linear-gradient\(currentColor", css);
    }

    [Fact]
    public void TP1_12_CSS_blogu_TUVAL_OLCULU_ve_SABIT_RENKSIZ()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-panel-musteri.css");
        var bas = css.IndexOf("PAKET T-P1", StringComparison.Ordinal);
        Assert.True(bas > 0);
        var blok = css[bas..];
        var yorumsuz = System.Text.RegularExpressions.Regex.Replace(blok, @"/\*.*?\*/", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", yorumsuz);   // sabit hex renk yok
        Assert.DoesNotMatch(@"\brgba?\(", yorumsuz);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 130px 130px 110px", yorumsuz, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr)", yorumsuz, StringComparison.Ordinal);
        Assert.Contains("padding: 20px 22px", yorumsuz, StringComparison.Ordinal);
        Assert.Contains("border-radius: 12px", yorumsuz, StringComparison.Ordinal);
        // app.css'e dokunulmadı: T-P1 kuralları yalnız bu şerit dosyasında.
        Assert.DoesNotContain("T-P1", CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"), StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_13_Tuval_sozlugu_TR_ve_EN_DOLU_ve_farkli()
    {
        var tr = Kuyumcu.Core.I18n.PanelTuvalSozlugu.Tr;
        var en = Kuyumcu.Core.I18n.PanelTuvalSozlugu.En;
        foreach (var p in typeof(Kuyumcu.Core.I18n.PanelTuvalMetinleri).GetProperties())
        {
            if (p.PropertyType != typeof(string)) continue;
            var t = (string)p.GetValue(tr)!;
            var n = (string)p.GetValue(en)!;
            Assert.False(string.IsNullOrWhiteSpace(t), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(n), p.Name);
            Assert.NotEqual(t, n);
        }
    }

    [Fact]
    public void TP1_14_Profil_ILK_cizimden_SONRA_gelirse_basit_yuz_yukleyicileri_YINE_acilir()
    {
        // Tam sayfa yenileme: profil yokken yüz PROFESYONEL çizilir; profil BASIT
        // gelince Mod.Degisti tetiklenmeden yeniden çizim olur. Kartlar iskelette KALMAZ.
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.Render();
        e.WaitForElement("#panel-sade");
        Bekle(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-borclu-c1")));
        Assert.Contains(Yollar(), y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
        Assert.Contains(Yollar(), y => y.EndsWith("/bankalar", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#panel-iskelet-soru-kasa"));
    }

    /// <summary>
    /// PAKET YASAL-2 tur 3 (orkestratör 30.09 22:20 madde 3): nakit sınırı aşımı Panel'de TEK
    /// kaynaktan — üstteki kırmızı şerit (GET /tevsik/ihlaller). Aynı olayın bildirimi
    /// (ilgiliVarlikTuru=TevsikIhlali) "Bekleyen iş" listesinde İKİNCİ KEZ sayılmaz; diğer
    /// bildirimler yerinde kalır.
    /// </summary>
    [Fact]
    public void YASAL2_Tur3_nakit_siniri_asimi_TEK_kaynak_serit_bekleyen_iste_ikinci_kez_yok()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK, BildirimVarsayilan.Replace("\"bildirimler\":[", """
            "bildirimler":[
              {"id":"tv1","tur":"TEVSIK_KURTARMA","onem":"KRITIK","baslik":"Nakit sınırı aşıldı — bildirim için son gün 08.10",
               "govde":"x","hedefRota":"/uyum?sekme=nakit","ilgiliVarlikTuru":"TevsikIhlali","ilgiliVarlikId":"i1",
               "olusturuldu":"2026-09-23T07:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
            """, StringComparison.Ordinal));
        ozel["/tevsik/ihlaller"] = (HttpStatusCode.OK, """
            {"ihlaller":[{"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"ODEME","cariId":"c9","cariAd":"Hurda Satıcı","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK","kalanIsGunu":3,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false}],
             "kapanmamisSayisi":1,"sinir":200,"dahaVar":false,"bayramEksik":false}
            """);
        var e = Basit();
        Bekle(e);
        e.WaitForElement("#nsp-odeme");
        Assert.StartsWith("1", e.Find("#nsp-odeme strong").TextContent.Trim(), StringComparison.Ordinal);   // şerit: 1 aşım
        Assert.Empty(e.FindAll("#panel-is-tv1"));                                                              // bekleyen işte tekrar YOK
        Assert.DoesNotContain("Nakit sınırı aşıldı", e.Find("#panel-soru-bekleyen").TextContent, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#panel-is-b1"));                                                               // diğer bildirim yerinde
    }

    /// <summary>YASAL-2 tur 3 (PRO): "Bekleyen işler" kutucuğu şeritle AYNI kaynaktan — aşım varken "iş yok — temiz" demez.</summary>
    [Fact]
    public void YASAL2_Tur3_PRO_bekleyen_kutucugu_serit_sayisini_okur_temiz_demez()
    {
        ozel["/tevsik/ihlaller"] = (HttpStatusCode.OK, """
            {"ihlaller":[{"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"TAHSILAT","cariId":"c9","cariAd":"X","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK","kalanIsGunu":3,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false}],
             "kapanmamisSayisi":1,"sinir":200,"dahaVar":false,"bayramEksik":false}
            """);
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Bekle(e);
        e.WaitForElement("#nsp-tahsilat");
        e.WaitForAssertion(() =>
        {
            var k = e.Find("#panel-kutucuk-bekleyen").TextContent;
            Assert.DoesNotContain("temiz", k, StringComparison.Ordinal);
            // PAKET RAPOR-3 (hasım YÜKSEK): "üstteki kırmızı kutuda" yönlendirmesi yerine iş kutucuğun KENDİ satırında.
            Assert.Contains(string.Format(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.IsNakitSiniri, 1), e.Find("#panel-bekleyen-yasal-nakit").TextContent, StringComparison.Ordinal);
            Assert.Empty(e.FindAll("#panel-kutucuk-bekleyen .bos-durum"));
        });
    }

    /// <summary>YASAL-2 tur 3: tek bildirim nakit aşımıysa "Bekleyen iş yok" DENMEZ — üstteki kutuya yönlendirir.</summary>
    [Fact]
    public void YASAL2_Tur3_yalniz_nakit_asimi_varken_bekleyen_is_YOK_demez_kutuya_yonlendirir()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK, """
            {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[
              {"id":"tv1","tur":"TEVSIK_KURTARMA","onem":"KRITIK","baslik":"Nakit sınırı aşıldı","govde":"x","hedefRota":"/uyum?sekme=nakit",
               "ilgiliVarlikTuru":"TevsikIhlali","ilgiliVarlikId":"i1","olusturuldu":"2026-09-23T07:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
            """);
        var e = Basit();
        Bekle(e);
        var metin = e.Find("#panel-soru-bekleyen").TextContent;
        Assert.DoesNotContain("Bekleyen iş yok", metin, StringComparison.Ordinal);
        // PAKET RAPOR-3 (hasım YÜKSEK): iş kartın KENDİ satırında, ilgili ekrana bağlantıyla (yönlendirme cümlesi yok).
        Assert.Equal("/uyum?sekme=nakit", e.Find("#panel-soru-bekleyen-yasal-nakit-git").GetAttribute("href"));
        Assert.DoesNotContain("üstteki", metin, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#panel-soru-bekleyen .bos-durum"));
        Assert.Empty(e.FindAll("#panel-is-tv1"));
    }

    // ── PAKET YASAL-4 — Y4-A (K-1) · Y4-B (K-8) ──────────────────────────────

    [Fact]
    public void Y4A_K1_iptal_edilmemis_eski_pusula_varken_Panelde_pusula_satiri_YOK()
    {
        // Sunucu: 10 gün önce düzenlenmiş, iptal edilmemiş pusula → takip kümesi BOŞ.
        ozel["/gider-pusulalari/sure-takibi"] = (HttpStatusCode.OK,
            """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,"gecikmis":[],"yaklasan":[],"uyarilar":[]}""");
        // Uyum listesi ÇİZİLSİN diye başka bir sayaç dolu (MASAK) — pusula satırı yine YOK olmalı.
        ozel["/masak/ozet"] = (HttpStatusCode.OK, """{"suresiGecmis":1,"yaklasan":0}""");
        var e = Basit();
        Bekle(e);
        Assert.NotNull(e.Find("#panel-uyum-masak-gecti"));
        Assert.Empty(e.FindAll("#panel-uyum-pusula"));
        Assert.Empty(e.FindAll("#panel-uyum-pusula-yeniden"));
        Assert.DoesNotContain("gider pusulasının 7 günlük düzenleme süresi", e.Find("#panel-soru-bekleyen").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Y4A_iptal_edilmis_yenisiz_pusula_Panelde_son_gunuyle_UYARI_satiri()
    {
        ozel["/gider-pusulalari/sure-takibi"] = (HttpStatusCode.OK,
            """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":1,"gecikmis":[],"yaklasan":[{"id":"p1","pusulaNo":"GP-2026-000001","hareketId":"h1","teslimTarihi":"2026-09-26T10:00:00Z","sonGun":"2026-10-03","kalanGun":2,"brutTutar":1000,"stopajTutar":0}],"enYakinSonGun":"2026-10-03","uyarilar":[]}""");
        var e = Basit();
        Bekle(e);
        var satir = e.Find("#panel-uyum-pusula-yeniden");
        Assert.Contains("03.10.2026", satir.TextContent, StringComparison.Ordinal);
        Assert.Equal("/gider-pusulalari", e.Find("#panel-uyum-pusula-yeniden-git").GetAttribute("href"));
        Assert.Empty(e.FindAll("#panel-uyum-pusula"));
    }

    [Fact]
    public void Y4B_K8_kalan_var_son_gun_gecti_KRITIK_satir_kalan_yoksa_satir_YOK()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK,
            """{"buAy":"2026-10","buAyKesilenTl":240.00,"gecikmis":[{"donem":"2026-08","kesilenTl":1340.00,"odenenTl":0,"kalanTl":1340.00,"sonGun":"2026-09-26","sonGunTeyit":"TEYITSIZ","sonGunGecti":true,"pusulaSayisi":4,"odemeler":[]}],"yaklasan":[],"uyariGunu":5}""");
        var e = Basit();
        Bekle(e);
        var satir = e.Find("#panel-uyum-stopaj-gecti-2026-08");
        Assert.Contains("panel-nokta--acil", satir.QuerySelector(".panel-nokta")!.ClassName, StringComparison.Ordinal);
        Assert.Contains("26.09.2026", satir.TextContent, StringComparison.Ordinal);
        Assert.Equal("/gider-pusulalari?sekme=stopaj", e.Find("#panel-uyum-stopaj-gecti-2026-08-git").GetAttribute("href"));
        // § 4.5 tek dokunuş: "Ödendi olarak kaydet" o dönemin ödeme çekmecesini açar (Panel POST etmez).
        var ode = e.Find("#panel-uyum-stopaj-gecti-2026-08-ode");
        Assert.Equal(Kuyumcu.Core.I18n.Yasal4Sozlugu.Tr.PanelStopajGit, ode.TextContent.Trim());
        Assert.Equal("Ödendi olarak kaydet", ode.TextContent.Trim());
        Assert.Equal("/gider-pusulalari?sekme=stopaj&ode=2026-08", ode.GetAttribute("href"));
        // Kasa: bakiye değişmez, kesilen stopaj ayrıca "vergi dairesine ait" yazar.
        Assert.Contains("vergi dairesine ait", e.Find("#panel-soru-kasa-stopaj").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Y4B_K8_kalan_sifirsa_stopaj_satiri_YOK()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK,
            """{"buAy":"2026-10","buAyKesilenTl":0,"gecikmis":[],"yaklasan":[],"uyariGunu":5}""");
        var e = Basit();
        Bekle(e);
        Assert.Empty(e.FindAll("[id^='panel-uyum-stopaj-']"));
        Assert.Empty(e.FindAll("#panel-soru-kasa-stopaj"));
    }

    [Fact]
    public void Y4B_stopaj_ozeti_okunamazsa_hata_bandi_sayilar_uydurulmaz()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.InternalServerError, """{"message":"Sunucu hatası"}""");
        var e = Basit();
        Bekle(e);
        Assert.Empty(e.FindAll("[id^='panel-uyum-stopaj-']"));
        Assert.NotEmpty(e.FindAll("#panel-soru-stopaj-hata"));
    }

    // ── PAKET YASAL-4 tur 4 (orkestratör 01.10 12:35): tam sayfa yükleme / girişten sonraki ilk Panel ──
    // F5'te ve doğrudan "/" adresinde profil (rol) İLK çizimden SONRA gelir. Panel rolü null
    // görüp uyum/stopaj yüklemesini hiç açmıyordu; stopaj KRİTİK satırı görünmüyor, yerine
    // "Bekleyen iş yok" yazıyordu.

    private const string StopajGecikmisOzet =
        """{"buAy":"2026-10","buAyKesilenTl":240.00,"gecikmis":[{"donem":"2026-08","kesilenTl":1340.00,"odenenTl":0,"kalanTl":1340.00,"sonGun":"2026-09-26","sonGunTeyit":"TEYITSIZ","sonGunGecti":true,"pusulaSayisi":4,"odemeler":[]}],"yaklasan":[],"uyariGunu":5}""";

    private int StopajOzetIstegi() => Yollar().Count(y => y.Contains("/stopaj/ozet", StringComparison.Ordinal));

    [Fact]
    public void Y4_Tur4_oturum_basta_null_sonra_SAHIP_BASIT_stopaj_KRITIK_satiri_gelir_is_yok_DENMEZ()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK, StopajGecikmisOzet);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Assert.Equal(0, StopajOzetIstegi());                    // rol yokken istek YOK
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.Render();
        e.WaitForElement("#panel-sade");
        Bekle(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-uyum-stopaj-gecti-2026-08")));
        Assert.Contains("panel-nokta--acil", e.Find("#panel-uyum-stopaj-gecti-2026-08 .panel-nokta").ClassName, StringComparison.Ordinal);
        Assert.Contains("vergi dairesine ait", e.Find("#panel-soru-kasa-stopaj").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.PanelSadeSozlugu.Tr.BekleyenYok, e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Yasal3Sozlugu.Tr.PanelSeritte, e.Markup, StringComparison.Ordinal);
        Assert.Equal(1, StopajOzetIstegi());                    // BİR KEZ yüklenir
        // Oturum sonradan yine değişirse (jeton yenileme, mod) yükleme TEKRARLANMAZ, iskelete dönmez.
        var o = Services.GetRequiredService<OturumServisi>();
        o.KullaniciAta(o.Kullanici);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-uyum-stopaj-gecti-2026-08")));
        Assert.Empty(e.FindAll("#panel-iskelet-soru-bekleyen"));
        Assert.Equal(1, StopajOzetIstegi());
    }

    [Fact]
    public async Task Y4_Tur4_oturum_var_profil_GELMEDEN_iskelet_kalir_MUHASEBE_gelince_PRO_stopaj_satiri()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK, StopajGecikmisOzet);
        var oturum = Services.GetRequiredService<OturumServisi>();
        await oturum.TokenKaydetAsync("erisim", "yenileme");    // F5: jeton depoda, profil (/auth/ben) henüz yok
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        Assert.NotNull(e.Find("#panel-iskelet-bekleyen"));      // "iş yok" / "rolünüz göremez" DENMEZ
        Assert.Empty(e.FindAll("#panel-bekleyen-rol"));
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Panel.BekleyenYok, e.Markup, StringComparison.Ordinal);
        Assert.Equal(0, StopajOzetIstegi());
        oturum.KullaniciAta(new OturumKullanici("u2", "Mu", "Hasebe", "m@ornek.com", "MUHASEBE", "s1",
            ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-stopaj-gecti-2026-08")));
        Assert.Empty(e.FindAll("#panel-iskelet-bekleyen"));
        Assert.Equal("/gider-pusulalari?sekme=stopaj&ode=2026-08",
            e.Find("#panel-stopaj-gecti-2026-08 a").GetAttribute("href"));
        Assert.Equal(1, StopajOzetIstegi());
    }

    [Fact]
    public async Task Y4_Tur4_profil_TEZGAHTAR_gelirse_iskelet_kalkar_stopaj_istegi_YOK()
    {
        var oturum = Services.GetRequiredService<OturumServisi>();
        await oturum.TokenKaydetAsync("erisim", "yenileme");
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-iskelet-bekleyen");
        oturum.KullaniciAta(new OturumKullanici("u3", "Tez", "Gahtar", "t@ornek.com", "TEZGAHTAR", "s1",
            ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-bekleyen-rol")));
        Assert.Empty(e.FindAll("#panel-iskelet-bekleyen"));
        Assert.Equal(0, StopajOzetIstegi());
    }
}
