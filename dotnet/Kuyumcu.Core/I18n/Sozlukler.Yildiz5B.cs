namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Para ve banka 12 ekranın "Yıldız" metinleri (TR + EN).
//
// Kaynak (TEK DOĞRULUK): dokumanlar/09-uygulama-kayitlari/ham/yildiz-tuval/kaynak/
//   Y_Kasalar · Y_Kasakuyrugu · Y_POScihazlari · Y_POSalacaklari · Y_Banka · Y_Cekvesenet ·
//   Y_Vadetakvimi · Y_Kur · Y_Gidervevirman · Y_Hurdaveramat · Y_Kasaraporu · Y_Giderpusulalari.
// Türkçe metin tuvaldeki AYNEN; tuval metni ürünün gerçek davranışıyla çelişiyorsa (Anayasa md.1:
// ekran yalan söylemez) en yakın doğru cümle yazılır ve PAKET-YILDIZ-5B raporunun "belirsiz
// eşleşme" tablosuna geçer.
// ⚠ Yalnız data-sema="yildiz" iken çizilen bileşenlerin metinleridir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Para ekranlarının ortak metinleri.</summary>
public static class YildizParaSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string ParaBanka(string dil) => En(dil) ? "Money and bank" : "Para ve banka";
    public static string Tutar(string dil) => En(dil) ? "Amount" : "Tutar";
    public static string Musteri(string dil) => En(dil) ? "Customer" : "Müşteri";
    public static string Fis(string dil) => En(dil) ? "Slip" : "Fiş";
    public static string Yok(string dil) => En(dil) ? "none" : "yok";
    public static string Bugun(string dil) => En(dil) ? "today" : "bugün";
    public static string Dun(string dil) => En(dil) ? "yesterday" : "dün";
    public static string EkranaDon(string dil) => En(dil) ? "Back to summary" : "Özete dön";
    public static string PesinMusteri(string dil) => En(dil) ? "Walk-in customer" : "Peşin müşteri";
    public static string Has(string dil) => En(dil) ? "fine" : "has";
}

/// <summary>Kasalar (tuval Y_Kasalar).</summary>
public static class YildizKasalarSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Cumle(string dil) => En(dil)
        ? "Ledger balance. Cash, card and transfer are counted separately; the end-of-day count is compared with cash only."
        : "Defter bakiyesi. Nakit, kart ve havale ayrı sayılır; gün sonu sayımı yalnız nakitle karşılaştırılır.";
    public static string GunuKapat(string dil) => En(dil) ? "Close the day" : "Günü kapat";
    public static string HareketEkle(string dil) => En(dil) ? "Add movement" : "Hareket ekle";
    public static string Ne(string dil) => En(dil) ? "What" : "Ne";
    public static string Islem(string dil) => En(dil) ? "Entries" : "İşlem";
    public static string Nakit(string dil) => En(dil) ? "Cash" : "Nakit";
    public static string Kart(string dil) => En(dil) ? "Card" : "Kart";
    public static string Havale(string dil) => En(dil) ? "Transfer" : "Havale";
    public static string Gecmis(string dil) => En(dil) ? "History" : "Geçmiş";
    public static string SonKapanis(string dil) => En(dil) ? "Last closing" : "Son kapanış";
    public static string BugunGiren(string dil) => En(dil) ? "In today" : "Bugün giren";
    public static string Cikan(string dil) => En(dil) ? "Out" : "Çıkan";
    public static string KasaYok(string dil) => En(dil) ? "There is no till yet. Add the till first; sales are written to it." : "Henüz kasa yok. Önce kasayı ekle; satışlar ona yazılır.";
    public static string KasaEkle(string dil) => En(dil) ? "Add till" : "Kasa ekle";
    public static string PasifKilit(string dil) => En(dil) ? "This till is inactive." : "Bu kasa kullanımda değil.";
    /// <summary>UX-S29: okunan hareket sınırı.</summary>
    public static string Sinir(int n, string dil) => En(dil) ? $"{n}+" : $"{n}+";
}

