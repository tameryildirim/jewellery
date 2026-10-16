using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-A § 4.1 / § 4.2.1 — GÖRÜNÜM ŞUBESİ (tek yardımcı)
//
// İKİ AYRI KAVRAM, İKİ AYRI İSİM:
//   • YETKİ KAPSAMI (değişmez): kullanıcının GÖREBİLECEĞİ şubeler. Taban P-04
//     global süzgeci (KuyumcuDbContext) + SubeYetkisi.TumSubelerRolleri.
//   • GÖRÜNÜM ŞUBESİ (bu dosya): tüm-şube yetkili kullanıcının O AN baktığı şube
//     (null = tüm şubeler). Yalnız DARALTIR; asla genişletmez; yazma yolunu
//     ve yasal kümülatifi (tevsik/MASAK) ETKİLEMEZ.
//
// Kural (Coz):
//   Şubeye kilitli rol : istenen boş → oturum şubesi; istenen ≠ oturum şubesi →
//                        403 (sessiz düşürme yok — AramaModulu deseni).
//   Tüm-şube rolü      : istenen boş (ya da "TUMU") → null (birleşik);
//                        istenen dolu → kiracıda AKTİF şube mi? değilse 404.
//
// Daralt: `SubeId ==` satırı TEK yerde (sube-suzgec-siniflandirma.txt sınıf b).
// ═════════════════════════════════════════════════════════════════════════════

public static class GorunumSubesi
{
    /// <summary>Tercih/istek değeri: "Tüm şubeler".</summary>
    public const string Tumu = "TUMU";

    /// <summary>
    /// İstenen görünüm şubesini çözer. Dönüş null = birleşik (tüm şubeler).
    /// </summary>
    /// <exception cref="ForbiddenException">Şubeye kilitli rol başka şube istedi.</exception>
    /// <exception cref="NotFoundException">Şube kiracıda yok ya da pasif.</exception>
    public static async Task<string?> Coz(KuyumcuDbContext db, AktifKullanici islem, string? istenen)
    {
        var bos = string.IsNullOrWhiteSpace(istenen);
        var deger = bos ? null : istenen!.Trim();
        var tumu = deger is not null && string.Equals(deger, Tumu, StringComparison.OrdinalIgnoreCase);

        if (!SubeYetkisi.TumSubeler(islem.Rol))
        {
            // FAIL-CLOSED: şubeye kilitli rolde oturum şubesi yoksa "birleşik"e
            // düşülmez (sessiz genişleme yok) — P-04 sözleşmesi md.3 ile aynı ilke.
            if (string.IsNullOrEmpty(islem.SubeId))
                throw new BadRequestException("Şube bağlamı gerekli: bu rol yalnız kendi şubesini görür ve oturumda şube yok.");
            if (bos) return islem.SubeId;
            if (tumu || !string.Equals(deger, islem.SubeId, StringComparison.Ordinal))
                throw new ForbiddenException("Bu şubeyi görme yetkiniz yok.");
            return islem.SubeId;
        }

        if (bos || tumu) return null;

        // Kiracı süzgeci global filtreden gelir; TenantId elle de yazılır (çok katmanlı).
        var aktif = await db.Subeler.AsNoTracking()
            .AnyAsync(s => s.Id == deger && s.TenantId == islem.TenantId && s.Aktif);
        if (!aktif) throw new NotFoundException("Şube bulunamadı ya da pasif.");
        return deger;
    }

    /// <summary>
    /// Görünüm daraltması: subeId doluysa yalnız o şubenin satırları. Boşsa sorgu
    /// değişmez (P-04 yetki süzgeci neyse o). TEK `SubeId ==` satırı buradadır.
    /// </summary>
    public static IQueryable<T> Daralt<T>(this IQueryable<T> q, string? subeId) where T : class, ISubeVarlik =>
        string.IsNullOrEmpty(subeId) ? q : q.Where(e => e.SubeId == subeId);

    /// <summary>Yanıtta taşınan kapsam etiketi (ekran rozeti buna göre yazar).</summary>
    public static string Kapsam(string? subeId) => subeId is null ? "TUM_SUBELER" : "SUBE";

    /// <summary>Kiracı geneli (şube bilmeyen) uçların yanıt etiketi.</summary>
    public const string KiraciGeneli = "KIRACI_GENELI";
}
