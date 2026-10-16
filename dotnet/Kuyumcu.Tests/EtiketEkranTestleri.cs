using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-66 — ETİKET BASIM TASLAĞININ KALICILIĞI (/urunler/etiketler)
//
// QA tur 8 (ORTA): sayfanın tek ağ çağrıları GET /urunler + GET /urunler/madenler
// idi; 200 ürünlük basım hazırlığı tek yanlış tıkla yok oluyor, kayıp sessizdi.
// Hedef: seçim + kopya + arama tarayıcı deposunda KİRACI + KULLANICI anahtarıyla
// saklanır, açılışta geri yüklenir, eksik kalem SAYIYLA bildirilir, "Taslağı
// temizle" formu boşaltır, depo kapalıysa sayfa hatasız çalışır.
//
// Depo, bUnit JSInterop üzerinden bellek içi bir sözlükle taklit edilir:
// localStorage.setItem/removeItem çağrıları sözlüğe yazar, getItem ondan okur.
// Böylece "sayfadan çıkıp dönmek" = bileşeni atıp aynı depoyla yeniden çizmek.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EtiketEkranTestleri : BunitContext
{
    private const string Kiraci = "01KIRACI0000000000000000AA";
    private const string Kullanici = "01KULLANICI000000000000BB";
    private const string Anahtar = "kuyumcu-etiket-taslak:" + Kiraci + ":" + Kullanici;

    private string urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"Y-1","ad":"Safir yüzük","ayarId":"K14",
          "gramAgirlik":4.0,"tasAgirlik":0.4,"hasGrami":2.3,"barkod":"K0123456789","aktif":true,
          "tasKokeni":"SENTETIK","tasIbaresi":null,"milyem":585,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":false},
         {"id":"u2","tip":"PLATIN","kod":"Y-2","ad":"Sade alyans","ayarId":"PT950",
          "gramAgirlik":5.0,"tasAgirlik":0.0,"hasGrami":4.75,"barkod":null,"aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":950,"maden":"PLATIN","madenAdi":"Platin","rodyumKaplama":false},
         {"id":"u3","tip":"ZIYNET","kod":"B-3","ad":"Burma bilezik","ayarId":"K22",
          "gramAgirlik":15.0,"tasAgirlik":0.0,"hasGrami":13.7,"barkod":"869003","aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":916,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":false}]
        """;

    /// <summary>Bellek içi tarayıcı deposu (anahtar → değer).</summary>
    private readonly Dictionary<string, string> depo = [];
    private readonly List<string> istekYollari = [];

    public EtiketEkranTestleri()
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

    private sealed class SahteApi(EtiketEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (t.istekYollari) t.istekYollari.Add(istek.Method.Method + " " + yol);
            if (yol.EndsWith("/urunler/madenler"))
                return Task.FromResult(Tamam("""{"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}"""));
            return Task.FromResult(Tamam(t.urunler));
        }

        private static HttpResponseMessage Tamam(string g) => new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────

    private static string B64Url(string s) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Oturum jetonu: yük tenantId + sub taşır (TokenServisi'nin gerçek talep adları).</summary>
    private static string Jeton(string kiraci, string kullanici) =>
        $"{B64Url("""{"alg":"HS256"}""")}.{B64Url($$"""{"sub":"{{kullanici}}","tenantId":"{{kiraci}}"}""")}.imza";

    private void OturumAc(string kiraci = Kiraci, string kullanici = Kullanici)
    {
        var o = Services.GetRequiredService<OturumServisi>();
        o.TokenKaydetAsync(Jeton(kiraci, kullanici), "yenile").GetAwaiter().GetResult();
    }

    /// <summary>JS deposunu bellek içi sözlüğe bağlar.</summary>
    private void DepoyuBagla()
    {
        JSInterop.Setup<string?>("localStorage.getItem", _ => true)
            .SetResult(null); // varsayılan; aşağıdaki anahtara özel kurulum önce eşleşir
        JSInterop.SetupVoid("localStorage.setItem", c =>
        {
            depo[(string)c.Arguments[0]!] = (string)c.Arguments[1]!;
            return true;
        }).SetVoidResult();
        JSInterop.SetupVoid("localStorage.removeItem", c =>
        {
            depo.Remove((string)c.Arguments[0]!);
            return true;
        }).SetVoidResult();
    }

    /// <summary>Sayfaya dönüş: depodaki taslak getItem ile verilir.</summary>
    private void DepodanOkut(string anahtar = Anahtar)
    {
        if (depo.TryGetValue(anahtar, out var v))
            JSInterop.Setup<string?>("localStorage.getItem", anahtar).SetResult(v);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> Ciz() => Render<Kuyumcu.Web.Pages.UrunEtiketleri>();

    private static void Sec(IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> e, string urunAdi)
        => e.FindAll("input[type=checkbox]").First(i => i.GetAttribute("aria-label") == urunAdi).Change(true);

    private static bool Secili(IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> e, string urunAdi)
        => e.FindAll("input[type=checkbox]").First(i => i.GetAttribute("aria-label") == urunAdi).HasAttribute("checked");

    private static EtiketTaslakMetinleri T => EtiketTaslakSozlugu.Tr;

    // ── P-66 kabul testleri ──────────────────────────────────────────────────

    [Fact]
    public async Task EtiketTaslagiGeriYuklenir()
    {
        OturumAc();
        DepoyuBagla();
        var e = Ciz();
        e.WaitForState(() => e.FindAll("input[type=checkbox]").Count == 3);
        Sec(e, "Safir yüzük");
        Sec(e, "Burma bilezik");
        e.Find("#et-adet").Input("4");

        // Taslak KİRACI + KULLANICI anahtarıyla yazıldı ve içeriği ekranla aynı.
        Assert.True(depo.ContainsKey(Anahtar), "taslak kiracı+kullanıcı anahtarıyla yazılmadı");
        using (var j = System.Text.Json.JsonDocument.Parse(depo[Anahtar]))
        {
            Assert.Equal(new[] { "u1", "u3" }, j.RootElement.GetProperty("secili").EnumerateArray().Select(x => x.GetString()).OfType<string>().ToArray());
            Assert.Equal(4, j.RootElement.GetProperty("kopya").GetInt32());
        }

        // Sayfadan çık → dön.
        await DisposeComponentsAsync();
        DepodanOkut();
        var d = Ciz();
        d.WaitForState(() => d.FindAll("#et-taslak-geri-yuklendi").Count == 1);

        Assert.True(Secili(d, "Safir yüzük"));
        Assert.True(Secili(d, "Burma bilezik"));
        Assert.False(Secili(d, "Sade alyans"));
        Assert.Equal("4", d.Find("#et-adet").GetAttribute("value"));
        Assert.Equal(8, d.FindAll(".urun-etiket").Count);
        Assert.Equal(string.Format(T.GeriYuklendi, 2, 4), d.Find("#et-taslak-geri-yuklendi").TextContent.Trim());
        Assert.Empty(d.FindAll("#et-taslak-eksik"));
    }

    [Fact]
    public void EtiketTaslagi_baska_kullanicinin_taslagini_okumaz()
    {
        OturumAc(kullanici: "01BASKAKULLANICI0000000CC");
        DepoyuBagla();
        depo[Anahtar] = """{"v":1,"secili":["u1","u2"],"kopya":3,"arama":""}""";
        DepodanOkut(); // yalnız ASIL kullanıcının anahtarına yanıt kurulu
        var e = Ciz();
        e.WaitForState(() => e.FindAll("input[type=checkbox]").Count == 3);
        Assert.Empty(e.FindAll("#et-taslak-geri-yuklendi"));
        Assert.Empty(e.FindAll(".urun-etiket"));
    }

    [Fact]
    public void EksikUrunlerGeriYuklemedeBildirilir()
    {
        OturumAc();
        DepoyuBagla();
        // u9 ve u7 artık listede yok (silinmiş/pasife alınmış) — sessizce düşürülmez.
        depo[Anahtar] = """{"v":1,"secili":["u1","u9","u7"],"kopya":2,"arama":""}""";
        DepodanOkut();
        var e = Ciz();
        e.WaitForState(() => e.FindAll("#et-taslak-geri-yuklendi").Count == 1);

        Assert.Equal(string.Format(T.EksikKalem, 2), e.Find("#et-taslak-eksik").TextContent.Trim());
        Assert.True(Secili(e, "Safir yüzük"));
        Assert.Equal(2, e.FindAll(".urun-etiket").Count);   // 1 ürün × 2 kopya
        // Depo ekranla aynı: budanmış taslak geri yazıldı.
        using var j = System.Text.Json.JsonDocument.Parse(depo[Anahtar]);
        Assert.Equal(new[] { "u1" }, j.RootElement.GetProperty("secili").EnumerateArray().Select(x => x.GetString()).OfType<string>().ToArray());
    }

    [Fact]
    public void TaslagiTemizleFormuBosaltir()
    {
        OturumAc();
        DepoyuBagla();
        depo[Anahtar] = """{"v":1,"secili":["u1","u2"],"kopya":5,"arama":"y-"}""";
        DepodanOkut();
        var e = Ciz();
        e.WaitForState(() => e.FindAll("#et-taslak-temizle").Count == 1);
        Assert.Equal(10, e.FindAll(".urun-etiket").Count);

        e.Find("#et-taslak-temizle").Click();

        Assert.False(depo.ContainsKey(Anahtar), "taslak depodan silinmedi");
        Assert.Empty(e.FindAll(".urun-etiket"));
        Assert.Equal("1", e.Find("#et-adet").GetAttribute("value"));
        Assert.Equal("", e.Find("#et-ara").GetAttribute("value") ?? "");
        Assert.All(e.FindAll("input[type=checkbox]"), c => Assert.False(c.HasAttribute("checked")));
        Assert.Contains(T.Temizlendi, e.Find("#et-taslak-temizlendi").TextContent);
        Assert.Empty(e.FindAll("#et-taslak-geri-yuklendi"));
        Assert.Empty(e.FindAll("#et-taslak-temizle"));   // temizlenecek şey kalmadı → düğme yok

        // Geri al: bellekteki kopya döner ve depo yeniden yazılır.
        e.Find("#et-taslak-geri-al").Click();
        Assert.Equal(10, e.FindAll(".urun-etiket").Count);
        Assert.True(depo.ContainsKey(Anahtar));
    }

    [Fact]
    public void DepoErisilemezseSayfaCalisir()
    {
        OturumAc();
        JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetException(new JSException("SecurityError: storage disabled"));
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("SecurityError: storage disabled"));
        JSInterop.SetupVoid("localStorage.removeItem", _ => true).SetException(new JSException("SecurityError: storage disabled"));

        var e = Ciz();
        e.WaitForState(() => e.FindAll("input[type=checkbox]").Count == 3);

        Assert.Empty(e.FindAll("#hata-bandi"));
        Assert.Empty(e.FindAll("#et-taslak-geri-yuklendi"));
        Assert.Equal(T.SaklanamiyorNotu, e.Find("#et-taslak-saklanamiyor").TextContent.Trim());

        // Ekran çalışmaya devam eder: seçim, adet, önizleme, temizleme.
        Sec(e, "Sade alyans");
        e.Find("#et-adet").Input("3");
        Assert.Equal(3, e.FindAll(".urun-etiket").Count);
        Assert.False(e.Find("#et-yazdir").HasAttribute("disabled"));
        e.Find("#et-taslak-temizle").Click();
        Assert.Empty(e.FindAll(".urun-etiket"));
    }

    [Fact]
    public void Yazdirma_sonrasi_taslak_temizlenir_ekran_secimi_korunur()
    {
        OturumAc();
        DepoyuBagla();
        var e = Ciz();
        e.WaitForState(() => e.FindAll("input[type=checkbox]").Count == 3);
        Sec(e, "Safir yüzük");
        Assert.True(depo.ContainsKey(Anahtar));

        e.Find("#et-yazdir").Click();

        Assert.Single(JSInterop.Invocations["window.print"]);
        Assert.False(depo.ContainsKey(Anahtar), "yazdırmadan sonra taslak silinmedi");
        Assert.Equal(T.YazdirmaGonderildi, e.Find("#et-yazdir-basari").TextContent.Trim());
        Assert.Single(e.FindAll(".urun-etiket"));
    }

    [Fact]
    public void Kimliksiz_oturumda_depo_kullanilmaz_ve_bu_soylenir()
    {
        DepoyuBagla();   // jeton yok → kiracı/kullanıcı çözülemez
        var e = Ciz();
        e.WaitForState(() => e.FindAll("input[type=checkbox]").Count == 3);
        Sec(e, "Safir yüzük");
        Assert.Empty(depo);
        Assert.NotNull(e.Find("#et-taslak-saklanamiyor"));
    }

    [Fact]
    public void Taslak_metinleri_TR_EN_dolu_ve_farkli()
    {
        foreach (var p in typeof(EtiketTaslakMetinleri).GetProperties())
        {
            var tr = (string)p.GetValue(EtiketTaslakSozlugu.Tr)!;
            var en = (string)p.GetValue(EtiketTaslakSozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            Assert.NotEqual(tr, en);
        }
    }
}
