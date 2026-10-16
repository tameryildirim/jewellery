namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A — HURDA · POS TAHSİLAT · BANKA "ZORUNLU ÖNCE": TR + EN METİNLER
//
// Kurucu 25.09: "ne işlem yapacaksa en basit haliyle, zorunlu olanlar hariç ilk
// etapta kullanıcı görmesin … isterse ekler ama eklemese bir kayıp yoksa açılır
// kapanır bir seçenek olarak kalsın". Bu dosya yalnız YENİ metinleri taşır;
// hub'a (Sozlukler.cs) dokunulmaz (orkestratör kuralı).
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer. Mevzuat hükmü / eşik sayısı bu dosyada YOKTUR.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>ZO-ISLEM-A metinleri (bir dilde).</summary>
public sealed record ZoIslemMetinleri(
    // ── Ortak "+ ekle" düğmeleri ──
    string DetayEkle,
    string DetayGizle,
    // ── Hurda ──
    string HurdaFiyatOzet,
    string HurdaSubeYokSahip,
    // ── Banka ──
    string BankaDigerBilgiler,
    string BankaDekontDetay,
    // BASİT mod jargonsuz karşılıklar (K8: "cari", "kümülatif"); PRO metni HUB'da kalır.
    string BankaMusteriBasit,
    string BankaMusteriYokBasit,
    // ── 28.09 tasarım hakemi düzeltmeleri (jargonsuz tek cümle) ──
    string HurdaKabulIpucu,
    string PosKilitNotu,
    string PosBankaYatan,
    string PosOnceHareketSec,
    string PosOnceOneriGetir,
    string PosIptalKilit,
    string BankaKaldirKilit,
    // Hurda defteri kilit kutusu (BASİT): TEK sade cümle; {0} = satırdaki düzeltme düğmesinin adı.
    string HurdaDefterKilitBasit,
    // MASAK formu (Banka dekontu + İşlemler ortak bileşeni): "Uyruk (TUR)" ham kod etiketi yerine.
    string MasakUyruk,
    string MasakUyrukIpucu,
    // Belge türü T.C. kimlik değilken uyruk boş gelir (fail-closed) — bu ipucu seçmeyi ister.
    string MasakUyrukSecin,
    string MasakUyrukYerTutucu,
    // ── 28.09 (2. hakem turu) ──
    string Vazgec,
    string PosBankaBulunamadi,
    string BankaMasakKilit,
    string MasakBaslikBasit,
    string MasakKilitSebepBasit,
    string BankaMusteriIpucuBasit,
    string MasakUyrukDiger,
    string MasakUyrukKod,
    string MasakUyrukKodIpucu,
    // ── 28.09 (3. doğrulama DÜŞÜK) ──
    // Ülke kodu ISO 3166-1 alpha-3 listesinde değilse alan altı hata.
    string MasakUyrukKodGecersiz,
    // BASİT modda belge no etiketi belge türüne göre, kısaltmasız (TCKN yok).
    string MasakBelgeNoTc,
    string MasakBelgeNoPasaport,
    string MasakBelgeNoDiger,
    // ── 28.09 (4. doğrulama: tasarım hakemi KRİTİK) ──
    // Hurda: nakit bu tutarda alınamıyorsa kaydetmeden ÖNCE tek sade cümle (eşik sayısı YOK).
    string HurdaNakitAlinamaz,
    // POS: öneri bulunamazsa gerçek yol + elle seçim.
    string PosOneriYokElle,
    string PosElleBaslik,
    string PosElleSecilen,
    string PosElleSecin,
    string PosElleFazla,
    string PosElleAcikYok,
    string PosElleKismi,
    // ── 28.09 (7. doğrulama: eşleşmeyi ekrandan geri alma) ──
    string PosGeriAl,
    string PosGeriAlOnay,
    string PosGeriAlSecim,
    string PosGeriAlGerekce,
    string PosGeriAlGerekceKisa,
    string PosGeriAlYok,
    string PosGeriAlYetki,
    string PosGeriAlSecinKilit,
    string PosEslesmeOneriyle,
    string PosEslesmeElle,
    string PosIptalKilitEslesmis,
    string PosEslestiSonuc,
    string PosGeriAlSonuc,
    // ── 29.09 (tasarım hakemi KRİTİK): Hurda — tahsil aracı zorunlu, MASAK, düz dil ──
    string HurdaDuzeltCekmeceNot,
    string HurdaTahsilSecinKilit,
    string HurdaMasakGerekli,
    // ── 29.09 (tasarım hakemi KRİTİK): POS yatma adayı — hesap adı + kalan ──
    // 29.09 (hakem YÜKSEK): tutar BİRİNCİL (sonucun adı), hesap · tarih ikincil;
    // kalan yalnız kısmen eşleşmiş harekette, ikincil satırın sonunda yazılır.
    string PosAdayKalan,
    // ── 29.09 (tasarım hakemi YÜKSEK): POS eşleştirme sonrası · banka POS satırı ──
    string PosTamamiEslesti,
    string PosKalanVar,
    string BankaPosEslestir,
    // ── 29.09 (tasarım hakemi KRİTİK): ramatçı mesajı cümle · kur oynaması · tahsil aşımı ──
    // {0} = tahsil edilmeyen tutar, {1} = işlem tutarı (Bicim.Para ile biçimli).
    string HurdaRamatciGerekli,
    string HurdaTahsilAsim,
    string HurdaTahsilKurEsitlendi,
    // 29.09 (doğrulayıcı ORTA): eşitlenen FARK tutar olarak yazılır — {0} yazılan, {1} kaydedilecek, {2} işaretli fark.
    string HurdaTahsilKurFarki,
    string HurdaHasHaneSiniri);

