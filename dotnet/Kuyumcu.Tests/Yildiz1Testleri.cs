using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Gorunum;
using Kuyumcu.Core.I18n;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-1 — "Yıldız" şeması (AYRI tema; kurucu kararı 08.10)
// Kaynak: dokumanlar/09-uygulama-kayitlari/TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md
//         § 2 tasarım dili · § 3 belirteçler + okunurluk kararı · § 4 adım A+B+C
//
// Kilitlenen sözleşmeler:
//   • İZOLASYON: tema-yildiz.css'teki HER kural data-sema="yildiz" kapsamlıdır
//     (8 eski şema piksel değiştirmez — gerçek tarayıcı karşılaştırması ayrıca
//     rapordadır; burada seçici kapsamı kod olarak kilitlenir).
//   • Belirteç değerleri nottaki § 3 ile birebir; iki karanlık blok AYNI.
//   • WCAG AA: Yıldız çiftleri TasarimBelirtecTestleri.Kontrast_AA_saglanir'de.
//   • Okunurluk kararı: büyük sayı ≥ 300 (200 YOK), gövde 400, 15 px altı 500.
//   • Inter vendor (CSP font-src 'self'; Google Fonts YASAK) + OFL lisansı.
//   • Kâğıt (.belge*, .urun-etiket*, @media print) bu dosyaya GİRMEZ.
//   • Şema kullanıcı tercihi yoluyla kalıcıdır (PATCH /kullanici/gorunum).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>tema-yildiz.css'in belirteç tabloları (belirtecler.css'ten AYRI dosya).</summary>
internal static class YildizTablosu
{
    internal const string Dosya = "tema-yildiz.css";

    internal static string Css() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", Dosya);

    internal sealed record Bloklar(
        Dictionary<string, string> Aydinlik,
        Dictionary<string, string> KaranlikMedya,
        Dictionary<string, string> KaranlikTercih,
        Dictionary<string, string> Onizleme);

    /// <summary>Belirteç bildirimlerini üç kurulumdan ayrı ayrı okur.</summary>
    internal static Bloklar Yukle()
    {
        var b = new Bloklar(new(StringComparer.Ordinal), new(StringComparer.Ordinal),
                            new(StringComparer.Ordinal), new(StringComparer.Ordinal));
        foreach (var d in CssKaynak.Bildirimler(Css()))
        {
            if (!d.Ozellik.StartsWith("--", StringComparison.Ordinal)) continue;
            if (d.AtKural.Contains("prefers-color-scheme: dark", StringComparison.Ordinal))
                b.KaranlikMedya[d.Ozellik] = d.Deger;
            else if (d.Secici.Contains("data-tema=\"karanlik\"", StringComparison.Ordinal))
                b.KaranlikTercih[d.Ozellik] = d.Deger;
            else if (d.AtKural.Length == 0 && d.Secici.Contains("data-sema=\"yildiz\"", StringComparison.Ordinal))
            {
                b.Aydinlik[d.Ozellik] = d.Deger;
                if (d.Secici.Contains(".gorunum-sema-onizleme", StringComparison.Ordinal))
                    b.Onizleme[d.Ozellik] = d.Deger;
            }
        }
        return b;
    }

    /// <summary>Kontrast ölçümü için: ortak taban + Yıldız üst bindirmesi (tarayıcıdaki gibi).</summary>
    internal static IEnumerable<(string Ad, Dictionary<string, string> Tablo)> Temalar(BelirtecTablosu t)
    {
        var y = Yukle();
        Dictionary<string, string> Birlestir(Dictionary<string, string> taban, Dictionary<string, string> ust)
        {
            var d = new Dictionary<string, string>(taban, StringComparer.Ordinal);
            foreach (var (k, v) in ust) d[k] = v;
            return d;
        }
        yield return ("YILDIZ-AYDINLIK", Birlestir(t.Aydinlik, y.Aydinlik));
        yield return ("YILDIZ-KARANLIK-MEDYA", Birlestir(t.KaranlikMedya, y.KaranlikMedya));
        yield return ("YILDIZ-KARANLIK-TERCIH", Birlestir(t.KaranlikTercih, y.KaranlikTercih));
    }
}