/// <summary>Kur (tuval Y_Kur).</summary>
public static class YildizKurSozlugu
{
    private static bool En(string dil) => dil == "en";
    /// <summary>Tuval cümlesi; "ekranda eski kur yazar" ürünün davranışı değil → en yakın doğru cümle (belirsiz eşleşme).</summary>
    public static string Cumle(string dil) => En(dil)
        ? "Prices are calculated from this rate. If the internet is down the last rate stays and its time is written here."
        : "Fiyatlar bu kurdan hesaplanır. İnternet kesikse son kur kalır ve saati burada yazar.";
    public static string SimdiGuncelle(string dil) => En(dil) ? "Update now" : "Şimdi güncelle";
    public static string ElleGir(string dil) => En(dil) ? "Enter manually" : "Elle gir";
    public static string Birim(string dil) => En(dil) ? "Unit" : "Birim";
    public static string Alis(string dil) => En(dil) ? "Buy" : "Alış";
    public static string Satis(string dil) => En(dil) ? "Sell" : "Satış";
    public static string HasAltin(string dil) => En(dil) ? "Fine gold" : "Has altın";
    public static string Ayar22Gram(string dil) => En(dil) ? "22K gram" : "22 ayar gram";
    public static string Kaynak(string dil) => En(dil) ? "Source" : "Kaynak";
    public static string SonSorgu(string dil) => En(dil) ? "Last query" : "Son sorgu";
    public static string GunFarki(string dil) => En(dil) ? "Day change" : "Gün farkı";
    public static string KurYok(string dil) => En(dil) ? "No rate has been received yet. Update now or enter it manually." : "Henüz kur alınmadı. Şimdi güncelle ya da elle gir.";
}

/// <summary>Hurda ve ramat (tuval Y_Hurdaveramat).</summary>
public static class YildizHurdaSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · old gold piling up" : "Para ve banka · biriken eski altın";
    /// <summary>Tuval cümlesi; "ramat dönünce eklenir" ürünün davranışı değil (ramat dönüşü kaydı yok) → ilk iki cümle (belirsiz eşleşme).</summary>
    public static string Cumle(string dil) => En(dil)
        ? "The fine-gold equivalent of the scrap piling up in the shop. It drops from here when you send it to the refinery."
        : "İşletmede biriken hurdanın has karşılığı. Rafineriye gönderince buradan düşer.";
    public static string RafineriyeGonder(string dil) => En(dil) ? "Send to refinery" : "Rafineriye gönder";
    public static string HurdaAl(string dil) => En(dil) ? "Buy scrap" : "Hurda al";
    public static string Alim(string dil) => En(dil) ? "Purchase" : "Alım";
    public static string Ayar(string dil) => En(dil) ? "Purity" : "Ayar";
    public static string Gram(string dil) => En(dil) ? "Gram" : "Gram";
    public static string Has(string dil) => En(dil) ? "Fine" : "Has";
    public static string BekleyenRamat(string dil) => En(dil) ? "Pending refinery return" : "Bekleyen ramat";
    public static string SonGonderim(string dil) => En(dil) ? "Last shipment" : "Son gönderim";
    public static string AlimYok(string dil) => En(dil) ? "No scrap has been bought yet." : "Henüz hurda alınmadı.";
    public static string Gram(decimal has, string metin, string dil) => En(dil) ? $"{metin} g fine" : $"{metin} g has";
}

