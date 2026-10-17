using Bunit;
using Kuyumcu.Web.Pages;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ (17.10) — GD şeridi yardımcıları.
//
// SATIŞ fişi artık HEP Yıldız tezgâhıdır (Bilesenler/Yildiz/YildizTezgah.razor). Bu yardımcılar
// kullanıcının Yıldız'daki GERÇEK yolunu sürer; kurallar/kapılar/gövdeler sayfanın aynı
// durumundan gelir (Islemler.Yildiz.cs):
//   • müşteri  : "Değiştir" (#yildiz-tezgah-degistir) → "Seç" (#yildiz-musteri-sec-{id})
//   • ödeme    : ödeme satırına dokun (#yildiz-odeme-0) → sayfanın özgün ödeme parçası
//                (FisOdemeKasa: #islem-odeme, #islem-kasa, #islem-odeme-otomatik …)
//   • karma    : "+ Başka ödeme" (#yildiz-tezgah-baska-odeme) — sayfanın OdemeyiBol'u (2 satır)
//   • kimlik   : "+ Kimlik bilgisi" (#yildiz-tezgah-kimlik) → çekmece (MasakParcasi, aynı kapı)
//   • kaydet   : #yildiz-tezgah-kaydet (kilit sebebi title'da ve #yildiz-tezgah-not satırında)
// ⚠ SESSİZ GEÇİŞ YOK: her adım öğenin çizilmesini bekler (WaitForElement HATA verir).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGDYardimcisi
{
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(10);

    public const string Kaydet = "#yildiz-tezgah-kaydet";
    public const string Kalem = ".yz-tezgah-kalem";

    /// <summary>Yıldız tezgâhı çizilene kadar bekler.</summary>
    public static IRenderedComponent<Islemler> TezgahBekle(this IRenderedComponent<Islemler> e)
    {
        e.WaitForElement("#yildiz-tezgah", Bekle);
        return e;
    }

    /// <summary>
    /// İşlem türü seçer — kullanıcının Yıldız yolu: görünür sekme (#yildiz-tur-{kod}: Satış · Alış · İade) varsa ona,
    /// yoksa "Diğer"e (#yildiz-tezgah-tur / #yildiz-tur-diger) basıp açılan tür ızgarasından (#islem-tur-{kod}) seçer.
    /// İkisi de sayfanın AYNI TurSecDiger yoludur.
    /// </summary>
    public static void TurSec(IRenderedComponent<Islemler> e, string kod)
    {
        if (e.FindAll($"#yildiz-tur-{kod}").Count > 0) { e.Find($"#yildiz-tur-{kod}").Click(); return; }
        var diger = e.FindAll("#yildiz-tezgah-tur").Count > 0 ? "#yildiz-tezgah-tur" : "#yildiz-tur-diger";
        e.WaitForElement(diger, Bekle).Click();
        e.WaitForElement($"#islem-tur-{kod}", Bekle).Click();
    }

    /// <summary>Üst satırdaki seçili müşteri adı (#yildiz-tezgah-ust-musteri); yoksa null.</summary>
    public static string? TezgahMusterisi(IRenderedComponent<Islemler> e) =>
        e.FindAll("#yildiz-tezgah-ust-musteri").FirstOrDefault()?.TextContent.Trim();

    /// <summary>Peşin Müşteri (ya da verilen ad) üst satırda görünene kadar bekler.</summary>
    public static void MusteriBekle(IRenderedComponent<Islemler> e, string ad) =>
        e.WaitForAssertion(() => Assert.StartsWith(ad, TezgahMusterisi(e) ?? ""), Bekle);

    /// <summary>"Değiştir" katmanından müşteri seçer (seçimle katman kapanır — Y2).</summary>
    public static void MusteriSec(IRenderedComponent<Islemler> e, string id)
    {
        e.WaitForElement("#yildiz-tezgah-degistir", Bekle);
        if (e.FindAll("#yildiz-musteri-secici").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement($"#yildiz-musteri-sat-{id}", Bekle);
        if (e.FindAll($"#yildiz-musteri-sec-{id}").Count == 0)
        {
            // zaten seçili: katmanı kapat
            e.Find("#yildiz-tezgah-degistir").Click();
            return;
        }
        e.Find($"#yildiz-musteri-sec-{id}").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-musteri-secici")), Bekle);
    }

    /// <summary>Tek ödeme satırının düzenini açar (sayfanın özgün ödeme parçası çizilir).</summary>
    public static void OdemeAc(IRenderedComponent<Islemler> e)
    {
        if (e.FindAll("#yildiz-tezgah-odeme-duzen").Count > 0) return;
        e.WaitForElement("#yildiz-odeme-0", Bekle).Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen", Bekle);
    }

    /// <summary>Ödeme düzenini açıp tek ödeme türünü (#islem-odeme) seçer.</summary>
    public static void OdemeSec(IRenderedComponent<Islemler> e, string deger)
    {
        OdemeAc(e);
        e.WaitForElement("#islem-odeme", Bekle);
        SeciciYardim.Sec(e, "islem-odeme", deger);
    }

    /// <summary>Ödeme düzeni açıkken seçili tek ödeme türü (#islem-odeme).</summary>
    public static string? OdemeTuru(IRenderedComponent<Islemler> e)
    {
        OdemeAc(e);
        return SeciciYardim.SeciliDeger(e, "islem-odeme");
    }

    /// <summary>"+ Başka ödeme": sayfanın OdemeyiBol'u — tek ödeme İLK satır olur, ikinci satır eklenir.</summary>
    public static void KarmaAc(IRenderedComponent<Islemler> e)
    {
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-baska-odeme").HasAttribute("disabled")), Bekle);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForElement("#islem-bacak-1-arac", Bekle);
    }

    /// <summary>"+ Kimlik bilgisi" çekmecesini açar (MASAK formu, aynı kapı).</summary>
    public static void KimlikAc(IRenderedComponent<Islemler> e)
    {
        if (e.FindAll("#cekmece-yildiz-kimlik").Count > 0) return;
        e.WaitForElement("#yildiz-tezgah-kimlik", Bekle).Click();
        e.WaitForElement("#cekmece-yildiz-kimlik", Bekle);
    }

    /// <summary>Kaydet kilitli mi (Yıldız tezgâhının TEK birincil düğmesi).</summary>
    public static bool KaydetKilitli(IRenderedComponent<Islemler> e) => e.Find(Kaydet).HasAttribute("disabled");

    /// <summary>Kaydet açılana kadar bekler (kilit sebebi hata iletisinde).</summary>
    public static void KaydetAcikBekle(IRenderedComponent<Islemler> e) =>
        e.WaitForAssertion(() => Assert.False(KaydetKilitli(e), e.Find(Kaydet).GetAttribute("title")), Bekle);

    /// <summary>Fişteki kalem satırı sayısı beklenene ulaşana kadar bekler.</summary>
    public static void KalemSayisiBekle(IRenderedComponent<Islemler> e, int beklenen) =>
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll(Kalem).Count), Bekle);

    /// <summary>
    /// 17.10 kurucu: müşterili satışta Kaydet önce "Fatura kesilsin mi?" sorar. Faturasız eski akışı sınayan
    /// testler Kaydet'ten sonra bunu çağırır: soru çıktıysa "Hayır" (gövde eskisiyle birebir), çıkmadıysa hiçbir şey.
    /// Fatura sorusunun kendi davranışı FaturaSorusuEkranTestleri'nde sınanır.
    /// </summary>
    public static void FaturaSorusunaHayir<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent
    {
        var hayir = e.FindAll("#yildiz-belge-hayir");
        if (hayir.Count > 0) hayir[0].Click();
    }
}
