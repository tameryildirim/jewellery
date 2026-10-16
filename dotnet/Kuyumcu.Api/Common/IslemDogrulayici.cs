using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

/// <summary>
/// Kiracı sınırının TEK kapısı (K1 — Mehmet/Güvenlik kararı, 2026-09-10).
/// Kullanıcıdan gelen her yabancı kimlik (cari, kasa, şube, kur snapshot)
/// işlem transaction'ı açılmadan ÖNCE buradan geçer. Bu kapıdan geçmeyen bir
/// kimlik hiçbir sorguda kullanılmaz.
///
/// Kanıtlanan açık: doğrulama yokken B işletmesi, A'nın kasaId/cariId
/// değerleriyle satış gönderip A'nın kasasına para, carisine borç yazabiliyordu.
/// </summary>
public static class IslemDogrulayici
{
    /// <summary>İşlem anındaki kur kaydı bu kiracıya ait olmalı.</summary>
    public static async Task KurDogrula(KuyumcuDbContext db, string kurSnapshotId, string tenantId)
    {
        var gecerli = await db.KurSnapshots.AsNoTracking()
            .AnyAsync(s => s.Id == kurSnapshotId && s.TenantId == tenantId);
        if (!gecerli) throw new NotFoundException("Kur kaydı bulunamadı");
    }

    /// <summary>Cari bu kiracıya ait olmalı. Boş/null kimlik sessizce geçer (cari opsiyonel uçlar için).</summary>
    public static async Task CariDogrula(KuyumcuDbContext db, string? cariId, string tenantId)
    {
        if (string.IsNullOrEmpty(cariId)) return;
        var gecerli = await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.Id == cariId && c.TenantId == tenantId);
        if (!gecerli) throw new NotFoundException("Cari bulunamadı");
    }

    /// <summary>
    /// PAKET P-16 (B-31) — SATIŞ carisi AKTİF de olmalı.
    ///
    /// <para>
    /// ⚠ NEDEN <see cref="CariDogrula"/>'ya EKLENMEDİ: pasif cariden TAHSİLAT
    /// yapılabilmelidir (kart kapatılırken borç tahsil edilir), ekstresi
    /// okunabilmelidir, düzeltme fişi kesilebilmelidir. Pasiflik "bu cariyle
    /// YENİ İŞ yapma" demektir, "bu cari yok" demek değildir. Bu yüzden ek
    /// kapı AYRI bir çağrıdır ve yalnız YENİ BORÇ/SATIŞ doğuran yollarda
    /// çağrılır.
    /// </para>
    /// <para>
    /// Ekran pasif cariyi listeden gizler; bu kapı ekranın gizlemesine
    /// GÜVENMEZ — doğrudan API'ye gönderilen kimlik de reddedilir (fail-closed).
    /// </para>
    /// </summary>
    public static async Task CariAktifDogrula(KuyumcuDbContext db, string? cariId, string tenantId)
    {
        if (string.IsNullOrEmpty(cariId)) return;
        var aktif = await db.Cariler.AsNoTracking()
            .Where(c => c.Id == cariId && c.TenantId == tenantId)
            .Select(c => (bool?)c.Aktif)
            .FirstOrDefaultAsync();
        if (aktif is null) throw new NotFoundException("Cari bulunamadı");
        if (aktif == false)
            throw new BadRequestException(
                "Pasif cariyle yeni satış yapılamaz. Önce cari kartını aktifleştirin " +
                "(Cariler ekranı → ilgili kart → Aktife al).");
    }

    /// <summary>
    /// MASAK kimlik tespiti bu kiracıya ait olmalı. Boş/null sessizce geçer
    /// (şüpheli bildirim kimlik tespitine bağlı olmak zorunda değildir).
    /// </summary>
    /// <remarks>
    /// ⚠ 2026-09-13 GÜVENLİK BULGUSU (Mehmet, ADR-0009 oylaması):
    /// <c>MasakModulu.SupheliOlustur</c> içinde <c>HareketId</c> ve <c>CariId</c>
    /// doğrulanıyordu ama <c>KimlikTespitiId</c> kullanıcı gövdesinden gelip
    /// DOĞRULANMADAN yazılıyordu. Yani başka bir kiracının kimlik tespiti
    /// kaydının kimliği, bu kiracının APPEND-ONLY şüpheli işlem bildirimine
    /// bağlanabiliyordu — ve kayıt append-only olduğu için sonradan da
    /// düzeltilemezdi. Kiracı sorgu filtresi bunu engellemez: filtre OKUMADA
    /// çalışır, INSERT'te yazılan ham kimlik değerini denetlemez.
    /// </remarks>
    public static async Task KimlikTespitiDogrula(
        KuyumcuDbContext db, string? kimlikTespitiId, string tenantId)
    {
        if (string.IsNullOrEmpty(kimlikTespitiId)) return;
        var gecerli = await db.MasakKimlikTespitleri.AsNoTracking()
            .AnyAsync(k => k.Id == kimlikTespitiId && k.TenantId == tenantId);
        if (!gecerli) throw new NotFoundException("Kimlik tespiti bulunamadı");
    }

    /// <summary>
    /// Kasa bu kiracıya ait olmalı; <paramref name="beklenenParaBirimi"/> verilirse
    /// kasanın dövizi tutmalı (K6 — TL tutarı döviz kasasına yazılamaz).
    /// İzlenen (tracked) varlık döner; bakiye çağıran tarafından güncellenir.
    /// </summary>
    /// <param name="an">
    /// İşlem anı. VERİLİRSE gün sonu kapanışı kilidi de uygulanır: kapanmış bir
    /// kasa-gününe yeni hareket yazılamaz. Verilmezse kilit kontrol EDİLMEZ —
    /// salt okunur çağrılar için.
    /// </param>
    public static async Task<Kasa> KasaDogrula(
        KuyumcuDbContext db, string kasaId, string tenantId,
        string? beklenenParaBirimi = null, DateTime? an = null)
    {
        var kasa = await db.Kasalar.FirstOrDefaultAsync(k => k.Id == kasaId && k.TenantId == tenantId)
            ?? throw new NotFoundException($"Kasa bulunamadı: {kasaId}");

        if (beklenenParaBirimi is not null && kasa.ParaBirimi != beklenenParaBirimi)
            throw new BadRequestException(
                $"Bu işlem {beklenenParaBirimi} kasası gerektirir; '{kasa.Ad}' kasasının para birimi {kasa.ParaBirimi}");

        if (an is { } zaman)
            await Modules.Kasa.GunKapanisiKapisi.Dogrula(db, tenantId, kasaId, zaman);

        return kasa;
    }

    /// <summary>Şube bu kiracıya ait olmalı (transferde hem kaynak hem hedef).</summary>
    public static async Task SubeDogrula(KuyumcuDbContext db, string subeId, string tenantId)
    {
        var gecerli = await db.Subeler.AsNoTracking()
            .AnyAsync(s => s.Id == subeId && s.TenantId == tenantId);
        if (!gecerli) throw new NotFoundException("Şube bulunamadı");
    }
}