/// <summary>Kasa raporu (tuval Y_Kasaraporu).</summary>
public static class YildizKasaRaporuSozlugu
{
    private static bool En(string dil) => dil == "en";
    private static readonly string[] Birler = ["", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"];
    private static readonly string[] Onlar = ["", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan"];

    /// <summary>Tuval "Sekiz" — gün sayısı yazıyla (1–99), cümle başı büyük harf; aşarsa rakam.</summary>
    public static string GunSayisi(int n, string dil)
    {
        if (En(dil) || n is < 1 or > 99) return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var s = (Onlar[n / 10] + " " + Birler[n % 10]).Trim();
        return char.ToUpper(s[0], new System.Globalization.CultureInfo("tr-TR")) + s[1..];
    }
    public static string Ust(string kasa, string aralik, string dil) => En(dil) ? $"Reports · {kasa} · {aralik}" : $"Raporlar · {kasa} · {aralik}";
    public static string Cumle(int gun, string dil) => En(dil)
        ? $"The difference between what came in and went out of the till in {GunSayisi(gun, dil)} days. Card and transfer are on their own lines; they do not mix with the cash count."
        : $"{GunSayisi(gun, dil)} günde kasaya giren ile çıkanın farkı. Kart ve havale ayrı satırda; nakit sayımıyla karışmaz.";
    public static string GunuKapat(string dil) => En(dil) ? "Close the day" : "Günü kapat";
    public static string TarihSec(string dil) => En(dil) ? "Choose dates" : "Tarih seç";
    public static string Hareket(string dil) => En(dil) ? "Movement" : "Hareket";
    public static string Adet(string dil) => En(dil) ? "Count" : "Adet";
    public static string NakitGiris(string dil) => En(dil) ? "Cash in" : "Nakit giriş";
    public static string Kart(string dil) => En(dil) ? "Card" : "Kart";
    public static string Havale(string dil) => En(dil) ? "Transfer" : "Havale";
    public static string NakitCikis(string dil) => En(dil) ? "Cash out" : "Nakit çıkış";
    public static string Giris(string dil) => En(dil) ? "In" : "Giriş";
    public static string Cikis(string dil) => En(dil) ? "Out" : "Çıkış";
    public static string Acilis(string dil) => En(dil) ? "Opening" : "Açılış";
    public static string Kapanis(string dil) => En(dil) ? "Closing" : "Kapanış";
    public static string Bos(string dil) => En(dil) ? "There is no till to report." : "Raporlanacak kasa yok.";
}

/// <summary>Kasa kuyruğu (tuval Y_Kasakuyrugu).</summary>
public static class YildizVezneSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · slips waiting for payment" : "Para ve banka · ödemesi bekleyen fişler";
    public static string Bekliyor(string dil) => En(dil) ? "Waiting" : "Bekliyor";
    public static string TahsilEt(string dil) => En(dil) ? "Collect" : "Tahsil et";
    public static string TahsilKilit(string dil) => En(dil) ? "Send the slip to the till first." : "Önce fişi kasaya gönder.";
    public static string Satis(int urun, string dil) => En(dil) ? $"Sale · {urun} item(s)" : $"Satış · {urun} ürün";
    public static string SatisYalin(string dil) => En(dil) ? "Sale" : "Satış";
    public static string Tahsilat(string dil) => En(dil) ? "Collection" : "Tahsilat";
    public static string Iscilik(string dil) => En(dil) ? "Workmanship" : "İşçilik";
    public static string Dk(string dil) => En(dil) ? "min" : "dk";
    public static string Sa(string dil) => En(dil) ? "h" : "sa";
    public static string Gun(string dil) => En(dil) ? "d" : "gün";
    /// <summary>Tuval notu; "bu ekran kapalı kalır" ürünün davranışı değil (görev ayrımı bir ayardır) → en yakın doğru cümle.</summary>
    public static string Not(string dil) => En(dil)
        ? "The salesperson prepares the slip, the person at the till collects it. In a one-person shop task separation stays off."
        : "Tezgâhtar fişi hazırlar, kasadaki kişi tahsil eder. Tek kişilik işletmede görev ayrımı kapalı kalır.";
    public static string Bos(string dil) => En(dil) ? "No slip is waiting at the till." : "Kasada bekleyen fiş yok.";
    public static string Sinir(int n, string dil) => En(dil) ? $"The first {n} slips are shown; there are more." : $"İlk {n} fiş gösteriliyor; daha fazlası var.";
    public static string Ac(string fis, string dil) => En(dil) ? $"{fis} — open" : $"{fis} — aç";
}

/// <summary>POS alacakları (tuval Y_POSalacaklari).</summary>
public static class YildizPosAlacakSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · money moving from card to bank" : "Para ve banka · karttan bankaya geçecek para";
    /// <summary>Tuval "Fiş/Müşteri" sütunları — POS alacağı fişe/müşteriye bağlı değil (sunucu yanıtında yok) → cihaz ve banka (belirsiz eşleşme).</summary>
    public static string Pos(string dil) => En(dil) ? "POS" : "POS";
    public static string Banka(string dil) => En(dil) ? "Bank" : "Banka";
    public static string Bankaya(string dil) => En(dil) ? "To bank" : "Bankaya";
    public static string Not(string toplam, string dil) => En(dil)
        ? $"Total {toplam} · passes to the bank after the commission is deducted."
        : $"Toplam {toplam} · komisyon kesildikten sonra bankaya geçer.";
    public static string Bos(string dil) => En(dil) ? "No card money is waiting for the bank." : "Bankaya geçmeyi bekleyen kart parası yok.";
    public static string Sinir(int n, string dil) => En(dil) ? $"The first {n} records are shown; narrow the dates for the others." : $"İlk {n} kayıt gösteriliyor; ötekiler için tarihi daralt.";
}

