using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-370 tur 3 — doğrulayıcı ORTA: Peşin kapısı istemcide kimlikEsigi'ne (370.000) bakıyor,
// sunucu (MasakKapisi Peşin K2) KimlikIstenenEnDusukTutar (200.000) ile reddediyordu. 250.000'lik Peşin
// fişte ekran engel göstermiyor, kayıt bir red turu yiyordu.
// Ölçekli model (fikstür ürünü 10.000 TL): kimlikEsigi 20.000 (≈ 370.000, fişin ÜSTÜ) · amaç beyanı
// 5.000 (≈ 200.000, fişin ALTI) · sunucu kimlikIstenenEnDusukTutar 5.000. Eşik sayıları sunucu yanıtından.
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class SATIS3EkranTestleri
{
    private const string Masak370OlcekliEsik =
        """{"kimlikEsigi":20000,"kimlikIstenenEnDusukTutar":5000,"amacBeyaniEsigi":5000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""";

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void MASAK370_Pesin_kimlik_esiginin_ALTINDA_ama_amac_kademesi_USTUNDE_engel_TEK_cumle(string mod)
    {
        tevsikJson = TevsikJson(50000m);                 // nakit haddi fişin üstü: tek engel MASAK
        masakEsikJson = Masak370OlcekliEsik;
        var e = TekKalemli(mod);
        e.WaitForAssertion(() => Assert.Equal(YkgKimlik(5000m), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        GA.KimlikFormuYok(e);   // Peşin'de kimlik kutusu yok; müşteri seçilmeli (TEMA TEMİZLİĞİ: tezgâhta "+ Kimlik bilgisi" de yok)
    }

    // Gereksinim ucu DÜŞERSE (masakHata) karar istemcinin bildiği tutara kalır: kimliğin FİİLEN istendiği tutar
    // (5.000 ≈ 200.000) aşıldıysa Kaydet kilitli ve gerçek sebep yazılı (fail-closed). Eski kod kimlikEsigi'ne
    // (20.000 ≈ 370.000) bakıp Kaydet'i açıyordu; sunucu da 400 dönüyordu.
    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void MASAK370_gercek_musteride_gereksinim_ucu_DUSERSE_amac_kademesi_ustunde_Kaydet_KILITLI(string mod)
    {
        tevsikJson = TevsikJson(50000m);
        masakEsikJson = Masak370OlcekliEsik;
        gereksinimOzel = _ => "bozuk-yanit";               // uç bozuk yanıt → istemci masakHata
        var e = TekKalemli(mod);
        GA.MusteriSec(e, "c1");
        e.WaitForAssertion(() => Assert.Equal(S3.MasakGereksinimHata, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
    }
}
