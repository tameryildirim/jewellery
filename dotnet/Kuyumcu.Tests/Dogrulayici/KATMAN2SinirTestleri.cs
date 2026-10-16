using System.Net;
using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — PAKET KATMAN-2 (kenar menüsü katlama yazımı).
// Paket para/gram/has/kur işlemez → finansal sınır testi KAPSAM DIŞI.
// Bu dosya uygulayıcının testlerinde OLMAYAN durumu sınar: bir katlama yazımı
// BİTTİKTEN SONRA gelen ikinci tıklama da sunucuya yazılmalıdır
// (tek yazıcı alanı "katlamaYazici" tamamlanan göreve takılı kalmamalı).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KATMAN2SinirTestleri : BunitContext
{
    private readonly List<string> patchler = [];
    private HttpStatusCode patchKodu = HttpStatusCode.OK;
    private bool gercekAsenkron;   // true = tarayıcıdaki fetch gibi her istek gerçekten asenkron döner

    private sealed class SahteApi(KATMAN2SinirTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (t.gercekAsenkron) await Task.Delay(5, iptal);
            var kod = HttpStatusCode.OK;
            if (istek.Method == HttpMethod.Patch)
            {
                var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
                lock (t.patchler) t.patchler.Add(govde);
                kod = t.patchKodu;
            }
            if (kod != HttpStatusCode.OK)
                return new HttpResponseMessage(kod)
                {
                    Content = new StringContent("""{"message":"kalıcı hata"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    """{"tercihler":{},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public KATMAN2SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<KenarCubugu> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "MUDUR", "s1", null,
                                ArayuzModuServisi.Profesyonel));
        Services.GetRequiredService<MenuTercihServisi>().Bekle = _ => Task.CompletedTask;
        Services.GetRequiredService<ApiIstemci>().Uyu = _ => Task.CompletedTask;
        var e = Render<KenarCubugu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        // Yetki (fire-and-forget) gelmeden rapor/yetkili gruplar çizilmez — asenkron modda yarış.
        e.WaitForAssertion(() => Assert.True(Services.GetRequiredService<YetkiServisi>().Yuklendi));
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kenar-grup-dugme-rapor")));
        return e;
    }

    private int PatchSayisi { get { lock (patchler) return patchler.Count; } }

    private static void Tikla(IRenderedComponent<KenarCubugu> e, string secici)
    {
        for (var deneme = 1; ; deneme++)
        {
            try
            {
                e.InvokeAsync(() => e.Find(secici).Click()).GetAwaiter().GetResult();
                return;
            }
            catch (Bunit.Rendering.UnknownEventHandlerIdException) when (deneme < 5)
            {
                e.Render();
            }
        }
    }

    [Fact]
    public void Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_sunucuya_yazilir()
    {
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));

        Tikla(e, "#kenar-grup-dugme-yonetim");
        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi), TimeSpan.FromSeconds(3));
        string son;
        lock (patchler) son = patchler[^1];
        Assert.Contains("yonetim", son, StringComparison.Ordinal);
    }

    [Fact]
    public void Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri()
    {
        gercekAsenkron = true;
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        Thread.Sleep(200);
        Tikla(e, "#kenar-grup-dugme-yonetim");
        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Kalici_hatadan_ve_bant_kapatildiktan_sonra_yeni_tiklama_yeniden_yazilir()
    {
        patchKodu = HttpStatusCode.InternalServerError;
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#kenar-sira-hatasi")));
        Tikla(e, "#kenar-sira-hatasi-kapat");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kenar-sira-hatasi")));

        patchKodu = HttpStatusCode.OK;
        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi), TimeSpan.FromSeconds(3));
    }
}
