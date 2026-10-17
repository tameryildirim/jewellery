using System.Globalization;
using Bunit;
using AngleSharp.Dom;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 t24 — orkestratör 02:30 (2 tur kuralı istisnası) · tasarım hakemi KRİTİK:
// Peşin Müşteri + yüksek tutarda ekran "Peşin satış: müşteri adı gerekmez." ile "Bu tutarda Peşin Müşteri
// olmaz — müşteri seçin." cümlelerini AYNI ANDA gösteriyordu; engeller tek tek ortaya çıkıyordu.
// Kural: yasal kapı hakkında ekranda aynı anda TEK, tutarlı mesaj; kaynak sunucunun kuralı (Peşin K2:
// fiş > tevsik haddi ya da ≥ MASAK kimlik eşiği · nakit kuralı). Her tutar aralığında (had altı, hadde eşit,
// had üstü; nakit/kart; MASAK üstü; gerçek müşteri) BASİT ve PRO'da görünen yasal mesaj ENVANTERİ çıkarılır.
// Ürün fiyatı 10.000 (fikstür); had sahte /tevsik/gunluk yanıtıyla ayarlanır — eşik sayısı testte sunucudan.
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class SATIS3EkranTestleri
{
    private string masakEsikJson = MasakEsik(200000m);

    private static string MasakEsik(decimal k)
    {
        var s = k.ToString(CultureInfo.InvariantCulture);
        return $$"""{"kimlikEsigi":{{s}},"amacBeyaniEsigi":{{s}},"beyanFormuEsigi":{{s}},"ekBelgeEsigi":{{s}},"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""";
    }

    /// <summary>Görsel olarak gizli mi (kendisi ya da atası: hidden, gorunmez, fis-not-gizli, gorsel-gizli).</summary>
    private static bool Gorunur(IElement? el)
    {
        if (GA.TezgahtaGizli(el)) return false;   // TEMA TEMİZLİĞİ: Yıldız tezgâhında display:none olan özgün parçalar
        for (var x = el; x is not null; x = x.ParentElement)
        {
            if (x.HasAttribute("hidden")) return false;
            var c = x.ClassList;
            if (c.Contains("gorunmez") || c.Contains("fis-not-gizli") || c.Contains("gorsel-gizli")) return false;
        }
        return el is not null;
    }

    /// <summary>Ekranda GÖRÜNEN yasal kapı mesajları (seçici → metin).</summary>
    private static List<(string Yer, string Metin)> YasalEnvanter(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        var l = new List<(string, string)>();
        // TEMA TEMİZLİĞİ 17.10: satış tezgâhının yasal cümle yerleri de (not satırı, hata satırı, uyarı satırları) envantere girer.
        foreach (var sec in new[] { "#islem-yasal-kapi", "#islem-pesin-notu", "#islem-tevsik-durum-metin", "#islem-kaydet-kilit", "#islem-pesin-esik", "#islem-pesin-hata",
                                    "#yildiz-tezgah-not", "#yildiz-tezgah-hata", "[id^=yildiz-tezgah-uyari-]" })
            foreach (var el in e.FindAll(sec))
                if (Gorunur(el)) l.Add((sec, el.TextContent.Trim()));
        // Kimlik formu: tezgâhta "+ Kimlik bilgisi" düğmesi (çekmece kapalıyken de) formun açıldığı anlamına gelir.
        if (e.FindComponents<MasakFormu>().Count > 0 || e.FindAll("#yildiz-tezgah-kimlik").Count > 0) l.Add(("MasakFormu", "kimlik"));
        return l;
    }

    private static readonly string[] PesinSerbestMetinleri = [SatisEkraniSozlugu.Tr.PesinKisa, PesinMusteriSozlugu.Tr.SeciliNot];
    private static readonly string[] PesinEngelMetinleri = [Satis3Sozlugu.Tr.PesinTutarKilit, Satis3Sozlugu.Tr.PesinVeNakitAl, SatisAldimSozlugu.Tr.DurumKimlik];

    /// <summary>Çelişen çift: "müşteri adı gerekmez" türü ipucu + Peşin engeli; ya da Peşin engeli + MASAK kimlik kutusu.</summary>
    private static int CelisenCift(List<(string Yer, string Metin)> env)
    {
        var serbest = env.Any(m => PesinSerbestMetinleri.Contains(m.Metin));
        var engel = env.Any(m => PesinEngelMetinleri.Contains(m.Metin) || YkgPesinKilidiMi(m.Metin));   // YASAL-KAPI-GERI: eşikli Peşin cümleleri
        var masak = env.Any(m => m.Yer == "MasakFormu");
        return (serbest && engel ? 1 : 0) + (engel && masak ? 1 : 0) + (serbest && masak ? 1 : 0);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> T24Fis(string mod, decimal had, decimal? kimlikEsigi = null)
    {
        tevsikJson = TevsikJson(had);
        if (kimlikEsigi is { } k) masakEsikJson = MasakEsik(k);
        return TekKalemli(mod);
    }

    private static string Ozet(List<(string Yer, string Metin)> env) => string.Join(" | ", env.Select(m => $"{m.Yer}={m.Metin}"));

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_had_ALTI_Pesin_serbest_engel_mesaji_YOK_Kaydet_ACIK(string mod)
    {
        var e = T24Fis(mod, 50000m);   // fiş 10.000 < had 50.000
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        Assert.DoesNotContain(env, m => PesinEngelMetinleri.Contains(m.Metin) || YkgPesinKilidiMi(m.Metin));
        Assert.Empty(e.FindAll("#islem-yasal-kapi"));
        // TEMA TEMİZLİĞİ 17.10: "#islem-pesin-notu" (eski fiş üst satırının görsel gizli Peşin ipucu) tezgâhta çizilmez —
        // tezgâhın not satırı engel değil yasal durumu söyler.
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);
        Assert.StartsWith(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent.Trim());
        Assert.Equal(0, CelisenCift(env));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_hadde_ESIT_sunucu_gibi_buyuktur_kiyasi_Pesin_serbest(string mod)
    {
        var e = T24Fis(mod, 10000m);   // fiş = had → sunucu (olcut > Had) kabul eder
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        Assert.DoesNotContain(env, m => PesinEngelMetinleri.Contains(m.Metin) || YkgPesinKilidiMi(m.Metin));
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);   // TEMA TEMİZLİĞİ: #islem-pesin-notu tezgâhta yok
        Assert.Equal(0, CelisenCift(env));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_had_USTU_NAKIT_iki_engel_TEK_cumlede_musteri_adi_gerekmez_YOK_istek_YOK(string mod)
    {
        var e = T24Fis(mod, 5000m);   // fiş 10.000 > had 5.000 · ödeme Nakit
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        var env = YasalEnvanter(e);
        Assert.Empty(e.FindAll("#islem-pesin-notu"));            // genel ipucu eşik üstünde HİÇ yok (DOM dahil)
        Assert.All(e.FindAll("#islem-tevsik-durum-metin"), d => Assert.False(Gorunur(d)));   // ödeme sütununda ikinci GÖRÜNEN durum satırı yok ("Neden?" ayrıntısı kalır)
        Assert.Equal(S3.PesinVeNakitAl, GA.Kaydet(e).GetAttribute("title"));
        var metinler = env.Select(m => m.Metin).Distinct().ToList();
        Assert.True(metinler.Count == 1, Ozet(env));             // ekranda TEK yasal cümle
        Assert.Equal(S3.PesinVeNakitAl, metinler[0]);
        Assert.Equal(S3.PesinVeNakitAl, e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: iki modda tek yer tezgâhın not satırı
        Assert.Equal(0, CelisenCift(env));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_had_USTU_KART_yalniz_Pesin_engeli_tek_cumle(string mod)
    {
        var e = T24Fis(mod, 5000m);
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.Equal(YkgTevsik(5000m), GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        Assert.Empty(e.FindAll("#islem-pesin-notu"));
        var metinler = env.Select(m => m.Metin).Distinct().ToList();
        Assert.True(metinler.Count == 1 && metinler[0] == YkgTevsik(5000m), Ozet(env));
        Assert.Equal(YkgTevsik(5000m), e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: iki modda tek yer tezgâhın not satırı
        Assert.Equal(0, CelisenCift(env));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_MASAK_kimlik_esigi_USTU_Pesinde_kimlik_kutusu_ACILMAZ_tek_cumle_musteri_secin(string mod)
    {
        var e = T24Fis(mod, 50000m, kimlikEsigi: 5000m);   // nakit haddi altı, MASAK kimlik eşiği üstü
        e.WaitForAssertion(() => Assert.Equal(YkgKimlik(5000m), GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        GA.KimlikFormuYok(e);   // Peşin'de kimlik kutusu anlamsız (sunucu K2 reddeder) — tezgâhta "+ Kimlik bilgisi" de yok
        Assert.Empty(e.FindAll("#islem-pesin-notu"));
        Assert.True(env.Select(m => m.Metin).Distinct().Count() == 1, Ozet(env));
        Assert.Equal(0, CelisenCift(env));
        // gerçek müşteri seçilince Peşin engeli kalkar, kimlik kutusu açılır
        GA.MusteriSec(e, "c1");
        GA.KimlikFormuAc(e);
        Assert.NotEmpty(e.FindComponents<MasakFormu>());
        Assert.DoesNotContain(YasalEnvanter(e), m => PesinEngelMetinleri.Contains(m.Metin) || YkgPesinKilidiMi(m.Metin) || PesinSerbestMetinleri.Contains(m.Metin));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_gercek_musteri_secilince_Pesin_mesajlari_KALKAR_yalniz_nakit_kurali_kalir(string mod)
    {
        var e = T24Fis(mod, 5000m);
        GA.MusteriSec(e, "c1");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        Assert.DoesNotContain(env, m => PesinEngelMetinleri.Contains(m.Metin) || PesinSerbestMetinleri.Contains(m.Metin));
        Assert.Equal(0, CelisenCift(env));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T24_gercek_musteride_MASAK_kimligi_eksik_VE_nakit_once_odeme_turu_form_ACILMAZ(string mod)
    {
        gereksinimOzel = _ => GereksinimKimlik;   // sunucu: kimlik gerekir
        var e = T24Fis(mod, 5000m, kimlikEsigi: 5000m);
        GA.MusteriSec(e, "c1");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // YASAL-KAPI-3 tur 2 (tasarım hakemi KRİTİK "nakitte form doldurtup Kaydet'te reddediyor"): nakit kuralı tetikliyken
        // kimlik formu AÇILMAZ — tek engel nakit cümlesi (önce ödeme türü); form karta geçince açılır.
        var cumle = string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m));
        e.WaitForAssertion(() => Assert.Equal(cumle, GA.Kaydet(e).GetAttribute("title")), Bekle);
        var env = YasalEnvanter(e);
        Assert.True(env.Count == 1, Ozet(env));   // TEK görünen cümle (görünmez kopya da yok)
        GA.KimlikFormuYok(e);
        Assert.Equal(cumle, e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: iki modda tek yer tezgâhın not satırı
        Assert.All(e.FindAll("#islem-tevsik-durum-metin"), d => Assert.False(Gorunur(d)));
        // karta geçince nakit engeli kalkar → yalnız kimlik engeli (YASAL-PARAM-EKRAN: tetikleyen eşikle)
        GA.OdemeSec(e, "KART");
        GA.KimlikFormuAc(e);   // YASAL-KAPI-3: form karta geçince açılır (tezgâhta "+ Kimlik bilgisi" çekmecesi)
        GA.MasakBelgeNoSil(e);   // SATIS-4 (b): kimlik müşteriden dolar — "kimlik eksik" hâlini kurmak için belge no silinir
        // YASAL-KAPI-2 (tasarım hakemi "aynı uyarı 2-3 kez"): BASİT'te kimlik formu açıkken eşik form başlığında TEK kez;
        // kilit sebebi ne yapılacağını söyler. Tur 2 (doğrulayıcı KRİTİK): PRO'da da eşik SAYISI kilitte yok — kademe
        // "Yasal kapılar" panelinde (YASAL-PARAM-EKRAN tur 3 kararının tek yeri).
        // P-TEZGAH-TUR-KIMLIK K4 (tur 2, doğrulayıcı DÜŞÜK "ölü değişken"): genel cümle yerine EKSİK ALANIN ADI (iki modda da) —
        // belge no silindi → kilit "Kaydetmek için eksik: <belge türünün no etiketi>" der; etiket formdakiyle AYNI kaynaktan.
        var belgeTuru = e.FindComponent<MasakFormu>().Instance.Model.BelgeTuru;
        var belgeNoAdi = MasakFormu.BelgeNoEtiketiFor(belgeTuru, Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr);
        e.WaitForAssertion(() => PTezgahTurKimlikYardim.EksikKilit(GA.Kaydet(e).GetAttribute("title"), belgeNoAdi), Bekle);
    }

    [Fact]
    public void T24_Sozluk_TR_EN_dolu_buyuk_harf_vurgusu_YOK()
    {
        foreach (var s in new[] { Satis3Sozlugu.Tr, Satis3Sozlugu.En })
            foreach (var m in new[] { s.PesinVeNakitAl, s.PesinVeNakitOde, s.KimlikVeNakitAl, s.KimlikVeNakitOde })
            {
                Assert.False(string.IsNullOrWhiteSpace(m));
                Assert.DoesNotMatch(@"\b\p{Lu}{3,}\b", m);
                Assert.DoesNotMatch(@"\d", m);   // eşik sayısı metne gömülmez
            }
    }
}
