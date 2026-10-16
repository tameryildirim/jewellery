using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A tur 2 — tasarım hakemi KRİTİK bulguları:
//   (1) "Emanet al", "Yeni iş", "Yeni liste"/"Düzenle", "Sayım başlat" Yıldız görünümünden ESKİ tema formuna
//       düşüyordu → artık Yıldız form kalıbında (YildizForm · tuval Y_Gidervevirman) açılır; özgün ekran
//       (data-yz-ozgun) çizilmez, eski .btn-birincil yok, kayıt MEVCUT geri çağrıyla aynı gövdeyi yollar.
//   (2) Sayım "Barkod okut" eski ekrana geçiyordu → okutma alanları Yıldız sayım yüzünde; "Sayımı bitir"
//       hareketli satır kararını aynı yüzde sorar; "+ N ürün daha" listeyi yerinde açar.
// Görsel eşitliğin kanıtı gerçek Chrome ölçümüdür (rapor § tur 2); bu sınıf DAVRANIŞ sözleşmesini kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz5AFormTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string SayimSatirlari = """
        [{"urunId":"u1","kod":"K1","ad":"22 Ayar Yarım Altın","barkod":"8691","birim":"ADET","sistemAdet":3,"sayilanAdet":2,"guncelAdet":3,"fark":-1,"oturumdaHareketVar":true},
         {"urunId":"u2","kod":"K2","ad":"925 Gümüş Kolye","barkod":"8692","birim":"ADET","sistemAdet":3,"sayilanAdet":null,"guncelAdet":3,"fark":null,"oturumdaHareketVar":false},
         {"urunId":"u3","kod":"K3","ad":"14 Ayar Bilezik","barkod":"8693","birim":"ADET","sistemAdet":1,"sayilanAdet":null,"guncelAdet":1,"fark":null,"oturumdaHareketVar":false},
         {"urunId":"u4","kod":"K4","ad":"24 Ayar Gram Altın","barkod":"8694","birim":"ADET","sistemAdet":2,"sayilanAdet":2,"guncelAdet":2,"fark":0,"oturumdaHareketVar":false},
         {"urunId":"u5","kod":"K5","ad":"22 Ayar Künye","barkod":"8695","birim":"ADET","sistemAdet":2,"sayilanAdet":2,"guncelAdet":2,"fark":0,"oturumdaHareketVar":false},
         {"urunId":"u6","kod":"K6","ad":"22 Ayar Tam Altın","barkod":"8696","birim":"ADET","sistemAdet":1,"sayilanAdet":1,"guncelAdet":1,"fark":0,"oturumdaHareketVar":false}]
        """;

    private static string SayimDetay(string durum) =>
        $$"""{"id":"sy1","ad":"Vitrin sayımı","subeId":"s1","kapsamTur":"TUMU","durum":"{{durum}}","toplamSatir":6,"sayilan":4,"sayilmayan":2,"farkli":1,"eksikAdet":1,"fazlaAdet":0,"netFarkHas":-1.5,"hareketli":1,"satirlar":{{SayimSatirlari}},"acildi":"2026-10-10T14:05:00Z"}""";

    private sealed class SahteApi(Yildiz5AFormTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var post = istek.Method == HttpMethod.Post;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/emanet/giris") => """{"id":"h9"}""",
                var v when v.Contains("/emanet/") => """{"cariId":"c1","mevcutHasGramBakiye":0,"hareketler":[]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Selin Ergüven","emanetteHas":0}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var v when v.Contains("/siparisler") && post => """{"id":"sp1","no":"S-2026-000009"}""",
                var v when v.Contains("/siparisler") => """{"limit":500,"azamiLimit":500,"toplam":0,"dahaFazlasiVar":false,"gecikenSayisi":0,"siparisler":[]}""",
                var v when v.Contains("/fiyat/listeler") && post => """{"id":"l2"}""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"l1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":8,"iscilikCarpanYuzde":6,"varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/kampanyalar") => "[]",
                var v when v.Contains("/urun-kategorileri") => "[]",
                var v when v.Contains("/reyonlar") => "[]",
                var v when v.Contains("/urunler") => "[]",
                var v when v.Contains("/sayim/sy1/kapat") => $$"""{"uygulanan":1,"atlanan":0,"detay":{{SayimDetay("KAPANDI")}}}""",
                var v when v.Contains("/sayim/sy1") => SayimDetay("ACIK"),
                var v when v.Contains("/sayim") && post => SayimDetay("ACIK"),
                var v when v.Contains("/sayim") => t.sayimAcik ? """[{"id":"sy1","ad":"Vitrin sayımı","kapsamTur":"TUMU","durum":"ACIK"}]""" : "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private bool sayimAcik = true;

    public Yildiz5AFormTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private void Hazirla(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", mod, mod));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
    }

    private string? SonPost(string yolParcasi)
    {
        lock (istekler) return istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.Contains(yolParcasi)).Govde;
    }

    /// <summary>Yıldız form kalıbı açık, eski ekran yok: data-yz-ozgun 0, eski .btn-birincil 0, eski .kart 0, tek .yz-b1.</summary>
    private static void YildizFormuAcik<T>(IRenderedComponent<T> e, string kok) where T : Microsoft.AspNetCore.Components.IComponent
    {
        Assert.NotNull(e.Find($"#{kok}.yz-form"));
        Assert.Empty(e.FindAll("[data-yz-ozgun]"));
        Assert.Empty(e.FindAll(".btn-birincil"));
        Assert.Empty(e.FindAll(".kart"));
        Assert.Single(e.FindAll(".yz-b1"));
        Assert.Empty(e.FindAll("[style]"));
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PRO")]
    public void Y5A_F01_emanet_al_yildiz_formunda_musteri_alan_altinda_kayit_ayni_govde(string mod)
    {
        Hazirla(mod);
        var e = Render<Kuyumcu.Web.Pages.Emanet>();
        e.WaitForElement("#yildiz-emanet-al", TimeSpan.FromSeconds(10)).Click();

        YildizFormuAcik(e, "yildiz-emanet-form");
        Assert.NotNull(e.Find("#e-cari"));
        Assert.NotNull(e.Find("#e-ayar"));
        Assert.NotNull(e.Find("#e-gram"));
        Assert.Empty(e.FindAll("#e-tas"));          // isteğe bağlı "+ Ayrıntı ekle" arkasında
        e.Find("#yildiz-emanet-form-ekle").Click();
        Assert.NotNull(e.Find("#e-tas"));
        Assert.NotNull(e.Find("#e-aciklama"));

        // Müşteri seçilmeden Kaydet: istek YOK, sebep alan altında (sessiz etkisizlik yok).
        e.Find("#e-giris-kaydet").Click();
        Assert.Null(SonPost("/emanet/giris"));
        Assert.Contains(OnayKilitSozlugu.Al("tr").EmanetMusteriSecilmedi, e.Markup);

        SeciciYardim.Sec(e, "e-cari", "c1");
        e.Find("#e-gram").Input("2,5");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/emanet/giris")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(SonPost("/emanet/giris")!).RootElement;
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
        Assert.Equal("K22", g.GetProperty("ayarId").GetString());
        Assert.Equal(2.5m, g.GetProperty("gramAgirlik").GetDecimal());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
        // Kayıttan sonra liste kalıbına dönülür.
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".yz-form")), TimeSpan.FromSeconds(10));
        Assert.NotNull(e.Find("#yildiz-emanet"));
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PRO")]
    public void Y5A_F02_yeni_is_yildiz_formunda_tur_secimi_zorunlular_ve_ayni_govde(string mod)
    {
        Hazirla(mod);
        var e = Render<Kuyumcu.Web.Pages.Siparisler>();
        e.WaitForElement("#sip-yeni-ac", TimeSpan.FromSeconds(10)).Click();

        YildizFormuAcik(e, "yildiz-siparis-form");
        Assert.NotNull(e.Find("#sip-f-cari"));
        Assert.NotNull(e.Find("#sip-f-kalem-tanim-0"));
        Assert.NotNull(e.Find("#sip-f-sube-sabit"));      // tek şube: salt okunur (UX-S22)
        Assert.Empty(e.FindAll("#sip-f-gun"));            // isteğe bağlı "+ Ayrıntı ekle" arkasında
        Assert.Empty(e.FindAll("#sip-f-kalem-gram-0"));
        Assert.Empty(e.FindAll(".zo-detay-ac"));          // eski "Daha fazla" tetiği yok

        // Müşteri yok → alan altı mesaj, POST yok.
        e.Find("#sip-f-kaydet").Click();
        Assert.Null(SonPost("/siparisler"));

        e.Find("#yildiz-siparis-form-secim-TAMIR").Click();
        Assert.Equal("true", e.Find("#yildiz-siparis-form-secim-TAMIR").GetAttribute("aria-pressed"));
        Assert.Contains(Services.GetRequiredService<DilServisi>().T.Siparis.EmanetUyarisi, e.Find("#yildiz-siparis-form-not").TextContent);
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        e.Find("#sip-f-kalem-tanim-0").Input("Küpe kilidi tamiri");
        e.Find("#yildiz-siparis-form-ekle").Click();
        Assert.NotNull(e.Find("#sip-f-gun"));
        e.Find("#sip-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/siparisler")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(SonPost("/siparisler")!).RootElement;
        Assert.Equal("TAMIR", g.GetProperty("tur").GetString());
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
        Assert.Equal("Küpe kilidi tamiri", g.GetProperty("kalemler")[0].GetProperty("tanim").GetString());
        e.WaitForAssertion(() => Assert.Contains("S-2026-000009", e.Markup), TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll(".yz-form"));
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PRO")]
    public void Y5A_F03_yeni_liste_ve_duzenle_yildiz_formunda_tuval_adlari_ve_ayni_govde(string mod)
    {
        Hazirla(mod);
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement("#fl-yeni", TimeSpan.FromSeconds(10)).Click();

        YildizFormuAcik(e, "yildiz-fiyat-form");
        Assert.Contains(YildizFiyatSozlugu.Kar("tr"), e.Find("#fl-marj-sar").TextContent);
        Assert.Contains(YildizFiyatSozlugu.Iscilik("tr"), e.Find("#fl-carpan-sar").TextContent);
        Assert.Empty(e.FindAll("#fl-varsayilan"));      // "+ Ayrıntı ekle" arkasında
        Assert.Empty(e.FindAll("#km-ad"));               // kampanyalar yalnız "Tüm ayrıntılar"da
        e.Find("#yildiz-fiyat-form-ekle").Click();
        Assert.NotNull(e.Find("#fl-varsayilan"));

        e.Find("#yildiz-fiyat-form-secim-TOPTAN").Click();
        e.Find("#fl-ad").Input("Toptan");
        e.Find("#fl-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/fiyat/listeler")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(SonPost("/fiyat/listeler")!).RootElement;
        Assert.Equal("Toptan", g.GetProperty("ad").GetString());
        Assert.Equal("TOPTAN", g.GetProperty("tur").GetString());
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".yz-form")), TimeSpan.FromSeconds(10));

        // Düzenle de aynı Yıldız formunu açar (eski ızgaraya düşmez); başlık listenin adı.
        e.Find("#fl-duzenle-l1").Click();
        YildizFormuAcik(e, "yildiz-fiyat-form");
        Assert.Equal("Perakende", e.Find("#yildiz-fiyat-form-baslik").TextContent.Trim());
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PRO")]
    public void Y5A_F04_sayim_barkod_okut_yildiz_yuzunde_say_ve_bitir_ayni_yol(string mod)
    {
        Hazirla(mod);
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        e.WaitForElement("#yildiz-sayim-rakam", TimeSpan.FromSeconds(10));
        // Hareketli satır kararı Yıldız yüzünde sorulur (eski ekrana geçmeden).
        Assert.NotNull(e.Find("#yildiz-sayim-okut #sy-hareketli-onay"));
        Assert.Empty(e.FindAll("#sy-barkod"));

        e.Find("#yildiz-sayim-b2").Click();                       // "Barkod okut"
        Assert.Empty(e.FindAll("[data-yz-ozgun]"));
        Assert.Empty(e.FindAll(".btn-birincil"));
        Assert.Single(e.FindAll(".yz-b1"));
        Assert.NotNull(e.Find("#yildiz-sayim-okut #sy-barkod"));
        Assert.NotNull(e.Find("#yildiz-sayim-okut #sy-adet"));
        Assert.Equal(YildizForm5ASozlugu.Say("tr"), e.Find("#yildiz-sayim-b2").TextContent.Trim());

        e.Find("#sy-barkod").Input("8692");
        e.Find("#sy-adet").Input("3");
        e.Find("#yildiz-sayim-b2").Click();                       // "Say" = MEVCUT Say
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/sayim/sy1/say")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(SonPost("/sayim/sy1/say")!).RootElement;
        Assert.Equal("8692", g.GetProperty("barkod").GetString());
        Assert.Equal(3m, g.GetProperty("sayilanAdet").GetDecimal());

        // "+ N ürün daha" listeyi Yıldız kalıbında açar (6 satır), eski ekrana geçmez.
        e.Find("#yildiz-sayim-ayrinti").Click();
        Assert.Equal(6, e.FindAll("#yildiz-sayim-liste .yz-liste-sat:not(.yz-liste-sat--bas)").Count);
        Assert.Empty(e.FindAll("[data-yz-ozgun]"));

        // "Sayımı bitir" eski ekrana geçmeden MEVCUT Kapat'ı çağırır; hareketli onayı gövdede.
        e.Find("#sy-hareketli-onay").Change(true);
        e.Find("#yildiz-sayim-b1").Click();
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/sayim/sy1/kapat")), TimeSpan.FromSeconds(10));
        Assert.True(JsonDocument.Parse(SonPost("/sayim/sy1/kapat")!).RootElement.GetProperty("hareketliSatirlariUygula").GetBoolean());
        Assert.Empty(e.FindAll("[data-yz-ozgun]"));
    }

    [Fact]
    public void Y5A_F05_sayim_baslat_yildiz_formunda_ve_ayni_govde()
    {
        sayimAcik = false;
        Hazirla("BASIT");
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        // PAKET P-SAYIM (ekran denetimi 10.10 Y10): tek şubede "Sayım başlat" FORM SORMAZ — ad tarih-saatle otomatik,
        // kapsam "tüm ürünler", MEVCUT Ac (POST /sayim) doğrudan; okutma alanı açılır. Reyon/kategoriyle sınırlı sayım
        // ikincil seçenekle aynı Yıldız formunda açılır (PSayimEkranTestleri.PS_B7). Boş ad kapısı UX1C2'de ölçülür.
        e.WaitForElement("#yildiz-sayim-b1", TimeSpan.FromSeconds(10)).Click();      // "Sayım başlat"
        e.WaitForAssertion(() => Assert.NotNull(SonPost("/sayim")), TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#yildiz-sayim-form"));
        var g = JsonDocument.Parse(SonPost("/sayim")!).RootElement;
        Assert.Matches(@"^Sayım \d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", g.GetProperty("ad").GetString());
        Assert.Equal("s1", g.GetProperty("subeId").GetString());
        e.WaitForElement("#yildiz-sayim-rakam", TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("[data-yz-ozgun]"));
    }

    [Fact]
    public void Y5A_F06_sayim_ayrinti_basligi_dolgu_icinde_ve_ikinci_birincil_yok_css()
    {
        var c = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var bas = c.IndexOf("/* YILDIZ-5A */", StringComparison.Ordinal);
        var son = c.IndexOf("/* YILDIZ-5A sonu */", StringComparison.Ordinal);
        Assert.True(bas >= 0 && son > bas);
        var blok = c[bas..son];
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz-sayim.yz-liste--ayrinti .yz-liste-ana { display: flex; flex-direction: column; gap: 32px; padding: 48px 64px 32px; }", blok);
        Assert.Contains("html:root[data-sema=\"yildiz\"] [data-yz-ozgun=\"sayim\"] #sy-listeye-don { display: none; }", blok);
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz-sayim .yz-sayim-okut .arama-kutu { flex: 0 0 auto; }", blok);
    }
}