/// <summary>POS cihazları (tuval Y_POScihazlari).</summary>
public static class YildizPosSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · card machines" : "Para ve banka · kart makineleri";
    public static string BosBaslik(string dil) => En(dil) ? "No POS defined yet" : "Henüz POS tanımlı değil";
    public static string BosMetin(string dil) => En(dil)
        ? "Without a POS, card sales are written to the cash till and a false difference appears at the end of the day. Add the device first, then the linked bank account."
        : "POS tanımlanmadan kartlı satışlar nakit kasasına yazılır ve gün sonunda sahte fark çıkar. Önce cihazı, sonra bağlı banka hesabını ekle.";
    public static string PosEkle(string dil) => En(dil) ? "Add POS" : "POS ekle";
    /// <summary>Dolu liste tuvalde çizilmedi → liste kalıbı (belirsiz eşleşme).</summary>
    public static string Cihaz(string dil) => En(dil) ? "Device" : "Cihaz";
    public static string Banka(string dil) => En(dil) ? "Bank" : "Banka";
    public static string Bekleyen(string dil) => En(dil) ? "Pending" : "Bekleyen";
    public static string Komisyon(string dil) => En(dil) ? "Commission" : "Komisyon";
    public static string Duzenle(string dil) => En(dil) ? "Edit" : "Düzenle";
    public static string Aktiflestir(string dil) => En(dil) ? "Activate" : "Aktife al";
    public static string KullanimdaDegil(string dil) => En(dil) ? "inactive" : "kullanımda değil";
}

/// <summary>Banka (tuval Y_Banka).</summary>
public static class YildizBankaSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · accounts and movements" : "Para ve banka · hesaplar ve hareketler";
    public static string BosBaslik(string dil) => En(dil) ? "No bank account" : "Banka hesabı yok";
    public static string BosMetin(string dil) => En(dil)
        ? "Add the account where transfers and card collections go; the movements appear here."
        : "Havale ve kart tahsilatlarının gideceği hesabı ekle; hareketler burada görünür.";
    public static string HesapEkle(string dil) => En(dil) ? "Add account" : "Hesap ekle";
    /// <summary>Dolu liste tuvalde çizilmedi → liste kalıbı (belirsiz eşleşme).</summary>
    public static string Hesap(string dil) => En(dil) ? "Account" : "Hesap";
    public static string Iban(string dil) => En(dil) ? "IBAN" : "IBAN";
    public static string Bakiye(string dil) => En(dil) ? "Balance" : "Bakiye";
    public static string Hareketler(string dil) => En(dil) ? "Movements" : "Hareketler";
    public static string KullanimdaDegil(string dil) => En(dil) ? "inactive" : "kullanımda değil";
}

/// <summary>Çek ve senet (tuval Y_Cekvesenet).</summary>
public static class YildizEvrakSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · received and given" : "Para ve banka · alınan ve verilen";
    public static string AraYerTutucu(string dil) => En(dil) ? "From whom or bank" : "Kimden ya da banka";
    public static string CekAl(string dil) => En(dil) ? "Take cheque" : "Çek al";
    public static string Hepsi(string dil) => En(dil) ? "All" : "Hepsi";
    public static string Alinan(string dil) => En(dil) ? "Received" : "Alınan";
    public static string Verilen(string dil) => En(dil) ? "Given" : "Verilen";
    public static string Belge(string dil) => En(dil) ? "Document" : "Belge";
    public static string Kimden(string dil) => En(dil) ? "From" : "Kimden";
    public static string Vade(string dil) => En(dil) ? "Due" : "Vade";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string TahsilEt(string dil) => En(dil) ? "Collect" : "Tahsil et";
    public static string TahsilKilit(string durum, string dil) => En(dil) ? $"This document cannot be collected ({durum})." : $"Bu evrak tahsil edilemez ({durum}).";
    public static string Bos(string dil) => En(dil) ? "No cheque or promissory note." : "Çek ya da senet yok.";
    public static string AramaBos(string dil) => En(dil) ? "Nothing matches the search." : "Aramaya uyan evrak yok.";
    public static string Sinir(int n, string dil) => En(dil) ? $"The first {n} documents are shown; narrow the filter in all details." : $"İlk {n} evrak gösteriliyor; tüm ayrıntılarda süzgeci daralt.";
    public static string Ac(string belge, string dil) => En(dil) ? $"{belge} — edit" : $"{belge} — düzenle";
}

