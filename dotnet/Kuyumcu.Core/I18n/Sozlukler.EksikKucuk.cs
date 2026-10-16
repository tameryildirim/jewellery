namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-KUCUK — B2-10 (vitrin tam ekran + işçilik ibaresi) ve B2-12
// (müşteri değişince "fiyat listesi değişti — güncelle") metinleri. TR + EN.
//
// İbare dayanağı: Fiyat Etiketi Yönetmeliği (RG 28.06.2014/29044) md.6 —
// "Bu satış fiyatlarına işçilik bedelinin dahil olup olmadığı ayrıca belirtilir."
// Yönetmelik ibarenin KELİMESİNİ dayatmaz; dahil/hariç olduğunun yazılmasını ister.
// ═════════════════════════════════════════════════════════════════════════════

public sealed record EksikKucukMetinleri(
    // ── B2-10 vitrin ──
    string IbareDahil,
    string IbareHaric,
    string IbareBaslik,
    string IbareSecilmedi,
    string IbareSecilmediTezgahtar,
    string IbareDahilSecenek,
    string IbareHaricSecenek,
    string IbareKaydedildi,
    string IbareYasalNot,
    string TamEkranAc,
    string TamEkranKilitli,
    string TamEkranBaslik,
    string TamEkranRotaBaslik,
    string TamEkranCik,
    string TamEkranTarayici,
    string TamEkranGuncelleme,
    // ── B2-12 satış ──
    string ListeDegisti,
    string ListeGuncelle,
    string ListeEskiKalsin,
    string ListeGuncellendi,
    string ListeHata);

public static class EksikKucukSozlugu
{
    public static readonly EksikKucukMetinleri Tr = new(
        IbareDahil: "Satış fiyatlarına işçilik bedeli dahildir.",
        IbareHaric: "Satış fiyatlarına işçilik bedeli dahil değildir.",
        IbareBaslik: "İşçilik ibaresi",
        IbareSecilmedi: "İşçilik ibaresi seçilmedi. Yasa gereği panoda fiyatlara işçiliğin dahil olup olmadığı yazmalı; seçmeden işletme ekranı açılmaz.",
        IbareSecilmediTezgahtar: "İşçilik ibaresi seçilmedi. İşletme ekranını açmak için müdürünüz bu seçimi yapmalı.",
        IbareDahilSecenek: "İşçilik dahil",
        IbareHaricSecenek: "İşçilik hariç",
        IbareKaydedildi: "İşçilik ibaresi kaydedildi: {0}",
        IbareYasalNot: "Fiyat Etiketi Yönetmeliği md.6: altın ve gümüş fiyatlarında işçiliğin dahil olup olmadığı ayrıca yazılır.",
        TamEkranAc: "İşletme ekranını aç",
        TamEkranKilitli: "Önce işçilik ibaresini seçin",
        TamEkranBaslik: "Güncel fiyatlar",
        TamEkranRotaBaslik: "İşletme ekranı",
        TamEkranCik: "Panoya dön",
        TamEkranTarayici: "Tam ekran",
        TamEkranGuncelleme: "Son güncelleme: {0}",
        ListeDegisti: "Müşteri değişti — bu müşterinin fiyat listesine göre {0} kalemin fiyatı değişiyor.",
        ListeGuncelle: "Fiyatları güncelle",
        ListeEskiKalsin: "Eski fiyatlar kalsın",
        ListeGuncellendi: "{0} kalemin fiyatı yeni listeye göre güncellendi.",
        ListeHata: "Yeni müşterinin fiyatları alınamadı; sepetteki fiyatlar değişmedi.");

    public static readonly EksikKucukMetinleri En = new(
        IbareDahil: "Sale prices include the workmanship charge.",
        IbareHaric: "Sale prices do not include the workmanship charge.",
        IbareBaslik: "Workmanship notice",
        IbareSecilmedi: "No workmanship notice selected. By law the board must state whether prices include workmanship; the shop screen stays closed until you choose.",
        IbareSecilmediTezgahtar: "No workmanship notice selected. Your manager must choose it before the shop screen can be opened.",
        IbareDahilSecenek: "Workmanship included",
        IbareHaricSecenek: "Workmanship excluded",
        IbareKaydedildi: "Workmanship notice saved: {0}",
        IbareYasalNot: "Price Tag Regulation art. 6: gold and silver prices must state separately whether workmanship is included.",
        TamEkranAc: "Open shop screen",
        TamEkranKilitli: "Choose the workmanship notice first",
        TamEkranBaslik: "Current prices",
        TamEkranRotaBaslik: "Shop screen",
        TamEkranCik: "Back to board",
        TamEkranTarayici: "Full screen",
        TamEkranGuncelleme: "Last update: {0}",
        ListeDegisti: "Customer changed — {0} item price(s) differ on this customer's price list.",
        ListeGuncelle: "Update prices",
        ListeEskiKalsin: "Keep old prices",
        ListeGuncellendi: "{0} item price(s) updated from the new list.",
        ListeHata: "Could not get the new customer's prices; basket prices were not changed.");

    public static EksikKucukMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
