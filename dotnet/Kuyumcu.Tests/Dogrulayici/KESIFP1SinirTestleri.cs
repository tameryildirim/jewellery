using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET KESIF-P1 FİNANSAL SINIR TESTLERİ.
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Kapsam: POS komisyon yüzde→kesir, valör/taksit tam sayı,
// fiyat listesi paneli para/has gösterimi (tr-TR, ₺, AwayFromZero).
// ═════════════════════════════════════════════════════════════════════════════

internal static class P1SinirOrtak
{
    public static HttpResponseMessage Json(string g, HttpStatusCode kod = HttpStatusCode.OK) => new(kod)
    {
        Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
    };
}

public sealed class KESIFP1PosSinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public KESIFP1PosSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<PosServisi>();
    }

    private const string Cihaz =
        """{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}""";

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            var detay = $$"""{"cihaz":{{Cihaz}},"taksitOranlari":[{"id":"o1","posCihazId":"p1","taksitAdedi":3,"komisyonOran":0.0275,"valorGun":null,"gecerliBaslangic":"2026-09-01T00:00:00Z","aktif":true}]}""";
            return yol switch
            {
                var y when y.EndsWith("/pos/cihazlar") && get => P1SinirOrtak.Json($"[{Cihaz}]"),
                var y when y.EndsWith("/pos/cihazlar/p1") && get => P1SinirOrtak.Json(detay),
                var y when y.EndsWith("/taksit-oranlari") || y.Contains("/taksit-oranlari/") => P1SinirOrtak.Json(detay),
                var y when y.EndsWith("/pos/cihazlar") => P1SinirOrtak.Json(detay),
                var y when y.EndsWith("/kasalar") => P1SinirOrtak.Json("""[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]"""),
                _ => P1SinirOrtak.Json("{}"),
            };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Yazmalar(string parca)
    { lock (istekler) return [.. istekler.Where(i => i.Metot != "GET" && i.Yol.Contains(parca))]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Pos> YeniCihaz(string oran, string valor)
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-yeni-ac").Click();
        e.WaitForElement("#pos-cihaz-ad").Input("Sınır POS");
        e.Find("#pos-cihaz-banka").Input("Sınır Bankası");
        e.Find("#pos-cihaz-oran").Input(oran);
        e.Find("#pos-cihaz-valor").Input(valor);
        return e;
    }

    private bool KaydetPasif(IRenderedComponent<Kuyumcu.Web.Pages.Pos> e) =>
        e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled");

    /// <summary>Kabul edilen girdi: gövdedeki kesir SABİT beklenenle birebir.</summary>
    [Theory]
    [InlineData("0", "0", "0", 0)]              // sıfır komisyon, sıfır valör
    [InlineData("1,85", "1", "0.0185", 1)]      // kabul örneği
    [InlineData("1,5", "2", "0.015", 2)]        // Türkçe virgül
    [InlineData("100", "90", "1", 90)]          // üst sınırlar dahil
    [InlineData("0,01", "0", "0.0001", 0)]      // kesir 4 hane (decimal(5,4)) sınırı
    public void Sinir_kabul_edilen_oran_govdeye_birebir_kesir(string oran, string valor, string beklenenKesir, int beklenenValor)
    {
        var e = YeniCihaz(oran, valor);
        e.WaitForAssertion(() => Assert.False(KaydetPasif(e)));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/pos/cihazlar")));
        var g = JsonDocument.Parse(Yazmalar("/pos/cihazlar")[0].Govde).RootElement;
        Assert.Equal(decimal.Parse(beklenenKesir, CultureInfo.InvariantCulture), g.GetProperty("pesinKomisyonOran").GetDecimal());
        Assert.Equal(beklenenValor, g.GetProperty("valorGun").GetInt32());
    }

    /// <summary>Reddedilen girdi: Kaydet pasif, tıklansa da istek 0 (sessiz 0/yuvarlama yok).</summary>
    [Theory]
    [InlineData("-1", "1")]                     // eksi komisyon
    [InlineData("100,01", "1")]                 // %100 üstü
    [InlineData("999.999.999,99", "1")]         // çok büyük
    [InlineData("1.500", "1")]                  // belirsiz binlik
    [InlineData("45.000", "1")]                 // Türkçe binlik → 45000 > 100
    [InlineData("", "1")]                       // boş
    [InlineData("1,85", "-1")]                  // eksi valör
    [InlineData("1,85", "91")]                  // valör üstü
    [InlineData("1,85", "1,5")]                 // kesirli valör
    [InlineData("1,85", "")]                    // boş valör
    public void Sinir_reddedilen_girdi_istek_atmaz(string oran, string valor)
    {
        var e = YeniCihaz(oran, valor);
        e.WaitForAssertion(() => Assert.True(KaydetPasif(e)));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        Assert.Empty(Yazmalar("/pos/cihazlar"));
    }

    /// <summary>
    /// Kuruş/hassasiyet sınırı: kesir sütunu decimal(5,4). '1,855' (%) = 0,01855 kesir 4 haneye SIĞMAZ;
    /// ya reddedilmeli ya da gövde 4 haneyi aşmamalı (aksi hâlde veritabanı sessizce 0,0186 yazar:
    /// görünen ≠ giden).
    /// </summary>
    [Fact]
    public void Sinir_yuzde_uc_hane_sessiz_yuvarlanmaz()
    {
        var e = YeniCihaz("1,855", "1");
        e.Render();
        if (KaydetPasif(e)) { Assert.Empty(Yazmalar("/pos/cihazlar")); return; }
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/pos/cihazlar")));
        var k = JsonDocument.Parse(Yazmalar("/pos/cihazlar")[0].Govde).RootElement.GetProperty("pesinKomisyonOran").GetDecimal();
        Assert.Equal(k, decimal.Round(k, 4));
    }

    /// <summary>Taksit adedi 2..12 tam sayı: 1 ve 13 reddedilir, 12 kabul.</summary>
    [Theory]
    [InlineData("1", false)]
    [InlineData("13", false)]
    [InlineData("12", true)]
    [InlineData("2", true)]
    public void Sinir_taksit_adedi(string adet, bool kabul)
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#pos-taksit-adet").Input(adet);
        e.Find("#pos-taksit-oran").Input("0");
        e.WaitForAssertion(() => Assert.Equal(!kabul, e.Find("#pos-taksit-ekle").HasAttribute("disabled")));
        e.Find("#pos-taksit-ekle").Click();
        if (!kabul) { Assert.Empty(Yazmalar("/taksit-oranlari")); return; }
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/taksit-oranlari")));
        var g = JsonDocument.Parse(Yazmalar("/taksit-oranlari")[0].Govde).RootElement;
        Assert.Equal(int.Parse(adet, CultureInfo.InvariantCulture), g.GetProperty("taksitAdedi").GetInt32());
        Assert.Equal(0m, g.GetProperty("komisyonOran").GetDecimal());
    }
}

