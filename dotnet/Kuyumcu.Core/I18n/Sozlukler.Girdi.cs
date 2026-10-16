using System.Globalization;
using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — girdi kuralı metinleri (TR + EN). Kural Core
// Domain/Girdi'dedir ve yalnız KOD döndürür; ekran (alan altı + Kaydet kilidi) ve
// sunucu (400 mesajı) metni BURADAN alır → aynı anahtar, aynı cümle.
// ⚠ Konumsal kayıt: eksik dil = derleme hatası. Her GirdiKodu'nun metni olduğu
//   GirdiKuralTestleri.Sozluk_her_kod_iki_dilde ile sınanır.
// Dil sade (hedef kitle: tezgâhtar) — jargon yok.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Girdi kuralı metinleri (bir dilde). {0}/{1} = hatanın sayı değerleri, {2} = tarih.</summary>
public sealed record GirdiMetinleri(
    string IbanBicim, string IbanUzunluk, string IbanKontrol,
    string TelefonKarakter, string TelefonHane, string TelefonAralik, string TelefonYabanci,
    string EpostaBicim, string EpostaUzun,
    string TarihBicim, string TarihGecersiz, string TarihErken, string TarihGec, string TarihIleri, string TarihCokEski,
    string DogumGelecek, string DogumResit, string DogumCokEski,
    string AralikTers, string AralikUzun,
    string SayiEksi, string SayiSifir, string SayiOndalik, string SayiTavan, string SayiEnAz, string SayiEnCok, string SayiTam,
    string TasBrut, string MilyemAralik, string YuzdeAralik, string GunAralik,
    string BarkodKarakter, string BarkodUzunluk, string BarkodBosluk, string Ean13Kontrol,
    string AdKisa, string AdHarf, string AdUzun,
    string AdresKisa, string AdresHarf, string AdresUzun,
    string PasaportBicim,
    string GerekceKisa, string GerekceUzun,
    string AyarBilinmiyor,
    string KimlikRakam, string KimlikHane, string TcknGecersiz,
    string IbanEksik, string KimlikHaneIkili, string IbanYabanciBicim,
    string FiyatMutlakDisi, string FiyatBantDisi,
    string IslemFiyatBantDisi,
    // ekran yardımcıları
    string TarihYerTutucu, string TarihTakvimAc, string IbanYerTutucu, string IbanIpucu);

