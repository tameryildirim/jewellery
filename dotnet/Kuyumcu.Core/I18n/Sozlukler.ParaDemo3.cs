namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-3 — PARA-DEMO-2 kapanış tasarım hakemi kalanları (istemci metinleri)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni metin
//   Sozlukler.<Paket>.cs). EKSİK ANAHTAR = DERLEME HATASI: konumsal record; Tr ve En
//   aynı kurucudan geçer.
// ⚠ BASİT metinlerde "MASAK" kelimesi GEÇMEZ (hakem madde 8); jargon yalnız PRO gerekçesinde.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>PARA-DEMO-3 ekran metinleri (bir dilde).</summary>
public sealed record ParaDemo3Metinleri(
    // ── KRİTİK: nakit payı kapısı reddi (sunucu cümlesi ayrıştırılıp yeniden yazılır) ──
    string NakitYetersiz,
    // ── YÜKSEK: takas/değişim birleşik kimlik kilidi ──
    string TakasEksikBasit,
    string TakasEksikPro,
    string GerekceGiderPusulasi,
    string GerekceKimlikTespiti,
    string GerekceBirlestir,
    string AlanKimlikNo,
    string AlanUyruk,
    string AlanIslemAmaci,
    string AlanBeyanAciklama,
    string AlanFonKaynagi,
    string AlanEkBelge,
    // ── ORTA: Gider "Kasa bakiyesi" → çekmecedeki nakit + toplam ──
    string GiderNakit,
    string GiderToplam,
    string GiderAyrimYok,
    // ── tur 2 (doğrulayıcı): Gider kilidi nakde bağlı · elle kart/havale araç payı reddi (istemci çevirisi) ──
    string GiderKilitNakit,
    string AracPayiYetersiz,
    string AracKart,
    string AracHavale);

/// <summary>PARA-DEMO-3 sözlüğü (TR + EN).</summary>
public static class ParaDemo3Sozlugu
{
    public static readonly ParaDemo3Metinleri Tr = new(
        NakitYetersiz: "'{0}' kasasında nakit yetersiz: çekmecede {1} nakit var, {2} nakit isteniyor. Kasadaki kart ({3}) ve havale ({4}) tahsilatı nakit olarak verilemez.",
        TakasEksikBasit: "Eksik: {0}.",
        TakasEksikPro: "Eksik bilgi: {0} — {1}.",
        GerekceGiderPusulasi: "gider pusulası için gerekli (VUK md.234)",
        GerekceKimlikTespiti: "müşteri kimlik tespiti için gerekli (MASAK)",
        GerekceBirlestir: "{0}; {1}",
        AlanKimlikNo: "kimlik belgesi no",
        AlanUyruk: "uyruk",
        AlanIslemAmaci: "işlem amacı",
        AlanBeyanAciklama: "beyan açıklaması (en az {0} karakter)",
        AlanFonKaynagi: "paranın kaynağı",
        AlanEkBelge: "ek belge",
        GiderNakit: "Çekmecedeki nakit",
        GiderToplam: "Kasa toplamı (kart ve havale dahil)",
        GiderAyrimYok: "Nakit ve kart ayrımı okunamadı; gösterilen tutar kasanın toplamıdır: {0}",
        GiderKilitNakit: "Çekmecedeki nakit yetmiyor: {0} var",
        AracPayiYetersiz: "'{0}' kasasındaki {1} tahsilatı {2}; {3} {1} olarak çıkarılamaz.",
        AracKart: "kart",
        AracHavale: "havale");

    public static readonly ParaDemo3Metinleri En = new(
        NakitYetersiz: "Not enough cash in '{0}': {1} cash in the drawer, {2} cash requested. Card ({3}) and transfer ({4}) takings in the till cannot be paid out as cash.",
        TakasEksikBasit: "Missing: {0}.",
        TakasEksikPro: "Missing details: {0} — {1}.",
        GerekceGiderPusulasi: "required for the expense voucher (Tax Procedure Law art. 234)",
        GerekceKimlikTespiti: "required for customer identification (AML)",
        GerekceBirlestir: "{0}; {1}",
        AlanKimlikNo: "ID document no",
        AlanUyruk: "nationality",
        AlanIslemAmaci: "purpose of transaction",
        AlanBeyanAciklama: "declaration text (at least {0} characters)",
        AlanFonKaynagi: "source of funds",
        AlanEkBelge: "supporting document",
        GiderNakit: "Cash in the drawer",
        GiderToplam: "Till total (card and transfer included)",
        GiderAyrimYok: "The cash/card split could not be read; the amount shown is the till total: {0}",
        GiderKilitNakit: "Not enough cash in the drawer: {0} available",
        AracPayiYetersiz: "'{0}' holds {2} of {1} takings; {3} cannot be paid out by {1}.",
        AracKart: "card",
        AracHavale: "transfer");

    public static ParaDemo3Metinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// PARA-DEMO-3 (hakem ORTA): ödeme türünün ekrandaki adı — gerçek karmada "Karışık" değil BACAK
    /// TÜRLERİ ("Kart + Havale"; sunucu <c>odemeAraclari</c>). Ekstre ekranı ve kâğıdıyla AYNI dil.
    /// <para>
    /// TEK AD ÜRETİCİSİ (tur 2, doğrulayıcı ORTA): gün sonu ve cari ekstresi bu yöntemi çağırır ve adlar
    /// TEK sözlükten — ekstrenin <see cref="ParaDemoSozlugu"/> ödeme adlarından — gelir. Tur 1'de gün sonu
    /// katalog sözlüğünü kullanıyordu: eski KARMA fiş gün sonunda "Karışık", ekstrede "Karma ödeme";
    /// İngilizcede "Bank transfer" / "Transfer" ayrışıyordu. Bilinmeyen kod uydurulmaz: ekstre sözlüğü
    /// tanımıyorsa katalog sözlüğünün davranışı (ham kodu çevirmeden döndürme kuralı) aynen korunur.
    /// </para>
    /// </summary>
    public static string OdemeAdi(string? odemeTuru, IReadOnlyList<string>? araclar, string dil)
    {
        var pd = ParaDemoSozlugu.Al(dil);
        return odemeTuru == "KARMA" && araclar is { Count: > 0 }
            ? string.Join(" + ", araclar.Select(a => pd.OdemeAdi("SATIS", 1m, 0m, a) ?? pd.OdemeDiger))
            : pd.OdemeAdi("SATIS", 1m, 0m, odemeTuru) ?? KatalogSozlugu.OdemeAraci(odemeTuru, dil);
    }
}
