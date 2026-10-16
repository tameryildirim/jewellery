using System.Globalization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

/// <summary>
/// Fatura numarası üretiminin tek kaynağı (Fatura ve Banka modülleri aynı
/// sayacı kullanır). Biçim: FTR-{yıl}-{sıra:6}.
///
/// Eski davranış COUNT+1 idi; defterde boşluk varsa (iptal, elle giriş, banka
/// dönüşümü) mevcut bir numarayı yeniden üretip benzersizlik kısıtını ihlal
/// ediyordu. Artık o yılın EN BÜYÜK sırası bulunup bir arttırılır.
/// </summary>
public static class FaturaNoUretici
{
    public static async Task<string> Siradaki(KuyumcuDbContext db, string tenantId, int yil)
    {
        var onEk = $"FTR-{yil}-";
        var numaralar = await db.Faturalar.AsNoTracking()
            .Where(f => f.TenantId == tenantId && EF.Functions.Like(f.FaturaNo, onEk + "%"))
            .Select(f => f.FaturaNo)
            .ToListAsync();

        var enBuyuk = 0;
        foreach (var no in numaralar)
        {
            // P-22: TASLAK belgenin numarası NULL'dur; sayaca girmez.
            if (no is null) continue;
            var kuyruk = no[onEk.Length..];
            if (int.TryParse(kuyruk, NumberStyles.None, CultureInfo.InvariantCulture, out var sira)
                && sira > enBuyuk)
                enBuyuk = sira;
        }

        return $"{onEk}{enBuyuk + 1:D6}";
    }

    /// <summary>
    /// Gider pusulası numarası: GP-2026-000001. Fatura numarasıyla AYNI desen
    /// (MAX+1, COUNT+1 değil — silinen kayıt numara tekrarına yol açmasın).
    /// </summary>
    public static async Task<string> SiradakiPusula(KuyumcuDbContext db, string tenantId, int yil)
    {
        var onEk = $"GP-{yil}-";
        var numaralar = await db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == tenantId && EF.Functions.Like(g.PusulaNo, onEk + "%"))
            .Select(g => g.PusulaNo)
            .ToListAsync();

        var enBuyuk = 0;
        foreach (var no in numaralar)
        {
            var kuyruk = no[onEk.Length..];
            if (int.TryParse(kuyruk, NumberStyles.None, CultureInfo.InvariantCulture, out var sira)
                && sira > enBuyuk)
                enBuyuk = sira;
        }

        return $"{onEk}{enBuyuk + 1:D6}";
    }
}
