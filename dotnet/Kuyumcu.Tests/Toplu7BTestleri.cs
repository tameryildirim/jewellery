using System.Net;
using System.Text;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-7B — para ekranları görünüm kalanları (GOC-A + YASAL-PARAM-2 kapanış devri)
//   T7B01 aynı hata metni: FormDurumu.SunucuHatasiTek (alan-bağlı → kısa genel cümle; alan dışı → tam)
//   T7B02 ekran: Yeni Kasa sunucu reddi → alan altı TAM tek kez, bant kısa genel cümle
//   T7B03 ekran: Yeni Kasa'da Kaydet altbilgide, İptal'den SONRA
//   T7B04 ortak CSS: hata anında gri ipucu her kapta gizli
//   T7B05 CokSatir: value öznitelikli (kayıttan sonra eski metin kalmaz)
//   T7B06 SayfaBasi AciklamaKatli: KAPALI (i) bilgi satırı; kapalıyken eski görünüm aynen
//   T7B07 Cekmece Alt: altbilgide İptal'den sonra çizilir
//   T7B08 sözlük: jargon/hukuk atfı yok (TR), EN dolu
//   T7B09 Yeni fatura: üç hazır seçim grubu KAPALI "Fatura ayrıntıları" satırında
//   T7B10 Yasal düzeltme: kayıttan sonra gerekçe boş + sonuç bandı görünür bölgeye gelir (JS)
//   T7B11 "Hurda alımı gir" doğru hedefe (HURDA_ALIS) gider
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Toplu7BSafTestleri
{
    private static readonly IReadOnlyDictionary<string, string> Esleme =
        new Dictionary<string, string> { ["Ad"] = "kasa-ad" };

    private static DilServisi YeniDil()
    {
        var c = new BunitContext();
        return new DilServisi(c.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());
    }

    private static ApiHata Ret(string mesaj, params string[] alanlar) => new(400, mesaj)
    {
        Alanlar = alanlar.ToDictionary(a => a, _ => new[] { mesaj }),
    };

    [Fact]
    public void T7B01a_Alana_bagli_retde_bant_KISA_genel_cumle_alan_altina_tam_cumle()
    {
        var dil = YeniDil();
        var form = new FormDurumu();
        var tam = "Bu adda bir kasa zaten var, başka bir ad yazın.";
        var bant = form.SunucuHatasiTek(Ret(tam, "ad"), dil, Esleme);
        Assert.Equal(Sozlukler.Tr.Urun.FormAlanHatasiGenel, bant);
        Assert.DoesNotContain(tam, bant);
        Assert.Equal(tam, form.Mesaj("kasa-ad"));
    }

    [Fact]
    public void T7B01b_Alan_disi_retde_bant_TAM_cumleyi_tasir_hata_yutulmaz()
    {
        var dil = YeniDil();
        var form = new FormDurumu();
        var bant = form.SunucuHatasiTek(new ApiHata(409, "Kasa başka bir kayıtta kullanılıyor."), dil, Esleme);
        Assert.Contains("Kasa başka bir kayıtta kullanılıyor.", bant);
        Assert.NotEqual(Sozlukler.Tr.Urun.FormAlanHatasiGenel, bant);
        // Ekranda çizilmeyen alan (eşlemede yok) → mesaj bantta kalır.
        var bant2 = new FormDurumu().SunucuHatasiTek(Ret("Döviz geçersiz.", "paraBirimi"), dil, Esleme);
        Assert.Contains("Döviz geçersiz.", bant2);
    }

    [Fact]
    public void T7B12_Kucuk_dugme_ana_alanda_ve_cekmecede_TEK_olcek_44_ve_kontrol_yazisi()
    {
        var css = File.ReadAllText(Yol("Kuyumcu.Web/wwwroot/css/olcek.css"));
        const string secici = "html[data-mod] .cekmece .dugme-kucuk:not(td *):not(th *):not(.satir-menu *) {";
        var i = css.IndexOf(secici, StringComparison.Ordinal);
        Assert.True(i > 0, "TOPLU-7B tek ölçek kuralı yok");
        var blok = css[i..css.IndexOf('}', i)];
        foreach (var s in new[] { ".ana-icerik .btn-kucuk", ".ana-icerik .dugme-kucuk", ".cekmece .btn-kucuk" })
            Assert.Contains("html[data-mod] " + s + ":not(td *):not(th *):not(.satir-menu *),", css);
        Assert.Contains("--dugme-yuksekligi-kucuk: var(--dugme-yuksekligi);", blok);
        Assert.Contains("font-size: var(--yazi-kontrol);", blok);
        Assert.Contains("html[data-mod] .cekmece .btn,\nhtml[data-mod] .cekmece .dugme { font-size: var(--yazi-kontrol); }", css);
    }

    [Fact]
    public void T7B04_Gri_ipucu_hata_aninda_HER_kapta_gizli_tek_ortak_kural()
    {
        var css = File.ReadAllText(Yol("Kuyumcu.Web/wwwroot/css/app.css"));
        Assert.Contains("\n.alan-hatali .alan-ipucu { display: none; }", css);
        // Modal kuralı (URUN-FORM-2) yerinde.
        Assert.Contains(".modal-panel .alan-hatali .alan-ipucu { display: none; }", css);
    }

    [Fact]
    public void T7B08_Sozluk_para_ekranlarinda_hukuk_ve_teknik_jargon_YOK_EN_dolu()
    {
        var tr = Sozlukler.Tr; var en = Sozlukler.En;
        string[] yasak = ["md.234", "tevsik", "mevzuat değil", "anlık görüntü", "mutabakat", "VUK"];
        var metinler = new[]
        {
            tr.Pusula.Aciklama, tr.Pusula.IptalEt, tr.Pusula.IptalOnay,
            tr.Kur.Aciklama, tr.Vezne.DefterKapilari, tr.Hurda.TahlilFarki, tr.Hurda.TahlilFarkiNotu,
            tr.Islem.SaticiKimligiNot, tr.Islem.SaticiKimligiEksik,
            KesifP1Sozlugu.Tr.PosKomisyonYuzdeAciklama,
            YasalDuzeltSozlugu.Tr.Aciklama,
        };
        foreach (var m in metinler)
            foreach (var y in yasak)
                Assert.DoesNotContain(y, m, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Faturası olmayan", tr.Pusula.Aciklama);
        Assert.DoesNotContain("VUK", en.Pusula.Aciklama + en.Pusula.IptalEt + en.Pusula.IptalOnay + en.Islem.SaticiKimligiEksik);
        Assert.NotEqual(tr.Pusula.Aciklama, en.Pusula.Aciklama);
        Assert.NotEqual(tr.Hurda.TahlilFarki, en.Hurda.TahlilFarki);
        Assert.NotEqual(Toplu7BSozlugu.Tr.FaturaAyrintiBaslik, Toplu7BSozlugu.En.FaturaAyrintiBaslik);
        Assert.NotEqual(YasalDuzeltSozlugu.Tr.Aciklama, YasalDuzeltSozlugu.En.Aciklama);
        // Yasal düzeltme açıklaması TEK cümle (paragraf değil).
        Assert.Equal(1, YasalDuzeltSozlugu.Tr.Aciklama.Count(c => c == '.'));
    }

    private static string Yol(string goreli)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return Path.Combine(d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı"), goreli);
    }
}

/// <summary>Ortak bileşenler: CokSatir · SayfaBasi · Cekmece.</summary>
public sealed class Toplu7BBilesenTestleri : BunitContext
{
    public Toplu7BBilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void T7B05_CokSatir_value_oznitelikli_bagli_deger_degisince_kutu_degisir()
    {
        var c = Render<CokSatir>(p => p
            .Add(x => x.ElemanId, "t7-not").Add(x => x.Etiket, "Not").Add(x => x.Deger, "eski metin"));
        var ta = c.Find("#t7-not");
        Assert.Equal("eski metin", ta.GetAttribute("value"));   // value özniteliği: tarayıcı "value"su kirlense de kutu bağlı değere çekilir
        c.Render(p => p.Add(x => x.ElemanId, "t7-not").Add(x => x.Etiket, "Not").Add(x => x.Deger, (string?)null));
        Assert.True(string.IsNullOrEmpty(c.Find("#t7-not").GetAttribute("value")));
        Assert.Equal("", c.Find("#t7-not").TextContent);
        Assert.Equal("0 / 500", c.Find("#t7-not-sayac").TextContent.Trim());
    }

    [Fact]
    public void T7B06_SayfaBasi_AciklamaKatli_KAPALI_bilgi_satiri_varsayilan_eski_gorunum_aynen()
    {
        var katli = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "deneme").Add(x => x.Baslik, "Deneme").Add(x => x.Aciklama, "Bir cümlelik açıklama.")
            .Add(x => x.AciklamaKatli, true));
        var d = katli.Find("details#sayfa-basi-deneme-bilgi");
        Assert.False(d.HasAttribute("open"));
        Assert.Equal("Bir cümlelik açıklama.", katli.Find("#sayfa-basi-deneme-aciklama").TextContent.Trim());
        Assert.Contains("details", katli.Find("#sayfa-basi-deneme-aciklama").ParentElement!.TagName.ToLowerInvariant());

        var duz = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "deneme").Add(x => x.Baslik, "Deneme").Add(x => x.Aciklama, "Bir cümlelik açıklama."));
        Assert.Empty(duz.FindAll("details"));
        Assert.Equal("p", duz.Find("#sayfa-basi-deneme-aciklama").TagName.ToLowerInvariant());
    }

    [Fact]
    public void T7B07_Cekmece_Alt_altbilgide_Iptal_ten_SONRA_cizilir()
    {
        var c = Render<Cekmece>(p => p
            .Add(x => x.Acik, true).Add(x => x.Varlik, "deneme").Add(x => x.Baslik, "Deneme").Add(x => x.KaydetGoster, false)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"govde-p\">gövde</p>")))
            .Add(x => x.Alt, (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"t7-kaydet\" type=\"button\">Kaydet</button>"))));
        var alt = c.Find(".cekmece-alt");
        var dugmeler = alt.QuerySelectorAll("button").Select(x => x.Id).ToList();
        Assert.Equal(["cekmece-deneme-iptal", "t7-kaydet"], dugmeler);   // ikincil önce, birincil sonda
        Assert.Empty(c.Find(".cekmece-govde").QuerySelectorAll("#t7-kaydet"));
    }
}

