using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Abonelik;

// ═════════════════════════════════════════════════════════════════════════════
// ABONELİK SERVİSİ — PAKET P-27 (F-1 + F-3 · R-1) · KOŞULSUZ 1.0.
//
// ÜÇ KURAL, ÜÇÜ DE BURADA (ekranda ya da controller'da DEĞİL):
//   (1) PLAN YAZAN TEK YOL BURASIDIR. `tenants.plan` alanına başka hiçbir
//       serviste atama yapılmaz — dotnet/denetim.sh `YETKISIZ_PLAN_YAZAN_UC`
//       sayacı bunu ölçer. İkinci bir yazma yolu, kapısı olmayan bir yol demek.
//   (2) KİRACI KENDİ PLANINI YÜKSELTEMEZ. Yükseltme = ücret doğuran hak
//       genişlemesi; yalnız sistem anahtarıyla korunan operatör ucundan olur.
//       Kiracının denemesi 403 döner VE reddedilen girişim denetime yazılır.
//   (3) HİÇBİR PLAN DEĞİŞİMİ İZSİZ OLAMAZ. Her yazma yolu aynı transaction
//       içinde denetim kaydı üretir (`PLAN_DEGISIMI_IZSIZ`).
//
// FAIL-CLOSED (DoD): abonelik dönemi bitince kiracı YAZAMAZ ama VERİSİNİ
// KAYBETMEZ — okuma, görüntüleme ve dışa aktarım açık kalır. Kapı mevcut
// KiraciDurumKapisi'dır; bu paket ona İKİNCİ bir zaman kaynağı ekler
// (abonelik bitişi), yeni bir kapı deseni AÇMAZ.
//
// ⚠ FİYAT/TAHSİLAT YOKTUR (K-16 · MÜHÜR 2): plan kataloğunda tutar kolonu
//   yoktur, hiçbir uç para tahsil etmez. Bu paket "kim hangi hakka sahip ve
//   bunu kim değiştirdi" sorusunu kapatır; rakam kurucudadır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Katalogdaki bir plan (fiyat YOK — K-16).</summary>
public sealed record AbonelikPlanDto(
    string Kod, string Ad, string? Aciklama, int Seviye,
    int? KullaniciTavan, int? SubeTavan, bool Aktif);

/// <summary>Bir abonelik DÖNEMİ (append-only tarihçenin bir satırı).</summary>
public sealed record AbonelikDonemDto(
    string Id, string PlanKodu, DateTime Baslangic, DateTime? Bitis,
    string Durum, string Kaynak, string? Gerekce, string? OperatorAdi);

/// <summary>
/// Kiracının abonelik görünümü. Ekran hiçbir süre/seviye hesabı YAPMAZ
/// (UX-S21): kalan gün, yükseltilebilirlik ve indirilebilir plan listesi
/// sunucudan gelir.
/// </summary>
public sealed record AbonelikDurumDto(
    string PlanKodu,
    string PlanAdi,
    int? Seviye,
    AbonelikDonemDto? Donem,
    int? KalanGun,
    bool SuresiDolduMu,
    bool YazabilirMi,
    bool YukseltmeKiraciyaKapali,
    string YukseltmeKapaliSebebi,
    IReadOnlyList<AbonelikPlanDto> Planlar,
    IReadOnlyList<string> IndirilebilirPlanlar,
    IReadOnlyList<AbonelikDonemDto> Gecmis,
    int GecmisSiniri,
    bool GecmisSinirinaDegdi);

