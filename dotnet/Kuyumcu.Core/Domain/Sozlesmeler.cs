namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d — TEK SÖZLÜK (UX-S31: "aynı kavram tek ad, tek dil, tek kaynak").
//
// ⚠ NEDEN VAR: QA turu 1 fikstür ajanı aynı kavramın iki ayrı ekranda iki ayrı
// kelime dağarcığıyla istendiğini kanıtladı — kasa hareketinde yön GIRIS/CIKIS,
// banka hareketinde GELEN/GIDEN; üstelik banka DTO'sunun yorumu GIRIS/CIKIS
// diyordu. Kabul edilen değerler artık TEK yerde durur; iki DTO da buradan
// beslenir ve doğrulama mesajı kabul edilen değerleri SAYAR.
//
// Geriye uyum kararı (Mühür 2, "sessiz eşleme YOK"): karşı sözlüğün değeri
// (kasaya "GELEN") gelirse 400 döner ve mesaj doğru değerleri listeler.
// Sessizce eşlemek defteri yanlış yöne yazardı.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hareket yönü sözlükleri — kasa ve banka.</summary>
public static class HareketYonu
{
    // ── Kasa (nakit kasası): para kasaya girer / kasadan çıkar ──
    public const string KasaGiris = "GIRIS";
    public const string KasaCikis = "CIKIS";
    public static readonly string[] KasaHepsi = [KasaGiris, KasaCikis];
    public const string KasaDesen = "GIRIS|CIKIS";

    // ── Banka (banka hesabı): para hesaba gelir / hesaptan gider ──
    public const string BankaGelen = "GELEN";
    public const string BankaGiden = "GIDEN";
    public static readonly string[] BankaHepsi = [BankaGelen, BankaGiden];
    public const string BankaDesen = "GELEN|GIDEN";

    public const string KasaMesaj =
        "Kasa hareketi yönü yalnız GIRIS veya CIKIS olabilir (banka hareketlerinde GELEN/GIDEN kullanılır)";
    public const string BankaMesaj =
        "Banka hareketi yönü yalnız GELEN veya GIDEN olabilir (kasa hareketlerinde GIRIS/CIKIS kullanılır)";

    public static bool KasaGecerli(string? yon) => yon is not null && KasaHepsi.Contains(yon);
    public static bool BankaGecerli(string? yon) => yon is not null && BankaHepsi.Contains(yon);

    /// <summary>Kasa yönünün Türkçe görünen adı (ham kod ekrana basılmaz — UX-S31).</summary>
    public static string KasaAd(string? yon) => yon switch
    {
        KasaGiris => "Giriş", KasaCikis => "Çıkış", _ => yon ?? "",
    };

    /// <summary>Banka yönünün Türkçe görünen adı.</summary>
    public static string BankaAd(string? yon) => yon switch
    {
        BankaGelen => "Gelen", BankaGiden => "Giden", _ => yon ?? "",
    };
}

/// <summary>
/// MASAK kimlik belgesi türleri — TEK KAYNAK.
/// DTO doğrulaması (<see cref="Desen"/>), kural motoru ve ekran seçicisi
/// aynı diziden beslenir. Yeni tür eklenince üçü birden güncellenir.
/// </summary>
public static class KimlikBelgesi
{
    public const string TcKimlik = "TC_KIMLIK";
    public const string Pasaport = "PASAPORT";
    public const string SurucuBelgesi = "SURUCU_BELGESI";
    public const string MaviKart = "MAVI_KART";
    public const string VergiLevhasi = "VERGI_LEVHASI";   // tüzel kişi

    public static readonly IReadOnlyList<string> Hepsi =
        [TcKimlik, Pasaport, SurucuBelgesi, MaviKart, VergiLevhasi];

    public const string Desen = "TC_KIMLIK|PASAPORT|SURUCU_BELGESI|MAVI_KART|VERGI_LEVHASI";

    public static bool Gecerli(string? t) => t is not null && Hepsi.Contains(t);

    /// <summary>Türkçe görünen ad — seçici ve hata mesajı için.</summary>
    public static string Ad(string t) => t switch
    {
        TcKimlik => "T.C. Kimlik Kartı",
        Pasaport => "Pasaport",
        SurucuBelgesi => "Sürücü Belgesi",
        MaviKart => "Mavi Kart",
        VergiLevhasi => "Vergi Levhası",
        _ => t,
    };

    /// <summary>Hata mesajında kullanılan "kabul edilen değerler" listesi.</summary>
    public static string KabulEdilenler => string.Join(", ", Hepsi);

    public static string Mesaj =>
        $"Geçerli bir kimlik belgesi türü seçilmelidir — kabul edilen değerler: {KabulEdilenler}";
}

/// <summary>İletişim alanlarının biçim kuralları — cari, şube, kullanıcı TEK kural.</summary>
public static class Iletisim
{
    /// <summary>
    /// Telefon: yalnız rakam ve baştaki '+', boşluk/parantez/tire serbest ama
    /// RAKAM sayısı 10–13 arasında olmalı. "abc" 200 ile kabul ediliyordu (madde 19).
    /// </summary>
    public const string TelefonDesen = @"^\+?[0-9 ()\-]{10,20}$";

    public const string TelefonMesaj =
        "Telefon yalnız rakam, boşluk, parantez ve tire içerebilir; başında + olabilir (10–13 hane)";

    /// <summary>Desen + hane sayısı birlikte doğrulanır (desen tek başına yetmez).</summary>
    /// <remarks>
    /// GIRDI-AILESI-2 tur 3 (doğrulayıcı KRİTİK): eski kural (desen + 10–13 hane) ekrandaki Telefon
    /// bileşeninin kuralından (ITU E.164 TR: ulusal 10 hane, NDC aralığı) AYRIŞIYORDU — ekran "geçersiz"
    /// derken Kaydet açık kalıyor, sunucu 200 ile kaydediyordu ("0000000000", "05001112233").
    /// Artık TEK kaynak: <see cref="Girdi.TelefonDogrulayici"/>. Desen sabiti yalnız arama (CariArama) içindir.
    /// </remarks>
    public static bool TelefonGecerli(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return true;   // boş = verilmedi
        return Girdi.TelefonDogrulayici.Denetle(t) is null;
    }
}
