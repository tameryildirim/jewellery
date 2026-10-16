using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37b — KARŞILAMA EKRANI ("pencere pencere") + HOŞ GELDİN + HAVA
//
// Kurucu (talimat 20): "ilk sayfa menü olarak pencere pencere olan bir ekran
// karşılamalı, en uygun yerlere hoş geldin kullanıcı ismi olmalı ve havanın
// kaç derece olduğu gibi, kullanıcıyı ekrana kilitleyecek … bir tasarım".
//
// ⚠ "Ekrana kilitleyen" bir HİS değildir; bu dosya onun DÖRT ÖLÇÜLEBİLİR
//   karşılığını sınar:
//    (1) İLK EKRANDA CEVAP — sabahın beş sorusu kutucuk olarak VAR
//    (2) BEKLETME YOK      — bir uç patlasa bile diğer kutucuklar çizilir
//    (3) CANLI             — kur mevcut CanliKurServisi akışından gelir
//    (4) TEK TIK           — en sık üç işlem tek tıkla
//
// ⚠ Sayaç karşılıkları (dotnet/denetim.sh `P-37b` bloğu):
//   JS_SOZDIZIM_HATASI = 0 · HAVA_BEKLETME_KAPISI = 1 ·
//   KARSILAMA_TEK_ISTEK_BEKLEME = 0 · KARSILAMA_HAVA_KONUM_IZNI = 0
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P37bKarsilamaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Bu yol parçasını içeren istek 500 döner (tek uç patlatma senaryosu).</summary>
    private string? patlayanYol;

    /// <summary>
    /// S1-j: GET /api/v1/hava yanıtı. null = 204 (sunucuda hava KAPALI ya da
    /// kaynağa ulaşılamadı — gerçek sunucunun varsayılanı), aksi hâlde 200 + bu gövde.
    /// </summary>
    private string? havaGovdesi;

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<string?> patlayan, Func<string?> hava) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            if (patlayan() is { } p && yol.Contains(p, StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"rapor ucu cevap vermedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // S1-j: hava artık KENDİ sunucumuzdan gelir (CSP connect-src 'self').
            if (yol.EndsWith("/api/v1/hava", StringComparison.Ordinal))
                return hava() is { } h
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(h, System.Text.Encoding.UTF8, "application/json"),
                    }
                    : new HttpResponseMessage(HttpStatusCode.NoContent);

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca GERÇEKÇİ yanıt.
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => """
                    {"satis":{"toplamTl":12500.00},"alis":{"toplamTl":3000.00},
                     "hurda":{"odenenTl":500.00},"netHasGramHareketi":4.25,
                     "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":9000.00,"paraBirimi":"TRY"}]}
                    """,
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """
                    {"gunler":[{"tarih":"2026-09-20","satisTl":1000.00},
                               {"tarih":"2026-09-21","satisTl":12500.00}]}
                    """,
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-09-21","bitis":"2026-09-21","toplamSatir":1,"ofset":0,
                     "limit":5,"dahaFazlasiVar":false,
                     "ozet":{"tlGiris":12500.00,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":1},
                     "storno":{"adet":0,"tutarTl":0},
                     "satirlar":[{"id":"h1","tarih":"2026-09-21T09:15:00Z","tur":"SATIS","yon":"GIRIS",
                                  "tlTutari":12500.00,"hasGrami":4.25,"maden":"ALTIN","paraBirimi":"TRY",
                                  "odemeTuru":"NAKIT","nakitTutar":12500.00,"veresiyeTutar":0,
                                  "fisId":"f1","faturaNo":null,"cariAd":"Ayşe Demir","urunAd":"22 Ayar Çeyrek",
                                  "subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false}]}
                    """,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK","TRUNCGIL"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public P37bKarsilamaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => patlayanYol, () => havaGovdesi))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        // S1-j: hava SUNUCUDAN gelir; sahte sunucunun varsayılanı 204 (KAPALI).
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private void OturumAc(string ad = "Ahmet", string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", ad, "Yılmaz", "a@ornek.com", rol, "s1", null,
                                ArayuzModuServisi.Profesyonel));

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Ekran()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-selam");
        return e;
    }

    // ── HOŞ GELDİN + KULLANICI ADI ──────────────────────────────────────────

    [Fact]
    public void Karsilama_selami_KULLANICI_ADINI_tasir()
    {
        var e = Ekran();
        var selam = e.Find("#panel-selam").TextContent;

        Assert.Contains("Ahmet", selam, StringComparison.Ordinal);
        // Selam saate göre seçilir; hangi saatte koşarsa koşsun dördünden biridir.
        Assert.Contains(new[] { "Günaydın", "İyi günler", "İyi akşamlar", "İyi geceler" },
            s => selam.Contains(s, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#panel-tarih").TextContent));
    }

    // ── (1) İLK EKRANDA CEVAP: SABAHIN BEŞ SORUSU ───────────────────────────

    [Fact]
    public void Karsilama_sabahin_BES_SORUSUNU_kutucuk_olarak_tasir()
    {
        var e = Ekran();

        // bugün ne sattım · altın kaç · kim borçlu/bekleyen iş · kasada ne var · tek tık
        Assert.NotNull(e.Find("#panel-kutucuk-gun-ozeti"));
        Assert.NotNull(e.Find("#panel-kutucuk-kur"));
        Assert.NotNull(e.Find("#panel-kutucuk-bekleyen"));
        Assert.NotNull(e.Find("#panel-kutucuk-kasa"));
        Assert.NotNull(e.Find("#panel-kutucuk-hizli-eylem"));
    }

    // ── (4) TEK TIK ─────────────────────────────────────────────────────────

    [Fact]
    public void Karsilama_en_sik_UC_ISLEMI_tek_tikla_baslatir()
    {
        var e = Ekran();

        Assert.Equal("/islemler", e.Find("#panel-satis-yap").GetAttribute("href"));
        Assert.Equal("/islemler", e.Find("#panel-alis-yap").GetAttribute("href"));
        Assert.Equal("/vezne", e.Find("#panel-tahsilat").GetAttribute("href"));
    }

    // ── (2) BEKLETME YOK — kutucuklar birbirini beklemez ────────────────────

    [Fact]
    public void Karsilama_bir_UC_PATLASA_bile_diger_kutucuklar_cizilir()
    {
        // Günlük özet ucu 500 dönüyor. ESKİ davranış: sayfanın tamamı
        // <Yukleniyor /> kapısında kilitleniyordu. YENİ: yalnız o kutucuk
        // hata bandı gösterir, kur ve hızlı eylemler çizilmeye devam eder.
        patlayanYol = "/raporlar/gunluk";
        var e = Ekran();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.NotNull(e.Find("#panel-kutucuk-kur"));
        Assert.NotNull(e.Find("#panel-satis-yap"));
        Assert.NotNull(e.Find("#panel-kutucuk-kasa"));
    }

    [Fact]
    public void Karsilama_sayfayi_TUMDEN_kilitleyen_yukleme_kapisi_YOK()
    {
        // Mutasyon kapısı: davranış testi tek başına, biri kapıyı geri koyarsa
        // (ve fikstür hızlı cevap verirse) yine yeşil kalabilir. Kaynak denetlenir.
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Panel.razor");

        Assert.DoesNotContain("<Yukleniyor />", kaynak, StringComparison.Ordinal);
        Assert.Contains("Yuklemeler.AddRange", kaynak, StringComparison.Ordinal);
    }

    // ── UX-S28 — hata "kayıt yok"a ÇEVRİLMEZ ────────────────────────────────

    [Fact]
    public void Karsilama_son_islemler_HATASI_bos_duruma_cevrilmez()
    {
        patlayanYol = "/raporlar/islem-defteri";
        var e = Ekran();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-defter-hata")));
        // "Bugün henüz işlem yok." = istek BAŞARILI ve sonuç gerçekten boşken çıkar.
        Assert.DoesNotContain("Bugün henüz işlem yok", e.Markup, StringComparison.Ordinal);
    }

    // ── KUTUCUK DÜZENİ — KULLANICI BAZINDA ──────────────────────────────────

    [Fact]
    public void Kutucuk_gizlenince_KULLANICI_tercihine_PATCH_atilir()
    {
        var e = Ekran();
        e.Find("#panel-duzen-modu").Click();
        e.WaitForElement("#panel-gizle-kur").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.Contains("/tercihler/benim", StringComparison.Ordinal)
                 && i.Govde.Contains("panel-duzeni", StringComparison.Ordinal)
                 && i.Govde.Contains("kur", StringComparison.Ordinal)));
        // Kiracı geneline yazan bir yol YOK (FAVORI_KULLANICI_DISI = 0).
        Assert.DoesNotContain(istekler,
            i => i.Metot == "PATCH" && i.Yol.Contains("/tercihler", StringComparison.Ordinal)
                 && !i.Yol.Contains("/benim", StringComparison.Ordinal));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#panel-kutucuk-kur")));
        Assert.NotNull(e.Find("#panel-goster-kur"));
    }

    [Fact]
    public void Kutucuk_klavyeyle_ALT_OK_ile_tasinir_erisilebilirlik()
    {
        var e = Ekran();
        e.Find("#panel-kutucuk-gun-ozeti").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
        {
            Key = "ArrowDown",
            AltKey = true,
        });

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Govde.Contains("panel-duzeni", StringComparison.Ordinal)));
    }

    [Fact]
    public void Duzen_sifirla_tercihi_SILER_geri_donus_yolu_vardir()
    {
        var e = Ekran();
        e.Find("#panel-duzen-modu").Click();
        e.WaitForElement("#panel-duzen-sifirla").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot is "PATCH" or "DELETE"
                 && i.Yol.Contains("/tercihler/benim", StringComparison.Ordinal)));
    }

    // ── HAVA — DIŞ KAYNAK, SESSİZ DÜŞER, SAYFAYI BEKLETMEZ ──────────────────

    [Fact]
    public void Hava_KAPALIYKEN_kutucuk_cizilmez_ve_HATA_BANDI_atilmaz()
    {
        // Sunucu 204 döner (Hava:Etkin varsayılanı false).
        var e = Ekran();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Yol.EndsWith("/api/v1/hava", StringComparison.Ordinal)));
        Assert.Empty(e.FindAll("#panel-hava"));
        Assert.DoesNotContain("Hava bilgisi alınamadı", e.Markup, StringComparison.Ordinal);
        // S1-j: istemci DIŞ KAYNAĞA HİÇ gitmez (CSP connect-src 'self').
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("open-meteo", StringComparison.Ordinal));
        Assert.All(istekler, i => Assert.StartsWith("/api/v1/", i.Yol, StringComparison.Ordinal));
    }

    [Fact]
    public void Hava_SUNUCUDAN_gelirse_selamda_derece_ve_sehir_gorunur()
    {
        // Tuval Panel.dc.html: "İstanbul 21°" — veri sunucudan, uydurma değil.
        havaGovdesi = """{"derece":21.4,"kod":0,"sehir":"İstanbul","kaynak":"Open-Meteo (CC BY 4.0)"}""";
        var e = Ekran();

        e.WaitForAssertion(() => Assert.Equal("21°C", e.Find("#panel-hava-derece").TextContent.Trim()));
        Assert.Equal("İstanbul", e.Find("#panel-hava-sehir").TextContent.Trim());
        Assert.Equal("Açık", e.Find("#panel-hava-durum").TextContent.Trim());
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("open-meteo", StringComparison.Ordinal));
    }

    [Fact]
    public void Hava_sunucu_HATA_verirse_UYDURMA_derece_yazilmaz_bant_atilmaz()
    {
        patlayanYol = "/hava";
        var e = Ekran();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Yol.EndsWith("/api/v1/hava", StringComparison.Ordinal)));
        Assert.Empty(e.FindAll("#panel-hava"));
        Assert.Empty(e.FindAll("#panel-hava-derece"));
        Assert.DoesNotContain("°C", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hava_sunucu_DERECESIZ_yanit_verirse_0_derece_UYDURULMAZ()
    {
        // "{}" gibi derecesiz bir gövde "0°C" olarak çizilseydi yalan olurdu.
        havaGovdesi = """{"sehir":"İstanbul","kaynak":"Open-Meteo"}""";
        var e = Ekran();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Yol.EndsWith("/api/v1/hava", StringComparison.Ordinal)));
        Assert.Empty(e.FindAll("#panel-hava"));
        Assert.DoesNotContain("°C", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hava_istemcisi_sunucu_cevap_vermezse_SESSIZCE_null_doner()
    {
        // Ağ katmanı istisna fırlatır: GetirAsync fırlatmaz, null döner.
        var http = new HttpClient(new PatlayanAg()) { BaseAddress = new Uri("http://test.local/") };
        var oturum = new OturumServisi(JSInterop.JSRuntime, Services);
        var api = new ApiIstemci(http, oturum,
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>());

        var h = new HavaServisi(api);
        var sonuc = await h.GetirAsync();

        Assert.Null(sonuc);
        Assert.True(h.Denendi);
    }

    private sealed class PatlayanAg : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            throw new HttpRequestException("ağ yok");
    }

    [Fact]
    public void Hava_BEKLETME_KAPISI_kodda_vardir()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Servisler", "HavaServisi.cs");
        var sunucu = CssKaynak.Metin("Kuyumcu.Api", "Modules", "Hava", "HavaModulu.cs");

        // İstemci: kısa iptal + sessiz düşme; KENDİ HttpClient'ı ve dış adresi YOK (S1-j).
        Assert.Contains("CancellationTokenSource(TimeSpan.FromSeconds(ZamanAsimiSaniye))", kaynak, StringComparison.Ordinal);
        Assert.Contains("catch (Exception)", kaynak, StringComparison.Ordinal);
        Assert.DoesNotContain("new HttpClient", kaynak, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", kaynak, StringComparison.Ordinal);
        // Sunucu: zaman aşımı + varsayılan KAPALI.
        Assert.Contains("istemci.Timeout = ZamanAsimi", sunucu, StringComparison.Ordinal);
        Assert.Contains("GetValue(\"Hava:Etkin\", false)", sunucu, StringComparison.Ordinal);
        // Kurucu şartı (b): konum izni İSTENMEZ. (Denetim sayacı
        // KARSILAMA_HAVA_KONUM_IZNI ile aynı, büyük/küçük harfe DUYARLI desen:
        // açıklama satırındaki "Geolocation API'si" bir çağrı değildir.)
        Assert.DoesNotContain("navigator.geolocation", kaynak, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("geolocation", kaynak, StringComparison.Ordinal);
    }

    [Fact]
    public void Hava_ham_WMO_kodu_ekrana_basilmaz_sozlukten_cevrilir()
    {
        // UX-S31: ham kaynak kodu (0, 61, 95…) kullanıcıya gösterilmez.
        Assert.Equal("Açık", Kuyumcu.Core.I18n.HavaSozlugu.Durum(0, "tr"));
        Assert.Equal("Yağmurlu", Kuyumcu.Core.I18n.HavaSozlugu.Durum(61, "tr"));
        Assert.Equal("Gök gürültülü", Kuyumcu.Core.I18n.HavaSozlugu.Durum(95, "tr"));
        Assert.Equal("", Kuyumcu.Core.I18n.HavaSozlugu.Durum(null, "tr"));
        Assert.Equal("", Kuyumcu.Core.I18n.HavaSozlugu.Durum(4242, "tr"));

        // ⚠ İngilizce arayüzde TÜRKÇE metin kalmaz (Sozlukler TR+EN kuralı).
        Assert.Equal("Clear", Kuyumcu.Core.I18n.HavaSozlugu.Durum(0, "en"));
        Assert.Equal("Rainy", Kuyumcu.Core.I18n.HavaSozlugu.Durum(61, "en"));
        Assert.Equal("Thunderstorm", Kuyumcu.Core.I18n.HavaSozlugu.Durum(95, "en"));
    }

    // ── JS SÖZDİZİMİ — canlıda çalışmayan menüyü birim testi GÖREMİYORDU ────

    [Theory]
    [InlineData("siralistesi.js")]
    [InlineData("suruklenebilir.js")]
    [InlineData("cekmece.js")]
    [InlineData("tema.js")]
    [InlineData("dil.js")]
    [InlineData("indir.js")]
    public void Js_dosyasi_ust_duzey_bir_ARTIK_PARCA_ile_baslamaz(string ad)
    {
        // ⚠ GERÇEK OLAY (md.1): siralistesi.js'in başında yarım kalmış
        //   "  baslat: function (kapVeyaId, net) {" satırları duruyordu; dosya
        //   AYRIŞMIYOR, window.kuyumcuSira HİÇ tanımlanmıyor, yani menü
        //   sürükle-bırak ve Ctrl/Cmd+K canlıda ÇALIŞMIYORDU. Bütün C# testleri
        //   yeşildi. Bu test o sınıf hatayı yakalar; tam ayrıştırma denetimi
        //   dotnet/denetim.sh içindeki `node --check` kapısındadır.
        var metin = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", ad);
        var ilk = metin.Split('\n').First(s => !string.IsNullOrWhiteSpace(s)).TrimStart();

        Assert.True(ilk.StartsWith("//", StringComparison.Ordinal)
                    || ilk.StartsWith("/*", StringComparison.Ordinal)
                    || ilk.StartsWith("window.", StringComparison.Ordinal)
                    || ilk.StartsWith("(function", StringComparison.Ordinal)
                    || ilk.StartsWith("const ", StringComparison.Ordinal)
                    || ilk.StartsWith("let ", StringComparison.Ordinal)
                    || ilk.StartsWith("function ", StringComparison.Ordinal),
            $"{ad} üst düzey olmayan bir parçayla başlıyor: «{ilk}»");
        Assert.Equal(metin.Count(c => c == '{'), metin.Count(c => c == '}'));
    }

    [Fact]
    public void Siralistesi_js_hem_id_hem_eleman_kabul_eder()
    {
        // KenarCubugu "kenar-nav" DİZGESİYLE, Panel "panel-izgara" dizgesiyle
        // çağırıyor; baslat() dizgeyi elemana çevirmezse sürükleme sessizce ölür.
        var metin = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "siralistesi.js");

        Assert.Contains("typeof kapVeyaId === \"string\"", metin, StringComparison.Ordinal);
        Assert.Contains("document.getElementById(kapVeyaId)", metin, StringComparison.Ordinal);
    }

    // ── CSP KAPISI — canlı bulgudan doğdu (md.1) ────────────────────────────
    // Hava kutucuğu kodda çalışıyordu ama tarayıcıda `connect-src 'self'`
    // dış isteği kesiyordu: kutucuk HER ZAMAN sessizce düşüyordu. Kapı artık
    // yapılandırmaya bağlı ve FAIL-CLOSED.

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://api.open-meteo.com")]                 // düz http açılmaz
    [InlineData("api.open-meteo.com")]                        // şemasız
    [InlineData("https://api.open-meteo.com/v1/forecast")]    // yol taşıyor
    [InlineData("https://a.test; script-src *")]              // başlık enjeksiyonu
    [InlineData("https://a.test 'unsafe-inline'")]            // boşluk enjeksiyonu
    public void Hava_CSP_eki_supheli_degerde_KAPALI_kalir(string? deger)
    {
        Assert.Equal(string.Empty, Kuyumcu.Api.Common.HavaOrigin.CspEki(deger));
    }

    [Fact]
    public void Hava_CSP_eki_yalniz_ACIKCA_verilen_https_origini_acar()
    {
        Assert.Equal(" https://api.open-meteo.com",
            Kuyumcu.Api.Common.HavaOrigin.CspEki("https://api.open-meteo.com"));
        Assert.Equal(" https://hava.ornek.test:8443",
            Kuyumcu.Api.Common.HavaOrigin.CspEki("https://hava.ornek.test:8443/"));
    }

    [Fact]
    public void Program_cs_connect_src_i_hava_kapisindan_gecirir()
    {
        // Kaynak denetimi: biri kapıyı atlayıp ham yapılandırma değerini
        // başlığa koyarsa (enjeksiyon yolu) bu test kırılır.
        var program = CssKaynak.Metin("Kuyumcu.Api", "Program.cs");

        Assert.Contains("HavaOrigin.CspEki(app.Configuration[\"Hava:KaynakOrigin\"])",
            program, StringComparison.Ordinal);
        Assert.Contains("\"connect-src 'self'\" + havaCspEki", program, StringComparison.Ordinal);
    }

    // ── ÖLÇÜT 1 — İLK EKRANDA CEVAP (canlı ölçümden doğdu) ──────────────────
    [Fact]
    public void Karsilama_genis_ekranda_kutucuk_ici_stat_izgarasi_IKI_SUTUNDUR()
    {
        // 1440×900 canlı ölçümünde hızlı eylemler satırı ilk ekrana SIĞMIYORDU
        // (alt kenar 963 px > 900). Gün özeti kartları kutucuk içinde tek sütuna
        // düşüyordu. Kural CSS'te TEK yerdedir; kalkarsa bu test kırılır.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");

        Assert.Contains("@media (min-width: 1200px)", css, StringComparison.Ordinal);
        Assert.Contains(".karsilama-kutucuk .stat-izgara { grid-template-columns: repeat(2, minmax(0, 1fr)); }",
            css, StringComparison.Ordinal);
    }
}
