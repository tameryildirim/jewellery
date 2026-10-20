namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-g — MENÜ ADLARI TUVALLE BİREBİR (TR + EN)
//
// KAYNAK (TEK DOĞRULUK — kurucu kararı, plan ile çelişirse TUVAL geçerlidir):
//   dokumanlar/02-mimari/sadelestirme/tuval/project/
//     Panel.dc.html (basit menü + kapalı grup önizlemesi, satır 32-43)
//     Profesyonel.dc.html (profesyonel menü, satır 30-47)
//     Ekranlar.dc.html ("Tüm ekranlar" — 38 ekranın tam adları)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: "HUB
//   Sozlukler.cs'e DOKUNMA"). Hub'daki Nav.* anahtarları SİLİNMEDİ — başka
//   ekranlar (kılavuz, rapor kataloğu, sayfa gövdeleri) onları okumaya devam
//   eder. Menü, "Tüm ekranlar" karoları, genel arama ve üst bar başlığı
//   (RotaBasliklari) artık adını BURADAN alır.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): adlar KONUMSAL bir record'dadır;
//   Tr ve En aynı kurucudan geçer, bir dilde ad unutulursa derleme kırılır.
//
// ⚠ İKİ AD: Ad = menü satırı, karo ve sayfa başlığındaki TAM ad;
//   Kisa = KAPALI grubun önizleme satırındaki kısa ad (tuval Panel.dc.html:
//   "Kasalar · Vezne · POS · Banka · Çek-senet · Kur · Gider · Hurda…").
//
// ⚠ denetim.sh `# >>> P-14` MENU_AYNI_AD sayacı TR adlarını bu dosyadaki
//   `Tr = new(` bloğundan `<Anahtar>: new("<Ad>", "<Kısa>")` satırlarıyla okur.
//   Satır biçimi değişirse sayaç "TARAMA YAPILMADI" der (sessiz geçmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir menü ekranının tam ve kısa adı.</summary>
/// <param name="Ad">Menü satırı, karo ve sayfa başlığındaki ad.</param>
/// <param name="Kisa">Kapalı grup önizlemesindeki kısa ad.</param>
/// <param name="Esanlam">
/// ARAMA eş anlamlısı — S1-g öncesi menü adı. Ekranda GÖRÜNMEZ (jargon sıfır);
/// yalnız menü araması ve "Tüm ekranlar" araması onu da tarar: eski adı
/// ("cari", "denetim izi") bilen kullanıcı ekranı bulmaya devam eder.
/// </param>
public sealed record MenuAdi(string Ad, string Kisa, string Esanlam = "");

/// <summary>Menünün bütün adları (bir dilde). Sıra = MenuTanimi sırası.</summary>
public sealed record MenuAdlariMetinleri(
    // ── Günlük iş ──
    MenuAdi Panel,
    MenuAdi Satis,
    MenuAdi Emanet,
    MenuAdi SiparisTamir,
    MenuAdi Bildirimler,
    // ── Müşteri ve stok ──
    MenuAdi Musteriler,
    MenuAdi Urunler,
    MenuAdi UrunAgaci,
    MenuAdi Etiketler,
    MenuAdi FiyatListeleri,
    MenuAdi Sayim,
    MenuAdi SatinAlma,      // PAKET EKSIK-B1-SATINALMA (B1-1)
    MenuAdi Irsaliyeler,    // PAKET EKSIK-B1-SATINALMA (B1-2)
    // ── Para ve banka ──
    MenuAdi Kasalar,
    MenuAdi KasaKuyrugu,
    MenuAdi GiseZimmeti,    // Y-03 (20.10)
    MenuAdi PosCihazlari,
    MenuAdi PosAlacaklari,
    MenuAdi Banka,
    MenuAdi CekSenet,
    MenuAdi VadeTakvimi,
    MenuAdi Kur,
    MenuAdi GiderVirman,
    MenuAdi HurdaRamat,
    // ── Belge ve uyum ──
    MenuAdi Faturalar,
    MenuAdi GiderPusulalari,
    MenuAdi Uyum,
    MenuAdi Kvkk,   // D1-19
    MenuAdi YasalSinirlar,
    // ── Raporlar ──
    MenuAdi RaporMerkezi,
    MenuAdi KarZarar,
    MenuAdi VadeTakibi,
    MenuAdi IslemDefteri,
    MenuAdi SatisAnalizi,
    MenuAdi KasaRaporu,
    // ── Yönetim ──
    MenuAdi Kullanicilar,
    MenuAdi Isletme,
    MenuAdi Yetkiler,
    MenuAdi KimNeYapti,
    MenuAdi Kurulum,
    MenuAdi VeriDisaAktar,
    MenuAdi VeriIceAktar,
    MenuAdi Yedekler,
    // ── Alt blok ──
    MenuAdi Kilavuz,
    MenuAdi Ayarlar,
    // ── Grup başlıkları (cümle düzeni — BÜYÜK HARF değil) ──
    string GrupGunluk,
    string GrupMusteriStok,
    string GrupParaBanka,
    string GrupBelgeUyum,
    string GrupRapor,
    string GrupYonetim,
    // ── Kullanıcı kartı / üst bar ──
    string KartAyraci,
    string KurGuncelleIpucu);

