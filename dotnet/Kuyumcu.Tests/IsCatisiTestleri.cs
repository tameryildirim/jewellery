using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İŞ ÇATISI — ÇEKİRDEK DAVRANIŞ (paket 6 Faz A)
//
// Ölçülen şey iş MANTIĞI değil ÇATIDIR: tek koşum, tekrarsız bildirim, hata
// sınıflandırması, elle koşum ayrımı ve kiracı izolasyonu.
//
// ⚠ IsZamanlayici'nin KENDİ döngüsü burada koşmaz (HttpOrtami tüm
//   IHostedService'leri kaldırır, üretimde de döngü ayrı bir süreçtir);
//   mantık IsCalistirici'de olduğu için kapsanır, döngü canlı doğrulamayla.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Testlerin IsCalistirici'yi üretimdeki gibi kurmasını sağlayan yardımcı.</summary>
internal static class IsCatisiKurulum
{
    internal static IConfiguration Yapilandirma(params (string Anahtar, string Deger)[] ayarlar) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(ayarlar.ToDictionary(a => a.Anahtar, a => (string?)a.Deger))
            .Build();

    /// <summary>Kiracıya sabitlenmiş db + işin ihtiyaç duyduğu scoped servisler.</summary>
    internal static ServiceProvider Kapsam(KuyumcuDbContext db, IConfiguration y)
    {
        var s = new ServiceCollection();
        s.AddSingleton(y);
        s.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        s.AddScoped(_ => db);
        s.AddScoped<DenetimServisi>();
        s.AddScoped<Kuyumcu.Api.Modules.Masak.SaklamaSuresiIsi>();
        // PAKET P-11 (B-24) — iş eşikleri artık kiracı ayarından okunur.
        s.AddScoped<IsAyarServisi>();
        return s.BuildServiceProvider();
    }

    internal static IsCalistirici Calistirici(
        KuyumcuDbContext db, IConfiguration y, params IZamanlanmisIs[] isler)
    {
        var kapsam = Kapsam(db, y);
        return new IsCalistirici(db, isler, y, kapsam, new UygulamaIciKanal(),
            kapsam.GetRequiredService<IsAyarServisi>(), NullLogger<IsCalistirici>.Instance);
    }
}

/// <summary>Sayaçlı sahte iş — çatının davranışını ölçmek için.</summary>
internal sealed class SahteIs(string kod, Func<IsBaglami, IsCikti> uret) : IZamanlanmisIs
{
    public int CagriSayisi { get; private set; }
    public string Kod => kod;
    public string Ad => $"Sahte {kod}";
    public bool Etkin(IConfiguration y) => y.GetValue($"Isler:{kod}:Etkin", true);
    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        CagriSayisi++;
        return Task.FromResult(uret(baglam));
    }
}

[Collection("veritabani")]
public sealed class IsCatisiTestleri(TestVeritabani vt)
{
    private static readonly IConfiguration Bos = IsCatisiKurulum.Yapilandirma();

