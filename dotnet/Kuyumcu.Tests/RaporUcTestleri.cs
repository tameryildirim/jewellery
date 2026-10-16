using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-3 (07.10, t1) — RAPOR-2 kalanları.
//   V1 Aylık "Tahsilat" = Gün sonu tanımı (açılış devri + virman GİRMEZ, ayrı alan).
//   V2 Aylık günlük kırılımda NET satış (grafik kartla aynı tanım).
//   E1 Bekleyen iş: yasal/nakit işi SATIR (Panel + Rapor merkezi AYNI bileşen), iş varken boş durum yok.
//   E2 Panel BASİT kâr satırı kapsamı söyler ("yalnız N satış · M satışın maliyeti yok").
//   E3 Panel PRO "Bugün satılan altın" = BASİT has sayısı; "Paralel defter" yok. Kur başlığı tek.
//   E4 Rapor merkezi: stopaj ayrı denenir; Kâr-zarar düğmesi menü yazımı; net has etiketi kapsamlı; tahsilat ⓘ;
//      stok yaşı kart altı cümleleri ⓘ'de; Gün sonu "Fark var" rozeti yok, iadeli günde "iade düşülmeden".
//   E5 Kâr-zarar: boş dönem boş durum; ters aralık tek hata + kart yok; maliyet yoksa grafik etiketi;
//      Ürünler "—" + tek ⓘ; eksik bandının açıklaması ⓘ'de.
//   E6 Para biçimi: tablo/kutu TL tutarı her zaman iki ondalık. Alan grafiği eksen yazıları SVG dışında.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class RaporUcHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<string> KasaAc(HttpClient c, Kiraci k, string ad, string paraBirimi = "TRY")
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static async Task Giris(HttpClient c, string kasaId, string arac, decimal tutar)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasaId}/hareket", new
        {
            yon = "GIRIS", tutar, odemeTuru = arac, aciklama = "RAPOR-3 test girişi",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    private static async Task AcilisDevri(HttpClient c, Kiraci k, string kasaId, decimal tl)
    {
        var satirlar = new[] { new { hedefId = kasaId, tlTutar = tl, hasGram = 0m } };
        var p = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new { tur = "KASA", satirlar });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        var jeton = (await p.Json()).GetProperty("provaJetonu").GetString()!;
        var u = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula", new
        {
            tur = "KASA", satirlar, provaJetonu = jeton,
            gerekce = "Eski programdan devreden acilis bakiyesi", kurSnapshotId = k.KurId,
        });
        Assert.True(u.IsSuccessStatusCode, await u.Content.ReadAsStringAsync());
    }

    // ── R3-01 · V1 (hasım VERİ, 150.000 ₺ farkı): aylık tahsilat = gün sonu tahsilatı; açılış devri ve virman ayrı ──
    [Fact]
    public async Task R3_01_Aylik_tahsilat_gun_sonu_ile_ayni_acilis_devri_ve_virman_girmez()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST r301");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa2 = await KasaAc(c, k, "R3 ikinci kasa");
        await Giris(c, k.KasaId, "NAKIT", 100m);                         // gerçek tahsilat
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "KASA", hedefId = kasa2, tutar = 300m,
        });
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());
        await AcilisDevri(c, k, kasa2, 700m);

        var bugun = Zaman.YerelBugun();
        var gun = bugun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var gs = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json()).GetProperty("tahsilat");
        var r = await c.GetAsync($"/api/v1/raporlar/aylik?yil={bugun.Year}&ay={bugun.Month}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var t = (await r.Json()).GetProperty("tahsilat");
        Assert.Equal(gs.GetProperty("toplam").GetDecimal(), t.GetProperty("toplamTl").GetDecimal());   // iki ekran AYNI sayı
        Assert.Equal(300m, t.GetProperty("virmanGiris").GetDecimal());
        Assert.Equal(700m, t.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(gs.GetProperty("acilisDevri").GetDecimal(), t.GetProperty("acilisDevri").GetDecimal());
    }

    // ── R3-03 · tur 2 (hasım ORTA "dövizli kasa"): Gün sonu USD girişini TL nakit diye SAYMAZ; aylık ile aynı sabit
    //    değer; döviz girişi para birimiyle ayrı alanda (TL'ye çevrilmez, toplanmaz) ──
    [Fact]
    public async Task R3_03_Gun_sonu_doviz_kasa_girisi_TL_tahsilata_girmez_aylik_ile_ayni()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST r303");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var usd = await KasaAc(c, k, "R3 dolar kasası", "USD");
        await Giris(c, k.KasaId, "NAKIT", 500m);
        await Giris(c, usd, "NAKIT", 100m);

        var bugun = Zaman.YerelBugun();
        var gun = bugun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var gr = await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}");
        Assert.Equal(HttpStatusCode.OK, gr.StatusCode);
        var gs = (await gr.Json()).GetProperty("tahsilat");
        var t = (await (await c.GetAsync($"/api/v1/raporlar/aylik?yil={bugun.Year}&ay={bugun.Month}")).Json()).GetProperty("tahsilat");
        Assert.Equal(500m, gs.GetProperty("toplam").GetDecimal());
        Assert.Equal(500m, gs.GetProperty("nakit").GetDecimal());
        Assert.Equal(500m, t.GetProperty("toplamTl").GetDecimal());
        var d = Assert.Single(gs.GetProperty("dovizGiris").EnumerateArray());
        Assert.Equal("USD", d.GetProperty("paraBirimi").GetString());
        Assert.Equal(100m, d.GetProperty("tutar").GetDecimal());
    }
}

