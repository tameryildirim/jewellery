namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-EKRAN — İNGİLİZCE MODDA TÜRKÇE KALAN EKRAN ADLARI (TR + EN)
//
// KURUCU BULGUSU (08.10): "temaları yaparken İngilizceye çevirdiğimizde ekran
// isimleri Türkçe kalıyor". KÖK NEDEN (gerçek Chrome taramasıyla ölçüldü):
//   MenuAdlari.Icin(Sozluk t) dili `ReferenceEquals(t, Sozlukler.En)` ile
//   anlıyordu. BASİT mod sözlüğü (BasitSozluk) PRO sözlüğün KOPYASIDIR → kimlik
//   değişir → BASİT + EN kullanıcıda menü, "Tüm ekranlar" karoları, favori
//   düğmeleri ve gruplar TÜRKÇE kalıyordu. Çözüm: Sozluk.DilKodu (kopyada
//   korunur) — kimlik karşılaştırması bir daha YAPILMAZ.
//
// BU DOSYA: sunucudan Türkçe gelen ve ekranda olduğu gibi basılan KATALOG
// adlarının (kur kalemi, zamanlanmış iş, plan, sistem cümleleri) İngilizce
// karşılığını TEK yerde tutar. Anahtar = sunucunun kararlı KODU (kur kodu, iş
// kodu); kodu olmayanlar için sunucu metninin AYNISI (tam eşleşme).
//
// ⚠ Eşleşme yoksa sunucu metni AYNEN döner — ad uydurulmaz (Anayasa md.1).
// ⚠ KİŞİ/VERİ ADLARI ÇEVRİLMEZ: müşteri, ürün, şube, kullanıcı adı, denetim
//   kaydı metni (DB'de saklanan olay satırı) bu sözlüğe GİRMEZ.
// ⚠ DriftTesti: DilEkranTestleri, buradaki Türkçe anahtarları sunucu sabitleriyle
//   karşılaştırır; sunucu metni değişirse test kırmızı olur (sessiz bayatlama yok).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucu kaynaklı katalog adlarının EN karşılıkları.</summary>
public static class DilEkranSozlugu
{
    private static bool En(string dil) => dil == "en";

