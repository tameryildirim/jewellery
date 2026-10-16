namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 — satış dışı para formlarında MASAK kapısı (tasarım hakemi KRİTİK, 10.10).
// /cariler Tahsilat çekmecesinde 250.000 ₺ nakit → sunucu MASAK 400 ("masak.aciklama 20 karakter …"),
// çekmecede kimlik/amaç alanı YOKTU (tezgâhtar çıkışsız); tutar 25.000'e inince eski hata kutusu duruyordu.
// Metinler sade, tek cümle; eşik SAYISI burada yoktur (MasakFormu başlığı sunucudan gelen eşiği bir kez söyler).
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>YASAL-KAPI-2 metinleri (bir dilde).</summary>
public sealed record YasalKapi2Metinleri(
    // Kimlik formu sunucudan gereksinim beklerken Kaydet kilidinin sebebi
    string MasakHazirlaniyor,
    // Gereksinim sorusu başarısız (fail-closed): kilit sebebi
    string MasakSorguHata,
    // Eşik bilgisi alınamadı → form açılamıyor; ne yapılacağı söylenir (çıkışsız kalmaz)
    string MasakEsikYok,
    // Form eksiksizken sunucu yine reddetti: {0} = sunucunun sadeleştirilmiş cümlesi
    string MasakKabulEdilmedi,
    // Fark sıfır ama Kaydet başka sebeple kilitli: "kaydedebilirsiniz" DENMEZ
    string FarkYok,
    // Veresiye penceresinin kilit sebebi (tutar yok)
    string VeresiyeTutarGirin,
    // Tur 2 (doğrulayıcı KRİTİK): PRO /islemler Kaydet kilidi — eşik SAYISI yok; kademe "Yasal kapılar" panelinde (tek yer)
    string MasakKilitProKimlik,
    string MasakKilitProAmac);

/// <summary>YASAL-KAPI-2 sözlüğü (TR + EN).</summary>
public static class YasalKapi2Sozlugu
{
    public static readonly YasalKapi2Metinleri Tr = new(
        MasakHazirlaniyor: "Kimlik formu hazırlanıyor…",
        MasakSorguHata: "Kimlik gereksinimi alınamadı; kayıt şimdilik kilitli. Tutarı yeniden yazın ya da sayfayı yenileyin.",
        MasakEsikYok: "Bu tutarda kimlik gerekiyor ama kimlik formu açılamadı. Sayfayı yenileyip yeniden deneyin.",
        MasakKabulEdilmedi: "Kimlik bilgileri kabul edilmedi: {0}",
        FarkYok: "Fark yok.",
        VeresiyeTutarGirin: "Kaydetmek için tutar girin.",
        MasakKilitProKimlik: "Kaydetmek için kimlik bilgilerini doldurun; kademe Yasal kapılar panelinde.",
        MasakKilitProAmac: "Kaydetmek için kimlik ve işlem amacını doldurun; kademe Yasal kapılar panelinde.");

    public static readonly YasalKapi2Metinleri En = new(
        MasakHazirlaniyor: "Preparing the identity form…",
        MasakSorguHata: "Identity requirement could not be loaded; saving is locked for now. Re-enter the amount or reload the page.",
        MasakEsikYok: "Identity is required for this amount but the identity form could not be opened. Reload the page and try again.",
        MasakKabulEdilmedi: "Identity details were not accepted: {0}",
        FarkYok: "No difference.",
        VeresiyeTutarGirin: "Enter an amount to save.",
        MasakKilitProKimlik: "Fill in the identity details to save; the tier is shown in the Legal checks panel.",
        MasakKilitProAmac: "Fill in the identity and the purpose of the transaction to save; the tier is shown in the Legal checks panel.");

    public static YasalKapi2Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
