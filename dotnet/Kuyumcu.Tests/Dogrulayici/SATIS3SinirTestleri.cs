using Bunit;
using Kuyumcu.Web.Bilesenler;
using Microsoft.Extensions.DependencyInjection;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

#pragma warning disable CS0618 // GOC-C-2 tur 2: FisFark.SifirSatirlariDusur [Obsolete] — ekrandan çağrılmaz; burada yalnız eski saf kural sözleşmesi sınanır

// ════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metin/sayıdır
// (üretim kodundan hesaplanmaz). SATIS-3'ün para hesaplayan yeni yolu:
// FisHesap.OtoDengele (karma ödemede "otomatik kalan") + FisFark.Durum
// (Eksik/Fazla/Tamam metni) + TL girdisinin ayrıştırılması (FormAlani.SayiCoz).
// Yuvarlama kuralı projede FisHesap.Yuvarla2 = AwayFromZero 2 hane.
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleri
{
    private static FisBacagi B(string arac, decimal tl, bool elle = false) =>
        new() { Arac = arac, TutarTl = tl, ElleYazildi = elle };

    [Fact]
    public void Sifir_toplam_iki_bos_bacak_DEGISMEZ()
    {
        var l = new List<FisBacagi> { B("NAKIT", 0m), B("KART", 0m) };
        Assert.False(FisHesap.OtoDengele(l, 0m));
        Assert.Equal(0m, l[0].TutarTl);
        Assert.Equal(0m, l[1].TutarTl);
        Assert.Equal(FarkDurumu.Tamam, FisFark.Durum(0m));
    }

    [Fact]
    public void Eksi_elle_fazla_yazilinca_hedef_EKSIYE_DUSMEZ_fark_FAZLA()
    {
        var nakit = B("NAKIT", 400m);
        var kart = B("KART", 1_500m, elle: true);
        var l = new List<FisBacagi> { nakit, kart };
        Assert.True(FisHesap.OtoDengele(l, 1_000m));
        Assert.Equal(0m, nakit.TutarTl);                     // −500 değil
        var fark = FisFark.Hesapla(1_000m, l);
        Assert.Equal(-500m, fark);
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(fark));
        Assert.False(FisFark.KaydetAcik(fark));
    }

    [Fact]
    public void Kurus_yuvarlama_AwayFromZero_0005_siniri_ve_kalan_KURUS_KAYBETMEZ()
    {
        Assert.Equal(0.01m, FisHesap.Yuvarla2(0.005m));
        Assert.Equal(-0.01m, FisHesap.Yuvarla2(-0.005m));
        Assert.Equal(0.00m, FisHesap.Yuvarla2(0.0049m));
        var kart = B("KART", 33.33m, elle: true);
        var nakit = B("NAKIT", 0m);
        var l = new List<FisBacagi> { nakit, kart };
        Assert.True(FisHesap.OtoDengele(l, 100.01m));
        Assert.Equal(66.68m, nakit.TutarTl);
        Assert.Equal(0m, FisFark.Hesapla(100.01m, l));
    }

    [Fact]
    public void Has_gram_4_hane_0_0001_fark_TAMAM_SAYILMAZ()
    {
        Assert.Equal(FarkDurumu.Eksik, FisFark.Durum(0.0001m));
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(-0.0001m));
        Assert.False(FisFark.KaydetAcik(0.0001m));
    }

    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99_kalan_dogru_ve_TR_bicimli()
    {
        var kart = B("KART", 0.01m, elle: true);
        var havale = B("HAVALE", 0m);
        var l = new List<FisBacagi> { kart, havale };
        Assert.True(FisHesap.OtoDengele(l, 999_999_999.99m));
        Assert.Equal(999_999_999.98m, havale.TutarTl);
        Assert.Equal("999.999.999,99 ₺", Bicim.Tl(999_999_999.99m).Replace(' ', ' '));
        Assert.DoesNotContain("TRY", Bicim.Tl(999_999_999.99m));
    }

    [Fact]
    public void Altin_ve_doviz_HEDEF_OLAMAZ_elle_TL_varsa_kalan_ATANMAZ()
    {
        var kart = B("KART", 3_000m, elle: true);
        var altin = B(FisAltinBacagi.Arac, 2_000m);
        var doviz = B("DOVIZ", 1_000m);
        var l = new List<FisBacagi> { kart, altin, doviz };
        Assert.False(FisHesap.OtoDengele(l, 10_000m));
        Assert.Equal(2_000m, altin.TutarTl);
        Assert.Equal(1_000m, doviz.TutarTl);
        Assert.Equal(4_000m, FisFark.Hesapla(10_000m, l));
    }

    [Fact]
    public void Tek_bacak_ve_bos_liste_DOKUNULMAZ_ikinci_cagri_IDEMPOTENT()
    {
        Assert.False(FisHesap.OtoDengele(new List<FisBacagi>(), 5m));
        var tek = B("NAKIT", 1m);
        Assert.False(FisHesap.OtoDengele(new List<FisBacagi> { tek }, 5m));
        Assert.Equal(1m, tek.TutarTl);
        var l = new List<FisBacagi> { B("NAKIT", 0m, elle: true), B("KART", 0m) };
        Assert.True(FisHesap.OtoDengele(l, 250m));
        Assert.False(FisHesap.OtoDengele(l, 250m));
        Assert.Equal(250m, l[1].TutarTl);
    }

    [Fact]
    public void Son_ELLE_YAZILMAYAN_TL_bacagi_hedef_ilk_degil()
    {
        var a = B("NAKIT", 0m);
        var b = B("HAVALE", 0m);
        var c = B("KART", 600m, elle: true);
        var l = new List<FisBacagi> { a, b, c };
        Assert.True(FisHesap.OtoDengele(l, 1_000m));
        Assert.Equal(0m, a.TutarTl);
        Assert.Equal(400m, b.TutarTl);
    }

    [Fact]
    public void Bos_null_ve_Turkce_giris_SayiCoz()
    {
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("   ").Sonuc);
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(-1_500.5m, FormAlani.SayiCoz("-1.500,50").Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (28.09 öğleden sonra) — PAKET SATIS-3 finansal sınır testleri (kalıcı).
// Kapsam: SATIS-3'ün eklediği OTOMATİK KALAN (FisHesap.OtoDengele — kart/nakit bacağı
// fişin kalanını üstlenir) + fiş kutularının geçtiği tr-TR sayı çözümü (FormAlani.SayiCoz)
// + ekrandaki para biçimi (Bicim.Tl, MidpointRounding.AwayFromZero).
// Beklenen değerler SABİT yazılmıştır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur2
{
    private static (FisBacagi kart, FisBacagi altin, List<FisBacagi> l) IkiBacak(decimal kartTl, decimal altinTl)
    {
        var kart = new FisBacagi { Arac = "KART", TutarTl = kartTl };
        var altin = new FisBacagi { Arac = FisAltinBacagi.Arac, TutarTl = altinTl };
        return (kart, altin, new List<FisBacagi> { kart, altin });
    }

    [Fact]
    public void Sinir_sifir_toplam_hedef_bacak_sifira_iner_eksiye_dusmez()
    {
        var (kart, altin, l) = IkiBacak(5_000m, 2_500m);
        Assert.True(FisHesap.OtoDengele(l, 0m));
        Assert.Equal(0m, kart.TutarTl);
        Assert.Equal(2_500m, altin.TutarTl);   // altın türetilir, dokunulmaz
    }

    [Fact]
    public void Sinir_eksi_toplam_hedef_bacak_eksi_olmaz()
    {
        var (kart, _, l) = IkiBacak(5_000m, 2_500m);
        FisHesap.OtoDengele(l, -1_000m);
        Assert.Equal(0m, kart.TutarTl);
    }

    [Fact]
    public void Sinir_fazla_odeme_hedef_sifir_fark_ekranda_kalir()
    {
        var (kart, altin, l) = IkiBacak(1m, 12_000m);
        FisHesap.OtoDengele(l, 10_000m);
        Assert.Equal(0m, kart.TutarTl);
        Assert.Equal(12_000m, FisHesap.BacakToplami(l));
    }

    [Fact]
    public void Sinir_kurus_kalani_birebir_bacak_toplami_fis_toplamina_esit()
    {
        var (kart, _, l) = IkiBacak(0m, 33_333.33m);
        Assert.True(FisHesap.OtoDengele(l, 100_000.01m));
        Assert.Equal(66_666.68m, kart.TutarTl);
        Assert.Equal(100_000.01m, FisHesap.BacakToplami(l));
    }

    [Fact]
    public void Sinir_cok_buyuk_tutar_999_999_999_99()
    {
        var (kart, _, l) = IkiBacak(0m, 0.01m);
        Assert.True(FisHesap.OtoDengele(l, 999_999_999.99m));
        Assert.Equal(999_999_999.98m, kart.TutarTl);
        Assert.Equal("999.999.999,99", Bicim.Tl(999_999_999.99m).Replace("₺", "").Trim().Replace(" ", ""));
    }

    [Fact]
    public void Sinir_doviz_ve_altin_hedef_olmaz_elle_yazilan_korunur()
    {
        var doviz = new FisBacagi { Arac = "DOVIZ", TutarTl = 100m };
        var altin = new FisBacagi { Arac = FisAltinBacagi.Arac, TutarTl = 200m };
        Assert.False(FisHesap.OtoDengele(new List<FisBacagi> { doviz, altin }, 1_000m));
        Assert.Equal(100m, doviz.TutarTl);
        Assert.Equal(200m, altin.TutarTl);
        var nakit = new FisBacagi { Arac = "NAKIT", TutarTl = 10m, ElleYazildi = true };
        var kart = new FisBacagi { Arac = "KART", TutarTl = 0m };
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { kart, nakit }, 1_000m));
        Assert.Equal(990m, kart.TutarTl);   // elle yazılmayan ilk bacak hedef (son olmasa da)
        Assert.Equal(10m, nakit.TutarTl);
    }

    [Fact]
    public void Sinir_bos_liste_ve_tek_bacak_dokunulmaz()
    {
        Assert.False(FisHesap.OtoDengele(new List<FisBacagi>(), 1_000m));
        var tek = new FisBacagi { Arac = "NAKIT", TutarTl = 5m };
        Assert.False(FisHesap.OtoDengele(new List<FisBacagi> { tek }, 1_000m));
        Assert.Equal(5m, tek.TutarTl);
    }

    [Theory]
    [InlineData(0.005, "0,01")]
    [InlineData(0.004, "0")]
    [InlineData(1.005, "1,01")]
    [InlineData(2.675, "2,68")]
    public void Sinir_kurus_yuvarlama_AwayFromZero_0_005(double girdi, string beklenen)
    {
        var s = Bicim.Tl((decimal)girdi).Replace("₺", "").Replace(" ", "").Trim();
        Assert.Equal(beklenen, s);
    }

    [Theory]
    [InlineData("45.000", 45000.0)]
    [InlineData("1,5", 1.5)]
    [InlineData("63.363,16", 63363.16)]
    [InlineData("0,0001", 0.0001)]
    [InlineData("999.999.999,99", 999999999.99)]
    public void Sinir_turkce_giris_cozulur(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void Sinir_belirsiz_1_500_reddedilir_bos_null_bos_doner()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("  ").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Gecersiz, FormAlani.SayiCoz("12a").Sonuc);
        Assert.Equal(-5.25m, FormAlani.SayiCoz("-5,25").Deger);
    }

    [Fact]
    public void Sinir_has_4_hane_gram_3_hane_bicimi()
    {
        Assert.Equal("7,5001 has", Bicim.Has(7.50005m).Replace(" ", " "));
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m).Replace(" ", " "));
    }
}