public sealed class AbonelikServisi(
    KuyumcuDbContext db,
    DenetimServisi denetim,
    KiraciDurumOnbellegi onbellek)
{
    /// <summary>Denetim izindeki varlık türü — SQL süzgeci: varlik_turu='KiraciAbonelik'.</summary>
    public const string DenetimVarligi = nameof(KiraciAbonelik);

    /// <summary>
    /// Plan değişimi izinin İLK sözcüğü. Denetim tablosunda ayrı "işlem" kolonu
    /// yoktur; damga detayın başına yazılır → detay LIKE 'PLAN_DEGISIMI%'.
    /// `PLAN_DEGISIMI_IZSIZ` sayacı canlıda BU damgayı arar.
    /// </summary>
    public const string IzDamgasi = "PLAN_DEGISIMI";

    /// <summary>Reddedilen yükseltme girişiminin damgası → detay LIKE 'PLAN_YUKSELTME_RED%'.</summary>
    public const string RedDamgasi = "PLAN_YUKSELTME_RED";

    /// <summary>Tarihçe listesi sınırı — UX-S29: sınır ekranda YAZILIR, sessiz kırpma yok.</summary>
    public const int GecmisSiniri = 50;

    public const string YukseltmeRedMesaji =
        "Plan yükseltme işletme hesabından yapılamaz: abonelik hakkını genişletmek "
        + "yetkili operatör onayı gerektirir. Talebinizi yöneticinize iletin; plan "
        + "indirme işlemini buradan kendiniz yapabilirsiniz.";

    // ── OKUMA ────────────────────────────────────────────────────────────────

    public async Task<AbonelikDurumDto> DurumGetir(AktifKullanici islem)
    {
        var now = DateTime.UtcNow;
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        var planlar = await PlanlariOku();
        var donem = await AktifDonemOku(db, islem.TenantId);

        // Tarihçe: en yenisi başta. Sınır AŞILDI mı da ölçülür (UX-S29).
        var gecmisHam = await db.Set<KiraciAbonelik>().AsNoTracking()
            .Where(a => a.TenantId == islem.TenantId)
            .OrderByDescending(a => a.Baslangic).ThenByDescending(a => a.Olusturuldu)
            .Take(GecmisSiniri + 1)
            .ToListAsync();
        var degdi = gecmisHam.Count > GecmisSiniri;
        var gecmis = gecmisHam.Take(GecmisSiniri).Select(Yansit).ToList();

        // Geçerli plan kodu: abonelik satırı VARSA odur; yoksa tenants.plan
        // (abonelik satırı doğmamış eski kiracı). Uydurulmaz, olan yazılır.
        var kod = donem?.PlanKodu ?? tenant.Plan;
        var plan = planlar.FirstOrDefault(p => p.Kod == kod);

        var bitis = donem?.Bitis;
        var doldu = bitis is not null && bitis.Value < now;
        var durum = new KiraciDurumu(tenant.Durum ?? "", tenant.DenemeBitis, bitis);

        return new AbonelikDurumDto(
            kod,
            plan?.Ad ?? kod,
            plan?.Seviye,
            donem is null ? null : Yansit(donem),
            KalanGun(bitis, now),
            doldu,
            durum.YazabilirMi(now),
            YukseltmeKiraciyaKapali: true,
            YukseltmeRedMesaji,
            [.. planlar.Select(p => new AbonelikPlanDto(
                p.Kod, p.Ad, p.Aciklama, p.Seviye, p.KullaniciTavan, p.SubeTavan, p.Aktif))],
            [.. planlar.Where(p => p.Aktif && plan is not null && p.Seviye < plan.Seviye)
                      .OrderByDescending(p => p.Seviye).Select(p => p.Kod)],
            gecmis,
            GecmisSiniri,
            degdi);
    }

    // ── KİRACI YOLU (yalnız İNDİRME) ─────────────────────────────────────────

    /// <summary>
    /// Kiracının kendi planını değiştirmesi. YÜKSELTME 403'tür (F-3 plan kapısı).
    /// Aynı plan gönderilirse hiçbir şey yazılmaz ve iz de üretilmez.
    /// </summary>
    public async Task<AbonelikDurumDto> KiraciPlanDegistir(string? plan, AktifKullanici islem)
    {
        // Servis içi ELLE doğrulama: DTO DataAnnotations YALNIZ controller yolunda koşar.
        // ⚠ TRIM YOK (P-09 K3 dersi): "KURUMSAL " kırpılıp kabul edilseydi, plan
        // kodu boşlukla kaçırılabilir ve katalog eşleşmesi gevşerdi. Kod BİREBİR
        // eşleşir; eşleşmiyorsa katalog dışıdır.
        var kod = plan ?? "";
        if (string.IsNullOrWhiteSpace(kod)) throw new BadRequestException("Plan kodu zorunludur");

        var now = DateTime.UtcNow;
        var planlar = await PlanlariOku();
        var yeni = planlar.FirstOrDefault(p => p.Kod == kod)
            ?? throw new BadRequestException(
                $"Geçersiz plan. Geçerli planlar: {string.Join(", ", planlar.Where(p => p.Aktif).Select(p => p.Kod))}");
        if (!yeni.Aktif)
            throw new BadRequestException($"'{yeni.Ad}' planı kapalıdır, yeni abonelikte seçilemez.");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");
        var donem = await AktifDonemOku(db, islem.TenantId);
        var mevcutKod = donem?.PlanKodu ?? tenant.Plan;

        if (mevcutKod == yeni.Kod) return await DurumGetir(islem);   // değişmediyse iz de yazılmaz

        var mevcut = planlar.FirstOrDefault(p => p.Kod == mevcutKod);

        // FAIL-CLOSED: mevcut plan katalogda yoksa seviye KIYASLANAMAZ → yükseltme sayılır.
        if (mevcut is null || yeni.Seviye > mevcut.Seviye)
        {
            // Reddedilen girişim KENDİ transaction'ında yazılır (RedKaydet) —
            // 403 atılınca iz de geri alınsaydı saldırı sinyali kaybolurdu.
            await denetim.RedKaydet(islem, DenetimVarligi, tenant.Id,
                $"{RedDamgasi}: {mevcutKod} → {yeni.Kod}");
            throw new ForbiddenException(YukseltmeRedMesaji);
        }

        var satir = DonemDegistir(db, tenant, donem, yeni.Kod,
            bitis: donem?.Bitis, kaynak: AbonelikKaynaklari.Kiraci,
            gerekce: null, operatorAdi: null, now);

        denetim.YazmaKaydet(islem, DenetimVarligi, satir.Id,
            $"{IzDamgasi}: {mevcutKod} → {yeni.Kod} · kaynak: {AbonelikKaynaklari.Kiraci} · yön: indirme");

        await db.SaveChangesAsync();
        onbellek.Bosalt(tenant.Id);
        return await DurumGetir(islem);
    }

    // ── Ortak yazma çekirdeği ────────────────────────────────────────────────

    /// <summary>
    /// TEK YAZMA ÇEKİRDEĞİ: eski dönemi SONLANDIRIR (silmez — append-only),
    /// yeni dönemi açar ve <c>tenants.plan</c> yansımasını günceller.
    /// <para>⚠ SaveChanges ÇAĞIRMAZ: çağıranın tek transaction'ı ile yazılır —
    /// dönem yazılıp iz yazılamazsa ikisi birden geri alınır.</para>
    /// </summary>
    internal static KiraciAbonelik DonemDegistir(
        KuyumcuDbContext hedef, Data.Tenant tenant, KiraciAbonelik? eski, string planKodu,
        DateTime? bitis, string kaynak, string? gerekce, string? operatorAdi, DateTime now)
    {
        if (eski is not null)
        {
            eski.Durum = AbonelikDurumlari.Sonlandi;
            eski.Bitis = now;               // dönem BURADA kapandı
            eski.Guncellendi = now;
        }

        var satir = new KiraciAbonelik
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenant.Id,
            PlanKodu = planKodu,
            Baslangic = now,
            Bitis = bitis,
            Durum = AbonelikDurumlari.Aktif,
            Kaynak = kaynak,
            Gerekce = gerekce,
            OperatorAdi = operatorAdi,
            Olusturuldu = now,
            Guncellendi = now,
        };
        hedef.Set<KiraciAbonelik>().Add(satir);

        // YANSIMA: tenants.plan okuma kolaylığıdır, KAYIT kiraci_abonelikleri'dir.
        // Bu atama bu dosyanın DIŞINDA hiçbir yerde yapılmaz (YETKISIZ_PLAN_YAZAN_UC).
        tenant.Plan = planKodu;
        tenant.Guncellendi = now;
        return satir;
    }

    /// <summary>Katalog tavanı — B-8: sınırsız ToListAsync yoktur, liste küçük de olsa sınırlıdır.</summary>
    public const int PlanTavani = 200;

    private Task<List<AbonelikPlani>> PlanlariOku() =>
        db.Set<AbonelikPlani>().AsNoTracking().OrderBy(p => p.Seviye).Take(PlanTavani).ToListAsync();

    /// <summary>Aktif dönem — yoksa null (uydurulmaz).</summary>
    internal static Task<KiraciAbonelik?> AktifDonemOku(KuyumcuDbContext hedef, string tenantId) =>
        hedef.Set<KiraciAbonelik>()
            .Where(a => a.TenantId == tenantId && a.Durum == AbonelikDurumlari.Aktif)
            .OrderByDescending(a => a.Baslangic)
            .FirstOrDefaultAsync();

    public static int? KalanGun(DateTime? bitis, DateTime now) =>
        bitis is null ? null : (int)Math.Ceiling((bitis.Value - now).TotalDays);

    private static AbonelikDonemDto Yansit(KiraciAbonelik a) => new(
        a.Id, a.PlanKodu, a.Baslangic, a.Bitis, a.Durum, a.Kaynak, a.Gerekce, a.OperatorAdi);
}

