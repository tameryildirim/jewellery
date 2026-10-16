namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-2 — "Yıldız" KABUĞU METİNLERİ (TR + EN)
//
// Kaynak: dokumanlar/09-uygulama-kayitlari/TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md § 4 D
//         ve tuval ham/yildiz-tuval/kaynak/Tum4.dc.html (39 ekran açıklaması).
//
// ⚠ Yalnız data-sema="yildiz" iken çizilen üst menü + "Tüm ekranlar" karelerinin
//   metinleridir. Diğer 8 şemanın hiçbir metni buradan gelmez.
// ⚠ Ekran adları BURADA YAZILMAZ: karo adları MenuAdlari'ndan (kenar çubuğuyla
//   aynı kaynak, UX-S31); burada yalnız kabuk etiketleri ve hover açıklamaları var.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yıldız kabuğu metinleri — dil kodu "tr" | "en".</summary>
public static class YildizSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Yıldız şemasının kod adı (tema.js / sunucu beyaz listesiyle aynı).</summary>
    public const string SemaKodu = "yildiz";

    public static string UstMenuEtiketi(string dil) => En(dil) ? "Main menu" : "Ana menü";

    // Beş üst menü kelimesi (tuval Bugun4.dc.html <nav>).
    public static string Bugun(string dil) => En(dil) ? "Today" : "Bugün";
    public static string Tezgah(string dil) => En(dil) ? "Counter" : "Tezgâh";
    public static string Musteriler(string dil) => En(dil) ? "Customers" : "Müşteriler";
    public static string Kasa(string dil) => En(dil) ? "Cash" : "Kasa";
    public static string TumEkranlar(string dil) => En(dil) ? "All screens" : "Tüm ekranlar";

    /// <summary>375 px'te beş kelimenin yerine geçen tek düğme.</summary>
    public static string MenuAc(string dil) => En(dil) ? "Menu" : "Menü";
    public static string MenuKapat(string dil) => En(dil) ? "Close menu" : "Menüyü kapat";

    // Sağ küme (tuval: HAS · 22 AYAR · saat · kullanıcı).
    public static string Has(string dil) => En(dil) ? "GOLD" : "HAS";
    public static string Ayar22(string dil) => En(dil) ? "22K" : "22 AYAR";
    public static string KurYok(string dil) => En(dil) ? "Rate unavailable" : "Kur yok";
    public static string KurEkraniAc(string dil) => En(dil) ? "Open rates" : "Kur ekranını aç";
    public static string KullaniciMenusuAc(string dil) => En(dil) ? "Account menu" : "Hesap menüsü";
    public static string KullaniciMenusu(string dil) => En(dil) ? "Account" : "Hesap";
    public static string DilEtiketi(string dil) => En(dil) ? "Language" : "Dil";

    /// <summary>
    /// "Tüm ekranlar" üst satırı (tuval: "39 EKRAN · 6 BÖLÜM · HİÇBİRİ GİZLİ DEĞİL").
    /// Yetkiye göre süzülmüş sayfada "hiçbiri gizli değil" YALAN olurdu: toplam
    /// ekran sayısına eşit değilse o parça YAZILMAZ (YILDIZ-4B: YILDIZ-2 hakem bulgusu —
    /// tuvalde olmayan "yetkin olanlar" ifadesi kalktı; en yakın tuval kalıbı = sayılar).
    /// </summary>
    public static string Ozet(string dil, int ekran, int bolum, bool hepsiGorunur) => En(dil)
        ? $"{ekran} screens · {bolum} sections" + (hepsiGorunur ? " · none hidden" : "")
        : $"{ekran} ekran · {bolum} bölüm" + (hepsiGorunur ? " · hiçbiri gizli değil" : "");

    /// <summary>Karenin üstüne gelince çıkan tek cümle (tuval .kac). Yol → açıklama.</summary>
    public static string Aciklama(string yol, string dil) =>
        (En(dil) ? AciklamaEn : AciklamaTr).TryGetValue(yol, out var m) ? m : "";

    /// <summary>Açıklaması olan rota sayısı — menü tanımıyla eşitliği testle kilitlidir.</summary>
    public static IReadOnlyCollection<string> AciklamaYollari => AciklamaTr.Keys;

    private static readonly Dictionary<string, string> AciklamaTr = new(StringComparer.Ordinal)
    {
        ["/"] = "Günün özeti: ne sattım, kasada ne var, kim borçlu.",
        ["/islemler"] = "Satış, alış, tahsilat, iade ve değişim tek fişte.",
        ["/emanet"] = "Müşterinin bıraktığı altını kaydet, sonra geri ver.",
        ["/siparisler"] = "Tamire ya da siparişe gelen işi teslime kadar izle.",
        ["/bildirimler"] = "Vadesi gelen ve bekleyen işler tek listede.",
        ["/cariler"] = "Kim borçlu, kime borçluyuz; tahsilat buradan.",
        ["/urunler"] = "Vitrindeki her ürün: ayar, gram, fiyat, adet.",
        ["/urunler/agac"] = "Ürünleri gruplara ayır: bilezik, kolye, sarrafiye.",
        ["/urunler/etiketler"] = "Barkodlu ürün etiketi hazırla ve bas.",
        ["/fiyatlandirma"] = "Perakende ve toptan fiyatlarını ayarla.",
        ["/sayim"] = "Vitrini say; eksik ve fazla hemen görünsün.",
        ["/satin-alma"] = "Tedarikçiye sipariş ver, gelen malı stoğa al.",
        ["/kasalar"] = "Her kasada ne var; günü buradan kapat.",
        ["/vezne"] = "Ödemesi bekleyen fişler sırayla.",
        ["/pos"] = "Kart makinelerini tanımla.",
        ["/pos/tahsilatlar"] = "Karttan bankaya geçecek paralar.",
        ["/banka"] = "Hesaplar, havale ve EFT hareketleri.",
        ["/evraklar"] = "Alınan ve verilen çekler, senetler.",
        ["/evraklar/takvim"] = "Hangi gün kimden ne alınacak.",
        ["/kur"] = "Altın ve döviz fiyatları, anlık.",
        ["/gider"] = "İşletme giderleri ve kasalar arası para aktarma.",
        ["/hurda"] = "Biriken eski altın ve rafineriye gönderim.",
        ["/faturalar"] = "Kesilen ve gelen faturalar.",
        ["/irsaliyeler"] = "Gönderilen ve teslim alınan malın irsaliyesi.",
        ["/gider-pusulalari"] = "Vatandaştan alınan altının yasal pusulası.",
        ["/uyum"] = "Yüksek tutarlı işlemlerin yasal bildirimi.",
        ["/yasal-sinirlar"] = "Nakit sınırı ve kimlik kuralları.",
        ["/raporlar"] = "Bütün raporlar tek yerde.",
        ["/raporlar/kar-zarar"] = "Ne kazandım, ne harcadım.",
        ["/raporlar/vade-takibi"] = "Vadesi geçen ve yaklaşan borçlar.",
        ["/raporlar/islem-defteri"] = "Bütün fişler, tarih sırasıyla.",
        ["/raporlar/satis-analizi"] = "En çok ne satıyor, ne zaman satıyor.",
        ["/raporlar/kasa"] = "Kasaya ne girdi, ne çıktı.",
        ["/kullanicilar"] = "Çalışanları ekle, giriş bilgisi ver.",
        ["/isletme"] = "İşletme adı, vergi numarası, adres.",
        ["/yetkiler"] = "Kim neyi görebilir, neyi yapabilir.",
        ["/denetim-izi"] = "Hangi çalışan ne zaman ne yaptı.",
        ["/kurulum"] = "İlk açılışta yapılacak ayarlar, adım adım.",
        ["/veri-disa-aktarim"] = "Kayıtları Excel dosyasına al.",
        ["/veri-ice-aktarim"] = "Eski programdaki kayıtları getir.",
        ["/yedekler"] = "Kayıtların yedeği ve geri yükleme.",
    };

    private static readonly Dictionary<string, string> AciklamaEn = new(StringComparer.Ordinal)
    {
        ["/"] = "Today at a glance: what I sold, what is in the till, who owes.",
        ["/islemler"] = "Sale, purchase, collection, return and exchange on one slip.",
        ["/emanet"] = "Record the gold a customer leaves, hand it back later.",
        ["/siparisler"] = "Follow a repair or an order until it is delivered.",
        ["/bildirimler"] = "Everything due or waiting, in one list.",
        ["/cariler"] = "Who owes, whom we owe; collect payments here.",
        ["/urunler"] = "Every item in the showcase: carat, grams, price, count.",
        ["/urunler/agac"] = "Group items: bracelet, necklace, bullion.",
        ["/urunler/etiketler"] = "Prepare and print barcode item labels.",
        ["/fiyatlandirma"] = "Set your retail and wholesale prices.",
        ["/sayim"] = "Count the showcase; shortages and surpluses show at once.",
        ["/satin-alma"] = "Order from suppliers and receive the goods into stock.",
        ["/kasalar"] = "What is in each till; close the day here.",
        ["/vezne"] = "Slips waiting for payment, in order.",
        ["/pos"] = "Define your card machines.",
        ["/pos/tahsilatlar"] = "Card money on its way to the bank.",
        ["/banka"] = "Accounts, transfers and EFT movements.",
        ["/evraklar"] = "Cheques and notes received and issued.",
        ["/evraklar/takvim"] = "Which day, who pays what.",
        ["/kur"] = "Gold and currency prices, live.",
        ["/gider"] = "Shop expenses and transfers between tills.",
        ["/hurda"] = "Accumulated scrap gold and shipments to the refinery.",
        ["/faturalar"] = "Invoices issued and received.",
        ["/irsaliyeler"] = "Waybills for goods sent and received.",
        ["/gider-pusulalari"] = "The legal slip for gold bought from a private person.",
        ["/uyum"] = "Legal reporting of high-value transactions.",
        ["/yasal-sinirlar"] = "Cash limit and identity rules.",
        ["/raporlar"] = "All reports in one place.",
        ["/raporlar/kar-zarar"] = "What I earned, what I spent.",
        ["/raporlar/vade-takibi"] = "Overdue and upcoming debts.",
        ["/raporlar/islem-defteri"] = "Every slip, by date.",
        ["/raporlar/satis-analizi"] = "What sells most, and when.",
        ["/raporlar/kasa"] = "What went into and out of the till.",
        ["/kullanicilar"] = "Add staff, give them a login.",
        ["/isletme"] = "Shop name, tax number, address.",
        ["/yetkiler"] = "Who may see what, who may do what.",
        ["/denetim-izi"] = "Which staff member did what, and when.",
        ["/kurulum"] = "First-run settings, step by step.",
        ["/veri-disa-aktarim"] = "Take your records out as an Excel file.",
        ["/veri-ice-aktarim"] = "Bring in records from your old program.",
        ["/yedekler"] = "Backups of your records, and restore.",
    };
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4A — "Yıldız" BUGÜN EKRANI METİNLERİ (tuval Bugun4 / Bugun4a, BİREBİR)
//
// Tuvaldeki yazım AYNEN korunur (kurucu 09.10: "birebir … istisnasız"): küçük
// harfli "biz borçluyuz", "Günü kapat", "maliyet eksik"; büyük harf (ÜST SATIR,
// ALTIN POZİSYONU …) CSS text-transform ile yapılır — metin cümle düzenindedir.
// ⚠ Yalnız data-sema="yildiz" iken çizilen YildizBugun bileşeninin metinleridir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yıldız Bugün metinleri — dil kodu "tr" | "en".</summary>
public static class YildizBugunSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Sağ liste başlığı (tuval ".ust": "Seni bekleyen").</summary>
    public static string SeniBekleyen(string dil) => En(dil) ? "Waiting for you" : "Seni bekleyen";
    /// <summary>İkincil eylem (tuval .b2): Kasalar ekranında günü kapatma.</summary>
    public static string GunuKapat(string dil) => En(dil) ? "Close the day" : "Günü kapat";

    // Cümle (tuval: "Bugün 11 satış. Kasada 230.356,54 ₺ nakit. Alacağın 32.654,79 ₺, borcun 3.840 ₺.").
    public static string CumleSatis(int adet, string dil) => En(dil)
        ? (adet == 0 ? "No sales today." : $"Today's sales · {adet} {(adet == 1 ? "sale" : "sales")}.")
        : (adet == 0 ? "Bugün satış yok." : $"Bugünkü satış · {adet} işlem."); // P-DIL-BORC-KASA (Y8): büyük rakamın etiketi
    /// <summary>Nakit ayrımı okunduysa "nakit" denir; okunamadıysa kasa toplamı "nakit" diye BASILMAZ.</summary>
    public static string CumleKasa(string tutar, bool nakit, string dil) => En(dil)
        ? (nakit ? $"{tutar} cash in the till." : $"{tutar} in the till.")
        : (nakit ? $"Kasada {tutar} nakit." : $"Kasada {tutar}.");
    /// <summary>P-DIL-BORC-KASA (Y7): tek kalıp — BorcDiliSozlugu.Toplam.</summary>
    public static string CumleCari(string alacak, string borc, string dil) => BorcDiliSozlugu.Toplam(alacak, borc, dil);

    // Liste satırları.
    public static string BizBorcluyuz(string dil) => BorcDiliSozlugu.KisaBizBorcluyuz(dil); // P-DIL-BORC-KASA: tek kaynak
    /// <summary>Vadesine kalan gün (tuval "15 gün" — aynı TUNALILAR verisinde vadeye 15 gün kalan alacak).</summary>
    public static string Gun(int gun, string dil) => En(dil) ? $"{gun} {(gun == 1 ? "day" : "days")}" : $"{gun} gün";
    /// <summary>Vadesi GEÇMİŞ alacak (tuvalde örneği yok — "belirsiz eşleşme": gün sayısı + "geçti").</summary>
    public static string GunGecti(int gun, string dil) => En(dil) ? $"{gun} {(gun == 1 ? "day" : "days")} overdue" : $"{gun} gün geçti";
    /// <summary>İş satırının tek eylemi (tuval "Aç").</summary>
    public static string Ac(string dil) => En(dil) ? "Open" : "Aç";

    // Alt şerit (tuval: "ALTIN POZİSYONU +157,3 g has · VİTRİN 157,3 g · KÂR maliyet eksik").
    public static string AltinPozisyonu(string dil) => En(dil) ? "Gold position" : "Altın pozisyonu";
    public static string Vitrin(string dil) => En(dil) ? "Display" : "Vitrin";
    public static string Kar(string dil) => En(dil) ? "Profit" : "Kâr";
    public static string MaliyetEksik(string dil) => En(dil) ? "cost missing" : "maliyet eksik";
    /// <summary>Has birimi (tuval "g has").</summary>
    public static string GramHas(string dil) => En(dil) ? "g fine" : "g has";

    // Kullanıcı menüsüne taşınan Basit/Profesyonel anahtarı (tuvalde üst barda anahtar YOK).
    public static string Gorunum(string dil) => En(dil) ? "View" : "Görünüm";

    // ── PAKET YILDIZ-4C — tuval Bugun4'ün iki satırı ──
    /// <summary>Tuval "Dünkü gün sonu kapatılmadı".</summary>
    public static string DunGunSonu(string dil) => En(dil) ? "Yesterday's day-end was not closed" : "Dünkü gün sonu kapatılmadı";
    /// <summary>Satırın kaynağı (tuval "Kasa").</summary>
    public static string KaynakKasa(string dil) => En(dil) ? "Till" : "Kasa";
    /// <summary>Satırın eylemi (tuval "Kapat") — gün sonu Kasalar ekranındadır.</summary>
    public static string Kapat(string dil) => En(dil) ? "Close" : "Kapat";
    /// <summary>İpucu: hangi kasalar (title).</summary>
    public static string DunGunSonuIpucu(string kasalar, string dil) => En(dil)
        ? $"Tills with activity yesterday and no day-end count: {kasalar}. Day-end is done on the Tills screen."
        : $"Dün hareketi olup gün sonu sayımı yapılmayan kasalar: {kasalar}. Gün sonu Kasalar ekranında yapılır.";
    /// <summary>Tuval "Derya Yıldızlı'nın emaneti" — iyelik eki Bicim'den (YildizBugunBicim.Iyelik).</summary>
    public static string Emaneti(string ad, string? iyelikli, string dil) => En(dil)
        ? $"{ad}'s safekeeping"
        : iyelikli is { } i ? $"{i} emaneti" : $"{ad} · emanet";
    /// <summary>Tuval "30 günü aştı" (eşik ayar sabitinden).</summary>
    public static string EmanetAsti(int esik, string dil) => En(dil) ? $"over {esik} days" : $"{esik} günü aştı";
    /// <summary>İpucu: emanet kaç gündür duruyor.</summary>
    public static string EmanetIpucu(int gun, string dil) => En(dil) ? $"Held for {gun} days." : $"{gun} gündür emanette.";
    /// <summary>UX-S29: emanet yaşı yalnız ilk N müşteride okundu.</summary>
    public static string EmanetSinir(int n, string dil) => En(dil)
        ? $"Safekeeping age was checked for the first {n} customers — all are on the Safekeeping screen."
        : $"Emanet süresi ilk {n} müşteride okundu — tamamı Emanet ekranında.";
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4B — Tezgâh (Tezgah4/4a) · Müşteriler (Musteri4/4a) · Tüm ekranlar
// (Tum4/4a) BİREBİR metinleri. TR = tuvaldeki yazım AYNEN (büyük harfli süzgeç
// adları tuvalde büyük harfle yazılmıştır; CSS dönüşümü yok).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yıldız "Tüm ekranlar" metinleri (tuval Tum4).</summary>
public static class YildizEkranSozlugu
{
    private static bool En(string dil) => dil == "en";
    /// <summary>Arama kutusu yer tutucusu ve etiketi (tuval "Ekran ara").</summary>
    public static string Ara(string dil) => En(dil) ? "Search screens" : "Ekran ara";
    /// <summary>
    /// Kare adı: tuval Tum4 metni AYNEN. Tek sapma "/gider": menü adı "Gider ve para aktarımı"
    /// (03.10 ekip oyu), tuval "Gider ve virman" yazar — kurucu 09.10 "tuval birebir" → Yıldız
    /// karesinde tuval adı (menünün eşanlamlı eski adı; arama iki adı da bulur).
    /// </summary>
    public static string KareAdi(string yol, string menuAdi, string dil) =>
        !En(dil) && yol == "/gider" ? "Gider ve virman" : menuAdi;
}

