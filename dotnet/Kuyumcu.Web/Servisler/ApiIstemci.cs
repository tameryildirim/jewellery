using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Servisler;

/// <summary>API'nin döndürdüğü Nest formatlı hata gövdesi.</summary>
public sealed class ApiHata : Exception
{
    public ApiHata(int statusCode, string mesaj) : base(mesaj) => StatusCode = statusCode;
    public int StatusCode { get; }
}

/// <summary>
/// Axios client.ts karşılığı: Bearer ekler, 401'de refresh → isteği yineler,
/// olmuyorsa oturumu kapatıp /giris'e yönlendirir.
/// </summary>
public sealed class ApiIstemci(HttpClient http, OturumServisi oturum, NavigationManager nav)
{
    private static readonly JsonSerializerOptions JsonAyar = new(JsonSerializerDefaults.Web);

    public async Task<T> GetAsync<T>(string yol) =>
        await OkuAsync<T>(await GonderAsync(() => Istek(HttpMethod.Get, yol)));

    public async Task<T> PostAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await GonderAsync(() => Istek(HttpMethod.Post, yol, govde)));

    public async Task<T> PatchAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await GonderAsync(() => Istek(HttpMethod.Patch, yol, govde)));

    public async Task<T> PutAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await GonderAsync(() => Istek(HttpMethod.Put, yol, govde)));

    public async Task<T> DeleteAsync<T>(string yol) =>
        await OkuAsync<T>(await GonderAsync(() => Istek(HttpMethod.Delete, yol)));

    public Task<JsonElement> GetAsync(string yol) => GetAsync<JsonElement>(yol);
    public Task<JsonElement> PostAsync(string yol, object? govde = null) => PostAsync<JsonElement>(yol, govde);
    public Task<JsonElement> PatchAsync(string yol, object? govde = null) => PatchAsync<JsonElement>(yol, govde);
    public Task<JsonElement> PutAsync(string yol, object? govde = null) => PutAsync<JsonElement>(yol, govde);

    // ── iç işleyiş ───────────────────────────────────────────────────────────

    private HttpRequestMessage Istek(HttpMethod metot, string yol, object? govde = null)
    {
        var istek = new HttpRequestMessage(metot, $"api/v1{yol}");
        if (!string.IsNullOrEmpty(oturum.AccessToken))
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oturum.AccessToken);
        if (govde is not null)
            istek.Content = JsonContent.Create(govde, options: JsonAyar);
        return istek;
    }

    private async Task<HttpResponseMessage> GonderAsync(Func<HttpRequestMessage> istekUret)
    {
        var yanit = await http.SendAsync(istekUret());

        if (yanit.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(oturum.RefreshToken))
        {
            if (await YenileAsync())
                yanit = await http.SendAsync(istekUret());   // taze token ile tekrar
            else
            {
                await oturum.CikisAsync();
                nav.NavigateTo("/giris");
            }
        }
        return yanit;
    }

    private async Task<bool> YenileAsync()
    {
        try
        {
            var yanit = await http.PostAsJsonAsync("api/v1/auth/yenile",
                new { refreshToken = oturum.RefreshToken }, JsonAyar);
            if (!yanit.IsSuccessStatusCode) return false;
            var veri = await yanit.Content.ReadFromJsonAsync<JsonElement>(JsonAyar);
            await oturum.TokenKaydetAsync(
                veri.GetProperty("accessToken").GetString()!,
                veri.GetProperty("refreshToken").GetString()!);
            return true;
        }
        catch { return false; }
    }

    private static async Task<T> OkuAsync<T>(HttpResponseMessage yanit)
    {
        if (yanit.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(JsonElement) || yanit.Content.Headers.ContentLength > 0)
                return (await yanit.Content.ReadFromJsonAsync<T>(JsonAyar))!;
            return default!;
        }

        var mesaj = "İstek başarısız";
        try
        {
            var hata = await yanit.Content.ReadFromJsonAsync<JsonElement>(JsonAyar);
            if (hata.TryGetProperty("message", out var m))
                mesaj = m.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", m.EnumerateArray().Select(x => x.GetString()))
                    : m.GetString() ?? mesaj;
        }
        catch { /* gövde okunamadıysa genel mesaj */ }

        throw new ApiHata((int)yanit.StatusCode, mesaj);
    }
}
