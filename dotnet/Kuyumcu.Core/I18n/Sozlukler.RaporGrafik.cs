namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-64 — RAPOR GRAFİKLERİ + DÖNEM KARŞILAŞTIRMA DELTASI: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç").
//   RaporBugunSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Kâr-zarar ve satış analizi grafiklerinin metinleri (bir dilde).</summary>
public sealed record RaporGrafikMetinleri(
    string GelirGiderKar,
    string BuDonem,
    string KarsilastirilanDonem,
    string MaliyetKirilimi,
    string MaliyetKirilimiNotu,
    string DigerUrunler,
    string MaliyetKirilimiBos,
    string OncekiyeGore,
    string YuzdeHesaplanamaz,
    string GirisCikisGrafik,
    string GirisCikisNotu,
    string GrafikBos);

public static class RaporGrafikSozlugu
{
    public static readonly RaporGrafikMetinleri Tr = new(
        GelirGiderKar: "Gelir, maliyet ve kâr (TL)",
        BuDonem: "Seçilen dönem",
        KarsilastirilanDonem: "Karşılaştırılan dönem",
        MaliyetKirilimi: "Maliyet dağılımı (ürüne göre)",
        MaliyetKirilimiNotu: "En çok maliyet tutan 5 ürün; kalanı tek dilimde.",
        DigerUrunler: "Diğer ürünler",
        MaliyetKirilimiBos: "Bu dönemde maliyeti bilinen satış yok.",
        OncekiyeGore: "önceki döneme göre",
        YuzdeHesaplanamaz: "Önceki dönem sıfır olduğu için yüzde hesaplanamaz.",
        GirisCikisGrafik: "Ödeme aracına göre giriş ve çıkış (TL)",
        GirisCikisNotu: "Yalnız TL hareketleri çizilir; döviz tutarları tabloda.",
        GrafikBos: "Bu dönemde çizilecek hareket yok.");

    public static readonly RaporGrafikMetinleri En = new(
        GelirGiderKar: "Revenue, cost and profit (₺)",
        BuDonem: "Selected period",
        KarsilastirilanDonem: "Compared period",
        MaliyetKirilimi: "Cost breakdown (by product)",
        MaliyetKirilimiNotu: "Top 5 products by cost; the rest in one slice.",
        DigerUrunler: "Other products",
        MaliyetKirilimiBos: "No sales with a known cost in this period.",
        OncekiyeGore: "vs. previous period",
        YuzdeHesaplanamaz: "Previous period is zero, so a percentage cannot be calculated.",
        GirisCikisGrafik: "Money in and out by payment method (₺)",
        GirisCikisNotu: "Only ₺ movements are charted; foreign currency is in the table.",
        GrafikBos: "Nothing to chart in this period.");

    public static RaporGrafikMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
