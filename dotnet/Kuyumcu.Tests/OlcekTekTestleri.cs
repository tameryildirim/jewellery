using System.Globalization;
using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-1 — TEK ÖLÇEK (2026-09-25)
// Kurucu 25.09 ~11:45: "ikisi de aynı ekran biri basit biri profesyonel, emanet
// al butonuna bi bak boyut değişiyor resmen".
// Kök neden: belirtecler.css'teki html[data-mod="profesyonel"] bloğu
// --dokunma-hedefi / --kontrol-yuksekligi / --bosluk-hucre belirteçlerini
// küçültüyordu (ölçüm 42 rota × 1440: 198 eleman farkı).
// Sözleşme: MOD ÖLÇÜ DEĞİŞTİRMEZ; düğme ailesi 3 boy (kontrol · küçük · büyük).
// Bu testler DOSYA İÇERİĞİ testidir; görsel kanıt gerçek tarayıcıda ölçülür
// (PAKET-OLCEK-1-uygulama-raporu.md § ölçüm).
// ═══════════════════════════════════════════════════════════════════════════
public class OlcekTekTestleri
{
    private static string Belirtecler() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
    private static string OlcekCss() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "olcek.css");

    // Ölçü taşıyan belirteçler — moda özel blokta HİÇBİRİ yazılamaz.
    private static readonly string[] OlcuBelirtecleri =
    {
        "--kontrol-yuksekligi", "--dokunma-hedefi", "--dugme-yuksekligi",
        "--bosluk-hucre", "--satir-yuksekligi", "--yazi-",
    };

    // Tırnaklı, tek tırnaklı ya da tırnaksız yazım: html[data-mod="profesyonel"],
    // html[data-mod='basit'], html[data-mod=profesyonel] — hepsi moda özeldir.
    private static readonly Regex ModSecici =
        new("html\\[data-mod\\s*=\\s*[\"']?(basit|profesyonel)[\"']?\\s*\\]", RegexOptions.Compiled);

    private static bool ModaOzel(string secici) =>
        secici.Contains("data-mod=\"basit\"", StringComparison.Ordinal)
        || secici.Contains("data-mod=\"profesyonel\"", StringComparison.Ordinal)
        || ModSecici.IsMatch(secici);

    // Moda özel seçicide YAZILAMAYAN doğrudan ölçü özellikleri (doğrulayıcı 25.09:
    // M3 — app.css'e `html[data-mod="profesyonel"] .sayfa-basi-eylem .btn { height: 32px }`
    // eklenince eski OL2 yalnız belirteç aradığı için yeşil kalıyordu).
    private static readonly Regex OlcuOzelligi = new(
        // v5 (doğrulayıcı 27.09): spesifikasyon "kalınlık" farkını da sayar → font-weight eklendi.
        "^(height|min-height|max-height|padding(-(top|right|bottom|left|block|inline)(-(start|end))?)?|font-size|font|font-weight|line-height|border-radius|border-(top|bottom|start|end)-(left|right|start|end)-radius)$",
        RegexOptions.Compiled);

    // GEREKÇELİ BEYAZ LİSTE — adıyla. Kenar menüsünün PRO yüzü (tuval Profesyonel.dc.html)
    // BAŞKA BİR MENÜ YÜZÜDÜR: grup önizlemesi ve satır ikonları gizlenir, ekranlar düz
    // liste olur (NE göründüğü değişir). Satır dolgusu ikonsuz 26 px girinti ile kurulur;
    // BASİT ölçüsü dayatılırsa ikonsuz satırlar ikon payı kadar boş kalır. Sahibi:
    // KATMAN / kabuk (T-K2). Kurucu kararı açık kalanlarda (rapor § 10). Yalnız bu dosya +
    // bu kapsayıcı; başka hiçbir moda özel ölçü bildirimi kabul edilmez.
    private static readonly (string Dosya, string SeciciOnek, string Gerekce)[] BeyazListe =
    {
        ("tuval-kabuk.css", "html[data-mod=\"profesyonel\"] .kenar-cubugu",
         "PRO kenar menüsü ayrı menü yüzü (tuval Profesyonel) — KATMAN/T-K2, kurucu kararı bekliyor"),
    };

    private static string Bosluksuz(string s) => Regex.Replace(s, "\\s+", " ").Trim();

    private static IEnumerable<string> Parcalar(string secici) =>
        secici.Split(',').Select(Bosluksuz).Where(s => s.Length > 0);

    // Moda özel bir ölçü bildirimini olcek.css NÖTRLER mi? Nötrleyici: aynı @kural içinde,
    // her seçici parçasında `html[data-mod="…"] ` yerine `html[data-mod] body ` yazılmış
    // (bir öğe fazla özgül, iki modu birden kapsayan) aynı özellik bildirimi.
    private static bool Notrlenir(CssKaynak.Bildirim b, List<CssKaynak.Bildirim> olcek) =>
        Parcalar(b.Secici).All(parca =>
        {
            var hedef = ModSecici.Replace(parca, "html[data-mod]").Replace("html[data-mod] ", "html[data-mod] body ");
            return olcek.Any(o => o.Ozellik == b.Ozellik
                                  && Bosluksuz(o.AtKural) == Bosluksuz(b.AtKural)
                                  && Parcalar(o.Secici).Contains(hedef));
        });

    internal static List<string> ModaOzelOlcuIhlalleri(string cssDizini)
    {
        var olcek = CssKaynak.Bildirimler(File.ReadAllText(Path.Combine(cssDizini, "olcek.css")));
        var ihlal = new List<string>();
        foreach (var dosya in Directory.GetFiles(cssDizini, "*.css").OrderBy(x => x, StringComparer.Ordinal))
        {
            var ad = Path.GetFileName(dosya);
            foreach (var b in CssKaynak.Bildirimler(File.ReadAllText(dosya)))
            {
                if (!ModaOzel(b.Secici)) continue;
                var belirtec = OlcuBelirtecleri.Any(o => b.Ozellik.StartsWith(o, StringComparison.Ordinal));
                var dogrudan = OlcuOzelligi.IsMatch(b.Ozellik);
                if (!belirtec && !dogrudan) continue;
                if (!belirtec && BeyazListe.Any(w => w.Dosya == ad
                        && Parcalar(b.Secici).All(p => p.StartsWith(w.SeciciOnek, StringComparison.Ordinal)))) continue;
                if (!belirtec && ad != "olcek.css" && Notrlenir(b, olcek)) continue;
                ihlal.Add($"{ad}: {Bosluksuz(b.AtKural)} {Bosluksuz(b.Secici)} {{ {b.Ozellik}: {b.Deger} }}".Trim());
            }
        }
        return ihlal;
    }

    [Fact]
    public void OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir()
    {
        var bloklar = CssKaynak.Bildirimler(Belirtecler()).Where(b => ModaOzel(b.Secici)).ToList();

        // Bloklar SİLİNMEDİ (tema önizlemesi ve TasarimBelirtecTestleri seçiciyi arar) …
        Assert.Contains(bloklar, b => b.Secici.Contains("html[data-mod=\"basit\"]", StringComparison.Ordinal));
        Assert.Contains(bloklar, b => b.Secici.Contains("html[data-mod=\"profesyonel\"]", StringComparison.Ordinal));

        // … ama tek bildirimleri mod işaretidir; ölçü belirteci YOKTUR.
        var ihlal = bloklar.Where(b => b.Ozellik != "--arayuz-modu")
                           .Select(b => $"{b.Secici} {{ {b.Ozellik}: {b.Deger} }}").ToList();
        Assert.True(ihlal.Count == 0, "Mod bloğu ölçü değiştiriyor (OLCEK-1):\n  " + string.Join("\n  ", ihlal));
    }

    [Fact]
    public void OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan()
    {
        // Tüm wwwroot/css/*.css: moda özel seçicide ne ölçü belirteci (--kontrol-yuksekligi …)
        // ne de doğrudan ölçü (height, min-height, max-height, padding*, font-size,
        // line-height, border-radius) yazılır. İstisna yalnız (a) adıyla gerekçeli beyaz
        // liste, (b) olcek.css'te iki modu birden kapsayan nötrleyicisi olan bildirim.
        var ihlal = ModaOzelOlcuIhlalleri(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css"));
        Assert.True(ihlal.Count == 0, "Moda özel ÖLÇÜ bildirimi (mod ölçü değiştirmez — OLCEK-1):\n  " + string.Join("\n  ", ihlal));
    }

    [Fact]
    public void OL2b_ihlal_dedektoru_kurucunun_hatasini_yakalar()
    {
        // Dedektörün kendisi sınanır (sahte dizinde): kurucunun gösterdiği kural
        // birebir yazılınca ihlal SAYILIR; beyaz listedeki menü yüzü ve olcek.css'te
        // nötrlenen üst çubuk kuralı sayılmaz; tek tırnaklı yazım da yakalanır.
        var dizin = Directory.CreateTempSubdirectory("olcek-ol2b-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dizin, "app.css"),
                "html[data-mod=\"profesyonel\"] .sayfa-basi-eylem .btn { height: 32px; min-height: 32px; }\n" +
                "html[data-mod='basit'] .x { padding: 4px; }\n" +
                "@media (min-width: 769px) { html[data-mod=\"profesyonel\"] .ust-yardim { font-size: 15px; } }\n" +
                "html[data-mod=\"profesyonel\"] .y { display: none; }\n" +
                "html[data-mod=\"profesyonel\"] .veri-tablosu td { font-weight: 600; }\n");
            File.WriteAllText(Path.Combine(dizin, "tuval-kabuk.css"),
                "html[data-mod=\"profesyonel\"] .kenar-cubugu .kenar-grup-sayi { font-size: 15px; font-weight: 600; }\n");
            File.WriteAllText(Path.Combine(dizin, "olcek.css"),
                "@media (min-width: 769px) { html[data-mod] body .ust-yardim { font-size: 16px; } }\n");

            var ihlal = ModaOzelOlcuIhlalleri(dizin);
            Assert.Equal(4, ihlal.Count);
            Assert.Contains(ihlal, x => x.Contains(".sayfa-basi-eylem .btn { height: 32px }", StringComparison.Ordinal));
            Assert.Contains(ihlal, x => x.Contains(".sayfa-basi-eylem .btn { min-height: 32px }", StringComparison.Ordinal));
            Assert.Contains(ihlal, x => x.Contains("html[data-mod='basit'] .x { padding: 4px }", StringComparison.Ordinal));
            Assert.Contains(ihlal, x => x.Contains(".veri-tablosu td { font-weight: 600 }", StringComparison.Ordinal));   // kalınlık (v5)
        }
        finally { Directory.Delete(dizin, true); }
    }

    [Fact]
    public void OL3_dugme_ailesi_UC_boy_tek_tanim()
    {
        var kok = CssKaynak.Bildirimler(Belirtecler())
            .Where(b => b.Secici == ":root" && b.AtKural.Length == 0)
            .GroupBy(b => b.Ozellik).ToDictionary(g => g.Key, g => g.Last().Deger);

        Assert.Equal("2.75rem", kok["--kontrol-yuksekligi"]);        // 44 px — kontrol
        Assert.Equal("var(--kontrol-yuksekligi)", kok["--dugme-yuksekligi"]);
        Assert.Equal("2.125rem", kok["--dugme-yuksekligi-kucuk"]);    // 34 px — küçük satır içi eylem
        Assert.Equal("3.25rem", kok["--dugme-yuksekligi-buyuk"]);     // 52 px — büyük-tam
        Assert.Equal("2.75rem", kok["--dokunma-hedefi"]);

        // Kullanıcı tercihi "rahat", tabanın ALTINDA kalamaz (eski 2.5rem < yeni 2.75rem).
        var rahat = CssKaynak.Bildirimler(Belirtecler())
            .Where(b => b.Secici == ":root[data-yogunluk=\"rahat\"]" && b.Ozellik == "--kontrol-yuksekligi")
            .Select(b => b.Deger).Single();
        Assert.True(Rem(rahat) >= Rem(kok["--kontrol-yuksekligi"]), $"rahat {rahat} < taban");
    }

    [Fact]
    public void OL4_telefon_kilidi_iki_modda_ayni_44px()
    {
        var kilit = CssKaynak.Bildirimler(Belirtecler())
            .Where(b => b.AtKural.Contains("max-width: 480px", StringComparison.Ordinal)
                        && b.Secici.Contains("html[data-mod]", StringComparison.Ordinal))
            .ToDictionary(b => b.Ozellik, b => b.Deger);
        foreach (var o in new[] { "--dokunma-hedefi", "--kontrol-yuksekligi", "--dugme-yuksekligi", "--dugme-yuksekligi-kucuk" })
            Assert.Equal("2.75rem", kilit[o]);
    }

    [Fact]
    public void OL5_index_olcek_cssi_ortak_cssten_HEMEN_ONCE_yukler()
    {
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html");
        var linkler = Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
        var i = linkler.IndexOf("css/olcek.css");
        Assert.True(i > 0, "index.html css/olcek.css'i yüklemiyor");
        Assert.Equal("css/ortak.css", linkler[^1]);           // ORTAK-1 O1 sözleşmesi korunur
        // DUGME-2 (aynı şerit, 29.09): dugme.css olcek.css ile ortak.css ARASINA girer —
        // olcek.css ortak.css'ten önceki son ÖLÇEK dosyasıdır; araya yalnız dugme.css girebilir.
        var arada = linkler.Skip(i + 1).Take(linkler.Count - 2 - i).ToList();
        Assert.True(arada.Count == 0 || arada.SequenceEqual(new[] { "css/dugme.css" }),
            "olcek.css ile ortak.css arasında yalnız css/dugme.css olabilir: " + string.Join(", ", arada));
        foreach (var serit in new[] { "css/app.css", "css/tuval-satis.css", "css/tuval-panel-musteri.css", "css/sade.css", "css/katman.css" })
            Assert.True(linkler.IndexOf(serit) < i, $"{serit} olcek.css'ten sonra yükleniyor");
    }

    [Fact]
    public void OL6_olcek_css_yalniz_olcu_yazar_sabit_renk_yok_ve_moddan_bagimsizdir()
    {
        var css = OlcekCss();
        var bildirim = CssKaynak.Bildirimler(css);
        Assert.NotEmpty(bildirim);

        var temiz = CssKaynak.YorumsuzCss(css);
        Assert.DoesNotContain(bildirim, b => Regex.IsMatch(b.Deger, "#[0-9a-fA-F]{3,8}\\b"));
        Assert.DoesNotContain("rgb(", temiz);
        Assert.DoesNotContain("hsl(", temiz);

        var renk = bildirim.Where(b => b.Ozellik is "color" or "background" or "background-color" or "border-color").ToList();
        Assert.Empty(renk);

        // Hiçbir kural belirli bir moda bağlanmaz (mod ölçü değiştirmez); yalnız
        // html[data-mod] (iki modu BİRDEN kapsayan özgüllük) ya da :root.
        Assert.DoesNotContain(bildirim, b => ModaOzel(b.Secici));
        // v17 (13f): @keyframes adımlarının seçicisi yüzdedir ("0%, 96%") — öğe seçmez, moda bağlanamaz.
        // İstisna YALNIZ olcek-* adlı keyframes içindir; renk/değer denetimi (yukarısı) onlara da uygulanır.
        Assert.All(bildirim.Where(b => !b.AtKural.StartsWith("@keyframes olcek-", StringComparison.Ordinal)), b => Assert.True(
            b.Secici.StartsWith(":root", StringComparison.Ordinal) || b.Secici.Split(',').All(s => s.Trim().StartsWith("html[data-mod] ", StringComparison.Ordinal)),
            $"olcek.css seçicisi html[data-mod]/:root ile başlamıyor: {b.Secici}"));
    }

    [Fact]
    public void OL7_iki_yuzlu_ekranlarin_kontrolleri_tek_belirtecten_olcu_alir()
    {
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string seciciParcasi, string ozellik) =>
            bildirim.Where(b => b.Secici.Contains(seciciParcasi, StringComparison.Ordinal) && b.Ozellik == ozellik)
                    .Select(b => b.Deger).LastOrDefault() ?? "(yok)";

        // /islemler — BASİT (Tezgah) ve PRO yüzü aynı kurala bağlı.
        foreach (var yuz in new[] { ".tuval-tezgah .fis-ust-satir .secici-tetik", ".tuval-pro #islem-ust-satir .secici-tetik" })
            Assert.Equal("var(--kontrol-yuksekligi)", Deger(yuz, "height"));
        foreach (var yuz in new[] { ".tuval-tezgah .barkod-satiri .girdi-arama", ".tuval-pro .fis-pro-giris .barkod-satiri .girdi-arama" })
            Assert.Equal("var(--kontrol-yuksekligi)", Deger(yuz, "height"));
        foreach (var yuz in new[] { ".tuval-tezgah .islem-alt-satir > #islem-kaydet", ".tuval-pro .islem-alt-satir > #islem-kaydet" })
            Assert.Equal("var(--dugme-yuksekligi-buyuk)", Deger(yuz, "height"));

        // /cariler — BASİT yüzü (.musteri-sade) PRO ile aynı ölçüye.
        Assert.Equal("var(--dugme-yuksekligi)", Deger(".musteri-sade .btn.musteri-yeni", "height"));
        Assert.Equal("var(--yazi-kontrol)", Deger(".musteri-sade .btn.musteri-yeni", "font-size"));
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", Deger(".musteri-sade .musteri-tablo .satir-eylem > .btn", "min-height"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", Deger(".musteri-sade .musteri-tablo .satir-eylem > .btn", "font-size"));

        // /ayarlar görünüm kartı alt düğmeleri 50 px → normal düğme boyu.
        Assert.Equal("var(--dugme-yuksekligi)", Deger(".btn.gk-alt-dugme", "min-height"));
    }

    [Fact]
    public void OL8_ana_alan_dugme_dolgusu_kenarligi_sayar_kontrol_TAM_44_kucuk_TAM_34()
    {
        // Doğrulayıcı (25.09): aynı /emanet sayfasında "+ Emanet Al" 44, "↩ İade Et" 46 px;
        // btn-kucuk 34 yerine 36 px. Neden: app.css `.ana-icerik .btn` dolgusu (H − 1.3em)/2
        // 1 px üst + 1 px alt kenarlığı saymıyordu. Hesap: dolgu×2 + 1.3em + kenar payı = H.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string secici, string ozellik) =>
            bildirim.Where(b => b.Secici == secici && b.Ozellik == ozellik).Select(b => b.Deger).LastOrDefault() ?? "(yok)";

        var kok = bildirim.Where(b => b.Secici == ":root").ToDictionary(b => b.Ozellik, b => b.Deger);
        Assert.Equal("2px", kok["--dugme-kenar-payi"]);   // .btn { border: 1px solid } × 2 (app.css)
        Assert.Contains("border: 1px solid", CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css")), StringComparison.Ordinal);

        const string kontrol = "calc((var(--dugme-yuksekligi) - 1.3em - var(--dugme-kenar-payi)) / 2)";
        const string kucuk = "calc((var(--dugme-yuksekligi-kucuk) - 1.3em - var(--dugme-kenar-payi)) / 2)";
        foreach (var o in new[] { "padding-top", "padding-bottom" })
        {
            Assert.Equal(kontrol, Deger("html[data-mod] .ana-icerik .btn:not(.btn-kucuk)", o));
            Assert.Equal(kucuk, Deger("html[data-mod] .ana-icerik .btn.btn-kucuk", o));
            Assert.Equal(kontrol, Deger("html[data-mod] .ana-icerik .btn.gk-alt-dugme", o));
        }
        Assert.Equal("1.3", Deger("html[data-mod] .ana-icerik .btn:not(.btn-kucuk)", "line-height"));
        Assert.Equal("1.3", Deger("html[data-mod] .ana-icerik .btn.btn-kucuk", "line-height"));

        // Sekme: dikey dolgu 0, boy belirteçten (TR/EN 45 px, PRO şeridi 42 px idi).
        foreach (var s in new[] { "html[data-mod] .ana-icerik .sekme", "html[data-mod] .tuval-pro .tuval-pro-sekmeler > .sekme" })
        {
            var b = bildirim.Where(x => x.Secici.Split(',').Select(p => Regex.Replace(p, "\\s+", " ").Trim()).Contains(s)).ToList();
            Assert.Contains(b, x => x.Ozellik == "min-height" && x.Deger == "var(--dugme-yuksekligi)");
            Assert.Contains(b, x => x.Ozellik == "padding-top" && x.Deger == "0");
            Assert.Contains(b, x => x.Ozellik == "padding-bottom" && x.Deger == "0");
        }

        // Panel araç çubuğundaki metinli küçük düğme ("Düzeni düzenle") 44 değil küçük boy.
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", Deger("html[data-mod] .karsilama-araclar button.dugme-kucuk", "min-height"));

        // Kenarlığı saymayan eski formül olcek.css'te hiçbir yerde kalmaz.
        Assert.DoesNotContain(bildirim, b => b.Deger.Contains("- 1.3em) / 2", StringComparison.Ordinal));
    }

    [Fact]
    public void OL9_ust_cubuk_iki_modda_BASIT_olcusunde()
    {
        // Üst çubuk <main> DIŞINDA ama aynı ekrandır (kurucu: "ikisi de aynı ekran").
        // app.css / tuval-kabuk.css PRO'da 16→15, 19→18, 14→13 px küçültüyordu; olcek.css
        // iki modu birden kapsayan seçiciyle BASİT değerine sabitler.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string parca) => bildirim.Where(b => b.AtKural.Contains("min-width: 769px", StringComparison.Ordinal)
                && !b.AtKural.Contains("max-width", StringComparison.Ordinal)   // v15: 769–1439 dar masaüstü kuralı OL28'de
                && b.Secici.Split(',').Select(s => Regex.Replace(s, "\\s+", " ").Trim()).Contains(parca) && b.Ozellik == "font-size")
            .Select(b => b.Deger).LastOrDefault() ?? "(yok)";
        // v11 (son doğrulama DÜŞÜK): değerler sabit px değil, tek ölçek BELİRTECİ.
        Assert.Equal("var(--yazi-kontrol)", Deger("html[data-mod] body .ust-yardim"));
        Assert.Equal("var(--yazi-kur-deger)", Deger("html[data-mod] body .ust-kur-serit .kur-bandi .deger"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", Deger("html[data-mod] body .ust-kur-serit .kur-bandi .etiket"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", Deger("html[data-mod] body .ust-kur-serit .son-sorgu"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", Deger("html[data-mod] body .ust-kur-serit .son-sorgu.kur-guncelle-saat"));
        // § 6'da sabit px yazı kalmaz.
        Assert.DoesNotContain(bildirim, b => b.Secici.Contains(".ust-", StringComparison.Ordinal)
            && b.Ozellik == "font-size" && Regex.IsMatch(b.Deger, "^[0-9.]+px$"));
        // Belirteçler gövde 18 px'te app.css BASİT değerini verir: 16 / 19 / 14.
        string Kok(string ad) => bildirim.Where(b => b.Secici.Trim() == ":root" && b.Ozellik == ad).Select(b => Regex.Replace(b.Deger, "\\s+", " ")).LastOrDefault() ?? "(yok)";
        Assert.Equal("calc(var(--yazi-govde) * 16 / 18)", Kok("--yazi-kontrol"));
        Assert.Equal("calc(var(--yazi-govde) * 19 / 18)", Kok("--yazi-kur-deger"));
        Assert.Equal("calc(var(--yazi-govde) * 14 / 18)", Kok("--yazi-kontrol-kucuk"));

        // BASİT değerleri app.css'teki BASİT kuralıyla aynı kalmalı (tek kaynak kayarsa kırmızı).
        var app = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"))
            .Where(b => b.AtKural.Contains("min-width: 769px", StringComparison.Ordinal) && b.Ozellik == "font-size").ToList();
        Assert.Contains(app, b => b.Secici.Trim() == ".ust-yardim" && b.Deger == "16px");
        Assert.Contains(app, b => b.Secici.Trim() == ".ust-kur-serit .kur-bandi .deger" && b.Deger == "19px");
        Assert.Contains(app, b => b.Secici.Trim() == ".ust-kur-serit .kur-bandi .etiket" && b.Deger == "14px");
    }

    // İki seçici parçası AYNI bildirimde mi (aynı değeri yapıca paylaşırlar — biri değişirse
    // diğeri de değişir; dizge değerine bağlı değil, "iki yüz tek kural" davranışını sınar).
    private static CssKaynak.Bildirim? OrtakBildirim(List<CssKaynak.Bildirim> bildirim, string a, string b, string ozellik) =>
        bildirim.LastOrDefault(x => x.Ozellik == ozellik
            && Parcalar(x.Secici).Contains(a) && Parcalar(x.Secici).Contains(b));

    [Fact]
    public void OL10_islemler_karo_ve_sekme_AYNI_olcu_kuralini_paylasir()
    {
        // Doğrulayıcı (25.09, v2): #islem-tur-SATIS … BASİT'te 70 px karo (köşe 10, dolgu 0 8,
        // kalınlık 500/600), PRO'da 44 px sekme (8, 0 16, 400/500). Kurucu: "ikisi de aynı ekran".
        // Yerleşim (ızgara ↔ şerit) mod işidir; ölçü TEK kuraldan gelir.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        const string karo = "html[data-mod] .tuval-tezgah .tuval-karo-izgara > .ekran-karo";
        const string sekme = "html[data-mod] .tuval-pro .tuval-pro-sekmeler > .sekme";
        foreach (var o in new[] { "min-height", "padding", "border-radius", "font-size", "font-weight", "line-height", "height" })
            Assert.True(OrtakBildirim(bildirim, karo, sekme, o) is not null, $"karo ve sekme '{o}' için ortak kuralda değil");
        Assert.Equal("var(--dugme-yuksekligi)", OrtakBildirim(bildirim, karo, sekme, "min-height")!.Deger);
        Assert.Equal("auto", OrtakBildirim(bildirim, karo, sekme, "height")!.Deger);   // tuval-satis 70 px sabiti ezilir
        Assert.StartsWith("var(--", OrtakBildirim(bildirim, karo, sekme, "border-radius")!.Deger, StringComparison.Ordinal);
        Assert.NotNull(OrtakBildirim(bildirim,
            "html[data-mod] .tuval-tezgah .tuval-karo-izgara > .ekran-karo.ekran-karo-birincil",
            "html[data-mod] .tuval-pro .tuval-pro-sekmeler > .sekme.secili", "font-weight"));

        // Ortak kuraldan SONRA iki yüzden yalnız birine ölçü yazan kural olmamalı (sonradan ayrışma).
        var ortakSira = bildirim.FindLastIndex(x => x.Ozellik == "min-height" && Parcalar(x.Secici).Contains(karo) && Parcalar(x.Secici).Contains(sekme));
        var olcu = new[] { "height", "min-height", "padding", "padding-left", "padding-right", "border-radius", "font-size", "font-weight" };
        var ayrisan = bildirim.Skip(ortakSira + 1)
            .Where(x => olcu.Contains(x.Ozellik) && Parcalar(x.Secici).Contains(karo) != Parcalar(x.Secici).Contains(sekme)
                        && (Parcalar(x.Secici).Contains(karo) || Parcalar(x.Secici).Contains(sekme)))
            .Select(x => $"{x.Secici} {{ {x.Ozellik} }}").ToList();
        Assert.Empty(ayrisan);
    }

    [Fact]
    public void OL11_cariler_arama_kutusu_iki_modda_TAM_kontrol_boyu()
    {
        // Doğrulayıcı (25.09, v2): #cari-arama BASİT'te 42 (çerçevenin 1 px kenarlığı
        // içeriği daraltıyordu: height calc(H − 2px)), PRO'da 44 px.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string secici, string ozellik) =>
            bildirim.Where(b => Parcalar(b.Secici).Contains(secici) && b.Ozellik == ozellik).Select(b => b.Deger).LastOrDefault() ?? "(yok)";
        const string cerceve = "html[data-mod] .musteri-sade .sayfa-basi-filtre > .musteri-ara";
        const string kutu = "html[data-mod] .musteri-sade .musteri-ara .girdi.girdi-arama";
        Assert.Equal(Deger(cerceve, "height"), Deger(kutu, "height"));          // kutu = çerçeve
        Assert.Equal("var(--kontrol-yuksekligi)", Deger(kutu, "height"));
        Assert.Equal("0", Deger(cerceve, "border-width"));                       // çizgi yer kaplamaz
        Assert.Contains("inset 0 0 0 1px var(--", Deger(cerceve, "box-shadow"), StringComparison.Ordinal);
        Assert.DoesNotContain(bildirim, b => Parcalar(b.Secici).Contains(kutu) && b.Deger.Contains("- 2px", StringComparison.Ordinal));
    }

    [Fact]
    public void OL12_kontrol_boyuna_cekilen_kucuk_dugmenin_yazisi_kontrol_yazisi()
    {
        // Doğrulayıcı (25.09, v2): 44 px düğmelerde yazı 13 / 14 / 16 px karışıktı (sayfa başı
        // şeridindeki btn-kucuk ×8 13 px; /islemler "Elle ekle" 16 px). Yükseklik ailesi = yazı ailesi.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string secici, string ozellik) =>
            bildirim.Where(b => Parcalar(b.Secici).Contains(secici) && b.Ozellik == ozellik).Select(b => b.Deger).LastOrDefault() ?? "(yok)";
        foreach (var s in new[]
                 {
                     "html[data-mod] .sayfa-basi-eylem .btn-kucuk",
                     "html[data-mod] .sayfa-basi-eylem .dugme-kucuk",
                     "html[data-mod] .sayfa-basi-filtre .btn-kucuk",
                     "html[data-mod] .sayfa-basi-filtre .dugme-kucuk",
                     "html[data-mod] .tuval-pro #islem-diger-ac.islem-diger-dugme",
                     "html[data-mod] .secici-arama > .btn-kucuk",
                     "html[data-mod] .secici-arama > .dugme-kucuk",
                     "html[data-mod] .tuval-tezgah #islem-elle > summary.btn",
                     "html[data-mod] .tuval-pro .fis-pro-giris #islem-elle > summary.btn",
                     "html[data-mod] .tuval-pro #islem-vazgec",
                 })
            Assert.True(Deger(s, "font-size") == "var(--yazi-kontrol)", $"{s} font-size = {Deger(s, "font-size")}");

        // Sekme yazısı tek boy (/ayarlar 14, /kur /yedekler 15, diğerleri 16 px idi): iki sekme ailesi AYNI kuralda.
        var sekme = OrtakBildirim(bildirim, "html[data-mod] .ana-icerik .sekme", "html[data-mod] .ana-icerik .sekmeler-sekme", "font-size");
        Assert.NotNull(sekme);
        Assert.Equal("var(--yazi-kontrol)", sekme!.Deger);   // v5: sekme kontrol boyunda → kontrol yazısı (16 px)
    }

    [Fact]
    public void OL13_kontrol_metni_UC_boy_tek_tanim_tuvalden_16_14_17()
    {
        // Doğrulayıcı (27.09, KRİTİK): kontrol metni tek tanım değildi (#cari-arama 14, #e-cari 16 px)
        // ve tuvalden küçüktü (tuval kutuları 16–18 px). Üç yazı boyu, :root'ta TEK tanım, gövde
        // ölçeğine (kullanıcı tercihi 16/18/20) bağlı: kontrol 16 · küçük 14 · büyük 17 px (gövde 18'de).
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        var kok = bildirim.Where(b => b.Secici == ":root" && b.AtKural.Length == 0).ToDictionary(b => b.Ozellik, b => b.Deger);
        Assert.Equal("calc(var(--yazi-govde) * 16 / 18)", kok["--yazi-kontrol"]);
        Assert.Equal("calc(var(--yazi-govde) * 14 / 18)", kok["--yazi-kontrol-kucuk"]);
        Assert.Equal("calc(var(--yazi-govde) * 17 / 18)", kok["--yazi-kontrol-buyuk"]);

        // Ana alandaki temel kontrol sınıfları (app.css .875rem / .8125rem) tek belirteçten yazı alır.
        string Deger(string parca) => bildirim.Where(b => b.Ozellik == "font-size" && Parcalar(b.Secici).Contains(parca))
            .Select(b => b.Deger).LastOrDefault() ?? "(yok)";
        foreach (var k in new[] { ".btn", ".dugme", ".girdi", ".secim", ".secici-tetik" })
            Assert.Equal("var(--yazi-kontrol)", Deger("html[data-mod] :where(.ana-icerik) " + k));
        foreach (var k in new[] { ".btn-kucuk", ".dugme-kucuk" })
            Assert.Equal("var(--yazi-kontrol-kucuk)", Deger("html[data-mod] :where(.ana-icerik) " + k));
        Assert.Equal("var(--yazi-kontrol-buyuk)", Deger("html[data-mod] :where(.ana-icerik) .btn-buyuk"));

        // Küçük sınıf kuralı genel kuraldan SONRA gelir (aynı özgüllük → sıra kazanır).
        var genel = bildirim.FindIndex(b => b.Ozellik == "font-size" && Parcalar(b.Secici).Contains("html[data-mod] :where(.ana-icerik) .btn"));
        var kucuk = bildirim.FindIndex(b => b.Ozellik == "font-size" && Parcalar(b.Secici).Contains("html[data-mod] :where(.ana-icerik) .btn-kucuk"));
        Assert.True(genel >= 0 && kucuk > genel, "btn-kucuk yazı kuralı genel .btn kuralından sonra değil");

        // olcek.css'te çıplak px/rem yazı boyu kalmaz — üst çubuk (tuval Panel ölçüsü, OL9) hariç.
        var ciplak = bildirim.Where(b => b.Ozellik == "font-size" && !b.Deger.StartsWith("var(--", StringComparison.Ordinal) && b.Deger != "inherit"
                                         && !b.Secici.Contains(".ust-", StringComparison.Ordinal))
                             .Select(b => $"{Bosluksuz(b.Secici)} {{ font-size: {b.Deger} }}").ToList();
        Assert.True(ciplak.Count == 0, "Belirteçsiz yazı boyu:\n  " + string.Join("\n  ", ciplak));
    }

    [Fact]
    public void OL14_tablo_satiri_ve_liste_yazisi_moddan_bagimsiz_TEK_belirtec()
    {
        // Doğrulayıcı (27.09, KRİTİK): /cariler aynı müşteri hücresi BASİT 18 px · dolgu 0 20 · 500,
        // PRO 14 px · 8 8 · 600; başlık 14→12 px; /urunler satır 51→55 px. Tablo = tek ölçek
        // (iki modda aynı): --tablo-yazi 15 · --tablo-baslik-yazi 13 · --tablo-dolgu 8 px ·
        // --tablo-satir-boyu (en az satır boyu). 16 px + 12/16 dolgu PRO tablolarını 1440'ta taşırdı.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        var kok = bildirim.Where(b => b.Secici == ":root" && b.AtKural.Length == 0).ToDictionary(b => b.Ozellik, b => b.Deger);
        Assert.Equal("calc(var(--yazi-govde) * 15 / 18)", kok["--tablo-yazi"]);          // 15 px (gövde 18)
        Assert.Equal("calc(var(--yazi-govde) * 13 / 18)", kok["--tablo-baslik-yazi"]);   // 13 px büyük harf
        Assert.Equal(".5rem .5rem", kok["--tablo-dolgu"]);
        Assert.Equal("calc(var(--kontrol-yuksekligi) + 1rem + 1px)", kok["--tablo-satir-boyu"]);   // v8: 61 px, kontrol boyuna bağlı (satır içi düzenleyici sığar; tuval 62)

        const string td = "html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td";
        const string th = "html[data-mod] .ana-icerik .veri-tablosu > thead > tr > th";
        List<CssKaynak.Bildirim> Kural(string parca) => bildirim.Where(b => Parcalar(b.Secici).Contains(parca)).ToList();
        // v6 (doğrulayıcı 28.09 ORTA, 375 px /cariler td BASİT 18 ↔ PRO 14 px): YAZI ve KALINLIK her
        // genişlikte (medyasız); DOLGU ve SATIR BOYU masaüstünde (≥ 769) — telefonda BASİT kart yığını
        // satır dolgusunu kartta taşır (yerleşim), PRO tablo hücresi app.css .tablo-sik = --tablo-dolgu.
        bool Medyasiz(CssKaynak.Bildirim b) => b.AtKural.Length == 0;
        bool Masaustu(CssKaynak.Bildirim b) => b.AtKural.Contains("min-width: 769px", StringComparison.Ordinal);
        Assert.Contains(Kural(td), b => Medyasiz(b) && b.Ozellik == "font-size" && b.Deger == "var(--tablo-yazi)");
        Assert.Contains(Kural(th), b => Medyasiz(b) && b.Ozellik == "font-size" && b.Deger == "var(--tablo-baslik-yazi)");
        Assert.Contains(Kural(th), b => Medyasiz(b) && b.Ozellik == "font-weight" && b.Deger == "600");
        // Yazı/kalınlık medya içinde İKİNCİ kez tanımlanmaz (tek tanım) ve hiçbir medya dar ekrana yazı yazmaz.
        Assert.DoesNotContain(Kural(td).Concat(Kural(th)), b => !Medyasiz(b) && (b.Ozellik == "font-size" || b.Ozellik == "font-weight"));
        Assert.Contains(Kural(td), b => Masaustu(b) && b.Ozellik == "padding" && b.Deger == "var(--tablo-dolgu)");
        Assert.Contains(Kural(td), b => Masaustu(b) && b.Ozellik == "height" && b.Deger == "var(--tablo-satir-boyu)");
        Assert.Contains(Kural(td), b => Masaustu(b) && b.Ozellik == "box-sizing" && b.Deger == "border-box");
        Assert.Contains(Kural(th), b => Masaustu(b) && b.Ozellik == "padding" && b.Deger == "var(--tablo-dolgu)");
        Assert.All(Kural(td).Concat(Kural(th)).Where(b => b.Ozellik is "padding" or "height"), b => Assert.True(Masaustu(b), b.ToString()));

        // /cariler BASİT yüzü (.musteri-sade) da aynı tablo sınıfını taşır (veri-tablosu musteri-tablo);
        // ad hücresinin kalınlığı PRO ile aynı (600).
        Assert.Contains(Kural("html[data-mod] .ana-icerik .musteri-sade .musteri-tablo > tbody > tr > td:first-child"),
            b => Medyasiz(b) && b.Ozellik == "font-weight" && b.Deger == "600");

        // Tablo kuralı şerit dosyasındaki BASİT/PRO tablo kurallarından DAHA ÖZGÜL olmalı
        // (tuval-panel-musteri `.musteri-sade .musteri-tablo > tbody > tr > td` 0,2,3 · app.css `.tablo-sik > tbody > tr > td` 0,1,3).
        Assert.True(Ozgulluk(td).CompareTo(Ozgulluk(".musteri-sade .musteri-tablo > tbody > tr > td")) > 0);
        Assert.True(Ozgulluk(td).CompareTo(Ozgulluk(".tablo-sik > tbody > tr > td")) > 0);
    }

    [Fact]
    public void OL15_iki_yuzlu_tekil_ogeler_tek_kuraldan_olcu_alir()
    {
        // Doğrulayıcı (27.09): aynı id iki modda farklı ölçü — /islemler #islem-cari-bakiye h28→24,
        // 14→13 px, köşe 999→12, 500→400; / #panel-selam 32→28 px, 700→600; hava 17→14 px.
        var bildirim = CssKaynak.Bildirimler(OlcekCss());
        string Deger(string parca, string oz) => bildirim.Where(b => b.Ozellik == oz && Parcalar(b.Secici).Contains(parca))
            .Select(b => b.Deger).LastOrDefault() ?? "(yok)";
        const string bakiye = "html[data-mod] .ana-icerik #islem-cari-bakiye";
        Assert.Equal("var(--yazi-kontrol-kucuk)", Deger(bakiye, "font-size"));
        Assert.Equal("500", Deger(bakiye, "font-weight"));
        Assert.Equal("var(--yaricap-hap)", Deger(bakiye, "border-radius"));
        Assert.Equal("0 12px", Deger(bakiye, "padding"));
        const string selam = "html[data-mod] .ana-icerik #panel-selam";
        Assert.Equal("var(--yazi-selam)", Deger(selam, "font-size"));
        Assert.Equal("var(--kalin-cok)", Deger(selam, "font-weight"));
        foreach (var h in new[] { "html[data-mod] .ana-icerik #panel-hava-sehir", "html[data-mod] .ana-icerik #panel-hava-derece" })
        {
            Assert.Equal("var(--yazi-alt)", Deger(h, "font-size"));
            Assert.Equal("400", Deger(h, "font-weight"));
        }
    }

    // ── v6 (tasarım hakemi 28.09) ─────────────────────────────────────────────────────
    // Mod yüzü SINIFLARI: BASİT/PRO iki ayrı bileşen ya da sarmalayıcı sınıfıyla çizilir
    // (.tuval-tezgah ↔ .tuval-pro, .musteri-sade, .musteri-panel-kap--sade, .panel-sade …).
    // Bu sınıflar yerleşim ve tuval ölçüsü taşır (şerit dosyaları) — olcek.css onları daha
    // özgül seçiciyle EZER. Ezilemeyen tek şey `!important`tır: yüz sınıfında !important ölçü
    // yazılırsa olcek.css'in tek ölçeği sessizce kaybeder (doğrulayıcı 28.09 DÜŞÜK: OL2 yalnız
    // html[data-mod=…] görüyordu). Canlı kanıt yine tutarlilik.mjs + olcek-senaryo.mjs'dir.
    private static readonly Regex YuzSinifi =
        new("\\.(tuval-pro|tuval-tezgah|[\\w-]*sade)(?![\\w-])", RegexOptions.Compiled);

    internal static List<string> YuzOnemliOlcuIhlalleri(string cssDizini)
    {
        var ihlal = new List<string>();
        foreach (var dosya in Directory.GetFiles(cssDizini, "*.css").OrderBy(x => x, StringComparer.Ordinal))
        {
            var ad = Path.GetFileName(dosya);
            if (ad == "olcek.css") continue;
            foreach (var b in CssKaynak.Bildirimler(File.ReadAllText(dosya)))
            {
                var onemli = b.Deger.Contains("!important", StringComparison.OrdinalIgnoreCase);
                var olcu = OlcuOzelligi.IsMatch(b.Ozellik) || OlcuBelirtecleri.Any(o => b.Ozellik.StartsWith(o, StringComparison.Ordinal));
                if (onemli && olcu && (YuzSinifi.IsMatch(b.Secici) || ModaOzel(b.Secici)))
                    ihlal.Add($"{ad}: {Bosluksuz(b.AtKural)} {Bosluksuz(b.Secici)} {{ {b.Ozellik}: {b.Deger} }}".Trim());
            }
        }
        return ihlal;
    }

    [Fact]
    public void OL16_mod_yuzu_sinifinda_important_OLCU_yok()
    {
        var ihlal = YuzOnemliOlcuIhlalleri(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css"));
        Assert.True(ihlal.Count == 0, "Mod yüzü sınıfında !important ölçü (olcek.css tek ölçeğini ezer — OLCEK-1 v6):\n  " + string.Join("\n  ", ihlal));
    }

    [Fact]
    public void OL16b_yuz_dedektoru_hakemin_ornegini_yakalar()
    {
        var dizin = Directory.CreateTempSubdirectory("olcek-ol16b-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dizin, "tuval-satis.css"),
                ".tuval-pro #islem-vazgec { font-size: 13px !important; }\n" +            // hakemin örneği
                ".musteri-panel-kap--sade #cekmece-cari .girdi { height: 50px !important; }\n" +
                ".tuval-tezgah .fis-tuval-sil { width: 28px; height: 28px; }\n" +          // !important yok → olcek.css ezer
                ".tuval-pro .x { color: red !important; }\n" +                              // ölçü değil
                ".liste .y { height: 10px !important; }\n");                                // yüz sınıfı değil
            File.WriteAllText(Path.Combine(dizin, "olcek.css"), "html[data-mod] .tuval-pro .z { height: 44px !important; }\n");
            var ihlal = YuzOnemliOlcuIhlalleri(dizin);
            Assert.Equal(2, ihlal.Count);
            Assert.Contains(ihlal, x => x.Contains(".tuval-pro #islem-vazgec { font-size: 13px !important }", StringComparison.Ordinal));
            Assert.Contains(ihlal, x => x.Contains("#cekmece-cari .girdi { height: 50px !important }", StringComparison.Ordinal));
        }
        finally { Directory.Delete(dizin, true); }
    }

    private static string SonDeger(List<CssKaynak.Bildirim> bildirim, string parca, string oz) =>
        bildirim.Where(b => b.Ozellik == oz && Parcalar(b.Secici).Contains(parca)).Select(b => b.Deger).LastOrDefault() ?? "(yok)";

    [Fact]
    public void OL17_musteri_cekmecesi_BASIT_yuzu_PRO_ile_ayni_kontrol_olcusu()
    {
        // Hakem 28.09 KRİTİK: /cariler → Düzenle: BASİT #duz-ad / kaydet / iptal 50 px · 17 px, kapat 38 px;
        // PRO 44 px · 18 px (ortak Cekmece + katman.css --katman-oge-boyu).
        var b = CssKaynak.Bildirimler(OlcekCss());
        const string k = "html[data-mod] .musteri-panel-kap--sade #cekmece-cari ";
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, k + ".girdi:not(textarea)", "height"));
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, k + ".girdi:not(textarea)", "min-height"));
        Assert.Equal("var(--katman-oge-boyu, var(--yazi-govde))", SonDeger(b, k + ".girdi", "font-size"));
        Assert.Equal("var(--yaricap-kontrol)", SonDeger(b, k + ".girdi", "border-radius"));
        Assert.Equal("var(--dugme-yuksekligi)", SonDeger(b, k + ".cekmece-alt > .dugme", "height"));
        Assert.Equal("var(--katman-oge-boyu, var(--yazi-govde))", SonDeger(b, k + ".cekmece-alt > .dugme", "font-size"));
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, k + ".form-daha-fazla", "min-height"));
        foreach (var kapat in new[] { k + ".cekmece-kapat", "html[data-mod] .cekmece .cekmece-bas .cekmece-kapat" })
        {
            Assert.Equal("var(--dokunma-hedefi)", SonDeger(b, kapat, "width"));
            Assert.Equal("var(--dokunma-hedefi)", SonDeger(b, kapat, "height"));
        }
        // Şerit kuralından (tuval-panel-musteri.css `.musteri-panel-kap--sade #cekmece-cari .girdi`, 1,2,0) daha özgül.
        Assert.True(Ozgulluk(k + ".girdi:not(textarea)").CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari .girdi")) > 0);
        Assert.True(Ozgulluk(k + ".cekmece-alt > .dugme").CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari .cekmece-alt > .dugme")) > 0);
    }

    [Fact]
    public void OL18_islemler_dolu_fis_sil_dugmeleri_ve_tutar_sutunu_tek_olcu()
    {
        // Hakem 28.09 KRİTİK: #islem-bacak-sil-0 BASİT 30 ↔ PRO 44; #islem-kalem-sil-0 BASİT 32 (375'te 28);
        // BASİT tutar sütunu 112 px'te "97.552,04" kesik (16 px yazı).
        var b = CssKaynak.Bildirimler(OlcekCss());
        const string bacakTezgah = "html[data-mod] .tuval-tezgah #islem-aldim .bacak-satiri > [id^=\"islem-bacak-sil-\"]";
        const string bacakGenel = "html[data-mod] .ana-icerik .bacak-satiri > [id^=\"islem-bacak-sil-\"]";
        foreach (var s in new[] { bacakTezgah, bacakGenel })
        {
            Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, s, "height"));
            Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, s, "width"));
        }
        Assert.True(Ozgulluk(bacakTezgah).CompareTo(Ozgulluk(".tuval-tezgah #islem-aldim .bacak-satiri > [id^=\"islem-bacak-sil-\"]")) > 0);
        Assert.True(Ozgulluk(bacakGenel).CompareTo(Ozgulluk(".tuval-pro .bacak-satiri > [id^=\"islem-bacak-sil-\"]")) > 0);
        foreach (var s in new[] { "html[data-mod] .tuval-tezgah .fis-tuval-sil", "html[data-mod] .ana-icerik [id^=\"islem-kalem-sil-\"]" })
            Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, s, "height"));   // 34, telefonda K3 kilidiyle 44
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, "html[data-mod] .tuval-tezgah .fis-tuval-sil", "width"));
        var izgara = b.Where(x => x.Ozellik == "grid-template-columns"
            && Parcalar(x.Secici).Contains("html[data-mod] .tuval-tezgah #islem-aldim .bacak-satiri")).ToList();
        Assert.Single(izgara);
        Assert.Contains("min-width: 1400px", izgara[0].AtKural, StringComparison.Ordinal);
        // v14 (tasarım hakemi t3 YÜKSEK): 8,5 kat (136 px) 7 haneli tutarı kesiyordu (görünür 121 < içerik 127).
        Assert.Contains("calc(var(--yazi-kontrol) * 9.5)", izgara[0].Deger, StringComparison.Ordinal);
        Assert.EndsWith("var(--kontrol-yuksekligi)", izgara[0].Deger, StringComparison.Ordinal);
        Assert.Contains("html[data-mod] .tuval-tezgah .fis-tuval-bacak-baslik", Parcalar(izgara[0].Secici));
    }

    [Fact]
    public void OL19_tekil_dugmeler_aileye_baglanir_katlanir_ozet_tek_odeme_suzgec()
    {
        // Hakem 28.09: dolu fişte katlanır başlık 48, PRO özet 46, "Tek ödeme" 40, /cariler "Diğer süzgeçler" 20 px.
        var b = CssKaynak.Bildirimler(OlcekCss());
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, "html[data-mod] .katlanir-baslik", "min-height"));
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, "html[data-mod] .tuval-pro .fis-ust-ozet", "min-height"));
        Assert.Equal("auto", SonDeger(b, "html[data-mod] .tuval-pro .fis-ust-ozet", "height"));
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, "html[data-mod] .tuval-pro .fis-pro-odeme-hap > .fis-pro-odeme-bag", "min-height"));
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, "html[data-mod] .ana-icerik .musteri-baglanti", "min-height"));
    }

    [Fact]
    public void OL20_telefon_ve_dar_izgara_aile_disi_boy_birakmaz()
    {
        // v7 ölçümü 28.09: 375 PRO #islem-kalem-duzenle-0 34 px (< 44); PRO #islem-pro-odeme-degistir 40 px;
        // 375 #islem-barkod dolgu BASİT 0 46 ↔ PRO 0 40; 960 /pos/tahsilatlar #pos-eslestir-uygula 68 px (sarma).
        var b = CssKaynak.Bildirimler(OlcekCss());
        foreach (var s in new[] { "html[data-mod] .tuval-pro .fis-pro-duzenle", "html[data-mod] .ana-icerik [id^=\"islem-kalem-duzenle-\"]" })
        {
            Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, s, "height"));
            Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, s, "width"));
        }
        // Şerit kuralından (tuval-satis.css `.tuval-pro .fis-pro-duzenle { height: 34px }`) daha özgül.
        Assert.True(Ozgulluk("html[data-mod] .tuval-pro .fis-pro-duzenle").CompareTo(Ozgulluk(".tuval-pro .fis-pro-duzenle")) > 0);
        foreach (var s in new[] { "html[data-mod] .tuval-pro .fis-pro-odeme-degistir", "html[data-mod] .tuval-pro #islem-diger-ac.islem-diger-dugme" })
        {
            Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, s, "min-height"));
            Assert.Equal("var(--yazi-kontrol)", SonDeger(b, s, "font-size"));
        }
        // Telefonda iki yüzün barkod dolgusu AYNI değer (BASİT yüzü de PRO'nun dar dolgusunu alır).
        var dolgu = b.Where(x => x.Ozellik == "padding-left" && x.Secici.Contains("#islem-barkod.girdi-arama", StringComparison.Ordinal)).ToList();
        Assert.Single(dolgu);
        Assert.Contains("max-width: 600px", dolgu[0].AtKural, StringComparison.Ordinal);
        Assert.Contains("html[data-mod] .tuval-tezgah .barkod-satiri #islem-barkod.girdi-arama", Parcalar(dolgu[0].Secici));
        Assert.Contains("html[data-mod] .tuval-pro .fis-pro-giris .barkod-satiri #islem-barkod.girdi-arama", Parcalar(dolgu[0].Secici));
        Assert.Equal("40px", dolgu[0].Deger);
        var izgara = b.Where(x => x.Ozellik == "grid-template-columns" && x.Secici.Contains("#pos-eslestir-uygula", StringComparison.Ordinal)).ToList();
        Assert.Single(izgara);
        Assert.Contains("max-width: 1100px", izgara[0].AtKural, StringComparison.Ordinal);
        Assert.Equal("repeat(2, minmax(0, 1fr))", izgara[0].Deger);
    }

    [Fact]
    public void OL21_v8_odeme_bacagi_kalinligi_not_kutusu_ve_ozet_acicilar_moddan_bagimsiz()
    {
        // v8 ölçümü 28.09 (olcek-senaryo.mjs 1440/375, açık+koyu): #islem-bacak-*-arac/-tutar kalınlık
        // BASİT 500/600 ↔ PRO 400; #duz-notlar 50/66 ↔ 60; #cekmece-cari-kaydet 600 ↔ 500;
        // #islem-diger-ayrinti-ac 23 px, #e-bilgi-ac 32 px (aile dışı boy); #duz-daha-fazla telefonda 50.
        var b = CssKaynak.Bildirimler(OlcekCss());
        const string arac = "html[data-mod] .ana-icerik .bacak-satiri [id^=\"islem-bacak-\"][id$=\"-arac\"]:not(#olcek-kimlik-agirligi)";
        const string tutar = "html[data-mod] .ana-icerik .bacak-satiri [id^=\"islem-bacak-\"][id$=\"-tutar\"]:not(#olcek-kimlik-agirligi)";
        Assert.Equal("500", SonDeger(b, arac, "font-weight"));
        Assert.Equal("600", SonDeger(b, tutar, "font-weight"));
        // Medyasız (her genişlik) ve şerit kuralından (kimlikli, 1,4,0) daha özgül.
        Assert.All(b.Where(x => x.Ozellik == "font-weight" && (Parcalar(x.Secici).Contains(arac) || Parcalar(x.Secici).Contains(tutar))),
            x => Assert.Equal("", x.AtKural));
        Assert.True(Ozgulluk(tutar).CompareTo(Ozgulluk(".tuval-tezgah #islem-aldim .bacak-satiri > [id$=\"-tutar-sar\"] .girdi")) > 0);

        const string not = "html[data-mod] textarea#duz-notlar.girdi:not(#olcek-kimlik-agirligi)";
        Assert.Equal("auto", SonDeger(b, not, "height"));
        Assert.Equal("1.3", SonDeger(b, not, "line-height"));
        Assert.Equal("calc(2lh + 1rem + 2px)", SonDeger(b, not, "min-height"));
        Assert.True(Ozgulluk(not).CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari textarea.girdi")) > 0);

        const string kaydet = "html[data-mod] .musteri-panel-kap--sade #cekmece-cari .cekmece-alt > #cekmece-cari-kaydet";
        Assert.Equal("500", SonDeger(b, kaydet, "font-weight"));
        Assert.True(Ozgulluk(kaydet).CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari .cekmece-alt > #cekmece-cari-kaydet")) > 0);
        Assert.Equal("var(--kontrol-yuksekligi)", SonDeger(b, "html[data-mod] .musteri-panel-kap--sade #cekmece-cari .form-daha-fazla", "height"));

        foreach (var s in new[] { "html[data-mod] .ana-icerik #islem-diger-ayrinti-ac", "html[data-mod] .ana-icerik #e-bilgi-ac" })
        {
            Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, s, "min-height"));
            Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, s, "font-size"));
        }
    }

    [Fact]
    public void OL22_v9_cekmece_fis_satiri_masak_ve_cariler_adi_iki_yuzde_tek_olcu()
    {
        // Tasarım hakemi tur 3 (28.09, sen.mjs 1440/375): #cekmece-cari başlık 21 ↔ 18 px, etiket 15/400 ↔
        // 18/500, Kaydet 282 ↔ 93 px, alt şerit zemini farklı; fiş kalem hücresi 18 px · 14/10 · 71 ↔
        // 15 px · 8 · 61; MASAK h3 21,06 ↔ 18,72; "Neden?" 15 ↔ 14 px; /cariler 960'ta 24 karakterlik ad
        // iki satır. Kurallar İKİ yüzü birden kapsar (yüz sınıfı taşımaz) ve şerit kuralından özgüldür.
        var b = CssKaynak.Bildirimler(OlcekCss());

        // (10a) Çekmece: yüz sınıfı (--sade) YOK → iki mod aynı kural.
        const string cek = "html[data-mod] .musteri-panel-kap #cekmece-cari";
        Assert.Equal("var(--yazi-cekmece-baslik)", SonDeger(b, cek + " .cekmece-bas h2", "font-size"));
        Assert.Equal("var(--yazi-etiket)", SonDeger(b, cek + " .alan-etiket", "font-size"));
        Assert.Equal("400", SonDeger(b, cek + " .alan-etiket", "font-weight"));
        Assert.Equal("18px 26px 24px", b.First(x => x.Ozellik == "padding" && x.AtKural.Length == 0 && Parcalar(x.Secici).Contains(cek + " .cekmece-alt")).Deger);
        var kok = b.Where(x => x.Secici == ":root" && x.AtKural.Length == 0).ToDictionary(x => x.Ozellik, x => x.Deger);
        Assert.Equal("calc(var(--yazi-govde) * 21 / 18)", kok["--yazi-cekmece-baslik"]);
        Assert.Equal("calc(var(--yazi-govde) * 15 / 18)", kok["--yazi-etiket"]);
        Assert.Equal("1 1 auto", SonDeger(b, cek + " .cekmece-alt > #cekmece-cari-kaydet", "flex"));
        Assert.Equal("1 1 100%", SonDeger(b, cek + " .cekmece-alt > .cekmece-kilit-sebep", "flex"));
        Assert.Equal("wrap", SonDeger(b, cek + " .cekmece-alt:has(> .cekmece-kilit-sebep)", "flex-wrap"));
        Assert.Contains(b, x => x.Ozellik == "width" && x.AtKural.Contains("min-width: 1024px", StringComparison.Ordinal)
                                && Parcalar(x.Secici).Contains("html[data-mod] .musteri-panel-kap .cekmece#cekmece-cari"));
        Assert.True(Ozgulluk(cek + " .alan-etiket").CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari .alan-etiket")) > 0);
        Assert.True(Ozgulluk(cek + " .cekmece-bas h2").CompareTo(Ozgulluk(".musteri-panel-kap--sade #cekmece-cari .cekmece-bas h2")) > 0);
        Assert.DoesNotContain(b, x => x.Secici.Contains(cek, StringComparison.Ordinal) && x.Secici.Contains("--sade", StringComparison.Ordinal));

        // (10b) Fiş kalem satırı: iki tablo, tek yazı / dolgu / satır boyu; genel tablo kuralından özgül.
        const string tz = "html[data-mod] .ana-icerik .fis-tuval-tablo > tbody > tr > td";
        const string pr = "html[data-mod] .ana-icerik .fis-pro-tablo > tbody > tr:not(.fis-pro-ayrinti) > td";
        Assert.Equal("var(--yazi-kontrol)", SonDeger(b, tz, "font-size"));
        Assert.Equal("var(--yazi-kontrol)", SonDeger(b, pr + ":not(.fis-pro-barkod)", "font-size"));
        foreach (var s in new[] { tz, pr })
        {
            Assert.Equal("var(--tablo-dolgu)", SonDeger(b, s, "padding"));
            Assert.Equal("var(--tablo-satir-boyu)", SonDeger(b, s, "height"));
            Assert.Equal("1rem", SonDeger(b, s + ":first-child", "padding-left"));
            Assert.Equal("1rem", SonDeger(b, s + ":last-child", "padding-right"));
        }
        Assert.True(Ozgulluk(pr).CompareTo(Ozgulluk("html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td")) > 0);
        Assert.True(Ozgulluk(tz).CompareTo(Ozgulluk(".tuval-tezgah .fis-tuval-tablo td")) > 0);
        Assert.Equal("calc(var(--yazi-kontrol) * 8.5)", SonDeger(b, "html[data-mod] .ana-icerik .fis-pro-tablo > thead > tr > th:nth-child(9)", "width"));
        Assert.Equal("calc(var(--dugme-yuksekligi-kucuk) + 1.5rem)", SonDeger(b, "html[data-mod] .ana-icerik .fis-pro-tablo > thead > tr > th:nth-child(10)", "width"));

        // (10c) MASAK başlığı ve "Neden?" / PRO yasal açıcısı.
        Assert.Equal("var(--yazi-lg)", SonDeger(b, "html[data-mod] .ana-icerik .kart--masak > .kart-baslik > h3", "font-size"));
        const string neden = "html[data-mod] .ana-icerik #islem-tevsik-durum #islem-tevsik-neden";
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, neden, "min-height"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, neden, "font-size"));
        Assert.True(Ozgulluk(neden).CompareTo(Ozgulluk(".tuval-tezgah #islem-aldim .fis-aldim-ayrinti > summary.metin-kucuk")) > 0);
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, "html[data-mod] .tuval-pro .fis-pro-yasal-ayrinti > summary", "min-height"));

        // (10d) /cariler ad sütunu (≥ 769 px) — genel 9em kuralından SONRA ve en az onun kadar özgül.
        const string ad = "html[data-mod] .ana-icerik .veri-tablosu.tablo-sik--kimlik > tbody > tr > td:first-child";
        var adB = b.Single(x => x.Ozellik == "min-width" && Parcalar(x.Secici).Contains(ad)
            && !x.AtKural.Contains("max-width", StringComparison.Ordinal));   // v15: 769–1399 dar masaüstü kuralı OL28'de
        Assert.Contains("min-width: 769px", adB.AtKural, StringComparison.Ordinal);
        Assert.Equal("calc(var(--tablo-yazi) * 14)", adB.Deger);
        Assert.True(b.IndexOf(adB) > b.FindIndex(x => x.Ozellik == "min-width" && x.Secici.Contains("td.hucre-vurgu:not(.sayi)", StringComparison.Ordinal)));

        // (10e) Sekme yazısı kimlikli şerit kuralını (sade.css #kur-sekme-seridi) ezer.
        const string sekme = "html[data-mod] .ana-icerik .sekmeler-sekme:not(#olcek-kimlik-agirligi)";
        Assert.Equal("var(--yazi-kontrol)", SonDeger(b, sekme, "font-size"));
        Assert.True(Ozgulluk(sekme).CompareTo(Ozgulluk("#kur-sekme-seridi .sekmeler-sekme")) > 0);

        // (10f) /bildirimler sınıfsız düğmeleri kontrol boyunda.
        foreach (var id in new[] { "#bildirim-sekme-acik", "#bildirim-sekme-kapali", "#bildirim-sekme-tumu", "#is-ayar-kaydet", "#is-ayar-varsayilan" })
            Assert.Equal("var(--dugme-yuksekligi)", SonDeger(b, "html[data-mod] .ana-icerik " + id, "min-height"));
    }

    [Fact]
    public void OL23_v10_panel_ve_cariler_basit_dugmeleri_aileye_urunler_adi_tek_satir()
    {
        // Son doğrulama 28.09 (canlı, BASİT): '/' #panel-borclu-eylem-* 38 px · 15 (375'te de 38),
        // #pozisyon-ayrinti / #pozisyon-alacak 20 px, #panel-yeni-satis 54; /cariler #cari-segment-* ve
        // #cari-suzgec-daha 34 px ama yazı 15; /urunler PRO 1280 kısa ürün adları iki satır (ad sütunu 135 px).
        var b = CssKaynak.Bildirimler(OlcekCss());
        var kok = b.Where(x => x.Secici == ":root" && x.AtKural.Length == 0).Select(x => x.Ozellik).ToHashSet();

        // (11a) Borçlu satır eylemi: küçük boy (telefonda belirteç kilidi 44) — height:auto kalmaz.
        const string borclu = "html[data-mod] .ana-icerik #panel-soru-borclu .panel-liste-satir > .dugme";
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, borclu, "height"));
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, borclu, "min-height"));
        Assert.Equal("0", SonDeger(b, borclu, "padding-block"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, borclu, "font-size"));
        Assert.True(Ozgulluk(borclu).CompareTo(Ozgulluk(".panel-sade #panel-soru-borclu .panel-liste-satir > .dugme")) > 0);

        // (11b) Pozisyon açıcısı ve kırılım öğeleri: küçük boy, küçük yazı; telefon dolgu kuralını ezer.
        foreach (var s in new[] { "html[data-mod] .ana-icerik #pozisyon-kart #pozisyon-ayrinti", "html[data-mod] .ana-icerik #pozisyon-kart .pozisyon-kirilim-oge" })
        {
            Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, s, "min-height"));
            Assert.Equal("0", SonDeger(b, s, "padding-block"));
            Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, s, "font-size"));
        }
        Assert.True(Ozgulluk("html[data-mod] .ana-icerik #pozisyon-kart .pozisyon-kirilim-oge")
            .CompareTo(Ozgulluk(".panel-sade .pozisyon-kart--sade .pozisyon-kirilim-oge")) > 0);
        Assert.True(Ozgulluk("html[data-mod] .ana-icerik #pozisyon-kart #pozisyon-ayrinti")
            .CompareTo(Ozgulluk(".pozisyon-kart--sade .pozisyon-ayrinti")) > 0);

        // (11c) Yeni satış büyük-tam boy.
        Assert.Equal("var(--dugme-yuksekligi-buyuk)", SonDeger(b, "html[data-mod] .ana-icerik #panel-yeni-satis", "height"));
        Assert.Equal("var(--yazi-kontrol-buyuk)", SonDeger(b, "html[data-mod] .ana-icerik #panel-yeni-satis", "font-size"));

        // (11d) /cariler segment ve "Diğer süzgeçler": 34 px düğmenin yazısı 14 px.
        const string seg = "html[data-mod] .ana-icerik .musteri-segment > .musteri-segment-oge";
        Assert.Equal("var(--dugme-yuksekligi-kucuk)", SonDeger(b, seg, "min-height"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, seg, "font-size"));
        Assert.Equal("var(--yazi-kontrol-kucuk)", SonDeger(b, "html[data-mod] .ana-icerik .musteri-baglanti", "font-size"));

        // (11e) /urunler ad sütunu (≥ 769 px) — genel 9em kuralından SONRA.
        const string ad = "html[data-mod] .ana-icerik #urun-liste-sar > .veri-tablosu > tbody > tr > td.hucre-vurgu:nth-child(2)";
        var adB = b.Single(x => x.Ozellik == "min-width" && Parcalar(x.Secici).Contains(ad));
                // 1440 tuval genişliğinde uygulanmaz (tablo 9em ile kabına sığıyor; 210 px eylemi kabın dışına itiyordu).
        Assert.Contains("max-width: 1399px", adB.AtKural, StringComparison.Ordinal);
