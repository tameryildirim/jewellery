using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Web.Pages;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ 17.10 — GB şeridi (İşlemler sınır/hasım testleri) ortak yardımcısı.
//
// SATIŞ fişi artık HER ZAMAN Yıldız tezgâhıdır (Islemler.YildizTezgahMi). Eski Platin satış yüzünün
// giriş kimlikleri (#islem-cari, #islem-kaydet, #islem-kaydet-kilit …) SATIŞ'ta çizilmez; kullanıcının
// gerçek yolu: müşteri "Değiştir" katmanında, ödeme türü ödeme satırına dokununca açılan düzende,
// Kaydet #yildiz-tezgah-kaydet (kilit sebebi title + Fark bloğunun not satırı), öteki türler ve
// "Diğer" işlemler tezgâhın "Diğer" düğmesinin (#yildiz-tezgah-tur) açtığı listeden.
// Kurallar/kapılar/gövde AYNI sayfa durumundan gelir — bu yardımcı yalnız GİRİŞ YOLUNU sürer.
//
// ⚠ SESSİZ GEÇİŞ YOK: beklenen öğe süre içinde çıkmazsa WaitForFailedException (yanlış yeşil üretilmez).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGBYardimcisi
{
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(10);

    /// <summary>Yıldız tezgâhı çizildi ve sistem carisi (Peşin Müşteri) varsayılan müşteri olarak yüklendi.</summary>
    public static IRenderedComponent<Islemler> YildizPesinBekle(this IRenderedComponent<Islemler> e, string pesinAdi = "Peşin Müşteri")
    {
        e.TezgahBekle();   // açılış bitti + renderer kuyruğu boş (TemaTemizligiGDYardimcisi)
        e.WaitForElement("#yildiz-tezgah", Bekle);
        e.WaitForAssertion(() => Assert.Contains(pesinAdi, e.Find("#yildiz-tezgah-ust").TextContent), Bekle);
        return e;
    }

    /// <summary>"Değiştir" katmanından müşteri seçer (YildizMusteriSecici → sayfanın MusteriDegisti'si); katman seçimle kapanır.</summary>
    public static void GbYildizMusteriSec(this IRenderedComponent<Islemler> e, string id)
    {
        e.WaitForElement("#yildiz-tezgah", Bekle);
        if (e.FindAll("#yildiz-tezgah-musteri").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement($"#yildiz-musteri-sec-{id}", Bekle).Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-musteri")), Bekle);
    }

    /// <summary>
    /// Tek ödemenin türünü seçer: ödeme satırına dokunulur (#yildiz-odeme-0 → #yildiz-tezgah-odeme-duzen); yasal kapı açıksa
    /// ya da PRO'da sayfanın seçicisi (#islem-odeme), BASİT kapı kapalıyken satır seçicisi (#islem-aldim-odeme).
    /// </summary>
    public static void YildizOdemeSec(this IRenderedComponent<Islemler> e, string deger)
    {
        e.WaitForElement("#yildiz-tezgah", Bekle);
        if (e.FindAll("#yildiz-tezgah-odeme-duzen").Count == 0) e.Find("#yildiz-odeme-0").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-tezgah-odeme-duzen #islem-odeme, #yildiz-tezgah-odeme-duzen #islem-aldim-odeme")), Bekle);
        SeciciYardim.Sec(e, e.FindAll("#yildiz-tezgah-odeme-duzen #islem-odeme").Count > 0 ? "islem-odeme" : "islem-aldim-odeme", deger);
        e.WaitForAssertion(() => Assert.Equal(deger, SeciciYardim.SeciliDeger(e,
            e.FindAll("#islem-odeme").Count > 0 ? "islem-odeme" : "islem-aldim-odeme")), Bekle);
    }

    /// <summary>Tezgâhın Kaydet düğmesi.</summary>
    public static IElement GbYildizKaydet(this IRenderedComponent<Islemler> e) => e.Find("#yildiz-tezgah-kaydet");

    /// <summary>Tezgâhtaki kalem satırı sayısı.</summary>
    public static int YildizKalemSayisi(this IRenderedComponent<Islemler> e) => e.FindAll(".yz-tezgah-kalem").Count;

    /// <summary>
    /// Satış yüzünden öteki türe geçer: "Diğer" (#yildiz-tezgah-tur) tüm türleri açar → listeden tür (#islem-tur-{tur}).
    /// Tür yüzü (YildizTurYuzu) eski formu çizer; #islem-kaydet beklenir.
    /// </summary>
    public static IRenderedComponent<Islemler> YildizTurGec(this IRenderedComponent<Islemler> e, string tur)
    {
        e.WaitForElement("#yildiz-tezgah", Bekle);
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-turler-acik", e.Find(".islem-sayfa").ClassList), Bekle);
        e.WaitForElement($"#islem-tur-{tur}", Bekle).Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah")), Bekle);
        e.WaitForElement("#islem-kaydet", Bekle);
        return e;
    }

    /// <summary>
    /// "Diğer" işlem paneli (Kapora, Değişim …): tezgâhın "Diğer" düğmesi tür listesini ve PRO "Diğer" işlemlerini açar →
    /// #islem-diger-{kod}. Panel fişin yerinde çizilir (#islem-diger-panel).
    /// </summary>
    public static IRenderedComponent<Islemler> YildizDigerAc(this IRenderedComponent<Islemler> e, string kod)
    {
        e.WaitForElement("#yildiz-tezgah", Bekle);
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-turler-acik", e.Find(".islem-sayfa").ClassList), Bekle);
        e.WaitForElement($"#islem-diger-{kod}", Bekle).Click();
        e.WaitForElement("#islem-diger-panel", Bekle);
        return e;
    }
}
