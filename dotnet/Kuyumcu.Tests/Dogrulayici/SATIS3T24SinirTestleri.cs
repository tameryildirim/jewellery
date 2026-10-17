using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 t24 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
// Uygulayıcıdan BAĞIMSIZ; beklenen değerler SABİT metin/sayıdır. Fikstür fişi 10.000 ₺ (tek kalem, Nakit, Peşin).
// Sunucu kıyasları (kaynak): TevsikKapisi.Dogrula Peşin → olcut > Had (katı büyük);
// MasakKapisi Peşin → MasakKuralMotoru.Gereksinim: t >= KimlikEsigi (büyük-eşit).
// ════════════════════════════════════════════════════════════════════════
public sealed partial class SATIS3EkranTestleri
{
    private const string PesinKilitTr = "Bu tutarda Peşin Müşteri olmaz — müşteri seçin.";
    private const string PesinNakitTr = "Müşteri seçin ve ödemeyi kart ya da havaleyle alın.";

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DOG_T24_had_bir_kurus_ALTINDA_fis_ustte_Pesin_ve_nakit_engeli(string mod)
    {
        var e = T24Fis(mod, 9999.99m);   // 10.000,00 > 9.999,99 → sunucu reddeder
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.Equal(PesinNakitTr, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#islem-pesin-notu"));
        Assert.Equal(PesinNakitTr, e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: tezgâhta Kaydet'in yanındaki tek cümle
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DOG_T24_MASAK_esigi_TAM_ESIT_Pesin_engeli_buyuk_esit(string mod)
    {
        var e = T24Fis(mod, 50000m, kimlikEsigi: 10000m);   // 10.000 >= 10.000 → kademe YalnizKimlik → sunucu reddeder
        e.WaitForAssertion(() => Assert.Equal("10.000\u00a0\u20ba ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin.", GA.Kaydet(e).GetAttribute("title")), Bekle);   // YASAL-KAPI-GERI: kilit cümlesi tutan eşiği söyler
        GA.KimlikFormuYok(e);   // TEMA TEMİZLİĞİ: tezgâhta "+ Kimlik bilgisi" de yok
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DOG_T24_MASAK_esigi_bir_kurus_USTUNDE_Pesin_serbest(string mod)
    {
        var e = T24Fis(mod, 50000m, kimlikEsigi: 10000.01m);   // 10.000 < 10.000,01 → kademe Yok
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        // TEMA TEMİZLİĞİ 17.10: "#islem-pesin-notu" (eski fiş üst satırının Peşin ipucu) tezgâhta çizilmez — engel yok: not satırı kötü değil.
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);
        Assert.Empty(e.FindAll("#islem-yasal-kapi"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DOG_T24_cok_buyuk_had_999_999_999_99_Pesin_serbest(string mod)
    {
        var e = T24Fis(mod, 999_999_999.99m, kimlikEsigi: 999_999_999.99m);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);   // TEMA TEMİZLİĞİ: #islem-pesin-notu tezgâhta yok
    }

    [Fact]
    public void DOG_T24_EN_metinleri_TR_ile_AYNI_degil_ve_sabit()
    {
        Assert.Equal(PesinNakitTr, Satis3Sozlugu.Tr.PesinVeNakitAl);
        Assert.Equal(PesinKilitTr, Satis3Sozlugu.Tr.PesinTutarKilit);
        Assert.Equal("Select a customer and take the payment by card or bank transfer.", Satis3Sozlugu.En.PesinVeNakitAl);
        Assert.NotEqual(Satis3Sozlugu.Tr.KimlikVeNakitOde, Satis3Sozlugu.En.KimlikVeNakitOde);
    }
}

public sealed partial class SATIS3EkranTestleri
{
    /// <summary>DOĞRULAYICI SONDASI (ORTA bulgu kanıtı): Peşin + TAKAS + KART, fark (5.000) had (9.000) altında ama
    /// Max(eski, yeni) = 10.000 > had. Sunucu MasakKapisi.DogrulaKumulatif Peşin dalında `tutar > tevsik.Had` ile
    /// (tutar = Max(eski,yeni)) REDDEDER. İstemcinin t24 tek-kaynak kuralı bunu önceden söylemeli.</summary>
    [Theory(Skip = "ORTA bulgu ACIK (dogrulayici pSATIS-3 t24, 30.09): istemci Pesin TAKAS kuralinda Max(eski,yeni) > had kiyasini yapmiyor; kapaninca Skip kaldirilir")]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DOG_T24_SONDA_Pesin_TAKAS_kart_deger_hadde_ustu_fark_alti_sunucu_reddeder(string mod)
    {
        tevsikJson = TevsikJson(9000m);
        var e = TakasDolu(mod, "5000");
        GA.MusteriSec(e, "pm1");
        Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari"));
        if (e.FindAll("#islem-odeme").Count > 0) GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled"),
            "Kaydet ETKİN; title=" + GA.Kaydet(e).GetAttribute("title")), Bekle);
    }
}
