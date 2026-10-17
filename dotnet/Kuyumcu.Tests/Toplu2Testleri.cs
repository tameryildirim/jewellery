using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Kisit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-2 — toplu tasarım kuyruğunun KALAN görünüm maddeleri (01.10).
//   Saf/CSS:  ortak.css TOPLU-2 kuralları (mod anahtarı 375, segment, içe aktar boşluğu,
//             kurulum adımları, kenar hata bandı, süzgeç seçici genişliği), menü vurgusu
//             (en uzun eşleşme), basılı ekstre metinleri, sözlük (iç kod yok), giriş ekranı.
//   bUnit:    Siparişler durum süzgeci ortak Secici · Şifre sıfırlama kural cümlesi tek kez ·
//             Kenar çubuğu hata bandı × düğmesi.
// ═══════════════════════════════════════════════════════════════════════════

internal static class Toplu2Ortak
{
    public static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "Kuyumcu.Web"))) d = d.Parent;
        return d!.FullName;
    }

    public static string Dosya(params string[] parca) => File.ReadAllText(Path.Combine([Kok(), .. parca]));

    /// <summary>ortak.css'in TOPLU-2 bölümü (yorumsuz).</summary>
    public static string Bolum()
    {
        var css = Dosya("Kuyumcu.Web", "wwwroot", "css", "ortak.css");
        var i = css.IndexOf("PAKET TOPLU-2", StringComparison.Ordinal);
        Assert.True(i > 0, "ortak.css TOPLU-2 bölümü yok");
        return Regex.Replace(css[i..], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    /// <summary>Seçicinin bildirim bloğu (yoksa null).</summary>
    public static string? Blok(string css, string secici)
    {
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        if (i < 0) return null;
        var a = css.IndexOf('{', i); var k = css.IndexOf('}', a);
        return css[(a + 1)..k];
    }

    public static HttpResponseMessage Yanit(string json, HttpStatusCode kod = HttpStatusCode.OK) =>
        new(kod) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}

public sealed class Toplu2SafTestleri
{
    // ── (1) 375 mod anahtarı: okunur yazı + yatay dolgu (daire değil) ──
    [Fact]
    public void T2_Mod_anahtari_375_okunur_boy_ve_yatay_dolgu()
    {
        var b = Toplu2Ortak.Bolum();
        var i = b.IndexOf("@media (max-width: 480px)", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var blok = Toplu2Ortak.Blok(b[i..], ".ust-bar .mod-anahtar button");
        Assert.NotNull(blok);
        Assert.Contains("font-size: var(--yazi-sm)", blok);
        Assert.Contains("padding: 0 var(--bosluk-3)", blok);
        Assert.Contains("white-space: nowrap", blok);
    }

    // ── (2) /cariler 375 segment: öğeler kabı eşit paylaşır ──
    [Fact]
    public void T2_Musteri_segmenti_dar_ekranda_esit_pay()
    {
        var blok = Toplu2Ortak.Blok(Toplu2Ortak.Bolum(), ".sayfa-basi .sayfa-basi-filtre > .musteri-segment > .musteri-segment-oge");
        Assert.NotNull(blok);
        Assert.Contains("flex: 1 1 0", blok);
    }

    // ── (3) Veri içe aktar: şablon düğmesi ile katlanır arasında boşluk ──
    [Fact]
    public void T2_Ice_aktar_sablon_dugmesi_katlanira_yapismaz()
    {
        var blok = Toplu2Ortak.Blok(Toplu2Ortak.Bolum(), ".zo-belge .alan.zo-ice-sablon + .katlanir");
        Assert.NotNull(blok);
        Assert.Contains("margin-top: var(--bosluk-3)", blok);
    }

    // TEMA TEMİZLİĞİ 17.10: T2_Kenar_hata_bandi_tek_satir_kisa_cumle kaldırıldı — kenar çubuklu kabuk (Layout/KenarCubugu.razor) bilerek boşaltıldı; #kenar-sira-hatasi bandı ve .kenar-uyari-kapat kuralı ürün CSS'inden kalktı, MenuSozlugu.KatlamaKaydedilemedi artık hiçbir ekranda çizilmez (kabuk YildizUstMenu).

    // ── (6) Süzgeç seçicileri ezilmez ──
    [Fact]
    public void T2_Suzgec_secicileri_asgari_genislik()
    {
        var b = Toplu2Ortak.Bolum();
        var blok = Toplu2Ortak.Blok(b, "html[data-mod=\"basit\"] :is(#urun-durum-filtre-sar, #cari-tur-filtre-sar, #sip-durum-suzgec-sar)");
        Assert.NotNull(blok);
        Assert.Contains("min-width: 7.5rem", blok);
    }

    // ── (6b) Vitrin pano başlığı değeriyle aynı hizada ──
    [Fact]
    public void T2_Vitrin_kalem_basligi_basa_hizali()
    {
        var blok = Toplu2Ortak.Blok(Toplu2Ortak.Bolum(), "#vitrin-pano thead th:not(.sayi)");
        Assert.NotNull(blok);
        Assert.Contains("text-align: start", blok);
    }

    // ── (7) Menü vurgusu: kenar çubuğuyla kalktı (tema temizliği 17.10; Yıldız: YildizKabuk.SeciliKimlik) ──

    // ── (8) Basılı ekstre: resmî ad, taraf etiketi, sade sınır metni ──
    [Fact]
    public void T2_Basili_ekstre_resmi_ad_ve_taraf_etiketi()
    {
        Assert.Equal("CARİ HESAP EKSTRESİ",
            Kuyumcu.Api.Modules.Cikti.CiktiServisi.BelgeAdi(Kuyumcu.Contracts.Cikti.BelgeTurleri.CariEkstre));
        Assert.Equal("Müşteri", Kuyumcu.Api.Modules.Cikti.CiktiServisi.CariTarafEtiketi("MUSTERI"));
        Assert.Equal("Tedarikçi", Kuyumcu.Api.Modules.Cikti.CiktiServisi.CariTarafEtiketi("TEDARIKCI"));
        Assert.Equal("Hesap sahibi", Kuyumcu.Api.Modules.Cikti.CiktiServisi.CariTarafEtiketi("HER_IKISI"));
        var kod = Toplu2Ortak.Dosya("Kuyumcu.Api", "Modules", "Cikti", "CiktiModulu.cs");
        Assert.DoesNotContain("\"Sunucu sınırı", kod);
        Assert.DoesNotContain("değilmedi", kod);
        Assert.DoesNotContain("new CiktiTaraf(\"Cari\"", kod);
    }

    // ── (9) Sözlük: iç kalem kodu ve teknik ek yok ──
    [Fact]
    public void T2_Toplu2_metinlerinde_ic_kod_yok()
    {
        foreach (var m in new[] { Toplu2Sozlugu.Tr, Toplu2Sozlugu.En })
        {
            Assert.DoesNotContain("K-24", m.KurulumNegatifAcilisPro);
            Assert.DoesNotContain("CSV", m.IceSablonIndir);
        }
        Assert.Same(Toplu2Sozlugu.En, Toplu2Sozlugu.Al("en"));
        Assert.Same(Toplu2Sozlugu.Tr, Toplu2Sozlugu.Al("tr"));
        // Ekran bu anahtarları OKUR (HUB metni değil).
        Assert.Contains("Toplu2Sozlugu.Al(Dil.Dil).KurulumNegatifAcilisPro", Toplu2Ortak.Dosya("Kuyumcu.Web", "Pages", "Kurulum.razor"));
        Assert.Contains("Toplu2Sozlugu.Al(Dil.Dil).IceSablonIndir", Toplu2Ortak.Dosya("Kuyumcu.Web", "Pages", "VeriIceAktarim.razor"));
    }

    // ── (10) Giriş: istisna türü ekrana basılmaz ──
    [Fact]
    public void T2_Giris_kisit_hatasi_istisna_adi_tasimaz()
    {
        var kod = Toplu2Ortak.Dosya("Kuyumcu.Web", "Pages", "Giris.razor");
        Assert.DoesNotMatch(@"kisitHatasi\s*=\s*\$""[^;]*GetType\(\)", kod);
        Assert.Contains("kisitHatasi = Dil.T.Auth.KisitHatasi;", kod);
    }

    // ── (11) Siparişler: durum süzgeci yerli <select> değil, ortak Secici ──
    [Fact]
    public void T2_Siparisler_yerli_select_yok()
    {
        var kod = Toplu2Ortak.Dosya("Kuyumcu.Web", "Pages", "Siparisler.razor");
        var isaretleme = Regex.Replace(kod, @"@\*.*?\*@", "", RegexOptions.Singleline);
        Assert.DoesNotContain("<select", isaretleme);
        Assert.Contains("<Secici ElemanId=\"sip-durum-suzgec\"", kod);
    }
}

// ═══ bUnit: Siparişler durum süzgeci ═══════════════════════════════════════
public sealed class Toplu2SiparisTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];

    public Toplu2SiparisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu2SiparisTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            t.istekler.Add((istek.Method.Method, yol, istek.RequestUri!.Query));
            return Task.FromResult(Toplu2Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/siparisler") => """
                    {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
                     "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => "[]",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void T2_Durum_suzgeci_Secici_secim_sunucuya_durum_ile_gider()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        var e = Render<Siparisler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/siparisler")));
        // Yerli <select> yok; tetik ortak Secici düğmesi, kimlik AYNI.
        Assert.Empty(e.FindAll("select#sip-durum-suzgec"));
        Assert.Contains("secici-tetik", e.Find("#sip-durum-suzgec").ClassName);
        Assert.Equal(Sozlukler.Tr.Siparis.TumDurumlar, e.Find("#sip-durum-suzgec").TextContent.Trim());

        istekler.Clear();
        SeciciYardim.Sec(e, "sip-durum-suzgec", "HAZIR");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/siparisler") && i.Sorgu.Contains("durum=HAZIR")));
    }
}

