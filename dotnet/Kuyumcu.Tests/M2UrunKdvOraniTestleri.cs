using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M2 (müşteri görüşmesi 16.10: "KDV oranını düzenlediği bir şey olacak") —
// ürün kartındaki KDV oranı faturada KULLANILIR. Eskiden kart alanı vardı ama
// fatura onu hiç okumuyordu (gizli hata). Öncelik: istekteki oran > ürün oranı >
// yapılandırma varsayılanı (0,20).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class M2UrunKdvOraniTestleri(TestVeritabani vt)
{
    private static string FisYaz(KuyumcuDbContext kurulum, Kiraci k, decimal? urunKdvYuzde, params decimal[] tutarlar)
    {
        var urun = kurulum.Urunler.Single(u => u.Id == k.UrunId);
        urun.KdvOrani = urunKdvYuzde;
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        foreach (var tl in tutarlar)
            kurulum.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
                TlTutari = tl, HasGrami = -1m, OdemeTuru = "KARMA", NakitTutar = tl, VeresiyeTutar = 0m,
                FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        kurulum.SaveChanges();
        return fisId;
    }

    [Fact]
    public async Task Urun_kartindaki_yuzde_10_orani_fisten_faturada_kullanilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "M2 ürün oranı");
        var fisId = FisYaz(kurulum, k, 10m, 1_100m);

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        Assert.Equal(0.10m, kalem.KdvOran);
        Assert.Equal(100.00m, kalem.KdvTutar);       // 1.100 KDV dahil → 100,00 KDV
        Assert.Equal(0.10m, fatura.KdvOran);          // tek oran → başlık da o
        Assert.Equal(100.00m, fatura.KdvTutar);
    }

    [Fact]
    public async Task Istekte_acikca_verilen_oran_urun_oranini_ezer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "M2 istek oranı");
        var fisId = FisYaz(kurulum, k, 10m, 1_200m);

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0.20m, kalem.KdvOran);
        Assert.Equal(200.00m, kalem.KdvTutar);
    }

    [Fact]
    public async Task Urun_orani_bossa_varsayilan_yuzde_20_eski_davranis_aynen()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "M2 varsayılan");
        var fisId = FisYaz(kurulum, k, null, 1_200m);

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(new MFatura.FaturaFistenDto { FisId = fisId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0.20m, kalem.KdvOran);
        Assert.Equal(200.00m, kalem.KdvTutar);
    }
}
