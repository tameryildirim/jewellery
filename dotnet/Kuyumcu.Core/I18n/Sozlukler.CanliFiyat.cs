namespace Kuyumcu.Core.I18n;

// Y-14 (20.10, rakip SarrafPro "paylaşılabilir fiyat ekranı") — oturumsuz canlı fiyat bağlantısı (TR + EN).
public static class CanliFiyatSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Live prices" : "Canlı fiyatlar";
    public static string Gecersiz(string dil) => En(dil)
        ? "This link is no longer valid. Ask the shop for a new link." : "Bu bağlantı artık geçerli değil. İşletmeden yeni bağlantı isteyin.";
    public static string Guncelleme(string zaman, string dil) => En(dil) ? $"Updated {zaman}" : $"Güncelleme {zaman}";
    public static string BaglantiOlustur(string dil) => En(dil) ? "Create live price link (30 days)" : "Canlı fiyat bağlantısı oluştur (30 gün)";
    public static string BaglantiAdi(string dil) => En(dil) ? "Live price link" : "Canlı fiyat bağlantısı";
    public static string BaglantiAciklama(string dil) => En(dil)
        ? "Share the link with customers; it shows the showcase board without login and expires in 30 days. Cancel it below anytime."
        : "Bağlantıyı müşteriye verin; oturumsuz açılır, vitrin panosunu gösterir, 30 gün sonra kendiliğinden kapanır. Aşağıdan istediğiniz an iptal edebilirsiniz.";
    public static string BaglantiKopyala(string dil) => En(dil) ? "Copy link" : "Bağlantıyı kopyala";
    public static string GecerlilikBitis(string zaman, string dil) => En(dil) ? $"valid until {zaman}" : $"{zaman} tarihine kadar geçerli";
}
