using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-AMBLEM-2 — ByGatech amblemi v2 (EMİR-AMBLEM-YILDIZ-2026-10-10 · kurucu 10.10 "eksiksiz ve aynen")
// Tuval: ham/yildiz-tuval/kaynak/Amblem_sol(.acik).dc.html (kabuk 52 px; YILDIZ-AMBLEM-4) · Giris4(a).dc.html (giriş 96 px).
//
// Görsel eşitliğin ASIL kanıtı başsız Chrome'da tuval ↔ uygulama zaman eşitlenmiş piksel karşılaştırmasıdır
// (rapor). Bu sınıf o ölçümün dayandığı SÖZLEŞMEYİ kilitler (v1 YildizAmblemTestleri'nin v2 uyarlaması):
//   • bUnit: 3B 8 yüzlü taş (4 taç kapalı üçgen + 4 pavyon "V"), ölçüler § 3 formülleriyle, gradyan + SMIL,
//     "ByGatech" bütün yazı (taşla AYNI gradyan), ek görsel/betik/satır içi stil yok; giriş Yıldız dalı.
//   • CSS: 28 sn dönüş 20°→380° linear, 11 sn ışık/parlama/yazı, süzgeç DIŞ sarmalayıcıda (3B kutuda filter
//     YOK), harf harf YOK, taş hiç kaybolmaz, açık/koyu renk + --glow, hareket azaltmada sabit.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizAmblem2Testleri : BunitContext
{
    private const string Y = "html:root[data-sema=\"yildiz\"]";
    private const string Azalt = "@media (prefers-reduced-motion: reduce)";
    private bool mfaIstensin;

    private sealed class SahteApi(Func<bool> mfa) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":[]}""",
                var y when y.EndsWith("/tercihler/benim") => """{"tercihler":{},"mesaj":"ok"}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","tenantId":"t1","altinHasFiyati":6656,"usdTry":41.95,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-08T09:02:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22","milyem":916,"gramFiyati":6097,"hasFiyati":6656}]}""",
                var y when y.EndsWith("/kullanici/gorunum") => """{"temaModu":"karanlik","sema":"yildiz","yaziBoyutu":18}""",
                var y when y.EndsWith("/gorunum") => """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.EndsWith("/genel/kisitlar") => """{"kaynak":"test","sayi":0,"liste":[]}""",
                var y when y.EndsWith("/auth/giris") && mfa() => """{"mfaGerekli":true,"mfaToken":"m1"}""",
                var y when y.EndsWith("/auth/giris") =>
                    """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""",
                var y when y.Contains("/subeler") => "[]",
                _ => """{"mesaj":"ok"}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public YildizAmblem2Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => mfaIstensin)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<YildizUstMenu> Menu(string dil = "tr", string ad = "Hakan", string soyad = "Tunalı")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", ad, soyad, "h@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Basit));
        if (dil != "tr") Services.GetRequiredService<DilServisi>().DegistirAsync(dil).GetAwaiter().GetResult();
        Assert.Equal(dil, Services.GetRequiredService<DilServisi>().Dil);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/islemler");
        return Render<YildizUstMenu>();
    }

    /// <summary>
    /// Giriş sayfası; yukle=true → dükkân görünümü önce sunucudan okunur. Tema temizliği (17.10): tek şema
    /// Yıldız — okunsa da okunmasa da düzen AYNIDIR (YA2_14 bunu kilitler).
    /// </summary>
    private IRenderedComponent<Giris> GirisSayfasi(bool yukle)
    {
        if (yukle) Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        return Render<Giris>();
    }

    private static string Css() => YildizTablosu.Css();

    /// <summary>tema-yildiz.css "/* AMBLEM */" bloğu (bir sonraki şerit başlığına kadar).</summary>
    private static string AmblemBlogu()
    {
        var css = Css();
        var i = css.IndexOf("/* AMBLEM */", StringComparison.Ordinal);
        Assert.True(i >= 0, "tema-yildiz.css'te /* AMBLEM */ bloğu yok");
        var j = css.IndexOf("/* YILDIZ-4D */", i, StringComparison.Ordinal);
        return j > i ? css[i..j] : css[i..];
    }

    private static List<CssKaynak.Bildirim> B() => CssKaynak.Bildirimler(AmblemBlogu());

    private static string BelirtecBlogu()
    {
        var css = Css();
        var i = css.IndexOf("/* AMBLEM — belirteçler */", StringComparison.Ordinal);
        var j = css.IndexOf("/* AMBLEM — belirteçler sonu */", StringComparison.Ordinal);
        Assert.True(i >= 0 && j > i, "AMBLEM belirteç bloğu yok");
        Assert.True(i < css.IndexOf("PAKET YILDIZ-2 — KABUK", StringComparison.Ordinal), "belirteç bloğu YILDIZ-1 belirteç bölümünde olmalı");
        return css[i..j];
    }

    private static string Deger(List<CssKaynak.Bildirim> b, string secici, string ozellik, string atKural = "")
    {
        var d = b.Where(x => x.Ozellik == ozellik && x.AtKural == atKural && x.Secici == secici).ToList();
        Assert.True(d.Count == 1, $"{atKural} {secici} {{ {ozellik} }} bulunamadı ya da birden çok ({d.Count})");
        return d[0].Deger;
    }

    private static double Sayi(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    // ── YA2-01) Kabuk: tek <a> → Bugün; dış sarmalayıcı + 3B kutu + 42 şerit; yazı 110×16 ─────────
    [Fact]
    public void YA2_01_kabuk_tek_baglanti_bugune_tas_52_ve_yazi_110x16()
    {
        var e = Menu();
        var a = e.Find("a#yildiz-marka");
        Assert.Equal("/", a.GetAttribute("href"));
        Assert.Contains("amb", a.ClassList);
        Assert.Equal(KabukTuvalSozlugu.MarkaYazimi, a.GetAttribute("aria-label"));
        Assert.Single(e.FindAll("a.amb"));
        var cocuk = a.Children.ToList();
        Assert.Equal(2, cocuk.Count);
        Assert.Equal(("span", "amb-parla"), (cocuk[0].TagName.ToLowerInvariant(), cocuk[0].ClassName));
        Assert.Equal("true", cocuk[0].GetAttribute("aria-hidden"));
        var yazi = cocuk[1];
        Assert.Equal("svg", yazi.TagName, ignoreCase: true);
        Assert.Contains("amb-yz", yazi.ClassList);
        Assert.Equal(("140", "16", "0 0 140 16"), (yazi.GetAttribute("width"), yazi.GetAttribute("height"), yazi.GetAttribute("viewBox")));
        // Yapı: .amb-parla > .amb-ik > (svg.amb-tanim, span.tas3d > 42 × svg.amb-yuz)
        var ik = cocuk[0].Children.Single();
        Assert.Equal("amb-ik", ik.ClassName);
        Assert.Equal(2, ik.Children.Length);
        Assert.Contains("amb-tanim", ik.Children[0].ClassList);
        Assert.Equal("tas3d", ik.Children[1].ClassName);
        Assert.Equal(42, ik.Children[1].Children.Length);   // kurucu 14.10 pırlanta: 26 kenar + 16 dik şerit
        Assert.All(ik.Children[1].Children, s => Assert.Contains("amb-yuz", s.ClassList));
    }

    // ── YA2-02) Taş (kurucu 14.10 pırlanta çizimi): 26 kenar + 16 dik şerit, eşit kalın, gradyan dolgulu ─────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void YA2_02_kurucu_14_10_pirlanta_42_serit_cizim_ozellikleri(bool kabuk)
    {
        var o = kabuk ? YildizUstMenu.KabukTasi : YildizUstMenu.GirisTasi;
        Assert.Equal(42, o.Kenarlar.Length);
        Assert.Single(o.Kenarlar.Select(k => k[1]).Distinct());                 // bütün çizgiler eşit kalınlıkta
        Assert.Equal(4, o.Kenarlar.Count(k => k[3] == "1"));                    // siluet: iki yan kenar + iki yan gövde
        var kap = kabuk ? "#yildiz-marka" : "#giris-amblem";
        Func<string, IReadOnlyList<AngleSharp.Dom.IElement>> bul;
        if (kabuk) { var m = Menu(); bul = s => m.FindAll(s); }
        else { var g = GirisSayfasi(true); bul = s => g.FindAll(s); }
        var seritler = bul($"{kap} .tas3d > svg").ToList();
        Assert.Equal(42, seritler.Count);
        for (var i = 0; i < 42; i++)
        {
            var y = o.Kenarlar[i];
            var s = seritler[i];
            Assert.Equal(y[3] == "1" ? $"amb-yuz amb-cift amb-f{i}" : $"amb-yuz amb-f{i}", s.GetAttribute("class"));
            Assert.Equal((y[0], y[1], $"0 0 {y[0]} {y[1]}"), (s.GetAttribute("width"), s.GetAttribute("height"), s.GetAttribute("viewBox")));
            var r = s.QuerySelector("rect")!;
            Assert.Equal((y[0], y[1], y[2], $"url(#{o.GradyanId})"), (r.GetAttribute("width"), r.GetAttribute("height"), r.GetAttribute("rx"), r.GetAttribute("fill")));
        }
        Assert.Empty(bul($"{kap} [style]"));   // satır içi stil yok (CSP)
    }

    // ── YA2-03) Işık: gradyan durakları + SMIL 11 sn BİREBİR; taş ve yazı aynı gradyan ─────────
    [Fact]
    public void YA2_03_gradyan_ve_SMIL_11sn_tuvalle_birebir_tas_ve_yazi_ayni_gradyan()
    {
        var e = Menu();
        var lg = e.Find("#yildiz-marka .amb-tanim linearGradient");
        Assert.Equal(("yildiz-amb-ti", "0", "0", "1", "0"), (lg.GetAttribute("id"), lg.GetAttribute("x1"), lg.GetAttribute("y1"), lg.GetAttribute("x2"), lg.GetAttribute("y2")));
        var duraklar = lg.QuerySelectorAll("stop").Select(s => (s.GetAttribute("offset"), s.GetAttribute("class"))).ToList();
        Assert.Equal([("0", "amb-s-govde"), (".42", "amb-s-orta"), (".5", "amb-s-parilti"), (".58", "amb-s-orta"), ("1", "amb-s-govde")], duraklar);
        Assert.All(lg.QuerySelectorAll("stop"), s => Assert.Null(s.GetAttribute("stop-color")));   // renk belirteçten
        var at = lg.QuerySelector("animateTransform")!;
        Assert.Equal(("gradientTransform", "translate", "-1.2 0;-1.2 0;1.2 0;1.2 0", "0;0.06;0.22;1", "11s", "indefinite"),
            (at.GetAttribute("attributeName"), at.GetAttribute("type"), at.GetAttribute("values"), at.GetAttribute("keyTimes"), at.GetAttribute("dur"), at.GetAttribute("repeatCount")));
        Assert.Equal("url(#yildiz-amb-ti)", e.Find("#yildiz-marka .amb-yz text").GetAttribute("fill"));
        Assert.Single(e.FindAll("#yildiz-marka linearGradient"));
    }

    // ── YA2-04) Yazı: marka büyük harfle bütün metin, Inter 13/400/4,4; tspan'lar arası boşluk yok ─────────
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void YA2_04_marka_buyuk_harf_tek_metin_Inter_13_400_aralik_4_4_bosluk_yok(string dil)
    {
        var e = Menu(dil);
        var t = e.Find("#yildiz-marka .amb-yz text");
        Assert.Equal(("0", "13", "Inter, system-ui, sans-serif", "13", "400", "4.4"),
            (t.GetAttribute("x"), t.GetAttribute("y"), t.GetAttribute("font-family"), t.GetAttribute("font-size"), t.GetAttribute("font-weight"), t.GetAttribute("letter-spacing")));
        var beklenen = Marka.Ad.ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
        Assert.Equal(beklenen, t.TextContent);
        Assert.Equal(beklenen.Length, t.ChildNodes.Length);   // harf başına tek tspan, boşluk düğümü yok
    }

    // ── YA2-05) Ek dosya / görsel / betik yok (emir § 8) ─────────
    [Fact]
    public void YA2_05_ek_dosya_gorsel_betik_ve_satir_ici_stil_yok()
    {
        var e = Menu();
        Assert.Empty(e.FindAll("#yildiz-marka :is(img, image, script, use, foreignObject, iframe, [style], [href]:not(a))"));
        var g = GirisSayfasi(true);
        Assert.Empty(g.FindAll("#giris-amblem :is(img, image, script, use, foreignObject, iframe, [style])"));
        var blok = CssKaynak.YorumsuzCss(AmblemBlogu());
        Assert.DoesNotContain("url(", blok);
        Assert.DoesNotContain("@import", blok);
    }

    // ── YA2-06) Dönüş: 28 sn, 20° → 380°, linear, sonsuz; duruş 20°; 3B kutu preserve-3d ─────────
    [Fact]
    public void YA2_06_donus_28sn_0_360_linear_durus_onden_cizim()
    {
        var b = B();
        Assert.Equal("amb-don 28s linear infinite", Deger(b, $"{Y} .amb-parla .tas3d", "animation"));
        Assert.Equal("rotateY(0deg)", Deger(b, "from", "transform", "@keyframes amb-don"));
        Assert.Equal("rotateY(360deg)", Deger(b, "to", "transform", "@keyframes amb-don"));
        Assert.Equal("rotateY(0deg)", Deger(b, $"{Y} .amb-parla .tas3d", "transform"));
        Assert.Equal("preserve-3d", Deger(b, $"{Y} .amb-parla .tas3d", "transform-style"));
        Assert.Single(b, x => x.Ozellik == "animation" && x.Deger.StartsWith("amb-don", StringComparison.Ordinal));
    }

    // ── YA2-07) Parlama: dış sarmalayıcıda; 3B kutuda (ve içinde) filter YOK ─────────
    [Fact]
    public void YA2_07_parlama_dis_sarmalayicida_3B_kutuda_filter_yok()
    {
        var b = B();
        Assert.Equal("amb-parla 11s ease-in-out infinite", Deger(b, $"{Y} .amb-parla", "animation"));
        Assert.Equal("drop-shadow(0 0 7px var(--glow))", Deger(b, "18%", "filter", "@keyframes amb-parla"));
        Assert.Equal("drop-shadow(0 0 0 var(--amb-sifir))", Deger(b, "0%, 100%", "filter", "@keyframes amb-parla"));
        Assert.Equal("drop-shadow(0 0 2px var(--amb-sifir))", Deger(b, "60%", "filter", "@keyframes amb-parla"));
        // 3B kutu ve altı: filter / süzgeçli animasyon YOK (tarayıcı 3B'yi düzleştirir — yaşandı).
        var ic = new[] { "amb-ik", "tas3d", "amb-yuz", "amb-tac", "amb-pav" };
        Assert.DoesNotContain(b, x => ic.Any(c => x.Secici.Contains(c, StringComparison.Ordinal))
                                      && (x.Ozellik.StartsWith("filter", StringComparison.Ordinal) || x.Ozellik.Contains("backdrop", StringComparison.Ordinal)
                                          || (x.Ozellik == "animation" && !x.Deger.StartsWith("amb-don", StringComparison.Ordinal) && x.Deger != "none")));
        Assert.DoesNotContain(b, x => x.AtKural == "@keyframes amb-don" && x.Ozellik != "transform");
        // Sarmalayıcı sırası (bUnit): süzgeçli öğe 3B kutunun ATASI.
        var e = Menu();
        Assert.NotNull(e.Find("#yildiz-marka > .amb-parla > .amb-ik > .tas3d"));
    }

    // ── YA2-08) Yazı bütün olarak karanlıktan çıkar; harf harf YOK; taş hiç kaybolmaz ─────────
    [Fact]
    public void YA2_08_yazi_butun_olarak_cikar_harf_harf_yok_tas_kaybolmaz()
    {
        var b = B();
        Assert.Equal("amb-yazi 11s ease-out infinite", Deger(b, $"{Y} .amb-yz", "animation"));
        const string k = "@keyframes amb-yazi";
        Assert.Equal(("0", "blur(4px)"), (Deger(b, "0%, 12%", "opacity", k), Deger(b, "0%, 12%", "filter", k)));
        Assert.Equal(("1", "blur(0)"), (Deger(b, "24%", "opacity", k), Deger(b, "24%", "filter", k)));
        Assert.Equal(("1", "blur(0)"), (Deger(b, "82%", "opacity", k), Deger(b, "82%", "filter", k)));
        Assert.Equal(("0", "blur(3px)"), (Deger(b, "91%", "opacity", k), Deger(b, "91%", "filter", k)));
        Assert.Equal("0", Deger(b, "100%", "opacity", k));
        // Harf harf YOK: harf keyframes'i yok, gecikme yok, tspan'ı (.h) hedefleyen kural yok.
        var tum = CssKaynak.Bildirimler(Css());
        Assert.DoesNotContain(tum, x => x.AtKural.StartsWith("@keyframes amb-harf", StringComparison.Ordinal));
        Assert.DoesNotContain(b, x => x.Ozellik.StartsWith("animation-delay", StringComparison.Ordinal));
        Assert.DoesNotContain(b, x => Regex.IsMatch(x.Secici, @"\.h\d?\b"));
        // Taş hiç kaybolmaz: taşa ya da onun atasına opaklık animasyonu / opacity:0 yok.
        Assert.DoesNotContain(b, x => (x.AtKural.Contains("amb-don") || x.AtKural.Contains("amb-parla")) && x.Ozellik == "opacity");
        Assert.DoesNotContain(b, x => x.Ozellik == "opacity" && x.Deger == "0" && x.AtKural.StartsWith("@media", StringComparison.Ordinal));
        Assert.DoesNotContain(b, x => Regex.IsMatch(x.Secici, @"\.amb-(ik|parla)\b") && x.Ozellik == "opacity" && x.Deger != "1");
    }

    // ── YA2-09) Renkler: açık / koyu durak + --glow (emir § 5) ─────────
    [Fact]
    public void YA2_09_acik_ve_koyu_renkler_ve_glow_tuvalle_birebir()
    {
        var t = CssKaynak.Bildirimler(BelirtecBlogu());
        string D(string sec, string oz, string at = "") => Deger(t, sec, oz, at);
        var acik = $"{Y} .amb-parla";
        Assert.Equal(("#3A3A3A", "#5E5E5E", "#C9C6BE", "rgba(20,20,20,.25)"),
            (D(acik, "--amb-govde"), D(acik, "--amb-orta"), D(acik, "--amb-parilti"), D(acik, "--glow")));
        Assert.Equal("rgba(255,255,255,0)", D(acik, "--amb-sifir"));
        foreach (var (sec, at) in new[] { ("html:root:not([data-tema=\"aydinlik\"])[data-sema=\"yildiz\"] .amb-parla", "@media (prefers-color-scheme: dark)"),
                                          ("html:root[data-tema=\"karanlik\"][data-sema=\"yildiz\"] .amb-parla", "") })
            Assert.Equal(("#6A6A6A", "#9A9A9A", "#FFFFFF", "rgba(243,242,239,.45)"),
                (D(sec, "--amb-govde", at), D(sec, "--amb-orta", at), D(sec, "--amb-parilti", at), D(sec, "--glow", at)));
        var b = B();
        Assert.Equal("var(--amb-govde)", Deger(b, $"{Y} .amb-parla .amb-s-govde", "stop-color"));
        Assert.Equal("var(--amb-orta)", Deger(b, $"{Y} .amb-parla .amb-s-orta", "stop-color"));
        Assert.Equal("var(--amb-parilti)", Deger(b, $"{Y} .amb-parla .amb-s-parilti", "stop-color"));
    }

    // ── YA2-10) Önden izdüşüm = kurucunun çizimi (124×111 ölçüsü); kabuk 52 / giriş 96, perspektif 16·S ─────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void YA2_10_onden_izdusum_kurucunun_cizimi(bool kabuk)
    {
        var b = B();
        double S = kabuk ? 52 : 96, W = .92 * S, H = W * 92 / 112;
        if (kabuk) { H = Math.Round(H * 24 / 92) / (24.0 / 92); W = H * 112 / 92; }   // kabuk: tabla ve kuşak tam piksel satırında
        double R = W / 2, a = R * 38 / 56, ic = R * 27.25 / 56, ust = -H / 2;
        if (kabuk) ust += Math.Floor(S / 2 + ust) + .5 - (S / 2 + ust);
        double kusak = ust + H * 24 / 92, uc = ust + H;
        var kap = kabuk ? $"{Y} .amb-parla" : $"{Y} .giris-amb";
        Assert.Equal($"{S}px", Deger(b, $"{kap} .amb-ik", "width"));
        Assert.Equal($"{40 * S}px", Deger(b, $"{kap} .amb-ik", "perspective"));   // 40·S: perspektif çizimin düz üst kenarını bükmesin
        Assert.Equal(("0 0", "hidden", "visible"), (Deger(b, $"{Y} .amb-parla .amb-yuz", "transform-origin"),
            Deger(b, $"{Y} .amb-parla .amb-yuz", "backface-visibility"), Deger(b, $"{Y} .amb-parla .amb-yuz.amb-cift", "backface-visibility")));
        var noktalar = new (double X, double Y)[] { (-a, ust), (0, ust), (a, ust), (-R, kusak), (-ic, kusak), (ic, kusak), (R, kusak), (0, uc) };
        var o = kabuk ? YildizUstMenu.KabukTasi : YildizUstMenu.GirisTasi;
        var on = 0;
        for (var i = 0; i < 42; i++)
        {
            var m = Deger(b, $"{kap} .amb-f{i}", "transform");
            Assert.StartsWith("matrix3d(", m, StringComparison.Ordinal);
            var v = m[9..^1].Split(',').Select(Sayi).ToArray();
            if (i >= 26 || v[10] <= 0) continue;            // dik şerit (26..51, duruşta kenarından görünür) ya da arka kenar
            on++;
            double L = Sayi(o.Kenarlar[i][0]), sw = Sayi(o.Kenarlar[i][1]);
            foreach (var x0 in new[] { sw / 2, L - sw / 2 })
            {
                double x = v[0] * x0 + v[4] * sw / 2 + v[12], y = v[1] * x0 + v[5] * sw / 2 + v[13];
                Assert.Contains(noktalar, n => Math.Abs(n.X - x) < .05 && Math.Abs(n.Y - y) < .05);
            }
        }
        Assert.Equal(15, on);                               // önde 11 kenar + 4 siluet
    }

    // ── YA2-11) Hareket azaltma: bütün animasyon kapalı; taş 20°, yazı tam opak, sabit ─────────
    [Fact]
    public void YA2_11_hareket_azaltmada_sabit_tas_20_derece_yazi_opak()
    {
        var b = B();
        Assert.Equal("none", Deger(b, $"{Y} :is(.amb-parla, .amb-parla .tas3d, .amb-yz, .giris-yildiz #giris-form)", "animation", Azalt));
        Assert.Equal("none", Deger(b, $"{Y} .amb-parla animateTransform", "display", Azalt));
        Assert.Equal("var(--amb-govde)", Deger(b, $"{Y} .amb-parla :is(.amb-s-orta, .amb-s-parilti)", "stop-color", Azalt));
        Assert.DoesNotContain(b, x => x.AtKural == Azalt && (x.Ozellik == "opacity" || x.Ozellik == "transform"));   // taş 20° (sınıf), yazı opak
        var css = AmblemBlogu();
        Assert.True(css.IndexOf(Azalt, StringComparison.Ordinal) > css.IndexOf("@keyframes amb-form-gel", StringComparison.Ordinal),
            "hareket azaltma kuralı animasyon kurallarından SONRA gelmeli");
    }

    // ── YA2-12) Dar masaüstü / telefon: amblem değişmez, şerit taşmaz; 1440 tuval birebir (v1 YA_08) ─────────
    [Fact]
    public void YA2_12_dar_genislik_kurallari_amblemi_degil_sagi_daraltir_1440_dokunulmaz()
    {
        var b = B();
        var genislik = b.Where(x => x.AtKural.Contains("width", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(genislik);
        foreach (var x in genislik)
        {
            var ust = Regex.Matches(x.AtKural, @"max-width:\s*(\d+)px").Select(m => int.Parse(m.Groups[1].Value)).ToList();
            Assert.True(ust.Count == 1 && ust[0] <= 1439, $"1440'a sızan kural: {x.AtKural} {x.Secici}");
            Assert.DoesNotContain(".amb", x.Secici);
            Assert.DoesNotContain("yildiz-marka", x.Secici);
        }
        // Tur 2 (hakem KRİTİK: 1366 x 768'de şerit taşıyordu): 1366 bu aralığın İÇİNDE — 22 AYAR ≤1439'da gizli.
        Assert.Equal("none", Deger(b, $"{Y} .yildiz-ust .yildiz-kur-22", "display", "@media (max-width: 1439px)"));
        Assert.Equal("none", Deger(b, $"{Y} .yildiz-ust .yildiz-kur-saat", "display", "@media (max-width: 1092px)"));
        const string siki = "@media (min-width: 1001px) and (max-width: 1316px)";
        Assert.Equal(("20px", "0 24px", "24px"), (Deger(b, $"{Y} .yildiz-ust", "gap", siki), Deger(b, $"{Y} .yildiz-ust", "padding", siki), Deger(b, $"{Y} .yildiz-ust .yildiz-kullanici", "margin-left", siki)));
    }

    // ── YA2-18) Tur 2: kullanıcı adı "AD S." — birden çok ön adda yalnız ilki; ≤600 baş harfler (data-bas) ─────────
    [Theory]
    [InlineData("Hakan", "Tunalı", "HAKAN T.", "HT")]
    [InlineData("Hatice Gülizar", "Karaosmanoğlu", "HATİCE K.", "HK")]
    [InlineData("  İsmail   Hakkı ", "Işık", "İSMAİL I.", "İI")]
    [InlineData("Ayşe", "", "AYŞE", "A")]
    public void YA2_18_kullanici_adi_ilk_on_ad_ve_soyad_bas_harfi_bas_harfler_data_bas(string ad, string soyad, string beklenen, string bas)
    {
        var e = Menu(ad: ad, soyad: soyad);
        var d = e.Find("#yildiz-kullanici-ac");
        Assert.Equal(beklenen, d.TextContent);                                   // erişilebilir ad = görünen ad
        Assert.Equal(beklenen, d.QuerySelector("span.yildiz-kullanici-ad")!.TextContent);   // "…" kesimi iç span'da
        Assert.Equal(bas, d.GetAttribute("data-bas"));
    }

    // ── YA2-19) Tur 2: ad şeridi ASLA taşırmaz (küçülür, "…"), 1440 satır tabanı korunur, ≤600 baş harf ─────────
    [Fact]
    public void YA2_19_ad_kutusu_kuculur_ucu_kesilir_satir_tabani_korunur_dar_ekranda_bas_harf()
    {
        var b = B();
        Assert.Equal(("0 1 auto", "0"), (Deger(b, $"{Y} .yildiz-ust .yildiz-kullanici", "flex"), Deger(b, $"{Y} .yildiz-ust .yildiz-kullanici", "min-width")));
        Assert.Equal("100%", Deger(b, $"{Y} .yildiz-ust .yildiz-kullanici-dugme", "max-width"));
        var ad = $"{Y} .yildiz-ust .yildiz-kullanici-ad";
        Assert.Equal(("inline-block", "100%", "hidden", "ellipsis", "top"),
            (Deger(b, ad, "display"), Deger(b, ad, "max-width"), Deger(b, ad, "overflow"), Deger(b, ad, "text-overflow"), Deger(b, ad, "vertical-align")));
        // Düğmenin kendisine overflow/display verilmez: satır tabanı kayar (1440'ta ad 1,1 px yukarı çıkıyordu, ölçüldü).
        Assert.DoesNotContain(b, x => x.Secici.EndsWith(".yildiz-kullanici-dugme", StringComparison.Ordinal) && x.AtKural == "" && (x.Ozellik is "overflow" or "display"));
        Assert.DoesNotContain(b, x => x.Secici.EndsWith(".yildiz-ust .yildiz-kullanici", StringComparison.Ordinal) && x.AtKural == "" && x.Ozellik == "display");
        const string tel = "@media (max-width: 600px)";
        Assert.Equal("0", Deger(b, ad, "font-size", tel));
        Assert.Equal("attr(data-bas)", Deger(b, $"{Y} .yildiz-ust .yildiz-kullanici-dugme::before", "content", tel));
    }

    // ── YA2-13) Giriş (Yıldız): amblem 96 ortada, yazı 220×32 ortalı; panel/başlık yok; form aynen ─────────
    [Fact]
    public void YA2_13_giris_yildizda_amblem_96_ortada_yazi_ortali_form_aynen()
    {
        var e = GirisSayfasi(true);
        Assert.Contains("giris-yildiz", e.Find("#giris-sayfa").ClassList);
        var h1 = e.Find("h1#giris-amblem.giris-amb");
        Assert.Equal(KabukTuvalSozlugu.MarkaYazimi, h1.GetAttribute("aria-label"));
        Assert.Equal(2, h1.Children.Length);
        Assert.Equal("amb-parla", h1.Children[0].ClassName);
        var yazi = h1.Children[1];
        Assert.Equal(("280", "32", "0 0 140 16"), (yazi.GetAttribute("width"), yazi.GetAttribute("height"), yazi.GetAttribute("viewBox")));
        var t = yazi.QuerySelector("text")!;
        Assert.Equal(("72", "13", "middle", "url(#giris-amb-ti)", "4.4"),
            (t.GetAttribute("x"), t.GetAttribute("y"), t.GetAttribute("text-anchor"), t.GetAttribute("fill"), t.GetAttribute("letter-spacing")));
        Assert.Equal(Marka.Ad.ToUpper(CultureInfo.GetCultureInfo("tr-TR")), t.TextContent);
        Assert.Equal("giris-amb-ti", e.Find("#giris-amblem linearGradient").GetAttribute("id"));
        Assert.Empty(e.FindAll("#giris-panel"));
        Assert.Empty(e.FindAll("#giris-marka"));
        Assert.Empty(e.FindAll("#sayfa-basi-giris"));
        foreach (var id in new[] { "giris-form", "giris-eposta", "giris-sifre", "giris-gonder", "giris-parola-unuttum", "giris-yeni-dukkan", "giris-surum", "giris-kisit", "giris-dil" })
            Assert.Single(e.FindAll($"#{id}"));
        Assert.Single(e.FindAll("h1"));
    }

    // ── YA2-14) Giriş: tek şema (tema temizliği 17.10) — görünüm okunmadan da Yıldız düzeni; eski panel YOK ─────────
    [Fact]
    public void YA2_14_giris_gorunum_okunmadan_da_yildiz_duzeni_eski_panel_yok()
    {
        var e = GirisSayfasi(false);
        Assert.Equal("giris-sayfa giris-iki-panel giris-yildiz", e.Find("#giris-sayfa").GetAttribute("class"));
        Assert.Single(e.FindAll("#giris-amblem"));
        Assert.Empty(e.FindAll("#giris-marka"));
        Assert.Empty(e.FindAll("#giris-panel"));
    }

    // ── YA2-15) Giriş: form 1,6 sn'de belirir (.9 s), alanlar yazılabilir (öznitelik kilidi yok) ─────────
    [Fact]
    public void YA2_15_giris_form_1_6sn_belirir_alanlar_yazilabilir()
    {
        var b = B();
        Assert.Equal("amb-form-gel .9s ease-out 1.6s both", Deger(b, $"{Y} .giris-yildiz #giris-form", "animation"));
        Assert.Equal(("0", "translateY(10px)"), (Deger(b, "from", "opacity", "@keyframes amb-form-gel"), Deger(b, "from", "transform", "@keyframes amb-form-gel")));
        Assert.Equal(("1", "none"), (Deger(b, "to", "opacity", "@keyframes amb-form-gel"), Deger(b, "to", "transform", "@keyframes amb-form-gel")));
        Assert.DoesNotContain(b, x => x.Secici.Contains("giris", StringComparison.Ordinal) && x.Ozellik is "pointer-events" or "visibility");
        var e = GirisSayfasi(true);
        Assert.Null(e.Find("#giris-eposta").GetAttribute("disabled"));
        Assert.Null(e.Find("#giris-sifre").GetAttribute("disabled"));
    }

    // ── YA2-16) Giriş (Yıldız): MFA ikinci adımı (C4_62 kurtarma kodu alanı) DEĞİŞMEZ ─────────
    [Fact]
    public void YA2_16_giris_yildizda_MFA_ikinci_adimi_aynen()
    {
        mfaIstensin = true;
        var e = GirisSayfasi(true);
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola12345");
        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#giris-mfa-kod")));
        Assert.Equal("one-time-code", e.Find("#giris-mfa-kod").GetAttribute("autocomplete"));
        Assert.Single(e.FindAll("#giris-mfa-dogrula"));
        Assert.Single(e.FindAll("#giris-mfa-vazgec"));
        Assert.Single(e.FindAll("#giris-amblem"));   // amblem MFA adımında da yerinde
    }
    // ── YA2-17) Giriş (Yıldız): alanlar tuval .alan kalıbı (altı kıl çizgi), hata çizgisi korunur; ≤768 dil sağ üstte ─────────
    [Fact]
    public void YA2_17_giris_yildiz_alan_cizgisi_hata_rengi_ve_dar_ekran_dil_yeri()
    {
        var b = B();
        var alan = $"{Y} .giris-yildiz :is(.girdi, .secim, .sifre-kutu)";
        Assert.Equal(("transparent", "0 0 1px", "solid", "var(--kenarlik)", "0"),
            (Deger(b, alan, "background"), Deger(b, alan, "border-width"), Deger(b, alan, "border-style"), Deger(b, alan, "border-color"), Deger(b, alan, "border-radius")));
        Assert.Equal("0", Deger(b, $"{Y} .giris-yildiz .sifre-kutu > .girdi.sifre-kutu-girdi", "border"));
        Assert.Equal("var(--tehlike)", Deger(b, $"{Y} .giris-yildiz :is(.girdi, .sifre-kutu).hatali", "border-color"));
        const string dar = "@media (max-width: 768px)";
        Assert.Equal(("absolute", "16px", "16px"), (Deger(b, $"{Y} .giris-yildiz .giris-dil", "position", dar), Deger(b, $"{Y} .giris-yildiz .giris-dil", "top", dar), Deger(b, $"{Y} .giris-yildiz .giris-dil", "right", dar)));
        Assert.Equal("80px", Deger(b, $"{Y} .giris-yildiz .giris-sol", "padding-top", dar));
        // Yalnız Yıldız girişinde: kural hiçbir öteki şemaya/sayfaya sızmaz.
        Assert.All(b.Where(x => x.Secici.Contains("giris", StringComparison.Ordinal)), x => Assert.StartsWith(Y, x.Secici));
    }
}
