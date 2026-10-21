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
// TEMA TEMİZLİĞİ 17.10: iki yüzün eski gövdesi KALDIRILDI — Panel yalnız Yıldız Bugün'ü çizer (BASİT ve PRO aynı).
// Aynı sözleşmeler (aynı yükleyiciler, aynı uçlar) Bugün'ün dev rakamı, cümlesi, "Seni bekleyen" satırları ve alt
// şeridi üzerinde sınanır; yalnız düzene ait testler gerekçeli yorumla kaldırıldı.
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
                // TEMA TEMİZLİĞİ 17.10 (YILDIZ-4C): Bugün'ün iki tuval satırının uçları — gerçekçi yanıt
                // ("{}"/sonuclar varsayılanı liste ayrıştıramaz → sahte emanet hata bandı olurdu).
                var y when y.EndsWith("/raporlar/kasa-defteri", StringComparison.Ordinal) =>
                    """{"baslangic":"2026-09-22","bitis":"2026-09-22","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":0,"kasalar":[{"kasaId":"k1","kasaAd":"Nakit","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":0,"guncelBakiye":0,"gunler":[]}]}""",
                var y when y.EndsWith("/cari", StringComparison.Ordinal) => "[]",
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
        Services.BugunServisleri();   // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün (YetkiServisi)
    }

    private void OturumAc(string mod, string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", mod, mod));

    // TEMA TEMİZLİĞİ 17.10: Panel artık YALNIZ Yıldız Bugün'ü çizer (eski BASİT "sabahın beş sorusu"
    // kartları ve PRO kutucuk ızgarası KALDIRILDI). Aynı yükleyicilerin verisi Bugün'ün dev rakamında,
    // cümlesinde, "Seni bekleyen" listesinde ve alt şeridinde durur — güvenceler oraya taşındı.
    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit(string rol = "SAHIP")
    {
        OturumAc(ArayuzModuServisi.Basit, rol);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#yildiz-bugun");
        return e;
    }

    /// <summary>Panel'in yükleyicileri bitti, Bugün yükleniyor işaretinde değil (olumsuz iddialar BOŞ DOM'da yeşil vermesin).</summary>
    private static void Bekle(IRenderedComponent<Kuyumcu.Web.Pages.Panel> e, bool pozisyon = false) => e.BugunBekle(pozisyon);

    private List<string> Yollar() { lock (istekler) return [.. istekler.Select(i => i.Yol)]; }

    private static string Cumle(IRenderedComponent<Kuyumcu.Web.Pages.Panel> e) =>
        e.FindAll("#yildiz-bugun-cumle").FirstOrDefault()?.TextContent ?? "";

    private static List<string> CariSatirlari(IRenderedComponent<Kuyumcu.Web.Pages.Panel> e) =>
        [.. e.BekleyenKimlikleri().Where(k => k.StartsWith("yildiz-bekleyen-cari-", StringComparison.Ordinal))];

    private static Kuyumcu.Core.I18n.PanelSadeMetinleri S => Kuyumcu.Core.I18n.PanelSadeSozlugu.Tr;

    // ── İKİ YÜZ ─────────────────────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: S31_01_Basit_yuz_SADE_duzeni_cizer_izgara_YOK kaldırıldı — BASİT "beş soru" kartları, selam ve tarih satırı Yıldız'da BİLEREK yok (tuvalde yok); Bugün düzeni Yildiz4ATestleri.Y4A_10'da sınanıyor.

    [Fact]
    public void S31_02_Basit_yuzde_TEK_birincil_dugme_Yeni_satis()
    {
        var e = Basit();
        Bekle(e);

        // Yıldız'da birincil düğme = .yildiz-b1 ("Yeni satış" hapı); eski sınıflar da sayılır ki ikinci birincil sızmasın.
        var birincil = e.FindAll(".yildiz-b1, .dugme-birincil, .btn-birincil");
        Assert.Single(birincil);
        Assert.Equal("yildiz-bugun-yeni-satis", birincil[0].Id);
        Assert.Equal("/islemler", birincil[0].GetAttribute("href"));
        Assert.Equal("Yeni satış", birincil[0].TextContent.Trim());
    }

    // TEMA TEMİZLİĞİ 17.10: S31_03_Profesyonel_yuz_P37b_izgarasini_AYNEN_cizer kaldırıldı — PRO kutucuk ızgarası kaldırıldı; PRO'da da aynı Bugün düzeni Yildiz4ATestleri.Y4A_11'de sınanıyor.

    // ── YÜZE GÖRE YÜKLEME ───────────────────────────────────────────────────

    [Fact]
    public void S31_04_Her_yuz_YALNIZ_kendi_isteklerini_atar()
    {
        var e = Basit();
        Bekle(e);
        var basitYollar = Yollar();
        Assert.Contains(basitYollar, y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
        Assert.Contains(basitYollar, y => y.Contains("/bildirimler", StringComparison.Ordinal));
        // PRO'ya özgü yükleyiciler (grafik, son işlemler, kur kaynakları) BASİT'te açılmaz.
        Assert.DoesNotContain(basitYollar, y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));
        Assert.DoesNotContain(basitYollar, y => y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal));
        Assert.DoesNotContain(basitYollar, y => y.Contains("/kur/kaynaklar", StringComparison.Ordinal));
    }

    // TEMA TEMİZLİĞİ 17.10: S31_05_Profesyonel_yuz_basit_yuzun_isteklerini_ATMAZ kaldırıldı — YILDIZ-4A: BASİT ve PRO aynı Bugün yüzünü çizer, borçlu/bildirim yükleyicileri İKİ modda da BİLEREK açılır (Panel.YuzeGoreYukle); PRO'da Bugün listesi Yildiz4ATestleri.Y4A_11'de.

    [Fact]
    public void S31_06_Mod_calisirken_degisince_eksik_yuz_YUKLENIR()
    {
        var e = Basit();
        Bekle(e);
        Assert.DoesNotContain(Yollar(), y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));

        // BASİT → PRO: PRO'nun eksik yükleyicileri O AN (bir kez) açılır; Bugün gövdesi ve verisi yerinde kalır.
        Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.Contains(Yollar(), y => y.Contains("/raporlar/aylik", StringComparison.Ordinal)));
        Bekle(e);
        Assert.Contains(Yollar(), y => y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal));
        Assert.NotNull(e.Find("#yildiz-bekleyen-cari-c1"));
        Assert.Single(Yollar(), y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));   // sade yükleyiciler TEKRARLANMAZ
    }

    // ── (1) BUGÜN NE SATTIM? ────────────────────────────────────────────────

    [Fact]
    public void S31_07_Satis_karti_TUTAR_ADET_HAS_yazar_ve_kayitlara_baglanir()
    {
        var e = Basit();
        Bekle(e);

        var deger = e.Find("#yildiz-bugun-rakam");
        Assert.Equal(Bicim.TlKurus(57550m), deger.TextContent.Trim());
        Assert.Equal("/raporlar/islem-defteri", deger.GetAttribute("href"));   // UX-S23
        Assert.StartsWith(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleSatis(3, "tr"), Cumle(e), StringComparison.Ordinal);
        // Not: satılan has Bugün tuvalinde yazılmaz (bilerek); has dili Rapor merkezinde (RaporGorunum testleri).
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

        Assert.Equal("Bugün satış yok.", Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleSatis(0, "tr"));
        Assert.StartsWith("Bugün satış yok.", Cumle(e), StringComparison.Ordinal);
        Assert.DoesNotContain("işlem.", Cumle(e), StringComparison.Ordinal);
        Assert.Equal(Bicim.TlKurus(0m), e.Find("#yildiz-bugun-rakam").TextContent.Trim());   // başarılı boş yanıt: gerçek sıfır
    }

    [Fact]
    public void S31_09_Ozet_ucu_PATLARSA_sayi_cizilmez_bant_durur()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.InternalServerError, """{"hata":"rapor ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        // UX-S28: hata "0 ₺"a çevrilmez — ne satış ne kasa parçası yazılır, bant durur.
        Assert.NotNull(e.Find("#yildiz-bugun-satis-hata"));
        Assert.Empty(e.FindAll("#yildiz-bugun .yildiz-bugun-tam"));
        Assert.Empty(e.FindAll("a#yildiz-bugun-rakam"));
        Assert.DoesNotContain("Kasada", Cumle(e), StringComparison.Ordinal);
        Assert.DoesNotContain("Bugün satış yok", Cumle(e), StringComparison.Ordinal);
        Assert.DoesNotContain(S.YetkiYok, e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_10_Ozet_403_ise_YETKI_notu_hata_bandi_degil()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.Forbidden, """{"hata":"Bu işlem için yetkiniz yok"}""");
        var e = Basit("TEZGAHTAR");
        Bekle(e);

        Assert.Equal("Bu bilgiyi yalnız yönetici görür.", e.Find("#yildiz-bugun-rakam").TextContent.Trim());
        Assert.Empty(e.FindAll("#yildiz-bugun-satis-hata"));
        Assert.Empty(e.FindAll("#yildiz-bugun .yildiz-bugun-tam"));
        Assert.DoesNotContain("Kasada", Cumle(e), StringComparison.Ordinal);   // kasa tutarı da yazılmaz
    }

    // ── (2) KASADA NE VAR? — UX-S32 ─────────────────────────────────────────

    [Fact]
    public void S31_11_Kasa_karti_PARA_BIRIMLERINI_TOPLAMAZ()
    {
        var e = Basit();
        Bekle(e);

        // TL kasaları: 44.200 + 40.000 = 84.200 ₺ nakit — Dolar kasası (500 $) EKLENMEZ.
        var cumle = Cumle(e);
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleKasa(Bicim.Tl(84200m), true, "tr"), cumle, StringComparison.Ordinal);
        Assert.DoesNotContain("84.700", e.Markup, StringComparison.Ordinal);
        // Ham para kodu ekrana basılmaz (UX-S31).
        Assert.DoesNotContain("USD", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    // ── (4) KİM BORÇLU? ─────────────────────────────────────────────────────

    [Fact]
    public void S31_12_Kim_borclu_satirda_TEK_eylem_yon_dogru()
    {
        var e = Basit();
        Bekle(e);

        var c1 = e.Find("#yildiz-bekleyen-cari-c1");
        Assert.Single(c1.QuerySelectorAll("a, button"));
        var tahsil = e.Find("#yildiz-bekleyen-cari-c1-eylem");
        Assert.Equal("Tahsil et", tahsil.TextContent.Trim());
        Assert.Equal("/cariler?cariId=c1&islem=tahsilat", tahsil.GetAttribute("href"));   // sunum 14.10: Tahsil et → tahsilat penceresi

        var c3 = e.Find("#yildiz-bekleyen-cari-c3");
        Assert.Single(c3.QuerySelectorAll("a, button"));
        var ode = e.Find("#yildiz-bekleyen-cari-c3-eylem");
        Assert.Equal("Öde", ode.TextContent.Trim());
        Assert.Equal("/cariler?cariId=c3", ode.GetAttribute("href"));
        var tutar = e.Find("#yildiz-bekleyen-cari-c3-durum");
        Assert.Contains("yildiz-bugun-sat-durum--kotu", tutar.ClassName, StringComparison.Ordinal);
        Assert.Equal("−22,0000 has", tutar.TextContent.Trim());   // RAPOR-GORUNUM: tek has dili (TL bakiyesi yok → has)
        Assert.Equal($"· {Kuyumcu.Core.I18n.YildizBugunSozlugu.BizBorcluyuz("tr")}", c3.QuerySelector(".yildiz-bugun-sat-ek")!.TextContent);
        Assert.DoesNotContain("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-cari-c1-durum").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_13_Kim_borclu_VADE_durumunu_sade_yazar()
    {
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() =>
            Assert.Equal(Kuyumcu.Core.I18n.YildizBugunSozlugu.GunGecti(12, "tr"), e.Find("#yildiz-bekleyen-cari-c1-durum").TextContent.Trim()));
        // Gecikme günü üzerine gelince de okunur.
        Assert.Equal("12 gündür gecikmede", e.Find("#yildiz-bekleyen-cari-c1").GetAttribute("title"));
        Assert.Equal(Kuyumcu.Core.I18n.YildizBugunSozlugu.Gun(3, "tr"), e.Find("#yildiz-bekleyen-cari-c2-durum").TextContent.Trim());
    }

    [Fact]
    public void S31_14_Kim_borclu_SIRA_once_alacak_sonra_borc()
    {
        var e = Basit();
        Bekle(e);

        Assert.Equal(["yildiz-bekleyen-cari-c1", "yildiz-bekleyen-cari-c2", "yildiz-bekleyen-cari-c3"], CariSatirlari(e));
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
        Assert.Contains("500", e.Find("#yildiz-bugun-borclu-sinir").TextContent, StringComparison.Ordinal);
        // UX-S29: sınıra değdiyse eksik toplam cümleye YAZILMAZ.
        Assert.DoesNotContain("bize borçlu", Cumle(e), StringComparison.Ordinal);
        // Not: "812 açık hesap" sayısı Bugün tuvalinde yazılmaz (bilerek); sayı Müşteriler ekranında.
    }

    [Fact]
    public void S31_16_Kim_borclu_403_YETKI_notu_500_BANT_bos_liste_BOS_DURUM()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        var e = Basit("TEZGAHTAR");
        Bekle(e);
        Assert.Equal(S.YetkiYok, e.Find("#yildiz-bugun-borclu-yetki").TextContent.Trim());
        Assert.Empty(e.FindAll("#yildiz-bugun-borclu-hata"));
        Assert.Empty(CariSatirlari(e));
        Assert.DoesNotContain("bize borçlu", Cumle(e), StringComparison.Ordinal);
    }

    [Fact]
    public void S31_17_Kim_borclu_HATA_bos_duruma_cevrilmez()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.InternalServerError, """{"hata":"rapor ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        Assert.NotNull(e.Find("#yildiz-bugun-borclu-hata"));
        Assert.Empty(CariSatirlari(e));
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
        Assert.DoesNotContain("bize borçlu", Cumle(e), StringComparison.Ordinal);
    }

    [Fact]
    public void S31_18_Kim_borclu_GERCEKTEN_bossa_bos_durum()
    {
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);

        // Başarılı boş yanıt: müşteri satırı yok, bant yok; toplam GERÇEK sıfırdır ve öyle yazılır.
        Assert.Empty(CariSatirlari(e));
        Assert.Empty(e.FindAll("#yildiz-bugun-borclu-hata"));
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleCari(Bicim.Tl(0m), Bicim.Tl(0m), "tr"), Cumle(e), StringComparison.Ordinal);
        Assert.NotNull(e.Find("#yildiz-bekleyen-is-b1"));   // öteki işler yerinde
    }

    [Fact]
    public void S31_19_Vade_ucu_patlarsa_LISTE_KALIR_bant_cikar()
    {
        ozel["/raporlar/vade-takibi"] = (HttpStatusCode.InternalServerError, """{"hata":"vade ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-vade-hata")));
        Assert.NotNull(e.Find("#yildiz-bekleyen-cari-c1"));
        Assert.Equal("Açık hesap", e.Find("#yildiz-bekleyen-cari-c1-durum").TextContent.Trim());
    }

    // ── (5) BEKLEYEN İŞ ─────────────────────────────────────────────────────

    [Fact]
    public void S31_20_Bekleyen_is_bildirimleri_BAGLANTIYLA_ham_kodsuz_listeler()
    {
        var e = Basit();
        Bekle(e);

        Assert.Contains(Yollar(), y => y.Contains("/bildirimler?durum=ACIK&limit=5", StringComparison.Ordinal));
        var git = e.Find("#yildiz-bekleyen-is-b1-eylem");
        Assert.Equal("/vade-takibi", git.GetAttribute("href"));
        Assert.Contains("vadesi geçti", e.Find("#yildiz-bekleyen-is-b1").TextContent, StringComparison.Ordinal);
        // "//" ile başlayan rota uygulama dışıdır → bağlantı OLMAZ.
        Assert.Empty(e.FindAll("#yildiz-bekleyen-is-b2-eylem"));
        Assert.Empty(e.Find("#yildiz-bekleyen-is-b2").QuerySelectorAll("a"));
        // Ham önem/tür kodu ekrana basılmaz (UX-S31); önem sade adla okunur.
        var metin = e.Find("#yildiz-bugun-liste").OuterHtml;
        Assert.DoesNotContain("KRITIK", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("VADE_GECIKTI", metin, StringComparison.Ordinal);
        Assert.Equal("Acil", e.Find("#yildiz-bekleyen-is-b1").GetAttribute("title"));
    }

    [Fact]
    public void S31_21_Bekleyen_is_SINIRA_degdiyse_soyler()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            BildirimVarsayilan.Replace("\"dahaFazlasiVar\":false", "\"dahaFazlasiVar\":true", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        Assert.Contains("İlk 5 iş", e.Find("#yildiz-bugun-bekleyen-sinir").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_22_Bekleyen_is_GERCEKTEN_bossa_bos_durum_hatada_DEGIL()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""");
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);
        Assert.Contains("Bekleyen iş yok.", e.Find("#yildiz-bugun-bekleyen-bos").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#yildiz-bugun-liste"));
        Assert.Empty(e.FindAll("#yildiz-bugun .bant-hata"));
    }

    [Fact]
    public void S31_23_Bekleyen_is_HATASI_bos_duruma_cevrilmez()
    {
        ozel["/bildirimler"] = (HttpStatusCode.InternalServerError, """{"hata":"bildirim ucu cevap vermedi"}""");
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);

        Assert.NotNull(e.Find("#yildiz-bugun-bekleyen-hata"));
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
        Assert.DoesNotContain("Bekleyen iş yok.", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S31_24_Bekleyen_is_UYUM_sayaclarini_yetkili_role_gosterir()
    {
        ozel["/masak/ozet"] = (HttpStatusCode.OK, """{"suresiGecmis":2,"yaklasan":0}""");
        var e = Basit();
        Bekle(e);

        var satir = e.Find("#yildiz-bekleyen-masak-gecti");
        Assert.StartsWith("2 ", satir.QuerySelector(".yildiz-bugun-sat-metin")!.TextContent.Trim(), StringComparison.Ordinal);
        Assert.Equal("/uyum", e.Find("#yildiz-bekleyen-masak-gecti-eylem").GetAttribute("href"));
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

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar")));
        var kar = e.Find("#yildiz-bugun-kar");
        Assert.Equal($"Kâr {Bicim.Tl(4120m)}", kar.TextContent.Trim());
        Assert.Equal("/raporlar/kar-zarar", kar.GetAttribute("href"));        // UX-S23
        Assert.Contains(Yollar(), y => y.Contains($"/raporlar/kar-zarar?baslangic={Bugun}&bitis={Bugun}", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#yildiz-bugun-kar-hata"));
    }

    [Fact]
    public void TP1_02a_Kar_403_ise_KAR_YAZILMAZ_uydurma_sifir_yok_bant_yok()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        // Görsel denetim O-19 (21.10): TEZGAHTAR'da rapor uçlarına istek ATILMAZ (sunucu zaten 403). Kâr ucunun 403'ü artık
        // rapor rolü olan ama kâr yetkisi daraltılmış kullanıcıda (MUHASEBE) sınanır.
        var e = Basit("MUHASEBE");
        Bekle(e);

        Assert.NotNull(e.Find("#yildiz-bugun-rakam .yildiz-bugun-tam"));
        Assert.Empty(e.FindAll("#yildiz-bugun-kar"));
        Assert.Empty(e.FindAll("#yildiz-bugun-kar-hata"));
        Assert.DoesNotContain("Kâr", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_02b_Kar_ucu_PATLARSA_bant_cikar_satis_kalir()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.InternalServerError, """{"hata":"kâr ucu cevap vermedi"}""");
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar-hata")));
        Assert.Empty(e.FindAll("#yildiz-bugun-kar"));
        Assert.Equal(Bicim.TlKurus(57550m), e.Find("#yildiz-bugun-rakam").TextContent.Trim());
    }

    [Fact]
    public void TP1_03_Maliyeti_bilinmeyen_varsa_RAKAM_basilmaz_eksik_der()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.OK,
            KarVarsayilan.Replace("\"karTl\":4120.00", "\"karTl\":-350.00", StringComparison.Ordinal)
                         .Replace("\"maliyetiBilinmeyenAdet\":0", "\"maliyetiBilinmeyenAdet\":2", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar")));
        var kar = e.Find("#yildiz-bugun-kar");
        // Maliyeti bilinmeyen satış varken rakam BASILMAZ (yanıltıcı olurdu); eksik olduğu söylenir (fail-closed).
        Assert.Equal($"Kâr {Kuyumcu.Core.I18n.YildizBugunSozlugu.MaliyetEksik("tr")}", kar.TextContent.Trim());
        Assert.DoesNotContain(Bicim.Tl(-350m), e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.Equal("/raporlar/kar-zarar", kar.GetAttribute("href"));
    }

    [Fact]
    public void TP1_03b_Zarar_KIRMIZI_yazilir_maliyet_tamsa()
    {
        ozel["/raporlar/kar-zarar"] = (HttpStatusCode.OK,
            KarVarsayilan.Replace("\"karTl\":4120.00", "\"karTl\":-350.00", StringComparison.Ordinal));
        var e = Basit();
        Bekle(e);

        // Maliyet tamsa zarar eksi işaretiyle YAZILIR (gizlenmez, sıfırlanmaz). Not: Bugün alt şeridinde renk ayrımı yok (tuval).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar")));
        Assert.Equal($"Kâr {Bicim.Tl(-350m)}", e.Find("#yildiz-bugun-kar").TextContent.Trim());
        Assert.Contains("−", e.Find("#yildiz-bugun-kar b").TextContent, StringComparison.Ordinal);
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

        // Yıldız cümlesi "Kasada X nakit" — yalnız kasaların TL nakdi (84.200 ₺). Banka parası, pasif hesap ve
        // döviz "nakit" diye kasaya EKLENMEZ (124.200 / 125.199 yazılmaz).
        Assert.Contains(Yollar(), y => y.EndsWith("/bankalar", StringComparison.Ordinal));
        var cumle = Cumle(e);
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleKasa(Bicim.Tl(84200m), true, "tr"), cumle, StringComparison.Ordinal);
        Assert.DoesNotContain("124.200", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("125.199", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_05_Banka_403_banka_bacagi_YAZILMAZ_500_ise_BANT()
    {
        // Bugün'de banka bacağı YOK (tuval); banka ucunun 403/500'ü kasa cümlesini BOZMAZ, uydurma toplam üretmez.
        ozel["/bankalar"] = (HttpStatusCode.Forbidden, """{"hata":"yetki yok"}""");
        var e = Basit();
        Bekle(e);
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleKasa(Bicim.Tl(84200m), true, "tr"), Cumle(e), StringComparison.Ordinal);

        ozel["/bankalar"] = (HttpStatusCode.InternalServerError, """{"hata":"banka ucu cevap vermedi"}""");
        var e2 = Render<Kuyumcu.Web.Pages.Panel>();
        Bekle(e2);
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleKasa(Bicim.Tl(84200m), true, "tr"), Cumle(e2), StringComparison.Ordinal);
        Assert.DoesNotContain("banka ucu cevap vermedi", e2.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
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

        Assert.Equal(["yildiz-bekleyen-cari-a1", "yildiz-bekleyen-cari-a2", "yildiz-bekleyen-cari-z2"], CariSatirlari(e));
        Assert.Equal("Tahsil et", e.Find("#yildiz-bekleyen-cari-a2-eylem").TextContent.Trim());
        Assert.Equal("Öde", e.Find("#yildiz-bekleyen-cari-z2-eylem").TextContent.Trim());
        // Gösterilmeyen satırlar toplamdan SİLİNMEZ: cümle 7 hesabın TL toplamını yazar (alacak 15.000, borç 1.600).
        Assert.Contains(Kuyumcu.Core.I18n.YildizBugunSozlugu.CumleCari(Bicim.Tl(15000m), Bicim.Tl(1600m), "tr"), Cumle(e), StringComparison.Ordinal);
    }

    [Fact]
    public void TP1_07_Bekleyen_is_satirinda_KAYNAK_etiketi_ham_kod_basilmaz()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            BildirimVarsayilan.Replace("\"tur\":\"VADE_GECIKTI\"", "\"tur\":\"VADE_HATIRLATMA\"", StringComparison.Ordinal));
        // Müşteri ayrıntısı (vade raporu) yoksa satırın durumu kaynak etiketidir.
        ozel["/raporlar/vade-takibi"] = (HttpStatusCode.OK,
            """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""");
        var e = Basit();
        Bekle(e);

        Assert.Equal("Vade", e.Find("#yildiz-bekleyen-is-b1-durum").TextContent.Trim());
        Assert.Equal("Bildirim", e.Find("#yildiz-bekleyen-is-b2-durum").TextContent.Trim());   // bilinmeyen kod uydurulmaz
        var metin = e.Find("#yildiz-bugun-liste").OuterHtml;
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

        var satir = e.Find("#yildiz-bekleyen-masak-gecti");
        Assert.Equal("Acil", satir.GetAttribute("title"));   // önem sade adla (ham "KRITIK" yok)
        Assert.Contains("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-masak-gecti-durum").ClassName, StringComparison.Ordinal);
        Assert.Equal("Uyum", e.Find("#yildiz-bekleyen-masak-gecti-durum").TextContent.Trim());
        Assert.Equal("/uyum", e.Find("#yildiz-bekleyen-masak-gecti-eylem").GetAttribute("href"));
        Assert.StartsWith("2 ", satir.QuerySelector(".yildiz-bugun-sat-metin")!.TextContent.Trim(), StringComparison.Ordinal);
        var bekleyen = e.Find("#yildiz-bugun-liste").TextContent;
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
        Bekle(e, pozisyon: true);

        // Bugün alt şeridi pozisyonu tuval biçimiyle (bir ondalık, işaretli) ve ayrıntıya (Rapor merkezi) bağlantıyla yazar.
        // Kartın "Ayrıntı" açılımı Bugün'de YOK (tuval); kartın kendisi AltinPozisyonuKartiTestleri'nde sınanıyor.
        var poz = e.Find("#yildiz-bugun-pozisyon");
        Assert.Equal($"Altın pozisyonu {YildizBugunHas(312.8m)}", poz.TextContent.Trim());
        Assert.Equal("/raporlar", poz.GetAttribute("href"));
        Assert.Equal("Vitrin 298,4 g", e.Find("#yildiz-bugun-vitrin").TextContent.Trim());
        Assert.Empty(e.FindAll("#pozisyon-kart"));
    }

    private static string YildizBugunHas(decimal g) => Kuyumcu.Web.Bilesenler.Yildiz.YildizBugunBicim.HasKisa(g, "g has", true);

    // TEMA TEMİZLİĞİ 17.10: TP1_10_Hava_basit_yuzde_tuval_bicimi_derece_kisa_durum_titleda kaldırıldı — hava/selam satırı Yıldız Bugün'de BİLEREK yok (tuvalde yok; YildizBugun başlık notu).

    [Fact]
    public void TP1_11_Yeni_satis_ARTI_isareti_CSS_ile_ikon_sikkesi_yok()
    {
        var e = Basit();
        var dugme = e.Find("#yildiz-bugun-yeni-satis");
        Assert.Empty(dugme.QuerySelectorAll("svg, img"));
        Assert.Equal("Yeni satış", dugme.TextContent.Trim());
        // Not: eski .panel-yeni-satis::before artı işareti eski yüzle kalktı; Yıldız hapı tuval CSS'i Yildiz4ATestleri'nde.
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
        // Tam sayfa yenileme: profil yokken çizilir; profil BASIT gelince Mod.Degisti tetiklenmeden yeniden
        // çizim olur. Bugün'ün verisi (borçlu, banka) yüklenmiş olur, liste yükleniyor işaretinde KALMAZ.
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#yildiz-bugun");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.Render();
        Bekle(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bekleyen-cari-c1")));
        Assert.Contains(Yollar(), y => y.Contains("/raporlar/cari-yaslandirma", StringComparison.Ordinal));
        Assert.Contains(Yollar(), y => y.EndsWith("/bankalar", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
    }

    /// <summary>
    /// PAKET YASAL-2 tur 3 (orkestratör 30.09 22:20 madde 3): nakit sınırı aşımı Panel'de TEK
    /// kaynaktan — GET /tevsik/ihlaller (NakitSiniriPanelSatiri, Yıldız'da görünmez yükler). Aynı olayın
    /// bildirimi (ilgiliVarlikTuru=TevsikIhlali) "Seni bekleyen" listesinde İKİNCİ KEZ sayılmaz; diğer
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
        e.WaitForAssertion(() => Assert.Contains(string.Format(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.IsNakitSiniri, 1),
            e.Find("#yildiz-bekleyen-yasal-nakit").TextContent, StringComparison.Ordinal));   // tek kaynak: 1 aşım
        Assert.Single(e.FindAll("#yildiz-bugun-liste > li[id='yildiz-bekleyen-yasal-nakit']"));
        Assert.Empty(e.FindAll("#yildiz-bekleyen-is-tv1"));                                                     // bekleyen işte tekrar YOK
        Assert.DoesNotContain("Nakit sınırı aşıldı", e.Find("#yildiz-bugun-liste").TextContent, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#yildiz-bekleyen-is-b1"));                                                      // diğer bildirim yerinde
    }

    /// <summary>YASAL-2 tur 3 (PRO): "Seni bekleyen" şeritle AYNI kaynaktan — aşım varken "iş yok" demez.</summary>
    [Fact]
    public void YASAL2_Tur3_PRO_bekleyen_kutucugu_serit_sayisini_okur_temiz_demez()
    {
        ozel["/tevsik/ihlaller"] = (HttpStatusCode.OK, """
            {"ihlaller":[{"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"TAHSILAT","cariId":"c9","cariAd":"X","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK","kalanIsGunu":3,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false}],
             "kapanmamisSayisi":1,"sinir":200,"dahaVar":false,"bayramEksik":false}
            """);
        // Öteki iş kaynakları boş: liste YALNIZ şeritteki aşımdan dolabilir.
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""");
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        Bekle(e);
        e.WaitForAssertion(() =>
        {
            Assert.Contains(string.Format(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.IsNakitSiniri, 1), e.Find("#yildiz-bekleyen-yasal-nakit").TextContent, StringComparison.Ordinal);
            Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
        });
        Assert.DoesNotContain("Bekleyen iş yok", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    /// <summary>YASAL-2 tur 3: tek bildirim nakit aşımıysa "Bekleyen iş yok" DENMEZ — iş kendi satırında, ilgili ekrana bağlantıyla.</summary>
    [Fact]
    public void YASAL2_Tur3_yalniz_nakit_asimi_varken_bekleyen_is_YOK_demez_kutuya_yonlendirir()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK, """
            {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[
              {"id":"tv1","tur":"TEVSIK_KURTARMA","onem":"KRITIK","baslik":"Nakit sınırı aşıldı","govde":"x","hedefRota":"/uyum?sekme=nakit",
               "ilgiliVarlikTuru":"TevsikIhlali","ilgiliVarlikId":"i1","olusturuldu":"2026-09-23T07:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
            """);
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
        var e = Basit();
        Bekle(e);
        var metin = e.Find("#yildiz-bugun").TextContent;
        Assert.DoesNotContain("Bekleyen iş yok", metin, StringComparison.Ordinal);
        Assert.Equal("/uyum?sekme=nakit", e.Find("#yildiz-bekleyen-yasal-nakit-eylem").GetAttribute("href"));
        Assert.DoesNotContain("üstteki", metin, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
        Assert.Empty(e.FindAll("#yildiz-bekleyen-is-tv1"));
    }

    // ── PAKET YASAL-4 — Y4-A (K-1) · Y4-B (K-8) ──────────────────────────────

    [Fact]
    public void Y4A_K1_iptal_edilmemis_eski_pusula_varken_Panelde_pusula_satiri_YOK()
    {
        // Sunucu: 10 gün önce düzenlenmiş, iptal edilmemiş pusula → takip kümesi BOŞ.
        ozel["/gider-pusulalari/sure-takibi"] = (HttpStatusCode.OK,
            """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,"gecikmis":[],"yaklasan":[],"uyarilar":[]}""");
        // Uyum satırları ÇİZİLSİN diye başka bir sayaç dolu (MASAK) — pusula satırı yine YOK olmalı.
        ozel["/masak/ozet"] = (HttpStatusCode.OK, """{"suresiGecmis":1,"yaklasan":0}""");
        var e = Basit();
        Bekle(e);
        Assert.NotNull(e.Find("#yildiz-bekleyen-masak-gecti"));
        Assert.Empty(e.FindAll("#yildiz-bekleyen-pusula"));
        Assert.Empty(e.FindAll("#yildiz-bekleyen-pusula-yeniden"));
        Assert.DoesNotContain("gider pusulasının 7 günlük düzenleme süresi", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Y4A_iptal_edilmis_yenisiz_pusula_Panelde_son_gunuyle_UYARI_satiri()
    {
        ozel["/gider-pusulalari/sure-takibi"] = (HttpStatusCode.OK,
            """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":1,"gecikmis":[],"yaklasan":[{"id":"p1","pusulaNo":"GP-2026-000001","hareketId":"h1","teslimTarihi":"2026-09-26T10:00:00Z","sonGun":"2026-10-03","kalanGun":2,"brutTutar":1000,"stopajTutar":0}],"enYakinSonGun":"2026-10-03","uyarilar":[]}""");
        var e = Basit();
        Bekle(e);
        var satir = e.Find("#yildiz-bekleyen-pusula-yeniden");
        Assert.Contains("03.10.2026", satir.TextContent, StringComparison.Ordinal);
        Assert.Equal("/gider-pusulalari", e.Find("#yildiz-bekleyen-pusula-yeniden-eylem").GetAttribute("href"));
        Assert.DoesNotContain("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-pusula-yeniden-durum").ClassName, StringComparison.Ordinal);   // UYARI, kritik değil
        Assert.Empty(e.FindAll("#yildiz-bekleyen-pusula"));
    }

    [Fact]
    public void Y4B_K8_kalan_var_son_gun_gecti_KRITIK_satir_kalan_yoksa_satir_YOK()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK,
            """{"buAy":"2026-10","buAyKesilenTl":240.00,"gecikmis":[{"donem":"2026-08","kesilenTl":1340.00,"odenenTl":0,"kalanTl":1340.00,"sonGun":"2026-09-26","sonGunTeyit":"TEYITSIZ","sonGunGecti":true,"pusulaSayisi":4,"odemeler":[]}],"yaklasan":[],"uyariGunu":5}""");
        var e = Basit();
        Bekle(e);
        var satir = e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08");
        Assert.Contains("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08-durum").ClassName, StringComparison.Ordinal);
        Assert.Contains("26.09.2026", satir.TextContent, StringComparison.Ordinal);
        // § 4.5 tek dokunuş: satırın tek eylemi o dönemin ödeme çekmecesini açar (Panel POST etmez).
        Assert.Equal("/gider-pusulalari?sekme=stopaj&ode=2026-08", e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08-eylem").GetAttribute("href"));
        Assert.Single(satir.QuerySelectorAll("a, button"));
        // Not: eski "Kasada ne var?" kartının "kesilen stopaj vergi dairesine ait" alt satırı Bugün tuvalinde yok (rapor: ÜRÜN BULGUSU değil, bilgi notu).
    }

    [Fact]
    public void Y4B_K8_kalan_sifirsa_stopaj_satiri_YOK()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK,
            """{"buAy":"2026-10","buAyKesilenTl":0,"gecikmis":[],"yaklasan":[],"uyariGunu":5}""");
        var e = Basit();
        Bekle(e);
        Assert.Contains(Yollar(), y => y.Contains("/stopaj/ozet", StringComparison.Ordinal));
        Assert.NotNull(e.Find("#yildiz-bugun-liste"));   // liste çizildi (boş DOM'da olumsuz iddia değil)
        Assert.Empty(e.FindAll("[id^='yildiz-bekleyen-stopaj-']"));
        Assert.Empty(e.FindAll("#yildiz-bugun-stopaj-hata"));
    }

    [Fact]
    public void Y4B_stopaj_ozeti_okunamazsa_hata_bandi_sayilar_uydurulmaz()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.InternalServerError, """{"message":"Sunucu hatası"}""");
        var e = Basit();
        Bekle(e);
        Assert.Empty(e.FindAll("[id^='yildiz-bekleyen-stopaj-']"));
        Assert.NotEmpty(e.FindAll("#yildiz-bugun-stopaj-hata"));
    }

    // ── PAKET YASAL-4 tur 4 (orkestratör 01.10 12:35): tam sayfa yükleme / girişten sonraki ilk Panel ──
    // F5'te ve doğrudan "/" adresinde profil (rol) İLK çizimden SONRA gelir. Panel rolü null
    // görüp uyum/stopaj yüklemesini hiç açmıyordu; stopaj KRİTİK satırı görünmüyor, yerine
    // "Bekleyen iş yok" yazıyordu.

    private const string StopajGecikmisOzet =
        """{"buAy":"2026-10","buAyKesilenTl":240.00,"gecikmis":[{"donem":"2026-08","kesilenTl":1340.00,"odenenTl":0,"kalanTl":1340.00,"sonGun":"2026-09-26","sonGunTeyit":"TEYITSIZ","sonGunGecti":true,"pusulaSayisi":4,"odemeler":[]}],"yaklasan":[],"uyariGunu":5}""";

    private int StopajOzetIstegi() => Yollar().Count(y => y.Contains("/stopaj/ozet", StringComparison.Ordinal));

    /// <summary>Öteki iş kaynakları boş — "Seni bekleyen" yalnız uyum/stopaj yükleyicisinin sonucuyla dolar.</summary>
    private void YalnizUyumIsleri()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""");
        ozel["/raporlar/cari-yaslandirma"] = (HttpStatusCode.OK,
            """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""");
    }

    [Fact]
    public void Y4_Tur4_oturum_basta_null_sonra_SAHIP_BASIT_stopaj_KRITIK_satiri_gelir_is_yok_DENMEZ()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK, StopajGecikmisOzet);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.YukleyicilerBitsin();
        Assert.Equal(0, StopajOzetIstegi());                    // rol yokken istek YOK
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.Render();
        Bekle(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08")));
        Assert.Contains("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08-durum").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.PanelSadeSozlugu.Tr.BekleyenYok, e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Yasal3Sozlugu.Tr.PanelSeritte, e.Markup, StringComparison.Ordinal);
        Assert.Equal(1, StopajOzetIstegi());                    // BİR KEZ yüklenir
        // Oturum sonradan yine değişirse (jeton yenileme, mod) yükleme TEKRARLANMAZ, yükleniyora dönmez.
        var o = Services.GetRequiredService<OturumServisi>();
        o.KullaniciAta(o.Kullanici);
        Bekle(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08")));
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
        Assert.Equal(1, StopajOzetIstegi());
    }

    [Fact]
    public async Task Y4_Tur4_oturum_var_profil_GELMEDEN_iskelet_kalir_MUHASEBE_gelince_PRO_stopaj_satiri()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK, StopajGecikmisOzet);
        YalnizUyumIsleri();
        var oturum = Services.GetRequiredService<OturumServisi>();
        await oturum.TokenKaydetAsync("erisim", "yenileme");    // F5: jeton depoda, profil (/auth/ben) henüz yok
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.YukleyicilerBitsin();
        // Öteki yükleyiciler BİTTİ ama profil yok: liste "yükleniyor"da kalır — "iş yok" DENMEZ.
        Assert.NotNull(e.Find("#yildiz-bugun-bekleyen-yukleniyor"));
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
        Assert.DoesNotContain(Kuyumcu.Core.I18n.PanelSadeSozlugu.Tr.BekleyenYok, e.Markup, StringComparison.Ordinal);
        Assert.Equal(0, StopajOzetIstegi());
        oturum.KullaniciAta(new OturumKullanici("u2", "Mu", "Hasebe", "m@ornek.com", "MUHASEBE", "s1",
            ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08")));
        Bekle(e);
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
        Assert.Equal("/gider-pusulalari?sekme=stopaj&ode=2026-08",
            e.Find("#yildiz-bekleyen-stopaj-gecti-2026-08-eylem").GetAttribute("href"));
        Assert.Equal(1, StopajOzetIstegi());
    }

    [Fact]
    public async Task Y4_Tur4_profil_TEZGAHTAR_gelirse_iskelet_kalkar_stopaj_istegi_YOK()
    {
        ozel["/stopaj/ozet"] = (HttpStatusCode.OK, StopajGecikmisOzet);
        YalnizUyumIsleri();
        var oturum = Services.GetRequiredService<OturumServisi>();
        await oturum.TokenKaydetAsync("erisim", "yenileme");
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.YukleyicilerBitsin();
        Assert.NotNull(e.Find("#yildiz-bugun-bekleyen-yukleniyor"));
        oturum.KullaniciAta(new OturumKullanici("u3", "Tez", "Gahtar", "t@ornek.com", "TEZGAHTAR", "s1",
            ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        // Yetkisiz rol: yükleniyor kalkar, gerçekten iş yok → boş durum; uyum/stopaj isteği ATILMAZ.
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-bekleyen-bos")));
        Bekle(e);
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
        Assert.Empty(e.FindAll("[id^='yildiz-bekleyen-stopaj-']"));
        Assert.Equal(0, StopajOzetIstegi());
    }
}
