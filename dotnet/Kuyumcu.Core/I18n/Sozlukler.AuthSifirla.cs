namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET AUTH-SIFIRLA — "Parolamı unuttum" ekran metinleri (Pages/SifreSifirla.razor
// + Giris.razor'daki tek bağlantı).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (P8); yeni metin ayrı dosyaya.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ⚠ Jargon yok (hedef kitle tezgâhtar): "jeton", "token", "SMTP" ekrana çıkmaz.
// ⚠ Madde (1): istek sonucu metni hesap VAR/YOK ayrımı yapmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>AUTH-SIFIRLA metinleri (bir dilde).</summary>
public sealed record AuthSifirlaMetinleri(
    // Giriş ekranındaki bağlantı
    string GiristekiBaglanti,
    // İstek adımı
    string IstekBaslik,
    string IstekAciklama,
    string BaglantiGonder,
    string IstekBasari,
    string DurumYukleniyor,
    string DurumHatasi,
    // Kapalı (fail-closed) durum
    string KapaliBaslik,
    string KapaliMesaj,
    // Yeni parola adımı
    string YeniBaslik,
    string YeniAciklama,
    string YeniParola,
    string YeniParolaTekrar,
    string ParolaKurali,
    string ParolalarAyniDegil,
    string ParolayiKaydet,
    string TamamBasari,
    string YeniBaglantiIste,
    // Tur 2 (tasarım hakemi): geçersiz bağlantı · deneme sınırı · kanal kapalıyken giriş yardımı
    string BaglantiGecersiz,
    string HizSiniriKalip,
    string GirisKapaliYardim,
    // Ortak
    string GiriseDon,
    string GonderiliyorKilit,
    string Goster,
    string Gizle)
{
    /// <summary>Deneme sınırı metni; süre Retry-After başlığından DAKİKAYA çevrilmiş gelir.</summary>
    public string HizSiniri(int dakika) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, HizSiniriKalip, Math.Max(1, dakika));
}

public static class AuthSifirlaSozlugu
{
    public static readonly AuthSifirlaMetinleri Tr = new(
        GiristekiBaglanti: "E-postayla yeni parola al",
        IstekBaslik: "Parolanı yenile",
        IstekAciklama: "Kayıtlı e-posta adresini yaz. Sana yeni parola belirleme bağlantısı gönderelim.",
        BaglantiGonder: "Bağlantı gönder",
        IstekBasari: "Bu e-posta adresi kayıtlıysa bağlantı gönderildi. Bağlantı 30 dakika geçerli; "
            + "gelen kutunu ve istenmeyen klasörünü kontrol et.",
        DurumYukleniyor: "Kontrol ediliyor…",
        DurumHatasi: "Sıfırlama bilgisi okunamadı. Sayfayı yenileyip yeniden dene.",
        KapaliBaslik: "E-postayla sıfırlama kapalı",
        KapaliMesaj: "Bu işletmede e-postayla parola sıfırlama açık değil. Şifre sıfırlama için işletme "
            + "sahibine ya da destek hattına başvur.",
        YeniBaslik: "Yeni parola belirle",
        YeniAciklama: "Yeni parolanı iki kez yaz.",
        YeniParola: "Yeni parola",
        YeniParolaTekrar: "Yeni parola (tekrar)",
        ParolaKurali: "En az 8 karakter; içinde en az bir harf ve bir rakam olsun.",
        ParolalarAyniDegil: "İki parola aynı değil.",
        ParolayiKaydet: "Parolayı kaydet",
        TamamBasari: "Parolan yenilendi. Yeni parolanla giriş yapabilirsin.",
        YeniBaglantiIste: "Yeni bağlantı iste",
        BaglantiGecersiz: "Bu bağlantı artık geçerli değil. Aşağıdan yeni bağlantı iste.",
        HizSiniriKalip: "Çok fazla deneme yaptın. {0} dakika sonra yeniden dene.",
        GirisKapaliYardim: "Parolanı işletme sahibin ya da müdürün Kullanıcılar ekranındaki “Şifre Sıfırla” ile yeniler; "
            + "işletme sahibiysen destek hattını ara.",
        GiriseDon: "Girişe dön",
        GonderiliyorKilit: "Gönderiliyor…",
        Goster: "Göster",
        Gizle: "Gizle");

    public static readonly AuthSifirlaMetinleri En = new(
        GiristekiBaglanti: "Get a new password by e-mail",
        IstekBaslik: "Reset your password",
        IstekAciklama: "Enter your registered e-mail address. We will send you a link to set a new password.",
        BaglantiGonder: "Send link",
        IstekBasari: "If this e-mail address is registered, a link has been sent. The link is valid for "
            + "30 minutes; check your inbox and spam folder.",
        DurumYukleniyor: "Checking…",
        DurumHatasi: "Reset information could not be read. Refresh the page and try again.",
        KapaliBaslik: "Reset by e-mail is off",
        KapaliMesaj: "Password reset by e-mail is not enabled for this shop. To reset your password, "
            + "contact the shop owner or the support line.",
        YeniBaslik: "Set a new password",
        YeniAciklama: "Type your new password twice.",
        YeniParola: "New password",
        YeniParolaTekrar: "New password (again)",
        ParolaKurali: "At least 8 characters, with at least one letter and one digit.",
        ParolalarAyniDegil: "The two passwords do not match.",
        ParolayiKaydet: "Save password",
        TamamBasari: "Your password has been reset. You can now sign in with your new password.",
        YeniBaglantiIste: "Request a new link",
        BaglantiGecersiz: "This link is no longer valid. Request a new link below.",
        HizSiniriKalip: "Too many attempts. Try again in {0} minutes.",
        GirisKapaliYardim: "Your shop owner or manager can reset your password under Users → Reset password; "
            + "if you are the shop owner, call the support line.",
        GiriseDon: "Back to sign in",
        GonderiliyorKilit: "Sending…",
        Goster: "Show",
        Gizle: "Hide");

    public static AuthSifirlaMetinleri Al(string? dil) =>
        string.Equals(dil, "en", StringComparison.OrdinalIgnoreCase) ? En : Tr;
}
