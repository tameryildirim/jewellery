using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fatura;
using Kuyumcu.Api.Modules.Fiyat;
using Kuyumcu.Api.Modules.Urun;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M1b (müşteri 16.10: "925'den maliyetim varsa 925 yazacağım", "bileziği kârsız
// satıyoruz") — alış (maliyet) milyemi: maliyet = net gram × alış milyemi × has;
// satış milyemi boşsa satış alış milyemiyle.
// M2b (müşteri 16.10: "150 bin liralık fatura… binde 1'i KDV'ye giren kısım, geri
// kalanı %0… %20 dediğimde 30 TL") — ürünün KDV'ye giren payı (binde).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class M1bM2bMotorTestleri
{
    private static Urun Bilezik(decimal? alis = null, decimal? satis = null) => new()
    {
        Id = "u1", TenantId = "t", Tip = "ZIYNET", Kod = "BLZ", Ad = "Burma Bilezik", AyarId = "K22",
        Milyem = 916m, GramAgirlik = 15.2m, HasGrami = 15.2m * 0.916m, AlisMilyemi = alis, SatisMilyemi = satis,
    };
    private static FiyatListesi Perakende() => new()
    {
        Id = "l1", TenantId = "t", Ad = "Perakende", HasMarjYuzde = 4m, IscilikCarpanYuzde = 100m, Varsayilan = true,
    };

    [Fact]
    public void Satis_milyemi_bossa_alis_milyemiyle_KARSIZ_satilir_maliyet_satisa_esit()
    {
        // 15,2 × 0,925 × 6.600 = 92.796,00 — müşteri: "10 gram bilezik sattığımda fiyat bu, bu maliyet"
        var t = FiyatMotoru.Hesapla(Bilezik(alis: 925m), 6_600m, Perakende(), null)!;
        Assert.Equal(92_796.00m, t.ListeFiyati);
        Assert.Equal(92_796.00m, t.MaliyetTl);
        Assert.Equal(0m, t.MarjTl);
        Assert.Equal(925m, t.SatisMilyemi);
    }

    [Fact]
    public void Satis_milyemi_doluysa_kar_satis_eksi_alis_milyemi_maliyetidir_has_degeri_DEGIL()
    {
        var t = FiyatMotoru.Hesapla(Bilezik(alis: 925m, satis: 950m), 6_600m, Perakende(), null)!;
        Assert.Equal(95_304.00m, t.ListeFiyati);          // 15,2 × 0,950 × 6.600
        Assert.Equal(92_796.00m, t.MaliyetTl);            // 15,2 × 0,925 × 6.600
        Assert.Equal(2_508.00m, t.ListeFiyati - t.MaliyetTl!.Value);   // gerçek kâr
        Assert.Equal(91_893.12m, t.HasDegeri);            // has değeri yalnız KDV/bilgi içindir
    }

    [Fact]
    public void Alis_milyemi_yoksa_maliyet_bilinmez_null()
    {
        var t = FiyatMotoru.Hesapla(Bilezik(satis: 950m), 6_600m, Perakende(), null)!;
        Assert.Null(t.MaliyetTl);
    }

    [Fact]
    public void Binde_1_ile_150_bin_faturada_matrah_150_KDV_30_kalan_sifir_oranli()
    {
        var s = OzelMatrahMotoru.HesaplaBinde([], 150_000m, 1m, 0.20m);
        Assert.Equal(150.00m, s.Matrah);
        Assert.Equal(30.00m, s.KdvTutar);
        Assert.Equal(149_820.00m, s.HasDeger);            // %0'a giren kısım
        Assert.Equal(149_970.00m, s.AraToplam);           // toplam − KDV
    }

    [Fact]
    public void Binde_yonteminde_satir_toplami_asilamaz()
    {
        var s = OzelMatrahMotoru.HesaplaBinde([], 1_200m, 1000m, 0.20m);
        Assert.Equal(1_000.00m, s.Matrah);
        Assert.Equal(200.00m, s.KdvTutar);
        Assert.Equal(0m, s.HasDeger);
    }

    [Fact]
    public void Binde_bossa_tek_giris_noktasi_yasal_hesaba_duser()
    {
        var bacak = new List<HasBacagi> { new(10m, 6_600m) };
        var yasal = OzelMatrahMotoru.Hesapla(bacak, 70_000m, 0.20m);
        var tek = OzelMatrahMotoru.Hesapla(bacak, 70_000m, 0.20m, null);
        Assert.Equal(yasal, tek);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000.001)]
    [InlineData(1.0005)]
    public void Kapi_gecersiz_binde_payini_reddeder(decimal binde)
    {
        var ex = Assert.Throws<BadRequestException>(() => UrunServisi.SatisMilyemiKapisi(null, null, "K22", null, binde));
        Assert.Equal("kdvMatrahBinde", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void Kapi_gumuste_alis_milyemini_reddeder()
    {
        var ex = Assert.Throws<BadRequestException>(() => UrunServisi.SatisMilyemiKapisi(null, null, "G925", 925m));
        Assert.Equal("alisMilyemi", AlanIsareti.Alan(ex));
    }
}

[Collection("veritabani")]
public sealed class M2bFaturaBindeTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Urunun_binde_payi_varsa_kuyum_faturasinda_musterinin_yontemi_uygulanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "M2b binde");
        kurulum.Urunler.Single(u => u.Id == k.UrunId).KdvMatrahBinde = 1m;
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
            TlTutari = 150_000m, HasGrami = -20m, OdemeTuru = "KARMA", NakitTutar = 150_000m, VeresiyeTutar = 0m,
            FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
            KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m, KdvTuru = "KUYUM" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        Assert.Equal(150.00m, kalem.KdvMatrah);
        Assert.Equal(30.00m, kalem.KdvTutar);
        Assert.Equal(150_000.00m, kalem.SatirToplam);      // toplam DEĞİŞMEZ
        Assert.Equal(30.00m, fatura.KdvTutar);
        Assert.Equal(150_000.00m, fatura.Toplam);
    }

    [Fact]
    public async Task Binde_payi_STANDART_KDV_turunde_uygulanmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "M2b standart");
        kurulum.Urunler.Single(u => u.Id == k.UrunId).KdvMatrahBinde = 1m;
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
            TlTutari = 1_200m, HasGrami = -1m, OdemeTuru = "KARMA", NakitTutar = 1_200m, VeresiyeTutar = 0m,
            FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
            KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(200.00m, kalem.KdvTutar);
    }
}
