using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4A — "Yıldız" BİREBİR: Bugün (tuval Bugun4/Bugun4a) + üst menü/kabuk
//
// Kurucu (09.10): "birebir tasarım neyse aynısını istisnasız olarak istiyorum".
// Görsel eşitliğin ASIL kanıtı gerçek Chrome'daki tuval-kiyas.mjs ölçümüdür (rapor);
// bu sınıf o ölçümün dayandığı SÖZLEŞMEYİ kilitler:
//   • saf: üst satır, şube adı, cümle, kur ve has biçimi (tuval yazımı);
//   • bUnit: Yıldız'da Panel = tuval düzeni (BASİT ve PRO), başka şemada özgün gövde,
//     "Seni bekleyen" satırları, UX-S28 hata, düğme/bağlantı hedefleri, üst menü kur
//     şeridi ve kullanıcı menüsüne taşınan Basit/Profesyonel anahtarı;
//   • CSS: tuval değerleri (ağırlık 200 dahil), kapsam, sabit renk yok; opsz yazı tipi.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz4ATestleri : BunitContext
{
    private string sema = "yildiz";
    private bool gunlukHata;
    private string yaslandirma = Yaslandirma;
    private readonly ConcurrentQueue<string> istekler = new();

    private const string Gunluk = """
        {"tarih":"2026-10-08","satis":{"adet":11,"toplamTl":113011.33,"toplamHas":20.4},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0,"hareketSayisi":11,
         "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":250356.54,"paraBirimi":"TRY"}]}
        """;

    private const string Yaslandirma = """
        {"yaslananCariler":[
          {"cariId":"c1","cariAd":"Tuna Akarsu","cariTur":"MUSTERI","tlBakiye":22174.73,"hasGramBakiye":0,"gunSayisi":15,"dilim":"0-30"},
          {"cariId":"c2","cariAd":"Kemal Ardıç","cariTur":"MUSTERI","tlBakiye":10480.06,"hasGramBakiye":0,"gunSayisi":30,"dilim":"0-30"},
          {"cariId":"c3","cariAd":"Anadolu Döküm","cariTur":"TEDARIKCI","tlBakiye":-3840,"hasGramBakiye":0,"gunSayisi":null,"dilim":null}],
         "ozetTl":{"0-30":0,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},"toplamSatir":3,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    // c1: vadeye 15 gün var (tuval "15 gün"); c2: vadesi 30 gün GEÇMİŞ ("30 gün geçti").
    private static string Vade => VadeKalip.Replace("VADE15", DateOnly.FromDateTime(DateTime.Today).AddDays(15).ToString("yyyy-MM-dd"));
    private const string VadeKalip = """
        {"ufukGun":7,"gecikenToplam":32654.79,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":32654.79,
         "cariler":[
          {"cariId":"c1","cariAd":"Tuna Akarsu","cariTur":"MUSTERI","telefon":null,"gecikenTutar":0,"yaklasanTutar":22174.73,"vadesizTutar":0,"enEskiGecikmeGun":null,"enYakinVade":"VADE15","borclar":[]},
          {"cariId":"c2","cariAd":"Kemal Ardıç","cariTur":"MUSTERI","telefon":null,"gecikenTutar":10480.06,"yaklasanTutar":0,"vadesizTutar":0,"enEskiGecikmeGun":30,"enYakinVade":null,"borclar":[]}]}
        """;

    private const string Pozisyon = """
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":157.26,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                  "netGram":157.26,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-10-08T09:00:00Z","donemdenBagimsiz":true}
        """;

    private const string KarEksik = """
        {"hasilatTl":113011.33,"maliyetTl":0,"karTl":null,"karMarjiYuzde":null,"satisAdedi":11,
         "maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":11,"maliyetiEksikSatisAdedi":11,"maliyetiEksikIadeAdedi":0,
         "karDurumu":"HESAPLANAMADI","guvenilirlik":"YOK","uyari":null}
        """;

    private sealed class SahteApi(Yildiz4ATestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            t.istekler.Enqueue($"{istek.Method} {yol}");
            if (t.gunlukHata && yol.EndsWith("/raporlar/gunluk"))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"Rapor alınamadı (sahte)"}""", System.Text.Encoding.UTF8, "application/json"),
                };
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk") => Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => t.yaslandirma,
                var y when y.EndsWith("/raporlar/vade-takibi") => Vade,
                var y when y.EndsWith("/raporlar/aylik") => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri") => """{"baslangic":"2026-10-08","bitis":"2026-10-08","toplamSatir":0,"ofset":0,"limit":5,"dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},"storno":{"adet":0,"tutarTl":0},"satirlar":[]}""",
                var y when y.EndsWith("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar") => KarEksik,
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":250356.54,"nakit":230356.54,"kart":20000,"havale":0}]""",
                var y when y.EndsWith("/pozisyon") => Pozisyon,
                var y when y.EndsWith("/kur/kaynaklar") => """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","tenantId":"t1","altinHasFiyati":6655.6,"usdTry":41.95,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-08T14:40:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22","milyem":916,"gramFiyati":6097.2,"hasFiyati":6655.6}]}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","kur"]}""",
                var y when y.Contains("/uyum/") => """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.EndsWith("/kullanicilar/ben") && istek.Method == HttpMethod.Patch =>
                    """{"id":"u1","arayuzModu":"BASIT"}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube","aktif":true}]""",
                // YILDIZ-4C: Bugün'ün iki yeni satırının uçları — gerçekçi yanıt ("{}" varsayılanı liste ayrıştıramaz → sahte hata bandı olurdu).
                var y when y.EndsWith("/raporlar/kasa-defteri") => """{"baslangic":"2026-10-07","bitis":"2026-10-07","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":1,"kasalar":[{"kasaId":"k1","kasaAd":"Ana Kasa","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":0,"guncelBakiye":0,"gunler":[]}]}""",
                var y when y.EndsWith("/cari") => "[]",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public Yildiz4ATestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private void Oturum(string mod, string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", rol, "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Panel(string mod = ArayuzModuServisi.Basit)
    {
        Oturum(mod);
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<YetkiServisi>().HazirlaAsync().GetAwaiter().GetResult();
        // Şube listesini kabukta SubeSecici okur (üst menü); burada aynı adım elle yapılır.
        var oturum = Services.GetRequiredService<OturumServisi>();
        KabukServisi.Ortak.GorunumHazirla(Services.GetRequiredService<ApiIstemci>(), oturum).GetAwaiter().GetResult();
        KabukServisi.Ortak.SubeListesiAta(oturum, [new SubeOzeti("s1", "Merkez Şube", true)]);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        // Tema temizliği (17.10): Panel'in tek yüzü Bugün'dür; sunucu eski şema dese de aynı yüz çizilir.
        e.WaitForElement("#yildiz-bugun", TimeSpan.FromSeconds(10));
        {
            e.WaitForAssertion(() =>
            {
                Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
                Assert.NotEmpty(e.FindAll("#yildiz-bugun-pozisyon"));
                Assert.NotEmpty(e.FindAll("#yildiz-bugun-cumle"));
                Assert.Contains("Müşteriler bize borçlu", e.Find("#yildiz-bugun-cumle").TextContent);
                Assert.Contains("nakit", e.Find("#yildiz-bugun-cumle").TextContent);
            }, TimeSpan.FromSeconds(10));
        }
        return e;
    }

    private static string Css() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");

    /// <summary>tema-yildiz.css'in YILDIZ-4A bölümü (yorumsuz).</summary>
    private static string Blok4A()
    {
        var c = Css();
        var i = c.IndexOf("PAKET YILDIZ-4A — BUGÜN", StringComparison.Ordinal);
        Assert.True(i > 0, "tema-yildiz.css'te YILDIZ-4A bölümü yok");
        // YILDIZ-4B: bölüm 4B başlığında biter (4B'nin kendi sınaması Yildiz4BTestleri'nde).
        var son = c.IndexOf("PAKET YILDIZ-4B — TEZGÂH", i, StringComparison.Ordinal);
        var bas = c.LastIndexOf("/*", i, StringComparison.Ordinal);
        var parca = son > 0 ? c[bas..c.LastIndexOf("/*", son, StringComparison.Ordinal)] : c[bas..];
        return Regex.Replace(parca, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string Kural(string blok, string secici)
    {
        var i = blok.IndexOf(secici, StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = blok.IndexOf('{', i);
        return blok[(a + 1)..blok.IndexOf('}', a)];
    }

    // ── SAF ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4A_01_ust_satir_gun_tarih_sube_tuval_yazimi()
    {
        Assert.Equal("Perşembe · 8 Ekim 2026 · Merkez Şube", YildizBugunBicim.UstSatir(new DateTime(2026, 10, 8), "Merkez Şube", "tr"));
        Assert.Equal("Thursday · 8 October 2026 · Main", YildizBugunBicim.UstSatir(new DateTime(2026, 10, 8), "Main", "en"));
        // şube bilinmiyorsa parça YAZILMAZ (tahmin yok)
        Assert.Equal("Perşembe · 8 Ekim 2026", YildizBugunBicim.UstSatir(new DateTime(2026, 10, 8), null, "tr"));
    }

    [Fact]
    public void Y4A_02_sube_adi_SubeSecici_kurali_tahmin_yok()
    {
        SubeOzeti A(string id, string ad) => new(id, ad, true);
        Assert.Null(YildizBugunBicim.SubeAdi([], null, true, "s1", "Tüm şubeler"));
        Assert.Equal("Merkez", YildizBugunBicim.SubeAdi([A("s1", "Merkez")], null, true, null, "Tüm şubeler"));
        Assert.Equal("Tüm şubeler", YildizBugunBicim.SubeAdi([A("s1", "Merkez"), A("s2", "Çarşı")], null, true, "s1", "Tüm şubeler"));
        Assert.Equal("Çarşı", YildizBugunBicim.SubeAdi([A("s1", "Merkez"), A("s2", "Çarşı")], "s2", true, "s1", "Tüm şubeler"));
        Assert.Equal("Merkez", YildizBugunBicim.SubeAdi([A("s1", "Merkez"), A("s2", "Çarşı")], null, false, "s1", "Tüm şubeler"));
        Assert.Null(YildizBugunBicim.SubeAdi([A("s1", "Merkez"), A("s2", "Çarşı")], "s9", true, "s1", "Tüm şubeler"));
    }

    [Fact]
    public void Y4A_03_cumle_tuvaldeki_gibi_bilinmeyen_parca_yazilmaz()
    {
        var tam = new YildizBugunVerisi { SatisAdet = 11, KasaTutar = Bicim.Tl(230356.54m), KasaNakit = true, AlacakTl = 32654.79m, BorcTl = 3840m };
        Assert.Equal($"Bugünkü satış · 11 işlem. Kasada {Bicim.Tl(230356.54m)} nakit. Müşteriler bize borçlu: {Bicim.Tl(32654.79m)}. Biz müşterilere borçluyuz: {Bicim.Tl(3840m)}." /* P-DIL-BORC-KASA: tek kalıp */,
            YildizBugunBicim.Cumle(tam, "tr"));
        Assert.Equal("3.840 ₺", Bicim.Tl(3840m));   // tuval "borcun 3.840 ₺" — kuruşsuz tam sayı
        // nakit ayrımı okunamadıysa "nakit" DENMEZ
        Assert.Equal($"Bugünkü satış · 11 işlem. Kasada {Bicim.Tl(1m)}.", YildizBugunBicim.Cumle(new YildizBugunVerisi { SatisAdet = 11, KasaTutar = Bicim.Tl(1m) }, "tr"));
        // satış hatası: satış parçası yok (0 satış diye yazılmaz); müşteri toplamı yoksa o parça yok
        Assert.Equal("", YildizBugunBicim.Cumle(new YildizBugunVerisi { SatisHata = "x", AlacakTl = 1m }, "tr"));
        Assert.Equal("Bugün satış yok.", YildizBugunBicim.Cumle(new YildizBugunVerisi(), "tr"));
        Assert.Equal("Today's sales · 1 sale." /* P-DIL-BORC-KASA (Y8) */, YildizBugunBicim.Cumle(new YildizBugunVerisi { SatisAdet = 1 }, "en"));
    }

    [Theory]
    [InlineData(6655.6, "6.656")]
    [InlineData(6097.2, "6.097")]
    [InlineData(12345.5, "12.346")]
    public void Y4A_04_kur_seridi_tam_lira_TL_isaretsiz(double n, string beklenen)
    {
        Assert.Equal(beklenen, YildizBugunBicim.KurTam((decimal)n));
        Assert.DoesNotContain("₺", YildizBugunBicim.KurTam((decimal)n));
    }

    [Fact]
    public void Y4A_05_alt_serit_has_bir_ondalik_isaretli_tuval()
    {
        Assert.Equal("+157,3 g has", YildizBugunBicim.HasKisa(157.26m, "g has", true));
        Assert.Equal("−2,0 g has", YildizBugunBicim.HasKisa(-2.04m, "g has", true));
        Assert.Equal("0,0 g has", YildizBugunBicim.HasKisa(0.04m, "g has", true));   // işaret yuvarlanmış değerden
        Assert.Equal("157,3 g", YildizBugunBicim.HasKisa(157.26m, "g", false));
        Assert.Equal("1.234,6 g", YildizBugunBicim.HasKisa(1234.56m, "g", false));
    }

    [Fact]
    public void Y4A_06_cari_toplamlari_pozitif_alacak_negatif_borc()
    {
        var (a, b) = YildizBugunBicim.CariToplamlari([22174.73m, 10480.06m, -3840m, 0m]);
        Assert.Equal(32654.79m, a);
        Assert.Equal(3840m, b);
    }

    // ── bUnit: Bugün ────────────────────────────────────────────────────────

    [Fact]
    public void Y4A_10_Yildiz_BASIT_Panel_tuval_duzeni_rakam_cumle_eylemler_alt_serit()
    {
        var e = Panel();
        foreach (var id in new[] { "panel-sade", "panel-karsilama", "panel-selam", "panel-hava", "panel-izgara", "panel-duzen-modu", "pozisyon-kart", "yasal-bantlar" })
            Assert.Empty(e.FindAll("#" + id));
        var r = e.Find("#yildiz-bugun-rakam");
        Assert.Equal("113.011", r.QuerySelector(".yildiz-bugun-tam")!.TextContent);
        Assert.Equal(",33 ₺", r.QuerySelector(".yildiz-bugun-kurus")!.TextContent);
        Assert.Equal(Bicim.TlKurus(113011.33m), r.TextContent);   // para/biçim davranışı değişmedi
        Assert.Equal("/raporlar/islem-defteri", r.GetAttribute("href"));
        Assert.Equal($"Bugünkü satış · 11 işlem. Kasada {Bicim.Tl(230356.54m)} nakit. Müşteriler bize borçlu: {Bicim.Tl(32654.79m)}. Biz müşterilere borçluyuz: {Bicim.Tl(3840m)}." /* P-DIL-BORC-KASA: tek kalıp */,
            e.Find("#yildiz-bugun-cumle").TextContent);
        Assert.Matches(@" · \d{1,2} [A-ZÇĞİÖŞÜa-zçğıöşü]+ \d{4} · Merkez Şube$", e.Find("#yildiz-bugun-ust-satir").TextContent);
        Assert.Equal("/islemler", e.Find("#yildiz-bugun-yeni-satis").GetAttribute("href"));
        Assert.Equal("Yeni satış", e.Find("#yildiz-bugun-yeni-satis").TextContent);
        Assert.Equal("/kasalar", e.Find("#yildiz-bugun-gunu-kapat").GetAttribute("href"));
        Assert.Equal("Günü kapat", e.Find("#yildiz-bugun-gunu-kapat").TextContent);
        Assert.Equal("Altın pozisyonu +157,3 g has", e.Find("#yildiz-bugun-pozisyon").TextContent);
        Assert.Equal("Vitrin 157,3 g", e.Find("#yildiz-bugun-vitrin").TextContent);
        Assert.Equal("Kâr maliyet eksik", e.Find("#yildiz-bugun-kar").TextContent);
        Assert.Empty(e.FindAll("[style]"));
    }

    [Fact]
    public void Y4A_11_Yildiz_PRO_da_ayni_tuval_duzeni_izgara_yok()
    {
        var e = Panel(ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#panel-izgara"));
        Assert.Empty(e.FindAll("#panel-duzen-modu"));
        Assert.NotEmpty(e.FindAll("#yildiz-bugun-liste"));
        Assert.Equal("113.011", e.Find("#yildiz-bugun-rakam .yildiz-bugun-tam").TextContent);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit, "#panel-sade")]
    [InlineData(ArayuzModuServisi.Profesyonel, "#panel-izgara")]
    public void Y4A_12_sunucu_eski_sema_dese_de_Panel_YildizBugun_ozgun_govde_YOK(string mod, string kok)
    {
        // Tema temizliği (17.10): Panel'in özgün gövdesi silindi. Sunucu (eski veri) "platin" dönse bile her modda
        // Bugün tuvali çizilir; eski kökler (#panel-sade / #panel-izgara / #panel-karsilama) YOK.
        sema = "platin";
        var e = Panel(mod);
        Assert.NotEmpty(e.FindAll("#yildiz-bugun"));
        Assert.Empty(e.FindAll(kok));
        Assert.Empty(e.FindAll("#panel-karsilama"));
    }

    [Fact]
    public void Y4A_13_seni_bekleyen_satirlari_tuval_kalibi()
    {
        var e = Panel();
        var satirlar = e.FindAll("#yildiz-bugun-liste > li.yildiz-bugun-sat");
        Assert.Equal(3, satirlar.Count);
        // alacak: ad · soluk tutar | "15 gün" | Tahsil et → müşteri kartı
        Assert.Equal("Tuna Akarsu ", satirlar[0].QuerySelector(".yildiz-bugun-sat-metin")!.ChildNodes[0].TextContent);   // tuval: "Ad " + soluk "· tutar"
        Assert.Equal($"· {Bicim.Tl(22174.73m)}", e.Find("#yildiz-bekleyen-cari-c1 .yildiz-bugun-sat-ek").TextContent);
        Assert.Equal("15 gün", e.Find("#yildiz-bekleyen-cari-c1-durum").TextContent);
        Assert.Equal("Tahsil et", e.Find("#yildiz-bekleyen-cari-c1-eylem").TextContent);
        Assert.Equal("/cariler?cariId=c1&islem=tahsilat", e.Find("#yildiz-bekleyen-cari-c1-eylem").GetAttribute("href"));
        Assert.Equal("30 gün geçti", e.Find("#yildiz-bekleyen-cari-c2-durum").TextContent);
        // borç: "· biz borçluyuz" | kırmızı "−3.840 ₺" | Öde
        Assert.Equal("· biz borçluyuz", e.Find("#yildiz-bekleyen-cari-c3 .yildiz-bugun-sat-ek").TextContent);
        var d = e.Find("#yildiz-bekleyen-cari-c3-durum");
        Assert.Equal("−3.840 ₺", d.TextContent);
        Assert.Contains("yildiz-bugun-sat-durum--kotu", d.ClassName);
        Assert.Equal("Öde", e.Find("#yildiz-bekleyen-cari-c3-eylem").TextContent);
        Assert.DoesNotContain("yildiz-bugun-sat-durum--kotu", e.Find("#yildiz-bekleyen-cari-c1-durum").ClassName);
        Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-bos"));
    }

    [Fact]
    public void Y4A_14_is_yoksa_bos_durum_yalniz_basarili_bos_yanitta()
    {
        yaslandirma = """{"yaslananCariler":[],"toplamSatir":0,"dahaFazlasiVar":false}""";
        var e = Panel();
        e.WaitForElement("#yildiz-bugun-bekleyen-bos");
        Assert.Contains("Bekleyen iş yok.", e.Find("#yildiz-bugun-bekleyen-bos").TextContent);
    }

    [Fact]
    public async Task Y4A_17_sunucu_sinirina_degdiyse_toplam_yazilmaz_not_yazilir_UX_S29()
    {
        yaslandirma = Yaslandirma.Replace("\"dahaFazlasiVar\":false", "\"dahaFazlasiVar\":true");
        Oturum(ArayuzModuServisi.Basit);
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        var e2 = Render<Kuyumcu.Web.Pages.Panel>();
        e2.WaitForElement("#yildiz-bugun-borclu-sinir");
        e2.WaitForElement("#yildiz-bugun-cumle");
        Assert.DoesNotContain("bize borçlu", e2.Find("#yildiz-bugun-cumle").TextContent);   // UX-S29: eksik toplam yazılmaz
    }

    [Fact]
    public async Task Y4A_15_satis_hatasi_sifira_cevrilmez_bant_cizilir_UX_S28()
    {
        gunlukHata = true;
        Oturum(ArayuzModuServisi.Basit);
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#yildiz-bugun-satis-hata");
        Assert.Empty(e.FindAll(".yildiz-bugun-tam"));
        Assert.DoesNotContain("0,00", e.Find("#yildiz-bugun").TextContent);
        Assert.DoesNotContain("Bugün satış yok", e.Find("#yildiz-bugun").TextContent);
    }

    [Fact]
    public void Y4A_16_her_baglanti_bir_ekrana_gider_bos_dugme_yok()
    {
        var e = Panel();
        var kok = e.Find("#yildiz-bugun");
        Assert.Empty(kok.QuerySelectorAll("button"));   // Panel POST ETMEZ; eylemler ekran açar
        foreach (var a in kok.QuerySelectorAll("a"))
            Assert.StartsWith("/", a.GetAttribute("href") ?? "");
    }

    // ── bUnit: üst menü ─────────────────────────────────────────────────────

    private IRenderedComponent<YildizUstMenu> Menu()
    {
        Oturum(ArayuzModuServisi.Profesyonel);
        Services.GetRequiredService<CanliKurServisi>().BaslatAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/");
        var e = Render<YildizUstMenu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-kur-22")));
        return e;
    }

    [Fact]
    public void Y4A_20_kur_seridi_tuval_metni_TL_isaretsiz_ayrac_metinde()
    {
        var e = Menu();
        var kur = e.Find("#yildiz-kur");
        Assert.Equal("SPAN", kur.TagName);
        Assert.Equal("HAS 6.656", e.Find("#yildiz-kur-has").TextContent);
        Assert.Equal("6.656", e.Find("#yildiz-kur-has b").TextContent);
        Assert.Equal("  ·  22 AYAR 6.097", e.Find("#yildiz-kur-22").TextContent);
        Assert.StartsWith("  ·  ", e.Find("#yildiz-kur-saat").TextContent);
        Assert.DoesNotContain("₺", kur.TextContent);
        Assert.Contains("₺", e.Find("#yildiz-kur-has").GetAttribute("title"));   // tam değer ipucunda
    }

    [Fact]
    public void Y4A_21_basit_profesyonel_anahtari_kullanici_menusunde_istek_atar()
    {
        var e = Menu();
        Assert.Equal("HAKAN T.", e.Find("#yildiz-kullanici-ac").TextContent);   // tuval: büyük harf metnin kendisi (tr-TR)
        var basit = e.Find("#yildiz-mod-basit");
        Assert.Equal("false", basit.GetAttribute("aria-pressed"));
        Assert.Equal("true", e.Find("#yildiz-mod-profesyonel").GetAttribute("aria-pressed"));
        basit.Click();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#yildiz-mod-basit").GetAttribute("aria-pressed")));
        Assert.Contains(istekler, i => i.StartsWith("PATCH ", StringComparison.Ordinal) && i.EndsWith("/kullanicilar/ben", StringComparison.Ordinal));
        Assert.True(Services.GetRequiredService<ArayuzModuServisi>().BasitMi);
    }

    // ── CSS sözleşmesi (tuval değerleri) ────────────────────────────────────

    [Fact]
    public void Y4A_30_kabuk_tuval_degerleri_menu_400_kur_400_kullanici_aralik()
    {
        var c = Regex.Replace(Css(), @"/\*.*?\*/", "", RegexOptions.Singleline);
        var oge = Kural(c, "html:root[data-sema=\"yildiz\"] .yildiz-menu-oge {");
        Assert.Contains("font-size: 14px;", oge);
        Assert.Contains("font-weight: 400;", oge);
        Assert.Contains("letter-spacing: .14em;", oge);
        Assert.Contains("margin-left: 24px;", Kural(c, "html:root[data-sema=\"yildiz\"] .yildiz-menu {"));
        var kur = Kural(c, "html:root[data-sema=\"yildiz\"] .yildiz-kur {");
        Assert.Contains("font-weight: 400;", kur);
        Assert.Contains("letter-spacing: .06em;", kur);
        Assert.DoesNotContain(".yildiz-kur > * + *::before", c);
        Assert.Contains("margin-left: 36px;", Kural(c, "html:root[data-sema=\"yildiz\"] .yildiz-kullanici {"));
        var ust = Kural(c, "html:root[data-sema=\"yildiz\"] .yildiz-ust {");
        Assert.Contains("font-size: 18px;", ust);
        Assert.Contains("line-height: 1.45;", ust);
    }

    [Fact]
    public void Y4A_31_bugun_tuval_degerleri_rakam_200_b1_b2_b3_satir_alt_serit()
    {
        var b = Blok4A();
        var r = Kural(b, ".yildiz-bugun .yildiz-bugun-rakam {");
        Assert.Contains("font-size: 112px;", r);
        Assert.Contains("font-weight: 200;", r);
        Assert.Contains("letter-spacing: -0.035em;", r);
        Assert.Contains("line-height: .95;", r);
        var k = Kural(b, ".yildiz-bugun .yildiz-bugun-kurus {");
        Assert.DoesNotContain("letter-spacing", k);   // tuvalde ebeveynin px değeri miras alınır
        Assert.Contains("grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);", Kural(b, ".yildiz-bugun-ana {"));
        Assert.Contains("padding: 72px 64px 56px;", Kural(b, ".yildiz-bugun-ana {"));
        Assert.Contains("font-weight: 300;", Kural(b, ".yildiz-bugun .yildiz-bugun-cumle {"));
        var b1 = Kural(b, ".yildiz-bugun .yildiz-b1 {");
        Assert.Contains("height: 56px;", b1);
        Assert.Contains("border-radius: 999px;", b1);
        Assert.Contains("background: var(--metin);", b1);
        Assert.Contains("border-bottom: 1px solid var(--kenarlik);", Kural(b, ".yildiz-bugun .yildiz-b2 {"));
        Assert.Contains("height: 40px;", Kural(b, ".yildiz-bugun .yildiz-b3 {"));
        var sat = Kural(b, ".yildiz-bugun .yildiz-bugun-sat {");
        Assert.Contains("padding: 22px 0;", sat);
        Assert.Contains("border-bottom: 1px solid var(--ayrac);", sat);
        var alt = Kural(b, ".yildiz-bugun .yildiz-bugun-alt {");
        Assert.Contains("gap: 48px;", alt);
        Assert.Contains("letter-spacing: .06em;", alt);
        Assert.Contains(".yildiz-kabuk:has(#yildiz-bugun) .ust-bar { display: none; }", b);
    }

    [Fact]
    public void Y4A_32_bolumdeki_her_secici_Yildiz_kapsamli_sabit_renk_yok()
    {
        var b = Blok4A();
        Assert.DoesNotMatch(@"#[0-9A-Fa-f]{3,8}\b(?![\w-])(?=[^{]*;)", b);   // sabit renk yok: yalnız var()
        foreach (Match m in Regex.Matches(b, @"(?<=^|[}\n])\s*([^{}@\n][^{}]*?)\s*\{", RegexOptions.Multiline))
            foreach (var s in Ayir(m.Groups[1].Value))
                Assert.StartsWith("html:root[data-sema=\"yildiz\"]", s.Trim());
    }

    private static IEnumerable<string> Ayir(string liste)
    {
        var derin = 0; var son = 0;
        for (var i = 0; i < liste.Length; i++)
        {
            if (liste[i] is '(' or '[') derin++;
            else if (liste[i] is ')' or ']') derin--;
            else if (liste[i] == ',' && derin == 0) { yield return liste[son..i]; son = i + 1; }
        }
        yield return liste[son..];
    }

    [Fact]
    public void Y4A_33_Inter_optik_boyutlu_surum_tuvalin_istedigi_dosya()
    {
        // tuval: css2?family=Inter:opsz,wght@14..32,200..600 → gstatic v20 (YILDIZ-4A'da vendor'landı)
        static string Ozet(string ad)
        {
            var yol = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "font", ad);
            return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(File.ReadAllBytes(yol))).ToLowerInvariant();
        }
        Assert.Equal("e990806dbc9b31e48655f37651732c80", Ozet("inter-latin.woff2"));
        Assert.Equal("e7fdb0113866ad8f42e81318d9adb714", Ozet("inter-latin-ext.woff2"));
        Assert.Contains("font-optical-sizing: auto;", Css());
    }

    [Fact]
    public void Y4A_34_karsilastirma_araci_kalici_ve_toleranslari_kurucu_kurali()
    {
        var yol = Path.Combine(CssKaynak.Kok(), "..", "dokumanlar", "09-uygulama-kayitlari", "betikler", "tur", "tuval-kiyas.mjs");
        var m = File.ReadAllText(yol);
        Assert.Contains("Math.abs(t.fs - u.fs) > 0.5", m);           // font-size ±0,5 px
        Assert.Contains("String(t.fw) !== String(u.fw)", m);         // ağırlık eşit
        Assert.Contains("Math.abs(t.ls - u.ls) > 0.01", m);          // harf aralığı ±0,01em
        Assert.Contains("t.renk !== u.renk", m);                     // renk eşit
        Assert.Contains("> 4) f(", m);                               // konum ±4 px
        Assert.Contains("fonts\\.googleapis\\.com", m);                  // Google Fonts yerel Inter ile değişir
    }
}
