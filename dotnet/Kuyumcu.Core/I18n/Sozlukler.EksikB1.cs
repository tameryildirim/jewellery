namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-B1-YAZDIR — cari ekstresi / emanet belgesi yazdırma düğmeleri,
// siparişe kapora ve usta, sipariş/tamir süreç şeridi (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed record EksikB1Metinleri(
    string EkstreYazdir,          // Cariler detay: cari ekstresini yazdır
    string BelgeYazdir,           // Emanet: teslim belgesini yazdır
    string AdimSeridi,            // şeridin erişilebilir adı
    string AdimAlindi,
    string AdimIslemde,
    string AdimTamirde,
    string AdimHazir,
    string AdimTeslim,
    string Usta,
    string UstaYok,
    string UstaSecin,
    string KaporaBaslik,
    string KaporaYok,
    string KaporaAl,
    string KaporaAlBaslik,
    string KaporaTutar,
    string OdemeSekli,
    string Nakit,
    string Kart,
    string Havale,
    string Kasa,
    string KaporaKaydet,
    string KaporaAlindi,          // "{0}" = tutar
    string KaporaSatir,
    string MakbuzYazdir,
    string KaporaGeriVer,
    string KaporaGeriVerAciklama,
    string KaporaGeriVerildi,
    string KaporaStornolu,
    string GeriVerYetki,
    string Gerekce,
    string TutarGerekli,
    string KasaGerekli,
    string KurYok,
    string KasaYok);

public static class EksikB1Sozlugu
{
    public static readonly EksikB1Metinleri Tr = new(
        EkstreYazdir: "Ekstreyi yazdır",
        BelgeYazdir: "Belgeyi yazdır",
        AdimSeridi: "İşin aşaması",
        AdimAlindi: "Alındı",
        AdimIslemde: "İşlemde",
        AdimTamirde: "Tamirde",
        AdimHazir: "Hazır",
        AdimTeslim: "Teslim edildi",
        Usta: "Usta",
        UstaYok: "Seçilmedi",
        UstaSecin: "Usta seçin",
        KaporaBaslik: "Kapora",
        KaporaYok: "Bu siparişe kapora alınmadı.",
        KaporaAl: "+ Kapora al",
        KaporaAlBaslik: "Kapora al",
        KaporaTutar: "Kapora tutarı",
        OdemeSekli: "Ödeme şekli",
        Nakit: "Nakit",
        Kart: "Kart",
        Havale: "Havale",
        Kasa: "Kasa",
        KaporaKaydet: "Kaporayı al",
        KaporaAlindi: "Kapora alındı: {0}",
        KaporaSatir: "Alınan kapora",
        MakbuzYazdir: "Makbuzu yazdır",
        KaporaGeriVer: "Kaporayı geri ver",
        KaporaGeriVerAciklama: "Kapora kasadan müşteriye geri ödenir. Kayıt silinmez.",
        KaporaGeriVerildi: "Kapora geri verildi.",
        KaporaStornolu: "Kapora geri verildi.",
        GeriVerYetki: "Kaporayı yalnız işletme sahibi ya da müdür geri verebilir.",
        Gerekce: "Neden geri veriliyor?",
        TutarGerekli: "Kapora tutarını yazın.",
        KasaGerekli: "Kasa seçin.",
        KurYok: "Güncel fiyat gelmeden kapora alınamaz.",
        KasaYok: "Kapora için açık bir TL kasası yok. Önce Kasalar ekranından kasa açın.");

    public static readonly EksikB1Metinleri En = new(
        EkstreYazdir: "Print statement",
        BelgeYazdir: "Print document",
        AdimSeridi: "Job progress",
        AdimAlindi: "Received",
        AdimIslemde: "In progress",
        AdimTamirde: "Being repaired",
        AdimHazir: "Ready",
        AdimTeslim: "Delivered",
        Usta: "Craftsman",
        UstaYok: "Not chosen",
        UstaSecin: "Choose a craftsman",
        KaporaBaslik: "Deposit",
        KaporaYok: "No deposit was taken for this order.",
        KaporaAl: "+ Take deposit",
        KaporaAlBaslik: "Take deposit",
        KaporaTutar: "Deposit amount",
        OdemeSekli: "Payment method",
        Nakit: "Cash",
        Kart: "Card",
        Havale: "Bank transfer",
        Kasa: "Till",
        KaporaKaydet: "Take deposit",
        KaporaAlindi: "Deposit taken: {0}",
        KaporaSatir: "Deposit taken",
        MakbuzYazdir: "Print receipt",
        KaporaGeriVer: "Refund deposit",
        KaporaGeriVerAciklama: "The deposit is paid back to the customer from the till. The record is not deleted.",
        KaporaGeriVerildi: "Deposit refunded.",
        KaporaStornolu: "Deposit refunded.",
        GeriVerYetki: "Only the owner or a manager can refund a deposit.",
        Gerekce: "Why is it being refunded?",
        TutarGerekli: "Enter the deposit amount.",
        KasaGerekli: "Choose a till.",
        KurYok: "A deposit cannot be taken before the current price arrives.",
        KasaYok: "There is no open TRY till for the deposit. Open one on the Tills screen first.");

    public static EksikB1Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
