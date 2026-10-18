namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// D1 — RAKİP EŞİTLEME DALGA 1 (18.10) ekran metinleri · TR + EN
//
// ⚠ Sozlukler.cs ORTAK HUB'dır → yeni metin ayrı dosyada (CiktiBelgeSozlugu deseni).
// ⚠ Konumsal record: Tr ve En aynı kurucudan geçer, eksik anahtar derleme hatasıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>D1 paketlerinin ekran metinleri.</summary>
public sealed record D1Metinleri(
    string KagitSecimi,
    string KagitA4,
    string Kagit80,
    string FaturaYazdir,
    string Beklet,
    string BekleyenlerSayi,
    string BekleyenAl,
    string BekleyenSil,
    string Bekletildi,
    string BekleyenAlindi,
    string OnceFisiBosalt,
    string KalemSayi,
    string MutabakatYazdir,
    string HurdaFotoEkle,
    string HurdaFoto,
    string HurdaFotoBaslik);

/// <summary>D1 metinleri — TR + EN.</summary>
public static class D1Sozlugu
{
    public static readonly D1Metinleri Tr = new(
        KagitSecimi: "Kâğıt",
        KagitA4: "A4",
        Kagit80: "80 mm fiş",
        FaturaYazdir: "Yazdır",
        Beklet: "Beklet",
        BekleyenlerSayi: "Bekleyen fişler ({0})",
        BekleyenAl: "Al",
        BekleyenSil: "Sil",
        Bekletildi: "Fiş bekletildi: {0}",
        BekleyenAlindi: "Bekleyen fiş tezgâha alındı: {0}",
        OnceFisiBosalt: "Önce tezgâhtaki fişi bekletin ya da vazgeçin",
        KalemSayi: "{0} kalem",
        MutabakatYazdir: "Mutabakat formu",
        HurdaFotoEkle: "Fotoğraf ekle",
        HurdaFoto: "Fotoğraf",
        HurdaFotoBaslik: "Hurda fotoğrafı");

    public static readonly D1Metinleri En = new(
        KagitSecimi: "Paper",
        KagitA4: "A4",
        Kagit80: "80 mm receipt",
        FaturaYazdir: "Print",
        Beklet: "Hold",
        BekleyenlerSayi: "Held receipts ({0})",
        BekleyenAl: "Resume",
        BekleyenSil: "Delete",
        Bekletildi: "Receipt held: {0}",
        BekleyenAlindi: "Held receipt resumed: {0}",
        OnceFisiBosalt: "First hold or cancel the receipt on the counter",
        KalemSayi: "{0} items",
        MutabakatYazdir: "Reconciliation form",
        HurdaFotoEkle: "Add photo",
        HurdaFoto: "Photo",
        HurdaFotoBaslik: "Scrap photo");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static D1Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