// ═══ bUnit: Şifre sıfırlama — kural cümlesi bir kez ═══════════════════════════
public sealed class Toplu2SifreSifirlaTestleri : BunitContext
{
    private readonly List<string> yollar = [];

    public Toplu2SifreSifirlaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu2SifreSifirlaTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            t.yollar.Add(yol);
            if (yol.EndsWith("/auth/parola-sifirlama/durum", StringComparison.Ordinal))
                return Task.FromResult(Toplu2Ortak.Yanit("""{"acik":true,"mesaj":null,"gecerlilikDakika":30}"""));
            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
                return Task.FromResult(Toplu2Ortak.Yanit(JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
            return Task.FromResult(Toplu2Ortak.Yanit("""{"hata":"beklenmeyen"}""", HttpStatusCode.NotFound));
        }
    }

    [Fact]
    public void T2_Kisa_parolada_kural_cumlesi_bir_kez_yazilir()
    {
        var m = AuthSifirlaSozlugu.Tr;
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sifre-sifirla#jeton=abc");
        var e = Render<SifreSifirla>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#sifirla-yukleniyor")));

        // Açılışta kural İPUCU olarak bir kez görünür.
        Assert.Equal(m.ParolaKurali, e.Find("#sifirla-yeni-sifre-ipucu").TextContent.Trim());

        e.Find("#sifirla-yeni-sifre").Input("kisa");
        e.Find("#sifirla-yeni-tekrar").Input("kisa");
        e.Find("#sifirla-yeni-form").Submit();

        // Hata alanın altında; aynı cümle ipucu olarak İKİNCİ kez basılmaz.
        var metin = e.Find("#sifirla-kart").TextContent;
        Assert.Single(Regex.Matches(metin, Regex.Escape(m.ParolaKurali)));
        Assert.Empty(e.FindAll("#sifirla-yeni-sifre-ipucu"));
        Assert.DoesNotContain(yollar, y => y.EndsWith("/tamamla"));

        // Kural sağlanınca hata kalkar, ipucu geri gelir.
        e.Find("#sifirla-yeni-sifre").Input("YeniParola456");
        e.Find("#sifirla-yeni-tekrar").Input("YeniParola456");
    }
}

