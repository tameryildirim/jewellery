using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ZO-BELGE 29.09 (hasım doğrulayıcı ORTA) — FİŞTEN FATURANIN TÜRÜ SUNUCUDA FİŞTEN
// TÜRETİLİR [DB + saf]
//
// Eskiden FistenOlustur `tur = dto.Tur ?? "SATIS"` idi: tür göndermeyen bir API
// istemcisi alış fişinden SATIŞ faturası kesiyor, satış fişine "ALIS" gönderen de
// kabul ediliyordu. Şimdi: bütün kalemler alış (ALIS/HURDA_ALIS) → ALIS; bütünü
// SATIS → SATIS; çelişen tür 400. Karışık fişte eski davranış korunur.
// (Test kurulumu defteri doğrudan yazar — yalnız test verisi; üretim yolu değildir.)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZoBelgeFistenTurTestleri(TestVeritabani vt)
{
    private static string FisYaz(KuyumcuDbContext kurulum, Kiraci k, params (string Tur, decimal Tl, decimal Has)[] satirlar)
    {
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        foreach (var (tur, tl, has) in satirlar)
            kurulum.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur, Tarih = now,
                TlTutari = tl, HasGrami = has, OdemeTuru = "NAKIT", NakitTutar = tl, VeresiyeTutar = 0m,
                FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        kurulum.SaveChanges();
        return fisId;
    }

    [Fact]
    public void ZBT1_FistenTurTuret_saf_kural()
    {
        Assert.Equal("ALIS", MFatura.FaturaServisi.FistenTurTuret(["ALIS", "HURDA_ALIS"]));
        Assert.Equal("ALIS", MFatura.FaturaServisi.FistenTurTuret(["HURDA_ALIS"]));
        Assert.Equal("SATIS", MFatura.FaturaServisi.FistenTurTuret(["SATIS", "SATIS"]));
        Assert.Null(MFatura.FaturaServisi.FistenTurTuret(["SATIS", "HURDA_ALIS"]));   // karışık
        Assert.Null(MFatura.FaturaServisi.FistenTurTuret(["SATIS_IADE"]));
        Assert.Null(MFatura.FaturaServisi.FistenTurTuret([]));
    }

    [Fact]
    public async Task ZBT2_Tur_gonderilmeyen_alis_fisi_ALIS_faturasi_olur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE fişten tür 1");
        var fisId = FisYaz(kurulum, k, ("ALIS", -1_500.50m, 2.5m), ("HURDA_ALIS", -600m, 1m));

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        Assert.Equal("ALIS", fatura.Tur);
        Assert.Equal(2_100.50m, fatura.Toplam);
    }

    [Fact]
    public async Task ZBT3_Alis_fisine_SATIS_turu_400_ve_fatura_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE fişten tür 2");
        var fisId = FisYaz(kurulum, k, ("ALIS", -1_000m, 2m));

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId, Tur = "SATIS", KdvOran = 0.20m }, k.Islem));
        Assert.Contains("alış fişidir", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Faturalar.Any(f => f.TenantId == k.TenantId));
    }

    [Fact]
    public async Task ZBT4_Satis_fisine_ALIS_turu_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE fişten tür 3");
        var fisId = FisYaz(kurulum, k, ("SATIS", 1_200m, -1m));

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).FistenOlustur(
                new MFatura.FaturaFistenDto { FisId = fisId, Tur = "ALIS", KdvOran = 0.20m }, k.Islem));
        Assert.Contains("satış fişidir", hata.Message);
    }

    [Fact]
    public async Task ZBT5_Karisik_fiste_eski_davranis_korunur_tur_yoksa_SATIS()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBELGE fişten tür 4");
        var fisId = FisYaz(kurulum, k, ("SATIS", 3_000m, -2m), ("HURDA_ALIS", -500m, 0.9m));

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal("SATIS", kontrol.Faturalar.Single(f => f.TenantId == k.TenantId).Tur);
    }
}
