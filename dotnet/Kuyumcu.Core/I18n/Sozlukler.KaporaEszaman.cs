namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN — sipariş detayında kapora geçmişi (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed record KaporaGecmisMetinleri(
    string Baslik,            // katlanır başlık
    string Guncel,            // rozet: bugün geçerli kapora
    string GeriVerildi,       // "{0}" = geri verildiği tarih-saat
    string Gecerli,           // durum: hâlâ geçerli
    string OdemeNakit,
    string OdemeKart,
    string OdemeHavale,
    // tur 2 (tasarım hakemi KRİTİK): büyük kaporada kimlik alanları — jargon ve ham alan adı YOK
    string MasakNot,          // alanlar açılınca tek sade cümle
    string MasakHazirlaniyor, // gereksinim sunucudan gelirken Kaydet kilidinin sebebi
    string MasakRet,
    string BaskaYerdeDegisti,   // 409: kayıt başka yerden değişti — kart yenilendi
    string GeriVerOnay);      // modal onay düğmesi: kısa, 375 px'te tek satır         // alanlar dolu iken sunucu yine reddetti

public static class KaporaGecmisSozlugu
{
    public static readonly KaporaGecmisMetinleri Tr = new(
        Baslik: "Kapora geçmişi",
        Guncel: "Güncel",
        GeriVerildi: "Geri verildi · {0}",
        Gecerli: "Geçerli",
        OdemeNakit: "Nakit",
        OdemeKart: "Kart",
        OdemeHavale: "Havale",
        MasakNot: "Bu tutarda müşterinin kimliği ve işlemin amacı yazılmalı.",
        MasakHazirlaniyor: "Kimlik alanları hazırlanıyor, birkaç saniye bekleyin.",
        MasakRet: "Kimlik bilgileri kabul edilmedi. Alanları kontrol edip yeniden deneyin.",
        BaskaYerdeDegisti: "Bu sipariş başka bir yerden değişti. Güncel hali gösteriliyor.",
        GeriVerOnay: "Geri ver");

    public static readonly KaporaGecmisMetinleri En = new(
        Baslik: "Deposit history",
        Guncel: "Current",
        GeriVerildi: "Refunded · {0}",
        Gecerli: "Valid",
        OdemeNakit: "Cash",
        OdemeKart: "Card",
        OdemeHavale: "Bank transfer",
        MasakNot: "For this amount, the customer's identity and the purpose of the transaction must be entered.",
        MasakHazirlaniyor: "Preparing the identity fields, please wait a few seconds.",
        MasakRet: "The identity details were not accepted. Check the fields and try again.",
        BaskaYerdeDegisti: "This order was changed somewhere else. Showing the latest version.",
        GeriVerOnay: "Refund");

    public static KaporaGecmisMetinleri Al(string dil) => dil == "en" ? En : Tr;

    public static string Odeme(KaporaGecmisMetinleri m, string? kod) => kod switch
    {
        "NAKIT" => m.OdemeNakit,
        "KART" => m.OdemeKart,
        "HAVALE" => m.OdemeHavale,
        _ => "—",
    };
}