// ═══ bUnit: Satış analizi — değişmezlik gerekçesi KAPALI katlanırda ═══════════
public sealed class Toplu2SatisAnaliziTestleri : BunitContext
{
    private const string OdemeAraci = """
        {"baslangic":"2026-08-19","bitis":"2026-09-17","kaynak":"KASA_HAREKETLERI",
         "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":12000.00,"cikisTutar":0,"adet":5}],
                     "girisTlToplam":12000.00,"adet":5}],
         "veresiye":{"doganTl":0,"kapananTl":0},
         "bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":5},
         "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"YUKSEK"}
        """;

    public Toplu2SatisAnaliziTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(Toplu2Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/raporlar/odeme-araci") => OdemeAraci,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void T2_Satis_analizi_gerekce_kapali_katlanirda_acilinca_gorunur()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<SatisAnalizi>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-ozet")));
        // Açılışta kutu YOK (BASİT kalabalığı); gerekçe silinmedi — katlanır başlığı var.
        Assert.Empty(e.FindAll("#degismez-ipucu-Rapor"));
        Assert.Contains(ZoBelgeSozlugu.Tr.RaporDuzeltmeBaslik, e.Find("#sa-duzeltme-ac").TextContent);
        e.Find("#sa-duzeltme-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sa-duzeltme #degismez-ipucu-Rapor")));
        Assert.Contains(Sozlukler.Tr.SayfaDuzeltme.RaporDuzeltmeYolu, e.Find("#degismez-ipucu-Rapor").TextContent);
    }
}