    // ── Kur kalemleri (KurModulu.Adlar ile aynı kodlar) ──────────────────────
    private static readonly IReadOnlyDictionary<string, string> KurEn = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["HAS"] = "Pure gold (gram)",
        ["GRAM"] = "Gold (gram)",
        ["KULCE"] = "Gold bar",
        ["CEYREK_YENI"] = "New quarter coin",
        ["CEYREK_ESKI"] = "Old quarter coin",
        ["YARIM_YENI"] = "New half coin",
        ["YARIM_ESKI"] = "Old half coin",
        ["TAM_YENI"] = "New full coin",
        ["TAM_ESKI"] = "Old full coin",
        ["ATA_YENI"] = "New Ata coin",
        ["ATA_ESKI"] = "Old Ata coin",
        ["RESAT"] = "Resat gold coin",
        ["HAMIT"] = "Hamit gold coin",
        ["GREMSE_YENI"] = "New Gremse coin",
        ["GREMSE_ESKI"] = "Old Gremse coin",
        ["BESLI"] = "Ata five-coin",
        ["BILEZIK_22"] = "22-carat bracelet",
        ["AYAR_14"] = "14-carat gold",
        ["AYAR_18"] = "18-carat gold",
        ["HURDA_22"] = "22-carat scrap",
        ["GUMUS"] = "Silver (gram)",
        ["ONS"] = "Gold ounce (ONS)",
    };

    /// <summary>Kur kaleminin ekran adı; kod tanınmıyorsa sunucunun adı AYNEN.</summary>
    public static string KurAdi(string dil, string? kod, string sunucuAdi) =>
        En(dil) && kod is not null && KurEn.TryGetValue(kod, out var ad) ? ad : sunucuAdi;

    /// <summary>Çevrilen kur kodları (test için).</summary>
    public static IReadOnlyCollection<string> KurKodlari => KurEn.Keys.ToArray();

    // ── Zamanlanmış işler (IZamanlanmisIs.Kod) ───────────────────────────────
    private static readonly IReadOnlyDictionary<string, string> IsEn = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["MASAK_SIB_SURESI"] = "MASAK reporting deadline",
        ["SAKLAMA_ONIZLEME"] = "Retention period preview",
        ["SAKLAMA_UYGULA"] = "Masking of identity data past retention",
        ["STOPAJ_HATIRLATMA"] = "Withholding tax payment reminder",
        ["TEVSIK_KURTARMA"] = "Cash limit breach scan",
        ["VADE_HATIRLATMA"] = "Due date reminder",
    };

    /// <summary>Zamanlanmış işin ekran adı; kod tanınmıyorsa sunucunun adı AYNEN.</summary>
    public static string IsAdi(string dil, string? kod, string sunucuAdi) =>
        En(dil) && kod is not null && IsEn.TryGetValue(kod, out var ad) ? ad : sunucuAdi;

    /// <summary>Çevrilen iş kodları (test için).</summary>
    public static IReadOnlyCollection<string> IsKodlari => IsEn.Keys.ToArray();

    // ── Sunucunun hazır cümleleri (tam eşleşme) ──────────────────────────────
    private static readonly IReadOnlyDictionary<string, string> CumleEn = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Süresi dolmuş kişisel veri yok."] = "No personal data past its retention period.",
        ["Başlangıç"] = "Starter",
        ["Kurumsal"] = "Corporate",
        ["Plan yükseltme işletme hesabından yapılamaz: abonelik hakkını genişletmek yetkili operatör onayı gerektirir. Talebinizi yöneticinize iletin; plan indirme işlemini buradan kendiniz yapabilirsiniz."]
            = "Upgrades cannot be made from the business account: widening subscription rights requires an authorised operator's approval. Pass your request to your manager; you can downgrade the plan yourself here.",
        ["FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar"]
            = "DIFFERENCE = sales total − invoices linked to receipts − invoices not linked to receipts",
    };

    /// <summary>Çevrilen sunucu cümleleri (test için; anahtar = sunucunun Türkçe metni).</summary>
    public static IReadOnlyCollection<string> SunucuCumleleri => CumleEn.Keys.ToArray();

    /// <summary>
    /// Sunucunun hazır cümlesinin ekran metni. Tam eşleşme yoksa metin AYNEN
    /// döner (kullanıcı verisi ve bilinmeyen cümle çevrilmez).
    /// </summary>
    public static string SunucuMetni(string dil, string? metin)
    {
        if (metin is null) return "";
        if (!En(dil)) return metin;
        if (CumleEn.TryGetValue(metin, out var ad)) return ad;
        // "{n} kayıtta kimlik alanları maskelenecek; tutar, tarih ve kademe korunacak."
        const string son = " kayıtta kimlik alanları maskelenecek; tutar, tarih ve kademe korunacak.";
        if (metin.EndsWith(son, StringComparison.Ordinal) && int.TryParse(metin[..^son.Length], out var n))
            return $"Identity fields will be masked in {n} records; amount, date and tier are kept.";
        return metin;
    }

    /// <summary>Stok sayımı penceresindeki "kayıtlı adet" etiketi (eskiden çıplak Türkçe literal).</summary>
    public static string KayitliAdet(string dil) => En(dil) ? "Recorded quantity" : "Kayıtlı adet";

    // ── Yaptırım listesi dayanağı (title ipucu) ──────────────────────────────
    /// <summary>Yaptırım listesi dayanak ipucu — mevzuat numaraları AYNEN, yalnız bağlaç çevrilir.</summary>
    public static string YaptirimDayanak(string dil) => En(dil)
        ? "Basis: Law No. 6415 art. 10 and 12; Law No. 7262 art. 3; MASAK General Communiqué Serial No. 34 art. 5"
        : Yasal3Sozlugu.YaptirimDayanak;

    // ── Kılavuz bölüm başlıkları (kilavuz.html çıpalarıyla aynı anahtarlar) ──
    private static readonly IReadOnlyDictionary<string, string> KilavuzEn = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tanitim"] = "Introduction and basic concepts",
        ["baslat"] = "Starting the application",
        ["giris"] = "Signing in",
        ["arayuz"] = "Interface tour",
        ["kur"] = "Rate bar (live)",
        ["panel"] = "Dashboard",
        ["cariler"] = "Accounts",
        ["urunler"] = "Products",
        ["islemler"] = "Transactions (sales · purchases · swaps · transfers)",
        ["kasalar"] = "Cash registers",
        ["banka"] = "Bank",
        ["faturalar"] = "Invoices and jeweller's VAT",
        ["raporlar"] = "Reports",
    };

    /// <summary>Kılavuz bölümünün ekran adı; çıpa tanınmıyorsa Türkçe başlık AYNEN.</summary>
    public static string KilavuzBolum(string dil, string capa, string trBaslik) =>
        En(dil) && KilavuzEn.TryGetValue(capa, out var ad) ? ad : trBaslik;

    /// <summary>Çevrilen kılavuz çıpaları (test için).</summary>
    public static IReadOnlyCollection<string> KilavuzCapalari => KilavuzEn.Keys.ToArray();

    // ── Birimler ve ayar adları (Bicim.* ve KurKaynakAdi tek kaynaktan alır) ──
    // KURUCU BULGUSU SINIFI (08.10 tasarım hakemi): "13,8760 has", "3 adet", "22 Ayar" EN modda
    // Türkçe kalıyordu — rakam biçimi (tr-TR) dile bağlı DEĞİL kuraldır, BİRİM/AD dile bağlıdır.

    /// <summary>Has gram birimi: TR "has", EN "fine" (Türk kuyumculuğunda has = saf altın).</summary>
    public static string HasBirimi(string dil) => En(dil) ? "fine" : "has";

    /// <summary>Adet birimi: TR "adet", EN "pcs".</summary>
    public static string AdetBirimi(string dil) => En(dil) ? "pcs" : "adet";

    /// <summary>
    /// Ayar adı. TR: çekirdek tanımdaki ad AYNEN ("22 Ayar", "925 Gümüş"). EN: altın "22k",
    /// 995 "995 Commercial fine", gümüş/platin/paladyum "925 Silver" · "950 Platinum" · "500 Palladium",
    /// çelik "Stainless steel". Tanınmayan kod için çekirdek ad AYNEN (ad uydurulmaz).
    /// </summary>
    public static string AyarAdi(string dil, string ayarId, string trAd)
    {
        if (!En(dil)) return trAd;
        if (ayarId == "K995") return "995 Commercial fine";
        if (ayarId == "CELIK") return "Stainless steel";
        var sayi = new string(ayarId.Where(char.IsDigit).ToArray());
        if (sayi.Length == 0) return trAd;
        if (ayarId.StartsWith('K')) return sayi + "k";
        if (ayarId.StartsWith("PT", StringComparison.Ordinal)) return sayi + " Platinum";
        if (ayarId.StartsWith("PD", StringComparison.Ordinal)) return sayi + " Palladium";
        if (ayarId.StartsWith('G')) return sayi + " Silver";
        return trAd;
    }

    /// <summary>Maden adı: TR çekirdek ad AYNEN; EN Gold/Silver/Platinum/Palladium/Steel/Rhodium. Tanınmayan kod AYNEN.</summary>
    public static string MadenAdi(string dil, string kod, string trAd) => !En(dil) ? trAd : kod switch
    {
        "ALTIN" => "Gold",
        "GUMUS" => "Silver",
        "PLATIN" => "Platinum",
        "PALADYUM" => "Palladium",
        "CELIK" => "Steel",
        "RODYUM" => "Rhodium",
        _ => trAd,
    };
}
