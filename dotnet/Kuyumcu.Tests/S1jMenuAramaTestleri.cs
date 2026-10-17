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

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu ve içindeki menü kutusu (#menu-arama-girdi) kaldırıldı.
    // Ekran araması Yıldız kabuğunda iki yerde yaşar: (a) "Tüm ekranlar" sayfasının arama kutusu
    // (S1dEkranlarTestleri), (b) üst bardaki Ctrl/Cmd+K paletinin EKRANLAR bölümü (GlobalArama) —
    // palet menü kutusuyla AYNI eşleşme kuralını (MenuTanimi.AramaEslesir) ve AYNI yetki süzgecini
    // kullanır. Menü kutusunun bUnit güvenceleri aşağıda bu palet üzerinden sınanır.

    /// <summary>Yetkiler yüklü, palet üst bar düğmesiyle açılmış (gerçek kullanıcı yolu).</summary>
    private IRenderedComponent<GlobalArama> Palet(string mod = "PROFESYONEL")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, mod));
        var yetki = Services.GetRequiredService<YetkiServisi>();
        var e = Render<GlobalArama>();
        e.InvokeAsync(() => yetki.HazirlaAsync()).GetAwaiter().GetResult();
        // Yetki yüklenince rapor ekranları da süzgeçten geçer (Satış analizi).
        Assert.True(yetki.Erisebilir("rapor"), "ön koşul: rapor modülü yüklenmeli");
        e.Find("#ust-kayit-ara").Click();
        Assert.NotNull(e.Find("#kabuk-arama-girdi"));
        return e;
    }

    /// <summary>Paletin EKRANLAR bölümünde çizilen ekran adları (sırayla).</summary>
    private static List<string> EkranSonuclari(IRenderedComponent<GlobalArama> e) =>
        [.. e.FindAll("#kabuk-arama-ekranlar .secici-ad").Select(s => s.TextContent.Trim())];

    private static KeyboardEventArgs Tus(string k) => new() { Key = k };

    /// <summary>
    /// Kutuya yazar ve yazılan değerin ÇİZİLDİĞİNİ bekler. ⚠ KabukServisi.Ortak
    /// statik olayı paralel test sınıflarından yeniden çizim tetikleyebilir;
    /// Input'tan hemen sonra Find eski ağacı görebiliyordu (tam süitte ölçülen
    /// kararsızlık). Değerin DOM'a yansıması beklenir.
    /// </summary>
    private static void Yaz(IRenderedComponent<GlobalArama> e, string metin)
    {
        e.Find("#kabuk-arama-girdi").Input(metin);
        e.WaitForAssertion(() =>
            Assert.Equal(metin, e.Find("#kabuk-arama-girdi").GetAttribute("value") ?? ""));
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

    // ── bUnit: PALETİN EKRANLAR BÖLÜMÜ (menü kutusunun Yıldız karşılığı) ────

    [Fact]
    public void Satis_yazinca_paletteki_ekranlar_YALNIZ_eslesenlerdir()
    {
        // ESKİ: Sat_yazinca_gorunen_menu_satirlari_YALNIZ_eslesenlerdir (kenar satırları hidden ile süzülürdü).
        var e = Palet();
        Yaz(e, "satış");   // EKSIK-B1-SATINALMA: "sat" "Satın alma"yı da bulur (doğru); bu test iki eşleşmeli listeyi sınar

        e.WaitForAssertion(() => Assert.Equal(["Satış", "Satış analizi"], EkranSonuclari(e)));
        // Sonuç palette (aria-modal komut paleti) çizilir; ayrı bir "eşleşen yok" satırı çıkmaz.
        Assert.NotNull(e.Find("#kabuk-arama-palet #kabuk-arama-paneli"));
        Assert.Empty(e.FindAll("#kabuk-arama-yok"));
    }

    [Fact]
    public void Basit_modda_da_palet_ekranlari_suzulur()
    {
        // ESKİ: Basit_modda_da_menu_suzulur. Mod süzgeç değildir (S1-g); yetki süzgeci aynıdır.
        var e = Palet(ArayuzModuServisi.Basit);
        Yaz(e, "satış");

        e.WaitForAssertion(() => Assert.Equal(["Satış", "Satış analizi"], EkranSonuclari(e)));
    }

    [Fact]
    public void Eski_adla_arama_calisir_cari_Musterileri_gosterir()
    {
        var e = Palet();
        Yaz(e, "cari");

        e.WaitForAssertion(() => Assert.Equal([MenuAdlari.Tr.Musteriler.Ad], EkranSonuclari(e)));
    }

    [Fact]
    public void Alt_blok_aranabilir_ayar_Ayarlari_gosterir()
    {
        var e = Palet();
        Yaz(e, "ayar");

        e.WaitForAssertion(() => Assert.Equal([MenuTanimi.AltBlok.Single(o => o.Yol == "/ayarlar").Etiket(Sozlukler.Tr)], EkranSonuclari(e)));
        Assert.Empty(e.FindAll("#kabuk-arama-yok"));
    }

    // TEMA TEMİZLİĞİ 17.10: Eslesme_yoksa_menude_TEK_satir_Eslesen_ekran_yok kaldırıldı — menü kutusu yok; palette ekran da kayıt da yoksa TEK, kısa "yok" satırı KabukAramaEkranTestleri.KATMAN1b_0_kayit_0_ekran_TEK_bos_durum_SIMGESIZ_ve_KISA'da.
    // TEMA TEMİZLİĞİ 17.10: Esc_ve_Temizle_menuyu_KATLAMA_TERCIHLI_haline_dondurur_tercihe_YAZMAZ kaldırıldı — katlanır menü ve katlama tercihi Yıldız'da yok; paletin Esc'i KabukAramaEkranTestleri.Esc_paleti_KAPATIR'da.

    [Fact]
    public void Ok_tuslari_eslesenler_arasinda_gezer_Enter_ekrana_gider()
    {
        var e = Palet();
        Yaz(e, "satış");
        e.WaitForAssertion(() => Assert.Equal(2, EkranSonuclari(e).Count));

        e.Find("#kabuk-arama-girdi").KeyDown(Tus("ArrowDown"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kabuk-arama-ekran-0").ClassList));

        e.Find("#kabuk-arama-girdi").KeyDown(Tus("ArrowDown"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kabuk-arama-ekran-1").ClassList));
        e.WaitForAssertion(() => Assert.DoesNotContain("secili", e.Find("#kabuk-arama-ekran-0").ClassList));

        e.Find("#kabuk-arama-girdi").KeyDown(Tus("ArrowUp"));
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kabuk-arama-ekran-0").ClassList));
        e.Find("#kabuk-arama-girdi").KeyDown(Tus("ArrowUp"));   // başa sarar
        e.WaitForAssertion(() => Assert.Contains("secili", e.Find("#kabuk-arama-ekran-1").ClassList));

        e.Find("#kabuk-arama-girdi").KeyDown(Tus("Enter"));
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/raporlar/satis-analizi", nav.Uri, StringComparison.Ordinal);
        // Gidince palet kapanır, metin silinir.
        Assert.Empty(e.FindAll("#kabuk-arama-palet"));
    }

    [Fact]
    public void Enter_secim_yokken_ILK_eslesene_gider()
    {
        var e = Palet();
        Yaz(e, "sat");
        e.WaitForAssertion(() => Assert.NotEmpty(EkranSonuclari(e)));
        e.Find("#kabuk-arama-girdi").KeyDown(Tus("Enter"));

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/islemler", nav.Uri, StringComparison.Ordinal);
    }

    // TEMA TEMİZLİĞİ 17.10: Sinir_metinlerinde_klavye_ve_girdi_COKMEZ kaldırıldı — menü kutusu yok; AYNI yedi sınır metni ve tuş dizisi palette KabukAramaEkranTestleri.Palet_klavyesi_bos_ve_sinir_metinlerde_COKMEZ'de, boş/boşluklu metinde eşleşme yokluğu yukarıdaki saf testlerde (Sadelestir/Bos_ya_da_sozcuksuz_sorgu).

    [Fact]
    public void Cok_uzun_metin_palette_COKMEZ_ve_ekran_eslesmez()
    {
        // ESKİ: Cok_uzun_metin_menude_COKMEZ_ve_eslesen_yok_der.
        var e = Palet();
        Yaz(e, new string('a', 2000));
        e.Find("#kabuk-arama-girdi").KeyDown(Tus("ArrowDown"));

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-yok")));
        Assert.Empty(EkranSonuclari(e));
        Assert.NotNull(e.Find("#kabuk-arama-palet"));
    }

    // TEMA TEMİZLİĞİ 17.10: Favoriler_de_suzulur kaldırıldı — menüdeki Favoriler satırı Yıldız kabuğunda yok (tuval Bugun4/Tum4).
    // TEMA TEMİZLİĞİ 17.10: Menu_kutusunun_yer_tutucusu_Menude_ara_kayit_ifadesi_YOK kaldırıldı — menü kutusu yok; ekran aramasının "Ekran ara" yer tutucusu "Tüm ekranlar" sayfasında (S1dEkranlarTestleri.Baslik_h1_ve_arama_yer_tutucusu_tuvalden). Paletin yer tutucusu bilerek kayıt + ekran der.
}
