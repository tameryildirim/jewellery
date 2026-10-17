using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 (Y-10) — /hurda EKRANI [bUnit]
//
// DoD EK 1/1: "tıklanınca hiçbir şey olmayan düğme" YOKTUR.
// DoD EK 1/3: boş/geçersiz gönderim alan altı Türkçe mesaj verir; POST ATILMAZ.
// UX-S21 has fiyatı kurdan gelir ve rozet kaynağı söyler.
// UX-S22 tek maden / tek kasa → seçici GİZLENİR.
// UX-S25 hurda satışı düzeltilemez → DegismezIpucu (devre dışı düğme DEĞİL).
// UX-S27 sunucunun döndürdüğü KALAN bakiye banda yazılır.
// UX-S28 hata "kayıt yok"a çevrilmez.
// UX-S29 sunucu sınırı ekranda yazar.
// UX-S30 kilitli düğmenin sebebi yanında yazar.
// K-13    fire kaydının YOKLUĞU ekranda gerekçesiyle görünür.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P20EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string? hataliYolParcasi;

    private string bakiyeYaniti = """
        {"subeId":"s1",
         "satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0}],
         "fireKaydiAcik":false,
         "fireNotu":"Bu sürümde fire/ramat farkı kaydı yoktur: fire imalat (atölye) sınırındadır ve kapsam kararı (K-13) kurucudadır."}
        """;

    private string kasaYaniti = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""";

    public P20EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => hataliYolParcasi, () => bakiyeYaniti, () => kasaYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string?> hataliParca,
        Func<string> bakiye,
        Func<string> kasalar) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            if (hataliParca() is { } p && yol.Contains(p))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası: hurda defteri okunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // ⚠ Sahte API'nin "{}" varsayılanı YETMEZ: yeni uçlara GERÇEKÇİ yanıt
            //   verilir, yoksa ekran boş modelle yeşil kalır ve sözleşme kayması
            //   görünmez olur.
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") => bakiye(),
                var y when y.EndsWith("/hurda/defter") => """
                    {"satirlar":[
                       {"id":"h1","tur":"HURDA_ALIS","tarih":"2026-09-20T08:00:00Z","tlTutari":-25648,
                        "hasGrami":9.16,"maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,
                        "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false}],
                     "toplamAdet":140,"limit":100,"dahaFazlasiVar":true,
                     "sinirNotu":"Yalnız son 100 hurda hareketi gösteriliyor (toplam 140)."}
                    """,
                var y when y.EndsWith("/islemler/hurda-satis") => """
                    {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":5,
                     "birimFiyatTlHasGram":6800,"toplamTl":34000,"tahsilEdilenTl":34000,"kalanTl":0,
                     "kalanHurdaHasGram":4.16,"kasaBakiyesi":284000,"paraBirimi":"TRY",
                     "tarih":"2026-09-21T09:00:00Z","fireKaydiAcik":false,"fireNotu":null}
                    """,
                var y when y.EndsWith("/kasalar") => kasalar(),
                var y when y.Contains("/cari") =>
                    """[{"id":"c1","ad":"Anadolu Rafineri"}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-21T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void OturumAc(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/bakiye")));
        // ZO-ISLEM-A: isteğe bağlı alanlar "+ Detay ekle", defter kapalı akordeon
        // arkasında. Bu dosyanın testleri o alanların KURALLARINI sınar; açılış
        // davranışı ZOISLEMAEkranTestleri'nde ayrıca sınanır.
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        return e;
    }

    // ── DoD EK 1/1 — ölü düğme yok ──────────────────────────────────────────

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_DISABLED()
    {
        var e = Ekran();
        foreach (var d in e.FindAll("button"))
        {
            var kilitli = d.HasAttribute("disabled");
            var tiklanabilir = d.Attributes.Any(a => a.Name.Contains("onclick", StringComparison.OrdinalIgnoreCase));
            Assert.True(kilitli || tiklanabilir, $"Ölü düğme: #{d.Id}");
        }
    }

    // ── UX-S22 — tek maden / tek kasa seçicisi gizlenir ─────────────────────

    [Fact]
    public void Tek_maden_ve_tek_kasa_varsa_seciciler_GIZLENIR()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-maden")));
        Assert.Contains("Altın", e.Find("#hrd-maden-sabit").TextContent);
        Assert.Empty(e.FindAll("#hrd-kasa"));
        Assert.Contains("Merkez Kasa", e.Find("#hrd-kasa-sabit").TextContent);
    }

    [Fact]
    public void Iki_maden_varsa_maden_secici_GORUNUR()
    {
        bakiyeYaniti = """
            {"subeId":"s1","satirlar":[
               {"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0},
               {"maden":"GUMUS","madenAdi":"Gümüş","hasGram":120,"girenHas":120,"cikanHas":0,"tahlilFarkiHas":0}],
             "fireKaydiAcik":false,"fireNotu":null}
            """;
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-maden")));
        Assert.Empty(e.FindAll("#hrd-maden-sabit"));
    }

    // ── UX-S21 — has fiyatı kurdan gelir, rozet kaynağı söyler ──────────────

    [Fact]
    public void Has_fiyati_KURDAN_gelir_ve_rozeti_soyler()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#hrd-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        Assert.Contains("Kurdan geldi", e.Find("#hrd-fiyat-kaynak").TextContent);
    }

    [Fact]
    public void Elle_girilen_fiyat_ELLE_GIRILDI_rozeti_alir_ve_EZILMEZ()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#hrd-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        e.Find("#hrd-fiyat").Input("7100");
        e.WaitForAssertion(() => Assert.Contains("Elle girildi", e.Find("#hrd-fiyat-kaynak").TextContent));
    }

    // ── DoD EK 1/3 — sessiz etkisizlik yok ──────────────────────────────────

    [Fact]
    public void Bos_has_gram_ile_gonderimde_alan_alti_mesaj_cikar_ve_POST_ATILMAZ()
    {
        var e = Ekran();
        // ⚠ Kur gelmeden düğme KİLİTLİDİR; kilit açılmadan tıklamak testi
        //   kararsız yapardı (yanlış sebeple yeşil/kırmızı).
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-kaydet").Click();

        e.WaitForElement("#alan-hata-hrd-has");   // render KUYRUĞA girer, beklenir
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
    }

    [Fact]
    public void Bakiyeden_fazla_cikis_ekranda_da_engellenir()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-has").Input("50");
        e.Find("#hrd-kaydet").Click();

        e.WaitForElement("#alan-hata-hrd-has");   // render KUYRUĞA girer, beklenir
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
    }

    // ── UX-S27 — sunucunun sonucu banda yazılır ─────────────────────────────

    [Fact]
    public void Kaydedilince_sunucunun_KALAN_BAKIYESI_banda_yazilir()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-has").Input("5");
        e.Find("#hrd-kaydet").Click();
        // Q10-2: geri alınamaz kayıt önce onaya gider; POST onaydan sonra.
        e.WaitForElement("#hrd-onay-onayla").Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")));
        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
        Assert.Contains("\"maden\":\"ALTIN\"", post.Govde);
        Assert.Contains("\"hasGrami\":5", post.Govde);
        Assert.Contains("\"kasaId\":\"ks1\"", post.Govde);
        Assert.Contains("\"kurSnapshotId\":\"kur-1\"", post.Govde);

        e.WaitForAssertion(() => Assert.Contains("4,160 g", e.Find("#hrd-basari").TextContent));
    }

    // ── UX-S30 — kilitli düğme sebebini söyler ──────────────────────────────

    [Fact]
    public void Bakiye_yokken_kaydet_KILITLI_ve_sebebi_yazili()
    {
        bakiyeYaniti = """{"subeId":"s1","satirlar":[],"fireKaydiAcik":false,"fireNotu":null}""";
        var e = Ekran();
        e.WaitForAssertion(() => Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        Assert.Contains("bakiyesi yok", e.Find("#hrd-kaydet-kilit").TextContent);
    }

    // ── UX-S28 — hata "kayıt yok"a çevrilmez ────────────────────────────────

    [Fact]
    public void Sunucu_hatasi_bos_duruma_CEVRILMEZ()
    {
        hataliYolParcasi = "/hurda/bakiye";
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Contains("Sunucu hatası", e.Find("#hrd-hata").TextContent));
        Assert.DoesNotContain("hurda hareketi yok", e.Markup);
    }

    // ── UX-S29 — sunucu sınırı ekranda yazar ────────────────────────────────

    [Fact]
    public void Sunucu_siniri_EKRANDA_yazar()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Contains("toplam 140", e.Find("#hrd-sinir-notu").TextContent));
    }

    // ── K-13 — fire gerekçesi SUNUCUDA durur, ekranda ÇİZİLMEZ ─────────────
    // ZO-ISLEM-A 28.09 (tasarım hakemi KRİTİK): "K-13 kararı", "kayıt türü DEĞİLDİR"
    // metni kullanıcıya gösterilen bir GELİŞTİRİCİ NOTUYDU. Ekran karar kodu basmaz.

    [Fact]
    public void Fire_gerekcesi_GELISTIRICI_NOTU_olarak_ekranda_CIZILMEZ()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-has")));
        Assert.Empty(e.FindAll("#hrd-fire-notu"));
        Assert.Empty(e.FindAll("#hrd-fire-ayrinti"));
        Assert.DoesNotContain("K-13", e.Markup);
    }

    // ── UX-S25 — düzeltilemezlik gerekçesi; devre dışı düğme DEĞİL ──────────

    [Fact]
    public void Hurda_satisi_icin_DegismezIpucu_cizilir_ve_icinde_dugme_YOKTUR()
    {
        var e = Ekran();
        var ipucu = e.WaitForElement("#degismez-ipucu-HurdaSatisi");
        // ZO-ISLEM-A 29.09 (tasarım hakemi KRİTİK): "bu sürümde stornolanmaz … ters kayıt motoru" bir
        // geliştirici notuydu; kutu iki modda da kullanıcı dilinde tek cümle — değişmezliği ve doğru yolu
        // (satırdaki Tahlil farkını düzelt) söyler.
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.HurdaDefterKilitBasit, Sozlukler.Tr.Hurda.Duzelt), ipucu.TextContent);
        Assert.DoesNotContain("bu sürümde", ipucu.TextContent);
        Assert.Empty(ipucu.QuerySelectorAll("button"));
    }

    // ── UX-S23 — bakiye kartı formu o madene ayarlar ────────────────────────

    [Fact]
    public void Bakiye_karti_tiklaninca_form_o_madene_ayarlanir()
    {
        bakiyeYaniti = """
            {"subeId":"s1","satirlar":[
               {"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0},
               {"maden":"GUMUS","madenAdi":"Gümüş","hasGram":120,"girenHas":120,"cikanHas":0,"tahlilFarkiHas":0}],
             "fireKaydiAcik":false,"fireNotu":null}
            """;
        var e = Ekran();
        e.WaitForElement("#hrd-bakiye-GUMUS").Click();
        e.WaitForAssertion(() => Assert.Contains("120", e.Find("#hrd-has-sinir").TextContent));
    }
}
