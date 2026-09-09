using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kur;

// ═════════════════════════════════════════════════════════════════════════════
// Kur modülü — NestJS portu.
// Kaynak: apps/api/src/modules/kur/*
//   • kur.controller.ts / kur.service.ts → KurController / KurServisi
//   • adaptorler/*                       → IKurAdaptoru / AltinApiAdaptoru
//   • kur.scheduler.ts (@Cron)           → KurZamanlayici (BackgroundService)
// Rota ve JSON alan adları NestJS API'siyle birebir aynı (Türkçe camelCase).
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class ManuelKurDto
{
    [Required(ErrorMessage = "Altın has fiyatı sayı olmalıdır")]
    [Range(1, double.MaxValue, ErrorMessage = "Altın fiyatı sıfırdan büyük olmalıdır")]
    public decimal AltinHasFiyati { get; set; }

    /// <summary>Opsiyonel: piyasa gram altın (24 ayar) satış fiyatı — vitrin için.</summary>
    [Range(0, double.MaxValue)] public decimal? GramAltinFiyati { get; set; }

    [Range(0, double.MaxValue)] public decimal? UsdTry { get; set; }
    [Range(0, double.MaxValue)] public decimal? EurTry { get; set; }
    [Range(0, double.MaxValue)] public decimal? GbpTry { get; set; }
}

public sealed class KurFiltreDto
{
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }

    [RegularExpression("HAREM|KAPALICARSI|IAB|MANUAL", ErrorMessage = "Geçersiz kaynak")]
    public string? Kaynak { get; set; }

    [Range(1, 500)]
    public int? Limit { get; set; }
}

/// <summary>Ayar bazlı fiyat — 1 gram bu ayarda kaç TL (Nest: AyarFiyati).</summary>
public sealed record AyarFiyati(
    string AyarId,
    string Label,
    int Milyem,
    decimal GramFiyati,   // TL/gram (has fiyatı × milyem/1000)
    decimal HasFiyati);   // TL/has gram (referans)

/// <summary>Zenginleştirilmiş kur snapshot yanıtı — Nest snapshotZenginlestir çıktısıyla birebir.</summary>
public sealed record KurSnapshotCevap(
    string Id,
    string TenantId,
    decimal AltinHasFiyati,
    decimal? GramAltinFiyati,
    decimal? UsdTry,
    decimal? EurTry,
    decimal? GbpTry,
    string Kaynak,
    DateTime Olusturuldu,
    List<AyarFiyati> AyarFiyatlari);

// ── Dış kur kaynağı adaptörü ─────────────────────────────────────────────────

/// <summary>Dış kaynaktan gelen ham kur verisi (Nest: DisKurVerisi).</summary>
public sealed record DisKurVerisi(
    decimal AltinHasFiyati,
    decimal? GramAltinFiyati,
    decimal? UsdTry,
    decimal? EurTry,
    decimal? GbpTry,
    string Kaynak);

/// <summary>Dış kur kaynağı adaptörü — her kaynak bu arayüzü uygular.</summary>
public interface IKurAdaptoru
{
    Task<DisKurVerisi> KurCek(CancellationToken iptal = default);
}