/// <summary>Yeni Kasa: sunucu reddi + altbilgi sırası.</summary>
public sealed class Toplu7BKasalarEkranTestleri : BunitContext
{
    private const string Tam = "Bu adda bir kasa zaten var, başka bir ad yazın.";

    public Toplu7BKasalarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/kasalar"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = Tam,
                        alanlar = new Dictionary<string, string[]> { ["ad"] = [Tam] },
                        statusCode = 400,
                    }), Encoding.UTF8, "application/json"),
                });
            var g = yol switch
            {
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => "[]",
                var y when y.EndsWith("/kasalar") => "[]",
                var y when y.Contains("/kur/guncel") => """{"id":"k1","altinHasFiyati":6800,"gramAltinFiyati":6860}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.WaitForElement("#kasa-yeni");
        e.Find("#kasa-yeni").Click();
        e.WaitForElement("#kasa-ad");
        return e;
    }

    [Fact]
    public void T7B02_Yeni_kasa_sunucu_reddi_alan_altinda_TAM_tek_kez_bant_kisa_genel_cumle()
    {
        var e = Ac();
        e.Find("#kasa-ad").Input("Ret kasa");
        e.Find("#kasa-olustur").Click();
        e.WaitForAssertion(() => Assert.Contains(Tam, e.Find("#alan-hata-kasa-ad").TextContent));
        Assert.Equal(1, e.Markup.Split(Tam).Length - 1);   // tam cümle sayfada TEK kez
        Assert.Contains(Sozlukler.Tr.Urun.FormAlanHatasiGenel, e.Markup);
    }

    [Fact]
    public void T7B03_Yeni_kasa_Kaydet_altbilgide_Iptal_ten_sonra_govdede_DEGIL()
    {
        var e = Ac();
        var alt = e.Find(".cekmece-alt");
        Assert.Equal(["cekmece-kasa-yeni-iptal", "kasa-olustur"], alt.QuerySelectorAll("button").Select(x => x.Id).ToList());
        Assert.Empty(e.Find(".cekmece-govde").QuerySelectorAll("#kasa-olustur"));
    }
}

/// <summary>Faturalar: açılışta yalnız zorunlu.</summary>
public sealed class Toplu7BFaturaEkranTestleri : BunitContext
{
    public Toplu7BFaturaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("/ozel-matrah-ayari") =>
                    """{"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,"kaynakBeyani":"x","maliMusavirTeyidi":false}""",
                var y when y.EndsWith("/faturalar") => "[]",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.Contains("/urunler") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void T7B09_Yeni_fatura_acilista_uc_hazir_secim_grubu_KAPALI_ozet_satirinda()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-yeni");
        e.Find("#fk-yeni").Click();
        e.WaitForElement("#fatura-cari");
        var d = e.Find("details#fatura-ayrinti");
        Assert.False(d.HasAttribute("open"));                       // açılışta kapalı: yalnız müşteri + toplam görünür
        Assert.Equal($"{Toplu7BSozlugu.Tr.FaturaAyrintiBaslik}: Satış · Standart · %20", e.Find("#fatura-ayrinti-ozet").TextContent.Trim());
        // Gruplar DOM'da ve kimlikleri aynı (mevcut testler/ekran okuyucu); katlı satırın İÇİNDE.
        foreach (var id in new[] { "fatura-tur", "fatura-kdvturu", "fatura-kdv" })
            Assert.Single(d.QuerySelectorAll($"#{id}"));
        // Zorunlu iki alan (müşteri, toplam) katlının DIŞINDA.
        Assert.Empty(d.QuerySelectorAll("#fatura-cari, #fatura-toplam"));
    }
}

