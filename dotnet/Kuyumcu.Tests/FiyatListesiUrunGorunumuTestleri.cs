using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT LİSTESİ = ÜRÜN GÖRÜNÜMÜ — paket 49 ekran testleri.
//
// Sınananlar:
//   • Liste seçilince TEK önizleme ucundan ürün tablosu çizilir; kaynak rozeti
//     (Liste / Ürüne özel) ve fiyatsız satırda sebep rozeti (Gümüş) görünür.
//   • TOPLU UYGULAMA ONAY KAPISI: düğme önce ?onizleme=true çağırır ve ETKİ
//     ÖZETİ modalını açar; onaysız hiçbir yazma isteği gitmez. Onaydan sonra
//     yazma isteği önizlemeden gelen parmak izini taşır.
//   • Tekil geçersiz kılma POST/PATCH gövdeleri; "kaldır" pasife alır (silmez).
//   • Gümüş satırında marj/işçilik alanları KAPALI ve sebebi yazılı (UX-S30).
//   • Sunucu hatası "kayıt yok"a çevrilmez (UX-S28).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FiyatListesiUrunGorunumuTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public FiyatListesiUrunGorunumuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        /// <summary>Önizleme ucu boş liste dönsün (boş durum senaryosu).</summary>
        public bool BosOnizleme;
        /// <summary>Önizleme ucu 500 dönsün (UX-S28 senaryosu).</summary>
        public bool OnizlemeHatasi;

        private const string Onizleme = """{"toplam":3,"limit":50,"offset":0,"satirlar":[{"urunId":"u1","kod":"K-1","ad":"Burma Bilezik","ayarId":"A22","gram":10,"hasGrami":9.16,"iscilikTl":566.2,"teklifVar":true,"sebep":null,"hasDegeri":37556,"marjTl":1877.8,"iscilikTlUygulanan":566.2,"listeFiyati":40000,"indirimTutari":0,"satisFiyati":40000,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null},{"urunId":"u2","kod":"K-2","ad":"Tek Taş Yüzük","ayarId":"A22","gram":5,"hasGrami":4.58,"iscilikTl":200,"teklifVar":true,"sebep":null,"hasDegeri":18778,"marjTl":187.78,"iscilikTlUygulanan":200,"listeFiyati":19165.78,"indirimTutari":0,"satisFiyati":19165.78,"kampanyaId":null,"kaynak":"URUN_GECERSIZ_KILMA","gecersizKilma":{"sabitFiyat":null,"hasMarjYuzde":1,"iscilikCarpanYuzde":null,"not":"toptancı","aktif":true}},{"urunId":"u3","kod":"K-3","ad":"Gümüş Kolye","ayarId":"G925","gram":12,"hasGrami":0,"iscilikTl":0,"teklifVar":false,"sebep":"GUMUS","hasDegeri":0,"marjTl":0,"iscilikTlUygulanan":0,"listeFiyati":0,"indirimTutari":0,"satisFiyati":0,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null}]}""";
        private const string BosCevap = """{"toplam":0,"limit":50,"offset":0,"satirlar":[]}""";
        private const string Ozet = """{"uygulandi":false,"hedeflenen":1,"uygulanacak":1,"toplamFark":-1200.5,"parmakIzi":"damga123","ornekler":[{"urunId":"u1","kod":"K-1","ad":"Burma Bilezik","eskiFiyat":40000,"yeniFiyat":38799.5,"fark":-1200.5}],"atlandi":[{"urunId":"u3","kod":"K-3","sebep":"GUMUS"}]}""";
        private const string Uygulandi = """{"uygulandi":true,"hedeflenen":1,"uygulanacak":1,"toplamFark":-1200.5,"parmakIzi":"damga123","ornekler":[],"atlandi":[]}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            if (OnizlemeHatasi && yol.Contains("/onizleme"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol switch
            {
                // ⚠ SIRA: alt yollar "/fiyat/listeler" dalından ÖNCE eşleşmeli.
                var v when v.Contains("/onizleme") => BosOnizleme ? BosCevap : Onizleme,
                var v when v.Contains("/toplu-uygula") => v.Contains("onizleme=true") ? Ozet : Uygulandi,
                var v when v.Contains("/urun-fiyatlari") => """{"id":"gk1","aktif":true}""",
                var v when v.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/kampanyalar") => "[]",
                var v when v.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik","ustId":null}]""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16}]""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null ? v.ToString() : null;
    }

    /// <summary>Paneli açar ve istek kaydını temizler.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Fiyatlandirma> Panel()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        e.Find("#fl-goster-fl1").Click();
        e.WaitForElement("#flp-tablo");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Liste_secilince_urun_tablosu_ve_KAYNAK_rozetleri_cizilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        e.Find("#fl-goster-fl1").Click();
        e.WaitForElement("#flp-tablo");

        Assert.Contains("/fiyat/listeler/fl1/onizleme", Assert.Single(istekler, i => i.Yol.Contains("/onizleme")).Yol);
        Assert.Equal(3, e.FindAll("#flp-tablo tbody tr").Count);
        // KESIF-P1 (#10): tutar tr-TR + ₺ ("40,000" en-US biçimiydi).
        Assert.Contains("40.000\u00a0₺", e.Find("#flp-satir-u1").TextContent);
        Assert.DoesNotContain("40,000", e.Find("#flp-satir-u1").TextContent);
        Assert.Contains("Liste", e.Find("#flp-kaynak-u1").TextContent);
        Assert.Contains("Ürüne özel", e.Find("#flp-kaynak-u2").TextContent);
        Assert.Contains("Gümüş", e.Find("#flp-rozet-u3").TextContent);   // fiyatsız satırın SEBEBİ
    }

    /// <summary>
    /// M49-EK (onay kapısı): "toplu uygula" ÖNCE etki özetini gösterir; onaysız
    /// yazma isteği ATILMAZ. Mutasyon "koşulu kaldır" biçiminde kurulur —
    /// düğme doğrudan yazma çağırırsa bu test kırmızıya döner.
    /// </summary>
    [Fact]
    public void Toplu_uygulama_ONCE_etki_ozeti_gosterir_onaysiz_YAZMA_YOK()
    {
        var e = Panel();
        e.Find("#flp-toplu-marj").Input("1");
        e.Find("#flp-sec-u1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#flp-toplu").HasAttribute("disabled")));

        e.Find("#flp-toplu").Click();
        e.WaitForElement("#flp-toplu-ozet");

        var cagrilar = istekler.Where(i => i.Yol.Contains("/toplu-uygula")).ToList();
        var kuru = Assert.Single(cagrilar);
        Assert.Contains("onizleme=true", kuru.Yol);                      // YAZMAYAN çağrı
        Assert.Contains("Etkilenen ürün", e.Find("#flp-ozet-sayi").TextContent);
        Assert.Contains("K-1", e.Find("#flp-ozet-satir-u1").TextContent);
        Assert.Contains("Gümüş", e.Find("#flp-ozet-atlanan").TextContent);

        e.Find("#flp-toplu-onayla").Click();
        e.WaitForAssertion(() =>
        {
            var yazma = Assert.Single(istekler, i => i.Yol.Contains("/toplu-uygula") && !i.Yol.Contains("onizleme=true"));
            Assert.Equal("damga123", Alan(yazma.Govde, "parmakIzi"));    // onaylanan özetin damgası
            Assert.Equal("1", Alan(yazma.Govde, "hasMarjYuzde"));
        });
    }

    [Fact]
    public void Urune_ozel_form_MEVCUT_degerlerle_dolar_PATCH_e_gider()
    {
        var e = Panel();
        e.Find("#flp-ozel-u2").Click();
        e.WaitForElement("#flp-ozel-form");

        Assert.Equal("1", e.Find("#flp-ozel-marj").GetAttribute("value"));
        e.Find("#flp-ozel-marj").Input("2.5");
        e.Find("#flp-ozel-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/urun-fiyatlari/u2"));
            Assert.Equal("2.5", Alan(istek.Govde, "hasMarjYuzde"));
        });
    }

    /// <summary>Geçersiz kılma SİLİNMEZ, pasife alınır (denetim izi korunur).</summary>
    [Fact]
    public void Gecersiz_kilmayi_kaldir_PASIFE_ALIR_silmez()
    {
        var e = Panel();
        e.Find("#flp-ozel-u2").Click();
        e.WaitForElement("#flp-ozel-form");
        e.Find("#flp-ozel-kaldir").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.Contains("/urun-fiyatlari/u2"));
            Assert.Equal("PATCH", istek.Metot);                          // DELETE değil
            Assert.Equal("False", Alan(istek.Govde, "aktif"));
        });
    }

    /// <summary>Q3 mührü ekranda: gümüşte yalnız sabit fiyat; kilidin sebebi yazılı (UX-S30).</summary>
    [Fact]
    public void Gumus_urunde_marj_alanlari_KAPALI_ve_sebebi_yazili()
    {
        var e = Panel();
        e.Find("#flp-ozel-u3").Click();
        e.WaitForElement("#flp-ozel-form");

        Assert.True(e.Find("#flp-ozel-marj").HasAttribute("disabled"));
        Assert.True(e.Find("#flp-ozel-carpan").HasAttribute("disabled"));
        Assert.False(e.Find("#flp-ozel-sabit").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#flp-ozel-gumus"));

        e.Find("#flp-ozel-sabit").Input("1200");
        e.Find("#flp-ozel-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/urun-fiyatlari"));
            Assert.Equal("1200", Alan(istek.Govde, "sabitFiyat"));
            Assert.Null(Alan(istek.Govde, "hasMarjYuzde"));              // gümüşe marj GÖNDERİLMEZ
        });
    }

    [Fact]
    public void Bos_onizlemede_BOS_DURUM_cikar()
    {
        sahte.BosOnizleme = true;
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        e.Find("#fl-goster-fl1").Click();
        e.WaitForElement("#flp-bos");
        Assert.Empty(e.FindAll("#flp-tablo"));
    }

    /// <summary>UX-S28 / Anayasa md.1: sunucu hatası "kayıt yok"a çevrilmez.</summary>
    [Fact]
    public void Onizleme_hatasinda_HATA_bandi_cikar_BOS_DURUM_cikmaz()
    {
        sahte.OnizlemeHatasi = true;
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        e.Find("#fl-goster-fl1").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Contains("Sunucu hatası", e.Find("#flp-panel").TextContent);
            Assert.Empty(e.FindAll("#flp-bos"));
            Assert.Empty(e.FindAll("#flp-tablo"));
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MAKUL ARALIK KAPISI — EKRAN (S-3c 2. dilim · kurul tur 3 engel 20)
    // Sunucu riskli değeri onaysız reddediyor; ekran bunu ÖNCEDEN söylemezse
    // kullanıcı sebebini bilmeden 400 yer (UX-S30 kırılır).
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>M20-U1 — toplu uygulamada riskli değer: uyarı çıkar, ONAYLA kilitli.</summary>
    [Fact]
    public void TOPLU_riskli_deger_uyarir_ve_onay_kutusu_isaretlenmeden_YAZMA_YOK()
    {
        var e = Panel();
        e.Find("#flp-toplu-marj").Input("-5");
        e.Find("#flp-sec-u1").Change(true);

        Assert.Contains("maliyet", e.Find("#flp-toplu-risk-uyari").TextContent);
        e.Find("#flp-toplu").Click();
        e.WaitForElement("#flp-toplu-ozet");
        Assert.True(e.Find("#flp-toplu-onayla").HasAttribute("disabled"));   // sebebi: risk onayı yok
        Assert.NotNull(e.Find("#flp-ozet-risk-not"));
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/toplu-uygula") && !i.Yol.Contains("onizleme=true"));

        e.Find("#flp-toplu-risk-onay").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#flp-toplu-onayla").HasAttribute("disabled")));
        e.Find("#flp-toplu-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var yazma = Assert.Single(istekler, i => i.Yol.Contains("/toplu-uygula") && !i.Yol.Contains("onizleme=true"));
            Assert.Equal("True", Alan(yazma.Govde, "riskOnayi"));
        });
    }

    /// <summary>M20-U2 — değer DEĞİŞİNCE onay düşer: onaylanan artık ekrandaki değer değildir.</summary>
    [Fact]
    public void TOPLU_risk_onayi_deger_degisince_DUSER()
    {
        var e = Panel();
        e.Find("#flp-toplu-marj").Input("-5");
        e.Find("#flp-toplu-risk-onay").Change(true);
        e.WaitForAssertion(() => Assert.Throws<Bunit.ElementNotFoundException>(() => e.Find("#flp-toplu-risk-not")));

        e.Find("#flp-toplu-marj").Input("-20");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#flp-toplu-risk-not")));
    }

    /// <summary>M20-U3 — ürüne özel fiyatta riskli değer: KAYDET kilitli, onayla açılır ve gövdeye girer.</summary>
    [Fact]
    public void URUN_OZEL_riskli_deger_KAYDET_kilitler_onayla_gonderilir()
    {
        var e = Panel();
        e.Find("#flp-ozel-u2").Click();
        e.WaitForElement("#flp-ozel-form");

        e.Find("#flp-ozel-marj").Input("-5");
        Assert.NotNull(e.Find("#flp-ozel-risk-uyari"));
        Assert.True(e.Find("#flp-ozel-kaydet").HasAttribute("disabled"));

        e.Find("#flp-ozel-risk-onay").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#flp-ozel-kaydet").HasAttribute("disabled")));
        e.Find("#flp-ozel-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.Contains("/urun-fiyatlari/u2"));
            Assert.Equal("-5", Alan(istek.Govde, "hasMarjYuzde"));
            Assert.Equal("True", Alan(istek.Govde, "riskOnayi"));
        });
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — ürün paneli: tuş tuş "1.500" ═══════════════════════
    // Toplu: marj "1.500" iken çarpan geçerliyse önizleme/uygulama marjı SESSİZCE düşürüp çarpanla gidiyordu.
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    [Fact]
    public void OK1_K4_toplu_marj_belirsiz_1_500_onizleme_YOK_sebep_yazar()
    {
        var e = Panel();
        e.Find("#flp-toplu-carpan").Input("10");
        foreach (var a in OK1TusTus1500) e.Find("#flp-toplu-marj").Input(a);
        e.Find("#flp-sec-u1").Change(true);
        e.WaitForAssertion(() => Assert.True(e.Find("#flp-toplu").HasAttribute("disabled")));
        Assert.Equal("1.500", e.Find("#flp-toplu-marj").GetAttribute("value"));
        Assert.NotEmpty(e.Find("#flp-toplu-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#flp-toplu").Click();
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/toplu-uygula"));
    }

    [Fact]
    public void OK1_K4_ozel_sabit_fiyat_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Panel();
        e.Find("#flp-ozel-u3").Click();
        e.WaitForElement("#flp-ozel-form");
        foreach (var a in OK1TusTus1500) e.Find("#flp-ozel-sabit").Input(a);
        e.Find("#flp-ozel-sabit").Blur();
        Assert.Equal("1.500", e.Find("#flp-ozel-sabit").GetAttribute("value"));
        Assert.True(e.Find("#flp-ozel-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#flp-ozel-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#flp-ozel-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}