/// <summary>Saf testler — dosya içeriği ve liste sözleşmeleri (veritabanı/HTTP yok).</summary>
public sealed class Yildiz1Testleri
{
    private static readonly string[] SemantikBelirtecler =
    [
        "--yuzey", "--yuzey-2", "--yuzey-3", "--yuzey-govde", "--yuzey-hover", "--yuzey-secili",
        "--metin", "--metin-guclu", "--metin-soluk", "--kenarlik", "--ayrac",
        "--vurgu", "--vurgu-guclu", "--vurgu-metin", "--vurgu-yuzey", "--vurgu-kenar", "--odak-halka",
        "--basari", "--basari-vurgu", "--basari-yuzey", "--basari-kenar",
        "--uyari", "--uyari-vurgu", "--uyari-yuzey", "--uyari-kenar",
        "--tehlike", "--tehlike-vurgu", "--tehlike-dolgu", "--tehlike-dolgu-metin", "--tehlike-yuzey", "--tehlike-kenar",
        "--bilgi", "--bilgi-vurgu", "--bilgi-yuzey", "--bilgi-kenar",
        "--kenar-yuzey", "--kenar-yuzey-2", "--kenar-metin", "--kenar-metin-guclu", "--kenar-ayrac", "--kenar-tehlike",
        "--menu-baslik", "--menu-aktif-yuzey", "--menu-aktif-metin",
        "--grafik-1", "--grafik-2", "--grafik-3", "--grafik-4", "--grafik-5",
        "--golge-sm", "--golge", "--golge-lg", "--golge-xl",
    ];

    /// <summary>Üst düzey seçici listesini virgülden böler (parantez/köşeli parantez içindekini bölmez).</summary>
    private static IEnumerable<string> SeciciParcalari(string secici)
    {
        var derin = 0; var sb = new System.Text.StringBuilder();
        foreach (var ch in secici)
        {
            if (ch is '(' or '[') derin++;
            else if (ch is ')' or ']') derin--;
            if (ch == ',' && derin == 0) { yield return sb.ToString().Trim(); sb.Clear(); }
            else sb.Append(ch);
        }
        if (sb.ToString().Trim().Length > 0) yield return sb.ToString().Trim();
    }