/// <summary>Yasal düzeltme formu: kayıttan sonra gerekçe boş, sonuç bandı görünür bölgeye.</summary>
public sealed class Toplu7BYasalEkranTestleri : BunitContext
{
    private const string Liste = """
        {"tarih":"2026-10-08","bugun":"2026-10-08","bayramEksik":false,"yonetimAcik":true,"tatiller":[],
         "satirlar":[
          {"id":"w1","anahtar":"MASAK_KIMLIK_ESIGI_TL","degerSayi":250000,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-10-08","bitis":null,"dayanak":"x","kaynakUrl":"https://www.resmigazete.gov.tr/x.pdf",
           "teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true,"esikTuru":"VE_USTU","esikTutari":250000,
           "resmiGazeteTarihi":"2026-10-08","resmiGazeteSayisi":"33400","baslangicSaati":"12:05","ayniGunDuzeltilebilir":true}]}
        """;
    private const string Onizleme = """
        {"id":"w1","anahtar":"MASAK_KIMLIK_ESIGI_TL","birim":"TL","yanlisDeger":250000,"yeniDeger":260000,
         "oncekiDeger":370000,"yanlisSaati":"12:05:10","onayKodu":"ABCDEF0123456789","buyukDegisim":false}
        """;
    private const string Sonuc = """
        {"id":"c1","anahtar":"MASAK_KIMLIK_ESIGI_TL","birim":"TL","degerSayi":260000,"yanlisDeger":250000,
         "yanlisSaati":"12:05:10","duzeltmeSaati":"12:40:02","duzeltilenSatirId":"w1"}
        """;

