using Kuyumcu.Core.I18n;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 34 Faz A — RAPOR EKRANLARI [bUnit]
//
// "Düğme var" YETMEZ (DoD ek 1): her düğme ya GERÇEK bir istek atar ya da
// disabled'dır. Bu dosya hangi uca, hangi query ile gidildiğini sahte API'nin
// kayıt listesinden okur (CariEkranTestleri deseni).
//
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: yeni uçlara GERÇEKÇİ yanıt verilir,
//   aksi hâlde ekran boş çizilir ve testler "sessizce" yeşil kalır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RaporEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];
    /// <summary>ZO-BELGE: bu uçlara 500 döner (hata katlanırın arkasına saklanmaz sınaması).</summary>
    private readonly HashSet<string> hatali = [];

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

    private const string Defter = """
        {"baslangic":"2026-09-11","bitis":"2026-09-17","toplamSatir":250,"ofset":0,"limit":100,
         "dahaFazlasiVar":true,
         "ozet":{"tlGiris":25000.00,"tlCikis":3000.00,"hasGiris":2.5,"hasCikis":1.0,"satirSayisi":250},
         "storno":{"adet":1,"tutarTl":99000.00},
         "satirlar":[
           {"id":"h1","tarih":"2026-09-17T09:15:00Z","tur":"SATIS","yon":"GIRIS","tlTutari":5000.00,
            "hasGrami":-1.0,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":"NAKIT","nakitTutar":5000.00,
            "veresiyeTutar":0,"fisId":null,"faturaNo":"FTR-1","cariAd":"Ayşe Demir","urunAd":"Bilezik",
            "subeAd":"Merkez","kullaniciAd":"Test Kullanici","stornoSatiri":false,"stornolandi":false},
           {"id":"h2","tarih":"2026-09-17T10:00:00Z","tur":"ALIS","yon":"CIKIS","tlTutari":-3000.00,
            "hasGrami":2.0,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":"NAKIT","nakitTutar":null,
            "veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,"urunAd":null,
            "subeAd":"Merkez","kullaniciAd":"Test Kullanici","stornoSatiri":false,"stornolandi":false}]}
        """;

    private const string OdemeAraci = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","kaynak":"KASA_HAREKETLERI",
         "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":12000.00,"cikisTutar":0,"adet":3}],
                     "girisTlToplam":12000.00,"adet":3}],
         "veresiye":{"doganTl":3000.00,"kapananTl":0},
         "bilinmeyen":{"nakitPayiBilinmeyenSatir":1,"nakitPayiBilinenSatir":4},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"YUKSEK"}
        """;

    private const string Personel = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","olcut":"KAYDEDEN_KULLANICI",
         "uyari":"SATAN_DEGIL_KAYDEDEN","toplamSatir":1,
         "satirlar":[{"kullaniciId":"u1","kaydedenAd":"Zeynep Yılmaz","islemSayisi":4,"ciroTl":40000.00,
                      "ortalamaSepetTl":10000.00,"iadeAdet":1,"iadeTl":2000.00,"iadeOrani":0.25}]}
        """;

    private const string SubeKarsilastirma = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","transferCiroyaKatilir":false,"toplamSatir":1,
         "toplamCiroTl":30000.00,
         "satirlar":[{"subeId":"s1","subeAd":"Merkez","satisAdet":2,"ciroTl":30000.00,"alisTl":0,
                      "hurdaTl":0,"hurdaHasGram":0,"malFarkiTl":30000.00,"transferAdet":1,"transferTl":50000.00}]}
        """;

    private const string KasaDefteri = """
        {"baslangic":"2026-09-04","bitis":"2026-09-17","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,
         "yazSaatiRiski":false,"toplamSatir":1,
         "kasalar":[{"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":250000.00,
                     "kapanisBakiye":251500.00,"guncelBakiye":251500.00,
                     "gunler":[{"tarih":"2026-09-16","devir":250000.00,"giris":0,"cikis":0,"hareketSayisi":0,
                                "kapanis":250000.00,"durum":"ACIK","sayilanNakit":null,
                                "kapanisDefterBakiye":null,"fark":null},
                               {"tarih":"2026-09-17","devir":250000.00,"giris":1500.00,"cikis":0,"hareketSayisi":1,
                                "kapanis":251500.00,"durum":"KAPALI","sayilanNakit":251450.00,
                                "kapanisDefterBakiye":251500.00,"fark":-50.00}]}]}
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

    public RaporEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, hatali))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // S3-4: Raporlar iki yüzlü (mod okunur; varsayılan PROFESYONEL)
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, HashSet<string> hatali) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol, istek.RequestUri.Query));
            if (hatali.Any(h => yol.EndsWith(h, StringComparison.Ordinal)))
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Katalog okunamadı (test)"}""", System.Text.Encoding.UTF8, "application/json"),
                });

            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => Katalog,
                var y when y.EndsWith("/raporlar/islem-defteri") => Defter,
                var y when y.EndsWith("/raporlar/odeme-araci") => OdemeAraci,
                var y when y.EndsWith("/raporlar/personel") => Personel,
                var y when y.EndsWith("/raporlar/sube-karsilastirma") => SubeKarsilastirma,
                var y when y.EndsWith("/raporlar/kasa-defteri") => KasaDefteri,
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => Yaslandirma,
                // ⚠ '{}' varsayılanı BIRAKILMAZ: mutabakat zarfı null alanla gelirse
                //   Raporlar.razor NullReference atar (gerçekçi yanıt şart).
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar") => """[{"id":"u1","ad":"Test","soyad":"Kullanici"},{"id":"u2","ad":"Zeynep","soyad":"Yılmaz"}]""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"USD Kasa","paraBirimi":"USD","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void OturumKur(string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private List<string> Sorgular(string ucEki)
        => istekler.Where(i => i.Yol.EndsWith(ucEki, StringComparison.Ordinal)).Select(i => i.Sorgu).ToList();

    // ── P-25 / Y-16 — R15 STOK YAŞLANDIRMA KARTI (Raporlar.razor) ───────────

    /// <summary>
    /// M25-19 — sayfa açılışında R15 ucuna GERÇEK istek atılır ve kart çizilir.
    /// "Sunucuda uç var, ekranda yok" kabul edilmez (UX-S25).
    /// </summary>
    [Fact]
    public void Raporlar_acilista_stok_yaslandirma_ucuna_istek_atar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/stok-yaslandirma")));

        var sorgu = Sorgular("/raporlar/stok-yaslandirma")[0];
        Assert.Contains("oluEsikGun=180", sorgu, StringComparison.Ordinal);
        Assert.Contains("devirPenceresiGun=90", sorgu, StringComparison.Ordinal);
        Assert.Contains("limit=100", sorgu, StringComparison.Ordinal);

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#stok-yaslandirma-kart")));
    }

    /// <summary>
    /// M25-20 — UX-S33: kart üstteki yıl/ay seçicisinden bağımsız olduğunu
    /// EKRANDA yazar. Yazmasaydı kullanıcı seçtiği ayın stoğunu sanırdı.
    /// </summary>
    [Fact]
    public void Stok_yaslandirma_karti_donemden_bagimsiz_oldugunu_yazar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-donem-bagimsiz")));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sy-donem-bagimsiz").TextContent));
    }

    /// <summary>
    /// M25-21 — yaş kovaları HAM KOD basmaz (UX-S31): "G0_30" değil "0-30 gün".
    /// </summary>
    [Fact]
    public void Stok_yaslandirma_kova_kodlarini_ham_basmaz()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#stok-yaslandirma-kart")));

        var metin = e.Find("#stok-yaslandirma-kart").TextContent;
        Assert.DoesNotContain("G0_30", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("G365_USTU", metin, StringComparison.Ordinal);
        Assert.Contains("0-30", metin, StringComparison.Ordinal);
    }

    /// <summary>
    /// M25-22 — B-64: GEÇERSİZ SÜZGEÇTE ESKİ TABLO GEÇERSİZLEŞTİRİLİR.
    /// Eşik sıfırlanınca eski ölü stok satırları ekranda KALMAZ ve hata bandı
    /// çıkar; kalsaydı kullanıcı onları yeni eşiğin sonucu sanırdı.
    /// </summary>
    [Fact]
    public void Gecersiz_esikte_eski_tablo_gecersizlesir_ve_hata_bandi_cikar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Contains("BLZ-1", e.Find("#stok-yaslandirma-kart").TextContent));

        var oncekiIstek = Sorgular("/raporlar/stok-yaslandirma").Count;

        // Ortak girdi ailesi metin girdisi kullanır; 0 geçersizdir.
        e.Find("#sy-esik").Input("0");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-hata")));
        // Bayat tablo GİTTİ.
        Assert.DoesNotContain("BLZ-1", e.Find("#stok-yaslandirma-kart").TextContent);
        // Ve sunucuya GİDİLMEDİ (geçersiz istek atılmaz).
        Assert.Equal(oncekiIstek, Sorgular("/raporlar/stok-yaslandirma").Count);

        // ⚠ TEST AÇIĞI KAPATILDI (P-25 mutasyon M25-8 / M25-8b, 2026-09-22):
        //   B-64'ü iki ayrı hat birden kapatıyor — (1) geçersiz süzgeçte
        //   `yaslandirma = null` ile veriyi düşürmek, (2) `syHata is not null`
        //   iken tabloyu hiç çizmemek. İkisi BİRBİRİNİ GÖLGELİYOR: yalnız biri
        //   silindiğinde davranış değişmiyor, dolayısıyla yukarıdaki iddialar
        //   tek başına hiçbir mutasyonu yakalayamıyordu. Yedeklilik BİLEREK
        //   korunuyor; o yüzden her iki hat da kaynakta ADIYLA kilitlenir.
        var kaynak = File.ReadAllText(KaynakYolu("Kuyumcu.Web/Pages/Raporlar.razor"));
        Assert.Contains("yaslandirma = null;\n            return;", kaynak, StringComparison.Ordinal);
        Assert.Contains("@if (syHata is not null)", kaynak, StringComparison.Ordinal);
    }

    /// <summary>Kaynak dosya yolu — çözüm kökü `Kuyumcu.sln` ile bulunur.</summary>
    private static string KaynakYolu(string goreli)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        var yol = Path.Combine(d!.FullName, goreli);
        Assert.True(File.Exists(yol), $"kaynak bulunamadı: {yol}");
        return yol;
    }

    /// <summary>
    /// M25-23 — eşik değişince sunucuya YENİ istek atılır (düğme/alan
    /// "tıklanınca hiçbir şey olmayan" olamaz — DoD ek 1).
    /// </summary>
    [Fact]
    public void Esik_degisince_sunucuya_yeni_istek_atilir()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/stok-yaslandirma")));
        var once = Sorgular("/raporlar/stok-yaslandirma").Count;

        e.Find("#sy-esik").Input("365");

        e.WaitForAssertion(() =>
            Assert.Contains(Sorgular("/raporlar/stok-yaslandirma"),
                q => q.Contains("oluEsikGun=365", StringComparison.Ordinal)));
        Assert.True(Sorgular("/raporlar/stok-yaslandirma").Count > once);
    }

    // ── İşlem defteri ────────────────────────────────────────────────────────

    [Fact]
    public void IslemDefteri_acilista_sunucuya_limitli_istek_atar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/islem-defteri")));

        var sorgu = Sorgular("/raporlar/islem-defteri")[0];
        // PAKET S1-i: sayfa boyu 100 → 10; ZO-BELGE 29.09: 10 → 6 (1440 açılış ≤ 1,0 ekran). Tek kaynak sabit.
        Assert.Contains($"limit={GorselSigmaSozlugu.IslemDefteriSayfaBoyu}", sorgu, StringComparison.Ordinal);
        Assert.Contains("ofset=0", sorgu, StringComparison.Ordinal);
        Assert.Contains("baslangic=", sorgu, StringComparison.Ordinal);
    }

    [Fact]
    public void IslemDefteri_suzgec_degisince_SUNUCUYA_yeni_istek_atar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/islem-defteri")));
        istekler.Clear();

        e.Find("#id-suzgec-ac").Click();   // ZO-BELGE: süzgeç KAPALI katlanırda
        SeciciYardim.Sec(e, "id-tur", "SATIS_IADE");

        e.WaitForAssertion(() =>
        {
            var sorgular = Sorgular("/raporlar/islem-defteri");
            Assert.NotEmpty(sorgular);
            Assert.Contains("tur=SATIS_IADE", sorgular[^1], StringComparison.Ordinal);
        });
    }

    [Fact]
    public void IslemDefteri_sayfa_ileri_geri_ofseti_gunceller()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/islem-defteri")));
        istekler.Clear();

        e.Find("#id-sonraki").Click();
        e.WaitForAssertion(() =>
            Assert.Contains($"ofset={GorselSigmaSozlugu.IslemDefteriSayfaBoyu}", Sorgular("/raporlar/islem-defteri")[^1], StringComparison.Ordinal));   // tek kaynak sayfa boyu

        istekler.Clear();
        e.Find("#id-onceki").Click();
        e.WaitForAssertion(() =>
            Assert.Contains("ofset=0", Sorgular("/raporlar/islem-defteri")[^1], StringComparison.Ordinal));
    }

    [Fact]
    public void IslemDefteri_sunucu_sinirini_ekranda_SOYLER()
    {
        // UX-S29 — sessiz kırpma yok.
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#id-sinir")));
    }

    [Fact]
    public void IslemDefteri_bilinmeyen_nakit_payi_SIFIR_yazilmaz()
    {
        // Anayasa md.1 — nakitTutar null olan satır "0,00 ₺" göstermez.
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() =>
        {
            var satirlar = e.FindAll("#id-tablo tbody tr");
            Assert.Equal(2, satirlar.Count);
            Assert.Contains("Bilinmiyor", satirlar[1].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void IslemDefteri_TL_isareti_ekranda_korunur()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() =>
        {
            var satirlar = e.FindAll("#id-tablo tbody tr");
            // Alış satırı EKSİ işaretiyle görünür (Math.Abs uygulanmaz).
            Assert.Contains("\u2212", satirlar[1].TextContent, StringComparison.Ordinal);   // T-K1: Bicim.Tl eksi işareti U+2212
        });
    }

    [Fact]
    public void IslemDefteri_her_dugme_ya_istek_atar_ya_KILITLIDIR()
    {
        // DoD ek 1 — "tıklanınca hiçbir şey olmayan" düğme yok.
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/islem-defteri")));

        foreach (var dugme in e.FindAll("button"))
        {
            if (dugme.HasAttribute("disabled")) continue;
            // P-36c: ortak <Secici>'nin tetiği de bir <button>'dır. Tıklanınca
            // istek atmaz ama SESSİZ DEĞİLDİR: seçenek listesini açar
            // (aria-expanded true). DoD ek md.1'in yasakladığı "hiçbir şey
            // olmayan düğme" değildir.
            if (dugme.GetAttribute("role") == "combobox") continue;
            // ZO-BELGE: ortak Katlanir başlığı istek atmaz ama SESSİZ DEĞİLDİR —
            // gövdeyi açar (aria-expanded değişir). Ölçülür, sonra geçilir.
            // PAKET GOC-D: ortak ızgaranın "Sütunlar" düğmesi istek atmaz ama SESSİZ DEĞİLDİR — sütun panelini
            // açar (aria-expanded değişir). Ölçülür, sonra geçilir (katlanır kalıbıyla aynı).
            if (dugme.ClassList.Contains("izgara-sutunlar-ac"))
            {
                var onceIz = dugme.GetAttribute("aria-expanded");
                dugme.Click();
                Assert.NotEqual(onceIz, e.Find("#" + dugme.Id).GetAttribute("aria-expanded"));
                continue;
            }
            if (dugme.ClassList.Contains("katlanir-baslik"))
            {
                var once = dugme.GetAttribute("aria-expanded");
                dugme.Click();
                Assert.NotEqual(once, e.Find("#" + dugme.Id).GetAttribute("aria-expanded"));
                continue;
            }
            istekler.Clear();
            dugme.Click();
            // Yazdır düğmesi JS çağırır (istek atmaz) ama sessiz DEĞİLDİR;
            // diğer düğmeler sunucuya gider.
            var id = dugme.GetAttribute("id") ?? "";
            if (id == "id-yazdir") continue;
            Assert.NotEmpty(istekler);
        }
    }

    // ── Satış analizi ────────────────────────────────────────────────────────

    [Fact]
    public void SatisAnalizi_sekme_degisince_ILGILI_uca_gider()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/odeme-araci")));

        e.Find("#sa-sekme-personel").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/personel")));

        e.Find("#sa-sekme-sube").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/sube-karsilastirma")));
    }

    [Fact]
    public void Personel_tablosu_KAYDEDEN_der_SATAN_ICERMEZ()
    {
        // M114 ekran tarafı.
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/odeme-araci")));

        e.Find("#sa-sekme-personel").Click();
        e.WaitForAssertion(() =>
        {
            var baslik = e.Find("#sa-personel-tablo thead").TextContent;
            Assert.Contains("Kaydeden", baslik, StringComparison.Ordinal);
            Assert.DoesNotContain("Satan", baslik, StringComparison.Ordinal);
            Assert.DoesNotContain("Tezgâhtar", baslik, StringComparison.Ordinal);
            // Uyarı gizlenmez.
            Assert.Contains("KAYDEDEN", e.Find("#sa-personel-uyari").TextContent.ToUpperInvariant(),
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Odeme_araci_bilinmeyen_nakit_payini_BEYAN_eder()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-bilinmeyen")));
    }

    [Fact]
    public void Sube_karsilastirma_TRANSFER_notunu_gosterir()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/odeme-araci")));
        e.Find("#sa-sekme-sube").Click();
        e.WaitForAssertion(() =>
            Assert.Contains("KATILMAZ", e.Find("#sa-transfer-notu").TextContent, StringComparison.Ordinal));
    }

    // ── Kasa defteri ─────────────────────────────────────────────────────────

    [Fact]
    public void Kasa_raporu_kapanmamis_gunu_ACIK_gosterir_ve_farki_bos_birakir()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        // ZO-BELGE: gün tablosu KAPALI "Geçmiş kayıtlar" katlanırında.
        e.WaitForElement("#kr-gecmis-ac");
        e.Find("#kr-gecmis-ac").Click();
        e.WaitForAssertion(() =>
        {
            var satirlar = e.FindAll("table.veri-tablosu tbody tr");
            Assert.Equal(2, satirlar.Count);
            Assert.Contains("AÇIK", satirlar[0].TextContent, StringComparison.Ordinal);
            // Kapanmamış günün farkı "—" (sıfır DEĞİL).
            Assert.Contains("—", satirlar[0].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Kasa_raporu_kasa_secilince_yeni_istek_atar()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotEmpty(Sorgular("/raporlar/kasa-defteri")));
        istekler.Clear();

        // ZO-BELGE: kasa seçici "Geçmiş kayıtlar" katlanırında, ortak Secici.
        e.Find("#kr-gecmis-ac").Click();
        SeciciYardim.Sec(e, "kr-kasa", "ks1");
        e.WaitForAssertion(() =>
            Assert.Contains("kasaId=ks1", Sorgular("/raporlar/kasa-defteri")[^1], StringComparison.Ordinal));
    }

    // ── Rapor Merkezi ────────────────────────────────────────────────────────

    [Fact]
    public void Rapor_Merkezi_katalogdan_kart_cizer()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        // ZO-BELGE (27.09): rapor merkezi KAPALI "Tüm raporlar" katlanırında; başlık sayıyı yazar.
        e.WaitForAssertion(() => Assert.Equal("4", e.Find("#rp-tum-raporlar-sayi").TextContent.Trim()));
        e.Find("#rp-tum-raporlar-ac").Click();
        e.WaitForAssertion(() =>
        {
            var kartlar = e.FindAll("#rapor-merkezi button");
            Assert.Equal(4, kartlar.Count);   // sahte katalogdaki rapor sayısı
            // UX-S31: ham kod (R10) basılmaz, çevrilmiş ad basılır.
            var metin = e.Find("#rapor-merkezi").TextContent;
            // P-14 (Y-9): ad sözlükten gelir; sabit dizge yazılırsa etiket
            // çarpışması düzeltmesi bu testi bayatlatır.
            // ZO-BELGE 28.09 (UX-S31, doğrulayıcı ORTA): R10/R11 düğmesi gittiği sayfanın
            // başlığı = menü adı (MenuAdlari tek kaynak); eski katalog adı ("Hareket Dökümü",
            // "Kasa Hareketleri") rapor merkezinde görünmez.
            var m = Kuyumcu.Core.I18n.MenuAdlari.Tr;
            Assert.Equal(m.IslemDefteri.Ad, e.Find("#rm-r10").TextContent.Trim());
            Assert.Equal(m.KasaRaporu.Ad, e.Find("#rm-r11").TextContent.Trim());
            Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.AdIslemDefteri, metin, StringComparison.Ordinal);
            Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.AdKasaDefteri, metin, StringComparison.Ordinal);
            // Diğer raporlar katalog adıyla (sözlükten).
            Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.Ad("R12"), e.Find("#rm-r12").TextContent.Trim());
            Assert.DoesNotContain("R10", metin, StringComparison.Ordinal);
        });
    }

    // ── ZO-BELGE (27.09) — PRO Özet: açılış ≤ 1 ekran ─────────────────────────

    /// <summary>
    /// ZB19 — PRO Özet sekmesinde rapor merkezi, ödeme kırılımı ve maden tablosu KAPALI
    /// katlanırdadır; başlıklar SAYIYI yazar (sessiz gizleme yok); açılınca aynı içerik.
    /// </summary>
    [Fact]
    public void ZB19_PRO_ozet_rapor_merkezi_kirilim_KAPALI_baslik_sayiyi_yazar_acilinca_ayni_icerik()
    {
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal("4", e.Find("#rp-tum-raporlar-sayi").TextContent.Trim()));
        Assert.Equal("false", e.Find("#rp-tum-raporlar-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#rapor-merkezi"));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-kart-ac")));
        Assert.Equal("false", e.Find("#sk-kart-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#sk-tablo"));

        e.Find("#rp-tum-raporlar-ac").Click();
        Assert.Equal(4, e.FindAll("#rapor-merkezi button").Count);
        e.Find("#sk-kart-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sk-kart-govde")));
        Assert.Equal("true", e.Find("#sk-kart-ac").GetAttribute("aria-expanded"));
        // Görsel kusur (27.09 ölçümü): kırılım ve maden tabloları sınıfsızdı (css'te "tablo"
        // kuralı YOK → başlıksız, kenarsız ham tablo) → ortak veri tablosu. Kaynak kilidi:
        var kaynak = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages", "Raporlar.razor"));
        Assert.DoesNotContain("<table class=\"tablo\"", kaynak, StringComparison.Ordinal);
    }

    /// <summary>ZB20 — UX-S28: katalog okunamazsa katlanır AÇIK doğar, hata kapalı kartın arkasına saklanmaz.</summary>
    [Fact]
    public void ZB20_PRO_katalog_hatasi_katlanir_ACIK_hata_gorunur()
    {
        hatali.Add("/raporlar/katalog");
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Contains("Katalog okunamadı", e.Find("#rp-tum-raporlar").TextContent, StringComparison.Ordinal));
        Assert.Equal("true", e.Find("#rp-tum-raporlar-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#rp-tum-raporlar-sayi"));   // sayı bilinmiyor → "0" yazılmaz
    }

    /// <summary>ZB21 — UX-S28: ödeme kırılımı okunamazsa katlanır AÇIK doğar, hata tıklamadan görünür.</summary>
    [Fact]
    public void ZB21_PRO_odeme_kirilimi_hatasi_katlanir_ACIK_hata_gorunur()
    {
        hatali.Add("/raporlar/odeme-araci");
        OturumKur();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#sk-kart-ac").GetAttribute("aria-expanded")));
        Assert.Contains("Katalog okunamadı", e.Find("#sk-kart").TextContent, StringComparison.Ordinal);   // sahte 500 metni
        Assert.Empty(e.FindAll("#sk-kart-sayi"));
    }
}