// ═══ TOPLU-2 tur 2 — tasarım hakeminin üç KRİTİK bulgusu ═══════════════════════════════════════

/// <summary>(KRİTİK 2) PRO üst çubuk 1440'ta iki satır · (KRİTİK 3) basılı ekstre 375 — CSS sözleşmesi.</summary>
public sealed class Toplu2Tur2CssTestleri
{
    [Fact]
    public void T2_PRO_ust_cubuk_1440_kaynak_adi_ipucuna_iner_araliklar_daralir()
    {
        var b = Toplu2Ortak.Bolum();
        var i = b.IndexOf("@media (min-width: 1440px)", StringComparison.Ordinal);
        Assert.True(i >= 0, "≥1440 PRO üst çubuk kuralı yok");
        var parca = b[i..];
        Assert.Contains("display: none", Toplu2Ortak.Blok(parca, "html[data-mod=\"profesyonel\"] .ust-kur-serit #ust-kur-kaynak") ?? "");
        Assert.Contains("column-gap: 18px", Toplu2Ortak.Blok(parca, "html[data-mod=\"profesyonel\"] .ust-bar") ?? "");
        // BASİT'e dokunulmaz (1440'ta 68 px ölçüldü).
        Assert.DoesNotContain("html[data-mod=\"basit\"] .ust-kur-serit #ust-kur-kaynak", b);
    }

    [Fact]
    public void T2_Belge_kayit_kimligi_tek_satir_ve_375_kart_duzeni()
    {
        var b = Toplu2Ortak.Bolum();
        var kimlik = Toplu2Ortak.Blok(b, ".belge-no .belge-no-kimlik");
        Assert.NotNull(kimlik);
        Assert.Contains("white-space: nowrap", kimlik);
        var i = b.IndexOf(".belge-tablo:has(thead) tbody td {", StringComparison.Ordinal);
        Assert.True(i > 0, "375 kart düzeni yok");
        Assert.Contains("@media screen and (max-width: 640px)", b[..i]);
        Assert.Contains("display: flex", Toplu2Ortak.Blok(b[i..], ".belge-tablo:has(thead) tbody td") ?? "");
        Assert.Contains("display: block", Toplu2Ortak.Blok(b, ".belge-tablo:has(thead) tbody tr") ?? "");
        Assert.Contains("content: attr(data-etiket)", Toplu2Ortak.Blok(b, ".belge-tablo:has(thead) tbody td[data-etiket]::before") ?? "");
        Assert.Contains("overflow: visible", Toplu2Ortak.Blok(b, ".belge-tablo:has(thead)") ?? "");
    }
}

/// <summary>Kuyruk DÜŞÜK maddeleri (tur 2): Bildirimler sekme hapı, Uyum/Etiketler yapışık metin.</summary>
public sealed class Toplu2Tur2KuyrukTestleri
{
    [Fact]
    public void T2_Bildirimler_sekmeleri_ortak_sekme_hapi_ailesinde()
    {
        var kod = Toplu2Ortak.Dosya("Kuyumcu.Web", "Pages", "Bildirimler.razor");
        Assert.Contains("<div class=\"sekmeler-serit\" role=\"tablist\" id=\"bildirim-sekme-seridi\">", kod);
        Assert.Contains("d == durum ? \"sekmeler-sekme secili\" : \"sekmeler-sekme\"", kod);
        foreach (var id in new[] { "bildirim-sekme-acik", "bildirim-sekme-kapali", "bildirim-sekme-tumu" })
            Assert.Matches($"role=\"tab\"[^>]*id=\"{id}\"", kod);
        // Ölçüm (PRO 960): iş tablosunda son çalışma tarih-saati iki satıra kırılıyordu (K3=6) → tek satır.
        // PAKET GOC-B: iş tablosu ortak ızgarada — hücre sınıfı kolon tanımında AYNI, id AYNI.
        Assert.Matches(@"Anahtar = ""son-calisma"".*\n.*HucreSinif = _ => ""metin-kucuk hucre-tek-satir""[^\n]*\n\s*Hucre = i => @<span id=""@\(\$""is-son-calisma-\{i\.Kod\}""\)"">", kod);
        var b = Toplu2Ortak.Bolum();
        Assert.Contains("border-radius: 999px", Toplu2Ortak.Blok(b, "html[data-mod] .ana-icerik #bildirim-sekme-seridi > .sekmeler-sekme") ?? "");
        Assert.Contains("width: fit-content", Toplu2Ortak.Blok(b, "#bildirim-sekme-seridi") ?? "");
    }

