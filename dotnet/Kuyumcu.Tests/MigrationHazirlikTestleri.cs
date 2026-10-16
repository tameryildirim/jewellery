using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-1 (+F-6) — ŞEMA HAZIRLIK KAPISI [DB testleri]
//
// FAIL-CLOSED 5: uygulama şemayı KENDİ DEĞİŞTİRMEZ. Otomatik `Migrate()`
// yedeksiz ve çok süreçli bir şema değişimi demektir ve GERİ ALINAMAZ.
// Şema yükseltmesi tek yoldan yapılır: doğrulanmış yedek + migration bundle
// (docs/RUNBOOK-SEMA-YUKSELTME.md).
//
// ⚠ Bu testler kuyumcu_test şemasında çalışır (TestVeritabani). Geliştirme
// şemasına (kuyumcu / kuyumcu_net) DOKUNMAZLAR — TestVeritabani zaten onlara
// bağlanmayı reddeder.
// ═════════════════════════════════════════════════════════════════════════════
public class MigrationHazirlikTestleri : IClassFixture<TestVeritabani>
{
    private readonly TestVeritabani vt;
    public MigrationHazirlikTestleri(TestVeritabani vt) => this.vt = vt;

    [Fact]
    public void GercekVeritabaninda_BekleyenMigrationYok()
    {
        using var db = vt.OlusturSistem();
        var bekleyen = db.Database.GetPendingMigrations().ToList();
        Assert.True(bekleyen.Count == 0,
            "Bekleyen migration var: " + string.Join(", ", bekleyen));
    }

    [Fact]
    public void UygulanmisMigrationSayisi_ModeldekiyleAynidir()
    {
        // SAYISAL İDDİA YASAĞI (mühür 3): beklenen sayı KODA GÖMÜLMEZ,
        // derlemedeki migration listesinden TÜRETİLİR.
        using var db = vt.OlusturSistem();
        var tanimli = db.Database.GetMigrations().Count();
        var uygulanmis = db.Database.GetAppliedMigrations().Count();
        Assert.True(tanimli > 0, "Derlemede hiç migration yok — şema kaynağı kayıp.");
        Assert.Equal(tanimli, uygulanmis);
    }

    [Fact]
    public void UygulamaSemayiKendiDegistirmez_MigrationsHistorySatirSayisiDegismez()
    {
        using var db = vt.OlusturSistem();
        var once = db.Database.GetAppliedMigrations().Count();

        // Uygulamanın normal çalışması (okuma + yazma) şemaya DOKUNMAZ.
        _ = db.Tenants.AsNoTracking().Take(1).ToList();

        var sonra = db.Database.GetAppliedMigrations().Count();
        Assert.Equal(once, sonra);

        // Ve kaynakta otomatik Migrate() AÇILMAMIŞTIR (M-T1-07).
        // denetim.sh aynı ölçümü CI'da yapar; burada testten de görünür olsun.
        var program = File.ReadAllText(Path.Combine(
            YayinDosyaSozlesmesiTestleri.ProjeKoku(),
            "dotnet", "Kuyumcu.Api", "Program.cs"));
        var kodSatirlari = program.Split('\n')
            .Where(s => !s.TrimStart().StartsWith("//", StringComparison.Ordinal));
        Assert.DoesNotContain(kodSatirlari,
            s => s.Contains("Database.Migrate()", StringComparison.Ordinal)
                 || s.Contains("Database.MigrateAsync(", StringComparison.Ordinal));
    }

    [Fact]
    public void UretimAcilisKapisiSistemKapsamiKullanir()
    {
        // GERÇEK ÜRETİM PROVASINDA YAKALANDI (2026-09-17): açılış kapısı
        // `app.Services.CreateScope()` ile DbContext çözüyordu ve her üretim
        // başlangıcı "Kiracı bağlamı çözülemedi" ile ÇÖKÜYORDU. Testlerin
        // hiçbiri Production ortamını çalıştırmadığı için görünmüyordu.
        // Bu sayaç aynı kalıbın geri dönmesini yakalar.
        var program = File.ReadAllText(Path.Combine(
            YayinDosyaSozlesmesiTestleri.ProjeKoku(),
            "dotnet", "Kuyumcu.Api", "Program.cs"));
        var kod = program.Split('\n')
            .Where(s => !s.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain(kod, s => s.Contains("app.Services.CreateScope()", StringComparison.Ordinal));
        Assert.Contains(kod, s => s.Contains("GetRequiredService<SistemKapsami>().Sistem()", StringComparison.Ordinal));
    }

    [Fact]
    public void EksikMigrationliSemadaBekleyenListesiDolar()
    {
        // GERÇEK bir "eski şema" kurulur: AYRI bir veritabanına migration'lar
        // SONDAN BİR ÖNCEKİNE kadar uygulanır. kuyumcu_test'in geçmişine
        // DOKUNULMAZ (Aşkın koşulu: yanlış şemada koşarsa geçmiş bozulur).
        var eskiVt = "kuyumcu_t1_eski";
        var baglanti = new MySqlConnector.MySqlConnectionStringBuilder(vt.Baglanti)
        {
            Database = eskiVt,
        }.ConnectionString;

        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
                Kuyumcu.Api.Data.KiraciModelOnbellegi>()
            .Options;

        using var db = new KuyumcuDbContext(ayar, new Kuyumcu.Api.Data.SistemKiraciBaglami(),
            TestVeritabani.TestSifreleyici);
        try
        {
            db.Database.EnsureDeleted();
            var tanimli = db.Database.GetMigrations().ToList();
            Assert.True(tanimli.Count >= 2, "En az iki migration bekleniyordu.");

            var sondanOnceki = tanimli[^2];
            var ecKapsam = ((Microsoft.EntityFrameworkCore.Infrastructure
                .IInfrastructure<IServiceProvider>)db).Instance;
            var gocMotoru = (Microsoft.EntityFrameworkCore.Migrations.IMigrator)
                ecKapsam.GetService(typeof(Microsoft.EntityFrameworkCore.Migrations.IMigrator))!;
            gocMotoru.Migrate(sondanOnceki);

            var bekleyen = db.Database.GetPendingMigrations().ToList();

            // Hazırlık kapısının TAM kuralı: bekleyen varsa hazır DEĞİL.
            Assert.NotEmpty(bekleyen);
            Assert.Equal(tanimli[^1], bekleyen.Single());
        }
        finally
        {
            db.Database.EnsureDeleted();   // geçici şema bırakılmaz
        }
    }
}
