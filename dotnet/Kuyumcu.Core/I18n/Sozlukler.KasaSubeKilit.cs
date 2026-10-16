namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q9-5 (QA turu 9, YÜKSEK) — KASALAR: ŞUBE KİLİDİ + EKSİ BAKİYE UYARISI
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin Sozlukler.<Alan>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ JARGON SIFIR: paket adı (P-44), "storno", "defter" ekrana yazılmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Kasalar ekranına Q9-5 ile eklenen metinler (bir dilde).</summary>
public sealed record KasaSubeKilitMetinleri(
    // ── Düzenleme çekmecesi: şube / para birimi kilidi (UX-S25/S30) ──
    string KilitBakiye,
    string KilitHareket,
    string KilitGunKapanisi,
    string KilitBilinmiyor,
    string KilitYol,
    string KilitYolBaglanti,
    // ── Eksi bakiye (kart + özet) ──
    string EksiKartUyari,
    string EksiKasayaGiris,
    string EksiOzetUyari,
    string EksiOzetAlt);

public static class KasaSubeKilitSozlugu
{
    public static readonly KasaSubeKilitMetinleri Tr = new(
        KilitBakiye: "Bu kasada para var; şubesi ve para birimi değiştirilemez.",
        KilitHareket: "Bu kasada daha önce işlem yapıldı; şubesi ve para birimi değiştirilemez. Eski işlemler şube raporlarında bu kasanın şubesinde görünür — şube değişirse geçmiş raporlar da değişirdi.",
        KilitGunKapanisi: "Bu kasada gün sonu kapatıldı; şubesi ve para birimi değiştirilemez. Kapanmış günün sayımı bu şubeye aittir.",
        KilitBilinmiyor: "Şube ve para birimi şu an değiştirilemiyor: kasanın geçmişi okunamadı. Sayfayı yenileyip tekrar deneyin.",
        KilitYol: "Doğru şubede yeni kasa açın, parayı bu kasadan oraya aktarın; bu kasa sıfırlanınca pasife alın.",
        KilitYolBaglanti: "Kasalar arası para aktar",
        EksiKartUyari: "Eksi bakiye — nakit kasa eksiye düşemez. Bu tutar, kural gelmeden önce girilmiş kayıtlardan kalmış olabilir; düzeltme gerekir: kasayı sayın, eksik parayı kasaya giriş olarak yazın.",
        EksiKasayaGiris: "Kasaya giriş yap",
        EksiOzetUyari: "{0} kasada eksi bakiye var. Nakit kasa eksiye düşemez; düzeltme gerekir.",
        EksiOzetAlt: "Eksi — düzeltme gerekir");

    public static readonly KasaSubeKilitMetinleri En = new(
        KilitBakiye: "This register holds money; its branch and currency cannot be changed.",
        KilitHareket: "This register already has transactions; its branch and currency cannot be changed. Past transactions appear under this register's branch in branch reports — changing the branch would change past reports too.",
        KilitGunKapanisi: "An end-of-day close was done on this register; its branch and currency cannot be changed. The closed day's count belongs to this branch.",
        KilitBilinmiyor: "Branch and currency cannot be changed right now: the register's history could not be read. Refresh the page and try again.",
        KilitYol: "Open a new register in the correct branch, transfer the money there from this register; once this register is at zero, deactivate it.",
        KilitYolBaglanti: "Transfer money between registers",
        EksiKartUyari: "Negative balance — a cash register cannot go negative. This amount may come from records entered before the rule existed; a correction is needed: count the register and record the missing cash as an incoming entry.",
        EksiKasayaGiris: "Add cash to register",
        EksiOzetUyari: "{0} register(s) have a negative balance. A cash register cannot go negative; a correction is needed.",
        EksiOzetAlt: "Negative — correction needed");

    public static KasaSubeKilitMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