/// <summary>ZO-ISLEM-A sözlüğü.</summary>
public static class ZoIslemSozlugu
{
    public static readonly ZoIslemMetinleri Tr = new(
        DetayEkle: "+ Alıcının kabulü, fiyat, ramatçı ve tahsilat ekle",
        DetayGizle: "Daha az göster",
        HurdaFiyatOzet: "Birim fiyat",
        HurdaSubeYokSahip: "Hesabınıza bir şube atanmamış. Kullanıcılar ekranından kendinize şube atayınca bu ekran açılır.",
        BankaDigerBilgiler: "Daha fazla (hesap sahibi)",
        BankaDekontDetay: "Daha fazla (tarih, müşteri, karşı taraf, açıklama)",
        BankaMusteriBasit: "Müşteri",
        BankaMusteriYokBasit: "Seçilmedi",
        HurdaKabulIpucu: "Alıcı daha az has saydıysa fark ayrıca kaydedilir.",
        PosKilitNotu: "Bu tahsilat satıştan gelir; yanlış eşleştirmeyi satırdaki ⋯ menüsünden Eşleşmeyi geri al ile, yanlış tahsilatı İptal ile ya da satışı iade ederek düzeltin.",
        PosBankaYatan: "Bankaya yatan tutar",
        PosOnceHareketSec: "Önce bankaya yatan tutarı seçin.",
        PosOnceOneriGetir: "Eşleştirmek için önce Öneri getir'e basın.",
        PosIptalKilit: "Yalnız bekleyen tahsilat iptal edilebilir.",
        BankaKaldirKilit: "Bakiyesi olan hesap kaldırılamaz; önce parayı başka hesaba aktarın.",
        HurdaDefterKilitBasit: "Kaydedilen hurda satışı değiştirilmez. Alıcıyla gram farkı çıkarsa satırdaki \"{0}\" ile ekleyin; para farkı için müşteri düzeltme fişini kullanın.",
        MasakUyruk: "Uyruk",
        MasakUyrukIpucu: "Listede yoksa \"Diğer ülke\"yi seçin.",
        MasakUyrukSecin: "Müşterinin uyruğunu seçin.",
        MasakUyrukYerTutucu: "Ülke seçin",
        Vazgec: "Vazgeç",
        PosBankaBulunamadi: "Bu tutarda banka hareketi bulunamadı.",
        BankaMasakKilit: "Kimlik bilgisi kaydedildikten sonra değiştirilemez; yanlışsa yeni işlemle doğrusunu kaydedin.",
        MasakBaslikBasit: "Kimlik bilgisi gerekli (tutar yüksek)",
        MasakKilitSebepBasit: "Kaydetmek için kimlik bilgilerini doldurun.",
        BankaMusteriIpucuBasit: "Müşteri seçerseniz bugünkü diğer işlemleriyle birlikte sayılır.",
        MasakUyrukDiger: "Diğer ülke",
        MasakUyrukKod: "Ülke kodu",
        MasakUyrukKodIpucu: "Pasaporttaki 3 harfli ülke kodu.",
        MasakUyrukKodGecersiz: "Bu kod bir ülke kodu değil. Pasaporttaki 3 harfli kodu yazın (örnek: FRA).",
        MasakBelgeNoTc: "T.C. kimlik numarası",
        MasakBelgeNoPasaport: "Pasaport numarası",
        MasakBelgeNoDiger: "Belge numarası",
        HurdaNakitAlinamaz: "Bu tutar nakit alınamaz; Havale ya da Kart seçin.",
        PosOneriYokElle: "Bu tutar bir günün toplamıyla tutmuyor. Bankanın yatırdığı tahsilatları aşağıdan işaretleyin.",
        PosElleBaslik: "Bankaya yatan tahsilatlar",
        PosElleSecilen: "Seçilen {0} · bankaya yatan {1}",
        PosElleSecin: "Eşleştirmek için aşağıdan en az bir tahsilat işaretleyin.",
        PosElleFazla: "Seçilen toplam bankaya yatandan {0} fazla; bir tahsilatın işaretini kaldırın.",
        PosElleAcikYok: "Bekleyen tahsilat yok.",
        PosElleKismi: "Bu tahsilattan {0} eşleşecek (bankaya yatanın kalanı kadar); geri kalanı açık kalır.",
        PosGeriAl: "Eşleşmeyi geri al",
        PosGeriAlOnay: "Seçilen eşleşme geri alınır; kayıt silinmez, izi kalır. Sonra tahsilatı doğru banka yatmasıyla yeniden eşleştirebilirsiniz.",
        PosGeriAlSecim: "Geri alınacak eşleşme",
        PosGeriAlGerekce: "Geri alma gerekçesi",
        PosGeriAlGerekceKisa: "Gerekçe en az 20 karakter olmalı ({0}/20).",
        PosGeriAlYok: "Bu tahsilatın geri alınacak eşleşmesi yok.",
        PosGeriAlYetki: "Eşleşmeyi yalnız müdür ya da işletme sahibi geri alabilir.",
        PosGeriAlSecinKilit: "Geri alınacak eşleşmeyi seçin.",
        PosEslesmeOneriyle: "Öneriyle eşleşti",
        PosEslesmeElle: "Elle eşleşti",
        PosIptalKilitEslesmis: "Bankaya eşleşmiş tahsilat iptal edilemez; önce bu menüden Eşleşmeyi geri al'ı kullanın.",
        PosEslestiSonuc: "{0} · {1} · {2} tahsilat",
        PosGeriAlSonuc: "{0} · {1}",
        HurdaDuzeltCekmeceNot: "Eski kayıt silinmez; girdiğiniz gram farkı kayda eklenir. Tahsil edilen para bu yolla değişmez.",
        HurdaTahsilSecinKilit: "Kaydetmek için tahsil aracını seçin.",
        HurdaMasakGerekli: "Bu tutarda alıcının kimlik bilgileri gerekiyor. Aşağıdaki alanları doldurup yeniden kaydedin.",
        PosAdayKalan: "kalan {0}",
        PosTamamiEslesti: "Bu yatmanın tamamı eşleşti.",
        PosKalanVar: "Bu yatmada eşleşmemiş {0} kaldı; yeniden seçip eşleştirebilirsiniz.",
        BankaPosEslestir: "POS tahsilatıyla eşleştir",
        HurdaRamatciGerekli: "Tahsil edilmeyen {0} için ramatçıyı seçin ya da tahsil edilen tutarı işlem tutarına ({1}) eşitleyin.",
        HurdaTahsilAsim: "Tahsil edilen tutar işlem tutarını ({1}) aşamaz; {0} fazla yazıldı.",
        HurdaTahsilKurEsitlendi: "Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi.",
        HurdaTahsilKurFarki: "Yazdığınız {0}, kaydedilecek {1} (fark {2}). Müşteriden alınan para farklıysa Vazgeç deyip tutarı düzeltin.",
        HurdaHasHaneSiniri: "Has gram en çok 6 ondalık hane alır (ör. 1,000001). Fazla haneyi silin.");

