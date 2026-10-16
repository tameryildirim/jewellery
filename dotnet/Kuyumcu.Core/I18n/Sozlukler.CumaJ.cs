namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-J — BASİT MOD JARGON + SIĞMA ARTIKLARI (orkestratör demo taraması
// 25.09 06:45, 42 rota × BASİT × 1440).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ve Sozlukler.Katalog.cs ORTAK HUB'dır; aynı
//   anda başka şeritler (CUMA-S, CUMA-M) de sözlüğe yazıyor. Bu paketin metinleri
//   burada durur; BasitSozluk katmanına TEK satırla bağlanır (Katalog.cs Kur()).
//
// ⚠ D10: PRO sözlük DEĞİŞMEZ. Aşağıdaki değerler yalnız BASİT modda (tezgâhtar
//   yüzü) PRO metnin yerine geçer; PRO modda muhasebeci "storno/cari" okumaya
//   devam eder.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'dadır;
//   Tr ve En aynı kurucudan geçer, bir dilde alan unutulursa derleme kırılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// BASİT mod karşılıkları: alan adı = PRO sözlükteki yol ('_' ile ayrılır).
/// Her değer jargon (Cari/Storno/Vezne/Mutabakat/Pasife al/Denetim izi) içermez.
/// </summary>
public sealed record CumaJSadeMetinleri(
    string Evrak_DegismezYol,
    string Evrak_OnayMetin,
    string IceAktarim_KaynakCari,
    string IceAktarim_GeriAlindiMesaji);

/// <summary>
/// Her iki modda da geçerli KISA ekran metinleri (K3: kısa etiket iki satıra
/// bölünmesin). Uzun metin title/açıklama olarak korunur.
/// </summary>
public sealed record CumaJEkranMetinleri(
    string EvrakSutunTur,
    string EvrakSutunVeren,
    string EvrakSutunVade,
    string FaturaNoKesimdeKisa,
    string IceAktarimHazirlikBaslik);

public static class CumaJSozlugu
{
    public static readonly CumaJSadeMetinleri SadeTr = new(
        Evrak_DegismezYol: "Tutar ya da müşteri yanlış girildiyse evrakı karşılıksız işaretleyin (borç geri gelir) ya da girişi ters kayıtla geri alın; sonra doğru evrakı yeniden girin.",
        Evrak_OnayMetin: "Bu işlem müşterinin borcunu tutarı kadar GERİ GETİRİR ve deftere yeni bir satır yazar. Kayıt silinmez; geri dönmek için evrakı tahsil edin ya da ters kayıtla geri alın.",
        IceAktarim_KaynakCari: "Müşteri kartları",
        IceAktarim_GeriAlindiMesaji: "Aktarım geri alındı; kayıtlar kullanımdan kaldırıldı");

    public static readonly CumaJSadeMetinleri SadeEn = new(
        Evrak_DegismezYol: "If the amount or the customer is wrong, mark the cheque as bounced (the debt comes back) or undo the entry with a reverse record, then enter the correct cheque again.",
        Evrak_OnayMetin: "This brings the customer's debt BACK for the full amount and writes a new ledger row. Nothing is deleted; to go back, collect the cheque or undo it with a reverse record.",
        IceAktarim_KaynakCari: "Customer cards",
        IceAktarim_GeriAlindiMesaji: "Import undone; records retired");

    public static readonly CumaJEkranMetinleri EkranTr = new(
        EvrakSutunTur: "Tür",
        EvrakSutunVeren: "Kimden",
        EvrakSutunVade: "Vade",
        FaturaNoKesimdeKisa: "Kesimde verilir",
        IceAktarimHazirlikBaslik: "Dosyayı hazırlayın");

    public static readonly CumaJEkranMetinleri EkranEn = new(
        EvrakSutunTur: "Type",
        EvrakSutunVeren: "From",
        EvrakSutunVade: "Due",
        FaturaNoKesimdeKisa: "Set on issue",
        IceAktarimHazirlikBaslik: "Prepare the file");

    public static CumaJSadeMetinleri Sade(string dil) => dil == "en" ? SadeEn : SadeTr;
    // 4. tur (doğrulayıcı bulgusu): evrakı veren tedarikçi de olabilir; "Müşteri"
    // başlığı PRO'da yanlış bilgi veriyordu. BASİT: "Kimden" (jargonsuz, doğru);
    // PRO: "Veren" (spesifikasyonun önerdiği kısa ad). Tam ad ("Evrakı veren
    // müşteri") iki modda da title'da durur.
    public static readonly CumaJEkranMetinleri EkranProTr = EkranTr with { EvrakSutunVeren = "Veren" };
    public static readonly CumaJEkranMetinleri EkranProEn = EkranEn with { EvrakSutunVeren = "Given by" };

    /// <summary>Kısa ekran metinleri. <paramref name="basit"/> yalnız "Veren" sütununu değiştirir.</summary>
    public static CumaJEkranMetinleri Ekran(string dil, bool basit = true) =>
        dil == "en" ? (basit ? EkranEn : EkranProEn) : (basit ? EkranTr : EkranProTr);

    /// <summary>Katmanın değiştirdiği anahtar sayısı (test için).</summary>
    public static int AnahtarSayisi => Esleme(SadeTr).Count;

    /// <summary>PRO anahtar yolu → basit değer (lambda yol: yanlış anahtar derlenmez).</summary>
    internal static List<(System.Linq.Expressions.Expression<Func<Sozluk, string>> Yol, string Deger)> Esleme(CumaJSadeMetinleri b) =>
    [
        (s => s.Evrak.DegismezYol, b.Evrak_DegismezYol),
        (s => s.Evrak.OnayMetin, b.Evrak_OnayMetin),
        (s => s.IceAktarim.KaynakCari, b.IceAktarim_KaynakCari),
        (s => s.IceAktarim.GeriAlindiMesaji, b.IceAktarim_GeriAlindiMesaji),
    ];

    // ── Altın pozisyonu kartı (/raporlar, /panel) — ayrı sözlük (PozisyonSozlugu) ──

    private static readonly Lazy<PozisyonMetinleri> pozisyonSadeTr = new(() => PozisyonSozlugu.Tr with
    {
        Bos = "Henüz stokta, hurdada ya da müşteri hesaplarında altın yok.",
        DefterFarki = KatalogSozlugu.Al("tr").PozisyonDefterFarkiBasit,
        DegismezGerekce = "Bu sayı hesaplanır: vitrindeki stok, hurda ve müşterilerin altın bakiyesinden çıkar; elle değiştirilmez.",
        DuzeltmeYolu = "Yanlışsa kaynağını düzeltin: stok için sayım, müşteri hesabı için düzeltme fişi, hurda için hurda ekranı.",
    });

    private static readonly Lazy<PozisyonMetinleri> pozisyonSadeEn = new(() => PozisyonSozlugu.En with
    {
        Bos = "No gold in stock, scrap or customer balances yet.",
        DefterFarki = KatalogSozlugu.Al("en").PozisyonDefterFarkiBasit,
        DegismezGerekce = "This number is worked out from showcase stock, scrap and customers' gold balances; it cannot be typed in by hand.",
        DuzeltmeYolu = "If it is wrong, fix where it comes from: a stock count for stock, a correction slip for a customer balance, the scrap screen for scrap.",
    });

    /// <summary>Altın pozisyonu kartı metinleri: BASİT modda jargonsuz kopya, PRO'da PozisyonSozlugu aynen.</summary>
    public static PozisyonMetinleri Pozisyon(string dil, bool basit) =>
        !basit ? PozisyonSozlugu.Al(dil) : dil == "en" ? pozisyonSadeEn.Value : pozisyonSadeTr.Value;
}