/// <summary>Girdi kuralı sözlüğü.</summary>
public static class GirdiSozlugu
{
    public static readonly GirdiMetinleri Tr = new(
        IbanBicim: "IBAN TR ile başlar; banka kodu yalnız rakamdır.",
        IbanUzunluk: "IBAN {1} karakter olmalı (şu an {0}).",
        IbanKontrol: "IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin.",
        TelefonKarakter: "Telefona yalnız rakam yazılır.",
        TelefonHane: "Telefon alan koduyla {1} haneli olmalı (şu an {0}).",
        TelefonAralik: "Bu numara Türkiye'de kullanılan bir telefon numarası değil.",
        TelefonYabanci: "Yurt dışı numara + ve ülke koduyla en çok {0} hane olmalı.",
        EpostaBicim: "Geçerli bir e-posta yazın (ör. ad@isletme.com).",
        EpostaUzun: "E-posta en çok {0} karakter olabilir.",
        TarihBicim: "Tarihi gün.ay.yıl olarak yazın (ör. 08.10.2026).",
        TarihGecersiz: "Takvimde böyle bir gün yok.",
        TarihErken: "Tarih {2} tarihinden önce olamaz.",
        TarihGec: "Tarih {2} tarihinden sonra olamaz.",
        TarihIleri: "Tarih bugünden ileri olamaz.",
        TarihCokEski: "{2} tarihinden eski bir tarih yazılamaz.",
        DogumGelecek: "Doğum tarihi bugünden sonra olamaz.",
        DogumResit: "Kişi en az {0} yaşında olmalı.",
        DogumCokEski: "Doğum tarihi {0} yıldan eski olamaz.",
        AralikTers: "Başlangıç tarihi bitiş tarihinden sonra olamaz.",
        AralikUzun: "Tarih aralığı en çok {0} yıl olabilir.",
        SayiEksi: "Eksi değer yazılamaz.",
        SayiSifir: "Değer 0'dan büyük olmalı.",
        SayiOndalik: "Virgülden sonra en çok {0} hane yazılır.",
        SayiTavan: "Değer çok büyük (en çok {0}).",
        SayiEnAz: "En az {0} olmalı.",
        SayiEnCok: "En çok {0} olabilir.",
        SayiTam: "Küsuratlı yazılamaz; tam sayı girin.",
        TasBrut: "Taş ağırlığı brüt gramdan küçük olmalı.",
        MilyemAralik: "Milyem {0} ile {1} arasında olmalı.",
        YuzdeAralik: "Oran %{0} ile %{1} arasında olmalı.",
        GunAralik: "Gün sayısı {0} ile {1} arasında olmalı.",
        BarkodKarakter: "Barkoda Türkçe harf ve özel işaret yazılamaz.",
        BarkodUzunluk: "Barkod {0}–{1} karakter olmalı.",
        BarkodBosluk: "Barkod boşlukla başlayamaz.",
        Ean13Kontrol: "13 haneli barkodun son hanesi tutmuyor; etiketi kontrol edin.",
        AdKisa: "En az {0} karakter yazın.",
        AdHarf: "En az bir harf olmalı.",
        AdUzun: "En çok {0} karakter yazılır.",
        AdresKisa: "Adres en az {0} karakter olmalı (mahalle, sokak, no).",
        AdresHarf: "Adreste en az bir harf olmalı.",
        AdresUzun: "Adres en çok {0} karakter olabilir.",
        PasaportBicim: "Pasaport no {0}–{1} harf ya da rakam olmalı, boşluksuz.",
        GerekceKisa: "Gerekçe en az {0} karakter olmalı (şu an {1}).",
        GerekceUzun: "Gerekçe en çok {0} karakter olabilir (şu an {1}).",
        AyarBilinmiyor: "Listede olmayan ayar seçilemez.",
        KimlikRakam: "Kimlik numarasına yalnız rakam yazılır.",
        KimlikHane: "Numara {0} haneli olmalı.",
        TcknGecersiz: KimlikNoSozlugu.Tr.TcknGecersiz,
        IbanEksik: "IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an {0} karakter).",
        KimlikHaneIkili: "Numara 10 (vergi no) ya da 11 (TC kimlik no) haneli olmalı (şu an {0}).",
        IbanYabanciBicim: "Yabancı IBAN ülke kodu, 2 kontrol hanesi ve en az 10 harf ya da rakamdan oluşur (şu an {0} karakter).",
        FiyatMutlakDisi: "Bu fiyat olağan aralığın dışında ({0}–{1} TL). Rakamı yanlış yazmış olabilirsiniz; kontrol edin.",
        FiyatBantDisi: "Bu fiyat son geçerli fiyattan ({0} TL) yüzde {1}'den fazla farklı. Rakamı yanlış yazmış olabilirsiniz; kontrol edin.",
        IslemFiyatBantDisi: "Bu has fiyatı güncel has fiyatına göre olağan aralığın dışında ({0}–{1} TL). Rakamı yanlış yazmış olabilirsiniz; kontrol edin.",
        TarihYerTutucu: "gg.aa.yyyy",
        TarihTakvimAc: "Takvimi aç",
        IbanYerTutucu: "TR00 0000 0000 0000 0000 0000 00",
        IbanIpucu: "26 karakter; 4'lü gruplar kendiliğinden ayrılır.");

