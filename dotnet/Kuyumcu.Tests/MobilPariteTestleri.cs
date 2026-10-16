using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-18 — MOBİL PARİTE (B-9) · TEMA BELİRTEÇLERİ (B-32) · STICKY (B-30)
//
// NEDEN BU DOSYA: 2026-09-21'de gerçek Chrome'da (CDP, 375×812) 44 rotanın
// 44'ü de `document.scrollWidth` 565-814 px veriyordu. Suçlu tablolar DEĞİL,
// ÜST BARDI: `.ust-bar` sarmıyor, `h1` küçülmüyordu. Düzeltme kabuk
// düzeyindedir; bu testler o mekanizmanın GERİ GELMESİNİ engeller.
//
// ⚠ SINIR (dürüstlük, Anayasa md.1): CSS kuralının VARLIĞI burada ölçülür,
//   piksel genişliği ÖLÇÜLMEZ — onu ancak gerçek tarayıcı ölçer ve uygulama
//   raporunda 44 satırlık tablo olarak durur. Bu testler o ölçümün yerine
//   geçmez, onun ön koşullarını kilitler.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MobilPariteCssTestleri
{
    private static List<CssKaynak.Bildirim> App() =>
        CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));

    private static bool Dar(CssKaynak.Bildirim b) =>
        b.AtKural.Contains("max-width: 768px", StringComparison.Ordinal)
        || b.AtKural.Contains("max-width: 480px", StringComparison.Ordinal);

    [Fact]
    public void B9_Ust_bar_DAR_EKRANDA_sarar_ve_baslik_kuculebilir()
    {
        var b = App();

        Assert.Contains(b, x => Dar(x) && x.Secici == ".ust-bar"
                                && x.Ozellik == "flex-wrap" && x.Deger == "wrap");
        Assert.Contains(b, x => Dar(x) && x.Secici == ".ust-bar h1" && x.Ozellik == "min-width" && x.Deger == "0");
        Assert.Contains(b, x => Dar(x) && x.Secici.Contains(".ust-sag", StringComparison.Ordinal)
                                && x.Ozellik == "flex-wrap" && x.Deger == "wrap");
        Assert.Contains(b, x => Dar(x) && x.Secici.Contains(".sube-secici", StringComparison.Ordinal)
                                && x.Ozellik == "flex-wrap" && x.Deger == "wrap");
    }

    [Fact]
    public void B9_Kaydirma_kutusu_MUTLAK_KONUMLU_torununu_da_hapseder()
    {
        // `/isletme` 412 px taşıyordu: `.gorunmez{position:absolute}` kapsayıcı
        // bloğu `.tablo-sar` OLMADIĞI için overflow kırpmasından kaçıyordu.
        Assert.Contains(App(), x => x.Secici == ".tablo-sar"
                                    && x.Ozellik == "position" && x.Deger == "relative");
    }

    [Fact]
    public void B9_Tasma_OVERFLOW_HIDDEN_ile_GIZLENMEZ()
    {
        // Anayasa md.1: "375'e sığdı" görüntüsü kabuk zeminine `overflow-x:hidden`
        // yazarak ELDE EDİLEMEZ. Kaydırma yalnız kendi kutusundadır.
        var yasak = App().Where(x => x.Ozellik is "overflow-x" or "overflow"
                                     && x.Deger.Contains("hidden", StringComparison.Ordinal)
                                     && (x.Secici.Contains(".ana-icerik", StringComparison.Ordinal)
                                         || x.Secici.Contains(".icerik-sutunu", StringComparison.Ordinal)
                                         || x.Secici.Trim() is "body" or "html" or ".uygulama"))
                          .Select(x => $"{x.Secici} {{ {x.Ozellik}: {x.Deger} }}").ToList();
        Assert.True(yasak.Count == 0, "Taşma gizlenmiş: " + string.Join(" · ", yasak));
    }

    [Fact]
    public void B32_Vurgu_yuzeyleri_HAM_PALET_degil_SEMANTIK_belirtecten_gelir()
    {
        // `.ozet-kutu--amber` --amber-50 kullanıyordu: ham palet TEMAYLA DEĞİŞMEZ,
        // karanlık temada açık sarı zemin + açık metin = okunamaz (B-32).
        foreach (var secici in new[] { ".ozet-kutu--amber", ".kart--masak", ".kart--soluk" })
        {
            var bildirimler = App().Where(x => x.Secici == secici).ToList();
            Assert.True(bildirimler.Count > 0, $"{secici} kuralı YOK.");
            foreach (var x in bildirimler)
            {
                Assert.DoesNotContain("--amber-", x.Deger, StringComparison.Ordinal);
                Assert.DoesNotContain("--slate-", x.Deger, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void B32_Razor_dosyalarinda_SABIT_RENK_tavani_asilmaz()
    {
        // Cırcır (YALNIZ azalır). 2026-09-21 taze ölçüm: P-18 öncesi 31, sonrası 14.
        // Kalan 14'ün TAMAMI Pages/Giris.razor'daki tanıtım panelidir; o panel
        // belirtecler.css'te BİLİNÇLİ olarak her iki temada da koyudur
        // (--giris-panel-*), yani tema değişkeni değildir.
        // S1-e (2026-09-23): giriş ekranı tuvale göre yeniden kuruldu; illüstrasyon
        // SVG'si kalktı, Giris.razor'da sabit renk 0. denetim.sh P-18 tavanı zaten 0'dı;
        // cırcır gerçeğe indirildi.
        const int tavan = 0;
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web");
        var desen = new System.Text.RegularExpressions.Regex(@"#[0-9a-fA-F]{3,8}\b|rgba?\([0-9 ,.%/]+\)");
        var bulgular = new List<string>();
        foreach (var klasor in new[] { "Pages", "Bilesenler", "Layout" })
            foreach (var dosya in Directory.GetFiles(Path.Combine(kok, klasor), "*.razor", SearchOption.AllDirectories))
                foreach (System.Text.RegularExpressions.Match m in desen.Matches(File.ReadAllText(dosya)))
                    bulgular.Add($"{Path.GetFileName(dosya)} → {m.Value}");

        Assert.True(bulgular.Count <= tavan,
            $"Razor sabit renk sayısı {bulgular.Count} > tavan {tavan}:\n  " + string.Join("\n  ", bulgular));
        Assert.All(bulgular, x => Assert.StartsWith("Giris.razor", x, StringComparison.Ordinal));
    }

    [Fact]
    public void B30_Yapiskan_ilk_sutun_KartListe_uzerinden_soezlesmeye_baglidir()
    {
        var b = App();
        var kurallar = b.Where(x => x.Secici.Contains("kart-liste--yapiskan", StringComparison.Ordinal)).ToList();
        Assert.True(kurallar.Count > 0, ".kart-liste--yapiskan kuralı YOK.");
        Assert.Contains(kurallar, x => x.Ozellik == "position" && x.Deger == "sticky");
        Assert.Contains(kurallar, x => x.Ozellik == "left" && x.Deger == "0");
        // Zemin belirteçten gelir: karanlık temada satır altından içerik sızmaz.
        Assert.Contains(kurallar, x => x.Ozellik == "background" && x.Deger.StartsWith("var(--", StringComparison.Ordinal));
        Assert.DoesNotContain(kurallar, x => x.Deger.Contains('#', StringComparison.Ordinal));
    }

    [Fact]
    public void B30_Yetkiler_ROL_sutunu_YAPISKAN_isaretlidir()
    {
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Yetkiler.razor");
        // PAKET GOC-D: matris ortak ızgarada (ızgara KartListe'yi kendisi kurar) — yapışkan ilk sütun ve kap kimliği AYNI.
        Assert.Contains("<Izgara", razor, StringComparison.Ordinal);
        Assert.Contains("YapiskanIlkSutun=\"true\"", razor, StringComparison.Ordinal);
        Assert.Contains("KartListeId=\"yetki-matris\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void B9_Veri_tablolari_TEK_sarmalayici_kalibini_kullanir()
    {
        // İkinci sarmalayıcı kalıbı açılmaz: P-18'in sahiplendiği 6 ekranda
        // `<div class="tablo-sar">` elle yazılmaz, KartListe kullanılır.
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages");
        foreach (var ad in new[] { "Cariler", "Urunler", "Isletme", "Banka", "Islemler", "Yetkiler" })
        {
            var metin = File.ReadAllText(Path.Combine(kok, ad + ".razor"));
            Assert.DoesNotContain("<div class=\"tablo-sar\">", metin, StringComparison.Ordinal);
            // PAKET GRID-1: ortak ızgaraya (Bilesenler/Grid/Izgara.razor) geçen liste
            // KartListe'yi ızgaranın İÇİNDEN kullanır — sarmalayıcı yine tektir.
            // PAKET GOC-A: ızgara "@using Kuyumcu.Web.Bilesenler.Grid" + <Izgara …> biçiminde de çağrılır.
            if (metin.Contains("Bilesenler.Grid.Izgara", StringComparison.Ordinal)
                || (metin.Contains("@using Kuyumcu.Web.Bilesenler.Grid", StringComparison.Ordinal) && metin.Contains("<Izgara ", StringComparison.Ordinal))) continue;
            // PAKET GOC-C-2: sayfa kendisi tablo çizmiyorsa (Islemler: satış tuvalinin tabloları Fis bileşenlerinde ortak
            // ızgarayla — KartListe ızgaranın içinde) sarmalayıcı şartı o bileşenlerde aranır (GocC2EkranTestleri.GC2T01).
            if (!System.Text.RegularExpressions.Regex.IsMatch(metin, @"<table\b")) continue;
            Assert.Contains("<KartListe", metin, StringComparison.Ordinal);
        }
        var izgara = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Bilesenler", "Grid", "Izgara.razor"));
        Assert.Contains("<KartListe", izgara, StringComparison.Ordinal);
        Assert.DoesNotContain("<div class=\"tablo-sar\">", izgara, StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KartListe bileşeninin SÖZLEŞMESİ (bUnit — gerçek çizim)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KartListeTestleri : BunitContext
{
    public KartListeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<KartListe> Ciz(bool yapiskan = false, string? id = "test-liste") =>
        Render<KartListe>(p => p
            .Add(x => x.Id, id)
            .Add(x => x.YapiskanIlkSutun, yapiskan)
            .Add(x => x.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
            {
                b.AddMarkupContent(0, "<table class=\"veri-tablosu\"><tbody><tr><td>satır</td></tr></tbody></table>");
            })));

    [Fact]
    public void Tabloyu_KAYDIRILABILIR_ve_KLAVYEYLE_ERISILEBILIR_bolgeye_alir()
    {
        var e = Ciz();
        var sar = e.Find("#test-liste-sar");

        Assert.Contains("tablo-sar", sar.ClassList);
        Assert.Equal("region", sar.GetAttribute("role"));
        Assert.Equal("0", sar.GetAttribute("tabindex"));           // WCAG 2.1.1
        Assert.False(string.IsNullOrWhiteSpace(sar.GetAttribute("aria-label")));
        Assert.Equal("test-liste-ipucu", sar.GetAttribute("aria-describedby"));
        Assert.Single(e.FindAll("#test-liste-sar table.veri-tablosu"));
    }

    [Fact]
    public void Ipucu_metni_SOZLUKTEN_gelir_ciplak_metin_yoktur()
    {
        var e = Ciz();
        var ipucu = e.Find("#test-liste-ipucu");
        Assert.Contains("kaydırılabilir", ipucu.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kart-liste-ipucu", ipucu.ClassList);
    }

    [Fact]
    public void Baslik_verilirse_ERISILEBILIR_AD_olur_verilmezse_sozlukten_gelir()
    {
        var ad = Render<KartListe>(p => p.Add(x => x.Id, "t2").Add(x => x.Baslik, "Cari listesi"))
                 .Find("#t2-sar").GetAttribute("aria-label");
        Assert.Equal("Cari listesi", ad);

        var varsayilan = Render<KartListe>(p => p.Add(x => x.Id, "t3")).Find("#t3-sar").GetAttribute("aria-label");
        Assert.Equal("Veri tablosu", varsayilan);
    }

    [Fact]
    public void YAPISKAN_ILK_SUTUN_yalniz_istenince_isaretlenir()
    {
        Assert.DoesNotContain("kart-liste--yapiskan", Ciz(yapiskan: false).Find(".kart-liste").ClassList);
        Assert.Contains("kart-liste--yapiskan", Ciz(yapiskan: true).Find(".kart-liste").ClassList);
    }

    [Fact]
    public void Id_verilmezse_BOS_id_ozniteligi_BASILMAZ()
    {
        // "id=''" seçicileri bozar ve aria-describedby'ı boşa düşürür.
        var kok = Render<KartListe>(p => p.Add(x => x.Id, null)).Find(".tablo-sar");
        Assert.False(kok.HasAttribute("id"));
        Assert.False(kok.HasAttribute("aria-describedby"));
    }

    [Fact]
    public void Satir_ici_STYLE_uretmez()
    {
        Assert.DoesNotContain("style=", Ciz().Markup, StringComparison.Ordinal);
    }
}
