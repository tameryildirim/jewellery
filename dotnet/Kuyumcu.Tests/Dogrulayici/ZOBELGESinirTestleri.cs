using System.Net;
using System.Reflection;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metindir (üretim
// kodundan hesaplanmaz). Paketin dokunduğu para/has GÖSTERİM ve GİRİŞ yolları:
//   • İşlem defteri "tek özet satırı" (#id-ozet: TL giriş/çıkış, net has)
//   • Kasa raporu gün sonu sayımı (#kr-sayim-tutar → POST gun-kapanisi)
//   • Rapor merkezi BASİT "bugün" yüzü has gösterimi (BugunHas)
// Sınırlar: 0 · eksi · kuruş 0,005 (AwayFromZero — Bicim.Tl) · has 4 hane
// (0,0001 — Bicim.Has/P-57 "has 4, gram 3") · 999.999.999,99 · boş/null ·
// Türkçe giriş ('45.000', '1,5', belirsiz '1.500' reddi — FormAlani.SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZOBELGESinirTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private string islemDefteri = Defter("0", "0", "0", "0");

    public ZOBELGESinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private static string Defter(string tlGiris, string tlCikis, string hasGiris, string hasCikis) => $$"""
        {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":1,"ofset":0,"limit":10,"dahaFazlasiVar":false,
         "ozet":{"tlGiris":{{tlGiris}},"tlCikis":{{tlCikis}},"hasGiris":{{hasGiris}},"hasCikis":{{hasCikis}},"satirSayisi":1},
         "storno":{"adet":0,"tutarTl":0},
         "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1,"hasGrami":0}]}
        """;

    private static string Bugun => DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private string? kasaDefteriOzel;

    private string KasaDefteri => $$"""
        {"baslangic":"{{Bugun}}","bitis":"{{Bugun}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,
         "yazSaatiRiski":false,"toplamSatir":1,"kasalar":[
          {"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,
           "kapanisBakiye":1200.00,"guncelBakiye":1200.00,"gunler":[
             {"tarih":"{{Bugun}}","devir":500.00,"giris":700.00,"cikis":0,"hareketSayisi":2,"kapanis":1200.00,
              "durum":"ACIK","sayilanNakit":null,"kapanisDefterBakiye":null,"fark":null}]}]}
        """;

    private sealed class SahteApi(ZOBELGESinirTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add($"{istek.Method.Method} {istek.RequestUri.PathAndQuery} {govde}");
            var g = yol switch
            {
                // PARA-DEMO: sayım çekmecesi nakit defterini bu uçtan okur (kart/havale yok → nakit = bakiye 1200).
                var x when x.EndsWith("/gun-kapanisi/onizleme") =>
                    """{"kasaId":"ks1","paraBirimi":"TRY","toplamBakiye":1200.00,"kartToplam":0,"havaleToplam":0,"nakitDefter":1200.00}""",
                var x when x.EndsWith("/gun-kapanisi") =>
                    """{"kasaId":"ks1","yerelGun":"2026-09-25","sayilanNakit":0,"defterBakiye":1200.00,"fark":0,"aciklama":null,"kilitli":true}""",
                var x when x.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var x when x.EndsWith("/raporlar/kasa-defteri") => t.kasaDefteriOzel ?? t.KasaDefteri,
                var x when x.EndsWith("/raporlar/islem-defteri") => t.islemDefteri,
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => """[{"id":"u1","ad":"Ayşe","soyad":"Yılmaz"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private string Ozet()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-ozet");
        return c.Find("#id-ozet").TextContent.Replace('\u00a0', ' ');
    }

    // ═══ İŞLEM DEFTERİ ÖZETİ ═══════════════════════════════════════════════

    [Fact]
    public void ZOBS01_ozet_sifir_TL_ve_has_sifir_yazar()
    {
        islemDefteri = Defter("0", "0", "0", "0");
        var o = Ozet();
        Assert.Contains("0 ₺", o);
        Assert.Contains("0,000", o);
    }

    [Fact]
    public void ZOBS02_ozet_cok_buyuk_tutar_999_999_999_99_tam_yazar()
    {
        islemDefteri = Defter("999999999.99", "0", "0", "0");
        Assert.Contains("999.999.999,99 ₺", Ozet());
    }

    [Fact]
    public void ZOBS03_ozet_kurus_0_005_AwayFromZero_0_01_olur()
    {
        islemDefteri = Defter("1.005", "0.004", "0", "0");
        var o = Ozet();
        Assert.Contains("1,01 ₺", o);    // 1,005 → 1,01 (AwayFromZero)
        Assert.Contains("0 ₺", o);       // 0,004 → 0
    }

    [Fact]
    public void ZOBS04_ozet_net_has_eksi_isaretle_yazar()
    {
        // giriş 1, çıkış 3,5 → net −2,5 has (işaret korunur)
        islemDefteri = Defter("0", "0", "1", "3.5");
        var o = Ozet();
        Assert.Matches(@"[−-]2,5", o);
    }

    [Fact]
    public void ZOBS05_ozet_net_has_0_0001_dort_hane_ile_gorunur()
    {
        // has 4 hane (P-57): 0,0001 has sıfıra yuvarlanıp kaybolmamalı.
        islemDefteri = Defter("0", "0", "0.0001", "0");
        var o = Ozet();
        Assert.True(o.Contains("0,0001"), "ozet: " + o.Replace("\n", " "));
    }

    [Fact]
    public void ZOBS06_ozet_alani_null_ise_cokmez_sifir_yazar()
    {
        islemDefteri = """{"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":0,"ofset":0,"limit":10,"dahaFazlasiVar":false,"ozet":null,"storno":null,"satirlar":[]}""";
        var o = Ozet();
        Assert.Contains("0 ₺", o);
    }

    // ═══ KASA SAYIMI (GİRİŞ) ═══════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.KasaRaporu> SayimAc()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        c.WaitForElement("#kr-sayim-ks1");
        c.Find("#kr-sayim-ks1").Click();
        c.WaitForElement("#kr-sayim-tutar");
        return c;
    }

    private int PostSayisi => istekler.Count(i => i.StartsWith("POST ", StringComparison.Ordinal) && i.Contains("gun-kapanisi"));
    private string SonPost => istekler.Last(i => i.StartsWith("POST ", StringComparison.Ordinal) && i.Contains("gun-kapanisi"));

    private void Gir(IRenderedComponent<Kuyumcu.Web.Pages.KasaRaporu> c, string metin)
    {
        c.Find("#kr-sayim-tutar").Input(metin);
        c.Find("#kr-sayim-tutar").Blur();
        c.Find("#cekmece-gun-sayimi-kaydet").Click();
    }

    [Fact]
    public void ZOBS07_sayim_SIFIR_kabul_edilir_gercek_istek_0_gider()
    {
        var c = SayimAc();
        Gir(c, "0");
        c.WaitForAssertion(() => Assert.Equal(1, PostSayisi));
        Assert.Contains("\"sayilanNakit\":0", SonPost);
    }

    [Fact]
    public void ZOBS08_sayim_EKSI_reddedilir_istek_gitmez()
    {
        var c = SayimAc();
        Gir(c, "-1");
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#alan-hata-kr-sayim-tutar")));
        Assert.Equal(0, PostSayisi);
    }

    [Fact]
    public void ZOBS09_sayim_BOS_reddedilir_istek_gitmez()
    {
        var c = SayimAc();
        c.Find("#cekmece-gun-sayimi-kaydet").Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#alan-hata-kr-sayim-tutar")));
        Assert.Equal(0, PostSayisi);
    }

    [Fact]
    public void ZOBS10_sayim_belirsiz_1_500_reddedilir_istek_gitmez()
    {
        var c = SayimAc();
        Gir(c, "1.500");
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#alan-hata-kr-sayim-tutar")));
        Assert.Equal(0, PostSayisi);
    }

    [Theory]
    [InlineData("45.000", "\"sayilanNakit\":45000")]
    [InlineData("1,5", "\"sayilanNakit\":1.5")]
    [InlineData("999.999.999,99", "\"sayilanNakit\":999999999.99")]
    [InlineData("0,005", "\"sayilanNakit\":0.005")]
    public void ZOBS11_sayim_Turkce_giris_dogru_sayiya_cevrilir(string metin, string beklenen)
    {
        var c = SayimAc();
        Gir(c, metin);
        c.WaitForAssertion(() => Assert.Equal(1, PostSayisi));
        Assert.Contains(beklenen, SonPost);
    }

    // ═══ RAPOR MERKEZİ "BUGÜN" HAS GÖSTERİMİ ═══════════════════════════════

    private string BugunHas(decimal gram)
    {
        // Sayfa ÇİZİLMEDEN örnek: BugunHas yalnız Dil (sözlük) ve Bicim.Gram kullanır.
        var r = new Kuyumcu.Web.Pages.Raporlar();
        typeof(Kuyumcu.Web.Pages.Raporlar).GetProperty("Dil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .SetValue(r, Services.GetRequiredService<DilServisi>());
        var m = typeof(Kuyumcu.Web.Pages.Raporlar).GetMethod("BugunHas", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((string)m.Invoke(r, [gram])!).Replace('\u00a0', ' ');
    }

    [Fact]
    public void ZOBS12_bugun_has_eksi_isaret_korunur()
    {
        Assert.StartsWith("−2,5", BugunHas(-2.5m));
    }

    [Fact]
    public void ZOBS13_bugun_has_0_0004_dort_hane_ile_gorunur()
    {
        // has 4 hane (P-57): 0,0004 has "0,000" olup kaybolmamalı.
        Assert.StartsWith("0,0004", BugunHas(0.0004m));
    }

    // ═══ KASA RAPORU ÖZET SATIRI (GÖSTERİM) — doğrulayıcı 28.09 ek sınırları ═══
    // Beklenen metinler SABİT yazıldı (üretim kodundan hesaplanmaz).

    private static string KasaTek(string bakiye, string fark, string durum) => $$"""
        {"baslangic":"{{Bugun}}","bitis":"{{Bugun}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,
         "yazSaatiRiski":false,"toplamSatir":1,"kasalar":[
          {"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,
           "kapanisBakiye":{{bakiye}},"guncelBakiye":{{bakiye}},"gunler":[
             {"tarih":"{{Bugun}}","devir":0,"giris":0,"cikis":0,"hareketSayisi":0,"kapanis":{{bakiye}},
              "durum":"{{durum}}","sayilanNakit":null,"kapanisDefterBakiye":null,"fark":{{fark}}}]}]}
        """;

    private string KasaBakiye(string bakiye, string fark = "null", string durum = "ACIK")
    {
        kasaDefteriOzel = KasaTek(bakiye, fark, durum);
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        c.WaitForElement("#kr-bakiye-ks1");
        return c.Find("#kr-bakiye-ks1").TextContent.Trim().Replace('\u00a0', ' ');
    }

    [Fact]
    public void ZOBS14_kasa_ozet_bakiye_sifir()
        => Assert.Equal("0 ₺", KasaBakiye("0"));

    [Fact]
    public void ZOBS15_kasa_ozet_bakiye_eksi_isaretli()
        => Assert.Equal("\u2212150,50 ₺", KasaBakiye("-150.5"));

    [Fact]
    public void ZOBS16_kasa_ozet_bakiye_cok_buyuk_tam()
        => Assert.Equal("999.999.999,99 ₺", KasaBakiye("999999999.99"));

    [Fact]
    public void ZOBS17_kasa_ozet_bakiye_kurus_0_005_AwayFromZero()
        => Assert.Equal("0,01 ₺", KasaBakiye("0.005"));

    [Fact]
    public void ZOBS18_kasa_ozet_eksi_sayim_farki_kirmizi_rozet_isaretli()
    {
        kasaDefteriOzel = KasaTek("1200", "-0.5", "KAPALI");
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        c.WaitForElement("#kr-bugun-ks1");
        var e = c.Find("#kr-bugun-ks1");
        Assert.Contains("\u22120,50 ₺", e.TextContent.Replace('\u00a0', ' '));
        Assert.Contains("rozet-kirmizi", e.ParentElement!.ClassList);
    }

    [Fact]
    public void ZOBS19_kasa_ozet_sifir_fark_kirmizi_DEGIL()
    {
        kasaDefteriOzel = KasaTek("1200", "0", "KAPALI");
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        c.WaitForElement("#kr-bugun-ks1");
        Assert.DoesNotContain("rozet-kirmizi", c.Find("#kr-bugun-ks1").ParentElement!.ClassList);
    }

    // ═══ 29.09 DEVAM 5 HASIM DOĞRULAYICI — SAYIM FARK GEREKÇESİ "*" SINIRI ═════
    // Ekran (KasaRaporu.SayimFarkVar) ile sunucu (KasaModulu.GunKapat) AYNI kuralı
    // uygulamalı: fark = Round(sayılan − defter, 2, AwayFromZero) ≠ 0 → gerekçe zorunlu.
    // Defter 1.200,00 ₺ (KasaDefteri fikstürü). Beklenen değerler SABİT, elle hesaplı.
    [Theory]
    [InlineData("1.200", false)]            // fark 0
    [InlineData("1.200,004", false)]        // 0,004 → 0,00 (AwayFromZero)
    [InlineData("1.199,996", false)]        // −0,004 → 0,00
    [InlineData("1.200,005", true)]         // 0,005 → 0,01 (AwayFromZero sınırı)
    [InlineData("1.199,995", true)]         // −0,005 → −0,01
    [InlineData("0", true)]                 // sıfır sayım: fark −1.200
    [InlineData("999.999.999,99", true)]    // çok büyük tutar
    [InlineData("1.200,0001", false)]       // 4 hane: 0,0001 → 0,00
    public void ZOBS20_sayim_gerekce_yildizi_AwayFromZero_sinirinda_sunucuyla_ayni(string metin, bool yildizBeklenir)
    {
        var c = SayimAc();
        c.Find("#kr-sayim-tutar").Input(metin);
        c.WaitForAssertion(() => Assert.Equal(yildizBeklenir,
            c.FindAll("label[for='kr-sayim-aciklama'] .zorunlu-isaret").Count == 1));
        // Çelişki yok: yıldız varsa "(isteğe bağlı)" yok, yoksa var.
        Assert.Equal(!yildizBeklenir, c.FindAll("#kr-sayim-aciklama-sar .alan-istege").Count == 1);
    }

    [Fact]
    public void ZOBS21_sayim_BOS_ya_da_belirsiz_girdide_yildiz_yok_istek_gitmez()
    {
        var c = SayimAc();
        c.Find("#kr-sayim-tutar").Input("");
        Assert.Empty(c.FindAll("label[for='kr-sayim-aciklama'] .zorunlu-isaret"));
        Gir(c, "1.500"); // belirsiz → alan hatası, istek yok
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#alan-hata-kr-sayim-tutar")));
        Assert.Equal(0, PostSayisi);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE (tur 28.09 gece) — HASIM DOĞRULAYICI: /raporlar REYON STOK
// kartındaki has gösterimi (ZB26: F3 "g" → Bicim.Has) için bağımsız sınırlar.
// Beklenen metinler SABİTTİR (üretim kodundan hesaplanmaz). Sunucu madenler[].
// toplamGram = Σ HasGram (RaporModulu.cs:1068) — yani "has" etiketi doğru birim.
// Atanmamış satırda gram = stok − yerleşik → EKSİ olabilir (RaporModulu.cs:1115).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOBELGEReyonSinirTestleri : BunitContext
{
    private string madenJson = """[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":0}]""";
    private string atanmamisMadenJson = """[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":0}]""";

    public ZOBELGEReyonSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte(ZOBELGEReyonSinirTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = istek.RequestUri!.AbsolutePath;
            string g =
                y.EndsWith("/raporlar/katalog") ? """{"toplam":0,"faz":"A","raporlar":[]}""" :
                y.EndsWith("/raporlar/aylik") ? """{"yil":2026,"ay":9,"satis":{"sayisi":0,"toplamTl":0,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},"alis":{"sayisi":0,"toplamTl":0,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},"tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0},"karTl":null,"karKaynagi":"HESAPLANAMADI","satisAlisFarkiTl":0,"netHasGramHareketi":0,"madenDagilimi":[],"madenUyarisi":null,"hareketSayisi":0,"gunler":[]}""" :
                y.EndsWith("/raporlar/cari-yaslandirma") ? """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""" :
                y.EndsWith("/raporlar/stok-degeri") ? """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""" :
                y.EndsWith("/raporlar/reyon-stok") ?
                    $$"""{"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,"subeId":"s1","subeAdi":"Merkez","adet":1,"urunSayisi":1,"madenler":{{t.madenJson}},"altinHasGram":0}],"atanmamis":[{"subeId":"s1","subeAdi":"Merkez","adet":1,"madenler":{{t.atanmamisMadenJson}}}],"toplamAltinHasGram":0,"uyari":null}""" :
                y.EndsWith("/raporlar/stok-yaslandirma") ? """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""" :
                y.EndsWith("/raporlar/pos-fatura-mutabakati") ? """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""" :
                y.EndsWith("/subeler") ? """[{"id":"s1","ad":"Merkez","aktif":true}]""" :
                y.EndsWith("/reyonlar") ? "[]" : "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private string Kart()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#reyon-stok-has-baslik")));
        return e.Find("#reyon-stok-kart").TextContent;
    }

    private static string M(string gram) => $$"""[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":{{gram}}}]""";

    [Fact]
    public void ZOBR01_reyon_has_SIFIR_dort_hane()
    {
        madenJson = M("0");
        Assert.Contains("Altın: 0,0000 has", Kart(), StringComparison.Ordinal);
    }

    [Fact]
    public void ZOBR02_atanmamis_has_EKSI_isaret_korunur()
    {
        atanmamisMadenJson = M("-2.5");
        var k = Kart();
        Assert.Matches(@"Altın: [−-]2,5000 has", k);
    }

    [Fact]
    public void ZOBR03_reyon_has_0_0001_kaybolmaz()
    {
        madenJson = M("0.0001");
        Assert.Contains("Altın: 0,0001 has", Kart(), StringComparison.Ordinal);
    }

    [Fact]
    public void ZOBR04_reyon_has_5_hane_AwayFromZero_0_00005_0_0001_olur()
    {
        // Sunucu 6 hane yollar; ekran 4 hane gösterir. 0,00005 → 0,0001 (yarım yukarı), 0,00004 → 0,0000.
        madenJson = M("0.00005");
        atanmamisMadenJson = M("0.00004");
        var k = Kart();
        Assert.Contains("Altın: 0,0001 has", k, StringComparison.Ordinal);
        Assert.Contains("Altın: 0,0000 has", k, StringComparison.Ordinal);
    }

    [Fact]
    public void ZOBR05_reyon_has_cok_buyuk_tam_yazilir_kirpilmaz()
    {
        madenJson = M("999999999.9999");
        var k = Kart();
        // Bicim.Has binlik gruplamasız (T-K1/P-57 açık kalanı) — rakamlar eksiksiz olmalı.
        Assert.True(k.Contains("999.999.999,9999 has") || k.Contains("999.999.999,9999 has"), k);
    }

    [Fact]
    public void ZOBR06_madenler_null_ya_da_bos_cokmez_satir_cizilir()
    {
        madenJson = "null";
        atanmamisMadenJson = "[]";
        var k = Kart();
        Assert.Contains("Vitrin 1", k, StringComparison.Ordinal);
        Assert.DoesNotContain(" g", k.Replace("\n", " ").Split("Vitrin 1")[1].Split("Merkez")[0], StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ZO-BELGE 29.09 TURU — HASIM DOĞRULAYICI (ikinci tur) SINIR TESTLERİ
// Bu turda İşlem defteri sayfa boyu 10 → 6 indirildi (GorselSigmaSozlugu).
// Sınanan: sunucuya giden limit/ofset SABİT (6 ve 0/6/12), aralık metni
// binlik ayraçlı ("1.234"), son sayfa kısmi dolu, 0 kayıtta aralık değil toplam,
// net has 4 hane ve eksi işaret, 999.999.999,99 tam. Beklenenler SABİT metin.
// ═════════════════════════════════════════════════════════════════════════════
// ZO-BELGE 29.09 devam 6 (tasarım hakemi KRİTİK): sayfa boyu 6 → 25; sınır senaryoları aynı, sayılar 25 tabanlı.
public sealed class ZOBELGESayfaSinirTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private int toplam = 1234;
    private string ozet = """{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0}""";

    public ZOBELGESayfaSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private string Defter(int ofset, int limit)
    {
        var adet = Math.Max(0, Math.Min(limit, toplam - ofset));
        var satirlar = string.Join(",", Enumerable.Range(ofset, adet).Select(i =>
            $$"""{"id":"h{{i}}","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1,"hasGrami":0}"""));
        var daha = ofset + adet < toplam ? "true" : "false";
        return $$"""
            {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":{{toplam}},"ofset":{{ofset}},"limit":{{limit}},"dahaFazlasiVar":{{daha}},
             "ozet":{{ozet}},"storno":{"adet":0,"tutarTl":0},"satirlar":[{{satirlar}}]}
            """;
    }

    private sealed class Sahte(ZOBELGESayfaSinirTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var q = System.Web.HttpUtility.ParseQueryString(istek.RequestUri.Query);
            t.istekler.Add(istek.RequestUri.PathAndQuery);
            var g = yol switch
            {
                var x when x.EndsWith("/raporlar/islem-defteri") =>
                    t.Defter(int.Parse(q["ofset"] ?? "0"), int.Parse(q["limit"] ?? "100")),
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static string N(string s) => s.Replace(' ', ' ').Trim();

    [Fact]
    public void ZOBP01_ilk_sayfa_limit_25_ofset_0_aralik_binlik_ayracli()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-sinir");
        Assert.Contains(istekler, i => i.Contains("/raporlar/islem-defteri?") && i.Contains("limit=25") && i.Contains("ofset=0"));
        Assert.Equal("1–25 / 1.234 kayıt", N(c.Find("#id-sinir").TextContent));
        Assert.Equal(25, c.FindAll("#id-tablo tbody tr").Count);
        Assert.True(c.Find("#id-onceki").HasAttribute("disabled"));
    }

    [Fact]
    public void ZOBP02_sonraki_ofset_25_ikinci_sayfa_26_50()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-sonraki");
        c.Find("#id-sonraki").Click();
        c.WaitForAssertion(() => Assert.Equal("26–50 / 1.234 kayıt", N(c.Find("#id-sinir").TextContent)));
        Assert.Contains(istekler, i => i.Contains("limit=25") && i.Contains("ofset=25"));
        Assert.False(c.Find("#id-onceki").HasAttribute("disabled"));
    }

    [Fact]
    public void ZOBP03_son_sayfa_kismi_51_kayitta_51_51_ve_sonraki_kilitli()
    {
        toplam = 51;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-sonraki");
        c.Find("#id-sonraki").Click();
        c.WaitForAssertion(() => Assert.Equal("26–50 / 51 kayıt", N(c.Find("#id-sinir").TextContent)));
        c.Find("#id-sonraki").Click();
        c.WaitForAssertion(() => Assert.Equal("51–51 / 51 kayıt", N(c.Find("#id-sinir").TextContent)));
        Assert.Single(c.FindAll("#id-tablo tbody tr"));
        Assert.True(c.Find("#id-sonraki").HasAttribute("disabled"));
    }

    [Fact]
    public void ZOBP04_sifir_kayitta_aralik_degil_toplam_yazar_sayfalama_yok()
    {
        toplam = 0;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-sinir");
        Assert.DoesNotContain("–", c.Find("#id-sinir").TextContent);
        Assert.Contains("0", c.Find("#id-sinir").TextContent);
        Assert.Empty(c.FindAll("#id-sonraki"));
    }

    [Fact]
    public void ZOBP05_ozet_eksi_net_has_4_hane_ve_buyuk_tutar_sayfa_degisince_ayni()
    {
        ozet = """{"tlGiris":999999999.99,"tlCikis":0.005,"hasGiris":0.0001,"hasCikis":0.0003,"satirSayisi":1234}""";
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-ozet-has");
        var has1 = N(c.Find("#id-ozet-has").TextContent);
        Assert.EndsWith("-0,0002 has", has1);
        Assert.Contains("999.999.999,99", N(c.Find("#id-ozet-giris-cikis").TextContent));
        Assert.Contains("0,01", N(c.Find("#id-ozet-giris-cikis").TextContent));   // 0,005 AwayFromZero
        c.Find("#id-sonraki").Click();
        c.WaitForAssertion(() => Assert.StartsWith("26–50", N(c.Find("#id-sinir").TextContent)));
        Assert.Equal(has1, N(c.Find("#id-ozet-has").TextContent));   // özet sayfaya göre DEĞİŞMEZ
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — DOĞRULAYICI TUR 3 (29.09) ek sınırları: BASİT "bugün" has
// gösterimi (BugunHas → Bicim.Has F4). Beklenen metinler SABİT yazıldı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZOBELGESinirTestleriTur3 : BunitContext
{
    public ZOBELGESinirTestleriTur3()
    {
        Services.AddScoped<DilServisi>();
    }

    private string BugunHas(decimal gram)
    {
        var r = new Kuyumcu.Web.Pages.Raporlar();
        typeof(Kuyumcu.Web.Pages.Raporlar).GetProperty("Dil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .SetValue(r, Services.GetRequiredService<DilServisi>());
        var m = typeof(Kuyumcu.Web.Pages.Raporlar).GetMethod("BugunHas", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((string)m.Invoke(r, [gram])!).Replace(' ', ' ');
    }

    [Fact]
    public void ZOBT3_01_bugun_has_SIFIR_isaretsiz_dort_hane() =>
        Assert.Equal("0,0000 has", BugunHas(0m));

    [Fact]
    public void ZOBT3_02_bugun_has_0_0001_kaybolmaz() =>
        Assert.Equal("0,0001 has", BugunHas(0.0001m));

    [Fact]
    public void ZOBT3_03_bugun_has_eksi_0_0001_isaretli() =>
        Assert.Equal("−0,0001 has", BugunHas(-0.0001m));

    [Fact]
    public void ZOBT3_04_bugun_has_5_hane_yarim_AwayFromZero_yuvarlanir() =>
        Assert.Equal("1,0001 has", BugunHas(1.00005m));

    [Fact]
    public void ZOBT3_05_bugun_has_cok_buyuk_binliksiz_tam() =>
        Assert.Equal("999.999.999,9999 has", BugunHas(999999999.9999m));

    [Fact]
    public void ZOBT3_06_bugun_has_eksi_buyuk_isaret_korunur() =>
        Assert.Equal("−123,4560 has", BugunHas(-123.456m));
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE (tur 29.09, hasım doğrulayıcı) — /faturalar YENİ FATURA
// "Toplam" alanı ham type=number → Form/Para (BICIM-2/ZO-BELGE) geçişinin
// finansal sınırları. Ölçülen şey POST /faturalar gövdesindeki "toplam" değeri
// (JSON ayrıştırılarak) ya da isteğin HİÇ gitmediği + alan altı hata.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOBELGEFaturaToplamSinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ZOBELGEFaturaToplamSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(ZOBELGEFaturaToplamSinirTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when istek.Method == HttpMethod.Post && v.StartsWith("/api/v1/faturalar", StringComparison.Ordinal)
                    => """{"id":"f9","faturaNo":"FTR-2026-000009"}""",
                var v when v.Contains("/ozel-matrah-ayari") => """
                    {"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,
                     "kaynakBeyani":"KAPALI","maliMusavirTeyidi":false}
                    """,
                var v when v.StartsWith("/api/v1/faturalar", StringComparison.Ordinal) && istek.Method == HttpMethod.Get => """
                    {"faturalar":[{"id":"f1","faturaNo":"FTR-2026-000001","cariAd":"Ayşe Demir",
                      "tur":"SATIS","araToplam":0.84,"kdvOran":0.2,"kdvTutar":0.17,
                      "toplam":1.005,"durum":"TASLAK","bankaHareketId":null,
                      "tarih":"2026-09-16T08:00:00Z","kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE"}]}
                    """,
                var v when v.Contains("/urunler") => "[]",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> YeniAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-yeni");
        e.Find("#fk-yeni").Click();
        e.WaitForElement("#fatura-toplam");
        e.Find("#fatura-cari").Click();
        e.Find("#fatura-cari-liste li[data-deger='c1']").Click();
        return e;
    }

    private void Gonder(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e, string metin)
    {
        e.Find("#fatura-toplam").Input(metin);
        istekler.Clear();
        e.Find("#fk-kaydet").Click();
    }

    private decimal PostToplam(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e)
    {
        decimal sonuc = -1m;
        e.WaitForAssertion(() =>
        {
            var post = istekler.Single(i => i.Metot == "POST");
            sonuc = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement.GetProperty("toplam").GetDecimal();
        });
        return sonuc;
    }

    private void PostYokHataVar(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e)
    {
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-fatura-toplam")));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact] public void ZOBF01_toplam_SIFIR_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, "0"); PostYokHataVar(e); }
    [Fact] public void ZOBF02_toplam_EKSI_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, "-5"); PostYokHataVar(e); }
    [Fact] public void ZOBF03_toplam_BOS_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, ""); PostYokHataVar(e); }
    [Fact] public void ZOBF04_toplam_belirsiz_1_500_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, "1.500"); PostYokHataVar(e); }
    [Fact] public void ZOBF05_toplam_45_000_binlik_45000_gider() { var e = YeniAc(); Gonder(e, "45.000"); Assert.Equal(45000m, PostToplam(e)); }
    [Fact] public void ZOBF06_toplam_1_virgul_5_bir_bucuk_gider() { var e = YeniAc(); Gonder(e, "1,5"); Assert.Equal(1.5m, PostToplam(e)); }
    [Fact] public void ZOBF07_toplam_cok_buyuk_tam_gider() { var e = YeniAc(); Gonder(e, "999.999.999,99"); Assert.Equal(999_999_999.99m, PostToplam(e)); }
    [Fact] public void ZOBF08_toplam_0_01_kurus_alt_sinir_kabul() { var e = YeniAc(); Gonder(e, "0,01"); Assert.Equal(0.01m, PostToplam(e)); }

    // ── Doğrulayıcı 30.09 (ZO-BELGE tur 4): Faturalar yeni-fatura çekmecesi bu turda değişti ──
    // Alan EnAz=0.01 bildiriyor ama ortak SayisalAlan EnAz'ı UYGULAMIYOR (doğrulayıcı bulgusu, ORTA):
    // kuruş ALTI tutar istemciden geçer, sunucu [Range(0.01,…)] 400 ile reddeder (canlı 5198: 400 kanıtlı).
    // Buradaki kilit: istemci tutarı SESSİZCE 0,01'e YUVARLAYIP kullanıcının yazmadığı fatura KESMEZ —
    // gövdedeki toplam yazılanla birebir aynı (sunucu kapısı devreye girer).
    [Fact] public void ZOBF10_toplam_0_005_sessizce_0_01e_yuvarlanmaz_sunucu_kapisina_aynen_gider() { var e = YeniAc(); Gonder(e, "0,005"); Assert.Equal(0.005m, PostToplam(e)); }
    [Fact] public void ZOBF11_toplam_0_004_sessizce_0_01e_yuvarlanmaz_sunucu_kapisina_aynen_gider() { var e = YeniAc(); Gonder(e, "0,004"); Assert.Equal(0.004m, PostToplam(e)); }
    [Fact] public void ZOBF12_toplam_yalniz_bosluk_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, "   "); PostYokHataVar(e); }
    [Fact] public void ZOBF13_toplam_unicode_eksi_reddedilir_istek_gitmez() { var e = YeniAc(); Gonder(e, "−5"); PostYokHataVar(e); }
    [Fact] public void ZOBF14_toplam_45_000_virgul_50_binlik_ve_kurus_gider() { var e = YeniAc(); Gonder(e, "45.000,50"); Assert.Equal(45000.50m, PostToplam(e)); }

    [Fact]
    public void ZOBF09_liste_toplam_1_005_AwayFromZero_1_01_yazar()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        Assert.Contains("1,01", e.Markup);
        Assert.DoesNotContain("1,005", e.Markup);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE (tur 4 · 29.09, hasım doğrulayıcı) — /evraklar ÖZET SATIRI ve
