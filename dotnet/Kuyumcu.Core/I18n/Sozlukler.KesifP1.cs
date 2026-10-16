namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P1 · PARA-VERİ-YETKİ (keşif 30.09 · #5 #7 #10 #12 #30 #31 #44
// #45 #60 #61 #71 #72 + KIYAS TEMA-TEK + TOPLU ZO-ISLEM-A "geri al sonrası").
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs HUB ve Sozlukler.Katalog.cs yalnız P8'in
//   (triyaj § Ortak kurallar). Bu paketin YENİ metni yalnız burada durur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da;
//   Tr ve En aynı kurucudan geçer.
// ⚠ Rol adı TEK KAYNAK: sunucunun 403 metni (RolFiltresi) ve ekranın yetki
//   kapısı (YetkiKapisi) ham rol kodunu (SAHIP · MUDUR) BURADAN çevirir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P1 metinleri (bir dilde).</summary>
public sealed record KesifP1Metinleri(
    // ── #72 · yetki kapısı ──
    string RolSahip,
    string RolMudur,
    string RolMuhasebe,
    string RolTezgahtar,
    string RolBaglac,
    string YetkiKapisiBaslik,
    string YetkiKapisiAciklama,
    string YetkiKapisiPanel,
    string SunucuGerekliRol,
    // ── #7 · TEMA-TEK (Ayarlar) ──
    string BenimAciklama,
    string BenimTemaNotu,
    string AyrintiBaslik,
    // ── #12 · #71 · İşletme kritik ayarları ──
    string AyarKaydet,
    string AyarVazgec,
    string AyarDegisiklikVar,
    string AyarOnayBaslik,
    string AyarOnayDugme,
    string AyarOnayGeriDonus,
    string AyarMaliyetEtki,
    string AyarKurEtki,
    string AyarModEtki,
    string AyarVezneEtki,
    string AyarBelirsiz,
    string AyarKaydedildi,
    // ── #30 · #31 · #5 · POS ──
    string PosKomisyonYuzde,
    string PosKomisyonYuzdeAciklama,
    string PosKomisyonAralik,
    string PosValorAralik,
    string PosTaksitAralik,
    string TamSayiHata,
    /// <summary>{0} = izin verilen en çok ondalık hane (tur 2: POS komisyon yüzdesi 2 hane → kesir 4 hane = decimal(5,4)).</summary>
    string OndalikSiniri,
    // ── TOPLU ZO-ISLEM-A · POS geri al ──
    string PosBankaYatmasiYok,
    // ── Tur 3 (orkestratör 18:10) ──
    /// <summary>POS cihaz tablosu ilk sütunu: cihaz adı + altında banka.</summary>
    string PosCihazBanka,
    /// <summary>{0} = valör günü (komisyon hücresinin alt satırı).</summary>
    string PosValorAlt,
    /// <summary>Fiyat paneli sınır uyarısı (UX-S29): {0} = süzgece uyan toplam, {1} = görünen.</summary>
    string FiyatDahaFazla,
    string FiyatUrunAra,
    string FiyatUrunAraOrnek,
    /// <summary>İşletme sayfa açıklaması — ham rol kodu (SAHİP) yok.</summary>
    string IsletmeAciklama,
    string IsletmeYalnizSahip,
    /// <summary>"AI modülü" yerine sade ad.</summary>
    string AkilliOneriler,
    /// <summary>Vezne ayrımı açıklaması — "dört göz kuralı" jargonu yok.</summary>
    string VezneAyrimAciklama);

public static class KesifP1Sozlugu
{
    public static readonly KesifP1Metinleri Tr = new(
        RolSahip: "işletme sahibi",
        RolMudur: "müdür",
        RolMuhasebe: "muhasebe",
        RolTezgahtar: "tezgâhtar",
        RolBaglac: " ya da ",
        YetkiKapisiBaslik: "Bu sayfayı yalnız {0} açabilir",
        YetkiKapisiAciklama: "Hesabınız: {0}. Bu sayfaya ihtiyacınız varsa işletme sahibinden isteyin.",
        YetkiKapisiPanel: "Ana sayfaya dön",
        SunucuGerekliRol: "Bu işlem için gerekli rol: {0}",
        BenimAciklama: "Bu iki seçim YALNIZ SİZİN içindir ve oturum açtığınız her tezgâhta geçerlidir. Birini işletme varsayılanında bırakırsanız sahibin seçimi izlenir.",
        BenimTemaNotu: "Aydınlık / karanlık seçimi yukarıdaki Görünüm kartındadır (tek yerden seçilir).",
        AyrintiBaslik: "Ayrıntılı görünüm ayarları (tek tek seçim)",
        AyarKaydet: "Değişikliği kaydet",
        AyarVazgec: "Vazgeç",
        AyarDegisiklikVar: "Kaydedilmemiş değişiklik: {0}",
        AyarOnayBaslik: "İşletme ayarını değiştir",
        AyarOnayDugme: "Değiştir",
        AyarOnayGeriDonus: "İstediğiniz zaman aynı yerden eski değere döndürebilirsiniz.",
        AyarMaliyetEtki: "Maliyet yöntemi: {0} → {1}. Bundan sonraki satışların maliyeti {1} ile hesaplanır.",
        AyarKurEtki: "Kur kaynağı: {0} → {1}. Kurlar bundan sonra {1} kaynağından çekilir.",
        AyarModEtki: "Varsayılan ekran: {0} → {1}. Kendi ekranını seçmemiş kullanıcılar {1} ekranla açılır.",
        AyarVezneEtki: "Vezne ayrımı: {0} → {1}.",
        AyarBelirsiz: "belirlenmedi",
        AyarKaydedildi: "Kaydedildi: {0}",
        PosKomisyonYuzde: "Komisyon (%)",
        PosKomisyonYuzdeAciklama: "Yüzde olarak yazın (ör. 1,85). Bu oran yasadan değil, bankanızla yaptığınız sözleşmeden gelir.",
        PosKomisyonAralik: "Komisyon 0 ile 100 arasında olmalı (ör. 1,85).",
        PosValorAralik: "Valör günü 0 ile 90 arasında tam sayı olmalı.",
        PosTaksitAralik: "Taksit sayısı 2 ile 12 arasında tam sayı olmalı.",
        TamSayiHata: "Tam sayı yazın (ör. 3). Küsuratlı değer kabul edilmez.",
        OndalikSiniri: "Virgülden sonra en çok {0} hane yazın.",
        PosBankaYatmasiYok: "Eşleşmemiş banka yatması bulunamadı.",
        PosCihazBanka: "Cihaz ve banka",
        PosValorAlt: "Valör {0} gün",
        FiyatDahaFazla: "Daha fazlası var: {0} üründen {1} tanesi görünüyor. Kategori seçin ya da arayın.",
        FiyatUrunAra: "Ürün ara",
        FiyatUrunAraOrnek: "Kod ya da ad",
        IsletmeAciklama: "İşletme bilgisi, abonelik planı ve şube yönetimi.",
        IsletmeYalnizSahip: "Bu ayarı yalnız işletme sahibi değiştirebilir.",
        AkilliOneriler: "Akıllı öneriler",
        VezneAyrimAciklama: "Açıkken fişi hazırlayan kişi aynı fişin parasını alamaz; parayı başka biri alır.");

    public static readonly KesifP1Metinleri En = new(
        RolSahip: "the shop owner",
        RolMudur: "a manager",
        RolMuhasebe: "accounting",
        RolTezgahtar: "a sales clerk",
        RolBaglac: " or ",
        YetkiKapisiBaslik: "Only {0} can open this page",
        YetkiKapisiAciklama: "Your account: {0}. If you need this page, ask the shop owner.",
        YetkiKapisiPanel: "Back to home",
        SunucuGerekliRol: "Role required for this action: {0}",
        BenimAciklama: "These two choices apply to you only, on every counter you sign in at. Leave one on the shop default to follow the owner's choice.",
        BenimTemaNotu: "Light / dark is chosen on the Appearance card above (one place only).",
        AyrintiBaslik: "Detailed appearance settings (one by one)",
        AyarKaydet: "Save change",
        AyarVazgec: "Cancel",
        AyarDegisiklikVar: "Unsaved change: {0}",
        AyarOnayBaslik: "Change business setting",
        AyarOnayDugme: "Change",
        AyarOnayGeriDonus: "You can switch back to the old value from the same place at any time.",
        AyarMaliyetEtki: "Costing method: {0} → {1}. The cost of future sales is calculated with {1}.",
        AyarKurEtki: "Rate source: {0} → {1}. Rates are fetched from {1} from now on.",
        AyarModEtki: "Default screen: {0} → {1}. Users who have not chosen their own screen open in {1}.",
        AyarVezneEtki: "Cashier separation: {0} → {1}.",
        AyarBelirsiz: "not set",
        AyarKaydedildi: "Saved: {0}",
        PosKomisyonYuzde: "Commission (%)",
        PosKomisyonYuzdeAciklama: "Enter as a percentage (e.g. 1.85). This rate comes from your agreement with the bank, not from the law.",
        PosKomisyonAralik: "Commission must be between 0 and 100 (e.g. 1.85).",
        PosValorAralik: "Value days must be a whole number between 0 and 90.",
        PosTaksitAralik: "Installment count must be a whole number between 2 and 12.",
        TamSayiHata: "Enter a whole number (e.g. 3). Fractions are not accepted.",
        OndalikSiniri: "Use at most {0} decimal places.",
        PosBankaYatmasiYok: "No unmatched bank deposit found.",
        PosCihazBanka: "Terminal and bank",
        PosValorAlt: "Value date {0} days",
        FiyatDahaFazla: "There is more: showing {1} of {0} products. Pick a category or search.",
        FiyatUrunAra: "Search products",
        FiyatUrunAraOrnek: "Code or name",
        IsletmeAciklama: "Business details, subscription plan and branch management.",
        IsletmeYalnizSahip: "Only the shop owner can change this setting.",
        AkilliOneriler: "Smart suggestions",
        VezneAyrimAciklama: "When on, the person who prepared a ticket cannot take its payment; someone else takes it.");

    public static KesifP1Metinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// Ham rol kodu → okunur ad (UX-S31). Bilinmeyen kod UYDURULMAZ: kendisiyle
    /// döner (Anayasa md.1 — bilinmeyen tahmin edilmez).
    /// </summary>
    public static string RolAdi(string? kod, string dil = "tr")
    {
        var m = Al(dil);
        return kod switch
        {
            "SAHIP" => m.RolSahip,
            "MUDUR" => m.RolMudur,
            "MUHASEBE" => m.RolMuhasebe,
            "TEZGAHTAR" => m.RolTezgahtar,
            _ => kod ?? "",
        };
    }

    /// <summary>Rol listesi → "dükkân sahibi ya da müdür".</summary>
    public static string RolListesi(IEnumerable<string> kodlar, string dil = "tr") =>
        string.Join(Al(dil).RolBaglac, kodlar.Select(k => RolAdi(k, dil)));
}
