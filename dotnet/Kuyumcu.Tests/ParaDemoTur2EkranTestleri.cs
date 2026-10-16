using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO tur 2 — /gider virman formu: kasadan aktarılan paranın aracı [bUnit]
//
// Kaynak kasada kart/havale payı VARSA "Ne aktarılıyor?" seçicisi açılır (yoksa gizli —
// zorunlu önce); seçilen araç gövdeye kasaAraci olarak gider; payı aşan tutar Kaydet'i
// sebebiyle kilitler (UX-S30); kasanın ayrımı okunamazsa hata bandı çıkar (UX-S28).
// Sahte API önizleme ucuna GERÇEKÇİ yanıt verir ("{}" varsayılanı değil).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ParaDemoTur2EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string onizleme = """{"toplamBakiye":251800,"kartToplam":500,"havaleToplam":300,"nakitDefter":251000}""";
    private bool onizlemeHata;

    public ParaDemoTur2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(ParaDemoTur2EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((metot, yol, govde));
            if (yol.EndsWith("/gun-kapanisi/onizleme") && t.onizlemeHata)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası: önizleme okunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            var g = yol switch
            {
                var y when y.EndsWith("/gun-kapanisi/onizleme") => t.onizleme,
                var y when y.EndsWith("/virman") && metot == "POST" =>
                    """{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakId":"ks1","kaynakAdi":"Merkez Kasa","hedefTur":"BANKA","hedefId":"bh1","hedefAdi":"Ziraat","tutar":500,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-10-01T06:00:00Z","subelerArasi":false}""",
                var y when y.EndsWith("/virman") =>
                    """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":251800,"aktif":true}]""",
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                var y when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[],"toplam":0}""",
                var y when y.EndsWith("/giderler") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> VirmanEkrani()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.Find("#gd-sekme-virman").Click();
        return e;
    }

    [Fact]
    public void PDE6_kasada_kart_payi_varsa_arac_secicisi_gorunur_KART_govdeye_gider()
    {
        var e = VirmanEkrani();
        Assert.Empty(e.FindAll("#gd-v-arac"));   // kaynak seçilmeden yok
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#gd-v-arac")));
        var secenekler = SeciciYardim.Secenekler(e, "gd-v-arac").Select(x => x.TextContent).ToList();
        Assert.Contains(secenekler, s => s.Contains("Kartla alınan para") && s.Contains("500"));
        Assert.Contains(secenekler, s => s.Contains("Havaleyle alınan para") && s.Contains("300"));

        SeciciYardim.Sec(e, "gd-v-arac", "KART");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("500");
        e.Find("#gd-v-kaydet").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/virman"));
        Assert.Contains("\"kasaAraci\":\"KART\"", post.Govde);
        Assert.Contains(istekler, i => i.Yol.EndsWith("/kasalar/ks1/gun-kapanisi/onizleme"));
    }

    [Fact]
    public void PDE6_kart_payini_ASAN_tutarda_Kaydet_KILITLI_sebep_yazar_POST_yok()
    {
        var e = VirmanEkrani();
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#gd-v-arac")));
        SeciciYardim.Sec(e, "gd-v-arac", "KART");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("600");
        Assert.True(e.Find("#gd-v-kaydet").HasAttribute("disabled"));
        Assert.Contains("yalnız", e.Find("#gd-v-kaydet-kilit").TextContent);
        Assert.Contains("500", e.Find("#gd-v-kaydet-kilit").TextContent);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/virman"));
    }

    [Fact]
    public void PDE6_kart_havale_payi_yoksa_secici_GIZLI_govde_nakit()
    {
        onizleme = """{"toplamBakiye":251000,"kartToplam":0,"havaleToplam":0,"nakitDefter":251000}""";
        var e = VirmanEkrani();
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/gun-kapanisi/onizleme")));
        Assert.Empty(e.FindAll("#gd-v-arac"));
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("100");
        e.Find("#gd-v-kaydet").Click();
        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/virman"));
        Assert.Contains("\"kasaAraci\":null", post.Govde);
    }

    [Fact]
    public void PDE6_bankadan_virmanda_onizleme_istenmez_secici_yok()
    {
        var e = VirmanEkrani();
        SeciciYardim.Sec(e, "gd-v-kaynak", "BANKA:bh1");
        Assert.Empty(e.FindAll("#gd-v-arac"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/gun-kapanisi/onizleme"));
    }

    [Fact]
    public void PDE6_ayrim_okunamazsa_HATA_bandi_cikar_sifir_uydurulmaz()
    {
        onizlemeHata = true;
        var e = VirmanEkrani();
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#gd-v-arac-hata")));
        Assert.Contains("okunamadı", e.Find("#gd-v-arac-hata").TextContent);
        Assert.Empty(e.FindAll("#gd-v-arac"));
    }
}
