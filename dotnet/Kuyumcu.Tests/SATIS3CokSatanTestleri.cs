using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — doğrulama bulgusu (ORTA, 28.09): işlem ekranının boş odak listesi
// "en çok satılan 8" olmalı. GET /urunler?siralama=cok-satan → son 90 günün SATIS
// hareketi sayısına göre azalan; eşitlikte ürün koduna göre (kararlı). Geçersiz
// sıralama sessizce koda düşmez → 400. Başka kiracının satışı sıralamayı etkilemez.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class SATIS3CokSatanTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static async Task<string> UrunVeStok(KuyumcuDbContext db, Kiraci k, string kod, decimal adet)
    {
        await new UrunServisi(db).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = kod, Ad = "Ürün " + kod, AyarId = "K22", GramAgirlik = 5m,
        }, k.Islem);
        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == kod);
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = urun.Id,
            Adet = adet, ToplamHasGram = adet * urun.HasGrami, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return urun.Id;
    }

    private static Task Sat(KuyumcuDbContext db, Kiraci k, string urunId) =>
        Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urunId, Adet = 1m, BirimFiyatTl = 1_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

    [Fact]
    public async Task Cok_satan_siralamasi_SATIS_sayisina_gore_esitlikte_KOD()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çok satan");
        using var db = vt.Olustur(k.TenantId);
        var a = await UrunVeStok(db, k, "CS-A", 5m);   // 0 satış
        var b = await UrunVeStok(db, k, "CS-B", 5m);   // 3 satış
        var c = await UrunVeStok(db, k, "CS-C", 5m);   // 1 satış
        await UrunVeStok(db, k, "CS-D", 5m);           // 0 satış
        for (var i = 0; i < 3; i++) await Sat(db, k, b);
        await Sat(db, k, c);

        var liste = await new UrunServisi(db).Listele(k.Islem, null, null, true, "CS-", 8, 0,
            siralama: UrunServisi.SiralamaCokSatan);

        Assert.Equal(["CS-B", "CS-C", "CS-A", "CS-D"], liste.Select(u => u.Kod).ToArray());
        Assert.Equal(a, liste[2].Id);

        // Varsayılan sıralama DEĞİŞMEDİ (koda göre).
        var varsayilan = await new UrunServisi(db).Listele(k.Islem, null, null, true, "CS-", 8, 0);
        Assert.Equal(["CS-A", "CS-B", "CS-C", "CS-D"], varsayilan.Select(u => u.Kod).ToArray());
    }

    [Fact]
    public async Task Cok_satan_LIMIT_uygulanir_ve_BASKA_kiracinin_satisi_sayilmaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "Çok satan A");
        var yabanci = TestKurulum.KiraciKur(sistem, "Çok satan B");
        using var db = vt.Olustur(k.TenantId);
        await UrunVeStok(db, k, "CT-A", 5m);
        var z = await UrunVeStok(db, k, "CT-Z", 5m);
        await Sat(db, k, z);
        using (var dbB = vt.Olustur(yabanci.TenantId))
        {
            var y = await UrunVeStok(dbB, yabanci, "CT-A", 5m);   // aynı kod, başka kiracı — 2 satış
            await Sat(dbB, yabanci, y);
            await Sat(dbB, yabanci, y);
        }

        var liste = await new UrunServisi(db).Listele(k.Islem, null, null, true, "CT-", 1, 0,
            siralama: UrunServisi.SiralamaCokSatan);

        Assert.Single(liste);
        Assert.Equal("CT-Z", liste[0].Kod);
        Assert.Equal(k.TenantId, liste[0].TenantId);
    }

    [Fact]
    public async Task Gecersiz_siralama_400_sessizce_KODA_dusmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çok satan geçersiz");
        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new UrunServisi(db).Listele(k.Islem, null, null, true, null, 8, 0, siralama: "fiyat"));
        Assert.Contains("sıralama", hata.Message);
    }
}