/// <summary>Yıldız "Müşteriler" metinleri (tuval Musteri4).</summary>
public static class YildizMusteriSozlugu
{
    private static bool En(string dil) => dil == "en";
    /// <summary>Üst satır (tuval "7 müşteri · 2 alacak · 1 borç").</summary>
    public static string Ust(int musteri, int alacak, int borc, string dil) => En(dil)
        ? $"{musteri} {(musteri == 1 ? "customer" : "customers")} · {BorcDiliSozlugu.Sayilar(alacak, borc, dil)}"
        : $"{musteri} müşteri · {BorcDiliSozlugu.Sayilar(alacak, borc, dil)}";
    /// <summary>Sunucu sınırına değince üst satırdaki sayı "100+" yazılır (UX-S29).</summary>
    public static string UstSinirli(int sinir, int alacak, int borc, string dil) => En(dil)
        ? $"{sinir}+ customers · {BorcDiliSozlugu.Sayilar(alacak, borc, dil)}"
        : $"{sinir}+ müşteri · {BorcDiliSozlugu.Sayilar(alacak, borc, dil)}";
    public static string Baslik(string dil) => En(dil) ? "Customers" : "Müşteriler";
    public static string AraYerTutucu(string dil) => En(dil) ? "Name or phone" : "Ad ya da telefon";
    public static string AraEtiket(string dil) => En(dil) ? "Search customers" : "Müşteri ara";
    public static string YeniMusteri(string dil) => En(dil) ? "New customer" : "Yeni müşteri";
    public static string Hepsi(string dil) => En(dil) ? "ALL" : "HEPSİ";
    /// <summary>Süzgeç (tuval "BORÇLU" AYNEN — "müşteri bize borçlu" olanlar; P-DIL-BORC-KASA: tuval geometrisi korunur).</summary>
    public static string Borclu(string dil) => En(dil) ? "OWING" : "BORÇLU";
    public static string Toptanci(string dil) => En(dil) ? "WHOLESALE" : "TOPTANCI";
    /// <summary>Tuvalde olmayan tür/pasif süzgeci — tuvalin ".ekle" kalıbıyla listenin altında (YILDIZ-4C; artı işareti ikondur).</summary>
    public static string DigerSuzgec(string dil) => En(dil) ? "Filter" : "Süzgeç";
    public static string DigerSuzgecKapat(string dil) => En(dil) ? "Close filter" : "Süzgeci kapat";
    /// <summary>Alt satırdaki segment adı (tuval "Toptancı · 0532 …").</summary>
    public static string ToptanciAlt(string dil) => En(dil) ? "Wholesaler" : "Toptancı";
    public static string Pasif(string dil) => En(dil) ? "inactive" : "kullanımda değil";
    public static string BizeBorclu(string dil) => BorcDiliSozlugu.KisaBizeBorclu(dil); // P-DIL-BORC-KASA
    /// <summary>Emanet payı (tuval "emanette 9,16 g has").</summary>
    public static string Emanette(string gram, string dil) => En(dil) ? $"{gram} g fine in safekeeping" : $"emanette {gram} g has";
    /// <summary>Has bakiyesi (tuvalde örneği yok — en yakın kalıp: soluk ek).</summary>
    public static string HasBakiye(string gram, string dil) => En(dil) ? $"{gram} g fine" : $"{gram} g has";
    /// <summary>Süzgeçte kimse yoksa (boş durum — istek başarılı).</summary>
    public static string SuzgecBos(string dil) => En(dil) ? "No customer matches this filter." : "Bu süzgeçte müşteri yok.";
    public static string Bos(string dil) => En(dil) ? "No customers yet." : "Henüz müşteri yok.";
    /// <summary>YILDIZ-4C: arama/süzgeç yanıtı boş (istek başarılı).</summary>
    public static string AramaBos(string dil) => En(dil) ? "No customer matches the search." : "Aramaya uyan müşteri yok.";
    /// <summary>YILDIZ-4C: tuval Musteri4 alt satırı AYNEN.</summary>
    public static string Not(string dil) => En(dil) ? "Tap a row to open the card: collection, statement, editing." : "Satıra dokununca kart açılır: tahsilat, ekstre, düzenleme.";
    /// <summary>Kartın içindeki eylem satırının etiketi (ekran okuyucu).</summary>
    public static string KartEylemleri(string dil) => En(dil) ? "Card actions" : "Kart eylemleri";
}