[Collection("veritabani")]
public sealed class RaporUcDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static Hareket Satir(Kiraci k, string tur, decimal tl, decimal has) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, Maden = "ALTIN",
        ParaBirimi = "TRY", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    // ── R3-02 · V2: aylık gunler[bugün].netSatisTl = satış − |iade| (storno çifti dışlı); satisTl brüt kalır ──
    [Fact]
    public async Task R3_02_Aylik_gunluk_kirilim_net_satis_iade_dusulmus()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-3-TEST r302");
        sistem.Add(Satir(k, "SATIS", 10_000m, -1.5m));
        sistem.Add(Satir(k, "SATIS_IADE", -2_500m, 0.4m));
        var kaynak = Satir(k, "SATIS", 4_000m, -0.6m);
        var ters = Satir(k, "SATIS", -4_000m, 0.6m);
        ters.StornoKaynakId = kaynak.Id;
        sistem.Add(kaynak);
        sistem.Add(ters);
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var a = J(await TestKurulum.RaporServisi(db, new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db)).AylikOzet(bugun.Year, bugun.Month, k.Islem));
        var g = a.GetProperty("gunler").EnumerateArray()
            .Single(x => x.GetProperty("tarih").GetString() == bugun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Assert.Equal(10_000m, g.GetProperty("satisTl").GetDecimal());
        Assert.Equal(7_500m, g.GetProperty("netSatisTl").GetDecimal());
        Assert.Equal(a.GetProperty("netSatisTl").GetDecimal(), g.GetProperty("netSatisTl").GetDecimal());   // tek gün = ay
    }
}

public sealed class RaporUcSafTestleri
{
    // ── R3-10 · E2: "neyin eksik olduğu" metni — TR + EN, üç durum ──
    [Fact]
    public void R3_10_Maliyet_yok_metni_TR_EN()
    {
        Assert.Equal("12 satışın maliyeti yok", KarRaporSozlugu.MaliyetYok(KarRaporSozlugu.Tr, 12, 0));
        Assert.Equal("1 iadenin maliyeti yok", KarRaporSozlugu.MaliyetYok(KarRaporSozlugu.Tr, 0, 1));
        Assert.Equal("12 satış ve 1 iadenin maliyeti yok", KarRaporSozlugu.MaliyetYok(KarRaporSozlugu.Tr, 12, 1));
        Assert.Equal("no cost for 12 sale(s) and 1 return(s)", KarRaporSozlugu.MaliyetYok(KarRaporSozlugu.En, 12, 1));
        Assert.Equal("Kâr-zarar", KarRaporSozlugu.Tr.KarZararDugme);
        // tur 2 (hasım DÜŞÜK): ayın altın hareketi etiketi açılış stoğunu da söyler.
        Assert.Equal("Ayın tüm altın hareketi (açılış, sayım, emanet dâhil)", KarRaporSozlugu.Tr.AyinTumAltinHareketi);
    }

    // ── R3-11 · E4/E5 düzen kuralları (asıl kanıt tarayıcı ölçümü; burada kuralın varlığı bekçisi) ──
    [Fact]
    public void R3_11_Rapor_css_secici_genisligi_ve_akordeon_boslugu()
    {
        var yol = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Kuyumcu.Web", "wwwroot", "css", "rapor-gorunum.css");
        var css = File.ReadAllText(yol);
        Assert.Matches(new Regex(@"#rp-yil-sar\s*\{[^}]*min-width:\s*7\.5rem"), css);
        Assert.Matches(new Regex(@"#rp-ay-sar\s*\{[^}]*min-width:\s*9\.5rem"), css);
        Assert.Matches(new Regex(@"#kz-nereden,\s*#kz-duzeltme\s*\{\s*margin-top:\s*var\(--bosluk-4\)"), css);
    }
}

public sealed class RaporUcEkranTestleri : BunitContext
{
    private readonly Dictionary<string, string> ozel = new(StringComparer.Ordinal);

