namespace Kuyumcu.Core.I18n;

// M9 (müşteri 16.10: "3.600 kâr etmiş görürsem üstüne vergi alır") — altın kârının iki parçası (TR + EN).
public static class KarAyrimiSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Makas(string dil) => En(dil) ? "Making-charge profit" : "İşçilik (makas) kârı";
    public static string MakasAlt(string dil) => En(dil) ? "has profit × sale-day gold price" : "has kârı × satış günü altın fiyatı";
    public static string AltinFiyat(string dil) => En(dil) ? "Gold price gain" : "Altın fiyat farkı";
    public static string AltinFiyatAlt(string dil) => En(dil) ? "stock gold gained value from purchase to sale" : "stoktaki altının alıştan satışa değer farkı";
    public static string MakasKisa(string dil) => En(dil) ? "Making charge" : "İşçilik kârı";
    public static string AltinFiyatKisa(string dil) => En(dil) ? "Gold price" : "Altın farkı";

    // ── Alış işçilik belgesi (M9) ──
    public static string IscilikBelgeleri(string dil) => En(dil) ? "Purchase making-charge documents" : "Alış işçilik belgeleri";
    public static string BelgesizUyari(int adet, string tutar, string dil) => En(dil)
        ? $"{adet} purchase(s) have no making-charge document ({tutar}). Without an invoice this difference may be treated as profit in a tax audit."
        : $"{adet} alışın işçilik belgesi yok ({tutar}). Faturası olmayan işçilik incelemede kâr sayılabilir.";
    public static string Ac(string dil) => En(dil) ? "Enter documents" : "Belgeleri gir";
    public static string Bos(string dil) => En(dil) ? "No purchase with a making charge in the last 90 days." : "Son 90 günde işçiliği olan alış yok.";
    public static string Odenen(string dil) => En(dil) ? "Paid" : "Ödenen";
    public static string HasBedeli(string dil) => En(dil) ? "Gold value" : "Has bedeli";
    public static string Iscilik(string dil) => En(dil) ? "Making charge" : "İşçilik";
    public static string BelgeYok(string dil) => En(dil) ? "No document" : "Belge yok";
    public static string BelgeGir(string dil) => En(dil) ? "Enter document" : "Belge gir";
    public static string Duzelt(string dil) => En(dil) ? "Edit" : "Düzelt";
    public static string BelgeTuru(string dil) => En(dil) ? "Document type" : "Belge türü";
    public static string Fatura(string dil) => En(dil) ? "Invoice" : "Fatura";
    public static string GiderPusulasi(string dil) => En(dil) ? "Expense voucher" : "Gider pusulası";
    public static string Belgesiz(string dil) => En(dil) ? "No document" : "Belgesiz";
    public static string BelgesizNot(string dil) => En(dil)
        ? "Recorded as undocumented; the warning stays. Ask the wholesaler for the making-charge invoice."
        : "Belgesiz olarak kaydedilir, uyarı sürer. Toptancıdan işçilik faturasını isteyin.";
    public static string BelgeNo(string dil) => En(dil) ? "Document no." : "Belge no";
    public static string BelgeTarihi(string dil) => En(dil) ? "Document date" : "Belge tarihi";
    public static string IscilikTutari(string dil) => En(dil) ? "Making charge on the document" : "Belgedeki işçilik";
    public static string Kaydedildi(string dil) => En(dil) ? "Saved." : "Kaydedildi.";
}
