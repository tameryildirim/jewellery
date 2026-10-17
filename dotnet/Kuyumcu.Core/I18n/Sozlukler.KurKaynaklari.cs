namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// KUR KAYNAKLARI (kurucu 17.10) — İşletme → Ayarlar: döviz kaynağı, faturada has
// fiyatı kaynağı ve gün kapanışı girişi (TR + EN). Konumsal kayıt: bir dilde metin
// eksikse DERLEME HATASI. Dil sade (hedef kitle: tezgâhtar) — açıklama paragrafı yok.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record KurKaynakMetinleri(
    string DovizKaynagi,          // seçici etiketi
    string HasFiyatKaynagi,       // seçici etiketi (KDVK 23/e has bedeli kaynağı)
    string Evds,                  // EVDS adı
    string Tcmb,                  // TCMB today.xml adı
    string AltinKaynagiylaAyni,   // döviz = altın kaynağının kendi kuru
    string Harem,                 // Harem Altın adı
    string Bist,                  // Borsa İstanbul kapanışı
    string IslemKuru,             // satıştaki kur (bugünkü davranış)
    string Elle,                  // kendi girdiğim kapanış
    string Binde,                 // faturada binde yöntemi
    string Sozlesmesiz,           // durum: izinsiz kaynak
    string AnahtarYok,            // durum: EVDS anahtarı yok
    string Kapali,                // durum: sözleşmesiz kaynaklar kapalı
    string SonCekim,              // durum: "Son çekim {0}"
    string SonKapanis,            // durum: "Son kapanış {0} · {1}"
    string KapanisYok,            // durum: hiç kapanış yok
    string KapanisGir,            // bağlantı / çekmece başlığı
    string Gun,                   // alan
    string Kapanis,               // alan
    string Birim,                 // alan
    string BirimGram,             // seçenek
    string BirimKg,               // seçenek
    string Not,                   // alan (isteğe bağlı)
    string KapanisKaydedildi,     // başarı
    string DovizEtki,             // onay cümlesi: "{0} → {1}"
    string HasFiyatEtki);         // onay cümlesi: "{0} → {1}"

public static class KurKaynakSozlugu
{
    public static readonly KurKaynakMetinleri Tr = new(
        DovizKaynagi: "Döviz kaynağı",
        HasFiyatKaynagi: "Faturada has fiyatı",
        Evds: "Merkez Bankası (EVDS)",
        Tcmb: "Merkez Bankası günlük kur",
        AltinKaynagiylaAyni: "Altın kaynağıyla aynı",
        Harem: "Harem Altın",
        Bist: "Borsa İstanbul kapanışı",
        IslemKuru: "Satıştaki kur",
        Elle: "Kendi girdiğim kapanış",
        Binde: "Binde yöntemi",
        Sozlesmesiz: "İzinsiz kaynak",
        AnahtarYok: "Anahtar tanımlı değil",
        Kapali: "Kapalı",
        SonCekim: "Son çekim {0}",
        SonKapanis: "Son kapanış {0} · {1}",
        KapanisYok: "Kapanış girilmedi",
        KapanisGir: "Kapanış gir",
        Gun: "Gün",
        Kapanis: "Kapanış",
        Birim: "Birim",
        BirimGram: "TL / gram",
        BirimKg: "TL / kg (bülten)",
        Not: "Not",
        KapanisKaydedildi: "Kapanış kaydedildi",
        DovizEtki: "Döviz kaynağı: {0} → {1}.",
        HasFiyatEtki: "Faturada has fiyatı: {0} → {1}. Yeni faturalar bununla kesilir.");

    public static readonly KurKaynakMetinleri En = new(
        DovizKaynagi: "Currency source",
        HasFiyatKaynagi: "Fine gold price on invoices",
        Evds: "Central Bank (EVDS)",
        Tcmb: "Central Bank daily rate",
        AltinKaynagiylaAyni: "Same as gold source",
        Harem: "Harem Altın",
        Bist: "Borsa Istanbul closing",
        IslemKuru: "Rate at the sale",
        Elle: "My own closing",
        Binde: "Per-mille method",
        Sozlesmesiz: "Unlicensed source",
        AnahtarYok: "Key not set",
        Kapali: "Off",
        SonCekim: "Last fetch {0}",
        SonKapanis: "Last closing {0} · {1}",
        KapanisYok: "No closing entered",
        KapanisGir: "Enter closing",
        Gun: "Day",
        Kapanis: "Closing",
        Birim: "Unit",
        BirimGram: "TRY / gram",
        BirimKg: "TRY / kg (bulletin)",
        Not: "Note",
        KapanisKaydedildi: "Closing saved",
        DovizEtki: "Currency source: {0} → {1}.",
        HasFiyatEtki: "Fine gold price on invoices: {0} → {1}. New invoices use it.");

    public static KurKaynakMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Döviz / has fiyatı kaynak kodu → ad (bilinmeyen kod kendisiyle görünür — uydurma ad yok).</summary>
    public static string Ad(string dil, string? kod, Func<string?, string> altinAdi)
    {
        var m = Al(dil);
        return kod switch
        {
            "EVDS" => m.Evds,
            "TCMB" => m.Tcmb,
            "ALTIN_KAYNAGI" => m.AltinKaynagiylaAyni,
            "HAREM" => m.Harem,
            "BIST" => m.Bist,
            "ISLEM_KURU" => m.IslemKuru,
            "ELLE" => m.Elle,
            "BINDE" => m.Binde,
            _ => altinAdi(kod),
        };
    }
}