/// <summary>Yıldız "Tezgâh" metinleri (tuval Tezgah4).</summary>
public static class YildizTezgahSozlugu
{
    private static bool En(string dil) => dil == "en";
    /// <summary>Müşterinin bakiye durumu (tuval "borcu yok").</summary>
    public static string BorcuYok(string dil) => BorcDiliSozlugu.BorcYok(dil); // P-DIL-BORC-KASA: tek kalıp
    /// <summary>Tuvalde örneği yok — en yakın kalıp: "borcu yok" ile aynı yapı.</summary>
    public static string Borcu(string tutar, string dil) => BorcDiliSozlugu.BizeBorclu(tutar, dil); // P-DIL-BORC-KASA
    public static string Alacagi(string tutar, string dil) => BorcDiliSozlugu.BizBorcluyuz(tutar, dil); // P-DIL-BORC-KASA
    public static string Degistir(string dil) => En(dil) ? "Change" : "Değiştir";
    public static string MusteriKapat(string dil) => En(dil) ? "Close" : "Kapat";
    /// <summary>Tür seçimi düğmesinin ekran okuyucu adı (tuvalde ilk kelime "Satış").</summary>
    public static string TurSec(string dil) => En(dil) ? "Change the transaction type" : "İşlem türünü değiştir";
    public static string BarkodEtiket(string dil) => En(dil) ? "Barcode or product name" : "Barkod ya da ürün adı";
    /// <summary>Kalem alt satırı (tuval "2 adet · 1,00 g · 1,9900 g has").</summary>
    public static string Adet(string adet, string dil) => En(dil) ? $"{adet} pcs" : $"{adet} adet";
    public static string Gram(string gram) => $"{gram} g";
    public static string Has(string has, string dil) => En(dil) ? $"{has} g fine" : $"{has} g has";
    public static string KalemDuzenle(string ad, string dil) => En(dil) ? $"Edit {ad}" : $"{ad} — düzenle";
    public static string KalemlerKapat(string dil) => En(dil) ? "Done" : "Tamam";
    /// <summary>YILDIZ-4B tur 2: kimlik/MASAK formunu açan hap (tuval "+ Başka ödeme" kalıbı).</summary>
    public static string KimlikEkle(string dil) => En(dil) ? "+ Identity details" : "+ Kimlik bilgisi";
    /// <summary>YILDIZ-4B tur 2: kimlik çekmecesinin başlığı.</summary>
    public static string KimlikBaslik(string dil) => En(dil) ? "Identity details" : "Kimlik bilgisi";
    public static string Bos(string dil) => En(dil) ? "Scan a barcode to start the sale." : "Satışa başlamak için barkodu okut.";
    public static string Odeme(string dil) => En(dil) ? "Payment" : "Ödeme";
    public static string OdemeDuzenle(string dil) => En(dil) ? "Edit payment" : "Ödemeyi düzenle";
    public static string BaskaOdeme(string dil) => En(dil) ? "+ Another payment" : "+ Başka ödeme";
    public static string TekOdeme(string dil) => En(dil) ? "− Single payment" : "− Tek ödeme";
    public static string ParaUstu(string dil) => En(dil) ? "+ Change" : "+ Para üstü";
    public static string Indirim(string dil) => En(dil) ? "+ Discount" : "+ İndirim";
    public static string ParaUstuKilit(string dil) => En(dil) ? "Change is calculated only for a single cash payment." : "Para üstü yalnız tek ödemeli nakitte hesaplanır.";
    public static string KalemYok(string dil) => En(dil) ? "Add a product first." : "Önce ürün ekleyin.";
    public static string Fark(string dil) => En(dil) ? "Difference" : "Fark";
    /// <summary>Yasal satır (tuval "Yasal kurallar uygun · nakit 30.000 ₺ sınırının altında"); sınır SUNUCUDAN.</summary>
    public static string YasalUygun(string dil) => En(dil) ? "Legal rules met" : "Yasal kurallar uygun";
    public static string NakitSinirAlti(string had, string dil) => En(dil) ? $"cash below the {had} limit" : $"nakit {had} sınırının altında";
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";

