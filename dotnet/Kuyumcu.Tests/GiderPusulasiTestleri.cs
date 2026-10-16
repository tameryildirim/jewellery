using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PLAN Adım 6 — hurda alışında zorunlu kimlik + gider pusulası (VUK md.234).
//
// Kabul ölçütü: "Kimliksiz hurda alışı REDDEDİLİR (test). Gider pusulası çıktısı
// örnekle gösterilir."
//
// MASAK'tan farkı burada kanıtlanır: TUTAR EŞİĞİ YOK. 1.000 TL'lik alımda da
// kimlik gerekir; MASAK eşiği 185.000 TL'dir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TcknDogrulamaTestleri
{
    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("12345678901", false)]   // algoritmadan geçmez
    [InlineData("00000000000", false)]   // 0 ile başlayamaz
    [InlineData("1000000014", false)]    // 10 hane
    [InlineData("100000001466", false)]  // 12 hane
    [InlineData("1000000014A", false)]   // harf
    [InlineData("", false)]
    public void TcknAlgoritmasiDogru(string tckn, bool beklenen)
    {
        Assert.Equal(beklenen, MHurda.GiderPusulasiKapisi.TcknGecerli(tckn));
    }
}

public sealed class HurdaKapiTestleri
{
    private static readonly MHurda.GiderPusulasiKapisi K = MHurda.GiderPusulasiKapisi.Varsayilan();

    private static MHurda.HurdaSaticiDto Satici(
        string tip = SaticiTipi.NihaiTuketici, string tckn = "10000000146") => new()
    {
        SaticiTipi = tip, AdSoyad = "Ali Veli", Tckn = tckn, Adres = "Ulus",
    };

    [Fact]
    public void SaticiYoksaReddedilir()
    {
        var ex = Assert.Throws<BadRequestException>(() => K.Dogrula(null));
        Assert.Contains("VUK md.234", ex.Message);
    }

    [Fact]
    public void GecersizSaticiTipiReddedilir()
    {
        var ex = Assert.Throws<BadRequestException>(() => K.Dogrula(Satici(tip: "KOMSU")));
        Assert.Contains("satıcı tipi", ex.Message);
    }

    [Fact]
    public void AdSoyadVeTcknZorunlu()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            K.Dogrula(new MHurda.HurdaSaticiDto { SaticiTipi = SaticiTipi.NihaiTuketici }));

        Assert.Contains("ad soyad", ex.Message);
        Assert.Contains("TCKN zorunludur", ex.Message);
    }

    [Fact]
    public void UydurmaTcknReddedilir()
    {
        var ex = Assert.Throws<BadRequestException>(() => K.Dogrula(Satici(tckn: "11111111111")));
        Assert.Contains("TC kimlik numarası geçerli değil", ex.Message);   // TCKN-DOGRULAMA: sade metin
    }

    [Fact]
    public void TcknBosluklaYazilabilir()
    {
        // "100 000 001 46" — kuyumcu okurken boşluk bırakabilir
        Assert.Equal(SaticiTipi.NihaiTuketici, K.Dogrula(Satici(tckn: "100 000 001 46")));
    }

    [Fact]
    public void MukellefteFaturaNoZorunlu_KimlikDegil()
    {
        // Mükellef fatura keser; pusula düzenlenmez, TCKN istenmez.
        var ex = Assert.Throws<BadRequestException>(() =>
            K.Dogrula(new MHurda.HurdaSaticiDto { SaticiTipi = SaticiTipi.Mukellef }));
        Assert.Contains("tedarikçi fatura numarası", ex.Message);

        var tip = K.Dogrula(new MHurda.HurdaSaticiDto
        {
            SaticiTipi = SaticiTipi.Mukellef, TedarikciFaturaNo = "A-2026-77",
        });
        Assert.Equal(SaticiTipi.Mukellef, tip);
    }

    // ── Stopaj — satıcının vergi durumuna göre ───────────────────────────────

    [Fact]
    public void NihaiTuketicidenStopajYapilmaz()
    {
        // GVK md.94'te vergi mükellefi olmayandan hurda alımı için tevkifat hükmü yok.
        Assert.Equal(0m, K.StopajOran(SaticiTipi.NihaiTuketici));
    }

    [Fact]
    public void EsnafMuaftanYuzde2Stopaj()
    {
        Assert.Equal(0.02m, K.StopajOran(SaticiTipi.EsnafMuaf));
    }

    [Fact]
    public void MukellefteStopajYok()
    {
        Assert.Equal(0m, K.StopajOran(SaticiTipi.Mukellef));
    }

    [Fact]
    public void StopajOranlariYapilandirmadanDegisir()
    {
        var kapi = new MHurda.GiderPusulasiKapisi(new MHurda.HurdaAyarlari
        {
            EsnafMuafStopajOran = 0.05m, PusulaDuzenlemeGunu = 14,
        });
        Assert.Equal(0.05m, kapi.StopajOran(SaticiTipi.EsnafMuaf));
        Assert.Equal(14, kapi.Ayarlar.PusulaDuzenlemeGunu);
    }

    [Fact]
    public void MukellefIcinPusulaDuzenlenmez()
    {
        Assert.False(SaticiTipi.PusulaGerekir(SaticiTipi.Mukellef));
        Assert.True(SaticiTipi.PusulaGerekir(SaticiTipi.NihaiTuketici));
        Assert.True(SaticiTipi.PusulaGerekir(SaticiTipi.EsnafMuaf));
    }
}

