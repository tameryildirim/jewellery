using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.NakitSiniri;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 — YASAL-KAPI-2 kapanış devri (tasarım hakemi KRİTİK, 10.10)
//   /cariler Tahsilat çekmecesinde 250.000 ₺ NAKİT: ekran önce kimlik formunu doldurtuyor, Kaydet'te sunucu
//   nakit sınırıyla (NAKIT_SINIRI) reddediyordu; ödeme türü Kart'a çevrilince hata silinmiyordu.
// Kural sunucuyla AYNI: bu işlem > kalan nakit hakkı (sunucunun /tevsik/gunluk yanıtı) → nakit sınırı.
// Sınırlar: 29.999,99 / 30.000 (serbest — "haddi AŞMASI") / 30.000,01; ödeme türü değişimi; yukarı-aşağı.
// Had sayısı sahte sunucudan gelir (istemciye gömülü değildir).
// ⚠ Görünürlük/sığma bUnit ile KANITLANMAZ — gerçek Chrome ölçümü rapordadır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: CariNakitSiniri (sunucu yanıtıyla).</summary>
public sealed class YasalKapi2NakitSiniriSafTestleri
{
    private static TevsikGunlukVeri Gunluk(decimal kalan, bool etkin = true, bool kumulatif = true) =>
        new(null, 30_000m, etkin, kumulatif, 30_000m - kalan, kalan, false, null);

    [Theory]
    [InlineData("29999.99", false)]
    [InlineData("30000", false)]        // tam had serbest (TevsikKurali: toplam <= had)
    [InlineData("30000.01", true)]
    [InlineData("250000", true)]
    public void YK3_S01_sinir_tablosu_kalan_hak_30000(string tutar, bool asildi)
    {
        var ns = new CariNakitSiniri();
        ns.Yukle(Gunluk(30_000m));
        var t = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(asildi, ns.Asildi(true, t, "c1"));
        Assert.False(ns.Asildi(false, t, "c1"));                 // kart/havale: nakit sınırı yok
        Assert.Equal(asildi, ns.Hata(true, t, "c1", "tr", YasalKapi3Sozlugu.Tr.DovizNakitSinirAsildi) is not null);
    }

    [Fact]
    public void YK3_S02_bugunku_nakit_dusulur_kalan_0_nakit_kapali_ve_tek_cumle()
    {
        var ns = new CariNakitSiniri();
        ns.Yukle(Gunluk(12_000m));                               // bugün 18.000 nakit alınmış
        Assert.False(ns.Asildi(true, 12_000m, "c1"));
        Assert.True(ns.Asildi(true, 12_000.01m, "c1"));
        Assert.Contains(Bicim.Para(12_000m, "TRY"), ns.Hata(true, 13_000m, "c1", "tr", "en çok {0}")!, StringComparison.Ordinal);
        Assert.False(ns.NakitKapali);

        ns.Yukle(Gunluk(0m));
        Assert.True(ns.NakitKapali);
        Assert.True(ns.Asildi(true, 0.01m, "c1"));
        Assert.Equal(YasalKapi3Sozlugu.Tr.NakitBugunYok, ns.Hata(true, 100m, "c1", "tr", "en çok {0}"));
    }

    [Fact]
    public void YK3_S03_kural_kapali_ya_da_kumulatif_yoksa_on_kontrol_yok_son_soz_sunucuda()
    {
        var ns = new CariNakitSiniri();
        Assert.False(ns.Asildi(true, 1_000_000m, "c1"));         // yanıt yok
        ns.Yukle(Gunluk(30_000m, etkin: false));
        Assert.Null(ns.Kalan);
        Assert.False(ns.Asildi(true, 1_000_000m, "c1"));
        ns.Yukle(Gunluk(30_000m, kumulatif: false));
        Assert.False(ns.Asildi(true, 1_000_000m, "c1"));
    }

    [Fact]
    public void YK3_S04_sunucu_reddi_izi_tutar_inince_odeme_degisince_musteri_degisince_kalkar_geri_cikinca_gelir()
    {
        var ns = new CariNakitSiniri();
        ns.Reddet(new ApiHata(400, "40.000,00 TL nakit, 30.000,00 TL nakit sınırını aşıyor") { Kod = "NAKIT_SINIRI" }, 40_000m, "c1");
        Assert.True(ns.Asildi(true, 40_000m, "c1"));
        Assert.Contains("nakit sınırını", ns.Hata(true, 40_000m, "c1", "tr", "{0}")!, StringComparison.Ordinal);
        Assert.False(ns.Asildi(true, 39_999.99m, "c1"));          // aşağı → kalkar
        Assert.True(ns.Asildi(true, 45_000m, "c1"));              // yukarı → geri
        Assert.False(ns.Asildi(false, 45_000m, "c1"));            // ödeme türü değişti → kalkar
        Assert.False(ns.Asildi(true, 45_000m, "c2"));             // başka müşteri → kalkar
        ns.Ac();
        Assert.False(ns.Asildi(true, 45_000m, "c1"));             // yeni form → iz yok
        Assert.True(CariNakitSiniri.NakitReddi(new ApiHata(400, "x") { Kod = "NAKIT_SINIRI" }));
        Assert.False(CariNakitSiniri.NakitReddi(new ApiHata(400, "MASAK: …")));
    }

