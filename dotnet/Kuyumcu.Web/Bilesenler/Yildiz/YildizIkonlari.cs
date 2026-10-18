namespace Kuyumcu.Web.Bilesenler.Yildiz;

/// <summary>
/// PAKET YILDIZ-4B — "Tüm ekranlar" karelerinin ve Yıldız ekranlarının ÇİZGİ İKONLARI.
/// TEK DOĞRULUK: tuval <c>Tum4.dc.html</c> / <c>Tezgah4.dc.html</c> / <c>Musteri4.dc.html</c>
/// (24×24 görünüm kutusu, tek <c>path</c>, çizgi). Değerler tuvalden BİREBİR kopyalandı
/// (kurucu 09.10: "birebir tasarım neyse aynısını"); sıra = <c>MenuTanimi.Ogeler</c> sırası
/// = tuvaldeki kare sırası. Yalnız Yıldız şemasında çizilir.
/// </summary>
public static class YildizIkonlari
{
    /// <summary>Menü yolu → tuvaldeki kare ikonunun <c>d</c> değeri (39 ekran).</summary>
    public static readonly IReadOnlyDictionary<string, string> Kare = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["/"] = "M4 4h7v7H4zM13 4h7v4h-7zM13 10h7v10h-7zM4 13h7v7H4z",   // Panel
        ["/islemler"] = "M5 8h14l-1 12H6zM9 8V6a3 3 0 0 1 6 0v2",   // Satış
        ["/emanet"] = "M5 10h14v10H5zM8 10V7a4 4 0 0 1 8 0v3M12 14v2",   // Emanet
        ["/siparisler"] = "M9 4h6v3H9zM7 5.5H5V21h14V5.5h-2M9 14l2 2 4-4",   // Sipariş ve tamir
        ["/bildirimler"] = "M6 9a6 6 0 0 1 12 0v5l2 3H4l2-3zM10 20a2 2 0 0 0 4 0",   // Bildirimler
        ["/cariler"] = "M9 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM3 20c0-3.3 2.7-6 6-6s6 2.7 6 6M16 4.5a3.5 3.5 0 0 1 0 6.5M18 14c2 .7 3 2.8 3 6",   // Müşteriler
        ["/urunler"] = "M7 3h10l4 6-9 12L3 9zM3 9h18M10 3l-2 6 4 12 4-12-2-6",   // Ürünler
        ["/urunler/agac"] = "M4 4h6v5H4zM14 10h6v5h-6zM14 17h6v4h-6zM7 9v10.5h7M7 12.5h7",   // Ürün ağacı
        ["/urunler/etiketler"] = "M3 12V4h8l10 10-8 8zM7.5 8.5h.01",   // Etiketler
        ["/fiyatlandirma"] = "M6 3h12v18H6zM9 8h6M9 12h6M9 16h3",   // Fiyat listeleri
        ["/sayim"] = "M9 5h10M9 12h10M9 19h10M4 5l1 1 2-2M4 12l1 1 2-2M4 19h2",   // Sayım
        ["/kasalar"] = "M4 5h16v14H4zM12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM4 9h2M18 9h2",   // Kasalar
        ["/vezne"] = "M4 6h16M4 12h16M4 18h10M17 16l3 2-3 2",   // Kasa kuyruğu
        ["/pos"] = "M7 2h10v20H7zM9 5h6v4H9zM10 13h.01M14 13h.01M10 17h.01M14 17h.01",   // POS cihazları
        ["/pos/tahsilatlar"] = "M3 6h18v12H3zM3 10h18M15 15h3",   // POS alacakları
        ["/banka"] = "M3 10l9-6 9 6M5 10v8M9.5 10v8M14.5 10v8M19 10v8M3 20h18",   // Banka
        ["/evraklar"] = "M3 7h18v10H3zM6 13h6M15 13h3M6 10h3",   // Çek ve senet
        ["/evraklar/takvim"] = "M4 5h16v15H4zM4 9h16M8 3v4M16 3v4M8 13h3v3H8z",   // Vade takvimi
        ["/kur"] = "M4 17l5-5 4 3 7-8M15 7h5v5",   // Kur
        ["/gider"] = "M4 8h13l-3-3M20 16H7l3 3",   // Gider ve virman
        ["/hurda"] = "M3 19h18l-2-6H5zM7 13l1.5-5h7L17 13",   // Hurda ve ramat
        ["/faturalar"] = "M6 3h9l3 3v15l-2.5-1.5L13 21l-2.5-1.5L8 21l-2-1.5zM9 9h6M9 13h6",   // Faturalar
        ["/gider-pusulalari"] = "M6 3h12v18H6zM9 7h6M9 11h6M9 15h3M14 16l1.5 1.5L18 15",   // Gider pusulaları
        ["/uyum"] = "M12 3l8 3v6c0 4.5-3.4 8.3-8 9-4.6-.7-8-4.5-8-9V6zM9 12l2 2 4-4",   // Uyum (MASAK)
        ["/kvkk"] = "M6 11V8a6 6 0 0 1 12 0v3M5 11h14v10H5zM12 15v2",   // KVKK (kilit)
        ["/yasal-sinirlar"] = "M12 4v16M5 8h14M5 8l-2 6h4zM19 8l-2 6h4zM8 20h8",   // Yasal sınırlar
        ["/raporlar"] = "M4 20V10M10 20V4M16 20v-7M2 20h20",   // Rapor merkezi
        ["/raporlar/kar-zarar"] = "M4 4v16h16M8 14l3-3 3 3 5-6",   // Kâr-zarar
        ["/raporlar/vade-takibi"] = "M12 7v5l3 2M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18z",   // Vade takibi
        ["/raporlar/islem-defteri"] = "M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3zM5 17a3 3 0 0 1 3-3h11M9 8h6",   // İşlem defteri
        ["/raporlar/satis-analizi"] = "M12 3a9 9 0 1 0 9 9h-9zM15 3.5a8 8 0 0 1 5.5 5.5H15z",   // Satış analizi
        ["/raporlar/kasa"] = "M4 5h16v14H4zM8 15v-3M12 15V9M16 15v-5",   // Kasa raporu
        ["/kullanicilar"] = "M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21c0-4.4 3.6-8 8-8s8 3.6 8 8",   // Kullanıcılar
        ["/isletme"] = "M3 21h18M5 21V8l7-4 7 4v13M9 21v-6h6v6",   // İşletme
        ["/yetkiler"] = "M15 7a4 4 0 1 1-3.5 5.9L4 20H2v-3l7.1-7.5A4 4 0 0 1 15 7zM16 8h.01",   // Yetkiler
        ["/denetim-izi"] = "M3 12a9 9 0 1 0 3-6.7M3 4v5h5M12 8v4l3 2",   // Kim ne yaptı
        ["/kurulum"] = "M4 6h9M17 6h3M15 4v4M4 12h3M11 12h9M9 10v4M4 18h11M19 18h1M17 16v4",   // Kurulum
        ["/veri-disa-aktarim"] = "M12 15V3M7 8l5-5 5 5M4 15v5h16v-5",   // Veri dışa aktar
        ["/veri-ice-aktarim"] = "M12 3v12M7 10l5 5 5-5M4 15v5h16v-5",   // Veri içe aktar
        ["/yedekler"] = "M4 6c0-1.7 3.6-3 8-3s8 1.3 8 3-3.6 3-8 3-8-1.3-8-3zM4 6v12c0 1.7 3.6 3 8 3s8-1.3 8-3V6M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3",   // Yedekler
        // YILDIZ-4C tur 3: tuvalden SONRA eklenen iki ekran (EKSIK-B1-SATINALMA) tuvalde YOK → menünün kendi seçimiyle AYNI
        // tuval ikonu (MenuTanimi: /satin-alma = Ikonlar.Urunler, /irsaliyeler = Ikonlar.Faturalar) — yeni çizim uydurulmaz (belirsiz eşleşme).
        ["/satin-alma"] = "M7 3h10l4 6-9 12L3 9zM3 9h18M10 3l-2 6 4 12 4-12-2-6",   // Satın alma (= tuval Ürünler)
        ["/irsaliyeler"] = "M6 3h9l3 3v15l-2.5-1.5L13 21l-2.5-1.5L8 21l-2-1.5zM9 9h6M9 13h6",   // İrsaliyeler (= tuval Faturalar)
    };

    /// <summary>Tuval Tezgah4 giriş kutusu: barkod çizgileri.</summary>
    public const string Barkod = "M4 6v12M7 6v12M10.5 6v12M14 6v12M16.5 6v12M20 6v12";

    /// <summary>Tuval Musteri4/Tum4 arama kutusu: büyüteç.</summary>
    public const string Ara = "M20 20l-3.5-3.5";

    /// <summary>Tuval Tezgah4 "Fark 0 ₺" onay işareti.</summary>
    public const string Onay = "M5 12.5l4.5 4.5L19 7.5";

    /// <summary>YILDIZ-4C: tuval ".ekle" kalıbının artısı (Y_Isletme "Logo ekle": M12 5v14M5 12h14).</summary>
    public const string Arti = "M12 5v14M5 12h14";

    /// <summary>YILDIZ-4C: açık ".ekle"nin kapatma işareti (artının yatay çizgisi).</summary>
    public const string Eksi = "M5 12h14";
}
