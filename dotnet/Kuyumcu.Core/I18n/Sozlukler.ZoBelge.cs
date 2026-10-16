namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — FATURALAR · EVRAKLAR · RAPOR MERKEZİ · İŞLEM DEFTERİ ·
// KASA RAPORU · DENETİM İZİ · VERİ İÇE AKTARIM: "zorunlu önce" TR + EN METİNLER
//
// Kurucu 25.09: "ne işlem yapacaksa en basit haliyle, zorunlu olanlar hariç ilk
// etapta kullanıcı görmesin … isterse ekler ama eklemese bir kayıp yoksa açılır
// kapanır bir seçenek olarak kalsın". Bu dosya yalnız YENİ metinleri taşır;
// hub'a (Sozlukler.cs) dokunulmaz (orkestratör kuralı).
//
// Göz incelemesi (25.09) jargon bulguları: "Faz B" (#8), "KVKK md.12", "MASAK",
// "KURU PROVA" (#16) tezgâhtar ekranına sızıyordu. Sade karşılıkları burada;
// hukuki dayanak SİLİNMEZ — profesyonel görünümde ve ilgili ipucunda kalır.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer. Mevzuat hükmü / eşik sayısı bu dosyada YOKTUR.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>ZO-BELGE metinleri (bir dilde).</summary>
public sealed record ZoBelgeMetinleri(
    // ── Ortak ──
    string Suzgec,
    string SuzgecTemizle,
    string GecmisKayitlar,
    string YanaKaydir,
    string YenidenDene,
    // ── Faturalar ──
    string FaturaOzelMatrahBilgi,
    // ── Evraklar ──
    string EvrakDigerDurumlar,
    string EvrakEkBilgiler,
    string EvrakCiroNotu,
    // ── Kasa raporu ──
    string KasaAciklama,
    string KasaBugunBakiye,
    string KasaBugunSayildi,
    string KasaBugunSayilmadi,
    string KasaBugunYok,
    string KasaFarkliGun,
    string KasaSayimZorunlu,
    string KasaFarkGerekce,
    string KasaFarkGerekceIpucu,
    // ── İşlem defteri ──
    string IslemAciklama,
    string IslemOzet,
    string IslemStornoOzet,
    // ── Denetim izi ──
    string DenetimAciklama,
    string DenetimOzet,
    string DenetimYasalDayanak,
    // ── Rapor merkezi ──
    string RaporYil,
    string RaporAy,
    string RaporDuzeltmeBaslik,
    string GunSonuNotlari,
    string RaporTumRaporlar,
    string IslemOzetHas,
    string IslemDuzeltmeBaslik,
    // ── Veri içe aktarım ──
    string IceAciklama,
    string IceKapsamNotu,
    string IceSablonYuklenemedi,
    string IceGecmisYuklenemedi,
    // ── Rapor merkezi · reyon stok (doğrulayıcı 28.09: ürün metni rapora uymuyordu) ──
    string ReyonStokBos,

    // ── 29.09 devam 3 (doğrulayıcı bulguları) ──
    string BaglantiHatasi,
    string GunSonuHesapBaslik,
    string GunSonuFormul,

    // ── 29.09 devam 6 (tasarım hakemi bulguları) ──
    string YazdirHazirlaniyor,
    string YazdirAralik,
    string YazdirKayit,
    string YazdirSinir,
    string RaporKar,
    string RaporKarAyni,
    string Ipucu,
    string IceProvaDugme,
    string IceNasilHazirlarim,
    string FaturaKalemlerIstegeBagli,
    string FaturaKalemYokIpucu,
    string IslemKaydeden,
    string FistenFisEtiket,
    string FistenIpucu,
    string IslemCariKaydeden,
    // ZO-BELGE 29.09 (orkestratör 18:05): Fişten Fatura Kes penceresinde ham kod kalmaz.
    string FistenCariYok,
    string FistenZatenFaturali,
    string FistenKalemGenel,
    string FistenTaslakFaturali,
    string FistenAramaYok,
    string FistenKilitOnizle,
    string FistenKilitKalemYok,
    string FistenKilitFaturali);

