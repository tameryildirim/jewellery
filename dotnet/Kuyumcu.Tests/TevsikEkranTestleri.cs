using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 — EKRAN TARAFI (bUnit)
//   TB-15: Cari → Tahsilat penceresinde "Nakit" SUNUCUNUN söylediğine göre kapanır.
//   TB-16: Uyum → "Nakit sınırı aşımları" sekmesi bileşeni + Panel KRİTİK satırı.
// Ölçülen şey "düğme var mı" değil: hangi uca HANGİ GÖVDEYLE gidildiği, hangi
// düğmenin neden kilitli olduğu. Sahte API her uca GERÇEKÇİ yanıt verir.
// ⚠ Görünürlük/sığma/taşma bUnit ile KANITLANMAZ — gerçek tarayıcı ölçümü rapordadır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TevsikEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string gunlukYaniti = """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false,"taksit":{"kuralDevrede":false,"nakitAlinabilir":null,"buyukIslemTutari":null,"buyukIslemGunu":null,"buyukIslemHaddi":null,"sebep":null}}""";
    private string ihlalYaniti = """{"ihlaller":[],"kapanmamisSayisi":0,"sinir":200,"dahaVar":false,"bayramEksik":false}""";
    private int ihlalDurumKodu = 200;
    private string dogYaniti = """{"satirlar":[],"sinir":100,"dahaVar":false,"gunSayisi":90}""";

    private const string UcIhlal = """
        {"ihlaller":[
          {"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"ODEME","cariId":"c9","cariAd":"Hurda Satıcı","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK","kalanIsGunu":3,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false},
          {"id":"i2","tur":"GUNLUK_KUMULATIF","yon":"TAHSILAT","cariId":"c1","cariAd":"Ayşe Demir","ihlalGunu":"2026-09-30","nakitToplamTl":35000,"hadTl":30000,"bildirimSonGunu":"2026-10-07","durum":"ACIK","kalanIsGunu":2,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false},
          {"id":"i3","tur":"TAKSIT","yon":"TAHSILAT","cariId":"c1","cariAd":"Ayşe Demir","ihlalGunu":"2026-09-20","nakitToplamTl":20000,"hadTl":30000,"bildirimSonGunu":"2026-09-26","durum":"BILDIRILDI","kalanIsGunu":0,"kapatmaTarihi":"2026-09-22T09:00:00Z","bildirimTarihi":"2026-09-22","bildirimEvrakNo":"E-1","gec":false}
        ],"kapanmamisSayisi":2,"sinir":200,"dahaVar":false,"bayramEksik":true}
        """;

    private TaskCompletionSource? kasaKapisi;
    private string gunlukYanitiC2 = """{"had":30000,"taksit":{"kuralDevrede":false,"nakitAlinabilir":null}}""";
    private string cariYaniti = """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":62000,"hasGramBakiye":0}]""";

    public TevsikEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(TevsikEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            if (yol.EndsWith("/kasalar") && t.kasaKapisi is { } kapi) await kapi.Task;
            var g = yol switch
            {
                var y when y.Contains("/tevsik/gunluk") && y.Contains("cariId=c2") => t.gunlukYanitiC2,
                var y when y.Contains("/tevsik/gunluk") => t.gunlukYaniti,
                var y when y.Contains("/dilekce") => """
                    {"taslak":true,"damga":"TASLAK — mali müşavirinizle kontrol edin",
                     "ihlal":{"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"ODEME","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK"},
                     "isletme":{"unvan":"Örnek Kuyumculuk","vergiDairesi":"Kadıköy","vergiNo":"1234567890","adres":"Moda Cd. 1","telefon":null},
                     "karsiTaraf":{"ad":"Hurda Satıcı","vergiNo":"11111111110","adres":null,"telefon":null},
                     "hareketler":[{"id":"h1","tarih":"2026-10-01T09:00:00Z","tur":"HURDA_ALIS","tutarTl":36000,"nakitTl":36000}],
                     "dayanak":"VUK mükerrer md.355; 459 sıra no.lu VUK Genel Tebliği §4.1"}
                    """,
                var y when y.Contains("/tevsik/ihlaller/dogrulanamayanlar") => t.dogYaniti,
                var y when y.Contains("/bildirildi") => """{"id":"i1","durum":"BILDIRILDI","gec":false,"bildirimTarihi":"2026-10-02","bildirimSonGunu":"2026-10-08"}""",
                var y when y.Contains("/gecersiz") => """{"id":"i1","durum":"GECERSIZ"}""",
                var y when y.Contains("/tevsik/ihlaller") => (kod = t.ihlalDurumKodu) == 200 ? t.ihlalYaniti : """{"message":"sunucu hatası"}""",
                var y when y.Contains("/masak/ozet") => """{"acikBildirim":2,"suresiGecmis":1,"yaklasan":1,"son30GunTespit":4}""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/fiyat/listeler") => "[]",
                var y when y.Contains("/cari/c1") && istek.Method == HttpMethod.Get =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                // TEMA TEMİZLİĞİ 17.10: Yıldız'da tahsilat kartın eylemidir — kart açılırken tam kart (GET /cari/{id}) çekilir.
                // Kart, müşterinin IBAN listesini de çeker (M7a — CariIbanListesi): gerçekçi boş liste.
                var y when y.EndsWith("/ibanlar") => "[]",
                var y when y.EndsWith("/cari/c2") && istek.Method == HttpMethod.Get =>
                    """{"id":"c2","ad":"Kemal Devirli","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                var y when y.Contains("/tahsilat") => """{"id":"c1"}""",
                var y when y.Contains("/cari") => t.cariYaniti,
                _ => "[]",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Oturum(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null ? v.ToString() : null;
    }

    // ═════════ TB-15 — Tahsilat penceresi ═════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> TahsilatAc()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.KartEylemi("c1", "tahsilat");   // TEMA TEMİZLİĞİ 17.10: satıra dokun → kart → Tahsilat
        e.WaitForElement("#th-tutar");
        return e;
    }

    [Fact]
    public void Tahsilat_buyuk_islem_borcunda_NAKIT_PASIF_ve_nedenli_olu_dugme_yok_havale_govdeye_gider()   // tur 2: NAKİT görünür ama PASİF + neden; ölü düğme yok
    {
        gunlukYaniti = """{"had":30000,"taksit":{"kuralDevrede":true,"nakitAlinabilir":0,"buyukIslemTutari":62000,"buyukIslemGunu":"2026-09-27","buyukIslemHaddi":30000,"sebep":"30.000,00 TL'yi aşan 27.09.2026 tarihli satıştan kalan borç"}}""";
        var e = TahsilatAc();
        e.WaitForElement("#th-taksit");

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/tevsik/gunluk?cariId=c1"));
        var nakit = Assert.Single(SeciciYardim.Secenekler(e, "th-odeme"), o => o.Deger() == "NAKIT");
        Assert.Equal("true", nakit.GetAttribute("aria-disabled"));                     // devre dışı…
        Assert.Contains("nakit alınamaz", nakit.TextContent, StringComparison.Ordinal);  // …ve nedeni yazılı
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "th-odeme"));
        Assert.Empty(e.FindAll("#th-taksit-kart"));    // tur 2: tıklanınca bir şey yapmayan "Kartla al" kalktı
        Assert.Empty(e.FindAll("#th-taksit-havale"));
        Assert.Contains("Kartla ya da havaleyle alın", e.Find("#th-taksit-cumle").TextContent, StringComparison.Ordinal);
        Assert.Contains("30.000", e.Find("#th-taksit-cumle").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("tevsik", e.Find("#th-taksit-cumle").TextContent, StringComparison.OrdinalIgnoreCase);

        SeciciYardim.Sec(e, "th-odeme", "NAKIT");                                   // pasif öğe seçilemez
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "th-odeme"));
        SeciciYardim.Sec(e, "th-odeme", "HAVALE");
        Assert.Equal("HAVALE", SeciciYardim.SeciliDeger(e, "th-odeme"));
        e.Find("#th-tutar").Input("20000");
        e.Find("#th-kaydet").Click();
        var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/tahsilat"));
        Assert.Equal("HAVALE", Alan(post.Govde, "odemeTuru"));
    }

    [Fact]
    public void Tahsilat_kismen_izinli_nakitte_sinir_cumlesi_ve_asiminda_KAYDET_kilitli_sebebi_yazili()
    {
        gunlukYaniti = """{"had":30000,"taksit":{"kuralDevrede":true,"nakitAlinabilir":4000,"buyukIslemTutari":50000,"buyukIslemGunu":"2026-09-27","buyukIslemHaddi":30000,"sebep":"x"}}""";
        var e = TahsilatAc();
        e.WaitForElement("#th-taksit");
        Assert.Contains("NAKIT", SeciciYardim.Secenekler(e, "th-odeme").Select(o => o.Deger()));
        Assert.Contains("4.000", e.Find("#th-taksit-cumle").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#th-taksit-kart"));

        Assert.DoesNotContain("uyari-kirmizi", e.Find("#th-taksit").GetAttribute("class") ?? "", StringComparison.Ordinal);

        e.Find("#th-tutar").Input("5000");
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        // Tur 2 (hakem KRİTİK): sebep TUTAR ALANININ ALTINDA kırmızı; "Eksik olan" satırı YOK; kutu kırmızı
        var alan = e.Find("#th-tutar").Closest(".alan")!;
        Assert.Contains("Nakit en çok", alan.TextContent, StringComparison.Ordinal);
        Assert.Contains("4.000", alan.TextContent, StringComparison.Ordinal);
        Assert.Contains("kartla ya da havaleyle", alan.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#th-kaydet-kilit"));
        Assert.Contains("uyari-kirmizi", e.Find("#th-taksit").GetAttribute("class") ?? "", StringComparison.Ordinal);

        e.Find("#th-tutar").Input("4000");
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.DoesNotContain("Nakit en çok", e.Find("#th-tutar").Closest(".alan")!.TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tur 2 (gerçek tarayıcı bulgusu, 375 koyu): A carisinin "en çok 10.000 ₺ nakit"
    /// kutusu, B carisinin penceresi açılırken (kasalar yüklenirken) B'nin başlığı altında
    /// görünüyordu. Pencere açılır açılmaz eski kutu temizlenir.
    /// </summary>
    [Fact]
    public void Tahsilat_baska_cariye_gecince_ONCEKI_carinin_nakit_kutusu_gorunmez()
    {
        gunlukYaniti = """{"had":30000,"taksit":{"kuralDevrede":true,"nakitAlinabilir":10000,"buyukIslemTutari":50000,"buyukIslemGunu":"2026-09-27","buyukIslemHaddi":30000,"sebep":"x"}}""";
        cariYaniti = """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":62000,"hasGramBakiye":0},{"id":"c2","ad":"Kemal Devirli","tur":"MUSTERI","tlBakiye":1000,"hasGramBakiye":0}]""";
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.KartEylemi("c1", "tahsilat");   // TEMA TEMİZLİĞİ 17.10: kartın eylemi
        e.WaitForElement("#th-taksit");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Vazgeç").Click();
        // Kasa önbelleği (10 sn) tarayıcıdaki gibi BAYAT: istek ağa gider ve beklenir.
        Services.GetRequiredService<ApiIstemci>().Onbellek.Simdi = () => DateTimeOffset.UtcNow.AddMinutes(5);
        kasaKapisi = new TaskCompletionSource();
        e.KartEylemi("c2", "tahsilat");
        e.WaitForElement("#th-tutar");
        Assert.Empty(e.FindAll("#th-taksit"));          // kasalar beklenirken eski kutu YOK
        kasaKapisi.SetResult();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("cariId=c2")));
        Assert.Empty(e.FindAll("#th-taksit"));          // c2'de kural devrede değil
    }

    [Fact]
    public void Tahsilat_kural_devrede_degilse_bant_YOK_nakit_varsayilan()
    {
        var e = TahsilatAc();
        Assert.Empty(e.FindAll("#th-taksit"));
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "th-odeme"));
    }

    // ═════════ TB-16 — Uyum sekmesi bileşeni ══════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Bilesenler.NakitSiniriAsimlari> Sekme(string rol = "SAHIP")
    {
        Oturum(rol);
        var e = Render<Kuyumcu.Web.Bilesenler.NakitSiniriAsimlari>();
        return e;
    }

    [Fact]
    public void Sekme_acik_kayitlar_listede_kapali_kayit_KAPALI_akordeonda_yon_metinleri_dogru()
    {
        ihlalYaniti = UcIhlal;
        var e = Sekme();
        e.WaitForElement("#nsa-liste");
        Assert.NotNull(e.Find("#nsa-satir-i1"));
        Assert.NotNull(e.Find("#nsa-satir-i2"));
        Assert.Empty(e.FindAll("#nsa-satir-i3"));
        Assert.Empty(e.FindAll("#nsa-kapali-i3"));           // akordeon kapalı: gövde DOM'da yok
        Assert.NotNull(e.Find("#nsa-kapali-ac"));
        Assert.Contains("3 iş günü", e.Find("#nsa-son-i1").TextContent, StringComparison.Ordinal);
        Assert.Contains("Mali müşavirinize danışın", e.Find("#nsa-son-i2").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("iş günü", e.Find("#nsa-son-i2").TextContent, StringComparison.Ordinal);
        // Tur 3 (zorunlu-önce): bayram notu açılışta görünmez; KAPALI "Bu liste nasıl oluşur?" içinde.
        Assert.Empty(e.FindAll("#nsa-bayram-eksik"));
        e.Find("#nsa-nasil-ac").Click();
        Assert.NotNull(e.Find("#nsa-bayram-eksik"));
        e.Find("#nsa-nasil-ac").Click();
        Assert.DoesNotContain("ODEME", e.Markup, StringComparison.Ordinal);   // ham kod basılmaz
        Assert.DoesNotContain("GUNLUK_KUMULATIF", e.Markup, StringComparison.Ordinal);

        e.Find("#nsa-kapali-ac").Click();
        Assert.Contains("Bildirildi", e.Find("#nsa-kapali-i3").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tur 3 (orkestratör 22:20 madde 4): Uyum → Nakit sınırı aşımları açılışta açıklama satırı
    /// taşımaz (0); açıklama + bayram notu + değişmezlik kutusu en altta KAPALI katlanırdadır.
    /// Açılışta görünen metinde ham mevzuat kodu yoktur.
    /// </summary>
    [Fact]
    public void Tur3_Uyum_nakit_acilista_aciklama_satiri_YOK_teknik_kutu_KAPALI_katlanirda()
    {
        ihlalYaniti = UcIhlal;
        var e = Sekme();
        e.WaitForElement("#nsa-liste");
        Assert.Empty(e.FindAll("#nsa-aciklama"));
        Assert.Empty(e.FindAll(".degismez-ipucu"));
        Assert.Empty(e.FindAll("#nsa > p"));                       // listeden önce serbest açıklama satırı yok
        var nasil = e.Find("#nsa-nasil-ac");
        Assert.Equal("false", nasil.GetAttribute("aria-expanded"));
        Assert.DoesNotContain("459", e.Find("#nsa").TextContent, StringComparison.Ordinal);

        nasil.Click();
        Assert.NotNull(e.Find("#nsa-aciklama"));
        Assert.NotNull(e.Find("#nsa-bayram-eksik"));
        Assert.NotNull(e.Find("#nsa-nasil-govde .degismez-ipucu"));
    }

    /// <summary>
    /// Tur 3 (madde 1): tahsilat kutusunda mevzuat kodu yalnız title ipucunda; görünür metinde yok.
    /// </summary>
    [Fact]
    public void Tur3_Tahsilat_kutusu_dayanak_YALNIZ_title_ipucunda_gorunur_metinde_kod_yok()
    {
        gunlukYaniti = """{"had":30000,"taksit":{"kuralDevrede":true,"nakitAlinabilir":0,"buyukIslemTutari":62000,"buyukIslemGunu":"2026-09-27","buyukIslemHaddi":30000,"sebep":"x"}}""";
        var e = TahsilatAc();
        e.WaitForElement("#th-taksit");
        Assert.Contains("459", e.Find("#th-taksit-neden").GetAttribute("title"), StringComparison.Ordinal);
        e.Find("#th-taksit-neden").SetAttribute("open", "");
        Assert.DoesNotContain("459", e.Find("#th-taksit").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("VUK", e.Find("#th-taksit").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Sekme_bos_durum_hata_bandi_ayri()
    {
        var e = Sekme();
        e.WaitForAssertion(() => Assert.Contains("Nakit sınırı aşımı yok", e.Markup, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#nsa-hata"));
    }

    [Fact]
    public void Sekme_sunucu_hatasi_BOS_DURUMA_cevrilmez_hata_bandi_cizilir()
    {
        ihlalDurumKodu = 500;
        var e = Sekme();
        e.WaitForElement("#nsa-hata");
        Assert.DoesNotContain("Nakit sınırı aşımı yok", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Dilekce_dugmesi_dilekce_ucunu_cagirir_TASLAK_damgali_belge_cizer_listeye_donulur()
    {
        ihlalYaniti = UcIhlal;
        var e = Sekme();
        e.WaitForElement("#nsa-dilekce-i1").Click();
        e.WaitForElement("#nsa-dilekce");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/tevsik/ihlaller/i1/dilekce"));
        Assert.Contains("TASLAK", e.Find("#nsa-dilekce-taslak").TextContent, StringComparison.Ordinal);
        Assert.Contains("Kadıköy", e.Find("#nsa-dilekce-muhatap").TextContent, StringComparison.Ordinal);
        Assert.Contains("36.000", e.Find("#nsa-dilekce-govde").TextContent, StringComparison.Ordinal);
        Assert.Contains("Eski altın alışı", e.Find("#nsa-dilekce-islemler").TextContent, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#nsa-dilekce-yazdir"));
        e.Find("#nsa-dilekce-geri").Click();
        Assert.NotNull(e.Find("#nsa-liste"));
    }

    [Fact]
    public void Bildirildi_cekmecesi_gerekce_20_karakterden_kisaysa_kilitli_gecerli_govde_POST_edilir_basari_bandi()
    {
        ihlalYaniti = UcIhlal;
        var e = Sekme();
        e.WaitForElement("#nsa-bildirildi-i1").Click();
        e.WaitForElement("#nsa-gerekce");
        Assert.DoesNotContain("(isteğe bağlı) (isteğe bağlı)", e.Markup, StringComparison.Ordinal);   // tur 2: etiket tekrarı yok
        e.Find("#nsa-gerekce").Input("kısa");
        // Tur 2 (K18): "4/20" TEK yerde (Kaydet kilit sebebi); "4 / 500" alan sayacı yok
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, "4/20"));
        Assert.DoesNotContain("4 / 500", e.Markup, StringComparison.Ordinal);
        e.Find("#nsa-gerekce").Input("Vergi dairesine dilekçe elden verildi");
        e.Find("#nsa-evrak-no").Input("E-45");
        var kaydet = e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet");
        Assert.False(kaydet.HasAttribute("disabled"));
        kaydet.Click();
        var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/tevsik/ihlaller/i1/bildirildi"));
        Assert.Equal("E-45", Alan(post.Govde, "evrakNo"));
        Assert.Equal("Vergi dairesine dilekçe elden verildi", Alan(post.Govde, "gerekce"));
        Assert.NotNull(Alan(post.Govde, "bildirimTarihi"));
        e.WaitForElement("#nsa-basari");
    }

    [Fact]
    public void Yanlis_kayit_yalniz_SAHIPte_gorunur_gerekce_sonra_ONAY_sonra_POST()
    {
        ihlalYaniti = UcIhlal;
        var m = Sekme("MUHASEBE");
        m.WaitForElement("#nsa-liste");
        Assert.Empty(m.FindAll("#nsa-gecersiz-i1"));

        var e = Sekme("SAHIP");
        e.WaitForElement("#nsa-gecersiz-i2").Click();
        e.WaitForElement("#nsa-gecersiz-gerekce").Input("havale yapılmış, kasaya yanlış NAKİT girilmiş");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/gecersiz"));   // önce onay
        e.WaitForElement("#nsa-gecersiz-onay-onayla").Click();
        var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/tevsik/ihlaller/i2/gecersiz"));
        Assert.Equal("havale yapılmış, kasaya yanlış NAKİT girilmiş", Alan(post.Govde, "gerekce"));
    }

    // ═════════ TUR 2 — tasarım hakemi bulguları ═════════════════════════════

    private const string GerekceIhlal = """
        {"ihlaller":[
          {"id":"g1","tur":"ISLEM_TUTARI","yon":"TAHSILAT","cariId":"c2","cariAd":"Fatma Gül Er","ihlalGunu":"2026-09-30","nakitToplamTl":11250.50,"hadTl":30000,"bildirimSonGunu":"2026-10-07","durum":"ACIK","kalanIsGunu":5,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false,"islemToplamTl":41250.50,"kaynakGunu":null},
          {"id":"g2","tur":"TAKSIT","yon":"TAHSILAT","cariId":"c1","cariAd":"Ayşe Demir","ihlalGunu":"2026-09-30","nakitToplamTl":20000,"hadTl":30000,"bildirimSonGunu":"2026-10-07","durum":"ACIK","kalanIsGunu":5,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false,"islemToplamTl":null,"kaynakGunu":"2026-09-27"},
          {"id":"g3","tur":"GUNLUK_KUMULATIF","yon":"ODEME","cariId":"c9","cariAd":"Hurda Satıcı","ihlalGunu":"2026-09-30","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-07","durum":"ACIK","kalanIsGunu":5,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false,"islemToplamTl":null,"kaynakGunu":null}
        ],"kapanmamisSayisi":3,"sinir":200,"dahaVar":false,"bayramEksik":false}
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Satir_gerekcesi_BASIT_ve_PRO_modda_AYNI_ve_fis_toplami_yazili(bool profesyonel)
    {
        ihlalYaniti = GerekceIhlal;
        Oturum();
        var mod = profesyonel ? ArayuzModuServisi.Profesyonel : ArayuzModuServisi.Basit;
        Services.GetRequiredService<ArayuzModuServisi>().Uygula(mod, mod);
        var e = Render<Kuyumcu.Web.Bilesenler.NakitSiniriAsimlari>();
        e.WaitForElement("#nsa-liste");
        var islem = e.Find("#nsa-neden-g1").TextContent;
        Assert.Contains("41.250,50", islem, StringComparison.Ordinal);      // fiş toplamı
        Assert.Contains("11.250,50", islem, StringComparison.Ordinal);      // nakit kısmı
        Assert.Contains("30.000", islem, StringComparison.Ordinal);         // sınır
        var taksit = e.Find("#nsa-neden-g2").TextContent;
        Assert.Contains("27.09.2026", taksit, StringComparison.Ordinal);    // büyük işlemin günü
        Assert.Contains("20.000", taksit, StringComparison.Ordinal);
        Assert.Contains("36.000", e.Find("#nsa-neden-g3").TextContent, StringComparison.Ordinal);
        Assert.Contains("Aynı gün", e.Find("#nsa-neden-g3").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Ekran_basina_TEK_birincil_EN_ACIL_odeme_satirinin_dilekcesi()
    {
        ihlalYaniti = GerekceIhlal;
        var e = Sekme();
        e.WaitForElement("#nsa-liste");
        var birinciller = e.FindAll("button.dugme-birincil");
        var b = Assert.Single(birinciller);
        Assert.Equal("nsa-dilekce-g3", b.Id);                              // ödeme yönü (son günlü) önce
        Assert.StartsWith("nsa-satir-g3", e.Find("#nsa-liste li").Id, StringComparison.Ordinal);
    }

    [Fact]
    public void Uyum_nakit_sekmesinde_MASAK_ozet_kutulari_ve_Yeni_bildirim_YOK_diger_sekmede_VAR()
    {
        ihlalYaniti = GerekceIhlal;
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Uyum>();
        e.WaitForElement("#uy-yeni-bildirim");
        Assert.NotEmpty(e.FindAll(".stat-izgara"));
        e.Find("#uy-sekme-nakit").Click();
        e.WaitForElement("#nsa-liste");
        Assert.Empty(e.FindAll("#uy-yeni-bildirim"));
        Assert.Empty(e.FindAll(".stat-izgara"));
        Assert.Empty(e.FindAll("#sayfa-basi-uyum-eylem"));                // boş eylem satırı (40 px boşluk) çizilmez
        Assert.Single(e.FindAll("button.dugme-birincil"));                  // tek birincil: dilekçe
    }

    [Fact]
    public void Kaynagi_bilinmeyen_nakit_KAPALI_akordeonda_satir_olarak_gorunur_hata_bos_duruma_cevrilmez()
    {
        ihlalYaniti = UcIhlal;
        dogYaniti = """{"satirlar":[{"id":"d1","tarih":"2026-09-30T08:00:00Z","kaynak":"TAHSILAT","cariId":"c1","cariAd":"Ayşe Demir","nakitTl":20000,"dogrulanamayanTl":20000}],"sinir":100,"dahaVar":false,"gunSayisi":90}""";
        var e = Sekme();
        e.WaitForElement("#nsa-dog-ac");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/tevsik/ihlaller/dogrulanamayanlar"));
        Assert.Empty(e.FindAll("#nsa-dog-d1"));                             // kapalı akordeon
        e.Find("#nsa-dog-ac").Click();
        var satir = e.Find("#nsa-dog-tutar-d1").TextContent;
        Assert.Contains("20.000", satir, StringComparison.Ordinal);
        Assert.Contains("kaynağı bilinmeyen", satir, StringComparison.Ordinal);
        Assert.Contains("Ayşe Demir", e.Find("#nsa-dog-d1").TextContent, StringComparison.Ordinal);

        dogYaniti = """{"satirlar":[],"sinir":100,"dahaVar":false,"gunSayisi":90}""";
        var bos = Sekme();
        bos.WaitForElement("#nsa-liste");
        Assert.Empty(bos.FindAll("#nsa-dog-ac"));                           // yoksa hiç çizilmez (zorunlu önce)
    }

    // ═════════ TB-16 — Panel satırı ═══════════════════════════════════════════

    [Fact]
    public void Panel_SAHIP_ODEME_son_gun_ve_is_gunu_TAHSILAT_musavir_satiri_KRITIK()
    {
        ihlalYaniti = UcIhlal.Replace("\"durum\":\"BILDIRILDI\"", "\"durum\":\"GECERSIZ\"");
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.NakitSiniriPanelSatiri>();
        e.WaitForElement("#nsp-odeme");
        Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/ihlaller?durum=KAPANMAMIS"));
        var odeme = e.Find("#nsp-odeme").TextContent;
        Assert.Contains("son gün 08.10", odeme, StringComparison.Ordinal);
        Assert.Contains("3 iş günü", odeme, StringComparison.Ordinal);
        var tahsil = e.Find("#nsp-tahsilat").TextContent;
        Assert.Contains("mali müşavirinize danışın", tahsil, StringComparison.Ordinal);
        Assert.DoesNotContain("iş günü", tahsil, StringComparison.Ordinal);
        Assert.Equal("/uyum?sekme=nakit", e.Find("#nsp-odeme-git").GetAttribute("href"));
        Assert.Contains("uyari-kirmizi", e.Find("#nsp").GetAttribute("class"), StringComparison.Ordinal);   // KRİTİK
    }

    [Fact]
    public void Panel_TEZGAHTAR_ve_MUDUR_istek_ATMAZ_satir_cizmez()
    {
        ihlalYaniti = UcIhlal;
        foreach (var rol in new[] { "TEZGAHTAR", "MUDUR" })
        {
            istekler.Clear();
            Oturum(rol);
            var e = Render<Kuyumcu.Web.Bilesenler.NakitSiniriPanelSatiri>();
            Assert.Empty(e.FindAll("#nsp"));
            Assert.DoesNotContain(istekler, i => i.Yol.Contains("/tevsik/ihlaller"));
        }
    }

    [Fact]
    public void Panel_hata_AŞIM_YOK_a_cevrilmez_hata_bandi()
    {
        ihlalDurumKodu = 500;
        Oturum("MUHASEBE");
        var e = Render<Kuyumcu.Web.Bilesenler.NakitSiniriPanelSatiri>();
        e.WaitForElement("#nsp-hata");
    }
}
