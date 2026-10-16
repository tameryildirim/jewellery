using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sayim;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-SAYIM — ekran denetimi 10.10 (kullanıcı gözü) K5 + Y10.
//   K5: barkod okut + Enter SAYILMIYORDU — "Adet (isteğe bağlı)" alanı "Bu alan zorunludur" diyordu.
//       Kural: okut + Enter = 1 adet (adet boşsa 1) · aynı barkod tekrar = +1 · adet yazılmışsa o adet ·
//       ADET biriminde kesir yok (UrunAdetKapisi). Sunucu: SayimSayDto.Artis (mevcut sayılana ekler).
//   Y10: "Sayım başlat" adı tarih-saatle OTOMATİK verir; Yıldız'da tek şubede form sormadan okutmaya geçer.
// Sayım kapanışı/düzeltme (SayimServisi.Kapat) DEĞİŞMEDİ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PSayimServisTestleri(TestVeritabani vt)
{
    private static SayimServisi Servis(KuyumcuDbContext db) => new(db, new MUrun.UrunServisi(db));
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperty(ad)!.GetValue(o)!;
    private static decimal? Sayilan(object detay, string urunId)
    {
        var s = ((System.Collections.IList)Prop<object>(detay, "satirlar")).Cast<object>()
            .Single(x => (string)x.GetType().GetProperty("urunId")!.GetValue(x)! == urunId);
        return (decimal?)s.GetType().GetProperty("sayilanAdet")!.GetValue(s);
    }

    private async Task<(Kiraci K, string OturumId)> Kur(string ad, string barkod, bool gram = false)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PSAYIM " + ad, stokAdet: 5m);
        using (var db = vt.OlusturSistem())
        {
            db.Urunler.Single(u => u.Id == k.UrunId).Barkod = barkod;
            db.SaveChanges();
            if (gram) TestKurulum.UrunuGramBirimiYap(db, k.UrunId);
        }
        using var adb = vt.Olustur(k.TenantId);
        var d = await Servis(adb).Ac(new SayimAcDto { Ad = "Vitrin", SubeId = k.SubeId, KapsamTur = "TUMU" }, k.Islem);
        return (k, Prop<string>(d, "Id"));
    }

    [Fact]
    public async Task PS_01_okut_artis_bos_satira_1_yazar_ayni_barkod_tekrar_arttirir()
    {
        var (k, id) = await Kur("okut", "869PS01");
        object d = null!;
        for (var i = 0; i < 3; i++)
        {
            using var db = vt.Olustur(k.TenantId);
            d = await Servis(db).Say(id, new SayimSayDto { Barkod = "869PS01", Artis = 1m }, k.Islem);
            Assert.Equal(i + 1m, Sayilan(d, k.UrunId));
        }
        Assert.Equal(3m, Sayilan(d, k.UrunId));
    }

    [Fact]
    public async Task PS_02_yazilan_adet_MUTLAK_kalir_artis_onun_ustune_ekler()
    {
        var (k, id) = await Kur("mutlak", "869PS02");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { Barkod = "869PS02", SayilanAdet = 5m }, k.Islem);
        var d = await Servis(db).Say(id, new SayimSayDto { Barkod = "869PS02", Artis = 1m }, k.Islem);
        Assert.Equal(6m, Sayilan(d, k.UrunId));
        d = await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 2m }, k.Islem);
        Assert.Equal(2m, Sayilan(d, k.UrunId));   // adet yazılmışsa o kadar (üstüne yazar)
    }

    [Fact]
    public async Task PS_03_ADET_biriminde_kesirli_artis_RED_ve_satir_degismez()
    {
        var (k, id) = await Kur("kesir", "869PS03");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { Barkod = "869PS03", Artis = 1m }, k.Islem);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { Barkod = "869PS03", Artis = 1.5m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        using var kontrol = vt.Olustur(k.TenantId);
        Assert.Equal(1m, Sayilan(await Servis(kontrol).Detay(id, k.Islem), k.UrunId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PS_04_sifir_ya_da_eksi_artis_RED(int artis)
    {
        var (k, id) = await Kur("sifir" + artis, "869PS04" + (artis + 5));
        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = artis }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        Assert.Null(Sayilan(await Servis(db).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task PS_05_artis_ile_mutlak_adet_BIRLIKTE_gelemez()
    {
        var (k, id) = await Kur("ikisi", "869PS05");
        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1m, SayilanAdet = 4m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        Assert.Null(Sayilan(await Servis(db).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task PS_06_GRAM_biriminde_kesirli_artis_kabul()
    {
        var (k, id) = await Kur("gram", "869PS06", gram: true);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1.25m }, k.Islem);
        var d = await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0.5m }, k.Islem);
        Assert.Equal(1.75m, Sayilan(d, k.UrunId));
    }

    [Fact]
    public async Task PS_07_artis_kolon_tavanini_asarsa_400_alan_artis()
    {
        var (k, id) = await Kur("tavan", "869PS07");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 999_999_999m }, k.Islem);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
    }
}

[Collection("http")]
public sealed class PSayimHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task PS_H1_okut_artis_uc_kez_200_sayilan_3_kesir_ve_bos_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PSAYIM http", stokAdet: 2m);
            db.Urunler.Single(u => u.Id == k.UrunId).Barkod = "869PSH1";
            db.SaveChanges();
        }
        var c = f.Ortam.Istemci(k);
        var ac = await c.PostJson("/api/v1/sayim", new { ad = "Sayım 10.10.2026 23:40", subeId = k.SubeId, kapsamTur = "TUMU" });
        Assert.Equal(HttpStatusCode.OK, ac.StatusCode);
        var id = (await ac.Json()).GetProperty("id").GetString()!;

        JsonElement son = default;
        for (var i = 0; i < 3; i++)
        {
            var r = await c.PostJson($"/api/v1/sayim/{id}/say", new { barkod = "869PSH1", artis = 1 });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            son = await r.Json();
        }
        var satir = son.GetProperty("satirlar").EnumerateArray().Single(s => s.GetProperty("urunId").GetString() == k.UrunId);
        Assert.Equal(3m, satir.GetProperty("sayilanAdet").GetDecimal());
        Assert.Equal(1m, satir.GetProperty("fark").GetDecimal());

        var kesir = await c.PostJson($"/api/v1/sayim/{id}/say", new { barkod = "869PSH1", artis = 1.5 });
        Assert.Equal(HttpStatusCode.BadRequest, kesir.StatusCode);
        var kesirJson = await kesir.Json();
        Assert.Contains("artis", kesirJson.ToString(), StringComparison.OrdinalIgnoreCase);

        // Gövdede ne adet ne artış: eski kural AYNEN ("Sayılan adet girilmedi").
        var bos = await c.PostJson($"/api/v1/sayim/{id}/say", new { barkod = "869PSH1" });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        Assert.Equal("Sayılan adet girilmedi", (await bos.Json()).GetProperty("message").GetString());

        // Kesir/boş red sonrası satır 3'te kaldı.
        var d = await (await c.GetAsync($"/api/v1/sayim/{id}")).Json();
        Assert.Equal(3m, d.GetProperty("satirlar").EnumerateArray().Single(s => s.GetProperty("urunId").GetString() == k.UrunId)
            .GetProperty("sayilanAdet").GetDecimal());
    }
}

