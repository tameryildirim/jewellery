namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-A — para ekranları göçünün YENİ metinleri (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Girdi kural metinleri BURADA DEĞİL: GirdiSozlugu (Sozlukler.Girdi.cs) — ekran ve sunucu
// aynı cümleyi oradan basar. Burada yalnız ekran akışına özgü cümleler var.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record GocAMetinleri(
    string VezneKalemUrunsuz,        // "{0}. kalem: ürün seçilmedi …"
    string FaturaTarihIpucu,         // fatura tarihi: ileri gün yok, en çok 1 yıl geri
    string VirmanAciklamaKalan,      // virman açıklaması: kasa/hesap adlarıyla birlikte kalan yer
    string SiralamaSinirNotu,        // G1: sunucu sınırına değen listede sıralama kapalı (UX-S29)
    string PosDurumBilinmiyor,       // POS tahsilat süzgeci: listede olmayan durum (iç kod yerine etiketler)
    string KurManuelSade);           // BASİT: manuel kur çekmecesinin tek sade cümlesi (ham kod/jargon yok)

public static class GocASozlugu
{
    public static readonly GocAMetinleri Tr = new(
        VezneKalemUrunsuz: "{0}. kalem: ürün seçilmedi. Ürün seçin ya da \"Kalemi sil\" ile kaldırın.",
        FaturaTarihIpucu: "Fatura tarihi bugünden ileri olamaz; en çok 1 yıl geri yazılır.",
        VirmanAciklamaKalan: "Kasa ve hesap adlarıyla birlikte en çok {0} karakter yazılabilir.",
        SiralamaSinirNotu: "Son {0} kayıt gösteriliyor. Liste uzun olduğu için sıralama kapalı.",
        PosDurumBilinmiyor: "Bu durum listede yok. Seçilebilenler: {0}.",
        KurManuelSade: "İnternet yokken fiyatı buradan elle yazın.");

    public static readonly GocAMetinleri En = new(
        VezneKalemUrunsuz: "Line {0}: no product selected. Select a product or remove the line with \"Remove line\".",
        FaturaTarihIpucu: "The invoice date cannot be in the future; at most 1 year back.",
        VirmanAciklamaKalan: "Together with the till and account names, at most {0} characters can be written.",
        SiralamaSinirNotu: "Showing the last {0} records. Sorting is off because the list is long.",
        PosDurumBilinmiyor: "This status is not in the list. You can choose: {0}.",
        KurManuelSade: "When there is no internet, type the price here by hand.");

    public static GocAMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
