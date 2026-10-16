namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-2 — MÜŞTERİLER (/cariler) "LİSTE + ÇEKMECE" YÜZÜ: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin için AYRI dosya —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs). PanelSadeSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ JARGON SIFIR (PLAN § 5B): etiket ≤ 3 kelime, mesaj ≤ 2 cümle.
//   Taslak: dokumanlar/02-mimari/sadelestirme/tuval/project/Musteriler.dc.html
//   ("Daha fazla (adres, TC, e-posta, limit) ▾", satırda "⋯").
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Müşteriler ekranının liste + çekmece yüzündeki metinler (bir dilde).</summary>
public sealed record MusteriListeMetinleri(
    string DigerIslemler,
    string MenuBaslik,
    string DahaFazla,
    string DahaFazlaIcerik,
    string DahaAz,
    string MenuIpucu);

public static class MusteriListeSozlugu
{
    public static readonly MusteriListeMetinleri Tr = new(
        DigerIslemler: "Diğer işlemler",
        MenuBaslik: "{0} için işlemler",
        DahaFazla: "Daha fazla",
        DahaFazlaIcerik: "adres, TC, e-posta, limit",
        DahaAz: "Daha az göster",
        MenuIpucu: "Birini seçin. Pencere kendiliğinden kapanır.");

    public static readonly MusteriListeMetinleri En = new(
        DigerIslemler: "More actions",
        MenuBaslik: "Actions for {0}",
        DahaFazla: "More",
        DahaFazlaIcerik: "address, ID, e-mail, limit",
        DahaAz: "Show less",
        MenuIpucu: "Pick one. This window closes by itself.");

    public static MusteriListeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
