namespace Kuyumcu.Core.I18n;

// 17.10 kurucu: stok girişi tek adımlı "Mal geldi" (Satın alma ekranı) — TR + EN.
public static class MalGeldiSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Dugme(string dil) => En(dil) ? "Goods arrived" : "Mal geldi";
    public static string Baslik(string dil) => En(dil) ? "Goods arrived — add to stock" : "Mal geldi — stoğa al";
    public static string Kaydet(string dil) => En(dil) ? "Add to stock" : "Stoğa al";
    public static string Bitti(string irsaliye, string dil) => En(dil)
        ? $"Goods added to stock. Delivery note {irsaliye}; the amount is written to the supplier's account."
        : $"Mal stoğa girdi. İrsaliye {irsaliye}; tutar tedarikçinin hesabına borç yazıldı.";
    public static string MasakDoldurun(string dil) => En(dil)
        ? "This amount needs the identity declaration below; fill it in and press Add to stock again."
        : "Bu tutar için aşağıdaki kimlik beyanı gerekli; doldurup yeniden Stoğa al'a basın.";
}
