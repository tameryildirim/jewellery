namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET S3-4 — RAPORLAR "BUGÜN" TEK SAYFA (basit mod yüzü): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs"). PanelSadeSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer, bir dilde alan unutulursa derleme
//   kırılır.
//
// ⚠ JARGON SIFIR (PLAN § 5B): hedef kitle ilkokul/lise mezunu tezgâhtar.
//   Başlıklar gündelik sorudur ("Bugün ne aldım?"); "mutabakat", "cari",
//   "tahsilat" başlıkta YOK. Etiket ≤ 3 kelime, mesaj ≤ 2 cümle.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Raporlar ekranının basit mod ("Bugün") yüzündeki tüm metinler (bir dilde).</summary>
public sealed record RaporBugunMetinleri(
    string Baslik,
    string Aciklama,
    string BugunNeSattim,
    string SatisAdet,
    string SatisYok,
    string BugunNeAldim,
    string AlisAdet,
    string AlisYok,
    string Hurda,
    string IslemBaglantiIpucu,
    string KasadaNeVar,
    string SuAnkiPara,
    string KasaYok,
    string KasaBaglantiIpucu,
    string BekleyenIs,
    string BekleyenYok,
    string BekleyenSinir,
    string Tumu,
    string GunSonu,
    string GunSonuTamam,
    string FarkVar,
    string FarkAciklama,
    string FaturasizSatis,
    string FaturaKes,
    string BagsizFatura,
    string FaturalaraBak,
    string FaturalariKontrolEt,
    string YetkiYok,
    string DigerRaporlar,
    string GorunumuDegistir,
    string HasBirim);

public static class RaporBugunSozlugu
{
    public static readonly RaporBugunMetinleri Tr = new(
        Baslik: "Bugün",
        Aciklama: "Bugünün özeti. Sayıya dokununca kayıtlar açılır.",
        BugunNeSattim: "Bugün ne sattım?",
        SatisAdet: "{0} satış",
        SatisYok: "Bugün henüz satış yok.",
        BugunNeAldim: "Bugün ne aldım?",
        AlisAdet: "{0} alış",
        AlisYok: "Bugün henüz alış yok.",
        Hurda: "Hurda",
        IslemBaglantiIpucu: "Bugünkü işlemleri aç",
        KasadaNeVar: "Kasada ne var?",
        SuAnkiPara: "Şu anki para",
        KasaYok: "Henüz kasa yok.",
        KasaBaglantiIpucu: "Kasaları aç",
        BekleyenIs: "Bekleyen iş",
        BekleyenYok: "Bekleyen iş yok.",
        BekleyenSinir: "İlk {0} iş gösterildi. Hepsi Bildirimler ekranında.",
        Tumu: "Tümü",
        GunSonu: "Gün sonu",
        GunSonuTamam: "Tamam, fark yok.",
        FarkVar: "Fark var",
        FarkAciklama: "Kasaya giren para ile kesilen faturalar tutmuyor.",
        FaturasizSatis: "Faturasız satış: {0}",
        FaturaKes: "Fatura kes",
        BagsizFatura: "Satışa bağlanmamış fatura: {0}",
        FaturalaraBak: "Faturalara bak",
        FaturalariKontrolEt: "Faturaları kontrol et",
        YetkiYok: "Bu bilgiyi yalnız yönetici görür.",
        DigerRaporlar: "Aylık özet, borç yaşı ve stok raporları profesyonel görünümde.",
        GorunumuDegistir: "Görünümü değiştir",
        HasBirim: "g has");

    public static readonly RaporBugunMetinleri En = new(
        Baslik: "Today",
        Aciklama: "Today's summary. Tap a number to open its records.",
        BugunNeSattim: "What did I sell today?",
        SatisAdet: "{0} sales",
        SatisYok: "No sales yet today.",
        BugunNeAldim: "What did I buy today?",
        AlisAdet: "{0} purchases",
        AlisYok: "No purchases yet today.",
        Hurda: "Scrap",
        IslemBaglantiIpucu: "Open today's transactions",
        KasadaNeVar: "What is in the till?",
        SuAnkiPara: "Money right now",
        KasaYok: "No tills yet.",
        KasaBaglantiIpucu: "Open tills",
        BekleyenIs: "Pending work",
        BekleyenYok: "No pending work.",
        BekleyenSinir: "First {0} items shown. All are on the Notifications screen.",
        Tumu: "All",
        GunSonu: "End of day",
        GunSonuTamam: "All good, no difference.",
        FarkVar: "Difference",
        FarkAciklama: "Money taken in does not match the invoices issued.",
        FaturasizSatis: "Sales without invoice: {0}",
        FaturaKes: "Issue invoice",
        BagsizFatura: "Invoices not linked to a sale: {0}",
        FaturalaraBak: "Open invoices",
        FaturalariKontrolEt: "Check invoices",
        YetkiYok: "Only managers can see this.",
        DigerRaporlar: "Monthly summary, debt age and stock reports are in the professional view.",
        GorunumuDegistir: "Change view",
        HasBirim: "g fine gold");

    public static RaporBugunMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
