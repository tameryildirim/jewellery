using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Tema durumu — "sistem" | "aydinlik" | "karanlik".
/// <para>
/// Tercih yalnız istemcide (localStorage 'kuyumcu-tema') tutulur; UX-3 ile
/// kullanıcı kaydına inecektir. Sunucuya gitmediği için burada regex/kapı yok,
/// ama beyaz liste HEM burada HEM js/tema.js içinde uygulanır (iki taraf da
/// güvenmez — Mehmet K1).
/// </para>
/// <para>
/// ⚠ DilServisi'nin eski <c>eval</c> deseni KOPYALANMAZ: CSP
/// <c>script-src 'self' 'wasm-unsafe-eval'</c> JS eval()'ına izin vermez.
/// Çağrı adlandırılmış fonksiyonadır: <c>kuyumcuTema.uygula(mod)</c>.
/// </para>
/// </summary>
public sealed class TemaServisi(IJSRuntime js)
{
    public const string Sistem = "sistem";
    public const string Aydinlik = "aydinlik";
    public const string Karanlik = "karanlik";

    /// <summary>Ekranda gösterilecek sıra — TemaSecici bu listeyi çizer.</summary>
    public static readonly string[] Secenekler = [Sistem, Aydinlik, Karanlik];

    private static readonly string[] Yazilabilir = [Aydinlik, Karanlik];

    public string Mod { get; private set; } = Sistem;

    public event Action? Degisti;

    /// <summary>Açılışta saklı tercihi okur (js/tema.js zaten uygulamıştır).</summary>
    public async Task YukleAsync()
    {
        try
        {
            Mod = Normalle(await js.InvokeAsync<string?>("kuyumcuTema.oku"));
        }
        catch (JSException)
        {
            // Betik yüklenmemişse (ör. ön-işleme) varsayılan "sistem" kalır.
            Mod = Sistem;
        }
    }

    /// <summary>
    /// Temayı değiştirir. Beyaz liste dışı her değer "sistem"e düşer ve
    /// JS tarafı özniteliği KALDIRIR (boş dizge yazmaz).
    /// </summary>
    public async Task DegistirAsync(string mod)
    {
        Mod = Normalle(mod);
        await js.InvokeVoidAsync("kuyumcuTema.uygula", Mod);
        Degisti?.Invoke();
    }

    private static string Normalle(string? mod) =>
        Array.IndexOf(Yazilabilir, mod) >= 0 ? mod! : Sistem;
}
