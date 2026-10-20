namespace Kuyumcu.Core.I18n;

// PAKET-42 (20.10) — sevk irsaliyesi: durum makinesi, taşıyan/plaka/teslim alanları, irsaliyesiz sevk listesi (TR + EN).
// Jargon sıfır: "fiili sevk" yerine "yola çıktı", "teslim" aynen; VUK kelimesi ekrana çıkmaz.
public static class SevkSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string DurumAdi(string? d, string dil) => d switch
    {
        "SEVK_EDILDI" => En(dil) ? "Dispatched" : "Yola çıktı",
        "TESLIM_EDILDI" => En(dil) ? "Delivered" : "Teslim edildi",
        "TASLAK" => En(dil) ? "Not dispatched yet" : "Henüz yola çıkmadı",
        _ => "—",
    };
    public static string SevkDurumu(string dil) => En(dil) ? "Dispatch" : "Sevk";
    public static string Tasiyan(string dil) => En(dil) ? "Carrier (person / company)" : "Taşıyan (kişi / firma)";
    public static string Plaka(string dil) => En(dil) ? "Vehicle plate" : "Araç plakası";
    public static string SevkZamani(string dil) => En(dil) ? "Dispatched at" : "Yola çıkış zamanı";
    public static string TeslimAlan(string dil) => En(dil) ? "Received by" : "Teslim alan";
    public static string TeslimZamani(string dil) => En(dil) ? "Delivered at" : "Teslim zamanı";
    public static string SevkEtBaslik(string dil) => En(dil) ? "Mark as dispatched" : "Yola çıktı olarak işaretle";
    public static string SevkEt(string dil) => En(dil) ? "Dispatched" : "Yola çıktı";
    public static string TeslimEtBaslik(string dil) => En(dil) ? "Mark as delivered" : "Teslim edildi olarak işaretle";
    public static string TeslimEt(string dil) => En(dil) ? "Delivered" : "Teslim edildi";
    public static string TeslimAlanGerekli(string dil) => En(dil) ? "Write who received it" : "Teslim alanın adını yazın";
    public static string SevkSonrasiKilit(string dil) => En(dil)
        ? "Dispatched — the document is printed; header and lines no longer change. To fix: cancel with a reason and open a new waybill."
        : "Yola çıktı — belge basıldı; başlık ve kalemler artık değişmez. Düzeltme: gerekçeyle iptal edip yeni irsaliye açın.";
    public static string IrsaliyesizBaslik(string dil) => En(dil) ? "Sales without a waybill" : "İrsaliyesiz satışlar";
    public static string IrsaliyesizAciklama(int gun, string dil) => En(dil)
        ? $"Sales in the last {gun} days not linked to any waybill line." : $"Son {gun} günde hiçbir irsaliye satırına bağlanmamış satışlar.";
    public static string IrsaliyesizYok(string dil) => En(dil) ? "Every sale has a waybill." : "Her satışın irsaliyesi var.";
    public static string Satis(string dil) => En(dil) ? "Sale" : "Satış";
    public static string Musteri(string dil) => En(dil) ? "Customer" : "Müşteri";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Has(string dil) => En(dil) ? "Fine gold (g)" : "Has (g)";
    public static string SinirNotu(int gosterilen, int toplam, string dil) => En(dil)
        ? $"Showing {gosterilen} of {toplam}." : $"{toplam} kayıttan {gosterilen} tanesi gösteriliyor.";
}
