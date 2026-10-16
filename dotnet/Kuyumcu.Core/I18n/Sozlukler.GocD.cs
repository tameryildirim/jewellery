namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-D — yönetim ve rapor ekranları göçünün YENİ metinleri (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Girdi kural metinleri BURADA DEĞİL: GirdiSozlugu (Sozlukler.Girdi.cs); burada yalnız
// o sözlükte karşılığı olmayan iki kural metni (rapor dönemi gün sınırı, süzgeç beyaz listesi)
// ve gün sonu özet ızgarasının iki sütun başlığı (eski tablo başlıksızdı) durur.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record GocDMetinleri(
    string RaporDonemUzun,        // {0} = gün (RaporDonemKurali.EnCokGun)
    string SuzgecBilinmiyor,      // beyaz liste dışı süzgeç
    string Bolum,                 // mutabakat/özet tablolarının "kalem" sütunu
    string Deger);                // mutabakat/özet tablolarının "değer" sütunu

public static class GocDSozlugu
{
    public static readonly GocDMetinleri Tr = new(
        RaporDonemUzun: "Rapor dönemi en çok {0} gün olabilir.",
        SuzgecBilinmiyor: "Bu süzgeç değeri tanınmıyor. Listeden seçin.",
        Bolum: "Kalem",
        Deger: "Değer");

    public static readonly GocDMetinleri En = new(
        RaporDonemUzun: "The report period can be at most {0} days.",
        SuzgecBilinmiyor: "This filter value is not recognised. Choose from the list.",
        Bolum: "Item",
        Deger: "Value");

    public static GocDMetinleri Al(string? dil) => dil == "en" ? En : Tr;
}
