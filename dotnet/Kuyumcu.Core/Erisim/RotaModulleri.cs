using Kuyumcu.Core.I18n;

namespace Kuyumcu.Core.Erisim;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-09 (Y-39) — YETKİ MATRİSİNİN TEK KAYNAĞI.
//
// ⚠ NEDEN BURADA: yetki matrisi bugüne dek API'de ELLE yazılmış bir liste
//   (Moduller.Tumu) idi; ekrandaki rota listesi (Kuyumcu.Web/Layout/RotaBasliklari)
//   ile arasında hiçbir bağ yoktu. Yeni bir ekran açıldığında matriste karşılığı
//   olmuyor, ekran ya herkese açık kalıyor ya da sessizce 403 veriyordu.
//   Artık matris BU HARİTADAN TÜRETİLİR (Moduller.Tumu = Modulleri) ve
//   RotaBasliklari ile iki yönlü test edilir (ROTA_YETKI_ESLESMEZ = 0).
//
// ⚠ NEDEN Core: Kuyumcu.Api → Kuyumcu.Web bağımlılığı vardır ama ters yön
//   yoktur; harita iki tarafın da gördüğü tek yerde (Core) durur.
//
// ⚠ NULL NE DEMEK: "bu rotanın modül kapısı YOKTUR" demektir — kimliği olan
//   herkes girer (panel, giriş, kılavuz, kendi ayarların) ya da kapı ROL
//   düzeyindedir (işletme ayarları = SAHİP). null bir "unuttum" değildir:
//   her null'ın gerekçesi aşağıda yazılıdır ve testle sayılır.
// ═════════════════════════════════════════════════════════════════════════════