/// <summary>ZO-BELGE sözlüğü.</summary>
public static class ZoBelgeSozlugu
{
    public static readonly ZoBelgeMetinleri Tr = new(
        Suzgec: "Süzgeç",
        SuzgecTemizle: "Süzgeci temizle",
        GecmisKayitlar: "Geçmiş kayıtlar",
        YanaKaydir: "Tablonun devamı için yana kaydırın",
        YenidenDene: "Yeniden dene",
        FaturaOzelMatrahBilgi: "Altın faturasındaki KDV kodu hakkında",
        EvrakDigerDurumlar: "Diğer durumlar",
        EvrakEkBilgiler: "Ek bilgiler (banka, keşide, borçlu, not)",
        EvrakCiroNotu: "Bu sürümde çek ve senet başka birine devredilemez (ciro yapılamaz).",
        KasaAciklama: "Her kasadaki para. Gün sonunda saydığınız parayı buradan girin.",
        KasaBugunBakiye: "Kasadaki para",
        KasaBugunSayildi: "Bugün sayıldı",
        KasaBugunSayilmadi: "Bugün sayılmadı",
        KasaBugunYok: "Bugün süzgecin dışında",
        KasaFarkliGun: "{0} günde sayım farkı var",
        KasaSayimZorunlu: "Saydığınız tutarı yazın.",
        KasaFarkGerekce: "Fark gerekçesi",
        KasaFarkGerekceIpucu: "Saydığınız tutar kasadaki tutarla tutmazsa yazılması gerekir.",
        IslemAciklama: "Seçtiğiniz günlerde yapılan bütün işlemler.",
        IslemOzet: "Giriş {0} · Çıkış {1} · {2} işlem",
        IslemStornoOzet: "{0} iptal kaydı ({1}) toplamlara katılmadı",
        DenetimAciklama: "Kim, ne zaman, hangi kaydı gördü ya da değiştirdi. Bu kayıtlar silinemez.",
        DenetimOzet: "{0} kayıt · {1} kişisel bilgi görüntüleme",
        DenetimYasalDayanak: "Yasal dayanak: Kişisel Verilerin Korunması Kanunu (KVKK md.12).",
        RaporYil: "Yıl",
        RaporAy: "Ay",
        RaporDuzeltmeBaslik: "Rapordaki bir rakam yanlışsa",
        GunSonuNotlari: "Ayrıntı",
        RaporTumRaporlar: "Tüm raporlar",
        IslemOzetHas: "Net altın",
        IslemDuzeltmeBaslik: "Listedeki bir işlem yanlışsa",
        IceAciklama: "Eski programınızdaki müşteri ve ürün kartlarını şablona yapıştırın. Önce deneme yapılır ve satır satır kontrol edilir; sonra tek tıkla uygulanır.",
        IceKapsamNotu: "Yalnız müşteri ve ürün kartları aktarılır. Geçmiş satış, fatura ve kasa hareketleri aktarılmaz: günlük toplamları ve yasal bildirim hesaplarını bozar.",
        IceSablonYuklenemedi: "Şablon bilgisi alınamadı. Aşağıdaki düğmeyle yeniden deneyin.",
        IceGecmisYuklenemedi: "Geçmiş alınamadı. Kartı kapatıp yeniden açarak ya da düğmeyle yeniden deneyin.",
        ReyonStokBos: "Bu seçimde stokta mal yok; reyonlara göre dağılım gösterilecek ürün bulunamadı.",
        BaglantiHatasi: "Sunucuya ulaşılamadı. Bağlantınızı kontrol edip sayfayı yenileyin.",
        GunSonuHesapBaslik: "Nasıl hesaplanır?",
        GunSonuFormul: "Fark = bugünkü satışların toplamı − satışa bağlı faturalar − satışa bağlanmamış faturalar.",
        YazdirHazirlaniyor: "Hazırlanıyor…",
        YazdirAralik: "Tarih aralığı: {0} – {1}",
        YazdirKayit: "{0} / {1} kayıt",
        YazdirSinir: "Kâğıda ilk {0} kayıt basıldı. Tamamı için tarih aralığını daraltın.",
        RaporKar: "Kâr",
        RaporKarAyni: "Bu ay satış ile alış arasındaki farkla aynı tutar.",
        Ipucu: "Açıklama",
        IceProvaDugme: "Önce kontrol et",
        IceNasilHazirlarim: "Dosyayı nasıl hazırlarım?",
        FaturaKalemlerIstegeBagli: "Kalemler (isteğe bağlı)",
        FaturaKalemYokIpucu: "Kalem eklemezseniz fatura yazdığınız toplam tutarla kesilir.",
        IslemKaydeden: "Kaydeden",
        FistenFisEtiket: "Fiş",
        FistenIpucu: "Fişin bütün kalemleri faturaya girer. Has bedeli fişten hesaplanır.",
        IslemCariKaydeden: "Müşteri · Kaydeden",
        FistenCariYok: "Müşteri yazılmamış",
        FistenZatenFaturali: "Bu fiş zaten faturalandı (Fatura No: {0}).",
        FistenKalemGenel: "Fiş kalemi",
        FistenTaslakFaturali: "Bu fiş taslak bir faturaya bağlı (numara kesimde verilir).",
        FistenAramaYok: "Bu adla eşleşen fiş yok. Fişteki müşterinin adını yazın.",
        FistenKilitOnizle: "Kaydetmek için önce fişi seçip Önizle'ye basın.",
        FistenKilitKalemYok: "Fişte faturaya girecek kalem yok.",
        FistenKilitFaturali: "Bu fiş zaten bir faturaya bağlı.");

