using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 41 — /banka dekont formundaki MASAK kapısı [bUnit]
//
// Sınananlar:
//   1. Eşik altı tutarda HİÇBİR MASAK alanı görünmez (bugünkü akış değişmez)
//   2. Eşik üstü tutarda /masak/gereksinim çağrılır ve kimlik alanları AÇILIR
//   3. Alanlar açıkken boşsa 'dekont-kaydet' KAPALI ve sebebi YAZILI (UX-S30)
//   4. Alanlar dolunca POST atılır ve gövdedeki masak bloğu DOĞRU alan
//      adlarını taşır (belgeTuru, belgeNo, adSoyad) — sunucu DTO'suyla birebir
//   5. Cari seçilmeden eşik üstü tutarda 'kümülatif kurulamıyor' uyarısı çıkar
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   gerçekçi yanıt verildi (/masak/esikler, /masak/gereksinim, /cari).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BankaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public BankaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];

        /// <summary>
        /// Q10-5: doluysa POST /bankalar bu gövdeyle 400 döner — gövde, canlı
        /// sunucunun 5166'da DÖNDÜRDÜĞÜ ŞEKLİN AYNISIDIR (alan adı camelCase "iban").
        /// </summary>
        public string? HesapPost400;

        /// <summary>KESIF-P6 tur 2: true → hareket listesine karşı tarafı ve açıklaması BOŞ ikinci satır eklenir.</summary>
        public bool BosHareketEkle;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));
            if (HesapPost400 is { } h400 && istek.Method == HttpMethod.Post && yol.EndsWith("/bankalar"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(h400, System.Text.Encoding.UTF8, "application/json"),
                };

            // Sunucunun MasakKuralMotoru.Gereksinim(tutar) çıktısıyla aynı şekil.
            // ZO-ISLEM-A 29.09: ekran müşteri seçiliyken eşik altında da sorar (günlük toplam) —
            // sahte yanıt tutara göre GERÇEKÇİ: kimlik eşiği altında (ve önceki işlem yokken) YOK.
            var tutarEslesme = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
            var sorulanTutar = tutarEslesme.Success
                ? decimal.Parse(tutarEslesme.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
            var gereksinim = sorulanTutar is > 0m and < 185000m
                ? """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}"""
                : yol.Contains("tutar=185000")
                ? """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}"""
                : """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";

            var y = yol switch
            {
                var v when v.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/masak/gereksinim") => gereksinim,
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") =>
                    BosHareketEkle
                        ? """{"hareketler":[{"id":"bh1","yon":"GELEN","tutar":1250,"karsiTarafAd":"Ali Veli","aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z"},{"id":"bh2","yon":"GELEN","tutar":8000,"karsiTarafAd":null,"aciklama":null,"durum":"BEKLIYOR","tarih":"2026-09-16T11:00:00Z"}]}"""
                        : """{"hareketler":[{"id":"bh1","yon":"GELEN","tutar":1250,"karsiTarafAd":"Ali Veli","aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z"}]}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                _ => "{}",
            };

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> DekontModali(string tutar, string? cariId = "c1")
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-dekont-gir");
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        istekler.Clear();
        // ZO-ISLEM-A: müşteri "+ … ekle" bölümünde (MASAK eşiğinde kendiliğinden açılır).
        if (cariId is not null) { e.DekontDetayAc(); SeciciYardim.Sec(e, "dekont-cari", cariId); }
        e.Find("#dekont-tutar").Input(tutar);
        return e;
    }

    [Fact]
    public void Esik_altinda_MASAK_alanlari_GORUNMEZ()
    {
        var e = DekontModali("1000");

        Assert.Empty(e.FindAll("#masak-belge-no"));
        Assert.Empty(e.FindAll("#dekont-kaydet-sebep"));
        Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled"));
        // ZO-ISLEM-A 29.09: müşteri seçiliyken eşik altında da gereksinim MÜŞTERİYLE sorulur (günlük
        // toplamı yalnız sunucu bilir); müşterisiz eşik altı sorgu YOKTUR. Sunucu YOK derse alan açılmaz.
        Assert.All(istekler.Where(i => i.Yol.Contains("/masak/gereksinim")),
            i => Assert.Contains("cariId=c1", i.Yol));
    }

    [Fact]
    public void Esik_ustunde_GEREKSINIM_ucu_cagrilir_ve_kimlik_alanlari_ACILIR()
    {
        var e = DekontModali("185000");

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/masak/gereksinim?tutar=185000"));
            Assert.NotNull(e.Find("#masak-belge-no"));
            Assert.NotNull(e.Find("#masak-ad-soyad"));
        });
    }

    [Fact]
    public void Kimlik_alanlari_BOSKEN_kaydet_KAPALI_ve_sebebi_yazili()
    {
        var e = DekontModali("185000");

        e.WaitForAssertion(() =>
        {
            Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled"));
            Assert.False(string.IsNullOrWhiteSpace(e.Find("#dekont-kaydet-sebep").TextContent));
        });
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void Kimlik_alanlari_DOLUNCA_POST_atilir_ve_masak_blogu_dogru_alan_adlariyla_gider()
    {
        var e = DekontModali("185000");
        e.WaitForElement("#masak-belge-no");

        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Ayşe Demir");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        e.Find("#dekont-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/bankalar/b1/hareket"));
            Assert.Contains("\"masak\"", istek.Govde);
            Assert.Contains("\"belgeTuru\"", istek.Govde);
            Assert.Contains("\"belgeNo\"", istek.Govde);
            Assert.Contains("\"adSoyad\"", istek.Govde);
            Assert.Contains("\"cariId\":\"c1\"", istek.Govde);
        });
    }

    [Fact]
    public void Cari_secilmeden_esik_ustunde_KUMULATIF_UYARISI_cikar()
    {
        var e = DekontModali("185000", cariId: null);

        e.WaitForAssertion(() =>
            Assert.False(string.IsNullOrWhiteSpace(e.Find("#dekont-masak-cari-uyari").TextContent)));
    }

    /// <summary>UX-S25: kimlik tespiti append-only — düzenle düğmesi YOK, gerekçe VAR.</summary>
    [Fact]
    public void MASAK_blogunda_DEGISMEZ_IPUCU_gerekce_ve_yol_yazar()
    {
        var e = DekontModali("185000");

        e.WaitForAssertion(() =>
        {
            var ipucu = e.Find("#degismez-ipucu-MasakKimlikTespiti");
            Assert.Empty(ipucu.QuerySelectorAll("button"));
            Assert.True(ipucu.TextContent.Trim().Length > 40);
        });
    }

    // ── PAKET Q10-5 (QA turu 10 /banka: "geçersiz IBAN'da sunucu 400
    //    döndükten sonra bile alan 'hatali' işaretlenmiyor") ──────────────

    [Fact]
    public void Q105_Sunucu_400_iban_alani_isaretlenir_duzeltince_duser()
    {
        sahte.HesapPost400 = """
            {"message":["Geçerli bir IBAN giriniz (TR + 24 rakam)"],"alanlar":{"iban":["Geçerli bir IBAN giriniz (TR + 24 rakam)"]},"error":"Bad Request","statusCode":400}
            """;
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#banka-yeni");
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-iban");
        e.Find("#hesap-banka").Input("QA Banka");
        e.Find("#hesap-ad").Input("Vadesiz");
        e.Find("#hesap-iban").Input("TR12345");
        e.Find("#hesap-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/bankalar"));
            var girdi = e.Find("#hesap-iban");
            Assert.Contains("hatali", girdi.ClassName);
            Assert.Equal("true", girdi.GetAttribute("aria-invalid"));
            Assert.Contains("IBAN", e.Find("#alan-hata-hesap-iban").TextContent);
        });

        e.Find("#hesap-iban").Input("TR330006100519786457841326");
        Assert.Empty(e.FindAll("#alan-hata-hesap-iban"));
        Assert.Equal("false", e.Find("#hesap-iban").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Q105_Sunucu_400_yalniz_alan_hatasi_ise_genel_bant_yazmaz_alan_duzeltilince_hic_hata_kalmaz()
    {
        // Doğrulama bulgusu (24.09): IBAN düzeltilince alan temizleniyordu ama
        // modal içindeki genel bant aynı mesajı bir sonraki gönderime dek taşıyordu.
        sahte.HesapPost400 = """
            {"message":["Geçerli bir IBAN giriniz (TR + 24 rakam)"],"alanlar":{"iban":["Geçerli bir IBAN giriniz (TR + 24 rakam)"]},"error":"Bad Request","statusCode":400}
            """;
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#banka-yeni");
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-iban");
        e.Find("#hesap-banka").Input("QA Banka");
        e.Find("#hesap-ad").Input("Vadesiz");
        e.Find("#hesap-iban").Input("TR12345");
        e.Find("#hesap-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("IBAN", e.Find("#alan-hata-hesap-iban").TextContent));
        // Mesaj alanın altında; genel bantta TEKRAR yazılmaz.
        var bantlar = e.FindAll(".bant-hata").Select(x => x.TextContent).ToList();
        Assert.DoesNotContain(bantlar, b => b.Contains("IBAN"));
        e.Find("#hesap-iban").Input("TR330006100519786457841326");
        Assert.DoesNotContain("Geçerli bir IBAN", e.Markup);
    }

    [Fact]
    public void Q105_BantMesaji_eslenemeyen_alan_ya_da_alansiz_hata_bantta_kalir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var esleme = new Dictionary<string, string> { ["Iban"] = "hesap-iban" };
        var yalnizAlan = new ApiHata(400, "iban kötü") { Alanlar = new Dictionary<string, string[]> { ["iban"] = ["iban kötü"] } };
        Assert.Null(FormDurumu.BantMesaji(yalnizAlan, dil, esleme));
        var yabanci = new ApiHata(400, "x kötü") { Alanlar = new Dictionary<string, string[]> { ["iban"] = ["a"], ["x"] = ["x kötü"] } };
        Assert.Equal("x kötü", FormDurumu.BantMesaji(yabanci, dil, esleme));
        var genel = new ApiHata(500, "sunucu hatası");
        Assert.Equal("sunucu hatası", FormDurumu.BantMesaji(genel, dil, esleme));
        Assert.Equal("iban kötü", FormDurumu.BantMesaji(yalnizAlan, dil, null));
    }

    [Fact]
    public void Q105_Basit_modda_dekont_degismezlik_metinleri_storno_demez_PRO_ayni()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = Render<Kuyumcu.Web.Pages.Banka>().HareketleriAc();   // ZO-ISLEM-A
        e.WaitForElement("#degismez-ipucu-BankaHareketiFinansalAlan");
        var metin = e.Find("#degismez-ipucu-BankaHareketiFinansalAlan").TextContent;
        Assert.DoesNotContain("storno", metin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.DekontKilitliAlanlarBasit, metin);
        Assert.Contains(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.DekontDegismezYoluBasit, metin);
    }

    [Fact]
    public void Q105_Profesyonel_modda_dekont_metinleri_degismez()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().HareketleriAc();   // ZO-ISLEM-A
        e.WaitForElement("#degismez-ipucu-BankaHareketiFinansalAlan");
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Banka.DekontKilitliAlanlar,
            e.Find("#degismez-ipucu-BankaHareketiFinansalAlan").TextContent);
    }

    // ── PAKET SADE-1 (göz #27): dört kilit kutusu TEK kısa satırın arkasında ──
    [Fact]
    public void SADE1_Degismez_kutulari_TEK_ozet_satirinin_arkasinda_katli_metinler_silinmedi()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().HareketleriAc();   // ZO-ISLEM-A
        e.WaitForElement("#degismez-ipucu-BankaHareketiFinansalAlan");
        var ayrinti = e.Find("#bh-degismez-ayrinti");
        Assert.Equal("details", ayrinti.TagName, StringComparer.OrdinalIgnoreCase);
        Assert.False(ayrinti.HasAttribute("open"));
        Assert.Contains(Kuyumcu.Core.I18n.SadeSozlugu.Tr.BankaDegismezOzet, e.Find("#bh-degismez-ozet").TextContent);
        // Tablo altındaki HER kilit kutusu katlanın İÇİNDEDİR (dışarıda metin duvarı yok).
        var kutular = e.FindAll(".degismez-ipucu[id^='degismez-ipucu-BankaHareketi']");
        Assert.NotEmpty(kutular);
        Assert.All(kutular, k => Assert.NotNull(k.Closest("#bh-degismez-ayrinti")));
    }

    // ═══ KESIF-P6 (#47) — dekont tutarı "abc" ve ABD biçimi "1,500.75" SESSİZ kabul edilmez ═══════
    // Keşif (PRO 1440): "abc" yazılıp çıkılınca kutu sessizce önceki değere dönüyordu; "1,500.75"
    // sessizce 1.500,75 sayılıyordu. Kural: alan altı hata + Kaydet PASİF + POST YOK.
    [Theory]
    [InlineData("abc")]
    [InlineData("1,500.75")]
    public void Banka_dekont_abc_kaydet_pasif(string yazilan)
    {
        var e = DekontModali("0,50");
        e.Find("#dekont-tutar").Input(yazilan);
        e.Find("#dekont-tutar").Blur();

        Assert.Equal(yazilan, e.Find("#dekont-tutar").GetAttribute("value"));   // yazılan KALIR, eski değere dönmez
        Assert.Equal("true", e.Find("#dekont-tutar").GetAttribute("aria-invalid"));
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Form.SayiAnlasilmadi,
            e.Find("#alan-hata-dekont-tutar").TextContent);
        Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#dekont-kaydet-sebep").TextContent));
        istekler.Clear();
        e.Find("#dekont-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ═══ KESIF-P6 tur 2 (TOPLU § ZO-ISLEM-A ORTA) — iki boş alan TEK satır, açıcılar KORUNUR ═══
    [Fact]
    public void KESIFP6_Banka_iki_bos_alan_tek_satir_sinifi_acicilar_korunur()
    {
        sahte.BosHareketEkle = true;
        var e = Render<Kuyumcu.Web.Pages.Banka>().HareketleriAc();
        e.WaitForElement("#si-banka-dekont-karsitaraf-bh2-ac");
        var bos = e.Find("#si-banka-dekont-karsitaraf-bh2-ac").Closest("td")!;
        var dolu = e.Find("#si-banka-dekont-karsitaraf-bh1-ac").Closest("td")!;
        Assert.Contains("zo-karsi-ikisi-bos", bos.ClassList);
        Assert.DoesNotContain("zo-karsi-ikisi-bos", dolu.ClassList);
        // Her veri girişi düzenlenebilir: iki açıcı da yerinde.
        Assert.NotNull(e.Find("#si-banka-dekont-aciklama-bh2-ac"));
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — dekont tutarı tuş tuş "1.500" ═══════════════════════
    // Kapısız formda "1.5" ara değeri kalıyordu: kutu hata yazarken Kaydet 1,5 TL'lik dekont POST edebilirdi.
    [Fact]
    public void OK1_K4_dekont_tutari_belirsiz_1_500_POST_YOK_sebep_yazar_duzeltince_yazilan_gider()
    {
        var e = DekontModali("1");
        foreach (var a in new[] { "1.", "1.5", "1.50", "1.500" }) e.Find("#dekont-tutar").Input(a);
        e.Find("#dekont-tutar").Blur();
        Assert.Equal("1.500", e.Find("#dekont-tutar").GetAttribute("value"));
        Assert.Equal("true", e.Find("#dekont-tutar").GetAttribute("aria-invalid"));
        Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled"));
        var etiket = e.Find("label[for='dekont-tutar']").TextContent.Replace("*", "").Trim();
        Assert.Contains(etiket, e.Find("#dekont-kaydet-sebep").TextContent);
        istekler.Clear();
        e.Find("#dekont-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        e.Find("#dekont-tutar").Input("1500");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
        var govde = System.Text.Json.JsonDocument.Parse(istekler.First(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(1500m, govde.GetProperty("tutar").GetDecimal());
    }
}
