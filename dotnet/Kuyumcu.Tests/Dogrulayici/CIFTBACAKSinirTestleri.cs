using Kuyumcu.Api.Common;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET CIFT-BACAK finansal sınır testleri.
// Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// K22 milyem 916 · 10 g → 9,16 has · 20 g → 18,32 has · G925 20 g → 18,5 has.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CIFTBACAKSinirTestleri(TestVeritabani vt)
{
    private const string Gerekce = "CIFT-BACAK dogrulayici sinir testi — yeterince uzun gerekce";

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"PAKETCIFT-BACAK-TEST dg {ad}"), 2_000m);
    }

    private async Task<string> EmanetGiris(Kiraci k, string ayarId, decimal gram)
    {
        using (var db = vt.Olustur(k))
            await new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance).Giris(
                new MEmanet.EmanetGirisDto
                {
                    CariId = k.CariId, AyarId = ayarId, GramAgirlik = gram, KurSnapshotId = k.KurId,
                    Aciklama = "dg emanet",
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
            await new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance).Cikis(k.CariId,
                new MEmanet.EmanetCikisDto { HasGrami = has, Maden = maden, KurSnapshotId = k.KurId, Aciklama = "dg iade" },
                k.Islem);
        using var oku = vt.OlusturSistem();
        return await oku.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_CIKIS" && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .Select(h => h.Id).FirstAsync();
    }

    private async Task StornoEt(Kiraci k, string id)
    {
        using var db = vt.Olustur(k);
        await new MSatis.StornoServisi(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance)
            .Storno(id, new MSatis.StornoDto { Gerekce = Gerekce, KurSnapshotId = k.KurId }, k.Islem);
    }

    private decimal EmanetNet(Kiraci k, string maden)
    {
        using var db = vt.OlusturSistem();
        return db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.CariId == k.CariId
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == maden)
            .Sum(h => h.HasGrami);
    }

    private async Task Takas(Kiraci k, decimal eskiGram, decimal adet, decimal birimFiyat = 20_000m)
    {
        using var db = vt.Olustur(k);
        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance,
                hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false })
            .Takas(new MSatis.TakasDto
            {
                Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
                CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = eskiGram,
                EskiUrunBirimFiyatTlHasGram = 1_000m,
                YeniUrunId = k.UrunId, YeniUrunAdet = adet, YeniUrunBirimFiyatTl = birimFiyat,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
    }

    // ── Saf kural sınırları ────────────────────────────────────────────────

    [Fact]
    public void Sinir_sifir_eksi_null_buyuk_ve_4hane()
    {
        Assert.Equal(0m, MEmanet.EmanetKurali.CariHasEtkisi("ALTIN", 0m));                // sıfır
        Assert.Equal(5m, MEmanet.EmanetKurali.CariHasEtkisi("ALTIN", -5m));               // eksi → işaret doğru
        Assert.Equal(0m, MEmanet.EmanetKurali.CariHasEtkisi(null, 5m));                    // null maden
        Assert.Equal(0m, MEmanet.EmanetKurali.CariHasEtkisi("", 5m));                      // boş maden
        Assert.Equal(0m, MEmanet.EmanetKurali.CariHasEtkisi("GUMUS", 999_999_999.99m));   // büyük, altın dışı
        Assert.Equal(-999_999_999.99m, MEmanet.EmanetKurali.CariHasEtkisi("ALTIN", 999_999_999.99m));
        Assert.Equal(-0.0001m, MEmanet.EmanetKurali.CariHasEtkisi("ALTIN", 0.0001m));      // 4 hane
        // ayna ile yazan kod her uçta aynı sonucu verir
        Assert.Equal(-0.0001m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 0.0001m, "ALTIN", false));
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_CIKIS", -0.0001m, "PLATIN", true));
    }

    [Fact]
    public void Sinir_turkce_sayi_girisi()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
    }

    // ── HATA-2 sınırları: elde kalan kapısı ────────────────────────────────

    [Fact]
    public async Task Sinir_elde_kalan_4hane_eksik_iade_bile_giris_stornosunu_engeller()
    {
        var k = YeniKiraci("4hane");
        var giris = await EmanetGiris(k, "K22", 10m);          // 9,16
        await EmanetCikis(k, Maden.Altin, 0.0001m);             // elde 9,1599 < 9,16
        await Assert.ThrowsAsync<ConflictException>(() => StornoEt(k, giris));
        Assert.Equal(9.1599m, EmanetNet(k, Maden.Altin));
    }

    [Fact]
    public async Task Sinir_elde_kalan_esitlik_iki_giris_bir_iade_diger_giris_stornolanabilir_defter_eksiye_dusmez()
    {
        var k = YeniKiraci("esitlik");
        var g1 = await EmanetGiris(k, "K22", 10m);             // 9,16
        await EmanetGiris(k, "K22", 10m);                       // 9,16
        await EmanetCikis(k, Maden.Altin, 9.16m);               // elde 9,16 (= g1)
        await StornoEt(k, g1);                                  // elde 9,16 ≥ 9,16 → 200
        Assert.Equal(0m, EmanetNet(k, Maden.Altin));
        using var db = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(db, k.CariId).Has);
    }

    [Fact]
    public async Task Sinir_emanet_cikis_stornosu_kapiya_takilmaz_ve_cift_storno_409()
    {
        var k = YeniKiraci("cikis storno");
        await EmanetGiris(k, "G925", 20m);                      // 18,5
        var c = await EmanetCikis(k, Maden.Gumus, 18.5m);
        await StornoEt(k, c);
        Assert.Equal(18.5m, EmanetNet(k, Maden.Gumus));
        await Assert.ThrowsAsync<ConflictException>(() => StornoEt(k, c));
        Assert.Equal(18.5m, EmanetNet(k, Maden.Gumus));
        using var db = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(db, k.CariId).Has);   // gümüş hiçbir adımda altın kolonuna girmez
    }

    // ── HATA-3 sınırları: takasın eski altın bacağı ─────────────────────────

    [Fact]
    public async Task Sinir_takas_adet2_ve_eski_yeniden_kucuk_net_negatif()
    {
        // 5 g K22 eski (4,58 has) + 2 adet yeni (2 × 9,16 = 18,32) → net −13,74; hurdaya +4,58.
        var k = YeniKiraci("takas adet2");
        await Takas(k, 5m, 2m, 5_000m);   // nakit fark 5.420 — tevsik haddi altında
        using var db = vt.Olustur(k);
        Assert.Equal(4.58m, await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, Maden.Altin));
    }

    [Fact]
    public async Task Sinir_takas_sonrasi_urun_gram_duzeltmesi_gecmis_hurda_bakiyesini_degistirmez()
    {
        // 20 g K22 eski (18,32 has) alındı. Sonradan verilen ürünün kartında gram 10 → 12 düzeltildi.
        // Dükkâna fiziken giren hurda DEĞİŞMEDİ: 18,32 kalmalı.
        var k = YeniKiraci("takas urun duzelt");
        await Takas(k, 20m, 1m);
        using (var db = vt.Olustur(k))
            Assert.Equal(18.32m, await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, Maden.Altin));

        using (var db = vt.Olustur(k))
            await new MUrun.UrunServisi(db, TestKurulum.Denetim(db))
                .Guncelle(k.UrunId, new MUrun.UrunGuncelleDto { GramAgirlik = 12m }, k.Islem);

        using var oku = vt.Olustur(k);
        Assert.Equal(18.32m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
    }

    [Fact]
    public async Task Sinir_takas_stornosu_409_hurda_degismez()
    {
        var k = YeniKiraci("takas storno");
        await Takas(k, 20m, 1m);
        string id;
        using (var s = vt.OlusturSistem())
            id = s.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "TAKAS").Id;
        await Assert.ThrowsAnyAsync<ApiException>(() => StornoEt(k, id));
        using var db = vt.Olustur(k);
        Assert.Equal(18.32m, await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, Maden.Altin));
    }
    // ── Tur 3 (yeniden deneme) ek sınırlar ─────────────────────────────────

    [Fact]
    public async Task Sinir_takas_kurus_yuvarlama_0005_yukari_ve_eski_has_6hane()
    {
        // 20 g K22 → 18,32 has × 1.000 = 18.320 TL; yeni 20.000,005 → fark 1.680,005 → AwayFromZero 1.680,01.
        var k = YeniKiraci("takas kurus");
        await Takas(k, 20m, 1m, 20_000.005m);
        using var s = vt.OlusturSistem();
        var h = s.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "TAKAS");
        Assert.Equal(1_680.01m, h.TlTutari);
        Assert.Equal(18.32m, h.TakasEskiHasGrami);
        Assert.Equal(9.16m, h.HasGrami);          // net 18,32 − 9,16
    }

    [Fact]
    public async Task Sinir_gumus_giris_stornosu_altin_kolonuna_sifir_yazar_emanet_sifirlanir()
    {
        var k = YeniKiraci("gumus storno");
        var g = await EmanetGiris(k, "G925", 50m);              // 46,25 has gümüş
        await StornoEt(k, g);
        Assert.Equal(0m, EmanetNet(k, Maden.Gumus));
        using var db = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(db, k.CariId).Has);   // senaryo 5.5: eskiden +46,25
    }

    [Fact]
    public async Task Sinir_altin_iade_edilmis_giris_stornosu_409_defter_ve_kolon_degismez()
    {
        var k = YeniKiraci("altin iade");
        var g = await EmanetGiris(k, "K22", 10m);                // 9,16
        await EmanetCikis(k, Maden.Altin, 9.16m);
        await Assert.ThrowsAsync<ConflictException>(() => StornoEt(k, g));
        Assert.Equal(0m, EmanetNet(k, Maden.Altin));             // eskiden −9,16
        using var db = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(db, k.CariId).Has);   // eskiden +9,16
    }
}