    public static readonly ZoBelgeMetinleri En = new(
        Suzgec: "Filter",
        SuzgecTemizle: "Clear filter",
        GecmisKayitlar: "Past records",
        YanaKaydir: "Scroll sideways to see the rest of the table",
        YenidenDene: "Try again",
        FaturaOzelMatrahBilgi: "About the VAT code on gold invoices",
        EvrakDigerDurumlar: "Other statuses",
        EvrakEkBilgiler: "More details (bank, drawer, debtor, note)",
        EvrakCiroNotu: "In this release cheques and notes cannot be passed on to someone else (no endorsement).",
        KasaAciklama: "The money in each till. Enter your end-of-day count here.",
        KasaBugunBakiye: "Money in till",
        KasaBugunSayildi: "Counted today",
        KasaBugunSayilmadi: "Not counted today",
        KasaBugunYok: "Today is outside the filter",
        KasaFarkliGun: "Count difference on {0} day(s)",
        KasaSayimZorunlu: "Enter the amount you counted.",
        KasaFarkGerekce: "Reason for the difference",
        KasaFarkGerekceIpucu: "Required when the counted amount differs from the till balance.",
        IslemAciklama: "Every transaction on the days you choose.",
        IslemOzet: "In {0} · Out {1} · {2} transactions",
        IslemStornoOzet: "{0} cancellation record(s) ({1}) not included in totals",
        DenetimAciklama: "Who viewed or changed which record, and when. These records cannot be deleted.",
        DenetimOzet: "{0} records · {1} personal data views",
        DenetimYasalDayanak: "Legal basis: Turkish Personal Data Protection Law (KVKK art. 12).",
        RaporYil: "Year",
        RaporAy: "Month",
        RaporDuzeltmeBaslik: "If a figure in the report is wrong",
        GunSonuNotlari: "Details",
        RaporTumRaporlar: "All reports",
        IslemOzetHas: "Net gold",
        IslemDuzeltmeBaslik: "If a transaction in the list is wrong",
        IceAciklama: "Paste the customer and product cards from your old program into the template. A trial run checks every row first; then apply with one click.",
        IceKapsamNotu: "Only customer and product cards are imported. Past sales, invoices and till movements are not: they would distort daily totals and statutory reporting.",
        IceSablonYuklenemedi: "Could not load the template information. Try again with the button below.",
        IceGecmisYuklenemedi: "Could not load the history. Close and reopen the card, or use the button to try again.",
        ReyonStokBos: "There is no stock for this selection, so there is nothing to show by display unit.",
        BaglantiHatasi: "Could not reach the server. Check your connection and refresh the page.",
        GunSonuHesapBaslik: "How is this calculated?",
        GunSonuFormul: "Difference = total of today's sales − invoices linked to a sale − invoices not linked to a sale.",
        YazdirHazirlaniyor: "Preparing…",
        YazdirAralik: "Date range: {0} – {1}",
        YazdirKayit: "{0} / {1} records",
        YazdirSinir: "The first {0} records were printed. Narrow the date range to print all of them.",
        RaporKar: "Profit",
        RaporKarAyni: "Same amount as the difference between sales and purchases this month.",
        Ipucu: "Explanation",
        IceProvaDugme: "Check first",
        IceNasilHazirlarim: "How do I prepare the file?",
        FaturaKalemlerIstegeBagli: "Line items (optional)",
        FaturaKalemYokIpucu: "Without line items the invoice is issued for the total you entered.",
        IslemKaydeden: "Recorded by",
        FistenFisEtiket: "Receipt",
        FistenIpucu: "Every line of the receipt goes on the invoice. The gold value is taken from the receipt.",
        IslemCariKaydeden: "Customer · Recorded by",
        FistenCariYok: "No customer entered",
        FistenZatenFaturali: "This receipt is already invoiced (Invoice No: {0}).",
        FistenKalemGenel: "Receipt line",
        FistenTaslakFaturali: "This receipt is linked to a draft invoice (the number is assigned when issued).",
        FistenAramaYok: "No receipt matches this name. Type the customer name on the receipt.",
        FistenKilitOnizle: "To save, first pick the receipt and press Preview.",
        FistenKilitKalemYok: "The receipt has no lines to invoice.",
        FistenKilitFaturali: "This receipt is already linked to an invoice.");