    public Toplu7BYasalEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var g = yol switch
            {
                var y when y.EndsWith("/duzeltme/onizleme") => Onizleme,
                var y when y.EndsWith("/duzeltme") => Sonuc,
                var y when y.Contains("/yasal-parametreler/gecmis") => """{"anahtar":"MASAK_KIMLIK_ESIGI_TL","satirlar":[]}""",
                var y when y.Contains("/yasal-parametreler") => Liste,
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(yol.EndsWith("/duzeltme") ? HttpStatusCode.Created : HttpStatusCode.OK)
            { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void T7B10_Duzeltme_kaydindan_sonra_gerekce_kutusu_BOS_sayac_0_ve_sonuc_bandi_gorunur_bolgeye_alinir()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/yasal-sinirlar?yonetim=1");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForElement("#ys-liste");
        e.Find("#ysy-dz-ac").Click();
        e.Find("#ysy-dz-deger").Input("260000");
        e.Find("#ysy-dz-gerekce").Input("RG rakamı yanlış yazılmıştı, düzeltiliyor (test).");
        e.Find("#ysy-dz-anahtar").Input("gizli-test-anahtari-gizli-test-anahtari");
        e.Find("#ysy-dz-operator").Input("Ramazan");
        e.Find("#ysy-dz-onizle").Click();
        e.WaitForElement("#ysy-dz-onay");
        e.Find("#ysy-dz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("Düzeltildi", e.Find("#ysy-basari").TextContent));

        // Kutu bağlı değere çekilir: "value" boş, metin düğümü yok, sayaç 0 / 1000.
        var kutu = e.Find("#ysy-dz-gerekce");
        Assert.True(string.IsNullOrEmpty(kutu.GetAttribute("value")));
        Assert.Equal("", kutu.TextContent);
        Assert.Equal("0 / 1000", e.Find("#ysy-dz-gerekce-sayac").TextContent.Trim());

        // Sonuç bandı görünür bölgeye alınır (ortak kuyumcuOdak mekanizması, band kimliğiyle).
        e.WaitForAssertion(() => Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "kuyumcuOdak.alana" && i.Arguments.Count > 0 && (string?)i.Arguments[0] == "ysy-basari"));
    }
}

/// <summary>Gider pusulaları "Hurda alımı gir" → Hurda alış akışı.</summary>
public sealed class Toplu7BPusulaYonlendirmeTestleri : BunitContext
{
    public Toplu7BPusulaYonlendirmeTestleri()
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
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") =>
                    """{"sure":{"gun":7,"dayanak":"x"},"gecikmisSayisi":0,"yaklasanSayisi":0,"gecikmis":[],"yaklasan":[],"uyarilar":[]}""",
                var y when y.EndsWith("/gider-pusulalari") => "[]",
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void T7B11_Hurda_alimi_gir_satis_ekranina_degil_HURDA_ALIS_akisina_gider()
    {
        var e = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        e.WaitForElement("#gp-hurda-alimi");
        e.Find("#gp-hurda-alimi").Click();
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/islemler?tur=HURDA_ALIS", nav.Uri);
        Assert.Equal("/islemler?tur=HURDA_ALIS", Kuyumcu.Web.Pages.GiderPusulalari.HurdaAlimiAdresi);
        // Hedef ekran bu sorguyu GERÇEKTEN okur (düz /islemler "Satış" açardı).
        Assert.Equal("HURDA_ALIS", DerinBaglanti.Oku(nav.Uri, "tur"));
    }
}