public sealed class KESIFP1FiyatPaneliSinirTestleri : BunitContext
{
    public KESIFP1FiyatPaneliSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (yol.Contains("/onizleme"))
                return Task.FromResult(P1SinirOrtak.Json("""
                    {"toplam":2,"limit":50,"offset":0,"satirlar":[
                     {"urunId":"s1","kod":"SNR-1","ad":"Sınır 1","ayarId":"K24","gram":1,"hasGrami":13.925,"iscilikTl":null,
                      "teklifVar":true,"sebep":null,"hasDegeri":999999999.99,"marjTl":-1234.5,"iscilikTlUygulanan":0.005,"listeFiyati":0,
                      "indirimTutari":0.004,"satisFiyati":45000,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null},
                     {"urunId":"s2","kod":"SNR-2","ad":"Sınır 2","ayarId":"K14","gram":1,"hasGrami":0.0001,"iscilikTl":null,
                      "teklifVar":true,"sebep":null,"hasDegeri":-0.004,"marjTl":0,"iscilikTlUygulanan":0,"listeFiyati":1500.75,
                      "indirimTutari":0,"satisFiyati":1500.75,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null}]}
                    """));
            return Task.FromResult(P1SinirOrtak.Json("{}"));
        }
    }

    private IRenderedComponent<FiyatListesiUrunPaneli> Ciz()
    {
        var e = Render<FiyatListesiUrunPaneli>(p => p
            .Add(x => x.ListeId, "fl1").Add(x => x.ListeAdi, "Perakende").Add(x => x.KurId, "kur1"));
        e.WaitForElement("#flp-satir-s1");
        return e;
    }

    private static List<string> Hucreler(IRenderedComponent<FiyatListesiUrunPaneli> e, string id) =>
        [.. e.FindAll($"#{id} td").Select(t => t.TextContent.Trim())];

    [Fact]
    public void Sinir_cok_buyuk_eksi_sifir_ve_kurus_yuvarlamasi_tr_TR()
    {
        var (k, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var e = Ciz();
            var s1 = e.Find("#flp-satir-s1").TextContent;
            Assert.Contains("999.999.999,99 ₺", s1);          // çok büyük tutar
            Assert.Contains("−1.234,50 ₺", s1);          // eksi: işaret doğru, kuruş iki hane
            Assert.Contains("0,01 ₺", s1);                    // 0,005 → 0,01 (AwayFromZero)
            Assert.Contains("45.000 ₺", s1);                  // tam lira
            Assert.Contains("24 Ayar", s1);                        // ayar adı
            Assert.DoesNotContain("K24", s1.Replace("SNR", ""), StringComparison.Ordinal);
            Assert.Contains("13,93", s1);                           // has 13,925 → 13,93 (AwayFromZero, 2 hane — spec)
            var s2 = e.Find("#flp-satir-s2").TextContent;
            Assert.Contains("1.500,75 ₺", s2);                // "1,500.75" değil
            Assert.DoesNotContain("−0", s2, StringComparison.Ordinal);   // −0,004 → "0 ₺" işaretsiz
            Assert.DoesNotContain("1,500.75", e.Markup, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = k;
            CultureInfo.CurrentUICulture = ui;
        }
    }

    /// <summary>Has 4 hane sınırı: 0,0001 g has ekranda SIFIR gibi görünmemeli.</summary>
    [Fact]
    public void Sinir_has_dort_hane_sifir_gorunmez()
    {
        var e = Ciz();
        var hucreler = Hucreler(e, "flp-satir-s2");
        Assert.DoesNotContain("0", hucreler);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (2026-09-30) — önceki GEÇERLİ değerin üstüne
// geçersiz yazma, düzenleme gidiş-dönüşü, satır içi oran sınırı, SayiCoz.
// Beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KESIFP1Tur2SinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public KESIFP1Tur2SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<PosServisi>();
    }

    private const string Cihaz =
        """{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}""";

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            var detay = $$"""{"cihaz":{{Cihaz}},"taksitOranlari":[{"id":"o1","posCihazId":"p1","taksitAdedi":3,"komisyonOran":0.0275,"valorGun":null,"gecerliBaslangic":"2026-09-01T00:00:00Z","aktif":true}]}""";
            return yol switch
            {
                var y when y.EndsWith("/pos/cihazlar") && get => P1SinirOrtak.Json($"[{Cihaz}]"),
                var y when y.EndsWith("/pos/cihazlar/p1") => P1SinirOrtak.Json(get ? detay : Cihaz),
                var y when y.EndsWith("/taksit-oranlari") || y.Contains("/taksit-oranlari/") => P1SinirOrtak.Json(detay),
                var y when y.EndsWith("/pos/cihazlar") => P1SinirOrtak.Json(detay),
                var y when y.EndsWith("/kasalar") => P1SinirOrtak.Json("""[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]"""),
                _ => P1SinirOrtak.Json("{}"),
            };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Yazmalar(string parca)
    { lock (istekler) return [.. istekler.Where(i => i.Metot != "GET" && i.Yol.Contains(parca))]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Pos> YeniCihaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-yeni-ac").Click();
        e.WaitForElement("#pos-cihaz-ad").Input("Sınır POS");
        e.Find("#pos-cihaz-banka").Input("Sınır Bankası");
        return e;
    }

    /// <summary>Önce geçerli valör '15' (Kaydet açık), sonra '1,5': eski 15 GİTMEMELİ — Kaydet pasif, istek 0.</summary>
    [Fact]
    public void T2_gecerli_valor_ustune_kesirli_yazinca_eski_deger_gitmez()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-oran").Input("1,85");
        e.Find("#pos-cihaz-valor").Input("15");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#pos-cihaz-valor").Input("1,5");
        e.WaitForAssertion(() => Assert.True(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        Assert.Empty(Yazmalar("/pos/cihazlar"));
    }

    /// <summary>Önce geçerli oran '1,85', sonra '1,855': eski 0,0185 GİTMEMELİ — Kaydet pasif, istek 0, alan altında "2 hane".</summary>
    [Fact]
    public void T2_gecerli_oran_ustune_uc_ondalik_yazinca_eski_deger_gitmez()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-valor").Input("1");
        e.Find("#pos-cihaz-oran").Input("1,85");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#pos-cihaz-oran").Input("1,855");
        e.WaitForAssertion(() => Assert.True(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        Assert.Contains("Virgülden sonra en çok 2 hane", e.Find("#cekmece-pos-cihaz").TextContent);
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        Assert.Empty(Yazmalar("/pos/cihazlar"));
    }

    /// <summary>Düzenleme gidiş-dönüşü: kayıtlı 0,0185 → kutuda '1,85' → dokunmadan kaydet → gövdede 0,0185 BİREBİR (kayma yok).</summary>
    [Fact]
    public void T2_duzenleme_gidis_donus_oran_kaymaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForAssertion(() => Assert.Equal("1,85", e.Find("#pos-cihaz-oran").GetAttribute("value")));
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/pos/cihazlar/p1")));
        var g = JsonDocument.Parse(Yazmalar("/pos/cihazlar/p1")[0].Govde).RootElement;
        Assert.Equal(0.0185m, g.GetProperty("pesinKomisyonOran").GetDecimal());
        Assert.Equal(1, g.GetProperty("valorGun").GetInt32());
    }

    /// <summary>Satır içi taksit oranı: '0,005' (%) → 3 hane: istek 0; '2,5' → gövdede 0,025 BİREBİR.</summary>
    [Theory]
    [InlineData("0,005", null)]
    [InlineData("-0,5", null)]
    [InlineData("2,5", "0.025")]
    [InlineData("0", "0")]
    public void T2_satir_ici_oran_sinirlari(string girdi, string? beklenenKesir)
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#si-pos-taksit-oran-o1-ac").Click();
        e.WaitForElement("#si-pos-taksit-oran-o1").Input(girdi);
        e.Find("#si-pos-taksit-oran-o1-kaydet").Click();
        if (beklenenKesir is null)
        {
            e.Render();
            Assert.Empty(Yazmalar("/taksit-oranlari/o1"));
            return;
        }
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/taksit-oranlari/o1")));
        var g = JsonDocument.Parse(Yazmalar("/taksit-oranlari/o1")[0].Govde).RootElement;
        Assert.Equal(decimal.Parse(beklenenKesir, CultureInfo.InvariantCulture), g.GetProperty("komisyonOran").GetDecimal());
    }

    /// <summary>Tabloda kesir → tr-TR yüzde: 0,0185 → "%1,85", 0,0275 → "%2,75"; en-US kalıbı yok.</summary>
    [Fact]
    public void T2_tablo_yuzdesi_en_US_kulturunde_de_tr_TR()
    {
        var eski = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
            e.WaitForAssertion(() => Assert.Contains("%1,85", e.Markup));
            Assert.DoesNotContain("1.85", e.Markup);
        }
        finally { CultureInfo.CurrentCulture = eski; CultureInfo.CurrentUICulture = eski; }
    }
}