    [Fact]
    public void T2_Uyum_risk_ve_Etiketler_metin_satirlari_karta_yapismaz()
    {
        var b = Toplu2Ortak.Bolum();
        Assert.Contains("margin-top: var(--bosluk-3)", Toplu2Ortak.Blok(b, ".sekmeler-panel #uy-risk-aciklama") ?? "");
        Assert.Contains("margin-bottom: var(--bosluk-3)", Toplu2Ortak.Blok(b, ".sekmeler-panel :is(#uy-risk-aciklama, #uy-risk-durdurmaz, #uy-risk-kapali):has(+ .stat-izgara)") ?? "");
        Assert.Contains("padding: var(--bosluk-4)", Toplu2Ortak.Blok(b, ".kart:has(#et-ara)") ?? "");
        Assert.Contains("margin-block: var(--bosluk-3) var(--bosluk-2)", Toplu2Ortak.Blok(b, "#et-sinir") ?? "");
        Assert.Contains("margin-bottom: var(--bosluk-3)", Toplu2Ortak.Blok(b, "#et-yazdir-kilit") ?? "");
    }
}

/// <summary>(KRİTİK 3) Basılı ekstre: belge no yerine ekstre tarihi, teknik satır yok, kimlik uyarısı belgenin dışında.</summary>
public sealed class Toplu2CiktiTestleri : BunitContext
{
    private string tur = Kuyumcu.Contracts.Cikti.BelgeTurleri.CariEkstre;
    private bool kimlikEksik;

