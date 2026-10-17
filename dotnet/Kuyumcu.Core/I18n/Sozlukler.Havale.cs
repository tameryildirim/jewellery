namespace Kuyumcu.Core.I18n;

// 17.10 havaleden satış — tezgâh ve karar kutusu metinleri (TR + EN).
public static class HavaleSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Not(string ad, string tutar, string dil) => En(dil)
        ? $"Transfer from {ad}: {tutar} — choose the items sold" : $"{ad} havalesi: {tutar} — satılan ürünleri seçin";
    public static string ToplamEsitOlmali(string havale, string toplam, string dil) => En(dil)
        ? $"Slip total must equal the transfer ({havale}); now {toplam}" : $"Fiş toplamı gelen havaleye ({havale}) eşit olmalı; şu an {toplam}";
    public static string KarmaYok(string dil) => En(dil)
        ? "When selling from a transfer the payment is the transfer itself" : "Havaleden satışta ödeme yalnız gelen havaledir";
    public static string UrunSecSat(string dil) => En(dil) ? "Choose items and sell" : "Ürün seç ve sat";
    public static string Bekleyenler(int adet, string dil) => En(dil) ? $"Incoming transfers ({adet})" : $"Gelen havaleler ({adet})";
    public static string Birak(string dil) => En(dil) ? "Not from a transfer" : "Havaleden değil";
    public static string SatisGirildi(string dil) => En(dil) ? "Sale already entered" : "Satış zaten girildi";
}
