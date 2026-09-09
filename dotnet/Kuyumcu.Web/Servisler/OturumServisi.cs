using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

public sealed record OturumKullanici(
    string Id, string Ad, string Soyad, string Email, string Rol, string? SubeId);

/// <summary>
/// Token + kullanıcı durumu. Zustand auth.store karşılığı:
/// yalnızca tokenler localStorage'da kalıcıdır; profil girişte/yenilemede yüklenir.
/// </summary>
public sealed class OturumServisi(IJSRuntime js)
{
    private const string AccessAnahtar = "kuyumcu-access";
    private const string RefreshAnahtar = "kuyumcu-refresh";

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public OturumKullanici? Kullanici { get; private set; }
    public bool GirisYapildi => !string.IsNullOrEmpty(AccessToken);

    public event Action? Degisti;

    public async Task YukleAsync()
    {
        AccessToken = await js.InvokeAsync<string?>("localStorage.getItem", AccessAnahtar);
        RefreshToken = await js.InvokeAsync<string?>("localStorage.getItem", RefreshAnahtar);
    }

    public async Task TokenKaydetAsync(string access, string refresh)
    {
        AccessToken = access;
        RefreshToken = refresh;
        await js.InvokeVoidAsync("localStorage.setItem", AccessAnahtar, access);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshAnahtar, refresh);
        Degisti?.Invoke();
    }

    public void KullaniciAta(OturumKullanici? kullanici)
    {
        Kullanici = kullanici;
        Degisti?.Invoke();
    }

    public async Task CikisAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        Kullanici = null;
        await js.InvokeVoidAsync("localStorage.removeItem", AccessAnahtar);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshAnahtar);
        Degisti?.Invoke();
    }
}
