using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules;
using Kuyumcu.Api.Modules.Isler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İŞ ÇATISI — SAF TESTLER (paket 6 Faz A)
//
// Veritabanı ve HTTP gerektirmeyen dört kapı:
//   • anahtar üretimi YEREL güne bağlıdır (K5 tuzağı)
//   • tekillik anahtarı SAAT içermez
//   • DI'da IBildirimKanali uygulaması TAM 1'dir (ADR-0016)
//   • Isler/ ağacı ADR-0008'in Uygula()'sını ve deftere yazmayı GÖRMEZ
//     (denetim.sh sayaçlarının test tarafı ikizi)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class IsCatisiSafTestleri
{
    // ── Anahtar üretimi ─────────────────────────────────────────────────────

    /// <summary>M6-4 karşıtı: gece yarısından sonraki tur "yarına" düşmez.</summary>
    [Fact]
    public void Calisma_anahtari_Istanbulda_0030_ve_2330_icin_AYNI_gunu_uretir()
    {
        var gun = new DateOnly(2026, 9, 16);
        // İstanbul 00:30 ve 23:30'un UTC karşılıkları
        var gece = TimeZoneInfo.ConvertTimeToUtc(
            gun.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Unspecified), Zaman.Isletme);
        var aksam = TimeZoneInfo.ConvertTimeToUtc(
            gun.ToDateTime(new TimeOnly(23, 30), DateTimeKind.Unspecified), Zaman.Isletme);

        var i = new Kuyumcu.Api.Modules.Isler.Isler.VadeHatirlatmaIsi();
        var a1 = i.CalismaAnahtari(DateOnly.FromDateTime(Zaman.UtcdenYerele(gece)));
        var a2 = i.CalismaAnahtari(DateOnly.FromDateTime(Zaman.UtcdenYerele(aksam)));

        Assert.Equal("2026-09-16", a1);
        Assert.Equal(a1, a2);
        // UTC günü kullanılsaydı gece 00:30 "2026-09-15" olurdu.
        Assert.NotEqual(DateOnly.FromDateTime(gece).ToString("yyyy-MM-dd"), a1);
    }

    /// <summary>M6-4: tekillik anahtarına saat eklenirse iş günde 96 bildirim üretir.</summary>
    [Fact]
    public void Tekillik_anahtari_kimlik_ve_yerel_gun_icerir_SAAT_icermez()
    {
        var gun = new DateOnly(2026, 9, 16);
        var a = IsAnahtarlari.Tekillik("VADE", "01CARIKIMLIK", gun);

        Assert.Equal("VADE:01CARIKIMLIK:2026-09-16", a);
        Assert.Contains("01CARIKIMLIK", a, StringComparison.Ordinal);
        Assert.Equal(2, a.Count(c => c == ':'));   // saat eklenirse üçüncü ':' ya da '-Ehhmm' çıkar
        Assert.DoesNotContain("-E", a, StringComparison.Ordinal);
    }

    /// <summary>Elle koşum anahtarı günlük anahtardan AYRIŞIR ama aynı güne bağlıdır.</summary>
    [Fact]
    public void Elle_kosum_anahtari_gunluk_anahtarla_cakismaz()
    {
        var an = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 9, 16, 14, 35, 0, DateTimeKind.Unspecified), Zaman.Isletme);

        var elle = IsAnahtarlari.Elle(an);
        Assert.Equal("2026-09-16-E1435", elle);
        Assert.NotEqual(IsAnahtarlari.Gunluk(new DateOnly(2026, 9, 16)), elle);
        Assert.StartsWith("2026-09-16", elle, StringComparison.Ordinal);
    }

    // ── DI tekeli ───────────────────────────────────────────────────────────

    /// <summary>M6-7: ikinci bir IBildirimKanali kaydı sessizce dışarıya kanal açardı.</summary>
    [Fact]
    public void DI_de_IBildirimKanali_uygulamasi_TAM_1_dir()
    {
        var s = new ServiceCollection();
        s.AddKuyumcuModulleri();

        var kayitlar = s.Where(d => d.ServiceType == typeof(IBildirimKanali)).ToList();

        Assert.Single(kayitlar);
        Assert.Equal(typeof(UygulamaIciKanal), kayitlar[0].ImplementationType);
    }

    /// <summary>Üç Faz A işi + PAKET YASAL-2 ile Faz B işi (TEVSIK_KURTARMA) kayıtlı.</summary>
    [Fact]
    public void DI_de_uc_Faz_A_isi_ve_tevsik_kurtarma_kayitlidir()
    {
        var s = new ServiceCollection();
        s.AddKuyumcuModulleri();

        var tipler = s.Where(d => d.ServiceType == typeof(IZamanlanmisIs))
            .Select(d => d.ImplementationType!.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(["MasakSibSuresiIsi", "SaklamaOnizlemeIsi", "SaklamaUygulaIsi", "StopajHatirlatmaIsi", "TevsikKurtarmaIsi", "VadeHatirlatmaIsi"], tipler);   // YASAL-3: SAKLAMA_UYGULA · YASAL-4: STOPAJ_HATIRLATMA
    }

    // ── Kaynak taraması (denetim.sh'ın test tarafı ikizi) ────────────────────

    /// <summary>
    /// Isler/ ağacının KAYNAK metni taranır — derleme dizini değil. Yol,
    /// CssKaynak.Kok() ile Kuyumcu.sln'den türetilir (sabit yol yazılmaz).
    /// </summary>
    private static IReadOnlyList<(string Ad, string Metin)> IslerKaynaklari()
    {
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api", "Modules", "Isler");
        Assert.True(Directory.Exists(kok), $"Kaynak dizini yok: {kok}");
        return [.. Directory.GetFiles(kok, "*.cs", SearchOption.AllDirectories)
            .Select(y => (Path.GetFileName(y), CssKaynak.YorumsuzKod(File.ReadAllText(y))))];
    }

    /// <summary>
    /// M6-6 (PAKET YASAL-3 § 4.3 ile güncellendi): ADR-0008 Uygula() iş ağacında YALNIZ
    /// SaklamaUygulaIsi'nden ve TEK kez çağrılır (30 gün önceden bildirim + bekletme +
    /// varsayılan kuru koşum). Başka hiçbir iş dosyası maskelemeyi tetiklemez.
    /// </summary>
    [Fact]
    public void Isler_agacinda_Uygula_cagrisi_yalniz_SaklamaUygulaIsi_ve_tek()
    {
        foreach (var (ad, metin) in IslerKaynaklari())
        {
            var adet = System.Text.RegularExpressions.Regex.Matches(metin, @"Uygula\(").Count;
            if (ad == "SaklamaUygulaIsi.cs") Assert.Equal(1, adet);
            else Assert.True(adet == 0, $"{ad}: Uygula( {adet} kez");
        }
    }

    /// <summary>İş çatısı deftere YAZMAZ.</summary>
    [Fact]
    public void Isler_agacinda_defter_yazimi_gecmez()
    {
        foreach (var (ad, metin) in IslerKaynaklari())
        {
            Assert.DoesNotContain("HareketFabrikasi", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("Hareketler.Add(", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("KasaHareketleri.Add(", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("IgnoreQueryFilters", metin, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// M6-9: zamanlayıcı kiracı gezmesini KİRACIYA SABİT kapsamla yapar.
    /// ⚠ Bu bir KAYNAK testidir ve bilinçlidir: HttpOrtami tüm IHostedService'leri
    ///   kaldırdığı için döngü HTTP testinde koşmaz (boşluk canlı doğrulamayla
    ///   kapatıldı). Yine de "Kiraci(id) yerine Sistem()" mutasyonu burada yakalanır.
    /// </summary>
    [Fact]
    public void Zamanlayici_is_kosumunu_KIRACI_kapsaminda_yapar()
    {
        var metin = IslerKaynaklari().Single(k => k.Ad == "IsZamanlayici.cs").Metin;

        Assert.Contains("kapsam.Kiraci(kiraciId)", metin, StringComparison.Ordinal);
        // İş çalıştırıcısı sistem kapsamından ÇÖZÜLMEZ; yalnız kiracı kapsamından.
        Assert.DoesNotContain("sistem.ServiceProvider.GetRequiredService<IsCalistirici>", metin, StringComparison.Ordinal);
        Assert.Contains("kiraciKapsami.ServiceProvider.GetRequiredService<IsCalistirici>", metin, StringComparison.Ordinal);
    }

    /// <summary>Sistem kapsamının "izin verilen kullanım" listesi güncellendi mi?</summary>
    [Fact]
    public void SistemKapsami_izinli_kullanim_listesi_zamanlayiciyi_sayar()
    {
        var metin = File.ReadAllText(
            Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api", "Data", "SistemKapsami.cs"));

        Assert.Contains("IsZamanlayici → aktif kiracı listesi", metin, StringComparison.Ordinal);
        Assert.Contains("IsZamanlayici → kiracı başına iş koşumu", metin, StringComparison.Ordinal);
    }
}
