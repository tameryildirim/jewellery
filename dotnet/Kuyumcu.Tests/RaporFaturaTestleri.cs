using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MRapor = Kuyumcu.Api.Modules.Rapor;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// K5 — Raporların gün sınırı İstanbul saatine göre olmalı (kayıtlar UTC).
// Fatura — kuyum KDV'si ve fatura numarası üretimi.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RaporTestleri(TestVeritabani vt)
{
    /// <summary>Belirli bir UTC anına satış hareketi yazar.</summary>
    private static void SatisHareketiYaz(KuyumcuDbContext db, Kiraci k, DateTime utcAn, decimal tl)
    {
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = utcAn, TlTutari = tl, HasGrami = -1m, KurSnapshotId = k.KurId,
            KullaniciId = k.KullaniciId, Olusturuldu = utcAn,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GunlukOzet_IstanbulGecesindeYapilanSatisAyniYerelGundeGorunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Gece mesaisi");
        using var db = vt.Olustur(k);

        // 2026-09-10 22:30 UTC = İstanbul'da 2026-09-11 01:30
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 10, 22, 30, 0, DateTimeKind.Utc), 1_000m);

        var rapor = TestKurulum.RaporServisi(db);
        var ozet = await rapor.GunlukOzet("2026-09-11", k.Islem);

        Assert.Equal(1_000m, TestKurulum.Sayi(ozet, "satis", "toplamTl"));
    }

    [Fact]
    public async Task GunlukOzet_OncekiYerelGunuKarismaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Gün sınırı");
        using var db = vt.Olustur(k);

        // İstanbul 2026-09-11 01:30 → 10 Eylül raporunda GÖRÜNMEMELİ
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 10, 22, 30, 0, DateTimeKind.Utc), 1_000m);
        // İstanbul 2026-09-10 23:00 → 10 Eylül raporunda GÖRÜNMELİ
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 10, 20, 0, 0, DateTimeKind.Utc), 500m);

        var rapor = TestKurulum.RaporServisi(db);
        var onu = await rapor.GunlukOzet("2026-09-10", k.Islem);
        var onbiri = await rapor.GunlukOzet("2026-09-11", k.Islem);

        Assert.Equal(500m, TestKurulum.Sayi(onu, "satis", "toplamTl"));
        Assert.Equal(1_000m, TestKurulum.Sayi(onbiri, "satis", "toplamTl"));
    }

    [Fact]
    public async Task AylikOzet_AyinIlkGecesiDogruAyaDuser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Ay sınırı");
        using var db = vt.Olustur(k);

        // İstanbul 2026-10-01 01:00 → Ekim ayına ait
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc), 700m);

        var rapor = TestKurulum.RaporServisi(db);
        var eylul = await rapor.AylikOzet(2026, 9, k.Islem);
        var ekim = await rapor.AylikOzet(2026, 10, k.Islem);

        Assert.Equal(0m, TestKurulum.Sayi(eylul, "satis", "toplamTl"));
        Assert.Equal(700m, TestKurulum.Sayi(ekim, "satis", "toplamTl"));
    }

    [Fact]
    public async Task AylikOzet_GunlukKirilimDoner()
    {
        // SIRADAKI-ADIM-KARAR Ö3: Panel.razor:230 ve Raporlar.razor:187 `gunler` bekliyordu,
        // AylikOzet hiç döndürmüyordu → iki sayfada grafik kalıcı boştu.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Günlük kırılım");
        using var db = vt.Olustur(k);

        // 3 Eylül İstanbul 14:00 → 1.000; 5 Eylül İstanbul 02:00 (UTC 4 Eylül 23:00) → 250 (gün sınırı yerel)
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 3, 11, 0, 0, DateTimeKind.Utc), 1_000m);
        SatisHareketiYaz(db, k, new DateTime(2026, 9, 4, 23, 0, 0, DateTimeKind.Utc), 250m);

        var ozet = await TestKurulum.RaporServisi(db).AylikOzet(2026, 9, k.Islem);
        var gunler = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(ozet))
            .RootElement.GetProperty("gunler").EnumerateArray()
            .ToDictionary(g => g.GetProperty("tarih").GetString()!, g => g.GetProperty("satisTl").GetDecimal());

        Assert.Equal(1_000m, gunler["2026-09-03"]);
        Assert.Equal(250m, gunler["2026-09-05"]);   // UTC 4'ü ama yerel 5'i
        Assert.Equal(0m, gunler["2026-09-04"]);     // boş gün sıfır dolgulu, eksik değil
        Assert.Equal("2026-09-01", gunler.Keys.Min());
        Assert.True(gunler.Count >= 5);             // en az bugüne kadar
    }
}

