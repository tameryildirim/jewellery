using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Canlı kur durumu. 60 sn'lik yoklama yerine SignalR push:
///   • Açılışta bir kez REST'ten (/kur/guncel) son kuru alır (anında göster).
///   • /hubs/kur'a bağlanıp "KurGuncellendi" olaylarını dinler; yeni snapshot
///     yazıldığında sunucu push eder, ekranlar anında güncellenir.
/// WASM'da scoped == tekil: tüm uygulama için TEK HubConnection paylaşılır.
/// Bileşenler <see cref="Degisti"/> olayına abone olur, <see cref="Son"/>'u okur.
/// </summary>
public sealed class CanliKurServisi(ApiIstemci api, OturumServisi oturum, NavigationManager nav) : IAsyncDisposable
{
    /// <summary>Çubuk/işlem ekranı için gereken kur alanları (JSON camelCase eşleşir).</summary>
    public sealed record KurAnlik(
        string Id, decimal AltinHasFiyati, decimal? GramAltinFiyati,
        decimal? UsdTry, decimal? EurTry, decimal? GbpTry);

    private HubConnection? hub;
    private Task? baslatmaGorevi;

    /// <summary>En son bilinen kur (push veya ilk REST çağrısından).</summary>
    public KurAnlik? Son { get; private set; }

    /// <summary>Canlı bağlantı kurulu mu (WebSocket açık)?</summary>
    public bool Bagli => hub?.State == HubConnectionState.Connected;

    public event Action? Degisti;

    /// <summary>Tekil başlatma — birden çok bileşen çağırsa da bir kez çalışır.</summary>
    public Task BaslatAsync() => baslatmaGorevi ??= BaslatIcAsync();

    private async Task BaslatIcAsync()
    {
        // 1) Anında göstermek için son kuru REST'ten al (push beklemeden)
        await TazeleAsync();

        // 2) Canlı push kanalına bağlan
        hub = new HubConnectionBuilder()
            .WithUrl(nav.ToAbsoluteUri("/hubs/kur"),
                o => o.AccessTokenProvider = () => Task.FromResult(oturum.AccessToken))
            .WithAutomaticReconnect()
            .Build();

        hub.On<KurAnlik>("KurGuncellendi", Uygula);

        // Yeniden bağlanınca aradaki kaçırılan güncellemeyi REST ile telafi et
        hub.Reconnected += async _ => await TazeleAsync();

        try { await hub.StartAsync(); }
        catch { /* bağlanamadıysa REST'ten alınan Son gösterilir; reconnect denemeye devam eder */ }
    }

    /// <summary>Son kuru REST'ten çek (ilk yükleme + reconnect telafisi + elle çekim sonrası).</summary>
    public async Task TazeleAsync()
    {
        try { Uygula(await api.GetAsync<KurAnlik>("/kur/guncel")); }
        catch { /* kur yoksa Son null kalır, çubuk gizli */ }
    }

    /// <summary>Yeni kuru uygula ve aboneleri bilgilendir (push, ilk REST veya elle çekim).</summary>
    public void Uygula(KurAnlik kur)
    {
        Son = kur;
        Degisti?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (hub is not null) await hub.DisposeAsync();
    }
}
