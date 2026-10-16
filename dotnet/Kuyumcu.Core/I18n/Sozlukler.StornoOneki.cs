namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q10-1 — BASİT MODDA "STORNO:" AÇIKLAMA ÖNEKİ (QA turu 10, /gider)
//
// Sunucu storno satırının açıklamasını "STORNO: <gerekçe>" diye yazar
// (StornoServisi). Bu DEFTER VERİSİDİR ve DEĞİŞMEZ; yalnız GÖSTERİMDE, basit
// modda D10 karşılığıyla ("Geri alındı:") okunur. PROFESYONEL modda metin
// olduğu gibi kalır (muhasebe dili).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin/yardımcı Sozlukler.<Alan>.cs). "Geri alındı" için yeni metin AÇILMADI:
//   KAT-1'in D10 karşılığı (KatalogSozlugu.GeriAlindi) okunur — aynı kavram tek
//   kaynaktan (UX-S31). Yeni olan tek metin: gider değişmezlik notunun basit hâli.
// ⚠ Önek eşleşmesi büyük/küçük harf DUYARSIZDIR ("STORNO:", "Storno:"):
//   QA 10'da K8 sayacı tam büyük harfli "STORNO" yazımını kaçırmıştı.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Q10-1 ile eklenen basit mod metinleri (bir dilde). Konumsal: eksik dil = derleme hatası.</summary>
public sealed record StornoOnekiMetinleri(
    // /gider "değişmez" ipucunun basit mod karşılığı (hub metni "STORNO + yeni kayıt" der)
    string GiderDegismezNotuBasit);

/// <summary>Storno açıklama önekinin basit moddaki sade gösterimi.</summary>
public static class StornoOnekiSozlugu
{
    public static readonly StornoOnekiMetinleri Tr = new(
        GiderDegismezNotuBasit: "Giderin tutarı sonradan değiştirilemez. Yanlışsa \"Geri al (düzeltme fişi)\" ile geri alın, doğrusunu yeniden girin.");

    public static readonly StornoOnekiMetinleri En = new(
        GiderDegismezNotuBasit: "An expense amount cannot be changed later. If it is wrong, use \"Undo (correction slip)\" and enter the correct one again.");

    public static StornoOnekiMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Sunucunun storno satırı açıklamasına koyduğu önek (StornoServisi).</summary>
    public const string SunucuOneki = "STORNO:";

    /// <summary>
    /// Açıklamayı ekranda gösterilecek biçime çevirir. Basit modda baştaki
    /// "STORNO:" öneki "Geri alındı:" (EN "Undone:") olur; profesyonel modda ve
    /// önek yoksa metin DEĞİŞMEZ. null → null (boş gösterimi çağıran seçer).
    /// </summary>
    public static string? Goster(string? aciklama, string dil, bool basit)
    {
        if (!basit || string.IsNullOrEmpty(aciklama)) return aciklama;
        var metin = aciklama.TrimStart();
        if (!metin.StartsWith(SunucuOneki, StringComparison.OrdinalIgnoreCase)) return aciklama;
        var gerekce = metin[SunucuOneki.Length..].TrimStart();
        var onek = KatalogSozlugu.Al(dil).GeriAlindi + ":";
        return gerekce.Length == 0 ? onek : onek + " " + gerekce;
    }
}
