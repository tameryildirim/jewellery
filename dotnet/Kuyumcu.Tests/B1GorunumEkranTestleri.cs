using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-1 — /ayarlar → "BENİM GÖRÜNÜMÜM" KARTI [bUnit]
//
// Tuval notu n4: "her kullanıcı kendi temasını seçer; SAHİP dükkân
// varsayılanını belirler." Bu dosya EKRAN sözleşmesini kilitler:
//   • kart + punto seçicisi vardır (tema temizliği 17.10: şema seçicisi kalktı — tek şema
//     Yıldız; tema modu seçicisi KESIF-P1'den beri Görünüm kartında)
//   • her seçicinin İLK seçeneği "Dükkân varsayılanı"dır → DEVRALMA (silme)
//     yolu ekranda VARDIR (UX-S25: oluşturulan kayıt düzeltilebilir)
//   • seçim GERÇEK PATCH atar (DoD EK md.1: tıklanınca hiçbir şey olmayan
//     kontrol YOK) ve sonuç BANTTA görünür (UX-S27)
//   • sunucudan okunamadıysa KİLİTLİ + SEBEBİ yazılı (UX-S30, Anayasa md.1)
//   • ham kod (platin/zumrut/karanlik) ekrana BASILMAZ (UX-S31)
//   • KÂĞIT DAİMA BEYAZ notu ekranda yazılıdır (kırmızı çizgi)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class B1GorunumEkranTestleri : BunitContext
{
    private readonly SahteApi sahte = new();

    public B1GorunumEkranTestleri()
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
        Services.AddScoped<KullaniciGorunumServisi>();
    }

    /// <summary>
    /// ⚠ Sahte API'nin "{}" varsayılanı yeni uca YETMEZ: /kullanici/gorunum
    ///   GERÇEKÇİ yanıt döndürür, yoksa servis "yüklenmedi" sayar ve seçiciler
    ///   kilitli kalır — test YANLIŞ YEŞİL olurdu.
    /// </summary>
    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string Yontem, string Yol, string Govde)> Istekler { get; } = [];

        /// <summary>Kullanıcının KENDİ punto satırı — null ise dükkândan devralıyor.</summary>
        public int? OzelYazi { get; set; }

        /// <summary>true ise sunucu 500 döner (okunamadı yolu).</summary>
        public bool Bozuk { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Istekler.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal))
            {
                if (Bozuk) return new HttpResponseMessage(HttpStatusCode.InternalServerError);

                if (istek.Method == HttpMethod.Patch
                    && System.Text.RegularExpressions.Regex.Match(
                           govde, "\"yaziBoyutu\"\\s*:\\s*(\\d+|null)") is { Success: true } im)
                    OzelYazi = im.Groups[1].Value == "null" ? null : int.Parse(im.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

                var etkin = OzelYazi ?? 20;                        // dükkân varsayılanı: 20 px
                var kaynak = OzelYazi is null ? "kiraci" : "kullanici";
                var ic =
                    $$"""
                    {"temaModu":"sistem","sema":"yildiz","yaziBoyutu":{{etkin}},
                     "temaModuKaynagi":"sistem","semaKaynagi":"sistem","yaziBoyutuKaynagi":"{{kaynak}}",
                     "ozelTemaModu":null,"ozelSema":null,"ozelYaziBoyutu":{{(OzelYazi is null ? "null" : OzelYazi.ToString())}},
                     "kiraciTemaModu":"sistem","kiraciSema":"yildiz","kiraciYaziBoyutu":20,
                     "semalar":["yildiz"],
                     "temaModlari":["sistem","aydinlik","karanlik"],"yaziBoyutlari":[16,18,20]}
                    """;
                return Json(istek.Method == HttpMethod.Patch
                    ? $$"""{"gorunum":{{ic}},"mesaj":"Görünüm kaydedildi"}"""
                    : ic);
            }

            if (yol.EndsWith("/gorunum", StringComparison.Ordinal))
                return Json("""{"sema":"yildiz","yaziOlcegi":"20","temaModu":"sistem"}""");

            return Json("{}");
        }

        private static HttpResponseMessage Json(string g) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private KullaniciGorunumServisi Servis => Services.GetRequiredService<KullaniciGorunumServisi>();

    // ── BE1) Kart ve punto seçicisi VARDIR; şema seçicisi YOK (tema temizliği 17.10) ─
    [Fact]
    public void BE1_Benim_gorunumum_karti_punto_secicisi_var_sema_secicisi_yok()
    {
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.NotNull(e.Find("#ayar-benim-gorunum"));
        Assert.Empty(e.FindAll("#benim-sema"));
        // KESIF-P1 · TEMA-TEK: tema radyosu burada değil (tek seçici Görünüm kartı); yeri yazılır.
        Assert.Empty(e.FindAll("#benim-tema"));
        Assert.NotNull(e.Find("#benim-tema-notu"));
        Assert.NotNull(e.Find("#benim-yazi"));

        // ⚠ MEVCUT HÂLİ BOZULMADI: dükkân kartı (punto) ve öteki bölümler yerinde; dükkân şema seçicisi YOK.
        Assert.NotNull(e.Find("#ayar-gorunum"));
        Assert.NotNull(e.Find("#gorunum-olcek"));
        Assert.Empty(e.FindAll("#gorunum-sema"));
        Assert.NotNull(e.Find("#ayar-arayuz-modu"));
    }

    // ── BE2) İLK seçenek "İşletme varsayılanı" — DEVRALMA yolu ekranda var ──
    //       (UX-S25: oluşturulan her kayıt düzeltilebilir; bu, silme yoludur)
    [Fact]
    public async Task BE2_Devralma_secenegi_ekranda_vardir_ve_liste_sunucudan_gelir()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        // 1 devral + 3 punto = 4.
        Assert.Equal(4, e.FindAll("#benim-yazi .secim-secenek").Count);
        Assert.Empty(e.FindAll("#benim-tema .secim-secenek"));   // KESIF-P1 · TEMA-TEK

        // Ham kod EKRANA BASILMAZ (UX-S31) — sözlükten çevrilmiş ad görünür.
        var metin = e.Find("#benim-yazi").TextContent;
        Assert.Contains("Çok büyük", metin);
        Assert.Contains("İşletme varsayılanı", metin);
    }

    // ── BE3) Seçim GERÇEK PATCH atar ve sonuç BANTTA görünür ────────────────
    //       (DoD EK md.1 + UX-S27)
    [Fact]
    public async Task BE3_Punto_secimi_gercek_istek_atar_ve_sonuc_gorunur()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        // 0 = "İşletme varsayılanı", 1 = 16, 2 = 18, 3 = 20
        e.Find("#benim-yazi-secenek-1").Change(true);

        var patch = sahte.Istekler.Single(i =>
            i.Yontem == "PATCH" && i.Yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal));
        Assert.Contains("\"yaziBoyutu\":16", patch.Govde);
        Assert.DoesNotContain("sema", patch.Govde);   // tek şema: şema alanı hiç gönderilmez
        Assert.Equal(16, Servis.Durum!.YaziBoyutu);
        Assert.Equal("kullanici", Servis.Durum!.YaziBoyutuKaynagi);

        e.WaitForAssertion(() => Assert.NotEmpty(e.Find("#benim-gorunum-basari").TextContent.Trim()));
    }

    // ── BE4) "İşletme varsayılanı" seçimi null gönderir → DEVREDER ──────────
    [Fact]
    public async Task BE4_Dukkan_varsayilani_secimi_null_gonderir()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        e.Find("#benim-yazi-secenek-1").Change(true);      // önce kendi seçimi
        e.Find("#benim-yazi-secenek-0").Change(true);      // sonra devret

        var sonPatch = sahte.Istekler.Last(i =>
            i.Yontem == "PATCH" && i.Yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal));
        Assert.Contains("null", sonPatch.Govde);

        Assert.Null(Servis.Durum!.OzelYaziBoyutu);
        Assert.Equal(20, Servis.Durum!.YaziBoyutu);        // dükkâna DÜŞTÜ
        Assert.Equal("kiraci", Servis.Durum!.YaziBoyutuKaynagi);
    }

    // ── BE5) Sunucudan OKUNAMADIYSA kilitli + SEBEBİ yazılı ─────────────────
    //       (UX-S30 · Anayasa md.1: "varsayılan seçildi" diye gizlenmez)
    [Fact]
    public async Task BE5_Okunamadiysa_kilitli_kalir_ve_sebebi_yazilir()
    {
        sahte.Bozuk = true;
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        Assert.False(Servis.Yuklendi);
        Assert.NotEmpty(e.FindAll("#benim-yazi input.secim-radyo"));
        Assert.All(e.FindAll("#benim-yazi input.secim-radyo"),
            r => Assert.True(r.HasAttribute("disabled")));
        Assert.NotEmpty(e.Find("#benim-yazi-durum").TextContent.Trim());

        // Kilitli seçici İSTEK ATMAZ — sessiz etkisizlik değil, kilit + sebep.
        var oncesi = sahte.Istekler.Count;
        e.Find("#benim-yazi-secenek-1").Change(true);
        Assert.Equal(oncesi, sahte.Istekler.Count);
    }

    // ── BE6) KIRMIZI ÇİZGİ: "kâğıt daima beyaz" notu EKRANDA yazılıdır ──────
    [Fact]
    public void BE6_Kagit_daima_beyaz_notu_ekranda_yazilidir()
    {
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        var not = e.Find("#benim-kagit-notu").TextContent;
        Assert.Contains("Kâğıt daima beyaz", not, StringComparison.Ordinal);
    }
}
