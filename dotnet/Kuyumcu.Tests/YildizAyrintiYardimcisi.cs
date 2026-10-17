using Bunit;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ (kurucu kararı 17.10): "tema sadece Yıldız, karanlık ve açık mod kalacak".
//
// Eski 8 şemada sayfanın ÖZGÜN gövdesi doğrudan çizilirdi; tek şema Yıldız'da aynı gövde
// (aynı kimlikler, aynı kapılar, aynı istekler) "Tüm ayrıntılar" düğmesinin ARKASINDADIR
// (YildizOzgunCiz => yildizAyrinti). Özgün gövdeyi sınayan testler artık kullanıcının
// YOLUNU izler: önce o düğmeye basar. Güvence aynıdır; yalnız giriş kapısı Yıldız'ınkidir.
//
// ⚠ SESSİZ GEÇİŞ YOK: düğme süre içinde çıkmazsa yardımcı HATA verir (WaitForFailedException) —
//   "açılmadı ama olumsuz denetimler geçti" türünden yanlış yeşil üretilmez.
// ═════════════════════════════════════════════════════════════════════════════
internal static class YildizAyrintiYardimcisi
{
    /// <summary>Yıldız yüzlerinin ortak "Tüm ayrıntılar" düğmesi (YildizListe/Ozet/Bos/Takvim: <c>{Kok}-ayrinti</c>).</summary>
    public const string AyrintiDugmesi = "button.yz-ekle[id$='-ayrinti']";

    /// <summary>Özgün gövdenin kabı (sayfanın YildizOzgunIsaret'i; etiket ekranında data-yz-etiket="acik").</summary>
    public const string OzgunGovde = "[data-yz-ozgun], [data-yz-etiket='acik']";

    /// <summary>
    /// "Tüm ayrıntılar"ı açar ve özgün gövde (<c>[data-yz-ozgun]</c> ya da etiket ekranında
    /// <c>[data-yz-etiket="acik"]</c>) çizilene kadar bekler. Aynı bileşeni döndürür (zincirlenir).
    /// </summary>
    public static IRenderedComponent<T> AyrintiAc<T>(this IRenderedComponent<T> e, string? dugme = null, TimeSpan? sure = null)
        where T : IComponent
    {
        var bekle = sure ?? TimeSpan.FromSeconds(5);
        // Bağlantı sorgusuyla (ör. /gider?sekme=virman) özgün gövde ZATEN açık geldiyse basılacak düğme yoktur.
        if (e.FindAll(OzgunGovde).Count > 0) return e;
        e.WaitForElement(dugme ?? AyrintiDugmesi, bekle).Click();
        e.WaitForElement(OzgunGovde, bekle);
        return e;
    }
}
