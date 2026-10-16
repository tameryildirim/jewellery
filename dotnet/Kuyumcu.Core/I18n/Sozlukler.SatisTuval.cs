namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S1 — SATIŞ BASİT MOD TUVALE (Tezgah.dc.html) · TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: "yeni
//   metin için AYRI dosya aç"). TezgahFisiSozlugu / FisYazdirSozlugu deseni.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı
//   kurucudan geçer.
// ⚠ Metinler tuvalin (Tezgah.dc.html) KENDİ sözcükleridir: "Alış · eski altın",
//   "İade", "Değişim", "İndirim", "Son 3 satış", "Kaydet ve yazdır".
// ⚠ EŞİK SAYISI YOK (B-3 / Mehmet kuralı): nakit uyarısındaki tutar {0}
//   yer tutucusudur ve SUNUCUNUN /tevsik/gunluk yanıtından gelir; sunucu
//   henüz yanıt vermediyse sayısız metin (NakitUyariSayisiz) basılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tuval tezgâh yüzünün (basit mod satış) metinleri.</summary>
public sealed record SatisTuvalMetinleri(
    string KaroAlisEskiAltin,
    string KaroIade,
    string KaroDegisim,
    string KaroIndirim,
    string IndirimKilit,
    string Tarih,
    string TarihIpucu,
    string Para,
    string ParaTl,
    string ParaIpucu,
    string SonUcSatis,
    string Pesin,
    string SonSatisTumu,
    string NakitUyari,
    string NakitUyariSayisiz,
    string Vazgec,
    string VazgecIpucu,
    string KaydetYazdir,
    string SutunUrun,
    string SutunAyar,
    string SutunGram,
    string SutunTutar,
    string SutunOdeme,
    string SutunAyrinti,
    string ToplamGram,
    string KasaYok,
    string AciklamaYerTutucu,
    string BarkodYerTutucu,
    string AdlaCokEslesme,
    string KaydetTek,
    string CokFisNotu,
    string YazdirAcildi);

/// <summary>Tuval tezgâh metinleri — TR + EN.</summary>
public static class SatisTuvalSozlugu
{
    public static readonly SatisTuvalMetinleri Tr = new(
        KaroAlisEskiAltin: "Alış · eski altın",
        KaroIade: "İade",
        KaroDegisim: "Değişim",
        KaroIndirim: "İndirim",
        IndirimKilit: "İndirim: önce fişe ürün ekleyin.",
        Tarih: "Tarih",
        TarihIpucu: "Fişin tarihi kayıt anıdır; program kendisi yazar.",
        Para: "Para",
        ParaTl: "TL",
        ParaIpucu: "Satış fişi Türk lirasıyla kesilir.",
        SonUcSatis: "Son 3 satış",
        Pesin: "Peşin",
        SonSatisTumu: "Tüm satışlar",
        NakitUyari: "{0} üstündeki fişte nakit alınamaz — tamamını kart ya da havaleyle alın.",
        NakitUyariSayisiz: "Yasal sınırın üstündeki fişte nakit alınamaz — tamamını kart ya da havaleyle alın.",
        Vazgec: "Vazgeç",
        VazgecIpucu: "Fişi boşaltır: ürünler, ödeme ve açıklama silinir, müşteri Peşin olur.",
        KaydetYazdir: "Kaydet ve yazdır",
        SutunUrun: "Ürün",
        SutunAyar: "Ayar",
        SutunGram: "Gram",
        SutunTutar: "Tutar",
        SutunOdeme: "Ödeme",
        SutunAyrinti: "Ayrıntı",
        ToplamGram: "Toplam · {0} g",
        KasaYok: "Kasa seçilmedi",
        AciklamaYerTutucu: "Açıklama (istersen)",
        BarkodYerTutucu: "Barkodu okut ya da ürün adı yaz",
        AdlaCokEslesme: "Bu adla {0} ürün var; adı biraz daha yazın ya da Elle ekle'den seçin.",
        KaydetTek: "Kaydet",
        CokFisNotu: "Bu {0} ürün {0} ayrı fiş olarak kaydedilir; her fiş ayrı yazdırılır.",
        YazdirAcildi: "Fiş yeni sekmede yazdırmaya açıldı. Açılmadıysa \"Fiş yazdır\"a basın.");

    public static readonly SatisTuvalMetinleri En = new(
        KaroAlisEskiAltin: "Buy · old gold",
        KaroIade: "Return",
        KaroDegisim: "Exchange",
        KaroIndirim: "Discount",
        IndirimKilit: "Discount: add a product to the receipt first.",
        Tarih: "Date",
        TarihIpucu: "The receipt date is the moment it is saved; the program fills it in.",
        Para: "Currency",
        ParaTl: "TL",
        ParaIpucu: "The sales receipt is issued in Turkish lira.",
        SonUcSatis: "Last 3 sales",
        Pesin: "Walk-in",
        SonSatisTumu: "All sales",
        NakitUyari: "No cash on a receipt above {0} — take the whole amount by card or transfer.",
        NakitUyariSayisiz: "No cash on a receipt above the legal limit — take the whole amount by card or transfer.",
        Vazgec: "Cancel",
        VazgecIpucu: "Clears the receipt: products, payment and note are removed, customer becomes Walk-in.",
        KaydetYazdir: "Save and print",
        SutunUrun: "Product",
        SutunAyar: "Karat",
        SutunGram: "Gram",
        SutunTutar: "Amount",
        SutunOdeme: "Payment",
        SutunAyrinti: "Detail",
        ToplamGram: "Total · {0} g",
        KasaYok: "No till selected",
        AciklamaYerTutucu: "Note (optional)",
        BarkodYerTutucu: "Scan the barcode or type the product name",
        AdlaCokEslesme: "{0} products match this name; type a little more or pick one under Add by hand.",
        KaydetTek: "Save",
        CokFisNotu: "These {0} products are saved as {0} separate receipts; each receipt is printed separately.",
        YazdirAcildi: "The receipt opened for printing in a new tab. If it did not open, press \"Print receipt\".");

    /// <summary>Dil koduna göre metinler; bilinmeyen dil Türkçeye düşer.</summary>
    public static SatisTuvalMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
