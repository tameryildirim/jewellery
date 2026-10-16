namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO — UZUN DEMO KAYDININ PARA / YASAL BULGULARI (istemci metinleri)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler de
//   ona dokunuyor (orkestratör koşulu: yeni metin Sozlukler.<Paket>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ JARGON SIFIR: "has", "kasa hareketi", "storno" gibi terimler tezgâhtara
//   açıklamasız yazılmaz; ham kod (NAKIT, KART) ekrana basılmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>PARA-DEMO ekran metinleri (bir dilde).</summary>
public sealed record ParaDemoMetinleri(
    // ── Madde 1: Günü Kapat — nakit sayımı ──
    string KasaToplam,
    string KartSayimaGirmez,
    string HavaleSayimaGirmez,
    string SayilacakNakit,
    string NakitAyrimNotu,
    string OnizlemeHata,
    string OnizlemeKilit,
    // ── Madde 1: gün sonu mutabakatı alt kalemleri ──
    string MutabakatAlt,
    string MutabakatAltDiger,
    // ── Madde 2: tahsilatta altın ──
    string TahsilatIpucu,
    string HasGramEtiket,
    string HasGramIpucu,
    string HasTlBorcUyari,
    string TahsilatKaydedildi,
    // ── Madde 3: takasta satıcı kimliği ──
    string TakasKimlikNotu,
    string TakasEsnafKilit,
    string TakasPusulaNotu,
    // ── Madde 4: Peşin Müşteri + veresiye ──
    string PesinVeresiyeKilit,
    // ── Madde 5: ekstre ──
    string EkstreSutunBakiye,
    string EkstrePesinOdendi,
    string EkstreBakiyeSonra,
    string EkstreBakiyeBilinmiyor,
    string OdemeNakit,
    string OdemeKart,
    string OdemeHavale,
    string OdemeVeresiye,
    string OdemeKarma,
    string OdemeEskiAltin,
    string OdemeDoviz,
    string OdemeDiger,
    string OdemeHas,
    // ── Tur 2: kasadan virmanda aktarılan paranın aracı ──
    string VirmanAracEtiket,
    string VirmanKartPayi,
    string VirmanHavalePayi,
    string VirmanAracIpucu,
    string VirmanPayAsildi,
    string VirmanAyrimHata)
{
    /// <summary>
    /// Ödeme türünün ekrandaki adı (UX-S31: ham kod basılmaz). Has ile yapılan tahsilat
    /// (TL 0, gram ≠ 0) "Altın" yazar. Bilinmeyen kod UYDURULMAZ → null (yalnız tür yazılır).
    /// </summary>
    public string? OdemeAdi(string tur, decimal tl, decimal has, string? odemeTuru)
    {
        if (tur == "TAHSILAT" && tl == 0m && has != 0m) return OdemeHas;
        return odemeTuru switch
        {
            "NAKIT" => OdemeNakit,
            "KART" => OdemeKart,
            "HAVALE" => OdemeHavale,
            "VERESIYE" => OdemeVeresiye,
            "KARMA" => OdemeKarma,
            "ALTIN" => OdemeEskiAltin,
            "DOVIZ" => OdemeDoviz,
            "DIGER" => OdemeDiger,
            _ => null,
        };
    }
}

