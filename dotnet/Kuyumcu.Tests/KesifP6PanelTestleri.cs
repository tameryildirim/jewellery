using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P6 tur 2 — Panel kur kartında kur kaynağı SALT OKUNUR (YETKİ).
//   Doğrulayıcı KRİTİK: Panel'deki onaysız #kur-kaynak seçicisi MÜDÜR'e yalnız 403
//   veriyor, SAHİP'e İşletme onay kalıbını atlayan ikinci yol açıyordu. Ekip oyu
//   (30.09 18:10): kaynak YALNIZ SAHİP ve YALNIZ İşletme'de (onaylı) değişir.
//   Sahte API P37bKarsilamaTestleri ile aynı gerçekçi yanıtları verir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KesifP6PanelTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Bu yol parçasını içeren istek 500 döner (tek uç patlatma senaryosu).</summary>
    private readonly string? patlayanYol = null;

    /// <summary>
    /// S1-j: GET /api/v1/hava yanıtı. null = 204 (sunucuda hava KAPALI ya da
    /// kaynağa ulaşılamadı — gerçek sunucunun varsayılanı), aksi hâlde 200 + bu gövde.
    /// </summary>
    private readonly string? havaGovdesi = null;

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<string?> patlayan, Func<string?> hava) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            if (patlayan() is { } p && yol.Contains(p, StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"rapor ucu cevap vermedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // S1-j: hava artık KENDİ sunucumuzdan gelir (CSP connect-src 'self').
            if (yol.EndsWith("/api/v1/hava", StringComparison.Ordinal))
                return hava() is { } h
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(h, System.Text.Encoding.UTF8, "application/json"),
                    }
                    : new HttpResponseMessage(HttpStatusCode.NoContent);

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca GERÇEKÇİ yanıt.
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => """
                    {"satis":{"toplamTl":12500.00},"alis":{"toplamTl":3000.00},
                     "hurda":{"odenenTl":500.00},"netHasGramHareketi":4.25,
                     "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":9000.00,"paraBirimi":"TRY"}]}
                    """,
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """
                    {"gunler":[{"tarih":"2026-09-20","satisTl":1000.00},
                               {"tarih":"2026-09-21","satisTl":12500.00}]}
                    """,
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-09-21","bitis":"2026-09-21","toplamSatir":1,"ofset":0,
                     "limit":5,"dahaFazlasiVar":false,
                     "ozet":{"tlGiris":12500.00,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":1},
                     "storno":{"adet":0,"tutarTl":0},
                     "satirlar":[{"id":"h1","tarih":"2026-09-21T09:15:00Z","tur":"SATIS","yon":"GIRIS",
                                  "tlTutari":12500.00,"hasGrami":4.25,"maden":"ALTIN","paraBirimi":"TRY",
                                  "odemeTuru":"NAKIT","nakitTutar":12500.00,"veresiyeTutar":0,
                                  "fisId":"f1","faturaNo":null,"cariAd":"Ayşe Demir","urunAd":"22 Ayar Çeyrek",
                                  "subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false}]}
                    """,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK","TRUNCGIL"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public KesifP6PanelTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => patlayanYol, () => havaGovdesi))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        // S1-j: hava SUNUCUDAN gelir; sahte sunucunun varsayılanı 204 (KAPALI).
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private void OturumAc(string ad = "Ahmet", string rol = "SAHIP")
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", ad, "Yılmaz", "a@ornek.com", rol, "s1", null,
                                ArayuzModuServisi.Profesyonel));

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Ekran(string rol)
    {
        OturumAc(rol: rol);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-selam");
        return e;
    }

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public void Panel_kur_kaynagi_secici_yok_PATCH_yok(string rol)
    {
        var e = Ekran(rol);
        e.WaitForAssertion(() => Assert.Equal("Altınkaynak", e.Find("#panel-kur-kaynak-ad").TextContent.Trim()),
            TimeSpan.FromSeconds(10));
        // Seçici (combobox) yok, tıklanabilir kaynak listesi yok.
        Assert.Empty(e.FindAll("#kur-kaynak"));
        Assert.Empty(e.FindAll("#kur-kaynak-liste"));
        Assert.Empty(e.FindAll("#panel-kur-kaynak-alan [role='combobox']"));
        // Panel hiçbir koşulda kaynak yazmaz.
        Assert.DoesNotContain(istekler, i => i.Item1 == "PATCH" && i.Item2.Contains("/kur/kaynak", StringComparison.Ordinal));
    }

    [Fact]
    public void Panel_kur_kaynagi_SAHIPE_Isletme_baglantisi_verir()
    {
        var e = Ekran("SAHIP");
        e.WaitForElement("#panel-kur-kaynak-isletme-bag");
        Assert.Equal("/isletme", e.Find("#panel-kur-kaynak-isletme-bag").GetAttribute("href"));
        Assert.Equal(KesifP6Sozlugu.Tr.KaynakIsletmeBaglanti, e.Find("#panel-kur-kaynak-isletme-bag").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-kur-kaynak-yalniz-sahip"));
    }

    [Theory]
    [InlineData("MUDUR")]
    [InlineData("TEZGAHTAR")]
    public void Panel_kur_kaynagi_diger_rollere_sebep_yazar_baglanti_yok(string rol)
    {
        var e = Ekran(rol);
        e.WaitForElement("#panel-kur-kaynak-yalniz-sahip");
        Assert.Equal(KesifP6Sozlugu.Tr.KaynakYalnizSahip, e.Find("#panel-kur-kaynak-yalniz-sahip").TextContent.Trim());
        Assert.Empty(e.FindAll("#panel-kur-kaynak-isletme-bag"));
    }
}
