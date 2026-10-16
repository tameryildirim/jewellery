using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Gider;

/// <summary>
/// PAKET P-19 (Y-12) — DÖNEM GİDERİ ÖLÇÜMÜ (kâr-zararın okuduğu tek kaynak).
///
/// <para>
/// ⚠ NEDEN <c>Modules/Maliyet</c> İÇİNDE DEĞİL: gider ölçümü giderin kendi
/// modülünün sorumluluğudur; kâr-zarar onu ÇAĞIRIR, yeniden yazmaz. İkinci bir
/// gider toplamı yazılsaydı (ör. gider ekranında başka, kâr-zararda başka
/// süzgeç), aynı masraf iki farklı rakamla görünürdü.
/// </para>
/// <para>
/// ⚠ SINIRLILIK (TB-19 hijyeni): sorgu sunucuda GROUP BY yapar; belleğe gelen
/// satır sayısı KATEGORİ SAYISI kadardır, hareket sayısı kadar DEĞİL.
/// </para>
/// <para>
/// ⚠ İŞARET: gider satırı kasa_hareketleri'nde NEGATİFTİR; toplam
/// <c>SUM(-tutar)</c> ile POZİTİF okunur. Storno satırı POZİTİF tutarlı ve AYNI
/// kategoriliDİR → stornolanan gider toplamdan kendiliğinden düşer.
/// </para>
/// </summary>
public static class GiderOlcumu
{
    /// <summary>
    /// ⚠ Tip adı BİLEREK uzundur: "Sonuc" adı Kuyumcu.Contracts tarafında zaten
    /// vardı ve iki ayrı "Sonuc" tanımı JSON eşleşmesini sessizce bozabilirdi
    /// (denetim.sh P-03 CONTRACTS_DISI_MODEL bunu yakaladı).
    /// </summary>
    public sealed record GiderDonemOlcumu(decimal ToplamTl, IReadOnlyList<object> Kirilim);

    public static async Task<GiderDonemOlcumu> Donem(
        KuyumcuDbContext db, string tenantId, DateTime bas, DateTime sonBit, string? subeId)
    {
        var q = db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == tenantId
                     && h.GiderKategoriId != null
                     && h.Tarih >= bas && h.Tarih < sonBit);

        // kasa_hareketleri'nde sube_id YOKTUR; şube bilgisi KASADAN gelir.
        if (!string.IsNullOrEmpty(subeId))
        {
            var subeKasalari = db.Kasalar.AsNoTracking()
                .Where(k => k.TenantId == tenantId && k.SubeId == subeId)
                .Select(k => k.Id);
            q = q.Where(h => subeKasalari.Contains(h.KasaId));
        }

        var ham = await q
            .GroupBy(h => h.GiderKategoriId!)
            .Select(g => new
            {
                KategoriId = g.Key,
                TutarTl = g.Sum(x => -x.Tutar),
                Adet = g.Count(),
                // Kategori adı AYNI sorguda çözülür: ikinci bir materyalizasyon
                // açılmaz (rapor hattının TB-19 tavanı korunur).
                KategoriAdi = db.GiderKategorileri
                    .Where(k => k.Id == g.Key).Select(k => k.Ad).FirstOrDefault(),
            })
            .ToListAsync();

        var kirilim = ham
            .OrderByDescending(x => x.TutarTl)
            .Select(x => (object)new
            {
                kategoriId = x.KategoriId,
                kategoriAdi = x.KategoriAdi,
                tutarTl = Math.Round(x.TutarTl, 2),
                adet = x.Adet,
            })
            .ToList();

        return new GiderDonemOlcumu(ham.Sum(x => x.TutarTl), kirilim);
    }
}
