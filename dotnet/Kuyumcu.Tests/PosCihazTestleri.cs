using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MPos = Kuyumcu.Api.Modules.Pos;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 — POS CİHAZI TANIMI [DB]
//
// Cihaz, komisyon oranının ve valör gününün TEK kaynağıdır. Yanlış kiracının
// şubesine bağlanan ya da açık alacağı varken kapatılan bir cihaz, POS alacak
// defterini sessizce yanlış yapar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PosCihazTestleri(TestVeritabani vt)
{
    private static MPos.PosServisi Servis(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db));

    private static MPos.PosCihazOlusturDto Dto(Kiraci k, string ad = "Garanti POS") => new()
    {
        Ad = ad, BankaAdi = "Garanti", SubeId = k.SubeId, KasaId = k.KasaId,
        PesinKomisyonOran = 0.0185m, ValorGun = 1,
    };

    [Fact]
    public async Task Cihaz_Olusturulur_VeListelenir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS cihaz 1");
        using var db = vt.Olustur(k);

        await Servis(db).CihazOlustur(Dto(k), k.Islem);

        var liste = await Servis(db).CihazListele(k.Islem, null, null);
        var c = Assert.Single(liste);
        Assert.Equal("Garanti POS", c.Ad);
        Assert.Equal(0.0185m, c.PesinKomisyonOran);
        Assert.Equal(1, c.ValorGun);
        Assert.Equal("TRY", c.ParaBirimi);   // PAKET-15: POS yalnız TRY
        Assert.True(c.Aktif);
    }

    [Fact]
    public async Task Cihaz_BaskaKiracininSubesiyle_Reddedilir()
    {
        // Kiracı sertliği (ADR-0010): global süzgeç TEK BAŞINA dayanak değildir.
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS kiracı B");
        using var db = vt.Olustur(a);

        var dto = Dto(a);
        dto.SubeId = b.SubeId;   // YABANCI şube

        await Assert.ThrowsAnyAsync<ApiException>(() => Servis(db).CihazOlustur(dto, a.Islem));
    }

    [Fact]
    public async Task Cihaz_KasaParaBirimiUyusmazsa_Reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS döviz kasa");
        var usdKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "USD");
        using var db = vt.Olustur(k);

        var dto = Dto(k);
        dto.KasaId = usdKasa;

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).CihazOlustur(dto, k.Islem));
        Assert.Contains("TRY", hata.Message);
    }

    [Fact]
    public async Task Cihaz_AyniAd_Conflict()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS aynı ad");
        using var db = vt.Olustur(k);

        await Servis(db).CihazOlustur(Dto(k), k.Islem);
        await Assert.ThrowsAsync<ConflictException>(() => Servis(db).CihazOlustur(Dto(k), k.Islem));
    }

    [Fact]
    public async Task Cihaz_AcikAlacakVarken_PasifeAlinamaz()
    {
        // Fail-closed: açık POS alacağı olan cihaz kapatılırsa alacak kaybolur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS açık alacak");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).CihazOlustur(Dto(k), k.Islem);
        var cihazId = PosYardim.CihazId(cevap);

        PosYardim.TahsilatYaz(vt, k, cihazId, 10_000m);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).CihazPasifeAl(cihazId, k.Islem));
        Assert.Contains("pasife alınamaz", hata.Message);
    }

    [Fact]
    public async Task Cihaz_OranDegisikligi_GecmisTahsilatiDegistirmez()
    {
        // Oran satıra DONDURULUR (snapshot) — kur_snapshot mantığının aynısı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS oran snapshot");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).CihazOlustur(Dto(k), k.Islem);
        var cihazId = PosYardim.CihazId(cevap);
        PosYardim.TahsilatYaz(vt, k, cihazId, 10_000m);

        await Servis(db).CihazGuncelle(cihazId,
            new MPos.PosCihazGuncelleDto { PesinKomisyonOran = 0.05m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var satir = kontrol.PosTahsilatlari.AsNoTracking()
            .Single(t => t.TenantId == k.TenantId && t.PosCihazId == cihazId);
        Assert.Equal(0.0185m, satir.KomisyonOran);
        Assert.Equal(185.00m, satir.KomisyonTutar);
    }

    [Fact]
    public async Task CihazPatch_GonderilmeyenAlanlari_KORUR()
    {
        // ⚠ Kurul engel 4: tam-gövde PATCH kalem verisini sessizce imha ediyordu.
        // Yalnız "ad" gönderilen istek sonrası oran / valör / kasa / banka hesabı
        // AYNEN KALMALI.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS patch koruma");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).CihazOlustur(Dto(k), k.Islem);
        var cihazId = PosYardim.CihazId(cevap);

        await Servis(db).CihazGuncelle(cihazId,
            new MPos.PosCihazGuncelleDto { Ad = "Garanti POS 2" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var c = kontrol.PosCihazlari.AsNoTracking().Single(x => x.Id == cihazId);
        Assert.Equal("Garanti POS 2", c.Ad);
        Assert.Equal(0.0185m, c.PesinKomisyonOran);
        Assert.Equal(1, c.ValorGun);
        Assert.Equal(k.KasaId, c.KasaId);
        Assert.Null(c.BankaHesapId);
    }

    [Fact]
    public async Task Cihaz_Oran_YuzdeYuzuAsarsa_Reddedilir()
    {
        // decimal(5,4) sessizce KIRPMAZ: Range servis içinde de tekrarlanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS oran sınırı");
        using var db = vt.Olustur(k);

        var dto = Dto(k);
        dto.PesinKomisyonOran = 1.5m;

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).CihazOlustur(dto, k.Islem));
    }

    [Fact]
    public async Task Cihaz_PasifeAlinir_VeAyniUctanGeriAcilir()
    {
        // UX-S25: oluşturulan her kayıt düzeltilebilir ya da neden düzeltilemediği yazar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "POS pasif/aktif");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).CihazOlustur(Dto(k), k.Islem);
        var cihazId = PosYardim.CihazId(cevap);

        await Servis(db).CihazPasifeAl(cihazId, k.Islem);
        Assert.False(vt.OlusturSistem().PosCihazlari.AsNoTracking().Single(x => x.Id == cihazId).Aktif);

        await Servis(db).CihazGuncelle(cihazId, new MPos.PosCihazGuncelleDto { Aktif = true }, k.Islem);
        Assert.True(vt.OlusturSistem().PosCihazlari.AsNoTracking().Single(x => x.Id == cihazId).Aktif);
    }
}