[Collection("veritabani")]
public sealed class FaturaTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task KuyumKdv_HasBedeliMatrahDisiKalir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Kuyum KDV");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Tur = "SATIS", Toplam = 385_676.63m, KdvOran = 0.20m,
            KdvTuru = "KUYUM", HasDeger = 381_476.63m,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        // KDV yalnız 4.200 TL işçilik matrahından: 4200 − 4200/1,20 = 700,00
        Assert.Equal(700.00m, fatura.KdvTutar);
        Assert.Equal(384_976.63m, fatura.AraToplam);
    }

    [Fact]
    public async Task FaturaNo_MevcutNumarayiTekrarUretmemeli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Fatura no");
        using var db = vt.Olustur(k);
        var yil = DateTime.UtcNow.Year;

        // Deftere elle 2 numaralı fatura konur (1 numara hiç kullanılmamış).
        // Sayaç COUNT+1 ile üretilirse ikinci fatura yine "000002" olur → çakışma.
        db.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaNo = $"FTR-{yil}-000002",
            CariId = k.CariId, Tur = "SATIS", KdvTuru = "STANDART", HasDeger = 0m,
            AraToplam = 100m, KdvOran = 0.20m, KdvTutar = 20m, Toplam = 120m,
            ParaBirimi = "TRY", Durum = "KESILDI", Tarih = DateTime.UtcNow,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();

        var servis = new MFatura.FaturaServisi(db);
        var taslak = await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Tur = "SATIS", Toplam = 1_200m, KdvOran = 0.20m,
        }, k.Islem);

        // ⚠ PAKET P-22 / B-3: TASLAK belge numara TÜKETMEZ. Numara yalnız
        //   KESİLDİĞİNDE verilir; sayaç kuralı (MAX+1) ORADA sınanır.
        var taslakId = (string)taslak.GetType().GetProperty("Id")!.GetValue(taslak)!;
        using (var ara = vt.OlusturSistem())
            Assert.Null(ara.Faturalar.Single(f => f.Id == taslakId).FaturaNo);

        await servis.DurumGuncelle(taslakId, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var numaralar = kontrol.Faturalar.Where(f => f.TenantId == k.TenantId)
            .Select(f => f.FaturaNo).ToList();
        Assert.Equal(2, numaralar.Count);
        Assert.Equal(numaralar.Count, numaralar.Distinct().Count());
        Assert.Contains($"FTR-{yil}-000003", numaralar);
    }

    [Fact]
    public async Task FaturaDurumu_IptalSonrasiDegistirilemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Fatura durumu");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        var f = await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Tur = "SATIS", Toplam = 1_200m, KdvOran = 0.20m,
        }, k.Islem);
        var id = kontrolId(db, k.TenantId);

        await servis.DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "IPTAL" }, k.Islem);
        await Assert.ThrowsAnyAsync<ApiException>(() =>
            servis.DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem));

        static string kontrolId(KuyumcuDbContext db, string tenantId) =>
            db.Faturalar.AsNoTracking().First(x => x.TenantId == tenantId).Id;
    }
}
