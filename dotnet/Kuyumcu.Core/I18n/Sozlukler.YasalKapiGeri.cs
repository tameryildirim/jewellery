namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI — Peşin Müşteri kilidi HANGİ eşiğin tuttuğunu söyler.
// Kurucu bulgusu (09.10): fiş 230.000 → 150.000 ₺'ye inince ekran aynı "Bu tutarda Peşin Müşteri
// olmaz" cümlesini yazıyordu; kilit artık başka bir eşikten (tevsik haddi) gelse de değişim
// görünmüyordu ("algılamadı"). Sunucu reddiyle AYNI sıra: önce MASAK kimlik kademesi, sonra tevsik
// haddi (MasakKapisi / TevsikKapisi · SistemCarisi.EsikMesaji). {0} = SUNUCUNUN bildirdiği eşik
// (Bicim.Tl) — sayı istemciye gömülmez.
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>YASAL-KAPI-GERI metinleri (bir dilde).</summary>
public sealed record YasalKapiGeriMetinleri(
    // Peşin + tutar ≥ kimlik isteyen en düşük tutar (MASAK kademesi) — {0}: o tutar
    string PesinKimlikKilit,
    // Peşin + tutar > tevsik haddi (kartla bile; SistemCarisi K2) — {0}: had
    string PesinTevsikKilit);

/// <summary>YASAL-KAPI-GERI sözlüğü (TR + EN).</summary>
public static class YasalKapiGeriSozlugu
{
    public static readonly YasalKapiGeriMetinleri Tr = new(
        PesinKimlikKilit: "{0} ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin.",
        PesinTevsikKilit: "{0} üstündeki işlem müşteri adına yazılır — Peşin Müşteri olmaz, müşteri seçin.");

    public static readonly YasalKapiGeriMetinleri En = new(
        PesinKimlikKilit: "Identity is required at {0} and above — walk-in customer is not allowed, select a customer.",
        PesinTevsikKilit: "Transactions above {0} are recorded under a customer — walk-in customer is not allowed, select a customer.");

    public static YasalKapiGeriMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
