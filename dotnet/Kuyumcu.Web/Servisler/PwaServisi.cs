using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET-44 / PWA-1 (20.10) — js/pwa.js köprüsü (TemaServisi deseni). JS yoksa (bUnit, iOS özel sekme, eski
/// tarayıcı) servis sessizce "özellik yok" durumunda kalır; hiçbir ekran bu yüzden kırılmaz.
/// </summary>
public sealed class PwaServisi(IJSRuntime js) : IDisposable
{
    private DotNetObjectReference<PwaServisi>? ref_;

    public bool GuncellemeVar { get; private set; }
    public bool KurulabilirMi { get; private set; }
    public bool Cevrimdisi { get; private set; }
    /// <summary>Tarayıcı service worker destekliyor ve kayıt denendi.</summary>
    public bool Destekli { get; private set; }
    public event Action? Degisti;

    public async Task BaslatAsync()
    {
        try
        {
            ref_ ??= DotNetObjectReference.Create(this);
            Destekli = await js.InvokeAsync<bool>("kuyumcuPwa.baslat", ref_);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException) { Destekli = false; }
    }

    [JSInvokable] public void GuncellemeHazir() { GuncellemeVar = true; Degisti?.Invoke(); }
    [JSInvokable] public void KurulumMumkun(bool mumkun) { KurulabilirMi = mumkun; Degisti?.Invoke(); }
    [JSInvokable] public void AgDurumu(bool cevrimici) { Cevrimdisi = !cevrimici; Degisti?.Invoke(); }

    public async Task<bool> KurAsync()
    {
        try { var ok = await js.InvokeAsync<bool>("kuyumcuPwa.kur"); if (ok) { KurulabilirMi = false; Degisti?.Invoke(); } return ok; }
        catch (Exception ex) when (ex is JSException or InvalidOperationException) { return false; }
    }

    public async Task YenileAsync()
    {
        try { await js.InvokeAsync<bool>("kuyumcuPwa.yenile"); }
        catch (Exception ex) when (ex is JSException or InvalidOperationException) { }
    }

    public void Dispose() => ref_?.Dispose();
}
