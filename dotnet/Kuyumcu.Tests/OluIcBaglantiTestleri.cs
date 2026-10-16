using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-D1 — ÖLÜ İÇ BAĞLANTI KAPISI (kimlikler D-b · D-c)
//
// ÖLÇÜLMÜŞ ARIZA (23 Eylül 2026, orkestratör doğruladı):
//   Pages/Panel.razor → href="/islem-defteri" id="panel-defter-git".
//   Böyle bir @page YOKTU; gerçeği "/raporlar/islem-defteri". Uygulamadaki
//   TEK ölü iç bağlantıydı ve cumartesi müşteri demosunun 2. ekranındaydı —
//   tıklanınca "Sayfa bulunamadı" çizerdi.
//   Aynı sınıf ikinci hata: betikler/demo-tohumla.js prova tablosunda
//   rota: '/vade-takibi' (gerçeği '/raporlar/vade-takibi'); 'yol' alanı (API
//   ucu) doğru olduğu için prova YEŞİL dönüyor, rapora yanlış rota adı giriyordu.
//
// NEDEN BU TESTLER VAR: iki arıza da tek tek DÜZELTİLEBİLİRDİ, ama düzeltme
// mekanizma değildir — yarın yeni bir sayfa yeniden yanlış adrese bağlanır.
// Mevcut kapıların hiçbiri bu soruyu sormuyordu: P02-1 rota SAYISINI ölçüyor,
// P-09 rota↔yetki EŞLEŞMESİNİ ölçüyor; "bu bağlantı bir yere gidiyor mu"
// sorusu ölçüsüzdü. Burası o boşluğu kapatır ve denetim.sh'taki P02-1b
// sayacının SÖKÜLMEDİĞİNİ de doğrular (sayacın sayacı).
//
// ⚠ Bu testler derleme çıktısını değil KAYNAK AĞACI okur; .razor derlenmez,
//   betikler hiç derlenmez.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class OluIcBaglantiTestleri
{
    // ── Kaynak ağacı yardımcıları (OlcumKapisiTestleri ile aynı yöntem) ─────

    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;

    private static string WebDizin(string alt) =>
        Path.Combine(DotnetKoku(), "Kuyumcu.Web", alt);

    /// <summary>Bağlantı taramasının kapsamı: ekranlar, bileşenler, yerleşim, istemci servisleri.</summary>
    private static readonly string[] TarananKlasorler = ["Pages", "Bilesenler", "Layout", "Servisler"];

    private static IEnumerable<string> KaynakDosyalari() =>
        TarananKlasorler
            .Select(WebDizin)
            .Where(Directory.Exists)
            .SelectMany(k => Directory.GetFiles(k, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".razor", StringComparison.Ordinal)
                     || f.EndsWith(".cs", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>Tip kısıtını soyar, ad korunur: {Id:int} → {Id}.</summary>
    private static string SablonSadelestir(string r) =>
        Regex.Replace(r, @"\{([A-Za-z]+)(:[a-z]+)?\}", "{$1}");

    /// <summary>Uygulamanın gerçek rota kümesi — tek kaynak @page direktifleridir.</summary>
    internal static HashSet<string> Rotalar()
    {
        var küme = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dosya in Directory.GetFiles(WebDizin("Pages"), "*.razor", SearchOption.AllDirectories))
            foreach (var satır in File.ReadAllLines(dosya))
            {
                var m = Regex.Match(satır, @"^@page\s+""([^""]+)""");
                if (m.Success) küme.Add(SablonSadelestir(m.Groups[1].Value));
            }
        return küme;
    }

    /// <summary>
    /// Düz metin iç bağlantı hedefleri: href="/…" ve NavigateTo("/…").
    /// <para>Değişken/enterpolasyonlu hedefler (href="@(...)") statik olarak
    /// çözülemez; bu kapının iddiası DÜZ METİN bağlantılarla sınırlıdır ve bu
    /// sınır burada açıkça yazılıdır (Anayasa md.1: kapsam gizlenmez).</para>
    /// </summary>
    internal static List<(string Hedef, string Dosya)> IcBaglantilar()
    {
        var liste = new List<(string, string)>();
        foreach (var dosya in KaynakDosyalari())
        {
            var metin = File.ReadAllText(dosya);
            foreach (Match m in Regex.Matches(metin, @"href=""(/[^""]*)"""))
                liste.Add((m.Groups[1].Value, dosya));
            foreach (Match m in Regex.Matches(metin, @"NavigateTo\(""(/[^""]*)"""))
                liste.Add((m.Groups[1].Value, dosya));
        }
        return liste;
    }

    /// <summary>
    /// Rota olarak sınanabilir mi? Sorgu/çapa soyulur; uzantılı hedef STATİK
    /// VARLIKTIR (/kilavuz.html, /img/ikon.svg#…), rota değildir — varlığının
    /// olup olmadığı ayrı bir konudur ve bu kapının iddiası değildir.
    /// </summary>
    private static string? RotayaIndirge(string hedef)
    {
        var yol = Regex.Replace(hedef, @"[?#].*$", "");
        if (yol.Length == 0) return null;
        var sonParça = yol[(yol.LastIndexOf('/') + 1)..];
        if (Regex.IsMatch(sonParça, @"\.[A-Za-z0-9]+$")) return null;   // statik varlık
        return SablonSadelestir(yol);
    }

    /// <summary>Hedef bir rota şablonuyla örtüşüyor mu ({Id} her parçayı karşılar).</summary>
    private static bool RotayaUyar(string hedef, IEnumerable<string> rotalar)
    {
        var h = hedef.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var r in rotalar)
        {
            var p = r.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length != h.Length) continue;
            var uyar = true;
            for (var i = 0; i < p.Length && uyar; i++)
                uyar = p[i].StartsWith('{')
                    || string.Equals(p[i], h[i], StringComparison.OrdinalIgnoreCase);
            if (uyar) return true;
        }
        return false;
    }

    // ── 1) ASIL KAPI: OLU_IC_BAGLANTI = 0 ───────────────────────────────────

    /// <summary>
    /// Her düz metin iç bağlantı hedefi @page kümesinde OLMALIDIR.
    /// Bu test kırmızıysa kullanıcı bir düğmeye basıp "Sayfa bulunamadı" görür.
    /// </summary>
    [Fact]
    public void Hicbir_ic_baglanti_OLU_degildir()
    {
        var rotalar = Rotalar();
        Assert.True(rotalar.Count >= 40, $"@page ölçümü boş/eksik döndü ({rotalar.Count}) — kapı oyunlanmış.");

        var ölüler = IcBaglantilar()
            .Select(b => (Rota: RotayaIndirge(b.Hedef), b.Hedef, b.Dosya))
            .Where(b => b.Rota is not null)
            .Where(b => !RotayaUyar(b.Rota!, rotalar))
            .Select(b => $"{b.Hedef}  ←  {Path.GetFileName(b.Dosya)}")
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.True(ölüler.Count == 0,
            "ÖLÜ İÇ BAĞLANTI (hedefin @page karşılığı yok):\n  " + string.Join("\n  ", ölüler));
    }

    /// <summary>
    /// Tarama gerçekten bir şey ölçüyor mu? Sayaç sıfıra düşerse "0 ölü bağlantı"
    /// iddiası boş kümeden gelir ve kapı temenniye döner (fail-open).
    /// </summary>
    [Fact]
    public void Baglanti_taramasi_BOS_donmez()
    {
        var sayı = IcBaglantilar().Select(b => RotayaIndirge(b.Hedef)).Count(r => r is not null);
        Assert.True(sayı >= 15, $"iç bağlantı taraması {sayı} sonuç döndü — ölçüm oyunlanmış.");
    }

    // ── 2) D-b geri dönüş testi: Panel'in "Son işlemler" bağlantısı ─────────

    /// <summary>
    /// Demonun 2. ekranındaki bağlantı. Adres tek tek de kilitlenir: genel kapı
    /// "bir yere gidiyor" der, bu test "DOĞRU yere gidiyor" der.
    /// </summary>
    [Fact]
    public void Panel_son_islemler_baglantisi_islem_defteri_rotasina_gider()
    {
        var metin = File.ReadAllText(Path.Combine(WebDizin("Pages"), "Panel.razor"));
        var m = Regex.Match(metin, @"href=""([^""]+)""\s+id=""panel-defter-git""");
        Assert.True(m.Success, "Panel.razor'da panel-defter-git bağlantısı bulunamadı.");
        Assert.Equal("/raporlar/islem-defteri", m.Groups[1].Value);
        Assert.Contains("/raporlar/islem-defteri", Rotalar());
    }

    // ── 3) Mekanizma sökülmedi mi? (sayacın sayacı) ─────────────────────────

    [Fact]
    public void Denetim_betiginde_OLU_IC_BAGLANTI_sayaci_durur()
    {
        var denetim = File.ReadAllText(Path.Combine(DotnetKoku(), "denetim.sh"));
        Assert.Contains("OLU_IC_BAGLANTI", denetim);
        Assert.Contains("P02_OLU_N", denetim);
        Assert.Contains("NavigateTo", denetim);
    }

    // ── 4) D-c: demo provası rota etiketleri tek kaynaktan doğrulanır ───────

    private static string TohumBetigi() => File.ReadAllText(Path.Combine(
        DepoKoku(), "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js"));

    [Fact]
    public void Demo_prova_tablosunda_OLU_rota_etiketi_yoktur()
    {
        var rotalar = Rotalar();
        var etiketler = Regex.Matches(TohumBetigi(), @"\{\s*no:\s*\d+,\s*rota:\s*'([^']+)'")
            .Select(m => m.Groups[1].Value)
            .ToList();

        Assert.True(etiketler.Count >= 10, $"prova tablosu {etiketler.Count} adım döndürdü — ölçüm oyunlanmış.");
        var ölü = etiketler.Where(r => !RotayaUyar(SablonSadelestir(r), rotalar)).ToList();
        Assert.True(ölü.Count == 0, "demo-tohumla.js prova tablosunda ÖLÜ ROTA etiketi: " + string.Join(", ", ölü));
    }

    /// <summary>
    /// Etiketi bugün düzeltmek yetmez: betik bir dahaki sefere kendini
    /// doğrulamalı. Kapı sökülürse bu test kırmızı olur.
    /// </summary>
    [Fact]
    public void Demo_betigi_prova_rotalarini_page_listesine_karsi_dogrular()
    {
        var betik = TohumBetigi();
        Assert.Contains("uygulamaRotalari", betik);
        Assert.Contains("@page", betik);
        // Hem tanım hem ÇAĞRI olmalı: yalnız tanım kalırsa kapı hiç işlemez.
        Assert.True(Regex.Matches(betik, @"provaRotalariniDogrula").Count >= 2,
            "provaRotalariniDogrula tanımlı ama ÇAĞRILMIYOR — kapı fail-open.");
    }
}
