using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-2 — HURDA ÇIKIŞI FİNANSAL SINIR TESTLERİ  [gerçek MySQL]
//
// QA 10: "bakiye yeterliyken 'bakiye yok' hatası". Sunucu kapısının sınırı
// ölçülür: 0 gram · eksi gram · bakiye = istenen (GEÇER) · bakiye + 0,0001
// (GEÇMEZ). Ölçülen bakiye: oturum şubesinde o madenin has bakiyesi
// (HurdaStokKapisi.Bakiye — ekranın okuduğu /hurda/bakiye ile TEK kaynak).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class Q102HurdaSinirTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    /// <summary>10 g K22 = 9,16 has girer.</summary>
    private static MSatis.HurdaAlisDto AlisDto(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private static MSatis.HurdaSatisDto SatisDto(Kiraci k, decimal has) => new()
    {
        Maden = "ALTIN", HasGrami = has, BirimFiyatTlHasGram = 2_900m,
        KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private int SatisSayisi(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Count(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");
    }

    [Fact]
    public async Task Bakiyeye_ESIT_cikis_GECER_ve_bakiye_sifira_iner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Q102-esit"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // 9,16 has

        await Satis(db).HurdaSatis(SatisDto(k, 9.16m), k.Islem);

        using var okuma = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(okuma, k.Islem, "ALTIN"));
        Assert.Equal(1, SatisSayisi(k));
    }

    [Fact]
    public async Task Bakiye_arti_0_0001_GECMEZ_ve_hicbir_sey_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Q102-asim"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var kasaOnce = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, 9.1601m), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);
        Assert.Contains("9.16", ex.Message.Replace(',', '.'));      // mevcut bakiyeyi SÖYLER

        Assert.Equal(0, SatisSayisi(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ── Q10-2 düzeltme turu: 6 haneli has (10,005 g × K22 = 9,16458) ────────
    // Canlı 5174'te ÖLÇÜLDÜ: bakiye 9,16458 iken 9,164581 çıkışı 200 aldı ve
    // has bakiyesi −0,000001'e düştü (kapı "bakiye + 0,000001" payı taşıyordu).

    private static MSatis.HurdaAlisDto AlisDto6(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10.005m,
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    [Fact]
    public async Task Alti_haneli_bakiye_arti_0_000001_GECMEZ_defter_eksiye_dusmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Q102-6hane-asim"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto6(k), k.Islem);
        Assert.Equal(9.16458m, await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, "ALTIN"));
        var kasaOnce = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, 9.164581m), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        Assert.Equal(0, SatisSayisi(k));
        using var okuma = vt.Olustur(k);
        Assert.Equal(9.16458m, await MHurda.HurdaStokKapisi.Bakiye(okuma, k.Islem, "ALTIN"));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task Alti_haneli_bakiyeye_ESIT_cikis_GECER_bakiye_tam_sifir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Q102-6hane-esit"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto6(k), k.Islem);

        await Satis(db).HurdaSatis(SatisDto(k, 9.16458m), k.Islem);

        using var okuma = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(okuma, k.Islem, "ALTIN"));
        Assert.Equal(1, SatisSayisi(k));
    }

    [Fact]
    public async Task Cikis_kapisi_alti_haneden_kucuk_pozitifi_sifir_sayar_reddeder()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Q102-6hane-mikro"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto6(k), k.Islem);
        // 0,0000004 deftere 6 haneyle 0 yazılırdı → kapı reddeder.
        await Assert.ThrowsAsync<BadRequestException>(
            () => MHurda.HurdaStokKapisi.DogrulaCikis(db, k.Islem, "ALTIN", 0.0000004m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Sifir_ve_eksi_gram_reddedilir_ve_bakiye_degismez(int has)
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"Q102-sinir{has}"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has), k.Islem));
        Assert.Contains("sıfırdan büyük", ex.Message);

        Assert.Equal(0, SatisSayisi(k));
        using var okuma = vt.Olustur(k);
        Assert.Equal(9.16m, await MHurda.HurdaStokKapisi.Bakiye(okuma, k.Islem, "ALTIN"));
    }

    /// <summary>
    /// Çıkış kapısı KENDİ BAŞINA da sıfır/eksiyi reddeder: servis ucundaki ön
    /// kontrol kaldırılsa bile (ya da kapı başka bir yoldan çağrılsa) eksi has
    /// "çıkış" olarak bakiyeyi ARTIRAMAZ. İki kapı birbirini maskelediği için
    /// kapı doğrudan sınanır (mutasyon M-Q102-9).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Cikis_kapisi_dogrudan_sifir_ve_eksiyi_reddeder(int has)
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"Q102-kapi{has}"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => MHurda.HurdaStokKapisi.DogrulaCikis(db, k.Islem, "ALTIN", has));
        Assert.Contains("sıfırdan büyük", ex.Message);
    }
}
