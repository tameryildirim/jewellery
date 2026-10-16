using Kuyumcu.Api.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-1 (+F-6) — ÜRETİM AÇILIŞ KAPISI [saf testler]
//
// Sınanan davranış: üretimde ÖRNEK/VARSAYILAN sırla, örnek parolayla ya da TLS
// beyanı olmadan uygulama HİÇ AÇILMAZ (fail-closed). Bunlar saf testlerdir:
// ne veritabanı ne HTTP sunucusu gerekir, ne de süreç ortam değişkenleri
// değiştirilir (VeritabaniAdi testi hariç; o try/finally ile geri alır).
//
// NEDEN ÖNEMLİ: kuruldaki KRİTİK bulgu, her müşteride AYNI JWT sırrının
// kullanılabilmesiydi — bir kurulumdan alınan token diğerinde geçerli olurdu.
// ═════════════════════════════════════════════════════════════════════════════
public class UretimAcilisDenetimiTestleri
{
    /// <summary>Geliştirme ortamının GERÇEK sırları teste kopyalanmaz; yalnız kara listedeki yer tutucu kullanılır.</summary>
    private const string SablonJwtSirri = "DEGISTIR-en-az-32-karakter-uretim-secreti";

    private const string GucluSir = "0f8c1b2d3e4a5968770a1b2c3d4e5f60718293a4b5c6d7e8";
    private const string GucluParola = "K7-uretim-parolasi-9x2Qv";