[Collection("veritabani")]
public sealed class HurdaAkisTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.HurdaAlisDto Dto(
        // Varsayılan tutar 459 s. VUK Genel Tebliği haddinin (30.000 TL) ALTINDA tutulur:
        // 9,16 has × 2.800 = 25.648 TL. Aksi hâlde bu testler tevsik kapısına
        // takılır ve asıl sınadıkları şeyi (pusula, stopaj, süre) sınayamaz.
        Kiraci k, decimal gram = 10m, decimal fiyat = 2_800m,
        MHurda.HurdaSaticiDto? satici = null, string? kmts = null) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = gram,
        BirimFiyatTlHasGram = fiyat, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = satici ?? TestKurulum.HurdaSatici(), KmtsKodu = kmts,
    };

    [Fact]
    public async Task KimliksizHurdaAlisiReddedilir_TutarKucukOlsaBile()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-kimliksiz"));
        using var db = vt.Olustur(k);

        // 1 gram × 2.800 TL — MASAK eşiğinin (185.000) çok altında (ISLEM-BANT: 100 TL/has g bant dışı).
        var dto = Dto(k, gram: 1m, fiyat: 2_800m);
        dto.Satici = null;

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaAlis(dto, k.Islem));
        Assert.Contains("VUK md.234", ex.Message);

        // Fail-closed: hiçbir şey yazılmamış olmalı
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS"));
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task NihaiTuketicidenAlimda_PusulaOlusur_StopajSifir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-pusula"));
        using var db = vt.Olustur(k);

        // 10 g × 916/1000 = 9,16 has × 2.800 = 25.648 TL (459 s. VUK Genel Tebliği haddi altında)
        await Satis(db).HurdaAlis(Dto(k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);

        Assert.StartsWith("GP-", p.PusulaNo);
        Assert.Equal(SaticiTipi.NihaiTuketici, p.SaticiTipi);
        Assert.Equal(0m, p.StopajOran);
        Assert.Equal(0m, p.StopajTutar);
        Assert.Equal(p.BrutTutar, p.NetOdenen);        // stopaj yok → net = brüt
        Assert.Equal(25_648m, p.BrutTutar);
        Assert.Equal("10000000146", p.SaticiTckn);     // okurken çözülür
        Assert.Equal(7, (p.DuzenlemeSonTarihi - p.TeslimTarihi).Days);   // VUK 7 gün

        // Harekete bağlı mı?
        var hareket = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
        Assert.Equal(hareket.Id, p.HareketId);
    }

    [Fact]
    public async Task EsnafMuaftanAlimda_Yuzde2StopajKesilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-esnaf"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(
            Dto(k, satici: TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf)), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);

        Assert.Equal(0.02m, p.StopajOran);
        Assert.Equal(512.96m, p.StopajTutar);              // 25.648 × 0,02
        Assert.Equal(25_135.04m, p.NetOdenen);             // 25.648 − 512,96
        Assert.Equal(p.BrutTutar - p.StopajTutar, p.NetOdenen);
    }

    [Fact]
    public async Task MukelleftenAlimda_PusulaOlusmaz_FaturaNoHarekete_Yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-mukellef"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(Dto(k, satici: new MHurda.HurdaSaticiDto
        {
            SaticiTipi = SaticiTipi.Mukellef, TedarikciFaturaNo = "TDK-2026-451",
        }), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.GiderPusulalari.Where(g => g.TenantId == k.TenantId));

        var hareket = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
        Assert.Equal("TDK-2026-451", hareket.FaturaNo);
    }

    [Fact]
    public async Task PusulaNumarasiSirali_VeKiraciIcindeTekil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-no"), 1_000m);
        using var db = vt.Olustur(k);

        // ⚠ Üç alım AYNI cari, AYNI gün: 459 s. VUK Genel Tebliği kümülatif haddi (30.000 TL)
        // burada devreye girer. Fiyat düşük tutulur ki bu test PUSULA NUMARASI
        // sırasını sınasın, tevsik kapısını değil (o ayrı testlerde sınanıyor).
        await Satis(db).HurdaAlis(Dto(k, gram: 5m, fiyat: 500m), k.Islem);
        await Satis(db).HurdaAlis(Dto(k, gram: 6m, fiyat: 500m), k.Islem);
        await Satis(db).HurdaAlis(Dto(k, gram: 7m, fiyat: 500m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var nolar = kontrol.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == k.TenantId).Select(g => g.PusulaNo)
            .OrderBy(x => x).ToList();

        Assert.Equal(3, nolar.Count);
        Assert.Equal(3, nolar.Distinct().Count());
        Assert.EndsWith("000001", nolar[0]);
        Assert.EndsWith("000003", nolar[2]);
    }

    [Fact]
    public async Task TcknDiskeSifreliIner_DuzMetinYok()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-sifre"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Dto(k), k.Islem);

        // Ham SQL: converter devreye girmez, diskte ne varsa o görünür.
        using var kontrol = vt.OlusturSistem();
        var ham = kontrol.Database.SqlQuery<string>(
            $"SELECT satici_tckn AS Value FROM gider_pusulalari WHERE tenant_id = {k.TenantId}").Single();

        Assert.DoesNotContain("10000000146", ham);
        Assert.StartsWith("v1:", ham);

        // Hash dolu ve düz metin değil
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(p.SaticiTcknHash));
        Assert.DoesNotContain("10000000146", p.SaticiTcknHash);
    }

    [Fact]
    public async Task KmtsKodu_HareketeIsteğeBagliYazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-kmts"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(Dto(k, kmts: "  KMTS-ABC-123  "), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");
        Assert.Equal("KMTS-ABC-123", h.KmtsKodu);   // trim edilir
    }

    [Fact]
    public async Task KmtsVerilmezseNullKalir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-kmtsyok"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(Dto(k, kmts: "   "), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");
        Assert.Null(h.KmtsKodu);
    }

    [Fact]
    public async Task PusulaBaskaKiracidanGorunmez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-A"));
        var b = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-B"));

        using var aDb = vt.Olustur(a);
        await Satis(aDb).HurdaAlis(Dto(a), a.Islem);

        using var bDb = vt.Olustur(b);
        Assert.Empty(bDb.GiderPusulalari.ToList());
    }

    // ── Canlı testte yakalanan iki hatanın regresyonu ────────────────────────

    [Fact]
    public async Task EsnafMuaf_StopajKasadanDusulur_SaticiNetAlir()
    {
        // Canlı bulgu (2026-09-12): pusula "net 61.042,24" diyordu ama kasadan
        // 62.288 çıkmıştı. Satıcının eline geçen NET'tir; stopaj kesilir.
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-stopaj-kasa"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(
            Dto(k, satici: TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf)), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);

        // Kasadan çıkan = pusuladaki NET, brüt değil
        Assert.Equal(TestKurulum.KasaAcilis - p.NetOdenen, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.NotEqual(p.BrutTutar, p.NetOdenen);   // stopaj gerçekten kesilmiş

        var hareket = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
        Assert.Equal(-p.NetOdenen, hareket.TlTutari);
    }

    [Fact]
    public async Task KasaHareketiIsaretliYazilir_BakiyeDefterdenTuretilebilir()
    {
        // Canlı bulgu: KasaGuncelle Math.Abs kullanıyordu; para ÇIKIŞLARI defterde
        // GİRİŞ gibi duruyordu (bakiye −49.576 iken defter toplamı +199.576) ve
        // gün sonu raporu hurda ödemesini "tahsilat" sayıyordu.
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Hurda-isaret"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(Dto(k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var hareketler = kontrol.KasaHareketleri.AsNoTracking()
            .Where(x => x.KasaId == k.KasaId).ToList();

        Assert.Single(hareketler);
        Assert.True(hareketler[0].Tutar < 0m, "hurda ödemesi kasadan ÇIKIŞTIR, negatif olmalı");

        // Bertan kuralı: bakiye append-only defterden yeniden türetilebilmeli
        var defterden = TestKurulum.KasaAcilis + hareketler.Sum(x => x.Tutar);
        Assert.Equal(TestKurulum.KasaBakiye(kontrol, k.KasaId), defterden);
    }
}
