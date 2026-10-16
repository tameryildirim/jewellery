using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A · 13. doğrulama (29.09, tasarım hakemi 2 KRİTİK — Hurda):
//  (1) "isteğe bağlı" ramatçı alanı gönderimde yalnız ALAN ADIYLA hata veriyordu;
//      kur oynaması (80.709,77 < 80.714) tezgâhtarın yazdığı tutarı eskitip ramatçı
//      istetiyordu. → mesaj CÜMLE (eksik tutar + işlem tutarı), görülmüş Tutar yazıldıysa
//      güncel Tutar'a eşitlenir ve onayda yazar, koşul oluşunca etiket zorunlu olur.
//  (2) onayda görülen tutar = gönderilen gövde = sunucunun döndürdüğü tutar (bant).
// Sahte sunucu hurda-satis yanıtını GÖVDEDEN hesaplar (sunucu formülü: kabul × fiyat,
// 2 hane) — sabit yanıt bu zinciri sınayamazdı.
// ═══════════════════════════════════════════════════════════════════════════
public sealed class ZOISLEMAHurdaTahsilTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ZOISLEMAHurdaTahsilTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":20,"girenHas":20,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""",
                var y when y.EndsWith("/hurda/defter") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/islemler/hurda-satis") => SatisYaniti(govde),
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

        /// <summary>Sunucu formülü (SatisModulu.HurdaSatis): toplam = Yuvarla2(kabul × birim fiyat). Has önce Yuvarla6 (AwayFromZero) — sunucuyla aynı.</summary>
        private static string SatisYaniti(string govde)
        {
            var b = System.Text.Json.JsonDocument.Parse(govde).RootElement;
            var has = Math.Round(b.GetProperty("hasGrami").GetDecimal(), 6, MidpointRounding.AwayFromZero);
            var kabul = b.TryGetProperty("aliciKabulHasGrami", out var k) && k.ValueKind == System.Text.Json.JsonValueKind.Number
                ? Math.Round(k.GetDecimal(), 6, MidpointRounding.AwayFromZero) : has;
            var fiyat = b.GetProperty("birimFiyatTlHasGram").GetDecimal();
            var toplam = Math.Round(kabul * fiyat, 2, MidpointRounding.AwayFromZero);
            var tahsil = b.TryGetProperty("tahsilEdilenTl", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.Number
                ? t.GetDecimal() : toplam;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return "{\"id\":\"h9\",\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGrami\":" + has.ToString(inv)
                 + ",\"birimFiyatTlHasGram\":" + fiyat.ToString(inv) + ",\"toplamTl\":" + toplam.ToString(inv)
                 + ",\"tahsilEdilenTl\":" + tahsil.ToString(inv) + ",\"kalanTl\":" + (toplam - tahsil).ToString(inv)
                 + ",\"kalanHurdaHasGram\":" + (20m - has).ToString(inv)
                 + ",\"kasaBakiyesi\":250000,\"paraBirimi\":\"TRY\",\"tarih\":\"2026-09-29T09:00:00Z\","
                 + "\"fireKaydiAcik\":false,\"fireNotu\":null,\"aliciKabulHasGrami\":" + kabul.ToString(inv)
                 + ",\"tahlilFarkiHasGram\":" + (kabul - has).ToString(inv) + "}";
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "MUDUR", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/defter")));
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.WaitForElement("#hrd-detay-ac").Click();
        return e;
    }

    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    private void KurDegistir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string id, decimal has) =>
        e.InvokeAsync(() => Services.GetRequiredService<CanliKurServisi>()
            .Uygula(new CanliKurServisi.KurAnlik(id, has, has + 50m, 34m, 37m, 43.5m)));

    private bool PostVar() => istekler.Any(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));

    private System.Text.Json.JsonElement PostGovdesi() =>
        System.Text.Json.JsonDocument.Parse(istekler.Single(i => i.Metot == "POST").Govde).RootElement;

    private static string RamatciEtiketi(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e) =>
        e.Find("label[for=hrd-cari]").TextContent;

    [Fact]
    public void ZO29t_eksik_tahsilde_ramatci_mesaji_CUMLE_eksik_ve_islem_tutariyla_etiket_zorunlu_olur()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");                           // 0,5 × 6.800 = 3.400
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        // Tahsil boşken (= tamamı) ramatçı isteğe bağlıdır.
        Assert.Contains(Dil().T.Ortak.Istege, RamatciEtiketi(e));

        Gir(e, "#hrd-tahsil", "3000");
        // Koşul oluşunca etiketten "isteğe bağlı" KALKAR.
        e.WaitForAssertion(() => Assert.DoesNotContain(Dil().T.Ortak.Istege, RamatciEtiketi(e)));

        e.Find("#hrd-kaydet").Click();
        var beklenen = string.Format(ZoIslemSozlugu.Tr.HurdaRamatciGerekli,
            Bicim.Para(400m, "TRY"), Bicim.Para(3400m, "TRY"));
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        // Mesaj alanın ADI değildir.
        Assert.NotEqual(HurdaOnaySozlugu.Tr.RamatciBasit, e.Find("#alan-hata-hrd-cari").TextContent.Trim());
        Assert.NotEqual(Dil().T.Hurda.Ramatci, e.Find("#alan-hata-hrd-cari").TextContent.Trim());
        Assert.Empty(e.FindAll("#hrd-onay-hedef"));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29t_kur_oynamasi_GORULEN_tutar_yazildiysa_ramatci_istenmez_guncel_tutara_esitlenir_onayda_yazar()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "3400");                       // tezgâhtar ekrandaki Tutar'ı yazdı

        KurDegistir(e, "kur-2", 6_810m);                     // kur oynadı: 0,5 × 6.810 = 3.405
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3405m, "TRY"), e.Find("#hrd-tutar").TextContent));
        // Kastı "tamamı" → ramatçı zorunlu DEĞİL.
        Assert.Contains(Dil().T.Ortak.Istege, RamatciEtiketi(e));

        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Empty(e.FindAll("#alan-hata-hrd-cari"));
        Assert.Contains(ZoIslemSozlugu.Tr.HurdaTahsilKurEsitlendi, ozet);
        Assert.Contains($"{HurdaOnaySozlugu.Tr.OzetTutar}: {Bicim.Para(3405m, "TRY")}", ozet);
        Assert.Contains($"{HurdaOnaySozlugu.Tr.OzetTahsil}: {Bicim.Para(3405m, "TRY")}", ozet);

        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var g = PostGovdesi();
        Assert.Equal(3405m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal(6810m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal("kur-2", g.GetProperty("kurSnapshotId").GetString());
    }

    [Fact]
    public void ZO29t_hic_GORULMEMIS_eksik_tutar_esitlenmez_ramatci_istenir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "3399");                       // ekranda hiç görünmemiş bir tutar
        KurDegistir(e, "kur-2", 6_810m);
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3405m, "TRY"), e.Find("#hrd-tutar").TextContent));

        e.Find("#hrd-kaydet").Click();
        var beklenen = string.Format(ZoIslemSozlugu.Tr.HurdaRamatciGerekli,
            Bicim.Para(6m, "TRY"), Bicim.Para(3405m, "TRY"));
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29t_elle_fiyatta_kur_esitlemesi_KAPALI_eski_tutar_ramatci_ister()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "3400");
        KurDegistir(e, "kur-2", 6_810m);                     // (0,5; 3.400) "görülmüş" kaydı oluşur
        Gir(e, "#hrd-fiyat", "7000");                        // elle fiyat → 3.500
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3500m, "TRY"), e.Find("#hrd-tutar").TextContent));

        e.Find("#hrd-kaydet").Click();
        var beklenen = string.Format(ZoIslemSozlugu.Tr.HurdaRamatciGerekli,
            Bicim.Para(100m, "TRY"), Bicim.Para(3500m, "TRY"));
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29t_tahsil_tutari_asarsa_alan_alti_CUMLE_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "5000");
        e.Find("#hrd-kaydet").Click();
        var beklenen = string.Format(ZoIslemSozlugu.Tr.HurdaTahsilAsim,
            Bicim.Para(1600m, "TRY"), Bicim.Para(3400m, "TRY"));
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.Find("#alan-hata-hrd-tahsil").TextContent.Trim()));
        Assert.Empty(e.FindAll("#hrd-onay-hedef"));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29u_ONAYDAKI_tutar_gonderilen_govde_ve_bant_AYNI_kur_pencere_acikken_degisse_de()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "12,5");
        Gir(e, "#hrd-kabul-has", "12,3");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-tahlil-gerekce")));
        e.Find("#hrd-tahlil-gerekce").Input("Rafineri tahlilinde 0,2 g eksik çıktı, tutanak alındı");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(83640m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        var onaylanan = Bicim.Para(Math.Round(12.3m * 6800m, 2), "TRY");       // 83.640 ₺
        Assert.Contains($"{HurdaOnaySozlugu.Tr.OzetTutar}: {onaylanan}", ozet);

        KurDegistir(e, "kur-2", 6_799.93m);                  // pencere açıkken kur oynar
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(Math.Round(12.3m * 6799.93m, 2), "TRY"),
            e.Find("#hrd-tutar").TextContent));
        Assert.Equal(ozet, e.Find("#hrd-onay-hedef").TextContent);

        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var g = PostGovdesi();
        Assert.Equal(6800m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal(83640m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        // Sunucunun hesapladığı ve banda yazılan tutar = onaylanan tutar.
        e.WaitForAssertion(() => Assert.Contains(onaylanan, e.Find("#hrd-basari").TextContent));
        Assert.DoesNotContain(Bicim.Para(Math.Round(12.3m * 6799.93m, 2), "TRY"), e.Find("#hrd-basari").TextContent);
    }

    [Fact]
    public void ZO29v_mesaj_gorunurken_kur_degisirse_tutar_BAYAT_kalmaz_ramatci_secilince_kalkar()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "3000");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(ZoIslemSozlugu.Tr.HurdaRamatciGerekli, Bicim.Para(400m, "TRY"), Bicim.Para(3400m, "TRY")),
            e.Find("#alan-hata-hrd-cari").TextContent.Trim()));

        KurDegistir(e, "kur-2", 7_000m);                     // 0,5 × 7.000 = 3.500
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(ZoIslemSozlugu.Tr.HurdaRamatciGerekli, Bicim.Para(500m, "TRY"), Bicim.Para(3500m, "TRY")),
            e.Find("#alan-hata-hrd-cari").TextContent.Trim()));

        // Tahsil tutara eşitlenince mesaj KALKAR (Kaydet'e basmadan).
        Gir(e, "#hrd-tahsil", "3500");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-cari")));
        Assert.False(PostVar());
    }

    // ── 14. doğrulama (29.09, doğrulayıcı KRİTİK): belirsiz "1.500" tahsil = TAMAMI sayılıyordu ──
    [Fact]
    public void ZO29w_belirsiz_1_500_tahsil_alan_hatasi_KALIR_onay_yok_POST_yok_bolum_kapanmaz()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");                                  // 0,5 × 6.800 = 3.400
        Gir(e, "#hrd-tahsil", "1.500");
        // Kaydet'ten ÖNCE de bölüm kapatılamaz (değer null ama kutu anlaşılamayan metin taşıyor).
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-detay-gizle")));
        e.Find("#hrd-kaydet").Click();
        // Bileşenin kendi belirsizlik mesajı Kaydet'ten SONRA da durur (form.Temizle onu silmez).
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-hrd-tahsil").TextContent)));
        Assert.Equal("1.500", e.Find("#hrd-tahsil").GetAttribute("value"));
        Assert.Equal("true", e.Find("#hrd-tahsil").GetAttribute("aria-invalid"));
        Assert.Empty(e.FindAll("#hrd-onay-hedef"));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        // Hatalı kutu varken "Detayı gizle" yok: kapalı bölüm gizli değer taşıyamaz.
        Assert.Empty(e.FindAll("#hrd-detay-gizle"));
        // Kur yenilense de kullanıcının yazdığı metin EZİLMEZ ve kayıt yine engellenir.
        KurDegistir(e, "kur-b", 6810m);
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(3405m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Assert.Equal("1.500", e.Find("#hrd-tahsil").GetAttribute("value"));
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29w_belirsiz_duzeltilince_kayit_acilir_ve_YAZILAN_tahsil_gider()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "1.500");
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Gir(e, "#hrd-tahsil", "3400");                              // tamamı, açıkça yazıldı
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-tahsil")));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        Assert.Equal(3400m, PostGovdesi().GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZO29w_belirsiz_birim_fiyat_1_500_eski_kur_fiyati_GITMEZ_onay_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-fiyat", "1.500");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-hrd-fiyat").TextContent)));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.False(PostVar());
    }

    // ── 14. doğrulama (doğrulayıcı ORTA): büyük kur sıçramasında eşitlenen FARK tutar olarak yazılır ──
    [Fact]
    public void ZO29x_kur_esitlemesi_onayda_YAZILAN_KAYDEDILECEK_ve_FARK_tutar_olarak_yazar()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-tahsil", "6800");                             // ekranda görülen Tutar
        KurDegistir(e, "kur-b", 7500m);
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(7500m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains(ZoIslemSozlugu.Tr.HurdaTahsilKurEsitlendi, ozet);
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.HurdaTahsilKurFarki,
            Bicim.Para(6800m, "TRY"), Bicim.Para(7500m, "TRY"), "+" + Bicim.Para(700m, "TRY")), ozet);
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29x_kur_dususunde_fark_EKSI_isaretle_yazilir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-tahsil", "6800");
        KurDegistir(e, "kur-b", 6790m);
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(6790m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.HurdaTahsilKurFarki,
            Bicim.Para(6800m, "TRY"), Bicim.Para(6790m, "TRY"), Bicim.Para(-10m, "TRY")), ozet);
    }

    // 29.09 (doğrulayıcı KRİTİK — has 6 haneden fazla ondalık): sunucu hası Yuvarla6 ile
    // yuvarlayıp TL'yi yuvarlanmış hasla hesaplar. Ekran 7. haneyi kabul etseydi onay
    // 6.800,00 der, sunucu 6.800,01 hesaplar, tahsil 0,01 eksik kalır → ramatçısız 400.
    // Karar: alan sunucunun kabul ettiği 6 haneyle sınırlanır (yazılan = kaydedilen).
    [Fact]
    public void ZO29y_has_7_hane_ALAN_HATASI_onay_yok_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1,0000005");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(ZoIslemSozlugu.Tr.HurdaHasHaneSiniri,
            e.Find("#alan-hata-hrd-has").TextContent.Trim()));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29y_alici_kabul_7_hane_ALAN_HATASI_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "2");
        Gir(e, "#hrd-kabul-has", "1,9999995");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(ZoIslemSozlugu.Tr.HurdaHasHaneSiniri,
            e.Find("#alan-hata-hrd-kabul-has").TextContent.Trim()));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.False(PostVar());
    }

    [Fact]
    public void ZO29y_has_6_hane_ONAY_GOVDE_KAYIT_ayni_ramatci_bos()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1,000001");
        var tutar = Math.Round(1.000001m * 6800m, 2, MidpointRounding.AwayFromZero);   // 6.800,01
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(tutar, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains($"{HurdaOnaySozlugu.Tr.OzetTutar}: {Bicim.Para(tutar, "TRY")}", ozet);
        Assert.Contains("1,000001 g", ozet);
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var g = PostGovdesi();
        Assert.Equal(1.000001m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(tutar, g.GetProperty("tahsilEdilenTl").GetDecimal());          // tamamı = onaydaki tutar
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(tutar, "TRY"), e.Find("#hrd-basari").TextContent));
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();
}
