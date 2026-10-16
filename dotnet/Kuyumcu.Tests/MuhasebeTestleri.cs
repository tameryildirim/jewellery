using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Kuyumcu.Api.Common;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// K2 / K3 / K4 / K6 — has-cari defterinin doğruluğu.
// Mühürlenen kurallar (Bertan, 2026-09-10):
//   • hareketler.has_grami = fiziki akış (bize giren +, çıkan −)
//   • cari_bakiyeler.has_gram_bakiye = alacak perspektifi (+ = müşteri borçlu)
//   • TL ile kapanan işlemin (alış, hurda, takas, satış) has bacağı CARİYE YAZILMAZ
//   • kasaId verildiyse peşin → cariye TL yazılmaz; verilmediyse cari borçlanır
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MuhasebeTestleri(TestVeritabani vt)
{
    private MSatis.SatisServisi Satis(Kuyumcu.Api.Data.KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    // ── K2: alışta çifte kayıt ───────────────────────────────────────────────

    [Fact]
    public async Task Alis_PesinOdemedeCariBorcuOlusmamali()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Alış peşin");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 12_000m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis - 24_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    [Fact]
    public async Task Alis_KasaVerilmezseCariBorclanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Alış veresiye");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 12_000m,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-24_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ── K3: hurda alışta üçlü kayıt ──────────────────────────────────────────

    [Fact]
    public async Task HurdaAlis_PesinOdemedeYalnizKasadanCikar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda peşin"), 2_000m);
        using var db = vt.Olustur(k);

        // 7,9 g 22 ayar → has 7,2364 · 1.000 TL/has → 7.236,40 TL
        await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 7.9m,
            BirimFiyatTlHasGram = 1_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(),
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis - 7_236.40m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        var (tl, has) = TestKurulum.CariBakiye(kontrol, k.CariId);
        Assert.Equal(0m, tl);
        Assert.Equal(0m, has);

        // Fiziki akış deftere yazılmaya devam eder
        var hareket = kontrol.Hareketler.Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
        Assert.Equal(7.236400m, hareket.HasGrami);
        Assert.Equal(-7_236.40m, hareket.TlTutari);
    }

    [Fact]
    public async Task HurdaAlis_KasaVerilmezseYalnizCariTlBorclanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda veresiye"), 2_000m);
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 7.9m,
            BirimFiyatTlHasGram = 1_000m, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(),
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (tl, has) = TestKurulum.CariBakiye(kontrol, k.CariId);
        Assert.Equal(-7_236.40m, tl);
        Assert.Equal(0m, has);
    }

    // ── K4: has işaret sözleşmesi ────────────────────────────────────────────

    [Fact]
    public async Task EmanetGiris_BizBorclandigimizIcinHasBakiyesiEksilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Emanet");
        using var db = vt.Olustur(k);

        var servis = new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance);
        // 10 g 22 ayar → 9,16 has
        await servis.Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-9.160000m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }

    [Fact]
    public async Task Veresiye_AltinVerildigindeMusteriHasBorclusuOlur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Veresiye");
        using var db = vt.Olustur(k);

        var servis = new MCari.CariServisi(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));
        await servis.VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = 1_000m, HasGrami = 5m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (tl, has) = TestKurulum.CariBakiye(kontrol, k.CariId);
        Assert.Equal(1_000m, tl);
        Assert.Equal(5m, has);
    }

    [Fact]
    public async Task EmanetCikis_EmanetiOlmayanMusteriIcinReddedilmeli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Emanet yok");
        using var db = vt.Olustur(k);

        // Müşteri 20 has ALTIN BORÇLUSU (veresiye) — ama emaneti yok.
        var cari = new MCari.CariServisi(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));
        await cari.VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = 1m, HasGrami = 20m, KurSnapshotId = k.KurId,
        }, k.Islem);

        var emanet = new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance);
        await Assert.ThrowsAnyAsync<ApiException>(() => emanet.Cikis(k.CariId, new MEmanet.EmanetCikisDto
        {
            HasGrami = 5m, KurSnapshotId = k.KurId,
        }, k.Islem));
    }

    [Fact]
    public async Task EmanetCikis_AltinBorcuOlsaBileEmanetIadeEdilebilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Emanet + borç");
        using var db = vt.Olustur(k);

        var cari = new MCari.CariServisi(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));
        await cari.VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = 1m, HasGrami = 20m, KurSnapshotId = k.KurId,
        }, k.Islem);

        var emanet = new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance);
        await emanet.Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "K24", GramAgirlik = 9m, KurSnapshotId = k.KurId,
        }, k.Islem);
        await emanet.Cikis(k.CariId, new MEmanet.EmanetCikisDto
        {
            HasGrami = 9m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        // 20 has borç durur, emanet tamamen iade edildi
        Assert.Equal(20m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }

    [Fact]
    public async Task Takas_CariHasBakiyesiniDegistirmemeli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Takas"), 2_000m);
        using var db = vt.Olustur(k);

        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 10m,
            EskiUrunBirimFiyatTlHasGram = 1_000m, YeniUrunId = k.UrunId,
            YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 10_000m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        // eski 9,16 has × 1.000 = 9.160 TL · yeni 10.000 TL → fark 840 TL müşteriden
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
        Assert.Equal(TestKurulum.KasaAcilis + 840m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task TlIleKapananIslemler_CariHasBakiyesineDokunmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Karma akış"), 2_000m);
        using var db = vt.Olustur(k);
        var s = Satis(db);

        await s.Satis(new MSatis.SatisOlusturDto
        {
            // ⚠ Üç işlem AYNI cari AYNI gün: 459 s. VUK Genel Tebliği kümülatif haddi (30.000 TL)
            // geçerli. Tutarlar had altında tutulur; bu test has bakiyesini sınıyor.
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await s.Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 4_000m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await s.HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K14", GramAgirlik = 20m,
            BirimFiyatTlHasGram = 1_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(),
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }

    // ── K6: kasa para birimi ─────────────────────────────────────────────────

    [Fact]
    public async Task Satis_DovizKasasinaTlYazilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Döviz kasası", kasaDoviz: "USD");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 25_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task Satis_KasaHareketiKasaninParaBirimiyleKaydedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "TL kasası");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 25_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var hareket = kontrol.KasaHareketleri.Single(h => h.KasaId == k.KasaId);
        Assert.Equal("TRY", hareket.ParaBirimi);
        Assert.Equal("NAKIT", hareket.OdemeTuru);
    }

    // ── Mevcut davranışın korunması (regresyon) ──────────────────────────────

    [Fact]
    public async Task Satis_StokYetersizIseReddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Stok sınırı", stokAdet: 1m);
        using var db = vt.Olustur(k);

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1m, kontrol.Stoklar.Single(s => s.UrunId == k.UrunId).Adet);
    }

    [Fact]
    public async Task Satis_PesinSatistaKasaArtarCariDegismez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Peşin satış");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 25_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 25_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);

        var hareket = kontrol.Hareketler.Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
        Assert.True(hareket.TlTutari > 0, "satışta TL bacağı pozitif olmalı");
        Assert.True(hareket.HasGrami < 0, "satışta has bacağı negatif olmalı");
    }
}
