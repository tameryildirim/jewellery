using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Oturumdaki kullanıcı. ⚠ Yeni alanlar SONA ve VARSAYILANLI eklenir (Aşkın K2):
/// konumsal record.ın mevcut 6 argümanlı çağrıları (onlarca bUnit testi) derlenmeye
/// devam etmelidir.
/// <para>ArayuzModu = kullanıcının KENDİ tercihi (null = kiracı varsayılanını devral);
/// EtkinArayuzModu = SUNUCUNUN çözdüğü mod. İstemci bu çözümü tekrarlamaz.</para>
/// </summary>
public sealed record OturumKullanici(
    string Id, string Ad, string Soyad, string Email, string Rol, string? SubeId,
    string? ArayuzModu = null, string EtkinArayuzModu = "PROFESYONEL");

/// <summary>
/// Token + kullanıcı durumu. Zustand auth.store karşılığı:
/// yalnızca tokenler localStorage'da kalıcıdır; profil girişte/yenilemede yüklenir.
/// </summary>
/// <remarks>
/// ⚠ PAKET P-07: kurucuda <b>HttpClient YOKTUR</b>. Web tarafındaki her istek
/// <see cref="ApiIstemci"/> dayanıklılık bandından (429 geri çekilmesi, istek
/// fırtınası kapısı, tek uçuşlu jeton yenileme) geçmek zorundadır; ikinci bir
/// HTTP yolu bandı deler (denetim.sh: APIISTEMCI_DISI_HTTPCLIENT = 0).
/// ApiIstemci bu servise BAĞLIDIR; döngüsel bağımlılık doğmasın diye istemci
/// kurucuda değil, çağrı anında kapsamdan çözülür (ArayuzModuServisi deseni).
/// </remarks>
public sealed class OturumServisi(IJSRuntime js, IServiceProvider kapsam)
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

    /// <summary>
    /// Keşif B006 — sekmeler arası yenileme kilidinin adı (tarayıcı geneli).
    /// </summary>
    internal const string YenilemeKilidiAdi = "kuyumcu-jeton-yenile";

    /// <summary>
    /// Tarayıcı genelinde (tüm sekmeler) yenileme kilidini alır. Kilit alınamazsa
    /// (JS yok, eski tarayıcı, test ortamı) null döner; çağıran kilitsiz sürer —
    /// bu, önceki davranıştan daha kötü olamaz.
    /// </summary>
    internal async Task<string?> SekmeKilidiAlAsync()
    {
        try { return await js.InvokeAsync<string?>("kuyumcuOturumKilidi.al", YenilemeKilidiAdi); }
        catch { return null; }
    }

    internal async Task SekmeKilidiBirakAsync(string? kimlik)
    {
        if (string.IsNullOrEmpty(kimlik)) return;
        try { await js.InvokeVoidAsync("kuyumcuOturumKilidi.birak", kimlik); }
        catch { /* sayfa kapanıyor olabilir; JS tarafı 15 sn sonra kendisi bırakır */ }
    }

    /// <summary>
    /// Keşif B006 — başka bir sekme jetonu döndürdüyse (localStorage'daki yenileme
    /// jetonu bellektekinden FARKLI ve dolu) o çifti benimser.
    /// <para>
    /// ⚠ GÜVENLİK (orkestratör 30.09 05:52, KRİTİK): depodaki çift YALNIZ bu sekmenin
    /// kullanıcısına VE kiracısına aitse benimsenir. Kimlik isimden değil JETONUN
    /// KENDİSİNDEN okunur: bellekteki erişim jetonunun <c>sub</c> + <c>tenantId</c>
    /// iddiaları, depodakinin iddialarıyla birebir eşleşmelidir. Ortak bilgisayarda
    /// Ali çıkıp Veli girdiyse Ali'nin açık kalan sekmesi Veli'nin çiftini BENİMSEMEZ
    /// (ekranda Ali görünürken istekler Veli adına gitmesin). Jeton okunamazsa
    /// eşleşme yok sayılır (fail-closed).
    /// </para>
    /// Aynı kullanıcıda benimsenirse Kullanici'nın rol ve şubesi de yeni jetondan
    /// yenilenir (başka sekmede rol/şube değişmiş olabilir).
    /// </summary>
    internal async Task<CiftBenimseme> BaskaSekmeninCiftiniBenimseAsync()
    {
        string? depoAccess, depoRefresh;
        try
        {
            depoAccess = await js.InvokeAsync<string?>("localStorage.getItem", AccessAnahtar);
            depoRefresh = await js.InvokeAsync<string?>("localStorage.getItem", RefreshAnahtar);
        }
        catch { return CiftBenimseme.Yok; }

        if (string.IsNullOrEmpty(depoAccess) || string.IsNullOrEmpty(depoRefresh)) return CiftBenimseme.Yok;
        if (depoRefresh == RefreshToken) return CiftBenimseme.Yok;

        var bizim = JetonKimligi.Oku(AccessToken);
        var depodaki = JetonKimligi.Oku(depoAccess);
        if (bizim is null || depodaki is null || !bizim.AyniKisi(depodaki))
            return CiftBenimseme.BaskaKullanici;

        AccessToken = depoAccess;
        RefreshToken = depoRefresh;
        if (Kullanici is not null)
            Kullanici = Kullanici with { Rol = depodaki.Rol ?? Kullanici.Rol, SubeId = depodaki.SubeId };
        Degisti?.Invoke();
        return CiftBenimseme.Benimsendi;
    }

    /// <summary>
    /// Giriş ekranının göstereceği oturum bildirimi (kod); metin sözlükten gelir
    /// (GirisSozlugu). Okumak silmez: giriş ekranı birden çok kez çizilebilir
    /// (kabuk ve istemci ayrı ayrı /giris'e yönlendirir). Yeni oturum açılınca
    /// (<see cref="TokenKaydetAsync"/>) kalkar.
    /// </summary>
    public string? GirisBildirimi { get; private set; }

    public const string BildirimBaskaKullaniciKodu = "baska-kullanici";

    /// <summary>
    /// Bu tarayıcıda BAŞKA bir kullanıcı oturum açtı: bu sekmenin oturumu YALNIZ
    /// BELLEKTE temizlenir. ⚠ localStorage'a DOKUNULMAZ (o çift öteki kullanıcının
    /// açık oturumudur; silmek onu da düşürür) ve sunucuya HİÇBİR istek gitmez.
    /// </summary>
    internal void BaskaKullaniciYuzundenBirak()
    {
        AccessToken = null;
        RefreshToken = null;
        Kullanici = null;
        GirisBildirimi = BildirimBaskaKullaniciKodu;
        Degisti?.Invoke();
    }

    public async Task TokenKaydetAsync(string access, string refresh)
    {
        AccessToken = access;
        RefreshToken = refresh;
        GirisBildirimi = null;
        // Yenilemede (aynı kişi) rol/şube başka yerde değişmiş olabilir: ekrandaki profil
        // yeni jetondan güncellenir. Kişi farklıysa (giriş) profil zaten ayrıca atanır.
        var yeni = JetonKimligi.Oku(access);
        if (Kullanici is not null && yeni is not null && yeni.Sub == Kullanici.Id)
            Kullanici = Kullanici with { Rol = yeni.Rol ?? Kullanici.Rol, SubeId = yeni.SubeId };
        await js.InvokeVoidAsync("localStorage.setItem", AccessAnahtar, access);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshAnahtar, refresh);
        Degisti?.Invoke();
    }

    public void KullaniciAta(OturumKullanici? kullanici)
    {
        Kullanici = kullanici;
        Degisti?.Invoke();
    }

    /// <summary>
    /// Oturumu kapatır.
    ///
    /// ⚠ GÜVENLİK: tokeni yalnız tarayıcıdan silmek YETMEZ. Refresh token'ın
    /// sunucudaki karşılığı iptal edilmezse, çıkıştan önce kopyalanmış bir
    /// token çıkıştan SONRA da yeni access token üretmeye devam eder —
    /// "çıkış yaptım" diyen kullanıcının oturumu fiilen açık kalır.
    /// Bu yüzden önce POST /auth/cikis ile sunucudaki kayıt iptal edilir.
    ///
    /// Ağ hatası yerel temizliği engellemez: sunucuya ulaşılamıyorsa bile
    /// kullanıcı bu tarayıcıda çıkmış olmalıdır.
    /// </summary>
    public async Task CikisAsync()
    {
        var refresh = RefreshToken;
        if (!string.IsNullOrEmpty(refresh)
            && kapsam.GetService(typeof(ApiIstemci)) is ApiIstemci api)
        {
            // İptal başarısız olsa bile (ağ yok / zaman aşımı) yerel temizlik yapılır;
            // ApiIstemci bu yolda fırlatmaz, false döner.
            await api.JetonIptalEtAsync(refresh);
        }

        AccessToken = null;
        RefreshToken = null;
        Kullanici = null;
        await js.InvokeVoidAsync("localStorage.removeItem", AccessAnahtar);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshAnahtar);
        Degisti?.Invoke();
    }
}

