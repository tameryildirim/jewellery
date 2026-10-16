using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-41 — YAYIN ÇIKTI ENVANTERİ [saf testler, gerçek betik çağrılır]
//
// ÖLÇÜLEN ARIZA (tur 8, 23.09.2026): kaynakta `Kuyumcu.Web/wwwroot/kilavuz.html`
// 60.885 bayt VAR, yayın anlık görüntüsünde YOK → canlı `/kilavuz` = `404 0`.
// Yayın "başarılı" publish'ten çıkmıştı ve ağaç damgası tutuyordu; damga
// çıktının TAM olduğunu sormaz. Ölçüm aracı bozuk olduğu için grup 1'in 6
// rotası hiç açılamadı ve 1/1 puanlandı.
//
// Bu testler `betikler/yayin-butunluk.sh --uret` betiğini GERÇEKTEN çalıştırır
// (metin denetimi DEĞİL) ve envanterin kapsamını kanıtlar. Fikstür yayın dizini
// sentetiktir ama wwwroot KÖK dosyaları GERÇEK kaynaktan kopyalanır — yani
// "kilavuz.html envantere giriyor mu" sorusu gerçek dosya üzerinde ölçülür.
//
// 5199'a DOKUNULMAZ: yalnız --uret çağrılır, hiçbir sunucu açılmaz.
// ═════════════════════════════════════════════════════════════════════════════
public class YayinEnvanterTestleri
{
    private static string ProjeKoku() => YayinDosyaSozlesmesiTestleri.ProjeKoku();

    private static string BetikYolu() =>
        Path.Combine(ProjeKoku(), "dokumanlar", "09-uygulama-kayitlari", "betikler", "yayin-butunluk.sh");

    private static string KaynakWwwroot() =>
        Path.Combine(ProjeKoku(), "dotnet", "Kuyumcu.Web", "wwwroot");

    /// <summary>Sentetik yayın dizini: 66 boot varlığı + GERÇEK wwwroot kök dosyaları.</summary>
    private static string FiksturKur(out string[] bootVarliklari)
    {
        var d = Path.Combine(Path.GetTempPath(), "p41-envanter-" + Guid.NewGuid().ToString("N"));
        var fw = Path.Combine(d, "wwwroot", "_framework");
        Directory.CreateDirectory(fw);
        File.WriteAllText(Path.Combine(d, "Kuyumcu.Api.dll"), "SAHTE-KUYUMCU-API-IKILISI");

        foreach (var kaynak in Directory.GetFiles(KaynakWwwroot()))
            File.Copy(kaynak, Path.Combine(d, "wwwroot", Path.GetFileName(kaynak)), overwrite: true);

        var assembly = new Dictionary<string, string>();
        for (var i = 0; i < 60; i++) assembly["Sahte.Derleme" + i.ToString("00") + ".wasm"] = "sha256-P41TEST";
        var icu = new Dictionary<string, string>
        {
            ["icudt_CJK.dat"] = "sha256-P41TEST",
            ["icudt_EFIGS.dat"] = "sha256-P41TEST",
            ["icudt_no_CJK.dat"] = "sha256-P41TEST",
        };
        var wasmNative = new Dictionary<string, string> { ["dotnet.native.wasm"] = "sha256-P41TEST" };
        var jsNative = new Dictionary<string, string> { ["dotnet.native.8.0.0.test.js"] = "sha256-P41TEST" };
        var jsRuntime = new Dictionary<string, string> { ["dotnet.runtime.8.0.0.test.js"] = "sha256-P41TEST" };

        bootVarliklari = assembly.Keys
            .Concat(icu.Keys).Concat(wasmNative.Keys)
            .Concat(jsNative.Keys).Concat(jsRuntime.Keys).ToArray();

        foreach (var ad in bootVarliklari)
            File.WriteAllText(Path.Combine(fw, ad), "VARLIK:" + ad);
        File.WriteAllText(Path.Combine(fw, "dotnet.js"), "// sahte dotnet.js yukleyicisi");

        var boot = new
        {
            mainAssemblyName = "Kuyumcu.Web",
            resources = new
            {
                assembly,
                icu,
                wasmNative,
                jsModuleNative = jsNative,
                jsModuleRuntime = jsRuntime,
            },
        };
        File.WriteAllText(Path.Combine(fw, "blazor.boot.json"),
            JsonSerializer.Serialize(boot, new JsonSerializerOptions { WriteIndented = true }));
        return d;
    }

