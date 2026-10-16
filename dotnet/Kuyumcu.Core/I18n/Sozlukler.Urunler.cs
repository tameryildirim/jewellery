using Kuyumcu.Core.Domain;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-3 — ÜRÜNLER (/urunler) + ETİKETLER (/urunler/etiketler): TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin için AYRI dosya —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs). S3-2 MusteriListeSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'larda
//   durur; Tr ve En aynı kurucudan geçer. Ürün grubu adları da record'dur:
//   UrunTipleri'ne yeni grup eklenip burada karşılığı yazılmazsa
//   UrunGrubuAdlari.Ad() ham kodu döndürür ve S33 testi kırmızıya döner.
//
// ⚠ JARGON SIFIR (PLAN § 5B): etiket ≤ 3 kelime, mesaj ≤ 2 cümle; ham kod
//   (ZIYNET, DEGERLI_TAS …) ekrana basılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ürünler ekranının liste + çekmece yüzündeki metinler (bir dilde).</summary>
public sealed record UrunListeMetinleri(
    string DigerIslemler,
    string MenuBaslik,
    string MenuIpucu,
    string Suzgec,
    string SuzgecSayili,
    string SuzgecKapat,
    string Durum,
    string DurumHepsi,
    string StokSutunu,
    string StokAdetIpucu,
    string StokYukleniyor,
    string StokAlinamadi,
    string SubeBilinmiyor,
    string DahaFazla,
    string DahaFazlaIcerik,
    string DahaAz,
    string MadenKilit);

/// <summary>Ürün grubu (UrunTipleri) kodlarının ekrandaki adları — bir dilde.</summary>
public sealed record UrunGrubuAdlari(
    string Ziynet, string Kulce, string Hurda, string Sikke, string Pirlanta, string Saat,
    string Gumus, string Platin, string Paladyum, string Celik, string DegerliTas)
{
    /// <summary>Kodu ekrandaki ada çevirir; tanınmayan kod DEĞİŞMEDEN döner (gizlenmez).</summary>
    public string Ad(string? kod) => kod switch
    {
        UrunTipleri.Ziynet => Ziynet,
        UrunTipleri.Kulce => Kulce,
        UrunTipleri.Hurda => Hurda,
        UrunTipleri.Sikke => Sikke,
        UrunTipleri.Pirlanta => Pirlanta,
        UrunTipleri.Saat => Saat,
        UrunTipleri.Gumus => Gumus,
        UrunTipleri.Platin => Platin,
        UrunTipleri.Paladyum => Paladyum,
        UrunTipleri.Celik => Celik,
        UrunTipleri.DegerliTas => DegerliTas,
        null => "",
        _ => kod,
    };
}

/// <summary>Etiket ekranının metinleri (bir dilde).</summary>
public sealed record EtiketEkranMetinleri(
    string AramaSonucYok,
    string ListeBos,
    string SecimKaldirildi,
    string SeciliOzet,
    string OnceSecin);

public static class UrunListeSozlugu
{
    public static readonly UrunListeMetinleri Tr = new(
        DigerIslemler: "Diğer işlemler",
        MenuBaslik: "{0} için işlemler",
        MenuIpucu: "Birini seçin. Pencere kendiliğinden kapanır.",
        Suzgec: "Süzgeç",
        SuzgecSayili: "Süzgeç ({0})",
        SuzgecKapat: "Süzgeci kapat",
        Durum: "Durum",
        DurumHepsi: "Hepsi",
        StokSutunu: "Stok",
        StokAdetIpucu: "{0} — şube stoklarını aç",
        StokYukleniyor: "…",
        StokAlinamadi: "Stok adetleri alınamadı. Satırdaki Stok düğmesiyle bakabilirsiniz.",
        SubeBilinmiyor: "Tanımsız şube",
        DahaFazla: "Daha fazla",
        DahaFazlaIcerik: "fiyat, taş, kategori, kart",
        DahaAz: "Daha az göster",
        MadenKilit: "{0} ürünü {1} olur; maden gruptan gelir.");

    public static readonly UrunListeMetinleri En = new(
        DigerIslemler: "More actions",
        MenuBaslik: "Actions for {0}",
        MenuIpucu: "Pick one. This window closes by itself.",
        Suzgec: "Filter",
        SuzgecSayili: "Filter ({0})",
        SuzgecKapat: "Close filter",
        Durum: "Status",
        DurumHepsi: "All",
        StokSutunu: "Stock",
        StokAdetIpucu: "{0} — open branch stock",
        StokYukleniyor: "…",
        StokAlinamadi: "Stock counts could not be loaded. Use the Stock button on the row.",
        SubeBilinmiyor: "Unknown branch",
        DahaFazla: "More",
        DahaFazlaIcerik: "price, stone, category, card",
        DahaAz: "Show less",
        MadenKilit: "A {0} item is {1}; the metal comes from the group.");

    public static readonly UrunGrubuAdlari GrupTr = new(
        Ziynet: "Ziynet", Kulce: "Külçe", Hurda: "Hurda", Sikke: "Sikke", Pirlanta: "Pırlanta",
        Saat: "Saat", Gumus: "Gümüş", Platin: "Platin", Paladyum: "Paladyum", Celik: "Çelik",
        DegerliTas: "Değerli taş");

    public static readonly UrunGrubuAdlari GrupEn = new(
        Ziynet: "Jewellery", Kulce: "Bullion", Hurda: "Scrap", Sikke: "Coin", Pirlanta: "Diamond",
        Saat: "Watch", Gumus: "Silver", Platin: "Platinum", Paladyum: "Palladium", Celik: "Steel",
        DegerliTas: "Gemstone");

    public static readonly EtiketEkranMetinleri EtiketTr = new(
        AramaSonucYok: "\"{0}\" ile eşleşen ürün yok. Aramayı değiştirin.",
        ListeBos: "Etiketi basılacak ürün yok.",
        SecimKaldirildi: "Aramada görünmeyen {0} seçim kaldırıldı.",
        SeciliOzet: "{0} ürün seçili · {1} etiket",
        OnceSecin: "Önce listeden ürün seçin.");

    public static readonly EtiketEkranMetinleri EtiketEn = new(
        AramaSonucYok: "No product matches \"{0}\". Change the search.",
        ListeBos: "No products to label.",
        SecimKaldirildi: "{0} selections hidden by the search were removed.",
        SeciliOzet: "{0} products selected · {1} labels",
        OnceSecin: "Select products from the list first.");

    public static UrunListeMetinleri Al(string dil) => dil == "en" ? En : Tr;
    public static UrunGrubuAdlari Grup(string dil) => dil == "en" ? GrupEn : GrupTr;
    public static EtiketEkranMetinleri Etiket(string dil) => dil == "en" ? EtiketEn : EtiketTr;
}
