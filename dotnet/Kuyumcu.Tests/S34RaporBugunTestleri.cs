using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-4 — RAPORLAR "BUGÜN" TEK SAYFA (basit yüz) · PLAN S-3 "Raporlar (basit)"
//
// İKİ YÜZ, TEK GÖVDE (PLAN § 5):
//   • BASİT  → Bugün ne sattım? · Bugün ne aldım? (hurda AYRI) · Altın pozisyonum
//              (B-2) · Kasada ne var? · Bekleyen iş · Gün sonu (FARK VAR → düzeltme).
//   • PROFESYONEL → rapor merkezi + bütün kartlar AYNEN (RaporlarEkranTestleri korur).
//
// Sınanan sözleşmeler: UX-S21 (tarih istemciden gönderilmez) · UX-S23 (sayı →
// kayıt bağlantısı) · UX-S28 (hata/403 "kayıt yok"a çevrilmez) · UX-S29 (sınır
// yazılır) · UX-S31 (ham kod yok, TR/EN eksiksiz) · UX-S32 (para birimleri
// toplanmaz) · yüze göre yükleme · mod değişince eksik yüz yüklenir · birincil
// düğme yok · yetki ≠ mod.
// ⚠ bUnit sözleşmesi: sahte API "{}" varsayılanı bırakmaz — her uca GERÇEKÇİ yanıt.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S34RaporBugunTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private readonly Dictionary<string, (HttpStatusCode Kod, string Govde)> ozel = [];

    private const string GunlukVarsayilan = """
        {"tarih":"2026-09-23","satis":{"adet":3,"toplamTl":57550.00,"toplamHas":10.4},
         "alis":{"adet":2,"toplamTl":8000.00,"toplamHas":2.5},
         "hurda":{"toplamHas":1.2,"odenenTl":3000.00},"netHasGramHareketi":-6.7,"hareketSayisi":6,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":44200.00,"paraBirimi":"TRY"},
                    {"id":"k2","ad":"Banka","bakiye":40000.00,"paraBirimi":"TRY"},
                    {"id":"k3","ad":"Dolar Kasa","bakiye":500.00,"paraBirimi":"USD"}]}
        """;

    private const string MutabakatFarkli = """
        {"tarih":"2026-09-23",
         "tahsilat":{"nakit":61550.00,"kart":0,"havale":0,"diger":0,"toplam":61550.00},
         "satis":{"sayisi":3,"toplamTl":57550.00,"faturaliSayisi":1,"faturaliTl":4000.00,
                  "faturasizSayisi":2,"faturasizTl":53550.00},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,
                  "stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":2,"toplamTl":8000.00,"kdvToplam":0,"satisFaturasiTl":8000.00},
         "fark":53550.00,"mutabik":false,"uyarilar":["2 satışın faturası kesilmedi."],
         "faturasizIslemler":[],
         "faturaEslesme":{"bagliSayisi":1,"bagliTl":4000.00,"bagimsizSayisi":1,"bagimsizTl":4000.00,
                          "faturasizTl":53550.00,"farkTanimi":"FARK = kasaya giren para − satış faturaları",
                          "bagimsizFaturalar":[]}}
        """;

    private const string MutabakatTamam = """
        {"tarih":"2026-09-23",
         "tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
         "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,
                  "stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
        """;

    private const string BildirimVarsayilan = """
        {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,
         "bildirimler":[
           {"id":"b1","tur":"VADE_GECIKTI","onem":"KRITIK","baslik":"Hasan Kaya'nın vadesi geçti",
            "govde":"38.900 TL","hedefRota":"/raporlar/vade-takibi","ilgiliVarlikTuru":"Cari","ilgiliVarlikId":"c1",
            "olusturuldu":"2026-09-23T06:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
           {"id":"b2","tur":"DIS","onem":"UYARI","baslik":"Dış bağlantılı iş",
            "govde":"","hedefRota":"//kotu.ornek/yol","ilgiliVarlikTuru":null,"ilgiliVarlikId":null,
            "olusturuldu":"2026-09-23T05:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
        """;

    private const string PozisyonVarsayilan = """
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":14.4,"cariBorcGram":0,
                  "netGram":312.8,"alacakliCariSayisi":1,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-09-23T09:00:00Z","donemdenBagimsiz":true}
        """;

    private sealed class SahteApi(
        List<(string, string)> kayit,
        Dictionary<string, (HttpStatusCode, string)> ozel) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add((istek.Method.Method, tam));

            foreach (var (parca, (kod, govde)) in ozel)
            {
                if (yol.EndsWith(parca, StringComparison.Ordinal))
                    return Task.FromResult(Yanit(kod, govde));
            }

            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => GunlukVarsayilan,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati", StringComparison.Ordinal) => MutabakatFarkli,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) => BildirimVarsayilan,
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => PozisyonVarsayilan,
                // Profesyonel yüzün uçları (yalnız mod testlerinde çağrılır) — gerçekçi boş zarflar.
                var y when y.EndsWith("/raporlar/katalog", StringComparison.Ordinal) =>
                    """{"toplam":1,"faz":"A","raporlar":[{"kod":"R01","ad":"Günlük Özet","rota":"/raporlar","uc":"/api/v1/raporlar/gunluk","roller":["SAHIP"],"faz":"A","grup":"GUNLUK"}]}""",
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri", StringComparison.Ordinal) =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok", StringComparison.Ordinal) => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma", StringComparison.Ordinal) =>
                    """{"olculduAn":"2026-09-23T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar", StringComparison.Ordinal) => "[]",
                // PAKET RAPOR-2: Bekleyen iş kartı Panel'in tanımıyla uyum sayaçlarını ve yasal bantları da okur
                // (gerçekçi "iş yok" yanıtları).
                var y when y.EndsWith("/masak/ozet", StringComparison.Ordinal) => """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}""",
                var y when y.EndsWith("/gider-pusulalari/sure-takibi", StringComparison.Ordinal) =>
                    """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,"gecikmis":[],"yaklasan":[],"uyarilar":[]}""",
                var y when y.EndsWith("/masak/risk/ozet", StringComparison.Ordinal) =>
                    """{"dusuk":0,"orta":0,"yuksek":0,"pepSayisi":0,"tazelenecek":0,"profilsiz":0,"kuralEtkin":true,"listeSiniri":20,"yuksekRiskliler":[]}""",
                var y when y.EndsWith("/stopaj/ozet", StringComparison.Ordinal) =>
                    """{"buAy":"2026-10","buAyKesilenTl":0,"gecikmis":[],"yaklasan":[],"uyariGunu":5}""",
                var y when y.EndsWith("/masak/uyum-ozet", StringComparison.Ordinal) =>
                    """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":false,"listeTarihi":"2026-10-01","listeEski":false,"listeYasiGun":2,"acikTam":0,"acikBenzerlik":0}""",
                _ => """{"hata":"sahte API bu ucu tanımıyor"}""",
            };
            var durum = cevap.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return Task.FromResult(Yanit(durum, cevap));
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) => new(kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public S34RaporBugunTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, ozel))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private void OturumAc(string mod, string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Basit(string rol = "SAHIP")
    {
        OturumAc(ArayuzModuServisi.Basit, rol);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#raporlar-bugun");
        Bekle(e);
        return e;
    }

    private static void Bekle(IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> e)
        => e.WaitForAssertion(() =>
        {
            Assert.True(e.Instance.BugunYuklemesiBitti);
            Assert.Empty(e.FindAll(".yukleniyor-kutu"));
        }, TimeSpan.FromSeconds(10));

    private List<string> Yollar() { lock (istekler) return [.. istekler.Select(i => i.Yol)]; }

    // ── İKİ YÜZ ─────────────────────────────────────────────────────────────

    [Fact]
    public void S34_01_Basit_yuz_BUGUN_tek_sayfasini_cizer_rapor_merkezi_YOK()
    {
        var e = Basit();

        foreach (var id in new[] { "#bugun-satis", "#bugun-alis", "#pozisyon-kart", "#bugun-kasa",
                                   "#bugun-bekleyen", "#bugun-gun-sonu", "#bugun-diger-raporlar" })
            Assert.NotNull(e.Find(id));
        Assert.Equal("Bugün", e.Find("#sayfa-basi-raporlar-bugun-baslik").TextContent.Trim());
        Assert.Empty(e.FindAll("#raporlar-pro"));
        Assert.Empty(e.FindAll("#rapor-merkezi"));
        Assert.Empty(e.FindAll("#stok-yaslandirma-kart"));
        // Özellik silinmedi: profesyonel görünüme giden yol ekranda yazar.
        Assert.Equal("/ayarlar", e.Find("#bugun-gorunum-git").GetAttribute("href"));
        // UX-S25: türetilmiş değer. ZO-BELGE (kurucu 25.09: "değiştirilemez" kutusu açılışta YOK): BASİT "Bugün"
        // yüzünde çizilmez; profesyonel yüzde durur.
        Assert.Empty(e.FindAll("#degismez-ipucu-Rapor"));
    }

    [Fact]
    public void S34_02_Profesyonel_yuz_rapor_merkezini_AYNEN_cizer_bugun_YOK()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-tum-raporlar");

        Assert.NotNull(e.Find("#raporlar-pro"));
        Assert.NotNull(e.Find("#rp-kar-zarar"));
        Assert.NotNull(e.Find("#stok-yaslandirma-kart"));
        Assert.Empty(e.FindAll("#raporlar-bugun"));
        Assert.Empty(e.FindAll("#bugun-satis"));
    }

    // ── YÜZE GÖRE YÜKLEME + UX-S21 ─────────────────────────────────────────

    [Fact]
    public void S34_03_Basit_yuz_YALNIZ_kendi_isteklerini_atar_TARIH_gondermez()
    {
        Basit();
        var yollar = Yollar();

        var gunluk = Assert.Single(yollar, y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));
        Assert.DoesNotContain("tarih=", gunluk, StringComparison.Ordinal);   // UX-S21: gün sunucuda çözülür
        var mut = Assert.Single(yollar, y => y.Contains("/raporlar/pos-fatura-mutabakati", StringComparison.Ordinal));
        Assert.DoesNotContain("tarih=", mut, StringComparison.Ordinal);
        Assert.Contains(yollar, y => y.Contains("/bildirimler?durum=ACIK&limit=5", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/raporlar/katalog", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/raporlar/stok-yaslandirma", StringComparison.Ordinal));
        // Bu yüz POST ETMEZ.
        lock (istekler) Assert.All(istekler, i => Assert.Equal("GET", i.Metot));
    }

    [Fact]
    public void S34_04_Profesyonel_yuz_basit_yuzun_isteklerini_ATMAZ()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-tum-raporlar");
        e.WaitForAssertion(() => Assert.Contains(Yollar(), y => y.Contains("/raporlar/pos-fatura-mutabakati", StringComparison.Ordinal)));

        var yollar = Yollar();
        Assert.Contains(yollar, y => y.Contains("/raporlar/aylik", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/bildirimler", StringComparison.Ordinal));
        Assert.DoesNotContain(yollar, y => y.Contains("/pozisyon", StringComparison.Ordinal));
    }

    [Fact]
    public void S34_05_Mod_calisirken_degisince_eksik_yuz_YUKLENIR()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-tum-raporlar");

        Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit);
        e.WaitForElement("#raporlar-bugun");
        Bekle(e);

        Assert.NotNull(e.Find("#bugun-satis-deger"));
        Assert.Contains(Yollar(), y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));
    }

    /// <summary>ZO-BELGE 29.09 (doğrulayıcı KRİTİK): SERT YÜKLEME (F5 / adres çubuğu). Profil
    /// sayfadan SONRA gelir; AnaYerlesim onu <c>Oturum.KullaniciAta</c> ile yazar ve bu
    /// <c>Mod.Degisti</c> tetiklemez. Sayfa önce PRO (profil yok) yüzüyle açılır, profil
    /// BASİT gelince BASİT yükleyicileri başlamalı, "Yükleniyor" kalkmalı.</summary>
    [Fact]
    public void ZB43_Sert_yukleme_profil_sonradan_BASIT_gelince_bugun_yuklenir_Yukleniyor_kalkar()
    {
        // Profil YOK → etkin mod PRO (ArayuzModuServisi varsayılanı).
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-tum-raporlar");
        Assert.DoesNotContain(Yollar(), y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));

        // AnaYerlesim'in /auth/ben sonrası yaptığı: yalnız KullaniciAta (Mod.Uygula DEĞİL).
        OturumAc(ArayuzModuServisi.Basit);
        e.WaitForElement("#raporlar-bugun");
        Bekle(e);

        var yollar = Yollar();
        Assert.Contains(yollar, y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));
        Assert.Contains(yollar, y => y.Contains("/raporlar/pos-fatura-mutabakati", StringComparison.Ordinal));
        Assert.Contains(yollar, y => y.Contains("/bildirimler?durum=ACIK", StringComparison.Ordinal));
        Assert.DoesNotContain("Yükleniyor", e.Find("#raporlar-bugun").TextContent, StringComparison.Ordinal);
        // Tek sefer: oturum olayı yinelenirse (jeton yenileme) aynı yüz ikinci kez istek atmaz.
        OturumAc(ArayuzModuServisi.Basit);
        Bekle(e);
        Assert.Single(Yollar(), y => y.Contains("/raporlar/gunluk", StringComparison.Ordinal));
    }

    // ── (1) BUGÜN NE SATTIM? ───────────────────────────────────────────────

    [Fact]
    public void S34_06_Satis_karti_TUTAR_ADET_HAS_yazar_ve_kayitlara_baglanir()
    {
        var e = Basit();
        var deger = e.Find("#bugun-satis-deger");
        Assert.Equal(Bicim.TlKurus(57550m), deger.TextContent.Trim());
        Assert.Equal("/raporlar/islem-defteri", deger.GetAttribute("href"));   // UX-S23
        Assert.Equal("3 satış", e.Find("#bugun-satis-adet").TextContent.Trim());
        Assert.Equal("10,4000 has", e.Find("#bugun-satis-has").TextContent.Trim());
    }

    // ── (2) BUGÜN NE ALDIM? — hurda AYRI ─────────────────────────────────────

    [Fact]
    public void S34_07_Alis_karti_hurda_AYRI_satirda_alisa_KARISMAZ()
    {
        var e = Basit();
        var deger = e.Find("#bugun-alis-deger");
        Assert.Equal(Bicim.TlKurus(8000m), deger.TextContent.Trim());          // 8.000 — 11.000 DEĞİL
        Assert.Equal("/raporlar/islem-defteri", deger.GetAttribute("href"));
        Assert.Equal("2 alış", e.Find("#bugun-alis-adet").TextContent.Trim());
        Assert.Equal("2,5000 has", e.Find("#bugun-alis-has").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(3000m), e.Find("#bugun-hurda-tl").TextContent.Trim());
        Assert.Equal("1,2000 has", e.Find("#bugun-hurda-has").TextContent.Trim());
        Assert.Equal("/hurda", e.Find("#bugun-hurda-git").GetAttribute("href"));
    }

    [Fact]
    public void S34_08_Alis_ve_hurda_yoksa_BOS_metni_hurda_satiri_CIZILMEZ()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.OK, """
            {"tarih":"2026-09-23","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
             "netHasGramHareketi":0,"hareketSayisi":0,"kasalar":[]}
            """);
        var e = Basit();
        Assert.Equal("Bugün henüz alış yok.", e.Find("#bugun-alis-yok").TextContent.Trim());
        Assert.Equal("Bugün henüz satış yok.", e.Find("#bugun-satis-yok").TextContent.Trim());
        Assert.Empty(e.FindAll("#bugun-hurda"));
        // Kasa yoksa boş durum — ama sayı "0 ₺" UYDURULMAZ.
        Assert.Contains("Henüz kasa yok.", e.Find("#bugun-kasa").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-kasa-deger"));
    }

    // ── (3) KASADA NE VAR? — UX-S32 ───────────────────────────────────────

    [Fact]
    public void S34_09_Kasa_TL_grubu_toplanir_DOLAR_ayri_satirda_TOPLANMAZ()
    {
        var e = Basit();
        var deger = e.Find("#bugun-kasa-deger");
        Assert.Equal(Bicim.BelgePara(84200m, "TRY"), deger.TextContent.Trim());   // 44.200 + 40.000; 500 $ KATILMAZ
        Assert.Equal("/kasalar", deger.GetAttribute("href"));
        Assert.Contains("TL kasalar", e.Find("#bugun-kasa-grup").TextContent, StringComparison.Ordinal);
        Assert.Equal(2, e.FindAll("#bugun-kasa-liste li").Count);
        var dolar = e.Find("#bugun-kasa-grup-1");
        Assert.Contains("Dolar kasalar", dolar.TextContent, StringComparison.Ordinal);
        Assert.Contains(Bicim.BelgePara(500m, "USD"), dolar.TextContent, StringComparison.Ordinal);
        // UX-S31: ham para kodu ekrana basılmaz.
        Assert.DoesNotContain("TRY", e.Find("#bugun-kasa").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("USD", e.Find("#bugun-kasa").TextContent, StringComparison.Ordinal);
    }

    // ── (5) GÜN SONU — FARK VAR → düzeltmeye bağlantı ─────────────────────

    [Fact]
    public void S34_10_Gun_sonu_FARK_VAR_ise_tutar_ve_DUZELTME_baglantisi_cizilir()
    {
        var e = Basit();
        // PAKET RAPOR-3 (hasım DÜŞÜK): "Fark var" rozeti farkın ne olduğunu söylemiyordu → kalktı; fark ADIYLA satırda.
        Assert.DoesNotContain("Fark var", e.Find("#bugun-fark-var").TextContent, StringComparison.Ordinal);
        Assert.Equal(Bicim.TlKurus(53550m), e.Find("#bugun-fark-tutar").TextContent.Trim());
        // ZO-BELGE 29.09: basit yüzde SADE açıklama sözlükten; sunucunun ham formülü basılmaz (formül katlanırda, ZB34).
        // DEGISIM-GUNSONU: faturasız + bağsız fatura birlikte → farkın adı/açıklaması genel ("Satış ile fatura farkı").
        Assert.Equal(DegisimGunSonuSozlugu.Tr.AciklamaGenel, e.Find("#bugun-fark-aciklama").TextContent.Trim());
        Assert.Equal($"{DegisimGunSonuSozlugu.Tr.FarkGenel}: {Bicim.TlKurus(53550m)}", e.Find("#bugun-fark-satir").TextContent.Trim());
        Assert.DoesNotContain("FARK = kasaya giren para", e.Markup, StringComparison.Ordinal);
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Tr.FaturasizAdet, 2), e.Find("#bugun-faturasiz").TextContent, StringComparison.Ordinal);
        Assert.Equal("/faturalar", e.Find("#bugun-fatura-kes").GetAttribute("href"));
        Assert.Contains("Satışa bağlanmamış fatura: 1", e.Find("#bugun-bagsiz-fatura").TextContent, StringComparison.Ordinal);
        Assert.Equal("/faturalar", e.Find("#bugun-faturalara-bak").GetAttribute("href"));
        Assert.Empty(e.FindAll("#bugun-gun-sonu-tamam"));
        // Anayasa md.1: sunucu uyarısı basit modda da SİLİNMEZ. ZO-BELGE 27.09: yukarıdaki
        // düzeltme satırlarının tekrarı olduğu için KAPALI "Ayrıntı" katlanırında; başlık
        // uyarı SAYISINI yazar (sessiz gizleme yok), açılınca metin aynen görünür.
        // DEGISIM-2 (hakem DÜŞÜK "'Ayrıntı 1' neyi sayıyor"): sayı başlıkta ADIYLA — "Ayrıntı (1 uyarı)", çıplak rozet yok.
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Tr.NotlarUyariSayili, 1), e.Find("#bugun-gun-sonu-notlar").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-gun-sonu-notlar-sayi"));
        Assert.DoesNotContain("2 satışın faturası kesilmedi.", e.Find("#bugun-gun-sonu").TextContent, StringComparison.Ordinal);
        e.Find("#bugun-gun-sonu-notlar-ac").Click();
        Assert.Contains("2 satışın faturası kesilmedi.", e.Find("#bugun-gun-sonu").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("⚠", e.Find("#bugun-gun-sonu").TextContent, StringComparison.Ordinal);   // emoji yok, ikon dili
    }

    [Fact]
    public void S34_11_Gun_sonu_MUTABIK_ise_TAMAM_yazar_duzeltme_baglantisi_YOK()
    {
        ozel["/raporlar/pos-fatura-mutabakati"] = (HttpStatusCode.OK, MutabakatTamam);
        var e = Basit();
        Assert.Contains("Tamam, fark yok.", e.Find("#bugun-gun-sonu-tamam").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-fark-var"));
        Assert.Empty(e.FindAll("#bugun-fatura-kes"));
    }

    [Fact]
    public void S34_12_Fark_var_ama_faturasiz_ve_bagsiz_YOKSA_genel_kontrol_baglantisi_cizilir()
    {
        ozel["/raporlar/pos-fatura-mutabakati"] = (HttpStatusCode.OK,
            MutabakatTamam.Replace("\"fark\":0,\"mutabik\":true", "\"fark\":150.00,\"mutabik\":false", StringComparison.Ordinal));
        var e = Basit();
        Assert.NotNull(e.Find("#bugun-fark-var"));
        Assert.Equal("/faturalar", e.Find("#bugun-faturalari-kontrol").GetAttribute("href"));
        Assert.Empty(e.FindAll("#bugun-faturasiz"));
        // Sunucu FarkTanimi göndermediyse sözlükteki sade açıklama yazar (DEGISIM-GUNSONU: farkın türüne göre — genel).
        Assert.Equal(DegisimGunSonuSozlugu.Tr.AciklamaGenel, e.Find("#bugun-fark-aciklama").TextContent.Trim());
    }

    // ── UX-S28 — hata / 403 "kayıt yok"a çevrilmez ─────────────────────────

    [Fact]
    public void S34_13_Ozet_HATA_ise_bant_durur_SIFIR_uydurulmaz()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.InternalServerError, """{"message":"rapor ucu cevap vermedi"}""");
        var e = Basit();
        Assert.Contains("rapor ucu cevap vermedi", e.Find("#bugun-satis-hata").TextContent, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#bugun-alis-hata"));
        Assert.NotNull(e.Find("#bugun-kasa-hata"));
        Assert.Empty(e.FindAll("#bugun-satis-deger"));
        Assert.Empty(e.FindAll("#bugun-satis-yok"));
        Assert.DoesNotContain("Henüz kasa yok.", e.Find("#bugun-kasa").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S34_14_Ozet_ve_gun_sonu_403_ise_YETKI_notu_hata_bandi_degil()
    {
        ozel["/raporlar/gunluk"] = (HttpStatusCode.Forbidden, """{"hata":"Bu işlem için yetkiniz yok"}""");
        ozel["/raporlar/pos-fatura-mutabakati"] = (HttpStatusCode.Forbidden, """{"hata":"Bu işlem için yetkiniz yok"}""");
        var e = Basit("TEZGAHTAR");
        Assert.Equal("Bu bilgiyi yalnız yönetici görür.", e.Find("#bugun-satis-yetki").TextContent.Trim());
        Assert.NotNull(e.Find("#bugun-kasa-yetki"));
        Assert.NotNull(e.Find("#bugun-gun-sonu-yetki"));
        Assert.Empty(e.FindAll("#bugun-satis-hata"));
        Assert.Empty(e.FindAll("#bugun-gun-sonu-hata"));
        Assert.Empty(e.FindAll("#bugun-satis-deger"));
    }

    [Fact]
    public void S34_15_Gun_sonu_HATA_ise_bant_durur_TAMAM_yazilmaz()
    {
        ozel["/raporlar/pos-fatura-mutabakati"] = (HttpStatusCode.InternalServerError, """{"message":"mutabakat ucu cevap vermedi"}""");
        var e = Basit();
        Assert.Contains("mutabakat ucu cevap vermedi", e.Find("#bugun-gun-sonu-hata").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-gun-sonu-tamam"));
        Assert.Empty(e.FindAll("#bugun-fark-var"));
    }

    // ── (4) BEKLEYEN İŞ — UX-S23 / UX-S29 / UX-S31 ─────────────────────────

    [Fact]
    public void S34_16_Bekleyen_is_ic_rota_baglanti_dis_rota_DUZ_metin_onem_SOZLUKTEN()
    {
        var e = Basit();
        Assert.Equal("/raporlar/vade-takibi", e.Find("#bugun-is-git-b1").GetAttribute("href"));
        Assert.Empty(e.FindAll("#bugun-is-git-b2"));                        // "//…" bağlantı olmaz
        Assert.Contains("Dış bağlantılı iş", e.Find("#bugun-is-b2").TextContent, StringComparison.Ordinal);
        Assert.Equal("Acil", e.Find("#bugun-is-b1 .panel-nokta").GetAttribute("aria-label"));
        Assert.DoesNotContain("KRITIK", e.Find("#bugun-bekleyen").InnerHtml, StringComparison.Ordinal);
        Assert.Equal("/bildirimler", e.Find("#bugun-bekleyen-tumu").GetAttribute("href"));
        Assert.Empty(e.FindAll("#bugun-bekleyen-sinir"));
    }

    [Fact]
    public void S34_17_Bekleyen_is_SINIR_uyarisi_BOS_durum_ve_HATA_ayri()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            BildirimVarsayilan.Replace("\"dahaFazlasiVar\":false", "\"dahaFazlasiVar\":true", StringComparison.Ordinal));
        var e = Basit();
        Assert.Contains("İlk 5 iş gösterildi", e.Find("#bugun-bekleyen-sinir").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void S34_18_Bekleyen_is_bos_ise_BOS_DURUM_hata_ise_BANT()
    {
        ozel["/bildirimler"] = (HttpStatusCode.OK,
            """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""");
        var e = Basit();
        Assert.Contains("Bekleyen iş yok.", e.Find("#bugun-bekleyen").TextContent, StringComparison.Ordinal);

        ozel["/bildirimler"] = (HttpStatusCode.InternalServerError, """{"message":"bildirim ucu cevap vermedi"}""");
        var e2 = Render<Kuyumcu.Web.Pages.Raporlar>();
        e2.WaitForElement("#raporlar-bugun");
        Bekle(e2);
        Assert.Contains("bildirim ucu cevap vermedi", e2.Find("#bugun-bekleyen-hata").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Bekleyen iş yok.", e2.Find("#bugun-bekleyen").TextContent, StringComparison.Ordinal);
    }

    // ── GÖRSEL DİL / DoD ───────────────────────────────────────────────────

    [Fact]
    public void S34_19_Basit_yuzde_BIRINCIL_dugme_yok_her_dugme_gercek_eylem()
    {
        var e = Basit();
        Assert.Empty(e.FindAll("#raporlar-bugun .dugme-birincil, #raporlar-bugun .btn-birincil"));
        // DoD EK (1): tıklanınca hiçbir şey olmayan düğme YOK — bu yüzdeki
        // <button>'lar yalnız B-2 kartınındır ve GET atar; diğer eylemler bağlantıdır.
        // ZO-BELGE: pozisyon kartı artık SADE (T-P1 kısa kart). "Ayrıntı" istek
        // atmaz ama SESSİZ DEĞİLDİR — kırılımı ve değişmez gerekçesini açar (en sonda ölçülür).
        // ZO-BELGE 27.09: katlanır başlığı istek atmaz ama SESSİZ DEĞİLDİR — gövdeyi açar.
        foreach (var k in e.FindAll("#raporlar-bugun button.katlanir-baslik").Select(b => b.Id).ToList())
        {
            e.Find($"#{k}").Click();
            e.WaitForAssertion(() => Assert.Equal("true", e.Find($"#{k}").GetAttribute("aria-expanded")));
        }
        foreach (var d in e.FindAll("#raporlar-bugun button").Where(b => b.Id != "pozisyon-ayrinti" && !b.ClassList.Contains("katlanir-baslik")).ToList())
        {
            var once = Yollar().Count;
            d.Click();
            e.WaitForAssertion(() => Assert.True(Yollar().Count > once, $"düğme istek atmadı: {d.Id}"));
        }
        foreach (var a in e.FindAll("#bugun-sade a.dugme"))
            Assert.StartsWith("/", a.GetAttribute("href"), StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#degismez-ipucu-AltinPozisyonu"));   // açılışta YOK
        e.Find("#pozisyon-ayrinti").Click();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#pozisyon-ayrinti").GetAttribute("aria-expanded")));
        Assert.NotNull(e.Find("#degismez-ipucu-AltinPozisyonu"));
    }

    [Fact]
    public async Task S34_20_Sozluk_TR_ve_EN_eksiksiz_ve_ingilizce_yuz_cizilir()
    {
        foreach (var p in typeof(RaporBugunMetinleri).GetProperties())
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(RaporBugunSozlugu.Tr)), $"TR boş: {p.Name}");
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(RaporBugunSozlugu.En)), $"EN boş: {p.Name}");
        }
        Assert.Same(RaporBugunSozlugu.En, RaporBugunSozlugu.Al("en"));
        Assert.Same(RaporBugunSozlugu.Tr, RaporBugunSozlugu.Al("tr"));

        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Basit();
        Assert.Equal("Today", e.Find("#sayfa-basi-raporlar-bugun-baslik").TextContent.Trim());
        Assert.Equal("What did I buy today?", e.Find("#bugun-alis-baslik").TextContent.Trim());
    }
}
