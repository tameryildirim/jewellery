using Bunit;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SATIS-3 t21 — hasım doğrulayıcı KRİTİK: "ÖDEME SATIRI ASLA SESSİZCE DÜŞMEZ".
// Kur yokken döviz satırının TL'si 0 çıkar; otomatik kalan başka satıra geçer ve fark
// 0 görünür. Eskiden FisFark.Gonderilecek bu satırı gövdeden düşürüyordu → müşterinin
// verdiği döviz kayda girmeden fiş tam nakit/kart kaydediliyordu. Şimdi: satır gövdede
// kalır, Kaydet "Döviz kuru alınamadı" sebebiyle KİLİTLİ, POST 0; kur gelince
// ("Kuru yeniden al" → TEK /kur/guncel isteği) TL kurdan türetilir ve gövdeye girer.
// İstemci kur UYDURMAZ. Aynı dosyada: veresiye koşulları gönderilen satırlara bakar
// (boş Veresiye satırı Kaydet'i kilitlemez) ve ödeme türleri tablosu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SATIS3DovizKurTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kurYok = true;

    public SATIS3DovizKurTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(SATIS3DovizKurTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/tevsik/") => """{"had":2000000000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":2000000000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                // GBP kur kaydında YOK (KurModulu GbpTry nullable) — kurYok iken USD de yok.
                var v when v.Contains("/kur/guncel") => t.kurYok
                    ? """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}"""
                    : """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"kusd","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/pos/cihazlar") => """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private static Kuyumcu.Core.I18n.Satis3Metinleri S3 => Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr");
    private int Say(string parca) { lock (istekler) return istekler.Count(i => i.Yol.Contains(parca)); }
    private List<string> FisGovdeleri() { lock (istekler) return [.. istekler.Where(i => i.Yol.EndsWith("/islemler/satis-fisi")).Select(i => i.Govde)]; }
    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.Find("#islem-kaydet");

    /// <summary>Karma kip, 10.000 TL tek kalem; bacak-0 = araç, bacak-1 otomatik kalan (Kart).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ac(string arac0)
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-cari");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.Find("#islem-karma").Change(true);
        e.WaitForElement("#islem-bacak-ekle");
        e.Find("#islem-bacak-ekle").Click();
        e.WaitForElement("#islem-bacak-0-arac");
        SeciciYardim.Sec(e, "islem-bacak-0-arac", arac0);
        return e;
    }

    // ── saf: FisFark ─────────────────────────────────────────────────────────

    [Fact]
    public void T21_Gonderilecek_kur_yokken_DOVIZ_satirini_DUSURMEZ_miktar_0_ise_dusurur()
    {
        var d = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "GBP", DovizTutar = 100m, TutarTl = 0m };
        var bos = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "USD", DovizTutar = 0m, TutarTl = 0m };
        var n = new FisBacagi { Arac = "NAKIT", TutarTl = 50_000m };
        var g = FisFark.Gonderilecek([n, d, bos]);
        Assert.Contains(d, g);
        Assert.DoesNotContain(bos, g);
        Assert.True(FisFark.DovizKuruEksik([n, d]));
        Assert.False(FisFark.DovizKuruEksik([n, bos]));
        Assert.False(FisFark.DovizKuruEksik([n, new FisBacagi { Arac = "DOVIZ", DovizTutar = 100m, TutarTl = 4_000m }]));
    }

    /// <summary>Ödeme türleri tablosu: hiçbir türde kullanıcının GİRDİĞİ satır gövdeden sessizce düşmez; yalnız
    /// tutarı/miktarı 0 olan TL/döviz satırı "boş satır" sayılır. Altın satırı her zaman gider (kendi kilidi var).</summary>
    [Theory]
    [InlineData("NAKIT", "100", "0", true)]
    [InlineData("KART", "100", "0", true)]
    [InlineData("HAVALE", "100", "0", true)]
    [InlineData("VERESIYE", "100", "0", true)]
    [InlineData("DOVIZ", "4000", "100", true)]
    [InlineData("DOVIZ", "0", "100", true)]    // kur yok: TL 0 ama miktar var → GİDER (ve kilit)
    [InlineData("NAKIT", "0", "0", false)]
    [InlineData("KART", "0", "0", false)]
    [InlineData("HAVALE", "0", "0", false)]
    [InlineData("VERESIYE", "0", "0", false)]
    [InlineData("DOVIZ", "0", "0", false)]
    [InlineData("ALTIN", "0", "0", true)]
    public void T21_odeme_turleri_tablosu_girilen_satir_SESSIZCE_DUSMEZ(string arac, string tl, string doviz, bool gider)
    {
        var c = System.Globalization.CultureInfo.InvariantCulture;
        var b = new FisBacagi { Arac = arac, TutarTl = decimal.Parse(tl, c), DovizTutar = decimal.Parse(doviz, c), ParaBirimi = arac == "DOVIZ" ? "USD" : "TRY" };
        Assert.Equal(gider, FisFark.Gonderilecek([b]).Contains(b));
    }

    // ── ekran: kur yokken kilit, kur gelince gönderim ─────────────────────────

    [Fact]
    public void T21_kur_yokken_doviz_girilince_Kaydet_PASIF_sebep_yazili_fark_TAMAM_demez_POST_yok()
    {
        var e = Ac("DOVIZ");
        e.Find("#islem-bacak-ekle").Click();                    // bacak-1: otomatik kalan (tamamını üstlenir)
        e.WaitForElement("#islem-bacak-1-arac");
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");
        e.WaitForAssertion(() => Assert.Equal("0.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-0-kur-yok")), Bekle);
        Assert.Contains(S3.DovizKuruYok, e.Find("#islem-bacak-0-kur-yok").TextContent);
        Assert.Equal(S3.DovizKuruYok, e.Find("#islem-bacak-fark-metin").TextContent.Trim());   // "0 ₺ ✓" değil
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        Assert.Equal(S3.DovizKuruYok, e.Find("#islem-kaydet-kilit").TextContent.Trim());
        Kaydet(e).Click();
        Assert.Empty(FisGovdeleri());
    }

    [Fact]
    public void T21_Kuru_yeniden_al_TEK_istek_kur_gelince_doviz_GOVDEDE_ve_kaydedilir()
    {
        var e = Ac("DOVIZ");
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-0-kur-tekrar")), Bekle);
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        kurYok = false;                                           // kur artık var
        var once = Say("/kur/guncel");
        e.Find("#islem-bacak-0-kur-tekrar").Click();
        e.WaitForAssertion(() => Assert.Equal("10000.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);   // 250 × 40
        Assert.Equal(once + 1, Say("/kur/guncel"));              // düğme TEK istek
        Assert.Empty(e.FindAll("#islem-bacak-0-kur-yok"));
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        e.WaitForAssertion(() =>
        {
            var kok = System.Text.Json.JsonDocument.Parse(Assert.Single(FisGovdeleri())).RootElement;
            var o = kok.GetProperty("odemeler");
            var d = o.EnumerateArray().Single(x => x.GetProperty("arac").GetString() == "DOVIZ");
            Assert.Equal(250m, d.GetProperty("dovizTutar").GetDecimal());
            Assert.Equal(10_000m, d.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("USD", d.GetProperty("paraBirimi").GetString());
        }, Bekle);
    }

    // ── hasım doğrulayıcı DÜŞÜK: veresiye koşulları gönderilen satırlara bakar ──

    [Fact]
    public void T21_bos_ikinci_VERESIYE_satiri_gonderilmez_Kaydet_KILITLEMEZ()
    {
        kurYok = false;
        var e = Ac("VERESIYE");
        SeciciYardim.Sec(e, "islem-bacak-0-tutar", "10000");     // elle: tamamı veresiye
        e.Find("#islem-bacak-ekle").Click();
        e.WaitForElement("#islem-bacak-1-arac");
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "VERESIYE");   // ikinci, BOŞ (0) veresiye satırı
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("[id$='-arac'][id^='islem-bacak-']").Count), Bekle);
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        e.WaitForAssertion(() =>
        {
            var o = System.Text.Json.JsonDocument.Parse(Assert.Single(FisGovdeleri())).RootElement.GetProperty("odemeler");
            Assert.Equal(1, o.GetArrayLength());
            Assert.Equal("VERESIYE", o[0].GetProperty("arac").GetString());
            Assert.Equal(10_000m, o[0].GetProperty("tutarTl").GetDecimal());
        }, Bekle);
    }

    /// <summary>t21 (canlı ölçümde bulundu): kuru olan ama o para biriminde KASASI olmayan döviz satırı (EUR; kasalar TRY+USD)
    /// Kaydet'i kilitler — sebep "Ödemeyi tamamlayın" gibi belirsiz değil, hangi kasanın eksik olduğunu söyler (UX-S30); POST 0.</summary>
    [Fact]
    public void T21_kasasi_olmayan_doviz_satiri_sebebi_EKSIK_KASAYI_soyler()
    {
        kurYok = false;
        var e = Ac("DOVIZ");
        SeciciYardim.Sec(e, "islem-bacak-0-birim", "EUR");
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "100");
        e.WaitForAssertion(() => Assert.Equal("4400.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);   // 100 × 44
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.OdemeKasasiYok, "EUR"), e.Find("#islem-kaydet-kilit").TextContent.Trim()), Bekle);
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        Kaydet(e).Click();
        Assert.Empty(FisGovdeleri());
    }

    /// <summary>t21 — BASİT tuvalin alt "Fark" çubuğu da kur yokken "0 ₺ ✓" demez: tutar "—", sebep metni görünür.</summary>
    [Fact]
    public void T21_BASIT_fark_cubugu_kur_yokken_TAMAM_demez()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-cari");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-odeme-bol");
        e.Find("#islem-odeme-bol").Click();
        e.WaitForElement("#islem-bacak-1-arac");
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "DOVIZ");
        SeciciYardim.Sec(e, "islem-bacak-1-doviz", "100");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-1-kur-yok")), Bekle);
        Assert.Equal("—", e.Find("#islem-fark-tutar").TextContent.Trim());
        Assert.DoesNotContain("fis-fark-tamam", e.Find("#islem-fark").ClassName);
        Assert.Equal(S3.DovizKuruYok, e.Find("#islem-fark-durum").TextContent.Trim());
        Assert.True(Kaydet(e).HasAttribute("disabled"));
    }
}
