using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-04 (kimlik Y-1) — OTURUM ŞUBESİ SÜZGECİNİN TEK YOLU.
//
// NEDEN: şube izolasyonunun asıl mekanizması global sorgu filtresidir
// (KuyumcuDbContext.KiraciVeSubeFiltresiKur). Ama o filtre "TÜM şubeler"
// ayrıcalığı olan rollerde (SAHIP/MUDUR/MUHASEBE) BİLEREK kurulmaz; oysa bazı
// iş akışları rol ne olursa olsun TEZGÂHIN şubesinde yürür — vezne kuyruğu
// bunun örneğidir (R6: başka şubenin fişi kuyrukta görünmez).
//
// Bu kural ELLE `x.SubeId == subeId` yazılarak 3 ayrı yerde tekrarlanıyordu.
// Elle yazılan her kopya, bir sonraki uçta UNUTULABİLİR olandır. Kural tek
// yerde durur; `denetim.sh` → GUVENLIK_ELLE_IZOLASYON sayacı Modules altında
// elle yazılmış yeni bir izolasyon süzgeci belirirse KIRMIZI verir.
//
// FAIL-CLOSED: oturumun şubesi yoksa sorgu "süzgeçsiz" devam etmez; 400 ile
// durur. Sessiz genişleme (fail-open) ve sessiz boşluk (fail-blind) yasaktır.
// ═════════════════════════════════════════════════════════════════════════════

public static class SubeSuzgeci
{
    /// <summary>
    /// Sorguyu oturumun şubesine kilitler. Şube yoksa <see cref="BadRequestException"/>.
    /// </summary>
    public static IQueryable<T> OturumSubesinde<T>(
        this IQueryable<T> sorgu, AktifKullanici islem, string hataMesaji)
        where T : class, ISubeVarlik
    {
        var sube = string.IsNullOrEmpty(islem.SubeId)
            ? throw new BadRequestException(hataMesaji)
            : islem.SubeId!;
        return sorgu.Where(e => e.SubeId == sube);
    }
}
