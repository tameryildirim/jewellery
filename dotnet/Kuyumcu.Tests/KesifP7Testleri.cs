using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P7 — BİLEŞEN-GÖRÜNÜM (keşif 30.09, 29 bulgu + TOPLU birleştirmeleri)
// Sınıf kuralları TEK kaynakta: Secici (K10), SatirIciDuzenle (K7 satır içi),
// SayfaBasi (K7 süzgeç yuvası), Katlanir (K10 başlık) + ortak/katlanir/dugme/katman.css.
// Bu testler bileşen SÖZLEŞMESİNİ (sınıf, tam metin, title) ve CSS kuralının
// VARLIĞINI/ÖZGÜLLÜĞÜNÜ kilitler; görünürlük/sığma kanıtı gerçek tarayıcı ölçümüdür
// (dokumanlar/09-uygulama-kayitlari/PAKET-KESIF-P7-uygulama-raporu.md).
// ═══════════════════════════════════════════════════════════════════════════
public sealed class KesifP7Testleri : BunitContext
{
    public KesifP7Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private const string UzunDeger = "Merkez Şube Altın ve Döviz Ana Kasası (Euro)";

    private static string Css(string ad) => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", ad);
    private static List<CssKaynak.Bildirim> Bildirimler(string ad) => CssKaynak.Bildirimler(Css(ad));

    private static IEnumerable<CssKaynak.Bildirim> Kural(string dosya, string secici, string ozellik) =>
        Bildirimler(dosya).Where(b => Normal(b.Secici) == secici && b.Ozellik == ozellik);

    private static string Normal(string s) => Regex.Replace(s.Trim(), "\\s+", " ");

    private static string TekDeger(string dosya, string secici, string ozellik)
    {
        var l = Kural(dosya, secici, ozellik).ToList();
        Assert.True(l.Count == 1, $"{dosya}: '{secici}' {{ {ozellik} }} tam 1 kez olmalı (bulunan {l.Count})");
        return l[0].Deger;
    }

    /// <summary>Kaba CSS özgüllüğü (id, sınıf/öznitelik/sözde sınıf, öğe); :is/:has argümanı en özgül
    /// parça sayılır (:where 0). Yalnız karşılaştırma içindir.</summary>
    internal static (int Id, int Sinif, int Oge) Ozgulluk(string secici)
    {
        var s = Regex.Replace(secici, ":where\\([^()]*\\)", "");
        var id = Regex.Matches(s, "#[\\w-]+").Count;
        var sinif = Regex.Matches(s, "\\.[\\w-]+|\\[[^\\]]+\\]|:(?!:)(?!is\\(|has\\(|not\\(|where\\()[\\w-]+").Count;
        var oge = Regex.Matches(Regex.Replace(s, "\\[[^\\]]+\\]|[.#:][\\w-]+(\\([^)]*\\))?", " "), "(?<![\\w-])[a-z][\\w-]*").Count;
        return (id, sinif, oge);
    }

    private static int Karsilastir((int, int, int) a, (int, int, int) b) =>
        a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2 != b.Item2 ? a.Item2.CompareTo(b.Item2) : a.Item3.CompareTo(b.Item3);

    // ── 1) K10 · SECİCİ ─────────────────────────────────────────────────────────
    private IRenderedComponent<Secici> SeciciKur(string? deger) =>
        Render<Secici>(p => p
            .Add(c => c.ElemanId, "p7-kasa")
            .Add(c => c.Etiket, "Kasa")
            .Add(c => c.Secenekler, new List<SecenekOgesi> { new("K1", UzunDeger), new("K2", "Ana Kasa") })
            .Add(c => c.Deger, deger)
            .Add(c => c.YerTutucu, "Kasa seçin"));

