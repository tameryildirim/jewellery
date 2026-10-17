using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET DUGME-2 — DÜĞME / SEÇİCİ YAZISI DİKEY ORTADA + DÜĞME ZEMİNDEN AYIRT EDİLİR
// Kurucu 25.09 ~12:20 (Faturalar, BASİT koyu): "yazının biri butonun üstünde
// diğeri altında, butonlar zeminle aynı renkte" → tüm ekranlar.
// Bu testler DOSYA İÇERİĞİ + BELİRTEÇ KONTRASTI testidir; görsel kanıt gerçek
// tarayıcıda dugme-olc.mjs ile ölçülür (PAKET-DUGME-2-uygulama-raporu.md).
// ═══════════════════════════════════════════════════════════════════════════
public class DugmeIkiTestleri
{
    private static string DugmeCss() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "dugme.css");
    private static List<CssKaynak.Bildirim> Bildirimler() => CssKaynak.Bildirimler(DugmeCss());

    private static List<string> Linkler() =>
        Regex.Matches(CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html")),
                "<link rel=\"stylesheet\" href=\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value).ToList();

    private static IEnumerable<CssKaynak.Bildirim> Kural(string seciciParcasi, string ozellik) =>
        Bildirimler().Where(b => b.Secici.Contains(seciciParcasi, StringComparison.Ordinal)
                                 && b.Ozellik == ozellik);

    [Fact]
    public void D2_01_index_dugme_cssi_olcek_cssten_SONRA_ortak_cssten_ONCE_yukler()
    {
        var l = Linkler();
        var dugme = l.IndexOf("css/dugme.css");
        Assert.True(dugme > 0, "index.html css/dugme.css'i yüklemiyor");
        Assert.True(l.IndexOf("css/olcek.css") < dugme, "dugme.css olcek.css'ten SONRA yüklenmeli");
        Assert.Equal("css/ortak.css", l[^1]);                 // ORTAK-1 O1 korunur
        Assert.Equal(l.Count - 2, dugme);                     // ortak.css'ten hemen önce
        Assert.True(new FileInfo(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css", "dugme.css")).Length > 0);
    }

    [Fact]
    public void D2_02_dugme_css_sabit_renk_YAZMAZ_ve_moda_ozel_secici_KULLANMAZ()
    {
        var kod = CssKaynak.YorumsuzCss(DugmeCss());
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", kod);
        Assert.DoesNotMatch(@"\b(rgba?|hsla?|oklab|oklch|lab|lch|color-mix)\(", kod);
        Assert.DoesNotMatch(@"data-mod\s*=", kod);            // OLCEK-1: mod ölçü/görünüm ayırmaz
        Assert.DoesNotContain("!important", kod);
        // Satır içi SVG/veri URI'si yok (ok rengi belirteçten gelmeli).
        Assert.DoesNotContain("url(", kod);
    }

    [Fact]
    public void D2_03_dugme_ailesi_yazisi_DIKEY_ORTADA_ve_satir_yuksekligi_belirtecten()
    {
        var hiza = Kural(".dugme, .btn", "align-items").ToList();
        Assert.Contains(hiza, b => b.Deger == "center"
                                   && b.Secici.Contains(".sekmeler-sekme", StringComparison.Ordinal)
                                   && b.Secici.Contains("button.ikincil", StringComparison.Ordinal));
        Assert.Contains(Kural(".dugme, .btn", "line-height"), b => b.Deger == "var(--dugme-satir)");
    }

    [Fact]
    public void D2_04_yerlesik_select_KALMAZ_ok_belirtec_renginde()
    {
        var sec = Bildirimler().Where(b => b.Secici.Contains("select:not([multiple])", StringComparison.Ordinal)
                                           && !b.Secici.Contains(":disabled", StringComparison.Ordinal)).ToList();
        Assert.Contains(sec, b => b.Ozellik == "appearance" && b.Deger == "none");
        Assert.Contains(sec, b => b.Ozellik == "-webkit-appearance" && b.Deger == "none");
        var resim = Assert.Single(sec, b => b.Ozellik == "background-image");
        Assert.Equal(4, Regex.Matches(resim.Deger, @"var\(--secim-ok\)").Count);   // iki kol × iki durak
        Assert.Contains(sec, b => b.Ozellik == "background-repeat" && b.Deger == "no-repeat");
        Assert.Contains(sec, b => b.Ozellik == "padding-inline-end");
        // Seçici genel olmalı: tek sayfaya (#id) bağlanırsa başka ekranda native kalır.
        Assert.DoesNotContain(sec, b => b.Secici.Contains('#'));
    }

    [Fact]
    public void D2_05_basili_ve_secili_dugme_DOLU_vurgu_zemini_ile_gorunur()
    {
        var basili = Bildirimler().Where(b => b.Secici.Contains("[aria-pressed=\"true\"]", StringComparison.Ordinal)).ToList();
        // KESIF-P7 (TOPLU DUGME-2 [YÜKSEK] tuval gerilemesi): dolu seçili zemin ana eylem mavisi
        // (--sekme-secili-zemin = --vurgu) DEĞİL, tuvalin lacivert anahtar dolgusu (--secili-dolgu).
        Assert.Contains(basili, b => b.Ozellik == "background" && b.Deger == "var(--secili-dolgu)");
        Assert.Contains(basili, b => b.Ozellik == "color" && b.Deger == "var(--secili-dolgu-metin)");
        Assert.Contains(basili, b => b.Secici.Contains(".secili", StringComparison.Ordinal));
    }

    [Fact]
    public void D2_06_secili_olmayan_sekme_ve_cercevesiz_eylemler_KENAR_tasir()
    {
        // KESIF-P7 (TOPLU DUGME-2 [YÜKSEK]): kaplamalı segmentte seçili olmayan öğe KENDİ çerçevesini
        // taşımaz ("hap içinde hap"); ≥ 3:1 sınırı KAP kenarı verir. Seçili dolgu lacivert (--secili-dolgu).
        Assert.Contains(Kural(".sekmeler-serit > .sekmeler-sekme", "border"),
            b => b.Deger == "1px solid transparent" && b.Secici.Contains(".musteri-segment > .musteri-segment-oge", StringComparison.Ordinal));
        Assert.Contains(Kural(":is(.sekmeler-serit, .musteri-segment)", "border"), b => b.Deger == "1px solid var(--sekme-kenar)");
        Assert.Contains(Kural(".sekmeler-serit > .sekmeler-sekme.secili", "background"), b => b.Deger == "var(--secili-dolgu)");
        Assert.Contains(Kural(".katlanir > .katlanir-h > .katlanir-baslik", "border"), b => b.Deger == "1px solid var(--dugme-kenar)");
        // Tema temizliği (17.10): şema karosu (.gk-sema) kalktı; görünüm kartı seçenekleri aynı kenarı taşır.
        Assert.Contains(Kural(".gk-secenek:not(.secili)", "border-color"), b => b.Deger == "var(--dugme-kenar)");
        Assert.Contains(Kural("button:is(.pozisyon-ayrinti, .pozisyon-kirilim-oge, .musteri-baglanti)", "border"),
            b => b.Deger == "1px solid var(--dugme-kenar)");
        Assert.Contains(Kural(".karsilama-araclar button.dugme-ikincil", "border"), b => b.Deger == "1px solid var(--dugme-kenar)");
    }

    [Fact]
    public void D2_09_satir_ici_duzenleme_ve_gorunum_alt_dugmesi_ETKILESIMLI_kenar_tasir()
    {
        Assert.Contains(Kural(".ana-icerik .si-tetik", "border-color"), b => b.Deger == "var(--dugme-kenar)");
        Assert.Contains(Kural(".gk .gk-alt-dugme.btn-ikincil", "border-color"), b => b.Deger == "var(--dugme-kenar)");
        Assert.Contains(Kural("tr.satir-vurgu :is(.btn, .dugme)", "border-color"), b => b.Deger == "var(--dugme-kenar-vurgulu)");
    }

    [Fact]
    public void D2_10_dar_pencerede_EYLEM_satirin_altinda_kesilmez_ve_VERI_ORTULMEZ()
    {
        // Tur 3 (tasarım hakemi 29.09 16:15, YÜKSEK): tur 2'nin yapışık eylem sütunu kesilmeyi
        // çözüyor ama veriyi örtüyordu (binen.mjs: /urunler PRO 1024'te 25, 960'ta 37; /vitrin 25).
        // Kural: HİÇBİR hücreye yapışık konum yazılmaz; eylem hücreleri veri satırının altına,
        // SOLA iner (OLCEK-1 § 13e kalıbı) → düğme ekran dışına düşmez, hiçbir yazının üstüne binmez.
        Assert.DoesNotContain(Bildirimler(), b => b.Ozellik == "position" && b.Deger == "sticky");

        static int? UstSinir(string atKural)
        {
            var m = Regex.Match(atKural, @"@media \(min-width: 769px\) and \(max-width: (\d+)px\)");
            return m.Success ? int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null;
        }
        // (4a) /vitrin: 1280 ZORUNLU ölçüm genişliğinde tablo taşıyordu (1086 > 914) → aralık 1280'i
        // KAPSAR, 1440'a (tuval boyutu, tablo sığıyor) DOKUNMAZ; telefon (≤ 768) kapsam dışı.
        var vitrin = Bildirimler().Where(b => b.Secici.Contains("#vitrin-ayarlar", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(vitrin);
        Assert.All(vitrin, b => Assert.True(UstSinir(b.AtKural) is >= 1280 and < 1440, b.AtKural));
        Assert.Contains(vitrin, b => b.Secici.EndsWith("#vitrin-ayarlar", StringComparison.Ordinal) && b.Ozellik == "display" && b.Deger == "grid");
        Assert.Contains(vitrin, b => b.Ozellik == "grid-template-columns" && b.Deger == "repeat(6, auto)");   // 8 sütun − 2 eylem
        Assert.Contains(vitrin, b => b.Secici.Contains("th:nth-last-child(-n+2)", StringComparison.Ordinal) && b.Ozellik == "position" && b.Deger == "absolute");
        Assert.Contains(vitrin, b => b.Secici.Contains("td:nth-last-child(2):not([colspan])", StringComparison.Ordinal) && b.Ozellik == "grid-column" && b.Deger == "1 / 2");
        Assert.Contains(vitrin, b => b.Secici.Contains("td:last-child:not([colspan])", StringComparison.Ordinal) && b.Ozellik == "grid-column" && b.Deger == "2 / -1");
        Assert.Contains(vitrin, b => b.Secici.Contains("td:first-child:not([colspan])", StringComparison.Ordinal) && b.Ozellik == "white-space" && b.Deger == "nowrap");

        // (4b) /urunler PRO (10 başlık) 769–1099 ızgarası: STOK düğmesi (#u-stok-adet-*) eylem satırına,
        // ilk sütuna iner; DURUM veri satırındaki boşluğa döner (dense); sütun sayısı 10 − eylem − STOK = 8.
        var urun = Bildirimler().Where(b => b.Secici.Contains("#urun-liste-sar", StringComparison.Ordinal)
                                            && b.Secici.Contains(":nth-last-child(10)", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(urun);
        Assert.All(urun, b => Assert.Equal(1099, UstSinir(b.AtKural)));
        Assert.Contains(urun, b => b.Ozellik == "--eylem-alti-sutun" && b.Deger == "8");
        Assert.Contains(urun, b => b.Ozellik == "grid-auto-flow" && b.Deger == "row dense");
        Assert.Contains(urun, b => b.Secici.EndsWith("td:has(> [id^=\"u-stok-adet-\"])", StringComparison.Ordinal) && b.Ozellik == "grid-column" && b.Deger == "1 / 2");
        Assert.Contains(urun, b => b.Secici.EndsWith("td:last-child:has(> .satir-eylem)", StringComparison.Ordinal) && b.Ozellik == "grid-column" && b.Deger == "2 / -1");
        Assert.Contains(urun, b => b.Secici.EndsWith("th:nth-last-child(3)", StringComparison.Ordinal) && b.Ozellik == "position" && b.Deger == "absolute");

        // (4c) /fiyatlandirma (#fl-tablo, #km-tablo) ve /kullanicilar (.satir-eylemler.saga-yasla) 769–1099:
        // tur 2'de yapışık sütunun örttüğü tablolar; eylem hücresi alt satıra, sola iner.
        var diger = Bildirimler().Where(b => b.Secici.Contains(":is(#fl-tablo, #km-tablo, .veri-tablosu:has(> tbody > tr > td > .satir-eylemler.saga-yasla))", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(diger);
        Assert.All(diger, b => Assert.Equal(1099, UstSinir(b.AtKural)));
        Assert.Contains(diger, b => b.Ozellik == "display" && b.Deger == "grid");
        Assert.Contains(diger, b => b.Secici.EndsWith("td:last-child:not([colspan])", StringComparison.Ordinal) && b.Ozellik == "grid-column" && b.Deger == "1 / -1");
        Assert.Contains(diger, b => b.Secici.EndsWith("th:last-child", StringComparison.Ordinal) && b.Ozellik == "position" && b.Deger == "absolute");
        Assert.Contains(Bildirimler(), b => b.Secici.EndsWith(".tablo-sar > #fl-tablo", StringComparison.Ordinal) && b.Ozellik == "--d2-eylem-alti-sutun" && b.Deger == "6");
    }

    [Fact]
    public void D2_11_ekranlar_favori_yildizi_DINLENIRKEN_gorunur_ve_kenar_tasir()
    {
        // Hasım doğrulayıcı 29.09: #ekran-favori-* 38 düğme opacity:0 (app.css) → zemin=1,00 kenar=1,00.
        var yildiz = Bildirimler().Where(b => b.AtKural.Length == 0
                                              && b.Secici.EndsWith(".ekran-karo-kap > .ekran-yildiz", StringComparison.Ordinal)).ToList();
        Assert.Contains(yildiz, b => b.Ozellik == "opacity" && b.Deger == "1");
        Assert.Contains(yildiz, b => b.Ozellik == "border" && b.Deger == "1px solid var(--dugme-kenar)");
        Assert.Contains(yildiz, b => b.Ozellik == "color" && b.Deger == "var(--sekme-metin)");      // ≥ 4,5:1 (D2_07)
        // Karo yerleşimi tuvaldeki gibi kalır: karonun iç boşluğu DEĞİŞMEZ (36 px ayırmak 1440'ta 6 adı
        // iki satıra kırıyordu); yıldız masaüstünde köşeye 1 px yaklaşır, yazının harf çizgisine değmez.
        Assert.DoesNotContain(Bildirimler(), b => b.Secici.EndsWith(".ekran-karo-kap > .ekran-karo", StringComparison.Ordinal)
                                                  && b.Ozellik == "padding-right");
        Assert.Contains(Bildirimler(), b => b.Secici.EndsWith(".ekran-karo-kap > .ekran-yildiz", StringComparison.Ordinal)
                                            && b.AtKural.Contains("hover: hover", StringComparison.Ordinal)
                                            && b.Ozellik == "top" && b.Deger == "1px");
    }

    // WCAG 1.4.11: etkileşimli bileşen sınırı ≥ 3:1 — düğme kenarı hem kart yüzeyinde hem
    // sayfa gövdesinde; seçili sekme yazısı ≥ 4,5:1. 8 şema × aydınlık/karanlık (iki kurulum).
    private static readonly (string On, string Zemin, double Asgari)[] DugmeCiftleri =
    [
        ("--dugme-kenar",        "--yuzey",              3.0),
        ("--dugme-kenar",        "--yuzey-govde",        3.0),
        ("--sekme-kenar",        "--yuzey",              3.0),
        ("--sekme-secili-zemin", "--yuzey",              3.0),
        ("--sekme-secili-metin", "--sekme-secili-zemin", 4.5),
        ("--sekme-metin",        "--yuzey",              4.5),
        ("--secim-ok",           "--yuzey",              3.0),
        ("--dugme-kenar-vurgulu", "--vurgu-yuzey",        3.0),   // tur 2: vurgulu satırdaki düğme (/fiyatlandirma)
    ];

    [Fact]
    public void D2_07_dugme_sekme_belirtecleri_HER_temada_zeminden_ayrilir()
    {
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();
        var temaSayisi = 0;
        foreach (var (tema, tablo) in t.Temalar())
        {
            temaSayisi++;
            foreach (var (on, zemin, asgari) in DugmeCiftleri)
            {
                var a = t.Coz(on, tablo);
                var z = t.Coz(zemin, tablo);
                if (a is null || z is null || !Kontrast.HexMi(a) || !Kontrast.HexMi(z))
                {
                    kirik.Add($"{tema}: {on}/{zemin} çözülemedi ({a ?? "YOK"} / {z ?? "YOK"})");
                    continue;
                }
                var oran = Kontrast.Oran(a, z);
                if (oran < asgari) kirik.Add($"{tema}: {on} {a} / {zemin} {z} = {oran:0.00} < {asgari}");
            }
        }
        // Tema temizliği (17.10): 3 temel kurulum + Yıldız'ın 3 kurulumu (8 şema silindi).
        Assert.True(temaSayisi == 6, $"tema sayısı {temaSayisi} — 3 temel + 3 Yıldız bekleniyordu");
        Assert.True(kirik.Count == 0, "Kontrast altı:\n  " + string.Join("\n  ", kirik));
    }

    [Fact]
    public void D2_08_belirtecler_yalniz_TAKMA_AD_tasir_sabit_deger_yok()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        var d2 = CssKaynak.Bildirimler(css)
            .Where(b => b.Ozellik is "--dugme-kenar" or "--sekme-kenar" or "--sekme-metin" or "--sekme-secili-zemin"
                        or "--sekme-secili-metin" or "--secim-ok" or "--dugme-kenar-vurgulu")
            .ToList();
        Assert.True(d2.Count >= 12, $"DUGME-2 belirteçleri :root + önizleme bloklarında olmalı (bulunan {d2.Count})");
        Assert.All(d2, b => Assert.Matches(@"^var\(--[a-z0-9-]+\)$", b.Deger));
    }
}
