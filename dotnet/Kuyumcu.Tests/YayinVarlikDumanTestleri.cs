using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Kuyumcu.Web.Layout;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-43 — CANLI DUMAN KAPISININ DERLEME-ZAMANI İKİZİ
//
// NEDEN VAR (tur 8'de ÖLÇÜLEN arıza, iddia değil kanıt):
//   • Yayın anlık görüntüsünde wwwroot/kilavuz.html YOKTU (kaynakta 60.845
//     bayt VAR) → canlı /kilavuz.html 404 döndü, /kilavuz sayfasının 14
//     bağlantısının 14'ü de boşa çıktı.
//   • _framework varlıklarından biri bozuk servis edilince tarayıcı "Failed
//     to start platform" verdi; grup 1'in 6 rotası HİÇ açılmadı.
//
// Canlı kapı `betikler/duman-testi.sh`'tir (5199 üzerinde HTTP). Bu dosya
// aynı kuralların SUNUCU GEREKTİRMEYEN yarısıdır: kaynak ağaç bağlantılarının
// karşılığı var mı, kılavuz publish dışı bırakılmış mı, boot.json'un saydığı
// varlıklar diskte 0 bayt mı. Böylece kapı yalnız "5199 ayaktaysa" değil,
// her `dotnet test` koşumunda da ölçer.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YayinVarlikDumanTestleri
{
    /// <summary>Kaynaktaki kılavuzun alt sınırı (kaynak 60.845 bayt; altı = kırpılmış/boş).</summary>
    private const long KilavuzAltSinirBayt = 60_000;

    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string WebKoku() => Path.Combine(DotnetKoku(), "Kuyumcu.Web");
    private static string WebWwwroot() => Path.Combine(WebKoku(), "wwwroot");
    private static string KilavuzHtmlYolu() => Path.Combine(WebWwwroot(), "kilavuz.html");

    // ── glob → regex (MSBuild öğe kalıpları: ** / * / ?) ─────────────────────
    private static bool GlobEslesir(string kalip, string goreliYol)
    {
        var k = kalip.Trim().Replace('\\', '/');
        var y = goreliYol.Replace('\\', '/');
        var rx = "^" + Regex.Escape(k)
            .Replace(@"\*\*/", "(?:.*/)?")
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", "[^/]") + "$";
        return Regex.IsMatch(y, rx, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Bir csproj'un kılavuzu yayından DÜŞÜREN öğelerini döndürür:
    /// (a) herhangi bir öğe türünde <c>Remove="…"</c> kalıbı kılavuzu yakalıyorsa,
    /// (b) <c>Update="…"</c> kılavuzu yakalıyor ve <c>CopyToPublishDirectory="Never"</c> ise.
    /// </summary>
    private static List<string> KilavuzuDusurenOgeler(string csprojYolu, string goreliYol)
    {
        var sonuc = new List<string>();
        var x = XDocument.Load(csprojYolu);
        foreach (var e in x.Descendants().Where(e => e.Parent?.Name.LocalName == "ItemGroup"))
        {
            foreach (var ozn in new[] { "Remove", "Update", "Exclude" })
            {
                var deger = (string?)e.Attribute(ozn);
                if (deger is null) continue;
                var eslesti = deger.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Any(k => GlobEslesir(k, goreliYol));
                if (!eslesti) continue;
                var kopya = (string?)e.Attribute("CopyToPublishDirectory") ?? (string?)e.Element(e.Name.Namespace + "CopyToPublishDirectory");
                var yayinDisi = ozn is "Remove" or "Exclude"
                                || string.Equals(kopya, "Never", StringComparison.OrdinalIgnoreCase);
                if (yayinDisi)
                    sonuc.Add($"{Path.GetFileName(csprojYolu)}: <{e.Name.LocalName} {ozn}=\"{deger}\"{(kopya is null ? "" : $" CopyToPublishDirectory=\"{kopya}\"")}>");
            }
        }
        return sonuc;
    }

    /// <summary>
    /// Kılavuz kaynakta VAR, 60.000 bayttan büyük, bölüm çıpaları dolu ve
    /// hiçbir csproj onu yayından düşürmüyor. Tur 8'de yayında YOKTU.
    /// </summary>
    [Fact]
    public void KilavuzHtml_KaynaktaVarVeBosDegil()
    {
        var yol = KilavuzHtmlYolu();
        Assert.True(File.Exists(yol), $"kilavuz.html kaynakta YOK: {yol}");
        var bayt = new FileInfo(yol).Length;
        Assert.True(bayt >= KilavuzAltSinirBayt,
            $"kilavuz.html {bayt} bayt — en az {KilavuzAltSinirBayt} olmalı (kırpılmış ya da boş dosya).");

        var icerik = File.ReadAllText(yol);
        var bolumSayisi = Regex.Matches(icerik, "<section\\s+id=\"[^\"]+\"").Count;
        Assert.True(bolumSayisi > 0, "kilavuz.html'de hiç <section id=\"…\"> çıpası yok.");

        // Yayından düşürme: Web projesi (kılavuzun sahibi) + onu yayınlayan Api projesi.
        var dusurenler = new List<string>();
        dusurenler.AddRange(KilavuzuDusurenOgeler(Path.Combine(WebKoku(), "Kuyumcu.Web.csproj"), "wwwroot/kilavuz.html"));
        dusurenler.AddRange(KilavuzuDusurenOgeler(Path.Combine(DotnetKoku(), "Kuyumcu.Api", "Kuyumcu.Api.csproj"), "wwwroot/kilavuz.html"));
        Assert.True(dusurenler.Count == 0,
            "kilavuz.html yayından DÜŞÜRÜLÜYOR (tur 8'deki /kilavuz.html 404'ünün tam karşılığı):\n  "
            + string.Join("\n  ", dusurenler));
    }

    /// <summary>
    /// Kilavuz.razor'daki HER bağlantı (düz href + bölüm listesinden üretilen
    /// <c>/kilavuz.html#çıpa</c>) kaynak wwwroot'ta karşılık bulur: dosya var ve
    /// çıpa kılavuzda <c>id="…"</c> olarak geçer. Bugünkü sayım 1 + 13 = 14.
    /// </summary>
    [Fact]
    public void KilavuzRazor_TumBaglantilarininKarsiligiVar()
    {
        var razor = File.ReadAllText(Path.Combine(WebKoku(), "Pages", "Kilavuz.razor"));
        var kilavuz = File.ReadAllText(KilavuzHtmlYolu());

        // (1) Düz href'ler — Razor ifadesi olanlar (href="@(…)") ayrı çözülür.
        var duzHrefler = Regex.Matches(razor, "href=\"(/[^\"@]*)\"")
            .Select(m => m.Groups[1].Value).ToList();
        // (2) Bölüm çıpaları: ("capa", "Başlık") çiftleri. Şablon href="@($"/kilavuz.html#{capa}")".
        Assert.Contains("/kilavuz.html#{capa}", razor);
        var capalar = Regex.Matches(razor, "\\(\\s*\"([a-z0-9-]+)\"\\s*,\\s*\"[^\"]+\"\\s*\\)")
            .Select(m => m.Groups[1].Value).ToList();

        var baglantilar = duzHrefler.Concat(capalar.Select(c => $"/kilavuz.html#{c}")).ToList();
        Assert.True(baglantilar.Count >= 14,
            $"Kilavuz.razor'da {baglantilar.Count} bağlantı bulundu; tur 8 ölçümünde 14'tü (ayrıştırıcı bozulmuş olabilir).");

        var karsiliksiz = new List<string>();
        foreach (var b in baglantilar)
        {
            var parca = b.Split('#', 2);
            var dosya = parca[0].TrimStart('/');
            var tam = Path.Combine(WebWwwroot(), dosya.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrEmpty(dosya) || !File.Exists(tam) || new FileInfo(tam).Length == 0)
            {
                karsiliksiz.Add($"{b} → wwwroot/{dosya} YOK ya da 0 bayt");
                continue;
            }
            if (parca.Length == 2)
            {
                var hedef = dosya == "kilavuz.html" ? kilavuz : File.ReadAllText(tam);
                if (!Regex.IsMatch(hedef, $"id=\"{Regex.Escape(parca[1])}\""))
                    karsiliksiz.Add($"{b} → wwwroot/{dosya} içinde id=\"{parca[1]}\" YOK");
            }
        }
        Assert.True(karsiliksiz.Count == 0,
            $"Kilavuz.razor'un {baglantilar.Count} bağlantısından {karsiliksiz.Count} tanesinin kaynakta karşılığı yok:\n  "
            + string.Join("\n  ", karsiliksiz));
    }

    /// <summary>
    /// Kenar çubuğunun her bağlantısı gerçek bir <c>@page</c>'e oturur (SPA
    /// rotası index.html yedeğiyle açılır → index.html ve onun yerel css/js
    /// bağlantıları da wwwroot'ta VAR olmalı). Uzantılı bir menü yolu
    /// (ör. <c>/kilavuz.html</c>) doğrudan wwwroot dosyasıdır.
    /// </summary>
    [Fact]
    public void MenuTanimi_StatikBaglantilarinKarsiligiVar()
    {
        var sayfalar = Directory.GetFiles(Path.Combine(WebKoku(), "Pages"), "*.razor", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), "^@page\\s+\"([^\"]+)\"", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        var karsiliksiz = new List<string>();
        foreach (var o in MenuTanimi.Ogeler)
        {
            var yol = o.Yol.Split('?', '#')[0];
            if (Path.HasExtension(yol))
            {
                var tam = Path.Combine(WebWwwroot(), yol.TrimStart('/'));
                if (!File.Exists(tam) || new FileInfo(tam).Length == 0)
                    karsiliksiz.Add($"menü {o.Yol} → wwwroot dosyası YOK ya da 0 bayt");
            }
            else if (!sayfalar.Contains(yol))
            {
                karsiliksiz.Add($"menü {o.Yol} → hiçbir Pages/*.razor '@page \"{yol}\"' tanımlamıyor");
            }
        }

        // SPA yedeği: menü rotalarının HEPSİ index.html üzerinden açılır.
        var index = Path.Combine(WebWwwroot(), "index.html");
        Assert.True(File.Exists(index) && new FileInfo(index).Length > 0, "wwwroot/index.html YOK ya da 0 bayt — hiçbir rota açılmaz.");
        foreach (Match m in Regex.Matches(File.ReadAllText(index), "(?:href|src)=\"([^\"]+)\""))
        {
            var v = m.Groups[1].Value;
            // Mutlak/kök/boş bağlantılar ve derleme çıktısı _framework bu testin kapsamı değil
            // (_framework BootJsonVarliklari_SifirBaytDegil'de ölçülür).
            if (v is "/" or "." || v.StartsWith("http", StringComparison.Ordinal)
                || v.StartsWith("_framework/", StringComparison.Ordinal) || v.StartsWith('#'))
                continue;
            var tam = Path.Combine(WebWwwroot(), v.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(tam) || new FileInfo(tam).Length == 0)
                karsiliksiz.Add($"index.html {v} → wwwroot dosyası YOK ya da 0 bayt");
        }

        Assert.True(MenuTanimi.Ogeler.Count > 0, "MenuTanimi.Ogeler boş — ölçülecek bağlantı yok.");
        Assert.True(karsiliksiz.Count == 0,
            $"{karsiliksiz.Count} statik bağlantının karşılığı yok:\n  " + string.Join("\n  ", karsiliksiz));
    }

    /// <summary>
    /// Yayın dizini: <c>$KUYUMCU_YAYIN_DIZINI</c>, yoksa <c>$HOME/.kuyumcu-yayin</c>
    /// (betikler/yayin-yolu.sh ile AYNI öncelik — P-42 tek kaynak).
    /// </summary>
    private static string YayinDizini()
    {
        var env = Environment.GetEnvironmentVariable("KUYUMCU_YAYIN_DIZINI");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kuyumcu-yayin");
    }

    /// <summary>boot.json'daki (tür, ad) çiftleri — satelliteResources iç içe olduğu için kapsam dışı (duman-testi.sh ile aynı kural).</summary>
    private static List<(string Tur, string Ad)> BootVarliklari(string bootYolu)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(bootYolu));
        var r = doc.RootElement.GetProperty("resources");
        var liste = new List<(string, string)>();
        foreach (var tur in r.EnumerateObject())
        {
            if (tur.Value.ValueKind != JsonValueKind.Object) continue;
            foreach (var ad in tur.Value.EnumerateObject())
                if (ad.Value.ValueKind == JsonValueKind.String)
                    liste.Add((tur.Name, ad.Name));
        }
        return liste;
    }

    /// <summary>
    /// boot.json'un saydığı HER varlık diskte var ve 0 bayt DEĞİL. Tek bir
    /// 0 baytlık/eksik varlık = "Failed to start platform" = uygulama açılmaz.
    /// İki kaynak ölçülür: (1) Web derleme çıktısı (her zaman), (2) yayın dizini
    /// (VARSA — yayın hiç üretilmemiş bir makinede yalnız (1) ölçülür; ikisi de
    /// yoksa test KIRMIZI, sessiz geçiş yok).
    /// </summary>
    [Fact]
    public void BootJsonVarliklari_SifirBaytDegil()
    {
        var olculen = new List<string>();
        var bozuk = new List<string>();

        void Olc(string etiket, string wwwroot)
        {
            var boot = Path.Combine(wwwroot, "_framework", "blazor.boot.json");
            if (!File.Exists(boot)) return;
            var varliklar = BootVarliklari(boot);
            Assert.True(varliklar.Count > 0, $"{etiket}: boot.json'da hiç varlık yok ({boot}).");
            foreach (var (tur, ad) in varliklar)
            {
                var tam = Path.Combine(wwwroot, "_framework", ad);
                if (!File.Exists(tam)) bozuk.Add($"{etiket}: _framework/{ad} ({tur}) YOK");
                else if (new FileInfo(tam).Length == 0) bozuk.Add($"{etiket}: _framework/{ad} ({tur}) 0 bayt");
            }
            olculen.Add($"{etiket}: {varliklar.Count} varlık");
        }

        // (1) Web derleme çıktısı — test koşumu Web projesini derlediği için daima vardır.
        var cikti = Path.Combine(WebKoku(), "bin");
        foreach (var yapilandirma in new[] { "Debug", "Release" })
        {
            var w = Path.Combine(cikti, yapilandirma, "net8.0", "wwwroot");
            if (Directory.Exists(w)) Olc($"derleme/{yapilandirma}", w);
        }
        // (2) Yayın dizini (5199'un servis ettiği anlık görüntü).
        var yayin = Path.Combine(YayinDizini(), "wwwroot");
        if (Directory.Exists(yayin)) Olc($"yayın ({YayinDizini()})", yayin);

        Assert.True(olculen.Count > 0,
            "Hiçbir blazor.boot.json bulunamadı (derleme çıktısı da yayın dizini de yok) — ölçülemedi, geçilemez.");
        Assert.True(bozuk.Count == 0,
            $"boot.json varlıklarından {bozuk.Count} tanesi eksik/0 bayt (tur 8 'Failed to start platform' kök nedeni) — ölçülen: "
            + string.Join(" · ", olculen) + "\n  " + string.Join("\n  ", bozuk.Take(30)));
    }
}
