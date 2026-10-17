using Bunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ (17.10) — GE şeridi: Emanet ekranının Yıldız yolu.
//
// Emanet'in Yıldız yüzünde "Tüm ayrıntılar" düğmesi YOKTUR (YildizBaglami AyrintiAc vermez):
// özgün gövde (data-yz-ozgun="emanet": müşteri seçici, özet, "Emanet al / Emanet iade et",
// "Eski kayıtlar", çekmeceler) kullanıcının listede bir emanet satırının MÜŞTERİ ADINA basmasıyla
// açılır (YildizMusteriAc: o müşteri seçili gelir). Liste, GET /cari müşterilerinin
// GET /emanet/{id} hareketlerinden kurulur → satır için en az bir geri alınmamış EMANET_GIRIS gerekir.
//
// Özgün gövdede müşteri seçici (#e-cari) TÜM müşterileri listeler; emaneti olmayan ya da sınır
// değerli bir müşteriyi sınamak için kullanıcı yolu: satırdan özgün gövdeyi aç → seçiciden müşteriyi seç.
// Fikstürü yalnız tek (emanet geçmişi olmayan) müşteri içeren testler bu yüzden listeye "kapı"
// müşterisini (c0) ekler — kapı müşterisi yalnız özgün gövdeye geçişi sağlar, sınanan değerlere karışmaz.
//
// ⚠ SESSİZ GEÇİŞ YOK: satır süre içinde çıkmazsa WaitForFailedException (yanlış yeşil üretilmez).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGEYardimcisi
{
    /// <summary>Kapı müşterisinin GET /cari satırı (JSON nesnesi).</summary>
    public const string KapiCari = """{"id":"c0","ad":"Kapı Müşteri"}""";

    /// <summary>Kapı müşterisinin GET /emanet/c0 yanıtı: tek geri alınmamış giriş (hareket hk0).</summary>
    public const string KapiEmanet = """
        {"cariId":"c0","mevcutHasGramBakiye":1,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1}],
         "hareketler":[{"id":"hk0","tur":"EMANET_GIRIS","tarih":"2026-09-01T10:00:00Z","hasGrami":1,
                        "aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
        """;

    /// <summary>Kapı müşterisinin liste satırındaki hareket kimliği.</summary>
    public const string KapiHareket = "hk0";

    /// <summary>
    /// Emanet listesinde <paramref name="hareketId"/> satırının müşteri adına basar ve özgün gövde
    /// (<c>[data-yz-ozgun]</c>) ile müşteri seçici çizilene kadar bekler. Gövde zaten açıksa dokunmaz.
    /// </summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Emanet> EmanetSatiriAc(
        this IRenderedComponent<Kuyumcu.Web.Pages.Emanet> e, string hareketId = KapiHareket, TimeSpan? sure = null)
    {
        var bekle = sure ?? TimeSpan.FromSeconds(5);
        if (e.FindAll("[data-yz-ozgun]").Count > 0) return e;
        e.WaitForElement($"#yildiz-emanet-musteri-{hareketId}", bekle).Click();
        e.WaitForElement("[data-yz-ozgun] #e-cari", bekle);
        return e;
    }
}
