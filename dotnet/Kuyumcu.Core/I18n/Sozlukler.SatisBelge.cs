namespace Kuyumcu.Core.I18n;

// M6 (16.10) — Tezgâh satış belgesi (Fiş / Fatura) metinleri (TR + EN).
// Müşteri: "fişe hiç gerek yok… TC'yi yazın, sisteme girdiğim anda fatura kessin"; "30 bin lira geçtiğinde isim konulmalı".
public static class SatisBelgeSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Grup(string dil) => En(dil) ? "Document" : "Belge";
    public static string Fis(string dil) => En(dil) ? "Receipt" : "Fiş";
    public static string Fatura(string dil) => En(dil) ? "Invoice" : "Fatura";
    public static string MusteriSecin(string dil) => En(dil) ? "Choose a customer for the invoice" : "Fatura için müşteri seçin";
    public static string Zorunlu(string tutar, string dil) => En(dil) ? $"Over {tutar} an invoice is required" : $"{tutar} üstünde fatura zorunlu";
    public static string FaturaKesildi(string no, string dil) => En(dil) ? $"Invoice {no}" : $"Fatura {no}";
    public static string FaturaTaslak(string dil) => En(dil) ? "Invoice draft created — accounting will issue it" : "Fatura taslağı oluştu — muhasebe keser";
    public static string FaturaOlusmadi(string sebep, string dil) => En(dil) ? $"Sale saved, invoice not created: {sebep}" : $"Satış kaydedildi, fatura oluşmadı: {sebep}";
}