Assert.Contains("min-width: 769px", adB.AtKural, StringComparison.Ordinal);
        Assert.Equal("calc(var(--tablo-yazi) * 13)", adB.Deger);   // v15: 1280'de 14 × tabloyu taşırıyordu (OL28)
        Assert.True(b.IndexOf(adB) > b.FindIndex(x => x.Ozellik == "min-width" && x.Secici.Contains("td.hucre-vurgu:not(.sayi)", StringComparison.Ordinal)));

        // Belirteçler tanımlı (sabit px yok).
        Assert.Contains("--yazi-kontrol-kucuk", kok);
        Assert.Contains("--yazi-kontrol-buyuk", kok);
    }

    [Fact]
    public void OL24_v11_eski_ikincil_dugme_kontrol_yazisi_satir_ici_deger_hucreden()
    {
        // Son doğrulama 28.09 akşam + genişletilmiş yazı-aile ölçümü (41 rota, iki mod): eski
        // `class="ikincil"` düğmeler 44 px ama 14 px yazı (/bildirimler, /vezne); /cariler PRO
        // telefon hücresinin satır içi düzenleme açıcısı 44 px · 15 px.
        var b = CssKaynak.Bildirimler(OlcekCss());

        // (12a) 44 px eski ikincil düğmenin yazısı kontrol yazısı ve app.css kuralından özgül.
        const string ikincil = "html[data-mod] .ana-icerik button.ikincil:not(.dugme):not(.btn)";
        Assert.Equal("var(--yazi-kontrol)", SonDeger(b, ikincil, "font-size"));
        var app = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(app, x => x.Secici.Trim() == "button.ikincil:not(.dugme):not(.btn)" && x.Ozellik == "min-height");
        Assert.True(Ozgulluk(ikincil).CompareTo(Ozgulluk("button.ikincil:not(.dugme):not(.btn)")) > 0);

        // (12b) Satır içi düzenleme açıcısı (.si-tetik) yazısını HÜCREDEN alır: app.css `font: inherit`
        // kalır ve olcek.css onun yazısını ezmez (16 px'e çekmek aynı numarayı BASİT 15 ↔ PRO 16 yapardı).
        Assert.Contains(app, x => x.Secici.Trim() == ".si-tetik" && x.Ozellik == "font" && x.Deger == "inherit");
        Assert.DoesNotContain(b, x => x.Secici.Contains("si-tetik", StringComparison.Ordinal)
            && (x.Ozellik == "font-size" || x.Ozellik == "font"));
    }

    [Fact]
    public void OL25_v12_ayarlar_secim_kartlari_secilince_buyumez()
    {
        // Son doğrulama 28.09 gece (DÜŞÜK ×2): #gk-mod-* seçiliyken 68 ↔ 66 px (2 px ↔ 1 px kenarlık).
        // Kenarlık her durumda 1 px, seçili kalınlık içe gölgeyle (boya katkısı yok).
        // Tema temizliği (17.10): şema karoları (#gk-sema-*) ve ızgarası kalktı; kuralları da silindi.
        var b = CssKaynak.Bildirimler(OlcekCss());
        var app = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));
        foreach (var (kart, secili, golge, appSecili) in new[]
        {
            ("html[data-mod] .gk .gk-secenek", "html[data-mod] .gk .gk-secenek.secili", "inset 0 0 0 1px var(--vurgu)", ".gk-secenek.secili"),
        })
        {
            Assert.Equal("1px", SonDeger(b, kart, "border-width"));
            Assert.Equal(golge, SonDeger(b, secili, "box-shadow"));
            // Seçili durumda kenarlık kalınlığını yeniden büyüten kural bu dosyada yok.
            Assert.DoesNotContain(b, x => Parcalar(x.Secici).Contains(secili)
                && (x.Ozellik == "border" || x.Ozellik == "border-width"));
            // app.css'in kalın kenarlık kuralı (seçili) duruyor ve bu dosyanın 1 px kuralı ondan özgül.
            Assert.Contains(app, x => x.Secici.Trim() == appSecili && x.Ozellik == "border");
            Assert.True(Ozgulluk(kart).CompareTo(Ozgulluk(appSecili)) > 0);
        }
        Assert.DoesNotContain(b, x => x.Secici.Contains(".gk-sema", StringComparison.Ordinal) && !x.Secici.Contains(".gk-sema-alt", StringComparison.Ordinal));
    }

    // CSS özgüllüğü (id, sınıf/öznitelik/sözde-sınıf, öğe) — :where() 0 sayılır.
    private static (int, int, int) Ozgulluk(string secici)
    {
        var s = Regex.Replace(secici, ":where\\([^)]*\\)", "");
        var id = Regex.Matches(s, "#[\\w-]+").Count;
        var sinif = Regex.Matches(s, "\\.[\\w-]+|\\[[^\\]]+\\]|:(?!:)[\\w-]+").Count;
        var oge = Regex.Matches(Regex.Replace(s, "\\[[^\\]]+\\]|[.#:][\\w-]+(\\([^)]*\\))?", " "), "(?<![\\w-])[a-z][\\w-]*").Count;
        return (id, sinif, oge);
    }

    private static double Rem(string deger) =>
        double.Parse(deger.Replace("rem", "", StringComparison.Ordinal).Trim(), CultureInfo.InvariantCulture);

    // ── v13 (çapraz hakem 28.09 KRİTİK) ─────────────────────────────────────────────────
    // Doğrulayıcı M3: tuval-satis.css sonuna `.tuval-pro #islem-kaydet { height: 32px; min-height: 32px;
    // font-size: 13px; }` eklendi → OL2 (yalnız html[data-mod=…]) ve OL16 (yalnız !important) yeşil kaldı.
    // Kural: mod yüzü sınıfıyla KONTROL öznesine yazılan her ölçü bildirimi onaylı envanterde
    // (Kuyumcu.Tests/OlcekYuzEnvanteri.txt) birebir bulunur; yeni ya da değişmiş satır = ihlal.
    // Aynı kural denetim.sh "OLCEK-1: YUZ_OLCU_ENVANTER_DISI".
    private static readonly Regex KontrolOznesi = new(
        "#[\\w-]+|^(button|input|select|textarea|summary)(?![\\w-])|\\.(btn|girdi)[\\w-]*|\\.(secici-tetik|sekme|ekran-karo|fis-salt-kutu)(?![\\w-])",
        RegexOptions.Compiled);

    // Seçici parçasının öznesi (son bileşik): parantez grupları (:has(…), :is(…)) ve sözde sınıflar atılır.
    internal static string Ozne(string parca)
    {
        var s = parca;
        while (true)
        {
            var t = Regex.Replace(s, "\\([^()]*\\)", "");
            if (t == s) break;
            s = t;
        }
        s = Regex.Replace(s, "::?[\\w-]+", "");
        var k = Regex.Split(s.Trim(), "[\\s>+~]+").Where(x => x.Length > 0).ToArray();
        return k.Length == 0 ? "" : k[^1];
    }

    internal static List<string> YuzKontrolOlcuSatirlari(string cssDizini)
    {
        var satir = new List<string>();
        foreach (var dosya in Directory.GetFiles(cssDizini, "*.css").OrderBy(x => x, StringComparer.Ordinal))
        {
            var ad = Path.GetFileName(dosya);
            if (ad == "olcek.css") continue;
            foreach (var b in CssKaynak.Bildirimler(File.ReadAllText(dosya)))
            {
                if (!OlcuOzelligi.IsMatch(b.Ozellik)) continue;
                foreach (var p in Parcalar(b.Secici))
                    if (YuzSinifi.IsMatch(p) && KontrolOznesi.IsMatch(Ozne(p)))
                        satir.Add($"{ad} | {Bosluksuz(b.AtKural)} | {p} | {b.Ozellik} | {Bosluksuz(b.Deger)}");
            }
        }
        return satir;
    }

    internal static HashSet<string> YuzEnvanteri(string yol) =>
        File.ReadAllLines(yol).Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);

    internal static List<string> YuzEnvanterDisi(string cssDizini, HashSet<string> envanter) =>
        YuzKontrolOlcuSatirlari(cssDizini).Where(x => !envanter.Contains(x)).Distinct().ToList();

    [Fact]
    public void OL27_v14_cariler_bakiye_rozeti_sarar_ve_odeme_bacagi_secicisi_tutara_binmez()
    {
        // Tasarım hakemi t3 (28.09 23:36) KRİTİK: PRO /cariler 1440'ta tablo 1196 > kap 1122, ⋯ sağ kenarı 1481
        // (olcek.css kapalıyken sığıyordu). YÜKSEK: BASİT ödeme bacağında 7 haneli tutar kesik; tutar sütunu
        // genişleyince SATIS-3'ün araç seçicisi en az 150 px'i satıra sığmayıp tutar kutusunun üstüne biniyordu.
        var b = CssKaynak.Bildirimler(OlcekCss());

        // (10g) Bakiye kabı (app.css .cari-bakiye inline-flex nowrap) masaüstü tablosunda sarar, sağa yaslı kalır.
        const string bakiye = "html[data-mod] .ana-icerik .veri-tablosu.tablo-sik--kimlik > tbody > tr > td .cari-bakiye";
        var sar = b.Single(x => x.Ozellik == "flex-wrap" && Parcalar(x.Secici).Contains(bakiye));
        Assert.Equal("wrap", sar.Deger);
        Assert.Contains("min-width: 769px", sar.AtKural, StringComparison.Ordinal);
        Assert.Equal("flex-end", SonDeger(b, bakiye, "justify-content"));
        Assert.True(Ozgulluk(bakiye).CompareTo(Ozgulluk(".cari-bakiye")) > 0);
        // Satır boyu korunur: iki satır (21 + aralık + 21) kontrol boyunu (44) aşmaz → aralık en çok 2 px.
        Assert.Equal("2px", SonDeger(b, bakiye, "row-gap"));

        // (7c) Araç seçicisi yalnız BASİT ≥ 1400 ızgarasında sütununa uyar ve uzun adı kelime sınırında sarar.
        const string arac = "html[data-mod] .tuval-tezgah #islem-aldim .bacak-satiri [id^=\"islem-bacak-\"][id$=\"-arac\"]:not(#olcek-kimlik-agirligi)";
        var enAz = b.Single(x => x.Ozellik == "min-width" && Parcalar(x.Secici).Contains(arac));
        Assert.Equal("0", enAz.Deger);
        Assert.Contains("min-width: 1400px", enAz.AtKural, StringComparison.Ordinal);
        Assert.True(Ozgulluk(arac).CompareTo(Ozgulluk(".islem-sayfa .fis-odeme-kart .bacak-satiri [id$=\"-arac\"]")) > 0);
        var metin = arac + " > .secici-metin";
        var ws = b.Single(x => x.Ozellik == "white-space" && Parcalar(x.Secici).Contains(metin));
        Assert.Equal("normal", ws.Deger);
        Assert.Contains("min-width: 1400px", ws.AtKural, StringComparison.Ordinal);
        Assert.Equal("clip", SonDeger(b, metin, "text-overflow"));
        Assert.Equal("1.15", SonDeger(b, metin, "line-height"));   // 2 × 16 × 1,15 = 37 < 44
        Assert.True(Ozgulluk(metin).CompareTo(Ozgulluk(".secici-metin")) > 0);
    }

    [Fact]
    public void OL28_v15_1280de_liste_tablosu_kaba_sigar_eylem_sutunu_yapisir_ust_cubuk_tek_satir()
    {
        // Son doğrulama (29.09) KRİTİK: 1280 PRO'da olcek.css tabloları kabından taşırıyordu (/cariler 1049 > 962,
        // /urunler 1143 > 962; olcek kapalıyken 962). ORTA: PRO /cariler 1280'de üst çubuk 93 px (BASİT 68).
        var b = CssKaynak.Bildirimler(OlcekCss());
        const string aralik = "(min-width: 769px) and (max-width: 1399px)";
        const string tablo = "html[data-mod] .ana-icerik .veri-tablosu:not(.fis-pro-tablo)";
        CssKaynak.Bildirim Tek(string parca, string oz) => b.Single(x => x.Ozellik == oz && Parcalar(x.Secici).Contains(parca));

        // (13a) Tablo ölçeği GENİŞLİKTEN türer (moddan değil): 769–1399'da aynı belirteçler bir basamak küçük.
        foreach (var (oz, deger) in new[] { ("--tablo-yazi", "calc(var(--yazi-govde) * 14 / 18)"),
                                            ("--tablo-baslik-yazi", "calc(var(--yazi-govde) * 12 / 18)"),
                                            ("--tablo-dolgu", ".5rem .25rem") })
        {
            var x = Tek(tablo, oz);
            Assert.Equal(deger, Regex.Replace(x.Deger, "\\s+", " "));
            Assert.Contains(aralik, x.AtKural, StringComparison.Ordinal);
            Assert.DoesNotContain("data-mod=", x.Secici, StringComparison.Ordinal);   // iki mod aynı ölçü
        }
        // Ad sütunu 13 × yazı (24 karakterlik ad tek satır) — iki tablo tek bildirimde; ≥1400'de § 10d (14 ×) kalır.
        const string cariAd = "html[data-mod] .ana-icerik .veri-tablosu.tablo-sik--kimlik > tbody > tr > td:first-child";
        const string urunAd = "html[data-mod] .ana-icerik #urun-liste-sar > .veri-tablosu > tbody > tr > td.hucre-vurgu:nth-child(2)";
        foreach (var parca in new[] { cariAd, urunAd })
        {
            var ad = b.Single(x => x.Ozellik == "min-width" && Parcalar(x.Secici).Contains(parca) && x.AtKural.Contains("max-width: 1399px", StringComparison.Ordinal));
            Assert.Equal("calc(var(--tablo-yazi) * 13)", ad.Deger);
            Assert.Contains("min-width: 769px", ad.AtKural, StringComparison.Ordinal);
        }
        Assert.Equal(".5rem", Tek("html[data-mod] .ana-icerik .veri-tablosu .satir-eylem > .btn", "padding-inline").Deger);

        // (13b) Satır eylemleri sütunu sağa YAPIŞIR (gövde + başlık); zemin belirteçten iç gölge (background yazılmaz, OL6).
        const string eylemTd = "html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td:last-child:has(> .satir-eylem)";
        const string eylemTh = "html[data-mod] .ana-icerik .veri-tablosu:has(> tbody > tr > td:last-child > .satir-eylem) > thead > tr > th:last-child";
        foreach (var sec in new[] { eylemTd, eylemTh })
        {
            var poz = Tek(sec, "position");
            Assert.Equal("sticky", poz.Deger);
            Assert.Contains(aralik, poz.AtKural, StringComparison.Ordinal);
            Assert.Equal("0", Tek(sec, "right").Deger);
            Assert.Equal("1", Tek(sec, "z-index").Deger);
            Assert.Matches("^inset 0 0 0 100vmax var\\(--yuzey(-2)?\\), -1px 0 0 0 var\\(--ayrac\\)$", Tek(sec, "box-shadow").Deger);
        }
        Assert.Equal("inset 0 0 0 100vmax var(--yuzey-2), -1px 0 0 0 var(--ayrac)",
            Tek("html[data-mod] .ana-icerik .veri-tablosu > tbody > tr:hover > td:last-child:has(> .satir-eylem)", "box-shadow").Deger);

        // (13c) Satır eylemindeki emoji süsü gizlenir; ince çizgi Ikon (<svg>) korunur.
        var emoji = Tek("html[data-mod] .ana-icerik .veri-tablosu .satir-eylem > .btn > span[aria-hidden=\"true\"]:not(:has(svg))", "display");
        Assert.Equal("none", emoji.Deger);
        Assert.Equal("", emoji.AtKural.Trim());

        // (13d) 769–1439'da kur değeri kontrol yazısı (iki modda aynı) → 1280'de üst çubuk tek satır.
        var kur = b.Single(x => x.Ozellik == "font-size" && x.AtKural.Contains("max-width: 1439px", StringComparison.Ordinal)
            && Parcalar(x.Secici).Contains("html[data-mod] body .ust-kur-serit .kur-bandi .deger"));
        Assert.Equal("var(--yazi-kontrol)", kur.Deger);
        Assert.Contains("min-width: 769px", kur.AtKural, StringComparison.Ordinal);
        // Aralıklar ve yan dolgu (iki mod birden: html[data-mod] body …) 1280'de en uzun başlığa da yer açar.
        CssKaynak.Bildirim Ust(string parca, string oz, string medya) => b.Single(x => x.Ozellik == oz
            && x.AtKural.Contains(medya, StringComparison.Ordinal) && Parcalar(x.Secici).Contains(parca));
        Assert.Equal("12px", Ust("html[data-mod] body .ust-bar", "column-gap", "max-width: 1439px").Deger);
        Assert.Equal("20px", Ust("html[data-mod] body .ust-bar", "padding-inline", "max-width: 1439px").Deger);
        Assert.Equal("10px", Ust("html[data-mod] body .ust-kur-serit .kur-bandi", "column-gap", "max-width: 1439px").Deger);
        // 769–1279: iki satırlı düzen (kur şeridi alt satırda tam genişlik) — iki modda aynı boy, öğe kaybolmaz.
        Assert.Equal("3", Ust("html[data-mod] body .ust-bar > .ust-kur-serit", "order", "max-width: 1279px").Deger);
        Assert.Equal("100%", Ust("html[data-mod] body .ust-bar > .ust-kur-serit", "flex-basis", "max-width: 1279px").Deger);
        Assert.DoesNotContain(b, x => Parcalar(x.Secici).Any(p => p.Contains(".ust-", StringComparison.Ordinal))
            && x.Ozellik is "display" or "overflow" or "overflow-x" && x.AtKural.Contains("max-width: 1", StringComparison.Ordinal));
    }

    [Fact]
    public void OL29_v17_960ta_musteri_ve_urun_listesi_eylemleri_alt_satirda_yapisma_yok_tasma_solar()
    {
        // Son doğrulama (29.09) YÜKSEK: 960 px'te (13b) yapışık eylem sütunu /cariler tutarlarını ÖRTÜYORDU
        // (K9 = 6, iki modda; PRO "-3.840 ₺ Borç" → "Bor"). v17: 769–1099'da iki liste ızgaradır, eylemler
        // kendi satırında (grid-column 1 / -1, yapışma YOK) → hiçbir yazıya binmez; veri taşarsa kap solar.
        var b = CssKaynak.Bildirimler(OlcekCss());
        const string aralik = "(min-width: 769px) and (max-width: 1099px)";
        CssKaynak.Bildirim Tek(string parca, string oz) => b.Single(x => x.Ozellik == oz
            && x.AtKural.Contains(aralik, StringComparison.Ordinal) && Parcalar(x.Secici).Contains(parca));
        foreach (var kap in new[] { "#cari-liste-sar", "#urun-liste-sar" })
        {
            var tablo = $"html[data-mod] .ana-icerik {kap} > .veri-tablosu:has(> tbody > tr > td:last-child > .satir-eylem)";
            Assert.Equal("grid", Tek(tablo, "display").Deger);
            Assert.Equal("repeat(var(--eylem-alti-sutun, 7), auto)", Tek(tablo, "grid-template-columns").Deger);
            foreach (var alt in new[] { "> thead", "> tbody", "> thead > tr", "> tbody > tr" })
                Assert.Equal("contents", Tek($"{tablo} {alt}", "display").Deger);
            // Sütun sayısı başlıktan (eylem başlığı hariç): 8 başlık → 7 sütun (PRO /cariler).
            Assert.Equal("7", Tek($"html[data-mod] .ana-icerik {kap} > .veri-tablosu:has(> thead > tr > th:first-child:nth-last-child(8))", "--eylem-alti-sutun").Deger);
            // Eylem hücresi: yeni ızgara satırı, yapışma YOK, kendi ayracı altta.
            var eylem = $"html[data-mod] .ana-icerik {kap} > .veri-tablosu > tbody > tr > td:last-child:has(> .satir-eylem)";
            Assert.Equal("1 / -1", Tek(eylem, "grid-column").Deger);
            Assert.Equal("static", Tek(eylem, "position").Deger);
            Assert.Equal("none", Tek(eylem, "box-shadow").Deger);
            Assert.Equal("1px solid var(--ayrac)", Tek(eylem, "border-bottom").Deger);
            // Eylem başlığı sütun açmaz (yalnız ekran okuyucu).
            var th = $"html[data-mod] .ana-icerik {kap} > .veri-tablosu:has(> tbody > tr > td:last-child > .satir-eylem) > thead > tr > th:last-child";
            Assert.Equal("absolute", Tek(th, "position").Deger);
            Assert.Equal("inset(50%)", Tek(th, "clip-path").Deger);
            // Taşma işareti: kap kaydırma zaman çizelgesine bağlı solar (kaymıyorsa uygulanmaz).
            var solma = b.Single(x => x.Ozellik == "animation-timeline" && Parcalar(x.Secici).Contains($"html[data-mod] .ana-icerik {kap}"));
            Assert.Equal("scroll(self inline)", solma.Deger);
            Assert.Contains("@supports (animation-timeline: scroll())", solma.AtKural, StringComparison.Ordinal);
            Assert.Contains(aralik, solma.AtKural, StringComparison.Ordinal);
        }
        var maske = b.Where(x => x.AtKural == "@keyframes olcek-tasma-solma" && x.Ozellik == "mask-image").Select(x => x.Deger).ToList();
        Assert.Contains("linear-gradient(to left, transparent 0, var(--metin) 2rem)", maske);
        Assert.Contains("none", maske);
        // Bu aralıkta iki listede sticky kalmamalı (13b'yi özgüllükle ezer; (13b) başka tablolarda sürer).
        Assert.DoesNotContain(b, x => x.Ozellik == "position" && x.Deger == "sticky" && x.AtKural.Contains(aralik, StringComparison.Ordinal));
    }

    [Fact]
    public void OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden()
    {
        var envanter = YuzEnvanteri(CssKaynak.Yol("Kuyumcu.Tests", "OlcekYuzEnvanteri.txt"));
        Assert.True(envanter.Count > 0, "Envanter boş okundu");
        var disi = YuzEnvanterDisi(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css"), envanter);
        Assert.True(disi.Count == 0,
            "Mod yüzü sınıfında onaysız KONTROL ölçüsü (aynı öğe iki modda farklı boy olabilir — OLCEK-1 v13).\n" +
            "Yol: olcek.css'te iki yüz için tek kural + tutarlilik.mjs canlı ölçümü + OlcekYuzEnvanteri.txt satırı:\n  "
            + string.Join("\n  ", disi));
    }

    [Fact]
    public void OL26b_envanter_dedektoru_hakemin_M3_ornegini_yakalar()
    {
        var dizin = Directory.CreateTempSubdirectory("olcek-ol26b-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dizin, "tuval-satis.css"),
                ".tuval-pro .islem-alt-satir > #islem-kaydet { padding: 0 22px; }\n" +                       // onaylı
                ".tuval-pro #islem-kaydet { height: 32px; min-height: 32px; font-size: 13px; }\n" +          // hakemin M3'ü: 3 ihlal
                ".tuval-tezgah #islem-ust-satir > .alan:first-child:has(> .rozet-satir) .secici-tetik { padding-right: 9px; }\n" + // :has içi özne değil
                ".musteri-sade .kart-govde { padding: 30px; }\n" +                                          // kontrol değil
                ".liste #x { height: 10px; }\n" +                                                           // yüz sınıfı değil
                ".tuval-pro #islem-vazgec { color: red; }\n");                                              // ölçü değil
            File.WriteAllText(Path.Combine(dizin, "olcek.css"), "html[data-mod] .tuval-pro #y { height: 1px; }\n");
            var env = new HashSet<string>(StringComparer.Ordinal)
            {
                "tuval-satis.css |  | .tuval-pro .islem-alt-satir > #islem-kaydet | padding | 0 22px",
            };
            var disi = YuzEnvanterDisi(dizin, env);
            Assert.Equal(4, disi.Count);
            Assert.Contains("tuval-satis.css |  | .tuval-pro #islem-kaydet | height | 32px", disi);
            Assert.Contains("tuval-satis.css |  | .tuval-pro #islem-kaydet | min-height | 32px", disi);
            Assert.Contains("tuval-satis.css |  | .tuval-pro #islem-kaydet | font-size | 13px", disi);
            Assert.Contains(disi, x => x.Contains(".rozet-satir) .secici-tetik | padding-right | 9px", StringComparison.Ordinal));
            // Onaylı satırın DEĞERİ değişirse (22 → 10 px) o da envanter dışıdır.
            File.WriteAllText(Path.Combine(dizin, "tuval-satis.css"), ".tuval-pro .islem-alt-satir > #islem-kaydet { padding: 0 10px; }\n");
            Assert.Single(YuzEnvanterDisi(dizin, env));
            Assert.Equal("secici-tetik", Ozne(".tuval-pro #a > .alan:first-child:has(> .rozet-satir) .secici-tetik").TrimStart('.'));
        }
        finally { Directory.Delete(dizin, true); }
    }
}