public static class ParaDemoSozlugu
{
    public static readonly ParaDemoMetinleri Tr = new(
        KasaToplam: "Kasadaki toplam",
        KartSayimaGirmez: "Kart (sayıma girmez)",
        HavaleSayimaGirmez: "Havale (sayıma girmez)",
        SayilacakNakit: "Sayılacak nakit",
        NakitAyrimNotu: "Kartla ve havaleyle alınan para çekmecede durmaz; yalnız nakit sayılır.",
        OnizlemeHata: "Çekmecedeki nakit hesaplanamadı: {0}",
        OnizlemeKilit: "Çekmecedeki nakit hesaplanamadı — pencereyi kapatıp yeniden açın.",
        MutabakatAlt: "Nakit {0} · Kart {1} · Havale {2}",
        MutabakatAltDiger: " · Diğer {0}",
        TahsilatIpucu: "Müşteriden alınan ödeme. Para TL borcunu düşürür; altın, müşterinin altın hesabına yazılır.",
        HasGramEtiket: "Altınla ödeme (saf altın gramı)",
        HasGramIpucu: "Müşterinin altın hesabına yazılır; TL borcu değişmez.",
        HasTlBorcUyari: "Bu müşterinin {0} borcu var. Girdiğiniz altın bu borcu DÜŞÜRMEZ; müşterinin altın hesabına yazılır. Borcu altınla kapatmak için önce Satış → Hurda Alış'ta altını satın alın, sonra aynı tutarı burada para olarak girin.",
        TahsilatKaydedildi: "Tahsilat kaydedildi · Borç: {0} · Altın: {1}",
        TakasKimlikNotu: "Müşteriden alınan eski altın için kimlik gerekir (gider pusulası).",
        TakasEsnafKilit: "Esnaf muafiyetli satıcıdan alınan altında kesinti yapılır; bu işlemi Hurda Alış ve Satış olarak ayrı girin.",
        TakasPusulaNotu: "Gider pusulası: {0}",
        PesinVeresiyeKilit: "Peşin Müşteri'ye veresiye yazılamaz — müşteri seçin ya da ödemeyi değiştirin.",
        EkstreSutunBakiye: "Bakiye",
        EkstrePesinOdendi: "{0} · ödendi, borca yazılmadı",
        EkstreBakiyeSonra: "Bakiye {0}",
        EkstreBakiyeBilinmiyor: "Bakiye —",
        OdemeNakit: "Nakit",
        OdemeKart: "Kart",
        OdemeHavale: "Havale",
        OdemeVeresiye: "Veresiye",
        OdemeKarma: "Karma ödeme",
        OdemeEskiAltin: "Eski altın",
        OdemeDoviz: "Döviz",
        OdemeDiger: "Diğer",
        OdemeHas: "Altın",
        VirmanAracEtiket: "Ne aktarılıyor?",
        VirmanKartPayi: "Kartla alınan para (kasada {0})",
        VirmanHavalePayi: "Havaleyle alınan para (kasada {0})",
        VirmanAracIpucu: "Kartla ve havaleyle alınan para çekmecede durmaz. Bankaya geçen bu parayı buradan seçin; nakit sayımı bozulmaz.",
        VirmanPayAsildi: "Seçilen ödemeden kasada yalnız {0} var.",
        VirmanAyrimHata: "Kasadaki kart ve havale payı okunamadı: {0} — aktarım nakit sayılır.");

    public static readonly ParaDemoMetinleri En = new(
        KasaToplam: "Register total",
        KartSayimaGirmez: "Card (not counted)",
        HavaleSayimaGirmez: "Transfer (not counted)",
        SayilacakNakit: "Cash to count",
        NakitAyrimNotu: "Card and bank transfer payments are not in the drawer; only cash is counted.",
        OnizlemeHata: "The cash in the drawer could not be calculated: {0}",
        OnizlemeKilit: "The cash in the drawer could not be calculated — close and reopen this panel.",
        MutabakatAlt: "Cash {0} · Card {1} · Transfer {2}",
        MutabakatAltDiger: " · Other {0}",
        TahsilatIpucu: "Payment received from the customer. Money lowers the debt; gold goes to the customer's gold account.",
        HasGramEtiket: "Payment in gold (pure gold grams)",
        HasGramIpucu: "Goes to the customer's gold account; the money debt does not change.",
        HasTlBorcUyari: "This customer owes {0}. The gold you enter does NOT lower this debt; it goes to the customer's gold account. To settle the debt with gold, first buy the gold under Sale → Scrap Purchase, then enter the same amount here as money.",
        TahsilatKaydedildi: "Payment saved · Debt: {0} · Gold: {1}",
        TakasKimlikNotu: "Identity is required for the old gold taken from the customer (expense voucher).",
        TakasEsnafKilit: "Gold bought from an exempt tradesman has a withholding; enter this as a Scrap Purchase and a Sale separately.",
        TakasPusulaNotu: "Expense voucher: {0}",
        PesinVeresiyeKilit: "Credit cannot be written to the Walk-in Customer — choose a customer or change the payment.",
        EkstreSutunBakiye: "Balance",
        EkstrePesinOdendi: "{0} · paid, not added to the debt",
        EkstreBakiyeSonra: "Balance {0}",
        EkstreBakiyeBilinmiyor: "Balance —",
        OdemeNakit: "Cash",
        OdemeKart: "Card",
        OdemeHavale: "Transfer",
        OdemeVeresiye: "Credit",
        OdemeKarma: "Mixed payment",
        OdemeEskiAltin: "Old gold",
        OdemeDoviz: "Foreign currency",
        OdemeDiger: "Other",
        OdemeHas: "Gold",
        VirmanAracEtiket: "What is being moved?",
        VirmanKartPayi: "Card payments (in register {0})",
        VirmanHavalePayi: "Bank transfer payments (in register {0})",
        VirmanAracIpucu: "Card and bank transfer payments are not in the drawer. Choose them here when they reach the bank; the cash count stays correct.",
        VirmanPayAsildi: "The register holds only {0} of the chosen payment.",
        VirmanAyrimHata: "The register's card and transfer share could not be read: {0} — the move counts as cash.");

    public static ParaDemoMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
