namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Günlük iş + Müşteri ve stok kalan 8 ekranın "Yıldız" metinleri (TR + EN).
//
// Kaynak (TEK DOĞRULUK): dokumanlar/09-uygulama-kayitlari/ham/yildiz-tuval/kaynak/
//   Y_Emanet · Y_Siparisvetamir · Y_Bildirimler · Y_Urunler · Y_Urunagaci · Y_Etiketler ·
//   Y_Fiyatlisteleri · Y_Sayim (.dc.html). Türkçe metin tuvaldeki AYNEN; tuval metni ürünün
//   gerçek davranışıyla çelişiyorsa (Anayasa md.1: ekran yalan söylemez) en yakın doğru cümle
//   yazılır ve PAKET-YILDIZ-5A raporunun "belirsiz eşleşme" tablosuna geçer.
// ⚠ Yalnız data-sema="yildiz" iken çizilen bileşenlerin metinleridir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Liste kalıbının ortak metinleri.</summary>
public static class YildizListeSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string ListeyeDon(string dil) => En(dil) ? "Back to list" : "Listeye dön";
    public static string TumAyrintilar(string dil) => En(dil) ? "All details" : "Tüm ayrıntılar";
    public static string Bugun(string dil) => En(dil) ? "Today" : "Bugün";
    public static string Dun(string dil) => En(dil) ? "Yesterday" : "Dün";
    public static string Hepsi(string dil) => En(dil) ? "All" : "Hepsi";
}

/// <summary>Emanet (tuval Y_Emanet).</summary>
public static class YildizEmanetSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Daily work · gold left with us, on record" : "Günlük iş · bırakılan altın, kayıt altında";
    public static string Baslik(string dil) => En(dil) ? "Safekeeping" : "Emanet";
    public static string AraYerTutucu(string dil) => En(dil) ? "Search customer" : "Müşteri ara";
    public static string EmanetAl(string dil) => En(dil) ? "Take safekeeping" : "Emanet al";
    public static string Emanette(string dil) => En(dil) ? "In safekeeping" : "Emanette";
    public static string GeriVerildi(string dil) => En(dil) ? "Returned" : "Geri verildi";
    public static string Musteri(string dil) => En(dil) ? "Customer" : "Müşteri";
    public static string NeBirakti(string dil) => En(dil) ? "What was left" : "Ne bıraktı";
    public static string Has(string dil) => En(dil) ? "Fine gold" : "Has";
    public static string NeZaman(string dil) => En(dil) ? "When" : "Ne zaman";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string GeriVer(string dil) => En(dil) ? "Return" : "Geri ver";
    /// <summary>Tuval "30 günü aştı".</summary>
    public static string GunuAsti(int gun, string dil) => En(dil) ? $"over {gun} days" : $"{gun} günü aştı";
    public static string Not(string dil) => En(dil)
        ? "Safekeeping does not mix with the customer's debt or credit; it stays on its own line. A wrong record is not deleted; it is reversed with a reason."
        : "Emanet, müşterinin borcuna ya da alacağına karışmaz; ayrı satırda durur. Yanlış kayıt silinmez, gerekçeyle geri alınır.";
    public static string Bos(string dil) => En(dil) ? "No gold in safekeeping." : "Emanette altın yok.";
    public static string SekmeBos(string dil) => En(dil) ? "Nothing in this tab." : "Bu sekmede kayıt yok.";
    public static string GeriVerildiKilit(string dil) => En(dil) ? "This safekeeping was returned." : "Bu emanet geri verildi.";
    /// <summary>UX-S29: okunan müşteri sınırı.</summary>
    public static string Sinir(int n, string dil) => En(dil)
        ? $"Safekeeping of the first {n} customers is shown; choose a customer for the others."
        : $"İlk {n} müşterinin emaneti gösteriliyor; ötekiler için müşteri seçin.";
    public static string Ayrinti(string ad, string dil) => En(dil) ? $"{ad} — safekeeping details" : $"{ad} — emanet ayrıntısı";
}

