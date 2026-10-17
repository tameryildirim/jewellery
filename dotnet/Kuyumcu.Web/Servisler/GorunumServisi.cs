using Kuyumcu.Contracts.Sayfalar.Gorunum;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Kiracı görünümü — renk şeması + yazı tipi ölçeği (paket P-36b, talimat 16).
///
/// <para>
/// ⚠ <b>KARAR SUNUCUNUNDUR.</b> Beyaz liste HEM burada HEM js/tema.js'teki
/// <c>kuyumcuGorunum</c> ad alanında uygulanır (iki taraf da güvenmez —
/// Mehmet K1). Bu sınıf kural ÜRETMEZ, sunucunun döndüğünü uygular.
/// </para>
/// <para>
/// ⚠ <b>localStorage'a YAZILMAZ.</b> Dil ve tema CİHAZ tercihidir; renk şeması
/// İŞLETME tercihidir (ArayuzModuServisi'nin gerekçesinin aynısı): aynı
/// dükkânın iki tezgâhı farklı renkte olmaz.
/// </para>
/// <para>
/// ⚠ Kurucusunda <b>ApiIstemci YOKTUR</b> (Aşkın K2): yerleşim çizen onlarca
/// bUnit sınıfı bu servisi tek satırla kaydedebilsin diye. İstek gerektiğinde
/// ApiIstemci kapsamdan O AN çözülür.
/// </para>
/// </summary>
public sealed class GorunumServisi(IJSRuntime js, IServiceProvider kapsam)
{
    // ⚠ TEMA TEMİZLİĞİ (kurucu 17.10): tek şema Yıldız. Bu iki dizi sunucudaki
    //   GorunumSecenekleri'nin AYNADAKİ karşılığıdır ve eşitlikleri
    //   GorunumEkranTestleri.GE8 ile KİLİTLİDİR — biri değişirse öteki de
    //   değişmek ZORUNDADIR, yoksa test kırmızı döner.
    public const string SemaVarsayilan = "yildiz";
    public const string OlcekVarsayilan = "18";

    /// <summary>Tek şema — sunucudaki GorunumSecenekleri.Semalar ile AYNI.</summary>
    public static readonly string[] Semalar = [SemaVarsayilan];
    public static readonly string[] Olcekler = ["16", OlcekVarsayilan, "20"];

    public string Sema { get; private set; } = SemaVarsayilan;
    public string YaziOlcegi { get; private set; } = OlcekVarsayilan;

    /// <summary>Sunucudan hiç okunmadıysa false — ekran "yükleniyor" der, tahmin etmez.</summary>
    public bool Yuklendi { get; private set; }

    /// <summary>
    /// PAKET SADE-1 (doğrulama bulgusu): okuma en az bir kez DENENİP BİTTİ mi.
    /// Ekran "okunamadı" hata bandını yalnız <c>OkumaDenendi &amp;&amp; !Yuklendi</c>
    /// iken çizer — istek sürerken bant yanıp sönmez (KullaniciGorunumServisi ile aynı desen).
    /// </summary>
    public bool OkumaDenendi { get; private set; }

    public event Action? Degisti;

    /// <summary>Açılışta kiracının görünümünü okur ve UYGULAR.</summary>
    public async Task YukleAsync()
    {
        if (kapsam.GetService(typeof(ApiIstemci)) is not ApiIstemci api) return;
        try
        {
            var g = await api.GetAsync<GorunumAyari>("/gorunum");
            await Uygula(g.Sema, g.YaziOlcegi);
            Yuklendi = true;
        }
        catch (ApiHata)
        {
            // md.1: okunamayan ayar "varsayılan seçildi" diye GİZLENMEZ; ekran
            // Yuklendi=false görür ve kaydetme düğmesini sebebiyle kilitler.
        }
        finally
        {
            // SADE-1: deneme bitti (başarılı ya da değil) — ekran artık hata bandını
            // çizebilir; başarısızlıkta da haber verilir ki açık sayfa yeniden çizilsin.
            OkumaDenendi = true;
            if (!Yuklendi) Degisti?.Invoke();
        }
    }

    /// <summary>
    /// Dükkân yazı boyutunu değiştirir: PATCH /gorunum → SUNUCU otoritedir, dönen
    /// değer uygulanır. Gönderilmeyen alan sunucuda KORUNUR (PATCH sözleşmesi).
    /// Tema temizliği (17.10): şema seçilemez (tek şema Yıldız) — yalnız punto gider.
    /// </summary>
    public async Task DegistirAsync(string yaziOlcegi)
    {
        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var govde = new { yaziOlcegi };
        var cevap = await api.PatchAsync<GorunumSarmal>("/gorunum", govde);
        await Uygula(cevap.Gorunum.Sema, cevap.Gorunum.YaziOlcegi);
        Yuklendi = true;
    }

    /// <summary>
    /// PAKET S1-f — tuval "Bunu dükkân varsayılanı yap": kullanıcının ETKİN
    /// üç görünüm seçimi (şema, punto, aydınlık/karanlık) TEK PATCH ile dükkân
    /// varsayılanı olur. Basit/profesyonel varsayılanı ayrı uçtadır
    /// (ArayuzModuServisi.DukkanVarsayilaniYapAsync → PATCH /admin/tenant). SAHİP kapısı SUNUCUDADIR ([RolGerekli("SAHIP")]);
    /// ekran yalnız sebebini söyler. Dönen gövde sunucunun yazdığıdır.
    /// <para>⚠ Sayfaya UYGULANMAZ (kuyumcuGorunum çağrılmaz): ekranı çizen
    /// kullanıcının kendi çözümüdür; dükkân varsayılanı yalnız devralan
    /// kullanıcılar için anlam taşır.</para>
    /// </summary>
    public async Task<GorunumAyari> VarsayilanYapAsync(string sema, string yaziOlcegi, string temaModu)
    {
        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var cevap = await api.PatchAsync<GorunumSarmal>("/gorunum",
            new { sema, yaziOlcegi, temaModu });
        Sema = Array.IndexOf(Semalar, cevap.Gorunum.Sema) >= 0 ? cevap.Gorunum.Sema : SemaVarsayilan;
        YaziOlcegi = Array.IndexOf(Olcekler, cevap.Gorunum.YaziOlcegi) >= 0 ? cevap.Gorunum.YaziOlcegi : OlcekVarsayilan;
        Yuklendi = true;
        Degisti?.Invoke();
        return cevap.Gorunum;
    }

    private async Task Uygula(string sema, string olcek)
    {
        Sema = Array.IndexOf(Semalar, sema) >= 0 ? sema : SemaVarsayilan;
        YaziOlcegi = Array.IndexOf(Olcekler, olcek) >= 0 ? olcek : OlcekVarsayilan;
        try
        {
            await js.InvokeVoidAsync("kuyumcuGorunum.uygula",
                new { sema = Sema, yaziOlcegi = YaziOlcegi });
        }
        catch (JSException)
        {
            // Betik yüklenmemişse (ön-işleme, bUnit) öznitelik yazılmaz; durum
            // C# tarafında doğrudur ve bir sonraki çağrıda uygulanır.
        }
        Degisti?.Invoke();
    }

    /// <summary>PATCH yanıtının sarmalı: <c>{ gorunum, mesaj }</c>.</summary>
    public sealed record GorunumSarmal(GorunumAyari Gorunum, string? Mesaj);
}
