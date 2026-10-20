using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// PAKET-E-BELGE (20.10) — /e-belgeler giden kutusu ekranı: sayaçlar, satır eylemleri duruma göre, olay izi katlanır paneli.
// Canlı tarayıcı kanıtı boş kutuyla alındı (demo kiracısında gönderim yok); satır çizimi burada sahte API ile doğrulanır.
public sealed class EBelgelerEkranTestleri : BunitContext
{
    private const string Kutu = """
        {"satirlar":[
          {"gonderim":{"faturaId":"f1","durum":"HATA","senaryo":"EARSIV","belgeNo":null,"sonHata":"Vergi numarası geçersiz","denemeSayisi":2,"gonderildi":null,"sonuclandi":null,"iptalEdilebilir":false},
           "faturaNo":"FTR-2026-000001","aliciUnvan":"Hatice Demirkol","toplam":48940.00,"tarih":"2026-10-20T10:00:00Z"},
          {"gonderim":{"faturaId":"f2","durum":"KABUL","senaryo":"EFATURA","belgeNo":"MHK2026000000014","sonHata":null,"denemeSayisi":1,"gonderildi":"2026-10-20T10:05:00Z","sonuclandi":"2026-10-20T10:06:00Z","iptalEdilebilir":true},
           "faturaNo":"FTR-2026-000002","aliciUnvan":"Has Toptan A.Ş.","toplam":120000.00,"tarih":"2026-10-20T11:00:00Z"}],
         "sayaclar":{"KUYRUKTA":0,"GONDERILDI":0,"KABUL":1,"HATA":1,"RET":0,"IPTAL":0},"toplam":2,"dahaFazlasiVar":false}
        """;
    private const string Olaylar = """
        [{"zaman":"2026-10-20T10:06:00Z","eskiDurum":"GONDERILDI","yeniDurum":"KABUL","kaynak":"SORGU","mesaj":null},
         {"zaman":"2026-10-20T10:05:00Z","eskiDurum":"KUYRUKTA","yeniDurum":"GONDERILDI","kaynak":"ISTEK","mesaj":null},
         {"zaman":"2026-10-20T10:00:00Z","eskiDurum":null,"yeniDurum":"KUYRUKTA","kaynak":"KUYRUK","mesaj":"Fatura kesildi"}]
        """;

    public EBelgelerEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            string? govde = yol.Contains("/ebelge/kutu") ? Kutu : yol.Contains("/olaylar") ? Olaylar : null;
            return Task.FromResult(govde is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"mesaj":"yok"}""", System.Text.Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void Kutu_satirlari_duruma_gore_eylem_gosterir_ve_olay_izi_katlanir_acilir()
    {
        var e = Render<Kuyumcu.Web.Pages.EBelgeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#eb-satir-f1")));

        // Sayaçlar
        Assert.Contains("1", e.Find("#eb-suz-hata strong").TextContent);
        Assert.Contains("1", e.Find("#eb-suz-kabul strong").TextContent);

        // HATA satırı: sebep + yeniden dene; PDF/XML yok
        Assert.Equal("Vergi numarası geçersiz", e.Find("#eb-hata-f1").TextContent.Trim());
        Assert.NotEmpty(e.FindAll("#eb-dene-f1"));
        Assert.Empty(e.FindAll("#eb-pdf-f1, #eb-xml-f1"));

        // KABUL satırı: belge no + PDF/XML; yeniden dene yok
        Assert.Contains("MHK2026000000014", e.Find("#eb-satir-f2").TextContent);
        Assert.NotEmpty(e.FindAll("#eb-pdf-f2"));
        Assert.NotEmpty(e.FindAll("#eb-xml-f2"));
        Assert.Empty(e.FindAll("#eb-dene-f2"));

        // Olay izi kapalı gelir; tıklayınca o satırın izi (3 olay) açılır, tekrar tıklayınca kapanır
        Assert.Empty(e.FindAll("#eb-olaylar-f2"));
        e.Find("#eb-olay-f2").Click();
        e.WaitForAssertion(() => Assert.Equal(3, e.FindAll("#eb-olaylar-f2 li").Count));
        Assert.Contains("Fatura kesildi", e.Find("#eb-olaylar-f2").TextContent);
        e.Find("#eb-olay-f2").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#eb-olaylar-f2")));
    }
}