/// <summary>Sipariş ve tamir (tuval Y_Siparisvetamir).</summary>
public static class YildizSiparisSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Daily work · track until delivery" : "Günlük iş · teslime kadar izle";
    public static string AraYerTutucu(string dil) => En(dil) ? "Search customer or job" : "Müşteri ya da iş ara";
    public static string YeniIs(string dil) => En(dil) ? "New job" : "Yeni iş";
    public static string Acik(string dil) => En(dil) ? "Open" : "Açık";
    public static string Hazir(string dil) => En(dil) ? "Ready" : "Hazır";
    public static string TeslimEdildi(string dil) => En(dil) ? "Delivered" : "Teslim edildi";
    public static string Is(string dil) => En(dil) ? "Job" : "İş";
    public static string Musteri(string dil) => En(dil) ? "Customer" : "Müşteri";
    public static string Teslim(string dil) => En(dil) ? "Due" : "Teslim";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string TeslimEt(string dil) => En(dil) ? "Deliver" : "Teslim et";
    public static string TeslimKilit(string dil) => En(dil) ? "Not ready for delivery yet." : "Henüz teslime hazır değil.";
    public static string SekmeBos(string dil) => En(dil) ? "No jobs in this tab." : "Bu sekmede iş yok.";
    public static string AramaBos(string dil) => En(dil) ? "No job matches the search." : "Aramaya uyan iş yok.";
    public static string Ayrinti(string no, string dil) => En(dil) ? $"{no} — details and editing" : $"{no} — ayrıntı ve düzenleme";
}

/// <summary>Bildirimler (tuval Y_Bildirimler).</summary>
public static class YildizBildirimSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Daily work · everything waiting for you" : "Günlük iş · seni bekleyen her şey";
    public static string Okunmadi(string dil) => En(dil) ? "Unread" : "Okunmadı";
    public static string NeOldu(string dil) => En(dil) ? "What happened" : "Ne oldu";
    public static string Nereden(string dil) => En(dil) ? "From" : "Nereden";
    public static string NeZaman(string dil) => En(dil) ? "When" : "Ne zaman";
    public static string Git(string dil) => En(dil) ? "Go" : "Git";
    public static string GitKilit(string dil) => En(dil) ? "This notification has no screen to open." : "Bu bildirimin açılacak ekranı yok.";
    public static string Sistem(string dil) => En(dil) ? "System" : "Sistem";
    /// <summary>Tuval "Okunan bildirim kaybolmaz, soluklaşır." — sunucu kapalıyı N gün gösterir (doğru cümle).</summary>
    public static string Not(int gun, string dil) => En(dil)
        ? $"A read notification fades; it stays on the list for {gun} days."
        : $"Okunan bildirim soluklaşır; {gun} gün listede kalır.";
    public static string OkunduSay(string baslik, string dil) => En(dil) ? $"{baslik} — mark as read" : $"{baslik} — okundu say";
}

/// <summary>Ürünler (tuval Y_Urunler).</summary>
public static class YildizUrunSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Customers and stock · every piece in the display" : "Müşteri ve stok · vitrindeki her parça";
    public static string AraYerTutucu(string dil) => En(dil) ? "Product name or barcode" : "Ürün adı ya da barkod";
    public static string YeniUrun(string dil) => En(dil) ? "New product" : "Yeni ürün";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Barkod(string dil) => En(dil) ? "Barcode" : "Barkod";
    public static string Ayar(string dil) => En(dil) ? "Purity" : "Ayar";
    public static string Gram(string dil) => En(dil) ? "Gram" : "Gram";
    public static string Adet(string dil) => En(dil) ? "Qty" : "Adet";
    public static string Fiyat(string dil) => En(dil) ? "Price" : "Fiyat";
    public static string Not(string dil) => En(dil)
        ? "Prices are calculated from the current rate; they are recalculated at the moment of sale."
        : "Fiyatlar o anki kurdan hesaplanır; satış anında yeniden hesaplanır.";
    public static string Duzenle(string ad, string dil) => En(dil) ? $"{ad} — edit" : $"{ad} — düzenle";
}

