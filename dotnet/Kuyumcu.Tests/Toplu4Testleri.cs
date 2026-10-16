using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-4 — toplu tasarım kuyruğu (TOPLU-2 t2 · ŞUBE-KAPSAM-B t2 · TOPLU-2
// kapanışı · TOPLU-3 t2 kalanları). YALNIZ GÖRÜNÜM (hasımsız paket): her kabul
// maddesi burada bir testle sabitlenir ve mutasyonla kırmızıya döndüğü gösterilir.
//   bUnit : Panel "Kim borçlu?" TL/has ayrı satır · Vitrin ⓘ + PRO rozet/sütun ·
//           Siparişler tek vazgeç / şerit / rozet · Kurulum sayı etiketi · Ürünler
//           sayaç satırı · Kur rozeti kaynak satırında · Yaşlandırma eksi + has ⓘ ·
//           Kullanıcı kartı bakılan şube.
//   kaynak: Hurda açıcı kalıbı · Uyum risk ⓘ · Vade ufuk ⓘ · İşlem defteri rozet.
//   CSS   : toplu4.css kuralları + yükleme sırası + belirteç.
//   sözlük: Toplu4 TR/EN · sentence-case "Yeni x" · bozuk cümle · PRO Kurulum metni.
//   P-37c : sayaç metni ve kapsamı bugünkü İKİ katmanlı davranışı anlatır.
// ═════════════════════════════════════════════════════════════════════════════

internal static class Toplu4Ortak
{
    public static string Dosya(params string[] parca) => Toplu3Ortak.Dosya(parca);

    /// <summary>toplu4.css (yorumsuz).</summary>
    public static string Css() => Regex.Replace(
        Dosya("Kuyumcu.Web", "wwwroot", "css", "toplu4.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    public static string? Blok(string css, string secici) => Toplu3Ortak.Blok(css, secici);
}

/// <summary>Ortak bUnit tabanı: yol parçası → gövde (sıra önemli), bilinmeyen uç "{}".</summary>
public abstract class Toplu4Tabani : BunitContext
{
    protected readonly List<(string Parca, bool Son, string Govde)> yollar = [];
    protected readonly List<(string Metot, string Yol)> istekler = [];

    protected Toplu4Tabani()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    protected void Yol(string parca, string govde, bool son = true) => yollar.Add((parca, son, govde));

    protected void Oturum(string mod, string rol = "SAHIP", string sube = "s1")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, sube, mod, mod));
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => mod == ArayuzModuServisi.Basit);
    }

    private sealed class Sahte(Toplu4Tabani t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (t.istekler) t.istekler.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery));
            var g = "{}";
            foreach (var (parca, son, govde) in t.yollar)
            {
                if (son ? yol.EndsWith(parca, StringComparison.Ordinal) : yol.Contains(parca, StringComparison.Ordinal))
                { g = govde; break; }
            }
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}