    public static readonly ZoIslemMetinleri En = new(
        DetayEkle: "+ Add buyer's accepted weight, price, refinery and payment",
        DetayGizle: "Show less",
        HurdaFiyatOzet: "Unit price",
        HurdaSubeYokSahip: "No branch is assigned to your account. Assign yourself a branch on the Users screen and this page will open.",
        BankaDigerBilgiler: "More (account holder)",
        BankaDekontDetay: "More (date, customer, counterparty, note)",
        BankaMusteriBasit: "Customer",
        BankaMusteriYokBasit: "Not selected",
        HurdaKabulIpucu: "If the buyer counts less fine gold, the difference is recorded separately.",
        PosKilitNotu: "This payment comes from a sale; fix a wrong match with Undo match in the row's ⋯ menu, and a wrong payment with Cancel or by returning the sale.",
        PosBankaYatan: "Amount paid into the bank",
        PosOnceHareketSec: "First pick the amount paid into the bank.",
        PosOnceOneriGetir: "Press Get suggestion before matching.",
        PosIptalKilit: "Only a pending payment can be cancelled.",
        BankaKaldirKilit: "An account with a balance cannot be removed; move the money to another account first.",
        HurdaDefterKilitBasit: "A saved scrap sale cannot be changed. If the buyer's weight differs, add it with \"{0}\" on its row; for money use a customer correction voucher.",
        MasakUyruk: "Nationality",
        MasakUyrukIpucu: "If it is not listed, pick \"Other country\".",
        MasakUyrukSecin: "Choose the customer's nationality.",
        MasakUyrukYerTutucu: "Choose a country",
        Vazgec: "Cancel",
        PosBankaBulunamadi: "No bank movement found for this amount.",
        BankaMasakKilit: "Identity details cannot be changed once saved; if wrong, record the correct ones with a new transaction.",
        MasakBaslikBasit: "Identity details needed (large amount)",
        MasakKilitSebepBasit: "Fill in the identity details to save.",
        BankaMusteriIpucuBasit: "If you pick a customer, it is counted together with their other deals today.",
        MasakUyrukDiger: "Other country",
        MasakUyrukKod: "Country code",
        MasakUyrukKodIpucu: "The 3-letter country code in the passport.",
        MasakUyrukKodGecersiz: "This is not a country code. Type the 3-letter code in the passport (e.g. FRA).",
        MasakBelgeNoTc: "Turkish ID number",
        MasakBelgeNoPasaport: "Passport number",
        MasakBelgeNoDiger: "Document number",
        HurdaNakitAlinamaz: "This amount cannot be taken in cash; choose Transfer or Card.",
        PosOneriYokElle: "This amount does not match a day's total. Tick the payments the bank paid in below.",
        PosElleBaslik: "Payments paid into the bank",
        PosElleSecilen: "Selected {0} · paid into the bank {1}",
        PosElleSecin: "Tick at least one payment below to match.",
        PosElleFazla: "The selected total is {0} more than the bank payment; untick a payment.",
        PosElleAcikYok: "No pending payments.",
        PosElleKismi: "{0} of this payment will be matched (up to what is left of the bank payment); the rest stays open.",
        PosGeriAl: "Undo match",
        PosGeriAlOnay: "The selected match is undone; the record is not deleted and its trace stays. You can then match the payment to the right bank payment again.",
        PosGeriAlSecim: "Match to undo",
        PosGeriAlGerekce: "Reason for undoing",
        PosGeriAlGerekceKisa: "The reason must be at least 20 characters ({0}/20).",
        PosGeriAlYok: "This payment has no match to undo.",
        PosGeriAlYetki: "Only a manager or the business owner can undo a match.",
        PosGeriAlSecinKilit: "Choose the match to undo.",
        PosEslesmeOneriyle: "Matched by suggestion",
        PosEslesmeElle: "Matched by hand",
        PosIptalKilitEslesmis: "A payment matched to the bank cannot be cancelled; first use Undo match in this menu.",
        PosEslestiSonuc: "{0} · {1} · {2} payment(s)",
        PosGeriAlSonuc: "{0} · {1}",
        HurdaDuzeltCekmeceNot: "The old record is not deleted; the weight difference you enter is added to it. The collected money does not change this way.",
        HurdaTahsilSecinKilit: "Choose how the payment is collected to save.",
        HurdaMasakGerekli: "This amount requires the buyer's identity details. Fill in the fields below and save again.",
        PosAdayKalan: "remaining {0}",
        PosTamamiEslesti: "This deposit is fully matched.",
        PosKalanVar: "{0} of this deposit is still unmatched; select it again to match.",
        BankaPosEslestir: "Match to card payment",
        HurdaRamatciGerekli: "Pick the refiner for the uncollected {0}, or make the collected amount equal to the total ({1}).",
        HurdaTahsilAsim: "The collected amount cannot exceed the total ({1}); {0} too much was entered.",
        HurdaTahsilKurEsitlendi: "The rate changed, so the collected amount was set to the current total.",
        HurdaTahsilKurFarki: "You entered {0}; {1} will be recorded (difference {2}). If the cash received differs, cancel and correct the amount.",
        HurdaHasHaneSiniri: "Fine gold grams take at most 6 decimal places (e.g. 1.000001). Remove the extra digits.");

