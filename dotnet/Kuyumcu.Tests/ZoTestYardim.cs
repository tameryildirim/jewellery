using Bunit;
using Bunit.Extensions.WaitForHelpers;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A — eski ekran testleri için "akordeonu / + ekle bölümünü aç"
// yardımcısı. /banka hareketleri kapalı Katlanir'da (bh-gecmis), dekontun
// isteğe bağlı alanları "+ … ekle" arkasında (dekont-detay-ac). Eski testler bu
// alanların KURALLARINI sınar; açılış davranışı ZOISLEMAEkranTestleri'ndedir.
// ═════════════════════════════════════════════════════════════════════════════

public static class ZoTestYardim
{
    /// <summary>
    /// /banka hareket akordeonunu açar. Akordeon hiç çizilmediyse (hesap yok,
    /// liste hatası) sayfa OLDUĞU GİBİ döner — hata/boş durum testleri bozulmaz.
    /// </summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Banka> HareketleriAc(
        this IRenderedComponent<Kuyumcu.Web.Pages.Banka> e)
    {
        try
        {
            var ac = e.WaitForElement("#bh-gecmis-ac", TimeSpan.FromSeconds(2));
            if (ac.GetAttribute("aria-expanded") != "true") ac.Click();
        }
        catch (WaitForFailedException) { /* akordeon yok — sayfa hata/boş durumda */ }
        return e;
    }

    /// <summary>Dekont formunun isteğe bağlı bölümünü açar (zaten açıksa dokunmaz).</summary>
    public static void DekontDetayAc(this IRenderedComponent<Kuyumcu.Web.Pages.Banka> e)
    {
        if (e.FindAll("#dekont-detay-ac").Count > 0) e.Find("#dekont-detay-ac").Click();
    }
}