// ═══ (1) KRİTİK — Panel "Kim borçlu?" ═══════════════════════════════════════
public sealed class Toplu4PanelTestleri : Toplu4Tabani
{
    public Toplu4PanelTestleri()
    {
        Yol("/raporlar/gunluk", """
            {"tarih":"2026-10-02","satis":{"adet":0,"toplamTl":0,"toplamHas":0},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0,"hareketSayisi":0,"kasalar":[]}
            """);
        Yol("/raporlar/cari-yaslandirma", """
            {"yaslananCariler":[
               {"cariId":"c1","cariAd":"Anadolu Döküm Toptan Kuyumculuk Sanayi ve Ticaret","cariTur":"MUSTERI","tlBakiye":24423.82,"hasGramBakiye":12.3,
                "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"},
               {"cariId":"c2","cariAd":"Tuna Akarsu","cariTur":"MUSTERI","tlBakiye":22096.97,"hasGramBakiye":0,
                "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":5,"dilim":"0-30"}],
             "ozetTl":{"0-30":46520.79,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},
             "toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false}
            """);
        Yol("/raporlar/vade-takibi", """
            {"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}
            """);
        Yol("/bildirimler", """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""", son: false);
        Yol("/raporlar/aylik", """{"gunler":[]}""");
        Yol("/raporlar/islem-defteri", """
            {"baslangic":"2026-10-02","bitis":"2026-10-02","toplamSatir":0,"ofset":0,"limit":5,"dahaFazlasiVar":false,
             "ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},"storno":{"adet":0,"tutarTl":0},"satirlar":[]}
            """, son: false);
        Yol("/raporlar/kar-zarar", """
            {"hasilatTl":0,"maliyetTl":0,"karTl":0,"karMarjiYuzde":0,"hasilatHas":0,"maliyetHas":0,"karHas":0,"satisAdedi":0,
             "maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"VERI_YOK","uyari":null}
            """);
        Yol("/bankalar", """{"hesaplar":[]}""");
        Yol("/kur/kaynaklar", """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""");
        Yol("/tercihler/benim", """{"tercihler":{}}""", son: false);
        Yol("/uyum/", """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""", son: false);
        Yol("/pozisyon", """
            {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":0,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
             "netGram":0,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
             "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T06:00:00Z","donemdenBagimsiz":true}
            """);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Ciz()
    {
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-borclu-tutar-c1", TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T4_Kim_borclu_TL_ve_has_AYRI_satir_parcalari()
    {
        var e = Ciz();
        var parca = e.FindAll("#panel-borclu-tutar-c1 > .panel-tutar-parca");
        Assert.Equal(2, parca.Count);
        Assert.Equal(Bicim.Tl(24423.82m), parca[0].TextContent.Trim());
        Assert.Equal("12,3000 has", parca[1].TextContent.Trim());   // RAPOR-GORUNUM: tek has dili (Bicim.Has)
        // Tek satırlık birleşik metin hücrede YOK; tamamı title'da okunur.
        Assert.DoesNotContain(" · ", e.Find("#panel-borclu-tutar-c1").TextContent, StringComparison.Ordinal);
        Assert.Equal($"{Bicim.Tl(24423.82m)} · 12,3000 has", e.Find("#panel-borclu-tutar-c1").GetAttribute("title"));
        // Yalnız TL'si olan satır tek parça.
        Assert.Single(e.FindAll("#panel-borclu-tutar-c2 > .panel-tutar-parca"));
        // Ad kesilmez/kırpılmaz: tamamı metinde (title'da da).
        Assert.Contains("Sanayi ve Ticaret", e.Find("#panel-borclu-c1 .panel-liste-ad").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Kim_borclu_css_tutar_sutunu_icerik_kadar_dar_kapta_iki_satir()
    {
        var css = Toplu4Ortak.Css();
        var satir = Toplu4Ortak.Blok(css, ".ana-icerik .panel-sade #panel-soru-borclu .panel-liste-satir") ?? "";
        Assert.Contains("minmax(130px, max-content)", satir, StringComparison.Ordinal);
        var tutar = Toplu4Ortak.Blok(css, ".ana-icerik .panel-sade #panel-soru-borclu .panel-liste-tutar") ?? "";
        Assert.Contains("flex-direction: column", tutar, StringComparison.Ordinal);
        var ad = Toplu4Ortak.Blok(css, ".ana-icerik .panel-sade #panel-soru-borclu .panel-liste-ad") ?? "";
        Assert.Contains("word-break: normal", ad, StringComparison.Ordinal);
        var kap = Toplu3Ortak.Medya(css, "@container (max-width: 559px)");
        Assert.Contains("\"ad eylem\" \"durum tutar\"", kap, StringComparison.Ordinal);
        Assert.Contains("minmax(0, 1fr) auto", kap, StringComparison.Ordinal);
    }
}

// ═══ (3) Vitrin panosu ══════════════════════════════════════════════════════
public sealed class Toplu4VitrinTestleri : Toplu4Tabani
{
    public Toplu4VitrinTestleri()
    {
        Yol("/pano/guncel", """
            {"kaynak":"ALTINKAYNAK","zaman":"2026-09-24T08:00:00Z",
             "satirlar":[{"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"}],
             "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false}
            """, son: false);
        Yol("/pano", """
            {"ayarlar":[
               {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":-10.00,"satisMarjTl":20.00,"yuvarlamaAdimi":10,"aktif":true,"ozel":true},
               {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","etiket":null,"sira":1,"alisMarjTl":0,"satisMarjTl":0,"yuvarlamaAdimi":10,"aktif":true,"ozel":false}],
             "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":2}
            """, son: false);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vitrin> Ciz(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        // TOPLU-6: ayar tablosu ortak Katlanir içinde — BASİT'te KAPALI gelir; test önce açar.
        var ac = e.WaitForElement("#vitrin-ayar-kat-ac", TimeSpan.FromSeconds(10));
        if (ac.GetAttribute("aria-expanded") != "true") ac.Click();
        e.WaitForElement("#vitrin-ayarlar", TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T4_Vitrin_tavan_notu_kaynak_satirindaki_TEK_ipucunda_sunucu_oneki_yok()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var tavan = e.Find("#vitrin-pano-ipucu > #vitrin-tavan");
        Assert.Equal(string.Format(Toplu4Sozlugu.Tr.VitrinTavanNotu, 16), tavan.TextContent.Trim());
        Assert.DoesNotContain("Sunucu", e.Markup, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#vitrin-kaynak-satir > #vitrin-kaynak"));
    }

    [Fact]
    public void T4_Vitrin_ayar_aciklamalari_TEK_ipucunda_tablodan_once()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var ipucu = e.Find("#vitrin-ayar-ipucu");
        Assert.NotNull(ipucu.QuerySelector("#vitrin-kalem-kaynak"));
        Assert.NotNull(ipucu.QuerySelector("#vitrin-duzenlenebilir"));
        Assert.Contains(Sozlukler.Tr.Pano.AyarAciklama, ipucu.TextContent, StringComparison.Ordinal);
        // Ayar kartında ipucu DIŞINDA açıklama paragrafı kalmadı.
        var kart = e.Find("#vitrin-ayar-baslik-satir").ParentElement!;
        Assert.DoesNotContain(kart.Children, c => c.TagName == "P");
        var html = e.Markup;
        Assert.True(html.IndexOf("vitrin-ayar-ipucu", StringComparison.Ordinal) < html.IndexOf("id=\"vitrin-ayarlar\"", StringComparison.Ordinal));
    }

    [Fact]
    public void T4_Vitrin_degistirilebilir_rozeti_ve_Varsayilan_sutunu_YALNIZ_PRO()
    {
        var b = Ciz(ArayuzModuServisi.Basit);
        Assert.Empty(b.FindAll("#vitrin-baslik-alis-marj .rozet"));
        Assert.Empty(b.FindAll("#vitrin-duzenlenebilir-sutunlar"));   // GOC-C-2: rozet satırı yalnız PRO
        Assert.Empty(b.FindAll("#vitrin-varsayilan-CEYREK_YENI"));
        Assert.Empty(b.FindAll("#vitrin-varsayilana-don-GRAM"));
        // TOPLU-6: "Sıra" sütunu da yalnız PRO (7 → 6).
        Assert.Equal(6, b.FindAll("#vitrin-ayarlar thead th").Count);
        Assert.Equal(6, b.FindAll("#vitrin-ayar-GRAM td").Count);
    }

    [Fact]
    public void T4_Vitrin_PRO_rozet_ve_Varsayilan_sutunu_KORUNUR()
    {
        var p = Ciz(ArayuzModuServisi.Profesyonel);
        // GOC-C-2: rozet sütun başlığında değil, ızgaranın üstündeki TEK satırda (yalnız PRO) — sayısı yine 1.
        Assert.Single(p.FindAll("#vitrin-duzenlenebilir-sutunlar .rozet"));
        Assert.Equal(8, p.FindAll("#vitrin-ayarlar thead th").Count);
        Assert.NotNull(p.Find("#vitrin-varsayilan-CEYREK_YENI"));
        Assert.NotNull(p.Find("#vitrin-varsayilana-don-GRAM"));
    }
}

// ═══ (4) Siparişler Yeni formu ══════════════════════════════════════════════
public sealed class Toplu4SiparisTestleri : Toplu4Tabani
{
    public Toplu4SiparisTestleri()
    {
        Yol("/siparisler", """
            {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
             "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
            """);
        Yol("/subeler", """[{"id":"s1","ad":"Merkez"}]""");
        Yol("/cari", """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""");
        Yol("/urunler", """[{"id":"u-1","ad":"Bilezik","ayarId":"K22","gramAgirlik":10.0}]""");
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> FormAcik(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Siparisler>();
        e.WaitForElement("#sip-yeni-ac", TimeSpan.FromSeconds(10)).Click();
        e.WaitForElement("#sip-form", TimeSpan.FromSeconds(10));
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Form_acikken_ustteki_dugme_gizli_TEK_vazgec(string mod)
    {
        var e = FormAcik(mod);
        Assert.True(e.Find("#sip-yeni-ac").HasAttribute("hidden"));
        Assert.NotNull(e.Find("#sip-f-vazgec"));
        e.Find("#sip-f-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#sip-form")));
        Assert.False(e.Find("#sip-yeni-ac").HasAttribute("hidden"));
    }

    [Fact]
    public void T4_BASIT_oturumdan_rozeti_ve_tur_grubu_YOK()
    {
        var e = FormAcik(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#sip-f-sube-sabit .rozet"));
        Assert.Contains("Merkez", e.Find("#sip-f-sube-sabit").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#sip-tur-grubu"));
    }

    [Fact]
    public void T4_PRO_tur_grubu_durum_suzgeciyle_AYNI_seritte_rozet_var()
    {
        var e = FormAcik(ArayuzModuServisi.Profesyonel);
        var serit = e.Find("#sayfa-basi-siparisler-filtre");
        Assert.NotNull(serit.QuerySelector("#sip-tur-grubu #sip-sekme-tamir"));
        Assert.NotNull(serit.QuerySelector("#sip-durum-suzgec"));
        // TOPLU-6 (TOPLU-4 hakemi ORTA): "oturumdan" rozeti PRO'da da kalktı.
        Assert.Empty(e.FindAll("#sip-f-sube-sabit .rozet"));
    }
}

// ═══ (12) Kurulum adım sayıları ═════════════════════════════════════════════
public sealed class Toplu4KurulumTestleri : Toplu4Tabani
{
    public Toplu4KurulumTestleri()
    {
        Yol("/kurulum/durum", """
            {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
             "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
             "muhurlenebilir":false,"eksikAdimlar":[],"atlananAdimlar":[],
             "adimlar":[
               {"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KULLANICILAR","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KUR","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KASA_BANKA","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false},
               {"anahtar":"STOK_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
             "sayilar":{"sube":1,"kullanici":2,"kur":6,"kasa":1,"banka":1,"acilisCari":0,"acilisKasa":0,"acilisBanka":0},
             "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu"}
            """);
        Yol("/kasalar", """[{"id":"k1","ad":"Merkez Kasa","aktif":true}]""");
        Yol("/bankalar", """{"hesaplar":[]}""");
        Yol("/kur/guncel", """{"id":"kur-1","altinHasFiyati":6827.2}""", son: false);
        Yol("/yetkiler/benim", """{"moduller":["kurulum"]}""", son: false);
        Yol("/cari", "[]", son: false);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Kurulum_adim_sayilari_neyin_sayisi_oldugunu_soyler(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Kurulum>();
        e.WaitForElement("#kur-adim-ISLETME", TimeSpan.FromSeconds(10));
        string Sayi(string a) => e.Find($"#kur-adim-{a} .metin-soluk.metin-kucuk").TextContent.Trim();
        Assert.Equal("1 şube", Sayi("ISLETME"));
        Assert.Equal("2 kullanıcı", Sayi("KULLANICILAR"));
        Assert.Equal("6 kur kaydı", Sayi("KUR"));
        Assert.Equal("2 kasa ya da banka hesabı", Sayi("KASA_BANKA"));
        Assert.Equal("0 müşteri açılışı", Sayi("CARI_ACILIS"));
        // Açıklamasız tek başına rakam kalmadı.
        Assert.DoesNotContain(e.FindAll("#kur-adimlar .metin-soluk.metin-kucuk"), x => Regex.IsMatch(x.TextContent.Trim(), @"^\d+$"));
    }
}

// ═══ (5) Liste sayaç satırı ═════════════════════════════════════════════════
public sealed class Toplu4UrunSayacTestleri : Toplu4Tabani
{
    private string urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22","gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true,"birim":"ADET"},
         {"id":"u2","tip":"ZIYNET","kod":"KLY-03","ad":"Kolye","ayarId":"K22","gramAgirlik":6,"tasAgirlik":0,"hasGrami":5.5,"barkod":null,"aktif":true,"birim":"ADET"}]
        """;

    public Toplu4UrunSayacTestleri()
    {
        Yol("/urunler/stok-adetleri", "[]", son: false);
        Yol("/urunler/madenler", """
            {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
              "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
             "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
            """);
        Yol("/urun-kategorileri", "[]", son: false);
        Yol("/reyonlar", "[]", son: false);
        Yol("/subeler", """[{"id":"s1","ad":"Merkez"}]""");
        Yol("/kur/pariteler", """{"kaynak":"TEST","pariteler":[]}""", son: false);
        yollar.Add(("/urunler", false, ""));   // yer tutucu — Gövde aşağıda güncellenir
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Ciz()
    {
        yollar[^1] = ("/urunler", false, urunler);
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        e.WaitForElement("#urun-sinir", TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T4_Urunler_sinir_ayri_paragraf_degil_SAYAC_satiri_cumle_yalniz_sinirda()
    {
        var e = Ciz();
        var s = e.Find("#urun-sinir");
        // TOPLU-6 (TOPLU-4 hakemi ORTA): sınırın altında yalnız sayı; "en çok 50" title'da.
        Assert.Equal(string.Format(Toplu6Sozlugu.Tr.ListeSayacSade, 2), s.TextContent.Trim());
        Assert.Equal(string.Format(Toplu6Sozlugu.Tr.ListeSiniriBilgi, 50), s.QuerySelector("span")!.GetAttribute("title"));
        Assert.DoesNotContain("metin-kucuk", s.ClassName ?? "", StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#urun-sinir-asildi"));
        Assert.DoesNotContain(string.Format(Sozlukler.Tr.Kabuk.SinirUyarisi, 50), e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Urunler_sinirda_daraltma_cumlesi_ayni_satirda()
    {
        urunler = "[" + string.Join(",", Enumerable.Range(1, 50).Select(i =>
            $$"""{"id":"x{{i}}","tip":"ZIYNET","kod":"K{{i}}","ad":"Ürün {{i}}","ayarId":"K22","gramAgirlik":1,"tasAgirlik":0,"hasGrami":0.9,"barkod":null,"aktif":true}""")) + "]";
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#urun-sinir > #urun-sinir-asildi")));
        Assert.Contains(string.Format(Sozlukler.Tr.Kabuk.SinirUyarisi, 50), e.Find("#urun-sinir-asildi").TextContent, StringComparison.Ordinal);
    }
}

// ═══ (8) Kur rozeti ═════════════════════════════════════════════════════════
public sealed class Toplu4KurTestleri : Toplu4Tabani
{
    public Toplu4KurTestleri()
    {
        Yol("/kur/kaynaklar", """{"kaynaklar":["ALTINKAYNAK","ANKARA"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""");
        Yol("/kur/gecmis", "[]");
        Yol("/kur/pariteler", """{"kaynak":"ALTINKAYNAK","pariteler":[]}""");
        Yol("/kur/guncel", """
            {"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,"eurTry":null,"gbpTry":null,
             "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-30T09:00:00Z","ayarFiyatlari":[]}
            """);
    }

    [Fact]
    public void T4_Kur_ucretsiz_rozeti_kaynak_satirinda_ve_aciklama_ipucunda()
    {
        Oturum(ArayuzModuServisi.Basit, rol: "MUDUR");
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-kaynak-satir > #kur-sozlesmesiz")), TimeSpan.FromSeconds(10));
        Assert.Single(e.FindAll("#kur-sozlesmesiz"));
        Assert.NotNull(e.Find("#kur-kaynak-satir > #kur-has-kaynak"));
        Assert.Equal(Toplu3Sozlugu.Tr.KurUcretsizIpucu, e.Find("#kur-kaynak-satir > #kur-ucretsiz-ipucu > p").TextContent.Trim());
        Assert.Equal(Toplu3Sozlugu.Tr.KurUcretsizRozet, e.Find("#kur-sozlesmesiz").TextContent.Trim());
    }
}

// ═══ (8, tur 2) Vade takibi — tasarım hakemi 02.10 KRİTİK ═════════════════════
public sealed class Toplu4VadeTestleri : Toplu4Tabani
{
    public Toplu4VadeTestleri()
    {
        Yol("/raporlar/vade-takibi", """
            {"ufukGun":7,"gecikenToplam":3000,"yaklasanToplam":0,"vadesizToplam":1500,"ufukOtesiToplam":0,"acikToplam":4500,
             "cariler":[{"cariId":"c1","cariAd":"Geciken Musteri","cariTur":"MUSTERI","telefon":"0532",
               "gecikenTutar":3000,"yaklasanTutar":0,"vadesizTutar":1500,"enEskiGecikmeGun":12,"enYakinVade":"2026-09-03",
               "borclar":[{"hareketId":"h1","tarih":"2026-08-20T08:00:00Z","vade":"2026-09-03","acikTutar":3000,"gecikmeGun":12,"durum":"GECIKTI"},
                          {"hareketId":"h2","tarih":"2026-09-30T08:00:00Z","vade":"2026-10-02","acikTutar":500,"gecikmeGun":0,"durum":"VADESINDE"}]}]}
            """);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VadeTakibi> Ciz(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#vade-vadesiz-kart")), TimeSpan.FromSeconds(10));
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Vade_vadesiz_karti_KISA_alt_metin_aciklama_ipucunda(string mod)
    {
        var e = Ciz(mod);
        Assert.Equal(Toplu4Sozlugu.Tr.VadeVadesizKisa, e.Find("#vade-vadesiz-alt > span").TextContent.Trim());
        Assert.Equal(Toplu4Sozlugu.Tr.VadeVadesizAciklama, e.Find("#vade-vadesiz-alt > #vade-vadesiz-ipucu > p").TextContent.Trim());
        // Eski 6 satırlık geliştirici açıklaması ("bu alan eklenmeden önce …") ekranda YOK.
        Assert.DoesNotContain("eklenmeden", e.Markup, StringComparison.Ordinal);
        Assert.True(Toplu4Sozlugu.Tr.VadeVadesizKisa.Length <= 24);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Vade_ipucu_gun_seciminin_YANINDA_yetim_satir_YOK(string mod)
    {
        var e = Ciz(mod);
        // ⓘ doğrudan gün seçiminin yanında; yetim metin satırı (eski #vade-ufuk-ipucu-satir) YOK.
        Assert.Equal("sayfa-basi-vade-takibi-filtre", e.Find("#vade-ufuk-ipucu").ParentElement!.Id);
        Assert.Empty(e.FindAll("#vade-ufuk-ipucu-satir"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#vade-ufuk-ipucu > #vade-ufuk-aciklama").TextContent));
        // "Kaç gün ilerisi" ekranda YALNIZ bir kez (kutunun etiketi) — ikinci yetim satır yok.
        Assert.Single(Regex.Matches(e.Markup, ">" + Regex.Escape(Toplu4Sozlugu.Tr.VadeUfukBilgi) + "<"));
    }

    [Fact]
    public void T4_Vade_serbest_gun_kutusu_Baska_gun_katinda_KAPALI_yazinca_acik_kalir()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var kat = e.Find("#vade-baska-gun");
        Assert.Equal("DETAILS", kat.TagName);
        Assert.False(kat.HasAttribute("open"));
        Assert.Equal(Toplu4Sozlugu.Tr.VadeBaskaGun, e.Find("#vade-baska-gun > summary").TextContent.Trim());
        Assert.NotNull(e.Find("#vade-baska-gun #vade-ufuk"));
        e.Find("#vade-ufuk").Input("14");
        e.WaitForAssertion(() => Assert.True(e.Find("#vade-baska-gun").HasAttribute("open")));
        // 30 hızlı düğmeyle aynı olsa da yazarken kat KAPANMAZ (yapışkan bayrak).
        e.Find("#vade-ufuk").Input("30");
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#vade-ufuk-30").GetAttribute("aria-pressed")));
        Assert.True(e.Find("#vade-baska-gun").HasAttribute("open"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Vade_degismez_notu_TEK_satir_duzeltme_yolu_ipucunda(string mod)
    {
        var e = Ciz(mod);
        var not = e.Find("#vade-degismez-satir > #degismez-ipucu-VadeTakibi");
        Assert.Empty(not.QuerySelectorAll(".degismez-alternatif"));
        Assert.Equal(Toplu4Sozlugu.Tr.VadeDegismezKisa, not.QuerySelector(".degismez-gerekce")!.TextContent.Trim());
        var ip = e.Find("#vade-degismez-satir > #vade-degismez-ipucu > #vade-degismez-aciklama").TextContent;
        Assert.Contains("ters kayıt", ip, StringComparison.Ordinal);
        Assert.Contains("display: flex", Toplu4Ortak.Blok(Toplu4Ortak.Css(), ".t4-degismez-satir .degismez-govde") ?? "", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T4_Vade_durum_hucresi_ham_kod_degil_sozlukten(string mod)
    {
        var e = Ciz(mod);
        var hucreler = e.FindAll("td.metin-kucuk").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("Gecikti (12 gün)", hucreler);
        Assert.Contains("Vadesinde", hucreler);
        foreach (var ham in new[] { "VADESİNDE", "VADESINDE", "GECİKTİ", "GECIKTI", "YAKLAŞIYOR" })
            Assert.DoesNotContain(ham, e.Find(".dikey-bosluk").TextContent, StringComparison.Ordinal);
    }
}

// ═══ (11) Yaşlandırma ═══════════════════════════════════════════════════════
public sealed class Toplu4YaslandirmaTestleri : Toplu4Tabani
{
    public Toplu4YaslandirmaTestleri()
    {
        Yol("/raporlar/katalog", """{"toplam":0,"faz":"A","raporlar":[]}""");
        Yol("/raporlar/aylik", """{"yil":2026,"ay":9,"gunler":[]}""");
        Yol("/raporlar/cari-yaslandirma", """
            {"yaslananCariler":[
               {"cariId":"e1","cariAd":"Alacaklı Müşteri","cariTur":"MUSTERI","tlBakiye":-3840.00,"hasGramBakiye":-1.5,
                "dovizBakiyeleri":{},"dovizBakiyesiVar":false,"gunSayisi":4,"dilim":"0-30","belirsizNedeni":null,"acilisDahil":false},
               {"cariId":"b1","cariAd":"Borçlu Müşteri","cariTur":"MUSTERI","tlBakiye":100.00,"hasGramBakiye":0,
                "dovizBakiyeleri":{},"dovizBakiyesiVar":false,"gunSayisi":10,"dilim":"0-30","belirsizNedeni":null,"acilisDahil":false}],
             "ozetTl":{"0-30":-3740.00,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},"ozetDoviz":{"USD":0,"EUR":0,"GBP":0},
             "toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false}
            """);
        Yol("/subeler", """[{"id":"s1","ad":"Merkez","aktif":true}]""");
        Yol("/reyonlar", "[]");
        Yol("/raporlar/odeme-araci", """{"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI","araclar":[],"veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},"storno":{"adet":0,"tutarTl":0},"guvenilirlik":"VERI_YOK","satisKirilimi":{"kaynak":"ODEME_KALEMLERI","satirlar":[],"brutSatisTl":0,"kirilimToplamTl":0,"farkTl":0,"kasayaGirenTl":0,"mahsupAltinTl":0,"veresiyeTl":0,"fisAdet":0,"kalemsizFisAdet":0,"kalemsizTl":0}}""");
        Yol("/raporlar/stok-degeri", """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""");
        Yol("/raporlar/reyon-stok", """{"reyonlar":[],"subeler":[]}""");
        Yol("/raporlar/stok-yaslandirma", """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"madenDagilimi":[],"yasiBilinmeyenAdet":0,"toplamAdet":0,"devir":null,"oluStok":{"urunler":[],"dahaFazlasiVar":false},"guvenilirlik":"VERI_YOK","uyarilar":[]}""");
        Yol("/raporlar/pos-fatura-mutabakati", """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""");
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ac()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-tablo")), TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T4_Eksi_tutar_rengi_ISARETE_bagli_dilim_rengine_degil()
    {
        var e = Ac();
        Assert.Contains("yas-eksi", e.Find("#yas-satir-e1 td.yas-0-30").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("yas-eksi", e.Find("#yas-satir-b1 td.yas-0-30").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("yas-eksi", e.Find("#yas-satir-b1 td.yas-31-60").ClassName, StringComparison.Ordinal);   // sıfır
        Assert.Contains("yas-eksi", e.Find("#yas-satir-e1 td.yas-toplam-tl").ClassName, StringComparison.Ordinal);
        var blok = Toplu4Ortak.Blok(Toplu4Ortak.Css(), "#yas-tablo td.yas-eksi") ?? "";
        Assert.Contains("var(--tehlike)", blok, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Has_notu_paragraf_degil_ipucunda()
    {
        var e = Ac();
        var not = e.Find("#yas-kart > #yas-has-not-satir > #yas-has-ipucu > #yas-has-notu");
        Assert.Equal(Sozlukler.Tr.Rapor.YasHasNotu, not.TextContent.Trim());
        Assert.Empty(e.FindAll("#yas-kart > p.yas-not"));
        Assert.Contains("display: none", Toplu4Ortak.Blok(Toplu4Ortak.Css(), "#yas-kart:has(#yas-tablo.yas-has-bos) > #yas-has-not-satir") ?? "", StringComparison.Ordinal);
    }
}

// ═══ (14a) Kullanıcı kartı — çok şubeli görünüm ════════════════════════════
public sealed class Toplu4KullaniciKartiTestleri : Toplu4Tabani
{
    public Toplu4KullaniciKartiTestleri()
    {
        Yol("/subeler", """[{"id":"s0","ad":"Şube 0","aktif":true},{"id":"s1","ad":"Şube 1","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""", son: false);
        Yol("/tercihler/benim", """{"tercihler":{},"gorunumSubesi":{"kayitli":null,"subeId":null,"tumSubeler":true,"gecersizTercih":false,"deger":"TUMU"}}""", son: false);
        Yol("/yetkiler/benim", """{"moduller":["cari","urun"]}""", son: false);
    }

    [Fact]
    public void T4_Cok_subeli_SAHIP_kartinda_bakilan_sube_yazar()
    {
        Oturum(ArayuzModuServisi.Profesyonel, sube: "s0");
        var ust = Render<SubeSecici>();
        var kart = Render<SubeSecici>(p => p.Add(x => x.KartIci, true));
        ust.WaitForAssertion(() => Assert.NotNull(ust.Find("#ust-sube-secim")), TimeSpan.FromSeconds(10));
        kart.WaitForAssertion(() => Assert.NotNull(kart.Find("#ust-sube-gorunum")), TimeSpan.FromSeconds(10));
        Assert.Contains(Sozlukler.Tr.Kabuk.SubeTumu, kart.Find("#ust-sube-gorunum").TextContent, StringComparison.Ordinal);
        Assert.Equal(Toplu4Sozlugu.Tr.KartGorunumIpucu, kart.Find("#ust-sube-gorunum").GetAttribute("title"));
        Assert.Empty(kart.FindAll("#ust-sube-sabit"));
        Assert.Empty(ust.FindAll("#ust-sube-gorunum"));
    }
}

// ═══ (14b) İşlem defteri — süzgeç KAPALIYKEN şube iki kez yazılmaz (gerçek tarayıcı bulgusu) ═══
public sealed class Toplu4IslemDefteriTestleri : Toplu4Tabani
{
    private void Hazirla(string deger, string? subeId, bool tum)
    {
        var sj = subeId is null ? "null" : $"\"{subeId}\"";
        Yol("/subeler", """[{"id":"s1","ad":"Merkez Şube","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""", son: false);
        Yol("/tercihler/benim", "{\"tercihler\":{},\"gorunumSubesi\":{\"kayitli\":\"" + deger + "\",\"subeId\":" + sj
            + ",\"tumSubeler\":" + (tum ? "true" : "false") + ",\"gecersizTercih\":false,\"deger\":\"" + deger + "\"}}", son: false);
        Yol("/yetkiler/benim", """{"moduller":["rapor"]}""", son: false);
        Yol("/kullanicilar", "[]", son: false);
        Yol("/raporlar/islem-defteri", """
            {"baslangic":"2026-10-02","bitis":"2026-10-02","toplamSatir":0,"ofset":0,"limit":25,"dahaFazlasiVar":false,
             "ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},"storno":{"adet":0,"tutarTl":0},"satirlar":[]}
            """, son: false);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.IslemDefteri> Ciz()
    {
        Oturum(ArayuzModuServisi.Profesyonel, sube: "s1");
        var ust = Render<SubeSecici>();
        ust.WaitForAssertion(() =>
        {
            var g = KabukServisi.Ortak.GorunumIcin(Services.GetRequiredService<OturumServisi>());
            Assert.True(g is { Yuklendi: true } && g.Subeler.Count == 2);
        }, TimeSpan.FromSeconds(10));
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#id-rozet-kap #gorunum-rozeti")), TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T4_Defter_sube_seciliyken_rozet_sarmalayicisi_gizler_suzgec_basligi_subeyi_yazar()
    {
        Hazirla("s2", "s2", tum: true);
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Contains("id-rozet-suzgecte", e.Find("#id-rozet-kap").ClassName ?? "", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.Contains("Şube 2", e.Find("#id-suzgec").TextContent, StringComparison.Ordinal);
        // Süzgeç akordeonu KAPALI: #id-sube DOM'da yok — eski :has(#id-sube) kuralı tek başına yetmiyordu.
        Assert.Empty(e.FindAll("#id-sube"));
        var css = Toplu4Ortak.Css();
        Assert.Contains("display: contents", Toplu4Ortak.Blok(css, "#id-rozet-kap") ?? "", StringComparison.Ordinal);
        Assert.Contains("display: none", Toplu4Ortak.Blok(css, "#id-rozet-kap.id-rozet-suzgecte > p.gorunum-rozeti") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Defter_tum_subelerde_rozet_GORUNUR_kalir()
    {
        Hazirla("TUMU", null, tum: true);
        var e = Ciz();
        Assert.DoesNotContain("id-rozet-suzgecte", e.Find("#id-rozet-kap").ClassName ?? "", StringComparison.Ordinal);
    }
}

// ═══ Kaynak + CSS + sözlük testleri ═════════════════════════════════════════
public sealed class Toplu4KaynakTestleri
{
    private static string Razor(string ad) => Toplu4Ortak.Dosya("Kuyumcu.Web", "Pages", ad);

    [Fact]
    public void T4_toplu4_css_toplu3ten_sonra_olcekten_ve_ortaktan_once()
    {
        var html = Toplu4Ortak.Dosya("Kuyumcu.Web", "wwwroot", "index.html");
        var t3 = html.IndexOf("css/toplu3.css", StringComparison.Ordinal);
        var t4 = html.IndexOf("css/toplu4.css", StringComparison.Ordinal);
        var ol = html.IndexOf("css/olcek.css", StringComparison.Ordinal);
        var o = html.IndexOf("css/ortak.css", StringComparison.Ordinal);
        Assert.True(t3 > 0 && t4 > t3 && ol > t4 && o > ol);
    }

    [Fact]
    public void T4_Sabit_renk_yok_yalniz_belirtec()
    {
        var css = Toplu4Ortak.Css();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b(?![\w-])(?=[^{]*;)", Regex.Replace(css, @"#[a-z][\w-]*", ""));
        Assert.DoesNotContain("rgb(", css, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Etiketsiz_seritte_eylem_ustu_etiket_payi_SIFIR()
    {
        var css = Toplu4Ortak.Css();
        const string kosul = ".sayfa-basi:not(:has(.sayfa-basi-filtre :is(.alan-etiket, .alan > label)))";
        Assert.Contains("padding-top: 0", Toplu4Ortak.Blok(css, kosul + " > .sayfa-basi-eylem") ?? "", StringComparison.Ordinal);
        Assert.Contains("padding-top: 0", Toplu4Ortak.Blok(css, kosul + " > .sayfa-basi-filtre > .arama-kutu") ?? "", StringComparison.Ordinal);
        // P-37 taban kuralı (etiketli şeritte hiza) app.css'te DEĞİŞMEDİ.
        Assert.Contains(".sayfa-basi-eylem { padding-top: var(--arac-etiket-bosluk); }",
            Toplu4Ortak.Dosya("Kuyumcu.Web", "wwwroot", "css", "app.css"), StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Hurda_acici_metin_baglantisi_kalibi_gizle_tetigin_yerinde()
    {
        var css = Toplu4Ortak.Css();
        var b = Toplu4Ortak.Blok(css, "#hrd-detay-ac, #hrd-detay-gizle") ?? "";
        Assert.Contains("width: auto", b, StringComparison.Ordinal);
        Assert.Contains("align-self: flex-start", b, StringComparison.Ordinal);
        Assert.Contains("border: 0", b, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", b, StringComparison.Ordinal);
        Assert.Contains("rotate(180deg)", Toplu4Ortak.Blok(css, "#hrd-detay-gizle .zo-detay-ok") ?? "", StringComparison.Ordinal);
        Assert.StartsWith("+ ", ZoIslemSozlugu.Tr.DetayEkle, StringComparison.Ordinal);
        Assert.EndsWith(" ekle", ZoIslemSozlugu.Tr.DetayEkle, StringComparison.Ordinal);
        Assert.StartsWith("+ ", ZoIslemSozlugu.En.DetayEkle, StringComparison.Ordinal);
        // "Daha az göster" bölümün ÜSTÜNDE (tetiğin yerinde), sonunda değil.
        var r = Razor("Hurda.razor");
        var gizle = r.IndexOf("id=\"hrd-detay-gizle\"", StringComparison.Ordinal);
        var detay = r.IndexOf("id=\"hrd-detay\"", StringComparison.Ordinal);
        Assert.True(gizle > 0 && detay > gizle, "hrd-detay-gizle, hrd-detay bölümünden ÖNCE gelmeli");
    }

    [Fact]
    public void T4_Uyum_risk_ozeti_3_arti_3_dar_ekranda_2()
    {
        var css = Toplu4Ortak.Css();
        Assert.Contains("repeat(3, minmax(0, 1fr))", Toplu4Ortak.Blok(css, "#uy-risk-ozet") ?? "", StringComparison.Ordinal);
        Assert.Contains("grid-column: auto", Toplu4Ortak.Blok(css, "#uy-risk-ozet > .stat-kart--genis") ?? "", StringComparison.Ordinal);
        Assert.Contains("repeat(2, minmax(0, 1fr))", Toplu3Ortak.Medya(css, "@media (max-width: 640px) { #uy-risk-ozet"), StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Uyum_ve_Vade_aciklamalari_ipucunda()
    {
        var u = Razor("Uyum.razor");
        var ip = u.IndexOf("id=\"uy-risk-ipucu\"", StringComparison.Ordinal);
        var son = u.IndexOf("</details>", ip, StringComparison.Ordinal);
        Assert.True(ip > 0 && son > ip);
        var govde = u[ip..son];
        foreach (var id in new[] { "uy-risk-aciklama", "uy-risk-durdurmaz", "uy-risk-kapali" })
            Assert.Contains($"id=\"{id}\"", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("metin-kucuk", govde, StringComparison.Ordinal);
        var v = Razor("VadeTakibi.razor");
        var vi = v.IndexOf("id=\"vade-ufuk-ipucu\"", StringComparison.Ordinal);
        Assert.True(vi > 0 && v.IndexOf("id=\"vade-ufuk-aciklama\"", StringComparison.Ordinal) > vi);
    }

    [Fact]
    public void T4_Islem_defteri_sube_suzgeci_varken_gorunum_rozeti_gizli()
    {
        Assert.Contains("display: none", Toplu4Ortak.Blok(Toplu4Ortak.Css(), ".ana-icerik:has(#id-sube) p.gorunum-rozeti") ?? "", StringComparison.Ordinal);
        Assert.Contains("ElemanId=\"id-sube\"", Razor("IslemDefteri.razor"), StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Sozluk_TR_EN_dolu_ve_jargonsuz()
    {
        foreach (var m in new[] { Toplu4Sozlugu.Tr, Toplu4Sozlugu.En })
            foreach (var p in typeof(Toplu4Metinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.Same(Toplu4Sozlugu.En, Toplu4Sozlugu.Al("en"));
        Assert.Same(Toplu4Sozlugu.Tr, Toplu4Sozlugu.Al("tr"));
        Assert.DoesNotContain("unucu", Toplu4Sozlugu.Tr.VitrinTavanNotu, StringComparison.Ordinal);
        Assert.DoesNotContain("beklemede", Toplu2Sozlugu.Tr.KurulumNegatifAcilisPro, StringComparison.Ordinal);
        Assert.DoesNotContain("tutucu", Toplu2Sozlugu.Tr.KurulumNegatifAcilisPro, StringComparison.Ordinal);
        Assert.DoesNotContain("pending", Toplu2Sozlugu.En.KurulumNegatifAcilisPro, StringComparison.Ordinal);
        Assert.DoesNotContain("değildi", Sozlukler.Tr.RaporSeti.SinirUyarisi, StringComparison.Ordinal);
        Assert.DoesNotContain("tekil uç", Sozlukler.Tr.Uyum.RiskCariSecilmedi, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Yeni_x_dugme_dili_cumle_duzeninde()
    {
        var t = Sozlukler.Tr;
        foreach (var m in new[] { t.Urun.YeniUrun, t.Fatura.YeniFatura, t.Kullanici.YeniKullanici, t.Sayim.YeniOturum,
                                  t.Fiyat.YeniListe, t.Fiyat.YeniKampanya, t.Isletme.YeniSube, t.UrunAgaci.YeniKategori, t.UrunAgaci.YeniReyon })
            Assert.Matches(@"^Yeni [a-zçğıöşü]", m);
    }

    [Fact]
    public void T4_Kurulum_adim_satiri_masaustunde_sabit_dort_sutun()
    {
        var css = Toplu4Ortak.Css();
        var m = Toplu3Ortak.Medya(css, "@media (min-width: 641px)");
        Assert.Contains("grid-template-areas: \"durum ad sayi eylem\"", m, StringComparison.Ordinal);
        Assert.Contains("8rem minmax(0, 1fr) auto 10rem", m, StringComparison.Ordinal);
        Assert.Contains("grid-area: eylem", m, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Kalan_12px_aciklama_ve_sayac_satirlari_en_az_14px()
    {
        var css = Toplu4Ortak.Css();
        foreach (var secici in new[] { "#kur-pro-notu, #kur-pro-notu-bag", "#sip-ozet .metin-kucuk, #sip-ozet strong", "#uy-risk-cari-sinir", "#vitrin-atlanan-yok", ".t4-sayac" })
            Assert.Contains("max(14px, var(--yazi-sm))", Toplu4Ortak.Blok(css, secici) ?? "", StringComparison.Ordinal);
        // Toplu4 CSS'inde 14 px altına inebilen çıplak --yazi-sm yok (bir ölçekte 13 px).
        Assert.DoesNotMatch(@"font-size:\s*var\(--yazi-sm\)", css);
    }

    [Fact]
    public void T4_Vade_ipucu_tetigi_filtre_yuvasinda_genislemez_dar_ekranda_yuva_genisliginde_acilir()
    {
        // Tasarım hakemi 02.10: filtre yuvası her çocuğu sütun + 220 px yapar (app.css); ⓘ tetiği bundan muaf.
        var css = Toplu4Ortak.Css();
        var blok = Toplu4Ortak.Blok(css, "#sayfa-basi-vade-takibi-filtre > #vade-ufuk-ipucu") ?? "";
        Assert.Contains("display: inline-block", blok, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto", blok, StringComparison.Ordinal);
        Assert.Contains("min-width: 0", blok, StringComparison.Ordinal);
        var i = css.IndexOf("#sayfa-basi-vade-takibi-filtre { position: relative; }", StringComparison.Ordinal);
        Assert.True(i > 0);
        var m = Toplu3Ortak.Medya(css, "@media (max-width: 640px)", css.LastIndexOf("@media (max-width: 640px)", i, StringComparison.Ordinal));
        Assert.Contains("#sayfa-basi-vade-takibi-filtre > #vade-ufuk-ipucu { position: static; }", m, StringComparison.Ordinal);
        Assert.Contains("#sayfa-basi-vade-takibi-filtre > #vade-ufuk-ipucu > summary", Toplu3Ortak.Medya(css, "@media (max-width: 480px)"), StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: 0; inset-inline-end: 0; width: auto; max-width: none;", m, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", Toplu4Ortak.Blok(css, ".t4-baska-gun > summary") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T4_Dar_ekranda_ipucu_katmani_satira_bagli_tam_genislik_ekran_disina_tasmaz()
    {
        // Gerçek tarayıcı (02.10, 375 px): ⓘ katmanı tetiğe sağdan yaslıyken /kur x=-6, /vade x=-91 idi.
        var css = Toplu4Ortak.Css();
        var i = css.IndexOf(".t4-ipucu-satir { position: relative; }", StringComparison.Ordinal);
        Assert.True(i > 0);
        var m = Toplu3Ortak.Medya(css, "@media (max-width: 640px)", css.LastIndexOf("@media (max-width: 640px)", i, StringComparison.Ordinal));
        Assert.Contains(".t4-ipucu-satir { position: relative; }", m, StringComparison.Ordinal);
        Assert.Contains(".t4-ipucu-satir > .zo-ipucu { position: static; }", m, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: 0; inset-inline-end: 0; width: auto; max-width: none;", m, StringComparison.Ordinal);
    }

    [Fact]
    public void T4_P37c_seritte_T216_tetige_bagli_katman_ve_perde_altta()
    {
        var ortak = Toplu4Ortak.Dosya("Kuyumcu.Web", "wwwroot", "css", "ortak.css");
        var b = Toplu3Ortak.Blok(ortak, ".sayfa-basi .sayfa-basi-filtre .secici-kap > .secici-liste-kap:not(.sabit)") ?? "";
        Assert.Contains("position: absolute", b, StringComparison.Ordinal);
        var app = Toplu4Ortak.Dosya("Kuyumcu.Web", "wwwroot", "css", "app.css");
        Assert.Contains(".secici-perde { position: fixed; inset: 0; z-index: 40;", app, StringComparison.Ordinal);
        var denetim = Toplu4Ortak.Dosya("denetim.sh");
        Assert.Contains("P37C_T216", denetim, StringComparison.Ordinal);
        Assert.Contains("P37C_PERDE", denetim, StringComparison.Ordinal);
        Assert.Contains("taban akışta · şeritte T2-16", denetim, StringComparison.Ordinal);
        var test = Toplu4Ortak.Dosya("Kuyumcu.Tests", "P37cPanelVeKapsamTestleri.cs");
        Assert.Contains("BUGÜNKÜ DAVRANIŞ İKİ KATMANDIR", test, StringComparison.Ordinal);
    }
}