    public static ZoIslemMetinleri Al(string dil) => dil == "en" ? En : Tr;
}

/// <summary>
/// ZO-ISLEM-A 28.09 (2. hakem turu) — MASAK uyruk seçicisinin ülke listesi.
/// <para>
/// Kod ISO 3166-1 alpha-3'tür (sunucu <c>MasakBilgiDto.Uyruk</c> ≤ 3 karakter,
/// risk motoru alpha-3 bekler); ad ekranda gösterilir, gövdeye KOD gider.
/// Liste bir mevzuat listesi DEĞİLDİR (yüksek riskli ülke listesi sunucuda,
/// yapılandırmadadır); yalnız sık görülen uyrukları tek dokunuşla seçtirir.
/// Listede olmayan uyruk için "Diğer ülke" seçilir ve 3 harfli kod yazılır —
/// kod ISO 3166-1 alpha-3 listesinde değilse (<see cref="KodGecerli"/>) alan altı hata
/// çıkar ve kayıt kilitlenir (28.09 3. doğrulama).
/// </para>
/// </summary>
public static class UlkeListesi
{
    /// <summary>"Diğer ülke" seçeneğinin değeri (gövdeye GİTMEZ; kod kutusu açılır).</summary>
    public const string Diger = "__DIGER__";

    public sealed record Ulke(string Kod, string Tr, string En);