/// <summary>Menü adları — dil seçimi <see cref="Icin"/> ile.</summary>
public static class MenuAdlari
{
    public static readonly MenuAdlariMetinleri Tr = new(
        Panel: new("Panel", "Panel"),
        Satis: new("Satış", "Satış", "İşlemler"),
        Emanet: new("Emanet", "Emanet"),
        SiparisTamir: new("Sipariş ve tamir", "Sipariş ve tamir", "Siparişler"),
        Bildirimler: new("Bildirimler", "Bildirimler"),
        Musteriler: new("Müşteriler", "Müşteriler", "Cariler"),
        Urunler: new("Ürünler", "Ürünler"),
        UrunAgaci: new("Ürün ağacı", "Ürün ağacı"),
        Etiketler: new("Etiketler", "Etiketler", "Ürün etiketleri"),
        FiyatListeleri: new("Fiyat listeleri", "Fiyat listeleri"),
        Sayim: new("Sayım", "Sayım"),
        SatinAlma: new("Satın alma", "Satın alma", "Tedarik siparişi Mal kabul"),
        Irsaliyeler: new("İrsaliyeler", "İrsaliyeler", "Sevk irsaliyesi"),
        Kasalar: new("Kasalar", "Kasalar"),
        KasaKuyrugu: new("Kasa kuyruğu", "Vezne", "Vezne"),
        GiseZimmeti: new("Gişe zimmeti", "Gişe", "Zimmet tezgâhtar kasası vardiya"),
        PosCihazlari: new("POS cihazları", "POS"),
        PosAlacaklari: new("POS alacakları", "POS alacakları", "POS hakedişleri"),
        Banka: new("Banka", "Banka"),
        CekSenet: new("Çek ve senet", "Çek-senet", "Çek / Senet evrak"),
        VadeTakvimi: new("Vade takvimi", "Vade takvimi"),
        Kur: new("Kur", "Kur"),
        GiderVirman: new("Gider ve para aktarımı", "Gider", "Gider ve virman"), // 03.10 ekip oyu (Ertuğ+Ramazan-ajan): jargon sıfır; eski ad eşanlamda aranır
        HurdaRamat: new("Hurda ve ramat", "Hurda"),
        Faturalar: new("Faturalar", "Faturalar"),
        GiderPusulalari: new("Gider pusulaları", "Gider pusulaları"),
        Uyum: new("Uyum (MASAK)", "Uyum (MASAK)", "Uyum"),
        Kvkk: new("KVKK", "KVKK", "Kişisel veriler"),
        YasalSinirlar: new("Yasal sınırlar", "Yasal sınırlar"),
        RaporMerkezi: new("Rapor merkezi", "Rapor merkezi", "Raporlar Dökümler"),
        KarZarar: new("Kâr-zarar", "Kâr-zarar"),
        VadeTakibi: new("Vade takibi", "Vade", "Gecikmiş alacaklar"),
        IslemDefteri: new("İşlem defteri", "İşlem defteri", "Hareket dökümü"),
        SatisAnalizi: new("Satış analizi", "Satış"),
        KasaRaporu: new("Kasa raporu", "Kasa", "Kasa hareketleri"),
        Kullanicilar: new("Kullanıcılar", "Kullanıcılar"),
        Isletme: new("İşletme", "İşletme"),
        Yetkiler: new("Yetkiler", "Yetkiler"),
        KimNeYapti: new("Kim ne yaptı", "Kim ne yaptı", "Denetim izi"),
        Kurulum: new("Kurulum", "Kurulum", "Kuruluş sihirbazı"),
        VeriDisaAktar: new("Veri dışa aktar", "Dışa aktar"),
        VeriIceAktar: new("Veri içe aktar", "İçe aktar"),
        Yedekler: new("Yedekler", "Yedek"),
        Kilavuz: new("Kılavuz", "Kılavuz"),
        Ayarlar: new("Ayarlar", "Ayarlar"),
        GrupGunluk: "Günlük iş",
        GrupMusteriStok: "Müşteri ve stok",
        GrupParaBanka: "Para ve banka",
        GrupBelgeUyum: "Belge ve uyum",
        GrupRapor: "Raporlar",
        GrupYonetim: "Yönetim",
        KartAyraci: " · ",
        KurGuncelleIpucu: "Kuru şimdi yeniden sor (normalde 10 saniyede bir kendisi sorar)");

