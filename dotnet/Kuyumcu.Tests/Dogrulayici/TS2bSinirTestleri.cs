using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══ PAKET T-S2b — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet, 2026-09-25) ═══
// Kapsam: kalem işçiliği (FisProKalemKarti.TeklifIscilik · FisHesap.SatirIscilik/ToplamIscilik),
// has gösterimi (FisHesap.HasSayi) ve Türkçe giriş (FormAlani.SayiCoz). Beklenen değerler SABİT
// yazılır; üretim kodundan hesaplanmaz. Yuvarlama kuralı: MidpointRounding.AwayFromZero (Yuvarla2).
public sealed class TS2bSinirTestleri
{
    private static Teklif T(decimal? iscilik, decimal? indirim, decimal? satis, bool sabit = false) =>
        new(true, null, 10000m, 9500m, 0m, iscilik, indirim, satis, indirim is > 0 ? "kmp" : null, sabit, null);

    [Fact]
    public void Sifir_iscilik_indirimsiz_teklifte_0_doner_bilinmiyor_DEGIL() =>
        Assert.Equal(0m, FisProKalemKarti.TeklifIscilik(T(0m, 0m, 10000m), 10000m));

    [Fact]
    public void Kampanya_indirimi_1_kurus_bile_olsa_iscilik_bilinmez()
    {
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, 0.01m, 9999.99m), 9999.99m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, 1000m, 9000m), 9000m));
    }

    [Fact]
    public void Eksi_indirim_tutari_islemez_uydurma_yok_null()
    {
        // Sunucu negatif indirim döndürmez; dönerse (zam) işçilik ayrıştırılmış sayılmaz → null ("—").
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, -100m, 10100m), 10100m));
    }

    [Fact]
    public void Bos_null_teklif_ve_null_iscilik_null()
    {
        Assert.Null(FisProKalemKarti.TeklifIscilik(null, 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(null, null, 10000m), 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, null, 10000m), null));
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, null, 10000m, sabit: true), 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(new Teklif(false, "FIYAT_YOK", null, null, null, 500m, null, 10000m, null, false, null), 10000m));
    }

    [Fact]
    public void Elle_degisen_fiyatta_1_kurus_fark_bile_iscilik_null() =>
        Assert.Null(FisProKalemKarti.TeklifIscilik(T(500m, null, 10000m), 10000.01m));

    [Fact]
    public void Kurus_yuvarlamasi_0_005_sinirinda_yukari_AwayFromZero()
    {
        var k = new FisKalemi { UrunId = "a", UrunAdi = "A", Adet = 1m, BirimFiyat = 1m, IscilikTl = 0.005m };
        Assert.Equal(0.01m, FisHesap.SatirIscilik(k));
        var k2 = new FisKalemi { UrunId = "b", UrunAdi = "B", Adet = 3m, BirimFiyat = 1m, IscilikTl = 0.335m }; // 1,005
        Assert.Equal(1.01m, FisHesap.SatirIscilik(k2));
        var k3 = new FisKalemi { UrunId = "c", UrunAdi = "C", Adet = 1m, BirimFiyat = 1m, IscilikTl = 0.0049m };
        Assert.Equal(0.00m, FisHesap.SatirIscilik(k3));
    }

    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99_kaybolmaz_ve_tr_TR_basilir()
    {
        var k = new FisKalemi { UrunId = "a", UrunAdi = "A", Adet = 1m, BirimFiyat = 999_999_999.99m, IscilikTl = 999_999_999.99m };
        Assert.Equal(999_999_999.99m, FisHesap.SatirIscilik(k));
        Assert.Equal((999_999_999.99m, false), FisHesap.ToplamIscilik([k]));
        Assert.Equal("999.999.999,99\u00A0₺", Bicim.Tl(999_999_999.99m));   // tr-TR: sayı ile ₺ arasında bölünmez boşluk
    }

    [Fact]
    public void Fis_iskontosu_sifir_ve_eksi_toplami_bozmaz_pozitif_bilinmez()
    {
        var k = new FisKalemi { UrunId = "a", UrunAdi = "A", Adet = 2m, BirimFiyat = 100m, IscilikTl = 25m };
        Assert.Equal((50m, false), FisHesap.ToplamIscilik([k], 0m));
        Assert.Equal((50m, false), FisHesap.ToplamIscilik([k], -5m));
        Assert.Equal((0m, true), FisHesap.ToplamIscilik([k], 0.01m));
        Assert.Equal((0m, false), FisHesap.ToplamIscilik([], 5m));
    }

    [Fact]
    public void Has_4_hane_0_0001_kaybolmaz_0_00005_yukari_yuvarlanir()
    {
        Assert.Equal("0,0001", FisHesap.HasSayi(0.0001m));
        Assert.Equal("0,0001", FisHesap.HasSayi(0.00005m));
        Assert.Equal("0,00", FisHesap.HasSayi(0.00004m));
        Assert.Equal("1.234,5678", FisHesap.HasSayi(1234.5678m));
    }

    [Fact]
    public void Turkce_giris_45_000_ve_1_5_kabul_belirsiz_1_500_red()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}
