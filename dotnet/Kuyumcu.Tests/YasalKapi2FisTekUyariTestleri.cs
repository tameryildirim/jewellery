using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 tur 2 — tasarım hakemi KRİTİK ×3 (fiş):
//  (1) Nakit seçili ve nakit kuralı tetikliyken kimlik formu AÇILMAZ (önce ödeme türü); karta/havaleye geçince açılır,
//      nakde dönünce yine kapanır — her değişimde yeniden değerlendirilir.
//  (2) Aynı yasal uyarı TEK kez: hakemin sayacı görünmez (gorunmez/gorsel-gizli) kopyaları da saydığı için envanter
//      bileşenin TÜM METNİ üzerinden çıkarılır (öznitelikler — düğmenin title'ı — sayılmaz).
//  (3) PRO: "tek kasa — otomatik seçildi" teknik notu ve "teyit bekleniyor" iç notu yok; panel engeli fişte söylenirken katlı.
// Fikstür: ürün 10.000 ₺ (SATIS3EkranTestleri); had ve MASAK eşiği sahte sunucu yanıtıyla (istemcide sayı yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class SATIS3EkranTestleri
{
    /// <summary>Bileşenin TÜM metni (görünmez/ekran okuyucu kopyaları dahil; öznitelikler hariç). TEMA TEMİZLİĞİ 17.10: Yıldız
    /// tezgâhında display:none olan özgün parçalar (ekran okuyucu da okumaz) hariç — GA.TumMetin.</summary>
    private static string TumMetin(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => GA.TumMetin(e);

    private static int Kac(string metin, string aranan)
    {
        var n = 0;
        for (var i = metin.IndexOf(aranan, StringComparison.Ordinal); i >= 0; i = metin.IndexOf(aranan, i + aranan.Length, StringComparison.Ordinal)) n++;
        return n;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Yk3MusteriliFis(string mod, decimal had, decimal kimlikEsigi)
    {
        gereksinimOzel = _ => GereksinimKimlik;   // sunucu: kimlik gerekir
        var e = T24Fis(mod, had, kimlikEsigi: kimlikEsigi);
        GA.MusteriSec(e, "c1");
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YK3_F01_nakitte_kimlik_formu_ACILMAZ_kart_havale_acar_nakde_donunce_kapanir_uyari_TEK(string mod)
    {
        var e = Yk3MusteriliFis(mod, 5000m, 5000m);   // fiş 10.000: nakit kuralı + kimlik eşiği
        GA.NakitSec(e);
        var cumle = string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m));     // "5.000 ₺ üstündeki fişte nakit kullanılamaz — …" (had sunucudan)
        e.WaitForAssertion(() => Assert.Equal(cumle, GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.KimlikFormuYok(e);
        Assert.Empty(e.FindAll("[id^=masak-]"));
        var metin = TumMetin(e);
        Assert.Equal(1, Kac(metin, cumle));                                  // tek cümle, görünmez kopya da yok
        Assert.Equal(cumle, e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: iki modda tek yer tezgâhın not satırı (Kaydet'in yanı)
        Assert.Equal(0, Kac(metin, S3.KimlikVeNakitAl));                     // "kimlik bilgilerini girin" denmez
        Assert.Equal(0, Kac(metin, SatisAldimSozlugu.Tr.Neden));            // ikinci "Neden?" yok
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));

        var ilk = true;
        foreach (var (tur, form) in new[] { ("KART", true), ("NAKIT", false), ("HAVALE", true), ("NAKIT", false) })
        {
            GA.OdemeSec(e, tur);
            if (form)
            {
                GA.KimlikFormuAc(e);                                            // TEMA TEMİZLİĞİ: "+ Kimlik bilgisi" çekmecesi
                Assert.NotEmpty(e.FindComponents<MasakFormu>());
                Assert.Equal(0, Kac(TumMetin(e), cumle));                       // nakit hatası değişimde KALKAR
                if (ilk) { GA.MasakBelgeNoSil(e); ilk = false; }      // kimlik eksik hâli: Kaydet kilitli
                e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
                Assert.False(string.IsNullOrWhiteSpace(GA.Kaydet(e).GetAttribute("title")));   // sebep düğmede (UX-S30)
                PTezgahTurKimlikYardim.EksikKilit(e.Find("#yildiz-tezgah-not").TextContent.Trim());   // P-TEZGAH-TUR-KIMLIK K4: kilit satırı Kaydet'in yanında eksik alanın adını söyler
                Assert.Empty(e.FindAll("#islem-yasal-kapi"));                    // "gereken alanlar açıldı" bandı yok
            }
            else
            {
                e.WaitForAssertion(() => GA.KimlikFormuYok(e), Bekle);
                Assert.Equal(1, Kac(TumMetin(e), cumle));                       // nakde dönünce geri gelir, yine TEK
            }
        }
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit, "9999.99", true)]    // fiş 10.000 > had → nakit kuralı
    [InlineData(ArayuzModuServisi.Basit, "10000", false)]     // fiş = had → serbest (sunucu: toplam > had)
    [InlineData(ArayuzModuServisi.Basit, "10000.01", false)]  // fiş < had → serbest
    [InlineData(ArayuzModuServisi.Profesyonel, "9999.99", true)]
    [InlineData(ArayuzModuServisi.Profesyonel, "10000", false)]
    public void YK3_F02_sinir_had_alti_esit_ustu_nakit_form_ve_engel(string mod, string hadMetin, bool engel)
    {
        var had = decimal.Parse(hadMetin, System.Globalization.CultureInfo.InvariantCulture);
        var e = Yk3MusteriliFis(mod, had, 5000m);
        GA.NakitSec(e);   // TEMA TEMİZLİĞİ 17.10: tezgâhta ödeme türü ödeme satırına dokunarak seçilir (iki modda)
        if (engel)
        {
            e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(had)), GA.Kaydet(e).GetAttribute("title")), Bekle);
            GA.KimlikFormuYok(e);
        }
        else
        {
            GA.KimlikFormuAc(e);   // nakit serbest → kimlik formu açılır ("+ Kimlik bilgisi" çekmecesi)
            Assert.NotEmpty(e.FindComponents<MasakFormu>());
            Assert.DoesNotContain("nakit kullanılamaz", TumMetin(e), StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YK3_F03_Pesin_kart_otomatik_engel_TEK_otomatik_notu_ve_panel_notu_YOK(string mod)
    {
        var e = T24Fis(mod, 5000m);   // Peşin + fiş 10.000 > had → ödeme kendiliğinden kart, Peşin engeli
        e.WaitForAssertion(() => Assert.Equal(YkgTevsik(5000m), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.OdemeDuzenAc(e);   // TEMA TEMİZLİĞİ: otomatik ödeme notu tezgâhın ödeme düzeninde aranır (kapalı düzen yanlış yeşil vermez)
        var metin = TumMetin(e);
        Assert.Equal(1, Kac(metin, YkgTevsik(5000m)));
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
        Assert.Empty(e.FindAll("#islem-pro-nakit-not"));
        Assert.Equal(0, Kac(metin, SatisAldimSozlugu.Tr.Neden));
    }

    [Fact]
    public void YK3_F04_PRO_panel_engel_fiste_soylenirken_KATLI_ic_not_YOK()
    {
        var e = Yk3MusteriliFis(ArayuzModuServisi.Profesyonel, 5000m, 5000m);
        GA.NakitSec(e);
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), GA.Kaydet(e).GetAttribute("title")), Bekle);
        // TEMA TEMİZLİĞİ 17.10: PRO "Yasal kapılar" yan paneli (#islem-pro-yasal-ayrinti) satışta çizilmez — engel tezgâhın not
        // satırında TEK kez söylenir; iç not ("teyit") ve teknik "tek kasa otomatik" notu ödeme düzeni açıkken de yok.
        Assert.Empty(e.FindAll("#islem-pro-yasal-ayrinti"));
        Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), e.Find("#yildiz-tezgah-not").TextContent.Trim());
        GA.OdemeDuzenAc(e);
        Assert.DoesNotContain("teyit", TumMetin(e), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Sozlukler.Tr.Islem.TekKasaOtomatik, TumMetin(e), StringComparison.Ordinal);
    }

    [Fact]
    public void YK3_F05_sozluk_panel_amac_satiri_ic_not_tasimaz_TR_EN()
    {
        Assert.DoesNotContain("teyit", YasalParamSozlugu.Tr.PanelAmac, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("confirmation", YasalParamSozlugu.Al("en").PanelAmac, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{0}", YasalParamSozlugu.Tr.PanelAmac, StringComparison.Ordinal);
    }
}
