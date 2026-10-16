namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-2 — ALTIN POZİSYONU KARTI: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı dalgada
//   başka şeritler de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI
//   dosya aç"). GorunumSozlugu / HavaSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçtiği için bir dilde alan unutulursa
//   derleme kırılır. Maden adları da burada iki dillidir — ham kod (GUMUS)
//   ekrana basılmaz; sözlükte olmayan bir kod için genel "Diğer maden" metni
//   döner (ne ham kod ne uydurma ad).
//
// ⚠ Jargon sıfır (PLAN § 1): hedef kitle tezgâhtar/patron — "pozisyon" kelimesi
//   taslaktaki başlıkta ("Altın pozisyonum") geçtiği için korunur; kırılım
//   satırları gündelik dille yazılır (Vitrin, Alacak, Borç, Hurda).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Altın pozisyonu kartının bir dildeki tüm metinleri.</summary>
public sealed record PozisyonMetinleri(
    string Baslik,
    string BirimHas,
    string Vitrin,
    string Hurda,
    string Alacak,
    string Borc,
    string DonemdenBagimsiz,
    string YetkiYok,
    string YuklemeHatasi,
    string Bos,
    string DigerMadenlerBaslik,
    string MadenlerToplanmaz,
    string Konsinye,
    string Emanet,
    string PozisyonaGirmez,
    string DefterFarki,
    string AlacakListesiBaslik,
    string BorcListesiBaslik,
    string ListeBos,
    string ListeSiniri,
    string DahaFazlasiVar,
    string CariSutun,
    string HasSutun,
    string DegismezGerekce,
    string DuzeltmeYolu,
    string VitrinBaglantiIpucu,
    string DigerMaden);

public static class PozisyonSozlugu
{
    public static readonly PozisyonMetinleri Tr = new(
        Baslik: "Altın pozisyonum",
        BirimHas: "g has",
        Vitrin: "Vitrin",
        Hurda: "Hurda",
        Alacak: "Alacak",
        Borc: "Borç",
        DonemdenBagimsiz: "(bugün itibarıyla — dönemden bağımsız)",
        YetkiYok: "Bu kartı patron, müdür ve muhasebe görür.",
        YuklemeHatasi: "Altın pozisyonu okunamadı",
        Bos: "Henüz stokta, hurdada ya da carilerde altın yok.",
        DigerMadenlerBaslik: "Diğer madenler",
        MadenlerToplanmaz: "Farklı madenlerin gramları toplanmaz; her maden kendi satırındadır.",
        Konsinye: "Konsinye mal",
        Emanet: "Müşteri emaneti",
        PozisyonaGirmez: "sizin değil — pozisyona girmez",
        DefterFarki: "Uyarı: carilerin altın bakiyesi ile defter {0} g farklı. Cari ekstrelerini kontrol edin.",
        AlacakListesiBaslik: "Bize altın borçlu olanlar",
        BorcListesiBaslik: "Bizim altın borçlu olduklarımız",
        ListeBos: "Bu listede kimse yok.",
        ListeSiniri: "En büyük {0} kayıt gösterilir; tamamı için Müşteriler ekranını kullanın.",
        DahaFazlasiVar: "Daha fazlası var: {0} kayıttan {1} tanesi gösteriliyor.",
        CariSutun: "Müşteri / tedarikçi",
        HasSutun: "Altın (g has)",
        DegismezGerekce: "Bu sayı hesaplanır: vitrindeki stok, hurda ve carilerin altın bakiyesinden türetilir; elle değiştirilmez.",
        DuzeltmeYolu: "Yanlışsa kaynağını düzeltin: stok için sayım, cari için düzeltme fişi, hurda için hurda ekranı.",
        VitrinBaglantiIpucu: "Vitrindeki ürünleri aç",
        DigerMaden: "Diğer maden");

    public static readonly PozisyonMetinleri En = new(
        Baslik: "My gold position",
        BirimHas: "g fine",
        Vitrin: "Showcase",
        Hurda: "Scrap",
        Alacak: "Receivable",
        Borc: "Payable",
        DonemdenBagimsiz: "(as of today — independent of the period)",
        YetkiYok: "This card is visible to owners, managers and accounting.",
        YuklemeHatasi: "Gold position could not be loaded",
        Bos: "No gold in stock, scrap or customer balances yet.",
        DigerMadenlerBaslik: "Other metals",
        MadenlerToplanmaz: "Grams of different metals are never added together; each metal has its own row.",
        Konsinye: "Consignment goods",
        Emanet: "Customer deposits",
        PozisyonaGirmez: "not yours — excluded from the position",
        DefterFarki: "Warning: customer gold balances differ from the ledger by {0} g. Check the account statements.",
        AlacakListesiBaslik: "Who owes us gold",
        BorcListesiBaslik: "Whom we owe gold",
        ListeBos: "Nobody is on this list.",
        ListeSiniri: "The largest {0} records are shown; use the Customers screen for all of them.",
        DahaFazlasiVar: "There is more: showing {1} of {0} records.",
        CariSutun: "Customer / supplier",
        HasSutun: "Gold (g fine)",
        DegismezGerekce: "This number is calculated from showcase stock, scrap and customer gold balances; it cannot be edited by hand.",
        DuzeltmeYolu: "If it is wrong, fix the source: a stock count for stock, a correction slip for a customer, the scrap screen for scrap.",
        VitrinBaglantiIpucu: "Open showcase products",
        DigerMaden: "Other metal");

    /// <summary>Seçili dilin metinleri.</summary>
    public static PozisyonMetinleri Al(string dil) => dil == "en" ? En : Tr;

    private static readonly IReadOnlyDictionary<string, (string Tr, string En)> Madenler =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["ALTIN"] = ("Altın", "Gold"),
            ["GUMUS"] = ("Gümüş", "Silver"),
            ["PLATIN"] = ("Platin", "Platinum"),
            ["PALADYUM"] = ("Paladyum", "Palladium"),
            ["CELIK"] = ("Çelik", "Steel"),
        };

    /// <summary>Maden kodunun iki dilli adı; sözlükte yoksa genel "Diğer maden" (ham kod basılmaz).</summary>
    public static string MadenAdi(string? kod, string dil) =>
        kod is not null && Madenler.TryGetValue(kod, out var ad)
            ? (dil == "en" ? ad.En : ad.Tr)
            : Al(dil).DigerMaden;

    /// <summary>Test kilidi: Kuyumcu.Core.Domain.Maden.Hepsi'nin her kodu sözlükte olmalı.</summary>
    public static bool MadenVar(string kod) => Madenler.ContainsKey(kod);
}
