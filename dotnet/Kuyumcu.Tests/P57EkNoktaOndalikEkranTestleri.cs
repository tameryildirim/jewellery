using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.P57;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-57 EK — KABUL KOŞULU 3: /raporlar/satis-analizi ve /siparisler/{Id}
// sayfalarında ekrana basılan sayılarda '.' ondalık ayırıcı KALMAZ [bUnit]
//
// Doğrulayıcı bulgusu (KRİTİK): SatisAnalizi.razor:174 iade oranı
// "(o2*100).ToString("F1", Invariant) + "%"" → "12.5%"; SiparisDetay.razor:345-346
// kalem gram/adet "0.###" Invariant → "10.25" / "1.5". Artık Bicim.Yuzde /
// Bicim.Miktar (tr-TR) kullanılır.
// ⚠ Tarayıcı kültürü en-US taklit edilir: kültürsüz ToString de yakalanır.
// ⚠ Fikstür KESİRLİ sayı taşır — tam sayı nokta/virgül farkını göstermez.
// ═════════════════════════════════════════════════════════════════════════════

internal static class NoktaOndalikDenetimi
{
    // "12.5", "1.5" — tarih ("16.09.2026") ve tr-TR binlik ("25.000,50 ₺") eşleşmez.
    // T-K1: ekran TL biçimi sıfır kuruşu atar ("1.500 ₺") — sonunda NBSP+₺ olan sayı
    // Bicim.Tl çıktısıdır (binlik nokta), kültürsüz ondalık DEĞİLDİR; hariç tutulur.
    internal static readonly Regex Desen =
        new(@"(?<![\d.,])-?\d+\.\d+(?![\d.,]|\u00a0₺)", RegexOptions.CultureInvariant);

    internal static void Yok(string bolum, string metin)
    {
        var bulunan = Desen.Matches(metin).Select(m => m.Value).ToList();
        Assert.True(bulunan.Count == 0, $"{bolum} nokta ondalık içeriyor: {string.Join(" | ", bulunan)}");
    }

    internal static void EnUsIle(Action govde)
    {
        var onceki = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try { govde(); }
        finally { CultureInfo.CurrentCulture = onceki; }
    }
}

