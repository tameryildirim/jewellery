namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P2 · MÜŞTERİ — cari detay has toplamı (#8), uyruk tek standart (#22),
// vade tam sayı (#5), düzeltme fişi tek sayaç (#21 alt), ad alan altı mesajı (ORTAK-1).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   AYRI dosyaya). HUB'daki eski `Cari.UyrukIpucu` ("İki harfli …") P8'e aittir;
//   ekran artık bu dosyadaki yeni anahtarı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P2 metinleri (bir dilde).</summary>
public sealed record KesifP2Metinleri(
    string UyrukSecin,
    string UyrukDiger,
    string UyrukKod,
    string UyrukKodIpucu,
    string UyrukKodGecersiz,
    string UyrukDuzeltin,
    string UyrukDuzeltinAciklama,
    string EmanetRozet,
    string EmanetBakiyeyeGirmez,
    string HasEtkisiKuralOzet,
    string HasEtkisiKural,
    string SayfaDisiEtki,
    string DahaFazlaVar,
    string HareketlerBaslik,
    string SutunIslem,
    string SutunTutar,
    string SutunHasEtkisi,
    string VadeTamSayi,
    string VadeTamSayiEtiket,
    string GerekceSayac,
    string AdZorunlu,
    string BakiyeUyusmazligi,
    // ── Tur 3 (hakem: ham kod, emanet kararı 3, borç/alacak tek bakış, has birimi) ──
    string DuzeltmeUyari,
    string DuzeltmeYon,
    string DuzeltmeHasEtiket,
    string BirimHas,
    string YonBizeBorclu,
    string YonBizBorcluyuz,
    string SutunBakiye,
    string AltinBakiye,
    string Emanette,
    string EmanetteAlt,
    string EmanetSatir,
    // PAKET CARI-BAKIYE: TL aynası bandı ve liste "Emanette" rozeti.
    string BakiyeUyusmazligiTl,
    string ListeEmanette);

public static class KesifP2Sozlugu
{
    public static readonly KesifP2Metinleri Tr = new(
        UyrukSecin: "Seçilmedi",
        UyrukDiger: "Diğer ülke",
        UyrukKod: "Ülke kodu (3 harf)",
        UyrukKodIpucu: "Pasaporttaki 3 harfli kod (ör. FRA, NLD).",
        UyrukKodGecersiz: "Bu kod tanınmadı. Pasaporttaki 3 harfli ülke kodunu yazın (ör. FRA).",
        UyrukDuzeltin: "Uyruk kodunu düzeltin",
        UyrukDuzeltinAciklama: "Kayıttaki \"{0}\" kodu eski biçimde ve bir ülkeyle eşlenemedi. Listeden ülkeyi seçin.",
        EmanetRozet: "Emanet",
        EmanetBakiyeyeGirmez: "Emanet — bakiyeye girmez",
        HasEtkisiKuralOzet: "Has sütunu nasıl okunur?",
        HasEtkisiKural: "Has sütunu, işlemin müşterinin altın bakiyesine etkisidir; satırların toplamı üstteki altın bakiyesine eşittir. Emanet (müşterinin tamir ya da saklama için bıraktığı mal) bakiyeye girmez: satırında \"Emanet — bakiyeye girmez\" yazar, altını ayrı \"Emanette\" kutusunda görünür. Müşteri listesindeki altın bakiyesi de aynı (emanetsiz) tutardır; emanet orada ayrı \"Emanette\" rozetiyle görünür. Satış ve alış altın bakiyesini değil TL bakiyesini etkiler.",
        SayfaDisiEtki: "Listede görünmeyen eski işlemlerin etkisi: {0}",
        DahaFazlaVar: "En yeni {0} işlem gösteriliyor; daha eskisi var.",
        HareketlerBaslik: "İşlemler",
        SutunIslem: "İşlem",
        SutunTutar: "Tutar",
        SutunHasEtkisi: "Altın hesabına etkisi",
        VadeTamSayi: "Gün sayısı tam sayı olmalı (ör. 15).",
        VadeTamSayiEtiket: "Vade (tam gün)",
        GerekceSayac: "{0} karakter · en az {1}",
        AdZorunlu: "{0} boş bırakılamaz.",
        BakiyeUyusmazligi: "Kayıtlı altın bakiyesi ile işlemlerin toplamı uyuşmuyor (fark {0}). Bakiyenin işlemlerden yeniden hesaplanması gerekir; düzeltme fişi bu farkı kapatmaz. Destekle iletişime geçin.",
        DuzeltmeUyari: "Bakiyeyi elle düzeltir. Bakiye düzeltme olarak kaydedilir ve geri alınamaz. Gerekçe zorunlu (en az 20 karakter).",
        DuzeltmeYon: "+ yazarsanız müşteri borçlanır · − yazarsanız biz borçlanırız",
        DuzeltmeHasEtiket: "Altın farkı (±)",
        BirimHas: "has",
        YonBizeBorclu: BorcDiliSozlugu.BizeBorcluEtiket("tr"), // P-DIL-BORC-KASA
        YonBizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzEtiket("tr"),
        SutunBakiye: "Bakiye",
        AltinBakiye: "Altın bakiyesi",
        Emanette: "Emanette",
        EmanetteAlt: "Müşterinin bıraktığı · bakiyeye girmez",
        EmanetSatir: "Emanet {0}",
        BakiyeUyusmazligiTl: "Kayıtlı TL bakiyesi ile işlemlerin toplamı uyuşmuyor (fark {0}). Bakiyenin işlemlerden yeniden hesaplanması gerekir; düzeltme fişi bu farkı kapatmaz. Destekle iletişime geçin.",
        ListeEmanette: "Emanette {0}");

    public static readonly KesifP2Metinleri En = new(
        UyrukSecin: "Not selected",
        UyrukDiger: "Other country",
        UyrukKod: "Country code (3 letters)",
        UyrukKodIpucu: "The 3-letter code in the passport (e.g. FRA, NLD).",
        UyrukKodGecersiz: "This code is not recognised. Type the 3-letter country code from the passport (e.g. FRA).",
        UyrukDuzeltin: "Fix the nationality code",
        UyrukDuzeltinAciklama: "The stored code \"{0}\" is in the old format and could not be matched to a country. Choose the country from the list.",
        EmanetRozet: "Deposit",
        EmanetBakiyeyeGirmez: "Deposit — not in the balance",
        HasEtkisiKuralOzet: "How to read the fine gold column",
        HasEtkisiKural: "The fine gold column is the effect of the transaction on the customer's gold balance; the rows add up to the gold balance above. A deposit (goods the customer left for repair or safekeeping) is not part of the balance: its row says \"Deposit — not in the balance\" and its gold appears in the separate \"On deposit\" box. The gold balance in the customer list is the same amount (without deposits); deposits appear there with a separate \"On deposit\" badge. Sales and purchases affect the TL balance, not the gold balance.",
        SayfaDisiEtki: "Effect of older transactions not listed: {0}",
        DahaFazlaVar: "Showing the latest {0} transactions; there are older ones.",
        HareketlerBaslik: "Transactions",
        SutunIslem: "Transaction",
        SutunTutar: "Amount",
        SutunHasEtkisi: "Effect on gold account",
        VadeTamSayi: "The number of days must be a whole number (e.g. 15).",
        VadeTamSayiEtiket: "Due date (whole days)",
        GerekceSayac: "{0} characters · at least {1}",
        AdZorunlu: "{0} cannot be empty.",
        BakiyeUyusmazligi: "The recorded gold balance does not match the total of the transactions (difference {0}). The balance must be recalculated from the transactions; an adjustment slip does not close this gap. Please contact support.",
        DuzeltmeUyari: "Adjusts the balance manually. It is saved as a balance adjustment and cannot be undone. A reason is required (at least 20 characters).",
        DuzeltmeYon: "+ the customer owes more · − we owe more",
        DuzeltmeHasEtiket: "Gold difference (±)",
        BirimHas: "fine",
        YonBizeBorclu: BorcDiliSozlugu.BizeBorcluEtiket("en"),
        YonBizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzEtiket("en"),
        SutunBakiye: "Balance",
        AltinBakiye: "Gold balance",
        Emanette: "On deposit",
        EmanetteAlt: "Left by the customer · not in the balance",
        EmanetSatir: "Deposit {0}",
        BakiyeUyusmazligiTl: "The recorded TL balance does not match the total of the transactions (difference {0}). The balance must be recalculated from the transactions; an adjustment slip does not close this gap. Please contact support.",
        ListeEmanette: "On deposit {0}");

    public static KesifP2Metinleri Al(string dil) => dil == "en" ? En : Tr;
}

/// <summary>
/// KESIF-P2 (#22) — eski 2 harfli uyruk (ISO 3166-1 alpha-2) → alpha-3 eşlemesi.
/// Kaynak: IANA tz veritabanı <c>iso3166.tab</c> (249 alpha-2 satırı) → .NET/ICU
/// <c>RegionInfo.ThreeLetterISORegionName</c> (30.09 üretildi; ICU'nun tanımadığı EH → ESH elle —
/// <see cref="UlkeListesi.IsoKodlari"/> ile aynı yöntem). Her hedef kod IsoKodlari içindedir
/// (KesifP2 testi sınar). Mevzuat listesi DEĞİLDİR; yalnız yazım standardını birleştirir.
/// Listede olmayan 2 harfli değer EŞLENMEZ ve uydurulmaz (ekip kararı 4).
/// </summary>
public static class UyrukEslemesi
{
    public static readonly IReadOnlyDictionary<string, string> Alpha2Alpha3 = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AD"] = "AND", ["AE"] = "ARE", ["AF"] = "AFG", ["AG"] = "ATG", ["AI"] = "AIA", ["AL"] = "ALB",
        ["AM"] = "ARM", ["AO"] = "AGO", ["AQ"] = "ATA", ["AR"] = "ARG", ["AS"] = "ASM", ["AT"] = "AUT",
        ["AU"] = "AUS", ["AW"] = "ABW", ["AX"] = "ALA", ["AZ"] = "AZE", ["BA"] = "BIH", ["BB"] = "BRB",
        ["BD"] = "BGD", ["BE"] = "BEL", ["BF"] = "BFA", ["BG"] = "BGR", ["BH"] = "BHR", ["BI"] = "BDI",
        ["BJ"] = "BEN", ["BL"] = "BLM", ["BM"] = "BMU", ["BN"] = "BRN", ["BO"] = "BOL", ["BQ"] = "BES",
        ["BR"] = "BRA", ["BS"] = "BHS", ["BT"] = "BTN", ["BV"] = "BVT", ["BW"] = "BWA", ["BY"] = "BLR",
        ["BZ"] = "BLZ", ["CA"] = "CAN", ["CC"] = "CCK", ["CD"] = "COD", ["CF"] = "CAF", ["CG"] = "COG",
        ["CH"] = "CHE", ["CI"] = "CIV", ["CK"] = "COK", ["CL"] = "CHL", ["CM"] = "CMR", ["CN"] = "CHN",
        ["CO"] = "COL", ["CR"] = "CRI", ["CU"] = "CUB", ["CV"] = "CPV", ["CW"] = "CUW", ["CX"] = "CXR",
        ["CY"] = "CYP", ["CZ"] = "CZE", ["DE"] = "DEU", ["DJ"] = "DJI", ["DK"] = "DNK", ["DM"] = "DMA",
        ["DO"] = "DOM", ["DZ"] = "DZA", ["EC"] = "ECU", ["EE"] = "EST", ["EG"] = "EGY", ["EH"] = "ESH",
        ["ER"] = "ERI", ["ES"] = "ESP", ["ET"] = "ETH", ["FI"] = "FIN", ["FJ"] = "FJI", ["FK"] = "FLK",
        ["FM"] = "FSM", ["FO"] = "FRO", ["FR"] = "FRA", ["GA"] = "GAB", ["GB"] = "GBR", ["GD"] = "GRD",
        ["GE"] = "GEO", ["GF"] = "GUF", ["GG"] = "GGY", ["GH"] = "GHA", ["GI"] = "GIB", ["GL"] = "GRL",
        ["GM"] = "GMB", ["GN"] = "GIN", ["GP"] = "GLP", ["GQ"] = "GNQ", ["GR"] = "GRC", ["GS"] = "SGS",
        ["GT"] = "GTM", ["GU"] = "GUM", ["GW"] = "GNB", ["GY"] = "GUY", ["HK"] = "HKG", ["HM"] = "HMD",
        ["HN"] = "HND", ["HR"] = "HRV", ["HT"] = "HTI", ["HU"] = "HUN", ["ID"] = "IDN", ["IE"] = "IRL",
        ["IL"] = "ISR", ["IM"] = "IMN", ["IN"] = "IND", ["IO"] = "IOT", ["IQ"] = "IRQ", ["IR"] = "IRN",
        ["IS"] = "ISL", ["IT"] = "ITA", ["JE"] = "JEY", ["JM"] = "JAM", ["JO"] = "JOR", ["JP"] = "JPN",
        ["KE"] = "KEN", ["KG"] = "KGZ", ["KH"] = "KHM", ["KI"] = "KIR", ["KM"] = "COM", ["KN"] = "KNA",
        ["KP"] = "PRK", ["KR"] = "KOR", ["KW"] = "KWT", ["KY"] = "CYM", ["KZ"] = "KAZ", ["LA"] = "LAO",
        ["LB"] = "LBN", ["LC"] = "LCA", ["LI"] = "LIE", ["LK"] = "LKA", ["LR"] = "LBR", ["LS"] = "LSO",
        ["LT"] = "LTU", ["LU"] = "LUX", ["LV"] = "LVA", ["LY"] = "LBY", ["MA"] = "MAR", ["MC"] = "MCO",
        ["MD"] = "MDA", ["ME"] = "MNE", ["MF"] = "MAF", ["MG"] = "MDG", ["MH"] = "MHL", ["MK"] = "MKD",
        ["ML"] = "MLI", ["MM"] = "MMR", ["MN"] = "MNG", ["MO"] = "MAC", ["MP"] = "MNP", ["MQ"] = "MTQ",
        ["MR"] = "MRT", ["MS"] = "MSR", ["MT"] = "MLT", ["MU"] = "MUS", ["MV"] = "MDV", ["MW"] = "MWI",
        ["MX"] = "MEX", ["MY"] = "MYS", ["MZ"] = "MOZ", ["NA"] = "NAM", ["NC"] = "NCL", ["NE"] = "NER",
        ["NF"] = "NFK", ["NG"] = "NGA", ["NI"] = "NIC", ["NL"] = "NLD", ["NO"] = "NOR", ["NP"] = "NPL",
        ["NR"] = "NRU", ["NU"] = "NIU", ["NZ"] = "NZL", ["OM"] = "OMN", ["PA"] = "PAN", ["PE"] = "PER",
        ["PF"] = "PYF", ["PG"] = "PNG", ["PH"] = "PHL", ["PK"] = "PAK", ["PL"] = "POL", ["PM"] = "SPM",
        ["PN"] = "PCN", ["PR"] = "PRI", ["PS"] = "PSE", ["PT"] = "PRT", ["PW"] = "PLW", ["PY"] = "PRY",
        ["QA"] = "QAT", ["RE"] = "REU", ["RO"] = "ROU", ["RS"] = "SRB", ["RU"] = "RUS", ["RW"] = "RWA",
        ["SA"] = "SAU", ["SB"] = "SLB", ["SC"] = "SYC", ["SD"] = "SDN", ["SE"] = "SWE", ["SG"] = "SGP",
        ["SH"] = "SHN", ["SI"] = "SVN", ["SJ"] = "SJM", ["SK"] = "SVK", ["SL"] = "SLE", ["SM"] = "SMR",
        ["SN"] = "SEN", ["SO"] = "SOM", ["SR"] = "SUR", ["SS"] = "SSD", ["ST"] = "STP", ["SV"] = "SLV",
        ["SX"] = "SXM", ["SY"] = "SYR", ["SZ"] = "SWZ", ["TC"] = "TCA", ["TD"] = "TCD", ["TF"] = "ATF",
        ["TG"] = "TGO", ["TH"] = "THA", ["TJ"] = "TJK", ["TK"] = "TKL", ["TL"] = "TLS", ["TM"] = "TKM",
        ["TN"] = "TUN", ["TO"] = "TON", ["TR"] = "TUR", ["TT"] = "TTO", ["TV"] = "TUV", ["TW"] = "TWN",
        ["TZ"] = "TZA", ["UA"] = "UKR", ["UG"] = "UGA", ["UM"] = "UMI", ["US"] = "USA", ["UY"] = "URY",
        ["UZ"] = "UZB", ["VA"] = "VAT", ["VC"] = "VCT", ["VE"] = "VEN", ["VG"] = "VGB", ["VI"] = "VIR",
        ["VN"] = "VNM", ["VU"] = "VUT", ["WF"] = "WLF", ["WS"] = "WSM", ["YE"] = "YEM", ["YT"] = "MYT",
        ["ZA"] = "ZAF", ["ZM"] = "ZMB", ["ZW"] = "ZWE",
    };

    /// <summary>2 harfli kod eşlenebiliyor mu? (büyük/küçük harf ve boşluk tolere edilir)</summary>
    public static string? Esle(string? alpha2) =>
        alpha2 is { Length: > 0 } && Alpha2Alpha3.TryGetValue(alpha2.Trim().ToUpperInvariant(), out var a3) ? a3 : null;
}
