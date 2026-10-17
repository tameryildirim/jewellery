using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-5 — KUR + VİTRİN + EMANET (dalga S3B, demo adım 7 ve 8)
//
// Sınanan sözleşme:
//   KUR    · HAS ÖNDE: has fiyatı sayfanın İLK kartındaki en büyük sayıdır
//          · iki yüz: kaynak seçici + kur geçmişi yalnız profesyonel yüzde,
//            basit yüzde yerine not + Görünüm bağlantısı (özellik silinmez)
//          · ham kod 0: MANUAL / ALTINKAYNAK / K22 / CEYREK_YENI (basit) YOK
//          · manuel kur girişi ÇEKMECEDE (tek düzenleme kalıbı), boş has → alan
//            altı mesaj + istek YOK
//   VİTRİN · kaynak okunur adla · Kur ekranına dönüş bağlantısı
//   EMANET · giriş / iade ÇEKMECEDE, ayar seçicide görünen ad (gövdede kod)
//          · AYRINTI çekmecesi: has gram · maden · müşteri KİLİTLİ ve gerekçeli,
//            Kaydet YOK, geri alma yolu ekranda (48-A storno onayına gider)
//          · hata "kayıt yok"a çevrilmez (UX-S28)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S35KurEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.Contains("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK","ANKARA"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""",
                var y when y.Contains("/kur/gecmis") => """
                    [{"id":"k2","altinHasFiyati":6810,"kaynak":"MANUAL",
                      "olusturuldu":"2026-09-14T08:00:00Z","ayarFiyatlari":[]},
                     {"id":"k1","altinHasFiyati":6800,"kaynak":"ALTINKAYNAK",
                      "olusturuldu":"2026-09-13T08:00:00Z","ayarFiyatlari":[]}]
                    """,
                var y when y.Contains("/kur/pariteler") => """
                    {"kaynak":"ALTINKAYNAK","pariteler":[
                      {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","alis":11500,"satis":11800,"birim":"adet"}]}
                    """,
                var y when y.Contains("/kur/guncel") => """
                    {"id":"kur-9","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,
                     "eurTry":null,"gbpTry":null,
                     "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-14T09:00:00Z",
                     "ayarFiyatlari":[{"ayarId":"K22","milyem":916,"gramFiyati":6228.8}]}
                    """,
                var y when y.Contains("/kur/manuel") =>
                    """{"id":"kur-10","altinHasFiyati":6900,"kaynak":"MANUAL","olusturuldu":"2026-09-14T10:00:00Z","ayarFiyatlari":[]}""",
                _ => """{"hata":"sahte API bu ucu tanımıyor"}""",
            };
            var kod = g.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return new HttpResponseMessage(kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public S35KurEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private void OturumAc(string mod)
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> Ciz(string? mod = null)
    {
        if (mod is not null) OturumAc(mod);
        var e = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        e.WaitForElement("#kur-has-onde");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kur-pariteler-tablo, .veri-tablosu")));
        lock (istekler) istekler.Clear();
        return e;
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    [Fact]
    public void S35_01_HAS_ONDE_has_fiyati_ilk_kartta_ve_kaynak_seciciden_ONCE()
    {
        var e = Ciz();

        var ilkKart = e.Find(".kart");
        Assert.NotNull(ilkKart.QuerySelector("#kur-has-onde"));
        Assert.Equal(Bicim.Tl(6800m), e.Find("#kur-has-deger").TextContent.Trim());
        Assert.Equal(KurVitrinEmanetSozlugu.Tr.HasBaslik, e.Find("#kur-has-baslik").TextContent.Trim());

        // DOM sırası: has kartı kaynak seçici kartından ÖNCE gelir.
        var markup = e.Markup;
        Assert.True(markup.IndexOf("id=\"kur-has-onde\"", StringComparison.Ordinal)
                    < markup.IndexOf("id=\"kur-kaynak-kart\"", StringComparison.Ordinal));
    }

    [Fact]
    public void S35_02_Has_alt_satiri_gram_ve_doviz_yazar_DEGERI_OLMAYANI_uydurmaz()
    {
        var e = Ciz();

        Assert.Contains(Bicim.Tl(7100m), e.Find("#kur-has-gram").TextContent);
        Assert.Contains(Bicim.Tl(34.20m), e.Find("#kur-has-usd").TextContent);
        // eurTry / gbpTry null: satır YOK (sıfır uydurulmaz).
        Assert.Empty(e.FindAll("#kur-has-eur"));
        Assert.Empty(e.FindAll("#kur-has-gbp"));
        // Kaynak okunur adla (ham ALTINKAYNAK değil).
        Assert.Contains("Altınkaynak", e.Find("#kur-has-kaynak").TextContent);
        Assert.DoesNotContain("ALTINKAYNAK", e.Find("#kur-has-kaynak").TextContent);
    }

    [Fact]
    public void S35_03_Basit_yuz_kaynak_ve_gecmis_YOK_yerine_not_ve_gorunum_baglantisi()
    {
        var e = Ciz(ArayuzModuServisi.Basit);

        Assert.Empty(e.FindAll("#kur-kaynak-kart"));
        Assert.Empty(e.FindAll("#kur-gecmis-bas"));
        Assert.Equal(KurVitrinEmanetSozlugu.Tr.ProNotu,
            e.Find("#kur-pro-notu").FirstChild!.TextContent.Trim());
        Assert.Equal("/ayarlar", e.Find("#kur-pro-notu-bag").GetAttribute("href"));
        // HAS ve sarrafiye basit yüzde de VAR (tezgâhtarın günlük işi).
        Assert.NotNull(e.Find("#kur-has-onde"));
        Assert.Contains("Yeni Çeyrek", e.Markup);
        // Yetki ≠ mod: manuel giriş düğmesi basit yüzde de durur.
        Assert.NotNull(e.Find("#kur-manuel-ac"));
    }

    [Fact]
    public void S35_04_Profesyonel_yuz_kaynak_ve_gecmis_VAR_not_YOK()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);

        Assert.NotNull(e.Find("#kur-kaynak-kart"));
        Assert.NotNull(e.Find("#kur-gecmis-bas"));
        Assert.Empty(e.FindAll("#kur-pro-notu"));
        // Profesyonel yüzde parite kodu ürün kodu gibi kalır.
        Assert.Contains("CEYREK_YENI", e.Markup);
    }

    [Fact]
    public void S35_05_Basit_yuzde_HAM_KOD_yok()
    {
        var e = Ciz(ArayuzModuServisi.Basit);

        // GÖRÜNEN metin (öznitelikler hariç) taranır.
        var metin = string.Concat(e.Nodes.Select(n => n.TextContent));
        foreach (var kod in new[] { "CEYREK_YENI", "ALTINKAYNAK", "MANUAL", "K22" })
            Assert.DoesNotContain(kod, metin, StringComparison.Ordinal);
        Assert.Contains("22 Ayar", metin);
    }

    [Fact]
    public void S35_06_Gecmis_rozeti_okunur_ad_yazar_ham_MANUAL_yazmaz()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);

        var satirlar = e.FindAll("tbody tr").Where(tr => tr.QuerySelector("[id^='kur-iptal-']") is not null).ToList();
        Assert.Equal(2, satirlar.Count);
        Assert.All(satirlar, tr =>
        {
            Assert.DoesNotContain("MANUAL", tr.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("ALTINKAYNAK", tr.TextContent, StringComparison.Ordinal);
        });
        Assert.Contains(Sozlukler.Tr.Kur.KaynakManuel, satirlar[0].TextContent);
        Assert.NotNull(satirlar[0].QuerySelector(".rozet-amber"));
    }

    [Fact]
    public void S35_07_Manuel_kur_CEKMECEDE_acilir_modal_degil_tek_kaydet()
    {
        var e = Ciz();
        e.Find("#kur-manuel-ac").Click();

        Assert.NotNull(e.Find("#cekmece-kur-manuel"));
        Assert.NotNull(e.Find("#cekmece-kur-manuel #m-has"));
        // Çekmecenin kendi Kaydet'i çizilmez: TEK kaydet düğmesi #m-kaydet.
        Assert.Empty(e.FindAll("#cekmece-kur-manuel-kaydet"));
        Assert.Single(e.FindAll("#cekmece-kur-manuel button.btn-birincil, #cekmece-kur-manuel button.dugme-birincil"));
        // Vazgeç çekmece altbilgisinde; tıklanınca çekmece kapanır (istek yok).
        e.Find("#cekmece-kur-manuel-iptal").Click();
        Assert.Empty(e.FindAll("#cekmece-kur-manuel"));
        Assert.DoesNotContain(Istekler(), i => i.Metot != "GET");
    }

    [Fact]
    public void S35_08_Manuel_kur_bos_HAS_alan_alti_mesaj_ve_ISTEK_YOK_gecerli_POST()
    {
        var e = Ciz();
        e.Find("#kur-manuel-ac").Click();
        e.Find("#m-has").Input("0");
        e.Find("#m-kaydet").Click();

        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-m-has").TextContent));
        Assert.NotNull(e.Find("#cekmece-kur-manuel"));

        e.Find("#m-has").Input("6900");
        e.Find("#m-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(Istekler(), i => i.Metot == "POST");
            Assert.EndsWith("/kur/manuel", post.Yol);
            using var d = System.Text.Json.JsonDocument.Parse(post.Govde);
            Assert.Equal(6900m, d.RootElement.GetProperty("altinHasFiyati").GetDecimal());
            Assert.Empty(e.FindAll("#cekmece-kur-manuel"));
        });
    }

    [Fact]
    public async Task S35_09_Sozluk_TR_ve_EN_eksiksiz_ve_ingilizce_yuz()
    {
        foreach (var p in typeof(KurVitrinEmanetMetinleri).GetProperties())
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(KurVitrinEmanetSozlugu.Tr)), $"TR boş: {p.Name}");
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(KurVitrinEmanetSozlugu.En)), $"EN boş: {p.Name}");
        }
        Assert.Same(KurVitrinEmanetSozlugu.En, KurVitrinEmanetSozlugu.Al("en"));
        Assert.Same(KurVitrinEmanetSozlugu.Tr, KurVitrinEmanetSozlugu.Al("tr"));

        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Ciz();
        Assert.Equal("Fine gold · 1 gram", e.Find("#kur-has-baslik").TextContent.Trim());
    }
}