    private Kiraci YeniKiraci(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static BildirimTaslagi Taslak(string anahtar, string? roller = null) =>
        new("SAHTE", BildirimOnem.Uyari, "Sahte başlık", "Sahte gövde", "/uyum", null, null, anahtar, roller);

    // ── Tek koşum ───────────────────────────────────────────────────────────

    /// <summary>M6-1 / M6-2: aynı yerel günde ikinci koşum VERİTABANI tarafından reddedilir.</summary>
    [Fact]
    public async Task Ayni_yerel_gunde_ikinci_kosum_ATLANIR_ve_tek_satir_kalir()
    {
        var k = YeniKiraci("PAKET6-CATI tek koşum");
        using var db = vt.Olustur(k);
        var an = DateTime.UtcNow;
        var i = new SahteIs("TEK_KOSUM", _ => new IsCikti([Taslak($"TEK:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var birinci = await c.Calistir(i, k.TenantId, an, null, default);
        var ikinci = await c.Calistir(i, k.TenantId, an, null, default);

        Assert.False(birinci.Atlandi);
        Assert.True(ikinci.Atlandi);
        Assert.Equal(1, i.CagriSayisi);   // ikinci çağrıda iş hiç koşmadı

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.IsCalismalari
            .CountAsync(x => x.TenantId == k.TenantId && x.IsKodu == "TEK_KOSUM"));
    }

    /// <summary>M6-3 / M6-4: iki tur aynı taslağı üretse de kullanıcı TEK satır görür.</summary>
    [Fact]
    public async Task Iki_tur_ayni_taslagi_uretse_de_TEK_bildirim_olur()
    {
        var k = YeniKiraci("PAKET6-CATI tekillik");
        using var db = vt.Olustur(k);
        var i = new SahteIs("TEKILLIK", _ => new IsCikti([Taslak($"TEKIL:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        // İki AYRI koşum: biri günlük, biri elle (çalışma anahtarları farklı).
        var gunluk = await c.Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
        var elle = await c.Calistir(i, k.TenantId, DateTime.UtcNow, k.KullaniciId, default);

        Assert.Equal(1, gunluk.UretilenBildirim);
        Assert.Equal(0, elle.UretilenBildirim);
        Assert.Equal(1, elle.AtlananBildirim);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Bildirimler.CountAsync(b => b.TenantId == k.TenantId));
        // Elle koşum AYRI bir çalışma satırı açtı (anahtar çakışmadı).
        Assert.Equal(2, await kontrol.IsCalismalari
            .CountAsync(x => x.TenantId == k.TenantId && x.IsKodu == "TEKILLIK"));
    }

    // ── Hata gizleme yasağı ─────────────────────────────────────────────────

    /// <summary>İş istisna atarsa koşum HATALI yazılır; tur DÜŞMEZ ve sır sızmaz.</summary>
    [Fact]
    public async Task Is_istisna_atarsa_durum_HATALI_ve_hata_ozeti_SIR_TASIMAZ()
    {
        const string sir = "server=localhost;password=COK-GIZLI";
        var k = YeniKiraci("PAKET6-CATI hata");
        var digerKiraci = YeniKiraci("PAKET6-CATI hata sonraki");

        var patlayan = new SahteIs("PATLAYAN", _ => throw new InvalidOperationException(sir));
        var saglam = new SahteIs("SAGLAM", _ => new IsCikti([Taslak($"SAGLAM:{digerKiraci.TenantId}")], 1));

        using (var db = vt.Olustur(k))
        {
            var c = IsCatisiKurulum.Calistirici(db, Bos, patlayan);
            var sonuc = await c.Calistir(patlayan, k.TenantId, DateTime.UtcNow, null, default);
            Assert.Equal(IsDurum.Hatali, sonuc.Durum);
            Assert.False(string.IsNullOrWhiteSpace(sonuc.HataOzeti));
            Assert.DoesNotContain("password", sonuc.HataOzeti!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GIZLI", sonuc.HataOzeti!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("InvalidOperationException", sonuc.HataOzeti!, StringComparison.Ordinal);
        }

        // Sonraki kiracının işi yine koşar — tur düşmedi.
        using (var db2 = vt.Olustur(digerKiraci))
        {
            var c2 = IsCatisiKurulum.Calistirici(db2, Bos, saglam);
            var sonuc2 = await c2.Calistir(saglam, digerKiraci.TenantId, DateTime.UtcNow, null, default);
            Assert.Equal(IsDurum.Basarili, sonuc2.Durum);
            Assert.Equal(1, sonuc2.UretilenBildirim);
        }

        using var kontrol = vt.OlusturSistem();
        var satir = await kontrol.IsCalismalari.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.IsKodu == "PATLAYAN");
        Assert.Equal(IsDurum.Hatali, satir.Durum);
        Assert.NotNull(satir.HataOzeti);
        Assert.True(satir.HataOzeti!.Length <= 500);
        Assert.NotNull(satir.Bitis);
    }

    /// <summary>
    /// Aşkın'ın koşulu: BAŞKA bir DbUpdateException (kolon taşması, 1406)
    /// "zaten çalıştı" sayılamaz — HATALI yazılır. Her istisnayı yutmak md.1 ihlali.
    /// </summary>
    [Fact]
    public async Task Benzersizlik_DISI_veritabani_hatasi_ATLANDI_sayilmaz_HATALI_yazilir()
    {
        var k = YeniKiraci("PAKET6-CATI 1406");
        using var db = vt.Olustur(k);

        // tekillik_anahtari VARCHAR(160); 200 karakter taşırır (MySQL 1406).
        var uzun = new string('X', 200);
        var i = new SahteIs("TASAN", _ => new IsCikti([Taslak(uzun)], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var sonuc = await c.Calistir(i, k.TenantId, DateTime.UtcNow, null, default);

        Assert.False(sonuc.Atlandi);
        Assert.Equal(IsDurum.Hatali, sonuc.Durum);
        Assert.Equal(0, sonuc.UretilenBildirim);
        Assert.Equal(0, sonuc.AtlananBildirim);
        Assert.Contains("VERITABANI_HATASI", sonuc.HataOzeti!, StringComparison.Ordinal);
    }

    // ── Kiracı izolasyonu ───────────────────────────────────────────────────

    /// <summary>M6-9 karşıtı: A kiracısının bildirimi B'nin listesinde YOKTUR.</summary>
    [Fact]
    public async Task Iki_kiracida_ayni_kosullar_bildirimler_KARISMAZ()
    {
        var a = YeniKiraci("PAKET6-CATI kiracı A");
        var b = YeniKiraci("PAKET6-CATI kiracı B");
        var i = new SahteIs("IZOLASYON", bg => new IsCikti([Taslak($"IZO:{bg.TenantId}")], 1));

        using (var dbA = vt.Olustur(a))
            await IsCatisiKurulum.Calistirici(dbA, Bos, i).Calistir(i, a.TenantId, DateTime.UtcNow, null, default);
        using (var dbB = vt.Olustur(b))
            await IsCatisiKurulum.Calistirici(dbB, Bos, i).Calistir(i, b.TenantId, DateTime.UtcNow, null, default);

        // Kiracı bağlamından okurken KARŞI kiracının satırı GÖRÜNMEZ.
        using var okuA = vt.Olustur(a);
        var listeA = await okuA.Bildirimler.AsNoTracking().ToListAsync();
        Assert.Single(listeA);
        Assert.Equal(a.TenantId, listeA[0].TenantId);
        Assert.DoesNotContain(listeA, x => x.TenantId == b.TenantId);

        using var okuB = vt.Olustur(b);
        var listeB = await okuB.Bildirimler.AsNoTracking().ToListAsync();
        Assert.Single(listeB);
        Assert.Equal(b.TenantId, listeB[0].TenantId);
    }

    // ── Kapalı iş ───────────────────────────────────────────────────────────

    /// <summary>M6-8: kapalı iş HİÇ koşmaz — çalışma satırı bile açılmaz.</summary>
    [Fact]
    public async Task Kapali_is_hic_kosmaz_calisma_satiri_bile_acilmaz()
    {
        var k = YeniKiraci("PAKET6-CATI kapalı iş");
        using var db = vt.Olustur(k);
        var y = IsCatisiKurulum.Yapilandirma(("Isler:KAPALI_IS:Etkin", "false"));
        var i = new SahteIs("KAPALI_IS", _ => new IsCikti([Taslak($"KAP:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, y, i);

        Assert.False(c.Etkin(i));
        Assert.Empty(c.EtkinIsler);
        var sonuclar = await c.TumEtkinIsleriCalistir(k.TenantId, DateTime.UtcNow, default);

        Assert.Empty(sonuclar);
        Assert.Equal(0, i.CagriSayisi);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.IsCalismalari.CountAsync(x => x.TenantId == k.TenantId));
    }
}
