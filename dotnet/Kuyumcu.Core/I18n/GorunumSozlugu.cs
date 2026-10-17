namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-1 — ŞEMA (tema temizliği 17.10: yalnız Yıldız) · 3 TEMA MODU · 3 YAZI BOYUTU: TR + EN AD VE AÇIKLAMA
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı dalgada
//   başka şeritler de ona dokunuyor; ayrıca SozlukGorunum'un alanları `required`
//   olduğu için oraya alan eklemek İKİ sözlük gövdesini birden düzenlemeyi
//   gerektirirdi. Yeni metin bu yüzden buraya, kapalı bir sözlüğe yazıldı
//   (BenzersizlikSozlugu / DogrulamaSozlugu ile aynı desen).
//
// ⚠ UX-S31 — ham kod (platin-mavi, otomatik, 18) EKRANA BASILMAZ; ekran her
//   zaman buradan çevirir. Sözlükte OLMAYAN bir kod için Ad(kod) ham kodu
//   DÖNDÜRMEZ, boş döner — yeni şema eklendiğinde buraya satır eklenmesi
//   AnahtarEksigi() testiyle zorunlu tutulur.
//
// ⚠ Kaynak: dokumanlar/02-mimari/sadelestirme/TEMA-SISTEMI-2026-09-23.md § 4/§ 8.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir görünüm seçeneğinin iki dildeki adı ve tek cümlelik açıklaması.</summary>
/// <param name="AdTr">Türkçe görünen ad.</param>
/// <param name="AciklamaTr">Türkçe tek cümle — ne değişeceğini söyler.</param>
/// <param name="AdEn">İngilizce görünen ad.</param>
/// <param name="AciklamaEn">İngilizce tek cümle.</param>
public readonly record struct GorunumMetni(string AdTr, string AciklamaTr, string AdEn, string AciklamaEn)
{
    public string Ad(string dil) => dil == "en" ? AdEn : AdTr;
    public string Aciklama(string dil) => dil == "en" ? AciklamaEn : AciklamaTr;
}

/// <summary>Renk şeması / tema modu / yazı boyutu kodlarının iki dilli karşılığı.</summary>
public static class GorunumSozlugu
{
    /// <summary>
    /// Renk şeması — anahtar: sunucudaki kanonik slug. Tema temizliği (kurucu 17.10):
    /// tek şema Yıldız; açık/koyu <see cref="TemaModlari"/>'ndadır.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, GorunumMetni> Semalar =
        new Dictionary<string, GorunumMetni>(StringComparer.Ordinal)
        {
            ["yildiz"] = new(
                "Yıldız", "Siyah (ya da kırık beyaz) zemin, kartsız sade sayfa, tek yuvarlak birincil düğme; ince çizgiler ve büyük rakamlar.",
                "Star", "A black (or off-white) ground, card-free pages and a single round primary button; hairline rules and large figures."),
        };

    /// <summary>3 tema modu.</summary>
    public static readonly IReadOnlyDictionary<string, GorunumMetni> TemaModlari =
        new Dictionary<string, GorunumMetni>(StringComparer.Ordinal)
        {
            ["sistem"] = new(
                // S1-f: ad tuvaldeki gibi (Ayarlar.dc.html) — "Sistem" hedef kitleye
                // jargondu (plan § 5B); aynı kavram iki ekranda tek ad (UX-S31).
                "Bilgisayara uy", "Cihazın aydınlık/karanlık tercihi izlenir.",
                "Follow this device", "The device's own light/dark preference is followed."),
            ["aydinlik"] = new(
                "Aydınlık", "Her zaman aydınlık — vitrin ışığı altında en okunaklısı.",
                "Light", "Always light — the most legible under showroom lighting."),
            ["karanlik"] = new(
                "Karanlık", "Her zaman karanlık — akşam vardiyası ve uzun mesai için.",
                "Dark", "Always dark — for the evening shift and long hours."),
        };

    /// <summary>3 yazı boyutu — anahtar: punto kodu ("16" | "18" | "20").</summary>
    public static readonly IReadOnlyDictionary<string, GorunumMetni> YaziBoyutlari =
        new Dictionary<string, GorunumMetni>(StringComparer.Ordinal)
        {
            ["16"] = new(
                // S1-f: adlar tuvaldeki gibi (Normal · Büyük · Çok büyük); piksel
                // değeri ekranda ayrıca yazılır, "punto" jargonu kalktı.
                "Normal", "Standart gövde — tek ekrana en çok satır sığar.",
                "Normal", "The standard body size — the most rows per screen."),
            ["18"] = new(
                "Büyük", "Gövde 18 punto; uzaktan ve küçük ekranda daha rahat okunur.",
                "Large", "An 18pt body; easier to read from a distance and on small screens."),
            ["20"] = new(
                "Çok büyük", "En büyük gövde — tezgâh üstü ekran ve zayıf görüş için.",
                "Extra large", "The largest body — for counter-top screens and weaker eyesight."),
        };

    /// <summary>Kaynak rozeti metni (UX-S21): değer nereden geldi?</summary>
    public static readonly IReadOnlyDictionary<string, GorunumMetni> Kaynaklar =
        new Dictionary<string, GorunumMetni>(StringComparer.Ordinal)
        {
            ["kullanici"] = new(
                "Sizin seçiminiz", "Bu değeri siz seçtiniz; işletme varsayılanını geçersiz kılar.",
                "Your choice", "You chose this value; it overrides the shop default."),
            ["kiraci"] = new(
                "İşletme varsayılanı", "Bu değer işletme varsayılanından geliyor; kendi seçiminizle değiştirebilirsiniz.",
                "Shop default", "This value comes from the shop default; you can override it with your own choice."),
            ["sistem"] = new(
                "Sistem varsayılanı", "Ne siz ne işletme bir seçim yaptı; ürünün varsayılanı geçerli.",
                "System default", "Neither you nor the shop has chosen; the product default applies."),
        };

    /// <summary>Sözlükte olmayan kodun adı BOŞ döner — ham kod ekrana SIZMAZ (UX-S31).</summary>
    public static string Ad(IReadOnlyDictionary<string, GorunumMetni> sozluk, string? kod, string dil) =>
        kod is not null && sozluk.TryGetValue(kod, out var m) ? m.Ad(dil) : "";

    /// <inheritdoc cref="Ad"/>
    public static string Aciklama(IReadOnlyDictionary<string, GorunumMetni> sozluk, string? kod, string dil) =>
        kod is not null && sozluk.TryGetValue(kod, out var m) ? m.Aciklama(dil) : "";

    /// <summary>
    /// Verilen kod listesinde sözlükte KARŞILIĞI OLMAYANLARI döndürür.
    /// Test bunu boş bekler: yeni bir şema açılıp çevirisi unutulursa KIRMIZI olur.
    /// </summary>
    public static IReadOnlyList<string> AnahtarEksigi(
        IReadOnlyDictionary<string, GorunumMetni> sozluk, IEnumerable<string> kodlar) =>
        [.. kodlar.Where(k => !sozluk.ContainsKey(k))];
}
