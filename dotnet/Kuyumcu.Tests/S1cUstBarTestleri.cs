using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-c — ÜST BAR: KUR ŞERİDİ + YARDIM + BASİT/PROFESYONEL
//
// Tek doğruluk kaynağı: kurucunun onayladığı tuval
//   dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html <header>
//   "Panel · HAS · GRAM 22 · USD · 09:02 · ─── · Yardım · (Basit|Profesyonel)"
//   Profesyonel.dc.html: + EUR · "14:32 · Altınkaynak".
//
// ⚠ Sahte /kur/guncel yanıtı GERÇEKÇİDİR (bUnit '{}' tuzağı): sunucunun
//   KurSnapshotCevap alanları — kaynak + ayarFiyatlari[] — taşınır. K22'nin
//   gramFiyati BİLEREK has × 0,916'ya EŞİT DEĞİLDİR (5555): ekran bunu
//   gösteriyorsa değer sunucudan okunuyordur, istemcide hesaplanmıyordur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1cUstBarTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kurYaniti = TamKur("ALTINKAYNAK");
    private System.Net.HttpStatusCode kurDurum = System.Net.HttpStatusCode.OK;

    private static string TamKur(string kaynak) => $$"""
        {"id":"kur-1","tenantId":"t1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,
         "usdTry":41.95,"eurTry":49.10,"gbpTry":56.30,"kaynak":"{{kaynak}}",
         "olusturuldu":"2026-09-23T09:02:00Z",
         "ayarFiyatlari":[
           {"ayarId":"K14","label":"14 Ayar Altın","milyem":585,"gramFiyati":4023.05,"hasFiyati":6877},
           {"ayarId":"K22","label":"22 Ayar Altın","milyem":916,"gramFiyati":5555,"hasFiyati":6877},
           {"ayarId":"K24","label":"24 Ayar (Has)","milyem":1000,"gramFiyati":6877,"hasFiyati":6877}]}
        """;

    public S1cUstBarTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(S1cUstBarTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (sahip.istekler) sahip.istekler.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/kur/guncel") && sahip.kurDurum != System.Net.HttpStatusCode.OK)
                return new HttpResponseMessage(sahip.kurDurum)
                {
                    Content = new StringContent("""{"message":"Kur kaynağına ulaşılamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => sahip.kurYaniti,
                var y when y.EndsWith("/kur/guncelle") => TamKur("TRUNCGIL"),
                var y when y.EndsWith("/bildirimler/sayi") => """{"sayi":0,"enYuksekOnem":null}""",
                var y when y.EndsWith("/kullanicilar/ben") =>
                    """{"id":"u1","arayuzModu":"PROFESYONEL"}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":null,"etkinArayuzModu":"PROFESYONEL"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<UstBar> Ciz(string mod = "BASIT", string rol = "SAHIP", string rota = "/")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", mod, mod));
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        return Render<UstBar>();
    }

    private static List<string> SeritIdleri(IRenderedComponent<UstBar> e) =>
        e.FindAll(".kur-bandi .kur-oge").Select(o => o.Id ?? "").ToList();

    // ── Kur şeridi ─────────────────────────────────────────────────────────

    [Fact]
    public void Basit_modda_serit_TUVAL_SIRASIYLA_has_gram22_usd_cizer_EUR_yok()
    {
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-has")));

        Assert.Equal(new[] { "ust-kur-has", "ust-kur-gram22", "ust-kur-usd" }, SeritIdleri(e));
        Assert.Equal("Has", e.Find("#ust-kur-has .etiket").TextContent.Trim());
        Assert.Equal("Gram 22", e.Find("#ust-kur-gram22 .etiket").TextContent.Trim());
        Assert.Equal("USD", e.Find("#ust-kur-usd .etiket").TextContent.Trim());
        Assert.Equal(Bicim.TlTam(6877m), e.Find("#ust-kur-has .deger").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(41.95m), e.Find("#ust-kur-usd .deger").TextContent.Trim());
        Assert.Empty(e.FindAll("#ust-kur-eur"));
        Assert.Empty(e.FindAll("#ust-kur-kaynak"));
    }

    [Fact]
    public void Gram22_SUNUCUNUN_ayar_fiyatindan_gelir_istemcide_HESAPLANMAZ()
    {
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-gram22")));
        // Sunucu 5555 dedi; has × 0,916 = 6299,33 olurdu. Ekran sunucuyu söyler.
        Assert.Equal(Bicim.TlTam(5555m), e.Find("#ust-kur-gram22 .deger").TextContent.Trim());
        Assert.DoesNotContain(Bicim.TlTam(6877m * 0.916m), e.Markup);
        Assert.DoesNotContain(Bicim.TlKurus(6877m * 0.916m), e.Markup);
    }

    /// <summary>
    /// PAKET T-K1 — kur şeridi tuval biçimi (Panel.dc.html: "HAS 6.877 ₺ · GRAM 22
    /// 6.301 ₺ · USD 41,95 ₺"): HAS ve GRAM 22 TAM LİRAYA yuvarlanır, USD/EUR kuruş
    /// hep iki hane (sıfır da olsa). Tam değer ipucunda kuruşuyla kalır. Değerler
    /// BİLEREK kesirli: 6876,60 → 6.877 (yukarı), 5554,40 → 5.554 (aşağı), 42,00 → "42,00".
    /// Beklenenler LİTERAL — biçim sözleşmesinin kendisi sınanıyor.
    /// </summary>
    [Fact]
    public void TK1_kur_seridi_has_gram_TAM_lira_usd_eur_IKI_hane_ipucu_kurusuyla()
    {
        kurYaniti = """
            {"id":"kur-2","tenantId":"t1","altinHasFiyati":6876.60,"gramAltinFiyati":6912.40,
             "usdTry":42.00,"eurTry":49.1,"gbpTry":56.30,"kaynak":"ALTINKAYNAK",
             "olusturuldu":"2026-09-23T09:02:00Z",
             "ayarFiyatlari":[{"ayarId":"K22","label":"22 Ayar Altın","milyem":916,"gramFiyati":5554.40,"hasFiyati":6876.60}]}
            """;
        var e = Ciz("PROFESYONEL");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-eur")));

        Assert.Equal("6.877\u00a0₺", e.Find("#ust-kur-has .deger").TextContent.Trim());
        Assert.Equal("5.554\u00a0₺", e.Find("#ust-kur-gram22 .deger").TextContent.Trim());
        Assert.Equal("42,00\u00a0₺", e.Find("#ust-kur-usd .deger").TextContent.Trim());
        Assert.Equal("49,10\u00a0₺", e.Find("#ust-kur-eur .deger").TextContent.Trim());
        Assert.DoesNotContain("TRY", e.Find("#ust-kur-serit").TextContent);

        var ipucu = e.Find(".kur-bandi").GetAttribute("title") ?? "";
        Assert.Contains("6.876,60\u00a0₺", ipucu);
        Assert.Contains("5.554,40\u00a0₺", ipucu);
    }

    [Fact]
    public void Yanitta_K22_yoksa_Gram22_CIZILMEZ_tahmin_yok()
    {
        kurYaniti = """{"id":"kur-1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,"usdTry":41.95,"eurTry":49.10,"gbpTry":56.30}""";
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-has")));
        Assert.Empty(e.FindAll("#ust-kur-gram22"));
        Assert.Equal(new[] { "ust-kur-has", "ust-kur-usd" }, SeritIdleri(e));
    }

    [Fact]
    public void Profesyonel_modda_EUR_ve_KAYNAK_ADI_gorunur()
    {
        var e = Ciz("PROFESYONEL");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-eur")));

        Assert.Equal(new[] { "ust-kur-has", "ust-kur-gram22", "ust-kur-usd", "ust-kur-eur" }, SeritIdleri(e));
        Assert.Equal(Bicim.TlKurus(49.10m), e.Find("#ust-kur-eur .deger").TextContent.Trim());
        // Tuval: "14:32 · Altınkaynak" — ad SÖZLÜKTEN, ham kod (ALTINKAYNAK) basılmaz.
        var kaynak = e.Find("#ust-kur-kaynak").TextContent.Trim();
        Assert.Equal("· " + Sozlukler.Tr.Kur.KaynakAltinkaynak, kaynak);
        Assert.DoesNotContain("ALTINKAYNAK", e.Find("header.ust-bar").TextContent);
    }

    [Fact]
    public void Taninmayan_kaynak_kodu_TAHMIN_EDILMEZ_ham_kod_basilmaz()
    {
        kurYaniti = TamKur("YENIKAYNAK");
        var e = Ciz("PROFESYONEL");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-eur")));
        Assert.Empty(e.FindAll("#ust-kur-kaynak"));
        Assert.DoesNotContain("YENIKAYNAK", e.Markup);
    }

    [Fact]
    public void Gizlenen_kurlar_SILINMEZ_ipucunda_TAM_listelenir()
    {
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kur-bandi")));
        var ipucu = e.Find(".kur-bandi").GetAttribute("title") ?? "";

        Assert.Contains($"{Sozlukler.Tr.Kur.GramAltin}: {Bicim.TlKurus(6912.40m)}", ipucu);
        Assert.Contains($"EUR: {Bicim.TlKurus(49.10m)}", ipucu);
        Assert.Contains($"GBP: {Bicim.TlKurus(56.30m)}", ipucu);
        Assert.Contains($"Gram 22: {Bicim.TlKurus(5555m)}", ipucu);
    }

    [Fact]
    public void Saat_yalniz_SS_DD_tam_tarih_title_da()
    {
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-saat")));
        var saat = e.Find("#ust-kur-saat .deger").TextContent.Trim();
        Assert.Matches(@"^\d{2}:\d{2}$", saat);
        var title = e.Find("#ust-kur-saat").GetAttribute("title") ?? "";
        Assert.Matches(@"^Son başarılı sorgu: \d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}$", title);
        Assert.DoesNotContain("hatali", e.Find("#ust-kur-saat").ClassName ?? "");
    }

    [Fact]
    public void Kur_alinamazsa_serit_BOS_KALMAZ_hata_gorunur_UX_S28()
    {
        kurDurum = System.Net.HttpStatusCode.InternalServerError;
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-yok")));

        Assert.Equal(UstBarSozlugu.KurYok("tr"), e.Find("#ust-kur-yok").TextContent.Trim());
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#ust-kur-yok").GetAttribute("title")));
        Assert.Contains("hatali", e.Find("#ust-kur-saat").ClassName ?? "");
        Assert.StartsWith("Son sorgu başarısız", e.Find("#ust-kur-saat").GetAttribute("title") ?? "");
        Assert.Empty(e.FindAll(".kur-bandi"));
    }

    // ── Yardım ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/cariler", "/kilavuz.html#cariler")]
    [InlineData("/islemler", "/kilavuz.html#islemler")]
    [InlineData("/gider-pusulalari/01JABC/belge", "/kilavuz.html#giderpusulasi")]
    [InlineData("/", "/kilavuz.html#panel")]
    [InlineData("/denetim-izi", "/kilavuz.html")]
    public void Yardim_bulunulan_ekranin_kilavuz_bolumune_YENI_SEKMEDE_gider(string rota, string beklenen)
    {
        var e = Ciz("BASIT", rota: rota);
        var a = e.Find("#ust-yardim");
        Assert.Equal(beklenen, a.GetAttribute("href"));
        Assert.Equal("_blank", a.GetAttribute("target"));
        Assert.Contains("noopener", a.GetAttribute("rel") ?? "");
        Assert.Equal("Yardım", a.TextContent.Trim());
        Assert.False(string.IsNullOrWhiteSpace(a.GetAttribute("title")));
    }

    [Fact]
    public void Yardim_haritasi_IKI_YONLU_gercek_capaya_ve_gercek_rotaya_baglidir()
    {
        var kilavuz = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "wwwroot", "kilavuz.html"));
        var capalar = Regex.Matches(kilavuz, "<section id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToHashSet();
        var ilkKesimler = RotaBaslikTestleri.YansimayaGoreRotalar()
            .Select(r => r.Trim('/').Split('/')[0]).ToHashSet();

        Assert.NotEmpty(YardimBaglantisi.Harita);
        foreach (var (kesim, capa) in YardimBaglantisi.Harita)
        {
            Assert.True(capalar.Contains(capa), $"'{capa}' kilavuz.html'de <section id> değil — ölü çıpa.");
            Assert.True(ilkKesimler.Contains(kesim), $"'/{kesim}' gerçek bir @page rotası değil.");
        }
    }

    // ── Mod anahtarı (id sözleşmesi + her düğme istek atar) ──────────────────

    [Fact]
    public void Mod_hapi_id_leri_AYNEN_yasar_ve_YALNIZ_METIN_tasir()
    {
        var e = Ciz("BASIT");
        var hap = e.Find("#ust-mod-anahtar");
        Assert.Equal("group", hap.GetAttribute("role"));
        var basit = e.Find("#ust-mod-basit");
        var pro = e.Find("#ust-mod-profesyonel");
        Assert.Contains("secili", basit.ClassName ?? "");
        Assert.Equal("true", basit.GetAttribute("aria-pressed"));
        Assert.Equal("false", pro.GetAttribute("aria-pressed"));
        Assert.Equal(Sozlukler.Tr.Arayuz.Basit, basit.TextContent.Trim());
        Assert.Equal(Sozlukler.Tr.Arayuz.Profesyonel, pro.TextContent.Trim());
        Assert.Empty(hap.QuerySelectorAll("svg"));
    }

    [Fact]
    public void Profesyonel_e_basinca_PATCH_kullanicilar_ben_GIDER_ve_hap_doner()
    {
        var e = Ciz("BASIT");
        e.Find("#ust-mod-profesyonel").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/kullanicilar/ben")));
        var patch = istekler.Last(i => i.Metot == "PATCH" && i.Yol.EndsWith("/kullanicilar/ben"));
        using var doc = JsonDocument.Parse(patch.Govde);
        Assert.Equal("PROFESYONEL", doc.RootElement.GetProperty("arayuzModu").GetString());
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#ust-mod-profesyonel").ClassName ?? ""));
    }

    [Fact]
    public void Kur_guncelle_SAHIP_te_POST_atar_TEZGAHTAR_da_hic_cizilmez()
    {
        var e = Ciz("BASIT", "SAHIP");
        e.Find(".kur-guncelle-btn").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kur/guncelle")));

        var t = Ciz("BASIT", "TEZGAHTAR");
        Assert.Empty(t.FindAll(".kur-guncelle-btn"));
    }

    /// <summary>
    /// PAKET S1-g (kurucu bulgusu): kur şeridinin yanındaki ETİKETSİZ '⟳'
    /// düğmesi kalktı. Tuvalde (Profesyonel.dc.html satır 65) saat düz metindir;
    /// "kuru şimdi sor" özelliği yetkili için SAATİN KENDİSİDİR ve ne yaptığını
    /// aria-label + aria-describedby ile söyler. Yetkisiz rolde saat düz metin.
    /// </summary>
    [Fact]
    public void S1g_kur_guncelle_SAATIN_kendisidir_etiketsiz_ikon_YOK()
    {
        var e = Ciz("PROFESYONEL", "SAHIP");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-saat")));
        var saat = e.Find("#ust-kur-saat");

        Assert.Equal("button", saat.TagName, ignoreCase: true);
        Assert.Contains("kur-guncelle-btn", saat.ClassName ?? "");
        Assert.Contains(Sozlukler.Tr.Kur.Guncelle, saat.GetAttribute("aria-label") ?? "");
        Assert.Equal("ust-kur-guncelle-aciklama", saat.GetAttribute("aria-describedby"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#ust-kur-guncelle-aciklama").TextContent));
        Assert.DoesNotContain("⟳", e.Find("header.ust-bar").TextContent, StringComparison.Ordinal);

        var t = Ciz("PROFESYONEL", "TEZGAHTAR");
        t.WaitForAssertion(() => Assert.NotEmpty(t.FindAll("#ust-kur-saat")));
        Assert.Equal("span", t.Find("#ust-kur-saat").TagName, ignoreCase: true);
    }

    // ── Düzen sırası (tuval) + dil ─────────────────────────────────────────

    [Fact]
    public void Duzen_sirasi_TUVALDEKI_gibi_baslik_serit_sonra_yardim_ve_hap()
    {
        var e = Ciz("BASIT");
        var ust = e.Find("header.ust-bar");
        var cocuklar = ust.Children.Select(c => c.TagName.ToLowerInvariant() + "." + (c.ClassName ?? "")).ToList();
        var h1 = cocuklar.FindIndex(c => c.StartsWith("h1"));
        var serit = cocuklar.FindIndex(c => c.Contains("ust-kur-serit"));
        var sag = cocuklar.FindIndex(c => c.Contains("ust-sag"));
        Assert.True(h1 >= 0 && h1 < serit && serit < sag, string.Join(" | ", cocuklar));

        var sagKume = e.Find(".ust-sag").Children.Select(c => c.Id ?? "").Where(i => i.Length > 0).ToList();
        Assert.True(sagKume.IndexOf("ust-yardim") < sagKume.IndexOf("ust-mod-anahtar"), string.Join(",", sagKume));
    }

    [Fact]
    public async Task EN_dilinde_serit_ve_Yardim_INGILIZCE()
    {
        var e = Ciz("BASIT");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-has")));
        await e.InvokeAsync(() => Services.GetRequiredService<DilServisi>().DegistirAsync("en"));
        e.WaitForAssertion(() => Assert.Equal("Help", e.Find("#ust-yardim").TextContent.Trim()));
        Assert.Equal("Fine", e.Find("#ust-kur-has .etiket").TextContent.Trim());
        Assert.Equal("Gram 22k", e.Find("#ust-kur-gram22 .etiket").TextContent.Trim());
    }

    // ── Kaynak dosya kilitleri ─────────────────────────────────────────────

    [Fact]
    public void UstBar_da_CIPLAK_metin_yok_son_sorgu_sozlukten()
    {
        var razor = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "Layout", "UstBar.razor"));
        Assert.DoesNotContain("\"Son sorgu\"", razor);
        Assert.DoesNotContain("\"Last query\"", razor);
        Assert.DoesNotContain("Son başarılı sorgu", razor);
        Assert.DoesNotContain("style=\"", razor);
    }

    [Fact]
    public void S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir()
    {
        var css = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "wwwroot", "css", "app.css"));
        var i = css.IndexOf("S1-c — ÜST BAR: KUR ŞERİDİ", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-c bloğu yok.");
        var blok = css[i..];

        // Sabit renk YOK (plan § 3): her renk belirteçten.
        var kod = Regex.Replace(blok, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", kod);
        Assert.DoesNotContain("overflow", kod);

        // Tuval ölçüleri
        Assert.Contains("min-height: 68px", kod);
        Assert.Contains("font-family: var(--yazi-marka)", kod);
        Assert.Contains(".ust-bar h1 { font-size: 22px; }", kod);
        Assert.Contains(".ust-kur-serit .kur-bandi .deger { font-size: 19px; }", kod);
        Assert.Contains("text-transform: uppercase; letter-spacing: .04em;", kod);
        Assert.Contains("font-variant-numeric: tabular-nums", kod);
        Assert.Contains("border-radius: var(--yaricap-tam)", kod);
        Assert.Contains("background: var(--kenar-yuzey); color: var(--kenar-metin-guclu)", kod);
        Assert.Contains(".ust-yardim { color: var(--vurgu);", kod);

        // Tipografi ≥769 px'e hapsedilmiş: dar ekran kuralları ezilmez.
        var medya = kod.IndexOf("@media (min-width: 769px)", StringComparison.Ordinal);
        Assert.True(medya > 0);
        Assert.True(kod.IndexOf("font-size: 22px", StringComparison.Ordinal) > medya);
        Assert.True(kod.IndexOf("padding: 6px 14px", StringComparison.Ordinal) > medya);
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }
}
