using Kuyumcu.Core.I18n;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>Dil durumu — TR/EN sözlükler Kuyumcu.Core'dan gelir, seçim localStorage'da.</summary>
public sealed class DilServisi(IJSRuntime js)
{
    private const string Anahtar = "kuyumcu-dil";

    public string Dil { get; private set; } = "tr";
    public Sozluk T => Sozlukler.Getir(Dil);
    public string Locale => Dil == "tr" ? "tr-TR" : "en-US";

    public event Action? Degisti;

    public async Task YukleAsync()
    {
        var kayitli = await js.InvokeAsync<string?>("localStorage.getItem", Anahtar);
        if (kayitli is "tr" or "en") Dil = kayitli;
    }

    public async Task DegistirAsync(string dil)
    {
        Dil = dil == "en" ? "en" : "tr";
        await js.InvokeVoidAsync("localStorage.setItem", Anahtar, Dil);
        await js.InvokeVoidAsync("eval", $"document.documentElement.lang = '{Dil}'");
        Degisti?.Invoke();
    }
}
