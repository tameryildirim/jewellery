using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Evrak;

/// <summary>
/// KIYMETLİ EVRAK DURUM MAKİNESİNİN TEK KARAR NOKTASI (paket 29 Faz A).
///
/// ⚠ NEDEN TEK KAPI: geçiş kontrolü her uca ayrı ayrı yazılsaydı, eklenen bir
/// sonraki uç (ciro, Faz B) onu unuturdu ve tahsil edilmiş bir çek yeniden
/// tahsile verilebilirdi. <c>GunKapanisiKapisi</c>'ndeki kararın aynısı:
/// kural âdet değil, ÇAĞRILMASI ZORUNLU BİR KAPIDIR.
///
/// ⚠ FAIL-CLOSED: tabloda olmayan her geçiş 400'dür. Bilinmeyen bir durum
/// adı da 400'dür — "tanımadığım durumdan her yere gidilebilir" varsayılmaz.
/// </summary>
public static class EvrakDurumKapisi
{
    /// <summary>
    /// <paramref name="mevcut"/> durumundan <paramref name="hedef"/> durumuna
    /// geçiş izinli değilse <see cref="BadRequestException"/> atar.
    /// </summary>
    public static void Dogrula(string mevcut, string hedef)
    {
        if (!EvrakGecisleri.Izinli.TryGetValue(mevcut, out var izinliler))
            throw new BadRequestException($"Bilinmeyen evrak durumu: {mevcut}");

        if (!EvrakDurumu.Tumu.Contains(hedef))
            throw new BadRequestException($"Bilinmeyen hedef durum: {hedef}");

        if (!izinliler.Contains(hedef, StringComparer.Ordinal))
            throw new BadRequestException(
                izinliler.Length == 0
                    ? $"'{mevcut}' son durumdur; başka bir duruma geçirilemez."
                    : $"'{mevcut}' durumundan '{hedef}' durumuna geçilemez. " +
                      $"İzinli geçişler: {string.Join(", ", izinliler)}");
    }

    /// <summary>Ekranın düğmeleri buradan doğar (UX-S30: kilitli düğme sebebini söyler).</summary>
    public static string[] IzinliGecisler(string mevcut) =>
        EvrakGecisleri.Izinli.TryGetValue(mevcut, out var l) ? l : [];
}