    /// <summary>Betiği gerçekten koşar; envanter satırlarını döndürür.</summary>
    private static string[] EnvanterUret(string yayinDizini)
    {
        var psi = new ProcessStartInfo("/bin/zsh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = ProjeKoku(),
        };
        psi.ArgumentList.Add(BetikYolu());
        psi.ArgumentList.Add("--uret");
        psi.ArgumentList.Add(yayinDizini);

        using var p = Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        Assert.True(p.WaitForExit(120_000), "yayin-butunluk.sh --uret 120 sn'de bitmedi.");
        Assert.True(p.ExitCode == 0, $"yayin-butunluk.sh --uret çıkış {p.ExitCode}:\n{cikti}");

        var envanter = Path.Combine(yayinDizini, ".yayin-envanteri");
        Assert.True(File.Exists(envanter), $"envanter üretilmedi: {envanter}\n{cikti}");
        return File.ReadAllLines(envanter, Encoding.UTF8)
            .Where(s => s.Trim().Length > 0)
            .ToArray();
    }

    /// <summary>Satır biçimi: "&lt;sha256&gt; &lt;yol&gt; &lt;bayt&gt;" → yol alanı.</summary>
    private static HashSet<string> Yollar(IEnumerable<string> satirlar) =>
        satirlar.Select(s => s.Split(' ')).Where(p => p.Length == 3).Select(p => p[1]).ToHashSet();

    [Fact]
    public void Envanter_TumWwwrootKokDosyalariniIcerir()
    {
        var d = FiksturKur(out _);
        var yollar = Yollar(EnvanterUret(d));

        // KAYNAKTA duran her wwwroot kök dosyası envanterde OLMAK ZORUNDA.
        // Kök taraması kaldırılırsa bu test kırmızıya döner (mutasyon M-41-2).
        foreach (var kaynak in Directory.GetFiles(KaynakWwwroot()))
        {
            var beklenen = "wwwroot/" + Path.GetFileName(kaynak);
            Assert.True(yollar.Contains(beklenen),
                $"envanterde yok: {beklenen} — yayın bu dosyayı servis etmeden geçebilir.");
        }

        // Sunucu ikilisi de envanterin parçasıdır.
        Assert.Contains("Kuyumcu.Api.dll", yollar);
    }

    [Fact]
    public void Envanter_BootJsonVarliklariniIcerir()
    {
        var d = FiksturKur(out var bootVarliklari);
        var satirlar = EnvanterUret(d);
        var yollar = Yollar(satirlar);

        foreach (var ad in bootVarliklari)
            Assert.True(yollar.Contains("wwwroot/_framework/" + ad),
                $"boot varlığı envanterde yok: {ad}");

        // blazor.boot.json kendi kaynak listesinde YOKTUR; ayrıca eklenmeli,
        // yoksa manifestin kendisi sessizce değiştirilebilir (tur 8'de tam bu oldu).
        Assert.Contains("wwwroot/_framework/blazor.boot.json", yollar);

        // Kabul koşulu: varlık sayısı ≥ 66 (60 assembly + 3 icu + 1 wasmNative + 2 jsModule).
        var varlikSayisi = yollar.Count(y => y.StartsWith("wwwroot/_framework/", StringComparison.Ordinal));
        Assert.True(varlikSayisi >= 66, $"_framework varlık sayısı {varlikSayisi} < 66");

        // Her satır "sha256 yol bayt" biçiminde ve sha 64 onaltılık karakter.
        foreach (var s in satirlar)
        {
            var p = s.Split(' ');
            Assert.True(p.Length == 3, $"bozuk envanter satırı: {s}");
            Assert.True(p[0].Length == 64 && p[0].All(Uri.IsHexDigit), $"sha256 değil: {p[0]}");
            Assert.True(long.TryParse(p[2], out var b) && b >= 0, $"bayt alanı sayı değil: {p[2]}");
        }
    }

    [Fact]
    public void Envanter_KilavuzHtmlSatiriniIcerir()
    {
        // Tur 8'in tam arızası: kaynakta var, yayında yok, kimse görmedi.
        var kilavuz = Path.Combine(KaynakWwwroot(), "kilavuz.html");
        Assert.True(File.Exists(kilavuz),
            "Kaynakta kilavuz.html YOK — /kilavuz rotasının kaynağı kaybolmuş demektir.");

        var d = FiksturKur(out _);
        var satirlar = EnvanterUret(d);

        var satir = satirlar.FirstOrDefault(s => s.Contains(" wwwroot/kilavuz.html ", StringComparison.Ordinal));
        Assert.True(satir is not null, "envanterde 'wwwroot/kilavuz.html' satırı YOK.");

        // Boyut gerçek dosyayla birebir aynı olmalı (0 baytlık kabuk geçmemeli).
        var beklenenBayt = new FileInfo(kilavuz).Length;
        var parcalar = satir!.Split(' ');
        Assert.Equal(beklenenBayt.ToString(), parcalar[2]);
        Assert.True(beklenenBayt > 0, "kilavuz.html 0 bayt.");
    }
}
