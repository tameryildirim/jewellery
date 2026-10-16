using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-j — MENÜ İÇİ ARAMA (kurucu 24.09 14:40, /urunler/agac, "sat")
//
// Kurucu: "menüde arama yapınca arama sonuçları farklı yerde beyaz zeminde
// geliyor, gelmemesi lazım, menüde olması lazım, sonuçların extra gelmemesi
// lazım, ayrıca altta bir uyarı veriyor arama yapınca".
//
// Ölçülen davranışlar:
//   • K11  — ilk tuşta çökme YOK: MenuTanimi.Sadelestir boş/boşluklu metinde
//            boş döner; keydown ↔ input sırası sonucu değiştirmez.
//   • K12  — sonuç menünün İÇİNDE süzülür: görünen .kenar-satir = eşleşenler,
//            eşleşen grup açık, eşleşmeyen satır/grup [hidden]; ayrı pencere 0.
//   • × / Esc menüyü kullanıcının KATLAMA TERCİHLİ hâline döndürür ve arama
//     tercihe HİÇ yazmaz.
//   • ↑/↓ eşleşen satırlar arasında, Enter ekrana gider.
//   • S1-g eş anlamlıları (eski adla arama) çalışır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1jMenuAramaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private string tercihJson = """{"tercihler":{}}""";

    private const string TumModuller =
        """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";

    private sealed class SahteApi(List<(string, string)> kayit, Func<string> tercih) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol));
            // ⚠ bUnit sözleşmesi: her uca GERÇEKÇİ yanıt ("{}" varsayılanı yok).
            var govde = yol.Contains("/yetkiler/benim", StringComparison.Ordinal)
                ? $$"""{"moduller":{{TumModuller}}}"""
                : yol.Contains("/tercihler/benim", StringComparison.Ordinal)
                    ? tercih()
                    : """{"sonuclar":[],"limit":10,"dahaVar":false}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public S1jMenuAramaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => tercihJson))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
        // ⚠ KabukServisi.Ortak STATİKTİR ve paralel sınıflar arasında paylaşılır:
        //   burada AramaSubesiAta ÇAĞRILMAZ (şube testinin durumunu ezerdi —
        //   KabukAramaEkranTestleri.Secilen_sube… kararsızlığı ölçüldü).
    }

    private IRenderedComponent<KenarCubugu> Menu(string mod = "PROFESYONEL")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<KenarCubugu>();
        // Yetki yüklenince rapor grubu da çizilir (Satış analizi).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-satir-raporlar-satis-analizi")));
        return e;
    }

    /// <summary>Görünen (hidden OLMAYAN) menü satırlarının id'leri.</summary>
    private static List<string> GorunenSatirlar(IRenderedComponent<KenarCubugu> e) =>
        [.. e.FindAll(".kenar-nav .kenar-satir").Where(s => !s.HasAttribute("hidden")).Select(s => s.Id!)];

    private static Dictionary<string, bool> GizlilikHaritasi(IRenderedComponent<KenarCubugu> e) =>
        e.FindAll(".kenar-nav .kenar-satir, .kenar-nav .kenar-grup-baslik")
            .ToDictionary(s => s.Id!, s => s.HasAttribute("hidden"));

    private static KeyboardEventArgs Tus(string k) => new() { Key = k };

    /// <summary>
    /// Kutuya yazar ve yazılan değerin ÇİZİLDİĞİNİ bekler. ⚠ KabukServisi.Ortak
    /// statik olayı paralel test sınıflarından yeniden çizim tetikleyebilir;
    /// Input'tan hemen sonra Find eski ağacı görebiliyordu (tam süitte ölçülen
    /// kararsızlık). Değerin DOM'a yansıması beklenir.
    /// </summary>
    private static void Yaz(IRenderedComponent<KenarCubugu> e, string metin)
    {
        e.Find("#menu-arama-girdi").Input(metin);
        e.WaitForAssertion(() =>
            Assert.Equal(metin, e.Find("#menu-arama-girdi").GetAttribute("value") ?? ""));
    }

    // ── SAF: Sadelestir ve eşleşme kuralı ──────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Sadelestir_bos_ve_bosluklu_metinde_COKMEZ_bos_doner(string metin)
    {
        // K11 kök nedeni: sozcukler[0] boş dizide IndexOutOfRangeException.
        Assert.Equal("", MenuTanimi.Sadelestir(metin));
    }

    [Theory]
    [InlineData("s", "s")]
    [InlineData("ş", "s")]
    [InlineData("İ", "i")]
    [InlineData("ı", "i")]
    [InlineData("Rapor", "rapor")]
    public void Sadelestir_tek_harf_ve_Turkce_harfte_calisir(string metin, string beklenen)
    {
        Assert.Equal(beklenen, MenuTanimi.Sadelestir(metin));
    }

    private static List<string> Eslesenler(string sorgu) =>
        [.. MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok)
            .Where(o => MenuTanimi.AramaEslesir(o, Sozlukler.Tr, sorgu))
            .Select(o => o.Yol)];

    [Fact]
    public void Sat_yalniz_Satis_ve_Satis_analizini_bulur()
    {
        // PAKET EKSIK-B1-SATINALMA: "sat" sözcük başı kuralıyla "Satın alma"yı da bulur (menü sırasıyla).
        Assert.Equal(["/islemler", "/satin-alma", "/raporlar/satis-analizi"], Eslesenler("sat"));
        // Büyük harf ve Türkçe harf katlanır.
        Assert.Equal(["/islemler", "/raporlar/satis-analizi"], Eslesenler("SATIŞ"));
    }

    [Theory]
    [InlineData("cari", "/cariler")]            // S1-g eski adı: Cariler → Müşteriler
    [InlineData("masak", "/uyum")]              // parantez içindeki sözcük
    [InlineData("kâr", "/raporlar/kar-zarar")]  // şapkalı harf + tire ayraç
    [InlineData("ısparta", null)]               // eşleşme yok, ama çökme de yok
    [InlineData("rapor", "/raporlar/kasa")]     // Sadelestir'in attığı genel ek BURADA atılmaz
    [InlineData("denetim izi", "/denetim-izi")] // iki sözcük: her biri bir sözcüğün başı
    public void Eslesme_kurali_eski_ad_ve_Turkce_harfle_calisir(string sorgu, string? icermeli)
    {
        var e = Eslesenler(sorgu);
        if (icermeli is null) Assert.Empty(e);
        else Assert.Contains(icermeli, e);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("***")]
    [InlineData("<>")]
    public void Bos_ya_da_sozcuksuz_sorgu_HICBIR_seyle_eslesmez(string sorgu)
    {
        Assert.Empty(Eslesenler(sorgu));
    }

    [Fact]
    public void Cok_uzun_sorgu_cokmez_ve_eslesmez()
    {
        Assert.Empty(Eslesenler(new string('ş', 5000)));
    }

    // ── bUnit: KENAR ÇUBUĞU KENDİ İÇİNDE SÜZÜLÜR ────────────────────────────

    [Fact]
    public void Sat_yazinca_gorunen_menu_satirlari_YALNIZ_eslesenlerdir()
    {
        var e = Menu();
        Yaz(e, "satış");   // EKSIK-B1-SATINALMA: "sat" artık "Satın alma"yı da bulur (doğru); bu test iki eşleşmeli gezinmeyi sınar

        Assert.Equal(["kenar-satir-islemler", "kenar-satir-raporlar-satis-analizi"], GorunenSatirlar(e));
        // Eşleşen gruplar AÇIK (başlık görünür), eşleşmeyenler başlığıyla GİZLİ.
        Assert.False(e.Find("#kenar-grup-gunluk").HasAttribute("hidden"));
        Assert.False(e.Find("#kenar-grup-rapor").HasAttribute("hidden"));
        foreach (var g in new[] { "musteri-stok", "para-banka", "belge-uyum", "yonetim" })
            Assert.True(e.Find($"#kenar-grup-{g}").HasAttribute("hidden"), g);
        // "Tüm ekranlar" ve alt blok da süzülür.
        Assert.True(e.Find("#kenar-tum-ekranlar").HasAttribute("hidden"));
        Assert.True(e.Find("#kenar-alt-kilavuz").HasAttribute("hidden"));
        Assert.True(e.Find("#kenar-alt-ayarlar").HasAttribute("hidden"));
        // Menü dışında pencere YOK; "eşleşen yok" satırı da yok.
        Assert.Empty(e.FindAll(".kabuk-arama-paneli"));
        Assert.Empty(e.FindAll("#menu-arama-yok"));
    }

    [Fact]
    public void Basit_modda_da_menu_suzulur()
    {
        var e = Menu(ArayuzModuServisi.Basit);
        Yaz(e, "satış");   // EKSIK-B1-SATINALMA: "sat" artık "Satın alma"yı da bulur (doğru); bu test iki eşleşmeli gezinmeyi sınar

        Assert.Equal(["kenar-satir-islemler", "kenar-satir-raporlar-satis-analizi"], GorunenSatirlar(e));
    }

    [Fact]
    public void Eski_adla_arama_calisir_cari_Musterileri_gosterir()
    {
        var e = Menu();
        Yaz(e, "cari");

        Assert.Equal(["kenar-satir-cariler"], GorunenSatirlar(e));
    }

    [Fact]
    public void Alt_blok_aranabilir_ayar_Ayarlari_gosterir()
    {
        var e = Menu();
        Yaz(e, "ayar");

        Assert.Empty(GorunenSatirlar(e));
        Assert.False(e.Find("#kenar-alt-ayarlar").HasAttribute("hidden"));
        Assert.Empty(e.FindAll("#menu-arama-yok"));
    }

    [Fact]
    public void Eslesme_yoksa_menude_TEK_satir_Eslesen_ekran_yok()
    {
        var e = Menu();
        Yaz(e, "zzqx");

        Assert.Empty(GorunenSatirlar(e));
        Assert.Equal("Eşleşen ekran yok", e.Find("#menu-arama-yok").TextContent.Trim());
        Assert.Single(e.FindAll("#menu-arama-yok"));
    }

    [Fact]
    public void Esc_ve_Temizle_menuyu_KATLAMA_TERCIHLI_haline_dondurur_tercihe_YAZMAZ()
    {
        // Kullanıcının kayıtlı tercihi: yalnız Raporlar açık.
        tercihJson = """{"tercihler":{"menu-acik-gruplar":"[\"rapor\"]"}}""";
        var e = Menu();
        e.WaitForAssertion(() => Assert.False(e.Find("#kenar-satir-raporlar-kasa").HasAttribute("hidden")));
        var once = GizlilikHaritasi(e);
        var yazmaOnce = istekler.Count(i => i.Metot is "PATCH" or "DELETE");

        Yaz(e, "sat");
        Assert.NotEqual(once, GizlilikHaritasi(e));
        e.Find("#menu-arama-girdi").KeyDown(Tus("Escape"));
        e.WaitForAssertion(() => Assert.Equal(once, GizlilikHaritasi(e)));
        e.WaitForAssertion(() => Assert.Equal("", e.Find("#menu-arama-girdi").GetAttribute("value") ?? ""));

        Yaz(e, "kasa");
        e.Find("#menu-arama-temizle").Click();
        e.WaitForAssertion(() => Assert.Equal(once, GizlilikHaritasi(e)));

        // Arama katlama tercihini ne okur ne yazar.
        Assert.Equal(yazmaOnce, istekler.Count(i => i.Metot is "PATCH" or "DELETE"));
    }

    [Fact]
    public void Ok_tuslari_eslesenler_arasinda_gezer_Enter_ekrana_gider()
    {
        var e = Menu();
        Yaz(e, "satış");   // EKSIK-B1-SATINALMA: "sat" artık "Satın alma"yı da bulur (doğru); bu test iki eşleşmeli gezinmeyi sınar

        e.Find("#menu-arama-girdi").KeyDown(Tus("ArrowDown"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kenar-satir-islemler").ClassList));
        e.WaitForAssertion(() => Assert.Equal("kenar-satir-islemler", e.Find("#menu-arama-girdi").GetAttribute("aria-activedescendant")));

        e.Find("#menu-arama-girdi").KeyDown(Tus("ArrowDown"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kenar-satir-raporlar-satis-analizi").ClassList));
        e.WaitForAssertion(() => Assert.DoesNotContain("secili", e.Find("#kenar-satir-islemler").ClassList));

        e.Find("#menu-arama-girdi").KeyDown(Tus("ArrowUp"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kenar-satir-islemler").ClassList));
        e.Find("#menu-arama-girdi").KeyDown(Tus("ArrowUp"));   // başa sarar
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kenar-satir-raporlar-satis-analizi").ClassList));

        e.Find("#menu-arama-girdi").KeyDown(Tus("Enter"));
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/raporlar/satis-analizi", nav.Uri, StringComparison.Ordinal);
        // Gidince arama temizlenir, menü eski hâline döner.
        Assert.Equal("", e.Find("#menu-arama-girdi").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Enter_secim_yokken_ILK_eslesene_gider()
    {
        var e = Menu();
        Yaz(e, "sat");
        e.Find("#menu-arama-girdi").KeyDown(Tus("Enter"));

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/islemler", nav.Uri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("s")]
    [InlineData("ş")]
    [InlineData("İ")]
    [InlineData("ı")]
    [InlineData("*?%&<>\\\"'")]
    public void Sinir_metinlerinde_klavye_ve_girdi_COKMEZ(string metin)
    {
        // K11: keydown, bağlama güncellenmeden ÖNCE gelir — ilk tuşta metin boş.
        var e = Menu();
        foreach (var t in new[] { "s", "ArrowDown", "ArrowUp", "Enter" })
            e.Find("#menu-arama-girdi").KeyDown(Tus(t));
        Yaz(e, metin);
        foreach (var t in new[] { "ArrowDown", "ArrowUp", "x" })
            e.Find("#menu-arama-girdi").KeyDown(Tus(t));

        if (string.IsNullOrWhiteSpace(metin))
        {
            // Yazılmamış sayılır: menü süzülmez, "eşleşen yok" satırı çıkmaz.
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#menu-arama-yok")));
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#menu-arama-temizle")));
        }
        else
        {
            e.WaitForElement("#menu-arama-temizle");
        }
    }

    [Fact]
    public void Cok_uzun_metin_menude_COKMEZ_ve_eslesen_yok_der()
    {
        var e = Menu();
        Yaz(e, new string('a', 2000));
        e.Find("#menu-arama-girdi").KeyDown(Tus("ArrowDown"));

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#menu-arama-yok")));
    }

    [Fact]
    public void Favoriler_de_suzulur()
    {
        tercihJson = """{"tercihler":{"menu-favoriler":"[\"/kasalar\",\"/islemler\"]"}}""";
        var e = Menu();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-favori-satir-kasalar")));

        Yaz(e, "sat");

        Assert.False(e.Find("#kenar-favori-satir-islemler").HasAttribute("hidden"));
        Assert.True(e.Find("#kenar-favori-satir-kasalar").HasAttribute("hidden"));
    }

    [Fact]
    public void Menu_kutusunun_yer_tutucusu_Menude_ara_kayit_ifadesi_YOK()
    {
        var e = Menu();
        var yt = e.Find("#menu-arama-girdi").GetAttribute("placeholder")!;
        // T-K2: tuvaldeki yazım "Ekran ara" (önceki S1-j metni "Menüde ara").
        Assert.Equal("Ekran ara", yt);
        Assert.DoesNotContain("Kayıt", yt, StringComparison.Ordinal);
    }
}
