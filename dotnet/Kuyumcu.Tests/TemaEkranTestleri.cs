using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA SERVİSİ — paket UX-2 Faz A1 (Aşkın K7 · Mehmet K1)
//
// ⚠ TEMA TEMİZLİĞİ (17.10): cihaza özel TemaSecici bileşeni (KESIF-P1'den beri hiçbir sayfada
//   çizilmiyordu) kaldırıldı; aydınlık/karanlık seçimi Görünüm kartındadır (sunucu, kullanıcı satırı —
//   S1fGorunumKartiTestleri). Aşağıdaki sözleşme TemaServisi'nin kendisini sınar (bileşensiz).
//
// ⚠ bUnit <html> çizmez → `data-tema` özniteliği DOM'dan OKUNAMAZ. Bu yüzden
//   sınanan şey ÇAĞRININ KENDİSİ ve ARGÜMANIDIR:
//   `kuyumcuTema.uygula("karanlik" | "aydinlik" | "sistem")`.
//   Özniteliği gerçekten kaldıran/yazan taraf wwwroot/js/tema.js'tir ve o da
//   TasarimBelirtecTestleri.Tema_betigi_EVAL_kullanmaz… ile sınanır.
//
// ⚠ `eval` YASAK: CSP script-src 'self' 'wasm-unsafe-eval' JS eval()'ına izin
//   vermez. Çağrı adlandırılmış fonksiyonadır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TemaEkranTestleri : BunitContext
{
    private const string Cagri = "kuyumcuTema.uygula";

    public TemaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(Cagri, _ => true).SetVoidResult();
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
    }

    private TemaServisi Tema => Services.GetRequiredService<TemaServisi>();

    private IReadOnlyList<object?[]> Cagrilar() =>
        JSInterop.Invocations[Cagri].Select(i => i.Arguments.ToArray()).ToList();

    [Fact]
    public void Varsayilan_tema_SISTEM_dir_ve_hicbir_JS_cagrisi_yapilmaz()
    {
        Assert.Equal(TemaServisi.Sistem, Tema.Mod);
        Assert.Empty(Cagrilar());   // sistem = öznitelik hiç yazılmaz
    }

    [Fact]
    public async Task Karanlik_secilince_KARANLIK_argumaniyla_uygulanir()
    {
        await Tema.DegistirAsync(TemaServisi.Karanlik);

        var cagrilar = Cagrilar();
        Assert.Single(cagrilar);
        Assert.Equal(TemaServisi.Karanlik, cagrilar[0][0]);
        Assert.Equal(TemaServisi.Karanlik, Tema.Mod);
    }

    [Fact]
    public async Task Aydinlik_secilince_AYDINLIK_argumaniyla_uygulanir()
    {
        await Tema.DegistirAsync(TemaServisi.Aydinlik);

        var cagrilar = Cagrilar();
        Assert.Single(cagrilar);
        Assert.Equal(TemaServisi.Aydinlik, cagrilar[0][0]);
        Assert.Equal(TemaServisi.Aydinlik, Tema.Mod);
    }

    [Fact]
    public async Task Sisteme_donunce_SISTEM_argumani_gider_BOS_DIZGE_DEGIL()
    {
        // JS tarafı "sistem" görünce removeAttribute yapar. Boş dizge
        // gönderilirse data-tema="" yazılır ve medya sorgusu bastırılır.
        await Tema.DegistirAsync(TemaServisi.Karanlik);
        await Tema.DegistirAsync(TemaServisi.Sistem);

        var cagrilar = Cagrilar();
        Assert.Equal(2, cagrilar.Count);
        Assert.Equal(TemaServisi.Sistem, cagrilar[1][0]);
        Assert.NotEqual("", cagrilar[1][0] as string);
        Assert.Equal(TemaServisi.Sistem, Tema.Mod);
    }

    [Fact]
    public async Task Beyaz_liste_disi_deger_SISTEME_duser()
    {
        await Tema.DegistirAsync("<script>alert(1)</script>");

        Assert.Equal(TemaServisi.Sistem, Tema.Mod);
        Assert.Equal(TemaServisi.Sistem, Cagrilar()[0][0]);
    }

    [Fact]
    public void Uc_tema_kodu_ve_hicbir_sayfa_eski_cihaz_seciciyi_cizmez()
    {
        // TemaSecici'nin id sözleşmesinin (#tema-sistem/-aydinlik/-karanlik) yerini Görünüm kartının
        // #gk-tema-* düğmeleri aldı (AyarlarTemaEkranTestleri aşağıda kilitler). Kod listesi AYNI kalır.
        Assert.Equal([TemaServisi.Sistem, TemaServisi.Aydinlik, TemaServisi.Karanlik], TemaServisi.Secenekler);
        foreach (var f in Directory.GetFiles(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web"), "*.razor", SearchOption.AllDirectories))
            Assert.DoesNotContain("<TemaSecici", File.ReadAllText(f), StringComparison.Ordinal);
    }
}

public sealed class AyarlarTemaEkranTestleri : BunitContext
{
    public AyarlarTemaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("kuyumcuTema.uygula", _ => true).SetVoidResult();
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<SurumServisi>();   // paket T-4a: Ayarlar künye kartı
        Services.AddScoped<ArayuzModuServisi>();   // UX-3: Ayarlar mod seçicisi
        Services.AddScoped<GorunumServisi>();      // P-36b: Ayarlar Görünüm bölümü
        Services.AddScoped<KullaniciGorunumServisi>();  // B-1: Ayarlar "Benim görünümüm" kartı
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        // ⚠ B-1: "{}" varsayılanı /kullanici/gorunum ucuna YETMEZ — servis
        //   "yüklenmedi" sayar ve kart kilitli çizilir. Gerçekçi yanıt döner.
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal)
                ? (istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde)
                : yol.EndsWith("/gorunum", StringComparison.Ordinal) ? B1SahteGorunumYaniti.Kiraci
                : "{}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void Ayarlar_sayfasinda_tema_karti_ve_secici_VARDIR()
    {
        // UX-2 tema seçicisinin evi Ayarlar'dır; UstBar'a DOM eklenmez
        // (Faz A1 "sıfır DOM değişikliği" sözü — Bertan KOŞUL 2).
        // KESIF-P1 · KIYAS TEMA-TEK: cihaza özel eski "Tema" kartı (TemaSecici,
        // localStorage) KALDIRILDI — tek seçici Görünüm kartıdır (sunucu, kullanıcı satırı).
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.Empty(e.FindAll("#ayar-tema"));
        Assert.Empty(e.FindAll("#tema-sistem"));
        Assert.NotNull(e.Find("#ayar-gorunum-karti #gk-tema-sistem"));
        Assert.NotNull(e.Find("#ayar-gorunum-karti #gk-tema-aydinlik"));
        Assert.NotNull(e.Find("#ayar-gorunum-karti #gk-tema-karanlik"));

        // Mevcut şifre formu bozulmadı (sıfır DOM kırığı)
        Assert.NotNull(e.Find("#mevcut-sifre"));
        Assert.NotNull(e.Find("#yeni-sifre"));
        Assert.NotNull(e.Find("#yeni-sifre-tekrar"));
    }
}