    public static ZoBelgeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}

// ═══════════════════════════════════════════════════════════════════════════
// ZO-BELGE (tasarım hakemi t3, 29.09) — "Kim ne yaptı" BASİT yüzünde ham kayıt
// türü ("TelemetriOlcumu") ve teknik alan adı ("IslemMs, IstekSayisi") basılıyordu.
// Hub'daki VarlikAdlari'nda OLMAYAN türler ve güncelleme detayındaki alan adları
// burada TR+EN. Kayıt DEĞİŞMEZ; PRO yüz ham metni görür. Tanınmayan alan adı
// tahmin edilmez: sayılır ("+2 bilgi daha").
// ═══════════════════════════════════════════════════════════════════════════
public static class ZoBelgeDenetimSozlugu
{
    private static readonly Dictionary<string, (string Tr, string En)> Varliklar = new()
    {
        ["BankaHareketiDuzeltme"] = ("Banka hareketi düzeltmesi", "Bank transaction correction"),
        ["BankaHareketiStorno"] = ("Banka hareketi geri alma", "Bank transaction reversal"),
        ["CariIban"] = ("Müşteri IBAN", "Customer IBAN"),
        ["CiktiBelgesi"] = ("Çıktı belgesi", "Printout"),
        ["DestekPaketi"] = ("Destek paketi", "Support bundle"),
        ["DisaAktarim"] = ("Dışa aktarım", "Export"),
        ["EkstreIthalati"] = ("Banka ekstresi yükleme", "Bank statement import"),
        ["EkstreSatiri"] = ("Ekstre satırı", "Statement line"),
        ["EmanetHareketiDuzeltme"] = ("Emanet düzeltmesi", "Consignment correction"),
        ["Gider"] = ("Gider", "Expense"),
        ["GiderKategorisi"] = ("Gider türü", "Expense type"),
        ["Gorunum"] = ("Görünüm ayarı", "Display setting"),
        ["IceAktarim"] = ("İçe aktarım", "Import"),
        ["IS_ELLE_CALISTIRMA"] = ("Elle başlatılan iş", "Manually started job"),
        ["IsAyari"] = ("Otomatik iş ayarı", "Scheduled job setting"),
        ["KasaHareketi"] = ("Kasa hareketi", "Cash transaction"),
        ["KasaHareketiStorno"] = ("Kasa hareketi geri alma", "Cash transaction reversal"),
        ["KiraciAyari"] = ("İşletme ayarı", "Business setting"),
        ["KiraciDaveti"] = ("Kullanıcı daveti", "User invitation"),
        ["KiymetliEvrak"] = ("Çek / senet", "Cheque / note"),
        ["KullaniciGorunum"] = ("Kişisel görünüm", "Personal display"),
        ["KullaniciTercihi"] = ("Kişisel tercih", "Personal preference"),
        ["KurIptali"] = ("Kur iptali", "Rate cancellation"),
        ["OdemeKalemi"] = ("Ödeme", "Payment"),
        ["PanoKalemAyari"] = ("Ana sayfa ayarı", "Home page setting"),
        ["PosCihaz"] = ("POS cihazı", "POS device"),
        ["PosTahsilat"] = ("Kartla tahsilat", "Card collection"),
        ["SatinalmaSiparisi"] = ("Satın alma siparişi", "Purchase order"),
        ["SistemCarisi"] = ("Sistem müşterisi", "System customer"),
        ["StokPartiTuketimi"] = ("Stok çıkışı", "Stock usage"),
        ["StokYerlesimi"] = ("Stok yeri", "Stock location"),
        ["Storno"] = ("Geri alma", "Reversal"),
        ["TelemetriOlcumu"] = ("Hız ölçümü", "Speed measurement"),
        ["TenantKimlik"] = ("İşletme kimliği", "Business identity"),
        ["VezneFisi"] = ("Vezne fişi", "Teller slip"),
        ["VezneFisKalemi"] = ("Vezne fişi satırı", "Teller slip line"),
        ["YedekKaydi"] = ("Yedek", "Backup"),
        ["YedekTalebi"] = ("Yedek talebi", "Backup request"),
        ["YedekTatbikati"] = ("Yedekten geri yükleme denemesi", "Restore drill"),
    };

