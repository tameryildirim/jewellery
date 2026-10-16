using System.Data.Common;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MPos = Kuyumcu.Api.Modules.Pos;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 — POS ALACAK DEFTERİ [DB]
//
// Ölçülen: kart tahsilatının brüt/komisyon/net ayrışması, valör tarihi, cihaz
// çözümünün FAIL-CLOSED davranışı, kasa bakiyesinin POS'tan ETKİLENMEMESİ ve
// "KART bacağı var ama PosTahsilat yok" durumunun 6 çağrı noktasında yakalanması.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PosTahsilatTestleri(TestVeritabani vt)
{
    private static readonly MPos.PosAyarlari Zorunlu = new() { CihazZorunlu = true };

    [Fact]
    public async Task KartSatisi_PosTahsilatiYazar_BrutKomisyonNet()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kart satış");
        var cihaz = PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 10_000m, cihaz), k.Islem);

        var t = Assert.Single(PosYardim.Tahsilatlar(vt, k));
        Assert.Equal(10_000.00m, t.BrutTutar);
        Assert.Equal(0.0185m, t.KomisyonOran);
        Assert.Equal(185.00m, t.KomisyonTutar);
        Assert.Equal(9_815.00m, t.NetTutar);
        Assert.Equal(0m, t.YatanTutar);
        Assert.Equal(PosTahsilatDurumu.Bekliyor, t.Durum);
        Assert.NotNull(t.KasaHareketId);   // kasa hareketiyle bağ KURULUR
    }

    [Fact]
    public async Task KartSatisi_ValorTarihiHesaplanir_TakvimGunu()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS valör");
        var cihaz = PosYardim.CihazYaz(vt, k, valorGun: 3);
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 1_000m, cihaz), k.Islem);

        var t = Assert.Single(PosYardim.Tahsilatlar(vt, k));
        var beklenen = Zaman.YerelGunBasiUtc(
            DateOnly.FromDateTime(Zaman.UtcdenYerele(t.IslemTarihi)).AddDays(3));
        Assert.Equal(beklenen, t.ValorTarihi);
    }

    [Fact]
    public void ValorIsGunu_Acikken_HaftasonuAtlanir()
    {
        // Saf hesap: 2026-09-18 Cuma + 1 iş günü → 21 Eylül Pazartesi.
        var cuma = Zaman.YerelGunBasiUtc(new DateOnly(2026, 9, 18)).AddHours(9);

        var takvim = MPos.PosDefteri.ValorHesapla(cuma, 1, isGunu: false);
        var isGunu = MPos.PosDefteri.ValorHesapla(cuma, 1, isGunu: true);

        Assert.Equal(Zaman.YerelGunBasiUtc(new DateOnly(2026, 9, 19)), takvim);
        Assert.Equal(Zaman.YerelGunBasiUtc(new DateOnly(2026, 9, 21)), isGunu);
    }

    [Fact]
    public async Task KartBacagi_BirdenCokAktifCihaz_Reddedilir()
    {
        // FAIL-CLOSED: ilk cihaz SEÇİLMEZ, istek reddedilir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS iki cihaz");
        PosYardim.CihazYaz(vt, k, "Garanti POS");
        PosYardim.CihazYaz(vt, k, "Akbank POS");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 1_000m), k.Islem));
        Assert.Contains("POS cihazı belirtilmeli", hata.Message);
    }

    [Fact]
    public async Task KartBacagi_TekCihaz_OtomatikSecilir()
    {
        // Aşkın koşulu: fail-open eğilimi de TESTLENİR.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS tek cihaz");
        PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 2_000m), k.Islem);

        Assert.Single(PosYardim.Tahsilatlar(vt, k));
    }

    [Fact]
    public async Task KartBacagi_CihazYok_CihazZorunluKapali_TahsilatYazilmaz()
    {
        // Paket öncesi davranış AYNEN sürer; sessizce hata verilmez.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS cihazsız kapalı");
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 1_000m), k.Islem);

        Assert.Empty(PosYardim.Tahsilatlar(vt, k));
    }

    [Fact]
    public async Task KartBacagi_CihazYok_CihazZorunluAcik_Reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS cihazsız açık");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(
            () => PosYardim.Satis(db, Zorunlu).Satis(PosYardim.KartSatis(k, 1_000m), k.Islem));
    }

    [Fact]
    public async Task KasaBakiyesi_PosTahsilatindanEtkilenmez()
    {
        // POS bir YAN DEFTERDİR: bakiyeye BRÜT girer, komisyon kasadan DÜŞMEZ.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kasa etkisi");
        var cihaz = PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 10_000m, cihaz), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 10_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task KarmaOdeme_YalnizKartBacagi_PosaYazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS karma");
        var cihaz = PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);

        await PosYardim.Satis(db).SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = k.CariId,
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m }],
            Odemeler =
            [
                new Kuyumcu.Api.Modules.Satis.OdemeBacagiDto
                    { Arac = "NAKIT", TutarTl = 4_000m, KasaId = k.KasaId },
                new Kuyumcu.Api.Modules.Satis.OdemeBacagiDto
                    { Arac = "KART", TutarTl = 6_000m, KasaId = k.KasaId, PosCihazId = cihaz },
            ],
        }, k.Islem);

        var t = Assert.Single(PosYardim.Tahsilatlar(vt, k));
        Assert.Equal(6_000.00m, t.BrutTutar);
        Assert.Equal(111.00m, t.KomisyonTutar);
        Assert.Equal(5_889.00m, t.NetTutar);
    }

    [Fact]
    public async Task KartIadesi_TersKayitYazar_KomisyonIadeEdilmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kart iade");
        var cihaz = PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);

        var satis = await PosYardim.Satis(db).Satis(PosYardim.KartSatis(k, 10_000m, cihaz), k.Islem);
        var hareketId = TestKurulum.Metin(satis, "id")
            ?? TestKurulum.Metin(satis, "Id")
            ?? vt.OlusturSistem().Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
                .OrderByDescending(h => h.Olusturuldu).First().Id;

        await PosYardim.Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = hareketId, Adet = 1m, OdemeTuru = "KART",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, PosCihazId = cihaz,
        }, k.Islem);

        var satirlar = PosYardim.Tahsilatlar(vt, k);
        Assert.Equal(2, satirlar.Count);
        var iade = satirlar[1];
        Assert.True(iade.BrutTutar < 0m);
        // Banka kesilen komisyonu İADE ETMEZ (varsayılan, DOĞRULANMAMIŞ teamül).
        Assert.Equal(0m, iade.KomisyonTutar);
        Assert.Equal(iade.BrutTutar, iade.NetTutar);
    }

    [Fact]
    public async Task Tahsilat_Iptal_GerekcesizReddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS iptal gerekçe");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var tahsilat = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() => PosYardim.Servis(db)
            .TahsilatIptal(tahsilat, new MPos.PosTahsilatIptalDto { Gerekce = "kısa" }, k.Islem));

        await PosYardim.Servis(db).TahsilatIptal(tahsilat,
            new MPos.PosTahsilatIptalDto { Gerekce = "Kart hamili bankaya itiraz etti, chargeback." }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var t = kontrol.PosTahsilatlari.AsNoTracking().Single(x => x.Id == tahsilat);
        Assert.Equal(PosTahsilatDurumu.Iptal, t.Durum);
        Assert.NotNull(t.IptalGerekcesi);   // satır SİLİNMEZ, gerekçe kalır
    }

    [Fact]
    public async Task ValorTakvimi_SunucudaToplanir_SatirDonmez()
    {
        // ÖLÇÜM YÖNTEMİ (Aşkın koşulu, belirsizlik bırakılmadı): EF komut
        // yakalayıcısı çalışan SQL metnini toplar; valör takvimi sorgusunda
        // GROUP BY ARANIR. ToListAsync + bellekte GroupBy'a dönülürse GROUP BY
        // kaybolur ve bu test KIRMIZI olur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS valör takvim");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        PosYardim.TahsilatYaz(vt, k, cihaz, 5_000m);

        var yakalayici = new SqlYakalayici();
        using var db = PosSqlOrtami.Kur(vt, k.TenantId, yakalayici);

        var takvim = await PosYardim.Servis(db).ValorTakvimi(k.Islem, null, null);

        var satir = Assert.Single(takvim);
        Assert.Equal(15_000.00m, satir.Brut);
        Assert.Equal(277.50m, satir.Komisyon);
        Assert.Equal(14_722.50m, satir.Net);
        Assert.Contains(yakalayici.Komutlar,
            s => s.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase)
                 && s.Contains("pos_tahsilatlari", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValorTakvimi_SayilariInvariantCultureIleBicimlenir()
    {
        // Test projesinde kültür ayarı YOKTUR; biçimleme açıkça sınanır
        // (tr-TR'de "1,85" üretip CSV/SVG'yi bozan tuzak, §6).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kültür");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);

        var onceki = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
        try
        {
            using var db = vt.OlusturSistem();
            var kaynak = Kuyumcu.Api.Modules.Disaaktarim.Kaynaklar.Bul("pos-tahsilatlari")!;
            var satirlar = new List<string[]>();
            await foreach (var s in kaynak.Satirlar(db, k.TenantId, maskeli: false)) satirlar.Add(s);

            var satir = Assert.Single(satirlar);
            Assert.Contains("9815.00", satir);      // ondalık NOKTA — virgül değil
            Assert.Contains("0.0185", satir);
        }
        finally { Thread.CurrentThread.CurrentCulture = onceki; }
    }

    [Fact]
    public async Task PosTahsilati_BaskaKiraciyaSizmaz()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS sızma A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS sızma B");
        var cihazA = PosYardim.CihazYaz(vt, a, "A POS");
        PosYardim.TahsilatYaz(vt, a, cihazA, 10_000m);

        using var db = vt.Olustur(b);
        var sayfa = await PosYardim.Servis(db).TahsilatListele(b.Islem, null, null, null, null, 50, 0);

        Assert.Equal(0, TestKurulum.Sayi(sayfa, "toplamSatir"));
        Assert.Empty(await PosYardim.Servis(db).ValorTakvimi(b.Islem, null, null));
    }

    [Fact]
    public async Task PosTahsilati_ElleTenantSarti_FiltresizBaglamda_DaKorur()
    {
        // ⚠ TEK KATMAN ÖLÇÜMÜ (Aşkın koşulu): global HasQueryFilter kiracı
        // sızmasını zaten engelliyor, bu yüzden "elle TenantId şartı" mutasyonu
        // filtreli bağlamda YAKALANMAZ. ADR-0010 elle şartı ikinci bir savunma
        // olarak ZORUNLU kılar; burada FİLTRESİZ bağlamla yalnız o katman ölçülür.
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS elle şart A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS elle şart B");
        var cihazA = PosYardim.CihazYaz(vt, a, "Elle şart POS");
        PosYardim.TahsilatYaz(vt, a, cihazA, 10_000m);

        using var filtresiz = vt.OlusturSistem();
        var servis = PosYardim.Servis(filtresiz);

        var sayfa = await servis.TahsilatListele(b.Islem, null, null, null, null, 50, 0);
        Assert.Equal(0, TestKurulum.Sayi(sayfa, "toplamSatir"));
        Assert.Empty(await servis.ValorTakvimi(b.Islem, null, null));
        Assert.Empty(await servis.CihazListele(b.Islem, null, null));
    }

    [Fact]
    public async Task Cihaz_ElleTenantSarti_YabanciCihaz_FiltresizBaglamda_404()
    {
        // ⚠ TEK KATMAN: cihaz ÇÖZÜMÜNDE üç savunma üst üste durur (global
        // süzgeç · elle TenantId · şube eşleşmesi). Bu test yalnız ELLE
        // TENANT ŞARTINI ölçer; bu yüzden fikstür, A kiracısına ait ama B'nin
        // ŞUBE ve KASA kimliğini taşıyan yapay bir cihaz yazar ve bağlam
        // FİLTRESİZ kurulur. Diğer iki savunma böylece devre dışı kalır.
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS elle cihaz A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS elle cihaz B");
        var cihazA = PosYardim.CihazYaz(vt, a, "Elle cihaz POS",
            kasaId: b.KasaId, subeId: b.SubeId);

        using var filtresiz = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            MPos.PosServisi.CihazCoz(filtresiz, new MPos.PosAyarlari(),
                b.TenantId, b.SubeId, cihazA, b.KasaId));
    }

    [Fact]
    public async Task Cihaz_BaskaKiracininCihaziyla_Satis_404()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS yabancı cihaz A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS yabancı cihaz B");
        var cihazA = PosYardim.CihazYaz(vt, a, "A POS 2");

        using var db = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(
            () => PosYardim.Satis(db).Satis(PosYardim.KartSatis(b, 1_000m, cihazA), b.Islem));
    }

    [Fact]
    public async Task KartBacagi_AltiCagriNoktasinin_HEPSI_PosaYazar()
    {
        // ⚠ FAZ A KABUL ÖLÇÜTÜ (Bertan + Aşkın birleşik koşulu):
        // CihazZorunlu=true kiracıda her KART kasa hareketi için TAM BİR
        // pos_tahsilatlari satırı bulunmalı. Altı nokta tek tek dolaşılır:
        // satis · satis-fisi · satis-iade · alis-iade · takas · iscilik.
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "POS 6 nokta", stokAdet: 200m), 2_000m);
        var cihaz = PosYardim.CihazYaz(vt, k);
        using var db = vt.Olustur(k);
        var satis = PosYardim.Satis(db, Zorunlu);

        // (1) satis
        await satis.Satis(PosYardim.KartSatis(k, 1_000m, cihaz), k.Islem);
        var satisId = SonHareket(k, "SATIS");

        // (2) satis-fisi (karma yolun kart bacağı)
        await satis.SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = k.CariId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 2_000m }],
            Odemeler = [new Kuyumcu.Api.Modules.Satis.OdemeBacagiDto
                { Arac = "KART", TutarTl = 2_000m, KasaId = k.KasaId, PosCihazId = cihaz }],
        }, k.Islem);

        // (3) satis-iade
        await satis.SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = satisId, Adet = 1m, OdemeTuru = "KART",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, PosCihazId = cihaz,
        }, k.Islem);

        // (4) alis-iade — önce kartla alış yapılır
        await satis.Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 500m,
            OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        var alisId = SonHareket(k, "ALIS");
        await satis.AlisIade(new MSatis.IadeDto
        {
            KaynakHareketId = alisId, Adet = 1m, OdemeTuru = "KART",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, PosCihazId = cihaz,
        }, k.Islem);

        // (5) takas — fark kartla tahsil edilir
        await satis.Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 1m,
            EskiUrunBirimFiyatTlHasGram = 1_000m,   // PAKET GOC-C (#7): 0 TL/has gram takas fiyatı artık 400 — geçerli fikstür
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 3_000m,
            KasaId = k.KasaId, OdemeTuru = "KART", KurSnapshotId = k.KurId, PosCihazId = cihaz,
        }, k.Islem);

        // (6) iscilik
        await satis.Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 250m, KasaId = k.KasaId,
            OdemeTuru = "KART", KurSnapshotId = k.KurId, PosCihazId = cihaz,
        }, k.Islem);

        // DENETİM: her KART kasa hareketinin bir POS satırı OLMALI.
        using var kontrol = vt.OlusturSistem();
        // ⚠ KAPSAM: POS defteri TAHSİLAT defteridir. Kartla yapılan ALIŞ
        // ÖDEMESİ (paranın bizden çıktığı bacak) POS cihazından geçmez ve
        // satır doğurmaz — bu bilinçli bir kapsam sınırıdır, eksiklik değil.
        var alisHareketleri = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS")
            .Select(h => h.Id).ToList();
        var kartHareketleri = kontrol.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.OdemeTuru == "KART"
                        && (h.HareketId == null || !alisHareketleri.Contains(h.HareketId)))
            .Select(h => h.Id).ToList();
        var posBagli = kontrol.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == k.TenantId && t.KasaHareketId != null)
            .Select(t => t.KasaHareketId!).ToList();

        Assert.Equal(6, kartHareketleri.Count);
        var eksik = kartHareketleri.Except(posBagli).ToList();
        Assert.True(eksik.Count == 0,
            $"KART kasa hareketi var ama POS tahsilatı yok: {string.Join(", ", eksik)}");
    }

    private string SonHareket(Kiraci k, string tur)
    {
        using var db = vt.OlusturSistem();
        return db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == tur)
            .OrderByDescending(h => h.Olusturuldu).First().Id;
    }

    /// <summary>Çalışan SQL metnini toplayan EF komut yakalayıcısı.</summary>
    private sealed class SqlYakalayici : DbCommandInterceptor
    {
        public List<string> Komutlar { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Komutlar.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