/// <summary>Türkçe sayı girişi tek merkez (FormAlani.SayiCoz): sabit beklenenler.</summary>
public sealed class KESIFP1Tur2SayiCozTestleri
{
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0", "0")]
    public void T2_SayiCoz_kabul(string girdi, string beklenen) =>
        Assert.Equal(decimal.Parse(beklenen, CultureInfo.InvariantCulture),
            Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(girdi).Deger);

    [Theory]
    [InlineData("1.500")]
    [InlineData("")]
    [InlineData("abc")]
    public void T2_SayiCoz_red(string girdi) =>
        Assert.Null(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(girdi).Deger);
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 3 — HASIM DOĞRULAYICI (Aşkın + Mehmet, 30.09 akşam). Fiyat paneli ve POS
// tablosunun TEK biçimcileri (Bicim.Tl · Bicim.KarHasGram · Bicim.Yuzde) kültür
// en-US'e zorlanmışken sınır değerlerde. Beklenenler SABİT metin.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KESIFP1Tur3BicimSinirTestleri
{
    private static T EnUs<T>(Func<T> f)
    {
        var (k, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            return f();
        }
        finally { CultureInfo.CurrentCulture = k; CultureInfo.CurrentUICulture = ui; }
    }

    [Theory]
    [InlineData("0", "0 ₺")]                              // sıfır
    [InlineData("-1234.5", "−1.234,50 ₺")]           // eksi: işaret korunur, tr-TR
    [InlineData("0.005", "0,01 ₺")]                       // kuruş sınırı AwayFromZero
    [InlineData("0.0049", "0 ₺")]                         // sınırın altı
    [InlineData("-0.005", "−0,01 ₺")]                // eksi kuruş sınırı
    [InlineData("999999999.99", "999.999.999,99 ₺")]      // çok büyük tutar
    [InlineData("92206.95", "92.206,95 ₺")]               // kabul örneği
    public void T3_Tl_en_US_kulturunde_tr_TR(string girdi, string beklenen) =>
        Assert.Equal(beklenen, EnUs(() => Bicim.Tl(decimal.Parse(girdi, CultureInfo.InvariantCulture))));

    [Theory]
    [InlineData("0.0001", "0,0001")]      // has 4 hane: sıfıra düşmez
    [InlineData("0.00005", "0,0001")]     // 4. hane sınırı AwayFromZero
    [InlineData("0.00004", "0,00")]       // 4 haneden küçük → sıfır
    [InlineData("13.9249", "13,92")]      // olağan 2 hane
    [InlineData("13.925", "13,93")]       // 2 hane sınırı AwayFromZero
    public void T3_KarHasGram_sade_sinirlar(string girdi, string beklenen) =>
        Assert.Equal(beklenen, EnUs(() => Bicim.KarHasGram(decimal.Parse(girdi, CultureInfo.InvariantCulture), sade: true)));

    [Theory]
    [InlineData("0.0185", "%1,85")]       // kabul örneği (kesir → yüzde)
    [InlineData("0", "%0")]               // sıfır
    [InlineData("1", "%100")]             // üst sınır
    [InlineData("0.0001", "%0,01")]       // decimal(5,4) en küçük adım
    public void T3_Pos_tablo_yuzdesi_kesirden(string kesir, string beklenen) =>
        Assert.Equal(beklenen, EnUs(() => Bicim.Yuzde(decimal.Parse(kesir, CultureInfo.InvariantCulture) * 100m)));
}
