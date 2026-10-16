namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-61 — SATIR EYLEMLERİ TAŞMA MENÜSÜ (ortak bileşen): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (paralel şeritler dokunuyor);
//   orkestratör koşulu "yeni metin AYRI dosyaya". KasaBildirimSozlugu ile aynı desen.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ Bu metinler YALNIZ sayfa kendi metnini vermediğinde kullanılır; Müşteriler
//   ve Ürünler kendi (S3-2/S3-3) metinlerini vermeyi sürdürür — ekrandaki söz
//   değişmez.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>SatirEylemleri bileşeninin varsayılan metinleri (bir dilde).</summary>
public sealed record SatirEylemMetinleri(
    string DigerIslemler,
    string MenuBaslik,
    string MenuIpucu);

public static class SatirEylemSozlugu
{
    public static readonly SatirEylemMetinleri Tr = new(
        DigerIslemler: "Diğer işlemler",
        MenuBaslik: "{0} için işlemler",
        MenuIpucu: "Birini seçin. Pencere kendiliğinden kapanır. Kapatmak için Esc.");

    public static readonly SatirEylemMetinleri En = new(
        DigerIslemler: "More actions",
        MenuBaslik: "Actions for {0}",
        MenuIpucu: "Pick one. This window closes by itself. Press Esc to close.");

    public static SatirEylemMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