/// <summary>Gider pusulaları (tuval Y_Giderpusulalari).</summary>
public static class YildizPusulaSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Documents and compliance · the voucher for gold bought from individuals" : "Belge ve uyum · vatandaştan alınan altının belgesi";
    public static string AraYerTutucu(string dil) => En(dil) ? "Search seller" : "Satıcı ara";
    public static string PusulaDuzenle(string dil) => En(dil) ? "Issue voucher" : "Pusula düzenle";
    public static string Pusula(string dil) => En(dil) ? "Voucher" : "Pusula";
    public static string Satici(string dil) => En(dil) ? "Seller" : "Satıcı";
    public static string NeAlindi(string dil) => En(dil) ? "What was bought" : "Ne alındı";
    public static string Tarih(string dil) => En(dil) ? "Date" : "Tarih";
    public static string KimlikAlindi(string dil) => En(dil) ? "ID taken" : "kimlik alındı";
    public static string Iptal(string dil) => En(dil) ? "cancelled" : "iptal";
    public static string Not(string dil) => En(dil)
        ? "No voucher is issued without ID and address (Tax Procedure Law art. 234)."
        : "Kimlik ve adres olmadan pusula açılmaz (VUK md. 234).";
    public static string Bos(string dil) => En(dil) ? "No expense voucher yet." : "Henüz gider pusulası yok.";
    public static string AramaBos(string dil) => En(dil) ? "No seller matches the search." : "Aramaya uyan satıcı yok.";
    public static string Sinir(int n, string dil) => En(dil) ? $"The last {n} vouchers are shown; narrow the dates in all details." : $"Son {n} pusula gösteriliyor; tüm ayrıntılarda tarihi daralt.";
    public static string Ac(string no, string dil) => En(dil) ? $"{no} — details" : $"{no} — ayrıntı";
}

/// <summary>Vade takvimi (tuval Y_Vadetakvimi).</summary>
public static class YildizVadeSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · who pays what on which day" : "Para ve banka · hangi gün kimden ne";
    public static string GunAdlari(string dil) => En(dil) ? "MON TUE WED THU FRI SAT SUN" : "PZT SAL ÇAR PER CUM CMT PAZ";
    public static string BuAy(string dil) => En(dil) ? "This month" : "Bu ay";
    public static string Gun(string dil) => En(dil) ? "Day" : "Gün";
    public static string Kim(string dil) => En(dil) ? "Who" : "Kim";
    /// <summary>Tuval "Anadolu Döküm · biz" — verilen evrakta ödeyen biziz.</summary>
    public static string Biz(string kim, string dil) => En(dil) ? $"{kim} · us" : $"{kim} · biz";
    public static string Bos(string dil) => En(dil) ? "No cheque or note falls due this month." : "Bu ay vadesi gelen çek ya da senet yok.";
    public static string Sinir(int n, string dil) => En(dil) ? $"The first {n} documents are shown." : $"İlk {n} evrak gösteriliyor.";
}

/// <summary>Gider ve virman (tuval Y_Gidervevirman).</summary>
public static class YildizGiderSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ust(string dil) => En(dil) ? "Money and bank · shop expense or transfer between tills" : "Para ve banka · işletme gideri ya da kasalar arası aktarma";
    public static string NeYapiyorsun(string dil) => En(dil) ? "What are you doing" : "Ne yapıyorsun";
    public static string Gider(string dil) => En(dil) ? "Expense" : "Gider";
    public static string Virman(string dil) => En(dil) ? "Transfer" : "Virman";
    public static string NeIcin(string dil) => En(dil) ? "What for" : "Ne için";
    public static string AyrintiEkle(string dil) => En(dil) ? "Add details" : "Ayrıntı ekle";
    public static string Kaydet(string dil) => En(dil) ? "Save" : "Kaydet";
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";
    public static string Not(string dil) => En(dil) ? "Note" : "Not";
    /// <summary>Tuval notunun ilk paragrafı; "fatura fotoğrafı" ikinci paragrafı ürünün davranışı değil (gider belgesi yükleme yok) → yazılmaz.</summary>
    public static string NotMetni(string dil) => En(dil)
        ? "An expense leaves the till and is written to profit and loss. A transfer moves money between two tills; the total does not change."
        : "Gider kasadan düşer, kâr-zarara yazılır. Virman iki kasa arasında para taşır; toplam değişmez.";
}
