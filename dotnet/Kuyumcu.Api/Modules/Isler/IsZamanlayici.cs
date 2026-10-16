using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler;

/// <summary>
/// ZAMANLANMIŞ İŞ TURU — yalnız UYANDIRIR ve KİRACI GEZER. İş MANTIĞI burada
/// YOKTUR (K0-5): mantık IsCalistirici'dedir ve birim testle kapsanır.
///
/// ⚠ TEST BOŞLUĞU DÜRÜSTÇE: HttpOrtami.cs tüm IHostedService'leri kaldırır,
///   yani BU DÖNGÜ HTTP testinde koşmaz. Döngünün kendisi (uyanma aralığı,
///   kiracı gezme, ikinci turun atlaması) CANLI doğrulamayla ölçülür —
///   "birim testle değil canlıyla doğrulandı" (Anayasa md.1).
///
/// ⚠ KİRACI İZOLASYONU: her kiracı için AYRI DI kapsamı açılır
///   (SistemKapsami.Kiraci) — tek DbContext'i kiracılar arasında paylaşmak
///   izleyici (change tracker) üzerinden sızıntı yolu açardı.
/// </summary>
public sealed class IsZamanlayici(
    SistemKapsami kapsam,
    ILogger<IsZamanlayici> gunluk,
    IConfiguration yapilandirma)
    : BackgroundService
{
    private readonly bool etkin = yapilandirma.GetValue("Isler:Etkin", true);

    private readonly TimeSpan turAraligi =
        TimeSpan.FromMinutes(Math.Clamp(yapilandirma.GetValue("Isler:TurDakika", 15), 1, 1440));

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        if (!etkin)
        {
            gunluk.LogInformation("Zamanlanmış iş turu KAPALI (Isler:Etkin=false)");
            return;
        }

        gunluk.LogInformation("Zamanlanmış iş turu başladı (aralık: {Dakika} dk)", turAraligi.TotalMinutes);

        while (!durdur.IsCancellationRequested)
        {
            try { await Tur(durdur); }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex) { gunluk.LogError(ex, "Zamanlanmış iş turu hatası"); }

            try { await Task.Delay(turAraligi, durdur); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task Tur(CancellationToken iptal)
    {
        // SistemKapsami "izin verilen kullanım" listesi madde 5: aktif kiracı listesi.
        List<string> kiracilar;
        await using (var sistem = kapsam.Sistem())
        {
            var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            kiracilar = await db.Tenants.AsNoTracking()
                .Where(t => t.Aktif).Select(t => t.Id).ToListAsync(iptal);
        }
        if (kiracilar.Count == 0) return;

        var an = DateTime.UtcNow;
        foreach (var kiraciId in kiracilar)
        {
            iptal.ThrowIfCancellationRequested();
            // Madde 6: kiracı başına iş koşumu — AYRI kapsam, kiracıya SABİT bağlam.
            await using var kiraciKapsami = kapsam.Kiraci(kiraciId);
            var calistirici = kiraciKapsami.ServiceProvider.GetRequiredService<IsCalistirici>();
            try
            {
                var sonuclar = await calistirici.TumEtkinIsleriCalistir(kiraciId, an, iptal);
                foreach (var s in sonuclar.Where(x => x.Atlandi))
                    gunluk.LogDebug("İş bu anahtarda zaten çalışmış: {Kod}/{Anahtar}", s.IsKodu, s.CalismaAnahtari);
            }
            catch (OperationCanceledException) when (iptal.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // FAIL-SOFT TUR: bir kiracının hatası diğerlerini düşürmez.
                gunluk.LogError(ex, "Kiracı iş turu hatası");
            }
        }
    }
}
