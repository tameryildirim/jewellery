using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════
// PAKET YILDIZ-3 — EKRAN İNCE AYARI (Bugün · Tezgâh · Müşteriler · Kasalar/Kur/Sayım)
// Kaynak: TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md § 4 E + F · tuval Bugun4 / Tezgah4 / Musteri4 / Y_*.
//
// Kilitlenen sözleşmeler:
//   • YildizSayi.Bol: kuruş + ₺ ayrılır, metin AYNEN korunur (para davranışı değişmez).
//   • Panel hero: yalnız Yıldız'da kuruş/₺ ayrı soluk öğe; başka şemada özgün markup.
//   • CSS (tema-yildiz.css E bölümü): her kural Yıldız kapsamlı; Panel düzeni yalnız #panel-sade
//     (/raporlar "Bugün" sekmesi aynı sınıfları taşır ve DEĞİŞMEZ); büyük sayı ≥ 300;
//     15 px altı yazı ≥ 500; sabit renk yok; Tezgâh'ta ayar rakamı başta + Fark bloğu;
//     özet ekran başlığı yalnız BASİT'te büyür.
// ⚠ Hasımsız paket (kurucu 30.09): bu testler + mutasyon + gerçek Chrome ölçümü bağlayıcı kanıttır.
// ═══════════════════════════════════════════════════════════

public sealed class Yildiz3Testleri : BunitContext
{
    private string sema = "yildiz";

    private const string Gunluk = """
        {"tarih":"2026-09-23","satis":{"adet":3,"toplamTl":57550.00,"toplamHas":10.4},
         "alis":{"adet":1,"toplamTl":3000.00,"toplamHas":1.0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":9.4,"hareketSayisi":4,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":44200.00,"paraBirimi":"TRY"}]}
        """;

    private const string Pozisyon = """
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":298.4,"hurdaGram":0,"cariAlacakGram":14.4,"cariBorcGram":0,
                  "netGram":312.8,"alacakliCariSayisi":1,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-09-23T09:00:00Z","donemdenBagimsiz":true}
        """;

    private const string Kar = """
        {"hasilatTl":57550.00,"maliyetTl":53430.00,"karTl":4120.00,"karMarjiYuzde":7.16,
         "hasilatHas":10.4,"maliyetHas":10.0,"karHas":0.4,"satisAdedi":3,
         "maliyetiBilinenAdet":3,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","uyari":null}
        """;

