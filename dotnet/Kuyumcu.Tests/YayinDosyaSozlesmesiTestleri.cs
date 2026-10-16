using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-1 (+F-6) — YAYIN ARTEFAKTLARININ SÖZLEŞMESİ [saf testler]
//
// Bu dosyalar KODA DERLENMEZ, bu yüzden bir hata ancak ÜRETİM MAKİNESİNDE
// ortaya çıkardı: 3306'nın dışa açık kalması, şablona sır düşmesi, nginx'in
// 80'de uygulamayı sunmaya devam etmesi, systemd biriminin Development ile
// başlaması... Hepsi geri dönüşü pahalı hatalar. Buradaki testler o dosyaları
// metin olarak sınar ve geri dönüşü YAKALAR.
// ═════════════════════════════════════════════════════════════════════════════
public class YayinDosyaSozlesmesiTestleri
{
    /// <summary>Test ikilisinden yukarı çıkarak proje kökünü bulur (mutlak yol GÖMÜLMEZ).</summary>
    internal static string ProjeKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "docker-compose.yml")))
            d = d.Parent;
        return d?.FullName
            ?? throw new InvalidOperationException("Proje kökü bulunamadı (docker-compose.yml yok).");
    }

    private static string Oku(params string[] parcalar) =>
        File.ReadAllText(Path.Combine(ProjeKoku(), Path.Combine(parcalar)));

    /// <summary>Yorum satırlarını atar: "buraya X yazmayın" açıklaması ihlal değildir.</summary>
    private static IEnumerable<string> KodSatirlari(string metin, string yorumOneki) =>
        metin.Split('\n')
            .Select(s => s.TrimEnd('\r'))
            .Where(s => !s.TrimStart().StartsWith(yorumOneki, StringComparison.Ordinal));

    // ── appsettings.Production ───────────────────────────────────────────────

    [Fact]
    public void ProductionSablonuHicbirSirAnahtariIcermez()
    {
        var metin = Oku("dotnet", "Kuyumcu.Api", "appsettings.Production.json");

        // Sır ANAHTARLARI şablonda hiç bulunmaz: değer boş bırakmak bile
        // "buraya sır yazılır" davetidir; ortam değişkeni TEK yoldur.
        foreach (var yasak in new[] { "\"Secret\"", "\"Anahtar\"", "\"ConnectionStrings\"", "password=" })
            Assert.DoesNotContain(yasak, metin, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("DEGISTIR-en-az-32", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSablonuVeOrnegiBirebirAynidir()
    {
        // İkisi ayrışırsa kuran kişi hangisinin doğru olduğunu bilemez.
        Assert.Equal(
            Oku("dotnet", "Kuyumcu.Api", "appsettings.Production.json"),
            Oku("dotnet", "Kuyumcu.Api", "appsettings.Production.json.ornek"));
    }

    [Fact]
    public void ProductionSablonuGunlukSeviyesiniYapilandirir()
    {
        // K14 (mühür 4) — kalıcı günlük asgarisi. Üretimde Information gürültüsü
        // diske yazılmaz; EF komut günlüğü (SQL + parametre) kapalıdır.
        var metin = Oku("dotnet", "Kuyumcu.Api", "appsettings.Production.json");
        Assert.Contains("\"Logging\"", metin);
        Assert.Contains("Microsoft.EntityFrameworkCore.Database.Command", metin);
        Assert.DoesNotContain("\"Default\": \"Information\"", metin);
    }

    // ── docker-compose.uretim.yml ────────────────────────────────────────────

    [Fact]
    public void ComposeUretimDosyasi3306PortuYayimlamaz()
    {
        var metin = Oku("docker-compose.uretim.yml");
        var kod = KodSatirlari(metin, "#").ToList();

        Assert.DoesNotContain(kod, s => Regex.IsMatch(s, @"3306\s*:\s*3306"));
        // MySQL servisinde `ports` anahtarı HİÇ olmamalı; yalnız `expose`.
        Assert.Contains(kod, s => s.TrimStart().StartsWith("expose:", StringComparison.Ordinal));
    }

    [Fact]
    public void ComposeUretimDosyasiSirGommez()
    {
        var kod = KodSatirlari(Oku("docker-compose.uretim.yml"), "#").ToList();

        // Geliştirme compose'undaki düz metin parola buraya SIZMAMALI.
        var gelistirmeParola = Regex.Match(
            Oku("docker-compose.yml"), @"MYSQL_ROOT_PASSWORD:\s*(\S+)").Groups[1].Value;
        Assert.NotEqual(string.Empty, gelistirmeParola);
        Assert.DoesNotContain(kod, s => s.Contains(gelistirmeParola, StringComparison.Ordinal));

        // Her sır ${...} ile .env'den gelir ve EKSİKSE compose ÇÖKER (`:?`).
        foreach (var anahtar in new[] { "MYSQL_ROOT_PASSWORD", "Jwt__Secret", "ConnectionStrings__Kuyumcu" })
            Assert.Contains(kod, s => s.Contains("${" + anahtar, StringComparison.Ordinal));
    }

    [Fact]
    public void ComposeUretimDosyasiKaynakLimitiVeLogRotasyonuIcerir()
    {
        var metin = Oku("docker-compose.uretim.yml");
        Assert.Contains("limits:", metin);
        Assert.Contains("cpus:", metin);
        Assert.Contains("memory:", metin);
        // K14 — json-file rotasyonu (sınırsız büyüyen günlük diski doldurur).
        Assert.Contains("driver: json-file", metin);
        Assert.Contains("max-size:", metin);
        Assert.Contains("max-file:", metin);
    }

    [Fact]
    public void ComposeUretimSaglikKontroluParolayiKomutSatirindaTasimaz()
    {
        // `mysqladmin ping -pPAROLA` parolayı konteynerin `ps` çıktısına düşürür.
        var metin = Oku("docker-compose.uretim.yml");
        // `mysqladmin ping -pPAROLA` biçimi YASAK; parola MYSQL_PWD ile verilir.
        Assert.DoesNotMatch(@"mysqladmin[^\n]*-p\S", metin);
        Assert.Contains("MYSQL_PWD", metin);
    }

    // ── nginx ────────────────────────────────────────────────────────────────

    [Fact]
    public void NginxConf80PortundaYalnizYonlendirmeYapar()
    {
        var metin = Oku("deploy", "nginx-kuyumcu.conf");
        var kod = KodSatirlari(metin, "#").ToList();

        Assert.Contains(kod, s => s.Contains("listen 443 ssl", StringComparison.Ordinal));
        Assert.Contains(kod, s => s.Contains("return 301 https://", StringComparison.Ordinal));

        // 80 bloğunda proxy_pass OLMAMALI: TLS'siz istek uygulamaya ulaşmaz.
        var seksenBlok = metin[..metin.IndexOf("listen 443", StringComparison.Ordinal)];
        Assert.DoesNotContain("proxy_pass", seksenBlok, StringComparison.Ordinal);
    }

    [Fact]
    public void NginxConfGuvenlikBasligiKopyalamaz()
    {
        // MÜHÜR 6 / K5-K6: başlıkların TEK KAYNAĞI Program.cs. Vekilde ikinci
        // kopya = iki katmanın ayrışması (eski conf SAMEORIGIN, API DENY).
        var kod = KodSatirlari(Oku("deploy", "nginx-kuyumcu.conf"), "#");
        Assert.DoesNotContain(kod, s => Regex.IsMatch(s,
            @"add_header\s+(X-Frame-Options|X-Content-Type-Options|Content-Security-Policy|Strict-Transport-Security|Referrer-Policy)"));

        var iis = Oku("deploy", "iis-web.config");
        Assert.DoesNotContain("name=\"X-Frame-Options\"", iis, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"X-Content-Type-Options\"", iis, StringComparison.Ordinal);
    }

    [Fact]
    public void NginxConfSistemUclariniAgKatmanindaKisitlar()
    {
        // K4 (mühür 4) — /api/v1/sistem/* kiracı AÇMA uçlarıdır; anahtar
        // kullanıcıya bağlı olmadıkları için MFA onları korumaz.
        var metin = Oku("deploy", "nginx-kuyumcu.conf");
        var yer = metin.IndexOf("location /api/v1/sistem/", StringComparison.Ordinal);
        Assert.True(yer > 0, "nginx şablonunda /api/v1/sistem/ bloğu yok (K4).");

        var blok = metin[yer..(metin.IndexOf('}', yer) + 1)];
        Assert.Contains("deny all;", blok, StringComparison.Ordinal);
    }

    [Fact]
    public void NginxConfStatikSunumYapmaz()
    {
        // MÜHÜR 6 / K7: WASM istemcisi de API sürecinden sunulur; nginx yalnız
        // ters vekil. `root` ile ikinci sunum hattı açılırsa /assets/ altındaki
        // dosyalar Program.cs'in güvenlik başlıklarını ALMAZ (sessiz boşluk).
        var metin = Oku("deploy", "nginx-kuyumcu.conf");
        // TEK İSTİSNA: /.well-known/acme-challenge (F-7 sertifika doğrulaması)
        // uygulamaya değil diske bakar ve uygulama içeriği SUNMAZ.
        var uygulamaBlogu = metin[metin.IndexOf("listen 443", StringComparison.Ordinal)..];
        var kod = KodSatirlari(uygulamaBlogu, "#").ToList();
        Assert.DoesNotContain(kod, s => Regex.IsMatch(s, @"^\s*root\s+"));
        Assert.DoesNotContain(kod, s => s.Contains("try_files", StringComparison.Ordinal));
        Assert.DoesNotContain(kod, s => Regex.IsMatch(s, @"^\s*index\s+"));
        // gzip zorunlu; brotli OPSİYONEL olduğu için şablona YAZILMAZ
        // (modül yoksa `nginx -t` düşer).
        Assert.Contains(kod, s => s.Trim() == "gzip on;");
        Assert.DoesNotContain(kod, s => Regex.IsMatch(s, @"^\s*brotli"));
    }

    // ── systemd + .env örneği ────────────────────────────────────────────────

    [Fact]
    public void SystemdBirimiProductionOrtaminiSabitler()
    {
        var metin = Oku("deploy", "systemd", "kuyumcu.service");
        var kod = KodSatirlari(metin, "#").ToList();

        Assert.Contains(kod, s => s.Trim() == "Environment=ASPNETCORE_ENVIRONMENT=Production");
        Assert.DoesNotContain(kod, s => s.Contains("ASPNETCORE_ENVIRONMENT=Development", StringComparison.Ordinal));

        // FAIL-CLOSED 6: ortam satırı EnvironmentFile'dan SONRA gelmeli,
        // yoksa .env içindeki bir Development değeri onu EZER.
        var dosyaSatiri = metin.IndexOf("EnvironmentFile=", StringComparison.Ordinal);
        var ortamSatiri = metin.IndexOf("Environment=ASPNETCORE_ENVIRONMENT=Production", StringComparison.Ordinal);
        Assert.True(ortamSatiri > dosyaSatiri,
            "ASPNETCORE_ENVIRONMENT=Production satırı EnvironmentFile'dan SONRA gelmeli.");

        // K14: kalıcı günlük journald'a, ayırt edici kimlikle.
        Assert.Contains(kod, s => s.Trim() == "StandardOutput=journal");
        Assert.Contains(kod, s => s.StartsWith("SyslogIdentifier=", StringComparison.Ordinal));

        // FAIL-CLOSED 5: çıkış kodu 3 (bekleyen migration) yeniden başlatılmaz.
        Assert.Contains(kod, s => s.Trim() == "RestartPreventExitStatus=3");
        Assert.Contains(kod, s => s.Trim() == "Type=notify");
    }

    [Fact]
    public void EnvOrnegindeGercekSirYoktur()
    {
        var metin = Oku("deploy", ".env.ornek");
        var kod = KodSatirlari(metin, "#").ToList();

        // Her sır satırı DEGISTIR ile başlayan yer tutucu taşır → uygulama
        // bu dosyayla OLDUĞU GİBİ açılmaz (fail-closed kanıtı).
        foreach (var anahtar in new[] { "MYSQL_ROOT_PASSWORD", "Jwt__Secret", "KUYUMCU_SIFRELEME_ANAHTARI" })
        {
            var satir = kod.Single(s => s.StartsWith(anahtar + "=", StringComparison.Ordinal));
            Assert.Contains("DEGISTIR", satir, StringComparison.Ordinal);
        }

        // Geliştirme parolası örneğe SIZMAMALI.
        var gelistirmeParola = Regex.Match(
            Oku("docker-compose.yml"), @"MYSQL_ROOT_PASSWORD:\s*(\S+)").Groups[1].Value;
        Assert.DoesNotContain(gelistirmeParola, metin, StringComparison.Ordinal);
    }

    // ── kurulum sihirbazı: sır ekrana basılmaz ───────────────────────────────

    [Fact]
    public void KurulumBetigiUretilenSirriEkranaBasmaz()
    {
        // Aşkın koşulu: betiğin STDOUT'unda sır DEĞİŞKENİ yankılanmamalı.
        var betik = Oku("dotnet", "kurulum.sh");
        foreach (var degisken in new[] { "JWT_SIR", "DB_PAROLA", "SIFRELEME", "YEDEK_ANAHTAR" })
        {
            // Ne doğrudan yankı, ne de dosyaya yazarken ekrana düşürme.
            Assert.DoesNotContain($"echo \"${degisken}\"", betik, StringComparison.Ordinal);
            Assert.DoesNotContain($"echo ${degisken}", betik, StringComparison.Ordinal);
            Assert.DoesNotContain($"printf '%s' \"${degisken}\"", betik, StringComparison.Ordinal);
        }

        // Var olan dosya sessizce EZİLMEZ.
        Assert.Contains("--yenile", betik, StringComparison.Ordinal);
        Assert.Contains("chmod 600", betik, StringComparison.Ordinal);
    }

    // ── eski yayın yolları pasifleştirildi ───────────────────────────────────

    [Fact]
    public void EskiYayinYollariUretimYoluDegildirUyarisiTasir()
    {
        // MÜHÜR 3 "tek yayın yolu": ikiz yayın düzeni doğmaz. Dosyaların
        // SİLİNMESİ kurucuya bırakıldı; uyarı başlığı zorunludur.
        Assert.Contains("ÜRETİM YAYIN YOLU DEĞİLDİR",
            Oku("deploy", "ecosystem.config.js"), StringComparison.Ordinal);
        Assert.Contains("URETIM YAYIN YOLU DEGILDIR",
            Oku("start.bat"), StringComparison.Ordinal);
        Assert.Contains("YALNIZ GELİŞTİRME",
            Oku("docker-compose.yml"), StringComparison.Ordinal);
    }

    [Fact]
    public void GelistirmeAyarlariYayinCiktisinaKOPYALANMAZ()
    {
        // İLK GERÇEK PUBLISH PROVASINDA YAKALANDI (2026-09-17):
        // appsettings.Development.json self-contained çıktıya kopyalanıyordu —
        // geliştirme MySQL parolası ve JWT sırrı müşteri sunucusuna taşınırdı.
        var csproj = Oku("dotnet", "Kuyumcu.Api", "Kuyumcu.Api.csproj");
        Assert.Contains("appsettings.Development.json", csproj, StringComparison.Ordinal);
        Assert.Contains("CopyToPublishDirectory=\"Never\"", csproj, StringComparison.Ordinal);

        // Betik de fail-closed: dosya yine de çıkarsa yayın DURUR.
        var betik = Oku("dotnet", "yayin-al.sh");
        Assert.Contains("exit 4", betik, StringComparison.Ordinal);
    }

    [Fact]
    public void YayinCiktisiKaynakAgacinaGIRMEZ()
    {
        // 165 MB'lık self-contained çıktı bir derleme ürünüdür, kaynak değildir.
        Assert.Contains("dotnet/yayin/", Oku(".gitignore"), StringComparison.Ordinal);
    }

    [Fact]
    public void YayinBetigiSelfContainedUretirVeNoBuildKullanmaz()
    {
        var betik = Oku("dotnet", "yayin-al.sh");
        Assert.Contains("--self-contained true", betik, StringComparison.Ordinal);
        Assert.Contains("migrations bundle", betik, StringComparison.Ordinal);
        // Ortam tuzağı: `dotnet ef ... --no-build` bu projede YASAK.
        Assert.DoesNotContain(
            KodSatirlari(betik, "#"), s => s.Contains("--no-build", StringComparison.Ordinal));
    }

    // ── runbook'lar tek kökte (docs/) ────────────────────────────────────────

    [Fact]
    public void RunbooklarTekBelgeKokundeYasar()
    {
        // MÜHÜR 3 / K6: ikinci bir belge kökü açılmaz.
        var docs = Path.Combine(ProjeKoku(), "docs");
        foreach (var ad in new[]
                 {
                     "DEPLOY.md",
                     "RUNBOOK-SEMA-YUKSELTME.md",
                     "RUNBOOK-MFA-KURTARMA.md",
                     "RUNBOOK-PILOT-DAVET.md",
                 })
            Assert.True(File.Exists(Path.Combine(docs, ad)), $"docs/{ad} yok.");

        // DEPLOY.md artık NestJS/pnpm anlatmaz.
        var deploy = Oku("docs", "DEPLOY.md");
        Assert.DoesNotContain("pnpm", deploy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NestJS", deploy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SemaRunbookuOnceYedekAdiminiZorunluKilar()
    {
        var metin = Oku("docs", "RUNBOOK-SEMA-YUKSELTME.md");
        Assert.Contains("yedek-al-ve-dogrula.sh", metin, StringComparison.Ordinal);
        Assert.Contains("kuyumcu-migrate", metin, StringComparison.Ordinal);

        // Yedek ADIMI, bundle çalıştırma ADIMINDAN önce gelmeli. (Ölçü bölüm
        // başlıklarıdır; "kuyumcu-migrate" sözcüğü önkoşul bölümünde de geçer.)
        var yedekAdimi = metin.IndexOf("## 2. ÖNCE YEDEK", StringComparison.Ordinal);
        var bundleAdimi = metin.IndexOf("## 4. Bundle", StringComparison.Ordinal);
        Assert.True(yedekAdimi > 0 && bundleAdimi > 0, "Runbook adım başlıkları değişmiş.");
        Assert.True(yedekAdimi < bundleAdimi, "Runbook 'önce yedek' sırasını bozuyor.");

        // SAYISAL İDDİA YASAĞI (mühür 3): migration sayısı belgeye GÖMÜLMEZ,
        // komut çıktısından türetilir.
        Assert.DoesNotContain("31 migration", metin, StringComparison.Ordinal);
        Assert.Contains("dotnet ef migrations list", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void MfaKurtarmaRunbookuSahipKilitlenmesiniAnlatir()
    {
        // 45-API devir koşulu (1): bu runbook YAZILMADAN üretim yayın adımı
        // başlayamaz — tek SAHİP kullanıcı MFA cihazını kaybederse kilitlenir.
        var metin = Oku("docs", "RUNBOOK-MFA-KURTARMA.md");
        foreach (var beklenen in new[] { "kurtarma kod", "kullanici_mfa", "denetim" })
            Assert.Contains(beklenen, metin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SirRotasyonuMfaGizleriniYenidenSifrelemeyiAnlatir()
    {
        // 45-API devir koşulu (2): AlanSifreleme anahtarı değişir de mfa_giz
        // alanları yeniden şifrelenmezse TÜM MFA kullanıcıları kilitlenir.
        var metin = Oku("docs", "DEPLOY.md");
        Assert.Contains("gizli_sifreli", metin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rotasyon", metin, StringComparison.OrdinalIgnoreCase);
    }
}
