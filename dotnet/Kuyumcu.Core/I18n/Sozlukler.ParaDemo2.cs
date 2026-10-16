namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-2 — tasarım hakemi t2 bulguları + PARA-DEMO açık kalanları (istemci metinleri)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni metin
//   Sozlukler.<Paket>.cs). EKSİK ANAHTAR = DERLEME HATASI: konumsal record; Tr ve En
//   aynı kurucudan geçer.
// ⚠ JARGON SIFIR, BÜYÜK HARFLE BAĞIRMA YOK, tek cümle (hakem t2 madde 6).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>PARA-DEMO-2 ekran metinleri (bir dilde).</summary>
public sealed record ParaDemo2Metinleri(
    // ── Madde 2: Günü Kapat ──
    string GunSayimBekleniyor,
    string GunOnizlemeYukleniyor,
    string Vazgec,
    // ── Madde 4: kasa kartı / "Kasada ne var?" ──
    string KasaAyrimSatiri,
    string KasaAyrimHata,
    // ── Madde 4/5: gün sonu ──
    string TahsilatTekSatir,
    string VirmanGirisSatiri,
    string AcilisDevriSatiri,
    string TahsilatTanimBaslik,
    string TahsilatTanimMetni,
    // ── Madde 6: has tahsilat uyarısı + takas esnaf kilidi ──
    string HasTlBorcUyari,
    string HasYolBaglanti,
    string TakasEsnafKilit,
    // ── Madde 7: tahsilat penceresi ──
    string AltinlaOdeEkle,
    string NotEkle,
    string GuncelBorc,
    string GuncelAlacak,
    string BorcYok,
    string GuncelAltin,
    string KasaTekSatir,
    // ── Madde 8: takas kimlik kilidi ──
    string TakasEksikBasit,
    string TakasEksikPro,
    string AlanSaticiTuru,
    string AlanAdSoyad,
    string AlanTckn,
    string AlanTcknHane,
    string AlanAdres,
    string AlanFaturaNo,
    // ── Madde 9: ekstre ──
    string EkstreHasBakiye,
    // ── Madde 1: BASİT tek ödeme satırı ──
    string AldimOdemeEtiket,
    // ── Tur 5: Panel "Kasada ne var?" alt satırı · tahsilat tutarı ──
    string KartHavaleEtiket,
    string TutarYaDaAltin);

/// <summary>PARA-DEMO-2 sözlüğü (TR + EN).</summary>
public static class ParaDemo2Sozlugu
{
    public static readonly ParaDemo2Metinleri Tr = new(
        GunSayimBekleniyor: "Çekmecedeki nakdi sayın ve «Sayılan nakit» kutusuna yazın.",
        GunOnizlemeYukleniyor: "Kasadaki nakit hesaplanıyor…",
        Vazgec: "Vazgeç",
        KasaAyrimSatiri: "Nakit {0} · Kart/Havale {1}",
        KasaAyrimHata: "Nakit ve kart ayrımı okunamadı: {0}",
        TahsilatTekSatir: "Tahsilat {0}: Nakit {1} · Kart {2} · Havale {3}",
        VirmanGirisSatiri: "Kasalar arası aktarım (tahsilata girmez)",
        AcilisDevriSatiri: "Açılış devri (tahsilata girmez)",
        TahsilatTanimBaslik: "Toplam tahsilat neyi sayar?",
        TahsilatTanimMetni: "Toplam tahsilat = o gün kasaya giren para (nakit + kart + havale + diğer); kasaya elle yapılan girişler de dahildir. Kasalar/banka arası aktarım ve açılış devri yeni para değildir; ayrı satırda gösterilir, toplama girmez.",
        HasTlBorcUyari: "Altın, müşterinin {0} borcunu düşürmez; borcu altınla kapatmak için altını önce «{1}» ile alın, sonra parayı burada girin.",
        HasYolBaglanti: "«{0}» ekranına git",
        TakasEsnafKilit: "Esnaf muafiyetli satıcıdan alınan altında kesinti yapılır; bunu «{0}» ve «{1}» olarak ayrı girin.",
        AltinlaOdeEkle: "+ Altınla öde",
        NotEkle: "+ Not ekle",
        GuncelBorc: BorcDiliSozlugu.BizeBorcluKalip("tr"), // P-DIL-BORC-KASA
        GuncelAlacak: BorcDiliSozlugu.BizBorcluyuzKalip("tr"),
        BorcYok: BorcDiliSozlugu.BorcYok("tr"),
        GuncelAltin: "Altın: {0}",
        KasaTekSatir: "Para «{0}» kasasına girer.",
        TakasEksikBasit: "Eksik: {0}.",
        TakasEksikPro: "Satıcının eksik bilgisi: {0} — gider pusulası için gerekli (VUK md.234).",
        AlanSaticiTuru: "satıcı türü",
        AlanAdSoyad: "ad soyad",
        AlanTckn: "TCKN",
        AlanTcknHane: "TCKN (11 hane)",
        AlanAdres: "adres",
        AlanFaturaNo: "satıcının fatura no'su",
        EkstreHasBakiye: "Bakiye {0} · {1}",
        AldimOdemeEtiket: "Ödeme şekli",
        KartHavaleEtiket: "Kart/Havale",
        TutarYaDaAltin: "Tutar ya da altın girin.");

