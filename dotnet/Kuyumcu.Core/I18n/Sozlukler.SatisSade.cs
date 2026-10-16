namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-2 — SATIŞ EKRANI SADELİĞİ (tuval + Pusula) · TR + EN
//
// Kaynak: kurucu 25.09 — "eski kayıtlar direkt açık gelmesin, piyano gibi açılır
// kapanır olsun" · "daha basit olamaz mı, Pusula'da böyle mi".
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record.
// ⚠ JARGON YOK (basit yüz): "storno", "cari", "ters kayıt" basit metinde geçmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satış ekranı sadelik metinleri (katlanır geçmiş, tek giriş, ödeme özeti).</summary>
public sealed record SatisSadeMetinleri(
    string BasitDegismez,
    string DegismezNeden,
    string KirilimIpucu,
    string UrunAraYerTutucu,
    string DigerAyrintilar,
    string OdemeOzetiIpucu,
    string OdemeKapat,
    string BaskaOdemeEkle,
    string TekOdemeyeDon,
    string Bugun,
    string UstOzetIpucu,
    string YasalUygun,
    string YasalAyrinti,
    string TekrarDene);

/// <summary>PAKET SATIS-2 — satış ekranı sadelik sözlüğü (TR + EN).</summary>
public static class SatisSadeSozlugu
{
    public static readonly SatisSadeMetinleri Tr = new(
        BasitDegismez: "Yanlış satışı Satış iadesi ile geri alın.",
        DegismezNeden: "Neden değiştirilemez?",
        KirilimIpucu: "Ödeme kırılımı",
        UrunAraYerTutucu: "Ürün ara ya da barkod okut (F2)",
        DigerAyrintilar: "+ Ayrıntı ekle",
        OdemeOzetiIpucu: "Ödeme türünü ve kasayı değiştir",
        OdemeKapat: "Tamam",
        BaskaOdemeEkle: "+ Başka ödeme ekle",
        TekOdemeyeDon: "Tek ödemeye dön",
        Bugun: "Bugün",
        UstOzetIpucu: "Tarih, para, satıcı ve fiyat listesi — değiştirmek için tıklayın",
        YasalUygun: "Yasal kapılar uygun",
        YasalAyrinti: "Yasal kapıların ayrıntısı",
        TekrarDene: "Tekrar dene");

    public static readonly SatisSadeMetinleri En = new(
        BasitDegismez: "Undo a wrong sale with a Sales return.",
        DegismezNeden: "Why can't it be changed?",
        KirilimIpucu: "Payment breakdown",
        UrunAraYerTutucu: "Search product or scan barcode (F2)",
        DigerAyrintilar: "+ Add details",
        OdemeOzetiIpucu: "Change payment type and cash desk",
        OdemeKapat: "Done",
        BaskaOdemeEkle: "+ Add another payment",
        TekOdemeyeDon: "Back to single payment",
        Bugun: "Today",
        UstOzetIpucu: "Date, currency, salesperson and price list — click to change",
        YasalUygun: "Legal checks OK",
        YasalAyrinti: "Legal check details",
        TekrarDene: "Try again");

    public static SatisSadeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