/// <summary>Ürün ağacı (tuval Y_Urunagaci).</summary>
public static class YildizAgacSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Customers and stock · groups and subgroups" : "Müşteri ve stok · gruplar ve alt gruplar";
    public static string YeniGrup(string dil) => En(dil) ? "New group" : "Yeni grup";
    public static string Grup(string dil) => En(dil) ? "Group" : "Grup";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Adet(string dil) => En(dil) ? "Qty" : "Adet";
    public static string Has(string dil) => En(dil) ? "Fine gold" : "Has";
    /// <summary>Tuval "Gruba dokununca alt gruplar açılır; sürükleyerek taşınır." — üründe sürükleme yok (doğru cümle).</summary>
    public static string Not(string dil) => En(dil)
        ? "Tap a group to edit it; a subgroup is added by choosing its parent group."
        : "Gruba dokununca düzenlemesi açılır; alt grup, üst grubu seçilerek eklenir.";
    public static string Duzenle(string ad, string dil) => En(dil) ? $"{ad} — edit group" : $"{ad} — grubu düzenle";
    public static string Sinir(int n, string dil) => En(dil)
        ? $"Quantity and fine gold are summed over the first {n} products."
        : $"Adet ve has ilk {n} üründen toplanır.";
}

/// <summary>Etiketler (tuval Y_Etiketler).</summary>
public static class YildizEtiketSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Customers and stock · barcoded product label" : "Müşteri ve stok · barkodlu ürün etiketi";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string UrunSecin(string dil) => En(dil) ? "Choose a product" : "Ürün seçin";
    public static string DigerUrun(int n, string dil) => En(dil) ? $"+{n} {(n == 1 ? "product" : "products")}" : $"+{n} ürün";
    public static string Adet(string dil) => En(dil) ? "Quantity" : "Adet";
    public static string Etiket(string dil) => En(dil) ? "labels" : "etiket";
    public static string EtiketBoyu(string dil) => En(dil) ? "Label size" : "Etiket boyu";
    /// <summary>Ürünün tek etiket boyu (app.css .urun-etiket 50 × 30 mm; tuval "Küçük · 38 × 12 mm" ürünle çelişir).</summary>
    public static string Boy(string dil) => "50 × 30 mm";
    public static string NeYazsin(string dil) => En(dil) ? "What the label shows" : "Etikette ne yazsın";
    public static string Icerik(string dil) => En(dil) ? "Name · metal · purity · gram · fine · barcode" : "Ad · maden · ayar · gram · has · barkod";
    public static string FiyatYazilsin(string dil) => En(dil) ? "Print the price" : "Fiyat yazılsın mı";
    public static string IstegeBagli(string dil) => En(dil) ? "· optional" : "· isteğe bağlı";
    public static string Hayir(string dil) => En(dil) ? "No" : "Hayır";
    public static string KurEskir(string dil) => En(dil) ? "goes stale when the rate changes" : "kur değişince eskir";
    public static string BaskaUrun(string dil) => En(dil) ? "Add another product" : "Başka ürün ekle";
    public static string SecimiKapat(string dil) => En(dil) ? "Close the list" : "Listeyi kapat";
    public static string Yazdir(string dil) => En(dil) ? "Print" : "Yazdır";
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";
    public static string VazgecKilit(string dil) => En(dil) ? "Nothing is selected." : "Seçili ürün yok.";
    public static string Onizleme(string dil) => En(dil) ? "Preview" : "Önizleme";
    /// <summary>Tuval "Etiket yazıcısı: Ayarlar › Yazıcılar." — üründe yazıcı ayarı yok (doğru cümle).</summary>
    public static string YaziciNotu(string dil) => En(dil)
        ? "Labels are printed from the browser's print window. Paper is always printed white."
        : "Etiket, tarayıcının yazdırma penceresinden basılır. Kâğıt her zaman beyaz basılır.";
    public static string Ekle(string dil) => En(dil) ? "Add" : "Ekle";
    public static string Cikar(string dil) => En(dil) ? "Remove" : "Çıkar";
    public static string AraYerTutucu(string dil) => En(dil) ? "Product name or barcode" : "Ürün adı ya da barkod";
    public static string AramaBos(string dil) => En(dil) ? "No product matches the search." : "Aramaya uyan ürün yok.";
    public static string UrunYok(string dil) => En(dil) ? "There are no active products yet." : "Henüz etkin ürün yok.";
    public static string EtiketeDon(string dil) => En(dil) ? "Back to label" : "Etikete dön";
    public static string Sinir(int n, string dil) => En(dil)
        ? $"The first {n} products are shown; narrow down with the search."
        : $"İlk {n} ürün gösteriliyor; aramayla daraltın.";
    public static string OnizlemeBos(string dil) => En(dil) ? "Choose a product to see its label." : "Etiketi görmek için ürün seçin.";
}