// ═════════════════════════════════════════════════════════════════════════════
// ABONELİK SİSTEM SERVİSİ — TEK YÜKSELTME YOLU (PAKET P-27).
//
// ⚠ NEDEN AYRI SINIF (ölçülmüş zorunluluk, tercih değil): /api/v1/sistem/*
//   uçları ANONİMDİR; istek kapsamında kiracı bağlamı YOKTUR ve bu yüzden
//   request-scoped KuyumcuDbContext HİÇ ÇÖZÜLEMEZ ("Kiracı bağlamı çözülemedi").
//   AbonelikServisi db enjekte eder → sistem ucunda 500 verirdi (2026-09-22
//   koşumunda gerçekten verdi). SistemServisi kalıbının AYNISI: yalnız
//   SistemKapsami alınır, db kapsam içinden çözülür.
//
// ⚠ YAZMA ÇEKİRDEĞİ ORTAK: dönem kapatma/açma ve tenants.plan yansıması
//   AbonelikServisi.DonemDegistir'dedir — iki kopya kural olamaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class AbonelikSistemServisi(
    SistemKapsami kapsam,
    KiraciDurumOnbellegi onbellek,
    IHttpContextAccessor? erisim = null)
{
    /// <summary>
    /// Operatörün plan/abonelik dönemi ataması. TEK YÜKSELTME YOLUDUR ve
    /// sistem anahtarı + operatör kimliği + ≥20 karakter gerekçe ister.
    /// </summary>
    public async Task<object> SistemPlanDegistir(string tenantId, SistemAbonelikDto dto, string operatorAdi)
    {
        // Servis içi ELLE doğrulama (DataAnnotations yalnız controller yolunda).
        var op = (operatorAdi ?? "").Trim();
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (op.Length < 3) throw new BadRequestException("Operatör kimliği zorunludur (en az 3 karakter)");
        if (gerekce.Length < 20) throw new BadRequestException("Gerekçe en az 20 karakter olmalıdır");

        var now = DateTime.UtcNow;
        if (dto.Bitis is not null && dto.Bitis.Value <= now)
            throw new BadRequestException("Abonelik bitişi ileri bir tarih olmalıdır");

        await using var sistem = kapsam.Sistem();
        var sdb = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var planlar = await sdb.Set<AbonelikPlani>().AsNoTracking()
            .OrderBy(p => p.Seviye).Take(AbonelikServisi.PlanTavani).ToListAsync();
        var kod = dto.Plan ?? "";   // TRIM YOK — kiracı yoluyla aynı katı eşleşme
        var yeni = planlar.FirstOrDefault(p => p.Kod == kod)
            ?? throw new BadRequestException(
                $"Geçersiz plan. Geçerli planlar: {string.Join(", ", planlar.Select(p => p.Kod))}");
        if (!yeni.Aktif)
            throw new BadRequestException($"'{yeni.Ad}' planı kapalıdır, yeni abonelikte seçilemez.");

        var tenant = await sdb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new NotFoundException("Kiracı bulunamadı");
        var donem = await AbonelikServisi.AktifDonemOku(sdb, tenantId);
        var eskiKod = donem?.PlanKodu ?? tenant.Plan;

        var satir = AbonelikServisi.DonemDegistir(sdb, tenant, donem, yeni.Kod, dto.Bitis,
            AbonelikKaynaklari.Sistem, gerekce, op, now);

        // İz, iş kaydıyla AYNI transaction'da yazılır (SaveChanges aşağıda TEK).
        sistem.ServiceProvider.GetRequiredService<DenetimServisi>().SistemYazmaKaydet(
            tenantId, AbonelikServisi.DenetimVarligi, satir.Id,
            $"{AbonelikServisi.IzDamgasi}: {eskiKod} → {yeni.Kod}"
            + $" · kaynak: {AbonelikKaynaklari.Sistem} · operatör: {op}"
            + $" · bitiş: {(dto.Bitis?.ToString("O") ?? "süresiz")}"
            + $" · gerekçe: {gerekce}"
            + $" · istek: {erisim?.HttpContext?.TraceIdentifier ?? "-"}");

        await sdb.SaveChangesAsync();
        onbellek.Bosalt(tenantId);   // süre uzatma GECİKMELİ etki etmesin

        return new
        {
            tenantId,
            plan = yeni.Kod,
            planAdi = yeni.Ad,
            eskiPlan = eskiKod,
            abonelikId = satir.Id,
            baslangic = satir.Baslangic,
            bitis = satir.Bitis,
            kalanGun = AbonelikServisi.KalanGun(satir.Bitis, now),
            yazabilirMi = new KiraciDurumu(tenant.Durum ?? "", tenant.DenemeBitis, satir.Bitis).YazabilirMi(now),
        };
    }
}
