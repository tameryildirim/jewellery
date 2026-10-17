using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI — eşiğe bağlı ekran durumu her fiş değişiminde yeniden değerlendirilir.
// Kurucu (09.10): "toplam tutarı 200.000 üzerine çıkardım sonra ürünlerden kaldırdım
// 200.000'in altına indirdim algılamadı".
// Sınanan: (1) sunucu MASAK reddinin açtığı form/kilit/bant/hata, kalem silinip tutar reddin
// altına inince KAPANIR; tutar geri çıkınca GERİ gelir ve yazılan beyan kaybolmaz;
// (2) eşik altında POST gövdesinde masak beyanı YOK; (3) Peşin kilidi hangi eşik tutuyorsa onu
// söyler (yukarı-aşağı-yukarı, tam eşikler); (4) Hurda satışında aynı sınıf; (5) iz kuralı (saf).
// ⚠ Eşik sayıları testte ölçeklidir (sunucudan gelir — istemciye gömülmez).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Saf: reddin izi kuralı.</summary>
public sealed class YasalKapiGeriIzTestleri
{
    [Fact]
    public void YKG_iz_yukari_asagi_yukari_ayni_musteride_gecerli_altinda_gecersiz()
    {
        var iz = YasalKapiIzi.Al(230_000m, "c1");
        Assert.True(iz.Gecerli(230_000m, "c1"));      // reddin ölçüsünde
        Assert.True(iz.Gecerli(250_000m, "c1"));      // üstünde
        Assert.False(iz.Gecerli(229_999.99m, "c1"));  // bir kuruş altında → düşer
        Assert.False(iz.Gecerli(150_000m, "c1"));     // kalem silindi
        Assert.True(iz.Gecerli(230_000m, "c1"));      // geri çıktı → geri gelir (iz silinmedi)
    }

    [Fact]
    public void YKG_iz_musteri_degisince_ya_da_iz_yoksa_gecersiz()
    {
        var iz = YasalKapiIzi.Al(-50_000m, "");       // iade/negatif tutar mutlak ölçülür; boş müşteri = müşterisiz
        Assert.True(iz.Gecerli(50_000m, null));
        Assert.False(iz.Gecerli(50_000m, "c2"));
        Assert.False(YasalKapiIzi.Gecerli(null, 1_000_000m, "c1"));
        Assert.True(YasalKapiIzi.Gecerli(iz, -60_000m, ""));
    }
}

public sealed partial class SATIS3EkranTestleri
{
    private const string YkgMasakRed =
        """{"message":"MASAK: 15.000,00 TL ve üstü işlemde kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter","alanlar":{"masak.aciklama":["en az 20 karakter"]},"statusCode":400}""";
    private const string YkgBeyan = "Kızının düğünü için bilezik alıyor";

    /// <summary>Elle fiyatlı kalem (u2 — sunucuda fiyat listesi yok).</summary>
    private static void YkgElleKalem(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string fiyat)
    {
        SeciciYardim.Sec(e, "islem-urun", "u2");
        e.Find("#islem-birimfiyat").Input(fiyat);
        e.Find("#islem-kalem-ekle").Click();
    }

    /// <summary>TEMA TEMİZLİĞİ 17.10: tezgâhta kalem satıra dokunup "Çıkar"la silinir (sayfanın KalemSil'i — özgün ✕ ile AYNI).</summary>
    private static void YkgKalemSil(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, int ix) => GA.KalemCikar(e, ix);

