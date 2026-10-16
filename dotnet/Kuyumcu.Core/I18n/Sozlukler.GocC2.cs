namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-C-2 — GOC-C kapanış devri (tasarım hakemi bulguları) ekran metinleri (TR + EN).
// Konumsal kayıt: bir dilde metin eksikse DERLEME HATASI (UX-S31 — çıplak metin yok).
// Girdi kural metinleri BURADA DEĞİL: GirdiSozlugu (Sozlukler.Girdi.cs).
// ⚠ Mevzuat hükmü YOKTUR: yalnız ekran dili.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record GocC2Metinleri(
    string OdemeAsimi,               // VERI-1: ödeme satırı fiş toplamını aşıyor (alan altı) — {0} = fazla tutar
    string KesirTusuEngeli,          // VERI-2: ADET alanında ondalık tuşu yazılmadı (tuş anı ipucu)
    string KesirTusuEngeliGenel,     // VERI-2: diğer tam sayı alanlarında (gün, sıra, sayılan adet) aynı ipucu
    string NakitSiniriKisa,          // açıklama satırı: nakit sınırı notunun TEK kısa satırı — {0} = sunucunun haddi (tam cümle ⓘ içinde)
    string NakitSiniriKisaSayisiz,   // had henüz bilinmiyorken kısa satır (sayı UYDURULMAZ)
    string NakitSiniriBilgi,         // ⓘ düğmesinin erişilebilir adı
    string SatirDuzenleBaslik,       // satış PRO: ✎ ile açılan satır düzenleme paneli başlığı — {0} = ürün adı
    string UrunAraKisa);             // /urunler arama kutusu yer tutucusu (PRO 1280'de 6 süzgeçli satırda sığar; okutucu Enter'ı kendisi basar)

public static class GocC2Sozlugu
{
    public static readonly GocC2Metinleri Tr = new(
        OdemeAsimi: "Bu tutar fiş toplamını {0} aşıyor. Azaltın ya da “Kalanı ata”ya basın.",
        KesirTusuEngeli: "Adet küsuratlı yazılamaz; tam sayı girin.",
        KesirTusuEngeliGenel: "Bu alana küsurat yazılamaz; tam sayı girin.",
        NakitSiniriKisa: "Nakit sınırı: {0}",
        NakitSiniriKisaSayisiz: "Yasal nakit sınırı var",
        NakitSiniriBilgi: "Nakit sınırı hakkında",
        SatirDuzenleBaslik: "{0} — satırı düzenle",
        UrunAraKisa: "Ad, kod ya da barkod");

    public static readonly GocC2Metinleri En = new(
        OdemeAsimi: "This amount exceeds the receipt total by {0}. Lower it or press “Assign remainder”.",
        KesirTusuEngeli: "Quantity cannot have decimals; enter a whole number.",
        KesirTusuEngeliGenel: "This field takes whole numbers only.",
        NakitSiniriKisa: "Cash limit: {0}",
        NakitSiniriKisaSayisiz: "A legal cash limit applies",
        NakitSiniriBilgi: "About the cash limit",
        SatirDuzenleBaslik: "{0} — edit line",
        UrunAraKisa: "Name, code or barcode");

    public static GocC2Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
