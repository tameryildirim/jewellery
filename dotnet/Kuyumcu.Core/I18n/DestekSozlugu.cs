// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5 · T-4b) — DESTEK PAKETİ + KISA HATA KODU METİNLERİ (TR + EN).
//
// NEDEN AYRI DOSYA: Sozlukler.cs ortak hub'dır ve aynı anda başka şeritler
// tarafından düzenlenir (PozisyonSozlugu / HavaSozlugu ile aynı desen).
// Konu kodları (KURULUM, HATA …) ekrana HAM BASILMAZ (UX-S31): KonuAdi ile
// çevrilir; bilinmeyen kod derleme değil çalışma zamanında "Diğer"e düşer ve
// DestekSozluguTestleri her kodun iki dilde karşılığı olduğunu kilitler.
// ═══════════════════════════════════════════════════════════════════════════
namespace Kuyumcu.Core.I18n;

public sealed record DestekMetinleri(
    string Baslik,
    string Aciklama,
    string IcerikBaslik,
    IReadOnlyList<string> IcerikVar,
    string IcermezBaslik,
    IReadOnlyList<string> IcerikYok,
    string KonuEtiket,
    string GerekceEtiket,
    string HataKoduEtiket,
    string HataKoduGecersiz,
    string Olustur,
    string KilitGerekce,
    string KilitKanal,
    string KilitCalisiyor,
    string OnayMetin,
    string Onayla,
    string Vazgec,
    string Basari,
    string GecmisBaslik,
    string GecmisBos,
    string GecmisSinir,
    string DahaFazlasiVar,
    string SutunTarih,
    string SutunPaketKodu,
    string SutunKonu,
    string DegismezVarlik,
    string DegismezGerekce,
    string DegismezYol,
    string Yuklenemedi,
    string EkranHatasi,
    string KodAliniyor,
    string KodAlinamadi,
    IReadOnlyDictionary<string, string> Konular);

public static class DestekSozlugu
{
    public static readonly DestekMetinleri Tr = new(
        Baslik: "Destek paketi",
        Aciklama: "Bir sorun yaşadığınızda destek ekibine bu dosyayı gönderin. Dosyada kişisel veri ve para bilgisi bulunmaz; yalnız sürüm, şema ve işlem özeti vardır.",
        IcerikBaslik: "Pakette neler var",
        IcerikVar:
        [
            "Sürüm, derleme zamanı ve şema adımları",
            "Son 24 saatteki istek ve sunucu hatası sayısı",
            "Son 200 işlemin yalnız tarihi, türü ve kısaltılmış kullanıcı kodu",
            "Seçtiğiniz konu ve varsa hata ekranındaki destek kodu",
        ],
        IcermezBaslik: "Pakette neler yok",
        IcerikYok:
        [
            "Müşteri adı, TC kimlik no, telefon, adres, e-posta",
            "Tutar, bakiye, has gram, fiyat",
            "Kullanıcı adı, parola, oturum bilgisi",
            "Yazdığınız açıklama (yalnız sizin işlem kaydınızda kalır)",
        ],
        KonuEtiket: "Konu",
        GerekceEtiket: "Ne oldu?",
        HataKoduEtiket: "Hata ekranındaki destek kodu (isteğe bağlı)",
        HataKoduGecersiz: "Destek kodu 8 karakter olmalı ve yalnız harf ile rakam içermeli.",
        Olustur: "Destek paketi oluştur",
        KilitGerekce: "Açıklama en az {0} karakter olmalı ({1}/{0}).",
        KilitKanal: "Destek paketi, güvenlik doğrulaması tamamlanana dek kapalıdır. Hata ekranındaki destek kodunu iletmeniz yeterlidir.",
        KilitCalisiyor: "Paket hazırlanıyor…",
        OnayMetin: "Paket oluşturulunca geri alınamaz ve işlem kaydınıza yazılır. Pakette kişisel veri bulunmaz. Devam edilsin mi?",
        Onayla: "Oluştur ve indir",
        Vazgec: "Vazgeç",
        Basari: "Destek paketi indirildi: {0}. Bu dosyayı destek ekibine iletin.",
        GecmisBaslik: "Oluşturulan paketler",
        GecmisBos: "Henüz destek paketi oluşturulmadı.",
        GecmisSinir: "Son {0} paket gösterilir.",
        DahaFazlasiVar: "Daha eski paketler var; tamamı işlem kaydında (denetim izi) görülür.",
        SutunTarih: "Tarih",
        SutunPaketKodu: "Paket kodu",
        SutunKonu: "Konu",
        DegismezVarlik: "Destek paketi kaydı",
        DegismezGerekce: "Paket kaydı işlem kaydının (denetim izi) parçasıdır; silinemez ve düzenlenemez.",
        DegismezYol: "Yanlış paket gönderdiyseniz yeni bir paket oluşturun.",
        Yuklenemedi: "Destek paketi geçmişi okunamadı.",
        EkranHatasi: "Bu ekranda beklenmeyen bir sorun oluştu. Aşağıdaki destek kodunu iletirseniz sorunu bulabiliriz.",
        KodAliniyor: "Destek kodu alınıyor…",
        KodAlinamadi: "Destek kodu alınamadı. Sayfayı yenileyip yeniden deneyin.",
        Konular: new Dictionary<string, string>
        {
            ["KURULUM"] = "Kurulum",
            ["EGITIM"] = "Kullanım / eğitim",
            ["HATA"] = "Hata",
            ["VERI"] = "Veri",
            ["DIGER"] = "Diğer",
        });