public static class RotaModulleri
{
    /// <summary>Rota şablonu → yetki modülü anahtarı (null = modül kapısı yok).</summary>
    public static readonly IReadOnlyDictionary<string, string?> Harita =
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["/"] = null,                               // panel — herkes
            ["/giris"] = null,                          // kimlik öncesi
            ["/sifre-sifirla"] = null,                  // kimlik öncesi — PAKET AUTH-SIFIRLA ("Parolamı unuttum")
            ["/cariler"] = "cari",
            ["/urunler"] = "urun",
            ["/urunler/agac"] = "urun",
            ["/urunler/etiketler"] = "urun",
            ["/islemler"] = "islem",
            ["/emanet"] = "emanet",
            ["/kur"] = "kur",
            ["/kasalar"] = "kasa",
            ["/pos"] = "pos",
            ["/pos/tahsilatlar"] = "pos",
            ["/vezne"] = "vezne",
            ["/banka"] = "banka",
            ["/banka/ekstre/{IthalatId}"] = "banka",
            ["/faturalar"] = "fatura",
            // PAKET-E-BELGE (20.10): giden kutusu faturanın türevidir — aynı kapı, yeni anahtar açılmadı.
            ["/e-belgeler"] = "fatura",
            // PAKET P-19 (Y-11/Y-12): gider + virman. YENİ yetki anahtarı AÇILMADI —
            // gider nakit KASASINDAN çıkar, virman kasa/banka arasında olur;
            // kasayı göremeyen bu ekranı da göremez (kapsam kilidi).
            ["/gider"] = "kasa",
            // PAKET P-20 (Y-10): hurda stoğu / ramat çıkışı ekranı. Okuma uçları
            // [ModulYetkisi("islem")] altındadır; matris de oradan türer.
            ["/hurda"] = "islem",
            ["/gider-pusulalari"] = "fatura",
            ["/gider-pusulalari/{Id}/belge"] = "fatura",
            ["/raporlar"] = "rapor",
            ["/raporlar/kar-zarar"] = "rapor",
            ["/raporlar/vade-takibi"] = "rapor",
            ["/raporlar/islem-defteri"] = "rapor",
            ["/raporlar/satis-analizi"] = "rapor",
            ["/raporlar/kasa"] = "rapor",
            ["/fiyatlandirma"] = "fiyat-listesi",
            ["/sayim"] = "sayim",
            // PAKET EKSIK-B1-SATINALMA: modül kapısı YOK — sunucu kapısı ROL düzeyinde
            // (SatinalmaController / IrsaliyeController [RolGerekli] SAHIP/MUDUR/MUHASEBE).
            ["/satin-alma"] = null,
            ["/irsaliyeler"] = null,
            ["/kullanicilar"] = "kullanici",
            ["/yetkiler"] = "kullanici",
            ["/isletme"] = null,                        // SAHİP rol kapısı (AdminController)
            ["/ayarlar"] = null,                        // kendi hesabın
            ["/kilavuz"] = null,                        // yardım metni
            ["/fiyat/{Anahtar}"] = null,                // Y-14: oturumsuz canlı fiyat bağlantısı (anahtar sorguda; modül kapısı sunucuda KUR)
            ["/uyum"] = "uyum",
            ["/kvkk"] = "cari",                         // D1-19 KVKK aydınlatma / rıza
            ["/denetim-izi"] = "denetim-izi",
            ["/veri-disa-aktarim"] = "veri-aktarim",
            ["/veri-ice-aktarim"] = "veri-aktarim",
            ["/kurulum"] = "kurulum",
            ["/bildirimler"] = "bildirim",
            ["/evraklar"] = "evrak",
            ["/evraklar/takvim"] = "evrak",
            ["/siparisler"] = "siparis",
            ["/siparisler/{Id}"] = "siparis",
            ["/yedekler"] = "yedek",
            // PAKET P-17 (Y-7): vitrin panosu kur kotasyonunun türevidir —
            // YENİ yetki anahtarı açılmadı, kuru göremeyen panoyu da göremez.
            ["/vitrin"] = "kur",
            // PAKET EKSIK-KUCUK (B2-10): dükkân ekranı panonun aynısıdır — aynı kapı.
            ["/vitrin/ekran"] = "kur",
            // PAKET YASAL-1: yasal sınırlar — modül kapısı YOK, kapı ROL düzeyinde
            // ([RolGerekli] SAHIP/MUDUR/MUHASEBE); ulusal rakam, kişisel veri yok.
            ["/yasal-sinirlar"] = null,
            // PAKET S1-d: "Tüm ekranlar" karo sayfası YALNIZ GEZİNMEDİR — veri
            // okumaz, uç çağırmaz. Kapı KARO BAŞINADIR: her karo kendi
            // ekranının modül/rol kapısıyla süzülür (MenuTanimi.YetkiliMi).
            ["/ekranlar"] = null,
            // PAKET P-17 (Y-6): ortak A4 çıktı. Modül kapısı BELGE TÜRÜNE
            // göredir (fiş=islem · makbuz/ekstre=cari · emanet=emanet) ve
            // sunucuda CiktiServisi.Modul ile tür başına sorulur; tek bir
            // rota anahtarı bu kapıyı ya çok geniş ya çok dar yapardı.
            ["/cikti/{Tur}/{Id}"] = null,
        };

    /// <summary>
    /// Yetkilendirilebilir modüllerin TÜRETİLMİŞ listesi (alfabetik, tekilleştirilmiş).
    /// <c>Moduller.Tumu</c> bunu okur — elle ikinci bir liste tutulmaz.
    /// </summary>
    public static readonly string[] Modulleri =
        [.. Harita.Values.Where(v => v is not null).Select(v => v!).Distinct().OrderBy(x => x, StringComparer.Ordinal)];

    /// <summary>
    /// Modül anahtarının ekrandaki adı. UX-S31: ham veritabanı kodu ("fiyat-listesi")
    /// ekrana BASILMAZ. Geri dal YOKTUR — bilinmeyen anahtar <see cref="KeyNotFoundException"/>
    /// atar ve testi kırar; sessizce ham kod göstermez.
    /// </summary>
    public static string Etiket(string modul, Sozluk t) => modul switch
    {
        "banka" => t.Nav.Banka,
        "bildirim" => t.Nav.Bildirimler,
        "cari" => t.Nav.Cariler,
        "denetim-izi" => t.Nav.DenetimIzi,
        "emanet" => t.Nav.Emanet,
        "evrak" => t.Nav.Evraklar,
        "fatura" => t.Nav.Faturalar,
        "fiyat-listesi" => t.Nav.Fiyatlandirma,
        "islem" => t.Nav.Islemler,
        "kasa" => t.Nav.Kasalar,
        "kullanici" => t.Nav.Kullanicilar,
        "kur" => t.Nav.Kur,
        "kurulum" => t.Nav.Kurulum,
        "pos" => t.Nav.Pos,
        "rapor" => t.Nav.Raporlar,
        "sayim" => t.Nav.Sayim,
        "siparis" => t.Nav.Siparisler,
        "urun" => t.Nav.Urunler,
        "uyum" => t.Nav.Uyum,
        "vezne" => t.Nav.Vezne,
        "veri-aktarim" => t.Nav.VeriAktarimi,
        "yedek" => t.Nav.Yedekler,
        _ => throw new KeyNotFoundException($"Yetki modülü etiketi tanımsız: {modul}"),
    };
}
