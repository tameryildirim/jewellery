using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CIFT-BACAK (30.09) — sıfırdan müşteri senaryosunun 3 HATA'sının
// gerileme testleri (kaynak: ham/SENARYO-CIFT-BACAK-2026-09-30.md).
//
//   HATA-1 (5.5 · 9.6): altın dışı emanet hareketinin stornosu ALTIN has
//          kolonuna yazıyordu; ayna aynı hatayı kopyaladığı için uyuşmazlık 0.
//   HATA-2 (5.3 · 9.3): iade edilmiş emanetin GİRİŞİ stornolanabiliyordu →
//          emanet defteri eksiye düşüyordu.
//   HATA-3 (7.3 · Y1): takasta alınan eski altın hurda bakiyesine girmiyordu.
//
// Mutasyon kodları (rapor § Mutasyon): MCB-1 … MCB-6 — her testin özeti hangi
// mutasyonun onu kırmızıya çevirdiğini yazar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CiftBacakTestleri(TestVeritabani vt)
{
    private const string Gerekce = "CIFT-BACAK gerileme testi — yeterince uzun gerekçe";

    private static MEmanet.EmanetServisi Emanet(KuyumcuDbContext db) =>
        new(db, NullLogger<MEmanet.EmanetServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"PAKETCIFT-BACAK-TEST {ad}"), 2_000m);
    }

    private async Task<string> EmanetGiris(Kiraci k, string ayarId, decimal gram)
    {
        using (var db = vt.Olustur(k))
            await Emanet(db).Giris(new MEmanet.EmanetGirisDto
            {
                CariId = k.CariId, AyarId = ayarId, GramAgirlik = gram, KurSnapshotId = k.KurId,
                Aciklama = "CIFT-BACAK emanet",
            }, k.Islem);
        using var oku = vt.OlusturSistem();
        return await oku.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS" && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .Select(h => h.Id).FirstAsync();
    }

    private async Task<string> EmanetCikis(Kiraci k, string maden, decimal has)
    {
        using (var db = vt.Olustur(k))
            await Emanet(db).Cikis(k.CariId, new MEmanet.EmanetCikisDto
            {
                HasGrami = has, Maden = maden, KurSnapshotId = k.KurId, Aciklama = "CIFT-BACAK iade",
            }, k.Islem);
        using var oku = vt.OlusturSistem();
        return await oku.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_CIKIS" && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .Select(h => h.Id).FirstAsync();
    }

    private async Task StornoEt(Kiraci k, string hareketId)
    {
        using var db = vt.Olustur(k);
        await Storno(db).Storno(hareketId, new MSatis.StornoDto
        {
            Gerekce = Gerekce, KurSnapshotId = k.KurId,
        }, k.Islem);
    }

    private (decimal Tl, decimal Has) Bakiye(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.CariBakiye(db, k.CariId);
    }

    private decimal EmanetNet(Kiraci k, string maden)
    {
        using var db = vt.OlusturSistem();
        return db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.CariId == k.CariId
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == maden)
            .Sum(h => h.HasGrami);
    }

    /// <summary>Ekstre yanıtı (kural aynası) — anonim nesne JSON'a çevrilip okunur.</summary>
    private async Task<JsonElement> Ekstre(Kiraci k)
    {
        using var db = vt.Olustur(k);
        var o = await Cari(db).Ekstre(k.CariId, k.Islem, null, null, 1000, 0);
        return JsonSerializer.SerializeToElement(o);
    }

    // ═══════════ HATA-1 — altın dışı emanetin stornosu altın kolonuna yazmaz ═══════

    /// <summary>
    /// Senaryo 5.5. MCB-1 (StornoServisi.CariEtkisi eski "−h.HasGrami" kuralına
    /// dönerse) ve MCB-3 (EmanetKurali.CariHasEtkisi madene bakmazsa) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task GumusEmanetGirisStornosu_altin_has_kolonunu_degistirmez()
    {
        var k = YeniKiraci("gumus giris storno");
        var giris = await EmanetGiris(k, "G925", 50m);          // 46,25 gümüş has
        Assert.Equal((0m, 0m), Bakiye(k));

        await StornoEt(k, giris);

        Assert.Equal((0m, 0m), Bakiye(k));                       // altın kolonu OYNAMAZ
        Assert.Equal(0m, EmanetNet(k, Maden.Gumus));             // emanet gümüş −46,25 → 0
    }

    /// <summary>
    /// Senaryo 9.6: gümüş İADESİNİN stornosu da altın kolonuna yazmaz. MCB-1 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task GumusEmanetIadeStornosu_altin_has_kolonunu_degistirmez()
    {
        var k = YeniKiraci("gumus iade storno");
        await EmanetGiris(k, "G925", 20m);                       // 18,5
        var cikis = await EmanetCikis(k, Maden.Gumus, 18.5m);
        Assert.Equal(0m, EmanetNet(k, Maden.Gumus));

        await StornoEt(k, cikis);

        Assert.Equal((0m, 0m), Bakiye(k));
        Assert.Equal(18.5m, EmanetNet(k, Maden.Gumus));          // emanet yeniden bizde
    }

    /// <summary>
    /// Altın emanetin stornosu altın kolonunu TERSLEMEYE DEVAM eder (senaryo 6.4b OK
    /// kalmalı). MCB-3'ün tersi (kural her madende 0 dönerse) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task AltinEmanetGirisStornosu_altin_kolonunu_tersler()
    {
        var k = YeniKiraci("altin giris storno");
        var giris = await EmanetGiris(k, "K22", 10m);            // 9,16
        Assert.Equal(-9.16m, Bakiye(k).Has);

        await StornoEt(k, giris);

        Assert.Equal(0m, Bakiye(k).Has);
        Assert.Equal(0m, EmanetNet(k, Maden.Altin));
    }

    /// <summary>
    /// Ayna (ekstre) storno satırını da madene göre sayar: kayıtlı = ayna, emanette
    /// negatif OLMAZ. MCB-2 (ayna eski "storno || ALTIN" kuralına dönerse) KIRMIZI:
    /// ayna gümüş stornosunu −hasGrami sayar, kayıtlı 0 kalır → uyuşmazlık ≠ 0.
    /// </summary>
    [Fact]
    public async Task Ayna_gumus_emanet_stornosunu_altin_saymaz()
    {
        var k = YeniKiraci("ayna");
        var giris = await EmanetGiris(k, "G925", 50m);
        await StornoEt(k, giris);
        var cikisGiris = await EmanetGiris(k, "G925", 20m);
        _ = cikisGiris;
        var cikis = await EmanetCikis(k, Maden.Gumus, 18.5m);
        await StornoEt(k, cikis);

        var j = await Ekstre(k);
        Assert.Equal(0m, j.GetProperty("bakiyeUyusmazligiHas").GetDecimal());
        Assert.Equal(0m, j.GetProperty("emanetteHas").GetDecimal());
        Assert.Equal(0m, j.GetProperty("bakiye").GetProperty("hasGramBakiye").GetDecimal());
        foreach (var s in j.GetProperty("hareketler").EnumerateArray())
            Assert.Equal(0m, s.GetProperty("cariHasEtkisi").GetDecimal());
    }

    /// <summary>Saf: tek kural üç tüketicide aynı sonucu verir (yazan · tersleyen · ayna).</summary>
    [Theory]
    [InlineData("GUMUS", -46.25, false, 0)]
    [InlineData("GUMUS", -46.25, true, 0)]
    [InlineData("PLATIN", 4.75, true, 0)]
    [InlineData("ALTIN", -9.16, true, 9.16)]
    [InlineData("ALTIN", 9.16, false, -9.16)]
    public void Ortak_kural_ayna_ile_ayni(string maden, double has, bool storno, double beklenen)
    {
        var h = (decimal)has;
        Assert.Equal((decimal)beklenen, MEmanet.EmanetKurali.CariHasEtkisi(maden, h));
        Assert.Equal((decimal)beklenen, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", h, maden, storno));
        Assert.Equal((decimal)beklenen, MCari.CariServisi.CariHasEtkisi("EMANET_CIKIS", h, maden, storno));
    }

    // ═══════════ HATA-2 — iade edilmiş emanetin girişi stornolanamaz ═══════════════

    /// <summary>
    /// Senaryo 5.3 (gümüş) ve 9.3 (altın). MCB-4 (elde kalan kapısı silinirse /
    /// karşılaştırma tersine dönerse) KIRMIZI: 200 döner, defter eksiye düşer.
    /// </summary>
    [Theory]
    [InlineData("G925", 100, "GUMUS", 92.5)]
    [InlineData("K22", 10, "ALTIN", 9.16)]
    public async Task IadeEdilmisEmanetinGirisStornosu_409_ve_hicbir_sey_yazilmaz(
        string ayarId, double gram, string maden, double has)
    {
        var k = YeniKiraci($"iade edilmis {maden}");
        var giris = await EmanetGiris(k, ayarId, (decimal)gram);
        await EmanetCikis(k, maden, (decimal)has);
        var bakiyeOnce = Bakiye(k);
        int satirOnce;
        using (var s = vt.OlusturSistem())
            satirOnce = s.Hareketler.Count(h => h.TenantId == k.TenantId);

        using (var db = vt.Olustur(k))
        {
            var x = await Assert.ThrowsAsync<ConflictException>(() => Storno(db).Storno(giris,
                new MSatis.StornoDto { Gerekce = Gerekce, KurSnapshotId = k.KurId }, k.Islem));
            Assert.Equal("Bu emanetin bir kısmı iade edildi; önce iadeyi geri alın.", x.Message);
        }

        Assert.Equal(bakiyeOnce, Bakiye(k));
        Assert.Equal(0m, EmanetNet(k, maden));                   // eksiye DÜŞMEDİ
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(satirOnce, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    /// <summary>
    /// Kısmi iade de engeller (elde 4,16 &lt; giriş 9,16); doğru yol önce iadenin
    /// stornosudur — ardından giriş stornosu 200 ve defter tam sıfırlanır (senaryo 6.5c/d).
    /// MCB-5 (karşılaştırma "&lt;" yerine "&lt; 0" ya da "&lt;= 0" olursa) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task KismiIade_giris_stornosunu_engeller_iade_geri_alininca_acilir()
    {
        var k = YeniKiraci("kismi iade");
        var giris = await EmanetGiris(k, "K22", 10m);            // 9,16
        var cikis = await EmanetCikis(k, Maden.Altin, 5m);        // elde 4,16

        using (var db = vt.Olustur(k))
            await Assert.ThrowsAsync<ConflictException>(() => Storno(db).Storno(giris,
                new MSatis.StornoDto { Gerekce = Gerekce, KurSnapshotId = k.KurId }, k.Islem));

        await StornoEt(k, cikis);                                // iade geri alındı → elde 9,16
        await StornoEt(k, giris);                                // artık serbest

        Assert.Equal(0m, EmanetNet(k, Maden.Altin));
        Assert.Equal(0m, Bakiye(k).Has);
    }

    /// <summary>
    /// Kapı madenleri KARIŞTIRMAZ: (a) altın iade edilmişken elde duran GÜMÜŞ altın
    /// girişinin stornosunu AÇMAZ (409); (b) gümüş iade edilmişken elde duran altının
    /// girişi serbesttir. MCB-6 (kapı madene bakmadan toplam okursa) KIRMIZI: (a)'da
    /// 9,16 − 9,16 + 18,5 = 18,5 ≥ 9,16 görünür ve storno geçer.
    /// </summary>
    [Fact]
    public async Task Elde_kalan_kapisi_madenleri_karistirmaz()
    {
        var a = YeniKiraci("maden ayrimi a");
        var altinA = await EmanetGiris(a, "K22", 10m);           // altın 9,16
        await EmanetCikis(a, Maden.Altin, 9.16m);                 // altın tamamen iade
        await EmanetGiris(a, "G925", 20m);                        // gümüş 18,5 elde
        using (var db = vt.Olustur(a))
            await Assert.ThrowsAsync<ConflictException>(() => Storno(db).Storno(altinA,
                new MSatis.StornoDto { Gerekce = Gerekce, KurSnapshotId = a.KurId }, a.Islem));
        Assert.Equal(0m, EmanetNet(a, Maden.Altin));

        var b = YeniKiraci("maden ayrimi b");
        var altinB = await EmanetGiris(b, "K22", 10m);           // altın 9,16 elde
        await EmanetGiris(b, "G925", 20m);
        await EmanetCikis(b, Maden.Gumus, 18.5m);                 // gümüş tamamen iade
        await StornoEt(b, altinB);                                // altın elde → 200
        Assert.Equal(0m, EmanetNet(b, Maden.Altin));
        Assert.Equal(0m, Bakiye(b).Has);
    }

    // ═══════════ HATA-3 — takasta alınan eski altın hurdaya girer ═════════════════

    private async Task Takas(Kiraci k, decimal eskiGram = 20m)
    {
        using var db = vt.Olustur(k);
        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = eskiGram,
            EskiUrunBirimFiyatTlHasGram = 1_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 20_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
    }

    /// <summary>
    /// Senaryo 7.3: 20 g K22 eski altın (18,32 has) → yeni ürün (9,16 has). Hurdaya
    /// NET has (9,16) değil ALINAN eski has (18,32) girer; yeni ürün stoktan çıkar.
    /// MCB-7 (TAKAS hurda türetmesinden çıkarılırsa → 0) ve MCB-8 (eski has yerine
    /// net has sayılırsa → 9,16) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Takas_eski_altin_hurda_bakiyesine_girer_yeni_urun_stoktan_cikar()
    {
        var k = YeniKiraci("takas hurda");
        decimal stokOnce;
        using (var s = vt.OlusturSistem())
            stokOnce = s.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId).Adet;

        await Takas(k);

        using var db = vt.Olustur(k);
        Assert.Equal(18.32m, await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, Maden.Altin));
        var satir = Assert.Single(await MHurda.HurdaStokKapisi.Bakiyeler(db, k.Islem));
        Assert.Equal(18.32m, satir.HasGram);
        Assert.Equal(18.32m, satir.GirenHas);

        var kirilim = await MHurda.HurdaStokKapisi.KiraciKirilimi(db, k.TenantId);
        Assert.Equal(18.32m, kirilim.Sum(x => x.HasToplam));     // pozisyon da aynı hurdayı görür

        using var s2 = vt.OlusturSistem();
        Assert.Equal(stokOnce - 1m, s2.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId).Adet);
        var takas = s2.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "TAKAS");
        Assert.Equal(9.16m, takas.HasGrami);                     // defter satırı NET kalır (değişmedi)
        Assert.Equal(1m, takas.Adet);
        Assert.Equal(18.32m, takas.TakasEskiHasGrami);           // tur 2: eski has işlem anında DONAR (MCB-10)
    }

    /// <summary>
    /// Sonda Y1: takasta alınan eski altın rafineriye SATILABİLİR (400 "yeterli hurda
    /// yok" dönmez) ve satıştan sonra hurda sıfırlanır. MCB-7/MCB-8 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Takas_eski_altini_hurda_satisinda_reddedilmez()
    {
        var k = YeniKiraci("takas hurda satis");
        await Takas(k);
        IslemBantFikstur.ReferansKur(vt.OlusturSistem(), k.TenantId, 1_000m);   // ISLEM-BANT: hurda satış fiyatı 1.000 ± %20 bandında

        using (var db = vt.Olustur(k))
            await Satis(db).HurdaSatis(new MSatis.HurdaSatisDto
            {
                Maden = Maden.Altin, HasGrami = 18.32m, BirimFiyatTlHasGram = 1_000m,
                KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        using var oku = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
        await Assert.ThrowsAnyAsync<ApiException>(
            () => MHurda.HurdaStokKapisi.DogrulaCikis(oku, k.Islem, Maden.Altin, 0.000001m));
    }

    /// <summary>
    /// Tur 2 (hasım KRİTİK): takastan sonra verilen ürünün kartı düzeltilirse
    /// (gram 10 → 12, sonra 10 → 8) geçmiş hurda bakiyesi, kartın "giren" kalemi
    /// ve hurda defterindeki takas satırı KAYMAZ — alınan eski has satıra
    /// donmuştur. MCB-9 (türetme ürün kartına bağlanırsa → 20,152) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Takas_eski_hasi_donar_urun_karti_duzeltilince_hurda_kaymaz()
    {
        var k = YeniKiraci("takas donar");
        await Takas(k);

        foreach (var gram in new[] { 12m, 8m })
        {
            using (var db = vt.Olustur(k))
                await new MUrun.UrunServisi(db, TestKurulum.Denetim(db))
                    .Guncelle(k.UrunId, new MUrun.UrunGuncelleDto { GramAgirlik = gram }, k.Islem);

            using var oku = vt.Olustur(k);
            Assert.Equal(18.32m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
            var satir = Assert.Single(await MHurda.HurdaStokKapisi.Bakiyeler(oku, k.Islem));
            Assert.Equal(18.32m, satir.GirenHas);
            var kirilim = await MHurda.HurdaStokKapisi.KiraciKirilimi(oku, k.TenantId);
            Assert.Equal(18.32m, kirilim.Sum(x => x.HasToplam));
        }
    }

    /// <summary>
    /// Tur 2 (hasım ORTA): hurda kartının "giren" kalemi ile hurda DEFTERİ
    /// listesindeki satırlar aynı kaynaktan gelir — takasın eski bacağı defterde
    /// "TAKAS" satırı olarak, NET has değil ALINAN eski has ile ve TL tutarı 0
    /// (hurda bedeli değil) görünür; girenlerin toplamı kartın girenine eşittir.
    /// MCB-11 (DefterSorgu takası saymazsa) ve MCB-12 (DefterHas net has
    /// döndürürse) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Hurda_defteri_takas_satirini_eski_has_ile_gosterir_kart_girenle_tutar()
    {
        var k = YeniKiraci("takas defter");
        await Takas(k);
        using (var db = vt.Olustur(k))
            await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
            {
                AyarId = "K22", GramAgirlik = 3m, BirimFiyatTlHasGram = 1_000m,
                KasaId = k.KasaId, KurSnapshotId = k.KurId, CariId = k.CariId,
                Satici = TestKurulum.HurdaSatici(),
            }, k.Islem);

        using var oku = vt.Olustur(k);
        var stok = new MHurda.HurdaStokServisi(oku, new MHurda.HurdaAyarlari { FireKaydiAcik = false });
        var defter = await stok.Defter(k.Islem);
        var takasSatiri = Assert.Single(defter.Satirlar, s => s.Tur == "TAKAS");
        Assert.Equal(18.32m, takasSatiri.HasGrami);
        Assert.Equal(0m, takasSatiri.TlTutari);
        Assert.Equal(2, defter.ToplamAdet);

        var kart = Assert.Single((await stok.Bakiye(k.Islem)).Satirlar);
        Assert.Equal(kart.GirenHas, defter.Satirlar.Sum(s => s.HasGrami));
        Assert.Equal(18.32m + 2.748m, kart.GirenHas);
    }

    /// <summary>
    /// Bu paketten önce yazılmış (adetsiz) TAKAS satırı hurdaya GİRMEZ — adet
    /// uydurulmaz; bugünkü davranış birebir korunur (rapor § Geçmiş TAKAS).
    /// </summary>
    [Fact]
    public async Task Adetsiz_eski_takas_satiri_hurdaya_girmez()
    {
        var k = YeniKiraci("adetsiz takas");
        using (var db = vt.Olustur(k))
        {
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAKAS",
                Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = 1.145m, Maden = Maden.Altin,
                KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using var oku = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
        Assert.Empty(await MHurda.HurdaStokKapisi.Bakiyeler(oku, k.Islem));
    }
}

// ─── HTTP: 409 gövdesi Türkçe ve ayrıştırılarak sınanır ──────────────────────

[Collection("http")]
public sealed class CiftBacakHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "CIFT-BACAK HTTP testi — yeterince uzun gerekçe";

    /// <summary>
    /// Senaryo 5.3 uçtan uca: POST /islemler/{id}/storno → 409 + Türkçe sebep;
    /// yanıt gövdesi JSON olarak ayrıştırılır. MCB-4 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task IadeEdilmisGumusEmanetGirisStornosu_HTTP_409_Turkce()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
            k = TestKurulum.KiraciKur(db, $"PAKETCIFT-BACAK-TEST http {Guid.NewGuid():N}"[..40]);
        var c = f.Ortam.Istemci(k, "SAHIP");

        var g = await c.PostJson("/api/v1/emanet/giris",
            new { cariId = k.CariId, ayarId = "G925", gramAgirlik = 100m, kurSnapshotId = k.KurId });
        Assert.True(g.IsSuccessStatusCode, await g.Content.ReadAsStringAsync());
        var cik = await c.PostJson($"/api/v1/emanet/{k.CariId}/cikis",
            new { hasGrami = 92.5m, maden = "GUMUS", kurSnapshotId = k.KurId });
        Assert.True(cik.IsSuccessStatusCode, await cik.Content.ReadAsStringAsync());

        string girisId;
        using (var db = f.Vt.OlusturSistem())
            girisId = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS")
                .Select(h => h.Id).SingleAsync();

        var r = await c.PostJson($"/api/v1/islemler/{girisId}/storno",
            new { gerekce = Gerekce, kurSnapshotId = k.KurId });
        var govde = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        using var j = JsonDocument.Parse(govde);
        var metin = string.Join(" | ", j.RootElement.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.String).Select(p => p.Value.GetString()));
        Assert.Contains("Bu emanetin bir kısmı iade edildi; önce iadeyi geri alın.", metin);

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
        Assert.False(await kontrol.Hareketler.AnyAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
    }
}