    public static readonly IReadOnlyList<Ulke> Tumu =
    [
        new("TUR", "Türkiye", "Türkiye"),
        new("AZE", "Azerbaycan", "Azerbaijan"),
        new("DEU", "Almanya", "Germany"),
        new("RUS", "Rusya", "Russia"),
        new("UKR", "Ukrayna", "Ukraine"),
        new("GEO", "Gürcistan", "Georgia"),
        new("IRN", "İran", "Iran"),
        new("IRQ", "Irak", "Iraq"),
        new("SYR", "Suriye", "Syria"),
        new("BGR", "Bulgaristan", "Bulgaria"),
        new("GRC", "Yunanistan", "Greece"),
        new("CYP", "Kıbrıs", "Cyprus"),
        new("NLD", "Hollanda", "Netherlands"),
        new("FRA", "Fransa", "France"),
        new("AUT", "Avusturya", "Austria"),
        new("BEL", "Belçika", "Belgium"),
        new("GBR", "Birleşik Krallık", "United Kingdom"),
        new("USA", "Amerika Birleşik Devletleri", "United States"),
        new("SAU", "Suudi Arabistan", "Saudi Arabia"),
        new("ARE", "Birleşik Arap Emirlikleri", "United Arab Emirates"),
        new("QAT", "Katar", "Qatar"),
        new("KWT", "Kuveyt", "Kuwait"),
        new("JOR", "Ürdün", "Jordan"),
        new("LBN", "Lübnan", "Lebanon"),
        new("EGY", "Mısır", "Egypt"),
        new("LBY", "Libya", "Libya"),
        new("KAZ", "Kazakistan", "Kazakhstan"),
        new("UZB", "Özbekistan", "Uzbekistan"),
        new("TKM", "Türkmenistan", "Turkmenistan"),
        new("KGZ", "Kırgızistan", "Kyrgyzstan"),
        new("AFG", "Afganistan", "Afghanistan"),
        new("PAK", "Pakistan", "Pakistan"),
    ];

    /// <summary>Kod listede mi? (listede değilse ekran "Diğer ülke" + kod kutusu gösterir)</summary>
    public static bool Listede(string? kod) =>
        kod is { Length: > 0 } && Tumu.Any(u => u.Kod == kod.Trim().ToUpperInvariant());