    private sealed class SahteOrtam : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Kuyumcu.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }

    private static IConfiguration Yapilandirma(
        string? jwt = GucluSir,
        string? parola = GucluParola,
        string? vt = "kuyumcu_uretim",
        bool tersVekil = true,
        string? urls = null,
        string? yedekAnahtari = null,
        string? alanAnahtari = null)
    {
        var d = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = jwt,
            ["ConnectionStrings:Kuyumcu"] =
                $"server=localhost;port=3306;database={vt};user=root;password={parola}",
            [UretimAcilisDenetimi.TersVekilAnahtari] = tersVekil ? "true" : "false",
        };
        if (urls is not null) d["Urls"] = urls;
        if (yedekAnahtari is not null) d[UretimAcilisDenetimi.YedekAnahtari] = yedekAnahtari;
        if (alanAnahtari is not null) d[AlanSifreleme.YapilandirmaAnahtari] = alanAnahtari;
        return new ConfigurationBuilder().AddInMemoryCollection(d).Build();
    }

    private static InvalidOperationException Durdu(IConfiguration y, IHostEnvironment? o = null) =>
        Assert.Throws<InvalidOperationException>(
            () => UretimAcilisDenetimi.Dogrula(y, o ?? new SahteOrtam()));

    // ── FAIL-CLOSED 1: JWT sırrı ─────────────────────────────────────────────

    [Fact]
    public void VarsayilanJwtSirriUretimdeAcilisiDurdurur()
    {
        var hata = Durdu(Yapilandirma(jwt: SablonJwtSirri));
        Assert.Contains("Jwt:Secret", hata.Message);
    }

    [Fact]
    public void KisaJwtSirriUretimdeAcilisiDurdurur()
    {
        // 31 bayt: sınırın BİR ALTI. Sınırın kendisi (32) geçmelidir.
        var otuzBir = new string('x', UretimAcilisDenetimi.AsgariSirBayt - 1);
        var hata = Durdu(Yapilandirma(jwt: otuzBir));
        Assert.Contains("kısa", hata.Message);

        var otuzIki = new string('x', UretimAcilisDenetimi.AsgariSirBayt);
        UretimAcilisDenetimi.Dogrula(Yapilandirma(jwt: otuzIki), new SahteOrtam());
    }

    [Fact]
    public void GelistirmeJwtSirriUretimdeAcilisiDurdurur()
    {
        // Geliştirme sırrının DEĞERİ teste yazılmaz; dosyadan okunur ve
        // denetçinin parmak izi listesiyle eşleştiği KANITLANIR.
        var devSir = GelistirmeAyari("Jwt:Secret");
        Assert.True(UretimAcilisDenetimi.KaraListede(devSir),
            "appsettings.Development.json'daki Jwt:Secret kara listede DEĞİL — " +
            "geliştirme sırrı üretimde kullanılabilirdi.");

        var hata = Durdu(Yapilandirma(jwt: devSir));
        Assert.Contains("Jwt:Secret", hata.Message);
    }

    // ── FAIL-CLOSED 2: veritabanı parolası ───────────────────────────────────

    [Fact]
    public void DegistirParolasiUretimdeAcilisiDurdurur()
    {
        var hata = Durdu(Yapilandirma(parola: "DEGISTIR"));
        Assert.Contains("parola", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GelistirmeParolasiUretimdeAcilisiDurdurur()
    {
        var devParola = GelistirmeBaglantiParolasi();
        Assert.True(UretimAcilisDenetimi.KaraListede(devParola),
            "Geliştirme MySQL parolası kara listede DEĞİL.");

        var hata = Durdu(Yapilandirma(parola: devParola));
        Assert.Contains("parola", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KaraListeTamDizgeKarsilastirir_MesruSirKilitlenmez()
    {
        // Risk notu: müşterinin sırrı tesadüfen kara listedeki bir dizgeyi
        // İÇERİYORSA kilitlenmemeli. Kural alt dizge eşleşmesi DEĞİLDİR.
        var devSir = GelistirmeAyari("Jwt:Secret");
        var icerenAmaAyniOlmayan = devSir + "-musteriye-ozgu-ek";
        Assert.False(UretimAcilisDenetimi.KaraListede(icerenAmaAyniOlmayan));
        UretimAcilisDenetimi.Dogrula(Yapilandirma(jwt: icerenAmaAyniOlmayan), new SahteOrtam());
    }

    // ── FAIL-CLOSED 4: TLS ───────────────────────────────────────────────────

    [Fact]
    public void TlsBeyaniYokkenUretimdeAcilisDurur()
    {
        var hata = Durdu(Yapilandirma(tersVekil: false, urls: "http://0.0.0.0:5000"));
        Assert.Contains("TLS", hata.Message);
    }

    [Fact]
    public void TersVekilBeyaniVarkenAcilisGecer() =>
        UretimAcilisDenetimi.Dogrula(Yapilandirma(tersVekil: true), new SahteOrtam());

    [Fact]
    public void KestrelHttpsUcNoktasiTlsBeyaniSayilir() =>
        UretimAcilisDenetimi.Dogrula(
            Yapilandirma(tersVekil: false, urls: "https://0.0.0.0:5001"), new SahteOrtam());

    // ── Geçen yol ve sır sızıntısı ───────────────────────────────────────────

    [Fact]
    public void GucluSirlarlaAcilisGecer() =>
        UretimAcilisDenetimi.Dogrula(Yapilandirma(), new SahteOrtam());

    [Fact]
    public void HataMesajiSirDegeriniIcermez()
    {
        // Denetçi ihlali bildirirken DEĞERİ yazarsa, o değer systemd/journald
        // günlüğüne ve konteyner loguna düşer — kapının tüm kazancı kaybolur.
        var devSir = GelistirmeAyari("Jwt:Secret");
        var devParola = GelistirmeBaglantiParolasi();

        foreach (var hata in new[]
                 {
                     Durdu(Yapilandirma(jwt: devSir)),
                     Durdu(Yapilandirma(jwt: SablonJwtSirri)),
                     Durdu(Yapilandirma(parola: devParola)),
                     Durdu(Yapilandirma(jwt: new string('x', 8))),
                 })
        {
            Assert.DoesNotContain(devSir, hata.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(devParola, hata.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(SablonJwtSirri, hata.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("xxxxxxxx", hata.Message, StringComparison.Ordinal);
        }
    }

    // ── Ortam farkı ──────────────────────────────────────────────────────────

    [Fact]
    public void GelistirmeOrtamindaDevBaglantisiKabulEdilir()
    {
        // 971+ testin HTTP bacağı bu kuralla ayakta duruyor: Development/Testing
        // ortamında kara liste kuralları UYGULANMAZ.
        var gelistirme = new SahteOrtam { EnvironmentName = Environments.Development };
        UretimAcilisDenetimi.Dogrula(
            Yapilandirma(jwt: SablonJwtSirri, parola: "DEGISTIR", tersVekil: false), gelistirme);

        var test = new SahteOrtam { EnvironmentName = "Testing" };
        UretimAcilisDenetimi.Dogrula(
            Yapilandirma(jwt: SablonJwtSirri, parola: "DEGISTIR", tersVekil: false), test);
    }

    [Fact]
    public void DenetimiZorlaAnahtariGelistirmedeDeAyniKuraliUygular()
    {
        // "Development'ta test edilen davranış Production'da farklılaşmasın"
        // (Mehmet koşulu): kural GÖRÜNMEZ değildir, tek anahtarla zorlanır.
        var d = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = SablonJwtSirri,
            ["ConnectionStrings:Kuyumcu"] = "server=localhost;database=x;user=root;password=abc123xyz",
            [UretimAcilisDenetimi.ZorlaAnahtari] = "true",
            [UretimAcilisDenetimi.TersVekilAnahtari] = "true",
        };
        var y = new ConfigurationBuilder().AddInMemoryCollection(d).Build();
        Assert.Throws<InvalidOperationException>(
            () => UretimAcilisDenetimi.Dogrula(
                y, new SahteOrtam { EnvironmentName = Environments.Development }));
    }

    // ── Sır disiplini: iki anahtar ayrı ──────────────────────────────────────

    [Fact]
    public void YedekAnahtariAlanSifrelemeAnahtariylaAyniOlamaz()
    {
        const string anahtar = "ZGV2LWFuYWh0YXItMzItYmF5dC1heW5pLW9sYW1heg==";
        var hata = Durdu(Yapilandirma(yedekAnahtari: anahtar, alanAnahtari: anahtar));
        Assert.Contains(UretimAcilisDenetimi.YedekAnahtari, hata.Message);

        // Farklıysa geçer.
        UretimAcilisDenetimi.Dogrula(
            Yapilandirma(yedekAnahtari: anahtar, alanAnahtari: "YmFza2EtYW5haHRhci0zMi1iYXl0LWF5cmk="),
            new SahteOrtam());
    }

    // ── Tek kaynaktan veritabanı adı ─────────────────────────────────────────

    [Fact]
    public void VeritabaniAdiCelisisiAcilisiDurdurur()
    {
        var onceki = Environment.GetEnvironmentVariable(
            UretimAcilisDenetimi.VeritabaniAdiOrtamDegiskeni);
        try
        {
            Environment.SetEnvironmentVariable(
                UretimAcilisDenetimi.VeritabaniAdiOrtamDegiskeni, "kuyumcu_net");
            var hata = Durdu(Yapilandirma(vt: "kuyumcu"));
            Assert.Contains(UretimAcilisDenetimi.VeritabaniAdiOrtamDegiskeni, hata.Message);

            // Aynı ad → geçer (büyük/küçük harf farkı sorun değildir).
            UretimAcilisDenetimi.Dogrula(Yapilandirma(vt: "KUYUMCU_NET"), new SahteOrtam());
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                UretimAcilisDenetimi.VeritabaniAdiOrtamDegiskeni, onceki);
        }
    }

    // ── Yardımcılar: geliştirme sırları KODA KOPYALANMAZ, dosyadan okunur ────

    private static string GelistirmeAyariDosyasi()
    {
        var kok = YayinDosyaSozlesmesiTestleri.ProjeKoku();
        return Path.Combine(kok, "dotnet", "Kuyumcu.Api", "appsettings.Development.json");
    }

    private static string GelistirmeAyari(string yol)
    {
        var y = new ConfigurationBuilder()
            .AddJsonFile(GelistirmeAyariDosyasi(), optional: false).Build();
        return y[yol] ?? throw new InvalidOperationException($"{yol} bulunamadı");
    }

    private static string GelistirmeBaglantiParolasi()
    {
        var baglanti = GelistirmeAyari("ConnectionStrings:Kuyumcu");
        var parca = baglanti.Split(';')
            .First(p => p.TrimStart().StartsWith("password=", StringComparison.OrdinalIgnoreCase));
        return parca[(parca.IndexOf('=') + 1)..];
    }
}
