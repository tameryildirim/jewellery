namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-2 — TEZGÂH FİŞİ: DÜĞME IZGARASI + VERDİM / ALDIM + FARK · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç").
//   PesinMusteriSozlugu / AltinBacagiSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ Jargon sıfır, etiket ≤ 3 kelime (PLAN § 5B). Tür düğmelerinin adları
//   BURADA YOK: pro sekmeleriyle aynı kaynaktan (Dil.T.Islem.*) gelir — aynı
//   kavram iki ekranda iki ad taşımaz (UX-S31).
//
// ⚠ EŞİK SAYISI YOK (PLAN § 3 md.6): hiçbir metin tutar sınırı söylemez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tezgâh fişinin (basit mod) bir dildeki tüm metinleri.</summary>
public sealed record TezgahFisiMetinleri(
    string IzgaraEtiket,
    string Odeme,
    string OdemeKilitSebebi,
    string TahsilatIpucu,
    string EmanetIpucu,
    string VerdimBaslik,
    string AldimBaslik,
    string VerdimBos,
    string Toplam,
    string Fark,
    string FarkTamam,
    string FarkEksik,
    string FarkFazla,
    string TekOdeme,
    string OdemeyiBol,
    string TekOdemeyeDon,
    string AcikHesapKaydet,
    string AcikHesapPesin,
    string AcikHesapCariYok,
    string AcikHesapVarZaten,
    string ElleEkle);

/// <summary>Tezgâh fişi metinleri — TR + EN.</summary>
public static class TezgahFisiSozlugu
{
    public static readonly TezgahFisiMetinleri Tr = new(
        IzgaraEtiket: "Ne yapıyorsun?",
        Odeme: "Ödeme",
        OdemeKilitSebebi: "Tedarikçiye ödeme bu ekrandan yapılmaz. Alışta ödeme türünü seçin.",
        TahsilatIpucu: "Müşterinin borcunu Müşteriler ekranında alırsınız.",
        EmanetIpucu: "Emanet ekranı açılır.",
        VerdimBaslik: "Verdim · ürünler",
        AldimBaslik: "Aldım · ödeme",
        VerdimBos: "Ürünü arayın ya da barkodunu okutun.",   // SATIS-3 t3: ayrı "Elle ekle" yok (KAYNAK 0)
        Toplam: "Toplam",
        Fark: "Fark",
        FarkTamam: "Fark yok, kaydedebilirsiniz.",
        FarkEksik: "Ödeme eksik. Fark kapanmadan kaydedilmez.",
        FarkFazla: "Ödeme fazla. Tutarları azaltın.",
        TekOdeme: "Tamamı tek ödemeyle alınıyor.",
        OdemeyiBol: "Ödemeyi böl",
        TekOdemeyeDon: "Tek ödemeye dön",
        AcikHesapKaydet: "Açık hesap olarak kaydet",
        AcikHesapPesin: "Peşin Müşteri'ye açık hesap yazılamaz. Gerçek müşteri seçin.",
        AcikHesapCariYok: "Açık hesap için müşteri seçin.",
        AcikHesapVarZaten: "Açık hesap satırı zaten var. Tutarını düzeltin.",
        ElleEkle: "Elle ekle");

    public static readonly TezgahFisiMetinleri En = new(
        IzgaraEtiket: "What are you doing?",
        Odeme: "Payment",
        OdemeKilitSebebi: "Supplier payments are not made on this screen. Choose the payment type on the purchase.",
        TahsilatIpucu: "Collect a customer's debt on the Customers screen.",
        EmanetIpucu: "Opens the safekeeping screen.",
        VerdimBaslik: "Given · items",
        AldimBaslik: "Received · payment",
        VerdimBos: "Search for the item or scan its barcode.",
        Toplam: "Total",
        Fark: "Difference",
        FarkTamam: "No difference, you can save.",
        FarkEksik: "Payment is short. It cannot be saved until the difference is zero.",
        FarkFazla: "Payment is too much. Lower the amounts.",
        TekOdeme: "Paid in full with one payment.",
        OdemeyiBol: "Split payment",
        TekOdemeyeDon: "Back to one payment",
        AcikHesapKaydet: "Save as open account",
        AcikHesapPesin: "No open account for the Walk-in Customer. Pick a real customer.",
        AcikHesapCariYok: "Pick a customer for an open account.",
        AcikHesapVarZaten: "There is already an open account line. Fix its amount.",
        ElleEkle: "Add by hand");

    /// <summary>Seçili dilin metinleri.</summary>
    public static TezgahFisiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
