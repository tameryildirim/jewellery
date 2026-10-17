using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-1 — /gider "Toplam gider" SUNUCUNUN NET TOPLAMINI gösterir +
// basit modda "STORNO:" öneki "Geri alındı:" olur [bUnit + saf]
//
// ⚠ Sahte API "{}" ile bırakılmadı: GET /giderler gerçek sunucu biçimini döndürür
//   (storno satırı EKSİ tutarlı, stornoSatiri=true; toplamlar[] para birimi başına).
//   Bu biçim canlıda 5174'te ölçülen yanıttan alındı (QA 10 senaryosu).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q101GiderToplamEkranTestleri : BunitContext
{
    private const string Kasalar =
        """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":5508.51,"aktif":true,"subeId":null}]""";

    /// <summary>Canlıda ölçülen biçim: gider +125,50 (stornolandı) ve storno satırı -125,50.</summary>
    private const string StornoluSatirlar = """
        [{"id":"h2","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":-125.50,"paraBirimi":"TRY","aciklama":"STORNO: QA tarayici turu test kaydi geri aliniyor","tarih":"2026-09-24T14:22:23","stornoSatiri":true,"stornolandi":false},
         {"id":"h1","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":125.50,"paraBirimi":"TRY","aciklama":"Kira gideri","tarih":"2026-09-24T14:22:22","stornoSatiri":false,"stornolandi":true}]
        """;

    private string giderYaniti = "";

    public Q101GiderToplamEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => giderYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(Func<string> giderler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/gider-kategorileri") =>
                    """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":2,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") => giderler(),
                var y when y.EndsWith("/virman") =>
                    """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.EndsWith("/kasalar") => Kasalar,
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static string Yanit(string satirlar, string? toplamlar) =>
        "{\"satirlar\":" + satirlar + ",\"toplamAdet\":2,\"limit\":100,\"dahaFazlasiVar\":false,"
        + "\"sinirNotu\":\"En çok 100 gider satırı gösterilir.\""
        + (toplamlar is null ? "" : ",\"toplamlar\":" + toplamlar) + "}";

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ekran(bool basit = false)
    {
        var mod = basit ? ArayuzModuServisi.Basit : ArayuzModuServisi.Profesyonel;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => modu.BasitMi);
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        e.WaitForAssertion(() => Assert.Contains("Kira gideri", e.Markup));
        return e;
    }

    /// <summary>Boşluk türü (NBSP, dar NBSP) biçimden bağımsız karşılaştırılır.</summary>
    private static string N(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"[\s\u00A0\u202F]+", " ").Trim();

    private static string Toplam(IRenderedComponent<Kuyumcu.Web.Pages.Gider> e) =>
        N(e.Find("#gd-toplam").TextContent);

    /// <summary>
    /// QA 10 senaryosu: 125,50 + storno → ₺0,00 (eski ekran ₺251,00 basıyordu).
    /// MUTASYON HEDEFİ (MQ101-2): ekran sunucu toplamını okumazsa (toplamlar
    /// atanmazsa) ya da eski istemci formülüne dönerse KIRMIZI.
    /// </summary>
    [Fact]
    public void Q101_ekran_storno_sonrasi_toplami_sunucudan_NET_SIFIR_gosterir()
    {
        giderYaniti = Yanit(StornoluSatirlar, """[{"paraBirimi":"TRY","tutar":0.00,"adet":2}]""");
        var e = Ekran();
        var t = Toplam(e);
        Assert.Contains(N(Bicim.Tl(0m)), t);
        Assert.DoesNotContain("251", t);
    }

    /// <summary>Kısmi senaryo: toplam SUNUCUNUN rakamıdır, ekran satırlardan yeniden hesaplamaz.</summary>
    [Fact]
    public void Q101_ekran_kismi_stornoda_sunucunun_150_toplamini_yazar()
    {
        giderYaniti = Yanit(StornoluSatirlar, """[{"paraBirimi":"TRY","tutar":150.00,"adet":4}]""");
        var e = Ekran();
        Assert.Contains(N(Bicim.Tl(150m)), Toplam(e));
    }

    /// <summary>UX-S32: TRY ve USD toplamı ayrı ayrı, kendi işaretiyle yazılır; toplanmaz.</summary>
    [Fact]
    public void Q101_ekran_para_birimlerini_ayri_yazar()
    {
        giderYaniti = Yanit(StornoluSatirlar,
            """[{"paraBirimi":"TRY","tutar":300,"adet":1},{"paraBirimi":"USD","tutar":15.75,"adet":3}]""");
        var t = Toplam(Ekran());
        Assert.Contains(N(Bicim.Tl(300m)), t);
        Assert.Contains("15,75", t);
        Assert.Contains("$", t);
        Assert.DoesNotContain("315,75", t);
        Assert.DoesNotContain("TRY", t);
    }

    /// <summary>Sunucu toplam döndürmediyse rakam UYDURULMAZ: "—".</summary>
    [Fact]
    public void Q101_ekran_toplam_gelmezse_rakam_uydurmaz()
    {
        giderYaniti = Yanit(StornoluSatirlar, null);
        var t = Toplam(Ekran());
        Assert.Contains("—", t);
        Assert.DoesNotContain("251", t);
        Assert.DoesNotContain(N(Bicim.Tl(0m)), t);
    }

    /// <summary>
    /// D10: BASİT modda "STORNO:" öneki "Geri alındı:" olur.
    /// MUTASYON HEDEFİ (MQ101-3): mod koşulu ters çevrilirse KIRMIZI.
    /// </summary>
    [Fact]
    public void Q101_basit_modda_STORNO_oneki_Geri_alindi_olur()
    {
        giderYaniti = Yanit(StornoluSatirlar, """[{"paraBirimi":"TRY","tutar":0,"adet":2}]""");
        var e = Ekran(basit: true);
        Assert.Contains("Geri alındı: QA tarayici turu test kaydi geri aliniyor", e.Markup);
        Assert.DoesNotContain("STORNO", e.Markup.ToUpperInvariant().Replace("GD-STORNO", ""));
    }

    /// <summary>PROFESYONEL modda muhasebe dili korunur: "STORNO:" öneki aynen görünür.</summary>
    [Fact]
    public void Q101_profesyonel_modda_STORNO_oneki_korunur()
    {
        giderYaniti = Yanit(StornoluSatirlar, """[{"paraBirimi":"TRY","tutar":0,"adet":2}]""");
        var e = Ekran(basit: false);
        Assert.Contains("STORNO: QA tarayici turu test kaydi geri aliniyor", e.Markup);
        Assert.DoesNotContain("Geri alındı:", e.Markup);
    }

    /// <summary>
    /// Mod geçişi sayfa yenilenmeden gövdeye yansır (tarayıcıda ölçülen kusur:
    /// "Basit"e basınca tablo "STORNO:" göstermeye devam ediyordu).
    /// MUTASYON HEDEFİ (MQ101-12): Modu.Degisti aboneliği kaldırılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public void Q101_mod_gecisi_tabloyu_yeniden_cizer()
    {
        giderYaniti = Yanit(StornoluSatirlar, """[{"paraBirimi":"TRY","tutar":0,"adet":2}]""");
        var e = Ekran(basit: false);
        Assert.Contains("STORNO: QA tarayici", e.Markup);
        Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Contains("Geri alındı: QA tarayici", e.Markup));
    }

    // ── saf: StornoOnekiSozlugu.Goster ─────────────────────────────────────

    [Theory]
    [InlineData("STORNO: yanlış kasa", "tr", true, "Geri alındı: yanlış kasa")]
    [InlineData("Storno:yanlış kasa", "tr", true, "Geri alındı: yanlış kasa")]
    [InlineData("STORNO: wrong till", "en", true, "Undone: wrong till")]
    [InlineData("STORNO: yanlış kasa", "tr", false, "STORNO: yanlış kasa")]
    [InlineData("Kira ödemesi", "tr", true, "Kira ödemesi")]
    [InlineData("Stornolu değil", "tr", true, "Stornolu değil")]
    [InlineData("STORNO:", "tr", true, "Geri alındı:")]
    public void Q101_storno_oneki_gosterimi(string girdi, string dil, bool basit, string beklenen) =>
        Assert.Equal(beklenen, StornoOnekiSozlugu.Goster(girdi, dil, basit));

    [Fact]
    public void Q101_storno_oneki_null_ve_bos_aynen_doner()
    {
        Assert.Null(StornoOnekiSozlugu.Goster(null, "tr", true));
        Assert.Equal("", StornoOnekiSozlugu.Goster("", "tr", true));
    }
}
