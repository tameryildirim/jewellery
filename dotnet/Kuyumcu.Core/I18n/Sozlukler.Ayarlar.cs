namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-h — AYARLAR SEKMELERİ: TR + EN METİNLER
//
// Kurucu (24.09 08:30): "o sayfa tren gibi aşağıya doğru gidiyor, birbiriyle
// alakasız kısımları tab tab ayır". /ayarlar'daki bölümler dört sekmeye
// ayrıldı; sekme adları Ayarlar.razor'daki GERÇEK bölümlerden çıkarıldı.
// PAKET T-K3 (kurucu 24.09 17:20 "tasarım olarak Sade Yüz Taslağı"): sekmeler
// tuvaldeki ad ve sırayla — Görünüm · Dükkân · Kur · Yazıcı · Güvenlik.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; iki dil aynı
//   kurucudan geçer.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>/ayarlar sekmelerinin metinleri (bir dilde).</summary>
public sealed record AyarlarSekmeMetinleri(
    string SekmeSeridi,
    string Gorunum,
    string Dukkan,
    string Kur,
    string Yazici,
    string Hesap,
    string Guvenlik,
    string Sistem,
    string Destek,
    string DilKoduTr,
    string DilKoduEn);

public static class AyarlarSekmeSozlugu
{
    /// <summary>Sekme kodları — URL'de <c>?sekme=</c> değeri olarak da kullanılır (derin bağlantı).</summary>
    public const string Gorunum = "gorunum";
    public const string Dukkan = "dukkan";
    public const string Kur = "kur";
    public const string Yazici = "yazici";
    public const string Guvenlik = "guvenlik";

    // PAKET T-K3: S1-h'nin sekme kodları; bölümleri artık Güvenlik sekmesindedir.
    // Eski derin bağlantı (?sekme=hesap …) Güvenlik'e yönlenir (Ayarlar.razor).
    public const string Hesap = "hesap";
    public const string Sistem = "sistem";
    public const string Destek = "destek";

    /// <summary>T-K3: Güvenlik sekmesine yönlenen eski sekme kodları.</summary>
    public static readonly IReadOnlySet<string> EskiKodlar = new HashSet<string>(StringComparer.Ordinal) { Hesap, Sistem, Destek };

    public static readonly AyarlarSekmeMetinleri Tr = new(
        SekmeSeridi: "Ayarlar bölümleri",
        Gorunum: "Görünüm",
        Dukkan: "İşletme",
        Kur: "Kur",
        Yazici: "Yazıcı",
        Hesap: "Hesap",
        Guvenlik: "Güvenlik",
        Sistem: "Sistem",
        Destek: "Destek",
        DilKoduTr: "TR",
        DilKoduEn: "EN");

    public static readonly AyarlarSekmeMetinleri En = new(
        SekmeSeridi: "Settings sections",
        Gorunum: "Appearance",
        Dukkan: "Shop",
        Kur: "Rates",
        Yazici: "Printer",
        Hesap: "Account",
        Guvenlik: "Security",
        Sistem: "System",
        Destek: "Support",
        DilKoduTr: "TR",
        DilKoduEn: "EN");

    public static AyarlarSekmeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
