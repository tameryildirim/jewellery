namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET KUR-BANT (09.10) — makul bant aşımının "fiyat doğru, yine de kaydet"
// ikinci onay yolunun metinleri (TR + EN). Bant iletisinin KENDİSİ GirdiSozlugu'ndadır
// (FiyatMutlakDisi / FiyatBantDisi) — ekran alan altı ve sunucu 400 aynı cümle.
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Dil sade (hedef kitle: tezgâhtar) — jargon yok.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record KurBantMetinleri(
    string OnayEtiket,          // onay kutusu: fiyat doğru
    string GerekceEtiket,       // gerekçe kutusu etiketi
    string GerekceIpucu,        // gerekçe kutusu ipucu
    string YetkiYok,            // tezgâhtar: onay yalnız sahip/müdür (ekran + 403)
    string OnayGerekli,         // bant dışı, onay kutusu işaretlenmedi (ekran bandı)
    string OnayliKaydedildi);   // başarı: onaylı aşım kaydedildi (denetime yazıldı)

public static class KurBantSozlugu
{
    public static readonly KurBantMetinleri Tr = new(
        OnayEtiket: "Fiyat doğru, yine de kaydet",
        GerekceEtiket: "Neden bu fiyat?",
        GerekceIpucu: "Örn: Piyasa bugün sert düştü, fiyatı kuyumcular odasından teyit ettim.",
        YetkiYok: "Fiyat doğruysa bunu yalnız işletme sahibi ya da müdür onaylayabilir.",
        OnayGerekli: "Fiyat olağandışı. Doğruysa kutuyu işaretleyip nedenini yazın.",
        OnayliKaydedildi: "Fiyat kaydedildi. Olağandışı fiyat onayınız işlem kaydına yazıldı.");

    public static readonly KurBantMetinleri En = new(
        OnayEtiket: "The price is correct, save anyway",
        GerekceEtiket: "Why this price?",
        GerekceIpucu: "E.g.: The market dropped sharply today; I confirmed the price with the jewellers' chamber.",
        YetkiYok: "If the price is correct, only the shop owner or a manager can approve it.",
        OnayGerekli: "The price is unusual. If it is correct, tick the box and write the reason.",
        OnayliKaydedildi: "Price saved. Your approval of the unusual price was written to the activity log.");

    public static KurBantMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
