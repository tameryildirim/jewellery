using Bunit;
using Kuyumcu.Web.Pages;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ 17.10 — GI şeridi (kabuk ve menü) ortak yardımcısı.
//
// Kenar çubuğu (Layout/KenarCubugu.razor) kaldırıldı; kullanıcının TÜM ekran
// listesine ulaştığı tek yer artık "Tüm ekranlar" sayfasıdır (Pages/Ekranlar →
// Bilesenler/Yildiz/YildizEkranlar: a.yz-kare kareleri, bölüm başına
// section#ekranlar-grup-{slug}). Eski kenar çubuğu testlerinin YETKİ / MOD /
// GRUP güvenceleri bu sayfa üzerinden sınanır.
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGIYardimcisi
{
    /// <summary>"Tüm ekranlar" kareleri (a.yz-kare) çizilene kadar bekler.</summary>
    public static IRenderedComponent<Ekranlar> KareleriBekle(this IRenderedComponent<Ekranlar> e)
    {
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("a.yz-kare")));
        return e;
    }

    /// <summary>Çizilen karelerin hedefleri, ekrandaki sırayla.</summary>
    public static List<string> KareYollari(this IRenderedComponent<Ekranlar> e) =>
        [.. e.FindAll("a.yz-kare").Select(a => a.GetAttribute("href")!)];

    /// <summary>Çizilen bölümlerin (grup) kimlikleri, ekrandaki sırayla.</summary>
    public static List<string> BolumKimlikleri(this IRenderedComponent<Ekranlar> e) =>
        [.. e.FindAll("section.yz-tum-bolum").Select(s => s.Id ?? "")];
}
