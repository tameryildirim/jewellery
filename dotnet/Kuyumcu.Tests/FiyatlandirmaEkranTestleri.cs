using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYATLANDIRMA EKRANI — listeler + kampanyalar (paket 17)
// Sınanan kapılar: FEY md.11/3 üç ay sınırı ekranda da kilitlenir · hedefsiz
// kategori/ürün kampanyası kaydedilemez · gövdeler doğru alanlarla gider.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FiyatlandirmaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public FiyatlandirmaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get => """
                    [{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true}]
                    """,
                var v when v.Contains("/fiyat/kampanyalar") && istek.Method == HttpMethod.Get => "[]",
                var v when v.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik","ustId":null}]""",
                // UX-4a: ekran artık ayar/has gram okuyor (gümüş hedef kilidi + ürün paneli).
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16}]""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":40000,"hasDegeri":37556,"marjTl":1877.8,"iscilikTl":566.2,"indirimTutari":0,"satisFiyati":40000,"sabitFiyat":false}""",
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Fiyatlandirma> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement("#fl-tablo tbody tr");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Acilista_listeler_ve_kampanyalar_cekilir_varsayilan_vurgulanir()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement("#fl-tablo tbody tr");
        Assert.Contains(istekler, i => i.Yol.EndsWith("/fiyat/listeler"));
        Assert.Contains(istekler, i => i.Yol.EndsWith("/fiyat/kampanyalar"));
        Assert.Contains("Perakende", e.Find("#fl-tablo tbody tr").TextContent);
    }

    [Fact]
    public void Yeni_liste_POST_govdesi_marj_ve_carpan_tasir()
    {
        var e = Ciz();
        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        e.Find("#fl-ad").Input("Toptan");
        SeciciYardim.Sec(e, "fl-tur", "TOPTAN");
        e.Find("#fl-marj").Input("-1.5");
        e.Find("#fl-carpan").Input("0");
        // S-3c: eksi marj artık RAHAT ARALIK DIŞIDIR — bilinçli onay olmadan
        // Kaydet kilitlidir (kurul tur 3 engel 20). Sözleşme burada da yaşar.
        Assert.True(e.Find("#fl-kaydet").HasAttribute("disabled"));
        e.Find("#fl-risk-onay").Change(true);
        e.Find("#fl-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/fiyat/listeler"));
            Assert.Equal("Toptan", Alan(istek.Govde, "ad"));
            Assert.Equal("TOPTAN", Alan(istek.Govde, "tur"));
            Assert.Equal("-1.5", Alan(istek.Govde, "hasMarjYuzde"));
            Assert.Equal("0", Alan(istek.Govde, "iscilikCarpanYuzde"));
            Assert.Equal("True", Alan(istek.Govde, "riskOnayi"));
        });
    }

    /// <summary>FEY md.11/3: üç aydan uzun kampanya — sunucu 400 verir, ekran daha önce kilitler.</summary>
    [Fact]
    public void Uc_aydan_uzun_kampanyada_Kaydet_KAPALI_ve_uyari_var()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Uzun");
        e.Find("#km-deger").Input("10");
        e.Find("#km-bas").Change("2026-09-01");
        e.Find("#km-bit").Change("2026-12-15");     // 106 gün

        e.WaitForAssertion(() =>
        {
            Assert.True(e.Find("#km-kaydet").HasAttribute("disabled"));
            Assert.NotNull(e.Find("#km-sure-uyari"));
        });

        e.Find("#km-bit").Change("2026-11-30");     // 91 gün — sınır içi
        e.WaitForAssertion(() =>
        {
            Assert.False(e.Find("#km-kaydet").HasAttribute("disabled"));
            Assert.Empty(e.FindAll("#km-sure-uyari"));
        });
    }

    [Fact]
    public void Hedefsiz_kategori_kampanyasi_KAYDEDILEMEZ()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Bilezik günleri");
        e.Find("#km-deger").Input("10");
        SeciciYardim.Sec(e, "km-hedef", "KATEGORI");
        e.WaitForElement("#km-hedefid");

        Assert.True(e.Find("#km-kaydet").HasAttribute("disabled"));
        SeciciYardim.Sec(e, "km-hedefid", "kat1");
        e.WaitForAssertion(() => Assert.False(e.Find("#km-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void Kampanya_POST_govdesi_TUMU_hedefinde_hedefId_gondermez()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Eylül");
        SeciciYardim.Sec(e, "km-tur", "TUTAR_YUZDE");
        e.Find("#km-deger").Input("7.5");
        e.Find("#km-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/fiyat/kampanyalar"));
            Assert.Equal("TUMU", Alan(istek.Govde, "hedefTur"));
            Assert.Null(Alan(istek.Govde, "hedefId"));
            Assert.Equal("TUTAR_YUZDE", Alan(istek.Govde, "indirimTuru"));
            Assert.Equal("7.5", Alan(istek.Govde, "deger"));
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", Alan(istek.Govde, "baslangic")!);
        });
    }

    // ── PAKET Q10-5 (QA turu 10 /fiyatlandirma: "alan hata mesajı alan
    //    düzeltildikten sonra ekranda kalıyor") ────────────────────────────

    [Fact]
    public void Q105_Ad_hatasi_ad_yazilinca_gonderim_beklemeden_duser()
    {
        var e = Ciz();
        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        e.Find("#fl-kaydet").Click();

        Assert.Single(e.FindAll("#alan-hata-fl-ad"));
        Assert.Contains("hatali", e.Find("#fl-ad").ClassName);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        e.Find("#fl-ad").Input("Toptan");

        Assert.Empty(e.FindAll("#alan-hata-fl-ad"));
        Assert.DoesNotContain("hatali", e.Find("#fl-ad").ClassName ?? "");
        Assert.Equal("false", e.Find("#fl-ad").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Q105_Kampanya_hedef_hatasi_hedef_secilince_duser()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Eylül");
        SeciciYardim.Sec(e, "km-hedef", "KATEGORI");
        e.Find("#km-deger").Input("5");
        e.Find("#km-kaydet").Click();
        Assert.Single(e.FindAll("#alan-hata-km-hedefid"));

        SeciciYardim.Sec(e, "km-hedefid", "kat1");

        Assert.Empty(e.FindAll("#alan-hata-km-hedefid"));
    }

    [Fact]
    public void Q105_Liste_turu_secicisi_ham_kod_basmaz_tablo_ile_ayni_ad()
    {
        var e = Ciz();
        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        var metin = e.Find("#fl-tur-sar").TextContent;
        Assert.DoesNotContain("PERAKENDE", metin);
        Assert.DoesNotContain("OZEL", metin);
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Fiyat.TurPerakende, metin);
    }

    [Fact]
    public void Q105_Liste_tablosu_basliklari_tek_satir_sinifi_tasir()
    {
        var e = Ciz();
        Assert.Contains("q10-th-tek", e.Find("#fl-tablo").ClassName);
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — liste marjı / kampanya değeri tuş tuş "1.500" ═══════
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    [Fact]
    public void OK1_K4_liste_marji_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Ciz();
        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        e.Find("#fl-ad").Input("Toptan");
        foreach (var a in OK1TusTus1500) e.Find("#fl-marj").Input(a);
        e.Find("#fl-marj").Blur();
        Assert.Equal("1.500", e.Find("#fl-marj").GetAttribute("value"));
        Assert.True(e.Find("#fl-kaydet").HasAttribute("disabled"));
        Assert.Contains(e.Find("label[for='fl-marj']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim(),
                        e.Find("#fl-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#fl-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void OK1_K4_kampanya_degeri_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Kısa");
        foreach (var a in OK1TusTus1500) e.Find("#km-deger").Input(a);
        e.Find("#km-deger").Blur();
        Assert.Equal("1.500", e.Find("#km-deger").GetAttribute("value"));
        Assert.True(e.Find("#km-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#km-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#km-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}