    private static int YkgKalemSayisi(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => GA.KalemSayisi(e);

    /// <summary>Gerçek müşteri c1 · u1 10.000 + u2 3.000 = 13.000 (eşik 15.000 altı) → Kaydet → sunucu MASAK reddi (bağlantılı toplam).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> YkgReddedilenFis(string mod)
    {
        masakEsikJson = MasakEsik(15000m);
        // sunucu: fiş açılırken c1'in bağlantılı toplamı eşik altı; Kaydet anında (başka kasada araya giren işlemle)
        // 13.000'lik fiş eşiği aşar — 3.000'lik fişle yine eşik altı (sunucunun güncel toplamı)
        var araIslemVar = false;
        gereksinimOzel = v => araIslemVar && YkgSorguTutari(v) >= 13000m ? GereksinimAmac : null;
        var e = TekKalemli(mod);
        GA.MusteriSec(e, "c1");
        YkgElleKalem(e, "3000");
        e.WaitForAssertion(() => Assert.Equal(2, YkgKalemSayisi(e)), Bekle);
        postRed = ("/islemler/satis", YkgMasakRed);
        araIslemVar = true;
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        GA.KimlikFormuAc(e);   // red alanları açtı (TEMA TEMİZLİĞİ: tezgâhta "+ Kimlik bilgisi" çekmecesi)
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-tezgah-hata")), Bekle);   // tezgâhın hata satırı (özgün #hata-bandi display:none)
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        e.Find("#masak-aciklama").Input(YkgBeyan);
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG01_MASAK_reddinden_sonra_kalem_silinip_tutar_inince_form_kilit_bant_hata_KALKAR(string mod)
    {
        var e = YkgReddedilenFis(mod);
        Assert.NotEmpty(e.FindAll(".kart--masak"));   // YASAL-KAPI-3 tur 2: form açıkken "gereken alanlar açıldı" bandı yok — form başlığı söyler
        YkgKalemSil(e, 0);   // u1 (10.000) çıkar → fiş 3.000, bağlantılı toplam eşik altı
        e.WaitForAssertion(() => Assert.Equal(1, YkgKalemSayisi(e)), Bekle);
        e.WaitForAssertion(() => GA.KimlikFormuYok(e), Bekle);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Empty(e.FindAll("#hata-bandi, #yildiz-tezgah-hata"));
        Assert.Empty(e.FindAll("#islem-yasal-kapi"));
    }

    private static decimal YkgSorguTutari(string yol)
    {
        var m = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
        return m.Success ? decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG07_MASAK_reddinden_sonra_fis_ISKONTOSU_tutari_dusurunce_sunucu_ret_bandi_ve_form_KALKAR(string mod)
    {
        // Kalem silmenin aksine iskonto hata bandına dokunmaz — ret bandını iz kuralı (YasalIzleriTazele) indirir.
        var e = YkgReddedilenFis(mod);
        GA.IndirimAlani(e).Input("50");   // 13.000 → 6.500 (TEMA TEMİZLİĞİ: tezgâhın "+ İndirim" hapı — FisIskontoSatiri AYNI)
        e.WaitForAssertion(() => GA.KimlikFormuYok(e), Bekle);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hata-bandi, #yildiz-tezgah-hata")), Bekle);
        Assert.Empty(e.FindAll("#islem-yasal-kapi"));
        e.Find("#islem-fis-iskonto").Input("");                // iskonto kalkar → 13.000: form geri, beyan yerinde
        GA.KimlikFormuAc(e);
        Assert.Equal(YkgBeyan, e.Find("#masak-aciklama").GetAttribute("value"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG02_asagi_sonra_tekrar_YUKARI_form_geri_gelir_yazilan_beyan_KAYBOLMAZ(string mod)
    {
        var e = YkgReddedilenFis(mod);
        YkgKalemSil(e, 0);
        e.WaitForAssertion(() => GA.KimlikFormuYok(e), Bekle);
        SeciciYardim.Sec(e, "islem-urun", "u1");   // aynı tutara geri çık (13.000)
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        GA.KimlikFormuAc(e);
        Assert.Equal(YkgBeyan, e.Find("#masak-aciklama").GetAttribute("value"));
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));   // amaç hâlâ eksik → kilitli (sunucuyla aynı)
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG03_esik_ALTINDA_POST_govdesinde_masak_beyani_YOK(string mod)
    {
        var e = YkgReddedilenFis(mod);
        YkgKalemSil(e, 0);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        postRed = null;
        var once = istekler.Count(i => i.Metot == "POST");
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.True(istekler.Count(i => i.Metot == "POST") > once), Bekle);
        var govde = SonPost("/islemler/satis").Govde;
        using var d = JsonDocument.Parse(govde);
        Assert.False(d.RootElement.TryGetProperty("masak", out var m) && m.ValueKind != JsonValueKind.Null, govde);
        Assert.DoesNotContain(YkgBeyan, govde);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG08_Pesin_sunucu_reddi_bandi_tutar_inince_KALKAR_cikinca_GERI_gelir(string mod)
    {
        // Had bilinmiyor (sunucu söylemedi) → istemci kilitlemez, son söz sunucuda: Peşin reddi "gerçek müşteri seçin" bandını açar.
        tevsikJson = TevsikJson(0m);
        var e = TekKalemli(mod);
        YkgElleKalem(e, "3000");
        e.WaitForAssertion(() => Assert.Equal(2, YkgKalemSayisi(e)), Bekle);
        postRed = ("/islemler/satis", """{"message":"Peşin Müşteri ile bu tutarda işlem yapılamaz: 13.000,00 TL, tevsik (459 s. VUK GT) eşiğine (12.000,00 TL) ulaşıyor.","alanlar":{"cariId":["gerçek müşteri"]},"statusCode":400}""");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-tezgah-hata")), Bekle);   // TEMA TEMİZLİĞİ: red tezgâhın hata satırında
        e.Find("#yildiz-tezgah-degistir").Click();             // "gerçek müşteri seçin" bandı tezgâhın müşteri katmanında (aynı id)
        e.WaitForElement("#islem-pesin-esik", Bekle);
        YkgKalemSil(e, 0);                                     // 13.000 → 3.000: reddin altı
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")), Bekle);
        SeciciYardim.Sec(e, "islem-urun", "u1");               // 13.000'e geri
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-pesin-esik", Bekle);
    }

    [Fact]
    public void YKG09_BASIT_nakit_siniri_ustu_otomatik_Kart_ISKONTO_ile_sinir_altina_inince_sunucuya_YENIDEN_sorulur_Nakit_doner()
    {
        // Nakit sınırı (tevsik) kuralının sunucu önizlemesi yalnız kalem ekle/sil/satır düzeltmesinde soruluyordu; fiş iskontosu
        // toplamı düşürünce bayat "kural devrede" kalıyor, ödeme Kart'ta ve "sınırı aştığı için" notu ekranda kalıyordu.
        tevsikJson = """{"had":12000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":12000,"islemTutariKuraliDevrede":true}""";
        var e = TekKalemli(ArayuzModuServisi.Basit);
        GA.MusteriSec(e, "c1");
        YkgElleKalem(e, "5000");                                            // 15.000 > 12.000
        GA.OdemeDuzenAc(e);                                                  // TEMA TEMİZLİĞİ: not tezgâhın ödeme düzeninde
        e.WaitForElement("#islem-odeme-otomatik", Bekle);                    // ödeme otomatik Kart
        Assert.Contains(Sozlukler.Tr.Islem.OdemeKart, e.Find("#yildiz-odeme-0").TextContent);
        tevsikJson = """{"had":12000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":12000,"islemTutariKuraliDevrede":false}""";
        var once = istekler.Count(i => i.Yol.Contains("/tevsik/gunluk"));
        GA.IndirimAlani(e).Input("50");                                     // 7.500 < 12.000
        e.WaitForAssertion(() => Assert.True(istekler.Count(i => i.Yol.Contains("/tevsik/gunluk")) > once), Bekle);   // yeni toplamla soruldu
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-odeme-otomatik")), Bekle);                            // Nakit'e döndü
        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-odeme-duzen"));                                                    // düzen açık — boş DOM değil
        e.WaitForAssertion(() => Assert.Contains(Sozlukler.Tr.Islem.OdemeNakit, e.Find("#yildiz-odeme-0").TextContent), Bekle);
        Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk") && i.Yol.Contains("fisToplamTl=7500"));
    }

    // ── Peşin kilidinin cümlesi hangi eşiğin tuttuğunu söyler (sunucu sırası: MASAK, sonra tevsik) ──

    // Doğrulayıcı (tur 3, DÜŞÜK): beklenti üretim sözlüğünden HESAPLANMAZ — sabit literal (sözlük metni değişirse test kırmızı olmalı).
    private const string YkgKimlik15000 = "15.000\u00a0\u20ba ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin.";
    private const string YkgTevsik5000 = "5.000\u00a0\u20ba üstündeki işlem müşteri adına yazılır — Peşin Müşteri olmaz, müşteri seçin.";
    /// <summary>Diğer YKG/T24 sınıflarının kullandığı biçim: metin SABİT (sözlükten okunmaz), yalnız tutar tr-TR N0 biçimlenir.</summary>
    internal static string YkgKimlik(decimal esik) =>
        esik.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + "\u00a0\u20ba ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin.";
    internal static string YkgTevsik(decimal had) =>
        had.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + "\u00a0\u20ba üstündeki işlem müşteri adına yazılır — Peşin Müşteri olmaz, müşteri seçin.";
    /// <summary>Eşikli Peşin kilit cümlesi mi (iki kademe de aynı sonla biter) — T24 envanter testleri için.</summary>
    private static bool YkgPesinKilidiMi(string m) =>
        m.EndsWith(" — Peşin Müşteri olmaz, müşteri seçin.", StringComparison.Ordinal);

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YKG04_Pesin_yukari_asagi_yukari_kilit_cumlesi_ESIGI_izler(string mod)
    {
        tevsikJson = TevsikJson(5000m);
        masakEsikJson = MasakEsik(15000m);
        var e = TekKalemli(mod);                 // 10.000 — tevsik üstü, kimlik altı
        e.WaitForAssertion(() => Assert.Equal(YkgTevsik5000, GA.Kaydet(e).GetAttribute("title")), Bekle);
        YkgElleKalem(e, "8000");                 // 18.000 — kimlik kademesi
        e.WaitForAssertion(() => Assert.Equal(YkgKimlik15000, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Equal(YkgKimlik15000, e.Find("#yildiz-tezgah-not").TextContent.Trim());   // TEMA TEMİZLİĞİ: tezgâhın not satırı (iki modda)
        YkgKalemSil(e, 1);                        // 10.000 — aşağı: cümle tevsike döner
        e.WaitForAssertion(() => Assert.Equal(YkgTevsik5000, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Equal(YkgTevsik5000, e.Find("#yildiz-tezgah-not").TextContent.Trim());
        YkgElleKalem(e, "8000");                 // yukarı yine
        e.WaitForAssertion(() => Assert.Equal(YkgKimlik15000, GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.KimlikFormuYok(e);   // Peşin'de kimlik kutusu yok (değişmedi) — tezgâhta "+ Kimlik bilgisi" de yok
    }

    [Theory]
    [InlineData("4999.99", false, false)]   // tevsik haddi 5.000 altı: Peşin serbest
    [InlineData("5000", false, false)]      // tam had: sunucu "had ÜSTÜ" der (olcut > had) → serbest
    [InlineData("5000.01", true, false)]    // had üstü → tevsik cümlesi
    [InlineData("14999.99", true, false)]   // kimlik eşiği bir kuruş altı → hâlâ tevsik
    [InlineData("15000", true, true)]       // tam kimlik eşiği (≥) → kimlik cümlesi
    public void YKG05_Pesin_TAM_ESIKLER_sunucu_kurali_ile_ayni(string fiyat, bool kilitli, bool kimlik)
    {
        tevsikJson = TevsikJson(5000m);
        masakEsikJson = MasakEsik(15000m);
        var e = Ekran(ArayuzModuServisi.Basit);
        YkgElleKalem(e, fiyat);
        e.WaitForAssertion(() => Assert.Equal(1, YkgKalemSayisi(e)), Bekle);
        if (!kilitli)
        {
            e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
            Assert.DoesNotContain("Peşin", GA.Kaydet(e).GetAttribute("title") ?? "");
            return;
        }
        var beklenen = kimlik ? YkgKimlik15000 : YkgTevsik5000;
        e.WaitForAssertion(() => Assert.Equal(beklenen, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
    }
}

public sealed partial class ZOISLEMAEkranTestleri
{
    // Hurda satışı: MASAK reddi (bağlantılı toplam) alanları açar; has azaltılıp tutar reddin altına inince
    // alanlar ve not kapanır, geri çıkınca geri gelir — yazılan açıklama kaybolmaz.
    [Fact]
    public void YKG06_hurda_MASAK_reddi_sonrasi_tutar_inince_alanlar_KAPANIR_cikinca_GERI_gelir()
    {
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim", MasakYalnizKimlik);
        Yol("/islemler/hurda-satis",
            """{"message":"MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter","alanlar":{"masak.aciklama":["en az 20 karakter"]},"statusCode":400}""",
            HttpStatusCode.BadRequest);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "9");                       // 61.200 — eşik altı
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForElement("#hrd-masak-not");
        Gir(e, "#hrd-has", "5");                       // aşağı: reddin tutarının altı
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-masak-not")), TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#hrd-masak"));
        Gir(e, "#hrd-has", "9");                       // yukarı: reddin tutarına geri
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-masak-not")), TimeSpan.FromSeconds(5));
    }
}