    public static readonly GirdiMetinleri En = new(
        IbanBicim: "IBAN starts with TR; the bank code is digits only.",
        IbanUzunluk: "IBAN must be {1} characters (now {0}).",
        IbanKontrol: "IBAN check digits do not match. Please check the number.",
        TelefonKarakter: "Only digits can be typed in a phone number.",
        TelefonHane: "Phone number must be {1} digits with area code (now {0}).",
        TelefonAralik: "This is not a phone number used in Türkiye.",
        TelefonYabanci: "A foreign number must have + and country code, at most {0} digits.",
        EpostaBicim: "Enter a valid e-mail (e.g. name@business.com).",
        EpostaUzun: "E-mail can be at most {0} characters.",
        TarihBicim: "Type the date as day.month.year (e.g. 08.10.2026).",
        TarihGecersiz: "This day does not exist in the calendar.",
        TarihErken: "Date cannot be before {2}.",
        TarihGec: "Date cannot be after {2}.",
        TarihIleri: "Date cannot be later than today.",
        TarihCokEski: "A date older than {2} cannot be entered.",
        DogumGelecek: "Date of birth cannot be in the future.",
        DogumResit: "The person must be at least {0} years old.",
        DogumCokEski: "Date of birth cannot be more than {0} years ago.",
        AralikTers: "Start date cannot be after end date.",
        AralikUzun: "Date range can be at most {0} years.",
        SayiEksi: "Negative values cannot be entered.",
        SayiSifir: "Value must be greater than 0.",
        SayiOndalik: "At most {0} digits after the decimal point.",
        SayiTavan: "Value is too large (at most {0}).",
        SayiEnAz: "Must be at least {0}.",
        SayiEnCok: "Can be at most {0}.",
        SayiTam: "Fractions are not allowed; enter a whole number.",
        TasBrut: "Stone weight must be less than gross gram.",
        MilyemAralik: "Fineness must be between {0} and {1}.",
        YuzdeAralik: "Rate must be between {0}% and {1}%.",
        GunAralik: "Number of days must be between {0} and {1}.",
        BarkodKarakter: "Barcode cannot contain Turkish letters or special symbols.",
        BarkodUzunluk: "Barcode must be {0}–{1} characters.",
        BarkodBosluk: "Barcode cannot start with a space.",
        Ean13Kontrol: "The last digit of this 13-digit barcode does not match; check the label.",
        AdKisa: "Type at least {0} characters.",
        AdHarf: "Must contain at least one letter.",
        AdUzun: "At most {0} characters.",
        AdresKisa: "Address must be at least {0} characters (district, street, no).",
        AdresHarf: "Address must contain at least one letter.",
        AdresUzun: "Address can be at most {0} characters.",
        PasaportBicim: "Passport no must be {0}–{1} letters or digits, without spaces.",
        GerekceKisa: "Reason must be at least {0} characters (now {1}).",
        GerekceUzun: "Reason can be at most {0} characters (now {1}).",
        AyarBilinmiyor: "A purity that is not in the list cannot be selected.",
        KimlikRakam: "Only digits can be typed in an ID number.",
        KimlikHane: "The number must be {0} digits.",
        TcknGecersiz: KimlikNoSozlugu.En.TcknGecersiz,
        IbanEksik: "IBAN is not complete yet or its check digits do not match (now {0} characters).",
        KimlikHaneIkili: "The number must be 10 (tax no) or 11 (ID no) digits (now {0}).",
        IbanYabanciBicim: "A foreign IBAN has a country code, 2 check digits and at least 10 letters or digits (now {0} characters).",
        FiyatMutlakDisi: "This price is outside the usual range ({0}–{1} TL). You may have mistyped it; please check.",
        FiyatBantDisi: "This price differs from the last valid price ({0} TL) by more than {1} percent. You may have mistyped it; please check.",
        IslemFiyatBantDisi: "This fine-gold price is outside the usual range for the current gold price ({0}–{1} TL). You may have mistyped it; please check.",
        TarihYerTutucu: "dd.mm.yyyy",
        TarihTakvimAc: "Open calendar",
        IbanYerTutucu: "TR00 0000 0000 0000 0000 0000 00",
        IbanIpucu: "26 characters; groups of 4 are separated automatically.");