// ════════════════════════════════════════════════════════════════════════
// Doğrulama TUR 3 (28.09 15:2x — Aşkın + Mehmet). Beklenenler SABİT; üretim
// kodundan hesaplanmaz. Kapsam: üç bacaklı karma (Nakit elle + Kart otomatik +
// Eski altın türetilmiş), eksi fiş toplamı, 0,005 sınırında fiş toplamı,
// 999.999.999,99 ile has 4 hane, Türkçe giriş sınırları.
// ════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur3
{
    private static FisBacagi B(string arac, decimal tl, bool elle = false) =>
        new() { Arac = arac, TutarTl = tl, ElleYazildi = elle };

    [Fact]
    public void Tur3_uc_bacak_nakit_elle_kart_kalan_altin_turetilmis_fark_SIFIR()
    {
        var nakit = B("NAKIT", 20_000m, elle: true);
        var kart = B("KART", 0m);
        var altin = B(FisAltinBacagi.Arac, 50_380m);
        var l = new List<FisBacagi> { nakit, kart, altin };
        Assert.True(FisHesap.OtoDengele(l, 97_552.04m));
        Assert.Equal(27_172.04m, kart.TutarTl);
        Assert.Equal(20_000m, nakit.TutarTl);       // elle yazılan korunur
        Assert.Equal(50_380m, altin.TutarTl);       // türetilen dokunulmaz
        Assert.Equal(0m, FisFark.Hesapla(97_552.04m, l));
        Assert.True(FisFark.KaydetAcik(0m));
    }

    [Fact]
    public void Tur3_eksi_fis_toplami_hedef_SIFIR_fark_FAZLA_kaydet_KAPALI()
    {
        var nakit = B("NAKIT", 250m);
        var kart = B("KART", 100m, elle: true);
        var l = new List<FisBacagi> { nakit, kart };
        Assert.True(FisHesap.OtoDengele(l, -50m));
        Assert.Equal(0m, nakit.TutarTl);
        var fark = FisFark.Hesapla(-50m, l);
        Assert.Equal(-150m, fark);
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(fark));
        Assert.False(FisFark.KaydetAcik(fark));
    }

    [Fact]
    public void Tur3_kurus_0_005_siniri_fis_toplami_yuvarlanir_kalan_iki_hane()
    {
        // Fiş toplamı Yuvarla2 (AwayFromZero) ile gelir: 100,005 → 100,01; 100,004 → 100,00
        Assert.Equal(100.01m, FisHesap.Yuvarla2(100.005m));
        Assert.Equal(100.00m, FisHesap.Yuvarla2(100.004m));
        var kart = B("KART", 0m);
        var nakit = B("NAKIT", 0.01m, elle: true);
        var l = new List<FisBacagi> { kart, nakit };
        Assert.True(FisHesap.OtoDengele(l, FisHesap.Yuvarla2(100.005m)));
        Assert.Equal(100.00m, kart.TutarTl);
        Assert.Equal(0m, FisFark.Hesapla(100.01m, l));
    }

    [Fact]
    public void Tur3_cok_buyuk_999_999_999_99_altin_bir_kurus_kart_kalani_tam()
    {
        var kart = B("KART", 0m);
        var altin = B(FisAltinBacagi.Arac, 0.01m);
        var l = new List<FisBacagi> { kart, altin };
        Assert.True(FisHesap.OtoDengele(l, 999_999_999.99m));
        Assert.Equal(999_999_999.98m, kart.TutarTl);
        Assert.Equal(0m, FisFark.Hesapla(999_999_999.99m, l));
        Assert.Equal("7,5000 has", Bicim.Has(7.5m).Replace(" ", " "));
        Assert.Equal("0,0001 has", Bicim.Has(0.00005m).Replace(" ", " "));   // 0,00005 → AwayFromZero 0,0001
    }

    [Theory]
    [InlineData("0,005", "Tamam", 0.005)]
    [InlineData("1.234.567,89", "Tamam", 1234567.89)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    [InlineData("-", "Gecersiz", null)]
    [InlineData("1,2,3", "Gecersiz", null)]
    public void Tur3_turkce_giris_sinirlari(string ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DOĞRULAYICI (hasım, 28.09 dördüncü tur) — Hurda alış salt okunur hesap satırı
// (HurdaGirisi.HasKarsiligi / HesaplananTutar → #hurda-hesap). Beklenen değerler
// ELLE hesaplandı (üretim kodundan TÜRETİLMEDİ): has = gram × milyem/1000 (6 hane),
// tutar = has × fiyat, 2 hane AwayFromZero (sunucu SatisModulu.HurdaAlis Yuvarla2).
// ═════════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur4
{
    private static HurdaGirisi H(decimal? gram, string ayar, decimal? fiyat) =>
        new() { Gram = gram, Ayar = ayar, Fiyat = fiyat, SaticiTipi = "NIHAI_TUKETICI" };

    [Fact]
    public void Tur4_sifir_gram_ve_sifir_fiyat_HESAPLANMAZ_tire()
    {
        Assert.Null(H(0m, "K22", 6_000m).HasKarsiligi);
        Assert.Null(H(0m, "K22", 6_000m).HesaplananTutar);
        Assert.Equal(13.74m, H(15m, "K22", 0m).HasKarsiligi);      // 15 × 0,916
        Assert.Null(H(15m, "K22", 0m).HesaplananTutar);
    }

    [Fact]
    public void Tur4_eksi_gram_ve_eksi_fiyat_EKSI_TUTAR_URETMEZ()
    {
        Assert.Null(H(-2.5m, "K18", 6_000m).HasKarsiligi);
        Assert.Null(H(-2.5m, "K18", 6_000m).HesaplananTutar);
        Assert.Null(H(2.5m, "K18", -6_000m).HesaplananTutar);
    }

    [Fact]
    public void Tur4_kurus_0_005_siniri_AwayFromZero_ToEven_DEGIL()
    {
        // 1 g × 0,750 = 0,75 has × 1,34 = 1,005 → AwayFromZero 1,01 (ToEven 1,00 olurdu)
        Assert.Equal(1.01m, H(1m, "K18", 1.34m).HesaplananTutar);
        // 1 g × 0,750 × 1,33 = 0,9975 → 1,00 ; 3 g × 0,750 × 1,34 = 3,015 → 3,02
        Assert.Equal(1.00m, H(1m, "K18", 1.33m).HesaplananTutar);
        Assert.Equal(3.02m, H(3m, "K18", 1.34m).HesaplananTutar);
    }

    [Fact]
    public void Tur4_has_4_hane_0_0001_gram_kaybolmaz()
    {
        // 0,0001 g × 0,916 = 0,0000916 → 6 hane 0,000092 ; ekranda F4 → 0,0001 has
        Assert.Equal(0.000092m, H(0.0001m, "K22", 6_000m).HasKarsiligi);
        Assert.Equal("0,0001 has", Bicim.Has(0.000092m).Replace(" ", " "));
        // 10,0001 g K14 = 5,8500585 → 6 hane: HasHesap.ToHasGram. PAKET PATCH-4 t2 (hasım ORTA): ToHasGram
        // projenin TEK 6 hane kuralına (Yuvarla6 = AwayFromZero) bağlandı → 5,850059 (eskiden ToEven 5,850058).
        // (sunucu hurda-alis AYNI işlevi çağırır — istemci/sunucu eşliği korunur; ekranda F4 → 5,8501)
        Assert.Equal(5.850059m, H(10.0001m, "K14", 1m).HasKarsiligi);
    }

    [Fact]
    public void Tur4_cok_buyuk_999_999_999_99_sinirinda_tam()
    {
        // 1.333.333,32 g × 0,750 = 999.999,99 has × 1.000 = 999.999.990,00
        var h = H(1_333_333.32m, "K18", 1_000m);
        Assert.Equal(999_999.99m, h.HasKarsiligi);
        Assert.Equal(999_999_990.00m, h.HesaplananTutar);
        // 999.999.999,99'a tam oturan: 1.333.333,332 g × 0,75 = 999.999,999 → 6 hane aynen; × 1.000 = 999.999.999,00
        Assert.Equal(999_999_999.00m, H(1_333_333.332m, "K18", 1_000m).HesaplananTutar);
        Assert.Contains("999.999.990", Bicim.Tl(999_999_990.00m));
    }

    [Fact]
    public void Tur4_bos_null_ve_bilinmeyen_ayar_tire()
    {
        Assert.Null(H(null, "K22", 6_000m).HasKarsiligi);
        Assert.Null(H(5m, "K22", null).HesaplananTutar);
        Assert.Null(H(5m, "YOK99", 6_000m).HasKarsiligi);
        Assert.Null(H(5m, "YOK99", 6_000m).HesaplananTutar);
    }

    [Theory]
    [InlineData("45.000", "Tamam", 45000.0)]
    [InlineData("1,5", "Tamam", 1.5)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("0,0001", "Tamam", 0.0001)]
    [InlineData("999.999.999,99", "Tamam", 999999999.99)]
    public void Tur4_turkce_giris_hurda_gram_fiyat(string ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DOĞRULAYICI (hasım, 28.09 beşinci tur — t5) — varsayılan kasa (FisFark.IkinciBacak /
// IlkBacak), karma kilit sebebi metinleri (Satis3Sozlugu.FarkEksik/FarkFazla + Bicim.Tl),
// "Güncel fiyatı kullan: {0}" düğme metni ve eşik sayısının metne gömülmemesi.
// Beklenen değerler SABİT yazıldı (üretim kodundan TÜRETİLMEDİ).
// ═════════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur5
{
    private static readonly List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> IkiTlBirUsd =
    [
        new("K1", "Ana Kasa", "TRY", true),
        new("K2", "Vitrin Kasası", "TRY", true),
        new("K3", "Dolar Kasası", "USD", true),
    ];

    [Fact]
    public void Tur5_sifir_ikinci_odeme_tutari_SIFIR_ve_fisin_varsayilan_kasasini_alir()
    {
        var b = FisFark.IkinciBacak(IkiTlBirUsd, "K2");
        Assert.Equal("NAKIT", b.Arac);
        Assert.Equal(0m, b.TutarTl);
        Assert.Equal("K2", b.KasaId);
    }

    [Fact]
    public void Tur5_bos_null_fis_kasasi_iki_TL_kasada_BOS_kalir_ve_doviz_kasasi_varsayilan_OLAMAZ()
    {
        Assert.Equal("", FisFark.IkinciBacak(IkiTlBirUsd, null).KasaId);
        Assert.Equal("", FisFark.IkinciBacak(IkiTlBirUsd, "K3").KasaId);   // USD kasası TL ödemeye giremez
        Assert.Equal("", FisFark.IkinciBacak(IkiTlBirUsd, "YOK").KasaId);
    }

    [Fact]
    public void Tur5_tek_TL_kasasi_otomatik_atanir_fis_kasasi_bossa_bile()
    {
        var kasalar = new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri>
        { new("K1", "Ana Kasa", "TRY", true), new("K3", "Dolar Kasası", "USD", true) };
        Assert.Equal("K1", FisFark.IkinciBacak(kasalar, "").KasaId);
    }

    [Fact]
    public void Tur5_cok_buyuk_ilk_bacak_999_999_999_99_tutar_birebir_tanimsiz_arac_NAKIT_kasasiz()
    {
        var kart = FisFark.IlkBacak("KART", "K1", 999_999_999.99m);
        Assert.Equal(999_999_999.99m, kart.TutarTl);
        Assert.Equal("K1", kart.KasaId);
        var veresiye = FisFark.IlkBacak("VERESIYE", "K1", 10m);
        Assert.Equal("NAKIT", veresiye.Arac);
        Assert.Equal("", veresiye.KasaId);   // fail-closed
    }

    [Fact]
    public void Tur5_eksi_ve_sifir_fark_durumu_ve_kilit_metni()
    {
        var m = Kuyumcu.Core.I18n.Satis3Sozlugu.Tr;
        Assert.Equal(FarkDurumu.Tamam, FisFark.Durum(0m));
        Assert.Equal(FarkDurumu.Eksik, FisFark.Durum(0.01m));
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(-0.01m));
        Assert.Equal("Eksik 7.500 ₺", string.Format(m.FarkEksik, Bicim.Tl(7_500m)));
        Assert.Equal("Fazla 0,01 ₺ — ödeme tutarını azaltın", string.Format(m.FarkFazla, Bicim.Tl(0.01m)));
    }

    [Theory]
    [InlineData(0.005, "Eksik 0,01 ₺")]     // AwayFromZero: 0,005 → 0,01
    [InlineData(0.015, "Eksik 0,02 ₺")]     // ToEven olsaydı 0,02; 0,025 aşağıda ayırır
    [InlineData(0.025, "Eksik 0,03 ₺")]     // ToEven olsaydı 0,02 — kural AwayFromZero
    [InlineData(999999999.99, "Eksik 999.999.999,99 ₺")]
    public void Tur5_kurus_yuvarlama_kilit_metni_AwayFromZero(double fark, string beklenen)
    {
        var m = Kuyumcu.Core.I18n.Satis3Sozlugu.Tr;
        Assert.Equal(beklenen, string.Format(m.FarkEksik, Bicim.Tl((decimal)fark)));
    }

    [Fact]
    public void Tur5_has_4_hane_0_0001_ve_guncel_fiyat_dugmesi_metni()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("Güncel fiyatı kullan: 6.574,25 ₺",
            string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.FiyatKullan, Bicim.Para(6_574.25m, "TRY")));
        Assert.Equal("Use current price: 999.999.999,99 ₺",
            string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.En.FiyatKullan, Bicim.Para(999_999_999.99m, "TRY")));
    }

    [Fact]
    public void Tur5_yeni_metinler_TR_EN_dolu_esik_sayisi_ve_jargon_YOK()
    {
        foreach (var m in new[] { Kuyumcu.Core.I18n.Satis3Sozlugu.Tr, Kuyumcu.Core.I18n.Satis3Sozlugu.En })
        {
            foreach (var s in new[] { m.NakitAlinamaz, m.OdemeyiTamamlayin, m.FiyatKullan, m.MusteriSecin, m.YasalUygun })
            {
                Assert.False(string.IsNullOrWhiteSpace(s));
                Assert.DoesNotMatch(@"\d", s.Replace("{0}", ""));   // eşik sayısı metne gömülmez (ADR-0005)
            }
        }
        var tr = Kuyumcu.Core.I18n.Satis3Sozlugu.Tr;
        Assert.DoesNotContain("bacak", tr.NakitAlinamaz, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NAKİT", tr.NakitAlinamaz);
        Assert.DoesNotContain("İKİ", Kuyumcu.Core.I18n.AltinBacagiSozlugu.Tr.IkiBelge);
    }

    [Theory]
    [InlineData("45.000", "Tamam", 45000.0)]
    [InlineData("1,5", "Tamam", 1.5)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("999.999.999,99", "Tamam", 999999999.99)]
    [InlineData("-7.500", "Belirsiz", null)]   // eksi de olsa tek nokta+3 hane belirsizdir
    [InlineData(null, "Bos", null)]
    public void Tur5_turkce_giris(string? ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DOĞRULAYICI (hasım, 28.09 altıncı tur — t6) — t6'nın dokunduğu para yolları:
// FisHesap.VarsayilanKasa (nakdin YAZILACAĞI kasa, oturumun şubesi) · AltinBacagiGirisi
// (FiyatKurdan / K-KUR-4 ön-dolgu yok) → AltinBacagiHesap.NetTl · FisYazdir.ParaUstu
// (karmada katlı para üstü). Beklenenler ELLE hesaplandı (üretim kodundan türetilmedi):
// has = gram × milyem/1000 (K22 = 916, 6 hane); TL = has × fiyat, 2 hane AwayFromZero.
// ═════════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur6
{
    private static readonly AltinBacagiAyarlari OranSifir =
        new([new(AltinBacagiGirisi.NihaiTuketici, 0m, true)], null, true);

    private static AltinBacagiGirisi A(decimal? gram, decimal? fiyat) =>
        new() { AyarId = "K22", Gram = gram, Fiyat = fiyat, SaticiTipi = AltinBacagiGirisi.NihaiTuketici };

    private static Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri K(string id, string? sube) => new(id, id, "TRY", true, sube);

    [Fact]
    public void Tur6_sifir_bos_kasa_listesi_null_sifir_gram_ve_sifir_fiyat_hesap_YOK_para_ustu_sifir()
    {
        Assert.Null(FisHesap.VarsayilanKasa([], "s1"));
        Assert.Null(FisHesap.VarsayilanKasa([], null));
        Assert.Null(A(0m, 6_574.25m).Deger(OranSifir));
        Assert.Null(A(5m, 0m).Deger(OranSifir));
        Assert.Equal(0m, FisYazdir.ParaUstu(0m, 0m));
    }

    [Fact]
    public void Tur6_eksi_gram_ve_eksi_fiyat_EKSI_TL_URETMEZ_eksik_para_ustu_ISARETI_eksi()
    {
        Assert.Null(A(-1m, 6_574.25m).Deger(OranSifir));
        Assert.Null(A(5m, -1m).Deger(OranSifir));
        Assert.Equal(-50m, FisYazdir.ParaUstu(100m, 150m));      // eksik: işaret eksi
        Assert.Equal(50m, FisYazdir.ParaUstu(150m, 100m));
    }

    [Fact]
    public void Tur6_kurus_0_005_siniri_AwayFromZero_ToEven_DEGIL()
    {
        // 1,25 g × 0,916 = 1,145 has; × 1 ₺ = 1,145 → AwayFromZero 1,15 (ToEven 1,14 olurdu)
        var d = A(1.25m, 1m).Deger(OranSifir);
        Assert.NotNull(d);
        Assert.Equal(1.145m, d!.HasGrami);
        Assert.Equal(1.15m, d.NetTl);
        Assert.Equal(0.01m, FisYazdir.ParaUstu(100.005m, 100m));   // 0,005 → 0,01
        Assert.Equal(0.00m, FisYazdir.ParaUstu(100.004m, 100m));
        Assert.Equal(-0.01m, FisYazdir.ParaUstu(100m, 100.005m));  // −0,005 → −0,01 (sıfırdan uzağa)
    }

    [Fact]
    public void Tur6_has_4_hane_0_0001_gram_KAYBOLMAZ()
    {
        // 0,0001 g × 0,916 = 0,0000916 → 6 hane 0,000092 has; × 6.574,25 = 0,604831 → 0,60 ₺
        var d = A(0.0001m, 6_574.25m).Deger(OranSifir);
        Assert.NotNull(d);
        Assert.Equal(0.000092m, d!.HasGrami);
        Assert.Equal(0.60m, d.NetTl);
        Assert.Equal("0,0001 has", Bicim.Has(d.HasGrami).Replace(" ", " "));
        // 5 g × 0,916 = 4,58 has × 6.571,95 = 30.099,531 → 30.099,53 (tarayıcıda ölçülen değer)
        Assert.Equal(30_099.53m, A(5m, 6_571.95m).Deger(OranSifir)!.NetTl);
    }

    [Fact]
    public void Tur6_cok_buyuk_999_999_999_99()
    {
        Assert.Equal(999_999_999.98m, FisYazdir.ParaUstu(999_999_999.99m, 0.01m));
        Assert.Equal(-999_999_999.99m, FisYazdir.ParaUstu(0m, 999_999_999.99m));
        // 1 g × 0,916 × 999.999,99 = 915.999,990840 → 915.999,99
        Assert.Equal(915_999.99m, A(1m, 999_999.99m).Deger(OranSifir)!.NetTl);
        // 1.091.703,0568 g (0,916 ile ~1.000.000 has) × 999,99 → taşma yok, 2 hane
        var d = A(1_000_000m, 999.99m).Deger(OranSifir)!;
        Assert.Equal(916_000m, d.HasGrami);
        Assert.Equal(915_990_840.00m, d.NetTl);
    }

    [Fact]
    public void Tur6_bos_null_fiyat_yoksa_hesap_YOK_KKUR4_on_dolgu_YAPILMAZ_para_ustu_null()
    {
        Assert.Null(A(5m, null).Deger(OranSifir));
        Assert.Null(A(null, 6_574.25m).Deger(OranSifir));
        Assert.Null(A(5m, 6_574.25m).Deger(null));                 // ayarlar gelmediyse (oran bilinmiyor) TL uydurulmaz
        var g = A(5m, null);
        g.OnDoldur(6_574.25m, OranSifir);                           // iskonto oranı YAPILANDIRILMAMIŞ → alan BOŞ kalır
        Assert.Null(g.Fiyat);
        Assert.False(g.FiyatKurdan);
        Assert.Null(FisYazdir.ParaUstu(null, 1_000m));
    }

    [Fact]
    public void Tur6_varsayilan_kasa_BASKA_subenin_kasasina_nakit_YAZMAZ()
    {
        var kasalar = new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> { K("A", "s2"), K("B", "s1"), K("C", "s1") };
        Assert.Equal("B", FisHesap.VarsayilanKasa(kasalar, "s1"));   // oturum şubesinin İLK kasası
        Assert.Null(FisHesap.VarsayilanKasa(kasalar, "s9"));         // şubede kasa yok → seçici zorunlu
        Assert.Null(FisHesap.VarsayilanKasa(kasalar, null));         // şubesiz oturum + iki şube → tahmin yok
        Assert.Equal("C", FisHesap.VarsayilanKasa([K("C", "s1"), K("B", "s1")], null));   // tek şube → ilk
        Assert.Equal("N", FisHesap.VarsayilanKasa([K("N", null), K("A", "s2")], "s1"));  // şubesiz (eski yanıt) kasa kabul
    }

    [Theory]
    [InlineData("0,0001", "Tamam", 0.0001)]
    [InlineData("6.571,95", "Tamam", 6571.95)]
    [InlineData("45.000", "Tamam", 45000.0)]
    [InlineData("1,5", "Tamam", 1.5)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("  ", "Bos", null)]
    public void Tur6_turkce_giris_altin_fiyat_ve_gram(string ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DOĞRULAYICI (hasım, 28.09 yedinci tur — t7). t7'nin dokunduğu para yolu:
// Takas "Yeni ürün" fiyatı artık TEK arama kutusundan (Islemler.Arama.cs TakasUrunAta)
// teklifle doluyor; fark önizlemesi TakasGirisi.Fark. Beklenenler ELLE hesaplandı:
// has = gram × milyem/1000 (K22 = 916), fark = yeniAdet×yeniFiyat − has×hasFiyat.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriTur7 : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public SATIS3SinirTestleriTur7()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private const string U1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
    private const string U2 = """{"id":"u2","ad":"Hasır Bilezik","ayarId":"K14","ayar":"14 Ayar","hasGrami":2.1,"iscilikTl":0,"satisFiyati":null,"gramAgirlik":3.6,"milyem":585,"barkod":"8690000000999","kod":"K-2"}""";

    private sealed class Sahte(SATIS3SinirTestleriTur7 t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => """{"had":50000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":50000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende Varsayilan","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") && v.Contains("urunId=u2") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler/barkod/8690000000412") => U1,
                var v when v.Contains("/urunler/barkod/8690000000999") => U2,
                var v when v.Contains("/urunler") => $"[{U1},{U2}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/islemler/takas") => """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"TAKAS","tlTutari":1}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TakasEkrani()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, ArayuzModuServisi.Profesyonel));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — Takas'a tezgâhın "Diğer" listesinden geçilir.
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        e.YildizTurGec("TAKAS");
        e.WaitForElement("#takas-eski-gram");
        return e;
    }

    private static void Okut(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string barkod)
    {
        e.Find("#islem-barkod").Focus();
        e.Find("#islem-barkod").Input(barkod);
        e.Find("#islem-barkod").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
    }

    /// <summary>
    /// SINIR (boş/null fiyat): Takas'ta önce fiyatı çıkan ürün (u1, 10.000) sonra fiyatı ÇIKMAYAN ürün (u2,
    /// LISTE_YOK) okutulunca yeni ürünün fiyatı BOŞ kalmalı (ya da sebep yazmalı) — u1'in 10.000'i u2'ye
    /// sessizce TAŞINMAMALI. Beklenen: #takas-yeni-fiyat boş VE Kaydet kilitli.
    /// </summary>
    [Fact]
    public void Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ()
    {
        var e = TakasEkrani();
        Okut(e, "8690000000412");
        e.WaitForAssertion(() => Assert.Equal("10.000", e.Find("#takas-yeni-fiyat").GetAttribute("value")), TimeSpan.FromSeconds(5));
        Okut(e, "8690000000999");
        e.WaitForAssertion(() => Assert.Contains("Hasır Bilezik", e.Find("#takas-yeni-secili").TextContent), TimeSpan.FromSeconds(5));
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#takas-eski-gram").Input("5");
        var deger = e.Find("#takas-yeni-fiyat").GetAttribute("value") ?? "";
        Assert.True(deger is "" or "0", $"u2'nin fiyatı çıkmadı ama kutuda önceki ürünün fiyatı duruyor: '{deger}'");
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"), "fiyatı bilinmeyen yeni ürünle Kaydet açık");
    }

    [Fact]
    public void Tur7_sifir_ve_bos_fark_HESAPLANMAZ_takas_TAMAM_degil()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 0m, EskiFiyat = 6800m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Null(t.Fark); Assert.False(t.Tamam);
        t.EskiGram = null; Assert.Null(t.Fark); Assert.False(t.Tamam);
        t.EskiGram = 5m; t.YeniFiyat = null; Assert.Null(t.Fark); Assert.False(t.Tamam);
        t.YeniFiyat = 0m; Assert.Null(t.Fark); Assert.False(t.Tamam);
        t.YeniFiyat = 10000m; t.YeniUrunId = ""; Assert.False(t.Tamam);
    }

    [Fact]
    public void Tur7_eksi_gram_ve_eksi_fiyat_fark_URETMEZ_eksi_fark_isareti_dogru()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = -5m, EskiFiyat = 6800m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Null(t.Fark); Assert.False(t.Tamam);
        t.EskiGram = 5m; t.YeniFiyat = -1m; Assert.Null(t.Fark); Assert.False(t.Tamam);
        // eski altın yeniden değerli: 10 g K22 → 9,16 has × 6.800 = 62.288; yeni 10.000 → fark −52.288 (iade)
        t.EskiGram = 10m; t.YeniFiyat = 10000m;
        Assert.Equal(-52288m, t.Fark);
    }

    [Fact]
    public void Tur7_kurus_0_005_siniri_AwayFromZero_ToEven_DEGIL()
    {
        // 1 g K22 → 0,916 has × 0,005 = 0,00458 ; yeni 1 × 0,02 = 0,02 → fark 0,01542 → 0,02
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 0.005m, YeniUrunId = "u1", YeniFiyat = 0.02m };
        Assert.Equal(0.01542m, t.Fark);
        Assert.Equal("0,02", Bicim.Tl(t.Fark!.Value).Replace("₺", "").Replace(" ", "").Replace(" ", "").Trim());
        Assert.Equal("0,01", Bicim.Tl(0.005m).Replace("₺", "").Replace(" ", "").Replace(" ", "").Trim());   // AwayFromZero
        Assert.Equal("0,03", Bicim.Tl(0.025m).Replace("₺", "").Replace(" ", "").Replace(" ", "").Trim());   // ToEven olsa 0,02
    }

    [Fact]
    public void Tur7_has_4_hane_0_0001_kaybolmaz()
    {
        // 0,0001 g K22 → 0,0000916 has, 6 haneye yuvarlanır
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 0.0001m };
        Assert.Equal(0.000092m, t.EskiHas);   // 0,0000916 → 6 hane (ToHasGram) → 0,000092 ; 4 hanede 0,0001 görünür, sıfıra düşmez
        t.EskiGram = 1.0001m;
        Assert.Equal(0.916092m, t.EskiHas);
    }

    [Fact]
    public void Tur7_cok_buyuk_999_999_999_99_fark_tam()
    {
        var t = new TakasGirisi { EskiAyar = "K24", EskiGram = 1m, EskiFiyat = 0.01m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 999_999_999.99m };
        // K24 milyem 995 ya da 999 olabilir — has×0,01 kuruşun altında kalır; fark 999.999.999,98 ile ,99 arasında
        Assert.Equal(999_999_999.98m, t.Fark);   // K24 = 1000 milyem → 1 has × 0,01 = 0,01
        Assert.Equal("999.999.999,99", Bicim.Tl(999_999_999.99m).Replace("₺", "").Replace(" ", "").Replace(" ", "").Trim());
    }

    [Theory]
    [InlineData("45.000", "Tamam", 45000.0)]
    [InlineData("1,5", "Tamam", 1.5)]
    [InlineData("0,0001", "Tamam", 0.0001)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    [InlineData(null, "Bos", null)]
    public void Tur7_turkce_giris_takas_fiyat_gram(string? ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ════════════════════════════════════════════════════════════════════════
// SATIS-3 — HASIM DOĞRULAYICI TUR 8 (28.09 t8 kapanış kontrolü) · beklenenler EL HESABI, sabit.
// ════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur8
{
    private static TakasGirisi Gecerli() => new()
    { EskiAyar = "K22", EskiGram = 10m, EskiFiyat = 6800m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 70000m };

    private static string Tl(decimal d) => Bicim.Tl(d).Replace("₺", "").Replace(" ", "").Replace(" ", "").Trim();

    [Fact]
    public void Tur8_sifir_adet_takas_TAMAM_degil()
    {
        var t = Gecerli(); t.YeniAdet = 0;
        Assert.False(t.Tamam);
        t.YeniAdet = 1; t.EskiFiyat = 0m;
        Assert.False(t.Tamam); Assert.Null(t.Fark);
    }

    [Fact]
    public void Tur8_eksi_adet_takas_TAMAM_degil()
    {
        var t = Gecerli(); t.YeniAdet = -1;
        Assert.False(t.Tamam);
    }

    [Fact]
    public void Tur8_gecerli_takas_farki_el_hesabi()
    {
        // 10 g K22 → 9,16 has × 6.800 = 62.288 ; yeni 70.000 → müşteri 7.712 öder
        var t = Gecerli();
        Assert.True(t.Tamam);
        Assert.Equal(7712m, t.Fark);
    }

    [Fact]
    public void Tur8_kurus_0_005_ve_0_015_AwayFromZero()
    {
        Assert.Equal("0,01", Tl(0.005m));
        Assert.Equal("0,02", Tl(0.015m));
        Assert.Equal("0", Tl(0.004999m));   // Bicim.Tl: tam sayıya yuvarlananda kuruş basılmaz (belgelenmiş)
    }

    [Fact]
    public void Tur8_has_4_hane_10_0001_g_K22()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 10.0001m };
        Assert.Equal(9.160092m, t.EskiHas);   // 9,1600916 → 6 hane
    }

    [Fact]
    public void Tur8_cok_buyuk_yeni_fiyat_999_999_999_99_fark_tam()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 1m, YeniUrunId = "u1", YeniFiyat = 999999999.99m };
        Assert.Equal(999999999.074m, t.Fark);
        Assert.True(t.Tamam);
    }

    [Fact]
    public void Tur8_bos_null_yeni_urun_ve_fiyat_TAMAM_degil_Sifirla_fiyatsiz()
    {
        var t = Gecerli(); t.YeniFiyat = null;
        Assert.False(t.Tamam); Assert.Null(t.Fark);
        t = Gecerli(); t.YeniUrunId = "";
        Assert.False(t.Tamam);
        t = Gecerli(); t.Sifirla(0m);
        Assert.Null(t.EskiFiyat); Assert.Null(t.YeniFiyat); Assert.Equal("", t.YeniUrunId); Assert.False(t.Tamam);
    }

    [Theory]
    [InlineData("45.000", "Tamam", 45000.0)]
    [InlineData("1,5", "Tamam", 1.5)]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("-1,5", "Tamam", -1.5)]
    [InlineData("999.999.999,99", "Tamam", 999999999.99)]
    [InlineData("", "Bos", null)]
    [InlineData("   ", "Bos", null)]
    [InlineData("1,2,3", "Gecersiz", null)]
    [InlineData("-", "Gecersiz", null)]
    public void Tur8_turkce_giris_SayiCoz(string ham, string sonuc, double? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : (decimal?)(decimal)deger.Value, c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 10 (28.09, t9 sonrası) — Takas/Hurda önizleme ve otomatik kalan
// sınırları. Beklenen değerler ELLE hesaplandı (üretim kodundan türetilmez):
//   K22 milyem 916 · 12,5 g → has 11,45 · × 0,10 = 1,145 → AwayFromZero 1,15 (ToEven 1,14 olurdu)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriTur10 : BunitContext
{
    public SATIS3SinirTestleriTur10()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    [Fact]
    public void T10_hurda_sifir_ve_eksi_gram_TUTAR_URETMEZ()
    {
        Assert.Null(new HurdaGirisi { Ayar = "K22", Gram = 0m, Fiyat = 6_500m }.HesaplananTutar);
        Assert.Null(new HurdaGirisi { Ayar = "K22", Gram = -3m, Fiyat = 6_500m }.HesaplananTutar);
        Assert.Null(new HurdaGirisi { Ayar = "K22", Gram = 3m, Fiyat = 0m }.HesaplananTutar);
        Assert.Null(new HurdaGirisi { Ayar = "K22", Gram = 3m, Fiyat = -1m }.HesaplananTutar);
        Assert.Null(new HurdaGirisi { Ayar = "K22", Gram = null, Fiyat = 6_500m }.HesaplananTutar);
    }

    [Fact]
    public void T10_hurda_kurus_yarimi_AwayFromZero_1_145_olur_1_15()
    {
        var h = new HurdaGirisi { Ayar = "K22", Gram = 12.5m, Fiyat = 0.10m };
        Assert.Equal(11.45m, h.HasKarsiligi);
        Assert.Equal(1.15m, h.HesaplananTutar);
    }

    [Fact]
    public void T10_hurda_has_4_hane_sinir_0_0001_gram()
    {
        // 0,0001 g × 0,916 = 0,0000916 → sunucu ToHasGram 6 hane → 0,000092
        var h = new HurdaGirisi { Ayar = "K22", Gram = 0.0001m, Fiyat = 1m };
        Assert.Equal(0.000092m, h.HasKarsiligi);
        Assert.Equal(0m, h.HesaplananTutar);   // 0,000092 ₺ → 2 hane 0,00
    }

    [Fact]
    public void T10_takas_eksi_ve_sifir_girdiler_TAMAM_degil_fark_yok()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = -5m, EskiFiyat = 6_800m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 10_000m };
        Assert.False(t.Tamam); Assert.Null(t.Fark);
        t.EskiGram = 5m; t.YeniAdet = 0;
        Assert.False(t.Tamam);
        t.YeniAdet = 1; t.YeniFiyat = 0m;
        Assert.False(t.Tamam); Assert.Null(t.Fark);
        t.YeniFiyat = null;
        Assert.False(t.Tamam); Assert.Null(t.Fark);
        t.YeniFiyat = 10_000m; t.EskiFiyat = -1m;
        Assert.False(t.Tamam); Assert.Null(t.Fark);
    }

    [Fact]
    public void T10_takas_fark_kurus_yarimi_ekranda_0_86_sunucu_Yuvarla2_ile_ayni()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 12.5m, EskiFiyat = 0.10m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 2m };
        Assert.Equal(1.145m, t.EskiDeger);
        Assert.Equal(0.855m, t.Fark);
        var c = Render<FisTakas>(p => p.Add(x => x.Takas, t).Add(x => x.Ayarlar, new[] { "K22" })
            .Add(x => x.UrunSecenekleri, Array.Empty<SecenekOgesi>()));
        var farkMetni = c.Find(".takas-fark-toplam .sayi").TextContent;
        Assert.Contains("0,86", farkMetni);          // sunucu: Yuvarla2(0,855) = 0,86 (AwayFromZero)
        Assert.DoesNotContain("0,85", farkMetni);
    }

    [Fact]
    public void T10_takas_cok_buyuk_tutar_999_999_999_99_tr_TR_ve_has_4_hane()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 0.0001m, EskiFiyat = 1m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 999_999_999.99m };
        Assert.True(t.Tamam);
        Assert.Equal(0.000092m, t.EskiHas);
        Assert.Equal(999_999_999.989908m, t.Fark);
        var c = Render<FisTakas>(p => p.Add(x => x.Takas, t).Add(x => x.Ayarlar, new[] { "K22" })
            .Add(x => x.UrunSecenekleri, Array.Empty<SecenekOgesi>()));
        var metin = c.Find(".takas-fark-govde").TextContent;
        Assert.Contains("999.999.999,99", metin);
        Assert.Contains("0,0001 has", metin);        // has 4 hane (F4 tr-TR)
        Assert.DoesNotContain("TRY", metin);
    }

    [Fact]
    public void T10_otomatik_kalan_EKSIYE_dusmez_buyuk_tutarda_kurus_kaybi_yok()
    {
        var kart = new FisBacagi { Arac = "KART", TutarTl = 5m };
        var nakit = new FisBacagi { Arac = "NAKIT", TutarTl = 12_000m, ElleYazildi = true };
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { nakit, kart }, 10_000m));
        Assert.Equal(0m, kart.TutarTl);             // 10.000 − 12.000 → 0 (eksi değil)

        var k2 = new FisBacagi { Arac = "KART", TutarTl = 0m };
        var n2 = new FisBacagi { Arac = "NAKIT", TutarTl = 0.01m, ElleYazildi = true };
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { n2, k2 }, 999_999_999.99m));
        Assert.Equal(999_999_999.98m, k2.TutarTl);
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,005", "0.005")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void T10_turkce_sayi_girisi_cozulur(string ham, string beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), s.Deger);
    }

    [Fact]
    public void T10_belirsiz_1_500_REDDEDILIR_bos_null()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
        Assert.Equal(-5m, FormAlani.SayiCoz("-5").Deger);   // işaret korunur; ret alanın işi
    }
}

