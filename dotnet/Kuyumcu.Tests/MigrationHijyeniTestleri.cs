using System.Reflection;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-5 — MİGRATION HİJYENİ (kurul tur 2, engel 20 · MÜHÜR 5 KOŞUL 6)
//
// SORUN (HAZIRLIK-KURULU-2.md § engel 20 / 212-213):
//   Data/Migrations/ altında 4 adet `*_FiyatListesiVeKampanya` ÇİFTİ (migration
//   + Designer, toplam 8 dosya) 0 BAYTTIR. EF onları görmez (içlerinde tip yok),
//   ama depoda dururlar; üretim kurulumunda "bunlar niye atlandı" sorusunu
//   doğururlar. Ayrıca 20260915115025_VadeTakibi.Designer.cs GÜDÜKTÜR —
//   BuildTargetModel gövdesi yoktur; o migration canlıda UYGULANMIŞ olmasına
//   rağmen anlık görüntüsü yoktur. Yanında 16 adet `.mutorj` mutasyon-yedeği
//   kaynak ağacında durur ve ajanlar bunları CANLI KOD sanar.
//
// BU PAKETİN KARARI (kurucu kalemi — KURUCUYA-KARARLAR-2026-09-16.md md.4):
//   SİLME YOK. Dosyalar yerinde kalır; silme Kurucu'nun tek komutluk işidir.
//   Bu pakette yapılan şey KAPIYI KAPATMAKTIR: bugünkü borç AD AD beyaz listeye
//   yazılır, beyaz liste dışında YENİ bir 0 baytlık dosya / güdük Designer /
//   .mutorj artığı girerse denetim ve test KIRMIZI olur.
//
// BEYAZ LİSTE ÇÜRÜMEZ: Kurucu silince beyaz liste kalemi "artık yok" diye
// kırmızıya döner → liste elle temizlenir. Sessiz eskime yasak (Anayasa md.1).
//
// ⚠ İKİZLİK: aynı üç sayaç dotnet/denetim.sh § 12'de de vardır. Testler
//   geliştirici masasında, denetim.sh kapıda koşar; ikisi de aynı listeyi
//   okur — biri değişip diğeri değişmezse son test bunu yakalar.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MigrationHijyeniTestleri
{
    // ── BUGÜNKÜ BORÇ — ad ad, gerekçeli (Kurucu silince BU LİSTE BOŞALIR) ────

    /// <summary>
    /// 0 baytlık migration dosyaları. Gerekçe: 15 Eylül'de `dotnet ef migrations
    /// add` dört kez yarıda kesildi; EF dosyayı açıp boş bırakıp çıktı. Beşinci
    /// deneme (20260915132909) gerçek migration'ı üretti ve canlıda UYGULANDI.
    /// Bu dördü __EFMigrationsHistory'de YOKTUR (T-5 raporu § 2 SQL kanıtı).
    /// Silme: Kurucu onayında (TurkReport kancası `rm`'i bloklar).
    /// </summary>
    private static readonly string[] BosMigrationBeyazListesi =
    {
        "20260915131748_FiyatListesiVeKampanya.cs",
        "20260915131748_FiyatListesiVeKampanya.Designer.cs",
        "20260915132123_FiyatListesiVeKampanya.cs",
        "20260915132123_FiyatListesiVeKampanya.Designer.cs",
        "20260915132303_FiyatListesiVeKampanya.cs",
        "20260915132303_FiyatListesiVeKampanya.Designer.cs",
        "20260915132645_FiyatListesiVeKampanya.cs",
        "20260915132645_FiyatListesiVeKampanya.Designer.cs",
    };

    /// <summary>
    /// BuildTargetModel gövdesi olmayan Designer'lar. VadeTakibi'ninki 336 bayttır
    /// (yalnız başlık + attribute); migration'ın kendisi sağlam ve UYGULANMIŞtır,
    /// eksik olan yalnız o adımdaki model anlık görüntüsüdür — geriye dönük
    /// `migrations script` üretimi bu adımda kör kalır. Boş dosyalar zaten
    /// yukarıdaki listede; burada tekrar edilmezler.
    /// </summary>
    private static readonly string[] GudukDesignerBeyazListesi =
    {
        "20260915115025_VadeTakibi.Designer.cs",
    };

    /// <summary>
    /// Mutasyon testi betiğinin (mut.sh) kaynak ağacında bıraktığı yedekler.
    /// 15-16 Eylül dalgalarından kalma; hepsi `dotnet/` altında. Silme Kurucu
    /// kalemi. Yeni artık sızmasın diye .gitignore'a `*.mutorj` eklendi.
    /// </summary>
    /// 28.09 16:25 Kurucu 16 artığın hepsini sildi → liste BOŞ; bundan sonra
    /// ağaçtaki her .mutorj kaçaktır.
    private static readonly string[] MutorjBeyazListesi = { };

    private const string SilmeNotu =
        "Silme Kurucu kalemidir (KURUCUYA-KARARLAR-2026-09-16.md md.4); " +
        "Kurucu sildiyse bu adı MigrationHijyeniTestleri beyaz listesinden VE " +
        "denetim.sh § 12 listesinden çıkarın.";

    private static string MigrationDizini() =>
        CssKaynak.Yol("Kuyumcu.Api", "Data", "Migrations");

    private static string[] TumMigrationDosyalari() =>
        Directory.GetFiles(MigrationDizini(), "*.cs").OrderBy(x => x).ToArray();

    // ── 1) 0 BAYTLIK DOSYA ──────────────────────────────────────────────────

    [Fact]
    public void Beyaz_liste_disinda_sifir_baytlik_migration_dosyasi_yok()
    {
        var kacaklar = TumMigrationDosyalari()
            .Where(y => new FileInfo(y).Length == 0)
            .Select(Path.GetFileName)
            .Where(ad => !BosMigrationBeyazListesi.Contains(ad))
            .ToArray();

        Assert.True(kacaklar.Length == 0,
            "0 baytlık migration dosyası üretildi (yarıda kesilmiş `dotnet ef migrations add`): "
            + string.Join(", ", kacaklar)
            + " — dosyayı yeniden üretin; boş dosya depoya girmez.");
    }

    [Fact]
    public void Sifir_bayt_beyaz_listesi_curumez_her_kalem_hala_yerinde_ve_bos()
    {
        foreach (var ad in BosMigrationBeyazListesi)
        {
            var y = Path.Combine(MigrationDizini(), ad);
            Assert.True(File.Exists(y), $"Beyaz liste kalemi artık yok: {ad}. {SilmeNotu}");
            Assert.True(new FileInfo(y).Length == 0,
                $"Beyaz liste kalemi artık boş değil: {ad}. İçerik kazandıysa beyaz listeden çıkarın.");
        }
    }

    // ── 2) GÜDÜK DESIGNER ───────────────────────────────────────────────────

    [Fact]
    public void Beyaz_liste_disindaki_her_Designer_BuildTargetModel_tasir()
    {
        var beklenenDisi = BosMigrationBeyazListesi.Concat(GudukDesignerBeyazListesi).ToHashSet();

        var kacaklar = TumMigrationDosyalari()
            .Where(y => y.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(y => !beklenenDisi.Contains(Path.GetFileName(y)))
            .Where(y => !File.ReadAllText(y).Contains("BuildTargetModel", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.True(kacaklar.Length == 0,
            "Designer dosyasında BuildTargetModel gövdesi yok (model anlık görüntüsü kayıp): "
            + string.Join(", ", kacaklar));
    }

    [Fact]
    public void Guduk_Designer_beyaz_listesi_curumez()
    {
        foreach (var ad in GudukDesignerBeyazListesi)
        {
            var y = Path.Combine(MigrationDizini(), ad);
            Assert.True(File.Exists(y), $"Beyaz liste kalemi artık yok: {ad}. {SilmeNotu}");
            Assert.False(File.ReadAllText(y).Contains("BuildTargetModel", StringComparison.Ordinal),
                $"{ad} artık BuildTargetModel taşıyor — düzeldi, beyaz listeden çıkarın.");
        }
    }

    // ── 3) DERLENEN MİGRATION KÜMESİ = DOLU DOSYA KÜMESİ ────────────────────

    /// <summary>Derlenmiş derlemedeki migration kimlikleri (EF'in gerçekten gördüğü küme).</summary>
    private static HashSet<string> DerlemedekiKimlikler() =>
        typeof(KuyumcuDbContext).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<MigrationAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Id)
            .ToHashSet();

    [Fact]
    public void Bos_migration_kimlikleri_EFnin_gordugu_kumede_yok()
    {
        var kimlikler = DerlemedekiKimlikler();

        foreach (var ad in BosMigrationBeyazListesi.Where(a => !a.EndsWith(".Designer.cs", StringComparison.Ordinal)))
        {
            var kimlik = ad[..^3]; // ".cs" atılır
            Assert.False(kimlikler.Contains(kimlik),
                $"0 baytlık sanılan {kimlik} EF tarafından görülüyor — dosya artık boş değil.");
        }

        // Gerçek olan beşinci deneme GÖRÜLÜYOR: boş ikizler onun yerine geçmiş olamaz.
        Assert.Contains("20260915132909_FiyatListesiVeKampanya", kimlikler);
    }

    [Fact]
    public void Dolu_migration_dosyasi_ile_derlenen_migration_kumesi_birebir_ayni()
    {
        var dosyadan = TumMigrationDosyalari()
            .Where(y => !y.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(y => !Path.GetFileName(y).StartsWith("KuyumcuDbContextModelSnapshot", StringComparison.Ordinal))
            .Where(y => new FileInfo(y).Length > 0)
            .Select(y => Path.GetFileNameWithoutExtension(y))
            .ToHashSet();

        var derlemeden = DerlemedekiKimlikler();

        Assert.Equal(dosyadan.OrderBy(x => x), derlemeden.OrderBy(x => x));
    }

    [Fact]
    public void ModelSnapshot_son_migrationin_Designer_modeliyle_ayni_varlik_kumesini_tasir()
    {
        var snapshot = File.ReadAllText(
            Path.Combine(MigrationDizini(), "KuyumcuDbContextModelSnapshot.cs"));

        var sonKimlik = DerlemedekiKimlikler().OrderBy(x => x, StringComparer.Ordinal).Last();
        var sonDesigner = File.ReadAllText(
            Path.Combine(MigrationDizini(), sonKimlik + ".Designer.cs"));

        static HashSet<string> Varliklar(string metin) =>
            System.Text.RegularExpressions.Regex
                .Matches(metin, @"modelBuilder\.Entity\(""([^""]+)""")
                .Select(m => m.Groups[1].Value).ToHashSet();

        var s = Varliklar(snapshot);
        Assert.NotEmpty(s);
        Assert.Equal(Varliklar(sonDesigner).OrderBy(x => x, StringComparer.Ordinal),
                     s.OrderBy(x => x, StringComparer.Ordinal));
    }

    // ── 4) .mutorj ARTIKLARI ────────────────────────────────────────────────

    private static string[] AgactakiMutorjlar() =>
        Directory.GetFiles(CssKaynak.Kok(), "*.mutorj", SearchOption.AllDirectories)
            .Where(y => !y.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !y.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(y => Path.GetRelativePath(CssKaynak.Kok(), y).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void Beyaz_liste_disinda_mutorj_artigi_yok()
    {
        var kacaklar = AgactakiMutorjlar().Except(MutorjBeyazListesi).ToArray();

        Assert.True(kacaklar.Length == 0,
            "Kaynak ağacında yeni .mutorj yedeği kaldı (mutasyon turu yedeği geri koymadı): "
            + string.Join(", ", kacaklar)
            + " — yedek geçici dizine alınır, ağaca bırakılmaz.");
    }

    [Fact]
    public void Mutorj_beyaz_listesi_curumez()
    {
        var agac = AgactakiMutorjlar().ToHashSet();
        foreach (var ad in MutorjBeyazListesi)
            Assert.True(agac.Contains(ad), $"Beyaz liste kalemi artık yok: {ad}. {SilmeNotu}");
    }

    [Fact]
    public void Gitignore_yeni_mutorj_artiklarini_disarida_tutar()
    {
        var gitignore = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "..", ".gitignore"));
        Assert.Contains("*.mutorj", gitignore, StringComparison.Ordinal);
    }

    // ── 5) İKİZLİK — denetim.sh aynı listeyi taşır ──────────────────────────

    [Fact]
    public void Denetim_sh_ayni_beyaz_listeyi_ve_uc_sayaci_tasir()
    {
        var d = CssKaynak.Metin("denetim.sh");

        Assert.Contains("T-5 MİGRATION HİJYENİ", d, StringComparison.Ordinal);
        Assert.Contains("BOS_MIGRATION_BEYAZ", d, StringComparison.Ordinal);
        Assert.Contains("GUDUK_DESIGNER_BEYAZ", d, StringComparison.Ordinal);
        Assert.Contains("MUTORJ_BEYAZ", d, StringComparison.Ordinal);

        foreach (var ad in BosMigrationBeyazListesi.Concat(GudukDesignerBeyazListesi))
            Assert.True(d.Contains(ad, StringComparison.Ordinal),
                $"denetim.sh beyaz listesinde eksik kalem: {ad} — iki liste ayrışamaz.");

        foreach (var ad in MutorjBeyazListesi)
            Assert.True(d.Contains(ad, StringComparison.Ordinal),
                $"denetim.sh .mutorj beyaz listesinde eksik kalem: {ad}.");
    }
}
