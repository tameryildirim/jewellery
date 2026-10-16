namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-TEZGAH-TUR-KIMLIK — ekran denetimi 10.10 (K4, Y4, Y2, O10) + YASAL-KAPI-3 tasarım kalanları.
//  • İşlem türü seçici görünür (Satış · Alış · İade · Tahsilat · Diğer) — küçük "SATIŞ" yazısının arkasında gizli değil.
//  • Kimlik çekmecesinde birincil "Tamam" GERÇEKTEN çalışır: eksik varsa alanın altında yazar, yoksa kapanır.
//  • Kaydet kilitliyse eksik ALANLARIN ADI tek cümlede ("Kaydetmek için eksik: işlem amacı, …").
// Yalnız ekran: MASAK/nakit kuralları ve eşik SAYILARI sunucuda (hiçbir sayı istemciye gömülmez).
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>P-TEZGAH-TUR-KIMLIK metinleri (bir dilde).</summary>
public sealed record TurKimlikMetinleri(
    // İşlem türü seçicisinin erişilebilir adı
    string TurSeciciEtiket,
    // Sekme adları (tezgâhtar dili, kısa)
    string Satis,
    string Alis,
    string Iade,
    string Diger,
    // Kilitli Kaydet'in tek cümlesi: {0} = eksik alan adları (virgülle)
    string KimlikEksik,
    // Kimlik çekmecesinin birincil düğmesi ve ikincil (kapat) düğmesi
    string Tamam,
    string Kapat,
    // Tamam'a basıldığında boş zorunlu alanın altındaki mesaj
    string AlanZorunlu,
    // Beyan açıklaması kısa: {0} = yazılan, {1} = en az (sunucudan)
    string AciklamaKisa,
    // Tur 2: "Diğer"den seçilen toptancıdan ürün alışı sekmede "Alış" ile karışmasın
    string UrunAlisi);

/// <summary>P-TEZGAH-TUR-KIMLIK sözlüğü (TR + EN).</summary>
public static class TurKimlikSozlugu
{
    public static readonly TurKimlikMetinleri Tr = new(
        TurSeciciEtiket: "İşlem türü",
        Satis: "Satış",
        Alis: "Alış",
        Iade: "İade",
        Diger: "Diğer",
        KimlikEksik: "Kaydetmek için eksik: {0}.",
        Tamam: "Tamam",
        Kapat: "Kapat",
        AlanZorunlu: "Bu alan zorunludur.",
        AciklamaKisa: "En az {1} karakter yazın ({0} / {1}).",
        UrunAlisi: "Ürün alışı");

    public static readonly TurKimlikMetinleri En = new(
        TurSeciciEtiket: "Transaction type",
        Satis: "Sale",
        Alis: "Purchase",
        Iade: "Return",
        Diger: "Other",
        KimlikEksik: "Missing before you can save: {0}.",
        Tamam: "Done",
        Kapat: "Close",
        AlanZorunlu: "This field is required.",
        AciklamaKisa: "Write at least {1} characters ({0} / {1}).",
        UrunAlisi: "Product purchase");

    public static TurKimlikMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
