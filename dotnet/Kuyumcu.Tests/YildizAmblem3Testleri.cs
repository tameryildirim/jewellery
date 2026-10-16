using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-AMBLEM-3 — Yıldız giriş ekranı tuval Giris4 / Giris4a BİREBİR + oturum öncesi şema hatırlama.
//   • Hakem KRİTİK-1: amblem tuvalde y=236, uygulamada 166 idi; form etiket/alan düzeni tuvalden farklıydı.
//   • Hakem KRİTİK-2: oturumsuz /giris hep eski (Platin) girişti — şema oturumdan önce bilinmiyordu.
//   • Ekip oyu: giriş öncesi şema = BU CİHAZDA en son kullanılan şema (tema.js 'mihenk-son-sema'); hiç giriş
//     yoksa varsayılan (Platin); çıkışta anahtar kalır; yalnız görünüm.
// Görsel eşitliğin ASIL kanıtı başsız Chrome'da tuval ↔ uygulama ölçümüdür (rapor). Bu sınıf sözleşmeyi kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizAmblem3Testleri : BunitContext
{
    private const string Y = "html:root[data-sema=\"yildiz\"] .giris-yildiz";
    private bool mfaIstensin;
    private string gorunumSema = "yildiz";

    private sealed class SahteApi(Func<bool> mfa, Func<string> sema) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kullanici/gorunum") => $$"""{"temaModu":"karanlik","sema":"{{sema()}}","yaziBoyutu":18}""",
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{sema()}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.EndsWith("/genel/kisitlar") =>
                    """{"kaynak":"docs/HARIC-LISTESI.md","toplam":1,"kisitlar":[{"kod":"H-1","baslik":"e-Fatura gönderimi","baslikEn":"e-Invoice","durum":"HARIC"}]}""",
                var y when y.EndsWith("/auth/giris") && mfa() => """{"mfaGerekli":true,"mfaToken":"m1"}""",
                var y when y.EndsWith("/auth/giris") =>
                    """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""",
                _ => """{"mesaj":"ok"}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public YildizAmblem3Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => mfaIstensin, () => gorunumSema)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Soğuk açılış: sunucu görünümü OKUNMADI; cihazdaki son şema = <paramref name="sonSema"/> (null = anahtar yok).</summary>
    private IRenderedComponent<Giris> SogukGiris(string? sonSema)
    {
        JSInterop.Setup<string?>("kuyumcuGorunum.sonSema").SetResult(sonSema);
        return Render<Giris>();
    }

    private static List<CssKaynak.Bildirim> B()
    {
        var css = YildizTablosu.Css();
        var i = css.IndexOf("/* GIRIS */", StringComparison.Ordinal);
        var j = css.IndexOf("/* YILDIZ-4D */", StringComparison.Ordinal);
        Assert.True(i >= 0 && j > i, "tema-yildiz.css'te /* GIRIS */ bloğu (YILDIZ-4D'den önce) yok");
        return CssKaynak.Bildirimler(css[i..j]);
    }

    private static string Deger(List<CssKaynak.Bildirim> b, string secici, string ozellik, string atKural = "")
    {
        var d = b.Where(x => x.Ozellik == ozellik && x.AtKural == atKural && x.Secici == secici).ToList();
        Assert.True(d.Count == 1, $"{atKural} {secici} {{ {ozellik} }} bulunamadı ya da birden çok ({d.Count})");
        return d[0].Deger;
    }

    private static string TemaJs() => CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "tema.js"));

    // ── YA3-01) Soğuk açılış + cihazdaki son şema Yıldız → tuval Giris4 düzeni (KRİTİK-2) ─────────
    [Fact]
    public void YA3_01_soguk_acilista_son_sema_yildiz_ise_yildiz_girisi_cizilir()
    {
        var e = SogukGiris("yildiz");
        Assert.Contains("giris-yildiz", e.Find("#giris-sayfa").ClassList);
        Assert.Single(e.FindAll("#giris-amblem"));
        Assert.Empty(e.FindAll("#giris-panel"));
        Assert.Empty(e.FindAll("#giris-marka"));
        Assert.Single(JSInterop.Invocations["kuyumcuGorunum.sonSema"]);
    }

    // ── YA3-02) Hiç giriş yapılmamış cihaz (anahtar yok) / başka şema → varsayılan (Platin) giriş AYNEN ─────────
    [Theory]
    [InlineData(null)]
    [InlineData("platin")]
    [InlineData("bordo")]
    [InlineData("YILDIZ-degil")]
    public void YA3_02_anahtar_yoksa_ya_da_baska_semaysa_eski_giris_aynen(string? son)
    {
        var e = SogukGiris(son);
        Assert.Equal("giris-sayfa giris-iki-panel", e.Find("#giris-sayfa").GetAttribute("class"));
        Assert.Single(e.FindAll("#giris-marka"));
        Assert.Single(e.FindAll("#giris-panel"));
        Assert.Empty(e.FindAll("#giris-amblem, #giris-alt, #giris-yildiz-satir"));
    }

    // ── YA3-03) Sunucu görünümü bu sekmede OKUNDUYSA o esastır (cihaz önbelleği sunucuyu ezmez) ─────────
    [Theory]
    [InlineData("platin", "yildiz", false)]   // önbellek Yıldız ama sunucu Platin dedi → eski giriş
    [InlineData("yildiz", null, true)]        // Yıldız oturumundan çıkış (aynı sekme), önbellek yok → Yıldız
    [InlineData("yildiz", "platin", true)]
    public async Task YA3_03_sunucu_gorunumu_okunduysa_o_esastir(string sunucu, string? son, bool yildizBeklenen)
    {
        gorunumSema = sunucu;
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        var e = SogukGiris(son);
        Assert.Equal(yildizBeklenen, e.Find("#giris-sayfa").ClassList.Contains("giris-yildiz"));
    }

    // ── YA3-04) Tuval Giris4 formu: etiket + alan satırı ×2, yer tutucular tuvaldeki gibi, tek satır unuttum | giriş ─────────
    [Fact]
    public void YA3_04_yildiz_formu_tuval_duzeni_yer_tutucular_ve_tek_satir()
    {
        var e = SogukGiris("yildiz");
        var form = e.Find("#giris-form");
        var cocuk = form.Children.Select(c => c.Id).ToList();
        Assert.Equal(new[] { "giris-eposta-sar", "giris-sifre-sar", "giris-yildiz-satir" }, cocuk);
        Assert.Equal("ad@isletme.com", e.Find("#giris-eposta").GetAttribute("placeholder"));
        Assert.Equal("••••••••", e.Find("#giris-sifre").GetAttribute("placeholder"));
        var satir = e.Find("#giris-yildiz-satir").Children.Select(c => c.Id).ToList();
        Assert.Equal(new[] { "giris-parola-unuttum", "giris-gonder" }, satir);
        Assert.Equal("submit", e.Find("#giris-gonder").GetAttribute("type"));
        Assert.Empty(e.FindAll("#giris-baglar"));
        // Göster tuval kalıbında, kutunun içinde (S1-e); davranış aynı
        Assert.Single(e.FindAll("#giris-sifre-kutu > #giris-sifre-goster"));
        foreach (var id in new[] { "giris-form", "giris-eposta", "giris-sifre", "giris-gonder", "giris-parola-unuttum", "giris-yeni-dukkan", "giris-surum", "giris-kisit", "giris-dil" })
            Assert.Single(e.FindAll($"#{id}"));
    }

    // ── YA3-05) Tuvalde OLMAYAN öğeler akıştan çıkar, ÖZELLİK SİLİNMEZ: alt bilgi satırı (Yeni dükkân kur · sürüm · HARİÇ) ─────────
    [Fact]
    public void YA3_05_haric_satiri_yeni_dukkan_ve_surum_yildizda_alt_bilgi_satirinda()
    {
        var e = SogukGiris("yildiz");
        var alt = e.Find("#giris-alt");
        Assert.Equal(new[] { "giris-yeni-dukkan", "giris-surum", "giris-kisit" }, alt.Children.Select(c => c.Id).ToList());
        Assert.Null(e.Find("#giris-kisit").GetAttribute("open"));               // kapalı başlar (hata yokken)
        Assert.Equal(e.Find("#giris-kart"), alt.ParentElement);
        // "Yeni dükkân kur" çalışır: kayıt sekmesine geçer; kayıtta alt satırda yeni dükkân düğmesi YOK
        e.Find("#giris-yeni-dukkan").Click();
        Assert.Single(e.FindAll("#kayit-form"));
        Assert.Empty(e.FindAll("#giris-alt #giris-yeni-dukkan"));
        Assert.Single(e.FindAll("#giris-alt #giris-surum"));
    }

    // ── YA3-06) Öteki şemalar: form ve kart DOM'u AYNEN (gönder → bağlar[unuttum, yeni dükkân]; sürüm + kısıt kartın çocuğu) ─────────
    [Fact]
    public void YA3_06_oteki_semalarda_dom_sirasi_ve_yer_tutucular_aynen()
    {
        var e = SogukGiris(null);
        var form = e.Find("#giris-form");
        Assert.Equal(new[] { "giris-eposta-sar", "giris-sifre-sar", "giris-gonder", "giris-baglar" }, form.Children.Select(c => c.Id).ToList());
        Assert.Equal(new[] { "giris-parola-unuttum", "giris-yeni-dukkan" }, e.Find("#giris-baglar").Children.Select(c => c.Id).ToList());
        Assert.Equal("ornek@isletme.com", e.Find("#giris-eposta").GetAttribute("placeholder"));
        Assert.Null(e.Find("#giris-sifre").GetAttribute("placeholder"));
        var kart = e.Find("#giris-kart").Children.Select(c => c.Id).Where(s => !string.IsNullOrEmpty(s)).ToList();
        Assert.Equal(new[] { "giris-form", "giris-surum", "giris-kisit" }, kart.SkipWhile(s => s != "giris-form").ToList());
    }

    // ── YA3-07) MFA ikinci adımı (C4_62 kurtarma kodu alanı) soğuk Yıldız girişinde de DEĞİŞMEZ ─────────
    [Fact]
    public void YA3_07_soguk_yildiz_girisinde_MFA_ikinci_adimi_aynen()
    {
        mfaIstensin = true;
        var e = SogukGiris("yildiz");
        e.Find("#giris-eposta").Input("qa@ornek.com");
        e.Find("#giris-sifre").Input("Parola12345");
        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#giris-mfa-kod")));
        Assert.Equal("one-time-code", e.Find("#giris-mfa-kod").GetAttribute("autocomplete"));
        Assert.Single(e.FindAll("#giris-mfa-dogrula"));
        Assert.Single(e.FindAll("#giris-mfa-vazgec"));
        Assert.Empty(e.FindAll("#giris-alt #giris-yeni-dukkan"));   // MFA adımında kayıt bağlantısı çizilmez (eskisi gibi)
        Assert.Single(e.FindAll("#giris-amblem"));
    }

    // ── YA3-08) Boş gönderim: alan altı Türkçe mesaj (sessiz etkisizlik yok) Yıldız düzeninde de ─────────
    [Fact]
    public void YA3_08_bos_gonderimde_alan_alti_mesaj_yildizda_da()
    {
        var e = SogukGiris("yildiz");
        e.Find("#giris-form").Submit();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#giris-eposta-sar .alan-alt *")));
        Assert.Contains("alan-hatali", e.Find("#giris-eposta-sar").ClassList);
    }

    // ── YA3-09) tema.js: son şema sunucudan gelen her uygulamada YAZILIR, beyaz liste, ilk boyamadan önce uygulanır, çıkışta silinmez ─────────
    [Fact]
    public void YA3_09_tema_js_son_semayi_yazar_okur_ilk_boyamada_uygular()
    {
        var js = TemaJs();
        Assert.Contains("var SON_SEMA = 'mihenk-son-sema';", js);
        var sema = Regex.Match(js, @"function sema\(deger\) \{(?<g>[\s\S]*?)\n    \}");
        Assert.True(sema.Success, "sema() bulunamadı");
        Assert.Contains("var s = yaz('data-sema', deger, SEMALAR);", sema.Groups["g"].Value);
        Assert.Contains("if (s) localStorage.setItem(SON_SEMA, s); else localStorage.removeItem(SON_SEMA);", sema.Groups["g"].Value);
        var son = Regex.Match(js, @"function sonSema\(\) \{(?<g>[\s\S]*?)\n    \}");
        Assert.True(son.Success, "sonSema() bulunamadı");
        Assert.Contains("localStorage.getItem(SON_SEMA)", son.Groups["g"].Value);
        Assert.Contains("return SEMALAR.indexOf(d) !== -1 ? d : '';", son.Groups["g"].Value);
        Assert.Contains("window.kuyumcuGorunum = { uygula: uygula, sema: sema, olcek: olcek, sonSema: sonSema };", js);
        // İlk boyamadan önce (blazor.webassembly.js'ten önce yüklenen dosyanın gövdesinde) uygulanır
        var i = js.IndexOf("window.kuyumcuGorunum =", StringComparison.Ordinal);
        Assert.True(js.IndexOf("yaz('data-sema', sonSema(), SEMALAR);", i, StringComparison.Ordinal) > i, "son şema ilk boyamada uygulanmıyor");
        // Yalnız sema() yazar/siler: başka hiçbir yer (çıkış dahil) anahtarı silmez
        Assert.Single(Regex.Matches(js, @"removeItem\(SON_SEMA\)"));
        Assert.Single(Regex.Matches(js, @"setItem\(SON_SEMA"));
        foreach (var dosya in Directory.GetFiles(CssKaynak.Yol("Kuyumcu.Web"), "*.*", SearchOption.AllDirectories)
                     .Where(d => (d.EndsWith(".cs") || d.EndsWith(".razor") || d.EndsWith(".js")) && !d.Contains("/bin/") && !d.Contains("/obj/") && !d.EndsWith("tema.js")))
            Assert.False(Regex.IsMatch(File.ReadAllText(dosya), @"""mihenk-son-sema""|localStorage[^\n]*mihenk-son-sema"), $"{dosya} anahtara dokunuyor");
    }

    // ── YA3-10) CSS — tuval Giris4 değerleri (form, .alan, .ust, girdi, b2, b1, alt bilgi) ─────────
    [Fact]
    public void YA3_10_css_tuval_giris4_degerleri_birebir()
    {
        var b = B();
        Assert.Equal("10px", Deger(b, $"{Y} #giris-form", "gap"));
        var alan = $"{Y} #giris-form .alan";
        Assert.Equal(("grid", "102px minmax(0, 1fr)", "1px solid var(--kenarlik)", "var(--metin-soluk)"),
            (Deger(b, alan, "display"), Deger(b, alan, "grid-template-columns"), Deger(b, alan, "border-bottom"), Deger(b, alan, "color")));
        var et = $"{Y} #giris-form .alan > .alan-etiket:not(#_)";
        Assert.Equal(("57px", "0 16px 0 0"), (Deger(b, et, "height"), Deger(b, et, "padding")));
        Assert.Equal(("11px", "500 !important", ".22em", "uppercase", "var(--metin-soluk)"),
            (Deger(b, et, "font-size"), Deger(b, et, "font-weight"), Deger(b, et, "letter-spacing"), Deger(b, et, "text-transform"), Deger(b, et, "color")));
        Assert.Equal(("inline-block", "86px"), (Deger(b, $"{Y} #giris-form .alan-etiket-metin", "display"), Deger(b, $"{Y} #giris-form .alan-etiket-metin", "width")));
        var kon = $"{Y} #giris-form .alan-kontrol";
        Assert.Equal(("57px", "row"), (Deger(b, kon, "height"), Deger(b, kon, "flex-direction")));
        // Hata/ipucu varken çizgi hücrelere iner (mesaj çizginin altında), hata rengi korunur
        Assert.Equal("0", Deger(b, $"{Y} #giris-form .alan:has(> .alan-alt > *)", "border-bottom"));
        Assert.Equal("1px solid var(--kenarlik)", Deger(b, $"{Y} #giris-form .alan:has(> .alan-alt > *) > :is(.alan-etiket, .alan-kontrol)", "border-bottom"));
        Assert.Equal("var(--tehlike)", Deger(b, $"{Y} #giris-form .alan.alan-hatali > :is(.alan-etiket, .alan-kontrol)", "border-bottom-color"));
        // Parola: girdi satırın tamamı (tuval 278 px), Göster kutunun içinde sağda — aynı flex sırasında değil
        Assert.Equal(("block", "relative"), (Deger(b, $"{Y} #giris-form .alan-kontrol > .sifre-kutu", "display"), Deger(b, $"{Y} #giris-form .alan-kontrol > .sifre-kutu", "position")));
        Assert.Equal(("absolute", "44px"), (Deger(b, $"{Y} #giris-form .sifre-goster", "position"), Deger(b, $"{Y} #giris-form .sifre-goster", "min-height")));
        var gir = $"{Y} #giris-form :is(.girdi, .sifre-kutu > .girdi.sifre-kutu-girdi)";
        Assert.Equal(("19px", "300", "1px 2px", "0", "var(--metin)", "normal"),
            (Deger(b, gir, "font-size"), Deger(b, gir, "font-weight"), Deger(b, gir, "padding"), Deger(b, gir, "border"), Deger(b, gir, "color"), Deger(b, gir, "letter-spacing")));
        Assert.Equal("var(--metin-soluk)", Deger(b, $"{Y} #giris-form .girdi::placeholder", "color"));
        var sat = $"{Y} #giris-form .giris-yildiz-satir";
        Assert.Equal(("flex", "space-between", "30px"), (Deger(b, sat, "display"), Deger(b, sat, "justify-content"), Deger(b, sat, "margin-top")));
        var b2 = $"{Y} #giris-form .giris-yildiz-satir > #giris-parola-unuttum";
        Assert.Equal(("40px", "0 4px", "14px", ".04em", "1px solid var(--kenarlik)"),
            (Deger(b, b2, "height"), Deger(b, b2, "padding"), Deger(b, b2, "font-size"), Deger(b, b2, "letter-spacing"), Deger(b, b2, "border-bottom")));
        var b1 = $"{Y} #giris-form .giris-yildiz-satir > #giris-gonder";
        Assert.Equal(("56px", "0 40px", "999px", "16px", "500", ".02em", "var(--metin)", "var(--yuzey-govde)"),
            (Deger(b, b1, "height"), Deger(b, b1, "padding"), Deger(b, b1, "border-radius"), Deger(b, b1, "font-size"), Deger(b, b1, "font-weight"), Deger(b, b1, "letter-spacing"), Deger(b, b1, "background"), Deger(b, b1, "color")));
        var alt = $"{Y} .giris-alt";
        Assert.Equal(("absolute", "40px", "12px", ".2em", "uppercase", "var(--metin-soluk)"),
            (Deger(b, alt, "position"), Deger(b, alt, "bottom"), Deger(b, alt, "font-size"), Deger(b, alt, "letter-spacing"), Deger(b, alt, "text-transform"), Deger(b, alt, "color")));
        // Ortalama: alt bilgi ve dil seçici akış dışı → amblem + form tuvaldeki gibi dikeyde ortalanır (1440×900: y=236)
        Assert.Equal("relative", Deger(b, $"{Y}.giris-iki-panel", "position"));
        Assert.Equal("static", Deger(b, $"{Y} .giris-sol", "position"));
        Assert.Equal("24px", Deger(b, $"{Y} main.giris-sol", "padding-top", "@media (max-width: 768px)"));
        // Telefonda alt bilgi akışta (hata mesajları formu uzatınca binmez — ölçüldü: 375'te K9 0)
        Assert.Equal("static", Deger(b, $"{Y} .giris-alt", "position", "@media (max-width: 768px)"));
    }

    // ── YA3-11) Kapsam: GIRIS bloğu yalnız Yıldız girişine; MFA/kayıt formuna, öteki şemalara kural yok; sabit renk yok ─────────
    [Fact]
    public void YA3_11_giris_blogu_kapsami_ve_belirtec_renkleri()
    {
        var b = B();
        Assert.NotEmpty(b);
        Assert.All(b.Where(x => !x.AtKural.StartsWith("@keyframes", StringComparison.Ordinal)), x => Assert.StartsWith(Y, x.Secici));
        Assert.DoesNotContain(b, x => x.Secici.Contains("mfa", StringComparison.Ordinal) || x.Secici.Contains("kayit", StringComparison.Ordinal));
        Assert.DoesNotContain(b, x => Regex.IsMatch(x.Deger, @"#[0-9A-Fa-f]{3,8}\b|rgba?\("));
        // 3B kutuya / amblem dönüşüne dokunulmadı (AMBLEM-2 KABUK/GİRİŞ amblemi birebir kalır)
        Assert.DoesNotContain(b, x => x.Secici.Contains(".amb", StringComparison.Ordinal) || x.Secici.Contains("tas3d", StringComparison.Ordinal));
    }
}