public sealed class SatisAnaliziEkranTestleri : BunitContext
{
    private const string Personel = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","olcut":"KAYDEDEN_KULLANICI","uyari":"SATAN_DEGIL_KAYDEDEN",
         "toplamSatir":1,"satirlar":[{"kullaniciId":"u1","kaydedenAd":"Test Kullanıcı","islemSayisi":8,
           "ciroTl":80000.00,"ortalamaSepetTl":10000.00,"iadeAdet":1,"iadeTl":10000.00,"iadeOrani":0.125}]}
        """;

    public SatisAnaliziEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/odeme-araci") =>
                    """{"baslangic":"2026-08-19","bitis":"2026-09-17","kaynak":"KASA_HAREKETLERI","araclar":[],"veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},"storno":{"adet":0,"tutarTl":0},"guvenilirlik":"YUKSEK"}""",
                var y when y.EndsWith("/raporlar/personel") => Personel,
                var y when y.EndsWith("/raporlar/sube-karsilastirma") =>
                    """{"baslangic":"2026-08-19","bitis":"2026-09-17","transferCiroyaKatilir":false,"toplamSatir":0,"toplamCiroTl":0,"satirlar":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void PersonelIadeOrani_TurkceYuzdeBasar_NoktaOndalikIcermez() => NoktaOndalikDenetimi.EnUsIle(() =>
    {
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-sekme-personel")));
        e.Find("#sa-sekme-personel").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-personel-tablo")));

        var hucreler = e.FindAll("#sa-personel-tablo tbody td").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("%12,5", hucreler);
        Assert.DoesNotContain(hucreler, h => h.EndsWith('%'));
        NoktaOndalikDenetimi.Yok("#sa-personel-tablo", e.Find("#sa-personel-tablo").TextContent);
    });
}

public sealed class SiparisDetayEkranTestleri : BunitContext
{
    private const string Detay = """
        {"id":"s3","no":"S-2026-000003","tur":"MUSTERI_SIPARISI","durum":"TESLIM_EDILDI","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":null,
         "tahminiTutarTl":null,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":false,"terminal":true,
         "izinliGecisler":[],"gerekceIsteyenGecisler":[],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":null,"urunAd":null,"tanim":"künye","ayarId":"K22",
                      "gramAgirlik":10.25,"adet":1.5,"sira":0},
                     {"id":"k2","urunId":null,"urunAd":null,"tanim":"yüzük","ayarId":null,
                      "gramAgirlik":null,"adet":2.0,"sira":1}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    public SiparisDetayEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("/siparisler/") => Detay,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => "[]",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void KalemGramVeAdet_TurkceVirgulBasar_NoktaOndalikIcermez() => NoktaOndalikDenetimi.EnUsIle(() =>
    {
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s3"));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sipd-kalem-k1")));

        var k1 = e.FindAll("#sipd-kalem-k1 td").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal("10,25", k1[3]);   // gram
        Assert.Equal("1,5", k1[4]);     // adet
        var k2 = e.FindAll("#sipd-kalem-k2 td").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal("—", k2[3]);       // gram yok → uydurulmaz
        Assert.Equal("2", k2[4]);
        NoktaOndalikDenetimi.Yok("#sipd-kalemler", e.Find("#sipd-kalemler").TextContent);
    });
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-57 EK 2 — /vezne fiş detayı kalem ADEDİ (doğrulayıcı KRİTİK, tur 2):
// Vezne.razor kalem adedi "0.###" InvariantCulture basıyordu → kesirli adet
// 1.5 ekranda "1.5". Artık Bicim.Miktar(k.Adet, null) → "1,5".
// Fikstür KESİRLİ adet (1.5) taşır; kültür en-US taklit edilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class VezneDetayEkranTestleri : BunitContext
{
    private const string Kuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":1,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[{"durum":"VEZNEDE","adet":1}],
         "fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":7500.00,"damgaliToplamTl":7500.00,
            "toplamTahmini":false,"cariId":"c1","cariAd":"Ayşe Yılmaz",
            "aciklama":"yarım çift küpe","hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Tezgâhtar",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"}]}
        """;

    private const string Detay = """
        {"id":"v1","durum":"VEZNEDE","cariId":"c1","cariAd":"Ayşe Yılmaz","aciklama":"yarım çift küpe",
         "kurSnapshotId":"kur-1","toplamTl":7500.00,"damgaliToplamTl":7500.00,"hareketFisId":null,
         "hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Tezgâhtar","tahsilEdenKullaniciId":null,
         "tahsilEdenAd":null,"iptalGerekce":null,"vezneZorunlu":false,"gerekceAsgari":10,
         "duzenlenebilir":false,"veznelenebilir":false,"tahsilEdilebilir":true,
         "iptalEdilebilir":true,"terminal":false,"mutabakatGerekli":false,
         "olusturuldu":"2026-09-17T08:00:00Z","guncellendi":"2026-09-17T08:05:00Z",
         "veznelendi":"2026-09-17T08:05:00Z","tahsilEdildi":null,"iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Küpe","adet":1.5,
                      "birimFiyatTl":4900.00,"iscilikTl":100.00,"sira":0,"tutarTl":7500.00}]}
        """;

    public VezneDetayEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("/vezne/fisler/") => Detay,
                var y when y.EndsWith("/vezne/kuyruk") => Kuyruk,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"k-1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true,"subeId":"sb1"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Küpe"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void KalemAdedi_TurkceVirgulBasar_NoktaOndalikIcermez() => NoktaOndalikDenetimi.EnUsIle(() =>
    {
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-sec-v1").Click();

        var satir = e.FindAll("#vz-detay-kalemler tbody tr").Single();
        var hucre = satir.QuerySelectorAll("td").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal("Küpe", hucre[0]);
        Assert.Equal("1,5", hucre[1]);   // kesirli adet tr-TR virgül
        NoktaOndalikDenetimi.Yok("#vz-detay-kalemler", e.Find("#vz-detay-kalemler").TextContent);
    });
}