/// <summary>Keşif B006 — depodaki çifti benimseme sonucu.</summary>
internal enum CiftBenimseme
{
    /// <summary>Depoda benimsenecek farklı çift yok; sunucuda yenilenir.</summary>
    Yok,
    /// <summary>Aynı kullanıcı + kiracının başka sekmede döndürdüğü çift benimsendi.</summary>
    Benimsendi,
    /// <summary>Depodaki çift BAŞKA kullanıcıya/kiracıya ait (ya da okunamadı): istek gönderilmez.</summary>
    BaskaKullanici,
}

/// <summary>
/// Erişim jetonunun (JWT) yük kısmından kimlik iddiaları. ⚠ İMZA DOĞRULAMASI DEĞİLDİR —
/// yalnız "bu iki jeton aynı kişinin mi" karşılaştırması içindir; yetki kararını
/// her istekte sunucu imzayla verir. İddia adları TokenServisi.AccessUret ile aynı.
/// </summary>
internal sealed record JetonKimligi(string Sub, string TenantId, string? Rol, string? SubeId)
{
    public bool AyniKisi(JetonKimligi o) =>
        string.Equals(Sub, o.Sub, StringComparison.Ordinal)
        && string.Equals(TenantId, o.TenantId, StringComparison.Ordinal);

    public static JetonKimligi? Oku(string? jwt)
    {
        if (string.IsNullOrEmpty(jwt)) return null;
        var parcalar = jwt.Split('.');
        if (parcalar.Length != 3) return null;
        try
        {
            var b64 = parcalar[1].Replace('-', '+').Replace('_', '/');
            b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            using var doc = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(b64));
            var kok = doc.RootElement;
            string? Al(string ad) =>
                kok.TryGetProperty(ad, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                    ? v.GetString() : null;
            var sub = Al("sub");
            var tenant = Al("tenantId");
            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(tenant)) return null;
            var sube = Al("subeId");
            return new JetonKimligi(sub, tenant, Al("rol"), string.IsNullOrEmpty(sube) ? null : sube);
        }
        catch { return null; }
    }
}
