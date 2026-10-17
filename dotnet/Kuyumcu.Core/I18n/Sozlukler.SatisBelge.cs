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
    public static string VarsayilanEtiket(string dil) => En(dil) ? "Suggest an invoice on every sale" : "Satışta fatura önerilsin";
    public static string VarsayilanAciklama(string dil) => En(dil)
        ? "On saving a sale to a named customer the counter asks \"Issue an invoice?\"; when this is on, Yes is the main button. Above the jeweller invoice limit an invoice is always required."
        : "Müşterili satışta Kaydet'e basınca \"Fatura kesilsin mi?\" diye sorulur; açıksa Evet öne çıkar. Kuyumcu fatura sınırının üstünde fatura her zaman zorunludur.";
    public static string VarsayilanFatura(string dil) => En(dil) ? "Sales now suggest an invoice." : "Satışta artık fatura önerilir.";
    public static string VarsayilanFis(string dil) => En(dil) ? "Sales no longer suggest an invoice." : "Satışta artık fatura önerilmez.";
    // 17.10 kurucu: "fiş seçenek olmamalı… müşteriye sorsun fatura kesilsin mi diye; zorunlu durumda hayır derse
    // emin misiniz sınırı aştınız uyarısı; evet derse fatura da kesilir".
    public static string Soru(string dil) => En(dil) ? "Issue an invoice?" : "Fatura kesilsin mi?";
    public static string Evet(string dil) => En(dil) ? "Yes" : "Evet";
    public static string Hayir(string dil) => En(dil) ? "No" : "Hayır";
    public static string SinirUyari(string tutar, string dil) => En(dil)
        ? $"This sale is over the invoice limit ({tutar}); an invoice is required. Are you sure?"
        : $"Satış fatura sınırını ({tutar}) aşıyor; fatura kesilmesi zorunlu. Emin misiniz?";
    public static string EvetFaturaKes(string dil) => En(dil) ? "Yes, issue the invoice" : "Evet, fatura kes";
    public static string GeriDon(string dil) => En(dil) ? "Go back" : "Geri dön";
    public static string FaturaFissiz(string dil) => En(dil) ? "No invoice" : "Fatura kesilmedi";
    public static string FaturaOlusmadi(string sebep, string dil) => En(dil) ? $"Sale saved, invoice not created: {sebep}" : $"Satış kaydedildi, fatura oluşmadı: {sebep}";
}
