namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q9-3 — SİPARİŞ BAŞLIĞI (cari · tür · şube) DÜZENLEMESİNİN GÖRÜNÜR SONUCU
// (QA turu 9 · KRİTİK, önceki turdan taşındı): TR + EN METİNLER
//
// Asıl bulgu (TASLAK'ta cari/tür/şube düzenlenemiyor) P-51 ile kapanmıştı; bu
// pakette canlı ölçümle yeniden üretilemedi. Aynı ölçüm, aynı düzenlemenin
// ÇEVRESİNDE şu kusurları gösterdi ve metinleri burada:
//   • kartta şube YOKTU — şube değiştirilince kullanıcı sonucu göremiyordu (UX-S27);
//   • müşteri listesinin sınır notu "{0}" yer tutucusunu çıplak basıyor ve
//     "sipariş" diyordu; ürün sınır notu da "sipariş" diyordu (UX-S29);
//   • geçiş geçmişi BASLIK_DUZENLENDI önekini, ham tür kodlarını ve kayıt
//     kimliklerini (ULID) ekrana basıyordu (UX-S31 · B-25);
//   • müşteri seçeneğinde ham "MUSTERI" kodu görünüyordu (UX-S31);
//   • teslim edilmiş / iptal edilmiş siparişte değişmezlik notu "iptal edin"
//     diyordu — o durumda iptal yolu YOKTUR (UX-S25: yol gerçekten var olmalı).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI: konumsal record, Tr ve En aynı kurucudan.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Sipariş detayına Q9-3 ile eklenen metinler (bir dilde).</summary>
public sealed record SiparisBaslikMetinleri(
    string SubeYok,
    string KayitListedeYok,
    string CariSinir,
    string CariDahaFazla,
    string UrunSinir,
    string UrunDahaFazla,
    string BaslikDuzenlendi,
    string Musteri,
    string Tedarikci,
    string HerIkisi,
    string Atolye,
    string Sarraf,
    string GeriAlmaNotu,
    string TerminalGerekce,
    string TerminalAlternatif);

public static class SiparisBaslikSozlugu
{
    public static readonly SiparisBaslikMetinleri Tr = new(
        SubeYok: "—",
        KayitListedeYok: "(listede olmayan kayıt)",
        CariSinir: "Listede en çok {0} aktif müşteri yer alır; aramak için listenin arama kutusuna yazın.",
        CariDahaFazla: "Daha fazla müşteri var: listede ada göre ilk {0} müşteri gösteriliyor, gerisi bu listede yok.",
        UrunSinir: "Listede en çok {0} aktif ürün yer alır; aramak için listenin arama kutusuna yazın.",
        UrunDahaFazla: "Daha fazla ürün var: listede koda göre ilk {0} ürün gösteriliyor; aradığınız yoksa kalemi ürün seçmeden “Serbest tanım” alanına yazın.",
        BaslikDuzenlendi: "Sipariş bilgileri düzeltildi",
        Musteri: "Müşteri",
        Tedarikci: "Tedarikçi",
        HerIkisi: "Müşteri ve tedarikçi",
        Atolye: "Atölye",
        Sarraf: "Sarraf",
        GeriAlmaNotu: "Düzenlemek için siparişi “Durumu değiştir” bölümündeki geri alma düğmeleriyle adım adım “Taslak” durumuna getirin; her geri alma gerekçeyle kaydedilir.",
        TerminalGerekce: "Bu sipariş teslim edildi ya da iptal edildi; kaydı artık değişmez.",
        TerminalAlternatif: "Yanlışlık varsa doğru bilgilerle yeni bir sipariş açın; bu kayıt geçmişte olduğu gibi kalır.");

    public static readonly SiparisBaslikMetinleri En = new(
        SubeYok: "—",
        KayitListedeYok: "(record not in the list)",
        CariSinir: "The list holds at most {0} active customers; type in the list's search box to find one.",
        CariDahaFazla: "There are more customers: the list shows the first {0} by name; the rest are not in this list.",
        UrunSinir: "The list holds at most {0} active products; type in the list's search box to find one.",
        UrunDahaFazla: "There are more products: the list shows the first {0} by code; if yours is missing, type the line into “Free-text description” without choosing a product.",
        BaslikDuzenlendi: "Order details corrected",
        Musteri: "Customer",
        Tedarikci: "Supplier",
        HerIkisi: "Customer and supplier",
        Atolye: "Workshop",
        Sarraf: "Money changer",
        GeriAlmaNotu: "To edit, move the order back step by step to “Draft” with the roll-back buttons under “Change status”; every roll-back is recorded with a reason.",
        TerminalGerekce: "This order was delivered or cancelled; its record no longer changes.",
        TerminalAlternatif: "If something is wrong, open a new order with the correct details; this record stays as it was.");

    public static SiparisBaslikMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
