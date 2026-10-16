namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — KARMA ÖDEMEDE ALTIN BACAĞI: TEK HESAP KAYNAĞI
// (KARAR-HAS-DEFTERI-2026-09-23 § 4.2 · B4-8 / B4-9 / B4-10 / B4-11)
//
// ⚠ NEDEN Core'DA: aynı formül hem SUNUCUDA (OdemeSepeti — fişi reddeden
//   kapı) hem EKRANDA (türetilen has/TL, salt okunur önizleme) çalışır. İki
//   ayrı yerde yazılsaydı iki ayrı yuvarlama doğar ve ekranın gösterdiği TL,
//   sunucunun "kuruşu kuruşuna" karşılaştırdığı TL'den bir kuruş sapar →
//   kasiyer hiçbir zaman kaydedemeyen bir fişe bakardı. Tek kaynak budur.
//
// ⚠ HAS GRAM TEK KAYNAKTAN: HasHesap.ToHasGram (B4-8). Kıymetsiz ayarda 0
//   döner — sahte has üretilmez (K20-5). Fiyat HAS GRAM başınadır (B4-9).
//
// ⚠ ORAN KODA GÖMÜLMEZ: stopaj oranı çağırandan gelir (sunucuda
//   GiderPusulasiKapisi.StopajOran, yapılandırmadan — ADR-0005).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Altın bacağının türetilmiş değerleri.</summary>
/// <param name="HasGrami">Eski altının has karşılığı (6 hane).</param>
/// <param name="BrutTl">has × TL/has-gram (2 hane) — gider pusulasının BRÜT tutarı.</param>
/// <param name="StopajOran">Satıcı tipine göre stopaj oranı (yapılandırmadan).</param>
/// <param name="StopajTutar">brüt × oran (2 hane) — pusulada belge verisi.</param>
/// <param name="NetTl">brüt − stopaj (2 hane) — bacağın fişi kapatan TL'si.</param>
public sealed record AltinBacagiDegeri(
    decimal HasGrami, decimal BrutTl, decimal StopajOran, decimal StopajTutar, decimal NetTl);

public static class AltinBacagiHesap
{
    /// <summary>
    /// B4-8..B4-10 — gram + ayar + TL/has-gram + stopaj oranı → has, brüt, stopaj, net.
    /// </summary>
    public static AltinBacagiDegeri Hesapla(
        decimal gramAgirlik, Ayar ayar, decimal birimFiyatTlHasGram, decimal stopajOran)
    {
        var hasGrami = HasHesap.ToHasGram(gramAgirlik, ayar);
        var brutTl = Yuvarla2(hasGrami * birimFiyatTlHasGram);
        var stopajTutar = Yuvarla2(brutTl * stopajOran);
        var netTl = Yuvarla2(brutTl - stopajTutar);
        return new AltinBacagiDegeri(hasGrami, brutTl, stopajOran, stopajTutar, netTl);
    }

    /// <summary>
    /// K-KUR-4 — TL/has-gram alanının ÖN-DOLGUSU. Oran yapılandırılmamışsa
    /// (<c>null</c>) ön-dolgu YOKTUR: alan boş açılır, elle girilir. Sistem
    /// kur kaydındaki has fiyatından kendiliğinden bir alış fiyatı UYDURMAZ
    /// (K-KUR-3). Has fiyatı bilinmiyorsa da ön-dolgu yoktur.
    /// </summary>
    public static decimal? OnDolgu(decimal? hasFiyati, decimal? alisIskontoOran) =>
        alisIskontoOran is { } oran && hasFiyati is { } fiyat && fiyat > 0m && oran >= 0m && oran < 1m
            ? Yuvarla2(fiyat * (1m - oran))
            : null;

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}