    public Toplu2CiktiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu2CiktiTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var duz = t.kimlikEksik
                ? """{"unvan":"Test Kuyumcu","sube":"Merkez","adres":null,"vergiDairesi":null,"vergiNo":null,"telefon":null}"""
                : """{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Çarşı 1","vergiDairesi":"Beyazıt","vergiNo":"1234567890","telefon":null}""";
            var j = $$"""
            {"tur":"{{t.tur}}","belgeAdi":"CARİ HESAP EKSTRESİ","belgeNo":"01M3VV6RZST5TXEW9T1YJEPP4P","dayanak":null,
             "duzenlemeTarihi":"2026-10-01T08:00:00Z","duzenleyen":{{duz}},
             "karsi":{"baslik":"Müşteri","ad":"Tuna Akarsu","vergiNo":null,"adres":null,"telefon":null},
             "bolumler":[{"baslik":"Hareketler","sutunBasliklari":["Tarih","İşlem","Açıklama","Has gram","Tutar"],
               "sutunBicimleri":["METIN","METIN","METIN","HAS","PARA"],
               "satirlar":[{"hucreler":["01.10.2026","Satış","Bilezik","1.5000","1000.00"],"vurgu":false,"paraBirimi":"TRY"}],"not":null}],
             "tutarlar":[{"etiket":"TL bakiye","deger":1000.00,"paraBirimi":"TRY","toplam":true}],
             "imzaBasliklari":["İşletme","Müşteri"],"not":null,"uyari":null,"belgeNoKayitKimligi":true}
            """;
            return Task.FromResult(Toplu2Ortak.Yanit(j));
        }
    }

    private IRenderedComponent<Cikti> Ekran()
    {
        var e = Render<Cikti>(p => p.Add(x => x.Tur, tur).Add(x => x.Id, "01M3VV6RZST5TXEW9T1YJEPP4P"));
        e.WaitForElement("#cikti-belge");
        return e;
    }

    [Fact]
    public void T2_Ekstrede_belge_no_yerine_ekstre_tarihi_kimlik_basilmaz()
    {
        var e = Ekran();
        Assert.Equal("Ekstre tarihi", e.Find(".belge-no .etiket").TextContent.Trim());
        Assert.Equal("01.10.2026", e.Find("#cikti-no").TextContent.Trim());
        Assert.DoesNotContain("01M3VV6RZST5TXEW9T1YJEPP4P", e.Find("#cikti-belge").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#cikti-no-aciklama"));
        // 375 kart düzeni için her hücre sütun başlığını taşır.
        var td = e.FindAll("#cikti-belge tbody tr")[0].QuerySelectorAll("td");
        Assert.Equal("İşlem", td[1].GetAttribute("data-etiket"));
        Assert.Equal("Tutar", td[4].GetAttribute("data-etiket"));
    }

    [Fact]
    public void T2_Satis_fisinde_kayit_kimligi_tek_satir_soluk_sinifla_basilir()
    {
        tur = Kuyumcu.Contracts.Cikti.BelgeTurleri.SatisFisi;
        var e = Ekran();
        // TOPLU-5 tur 2: ham kimlik yerine kısa kayıt kodu (son 8 hane) basılır.
        Assert.Equal("1YJE-PP4P", e.Find("#cikti-no").TextContent.Trim());
        Assert.Contains("belge-no-kimlik", e.Find("#cikti-no").ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T2_Duzenleyen_eksik_uyarisi_belgenin_DISINDA_onizlemenin_ustunde()
    {
        kimlikEksik = true;
        var e = Ekran();
        Assert.NotNull(e.Find("#cikti-kimlik-uyari #degismez-ipucu-IsletmeKimligi"));
        Assert.NotNull(e.Find("#cikti-kimlik-uyari #cikti-isletme-karti"));
        Assert.Empty(e.FindAll("#cikti-belge #degismez-ipucu-IsletmeKimligi"));
        Assert.Empty(e.FindAll("#cikti-belge #cikti-isletme-karti"));
        // Önizlemenin ÜSTÜNDE: uyarı belgeden önce gelir.
        var m = e.Markup;
        Assert.True(m.IndexOf("id=\"cikti-kimlik-uyari\"", StringComparison.Ordinal) < m.IndexOf("id=\"cikti-belge\"", StringComparison.Ordinal));
        // Düzeltme yolu çalışır.
        e.Find("#cikti-isletme-karti").Click();
        Assert.EndsWith("/isletme", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }
}

/// <summary>(KRİTİK 1) Döviz Veresiye / Tahsilat çekmecesi: BASİT'te geliştirici notu yok, tutar ZORUNLU ve sade adlı.</summary>
public sealed class Toplu2DovizCekmeceTestleri : BunitContext
{
    private bool usdKasasi = true;

    public Toplu2DovizCekmeceTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu2DovizCekmeceTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = istek.RequestUri!.AbsolutePath;
            var g = y switch
            {
                _ when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                _ when y.EndsWith("/kasalar") => t.usdKasasi
                    ? """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks-usd","ad":"USD Kasa","paraBirimi":"USD","aktif":true}]"""
                    : """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                _ when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,
                      "tlBakiye":-1500.00,"hasGramBakiye":2.5,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
                      "riskSeviye":null,"pepBeyani":false}]
                    """,
                _ when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(Toplu2Ortak.Yanit(g));
        }
    }

    private IRenderedComponent<Cariler> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        return Render<Cariler>();
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: Yıldız müşteri listesinde satır eylemi yok — eylemler satıra dokununca açılan KARTTA aynı
    /// kimliklerle durur (YildizKartEylemleri; #c-doviz-veresiye-c1 …). Önce satır (#yildiz-musteri-c1), sonra eylem.
    /// </summary>
    private static void Ac(IRenderedComponent<Cariler> e, string id)
    {
        e.WaitForState(() => e.FindAll("#yildiz-musteri-c1").Count > 0);
        e.Find("#yildiz-musteri-c1").Click();
        e.WaitForState(() => e.FindAll($"#{id}").Count > 0);
        e.FindAll($"#{id}")[0].Click();
    }

    [Fact]
    public void T2_BASIT_doviz_veresiyede_gelistirici_notu_yok_tutar_zorunlu_sade_ad()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        Ac(e, "c-doviz-veresiye-c1");
        Assert.Empty(e.FindAll("#dv-fis-bacagi-not"));
        Assert.Empty(e.FindAll("#dv-vadesiz-not"));
        var cekmece = e.Find("#cekmece-doviz-veresiye").TextContent;
        Assert.DoesNotContain("bu sürümde", cekmece, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Cari.VeresiyeIpucu, cekmece, StringComparison.Ordinal);
        var etiket = e.Find("#dv-tutar-sar").TextContent;
        Assert.Contains("Tutar (USD)", etiket, StringComparison.Ordinal);
        Assert.DoesNotContain("isteğe bağlı", etiket, StringComparison.Ordinal);
        Assert.Equal("true", e.Find("#dv-tutar").GetAttribute("aria-required"));
        // Kilit sebebi alanın ADIYLA aynı (K18: "isteğe bağlı" ↔ "kilitli" çelişkisi yok).
        Assert.Contains("Tutar (USD)", e.Find("#cekmece-doviz-veresiye-kilit").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T2_PRO_doviz_veresiye_notlari_sade_dil_bu_surumde_YOKTUR_basilmaz()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Ac(e, "c-doviz-veresiye-c1");
        Assert.Equal(Kuyumcu.Core.I18n.Toplu2Sozlugu.Tr.DovizVadeNot, e.Find("#dv-vadesiz-not").TextContent.Trim());
        Assert.Equal(Kuyumcu.Core.I18n.Toplu2Sozlugu.Tr.DovizFisBacagiNot, e.Find("#dv-fis-bacagi-not").TextContent.Trim());
        Assert.DoesNotContain("YOKTUR", e.Find("#cekmece-doviz-veresiye").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("isteğe bağlı", e.Find("#dv-tutar-sar").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T2_Doviz_tahsilatta_kasa_yok_satiri_bos_durum_kutusu_degil_tutar_zorunlu()
    {
        usdKasasi = false;
        var e = Ciz(ArayuzModuServisi.Basit);
        Ac(e, "c-doviz-tahsilat-c1");
        e.WaitForState(() => e.FindAll("#dt-kasa-yok").Count > 0);
        Assert.Contains("kasası yok", e.Find("#dt-kasa-yok").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#cekmece-doviz-tahsilat .bos-durum"));
        Assert.DoesNotContain("isteğe bağlı", e.Find("#dt-tutar-sar").TextContent, StringComparison.Ordinal);
        Assert.Contains("Tutar (USD)", e.Find("#dt-tutar-sar").TextContent, StringComparison.Ordinal);
        // Kasa satırı para biriminin hemen altında, tutardan ÖNCE.
        var m = e.Find("#cekmece-doviz-tahsilat").InnerHtml;
        Assert.True(m.IndexOf("dt-kasa-yok", StringComparison.Ordinal) < m.IndexOf("dt-tutar", StringComparison.Ordinal));
    }
}

// ═══ TUR 3 — doğrulama KRİTİK: süzgeç seçicisi açılınca sayfa zıplamaz ═══════════════
// Ölçüldü (başsız Chrome, BASİT 1280): açılan liste araç şeridinde AKIŞTA olduğu için tetiği
// 120 → 320 px genişletip tabloyu 199-375 px itiyordu. Kural: şeritteki liste akış dışı, tetiğe
// bağlı mutlak katman; 320 px asgari genişlik yalnız listede; açıkken hiza kapalıyla aynı.
// Piksel kanıtı birim testinde değil, başsız Chrome ölçümündedir (rapor § T3).
public sealed class Toplu2Tur3SeciciCssTestleri
{
    private const string Panel = ".sayfa-basi .sayfa-basi-filtre .secici-kap > .secici-liste-kap:not(.sabit)";

    [Fact]
    public void T3_Serit_secici_listesi_akis_disi_tetige_bagli_mutlak_katman()
    {
        var b = Toplu2Ortak.Bolum();
        var blok = Toplu2Ortak.Blok(b, Panel);
        Assert.NotNull(blok);
        Assert.Contains("position: absolute", blok);
        Assert.DoesNotContain("position: relative", blok);
        Assert.Contains("top: calc(100% + var(--bosluk-1))", blok);
        Assert.Contains("left: 0", blok);
        Assert.Contains("margin-top: 0", blok);
        // Asgari genişlik bu kuralda VERİLMEZ: app.css (2) listeye 20rem verir, %100 = tetik (kap).
        Assert.DoesNotContain("min-width", blok);
        // Yukarı ve sola açılma mutlak panelde de anlamını korur.
        Assert.Contains("bottom: calc(100% + var(--bosluk-1))",
            Toplu2Ortak.Blok(b, ".sayfa-basi .sayfa-basi-filtre .secici-kap > .secici-liste-kap.yukari:not(.sabit)") ?? "");
        Assert.Contains("right: 0",
            Toplu2Ortak.Blok(b, ".sayfa-basi .sayfa-basi-filtre .secici-kap > .secici-liste-kap.sola:not(.sabit)") ?? "");
    }

    [Fact]
    public void T3_Kirpan_ata_sabit_yerlesimi_ezilmez_ve_kural_app_cssten_sonra_yuklenir()
    {
        // `.sabit` (katman.css, position: fixed) kırpan atadan kurtarma yoludur; bu kural onu EZMEMELİ.
        var b = Toplu2Ortak.Bolum();
        foreach (Match m in Regex.Matches(b, @"\.sayfa-basi-filtre \.secici-kap > \.secici-liste-kap[^{,]*\{"))
            Assert.Contains(":not(.sabit)", m.Value);
        // ortak.css app.css'ten ve katman.css'ten SONRA yüklenir (aynı özgüllükte son kural kazanır).
        var html = Toplu2Ortak.Dosya("Kuyumcu.Web", "wwwroot", "index.html");
        var app = html.IndexOf("css/app.css", StringComparison.Ordinal);
        var katman = html.IndexOf("css/katman.css", StringComparison.Ordinal);
        var ortak = html.IndexOf("css/ortak.css", StringComparison.Ordinal);
        Assert.True(app > 0 && katman > 0 && ortak > katman && ortak > app, "ortak.css app.css/katman.css'ten sonra yüklenmeli");
        // 320 px asgari genişlik yalnız listede (app.css (2)); tetik/alan kuralında yok.
        var appCss = Toplu2Ortak.Dosya("Kuyumcu.Web", "wwwroot", "css", "app.css");
        Assert.Contains(".sayfa-basi-filtre .secici-liste-kap { min-width: min(max(100%, 20rem), 92vw)", appCss);
        Assert.DoesNotContain("20rem", Toplu2Ortak.Blok(b, ".sayfa-basi .sayfa-basi-filtre > .secici-alan") ?? "");
    }

    [Fact]
    public void T3_Panel_acikken_hiza_kapali_ile_ayni_alttan()
    {
        var b = Toplu2Ortak.Bolum();
        Assert.Contains("align-items: flex-end", Toplu2Ortak.Blok(b, ".sayfa-basi:has(.sayfa-basi-filtre .secici-liste-kap):not(#_)") ?? "");
        Assert.Contains("align-items: flex-end", Toplu2Ortak.Blok(b, ".sayfa-basi .sayfa-basi-filtre.filtre-yuvasi:has(.secici-liste-kap):not(#_)") ?? "");
        Assert.DoesNotContain("flex-start", Toplu2Ortak.Blok(b, ".sayfa-basi:has(.sayfa-basi-filtre .secici-liste-kap):not(#_)") ?? "flex-start");
    }
}

/// <summary>Gerçek DOM kuralın seçicisine uyar: şerit seçicisinin listesi tetikle AYNI .secici-kap'ta,
/// .sayfa-basi-filtre içinde doğar (CSS kuralı ancak o zaman eşleşir).</summary>
public sealed class Toplu2Tur3SeciciDomTestleri : BunitContext
{
    public Toplu2Tur3SeciciDomTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(Toplu2Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/siparisler") => """
                    {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
                     "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => "[]",
                var y when y.EndsWith("/urunler") => "[]",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void T3_Siparis_durum_suzgeci_listesi_tetikle_ayni_kapta_seritte_dogar_tetik_yerinde()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        var e = Render<Siparisler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sip-durum-suzgec")));
        Assert.Empty(e.FindAll("#sip-durum-suzgec-liste-kap"));
        e.Find("#sip-durum-suzgec").Click();
        var kap = e.Find("#sip-durum-suzgec-liste-kap");
        var secKap = kap.ParentElement!;
        Assert.Contains("secici-kap", secKap.ClassName);
        Assert.Same(secKap, e.Find("#sip-durum-suzgec").ParentElement);
        Assert.NotNull(kap.Closest(".sayfa-basi .sayfa-basi-filtre"));
        // Açılış tetiği değiştirmez: aynı düğme, aynı sınıf ailesi (+ acik), satır içi boy yok.
        var tetik = e.Find("#sip-durum-suzgec");
        Assert.Contains("acik", tetik.ClassName);
        Assert.Null(tetik.GetAttribute("style"));
        Assert.Null(kap.GetAttribute("style"));
    }
}
