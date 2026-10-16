namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — KARMA ÖDEMEDE ALTIN BACAĞI PANELİ: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır; aynı dalgada
//   başka şeritler de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI
//   dosya aç"). PozisyonSozlugu / GorunumSozlugu ile aynı desen.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da.
// ⚠ AYNI KAVRAM TEK AD: satıcı tipi, ad soyad, TCKN, adres, ayar, gram gibi
//   hurda alışıyla ORTAK kavramlar burada YOKTUR — Dil.T.Islem / Dil.T.Urun'dan
//   okunur; burada yalnız altın bacağına özgü metinler durur.
// ⚠ Jargon sıfır (PLAN § 5B): "mahsup", "stopaj" yerine gündelik dil.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Eski altınla ödeme panelinin bir dildeki metinleri.</summary>
public sealed record AltinBacagiMetinleri(
    string Baslik,
    string Aciklama,
    string AyarSecin,
    string GramGirin,
    string FiyatGirin,
    string FiyatReferans,
    string FiyatOnerildi,
    string FiyatElle,
    string HasKarsiligi,
    string FiseSayilan,
    string Hesaplandi,
    string BrutVeKesinti,
    string Fark,
    string FarkSifirOlmali,
    string IkiBelge,
    string TekBelgeMukellef,
    string EksikBaslik,
    string AdresGirin,
    string AyarlarYok,
    string TlYazilmaz,
    string PusulaDogdu,
    string TekOdemedeYok);

public static class AltinBacagiSozlugu
{
    public static readonly AltinBacagiMetinleri Tr = new(
        Baslik: "Eski altınla ödeme",
        Aciklama: "Müşterinin getirdiği altını tartın; sistem has karşılığını ve fişe sayılacak tutarı hesaplar.",
        AyarSecin: "Altının ayarını seçin",
        GramGirin: "Gramı girin (sıfırdan büyük)",
        FiyatGirin: "Has gram fiyatını girin — sistem alış fiyatını kendisi belirlemez",
        FiyatReferans: "Güncel has fiyatı (yalnız bilgi): {0}",
        FiyatOnerildi: "Ayarlardan önerildi",
        FiyatElle: "Elle girildi",
        HasKarsiligi: "Has karşılığı",
        FiseSayilan: "Fişe sayılan tutar",
        Hesaplandi: "Hesaplandı",
        BrutVeKesinti: "Brüt {0} · vergi kesintisi {1}",
        Fark: "Fark",
        FarkSifirOlmali: "Fark 0,00 TL olmadan fiş kaydedilemez",
        IkiBelge: "Kaydedince satış fişiyle birlikte gider pusulası da düzenlenir.",
        TekBelgeMukellef: "Satıcı vergi mükellefi: gider pusulası düzenlenmez, belge satıcının faturasıdır.",
        EksikBaslik: "Kaydetmek için:",
        AdresGirin: "Satıcı adresini girin (gider pusulası)",
        AyarlarYok: "Eski altın ayarları yüklenemedi; bu ödeme şu an eklenemez.",
        TlYazilmaz: "Tutar elle yazılmaz: gram, ayar ve fiyattan hesaplanır.",
        PusulaDogdu: "Eski altın için gider pusulası düzenlendi: {0}",
        TekOdemedeYok: "Eski altınla ödeme için '{0}' seçeneğini açıp '{1}' bacağı ekleyin.");

    public static readonly AltinBacagiMetinleri En = new(
        Baslik: "Payment with old gold",
        Aciklama: "Weigh the customer's gold; the system computes its fine-gold equivalent and the amount counted on the receipt.",
        AyarSecin: "Select the gold's karat",
        GramGirin: "Enter grams (greater than zero)",
        FiyatGirin: "Enter the fine-gram price — the system does not set the buying price itself",
        FiyatReferans: "Current fine-gold price (for reference only): {0}",
        FiyatOnerildi: "Suggested by settings",
        FiyatElle: "Entered manually",
        HasKarsiligi: "Fine-gold equivalent",
        FiseSayilan: "Amount counted on receipt",
        Hesaplandi: "Calculated",
        BrutVeKesinti: "Gross {0} · tax withholding {1}",
        Fark: "Difference",
        FarkSifirOlmali: "The receipt cannot be saved until the difference is 0.00",
        IkiBelge: "Saving also issues an expense voucher together with the sales receipt.",
        TekBelgeMukellef: "Seller is a registered taxpayer: no expense voucher; the seller's invoice is the document.",
        EksikBaslik: "To save:",
        AdresGirin: "Enter the seller's address (expense voucher)",
        AyarlarYok: "Old-gold settings could not be loaded; this payment cannot be added now.",
        TlYazilmaz: "The amount is not typed: it is computed from grams, karat and price.",
        PusulaDogdu: "Expense voucher issued for the old gold: {0}",
        TekOdemedeYok: "To take old gold as payment, turn on '{0}' and add a '{1}' part.");

    /// <summary>Seçili dilin metinleri.</summary>
    public static AltinBacagiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
