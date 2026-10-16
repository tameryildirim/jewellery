using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİŞTEN FATURA — paket 1 Faz A [DB]
//
// ZO-BELGE 30.09 (keşif K01): eski "MİKTAR = 1" sözleşmesi KALDIRILDI — adet
// kolonu (P-08/Y-14) açıldı; her hareket tek kalemdir, MİKTARI defterden okunur,
// satır toplamı defterdeki tutardır (bkz. § 2).
//
// Diğer çekirdek sözleşmeler:
//   • FİŞİN TÜM KALEMLERİ GİRER ve HEPSİ aynı fatura no ile damgalanır (B-4).
//   • Bir fiş yalnız bir faturaya bağlanır (ikinci deneme 400).
//   • Hediye kalemi BELGEDE görünür ama NET BEDELİ 0'dır: indirim = liste × adet.
//   • Has bedeli SUNUCUDA defterden türetilir; istemci gönderemez.
//   • Yabancı kiracının fişi 404 (kiracı sınırı — Mehmet K1-1).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FaturaFistenTestleri(TestVeritabani vt)
{
    private const decimal KurHasFiyati = 6_827.20m;      // TestKurulum.KiraciKur

    /// <summary>Verilen TL tutarlarıyla tek fiş altında hareket yazar; fis_id döner.</summary>
    private static string FisYaz(KuyumcuDbContext kurulum, Kiraci k, params decimal[] tutarlar)
    {
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        foreach (var tl in tutarlar)
            kurulum.Hareketler.Add(Satir(k, fisId, tl, now));
        kurulum.SaveChanges();
        return fisId;
    }

    private static Hareket Satir(Kiraci k, string? fisId, decimal tl, DateTime now,
        bool hediye = false, decimal? listeFiyati = null, decimal? indirim = null,
        string? faturaNo = null) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
        TlTutari = tl, HasGrami = -1m, OdemeTuru = "KARMA", NakitTutar = tl, VeresiyeTutar = 0m,
        FisId = fisId, Hediye = hediye, ListeFiyati = listeFiyati, IndirimTutari = indirim,
        FaturaNo = faturaNo, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
        KullaniciId = k.KullaniciId, Olusturuldu = now,
    };

    private static JsonElement Oku(object cevap) =>
        JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;

    // ── 1. Fişin TÜM kalemleri girer, HEPSİ damgalanır (M-F5) ────────────────

    [Fact]
    public async Task Iki_kalemli_fisten_fatura_iki_kalem_yazar_ve_IKI_hareket_de_ayni_fatura_no_alir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 fişten");
        var fisId = FisYaz(kurulum, k, 1_200m, 2_400m);

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        var kalemler = kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToList();
        var hareketler = kontrol.Hareketler.Where(h => h.TenantId == k.TenantId).ToList();

        Assert.Equal("KALEM", fatura.KalemKaynagi);
        Assert.Equal("KALEM", fatura.HasDegerKaynagi);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(3_600.00m, fatura.Toplam);
        Assert.Equal(600.00m, fatura.KdvTutar);           // 1200→200,00 + 2400→400,00
        Assert.Equal(3_600.00m, y.GetProperty("Toplam").GetDecimal());
        // FİŞİN İKİ HAREKETİ DE aynı damgayı taşır (B-4).
        // ⚠ P-22 / B-3: TASLAK belgenin NUMARASI YOKTUR; damga TASLAK İŞARETİDİR
        //   ve numara belge kesilince yazılır. Kural (tek damga) değişmedi.
        Assert.Null(fatura.FaturaNo);
        Assert.Equal(2, hareketler.Count);
        Assert.All(hareketler, h => Assert.Equal(
            Kuyumcu.Api.Modules.Fatura.FaturaSeriKapisi.Isaret(fatura.Id), h.FaturaNo));
        // Kalem ↔ defter bağı kurulur (mutabakat raporunun dayanağı).
        Assert.Equal(
            hareketler.Select(h => h.Id).OrderBy(x => x),
            kalemler.Select(x => x.HareketId!).OrderBy(x => x));
    }

    // ── 2. KALEM MİKTARI DEFTERDEN (ZO-BELGE 30.09 — keşif K01, KRİTİK) ─────
    // Eski "ADET=1 sözleşmesi" hareketler.adet kolonu YOKKEN yazılmıştı; kolon
    // P-08/Y-14 ile açıldı. 250 adetlik satış faturaya "1 ADET × 1.095.125"
    // giriyordu. VUK md.230/4: faturada malın "miktarı, fiyatı ve tutarı" bulunur.
    // Sözleşme: miktar defterden (adet → yoksa has ÷ birim has → yoksa 1), satır
    // toplamı DEFTERDEKİ tutardır (fatura toplamı değişmez), kalem kuralı tek:
    // Yuvarla2(miktar × birim fiyat) − indirim = satır toplamı.

    private static Hareket AdetliSatir(Kiraci k, string fisId, decimal tl, decimal? adet,
        decimal hasGrami, DateTime now, string? urunId = null)
    {
        var h = Satir(k, fisId, tl, now);
        h.Adet = adet;
        h.HasGrami = -hasGrami;
        if (urunId is not null) h.UrunId = urunId;
        return h;
    }

    private static decimal UrunBirimHas(KuyumcuDbContext db, Kiraci k) =>
        db.Urunler.Where(u => u.Id == k.UrunId).Select(u => u.HasGrami).Single();

    [Fact]
    public async Task K01_Fisten_faturada_kalem_miktari_DEFTERDEKI_ADET_birim_fiyat_ve_toplam_dogru()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE K01 adet");
        var birimHas = UrunBirimHas(kurulum, k);
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(AdetliSatir(k, fisId, 1_095_125m, 250m, 250m * birimHas, now));
        kurulum.Hareketler.Add(AdetliSatir(k, fisId, 100m, 3m, 3m * birimHas, now.AddMilliseconds(5)));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        var kalemler = kontrol.FaturaKalemleri.Where(x => x.TenantId == k.TenantId).ToList();
        var buyuk = kalemler.Single(x => x.Miktar == 250m);
        Assert.Equal(4_380.50m, buyuk.BirimFiyat);                 // 1.095.125 ÷ 250, kuruşu kuruşuna
        Assert.Null(buyuk.IndirimTutari);
        Assert.Equal(1_095_125m, buyuk.SatirToplam);
        Assert.Equal("ADET", buyuk.Birim);

        // 100 ÷ 3 bölünmez: birim fiyat kuruşa YUKARI, fark açık "indirim" (gizli fark yok).
        var kucuk = kalemler.Single(x => x.Miktar == 3m);
        Assert.Equal(33.34m, kucuk.BirimFiyat);
        Assert.Equal(0.02m, kucuk.IndirimTutari);
        Assert.Equal(100m, kucuk.SatirToplam);
        Assert.All(kalemler, x => Assert.Equal(x.SatirToplam,
            Math.Round(x.Miktar * x.BirimFiyat, 2, MidpointRounding.AwayFromZero) - (x.IndirimTutari ?? 0m)));
        // Fatura toplamı DEĞİŞMEDİ: defterdeki tutarların toplamı.
        Assert.Equal(1_095_225m, fatura.Toplam);
    }

    [Fact]
    public async Task K01_Adet_kolonu_BOS_eski_satirda_miktar_has_bolu_birim_has_iade_ucuyla_ayni()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE K01 eski");
        var birimHas = UrunBirimHas(kurulum, k);
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.Add(AdetliSatir(k, fisId, 30_000m, null, 4m * birimHas, DateTime.UtcNow));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(4m, kalem.Miktar);
        Assert.Equal(7_500m, kalem.BirimFiyat);
        Assert.Equal(30_000m, kalem.SatirToplam);
    }

    [Fact]
    public async Task K01_Gramli_urunde_miktar_GRAM_ve_birim_GRAM()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE K01 gram");
        var gramUrun = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Urunler.Add(new Urun
        {
            Id = gramUrun, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{gramUrun[..8]}", Ad = "Gram hurda",
            AyarId = "K22", Milyem = 916m, GramAgirlik = 1m, TasAgirlik = 0m, HasGrami = 0.916m, Birim = "GRAM",
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.Add(AdetliSatir(k, fisId, 50_000m, 12.5m, 12.5m * 0.916m, now, gramUrun));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(12.5m, kalem.Miktar);
        Assert.Equal("GRAM", kalem.Birim);
        Assert.Equal(4_000m, kalem.BirimFiyat);
        Assert.Equal(50_000m, kalem.SatirToplam);
    }

    [Fact]
    public async Task K01_Adetsiz_ve_turetilemeyen_eski_satirda_miktar_1_uydurulmaz_toplam_ayni()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 adet1");
        // Satir(): has -1 g, ürün kartı ~9,16 g has → 0,109 "adet" TAM SAYI değil: kart
        // satıştan sonra değişmiş sayılır, kesirli adet UYDURULMAZ, tek kalem (1) yazılır.
        var fisId = FisYaz(kurulum, k, 1_000m, 2_000m, 3_000m);

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalemler = kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToList();
        Assert.Equal(3, kalemler.Count);
        Assert.All(kalemler, x => Assert.Equal(1m, x.Miktar));
        Assert.Equal([1_000m, 2_000m, 3_000m], kalemler.Select(x => x.BirimFiyat).OrderBy(x => x));
        Assert.Equal([1_000m, 2_000m, 3_000m], kalemler.Select(x => x.SatirToplam).OrderBy(x => x));
        Assert.Equal([1, 2, 3], kalemler.Select(x => x.Sira));
    }

    [Fact]
    public async Task K01_Iki_adetlik_hediye_kalemi_liste_birim_fiyat_indirim_liste_carpi_adet_0_bedel()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE K01 hediye");
        var birimHas = UrunBirimHas(kurulum, k);
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(AdetliSatir(k, fisId, 5_000m, 1m, birimHas, now));
        var hediye = AdetliSatir(k, fisId, 0m, 2m, 2m * birimHas, now.AddMilliseconds(5));
        hediye.Hediye = true; hediye.ListeFiyati = 750m; hediye.IndirimTutari = 1_500m;
        kurulum.Hareketler.Add(hediye);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId && x.SatirToplam == 0m);
        Assert.Equal(2m, h.Miktar);
        Assert.Equal(750m, h.BirimFiyat);
        Assert.Equal(1_500m, h.IndirimTutari);
    }

    [Theory]
    [InlineData(1_095_125, 250, 4_380.50, null)]
    [InlineData(100, 3, 33.34, 0.02)]
    [InlineData(1_000, 1, 1_000, null)]
    [InlineData(50_000, 12.5, 4_000, null)]
    [InlineData(10, 0.333, 30.03, 0.0)]   // 10 ÷ 0,333 = 30,03 → 0,333 × 30,03 = 9,99999 → 10,00: indirim yok
    public void K01_Kalem_fiyati_kurali_satir_toplami_defterdeki_tutar(
        double tutar, double miktar, double beklenenBirim, double? beklenenIndirim)
    {
        var (birim, indirim) = MFatura.FaturaServisi.FistenKalemFiyati((decimal)tutar, (decimal)miktar);
        Assert.Equal((decimal)beklenenBirim, birim);
        Assert.Equal(beklenenIndirim is null or 0.0 ? null : (decimal?)(decimal)beklenenIndirim, indirim);
        Assert.Equal((decimal)tutar,
            Math.Round((decimal)miktar * birim, 2, MidpointRounding.AwayFromZero) - (indirim ?? 0m));
    }

    [Fact]
    public void K01_Miktar_kaynak_sirasi_adet_sonra_has_sonra_1()
    {
        var h = new Hareket { HasGrami = -27.48m, Adet = 3m };
        Assert.Equal(3m, MFatura.FaturaServisi.FistenKalemMiktari(h, 9.16m, "ADET"));
        h.Adet = null;
        Assert.Equal(3m, MFatura.FaturaServisi.FistenKalemMiktari(h, 9.16m, "ADET"));
        Assert.Equal(1m, MFatura.FaturaServisi.FistenKalemMiktari(h, 10m, "ADET"));   // 2,748 adet uydurulmaz
        Assert.Equal(2.748m, MFatura.FaturaServisi.FistenKalemMiktari(h, 10m, "GRAM"));
        Assert.Equal(1m, MFatura.FaturaServisi.FistenKalemMiktari(h, null, null));    // ürünsüz satır
        h.Adet = 0m;
        Assert.Equal(3m, MFatura.FaturaServisi.FistenKalemMiktari(h, 9.16m, "ADET"));
    }

    // ── 3. Aynı fişten ikinci fatura (M-F7) ──────────────────────────────────

    [Fact]
    public async Task Ayni_fisten_ikinci_fatura_400_ve_mesaj_fatura_numarasini_tasir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 çift fatura");
        var fisId = FisYaz(kurulum, k, 1_000m, 500m);

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.FistenOlustur(new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var db2 = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db2).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem));

        Assert.Contains("zaten", hata.Message);
        Assert.Contains("faturalandırılmış", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Faturalar.Where(f => f.TenantId == k.TenantId));
        Assert.Equal(2, kontrol.FaturaKalemleri.Count(x => x.TenantId == k.TenantId));
    }

    // ── 4. Hediye kalemi (M-F11) ─────────────────────────────────────────────

    [Fact]
    public async Task Hediyeli_fisten_faturada_hediye_kalemi_0_bedel_ve_indirim_liste_kadar_girer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 hediye");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(Satir(k, fisId, 5_000m, now));                                   // bedelli
        kurulum.Hareketler.Add(Satir(k, fisId, 0m, now, hediye: true,
            listeFiyati: 750m, indirim: 750m));                                                 // hediye
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalemler = kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToList();
        var hediye = kalemler.Single(x => x.SatirToplam == 0m);
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);

        // Kalem BELGEDE görünür (liste fiyatıyla) ama NET BEDELİ 0'dır.
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(750.00m, hediye.BirimFiyat);
        Assert.Equal(750.00m, hediye.IndirimTutari);
        Assert.Equal(0.00m, hediye.SatirToplam);
        Assert.Equal(0.00m, hediye.KdvTutar);
        Assert.Equal(0.00m, hediye.KdvMatrah);
        // Hediye fatura toplamını BÜYÜTMEZ.
        Assert.Equal(5_000.00m, fatura.Toplam);
    }

    /// <summary>
    /// UX-4c, dalga 1 doğrulayıcısının taşınan bulgusu (a): liste_fiyati NULL
    /// hediye kaleminde birim fiyat 0, indirim 0 yazılıyor ve satır toplamı 0
    /// çıktığı için "hediye 0 bedelle girmelidir" kapısı da SESSİZCE geçiyordu.
    /// Sonuç: faturada bedeli hiç görünmeyen bir hediye kalemi — GUT I/B-10.1'in
    /// "yanında bedelli mal vardır" savunması belgeyle desteklenemez.
    /// Artık FAIL-CLOSED: veri eksikse varsayılmaz, durulur (Anayasa md.1).
    /// </summary>
    [Fact]
    public async Task Hediye_kaleminde_LISTE_FIYATI_YOKSA_fatura_400_ile_REDDEDILIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETUX4c hediye liste yok");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(Satir(k, fisId, 5_000m, now));                       // bedelli
        kurulum.Hareketler.Add(Satir(k, fisId, 0m, now, hediye: true));             // liste_fiyati NULL
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem));

        Assert.Contains("hediye kaleminde liste fiyatı yok", hata.Message);

        // Ve HİÇBİR ŞEY yazılmadı: yarım fatura kalmaz.
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Faturalar.Where(f => f.TenantId == k.TenantId));
        Assert.Empty(kontrol.FaturaKalemleri.Where(f => f.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Hediye_kaleminde_liste_fiyati_SIFIR_da_REDDEDILIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETUX4c hediye liste sifir");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(Satir(k, fisId, 5_000m, now));
        kurulum.Hareketler.Add(Satir(k, fisId, 0m, now, hediye: true, listeFiyati: 0m, indirim: 0m));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem));

        Assert.Contains("hediye kaleminde liste fiyatı yok", hata.Message);
    }

    // ── 5. Tek bacaklı (fis_id NULL) hareket ─────────────────────────────────

    [Fact]
    public async Task Fis_id_NULL_hareketten_fatura_tek_kalem_yazar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 tek hareket");
        var now = DateTime.UtcNow;
        var h = Satir(k, null, 1_800m, now);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { HareketId = h.Id }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        Assert.Equal(1_800.00m, kalem.SatirToplam);
        Assert.Equal(h.Id, kalem.HareketId);
        // P-22 / B-3: taslakta damga TASLAK İŞARETİDİR (numara kesimde verilir).
        Assert.Equal(Kuyumcu.Api.Modules.Fatura.FaturaSeriKapisi.Isaret(fatura.Id),
                     kontrol.Hareketler.Single(x => x.Id == h.Id).FaturaNo);
    }

    // ── 6. Elle damgalanmış kalem ────────────────────────────────────────────

    [Fact]
    public async Task Fisin_bir_kalemi_elle_damgalanmissa_fisten_fatura_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 elle damga");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(Satir(k, fisId, 1_000m, now));
        kurulum.Hareketler.Add(Satir(k, fisId, 2_000m, now, faturaNo: "FTR-2026-000042"));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem));
        Assert.Contains("FTR-2026-000042", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.FaturaKalemleri.Where(x => x.TenantId == k.TenantId));
    }

    // ── 7. Kiracı sınırı (M-F12 Faz A karşılığı) ─────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_fis_id_si_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "PAKET1 kiracı A");
        var b = TestKurulum.KiraciKur(kurulum, "PAKET1 kiracı B");
        var yabanciFis = FisYaz(kurulum, b, 900m);

        // ⚠ ÇOK KATMANLI KORUMA TUZAĞI (Aşkın'ın bağlayıcı koşulu): kiracıya
        // sabitlenmiş bağlamda global HasQueryFilter zaten 404 üretir ve servisin
        // KENDİ TenantId şartı ölçülemez. Bu yüzden servis SİSTEM bağlamında
        // (filtre KAPALI) A'nın işlemiyle çağrılır: tek koruma servisin açık
        // `h.TenantId == islem.TenantId` şartıdır.
        using var db = vt.OlusturSistem();
        var hata = await Assert.ThrowsAsync<NotFoundException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = yabanciFis }, a.Islem));
        // Mesaj SINANIR: fiş kümesi sorgusunun kendi kiracı şartı ölçülsün.
        // "Cari bulunamadı" ile geçilseydi ARKADAKİ kapı ölçülmüş olurdu ve fişin
        // satırları yabancı kiracıya açılmış olurdu (çok katmanlı koruma tuzağı).
        Assert.Equal("Fiş bulunamadı", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Faturalar.Where(f => f.TenantId == a.TenantId));
        Assert.All(kontrol.Hareketler.Where(h => h.TenantId == b.TenantId),
            h => Assert.Null(h.FaturaNo));
    }

    // ── 8. KUYUM: has bedeli SUNUCUDA defterden türetilir ────────────────────

    [Fact]
    public async Task KUYUM_fisten_faturada_has_bedeli_defterden_turetilir_istemci_gonderemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 kuyum fişten");
        var fisId = FisYaz(kurulum, k, 10_000m, 12_000m);       // her satırda 1 has gram

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        var kalemler = kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToList();

        // has bedeli = |has_grami| × kur kaydındaki altin_has_fiyati (kalem başına)
        Assert.Equal(KurHasFiyati, kalemler[0].HasDeger);
        Assert.Equal(KurHasFiyati, kalemler[1].HasDeger);
        Assert.Equal(2 * KurHasFiyati, fatura.HasDeger);
        // KDV yalnız (satır toplamı − has bedeli) üzerinden — her kalem AYRI
        // (kalem sırası defterin Olusturuldu+Id sırasıdır, tutar sırası değil).
        foreach (var kalem in kalemler)
        {
            var matrahKdvDahil = kalem.SatirToplam - KurHasFiyati;
            var beklenen = Math.Round(
                matrahKdvDahil - Math.Round(matrahKdvDahil / 1.20m, 2, MidpointRounding.AwayFromZero),
                2, MidpointRounding.AwayFromZero);
            Assert.Equal(beklenen, kalem.KdvTutar);
        }
        Assert.Equal([10_000m, 12_000m], kalemler.Select(x => x.SatirToplam).OrderBy(x => x));
        Assert.Equal(kalemler.Sum(x => x.KdvTutar), fatura.KdvTutar);
        Assert.Equal("KALEM", fatura.HasDegerKaynagi);
    }

    // ── 9. Servis içi sınır: 201 kalemlik fiş ────────────────────────────────

    [Fact]
    public async Task Ikiyuzbir_kalemli_fisten_fatura_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 201 kalem");
        var fisId = FisYaz(kurulum, k, Enumerable.Repeat(100m, 201).ToArray());

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem));
        Assert.Contains("200", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.FaturaKalemleri.Where(x => x.TenantId == k.TenantId));
    }
}