    public static readonly DestekMetinleri En = new(
        Baslik: "Support package",
        Aciklama: "When you run into a problem, send this file to the support team. It contains no personal or monetary data; only version, schema and an activity summary.",
        IcerikBaslik: "What the package contains",
        IcerikVar:
        [
            "Version, build time and schema steps",
            "Request and server error counts for the last 24 hours",
            "Date, type and shortened user code of the last 200 actions",
            "The topic you chose and the support code from the error screen, if any",
        ],
        IcermezBaslik: "What the package does not contain",
        IcerikYok:
        [
            "Customer name, national ID, phone, address, e-mail",
            "Amounts, balances, fine gold grams, prices",
            "User name, password, session data",
            "Your description (it stays only in your own activity log)",
        ],
        KonuEtiket: "Topic",
        GerekceEtiket: "What happened?",
        HataKoduEtiket: "Support code from the error screen (optional)",
        HataKoduGecersiz: "The support code must be 8 characters, letters and digits only.",
        Olustur: "Create support package",
        KilitGerekce: "The description must be at least {0} characters ({1}/{0}).",
        KilitKanal: "The support package is closed until the security review is complete. Sending the support code from the error screen is enough.",
        KilitCalisiyor: "Preparing package…",
        OnayMetin: "Once created, the package cannot be undone and is written to your activity log. It contains no personal data. Continue?",
        Onayla: "Create and download",
        Vazgec: "Cancel",
        Basari: "Support package downloaded: {0}. Send this file to the support team.",
        GecmisBaslik: "Created packages",
        GecmisBos: "No support package has been created yet.",
        GecmisSinir: "The last {0} packages are shown.",
        DahaFazlasiVar: "Older packages exist; all of them are visible in the activity log (audit trail).",
        SutunTarih: "Date",
        SutunPaketKodu: "Package code",
        SutunKonu: "Topic",
        DegismezVarlik: "Support package record",
        DegismezGerekce: "The package record is part of the activity log (audit trail); it cannot be deleted or edited.",
        DegismezYol: "If you sent the wrong package, create a new one.",
        Yuklenemedi: "Support package history could not be read.",
        EkranHatasi: "This screen hit an unexpected problem. Sending the support code below helps us find it.",
        KodAliniyor: "Getting support code…",
        KodAlinamadi: "The support code could not be obtained. Reload the page and try again.",
        Konular: new Dictionary<string, string>
        {
            ["KURULUM"] = "Setup",
            ["EGITIM"] = "Usage / training",
            ["HATA"] = "Error",
            ["VERI"] = "Data",
            ["DIGER"] = "Other",
        });

    public static DestekMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Konu kodunu insan diline çevirir; bilinmeyen kod "Diğer" olur (ham basılmaz).</summary>
    public static string KonuAdi(string? kod, string dil)
    {
        var m = Al(dil);
        return kod is not null && m.Konular.TryGetValue(kod, out var ad) ? ad : m.Konular["DIGER"];
    }
}
