namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-GUNSONU — (1) Gün sonu "Fark var" tutarının ADI (sunucunun FARK tanımı
// DEĞİŞMEZ: satış toplamı − fişe bağlı faturalar − bağsız faturalar; yalnız ekranda ne
// olduğu yazılır), (2) Değişim (takas) BASİT fişi Verdim/Aldım kalıbında, (3) sayfa
// başlığı seçili tür. Yasal kural/eşik/kapı DEĞİŞMEZ (MASAK kimlik kapısı aynen; nakit
// sınırı sayısı sunucudan /tevsik/gunluk — burada sayı YOK).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>DEGISIM-GUNSONU metinleri (bir dilde).</summary>
public sealed record DegisimGunSonuMetinleri(
    // ── gün sonu: FARK'ın adı (sunucu tanımına göre üç durum) ──
    string FarkFaturasiz,
    string FarkFazlaFatura,
    string FarkGenel,
    string EtiketTutar,
    string AciklamaFaturasiz,
    string AciklamaFazlaFatura,
    string AciklamaGenel,
    string FaturasizAdet,
    string SatisToplami,
    string AdetTutar,
    // ── Değişim (takas) BASİT fişi: Verdim / Aldım ──
    string VerdimUrun,
    string UrunTutari,
    string VerdimBos,
    string AldimEskiAltin,
    string EskiGram,
    string EskiHasFiyati,
    string YeniFiyat,
    string FarkOdemesi,
    string FarkYok,
    string FarkBekleniyor,
    string KimlikBaslikSade,
    string KimlikNotuSade,
    string SaticiTipiSade,
    // ── tur 2: Peşin Müşteri bu tutarda olmaz → kilit tek adım ──
    string OnceMusteriSecin,
    // ── DEGISIM-2: BASİT kimlik tek dil ("satıcı" kuyumcunun kendisi demek) + hata/kilit adı = etiket ──
    string KimGetirdiSecin,
    string KilitKimGetirdi,
    string GetireninAdSoyad,
    string TcKimlikNo,
    string GetireninAdresi,
    // ── DEGISIM-2: BASİT farkın ödemesi (Satış'la aynı salt okunur satır, cümle düzeni) + VERDİM arama etiketi ──
    string OdemeTuruSade,
    string VerdimAramaEtiketi,
    // ── DEGISIM-2: gün sonu — bağsız fatura TEK ad, uyarı sayacı ne saydığını söyler ──
    string BagsizFaturaListe,
    string BagsizUyari,
    string NotlarUyariSayili);

/// <summary>DEGISIM-GUNSONU sözlüğü (TR + EN).</summary>
public static class DegisimGunSonuSozlugu
{
    public static readonly DegisimGunSonuMetinleri Tr = new(
        FarkFaturasiz: "Faturası kesilmemiş satış",
        FarkFazlaFatura: "Satıştan fazla kesilen fatura",
        FarkGenel: "Satış ile fatura farkı",
        EtiketTutar: "{0}: {1}",
        AciklamaFaturasiz: "Bu satışların faturası henüz kesilmedi.",
        AciklamaFazlaFatura: "Kesilen faturalar günün satışından fazla.",
        AciklamaGenel: "Günün satışı ile kesilen faturalar tutmuyor.",
        FaturasizAdet: "{0} satışın faturası yok",
        SatisToplami: "Satış toplamı",
        AdetTutar: "{0} adet · {1}",
        VerdimUrun: "Verdim · ürün",
        UrunTutari: "Ürün tutarı",
        VerdimBos: "Müşteriye verilecek ürünü arayın ya da barkodunu okutun.",
        AldimEskiAltin: "Aldım · eski altın",
        EskiGram: "Gram",
        EskiHasFiyati: "Has fiyatı (1 gram)",
        YeniFiyat: "Fiyat",
        FarkOdemesi: "Farkın ödemesi",
        FarkYok: "Fark yok",
        FarkBekleniyor: "Ürün ve eski altın girilince fark hesaplanır.",
        KimlikBaslikSade: "Eski altını getiren",
        KimlikNotuSade: "Eski altını getiren kişinin kimliği yazılır.",
        SaticiTipiSade: "Kim getirdi?",
        OnceMusteriSecin: "Önce müşteri seçin.",
        KimGetirdiSecin: "Kim getirdi? seçin.",
        KilitKimGetirdi: "kim getirdi",
        GetireninAdSoyad: "Getirenin adı soyadı",
        TcKimlikNo: "TC kimlik no",
        GetireninAdresi: "Adresi",
        OdemeTuruSade: "Ödeme türü",
        VerdimAramaEtiketi: "Ürün",
        BagsizFaturaListe: "Satışa bağlanmamış faturalar",
        BagsizUyari: "{0} satışa bağlanmamış fatura (toplam {1}) — elle kesilmiş olabilir",
        NotlarUyariSayili: "Ayrıntı ({0} uyarı)");

    public static readonly DegisimGunSonuMetinleri En = new(
        FarkFaturasiz: "Sales without an invoice",
        FarkFazlaFatura: "Invoiced more than sold",
        FarkGenel: "Sales vs. invoices difference",
        EtiketTutar: "{0}: {1}",
        AciklamaFaturasiz: "No invoice has been issued for these sales yet.",
        AciklamaFazlaFatura: "Invoices issued exceed the day's sales.",
        AciklamaGenel: "Today's sales do not match the invoices issued.",
        FaturasizAdet: "{0} sales without an invoice",
        SatisToplami: "Sales total",
        AdetTutar: "{0} pcs · {1}",
        VerdimUrun: "Given · item",
        UrunTutari: "Item amount",
        VerdimBos: "Search for the item to give or scan its barcode.",
        AldimEskiAltin: "Received · old gold",
        EskiGram: "Grams",
        EskiHasFiyati: "Fine gold price (1 gram)",
        YeniFiyat: "Price",
        FarkOdemesi: "Payment of the difference",
        FarkYok: "No difference",
        FarkBekleniyor: "The difference appears once the item and the old gold are entered.",
        KimlikBaslikSade: "Person bringing the old gold",
        KimlikNotuSade: "The identity of the person bringing the old gold is recorded.",
        SaticiTipiSade: "Who brought it?",
        OnceMusteriSecin: "Select a customer first.",
        KimGetirdiSecin: "Select who brought it.",
        KilitKimGetirdi: "who brought it",
        GetireninAdSoyad: "Full name of the person",
        TcKimlikNo: "ID number",
        GetireninAdresi: "Address",
        OdemeTuruSade: "Payment type",
        VerdimAramaEtiketi: "Item",
        BagsizFaturaListe: "Invoices not linked to a sale",
        BagsizUyari: "{0} invoices not linked to a sale (total {1}) — may have been issued manually",
        NotlarUyariSayili: "Details ({0} warnings)");

    public static DegisimGunSonuMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
