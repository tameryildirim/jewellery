namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CIFT-BACAK (tur 2) — emanet "Geri al" önceden kilidi + hurda defterinde
// takas satırı.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin Sozlukler.<Alan>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ Mevzuat hükmü YOKTUR: bu dosyada eşik/oran/kod yazılı değildir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>CIFT-BACAK metinleri (bir dilde).</summary>
public sealed record CiftBacakMetinleri(
    // Emanet: iadesi yapılmış girişin "Geri al" düğmesi neden kilitli (UX-S30).
    // Sunucunun 409 metniyle AYNI cümle (StornoServisi.EmanetIadeEdildiMesaji).
    string EmanetIadeEdildiKilit,
    // Hurda defteri: takasta müşteriden alınan eski altın satırının tür adı (UX-S31).
    string HurdaTurTakas);

/// <summary>CIFT-BACAK sözlüğü.</summary>
public static class CiftBacakSozlugu
{
    public static readonly CiftBacakMetinleri Tr = new(
        EmanetIadeEdildiKilit: "Bu emanetin bir kısmı iade edildi; önce iadeyi geri alın.",
        HurdaTurTakas: "Takasta alınan");

    public static readonly CiftBacakMetinleri En = new(
        EmanetIadeEdildiKilit: "Part of this deposit was returned; undo the return first.",
        HurdaTurTakas: "Taken in trade");

    public static CiftBacakMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