    public static readonly ParaDemo2Metinleri En = new(
        GunSayimBekleniyor: "Count the cash in the drawer and enter it in «Counted cash».",
        GunOnizlemeYukleniyor: "Calculating the cash in the till…",
        Vazgec: "Cancel",
        KasaAyrimSatiri: "Cash {0} · Card/Transfer {1}",
        KasaAyrimHata: "The cash/card split could not be read: {0}",
        TahsilatTekSatir: "Collected {0}: Cash {1} · Card {2} · Transfer {3}",
        VirmanGirisSatiri: "Transfers between tills (not a collection)",
        AcilisDevriSatiri: "Opening balance (not a collection)",
        TahsilatTanimBaslik: "What does total collected count?",
        TahsilatTanimMetni: "Total collected = money that entered the tills that day (cash + card + transfer + other), including manual till entries. Transfers between tills/bank and the opening balance are not new money; they are shown on separate lines and are not added to the total.",
        HasTlBorcUyari: "Gold does not reduce the customer's {0} debt; to settle the debt with gold, first buy the gold with «{1}», then enter the money here.",
        HasYolBaglanti: "Go to «{0}»",
        TakasEsnafKilit: "Gold bought from a tax-exempt artisan carries a withholding; enter it as «{0}» and «{1}» separately.",
        AltinlaOdeEkle: "+ Pay with gold",
        NotEkle: "+ Add note",
        GuncelBorc: BorcDiliSozlugu.BizeBorcluKalip("en"),
        GuncelAlacak: BorcDiliSozlugu.BizBorcluyuzKalip("en"),
        BorcYok: BorcDiliSozlugu.BorcYok("en"),
        GuncelAltin: "Gold: {0}",
        KasaTekSatir: "The money goes into the «{0}» till.",
        TakasEksikBasit: "Missing: {0}.",
        TakasEksikPro: "Seller details missing: {0} — required for the expense voucher (Tax Procedure Law art. 234).",
        AlanSaticiTuru: "seller type",
        AlanAdSoyad: "full name",
        AlanTckn: "ID number",
        AlanTcknHane: "ID number (11 digits)",
        AlanAdres: "address",
        AlanFaturaNo: "seller's invoice no",
        EkstreHasBakiye: "Balance {0} · {1}",
        AldimOdemeEtiket: "Payment method",
        KartHavaleEtiket: "Card/Transfer",
        TutarYaDaAltin: "Enter an amount or gold.");

    public static ParaDemo2Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
