using Kuyumcu.Core.I18n;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>Dil durumu — TR/EN sözlükler Kuyumcu.Core'dan gelir, seçim localStorage'da.</summary>
public sealed class DilServisi(IJSRuntime js)
{
    private const string Anahtar = "kuyumcu-dil";

    public string Dil { get; private set; } = "tr";
    /// <summary>
    /// Etkin sözlük. PAKET KAT-1 (K8): basit mod bağlıysa ve etkin mod BASİT ise
    /// D10 katmanı (<see cref="BasitSozluk"/>) döner — PRO sözlüğün kopyası, yalnız
    /// jargon anahtarları sade karşılıklarıyla. Bağ kurulmamışsa (bUnit, ön-işleme)
    /// davranış BİREBİR eskisidir.
    /// </summary>
    public Sozluk T => BasitMi ? BasitSozluk.Getir(Dil) : Sozlukler.Getir(Dil);

    // ── PAKET KAT-1 (K8) — basit mod sözlük katmanı ──────────────────────────
    private Func<bool>? basitKaynak;
    private bool sonBasit;

    /// <summary>Etkin arayüz modu BASİT mi (bağ yoksa her zaman false).</summary>
    public bool BasitMi => basitKaynak?.Invoke() == true;

    /// <summary>
    /// Mod kaynağını bağlar (Program.cs: <c>() =&gt; ArayuzModuServisi.BasitMi</c>).
    /// DilServisi ArayuzModuServisi'ne DI ile bağlanmaz: o servis OturumServisi'ne
    /// bağlıdır ve onlarca bUnit sınıfı DilServisi'ni tek başına kurar.
    /// </summary>
    public void BasitModuBagla(Func<bool> kaynak)
    {
        basitKaynak = kaynak;
        sonBasit = BasitMi;
    }

    /// <summary>
    /// Mod değiştiyse <see cref="Degisti"/> olayını tetikler — dil değişimine
    /// abone her ekran yeni metinlerle yeniden çizilir. Mod aynıysa SESSİZDİR
    /// (oturum tazelemesi ekranları boşuna yenilemesin).
    /// </summary>
    public void ModuDenetle()
    {
        var b = BasitMi;
        if (b == sonBasit) return;
        sonBasit = b;
        Degisti?.Invoke();
    }
    public string Locale => Dil == "tr" ? "tr-TR" : "en-US";

    public event Action? Degisti;

    public async Task YukleAsync()
    {
        var kayitli = await js.InvokeAsync<string?>("localStorage.getItem", Anahtar);
        if (kayitli is "tr" or "en") Dil = kayitli;
        if (Bicim.DilYayilimi) Bicim.Dil = Dil;
    }

    public async Task DegistirAsync(string dil)
    {
        Dil = dil == "en" ? "en" : "tr";
        if (Bicim.DilYayilimi) Bicim.Dil = Dil;
        await js.InvokeVoidAsync("localStorage.setItem", Anahtar, Dil);
        // ⚠ Eskiden burada `js.InvokeVoidAsync("eval", …)` vardı. CSP
        // `script-src 'self' 'wasm-unsafe-eval'` JS eval()'ına İZİN VERMEZ
        // ('wasm-unsafe-eval' yalnız WebAssembly derlemesini kapsar), bu
        // yüzden <html lang> canlıda hiç güncellenmiyordu. Adlandırılmış
        // fonksiyon: wwwroot/js/dil.js (Mehmet K1).
        await js.InvokeVoidAsync("kuyumcuDil.uygula", Dil);
        Degisti?.Invoke();
    }
}
