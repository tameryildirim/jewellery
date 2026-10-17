using System.Reflection;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ 17.10 — GF şeridi (Panel/Bugün) ortak yardımcısı.
//
// Panel artık YALNIZ Yıldız Bugün'ü (Bilesenler/Yildiz/YildizBugun.razor) çizer; eski karşılama
// + kutucuk ızgarası + BASİT "sabahın beş sorusu" gövdesi kaldırıldı. Bugün'ün verisi Panel'in
// AYNI yükleyicilerinden gelir (özet, nakit ayrımı, yaşlandırma, vade, bildirim, uyum, kâr,
// Panel.Yildiz.cs: dünkü gün sonu + uzun duran emanet).
//
// ⚠ "Yüklendi" kanıtı: Bugün'de her kartın kendi iskeleti YOK (liste doluyken uyum/stopaj
//   yükleyicisinin bitişi DOM'da görünmez). Olumsuz iddialar (Assert.Empty / DoesNotContain)
//   yanlış yeşil vermesin diye Panel'in kendi yükleme görevleri (Panel.Yuklemeler — testler için
//   açılmış iç özellik) TAMAMLANANA kadar beklenir; ardından Bugün'ün yükleniyor işaretlerinin
//   kalktığı doğrulanır. Süre dolarsa HATA (sessiz geçiş yok).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGFYardimcisi
{
    /// <summary>Yıldız Bugün'ün istediği servisler (YildizBugun → YetkiServisi; kayıtlıysa dokunulmaz).</summary>
    public static void BugunServisleri(this IServiceCollection s)
    {
        s.TryAddScoped<YetkiServisi>();
    }

    private static readonly PropertyInfo YuklemelerOzelligi =
        typeof(Kuyumcu.Web.Pages.Panel).GetProperty("Yuklemeler", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException("Panel.Yuklemeler bulunamadı — yardımcı güncellenmeli.");

    /// <summary>
    /// Bugün çizildi (#yildiz-bugun) + Panel'in TÜM yükleme görevleri bitti + "Seni bekleyen" ve dev rakam
    /// yükleniyor işaretinde değil. <paramref name="pozisyon"/> true ise alt şeridin pozisyon öğesi (ya da
    /// bandı) da beklenir (YildizBugun'un kendi GET /pozisyon isteği).
    /// </summary>
    public static IRenderedComponent<Kuyumcu.Web.Pages.Panel> BugunBekle(
        this IRenderedComponent<Kuyumcu.Web.Pages.Panel> e, bool pozisyon = false, TimeSpan? sure = null)
    {
        var bekle = sure ?? TimeSpan.FromSeconds(10);
        e.YukleyicilerBitsin(bekle);
        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("#yildiz-bugun-bekleyen-yukleniyor"));
            Assert.Empty(e.FindAll("#yildiz-bugun [aria-busy='true']"));
            if (pozisyon)
                Assert.True(e.FindAll("#yildiz-bugun-pozisyon").Count + e.FindAll("#yildiz-bugun-pozisyon-hata").Count > 0,
                    "alt şeridin pozisyon öğesi (ya da bandı) çizilmedi");
        }, bekle);
        return e;
    }

    /// <summary>
    /// Bugün çizildi (#yildiz-bugun) ve Panel'in o ana dek açtığı TÜM yükleme görevleri bitti (DOM'a bakmaz:
    /// "profil bekleniyor → liste yükleniyor kalır" gibi durumlar da sınanabilsin). Süre dolarsa HATA.
    /// </summary>
    public static void YukleyicilerBitsin(this IRenderedComponent<Kuyumcu.Web.Pages.Panel> e, TimeSpan? sure = null)
    {
        var bekle = sure ?? TimeSpan.FromSeconds(10);
        e.WaitForElement("#yildiz-bugun", bekle);
        var son = DateTime.UtcNow + bekle;
        while (true)
        {
            Task[] gorevler;
            try { gorevler = [.. (List<Task>)YuklemelerOzelligi.GetValue(e.Instance)!]; }
            catch (InvalidOperationException) { continue; }   // liste o an büyüyordu — yeniden oku
            var kalan = son - DateTime.UtcNow;
            if (kalan <= TimeSpan.Zero || !Task.WaitAll(gorevler, kalan))
                throw new TimeoutException("Panel yükleyicileri süre içinde bitmedi.");
            int simdi;
            try { simdi = ((List<Task>)YuklemelerOzelligi.GetValue(e.Instance)!).Count; }
            catch (InvalidOperationException) { continue; }
            if (simdi == gorevler.Length) return;   // bu arada yeni yükleyici eklenmediyse bitti
        }
    }

    /// <summary>
    /// Müşteriler (Cariler) — Yıldız listesinde satır eylemi YOK; eylemler müşterinin KARTINDADIR (YildizKartEylemleri:
    /// özgün CariEylemleri, aynı kimlikler <c>c-tahsilat-{id}</c>, <c>c-detay-{id}</c> …). Kullanıcı yolu: listedeki
    /// müşteri satırı (<c>#yildiz-musteri-{id}</c>) → kart → eylem. Düğme ya da satır süre içinde çıkmazsa HATA.
    /// </summary>
    public static void CariKartEylemi(this IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string cariId, string eylemOnek, TimeSpan? sure = null)
    {
        var bekle = sure ?? TimeSpan.FromSeconds(5);
        var dugme = $"#{eylemOnek}-{cariId}";
        if (e.FindAll(dugme).Count == 0)
        {
            e.WaitForElement($"#yildiz-musteri-{cariId}", bekle).Click();
            e.WaitForElement(dugme, bekle);
        }
        e.Find(dugme).Click();
    }

    /// <summary>"Seni bekleyen" listesindeki satır kimlikleri (yildiz-bekleyen-*) sırasıyla.</summary>
    public static List<string> BekleyenKimlikleri(this IRenderedComponent<Kuyumcu.Web.Pages.Panel> e) =>
        [.. e.FindAll("#yildiz-bugun-liste > li.yildiz-bugun-sat").Select(li => li.Id)];
}
