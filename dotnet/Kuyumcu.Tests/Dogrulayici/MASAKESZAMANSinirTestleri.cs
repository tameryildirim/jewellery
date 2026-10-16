using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MBanka = Kuyumcu.Api.Modules.Banka;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET MASAK-ESZAMAN finansal sınır testleri.
// Beklenen değerler SABİT sayıdır (üretim kodundan hesaplanmaz). Varsayılan motor:
// amaç beyanı kademesi 200.000 TL (MasakEsikleri varsayılanı) = kimlik isteyen en düşük tutar.
// Eşzamanlılık: her istek ayrı DbContext (ayrı MySQL bağlantısı), ortak başlangıç kapısı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MASAKESZAMANSinirTestleri(TestVeritabani vt)
{
    private const decimal Esik = 200_000m;   // SABİT beklenen (varsayılan motor, amaç beyanı kademesi)

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETMASAK-ESZAMAN-TEST DOG " + ad, stokAdet: 50m);
        sistem.SaveChanges();
        return k;
    }

    private async Task<List<Exception?>> Paralel(int n, Func<KuyumcuDbContext, int, Task> is_)
    {
        var baglamlar = Enumerable.Range(0, n).Select(_ => vt.OlusturSistem()).ToList();
        try
        {
            var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gorevler = baglamlar.Select((db, i) => Task.Run(async () =>
            {
                await kapi.Task;
                try { await is_(db, i); return (Exception?)null; }
                catch (Exception e) { return e; }
            })).ToList();
            kapi.SetResult();
            return [.. await Task.WhenAll(gorevler)];
        }
        finally { foreach (var b in baglamlar) b.Dispose(); }
    }

    private static MCari.TahsilatGirisDto Havale(Kiraci k, decimal tl) => new()
    {
        TlTutari = tl, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private async Task<decimal> Toplam(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow);
    }

    private async Task<int> TahsilatSayisi(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
    }

    private static void HepsiMasakReddi(IEnumerable<Exception?> sonuc)
    {
        var yabanci = sonuc.Where(e => e is not null && !(e is BadRequestException b && b.Message.StartsWith("MASAK"))).ToList();
        Assert.True(yabanci.Count == 0, "Beklenmeyen istisna: " + string.Join(" | ", yabanci.Select(e => e!.GetType().Name + ": " + e.Message)));
    }

    // ── SIFIR ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task S1_sifir_tutarli_tahsilat_reddedilir_satir_yazilmaz()
    {
        var k = Kur("S1");
        using var db = vt.OlusturSistem();
        await Assert.ThrowsAsync<BadRequestException>(() => Cari(db).TahsilatGirHareket(k.CariId, Havale(k, 0m), k.Islem));
        Assert.Equal(0, await TahsilatSayisi(k));
        Assert.Equal(0m, await Toplam(k));
    }

    // ── EKSİ ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task S2_eksi_tutar_on_paralelde_hepsi_reddedilir_kumulatif_bozulmaz()
    {
        var k = Kur("S2");
        var sonuc = await Paralel(10, (db, _) => Cari(db).TahsilatGirHareket(k.CariId, Havale(k, -40_000m), k.Islem));
        Assert.All(sonuc, e => Assert.IsType<BadRequestException>(e));
        Assert.Equal(0, await TahsilatSayisi(k));
        Assert.Equal(0m, await Toplam(k));
    }

    // ── KURUŞ (AwayFromZero, 0,005 sınırı) ───────────────────────────────────
    [Fact]
    public async Task S3a_kurus_alti_eszamanli_199999_99_tek_kabul_esik_asilmaz()
    {
        var k = Kur("S3a");
        using (var db0 = vt.OlusturSistem())
            await Cari(db0).TahsilatGirHareket(k.CariId, Havale(k, 160_000m), k.Islem);
        // 39.999,994 → 39.999,99 (AwayFromZero 2 hane). 160.000 + 39.999,99 = 199.999,99 < 200.000 → TEK kabul.
        var sonuc = await Paralel(10, (db, _) => Cari(db).TahsilatGirHareket(k.CariId, Havale(k, 39_999.994m), k.Islem));
        HepsiMasakReddi(sonuc);
        Assert.Equal(1, sonuc.Count(e => e is null));
        Assert.Equal(199_999.99m, await Toplam(k));
    }

    [Fact]
    public async Task S3b_kurus_0_005_yukari_yuvarlanir_esige_ulasan_beyansiz_reddedilir()
    {
        var k = Kur("S3b");
        using (var db0 = vt.OlusturSistem())
            await Cari(db0).TahsilatGirHareket(k.CariId, Havale(k, 160_000m), k.Islem);
        // 39.999,995 → 40.000,00 → kümülatif 200.000,00 = eşik → beyansız 400 (yuvarlama SONRASI ölçülür).
        var sonuc = await Paralel(5, (db, _) => Cari(db).TahsilatGirHareket(k.CariId, Havale(k, 39_999.995m), k.Islem));
        HepsiMasakReddi(sonuc);
        Assert.Equal(0, sonuc.Count(e => e is null));
        Assert.Equal(160_000m, await Toplam(k));
    }

    // ── HAS 4 HANE (0,0001 g) — kilit altında kayıp güncelleme yok ───────────
    [Fact]
    public async Task S4_has_0_0001_on_paralel_hepsi_yazilir_bakiye_0_0010()
    {
        var k = Kur("S4");
        decimal Has()
        {
            using var db = vt.OlusturSistem();
            return db.CariBakiyeler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.CariId == k.CariId)
                .Select(b => b.HasGramBakiye).FirstOrDefault();
        }
        var once = Has();
        var sonuc = await Paralel(10, (db, _) => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            HasGrami = 0.0001m, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.True(sonuc.All(e => e is null), string.Join(" | ", sonuc.Where(e => e is not null).Select(e => e!.Message)));
        Assert.Equal(0.0010m, Math.Abs(Has() - once));
        Assert.Equal(10, await TahsilatSayisi(k));
    }

    // ── ÇOK BÜYÜK TUTAR ──────────────────────────────────────────────────────
    [Fact]
    public async Task S5_999_999_999_99_beyansiz_eszamanli_hepsi_reddedilir()
    {
        var k = Kur("S5");
        var sonuc = await Paralel(3, (db, _) => Cari(db).TahsilatGirHareket(k.CariId, Havale(k, 999_999_999.99m), k.Islem));
        Assert.All(sonuc, e => Assert.IsType<BadRequestException>(e));
        Assert.Equal(0, await TahsilatSayisi(k));
        Assert.Equal(0m, await Toplam(k));
    }

    // ── BOŞ / NULL cari ──────────────────────────────────────────────────────
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task S6_bos_cari_yalniz_read_committed_islem_acar_yineleme_faz1_doner(string? cariId)
    {
        var k = Kur("S6" + (cariId is null ? "n" : "e"));
        using var db = vt.OlusturSistem();
        await using var tx = await db.Database.CariIcinBaslat(k.TenantId, cariId, DateTime.UtcNow);
        Assert.NotNull(tx);
        Assert.Equal(System.Data.IsolationLevel.ReadCommitted,
            Microsoft.EntityFrameworkCore.Storage.DbContextTransactionExtensions.GetDbTransaction(tx!).IsolationLevel);
        var faz1 = new MMasak.MasakGereksinim(BeyanKademesi.Yok, false, false, false, false, 0);
        var sonuc = await MMasak.MasakKapisi.Varsayilan().KilitAltindaYinele(db, k.TenantId, cariId, 500_000m, null, DateTime.UtcNow, faz1);
        Assert.Same(faz1, sonuc);
    }

    // ── ÇAPRAZ YOL: aynı cariye tahsilat + banka dekontu AYNI ANDA ───────────
    [Fact]
    public async Task S7_capraz_yol_5_tahsilat_5_dekont_40000_tam_4_kabul_160000()
    {
        var k = Kur("S7");
        var hesapId = Kimlik.Ulid();
        using (var db0 = vt.OlusturSistem())
        {
            db0.BankaHesaplari.Add(new BankaHesap
            {
                Id = hesapId, TenantId = k.TenantId, BankaAdi = "Test Bankası", HesapAdi = "Vadesiz",
                Iban = "TR900000000000000000000002", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            db0.SaveChanges();
        }
        var sonuc = await Paralel(10, (db, i) => i % 2 == 0
            ? Cari(db).TahsilatGirHareket(k.CariId, Havale(k, 40_000m), k.Islem)
            : new MBanka.BankaServisi(db).HareketEkleIc(hesapId, new MBanka.HareketEkleDto
            {
                Yon = HareketYonu.BankaGelen, Tutar = 40_000m, Aciklama = "çapraz " + i, CariId = k.CariId,
            }, k.Islem));
        HepsiMasakReddi(sonuc);
        Assert.Equal(4, sonuc.Count(e => e is null));
        Assert.Equal(160_000m, await Toplam(k));
    }

    // ── TÜRKÇE GİRİŞ (FormAlani.SayiCoz) ─────────────────────────────────────
    [Fact]
    public void S8_turkce_giris_45_000_ve_1_5_cozulur_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.False(FormAlani.SayiCoz("1.500").Tamam);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }
}
