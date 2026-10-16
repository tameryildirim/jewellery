using Kuyumcu.Api.Modules.Tevsik;
using Kuyumcu.Contracts.Sayfalar.NakitSiniri;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı. Sınanan yol: CariNakitSiniri (tahsilat
// çekmecesinin nakit sınırı ön kontrolü — istemci, sunucunun /tevsik/gunluk
// yanıtındaki KalanNakitHakki ile karar verir). Beklenen değerler SABİT
// yazıldı; ek olarak istemcinin kararı sunucunun saf çekirdeği
// TevsikKurali.Degerlendir ile aynı girdilerde karşılaştırılır (iki ayrı
// kod yolu — biri ötekinden türetilmez).
// Had 30.000 TL testin kendi fikstürüdür (sunucu yanıtı taklidi), istemci
// koduna gömülü bir sayı değildir.
// ════════════════════════════════════════════════════════════════════════
public sealed class YASALKAPI3SinirTestleri
{
    private const decimal Had = 30_000m;
    private const string Cari = "CARI-DG-1";
    private const string Kalip = "Nakit en çok {0} olabilir; kalanı kartla ya da havaleyle alın.";

    private static CariNakitSiniri Sinir(decimal bugunku, bool kuralEtkin = true, bool kumulatif = true)
    {
        var ns = new CariNakitSiniri();
        ns.Ac();
        ns.Yukle(new TevsikGunlukVeri(null, Had, kuralEtkin, kumulatif, bugunku,
            Math.Max(0m, Had - bugunku), false, null));
        return ns;
    }

    [Theory]
    [InlineData("0", false)]                // sıfır: ön kontrol yok
    [InlineData("-50000", false)]           // eksi: tutar alanı ret eder, sınır cümlesi üretilmez
    [InlineData("29999.99", false)]
    [InlineData("30000", false)]            // tam had serbest
    [InlineData("30000.01", true)]
    [InlineData("30000.001", true)]         // kuruş altı: sunucu da yuvarlamadan karşılaştırır
    [InlineData("999999999.99", true)]      // çok büyük tutar
    public void YK3S_01_kalan_hak_tam_hadde_sinir(string tutarMetin, bool beklenen)
    {
        var tutar = decimal.Parse(tutarMetin, System.Globalization.CultureInfo.InvariantCulture);
        var ns = Sinir(0m);
        Assert.Equal(beklenen, ns.Asildi(true, tutar, Cari));
        // aynı girdide sunucunun saf çekirdeği aynı kararı verir (eksi tutar hariç: sunucu mutlak değer alır,
        // eksi tutar zaten sayı kapısında reddedilir — istemci eksiye sınır cümlesi yazmaz)
        if (tutar >= 0m)
            Assert.Equal(beklenen, TevsikKurali.Degerlendir(0m, tutar, null, new TevsikEsikleri { Had = Had }).Gerekli);
    }

    [Theory]
    [InlineData("12000", "18000", false)]
    [InlineData("12000", "18000.01", true)]
    [InlineData("29999.99", "0.01", false)]
    [InlineData("29999.99", "0.02", true)]
    public void YK3S_02_kumulatif_kalan_hak_sunucuyla_ayni(string bugunkuM, string tutarM, bool beklenen)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        decimal bugunku = decimal.Parse(bugunkuM, inv), tutar = decimal.Parse(tutarM, inv);
        var ns = Sinir(bugunku);
        Assert.Equal(beklenen, ns.Asildi(true, tutar, Cari));
        Assert.Equal(beklenen, TevsikKurali.Degerlendir(bugunku, tutar, null, new TevsikEsikleri { Had = Had }).Gerekli);
    }

    [Fact]
    public void YK3S_03_nakit_degilse_ya_da_kural_kapaliysa_on_kontrol_yok()
    {
        Assert.False(Sinir(0m).Asildi(false, 250_000m, Cari));                          // Kart
        Assert.Null(Sinir(0m).Hata(false, 250_000m, Cari, "tr", Kalip));
        Assert.False(Sinir(0m, kuralEtkin: false).Asildi(true, 250_000m, Cari));       // kural kapalı → son söz sunucuda
        Assert.False(Sinir(0m, kumulatif: false).Asildi(true, 250_000m, Cari));        // müşteri bilinmiyor
        var bos = new CariNakitSiniri(); bos.Ac();                                      // yanıt yok (null)
        Assert.False(bos.Asildi(true, 250_000m, Cari));
        Assert.Null(bos.Kalan);
    }

    [Fact]
    public void YK3S_04_cumle_sunucunun_kalan_hakkini_tr_bicimde_soyler()
    {
        var h = Sinir(12_000m).Hata(true, 250_000m, Cari, "tr", Kalip);
        Assert.Equal("Nakit en çok 18.000\u00A0₺ olabilir; kalanı kartla ya da havaleyle alın.", h);
        Assert.DoesNotContain("TRY", h);
    }

    [Fact]
    public void YK3S_05_kalan_hak_sifir_nakit_kapali_tek_cumle()
    {
        var ns = Sinir(30_000m);
        Assert.True(ns.NakitKapali);
        Assert.True(ns.Asildi(true, 0.01m, Cari));
        Assert.Equal("Bugün bu müşteriden nakit alınamaz; kartla ya da havaleyle alın.", ns.Hata(true, 0.01m, Cari, "tr", Kalip));
    }

    [Fact]
    public void YK3S_06_sunucu_eksi_kalan_yollasa_bile_sifira_kenetlenir()
    {
        var ns = new CariNakitSiniri(); ns.Ac();
        ns.Yukle(new TevsikGunlukVeri(null, Had, true, true, 45_000m, -15_000m, false, null));
        Assert.Equal(0m, ns.Kalan);
        Assert.True(ns.NakitKapali);
    }

    [Fact]
    public void YK3S_07_sunucu_reddi_izi_odeme_turu_ve_tutar_degisince_kalkar_geri_doner()
    {
        var ns = new CariNakitSiniri(); ns.Ac();   // ön kontrol yok (yanıt alınamadı) — yalnız sunucu reddi
        ns.Reddet(new ApiHata(400, "Sunucu: nakit sınırı aşıldı.") { Kod = "NAKIT_SINIRI" }, 40_000m, Cari);
        Assert.True(ns.Asildi(true, 40_000m, Cari));
        Assert.Equal("Sunucu: nakit sınırı aşıldı.", ns.Hata(true, 40_000m, Cari, "tr", Kalip));
        Assert.False(ns.Asildi(false, 40_000m, Cari));        // Kart → silinir
        Assert.False(ns.Asildi(true, 39_999.99m, Cari));      // reddin ölçüsünün altı → silinir
        Assert.False(ns.Asildi(true, 40_000m, "CARI-BASKA")); // başka müşteri → silinir
        Assert.True(ns.Asildi(true, 40_000m, Cari));          // Nakit + aynı tutar → geri gelir
        ns.Ac();
        Assert.False(ns.Asildi(true, 40_000m, Cari));         // yeni form: iz kalmaz
    }

    // ── Tur 1 doğrulayıcısının YK3D_S01..S06 vakaları (adları tur 1 trx'inden; tur 2 doğrulayıcısı dosyayı
    //    yeniden yazarken kaynak kaybolduğu için aynı adlarla ve aynı iddialarla YENİDEN kuruldu) ──
    [Theory]
    [InlineData("0", "29999.99", false)]
    [InlineData("0", "30000", false)]
    [InlineData("0", "30000.004", true)]
    [InlineData("0", "30000.005", true)]
    [InlineData("0", "30000.01", true)]
    [InlineData("0", "999999999.99", true)]
    [InlineData("18000", "12000", false)]
    [InlineData("18000", "12000.01", true)]
    [InlineData("29999.99", "0.01", false)]
    [InlineData("29999.99", "0.02", true)]
    [InlineData("30000", "0.01", true)]
    [InlineData("35000", "0.01", true)]
    public void YK3D_S01_istemci_ve_sunucu_ayni_karar(string onceki, string bu, bool beklenen)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        decimal o = decimal.Parse(onceki, inv), t = decimal.Parse(bu, inv);
        Assert.Equal(beklenen, Sinir(o).Asildi(true, t, Cari));
        Assert.Equal(beklenen, TevsikKurali.Degerlendir(o, t, null, new TevsikEsikleri { Had = Had }).Gerekli);
    }

    [Fact]
    public void YK3D_S02_sifir_ve_eksi_tutar_nakit_siniri_hatasi_uretmez()
    {
        var ns = Sinir(30_000m);   // kalan hak 0 olsa bile
        Assert.Null(ns.Hata(true, 0m, Cari, "tr", Kalip));
        Assert.Null(ns.Hata(true, -1m, Cari, "tr", Kalip));
        Assert.False(ns.Asildi(true, 0m, Cari));
        Assert.False(ns.Asildi(true, -250_000m, Cari));
    }

    [Fact]
    public void YK3D_S03_yanit_yok_null_cari_on_kontrol_yok()
    {
        var ns = new CariNakitSiniri(); ns.Ac(); ns.Yukle(null);
        Assert.Null(ns.Kalan);
        Assert.False(ns.NakitKapali);
        Assert.False(ns.Asildi(true, 1_000_000m, null));
        Assert.Null(ns.Hata(true, 1_000_000m, null, "tr", Kalip));
    }

    [Fact]
    public void YK3D_S04_cok_buyuk_tutar_cumlesi_kalan_hakki_tr_TR_bicimde_soyler_TRY_kalibi_yok()
    {
        var h = Sinir(0m).Hata(true, 999_999_999.99m, Cari, "tr", Kalip);
        Assert.Equal("Nakit en çok 30.000\u00A0₺ olabilir; kalanı kartla ya da havaleyle alın.", h);
        Assert.DoesNotContain("TRY", h);
        Assert.DoesNotContain("999", h);
    }

    [Theory]
    [InlineData("", "Bos", false)]
    [InlineData("1,5", "Tamam", false)]
    [InlineData("1.500", "Belirsiz", false)]
    [InlineData("30.000", "Tamam", false)]
    [InlineData("30.000,01", "Tamam", true)]
    [InlineData("45.000", "Tamam", true)]
    public void YK3D_S05_turkce_giris_cozulur_sonra_sinir(string ham, string sonuc, bool asildi)
    {
        var c = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(asildi, Sinir(0m).Asildi(true, c.Deger ?? 0m, Cari));
    }

    [Fact]
    public void YK3D_S06_sunucu_reddi_izi_sinirda_tam_olcu_gecerli_bir_kurus_alti_dusur()
    {
        var ns = new CariNakitSiniri(); ns.Ac();
        ns.Reddet(new ApiHata(400, "ret") { Kod = "NAKIT_SINIRI" }, 30_000.01m, Cari);
        Assert.True(ns.Asildi(true, 30_000.01m, Cari));
        Assert.False(ns.Asildi(true, 30_000.00m, Cari));
    }

    [Fact]
    public void YK3S_08_yalniz_400_ve_NAKIT_SINIRI_kodu_nakit_reddidir()
    {
        Assert.True(CariNakitSiniri.NakitReddi(new ApiHata(400, "x") { Kod = "NAKIT_SINIRI" }));
        Assert.False(CariNakitSiniri.NakitReddi(new ApiHata(400, "x") { Kod = "MASAK" }));
        Assert.False(CariNakitSiniri.NakitReddi(new ApiHata(500, "x") { Kod = "NAKIT_SINIRI" }));
        Assert.False(CariNakitSiniri.NakitReddi(new ApiHata(400, "x")));
    }
}
