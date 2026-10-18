using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-06 (18.10) — müşteri kartında ödeme planı çekmecesi: plan taksitleri durumuyla çizilir, Yazdır bağlantısı
// ödeme planı belgesine gider; "+ Yeni ödeme planı" açık satışı, taksit sayısını, aralığı, senedi POST eder.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1OdemePlaniEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(D1OdemePlaniEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var plan = """{"id":"op1","cariId":"c1","cariAd":"Selin","kaynakHareketId":"h1","kaynakTarih":"2026-10-18T10:00:00Z","toplamTl":30000,"odenenTl":12000,"senetli":false,"durum":"AKTIF","olusturuldu":"2026-10-18T10:00:00Z","taksitler":[{"sira":1,"vade":"2026-11-18","tutarTl":10000,"odenenTl":10000,"durum":"ODENDI","evrakId":null},{"sira":2,"vade":"2026-12-18","tutarTl":10000,"odenenTl":2000,"durum":"KISMI","evrakId":null},{"sira":3,"vade":"2027-01-18","tutarTl":10000,"odenenTl":0,"durum":"BEKLIYOR","evrakId":null}]}""";
            var y = yol switch
            {
                var v when v.Contains("/odeme-planlari/adaylar") => """[{"hareketId":"h2","tarih":"2026-10-18T11:00:00Z","acikTutar":9000,"aciklama":null}]""",
                var v when v.Contains("/odeme-planlari") && istek.Method == HttpMethod.Post => plan,
                var v when v.Contains("/odeme-planlari") => "[" + plan + "]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    public D1OdemePlaniEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    [Fact]
    public void Plan_taksitleri_durumla_cizilir_yeni_plan_POST_eder()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.OdemePlaniCekmecesi>(p => p
            .Add(x => x.Cari, new CariVeri("c1", "Selin", "BIREYSEL", null, null, 39000m, 0m))
            .Add(x => x.Yonetebilir, true));
        e.WaitForElement("#op-plan-op1");
        Assert.Contains("ödendi", e.Find("#op-taksit-op1-1").TextContent);
        Assert.Contains("kısmen", e.Find("#op-taksit-op1-2").TextContent);
        Assert.Contains("18.12.2026", e.Find("#op-taksit-op1-2").TextContent);
        Assert.EndsWith("/cikti/ODEME_PLANI/op1", e.Find("#op-yazdir-op1").GetAttribute("href"));
        Assert.NotNull(e.Find("#op-iptal-op1"));

        e.Find("#op-yeni").Click();
        e.WaitForElement("#op-onizleme");
        Assert.Contains("3 taksit", e.Find("#op-onizleme").TextContent);   // tek aday ön seçili, varsayılan 3 taksit
        e.Find("#op-senetli").Change(true);
        e.Find("#op-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/odeme-planlari")); });
        string g; lock (istekler) g = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/odeme-planlari")).Govde;
        var k = JsonDocument.Parse(g).RootElement;
        Assert.Equal("h2", k.GetProperty("kaynakHareketId").GetString());
        Assert.Equal(3, k.GetProperty("taksitSayisi").GetInt32());
        Assert.Equal(1, k.GetProperty("aralikAy").GetInt32());
        Assert.True(k.GetProperty("senetli").GetBoolean());
    }

    [Fact]
    public void Yetkisiz_kullanici_plani_gorur_ama_acamaz_kaldiramaz()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.OdemePlaniCekmecesi>(p => p
            .Add(x => x.Cari, new CariVeri("c1", "Selin", "BIREYSEL", null, null, 39000m, 0m))
            .Add(x => x.Yonetebilir, false));
        e.WaitForElement("#op-plan-op1");
        Assert.Empty(e.FindAll("#op-yeni"));
        Assert.Empty(e.FindAll("#op-iptal-op1"));
    }
}
