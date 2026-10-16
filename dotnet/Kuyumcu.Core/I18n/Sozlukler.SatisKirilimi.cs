namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET G-5 (B-4 devri) — RAPORLAR: SATIŞLARIN ÖDEME KIRILIMI — TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs'e). RaporBugun ile aynı desen.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record.
// ⚠ Nakit/Kart/Havale/Döviz/Veresiye adları BURADA YOK: aynı kavram tek ad —
//   Dil.T.Islem.Odeme* anahtarlarından okunur. Yalnız yeni kavramlar burada.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Raporlar (profesyonel yüz) satış ödeme kırılımı kartının metinleri.</summary>
public sealed record SatisKirilimiMetinleri(
    string Baslik,
    string Aciklama,
    string EskiAltinMahsup,
    string EskiAltinNotu,
    string Kayitsiz,
    string KayitsizNotu,
    string Diger,
    string Arac,
    string Tutar,
    string FisSayisi,
    string KasayaGirer,
    string KasayaGirmez,
    string KasayaGiren,
    string KasayaGirenAlt,
    string BrutSatis,
    string BrutSatisAlt,
    string Fark,
    string FarkUyari,
    string SatisYok,
    string SunucuHesabi);

public static class SatisKirilimiSozlugu
{
    public static readonly SatisKirilimiMetinleri Tr = new(
        Baslik: "Satışlar neyle ödendi?",
        Aciklama: "Seçili ayın satışları, ödeme şekline göre.",
        EskiAltinMahsup: "Eski altın (mahsup)",
        EskiAltinNotu: "Müşterinin verdiği eski altın. Kasaya para olarak girmez.",
        Kayitsiz: "Ödeme şekli kayıtsız",
        KayitsizNotu: "{0} eski satışın ödeme şekli kaydedilmemiş.",
        Diger: "Diğer",
        Arac: "Ödeme şekli",
        Tutar: "Tutar",
        FisSayisi: "Fiş",
        KasayaGirer: "Kasaya girer",
        KasayaGirmez: "Kasaya girmez",
        KasayaGiren: "Kasaya giren",
        KasayaGirenAlt: "Eski altın ve veresiye hariç",
        BrutSatis: "Satış toplamı",
        BrutSatisAlt: "Eski altın düşülmeden",
        Fark: "Fark",
        FarkUyari: "Ödeme toplamı satış toplamını tutmuyor. Fark: {0}",
        SatisYok: "Bu ay satış yok.",
        SunucuHesabi: "Sunucu hesabı");

    public static readonly SatisKirilimiMetinleri En = new(
        Baslik: "How were sales paid?",
        Aciklama: "Sales of the selected month, by payment method.",
        EskiAltinMahsup: "Old gold (trade-in)",
        EskiAltinNotu: "Old gold given by the customer. It does not enter the till as cash.",
        Kayitsiz: "Payment method not recorded",
        KayitsizNotu: "{0} older sales have no recorded payment method.",
        Diger: "Other",
        Arac: "Payment method",
        Tutar: "Amount",
        FisSayisi: "Receipts",
        KasayaGirer: "Enters till",
        KasayaGirmez: "Does not enter till",
        KasayaGiren: "Into the till",
        KasayaGirenAlt: "Old gold and credit excluded",
        BrutSatis: "Total sales",
        BrutSatisAlt: "Before old gold is deducted",
        Fark: "Difference",
        FarkUyari: "Payments do not match total sales. Difference: {0}",
        SatisYok: "No sales this month.",
        SunucuHesabi: "Server calculation");

    public static SatisKirilimiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
