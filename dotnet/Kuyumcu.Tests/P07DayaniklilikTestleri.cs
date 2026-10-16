using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-07 — API DAYANIKLILIK BANDI (kimlikler B-10 · B-57)
//
// B-10: 429'da ekran sessizce boşalmaz. İstemci üstel geri çekilme + jitter ile
//       en fazla N kez dener; açılmazsa kullanıcıya "Yeniden dene" + kalan süre
//       söyleyen bir OranSinirHatasi fırlatılır (ApiHata'dan türer → mevcut
//       catch blokları hata bandını gösterir, liste boşaltılmaz — UX-S28/S30).
//
// B-57: QA turu 5 ölçümü — tek sayfa ömründe `kur/guncel` ucuna 130+ istek.
//       Kısa ömürlü GET önbelleği + tek uçuş fırtınayı söndürür; buna RAĞMEN
//       tavan aşılırsa istek ATILMAZ, hata görünür olur (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P07DayaniklilikTestleri : BunitContext
{
    private readonly List<string> yollar = [];
    private readonly List<TimeSpan> beklemeler = [];
    private DateTimeOffset saat = new(2026, 9, 18, 9, 0, 0, TimeSpan.Zero);

    private ApiIstemci Kur(HttpMessageHandler isleyici)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(isleyici) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();

        var oturum = Services.GetRequiredService<OturumServisi>();
        oturum.TokenKaydetAsync("access-1", "refresh-1").GetAwaiter().GetResult();

        var api = Services.GetRequiredService<ApiIstemci>();
        api.Simdi = () => saat;
        api.Jitter = tavan => tavan;                 // en kötü hâl: jitter tavanda
        api.Uyu = sure => { beklemeler.Add(sure); return Task.CompletedTask; };
        return api;
    }

    // ── B-10 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task B10_429_sonrasi_geri_cekilip_yeniden_dener_ve_basarili_olur()
    {
        var api = Kur(new SahteApi(yollar, sira: [429, 429, 200]));

        var sonuc = await api.GetAsync<Kayit>("/kur/guncel");

        Assert.Equal("ok", sonuc.Deger);
        Assert.Equal(3, yollar.Count);               // 1 asıl + 2 yeniden deneme
        Assert.Equal(2, beklemeler.Count);
    }

    [Fact]
    public async Task B10_geri_cekilme_USTEL_ve_JITTERLI_dir_en_fazla_N_deneme()
    {
        var api = Kur(new SahteApi(yollar, sira: [429, 429, 429, 429, 429, 429]));

        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<Kayit>("/kur/guncel"));

        // 1 asıl + ApiIstemci.GeriCekilmeDenemeTavani yeniden deneme
        Assert.Equal(ApiIstemci.GeriCekilmeDenemeTavani + 1, yollar.Count);
        Assert.Equal(ApiIstemci.GeriCekilmeDenemeTavani, beklemeler.Count);
        // 200·2^(n-1) + jitter(250) → 450 / 650 / 1050 ms: her basamak bir öncekinin üstünde
        Assert.Equal(TimeSpan.FromMilliseconds(200 + 250), beklemeler[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(400 + 250), beklemeler[1]);
        Assert.Equal(TimeSpan.FromMilliseconds(800 + 250), beklemeler[2]);
    }

    [Fact]
    public async Task B10_israrci_429_ekrani_bosaltmaz_Yeniden_dene_ve_kalan_sure_yazar()
    {
        var api = Kur(new SahteApi(yollar, sira: [429, 429, 429, 429]));

        var hata = await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<Kayit>("/kur/guncel"));

        Assert.IsAssignableFrom<ApiHata>(hata);      // mevcut catch(ApiHata) blokları yakalar
        Assert.Equal(429, hata.StatusCode);
        Assert.True(hata.KalanSaniye > 0);
        Assert.Contains("Yeniden dene", hata.Message);
        Assert.Contains(hata.KalanSaniye.ToString(), hata.Message);
    }

    [Fact]
    public async Task B10_sunucu_UZUN_bekleme_isterse_otomatik_denenmez_karar_kullaniciya_kalir()
    {
        var api = Kur(new SahteApi(yollar, sira: [429], retryAfter: 60));

        var hata = await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<Kayit>("/kur/guncel"));

        Assert.Equal(60, hata.KalanSaniye);
        Assert.Single(yollar);                       // tek istek — kullanıcı 3 dk bekletilmez
        Assert.Empty(beklemeler);
    }

    [Fact]
    public async Task B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir()
    {
        var api = Kur(new BirKez401(yollar));

        var gorevler = new[] { "/kur/guncel", "/cari", "/urunler", "/kasalar", "/subeler" }
            .Select(yol => Task.Run(() => api.GetAsync<Kayit>(yol)));
        var sonuclar = await Task.WhenAll(gorevler);

        Assert.All(sonuclar, s => Assert.Equal("ok", s.Deger));
        Assert.Equal(1, yollar.Count(y => y.Contains("auth/yenile")));
    }

    // ── B-57 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task B57_ayni_uca_tavani_asan_istek_ATILMAZ_hata_gorunur_olur()
    {
        var api = Kur(new SahteApi(yollar, sira: []));

        for (var i = 0; i < ApiIstemci.IstekTekrarTavani; i++)
            await api.GetAsync<Kayit>("/kur/guncel");

        var atilan = yollar.Count;
        var hata = await Assert.ThrowsAsync<IstekFirtinasiHatasi>(
            () => api.GetAsync<Kayit>("/kur/guncel"));

        Assert.Equal(atilan, yollar.Count);          // istek GÖNDERİLMEDİ
        Assert.Equal(ApiIstemci.IstekTekrarTavani, hata.Tavan);
        Assert.Equal(ApiIstemci.IstekTekrarTavani + 1, hata.Sayi);
        Assert.Contains("kur/guncel", hata.Yol);
        Assert.Contains("İstek gönderilmedi", hata.Message);
    }

    [Fact]
    public async Task B57_sorgu_dizesi_degisse_bile_AYNI_uc_sayilir()
    {
        var api = Kur(new SahteApi(yollar, sira: []));

        for (var i = 0; i < ApiIstemci.IstekTekrarTavani; i++)
            await api.GetAsync<Kayit>($"/kur/guncel?sayac={i}");

        await Assert.ThrowsAsync<IstekFirtinasiHatasi>(
            () => api.GetAsync<Kayit>("/kur/guncel?sayac=son"));
    }

    [Fact]
    public async Task B57_AYNI_ANDA_ucusan_GET_ler_TEK_istege_iner()
    {
        // ⚠ TEK UÇUŞ (single-flight) sınanır. Sunucu yavaş yanıt
        //   verdiği için 130 çağrı gerçekten ÜST ÜSTE biner — QA turu 5'in
        //   gördüğü tablo budur (sayfa açılışında aynı uca yığılma).
        var api = Kur(new YavasApi(yollar));

        var gorevler = Enumerable.Range(0, 130).Select(_ => api.GetAsync<Kayit>("/kur/guncel")).ToArray();
        var sonuclar = await Task.WhenAll(gorevler);

        Assert.All(sonuclar, s => Assert.Equal("ok", s.Deger));
        Assert.Single(yollar);
    }

    [Fact]
    public async Task B57_pencere_gecince_sayac_sifirlanir()
    {
        var api = Kur(new SahteApi(yollar, sira: []));

        for (var i = 0; i < ApiIstemci.IstekTekrarTavani; i++)
            await api.GetAsync<Kayit>("/kur/guncel");

        saat = saat.Add(ApiIstemci.IstekPenceresi);
        await api.GetAsync<Kayit>("/kur/guncel");    // fırlatmamalı

        Assert.Equal(ApiIstemci.IstekTekrarTavani + 1, yollar.Count);
    }

    // ── sahte sunucular ─────────────────────────────────────────────────────

    public sealed record Kayit(string Deger);

    /// <summary>Sırayla verilen durum kodlarını döner; sıra bitince 200.</summary>
    private sealed class SahteApi(List<string> kayit, int[] sira, int? retryAfter = null) : HttpMessageHandler
    {
        private int adim;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            lock (kayit) kayit.Add(istek.RequestUri!.PathAndQuery);
            var sayi = adim < sira.Length ? sira[adim] : 200;
            adim++;
            var yanit = new HttpResponseMessage((HttpStatusCode)sayi)
            {
                Content = new StringContent(
                    sayi == 200 ? """{"deger":"ok"}""" : """{"message":"Çok fazla istek","statusCode":429}""",
                    System.Text.Encoding.UTF8, "application/json"),
            };
            if (sayi == 429 && retryAfter is int s) yanit.Headers.Add("Retry-After", s.ToString());
            return Task.FromResult(yanit);
        }
    }

    /// <summary>Yavaş yanıt veren sunucu: eşzamanlı çağrılar gerçekten üst üste biner.</summary>
    private sealed class YavasApi(List<string> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            lock (kayit) kayit.Add(istek.RequestUri!.PathAndQuery);
            await Task.Delay(60, iptal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SahteGovde, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private const string SahteGovde = "{\"deger\":\"ok\"}";
    }

    /// <summary>İlk turda 401, yenilemeden sonra 200 — jeton yarışı senaryosu.</summary>
    private sealed class BirKez401(List<string> kayit) : HttpMessageHandler
    {
        private int yenilendi;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (kayit) kayit.Add(yol);

            if (yol.Contains("auth/yenile"))
            {
                await Task.Delay(30, iptal);
                Interlocked.Exchange(ref yenilendi, 1);
                return Json(HttpStatusCode.OK,
                    """{"accessToken":"access-2","refreshToken":"refresh-2"}""");
            }

            // Gerçek eşzamanlılık: beş istek 401'i AYNI ANDA görsün.
            await Task.Delay(20, iptal);
            return Volatile.Read(ref yenilendi) == 1
                ? Json(HttpStatusCode.OK, """{"deger":"ok"}""")
                : Json(HttpStatusCode.Unauthorized, """{"message":"Yetkisiz","statusCode":401}""");
        }

        private static HttpResponseMessage Json(HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }
}
