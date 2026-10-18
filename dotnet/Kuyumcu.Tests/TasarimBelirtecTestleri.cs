using System.Globalization;
using System.Text;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TASARIM BELİRTEÇLERİ — paket UX-2 Faz A1
//
// Aşkın'ın K4 koşulu: "kontrast testi ÖLÇÜLEBİLİR tanımla kurulur". Buradaki
// hesap WCAG 2.1'in kendi tanımıdır (sRGB doğrusallaştırma → göreli parlaklık
// → (L1+0.05)/(L2+0.05)); gözle bakmak kanıt sayılmaz.
//
// ⚠ ÖLÇÜM KAPSAMI:
//   • `var(--x)` takma adları ÇÖZÜLÜR (--govde → --yuzey-govde → #f8fafc).
//     Çözülmezse --amber-700 → --vurgu zinciri hiç ölçülmezdi.
//   • Alfa içeren değerler (rgb(… / .2), linear-gradient) kapsam DIŞIDIR:
//     üstüne bindiği zemin bilinmeden kontrast hesaplanamaz. Bu bilinçli bir
//     sınırdır, gizlenmiş bir boşluk değil.
//   • `--ayrac` DEKORATİF ayraçtır (kart çizgisi, tablo satır çizgisi) ve
//     WCAG 1.4.11 kapsamı dışındadır. Etkileşimli bileşen kenarı için
//     `--kenarlik` vardır ve o ≥3:1 ölçülür.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Küçük bir CSS ayrıştırıcısı — yorum ayıklar, bildirimleri bağlamıyla verir.</summary>
internal static class CssKaynak
{
    internal sealed record Bildirim(string AtKural, string Secici, string Ozellik, string Deger);

    private static string? kok;

    /// <summary>Kuyumcu.sln'i barındıran `dotnet/` klasörünü bulur.</summary>
    internal static string Kok()
    {
        if (kok is not null) return kok;
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı — test kaynak ağacının dışında koşuyor.");
        return kok = d!.FullName;
    }

    internal static string Yol(params string[] parcalar) =>
        Path.Combine(new[] { Kok() }.Concat(parcalar).ToArray());

    internal static string Metin(params string[] parcalar)
    {
        var y = Yol(parcalar);
        Assert.True(File.Exists(y), $"Dosya yok: {y}");
        return File.ReadAllText(y);
    }

    /// <summary>
    /// Yalnız KOD: JS `//` ve `/* */`, HTML `&lt;!-- --&gt;` yorumları silinir.
    /// (denetim.sh'ın ilkesi: "eskiden burada eval vardı" açıklaması ihlal sayılmaz.)
    /// </summary>
    internal static string YorumsuzKod(string metin)
    {
        var s = System.Text.RegularExpressions.Regex.Replace(metin, @"<!--.*?-->", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        s = YorumsuzCss(s);                                   // /* … */
        return System.Text.RegularExpressions.Regex.Replace(s, @"(?m)^[ \t]*//.*$", "");
    }

    /// <summary>/* … */ yorumlarını siler (yorumdaki renk "kod" sayılmaz).</summary>
    internal static string YorumsuzCss(string css)
    {
        var sb = new StringBuilder(css.Length);
        for (var i = 0; i < css.Length; i++)
        {
            if (css[i] == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                var son = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (son < 0) break;
                i = son + 1;
                continue;
            }
            sb.Append(css[i]);
        }
        return sb.ToString();
    }

    /// <summary>Bildirimleri (at-kural yığını + seçici) bağlamıyla çıkarır.</summary>
    internal static List<Bildirim> Bildirimler(string css)
    {
        var temiz = YorumsuzCss(css);
        var sonuc = new List<Bildirim>();
        var yigin = new List<string>();
        var tampon = new StringBuilder();

        void Bosalt()
        {
            var parca = tampon.ToString().Trim();
            tampon.Clear();
            if (parca.Length == 0) return;

            var atKural = string.Join(" ", yigin.Where(x => x.StartsWith('@')));
            var secici = string.Join(" ", yigin.Where(x => !x.StartsWith('@')));
            var ayrac = parca.IndexOf(':');
            if (ayrac <= 0) return;
            sonuc.Add(new Bildirim(atKural, secici,
                parca[..ayrac].Trim(), parca[(ayrac + 1)..].Trim()));
        }

        foreach (var ch in temiz)
        {
            switch (ch)
            {
                case '{':
                    yigin.Add(tampon.ToString().Trim().Replace("\n", " "));
                    tampon.Clear();
                    break;
                case '}':
                    Bosalt();
                    if (yigin.Count > 0) yigin.RemoveAt(yigin.Count - 1);
                    break;
                case ';':
                    Bosalt();
                    break;
                default:
                    tampon.Append(ch);
                    break;
            }
        }
        return sonuc;
    }
}

/// <summary>WCAG 2.1 göreli parlaklık ve kontrast oranı.</summary>
internal static class Kontrast
{
    internal static bool HexMi(string deger) =>
        System.Text.RegularExpressions.Regex.IsMatch(deger.Trim(), "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$");

