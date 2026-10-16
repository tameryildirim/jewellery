namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-1 — toplu tasarım kuyruğu (30.09–01.10) ekran metinleri.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve yalnız P8'indir (orkestratör
//   kuralı: yeni metin paketin kendi dosyasına). HUB metinleri DEĞİŞMEDİ.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ⚠ Eşik/oran SAYISI metne gömülmez; sayılar yer tutucuyla çağırandan gelir.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-1 metinleri (bir dilde).</summary>
public sealed record Toplu1Metinleri(
    // ── Faturalar · Fişten Fatura Kes (fişsiz satışlar) ──
    string FistenSonIslemler,
    string FistenSonIslemlerYerTutucu,
    string FistenFissizRozet,
    string FistenKalemSayisi,
    string FistenListeBos,
    string FistenListeSinir,
    string FistenOlustu,
    // ── Cari detay · bakiye uyuşmazlığı (tek uyarı, bakiyenin yanında) ──
    string UyusmazlikBasit,
    string UyusmazlikPro,
    string UyusmazlikAltinFark,
    string UyusmazlikTlFark,
    // ── Cari detay · iptal (storno) satırı ──
    string IptalSatiriBasit,
    string IptalSatiriPro,
    string IptalSebebi,
    // ── Kasa kuyruğu (vezne) · yeni fiş ──
    string VezneAciklamaEkle,
    string VezneFisToplami,
    // ── Gider · tek kategori ──
    string GiderTekKategoriKaynak,
    // ── Tur 2 (tasarım hakemi): fatura taslağı ve fişten-kes ──
    string FistenTekAlisRozet,
    string SatirDuzeltIpucu,
    string ToplamSatirlardan,
    string AdetSatistanGelir,
    string AdetAlistanGelir,
    string IslemKaydiniGor,
    string KaynakSatistan,
    string KaynakAlistan,
    string FistenIslemTaslakBagli,
    string FistenIslemFaturali);

public static class Toplu1Sozlugu
{
    public static readonly Toplu1Metinleri Tr = new(
        FistenSonIslemler: "Faturası kesilmemiş son işlemler",
        FistenSonIslemlerYerTutucu: "İşlem seçin…",
        FistenFissizRozet: "Tek satış",
        FistenKalemSayisi: "{0} kalem",
        FistenListeBos: "Son {0} günde faturası kesilmemiş işlem yok. Daha eski bir fiş için aşağıda arayın.",
        FistenListeSinir: "Son {0} günün en yeni {1} işlemi gösteriliyor. Daha eskisi için aşağıda fişi arayın.",
        FistenOlustu: "Taslak fatura oluşturuldu: {0} · {1}",
        UyusmazlikBasit: "Bu müşterinin bakiyesi kontrol edilmeli. Yöneticinize haber verin.",
        UyusmazlikPro: "Kayıtlı bakiye işlemlerin toplamıyla tutmuyor ({0}). Bakiye işlemlerden yeniden hesaplanmalı; düzeltme fişi bu farkı kapatmaz. Destekle iletişime geçin.",
        UyusmazlikAltinFark: "altın farkı {0}",
        UyusmazlikTlFark: "TL farkı {0}",
        IptalSatiriBasit: "{0} iptali",
        IptalSatiriPro: "{0} · storno",
        IptalSebebi: "İptal sebebi: {0}",
        VezneAciklamaEkle: "+ Açıklama ekle",
        VezneFisToplami: "Fiş toplamı",
        GiderTekKategoriKaynak: "tek kategori",
        FistenTekAlisRozet: "Tek alış",
        SatirDuzeltIpucu: "Düzeltmek için değerin üzerine tıklayın; toplam kendiliğinden güncellenir.",
        ToplamSatirlardan: "Toplam satırlardan hesaplanır.",
        AdetSatistanGelir: "Adet satış kaydından gelir; burada değiştirilemez.",
        AdetAlistanGelir: "Adet alış kaydından gelir; burada değiştirilemez.",
        IslemKaydiniGor: "İşlem kaydını gör",
        KaynakSatistan: "Satıştan geldi",
        KaynakAlistan: "Alıştan geldi",
        FistenIslemTaslakBagli: "Bu işlem taslak bir faturaya bağlı (numara kesimde verilir).",
        FistenIslemFaturali: "Bu işlem zaten faturalandı (Fatura No: {0}).");

    public static readonly Toplu1Metinleri En = new(
        FistenSonIslemler: "Recent transactions without an invoice",
        FistenSonIslemlerYerTutucu: "Select a transaction…",
        FistenFissizRozet: "Single sale",
        FistenKalemSayisi: "{0} items",
        FistenListeBos: "No transaction without an invoice in the last {0} days. Search below for an older receipt.",
        FistenListeSinir: "Showing the newest {1} transactions of the last {0} days. Search below for older receipts.",
        FistenOlustu: "Draft invoice created: {0} · {1}",
        UyusmazlikBasit: "This customer's balance needs checking. Please tell your manager.",
        UyusmazlikPro: "The recorded balance does not match the total of the transactions ({0}). The balance must be recalculated from the transactions; an adjustment slip does not close this gap. Please contact support.",
        UyusmazlikAltinFark: "gold difference {0}",
        UyusmazlikTlFark: "TL difference {0}",
        IptalSatiriBasit: "{0} cancellation",
        IptalSatiriPro: "{0} · reversal",
        IptalSebebi: "Cancellation reason: {0}",
        VezneAciklamaEkle: "+ Add a note",
        VezneFisToplami: "Slip total",
        GiderTekKategoriKaynak: "only category",
        FistenTekAlisRozet: "Single purchase",
        SatirDuzeltIpucu: "Click a value to correct it; the total updates by itself.",
        ToplamSatirlardan: "The total is calculated from the lines.",
        AdetSatistanGelir: "Quantity comes from the sale record; it cannot be changed here.",
        AdetAlistanGelir: "Quantity comes from the purchase record; it cannot be changed here.",
        IslemKaydiniGor: "View the transaction",
        KaynakSatistan: "From a sale",
        KaynakAlistan: "From a purchase",
        FistenIslemTaslakBagli: "This transaction is linked to a draft invoice (the number is given when it is issued).",
        FistenIslemFaturali: "This transaction has already been invoiced (Invoice No: {0}).");

    public static Toplu1Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
