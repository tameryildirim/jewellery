using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kuyumcu.Core.Kilavuz;

namespace Kuyumcu.Tests.Araclar;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-11) — KILAVUZ ÜRETİCİSİ: docs/KULLANIM_KILAVUZU.md (TEK KAYNAK)
// + docs/kilavuz-sablon.html (yalnız sunum) + docs/HARIC-LISTESI.md (gömülür)
// → Kuyumcu.Web/wwwroot/kilavuz.html (ÜRETİLİR, elle düzenlenmez).
//
// NEDEN TEST PROJESİNDE: üreticinin tek iki çağıranı (a) "kılavuz güncel mi"
// kapısı (KilavuzUretimTestleri) ve (b) yeniden üretme betiği
// dotnet/kilavuz-uret.sh'tır — o da aynı testi KILAVUZ_YAZ=1 ile koşar. Ürün
// derlemesine (WASM) girmesine gerek yoktur; çıpa işlevi ise iki tarafın da
// kullandığı Kuyumcu.Core.Kilavuz.KilavuzCapasi'dadır.
//
// DESTEKLENEN İŞARETLEME (kasıtlı olarak küçük — sürpriz yok):
//   # Başlık                      → <title> + kapak başlığı (ilk paragraf kapak metni)
//   ## Başlık {#capa}             → <section id="capa"> (çıpa ZORUNLU, tekil)
//   ### Başlık {#capa}            → <h3 id="capa"> (çıpa isteğe bağlı)
//   <!-- grup: Ad -->             → içindekilerde grup başlığı
//   <!-- ekran: /rota -->         → <span class="ekran-capa" id="e-…"> (KilavuzCapasi)
//   <!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md --> → HARİÇ tablosu GÖMÜLÜR (BK-26)
//   diğer <!-- … -->              → atlanır (yazar notu)
//   paragraf · "- " liste · "1. " adım listesi · "> [!IPUCU|BILGI|UYARI]" kutu
//   · "| a | b |" tablo · **kalın** · *eğik* · `kod` · [metin](#capa)
// Metin HTML-KAÇIRILIR: kaynağa yazılmış bir <script> kılavuzda metin olarak
// görünür, çalışmaz (kılavuz oturumsuz okunur — betik taşımaz).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Markdown tek kaynaktan kılavuz HTML'i üretir.</summary>
public static partial class KilavuzUretici
{
    /// <summary>Kılavuzdaki bir ekran işareti: rota, bağlı olduğu başlık, çıpa.</summary>
    public sealed record EkranIsareti(string Rota, string Baslik, string Capa, int Satir);

    /// <summary>Üretim sonucu.</summary>
    public sealed record Sonuc(string Html, IReadOnlyList<EkranIsareti> Ekranlar, IReadOnlyList<string> BolumCapalari);

    public const string HaricIsareti = "<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->";
    public const string Uretici = "dotnet/kilavuz-uret.sh";

    /// <summary>Dosya baytlarının sha256'sı (küçük harf onaltılık) — denetim.sh shasum ile aynı.</summary>
    public static string Sha256(byte[] bayt) => Convert.ToHexString(SHA256.HashData(bayt)).ToLowerInvariant();

    /// <summary>Üretim damgası: denetim.sh KILAVUZ_URETILMIS bunu kaynak dosyaların karmasıyla karşılaştırır.</summary>
    public static string Damga(string kaynakSha, string haricSha, string sablonSha) =>
        $"<!-- ÜRETİLDİ — ELLE DÜZENLEMEYİN. kaynak: docs/KULLANIM_KILAVUZU.md · kaynak-sha256: {kaynakSha} · " +
        $"haric-sha256: {haricSha} · sablon-sha256: {sablonSha} · üretici: {Uretici} -->";

