namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-c — ÜST BAR METİNLERİ (TR + EN): kur şeridi · Yardım · mod anahtarı
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır; aynı anda başka
//   şeritler de ona dokunuyor ve SozlukKur/SozlukArayuz alanları `required`
//   olduğundan tek alan eklemek iki sözlük gövdesini birden düzenlemeyi
//   gerektirirdi. Desen MenuSozlugu (S1-b) / GorunumSozlugu ile aynıdır.
//
// ⚠ UX-S31 — çıplak metin YASAK: üst barın tek bir Türkçe dizgesi bile Razor
//   içinde durmaz ("Son sorgu" / "Last query" eskiden satır içi üçlüydü).
//
// ⚠ Kaynak: kurucunun onayladığı tasarım tuvali
//   dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html <header>:
//   "HAS · GRAM 22 · USD · 09:02 · Yardım · Basit | Profesyonel".
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Üst bar metinleri — dil kodu "tr" | "en".</summary>
public static class UstBarSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Kur şeridi kısa etiketi: has altın (tuval "Has").</summary>
    public static string Has(string dil) => En(dil) ? "Fine" : "Has";

    /// <summary>
    /// Kur şeridi kısa etiketi: 22 ayar gram fiyatı (tuval "Gram 22"). Değer
    /// istemcide HESAPLANMAZ — sunucunun /kur/guncel yanıtındaki
    /// ayarFiyatlari[K22].gramFiyati okunur (UX-S21: sistemin bildiği değer).
    /// </summary>
    public static string Gram22(string dil) => En(dil) ? "Gram 22k" : "Gram 22";

    /// <summary>Kur şeridi kısa etiketi: dolar (tuval "USD").</summary>
    public static string Usd(string dil) => "USD";

    /// <summary>Kur şeridi kısa etiketi: euro (tuval profesyonel yüz "EUR").</summary>
    public static string Eur(string dil) => "EUR";

    /// <summary>Kur şeridi kısa etiketi: sterlin (yalnız ipucunda).</summary>
    public static string Gbp(string dil) => "GBP";

    /// <summary>Son başarılı kur sorgusunun tam zamanı (saatin title'ı).</summary>
    public static string SonSorgu(string dil, string zaman) =>
        En(dil) ? $"Last successful query: {zaman}" : $"Son başarılı sorgu: {zaman}";

    /// <summary>Son sorgu başarısızsa saatin title'ı: ne oldu + ne yap.</summary>
    public static string SonSorguHatali(string dil, string zaman, string sebep) => SonSorguHataliMetin(dil, zaman, sebep.TrimEnd().TrimEnd('.'));

    // PAKET ISTEK-1 (canlı ölçümde görüldü): sebep noktayla bitiyorsa ".." çıkıyordu.
    private static string SonSorguHataliMetin(string dil, string zaman, string sebep) => En(dil)
        ? $"Last query failed ({zaman}): {sebep}. The last known rate is still shown; the screen retries every 10 seconds."
        : $"Son sorgu başarısız ({zaman}): {sebep}. Ekranda son bilinen kur duruyor; ekran 10 saniyede bir yeniden dener.";

    /// <summary>
    /// PAKET ISTEK-1 — sunucu yoğunken (429) üst barın altındaki TEK sade satır.
    /// Jargon yok ("istek sınırı", "429" yazmaz); istemci kendiliğinden bekleyip
    /// yeniden dener, sonraki başarılı yanıtta bant kapanır.
    /// </summary>
    public static string Yogunluk(string dil) => En(dil)
        ? "A little busy right now — this will refresh by itself shortly."
        : "Biraz yoğunluk var, birazdan kendiliğinden yenilenecek.";

    /// <summary>
    /// PAKET ISTEK-1 — sayfa hata bandındaki yoğunluk (429) metni: ne olduğu, ne
    /// kadar kaldığı ve ne yapılacağı (UX-S30); jargon yok ("istek sınırı", "429").
    /// </summary>
    public static string OranSinir(string dil, int kalanSaniye) => En(dil)
        ? $"A little busy right now. Press “Try again” in {kalanSaniye.ToString(System.Globalization.CultureInfo.InvariantCulture)} seconds — nothing was lost."
        : $"Biraz yoğunluk var. {kalanSaniye.ToString(System.Globalization.CultureInfo.InvariantCulture)} saniye sonra “Yeniden dene”ye basın — bilgileriniz silinmedi.";

    /// <summary>Hiç kur alınamadıysa şeritte görünen kısa metin (UX-S28: hata "boş"a çevrilmez).</summary>
    public static string KurYok(string dil) => En(dil) ? "No rate yet" : "Kur alınamadı";

    /// <summary>Kur şeridinin ipucu başlığı — gizli kalan kurlar burada TAM listelenir.</summary>
    public static string KurIpucuBaslik(string dil) => En(dil) ? "Live rates" : "Canlı kurlar";

    /// <summary>Profesyonel yüzde saatin yanında kaynak adı öneki (tuval "14:32 · Altınkaynak").</summary>
    public const string Ayrac = " · ";

    /// <summary>Üst bardaki Yardım bağlantısının metni (tuval "Yardım").</summary>
    public static string Yardim(string dil) => En(dil) ? "Help" : "Yardım";

    /// <summary>Yardım bağlantısının title'ı: nereye gittiğini ve iş kaybı olmadığını söyler.</summary>
    public static string YardimIpucu(string dil) => En(dil)
        ? "Opens the user guide for this screen in a new tab — your work on this screen stays as it is."
        : "Bu ekranın kılavuz bölümünü yeni sekmede açar — bu ekrandaki işiniz olduğu gibi kalır.";
}