/// <summary>
/// truncgil finans adaptörü — ücretsiz, anahtarsız Türk altın + döviz verisi.
/// Kaynak: https://finans.truncgil.com/today.json
/// (Eski api.altinapi.com kaynağı kapandığı için buraya geçildi.)
///
/// Yanıt, anahtar → { "Alış", "Tür", "Satış", "Değişim" } biçiminde nesneler içerir.
/// Değerler tr-TR biçimindedir ("6.137,22"); Satış fiyatları kullanılır.
///   • gram-has-altin → has (24 ayar saf) TL/gram → altinHasFiyati (paralel defter tabanı)
///   • gram-altin     → piyasa gram altın (24 ayar) → gramAltinFiyati (vitrin)
///   • USD / EUR / GBP → döviz TL karşılığı
/// </summary>
public sealed class TruncgilAdaptoru(IHttpClientFactory httpFactory, ILogger logger) : IKurAdaptoru
{
    private const string Url = "https://finans.truncgil.com/today.json";
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<DisKurVerisi> KurCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(8));

        var http = httpFactory.CreateClient();
        using var istek = new HttpRequestMessage(HttpMethod.Get, Url);
        istek.Headers.Accept.ParseAdd("application/json");

        using var cevap = await http.SendAsync(istek, zamanAsimi.Token);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"truncgil HTTP {(int)cevap.StatusCode}");

        var govde = await cevap.Content.ReadAsStringAsync(zamanAsimi.Token);
        using var doc = JsonDocument.Parse(govde);
        var kok = doc.RootElement;

        // Has altın zorunlu; alınamıyorsa manuel kur gerekir.
        var has = SatisOku(kok, "gram-has-altin")
            ?? SatisOku(kok, "gram-altin")
            ?? throw new InvalidOperationException("truncgil: has altın fiyatı bulunamadı");

        var gramAltin = SatisOku(kok, "gram-altin");
        var usd = SatisOku(kok, "USD");
        var eur = SatisOku(kok, "EUR");
        var gbp = SatisOku(kok, "GBP");

        if (usd is null && eur is null && gbp is null)
            logger.LogWarning("truncgil: döviz kurları okunamadı, null kaydedilecek");

        return new DisKurVerisi(has, gramAltin, usd, eur, gbp, "HAREM");
    }

    /// <summary>Verilen anahtarın "Satış" alanını tr-TR sayı olarak okur; yoksa/geçersizse null.</summary>
    private static decimal? SatisOku(JsonElement kok, string anahtar)
    {
        if (!kok.TryGetProperty(anahtar, out var oge) || oge.ValueKind != JsonValueKind.Object)
            return null;
        if (!oge.TryGetProperty("Satış", out var satis) || satis.ValueKind != JsonValueKind.String)
            return null;

        // "$4.052,66" / "6.137,22" → sayı; para/işaret temizlenir, tr-TR ayraçları çözülür.
        var ham = satis.GetString()!.Replace("$", "").Replace("%", "").Trim();
        return decimal.TryParse(ham, NumberStyles.Number, Tr, out var deger) && deger > 0
            ? Math.Round(deger, 4, MidpointRounding.AwayFromZero)
            : null;
    }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KurServisi(
    KuyumcuDbContext db, IHttpClientFactory httpFactory, ILogger<KurServisi> logger,
    IHubContext<KurHub> kurHub)
{
    // Tek satır değişiklikle başka adaptöre geçilir (Nest: KUR_ADAPTORU provider'ı)
    private readonly IKurAdaptoru adaptoru = new TruncgilAdaptoru(httpFactory, logger);

    /// <summary>TS @kuyum/core ALTIN_AYARLARI ile birebir aynı değerler.</summary>
    private static readonly (string Id, string Label, int Milyem)[] AltinAyarlari =
    [
        ("K8",  "8 Ayar Altın",  333),
        ("K14", "14 Ayar Altın", 585),
        ("K18", "18 Ayar Altın", 750),
        ("K21", "21 Ayar Altın", 875),
        ("K22", "22 Ayar Altın", 916),
        ("K24", "24 Ayar (Has)", 1000),
    ];

    // ─── En güncel kur ────────────────────────────────────────────────────────

    public async Task<KurSnapshotCevap> GuncelKur(string tenantId)
    {
        var snapshot = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.Olusturuldu)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Henüz kur verisi yok. Lütfen kur güncelleyin.");
        return SnapshotZenginlestir(snapshot);
    }

    // ─── Kur geçmişi ─────────────────────────────────────────────────────────

    public async Task<List<KurSnapshotCevap>> KurGecmisi(string tenantId, KurFiltreDto filtre)
    {
        var q = db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == tenantId);

        if (filtre.Baslangic is not null) q = q.Where(s => s.Olusturuldu >= filtre.Baslangic);
        if (filtre.Bitis is not null) q = q.Where(s => s.Olusturuldu <= filtre.Bitis);
        if (!string.IsNullOrEmpty(filtre.Kaynak)) q = q.Where(s => s.Kaynak == filtre.Kaynak);

        var liste = await q.OrderByDescending(s => s.Olusturuldu)
            .Take(filtre.Limit ?? 100)
            .ToListAsync();

        return liste.Select(SnapshotZenginlestir).ToList();
    }

    // ─── Dış kaynaktan çek ve kaydet ─────────────────────────────────────────

    public async Task<KurSnapshotCevap> DisKaynakKurCek(string tenantId)
    {
        DisKurVerisi veri;
        try
        {
            veri = await adaptoru.KurCek();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dış kaynak kur çekme hatası");
            throw new ApiException(503,
                "Altın kuru şu an dış kaynaktan alınamıyor. Manuel kur girebilirsiniz.",
                "Service Unavailable");
        }

        return await SnapshotKaydet(tenantId, veri.AltinHasFiyati, veri.GramAltinFiyati,
            veri.UsdTry, veri.EurTry, veri.GbpTry, veri.Kaynak);
    }

    // ─── Manuel kur girişi ────────────────────────────────────────────────────

    public Task<KurSnapshotCevap> ManuelKurKaydet(ManuelKurDto dto, string tenantId) =>
        SnapshotKaydet(tenantId, dto.AltinHasFiyati, dto.GramAltinFiyati,
            dto.UsdTry, dto.EurTry, dto.GbpTry, "MANUAL");

    // ─── Zamanlayıcı için iç metod ────────────────────────────────────────────

    public async Task<KurSnapshotCevap?> OtomatikGuncelle(string tenantId)
    {
        try
        {
            var snapshot = await DisKaynakKurCek(tenantId);
            logger.LogInformation("[{TenantId}] Kur güncellendi: {Fiyat} TL/has gram",
                tenantId, snapshot.AltinHasFiyati);
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[{TenantId}] Otomatik kur güncellenemedi: {Mesaj}", tenantId, ex.Message);
            return null;
        }
    }

    // ─── Snapshot kayıt yardımcısı ────────────────────────────────────────────

    private async Task<KurSnapshotCevap> SnapshotKaydet(
        string tenantId, decimal altinHasFiyati, decimal? gramAltinFiyati,
        decimal? usdTry, decimal? eurTry, decimal? gbpTry, string kaynak)
    {
        var snapshot = new KurSnapshot
        {
            Id = Kimlik.Ulid(),
            TenantId = tenantId,
            AltinHasFiyati = Math.Round(altinHasFiyati, 4, MidpointRounding.AwayFromZero),
            GramAltinFiyati = gramAltinFiyati is null ? null : Math.Round(gramAltinFiyati.Value, 4, MidpointRounding.AwayFromZero),
            UsdTry = usdTry is null ? null : Math.Round(usdTry.Value, 4, MidpointRounding.AwayFromZero),
            EurTry = eurTry is null ? null : Math.Round(eurTry.Value, 4, MidpointRounding.AwayFromZero),
            GbpTry = gbpTry is null ? null : Math.Round(gbpTry.Value, 4, MidpointRounding.AwayFromZero),
            Kaynak = kaynak,
            Olusturuldu = DateTime.UtcNow,
        };
        db.KurSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var cevap = SnapshotZenginlestir(snapshot);
        // Canlı push: yalnız bu tenant'ın bağlı istemcilerine (yoklama yerine)
        try { await kurHub.Clients.Group(tenantId).SendAsync("KurGuncellendi", cevap); }
        catch (Exception ex) { logger.LogWarning(ex, "Kur push gönderilemedi (bağlı istemci yok olabilir)"); }
        return cevap;
    }

    // ─── Snapshot → zengin yanıt (ayar fiyatları dahil) ──────────────────────

    private static KurSnapshotCevap SnapshotZenginlestir(KurSnapshot s)
    {
        var hasFiyati = s.AltinHasFiyati;

        var ayarFiyatlari = AltinAyarlari.Select(ayar => new AyarFiyati(
            ayar.Id,
            ayar.Label,
            ayar.Milyem,
            // Nest: toHasGram(1g, ayar) × hasFiyatı, 2 haneye yuvarlanır
            Math.Round(ayar.Milyem / 1000m * hasFiyati, 2, MidpointRounding.AwayFromZero),
            hasFiyati)).ToList();

        return new KurSnapshotCevap(
            s.Id, s.TenantId, hasFiyati, s.GramAltinFiyati, s.UsdTry, s.EurTry, s.GbpTry, s.Kaynak, s.Olusturuldu, ayarFiyatlari);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kur")]
public sealed class KurController(KurServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kur/guncel — En güncel kur + tüm ayarların gram fiyatları (POS ekranı için tüm rollere açık)</summary>
    [HttpGet("guncel")]
    public Task<KurSnapshotCevap> GuncelKur() => servis.GuncelKur(Islem.TenantId);

    /// <summary>GET /api/v1/kur/gecmis?baslangic=..&amp;bitis=..&amp;kaynak=..&amp;limit=.. — Kur geçmişi — MUHASEBE+</summary>
    [HttpGet("gecmis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<List<KurSnapshotCevap>> KurGecmisi([FromQuery] KurFiltreDto filtre) =>
        servis.KurGecmisi(Islem.TenantId, filtre);

    /// <summary>POST /api/v1/kur/guncelle — Dış kaynaktan anında kur çek ve kaydet — MUDUR+ (200 OK)</summary>
    [HttpPost("guncelle")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<KurSnapshotCevap> DisKaynakKurCek() => servis.DisKaynakKurCek(Islem.TenantId);

    /// <summary>POST /api/v1/kur/manuel — Manuel kur girişi (internet yoksa / özel fiyat) — MUDUR+ (201)</summary>
    [HttpPost("manuel")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> ManuelKurGir([FromBody] ManuelKurDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.ManuelKurKaydet(dto, Islem.TenantId));
}

// ── Zamanlayıcı ──────────────────────────────────────────────────────────────

/// <summary>
/// Nest kur.scheduler.ts karşılığı (Task.Delay döngüsü):
///   • '0 */5 8-20 * * *' (Europe/Istanbul): her gün 08:00–20:59 arası ~5 dk'da bir
///     aktif tüm tenant'lar için dış kaynaktan kur güncelle (hafta sonu dahil).
///   • '0 0 0 * * *': her (İstanbul) günü bir kez 90 günden eski snapshot'ları temizle.
/// Dış kaynak/DB hatası uygulamayı düşürmez — loglanır ve sonraki tura geçilir.
/// </summary>
public sealed class KurZamanlayici(IServiceScopeFactory scopeFactory, ILogger<KurZamanlayici> logger)
    : BackgroundService
{
    private static readonly TimeSpan GuncellemeAraligi = TimeSpan.FromMinutes(5);
    private static readonly TimeZoneInfo Istanbul = IstanbulSaatDilimi();

    private DateOnly sonTemizlikGunu = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kur zamanlayıcı başladı (aralık: {Dakika} dk)", GuncellemeAraligi.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            var simdi = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Istanbul);
            try
            {
                // Günlük temizlik: İstanbul gününün ilk uyanışında bir kez çalışır
                if (DateOnly.FromDateTime(simdi) != sonTemizlikGunu)
                {
                    await EskiKurlariTemizle(stoppingToken);
                    sonTemizlikGunu = DateOnly.FromDateTime(simdi);
                }

                // Kur güncelleme penceresi: her gün 08:00–20:59 (İstanbul) — kuyumcu dükkân saatleri.
                // Piyasa hafta sonu kapalı olsa da vitrin en son fiyatta kalsın diye 7 gün çekilir.
                if (simdi.Hour is >= 8 and <= 20)
                {
                    await KurGuncelle(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kur zamanlayıcı tur hatası");
            }

            try { await Task.Delay(GuncellemeAraligi, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task KurGuncelle(CancellationToken iptal)
    {
        logger.LogDebug("Kur güncelleme görevi başladı");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var servis = scope.ServiceProvider.GetRequiredService<KurServisi>();

        // Aktif tüm tenant'lar için güncelle
        var aktifTenantlar = await db.Tenants.AsNoTracking()
            .Where(t => t.Aktif)
            .Select(t => t.Id)
            .ToListAsync(iptal);
        if (aktifTenantlar.Count == 0) return;

        // Nest'teki Promise.allSettled karşılığı: OtomatikGuncelle kendi hatasını yutar;
        // tek scoped DbContext eşzamanlı kullanılamayacağından sıralı çalışır.
        foreach (var tenantId in aktifTenantlar)
            await servis.OtomatikGuncelle(tenantId);
    }

    private async Task EskiKurlariTemizle(CancellationToken iptal)
    {
        var sinir = DateTime.UtcNow.AddDays(-90);
        logger.LogInformation("90 günden eski kur kayıtları temizleniyor (öncesi: {Sinir:O})", sinir);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        await db.KurSnapshots.Where(s => s.Olusturuldu < sinir).ExecuteDeleteAsync(iptal);

        logger.LogInformation("Eski kur kayıtları temizlendi");
    }

    private static TimeZoneInfo IstanbulSaatDilimi()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (Exception)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
            catch (Exception)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Europe/Istanbul", TimeSpan.FromHours(3), "Europe/Istanbul", "Europe/Istanbul");
            }
        }
    }
}