    // ── PAKET YILDIZ-4C — etkileşim hâlleri (tuval kalıplarıyla) ──
    /// <summary>Müşteri katmanı araması (tuval Y_Emanet yer tutucusu "Müşteri ara").</summary>
    public static string MusteriAra(string dil) => En(dil) ? "Search customers" : "Müşteri ara";
    /// <summary>Seçim listesinde seçili satırın durumu.</summary>
    public static string Secili(string dil) => En(dil) ? "selected" : "seçili";
    /// <summary>Seçim satırının eylemi (tuval .b3).</summary>
    public static string Sec(string dil) => En(dil) ? "Select" : "Seç";
    public static string MusteriSecAria(string ad, string dil) => En(dil) ? $"Select {ad}" : $"{ad} — seç";
    /// <summary>UX-S29: katmanda gösterilmeyen müşteri sayısı ve daraltma yolu.</summary>
    public static string MusteriSinir(int kalan, string dil) => En(dil)
        ? $"{kalan} more — type a name to narrow the list."
        : $"{kalan} müşteri daha — listeyi daraltmak için ad yazın.";
    /// <summary>PRO: satıcı ve fiyat listesi alanlarını açan .ekle.</summary>
    public static string SaticiListe(string dil) => En(dil) ? "Seller and price list" : "Satıcı ve fiyat listesi";
    /// <summary>Kalem düzenlemesi: kalemi fişten çıkar (tuval .b2).</summary>
    public static string Cikar(string dil) => En(dil) ? "Remove" : "Çıkar";
    public static string CikarAria(string ad, string dil) => En(dil) ? $"Remove {ad}" : $"{ad} — fişten çıkar";
    /// <summary>Karmada satır tavanı (5) — kilitli hapın sebebi (UX-S30).</summary>
    public static string BacakTavan(string dil) => En(dil) ? "At most 5 payment lines." : "En çok 5 ödeme satırı.";
    /// <summary>YILDIZ-4C tur 3: karma ödeme satırını çıkaran .b2 düğmesinin erişilebilir adı.</summary>
    public static string OdemeSatiriCikar(int sira, string dil) => En(dil) ? $"Remove payment line {sira}" : $"{sira}. ödeme satırını çıkar";
}
