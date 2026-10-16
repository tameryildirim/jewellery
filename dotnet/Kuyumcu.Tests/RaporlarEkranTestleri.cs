using Bunit;
using Kuyumcu.Contracts.Sayfalar.Raporlar;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-47 (QA turu 8 triyajı, KRİTİK) — CARİ YAŞLANDIRMADA HAS GRAM KOLONU [bUnit]
//
// ÖLÇÜLEN HATA: /raporlar cari yaşlandırma tablosu yalnız TL basıyordu. API
// hasGramBakiye:-16.946 dönerken ekran satırı "0,00 ₺"tan ibaretti — 16,946 g
// has alacağı kurucunun önünde GİZLİYDİ.
//
// Bu dosya üç kuralı sınar:
//   P47_01 — has kolonu var ve değeri basıyor.
//   P47_02 — TL=0 & has≠0 satırı "boş" sayılmaz; vurgulanır, rozet taşır.
//   P47_03 — has bakiyesi TL toplamına KARIŞMAZ; kendi kolonunda ayrı toplanır.
//
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: sayfanın açılışta çağırdığı uçlara
//   gerçekçi yanıt verilir (RaporEkranTestleri deseni).
// ⚠ Biçim sabit YAZILMAZ: beklenen metin Bicim.Has / Bicim.Tl ile üretilir;
//   P-57 (Bicim tr-TR birleştirmesi) biçimi değiştirirse test yine doğru ölçer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RaporlarEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];
    private string cariYaslandirma = CariYaslandirmaVarsayilan;

    // c1: TL=0, has=-16,946 g (QA turu 8'deki gizli alacak — birebir şekil).
    // c2: TL=1.500, has=2,5 g.   c3: TL=0, has=0, döviz yok → gerçekten boş.
    private const string CariYaslandirmaVarsayilan = """
        {"yaslananCariler":[
           {"cariId":"c1","cariAd":"Has Müşterisi","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":-16.946,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"},
           {"cariId":"c2","cariAd":"TL Müşterisi","cariTur":"MUSTERI","tlBakiye":1500.00,"hasGramBakiye":2.5,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":45,"dilim":"31-60"},
           {"cariId":"c3","cariAd":"Boş Cari","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":null,"dilim":"BELIRSIZ"}],
         "ozetTl":{"0-30":0,"31-60":1500.00,"61-90":0,"90+":0,"BELIRSIZ":0},
         "toplamSatir":3,"ofset":0,"limit":500,"dahaFazlasiVar":false}
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

    public RaporlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => cariYaslandirma))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // S3-4: Raporlar iki yüzlü (mod okunur; varsayılan PROFESYONEL)
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> yaslandirma) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol, istek.RequestUri.Query));

            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => Katalog,
                // G-5: sayfa açılışta satış ödeme kırılımını da ister — gerçekçi (boş dönem) yanıt.
                var y when y.EndsWith("/raporlar/odeme-araci") =>
                    """{"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI","araclar":[],"veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},"storno":{"adet":0,"tutarTl":0},"guvenilirlik":"VERI_YOK","satisKirilimi":{"kaynak":"ODEME_KALEMLERI","satirlar":[],"brutSatisTl":0,"kirilimToplamTl":0,"farkTl":0,"kasayaGirenTl":0,"mahsupAltinTl":0,"veresiyeTl":0,"fisAdet":0,"kalemsizFisAdet":0,"kalemsizTl":0}}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => yaslandirma(),
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
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-tablo")));
        return e;
    }

    /// <summary>
    /// M47-1 — HAS GRAM kolonu tabloda VAR ve cari satırı has bakiyesini basar.
    /// TL'si 0 olan c1 satırı "0,00 ₺"tan ibaret DEĞİLDİR: -16,946 has görünür.
    /// Tablo KartListe içindedir (375 px'te yatay kaydırılabilir, klavyeyle odaklanır).
    /// </summary>
    [Fact]
    public void P47_01_Yaslandirma_has_gram_kolonu_degeri_basar()
    {
        var e = Ac();

        var baslik = e.Find("#yas-has-baslik");
        Assert.Equal(Dil.T.Rapor.YasHasBakiye, baslik.TextContent.Trim());
        // Başlık sırası: has kolonu TL toplamından SONRA, ayrı kolon.
        var basliklar = e.FindAll("#yas-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(7, basliklar.Count);
        Assert.Equal(Dil.T.Rapor.YasHasBakiye, basliklar[^1]);

        var hucre = e.Find("#yas-satir-c1 td.yas-has");
        Assert.Equal(Bicim.Has(-16.946m), hucre.TextContent.Trim());
        Assert.Contains("metin-kirmizi", hucre.ClassName, StringComparison.Ordinal);

        // c1 satırının metni yalnız "0,00 ₺" hücrelerinden ibaret değildir.
        var satirMetni = e.Find("#yas-satir-c1").TextContent;
        Assert.Contains(Bicim.Has(-16.946m), satirMetni, StringComparison.Ordinal);

        Assert.Equal(Bicim.Has(2.5m), e.Find("#yas-satir-c2 td.yas-has").TextContent.Trim());

        // 375 px: KartListe sarmalayıcısı (overflow-x:auto + tabindex=0 bölge).
        var sar = e.Find("#yas-sar");
        Assert.Equal("0", sar.GetAttribute("tabindex"));
        Assert.Contains("tablo-sar", sar.ClassName, StringComparison.Ordinal);

        // Tablo altı tek cümlelik not: has TL'ye çevrilmez.
        Assert.Equal(Dil.T.Rapor.YasHasNotu, e.Find("#yas-has-notu").TextContent.Trim());
    }

    /// <summary>
    /// M47-2 — satır TL'ye göre değil, TL VEYA has (VEYA döviz) bakiyesine göre
    /// "boş" sayılır. TL=0 & has≠0 satırı çizilir, vurgulanır, rozet taşır;
    /// üçü de sıfır olan satır çizilmez.
    /// </summary>
    [Fact]
    public void P47_02_TL_sifir_has_dolu_satir_bos_gorunmez()
    {
        var e = Ac();

        var satir = e.Find("#yas-satir-c1");
        Assert.Contains("satir-vurgu", satir.ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains(Dil.T.Rapor.YasHasBakiyeVar, satir.TextContent, StringComparison.Ordinal);

        // TL'si dolu satır rozet/vurgu taşımaz.
        var c2 = e.Find("#yas-satir-c2");
        Assert.DoesNotContain("satir-vurgu", c2.ClassName ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain(Dil.T.Rapor.YasHasBakiyeVar, c2.TextContent, StringComparison.Ordinal);

        // Gerçekten boş satır (TL=0, has=0, döviz yok) çizilmez.
        Assert.Empty(e.FindAll("#yas-satir-c3"));
        Assert.Equal(2, e.FindAll("#yas-tablo tbody tr").Count);

        // Saf kural: has dolu satır boş DEĞİL; döviz dolu satır boş DEĞİL.
        Assert.False(Kuyumcu.Web.Pages.Raporlar.YasSatiriBos(
            new YaslananCari("x", "X", "MUSTERI", 0m, -16.946m, 3, "0-30")));
        Assert.False(Kuyumcu.Web.Pages.Raporlar.YasSatiriBos(
            new YaslananCari("x", "X", "MUSTERI", 0m, 0m, 3, "0-30", DovizBakiyesiVar: true)));
        Assert.True(Kuyumcu.Web.Pages.Raporlar.YasSatiriBos(
            new YaslananCari("x", "X", "MUSTERI", 0m, 0m, 3, "0-30")));
    }

    /// <summary>
    /// M47-3 — has bakiyesi TL toplamına KARIŞMAZ: TL alt toplamı yalnız TL
    /// bakiyelerinin toplamıdır (0 + 1.500 = 1.500 ₺); has kendi kolonunda ayrı
    /// toplanır (-16,946 + 2,5 = -14,446 has). Satırdaki TL toplam hücresi de
    /// yalnız TL'dir.
    /// </summary>
    [Fact]
    public void P47_03_Has_bakiyesi_TL_toplamina_KARISMAZ()
    {
        var e = Ac();

        Assert.Equal(Bicim.TlKurus(1500m), e.Find("#yas-toplam-tl").TextContent.Trim());
        Assert.Equal(Bicim.Has(-14.446m), e.Find("#yas-toplam-has").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(0m), e.Find("#yas-satir-c1 td.yas-toplam-tl").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(1500m), e.Find("#yas-satir-c2 td.yas-toplam-tl").TextContent.Trim());

        // Dilim alt toplamları da yalnız TL: 0-30 diliminde yalnız has'lı c1 var → 0 ₺.
        Assert.Equal(Bicim.TlKurus(0m), e.Find("#yas-alt-toplam td.yas-0-30").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(1500m), e.Find("#yas-alt-toplam td.yas-31-60").TextContent.Trim());
        Assert.Equal(Dil.T.Rapor.YasListelenenToplam, e.Find("#yas-alt-toplam th").TextContent.Trim());
    }

    /// <summary>
    /// UX-S29 — sunucu sınırı ekranda söylenir: dahaFazlasiVar=true iken alt
    /// toplamın yalnız listelenenleri kapsadığı yazılır; false iken bant yoktur.
    /// </summary>
    [Fact]
    public void P47_04_Yaslandirma_siniri_ekranda_soylenir()
    {
        var e = Ac();
        Assert.Empty(e.FindAll("#yas-sinir"));

        cariYaslandirma = CariYaslandirmaVarsayilan
            .Replace("\"toplamSatir\":3", "\"toplamSatir\":750", StringComparison.Ordinal)
            .Replace("\"dahaFazlasiVar\":false}", "\"dahaFazlasiVar\":true}", StringComparison.Ordinal);
        var e2 = Render<Kuyumcu.Web.Pages.Raporlar>();
        e2.WaitForAssertion(() => Assert.NotNull(e2.Find("#yas-sinir")));
        Assert.Equal(string.Format(Dil.T.Rapor.YasSinirUyari, 3, 750), e2.Find("#yas-sinir").TextContent.Trim());
    }
}
