namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-KATMAN — kendi Yıldız yüzü henüz olmayan ekranların SAYFA BAŞLIĞI (TR + EN).
//
// Denetim Y17/O16 (EKRAN-DENETIM-KULLANICI-GOZU-2026-10-10): Belge ve uyum, Raporlar, Yönetim
// ekranlarında başlık eski temanın küçük "Ad SS:DD · Yardım" satırıydı. Tuvalde HER ekran
// <main>'i aynı başlıkla açar: üst satır ("Grup · ne işe yarar", .ust) + ince başlık (.ince h1).
//
// Kaynak (TEK DOĞRULUK): dokumanlar/09-uygulama-kayitlari/ham/yildiz-tuval/kaynak/Y_*.dc.html
//   — üst satır metni tuvaldeki AYNEN. Tuvalde VERİ taşıyan üst satırlar (Kâr-zarar "Ekim 2026 ·
//   ilk 8 gün", Satış analizi "son 30 gün") uydurulmaz: yalnız grup adı yazılır. Tuvali olmayan
//   ekranlar (İrsaliyeler, Satın alma, Kılavuz, Ayarlar) yalnız grup adını taşır. İkisi de
//   PAKET-YILDIZ-KATMAN raporunun "belirsiz eşleşme" tablosundadır.
// ⚠ Yalnız data-sema="yildiz" iken ve yalnız aşağıdaki rotalarda çizilir (UstBar).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yıldız sayfa başlığının üst satırı ve (tuvalde farklıysa) başlık metni.</summary>
public static class YildizSayfaBasligiSozlugu
{
    private static bool En(string dil) => dil == "en";

    private static string BelgeUyum(string dil) => En(dil) ? "Documents and compliance" : "Belge ve uyum";
    private static string Raporlar(string dil) => En(dil) ? "Reports" : "Raporlar";
    private static string Yonetim(string dil) => En(dil) ? "Management" : "Yönetim";
    private static string MusteriStok(string dil) => En(dil) ? "Customers and stock" : "Müşteri ve stok";
    private static string ParaBanka(string dil) => En(dil) ? "Money and bank" : "Para ve banka";
    private static string Gunluk(string dil) => En(dil) ? "Daily work" : "Günlük iş";
    private static string Ile(string grup, string ek) => $"{grup} · {ek}";

    /// <summary>Rotanın üst satırı; Yıldız başlığı almayan rotada null.</summary>
    public static string? Ust(string yol, string dil) => yol switch
    {
        "/faturalar" => Ile(BelgeUyum(dil), En(dil) ? "issued and received" : "kesilen ve gelen"),
        "/irsaliyeler" => BelgeUyum(dil),
        "/uyum" => Ile(BelgeUyum(dil), En(dil) ? "MASAK · legal reporting" : "MASAK · yasal bildirim"),
        "/yasal-sinirlar" => Ile(BelgeUyum(dil), En(dil) ? "the program applies these by itself" : "program bunları kendiliğinden uygular"),
        "/raporlar" => Ile(Raporlar(dil), En(dil) ? "all reports" : "bütün raporlar"),
        "/raporlar/kar-zarar" => Raporlar(dil),
        "/raporlar/vade-takibi" => Ile(Raporlar(dil), En(dil) ? "whom to call" : "kimi aramak gerek"),
        "/raporlar/islem-defteri" => Ile(Raporlar(dil), En(dil) ? "every slip" : "bütün fişler"),
        "/raporlar/satis-analizi" => Raporlar(dil),
        "/kullanicilar" => Ile(Yonetim(dil), En(dil) ? "who signs in, with which rights" : "kim giriyor, hangi yetkiyle"),
        "/isletme" => Ile(Yonetim(dil), En(dil) ? "shop details" : "işletme bilgileri"),
        "/yetkiler" => Ile(Yonetim(dil), En(dil) ? "who can do what" : "kim neyi yapabilir"),
        "/denetim-izi" => Ile(Yonetim(dil), En(dil) ? "the trace of every action" : "her işlemin izi"),
        "/kurulum" => Ile(Yonetim(dil), En(dil) ? "first start, step by step" : "ilk açılış, adım adım"),
        "/veri-disa-aktarim" => Ile(Yonetim(dil), En(dil) ? "take records to a file" : "kayıtları dosyaya al"),
        "/veri-ice-aktarim" => Ile(Yonetim(dil), En(dil) ? "move from the old program" : "eski programdan taşı"),
        "/yedekler" => Ile(Yonetim(dil), En(dil) ? "copy of the records" : "kayıtların kopyası"),
        "/kilavuz" => Yonetim(dil),
        "/ayarlar" => Yonetim(dil),
        "/satin-alma" => MusteriStok(dil),
        // Görsel denetim Y-5 (21.10): bu ekranlar eski kabuk başlığında kalıyordu (22/300 + saat + Yardım) → Yıldız başlık deseni.
        "/gise" => Ile(ParaBanka(dil), En(dil) ? "morning hand-over, evening count" : "sabah teslim, akşam sayım"),
        "/e-belgeler" => Ile(BelgeUyum(dil), En(dil) ? "invoices on their way to GİB" : "faturaların GİB yolculuğu"),
        "/kvkk" => Ile(BelgeUyum(dil), En(dil) ? "privacy notice and consents" : "aydınlatma metni ve izinler"),
        "/vitrin" => Ile(ParaBanka(dil), En(dil) ? "prices for the shop window" : "vitrin için fiyatlar"),
        _ when yol.StartsWith("/siparisler/", StringComparison.Ordinal) => Ile(Gunluk(dil), En(dil) ? "order details" : "sipariş ayrıntısı"),
        _ when yol.StartsWith("/banka/ekstre/", StringComparison.Ordinal) => Ile(ParaBanka(dil), En(dil) ? "statement matching" : "ekstre eşleştirme"),
        _ => null,
    };

    /// <summary>
    /// Tuval başlığı ürün başlığından farklıysa tuvalinki (Y_UyumMASAK: "Uyum" — MASAK üst satırda);
    /// aynıysa null (üst barın rota adı kullanılır, tek kaynak).
    /// </summary>
    public static string? Baslik(string yol, string dil) => yol switch
    {
        "/uyum" => En(dil) ? "Compliance" : "Uyum",
        _ => null,
    };
}

/// <summary>YILDIZ-KATMAN: 5A kalanlarının Yıldız metinleri (Ürün ağacı çekmecesi, Etiketler ürün seçimi).</summary>
public static class YildizKatmanSozlugu
{
    private static bool En(string dil) => dil == "en";
    /// <summary>Ürün ağacı çekmecesi — tuval "grup" (Y_Urunagaci); düğme "Yeni grup" ile aynı ad.</summary>
    public static string GrupAdi(string dil) => En(dil) ? "Group name" : "Grup adı";
    public static string UstGrup(string dil) => En(dil) ? "Parent group" : "Üst grup";
    public static string DonguUyarisi(string dil) => En(dil) ? "A group cannot be moved under its own subgroup." : "Bir grup kendi alt grubunun altına taşınamaz.";
    /// <summary>Katman (çekmece/pencere) ikincil düğmesi — tuval form kalıbı b2 "Vazgeç" (Y_Isletme · Y_Yasalsinirlar · Y_Veridisaaktar).
    /// Yalnız Yıldız'da; öteki şemalar Ortak.Iptal ("İptal") AYNEN.</summary>
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";
}
