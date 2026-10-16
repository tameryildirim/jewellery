using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MGider = Kuyumcu.Api.Modules.Gider;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-44 — NAKİT KASA EKSİYE DÜŞEMEZ (KasaBakiyeKapisi)
//
// Çekmecede olmayan para ödenemez. Kural kâğıtta vardı ama kodda yalnız gider
// ve virman yollarında elle tekrarlanmıştı; alış, hurda alımı, iade, para üstü
// ve elle kasa çıkışı KAPISIZDI.
//
// Canlı ölçüm (2026-09-23, kuyumcu_net · kasa 01M3227XT7DXVZKS9HC54MBYAN):
//   bakiye −738.298,80 TL · defter toplamı −738.298,80 TL (defter tutarlı)
//   dört ALIS  −858.500,00 TL   ·   bir HURDA_ALIS  −24.548,80 TL
// Yani sızıntı SatisModulu.KasaGuncelle'nin negatif delta çağrılarındaydı.
//
// GEREKÇELİ İSTİSNA: storno ters kaydı kapıya TABİ DEĞİLDİR — bir hareketin
// tersi her zaman yazılabilmelidir, düzeltme yolu bakiyeye bağlı olamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KasaBakiyeKapisiTestleri(TestVeritabani vt)
{
    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance);

    private static MGider.VirmanServisi Virman(KuyumcuDbContext db) =>
        new(db, NullLogger<MGider.VirmanServisi>.Instance, TestKurulum.Denetim(db));

    private static MKasa.KasaHareketDto Cikis(decimal tutar) => new()
    {
        Yon = Kuyumcu.Core.Domain.HareketYonu.KasaCikis,
        Tutar = tutar,
        OdemeTuru = "NAKIT",
        Aciklama = "P-44 kapı testi",
    };

    private static MKasa.KasaHareketDto Giris(decimal tutar) => new()
    {
        Yon = Kuyumcu.Core.Domain.HareketYonu.KasaGiris,
        Tutar = tutar,
        OdemeTuru = "NAKIT",
        Aciklama = "P-44 kapı testi",
    };

    private static decimal Bakiye(TestVeritabani vt, string kasaId)
    {
        using var kontrol = vt.OlusturSistem();
        return TestKurulum.KasaBakiye(kontrol, kasaId);
    }

    // ── Elle kasa çıkışı ─────────────────────────────────────────────────────

    [Fact]
    public async Task Elle_kasa_cikisi_bakiyeyi_negatife_dusuremez_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 elle çıkış");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Kasa(db).HareketGir(k.KasaId, Cikis(TestKurulum.KasaAcilis + 0.01m), k.Islem));

        Assert.Contains("bakiyesi yetersiz", hata.Message);
        Assert.Contains("Nakit kasa eksiye düşemez", hata.Message);

        // Reddedilen istek hiçbir şey yazmamalı.
        Assert.Equal(TestKurulum.KasaAcilis, Bakiye(vt, k.KasaId));
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.KasaHareketleri.Where(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Elle_kasa_cikisi_bakiye_kadarsa_200_ve_bakiye_sifir()
    {
        // Sınır: bakiyenin TAMAMI çekilebilir. Kapı "<= 0" olsaydı kuyumcu
        // kasasını hiç boşaltamazdı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 tam boşaltma");
        using var db = vt.Olustur(k);

        await Kasa(db).HareketGir(k.KasaId, Cikis(TestKurulum.KasaAcilis), k.Islem);

        Assert.Equal(0m, Bakiye(vt, k.KasaId));
    }

    // ── Hurda alımı (SatisModulu negatif delta) ──────────────────────────────

    [Fact]
    public async Task Hurda_alimi_kasa_bakiyesini_asarsa_400()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 hurda"));
        using var db = vt.Olustur(k);

        // Kasayı meşru yoldan 1.000 TL'ye indir.
        await Kasa(db).HareketGir(k.KasaId, Cikis(TestKurulum.KasaAcilis - 1_000m), k.Islem);
        Assert.Equal(1_000m, Bakiye(vt, k.KasaId));

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId,
                KurSnapshotId = k.KurId, Satici = TestKurulum.HurdaSatici(),
            }, k.Islem));

        Assert.Contains("bakiyesi yetersiz", hata.Message);
        Assert.Equal(1_000m, Bakiye(vt, k.KasaId));
    }

    // ── Satış iadesi (SatisModulu negatif delta) ─────────────────────────────

    [Fact]
    public async Task Satis_iadesi_kasa_bakiyesini_asarsa_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 iade");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        var satisId = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();

        // Para meşru yoldan dışarı çıktı: kasada iadeyi karşılayacak nakit yok.
        await Kasa(db).HareketGir(k.KasaId, Cikis(253_000m), k.Islem);
        Assert.Equal(2_000m, Bakiye(vt, k.KasaId));

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisIade(new MSatis.IadeDto
            {
                KaynakHareketId = satisId, Adet = 1m, OdemeTuru = "NAKIT",
                KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));

        Assert.Contains("bakiyesi yetersiz", hata.Message);
        Assert.Equal(2_000m, Bakiye(vt, k.KasaId));
    }

    // ── Gerekçeli istisna: storno ────────────────────────────────────────────

    [Fact]
    public async Task Storno_negatif_bakiyede_bile_200_doner()
    {
        // Defter bütünlüğü: bir hareketin TERSİ her zaman yazılabilmelidir.
        // Aksi hâlde yanlış girilmiş bir kasa girişi, bakiye yetmediğinde
        // defterde ÇAKILI kalırdı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 storno");
        using var db = vt.Olustur(k);

        await Kasa(db).HareketGir(k.KasaId, Giris(10_000m), k.Islem);
        string hareketId;
        using (var kontrol = vt.OlusturSistem())
            hareketId = kontrol.KasaHareketleri
                .Single(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId).Id;

        // Kasa zaten eksideyken (canlı fikstürdeki hâl) storno yine çalışmalı.
        using (var yaz = vt.OlusturSistem())
        {
            var kasa = yaz.Kasalar.Single(x => x.Id == k.KasaId);
            kasa.Bakiye = -5_000m;
            yaz.SaveChanges();
        }

        using var db2 = vt.Olustur(k);
        await Storno(db2).KasaHareketiStorno(
            k.KasaId, hareketId,
            "P-44: yanlış girilen kasa girişi geri alınıyor (defter bütünlüğü sınaması)",
            k.Islem);

        // Ters kayıt yazıldı: −5.000 − 10.000 = −15.000
        Assert.Equal(-15_000m, Bakiye(vt, k.KasaId));
    }

    // ── Kapsam dışı: banka hesabı (KMH gerekçesi) ────────────────────────────

    [Fact]
    public async Task Banka_hesabi_kapiya_tabi_degil_200()
    {
        // KMH (kredili mevduat) negatif bakiyeye izin verir; kuralı kurucu/mali
        // müşavir belirler. Bu yüzden banka tarafına kapı KONMADI.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 banka");
        var hesapId = Kimlik.Ulid();
        using (var kur = vt.OlusturSistem())
        {
            kur.BankaHesaplari.Add(new BankaHesap
            {
                Id = hesapId, TenantId = k.TenantId, BankaAdi = "Test Bankası",
                HesapAdi = "Vadesiz", Iban = "TR200000000000000000000001",
                ParaBirimi = "TRY", Bakiye = 100m, Aktif = true,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            kur.SaveChanges();
        }

        using var db = vt.Olustur(k);
        await Virman(db).Olustur(new MGider.VirmanDto
        {
            KaynakTur = "BANKA", KaynakId = hesapId,
            HedefTur = "KASA", HedefId = k.KasaId,
            Tutar = 5_000m, Aciklama = "P-44 banka kapsam dışı sınaması",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-4_900m, kontrol.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesapId).Bakiye);
        Assert.Equal(TestKurulum.KasaAcilis + 5_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ── Değişmez: bakiye defterden türetilebilir kalır ───────────────────────

    [Fact]
    public async Task Kasa_hareket_toplami_her_zaman_bakiyeye_esit()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P-44 defter");
        using var db = vt.Olustur(k);

        await Kasa(db).HareketGir(k.KasaId, Giris(12_000m), k.Islem);
        await Kasa(db).HareketGir(k.KasaId, Cikis(7_500m), k.Islem);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 3_250m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        // Kapıya takılan istek deftere de bakiyeye de DOKUNMAZ.
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Kasa(db).HareketGir(k.KasaId, Cikis(1_000_000m), k.Islem));

        using var kontrol = vt.OlusturSistem();
        var defter = kontrol.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId)
            .Sum(h => h.Tutar);
        Assert.Equal(TestKurulum.KasaAcilis + defter, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(7_750m, defter);
    }
}