    public static GirdiMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Hatanın metni, kullanıcının dilinde (sayılar ve tarih o dilin biçimiyle).</summary>
    public static string Mesaj(GirdiHatasi h, string dil)
    {
        var m = Al(dil);
        var k = dil == "en" ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
        var sablon = h.Kod switch
        {
            GirdiKodu.IbanBicim => m.IbanBicim,
            GirdiKodu.IbanUzunluk => m.IbanUzunluk,
            GirdiKodu.IbanKontrol => m.IbanKontrol,
            GirdiKodu.TelefonKarakter => m.TelefonKarakter,
            GirdiKodu.TelefonHane => m.TelefonHane,
            GirdiKodu.TelefonAralik => m.TelefonAralik,
            GirdiKodu.TelefonYabanci => m.TelefonYabanci,
            GirdiKodu.EpostaBicim => m.EpostaBicim,
            GirdiKodu.EpostaUzun => m.EpostaUzun,
            GirdiKodu.TarihBicim => m.TarihBicim,
            GirdiKodu.TarihGecersiz => m.TarihGecersiz,
            GirdiKodu.TarihErken => m.TarihErken,
            GirdiKodu.TarihGec => m.TarihGec,
            GirdiKodu.TarihIleri => m.TarihIleri,
            GirdiKodu.TarihCokEski => m.TarihCokEski,
            GirdiKodu.DogumGelecek => m.DogumGelecek,
            GirdiKodu.DogumResit => m.DogumResit,
            GirdiKodu.DogumCokEski => m.DogumCokEski,
            GirdiKodu.AralikTers => m.AralikTers,
            GirdiKodu.AralikUzun => m.AralikUzun,
            GirdiKodu.SayiEksi => m.SayiEksi,
            GirdiKodu.SayiSifir => m.SayiSifir,
            GirdiKodu.SayiOndalik => m.SayiOndalik,
            GirdiKodu.SayiTavan => m.SayiTavan,
            GirdiKodu.SayiEnAz => m.SayiEnAz,
            GirdiKodu.SayiEnCok => m.SayiEnCok,
            GirdiKodu.SayiTam => m.SayiTam,
            GirdiKodu.TasBrut => m.TasBrut,
            GirdiKodu.MilyemAralik => m.MilyemAralik,
            GirdiKodu.YuzdeAralik => m.YuzdeAralik,
            GirdiKodu.GunAralik => m.GunAralik,
            GirdiKodu.BarkodKarakter => m.BarkodKarakter,
            GirdiKodu.BarkodUzunluk => m.BarkodUzunluk,
            GirdiKodu.BarkodBosluk => m.BarkodBosluk,
            GirdiKodu.Ean13Kontrol => m.Ean13Kontrol,
            GirdiKodu.AdKisa => m.AdKisa,
            GirdiKodu.AdHarf => m.AdHarf,
            GirdiKodu.AdUzun => m.AdUzun,
            GirdiKodu.AdresKisa => m.AdresKisa,
            GirdiKodu.AdresHarf => m.AdresHarf,
            GirdiKodu.AdresUzun => m.AdresUzun,
            GirdiKodu.PasaportBicim => m.PasaportBicim,
            GirdiKodu.GerekceKisa => m.GerekceKisa,
            GirdiKodu.GerekceUzun => m.GerekceUzun,
            GirdiKodu.AyarBilinmiyor => m.AyarBilinmiyor,
            GirdiKodu.KimlikRakam => m.KimlikRakam,
            GirdiKodu.KimlikHane => m.KimlikHane,
            GirdiKodu.TcknGecersiz => m.TcknGecersiz,
            GirdiKodu.IbanEksik => m.IbanEksik,
            GirdiKodu.KimlikHaneIkili => m.KimlikHaneIkili,
            GirdiKodu.IbanYabanciBicim => m.IbanYabanciBicim,
            GirdiKodu.FiyatMutlakDisi => m.FiyatMutlakDisi,
            GirdiKodu.FiyatBantDisi => m.FiyatBantDisi,
            GirdiKodu.IslemFiyatBantDisi => m.IslemFiyatBantDisi,
            _ => throw new ArgumentOutOfRangeException(nameof(h), h.Kod, "GirdiSozlugu: metni olmayan kod"),
        };
        // PAKET KUR-BANT tur 2 (doğrulayıcı DÜŞÜK): bant iletisindeki referans fiyat PARA biçimiyle
        // (iki ondalık: 6.827,20 / 49,45) — eskiden 6.827,2 ve 49,448 karışık çıkıyordu.
        var s1 = h.Kod == GirdiKodu.FiyatBantDisi && h.S1 is { } r ? r.ToString("#,0.00", k) : Sayi(h.S1, k);
        // PAKET ISLEM-BANT: işlem bandının iki ucu da TL — para biçimi (iki ondalık).
        var s2 = Sayi(h.S2, k);
        // PAKET ISLEM-BANT tur 2 (doğrulayıcı DÜŞÜK "aynı alanda iki sayı biçimi"): mutlak sınır iletisi de para biçimi —
        // işlem bandı iletisiyle aynı alanda (500,00–50.000,00 TL / 3.413,60–7.509,92 TL) tek biçim.
        if (h.Kod is GirdiKodu.IslemFiyatBantDisi or GirdiKodu.FiyatMutlakDisi)
        {
            if (h.S1 is { } a1) s1 = a1.ToString("#,0.00", k);
            if (h.S2 is { } a2) s2 = a2.ToString("#,0.00", k);
        }
        return string.Format(k, sablon, s1, s2,
            h.Gun is { } g ? TarihPenceresi.Yaz(g) : "");
    }

    private static string Sayi(decimal? d, CultureInfo k) =>
        d is { } x ? x.ToString("#,0.##########", k) : "";
}