// /raporlar/islem-defteri SATIR has/tutar sınırları. Beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOBELGESinirTestleriTur4 : BunitContext
{
    private string evrak = "{}";
    private string defter = "{}";

    public ZOBELGESinirTestleriTur4()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class Sahte(ZOBELGESinirTestleriTur4 t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var x when x.EndsWith("/evraklar") => t.evrak,
                var x when x.EndsWith("/raporlar/islem-defteri") => t.defter,
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => "[]",
                var x when x.Contains("/cari") => "[]",
                var x when x.EndsWith("/bankalar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static string Evrak(string portfoy, string karsiliksiz) => $$"""
        {"toplamAdet":1,"toplamTutar":{{portfoy}},"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":{{portfoy}},"tahsileVerilenTutar":0,
         "buHaftaVadeliTutar":0,"karsiliksizTutar":{{karsiliksiz}},
         "satirlar":[{"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":{{portfoy}},
            "paraBirimi":"TRY","vadeGunu":"2026-12-10","evrakNo":"CEK-1","bankaAdi":null,"bankaSube":null,
            "kesideYeri":null,"kesideTarihi":null,"borcluCariId":null,"borcluAd":null,"kaynakCariId":"c1",
            "kaynakCariAd":"Ayşe","notlar":null,"gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,
            "duzenlenebilir":true,"izinliGecisler":[]}]}
        """;

    private string EvrakOzet(string id)
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        c.WaitForElement("#" + id);
        return c.Find("#" + id).TextContent.Replace("\u00A0", " ").Replace("\u202F", " ");
    }

    [Fact] public void ZOBT4_01_evrak_portfoy_SIFIR() { evrak = Evrak("0", "0"); Assert.Contains("0 ₺", EvrakOzet("evrak-ozet-portfoy")); }
    [Fact] public void ZOBT4_02_evrak_portfoy_cok_buyuk_tam() { evrak = Evrak("999999999.99", "0"); Assert.Contains("999.999.999,99 ₺", EvrakOzet("evrak-ozet-portfoy")); }
    [Fact] public void ZOBT4_03_evrak_portfoy_kurus_1_005_AwayFromZero() { evrak = Evrak("1.005", "0"); var o = EvrakOzet("evrak-ozet-portfoy"); Assert.Contains("1,01 ₺", o); Assert.DoesNotContain("1,005", o); }
    [Fact] public void ZOBT4_04_evrak_karsiliksiz_kurus_0_004_sifira_iner() { evrak = Evrak("0", "0.004"); Assert.Contains("0 ₺", EvrakOzet("evrak-ozet-karsiliksiz")); }

    private static string Defter(string tl, string has) => $$"""
        {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":1,"ofset":0,"limit":25,"dahaFazlasiVar":false,
         "ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":1},"storno":null,
         "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"ALIS","odemeTuru":"NAKIT","tlTutari":{{tl}},"hasGrami":{{has}}}]}
        """;

    private string DefterMarkup()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForAssertion(() => Assert.Contains("id-ozet-has", c.Markup));
        return c.Markup.Replace("\u00A0", " ").Replace("\u202F", " ");
    }

    [Fact] public void ZOBT4_05_defter_satir_has_5_hane_AwayFromZero_0_0001() { defter = Defter("0", "0.00005"); Assert.Contains("0,0001 has", DefterMarkup()); }
    [Fact] public void ZOBT4_06_defter_satir_eksi_tutar_isareti_korunur() { defter = Defter("-200.50", "0"); var m = DefterMarkup(); Assert.Matches(@"[−-]\s?200,50 ₺|[−-]₺?200,50", m); }
}

/// <summary>Hasım doğrulayıcı 29.09 (DEVAM 7+8): elle kasa hareketi denetim metninin sade çevirisi
/// (ZoBelgeDenetimSozlugu.KasaHareketiMetni + Bicim.Para) — finansal sınırlar. Beklenen değerler
/// SABİT yazılmıştır (üretim kodundan hesaplanmaz).</summary>
public sealed class ZOBELGEKasaMetniSinirTestleri
{
    private static string? Cevir(string? detay, string dil = "tr") =>
        ZoBelgeDenetimSozlugu.KasaHareketiMetni(detay, dil, Bicim.Para)?
            .Replace(' ', ' ').Replace(' ', ' ').Replace('−', '-');

    [Fact] public void ZOBK01_sifir_tutar_sifir_TL_yazar() =>
        Assert.Equal("Kasaya elle giriş: 0 ₺", Cevir("elle kasa hareketi: GIRIS 0 TRY"));

    [Fact] public void ZOBK02_eksi_tutar_isaret_korunur() =>
        Assert.Equal("Kasadan elle çıkış: -25,75 $", Cevir("elle kasa hareketi: CIKIS -25.75 USD"));

    [Fact] public void ZOBK03_kurus_0_005_AwayFromZero_0_01_olur() =>
        Assert.Equal("Kasaya elle giriş: 0,01 ₺", Cevir("elle kasa hareketi: GIRIS 0.005 TRY"));

    [Fact] public void ZOBK04_kurus_0_004_sifira_yuvarlanir() =>
        Assert.Equal("Kasaya elle giriş: 0 ₺", Cevir("elle kasa hareketi: GIRIS 0.004 TRY"));

    [Fact] public void ZOBK05_cok_buyuk_tutar_tam_yazilir() =>
        Assert.Equal("Kasaya elle giriş: 999.999.999,99 ₺ · açıklama: x", Cevir("elle kasa hareketi: GIRIS 999999999.99 TRY · açıklama: x"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ZOBK06_bos_ya_da_null_detay_dokunulmaz(string? detay) => Assert.Null(Cevir(detay));

    [Fact] public void ZOBK07_binlik_ayracli_belirsiz_ham_tutar_cevrilmez_kayit_aynen_kalir() =>
        Assert.Null(Cevir("elle kasa hareketi: GIRIS 1,500.50 TRY"));

    [Fact] public void ZOBK08_EN_ceviride_de_ayni_para_bicimi_ve_not_eki() =>
        Assert.Equal("Cash out (manual): 1.500,50 ₺ · note: kira", Cevir("elle kasa hareketi: CIKIS 1500.50 TRY · açıklama: kira", "en"));
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (29.09 devam 9, kör inceleme) — "Fişten Fatura Kes" fiş aramasının
// alt satırı artık AramaServisi.EkranPara ile biçimleniyor. EkranPara İŞARETSİZDİR
// (Math.Abs) — çağıran işareti kendisi yazmalı. FisAra işaret yazmıyor; L-1b alış
// fişi kalemleri FisId taşır ve TlTutari NEGATİFTİR (SatisModulu: Yuvarla2(-toplamTl)).
// Beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class ZOBELGEAramaSinirTestleri(HttpFikstur f)
{
    private static string P(decimal n, string? pb) => Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(n, pb);

    [Fact] public void DZB_sifir() => Assert.Equal("0 ₺", P(0m, "TRY"));
    [Fact] public void DZB_null_pb_TL() => Assert.Equal("1.250 ₺", P(1250m, null));
    [Fact] public void DZB_kurus_0005_yukari() => Assert.Equal("0,01 ₺", P(0.005m, "TRY"));
    [Fact] public void DZB_kurus_0004_asagi() => Assert.Equal("0 ₺", P(0.004m, "TRY"));
    [Fact] public void DZB_cok_buyuk() => Assert.Equal("999.999.999,99 ₺", P(999_999_999.99m, "TRY"));

    private void Satir(Kuyumcu.Tests.Kiraci k, string fisId, string tur, decimal tl)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        db.Hareketler.Add(new Kuyumcu.Api.Data.Hareket
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = tur, Tarih = DateTime.UtcNow, Aciklama = "dzb isaret",
            TlTutari = tl, HasGrami = 0m, ParaBirimi = "TRY",
            CariId = k.CariId, FisId = fisId, KullaniciId = k.KullaniciId, KurSnapshotId = k.KurId,
            Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    // Eksi: alış fişi (TlTutari −) arama sonucunda satış fişiyle AYNI görünmemeli — işaret korunmalı.
    [Fact]
    public async Task DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz()
    {
        Kuyumcu.Tests.Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "DZB isaret");
        Satir(k, "FIS-DZB-ALIS", "ALIS", -2100.50m);
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/arama?q=FIS-DZB&tur=FIS");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var y = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<Kuyumcu.Contracts.Arama.AramaYaniti>(cevap.Content,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)))!;
        var a = Assert.Single(y.Sonuclar, s => s.Kimlik == "FIS-DZB-ALIS");
        Assert.Equal("1 kalem · −2.100,50 ₺", a.AltBilgi);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (29.09 devam 10, kör inceleme) — "Fişten Fatura Kes" fiş arama
// alt satırı: işaret NET fiş toplamından (satış + alış karışık fiş), sıfır toplamda
// işaret YOK, kuruş ve büyük tutar tr-TR; sunucu metni Web Bicim.Para ile birebir.
// Beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class ZOBELGEAramaSinirTestleri2(HttpFikstur f)
{
    private void Satir(Kuyumcu.Tests.Kiraci k, string fisId, string tur, decimal tl)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        db.Hareketler.Add(new Kuyumcu.Api.Data.Hareket
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = tur, Tarih = DateTime.UtcNow, Aciklama = "dzb2",
            TlTutari = tl, HasGrami = 0m, ParaBirimi = "TRY",
            CariId = k.CariId, FisId = fisId, KullaniciId = k.KullaniciId, KurSnapshotId = k.KurId,
            Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private async Task<string> Alt(Kuyumcu.Tests.Kiraci k, string q, string fisId)
    {
        var cevap = await f.Ortam.Istemci(k).GetAsync($"/api/v1/arama?q={q}&tur=FIS");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var y = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<Kuyumcu.Contracts.Arama.AramaYaniti>(cevap.Content,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)))!;
        return Assert.Single(y.Sonuclar, s => s.Kimlik == fisId).AltBilgi!;
    }

    [Fact]
    public async Task DZB2_karisik_fis_net_eksi_sifir_isaretsiz_kurus_ve_buyuk_tutar()
    {
        Kuyumcu.Tests.Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "DZB2 sinir");
        // Satış + alış aynı fişte (bozdurup alma): net −500,50.
        Satir(k, "FIS-DZ2-KAR", "SATIS", 1000m);
        Satir(k, "FIS-DZ2-KAR", "ALIS", -1500.50m);
        // Sıfır net: işaret basılmaz, "−0" YOK.
        Satir(k, "FIS-DZ2-SIF", "SATIS", 250m);
        Satir(k, "FIS-DZ2-SIF", "ALIS", -250m);
        // Tek kuruş eksi.
        Satir(k, "FIS-DZ2-KRS", "ALIS", -0.01m);
        // Çok büyük tutar.
        Satir(k, "FIS-DZ2-BYK", "SATIS", 999_999_999.99m);

        Assert.Equal("2 kalem · −500,50 ₺", await Alt(k, "FIS-DZ2", "FIS-DZ2-KAR"));
        Assert.Equal("2 kalem · 0 ₺", await Alt(k, "FIS-DZ2", "FIS-DZ2-SIF"));
        Assert.Equal("1 kalem · −0,01 ₺", await Alt(k, "FIS-DZ2", "FIS-DZ2-KRS"));
        Assert.Equal("1 kalem · 999.999.999,99 ₺", await Alt(k, "FIS-DZ2", "FIS-DZ2-BYK"));
    }

    // Sunucu metni ("−" + EkranPara) ekranın kendi biçimiyle (Bicim.Para) BİREBİR — eksi dahil.
    [Theory]
    [InlineData("-2100.50", "−2.100,50 ₺")]
    [InlineData("-0.005", "−0,01 ₺")]
    [InlineData("48790", "48.790 ₺")]
    public void DZB2_sunucu_isaretli_metin_Web_Bicim_ile_ayni(string girdi, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        var sunucu = (n < 0 ? "−" : "") + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(n, "TRY");
        Assert.Equal(beklenen, sunucu);
        Assert.Equal(beklenen, Kuyumcu.Web.Servisler.Bicim.Para(n, "TRY"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 4 (29.09 22:4x) — FİŞTEN FATURA KES ÖNİZLEME TOPLAMI
// Önizleme "Toplam" (#ff-toplam = Faturalar.FistenToplam) kesilecek faturanın
// toplamıyla ve tablodaki satırların toplamıyla AYNI olmalı. Beklenenler SABİT.
// Fiş okuma ucu toplamTl'yi İŞARETLİ toplar (SatisModulu FisOku: Σ TlTutari);
// sunucu faturayı kalem başına Math.Abs ile keser.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEFistenOnizlemeSinirTestleri(TestVeritabani vt)
{
    private static Kuyumcu.Contracts.Sayfalar.Faturalar.FisYaniti Fis(decimal toplamTl, params (string Tur, decimal Tl)[] k) =>
        new("F", "C", "Ayşe", null, k.Length, toplamTl, 0m,
            k.Select((x, i) => new Kuyumcu.Contracts.Sayfalar.Faturalar.FisKalemVeri(
                "h" + i, x.Tur, x.Tl, 0m, false, "Ürün", null)).ToList());

    [Fact] public void DZB4_01_sifir_toplam_sifir() =>
        Assert.Equal(0m, Kuyumcu.Web.Pages.Faturalar.FistenToplam(Fis(0m)));
    [Fact] public void DZB4_02_alis_fisi_eksi_toplam_isaretsiz() =>
        Assert.Equal(2100.50m, Kuyumcu.Web.Pages.Faturalar.FistenToplam(Fis(-2100.50m, ("ALIS", -1500.50m), ("HURDA_ALIS", -600m))));
    [Fact] public void DZB4_03_cok_buyuk_tam() =>
        Assert.Equal(999_999_999.99m, Kuyumcu.Web.Pages.Faturalar.FistenToplam(Fis(999_999_999.99m, ("SATIS", 999_999_999.99m))));

    [Fact] public void DZB4_04_alis_fisi_kurus_0_005_ekranda_AwayFromZero_0_01() =>
        Assert.Equal("0,01 ₺", Kuyumcu.Web.Servisler.Bicim.Tl(Kuyumcu.Web.Pages.Faturalar.FistenToplam(Fis(-0.005m, ("ALIS", -0.005m)))).Replace('\u00A0', ' ').Replace('\u202F', ' '));

    /// <summary>Canlıda ölçülen gerçek akış (29.09 r4): eski altınla satışta HURDA_ALIS satırı fis_id
    /// TAŞIMAZ (SatisModulu.AltinBacagi.cs "FisId BİLEREK BOŞ"); fiş yalnız SATIS kalemidir → önizleme
    /// toplamı = satış kalemi = kesilen fatura toplamı (30.000). Hediye (0 TL) toplamı değiştirmez.</summary>
    [Fact] public void DZB4_05_satis_ve_hediye_kalemli_fis_toplami_30000() =>
        Assert.Equal(30_000m, Kuyumcu.Web.Pages.Faturalar.FistenToplam(Fis(30_000m, ("SATIS", 30_000m), ("SATIS", 0m))));

    /// <summary>Karışık fiş (SATIS + HURDA_ALIS aynı fis_id) üretim akışında OLUŞMAZ ama sunucu onu
    /// kabul eder (FistenTurTuret null → SATIS) ve kalem başına Math.Abs ile keser: 3.000 + 500 = 3.500.
    /// Önizleme ise İŞARETLİ fiş toplamını gösterir (2.500). Bu test sunucu tarafını SABİTLER; önizleme
    /// farkı raporda DÜŞÜK bulgu olarak yazıldı (yalnız doğrudan defter yazımıyla erişilebilir).</summary>
    [Fact]
    public async Task DZB4_06_karisik_fisten_kesilen_faturanin_toplami_3500()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "DZB4 karışık fiş");
        var fisId = Kuyumcu.Api.Common.Kimlik.Ulid();
        var now = DateTime.UtcNow;
        foreach (var (tur, tl, has) in new[] { ("SATIS", 3_000m, -2m), ("HURDA_ALIS", -500m, 0.9m) })
            kurulum.Hareketler.Add(new Kuyumcu.Api.Data.Hareket
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur, Tarih = now,
                TlTutari = tl, HasGrami = has, OdemeTuru = "NAKIT", NakitTutar = tl, VeresiyeTutar = 0m,
                FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db).FistenOlustur(
            new Kuyumcu.Api.Modules.Fatura.FaturaFistenDto { FisId = fisId, Tur = "SATIS", KdvOran = 0.20m }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(3_500m, kontrol.Faturalar.Single(f => f.TenantId == k.TenantId).Toplam);
    }
}

/// <summary>
/// HASIM DOĞRULAYICI (ZO-BELGE d14, 30.09) — fişten kalem miktarı/birim fiyatı
/// (FistenKalemMiktari / FistenKalemFiyati) ve binlikli para kutusu metninin
/// FormAlani.SayiCoz ile gidiş-dönüşü. Beklenen değerler ELLE hesaplanmıştır.
/// </summary>
public sealed class ZOBELGEd14KalemSinirTestleri
{
    private static (decimal Birim, decimal? Ind) F(decimal t, decimal m) =>
        Kuyumcu.Api.Modules.Fatura.FaturaServisi.FistenKalemFiyati(t, m);

    private static decimal Y2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    [Fact]
    public void ZOB14_01_sifir_tutar_birim_sifir_indirim_yok()
    {
        var (b, i) = F(0m, 1m);
        Assert.Equal(0m, b);
        Assert.Null(i);
    }

    [Fact]
    public void ZOB14_02_eksi_tutar_mutlak_degerle_bolunur_iade_satiri()
    {
        var (b, i) = F(-100m, 3m);
        Assert.Equal(33.34m, b);
        Assert.Equal(0.02m, i);
    }

    [Fact]
    public void ZOB14_03_kurus_sinir_0_005_AwayFromZero_0_01_olur()
    {
        var (b, i) = F(0.005m, 1m);
        Assert.Equal(0.01m, b);
        Assert.Null(i);
    }

    [Fact]
    public void ZOB14_04_cok_buyuk_tutar_999_999_999_99_tek_adet_kaybolmaz()
    {
        var (b, i) = F(999_999_999.99m, 1m);
        Assert.Equal(999_999_999.99m, b);
        Assert.Null(i);
    }

    [Fact]
    public void ZOB14_05_cok_buyuk_tutar_7_adet_bolunmez_satir_toplami_korunur()
    {
        // 999.999.999,99 ÷ 7 = 142.857.142,855714… → 142.857.142,86; 7 × = 999.999.999,99+0,03
        var (b, i) = F(999_999_999.99m, 7m);
        Assert.Equal(142_857_142.86m, b);
        Assert.Equal(0.03m, i);
        Assert.Equal(999_999_999.99m, Y2(7m * b - (i ?? 0m)));
    }

    [Fact]
    public void ZOB14_06_gram_4_hane_miktar_0_0001()
    {
        var (b, i) = F(1m, 0.0001m);
        Assert.Equal(10_000m, b);
        Assert.Null(i);
    }

    [Fact]
    public void ZOB14_07_miktar_sifir_ya_da_eksi_1_sayilir()
    {
        Assert.Equal((250m, (decimal?)null), F(250m, 0m));
        Assert.Equal((250m, (decimal?)null), F(250m, -3m));
    }

    [Fact]
    public void ZOB14_08_degismez_1000_rastgele_ornekte_satir_toplami_defterdeki_tutar_ve_indirim_miktar_kurusundan_az()
    {
        var r = new Random(20260930);
        for (var n = 0; n < 1000; n++)
        {
            var t = Math.Round((decimal)r.NextDouble() * 2_000_000m, 2);
            if (t == 0m) continue;
            var m = r.Next(0, 3) == 0 ? Math.Round((decimal)r.NextDouble() * 50m + 0.001m, 3) : r.Next(1, 500);
            var (b, i) = F(t, m);
            Assert.Equal(t, Y2(m * b - (i ?? 0m)));
            Assert.True((i ?? 0m) >= 0m);
            Assert.True((i ?? 0m) <= Y2(m * 0.01m) + 0.01m, $"t={t} m={m} ind={i}");
        }
    }

    [Fact]
    public void ZOB14_09_miktar_adet_eksi_ise_has_bolu_birim_has_turetilir()
    {
        var h = new Kuyumcu.Api.Data.Hareket { HasGrami = 18.32m, Adet = -2m };
        Assert.Equal(2m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.FistenKalemMiktari(h, 9.16m, "ADET"));
    }

    [Fact]
    public void ZOB14_10_miktar_has_sifir_ve_adet_yoksa_1_null_urun()
    {
        var h = new Kuyumcu.Api.Data.Hareket { HasGrami = 0m, Adet = null };
        Assert.Equal(1m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.FistenKalemMiktari(h, 9.16m, "ADET"));
        Assert.Equal(1m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.FistenKalemMiktari(h, null, null));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("1.095.125,00", "1095125")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0,0001", "0.0001")]
    public void ZOB14_11_Turkce_giris_SayiCoz(string metin, string beklenen)
    {
        var c = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(metin);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact]
    public void ZOB14_12_belirsiz_1_500_ve_bos_reddedilir()
    {
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Belirsiz,
            Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Bos,
            Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(null).Sonuc);
    }

    [Theory]
    [InlineData("1500", "1.500,00")]
    [InlineData("0", "0,00")]
    [InlineData("999999999.99", "999.999.999,99")]
    [InlineData("0.005", "0,01")]
    public void ZOB14_13_para_kutusu_kalibi_gidis_donus_belirsizlik_dogurmaz(string deger, string metin)
    {
        var d = decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
        var g = Kuyumcu.Web.Servisler.Bicim.Girdi(d, "#,##0.00");
        Assert.Equal(metin, g);
        Assert.Equal(Y2(d), Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(g).Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (DEVAM 14 · Y4-D K-10b) — özel matrahta MADEN SÜZGECİ
// sınır testleri [DB]. Beklenen değerler ELLE hesaplanmış sabitlerdir
// (üretim kodundan türetilmez). Kur: TestKurulum.KiraciKur = 6.827,20 TL/has g.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEMadenSuzgeciSinirTestleri(TestVeritabani vt)
{
    private static Kuyumcu.Api.Data.Hareket Satir(Kiraci k, string fisId, decimal tl, decimal hasGram, string maden)
    {
        var now = DateTime.UtcNow;
        return new Kuyumcu.Api.Data.Hareket
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = tl, HasGrami = -hasGram, Maden = maden,
            OdemeTuru = "HAVALE", NakitTutar = 0m, VeresiyeTutar = 0m, Adet = 1m,
            FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId,
            UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        };
    }

    private static string GumusUrun(Kuyumcu.Api.Data.KuyumcuDbContext db, Kiraci k)
    {
        var id = Kuyumcu.Api.Common.Kimlik.Ulid(); var now = DateTime.UtcNow;
        db.Urunler.Add(new Kuyumcu.Api.Data.Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "GUMUS", Kod = $"G-{id[..8]}", Ad = "925 Gümüş Yüzük",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 10m, TasAgirlik = 0m, HasGrami = 9.25m,
            Birim = "ADET", IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        return id;
    }

    private async Task<Kuyumcu.Api.Data.Fatura> HareketFaturasi(string ad, decimal toplam, params (decimal tl, decimal has, string maden)[] satirlar)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, ad);
        var fis = Kuyumcu.Api.Common.Kimlik.Ulid();
        var hs = satirlar.Select(s => Satir(k, fis, s.tl, s.has, s.maden)).ToList();
        kurulum.Hareketler.AddRange(hs);
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db).Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = toplam, KdvOran = 0.20m, KdvTuru = "KUYUM", HareketId = hs[0].Id,
        }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        return kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
    }

    [Fact]
    public async Task ZOBM01_gumus_has_0_0001_dusulmez_KDV_tam()
    {
        var f = await HareketFaturasi("ZOBM01", 1_200m, (1_200m, 0.0001m, Kuyumcu.Core.Domain.Maden.Gumus));
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(200.00m, f.KdvTutar);            // 1200 − 1000
    }

    [Fact]
    public async Task ZOBM02_altin_has_0_0001_dort_hane_dusulur()
    {
        // 0,0001 × 6.827,20 = 0,68272 → 0,68 ; kalan 1.199,32 ; matrah 999,43 ; KDV 199,89
        var f = await HareketFaturasi("ZOBM02", 1_200m, (1_200m, 0.0001m, Kuyumcu.Core.Domain.Maden.Altin));
        Assert.Equal(0.68m, f.HasDeger);
        Assert.Equal(0.0001m, f.HasGram);
        Assert.Equal(199.89m, f.KdvTutar);
    }

    [Fact]
    public async Task ZOBM03_platin_bacagi_altin_fiyatiyla_dusulmez()
    {
        var f = await HareketFaturasi("ZOBM03", 60_000m, (60_000m, 5m, Kuyumcu.Core.Domain.Maden.Platin));
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(10_000.00m, f.KdvTutar);         // 60.000 − 50.000
    }

    [Fact]
    public async Task ZOBM04_gumus_cok_buyuk_tutar_999_999_999_99()
    {
        // 999.999.999,99 / 1,2 = 833.333.333,325 → 833.333.333,33 (AwayFromZero) ; KDV 166.666.666,66
        var f = await HareketFaturasi("ZOBM04", 999_999_999.99m, (999_999_999.99m, 9.25m, Kuyumcu.Core.Domain.Maden.Gumus));
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(166_666_666.66m, f.KdvTutar);
    }

    [Fact]
    public async Task ZOBM05_gumus_kurus_0_005_siniri_AwayFromZero()
    {
        // 0,03 / 1,2 = 0,025 → 0,03 (AwayFromZero) ; KDV 0,00
        var f = await HareketFaturasi("ZOBM05", 0.03m, (0.03m, 1m, Kuyumcu.Core.Domain.Maden.Gumus));
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(0.00m, f.KdvTutar);
        Assert.Equal(0.03m, f.AraToplam);
    }

    [Fact]
    public async Task ZOBM06_sifir_ve_eksi_toplam_reddedilir()
    {
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            HareketFaturasi("ZOBM06a", 0m, (1_000m, 1m, Kuyumcu.Core.Domain.Maden.Gumus)));
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            HareketFaturasi("ZOBM06b", -1_000m, (1_000m, 1m, Kuyumcu.Core.Domain.Maden.Gumus)));
    }

    [Fact]
    public async Task ZOBM07_kuyum_taslakta_gumus_kaleminin_fiyati_DUZENLENEBILIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBM07");
        var gumus = GumusUrun(kurulum, k);
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await servis.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 10_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new Kuyumcu.Api.Modules.Fatura.FaturaKalemDto { UrunId = gumus, Miktar = 1m, BirimFiyat = 10_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        var govde = System.Text.Json.JsonDocument.Parse("{\"birimFiyat\":12000}").RootElement;
        await servis.TaslakKalemGuncelle(fid, kid, govde, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(0m, kalem.HasDeger);
        Assert.Equal(2_000.00m, kalem.KdvTutar);     // 12.000 − 10.000
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Y4-D hedefli düzeltme 2 · 30.09) — 5. yolun (taslak kalem
// düzenleme) maden kaynağı OLUŞTURMA yoluyla aynı mı? Fişten yolda süzgeç
// DEFTERDEKİ hareket madenine (h.Maden) bakar; düzenleme yolu kalemin AyarId'sine.
// Ürünsüz (hurda) gümüş satırında AyarId null → iki yol ayrışır mı? [DB]
// Beklenen değerler ELLE: 10.000 TL KDV dahil, %20, has düşülmez → KDV 1.666,67;
// fiyat 12.000'e çekilince KDV 2.000,00.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEMadenSuzgeciBesinciYolSinirTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task ZOBM08_urunsuz_gumus_hurda_fisten_KUYUM_kalemi_DUZENLENEBILIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBM08");
        var now = DateTime.UtcNow;
        var h = new Kuyumcu.Api.Data.Hareket
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "HURDA_ALIS", Tarih = now, TlTutari = -10_000m, HasGrami = 9.25m,
            Maden = Kuyumcu.Core.Domain.Maden.Gumus, OdemeTuru = "HAVALE", NakitTutar = 0m, VeresiyeTutar = 0m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        };
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await servis.FistenOlustur(new Kuyumcu.Api.Modules.Fatura.FaturaFistenDto
        {
            HareketId = h.Id, KdvTuru = "KUYUM", KdvOran = 0.20m,
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        using (var once = vt.OlusturSistem())
        {
            var k0 = once.FaturaKalemleri.Single(x => x.Id == kid);
            Assert.Equal(0m, k0.HasDeger);             // gümüş: has düşülmedi (oluşturma yolu)
            Assert.Equal(1_666.67m, k0.KdvTutar);
        }
        var govde = System.Text.Json.JsonDocument.Parse("{\"birimFiyat\":12000}").RootElement;
        await servis.TaslakKalemGuncelle(fid, kid, govde, k.Islem);   // 400 atmamalı
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(0m, kalem.HasDeger);
        Assert.Equal(2_000.00m, kalem.KdvTutar);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (hakem 5 · 30.09) — kalem MADEN sütunu (tek kaynak) ve
// taslak düzenlemede BAŞLIK has gramı [DB]. Beklenen değerler ELLE:
// kur 6.827,20 TL/has g; test bilezik 10 g 22 ayar → has 9,16 g/adet.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEHakem5MadenSinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement G(string j) => System.Text.Json.JsonDocument.Parse(j).RootElement;

    private async Task<(Kiraci k, Kuyumcu.Api.Modules.Fatura.FaturaServisi s, Kuyumcu.Api.Data.KuyumcuDbContext db, string fid, string kid)>
        UrunFaturasi(string ad, decimal miktar, decimal birimFiyat, string kdvTuru = "KUYUM")
    {
        var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, ad);
        kurulum.SaveChanges();
        var db = vt.Olustur(k);
        var s = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await s.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = Math.Round(miktar * birimFiyat, 2), KdvOran = 0.20m, KdvTuru = kdvTuru,
            Kalemler = [new Kuyumcu.Api.Modules.Fatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = miktar, BirimFiyat = birimFiyat }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        return (k, s, db, fid, kid);
    }

    [Fact]
    public async Task ZOBH01_urun_kalemi_2_adet_altin_maden_ALTIN_has_bedeli_2_adetten()
    {
        var (k, _, _, fid, kid) = await UrunFaturasi("ZOBH01", 2m, 100_000m);
        Assert.Equal(9.16m, k.UrunHasGrami);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(Kuyumcu.Core.Domain.Maden.Altin, kalem.Maden);
        // 2 × 9,16 × 6.827,20 = 125.074,304 → 125.074,30
        Assert.Equal(125_074.30m, kalem.HasDeger);
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(125_074.30m, f.HasDeger);
    }

    [Fact]
    public async Task ZOBH02_baslik_has_grami_2_adetlik_kalemde_18_32()
    {
        var (_, _, _, fid, _) = await UrunFaturasi("ZOBH02", 2m, 100_000m);
        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(18.32m, f.HasGram);               // 2 adet × 9,16 has g
    }

    [Fact]
    public async Task ZOBH03_taslakta_miktar_1den_2ye_baslik_has_grami_guncellenir()
    {
        var (k, s, _, fid, kid) = await UrunFaturasi("ZOBH03", 1m, 100_000m);
        await s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(125_074.30m, kalem.HasDeger);     // dayanak kuru korunur, has 2 adetten
        Assert.Equal(18.32m, f.HasGram);               // yorum: "miktar düzeltmesi başlığı eski gramla bırakmaz"
    }

    [Fact]
    public async Task ZOBH04_sifir_ve_eksi_duzeltme_reddedilir_maden_korunur()
    {
        var (k, s, _, fid, kid) = await UrunFaturasi("ZOBH04", 1m, 100_000m);
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":-1}"), k.Islem));
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":0}"), k.Islem));
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(Kuyumcu.Core.Domain.Maden.Altin, kalem.Maden);
        Assert.Equal(100_000.00m, kalem.SatirToplam);
    }

    [Fact]
    public async Task ZOBH05_STANDART_faturada_kalem_madeni_yine_yazilir_has_dusulmez()
    {
        var (_, _, _, fid, kid) = await UrunFaturasi("ZOBH05", 1m, 1_200m, "STANDART");
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(Kuyumcu.Core.Domain.Maden.Altin, kalem.Maden);
        Assert.Equal(0m, kalem.HasDeger);
        Assert.Equal(200.00m, kalem.KdvTutar);          // 1.200 × 0,2/1,2
        Assert.Null(kontrol.Faturalar.Single(x => x.Id == fid).OzelMatrahKodu);
    }

    [Fact]
    public async Task ZOBH06_cok_buyuk_tutar_altin_kalem_999_999_999_99()
    {
        var (_, _, _, _, kid) = await UrunFaturasi("ZOBH06", 1m, 999_999_999.99m);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        // has 9,16 × 6.827,20 = 62.537,152 → 62.537,15 ; kalan 999.937.462,84
        // matrah 999.937.462,84/1,2 = 833.281.219,0333 → 833.281.219,03 ; KDV 166.656.243,81
        Assert.Equal(62_537.15m, kalem.HasDeger);
        Assert.Equal(166_656_243.81m, kalem.KdvTutar);
    }

    [Fact]
    public async Task ZOBH07_iscilik_null_PATCH_siler_maden_korunur()
    {
        var (k, s, _, fid, kid) = await UrunFaturasi("ZOBH07", 1m, 100_000m);
        await s.TaslakKalemGuncelle(fid, kid, G("{\"iscilikTl\":500}"), k.Islem);
        await s.TaslakKalemGuncelle(fid, kid, G("{\"iscilikTl\":null}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Null(kalem.IscilikTl);
        Assert.Equal(100_000.00m, kalem.SatirToplam);
        Assert.Equal(Kuyumcu.Core.Domain.Maden.Altin, kalem.Maden);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (30.09 · kalem.HasGrami TEK ANLAM turu) — FİNANSAL SINIRLAR [DB+saf]
// Beklenenler ELLE hesaplandı: kur 6.827,20 TL/has g · test ürünü 9,16 has g/adet.
// Kural (OzelMatrahMotoru başlığı): has bedeli = round2(has_grami × KUR KAYDINDAKİ fiyat),
// AwayFromZero. Taslakta miktar düzeltmesi bu kuru DEĞİŞTİRMEMELİ (kodun kendi yorumu).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEKalemHasSinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement G(string j) => System.Text.Json.JsonDocument.Parse(j).RootElement;

    private async Task<(Kuyumcu.Api.Modules.Fatura.FaturaServisi s, string fid, string kid, Kiraci k, Kuyumcu.Api.Data.KuyumcuDbContext db)>
        Kur1Adet(string ad, decimal miktar = 1m)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, ad);
        kurulum.SaveChanges();
        var db = vt.Olustur(k);
        var s = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await s.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m * miktar, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new Kuyumcu.Api.Modules.Fatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = miktar, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        return (s, fid, kid, k, db);
    }

    private decimal? HasGram(string kid) { using var c = vt.OlusturSistem(); return c.FaturaKalemleri.Single(x => x.Id == kid).HasGrami; }
    private decimal HasDeger(string kid) { using var c = vt.OlusturSistem(); return c.FaturaKalemleri.Single(x => x.Id == kid).HasDeger; }

    [Fact]
    public async Task ZOBHGS01_SIFIR_miktar_PATCH_reddedilir_has_degismez()
    {
        var (s, fid, kid, k, db) = await Kur1Adet("ZOBHGS01");
        using var _ = db;
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":0}"), k.Islem));
        Assert.Equal(9.16m, HasGram(kid));
        Assert.Equal(62_537.15m, HasDeger(kid));   // 9,16 × 6.827,20 = 62.537,152 → ,15
    }

    [Fact]
    public async Task ZOBHGS02_EKSI_miktar_PATCH_reddedilir_has_degismez()
    {
        var (s, fid, kid, k, db) = await Kur1Adet("ZOBHGS02");
        using var _ = db;
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":-2}"), k.Islem));
        Assert.Equal(9.16m, HasGram(kid));
    }

    [Theory]
    [InlineData("{\"miktar\":null}")]
    [InlineData("{\"miktar\":\"1,5\"}")]
    [InlineData("{\"miktar\":\"1.500\"}")]
    public async Task ZOBHGS03_BOS_ya_da_METIN_miktar_400_has_degismez(string govde)
    {
        var (s, fid, kid, k, db) = await Kur1Adet("ZOBHGS03" + govde.Length);
        using var _ = db;
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.TaslakKalemGuncelle(fid, kid, G(govde), k.Islem));
        Assert.Equal(9.16m, HasGram(kid));
    }

    [Fact]
    public void ZOBHGS04_KalemToplamHas_6_hane_AwayFromZero_yarim_sinir()
    {
        // 1 × 0,0000005 = 0,0000005 → 0,000001 ; 3 × 0,0000005 = 0,0000015 → 0,000002
        Assert.Equal(0.000001m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(1m, 0.0000005m));
        Assert.Equal(0.000002m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(3m, 0.0000005m));
    }

    [Fact]
    public void ZOBHGS05_KalemToplamHas_4_hane_0_0001_kaybolmaz_ve_sifir()
    {
        Assert.Equal(0.0001m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(1m, 0.0001m));
        Assert.Equal(0.0003m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(3m, 0.0001m));
        Assert.Equal(0m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(0m, 9.16m));
        Assert.Null(Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(2m, null));
    }

    [Fact]
    public void ZOBHGS06_KalemToplamHas_cok_buyuk_tasmaz()
    {
        // 999.999,999 × 999,999999 = 999.999.998,000000001 → 6 hane: 999.999.998,000000
        Assert.Equal(999_999_998.000000m, Kuyumcu.Api.Modules.Fatura.FaturaServisi.KalemToplamHas(999_999.999m, 999.999999m));
    }

    [Fact]
    public async Task ZOBHGS07_kesirli_miktar_1_5_has_orantili()
    {
        var (s, fid, kid, k, db) = await Kur1Adet("ZOBHGS07", 2m);
        using var _ = db;
        await s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":1.5}"), k.Islem);
        Assert.Equal(13.74m, HasGram(kid));        // 18,32 × 1,5 / 2
    }

    [Theory]
    // miktar 1 → N düzeltmesi: has bedeli = round2(N × 9,16 × 6.827,20) (kur kaydı DEĞİŞMEZ).
    [InlineData(3, 187_611.46)]      // 27,48 × 6.827,20 = 187.611,456
    [InlineData(100, 6_253_715.20)]  // 916 × 6.827,20
    public async Task ZOBHGS08_KURUS_miktar_duzeltmesi_has_bedelini_KUR_KAYDIYLA_hesaplar(int yeniMiktar, double beklenen)
    {
        var (s, fid, kid, k, db) = await Kur1Adet("ZOBHGS08" + yeniMiktar);
        using var _ = db;
        Assert.Equal(62_537.15m, HasDeger(kid));
        await s.TaslakKalemGuncelle(fid, kid, G($"{{\"miktar\":{yeniMiktar}}}"), k.Islem);
        Assert.Equal((decimal)beklenen, HasDeger(kid));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — ZO-BELGE tur: kalem.HasBirimFiyat (has
// bedeli DAYANAĞI = kur kaydı) sınırları. Beklenenler ELLE: kur 6.827,20 TL/has g;
// test ürünü 9,16 has g/adet. Yuvarlama MidpointRounding.AwayFromZero (OzelMatrahMotoru).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOBELGEHasDayanakSinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement G(string j) => System.Text.Json.JsonDocument.Parse(j).RootElement;

    private async Task<(Kuyumcu.Api.Modules.Fatura.FaturaServisi s, string fid, string kid, Kiraci k, Kuyumcu.Api.Data.KuyumcuDbContext db)>
        Kur(string ad, decimal miktar, decimal birimFiyat, string kdvTuru = "KUYUM", decimal? urunHas = null)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, ad);
        kurulum.SaveChanges();
        if (urunHas is { } uh)
        {
            kurulum.Urunler.Single(x => x.Id == k.UrunId).HasGrami = uh;
            kurulum.SaveChanges();
        }
        var db = vt.Olustur(k);
        var s = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await s.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = miktar * birimFiyat, KdvOran = 0.20m, KdvTuru = kdvTuru,
            Kalemler = [new Kuyumcu.Api.Modules.Fatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = miktar, BirimFiyat = birimFiyat }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        return (s, fid, kid, k, db);
    }

    private Kuyumcu.Api.Data.FaturaKalemi Oku(string kid) { using var c = vt.OlusturSistem(); return c.FaturaKalemleri.Single(x => x.Id == kid); }

    private void Yaz(string kid, decimal? hasGram, decimal? dayanak)
    {
        using var c = vt.OlusturSistem();
        var x = c.FaturaKalemleri.Single(y => y.Id == kid);
        x.HasGrami = hasGram; x.HasBirimFiyat = dayanak;
        c.SaveChanges();
    }

    [Theory]
    // 1,000000 g × 100,005 = 100,005 → AwayFromZero 100,01 (ToEven olsaydı 100,00)
    [InlineData(100.005, 100.01)]
    [InlineData(100.004999, 100.00)]
    public async Task ZOBHDS01_KURUS_0_005_dayanak_ile_AwayFromZero(double dayanak, double beklenen)
    {
        var (s, fid, kid, k, db) = await Kur("ZOBHDS01" + dayanak, 1m, 100_000m);
        using var _ = db;
        Yaz(kid, 1m, (decimal)dayanak);
        db.ChangeTracker.Clear();
        await s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":100000}"), k.Islem);
        Assert.Equal((decimal)beklenen, Oku(kid).HasDeger);
    }

    [Fact]
    public async Task ZOBHDS02_COK_BUYUK_tutar_999_999_999_99_STANDART_kalem_duzeltmesi()
    {
        var (s, fid, kid, k, db) = await Kur("ZOBHDS02", 1m, 1_000m, "STANDART");
        using var _ = db;
        await s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":999999999.99}"), k.Islem);
        var x = Oku(kid);
        Assert.Equal(999_999_999.99m, x.SatirToplam);
        Assert.Equal(833_333_333.33m, x.KdvMatrah);   // 999.999.999,99 / 1,2 = 833.333.333,325 → ,33
        Assert.Equal(166_666_666.66m, x.KdvTutar);    // 999.999.999,99 − 833.333.333,33
        Assert.Equal(0m, x.HasDeger);
        Assert.Null(x.HasBirimFiyat);                 // STANDART: dayanak yok
    }

    [Fact]
    public async Task ZOBHDS03_EKSI_birim_fiyat_400_dayanak_ve_has_degismez()
    {
        var (s, fid, kid, k, db) = await Kur("ZOBHDS03", 2m, 100_000m);
        using var _ = db;
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":-1}"), k.Islem));
        var x = Oku(kid);
        Assert.Equal(6_827.20m, x.HasBirimFiyat);
        Assert.Equal(18.32m, x.HasGrami);
        Assert.Equal(125_074.30m, x.HasDeger);        // 18,32 × 6.827,20 = 125.074,304
    }

    [Fact]
    public async Task ZOBHDS04_miktar_2_3_2_gidis_donus_has_ve_bedel_kaymaz()
    {
        var (s, fid, kid, k, db) = await Kur("ZOBHDS04", 2m, 100_000m);
        using var _ = db;
        await s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":3}"), k.Islem);
        Assert.Equal(187_611.46m, Oku(kid).HasDeger);  // 27,48 × 6.827,20 = 187.611,456
        await s.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);
        var x = Oku(kid);
        Assert.Equal(18.32m, x.HasGrami);
        Assert.Equal(125_074.30m, x.HasDeger);
        Assert.Equal(6_827.20m, x.HasBirimFiyat);
    }

    [Fact]
    public async Task ZOBHDS05_SIFIR_dayanak_fail_closed_KDV_tam()
    {
        var (s, fid, kid, k, db) = await Kur("ZOBHDS05", 1m, 100_000m);
        using var _ = db;
        Yaz(kid, 9.16m, 0m);
        db.ChangeTracker.Clear();
        await s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":100000}"), k.Islem);
        var x = Oku(kid);
        Assert.Equal(0m, x.HasDeger);
        Assert.Equal(16_666.67m, x.KdvTutar);          // 100.000 − 83.333,33
    }

    [Fact]
    public async Task ZOBHDS06_ESDEGER_duzeltme_olusturmadaki_has_bedelini_degistirmez_6_haneli_birim_has()
    {
        // Ürün birim has 0,916667; miktar 1,002 → ham 0,918500334 g (9 hane).
        // Oluşturma: round2(0,918500334 × 6.827,20); düzeltme: round2(round6(..) × 6.827,20).
        var (s, fid, kid, k, db) = await Kur("ZOBHDS06", 1.002m, 100_000m, "KUYUM", 0.916667m);
        using var _ = db;
        var once = Oku(kid).HasDeger;
        await s.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":100000}"), k.Islem);
        Assert.Equal(once, Oku(kid).HasDeger);
    }
}
