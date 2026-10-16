using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ YAZICI — PAKET P-28. Bellekteki birikimi veritabanına taşır.
//
// ⚠ KİRACI İZOLASYONU: her kiracı için AYRI DI kapsamı (SistemKapsami.Kiraci)
//   açılır — IsZamanlayici'nin AYNI kalıbı, yeni desen açılmadı. Tek DbContext
//   kiracılar arasında paylaşılsaydı izleyici üzerinden sızıntı yolu doğardı.
//
// ⚠ BİLİNMEYEN KİRACI YAZILMAZ: kiracı tablosunda karşılığı olmayan etiket
//   ATILIR (silinen kiracının artık penceresi). FK yoktur (P-34), bu yüzden
//   kontrol burada elle yapılır.
//
// ⚠ BAŞARISIZLIK GİZLENMEZ (md.1): yazılamayan birikim toplayıcıya GERİ
//   KONUR ve hata günlüğe düşer; sessizce yutulmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Birikimi veritabanına yazan servis (hem zamanlayıcı hem test çağırır).</summary>
public sealed class TelemetriYazici(
    TelemetriToplayici toplayici,
    SistemKapsami kapsam,
    ILogger<TelemetriYazici> gunluk)
{
    /// <summary>
    /// Bellekteki tüm pencereleri yazar. Dönen değer YAZILAN pencere sayısıdır.
    /// </summary>
    public async Task<int> Bosalt(CancellationToken iptal = default)
    {
        var birikim = toplayici.BosaltVeAl();
        if (birikim.Count == 0) return 0;

        HashSet<string> gecerliKiracilar;
        try
        {
            await using var sistem = kapsam.Sistem();
            var sdb = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            var etiketler = birikim.Select(b => b.TenantId).Distinct().ToList();
            gecerliKiracilar = [.. await sdb.Tenants.AsNoTracking()
                .Where(t => etiketler.Contains(t.Id)).Select(t => t.Id).ToListAsync(iptal)];
        }
        catch (Exception ex)
        {
            toplayici.GeriKoy(birikim);
            gunluk.LogError(ex, "Telemetri yazımı: kiracı listesi okunamadı, birikim geri kondu");
            return 0;
        }

        var yazilan = 0;
        foreach (var grup in birikim.GroupBy(b => b.TenantId))
        {
            if (!gecerliKiracilar.Contains(grup.Key)) continue;   // artık pencere
            var pencereler = grup.ToList();
            try
            {
                await using var kiraciKapsami = kapsam.Kiraci(grup.Key);
                var db = kiraciKapsami.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
                yazilan += await KiraciYaz(db, grup.Key, pencereler, iptal);
            }
            catch (Exception ex)
            {
                // FAIL-SOFT: bir kiracının hatası diğerlerini düşürmez, ama
                // O KİRACININ birikimi kaybolmaz — geri konur.
                toplayici.GeriKoy(pencereler);
                gunluk.LogError(ex, "Telemetri yazımı bir kiracıda başarısız, birikim geri kondu");
            }
        }
        return yazilan;
    }

    /// <summary>
    /// Tek kiracının pencerelerini ÜSTÜNE EKLEYEREK yazar (upsert).
    /// Aynı saat penceresi ikinci turda gelirse sayaçlar TOPLANIR — üzerine
    /// yazmak, turlar arası isteklerin sessizce kaybolması demekti.
    /// </summary>
    private static async Task<int> KiraciYaz(
        KuyumcuDbContext db, string tenantId,
        List<TelemetriBirikimi> pencereler, CancellationToken iptal)
    {
        var saatler = pencereler.Select(p => p.Saat).Distinct().ToList();
        var mevcut = await db.Set<TelemetriOlcumu>()
            .Where(o => o.TenantId == tenantId && saatler.Contains(o.OlcumSaati))
            .ToListAsync(iptal);

        var now = DateTime.UtcNow;
        foreach (var p in pencereler)
        {
            var satir = mevcut.FirstOrDefault(o => o.OlcumSaati == p.Saat);
            if (satir is null)
            {
                satir = new TelemetriOlcumu
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    OlcumSaati = p.Saat,
                    Olusturuldu = now,
                };
                db.Set<TelemetriOlcumu>().Add(satir);
                mevcut.Add(satir);
            }
            satir.IstekSayisi += p.IstekSayisi;
            satir.HataSayisi += p.HataSayisi;
            satir.IslemMs += p.IslemMs;
            satir.YanitBayt += p.YanitBayt;
            satir.Guncellendi = now;
        }

        await db.SaveChangesAsync(iptal);
        return pencereler.Count;
    }
}

/// <summary>
/// Yazıcıyı periyodik uyandırır. İŞ MANTIĞI YOKTUR (IsZamanlayici kalıbı):
/// mantık <see cref="TelemetriYazici"/>'dadır ve testle kapsanır.
/// <para>⚠ HttpOrtami tüm IHostedService'leri kaldırır: bu döngü HTTP testinde
/// KOŞMAZ. Döngü CANLI doğrulamayla ölçülür (Anayasa md.1 — dürüst sınır).</para>
/// </summary>
public sealed class TelemetriZamanlayici(
    IServiceScopeFactory fabrika,
    ILogger<TelemetriZamanlayici> gunluk,
    IConfiguration yapilandirma)
    : BackgroundService
{
    private readonly bool etkin = yapilandirma.GetValue("Telemetri:Etkin", true);

    private readonly TimeSpan aralik = TimeSpan.FromSeconds(
        Math.Clamp(yapilandirma.GetValue("Telemetri:YazmaSaniye", 60), 5, 3600));

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        if (!etkin)
        {
            gunluk.LogInformation("Telemetri yazma turu KAPALI (Telemetri:Etkin=false)");
            return;
        }
        gunluk.LogInformation("Telemetri yazma turu başladı (aralık: {Saniye} sn)", aralik.TotalSeconds);

        while (!durdur.IsCancellationRequested)
        {
            try
            {
                await using var kapsam = fabrika.CreateAsyncScope();
                await kapsam.ServiceProvider.GetRequiredService<TelemetriYazici>().Bosalt(durdur);
            }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex) { gunluk.LogError(ex, "Telemetri yazma turu hatası"); }

            try { await Task.Delay(aralik, durdur); }
            catch (OperationCanceledException) { break; }
        }
    }
}