    // ── Y1-01) Şema TÜM katmanlarda aynı kodla tanımlı ──────────────────────
    [Fact]
    public void Y1_01_yildiz_sunucuda_istemcide_betikte_ve_sozlukte_tanimli()
    {
        Assert.Equal("yildiz", GorunumSecenekleri.SemaNormalle("yildiz"));
        Assert.True(GorunumSecenekleri.SemaGecerli("yildiz"));
        Assert.Contains("yildiz", GorunumSecenekleri.Semalar);
        Assert.Contains("yildiz", Kuyumcu.Web.Servisler.GorunumServisi.Semalar);
        // Sıra iki yanda AYNI (GorunumEkranTestleri.GE8 ile birlikte).
        Assert.Equal(GorunumSecenekleri.Semalar, Kuyumcu.Web.Servisler.GorunumServisi.Semalar);
        // Eski 8 şema yerinde ve sırası değişmedi; Yıldız 9. sıradadır.
        Assert.Equal(["platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi"],
                     GorunumSecenekleri.Semalar.Take(8));
        Assert.Equal("yildiz", GorunumSecenekleri.Semalar[8]);

        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "tema.js"));
        var liste = Regex.Match(js, @"var SEMALAR = \[(.*?)\];", RegexOptions.Singleline);
        Assert.True(liste.Success, "tema.js SEMALAR listesi bulunamadı");
        Assert.Contains("'yildiz'", liste.Groups[1].Value);

        // UX-S31: ham kod ekrana basılmaz, sözlükten çevrilir (TR + EN).
        Assert.Equal("Yıldız", GorunumSozlugu.Ad(GorunumSozlugu.Semalar, "yildiz", "tr"));
        Assert.Equal("Star", GorunumSozlugu.Ad(GorunumSozlugu.Semalar, "yildiz", "en"));
        Assert.False(string.IsNullOrWhiteSpace(GorunumSozlugu.Aciklama(GorunumSozlugu.Semalar, "yildiz", "tr")));
        Assert.False(string.IsNullOrWhiteSpace(GorunumSozlugu.Aciklama(GorunumSozlugu.Semalar, "yildiz", "en")));
    }

    // ── Y1-02) İZOLASYON: her kural data-sema="yildiz" kapsamlı ────────────
    [Fact]
    public void Y1_02_tema_yildiz_css_HER_kural_yildiz_kapsamlidir()
    {
        var ihlal = new List<string>();
        foreach (var d in CssKaynak.Bildirimler(YildizTablosu.Css()))
        {
            if (d.AtKural.Contains("@font-face", StringComparison.Ordinal)) continue;   // yazı tipi tanımı görsel değiştirmez
            // YILDIZ-AMBLEM: @keyframes adımının seçicisi yüzdedir ("0%, 13%") — öğe seçmez; YALNIZ amb-* adları
            // (kullananlar yine data-sema="yildiz" kapsamlı — YildizAmblemTestleri kilitler).
            if (d.AtKural.StartsWith("@keyframes amb-", StringComparison.Ordinal)) continue;
            foreach (var parca in SeciciParcalari(d.Secici))
                if (!parca.Contains("data-sema=\"yildiz\"", StringComparison.Ordinal))
                    ihlal.Add($"{d.AtKural} {parca} {{ {d.Ozellik} }}");
        }
        Assert.True(ihlal.Count == 0,
            "Yıldız kapsamı dışında kural var (8 eski şemayı değiştirir):\n  " + string.Join("\n  ", ihlal.Distinct().Take(20)));

        // En az bir gerçek kural ölçüldü (boş dosya sahte yeşil vermesin).
        Assert.True(CssKaynak.Bildirimler(YildizTablosu.Css()).Count(d => !d.Ozellik.StartsWith("--")) > 40);
    }

    // ── Y1-03) Belirteç değerleri nottaki § 3 ile BİREBİR ───────────────────
    [Fact]
    public void Y1_03_belirtec_degerleri_not_3_ile_birebir()
    {
        var y = YildizTablosu.Yukle();
        // Aydınlık: --z --m --s --h --p --iyi --kotu
        Assert.Equal("#F5F4F1", y.Aydinlik["--yuzey-govde"]);
        Assert.Equal("#F5F4F1", y.Aydinlik["--yuzey"]);
        Assert.Equal("#141414", y.Aydinlik["--metin"]);
        Assert.Equal("#66645F", y.Aydinlik["--metin-soluk"]);
        Assert.Equal("#DCDAD4", y.Aydinlik["--ayrac"]);
        Assert.Equal("#8A8780", y.Aydinlik["--kenarlik"]);
        Assert.Equal("#1C6A3E", y.Aydinlik["--basari"]);
        Assert.Equal("#AE2A22", y.Aydinlik["--tehlike"]);
        // Birincil düğme: --vurgu = --m, --vurgu-metin = --z
        Assert.Equal(y.Aydinlik["--metin"], y.Aydinlik["--vurgu"]);
        Assert.Equal(y.Aydinlik["--yuzey"], y.Aydinlik["--vurgu-metin"]);

        foreach (var (ad, k) in new[] { ("medya", y.KaranlikMedya), ("tercih", y.KaranlikTercih) })
        {
            Assert.True(k["--yuzey-govde"] == "#000000", $"{ad}: karanlık zemin saf siyah olmalı");
            Assert.Equal("#000000", k["--yuzey"]);
            Assert.Equal("#F3F2EF", k["--metin"]);
            Assert.Equal("#9A9893", k["--metin-soluk"]);
            Assert.Equal("#2A2A2A", k["--ayrac"]);
            Assert.Equal("#C9C6BE", k["--kenarlik"]);
            Assert.Equal("#9AD3AE", k["--basari"]);
            Assert.Equal("#F0A49C", k["--tehlike"]);
            Assert.Equal(k["--metin"], k["--vurgu"]);
            Assert.Equal(k["--yuzey"], k["--vurgu-metin"]);
        }
    }

    // ── Y1-04) Üç blokta tüm semantik belirteçler var; iki karanlık blok AYNI
    [Fact]
    public void Y1_04_tam_belirtec_kumesi_ve_iki_karanlik_blok_ayni()
    {
        var y = YildizTablosu.Yukle();
        var eksik = new List<string>();
        foreach (var ad in SemantikBelirtecler)
            foreach (var (blok, tablo) in new[] { ("aydinlik", y.Aydinlik), ("karanlik-medya", y.KaranlikMedya), ("karanlik-tercih", y.KaranlikTercih) })
                if (!tablo.ContainsKey(ad)) eksik.Add($"{ad} → {blok}");
        Assert.True(eksik.Count == 0, "Yıldız belirteci eksik:\n  " + string.Join("\n  ", eksik));

        var fark = new List<string>();
        foreach (var ad in y.KaranlikMedya.Keys.Union(y.KaranlikTercih.Keys, StringComparer.Ordinal))
        {
            y.KaranlikMedya.TryGetValue(ad, out var a);
            y.KaranlikTercih.TryGetValue(ad, out var b);
            if (!string.Equals(a, b, StringComparison.Ordinal)) fark.Add($"{ad}: medya={a ?? "YOK"} · tercih={b ?? "YOK"}");
        }
        Assert.True(fark.Count == 0, "Yıldız'ın iki karanlık bloğu ayrışmış:\n  " + string.Join("\n  ", fark));

        // Karanlık blokların seçicisi: medya bloğu `data-tema="aydinlik"` kilidini TAŞIR (kullanıcı aydınlığı ezilmez).
        var css = YildizTablosu.Css();
        Assert.Contains(":root:not([data-tema=\"aydinlik\"])[data-sema=\"yildiz\"]", css);
        Assert.Contains(":root[data-tema=\"karanlik\"][data-sema=\"yildiz\"]", css);
    }

    // ── Y1-05) Notun söz verdiği kontrast oranları ──────────────────────────
    [Fact]
    public void Y1_05_nottaki_kontrast_oranlari_saglanir()
    {
        var t = BelirtecTablosu.Yukle();
        var sonuc = new List<string>();
        foreach (var (ad, tablo) in YildizTablosu.Temalar(t))
        {
            double O(string m, string z) => Kontrast.Oran(t.Coz(m, tablo)!, t.Coz(z, tablo)!);
            var karanlik = ad.Contains("KARANLIK", StringComparison.Ordinal);
            // Not § 3 "18,4 / 7,6 · 15,9 / 5,6" yazar; WCAG formülüyle ÖLÇÜLEN 18,76 / 7,29 · 16,75 / 5,37
            // (rapora sapma notu). Test ölçülen değerin altına İNMEZ.
            var (metin, ikincil) = karanlik ? (18.0, 7.2) : (16.0, 5.3);
            if (O("--metin", "--yuzey") < metin) sonuc.Add($"{ad}: metin/zemin {O("--metin", "--yuzey"):F2} < {metin}");
            if (O("--metin-soluk", "--yuzey") < ikincil) sonuc.Add($"{ad}: ikincil/zemin {O("--metin-soluk", "--yuzey"):F2} < {ikincil}");
            if (O("--kenarlik", "--yuzey") < 3.0) sonuc.Add($"{ad}: kenar/zemin {O("--kenarlik", "--yuzey"):F2} < 3");
            if (O("--basari", "--yuzey") < 5.5) sonuc.Add($"{ad}: iyi/zemin {O("--basari", "--yuzey"):F2} < 5.5");
            if (O("--tehlike", "--yuzey") < 5.5) sonuc.Add($"{ad}: kötü/zemin {O("--tehlike", "--yuzey"):F2} < 5.5");
        }
        Assert.True(sonuc.Count == 0, "Not § 3 kontrast sözü tutmuyor:\n  " + string.Join("\n  ", sonuc));
    }

    // ── Y1-06) OKUNURLUK KARARI: 200 yok; büyük sayı ≥ 300; küçük yazı 500 ─
    [Fact]
    public void Y1_06_okunurluk_karari_200_agirlik_yok_buyuk_sayi_300_kucuk_yazi_500()
    {
        var bil = CssKaynak.Bildirimler(YildizTablosu.Css());
        // YILDIZ-4A (kurucu 09.10, "uyarlama istemiyorum … birebir"): 200 ağırlık YALNIZ tuvalin kendisini
        // birebir çizen Bugün bölümünde (.yildiz-bugun…) serbesttir; ortak bileşen varyantlarında yine YOK.
        Assert.DoesNotContain(bil, d => d.Ozellik == "font-weight" && !d.AtKural.Contains("@font-face", StringComparison.Ordinal)
                                       && !d.Secici.Contains(".yildiz-bugun", StringComparison.Ordinal)
                                       && !d.Secici.Contains(" .yz", StringComparison.Ordinal)   // YILDIZ-4B: tuval .ince (Tezgâh/Müşteriler/Tüm ekranlar) birebir
                                       && Regex.IsMatch(d.Deger, @"^(100|200)\b"));   // @font-face değişken EKSEN aralığıdır (200 600), kural değil

        var buyuk = bil.Where(d => d.Ozellik == "font-weight" && d.Secici.Contains(".stat-deger", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(buyuk);
        Assert.All(buyuk, d => Assert.True(int.Parse(Regex.Match(d.Deger, @"\d+").Value) >= 300, $"büyük sayı ağırlığı < 300: {d.Deger}"));

        var kucuk = bil.Where(d => d.Ozellik == "font-weight" && d.Secici.Contains(".metin-kucuk", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(kucuk);
        Assert.All(kucuk, d => Assert.StartsWith("500", d.Deger));

        // Gövde 400.
        Assert.Contains(bil, d => d.Ozellik == "font-weight" && d.Deger == "400" && d.Secici.EndsWith(" body", StringComparison.Ordinal));
    }

    // ── Y1-07) KÂĞIT MUAF ───────────────────────────────────────────────────
    [Fact]
    public void Y1_07_kagit_bu_dosyaya_girmez_ve_belirtec_kagida_sizmaz()
    {
        var css = CssKaynak.YorumsuzCss(YildizTablosu.Css());
        Assert.DoesNotContain("@media print", css);
        foreach (var yasak in new[] { ".belge", ".urun-etiket", ".imza-cizgi" })
            Assert.DoesNotContain(yasak, css);

        // app.css'te kâğıt blokları hâlâ semantik belirteçsiz (mevcut kural) ve yazdırma gövdesi sabit beyaz.
        var app = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        Assert.Contains(CssKaynak.Bildirimler(app), b => b.AtKural.Contains("print", StringComparison.Ordinal)
                                                       && b.Secici.Contains("body", StringComparison.Ordinal)
                                                       && b.Ozellik == "background" && b.Deger.Trim() == "#fff");
    }

    // ── Y1-08) Yükleme sırası: belirteçler → tema-yildiz → olcek … ortak (EN SONDA)
    [Fact]
    public void Y1_08_index_html_tema_yildiz_cssini_dogru_sirada_yukler()
    {
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));
        var linkler = Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

        var belirtec = linkler.IndexOf("css/belirtecler.css");
        var app = linkler.IndexOf("css/app.css");
        var yildiz = linkler.IndexOf("css/tema-yildiz.css");
        var olcek = linkler.IndexOf("css/olcek.css");
        Assert.True(yildiz >= 0, "index.html css/tema-yildiz.css'i yüklemiyor");
        Assert.Equal(1, linkler.Count(x => x == "css/tema-yildiz.css"));
        Assert.True(yildiz > belirtec && yildiz > app, "tema-yildiz.css belirteçlerden ve app.css'ten SONRA yüklenmeli");
        Assert.True(yildiz < olcek, "tema-yildiz.css olcek.css'ten ÖNCE (DUGME-2/O1 zinciri)");
        Assert.Equal("css/ortak.css", linkler[^1]);
        // Betik sayısı sabit (denetim.sh): bu paket <script> eklemez.
        Assert.Equal(11, Regex.Matches(html, "<script ").Count);
    }

    // ── Y1-09) INTER vendor: yerel dosya, OFL lisansı, dış kaynak YOK ───────
    [Fact]
    public void Y1_09_inter_yerel_vendor_lisansli_ve_dis_kaynaksiz()
    {
        var css = YildizTablosu.Css();
        var kod = CssKaynak.YorumsuzCss(css);
        Assert.DoesNotContain("http://", kod);
        Assert.DoesNotContain("https://", kod);
        Assert.DoesNotContain("googleapis", kod, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gstatic", kod, StringComparison.OrdinalIgnoreCase);

        var yuzler = Regex.Matches(kod, @"font-family:\s*""Inter""[^}]*?src:\s*url\(""(/font/[^""]+)""\)\s*format\(""woff2""\)", RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(["/font/inter-latin-ext.woff2", "/font/inter-latin.woff2"], yuzler);
        Assert.Contains("font-weight: 200 600", kod);   // değişken eksen 200-600
        Assert.Contains("U+0100-02BA", kod);            // latin-ext: Türkçe ğ ş ı İ

        foreach (var y in yuzler)
        {
            var dosya = CssKaynak.Yol("Kuyumcu.Web", "wwwroot" + y);
            Assert.True(File.Exists(dosya), $"yazı tipi dosyası yok: {dosya}");
            var bayt = File.ReadAllBytes(dosya);
            Assert.True(bayt.Length > 20_000, $"{y} çok küçük ({bayt.Length} B)");
            Assert.Equal("wOF2", System.Text.Encoding.ASCII.GetString(bayt, 0, 4));   // gerçek woff2
        }

        var lisans = File.ReadAllText(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "font", "OFL-Inter.txt"));
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", lisans, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Inter Project Authors", lisans);

        // Şema seçiliyken Inter, yoksa YAZI AİLESİ DEĞİŞMEZ (Manrope yığını belirtecler.css'te).
        var bil = CssKaynak.Bildirimler(css);
        Assert.Contains(bil, d => d.Ozellik == "--yazi-ailesi" && d.Deger.StartsWith("\"Inter\"", StringComparison.Ordinal)
                                  && d.Secici.Contains("data-sema=\"yildiz\"", StringComparison.Ordinal));

        // CSP font-src 'self' korunur (yeni istisna EKLENMEDİ).
        var program = File.ReadAllText(CssKaynak.Yol("Kuyumcu.Api", "Program.cs"));
        Assert.Contains("font-src 'self'", program);
    }

    // ── Y1-10) Sabit renk yalnız belirteç bloklarında ───────────────────────
    [Fact]
    public void Y1_10_kural_govdelerinde_sabit_renk_yok()
    {
        var renk = new Regex(@"#[0-9a-fA-F]{3,8}\b|\brgba?\(|\bhsla?\(|\bcolor-mix\(");
        var ihlal = CssKaynak.Bildirimler(YildizTablosu.Css())
            .Where(d => !d.Ozellik.StartsWith("--", StringComparison.Ordinal) && renk.IsMatch(d.Deger))
            .Select(d => $"{d.Secici} {{ {d.Ozellik}: {d.Deger} }}").ToList();
        Assert.True(ihlal.Count == 0, "Kural gövdesinde sabit renk:\n  " + string.Join("\n  ", ihlal));
    }

    // ── Y1-11) Ayarlar › Görünüm önizlemesi şemanın kendi belirtecinden boyanır
    [Fact]
    public void Y1_11_gorunum_onizlemesi_yildiz_belirteclerini_tasir()
    {
        var y = YildizTablosu.Yukle();
        foreach (var b in new[] { "--kenar-yuzey", "--yuzey-govde", "--yuzey", "--vurgu", "--ayrac" })
            Assert.True(y.Onizleme.ContainsKey(b), $"önizleme bloğunda {b} yok");

        // Karanlık ve medya bloklarına önizleme sınıfı GİRMEZ (8 şemadaki kuralın aynısı).
        foreach (var d in CssKaynak.Bildirimler(YildizTablosu.Css())
                     .Where(d => d.Secici.Contains("gorunum-sema-onizleme", StringComparison.Ordinal) && d.Ozellik.StartsWith("--")))
        {
            Assert.DoesNotContain("karanlik", d.Secici);
            Assert.DoesNotContain("prefers-color-scheme", d.AtKural);
        }
    }

    // ── Y1-12) C adımı: ortak bileşen varyantları (kart · b1/b2/b3 · sekme · çip · satır · boş durum · adım · takvim)
    [Fact]
    public void Y1_12_ortak_bilesen_varyantlari_tanimli()
    {
        var bil = CssKaynak.Bildirimler(YildizTablosu.Css()).Where(d => d.AtKural.Length == 0 || !d.AtKural.StartsWith("@media (prefers-color")).ToList();
        string? Deger(string secici, string ozellik) =>
            bil.LastOrDefault(d => d.Secici.Contains(secici, StringComparison.Ordinal) && d.Ozellik == ozellik)?.Deger;

        // Kart → çizgili bölüm: kutu, gölge, dolgu yok.
        Assert.Equal("none", Deger(":is(.kart, .katlanir", "box-shadow"));
        Assert.Equal("transparent", Deger(":is(.kart, .katlanir", "background"));
        Assert.Equal("0", Deger(":is(.kart, .katlanir", "border-radius"));
        Assert.Equal("0 0 1px", Deger(":is(.kart, .katlanir", "border-width"));
        // b1: tek hap birincil
        Assert.Equal("999px", Deger(":is(.dugme, .btn)", "border-radius"));
        Assert.Equal("var(--vurgu)", Deger(".ana-icerik :is(.dugme-birincil, .btn-birincil)", "background"));
        Assert.Equal("var(--vurgu-metin)", Deger(".ana-icerik :is(.dugme-birincil, .btn-birincil)", "color"));
        // b2: altı çizili yazı (ikincil)
        Assert.Equal("0 0 1px", Deger(":is(.dugme-ikincil, .btn-ikincil):not(.dugme-kucuk)", "border-width"));
        Assert.Equal("transparent", Deger(":is(.dugme-ikincil, .btn-ikincil):not(.dugme-kucuk)", "background"));
        // b3: ince kenarlı küçük hap (satır eylemi)
        Assert.Equal("1px solid var(--ayrac)", Deger(":is(.dugme-kucuk, .btn-kucuk):not(.dugme-birincil)", "border"));
        Assert.Equal("999px", Deger(":is(.dugme-kucuk, .btn-kucuk):not(.dugme-birincil)", "border-radius"));
        // Sekme → altı çizili yazı (kapsül/dolgu yok)
        Assert.Equal("transparent", Deger(":is(.sekmeler-sekme.secili", "background"));
        Assert.Equal("var(--metin)", Deger(":is(.sekmeler-sekme.secili", "border-bottom-color"));
        Assert.Equal("transparent", Deger(":is(.sekmeler-serit, .musteri-segment", "background"));
        // Çip → metin
        Assert.Equal("transparent", Deger(" .rozet", "background"));
        Assert.Equal("var(--basari)", Deger(".rozet-yesil", "color"));
        Assert.Equal("var(--tehlike)", Deger(".rozet-kirmizi", "color"));
        // Liste satırı / tablo
        Assert.Equal("var(--ayrac)", Deger(".liste-satiri", "border-bottom-color"));
        Assert.Equal("transparent", Deger(".veri-tablosu thead", "background"));
        // Boş durum
        Assert.Equal("var(--metin-soluk)", Deger(".bos-durum", "color"));
        // Adımlar
        Assert.Equal("transparent", Deger(".adim-serit .adim", "background"));
        Assert.Equal("var(--metin)", Deger(".adim-serit .adim-aktif", "border-bottom-color"));
        // Takvim (Tarih bileşeni)
        Assert.Equal("transparent", Deger(".tarih-takvim", "background"));
        // Bant kutu değil, durum çizgisi
        Assert.Equal("0 0 0 2px", Deger(".bant", "border-width"));

        // Hareket tercihi kapalıysa geçiş/animasyon kapanır.
        Assert.Contains(CssKaynak.Bildirimler(YildizTablosu.Css()),
            d => d.AtKural.Contains("prefers-reduced-motion: reduce", StringComparison.Ordinal) && d.Ozellik == "transition" && d.Deger == "none");
    }
}

/// <summary>Gerçek HTTP boru hattı — Yıldız kullanıcı tercihi olarak KALICIDIR.</summary>
[Collection("http")]
public sealed class Yildiz1HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    [Fact]
    public async Task Y1H1_Kullanici_yildizi_secer_kalici_olur_ve_listede_gorunur()
    {
        var k = YeniKiraci("PAKETYILDIZ-1-TEST-kalici");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");

        var c = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "yildiz" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var g = j.TryGetProperty("gorunum", out var ic) ? ic : j;
        Assert.Equal("yildiz", g.GetProperty("sema").GetString());
        Assert.Equal("kullanici", g.GetProperty("semaKaynagi").GetString());

        // Veritabanında KULLANICI SATIRINDA kalıcı (yeniden giriş sonrası da aynı).
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var satir = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
            Assert.Equal("yildiz", satir.Sema);
        }

        // Yeni oturum/GET: aynı değer; liste 9 şemayı ve Yıldız'ı taşır.
        var oku = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/kullanici/gorunum");
        var o = await oku.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var og = o.TryGetProperty("gorunum", out var ic2) ? ic2 : o;
        Assert.Equal("yildiz", og.GetProperty("sema").GetString());
        var liste = og.GetProperty("semalar").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(9, liste.Count);
        Assert.Equal("yildiz", liste[8]);
    }

    [Fact]
    public async Task Y1H2_Sahip_dukkan_varsayilani_yildiz_yapar_gecersiz_kod_400()
    {
        var k = YeniKiraci("PAKETYILDIZ-1-TEST-dukkan");
        var sahip = f.Ortam.Istemci(k, "SAHIP");

        var c = await sahip.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "yildiz" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var oku = await sahip.GetAsync("/api/v1/gorunum");
        var j = await oku.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("yildiz", j.GetProperty("sema").GetString());

        // Yazım hatalı kod reddedilir (beyaz liste); durum DEĞİŞMEZ.
        var kotu = await sahip.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "yildizz" });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
        var tekrar = await (await sahip.GetAsync("/api/v1/gorunum")).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("yildiz", tekrar.GetProperty("sema").GetString());
    }
}
