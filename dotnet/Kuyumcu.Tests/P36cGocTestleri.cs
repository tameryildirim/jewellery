using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-36c — GÖÇ + PENCERE TEK TİPİ [bUnit + kaynak]
//
// Bu paket YENİ ÖZELLİK EKLEMEZ; iki şeyi kilitler:
//   1) ham <select> → ortak <Secici> göçü KİMLİK KORUYARAK yapıldı,
//   2) pencerelerin ölçülmüş üç kusuru kapandı: odak tuzağı (kapanışta odak
//      tetikleyiciye DÖNMÜYORDU), arka plan kaydırma kilidi YOKTU, Esc yolu
//      odağı geri vermiyordu.
//
// ⚠ Testler DAVRANIŞA bakar; "dosyada şu metin var" iddiası yalnız tek-yer
//   kuralı (ikinci mekanizma açılmadı) için kullanılır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class P36cGocTestleri : BunitContext
{
    public P36cGocTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── 1) GÖÇ: kimlik korundu, eleman türü combobox oldu ───────────────────

    /// <summary>
    /// Göçün sözleşmesi: `id` BİREBİR korunur (testler ve QA turları o
    /// kimlikleri kullanır), tür `select` → `combobox` olur.
    /// </summary>
    [Fact]
    public void C36c_1_Gocen_secici_kimligi_korur_ve_combobox_olur()
    {
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "kasa-doviz")
            .Add(x => x.Etiket, "Döviz")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("TRY", "TRY"), new("USD", "USD") })
            .Add(x => x.Deger, "TRY"));

        var tetik = c.Find("#kasa-doviz");
        Assert.Equal("combobox", tetik.GetAttribute("role"));
        // Testlerin ve QA'nın okuduğu gövde kodu DOM'da durur (ekranda değil).
        Assert.Equal("TRY", tetik.GetAttribute("data-deger"));
        Assert.DoesNotContain("TRY", tetik.GetAttribute("class") ?? "");
    }

    /// <summary>Seçenek satırı gövdeye giden kodu `data-deger` ile taşır.</summary>
    [Fact]
    public void C36c_2_Secenek_satiri_gonderilecek_kodu_tasir()
    {
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "t-secim")
            .Add(x => x.Etiket, "Tür")
            .Add(x => x.BosSecenekMetni, "—")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("GIRIS", "Gelir"), new("CIKIS", "Gider") }));

        var degerler = SeciciYardim.Secenekler(c, "t-secim").Select(o => o.Deger()).ToList();
        Assert.Equal(new[] { "", "GIRIS", "CIKIS" }, degerler);
        // UX-S31/B-25: ham kod EKRANDA görünmez, yalnız çevrilmiş ad görünür.
        Assert.Equal(new[] { "—", "Gelir", "Gider" },
            SeciciYardim.Secenekler(c, "t-secim").Select(o => o.Ad()).ToArray());
    }

    /// <summary>Yardımcı, olmayan seçeneği SESSİZCE geçmez (Anayasa md.1).</summary>
    [Fact]
    public void C36c_3_Olmayan_secenek_sessizce_gecilmez()
    {
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "t-secim")
            .Add(x => x.Etiket, "Tür")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("A", "A") }));

        var ex = Assert.Throws<InvalidOperationException>(() => SeciciYardim.Sec(c, "t-secim", "YOK"));
        Assert.Contains("YOK", ex.Message);
    }

    // ── 2) PENCERE: odak tuzağı kapandı ─────────────────────────────────────

    private IRenderedComponent<ModalPencere> Pencere(bool acik, Action? kapandi = null)
        => Render<ModalPencere>(p => p
            .Add(c => c.Acik, acik)
            .Add(c => c.Baslik, "Sınama")
            .Add(c => c.Kapat, () => kapandi?.Invoke())
            .AddChildContent("<p>içerik</p>"));

    /// <summary>
    /// ÖLÇÜLEN HATA: kapanışta `kuyumcuCekmece.kapat` panel DOM'dan ÇIKTIKTAN
    /// SONRA çağrılıyordu → odak tetikleyiciye dönmüyordu. Artık × düğmesine
    /// basıldığı anda, PANEL HÂLÂ DOM'dayken çağrılır.
    /// </summary>
    [Fact]
    public void C36c_4_Kapat_dugmesi_odagi_panel_DOMdayken_geri_verir()
    {
        var kapatCagrisi = JSInterop.SetupVoid("kuyumcuCekmece.kapat", _ => true);
        kapatCagrisi.SetVoidResult();
        var kapandi = false;
        var c = Pencere(true, () => kapandi = true);

        c.Find(".modal-kapat").Click();

        Assert.True(kapandi);                       // ebeveyne haber gitti
        Assert.Single(kapatCagrisi.Invocations);    // odak geri verildi
    }

    /// <summary>Esc yolu da AYNI yolu kullanır — ikinci mekanizma açılmaz.</summary>
    [Fact]
    public void C36c_5_Escape_de_odagi_geri_verir()
    {
        var kapatCagrisi = JSInterop.SetupVoid("kuyumcuCekmece.kapat", _ => true);
        kapatCagrisi.SetVoidResult();
        var c = Pencere(true);

        c.Find(".modal-panel").KeyDown("Escape");

        Assert.Single(kapatCagrisi.Invocations);
    }

    /// <summary>Kapalı pencerede odak geri verme ÇAĞRILMAZ (gereksiz JS yok).</summary>
    [Fact]
    public void C36c_6_Kapali_pencerede_odak_cagrisi_yok()
    {
        var kapatCagrisi = JSInterop.SetupVoid("kuyumcuCekmece.kapat", _ => true);
        kapatCagrisi.SetVoidResult();
        Pencere(false);
        Assert.Empty(kapatCagrisi.Invocations);
    }

    // ── 3) ARKA PLAN KAYDIRMA KİLİDİ — tek yer, sayaçlı ─────────────────────

    /// <summary>
    /// Kilit SAYAÇLIDIR: iç içe/ardışık iki pencere birbirinin kilidini açmaz.
    /// Tek bayrak kullanılsaydı ikinci pencere kapanınca sayfa kilitli kalırdı
    /// (ya da ilki kapanınca erken çözülürdü).
    /// </summary>
    [Fact]
    public void C36c_7_Kaydirma_kilidi_sayacli_ve_TEK_yerde()
    {
        var js = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "cekmece.js");
        // ⚠ TEST AÇIĞI DÜZELTMESİ (mutasyon M36c-3): yalnız "sayaç adı geçiyor
        //   mu" diye bakmak YETMEZ — artırma satırı silinse bile azaltma satırı
        //   adı taşıdığı için test yeşil kalıyordu. Sayacın İKİ UCU da ölçülür.
        Assert.Contains("document.__kuyumcuPencereSayisi = (document.__kuyumcuPencereSayisi || 0) + 1;", js);
        Assert.Contains("var n = (document.__kuyumcuPencereSayisi || 1) - 1;", js);
        Assert.Contains("document.body.classList.add('pencere-acik')", js);
        Assert.Contains("document.body.classList.remove('pencere-acik')", js);

        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        Assert.Contains("body.pencere-acik { overflow-y: hidden; }", css);
        // md.1 / P-18: YATAY taşma gizlenmez — kilit yalnız dikeydir.
        Assert.DoesNotContain("body.pencere-acik { overflow: hidden", css);
    }

    /// <summary>
    /// Sessiz yutma yasağı (md.1): odak geri verme `catch { }` ile YUTULMAZ;
    /// yalnız devre koptuğunda (JSDisconnectedException) sessiz kalır.
    /// </summary>
    [Fact]
    public void C36c_8_Odak_geri_verme_hatasi_YUTULMAZ()
    {
        foreach (var dosya in new[] { "ModalPencere.razor", "Cekmece.razor" })
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", dosya);
            var i = kaynak.IndexOf("private async Task OdakGeriVer()", StringComparison.Ordinal);
            Assert.True(i > 0, $"{dosya}: OdakGeriVer yok — odak tuzağı düzeltmesi kayıp.");
            var govde = kaynak[i..Math.Min(kaynak.Length, i + 400)];
            Assert.Contains("catch (JSDisconnectedException)", govde);
            Assert.DoesNotContain("catch { ", govde);
        }
    }

    /// <summary>
    /// TEK DÜZENLEME KALIBI (DoD EK md.2): kapatma yolu TEK — hem × hem Esc
    /// `KapatIstegi`ye gider; ikinci bir "Kapat.InvokeAsync()" yolu açılmaz.
    /// </summary>
    [Fact]
    public void C36c_9_Kapatma_yolu_TEKtir()
    {
        foreach (var dosya in new[] { "ModalPencere.razor", "Cekmece.razor" })
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", dosya);
            var kod = CssKaynak.YorumsuzKod(kaynak);
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(kod, @"Kapat\.InvokeAsync\(\)"));
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-36c/2 — GÖÇÜN BİTİŞ KİLİTLERİ (ikinci tur, 2026-09-21)
//
// Doğrulayıcı bulgusu: "HAM_SELECT → 0 ölçülmedi, PENCERE_* sayaçları yok".
// Aşağıdaki testler bu iddiaların KAYNAKTAN ölçülebilir karşılığını kurar ve
// denetim.sh sayaçlarıyla AYNI kuralı iki ayrı yerden kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class P36c2GocBitisTestleri
{
    /// <summary>Kapsam dışı (kilitli) ekranlar — kendi paketlerine devredildi.</summary>
    private static readonly string[] KilitliEkranlar =
    [
        "Faturalar", "KarZarar", "KasaRaporu", "Kilavuz", "Pos",
        "Raporlar", "SatisAnalizi", "Sayim", "Siparisler",
    ];

    /// <summary>Razor (@* *@) ve C# (//) yorumları bir DENETİM ÖĞESİ değildir.</summary>
    private static string Yorumsuz(string kaynak)
    {
        var s = System.Text.RegularExpressions.Regex.Replace(
            kaynak, @"@\*.*?\*@", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        return System.Text.RegularExpressions.Regex.Replace(
            s, @"(?m)^\s*//.*$", "");
    }

    /// <summary>Pages + Bilesenler + Layout altındaki TÜM .razor dosyaları.</summary>
    private static IEnumerable<(string Ad, string Metin)> TumRazor()
    {
        foreach (var klasor in new[] { "Pages", "Bilesenler", "Layout" })
        {
            var dizin = CssKaynak.Yol("Kuyumcu.Web", klasor);
            if (!System.IO.Directory.Exists(dizin)) continue;
            foreach (var y in System.IO.Directory.EnumerateFiles(dizin, "*.razor",
                         System.IO.SearchOption.AllDirectories))
                yield return (System.IO.Path.GetFileName(y), System.IO.File.ReadAllText(y));
        }
    }

    private static IEnumerable<(string Ad, string Metin)> SerbestEkranlar()
    {
        foreach (var (ad, metin) in TumRazor())
        {
            if (KilitliEkranlar.Contains(System.IO.Path.GetFileNameWithoutExtension(ad))) continue;
            yield return (ad, metin);
        }
    }

    /// <summary>
    /// P-36c/2 ana ürünü: SERBEST ekranlarda ham &lt;select&gt; KALMAZ.
    /// Kilitli ekranlar kapsam dışıdır ve ayrı sayaçla ölçülür.
    /// </summary>
    [Fact]
    public void C36c2_1_Serbest_ekranlarda_ham_select_KALMAZ()
    {
        var kalanlar = SerbestEkranlar()
            .Where(x => System.Text.RegularExpressions.Regex.IsMatch(Yorumsuz(x.Metin), @"<select[ >]"))
            .Select(x => x.Ad)
            .ToList();

        Assert.True(kalanlar.Count == 0,
            "Serbest ekranda ham <select> kaldı: " + string.Join(", ", kalanlar));
    }

    /// <summary>
    /// Tek tip düğme sırası: pencerelerde İKİNCİL ÖNCE, BİRİNCİL SONDA.
    /// (Ölçülen çoğunluk 20/25 bu sıradaydı; Çekmece ve 4 pencere buna çekildi.)
    /// </summary>
    [Fact]
    public void C36c2_2_Pencere_dugme_sirasi_TEK_TIPTIR()
    {
        var ters = new List<string>();
        foreach (var (ad, metin) in TumRazor())
        {
            var bloklar = System.Text.RegularExpressions.Regex
                .Matches(metin, @"<ModalPencere\b.*?</ModalPencere>",
                    System.Text.RegularExpressions.RegexOptions.Singleline)
                .Select(m => m.Value)
                .Concat(System.Text.RegularExpressions.Regex
                    .Matches(metin, @"<footer class=""cekmece-alt"">.*?</footer>",
                        System.Text.RegularExpressions.RegexOptions.Singleline)
                    .Select(m => m.Value));

            foreach (var blok in bloklar)
            {
                var dizi = string.Concat(System.Text.RegularExpressions.Regex
                    .Matches(blok, @"<button[^>]*class=""[^""]*(?:btn|dugme)-(birincil|ikincil)[^""]*""",
                        System.Text.RegularExpressions.RegexOptions.Singleline)
                    .Select(m => m.Groups[1].Value == "birincil" ? "B" : "I"));
                if (!dizi.Contains('B') || !dizi.Contains('I')) continue;
                if (dizi.IndexOf('B') < dizi.IndexOf('I')) ters.Add(ad);
            }
        }

        Assert.True(ters.Count == 0,
            "Birincil düğme ikincilden ÖNCE gelen pencere: " + string.Join(", ", ters));
    }

    /// <summary>Boş durum METİNSİZ olamaz — "kayıt yok" bile demeyen boş ekran yasak.</summary>
    [Fact]
    public void C36c2_3_Bos_durum_METINSIZ_olamaz()
    {
        var metinsiz = new List<string>();
        foreach (var (ad, metin) in TumRazor())
        {
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(metin, @"<BosDurum[^>]*>"))
            {
                if (!m.Value.Contains("Mesaj=")) metinsiz.Add($"{ad}: {m.Value}");
            }
        }
        Assert.True(metinsiz.Count == 0, "Metinsiz <BosDurum>: " + string.Join(" | ", metinsiz));
    }

    /// <summary>
    /// Anayasa md.1 — SESSİZ YUTMA YASAK: pencere bileşenlerinde çıplak
    /// `catch { }` kalmaz; her yakalama BEKLENEN istisna türünü söyler.
    /// (Doğrulayıcı bulgusu: P-36c yalnız odak yolunu daraltmıştı.)
    /// </summary>
    [Fact]
    public void C36c2_5_Pencere_bilesenlerinde_CIPLAK_catch_kalmaz()
    {
        var suclu = new List<string>();
        foreach (var dosya in new[] { "ModalPencere.razor", "Cekmece.razor" })
        {
            var kod = CssKaynak.YorumsuzKod(
                CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", dosya));
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(kod, @"catch\s*(\(|\{)"))
            {
                if (m.Groups[1].Value == "{") suclu.Add($"{dosya}@{m.Index}");
            }
        }
        Assert.True(suclu.Count == 0, "Çıplak catch: " + string.Join(", ", suclu));
    }

    /// <summary>
    /// Pencere BOYU sayılıdır: `.modal-panel` / `.cekmece` üzerinde en çok 3
    /// farklı genişlik bildirimi olur (tam ekran mobil geri dönüşü boy değildir).
    /// </summary>
    [Fact]
    public void C36c2_4_Pencere_genislik_cesidi_EN_COK_UCTUR()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var boylar = new HashSet<string>();
        foreach (System.Text.RegularExpressions.Match blok in
                 System.Text.RegularExpressions.Regex.Matches(
                     css, @"\.(?:modal-panel|cekmece)(?![\w-])[^{]*\{([^}]*)\}",
                     System.Text.RegularExpressions.RegexOptions.Singleline))
        {
            foreach (System.Text.RegularExpressions.Match d in
                     System.Text.RegularExpressions.Regex.Matches(
                         blok.Groups[1].Value, @"(?:max-)?width\s*:\s*([^;]+)"))
            {
                var v = d.Groups[1].Value.Trim();
                if (v is "none" or "100vw" or "100%" or "0") continue;
                boylar.Add(v);
            }
        }
        Assert.True(boylar.Count <= 3, "Pencere boyu çeşidi: " + string.Join(", ", boylar));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SEÇİCİ TEST YARDIMCISININ KENDİ KAPISI (P-36c/2)
//
// Mutasyon M36c2-7 açığı: yardımcının "kilitli seçicide istenen değer ZATEN
// seçiliyse no-op, DEĞİLSE kır" kuralını hiçbir test sınamıyordu — kural
// kaldırılınca 27 test yine yeşil kalıyordu. Yani kilitli bir seçiciye yanlış
// değer "atanmış gibi" sessizce geçilebilirdi (Anayasa md.1 ihlali).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SeciciYardimKapisiTestleri : BunitContext
{
    public SeciciYardimKapisiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<Secici> Kilitli(string deger) =>
        Render<Secici>(p => p
            .Add(x => x.ElemanId, "kilitli-sec")
            .Add(x => x.Etiket, "Kasa")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("ks1", "Merkez Kasa") })
            .Add(x => x.Deger, deger)
            .Add(x => x.Devredisi, true)
            .Add(x => x.DevredisiSebebi, "Tek kasa var; sistem seçti."));

    [Fact]
    public void Kilitli_secicide_ZATEN_secili_deger_istenirse_NO_OP()
    {
        var c = Kilitli("ks1");
        SeciciYardim.Sec(c, "kilitli-sec", "ks1");   // istisna ATMAZ
        Assert.Equal("ks1", SeciciYardim.SeciliDeger(c, "kilitli-sec"));
    }

    [Fact]
    public void Kilitli_secicide_BASKA_deger_istenirse_test_SESSIZCE_GECMEZ()
    {
        var c = Kilitli("ks1");
        var ex = Assert.Throws<InvalidOperationException>(
            () => SeciciYardim.Sec(c, "kilitli-sec", "ks2"));
        Assert.Contains("KİLİTLİ", ex.Message);
        Assert.Contains("Tek kasa var", ex.Message);   // sebep de yazılır (UX-S30)
    }
}
