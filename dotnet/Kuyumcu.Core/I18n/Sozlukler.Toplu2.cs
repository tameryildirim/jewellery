namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-2 — toplu tasarım kuyruğunun KALAN görünüm maddeleri (01.10).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni
//   metin paketin kendi dosyasına). HUB metinleri DEĞİŞMEDİ; ekran bu dosyadaki
//   karşılığı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ⚠ Mevzuat hükmü YOK: metinler bugünkü davranışı anlatır, kural koymaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-2 metinleri (bir dilde).</summary>
public sealed record Toplu2Metinleri(
    // ── Kurulum · eksi açılış gerekçesi (PRO) — iç kalem kodu "(K-24)" ekrana basılmaz (karar 2) ──
    string KurulumNegatifAcilisPro,
    // ── Veri içe aktar · şablon düğmesi — "(CSV)" teknik eki yok ──
    string IceSablonIndir,
    // ── Ürünler PRO tablo başlığı · "Ürün grubu" kısa adı (tam ad title'da) ──
    string UrunGrubuKisa,
    // ── Döviz Veresiye / Tahsilat çekmecesi (tur 2, tasarım hakemi KRİTİK) — tutar alanı ZORUNLU ve sade adlı;
    //    PRO notları sade dille ("bu sürümde YOKTUR" ekrana basılmaz); BASİT'te not YOK. ──
    string DovizTutar,
    string DovizFisBacagiNot,
    string DovizVadeNot,
    // ── Basılı cari ekstresi: belge no yerine ekstre tarihi (cari kimliği belge numarası değildir) ──
    string EkstreTarihi);

public static class Toplu2Sozlugu
{
    public static readonly Toplu2Metinleri Tr = new(
        KurulumNegatifAcilisPro: "Kasa ve banka açılışı neden eksi olamaz: kasada eksi para, banka mevduatında eksi bakiye fiziken bulunmaz. Eksi bakiye bir BORÇTUR ve borcun yeri caridir. Kredili mevduat (KMH) ayrı bir üründür ve açılışta eksi bakiye olarak girilmez.",
        IceSablonIndir: "Boş şablonu indir",
        UrunGrubuKisa: "Grup",
        DovizTutar: "Tutar ({0})",
        DovizFisBacagiNot: "Satış fişinde dövizle alınan ödeme müşteriye döviz borcu yazmaz; döviz borcu yalnız bu formla açılır.",
        DovizVadeNot: "Döviz borcu için vade tutulmaz; gecikme listesinde görünmez.",
        EkstreTarihi: "Ekstre tarihi");

    public static readonly Toplu2Metinleri En = new(
        KurulumNegatifAcilisPro: "Why a cash or bank opening cannot be negative: neither a cash drawer nor a bank deposit can physically hold a negative balance. A negative balance is a DEBT, and debts belong to a customer account. Overdraft (KMH) is a separate product and is not entered as a negative opening balance.",
        IceSablonIndir: "Download empty template",
        UrunGrubuKisa: "Group",
        DovizTutar: "Amount ({0})",
        DovizFisBacagiNot: "A foreign-currency payment taken on a sales receipt does not create a foreign-currency debt; such debt is opened only with this form.",
        DovizVadeNot: "Foreign-currency debts carry no due date and do not appear in the overdue list.",
        EkstreTarihi: "Statement date");

    public static Toplu2Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
