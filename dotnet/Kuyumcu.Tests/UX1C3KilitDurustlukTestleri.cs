using System.Net;
using Bunit;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// UX-1-C3 DOĞRULAMA DÜZELTMESİ (2. tur) — "STORNO_YOK damgalı bir varlığın
// sayfasında RENDER EDİLEN HİÇBİR METİN olmayan bir düzeltme yolu vaat etmez".
//
// NEDEN: 1. tur düzeltmesi yalnız sözlük anahtarlarını (StornoYokGerekce,
// PusulaDuzeltmeYolu) bağladı; çekmecenin KİLİT SEBEBİ (Cekmece.KilitSebebi)
// denetimsiz kaldı ve ekranda hâlâ "Düzeltme yalnız ters kayıtla yapılır."
// (Duzeltme.DefterGerekce) yazıyordu — oysa HURDA_ALIS storno edilemiyor
// (StornoServisi.ErtelenenTurler, sunucu 409). Bu test METNİ DEĞİL, EKRANA
// ÇİZİLEN DOM'u tarar: #cekmece-gider-pusulasi altındaki tüm görünür metin.
//
// Mevcut hiçbir testi değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class UX1C3KilitDurustlukTestleri : BunitContext
{
    /// <summary>Kullanıcıya "şu yoldan düzeltebilirsin" diyen kalıplar.</summary>
    internal static readonly string[] VaatKaliplari =
    [
        "ters kayıtla düzeltilir", "ters kayıtla düzeltin", "ters kayıt girilir",
        "ters kayıtla yapılır", "storno edilir", "storno edin", "storno yapılır",
        "iade fişi", "düzeltme fişi", "yeni pusula düzenlenir",
        "İşlemler ekranından",
    ];

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public UX1C3KilitDurustlukTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
    }

    private static void VaatEtmez(string metin, string neden)
    {
        Assert.False(string.IsNullOrWhiteSpace(metin), $"{neden}: metin boş.");
        foreach (var v in VaatKaliplari)
            Assert.False(metin.Contains(v, StringComparison.OrdinalIgnoreCase),
                $"{neden}: ekranda OLMAYAN bir düzeltme yolu vaat ediliyor → \"{v}\"\n{metin}");
    }

    /// <summary>
    /// Ön koşul + ekran: pusula STORNO_YOK damgalıdır ve çekmecede çizilen
    /// HİÇBİR metin (kilit sebebi dâhil) ters kayıt/storno vaat etmez.
    /// </summary>
    [Fact]
    public void C3_D1_Pusula_cekmecesinde_HICBIR_metin_olmayan_yolu_vaat_etmez()
    {
        // ⚠ PAKET S-3b: ön koşul DEĞİŞTİ. Hurda alışının STOK/MALİYET bacağı hâlâ
        // geri alınamıyor (ErtelenenTurler), ama BELGENİN kendisi artık gerekçeli
        // olarak iptal edilebiliyor — ekran bu yolu anlatabilir, "ters kayıt /
        // storno" vaadi ise hâlâ YASAK (o gerçekten yok).
        Assert.Contains("HURDA_ALIS", StornoServisi.ErtelenenTurler);
        Assert.Equal(GerekceAnahtarlari.Pusula,
            DuzeltmePolitikasi.Varliklar["GiderPusulasi"].GerekceAnahtari);

        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Detay")).Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-GiderPusulasi")));

        // ⚠ PAKET S-3d: aktif pusulada KİLİT METNİ YOKTUR (mali kimlik dışı
        //   alanlar artık yerinde düzeltiliyor), bu yüzden denetim çekmecenin
        //   TAMAMI üzerinden yürür — DegismezIpucu dâhil.
        VaatEtmez(c.Find("#degismez-ipucu-GiderPusulasi").TextContent, "#degismez-ipucu-GiderPusulasi");
        VaatEtmez(c.Find("#cekmece-gider-pusulasi").TextContent, "#cekmece-gider-pusulasi");
    }

    /// <summary>
    /// ⚠ PAKET S-3b: kilit sebebi artık "hiç yapılamıyor" DEMEZ — yapılamayan şey
    /// YERİNDE DÜZENLEMEdir ve metin bunu söyler; yapılabilen yol (gerekçeli
    /// iptal) ekranda GERÇEK BİR DÜĞME olarak durur (UX-S25 + UX-S30).
    /// </summary>
    [Fact]
    public void C3_D2_Pusula_kilit_sebebi_YERINDE_duzenlemenin_olmadigini_soyler()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Detay")).Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-GiderPusulasi")));

        // ⚠ PAKET S-3d: "yerinde değiştirilmez" iddiası artık MALİ ALANLAR için
        //   geçerlidir ve DegismezIpucu'nda yazar; adres/mal açıklaması ise
        //   yerinde düzeltilir (kurucu şartı). İki iddia birlikte doğru olmalı.
        var ipucu = c.Find("#degismez-ipucu-GiderPusulasi").TextContent;
        Assert.Contains("yerinde değiştirilmez", ipucu);
        Assert.Contains("VUK md.234", ipucu);

        // Düzenlenebilir alanlar GERÇEKTEN vardır ve Kaydet gerçek istek atar.
        Assert.NotEmpty(c.FindAll("#cekmece-gider-pusulasi-kaydet"));
        Assert.NotNull(c.Find("#gp-duz-adres"));
        // Mali alanların düzeltme yolu (gerekçeli iptal) da yerinde durur.
        Assert.NotEmpty(c.FindAll("[id^=gp-iptal-]"));
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            var g = yol switch
            {
                var y when y.Contains("/gider-pusulalari/sure-takibi") => """{"gecikmisSayisi":0,"yaklasanSayisi":0,"sure":{"gun":7,"dayanak":"VUK md.234"},"uyarilar":[]}""",
                var y when y.EndsWith("/gider-pusulalari") => """[{"id":"gp1","pusulaNo":"GP-2026-000001","saticiAdSoyad":"Ayşe Yılmaz","saticiTckn":"100*****146","ayarId":"22K","gramAgirlik":54.6,"brutTutar":74196,"stopajOran":0,"stopajTutar":0,"netOdenen":74196,"teslimTarihi":"2026-09-16T10:00:00Z","duzenlemeSonTarihi":"2026-09-23T10:00:00Z","gecikti":false,"kalanGun":7}]""",
                var y when y.Contains("/gider-pusulalari/") => """{"id":"gp1","pusulaNo":"GP-2026-000001","saticiAdSoyad":"Ayşe Yılmaz","saticiTckn":"10000000146","saticiAdres":"Kadıköy","saticiTipi":"NIHAI_TUKETICI","ayarId":"22K","gramAgirlik":54.6,"hasGrami":50,"brutTutar":74196,"stopajOran":0,"stopajTutar":0,"netOdenen":74196,"malAciklamasi":null,"teslimTarihi":"2026-09-16T10:00:00Z","duzenlemeSonTarihi":"2026-09-23T10:00:00Z","gecikti":false}""",
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