/// <summary>Fiyat listeleri (tuval Y_Fiyatlisteleri).</summary>
public static class YildizFiyatSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Customers and stock · retail, wholesale, special" : "Müşteri ve stok · perakende, toptan, özel";
    public static string YeniListe(string dil) => En(dil) ? "New list" : "Yeni liste";
    public static string Liste(string dil) => En(dil) ? "List" : "Liste";
    public static string Iscilik(string dil) => En(dil) ? "Labour" : "İşçilik";
    public static string Kar(string dil) => En(dil) ? "Profit" : "Kâr";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Kullanim(string dil) => En(dil) ? "Used by" : "Kullanım";
    public static string Varsayilan(string dil) => En(dil) ? "Default" : "Varsayılan";
    public static string Toptanci(int n, string dil) => En(dil) ? $"{n} {(n == 1 ? "wholesaler" : "wholesalers")}" : $"{n} toptancı";
    public static string Musteri(int n, string dil) => En(dil) ? $"{n} {(n == 1 ? "customer" : "customers")}" : $"{n} müşteri";
    public static string Kullanilmiyor(string dil) => En(dil) ? "Not used" : "Kullanılmıyor";
    public static string Pasif(string dil) => En(dil) ? "Inactive" : "Kullanımda değil";
    public static string Duzenle(string dil) => En(dil) ? "Edit" : "Düzenle";
    /// <summary>Tuval "Satışta müşterinin listesi kendiliğinden seçilir; tezgâhtar değiştirebilir." — satışta liste seçimi yok (doğru cümle).</summary>
    public static string Not(string dil) => En(dil)
        ? "At sale the customer's list is chosen automatically; otherwise the default list applies."
        : "Satışta müşterinin listesi kendiliğinden seçilir; yoksa varsayılan liste geçer.";
    public static string Sinir(int n, string dil) => En(dil)
        ? $"Usage is counted over the first {n} customers."
        : $"Kullanım ilk {n} müşteriden sayılır.";
    public static string Etkisi(string ad, string dil) => En(dil) ? $"{ad} — effect on products" : $"{ad} — ürünlere etkisi";
}

