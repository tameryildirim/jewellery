using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A (20. tur) — denetim.sh P-23 ODEME_KALEMSIZ_YOL (canlı) KIRMIZI:
// 0,00 TL'lik, ödeme kalemi olmayan KART/NAKIT satışı kaydedilebiliyordu
// (TEST-KESIF kiracısı, hareket 01M3QG1P6EVK9HM5CVRTCBKWXJ).
//
// Kök neden: tek etiketli yollar hareketi ödeme aracıyla etiketliyor, kalem
// fabrikası yalnız |tutar| > 0 iken kalem doğuruyor; BirimFiyatTl [Range(0,…)]
// 0'ı kabul ediyordu. Kural tek kaynaktadır: MSatis.OdemeTutarKapisi.
//   • ödemeli işlem 0 TL olamaz → 400 (satış · satış fişi ·
//     hurda alış · hurda satış), servis yolunda da (DTO'ya güvenilmez);
//   • para el değiştirmeyen meşru işlem (fark 0 takas, bedelsiz iade, bedelsiz mal girişi) ödeme
//     ETİKETİ taşımaz → kalemsiz etiketli satır hiçbir yoldan doğamaz.
// Her test denetim.sh sayacının test içi karşılığını (KalemsizYol) da ölçer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class OdemeTutarKapisiSafTestleri
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(0.004, true)]
    [InlineData(-0.004, true)]
    [InlineData(0.005, false)]
    [InlineData(-12.5, false)]
    public void ParaYok_iki_haneye_yuvarlanmis_mutlak_tutara_bakar(double tutar, bool beklenen)
        => Assert.Equal(beklenen, MSatis.OdemeTutarKapisi.ParaYok((decimal)tutar));

    [Fact]
    public void SifirOlamaz_Turkce_sebep_ve_ALAN_tasir()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeTutarKapisi.SifirOlamaz(0m, "Satış", "Birim fiyatı girin.", "birimFiyatTl"));
        Assert.Equal("Satış tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez. Birim fiyatı girin.", ex.Message);
        Assert.Equal("birimFiyatTl", AlanIsareti.Alan(ex));
        MSatis.OdemeTutarKapisi.SifirOlamaz(0.01m, "Satış", "x", "y");   // 1 kuruş geçer
    }

    [Fact]
    public void Etiket_para_yoksa_NULL_varsa_AYNEN()
    {
        Assert.Null(MSatis.OdemeTutarKapisi.Etiket(0m, "NAKIT"));
        Assert.Equal("KART", MSatis.OdemeTutarKapisi.Etiket(-5m, "KART"));
    }
}

