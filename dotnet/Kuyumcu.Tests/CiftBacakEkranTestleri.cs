using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CIFT-BACAK tur 2 (hasım DÜŞÜK) — iadesi yapılmış emanet GİRİŞİNİN
// "Geri al" düğmesi ONAYDAN ÖNCE kilitlenir ve sebebini söyler (DoD EK md.1,
// UX-S30). Kural sunucununkiyle aynı: o madende elde kalan < girişin has'ı.
//
//   c1: ALTIN g1 = 10 has giriş, g2 = 4 has giriş, i1 = 8 has iade → elde 6.
//       g1 (10 > 6) KİLİTLİ + sebep; g2 (4 ≤ 6) AÇIK; iade satırı AÇIK.
//       GÜMÜŞ k1 = 92,5 giriş, gümüş elde 92,5 → AÇIK (madenler karışmaz).
//       PLATİN p1 = 5 giriş, p2 = 5 iade → sunucu bakiyesi 0 olan madeni
//       madenBakiyeleri'ne YAZMAZ; listede yok = elde 0 → p1 KİLİTLİ (tur 3,
//       canlı ölçümde bulunan açık — MCB-14).
// MCB-13 (IadeEdildi kilidi kaldırılırsa) bu testi KIRMIZI yapar.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class CiftBacakEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    private sealed class SahteApi(CiftBacakEkranTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol));

            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.Contains("/emanet/c1") => """
                    {"cariId":"c1","mevcutHasGramBakiye":6,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":6},{"maden":"GUMUS","madenAdi":"Gümüş","gram":92.5}],
                     "hareketler":[
                       {"id":"g1","tur":"EMANET_GIRIS","tarih":"2026-09-20T10:00:00Z","hasGrami":10,
                        "aciklama":"Bilezik","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"g2","tur":"EMANET_GIRIS","tarih":"2026-09-21T10:00:00Z","hasGrami":4,
                        "aciklama":"Yüzük","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"i1","tur":"EMANET_CIKIS","tarih":"2026-09-22T10:00:00Z","hasGrami":-8,
                        "aciklama":"İade","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"k1","tur":"EMANET_GIRIS","tarih":"2026-09-23T10:00:00Z","hasGrami":92.5,
                        "aciklama":"Gümüş tepsi","stornoKaynakId":null,"fisId":null,"maden":"GUMUS"},
                       {"id":"p1","tur":"EMANET_GIRIS","tarih":"2026-09-24T10:00:00Z","hasGrami":5,
                        "aciklama":"Platin yüzük","stornoKaynakId":null,"fisId":null,"maden":"PLATIN"},
                       {"id":"p2","tur":"EMANET_CIKIS","tarih":"2026-09-25T10:00:00Z","hasGrami":-5,
                        "aciklama":"Platin iade","stornoKaynakId":null,"fisId":null,"maden":"PLATIN"}]}
                    """,
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""",
                _ => """{"hata":"sahte API bu ucu tanımıyor"}""",
            };
            var kod = g.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public CiftBacakEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    [Fact]
    public void Iadesi_yapilmis_emanet_girisinin_geri_al_dugmesi_onceden_kilitli_ve_sebebini_soyler()
    {
        var e = Render<Kuyumcu.Web.Pages.Emanet>().AyrintiAc();
        e.WaitForElement("#e-cari");
        e.WaitForAssertion(() => Assert.DoesNotContain(OnayKilitSozlugu.Tr.EmanetKurYok, e.Markup, StringComparison.Ordinal));
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForElement("#e-gecmis");
        e.Find("#e-gecmis-ac").Click();
        e.WaitForElement("#eh-storno-g1");

        var kilit = CiftBacakSozlugu.Tr.EmanetIadeEdildiKilit;
        foreach (var id in new[] { "g1", "p1" })
        {
            var d = e.Find($"#eh-storno-{id}");
            Assert.True(d.HasAttribute("disabled"), id);
            Assert.Equal(kilit, d.GetAttribute("title"));
        }

        // Elde kalan bu girişi karşılıyor → açık; iade satırı ve başka maden etkilenmez.
        foreach (var id in new[] { "g2", "i1", "k1", "p2" })
        {
            var d = e.Find($"#eh-storno-{id}");
            Assert.False(d.HasAttribute("disabled"), id);
            Assert.Null(d.GetAttribute("title"));
        }

        // Kilitli satırın ayrıntısı da sebebi yazar ve geri alma düğmesi sunmaz;
        // hiçbir storno isteği gitmez.
        e.Find("#eh-detay-g1").Click();
        var cekmece = e.WaitForElement("#cekmece-emanet-hareket");
        Assert.Contains(kilit, cekmece.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#eh-detay-geri-al-g1:not([disabled])"));
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/storno", StringComparison.Ordinal));
    }

    [Fact]
    public void Ingilizce_kilit_metni_sozlukten_gelir()
    {
        Assert.Equal("Part of this deposit was returned; undo the return first.",
            CiftBacakSozlugu.Al("en").EmanetIadeEdildiKilit);
        Assert.Equal(CiftBacakSozlugu.Tr.EmanetIadeEdildiKilit, CiftBacakSozlugu.Al("tr").EmanetIadeEdildiKilit);
    }
}