    // Yalnız zaman damgası olan alanlar: "neyin değiştiği" bilgisi taşımaz, basılmaz.
    private static readonly HashSet<string> Damgalar = ["Guncellendi", "Olusturuldu"];

    private static readonly Dictionary<string, (string Tr, string En)> Alanlar = new()
    {
        ["Ad"] = ("ad", "name"), ["Soyad"] = ("soyad", "surname"),
        ["Aciklama"] = ("açıklama", "description"), ["Notlar"] = ("notlar", "notes"),
        ["Telefon"] = ("telefon", "phone"), ["Adres"] = ("adres", "address"),
        ["VergiNo"] = ("vergi no", "tax no"), ["VergiDairesi"] = ("vergi dairesi", "tax office"),
        ["Aktif"] = ("aktif/pasif", "active/inactive"), ["Durum"] = ("durum", "status"),
        ["Deger"] = ("değer", "value"), ["Bakiye"] = ("bakiye", "balance"),
        ["TlBakiye"] = ("TL bakiye", "₺ balance"), ["HasGramBakiye"] = ("has bakiye", "fine gold balance"),
        ["SonIslemTarihi"] = ("son işlem tarihi", "last transaction date"),
        ["Adet"] = ("adet", "quantity"), ["AdetKalan"] = ("kalan adet", "remaining quantity"),
        ["ToplamHasGram"] = ("toplam has", "total fine gold"), ["GramAgirlik"] = ("gram", "weight"),
        ["HasGrami"] = ("has gram", "fine gram"), ["Toplam"] = ("toplam", "total"),
        ["ToplamTl"] = ("toplam TL", "total ₺"), ["AraToplam"] = ("ara toplam", "subtotal"),
        ["KdvTutar"] = ("KDV", "VAT"), ["FaturaNo"] = ("fatura no", "invoice no"),
        ["Tarih"] = ("tarih", "date"), ["IptalEdildi"] = ("iptal", "cancelled"),
        ["IptalGerekce"] = ("iptal gerekçesi", "cancel reason"), ["IptalTarihi"] = ("iptal tarihi", "cancel date"),
        ["Sayildi"] = ("sayıldı", "counted"), ["SayilanAdet"] = ("sayılan adet", "counted quantity"),
        ["Kapandi"] = ("kapandı", "closed"), ["KapatmaGerekcesi"] = ("kapatma gerekçesi", "closing reason"),
        ["Plan"] = ("paket", "plan"), ["Rol"] = ("rol", "role"),
        ["ArayuzModu"] = ("görünüm", "view mode"), ["VarsayilanArayuzModu"] = ("varsayılan görünüm", "default view mode"),
        ["YaziBoyutu"] = ("yazı boyutu", "text size"), ["Sema"] = ("renk şeması", "colour scheme"),
        ["TemaModu"] = ("açık/koyu", "light/dark"), ["KurKaynagi"] = ("kur kaynağı", "rate source"),
        ["HesapAdi"] = ("hesap adı", "account name"), ["BankaAdi"] = ("banka", "bank"),
        ["BankaSube"] = ("banka şubesi", "bank branch"), ["ParaBirimi"] = ("para birimi", "currency"),
        ["HasMarjYuzde"] = ("has kâr oranı", "fine gold margin"), ["IscilikCarpanYuzde"] = ("işçilik oranı", "labour rate"),
        ["KrediLimitiTl"] = ("borç sınırı", "credit limit"), ["MaliyetYontemi"] = ("maliyet yöntemi", "costing method"),
        ["Varsayilan"] = ("varsayılan", "default"), ["Sira"] = ("sıra", "order"),
        ["TahsilEdildi"] = ("tahsil edildi", "collected"), ["Veznelendi"] = ("vezneye geçti", "posted to teller"),
        ["GeriAlindi"] = ("geri alındı", "reversed"), ["Uygulandi"] = ("uygulandı", "applied"),
        ["SaticiAdres"] = ("satıcı adresi", "seller address"), ["MalAciklamasi"] = ("mal açıklaması", "goods description"),
        ["SozVerilenGun"] = ("söz verilen gün", "promised date"), ["ValorGun"] = ("valör günü", "value date"),
        ["Bitis"] = ("bitiş", "end"),
    };

