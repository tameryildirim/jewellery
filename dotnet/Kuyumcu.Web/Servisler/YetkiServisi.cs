namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Giriş yapan kullanıcının erişebileceği modülleri (ekranları) tutar — menü gizleme için.
/// /yetkiler/benim'den yüklenir; SAHIP her zaman tam yetkilidir.
/// WASM'da scoped == tekil: oturum boyunca paylaşılır.
///
/// ═══ PAKET P-37 — SESSİZ YUTMA KALDIRILDI (Anayasa md.1) ═══════════════════
/// ÖNCE (bu dosyanın 30. satırı):  <c>catch { moduller = []; }</c>
/// Ölçülen sonuç (canlı, 21 Eylül): yetki isteği 429/500 alınca modül listesi
/// BOŞALIYOR → menü 36 bağlantıdan 1'e (yalnız "Panel") düşüyor, komut paleti
/// hiçbir ekran bulamıyor ve kullanıcıya TEK BİR UYARI bile çıkmıyor. Kurucunun
/// "menüde arama yapamıyorum" şikâyetinin kökü budur.
///
/// ŞİMDİ: hata YUTULMAZ ve liste BOŞALTILMAZ.
///   • <see cref="Hata"/> dolar → kenar çubuğu görünür bir bant + "yeniden dene"
///     çizer (UX-S28: hata "kayıt yok"a çevrilmez).
///   • <c>moduller</c> alanına DOKUNULMAZ → son bilinen menü ayakta kalır.
///   • <see cref="yuklenenKullaniciId"/> ATANMAZ → sonraki tetikte yeniden denenir
///     (başarısız yükleme "yüklendi" sayılmaz).
/// ⚠ Bu bir yetki GEVŞETMESİ DEĞİLDİR: menü yalnız ÇİZİM yapar, son sözü sunucu
///   söyler. Eski davranış ise tersine bir GİZLEME hatasıydı — yetkili kullanıcı
///   kendi ekranlarını göremiyordu.
/// </summary>
public sealed class YetkiServisi(ApiIstemci api, OturumServisi oturum)
{
    private HashSet<string> moduller = [];
    private string? yuklenenKullaniciId;

    public event Action? Degisti;

    /// <summary>Son yükleme hatası — null ise hata yok. Ekranda BANT olarak görünür.</summary>
    public string? Hata { get; private set; }

    /// <summary>Yetkiler en az bir kez başarıyla yüklendi mi?</summary>
    public bool Yuklendi => yuklenenKullaniciId is not null;

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
            Hata = null;
        }
        catch (Exception ex)
        {
            // ⚠ moduller'e DOKUNULMAZ (son bilinen liste korunur) ve
            //   yuklenenKullaniciId ATANMAZ (yeniden denenebilir kalır).
            Hata = ex.Message;
        }
        Degisti?.Invoke();
    }

    /// <summary>Kullanıcının "yeniden dene" düğmesi — gerçek istek atar.</summary>
    public async Task YenidenDeneAsync()
    {
        Hata = null;
        Degisti?.Invoke();
        await HazirlaAsync();
    }

    private sealed record YetkiCevap(string[]? Moduller);
}