public sealed class S35VitrinEkranTestleri : BunitContext
{
    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var g = yol.Contains("/pano/guncel", StringComparison.Ordinal)
                ? """
                  {"kaynak":"ALTINKAYNAK","zaman":"2026-09-21T08:00:00Z",
                   "satirlar":[{"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"}],
                   "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false}
                  """
                : """
                  {"ayarlar":[],"yuvarlamaAdimlari":[0,10],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":0}
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public S35VitrinEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    [Fact]
    public void S35_10_Vitrin_kaynagi_okunur_adla_yazar_ham_kod_YOK()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-pano");

        var kaynak = e.Find("#vitrin-kaynak").TextContent;
        Assert.Contains("Altınkaynak", kaynak);
        Assert.DoesNotContain("ALTINKAYNAK", kaynak);
    }

    [Fact]
    public void S35_11_Vitrinden_Kur_ekranina_donus_baglantisi_VAR()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-pano");

        var bag = e.Find("#vitrin-kur-don");
        Assert.Equal("/kur", bag.GetAttribute("href"));
        Assert.Equal(KurVitrinEmanetSozlugu.Tr.KuraDon, bag.TextContent.Trim());
    }
}

public sealed class S35EmanetEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool cariHatali;
    /// <summary>
    /// PAKET EMANET-1: ayrı sunucu arama kutusu (#e-cari-ara) yalnız liste
    /// sunucu sınırına (50) değince görünür. Bu kutuyu kullanan test listeyi
    /// sınıra getirir (c1 + 49 müşteri).
    /// </summary>
    private bool sinirdaCari;

    private sealed class SahteApi(S35EmanetEkranTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/cari", StringComparison.Ordinal) && s.cariHatali)
                return Yanit(HttpStatusCode.ServiceUnavailable, """{"message":"cari servisi yok"}""");

            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.Contains("/storno") => """{"stornoHareketId":"01M3M5VNEKBJQYD5EE59FH9332","hasGrami":-3.2175}""",
                var y when y.Contains("/emanet/giris") => """{"id":"h9"}""",
                var y when y.Contains("/cikis") => """{"cariId":"c1","iadeEdilenHas":5,"maden":"ALTIN","madenAdi":"Altın"}""",
                var y when y.Contains("/emanet/") => """
                    {"cariId":"c1","mevcutHasGramBakiye":12.5,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":12.5}],
                     "hareketler":[
                       {"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":12.5,
                        "aciklama":"Bilezik 2 adet","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"h2","tur":"EMANET_CIKIS","tarih":"2026-09-11T10:00:00Z","hasGrami":-3,
                        "aciklama":null,"stornoKaynakId":"h0","fisId":null,"maden":"ALTIN"}]}
                    """,
                var y when y.Contains("/cari") && s.sinirdaCari => "[" + string.Join(",",
                    new[] { """{"id":"c1","ad":"Ayşe Demir"}""" }.Concat(
                        Enumerable.Range(2, 49).Select(i => $$"""{"id":"c{{i}}","ad":"Müşteri {{i}}"}"""))) + "]",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => """{"hata":"sahte API bu ucu tanımıyor"}""",
            };
            var kod = g.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return Yanit(kod, g);
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string g) => new(kod)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public S35EmanetEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> MusteriSecili()
    {
        // TEMA TEMİZLİĞİ 17.10: Yıldız yolu — listede müşterinin satırına bas (özgün gövde), seçicide c1.
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");
        SeciciYardim.Sec(e, "e-cari", "c1");
        // PAKET EMANET-1: geçmiş "Eski kayıtlar" katlanırında KAPALI gelir; açılır.
        e.WaitForElement("#e-gecmis-ac").Click();
        e.WaitForElement("#eh-detay-h1");
        lock (istekler) istekler.Clear();
        return e;
    }

    [Fact]
    public void S35_12_Giris_CEKMECEDE_ayar_gorunen_adla_govdeye_KOD_gider()
    {
        var e = MusteriSecili();
        e.Find("#e-giris-ac").Click();

        Assert.NotNull(e.Find("#cekmece-emanet-giris #e-gram"));
        Assert.Empty(e.FindAll("#cekmece-emanet-giris-kaydet"));
        // Ayar seçicisi: görünen metin "22 Ayar", ham "K22" metin olarak YOK.
        Assert.Contains("22 Ayar", e.Find("#e-ayar").TextContent);
        Assert.DoesNotContain("K22", e.Find("#e-ayar").TextContent);

        e.Find("#e-gram").Input("20");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(Istekler(), i => i.Metot == "POST");
            Assert.EndsWith("/emanet/giris", post.Yol);
            using var d = System.Text.Json.JsonDocument.Parse(post.Govde);
            Assert.Equal("K22", d.RootElement.GetProperty("ayarId").GetString());
            Assert.Equal("kur-9", d.RootElement.GetProperty("kurSnapshotId").GetString());
        });
    }

    [Fact]
    public void S35_13_Iade_CEKMECEDE_ve_POST_dogru_cariye()
    {
        var e = MusteriSecili();
        e.Find("#e-cikis-ac").Click();

        Assert.NotNull(e.Find("#cekmece-emanet-cikis #c-has"));
        Assert.Contains("22 Ayar", e.Find("#c-ayar").TextContent);
        e.Find("#c-has").Input("5");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(Istekler(), i => i.Metot == "POST");
            Assert.EndsWith("/emanet/c1/cikis", post.Yol);
        });
    }

    [Fact]
    public void S35_14_Ayrinti_cekmecesi_KILITLI_alanlari_gerekceyle_yazar_Kaydet_YOK()
    {
        var e = MusteriSecili();
        e.Find("#eh-detay-h1").Click();

        var c = e.Find("#cekmece-emanet-hareket");
        Assert.Equal("12,5000 has", e.Find("#eh-detay-has").TextContent.Trim());
        Assert.Equal("Altın", e.Find("#eh-detay-maden").TextContent.Trim());
        Assert.Equal("Ayşe Demir", e.Find("#eh-detay-musteri").TextContent.Trim());
        // EMANET-1 tasarım hakemi 28.09 (KRİTİK): ayrı "Değiştirilemez" başlığı ve kilit
        // kutusu kaldırıldı; kilitli alanlar girdisiz yazılır, düzeltme yolu TEK kısa satır.
        Assert.Empty(e.FindAll("#eh-detay-kilitli-baslik"));
        Assert.Empty(c.QuerySelectorAll(".degismez-ipucu"));
        Assert.NotNull(e.Find("#eh-detay-kilitli"));
        Assert.Equal(EmanetSadeSozlugu.Tr.DetayDuzeltme, e.Find("#eh-detay-duzeltme").TextContent.Trim());
        // Kilitli alanlar için GİRDİ yok, Kaydet düğmesi yok.
        Assert.Empty(c.QuerySelectorAll("input, textarea, select"));
        Assert.Empty(e.FindAll("#cekmece-emanet-hareket-kaydet"));
        // Hiçbir yazma isteği atılmadı.
        Assert.DoesNotContain(Istekler(), i => i.Metot != "GET");
    }

    [Fact]
    public void S35_15_Ayrintidan_GERI_AL_ortak_storno_onayina_gider_ve_48A_ucuna_POST()
    {
        var e = MusteriSecili();
        e.Find("#eh-detay-h1").Click();
        var dugme = e.Find("#eh-detay-geri-al-h1");
        Assert.False(dugme.HasAttribute("disabled"));
        Assert.Equal(EmanetSadeSozlugu.Tr.SatirGeriAl, dugme.TextContent.Trim());   // EMANET-1 t39: düğme yalnız "Geri al"

        dugme.Click();
        Assert.Empty(e.FindAll("#cekmece-emanet-hareket"));       // çekmece kapandı
        Assert.NotNull(e.Find("#eh-storno-onay"));                // ortak onay açıldı
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST"); // onaysız istek YOK

        e.Find("#eh-storno-gerekce").Input("Gram yanlış yazıldı, düzeltme fişiyle geri alınıyor.");
        e.Find("#eh-storno-onayla").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(Istekler(), i => i.Metot == "POST");
            Assert.Contains("/islemler/h1/storno", post.Yol);
            using var d = System.Text.Json.JsonDocument.Parse(post.Govde);
            Assert.Equal("kur-9", d.RootElement.GetProperty("kurSnapshotId").GetString());
            // Doğrulayıcı 28.09 (EMANET-1, DÜŞÜK): ham ULID tezgâhtara BASILMAZ; POST
            // yanıtı yine okunur (UX-S27) — ters has gramı banda yazılır.
            var bant = e.Find("#basari-bandi").TextContent;
            Assert.Contains(string.Format(EmanetSadeSozlugu.Tr.GeriAlYapildi, Bicim.Has(-3.2175m)), bant);
            Assert.DoesNotContain("01M3M5VNEKBJQYD5EE59FH9332", bant);
        });
    }

    [Fact]
    public void S35_16_Geri_alinamayan_kayitta_dugme_KILITLI_ve_sebebi_yazili()
    {
        var e = MusteriSecili();
        // h2 bir storno ters kaydıdır (stornoKaynakId dolu) → geri alınamaz.
        e.Find("#eh-detay-h2").Click();

        var dugme = e.Find("#eh-detay-geri-al-h2");
        Assert.True(dugme.HasAttribute("disabled"));
        // Ters kayıt "Giriş/Çıkış" diye değil "Geri alma kaydı" diye okunur.
        Assert.Equal(KurVitrinEmanetSozlugu.Tr.GeriAlmaKaydi, e.Find("#eh-detay-tur").TextContent.Trim());
        var kilit = e.Find("#eh-detay-geri-al-kilit-h2").TextContent;
        Assert.Contains(KurVitrinEmanetSozlugu.Tr.GeriAlKapali, kilit);
        dugme.Click();
        Assert.Empty(e.FindAll("#eh-storno-onay"));
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
    }

    [Fact]
    public void S35_17_Ayrinti_Vazgec_cekmeceyi_kapatir_istek_atmaz()
    {
        var e = MusteriSecili();
        e.Find("#eh-detay-h1").Click();
        e.Find("#cekmece-emanet-hareket-iptal").Click();

        Assert.Empty(e.FindAll("#cekmece-emanet-hareket"));
        Assert.DoesNotContain(Istekler(), i => i.Metot != "GET");
    }

    [Fact]
    public void S35_18_Cari_listesi_hatasi_son_bilinen_listeyi_SILMEZ()
    {
        sinirdaCari = true;   // EMANET-1: sunucu arama kutusu yalnız sınırda görünür
        var e = MusteriSecili();
        cariHatali = true;
        e.Find("#e-cari-ara").Input("Ay");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        // Seçili müşteri ve hareketleri yerinde: hata "kayıt yok"a çevrilmedi.
        Assert.NotNull(e.Find("#eh-detay-h1"));
        e.Find("#eh-detay-h1").Click();
        Assert.Equal("Ayşe Demir", e.Find("#eh-detay-musteri").TextContent.Trim());
    }
}
