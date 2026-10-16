namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-B (tur 2) — kimlik ve uyum ekranları göçünün YENİ ekran metinleri (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Girdi kural metinleri BURADA DEĞİL: GirdiSozlugu (Sozlukler.Girdi.cs).
// ═══════════════════════════════════════════════════════════════════════════

public sealed record GocBMetinleri(
    string DogumResitDegilUyari,     // 18 yaş altı: ENGEL DEĞİL, yalnız uyarı (K8f)
    string KimlikTuru,               // evrak borçlu kimlik türü seçici etiketi
    string KimlikTuruYerli,          // TC kimlik / vergi no
    string KimlikTuruYabanci,        // yabancı: pasaport / yabancı kimlik no
    string KimlikKayitliGizli,       // düzenlemede: kayıtlı kimlik gösterilmez, değişmez
    string KimlikKaldir,             // açık "kimliği kaldır" eylemi
    string KimlikKaldirilacak,       // Kaydet'e basınca silinecek uyarısı
    string BorcluPasaportNo);        // yabancı seçilince kutunun etiketi

public static class GocBSozlugu
{
    public static readonly GocBMetinleri Tr = new(
        DogumResitDegilUyari: "Kişi 18 yaşından küçük. Tarihi kontrol edin.",
        KimlikTuru: "Kimlik türü",
        KimlikTuruYerli: "TC kimlik / vergi no",
        KimlikTuruYabanci: "Yabancı (pasaport no)",
        KimlikKayitliGizli: "Kayıtlı kimlik no gizli tutulur ve değişmez. Değiştirmek için yenisini yazın.",
        KimlikKaldir: "Kimlik no'yu kaldır",
        KimlikKaldirilacak: "Kaydet'e basınca kayıtlı kimlik no silinecek.",
        BorcluPasaportNo: "Borçlu pasaport no");

    public static readonly GocBMetinleri En = new(
        DogumResitDegilUyari: "The person is under 18. Check the date.",
        KimlikTuru: "ID type",
        KimlikTuruYerli: "Turkish ID / tax no",
        KimlikTuruYabanci: "Foreign (passport no)",
        KimlikKayitliGizli: "The saved ID number is kept hidden and stays unchanged. Type a new one to change it.",
        KimlikKaldir: "Remove ID number",
        KimlikKaldirilacak: "The saved ID number will be deleted when you press Save.",
        BorcluPasaportNo: "Debtor passport no");

    public static GocBMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
