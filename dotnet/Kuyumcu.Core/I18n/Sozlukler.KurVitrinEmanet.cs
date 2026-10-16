namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET S3-5 — KUR + VİTRİN + EMANET (dalga S3B): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs"). RaporBugunSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer, bir dilde alan unutulursa derleme
//   kırılır.
//
// ⚠ JARGON SIFIR (PLAN § 5B): "storno" yerine "Geri al (düzeltme fişi)",
//   "cari" yerine "Müşteri". Etiket ≤ 3 kelime, mesaj ≤ 2 cümle.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Kur, Vitrin ve Emanet ekranlarına bu paketle eklenen metinler (bir dilde).</summary>
public sealed record KurVitrinEmanetMetinleri(
    // ── Kur: HAS önde ──
    string HasBaslik,
    string HasIpucu,
    string GramAltin,
    string Dolar,
    string Euro,
    string Sterlin,
    string KaynakVeSaat,
    string ProNotu,
    string GorunumuDegistir,
    // ── Vitrin ──
    string KuraDon,
    // ── Emanet: liste + çekmece ──
    string Ayrinti,
    string AyrintiBaslik,
    string Musteri,
    string Maden,
    string HasGram,
    string Tur,
    string Tarih,
    string Aciklama,
    string KilitliAlanlar,
    string KilitSebebi,
    string GramAyarNotu,
    string DuzeltmeYolu,
    string GeriAl,
    string GeriAlKapali,
    string GeriAlmaKaydi,
    string Bilinmiyor);

public static class KurVitrinEmanetSozlugu
{
    public static readonly KurVitrinEmanetMetinleri Tr = new(
        HasBaslik: "Has altın · 1 gram",
        HasIpucu: "24 ayar karşılığı",
        GramAltin: "Gram altın",
        Dolar: "Dolar",
        Euro: "Euro",
        Sterlin: "Sterlin",
        KaynakVeSaat: "{0} · {1}",
        ProNotu: "Kur kaynağı ve kur geçmişi profesyonel görünümde.",
        GorunumuDegistir: "Görünümü değiştir →",
        KuraDon: "← Kur ekranı",
        Ayrinti: "Ayrıntı",
        AyrintiBaslik: "Emanet kaydı",
        Musteri: "Müşteri",
        Maden: "Maden",
        HasGram: "Has gram",
        Tur: "Tür",
        Tarih: "Tarih",
        Aciklama: "Açıklama",
        KilitliAlanlar: "Değiştirilemez",
        KilitSebebi: "Has gram, maden ve müşteri emanet bakiyesini oluşturur.",
        GramAyarNotu: "Gram ve ayar kayıtta has grama çevrilip saklanır.",
        DuzeltmeYolu: "Yanlışsa önce geri alın, sonra doğrusunu yeniden girin.",
        GeriAl: "Geri al (düzeltme fişi)",
        GeriAlKapali: "Bu kayıt geri alınamaz.",
        GeriAlmaKaydi: "Geri alma kaydı",
        Bilinmiyor: "—");

    public static readonly KurVitrinEmanetMetinleri En = new(
        HasBaslik: "Fine gold · 1 gram",
        HasIpucu: "24 karat equivalent",
        GramAltin: "Gram gold",
        Dolar: "Dollar",
        Euro: "Euro",
        Sterlin: "Pound",
        KaynakVeSaat: "{0} · {1}",
        ProNotu: "Rate source and rate history are in the professional view.",
        GorunumuDegistir: "Change view →",
        KuraDon: "← Rates screen",
        Ayrinti: "Details",
        AyrintiBaslik: "Custody record",
        Musteri: "Customer",
        Maden: "Metal",
        HasGram: "Fine grams",
        Tur: "Type",
        Tarih: "Date",
        Aciklama: "Note",
        KilitliAlanlar: "Cannot be changed",
        KilitSebebi: "Fine grams, metal and customer make up the custody balance.",
        GramAyarNotu: "Weight and karat are converted to fine grams when saved.",
        DuzeltmeYolu: "If wrong, reverse it first, then enter the correct one.",
        GeriAl: "Reverse (correction slip)",
        GeriAlKapali: "This record cannot be reversed.",
        GeriAlmaKaydi: "Reversal record",
        Bilinmiyor: "—");

    public static KurVitrinEmanetMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
