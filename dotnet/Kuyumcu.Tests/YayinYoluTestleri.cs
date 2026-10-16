using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-42 — YAYIN DİZİNİ TEK KAYNAK YOL KAPISI
//
// ÖLÇÜLMÜŞ ARIZA (QA turu 8 triyajı, 23 Eylül 2026 — orkestratör ölçümü):
//   • Yayın anlık görüntüsü OTURUMA ÖZGÜ bir /private/tmp scratchpad dizininde
//     yaşıyordu. Oturum bitince o dizin yok olur → kurucunun 5199 inceleme
//     ekranı bir daha AÇILMAZ (qa-sunucu.sh `cd "$YAYIN"` → exit 4).
//   • Aynı yol BEŞ dosyaya elle kopyalanmıştı: yayinla.sh, qa-sunucu.sh,
//     yayin-butunluk.sh, nobetci.sh (×2 — SON-DURUM satırı ve 5199 kurtarması).
//     Biri güncellenip öteki unutulunca "sunucu başka dizinden koşar, envanter
//     başka dizini doğrular" sessiz tutarsızlığı doğar.
//   • Sonuç: tur 8'de grup 1'in 6 rotası (/cikti, /kasalar, /sayim, /banka,
//     /pos/tahsilatlar, /yetkiler) HİÇ açılamadı ve 1/1 puanlandı; tur 8
//     ortalamaları (3,17/2,74) karşılaştırılabilir DEĞİL.
//
// NE SINAR: düzeltmeyi değil MEKANİZMAYI. Yol tek kaynakta (betikler/yayin-yolu.sh)
// tanımlıdır; tüketen betiklerde ikinci bir yol literali YOKTUR; .claude/launch.json
// yayın yolunu sabitlemez, sürümlü betiğe devreder.
//
// ⚠ Bu testler derleme çıktısını değil KAYNAK AĞACI okur — kabuk betikleri
//   hiç derlenmez, dolayısıyla derleyici bu sınıfı koruyamaz.
// KARDEŞ KABUK TESTLERİ (dotnet test kabuk koşturamaz):
//   betikler/testler/yayin-butunluk-testleri.sh
//     ::test_yayin_sonrasi_salt_okunur · ::test_ikinci_yayin_kosumu_basarili
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YayinYoluTestleri
{
    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;

    private static string BetiklerKoku() =>
        Path.Combine(DepoKoku(), "dokumanlar", "09-uygulama-kayitlari", "betikler");

    private static string Oku(string goreliYol)
    {
        var tam = Path.Combine(BetiklerKoku(), goreliYol);
        Assert.True(File.Exists(tam), $"betik bulunamadı: {tam}");
        return File.ReadAllText(tam);
    }

    /// <summary>Yayın dizinini tüketen sürümlü betikler.</summary>
    private static readonly string[] Tuketiciler =
    {
        "yayinla.sh", "qa-sunucu.sh", "nobetci.sh", "yayin-butunluk.sh", "kapi.sh"
    };

    // ── 1) Betiklerde oturuma özgü yayın yolu kalmadı ────────────────────────
    // Arızanın ta kendisi: `.../<oturum-kimliği>/scratchpad/qa-yayin`. Oturum
    // kimliği 36 karakterlik bir UUID'dir; onu ARAMAK yerine deseni ararız ki
    // yarın başka bir oturumda yazılan yol da yakalansın.
    [Fact]
    public void Betiklerde_OturumaOzguYayinYoluKalmadi()
    {
        var ihlaller = new List<string>();

        foreach (var betik in Tuketiciler)
        {
            var metin = Oku(betik);
            var satirlar = metin.Split('\n');
            for (var i = 0; i < satirlar.Length; i++)
            {
                var s = satirlar[i];
                var kod = s.TrimStart();
                if (kod.StartsWith("#")) continue;          // yorum satırı: gerekçe metni serbest
                if (!s.Contains("scratchpad")) continue;
                if (!s.Contains("qa-yayin") && !s.Contains("/private/tmp")) continue;
                // SIR dizini geriye-uyum satırı bu kapının konusu DEĞİL (K-5'in işi)
                if (s.Contains("SIR_GECICI")) continue;
                ihlaller.Add($"{betik}:{i + 1}: {s.Trim()}");
            }
        }

        Assert.True(ihlaller.Count == 0,
            "Yayın dizini için oturuma özgü yol KALDI (oturum bitince 5199 açılmaz):\n"
            + string.Join("\n", ihlaller));
    }

    // ── 2) Nöbetçi yayın yolunu TEK KAYNAKTAN okur ───────────────────────────
    // nobetci.sh iki ayrı yerde yayın dizinine bakar: SON-DURUM.txt'teki
    // "Ekranında (5199) gördüğün sürüm" satırı ve 5199 düştüğünde kurtarma.
    // İkisi de yayin-yolu.sh'ten gelmelidir; aksi hâlde nöbetçi kurucuya YANLIŞ
    // sürüm tarihi yazar (tur 8'de yazdı).
    [Fact]
    public void Nobetci_YayinYolunuTekKaynaktanOkur()
    {
        var kaynak = Path.Combine(BetiklerKoku(), "yayin-yolu.sh");
        Assert.True(File.Exists(kaynak), "tek kaynak yol betiği yok: yayin-yolu.sh");
        var kaynakMetni = File.ReadAllText(kaynak);
        Assert.Contains("KUYUMCU_YAYIN_DIZINI", kaynakMetni);
        Assert.Contains(".kuyumcu-yayin", kaynakMetni);   // kalıcı varsayılan

        var nobetci = Oku("nobetci.sh");
        Assert.Contains("yayin-yolu.sh", nobetci);
        Assert.Contains("$YAYIN_DIZINI/Kuyumcu.Api.dll", nobetci);   // SON-DURUM satırı
        Assert.Contains("YAYIN=\"$YAYIN_DIZINI\"", nobetci);          // 5199 kurtarması

        // Tüketicilerin HEPSİ aynı kaynağı source eder (kapı, yalnız nöbetçiyi değil
        // hattın tamamını kapatır — beş kopya yolun dersi).
        foreach (var betik in Tuketiciler)
            Assert.True(Oku(betik).Contains("yayin-yolu.sh"),
                $"{betik} yayın yolunu tek kaynaktan okumuyor");
    }

    // ── 3) launch.json yayın yolunu SABİTLEMEZ ───────────────────────────────
    // JSON bir kabuk betiğini source edemez; bu yüzden yolu KOPYALAMAK yerine
    // sürümlü betiğe DEVREDER. Kopyalanan yol, tur 8'de olduğu gibi sessizce ayrışır.
    [Fact]
    public void LaunchJson_YayinYoluSabitDegil()
    {
        var yol = Path.Combine(DepoKoku(), ".claude", "launch.json");
        Assert.True(File.Exists(yol), $"launch.json yok: {yol}");
        var ham = File.ReadAllText(yol);

        using var belge = JsonDocument.Parse(ham);   // bozuk JSON = önizleme hiç açılmaz
        var yapilandirmalar = belge.RootElement.GetProperty("configurations");

        JsonElement? qa = null;
        foreach (var y in yapilandirmalar.EnumerateArray())
            if (y.TryGetProperty("name", out var ad) && ad.GetString() == "kuyumcu-qa")
                qa = y;
        Assert.True(qa is not null, "launch.json'da 'kuyumcu-qa' yapılandırması yok");

        // (a) Yayın dizini yolu JSON'a YAZILMAMIŞ olmalı.
        foreach (var yasak in new[] { "qa-yayin", ".kuyumcu-yayin", "/private/tmp" })
            Assert.False(ham.Contains(yasak),
                $"launch.json yayın yolunu sabitlemiş ('{yasak}') — tek kaynak yayin-yolu.sh olmalı");

        // (b) Bunun yerine sürümlü betiğe devretmiş olmalı.
        Assert.Equal("dokumanlar/09-uygulama-kayitlari/betikler/yayin-yolu.sh",
            qa!.Value.GetProperty("yayinYolu").GetString());
        var betik = qa.Value.GetProperty("betik").GetString();
        Assert.NotNull(betik);
        Assert.EndsWith("betikler/qa-sunucu.sh", betik);
        Assert.True(File.Exists(betik), $"launch.json'daki betik diskte yok: {betik}");
    }
}
