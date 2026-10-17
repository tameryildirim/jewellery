using Kuyumcu.Contracts.Sayfalar.Gorunum;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET B-1 — KULLANICININ KENDİ görünümü (renk şeması + tema modu + punto).
///
/// <para>
/// ⚠ <b>KARAR SUNUCUNUNDUR.</b> Üç katmanın (kullanıcı → dükkân → sistem)
/// çözümü sunucudadır; bu sınıf çözümü TEKRARLAMAZ, dönen değeri uygular.
/// Beyaz liste yine de HEM burada HEM <c>js/tema.js</c>'te uygulanır
/// (iki taraf da güvenmez — Mehmet K1).
/// </para>
/// <para>
/// ⚠ <b>Tercih kullanıcı SATIRINDA kalıcıdır</b>; aynı kişi başka tezgâhta oturum
/// açınca kendi görünümünü bulur. KESIF-P1 · TEMA-TEK: <c>kuyumcuTema.uygula</c>
/// sunucunun değerini localStorage'a yalnız İLK BOYAMA ÖNBELLEĞİ olarak yazar
/// (tema.js); doğruluk kaynağı DEĞİLDİR. Servis açılışta ve her oturum
/// değişiminde okunur (Program.cs) — yalnız /ayarlar'da değil.
/// </para>
/// <para>
/// ⚠ Kurucusunda <b>ApiIstemci YOKTUR</b> (Aşkın K2) — yerleşim çizen onlarca
/// bUnit sınıfı bu servisi tek satırla kaydedebilsin diye; istek gerektiğinde
/// kapsamdan O AN çözülür. <see cref="GorunumServisi"/> ile aynı desen.
/// </para>
/// </summary>
public sealed class KullaniciGorunumServisi(IJSRuntime js, IServiceProvider kapsam)
{
    public const string KaynakKullanici = "kullanici";
    public const string KaynakKiraci = "kiraci";
    public const string KaynakSistem = "sistem";

    /// <summary>Sunucunun döndürdüğü son durum — ekran bunu çizer.</summary>
    public KullaniciGorunumAyari? Durum { get; private set; }

    /// <summary>Sunucudan hiç okunmadıysa false — ekran "yüklenemedi" der, tahmin etmez.</summary>
    public bool Yuklendi { get; private set; }

    /// <summary>
    /// PAKET S1-f — okuma DENENDİ mi (başarılı ya da değil)? Ekran "okunuyor"
    /// ile "okunamadı"yı ayırır: yükleme sürerken "okunamadı" yazmak yalan olur.
    /// </summary>
    public bool OkumaDenendi { get; private set; }

    public event Action? Degisti;

    /// <summary>Açılışta kullanıcının görünümünü okur ve UYGULAR.</summary>
    public async Task YukleAsync()
    {
        if (kapsam.GetService(typeof(ApiIstemci)) is not ApiIstemci api) return;
        try
        {
            await Uygula(await api.GetAsync<KullaniciGorunumAyari>("/kullanici/gorunum"));
        }
        catch (ApiHata)
        {
            // md.1: okunamayan ayar "varsayılan seçildi" diye GİZLENMEZ; ekran
            // Yuklendi=false görür ve seçicileri sebebiyle kilitler.
        }
        OkumaDenendi = true;
        Degisti?.Invoke();
    }

    /// <summary>Alan başına "dükkân varsayılanını devral" — null gönderir.</summary>
    public Task TemaModuDevretAsync() => Gonder(new TemaGovde(null));

    /// <inheritdoc cref="TemaModuDevretAsync"/>
    public Task YaziBoyutuDevretAsync() => Gonder(new YaziGovde(null));

    public Task TemaModuSecAsync(string mod) => Gonder(new TemaGovde(mod));
    public Task YaziBoyutuSecAsync(int punto) => Gonder(new YaziGovde(punto));

    /// <summary>
    /// PAKET S1-f — tuval "Dükkân varsayılanına dön": ÜÇ görünüm alanı TEK
    /// istekte null gider (kullanıcı satırı temizlenir, dükkân varsayılanı
    /// devralınır). Basit/profesyonel mod bu uçta DEĞİLDİR (ArayuzModuServisi,
    /// PATCH /kullanicilar/ben) — kart onu ayrı ve sonucunu ayrı söyleyerek gönderir.
    /// </summary>
    public Task HepsiniDevretAsync() => Gonder(new HepsiGovde(null, null, null));

    private async Task Gonder(object govde)
    {
        var api = (ApiIstemci)kapsam.GetService(typeof(ApiIstemci))!;
        var cevap = await api.PatchAsync<Sarmal>("/kullanici/gorunum", govde);
        await Uygula(cevap.Gorunum);
    }

    private async Task Uygula(KullaniciGorunumAyari g)
    {
        Durum = g;
        Yuklendi = true;
        try
        {
            // Renk şeması + punto: tek çağrı (kuyumcuGorunum beyaz listeyi ikinci
            // kez uygular). Punto KANONİK kodla ('16'|'18'|'20') gider.
            await js.InvokeVoidAsync("kuyumcuGorunum.uygula",
                new
                {
                    sema = g.Sema,
                    yaziOlcegi = g.YaziBoyutu.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
            // Aydınlık/karanlık AYRI ad alanıdır (data-tema) — kuyumcuTema yazar.
            await js.InvokeVoidAsync("kuyumcuTema.uygula", g.TemaModu);
        }
        catch (JSException)
        {
            // Betik yüklenmemişse (ön-işleme, bUnit) öznitelik yazılmaz; durum
            // C# tarafında doğrudur ve bir sonraki çağrıda uygulanır.
        }
        Degisti?.Invoke();
    }

    /// <summary>PATCH yanıtının sarmalı: <c>{ gorunum, mesaj }</c>.</summary>
    public sealed record Sarmal(KullaniciGorunumAyari Gorunum, string? Mesaj);

    // ⚠ Gövdeler AYRI tiptir: PATCH sözleşmesi gereği yalnız DEĞİŞEN alan
    //   serileştirilir; anonim tipte null alanın atlanması ayara bağlı olurdu.
    private sealed record TemaGovde(string? TemaModu);
    private sealed record YaziGovde(int? YaziBoyutu);
    private sealed record HepsiGovde(string? TemaModu, string? Sema, int? YaziBoyutu);
}
