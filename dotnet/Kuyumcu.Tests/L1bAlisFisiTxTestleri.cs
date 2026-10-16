using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1b — ALIŞ FİŞİ: YAZIM AÇIK TRANSACTION İÇİNDE (servis düzeyi, gerçek MySQL)
//
// ⚠ NEDEN BU TEST (Anayasa md.1 — mutasyon M-L1b-3 kaydı): alış fişi bugün TEK
//   SaveChangesAsync ile yazar; EF bu tek çağrıyı zaten örtük bir transaction'da
//   yürütür. Bu yüzden "transaction'ı kaldır" mutasyonu HTTP atomiklik testini
//   (L1bH_06/07) KIRMAZ — o mutasyon bugünkü kodda EŞDEĞERDİR. Açık transaction'ın
//   işi, yazım döngüsüne ileride bir ara kayıt (flush) eklendiğinde "ya hep ya hiç"
//   sözünü korumaktır. Bu test o sözleşmeyi kilitler: SaveChanges anında açık bir
//   transaction VARDIR (EF'in örtük transaction'ı SavingChanges'ten SONRA açılır,
//   burada görünmez) ve fiş yazıldıktan sonra açık kalmaz (commit edilmiştir).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class L1bAlisFisiTxTestleri(TestVeritabani vt)
{
    private sealed class TransactionGozcusu : SaveChangesInterceptor
    {
        public readonly List<bool> AcikMi = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            AcikMi.Add(eventData.Context!.Database.CurrentTransaction is not null);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>TestVeritabani.Kur ile AYNI boru hattı + gözcü (TestOrtami'ye dokunmadan).</summary>
    private KuyumcuDbContext Baglam(Kiraci k, TransactionGozcusu gozcu)
    {
        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(vt.Baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<IModelCacheKeyFactory, KiraciModelOnbellegi>()
            .AddInterceptors(
                new KiraciYazmaDenetcisi(NullLogger<KiraciYazmaDenetcisi>.Instance),
                new DenetimYazmaDinleyicisi(new HttpContextAccessor()),
                new MasakHashDinleyicisi(),
                gozcu)
            .Options;
        return new KuyumcuDbContext(ayar, new SabitKiraciBaglami(k.TenantId), TestVeritabani.TestSifreleyici);
    }

    [Fact]
    public async Task L1bT_01_alis_fisi_SaveChanges_ACIK_transaction_icinde_ve_sonra_commit_edilmis()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETL1b-TEST-tx");
        var gozcu = new TransactionGozcusu();
        await using var db = Baglam(k, gozcu);
        var servis = new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance);

        var dto = new MSatis.AlisFisiDto
        {
            CariId = k.CariId, KasaId = k.KasaId, OdemeTuru = "HAVALE", KurSnapshotId = k.KurId,
            Kalemler =
            [
                new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m },
                new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 500m },
            ],
        };
        await servis.AlisFisi(dto, k.Islem);

        Assert.Equal([true], gozcu.AcikMi);            // TEK SaveChanges, açık transaction İÇİNDE
        Assert.Null(db.Database.CurrentTransaction);   // yöntem kendi açtığını kapattı (commit)

        await using var kontrol = vt.OlusturSistem();
        var satirlar = kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS").ToList();
        Assert.Equal(2, satirlar.Count);
        Assert.Single(satirlar.Select(s => s.FisId).Distinct());
        Assert.Equal(-2_000m, satirlar.Sum(s => s.TlTutari));
    }
}
