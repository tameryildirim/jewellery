using Bunit;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ (17.10) · GRUP GG — Müşteriler (Cariler) ekranı.
//
// Eski özgün liste (arama/süzgeç/segment/ızgara tablosu, satır eylem düğmeleri) KALDIRILDI;
// liste artık Yıldız tuvali (YildizMusteriler: satır #yildiz-musteri-{id}). Satır eylemleri
// (tahsilat, veresiye, detay, düzeltme, döviz…) aynı kimliklerle (CariEylemleri — TEK kaynak)
// müşteri KARTININ başında durur (YildizKartEylemleri, #yildiz-kart-eylemler). "Düzenle" kartın
// kendisidir (satıra dokunmak); "Pasife al / Kullanımdan kaldır" kartın altındaki #c-pasife-al.
//
// Bu yardımcı kullanıcının Yıldız'daki GERÇEK yolunu izler: satıra dokun → kart → eylem.
// ⚠ SESSİZ GEÇİŞ YOK: satır ya da eylem süre içinde çıkmazsa HATA (WaitForFailedException).
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGGYardimcisi
{
    private static readonly TimeSpan Sure = TimeSpan.FromSeconds(5);

    /// <summary>Yıldız listesindeki müşteri satırı seçicisi.</summary>
    public static string Satir(string cariId) => $"#yildiz-musteri-{cariId}";

    /// <summary>
    /// Satıra dokunur → müşteri kartı (düzenleme çekmecesi + kart eylemleri) açılır ve kartın tamamı
    /// sunucudan gelene dek (#duz-ad kilidi kalkana dek) beklenir. Kart yüklenmesi arka planda sürerken
    /// sonraki tıklamalar eski çizime düşmesin (bUnit "event handler id" yarışı — ağır yükte görüldü).
    /// </summary>
    public static IRenderedComponent<T> KartAc<T>(this IRenderedComponent<T> e, string cariId) where T : IComponent
    {
        e.WaitForElement(Satir(cariId), Sure).Click();
        e.WaitForElement("#yildiz-kart-eylemler", Sure);
        e.WaitForState(() => e.FindAll("#duz-ad").Count == 1 && !e.Find("#duz-ad").HasAttribute("disabled"), Sure);
        return e;
    }

    /// <summary>
    /// Kartı açar ve kartın eylem satırındaki <c>#c-{eylem}-{cariId}</c> düğmesine basar
    /// (eylem: tahsilat, veresiye, detay, duzeltme, doviz-tahsilat, doviz-veresiye, aktife-al).
    /// </summary>
    public static IRenderedComponent<T> KartEylemi<T>(this IRenderedComponent<T> e, string cariId, string eylem) where T : IComponent
    {
        e.KartAc(cariId);
        e.WaitForElement($"#yildiz-kart-eylemler #c-{eylem}-{cariId}", Sure).Click();
        return e;
    }

    /// <summary>Kartın eylem satırındaki düğmelerin kimlikleri (kart açık olmalı).</summary>
    public static List<string> KartEylemKimlikleri<T>(this IRenderedComponent<T> e) where T : IComponent =>
        [.. e.FindAll("#yildiz-kart-eylemler > button").Select(b => b.Id!)];
}