    public RaporUcEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new HataliUc(ozel, hatali, sorguOzel)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private readonly HashSet<string> hatali = new(StringComparer.Ordinal);
    /// <summary>RAPOR-3 tur 2: yol+sorgu birebir eşleşen yanıt (önceki ay isteği ayrı cevap alsın).</summary>
    private readonly Dictionary<string, string> sorguOzel = new(StringComparer.Ordinal);

    /// <summary>RAPOR-2 sahte API'si + istenen uçta 500 (stopaj ayrı deneme testi için).</summary>
    private sealed class HataliUc(Dictionary<string, string> ozel, HashSet<string> hatali, Dictionary<string, string> sorguOzel) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker ic = new(new RaporIkiTutarlilikTestleri.SahteApi(ozel));
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var tam = yol + istek.RequestUri.Query;
            foreach (var (anahtar, govde) in sorguOzel)
                if (tam.EndsWith(anahtar, StringComparison.Ordinal))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
                    });
            if (hatali.Any(h => yol.EndsWith(h, StringComparison.Ordinal)))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"Stopaj okunamadı"}""", System.Text.Encoding.UTF8, "application/json"),
                });
            return ic.SendAsync(istek, iptal);
        }
    }

    private void Oturum(string mod = ArayuzModuServisi.Basit) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Panel()
    {
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-sade");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#panel-iskelet-soru-bekleyen")), TimeSpan.FromSeconds(10));
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Raporlar()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.True(e.Instance.BugunYuklemesiBitti), TimeSpan.FromSeconds(10));
        return e;
    }

    private const string ListeYok = """{"saklamaYaklasan":2,"saklamaGun":30,"saklamaKuru":false,"listeTarihi":null,"listeEski":true,"listeYasiGun":null,"acikTam":1,"acikBenzerlik":0}""";

    // ── R3-20 · E1 (hasım YÜKSEK): yasal işler İKİ ekranda AYNI satırlar, ilgili ekrana bağlı; boş durum ve
    //   "üstteki uyarıda" yönlendirmesi YOK; Rapor merkezinde bant kartın içinde ikinci kez çizilmez ──
    [Fact]
    public void R3_20_Bekleyen_yasal_isler_iki_ekranda_ayni_satirlar_bos_durum_yok()
    {
        ozel["/masak/uyum-ozet"] = ListeYok;
        Oturum();
        YasalSatirlar(Panel(), "#panel-soru-bekleyen", "panel-soru-bekleyen");
        YasalSatirlar(Raporlar(), "#bugun-bekleyen", "bugun-bekleyen");
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#bugun-bekleyen-yasal-liste")));
        Assert.Empty(r.FindAll("#bugun-bekleyen #uyb-liste"));
        Assert.Empty(r.FindAll("#bugun-bekleyen #uyb-eslesme"));
    }

    private static void YasalSatirlar<T>(IRenderedComponent<T> e, string kok, string onek) where T : Microsoft.AspNetCore.Components.IComponent
    {
        {
            e.WaitForAssertion(() => Assert.NotNull(e.Find($"#{onek}-yasal-liste")));
            var liste = e.Find($"#{onek}-yasal-liste");
            // Satır düzeni (nokta · metin · sağda kaynak) her ekranda aynı ortak kuraldan (rapor-gorunum.css (17)).
            Assert.Contains("bekleyen-yasal-liste", liste.ClassName, StringComparison.Ordinal);
            Assert.Equal(KarRaporSozlugu.Tr.IsYasalListeYok, e.Find($"#{onek}-yasal-liste-eski-git").TextContent.Trim());
            Assert.Equal("/uyum?sekme=risk", e.Find($"#{onek}-yasal-liste-eski-git").GetAttribute("href"));
            Assert.Equal(string.Format(KarRaporSozlugu.Tr.IsYasalEslesme, 1), e.Find($"#{onek}-yasal-eslesme-git").TextContent.Trim());
            Assert.Equal("/uyum?sekme=saklama", e.Find($"#{onek}-yasal-saklama-git").GetAttribute("href"));
            Assert.Equal(3, liste.QuerySelectorAll("li .panel-nokta").Length);     // renkli noktalı satır
            Assert.Contains("panel-nokta--acil", e.Find($"#{onek}-yasal-eslesme .panel-nokta").ClassName, StringComparison.Ordinal);
            Assert.Empty(e.FindAll($"{kok} .bos-durum"));
            Assert.DoesNotContain("üstteki", e.Find(kok).TextContent, StringComparison.Ordinal);
        }
    }

    // ── R3-21 · E1: hiç iş yokken iki ekran da boş durumu AYNI cümleyle gösterir, satır yok ──
    [Fact]
    public void R3_21_Is_yokken_bos_durum_satir_yok()
    {
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.Contains(PanelSadeSozlugu.Tr.BekleyenYok, p.Find("#panel-soru-bekleyen").TextContent, StringComparison.Ordinal));
        Assert.Empty(p.FindAll("#panel-soru-bekleyen-yasal-liste"));
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Contains(RaporBugunSozlugu.Tr.BekleyenYok, r.Find("#bugun-bekleyen").TextContent, StringComparison.Ordinal));
        Assert.Empty(r.FindAll("#bugun-bekleyen-yasal-liste"));
    }

    // ── R3-22 · E2 (hasım ORTA): kâr satırı kapsamı söyler — "(yalnız 1 satış) · 12 satış ve 1 iadenin maliyeti yok" ──
    [Fact]
    public void R3_22_Panel_kar_satiri_kapsami_soyler()
    {
        ozel["/raporlar/kar-zarar"] = """
            {"hasilatTl":516597.32,"maliyetTl":3000,"karTl":1082.68,"karDurumu":"KISMI","satisAdedi":13,
             "maliyetiEksikSatisAdedi":12,"maliyetiEksikIadeAdedi":1,"maliyetiEksikTutarTl":500000,
             "maliyetiBilinenAdet":1,"maliyetiBilinmeyenAdet":12,"guvenilirlik":"DUSUK"}
            """;
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#panel-soru-satis-kar")));
        var metin = p.Find("#panel-soru-satis-kar").TextContent.Trim();
        Assert.Equal($"Kâr {Bicim.TlKurus(1082.68m)} (yalnız 1 satış) · 12 satış ve 1 iadenin maliyeti yok", metin);
        Assert.Contains("1.082,68 ₺", metin, StringComparison.Ordinal);       // tutar ile birim arası bölünmez
        Assert.DoesNotContain("eksik:", metin, StringComparison.Ordinal);
    }

    // ── R3-23 · E3 (hasım ORTA ×2): PRO "Bugün satılan altın" = BASİT has sayısı (net); "Paralel defter" / NET HAS GRAM yok;
    //   kur başlığı gövdede ikinci kez yok ──
    [Fact]
    public void R3_23_Panel_PRO_bugun_satilan_altin_BASIT_ile_ayni_sayi()
    {
        ozel["/raporlar/gunluk"] = RaporIkiTutarlilikTestleri.GunlukIadeli.Replace("\"netHasGramHareketi\":36.234", "\"netHasGramHareketi\":193.6992", StringComparison.Ordinal);
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        e.WaitForAssertion(() => Assert.Contains(KarRaporSozlugu.Tr.BugunSatilanAltin, e.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        var kart = e.FindAll(".stat-izgara .stat-kart").Single(k => k.TextContent.Contains(KarRaporSozlugu.Tr.BugunSatilanAltin, StringComparison.Ordinal));
        Assert.Equal(Bicim.Has(36.234m), kart.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.DoesNotContain(Bicim.Has(193.6992m), e.Find("#panel-izgara").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Dil(e).T.Panel.ParalelDefter, e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Dil(e).T.Panel.NetHasGram, e.Find("#panel-izgara").TextContent, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(e.Find("#panel-izgara").TextContent, Regex.Escape(Dil(e).T.Panel.Kur24k)));
    }

    private DilServisi Dil(IRenderedComponent<Kuyumcu.Web.Pages.Panel> _) => Services.GetRequiredService<DilServisi>();

    // ── R3-24 · E4 (hasım DÜŞÜK): stopaj okunamazsa öbür uyum sayaçları YİNE sayılır; stopaj hatası kendi bandında ──
    [Fact]
    public void R3_24_Rapor_merkezi_stopaj_ayri_denenir()
    {
        ozel["/masak/ozet"] = """{"acikBildirim":0,"suresiGecmis":2,"yaklasan":0,"son30GunTespit":0}""";
        hatali.Add("/stopaj/ozet");
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal(string.Format(KarRaporSozlugu.Tr.BekleyenUyumSatiri, 2),
            r.Find("#bugun-bekleyen-uyum-git").TextContent.Trim()));
        Assert.NotNull(r.Find("#bugun-bekleyen-stopaj-hata"));
        Assert.Empty(r.FindAll("#bugun-bekleyen-uyum-hata"));
    }

    // ── R3-25 · E6 (hasım DÜŞÜK): tutar her zaman iki ondalık — iki ekranda aynı ("180.000,00 ₺") ──
    [Fact]
    public void R3_25_Para_bicimi_iki_ondalik_iki_ekranda()
    {
        ozel["/raporlar/gunluk"] = RaporIkiTutarlilikTestleri.GunlukIadeli
            .Replace("\"toplamTl\":252277.99", "\"toplamTl\":180000", StringComparison.Ordinal);
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.Equal("180.000,00 ₺", p.Find("#panel-soru-satis-deger").TextContent.Trim()));
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal("180.000,00 ₺", r.Find("#bugun-satis-deger").TextContent.Trim()));
    }

    // ── R3-26 · E4 (hasım DÜŞÜK ×2): Gün sonu "Fark var" rozeti yok; iadeli günde faturasız satış "iade düşülmeden" der ──
    [Fact]
    public void R3_26_Gun_sonu_rozet_yok_iade_kapsam_notu()
    {
        ozel["/raporlar/gunluk"] = RaporIkiTutarlilikTestleri.GunlukIadeli;
        ozel["/raporlar/pos-fatura-mutabakati"] = """
            {"tarih":"2026-10-03","tahsilat":{"nakit":1000,"kart":0,"havale":0,"diger":0,"toplam":1000},
             "satis":{"sayisi":10,"toplamTl":259152.14,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":10,"faturasizTl":259152.14},
             "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
             "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"altinHasGrami":0,"madenDagilimi":[],"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
             "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
             "fark":259152.14,"mutabik":false,"farkTanimi":"x","uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
            """;
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#bugun-fark-var")));
        Assert.DoesNotContain(RaporBugunSozlugu.Tr.FarkVar, r.Find("#bugun-fark-var").TextContent, StringComparison.Ordinal);
        Assert.Empty(r.FindAll("#bugun-fark-var .rozet"));
        Assert.Equal($"({KarRaporSozlugu.Tr.GunSonuIadeDusulmeden})", r.Find("#bugun-fark-kapsam").TextContent.Trim());
    }

    // ── R3-27 · E6 + V2: Panel PRO alan grafiği — eksen yazıları SVG DIŞINDA (ezilmez); seri NET satış ──
    [Fact]
    public void R3_27_Panel_grafik_eksen_svg_disinda_seri_net()
    {
        // Gün 1: brüt 10.000 net 2.000 · Gün 2: brüt 5.000 net 5.000 → net seride gün 1 y = 12 + 0,6 × 204 = 134,4
        // (brüt seride gün 1 tepe = 12,0 olurdu).
        ozel["/raporlar/aylik"] = """{"gunler":[{"tarih":"2026-10-01","satisTl":10000,"netSatisTl":2000},{"tarih":"2026-10-02","satisTl":5000,"netSatisTl":5000}]}""";
        sorguOzel[OncekiAySorgu()] = """{"gunler":[]}""";   // tur 2: ayın ilk günlerinde önceki ay da istenir
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".grafik-sar polyline")), TimeSpan.FromSeconds(10));
        Assert.StartsWith("0.0,134.4 ", e.Find(".grafik-sar polyline").GetAttribute("points"), StringComparison.Ordinal);
        Assert.Empty(e.FindAll(".grafik-sar svg text"));
        Assert.Equal(2, e.FindAll(".grafik-sar .grafik-eksen .grafik-eksen-etiket").Count);
    }

    // ── R3-28 · E6 + tur 2 (hasım DÜŞÜK "Son 7 günlük satış ayın tüm günlerini çiziyor"): 8 günlük ayda grafik
    //    SON 7 günü çizer; en çok 5 yazı, son gün (bugün) HER ZAMAN yazılır ──
    [Fact]
    public void R3_28_Panel_grafik_son_7_gun_en_cok_bes_yazi_son_gun_yazili()
    {
        var gunler = string.Join(",", Enumerable.Range(1, 8).Select(i => $$"""{"tarih":"2026-10-0{{i}}","satisTl":{{i * 1000}},"netSatisTl":{{i * 1000}}}"""));
        ozel["/raporlar/aylik"] = "{\"gunler\":[" + gunler + "]}";
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".grafik-sar polyline")), TimeSpan.FromSeconds(10));
        Assert.Equal(7, e.Find(".grafik-sar polyline").GetAttribute("points")!.Split(' ').Length);   // 7 nokta
        var et = e.FindAll(".grafik-eksen-etiket").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(["02.10", "04.10", "06.10", "08.10"], et);
    }

    // ── R3-29 · tur 2: ayın ilk günlerinde (3 gün) "Son 7 gün" önceki ayın son günlerini de çizer ──
    [Fact]
    public void R3_29_Panel_grafik_ay_basinda_onceki_aydan_tamamlar()
    {
        ozel["/raporlar/aylik"] = """{"gunler":[{"tarih":"2026-10-01","satisTl":1,"netSatisTl":1},{"tarih":"2026-10-02","satisTl":2,"netSatisTl":2},{"tarih":"2026-10-03","satisTl":3,"netSatisTl":3}]}""";
        var eylul = string.Join(",", Enumerable.Range(1, 30).Select(i => $$"""{"tarih":"2026-09-{{i:00}}","satisTl":{{i}},"netSatisTl":{{i}}}"""));
        sorguOzel[OncekiAySorgu()] = "{\"gunler\":[" + eylul + "]}";
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-izgara");
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".grafik-sar polyline")), TimeSpan.FromSeconds(10));
        Assert.Equal(7, e.Find(".grafik-sar polyline").GetAttribute("points")!.Split(' ').Length);
        var et = e.FindAll(".grafik-eksen-etiket").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal("27.09", et[0]);
        Assert.Equal("03.10", et[^1]);
    }

    /// <summary>Panel'in önceki ay isteğinin sorgu eki (bugünden).</summary>
    private static string OncekiAySorgu()
    {
        var once = DateTime.Today.AddMonths(-1);
        return $"/raporlar/aylik?yil={once.Year}&ay={once.Month}";
    }

    // ── R3-33 · tur 2 (hasım ORTA "dövizli kasa"): Gün sonu dövizli girişi TL kırılımına KATMAZ, para birimiyle ayrı
    //    yazar — BASİT kırılım satırı + PRO ayrı satır ──
    private const string MutabakatDovizli = """
        {"tarih":"2026-10-08","tahsilat":{"nakit":500,"kart":0,"havale":0,"diger":0,"toplam":500,"virmanGiris":0,"acilisDevri":0,
            "dovizGiris":[{"paraBirimi":"USD","tutar":100}]},
         "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"altinHasGrami":0,"madenDagilimi":[],"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":0,"mutabik":true,"farkTanimi":"x","uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
        """;

    [Fact]
    public void R3_33_Gun_sonu_doviz_girisi_ayri_BASIT()
    {
        ozel["/raporlar/pos-fatura-mutabakati"] = MutabakatDovizli;
        Oturum();
        var r = Raporlar();
        r.WaitForElement("#bugun-tahsilat-kirilim");
        var metin = r.Find("#bugun-tahsilat-kirilim").TextContent;
        Assert.Contains(Bicim.TlKurus(500m), metin, StringComparison.Ordinal);
        Assert.Contains(string.Format(KarRaporSozlugu.Tr.DovizGirisKisa, Bicim.BelgePara(100m, "USD")), metin, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.TlKurus(600m), metin, StringComparison.Ordinal);
    }

    [Fact]
    public void R3_34_Gun_sonu_doviz_girisi_ayri_PRO()
    {
        ozel["/raporlar/pos-fatura-mutabakati"] = MutabakatDovizli;
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-gunsonu");
        e.Find("#rp-sekme-gunsonu").Click();
        e.WaitForElement("#mut-doviz-giris-USD");
        var satir = e.Find("#mut-doviz-giris-USD").TextContent;
        Assert.Contains(string.Format(KarRaporSozlugu.Tr.DovizKasaGirisi, "USD"), satir, StringComparison.Ordinal);
        Assert.Contains(Bicim.BelgePara(100m, "USD"), satir, StringComparison.Ordinal);
    }
}

// ── R3-31/32 · tur 2 (hasım KRİTİK "eksen yazısı noktasının altında değil"): her yazının yüzde konumu = X(i)/G,
//    yazı kendi noktasının ızgara hücresinde; son nokta HER ZAMAN yazılı ──
public sealed class RaporUcAlanGrafikTestleri : BunitContext
{
    private IRenderedComponent<AlanGrafik> Ciz(int n) => Render<AlanGrafik>(p => p
        .Add(x => x.Noktalar, Enumerable.Range(1, n)
            .Select(i => new GrafikNokta($"{i:00}.10", i * 100m)).ToList())
        .Add(x => x.Aciklama, "test"));

    private static void KonumlarNoktada(IRenderedComponent<AlanGrafik> e, int n)
    {
        var hucreler = e.FindAll(".grafik-eksen > .grafik-eksen-hucre");
        Assert.Equal(n - 1, hucreler.Count);
        foreach (var et in e.FindAll(".grafik-eksen-etiket"))
        {
            var i = int.Parse(et.GetAttribute("data-indeks")!, CultureInfo.InvariantCulture);
            var konum = double.Parse(et.GetAttribute("data-konum")!, CultureInfo.InvariantCulture);
            Assert.Equal((double)i / (n - 1) * 100, konum, 2);
            Assert.Equal($"{i + 1:00}.10", et.TextContent.Trim());
            // Yazı i. hücrenin sol kenarında (= X(i)); son nokta son hücrenin sağ kenarında.
            var hucre = i == n - 1 ? n - 2 : i;
            Assert.Same(hucreler[hucre], et.ParentElement);
            Assert.Equal(i == n - 1, et.ClassList.Contains("grafik-eksen-etiket--son"));
        }
    }

    [Fact]
    public void R3_31_Alan_grafik_8_nokta_son_yazi_ve_konumlar()
    {
        var e = Ciz(8);
        var et = e.FindAll(".grafik-eksen-etiket").ToList();
        Assert.InRange(et.Count, 2, 5);
        Assert.Equal("08.10", et[^1].TextContent.Trim());
        Assert.Equal("100", et[^1].GetAttribute("data-konum"));
        KonumlarNoktada(e, 8);
    }

    [Fact]
    public void R3_32_Alan_grafik_31_nokta_konumlar()
    {
        var e = Ciz(31);
        var et = e.FindAll(".grafik-eksen-etiket").ToList();
        Assert.InRange(et.Count, 2, 5);
        Assert.Equal("31.10", et[^1].TextContent.Trim());
        KonumlarNoktada(e, 31);
    }
}

[Collection("veritabani")]
public sealed class RaporUcRaporlarProTestleri : BunitContext
{
    public RaporUcRaporlarProTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(RaporIkiRaporlarProTestleri.SahteApiOlustur(() => KarRaporRaporlarEkranTestleri.AylikHesaplanamadi))
            { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    // ── R3-30 · E4: Kâr-zarar düğmesi menü yazımı; ayın net has etiketi kapsamlı; tahsilat ⓘ tanımı; stok yaşı kart altı
    //   cümleleri ⓘ içinde (kartta alt satır değil) ──
    [Fact]
    public void R3_30_Rapor_merkezi_PRO_etiketler_ve_ipuclari()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-ozet-kartlar")), TimeSpan.FromSeconds(10));
        Assert.Equal(KarRaporSozlugu.Tr.KarZararDugme, e.Find("#rp-kar-zarar").TextContent.Trim());
        Assert.Equal(KarRaporSozlugu.Tr.AyinTumAltinHareketi, e.Find("#rp-nethas-etiket > span").TextContent.Trim());
        Assert.Equal(KarRaporSozlugu.Tr.TahsilatKapsamIpucu, e.Find("#rp-tahsilat-ipucu p").TextContent.Trim());
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-devir-izgara")), TimeSpan.FromSeconds(10));
        Assert.NotNull(e.Find("details#sy-devir-ipucu #sy-ortalama-yas-not"));
        Assert.NotNull(e.Find("details#sy-devir-ipucu #sy-tahmini-sure-not"));
        Assert.DoesNotContain(KarRaporSozlugu.Tr.OrtalamaYasAlt, e.Find("#sy-devir-izgara").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(KarRaporSozlugu.Tr.TahminiSatisSuresiAlt, e.Find("#sy-devir-izgara").TextContent, StringComparison.Ordinal);
    }
}

[Collection("veritabani")]
public sealed class RaporUcKarZararTestleri : BunitContext
{
    private string ozetGovde = MadenAyrimiKarZararEkranTestleri.OzetTam;
    private string urunGovde = """{"satirlar":[],"urunSayisi":0,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}""";
    private readonly List<string> istekler = [];

    internal const string OzetBos = """
        {"hasilatTl":0,"maliyetTl":null,"karTl":null,"karMarjiYuzde":null,"hasilatHas":0,"maliyetHas":null,"karHas":null,
         "satisAdedi":0,"maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","uyari":null,
         "karDurumu":"VERI_YOK","donemKariTl":0,"iadeAdedi":0,"takasAdedi":0,"iscilikTl":0}
        """;

    internal const string OzetMaliyetsiz = """
        {"hasilatTl":508554.56,"maliyetTl":null,"karTl":null,"karMarjiYuzde":null,"hasilatHas":70,"maliyetHas":null,"karHas":null,
         "satisAdedi":3,"maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":3,"guvenilirlik":"DUSUK","uyari":null,
         "karDurumu":"HESAPLANAMADI","maliyetiEksikSatisAdedi":3,"maliyetiEksikTutarTl":508554.56,"donemKariTl":null,"iadeAdedi":0}
        """;

    internal const string UrunMaliyetsiz = """
        {"satirlar":[
          {"urunId":"u1","kod":"K-1","ad":"Bilezik","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":11662,"maliyetTl":null,"bilinenHasilatTl":null,"karTl":null,"karDurumu":"HESAPLANAMADI",
           "maliyetiEksikSatisAdedi":1,"maliyetiEksikTutarTl":11662,"hasilatHas":1.6,"maliyetHas":null,"karHas":null,
           "karMarjiYuzde":null,"maliyetiBilinmeyenAdet":1,"guvenilirlik":"DUSUK","hasKarDurumu":"HESAPLANAMADI","hasMaliyetiEksikSatisAdedi":1},
          {"urunId":"u2","kod":"K-2","ad":"Yüzük","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":14879.10,"maliyetTl":12000,"bilinenHasilatTl":14879.10,"karTl":2879.10,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":2,"maliyetHas":1.6,"karHas":0.4,
           "karMarjiYuzde":19.35,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":"TAM","hasMaliyetiEksikSatisAdedi":0}],
         "urunSayisi":2,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"DUSUK"}
        """;

    public RaporUcKarZararTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
    }

    private sealed class SahteApi(RaporUcKarZararTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (t.istekler) t.istekler.Add(yol);
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => t.urunGovde,
                var y when y.Contains("kar-zarar") => t.ozetGovde,
                var y when y.Contains("stok-degerlemesi") => MadenAyrimiKarZararEkranTestleri.StokBilinmiyor,
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ciz(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.True(c.FindAll("#kz-panel-stok").Count > 0 || c.FindAll("#kz-aralik-hata").Count > 0));
        return c;
    }

    // ── R3-40 · E5 (hasım DÜŞÜK): satışsız dönemde Özet ve Grafik "0 ₺" yerine boş durum ──
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void R3_40_Bos_donem_kartlar_yerine_bos_durum(bool basit)
    {
        ozetGovde = OzetBos;
        var c = Ciz(basit);
        c.WaitForAssertion(() => Assert.NotNull(c.Find($"#kz-donem-bos-{GorselSigmaSozlugu.KzOzet}")));
        Assert.Contains(KarRaporSozlugu.Tr.DonemSatisYok, c.Find($"#kz-donem-bos-{GorselSigmaSozlugu.KzOzet}").TextContent, StringComparison.Ordinal);
        Assert.NotNull(c.Find($"#kz-donem-bos-{GorselSigmaSozlugu.KzGrafik}"));
        Assert.Empty(c.FindAll("#kz-tl-kpi"));
        Assert.Empty(c.FindAll("#kz-grafik-cubuk"));
    }

    // ── R3-41 · E5 (hasım DÜŞÜK): ters aralıkta hata TEK yerde (alan altı), bantta ikinci kez yok; kartlar gizli ──
    [Fact]
    public void R3_41_Ters_aralik_tek_hata_kart_yok()
    {
        var c = Ciz(basit: true);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-bas").Change("2026-09-16");
        c.Find("#kz-bit").Change("2026-09-01");
        // PAKET GOC-D: ters aralık metni sunucuyla TEK kural + sözlük anahtarı (Core RaporDonemKurali → Kabuk.TarihAraligi).
        var tersMetni = Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.TarihAraligi;
        c.WaitForAssertion(() => Assert.Equal(tersMetni, c.Find("#kz-aralik-hata").TextContent.Trim()));
        Assert.Single(Regex.Matches(c.Markup, Regex.Escape(tersMetni)));
        Assert.Empty(c.FindAll("#kz-tl-kpi"));
    }

    // ── R3-42 · E5 (hasım DÜŞÜK ×3): maliyet yoksa grafik etiketi; Ürünler hücresi "—" + tabloda TEK ⓘ; eksik bandının
    //   açıklaması ⓘ içinde; tablo TL'leri iki ondalık (11.662,00 ile 14.879,10 aynı biçim) ──
    [Fact]
    public void R3_42_Maliyetsiz_donem_grafik_etiketi_urun_tire_tek_ipucu()
    {
        ozetGovde = OzetMaliyetsiz;
        urunGovde = UrunMaliyetsiz;
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-maliyet-yok")));
        Assert.Equal(KarRaporSozlugu.Tr.GrafikMaliyetYok, c.Find("#kz-grafik-maliyet-yok").TextContent.Trim());
        Assert.NotNull(c.Find("details#kz-maliyet-eksik-nasil-ipucu #kz-maliyet-eksik-nasil"));
        Assert.Single(c.FindAll("#kz-urun-not-ipucu"));
        Assert.Equal(KarRaporSozlugu.Tr.UrunMaliyetBilinmiyorNot, c.Find("#kz-urun-not-maliyet").TextContent.Trim());
        var satir = c.FindAll("#kz-urun-tablo tbody tr")[0];
        var maliyet = satir.QuerySelector($"td[data-etiket='{c.Services.GetRequiredService<DilServisi>().T.KarZarar.Maliyet}']")!;
        Assert.Equal("—", maliyet.QuerySelector("[aria-hidden='true']")!.TextContent.Trim());
        Assert.Contains("gorsel-gizli", satir.QuerySelector(".kz-urun-has-hesaplanamadi")!.ClassName, StringComparison.Ordinal);
        var hasilat = c.FindAll("#kz-urun-tablo tbody td.sayi:nth-child(3)").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(hasilat, h => h.StartsWith("11.662,00", StringComparison.Ordinal));
        Assert.Contains(hasilat, h => h.StartsWith("14.879,10", StringComparison.Ordinal));
    }
}
