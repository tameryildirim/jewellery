namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — ORTAK IZGARA (Bilesenler/Grid/Izgara.razor) METİNLERİ
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin Sozlukler.<Alan>.cs).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Dil: tezgâhtar dili (jargon yok): "kolon" değil "sütun", "varsayılan" yerine
// "ilk düzen" denmez — kurucunun cümlesi "varsayılana dön"dür, o korunur.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Ortak ızgara metinleri (bir dilde).</summary>
// GRID-1 tur 3 (doğrulayıcı ORTA): panel Pusula sadeliğinde — açıklama paragrafı ve kilitli
// satırların alt yazıları kalktı (kilit sebebi simge + title/aria); 375 px'te kart dikey
// olduğundan taşıma düğmeleri "Yukarı/Aşağı" der; kayıt onayı kısa ("Kaydedildi").
public sealed record IzgaraMetinleri(
    string SutunlarDugmesi,          // araç çubuğu: sütun panelini açar
    string SutunlarBaslik,           // panel başlığı
    string Goster,                   // gizli sütunu göster
    string Gizle,                    // görünen sütunu gizle
    string HepGorunur,               // zorunlu sütunun kilit sebebi (UX-S30; simgenin title/aria'sı)
    string SolaTasi,                 // sütunu bir sola taşı (tablo düzeni)
    string SagaTasi,                 // sütunu bir sağa taşı (tablo düzeni)
    string YukariTasi,               // dar ekran (kart): bir yukarı taşı
    string AsagiTasi,                // dar ekran (kart): bir aşağı taşı
    string EnBasta,                  // öne taşı kilidinin sebebi
    string EnSonda,                  // geri taşı kilidinin sebebi
    string VarsayilanaDon,           // kişisel düzeni siler
    string ZatenVarsayilan,          // "varsayılana dön" kilidinin sebebi
    string Kaydedildi,               // yazma onayı (satır içi, kısa)
    string VarsayilanaDonuldu,       // silme onayı (satır içi, kısa)
    string KaydedilemediOnek,        // yazma hatası bandı öneki ("Sütun düzeni kaydedilemedi: …")
    string GenelYazmaHatasi,         // sunucu mesajı olmayan hata (ağ vb.) — ham istisna metni basılmaz
    string OkunamadiNotu,            // tercih okunamadı → varsayılan düzen çiziliyor
    string SiralaArtan,              // başlık düğmesi erişilebilir adı: "{0} — artan sırala"
    string SiralaAzalan,             // "{0} — azalan sırala"
    string SiralaKaldir,             // "{0} — sıralamayı kaldır"
    string GizliSutunSayisi,         // araç çubuğu: "{0} sütun gizli"
    string KlavyeIpucu);             // başlıkta Alt+←/→ ipucu (title)

public static class IzgaraSozlugu
{
    public static readonly IzgaraMetinleri Tr = new(
        SutunlarDugmesi: "Sütunlar",
        SutunlarBaslik: "Sütunları düzenle",
        Goster: "Göster",
        Gizle: "Gizle",
        HepGorunur: "Her zaman görünür",
        SolaTasi: "Sola taşı",
        SagaTasi: "Sağa taşı",
        YukariTasi: "Yukarı taşı",
        AsagiTasi: "Aşağı taşı",
        EnBasta: "Zaten en başta",
        EnSonda: "Zaten en sonda",
        VarsayilanaDon: "Varsayılana dön",
        ZatenVarsayilan: "Düzen zaten varsayılan",
        Kaydedildi: "Kaydedildi",
        VarsayilanaDonuldu: "Varsayılana dönüldü",
        KaydedilemediOnek: "Sütun düzeni kaydedilemedi",
        GenelYazmaHatasi: "Bağlantı kurulamadı, lütfen tekrar deneyin.",
        OkunamadiNotu: "Kişisel sütun düzeniniz okunamadı; varsayılan düzen gösteriliyor.",
        SiralaArtan: "{0} — küçükten büyüğe sırala",
        SiralaAzalan: "{0} — büyükten küçüğe sırala",
        SiralaKaldir: "{0} — sıralamayı kaldır",
        GizliSutunSayisi: "{0} sütun gizli",
        KlavyeIpucu: "Alt + ← / → ile sütunu taşıyın");

    public static readonly IzgaraMetinleri En = new(
        SutunlarDugmesi: "Columns",
        SutunlarBaslik: "Arrange columns",
        Goster: "Show",
        Gizle: "Hide",
        HepGorunur: "Always visible",
        SolaTasi: "Move left",
        SagaTasi: "Move right",
        YukariTasi: "Move up",
        AsagiTasi: "Move down",
        EnBasta: "Already first",
        EnSonda: "Already last",
        VarsayilanaDon: "Reset to default",
        ZatenVarsayilan: "Layout is already the default",
        Kaydedildi: "Saved",
        VarsayilanaDonuldu: "Reset to default",
        KaydedilemediOnek: "Column layout could not be saved",
        GenelYazmaHatasi: "Could not connect, please try again.",
        OkunamadiNotu: "Your personal column layout could not be read; showing the default layout.",
        SiralaArtan: "{0} — sort ascending",
        SiralaAzalan: "{0} — sort descending",
        SiralaKaldir: "{0} — remove sorting",
        GizliSutunSayisi: "{0} hidden column(s)",
        KlavyeIpucu: "Alt + ← / → moves the column");

    public static IzgaraMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
