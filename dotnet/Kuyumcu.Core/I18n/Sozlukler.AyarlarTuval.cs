namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K3 — AYARLAR TUVALE: Dükkân / Kur / Yazıcı sekmelerinin metinleri.
//
// Tuval Ayarlar.dc.html bu üç sekmenin YALNIZ adını verir (içerik çizilmemiş).
// Ölçüm (koda karşı): dükkân kimliği /isletme'de, kur kaynağı/geçmişi /kur'da
// yaşar; yazıcı/kâğıt AYARI üründe YOKTUR (uç yok, tercih anahtarı yok). Bu
// yüzden sekmeler UYDURMA ayar göstermez: gerçeği söyler ve ayarın yaşadığı
// ekrana bağlanır (UX-S31 tek kaynak — ikinci form açılmaz).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; iki dil aynı
//   kurucudan geçer. Jargon yok (hedef kitle tezgâhtar).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>/ayarlar Dükkân · Kur · Yazıcı sekmelerinin metinleri (bir dilde).</summary>
public sealed record AyarlarTuvalMetinleri(
    string DukkanBaslik,
    string DukkanAciklama,
    string DukkanGit,
    string DukkanYetkiYok,
    string KurBaslik,
    string KurAciklama,
    string KurGit,
    string YaziciBaslik,
    string YaziciAciklama,
    string YaziciEtiketGit,
    string YetkiYok,
    string SistemDestekBaslik,
    string DukkanGorunumBaslik);

/// <summary>T-K3 metinleri — TR + EN.</summary>
public static class AyarlarTuvalSozlugu
{
    public static readonly AyarlarTuvalMetinleri Tr = new(
        DukkanBaslik: "İşletme bilgileri",
        DukkanAciklama: "İşletmenin adı, vergi numarası, adresi ve şubeleri İşletme ekranında tutulur.",
        DukkanGit: "İşletme ekranını aç",
        DukkanYetkiYok: "İşletme bilgilerini yalnız işletme sahibi değiştirir.",
        KurBaslik: "Kur kaynağı ve geçmişi",
        KurAciklama: "Kurlar kendiliğinden yenilenir; diğer ekranların üst barındaki saat son yenilemeyi gösterir. Kurun nereden alınacağı, pariteler ve kur geçmişi Kur ekranındadır.",
        KurGit: "Kur ekranını aç",
        YaziciBaslik: "Yazdırma",
        YaziciAciklama: "Fiş, fatura ve gider pusulası bilgisayarın yazdırma penceresiyle basılır; yazıcıyı ve kâğıt boyunu o pencereden seçersin. Seçimini bilgisayar hatırlar.",
        YaziciEtiketGit: "Ürün etiketi yazdır",
        YetkiYok: "Bu ekrana yetkin yok — işletme sahibine sor.",
        SistemDestekBaslik: "Daha fazla: sistem bilgisi ve destek",
        DukkanGorunumBaslik: "Daha fazla: işletmenin görünümünü tek tek seç");

    public static readonly AyarlarTuvalMetinleri En = new(
        DukkanBaslik: "Shop details",
        DukkanAciklama: "The shop's name, tax number, address and branches are kept on the Business screen.",
        DukkanGit: "Open Business screen",
        DukkanYetkiYok: "Only the shop owner can change shop details.",
        KurBaslik: "Rate source and history",
        KurAciklama: "Rates refresh on their own; the clock in the top bar of the other screens shows the last refresh. Where rates come from, cross rates and rate history are on the Rates screen.",
        KurGit: "Open Rates screen",
        YaziciBaslik: "Printing",
        YaziciAciklama: "Receipts, invoices and expense vouchers print through the computer's print window; you choose the printer and paper size there. The computer remembers your choice.",
        YaziciEtiketGit: "Print product labels",
        YetkiYok: "You do not have access to this screen — ask the shop owner.",
        SistemDestekBaslik: "More: system info and support",
        DukkanGorunumBaslik: "More: pick the shop's appearance one by one");

    public static AyarlarTuvalMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
