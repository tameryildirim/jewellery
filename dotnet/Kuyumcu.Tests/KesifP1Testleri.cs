using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P1 · PARA-VERİ-YETKİ — keşif 30.09 (#5 #7 #10 #12 #30 #31 #44 #45
// #60 #61 #71 #72) + KIYAS TEMA-TEK + TOPLU ZO-ISLEM-A "geri al sonrası liste".
//
// ⚠ Sahte API'ler DOKUNULAN her uca GERÇEKÇİ yanıt verir ('{}' varsayılanı
//   ekranı sessizce boş bırakıp testi yanlış yeşil yapardı).
// ═════════════════════════════════════════════════════════════════════════════

internal static class P1Ortak
{
    public static HttpResponseMessage Json(string g, HttpStatusCode kod = HttpStatusCode.OK) => new(kod)
    {
        Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
    };

    public static JsonElement Govde(string g) => JsonDocument.Parse(g).RootElement;
}

// ─────────────────────────────────────────────────────────────────────────────
// #7 + TEMA-TEK — /ayarlar
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1AyarlarTestleri : BunitContext
{
    private readonly SahteApi sahte = new();

    public KesifP1AyarlarTestleri()
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
    /// /kullanici/gorunum — PATCH yanıtı <see cref="Kapi"/> açılana dek BEKLETİLİR
    /// (canlı bulgu 800 ms gecikmede üretildi; burada gecikme elle tutulur).
    /// </summary>
    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string Yontem, string Yol, string Govde)> Istekler { get; } = [];
        public TaskCompletionSource? Kapi { get; set; }
        public bool PatchBozuk { get; set; }
        public string? OzelTema { get; set; }
        public int? OzelYazi { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (Istekler) Istekler.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal))
            {
                if (istek.Method == HttpMethod.Patch)
                {
                    if (Kapi is { } k) await k.Task;
                    if (PatchBozuk) return P1Ortak.Json("""{"message":"Kaydedilemedi"}""", HttpStatusCode.InternalServerError);
                    var g = P1Ortak.Govde(govde);
                    if (g.TryGetProperty("temaModu", out var t)) OzelTema = t.ValueKind == JsonValueKind.Null ? null : t.GetString();
                    if (g.TryGetProperty("yaziBoyutu", out var y)) OzelYazi = y.ValueKind == JsonValueKind.Null ? null : y.GetInt32();
                    return P1Ortak.Json($$"""{"gorunum":{{Durum()}},"mesaj":"Görünüm kaydedildi"}""");
                }
                return P1Ortak.Json(Durum());
            }
            if (yol.EndsWith("/gorunum", StringComparison.Ordinal))
                return P1Ortak.Json("""{"sema":"grafit","yaziOlcegi":"18","temaModu":"sistem"}""");
            return P1Ortak.Json("{}");
        }

        private string Durum()
        {
            var tema = OzelTema ?? "sistem";
            var yazi = OzelYazi ?? 18;
            return $$"""
                {"temaModu":"{{tema}}","sema":"grafit","yaziBoyutu":{{yazi}},
                 "temaModuKaynagi":"{{(OzelTema is null ? "kiraci" : "kullanici")}}","semaKaynagi":"kiraci",
                 "yaziBoyutuKaynagi":"{{(OzelYazi is null ? "kiraci" : "kullanici")}}",
                 "ozelTemaModu":{{(OzelTema is null ? "null" : $"\"{OzelTema}\"")}},"ozelSema":null,
                 "ozelYaziBoyutu":{{(OzelYazi is null ? "null" : OzelYazi.Value.ToString(CultureInfo.InvariantCulture))}},
                 "kiraciTemaModu":"sistem","kiraciSema":"grafit","kiraciYaziBoyutu":18,
                 "semalar":["platin","zumrut","grafit","bordo","okyanus","menekse","zeytin","fildisi"],
                 "temaModlari":["sistem","aydinlik","karanlik"],"yaziBoyutlari":[16,18,20]}
                """;
        }

        public (string Yontem, string Yol, string Govde) SonPatch()
        {
            lock (Istekler) return Istekler.Last(i => i.Yontem == "PATCH" && i.Yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal));
        }
    }

    private KullaniciGorunumServisi Servis => Services.GetRequiredService<KullaniciGorunumServisi>();

    private static (int Isaretli, int Secili) RadyoDurumu(IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> e, string grup)
    {
        var radyolar = e.FindAll($"#{grup} input.secim-radyo");
        var etiketler = e.FindAll($"#{grup} .secim-secenek");
        var isaretli = radyolar.Select((r, i) => (r, i)).Where(x => x.r.HasAttribute("checked")).Select(x => x.i).DefaultIfEmpty(-1).Single();
        var secili = etiketler.Select((l, i) => (l, i)).Where(x => x.l.ClassList.Contains("secili")).Select(x => x.i).DefaultIfEmpty(-1).Single();
        return (isaretli, secili);
    }

    /// <summary>
    /// Kabul #7: art arda 3 seçim — Tema (Karanlık → Aydınlık → Sistem; TEK seçici Görünüm kartı)
    /// ve Yazı boyutu (20 → 18 → Dükkân varsayılanı; Benim görünümüm radyosu). Her adımda: işaretli
    /// radyo = `.secili` kart (istek SÜRERKEN de) = son PATCH gövdesi = ardından gelen GET değeri.
    /// </summary>
    [Fact]
    public async Task Ayarlar_benim_tema_radyosu_PATCH_govdesiyle_ayni()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();

        // ── Tema: tek seçici (Görünüm kartı) — aria-pressed + .secili ──
        foreach (var kod in new[] { "karanlik", "aydinlik", "sistem" })
        {
            sahte.Kapi = new TaskCompletionSource();
            var tik = e.Find($"#gk-tema-{kod}").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            sahte.Kapi.SetResult();
            await tik;
            e.WaitForAssertion(() =>
            {
                var basili = e.FindAll("#ayar-gorunum-karti [id^='gk-tema-'][aria-pressed='true']");
                Assert.Single(basili);
                Assert.Equal($"gk-tema-{kod}", basili[0].Id);
                Assert.Contains("secili", e.Find($"#gk-tema-{kod}").ClassList);
            });
            Assert.Equal(kod, P1Ortak.Govde(sahte.SonPatch().Govde).GetProperty("temaModu").GetString());
            await Servis.YukleAsync();                                  // ardından gelen GET
            Assert.Equal(kod, Servis.Durum!.OzelTemaModu);
        }

        // ── Yazı boyutu: Benim görünümüm radyosu — 0 devral · 1=16 · 2=18 · 3=20 ──
        foreach (var (ix, beklenen) in new (int, int?)[] { (3, 20), (2, 18), (0, null) })
        {
            sahte.Kapi = new TaskCompletionSource();
            var degis = e.Find($"#benim-yazi-secenek-{ix}").ChangeAsync(new ChangeEventArgs { Value = true });
            // İstek SÜRERKEN: işaretli radyo ile .secili kart AYNI (bulgu: bir adım geriden).
            e.WaitForAssertion(() => Assert.Equal((ix, ix), RadyoDurumu(e, "benim-yazi")));
            sahte.Kapi.SetResult();
            await degis;
            e.WaitForAssertion(() => Assert.Equal((ix, ix), RadyoDurumu(e, "benim-yazi")));
            var g = P1Ortak.Govde(sahte.SonPatch().Govde).GetProperty("yaziBoyutu");
            Assert.Equal(beklenen, g.ValueKind == JsonValueKind.Null ? null : g.GetInt32());
            await Servis.YukleAsync();
            Assert.Equal(beklenen, Servis.Durum!.OzelYaziBoyutu);
        }
    }

    /// <summary>Kayıt BAŞARISIZSA ekran eski değere döner: radyo ve .secili yine aynı (Anayasa md.1).</summary>
    [Fact]
    public async Task Ayarlar_yazi_kaydi_basarisizsa_radyo_eski_degere_doner()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        Assert.Equal((0, 0), RadyoDurumu(e, "benim-yazi"));

        sahte.PatchBozuk = true;
        sahte.Kapi = new TaskCompletionSource();
        var degis = e.Find("#benim-yazi-secenek-3").ChangeAsync(new ChangeEventArgs { Value = true });
        e.WaitForAssertion(() => Assert.Equal((3, 3), RadyoDurumu(e, "benim-yazi")));
        sahte.Kapi.SetResult();
        await degis;
        e.WaitForAssertion(() => Assert.Equal((0, 0), RadyoDurumu(e, "benim-yazi")));
        e.WaitForAssertion(() => Assert.NotEmpty(e.Find("#benim-gorunum-hata").TextContent.Trim()));
    }

    /// <summary>KIYAS TEMA-TEK: sayfadaki aydınlık/karanlık seçici sayısı 3 → 1.</summary>
    [Fact]
    public async Task Ayarlar_tek_tema_secici_var()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        // Aydınlık seçeneği taşıyan kontrol grupları: Görünüm kartı (gk-tema-*), eski TemaSecici (#tema-*),
        // eski Benim görünümüm radyosu (#benim-tema). Yalnız ilki kalır.
        Assert.Single(e.FindAll("[id='gk-tema-aydinlik']"));
        Assert.Empty(e.FindAll("#tema-aydinlik"));
        Assert.Empty(e.FindAll("#benim-tema"));
        Assert.Empty(e.FindAll("#ayar-tema"));
        Assert.Equal(KesifP1Sozlugu.Tr.BenimTemaNotu, e.Find("#benim-tema-notu").TextContent.Trim());
    }

    /// <summary>TEMA-TEK: sunucu değeri UYGULANIR (kuyumcuTema.uygula) — yalnız /ayarlar'da değil, açılışta da.</summary>
    [Fact]
    public async Task TemaTek_sunucu_degeri_uygulanir_ve_acilista_yuklenir()
    {
        sahte.OzelTema = "karanlik";
        await Servis.YukleAsync();
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "kuyumcuTema.uygula"
            && i.Arguments.Count == 1 && (string?)i.Arguments[0] == "karanlik");

        // Program.cs: açılışta ve her oturum değişiminde kullanıcı görünümü okunur.
        var program = CssKaynak.Metin("Kuyumcu.Web", "Program.cs");
        Assert.Contains("GetRequiredService<KullaniciGorunumServisi>()", program, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"if \(oturum\.GirisYapildi\) await GorunumleriYukle\(\);"), program);
        Assert.Matches(new Regex(@"oturum\.Degisti \+= \(\) => \{ if \(oturum\.GirisYapildi\) _ = GorunumleriYukle\(\); \};"), program);
        // Sıra: önce dükkân, sonra kullanıcı (kullanıcı seçimi dükkânı ezer).
        var g = program.IndexOf("await gorunum.YukleAsync();", StringComparison.Ordinal);
        var b = program.IndexOf("await benimGorunum.YukleAsync();", StringComparison.Ordinal);
        Assert.True(g > 0 && b > g);

        // Hiçbir sayfa cihaz tema seçicisini (TemaSecici, localStorage) çizmez.
        foreach (var f in Directory.GetFiles(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages"), "*.razor"))
            Assert.DoesNotContain("<TemaSecici", File.ReadAllText(f), StringComparison.Ordinal);
    }

    /// <summary>
    /// Tur 3 (orkestratör 18:10): Görünüm kartında seçili işaret TEK KALIP — her seçili kartta tam bir
    /// ✓ (.gk-isaret, svg), satırının SON öğesi (sağda ayrı öğe), satırı ya düğmenin kendisi ya da
    /// .gk-sema-alt; "18 px" metnine gömülü ✓ yok; seçili olmayan kartta işaret yok.
    /// </summary>
    [Fact]
    public async Task GorunumKarti_secili_isaret_tek_kalip_sagda_ayri_oge()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        // TEMA TEMİZLİĞİ 17.10: tek şema Yıldız — Görünüm kartındaki renk şeması seçicileri (#gk-sema-*) bilerek kaldırıldı;
        // seçili işaret kalıbı kalan üç bölümde (aydınlık/karanlık · yazı · mod) AYNI sınanır.
        e.WaitForElement("#gk-tema-sistem.secili");
        Assert.Empty(e.FindAll("#ayar-gorunum-karti [id^='gk-sema-']"));
        var secililer = e.FindAll("#ayar-gorunum-karti .gk-sema.secili, #ayar-gorunum-karti .gk-secenek.secili");
        Assert.Equal<string>(["gk-tema-sistem", "gk-yazi-18", "gk-mod-profesyonel"], secililer.Select(b => b.Id ?? "").ToList());
        foreach (var b in secililer)
        {
            var isaret = Assert.Single(b.QuerySelectorAll(".gk-isaret"));
            Assert.NotNull(isaret.QuerySelector("svg"));
            Assert.Null(isaret.NextElementSibling);                       // satırın en sağındaki öğe
            var satir = isaret.ParentElement!;
            Assert.True(satir == b || satir.ClassList.Contains("gk-sema-alt"), b.Id + " işaret satırı: " + satir.ClassName);
            Assert.NotNull(isaret.PreviousElementSibling);               // solunda ad var (ayrı öğe)
        }
        Assert.Empty(e.FindAll("#ayar-gorunum-karti .gk-piksel .gk-tik"));
        Assert.Equal(secililer.Count, e.FindAll("#ayar-gorunum-karti .gk-isaret").Count);
    }

    /// <summary>#45: arayüz modu düğmelerinde ikon + metin tek kapta (satir-ici, gap) — boşluksuz yan yana değil.</summary>
    [Fact]
    public async Task Ayarlar_ikonlu_dugmelerde_ikon_metin_ayri_kapta()
    {
        await Servis.YukleAsync();
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        foreach (var id in new[] { "arayuz-devral", "arayuz-basit", "arayuz-profesyonel" })
        {
            var kap = e.Find($"#{id} > span.satir-ici");
            Assert.NotNull(kap.QuerySelector("svg"));
            Assert.False(string.IsNullOrWhiteSpace(kap.QuerySelector("span")?.TextContent));
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #12 · #71 · #72(İşletme) — /isletme
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1IsletmeTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool tenant403;
    private bool saltOkunur;

    public KesifP1IsletmeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(KesifP1IsletmeTestleri t) : HttpMessageHandler
    {
        private string maliyet = "FIFO";
        private string kur = "TRUNCGIL";
        private string mod = "PROFESYONEL";
        private bool vezne;

        private string Tenant() =>
            $$"""{"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true,"durum":"{{(t.saltOkunur ? "SALT_OKUNUR" : "AKTIF")}}","saltOkunurMu":{{(t.saltOkunur ? "true" : "false")}},"kurKaynagi":"{{kur}}","maliyetYontemi":"{{maliyet}}","varsayilanArayuzModu":"{{mod}}"}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var patch = istek.Method == HttpMethod.Patch;

            if (yol.EndsWith("/admin/tenant") && t.tenant403)
                return P1Ortak.Json("""{"message":"Bu işlem için gerekli rol: işletme sahibi"}""", HttpStatusCode.Forbidden);
            if (yol.EndsWith("/admin/tenant/maliyet-yontemi") && patch)
            { maliyet = P1Ortak.Govde(govde).GetProperty("yontem").GetString()!; return P1Ortak.Json(Tenant()); }
            if (yol.EndsWith("/admin/tenant") && patch)
            { mod = P1Ortak.Govde(govde).GetProperty("varsayilanArayuzModu").GetString()!; return P1Ortak.Json(Tenant()); }
            if (yol.EndsWith("/admin/tenant")) return P1Ortak.Json(Tenant());
            if (yol.EndsWith("/kur/kaynak") && patch)
            { kur = P1Ortak.Govde(govde).GetProperty("kaynak").GetString()!; return P1Ortak.Json("""{"kaynak":"ok"}"""); }
            if (yol.EndsWith("/kur/kaynaklar"))
                return P1Ortak.Json("""{"kaynaklar":["TRUNCGIL","ALTINKAYNAK","ANKARA"],"varsayilan":"TRUNCGIL","secili":null}""");
            if (yol.EndsWith("/admin/vezne") && patch)
            { vezne = P1Ortak.Govde(govde).GetProperty("zorunlu").GetBoolean(); return P1Ortak.Json($$"""{"zorunlu":{{(vezne ? "true" : "false")}}}"""); }
            if (yol.EndsWith("/admin/vezne")) return P1Ortak.Json($$"""{"zorunlu":{{(vezne ? "true" : "false")}}}""");
            if (yol.EndsWith("/subeler")) return P1Ortak.Json("""[{"id":"s1","ad":"Merkez","aktif":true}]""");
            return P1Ortak.Json("{}");
        }
    }

    private List<(string Metot, string Yol, string Govde)> Patchler() { lock (istekler) return [.. istekler.Where(i => i.Metot == "PATCH")]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        e.WaitForElement("#i-maliyet-yontemi");
        lock (istekler) istekler.Clear();
        return e;
    }

    /// <summary>
    /// Kabul #12: maliyet yöntemi, kur kaynağı, varsayılan arayüz modu, vezne — seçim anında istek 0;
    /// "Kaydet" + onay penceresi (maliyet etki cümlesi); onay = TEK PATCH + başarı bandı.
    /// </summary>
    [Fact]
    public void Isletme_secim_istek_atmaz_Kaydet_onayla_tek_PATCH()
    {
        var e = Ac();
        SeciciYardim.Sec(e, "i-maliyet-yontemi", "ORTALAMA");
        SeciciYardim.Sec(e, "i-kur-kaynagi", "ALTINKAYNAK");
        SeciciYardim.Sec(e, "i-varsayilan-arayuz-modu", "BASIT");
        e.Find("#i-vezne-zorunlu").Change(true);
        Assert.Empty(Patchler());                                  // seçim anında ağ isteği 0
        Assert.Contains(Sozlukler.Tr.Yonetim.MaliyetYontemi, e.Find("#i-ayar-taslak-metin").TextContent);

        // Yalnız MALİYET değişsin: öteki üçü taslaktan geri alınır (seçim yine istek atmaz).
        SeciciYardim.Sec(e, "i-kur-kaynagi", "TRUNCGIL");
        SeciciYardim.Sec(e, "i-varsayilan-arayuz-modu", "PROFESYONEL");
        e.Find("#i-vezne-zorunlu").Change(false);
        Assert.Empty(Patchler());

        e.Find("#i-ayar-kaydet").Click();
        var aciklama = e.WaitForElement("#i-ayar-onay-aciklama").TextContent;
        Assert.Contains("Bundan sonraki satışların maliyeti Ağırlıklı ortalama ile hesaplanır.", aciklama);
        Assert.Empty(Patchler());                                  // pencere açılması da istek atmaz

        e.Find("#i-ayar-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Patchler()));
        var p = Patchler().Single();
        Assert.EndsWith("/admin/tenant/maliyet-yontemi", p.Yol);
        Assert.Equal("ORTALAMA", P1Ortak.Govde(p.Govde).GetProperty("yontem").GetString());
        e.WaitForAssertion(() => Assert.Contains(Sozlukler.Tr.Yonetim.MaliyetYontemi, e.Find("#basari-bandi").TextContent));
        Assert.Empty(e.FindAll("#i-ayar-taslak"));                 // taslak kalmadı
    }

    /// <summary>Kabul #12 İptal: onay penceresinde Vazgeç → istek 0, ekran ESKİ değere döner.</summary>
    [Fact]
    public void Isletme_iptal_eski_degere_doner()
    {
        var e = Ac();
        SeciciYardim.Sec(e, "i-maliyet-yontemi", "LIFO");
        SeciciYardim.Sec(e, "i-kur-kaynagi", "ANKARA");
        e.Find("#i-ayar-kaydet").Click();
        e.WaitForElement("#i-ayar-onay-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#i-ayar-onay-onayla")));
        Assert.Empty(Patchler());
        Assert.Equal("FIFO", SeciciYardim.SeciliDeger(e, "i-maliyet-yontemi"));
        Assert.Equal("TRUNCGIL", SeciciYardim.SeciliDeger(e, "i-kur-kaynagi"));
        Assert.Empty(e.FindAll("#i-ayar-taslak"));

        // Taslak satırındaki Vazgeç de istek atmadan geri döndürür.
        SeciciYardim.Sec(e, "i-varsayilan-arayuz-modu", "BASIT");
        e.Find("#i-ayar-vazgec").Click();
        Assert.Empty(Patchler());
        Assert.Equal("PROFESYONEL", SeciciYardim.SeciliDeger(e, "i-varsayilan-arayuz-modu"));
    }

    /// <summary>Onayda çoklu değişiklik: her biri kendi mevcut ucuna BİR KEZ; kur kaynağı PATCH /kur/kaynak (kur yeniden çekilir).</summary>
    [Fact]
    public void Isletme_coklu_degisiklik_her_ucu_bir_kez_yazar()
    {
        var e = Ac();
        SeciciYardim.Sec(e, "i-kur-kaynagi", "ALTINKAYNAK");
        e.Find("#i-vezne-zorunlu").Change(true);
        e.Find("#i-ayar-kaydet").Click();
        var aciklama = e.WaitForElement("#i-ayar-onay-aciklama").TextContent;
        Assert.Contains("Truncgil", aciklama);
        Assert.Contains("Altınkaynak", aciklama);
        e.Find("#i-ayar-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Equal(2, Patchler().Count));
        Assert.Single(Patchler(), p => p.Yol.EndsWith("/kur/kaynak") && p.Govde.Contains("ALTINKAYNAK"));
        Assert.Single(Patchler(), p => p.Yol.EndsWith("/admin/vezne") && P1Ortak.Govde(p.Govde).GetProperty("zorunlu").GetBoolean());
    }

    /// <summary>Kabul #71: kur kaynağı seçeneklerinde ham kod 0; ad üst çubuktaki adla aynı (KurKaynakAdi.Ad).</summary>
    /// <summary>Tur 3 (orkestratör 18:10): ekranda ham rol kodu (SAHİP), "AI modülü", "dört göz kuralı" yok.</summary>
    [Fact]
    public void Isletme_ham_rol_kodu_ve_jargon_yok()
    {
        var e = Ac();
        var m = e.Markup;
        Assert.DoesNotMatch(new Regex("SAH[İI]P"), m);
        Assert.DoesNotContain("AI modül", m, StringComparison.Ordinal);
        Assert.DoesNotContain("dört göz", m, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(KesifP1Sozlugu.Tr.AkilliOneriler, e.Find("#i-ai-etkin-sar").TextContent);
        Assert.Equal(KesifP1Sozlugu.Tr.IsletmeAciklama, e.Find("#isletme-aciklama").TextContent.Trim());
        Assert.Equal(KesifP1Sozlugu.Tr.VezneAyrimAciklama, e.Find("#i-vezne-aciklama").TextContent.Trim());
    }

    /// <summary>
    /// Tur 3: kilitli (salt okunur) işletmede ayar kutularının kilit sebebi de ham rol kodu taşımaz —
    /// "Bu ayarı yalnız dükkân sahibi değiştirebilir." (Kilit sebebi yalnız kilitliyken çizilir; M1-38 bunu yakaladı.)
    /// </summary>
    [Fact]
    public void Isletme_kilit_sebebinde_ham_rol_kodu_yok()
    {
        saltOkunur = true;
        var e = Ac();
        e.WaitForAssertion(() => Assert.Contains(KesifP1Sozlugu.Tr.IsletmeYalnizSahip, e.Find("#i-ai-etkin-sar").TextContent));
        Assert.DoesNotMatch(new Regex("SAH[İI]P"), e.Markup);
    }

    [Fact]
    public void Isletme_kur_kaynagi_ham_kod_yok()
    {
        var e = Ac();
        var liste = SeciciYardim.ListeMetni(e, "i-kur-kaynagi");
        foreach (var ham in new[] { "ALTINKAYNAK", "ANKARA", "TRUNCGIL" })
        {
            Assert.DoesNotContain(ham, liste, StringComparison.Ordinal);
            Assert.Contains(KurKaynakAdi.Ad(ham, Sozlukler.Tr), liste);
        }
    }

    /// <summary>#72: /admin/tenant 403 → "Yükleniyor" yazılmaz; kilit sebebi sunucu metnidir.</summary>
    [Fact]
    public void Isletme_tenant_403te_sonsuz_Yukleniyor_yok()
    {
        tenant403 = true;
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        e.WaitForAssertion(() => Assert.Contains("işletme sahibi", e.Find("#i-kimlik-duzenle-kilit").TextContent));
        Assert.DoesNotContain(Sozlukler.Tr.Ortak.Yukleniyor, e.Find("#i-kimlik-duzenle-kilit").TextContent);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #72 — sayfa düzeyinde rol kapısı
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1YetkiKapisiTestleri : BunitContext
{
    private static readonly string[] SahipRotalari =
        ["/isletme", "/yetkiler", "/yedekler", "/denetim-izi", "/veri-ice-aktarim", "/veri-disa-aktarim"];

    public KesifP1YetkiKapisiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private void Rol(string rol) => Services.GetRequiredService<OturumServisi>().KullaniciAta(
        new OturumKullanici("u1", "Keşif", "Müdür", "m@ornek.com", rol, "s1"));

    private IRenderedComponent<YetkiKapisi> Ciz(string rota, bool profilDenendi = true)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        return Render<YetkiKapisi>(p => p
            .Add(x => x.ProfilDenendi, profilDenendi)
            .AddChildContent("<div id=\"cocuk\"><div role=\"tablist\">Sekme</div><button id=\"duzenle\">Düzenle</button>Yükleniyor… SAHIP</div>"));
    }

    /// <summary>Kabul #72: MÜDÜR 6 SAHİP rotasında tek durum; sekme/düzenle yok; Yükleniyor 0; SAHIP 0.</summary>
    [Fact]
    public void YetkiKapisi_MUDUR_6_rota_Yukleniyor_yok()
    {
        Rol("MUDUR");
        foreach (var rota in SahipRotalari)
        {
            var roller = YetkiKapisi.Rota(rota);                     // harita MenuTanimi'nden
            Assert.NotNull(roller);
            Assert.Equal(["SAHIP"], roller);
            var e = Ciz(rota);
            Assert.Equal("Bu sayfayı yalnız işletme sahibi açabilir", e.Find("#yetki-kapisi-baslik").TextContent.Trim());
            Assert.Empty(e.FindAll("#cocuk"));
            Assert.Empty(e.FindAll("[role=tablist]"));
            Assert.Empty(e.FindAll("#duzenle"));
            Assert.DoesNotContain("Yükleniyor", e.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("SAHIP", e.Markup, StringComparison.Ordinal);
            Assert.Contains("müdür", e.Find("#yetki-kapisi-aciklama").TextContent);
            Assert.Equal("/", e.Find("#yetki-kapisi-panel").GetAttribute("href"));
        }
    }

    [Fact]
    public void YetkiKapisi_SAHIP_ve_kapisiz_rota_sayfayi_cizer()
    {
        Rol("SAHIP");
        foreach (var rota in SahipRotalari)
            Assert.NotNull(Ciz(rota).Find("#cocuk"));
        Rol("TEZGAHTAR");
        Assert.Null(YetkiKapisi.Rota("/ayarlar"));
        Assert.NotNull(Ciz("/ayarlar").Find("#cocuk"));
        Assert.NotNull(Ciz("/bilinmeyen-rota").Find("#cocuk"));
    }

    /// <summary>Profil henüz gelmediyse kısa yükleniyor; profil denendi ama rol yoksa karar sunucuya kalır.</summary>
    [Fact]
    public void YetkiKapisi_rol_bilinmiyorsa_tahmin_etmez()
    {
        var bekliyor = Ciz("/isletme", profilDenendi: false);
        Assert.Empty(bekliyor.FindAll("#cocuk"));
        Assert.Empty(bekliyor.FindAll("#yetki-kapisi"));
        var denendi = Ciz("/isletme", profilDenendi: true);
        Assert.NotNull(denendi.Find("#cocuk"));
    }

    [Fact]
    public void AnaYerlesim_sayfayi_YetkiKapisi_ile_sarar()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Layout", "AnaYerlesim.razor");
        Assert.Contains("<YetkiKapisi ProfilDenendi=\"@profilDenendi\">@Body</YetkiKapisi>", kaynak, StringComparison.Ordinal);
    }

    /// <summary>Rol adı tek kaynak: ham kod çevrilir, bilinmeyen kod uydurulmaz.</summary>
    [Fact]
    public void RolAdi_tek_kaynak()
    {
        Assert.Equal("işletme sahibi ya da müdür", KesifP1Sozlugu.RolListesi(["SAHIP", "MUDUR"]));
        Assert.Equal("the shop owner or a manager", KesifP1Sozlugu.RolListesi(["SAHIP", "MUDUR"], "en"));
        Assert.Equal("YENI_ROL", KesifP1Sozlugu.RolAdi("YENI_ROL"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #10 · #60 · #61 — /fiyatlandirma ürün paneli
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1FiyatPaneliTestleri : BunitContext
{
    public KesifP1FiyatPaneliTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1"));
    }

    private readonly SahteApi sahte = new();

    private sealed class SahteApi : HttpMessageHandler
    {
        /// <summary>Süzgece uyan toplam (satır 3); sınır uyarısı testinde 60 yapılır.</summary>
        public int Toplam { get; set; } = 3;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (yol.Contains("/onizleme"))
                return Task.FromResult(P1Ortak.Json(ONIZLEME.Replace("\"toplam\":3,", $"\"toplam\":{Toplam},", StringComparison.Ordinal)));
            return Task.FromResult(P1Ortak.Json("{}"));
        }

        private const string ONIZLEME = """
                    {"toplam":3,"limit":50,"offset":0,"satirlar":[
                     {"urunId":"u1","kod":"BLZ-22-015","ad":"22 Ayar Burma Bilezik","ayarId":"K22","gram":15.19,"hasGrami":13.92,"iscilikTl":null,
                      "teklifVar":true,"sebep":null,"hasDegeri":92206.95,"marjTl":3688.28,"iscilikTlUygulanan":0,"listeFiyati":95895.23,
                      "indirimTutari":0,"satisFiyati":95895.23,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null},
                     {"urunId":"u2","kod":"GMS-BLK-925","ad":"925 Gümüş Bilezik","ayarId":"G925","gram":40,"hasGrami":37,"iscilikTl":null,
                      "teklifVar":true,"sebep":null,"hasDegeri":3200,"marjTl":0,"iscilikTlUygulanan":0,"listeFiyati":3200,
                      "indirimTutari":0,"satisFiyati":3200,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null},
                     {"urunId":"u3","kod":"KPE-14-001","ad":"14 Ayar Küpe Tozu","ayarId":"K14","gram":0.0002,"hasGrami":0.0001,"iscilikTl":null,
                      "teklifVar":true,"sebep":null,"hasDegeri":0.6,"marjTl":0,"iscilikTlUygulanan":0,"listeFiyati":0.6,
                      "indirimTutari":0,"satisFiyati":0.6,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null}]}
                    """;
    }

    private static readonly Regex EnUs = new(@"\d{1,3}(,\d{3})+\.\d");

    private IRenderedComponent<FiyatListesiUrunPaneli> Ciz()
    {
        var e = Render<FiyatListesiUrunPaneli>(p => p
            .Add(x => x.ListeId, "fl1").Add(x => x.ListeAdi, "Perakende").Add(x => x.KurId, "kur1"));
        e.WaitForElement("#flp-satir-u1");
        return e;
    }

    /// <summary>Kabul #10: kültür en-US'e zorlansa bile para "92.206,95 ₺", gram "13,92"; en-US kalıbı 0.</summary>
    [Fact]
    public void FiyatListesiUrunPaneli_en_US_kulturunde_tr_TR_bicim()
    {
        var (k, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var e = Ciz();
            var metin = e.Find("#flp-satir-u1").TextContent;
            Assert.Contains("92.206,95\u00a0₺", metin);
            Assert.Contains("95.895,23\u00a0₺", metin);
            Assert.Contains("13,92", metin);
            Assert.Contains("3.200\u00a0₺", e.Find("#flp-satir-u2").TextContent);   // "3,200" (=3,2) değil
            Assert.DoesNotMatch(EnUs, e.Markup);
            Assert.DoesNotContain("92,206", e.Markup, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = k;
            CultureInfo.CurrentUICulture = ui;
        }
    }

    /// <summary>
    /// Tur 2 (doğrulayıcı KRİTİK): has gramı 2 haneye yuvarlanınca sıfıra düşen sıfırdan farklı değer
    /// "0" GÖRÜNMEZ — 0,0001 → "0,0001"; olağan değer 2 hane ("13,92"). Tek kaynak Bicim.KarHasGram.
    /// </summary>
    [Fact]
    public void FiyatListesiUrunPaneli_kucuk_has_sifir_gorunmez()
    {
        var e = Ciz();
        e.WaitForElement("#flp-satir-u3");
        var hucreler = e.FindAll("#flp-satir-u3 td").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("0,0001", hucreler);
        Assert.DoesNotContain("0", hucreler);
        Assert.Contains("13,92", e.FindAll("#flp-satir-u1 td").Select(t => t.TextContent.Trim()));
    }

    /// <summary>
    /// Tur 3 (orkestratör 18:10): teknik açıklama satırları YOK ("sunucuda hesaplanır", "sayfa başına 50
    /// satır", "Süzgeçe uyan toplam"); sınır uyarısı sınıra değmeden çizilmez; süzgeç satırında iki kutunun
    /// da görünür etiketi var, onay kutusu kendi satırında.
    /// </summary>
    [Fact]
    public void FiyatListesiUrunPaneli_teknik_satir_yok_suzgec_tek_etiket()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#flp-sinir"));
        Assert.Empty(e.FindAll("#flp-toplam"));
        Assert.Empty(e.FindAll("#flp-daha-fazla"));
        Assert.DoesNotContain("sunucuda hesaplanır", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("50 satır", e.Markup, StringComparison.Ordinal);
        Assert.Equal(KesifP1Sozlugu.Tr.FiyatUrunAra, e.Find("#flp-suzgec #flp-ara-etiket").TextContent.Trim());
        Assert.NotNull(e.Find("#flp-suzgec label[for='flp-kategori']"));
        Assert.Equal(2, e.Find("#flp-suzgec").Children.Length);
        Assert.Empty(e.FindAll("#flp-suzgec #flp-yalniz-gk"));
        Assert.NotNull(e.Find("#flp-yalniz-gk"));
    }

    /// <summary>UX-S29: sınıra değince TEK kısa uyarı — kaç üründen kaçının göründüğü ve daraltma yolu.</summary>
    [Fact]
    public void FiyatListesiUrunPaneli_sinira_degince_tek_kisa_uyari()
    {
        sahte.Toplam = 60;
        var e = Ciz();
        Assert.Equal(string.Format(KesifP1Sozlugu.Tr.FiyatDahaFazla, 60, 3), e.Find("#flp-daha-fazla").TextContent.Trim());
        Assert.Empty(e.FindAll("#flp-toplam"));
    }

    /// <summary>
    /// Tur 3 (orkestratör 18:10): PRO'da satır eylemi ("Ürüne özel") ve seçim kutusu İLK sütunda
    /// (sola yapışık, kesif-p1.css) — ayrı sağ "Eylemler" sütunu yok; başlık/hücre sayısı eşit.
    /// BASİT'te İşçilik ve Kaynak sütunları çizilmez, eylem yok.
    /// </summary>
    [Fact]
    public void FiyatListesiUrunPaneli_PRO_eylem_ilk_sutunda_BASIT_kaynak_iscilik_gizli()
    {
        var e = Ciz();
        var basliklar = e.FindAll("#flp-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(10, basliklar.Count);
        Assert.Contains("Kaynak", basliklar);
        Assert.DoesNotContain("Eylemler", basliklar);
        Assert.Contains("flp-urun-baslik", e.Find("#flp-tablo thead th").ClassList);
        foreach (var id in new[] { "u1", "u2", "u3" })
        {
            var ilk = e.Find($"#flp-satir-{id} > td");
            Assert.Contains("flp-urun", ilk.ClassList);
            Assert.NotNull(ilk.QuerySelector($"#flp-ozel-{id}"));
            Assert.NotNull(ilk.QuerySelector($"#flp-sec-{id}"));
            Assert.Equal(basliklar.Count, e.FindAll($"#flp-satir-{id} > td").Count);
        }
        Assert.NotNull(e.Find("#flp-ozel-u1 > span.satir-ici > svg"));

        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", "BASIT", "BASIT"));
        var b = Ciz();
        var basit = b.FindAll("#flp-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(8, basit.Count);
        Assert.DoesNotContain("Kaynak", basit);
        Assert.DoesNotContain(basit, x => x.StartsWith("İşçilik", StringComparison.Ordinal));
        Assert.Equal(8, b.FindAll("#flp-satir-u1 > td").Count);
        Assert.Empty(b.FindAll("[id^='flp-ozel-']"));
        Assert.Empty(b.FindAll("[id^='flp-kaynak-']"));
    }

    /// <summary>Kabul #60/#61: ayar ADI görünür (ham K22/G925 yalnız title'da); kısa hücreler tek satır sınıfı taşır.</summary>
    [Fact]
    public void FiyatListesiUrunPaneli_ayar_adi()
    {
        var e = Ciz();
        var hucreler = e.FindAll("#flp-satir-u1 td");
        // PAKET GOC-C: tablo ortak ızgarada — ham kod title'ı hücrenin içindeki ad öğesinde (ızgara hücre özniteliği satıra göre değişmez).
        var ayar = e.Find("#flp-satir-u1 [title='K22']");
        Assert.Equal("22 Ayar", ayar.TextContent.Trim());
        Assert.Equal("925 Gümüş", e.Find("#flp-satir-u2 [title='G925']").TextContent.Trim());
        foreach (var satir in e.FindAll("tr[id^='flp-satir-'] td"))
            Assert.DoesNotMatch(new Regex(@"^(K22|K24|G925)$"), satir.TextContent.Trim());
        Assert.All(hucreler.Where(h => h.ClassList.Contains("sayi") || h.ClassList.Contains("metin-kucuk")),
            h => Assert.Contains("hucre-tek-satir", h.ClassList));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #30 · #31 · #5 — /pos
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1PosTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public KesifP1PosTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<PosServisi>();
    }

    private const string Cihaz =
        """{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}""";

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            var detay = $$"""{"cihaz":{{Cihaz}},"taksitOranlari":[{"id":"o1","posCihazId":"p1","taksitAdedi":3,"komisyonOran":0.0275,"valorGun":null,"gecerliBaslangic":"2026-09-01T00:00:00Z","aktif":true}]}""";
            return yol switch
            {
                var y when y.EndsWith("/pos/cihazlar") && get => P1Ortak.Json($"[{Cihaz}]"),
                var y when y.EndsWith("/pos/cihazlar/p1") && get => P1Ortak.Json(detay),
                var y when y.EndsWith("/taksit-oranlari") || y.Contains("/taksit-oranlari/") => P1Ortak.Json(detay),
                var y when y.EndsWith("/pos/cihazlar") => P1Ortak.Json(detay),
                var y when y.EndsWith("/kasalar") => P1Ortak.Json("""[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]"""),
                _ => P1Ortak.Json("{}"),
            };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Yazmalar(string parca)
    { lock (istekler) return [.. istekler.Where(i => i.Metot != "GET" && i.Yol.Contains(parca))]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Pos> YeniCihaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-yeni-ac").Click();
        e.WaitForElement("#pos-cihaz-ad").Input("Yapı Kredi POS");
        e.Find("#pos-cihaz-banka").Input("Yapı Kredi");
        return e;
    }

    /// <summary>Kabul #31: komisyon YÜZDE girilir — '1,85' → gövdede 0,0185; tabloda "%1,85" (tr-TR).</summary>
    [Fact]
    public void Pos_komisyon_yuzde_govdeye_kesir()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-oran").Input("1,85");
        e.Find("#pos-cihaz-valor").Input("2");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/pos/cihazlar")));
        var g = P1Ortak.Govde(Yazmalar("/pos/cihazlar")[0].Govde);
        Assert.Equal(0.0185m, g.GetProperty("pesinKomisyonOran").GetDecimal());
        Assert.Equal(2, g.GetProperty("valorGun").GetInt32());
        Assert.Contains("%1,85", e.Markup);
        Assert.DoesNotContain("1.85 %", e.Markup);
    }

    /// <summary>Kabul #5/#31: valör '1,5' → alan hatası, Kaydet pasif, istek 0 (eskiden sessizce 1).</summary>
    [Fact]
    public void Pos_valor_kesirli_reddedilir()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-oran").Input("1,85");
        e.Find("#pos-cihaz-valor").Input("1,5");
        e.WaitForAssertion(() => Assert.Contains(KesifP1Sozlugu.Tr.TamSayiHata, e.Find("#cekmece-pos-cihaz").TextContent));
        Assert.True(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled"));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        Assert.Empty(Yazmalar("/pos/cihazlar"));
    }

    /// <summary>Kabul #5: '1.500' (belirsiz) → kapı sebebi görünür, Kaydet pasif, istek 0.</summary>
    [Fact]
    public void Pos_belirsiz_sayi_kapi_sebebi_gorunur()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-valor").Input("1");
        e.Find("#pos-cihaz-oran").Input("1.500");
        e.WaitForAssertion(() => Assert.Contains(KesifP1Sozlugu.Tr.PosKomisyonYuzde, e.Find("#cekmece-pos-cihaz-kilit").TextContent));
        Assert.True(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled"));
        Assert.Empty(Yazmalar("/pos/cihazlar"));
    }

    /// <summary>Taksit: adet TAM SAYI, oran YÜZDE — '2,5' reddedilir; '6' + '2,75' → gövdede 6 ve 0,0275.</summary>
    [Fact]
    public void Pos_taksit_tam_sayi_ve_yuzde()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#pos-taksit-adet").Input("2,5");
        e.Find("#pos-taksit-oran").Input("2,75");
        e.WaitForAssertion(() => Assert.True(e.Find("#pos-taksit-ekle").HasAttribute("disabled")));
        Assert.NotEmpty(e.Find("#pos-taksit-ekle-kilit").TextContent.Trim());
        e.Find("#pos-taksit-ekle").Click();
        Assert.Empty(Yazmalar("/taksit-oranlari"));

        e.Find("#pos-taksit-adet").Input("6");
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-taksit-ekle").HasAttribute("disabled")));
        e.Find("#pos-taksit-ekle").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/taksit-oranlari")));
        var g = P1Ortak.Govde(Yazmalar("/taksit-oranlari")[0].Govde);
        Assert.Equal(6, g.GetProperty("taksitAdedi").GetInt32());
        Assert.Equal(0.0275m, g.GetProperty("komisyonOran").GetDecimal());
    }

    /// <summary>Kabul #30/#31: Pos.razor'da ham type="number" 0; taksit satırı üç sütunlu ızgarada değil.</summary>
    [Fact]
    public void Pos_type_number_yok()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Pos.razor");
        Assert.DoesNotContain("type=\"number\"", kaynak, StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#pos-taksit-adet");
        Assert.Empty(e.FindAll("input[type=number]"));
        Assert.Null(e.Find("#pos-taksit-ekle").Closest(".form-grid-3"));
        Assert.NotNull(e.Find("#pos-taksit-oran").Closest(".form-grid-2"));
    }

    /// <summary>
    /// Tur 2 (doğrulayıcı KRİTİK): yüzde en çok 2 ondalık — '1,855' (kesir 0,01855, kolon decimal(5,4))
    /// sessizce 0,0186 olarak KAYDEDİLMEZ: alan altında hata, Kaydet pasif, istek 0; '1,85' kabul.
    /// </summary>
    [Fact]
    public void Pos_komisyon_uc_ondalik_reddedilir()
    {
        var e = YeniCihaz();
        e.Find("#pos-cihaz-valor").Input("1");
        e.Find("#pos-cihaz-oran").Input("1,855");
        var mesaj = string.Format(CultureInfo.InvariantCulture, KesifP1Sozlugu.Tr.OndalikSiniri, 2);
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Find("#cekmece-pos-cihaz").TextContent));
        Assert.True(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled"));
        Assert.Contains(KesifP1Sozlugu.Tr.PosKomisyonYuzde, e.Find("#cekmece-pos-cihaz-kilit").TextContent);
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        Assert.Empty(Yazmalar("/pos/cihazlar"));

        e.Find("#pos-cihaz-oran").Input("1,85");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-pos-cihaz-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-pos-cihaz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Yazmalar("/pos/cihazlar")));
        Assert.Equal(0.0185m, P1Ortak.Govde(Yazmalar("/pos/cihazlar")[0].Govde).GetProperty("pesinKomisyonOran").GetDecimal());
    }

    /// <summary>Tur 2: taksit oranı da en çok 2 ondalık — '2,755' reddedilir, istek 0.</summary>
    [Fact]
    public void Pos_taksit_oran_uc_ondalik_reddedilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#pos-taksit-adet").Input("6");
        e.Find("#pos-taksit-oran").Input("2,755");
        e.WaitForAssertion(() => Assert.True(e.Find("#pos-taksit-ekle").HasAttribute("disabled")));
        Assert.Contains(KesifP1Sozlugu.Tr.PosKomisyonYuzde, e.Find("#pos-taksit-ekle-kilit").TextContent);
        e.Find("#pos-taksit-ekle").Click();
        Assert.Empty(Yazmalar("/taksit-oranlari"));
    }

    /// <summary>Tur 2: satır içi taksit oranı '3,555' → hata bandı, PATCH 0.</summary>
    [Fact]
    public void Pos_satir_ici_oran_uc_ondalik_istek_atmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#si-pos-taksit-oran-o1-ac").Click();
        e.Find("#si-pos-taksit-oran-o1").Input("3,555");
        e.Find("#si-pos-taksit-oran-o1-kaydet").Click();
        var mesaj = string.Format(CultureInfo.InvariantCulture, KesifP1Sozlugu.Tr.OndalikSiniri, 2);
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Markup));
        Assert.Empty(Yazmalar("/taksit-oranlari/"));
    }

    /// <summary>Taksit oranları tablosu (satır içi düzenleme) YÜZDE okunur: 0,0275 → "2,75", kesir değil.</summary>
    /// <summary>
    /// Tur 3 (orkestratör 18:10): cihaz tablosu 5 sütun; ad ve banka TEK hücrede ve tek satıra
    /// ZORLANMAZ (hucre-tek-satir yok, en çok genişlik kesif-p1.css'te); valör komisyonun alt satırında;
    /// eylem kümesi son sütunda .satir-eylem (769–1399 yapışık) ve q10-yigin-eylem; 375 px kart yığını
    /// (q10-yigin) için her veri hücresi data-etiket taşır.
    /// </summary>
    [Fact]
    public void Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1");
        var tablo = e.Find("#pos-cihaz-tablo");
        Assert.Contains("q10-yigin", tablo.ClassList);
        Assert.Equal(5, e.FindAll("#pos-cihaz-tablo thead th").Count);
        var tdler = e.FindAll("#pos-satir-p1 > td");
        Assert.Equal(5, tdler.Count);
        var ad = e.Find("#pos-ad-p1");
        Assert.DoesNotContain("hucre-tek-satir", ad.ClassList);
        Assert.Empty(ad.QuerySelectorAll(".hucre-tek-satir"));
        Assert.Contains("Garanti POS", ad.TextContent);
        Assert.Equal("Garanti", e.Find("#pos-banka-p1").TextContent.Trim());
        Assert.Equal("%1,85", e.Find("#pos-oran-p1").TextContent.Trim());
        Assert.Equal(string.Format(KesifP1Sozlugu.Tr.PosValorAlt, 1), e.Find("#pos-valor-p1").TextContent.Trim());
        var eylem = tdler[^1];
        Assert.Contains("q10-yigin-eylem", eylem.ClassList);
        var kume = Assert.Single(eylem.Children, c => c.ClassList.Contains("satir-eylem"));
        Assert.NotNull(kume.QuerySelector("#pos-duzenle-p1"));
        Assert.NotNull(kume.QuerySelector("#pos-pasif-p1"));
        Assert.All(tdler.Take(4), t => Assert.False(string.IsNullOrWhiteSpace(t.GetAttribute("data-etiket"))));
    }

    [Fact]
    public void Pos_taksit_tablosu_yuzde_gosterir()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForAssertion(() => Assert.Contains("2,75", e.Find("#cekmece-pos-cihaz").TextContent));
        Assert.DoesNotContain("0,0275", e.Find("#cekmece-pos-cihaz").TextContent);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Sayı kutusu — TamSayi parametresi (saf bileşen)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1SayiTestleri : BunitContext
{
    public KesifP1SayiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    [Fact]
    public void Sayi_TamSayi_kesirliyi_reddeder_tam_sayiyi_kabul_eder()
    {
        decimal? son = 5m;
        var e = Render<Kuyumcu.Web.Bilesenler.Form.Sayi>(p => p
            .Add(x => x.ElemanId, "t-gun").Add(x => x.Etiket, "Gün").Add(x => x.TamSayi, true)
            .Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.Deger, son).Add(x => x.DegerChanged, (decimal? v) => son = v));
        Assert.Equal("numeric", e.Find("#t-gun").GetAttribute("inputmode"));
        e.Find("#t-gun").Input("1,5");
        Assert.Null(son);
        Assert.Contains(KesifP1Sozlugu.Tr.TamSayiHata, e.Markup);
        e.Find("#t-gun").Input("3");
        Assert.Equal(3m, son);
        Assert.DoesNotContain(KesifP1Sozlugu.Tr.TamSayiHata, e.Markup);
    }

    /// <summary>Tur 2: EnCokOndalik=2 → '1,855' null + hata; '1,85' ve '1,850' kabul.</summary>
    [Fact]
    public void Sayi_EnCokOndalik_fazla_haneyi_reddeder()
    {
        decimal? son = 5m;
        var e = Render<Kuyumcu.Web.Bilesenler.Form.Sayi>(p => p
            .Add(x => x.ElemanId, "t-yuzde").Add(x => x.Etiket, "Yüzde").Add(x => x.EnCokOndalik, 2)
            .Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.Deger, son).Add(x => x.DegerChanged, (decimal? v) => son = v));
        var mesaj = string.Format(CultureInfo.InvariantCulture, KesifP1Sozlugu.Tr.OndalikSiniri, 2);
        e.Find("#t-yuzde").Input("1,855");
        Assert.Null(son);
        Assert.Contains(mesaj, e.Markup);
        e.Find("#t-yuzde").Input("1,85");
        Assert.Equal(1.85m, son);
        Assert.DoesNotContain(mesaj, e.Markup);
        e.Find("#t-yuzde").Input("1,850");
        Assert.Equal(1.85m, son);
    }

    [Fact]
    public void Sayi_TamSayi_kapali_iken_davranis_degismez()
    {
        decimal? son = null;
        var e = Render<Kuyumcu.Web.Bilesenler.Form.Sayi>(p => p
            .Add(x => x.ElemanId, "t-oran").Add(x => x.Etiket, "Oran")
            .Add(x => x.Deger, son).Add(x => x.DegerChanged, (decimal? v) => son = v));
        Assert.Equal("decimal", e.Find("#t-oran").GetAttribute("inputmode"));
        e.Find("#t-oran").Input("1,5");
        Assert.Equal(1.5m, son);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #72 — sunucu 403 metni: rol KODU değil rol ADI (RolFiltresi.cs)
// ─────────────────────────────────────────────────────────────────────────────
[Collection("http")]
public sealed class KesifP1HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static readonly Regex HamRol = new(@"\b(SAHIP|MUDUR|MUHASEBE|TEZGAHTAR)\b");

    [Fact]
    public async Task RolFiltresi_mesajinda_ham_rol_kodu_yok()
    {
        var k = YeniKiraci("PAKETKESIF-P1-HTTP rol metni");

        // Tek rol: MÜDÜR → SAHİP ucu.
        var c1 = await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/admin/tenant");
        Assert.Equal(HttpStatusCode.Forbidden, c1.StatusCode);
        var m1 = JsonDocument.Parse(await c1.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;
        Assert.Equal("Bu işlem için gerekli rol: işletme sahibi", m1);
        Assert.DoesNotMatch(HamRol, m1);

        // Çok rol: TEZGÂHTAR → [RolGerekli("SAHIP","MUDUR")] ucu. Modül kapısı önce keser —
        // TEZGÂHTAR'a "kullanici" modülü verilir ki RolFiltresi'nin metni ölçülsün.
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Kuyumcu.Api.Modules.Yetki.Moduller.Tumu],
            ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor"],
            ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "kullanici"],
        };
        var y = await f.Ortam.Istemci(k, "SAHIP").PutAsync("/api/v1/yetkiler",
            new StringContent(JsonSerializer.Serialize(new { matris }), System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var c2 = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsync("/api/v1/kullanicilar",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Forbidden, c2.StatusCode);
        var m2 = JsonDocument.Parse(await c2.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;
        Assert.Equal("Bu işlem için gerekli rol: işletme sahibi ya da müdür", m2);
        Assert.DoesNotMatch(HamRol, m2);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Tur 3 · şerit stil dosyası kesif-p1.css (saf dosya ölçüsü)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class KesifP1CssTestleri
{
    private static List<CssKaynak.Bildirim> B() =>
        CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "kesif-p1.css"));

    private static string Deger(string seciciParca, string ozellik, string? atKural = null) =>
        B().Single(b => Regex.Replace(b.Secici, @"\s+", " ").Contains(seciciParca, StringComparison.Ordinal)
                        && b.Ozellik == ozellik && (atKural is null ? string.IsNullOrWhiteSpace(b.AtKural) : b.AtKural.Contains(atKural, StringComparison.Ordinal))).Deger;

    /// <summary>index.html dosyayı yükler; olcek.css'ten ÖNCE (OL5: olcek → dugme → ortak sırası bozulmaz).</summary>
    [Fact]
    public void KesifP1_css_olcekten_once_yuklenir()
    {
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html");
        var linkler = Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
        var i = linkler.IndexOf("css/kesif-p1.css");
        Assert.True(i > linkler.IndexOf("css/app.css"), "kesif-p1.css app.css'ten sonra yüklenmeli");
        Assert.True(i < linkler.IndexOf("css/olcek.css"), "kesif-p1.css olcek.css'ten önce yüklenmeli");
    }

    /// <summary>
    /// POS: ad hücresinin EN ÇOK genişliği var ve tek satıra zorlanmaz; fiyat paneli ilk sütunu sola yapışık
    /// (opak zemin belirteçten); Görünüm kartı işaret satırı SARMAZ ve ad–işaret arası ≥ 4 px (--bosluk-1).
    /// Sabit renk yok.
    /// </summary>
    [Fact]
    public void KesifP1_css_kurallari_yerinde_sabit_renk_yok()
    {
        Assert.Equal("16rem", Deger("#pos-cihaz-tablo td[id^=\"pos-ad-\"]", "max-width"));
        Assert.Equal("anywhere", Deger("#pos-cihaz-tablo td[id^=\"pos-ad-\"]", "overflow-wrap"));
        Assert.DoesNotContain(B(), b => b.Secici.Contains("pos-ad-", StringComparison.Ordinal) && b.Ozellik == "white-space");
        Assert.Equal("sticky", Deger("#flp-tablo td.flp-urun", "position"));
        Assert.Equal("0", Deger("#flp-tablo td.flp-urun", "left"));
        Assert.Contains("var(--yuzey)", Deger("#flp-tablo td.flp-urun", "box-shadow"));
        Assert.Equal("nowrap", Deger(".gk .gk-sema-alt:has(> .gk-isaret)", "flex-wrap"));
        Assert.Equal("var(--bosluk-1)", Deger(".gk .gk-sema-alt:has(> .gk-isaret)", "column-gap"));
        Assert.Equal("auto", Deger(".gk .gk-isaret", "margin-inline-start"));
        var bosluk1 = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css"))
            .First(b => b.Ozellik == "--bosluk-1").Deger;
        Assert.Equal(".25rem", bosluk1);                                     // 4 px ≥ 4 px
        Assert.DoesNotContain(B(), b => Regex.IsMatch(b.Deger, "#[0-9a-fA-F]{3,8}\\b|rgb\\(|hsl\\("));
        Assert.DoesNotContain(B(), b => b.Secici.Contains("data-mod=", StringComparison.Ordinal));
    }
}
