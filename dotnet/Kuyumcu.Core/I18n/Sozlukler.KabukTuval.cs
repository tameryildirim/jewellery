using System.Globalization;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K2 — KABUK TUVALE (kenar çubuğu + üst bar) METİNLERİ (TR + EN)
//
// Tek doğruluk kaynağı: dokumanlar/02-mimari/sadelestirme/tuval/project/
//   Panel.dc.html (BASİT) · Profesyonel.dc.html (PRO) · Ayarlar.dc.html
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler HUB'ına dokunulmaz (dalga kuralı); yeni metin
//   Sozlukler.<Alan>.cs'e yazılır. İki dil YAN YANA durur.
// ⚠ Marka adı İKİNCİ KEZ YAZILMAZ: tuvaldeki "Mihenk" yazımı Marka.Ad'dan
//   (tek kaynak, "MİHENK") Türkçe kültürle TÜRETİLİR (P-D2).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kenar çubuğu ve üst bar tuval metinleri — dil kodu "tr" | "en".</summary>
public static class KabukTuvalSozlugu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static bool En(string dil) => dil == "en";

    /// <summary>
    /// Tuvaldeki marka yazımı ("Mihenk"): Marka.Ad'ın Türkçe küçük harfe
    /// indirilip ilk harfinin büyütülmüş hâli (İ → i, I → ı kuralları tr-TR).
    /// </summary>
    public static string MarkaYazimi
    {
        get
        {
            var kucuk = Marka.Ad.ToLower(Tr);
            return kucuk.Length == 0 ? kucuk : char.ToUpper(kucuk[0], Tr) + kucuk[1..];
        }
    }

    /// <summary>Menü kutusunun yer tutucusu (tuval: "Ekran ara").</summary>
    public static string EkranAra(string dil) => En(dil) ? "Find screen" : "Ekran ara";

    /// <summary>PRO kutusundaki kısayol ipucunun açıklaması (kbd'nin title'ı).</summary>
    public static string KisayolIpucu(string dil) => En(dil)
        ? "Ctrl+K (⌘K on Mac): opens the search window for screens, customers and products"
        : "Ctrl+K (Mac'te ⌘K): ekran, müşteri ve ürün arama penceresini açar";

    /// <summary>Kullanıcı kartının menü düğmesi — erişilebilir ad.</summary>
    public static string KartMenusuAc(string dil) => En(dil)
        ? "Account menu: language, branch, help, reset, sign out"
        : "Hesap menüsü: dil, şube, kılavuz, sıfırlama, çıkış";

    /// <summary>Kullanıcı kartı menüsünün başlığı (ekran okuyucu ve görünür).</summary>
    public static string KartMenusu(string dil) => En(dil) ? "Account" : "Hesap";

    /// <summary>Dil seçicinin kart menüsündeki etiketi.</summary>
    public static string Dil(string dil) => En(dil) ? "Language" : "Dil";

    /// <summary>Dişli ikonunun erişilebilir adı (tuval: aria-label="Ayarlar").</summary>
    public static string AyarlarDisli(string dil) => En(dil) ? "Settings" : "Ayarlar";

    /// <summary>Menüdeki Bildirimler satırının sayı rozeti — erişilebilir metin.</summary>
    public static string BildirimRozeti(string dil, int adet) => En(dil)
        ? $"{adet} open notifications"
        : $"{adet} açık bildirim";

    /// <summary>
    /// Saat ipucunun kaynak satırı: dar masaüstünde (1280) PRO şeridindeki
    /// "· Altınkaynak" yazısı yer açmak için gizlenir, bilgi BURADA kalır.
    /// </summary>
    public static string KurKaynagi(string dil, string kaynak) => En(dil) ? $"Source: {kaynak}" : $"Kaynak: {kaynak}";

    /// <summary>Üst bardaki sayfa sekmeleri yuvasının erişilebilir adı.</summary>
    public static string SayfaSekmeleri(string dil) => En(dil) ? "Page sections" : "Sayfa bölümleri";
}
