using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET G-5 (B-4 devri) — RAPORLAR (profesyonel yüz) "SATIŞLAR NEYLE ÖDENDİ"
// KARTI [bUnit]
//
//   • ALTIN satırı sözlükten "Eski altın (mahsup)" adıyla çizilir; ham "ALTIN"
//     kodu kartta YOKTUR (UX-S31).
//   • Toplamlar SUNUCUDAN gelir: kart satırları toplamaz, sunucunun
//     kasayaGirenTl / kirilimToplamTl değerini basar (UX-S21).
//   • Altınsız dönemde eski altın satırı ve kartı ÇİZİLMEZ.
//   • Hata "satış yok"a çevrilmez (UX-S28).
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: sayfanın açılışta çağırdığı uçlara
//   gerçekçi yanıt verilir (RaporlarEkranTestleri deseni).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class G5SatisKirilimiEkranTestleri : BunitContext
{
    private readonly List<(string Yol, string Sorgu)> istekler = [];
    private string odemeAraci = AltinliYanit;
    private bool odemeAraciHata;

    // Sunucunun B-4 canlı adım 18 sonrası döndürdüğü şekil (satisKirilimi birebir alanlar).
    // Kartlar bu gövdedeki kasayaGirenTl / mahsupAltinTl / kirilimToplamTl değerlerini
    // basar; ekran satırları kendisi toplamaz (G5_Ekran_kasaya_giren_karti_eski_altini_katmaz).
    private const string AltinliYanit = """
        {"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI",
         "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":10840.00,"cikisTutar":0,"adet":1}],"girisTlToplam":10840.00,"adet":1}],
         "veresiye":{"doganTl":0,"kapananTl":0},
         "bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":2},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"TAM",
         "satisKirilimi":{"kaynak":"ODEME_KALEMLERI",
           "satirlar":[{"arac":"NAKIT","tutarTl":10840.00,"fisAdet":1,"kasayaGirer":true},
                       {"arac":"ALTIN","tutarTl":9160.00,"fisAdet":1,"kasayaGirer":false}],
           "brutSatisTl":20000.00,"kirilimToplamTl":20000.00,"farkTl":0.00,
           "kasayaGirenTl":10840.00,"mahsupAltinTl":9160.00,"veresiyeTl":0.00,
           "fisAdet":1,"kalemsizFisAdet":0,"kalemsizTl":0.00}}
        """;

    private const string AltinsizYanit = """
        {"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI",
         "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":20000.00,"cikisTutar":0,"adet":1}],"girisTlToplam":20000.00,"adet":1}],
         "veresiye":{"doganTl":0,"kapananTl":0},
         "bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":1},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"TAM",
         "satisKirilimi":{"kaynak":"ODEME_KALEMLERI",
           "satirlar":[{"arac":"NAKIT","tutarTl":20000.00,"fisAdet":1,"kasayaGirer":true}],
           "brutSatisTl":20000.00,"kirilimToplamTl":20000.00,"farkTl":0.00,
           "kasayaGirenTl":20000.00,"mahsupAltinTl":0.00,"veresiyeTl":0.00,
           "fisAdet":1,"kalemsizFisAdet":0,"kalemsizTl":0.00}}
        """;

    private const string BosYanit = """
        {"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI","araclar":[],
         "veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"VERI_YOK",
         "satisKirilimi":{"kaynak":"ODEME_KALEMLERI","satirlar":[],"brutSatisTl":0,"kirilimToplamTl":0,"farkTl":0,
           "kasayaGirenTl":0,"mahsupAltinTl":0,"veresiyeTl":0,"fisAdet":0,"kalemsizFisAdet":0,"kalemsizTl":0}}
        """;


    private const string Katalog = """
        {"toplam":15,"faz":"A","raporlar":[
          {"kod":"R01","ad":"Günlük Özet","rota":"/raporlar","uc":"/api/v1/raporlar/gunluk","roller":["SAHIP"],"faz":"A","grup":"GUNLUK"},
          {"kod":"R10","ad":"İşlem Defteri","rota":"/raporlar/islem-defteri","uc":"/api/v1/raporlar/islem-defteri","roller":["SAHIP"],"faz":"A","grup":"SATIS"},
          {"kod":"R12","ad":"Ödeme Aracı Kırılımı","rota":"/raporlar/satis-analizi","uc":"/api/v1/raporlar/odeme-araci","roller":["SAHIP"],"faz":"A","grup":"SATIS"},
          {"kod":"R11","ad":"Kasa Defteri","rota":"/raporlar/kasa","uc":"/api/v1/raporlar/kasa-defteri","roller":["SAHIP"],"faz":"A","grup":"GUNLUK"}]}
        """;

    // P-25 / Y-16 — R15 sahte yanıtı. '{}' varsayılanı BIRAKILMAZ: zarf null
    // alanla gelirse Raporlar.razor sessizce boş çizerdi (gerçekçi yanıt şart).
    private const string Yaslandirma = """
        {"olculduAn":"2026-09-22T06:00:00Z","subeId":null,
         "kovalar":[
           {"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":4.000,"hasGram":12.500000,"maliyetTl":50000.00,"maliyetiBilinmeyenAdet":0.000},
           {"kod":"G31_90","altSinirGun":31,"ustSinirGun":90,"adet":2.000,"hasGram":6.250000,"maliyetTl":25000.00,"maliyetiBilinmeyenAdet":1.000},
           {"kod":"G91_180","altSinirGun":91,"ustSinirGun":180,"adet":0.000,"hasGram":0.000000,"maliyetTl":0.00,"maliyetiBilinmeyenAdet":0.000},
           {"kod":"G181_365","altSinirGun":181,"ustSinirGun":365,"adet":1.000,"hasGram":3.125000,"maliyetTl":0.00,"maliyetiBilinmeyenAdet":1.000},
           {"kod":"G365_USTU","altSinirGun":366,"ustSinirGun":null,"adet":3.000,"hasGram":9.375000,"maliyetTl":30000.00,"maliyetiBilinmeyenAdet":0.000}],
         "toplamAdet":10.000,"altinHasGram":31.250000,
         "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":10.000,"toplamGram":31.250000}],
         "yasiBilinmeyenAdet":2.000,
         "devir":{"pencereGun":90,"tuketilenAdet":5.000,"kalanAdet":10.000,"devirHizi":0.5000,
                  "yillikTahmini":2.0278,"ortalamaRafOmruGun":180.0,"yontem":"PARTI_TUKETIMI",
                  "tanim":"devirHizi = pencerede tüketilen adet ÷ bugün rafta kalan adet"},
         "oluStok":{"esikGun":180,"toplamSatir":2,"toplamAdet":4.000,"ofset":0,"limit":100,
                    "dahaFazlasiVar":false,
                    "urunler":[{"urunId":"u1","kod":"BLZ-1","ad":"Bilezik","ayarId":"K22","maden":"ALTIN",
                                "adet":3.000,"hasGram":9.375000,"enEskiGiris":"2024-01-15T00:00:00Z","yasGun":616},
                               {"urunId":"u2","kod":"YZK-7","ad":"Yüzük","ayarId":"K14","maden":"ALTIN",
                                "adet":1.000,"hasGram":3.125000,"enEskiGiris":"2025-01-10T00:00:00Z","yasGun":255}]},
         "guvenilirlik":"YUKSEK",
         "uyarilar":["2 adet malın maliyet partisi yok; yaşı BİLİNMİYOR ve kovalara dağıtılmadı."]}
        """;

    private const string Mutabakat = """
        {"tarih":"2026-09-17",
         "tahsilat":{"nakit":150.00,"kart":0,"havale":0,"diger":0,"toplam":150.00},
         "satis":{"sayisi":1,"toplamTl":4000.00,"faturaliSayisi":0,"faturaliTl":0,
                  "faturasizSayisi":1,"faturasizTl":4000.00},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,
                  "stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":4000.00,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],
         "faturaEslesme":null}
        """;

    public G5SatisKirilimiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // varsayılan PROFESYONEL: kart bu yüzdedir
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(G5SatisKirilimiEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            t.istekler.Add((yol, istek.RequestUri.Query));

            if (yol.EndsWith("/raporlar/odeme-araci") && t.odemeAraciHata)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Rapor hesaplanamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/odeme-araci") => t.odemeAraci,
                var y when y.EndsWith("/raporlar/katalog") => Katalog,
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{"0-30":0,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => Yaslandirma,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private DilServisi Dil => Services.GetRequiredService<DilServisi>();

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/raporlar/odeme-araci")));
        e.WaitForAssertion(() => Assert.Null(e.FindAll("#sk-kart .yukleniyor-kutu").FirstOrDefault()));
        // ZO-BELGE (27.09): kırılım KAPALI katlanırda; hata varsa katlanır AÇIK doğar
        // (tıklanmaz — kapatmamak için). Açık değilse başlığa tıklanır.
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-kart-ac")));
        if (e.Find("#sk-kart-ac").GetAttribute("aria-expanded") != "true") e.Find("#sk-kart-ac").Click();
        return e;
    }

    /// <summary>
    /// G5-5 — ALTIN satırı sözlükteki adla çizilir; kartta ham "ALTIN" kodu YOK
    /// (TR ve EN). İstek seçili YIL/AY'ın tam ayını sorar.
    /// </summary>
    [Fact]
    public async Task G5_Ekranda_ham_ALTIN_kodu_basilmaz()
    {
        var e = Ac();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-satir-altin")));

        var tr = SatisKirilimiSozlugu.Tr;
        Assert.Equal(tr.EskiAltinMahsup, e.Find("#sk-satir-altin td").TextContent.Trim());
        Assert.Contains(tr.KasayaGirmez, e.Find("#sk-satir-altin").TextContent);
        Assert.DoesNotContain("ALTIN", e.Find("#sk-kart").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("NAKIT", e.Find("#sk-kart").TextContent, StringComparison.Ordinal);

        var sorgu = istekler.Last(i => i.Yol.EndsWith("/raporlar/odeme-araci")).Sorgu;
        var bugun = DateTime.Today;
        var sonGun = DateTime.DaysInMonth(bugun.Year, bugun.Month);
        Assert.Contains($"baslangic={bugun:yyyy-MM}-01", sorgu);
        Assert.Contains($"bitis={bugun:yyyy-MM}-{sonGun:00}", sorgu);

        await e.InvokeAsync(() => Dil.DegistirAsync("en"));
        e.WaitForAssertion(() => Assert.Equal(SatisKirilimiSozlugu.En.EskiAltinMahsup,
            e.Find("#sk-satir-altin td").TextContent.Trim()));
        Assert.DoesNotContain("ALTIN", e.Find("#sk-kart").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// G5-2 (ekran) — "Kasaya giren" kartı SUNUCUNUN kasayaGirenTl'sini basar;
    /// eski altın ayrı kartta, satış toplamı brüttür. İstemci toplam UYDURMAZ.
    /// </summary>
    [Fact]
    public void G5_Ekran_kasaya_giren_karti_eski_altini_katmaz()
    {
        var e = Ac();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-ozet")));
        var ozet = e.Find("#sk-ozet").TextContent;
        var tr = SatisKirilimiSozlugu.Tr;
        Assert.Contains(tr.KasayaGiren, ozet);
        Assert.Contains(Bicim.BelgePara(10_840m, "TRY"), ozet);
        Assert.Contains(tr.EskiAltinMahsup, ozet);
        Assert.Contains(Bicim.BelgePara(9_160m, "TRY"), ozet);
        Assert.Contains(Bicim.BelgePara(20_000m, "TRY"), ozet);
        Assert.Equal(Bicim.BelgePara(20_000m, "TRY"), e.Find("#sk-toplam").TextContent.Trim());
        Assert.Contains(Dil.T.Islem.OdemeNakit, e.Find("#sk-satir-nakit").TextContent);
        Assert.Empty(e.FindAll("#sk-fark"));
    }

    /// <summary>G5-3 (ekran) — altınsız dönemde eski altın satırı ve kartı ÇİZİLMEZ.</summary>
    [Fact]
    public void G5_Ekran_altinsiz_donemde_eski_altin_satiri_ve_karti_yok()
    {
        odemeAraci = AltinsizYanit;
        var e = Ac();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-satir-nakit")));
        Assert.Empty(e.FindAll("#sk-satir-altin"));
        Assert.DoesNotContain(SatisKirilimiSozlugu.Tr.EskiAltinMahsup, e.Find("#sk-kart").TextContent);
        Assert.Single(e.FindAll("#sk-tablo tbody tr"));
    }

    /// <summary>Boş dönem: boş durum görünür, tablo çizilmez.</summary>
    [Fact]
    public void G5_Ekran_satissiz_donemde_bos_durum()
    {
        odemeAraci = BosYanit;
        var e = Ac();
        e.WaitForAssertion(() => Assert.Contains(SatisKirilimiSozlugu.Tr.SatisYok, e.Find("#sk-kart").TextContent));
        Assert.Empty(e.FindAll("#sk-tablo"));
    }

    /// <summary>UX-S28 — hata "satış yok"a çevrilmez: hata bandı görünür, boş durum basılmaz.</summary>
    [Fact]
    public void G5_Ekran_hata_bos_duruma_cevrilmez()
    {
        odemeAraciHata = true;
        var e = Ac();
        e.WaitForAssertion(() => Assert.Contains("Rapor hesaplanamadı", e.Find("#sk-kart").TextContent));
        Assert.DoesNotContain(SatisKirilimiSozlugu.Tr.SatisYok, e.Find("#sk-kart").TextContent);
        Assert.Empty(e.FindAll("#sk-tablo"));
    }
}
