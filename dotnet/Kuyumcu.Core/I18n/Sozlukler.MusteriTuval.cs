namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-M1 — MÜŞTERİLER TUVALE (tuval/project/Musteriler.dc.html): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve bu dalgada ona dokunulmaz
//   (orkestratör koşulu: yeni metin Sozlukler.<Alan>.cs). S3-2'nin
//   Sozlukler.Musteriler.cs dosyası kendi testiyle (S32_14) kilitli olduğundan
//   bu paketin metinleri ayrı bir record'da durur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'dadır.
// ⚠ JARGON SIFIR (PLAN § 5B): "cari", "pasife al", "denetim izi" BASİT yüzde
//   kullanılmaz; tuvalin kendi etiketleri birebir alınır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Müşteriler ekranının BASİT (tuval) yüzündeki metinler (bir dilde).</summary>
public sealed record MusteriTuvalMetinleri(
    string AramaYerTutucu,
    string SegmentEtiketi,
    string Hepsi,
    string Borclu,
    string Toptanci,
    string SutunMusteri,
    string SutunTelefon,
    string SutunBorc,
    string SutunHas,
    string VadeGecti,
    string Sayac,
    string DigerSuzgecler,
    string DigerSuzgeclerKapat,
    string SegmentBos,
    string AdSoyad,
    string Not,
    string VadeUyariOnce,
    string VadeUyariSonra,
    string TahsilatDugmesi,
    string SonDegisiklik,
    string SonDegisiklikKimli,
    string Bugun,
    string Dun,
    string KimNeYapti,
    string KullanimdanKaldir,
    string SatiraTikla,
    // T-M1 doğrulayıcı düzeltmesi (yarış kilidi + kişi yer tutucusu)
    string KartYukleniyor,
    string KartYuklenemedi,
    string KisiAliniyor);

public static class MusteriTuvalSozlugu
{
    public static readonly MusteriTuvalMetinleri Tr = new(
        AramaYerTutucu: "Ad ya da telefon",
        SegmentEtiketi: "Müşteri süzgeci",
        Hepsi: "Hepsi",
        Borclu: "Borçlu",
        Toptanci: "Toptancı",
        SutunMusteri: "Müşteri",
        SutunTelefon: "Telefon",
        SutunBorc: "Borç",
        SutunHas: "Has",
        VadeGecti: "Vade geçti",
        Sayac: "{0} müşteri · {1} gösteriliyor",
        DigerSuzgecler: "Diğer süzgeçler",
        DigerSuzgeclerKapat: "Süzgeçleri gizle",
        SegmentBos: "Bu süzgeçte müşteri yok. \"Hepsi\"ni seçin.",
        AdSoyad: "Ad soyad",
        Not: "Not",
        VadeUyariOnce: "Vadesi {0} gün geçti. Para almak için \u201C",
        VadeUyariSonra: "\u201D düğmesini kullan.",
        TahsilatDugmesi: "Tahsilat",
        SonDegisiklik: "Son değişiklik: {0}",
        SonDegisiklikKimli: "Son değişiklik: {0}, {1}",
        Bugun: "bugün {0}",
        Dun: "dün {0}",
        KimNeYapti: "Kim ne yaptı",
        KullanimdanKaldir: "Kullanımdan kaldır",
        SatiraTikla: "Satıra dokunun: sağda açılır, orada düzeltin.",
        KartYukleniyor: "Müşteri kartı yükleniyor…",
        KartYuklenemedi: "Müşteri kartı alınamadı. Paneli kapatıp yeniden açın.",
        KisiAliniyor: "kişi alınıyor…");

    public static readonly MusteriTuvalMetinleri En = new(
        AramaYerTutucu: "Name or phone",
        SegmentEtiketi: "Customer filter",
        Hepsi: "All",
        Borclu: "Owing",
        Toptanci: "Wholesaler",
        SutunMusteri: "Customer",
        SutunTelefon: "Phone",
        SutunBorc: "Owes",
        SutunHas: "Fine gold",
        VadeGecti: "Overdue",
        Sayac: "{0} customers · {1} shown",
        DigerSuzgecler: "More filters",
        DigerSuzgeclerKapat: "Hide filters",
        SegmentBos: "No customers in this filter. Choose \"All\".",
        AdSoyad: "Full name",
        Not: "Note",
        VadeUyariOnce: "{0} days overdue. To collect, use the ",
        VadeUyariSonra: " button.",
        TahsilatDugmesi: "Collect payment",
        SonDegisiklik: "Last change: {0}",
        SonDegisiklikKimli: "Last change: {0}, {1}",
        Bugun: "today {0}",
        Dun: "yesterday {0}",
        KimNeYapti: "Who did what",
        KullanimdanKaldir: "Remove from use",
        SatiraTikla: "Tap a row: it opens on the right, edit it there.",
        KartYukleniyor: "Loading customer card…",
        KartYuklenemedi: "Customer card could not be loaded. Close the panel and open it again.",
        KisiAliniyor: "loading person…");

    public static MusteriTuvalMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
