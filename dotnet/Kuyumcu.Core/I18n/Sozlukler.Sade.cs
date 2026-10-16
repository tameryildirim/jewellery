namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SADE-1 — göz incelemesi artıkları (TR + EN).
//   Çift başlık, sonsuz "Yükleniyor", teknik/hukuki metnin tezgâhtar diline
//   inmesi, boş kart, tekrar eden boş durum ve kilit kutuları.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin AYRI dosyaya — Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da
//   durur; Tr ve En aynı kurucudan geçer.
// ⚠ HUB'daki hiçbir metin SİLİNMEDİ: hukuki/teknik tam metin PRO yüzde ya da
//   "Ayrıntı" katlanında yaşamaya devam eder; burası yalnız sade karşılıktır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>SADE-1 metinleri (bir dilde).</summary>
public sealed record SadeMetinleri(
    string YenidenDene,
    // ── /kurulum ──
    string KurulumAdimlarBaslik,
    string KurulumOkunamadi,
    // ── /hurda ──
    string HurdaBakiyeOkunamadi,
    string HurdaFormKilitli,
    string HurdaSubeYok,
    // ── /kur ──
    string KurDenemeRozet,
    string KurKaynakHukukBaslik,
    string KurDenemeIpucu,
    // ── /pos ──
    string PosAciklamaBasit,
    // ── /pos/tahsilatlar ──
    string PosTakvimBos,
    string PosDefterBaslik,
    // ── /banka ──
    string BankaDegismezOzet,
    // ── /ayarlar ──
    string GorunumOkunamadi,
    // ── /sayim ──
    string SayimIptalEdildi,
    // ── /kilavuz ──
    string KilavuzEkranlarOzet);

public static class SadeSozlugu
{
    public static readonly SadeMetinleri Tr = new(
        YenidenDene: "Yeniden dene",
        KurulumAdimlarBaslik: "Adımlar",
        KurulumOkunamadi: "Kurulum bilgisi okunamadı. Bağlantınızı kontrol edip yeniden deneyin.",
        HurdaBakiyeOkunamadi: "Hurda bakiyesi okunamadı — sebebi yukarıdaki uyarıda yazıyor.",
        HurdaFormKilitli: "Kayıt kapalı: ekranın bilgileri okunamadığı için ramat çıkışı kaydedilemez.",
        HurdaSubeYok: "Hesabınıza bir şube atanmamış. İşletme sahibi Kullanıcılar ekranından size şube atayınca bu ekran açılır.",
        KurDenemeRozet: "Deneme kur kaynağı",
        KurKaynakHukukBaslik: "Kur kaynağının kullanım şartı",
        KurDenemeIpucu: "Kur bilgisi deneme amaçlı bir kaynaktan geliyor. Kaynağın kullanım şartını işletme sahibi görür.",
        PosAciklamaBasit: "Kartla alınan para bankadan birkaç gün içinde hesaba geçer; resmî tatillerde gecikebilir.",
        PosTakvimBos: "Bu tarihlerde bankanın yatıracağı bir ödeme yok.",
        PosDefterBaslik: "Kartlı tahsilatlar",
        BankaDegismezOzet: "Kaydedilen banka hareketleri değiştirilemez. Nasıl düzeltileceğini görmek için tıklayın.",
        GorunumOkunamadi: "Görünüm ayarı sunucudan okunamadı; bu yüzden seçim şu an kaydedilemez.",
        SayimIptalEdildi: "İptal edildi",
        KilavuzEkranlarOzet: "Tüm ekranların listesi ({0})");

    public static readonly SadeMetinleri En = new(
        YenidenDene: "Try again",
        KurulumAdimlarBaslik: "Steps",
        KurulumOkunamadi: "Setup status could not be read. Check your connection and try again.",
        HurdaBakiyeOkunamadi: "Scrap balance could not be read — the reason is in the warning above.",
        HurdaFormKilitli: "Saving is off: the screen data could not be read, so a refinery shipment cannot be recorded.",
        HurdaSubeYok: "No branch is assigned to your account. This screen opens once the owner assigns you a branch on the Users screen.",
        KurDenemeRozet: "Trial rate source",
        KurKaynakHukukBaslik: "Rate source terms of use",
        KurDenemeIpucu: "Rates come from a trial source. The shop owner can see the source's terms of use.",
        PosAciklamaBasit: "Card payments reach the account from the bank within a few days; public holidays may delay them.",
        PosTakvimBos: "No bank payout is due in these dates.",
        PosDefterBaslik: "Card collections",
        BankaDegismezOzet: "Saved bank transactions cannot be changed. Click to see how to correct them.",
        GorunumOkunamadi: "The appearance setting could not be read from the server, so a choice cannot be saved right now.",
        SayimIptalEdildi: "Cancelled",
        KilavuzEkranlarOzet: "List of all screens ({0})");

    public static SadeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
