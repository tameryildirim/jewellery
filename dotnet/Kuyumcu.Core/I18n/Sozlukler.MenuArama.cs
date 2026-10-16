namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-j — MENÜ İÇİ ARAMA + Ctrl+K KAYIT ARAMASI METİNLERİ (TR + EN)
//
// Kurucu (24.09 14:40, /urunler/agac, "Menüde ara" kutusuna "sat"): "arama
// sonuçları farklı yerde beyaz zeminde geliyor, menüde olması lazım".
// Menü kutusu artık YALNIZ menüyü süzer; kayıt (müşteri/ürün/fiş) araması
// üst bardaki "Kayıt ara" düğmesinin (ve Ctrl+K kısayolunun) açtığı palettedir.
//
// ⚠ NEDEN AYRI DOSYA: SozlukKabuk alanları `required`; tek alan eklemek
//   Sozlukler.cs'in iki dev gövdesini birden düzenlemeyi gerektirirdi
//   (MenuSozlugu / HavaSozlugu ile aynı desen). İki dil YAN YANA durur.
// ⚠ Jargon sıfır (plan § 5B): "kayıt" yerine kullanıcının arayacağı şeyler
//   (müşteri, ürün, fiş) sayılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Menü araması ve kayıt paleti metinleri — dil kodu "tr" | "en".</summary>
public static class MenuAramaSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Menü kutusunun yer tutucusu (kayıt ifadesi YOK — kutu yalnız menüyü süzer).</summary>
    public static string YerTutucu(string dil) => En(dil) ? "Search the menu" : "Menüde ara";

    /// <summary>Menü kutusunun erişilebilir adı ve görünmez etiketi.</summary>
    public static string Etiket(string dil) => En(dil)
        ? "Search the menu — only the matching screens stay in the menu"
        : "Menüde ara — yalnız eşleşen ekranlar menüde kalır";

    /// <summary>Temizle (×) düğmesi.</summary>
    public static string Temizle(string dil) => En(dil) ? "Clear the search, show the whole menu" : "Aramayı temizle, menünün tamamını göster";

    /// <summary>Menüde hiçbir satır eşleşmediğinde menünün içindeki TEK satır.</summary>
    public static string EslesenYok(string dil) => En(dil) ? "No matching screen" : "Eşleşen ekran yok";

    /// <summary>Ekran okuyucu için eşleşme sayısı (görünmez durum satırı).</summary>
    public static string EslesenSayisi(string dil, int adet) => En(dil)
        ? $"{adet} matching screens. Use the arrow keys to move, Enter to open."
        : $"{adet} eşleşen ekran. Oklarla gezin, Enter ile aç.";

    /// <summary>Üst bardaki kayıt araması düğmesi (görünür yazı).</summary>
    public static string KayitAra(string dil) => En(dil) ? "Find customer or product" : "Müşteri, ürün ara";

    /// <summary>Kayıt araması düğmesinin ipucu — ne aradığı ve kısayolu.</summary>
    public static string KayitAraIpucu(string dil) => En(dil)
        ? "Find a customer, product or receipt (Ctrl+K)"
        : "Müşteri, ürün ya da fiş bul (Ctrl+K)";

    /// <summary>Kayıt paletinin başlığı (diyalog adı).</summary>
    public static string PaletBaslik(string dil) => En(dil) ? "Find customer, product, receipt" : "Müşteri, ürün, fiş bul";

    /// <summary>
    /// KATMAN-1b (tasarım hakemi 29.09): palette ne ekran ne kayıt eşleşince TEK, kısa "yok".
    /// </summary>
    public static string PaletSonucYok(string dil) => En(dil) ? "Nothing found" : "Sonuç yok";

    /// <summary>
    /// KATMAN-1b (tasarım hakemi 29.09): yalnız sunucu "daha var" dediğinde çıkan sade satır.
    /// Palette süzgeç yoktur — "süzgeç" sözcüğü geçmez; yol: adı biraz daha yazmak.
    /// </summary>
    public static string PaletDahaVar(string dil) => En(dil)
        ? "More results, type a little more of the name"
        : "Daha fazla sonuç var, adı biraz daha yazın";
}