    /// <summary>Hub'da adı olmayan kayıt türünün görünen adı; bilinmiyorsa null.</summary>
    public static string? VarlikAdi(string? kod, string dil) =>
        kod is not null && Varliklar.TryGetValue(kod, out var a) ? (dil == "en" ? a.En : a.Tr) : null;

    private static readonly System.Text.RegularExpressions.Regex AlanListesi =
        new(@"^[A-Z][A-Za-z]*(, [A-Z][A-Za-z]*)*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    // 29.09 doğrulayıcı ORTA: sunucunun okuma izi gerekçeleri ASCII kod adıdır
    // ("stok-yaslandirma", "vade-takibi", "pozisyon-cariler 9/2026"). Q10-5 çevirisi
    // tanımadığı kodu yalnız boşlukla ayırıyordu → BASİT ekranda "stok yaslandirma"
    // (ş/ı'sız). Sunucu metni kayıttır (denetçi ham kodu PRO'da görür, RaporSeti testleri
    // kodu arar) → düzeltme GÖSTERİMDE, Q10-5'ten ÖNCE, tam kod adı eşleşmesiyle.
    private static readonly (System.Text.RegularExpressions.Regex Kalip, string Tr, string En)[] KodAdlari =
    [
        (new(@"(?<![\p{L}-])stok-yaslandirma(?![\p{L}-])", System.Text.RegularExpressions.RegexOptions.CultureInvariant), "stok yaşlandırma", "stock ageing"),
        (new(@"(?<![\p{L}-])pozisyon-cariler(?![\p{L}-])", System.Text.RegularExpressions.RegexOptions.CultureInvariant), "müşteri pozisyonları", "customer positions"),
        (new(@"(?<![\p{L}-])vade-takibi(?![\p{L}-])", System.Text.RegularExpressions.RegexOptions.CultureInvariant), "vade takibi", "due date tracking"),
    ];

    /// <summary>Bilinen ASCII kod adlarını Türkçe (ya da İngilizce) okunur ada çevirir;
    /// tanınmayanlar olduğu gibi kalır (sonraki Q10-5 çevirisine bırakılır).</summary>
    public static string? KodAdlariniCevir(string? detay, string dil)
    {
        if (string.IsNullOrEmpty(detay)) return detay;
        var s = detay;
        foreach (var (kalip, tr, en) in KodAdlari) s = kalip.Replace(s, dil == "en" ? en : tr);
        return s;
    }

    /// <summary>
    /// Güncelleme detayı yalnız alan adı listesiyse ("Ad, Guncellendi, Telefon")
    /// sade metne çevirir: "Değişen: ad, telefon". Liste değilse null (dokunulmaz).
    /// </summary>
    public static string? AlanDegisikligi(string? detay, string dil)
    {
        if (string.IsNullOrEmpty(detay) || !AlanListesi.IsMatch(detay)) return null;
        var en = dil == "en";
        var adlar = new List<string>();
        var bilinmeyen = 0;
        foreach (var p in detay.Split(", "))
        {
            if (Damgalar.Contains(p)) continue;
            if (Alanlar.TryGetValue(p, out var a)) adlar.Add(en ? a.En : a.Tr);
            else bilinmeyen++;
        }
        if (adlar.Count == 0 && bilinmeyen == 0) return en ? "Record updated" : "Kayıt güncellendi";
        if (adlar.Count == 0) return en ? $"{bilinmeyen} details changed" : $"{bilinmeyen} bilgi değişti";
        var s = (en ? "Changed: " : "Değişen: ") + string.Join(", ", adlar);
        return bilinmeyen == 0 ? s : s + (en ? $" (+{bilinmeyen} more)" : $" (+{bilinmeyen} bilgi daha)");
    }

    // 29.09 doğrulayıcı KRİTİK: elle kasa hareketinin denetim metni sunucuda
    // "elle kasa hareketi: GIRIS 1500.50 TRY · açıklama: …" diye (ham yön kodu,
    // InvariantCulture sayı, "TRY" kodu) YAZILIR. Kayıt değişmez (PRO ve denetçi ham
    // metni görür); BASİT gösterimde yön sözlükten, tutar ekranın para biçiminden
    // (paraBicimi = Bicim.Para — Core, Web'deki Bicim'i tanımaz) geçer.
    // Storno varyantı: "elle kasa hareketi stornosu (kaynak <kimlik>) [· KAPALI GÜN STORNOSU] · gerekçe: …"
    // → ham kimlik BASİT'te basılmaz. Kalıba uymayan metin null döner (dokunulmaz).
    private static readonly System.Text.RegularExpressions.Regex ElleKasa =
        new(@"^elle kasa hareketi: (?<yon>GIRIS|CIKIS) (?<tutar>-?\d+(?:\.\d+)?) (?<pb>[A-Z]{3})(?<kalan> · .*)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Singleline);

    private static readonly System.Text.RegularExpressions.Regex ElleKasaStorno =
        new(@"^elle kasa hareketi stornosu \(kaynak [^)]*\)(?<kapali> · KAPALI GÜN STORNOSU)?(?<kalan> · .*)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Singleline);

    /// <summary>Elle kasa hareketi (ve stornosu) denetim metnini sade dile çevirir:
    /// "Kasaya elle giriş: 1.500,50 ₺ · açıklama: …". Kalıp tutmazsa null.</summary>
    public static string? KasaHareketiMetni(string? detay, string dil, Func<decimal, string, string> paraBicimi)
    {
        if (string.IsNullOrEmpty(detay)) return null;
        var en = dil == "en";
        var m = ElleKasa.Match(detay);
        if (m.Success)
        {
            if (!decimal.TryParse(m.Groups["tutar"].Value, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var tutar)) return null;
            var giris = m.Groups["yon"].Value == "GIRIS";
            var bas = giris ? (en ? "Cash in (manual)" : "Kasaya elle giriş")
                            : (en ? "Cash out (manual)" : "Kasadan elle çıkış");
            return $"{bas}: {paraBicimi(tutar, m.Groups["pb"].Value)}{KalanCevir(m.Groups["kalan"].Value, en)}";
        }
        var s = ElleKasaStorno.Match(detay);
        if (s.Success)
        {
            var bas = en ? "Manual cash entry reversed" : "Elle kasa hareketi geri alındı";
            var kapali = s.Groups["kapali"].Success ? (en ? " · closed day" : " · kapanmış gün") : "";
            return bas + kapali + KalanCevir(s.Groups["kalan"].Value, en);
        }
        return null;
    }

    // Sunucunun sabit Türkçe ekleri: " · açıklama: x", " · gerekçe: x" (kullanıcı metni aynen kalır).
    private static string KalanCevir(string kalan, bool en)
    {
        if (string.IsNullOrEmpty(kalan) || !en) return kalan;
        return kalan.Replace(" · açıklama: ", " · note: ").Replace(" · gerekçe: ", " · reason: ");
    }
}
