using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Pano;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-KUCUK — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). Paket iki para yoluna dokunur:
//  (1) /vitrin/ekran — müşteriye dönük fiyat GÖSTERİMİ (Bicim.Tl, AwayFromZero 2 hane),
//  (2) satış sepeti — müşteri değişince yeni liste fiyatı ÖNERİSİ (B2-12).
// ════════════════════════════════════════════════════════════════════════

/// <summary>(1) Dükkân ekranı fiyat gösterimi: sıfır, kuruş, çok büyük, null.</summary>
public sealed class EKSIKKUCUKVitrinSinirTestleri : BunitContext
{
    private string satirlarJson = "[]";
    private string ibare = "\"DAHIL\"";

    public EKSIKKUCUKVitrinSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(() => satirlarJson, () => ibare)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Func<string> satirlar, Func<string> ibare) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = $$"""
            {"kaynak":"ALTINKAYNAK","zaman":"2026-10-10T08:00:00Z","satirlar":{{satirlar()}},
             "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false,
             "iscilikIbaresi":{{ibare()}}}
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static string Metin(IRenderedComponent<Kuyumcu.Web.Pages.VitrinEkrani> e, string id) =>
        e.Find(id).TextContent.Replace(' ', ' ').Replace(' ', ' ').Trim();

    [Fact]
    public void DG_V01_sifir_kurus_buyuk_ve_null_fiyat_tr_TR_ve_AwayFromZero()
    {
        satirlarJson = """
        [{"kod":"A","etiket":"Sıfır","alis":0,"satis":0.005,"birim":"GRAM"},
         {"kod":"B","etiket":"Büyük","alis":999999999.99,"satis":999999999.994,"birim":"ADET"},
         {"kod":"C","etiket":"Yok","alis":null,"satis":1500.5,"birim":"ADET"}]
        """;
        var e = Render<Kuyumcu.Web.Pages.VitrinEkrani>();
        e.WaitForElement("#vtam-tablo");

        var a = e.FindAll("#vtam-satir-A span").Select(s => s.TextContent.Replace(' ', ' ').Trim()).ToList();
        Assert.Equal("0 ₺", a[1]);           // sıfır: "0 ₺", eksi işareti/boş değil
        Assert.Equal("0,01 ₺", a[2]);        // 0,005 → AwayFromZero → 0,01

        var b = e.FindAll("#vtam-satir-B span").Select(s => s.TextContent.Replace(' ', ' ').Trim()).ToList();
        Assert.Equal("999.999.999,99 ₺", b[1]);
        Assert.Equal("999.999.999,99 ₺", b[2]);   // ,994 → ,99

        var c = e.FindAll("#vtam-satir-C span").Select(s => s.TextContent.Replace(' ', ' ').Trim()).ToList();
        Assert.Equal("—", c[1]);             // null fiyat "0" diye BASILMAZ
        Assert.Equal("1.500,50 ₺", c[2]);
        Assert.DoesNotContain("TRY", e.Markup, StringComparison.Ordinal);
        Assert.Equal("Satış fiyatlarına işçilik bedeli dahildir.", Metin(e, "#vtam-ibare"));
    }

    [Theory]
    [InlineData("null")]
    public void DG_V02_gecersiz_ya_da_bos_ibarede_fiyat_GOSTERILMEZ_fail_closed(string ham)
    {
        satirlarJson = """[{"kod":"A","etiket":"Gram","alis":6800,"satis":6900,"birim":"GRAM"}]""";
        ibare = ham;
        var e = Render<Kuyumcu.Web.Pages.VitrinEkrani>();
        // Geçersiz değer tablo + ibaresiz fiyat göstermemeli: ya kilit bandı ya da hiç fiyat.
        e.WaitForAssertion(() =>
        {
            var tablo = e.FindAll("#vtam-tablo").Count;
            var ibareVar = e.FindAll("#vtam-ibare").Count;
            Assert.False(tablo > 0 && ibareVar == 0, "ibaresiz fiyat tablosu basıldı");
            Assert.DoesNotContain("6.900", e.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void DG_V03_ibare_kod_dogrulayici_sabit_kume()
    {
        Assert.True(IscilikIbaresiDegerleri.Gecerli("DAHIL"));
        Assert.True(IscilikIbaresiDegerleri.Gecerli("HARIC"));
        Assert.False(IscilikIbaresiDegerleri.Gecerli(null));
        Assert.False(IscilikIbaresiDegerleri.Gecerli(""));
        Assert.False(IscilikIbaresiDegerleri.Gecerli("dahil"));
        Assert.False(IscilikIbaresiDegerleri.Gecerli("DAHİL"));   // Türkçe İ ile yazılmış kod geçersiz
    }

    [Fact]
    public void DG_V04_Turkce_sayi_girisi_SayiCoz()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);   // binlik mi ondalık mı belirsiz → RED
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("  ").Sonuc);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);   // has/gram 4 hane
    }
}

/// <summary>(2) Müşteri değişince liste fiyatı önerisi: sıfır, eksi, çok büyük, kuruş-altı.</summary>
public sealed class EKSIKKUCUKSatisSinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string c1Fiyat = "11000";

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public EKSIKKUCUKSatisSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => c1Fiyat))
        { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static string Teklif(string fiyat) =>
        $$"""{"teklifVar":true,"listeFiyati":{{fiyat}},"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":{{fiyat}},"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""";

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> c1) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") || v.Contains("/cari?") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":300000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":300000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") && v.Contains("cariId=c1") => Teklif(c1()),
                var v when v.Contains("/fiyat/teklif") => Teklif("10000"),
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> SepetliPos()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif")));
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı — müşteri "Değiştir" katmanından seçilir (aynı cariId → MusteriDegisti →
    // aynı teklif isteği), Kaydet #yildiz-tezgah-kaydet. ⚠ ÜRÜN BULGUSU: fiyat listesi sorusu (FisListeDegisti —
    // #islem-liste-degisti/-guncelle/-eski/-hata) yalnız özgün kalem tablosunun parçasında (Islemler.razor KalemParcasi)
    // çizilir; Yıldız tezgâhı o parçayı ÇİZMEZ. "Sorar" testleri bandı bekleyip KIRMIZI kalır; "sormaz" testleri gövdeyi
    // sınadıktan sonra bandın çizilebileceği yerin varlığını denetler (boş DOM'da yanlış yeşil yok).
    private static void ListeBandiYeriVar(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        Assert.True(e.FindAll("#islem-kalem-tablo, #islem-kalem-liste").Count > 0,
            "ÜRÜN BULGUSU (tema temizliği 17.10): fiyat listesi sorusu (FisListeDegisti) Yıldız tezgâhında çizilmiyor — 'sormaz' denetimi anlamsız kalır");


    private decimal GidenFiyat(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        TemaTemizligiGDYardimcisi.KaydetAcikBekle(e);
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde).RootElement;
        return (g.TryGetProperty("kalemler", out var k) ? k[0] : g).GetProperty("birimFiyatTl").GetDecimal();
    }

    private void CariyeGecVeTeklifiBekle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif") && i.Yol.Contains("cariId=c1")));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("-0.01")]
    public void DG_S01_sifir_ya_da_eksi_teklif_ONERILMEZ_eski_fiyat_gider(string fiyat)
    {
        c1Fiyat = fiyat;
        var e = SepetliPos();
        CariyeGecVeTeklifiBekle(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/tevsik")));
        Assert.Empty(e.FindAll("#islem-liste-degisti"));
        Assert.Equal(10000m, GidenFiyat(e));
        ListeBandiYeriVar(e);
    }

    [Fact]
    public void DG_S02_cok_buyuk_tutar_onerilir_guncellenince_govde_birebir()
    {
        c1Fiyat = "999999999.99";
        var e = SepetliPos();
        CariyeGecVeTeklifiBekle(e);
        e.WaitForElement("#islem-liste-guncelle").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-liste-degisti")));
        Assert.Contains("999.999.999,99", e.Markup.Replace(' ', ' '), StringComparison.Ordinal);
        Assert.Equal(999999999.99m, GidenFiyat(e));
    }

    [Fact]
    public void DG_S03_kurus_farki_0_01_bile_SORULUR_ve_kendiliginden_degismez()
    {
        c1Fiyat = "10000.01";
        var e = SepetliPos();
        CariyeGecVeTeklifiBekle(e);
        e.WaitForElement("#islem-liste-degisti");
        Assert.Equal(10000m, GidenFiyat(e));   // basılmadı → eski fiyat
    }

    [Fact]
    public void DG_S04_ayni_tutar_farkli_yazim_10000_00_SORULMAZ()
    {
        c1Fiyat = "10000.00";
        var e = SepetliPos();
        CariyeGecVeTeklifiBekle(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/tevsik")));
        Assert.Empty(e.FindAll("#islem-liste-degisti"));
        Assert.Equal(10000m, GidenFiyat(e));
        ListeBandiYeriVar(e);
    }
}

/// <summary>
/// (3) Sunucu sınırı: DB'deki bozuk/küçük harf/boş ibare değeri panoya ASLA geçmez → null
/// (dükkân ekranı kilitli kalır). İstemci yalnız null'u kilit sayar; bu yüzden sunucu
/// normalizasyonu fail-closed zincirinin tek halkasıdır ve burada sabitlenir.
/// </summary>
[Collection("http")]
public sealed class EKSIKKUCUKSunucuSinirTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData("dahil")]
    [InlineData("")]
    [InlineData("BELKI")]
    [InlineData("DAHİL")]
    [InlineData(" DAHIL")]
    public async Task DG_H01_DBdeki_gecersiz_ibare_panoda_NULL_doner(string ham)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PAKETEKSIK-KUCUK-TEST dg bozuk ibare");
            db.KiraciAyarlari.Add(new Kuyumcu.Api.Data.KiraciAyari
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId,
                Anahtar = "pano.iscilik_ibaresi", Deger = ham, Guncellendi = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
        var r = await f.Ortam.Istemci(k).GetAsync("/api/v1/pano/guncel");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(JsonValueKind.Null, j.GetProperty("iscilikIbaresi").ValueKind);
    }

    [Theory]
    [InlineData("{\"iscilikIbaresi\":\"\"}", 400)]
    [InlineData("{\"iscilikIbaresi\":\"BELKI\"}", 400)]
    [InlineData("{\"iscilikIbaresi\":123}", 400)]
    [InlineData("{}", 400)]
    public async Task DG_H02_PATCH_gecersiz_govde_400_kayit_DOGMAZ(string govde, int beklenen)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETEKSIK-KUCUK-TEST dg patch");
        var r = await f.Ortam.Istemci(k).PatchAsync("/api/v1/pano/iscilik-ibaresi",
            new StringContent(govde, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(beklenen, (int)r.StatusCode);
        var p = await (await f.Ortam.Istemci(k).GetAsync("/api/v1/pano/guncel")).Json();
        Assert.Equal(JsonValueKind.Null, p.GetProperty("iscilikIbaresi").ValueKind);
    }
}
