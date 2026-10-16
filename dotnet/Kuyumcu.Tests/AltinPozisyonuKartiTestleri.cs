using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-2 — ALTIN POZİSYONU KARTI (bUnit)
//
// Sahte API GERÇEKÇİ yanıt döner ('{}' varsayılanına düşmez). Kilitlenenler:
//   • büyük sayı sunucudan gelir, işaretli ve Türkçe ayraçlı basılır
//   • vitrin/alacak kırılımı + dönemden bağımsız alt metni (UX-S33)
//   • her düğme GERÇEK istek atar (DoD EK 1) — alacak/borç listesi
//   • gümüş ayrı satırda, altına eklenmez; "toplanmaz" notu (K20-2)
//   • 403 → yetki notu; 500 → hata bandı, boş durum DEĞİL (UX-S28)
//   • liste sınırı + "daha fazlası var" (UX-S29); cari bağlantısı (UX-S23)
//   • ham maden kodu ekrana basılmaz (UX-S31)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AltinPozisyonuKartiTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private HttpStatusCode pozisyonDurumu = HttpStatusCode.OK;

    private const string PozisyonYaniti = """
    {"anaMaden":"ALTIN",
     "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":36.4,"cariBorcGram":22.0,
              "netGram":312.8,"alacakliCariSayisi":3,"borcluOlduguCariSayisi":1,"konsinyeGram":45.8,"emanetGram":5},
     "digerMadenler":[{"maden":"GUMUS","vitrinGram":185,"hurdaGram":0,"cariAlacakGram":5,"cariBorcGram":0,
                       "netGram":190,"alacakliCariSayisi":1,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0}],
     "madenlerToplanmaz":true,"defterBakiyeFarki":0,
     "hesaplanmaAni":"2026-09-23T09:00:00Z","donemdenBagimsiz":true}
    """;

    private const string AlacakYaniti = """
    {"maden":"ALTIN","yon":"ALACAK","satirlar":[
       {"cariId":"c-cok","cariAd":"Ayşe Kaya","hasGram":20.0},
       {"cariId":"c-orta","cariAd":"Veli Demir","hasGram":12.4}],
     "toplamSatir":3,"ofset":0,"limit":50,"dahaFazlasiVar":true}
    """;

    private const string BorcYaniti = """
    {"maden":"ALTIN","yon":"BORC","satirlar":[
       {"cariId":"c-top","cariAd":"Mehmet Öz (toptancı)","hasGram":-22.0}],
     "toplamSatir":1,"ofset":0,"limit":50,"dahaFazlasiVar":false}
    """;

    public AltinPozisyonuKartiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<AltinPozisyonuKarti> Kart(string bekle = "#pozisyon-net")
    {
        var c = Render<AltinPozisyonuKarti>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find(bekle)));
        return c;
    }

    /// <summary>
    /// PAKET S1-g (kurucu bulgusu, 1280 px ölçümü): "Alacak 0,0 g" DÜĞME ROZETİ
    /// cümlenin ortasına düşüyor, satır space-between ile kırılıyordu. Tuval
    /// (Panel.dc.html satır 94) kırılımı aynı biçimli düz öğeler olarak dizer.
    /// Kilit: kırılım kabı .pozisyon-kirilim; her öğe .pozisyon-kirilim-oge;
    /// kırılımda .dugme / .satir-arasi YOK; CSS öğeyi kırmaz (nowrap).
    /// </summary>
    [Fact]
    public void S1g_kirilim_ogeleri_AYNI_bicimde_rozet_metne_karismaz()
    {
        var c = Kart();
        var kirilim = c.Find("#pozisyon-kirilim");

        Assert.Contains("pozisyon-kirilim", kirilim.ClassName ?? "");
        Assert.DoesNotContain("satir-arasi", kirilim.ClassName ?? "");
        Assert.Empty(kirilim.QuerySelectorAll(".dugme"));
        foreach (var id in new[] { "pozisyon-vitrin", "pozisyon-alacak", "pozisyon-borc" })
            Assert.Contains("pozisyon-kirilim-oge", c.Find($"#{id}").ClassName ?? "");

        var css = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(),
            "Kuyumcu.Web", "wwwroot", "css", "app.css"));
        Assert.Matches(@"\.pozisyon-kirilim-oge\s*\{[^}]*white-space:\s*nowrap", css);
        Assert.Matches(@"\.pozisyon-kirilim\s*\{[^}]*flex-wrap:\s*wrap", css);
    }

    [Fact]
    public void B2K_01_buyuk_sayi_ve_kirilim_sunucudan_gelir()
    {
        var c = Kart();
        Assert.Equal("Altın pozisyonum", c.Find("#pozisyon-baslik").TextContent.Trim());
        // MADEN-2 (ilke "tek has biçimi"): net pozisyon Bicim.Has ile AYNI — binlik + 4 ondalık.
        Assert.Equal("+312,8000 has", c.Find("#pozisyon-net").TextContent.Trim());
        Assert.Contains("Vitrin 298,4000 has", c.Find("#pozisyon-vitrin").TextContent);
        Assert.Contains("Alacak 36,4000 has", c.Find("#pozisyon-alacak").TextContent);
        Assert.Contains("Borç 22,0000 has", c.Find("#pozisyon-borc").TextContent);
        Assert.Equal("/urunler?maden=ALTIN", c.Find("#pozisyon-vitrin").GetAttribute("href"));
        Assert.Contains("dönemden bağımsız", c.Find("#pozisyon-donem").TextContent);
        // Hurda sıfırken bağlantı çizilmez; tutarlılık farkı yokken uyarı yok.
        Assert.Empty(c.FindAll("#pozisyon-hurda"));
        Assert.Empty(c.FindAll("#pozisyon-defter-farki"));
        Assert.Single(istekler, i => i.EndsWith("/pozisyon"));
    }

    [Fact]
    public void B2K_02_gumus_ayri_satirda_altina_eklenmez_ham_kod_basilmaz()
    {
        var c = Kart();
        var satir = c.Find("#pozisyon-maden-GUMUS").TextContent;
        Assert.Contains("Gümüş", satir);
        Assert.Contains("+190,0 g", satir);
        Assert.Contains("toplanmaz", c.Find("#pozisyon-toplanmaz").TextContent);
        // Birleşik rakam (312,8 + 190 = 502,8) hiçbir yerde yok.
        Assert.DoesNotContain("502,8", c.Markup);
        Assert.DoesNotContain(">GUMUS", c.Markup);
        // Konsinye ve emanet "pozisyona girmez" notuyla ayrı.
        Assert.Contains("45,8", c.Find("#pozisyon-konsinye").TextContent);
        Assert.Contains("pozisyona girmez", c.Find("#pozisyon-disarida").TextContent);
    }

    [Fact]
    public void B2K_03_alacak_dugmesi_istek_atar_liste_sinir_ve_baglanti_cizer()
    {
        var c = Kart();
        istekler.Clear();
        c.Find("#pozisyon-alacak").Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-cari-liste")));
        Assert.Contains(istekler, i => i.Contains("/pozisyon/cariler") && i.Contains("yon=ALACAK")
                                       && i.Contains("maden=ALTIN") && i.Contains("limit=50"));
        Assert.Contains("Bize altın borçlu olanlar", c.Find("#pozisyon-cari-liste-baslik").TextContent);
        Assert.Equal("/cariler?cariId=c-cok", c.Find("#pozisyon-cari-c-cok a").GetAttribute("href"));
        Assert.Contains("20,0", c.Find("#pozisyon-cari-c-cok").TextContent);
        Assert.Contains("50", c.Find("#pozisyon-cari-sinir").TextContent);
        Assert.Contains("3 kayıttan 2", c.Find("#pozisyon-cari-daha-fazla").TextContent);
    }

    [Fact]
    public void B2K_04_borc_dugmesi_ters_yonu_ister_ve_mutlak_deger_basar()
    {
        var c = Kart();
        istekler.Clear();
        c.Find("#pozisyon-borc").Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-cari-c-top")));
        Assert.Contains(istekler, i => i.Contains("yon=BORC"));
        Assert.Contains("Bizim altın borçlu olduklarımız", c.Find("#pozisyon-cari-liste-baslik").TextContent);
        var satir = c.Find("#pozisyon-cari-c-top").TextContent;
        Assert.Contains("22,0", satir);
        Assert.DoesNotContain("-22", satir);
        Assert.Empty(c.FindAll("#pozisyon-cari-daha-fazla"));
    }

    [Fact]
    public void B2K_05_yetkisiz_rol_403_yetki_notu_gorur_hata_degil()
    {
        pozisyonDurumu = HttpStatusCode.Forbidden;
        var c = Kart("#pozisyon-yetki-yok");
        Assert.Contains("patron", c.Find("#pozisyon-yetki-yok").TextContent);
        Assert.Empty(c.FindAll("#pozisyon-hata"));
        Assert.Empty(c.FindAll("#pozisyon-net"));
    }

    [Fact]
    public void B2K_06_sunucu_hatasi_bos_durum_degil_hata_bandi()
    {
        pozisyonDurumu = HttpStatusCode.InternalServerError;
        var c = Kart("#pozisyon-hata");
        Assert.Contains("Altın pozisyonu okunamadı", c.Find("#pozisyon-hata").TextContent);
        Assert.Empty(c.FindAll(".bos-durum"));
        Assert.Empty(c.FindAll("#pozisyon-net"));
    }

    [Fact]
    public void B2K_07_degismez_ipucu_duzeltme_yolunu_yazar_ve_her_dugme_istek_atar()
    {
        var c = Kart();
        var ipucu = c.Find("#degismez-ipucu-AltinPozisyonu").TextContent;
        Assert.Contains("hesaplanır", ipucu);
        Assert.Contains("düzeltme fişi", ipucu);

        // DoD EK 1: karttaki HER düğme gerçek istek atar (tıklanınca hiçbir şey
        // olmayan düğme yok).
        foreach (var id in c.FindAll("button").Select(b => b.Id).ToList())
        {
            istekler.Clear();
            c.Find($"#{id}").Click();
            c.WaitForAssertion(() => Assert.NotEmpty(istekler));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CJ_02_degismez_ipucu_BASIT_modda_cari_demez_PRO_metni_korunur(bool basit)
    {
        // PAKET CUMA-J (demo taraması /raporlar K8): "cari için düzeltme fişi" basit modda jargondu.
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Kart();
        var ipucu = c.Find("#degismez-ipucu-AltinPozisyonu").TextContent;
        if (basit)
        {
            Assert.Contains("Yanlışsa kaynağını düzeltin: stok için sayım, müşteri hesabı için düzeltme fişi, hurda için hurda ekranı.", ipucu, StringComparison.Ordinal);
            Assert.Contains("müşteri hesabı için düzeltme fişi", ipucu, StringComparison.Ordinal);
            Assert.DoesNotMatch(CumaJJargon.Re, ipucu);
        }
        else
        {
            Assert.Contains(Kuyumcu.Core.I18n.PozisyonSozlugu.Tr.DuzeltmeYolu, ipucu, StringComparison.Ordinal);
            Assert.Contains("cari için düzeltme fişi", ipucu, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void B2K_08_sozluk_tum_madenleri_ve_iki_dili_karsilar()
    {
        foreach (var m in Kuyumcu.Core.Domain.Maden.Hepsi)
            Assert.True(PozisyonSozlugu.MadenVar(m), $"sözlükte yok: {m}");
        Assert.Equal("Silver", PozisyonSozlugu.MadenAdi("GUMUS", "en"));
        Assert.Equal("Diğer maden", PozisyonSozlugu.MadenAdi("BILINMEYEN", "tr"));
        Assert.NotEqual(PozisyonSozlugu.Tr.Baslik, PozisyonSozlugu.En.Baslik);
    }

    private sealed class SahteApi(AltinPozisyonuKartiTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            t.istekler.Add(tam);

            if (yol.EndsWith("/pozisyon") && t.pozisyonDurumu != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(t.pozisyonDurumu)
                {
                    Content = new StringContent("""{"message":"sunucu reddetti"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            var g = yol switch
            {
                var y when y.EndsWith("/pozisyon/cariler") && tam.Contains("yon=BORC") => BorcYaniti,
                var y when y.EndsWith("/pozisyon/cariler") => AlacakYaniti,
                var y when y.EndsWith("/pozisyon") => PozisyonYaniti,
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
