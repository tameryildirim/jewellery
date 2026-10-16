namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-6 — kalan görünüm kuyruğu (Vitrin, Kur, Vade, Kurulum, Siparişler,
// liste sayaçları, Uyum, Panel yasal liste bandı, Gider / Para aktarımı).
// YALNIZ GÖRÜNÜM: metinler bugünkü davranışı sade dille anlatır; hiçbir yasal
// kural, eşik, kapı ya da hesap DEĞİŞMEZ (gider nakit kapısı, virman kapısı,
// yasal liste fail-closed uyarısı aynen — yalnız dil ve yerleşim).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına). Ekran bu dosyadaki karşılığı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-6 metinleri (bir dilde).</summary>
public sealed record Toplu6Metinleri(
    // ── /gider: nakit aşımı tek cümle; kart payı yalnız fark yaratıyorsa ──
    string GiderNakitYetmiyor,
    string GiderCikisYolu,
    // ── /gider?sekme=virman: BASİT'te her yerde "Para aktarımı" ──
    string VirmanSekmeBasit,
    string VirmanListeBasit,
    string VirmanYokBasit,
    string VirmanYapBasit,
    string VirmanAciklamaBilgi,
    string VirmanBankaYok,
    string VirmanHesapYok,
    string VirmanBankaEkle,
    // ── TOPLU-6 t2 (tasarım hakemi KRİTİK): BASİT'te kaydet düğmesi, sonuç/onay bantları ve
    //    sayfa açıklaması da "para aktarımı" der — aynı ekranda iki ad olmaz ──
    string VirmanKaydedildiBasit,
    string VirmanStornoOnayBasit,
    string VirmanStornoYapildiBasit,
    string EksiKasaUyariBasit,
    string SayfaAciklamaBasit,
    // ── /kurulum: eksik adıma eylem; alanı olmayan "Açıklama" etiketi yerine soru ──
    string KurulumSimdiGir,
    string KurulumAcilisNasil,
    string KurulumMuhurNe,
    // ── /siparisler: isteğe bağlı alanlar "+ … ekle" bağlantı kalıbı; tek kalem çıkarılmaz ──
    string SiparisDetayEkle,
    string SiparisDetayGizle,
    string SiparisTekKalem,
    // ── liste sayacı: sınırın altında yalnız sayı ──
    string ListeSayacSade,
    string ListeSiniriBilgi,
    // ── Panel yasal liste bandı: çıkmaz sokak yok, tutarlı dil ──
    string PanelListeYok,
    string PanelListeEski,
    string PanelListeGit,
    string PanelBekleyenSeritte,
    string UyumListeYukleyen,
    // ── /kur, /uyum: açıklama satırları ⓘ içinde ──
    string KurProBilgi,
    string UyumAramaBilgi);

/// <summary>TOPLU-6 sözlüğü — Tr/En aynı anahtarlar (eksik anahtar derlenmez).</summary>
public static class Toplu6Sozlugu
{
    public static readonly Toplu6Metinleri Tr = new(
        GiderNakitYetmiyor: "Çekmecede {0} nakit var; {1} gider için yetmiyor.",
        GiderCikisYolu: "Kasaya para girin ya da bankadan aktarın.",
        VirmanSekmeBasit: "Para aktarımı",
        VirmanListeBasit: "Para aktarımları",
        VirmanYokBasit: "Henüz para aktarımı yok.",
        VirmanYapBasit: "Para aktar",
        VirmanAciklamaBilgi: "Para aktarımı nedir?",
        VirmanBankaYok: "Henüz banka hesabı yok. Kasadan bankaya para aktarmak için önce banka hesabı ekleyin.",
        VirmanHesapYok: "Aktarılacak hesap yok. Önce banka hesabı ekleyin.",
        VirmanBankaEkle: "Banka hesabı ekle",
        VirmanKaydedildiBasit: "Para aktarıldı",
        VirmanStornoOnayBasit: "Bu para aktarımının iki tarafı birlikte geri alınacak. Onaylıyor musunuz?",
        VirmanStornoYapildiBasit: "Para aktarımı geri alındı (iki taraf birlikte)",
        EksiKasaUyariBasit: "Bu kasanın bakiyesi eksi ({0}). Gider yazmadan önce kasaya para girin ya da bankadan para aktarın.",
        SayfaAciklamaBasit: "İşletmenin masrafları ve kasa/banka arası para aktarımları. Para aktarımı ciroya girmez: para işletmeden çıkmaz, yer değiştirir.",
        KurulumSimdiGir: "Şimdi gir",
        KurulumAcilisNasil: "Nasıl girilir?",
        KurulumMuhurNe: "Mühürden sonra ne olur?",
        SiparisDetayEkle: "+ Söz günü, açıklama, tahmini tutar ekle",
        SiparisDetayGizle: "Daha az göster",
        SiparisTekKalem: "Tek kalem çıkarılamaz; önce yeni kalem ekleyin.",
        ListeSayacSade: "{0} kayıt gösteriliyor",
        ListeSiniriBilgi: "Liste en çok {0} kayıt gösterir.",
        PanelListeYok: "Yasal liste henüz yüklenmedi.",
        PanelListeEski: "Yasal liste {0} gündür güncellenmedi.",
        PanelListeGit: "Uyum ekranında gör",
        PanelBekleyenSeritte: "Bekleyen {0} iş üstteki uyarıda.",
        UyumListeYukleyen: "Liste sistem yöneticisi tarafından yüklenir; yüklendiğinde bu uyarı kendiliğinden kalkar.",
        KurProBilgi: "Kur kaynağı ve kur geçmişi nerede?",
        UyumAramaBilgi: "Arama nasıl yapılır?");

    public static readonly Toplu6Metinleri En = new(
        GiderNakitYetmiyor: "There is {0} cash in the drawer; not enough for a {1} expense.",
        GiderCikisYolu: "Add cash to the till or transfer from the bank.",
        VirmanSekmeBasit: "Money transfer",
        VirmanListeBasit: "Money transfers",
        VirmanYokBasit: "No money transfers yet.",
        VirmanYapBasit: "Transfer money",
        VirmanAciklamaBilgi: "What is a money transfer?",
        VirmanBankaYok: "No bank account yet. Add a bank account first to move money from the till to the bank.",
        VirmanHesapYok: "There is no account to transfer to. Add a bank account first.",
        VirmanBankaEkle: "Add bank account",
        VirmanKaydedildiBasit: "Money transferred",
        VirmanStornoOnayBasit: "Both sides of this money transfer will be reversed together. Do you confirm?",
        VirmanStornoYapildiBasit: "Money transfer reversed (both sides together)",
        EksiKasaUyariBasit: "This till's balance is negative ({0}). Before recording an expense, add cash to the till or transfer money from the bank.",
        SayfaAciklamaBasit: "The shop's expenses and money transfers between tills and banks. A money transfer is not revenue: the money does not leave the shop, it only moves.",
        KurulumSimdiGir: "Enter now",
        KurulumAcilisNasil: "How do I enter it?",
        KurulumMuhurNe: "What happens after sealing?",
        SiparisDetayEkle: "+ Add promised date, note, estimated amount",
        SiparisDetayGizle: "Show less",
        SiparisTekKalem: "The only item cannot be removed; add another item first.",
        ListeSayacSade: "{0} records shown",
        ListeSiniriBilgi: "The list shows at most {0} records.",
        PanelListeYok: "The legal list has not been loaded yet.",
        PanelListeEski: "The legal list has not been updated for {0} days.",
        PanelListeGit: "See on the compliance screen",
        PanelBekleyenSeritte: "{0} pending item(s) in the notice above.",
        UyumListeYukleyen: "The list is loaded by the system administrator; this notice clears by itself once it is loaded.",
        KurProBilgi: "Where are the rate source and rate history?",
        UyumAramaBilgi: "How does search work?");

    public static Toplu6Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