/// <summary>Sayım (tuval Y_Sayim).</summary>
public static class YildizSayimSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Customers and stock · count the display, see the difference" : "Müşteri ve stok · vitrini say, fark görünsün";
    public static string Urun(string dil) => En(dil) ? "products" : "ürün";
    public static string UrunBaslik(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Kayit(string dil) => En(dil) ? "Record" : "Kayıt";
    public static string Sayilan(string dil) => En(dil) ? "Counted" : "Sayılan";
    public static string Fark(string dil) => En(dil) ? "Difference" : "Fark";
    public static string Bitir(string dil) => En(dil) ? "Finish count" : "Sayımı bitir";
    public static string BarkodOkut(string dil) => En(dil) ? "Scan barcode" : "Barkod okut";
    public static string Baslangic(string dil) => En(dil) ? "STARTED" : "BAŞLANGIÇ";
    public static string Sayan(string dil) => En(dil) ? "COUNTED BY" : "SAYAN";
    public static string HasFarki(string dil) => En(dil) ? "FINE GOLD DIFFERENCE" : "HAS FARKI";
    public static string Yeni(string dil) => En(dil) ? "Start a count" : "Sayım başlat";
    public static string AcikYok(string dil) => En(dil) ? "There is no open count. Start a count; the difference shows up as you scan." : "Açık sayım yok. Sayımı başlatın; okuttukça fark görünür.";
    public static string Gecmis(string dil) => En(dil) ? "Earlier counts" : "Önceki sayımlar";
    public static string Ad(string dil) => En(dil) ? "Count" : "Sayım";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string Ac(string ad, string dil) => En(dil) ? $"{ad} — open" : $"{ad} — aç";
    public static string Daha(int n, string dil) => En(dil) ? $"{n} more {(n == 1 ? "product" : "products")}" : $"{n} ürün daha";
    public static string SayimaDon(string dil) => En(dil) ? "Back to count" : "Sayıma dön";
    public static string OncekiSayimlar(string dil) => En(dil) ? "Earlier counts" : "Önceki sayımlar";
    public static string BitirKilit(string dil) => En(dil) ? "Scan at least one product first." : "Önce en az bir ürün okutun.";
    public static string GecmisYok(string dil) => En(dil) ? "No earlier counts." : "Önceki sayım yok.";

    private static readonly string[] Tr = ["sıfır", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz", "on"];
    private static readonly string[] Ing = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"];

    /// <summary>Tuval cümlesindeki sayı yazıyla ("İki ürün", "bir üründe"); 10'dan büyükse rakam.</summary>
    public static string Yazi(int n, string dil, bool buyukBas = false)
    {
        var s = n is >= 0 and <= 10 ? (En(dil) ? Ing[n] : Tr[n]) : n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!buyukBas || s.Length == 0 || char.IsDigit(s[0])) return s;
        var ilk = En(dil) ? char.ToUpperInvariant(s[0]) : char.ToUpper(s[0], System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        return ilk + s[1..];
    }

    /// <summary>Tuval "Sayım sürüyor. İki ürün henüz okutulmadı; bir üründe adet farkı var."</summary>
    public static string Cumle(string durum, int sayilmayan, int farkli, string dil)
    {
        if (durum == "KAPANDI") return En(dil) ? "The count is closed." : "Sayım kapandı.";
        if (durum == "IPTAL") return En(dil) ? "The count was cancelled." : "Sayım iptal edildi.";
        var bas = En(dil) ? "The count is in progress." : "Sayım sürüyor.";
        var a = sayilmayan > 0
            ? (En(dil) ? $" {Yazi(sayilmayan, dil, true)} {(sayilmayan == 1 ? "product has" : "products have")} not been scanned yet;"
                       : $" {Yazi(sayilmayan, dil, true)} ürün henüz okutulmadı;")
            : (En(dil) ? " Every product has been scanned;" : " Bütün ürünler okutuldu;");
        var b = farkli > 0
            ? (En(dil) ? $" {Yazi(farkli, dil)} {(farkli == 1 ? "product has" : "products have")} a quantity difference."
                       : $" {Yazi(farkli, dil)} üründe adet farkı var.")
            : (En(dil) ? " no quantity difference." : " adet farkı yok.");
        return bas + a + b;
    }
}

/// <summary>
/// YILDIZ-5A tur 2 (tasarım hakemi KRİTİK): birincil eylemlerin (Emanet al · Yeni iş · Yeni liste ·
/// Sayım başlat · Barkod okut) Yıldız form kalıbı metinleri. Kalıp tuval Y_Gidervevirman
/// ("… Ayrıntı ekle · Kaydet · Vazgeç · Not"); metinler o tuvalden AYNEN.
/// </summary>
public static class YildizForm5ASozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Kaydet(string dil) => En(dil) ? "Save" : "Kaydet";
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";
    public static string AyrintiEkle(string dil) => En(dil) ? "Add details" : "Ayrıntı ekle";
    public static string Not(string dil) => En(dil) ? "Note" : "Not";
    public static string Say(string dil) => En(dil) ? "Count" : "Say";
    public static string Tur(string dil) => En(dil) ? "Type" : "Tür";
    /// <summary>Sipariş formu notu: kaydedilen işin nerede göründüğü (ekranın gerçek davranışı).</summary>
    public static string SiparisNot(string dil) => En(dil)
        ? "A saved job appears under Open; once it is ready, Deliver opens."
        : "Kaydedilen iş Açık sekmesinde görünür; hazır olunca Teslim et açılır.";
    /// <summary>Sayım başlatma notu (sayfa kuralı: sayılmayan satır sıfırlanmaz).</summary>
    public static string SayimNot(string dil) => En(dil)
        ? "The difference shows up as you scan. An uncounted product is not set to zero."
        : "Okuttukça fark görünür. Sayılmayan ürün sıfırlanmaz.";
}
