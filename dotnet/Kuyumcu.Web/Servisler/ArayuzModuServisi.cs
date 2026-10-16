namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Etkin arayüz modunun istemci tarafındaki okunma noktası (paket UX-3 Faz A).
///
/// <para>
/// ⚠ <b>ÇÖZÜM BURADA YAPILMAZ.</b> "kullanıcı tercihi ?? kiracı varsayılanı"
/// kuralı YALNIZ sunucudadır (<c>ArayuzModu.Coz</c>) ve değer /auth/ben
/// yanıtındaki <c>etkinArayuzModu</c> alanıyla gelir (Bertan KOŞUL 6).
/// Bu sınıf o değeri okur; kuralı tekrarlamaz.
/// </para>
/// <para>
/// ⚠ <b>localStorage'a YAZILMAZ.</b> DilServisi/TemaServisi deseni buraya
/// birebir kopyalanmaz: dil ve tema CİHAZ tercihidir, mod HESAP tercihidir.
/// Aynı tezgâhtar başka tezgâhta oturum açınca aynı modu görmelidir.
/// </para>
/// <para>
/// ⚠ Kurucusunda <b>HttpClient/ApiIstemci YOKTUR</b> (Aşkın K2): POS ve
/// yerleşim çizen onlarca bUnit sınıfı bu servisi tek satırla kaydedebilsin,
/// sahte API kurmak zorunda kalmasın diye. PATCH gerektiğinde ApiIstemci
/// kapsamdan O AN çözülür.
/// </para>
/// </summary>
public sealed class ArayuzModuServisi(OturumServisi oturum, IServiceProvider kapsam)
{
    public const string Basit = "BASIT";
    public const string Profesyonel = "PROFESYONEL";

    public event Action? Degisti;

    /// <summary>Sunucunun çözdüğü etkin mod. Profil henüz yüklenmediyse PROFESYONEL (bugünkü ekran).</summary>
    public string EtkinMod => oturum.Kullanici?.EtkinArayuzModu ?? Profesyonel;

    /// <summary>Kullanıcının KENDİ tercihi; null = kiracı varsayılanını devral.</summary>
    public string? KullaniciModu => oturum.Kullanici?.ArayuzModu;

    public bool BasitMi => EtkinMod == Basit;
    public bool ProfesyonelMi => EtkinMod != Basit;

    // PAKET S1-g: `MenudeGorunur(yol)` KALDIRILDI. Kurucunun tuvali
    // (Panel.dc.html basit yüz, Tezgah.dc.html) basit modda da 6 katlanır grup ve
    // YETKİLİ her ekranı gösterir ("menüde HİÇBİR ekran kaybolmadı" — canvas.json
    // n2). Menü ve menü araması artık YALNIZ yetkiyle süzülür (M-UX3-06: mod
    // yetkiyi gevşetmez — yetkisiz ekran basit modda da görünmez).

    /// <summary>
    /// Kendi modunu değiştirir: PATCH /kullanicilar/ben → sunucu otoritedir.
    /// <paramref name="mod"/> null ise kiracı varsayılanına DEVREDİLİR
    /// (gövdeye açık null konur — "alan yok" ile karıştırılmaz).
    /// </summary>
    public async Task DegistirAsync(string? mod)
    {
        if (mod is not null && mod != Basit && mod != Profesyonel) return;

        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var cevap = await api.PatchAsync<System.Text.Json.JsonElement>(
            "/kullanicilar/ben", new { arayuzModu = mod });

        // Etkin modu SUNUCU söyler. PATCH yanıtı kullanıcı satırıdır; etkin mod
        // alanı orada yoktur → kullanıcı tercihi doluysa etkin mod odur, null ise
        // kiracı varsayılanı /auth/ben'den gelir; onu tazelemek için yeniden okunur.
        var yeniTercih = cevap.TryGetProperty("arayuzModu", out var m) && m.ValueKind
            is System.Text.Json.JsonValueKind.String ? m.GetString() : null;

        var etkin = yeniTercih;
        if (etkin is null)
        {
            var ben = await api.GetAsync("/auth/ben");
            etkin = ben.TryGetProperty("etkinArayuzModu", out var e) ? e.GetString() : null;
        }

        Uygula(yeniTercih, etkin ?? Profesyonel);
    }

    /// <summary>
    /// PAKET S1-f — KENDİ modunu DAR uçla yazar: PATCH /kullanicilar/ben/arayuz-modu.
    /// <para>Neden ayrı uç: /kullanicilar/ben sınıf düzeyinde "kullanici" modül
    /// yetkisi ister; varsayılan matriste TEZGAHTAR ve MUHASEBE'de bu modül yok
    /// → 403. Görünüm kartı "her kullanıcı" içindir (tuval n4).</para>
    /// <para>Etkin mod SUNUCUNUN yanıtından okunur (çözüm istemcide yapılmaz).
    /// <paramref name="mod"/> null ise kiracı varsayılanı devralınır.</para>
    /// </summary>
    public async Task KendiModunuYazAsync(string? mod)
    {
        if (mod is not null && mod != Basit && mod != Profesyonel) return;

        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var cevap = await api.PatchAsync<System.Text.Json.JsonElement>(
            "/kullanicilar/ben/arayuz-modu", new { arayuzModu = mod });

        var tercih = cevap.TryGetProperty("arayuzModu", out var t)
            && t.ValueKind is System.Text.Json.JsonValueKind.String ? t.GetString() : null;
        var etkin = cevap.TryGetProperty("etkinArayuzModu", out var e)
            && e.ValueKind is System.Text.Json.JsonValueKind.String ? e.GetString() : null;
        Uygula(tercih, etkin ?? Profesyonel);
    }

    /// <summary>
    /// PAKET S1-f — SAHİP: dükkânın basit/profesyonel VARSAYILANINI yazar
    /// (PATCH /admin/tenant — mevcut uç, [RolGerekli("SAHIP")] sınıf düzeyinde).
    /// Dönen değer SUNUCUNUN yazdığıdır; ekran bantta onu söyler (UX-S27).
    /// Kendi tercihi olmayan kullanıcının etkin modu değişebileceği için
    /// /auth/ben'den tazelenir — çözüm yine sunucudadır.
    /// </summary>
    public async Task<string?> DukkanVarsayilaniYapAsync(string mod)
    {
        if (mod != Basit && mod != Profesyonel) return null;

        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var cevap = await api.PatchAsync<System.Text.Json.JsonElement>(
            "/admin/tenant", new { varsayilanArayuzModu = mod });
        var yazilan = cevap.TryGetProperty("varsayilanArayuzModu", out var v)
            && v.ValueKind is System.Text.Json.JsonValueKind.String ? v.GetString() : null;

        if (KullaniciModu is null)
        {
            var ben = await api.GetAsync("/auth/ben");
            var etkin = ben.TryGetProperty("etkinArayuzModu", out var e) ? e.GetString() : null;
            Uygula(null, etkin ?? Profesyonel);
        }
        return yazilan;
    }

    /// <summary>
    /// Sunucudan gelen değerleri oturuma yazar ve dinleyicileri uyandırır.
    /// AnaYerlesim profil yüklerken de bunu çağırır.
    /// </summary>
    public void Uygula(string? kullaniciModu, string etkinMod)
    {
        if (oturum.Kullanici is null) return;
        oturum.KullaniciAta(oturum.Kullanici with
        {
            ArayuzModu = kullaniciModu,
            EtkinArayuzModu = etkinMod,
        });
        Degisti?.Invoke();
    }
}