    [Fact]
    public void Secici_uzun_deger_tam_metin_ve_title()
    {
        var c = SeciciKur("K1");
        var metin = c.Find("#p7-kasa .secici-metin");
        // Tam metin DOM'da (kesilmiş bir kopya değil) ve title ipucunda birebir.
        Assert.Equal(UzunDeger, metin.TextContent.Trim());
        Assert.Equal(UzunDeger, metin.GetAttribute("title"));
        // Tetik başlığı kilit sebebi sözleşmesine ayrılmış kalır (etkin seçicide title yok).
        Assert.Null(c.Find("#p7-kasa").GetAttribute("title"));
        // Yer tutucu bir DEĞER değildir → title verilmez.
        var bos = SeciciKur(null).Find("#p7-kasa .secici-metin");
        Assert.Null(bos.GetAttribute("title"));
        Assert.Contains("yer-tutucu", bos.ClassList);
    }

    [Fact]
    public void Secici_ellipsis_sinifi_yok()
    {
        var c = SeciciKur("K1");
        var metin = c.Find("#p7-kasa .secici-metin");
        Assert.Contains("secici-metin--tam", metin.ClassList);
        Assert.DoesNotContain(c.FindAll("[class]"), e => Regex.IsMatch(e.ClassName ?? "", "ellipsis|kesik|uc-nokta"));

        // Tam metin sınıfı SARAR, gizleyen taşma yok, üç nokta yok.
        const string s = ".secici-tetik > .secici-metin--tam";
        Assert.Equal("normal", TekDeger("ortak.css", s, "white-space"));
        Assert.Equal("clip", TekDeger("ortak.css", s, "text-overflow"));
        Assert.Equal("visible", TekDeger("ortak.css", s, "overflow"));
        Assert.Equal("break-word", TekDeger("ortak.css", s, "overflow-wrap"));
        // Kutu yüksekliği sabitlenmez (1–2 satır kontrol yüksekliğine sığar, uzunsa büyür).
        Assert.Equal("auto", TekDeger("ortak.css", ".secici-tetik:has(> .secici-metin--tam)", "height"));
        // Satış'a özgü seçici-metin kuralları (tuval-satis.css) EZİLMEZ: genel kural onlardan az özgül.
        var satis = Bildirimler("tuval-satis.css")
            .Where(b => b.Secici.Contains("secici-metin", StringComparison.Ordinal) && b.Ozellik == "white-space")
            .SelectMany(b => b.Secici.Split(','))
            .Select(x => x.Trim()).Where(x => x.Contains("secici-metin", StringComparison.Ordinal)).ToList();
        Assert.True(satis.Count >= 3, "tuval-satis.css Satış seçici-metin kuralları bulunamadı");
        Assert.All(satis, x => Assert.True(Karsilastir(Ozgulluk(x), Ozgulluk(s)) > 0,
            $"'{x}' genel kuraldan ({s}) daha özgül olmalı — Satış kuralı ezilir"));
    }

    // ── 2) K7 · SATIR İÇİ DÜZENLEME ─────────────────────────────────────────────
    [Fact]
    public void SatirIciDuzenle_acik_kutu_hucre_sinifi()
    {
        var c = Render<SatirIciDuzenle>(p => p
            .Add(x => x.Varlik, "cari").Add(x => x.Alan, "telefon").Add(x => x.Id, "7")
            .Add(x => x.Etiket, "Telefon").Add(x => x.Deger, "0532 000 01 06"));

        var kok = c.Find("span.satir-ici");
        Assert.DoesNotContain("si-acik", kok.ClassList);
        Assert.Empty(c.FindAll(".si-kutu"));

        c.Find("#si-cari-telefon-7-ac").Click();

        kok = c.Find("span.satir-ici");
        Assert.Contains("si-acik", kok.ClassList);
        // Girdi + Kaydet + İptal TEK parça; id'ler DEĞİŞMEDİ.
        var kutu = c.Find("span.satir-ici.si-acik > .si-kutu");
        Assert.NotNull(kutu.QuerySelector("#si-cari-telefon-7"));
        Assert.NotNull(kutu.QuerySelector("#si-cari-telefon-7-kaydet"));
        Assert.NotNull(kutu.QuerySelector("#si-cari-telefon-7-iptal"));

        // Kutu sarmaz, düğmeler girdi boyuna uzar (dar hücrede alt satıra düşmez).
        Assert.Equal("nowrap", TekDeger("ortak.css", ".satir-ici.si-acik > .si-kutu", "flex-wrap"));
        Assert.Equal("stretch", TekDeger("ortak.css", ".satir-ici.si-acik > .si-kutu", "align-items"));
        Assert.Equal("column", TekDeger("ortak.css", ".satir-ici.si-acik", "flex-direction"));

        // İptal: kök sınıf geri kalkar (kapalı hâl app.css görünümü).
        c.Find("#si-cari-telefon-7-iptal").Click();
        Assert.DoesNotContain("si-acik", c.Find("span.satir-ici").ClassList);
    }

