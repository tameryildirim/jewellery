using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-40 — BULUNAMAYAN ROTA (/vade-takvimi) EKRANI
//
// HATANIN YENİDEN ÜRETİLMESİ: tasarım denetimi `/vade-takvimi` adresini 4
// genişlikte de "boş içerik" (<40 karakter) ölçtü ve bozuk sayfa diye eskale
// etti. Kök neden: BÖYLE BİR ROTA YOK — Router'ın <NotFound> bloğu tek bir 18
// karakterlik cümleden ibaretti. Kullanıcı ne olduğunu, nereye gideceğini
// göremiyordu → UX-S28 (sessiz boş ekran) ihlali.
//
// Aşağıdaki testler ekranın GERÇEKTEN dolduğunu ölçer: başlık + hata bandı +
// istenen adres + çıkış yolu. Metin uzunluğu eşiği denetçinin eşiğidir (40).
//
// ⚠ Öneri üretimi SAF sınıfta (RotaOnerisi) sınanır; bileşen yalnız çizer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RotaOnerisiSafTestleri
{
    private static readonly Sozluk Tr = Sozlukler.Tr;

    /// <summary>
    /// Denetçinin ölçtüğü asıl adres. "/vade-takvimi" iki gerçek ekranın
    /// adını karıştırmaktan doğuyor; ikisi de önerilmeli.
    /// </summary>
    [Fact]
    public void Vade_takvimi_hem_vade_takibini_hem_evrak_takvimini_onerir()
    {
        var yollar = RotaOnerisi.Oner("/vade-takvimi", Tr).Select(o => o.Yol).ToList();

        Assert.Contains("/raporlar/vade-takibi", yollar);
        Assert.Contains("/evraklar/takvim", yollar);
    }

    /// <summary>Uydurma öneri yasak (Anayasa md.1): hiçbir parça tutmuyorsa liste BOŞ.</summary>
    [Fact]
    public void Hicbir_parca_tutmuyorsa_oneri_URETILMEZ()
    {
        Assert.Empty(RotaOnerisi.Oner("/zzzqqqwww", Tr));
    }

    [Fact]
    public void Bos_ya_da_kok_adres_icin_oneri_uretilmez()
    {
        Assert.Empty(RotaOnerisi.Oner("/", Tr));
        Assert.Empty(RotaOnerisi.Oner("", Tr));
        Assert.Empty(RotaOnerisi.Oner(null, Tr));
    }

    /// <summary>Uzun liste "bulunamadı" mesajını boğardı: tavan 3.</summary>
    [Fact]
    public void Oneri_sayisi_tavani_asmaz()
    {
        Assert.True(RotaOnerisi.Oner("/rapor-kasa-banka-cari-urun", Tr).Count
                    <= RotaOnerisi.EnCokOneri);
    }

    /// <summary>Her öneri GERÇEK bir menü rotasıdır — ölü bağlantı yasak (DoD ek 1).</summary>
    [Fact]
    public void Onerilen_her_yol_gercek_bir_menu_rotasidir()
    {
        var gercek = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok)
            .Select(o => o.Yol).ToHashSet(StringComparer.Ordinal);

        foreach (var yol in new[] { "/vade-takvimi", "/kasalar-x", "/urun-agac" })
            foreach (var o in RotaOnerisi.Oner(yol, Tr))
                Assert.Contains(o.Yol, gercek);
    }

    /// <summary>Etiket sözlükten gelir; çıplak metin yok (UX-S31).</summary>
    [Fact]
    public void Oneri_etiketi_sozlukten_gelir_ve_dile_gore_degisir()
    {
        var tr = RotaOnerisi.Oner("/vade-takvimi", Tr)
            .First(o => o.Yol == "/evraklar/takvim").Etiket;
        var en = RotaOnerisi.Oner("/vade-takvimi", Sozlukler.En)
            .First(o => o.Yol == "/evraklar/takvim").Etiket;

        Assert.NotEqual(tr, en);
        Assert.False(string.IsNullOrWhiteSpace(tr));
        Assert.False(string.IsNullOrWhiteSpace(en));
    }
}

public sealed class RotaYokEkranTestleri : BunitContext
{
    /// <summary>Denetçinin "boş içerik" eşiği.</summary>
    private const int DenetciEsigi = 40;

