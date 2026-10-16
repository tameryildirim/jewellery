using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET ISLEM-BANT-2 finansal sınır testleri.
// Paket işlem içi has gram fiyatının EKRAN tutar gösterimini (IslemBantGosterim.TutarGosterilir /
// FiyatHesabaGirer) ve Kaydet kilidini (KaydetKilidi) değiştirdi. Beklenen değerler SABİT yazılır:
//   mutlak sınır 500,00 – 50.000,00 (onayla aşılamaz) · K22 milyem 916 · has 6 hane AwayFromZero ·
//   TL 2 hane AwayFromZero (HurdaGirisi.HesaplananTutar).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ISLEMBANT2SinirTestleri
{
    private const string Gerekce = "Bozuk hurda, ayarı düşük çıktı; ustayla teyit ettim.";

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    [InlineData(-5.0)]
    public void D01_sifir_ve_eksi_fiyat_tutara_girmez_ve_tutar_gosterilmez(double f)
    {
        var fiyat = (decimal)f;
        Assert.False(IslemBantGosterim.FiyatHesabaGirerAyar(fiyat, "K22"));
        Assert.False(IslemBantGosterim.TutarGosterilirAyar(fiyat, "K22"));
        Assert.NotNull(TutarKurali.Denetle(fiyat, TutarTuru.Pozitif));    // ortak Para Tur=Pozitif reddeder
        var h = new HurdaGirisi { Ayar = "K22", Gram = 10m };
        h.FiyatGirildi(fiyat);
        Assert.Null(h.HesaplananTutar);                                    // eksi tutar ekrana çıkamaz
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 5m, YeniUrunId = "u1", YeniFiyat = 10_000m };
        t.FiyatGirildi(fiyat);
        Assert.False(t.EskiFiyatHesabaGirer);
        Assert.Null(t.Fark);
    }

    [Theory]
    [InlineData(0.01, false)]
    [InlineData(499.99, false)]
    [InlineData(500.00, true)]
    [InlineData(50_000.00, true)]
    [InlineData(50_000.01, false)]
    [InlineData(999_999_999.99, false)]
    public void D02_mutlak_sinir_uclari_kutu_alti_tutar(double f, bool gorunur)
    {
        Assert.Equal(gorunur, IslemBantGosterim.TutarGosterilirAyar((decimal)f, "K22"));
        Assert.True(IslemBantGosterim.FiyatHesabaGirerAyar((decimal)f, "K22"));   // pozitif: model hesabına girer
    }

    [Fact]
    public void D03_cok_buyuk_tutar_onayla_bile_kaydedilemez_mutlak_cumlesi()
    {
        var h = FiyatBandi.IslemDenetle(999_999_999.99m, IslemFiyatBaglami.HurdaAlis, 6_827.20m, Ayarlar.MadenBul("K22"));
        Assert.NotNull(h);
        Assert.Equal(GirdiKodu.FiyatMutlakDisi, h!.Value.Kod);
        var kilit = IslemBantGosterim.KaydetKilidi(h, new IslemBantOnayDurumu { Onay = true, Gerekce = Gerekce }, "SAHIP", "tr");
        Assert.Equal(GirdiSozlugu.Mesaj(h.Value, "tr"), kilit);
        Assert.Contains("500,00", kilit);
        Assert.Contains("50.000,00", kilit);
    }

    [Fact]
    public void D04_kurus_yuvarlamasi_AwayFromZero_0005_siniri()
    {
        // K22 1 g → 0,916 has; 0,916 × 5.001,25 = 4.581,145 → AwayFromZero 4.581,15 (ToEven 4.581,14 olurdu).
        var h = new HurdaGirisi { Ayar = "K22", Gram = 1m };
        h.FiyatGirildi(5_001.25m);
        Assert.Equal(0.916m, h.HasKarsiligi);
        Assert.Equal(4_581.15m, h.HesaplananTutar);
        Assert.True(IslemBantGosterim.TutarGosterilirAyar(5_001.25m, "K22"));
    }

    [Fact]
    public void D05_has_gram_kucuk_deger_0_0001()
    {
        // 0,0001 g × 916/1000 = 0,0000916 → 6 hane AwayFromZero 0,000092; × 6.000 = 0,552 → 0,55.
        var h = new HurdaGirisi { Ayar = "K22", Gram = 0.0001m };
        h.FiyatGirildi(6_000m);
        Assert.Equal(0.000092m, h.HasKarsiligi);
        Assert.Equal(0.55m, h.HesaplananTutar);
    }

    [Fact]
    public void D06_bos_null_fiyat()
    {
        Assert.False(IslemBantGosterim.FiyatHesabaGirerAyar(null, "K22"));
        Assert.False(IslemBantGosterim.TutarGosterilirAyar(null, "K22"));
        Assert.Null(IslemBantGosterim.KaydetKilidi(null, new IslemBantOnayDurumu(), "TEZGAHTAR", "tr"));
        var h = new HurdaGirisi { Ayar = "K22", Gram = 10m };
        h.FiyatGirildi(null);
        Assert.Null(h.HesaplananTutar);
        Assert.False(IslemBantGosterim.HurdaAlisKilitli(null));
        Assert.False(IslemBantGosterim.HurdaAlisKilitli(""));
    }

    [Theory]
    [InlineData("45.000", FormAlani.SayiSonucu.Tamam, 45000.0)]
    [InlineData("1,5", FormAlani.SayiSonucu.Tamam, 1.5)]
    [InlineData("6.827,20", FormAlani.SayiSonucu.Tamam, 6827.2)]
    [InlineData("1.500", FormAlani.SayiSonucu.Belirsiz, null)]
    [InlineData("abc", FormAlani.SayiSonucu.Gecersiz, null)]
    public void D07_Turkce_giris_SayiCoz(string ham, FormAlani.SayiSonucu sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        if (deger is { } d) Assert.Equal((decimal)d, c.Deger);
    }

    [Theory]
    [InlineData("sahip")]       // küçük harf: sunucu rolü büyük harf — kilit fail-closed
    [InlineData("TEZGAHTAR")]
    [InlineData(" ")]
    public void D08_hurda_alis_rol_kumesi_disi_kilitli(string rol) =>
        Assert.True(IslemBantGosterim.HurdaAlisKilitli(rol));

    [Fact]
    public void D09_oran_disi_fiyat_MUHASEBE_onaylayamaz_SAHIP_gerekceyle_acar()
    {
        var h = FiyatBandi.IslemDenetle(2_000m, IslemFiyatBaglami.HurdaAlis, 6_827.20m, Ayarlar.MadenBul("K22"));
        Assert.NotNull(h);
        Assert.Equal(GirdiKodu.IslemFiyatBantDisi, h!.Value.Kod);
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok,
            IslemBantGosterim.KaydetKilidi(h, new IslemBantOnayDurumu { Onay = true, Gerekce = Gerekce }, "MUHASEBE", "tr"));
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.GerekceKisa, 20m, 19m), "tr"),
            IslemBantGosterim.KaydetKilidi(h, new IslemBantOnayDurumu { Onay = true, Gerekce = new string('a', 19) }, "SAHIP", "tr"));
        Assert.Null(IslemBantGosterim.KaydetKilidi(h, new IslemBantOnayDurumu { Onay = true, Gerekce = new string('a', 20) }, "SAHIP", "tr"));
    }
}
