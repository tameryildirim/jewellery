using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-61 — /gider-pusulalari SATIR EYLEMLERİ (ortak SatirEylemleri bileşeni)
//
// Kabul: satırda "Düzenle" eylemi VARDIR ve gerçek istek atar; satırda görünen
// eylem en çok 3'tür. Mutasyon M61-2 (Düzenle eylemi sayfadan kaldırılır) →
// Satirda_DuzenleEylemiVar KIRMIZI.
// ⚠ Sahte API her uca GERÇEKÇİ yanıt döndürür ('{}' varsayılanı yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GiderPusulaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    private const string PusulaListesi = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Ali Veli","saticiTckn":"111******10","ayarId":"22K","gramAgirlik":10.0,
          "hasGrami":9.16,"brutTutar":10000.00,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":10000.00,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null},
         {"id":"gp2","pusulaNo":"GP-2026-000002","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Ayşe Kaya","saticiTckn":"222******20","ayarId":"14K","gramAgirlik":5.0,
          "hasGrami":2.92,"brutTutar":3000.00,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":3000.00,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":true,"iptalGerekce":"Yanlış giriş","iptalTarihi":"2099-09-02T00:00:00Z"}]
        """;

    private const string SureTakibi = """
        {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
         "gecikmis":[],"yaklasan":[],"uyarilar":[]}
        """;

    private const string Detay = """
        {"id":"gp1","pusulaNo":"GP-2026-000001","saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Ali Veli",
         "saticiTckn":"11111111110","saticiAdres":"Merkez mah. 1","ayarId":"22K","gramAgirlik":10.0,
         "hasGrami":9.16,"malAciklamasi":"22 ayar hurda bilezik","brutTutar":10000.00,
         "stopajOran":0.0,"stopajTutar":0.00,"netOdenen":10000.00,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "subeId":null,"cariId":null,"hareketId":null,"duzeltilenId":null,
         "olusturuldu":"2099-09-01T00:00:00Z","iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}
        """;

    public GiderPusulaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi(List<(string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol));
            var g = yol switch
            {
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => SureTakibi,
                var y when y.Contains("/gider-pusulalari/") => Detay,
                var y when y.EndsWith("/gider-pusulalari") => PusulaListesi,
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> Ciz()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count == 2);
        istekler.Clear();
        return c;
    }

    [Fact]
    public void Satirda_DuzenleEylemiVar()
    {
        var c = Ciz();
        foreach (var id in new[] { "gp1", "gp2" })
        {
            // Düzenle SATIRDA görünür (taşma penceresinde değil), etkin ve adlı.
            var kap = c.Find($"#gp-eylem-{id}");
            var gorunen = kap.QuerySelectorAll("button").ToList();
            Assert.InRange(gorunen.Count, 1, 3);
            var duzenle = kap.QuerySelector($"#gp-duzenle-{id}");
            Assert.NotNull(duzenle);
            Assert.Equal(GiderPusulasiSozlugu.Tr.Duzenle, duzenle!.TextContent.Trim());
            Assert.False(duzenle.HasAttribute("disabled"));
            Assert.False(string.IsNullOrWhiteSpace(duzenle.GetAttribute("aria-label")));
            // Detay ve Yazdır yolları kaybolmadı (yeni id'ler).
            Assert.NotNull(kap.QuerySelector($"#gp-detay-{id}"));
            Assert.NotNull(kap.QuerySelector($"#gp-yazdir-{id}"));
        }
        // Satır içi style yok (P-61 kaldırdı).
        Assert.All(c.FindAll("tbody td:last-child"), td => Assert.False(td.HasAttribute("style")));

        // Düzenle gerçek istek atar: pusula detayı GET edilir, çekmece açılır (DoD EK 1).
        c.Find("#gp-duzenle-gp1").Click();
        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Item1 == "GET" && i.Item2.EndsWith("/gider-pusulalari/gp1")));
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-gider-pusulasi")));
    }
}
