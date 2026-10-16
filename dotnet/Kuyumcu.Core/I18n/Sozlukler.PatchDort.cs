namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PATCH-4 — satış fişi istemci tutar tavanı (hasım bulgusu ORTA: 13 haneli birim
// fiyat satırda kesiliyor, kullanıcı hatayı ancak kaydederken görüyordu).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına). EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record.
// ⚠ TAVAN TEK KAYNAK: değer sunucunun KolonTavani.D15_2 (decimal(15,2)) sınırıyla AYNIDIR;
//   eşitlik PatchDortTestleri'nde derleyici dışı kilitle sınanır (ayrışırsa test kırmızı).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>PATCH-4 metinleri (bir dilde).</summary>
public sealed record PatchDortMetinleri(
    // Tavan rakamla değil HANE sayısıyla söylenir: 375 px'te 22 karakterlik bölünmez
    // "9.999.999.999.999,99 ₺" dar sütunda taştı (başsız Chrome ölçümü, K3).
    string TutarTavaniAsildi,
    // PATCH-4 t2: satır içi ADET hücresi dar (≈ 60 px) — uzun ileti hücreden taştı
    // (başsız Chrome, PRO 1280/375, K3 = 1). Hücre altı için kısa biçim.
    string SatirTutariCokBuyuk);

public static class PatchDortSozlugu
{
    /// <summary>Satış kalemi birim fiyatı ve satır tutarı için istemci tavanı = sunucu kolon tavanı decimal(15,2).</summary>
    public const decimal TutarTavani = 9_999_999_999_999.99m;

    public static readonly PatchDortMetinleri Tr = new(
        TutarTavaniAsildi: "Tutar çok büyük — satır tutarı en çok 13 haneli olabilir.",
        SatirTutariCokBuyuk: "Satır tutarı çok büyük.");

    public static readonly PatchDortMetinleri En = new(
        TutarTavaniAsildi: "Amount is too large — a line total can have at most 13 digits.",
        SatirTutariCokBuyuk: "Line total too large.");

    public static PatchDortMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
