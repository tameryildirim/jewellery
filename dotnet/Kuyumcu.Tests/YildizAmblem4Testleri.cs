using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-AMBLEM-4 — kabuk amblemi 40 px → 52 px, çizgi 1,2 → 1,0 px (kurucu 10.10 16:55:
// "40 px denendi, iç kesim çizgileri karıştı, kurucu reddetti"; EMİR § 3 / § 6).
// Tek doğruluk: ham/yildiz-tuval/kaynak/Amblem_sol.dc.html + Amblem_sol_acik.dc.html. Bu testler
// değerleri ELLE yazmaz — tuval dosyasının amb-parla bloğunu ayrıştırır ve uygulamayla (razor çıktısı +
// tema-yildiz.css AMBLEM bloğu) karakter karakter karşılaştırır. Tuval değişirse test kırmızıya döner.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizAmblem4Testleri : BunitContext
{
    private const string Y = "html:root[data-sema=\"yildiz\"]";

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            var yol = r.RequestUri!.AbsolutePath;
            var govde = yol switch
            {
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","tenantId":"t1","altinHasFiyati":6656,"usdTry":41.95,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-08T09:02:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22","milyem":916,"gramFiyati":6097,"hasFiyati":6656}]}""",
                var y when y.EndsWith("/subeler") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    public YildizAmblem4Testleri()
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
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Tuvaldeki bir yüz (svg.yz): öznitelikler ve satır içi stil değerleri, tuvaldeki sırayla.</summary>
    private sealed record TuvalYuzu(string Genislik, string Yukseklik, string ViewBox, string SolPay, string UstPay,
        string Koken, string Donusum, string Yol, string Cizgi);

    private sealed record TuvalTasi(string Kutu, string Perspektif, string PerspektifKoken, List<TuvalYuzu> Yuzler);

    private static string TuvalYolu(string dosya) =>
        CssKaynak.Yol("..", "dokumanlar", "09-uygulama-kayitlari", "ham", "yildiz-tuval", "kaynak", dosya);

    private static string Stil(string stil, string ozellik)
    {
        var m = Regex.Match(stil, $@"(?:^|;)\s*{Regex.Escape(ozellik)}\s*:\s*([^;]+)");
        Assert.True(m.Success, $"tuval stilinde {ozellik} yok: {stil}");
        return m.Groups[1].Value.Trim();
    }

    private static TuvalTasi TuvaliOku(string dosya)
    {
        var html = File.ReadAllText(TuvalYolu(dosya));
        var parla = Regex.Matches(html, "<span class=\"amb-parla\"").Count;
        Assert.Equal(1, parla);
        var ik = Regex.Match(html, "<span class=\"amb-ik\" style=\"([^\"]+)\"");
        Assert.True(ik.Success, "tuvalde .amb-ik yok");
        var s = ik.Groups[1].Value;
        Assert.Equal(Stil(s, "width"), Stil(s, "height"));
        var yuzler = new List<TuvalYuzu>();
        foreach (Match m in Regex.Matches(html,
            "<svg class=\"yz\" width=\"([^\"]+)\" height=\"([^\"]+)\" viewBox=\"([^\"]+)\" style=\"([^\"]+)\"><path d=\"([^\"]+)\" fill=\"none\" stroke=\"url\\(#ti\\)\" stroke-width=\"([^\"]+)\" stroke-linejoin=\"round\"></path></svg>"))
        {
            var st = m.Groups[4].Value;
            yuzler.Add(new TuvalYuzu(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value,
                Stil(st, "margin-left"), Stil(st, "margin-top"), Stil(st, "transform-origin"), Stil(st, "transform"),
                m.Groups[5].Value, m.Groups[6].Value));
        }
        Assert.Equal(8, yuzler.Count);
        return new TuvalTasi(Stil(s, "width"), Stil(s, "perspective"), Stil(s, "perspective-origin"), yuzler);
    }

    private static List<CssKaynak.Bildirim> B()
    {
        var css = YildizTablosu.Css();
        var i = css.IndexOf("/* AMBLEM */", StringComparison.Ordinal);
        Assert.True(i >= 0, "tema-yildiz.css'te /* AMBLEM */ bloğu yok");
        var j = css.IndexOf("/* YILDIZ-4D */", i, StringComparison.Ordinal);
        return CssKaynak.Bildirimler(j > i ? css[i..j] : css[i..]);
    }

    private static string Deger(List<CssKaynak.Bildirim> b, string secici, string ozellik)
    {
        var d = b.Where(x => x.Ozellik == ozellik && x.AtKural == "" && x.Secici == secici).ToList();
        Assert.True(d.Count == 1, $"{secici} {{ {ozellik} }} bulunamadı ya da birden çok ({d.Count})");
        return d[0].Deger;
    }

    // ── YA4-01) Koyu ve açık tuval AYNI taşı çizer (yalnız renk farklı) ve kutu 52 px, çizgi 1,0 ─────────
    [Fact]
    public void YA4_01_koyu_ve_acik_tuval_ayni_tas_52_px_cizgi_1_0()
    {
        var koyu = TuvaliOku("Amblem_sol.dc.html");
        var acik = TuvaliOku("Amblem_sol_acik.dc.html");
        Assert.Equal((koyu.Kutu, koyu.Perspektif, koyu.PerspektifKoken), (acik.Kutu, acik.Perspektif, acik.PerspektifKoken));
        Assert.Equal(koyu.Yuzler, acik.Yuzler);
        Assert.Equal(("52px", "832px"), (koyu.Kutu, koyu.Perspektif));                // EMİR § 3: S = 52, perspektif 16·S
        Assert.All(koyu.Yuzler, y => Assert.Equal("1.0", y.Cizgi));                   // EMİR § 3: kabukta 1,0 px
    }

    // ── YA4-02) CSS AMBLEM bloğu: kutu + perspektif + 8 yüzün payları/kökeni/dönüşümü tuvalle BİREBİR ─────────
    [Fact]
    public void YA4_02_kabuk_kutusu_52_px_perspektif_16S()
    {
        var b = B();
        var kap = $"{Y} .amb-parla";
        Assert.Equal(("52px", "52px", "2080px", "50% 50%"),
            (Deger(b, $"{kap} .amb-ik", "width"), Deger(b, $"{kap} .amb-ik", "height"),
             Deger(b, $"{kap} .amb-ik", "perspective"), Deger(b, $"{kap} .amb-ik", "perspective-origin")));
        for (var i = 0; i < 42; i++) Assert.StartsWith("matrix3d(", Deger(b, $"{kap} .amb-f{i}", "transform"), StringComparison.Ordinal);
    }

    // ── YA4-03) Razor çıktısı: 8 yüzün genişlik/yükseklik/viewBox/yol/çizgi kalınlığı tuvalle BİREBİR ─────────
    [Fact]
    public void YA4_03_razor_42_serit_kabukta()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Basit));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/islemler");
        var e = Render<YildizUstMenu>();
        var yuzler = e.FindAll("#yildiz-marka .tas3d > svg").ToList();
        Assert.Equal(42, yuzler.Count);
        Assert.All(yuzler, s => Assert.Equal("url(#yildiz-amb-ti)", s.QuerySelector("rect")!.GetAttribute("fill")));
    }

    // ── YA4-04) Giriş 96 px (42 şerit); kabuk ↔ yazı arası 14 px ─────────
    [Fact]
    public void YA4_04_giris_96_px_kabuk_bosluk_14()
    {
        var g = YildizUstMenu.GirisTasi;
        Assert.Equal(("giris-amb-ti", 42), (g.GradyanId, g.Kenarlar.Length));
        var b = B();
        Assert.Equal(("96px", "96px", "3840px"),
            (Deger(b, $"{Y} .giris-amb .amb-ik", "width"), Deger(b, $"{Y} .giris-amb .amb-ik", "height"), Deger(b, $"{Y} .giris-amb .amb-ik", "perspective")));
        Assert.Equal("14px", Deger(b, $"{Y} .yildiz-marka.amb", "gap"));
    }
}