    public RotaYokEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<RotaYok> Ciz(string adres)
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(adres);
        return Render<RotaYok>();
    }

    /// <summary>HATAYI YENİDEN ÜRETEN TEST: eski <NotFound> 18 karakterdi.</summary>
    [Fact]
    public void Bulunamayan_adres_ekrani_BOS_DEGILDIR()
    {
        var e = Ciz("/vade-takvimi");

        var metin = e.Find("#rota-yok").TextContent.Trim();
        Assert.True(metin.Length >= DenetciEsigi,
            $"bulunamayan rota ekranı {metin.Length} karakter — sessiz boş ekran (UX-S28)");
    }

    /// <summary>UX-S28: bu bir HATA durumudur, "kayıt yok" değil → hata bandı görünür.</summary>
    [Fact]
    public void Hata_bandi_ve_baslik_gorunur()
    {
        var e = Ciz("/vade-takvimi");

        Assert.Equal(Sozlukler.Tr.Kabuk.RotaYokBaslik, e.Find("#rota-yok-baslik").TextContent.Trim());
        Assert.Contains(Sozlukler.Tr.Kabuk.RotaYokAciklama, e.Find("#rota-yok-bant").TextContent);
        Assert.Equal("alert", e.Find("#rota-yok-bant").GetAttribute("role"));
    }

    /// <summary>Kullanıcı hangi adresi istediğini görmeli; yoksa yazım hatasını bulamaz.</summary>
    [Fact]
    public void Istenen_adres_ekranda_yazar_ve_sorgu_dizesi_kirpilir()
    {
        Assert.Equal("/vade-takvimi", Ciz("/vade-takvimi").Find("#rota-yok-adres").TextContent.Trim());
        Assert.Equal("/vade-takvimi", Ciz("/vade-takvimi?q=1#x").Find("#rota-yok-adres").TextContent.Trim());
    }

    /// <summary>DoD ek 1: çıkış yolu GERÇEK bağlantıdır, ölü düğme değil.</summary>
    [Fact]
    public void Panele_donus_baglantisi_vardir()
    {
        Assert.Equal("/", Ciz("/vade-takvimi").Find("#rota-yok-panele").GetAttribute("href"));
    }

    [Fact]
    public void Benzer_ekran_onerileri_tiklanabilir_baglanti_olarak_cizilir()
    {
        var e = Ciz("/vade-takvimi");
        var yollar = e.FindAll("#rota-yok-oneriler a")
            .Select(a => a.GetAttribute("href")!).ToList();

        Assert.Contains("/raporlar/vade-takibi", yollar);
        Assert.Contains("/evraklar/takvim", yollar);
        Assert.All(yollar, y => Assert.StartsWith("/", y));
    }

    /// <summary>Öneri yoksa boş başlık çizilmez — yalancı bölüm olmaz.</summary>
    [Fact]
    public void Oneri_yoksa_oneri_bolumu_HIC_cizilmez()
    {
        var e = Ciz("/zzzqqqwww");

        Assert.Empty(e.FindAll("#rota-yok-oneriler"));
        Assert.True(e.Find("#rota-yok").TextContent.Trim().Length >= DenetciEsigi);
    }
}

public sealed class RotaYokYonlendiriciTestleri
{
    private static string ProjeKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamad\u0131");
    }

    private static string App() =>
        File.ReadAllText(Path.Combine(ProjeKoku(), "Kuyumcu.Web", "App.razor"));

    /// <summary>
    /// Router'ın &lt;NotFound&gt; bloğu RotaYok ekranını çizmezse bütün P-40 işi
    /// devre dışı kalır: bileşen testleri yeşil kalır, kullanıcı yine boş ekran görür.
    /// </summary>
    [Fact]
    public void NotFound_blogu_RotaYok_ekranini_cizer()
    {
        var app = App();

        Assert.Contains("<NotFound>", app, StringComparison.Ordinal);
        Assert.Contains("RotaYok", app, StringComparison.Ordinal);
    }

    /// <summary>Eski tek cümlelik 404 geri gelmesin (18 karakter = "boş içerik").</summary>
    [Fact]
    public void Eski_tek_cumlelik_404_geri_GELMEZ()
    {
        Assert.DoesNotContain("<p role=\"alert\">Sayfa bulunamad\u0131.</p>", App(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Denetçinin ölçtüğü `/vade-takvimi` gerçekten kayıtlı bir rota DEĞİLDİR;
    /// bir gün eklenirse bu test kırmızıya döner ve raporun gerekçesi güncellenir.
    /// </summary>
    [Fact]
    public void Vade_takvimi_kayitli_bir_rota_degildir()
    {
        var rotalar = typeof(Kuyumcu.Web.App).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>())
            .Select(r => r.Template)
            .ToList();

        Assert.NotEmpty(rotalar);
        Assert.DoesNotContain("/vade-takvimi", rotalar);
    }
}