    // ── 3) K7 · SAYFA BAŞI SÜZGEÇ YUVASI ────────────────────────────────────────
    [Fact]
    public void SayfaBasi_filtre_yuvasi_etiketli_etiketsiz_ayni_hiza_sinifi()
    {
        var etiketli = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "p7a").Add(x => x.Baslik, "A")
            .Add(x => x.Filtreler, (RenderFragment)(b => b.AddMarkupContent(0,
                "<div class=\"secici-alan\"><label class=\"alan-etiket\" for=\"t\">Tür</label><div class=\"secici-kap\"><button id=\"t\" class=\"secici-tetik\"></button></div></div>"))));
        var etiketsiz = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "p7b").Add(x => x.Baslik, "B")
            .Add(x => x.Filtreler, (RenderFragment)(b => b.AddMarkupContent(0,
                "<div class=\"arama-kutu\"><input class=\"girdi\" aria-label=\"Ara\" /></div>"))));

        var a = etiketli.Find("#sayfa-basi-p7a-filtre");
        var b2 = etiketsiz.Find("#sayfa-basi-p7b-filtre");
        Assert.Contains("filtre-yuvasi", a.ClassList);
        Assert.Contains("filtre-yuvasi", b2.ClassList);
        Assert.Equal(a.ClassName, b2.ClassName);   // içerik ne olursa olsun AYNI hiza sınıfı

        // Yuva ALTTAN hizalar; tuval yüzünün ortadan hizası (tuval-panel-musteri.css) EZİLİR.
        const string yuva = ".sayfa-basi .sayfa-basi-filtre.filtre-yuvasi";
        Assert.Equal("flex-end", TekDeger("ortak.css", yuva, "align-items"));
        Assert.Equal("flex-start", TekDeger("ortak.css", yuva + ":has(.secici-liste-kap)", "align-items"));
        // Tuval arama kabındaki (etiket payı almayan) görsel gizli etiket etiket satırına taşınır.
        Assert.Equal("calc(-1 * var(--arac-etiket-bosluk))", TekDeger("ortak.css", yuva + " > :not(.arama-kutu) .arama-kutu > label.gorsel-gizli", "top"));
        var musteri = Bildirimler("tuval-panel-musteri.css")
            .Single(x => Normal(x.Secici) == ".musteri-sade .sayfa-basi .sayfa-basi-filtre" && x.Ozellik == "align-items");
        Assert.Equal("center", musteri.Deger);
        Assert.True(Karsilastir(Ozgulluk(yuva), Ozgulluk(musteri.Secici)) >= 0, "yuva kuralı tuval yüzünden az özgül");
        var linkler = Regex.Matches(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"), "href=\"css/([^\"]+)\"")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.True(linkler.IndexOf("ortak.css") > linkler.IndexOf("tuval-panel-musteri.css"), "ortak.css tuvalden SONRA yüklenmeli");
    }

    // ── 4) K10 · KATLANIR BAŞLIĞI ───────────────────────────────────────────────
    [Fact]
    public void Katlanir_basligi_kesilmez_tam_metin_title()
    {
        const string baslik = "Ek bilgiler (banka, keşide, borçlu, not)";
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "p7k").Add(x => x.Baslik, baslik));
        var ad = c.Find("#p7k-ac .katlanir-ad");
        Assert.Equal(baslik, ad.TextContent.Trim());
        Assert.Equal(baslik, ad.GetAttribute("title"));
        var css = CssKaynak.YorumsuzCss(Css("katlanir.css"));
        Assert.DoesNotContain("ellipsis", css);
        Assert.Equal("normal", TekDeger("katlanir.css", ".katlanir-ad", "white-space"));
        Assert.Equal("visible", TekDeger("katlanir.css", ".katlanir-ad", "overflow"));
    }

    // ── 5) K7 · FORM IZGARASI (seçici + not alanı) ──────────────────────────────
    [Fact]
    public void Form_izgarasi_secici_not_alani_satir_araligini_izgaradan_devralir()
    {
        const string alan = ":is(.form-grid-2, .form-grid-3) > .alan:has(> .secici-alan):has(> :not(.secici-alan))";
        Assert.Equal("inherit", TekDeger("ortak.css", alan, "row-gap"));
        Assert.Equal("inherit", TekDeger("ortak.css", alan + " > .secici-alan", "row-gap"));
        // Sarmalayıcı .alan içindeki ikinci .alan (notlu sayı alanı) da aynı aralığı alır.
        Assert.Equal("inherit", TekDeger("ortak.css", ":is(.form-grid-2, .form-grid-3) > .alan > .alan", "row-gap"));
        // Süzgeç şeridinde seçici en uzun sözcüğünün altına inmez (kelime ortasından bölünmez).
        Assert.Equal("auto", TekDeger("ortak.css", ".sayfa-basi .sayfa-basi-filtre > .secici-alan", "min-width"));
        var sar = Bildirimler("ortak.css").Single(x => Normal(x.Secici) == ".sayfa-basi .sayfa-basi-filtre.filtre-yuvasi" && x.Ozellik == "flex-wrap");
        Assert.Equal("wrap", sar.Deger);
        Assert.Contains("max-width: 1279px", sar.AtKural, StringComparison.Ordinal);
        // app.css'teki esnek sütun kuralını (gap: 4 px) özgüllükle geçer.
        Assert.True(Karsilastir(Ozgulluk(alan), Ozgulluk(".alan:has(> .secici-alan):has(> :not(.secici-alan))")) > 0);
    }

    // ── 6) K10/K7 · dar ekranda seçicili alan kendi satırında (#27/#69) ─────────
    [Fact]
    public void Dar_ekranda_secicili_alan_kendi_satirinda_odeme_bacagi_disinda()
    {
        var b = Bildirimler("ortak.css").Where(x => x.AtKural.Contains("max-width: 480px", StringComparison.Ordinal)).ToList();
        Assert.Contains(b, x => Normal(x.Secici) == ".satir:has(> .alan > .secici-alan)" && x.Ozellik == "flex-wrap" && x.Deger == "wrap");
        Assert.Contains(b, x => Normal(x.Secici) == ".satir:has(> .alan > .secici-alan) > .alan:has(> .secici-alan)" && x.Ozellik == "flex" && x.Deger == "1 1 100%");
        // Satış ödeme bacağı (.bacak-satiri) .satir DEĞİLDİR → kurucunun Havale · Kasa hizası etkilenmez.
        var bacak = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "Fis", "FisOdemeBacaklari.razor");
        Assert.DoesNotMatch("class=\"[^\"]*\\bsatir\\b[^\"]*bacak-satiri|class=\"[^\"]*bacak-satiri[^\"]*\\bsatir\\b", bacak);
    }

    // ── 6b) Sonra turu ölçümüyle eklenen kurallar (uzun değer komşusuz, çekmecede iki satır sığar) ──
    [Fact]
    public void Uzun_deger_komsusuz_satirda_cekmecede_iki_satir_kontrol_boyuna_sigar()
    {
        Assert.Equal("1.2", TekDeger("ortak.css", "html[data-mod] .cekmece .secici-tetik > .secici-metin--tam", "line-height"));
        // Çekmece kuralı olcek.css'in genel satır aralığıyla (html[data-mod] .ana-icerik .secici-tetik > .secici-metin)
        // AYNI özgüllüktedir; ortak.css index.html'de EN SONDA yüklendiği için sıra kazanır.
        Assert.True(Karsilastir(Ozgulluk("html[data-mod] .cekmece .secici-tetik > .secici-metin--tam"),
                                Ozgulluk("html[data-mod] .ana-icerik .secici-tetik > .secici-metin")) >= 0);
        var linkler = Regex.Matches(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"), "href=\"css/([^\"]+)\"")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal("ortak.css", linkler[^1]);
        Assert.True(linkler.IndexOf("olcek.css") < linkler.IndexOf("ortak.css"));
        Assert.Equal("1 / -1", TekDeger("ortak.css", ".form-izgara > :is(.alan, .secici-alan):has(#degisim-kaynak)", "grid-column"));
        var kr = Bildirimler("ortak.css").Single(x => Normal(x.Secici) == "#kr-suzgec > .alan:has(.secici-tetik)" && x.Ozellik == "grid-column");
        Assert.Equal("1 / -1", kr.Deger);
        Assert.Contains("max-width: 1279px", kr.AtKural, StringComparison.Ordinal);
        var sip = Bildirimler("ortak.css").Where(x => Normal(x.Secici) == "#sip-f-kalemler").ToList();
        Assert.Contains(sip, x => x.Ozellik == "overflow-x" && x.Deger == "auto" && x.AtKural.Contains("max-width: 640px", StringComparison.Ordinal));
    }

    // ── 7) K3 · KISA DEĞER HÜCRESİ ──────────────────────────────────────────────
    [Fact]
    public void Kisa_deger_hucresi_bolunmez_tablo_kendi_kabinda_kayar()
    {
        Assert.Equal("nowrap", TekDeger("ortak.css", ".veri-tablosu > tbody > tr > td > code, .belge-tablo td > code", "white-space"));
        Assert.Equal("nowrap", TekDeger("ortak.css", ".veri-tablosu > tbody > tr > td :is(.rozet, .cari-rozet)", "white-space"));
        // Ad ile arkasındaki rozet arası ≥ 4 px (bosluk-2 = 8 px).
        Assert.Equal("var(--bosluk-2)", TekDeger("ortak.css", ".veri-tablosu > tbody > tr > td.hucre-vurgu > :is(.rozet, .cari-rozet, span:has(> .rozet))", "margin-inline-start"));
        foreach (var anahtar in new[] { "gider-pusulalari", "yetkiler", "uyum" })
            Assert.Contains(Bildirimler("ortak.css"), x => x.Secici.Contains($"#sayfa-basi-{anahtar}", StringComparison.Ordinal)
                                                           && x.Ozellik == "white-space" && x.Deger == "nowrap");
        Assert.Equal("nowrap", TekDeger("ortak.css", "#gpb-duzenle", "white-space"));
        // Yazdırma önizlemesi: YALNIZ ekranda; baskı düzeni değişmez.
        var belge = Bildirimler("ortak.css").Where(x => Normal(x.Secici) == ".belge-tablo").ToList();
        Assert.Contains(belge, x => x.Ozellik == "overflow-x" && x.Deger == "auto");
        Assert.All(belge, x => Assert.Contains("screen", x.AtKural, StringComparison.Ordinal));
    }

    // Tur 2 (doğrulayıcı [DÜŞÜK], ölçüldü /cariler PRO 1280 telefon açık: tablo kabı 1021 > 962, K9=3; 8rem'de telefonun ilk hanesi kesildi, 9,5rem'de kap 981 > 962 K9=4 → 8,25rem):
    // tablo hücresindeki açık kutunun girdisi kendi boyuyla (size=20) sütunu genişletmez.
    [Fact]
    public void Tablo_hucresinde_acik_satir_ici_girdi_sutunu_genisletmez()
    {
        // 8,25rem + 0,5rem iç boşluk: "0532 123 45 67" tam görünür (girdi scrollWidth = clientWidth 130), tablo kabı 962 = 962.
        Assert.Equal("8.25rem", TekDeger("ortak.css", "td .satir-ici.si-acik > .si-kutu > .si-girdi", "inline-size"));
        Assert.Equal("var(--bosluk-2)", TekDeger("ortak.css", "td .satir-ici.si-acik > .si-kutu > .si-girdi", "padding-inline"));
        Assert.Equal("1 1 8rem", TekDeger("ortak.css", ".satir-ici.si-acik > .si-kutu > .si-girdi", "flex"));
    }

    // ── 8) K17 · SAYFA BAŞI AÇIKLAMA SATIRI ─────────────────────────────────────
    [Fact]
    public void Sayfa_basi_aciklama_satiri_cizilmez_metin_DOMda_kalir()
    {
        // Tur 2: display:none DEĞİL — görsel gizleme (ekran okuyucu okur), yer kaplamaz.
        const string ac = ".sayfa-basi > .sayfa-basi-metin > .sayfa-basi-aciklama";
        Assert.Empty(Kural("ortak.css", ac, "display"));
        Assert.Equal("absolute", TekDeger("ortak.css", ac, "position"));
        Assert.Equal("rect(0 0 0 0)", TekDeger("ortak.css", ac, "clip"));
        Assert.Equal("clip", TekDeger("ortak.css", ac, "overflow"));   // taşan metin kaydırma alanına katılmaz (O7: "hidden" yasak)
        Assert.Equal("1px", TekDeger("ortak.css", ac, "height"));
        var e = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "p7c").Add(x => x.Baslik, "C").Add(x => x.Aciklama, "Açıklama metni."));
        Assert.Equal("Açıklama metni.", e.Find("#sayfa-basi-p7c-aciklama").TextContent);   // metin SİLİNMEDİ (SADE-1)
    }

    // ── 9) DUGME-2 tuval gerilemesi: segment lacivert, hap içinde hap yok ───────
    [Fact]
    public void Segment_secili_lacivert_dolgu_secili_olmayan_cercevesiz_kap_kenarli()
    {
        const string oge = "html[data-mod] .ana-icerik .sekmeler-serit > .sekmeler-sekme, html[data-mod] .ana-icerik .musteri-segment > .musteri-segment-oge";
        Assert.Equal("1px solid transparent", TekDeger("dugme.css", oge, "border"));
        Assert.Equal("1px solid var(--sekme-kenar)", TekDeger("dugme.css", "html[data-mod] .ana-icerik :is(.sekmeler-serit, .musteri-segment)", "border"));
        const string secili = "html[data-mod] .ana-icerik .sekmeler-serit > .sekmeler-sekme.secili, html[data-mod] .ana-icerik .musteri-segment > .musteri-segment-oge--secili";
        Assert.Equal("var(--secili-dolgu)", TekDeger("dugme.css", secili, "background"));
        Assert.Equal("var(--secili-dolgu-metin)", TekDeger("dugme.css", secili, "color"));
        // Aydınlıkta tuvalin lacivert anahtar belirteci — ana eylem mavisi (--vurgu) DEĞİL.
        Assert.Equal("var(--kenar-yuzey)", TekDeger("dugme.css", "html[data-mod]", "--secili-dolgu"));
        // Seçili DOLGU hiçbir temada ana eylem mavisi (--vurgu) değildir (karanlıkta --vurgu yalnız SINIR çizgisi).
        Assert.DoesNotContain(Bildirimler("dugme.css"), x => x.Ozellik is "--secili-dolgu" or "--secili-dolgu-metin" && x.Deger == "var(--vurgu)");
    }

    // Seçili dolgu okunur: metin/dolgu ≥ 4,5:1 ve KAP kenarının (--sekme-kenar) kart yüzeyiyle farkı ≥ 3:1 —
    // her tema (taban + Yıldız). Eşleme dugme.css'in KENDİSİNDEN okunur (html[data-mod]; tema temizliği
    // 17.10 ile Fildişi'ye özgü eşleme şemayla birlikte kalktı). KESIF-P7 tur 2 (tasarım hakemi): karanlık için AYRI eşleme YOKTUR — üst çubuk
    // anahtarı gibi iki temada aynı belirteç ailesi (dolu, çerçevesiz: kenar = dolgu).
    private static Dictionary<string, string> SeciliEsleme(Func<CssKaynak.Bildirim, bool> blok)
    {
        var d = Bildirimler("dugme.css").Where(x => x.Ozellik.StartsWith("--secili-dolgu", StringComparison.Ordinal) && blok(x))
            .ToDictionary(x => x.Ozellik, x => Regex.Match(x.Deger, @"^var\((--[a-z0-9-]+)\)$").Groups[1].Value);
        Assert.Equal(3, d.Count);
        Assert.All(d.Values, v => Assert.StartsWith("--", v, StringComparison.Ordinal));
        return d;
    }

    [Fact]
    public void Segment_secili_dolgu_HER_temada_okunur()
    {
        var e = SeciliEsleme(x => Normal(x.Secici) == "html[data-mod]" && x.AtKural.Length == 0);
        // Tema temizliği (17.10): şemaya özgü eşleme KALMADI.
        Assert.DoesNotContain(Bildirimler("dugme.css"), x => x.Secici.Contains("data-sema", StringComparison.Ordinal));
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();
        foreach (var (tema, tablo) in t.Temalar())
        {
            foreach (var (on, zemin, asgari) in new[]
                     {
                         (e["--secili-dolgu-metin"], e["--secili-dolgu"], 4.5),
                         ("--kenarlik", "--yuzey", 3.0),   // --sekme-kenar = var(--kenarlik) (belirtecler.css takma adı)
                     })
            {
                var a = t.Coz(on, tablo); var z = t.Coz(zemin, tablo);
                if (a is null || z is null || !Kontrast.HexMi(a) || !Kontrast.HexMi(z)) { kirik.Add($"{tema}: {on}/{zemin} çözülemedi"); continue; }
                var oran = Kontrast.Oran(a, z);
                if (oran < asgari) kirik.Add($"{tema}: {on} {a} / {zemin} {z} = {oran:0.00} < {asgari}");
            }
        }
        Assert.True(kirik.Count == 0, "Kontrast altı:\n  " + string.Join("\n  ", kirik));
    }

    // KESIF-P7 tur 2 (tasarım hakemi KRİTİK, DUGME-2 koyu tema): karanlıkta seçili segment/sekme ana eylem
    // mavisiyle (.btn-birincil zemini --vurgu) ÇERÇEVELENMEZ ve BOYANMAZ; seçili = dolu, çerçevesiz
    // (kenar = dolgu) — üst çubuk anahtarı (#ust-mod-basit) ile aynı yüzey.
    [Fact]
    public void Koyu_temada_secili_kenar_birincil_dugme_zemini_degil_ve_cercevesiz()
    {
        Assert.Equal("var(--vurgu)", TekDeger("app.css", ".btn-birincil", "background"));
        Assert.DoesNotContain(Bildirimler("dugme.css"), x => x.Ozellik.StartsWith("--secili-dolgu", StringComparison.Ordinal)
            && (x.AtKural.Contains("prefers-color-scheme", StringComparison.Ordinal) || x.Secici.Contains("karanlik", StringComparison.Ordinal))
            && !x.Deger.Contains("kenar-yuzey", StringComparison.Ordinal) && !x.Deger.Contains("kenar-metin", StringComparison.Ordinal));
        var e = SeciliEsleme(x => Normal(x.Secici) == "html[data-mod]" && x.AtKural.Length == 0);
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();
        var koyuSayisi = 0;
        foreach (var (tema, tablo) in t.Temalar())
        {
            if (!tema.StartsWith("KARANLIK", StringComparison.Ordinal)) continue;
            koyuSayisi++;
            var kenar = t.Coz(e["--secili-dolgu-kenar"], tablo);
            var dolgu = t.Coz(e["--secili-dolgu"], tablo);
            var birincil = t.Coz("--vurgu", tablo);
            var anahtar = t.Coz("--kenar-yuzey", tablo);
            if (kenar is null || dolgu is null || birincil is null || anahtar is null) { kirik.Add($"{tema}: çözülemedi"); continue; }
            if (string.Equals(kenar, birincil, StringComparison.OrdinalIgnoreCase)) kirik.Add($"{tema}: seçili kenar {kenar} = birincil zemin");
            if (string.Equals(dolgu, birincil, StringComparison.OrdinalIgnoreCase)) kirik.Add($"{tema}: seçili dolgu {dolgu} = birincil zemin");
            if (!string.Equals(kenar, dolgu, StringComparison.OrdinalIgnoreCase)) kirik.Add($"{tema}: seçili kenar {kenar} ≠ dolgu {dolgu} (hap içinde hap)");
            if (!string.Equals(dolgu, anahtar, StringComparison.OrdinalIgnoreCase)) kirik.Add($"{tema}: seçili dolgu {dolgu} ≠ üst çubuk anahtarı {anahtar}");
        }
        Assert.True(koyuSayisi > 0, "karanlık tema tablosu bulunamadı");
        Assert.True(kirik.Count == 0, string.Join("\n  ", kirik));
    }

    // ── 10) Yerleşik seçici kabını aşmaz · satır içi açıcı kesikli kutu çizmez ──
    [Fact]
    public void Select_kabini_asmaz_ve_satir_ici_acici_dinlenirken_kutu_cizmez()
    {
        Assert.Equal("100%", TekDeger("dugme.css", "html[data-mod] select:not([multiple]):not([size])", "max-width"));
        Assert.Equal("0", TekDeger("dugme.css", "html[data-mod] select:not([multiple]):not([size])", "min-width"));
        Assert.Equal("transparent", TekDeger("dugme.css", "html[data-mod] .ana-icerik .si-tetik", "border-color"));
        Assert.Equal("var(--dugme-kenar)", TekDeger("dugme.css", "html[data-mod] .ana-icerik .si-tetik", "border-bottom-color"));
        Assert.Equal("var(--dugme-kenar)", TekDeger("dugme.css", "html[data-mod] .ana-icerik .si-tetik:is(:hover, :focus-visible)", "border-color"));
    }

    // ── 11) KATMAN-1b: Kasalar ⋯ satır menüsü tek görünüm ──────────────────────
    [Fact]
    public void Satir_menusu_etkin_opak_devre_disi_dolgusuz_genislik_sinirli()
    {
        var b = Bildirimler("katman.css");
        Assert.Contains(b, x => x.Secici.Contains(".satir-menu[popover][data-katman=\"bagli\"]:popover-open", StringComparison.Ordinal)
                                && x.Ozellik == "max-width" && x.Deger.StartsWith("min(22rem", StringComparison.Ordinal));
        Assert.Equal("1", TekDeger("katman.css", ".satir-menu[popover] .satir-menu-ogeler > :is(.btn, .dugme):not(:disabled)", "opacity"));
        Assert.Equal("1px solid var(--dugme-kenar)", TekDeger("katman.css", ".satir-menu[popover] .satir-menu-ogeler > .btn-ikincil:not(:disabled)", "border"));
        Assert.Equal("transparent", TekDeger("katman.css", ".satir-menu[popover] .satir-menu-ogeler > :is(.btn, .dugme):disabled", "background"));
        Assert.Equal("var(--metin-soluk)", TekDeger("katman.css", ".satir-menu[popover] .satir-menu-ogeler > :is(.btn, .dugme):disabled", "color"));
        // app.css .kasa-duzenle-btn (opacity .65) menü öğesinde ezilir.
        Assert.True(Karsilastir(Ozgulluk(".satir-menu[popover] .satir-menu-ogeler > :is(.btn, .dugme):not(:disabled)"), Ozgulluk(".kasa-duzenle-btn")) > 0);
    }
}
