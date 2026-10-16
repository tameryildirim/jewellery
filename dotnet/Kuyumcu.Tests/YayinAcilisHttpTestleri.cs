using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-1 (+F-6) — AÇILIŞ VE SAĞLIK UÇLARI [HTTP testleri]
//
// ⚠ İZOLASYON (Aşkın koşulu, M-T1-05): fail-closed kapısı `builder.Build()`
// ÖNCESİ çalışır; WebApplicationFactory'nin yapılandırma geri çağrıları o an
// için ÇOK GEÇTİR. Bu yüzden üretim profili yalnız SÜREÇ ORTAM DEĞİŞKENİYLE
// kurulabilir ve ortam değişkeni SÜREÇ GENELİDİR. Testler bu yüzden
// paralelleştirmesi KAPALI bir koleksiyonda koşar ve her değişkeni
// try/finally ile geri alır.
// ═════════════════════════════════════════════════════════════════════════════
[CollectionDefinition("YayinAcilis", DisableParallelization = true)]
public sealed class YayinAcilisKoleksiyonu { }

[Collection("YayinAcilis")]
public class YayinAcilisHttpTestleri
{
    // ── Gerçek HTTP: sağlık uçları ───────────────────────────────────────────

    [Fact]
    public async Task SaglikUcuSirVeSurumSizdirmaz()
    {
        using var ortam = new HttpOrtami();
        var istemci = ortam.AnonimIstemci();

        var yanit = await istemci.GetAsync("/saglik");
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var govde = await yanit.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", govde);

        // Gövde ne sürüm, ne şema, ne bağlantı, ne sır sızdırır.
        foreach (var yasak in new[]
                 {
                     HttpOrtami.TestSirri, "password", "server=", "Migration",
                     "kuyumcu_test", "Version", "8.0",
                 })
            Assert.DoesNotContain(yasak, govde, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HazirUcuSemaGuncelken200Doner()
    {
        using var ortam = new HttpOrtami();
        var istemci = ortam.AnonimIstemci();

        var yanit = await istemci.GetAsync("/saglik/hazir");
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        using var belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.True(belge.RootElement.GetProperty("hazir").GetBoolean());
        Assert.Equal("HAZIR", belge.RootElement.GetProperty("kod").GetString());
    }

    [Fact]
    public async Task HazirUcuGovdesindeMigrationAdiVerilmez()
    {
        // Şema bilgisi = saldırgana sürüm bilgisi. Gövde yalnız kısa bir kod
        // taşır; hangi migration'ın eksik olduğunu SÖYLEMEZ.
        using var ortam = new HttpOrtami();
        var istemci = ortam.AnonimIstemci();

        var govde = await (await istemci.GetAsync("/saglik/hazir")).Content.ReadAsStringAsync();
        using var belge = JsonDocument.Parse(govde);

        // Yalnız iki alan: hazir + kod.
        Assert.Equal(2, belge.RootElement.EnumerateObject().Count());
        Assert.DoesNotMatch(@"\d{14}", govde);   // migration adı damgası
    }

    [Fact]
    public async Task HazirUcuBekleyenMigrationVarken503Doner()
    {
        // GERÇEK bir "eski şema" kurulur (sondan bir önceki migration'a kadar)
        // ve uygulama O ŞEMAYA bağlanır. kuyumcu_test'in geçmişine DOKUNULMAZ.
        using var eski = new EskiSemaFiksturu("kuyumcu_t1_hazir");
        using var ortam = new HttpOrtami();
        using var kapsam = new UretimProfiliKapsami(
            ("ConnectionStrings__Kuyumcu", eski.Baglanti));

        var yanit = await ortam.AnonimIstemci().GetAsync("/saglik/hazir");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, yanit.StatusCode);

        using var belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.False(belge.RootElement.GetProperty("hazir").GetBoolean());
        Assert.Equal("SEMA_ESKI", belge.RootElement.GetProperty("kod").GetString());
    }

    [Fact]
    public async Task HazirUcuVeritabaniYokkenHatayiHazirDiyeYutmaz()
    {
        // Anayasa md.1: erişilemeyen veritabanı "hazır" sayılmaz.
        using var ortam = new HttpOrtami();
        using var kapsam = new UretimProfiliKapsami(
            ("ConnectionStrings__Kuyumcu",
                "server=127.0.0.1;port=3307;database=olmayan_vt;user=root;password=yok-boyle-bir-parola"));

        var yanit = await ortam.AnonimIstemci().GetAsync("/saglik/hazir");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, yanit.StatusCode);

        var govde = await yanit.Content.ReadAsStringAsync();
        Assert.Contains("VERITABANI_YOK", govde, StringComparison.Ordinal);
        // Sürücü mesajı, sunucu adı ve parola gövdeye SIZMAZ.
        Assert.DoesNotContain("olmayan_vt", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("parola", govde, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HazirUcuKimlikIstemez()
    {
        // Servis yöneticisi / yük dengeleyici token taşımaz; uç anonim olmalı.
        using var ortam = new HttpOrtami();
        var yanit = await ortam.AnonimIstemci().GetAsync("/saglik/hazir");
        Assert.NotEqual(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    // ── Fail-closed: üretim profili süreci AÇMAZ ─────────────────────────────

    // ⚠ SIRA ÖNEMLİ: HttpOrtami'nin KURUCUSU Jwt__Secret / ConnectionStrings__Kuyumcu
    // ortam değişkenlerini kendi test değerleriyle EZER. Bu yüzden üretim profili
    // fabrika kurulduktan SONRA, istemci (yani gerçek Build()) oluşturulmadan
    // ÖNCE kurulur.

    [Fact]
    public void VarsayilanSirliFabrikaAcilistaInvalidOperationExceptionAtar()
    {
        // M-T1-05'in hedefi: kapı TEMBEL olursa bu test yeşil kalır ve
        // varsayılan sırlı bir sunucu ayağa kalkar. Kapı Build() ÖNCESİ
        // çalıştığı için istemci hiç OLUŞTURULAMAZ.
        using var ortam = new HttpOrtami();
        using var kapsam = new UretimProfiliKapsami(
            ("Jwt__Secret", "DEGISTIR-en-az-32-karakter-uretim-secreti"),
            ("Uretim__DenetimiZorla", "true"),
            ("Uretim__TersVekilArkasinda", "true"));

        var koku = KokHata(Assert.ThrowsAny<Exception>(() => ortam.AnonimIstemci()));
        Assert.IsType<InvalidOperationException>(koku);
        Assert.Contains("Jwt:Secret", koku.Message);
        Assert.DoesNotContain("DEGISTIR-en-az-32", koku.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GelistirmeParolasiylaUretimProfiliAcilmaz()
    {
        // Uçtan uca kanıt: kuyumcu_test bağlantısı GELİŞTİRME parolasını taşır
        // ve o parola kara listededir. Üretim profilinde süreç açılamaz.
        using var ortam = new HttpOrtami();
        using var kapsam = new UretimProfiliKapsami(
            ("Uretim__DenetimiZorla", "true"),
            ("Uretim__TersVekilArkasinda", "true"));

        var koku = KokHata(Assert.ThrowsAny<Exception>(() => ortam.AnonimIstemci()));
        Assert.IsType<InvalidOperationException>(koku);
        Assert.Contains("parola", koku.Message, StringComparison.OrdinalIgnoreCase);

        // Parolanın DEĞERİ hata mesajına düşmez (systemd/journald sızıntısı).
        var parola = new MySqlConnector.MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__Kuyumcu")!).Password;
        Assert.False(string.IsNullOrEmpty(parola));
        Assert.DoesNotContain(parola, koku.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TlsBeyaniOlmadanUretimProfiliAcilmaz()
    {
        using var ortam = new HttpOrtami();
        // Parola kuralı TLS kuralından ÖNCE gelir; TLS'i sınamak için bağlantıya
        // kara listede OLMAYAN bir parola verilir (bu profille zaten açılmaz).
        using var kapsam = new UretimProfiliKapsami(
            ("ConnectionStrings__Kuyumcu",
                "server=localhost;port=3306;database=kuyumcu_test;user=root;password=Yerel-Tls-Denemesi-7731"),
            ("Uretim__DenetimiZorla", "true"),
            ("Uretim__TersVekilArkasinda", "false"),
            ("ASPNETCORE_URLS", "http://127.0.0.1:5310"));

        var koku = KokHata(Assert.ThrowsAny<Exception>(() => ortam.AnonimIstemci()));
        Assert.IsType<InvalidOperationException>(koku);
        Assert.Contains("TLS", koku.Message);
    }

    [Fact]
    public async Task MesruUretimProfiliAcilirVeSaglik200Doner()
    {
        // Kapının fazla katı OLMADIĞININ kanıtı: kuruluma özgü sır + ters vekil
        // beyanı verildiğinde üretim kuralları açıkken uygulama AÇILIR.
        using var ortam = new HttpOrtami();
        using var kapsam = new UretimProfiliKapsami(
            ("ConnectionStrings__Kuyumcu", MesruParolaliBaglanti()),
            ("Uretim__DenetimiZorla", "true"),
            ("Uretim__TersVekilArkasinda", "true"));

        var yanit = await ortam.AnonimIstemci().GetAsync("/saglik");
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    /// <summary>
    /// Test bağlantısının parolasını kara liste DIŞINA taşıyan bir kopya.
    /// (Sunucu yine aynı; /saglik veritabanına dokunmaz.)
    /// </summary>
    private static string MesruParolaliBaglanti()
    {
        var b = new MySqlConnector.MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__Kuyumcu")!);
        b.Password += "-kuruluma-ozgu-ek";
        Assert.False(UretimAcilisDenetimi.KaraListede(b.Password));
        return b.ConnectionString;
    }

    /// <summary>
    /// Sondan bir önceki migration'a kadar göç ettirilmiş AYRI bir şema kurar
    /// ve iş bitince siler. Geliştirme ve test şemalarına DOKUNMAZ.
    /// </summary>
    private sealed class EskiSemaFiksturu : IDisposable
    {
        public string Baglanti { get; }
        private readonly Kuyumcu.Api.Data.KuyumcuDbContext db;

        public EskiSemaFiksturu(string semaAdi)
        {
            var kaynak = new TestVeritabani();      // şema kurulumunu tetikler
            Baglanti = new MySqlConnector.MySqlConnectionStringBuilder(kaynak.Baglanti)
            {
                Database = semaAdi,
            }.ConnectionString;

            var ayar = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<
                    Kuyumcu.Api.Data.KuyumcuDbContext>()
                .UseMySql(Baglanti, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.4.0-mysql"))
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
                    Kuyumcu.Api.Data.KiraciModelOnbellegi>()
                .Options;

            db = new Kuyumcu.Api.Data.KuyumcuDbContext(
                ayar, new Kuyumcu.Api.Data.SistemKiraciBaglami(), TestVeritabani.TestSifreleyici);
            db.Database.EnsureDeleted();

            var tanimli = Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions
                .GetMigrations(db.Database).ToList();
            var kapsayici = ((Microsoft.EntityFrameworkCore.Infrastructure
                .IInfrastructure<IServiceProvider>)db).Instance;
            var gocMotoru = (Microsoft.EntityFrameworkCore.Migrations.IMigrator)
                kapsayici.GetService(typeof(Microsoft.EntityFrameworkCore.Migrations.IMigrator))!;
            gocMotoru.Migrate(tanimli[^2]);
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }
    }

    private static Exception KokHata(Exception e)
    {
        while (e.InnerException is not null) e = e.InnerException;
        return e;
    }

    /// <summary>
    /// Süreç ortam değişkenlerini geçici olarak kurar ve HER DURUMDA geri alır.
    /// (Koleksiyon paralelleştirmesi kapalı olduğu için başka test etkilenmez.)
    /// </summary>
    private sealed class UretimProfiliKapsami : IDisposable
    {
        private readonly (string Ad, string? Onceki)[] eskiler;

        public UretimProfiliKapsami(params (string Ad, string Deger)[] ayarlar)
        {
            eskiler = ayarlar
                .Select(a => (a.Ad, Environment.GetEnvironmentVariable(a.Ad)))
                .ToArray();
            foreach (var (ad, deger) in ayarlar)
                Environment.SetEnvironmentVariable(ad, deger);
        }

        public void Dispose()
        {
            foreach (var (ad, onceki) in eskiler)
                Environment.SetEnvironmentVariable(ad, onceki);
        }
    }
}
