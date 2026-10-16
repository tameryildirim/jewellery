using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MPos = Kuyumcu.Api.Modules.Pos;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 FAZ B — BANKA YATMASI EŞLEŞTİRME [DB]
//
// yatan_tutar ve durum TÜRETİLMİŞ ÖZETTİR (Karar C): elle atanmaz, yalnız
// PosDefteri.Ozetle geri_alindi=false eşleşmelerin toplamından hesaplar.
// Eşleşme SİLİNMEZ (ADR-0004): geri_alindi + gerekçe.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PosEslestirmeTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Banka ekstresi yanlış hesaba düşmüş, mutabakat düzeltildi.";

    [Fact]
    public async Task BankaYatmasi_UcTahsilatiKapatir_DurumYATTI()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS eşleşme 3");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);   // net 981,50
        var t2 = PosYardim.TahsilatYaz(vt, k, cihaz, 2_000m);   // net 1.963,00
        var t3 = PosYardim.TahsilatYaz(vt, k, cihaz, 3_000m);   // net 2.944,50
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_889.00m);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar =
            [
                new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 981.50m },
                new MPos.PosEslestirSatirDto { PosTahsilatId = t2, Tutar = 1_963.00m },
                new MPos.PosEslestirSatirDto { PosTahsilatId = t3, Tutar = 2_944.50m },
            ],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        foreach (var id in new[] { t1, t2, t3 })
        {
            var t = kontrol.PosTahsilatlari.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal(PosTahsilatDurumu.Yatti, t.Durum);
            Assert.Equal(t.NetTutar, t.YatanTutar);
        }
        Assert.Equal("ESLESTI",
            kontrol.BankaHareketleri.AsNoTracking().Single(x => x.Id == hareket).Durum);
    }

    [Fact]
    public async Task KismiEslesme_DurumKISMI()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kısmi");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 5_000m }],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var t = kontrol.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t1);
        Assert.Equal(PosTahsilatDurumu.Kismi, t.Durum);
        Assert.Equal(5_000.00m, t.YatanTutar);
    }

    [Fact]
    public async Task Eslesme_NetTutariAsamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS net aşım");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);    // net 981,50
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m);

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 2_000m }],
            }, k.Islem));
        Assert.Contains("net tutar", hata.Message);
    }

    [Fact]
    public async Task Eslesme_BankaHareketiTutariniAsamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS hareket aşım");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 1_000m);

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 2_000m }],
            }, k.Islem));
        Assert.Contains("banka hareketi tutarını", hata.Message);
    }

    [Fact]
    public async Task Eslesme_GidenHareketle_Reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS giden");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m, "GIDEN");

        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 100m }],
            }, k.Islem));
    }

    [Fact]
    public async Task EslesmeGeriAl_SatirSilmez_OzetiDuzeltir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS geri al");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 9_815m);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 9_815m }],
        }, k.Islem);

        string eslesmeId;
        using (var oku = vt.OlusturSistem())
            eslesmeId = oku.PosYatmaEslesmeleri.AsNoTracking()
                .Single(e => e.TenantId == k.TenantId).Id;

        await PosYardim.Servis(db).EslesmeGeriAl(eslesmeId,
            new MPos.PosEslesmeGeriAlDto { Gerekce = Gerekce }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var e = kontrol.PosYatmaEslesmeleri.AsNoTracking().Single(x => x.Id == eslesmeId);
        Assert.True(e.GeriAlindi);                    // SATIR DURUYOR (append-only)
        Assert.Equal(Gerekce, e.GeriAlmaGerekcesi);

        var t = kontrol.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t1);
        Assert.Equal(0m, t.YatanTutar);
        Assert.Equal(PosTahsilatDurumu.Bekliyor, t.Durum);
        Assert.Equal("BEKLIYOR",
            kontrol.BankaHareketleri.AsNoTracking().Single(x => x.Id == hareket).Durum);
    }

    [Fact]
    public async Task EslesmeGeriAl_GerekcesizReddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS geri al gerekçe");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 981.50m);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 981.50m }],
        }, k.Islem);

        string eslesmeId;
        using (var oku = vt.OlusturSistem())
            eslesmeId = oku.PosYatmaEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId).Id;

        await Assert.ThrowsAsync<BadRequestException>(() => PosYardim.Servis(db)
            .EslesmeGeriAl(eslesmeId, new MPos.PosEslesmeGeriAlDto { Gerekce = "kısa" }, k.Islem));
    }

    [Fact]
    public async Task OtoOneri_TekAdayYoksa_BosDoner()
    {
        // "Yakın olanı seçelim" VARSAYIMI YAPILMAZ (Anayasa md.1).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS öneri yok");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 7_777.77m);

        using var db = vt.Olustur(k);
        var oneri = await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem);

        Assert.False(TestKurulum.Bayrak(oneri, "bulundu"));
    }

    [Fact]
    public async Task OtoOneri_TutarTutuyorsa_KumeyiOnerir_AmaYAZMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS öneri var");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);   // net 981,50
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 981.50m);

        using var db = vt.Olustur(k);
        var oneri = await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem);

        Assert.True(TestKurulum.Bayrak(oneri, "bulundu"));
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.PosYatmaEslesmeleri.AsNoTracking().Where(e => e.TenantId == k.TenantId));
    }

    [Fact]
    public async Task TaksitOrani_PesinOranaGoreUstunGelir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS taksit oranı");
        using var db = vt.Olustur(k);

        var cevap = await PosYardim.Servis(db).CihazOlustur(new MPos.PosCihazOlusturDto
        {
            Ad = "Taksitli POS", BankaAdi = "Garanti", SubeId = k.SubeId, KasaId = k.KasaId,
            PesinKomisyonOran = 0.0185m, ValorGun = 1,
        }, k.Islem);
        var cihazId = PosYardim.CihazId(cevap);

        await PosYardim.Servis(db).TaksitOranEkle(cihazId,
            new MPos.PosTaksitOranOlusturDto { TaksitAdedi = 6, KomisyonOran = 0.05m }, k.Islem);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 10_000m, cihazId, taksit: 6), k.Islem);

        var t = Assert.Single(PosYardim.Tahsilatlar(vt, k));
        Assert.Equal(6, t.TaksitAdedi);
        Assert.Equal(0.05m, t.KomisyonOran);       // peşin oran DEĞİL
        Assert.Equal(500.00m, t.KomisyonTutar);
        Assert.Equal(9_500.00m, t.NetTutar);
    }

    [Fact]
    public async Task EslesmisTahsilat_IptalEdilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS eşleşmiş iptal");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 981.50m);

        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 981.50m }],
        }, k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() => PosYardim.Servis(db)
            .TahsilatIptal(t1, new MPos.PosTahsilatIptalDto { Gerekce = Gerekce }, k.Islem));
    }
}