/// <summary>bUnit — ekran sözleşmesi: okut+Enter=1, tekrar=+1, yazılan adet mutlak, gramda gram istenir,
/// otomatik ad, Yıldız'da "Sayım başlat" doğrudan okutmaya geçer. Sahte API durumlu: sayılanı gerçekten tutar.</summary>
public sealed class PSayimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Dictionary<string, decimal?> sayilan = new() { ["u1"] = null, ["u2"] = null };
    private bool yildiz;
    private bool oturumVar = true;
    private bool reyonVar;

    private sealed class SahteApi(PSayimEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var post = istek.Method == HttpMethod.Post;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (post && yol.EndsWith("/sayim/o1/say"))
            {
                var g = JsonDocument.Parse(govde).RootElement;
                var kod = g.TryGetProperty("barkod", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() : null;
                var uid = kod switch { "869001" => "u1", "869002" => "u2", _ => g.GetProperty("urunId").GetString()! };
                if (g.TryGetProperty("artis", out var a) && a.ValueKind == JsonValueKind.Number)
                    t.sayilan[uid] = (t.sayilan[uid] ?? 0m) + a.GetDecimal();
                else t.sayilan[uid] = g.GetProperty("sayilanAdet").GetDecimal();
            }
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => t.yildiz ? """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""" : """{"sema":"platin"}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var v when v.Contains("/reyonlar") => t.reyonVar ? """[{"id":"r1","ad":"Vitrin"}]""" : "[]",
                var v when v.Contains("/urun-kategorileri/duz") => "[]",
                var v when v.EndsWith("/sayim") && post => t.Detay(),
                var v when v.EndsWith("/sayim") => t.oturumVar ? """[{"id":"o1","ad":"Vitrin","kapsamTur":"TUMU","durum":"ACIK"}]""" : "[]",
                var v when v.Contains("/sayim/o1") => t.Detay(),
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private string Detay()
    {
        static string N(decimal? d) => d is { } x ? x.ToString(CultureInfo.InvariantCulture) : "null";
        string F(decimal sistem, decimal? s) => s is { } x ? (x - sistem).ToString(CultureInfo.InvariantCulture) : "null";
        var sayilanSayi = sayilan.Values.Count(v => v is not null);
        return $$"""
            {"id":"o1","ad":"Vitrin","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":2,"sayilan":{{sayilanSayi}},"sayilmayan":{{2 - sayilanSayi}},
             "farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"acildi":"2026-10-10T14:05:00Z",
             "satirlar":[
              {"urunId":"u1","kod":"BB1","ad":"Burma Bilezik","barkod":"869001","birim":"ADET","sistemAdet":5,"sayilanAdet":{{N(sayilan["u1"])}},"guncelAdet":5,"fark":{{F(5, sayilan["u1"])}},"oturumdaHareketVar":false},
              {"urunId":"u2","kod":"HZ1","ad":"Hurda Zincir","barkod":"869002","birim":"GRAM","sistemAdet":12.5,"sayilanAdet":{{N(sayilan["u2"])}},"guncelAdet":12.5,"fark":{{F(12.5m, sayilan["u2"])}},"oturumdaHareketVar":false}]}
            """;
    }

    public PSayimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private void Hazirla(bool yildizMi, string mod = "BASIT")
    {
        yildiz = yildizMi;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", mod, mod));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
    }

    private List<string> SayGovdeleri()
    {
        lock (istekler) return [.. istekler.Where(i => i.Metot == "POST" && i.Yol.EndsWith("/sayim/o1/say")).Select(i => i.Govde)];
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> KlasikOturum()
    {
        Hazirla(false);
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        e.WaitForElement("#sy-tablo tbody tr", TimeSpan.FromSeconds(10));
        e.Find("#sy-tablo tbody tr button").Click();
        e.WaitForElement("#sy-barkod", TimeSpan.FromSeconds(10));
        return e;
    }

    private static void OkutEnter(IRenderedComponent<Kuyumcu.Web.Pages.Sayim> e, string barkod)
    {
        e.Find("#sy-barkod").Input(barkod);
        e.Find("#sy-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    }

    [Fact]
    public void PS_B1_okut_Enter_uc_kez_UC_sayilir_adet_hatasi_YOK()
    {
        var e = KlasikOturum();
        for (var i = 1; i <= 3; i++)
        {
            OkutEnter(e, "869001");
            var beklenen = i;
            e.WaitForAssertion(() => Assert.Equal(beklenen, SayGovdeleri().Count), TimeSpan.FromSeconds(10));
        }
        foreach (var g in SayGovdeleri())
        {
            var j = JsonDocument.Parse(g).RootElement;
            Assert.Equal(1m, j.GetProperty("artis").GetDecimal());
            Assert.False(j.TryGetProperty("sayilanAdet", out _), "boş adette mutlak adet gitmemeli: " + g);
            Assert.Equal("869001", j.GetProperty("barkod").GetString());
        }
        Assert.Equal(3m, sayilan["u1"]);
        var dil = Services.GetRequiredService<DilServisi>();
        e.WaitForAssertion(() => Assert.Contains("× 3", e.Find("#basari-bandi").TextContent), TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#alan-hata-sy-adet"));
        Assert.DoesNotContain(dil.T.Kabuk.ZorunluAlan, e.Markup);
        Assert.Equal("", e.Find("#sy-barkod").GetAttribute("value") ?? "");
        // Etiket ile kural aynı şeyi söyler: alan "isteğe bağlı".
        Assert.Contains(dil.T.Ortak.Istege, e.Find("label[for=sy-adet]").TextContent);
    }

    [Fact]
    public void PS_B2_adet_yazilirsa_o_adet_MUTLAK_gider()
    {
        var e = KlasikOturum();
        e.Find("#sy-barkod").Input("869001");
        e.Find("#sy-adet").Input("4");
        e.Find("#sy-say").Click();
        e.WaitForAssertion(() => Assert.Single(SayGovdeleri()), TimeSpan.FromSeconds(10));
        var j = JsonDocument.Parse(SayGovdeleri()[0]).RootElement;
        Assert.Equal(4m, j.GetProperty("sayilanAdet").GetDecimal());
        Assert.False(j.TryGetProperty("artis", out _));
        OkutEnter(e, "869001");
        e.WaitForAssertion(() => Assert.Equal(2, SayGovdeleri().Count), TimeSpan.FromSeconds(10));
        Assert.Equal(5m, sayilan["u1"]);
    }

    [Fact]
    public void PS_B3_gramli_urunde_bos_adet_ISTEK_ATMAZ_alan_altinda_gram_ister()
    {
        var e = KlasikOturum();
        OkutEnter(e, "869002");
        var dil = Services.GetRequiredService<DilServisi>();
        e.WaitForAssertion(() => Assert.Equal(dil.T.Sayim.GramMiktarYaz, e.Find("#alan-hata-sy-adet").TextContent.Trim()), TimeSpan.FromSeconds(10));
        Assert.Empty(SayGovdeleri());
        // Gram yazılınca gider (kesir serbest).
        e.Find("#sy-adet").Input("2,5");
        e.Find("#sy-say").Click();
        e.WaitForAssertion(() => Assert.Single(SayGovdeleri()), TimeSpan.FromSeconds(10));
        Assert.Equal(2.5m, sayilan["u2"]);
    }

    [Fact]
    public void PS_B4_bos_barkodla_Enter_istek_atmaz_barkod_alaninda_hata()
    {
        var e = KlasikOturum();
        OkutEnter(e, "");
        var dil = Services.GetRequiredService<DilServisi>();
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-sy-barkod").TextContent)), TimeSpan.FromSeconds(10));
        Assert.Empty(SayGovdeleri());
        Assert.Empty(e.FindAll("#alan-hata-sy-adet"));
    }

    [Fact]
    public void PS_B5_yeni_sayim_adi_tarih_saatle_OTOMATIK_dolu()
    {
        oturumVar = false;
        Hazirla(false);
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        e.WaitForElement("#sy-yeni", TimeSpan.FromSeconds(10)).Click();
        var ad = e.Find("#sy-ad").GetAttribute("value") ?? "";
        Assert.Matches(@"^Sayım \d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", ad);
        e.Find("#sy-ac").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/sayim")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/sayim")).Govde).RootElement;
        Assert.Equal(ad, g.GetProperty("ad").GetString());
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PRO")]
    public void PS_B6_yildiz_sayim_baslat_FORM_SORMADAN_acar_ve_okut_Enter_uc_kez_UC(string mod)
    {
        oturumVar = false;
        Hazirla(true, mod);
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        e.WaitForElement("#yildiz-sayim-b1", TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#yildiz-sayim-b2"));          // reyon/kategori yok → ikincil seçenek gösterilmez
        e.Find("#yildiz-sayim-b1").Click();                  // "Sayım başlat"
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/sayim")), TimeSpan.FromSeconds(10));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/sayim")).Govde).RootElement;
        Assert.Matches(@"^Sayım \d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", g.GetProperty("ad").GetString());
        Assert.Equal("TUMU", g.GetProperty("kapsamTur").GetString());
        Assert.Equal("s1", g.GetProperty("subeId").GetString());
        e.WaitForElement("#yildiz-sayim-okut #sy-barkod", TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#yildiz-sayim-form"));       // form ara adımı yok
        for (var i = 1; i <= 3; i++)
        {
            OkutEnter(e, "869001");
            var beklenen = i;
            e.WaitForAssertion(() => Assert.Equal(beklenen, SayGovdeleri().Count), TimeSpan.FromSeconds(10));
        }
        Assert.Equal(3m, sayilan["u1"]);
        e.WaitForAssertion(() => Assert.Equal("1", e.Find("#yildiz-sayim-rakam").TextContent.Split(' ')[0].Trim()), TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#alan-hata-sy-adet"));
    }

    [Fact]
    public void PS_B7_yildiz_reyon_varsa_ikincil_secenek_formu_otomatik_adla_acar()
    {
        oturumVar = false; reyonVar = true;
        Hazirla(true);
        var e = Render<Kuyumcu.Web.Pages.Sayim>();
        var dil = Services.GetRequiredService<DilServisi>();
        e.WaitForAssertion(() => Assert.Equal(dil.T.Sayim.BolumleBaslat, e.Find("#yildiz-sayim-b2").TextContent.Trim()), TimeSpan.FromSeconds(10));
        e.Find("#yildiz-sayim-b2").Click();
        e.WaitForElement("#yildiz-sayim-form", TimeSpan.FromSeconds(10));
        Assert.Matches(@"^Sayım \d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", e.Find("#sy-ad").GetAttribute("value") ?? "");
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}