    internal static (double R, double G, double B) Ayir(string hex)
    {
        var h = hex.Trim().TrimStart('#');
        if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
        return (int.Parse(h[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
                int.Parse(h[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0,
                int.Parse(h[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0);
    }

    private static double Dogrusal(double k) => k <= 0.03928 ? k / 12.92 : Math.Pow((k + 0.055) / 1.055, 2.4);

    /// <summary>WCAG 2.1 göreli parlaklık.</summary>
    internal static double Parlaklik(string hex)
    {
        var (r, g, b) = Ayir(hex);
        return 0.2126 * Dogrusal(r) + 0.7152 * Dogrusal(g) + 0.0722 * Dogrusal(b);
    }

    /// <summary>İki rengin kontrast oranı (1 … 21).</summary>
    internal static double Oran(string hex1, string hex2)
    {
        var l1 = Parlaklik(hex1);
        var l2 = Parlaklik(hex2);
        var (ust, alt) = l1 >= l2 ? (l1, l2) : (l2, l1);
        return (ust + 0.05) / (alt + 0.05);
    }
}

/// <summary>
/// belirtecler.css'in belirteç tabloları.
/// <para>
/// ⚠ KARANLIK TEMA İKİ BLOKTA tanımlıdır ve CSS'te bunları birleştirmenin
/// yolu yoktur (biri medya sorgusunun İÇİNDE):
///   • <c>@media (prefers-color-scheme: dark) :root:not([data-tema="aydinlik"])</c>
///   • <c>:root[data-tema="karanlik"]</c>
/// İkisi tek bir sözlüğe indirgenirse SON yazan kazanır ve birinin bozulması
/// ölçülmeden geçer (M-UX2A1-01b bu boşluğu canlı olarak gösterdi: medya
/// bloğundaki metin rengi kontrast altına indirildiği hâlde test YEŞİL kaldı).
/// Bu yüzden iki blok AYRI tutulur, ayrı ayrı ölçülür ve birbirine EŞİT
/// olmaları da ayrıca sınanır.
/// </para>
/// </summary>
internal sealed class BelirtecTablosu
{
    internal Dictionary<string, string> Aydinlik { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, string> KaranlikMedya { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, string> KaranlikTercih { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// PAKET P-36b — :root[data-sema="…"] renk şemaları. Her şema AYDINLIK
    /// tablosunun ÜSTÜNE biner (tanımlamadığı belirteç temelden gelir), tıpkı
    /// tarayıcıdaki gibi. Böylece "kurumsal" şema --uyari'yi tanımlamasa bile
    /// kontrast ölçümü o çifti ATLAMAZ.
    /// </summary>
    internal Dictionary<string, Dictionary<string, string>> Semalar { get; } =
        new(StringComparer.Ordinal);

    /// <summary>PAKET S1-a — karanlık tarafın ŞEMA blokları, iki kurulum için ayrı.
    /// Medya bloğundaki ve `data-tema="karanlik"` bloğundaki şema değerleri tek
    /// sözlüğe indirgenirse biri sessizce bozulur (M-UX2A1-01b'nin aynısı).</summary>
    internal Dictionary<string, Dictionary<string, string>> KaranlikMedyaSemalar { get; } =
        new(StringComparer.Ordinal);

    internal Dictionary<string, Dictionary<string, string>> KaranlikTercihSemalar { get; } =
        new(StringComparer.Ordinal);

    /// <summary>Ölçülecek temalar: ad + belirteç sözlüğü.</summary>
    internal IEnumerable<(string Ad, Dictionary<string, string> Tablo)> Temalar()
    {
        yield return ("AYDINLIK", Aydinlik);
        yield return ("KARANLIK-MEDYA", KaranlikMedya);
        yield return ("KARANLIK-TERCIH", KaranlikTercih);

        // ⚠ Renk şemaları AYDINLIK temanın şemasıdır (karanlıkta karanlık palet
        //   kazanır — belirtecler.css'teki blok sırası bunu zorlar). Bu yüzden
        //   temel olarak Aydinlik alınır.
        foreach (var (ad, ustu) in Semalar.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var birlesik = new Dictionary<string, string>(Aydinlik, StringComparer.Ordinal);
            foreach (var (k, v) in ustu) birlesik[k] = v;
            yield return ("SEMA-" + ad.ToUpperInvariant(), birlesik);
        }

        // TEMA TEMİZLİĞİ (17.10): tek şema Yıldız. Ürünün TEK yüzü olduğu için her
        // tema ölçütü (kontrast, hover/seçili merdiveni, vurgu metni, amber yasağı,
        // semantik belirteç eksiksizliği) Yıldız'ın üç kurulumunda da koşar.
        foreach (var y in YildizTablosu.Temalar(this))
            yield return y;

        // PAKET S1-a — şema × KARANLIK, iki kurulumda ayrı ayrı ölçülür (tema temizliğinden
        // sonra belirtecler.css'te şema bloğu kalmadı; döngü boş döner ve
        // Tek_sema_YILDIZ_… testi bunun BOŞ kaldığını kilitler).
        foreach (var (kaynak, etiket, taban) in new[]
                 {
                     (KaranlikMedyaSemalar, "KARANLIK-MEDYA-SEMA-", KaranlikMedya),
                     (KaranlikTercihSemalar, "KARANLIK-TERCIH-SEMA-", KaranlikTercih),
                 })
        {
            foreach (var (ad, ustu) in kaynak.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var birlesik = new Dictionary<string, string>(taban, StringComparer.Ordinal);
                foreach (var (k, v) in ustu) birlesik[k] = v;
                yield return (etiket + ad.ToUpperInvariant(), birlesik);
            }
        }
    }

    /// <summary>`:root[data-sema="kurumsal"]` → "kurumsal"; değilse null.</summary>
    /// <summary>Seçici LİSTESİNDEKİ tüm şema adları: `:root[data-sema="platin"],
    /// :root[data-sema="kurumsal"]` → ["platin", "kurumsal"]. Yalnız ilkini almak
    /// takma adı sessizce düşürür (S1-a'da canlı olarak görüldü).</summary>
    private static List<string> SemaAdlari(string secici) =>
        System.Text.RegularExpressions.Regex.Matches(secici, @"data-sema=""([a-z-]+)""")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    internal static BelirtecTablosu Yukle()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        var tablo = new BelirtecTablosu();

        foreach (var b in CssKaynak.Bildirimler(css))
        {
            if (!b.Ozellik.StartsWith("--", StringComparison.Ordinal)) continue;

            var semalar = SemaAdlari(b.Secici);

            if (b.AtKural.Contains("prefers-color-scheme: dark", StringComparison.Ordinal))
            {
                if (semalar.Count == 0) tablo.KaranlikMedya[b.Ozellik] = b.Deger;
                else foreach (var sema in semalar) Koy(tablo.KaranlikMedyaSemalar, sema, b);
            }
            else if (b.Secici.Contains("data-tema=\"karanlik\"", StringComparison.Ordinal))
            {
                if (semalar.Count == 0) tablo.KaranlikTercih[b.Ozellik] = b.Deger;
                else foreach (var sema in semalar) Koy(tablo.KaranlikTercihSemalar, sema, b);
            }
            else if (b.AtKural.Length == 0 && b.Secici == ":root")
                tablo.Aydinlik[b.Ozellik] = b.Deger;
            else if (b.AtKural.Length == 0 && semalar.Count > 0)
                foreach (var sema in semalar) Koy(tablo.Semalar, sema, b);
        }
        return tablo;
    }

    private static void Koy(Dictionary<string, Dictionary<string, string>> hedef,
                            string sema, CssKaynak.Bildirim b)
    {
        if (!hedef.TryGetValue(sema, out var s))
            hedef[sema] = s = new Dictionary<string, string>(StringComparer.Ordinal);
        s[b.Ozellik] = b.Deger;
    }

    /// <summary>
    /// Belirteci çözer: var(--x) zincirleri takip edilir. Karanlık bloklar
    /// yalnız DEĞİŞENLERİ içerir; gerisi :root'tan miras alınır.
    /// </summary>
    internal string? Coz(string ad, Dictionary<string, string> tema, int derinlik = 0)
    {
        if (derinlik > 8) return null;
        if (!tema.TryGetValue(ad, out var deger)
            && (ReferenceEquals(tema, Aydinlik) || !Aydinlik.TryGetValue(ad, out deger)))
            return null;

        var m = System.Text.RegularExpressions.Regex.Match(deger!, @"^var\(\s*(--[a-z0-9-]+)\s*\)$");
        return m.Success ? Coz(m.Groups[1].Value, tema, derinlik + 1) : deger;
    }
}

public sealed class TasarimBelirtecTestleri
{
    /// <summary>Tema ile DEĞİŞMESİ gereken semantik belirteçler.</summary>
    private static readonly string[] SemantikBelirtecler =
    [
        "--yuzey", "--yuzey-2", "--yuzey-3", "--yuzey-govde",
        "--metin", "--metin-guclu", "--metin-soluk",
        "--kenarlik", "--ayrac",
        "--vurgu", "--vurgu-guclu", "--vurgu-metin", "--vurgu-yuzey", "--vurgu-kenar",
        "--basari", "--basari-vurgu", "--basari-yuzey", "--basari-kenar",
        "--uyari", "--uyari-vurgu", "--uyari-yuzey", "--uyari-kenar",
        "--tehlike", "--tehlike-vurgu", "--tehlike-dolgu", "--tehlike-dolgu-metin",
        "--tehlike-yuzey", "--tehlike-kenar",
        "--bilgi", "--bilgi-vurgu", "--bilgi-yuzey", "--bilgi-kenar",
        "--golge-sm", "--golge", "--golge-lg", "--golge-xl",
        // UX-2 Faz A2 — grafik serisi + odak halkası (iki temada da tanımlı)
        "--grafik-1", "--grafik-2", "--grafik-3", "--grafik-4", "--grafik-5",
        "--odak-halka",
        // PAKET S1-a — Radix adım 4/5 (hover/seçili) ve menü rolleri: yoksa 8 şema
        // hover/seçili durumda HAM RENGE düşer, açık menülü Fildişi hiç çalışmaz.
        "--yuzey-hover", "--yuzey-secili",
        "--kenar-yuzey", "--kenar-yuzey-2", "--kenar-metin", "--kenar-metin-guclu",
        "--kenar-ayrac", "--kenar-tehlike",
        "--menu-baslik", "--menu-aktif-yuzey", "--menu-aktif-metin",
    ];

    /// <summary>(ön metin, zemin, asgari oran) — İKİ TEMADA da sınanır.</summary>
    internal static readonly (string Metin, string Zemin, double Asgari)[] Ciftler =
    [
        ("--metin",        "--yuzey",          4.5),
        ("--metin",        "--yuzey-2",        4.5),
        ("--metin",        "--yuzey-3",        4.5),
        ("--metin",        "--yuzey-govde",    4.5),
        ("--metin-guclu",  "--yuzey",          4.5),
        ("--metin-soluk",  "--yuzey",          4.5),
        ("--metin-soluk",  "--yuzey-2",        4.5),
        ("--metin-soluk",  "--yuzey-3",        4.5),
        ("--metin-soluk",  "--yuzey-govde",    4.5),
        ("--vurgu-metin",  "--vurgu",          4.5),
        ("--vurgu",        "--yuzey",          4.5),
        ("--vurgu-guclu",  "--vurgu-yuzey",    4.5),
        ("--basari",       "--yuzey",          4.5),
        ("--basari",       "--basari-yuzey",   4.5),
        ("--uyari",        "--yuzey",          4.5),
        ("--uyari",        "--uyari-yuzey",    4.5),
        ("--tehlike",      "--yuzey",          4.5),
        ("--tehlike",      "--tehlike-yuzey",  4.5),
        ("--tehlike-dolgu-metin", "--tehlike-dolgu", 4.5),
        ("--bilgi",        "--yuzey",          4.5),
        ("--bilgi",        "--bilgi-yuzey",    4.5),
        ("--kenar-metin",  "--kenar-yuzey",    4.5),
        ("--kenar-metin-guclu", "--kenar-yuzey", 4.5),
        // Etkileşimli bileşen kenarı (girdi, seçim, ikincil düğme) — WCAG 1.4.11
        ("--kenarlik",     "--yuzey",          3.0),
        // UX-2 A2: odak halkası ve grafik serisi WCAG 1.4.11 (grafik nesnesi) ≥3:1
        ("--odak-halka",   "--yuzey",          3.0),
        ("--odak-halka",   "--yuzey-2",        3.0),
        ("--grafik-1",     "--yuzey",          3.0),
        ("--grafik-2",     "--yuzey",          3.0),
        ("--grafik-3",     "--yuzey",          3.0),
        ("--grafik-4",     "--yuzey",          3.0),
        ("--grafik-5",     "--yuzey",          3.0),
        // PAKET S1-a — hover/seçili yüzeyler de metin taşır; menü rolleri ölçülür.
        ("--metin",        "--yuzey-hover",    4.5),
        ("--metin",        "--yuzey-secili",   4.5),
        ("--metin-soluk",  "--yuzey-hover",    4.5),
        ("--metin-soluk",  "--yuzey-secili",   4.5),
        ("--menu-baslik",  "--kenar-yuzey",    4.5),
        ("--menu-baslik",  "--kenar-yuzey-2",  4.5),
        ("--menu-aktif-metin", "--menu-aktif-yuzey", 4.5),
        ("--kenar-tehlike", "--kenar-yuzey",   4.5),
    ];

    /// <summary>
    /// Ham hex kilidi (Aşkın K8 — RATCHET). Faz A1 sonunda app.css'te
    /// KÂĞIT DIŞI hiçbir bildirimde ham hex kalmadı. Bu sayı yalnız AŞAĞI
    /// güncellenir; yukarı çekilmesi bir gerilemedir.
    /// </summary>
    private const int HamHexTavani = 0;

    /// <summary>Kâğıt (temadan muaf) bağlamı: belge, ürün etiketi ve @media print.</summary>
    private static bool KagitMi(CssKaynak.Bildirim b) =>
        b.AtKural.Contains("print", StringComparison.OrdinalIgnoreCase)
        || b.Secici.Contains(".belge", StringComparison.Ordinal)
        || b.Secici.Contains(".urun-etiket", StringComparison.Ordinal)
        || b.Secici.Contains(".imza-cizgi", StringComparison.Ordinal);

    [Fact]
    public void Her_semantik_belirtecin_AYDINLIK_ve_KARANLIK_karsiligi_VARDIR()
    {
        var t = BelirtecTablosu.Yukle();
        var eksik = new List<string>();

        foreach (var ad in SemantikBelirtecler)
            foreach (var (tema, tablo) in t.Temalar())
                if (!tablo.ContainsKey(ad))
                    eksik.Add($"{ad} → {tema} blokta YOK");

        Assert.True(eksik.Count == 0, "Eksik belirteçler:\n  " + string.Join("\n  ", eksik));
    }

    [Fact]
    public void Iki_karanlik_blok_AYNI_belirtecleri_AYNI_degerle_tanimlar()
    {
        // CSS'te medya sorgusu içindeki blokla data-tema bloğu birleştirilemez;
        // ikisi elle kopyalanır ve SESSİZCE ayrışabilir. Sistem karanlıkken ile
        // kullanıcı karanlığı seçtiğinde farklı renkler çıkması bir hatadır.
        var t = BelirtecTablosu.Yukle();
        var fark = new List<string>();

        foreach (var ad in t.KaranlikMedya.Keys.Union(t.KaranlikTercih.Keys, StringComparer.Ordinal))
        {
            t.KaranlikMedya.TryGetValue(ad, out var a);
            t.KaranlikTercih.TryGetValue(ad, out var b);
            if (!string.Equals(a, b, StringComparison.Ordinal))
                fark.Add($"{ad}: medya={a ?? "YOK"} · tercih={b ?? "YOK"}");
        }

        // PAKET S1-a — 8 şemanın karanlık blokları da iki kurulumda AYNI olmalı.
        foreach (var sema in t.KaranlikMedyaSemalar.Keys
                     .Union(t.KaranlikTercihSemalar.Keys, StringComparer.Ordinal)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            t.KaranlikMedyaSemalar.TryGetValue(sema, out var ms);
            t.KaranlikTercihSemalar.TryGetValue(sema, out var ts);
            if (ms is null || ts is null)
            {
                fark.Add($"şema {sema}: medya={(ms is null ? "YOK" : "var")} · tercih={(ts is null ? "YOK" : "var")}");
                continue;
            }
            foreach (var ad in ms.Keys.Union(ts.Keys, StringComparer.Ordinal))
            {
                ms.TryGetValue(ad, out var a2);
                ts.TryGetValue(ad, out var b2);
                if (!string.Equals(a2, b2, StringComparison.Ordinal))
                    fark.Add($"{sema}/{ad}: medya={a2 ?? "YOK"} · tercih={b2 ?? "YOK"}");
            }
        }

        Assert.True(fark.Count == 0,
            "İki karanlık blok ayrışmış:\n  " + string.Join("\n  ", fark));
    }

    [Fact]
    public void Karanlik_tema_HEM_medya_sorgusu_HEM_kullanici_tercihi_ile_kurulur()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");

        // Sistem tercihi + kullanıcının "aydınlık" kilidi (yoksa tercih ezilir)
        Assert.Contains("@media (prefers-color-scheme: dark)", css);
        Assert.Contains(":root:not([data-tema=\"aydinlik\"])", css);
        // Sistem aydınlıkken karanlığı zorlama
        Assert.Contains(":root[data-tema=\"karanlik\"]", css);
    }

    [Fact]
    public void Kontrast_AA_saglanir()
    {
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();

        foreach (var (tema, tablo) in t.Temalar())
        {
            foreach (var (metin, zemin, asgari) in Ciftler)
            {
                var m = t.Coz(metin, tablo);
                var z = t.Coz(zemin, tablo);

                if (m is null || z is null)
                {
                    kirik.Add($"{tema}: {metin}/{zemin} → belirteç çözülemedi ({metin}={m ?? "YOK"}, {zemin}={z ?? "YOK"})");
                    continue;
                }
                if (!Kontrast.HexMi(m) || !Kontrast.HexMi(z))
                {
                    kirik.Add($"{tema}: {metin}/{zemin} → hex değil ({m} / {z}); alfa/gradyan ölçülemez");
                    continue;
                }

                var oran = Kontrast.Oran(m, z);
                if (oran + 1e-9 < asgari)
                    kirik.Add($"{tema}: {metin}({m}) / {zemin}({z}) = {oran.ToString("F2", CultureInfo.InvariantCulture)}:1 — en az {asgari.ToString("F1", CultureInfo.InvariantCulture)}:1 olmalı");
            }
        }

        // PAKET YILDIZ-1 → TEMA TEMİZLİĞİ: Yıldız'ın açık + iki karanlık kurulumu artık
        // t.Temalar()'ın içindedir (yukarıdaki döngü ölçer; ikinci kez ölçülmez).
        Assert.Contains(t.Temalar(), x => x.Ad == "YILDIZ-AYDINLIK");
        Assert.Contains(t.Temalar(), x => x.Ad == "YILDIZ-KARANLIK-MEDYA");
        Assert.Contains(t.Temalar(), x => x.Ad == "YILDIZ-KARANLIK-TERCIH");

        Assert.True(kirik.Count == 0, "WCAG 2.1 AA ihlali:\n  " + string.Join("\n  ", kirik));
    }

    [Fact]
    public void Ham_hex_beyaz_listeyi_asmaz()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var hexDeseni = new System.Text.RegularExpressions.Regex(@"#[0-9a-fA-F]{3,8}\b");

        var ihlaller = CssKaynak.Bildirimler(css)
            .Where(b => !KagitMi(b))
            .SelectMany(b => hexDeseni.Matches(b.Deger)
                .Select(m => $"{(b.AtKural.Length > 0 ? b.AtKural + " " : "")}{b.Secici} {{ {b.Ozellik}: {b.Deger} }} → {m.Value}"))
            .ToList();

        Assert.True(ihlaller.Count <= HamHexTavani,
            $"app.css'te kâğıt dışı ham hex sayısı {ihlaller.Count}, tavan {HamHexTavani}:\n  "
            + string.Join("\n  ", ihlaller));
    }

    [Fact]
    public void Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez()
    {
        // Bertan KOŞUL 5: kâğıt beyaz, mürekkep siyah. Karanlık temanın kâğıda
        // sızması geri dönülemez bir müşteri hatasıdır; yorum değil TEST tutar.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");

        var sizintilar = new List<string>();
        foreach (var b in CssKaynak.Bildirimler(css).Where(KagitMi))
        {
            foreach (var belirtec in SemantikBelirtecler)
            {
                if (b.Deger.Contains($"var({belirtec})", StringComparison.Ordinal)
                    || b.Deger.Contains($"var({belirtec},", StringComparison.Ordinal)
                    || b.Deger.Contains($"var({belirtec} ,", StringComparison.Ordinal))
                {
                    sizintilar.Add($"{(b.AtKural.Length > 0 ? b.AtKural + " " : "")}{b.Secici} {{ {b.Ozellik}: {b.Deger} }} → {belirtec}");
                }
            }
        }

        Assert.True(sizintilar.Count == 0,
            "Kâğıt blokları temadan MUAF olmalı, semantik belirtece referans var:\n  "
            + string.Join("\n  ", sizintilar));
    }

    [Fact]
    public void Index_html_belirtecleri_app_css_ten_ONCE_ve_tema_js_i_blazordan_ONCE_yukler()
    {
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));

        var belirtec = html.IndexOf("css/belirtecler.css", StringComparison.Ordinal);
        var app = html.IndexOf("css/app.css", StringComparison.Ordinal);
        var tema = html.IndexOf("js/tema.js", StringComparison.Ordinal);
        var blazor = html.IndexOf("_framework/blazor.webassembly.js", StringComparison.Ordinal);

        Assert.True(belirtec >= 0, "index.html belirtecler.css'i yüklemiyor");
        Assert.True(app > belirtec, "belirtecler.css app.css'ten SONRA yükleniyor");
        Assert.True(tema >= 0, "index.html js/tema.js'i yüklemiyor");
        Assert.True(blazor > tema, "tema.js blazor.webassembly.js'ten SONRA yükleniyor (FOUC)");

        // CSP'de script-src 'unsafe-inline' yok → satır içi <script> ÇALIŞMAZ.
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Tema_betigi_EVAL_kullanmaz_ve_beyaz_liste_disini_KALDIRIR()
    {
        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "tema.js"));

        Assert.DoesNotContain("eval(", js);
        Assert.Contains("window.kuyumcuTema", js);
        Assert.Contains("removeAttribute('data-tema')", js);
        // "sistem"de data-tema="" YAZILMAZ (boş dizge medya sorgusunu bastırır)
        Assert.DoesNotContain("setAttribute('data-tema', '')", js);
    }

    [Fact]
    public void Kilavuz_html_kendi_paletini_TASIMAZ_belirtecleri_baglar()
    {
        // Bertan KOŞUL 1: üçüncü palet kaynağı kapatıldı.
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "kilavuz.html");

        Assert.Contains("css/belirtecler.css", html);
        Assert.DoesNotContain("--amber-50:", html);
        Assert.DoesNotContain("--slate-900:", html);
        Assert.DoesNotContain("--golge-lg:", html);
    }

    [Fact]
    public void Dar_ekran_kurallari_ve_tasma_hapsi_VARDIR()
    {
        // Aşkın K3: taşma document'ta değil <main> içinde oluşur; kaydırma
        // `.tablo-sar` / `.grafik-sar` içinde kalmalı, `.icerik-sutunu`
        // min-width:0 ile daralabilmeli.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var b = CssKaynak.Bildirimler(css);

        Assert.Contains(b, x => x.Secici == ".icerik-sutunu" && x.Ozellik == "min-width" && x.Deger == "0");
        Assert.Contains(b, x => x.Secici == ".tablo-sar" && x.Ozellik == "overflow-x" && x.Deger == "auto");
        Assert.Contains(b, x => x.Secici == ".tablo-sar" && x.Ozellik == "max-width" && x.Deger == "100%");
        Assert.Contains(b, x => x.Secici == ".grafik-sar" && x.Ozellik == "min-width" && x.Deger == "0");

        // Form ızgaraları dar ekranda tek sütuna iner, modal genişliği sınırlanır
        Assert.Contains(b, x => x.AtKural.Contains("max-width: 768px", StringComparison.Ordinal)
                                && x.Secici.Contains(".form-grid-3", StringComparison.Ordinal)
                                && x.Ozellik == "grid-template-columns" && x.Deger == "1fr");
        Assert.Contains(b, x => x.AtKural.Contains("max-width: 768px", StringComparison.Ordinal)
                                && x.Secici == ".modal-panel" && x.Ozellik == "max-width");
    }

    [Fact]
    public void Dugme_takma_adlari_TANIMLI_dir()
    {
        // Aşkın K1: razor'da `class="dugme dugme-birincil"` var ama app.css'te
        // `.dugme` seçicisi YOKTU → o düğmeler stilsiz çiziliyordu.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var seciciler = CssKaynak.Bildirimler(css).Select(x => x.Secici).ToHashSet(StringComparer.Ordinal);

        foreach (var ad in new[] { ".dugme", ".dugme-birincil", ".dugme-ikincil",
                                   ".dugme-tehlike", ".dugme-kucuk", ".dugme-satir" })
            Assert.Contains(ad, seciciler);
    }

    [Fact]
    public void Kontrast_hesabi_WCAG_referans_degerlerini_URETIR()
    {
        // Hesabın kendisi doğrulanmazsa "yeşil" hiçbir şey kanıtlamaz.
        Assert.Equal(21.00, Math.Round(Kontrast.Oran("#000000", "#ffffff"), 2));
        Assert.Equal(1.00, Math.Round(Kontrast.Oran("#777777", "#777777"), 2));
        // WCAG'ın kendi örneği: #777 üzerinde beyaz = 4.48:1 (AA'yı SAĞLAMAZ)
        Assert.Equal(4.48, Math.Round(Kontrast.Oran("#777777", "#ffffff"), 2));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UX-2 FAZ A2 — YENİ KAPILAR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Renk anlamı taşıyan belirteç önekleri (yoğunluk/mod bunlara dokunamaz).</summary>
    private static readonly string[] RenkOnekleri =
    [
        "--yuzey", "--metin", "--kenarlik", "--ayrac", "--vurgu", "--basari",
        "--uyari", "--tehlike", "--bilgi", "--kenar-", "--grafik-", "--odak-halka",
        "--amber-", "--slate-", "--yesil", "--kirmizi", "--mavi", "--sari", "--gri-",
        "--giris-panel",
    ];

    /// <summary>
    /// PAKET S-3e (QA turu 5 /fiyatlandirma — YÜKSEK): vurgulu tablo satırı
    /// karanlık temada AÇIK amber zeminde kalıyor, üstündeki --metin (#e8eef6)
    /// okunmuyordu. Sebep: HAM palet belirteci (--amber-50) temayla DEĞİŞMEZ.
    /// Kural: zemin boyayan satır belirteci SEMANTİK olmalı.
    /// </summary>
    [Fact]
    public void Vurgulu_tablo_satiri_HAM_PALET_degil_SEMANTIK_belirtec_kullanir()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var bildirim = CssKaynak.Bildirimler(css)
            .Where(b => b.Secici.Contains("satir-vurgu", StringComparison.Ordinal)
                        && b.Ozellik is "background" or "background-color")
            .ToList();

        Assert.True(bildirim.Count > 0, "tr.satir-vurgu zemin bildirimi YOK.");
        foreach (var b in bildirim)
        {
            Assert.DoesNotContain("--amber-", b.Deger, StringComparison.Ordinal);
            Assert.DoesNotContain("--slate-", b.Deger, StringComparison.Ordinal);
            Assert.Contains("var(--vurgu-yuzey", b.Deger, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Yogunluk_ve_mod_bloklari_YALNIZ_OLCU_belirteci_degistirir()
    {
        // Brif kabul ölçütü: "[data-yogunluk='sik'] ve [data-mod='basit'] blokları
        // yalnız ölçü belirteci değiştirir, RENK belirteci değiştirmez."
        // Aksi hâlde yoğunluk/mod seçimi kontrast sözleşmesini sessizce bozardı.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        var bloklar = CssKaynak.Bildirimler(css)
            .Where(b => b.Secici.Contains("data-yogunluk", StringComparison.Ordinal)
                        || b.Secici.Contains("data-mod", StringComparison.Ordinal))
            .ToList();

        Assert.True(bloklar.Count > 0, "[data-yogunluk] / [data-mod] blokları YOK.");

        var ihlal = bloklar
            .Where(b => RenkOnekleri.Any(o => b.Ozellik.StartsWith(o, StringComparison.Ordinal)))
            .Select(b => $"{b.Secici} {{ {b.Ozellik}: {b.Deger} }}")
            .ToList();

        Assert.True(ihlal.Count == 0,
            "Yoğunluk/mod bloğu RENK belirteci değiştiriyor:\n  " + string.Join("\n  ", ihlal));

        // Dört blok da tanımlı olmalı (sık/rahat × basit/profesyonel)
        foreach (var s in new[] { "[data-yogunluk=\"sik\"]", "[data-yogunluk=\"rahat\"]",
                                  "html[data-mod=\"basit\"]", "html[data-mod=\"profesyonel\"]" })
            Assert.Contains(s, css);
    }

    [Fact]
    public void Vendor_CSS_LISANSLIDIR_ve_HICBIR_SECICI_tasimaz()
    {
        // Brif riski: "Open Props vendored kopyası lisans başlığı olmadan girerse
        // denetim kapısına takılır." Ayrıca ikinci bir bileşen sistemi getirmemeli:
        // dosyada :root DIŞINDA seçici olamaz.
        var yol = new[] { "Kuyumcu.Web", "wwwroot", "css", "vendor", "open-props-easing-golge.css" };
        var ham = CssKaynak.Metin(yol);

        Assert.Contains("MIT", ham);
        Assert.Contains("Copyright", ham);
        Assert.Contains("open-props", ham);

        var seciciler = CssKaynak.Bildirimler(ham).Select(b => b.Secici).Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal([":root"], seciciler);

        // Yalnız --op-* değerleri; hiçbir element özelliği tanımlanmaz.
        Assert.All(CssKaynak.Bildirimler(ham),
            b => Assert.StartsWith("--op-", b.Ozellik, StringComparison.Ordinal));

        // Yükleme sırası: vendor → belirtecler → app
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));
        var v = html.IndexOf("css/vendor/open-props", StringComparison.Ordinal);
        var b2 = html.IndexOf("css/belirtecler.css", StringComparison.Ordinal);
        var a = html.IndexOf("css/app.css", StringComparison.Ordinal);
        Assert.True(v >= 0 && v < b2 && b2 < a, $"Yükleme sırası bozuk: vendor={v}, belirtec={b2}, app={a}");
    }

    [Fact]
    public void Odak_halkasi_TEK_KAYNAKTAN_beslenir_ve_etkilesim_siniflarini_kapsar()
    {
        // Faz D kabul ölçütünün A2'ye çekilmiş hâli: odak halkası tanımı olmayan
        // .btn/.dugme/.girdi/.secim kalmaz ve halkanın rengi HER YERDE
        // var(--odak-halka)'dır (ikinci kaynak = sessiz kontrast kaybı).
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var b = CssKaynak.Bildirimler(css);

        var halka = b.Where(x => x.Secici.Contains(":focus-visible", StringComparison.Ordinal)
                                 && x.Ozellik == "outline").ToList();
        Assert.True(halka.Count > 0, "Hiçbir :focus-visible outline kuralı yok.");
        Assert.Contains(halka, x => x.Deger.Contains("var(--odak-halka)", StringComparison.Ordinal));

        foreach (var sinif in new[] { ".btn", ".dugme", ".girdi", ".secim", ".sekme" })
            Assert.Contains(halka, x => x.Secici.Contains(sinif + ":focus-visible", StringComparison.Ordinal));
    }

    [Fact]
    public void Dar_ekranda_icerik_tam_genislik_olur_ve_eski_kenar_cubugu_kurali_KALMADI()
    {
        // QA turu 1: 27 rotanın HEPSİNDE 375px'te yatay taşma (scrollWidth 499-1000).
        // Sebep .uygulama{display:flex} + sabit genişlikli kenar çubuğuydu. Tema temizliği (17.10):
        // kenar çubuğu kabukla birlikte kalktı (Yıldız üst menü); dar ekranda içerik yine tam genişlik.
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var hepsi = CssKaynak.Bildirimler(css).ToList();
        var dar = hepsi.Where(x => x.AtKural.Contains("max-width: 768px", StringComparison.Ordinal)).ToList();

        Assert.Contains(dar, x => x.Secici == ".uygulama" && x.Ozellik == "display" && x.Deger == "block");
        Assert.Contains(dar, x => x.Secici == ".icerik-sutunu" && x.Ozellik == "width" && x.Deger == "100%");
        Assert.Contains(dar, x => x.Secici.Contains(".form-grid-2", StringComparison.Ordinal)
                                  && x.Ozellik == "grid-template-columns" && x.Deger == "1fr");
        // Hamburger geniş ekranda gizli (Yıldız kabuğunda her genişlikte gizli: tema-yildiz.css)
        Assert.Contains(hepsi, x => x.Secici == ".kabuk-menu-dugme" && x.Ozellik == "display" && x.Deger == "none");
        // Eski kabuğun hiçbir kuralı kalmadı (ölü seçici yok).
        Assert.DoesNotContain(hepsi, x => System.Text.RegularExpressions.Regex.IsMatch(x.Secici, @"[.#]kenar-[a-z]|\.menu-acik\b"));
    }

    [Fact]
    public void Cekmece_betigi_EVAL_kullanmaz_ve_odagi_GERI_VERIR()
    {
        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "cekmece.js"));

        Assert.DoesNotContain("eval(", js);
        Assert.Contains("window.kuyumcuCekmece", js);
        Assert.Contains("__kuyumcuOncekiOdak", js);   // odak tetikleyiciye döner
        Assert.Contains("'Tab'", js);                  // odak hapsi
    }

    [Fact]
    public void Mod_yazicisi_BEYAZ_LISTE_disini_KALDIRIR_ve_yeni_betik_eklemez()
    {
        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "tema.js"));