    /// <summary>
    /// 28.09 (3. doğrulama DÜŞÜK — "Diğer ülke" kodu doğrulanmıyordu): ISO 3166-1 alpha-3
    /// kodlarının TAMAMI (249). Kaynak: IANA tz veritabanı <c>iso3166.tab</c> (ISO 3166-1
    /// alpha-2 listesi, 249 satır) → .NET/ICU <c>RegionInfo.ThreeLetterISORegionName</c>
    /// ile üretildi (ICU'nun tanımadığı EH → ESH elle). Pasaportlarda kullanılan ICAO
    /// Doc 9303 özel kodları (vatansız XXA, mülteci XXB/XXC, belirsiz XXX, BM UNO/UNA/UNK)
    /// ayrıca kabul edilir. Mevzuat listesi DEĞİLDİR — yalnız yazım hatasını yakalar.
    /// </summary>
    public static readonly IReadOnlySet<string> IsoKodlari = new HashSet<string>(StringComparer.Ordinal)
    {
        "ABW", "AFG", "AGO", "AIA", "ALA", "ALB", "AND", "ARE", "ARG", "ARM", "ASM", "ATA", "ATF", "ATG", "AUS", "AUT",
        "AZE", "BDI", "BEL", "BEN", "BES", "BFA", "BGD", "BGR", "BHR", "BHS", "BIH", "BLM", "BLR", "BLZ", "BMU", "BOL",
        "BRA", "BRB", "BRN", "BTN", "BVT", "BWA", "CAF", "CAN", "CCK", "CHE", "CHL", "CHN", "CIV", "CMR", "COD", "COG",
        "COK", "COL", "COM", "CPV", "CRI", "CUB", "CUW", "CXR", "CYM", "CYP", "CZE", "DEU", "DJI", "DMA", "DNK", "DOM",
        "DZA", "ECU", "EGY", "ERI", "ESH", "ESP", "EST", "ETH", "FIN", "FJI", "FLK", "FRA", "FRO", "FSM", "GAB", "GBR",
        "GEO", "GGY", "GHA", "GIB", "GIN", "GLP", "GMB", "GNB", "GNQ", "GRC", "GRD", "GRL", "GTM", "GUF", "GUM", "GUY",
        "HKG", "HMD", "HND", "HRV", "HTI", "HUN", "IDN", "IMN", "IND", "IOT", "IRL", "IRN", "IRQ", "ISL", "ISR", "ITA",
        "JAM", "JEY", "JOR", "JPN", "KAZ", "KEN", "KGZ", "KHM", "KIR", "KNA", "KOR", "KWT", "LAO", "LBN", "LBR", "LBY",
        "LCA", "LIE", "LKA", "LSO", "LTU", "LUX", "LVA", "MAC", "MAF", "MAR", "MCO", "MDA", "MDG", "MDV", "MEX", "MHL",
        "MKD", "MLI", "MLT", "MMR", "MNE", "MNG", "MNP", "MOZ", "MRT", "MSR", "MTQ", "MUS", "MWI", "MYS", "MYT", "NAM",
        "NCL", "NER", "NFK", "NGA", "NIC", "NIU", "NLD", "NOR", "NPL", "NRU", "NZL", "OMN", "PAK", "PAN", "PCN", "PER",
        "PHL", "PLW", "PNG", "POL", "PRI", "PRK", "PRT", "PRY", "PSE", "PYF", "QAT", "REU", "ROU", "RUS", "RWA", "SAU",
        "SDN", "SEN", "SGP", "SGS", "SHN", "SJM", "SLB", "SLE", "SLV", "SMR", "SOM", "SPM", "SRB", "SSD", "STP", "SUR",
        "SVK", "SVN", "SWE", "SWZ", "SXM", "SYC", "SYR", "TCA", "TCD", "TGO", "THA", "TJK", "TKL", "TKM", "TLS", "TON",
        "TTO", "TUN", "TUR", "TUV", "TWN", "TZA", "UGA", "UKR", "UMI", "URY", "USA", "UZB", "VAT", "VCT", "VEN", "VGB",
        "VIR", "VNM", "VUT", "WLF", "WSM", "YEM", "ZAF", "ZMB", "ZWE",
        "XXA", "XXB", "XXC", "XXX", "UNO", "UNA", "UNK",
        // 28.09 (doğrulayıcı DÜŞÜK): Kosova pasaportları ISO 3166 dışı "RKS" kodunu taşır.
        // Birincil kaynak: AB Konseyi PRADO kaydı RKS-AO-03001 (olağan pasaport, 2013–)
        // https://www.consilium.europa.eu/prado/en/RKS-AO-03001/index.html. "XKX" pasaportta
        // doğrulanamadı → eklenmedi.
        "RKS",
    };

    /// <summary>Uyruk kodu geçerli mi? Boş = girilmemiş (geçerli sayılır; zorunluluğu sunucu söyler).</summary>
    public static bool KodGecerli(string? kod) =>
        string.IsNullOrWhiteSpace(kod) || IsoKodlari.Contains(kod.Trim().ToUpperInvariant());
}