[Collection("veritabani")]
public sealed class ZOISLEMAOdemeTutarKapisiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    /// <summary>denetim.sh P-23 ODEME_KALEMSIZ_YOL sorgusunun test içi karşılığı (P23c ile aynı ölçüt).</summary>
    private int KalemsizYol(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        var hepsi = kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId).ToList();
        var kalemHareketleri = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).Select(x => x.HareketId).ToList();
        var stornolananlar = hepsi.Where(h => h.StornoKaynakId is not null).Select(h => h.StornoKaynakId!).ToHashSet();
        return hepsi.Count(h =>
            ((h.NakitTutar ?? 0m) + (h.VeresiyeTutar ?? 0m) > 0m || OdemeAraci.Gecerli(h.OdemeTuru))
            && h.StornoKaynakId is null && !stornolananlar.Contains(h.Id) && !kalemHareketleri.Contains(h.Id));
    }

    private int HareketSayisi(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == tur);
    }

    private static MSatis.SatisOlusturDto SatisDto(Kiraci k, decimal fiyat, string odeme = "KART") => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = fiyat,
        OdemeTuru = odeme, KasaId = odeme == "VERESIYE" ? null : k.KasaId, KurSnapshotId = k.KurId,
    };

    // ── SATIŞ ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("KART")]
    [InlineData("NAKIT")]
    [InlineData("VERESIYE")]
    public async Task ZOP23_sifir_TL_satis_400_ve_hicbir_satir_YAZILMAZ(string odeme)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), $"PAKETZO-ISLEM-A-TEST p23 sifir {odeme}");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(SatisDto(k, 0m, odeme), k.Islem));
        Assert.StartsWith("Satış tutarı 0 TL olamaz", ex.Message);
        Assert.Equal("birimFiyatTl", AlanIsareti.Alan(ex));
        Assert.Equal(0, HareketSayisi(k, "SATIS"));
        Assert.Equal(0, KalemsizYol(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(100m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId).Adet);
    }

    [Fact]
    public async Task ZOP23_odeme_turu_BOS_satis_servis_yolunda_da_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 odemesiz");
        using var db = vt.Olustur(k);
        var dto = SatisDto(k, 5_000m);
        dto.OdemeTuru = null!;

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(dto, k.Islem));
        Assert.StartsWith("Ödeme türü seçilmedi ya da geçersiz", ex.Message);
        Assert.Equal("odemeTuru", AlanIsareti.Alan(ex));
        Assert.Equal(0, HareketSayisi(k, "SATIS"));
    }

    [Fact]
    public async Task ZOP23_normal_satis_GECER_ve_kalemi_dogar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 normal");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 0.01m), k.Islem);   // 1 kuruş: sınırın hemen üstü

        Assert.Equal(1, HareketSayisi(k, "SATIS"));
        Assert.Equal(0, KalemsizYol(k));
        using var kontrol = vt.OlusturSistem();
        var kalem = Assert.Single(kontrol.OdemeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
        Assert.Equal("KART", kalem.Arac);
        Assert.Equal(0.01m, kalem.TutarTl);
    }

    [Fact]
    public async Task ZOP23_yuzde_100_iskontolu_satis_fisi_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 fis");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = k.CariId,
            Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 3_000m }],
            FisIskontoYuzde = 100m,
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "KART", TutarTl = 3_000m, KasaId = k.KasaId }],
            KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.StartsWith("Satış fişi tutarı 0 TL olamaz", ex.Message);
        Assert.Equal(0, HareketSayisi(k, "SATIS"));
    }

    // ── ALIŞ ─────────────────────────────────────────────────────────────────

    // ── ALIŞ — bedelsiz mal girişi MEŞRUDUR (L1b D1 dogrulayıcı testi), ama
    //    para hareketi değildir: etiket TAŞIMAZ, kasa/cari satırı yazılmaz.
    //    Satıştan farkı: 0 TL satış KDVK md.27 bedelsiz teslimdir ve satış
    //    diye yazılmaz (hediye kuralıyla aynı gerekçe) → orada 400.

    [Fact]
    public async Task ZOP23_bedelsiz_alis_ve_alis_fisi_kalemi_KAYDEDILIR_etiket_TASIMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 alis");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 0m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Satis(db).AlisFisi(new MSatis.AlisFisiDto
        {
            CariId = k.CariId, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Kalemler =
            [
                new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m },
                new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 0m },
            ],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var alislar = kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.Tur == "ALIS").ToList();
        Assert.Equal(3, alislar.Count);
        Assert.All(alislar.Where(h => h.TlTutari == 0m), h => Assert.Null(h.OdemeTuru));
        Assert.Equal("HAVALE", Assert.Single(alislar, h => h.TlTutari != 0m).OdemeTuru);   // paralı kalem etiketini KORUR
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(TestKurulum.KasaAcilis - 1_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ── HURDA ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ZOP23_sifir_fiyatli_hurda_alisi_ve_satisi_400()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 hurda"));
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, BirimFiyatTlHasGram = 0m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId, Satici = TestKurulum.HurdaSatici(),
        }, k.Islem));
        Assert.StartsWith("Hurda alışı tutarı 0 TL olamaz", ex.Message);
        Assert.Equal(0, HareketSayisi(k, "HURDA_ALIS"));

        // Stok olsun ki çıkış kapısı değil TUTAR kapısı ölçülsün.
        await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, BirimFiyatTlHasGram = 2_800m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId, Satici = TestKurulum.HurdaSatici(),
        }, k.Islem);

        var ex2 = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(new MSatis.HurdaSatisDto
        {
            Maden = "ALTIN", HasGrami = 5m, BirimFiyatTlHasGram = 0m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem));
        Assert.StartsWith("Hurda satışı tutarı 0 TL olamaz", ex2.Message);
        Assert.Equal("birimFiyatTlHasGram", AlanIsareti.Alan(ex2));
        Assert.Equal(0, HareketSayisi(k, "HURDA_SATIS"));
        Assert.Equal(0, KalemsizYol(k));
    }

    // ── PARA EL DEĞİŞTİRMEYEN MEŞRU İŞLEMLER: etiket YOK, kalem beklenmez ────

    [Fact]
    public async Task ZOP23_fark_0_takas_KAYDEDILIR_odeme_etiketi_TASIMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 takas");
        using var db = vt.Olustur(k);
        var eskiHas = Kuyumcu.Core.Domain.HasHesap.ToHasGram(10m, Kuyumcu.Core.Domain.Ayarlar.Bul("K22")!);

        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 10m,
            EskiUrunBirimFiyatTlHasGram = 5_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = eskiHas * 5_000m,
            KasaId = k.KasaId, OdemeTuru = "NAKIT", KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "TAKAS");
        Assert.Equal(0m, h.TlTutari);
        Assert.Null(h.OdemeTuru);
        Assert.Equal(0, KalemsizYol(k));
        Assert.Empty(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
    }

    [Fact]
    public async Task ZOP23_bedelsiz_iade_KAYDEDILIR_etiket_ve_kasa_satiri_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 iade");
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 5_000m, "NAKIT"), k.Islem);
        string kaynak;
        using (var ara = vt.OlusturSistem())
            kaynak = ara.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS").Id;
        decimal kasaOnce;
        using (var ara = vt.OlusturSistem()) kasaOnce = TestKurulum.KasaBakiye(ara, k.KasaId);

        await Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = kaynak, Adet = 1m, IadeTutariTl = 0m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var iade = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS_IADE");
        Assert.Equal(0m, iade.TlTutari);
        Assert.Null(iade.OdemeTuru);
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Empty(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.HareketId == iade.Id).ToList());
    }

    [Fact]
    public async Task ZOP23_parali_iade_ETIKETINI_ve_kasa_satirini_KORUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST p23 iade para");
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 5_000m, "NAKIT"), k.Islem);
        string kaynak;
        using (var ara = vt.OlusturSistem())
            kaynak = ara.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS").Id;

        await Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = kaynak, Adet = 1m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var iade = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS_IADE");
        Assert.Equal("NAKIT", iade.OdemeTuru);
        Assert.Equal(-5_000m, iade.TlTutari);
        Assert.Single(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.HareketId == iade.Id).ToList());
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(0, KalemsizYol(k));
    }
}

