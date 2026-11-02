using System.Diagnostics;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-5 · K-26) — KURULUM ADRESİ SORAR, KISAYOLU O ADRESLE ÜRETİR
//
// K-26 kararı: adres kısayola GÖMÜLMEZ, kurulumda sorulur (varsayılan
// http://localhost:5199), .env'de TEK satırda (BYGATECH_ADRES) tutulur, adres
// değişirse kısayol yeniden üretilir. Bu testler dotnet/kurulum.sh'ı GERÇEKTEN
// çalıştırır (geçici dizinde; sırlar geçici .env'e yazılır ve test sonunda
// dosya boşaltılır). kurulum.ps1 bu makinede koşturulamaz → metin sözleşmesi.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KurulumAdresKisayolTestleri : IDisposable
{
    private readonly string dizin = Path.Combine(Path.GetTempPath(), "p33-kurulum-" + Guid.NewGuid().ToString("N")[..8]);
    private string Env => Path.Combine(dizin, ".env");
    private string Masaustu => Path.Combine(dizin, "masaustu");

    public KurulumAdresKisayolTestleri() => Directory.CreateDirectory(dizin);

    public void Dispose()
    {
        // Sır taşıyan geçici dosya önce BOŞALTILIR (silme kancası bloklayabilir).
        try { if (File.Exists(Env)) File.WriteAllText(Env, ""); } catch { /* en iyi çaba */ }
        try { Directory.Delete(dizin, recursive: true); } catch { /* en iyi çaba */ }
    }

    private static string Betik() =>
        Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "kurulum.sh");

    private (int Kod, string Cikti) Kos(params string[] arg)
    {
        var bilgi = new ProcessStartInfo("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,   // etkileşimsiz: soru sorulamaz → varsayılan
            UseShellExecute = false,
        };
        bilgi.ArgumentList.Add(Betik());
        foreach (var a in arg) bilgi.ArgumentList.Add(a);
        using var p = Process.Start(bilgi)!;
        p.StandardInput.Close();
        var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(60_000);
        return (p.ExitCode, cikti);
    }

    private string KisayolMetni()
    {
        var dosyalar = Directory.GetFiles(Masaustu);
        Assert.Single(dosyalar);   // adres değişince İKİNCİ kısayol birikmez
        return File.ReadAllText(dosyalar[0]);
    }

    private string[] Satirlar(string anahtar) =>
        File.ReadAllLines(Env).Where(s => s.StartsWith(anahtar + "=", StringComparison.Ordinal)).ToArray();

    [Fact]
    public void Kurulum_verilen_ADRESI_tek_satira_ve_KISAYOLA_yazar_sirri_EKRANA_basmaz()
    {
        var (kod, cikti) = Kos("--hedef", Env, "--adres", "http://localhost:5321",
            "--kisayol-dizin", Masaustu, "--sahip", "p33-olmayan-kullanici");
        Assert.True(kod == 0, cikti);

        Assert.Equal(["BYGATECH_ADRES=http://localhost:5321"], Satirlar("BYGATECH_ADRES"));
        // Adres bu makineyi gösteriyor → uygulama AYNI portu dinler.
        Assert.Equal(["ASPNETCORE_URLS=http://127.0.0.1:5321"], Satirlar("ASPNETCORE_URLS"));
        // ETKİN anahtar "Urls": appsettings.Production.json'daki "Urls"u yalnız
        // o ezer (P-33 yerel kap tatbikatında ölçüldü).
        Assert.Equal(["Urls=http://127.0.0.1:5321"], Satirlar("Urls"));
        Assert.Contains("http://localhost:5321", KisayolMetni());

        foreach (var anahtar in new[] { "Jwt__Secret", "MYSQL_ROOT_PASSWORD", "KUYUMCU_SIFRELEME_ANAHTARI", "Yedek__SifrelemeAnahtari" })
        {
            var deger = Satirlar(anahtar).Single()[(anahtar.Length + 1)..];
            Assert.True(deger.Length >= 16);
            Assert.DoesNotContain(deger, cikti, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Etkilesimsiz_kurulumda_adres_VARSAYILANA_duser_ve_bunu_SOYLER()
    {
        var (kod, cikti) = Kos("--hedef", Env, "--kisayol-dizin", Masaustu, "--sahip", "p33-olmayan-kullanici");
        Assert.True(kod == 0, cikti);
        Assert.Equal(["BYGATECH_ADRES=http://localhost:5199"], Satirlar("BYGATECH_ADRES"));
        Assert.Contains("varsayılan", cikti, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5199", KisayolMetni());
    }

    [Fact]
    public void Adres_degisince_KISAYOL_yeniden_uretilir_SATIR_guncellenir_sirlar_DEGISMEZ()
    {
        Assert.Equal(0, Kos("--hedef", Env, "--adres", "http://localhost:5321",
            "--kisayol-dizin", Masaustu, "--sahip", "p33-olmayan-kullanici").Kod);
        var jwtOnce = Satirlar("Jwt__Secret").Single();

        var (kod, cikti) = Kos("--hedef", Env, "--kisayol", "--adres", "https://dukkan.ornek",
            "--kisayol-dizin", Masaustu);
        Assert.True(kod == 0, cikti);

        Assert.Equal(["BYGATECH_ADRES=https://dukkan.ornek"], Satirlar("BYGATECH_ADRES"));
        // Başka ana makine → ters vekil arkasındaki iç bağlama noktası.
        Assert.Equal(["ASPNETCORE_URLS=http://127.0.0.1:5000"], Satirlar("ASPNETCORE_URLS"));
        Assert.Equal(["Urls=http://127.0.0.1:5000"], Satirlar("Urls"));
        var kisayol = KisayolMetni();
        Assert.Contains("https://dukkan.ornek", kisayol);
        Assert.DoesNotContain("5321", kisayol);
        Assert.Equal(jwtOnce, Satirlar("Jwt__Secret").Single());
    }

    [Fact]
    public void Kisayol_modu_adres_verilmezse_KAYITLI_satiri_kullanir()
    {
        Assert.Equal(0, Kos("--hedef", Env, "--adres", "http://localhost:5322",
            "--kisayol-dizin", Masaustu, "--sahip", "p33-olmayan-kullanici").Kod);
        var (kod, cikti) = Kos("--hedef", Env, "--kisayol", "--kisayol-dizin", Masaustu);
        Assert.True(kod == 0, cikti);
        Assert.Contains("http://localhost:5322", KisayolMetni());
        Assert.DoesNotContain("varsayılan", cikti, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://localhost:5199/panel")]
    [InlineData("localhost:5199")]
    public void Gecersiz_adres_REDDEDILIR_hicbir_dosya_yazilmaz(string adres)
    {
        var (kod, cikti) = Kos("--hedef", Env, "--adres", adres, "--kisayol-dizin", Masaustu,
            "--sahip", "p33-olmayan-kullanici");
        Assert.Equal(2, kod);
        Assert.Contains("geçersiz adres", cikti, StringComparison.Ordinal);
        Assert.False(File.Exists(Env));
        Assert.False(Directory.Exists(Masaustu));
    }

    [Fact]
    public void Windows_kurulumu_AYNI_sozlesmeyi_tasir()
    {
        var ps = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "kurulum.ps1"));
        Assert.Contains("[string]$Adres = ''", ps, StringComparison.Ordinal);
        Assert.Contains("$VarsayilanAdres = 'http://localhost:5199'", ps, StringComparison.Ordinal);
        Assert.Contains("\"BYGATECH_ADRES=$Adres\"", ps, StringComparison.Ordinal);
        Assert.Contains("[InternetShortcut]", ps, StringComparison.Ordinal);
        Assert.Contains("[switch]$SadeceKisayol", ps, StringComparison.Ordinal);
        Assert.Contains("\"Urls=$(Bag-Adresi $Adres)\"", ps, StringComparison.Ordinal);
        Assert.DoesNotContain("'ASPNETCORE_URLS=http://127.0.0.1:5000'", ps, StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-5) — TEMİZ KURULUM TATBİKATININ BULDUĞU KUSUR: belgelerdeki
// "./dotnet/yedek-al-ve-dogrula.sh" çağrısı (depo kökünden) kitaplığı
// dotnet/dotnet/yedek-ortak.sh'ta arıyor ve betik HİÇ çalışmıyordu. Betik
// hangi dizinden çağrılırsa çağrılsın kendi kitaplığını bulmalı; ortam eksikse
// "kitaplık yok" değil, fail-closed "KUYUMCU_DB_SIFRE tanımlı değil" demeli.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YedekBetigiCagriTestleri
{
    [Theory]
    [InlineData("depo")]
    [InlineData("dotnet")]
    public void Yedek_betigi_HER_cagri_dizininden_kitapligini_bulur(string nereden)
    {
        var dotnet = KilavuzUretimTestleri.DotnetKoku();
        var bilgi = new ProcessStartInfo("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = nereden == "depo" ? Directory.GetParent(dotnet)!.FullName : dotnet,
        };
        bilgi.ArgumentList.Add(nereden == "depo" ? "./dotnet/yedek-al-ve-dogrula.sh" : "./yedek-al-ve-dogrula.sh");
        bilgi.ArgumentList.Add("tam");
        // Sır YOK: betik veritabanına hiç dokunmadan fail-closed durmalı.
        bilgi.Environment.Remove("KUYUMCU_DB_SIFRE");
        using var p = Process.Start(bilgi)!;
        var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(30_000);

        Assert.DoesNotContain("No such file", cikti, StringComparison.Ordinal);
        Assert.DoesNotContain("command not found", cikti, StringComparison.Ordinal);
        Assert.Contains("KUYUMCU_DB_SIFRE tanımlı değil", cikti, StringComparison.Ordinal);
        Assert.NotEqual(0, p.ExitCode);
    }
}
