using Bunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ 17.10 — GH şeridi ortak yardımcıları (yalnız Yıldız yüzü kaldı).
//
// Sayım ekranında özgün gövde (oturum LİSTESİ #sy-tablo ve oturum ayrıntısı #sy-satirlar) "Tüm ayrıntılar"
// arkasındadır; ama Yıldız yüzü "Tüm ayrıntılar"ı YALNIZ bir sayım açıkken verir (Sayim.Yildiz.cs AyrintiAc).
// Kullanıcının gerçek yolu: açık sayım varsa Yıldız onu kendiliğinden açar (yoksa Yıldız listesinde bir sayıma
// dokunulur) → "Tüm ayrıntılar" → özgün ekranda "← Sayım" (#sy-listeye-don) = özgün oturum listesi.
//
// ⚠ SESSİZ GEÇİŞ YOK: her adım öğeyi BEKLER; çıkmazsa WaitFor… HATA verir (boş DOM'da yanlış yeşil yok).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGHYardimcisi
{
    private static readonly TimeSpan Varsayilan = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Sayım: özgün OTURUM AYRINTISI (#sy-satirlar). Açık sayım Yıldız'da kendiliğinden açılır; açık sayım yoksa
    /// Yıldız listesindeki ilk sayıma dokunulur. Ardından "Tüm ayrıntılar".
    /// </summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Sayim> SayimOzgunAyrinti(
        this IRenderedComponent<Kuyumcu.Web.Pages.Sayim> e, TimeSpan? sure = null)
    {
        var bekle = sure ?? Varsayilan;
        e.WaitForAssertion(() => Assert.True(
            e.FindAll("#yildiz-sayim-ayrinti").Count > 0 || e.FindAll("button[id^='yildiz-sayim-oturum-']").Count > 0,
            "Yıldız sayım yüzü ne açık sayımı ne de sayım listesini çizdi"), bekle);
        if (e.FindAll("#yildiz-sayim-ayrinti").Count == 0)
        {
            // Listede AÇIK sayım varsa Yıldız onu kendiliğinden açar (OnAfterRender) — elle dokunmak o yolla yarışır.
            var acik = Kuyumcu.Core.I18n.Sozlukler.Tr.Sayim.Acik;
            var acikVar = e.FindAll("#yildiz-sayim-liste .yz-liste-sat").Any(r =>
                r.QuerySelectorAll("span").Any(s => s.TextContent.Trim() == acik));
            if (!acikVar) e.Find("button[id^='yildiz-sayim-oturum-']").Click();
        }
        e.AyrintiAc(sure: bekle);
        e.WaitForElement("#sy-satirlar", bekle);
        return e;
    }

    /// <summary>
    /// Emanet: Yıldız listesinde "Tüm ayrıntılar" YOKTUR (Emanet.Yildiz.cs — AyrintiAc bağlanmaz); özgün ekran bir
    /// emanet satırına (müşteri adına) dokununca o müşteri SEÇİLİ açılır (YildizMusteriAc). Kullanıcının gerçek yolu budur.
    /// </summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Emanet> EmanetOzgunAc(
        this IRenderedComponent<Kuyumcu.Web.Pages.Emanet> e, string? hareketId = null, TimeSpan? sure = null)
    {
        var bekle = sure ?? Varsayilan;
        var secici = hareketId is null ? "button[id^='yildiz-emanet-musteri-']" : $"#yildiz-emanet-musteri-{hareketId}";
        e.WaitForElement(secici, bekle).Click();
        e.WaitForElement(YildizAyrintiYardimcisi.OzgunGovde, bekle);
        return e;
    }

    /// <summary>Sayım: özgün OTURUM LİSTESİ (#sy-tablo) — ayrıntıdan "← Sayım" (#sy-listeye-don).</summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Sayim> SayimOzgunListe(
        this IRenderedComponent<Kuyumcu.Web.Pages.Sayim> e, TimeSpan? sure = null)
    {
        var bekle = sure ?? Varsayilan;
        e.SayimOzgunAyrinti(bekle);
        e.WaitForElement("#sy-listeye-don", bekle).Click();
        e.WaitForElement("#sy-tablo, #sy-yeni", bekle);
        return e;
    }
}