// ════════════════════════════════════════════════════════════════════════
// TUR 11 — HASIM DOĞRULAYICI (29.09, Aşkın + Mehmet) — t10 yeni yolları:
// FisFark.SifirSatirlariDusur · YeniBacakAraci · IkinciBacak (kasa) ·
// FisHesap.VarsayilanKasa · NakitKapali (0/eksi) · OtoDengele sınırları ·
// Islemler.TlSembolu (sunucu metnindeki TL tutarı). Beklenenler SABİT.
// ════════════════════════════════════════════════════════════════════════
public class SATIS3SinirTestleriTur11
{
    private static FisBacagi B(string arac, decimal tl, bool elle = false) => new() { Arac = arac, TutarTl = tl, ElleYazildi = elle };
    private static Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri K(string id, string pb = "TRY", string? sube = null) => new(id, id, pb, true, sube);

    [Fact]
    public void T11_sifir_otomatik_satir_duser_elle_sifir_ve_altin_KALIR()
    {
        var elleSifir = B("KART", 0m, elle: true);
        var oto = B("HAVALE", 0m);
        var altin = B("ALTIN", 0m);
        var nakit = B("NAKIT", 100m, elle: true);
        var l = new List<FisBacagi> { nakit, elleSifir, oto, altin };
        Assert.True(FisFark.SifirSatirlariDusur(l, null));
        Assert.Equal(3, l.Count);
        Assert.DoesNotContain(oto, l);
        Assert.Contains(elleSifir, l);
        Assert.Contains(altin, l);
    }

    [Fact]
    public void T11_sifir_satir_DUZENLENEN_ise_kalir_hepsi_sifirsa_liste_BOSALMAZ()
    {
        var a = B("NAKIT", 0m); var k = B("KART", 0m);
        var l = new List<FisBacagi> { a, k };
        Assert.False(FisFark.SifirSatirlariDusur(l, null));   // hepsi düşecekti → hiçbiri düşmez
        Assert.Equal(2, l.Count);
        var l2 = new List<FisBacagi> { B("NAKIT", 50m, true), k };
        Assert.False(FisFark.SifirSatirlariDusur(l2, k));     // düzenlenen satır düşmez
        Assert.Equal(2, l2.Count);
    }

    [Fact]
    public void T11_eksi_tutarli_otomatik_satir_SIFIR_sayilmaz_dusmez()
    {
        var eksi = B("KART", -0.01m);
        var l = new List<FisBacagi> { B("NAKIT", 10m, true), eksi };
        Assert.False(FisFark.SifirSatirlariDusur(l, null));
        Assert.Equal(2, l.Count);
    }

    [Fact]
    public void T11_yeni_bacak_araci_sirasi_NAKIT_KART_HAVALE_ucu_de_varsa_NAKIT()
    {
        Assert.Equal("KART", FisFark.YeniBacakAraci([B("NAKIT", 1m)]));
        Assert.Equal("HAVALE", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m)]));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([B("KART", 1m)]));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m), B("HAVALE", 1m)]));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([]));
    }

    [Fact]
    public void T11_ikinci_bacak_tutar_SIFIR_iki_TL_kasada_fis_kasasi_dovize_GIRMEZ()
    {
        var kasalar = new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> { K("K1"), K("K2"), K("USD1", "USD") };
        var y = FisFark.IkinciBacak(kasalar, "K2", [B("NAKIT", 100m)]);
        Assert.Equal("KART", y.Arac);
        Assert.Equal(0m, y.TutarTl);
        Assert.Equal("K2", y.KasaId);
        var y2 = FisFark.IkinciBacak(kasalar, "USD1", [B("NAKIT", 100m)]);   // TL bacağına döviz kasası önerilmez
        Assert.Equal("", y2.KasaId);
        var tek = FisFark.IkinciBacak(new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> { K("T1"), K("U1", "USD") }, null, null);
        Assert.Equal("NAKIT", tek.Arac);
        Assert.Equal("T1", tek.KasaId);   // tek TL kasası otomatik
    }

    [Fact]
    public void T11_varsayilan_kasa_sube_kurali_ve_bos_liste()
    {
        Assert.Null(FisHesap.VarsayilanKasa([], "S1"));
        var l = new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> { K("A", sube: "S2"), K("B", sube: "S1"), K("C", sube: "S1") };
        Assert.Equal("B", FisHesap.VarsayilanKasa(l, "S1"));
        Assert.Null(FisHesap.VarsayilanKasa(l, "S9"));        // şubede kasa yok → null (seçici zorunlu)
        Assert.Null(FisHesap.VarsayilanKasa(l, null));        // şubesiz oturum, iki şube → null
        Assert.Equal("A", FisHesap.VarsayilanKasa([K("A", sube: "S2"), K("B", sube: "S2")], null));
        Assert.Equal("X", FisHesap.VarsayilanKasa([K("X"), K("B", sube: "S1")], "S9"));   // şubesiz kasa her şubeye aday
    }

    [Fact]
    public void T11_nakit_kapali_sifir_ve_eksi_nakit_KILITLEMEZ_kurus_KILITLER()
    {
        Assert.False(FisHesap.NakitKapali(true, [B("NAKIT", 0m), B("KART", 50_000m)]));
        Assert.False(FisHesap.NakitKapali(true, [B("NAKIT", -1m)]));
        Assert.True(FisHesap.NakitKapali(true, [B("NAKIT", 0.01m)]));
        Assert.False(FisHesap.NakitKapali(false, [B("NAKIT", 999_999_999.99m)]));
    }

    [Fact]
    public void T11_oto_dengele_kurus_0_005_ve_cok_buyuk_ve_sifir_toplam()
    {
        var n = B("NAKIT", 0.005m, true); var k = B("KART", 0m);
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { n, k }, 0.01m));
        Assert.Equal(0.005m, k.TutarTl);                        // kalan birebir: 0,01 − 0,005 (yuvarlanmaz, toplam = fiş)
        var n2 = B("NAKIT", 0.0001m, true); var k2 = B("KART", 5m);
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { n2, k2 }, 999_999_999.99m));
        Assert.Equal(999_999_999.9899m, k2.TutarTl);
        var k3 = B("KART", 7m);
        Assert.True(FisHesap.OtoDengele(new List<FisBacagi> { B("NAKIT", 3m, true), k3 }, 0m));
        Assert.Equal(0m, k3.TutarTl);                           // eksiye düşmez
    }

    [Theory]
    [InlineData("Peşin Müşteri ile bu tutarda işlem yapılamaz: 127.937,79 TL, eşiğine", "Peşin Müşteri ile bu tutarda işlem yapılamaz: 127.937,79\u00a0₺, eşiğine")]
    [InlineData("Tutar 45.000 TL", "Tutar 45.000\u00a0₺")]
    [InlineData("Tutar 999.999.999,99 TL oldu", "Tutar 999.999.999,99\u00a0₺ oldu")]
    [InlineData("TL yok burada", "TL yok burada")]
    [InlineData("", "")]
    public void T11_sunucu_TL_tutari_tek_bicime(string ham, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Web.Pages.Islemler.TlSembolu(ham));

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    public void T11_turkce_giris(string ham, string beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), s.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (29.09, tur t11 sonrası) — TEVSİK HADDİ EŞİTLİK SINIRI
// Birincil kaynak: 459 s. VUK Genel Tebliği §4.1 "… TL'yi AŞAN tutardaki her türlü
// tahsilat ve ödemeleri …" (Resmî Gazete 24.12.2015/29572). Sunucu buna uyar:
// TevsikKurali.Degerlendir "tam had hâlâ serbesttir" (toplam <= Had → gerekmez),
// TevsikKapisi Peşin Müşteri: olcut > Had. Fiş TAM hadde iken istemci Kaydet'i
// KİLİTLEMEMELİ (yasal ve sunucunun kabul ettiği işlem ekranda engellenmez).
// Fiş toplamı sabit 10.000 (u1 satış fiyatı); beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3TevsikEsitlikSinirTestleri : BunitContext
{
    private string tevsikJson = "";

    public SATIS3TevsikEsitlikSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(SATIS3TevsikEsitlikSinirTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            const string u1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => t.tevsikJson,
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende Varsayilan","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $"[{u1}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Fis(string had, string mod = ArayuzModuServisi.Basit)
    {
        tevsikJson = "{\"had\":" + had + ",\"kuralEtkin\":true,\"kumulatifHesaplandi\":true,\"bugunkuNakitToplam\":0,\"kalanNakitHakki\":" + had + ",\"islemTutariKuraliDevrede\":false}";
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — müşteri "Değiştir"de, ödeme satırda, Kaydet #yildiz-tezgah-kaydet.
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, e.YildizKalemSayisi()), Bekle);
        return e;
    }

    [Fact]
    public void Kontrol_had_9999_99_gercek_musteri_NAKIT_fis_10000_KILITLI()
    {
        var e = Fis("9999.99");
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("NAKIT");   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(e.GbYildizKaydet().HasAttribute("disabled")), Bekle);
    }

    [Fact]
    public void Esitlik_had_10000_gercek_musteri_NAKIT_fis_TAM_10000_KILITLENMEZ_asan_degil()
    {
        var e = Fis("10000");
        e.GbYildizMusteriSec("c1");
        Assert.Contains("Nakit", e.Find("#yildiz-odeme-0").TextContent);   // tam hadde ödeme nakitte kalır (aşan değil)
        e.WaitForAssertion(() => Assert.False(e.GbYildizKaydet().HasAttribute("disabled"),
            e.GbYildizKaydet().GetAttribute("title")), Bekle);
    }

    [Fact]
    public void Esitlik_had_10000_Pesin_Musteri_fis_TAM_10000_KILITLENMEZ_sunucu_olcut_buyuktur_Had()
    {
        var e = Fis("10000");
        Assert.Contains("Nakit", e.Find("#yildiz-odeme-0").TextContent);
        e.WaitForAssertion(() => Assert.False(e.GbYildizKaydet().HasAttribute("disabled"),
            e.GbYildizKaydet().GetAttribute("title")), Bekle);
    }
}