    [Fact]
    public void YK3_S05_sozluk_TR_EN_dolu()
    {
        foreach (var m in new[] { YasalKapi3Sozlugu.Tr, YasalKapi3Sozlugu.En })
            foreach (var p in typeof(YasalKapi3Metinleri).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.Contains("{0}", YasalKapi3Sozlugu.En.DovizNakitSinirAsildi, StringComparison.Ordinal);
    }
}

/// <summary>bUnit: Müşteriler çekmeceleri (sahte sunucu — kural SUNUCUNUN yanıtından).</summary>
public sealed class YasalKapi2NakitSiniriEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal kalanHak = 30_000m;
    private bool kuralEtkin = true;
    private bool sunucuNakitReddi;   // ön kontrolden geçen nakit gövdeyi de reddet (pencere açıkken başka kasada nakit alındı)
    private bool sunucuAlanReddi;    // tutar alanına bağlı başka bir sunucu reddi (alan altında)

    public YasalKapi2NakitSiniriEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(YasalKapi2NakitSiniriEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            string g;
            if (yol.Contains("/masak/esikler"))
                g = """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":0,"ekBelgeEsigi":0,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"],"kimlikIstenenEnDusukTutar":200000}""";
            else if (yol.Contains("/masak/gereksinim"))
            {
                var q = System.Web.HttpUtility.ParseQueryString(istek.RequestUri.Query);
                var tutar = decimal.Parse(q["tutar"]!, System.Globalization.CultureInfo.InvariantCulture);
                var r = YasalKapi2SafTestleri.Gereksinim(tutar, 0m);
                g = JsonSerializer.Serialize(new { kademe = r.Kademe, kimlikGerekli = r.KimlikGerekli, amacGerekli = r.AmacGerekli, beyanFormuGerekli = false, ekBelgeGerekli = false, asgariAciklamaUzunlugu = 20, olcuTutarTl = r.OlcuTutarTl, islemTutarTl = r.IslemTutarTl });
            }
            else if (yol.Contains("/tevsik/gunluk"))
                g = JsonSerializer.Serialize(new { taksit = new { kuralDevrede = false, nakitAlinabilir = (decimal?)null }, had = 30000m, kuralEtkin = t.kuralEtkin, kumulatifHesaplandi = true, bugunkuNakitToplam = 30000m - t.kalanHak, kalanNakitHakki = t.kalanHak, islemTutariKuraliDevrede = false });
            else if (istek.Method == HttpMethod.Post && (yol.Contains("/tahsilat") || yol.Contains("/doviz-tahsilat")))
            {
                var e = JsonDocument.Parse(govde).RootElement;
                var nakit = !e.TryGetProperty("odemeTuru", out var o) || o.GetString() == "NAKIT";
                if (t.sunucuAlanReddi) { kod = 400; g = """{"message":"Tutar kasaya sığmıyor","alanlar":{"tlTutari":["Tutar kasaya sığmıyor"]}}"""; }
                else if (nakit && t.sunucuNakitReddi) { kod = 400; g = """{"message":"40.000,00 TL nakit, 30.000,00 TL nakit sınırını 10.000,00 TL aşıyor; bu ödeme kartla ya da havaleyle yapılmalı.","kod":"NAKIT_SINIRI"}"""; }
                else g = """{"id":"h1"}""";
            }
            else g = yol switch
            {
                var z when z.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"Dolar Kasası","paraBirimi":"USD","aktif":true}]""",
                var z when z.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var z when z.Contains("/fiyat/listeler") => "[]",
                var z when z.Contains("/cari/c1") && istek.Method == HttpMethod.Get =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                var z when z.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0}]""",
                _ => "[]",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Cariler> Ciz(string? mod = null)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Cariler>();
        e.WaitForElement("button");
        if (mod is not null) { Services.GetRequiredService<ArayuzModuServisi>().Uygula(mod, mod); e.Render(); }
        return e;
    }

    private IRenderedComponent<Cariler> Tahsilat(IRenderedComponent<Cariler> e)
    {
        // TEMA TEMİZLİĞİ 17.10: tahsilat Yıldız'da müşteri kartının eylemidir (satıra dokun → kart → Tahsilat).
        e.KartEylemi("c1", "tahsilat");
        e.WaitForElement("#th-tutar");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk")));   // sunucunun nakit hakkı geldi
        e.Render();
        return e;
    }

    /// <summary>Tutar alanının altındaki hata metni (yoksa boş).</summary>
    private static string AlanHatasi(IRenderedComponent<Cariler> e, string id) =>
        e.Find($"#{id}-sar").QuerySelectorAll(".alan-alt *").Select(x => x.TextContent.Trim()).FirstOrDefault(x => x.Length > 0) ?? "";

    private int Post(string yol) => istekler.Count(i => i.Metot == "POST" && i.Yol.Contains(yol));

    [Fact]
    public void YK3_01_nakit_250000_once_nakit_siniri_form_doldurtulmaz_kart_secilince_hata_kalkar_form_acilir_geri_nakit_hata_geri()
    {
        var e = Tahsilat(Ciz());
        e.Find("#th-tutar").Input("250000");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(30_000m, "TRY"), AlanHatasi(e, "th-tutar"), StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#th-masak"));                              // kimlik formu doldurtulmaz
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.DoesNotContain("kimlik", string.Concat(e.FindAll("#th-kaydet-kilit").Select(k => k.TextContent)), StringComparison.OrdinalIgnoreCase);   // form yokken "kimlik doldurun" denmez

        SeciciYardim.Sec(e, "th-odeme", "KART");
        e.WaitForElement("#th-masak");                                      // kart: MASAK formu açılır
        Assert.Equal("", AlanHatasi(e, "th-tutar"));                        // nakit sınırı cümlesi kalktı

        SeciciYardim.Sec(e, "th-odeme", "NAKIT");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#th-masak")));
        Assert.Contains(Bicim.Para(30_000m, "TRY"), AlanHatasi(e, "th-tutar"), StringComparison.Ordinal);

        SeciciYardim.Sec(e, "th-odeme", "HAVALE");
        e.WaitForElement("#th-masak");
        Assert.Equal("", AlanHatasi(e, "th-tutar"));
        e.Find("#th-kaydet").Click();
        Assert.Equal(0, Post("/tahsilat"));                                 // havale + MASAK eksik: POST yok (kimlik kilidi)
    }

    [Theory]
    [InlineData("29999,99", false)]
    [InlineData("30000", false)]
    [InlineData("30000,01", true)]
    public void YK3_02_sinirda_nakit_kaydet_kilidi_ve_POST(string tutar, bool asildi)
    {
        var e = Tahsilat(Ciz());
        e.Find("#th-tutar").Input(tutar);
        e.WaitForAssertion(() => Assert.Equal(asildi, e.Find("#th-kaydet").HasAttribute("disabled")));
        Assert.Equal(asildi, AlanHatasi(e, "th-tutar").Length > 0);
        e.Find("#th-kaydet").Click();
        Assert.Equal(asildi ? 0 : 1, Post("/tahsilat"));
    }

    [Fact]
    public void YK3_03_yukari_asagi_nakit_siniri_her_degisimde_yeniden_degerlendirilir()
    {
        var e = Tahsilat(Ciz());
        foreach (var (t, asildi) in new[] { ("40000", true), ("20000", false), ("30000,01", true), ("30000", false), ("250000", true), ("1000", false) })
        {
            e.Find("#th-tutar").Input(t);
            e.WaitForAssertion(() => Assert.Equal(asildi, AlanHatasi(e, "th-tutar").Length > 0), TimeSpan.FromSeconds(2));
            Assert.Equal(asildi, e.Find("#th-kaydet").HasAttribute("disabled"));
        }
    }

    [Fact]
    public void YK3_04_sunucu_NAKIT_SINIRI_reddi_bantta_degil_alan_altinda_odeme_turu_degisince_kalkar_geri_gelir()
    {
        kalanHak = 1_000_000m;          // ön kontrol geçer; sunucu yine reddeder (bugün başka kasada nakit alındı)
        sunucuNakitReddi = true;
        var e = Tahsilat(Ciz());
        e.Find("#th-tutar").Input("40000");
        e.WaitForAssertion(() => Assert.False(e.Find("#th-kaydet").HasAttribute("disabled")));
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("nakit sınırını", AlanHatasi(e, "th-tutar"), StringComparison.Ordinal));
        Assert.DoesNotContain("nakit sınırını", string.Concat(e.FindAll(".hata-bandi").Select(b => b.TextContent)), StringComparison.Ordinal);
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));

        SeciciYardim.Sec(e, "th-odeme", "KART");
        e.WaitForAssertion(() => Assert.Equal("", AlanHatasi(e, "th-tutar")));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
        SeciciYardim.Sec(e, "th-odeme", "NAKIT");
        e.WaitForAssertion(() => Assert.Contains("nakit sınırını", AlanHatasi(e, "th-tutar"), StringComparison.Ordinal));
        e.Find("#th-tutar").Input("39999,99");
        e.WaitForAssertion(() => Assert.Equal("", AlanHatasi(e, "th-tutar")));
    }

    [Fact]
    public void YK3_05_bugunku_hak_0_ise_Nakit_secilemez_gerekcesi_yazar_varsayilan_Kart()
    {
        kalanHak = 0m;
        var e = Tahsilat(Ciz());
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "th-odeme")));
        var nakit = Assert.Single(SeciciYardim.Secenekler(e, "th-odeme"), o => o.Deger() == "NAKIT");
        Assert.Equal("true", nakit.GetAttribute("aria-disabled"));
        Assert.Contains(YasalKapi3Sozlugu.Tr.NakitBugunPasif, nakit.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void YK3_06_kural_kapaliysa_on_kontrol_yok_nakit_250000_kimlik_formu_acilir()
    {
        kuralEtkin = false;
        var e = Tahsilat(Ciz());
        e.Find("#th-tutar").Input("250000");
        e.WaitForElement("#th-masak");
        Assert.Equal("", AlanHatasi(e, "th-tutar"));
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void YK3_07_PRO_cekmecede_kural_satiri_ve_ic_not_yok_esik_tek_kez_zorunlular_katlanirdan_once(string mod)
    {
        var e = Tahsilat(Ciz(mod));
        SeciciYardim.Sec(e, "th-odeme", "KART");
        e.Find("#th-tutar").Input("250000");
        e.WaitForElement("#th-masak");
        Assert.Empty(e.FindAll("#masak-kural"));
        var metin = e.Find("#th-masak").TextContent;
        Assert.DoesNotContain("teyidi", metin, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(string.Concat(e.Nodes.Select(n => n.TextContent)), @"200\.000\s*₺"));
        var html = e.Find("#th-masak").InnerHtml;
        Assert.True(html.IndexOf("masak-aciklama", StringComparison.Ordinal) < html.IndexOf("masak-diger", StringComparison.Ordinal),
            "zorunlu açıklama, isteğe bağlı 'Diğer bilgiler' katlanırından ÖNCE gelmeli");
        Assert.Empty(e.FindAll("#th-ipucu"));                               // form açıkken açıklama satırı yok
    }

    [Fact]
    public void YK3_08_doviz_tahsilat_TL_karsiligi_kalan_hakki_asarsa_alan_altinda_tek_cumle_form_yok_kilitli()
    {
        var e = Ciz();
        e.KartEylemi("c1", "doviz-tahsilat");   // TEMA TEMİZLİĞİ 17.10: kartın eylemi
        e.WaitForElement("#dt-tutar");
        e.Find("#dt-tutar").Input("10000");          // 10.000 USD × 34 = 340.000 TL > 30.000
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(30_000m, "TRY"), AlanHatasi(e, "dt-tutar"), StringComparison.Ordinal));
        // MASAK gereksinimi (340.000 → kimlik) GELDİKTEN sonra da form çizilmez (önce tutar)
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim") && i.Yol.Contains("340000")), TimeSpan.FromSeconds(3));
        Thread.Sleep(300); e.Render();
        Assert.Empty(e.FindAll("#dt-masak"));
        Assert.True(e.Find("#cekmece-doviz-tahsilat-kaydet").HasAttribute("disabled"));
        e.Find("#dt-tutar").Input("800");            // 27.200 TL
        e.WaitForAssertion(() => Assert.Equal("", AlanHatasi(e, "dt-tutar")));
        Assert.False(e.Find("#cekmece-doviz-tahsilat-kaydet").HasAttribute("disabled"));
        e.Find("#cekmece-doviz-tahsilat-kaydet").Click();
        Assert.Equal(1, Post("/doviz-tahsilat"));
    }

    [Fact]
    public void YK3_09_odeme_turu_degisince_sunucunun_eski_tutar_reddi_alan_altindan_kalkar()
    {
        sunucuAlanReddi = true;
        var e = Tahsilat(Ciz());
        e.Find("#th-tutar").Input("1000");
        e.WaitForAssertion(() => Assert.False(e.Find("#th-kaydet").HasAttribute("disabled")));
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("kasaya sığmıyor", AlanHatasi(e, "th-tutar"), StringComparison.Ordinal));
        SeciciYardim.Sec(e, "th-odeme", "KART");
        e.WaitForAssertion(() => Assert.Equal("", AlanHatasi(e, "th-tutar")));
    }
}
