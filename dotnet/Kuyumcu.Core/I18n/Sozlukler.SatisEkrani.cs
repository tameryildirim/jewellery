namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-i — SATIŞ EKRANI (/islemler) DİLİ VE SON FİŞ (kurucu bulgusu 24.09)
//
// Kurucu (24.09 10:20, /islemler "Elle ekle" açık ekran görüntüsü): "normal mi
// yazılar kutuya sığmaz, inputlar aynı seviyede değil". Aynı turda: basit modda
// "Cariler *" etiketi, iki uzun açıklama satırı, sayfa başlığı "İşlemler" ile üst
// bar "Satış" ikiliği, başarı bandıyla 3 sn'de kaybolan fiş numarası (QA-9).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; iki dil aynı
//   kurucudan geçer.
// ⚠ D10: "Cari" profesyonel yüzde kalır (muhasebe dili); basit yüzde "Müşteri".
//   Ayrı anahtardır — pro metni değişmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>/islemler basit yüz metinleri ve son fiş satırı (bir dilde).</summary>
public sealed record SatisEkraniMetinleri(
    /// <summary>Basit modda müşteri seçicisinin etiketi ("Cariler" yerine).</summary>
    string MusteriEtiketi,
    /// <summary>Basit modda pasif cari notunun KISA hâli (uzun hâli title'da).</summary>
    string PasifKisa,
    /// <summary>Basit modda Peşin Müşteri notunun KISA hâli (uzun hâli title'da).</summary>
    string PesinKisa,
    /// <summary>Basit yüz alt başlığı — jargonsuz tek cümle.</summary>
    string BasitAciklama,
    /// <summary>Son kaydedilen fiş satırı: "Son fiş: {0}".</summary>
    string SonFis,
    /// <summary>Barkod kutusunun KISA yer tutucusu (375 px'te kutuya sığar; S1-i doğrulama bulgusu).</summary>
    string BarkodKisaIpucu);

public static class SatisEkraniSozlugu
{
    public static readonly SatisEkraniMetinleri Tr = new(
        MusteriEtiketi: "Müşteri",
        PasifKisa: "Pasif müşteriler listede yok.",
        PesinKisa: "Peşin satış: müşteri adı gerekmez.",
        BasitAciklama: "Ürünü okutun, müşteriyi seçin, ödemeyi alıp fişi kaydedin.",
        SonFis: "Son fiş: {0}",
        BarkodKisaIpucu: "Barkodu okutun ya da yazın");

    public static readonly SatisEkraniMetinleri En = new(
        MusteriEtiketi: "Customer",
        PasifKisa: "Inactive customers are not listed.",
        PesinKisa: "Walk-in sale: no customer name needed.",
        BasitAciklama: "Scan the item, pick the customer, take the payment and save the receipt.",
        SonFis: "Last receipt: {0}",
        BarkodKisaIpucu: "Scan or type the barcode");

    public static SatisEkraniMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