        Assert.Contains("window.kuyumcuMod", js);
        Assert.Contains("removeAttribute('data-mod')", js);
        Assert.DoesNotContain("eval(", js);

        // index.html betik sayısı sabit (CDN yok, satır içi yok)
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));
        // P-37 (P-36b kapanışında işlendi): 6 → 7, js/siralistesi.js. Menü
        // sırasını sürükleyerek değiştirmek için gereken yerel köprü betiği;
        // KENDİ KÖKÜMÜZDEN gelir (CDN/npm/satır içi YOK), CSP'de yeni istisna
        // gerekmez. Sayı değil, "dışarıdan kaynak çağrılmıyor" kuralı kilittir.
        // P-39 (K6): 7 → 8, js/secici-yerlesim.js. Açılır seçici panelinin
        // görüş alanı denetimi (TASARIM-DENETIMI § 8.2: 5 vakada panel
        // katlamanın TAMAMEN altında açılıyordu). Yine KENDİ kökümüzden gelir;
        // CDN/npm/satır içi YOK, yeni CSP istisnası gerekmez.
        // KATMAN-1: 8 → 9, js/katman.js. Açılır katmanları (satır menüsü "⋯", "Diğer ▾",
        // kayıt arama paleti) açıcısına bağlayan TEK yardımcı — ölçüldü: popover UA
        // varsayılanıyla ekranın ORTASINDA açılıyordu. Kendi kökümüzden, CSP istisnası yok.
        // EMANET-1 keşif B006: 9 → 10, js/oturum-kilidi.js. Sekmeler arası tek uçuşlu jeton
        // yenileme kilidi (navigator.locks) — iki sekmede tüm oturumların düşmesini önler.
        // Kendi kökümüzden, eval yok, CSP istisnası yok.
        // GIRDI-AILESI-2 (kurucu 08.10 "harf yazılamaz, tuş anında + yapıştırma temizliği"): 10 → 11,
        // js/girdi-maske.js. Ortak girdi bileşenlerinin tuş/yapıştırma maskesi; belge düzeyinde tek
        // dinleyici. Kendi kökümüzden, eval yok, satır içi yok, CSP istisnası yok.
        Assert.Equal(12, System.Text.RegularExpressions.Regex.Matches(html, "<script ").Count);   // D1-13: js/imza.js
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Guvenlik_basliklarinin_TEK_KAYNAGI_Program_cs_tir()
    {
        // ESKİ HALİ (K12): "nginx kabuğu API ile AYNI CSP'yi göndersin."
        // O kural iki kopyayı elle senkron tutmaya çalışıyordu.
        //
        // YENİ HALİ (Paket T-1, MÜHÜR 6 / K5-K6-K7): nginx artık STATİK SUNMAZ
        // (WASM istemcisi de API sürecinden gelir), bu yüzden başlık TEK
        // KAYNAKTAN — Program.cs'ten — çıkar. Vekildeki ikinci kopya:
        //   • iki katmanı ayrıştırıyordu (conf SAMEORIGIN ↔ API DENY),
        //   • /assets/ location'ındaki add_header KALITIMI sunucu düzeyi
        //     başlıkları sessizce DÜŞÜRÜYORDU.
        var nginx = CssKaynak.Metin("..", "deploy", "nginx-kuyumcu.conf");
        var program = CssKaynak.Metin("Kuyumcu.Api", "Program.cs");

        // 1) Vekilde güvenlik başlığı KOPYASI yok (yorumlar sayılmaz).
        var nginxKod = nginx.Split('\n')
            .Where(x => !x.TrimStart().StartsWith("#", StringComparison.Ordinal));
        Assert.DoesNotContain(nginxKod, x => System.Text.RegularExpressions.Regex.IsMatch(
            x, @"add_header\s+(X-Frame-Options|X-Content-Type-Options|Content-Security-Policy|Strict-Transport-Security|Referrer-Policy)"));
        Assert.DoesNotContain("X-Frame-Options SAMEORIGIN", nginx);

        // 2) Tam CSP yönerge kümesi Program.cs'te DURUYOR.
        foreach (var yonerge in new[]
                 {
                     "default-src 'self'", "script-src 'self' 'wasm-unsafe-eval'",
                     "style-src 'self' 'unsafe-inline'", "img-src 'self' data:",
                     "font-src 'self'", "connect-src 'self'", "object-src 'none'",
                     "base-uri 'self'", "form-action 'self'", "frame-ancestors 'none'",
                 })
            Assert.Contains(yonerge, program);

        // 3) Geniş 'unsafe-eval' hiçbir yerde yok; script-src'de unsafe-inline yok.
        Assert.DoesNotContain("'unsafe-eval'",
            program.Replace("'wasm-unsafe-eval'", string.Empty));
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", program);

        // 4) HSTS de tek kopya: Program.cs'te UseHsts, nginx'te YOK.
        //    'preload' bu turda EKLENMEZ (Mehmet vetosu — geri dönüşü zordur).
        Assert.Contains("UseHsts()", program);
        // Yorumdaki "'preload' BU TURDA EKLENMEZ" açıklaması ihlal değildir;
        // ölçüm yalnız KOD satırlarında yapılır.
        Assert.DoesNotContain(nginxKod, x => x.Contains("preload", StringComparison.Ordinal));
        Assert.DoesNotContain("preload", program.Split('\n')
            .Where(x => !x.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .Aggregate(string.Empty, (a, b) => a + "\n" + b));
    }

    [Fact]
    public void Kilavuz_DORT_TEMEL_DURUM_galerisini_sergiler_ve_sabit_renk_tasimaz()
    {
        // K14: "kilavuz.html'de en az dört temel durum (aydınlık/karanlık ×
        // basit/profesyonel) sergilenmezse görsel regresyon kaçar."
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "kilavuz.html");
        var galeriBas = html.IndexOf("<section id=\"galeri\"", StringComparison.Ordinal);
        Assert.True(galeriBas > 0, "kilavuz.html'de tasarım galerisi bölümü YOK.");

        var galeri = html[galeriBas..html.IndexOf("</section>", galeriBas, StringComparison.Ordinal)];
        foreach (var durum in new[]
                 {
                     "data-tema=\"aydinlik\" data-mod=\"basit\"",
                     "data-tema=\"aydinlik\" data-mod=\"profesyonel\"",
                     "data-tema=\"karanlik\" data-mod=\"basit\"",
                     "data-tema=\"karanlik\" data-mod=\"profesyonel\"",
                 })
            Assert.Contains(durum, galeri);

        // Galeride SATIR İÇİ STİL ve SABİT RENK yok (yalnız var() belirteçleri)
        Assert.DoesNotContain("style=", galeri);
        Assert.DoesNotContain("#", galeri.Replace("&amp;", ""));

        // Önizleme kapsamı belirteçlere BAĞLI olmalı (ikinci palet açılmaz)
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        Assert.Contains(".tema-onizleme[data-tema=\"karanlik\"]", css);
        Assert.Contains(".tema-onizleme[data-mod=\"basit\"]", css);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET S1-a — 8 ŞEMA × AYDINLIK/KARANLIK + 18 px ÖLÇEK
    // Kaynak: PLAN-SADELESTIRME-PUSULA-2026-09-23.md § 5A (BAĞLAYICI) +
    // kurucunun onayladığı tasarım tuvali + sadelestirme/TEMA-SISTEMI-2026-09-23.md
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Tema temizliği (17.10) öncesi satılan 8 renk şeması — artık HİÇBİRİNİN bloğu kalmamalı.</summary>
    private static readonly string[] EskiSemalar =
        ["platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi", "kurumsal"];

    [Fact]
    public void Tek_sema_YILDIZ_uc_blokla_tanimli_eski_sekiz_sema_bloku_YOK()
    {
        // Kurucu 17.10: "tema sadece Yıldız, karanlık ve açık mod kalacak".
        var t = BelirtecTablosu.Yukle();
        Assert.Empty(t.Semalar);
        Assert.Empty(t.KaranlikMedyaSemalar);
        Assert.Empty(t.KaranlikTercihSemalar);
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        foreach (var sema in EskiSemalar)
            Assert.DoesNotContain($"data-sema=\"{sema}\"", css, StringComparison.Ordinal);

        // Yıldız açık + sistem karanlık + kullanıcı karanlık blokları var ve yüzeyi tanımlar.
        var y = YildizTablosu.Yukle();
        Assert.True(y.Aydinlik.ContainsKey("--yuzey"), "Yıldız aydınlık bloğu yok");
        Assert.True(y.KaranlikMedya.ContainsKey("--yuzey"), "Yıldız karanlık (medya) bloğu yok");
        Assert.True(y.KaranlikTercih.ContainsKey("--yuzey"), "Yıldız karanlık (tercih) bloğu yok");
    }

    [Fact]
    public void Hover_ve_secili_yuzeyler_HER_BLOKTA_AYRIDIR()
    {
        // Araştırma K8: bu iki rol yoksa 8 şema da hover/seçili durumda ham renge
        // düşer. "Tanımlı ama yüzeyle aynı" da çözüm değildir — fark ÖLÇÜLÜR.
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();

        foreach (var (tema, tablo) in t.Temalar())
        {
            var yuzey = t.Coz("--yuzey", tablo);
            var hover = t.Coz("--yuzey-hover", tablo);
            var secili = t.Coz("--yuzey-secili", tablo);

            if (yuzey is null || hover is null || secili is null)
            { kirik.Add($"{tema}: hover/seçili belirteci YOK"); continue; }

            if (string.Equals(yuzey, hover, StringComparison.OrdinalIgnoreCase))
                kirik.Add($"{tema}: --yuzey-hover, --yuzey ile AYNI ({hover})");
            if (string.Equals(hover, secili, StringComparison.OrdinalIgnoreCase))
                kirik.Add($"{tema}: --yuzey-secili, --yuzey-hover ile AYNI ({secili})");
        }

        Assert.True(kirik.Count == 0, "Hover/seçili merdiveni kırık:\n  " + string.Join("\n  ", kirik));
    }

    [Fact]
    public void Vurgu_metni_SABIT_BEYAZ_DEGIL_OLCULMUSTUR()
    {
        // § 5A / TEMA-SISTEMI K5: --vurgu-metin her şemada ÖLÇÜLEREK seçilir.
        // Karanlıkta vurgu açıldığı için üstündeki metin KOYU olmak zorundadır;
        // sabit #ffffff oraya 1,5:1 ile biner.
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();

        foreach (var (tema, tablo) in t.Temalar())
        {
            var m = t.Coz("--vurgu-metin", tablo);
            var v = t.Coz("--vurgu", tablo);
            if (m is null || v is null) { kirik.Add($"{tema}: --vurgu/--vurgu-metin YOK"); continue; }

            if (string.Equals(m, "#ffffff", StringComparison.OrdinalIgnoreCase))
                kirik.Add($"{tema}: --vurgu-metin SABİT #ffffff — ölçülmemiş");

            if (tema.StartsWith("KARANLIK", StringComparison.Ordinal)
                && Kontrast.Parlaklik(m) > Kontrast.Parlaklik(v))
                kirik.Add($"{tema}: karanlıkta --vurgu-metin({m}) vurgudan({v}) AÇIK — K5 ihlali");
        }

        Assert.True(kirik.Count == 0, "Vurgu metni sözleşmesi:\n  " + string.Join("\n  ", kirik));
    }

    [Fact]
    public void Altin_amber_vurgu_HICBIR_SEMADA_YOKTUR()
    {
        // Kurucu ve müşteri altın/amber vurguyu REDDETTİ (plan § 5A). Kural
        // ölçülebilir: OKLCH hue 55–100° VE chroma ≥0,10 VE L ≥0,55 bandı yasak;
        // ayrıca eski amber merdiveni değerleri vurgu olamaz.
        string[] eskiAmber = ["#f59e0b", "#d97706", "#b45309", "#f0b429", "#fcd34d", "#fbbf24", "#92400e"];
        string[] vurgular = ["--vurgu", "--vurgu-guclu", "--odak-halka"];

        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();

        foreach (var (tema, tablo) in t.Temalar())
            foreach (var ad in vurgular)
            {
                var d = t.Coz(ad, tablo);
                if (d is null || !Kontrast.HexMi(d)) continue;

                if (eskiAmber.Contains(d, StringComparer.OrdinalIgnoreCase))
                    kirik.Add($"{tema}: {ad} = {d} — eski amber merdiveni");

                var (L, C, H) = Oklch.Cevir(d);
                if (H is >= 55 and <= 100 && C >= 0.10 && L >= 0.55)
                    kirik.Add($"{tema}: {ad} = {d} → altın bandı (L={L:F2} C={C:F2} H={H:F0}°)");
            }

        Assert.True(kirik.Count == 0, "Altın/amber vurgu:\n  " + string.Join("\n  ", kirik));
    }

    [Fact]
    public void Yazi_olcegi_UC_KADEME_ve_VARSAYILAN_18px()
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        var b = CssKaynak.Bildirimler(css);

        string? Deger(string secici, string ozellik) => b
            .Where(x => x.AtKural.Length == 0 && x.Secici == secici && x.Ozellik == ozellik)
            .Select(x => x.Deger).LastOrDefault();

        // Varsayılan gövde 18 px (tuval notu n6 · § 5B: yaşlı/ilkokul-lise mezunu)
        Assert.Equal("18px", Deger(":root", "--yazi-govde"));
        Assert.Equal("20px", Deger(":root", "--yazi-rakam"));

        // Üç kademe + eski kodların göçü
        Assert.Equal("16px", Deger(":root[data-yazi=\"16\"], :root[data-olcek=\"yogun\"]", "--yazi-govde"));
        Assert.Equal("18px", Deger(":root[data-yazi=\"18\"]", "--yazi-govde"));
        Assert.Equal("20px", Deger(":root[data-yazi=\"20\"], :root[data-olcek=\"okunakli\"]", "--yazi-govde"));

        // § 5A: para/gram/has gövdeden BİR kademe büyük
        Assert.Equal("18px", Deger(":root[data-yazi=\"16\"], :root[data-olcek=\"yogun\"]", "--yazi-rakam"));
        Assert.Equal("22px", Deger(":root[data-yazi=\"20\"], :root[data-olcek=\"okunakli\"]", "--yazi-rakam"));

        // iOS otomatik yakınlaştırma eşiği: hiçbir kademe 16 px'in ALTINA inmez
        foreach (var x in b.Where(x => x.Ozellik == "--yazi-govde"))
            Assert.True(int.Parse(x.Deger.Replace("px", ""), CultureInfo.InvariantCulture) >= 16,
                $"{x.Secici}: --yazi-govde {x.Deger} — 16 px altı yasak");
    }

    [Fact]
    public void Tema_betigi_TEK_semayi_yazar_ve_CSS_karsiligi_VARDIR()
    {
        // "İstemci bir kod yazıyor, CSS tanımıyor" = sessizce tabana düşen tema. Tema temizliği (17.10):
        // tema.js yalnız 'yildiz' yazar (beyaz liste yok, kalıcı öznitelik), eski kodların hiçbiri betikte yok.
        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "tema.js"));
        Assert.Contains("var SEMA = 'yildiz';", js);
        Assert.DoesNotContain("var SEMALAR", js);
        foreach (var sema in EskiSemalar)
            Assert.DoesNotContain($"'{sema}'", js);
        Assert.True(YildizTablosu.Yukle().Aydinlik.ContainsKey("--yuzey"), "tema.js 'yildiz' yazıyor ama tema-yildiz.css'te blok YOK");
    }

    [Fact]
    public void Sunucunun_sundugu_HER_SEMA_ve_PUNTO_kodunun_CSS_karsiligi_VARDIR()
    {
        // İKİ PAKETİN EL SIKIŞMASI: görünüm seçeneklerini sunan taraf
        // (GorunumServisi — B-1) ile belirteçleri tanımlayan taraf (S1-a) aynı
        // kodları taşımak ZORUNDA. Biri kod eklerse öteki blok açmadıkça ekran
        // SESSİZCE varsayılana düşer — kullanıcı "tema çalışmıyor" der.
        var t = BelirtecTablosu.Yukle();
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        var eksik = new List<string>();

        // Tema temizliği (17.10): sunulan TEK şema Yıldız'dır; bloğu tema-yildiz.css'tedir.
        Assert.Equal(["yildiz"], Kuyumcu.Web.Servisler.GorunumServisi.Semalar);
        foreach (var kod in Kuyumcu.Web.Servisler.GorunumServisi.Semalar)
            if (!(kod == "yildiz" && YildizTablosu.Yukle().Aydinlik.ContainsKey("--yuzey")) && !t.Semalar.ContainsKey(kod))
                eksik.Add($"sema '{kod}': CSS'te blok YOK");

        foreach (var kod in Kuyumcu.Web.Servisler.GorunumServisi.Olcekler)
            if (!css.Contains($"[data-yazi=\"{kod}\"]", StringComparison.Ordinal))
                eksik.Add($"punto '{kod}': belirtecler.css'te [data-yazi] bloğu YOK");

        Assert.True(eksik.Count == 0,
            "Sunucu/CSS şema el sıkışması kırık:\n  " + string.Join("\n  ", eksik));
    }
}

/// <summary>
/// OKLCH (algısal açıklık) dönüşümü — altın/amber yasağının ÖLÇÜLEBİLİR testi
/// için. Kaynak: Björn Ottosson, "A perceptual color space for image processing"
/// (oklab), sRGB → linear → LMS → Oklab → polar.
/// </summary>
internal static class Oklch
{
    private static double Dogrusal(double k) => k <= 0.04045 ? k / 12.92 : Math.Pow((k + 0.055) / 1.055, 2.4);

    internal static (double L, double C, double H) Cevir(string hex)
    {
        var (r0, g0, b0) = Kontrast.Ayir(hex);
        double r = Dogrusal(r0), g = Dogrusal(g0), b = Dogrusal(b0);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        var ll = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
        var aa = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var bb = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;

        var h = Math.Atan2(bb, aa) * 180.0 / Math.PI;
        if (h < 0) h += 360;
        return (ll, Math.Sqrt(aa * aa + bb * bb), h);
    }
}