// ═══ Doğrulayıcı tur 12 (29.09, hasım — kör inceleme): TevsikOneri (> had), sunucu metni TL→₺, biçim sınırları ═══
public class SATIS3SinirTestleriTur12
{
    private static string Duz(string s) => new(s.Where(c => !char.IsWhiteSpace(c) && c != (char)0x202F && c != (char)0x00A0).ToArray());

    [Theory]
    [InlineData("NAKIT", "SATIS", "0", "30000", false)]              // sıfır
    [InlineData("NAKIT", "SATIS", "-50000", "30000", false)]         // eksi: öneri doğmaz
    [InlineData("NAKIT", "SATIS", "30000", "30000", false)]          // tam had: "aşan" değil
    [InlineData("NAKIT", "SATIS", "30000.01", "30000", true)]        // 1 kuruş aşım
    [InlineData("NAKIT", "ALIS", "999999999.99", "30000", true)]     // çok büyük
    [InlineData("NAKIT", "HURDA_ALIS", "30000.005", "30000", true)]  // kuruş altı aşım da aşımdır (sunucu da > Had)
    [InlineData("NAKIT", "SATIS", "90000", "0", false)]              // had bilinmiyor/0: istemci kilitlemez
    [InlineData("NAKIT", "SATIS", "90000", "-1", false)]
    [InlineData("KART", "SATIS", "90000", "30000", false)]
    [InlineData("NAKIT", "SATIS_IADE", "90000", "30000", false)]
    public void Tur12_TevsikOneri_sinirlari(string odeme, string islem, string toplam, string had, bool beklenen)
        => Assert.Equal(beklenen, TevsikOneri.Gerekli(odeme, islem,
            decimal.Parse(toplam, System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(had, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Tur12_TevsikOneri_null_girdiler_false()
    {
        Assert.False(TevsikOneri.Gerekli(null, "SATIS", 90_000m, 30_000m));
        Assert.False(TevsikOneri.Gerekli("NAKIT", null, 90_000m, 30_000m));
    }

    [Fact]
    public void Tur12_TlSembolu_cok_buyuk_ve_kurus_tutarlar()
    {
        Assert.Equal("Fark999.999.999,99₺.", Duz(Kuyumcu.Web.Pages.Islemler.TlSembolu("Fark 999.999.999,99 TL.")));
        Assert.Equal("Eşik30.000₺", Duz(Kuyumcu.Web.Pages.Islemler.TlSembolu("Eşik 30.000,00 TL")));
        Assert.Equal("0,01₺", Duz(Kuyumcu.Web.Pages.Islemler.TlSembolu("0,01 TL")));
        Assert.DoesNotContain("TL", Kuyumcu.Web.Pages.Islemler.TlSembolu("A 1.234,56 TL ve B 7,00 TL"));
        Assert.Equal("", Kuyumcu.Web.Pages.Islemler.TlSembolu(""));
        Assert.Equal("TLkodu", Kuyumcu.Web.Pages.Islemler.TlSembolu("TLkodu"));   // sayısız "TL" dokunulmaz
    }

    [Fact]
    public void Tur12_Bicim_kurus_yuvarlama_AwayFromZero_ve_has_4_hane()
    {
        Assert.Equal("0,01₺", Duz(Bicim.Tl(0.005m)));
        Assert.Equal("1,01₺", Duz(Bicim.Tl(1.005m)));
        Assert.Equal("999.999.999,99₺", Duz(Bicim.Tl(999_999_999.99m)));
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("0,0000 has", Bicim.Has(0.00004m));
    }

    [Theory]
    [InlineData("45.000", "45000", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1,5", "1.5", FormAlani.SayiSonucu.Tamam)]
    [InlineData("999.999.999,99", "999999999.99", FormAlani.SayiSonucu.Tamam)]
    [InlineData("0,0001", "0.0001", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1.500", null, FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("", null, FormAlani.SayiSonucu.Bos)]
    [InlineData("abc", null, FormAlani.SayiSonucu.Gecersiz)]
    public void Tur12_Turkce_giris_SayiCoz(string ham, string? beklenen, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Equal(beklenen is null ? null : decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}

// ═══ Doğrulayıcı tur 13 (29.09, hasım — kör inceleme): PRO "Yasal kapılar" nakit satırı sınırları + t13 sekme dönüşü ═══
// Beklenen metinler SABİT ("Uygun"/"Aşıldı"/"Bilinmiyor"); ölçüt sunucunun TevsikController'ı: kalan = max(0, had − bugünkü).
public sealed class SATIS3SinirTestleriTur13 : BunitContext
{
    private string tevsikJson = "";
    private bool tevsikHata;

    public SATIS3SinirTestleriTur13()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriTur13 t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.Contains("/tevsik/gunluk") && t.tevsikHata)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"hata":"tevsik çöktü"}""", System.Text.Encoding.UTF8, "application/json") });
            const string u1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => t.tevsikJson,
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende Varsayilan","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $"[{u1}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);

    private static string Json(string had, string kalan, bool kumulatif, bool kural) =>
        "{\"had\":" + had + ",\"kuralEtkin\":true,\"kumulatifHesaplandi\":" + (kumulatif ? "true" : "false")
        + ",\"bugunkuNakitToplam\":0,\"kalanNakitHakki\":" + kalan + ",\"islemTutariKuraliDevrede\":" + (kural ? "true" : "false") + "}";

    // TEMA TEMİZLİĞİ 17.10: PRO yan sütunun "Yasal kapılar" nakit satırı (#islem-pro-nakit — Uygun/Aşıldı/Bilinmiyor) SATIŞ'ta
    // artık çizilmez (satış hep Yıldız tezgâhı). Aynı nakit durumu Yıldız'da Fark bloğunun not satırında (#yildiz-tezgah-not)
    // söylenir: "Yasal kurallar uygun · nakit <had> sınırının altında" (Islemler.YildizNot). Sınırlar o satırda sınanır:
    // "Uygun" = satır uygun der; "Aşıldı"/"Bilinmiyor" = satır uygun DEMEZ (uydurma yok).

    /// <summary>PRO satış, gerçek müşteri (c1 — kümülatif ölçülür), 10.000 ₺'lik tek kalem (Yıldız tezgâhı).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> ProFis(string json)
    {
        tevsikJson = json;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        e.GbYildizMusteriSec("c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, e.YildizKalemSayisi()), Bekle);
        return e;
    }

    private static string Uygun => Kuyumcu.Core.I18n.YildizTezgahSozlugu.YasalUygun("tr");

    /// <summary>Not satırı (yoksa boş) — önce Yıldız Fark bloğunun çizildiği doğrulanır (boş DOM'da yanlış yeşil yok).</summary>
    private static string Not(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Assert.Single(e.FindAll("#yildiz-tezgah-fark"));
        return e.FindAll("#yildiz-tezgah-not").FirstOrDefault()?.TextContent.Trim() ?? "";
    }

    private static void UygunDer(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.WaitForAssertion(() => Assert.StartsWith(Uygun, Not(e)), Bekle);

    private static void UygunDemez(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Thread.Sleep(300);
        e.WaitForAssertion(() => Assert.DoesNotContain(Uygun, Not(e)), Bekle);
    }

    [Fact]
    public void Tur13_esitlik_fis_10000_kalan_10000_UYGUN_asan_degil()
    {
        var e = ProFis(Json("30000", "10000", true, false));
        UygunDer(e);
    }

    [Fact]
    public void Tur13_bir_kurus_asim_fis_10000_kalan_9999_99_ASILDI()
    {
        var e = ProFis(Json("30000", "9999.99", true, false));
        UygunDemez(e);
    }

    [Fact]
    public void Tur13_cok_buyuk_kalan_999999999_99_UYGUN()
    {
        var e = ProFis(Json("999999999.99", "999999999.99", true, false));
        UygunDer(e);
    }

    [Fact]
    public void Tur13_kumulatif_hesaplanmadiysa_kalan_0_ASILDI_DEMEZ()
    {
        var e = ProFis(Json("30000", "0", false, false));
        UygunDer(e);
    }

    [Fact]
    public void Tur13_sekme_donusu_bos_fis_kalan_SIFIR_ASILDI_DEMEZ()
    {
        // Günün hakkı tükenmiş (kalan 0) ama fiş boş: 0 > 0 değil → "Aşıldı" değil.
        var e = ProFis(Json("30000", "0", true, true));
        UygunDemez(e);   // sunucu nakit kuralını devrede sayıyor ("Aşıldı")
        tevsikJson = Json("30000", "0", true, false);
        e.Find("#yildiz-tur-HURDA_ALIS").Click();
        e.WaitForElement("#yildiz-tur-SATIS", Bekle).Click();
        e.WaitForElement("#yildiz-tezgah", Bekle);
        UygunDer(e);
    }

    [Fact]
    public void Tur13_sekme_donusu_SUNUCU_HATASINDA_onceki_ASILDI_tasinmaz_BILINMIYOR()
    {
        var e = ProFis(Json("5000", "5000", true, true));
        e.YildizOdemeSec("NAKIT");   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        UygunDemez(e);
        tevsikHata = true;
        e.Find("#yildiz-tur-HURDA_ALIS").Click();
        e.WaitForElement("#yildiz-tur-SATIS", Bekle).Click();
        e.WaitForElement("#yildiz-tezgah", Bekle);
        UygunDemez(e);
    }

    [Theory]
    [InlineData("45.000", "45000", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1,5", "1.5", FormAlani.SayiSonucu.Tamam)]
    [InlineData("0,0001", "0.0001", FormAlani.SayiSonucu.Tamam)]
    [InlineData("999.999.999,99", "999999999.99", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1.500", null, FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("", null, FormAlani.SayiSonucu.Bos)]
    public void Tur13_Turkce_giris_SayiCoz(string ham, string? beklenen, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Equal(beklenen is null ? null : decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}

// ═══ Doğrulayıcı tur 14 (29.09, hasım — kör inceleme): t14 PRO arama listesi satırı ═══
// Liste satırı "ad · ayar · gram · fiyat" basar (FisKalemGirisi: Bicim.Gram + Bicim.Tl). Beklenenler SABİT metin;
// yuvarlama kuralı projede AwayFromZero (Bicim.Tl gövdesi). Gram 3 hane, grupsuz (BicimTestleri: "10250,000 g").
public sealed class SATIS3SinirTestleriTur14
{
    private static string Duz(string s) => new(s.Where(c => !char.IsWhiteSpace(c) && c != (char)0x202F && c != (char)0x00A0).ToArray());

    [Fact]
    public void Tur14_sifir_gram_ve_sifir_tutar()
    {
        Assert.Equal("0,000 g", Bicim.Gram(0m));
        Assert.Equal("0₺", Duz(Bicim.Tl(0m)));
    }

    [Fact]
    public void Tur14_eksi_kurusa_yuvarlaninca_sifir_ISARETSIZ_eksi_gram_isaretli()
    {
        Assert.Equal("0₺", Duz(Bicim.Tl(-0.004m)));
        Assert.StartsWith("-", Bicim.Gram(-1.5m));
        Assert.Equal("-1,500 g", Bicim.Gram(-1.5m));
    }

    [Fact]
    public void Tur14_kurus_0_005_siniri_AwayFromZero_ToEven_DEGIL()
    {
        Assert.Equal("2,35₺", Duz(Bicim.Tl(2.345m)));    // ToEven olsaydı 2,34
        Assert.Equal("2,34₺", Duz(Bicim.Tl(2.3449m)));
        Assert.Equal("0,001 g", Bicim.Gram(0.0005m));    // gram 3 hane, yarım yukarı
    }

    [Fact]
    public void Tur14_has_4_hane_0_0001()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("0,0001 has", Bicim.Has(0.00005m));
    }

    [Fact]
    public void Tur14_cok_buyuk_999_999_999_99_ve_buyuk_gram()
    {
        Assert.Equal("999.999.999,99₺", Duz(Bicim.Tl(999_999_999.99m)));
        Assert.Equal("999999,999 g", Bicim.Gram(999_999.999m));
    }

    [Theory]
    [InlineData("10,000", "10", FormAlani.SayiSonucu.Tamam)]
    [InlineData("18,35", "18.35", FormAlani.SayiSonucu.Tamam)]
    [InlineData("45.000", "45000", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1.500", null, FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("  ", null, FormAlani.SayiSonucu.Bos)]
    public void Tur14_Turkce_giris_SayiCoz(string ham, string? beklenen, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Equal(beklenen is null ? null : decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}

// ════════════════════════════════════════════════════════════════════════
// SATIS-3 k2 (tasarım hakemi K19 + eski altın satıcı kimliği) — HASIM DOĞRULAYICI
// SINIR TESTLERİ (29.09). Beklenen değerler SABİT; üretim kodundan hesaplanmaz.
// Yeni para yolları: FisFark.Gonderilecek (0 satır gövdeye girmez), FisHesap.OtoDengele
// (hedef = TUTARI OLAN son elle yazılmamış TL satırı), FisFark.YeniBacakAraci(nakitsiz),
// AltinBacagiGirisi.MusteridenDoldur (TCKN ≠ VKN).
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriK2
{
    private static FisBacagi B(string arac, decimal tl, bool elle = false) =>
        new() { Arac = arac, TutarTl = tl, ElleYazildi = elle };

    [Fact]
    public void K2_sifir_iki_bos_satir_GONDERILMEZ_otodengele_sifir_toplamda_DEGISMEZ()
    {
        var kart = B("KART", 0m); var havale = B("HAVALE", 0m);
        var l = new List<FisBacagi> { kart, havale };
        Assert.Empty(FisFark.Gonderilecek(l));          // sayfa bunu "Ödeme tutarını girin" kilidine çevirir
        Assert.False(FisHesap.OtoDengele(l, 0m));
        Assert.Equal(0m, kart.TutarTl); Assert.Equal(0m, havale.TutarTl);
    }

    [Fact]
    public void K2_eksi_tutar_SESSIZCE_DUSMEZ_gonderilecekte_kalir_ve_hedef_eksiye_inmez()
    {
        var eksi = B("HAVALE", -5m, elle: true);
        Assert.Single(FisFark.Gonderilecek([eksi]));   // 0 değil → düşürülmez; sayfanın >0 kapısı kilitler
        var kart = B("KART", 10_000m); var bos = B("HAVALE", 0m);
        var altin = B(FisAltinBacagi.Arac, 12_000m);
        var l = new List<FisBacagi> { kart, bos, altin };
        FisHesap.OtoDengele(l, 10_000m);
        Assert.Equal(0m, kart.TutarTl);                 // eksiye düşmez; fark FAZLA
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(FisFark.Hesapla(10_000m, l)));
    }

    [Fact]
    public void K2_kurus_kalan_127947_77_eksi_altin_64846_76_KART_63101_01()
    {
        var kart = B("KART", 127_947.77m); var bos = B("HAVALE", 0m);
        var altin = B(FisAltinBacagi.Arac, 64_846.76m);
        var l = new List<FisBacagi> { kart, bos, altin };
        Assert.True(FisHesap.OtoDengele(l, 127_947.77m));
        Assert.Equal(63_101.01m, kart.TutarTl);
        Assert.Equal(0m, bos.TutarTl);
        Assert.Equal(0m, FisFark.Hesapla(127_947.77m, l));
        Assert.Equal(0.01m, FisHesap.Yuvarla2(0.005m));       // AwayFromZero, ToEven değil
        Assert.Equal(-0.01m, FisHesap.Yuvarla2(-0.005m));
    }

    [Fact]
    public void K2_has_4_hane_0_0001_gram_K24_has_0_0001_ve_brut_100()
    {
        var d = Kuyumcu.Core.Domain.AltinBacagiHesap.Hesapla(0.0001m, Kuyumcu.Core.Domain.Ayarlar.Altin["K24"], 1_000_000m, 0m);
        Assert.Equal(0.0001m, d.HasGrami);
        Assert.Equal(100.00m, d.NetTl);
        // 0 TL'lik ALTIN satırı da gövdeye girer (kendi kilidi var) — sessizce düşmez.
        Assert.Single(FisFark.Gonderilecek([B(FisAltinBacagi.Arac, 0m)]));
    }

    [Fact]
    public void K2_cok_buyuk_999_999_999_99_altin_bir_kurus_kart_999_999_999_98()
    {
        var kart = B("KART", 999_999_999.99m); var bos = B("HAVALE", 0m);
        var altin = B(FisAltinBacagi.Arac, 0.01m);
        var l = new List<FisBacagi> { kart, bos, altin };
        FisHesap.OtoDengele(l, 999_999_999.99m);
        Assert.Equal(999_999_999.98m, kart.TutarTl);
        Assert.Equal(2, FisFark.Gonderilecek(l).Count);
    }

    [Fact]
    public void K2_bos_null_musteri_yoksa_dolmaz_VKN_TCKN_kutusuna_girmez_elle_yazilan_korunur()
    {
        var g = new AltinBacagiGirisi();
        Assert.False(g.MusteridenDoldur(null, null, null, null, null));   // müşteri yok → değişiklik yok
        Assert.Null(g.AdSoyad);
        // Şirket: 10 haneli VKN → TCKN kutusu BOŞ kalır.
        Assert.True(g.MusteridenDoldur("c1", "Anadolu Holding A.Ş.", "1234567890", "VKN", "Ulus, Ankara"));
        Assert.Equal("Anadolu Holding A.Ş.", g.AdSoyad);
        Assert.Null(g.Tckn); Assert.False(g.TcknCariden);
        // 11 hane ama tür VKN beyanlı → yine TCKN değil.
        var g2 = new AltinBacagiGirisi();
        g2.MusteridenDoldur("c2", "X", "12345678901", "VKN", null);
        Assert.Null(g2.Tckn);
        // Elle yazılan ad başka müşteriye geçince EZİLMEZ.
        var g3 = new AltinBacagiGirisi { AdSoyad = "Elle Yazılan" };
        g3.MusteridenDoldur("c3", "Başka Müşteri", "10000000146", "TCKN", null);
        Assert.Equal("Elle Yazılan", g3.AdSoyad);
        Assert.Equal("10000000146", g3.Tckn);
        // Peşin'e dönülünce müşteriden gelen silinir, elle yazılan kalır.
        g3.MusteridenDoldur(null, null, null, null, null);
        Assert.Equal("Elle Yazılan", g3.AdSoyad);
        Assert.Null(g3.Tckn);
    }

    [Fact]
    public void K2_nakitsiz_fiste_yeni_satir_ASLA_NAKIT_degil_uc_arac_doluyken_KART()
    {
        Assert.Equal("KART", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m), B("HAVALE", 1m)], nakitsiz: true));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m), B("HAVALE", 1m)], nakitsiz: false));
    }

    [Theory]
    [InlineData("127.947,77", "127947.77", FormAlani.SayiSonucu.Tamam)]
    [InlineData("45.000", "45000", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1,5", "1.5", FormAlani.SayiSonucu.Tamam)]
    [InlineData("1.500", null, FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("", null, FormAlani.SayiSonucu.Bos)]
    public void K2_Turkce_giris_SayiCoz(string ham, string? beklenen, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Equal(beklenen is null ? null : decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}

// ═══ Doğrulayıcı t15 (29.09, hasım — kör inceleme): TAKAS farkı nakit kapısı ════════════════════════════════
// SATIS-3 t15 takas farkını istemcide tevsik kapısına sokar (TevsikTutar = |Fark|, işlem türü SATIS gibi) ve farkın
// ödeme türünü (TakasDto.OdemeTuru: NAKIT|KART|HAVALE) gövdeye yazar. Sunucu (SatisModulu.Takas): NAKIT'te
// tevsik.Dogrula(|Yuvarla2(yeni − eski)|), kural "olcut > Had" (tam had serbest). Beklenen değerler SABİT sayılardır:
// ürün u1 = 10.000 ₺ · eski 1 g 22 ayar (milyem 916 → has 0,916) · has fiyatı elle.
public sealed class SATIS3SinirTestleriT15 : BunitContext
{
    private string tevsikJson = "";
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public SATIS3SinirTestleriT15()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriT15 t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            const string u1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => t.tevsikJson,
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende Varsayilan","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $"[{u1}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş.","vergiNo":"10000000146","kimlikTuru":"TCKN","adres":"Ulus, Ankara"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":2000000000,"amacBeyaniEsigi":2000000000,"beyanFormuEsigi":2000000000,"ekBelgeEsigi":2000000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/islemler/takas") => """{"eskiHasGrami":0.916,"yeniHasGrami":3.94,"farkTl":1}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private int TakasPostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")); }
    private string SonTakasGovde() { lock (istekler) return istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")).Govde; }

    /// <summary>Takas formu: 1 g 22 ayar eski + u1 yeni + gerçek müşteri. had/eskiFiyat metin (Türkçe giriş).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Takas(string had, string eskiFiyat, string mod = ArayuzModuServisi.Basit, string gram = "1")
    {
        tevsikJson = "{\"had\":" + had + ",\"kuralEtkin\":true,\"kumulatifHesaplandi\":true,\"bugunkuNakitToplam\":0,\"kalanNakitHakki\":" + had + ",\"islemTutariKuraliDevrede\":false}";
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — Takas'a tezgâhın "Diğer" listesinden geçilir (tür yüzü eski form).
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        e.YildizTurGec("TAKAS");
        e.WaitForElement("#takas-eski-gram");
        e.Find("#islem-barkod").Focus();
        foreach (var q in new[] { "b", "bi", "bil" }) e.Find("#islem-barkod").Input(q);
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-0").MouseDown());
        e.WaitForElement("#takas-yeni-fiyat", Bekle);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        ParaDemoEkranYardim.TakasSaticiDoldur(e);   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
        e.Find("#takas-eski-gram").Input(gram);
        e.Find("#takas-eski-fiyat").Input(eskiFiyat);
        e.Find("#takas-eski-fiyat").Blur();
        return e;
    }

    private static bool Kilitli(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.Find("#islem-kaydet").HasAttribute("disabled");

    // Kontrol: fark = 10.000 − 0,916 × 5.000 = 5.420,00 · had 5.419,99 → NAKİT kilitli (aşan)
    [Fact]
    public void T15S_kontrol_fark_5420_had_5419_99_NAKIT_KILITLI()
    {
        var e = Takas("5419.99", "5000");
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(Kilitli(e)), Bekle);
        Assert.Equal(0, TakasPostSayisi());
    }

    // Eşitlik: fark TAM had (5.420,00) → sunucu "olcut > Had" kabul eder → istemci kilitlemez
    [Fact]
    public void T15S_esitlik_fark_TAM_had_5420_NAKIT_KILITLENMEZ_govdede_NAKIT()
    {
        var e = Takas("5420", "5000");
        e.WaitForAssertion(() => Assert.False(Kilitli(e), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, TakasPostSayisi()), Bekle);
        var g = System.Text.Json.JsonDocument.Parse(SonTakasGovde()).RootElement;
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        Assert.Equal(5000m, g.GetProperty("eskiUrunBirimFiyatTlHasGram").GetDecimal());
    }

    // Eksi fark (dükkân müşteriye öder): 10.000 − 0,916 × 8 g × 6.800 = −39.830,40 · |fark| > had 5.000 → NAKİT kilitli,
    // kilit sebebi ÖDEME yönünde ("ödenemez"), "alınamaz" değil. Had 40.000 iken |−39.830,40| < had → serbest.
    // ISLEM-BANT-2: eski "1 g × 50.000" has fiyatı işlem bandının (6.800 → 3.400–7.480) dışındaydı ve Kaydet artık
    // bant dışında kilitli; aynı yön ve aynı had ilişkisi bant içi fiyat + 8 g ile kuruldu.
    [Fact]
    public void T15S_eksi_fark_mutlak_deger_hadden_buyuk_NAKIT_KILITLI_odeme_yonunde()
    {
        var e = Takas("5000", "6800", gram: "8");
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(Kilitli(e)), Bekle);
        Assert.Equal(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.NakitOdenemez, e.Find("#islem-kaydet").GetAttribute("title"));
        Assert.Equal(0, TakasPostSayisi());
    }

    [Fact]
    public void T15S_eksi_fark_mutlak_deger_hadden_kucuk_NAKIT_serbest()
    {
        var e = Takas("40000", "6800", gram: "8");
        e.WaitForAssertion(() => Assert.False(Kilitli(e), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
    }

    // Sıfır: has fiyatı 0 → fark hesaplanmaz (null) → gönderilmez; tevsik kilidi değil "eksik alan" kilidi
    [Fact]
    public void T15S_sifir_has_fiyati_POST_YOK()
    {
        var e = Takas("5000", "0");
        e.WaitForAssertion(() => Assert.True(Kilitli(e)), Bekle);
        Assert.NotEqual(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.NakitAlinamaz, e.Find("#islem-kaydet").GetAttribute("title"));
        Assert.Equal(0, TakasPostSayisi());
    }

    // Eksi gram reddedilir
    [Fact]
    public void T15S_eksi_gram_POST_YOK()
    {
        var e = Takas("5000", "5000", gram: "-1");
        e.WaitForAssertion(() => Assert.True(Kilitli(e)), Bekle);
        Assert.Equal(0, TakasPostSayisi());
    }

    // Çok büyük: has fiyatı 999.999.999,99 → ISLEM-BANT-2 (kasıtlı davranış değişikliği): mutlak sınır (has 500–50.000)
    // dışı — sunucu her durumda 400 verir (ISLEM-BANT IB10), ekran artık KART seçilse bile Kaydet'i alanın cümlesiyle
    // kilitler ve gövde HİÇ gitmez. (Eski beklenti "KART'la gövdeye birebir gider" sunucunun reddedeceği bir istekti.)
    [Fact]
    public void T15S_cok_buyuk_has_fiyati_NAKIT_kilitli_KART_govdede_birebir()
    {
        var e = Takas("30000", "999.999.999,99");
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(Kilitli(e)), Bekle);
        if (e.FindAll("#islem-odeme").Count > 0) SeciciYardim.Sec(e, "islem-odeme", "KART");
        var mutlak = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr");
        e.WaitForAssertion(() => Assert.Equal(mutlak, e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        Assert.True(Kilitli(e));
        e.Find("#islem-kaydet").Click();
        Assert.Equal(0, TakasPostSayisi());
    }

    // Türkçe giriş: "5.000" = 5.000 → fark 5.420 = had → serbest; "1.500" belirsiz → gönderilmez
    [Fact]
    public void T15S_Turkce_giris_binlik_nokta_5_000_kabul()
    {
        var e = Takas("5420", "5.000");
        e.WaitForAssertion(() => Assert.False(Kilitli(e), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
    }

    [Fact]
    public void T15S_Turkce_giris_belirsiz_1_500_POST_YOK()
    {
        var e = Takas("30000", "1.500");
        System.Threading.Thread.Sleep(300);
        // "1.500" belirsizdir (1,5 mi 1.500 mü?): ya Kaydet kilitli ya da tıklanınca POST gitmez ve alan altında mesaj çıkar.
        if (!Kilitli(e)) e.Find("#islem-kaydet").Click();
        System.Threading.Thread.Sleep(500);
        Assert.True(TakasPostSayisi() == 0, "BELİRSİZ GİRDİ GÖNDERİLDİ: ekranda '" + e.Find("#takas-eski-fiyat").GetAttribute("value")
            + "' yazarken gövde = " + (TakasPostSayisi() > 0 ? SonTakasGovde() : "-"));
        // t16 (uygulayıcı): belirsizlik mesajının KANONİK metni SayiGirisiSozlugu'ndadır ("Ondalık için virgül kullanın…",
        // P78SayiGirisiTestleri sabitler; "belirsiz" kelimesi içermez). Doğrulayıcının amacı — alan altında mesaj — korunur.
        Assert.Contains(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#takas-eski-fiyat-sar").TextContent, StringComparison.Ordinal);
        Assert.Equal("true", e.Find("#takas-eski-fiyat").GetAttribute("aria-invalid"));
        Assert.Equal("1.500", e.Find("#takas-eski-fiyat").GetAttribute("value"));
    }

    // Has 4 hane / 6 hane: 0,0001 g 22 ayar → has 0,000092 (6 hane, sunucu HasHesap.ToHasGram ile aynı)
    [Fact]
    public void T15S_has_kucuk_gram_TakasGirisi_EskiHas_6_hane()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 0.0001m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Equal(0.000092m, t.EskiHas);
        Assert.Equal(10000m - 0.46m, t.Fark);
    }

    // Kuruş sınırı: yeni fiyat 10.000,004 → ham fark 5.420,004; sunucu Yuvarla2 (AwayFromZero) → 5.420,00 = had → KABUL.
    // İstemci ham farkla (> had) KİLİTLERSE sunucunun kabul ettiği işlem ekranda engellenir.
    [Fact]
    public void T15S_kurus_siniri_ham_fark_5420_004_had_5420_sunucu_kabul_eder()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000.004m };
        var sunucuFarki = Math.Round(t.YeniDeger - t.EskiDeger, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(5420.00m, sunucuFarki);
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", sunucuFarki, 5420m));
        Assert.Equal(5420.004m, t.Fark);   // istemci ham farkı — kapıya bu gider (bkz. rapor bulgusu)
    }

    // Boş/null: yeni fiyat yok → fark null
    [Fact]
    public void T15S_bos_yeni_fiyat_Fark_null()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = null };
        Assert.Null(t.Fark);
        Assert.False(t.Tamam);
    }

    // ── Doğrulayıcı t16 (29.09, hasım — kör inceleme): t16 "görünen ≠ giden" düzeltmesi YALNIZ has gram fiyatı
    //    kutularına (GecersizdeBosalt) uygulandı. Aynı sınıftaki diğer sayısal kutular: kutu "1.500" + kırmızı hata
    //    gösterirken ÖNCEKİ değer (teklif fiyatı / ara tuşlardan kalan 1,5 g) gövdeye gidiyor mu? Beklenen SABİT:
    //    Kaydet KİLİTLİ ve POST YOK.
    [Fact]
    public void T16D_takas_YENI_fiyat_belirsiz_1_500_iken_teklif_fiyati_POST_EDILMEZ()
    {
        var e = Takas("99999999", "5000");
        e.WaitForAssertion(() => Assert.False(Kilitli(e), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#takas-yeni-fiyat").Input("1.500");
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#takas-yeni-fiyat").GetAttribute("aria-invalid")), Bekle);
        e.Find("#islem-kaydet").Click();
        Thread.Sleep(300);
        Assert.True(TakasPostSayisi() == 0, TakasPostSayisi() == 0 ? "" : SonTakasGovde());   // kutu hata derken 10.000 ₺ gitmemeli
        Assert.True(Kilitli(e));
    }

    [Fact]
    public void T16D_takas_ESKI_gram_tus_tus_1_500_iken_1_5_gram_POST_EDILMEZ()
    {
        var e = Takas("99999999", "5000");
        e.WaitForAssertion(() => Assert.False(Kilitli(e), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        foreach (var q in new[] { "1", "1.", "1.5", "1.50", "1.500" }) e.Find("#takas-eski-gram").Input(q);
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#takas-eski-gram").GetAttribute("aria-invalid")), Bekle);
        e.Find("#islem-kaydet").Click();
        Thread.Sleep(300);
        Assert.True(TakasPostSayisi() == 0, TakasPostSayisi() == 0 ? "" : SonTakasGovde());   // kutu "1.500" + hata derken 1,5 g gitmemeli
        Assert.True(Kilitli(e));
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 t15 — HASIM DOĞRULAYICI (29.09, ikinci koşum) SINIR TESTLERİ
// Beklenen değerler SABİT (üretim kodundan hesaplanmaz). Canlı bulgu (5162, başsız Chrome):
// Takas "Has Gram Fiyatı"na "1.500" yapıştırıldı → alan altında "Ondalık için virgül kullanın" yazarken
// Kaydet ETKİN kaldı; ~6 sn sonra alan kurla (6.564,43) sessizce değişti ve POST gövdesi 6564.43 gitti.
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriHasimT15 : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public SATIS3SinirTestleriHasimT15()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriHasimT15 t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            const string u1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $"[{u1}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş.","vergiNo":"10000000146","kimlikTuru":"TCKN","adres":"Ulus, Ankara"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":2000000000,"amacBeyaniEsigi":2000000000,"beyanFormuEsigi":2000000000,"ekBelgeEsigi":2000000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/islemler/takas") => """{"eskiHasGrami":0.916,"yeniHasGrami":3.94,"farkTl":1}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private int TakasPost() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")); }

    // ── Türkçe giriş (FormAlani.SayiCoz) — P-78 kuralı ──
    [Theory]
    [InlineData("45.000", 45000.0, FormAlani.SayiSonucu.Tamam)]
    [InlineData("1,5", 1.5, FormAlani.SayiSonucu.Tamam)]
    [InlineData("0,005", 0.005, FormAlani.SayiSonucu.Tamam)]
    [InlineData("0", 0.0, FormAlani.SayiSonucu.Tamam)]
    [InlineData("-5", -5.0, FormAlani.SayiSonucu.Tamam)]
    public void H15_SayiCoz_Turkce_giris_kabul(string ham, double beklenen, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void H15_SayiCoz_cok_buyuk_999_999_999_99_birebir()
    {
        var c = FormAlani.SayiCoz("999.999.999,99");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(999999999.99m, c.Deger);
    }

    [Theory]
    [InlineData("1.500", FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("", FormAlani.SayiSonucu.Bos)]
    [InlineData(null, FormAlani.SayiSonucu.Bos)]
    public void H15_SayiCoz_belirsiz_ve_bos_deger_uretmez(string? ham, FormAlani.SayiSonucu sonuc)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── Takas modeli: sıfır · eksi · has 6 hane · çok büyük ──
    [Fact]
    public void H15_Takas_sifir_gram_fark_yok_tamam_degil()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 0m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Null(t.Fark);
        Assert.False(t.Tamam);
    }

    [Fact]
    public void H15_Takas_eksi_gram_tamam_degil_fark_yok()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = -1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Null(t.Fark);
        Assert.False(t.Tamam);
    }

    [Fact]
    public void H15_Takas_has_1_2345_g_22_ayar_1_130802_has()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1.2345m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000m };
        Assert.Equal(1.130802m, t.EskiHas);            // 1,2345 × 916 / 1000
        Assert.Equal(10000m - 5654.01m, t.Fark);       // 1,130802 × 5000 = 5654,01
    }

    [Fact]
    public void H15_Takas_cok_buyuk_yeni_fiyat_fark_birebir()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 999999999.99m };
        Assert.Equal(999995419.99m, t.Fark);           // 999.999.999,99 − 4.580,00
    }

    // ── Kuruş sınırı: istemci sunucunun reddettiğini ASLA serbest bırakmaz (fail-closed yön) ──
    // Sunucu Yuvarla2(fark) > had ise reddeder (AwayFromZero). ham fark = had + 0,005 → sunucu had+0,01 → RED.
    [Fact]
    public void H15_kurus_siniri_had_arti_0_005_istemci_de_kilitler()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 10000.005m };
        Assert.Equal(5420.005m, t.Fark);
        Assert.True(TevsikOneri.Gerekli("NAKIT", "SATIS", Math.Abs(t.Fark!.Value), 5420m));
        // had − 0,004: sunucu 5419,996 → 5420,00 = had → kabul; istemci de serbest
        var u = new TakasGirisi { EskiAyar = "K22", EskiGram = 1m, EskiFiyat = 5000m, YeniUrunId = "u1", YeniFiyat = 9999.996m };
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", Math.Abs(u.Fark!.Value), 5420m));
    }

    // ── EKRAN: belirsiz girdi alan altında hata gösterirken Kaydet POST ATMAMALI (görünen ≠ giden) ──
    [Fact]
    public void H15_Takas_belirsiz_1_500_hata_gorunurken_Kaydet_POST_atmaz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, ArayuzModuServisi.Profesyonel));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — Takas'a tezgâhın "Diğer" listesinden geçilir (tür yüzü eski form).
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        e.YildizTurGec("TAKAS");
        e.WaitForElement("#takas-eski-gram");
        e.Find("#islem-barkod").Focus();
        foreach (var q in new[] { "b", "bi", "bil" }) e.Find("#islem-barkod").Input(q);
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-0").MouseDown());
        e.WaitForElement("#takas-yeni-fiyat", Bekle);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        ParaDemoEkranYardim.TakasSaticiDoldur(e);   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
        e.Find("#takas-eski-gram").Input("1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#takas-eski-fiyat").Input("1.500");
        // alan altında belirsizlik mesajı görünür (bileşen kuralı)
        Assert.Equal("true", e.Find("#takas-eski-fiyat").GetAttribute("aria-invalid"));
        if (!e.Find("#islem-kaydet").HasAttribute("disabled")) e.Find("#islem-kaydet").Click();
        System.Threading.Thread.Sleep(500);
        Assert.True(TakasPost() == 0, "Ekranda '1.500' + hata varken takas POST'u gitti: " +
            string.Join(" | ", istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")).Select(i => i.Govde)));
    }
}

// ════════════════════════════════════════════════════════════════════════
// SATIS-3 t17 — HASIM DOĞRULAYICI (29.09): GecersizAlanKapisi + SayisalAlan sınırları.
// Kapı basamaklanmış bir Para/Gram kutusuna tr-TR girdi verilir; çağırana giden
// değer ve kapı kaydı SABİT beklenen değerlerle ölçülür (üretim kodundan hesaplanmaz).
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriT17Kapi : BunitContext
{
    public SATIS3SinirTestleriT17Kapi()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private (IRenderedComponent<Microsoft.AspNetCore.Components.CascadingValue<GecersizAlanKapisi>> c, GecersizAlanKapisi k, List<decimal?> giden)
        ParaKutusu(decimal? baslangic = 7m, bool gram = false)
    {
        var k = new GecersizAlanKapisi();
        var giden = new List<decimal?>();
        var c = Render<Microsoft.AspNetCore.Components.CascadingValue<GecersizAlanKapisi>>(p => p
            .Add(x => x.Value, k).Add(x => x.IsFixed, true)
            .AddChildContent(b =>
            {
                if (gram)
                {
                    b.OpenComponent<Gram>(0);
                }
                else
                {
                    b.OpenComponent<Para>(0);
                }
                b.AddAttribute(1, "ElemanId", "t17-k");
                b.AddAttribute(2, "Etiket", "Tutar");
                b.AddAttribute(3, "Deger", baslangic);
                b.AddAttribute(4, "DegerChanged", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<decimal?>(this, v => giden.Add(v)));
                b.CloseComponent();
            }));
        return (c, k, giden);
    }

    [Fact]
    public void T17S_sifir_gecerli_kapiya_YAZILMAZ_0_gider()
    {
        var (c, k, giden) = ParaKutusu();
        c.Find("#t17-k").Input("0");
        Assert.False(k.Var);
        Assert.Equal(0m, giden[^1]);
    }

    [Fact]
    public void T17S_eksi_isaret_korunur_kapi_yalniz_ayristirma_hatasi_icin()
    {
        var (c, k, giden) = ParaKutusu();
        c.Find("#t17-k").Input("-5");
        Assert.False(k.Var);            // eksi ayrıştırılabilir → kapı değil, çağıranın EnAz/iş kuralı karar verir
        Assert.Equal(-5m, giden[^1]);   // işaret DOĞRU: -5 (sessiz 5'e dönmez)
        c.Find("#t17-k").Input("-");
        Assert.True(k.Var);             // tek başına "-" anlaşılamaz → kapı kilitler
        Assert.Null(giden[^1]);
    }

    // GIRDI-AILESI-2 tur 3 (doğrulayıcı YÜKSEK · K9 "en çok 2 ondalık, fazlası sessizce yuvarlanmaz, hata"):
    // "0,005" artık ne olduğu gibi gider ne yuvarlanır — alan altı hata + kapı, çağırana null.
    [Fact]
    public void T17S_kurus_0_005_girdisi_yuvarlanmaz_kapi_kilitler_null_gider()
    {
        var (c, k, giden) = ParaKutusu();
        c.Find("#t17-k").Input("0,005");
        Assert.True(k.Var);
        Assert.Null(giden[^1]);
        c.Find("#t17-k").Input("0,01");
        Assert.False(k.Var);
        Assert.Equal(0.01m, giden[^1]);
    }

    [Fact]
    public void T17S_has_gram_4_hane_0_0001_kaybolmaz()
    {
        var (c, k, giden) = ParaKutusu(null, gram: true);
        c.Find("#t17-k").Input("1,0001");
        Assert.False(k.Var);
        Assert.Equal(1.0001m, giden[^1]);
        c.Find("#t17-k").Input("0,0001");
        Assert.Equal(0.0001m, giden[^1]);
    }

    [Fact]
    public void T17S_cok_buyuk_999_999_999_99_birebir()
    {
        var (c, k, giden) = ParaKutusu();
        c.Find("#t17-k").Input("999.999.999,99");
        Assert.False(k.Var);
        Assert.Equal(999999999.99m, giden[^1]);
    }

    [Fact]
    public void T17S_bos_null_gider_kapi_bos_eski_deger_GITMEZ()
    {
        var (c, k, giden) = ParaKutusu(5000m);
        c.Find("#t17-k").Input("");
        Assert.False(k.Var);
        Assert.Null(giden[^1]);
    }

    [Fact]
    public void T17S_turkce_45_000_ve_1_5_cozulur_belirsiz_1_500_kapiya_yazilir_null_gider_metin_korunur()
    {
        var (c, k, giden) = ParaKutusu(5000m);
        c.Find("#t17-k").Input("45.000");
        Assert.Equal(45000m, giden[^1]);
        c.Find("#t17-k").Input("1,5");
        Assert.Equal(1.5m, giden[^1]);
        c.Find("#t17-k").Input("1.500");
        Assert.True(k.Var);
        Assert.Equal(("t17-k", "Tutar"), k.Ilk);
        Assert.Null(giden[^1]);                                          // ara/eski değer (1,5 / 5000) GİTMEZ
        Assert.Equal("1.500", c.Find("#t17-k").GetAttribute("value"));  // kullanıcının metni ezilmez
        Assert.Equal("true", c.Find("#t17-k").GetAttribute("aria-invalid"));
        c.Find("#t17-k").Input("1.500,00");
        Assert.False(k.Var);                                             // düzeltilince kilit kalkar
        Assert.Equal(1500m, giden[^1]);
    }

    [Fact]
    public void T17S_kapi_on_ek_ayrimi_ve_dispose_kaydi_siler()
    {
        var k = new GecersizAlanKapisi();
        k.Bildir("avans-mahsup-tutar", "Tutar", true);
        Assert.True(k.OnekleVar("avans-"));
        Assert.False(k.OnekleVar("siparis-"));
        Assert.Equal(["Tutar"], k.Etiketler("avans-"));
        k.Bildir("avans-mahsup-tutar", "Tutar", false);
        Assert.False(k.Var);
        Assert.Null(k.Ilk);
    }
}

// ═══ DOĞRULAYICI t18 (29.09 15:00) — PRO alt eylem şeridi: ödeme özeti sarmalayıcısı ═══
// t18 CSS'i ".fis-pro-odeme-ozet-sar:not(:has(> :not([hidden]))) { display:none }" kuralına dayanır.
// Önkoşul: karmada sarmalayıcının TÜM çocukları [hidden] olmalı (yoksa kural devreye girmez ve Kaydet
// yine alta sarar); tek ödemede özet GÖRÜNÜR kalmalı (bilgi kaybolmaz). Karmada gizli özet de para
// taşır → tutar biçimi sınırları (AwayFromZero, tr-TR, U+2212) sabit metinle sınanır.
public sealed class SATIS3SinirTestleriT18 : BunitContext
{
    public SATIS3SinirTestleriT18()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }
    private static string N(string s) => System.Text.RegularExpressions.Regex.Replace(s.Replace(' ', ' ').Replace(' ', ' '), @"\s+", " ").Trim();

    private IRenderedComponent<FisProOdemeHapi> Hap(bool karma, decimal toplam, params decimal[] tutarlar) =>
        Render<FisProOdemeHapi>(p => p.Add(x => x.Karma, karma).Add(x => x.Toplam, toplam)
            .Add(x => x.OdemeTuru, "NAKIT").Add(x => x.KasaAdi, "Ana Kasa")
            .Add(x => x.Bacaklar, tutarlar.Select((t, i) => new FisBacagi { Arac = i == 0 ? "NAKIT" : "KART", TutarTl = t }).ToList()));

    [Fact]
    public void T18_karmada_ozet_sarmalayicisinin_TUM_cocuklari_gizli()
    {
        var c = Hap(true, 1000m, 600m, 400m);
        var sar = c.Find(".fis-pro-odeme-ozet-sar");
        Assert.NotEmpty(sar.Children);
        Assert.All(sar.Children, ch => Assert.True(ch.HasAttribute("hidden"), ch.OuterHtml));
    }
    [Fact]
    public void T18_tek_odemede_ozet_GORUNUR_bilgi_kaybolmaz()
    {
        var c = Hap(false, 45_000m);
        var oz = c.Find("#islem-pro-odeme-ozet");
        Assert.False(oz.HasAttribute("hidden"));
        Assert.Equal("Nakit · Ana Kasa · 45.000 ₺", N(oz.TextContent));
    }
    [Fact] public void T18_sifir_bacaklar_fark_sifir() =>
        Assert.Equal("Nakit 0 ₺ · Kart 0 ₺ → Fark 0 ₺", N(Hap(true, 0m, 0m, 0m).Find("#islem-pro-odeme-ozet").TextContent));
    [Fact] public void T18_kurus_0_005_yukari_yuvarlanir() =>
        Assert.Contains("Nakit 0,01 ₺", N(Hap(true, 0.005m, 0.005m, 0m).Find("#islem-pro-odeme-ozet").TextContent));
    [Fact] public void T18_cok_buyuk_999_999_999_99_tr_TR() =>
        Assert.Contains("Nakit 999.999.999,99 ₺", N(Hap(true, 999_999_999.99m, 999_999_999.99m, 0m).Find("#islem-pro-odeme-ozet").TextContent));
    [Fact] public void T18_eksik_odeme_fark_kirmizi_ve_TRY_yok()
    {
        var c = Hap(true, 1000m, 600m, 300m);
        var f = c.Find("#islem-pro-fark");
        Assert.Contains("metin-kirmizi", f.ClassName);
        Assert.DoesNotContain("TRY", c.Find("#islem-pro-odeme-ozet").TextContent);
    }
}

// ═══ DOĞRULAYICI t19 (29.09, hasım — kör inceleme): MASAK kapısı fiş TOPLAMINA + müşteri kümülatifine bakar ═══
// Beklenen değerler SABİT metindir (üretim kodundan hesaplanmaz). Sahte eşik 200.000 (kimlik = amaç).
// Sınanan: eşik tam sınırı (>=) · 1 kuruş altı · sorgu tutarı InvariantCulture · çok büyük tutar · sıfır/eksi/belirsiz
// girişte soru GİTMEZ · gereksinim ucu düşerse istek FIRTINASI yok (fail-closed: Kaydet kilitli) · müşteri Peşin'e
// dönünce eski "kimlik gerekli" cevabı kalkar.
public sealed class SATIS3SinirTestleriT19 : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private Func<string, string?>? gereksinimOzel;
    private bool gereksinimHata;
    private int hataSayaci;
    private const string Kimlik = """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";

    public SATIS3SinirTestleriT19()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriT19 t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/masak/gereksinim") && t.gereksinimHata && Interlocked.Increment(ref t.hataSayaci) <= 30)   // 30 hatadan sonra düzelir: döngü varsa test ASILMAZ, sayı ölçülür
                return new(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"gereksinim düştü"}""", System.Text.Encoding.UTF8, "application/json") };
            const string u1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
            const string u2 = """{"id":"u2","ad":"Hasır Bilezik","ayarId":"K14","ayar":"14 Ayar","hasGrami":2.1,"iscilikTl":0,"satisFiyati":null,"gramAgirlik":3.6,"milyem":585,"barkod":"8690000000999","kod":"K-2"}""";
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => """{"had":2000000000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":2000000000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") && v.Contains("urunId=u2") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $"[{u1},{u2}]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş.","vergiNo":"10000000146","kimlikTuru":"TCKN","adres":"Ulus, Ankara"}]""",
                var v when v.Contains("/masak/gereksinim") && t.gereksinimOzel?.Invoke(v) is { } oz => oz,
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private List<string> Sorgular() { lock (istekler) return istekler.Where(i => i.Yol.Contains("/masak/gereksinim")).Select(i => i.Yol).ToList(); }

    // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı. MASAK kimlik formu fişin altına AÇILMAZ — eşik/kümülatif tutunca
    // Fark bloğunda "+ Kimlik bilgisi" (#yildiz-tezgah-kimlik) çıkar, form (aynı MasakParcasi, .kart--masak) onun açtığı
    // çekmecededir (#cekmece-yildiz-kimlik). "Form açılır" = bu giriş görünür; kilit = #yildiz-tezgah-kaydet (title).
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, ArayuzModuServisi.Profesyonel));
        return Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
    }

    private static void Kalem(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, string fiyat, int beklenen)
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        if (urunId == "u1") e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-birimfiyat").Input(fiyat);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.YildizKalemSayisi()), Bekle);
    }

    /// <summary>Kimlik formu açıldı mı: Yıldız'da "+ Kimlik bilgisi" girişi (form onun çekmecesinde).</summary>
    private static bool FormAcik(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Assert.Single(e.FindAll("#yildiz-tezgah-fark"));   // tezgâh çizildi — olumsuz denetim boş DOM'da yeşil vermez
        return e.FindAll("#yildiz-tezgah-kimlik").Count > 0;
    }

    /// <summary>Kimlik çekmecesini açar (formun kendisi: .kart--masak / #islem-masak-hata).</summary>
    private static void CekmeceAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.Find("#yildiz-tezgah-kimlik").Click();
        e.WaitForElement("#cekmece-yildiz-kimlik", Bekle);
    }

    [Fact]
    public void T19d_esik_TAM_SINIRI_100000_arti_100000_form_ACILIR_Kaydet_KILITLI()
    {
        gereksinimOzel = v => v.Contains("tutar=200000&cariId=c1") ? Kimlik : null;
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "100.000", 1);
        Kalem(e, "u2", "100.000", 2);
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        CekmeceAc(e);
        Assert.NotEmpty(e.FindAll("#cekmece-yildiz-kimlik .kart--masak"));
        Satis4Yardim.MasakBelgeNoSil(e);   // SATIS-4 (b): kimlik müşteriden dolar; kilit sınaması için belge no silinir (yeniden dolmaz)
        Assert.Contains(Sorgular(), s => s.EndsWith("/masak/gereksinim?tutar=200000&cariId=c1"));
        Assert.True(e.GbYildizKaydet().HasAttribute("disabled"));
    }

    [Fact]
    public void T19d_bir_KURUS_alti_199999_99_sorgu_INVARIANT_nokta_form_ACILMAZ()
    {
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "100.000", 1);
        Kalem(e, "u2", "99.999,99", 2);
        e.WaitForAssertion(() => Assert.Contains(Sorgular(), s => s.EndsWith("/masak/gereksinim?tutar=199999.99&cariId=c1")), Bekle);
        Assert.DoesNotContain(Sorgular(), s => s.Contains("199999,99"));
        Assert.False(FormAcik(e));
        Assert.Empty(e.FindAll(".kart--masak"));
    }

    [Fact]
    public void T19d_cok_buyuk_999_999_999_99_sorgu_birebir_form_ACILIR()
    {
        gereksinimOzel = v => v.Contains("tutar=999999999.99") ? Kimlik : null;
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "999.999.999,99", 1);
        e.WaitForAssertion(() => Assert.Contains(Sorgular(), s => s.EndsWith("/masak/gereksinim?tutar=999999999.99&cariId=c1")), Bekle);
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        CekmeceAc(e);
        Assert.NotEmpty(e.FindAll("#cekmece-yildiz-kimlik .kart--masak"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.500")]   // belirsiz: FormAlani.SayiCoz reddeder → ne 1,5 ne 1500 sorulur
    [InlineData("")]
    public void T19d_sifir_eksi_belirsiz_bos_fiyatta_MASAK_sorusu_GITMEZ_POST_yok(string fiyat)
    {
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-birimfiyat").Input(fiyat);
        e.Find("#islem-kalem-ekle").Click();
        Thread.Sleep(400);
        Assert.DoesNotContain(Sorgular(), s => s.Contains("tutar=-") || s.Contains("tutar=0&") || s.Contains("tutar=1.5&") || s.Contains("tutar=1500&"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));
    }

    [Fact]
    public void T19d_gereksinim_ucu_DUSERSE_istek_FIRTINASI_yok_Kaydet_KILITLI()
    {
        gereksinimHata = true;
        gereksinimOzel = v => v.Contains("tutar=250000") ? Kimlik : null;   // düzelince sunucu kimlik ister (eşik üstü)
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "250.000", 1);   // eşik (200.000) üstü → form açılmalı, cevap gelmedi → fail-closed
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        Thread.Sleep(1500);
        var n = Sorgular().Count(s => s.Contains("tutar=250000"));
        Assert.True(n <= 3, $"gereksinim ucu düşünce {n} istek art arda atıldı (hata sonrası yeniden deneme döngüsü)");
        Assert.True(e.GbYildizKaydet().HasAttribute("disabled"));   // kimlik girilmeden Kaydet KİLİTLİ (fail-closed)
    }

    [Fact]
    public void T20_gereksinim_ucu_DUSERSE_Turkce_HATA_BANDI_ve_Yeniden_dene_TEK_istek_atar()
    {
        gereksinimHata = true;
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "250.000", 1);
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        CekmeceAc(e);
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr").MasakGereksinimHata, e.Find("#islem-masak-hata").TextContent), Bekle);
        Assert.True(e.GbYildizKaydet().HasAttribute("disabled"));
        Thread.Sleep(300);
        var once = Sorgular().Count(s => s.Contains("tutar=250000"));
        e.Find("#islem-masak-tekrar").Click();
        e.WaitForAssertion(() => Assert.Equal(once + 1, Sorgular().Count(s => s.Contains("tutar=250000"))), Bekle);
        Thread.Sleep(300);
        Assert.Equal(once + 1, Sorgular().Count(s => s.Contains("tutar=250000")));   // düğme TEK istek; döngü yok
        Assert.NotEmpty(e.FindAll("#islem-masak-hata"));
    }

    /// <summary>t21 (hasım doğrulayıcı ORTA): MASAK kuralı alınamadıysa Kaydet sebebi GERÇEK engeli söyler ("kimlik bilgilerini
    /// girin" değil — doldurulacak alan yok) ve BASİT üst bandı "gereken alanlar açıldı" demez. PRO + BASİT.
    /// TEMA TEMİZLİĞİ 17.10: sebep Yıldız'da Kaydet'in title'ı ve Fark bloğunun not satırı (özgün #islem-kaydet-kilit SATIŞ'ta yok).</summary>
    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void T21_gereksinim_ucu_DUSERSE_kilit_sebebi_KURAL_ALINAMADI_bant_ALANLAR_ACILDI_demez(string mod)
    {
        gereksinimHata = true;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizPesinBekle();
        e.GbYildizMusteriSec("c1");
        if (mod == ArayuzModuServisi.Profesyonel) e.YildizOdemeSec("KART");   // BASİT: tek ödeme Nakit (tevsik haddi burada çok yüksek)
        Kalem(e, "u1", "250.000", 1);
        var s3 = Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr");
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        e.WaitForAssertion(() => Assert.Equal(s3.MasakGereksinimHata, e.GbYildizKaydet().GetAttribute("title")), Bekle);
        Assert.Equal(s3.MasakGereksinimHata, e.Find("#yildiz-tezgah-not").TextContent.Trim());
        Assert.True(e.GbYildizKaydet().HasAttribute("disabled"));
        var dil = Services.GetRequiredService<DilServisi>().T;
        Assert.Empty(e.FindAll("#islem-yasal-kapi"));
        Assert.DoesNotContain(dil.Arayuz.YasalKapiAcildi, e.Find("#yildiz-tezgah-fark").TextContent);   // "… gereken alanlar açıldı." YOK
        CekmeceAc(e);
        Assert.NotEmpty(e.FindAll("#islem-masak-hata"));
        Assert.DoesNotContain(dil.Islem.MasakKimlikEksik, e.Markup);
    }

    [Fact]
    public void T19d_kumulatif_musteriden_PESINE_donunce_eski_kimlik_cevabi_KALKAR()
    {
        gereksinimOzel = v => v.Contains("tutar=150000&cariId=c1") ? Kimlik : null;
        var e = Ekran();
        e.GbYildizMusteriSec("c1");
        e.YildizOdemeSec("KART");
        Kalem(e, "u1", "150.000", 1);
        e.WaitForAssertion(() => Assert.True(FormAcik(e)), Bekle);
        e.GbYildizMusteriSec("pm1");
        e.WaitForAssertion(() => Assert.False(FormAcik(e)), Bekle);
        Assert.Empty(e.FindAll(".kart--masak"));
        Assert.NotEqual(Kuyumcu.Core.I18n.Sozlukler.Tr.Islem.MasakKimlikEksik, e.GbYildizKaydet().GetAttribute("title"));
    }
}

// ═══ DOĞRULAYICI t20/k2 (29.09 akşam, hasım — kör inceleme) ═══════════════════════════════════════════
// Sınanan: boş (0) ödeme satırı gönderilmez ama 1 kuruş gönderilir · altın satırı 0 olsa da gider · çok büyük
// tutar düşmez · nakit kullanılamayan fişte yeni satır Nakit AÇILMAZ · tevsik "aşan" sınırı (tam hadde nakit
// serbest, 1 kuruş üstü değil) · Türkçe sayı girişi · kuruş yuvarlaması. Beklenen değerler SABİT.
public sealed class SATIS3SinirTestleriK2Dogrulayici
{
    private static FisBacagi B(string arac, decimal t) => new() { Arac = arac, TutarTl = t };
    private static string N(string s) => s.Replace(' ', ' ').Replace(' ', ' ');

    [Fact] public void K2D_sifir_satir_GONDERILMEZ_bir_kurus_GONDERILIR()
    {
        var g = FisFark.Gonderilecek([B("KART", 50000m), B("NAKIT", 0m), B("HAVALE", 0.01m)]);
        Assert.Equal(2, g.Count);
        Assert.Equal(new[] { "KART", "HAVALE" }, g.Select(x => x.Arac).ToArray());
        Assert.Equal(50000.01m, g.Sum(x => x.TutarTl));
    }

    [Fact] public void K2D_altin_satiri_tutari_0_olsa_da_GIDER() =>
        Assert.Single(FisFark.Gonderilecek([B("ALTIN", 0m), B("NAKIT", 0m)]));

    [Fact] public void K2D_hepsi_bos_ise_liste_BOS() =>
        Assert.Empty(FisFark.Gonderilecek([B("NAKIT", 0m), B("KART", 0m)]));

    [Fact] public void K2D_cok_buyuk_999_999_999_99_dusmez()
    {
        var g = FisFark.Gonderilecek([B("HAVALE", 999_999_999.99m), B("KART", 0m)]);
        Assert.Equal(999_999_999.99m, Assert.Single(g).TutarTl);
    }

    [Fact] public void K2D_eksi_satir_listede_KALIR_sayfa_kapisi_reddetmeli()
    {
        // Gonderilecek yalnız 0'ı düşürür; eksi satır listede kalır → KarmaGonderilebilir "> 0" kapısı şart.
        var g = FisFark.Gonderilecek([B("KART", 150m), B("NAKIT", -50m)]);
        Assert.Contains(g, x => x.TutarTl == -50m);
        Assert.False(g.All(x => x.TutarTl > 0m));
    }

    [Theory]
    [InlineData(false, "NAKIT")]
    [InlineData(true, "KART")]
    public void K2D_bos_fiste_yeni_satir_araci(bool nakitsiz, string beklenen) =>
        Assert.Equal(beklenen, FisFark.YeniBacakAraci([], nakitsiz));

    [Fact] public void K2D_nakitsiz_Kart_var_ise_HAVALE_uc_arac_dolu_ise_KART_asla_NAKIT_degil()
    {
        Assert.Equal("HAVALE", FisFark.YeniBacakAraci([B("NAKIT", 50000m), B("KART", 0m)], true));
        Assert.Equal("KART", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m), B("HAVALE", 1m)], true));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([B("NAKIT", 1m), B("KART", 1m), B("HAVALE", 1m)], false));
    }

    [Theory]
    [InlineData("30000", false)]      // tam hadde nakit serbest ("aşan")
    [InlineData("30000.01", true)]    // 1 kuruş üstü tevsik ister
    [InlineData("0", false)]
    [InlineData("-50000", false)]
    [InlineData("999999999.99", true)]
    public void K2D_tevsik_asan_siniri(string tutar, bool beklenen) =>
        Assert.Equal(beklenen, TevsikOneri.Gerekli("NAKIT", "SATIS", decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture), 30000m));

    [Fact] public void K2D_had_bilinmiyorsa_0_nakit_kilitlenmez() =>
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", 999_999_999.99m, 0m));

    [Fact] public void K2D_kart_odemede_tevsik_gerekmez() =>
        Assert.False(TevsikOneri.Gerekli("KART", "SATIS", 50000m, 30000m));

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void K2D_turkce_giris_cozulur(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact] public void K2D_belirsiz_1_500_REDDEDILIR_bos_BOS()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    [Theory]
    [InlineData("0.005", "0,01 ₺")]
    [InlineData("0.004", "0 ₺")]
    [InlineData("50000", "50.000 ₺")]
    [InlineData("999999999.99", "999.999.999,99 ₺")]
    public void K2D_tl_bicimi_kurus_AwayFromZero(string tutar, string beklenen) =>
        Assert.Equal(beklenen, N(Bicim.Tl(decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture))));
}

// ═══ DOĞRULAYICI k2/2 (29.09 akşam, hasım — kör inceleme, § 33 turu) ═══════════════════════════════════
// Sınanan (k2 değişiklikleri): OtoDengele hedef seçimi (boş satır kalanı ÇALMAZ) · eksi/çok büyük/kuruş ·
// nakit kullanılamayan fişte ikinci satır Nakit açılmaz · 0 ₺ nakit satırı nakit kilidini tetiklemez ·
// fiş toplamı yuvarlaması (AwayFromZero, 0,005) · has 0,0001 · DÖVİZ satırı kur yokken SESSİZCE düşmemeli
// (FisOdemeBacaklari.DovizTl yorumu: "Kur yoksa 0 döner ve bacak fişi kapatamaz; sunucu da reddeder (fail-closed)").
// Beklenen değerler SABİT (üretim kodundan hesaplanmaz).
public sealed class SATIS3SinirTestleriK2Hasim2
{
    private static FisBacagi B(string arac, decimal tl, bool elle = false) => new() { Arac = arac, TutarTl = tl, ElleYazildi = elle };
    private static FisKalemi K(decimal adet, decimal fiyat, decimal iskYuzde = 0m) =>
        new() { UrunId = "u1", UrunAdi = "Burma", Adet = adet, BirimFiyat = fiyat, SatirIskontoYuzde = iskYuzde };

    [Fact] public void K2H_bos_satir_kalani_CALMAZ_kart_altin_ile_dengelenir()
    {
        // Kart 127.947,77 (otomatik) + boş Nakit (0, otomatik) + Eski altın 63.101 → Kart = 64.846,77, boş satır 0 kalır.
        var l = new List<FisBacagi> { B("KART", 127_947.77m), B("NAKIT", 0m), B("ALTIN", 63_101m) };
        Assert.True(FisHesap.OtoDengele(l, 127_947.77m));
        Assert.Equal(64_846.77m, l[0].TutarTl);
        Assert.Equal(0m, l[1].TutarTl);
        Assert.Equal(63_101m, l[2].TutarTl);
    }

    [Fact] public void K2H_elle_yazilan_toplami_asarsa_hedef_EKSIYE_dusmez()
    {
        var l = new List<FisBacagi> { B("KART", 150_000m, elle: true), B("NAKIT", 10m) };
        FisHesap.OtoDengele(l, 100_000m);
        Assert.Equal(0m, l[1].TutarTl);
        Assert.Equal(150_000m, l[0].TutarTl);
    }

    [Fact] public void K2H_cok_buyuk_toplamda_kurus_kalan_dogru()
    {
        var l = new List<FisBacagi> { B("KART", 0.01m, elle: true), B("NAKIT", 5m) };
        Assert.True(FisHesap.OtoDengele(l, 999_999_999.99m));
        Assert.Equal(999_999_999.98m, l[1].TutarTl);
    }

    [Fact] public void K2H_nakitsiz_fiste_ikinci_satir_NAKIT_acilmaz()
    {
        Assert.Equal("KART", FisFark.YeniBacakAraci([], true));
        Assert.Equal("KART", FisFark.YeniBacakAraci([B("NAKIT", 100m)], true));
        Assert.Equal("HAVALE", FisFark.YeniBacakAraci([B("KART", 100m)], true));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([B("KART", 100m)], false));
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("0.01", true)]
    public void K2H_sifir_nakit_satiri_kilidi_TETIKLEMEZ(string nakit, bool beklenen) =>
        Assert.Equal(beklenen, FisHesap.NakitKapali(true, [B("KART", 1000m), B("NAKIT", decimal.Parse(nakit, System.Globalization.CultureInfo.InvariantCulture))]));

    [Fact] public void K2H_eksi_nakit_satiri_kilidi_TETIKLEMEZ_ama_gonderilir() =>
        Assert.False(FisHesap.NakitKapali(true, [B("NAKIT", -5m)]));

    [Theory]
    [InlineData("1", "2.345", "0", "2.35")]      // 0,005 → yukarı (AwayFromZero; ToEven 2,34 verirdi)
    [InlineData("3", "0.335", "0", "1.01")]      // 1,005 → 1,01
    [InlineData("1", "2.325", "0", "2.33")]      // ToEven 2,32 verirdi
    [InlineData("1", "999999999.99", "0", "999999999.99")]
    [InlineData("1", "0", "0", "0")]
    [InlineData("1", "100", "0.005", "99.99")]   // satır iskontosu 0,005 ₺ → 0,01 ₺
    public void K2H_fis_toplami_yuvarlama(string adet, string fiyat, string isk, string beklenen)
    {
        var c = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal(decimal.Parse(beklenen, c), FisHesap.Toplam([K(decimal.Parse(adet, c), decimal.Parse(fiyat, c), decimal.Parse(isk, c))], null));
    }

    [Fact] public void K2H_fis_iskontosu_kurus_siniri() =>
        Assert.Equal(99.99m, FisHesap.Toplam([K(1m, 100m)], 0.005m));

    [Fact] public void K2H_has_0_0001_gram_22_ayar_6_haneye_yuvarlanir() =>
        Assert.Equal(0.000092m, Kuyumcu.Core.Domain.HasHesap.ToHasGram(0.0001m, Kuyumcu.Core.Domain.Ayarlar.Bul("K22")!));

    [Fact] public void K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ()
    {
        // Kur kaydında GBP yok (KurModulu GbpTry null olabilir) → DovizTl 0 döner. Otomatik Nakit satırı
        // fişin tamamını üstlenir (OtoDengele DÖVİZ'i hedef almaz) → fark 0. Döviz satırı gövdeden DÜŞERSE
        // müşterinin verdiği 100 GBP kayda girmez ve fiş tam nakit kaydedilir. Beklenen: satır gövdede kalır
        // (sunucu 0 TL döviz satırını reddeder — fail-closed) ya da sayfa kapısı kilitler.
        var doviz = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "GBP", DovizTutar = 100m, TutarTl = 0m };
        var l = new List<FisBacagi> { B("NAKIT", 0m), doviz };
        FisHesap.OtoDengele(l, 50_000m);
        Assert.Equal(50_000m, l[0].TutarTl);   // otomatik nakit tamamını aldı → fark 0
        Assert.Contains(doviz, FisFark.Gonderilecek(l));
    }
}

// ════════════════════════════════════════════════════════════════════════
// SATIS-3 t21 — HASIM DOĞRULAYICI (Aşkın + Mehmet) BAĞIMSIZ SINIR TESTLERİ.
// Döviz ödeme satırı: kur yok/kısmi kur (GBP yok), kur gelince otomatik kalanın
// yeniden dengelenmesi, AwayFromZero 0,005 sınırı, çok büyük tutar, 4 hane
// miktar, eksi ve belirsiz Türkçe giriş. Beklenen değerler SABİT sayıdır.
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriT21Hasim : BunitContext
{
    private readonly List<(string Yol, string Govde)> istekler = [];
    private string kurJson = """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""";

    public SATIS3SinirTestleriT21Hasim()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriT21Hasim t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/tevsik/") => """{"had":2000000000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":2000000000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => t.kurJson,
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"kusd","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true},{"id":"kgbp","ad":"Sterlin Kasa","paraBirimi":"GBP","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/pos/cihazlar") => """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private static Kuyumcu.Core.I18n.Satis3Metinleri S3 => Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr");
    private List<string> Govdeler() { lock (istekler) return [.. istekler.Where(i => i.Yol.EndsWith("/islemler/satis-fisi")).Select(i => i.Govde)]; }
    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.GbYildizKaydet();

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı. Karma ödeme tezgâhın "+ Başka ödeme"sidir (sayfanın OdemeyiBol'u:
    /// tek ödeme ilk satır olur + ikinci boş satır). İlk satır Döviz'e çevrilir; tek satır isteniyorsa ikinci satır "Çıkar"la
    /// düşer (Döviz satırı tek ödemeye İNDİRİLEMEZ — karma kalır). Sonuç eskisiyle aynı: [Döviz] ya da [Döviz, TL otomatik kalan].
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> DovizliAc(bool ikinciSatir)
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        e.GbYildizMusteriSec("c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, e.YildizKalemSayisi()), Bekle);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForElement("#islem-bacak-1-arac", Bekle);
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "DOVIZ");
        if (!ikinciSatir)
        {
            e.Find("#islem-bacak-sil-1").Click();
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-1-arac")), Bekle);
        }
        e.WaitForElement("#islem-bacak-0-doviz", Bekle);
        return e;
    }

    /// <summary>Kur yokken otomatik 2. satır toplamın TAMAMINI üstlenir; kur gelince (Kuru yeniden al) döviz 10.000 TL olur,
    /// otomatik satır 0'a iner → gövdede YALNIZ döviz, ödeme toplamı fiş toplamı (10.000) — fazla ödeme kaydı yok.</summary>
    [Fact]
    public void H21_kur_gelince_otomatik_kalan_yeniden_dengelenir_govde_toplami_10000()
    {
        var e = DovizliAc(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-0-kur-tekrar")), Bekle);
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
        e.Find("#islem-bacak-0-kur-tekrar").Click();
        e.WaitForAssertion(() => Assert.Equal("10000.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        e.WaitForAssertion(() =>
        {
            var o = System.Text.Json.JsonDocument.Parse(Assert.Single(Govdeler())).RootElement.GetProperty("odemeler");
            Assert.Equal(10_000m, o.EnumerateArray().Sum(x => x.GetProperty("tutarTl").GetDecimal()));
            Assert.Contains(o.EnumerateArray(), x => x.GetProperty("arac").GetString() == "DOVIZ" && x.GetProperty("tutarTl").GetDecimal() == 10_000m);
        }, Bekle);
    }

    /// <summary>KISMİ kur: USD/EUR var, GBP YOK (KurModulu GbpTry nullable). GBP 100 → TL 0, Kaydet PASİF, sebep "kur alınamadı", POST 0.</summary>
    [Fact]
    public void H21_GBP_kuru_yok_USD_var_iken_GBP_satiri_kilitler_POST_yok()
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = DovizliAc(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-birim", "GBP");
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "100");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-0-kur-yok")), Bekle);
        Assert.Equal("0.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        Assert.Equal(S3.DovizKuruYok, Kaydet(e).GetAttribute("title"));                       // Yıldız: kilit sebebi Kaydet'in yanında
        Assert.Equal(S3.DovizKuruYok, e.Find("#yildiz-tezgah-not").TextContent.Trim());     // ve Fark bloğunun not satırında
        Kaydet(e).Click();
        Assert.Empty(Govdeler());
    }

    /// <summary>0,005 sınırı: 0,5 USD × 40,01 = 20,005 → AwayFromZero 20,01 (ToEven 20,00 verirdi; sunucu OdemeSepeti AwayFromZero).</summary>
    [Fact]
    public void H21_kurus_yuvarlama_0005_AwayFromZero()
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40.01,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = DovizliAc(ikinciSatir: false);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "0,5");
        e.WaitForAssertion(() => Assert.Equal("20.01", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
    }

    /// <summary>4 hane miktar: 0,0001 USD × 40 = 0,004 → 0,00 TL. Miktar girildi ama TL 0 → satır DÜŞMEZ, fiş kilitli (sessiz düşme yok).</summary>
    [Fact]
    public void H21_dort_hane_miktar_TL_sifira_yuvarlaninca_satir_dusmez_kilitli()
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = DovizliAc(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "0,0001");
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        Assert.Empty(Govdeler());
    }

    /// <summary>Çok büyük: 24.999.999,99 USD × 40 = 999.999.999,60 TL (taşma yok, 2 hane).</summary>
    [Fact]
    public void H21_cok_buyuk_tutar_999999999_60()
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = DovizliAc(ikinciSatir: false);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "24.999.999,99");
        e.WaitForAssertion(() => Assert.Equal("999999999.60", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
    }

    /// <summary>Eksi ve belirsiz Türkçe giriş ('-5', '1.500') döviz kutusunda kabul edilmez: Kaydet PASİF, POST 0.</summary>
    [Theory]
    [InlineData("-5")]
    [InlineData("1.500")]
    public void H21_eksi_ve_belirsiz_doviz_girisi_kaydettirmez(string giris)
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = DovizliAc(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", giris);
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        Assert.Empty(Govdeler());
    }

    /// <summary>Saf: sıfır/boş/null benzeri — miktar 0 ve TL 0 döviz satırı boş satırdır (düşer, kilitlemez); 0,0001 miktar ise kilitler.</summary>
    [Fact]
    public void H21_saf_sifir_ve_dort_hane()
    {
        var bos = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "USD", DovizTutar = 0m, TutarTl = 0m };
        var kucuk = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "USD", DovizTutar = 0.0001m, TutarTl = 0m };
        Assert.Empty(FisFark.Gonderilecek([bos]));
        Assert.False(FisFark.DovizKuruEksik([bos]));
        Assert.Single(FisFark.Gonderilecek([kucuk]));
        Assert.True(FisFark.DovizKuruEksik([kucuk]));
    }
}

// ════════════════════════════════════════════════════════════════════════
// SATIS-3 t21 — HASIM DOĞRULAYICI TUR 2 (Aşkın + Mehmet, 30.09) — BAĞIMSIZ
// SINIR TESTLERİ. Önceki H21 kümesinin kapsamadığı durumlar: kur yokken döviz
// miktarı SİLİNİNCE (boş/null) satır düşer ve fiş kalan araçla kaydedilir;
// Türkçe binlik ('45.000') ve ondalık ('1,5') miktar; 0,005 sınırında
// AwayFromZero ≠ ToEven ayrımı (20,025); kur gelip kaydedilebilir olduktan sonra
// birim kuru OLMAYAN birime çevrilince yeniden kilit; saf çok büyük / eksi.
// Beklenen değerler SABİT sayıdır (üretim kodundan hesaplanmaz).
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriT21HasimTur2 : BunitContext
{
    private readonly List<(string Yol, string Govde)> istekler = [];
    private string kurJson = """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""";

    public SATIS3SinirTestleriT21HasimTur2()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class Sahte(SATIS3SinirTestleriT21HasimTur2 t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/tevsik/") => """{"had":2000000000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":2000000000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => t.kurJson,
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"kusd","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true},{"id":"kgbp","ad":"Sterlin Kasa","paraBirimi":"GBP","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":1850000000,"amacBeyaniEsigi":1850000000,"beyanFormuEsigi":1850000000,"ekBelgeEsigi":1850000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/pos/cihazlar") => """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private const string KurVar = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""";
    private static Kuyumcu.Core.I18n.Satis3Metinleri S3 => Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr");
    private List<string> Govdeler() { lock (istekler) return [.. istekler.Where(i => i.Yol.EndsWith("/islemler/satis-fisi")).Select(i => i.Govde)]; }
    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.GbYildizKaydet();

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı. Karma ödeme tezgâhın "+ Başka ödeme"sidir (sayfanın OdemeyiBol'u:
    /// tek ödeme ilk satır olur + ikinci boş satır). İlk satır Döviz'e çevrilir; tek satır isteniyorsa ikinci satır "Çıkar"la
    /// düşer (Döviz satırı tek ödemeye İNDİRİLEMEZ — karma kalır). Sonuç eskisiyle aynı: [Döviz] ya da [Döviz, TL otomatik kalan].
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ac(bool ikinciSatir)
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        e.GbYildizMusteriSec("c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, e.YildizKalemSayisi()), Bekle);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForElement("#islem-bacak-1-arac", Bekle);
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "DOVIZ");
        if (!ikinciSatir)
        {
            e.Find("#islem-bacak-sil-1").Click();
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-1-arac")), Bekle);
        }
        e.WaitForElement("#islem-bacak-0-doviz", Bekle);
        return e;
    }

    /// <summary>Boş/null: kur yokken 100 USD yazıldı (kilit) → kutu SİLİNDİ → döviz satırı boş sayılır, kilit kalkar;
    /// gövdede DÖVİZ YOK, tek ödeme satırı 10.000 TL (toplam korunur, döviz hayalet kaydı yok).</summary>
    [Fact]
    public void V21_kur_yokken_doviz_miktari_SILININCE_satir_duser_kalan_arac_10000_kaydeder()
    {
        var e = Ac(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "100");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-bacak-0-kur-yok")), Bekle);
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-0-kur-yok")), Bekle);
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")), Bekle);
        Kaydet(e).Click();
        e.WaitForAssertion(() =>
        {
            var o = System.Text.Json.JsonDocument.Parse(Assert.Single(Govdeler())).RootElement.GetProperty("odemeler");
            Assert.DoesNotContain(o.EnumerateArray(), x => x.GetProperty("arac").GetString() == "DOVIZ");
            Assert.Equal(10_000m, o.EnumerateArray().Sum(x => x.GetProperty("tutarTl").GetDecimal()));
        }, Bekle);
    }

    /// <summary>Türkçe giriş: '1,5' USD × 40 = 60,00 TL; '45.000' USD × 40 = 1.800.000,00 TL (binlik ayırıcı).</summary>
    [Theory]
    [InlineData("1,5", "60.00")]
    [InlineData("45.000", "1800000.00")]
    public void V21_turkce_doviz_miktari_TL_karsiligi(string giris, string beklenenTl)
    {
        kurJson = KurVar;
        var e = Ac(ikinciSatir: false);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", giris);
        e.WaitForAssertion(() => Assert.Equal(beklenenTl, SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        Assert.Empty(e.FindAll("#islem-bacak-0-kur-yok"));
    }

    /// <summary>0,005 sınırı — ToEven ile AYRIŞAN durum: 0,5 × 40,05 = 20,025 → AwayFromZero 20,03 (ToEven 20,02 verirdi).</summary>
    [Fact]
    public void V21_kurus_20_025_AwayFromZero_20_03()
    {
        kurJson = """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40.05,"eurTry":44,"ayarFiyatlari":[]}""";
        var e = Ac(ikinciSatir: false);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "0,5");
        e.WaitForAssertion(() => Assert.Equal("20.03", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
    }

    /// <summary>Kur geldi, fiş kaydedilebilir (250 USD = 10.000) → birim GBP'ye çevrildi (kurda GBP YOK) → TL 0, yeniden KİLİT, POST 0.</summary>
    [Fact]
    public void V21_kaydedilebilir_iken_kuru_olmayan_birime_gecis_yeniden_kilitler()
    {
        kurJson = KurVar;
        var e = Ac(ikinciSatir: true);
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");
        e.WaitForAssertion(() => Assert.Equal("10000.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")), Bekle);
        SeciciYardim.Sec(e, "islem-bacak-0-birim", "GBP");
        e.WaitForAssertion(() => Assert.Equal("0.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")), Bekle);
        Assert.Equal(S3.DovizKuruYok, Kaydet(e).GetAttribute("title"));                       // Yıldız: kilit sebebi Kaydet'in yanında
        Assert.Equal(S3.DovizKuruYok, e.Find("#yildiz-tezgah-not").TextContent.Trim());     // ve Fark bloğunun not satırında
        Kaydet(e).Click();
        Assert.Empty(Govdeler());
    }

    /// <summary>Saf: çok büyük TL (999.999.999,99) döviz satırı gider, kilit yok; eksi miktar kur-eksik sayılmaz ve
    /// TL 0 iken boş satır olarak düşer (ekranda eksi giriş zaten reddedilir — H21 eksi testi).</summary>
    [Fact]
    public void V21_saf_cok_buyuk_ve_eksi()
    {
        var buyuk = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "USD", DovizTutar = 24_999_999.99975m, TutarTl = 999_999_999.99m };
        Assert.Single(FisFark.Gonderilecek([buyuk]));
        Assert.False(FisFark.DovizKuruEksik([buyuk]));
        Assert.Equal(0m, FisFark.Hesapla(999_999_999.99m, [buyuk]));
        var eksi = new FisBacagi { Arac = "DOVIZ", ParaBirimi = "USD", DovizTutar = -5m, TutarTl = 0m };
        Assert.False(FisFark.DovizKuruEksik([eksi]));
        Assert.Empty(FisFark.Gonderilecek([eksi]));
    }
}

// ═════════════════════════════════════════════════
// SATIS-3 t22 — HASIM DOĞRULAYICI (kapora nakit haddi). Beklenenler SABİT; had sahte sunucudan (30000).
// ═════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriT22Hasim : BunitContext
{
    private const string HamRed = "Tevsik zorunluluğu: işlemin toplam tutarı 40.000,00 TL, 30.000,00 TL haddini aşıyor. Bu durumda ödemenin BİR KISMININ nakit yapılması da (459 s. VUK Genel Tebliği §4.1.2).";
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public SATIS3SinirTestleriT22Hasim()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.StartsWith("/api/v1", StringComparison.Ordinal)) yol = yol[7..];
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((metot, yol, govde));
            if (metot == "POST" && (yol == "/islemler/avans/iade" || yol == "/islemler/avans"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                { Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { message = HamRed }), System.Text.Encoding.UTF8, "application/json") };
            var y = (metot, yol) switch
            {
                (_, var v) when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","sistemCarisi":true}""",
                (_, var v) when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                (_, var v) when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":true}""",
                (_, var v) when v.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                (_, var v) when v.Contains("/urunler") => "[]",
                (_, var v) when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                (_, var v) when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                (_, var v) when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                (_, var v) when v.Contains("/pos/cihazlar") || v.Contains("/iade-adaylari") || v.Contains("/reyon") => "[]",
                (_, var v) when v.Contains("/islemler/personeller") => """{"satirlar":[],"toplam":0,"limit":50,"sinirAsildi":false}""",
                ("GET", var v) when v.StartsWith("/islemler/avans/") => """{"cariId":"c1","acikAvans":40000}""",
                (_, var v) when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Kapora()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — Kapora tezgâhın "Diğer" listesinden açılır.
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.YildizDigerAc("avans");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-kur-yok")));
        SeciciYardim.Sec(e, "avans-cari", "c1");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#avans-acik")));
        return e;
    }

    private int PostSay(string yol) { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol == yol); }

    [Theory]
    [InlineData("30000", false)]
    [InlineData("30000,01", true)]
    [InlineData("29999,99", false)]
    [InlineData("45.000", true)]
    [InlineData("999999999,99", true)]
    [InlineData("1,5", false)]
    public void D22_nakit_had_siniri_ve_turkce_giris(string giris, bool kilitli)
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input(giris);
        e.WaitForAssertion(() => Assert.Equal(kilitli, e.Find("#avans-al").HasAttribute("disabled")));
        e.Find("#avans-al").Click();
        if (kilitli) Assert.Equal(0, PostSay("/islemler/avans"));
    }

    [Fact]
    public void D22_cok_buyuk_tutar_kartla_gonderilir_govde_999999999_99()
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input("999999999,99");
        SeciciYardim.Sec(e, "avans-odeme", "KART");
        e.WaitForAssertion(() => Assert.False(e.Find("#avans-al").HasAttribute("disabled")));
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSay("/islemler/avans")));
        string g; lock (istekler) g = istekler.Last(i => i.Yol == "/islemler/avans").Govde;
        using var d = System.Text.Json.JsonDocument.Parse(g);
        Assert.Equal(999999999.99m, d.RootElement.GetProperty("tutar").GetDecimal());
        Assert.Equal("KART", d.RootElement.GetProperty("odemeTuru").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.500")]
    [InlineData("")]
    public void D22_sifir_eksi_belirsiz_bos_POST_atmaz(string giris)
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input(giris);
        e.Find("#avans-al").Click();
        Assert.Equal(0, PostSay("/islemler/avans"));
    }

    /// <summary>Sunucu avans İADESİNİ de tevsikle reddeder (SatisModulu.P21 AvansIade → tevsik.Dogrula). Ret kullanıcıya GÖRÜNMELİ.</summary>
    [Fact]
    public void D22_avans_iadesi_tevsik_reddi_SESSIZ_kalmamali()
    {
        var e = Kapora();
        e.Find("#avans-iade-tutar").Input("40000");
        e.Find("#avans-iade-gerekce").Input("Müşteri siparişi iptal etti, kapora geri");
        e.Find("#avans-iade").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSay("/islemler/avans/iade")));
        // Beklenen: ya hata bandı ya da iade düğmesi yanında kısa ret metni — ikisinden biri görünmeli.
        e.WaitForAssertion(() => Assert.True(
            e.FindAll("#islem-diger-hata").Count > 0 || e.FindAll("#avans-iade-red").Count > 0 || e.Markup.Contains("nakit", StringComparison.OrdinalIgnoreCase) && e.FindAll("[role=alert]").Count > 0,
            "avans iadesinin tevsik reddi ekranda hiçbir yerde görünmüyor"), TimeSpan.FromSeconds(3));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SATIS-3 t23 — HASIM DOĞRULAYICI (Aşkın + Mehmet, 2026-09-30) — kendi sınır testleri.
// (1) Değişim'in tevsik reddi kısa metne düşmeli (uygulayıcının testi yok: "or Degisim" silinince hiçbir test kırılmadı).
// (2) Tevsik DIŞI 400 kısa ret metnine çevrilmemeli (filtre aşırı geniş olmamalı) — hata bandında sunucu mesajı.
// (3) Kuruş sınırı: istemci kıyası sunucuyla AYNI (yuvarlanmamış tutar, "> had"): 30000,005 ve 30000,004 kilitli; 30.000,00 serbest ve gövdede 30000.00.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3SinirTestleriT23Hasim : BunitContext
{
    private const string TevsikRed = "Tevsik zorunluluğu: 35.000,00 TL nakit ödeme, 30.000,00 TL haddini 5.000,00 TL aşıyor. 459 s. VUK Genel Tebliği gereği ödeme banka aracılığıyla yapılmalıdır.";
    private const string BaskaRed = "Kasa bu şubeye ait değil.";
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Sahte api;

    public SATIS3SinirTestleriT23Hasim()
    {
        api = new Sahte(istekler);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped(_ => new HttpClient(api) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        /// <summary>POST yolu → 400 mesajı (yoksa 200).</summary>
        public Dictionary<string, string> Red { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.StartsWith("/api/v1", StringComparison.Ordinal)) yol = yol[7..];
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((metot, yol, govde));
            if (metot == "POST" && Red.TryGetValue(yol, out var m))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                { Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { message = m }), System.Text.Encoding.UTF8, "application/json") };
            var y = (metot, yol) switch
            {
                (_, var v) when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","sistemCarisi":true}""",
                (_, var v) when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                (_, var v) when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":true}""",
                (_, var v) when v.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                (_, var v) when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":9.16,"iscilikTl":0,"satisFiyati":10000},{"id":"u2","ad":"Tektaş Yüzük","ayarId":"K14","hasGrami":2,"iscilikTl":0,"satisFiyati":20000}]""",
                (_, var v) when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                (_, var v) when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                (_, var v) when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                (_, var v) when v.Contains("/pos/cihazlar") || v.Contains("/reyon") => "[]",
                (_, var v) when v.Contains("/iade-adaylari") =>
                    """[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tlTutari":10000,"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":2,"iadeEdilenAdet":0,"kalanAdet":2,"cariId":"c1"}]""",
                (_, var v) when v.Contains("/islemler/personeller") => """{"satirlar":[],"toplam":0,"limit":50,"sinirAsildi":false}""",
                ("GET", var v) when v.StartsWith("/islemler/avans/") => """{"cariId":"c1","acikAvans":0}""",
                ("POST", "/islemler/avans") => """{"hareketId":"a1","cariId":"c1","tutar":1000,"acikAvans":1000}""",
                ("POST", "/islemler/degisim") => """{"fisId":"f9","farkTl":10000,"farkYonu":"MUSTERI_ODER","kalanIadeHakki":1}""",
                (_, var v) when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Panel(string tur)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        // TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — "Diğer" işlemler tezgâhın "Diğer" listesinden açılır.
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.YildizDigerAc(tur.ToLowerInvariant());
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-kur-yok")));
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Kapora()
    {
        var e = Panel(Kuyumcu.Web.Bilesenler.Fis.DigerTurleri.Avans);
        SeciciYardim.Sec(e, "avans-cari", "c1");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#avans-acik")));
        return e;
    }

    private int PostSay(string yol) { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol == yol); }

    [Fact]
    public void Y23_Degisim_tevsik_reddi_dugme_yaninda_KISA_metin_hata_bandinda_ham_paragraf_YOK()
    {
        api.Red["/islemler/degisim"] = TevsikRed;
        var e = Panel(Kuyumcu.Web.Bilesenler.Fis.DigerTurleri.Degisim);
        SeciciYardim.Sec(e, "degisim-kaynak", "h1");
        SeciciYardim.Sec(e, "degisim-yeni-urun", "u2");
        e.Find("#degisim-yeni-fiyat").Input("45000");
        e.Find("#degisim-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSay("/islemler/degisim")));
        e.WaitForAssertion(() => Assert.Equal("Bu işlem nakitle yapılamaz — kart ya da havale kullanın.", e.Find("#degisim-kaydet-red").TextContent.Trim()));
        Assert.Empty(e.FindAll("#islem-diger-hata"));
        Assert.DoesNotContain("Tevsik zorunluluğu", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Y23_Tevsik_DISI_400_kisa_ret_metnine_CEVRILMEZ_hata_bandinda_sunucu_mesaji()
    {
        api.Red["/islemler/avans"] = BaskaRed;
        var e = Kapora();
        e.Find("#avans-tutar").Input("1000");
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSay("/islemler/avans")));
        e.WaitForAssertion(() => Assert.Contains(BaskaRed, e.Find("#islem-diger-hata").TextContent, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#avans-al-red"));
    }

    [Theory]
    [InlineData("30000,005", true, 30000.01)]
    [InlineData("30000,004", true, 30000.00)]   // sunucu tevsik.Dogrula YUVARLANMAMIŞ dto.Tutar ile kıyaslar (P21 AvansAl) → 30000,004 > 30000 sunucuda da ret; istemci aynı
    [InlineData("30.000,00", false, 30000.00)]
    public void Y23_kurus_siniri_had_kiyasi_yuvarlanmis_tutarla(string giris, bool kilitli, double beklenenGovde)
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input(giris);
        e.WaitForAssertion(() => Assert.Equal(kilitli, e.Find("#avans-al").HasAttribute("disabled")));
        if (kilitli) { e.Find("#avans-al").Click(); Assert.Equal(0, PostSay("/islemler/avans")); return; }
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSay("/islemler/avans")));
        string g; lock (istekler) g = istekler.Last(i => i.Yol == "/islemler/avans").Govde;
        using var d = System.Text.Json.JsonDocument.Parse(g);
        Assert.Equal((decimal)beklenenGovde, d.RootElement.GetProperty("tutar").GetDecimal());
        Assert.Equal("NAKIT", d.RootElement.GetProperty("odemeTuru").GetString());
    }
}