    private sealed class SahteApi(Func<string> sema) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk") => Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => """{"yaslananCariler":[],"ozetTl":{"0-30":0,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/vade-takibi") => """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri") => """{"baslangic":"2026-09-23","bitis":"2026-09-23","toplamSatir":0,"ofset":0,"limit":5,"dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},"storno":{"adet":0,"tutarTl":0},"satirlar":[]}""",
                var y when y.EndsWith("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar") => Kar,
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":44200.00,"nakit":44200.00,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/pozisyon") => Pozisyon,
                var y when y.EndsWith("/kur/kaynaklar") => """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/") => """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{sema()}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public Yildiz3Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => sema)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
        Services.AddScoped<YetkiServisi>();   // YILDIZ-4A: Yıldız Panel'i YildizBugun çizer ("Günü kapat" yetki süzgeci)
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> BasitPanel()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement(sema == "yildiz" ? "#yildiz-bugun" : "#panel-sade");   // YILDIZ-4A: Yıldız'da gövde tuvalin kendisi
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".karsilama-iskelet")), TimeSpan.FromSeconds(10));
        return e;
    }

    private static string Css() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");

    /// <summary>Yalnız YILDIZ-3 bölümü (E), yorumsuz.</summary>
    private static string EBlok()
    {
        var c = Css();
        var isaret = c.IndexOf("PAKET YILDIZ-3", StringComparison.Ordinal);
        Assert.True(isaret > 0, "tema-yildiz.css'te YILDIZ-3 bölümü yok");
        var bas = c.LastIndexOf("/*", isaret, StringComparison.Ordinal);   // başlık yorumunun başı
        // YILDIZ-4A: E bölümü bir sonraki paketin (tuval-birebir) başlığında biter.
        var sonIsaret = c.IndexOf("PAKET YILDIZ-4A — BUGÜN", isaret, StringComparison.Ordinal);
        var son = sonIsaret > 0 ? c.LastIndexOf("/*", sonIsaret, StringComparison.Ordinal) : c.Length;
        return Regex.Replace(c[bas..son], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    /// <summary>Kural gövdesi: seçici ifadesi verilen ilk kuralın { … } içi.</summary>
    private static string Kural(string blok, string secici)
    {
        var i = blok.IndexOf(secici, StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = blok.IndexOf('{', i);
        var b = blok.IndexOf('}', a);
        return blok[(a + 1)..b];
    }

    // ── Saf kurallar ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("98.112,96 ₺", "98.112", ",96 ₺")]
    [InlineData("113.011,33 ₺", "113.011", ",33 ₺")]
    [InlineData("1.234.567,89 ₺", "1.234.567", ",89 ₺")]
    [InlineData("−3.840,00 ₺", "−3.840", ",00 ₺")]
    [InlineData("57.550,00 ₺", "57.550", ",00 ₺")]
    public void Y3_01_YildizSayi_Bol_kurus_ve_TL_ayirir_ve_birlesince_ozgun_olur(string metin, string tam, string kurus)
    {
        var p = YildizSayi.Bol(metin);
        Assert.NotNull(p);
        Assert.Equal(tam, p!.Value.Tam);
        Assert.Equal(kurus, p.Value.Kurus);
        Assert.Equal(metin, p.Value.Tam + p.Value.Kurus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("—")]
    [InlineData("0 ₺")]
    [InlineData("1.234 ₺")]
    [InlineData("12,5 ₺")]
    [InlineData(",96 ₺")]
    [InlineData("a,96 ₺")]
    public void Y3_02_YildizSayi_Bol_kurus_yoksa_null_doner_cagiran_ozgun_dizgeyi_basar(string? metin)
        => Assert.Null(YildizSayi.Bol(metin));

    // ── Panel hero (bUnit) ─────────────────────────────────────────────────

    [Fact]
    public void Y3_10_Yildiz_hero_kurus_ve_TL_ayri_soluk_oge_metin_AYNI()
    {
        sema = "yildiz";
        var e = BasitPanel();
        // YILDIZ-4A: Yıldız'da baş rakam tuvalin dev rakamıdır (#yildiz-bugun-rakam).
        var d = e.Find("#yildiz-bugun-rakam");
        Assert.Equal("57.550", d.QuerySelector(".yildiz-bugun-tam")!.TextContent);
        Assert.Equal(",00 ₺", d.QuerySelector(".yildiz-bugun-kurus")!.TextContent);
        Assert.Equal(Bicim.TlKurus(57550m), d.TextContent.Trim());   // para/biçim davranışı değişmedi
        Assert.Equal("/raporlar/islem-defteri", d.GetAttribute("href"));
    }

    [Fact]
    public void Y3_11_Baska_semada_hero_ozgun_markup_kurus_ogesi_YOK()
    {
        sema = "platin";
        var e = BasitPanel();
        var d = e.Find("#panel-soru-satis-deger");
        Assert.Empty(d.QuerySelectorAll(".yildiz-kurus"));
        Assert.Empty(d.QuerySelectorAll(".yildiz-tam"));
        Assert.Equal(Bicim.TlKurus(57550m), d.TextContent.Trim());
    }

    [Fact]
    public void Y3_12_Yildiz_Panelde_bilesenler_ve_idler_ayni_kalir()
    {
        sema = "yildiz";
        var e = BasitPanel();
        // YILDIZ-4A: Yıldız'da tuvalde olmayan kartlar ÇİZİLMEZ; verileri "Seni bekleyen"/cümle/alt şeritte.
        foreach (var id in new[] { "yildiz-bugun", "yildiz-bugun-rakam", "yildiz-bugun-cumle", "yildiz-bugun-yeni-satis", "yildiz-bugun-alt" })
            Assert.NotEmpty(e.FindAll("#" + id));
        foreach (var id in new[] { "panel-sade", "panel-karsilama", "panel-izgara", "pozisyon-kart" })
            Assert.Empty(e.FindAll("#" + id));
        Assert.Empty(e.FindAll("[style]"));   // satır içi style yok
    }

    // ── CSS sözleşmesi (E bölümü) ──────────────────────────────────────────

    [Fact]
    public void Y3_20_Panel_duzeni_yalniz_panel_sade_kimligine_baglidir()
    {
        // YILDIZ-4A: Yıldız'da Panel #panel-sade ÇİZMEZ → E1 uyarlama kuralları kaldırıldı (ölü kural yok);
        // /raporlar "Bugün" sekmesine bağlı çıplak .panel-sade kuralı da hiç yazılmadı.
        var c = Css();
        Assert.DoesNotContain("#panel-sade", Regex.Replace(c, @"/\*.*?\*/", "", RegexOptions.Singleline));
        Assert.DoesNotMatch(@"\.ana-icerik\s+\.panel-sade\s*[{,]", c);
    }

    [Fact]
    public void Y3_21_Bas_rakam_tuval_112px_200_kurus_yari_boy_soluk()
    {
        // YILDIZ-4A: baş rakam tuvalin değeriyle (112 px / 200) Bugün bölümünde; kuruş yarı boy soluk.
        var c = Regex.Replace(Css(), @"/\*.*?\*/", "", RegexOptions.Singleline);
        var h = Kural(c, ".yildiz-bugun .yildiz-bugun-rakam {");
        Assert.Contains("font-size: 112px;", h);
        Assert.Contains("font-weight: 200;", h);
        var k = Kural(c, ".yildiz-bugun .yildiz-bugun-kurus {");
        Assert.Contains("font-size: .5em;", k);
        Assert.Contains("color: var(--metin-soluk);", k);
    }

    [Fact]
    public void Y3_22_Tezgah_ayar_rakami_basta_toplam_72_fark_blogu_ust_cizgili()
    {
        var b = EBlok();
        Assert.Contains("grid-template-areas: \"ayar ad tutar sil\" \"ayar gram tutar sil\";", b);
        var ayar = Kural(b, "#islem-kalem-tablo > tbody > tr > td:nth-child(2) {");
        Assert.Contains("grid-area: ayar;", ayar);
        Assert.Contains("font-size: 40px;", ayar);
        Assert.Contains("font-weight: 300;", ayar);
        Assert.Contains("font-size: min(72px,", Kural(b, "#islem-verdim-toplam .fis-tuval-tutar {"));
        // Başlık satırı ekran okuyucuda kalır ama görünür kutu üretmez (ekran dışı) — üst üste binme ölçümünde hayalet kutu YOK
        Assert.Contains("left: -9999px;", Kural(b, "#islem-kalem-tablo > thead {"));
        var fark = Kural(b, "#islem-fark {");
        Assert.Contains("border-top: 1px solid var(--kenarlik);", fark);
        Assert.Contains("font-size: min(56px,", Kural(b, "#islem-fark .fis-tuval-tutar {"));
    }

    [Fact]
    public void Y3_23_E_bolumundeki_HER_secici_Yildiz_kapsamlidir()
    {
        var b = EBlok();
        // @media sarmalayıcılarını soy, kural başlıklarını (… {) topla
        foreach (Match m in Regex.Matches(b, @"(?<=^|[}\n])\s*([^{}@\n][^{}]*?)\s*\{", RegexOptions.Multiline))
        {
            var liste = m.Groups[1].Value;
            foreach (var s in SecicileriAyir(liste))
                Assert.StartsWith("html:root[data-sema=\"yildiz\"]", s.Trim());
        }
    }

    private static IEnumerable<string> SecicileriAyir(string liste)
    {
        var derin = 0; var son = 0;
        for (var i = 0; i < liste.Length; i++)
        {
            if (liste[i] is '(' or '[') derin++;
            else if (liste[i] is ')' or ']') derin--;
            else if (liste[i] == ',' && derin == 0) { yield return liste[son..i]; son = i + 1; }
        }
        yield return liste[son..];
    }

    [Fact]
    public void Y3_24_Okunurluk_E_bolumunde_200_agirlik_yok_15_alti_yazi_en_az_500()
    {
        var b = EBlok();
        Assert.DoesNotContain("font-weight: 200", b);
        Assert.DoesNotMatch(@"#[0-9A-Fa-f]{3,8}\b", b);   // sabit renk yok: yalnız var()
        foreach (Match kural in Regex.Matches(b, @"\{([^{}]*)\}"))
        {
            var g = kural.Groups[1].Value;
            var boy = Regex.Match(g, @"font-size:\s*(\d+(?:\.\d+)?)px");
            if (!boy.Success || double.Parse(boy.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 15) continue;
            var agirlik = Regex.Match(g, @"font-weight:\s*(\d+)");
            Assert.True(agirlik.Success && int.Parse(agirlik.Groups[1].Value) >= 500, "15 px altı yazı 500 değil: " + g.Trim());
        }
    }

    [Fact]
    public void Y3_25_Ozet_ekran_basligi_yalniz_BASIT_modda_buyur_375te_40px()
    {
        var b = EBlok();
        var h1 = Kural(b, ".yildiz-kabuk:has(#ust-mod-basit[aria-pressed=\"true\"]):has(:is(#sayfa-basi-cariler, #sayfa-basi-kasalar, #sayfa-basi-kur, #sayfa-basi-sayim)) .ust-bar h1 {");
        Assert.Contains("font-size: 64px;", h1);
        Assert.Contains("font-weight: 300;", h1);
        Assert.Contains("font-size: 40px;", b[b.IndexOf("@media (max-width: 600px)", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Y3_26_Musteriler_Kasalar_Kur_kurallari_ve_alan_kalibi_vardir()
    {
        var b = EBlok();
        Assert.Contains("#cari-liste-tablo > tbody > tr > td[data-kolon=\"tl-bakiye\"] { font-weight: 300; }", b);
        Assert.Contains("grid-template-areas: \"ad bakiye eylem\" \"ayrim bakiye eylem\";", b);   // kasa satırı = ad · bakiye · eylemler
        Assert.Contains("#kur-has-onde > #kur-has-deger {", b);
        Assert.Contains(".ana-icerik .secici-tetik {", b);
        Assert.Contains("border-width: 0 0 1px;", Kural(b, ".ana-icerik .secici-tetik {"));
    }

    [Fact]
    public void Y3_27_Yildiz3_degisen_razor_dosyalari_sema_kosullu_kucuk_dokunustur()
    {
        var panel = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Panel.razor");
        // YILDIZ-4A: Yıldız dalı TEK yerde (@if (YildizMi) … else <text>özgün gövde</text>);
        // özgün satış rakamı başka şemada AYNEN TlKurus, gövdeye Yıldız yaması kalmadı.
        Assert.Contains("@inject IServiceProvider Hizmetler", panel);
        Assert.Contains("@if (YildizMi)", panel);
        Assert.Contains("title=\"@S.SatisBaglantiIpucu\">@Bicim.TlKurus(SatisGosterTl)</a>", panel);
        Assert.DoesNotContain("YildizSayi.Bol(", panel);
    }

    [Fact]
    public void Y3_28_Kagit_muaf_E_bolumu_belge_etiket_ve_yazdirma_kuralina_DOKUNMAZ()
    {
        var b = EBlok();
        Assert.DoesNotContain(".belge", b);
        Assert.DoesNotContain(".urun-etiket", b);
        Assert.DoesNotContain("@media print", b);
        Assert.DoesNotContain("@page", b);
    }

    [Fact]
    public void Y3_29_Yildiz3_mevcut_css_dosyalarina_dokunmaz_tek_dosya_tema_yildiz()
    {
        // İzolasyon ilkesi: yeni Yıldız kuralları yalnız tema-yildiz.css'tedir; başka css'te 'yildiz' kuralı YOK
        var dizin = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css");
        foreach (var f in Directory.GetFiles(dizin, "*.css"))
        {
            if (Path.GetFileName(f) == "tema-yildiz.css") continue;
            Assert.DoesNotContain("yildiz-kurus", File.ReadAllText(f));
        }
    }

    [Fact]
    public void Y3_30_Dar_fis_kartinda_Toplam_ve_Fark_rakami_kap_genisligine_gore_kuculur_sira_dogru()
    {
        var b = EBlok();
        var kap = b.IndexOf("@container fis-tuval (max-width: 520px)", StringComparison.Ordinal);
        Assert.True(kap > 0, "dar kap kuralı yok");
        // Kaskad sırası: dar kap kuralı taban Fark/Toplam kurallarından SONRA gelmeli (7 haneli eksi tutar 375'te sığar)
        Assert.True(kap > b.IndexOf("#islem-fark .fis-tuval-tutar {", StringComparison.Ordinal), "dar kap kuralı tabandan önce: ezilir");
        Assert.True(kap > b.IndexOf("#islem-verdim-toplam .fis-tuval-tutar {", StringComparison.Ordinal));
        var govde = b[kap..];
        Assert.Contains("font-size: min(72px, 10cqi);", govde);
        Assert.Contains("font-size: min(56px, 10cqi);", govde);
        // sağ sütun içeriğe göre büyür (7 haneli Fark 1440'ta taşmaz), üst sınır var
        Assert.Contains("minmax(360px, min(max-content, 520px))", b);
    }

    // ── YILDIZ-3 tasarım hakemi KRİTİK: Tezgâh'ta Kaydet · Toplam · Fark HER ZAMAN ekranda ──────────────

    [Fact]
    public void Y3_31_Tezgah_alt_satir_pencere_altina_yapisir_ve_yapiskanligi_bozan_kaplar_acilir()
    {
        var b = EBlok();
        var alt = Kural(b, "html:root[data-sema=\"yildiz\"] .tuval-tezgah .islem-alt-satir {");
        Assert.Contains("position: sticky;", alt);
        Assert.Contains("bottom: 0;", alt);
        Assert.Contains("background: var(--yuzey-govde);", alt);   // altından içerik sızmaz (sabit renk yok)
        // sticky, taşma gizleyen/kaydıran atada çalışmaz: fiş kabı ve sayfa kabı açılır (yalnız Tezgâh sayfasında)
        Assert.Contains("overflow: visible;", Kural(b, "html:root[data-sema=\"yildiz\"] .tuval-tezgah .fis-tuval {"));
        var ana = Kural(b, "html:root[data-sema=\"yildiz\"] .ana-icerik:has(.islem-sayfa.tuval-tezgah) {");
        Assert.Contains("overflow-y: visible;", ana);
        Assert.Contains("overflow-x: clip;", ana);
        // tuvaldeki 100vh-420 zorlaması (sayfayı pencereden uzatan kural) Yıldız'da bırakılır
        Assert.Contains("min-height: 0;", Kural(b, "html:root[data-sema=\"yildiz\"] .tuval-tezgah .fis-tuval {"));
    }

    [Fact]
    public void Y3_32_Iki_sutunlu_fiste_Toplam_ve_Fark_alt_satirin_hemen_ustune_yapisir_opak_zeminle()
    {
        var b = EBlok();
        var kap = b.IndexOf("@container fis-tuval (min-width: 801px)", StringComparison.Ordinal);
        Assert.True(kap > 0, "iki sütunlu fiş için yapışkan Toplam/Fark kuralı yok");
        var g = Kural(b[kap..], "#islem-fark {");
        Assert.Contains("position: sticky;", g);
        // alt satır yüksekliği = 52 (düğme) + 2×12 (dolgu) + 1 (çizgi) = 77: yapışma payı birebir (aralıkta içerik sızmaz)
        Assert.Contains("bottom: 77px;", g);
        // `#islem-fark.fis-fark-tamam { background: transparent }` kimlik özgüllüğünü yenmek için !important
        Assert.Contains("background: var(--yuzey-govde) !important;", g);
        // dar tek sütunda iki yapışkan üst üste binerdi: yalnız ≥801 px kapta tanımlı
        Assert.DoesNotContain("sticky", b[..kap].Replace(Kural(b, "html:root[data-sema=\"yildiz\"] .tuval-tezgah .islem-alt-satir {"), ""));
        // PRO kalem kartı bu bölümde değil: .tuval-pro yapışkan kuralı yok
        Assert.DoesNotContain(".tuval-pro", b[kap..]);
    }

    [Fact]
    public void Y3_33_Tezgah_Vazgec_ve_Kaydet_hap_alcak_pencerede_Toplam_Fark_kuculur()
    {
        var b = EBlok();
        Assert.Contains("border-radius: 999px !important;", Kural(b, "html:root[data-sema=\"yildiz\"] .islem-alt-satir > #islem-vazgec {"));
        var m = b.IndexOf("@media (max-height: 820px)", StringComparison.Ordinal);
        Assert.True(m > 0, "alçak pencere kuralı yok");
        var g = b[m..];
        Assert.Contains("font-size: min(48px, 12.5cqi);", g);
        Assert.Contains("font-size: min(40px, 14cqi);", g);
        // kaskad: alçak pencere kuralı dar-kap ve taban Toplam kurallarından SONRA gelmeli (yoksa ezilir)
        Assert.True(m > b.IndexOf("@container fis-tuval (max-width: 520px)", StringComparison.Ordinal));
    }
}
