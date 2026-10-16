namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-i — GÖRSEL SIĞMA 2: TR + EN METİNLER
//
// Kurucu (24.09): "o sayfa tren gibi aşağıya doğru gidiyor, birbiriyle alakasız
// kısımları tab tab ayır" · "sadece bu ekrana takılma, diğer ekranlara da bak".
// Tren sayfalar (/raporlar/kar-zarar, /yedekler, /kur, /isletme) ortak Sekmeler
// bileşenine bölündü; uzun listeler (/denetim-izi, /raporlar/kasa,
// /raporlar/islem-defteri) sayfalandı ve toplam kayıt sayısını yazar (UX-S29);
// elle yazılmış tablist'ler (SatisAnalizi/Uyum/Gider) ortak bileşene taşındı.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; iki dil aynı
//   kurucudan geçer.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>S1-i sekme adları, sayfalama ve kısa başlık metinleri (bir dilde).</summary>
public sealed record GorselSigmaMetinleri(
    // ── Kâr-zarar ──
    string KzSerit, string KzOzet, string KzGrafik, string KzKopru, string KzUrunler, string KzStok,
    // ── Yedekler ──
    string YdSerit, string YdDurum, string YdElle, string YdKosular, string YdTatbikatlar,
    // ── Kur ──
    string KurSerit, string KurGuncel, string KurPariteler, string KurKaynak, string KurGecmis,
    // ── İşletme ──
    string IsSerit, string IsKimlik, string IsAbonelik, string IsAyarlar, string IsSubeler,
    // ── Elle yazılmış tablist'lerin şerit adları ──
    string GiderSerit, string UyumSerit, string SatisAnaliziSerit,
    // ── Sayfalama (ortak) ──
    string Onceki, string Sonraki,
    /// <summary>{0}–{1} / {2} kayıt</summary>
    string KayitAraligi,
    /// <summary>Toplam {0} kayıt</summary>
    string ToplamKayit,
    // ── Kasa defteri: gün sayfalaması ──
    /// <summary>Son {0} gün gösteriliyor · dönemde {1} gün</summary>
    string KasaGunBilgisi,
    /// <summary>{0} gün daha göster</summary>
    string KasaDahaFazlaGun,
    string KasaTumGunler,
    // ── Denetim izi ──
    string HassasKayit,
    string DegistirilemezIkonEtiketi,
    /// <summary>{0} kayıt daha göster</summary>
    string DahaFazlaKayit);

public static class GorselSigmaSozlugu
{
    // Sekme kodları — URL'de ?sekme= değeri (derin bağlantı). Değiştirilmez.
    public const string KzOzet = "ozet", KzGrafik = "grafik", KzKopru = "kopru", KzUrunler = "urunler", KzStok = "stok";
    public const string YdDurum = "durum", YdElle = "elle", YdKosular = "kosular", YdTatbikatlar = "tatbikatlar";
    public const string KurGuncel = "guncel", KurPariteler = "pariteler", KurKaynak = "kaynak", KurGecmis = "gecmis";
    public const string IsKimlik = "kimlik", IsAbonelik = "abonelik", IsAyarlar = "ayarlar", IsSubeler = "subeler";

    /// <summary>Kasa defterinde bir kasanın ilk bakışta gösterilen gün sayısı.</summary>
    public const int KasaIlkGun = 7;

    /// <summary>Denetim izinin sayfa boyu (sunucu limit/offset ile sayfalanır).
    /// PAKET ZO-BELGE (29.09 devam 6, tasarım hakemi KRİTİK): 7 → 25. Açılış ölçüsü sayfa kısaltılarak
    /// sağlanmaz (günde 50 işlemde 9 sayfa gezdiriyordu); özet ve süzgeç katlanır. Ölçüm kuralı orkestratörde.</summary>
    public const int DenetimSayfaBoyu = 25;

    /// <summary>İşlem defterinin sayfa boyu (sunucu limit/ofset ile sayfalanır).
    /// PAKET ZO-BELGE (29.09 devam 6, tasarım hakemi KRİTİK): 6 → 25. Yazdır artık seçili aralığın
    /// TAMAMINI ayrı isteklerle çeker; ekran sayfası en az 25 satır. Ölçüm kuralı orkestratörde.</summary>
    public const int IslemDefteriSayfaBoyu = 25;

    /// <summary>Kur geçmişinin ilk bakışta gösterilen satır sayısı.</summary>
    public const int KurGecmisIlk = 10;

    public static readonly GorselSigmaMetinleri Tr = new(
        KzSerit: "Kâr-zarar bölümleri", KzOzet: "Özet", KzGrafik: "Grafikler", KzKopru: "Kâr köprüsü",
        KzUrunler: "Ürünler", KzStok: "Stok",
        YdSerit: "Yedek bölümleri", YdDurum: "Durum", YdElle: "Elle yedek", YdKosular: "Koşular",
        YdTatbikatlar: "Tatbikatlar",
        KurSerit: "Kur bölümleri", KurGuncel: "Güncel kur", KurPariteler: "Pariteler", KurKaynak: "Kaynak",
        KurGecmis: "Geçmiş",
        IsSerit: "İşletme bölümleri", IsKimlik: "Kimlik", IsAbonelik: "Abonelik", IsAyarlar: "Ayarlar",
        IsSubeler: "Şubeler",
        GiderSerit: "Gider bölümleri", UyumSerit: "Uyum bölümleri", SatisAnaliziSerit: "Satış analizi raporları",
        Onceki: "Önceki", Sonraki: "Sonraki",
        KayitAraligi: "{0}–{1} / {2} kayıt",
        ToplamKayit: "Toplam {0} kayıt",
        KasaGunBilgisi: "Son {0} gün gösteriliyor · dönemde {1} gün",
        KasaDahaFazlaGun: "{0} gün daha göster",
        KasaTumGunler: "Tüm günler gösteriliyor",
        HassasKayit: "Hassas kayıt",
        DegistirilemezIkonEtiketi: "Değiştirilemez",
        DahaFazlaKayit: "{0} kayıt daha göster");

    public static readonly GorselSigmaMetinleri En = new(
        KzSerit: "Profit and loss sections", KzOzet: "Summary", KzGrafik: "Charts", KzKopru: "Profit bridge",
        KzUrunler: "Products", KzStok: "Stock",
        YdSerit: "Backup sections", YdDurum: "Status", YdElle: "Manual backup", YdKosular: "Runs",
        YdTatbikatlar: "Drills",
        KurSerit: "Rate sections", KurGuncel: "Current rate", KurPariteler: "Parities", KurKaynak: "Source",
        KurGecmis: "History",
        IsSerit: "Business sections", IsKimlik: "Identity", IsAbonelik: "Subscription", IsAyarlar: "Settings",
        IsSubeler: "Branches",
        GiderSerit: "Expense sections", UyumSerit: "Compliance sections", SatisAnaliziSerit: "Sales analysis reports",
        Onceki: "Previous", Sonraki: "Next",
        KayitAraligi: "{0}–{1} / {2} records",
        ToplamKayit: "{0} records in total",
        KasaGunBilgisi: "Showing last {0} days · {1} days in period",
        KasaDahaFazlaGun: "Show {0} more days",
        KasaTumGunler: "All days shown",
        HassasKayit: "Sensitive record",
        DegistirilemezIkonEtiketi: "Immutable",
        DahaFazlaKayit: "Show {0} more records");

    public static GorselSigmaMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
