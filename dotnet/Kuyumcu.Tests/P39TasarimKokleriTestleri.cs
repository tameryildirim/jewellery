using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-39 — TASARIM DENETİMİ KÖK NEDENLERİ (8 kök · 43 ekran)
//
// Kaynak ölçüm: dokumanlar/09-uygulama-kayitlari/TASARIM-DENETIMI-2026-09-21.md
//   43 rota + 25 açılan form/çekmece = 68 anahtar × 4 genişlik = 267 ölçüm
//   satırı, tamamı getBoundingClientRect ile. Göz kararı YOK.
//
// ⚠ NE ÖLÇÜLÜR, NE ÖLÇÜLMEZ (Anayasa md.1): birim testi TARAYICI DEĞİLDİR,
//   piksel hesaplamaz. Burada kusuru DOĞURAN MEKANİZMANIN kaynakta durduğu
//   sınanır; piksel tarafı denetçinin betiğiyle tarayıcıda ölçülür ve rapora
//   sayıyla yazılır (betikler/p39-tasarim-olcum.js · rapor § Canlı ölçüm).
//
// ⚠ Sayaç karşılıkları (dotnet/denetim.sh `# >>> P-39` bloğu):
//   UST_BAR_TASMA_1024=0 · FORM_SATIR_HIZA_SAPMASI=0 · DOKUNMA_HEDEFI_KUCUK=0
//   DUGME_YUKSEKLIK_CESIDI<=2 · TABLO_SARMALAYICISIZ=0 · PANEL_VIEWPORT_DISI=0
//   ONIZLEME_MUKERRER=0 · KONTROL_YUKSEKLIK_SAPMASI=0
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P39TasarimKokleriTestleri : BunitContext
{
    public P39TasarimKokleriTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string Metin(params string[] p) =>
        File.ReadAllText(Path.Combine(new[] { Kok() }.Concat(p).ToArray()));

    private static string AppCss() => Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");

    /// <summary>
    /// `/* … *​/` bloklarını BOŞLUKLA değiştirir (konumlar kaymasın). Şart:
    /// bu dosyanın yorumları da "@media" ve "eval(" geçiyor; yorum metni
    /// mekanizma sanılırsa test yalan söyler (Anayasa md.1).
    /// </summary>
    private static string Yorumsuz(string k)
    {
        var b = new System.Text.StringBuilder(k.Length);
        for (var i = 0; i < k.Length; i++)
        {
            if (k[i] == '/' && i + 1 < k.Length && k[i + 1] == '*')
            {
                var son = k.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (son < 0) son = k.Length - 2;
                for (var j = i; j <= son + 1 && j < k.Length; j++) b.Append(k[j] == '\n' ? '\n' : ' ');
                i = son + 1;
                continue;
            }
            if (k[i] == '/' && i + 1 < k.Length && k[i + 1] == '/')
            {
                var son = k.IndexOf('\n', i);
                if (son < 0) son = k.Length;
                for (var j = i; j < son; j++) b.Append(' ');
                i = son - 1;
                continue;
            }
            b.Append(k[i]);
        }
        return b.ToString();
    }
    private static string Belirtecler() => Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");

    /// <summary>Metnin bir `@media` bloğunun İÇİNDE mi yoksa taban kuralda mı olduğunu söyler.</summary>
    private static bool MedyaSorgusuIcinde(string css, int konum)
    {
        var derinlik = 0;
        var medyaDerinligi = -1;
        for (var i = 0; i < konum; i++)
        {
            if (css[i] == '{') derinlik++;
            else if (css[i] == '}')
            {
                derinlik--;
                // Blok kapandığında at-kural da kapanır: `>` değil `>=`.
                if (medyaDerinligi >= derinlik) medyaDerinligi = -1;
            }
            else if (css[i] == '@' && css.AsSpan(i).StartsWith("@media"))
                medyaDerinligi = derinlik;
        }
        return medyaDerinligi >= 0;
    }

    // ═══ K1 — ÜST BAR 1024 px'te BELGEYİ YATAY KAYDIRIYORDU ═════════════════
    // Ölçüldü: 43 rotanın TAMAMI 1024 px'te 91-160 px kayıyor; `scrollTo(2000,0)`
    // sonrası scrollX = 124,5 (/urunler). Kaynak: ust-sag → #ust-mod-anahtar.

    [Fact]
    public void K1_Ust_bar_sarmasi_MEDYA_SORGUSU_DISINDA_tabanda_durur()
    {
        var css = Yorumsuz(AppCss());

        var i = css.IndexOf("\n.ust-bar   { flex-wrap: wrap;", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te `.ust-bar` taban sarma kuralı YOK — 1024 px'te 43 ekran yatay kayar.");
        Assert.False(MedyaSorgusuIcinde(css, i),
            "`.ust-bar` sarması bir @media bloğunun içinde — 769-1100 px bandı yine taşar (P-18'in hatası buydu).");

        var j = css.IndexOf("\n.ust-sag   { flex-wrap: wrap;", StringComparison.Ordinal);
        Assert.True(j > 0, "app.css'te `.ust-sag` taban sarma kuralı YOK.");
        Assert.False(MedyaSorgusuIcinde(css, j), "`.ust-sag` sarması @media içine hapsedilmiş.");
    }

    [Fact]
    public void K1_Tasma_overflow_hidden_ile_GIZLENMEZ()
    {
        // Anayasa md.1: taşma çözülür, saklanmaz.
        var css = Yorumsuz(AppCss());
        foreach (var secici in new[] { ".ust-bar", ".ust-sag", ".icerik-sutunu", ".ana-icerik" })
        {
            var i = css.IndexOf("\n" + secici + " ", StringComparison.Ordinal);
            while (i > 0)
            {
                var acilis = css.IndexOf('{', i);
                var kapanis = css.IndexOf('}', acilis);
                var govde = css[(acilis + 1)..kapanis];
                Assert.DoesNotContain("overflow-x: hidden", govde);
                Assert.DoesNotContain("overflow: hidden", govde);
                i = css.IndexOf("\n" + secici + " ", kapanis, StringComparison.Ordinal);
            }
        }
    }

    // ═══ K2 — SATIR ETİKETE GÖRE HİZALANIYORDU ══════════════════════════════
    // 28 kesin (kademe A) kusur / 8 satır kalıbı / 6 ekran; 10,2 - 41 px.

    [Fact]
    public void K2_Alan_hucresi_UC_RAYLI_cizilir_etiket_kontrol_alt()
    {
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-adet")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.Deger, 3m));

        var hucre = c.Find("#p39-adet-sar");
        var cocuklar = hucre.Children;

        // TAM ÜÇ çocuk şarttır: subgrid kendi içinde örtük ray AÇAMAZ; dördüncü
        // çocuk üçüncü rayın üstüne yığılır ve hiza kusuru geri gelir.
        Assert.Equal(3, cocuklar.Length);
        Assert.Contains("alan-etiket", cocuklar[0].ClassName);
        Assert.Equal("alan-kontrol", cocuklar[1].ClassName);
        Assert.Equal("alan-alt", cocuklar[2].ClassName);

        // Kontrol GERÇEKTEN ikinci rayda (kutu kabuğun içinde değil dışında olamaz).
        Assert.NotNull(cocuklar[1].QuerySelector("input#p39-adet"));
    }

    [Fact]
    public void K2_Alan_alti_iceriginin_TAMAMI_ucuncu_raydadir()
    {
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-oran")
            .Add(x => x.Etiket, "Oran")
            .Add(x => x.Ipucu, "Yüzde olarak yazın")
            .Add(x => x.Hata, "Geçersiz değer"));

        var hucre = c.Find("#p39-oran-sar");
        Assert.Equal(3, hucre.Children.Length);

        var alt = hucre.Children[2];
        Assert.Equal("alan-alt", alt.ClassName);
        // İpucu ve hata AYNI rayda, kontrolün ALTINDA (DoD EK md.3).
        Assert.NotNull(alt.QuerySelector("#p39-oran-ipucu"));
        Assert.NotNull(alt.QuerySelector("[role=alert]"));
    }

    [Fact]
    public void K2_Satir_kapsayicilari_SUBGRID_ile_ortak_raya_oturur()
    {
        var css = AppCss();
        Assert.Contains(".form-grid-2 > .alan, .form-grid-3 > .alan,", css);
        Assert.Contains("grid-template-rows: subgrid", css);
        Assert.Contains(".satir:has(> .alan)", css);
        // Alan OLMAYAN öğe (düğme) üç rayı birden kaplar, kontrol hizasını bozmaz.
        Assert.Contains(".form-grid-2 > :not(.alan):not(.secici-alan):not(.arama-kutu)", css);
    }

    [Fact]
    public void K2_Kabuksuz_ogeler_de_KONTROL_rayina_oturur()
    {
        // CANLI ÖLÇÜM ARTIĞI (2026-09-21, 5198): ilk subgrid kuralından sonra
        // üç kalıp kalmıştı ve üçü de `.alan` OLMAYAN öğelerden doğuyordu —
        //   `div.arama-kutu` (Arama bileşeni): /emanet 10,5 px · /urunler/etiketler 8 px
        //   kabuk yapısı taşımayan sarmalayıcı `.alan` (yalnız düğme): /pos/tahsilatlar
        // İkisi de KONTROL rayına (2) oturur; "en alta yasla" yetmez.
        var css = AppCss();
        Assert.Contains(".form-grid-2 > .arama-kutu, .form-grid-3 > .arama-kutu { grid-row: 2 / 3;", css);
        Assert.Contains(".satir:has(> .alan) > .arama-kutu, .satir:has(> .secici-alan) > .arama-kutu", css);
        // Çok çocuklu sarmalayıcı HARİÇ: hepsini tek raya koymak onları yan yana
        // sütunlara dağıtıyor (/veri-ice-aktarim @375 ana içerik 72 px kaydı).
        Assert.Contains(".alan:not(:has(> .alan-kontrol)):not(:has(> .secici-alan)) > :only-child", css);
    }

    [Fact]
    public void K2_IKINCI_ALAN_AILESI_de_ayni_uc_raya_oturur()
    {
        // `Secici` kendi kabuğunu çizer (`.secici-alan`). Aile dışarıda kalınca
        // /emanet satırı 10,5 px, /urunler/etiketler 8 px kayıyordu (ölçüldü).
        var css = AppCss();
        Assert.Contains(".secici-alan { display: grid; grid-template-rows: auto auto auto;", css);
        Assert.Contains(".form-grid-2 > .secici-alan, .form-grid-3 > .secici-alan", css);
        Assert.Contains(".satir:has(> .secici-alan)", css);
        // Sarmalayıcı `.alan` içindeki ikinci aile rayları AKTARIR.
        Assert.Contains(".alan > .secici-alan { grid-row: span 3; grid-template-rows: subgrid;", css);
    }

    [Fact]
    public void K2_Alt_izgara_bosluğu_UST_IZGARADAN_gelir()
    {
        // Alt ızgaranın kendi satır boşluğu rayları kaydırıyordu: hücre .25rem,
        // satır .5rem olunca kontrol 4 px yukarı çıkıyordu (3 genişlikte ölçüldü).
        var css = AppCss();
        var blok = css[css.IndexOf(".form-grid-2 > .alan, .form-grid-3 > .alan,", StringComparison.Ordinal)..];
        blok = blok[..blok.IndexOf('}')];
        Assert.Contains("row-gap: inherit", blok);
    }

    [Fact]
    public void K5_Dosya_secici_girdi_kapsayicisini_ASMAZ()
    {
        // Yerel dosya girdisinin içsel genişliği vardır ve esnek kutuda küçülmez.
        Assert.Contains("input[type=\"file\"].girdi { max-width: 100%; }", AppCss());
    }

    [Fact]
    public void K2_Ekran_basina_satir_ici_hizalama_yamasi_GERI_GELMEZ()
    {
        // `/islemler` üç satırda `style="align-items:flex-end"` taşıyordu:
        // hem satır içi style yasağını hem "kök neden" kuralını çiğniyordu —
        // ve ölçüm gösterdi ki İŞE DE YARAMIYORDU (19 px kusur duruyordu).
        var kok = Kok();
        var acik = new List<string>();
        foreach (var klasor in new[] { "Pages", "Bilesenler", "Layout" })
            foreach (var f in Directory.GetFiles(Path.Combine(kok, "Kuyumcu.Web", klasor), "*.razor", SearchOption.AllDirectories))
                if (File.ReadAllText(f).Contains("style=\"align-items", StringComparison.Ordinal))
                    acik.Add(Path.GetFileName(f));

        Assert.Empty(acik);
    }

    // ═══ K3 — DOKUNMA HEDEFİ (375 px'te 443/455 = %97,4) ════════════════════

    [Fact]
    public void K3_Telefonda_dokunma_hedefi_44_pikselin_ALTINA_INEMEZ()
    {
        var t = Belirtecler();

        // Yoğunluk ve mod belirteçleri masaüstünde 2rem'e (32 px) iniyordu;
        // telefonda bu kilit onları EZER (blok dosyanın SONUNDA, aynı özgüllük).
        var i = t.IndexOf("@media (max-width: 480px)", StringComparison.Ordinal);
        Assert.True(i > 0, "belirtecler.css'te ≤480px dokunma hedefi kilidi YOK.");
        var blok = t[i..];
        Assert.Contains("--dokunma-hedefi: 2.75rem", blok);
        Assert.Contains("--kontrol-yuksekligi: 2.75rem", blok);
        Assert.Contains("--dugme-yuksekligi: 2.75rem", blok);

        // Kilit, yoğunluk/mod bloklarından SONRA gelmeli — yoksa ezilemez.
        var yogunluk = t.IndexOf("[data-yogunluk=\"sik\"]", StringComparison.Ordinal);
        var mod = t.IndexOf("html[data-mod=\"profesyonel\"]", StringComparison.Ordinal);
        Assert.True(i > yogunluk && i > mod,
            "≤480px kilidi yoğunluk/mod bloklarından ÖNCE — sıra kaybeder, 32 px geri gelir.");
    }

    [Fact]
    public void K3_Kabuk_dugmeleri_de_asgari_kutuyu_alir()
    {
        var css = AppCss();
        var i = css.IndexOf("/* ── K3 · DOKUNMA HEDEFİ", StringComparison.Ordinal);
        Assert.True(i > 0);
        var blok = css[i..(css.IndexOf("/* ── K5", i, StringComparison.Ordinal))];

        // Ölçülen beş kabuk düğmesi (Çıkış · Menüyü aç · ⟳ · Basit · Profesyonel)
        // HER ekranda tekrar ediyordu; hepsi tek blokta kapanır.
        Assert.Contains(".kabuk-menu-dugme", blok);
        Assert.Contains(".kur-guncelle-btn", blok);
        Assert.Contains(".mod-anahtar button", blok);
        Assert.Contains(".kenar-alt-butonlar button", blok);
        Assert.Contains("min-width: var(--dokunma-hedefi)", blok);
    }

    // ═══ K4 — DÜĞME ÖLÇEĞİ (15 farklı yükseklik) ════════════════════════════

    [Fact]
    public void K4_Dugme_yuksekligi_IKI_degerden_gelir()
    {
        var css = AppCss();
        var degerler = System.Text.RegularExpressions.Regex
            .Matches(css, @"^\.(dugme|dugme-kucuk|dugme-benzeri|sekme|btn|pill)[^{]*\{[^}]*min-height: *([^;]+)",
                     System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Groups[2].Value.Trim())
            .Distinct()
            .ToList();

        Assert.True(degerler.Count <= 2,
            $"Düğme ölçeği {degerler.Count} değere çıkmış: {string.Join(" · ", degerler)}");
        Assert.All(degerler, d => Assert.StartsWith("var(--", d));   // sabit px YOK
    }

    [Fact]
    public void K4_Kart_yaricapi_TEK_belirtecten_gelir()
    {
        Assert.Contains("--kart-yaricap:", Belirtecler());
        Assert.Contains(".kart, .ozet-kutu { border-radius: var(--kart-yaricap, .75rem); }", AppCss());
    }

    // ═══ K5 — TABLO TAŞMASI ═════════════════════════════════════════════════

    [Fact]
    public void K5_Her_tablo_KENDI_kaydirma_kabindadir()
    {
        var kok = Path.Combine(Kok(), "Kuyumcu.Web");
        // ⚠ M39-5 DERSİ: "tablo-sar" alt dizgesi ÖLÜ `tablo-sarmal` sınıfıyla da
        // eşleşiyordu; sarmalayıcı sınıfı TAM yazılır.
        var sarici = new[] { "class=\"tablo-sar\"", "kart-liste", "KartListe", "yazdirma-tablo", "belge-tablo" };
        var acik = new List<string>();

        foreach (var f in Directory.GetFiles(kok, "*.razor", SearchOption.AllDirectories))
        {
            var m = File.ReadAllText(f);
            foreach (System.Text.RegularExpressions.Match t in
                     System.Text.RegularExpressions.Regex.Matches(m, "<table"))
            {
                var kapsam = m[Math.Max(0, t.Index - 600)..Math.Min(m.Length, t.Index + 120)];
                if (!sarici.Any(s => kapsam.Contains(s, StringComparison.Ordinal)))
                    acik.Add($"{Path.GetFileName(f)}@{t.Index}");
            }
        }

        // `/hurda`nın 58 px'lik ana içerik kayması ölü `tablo-sarmal` sınıfından
        // doğmuştu: sınıf CSS'te TANIMSIZDI, kimse fark etmemişti.
        Assert.Empty(acik);
    }

    [Fact]
    public void K5_Olu_sarmalayici_sinifi_tablo_sarmal_KULLANILMAZ()
    {
        var kok = Path.Combine(Kok(), "Kuyumcu.Web");
        var css = AppCss();
        Assert.DoesNotContain(".tablo-sarmal", css);   // tanımsız olduğu doğrulanır

        foreach (var f in Directory.GetFiles(kok, "*.razor", SearchOption.AllDirectories))
            Assert.DoesNotContain("tablo-sarmal", File.ReadAllText(f));
    }

    // ═══ K6 — AÇILIR PANEL GÖRÜŞ ALANI ══════════════════════════════════════

    [Fact]
    public void K6_Panel_yer_yoksa_YUKARI_acilir_ve_tek_mekanizmadir()
    {
        var js = Yorumsuz(Metin("Kuyumcu.Web", "wwwroot", "js", "secici-yerlesim.js"));
        Assert.Contains("window.kuyumcuSecici", js);
        Assert.Contains("classList.add('yukari')", js);
        Assert.Contains("scrollIntoView", js);
        Assert.DoesNotContain("eval(", js);

        Assert.Contains(".secici-liste-kap.yukari", AppCss());
        Assert.Contains("js/secici-yerlesim.js", Metin("Kuyumcu.Web", "wwwroot", "index.html"));

        // Tek çağıran Secici.razor'dur — ikinci yerleşim deseni açılmaz.
        var kok = Path.Combine(Kok(), "Kuyumcu.Web");
        var cagiranlar = Directory.GetFiles(kok, "*.razor", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("kuyumcuSecici", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Single(cagiranlar);
        Assert.Equal("Secici.razor", cagiranlar[0]);
    }

    [Fact]
    public void K6_Panel_gorus_alanindan_UZUN_olamaz()
    {
        // Panel katlamayı aşarsa liste KENDİ İÇİNDE kayar; sayfa değil.
        var css = AppCss();
        Assert.Contains("max-height: min(24rem, 70vh)", css);
    }

    // ═══ K7 — MÜKERRER ÖNİZLEME (8 alan / 5 ekran) ══════════════════════════

    [Fact]
    public void K7_Girdinin_kendi_degerini_tekrar_eden_onizleme_CIZILMEZ()
    {
        // `/fiyatlandirma`: girdi "100", altındaki önizleme "100" — birebir aynı.
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-carpan")
            .Add(x => x.Etiket, "İşçilik çarpanı (%)")
            .Add(x => x.Deger, 100m));

        Assert.Empty(c.FindAll("#p39-carpan-onizleme"));
    }

    [Fact]
    public void K7_Para_onizlemesi_de_tekrar_sayilir_birim_yaninda_kalir()
    {
        // `/cariler` Kredi Limiti: "750000" ↔ "₺750.000,00" (4 genişlikte de).
        var c = Render<Para>(p => p
            .Add(x => x.ElemanId, "p39-limit")
            .Add(x => x.Etiket, "Kredi Limiti")
            .Add(x => x.Deger, 750000m)
            .Add(x => x.ParaBirimi, "TRY"));

        Assert.Empty(c.FindAll("#p39-limit-onizleme"));
        // UX-S32 KORUNUR: para birimi yine tutarla birlikte taşınır.
        // KAT-1 (K5): son ek ₺ işaretidir (kurucu 24.09: "TRY yerine ₺").
        Assert.Equal("\u20ba", c.Find("#p39-limit-birim").TextContent.Trim());
    }

    [Fact]
    public void K7_Bilgi_EKLEYEN_onizleme_KORUNUR()
    {
        // Has karşılığı girdide OLMAYAN bilgidir (UX-S21) — silinmez.
        var c = Render<Gram>(p => p
            .Add(x => x.ElemanId, "p39-gram")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Deger, 10m)
            .Add(x => x.HasKarsiligi, 9.16m));

        var onizleme = c.Find("#p39-gram-onizleme").TextContent;
        Assert.Contains("has", onizleme);
        // Gramın yeniden biçimlenmiş hâli (kutunun tekrarı) ARTIK YOK.
        Assert.DoesNotContain(" g", onizleme);
    }

    // ─── KURUCU BULGUSU 2026-09-22 ──────────────────────────────────────────
    // "adet kısmında ne yazarsam altta da o görünüyor."
    // K7 ölçütü RAKAM DİZİSİ karşılaştırmasıydı: sondaki/baştaki sıfır diziyi
    // değiştirdiği için ("1520" ⊄ "152") mükerrer önizleme SIZIYORDU.
    // 5198'de `/islemler` Adet kutusunda ölçülen gerçek çıktı:
    //     "1,0" → "1" · "15,20" → "15.2" · "2,50" → "2.5" · "007" → "7"
    // Aşağıdaki veri kümesi bu dört ölçümün BİREBİR kendisidir.
    [Theory]
    [InlineData("1,0", "1")]        // sondaki sıfır
    [InlineData("15,20", "15.2")]   // gramlı ürün adedi — kurucunun gördüğü satır
    [InlineData("2,50", "2.5")]
    [InlineData("007", "7")]        // baştaki sıfır
    [InlineData("1.0", "1")]        // nokta ile yazan kullanıcı
    public void K7_Yazim_farki_BILGI_DEGILDIR_onizleme_yine_CIZILMEZ(
        string yazilan, string beklenenSayi)
    {
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-adet")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.Deger, 0m));

        // Kullanıcı kutuya YAZAR (parametreyle beslemek bu kusuru gizliyordu:
        // `ham` o yolda hep makine biçiminde doğuyor, sıfırlar hiç oluşmuyor).
        c.Find("#p39-adet").Input(yazilan);

        // Kutuya yazılan metin ile önizlemenin basacağı metin FARKLI yazımlar,
        // AYNI sayıdır — kusurun çekirdeği tam olarak budur.
        Assert.Equal(decimal.Parse(beklenenSayi, System.Globalization.CultureInfo.InvariantCulture),
                     FormAlani.Ondalik(yazilan));
        Assert.Empty(c.FindAll("#p39-adet-onizleme"));
        Assert.DoesNotContain("p39-adet-onizleme",
            c.Find("#p39-adet").GetAttribute("aria-describedby") ?? "");
    }

    [Fact]
    public void K7_Yazarken_de_gecerli_ADET_onizlemesi_hic_dogmaz()
    {
        // Rakam rakam yazılırken HİÇBİR ara adımda önizleme parlamaz
        // (kurucunun gördüğü "altta beliren sayı" budur).
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-adet2")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.BirimKodu, "ADET")
            .Add(x => x.Deger, 0m));

        foreach (var ara in new[] { "1", "15", "15,", "15,2", "15,20" })
        {
            c.Find("#p39-adet2").Input(ara);
            Assert.True(c.FindAll("#p39-adet2-onizleme").Count == 0,
                $"'{ara}' yazılırken mükerrer önizleme çizildi.");
        }
    }

    [Fact]
    public void K7_Sayiya_cevrilemeyen_metinde_sistemin_bildigi_deger_GOSTERILIR()
    {
        // Md.1: kutuda çöp varken sistemin hâlâ tuttuğu sayı GİZLENMEZ.
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "p39-adet3")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.Deger, 12m));

        c.Find("#p39-adet3").Input("1,2,3");

        Assert.NotNull(c.Find("#p39-adet3-hata"));
        Assert.Equal("12", c.Find("#p39-adet3-onizleme").TextContent.Trim());
    }

    [Fact]
    public void K7_Onizleme_yoksa_aria_describedby_de_ONU_GOSTERMEZ()
    {
        // Ekran okuyucu kullanıcısına da aynı değer iki kez okutulmaz.
        var c = Render<Para>(p => p
            .Add(x => x.ElemanId, "p39-tutar")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.Deger, 1234.5m));

        var ac = c.Find("#p39-tutar").GetAttribute("aria-describedby") ?? "";
        Assert.DoesNotContain("p39-tutar-onizleme", ac);
    }

    // ═══ K8 — İKİ FARKLI KONTROL YÜKSEKLİĞİ ═════════════════════════════════

    [Fact]
    public void K8_Girdi_ve_secici_AYNI_belirtecten_yukseklik_alir()
    {
        var css = AppCss();
        var i = css.IndexOf(".girdi:not(.girdi-cok-satir), .secim, .secici-tetik, .secici-ara {",
                            StringComparison.Ordinal);
        Assert.True(i > 0, "Tek satırlık kontroller için ORTAK yükseklik kuralı YOK.");

        var govde = css[css.IndexOf('{', i)..css.IndexOf('}', i)];
        // Geri düşüş ZORUNLU: belirteç yoksa kutu çökmesin (canlı ölçüm dersi).
        Assert.Contains("height: var(--kontrol-yuksekligi, 2.25rem)", govde);
        Assert.Contains("box-sizing: border-box", govde);
        Assert.Contains("padding-block: 0", govde);

        // Çok satırlı kutu HARİÇTİR — textarea sabit yüksekliğe hapsedilmez.
        Assert.Contains(":not(.girdi-cok-satir)", css[i..(i + 60)]);
    }

    [Fact]
    public void K8_Kontrol_ailesinde_SABIT_piksel_yukseklik_bildirimi_YOK()
    {
        var css = AppCss();
        var sabit = System.Text.RegularExpressions.Regex
            .Matches(css, @"^\.(girdi|secim|secici-tetik|secici-ara)[^{]*\{[^}]*(min-)?height: *[0-9]",
                     System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Value[..Math.Min(70, m.Value.Length)])
            .ToList();

        Assert.Empty(sabit);
    }

    [Fact]
    public void K8_Kontrol_yuksekligi_belirteci_yogunluga_bagli_MODA_BAGLI_DEGIL()
    {
        // PAKET OLCEK-1 (2026-09-25) bu sözleşmeyi DEĞİŞTİRDİ: eskiden profesyonel
        // mod --kontrol-yuksekligi'ni 2rem'e (32 px) indiriyordu; kurucu "Emanet Al
        // butonunun boyutu değişiyor resmen" dedi. Taban 2.75rem (44 px) tek
        // tanımdır; mod satırı YOKTUR. Yoğunluk (kullanıcı tercihi) kalır.
        var t = Belirtecler();
        Assert.Contains("--kontrol-yuksekligi: 2.75rem", t);
        Assert.Contains(":root[data-yogunluk=\"sik\"]                                   { --kontrol-yuksekligi: 2rem; }", t);
        Assert.DoesNotContain("html[data-mod=\"profesyonel\"], .tema-onizleme[data-mod=\"profesyonel\"] { --kontrol-yuksekligi", t);
        Assert.DoesNotContain("html[data-mod=\"basit\"], .tema-onizleme[data-mod=\"basit\"]     { --kontrol-yuksekligi", t);

        // Renk belirteci burada TANIMLANAMAZ (yoğunluk/mod sözleşmesi).
        var i = t.IndexOf("PAKET P-39", StringComparison.Ordinal);
        var blok = t[i..];
        foreach (var renk in new[] { "--yuzey", "--metin", "--vurgu", "--tehlike", "--basari" })
            Assert.DoesNotContain(renk + ":", blok);
    }
}
