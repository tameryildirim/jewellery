using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-11 (18.10) — kör sayım ekranı (Kasalar → Günü Kapat): sunucu Gizli önizleme döndürür →
// ekran rakam ve fark YAZMAZ, kuralı söyler; sayım tutmayınca (400) gerekçe alanı açılır, sayılan kilitlenir;
// gerekçeyle kapanınca bantta fark yazar.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1KorSayimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private int postSayisi;

    public D1KorSayimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(D1KorSayimEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (!get && yol.EndsWith("/gun-kapanisi"))
            {
                t.postSayisi++;
                return t.postSayisi == 1
                    ? ParaDemoSahte.Hata(400, "Sayım defterle tutmadı. Farkın nedenini yazıp yeniden kaydedin; fark kapanıştan sonra görünür.")
                    : ParaDemoSahte.Tamam("""{"kasaId":"k1","fark":-25.5,"kilitli":true}""");
            }
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when get && v.Contains("/gun-kapanisi/onizleme") =>
                    """{"toplamBakiye":0,"kartToplam":0,"havaleToplam":0,"nakitDefter":0,"gizli":true}""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}]""",
                var v when v.Contains("/islemler/personeller") => """{"personeller":[],"oturumId":"u1","secimSerbest":true}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            });
        }
    }

    [Fact]
    public void Kor_sayimda_rakam_ve_fark_yazilmaz_tutmayinca_gerekce_acilir_kapaninca_fark_bantta()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Mehmet", "Müdür", "m@ornek.com", "MUDUR", "s1", ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-kor-not");
        Assert.Equal(D1Sozlugu.Tr.KorSayimNotu, e.Find("#gun-kor-not").TextContent.Trim());
        Assert.Empty(e.FindAll("#gun-nakit-defter"));   // beklenen tutar yazılmaz (0 da yazılmaz)

        e.Find("#gun-sayilan").Input("974,5");
        Assert.Empty(e.FindAll("#gun-fark-satir"));     // fark ekranda hesaplanmaz
        Assert.Empty(e.FindAll("#gun-aciklama"));
        Assert.False(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));

        e.Find("#gun-kapat-onayla").Click();
        e.WaitForElement("#gun-aciklama");               // sunucu "tutmadı" dedi → gerekçe alanı
        Assert.True(e.Find("#gun-sayilan").HasAttribute("disabled"));
        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));   // gerekçe boş → kilitli

        e.Find("#gun-aciklama").Input("Sabah açılışta kasaya 25,50 TL bozuk para konmadı.");
        e.Find("#gun-kapat-onayla").Click();
        e.WaitForAssertion(() => Assert.Equal(2, postSayisi));
        string son; lock (istekler) son = istekler.Last(i => i.Metot == "POST").Govde;
        var g = JsonDocument.Parse(son).RootElement;
        Assert.Equal(974.5m, g.GetProperty("sayilanNakit").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains("25,50", e.Markup));   // kapanıştan sonra fark bantta
    }
}
