using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// /ayarlar → GÖRÜNÜM BÖLÜMÜ [bUnit] — paket P-36b (kurucu talimatı 16)
//
// "renk ve yazı tipi benim işim." Bu testler ekranın SÖZLEŞMESİNİ kilitler:
//   • bölüm var, iki seçici var, seçenekler TEK diziden üretilir (UX-S26/S31)
//   • SAHİP değilse kilitli ve SEBEBİ yazılı (UX-S30 · DoD EK md.1)
//   • seçim GERÇEK istek atar (tıklanınca hiçbir şey olmayan düğme YOK)
//   • kaydedince sonuç GÖRÜNÜR (UX-S27)
//
// ⚠ Sahte API'nin "{}" varsayılanı yeni uca YETMEZ: /gorunum GERÇEKÇİ yanıt
//   döndürür, yoksa servis "yüklenmedi" sayar ve test yanlış yeşil olurdu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GorunumEkranTestleri : BunitContext
{
    private readonly SahteApi sahte = new();

    public GorunumEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<SurumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        // ⚠ PAKET B-1: /ayarlar artık KENDİ görünüm kartını da çiziyor
        //   (@inject KullaniciGorunumServisi). Kayıt olmazsa sayfa HİÇ
        //   çizilmez — bu satır yoksa bu dosyanın 6 testi "DI yok" diye kırmızı.
        Services.AddScoped<KullaniciGorunumServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string Yontem, string Yol, string Govde)> Istekler { get; } = [];
        public string Sema { get; set; } = "altin";
        public string Olcek { get; set; } = "yogun";

        /// <summary>PAKET B-1 — kullanıcının KENDİ satırı (null = devral).</summary>
        public string? OzelSema { get; set; }

        /// <summary>Gövdeden gönderilen dizge alanı okur ("sema":"zumrut" → zumrut).</summary>
        private static string? Alan(string govde, string ad)
        {
            var im = System.Text.RegularExpressions.Regex.Match(
                govde, "\"" + ad + "\"\\s*:\\s*\"([^\"]*)\"");
            return im.Success ? im.Groups[1].Value : null;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Istekler.Add((istek.Method.Method, yol, govde));

            // ⚠ PAKET B-1: "/kullanici/gorunum" da "/gorunum" ile BİTER —
            //   bu dal ÖNCE gelmezse kullanıcı ucu dükkân gövdesini alır ve
            //   test YANLIŞ YEŞİL döner. Yanıt GERÇEKÇİDİR (boş "{}" değil).
            if (yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal))
            {
                if (istek.Method == HttpMethod.Patch && govde.Contains("\"sema\"", StringComparison.Ordinal))
                    OzelSema = Alan(govde, "sema");

                var etkin = OzelSema ?? Sema;
                var kaynak = OzelSema is null ? "kiraci" : "kullanici";
                var govdeYanit =
                    $$"""
                    {"temaModu":"sistem","sema":"{{etkin}}","yaziBoyutu":18,
                     "temaModuKaynagi":"sistem","semaKaynagi":"{{kaynak}}","yaziBoyutuKaynagi":"sistem",
                     "ozelTemaModu":null,"ozelSema":{{(OzelSema is null ? "null" : $"\"{OzelSema}\"")}},"ozelYaziBoyutu":null,
                     "kiraciTemaModu":"sistem","kiraciSema":"{{Sema}}","kiraciYaziBoyutu":18,
                     "semalar":["platin","zumrut","grafit","bordo","okyanus","menekse","zeytin","fildisi"],
                     "temaModlari":["sistem","aydinlik","karanlik"],"yaziBoyutlari":[16,18,20]}
                    """;
                return Json(istek.Method == HttpMethod.Patch
                    ? $$"""{"gorunum":{{govdeYanit}},"mesaj":"Görünüm kaydedildi"}"""
                    : govdeYanit);
            }

            if (yol.EndsWith("/gorunum", StringComparison.Ordinal))
            {
                if (istek.Method == HttpMethod.Patch)
                {
                    // ⚠ B-1: sabit "kurumsal" YAZILMAZ — gönderileni YANSITIR;
                    //   yoksa 8 şemalı listede 2. seçeneği seçmek "kurumsal"
                    //   döndürür ve ekran yanlışı doğru sanardı.
                    if (govde.Contains("\"sema\"", StringComparison.Ordinal)) Sema = Alan(govde, "sema") ?? Sema;
                    if (govde.Contains("yaziOlcegi", StringComparison.Ordinal)) Olcek = Alan(govde, "yaziOlcegi") ?? Olcek;
                    return Json($$"""{"gorunum":{"sema":"{{Sema}}","yaziOlcegi":"{{Olcek}}"},"mesaj":"Görünüm kaydedildi"}""");
                }
                return Json($$"""{"sema":"{{Sema}}","yaziOlcegi":"{{Olcek}}"}""");
            }
            return Json("{}");
        }

        private static HttpResponseMessage Json(string g) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private GorunumServisi Servis => Services.GetRequiredService<GorunumServisi>();

    private void SahipOl()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@x.com", "SAHIP", null));
    }

    private void TezgahtarOl()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u2", "Ali", "Veli", "a@x.com", "TEZGAHTAR", null));
    }

    // ── GE1) Bölüm ve iki seçici VARDIR ─────────────────────────────────────
    [Fact]
    public void GE1_Gorunum_bolumu_ve_punto_secicisi_vardir_sema_secicisi_yok()
    {
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.NotNull(e.Find("#ayar-gorunum"));
        Assert.Empty(e.FindAll("#gorunum-sema"));      // tema temizliği 17.10: tek şema Yıldız
        Assert.NotNull(e.Find("#gorunum-olcek"));

        // Mevcut bölümlere DOKUNULMADI (kilit yalnız yeni bölüm için açıldı).
        // KESIF-P1 · TEMA-TEK: cihaz tema kartı (#ayar-tema) bilerek kaldırıldı.
        Assert.Empty(e.FindAll("#ayar-tema"));
        Assert.NotNull(e.Find("#ayar-arayuz-modu"));
        Assert.NotNull(e.Find("#mevcut-sifre"));
    }

    // ── GE2) Seçenekler TEK diziden üretilir; ham kod EKRANA BASILMAZ ───────
    [Fact]
    public void GE2_Secenekler_tek_kaynaktan_uretilir_ve_ham_kod_basilmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.Equal(GorunumServisi.Olcekler.Length, e.FindAll("#gorunum-olcek .secim-secenek").Count);

        var metin = e.Find("#gorunum-olcek").TextContent;
        Assert.DoesNotContain("okunakli", metin);      // ham kod
        Assert.DoesNotContain("yogun", metin);
        Assert.Contains("Çok büyük", metin);           // sözlükten çevrilmiş ad
    }

    // ── GE3) SAHİP değilse KİLİTLİ ve sebebi YAZILI (UX-S30) ────────────────
    [Fact]
    public void GE3_Sahip_degilse_kilitli_ve_sebebi_yazili()
    {
        TezgahtarOl();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.NotEmpty(e.FindAll("#gorunum-olcek input.secim-radyo"));
        Assert.All(e.FindAll("#gorunum-olcek input.secim-radyo"),
            r => Assert.True(r.HasAttribute("disabled")));
        Assert.NotEmpty(e.Find("#gorunum-olcek-durum").TextContent.Trim());
    }

    // ── GE4) Ayar OKUNAMADIYSA kilitli — "varsayılan seçildi" diye gizlenmez ─
    //       (Anayasa md.1: bilinmeyen tahmin edilmez)
    [Fact]
    public void GE4_Ayar_okunmadiysa_kilitli_kalir()
    {
        SahipOl();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();   // YukleAsync çağrılmadı

        Assert.False(Servis.Yuklendi);
        Assert.All(e.FindAll("#gorunum-olcek input.secim-radyo"),
            r => Assert.True(r.HasAttribute("disabled")));
        Assert.NotEmpty(e.Find("#gorunum-olcek-durum").TextContent.Trim());
    }

    // ── GE5) SAHİP seçince GERÇEK PATCH atılır ve sonuç GÖRÜNÜR (UX-S27) ────
    [Fact]
    public async Task GE5_Punto_secimi_gercek_istek_atar_ve_sonuc_gorunur()
    {
        SahipOl();
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        // Tema temizliği (17.10): dükkânın tek seçimi punto; şema seçici kalktı.
        e.Find("#gorunum-olcek-secenek-2").Change(true);

        var beklenen = GorunumServisi.Olcekler[2];
        var patch = sahte.Istekler.Single(i =>
            i.Yontem == "PATCH" && i.Yol.EndsWith("/gorunum") && !i.Yol.Contains("/kullanici/"));
        Assert.Contains(beklenen, patch.Govde);
        Assert.Equal(beklenen, Servis.YaziOlcegi);
        e.WaitForAssertion(() => Assert.NotEmpty(e.Find("#gorunum-basari").TextContent.Trim()));
    }

    // ── GE6) PATCH gövdesi YALNIZ değişen alanı taşır (PATCH sözleşmesi) ────
    [Fact]
    public async Task GE6_Patch_govdesi_yalniz_degisen_alani_tasir()
    {
        SahipOl();
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        e.Find("#gorunum-olcek-secenek-1").Change(true);

        var patch = sahte.Istekler.Single(i => i.Yontem == "PATCH");
        Assert.Contains("yaziOlcegi", patch.Govde);
        Assert.DoesNotContain("\"sema\"", patch.Govde);   // öteki alan EZİLMEZ
    }

    // ── GE7) Beyaz liste İSTEMCİDE de uygulanır (iki taraf da güvenmez) ─────
    [Fact]
    public async Task GE7_Sunucudan_gelen_bilinmeyen_sema_varsayilana_duser()
    {
        sahte.Sema = "pembe";
        await Servis.YukleAsync();
        Assert.Equal(GorunumServisi.SemaVarsayilan, Servis.Sema);
    }

    // ── GE8) İstemci ve sunucu seçenek listeleri AYNI (tek kaynak) ──────────
    [Fact]
    public void GE8_Istemci_ve_sunucu_secenekleri_ayni()
    {
        Assert.Equal(Kuyumcu.Api.Modules.Gorunum.GorunumSecenekleri.Semalar, GorunumServisi.Semalar);
        Assert.Equal(Kuyumcu.Api.Modules.Gorunum.GorunumSecenekleri.Olcekler, GorunumServisi.Olcekler);
    }
}