/// <summary>HTTP yolu: 400 gövdesi Türkçe ve AYRIŞTIRILARAK sınanır; hiçbir satır yazılmaz.</summary>
[Collection("http")]
public sealed class ZOISLEMAOdemeTutarKapisiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETZO-ISLEM-A-TEST-http-" + ad);
    }

    private int SatisSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
    }

    [Fact]
    public async Task ZOP23h_sifir_TL_KART_satis_400_mesaj_ve_alan_govdede()
    {
        var k = YeniKiraci("sifir");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = 0, odemeTuru = "KART",
            kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var g = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("Satış tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez.", g.GetProperty("message").GetString());
        Assert.Equal(0, SatisSayisi(k));
    }

    [Fact]
    public async Task ZOP23h_odemesiz_satis_fisi_400_Turkce_alan_mesaji()
    {
        var k = YeniKiraci("odemesiz");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 1, birimFiyatTl = 3000 } },
            odemeler = Array.Empty<object>(), kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var g = await c.Content.ReadFromJsonAsync<JsonElement>();
        var alan = g.GetProperty("alanlar").GetProperty("odemeler")[0].GetString();
        Assert.Equal("Fiş en az bir ödeme bacağı içermelidir", alan);
        Assert.Equal(0, SatisSayisi(k));
    }
}