    public static Sonuc Uret(string md, string sablon, string haricMd, string kaynakSha, string haricSha, string sablonSha)
    {
        var satirlar = md.Replace("\r\n", "\n").Split('\n');
        var icerik = new StringBuilder();
        var toc = new StringBuilder();
        var ekranlar = new List<EkranIsareti>();
        var capalar = new List<string>();
        var kimlikler = new HashSet<string>(StringComparer.Ordinal);

        string? baslik = null;
        var kapakMetni = new StringBuilder();
        var kapakBitti = false;
        var bolumAcik = false;
        var bolumNo = 0;
        string? sonBaslik = null;

        var paragraf = new List<string>();
        var liste = new List<string>();
        var listeSirali = false;
        var kutu = new List<string>();
        string? kutuTuru = null;
        var tablo = new List<string>();

        void Kapat()
        {
            if (paragraf.Count > 0)
            {
                icerik.Append("      <p>").Append(Satir(string.Join(' ', paragraf))).Append("</p>\n");
                paragraf.Clear();
            }
            if (liste.Count > 0)
            {
                icerik.Append(listeSirali ? "      <ol class=\"adimlar\">\n" : "      <ul>\n");
                foreach (var l in liste) icerik.Append("        <li>").Append(Satir(l)).Append("</li>\n");
                icerik.Append(listeSirali ? "      </ol>\n" : "      </ul>\n");
                liste.Clear();
            }
            if (kutu.Count > 0)
            {
                icerik.Append("      <div class=\"kutu ").Append(kutuTuru).Append("\">");
                foreach (var p in string.Join('\n', kutu).Split("\n\n"))
                {
                    var metin = p.Replace('\n', ' ').Trim();
                    if (metin.Length > 0) icerik.Append("<p>").Append(Satir(metin)).Append("</p>");
                }
                icerik.Append("</div>\n");
                kutu.Clear();
                kutuTuru = null;
            }
            if (tablo.Count > 0)
            {
                TabloYaz(icerik, tablo);
                tablo.Clear();
            }
        }

        for (var i = 0; i < satirlar.Length; i++)
        {
            var ham = satirlar[i];
            var s = ham.TrimEnd();
            var no = i + 1;

            // ── Yorumlar ve işaretler ────────────────────────────────────────
            if (s.TrimStart().StartsWith("<!--", StringComparison.Ordinal))
            {
                var t = s.Trim();
                if (!t.EndsWith("-->", StringComparison.Ordinal))
                {
                    // Çok satırlı yazar notu: kapanışa kadar atla.
                    while (i + 1 < satirlar.Length && !satirlar[i].Contains("-->", StringComparison.Ordinal)) i++;
                    continue;
                }
                var govde = t[4..^3].Trim();
                if (govde.StartsWith("grup:", StringComparison.Ordinal))
                {
                    Kapat();
                    toc.Append("      <div class=\"bolum\">").Append(Kacir(govde[5..].Trim())).Append("</div>\n");
                }
                else if (govde.StartsWith("ekran:", StringComparison.Ordinal))
                {
                    Kapat();
                    var rota = govde[6..].Trim();
                    if (sonBaslik is null)
                        throw new InvalidOperationException($"satır {no}: ekran işareti ({rota}) hiçbir başlığın altında değil.");
                    var capa = KilavuzCapasi.Ekran(rota);
                    if (!kimlikler.Add(capa))
                        throw new InvalidOperationException($"satır {no}: '{rota}' için çıpa '{capa}' İKİNCİ kez üretiliyor.");
                    ekranlar.Add(new EkranIsareti(rota, sonBaslik, capa, no));
                    icerik.Append("      <span class=\"ekran-capa\" id=\"").Append(capa).Append("\"></span>\n");
                }
                else if (t == HaricIsareti)
                {
                    Kapat();
                    HaricGom(icerik, haricMd);
                }
                continue;
            }

            // ── H1: başlık + kapak ───────────────────────────────────────────
            if (s.StartsWith("# ", StringComparison.Ordinal))
            {
                if (baslik is not null) throw new InvalidOperationException($"satır {no}: ikinci '# ' başlığı.");
                baslik = s[2..].Trim();
                continue;
            }
            if (baslik is not null && !kapakBitti && !bolumAcik)
            {
                if (s.Length == 0) { if (kapakMetni.Length > 0) kapakBitti = true; continue; }
                if (!s.StartsWith('#')) { kapakMetni.Append(kapakMetni.Length > 0 ? " " : "").Append(s.Trim()); continue; }
                kapakBitti = true;
            }

            // ── H2: bölüm ────────────────────────────────────────────────────
            if (s.StartsWith("## ", StringComparison.Ordinal))
            {
                Kapat();
                var (metin, capa) = BaslikAyir(s[3..], no);
                if (capa is null) throw new InvalidOperationException($"satır {no}: '## {metin}' bölümünün çıpası yok ({{#capa}} zorunlu).");
                if (!kimlikler.Add(capa)) throw new InvalidOperationException($"satır {no}: '{capa}' çıpası İKİNCİ kez kullanılıyor.");
                if (bolumAcik) icerik.Append("    </section>\n\n");
                bolumNo++;
                icerik.Append("    <section id=\"").Append(capa).Append("\">\n")
                      .Append("      <h2><span class=\"no\">").Append(bolumNo).Append("</span> ").Append(Satir(metin)).Append("</h2>\n");
                toc.Append("      <a href=\"#").Append(capa).Append("\">").Append(Satir(metin)).Append("</a>\n");
                capalar.Add(capa);
                bolumAcik = true;
                sonBaslik = metin;
                continue;
            }

            // ── H3: alt başlık ───────────────────────────────────────────────
            if (s.StartsWith("### ", StringComparison.Ordinal))
            {
                Kapat();
                if (!bolumAcik) throw new InvalidOperationException($"satır {no}: '###' başlığı bir '##' bölümünün dışında.");
                var (metin, capa) = BaslikAyir(s[4..], no);
                if (capa is not null)
                {
                    if (!kimlikler.Add(capa)) throw new InvalidOperationException($"satır {no}: '{capa}' çıpası İKİNCİ kez kullanılıyor.");
                    icerik.Append("      <h3 id=\"").Append(capa).Append("\">").Append(Satir(metin)).Append("</h3>\n");
                    toc.Append("      <a class=\"alt\" href=\"#").Append(capa).Append("\">— ").Append(Satir(metin)).Append("</a>\n");
                    capalar.Add(capa);
                }
                else
                {
                    icerik.Append("      <h3>").Append(Satir(metin)).Append("</h3>\n");
                }
                sonBaslik = metin;
                continue;
            }

            if (s.Length == 0) { Kapat(); continue; }
            if (!bolumAcik) throw new InvalidOperationException($"satır {no}: metin hiçbir '##' bölümünün içinde değil.");

            // ── Kutu (> [!TÜR]) ─────────────────────────────────────────────
            if (s.StartsWith('>'))
            {
                var govde = s.Length > 1 ? s[1..].TrimStart() : "";
                if (kutu.Count == 0 && kutuTuru is null)
                {
                    Kapat();
                    var m = KutuTuruDeseni().Match(govde);
                    if (!m.Success) throw new InvalidOperationException($"satır {no}: kutu '> [!IPUCU|BILGI|UYARI]' ile başlamalı.");
                    kutuTuru = m.Groups[1].Value switch { "IPUCU" => "ipucu", "BILGI" => "bilgi", _ => "uyari" };
                    govde = govde[m.Length..].TrimStart();
                }
                kutu.Add(govde);
                continue;
            }

            // ── Tablo ────────────────────────────────────────────────────────
            if (s.StartsWith('|'))
            {
                if (tablo.Count == 0) Kapat();
                tablo.Add(s);
                continue;
            }

            // ── Listeler ─────────────────────────────────────────────────────
            var madde = MaddeDeseni().Match(s);
            if (madde.Success)
            {
                var sirali = char.IsDigit(madde.Groups[1].Value[0]);
                if (paragraf.Count > 0 || (liste.Count > 0 && sirali != listeSirali)) Kapat();
                listeSirali = sirali;
                liste.Add(madde.Groups[2].Value.Trim());
                continue;
            }
            if (liste.Count > 0 && ham.StartsWith("  ", StringComparison.Ordinal))
            {
                liste[^1] += " " + s.Trim();
                continue;
            }

            // ── Paragraf ─────────────────────────────────────────────────────
            if (liste.Count > 0 || tablo.Count > 0) Kapat();
            paragraf.Add(s.Trim());
        }
        Kapat();
        if (bolumAcik) icerik.Append("    </section>\n");
        if (baslik is null) throw new InvalidOperationException("kaynakta '# ' başlığı yok.");

        var kapak = new StringBuilder()
            .Append("    <div class=\"kapak\">\n      <h1>").Append(Kacir(baslik)).Append("</h1>\n");
        if (kapakMetni.Length > 0) kapak.Append("      <p>").Append(Satir(kapakMetni.ToString())).Append("</p>\n");
        kapak.Append("    </div>\n");

        var html = sablon.Replace("\r\n", "\n")
            .Replace("{{BASLIK}}", Kacir(baslik))
            .Replace("{{DAMGA}}", Damga(kaynakSha, haricSha, sablonSha))
            .Replace("{{URETICI}}", Uretici)
            .Replace("{{ICINDEKILER}}", toc.ToString().TrimEnd('\n'))
            .Replace("{{KAPAK}}", kapak.ToString().TrimEnd('\n'))
            .Replace("{{ICERIK}}", icerik.ToString().TrimEnd('\n'));
        if (!html.EndsWith('\n')) html += "\n";
        return new Sonuc(html, ekranlar, capalar);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static (string Metin, string? Capa) BaslikAyir(string s, int no)
    {
        var m = CapaDeseni().Match(s);
        if (!m.Success) return (s.Trim(), null);
        var capa = m.Groups[1].Value;
        if (!GecerliCapaDeseni().IsMatch(capa))
            throw new InvalidOperationException($"satır {no}: çıpa '{capa}' yalnız küçük harf, rakam ve '-' içerebilir.");
        if (capa.StartsWith("e-", StringComparison.Ordinal))
            throw new InvalidOperationException($"satır {no}: 'e-' öneki ekran çıpalarına ayrılmıştır ('{capa}').");
        return (s[..m.Index].Trim(), capa);
    }

    /// <summary>HTML kaçırma: &amp; &lt; &gt; &quot; — kaynaktaki işaretleme metin olarak kalır.</summary>
    public static string Kacir(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>Satır içi biçim: önce kaçırma, sonra `kod`, [bağlantı](#çıpa), **kalın**, *eğik*.</summary>
    public static string Satir(string s)
    {
        var kodlar = new List<string>();
        var ara = KodDeseni().Replace(s, m => { kodlar.Add(m.Groups[1].Value); return $"\u0001{kodlar.Count - 1}\u0001"; });
        var e = Kacir(ara);
        e = BaglantiDeseni().Replace(e, m =>
        {
            var hedef = m.Groups[2].Value;
            if (!hedef.StartsWith('#'))
                throw new InvalidOperationException($"kılavuzda yalnız iç bağlantı (#çıpa) kullanılır: '{hedef}'.");
            return $"<a class=\"ic\" href=\"{hedef}\">{m.Groups[1].Value}</a>";
        });
        e = KalinDeseni().Replace(e, "<strong>$1</strong>");
        e = EgikDeseni().Replace(e, "<em>$1</em>");
        return KodYeriDeseni().Replace(e, m => $"<code>{Kacir(kodlar[int.Parse(m.Groups[1].Value)])}</code>");
    }

    private static void TabloYaz(StringBuilder b, List<string> satirlar)
    {
        static string[] Hucreler(string s) => s.Trim().Trim('|').Split('|').Select(h => h.Trim()).ToArray();
        var baslik = Hucreler(satirlar[0]);
        b.Append("      <div class=\"tablo-sar\"><table class=\"tablo\">\n        <thead><tr>");
        foreach (var h in baslik) b.Append("<th>").Append(Satir(h)).Append("</th>");
        b.Append("</tr></thead>\n        <tbody>\n");
        foreach (var s in satirlar.Skip(1))
        {
            if (AyiriciDeseni().IsMatch(s)) continue;
            b.Append("          <tr>");
            foreach (var h in Hucreler(s)) b.Append("<td>").Append(Satir(h)).Append("</td>");
            b.Append("</tr>\n");
        }
        b.Append("        </tbody>\n      </table></div>\n");
    }

    /// <summary>
    /// BK-26: docs/HARIC-LISTESI.md § 1 tablosunun "Kalem" ve "Durum" sütunları
    /// GÖMÜLÜR — kılavuz kendi kısıt cümlesini YAZMAZ. Kanıt sütunu (depo yolu)
    /// son kullanıcıya gösterilmez.
    /// </summary>
    public static IReadOnlyList<(string Kalem, string Durum)> HaricSatirlari(string haricMd)
    {
        var satirlar = haricMd.Replace("\r\n", "\n").Split('\n');
        var bas = Array.FindIndex(satirlar, s => s.StartsWith("## 1.", StringComparison.Ordinal));
        if (bas < 0) throw new InvalidOperationException("HARIC-LISTESI.md: '## 1.' bölümü bulunamadı.");
        var sonuc = new List<(string, string)>();
        var tabloBasladi = false;
        for (var i = bas + 1; i < satirlar.Length; i++)
        {
            var s = satirlar[i].Trim();
            if (s.StartsWith("## ", StringComparison.Ordinal)) break;
            if (!s.StartsWith('|')) { if (tabloBasladi) break; continue; }
            if (!tabloBasladi) { tabloBasladi = true; continue; }          // başlık satırı
            if (AyiriciDeseni().IsMatch(s)) continue;
            var h = s.Trim('|').Split('|').Select(x => x.Trim()).ToArray();
            if (h.Length < 3) continue;
            sonuc.Add((h[1], h[2]));
        }
        if (sonuc.Count == 0) throw new InvalidOperationException("HARIC-LISTESI.md § 1 tablosu boş.");
        return sonuc;
    }

    private static void HaricGom(StringBuilder b, string haricMd)
    {
        var satirlar = new List<string> { "| Kalem | Bu sürümde |", "|---|---|" };
        satirlar.AddRange(HaricSatirlari(haricMd).Select(x => $"| {x.Kalem} | {x.Durum} |"));
        TabloYaz(b, satirlar);
    }

    [GeneratedRegex(@"\{#([^}]*)\}\s*$")] private static partial Regex CapaDeseni();
    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*$")] private static partial Regex GecerliCapaDeseni();
    [GeneratedRegex(@"^\[!(IPUCU|BILGI|UYARI)\]")] private static partial Regex KutuTuruDeseni();
    [GeneratedRegex(@"^(-|\d+\.)\s+(.*)$")] private static partial Regex MaddeDeseni();
    [GeneratedRegex(@"`([^`]+)`")] private static partial Regex KodDeseni();
    [GeneratedRegex("\u0001(\\d+)\u0001")] private static partial Regex KodYeriDeseni();
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)\s]+)\)")] private static partial Regex BaglantiDeseni();
    [GeneratedRegex(@"\*\*(.+?)\*\*")] private static partial Regex KalinDeseni();
    [GeneratedRegex(@"(?<![\*\w])\*(?!\s)(.+?)(?<!\s)\*(?![\*\w])")] private static partial Regex EgikDeseni();
    [GeneratedRegex(@"^\|?\s*:?-{3,}")] private static partial Regex AyiriciDeseni();
}
