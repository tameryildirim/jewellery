using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-h — GÖRSEL SIĞMA 1 (kurucu 24.09 08:30: "yer yok gibi dibine
// girmişsin, dar dar" · "tren gibi aşağıya doğru gidiyor, tab tab ayır")
//
// Kilitler:
//   (1) Türkçe dil verisi: Kuyumcu.Web.csproj BlazorWebAssemblyLoadAllGlobalizationData
//       = true (İngilizce tarayıcıda "TRY6,754.91" çıkıyordu — ICU parçası tr içermez).
//   (2) Ortak Sekmeler: role=tablist/tab/tabpanel, aria-selected, aria-controls,
//       dolaşan tabindex, ← → Home End, ?sekme= derin bağlantı + tarih kaydı.
//   (3) /ayarlar sekmeli: bölümler taşındı, id'ler AYNI, dar sütun yok, emoji yok,
//       ham rol kodu yok.
//   (4) Karo adı harf ortasından bölünmez (CSS kilidi — gerçek ölçüm tarayıcıda).
//   (5) Altın pozisyonu "dışarıda" satırı: parçalar blok (flex) öğe → binme 0.
// bUnit CSS uygulamaz: görünürlük/sığma/binme ölçümü rapordaki tarayıcı tablosudur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1hGorselSigmaTestleri : BunitContext
{
    private readonly List<string> istekler = [];

    public S1hGorselSigmaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<SurumServisi>();
    }

    private sealed class SahteApi(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add($"{istek.Method.Method} {yol}");
            // ⚠ bUnit sözleşmesi: her uca GERÇEKÇİ yanıt.
            var g = yol switch
            {
                var x when x.EndsWith("/destek/paket/gecmis") =>
                    """{"kanalAcik":true,"sinir":50,"dahaFazlasiVar":false,"asgariGerekce":20,"konular":["KURULUM","HATA","DIGER"],"kayitlar":[]}""",
                var x when x.EndsWith("/meta/surum") =>
                    """{"surum":"1.0.0","derlemeZamani":"2026-09-24T06:04:00Z","derlemeZamaniKaynagi":"damga","ortam":"Production","migrationSayisi":71,"sonMigration":"20260924004513_P77","bekleyenMigrationSayisi":0}""",
                var x when x.Contains("/mfa/durum") =>
                    """{"etkin":false,"kurulumBekliyor":false,"kilitli":false,"kalanKurtarma":0}""",
                var x when x.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var x when x.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var x when x.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                var x when x.EndsWith("/pozisyon", StringComparison.Ordinal) => PozisyonYaniti,
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private const string PozisyonYaniti = """
    {"anaMaden":"ALTIN",
     "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":36.4,"cariBorcGram":22.0,
              "netGram":312.8,"alacakliCariSayisi":3,"borcluOlduguCariSayisi":1,"konsinyeGram":0,"emanetGram":16.9},
     "digerMadenler":[],"madenlerToplanmaz":true,"defterBakiyeFarki":0,
     "hesaplanmaAni":"2026-09-24T09:00:00Z","donemdenBagimsiz":true}
    """;

    private static string Oku(params string[] yol) => File.ReadAllText(Path.Combine([CssKaynak.Kok(), .. yol]));

    private static string S1hBlogu()
    {
        var css = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var i = css.IndexOf("PAKET S1-h — GÖRSEL SIĞMA 1", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-h bloğu yok.");
        return css[i..];
    }

    // ── (1) Türkçe dil verisi ────────────────────────────────────────────────
    [Fact]
    public void S1h_01_csproj_TUM_dil_verisini_yukler_Ingilizce_tarayicida_TRY_cikmaz()
    {
        var csproj = Oku("Kuyumcu.Web", "Kuyumcu.Web.csproj");
        Assert.Matches(@"<BlazorWebAssemblyLoadAllGlobalizationData>\s*true\s*</BlazorWebAssemblyLoadAllGlobalizationData>", csproj);
        // Değişmez kültür modu tr-TR verisini tamamen kapatır — açılmamalı.
        Assert.DoesNotMatch(@"<InvariantGlobalization>\s*true", csproj);
        Assert.DoesNotMatch(@"<BlazorIcuDataFileName>", csproj);
    }

    // ── (2) Ortak sekme bileşeni ─────────────────────────────────────────────
    private static readonly SekmeTanimi[] UcSekme =
    [
        new("a", "Birinci"), new("b", "İkinci"), new("c", "Üçüncü"),
    ];

    private IRenderedComponent<Sekmeler> SekmeCiz(string uri = "/deneme")
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        return Render<Sekmeler>(p => p
            .Add(x => x.Liste, UcSekme)
            .Add(x => x.Onek, "t")
            .Add(x => x.Etiket, "Deneme bölümleri")
            .Add(x => x.Panel, kod => $"<p id=\"icerik-{kod}\">{kod}</p>"));
    }

    [Fact]
    public void S1h_02_Sekmeler_ARIA_rolleri_ve_aria_selected_tek_secili()
    {
        var c = SekmeCiz();
        var serit = c.Find("[role=tablist]");
        Assert.Equal("Deneme bölümleri", serit.GetAttribute("aria-label"));
        var sekmeler = c.FindAll("[role=tab]");
        Assert.Equal(3, sekmeler.Count);
        Assert.Equal("true,false,false", string.Join(",", sekmeler.Select(s => s.GetAttribute("aria-selected"))));
        Assert.Equal("0,-1,-1", string.Join(",", sekmeler.Select(s => s.GetAttribute("tabindex"))));
        foreach (var s in sekmeler)
        {
            var panel = c.Find($"#{s.GetAttribute("aria-controls")}");
            Assert.Equal("tabpanel", panel.GetAttribute("role"));
            Assert.Equal(s.Id, panel.GetAttribute("aria-labelledby"));
        }
        // TÜM paneller DOM'da (özellik silinmez), seçili olmayan hidden.
        Assert.False(c.Find("#t-panel-a").HasAttribute("hidden"));
        Assert.True(c.Find("#t-panel-b").HasAttribute("hidden"));
        Assert.True(c.Find("#t-panel-c").HasAttribute("hidden"));
        Assert.NotNull(c.Find("#icerik-c"));
    }

    [Fact]
    public void S1h_03_Sekmeler_tiklama_URLye_sekme_yazar_ve_TARIH_KAYDI_acar()
    {
        var c = SekmeCiz();
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var once = nav.History.Count;

        c.Find("#t-sekme-b").Click();

        Assert.EndsWith("/deneme?sekme=b", nav.Uri);
        Assert.Equal(once + 1, nav.History.Count);
        Assert.False(nav.History.First().Options.ReplaceHistoryEntry); // geri tuşu önceki sekmeye döner
        c.WaitForAssertion(() => Assert.Equal("true", c.Find("#t-sekme-b").GetAttribute("aria-selected")));
        Assert.Equal("false", c.Find("#t-sekme-a").GetAttribute("aria-selected"));
        Assert.True(c.Find("#t-panel-a").HasAttribute("hidden"));
        Assert.False(c.Find("#t-panel-b").HasAttribute("hidden"));
    }

    [Fact]
    public void S1h_04_Sekmeler_derin_baglanti_ve_GERI_gibi_URL_degisimi_secimi_izler()
    {
        var c = SekmeCiz("/deneme?sekme=c");
        Assert.Equal("true", c.Find("#t-sekme-c").GetAttribute("aria-selected"));
        Assert.False(c.Find("#t-panel-c").HasAttribute("hidden"));

        // Tarayıcının geri tuşu = konum değişimi; bileşen URL'yi izler.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/deneme?sekme=a");
        c.WaitForAssertion(() => Assert.Equal("true", c.Find("#t-sekme-a").GetAttribute("aria-selected")));
        Assert.True(c.Find("#t-panel-c").HasAttribute("hidden"));

        // Bilinmeyen kod TAHMİN EDİLMEZ → ilk sekme.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/deneme?sekme=yok");
        c.WaitForAssertion(() => Assert.Equal("true", c.Find("#t-sekme-a").GetAttribute("aria-selected")));
    }

    [Fact]
    public void S1h_05_Sekmeler_klavye_ok_Home_End_ve_odak()
    {
        var c = SekmeCiz();
        string Secili() => c.FindAll("[role=tab]").Single(s => s.GetAttribute("aria-selected") == "true").Id ?? "";

        c.Find("#t-sekme-a").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        c.WaitForAssertion(() => Assert.Equal("t-sekme-b", Secili()));
        Assert.Equal("0", c.Find("#t-sekme-b").GetAttribute("tabindex"));
        Assert.Equal("-1", c.Find("#t-sekme-a").GetAttribute("tabindex"));

        c.Find("#t-sekme-b").KeyDown(new KeyboardEventArgs { Key = "End" });
        c.WaitForAssertion(() => Assert.Equal("t-sekme-c", Secili()));

        c.Find("#t-sekme-c").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" }); // sona sarma
        c.WaitForAssertion(() => Assert.Equal("t-sekme-a", Secili()));

        c.Find("#t-sekme-a").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" }); // başa sarma
        c.WaitForAssertion(() => Assert.Equal("t-sekme-c", Secili()));

        c.Find("#t-sekme-c").KeyDown(new KeyboardEventArgs { Key = "Home" });
        c.WaitForAssertion(() => Assert.Equal("t-sekme-a", Secili()));

        // Başka tuş sekme değiştirmez.
        c.Find("#t-sekme-a").KeyDown(new KeyboardEventArgs { Key = "x" });
        Assert.Equal("t-sekme-a", Secili());

        // Odak seçilen sekmeye taşınır (dolaşan tabindex kalıbı).
        Assert.Contains(JSInterop.Invocations, i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    // ── (3) /ayarlar sekmeli ─────────────────────────────────────────────────
    private IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> AyarCiz(string rol = "SAHIP", string uri = "/ayarlar")
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1"));
        return Render<Kuyumcu.Web.Pages.Ayarlar>();
    }

    // PAKET T-K3 (tuval Ayarlar.dc.html): sekmeler tuvaldeki ad ve sırayla —
    // Görünüm · Dükkân · Kur · Yazıcı · Güvenlik; S1-h'nin Hesap/Sistem/Destek
    // bölümleri Güvenlik sekmesine taşındı (id'leri aynı).
    [Fact]
    public void S1h_06_Ayarlar_bolumleri_sekmelere_ayrilir_TUVAL_sirasi_bes_sekme()
    {
        var sahip = AyarCiz();
        Assert.Equal(["ayar-sekme-gorunum", "ayar-sekme-dukkan", "ayar-sekme-kur", "ayar-sekme-yazici", "ayar-sekme-guvenlik"],
            sahip.FindAll("#ayar-sekme-seridi [role=tab]").Select(s => s.Id ?? "").ToArray());
        Assert.Equal(["Görünüm", "İşletme", "Kur", "Yazıcı", "Güvenlik"],
            sahip.FindAll("[role=tab]").Select(s => s.TextContent.Trim()).ToArray());
    }

    [Fact]
    public void S1h_07_Ayarlar_SAHIP_olmayan_ayni_bes_sekmeyi_gorur_destek_karti_yok()
    {
        var c = AyarCiz("TEZGAHTAR");
        Assert.Equal(5, c.FindAll("[role=tab]").Count);
        Assert.Empty(c.FindAll("#ayar-sekme-destek"));
        Assert.Empty(c.FindAll("#ayar-destek"));
    }

    [Fact]
    public void S1h_08_Ayarlar_tasinan_bolumler_YASAR_idler_ayni_ve_dogru_panelde()
    {
        var c = AyarCiz();
        (string Id, string Panel)[] beklenen =
        [
            // KESIF-P1 · TEMA-TEK: "ayar-tema" (cihaz tema kartı) bilerek kaldırıldı.
            ("ayar-gorunum-karti", "gorunum"), ("ayar-gorunum-ayrinti", "gorunum"),
            ("ayar-benim-gorunum", "gorunum"), ("ayar-arayuz-modu", "gorunum"), ("ayar-dil", "gorunum"),
            ("ayar-gorunum", "dukkan"),
            ("ay-adsoyad", "guvenlik"), ("ay-eposta", "guvenlik"), ("ay-rol", "guvenlik"), ("ay-profil-duzenle", "guvenlik"),
            ("ayar-mfa", "guvenlik"), ("ay-sifre-guncelle", "guvenlik"),
            ("ayar-kunye", "guvenlik"),
            ("ayar-destek", "guvenlik"),
            ("ayar-panel-govde-hesap", "guvenlik"), ("ayar-panel-govde-sistem", "guvenlik"), ("ayar-panel-govde-destek", "guvenlik"),
        ];
        foreach (var (id, panel) in beklenen)
        {
            var e = c.Find($"#{id}");
            Assert.NotNull(e.Closest($"#ayar-panel-{panel}"));
        }
        // T-K3: "Profili düzenle" profil kartının başında (sayfa başı bloğu kalktı).
        Assert.NotNull(c.Find("#ayar-profil #ay-profil-duzenle"));
        // T-K3: dil seçimi Görünüm sekmesinde.
        Assert.NotNull(c.Find("#ayar-panel-gorunum #ayar-dil .bayrak"));
    }

    [Fact]
    public void S1h_09_Ayarlar_derin_baglanti_sekme_guvenlik_acik_digerleri_gizli()
    {
        var c = AyarCiz(uri: "/ayarlar?sekme=guvenlik");
        Assert.Equal("true", c.Find("#ayar-sekme-guvenlik").GetAttribute("aria-selected"));
        Assert.False(c.Find("#ayar-panel-guvenlik").HasAttribute("hidden"));
        foreach (var p in new[] { "gorunum", "dukkan", "kur", "yazici" })
            Assert.True(c.Find($"#ayar-panel-{p}").HasAttribute("hidden"), p);
    }

    [Fact]
    public void S1h_10_Ayarlar_emoji_yok_ham_rol_kodu_yok_dar_sutun_yok()
    {
        var c = AyarCiz();
        var m = c.Markup;
        foreach (var emoji in new[] { "✏️", "🏢", "🟢", "⚙️", "🇹🇷", "🇬🇧" })
            Assert.DoesNotContain(emoji, m);
        Assert.NotNull(c.Find("#ay-profil-duzenle svg"));   // Ikon bileşeni
        Assert.Equal("Sahip", c.Find("#ay-rol").TextContent.Trim()); // UX-S31
        Assert.Empty(c.FindAll(".ayarlar-sutun"));           // 32rem dar sütun kalktı
        Assert.NotNull(c.Find(".ayarlar-sayfa"));
    }

    // ── (4) CSS kilitleri ────────────────────────────────────────────────────
    [Fact]
    public void S1h_11_CSS_karo_ve_kisa_etiket_HARF_ORTASINDAN_bolunmez()
    {
        var app = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var bil = CssKaynak.Bildirimler(app).ToList();
        string[] kisaEtiket = [".ekran-karo-ad", ".gk-sema-ad", ".sekmeler-sekme", ".gk-bolum-baslik", ".gk-piksel", ".gk-mod-ad"];
        // Kaskat: aynı @-kural + seçici + özellik için SON bildirim kazanır. Önceki
        // paketlerin (S1-d `.ekran-karo-ad { word-break: break-word }`) kuralı
        // dosyada durur ama S1-h bloğu onu ezer — kilit ETKİN değeri sınar.
        var etkin = bil.GroupBy(b => (b.AtKural, b.Secici, b.Ozellik)).Select(g => g.Last()).ToList();
        var ihlal = etkin.Where(b => kisaEtiket.Any(s => b.Secici.Contains(s, StringComparison.Ordinal))
                                   && ((b.Ozellik == "word-break" && (b.Deger.Contains("break-all") || b.Deger.Contains("break-word")))
                                       || (b.Ozellik == "overflow-wrap" && b.Deger.Contains("anywhere"))))
                       .Select(b => $"{b.AtKural} {b.Secici} {{ {b.Ozellik}: {b.Deger} }}").ToList();
        Assert.True(ihlal.Count == 0, "Kısa etiket harf ortasından bölünüyor: " + string.Join(" · ", ihlal));

        var blok = S1hBlogu();
        Assert.Matches(@"\.ekran-karo-ad\s*\{[^}]*word-break:\s*normal;", blok);
        Assert.Matches(@"\.ekranlar-izgara\s*\{\s*grid-template-columns:\s*repeat\(auto-fill,\s*minmax\(14rem,\s*1fr\)\)", blok);
        Assert.Matches(@"\.islem-izgara-karolar\s*\{\s*grid-template-columns:\s*repeat\(auto-fill", blok);
    }

    [Fact]
    public void S1h_12_CSS_blogu_belirtecle_Ayarlar_genis_sekme_hap_pozisyon_flex()
    {
        var blok = Regex.Replace(S1hBlogu(), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", blok);
        Assert.DoesNotMatch(@"\.ayarlar-sayfa\s*\{[^}]*max-width", blok);
        Assert.Matches(@"\.ayar-ikili\s*\{[^}]*grid-template-columns:\s*repeat\(2,\s*minmax\(0,\s*1fr\)\)", blok);
        Assert.Matches(@"\.sekmeler-sekme\.secili\s*\{[^}]*var\(--vurgu\)", blok);
        Assert.Matches(@"\.sekmeler-sekme:focus-visible\s*\{[^}]*var\(--odak-halka\)", blok);
        Assert.Matches(@"\.pozisyon-disarida\s*\{[^}]*display:\s*flex;[^}]*flex-wrap:\s*wrap", blok);
        // Ayarlar.razor artık 32rem'lik dar sütun sınıfını kullanmaz.
        Assert.DoesNotContain("ayarlar-sutun", Oku("Kuyumcu.Web", "Pages", "Ayarlar.razor"));
    }

    // ── (5) Altın pozisyonu "dışarıda" satırı ────────────────────────────────
    [Fact]
    public void S1h_13_Altin_pozisyonu_disarida_satiri_blok_ogeli_kapta()
    {
        var c = Render<AltinPozisyonuKarti>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pozisyon-emanet")));
        var p = c.Find("#pozisyon-disarida");
        Assert.Contains("pozisyon-disarida", p.ClassList);
        Assert.Contains("16,9", c.Find("#pozisyon-emanet").TextContent);
    }

    // ── Sözlük: iki dil aynı anahtarlar (konumsal record — derleme de kilitler) ──
    [Fact]
    public void S1h_14_Sozluk_TR_EN_dolu_ve_farkli()
    {
        foreach (var m in new[] { AyarlarSekmeSozlugu.Tr, AyarlarSekmeSozlugu.En })
            foreach (var d in m.GetType().GetProperties().Select(x => (string?)x.GetValue(m)))
                Assert.False(string.IsNullOrWhiteSpace(d));
        Assert.Equal("Güvenlik", AyarlarSekmeSozlugu.Al("tr").Guvenlik);
        Assert.Equal("Security", AyarlarSekmeSozlugu.Al("en").Guvenlik);
    }
}