    public static readonly MenuAdlariMetinleri En = new(
        Panel: new("Dashboard", "Dashboard"),
        Satis: new("Sales", "Sales", "Transactions"),
        Emanet: new("Safekeeping", "Safekeeping"),
        SiparisTamir: new("Orders and repairs", "Orders and repairs", "Orders"),
        Bildirimler: new("Notifications", "Notifications"),
        Musteriler: new("Customers", "Customers", "Accounts"),
        Urunler: new("Products", "Products"),
        UrunAgaci: new("Product tree", "Product tree"),
        Etiketler: new("Labels", "Labels", "Product labels"),
        FiyatListeleri: new("Price lists", "Price lists"),
        Sayim: new("Stock count", "Stock count"),
        SatinAlma: new("Purchasing", "Purchasing", "Purchase orders Goods receipt"),
        Irsaliyeler: new("Waybills", "Waybills", "Dispatch notes"),
        Kasalar: new("Cash registers", "Cash registers"),
        KasaKuyrugu: new("Cashier queue", "Cashier", "Cashier desk"),
        GiseZimmeti: new("Till custody", "Till", "Custody clerk shift"),
        PosCihazlari: new("POS terminals", "POS"),
        PosAlacaklari: new("POS receivables", "POS receivables", "POS settlements"),
        Banka: new("Bank", "Bank"),
        CekSenet: new("Cheques & notes", "Cheques", "Cheques and notes"),
        VadeTakvimi: new("Due calendar", "Due calendar"),
        Kur: new("Rates", "Rates"),
        GiderVirman: new("Expenses", "Expenses", "Expenses and transfers"),
        HurdaRamat: new("Scrap & refinery", "Scrap", "Scrap and refinery"),
        Faturalar: new("Invoices", "Invoices"),
        GiderPusulalari: new("Expense vouchers", "Expense vouchers"),
        Uyum: new("Compliance", "Compliance", "MASAK"),
        Kvkk: new("Data protection", "Data protection", "KVKK"),
        YasalSinirlar: new("Legal limits", "Legal limits"),
        RaporMerkezi: new("Report centre", "Report centre", "Reports Listings"),
        KarZarar: new("Profit and loss", "Profit and loss"),
        VadeTakibi: new("Overdue tracking", "Overdue", "Overdue receivables"),
        IslemDefteri: new("Transaction ledger", "Ledger", "Movement listing"),
        SatisAnalizi: new("Sales analysis", "Sales"),
        KasaRaporu: new("Cash report", "Cash", "Cash movements"),
        Kullanicilar: new("Users", "Users"),
        Isletme: new("Business", "Business"),
        Yetkiler: new("Permissions", "Permissions"),
        KimNeYapti: new("Who did what", "Who did what", "Audit log"),
        Kurulum: new("Setup", "Setup", "Setup wizard"),
        VeriDisaAktar: new("Data export", "Export"),
        VeriIceAktar: new("Data import", "Import"),
        Yedekler: new("Backups", "Backup"),
        Kilavuz: new("Guide", "Guide"),
        Ayarlar: new("Settings", "Settings"),
        GrupGunluk: "Daily work",
        GrupMusteriStok: "Customers and stock",
        GrupParaBanka: "Money and bank",
        GrupBelgeUyum: "Documents and compliance",
        GrupRapor: "Reports",
        GrupYonetim: "Administration",
        KartAyraci: " · ",
        KurGuncelleIpucu: "Ask for the rates again now (it normally asks by itself every 10 seconds)");

    /// <summary>Sözlüğün diline göre menü adları (EN sözlüğü → EN, aksi → TR).</summary>
    public static MenuAdlariMetinleri Icin(Sozluk t) => t.DilKodu == "en" ? En : Tr;

    /// <summary>Dil koduna göre menü adları ("en" → EN, aksi → TR).</summary>
    public static MenuAdlariMetinleri Icin(string dil) => dil == "en" ? En : Tr;
}
