using System.Text.RegularExpressions;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-D2 — ÜRÜN ADI KAPISI (demo hazırlık dalgası · Z3 / V17 / D-d)
//
// ÖLÇÜLMÜŞ DURUM (23 Eylül 2026, orkestratör): kaynak ağacında eski ürün adı ("Kuyumcu" + boşluk + "ERP")
// 44 dosyada geçiyordu, "Mihenk" 0 dosyada. Kurucu kararı (23 Eylül, birebir):
// "ürünün adı mihenk olacak". Cumartesi demosunda müşteri ürünün adını tarayıcı
// sekmesinde, açılış ekranında ve giriş kartında GÖRÜR.
//
// NEDEN TEST: adı elle 44 yerde değiştirmek düzeltmedir, mekanizma değildir —
// yarın yeni bir sayfa yeniden eski adı yazar. Bu dosya adı TEK KAYNAĞA
// (Kuyumcu.Core/I18n/Sozlukler.Marka.cs → Marka.Ad) bağlar ve eski adın geri
// sızmasını kırmızıya çevirir. denetim.sh P02-1d sayacının sökülmediğini de
// doğrular (sayacın sayacı).
//
// SINIR (bilinçli, rapor § "Dokunulmayanlar"): YASAL BELGE metinleri ve FİRMA
// UNVANI (ByGatech A.Ş.) bu kapının konusu değildir; yalnız YAZILIMIN ADI.
//
// ⚠ Bu testler derleme çıktısını değil KAYNAK AĞACI okur.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MarkaAdiTestleri
{
    /// <summary>Ölü marka — kaynak ağacında hiçbir yerde geçmemeli.</summary>
    // ⚠ Parça parça kuruluyor: bu dosyanın KENDİSİ taranan ağaçta olduğu için
    //   dizge düz yazılsaydı test kendini bulur ve daima kırmızı olurdu.
    private static readonly string EskiUrunAdi = string.Concat("Kuyumcu", " ", "ERP");

    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;

    /// <summary>Taranan uzantılar: kullanıcıya metin taşıyabilen her kaynak türü.</summary>
    private static readonly string[] Uzantilar =
        [".cs", ".razor", ".html", ".css", ".json", ".sh", ".ps1", ".js"];

    /// <summary>
    /// ELENENLER ve GEREKÇELERİ:
    ///  · bin/obj  → derleme çıktısı, kaynak değil.
    ///  · yayin/   → yayın anlık görüntüsü; kaynaktan YENİDEN ÜRETİLİR (Z1).
    ///    Kaynak temizse bir sonraki yayın da temiz doğar; burayı saymak
    ///    "eski çıktı duruyor" diye kaynağı kırmızı gösterirdi.
    /// </summary>
    private static bool Elendi(string yol) =>
        yol.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || yol.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || yol.Contains($"{Path.DirectorySeparatorChar}yayin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static IEnumerable<string> KaynakDosyalari() =>
        Directory.GetFiles(DotnetKoku(), "*.*", SearchOption.AllDirectories)
            .Where(f => Uzantilar.Contains(Path.GetExtension(f)))
            .Where(f => !Elendi(f));

    private static string Oku(params string[] parcalar) =>
        File.ReadAllText(Path.Combine([DotnetKoku(), .. parcalar]));

    // ── 1) Kaynak ağacında eski ad kalmadı ──────────────────────────────────

    [Fact]
    public void Kaynak_agacinda_eski_urun_adi_kalmadi()
    {
        var dosyalar = KaynakDosyalari().ToList();

        // Sayaç oyunlanmasın: tarama gerçekten çalışmış olmalı.
        Assert.True(dosyalar.Count > 100, $"tarama BOŞ döndü ({dosyalar.Count} dosya) — sayaç oyunlanmış");

        var kirli = dosyalar
            .Where(f => File.ReadAllText(f).Contains(EskiUrunAdi, StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(DotnetKoku(), f))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.True(kirli.Count == 0,
            $"ESKI_URUN_ADI ≠ 0 — '{EskiUrunAdi}' hâlâ geçiyor: {string.Join(", ", kirli)}");
    }

    // ── 2) Yeni ad gerçekten ekranda ────────────────────────────────────────

    [Fact]
    public void Acilis_ekrani_sekme_basligini_ve_yukleniyor_metnini_yeni_adla_yazar()
    {
        var index = Oku("Kuyumcu.Web", "wwwroot", "index.html");
        Assert.Contains($"<title>{Marka.Ad}</title>", index, StringComparison.Ordinal);
        Assert.Contains(Marka.YukleniyorMetni, index, StringComparison.Ordinal);
    }

    [Fact]
    public void Kilavuz_basligi_tek_kaynakla_ayni()
    {
        var kilavuz = Oku("Kuyumcu.Web", "wwwroot", "kilavuz.html");
        Assert.Contains($"<title>{Marka.KilavuzBasligi}</title>", kilavuz, StringComparison.Ordinal);
    }

    [Fact]
    public void Sozluk_uygulama_adi_iki_dilde_de_tek_kaynaktan_gelir()
    {
        Assert.Equal(Marka.Ad, Sozlukler.Getir("tr").Auth.UygulamaAdi);
        // Marka adı çevrilmez: EN sözlükte de aynı ad durur (kurucu kararı).
        Assert.Equal(Marka.Ad, Sozlukler.Getir("en").Auth.UygulamaAdi);
    }

    // ── 3) Mekanizma: başlık tek kalıptan üretilir ──────────────────────────

    /// <summary>
    /// BEYAZ LİSTE — gerekçesiyle. YASAL BELGE ÇIKTI sayfasıdır: gider pusulası
    /// belgesi tarayıcıdan basılır ve birçok tarayıcı sekme başlığını basılan
    /// kâğıdın üst bilgisine yazar. Yazılımın adının yasal belgenin üstüne
    /// düşmesi istenmez (P-D2 SINIR kuralı: yasal belge şablonlarında yalnız
    /// firma unvanı ve yasal metin bulunur). Bu sayfa markasız kalır —
    /// başlığında eski ürün adı da zaten hiç geçmiyordu (ölçüldü).
    /// </summary>
    private static readonly HashSet<string> Muaf = new(StringComparer.Ordinal)
    {
        "PusulaBelgesi.razor",
    };

    [Fact]
    public void Her_sayfa_basligi_marka_tek_kaynagindan_uretilir()
    {
        var sayfalar = Directory
            .GetFiles(Path.Combine(DotnetKoku(), "Kuyumcu.Web", "Pages"), "*.razor", SearchOption.AllDirectories)
            .Where(f => !Elendi(f))
            .ToList();

        Assert.True(sayfalar.Count > 20, $"tarama BOŞ döndü ({sayfalar.Count} sayfa) — sayaç oyunlanmış");

        var bulunanBasliklar = 0;
        var kacak = new List<string>();
        var muafBulundu = 0;

        foreach (var s in sayfalar)
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(s), @"<PageTitle>(.*?)</PageTitle>", RegexOptions.Singleline))
            {
                bulunanBasliklar++;
                if (Muaf.Contains(Path.GetFileName(s))) { muafBulundu++; continue; }
                if (!m.Groups[1].Value.Contains("Marka.", StringComparison.Ordinal))
                    kacak.Add($"{Path.GetFileName(s)}: {m.Groups[1].Value.Trim()}");
            }
        }

        Assert.True(bulunanBasliklar > 20, $"PageTitle taraması BOŞ döndü ({bulunanBasliklar}) — sayaç oyunlanmış");
        Assert.True(kacak.Count == 0,
            $"sayfa başlığı marka tek kaynağını kullanmıyor: {string.Join(" | ", kacak)}");
        // Muafiyet listesi ÖLÜ kalmasın: listelenen sayfa gerçekten var olmalı.
        Assert.Equal(Muaf.Count, muafBulundu);
    }

    [Fact]
    public void Sayfa_basligi_kalibi_ad_ve_ayraci_dogru_kurar()
    {
        Assert.Equal($"Kasa{Marka.Ayrac}{Marka.Ad}", Marka.SayfaBasligi("Kasa"));
        // Sayfa adı yoksa boş ayraç basılmaz.
        Assert.Equal(Marka.Ad, Marka.SayfaBasligi(null));
        Assert.Equal(Marka.Ad, Marka.SayfaBasligi("   "));
    }

    [Fact]
    public void Acilis_betikleri_yeni_adi_tasir()
    {
        // Müşteri programı masaüstü kısayolundan açar (DEMO-SENARYOSU ADIM 1) ve
        // konsol penceresinde ürün adını GÖRÜR. Ad orada ASCII'dir: .bat konsol
        // kod sayfası (cp857/850) "İ" harfini bozuk çizer.
        foreach (var betik in new[] { "start.bat", "stop.bat" })
        {
            var yol = Path.Combine(DepoKoku(), betik);
            Assert.True(File.Exists(yol), $"{betik} YOK");
            var icerik = File.ReadAllText(yol);
            Assert.DoesNotContain(EskiUrunAdi, icerik, StringComparison.Ordinal);
            Assert.Contains(Marka.AdAscii, icerik, StringComparison.Ordinal);
        }
    }

    // ── 4) Sayacın sayacı: denetim.sh kapısı sökülmedi ──────────────────────

    [Fact]
    public void Denetim_betigi_eski_urun_adi_sayacini_tasiyor()
    {
        var denetim = File.ReadAllText(Path.Combine(DepoKoku(), "dotnet", "denetim.sh"));

        // Yalnız "dizge geçiyor mu" yetmez (ölçüldü: M1-4 mutasyonu sayacın ATAMASINI
        // bozdu, test yine YEŞİL kaldı). Kapının BLOĞU sökülüp sökülmediği ölçülür.
        var blok = Regex.Match(denetim, @"# ── P02-1d\).*?(?=# ── P02-1c\))", RegexOptions.Singleline);
        Assert.True(blok.Success, "denetim.sh'ta P02-1d (ESKI_URUN_ADI) bloğu YOK — kapı sökülmüş");
        var b = blok.Value;

        Assert.Contains("ESKI_URUN_ADI", b, StringComparison.Ordinal);
        // Sayaç gerçekten ATANIYOR mu (ad değişirse kapı sessizce ölür).
        Assert.Matches(new Regex(@"^P02_MARKA_N=", RegexOptions.Multiline), b);
        // ve KARAR veriyor mu: sıfır dalı + kırık dalı + boş-tarama (oyunlama) dalı.
        Assert.Matches(new Regex(@"\$\{P02_MARKA_N:-\d+\}""?\s+-eq\s+0"), b);
        Assert.Contains("p02_ok", b, StringComparison.Ordinal);
        Assert.Contains("p02_kir", b, StringComparison.Ordinal);
        Assert.Contains("P02_MARKA_TARANAN", b, StringComparison.Ordinal);
        // Yayın anlık görüntüsü kapsam DIŞI olmalı (kaynaktan yeniden üretilir).
        Assert.Contains("yayin", b, StringComparison.Ordinal);
        // Tek kaynak dosyası kapıda adıyla anılır.
        Assert.Contains("Sozlukler.Marka.cs", b, StringComparison.Ordinal);
        // Kaynak ağacın dışındaki iki açılış betiği de kapsamda olmalı.
        Assert.Contains("start.bat", b, StringComparison.Ordinal);
        Assert.Contains("stop.bat", b, StringComparison.Ordinal);
    }
}
