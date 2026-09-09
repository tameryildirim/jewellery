namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Giriş yapan kullanıcının erişebileceği modülleri (ekranları) tutar — menü gizleme için.
/// /yetkiler/benim'den yüklenir; SAHIP her zaman tam yetkilidir.
/// WASM'da scoped == tekil: oturum boyunca paylaşılır.
/// </summary>
public sealed class YetkiServisi(ApiIstemci api, OturumServisi oturum)
{
    private HashSet<string> moduller = [];
    private string? yuklenenKullaniciId;

    public event Action? Degisti;

    /// <summary>Verilen modüle erişim var mı? SAHIP → daima evet.</summary>
    public bool Erisebilir(string modul) =>
        oturum.Kullanici?.Rol == "SAHIP" || moduller.Contains(modul);

    /// <summary>Giriş yapan kullanıcı için yetkileri (idempotent) yükle. Kullanıcı değişince tekrar çeker.</summary>
    public async Task HazirlaAsync()
    {
        var kid = oturum.Kullanici?.Id;
        if (string.IsNullOrEmpty(kid) || kid == yuklenenKullaniciId) return;
        try
        {
            var cevap = await api.GetAsync<YetkiCevap>("/yetkiler/benim");
            moduller = [.. cevap.Moduller ?? []];
            yuklenenKullaniciId = kid;
        }
        catch { moduller = []; }
        Degisti?.Invoke();
    }

    private sealed record YetkiCevap(string[]? Moduller);
}
