using System.Net;
using System.Net.Http.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ISTEK-1 — İSTEK SINIRI VE TEKRAR ÇEKİLEN LİSTELER
//
// KAYNAK: orkestratör ölçümü (25.09, başsız Chrome, TEK kullanıcı) — 10 ekran
// 30 sn'de 67 istek (~134/dk) > genel sınır 100/dk; göz incelemesinde 11 KRİTİK
// bulgu (tablolar boşalıyor, kırmızı "çok yoğun" bantları, zilde "?").
//
// Bu dosya: (1) liste önbelleği — tek uçuş, 60 sn tazelik, yazma/oturum
// sonrası geçersiz kılma, beyaz liste; (2) 429 — liste EKRANDA KALIR, sade tek
// bant, Retry-After'a göre bir kez bekleyip yeniden deneme; (3) zil "?"
// basmaz; (4) filtre AÇMAK istek atmaz; (5) ürün listesi stok sütunu TEK
// istek; (6) HTTP — genel sınır 300/dk, giriş hâlâ 5/dk, toplu stok ucu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Programlanabilir sahte sunucu: her isteği kaydeder, yanıtı işlevden alır.</summary>
internal sealed class Istek1SahteApi(Func<HttpRequestMessage, (int Kod, string Govde, int? RetryAfter)> yanitla)
    : HttpMessageHandler
{
    public List<(string Metot, string Yol)> Kayit { get; } = [];
    public TimeSpan Gecikme { get; set; } = TimeSpan.Zero;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
    {
        lock (Kayit) Kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery));
        if (Gecikme > TimeSpan.Zero) await Task.Delay(Gecikme, iptal);
        var (kod, govde, ra) = yanitla(istek);
        var y = new HttpResponseMessage((HttpStatusCode)kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
        if (ra is int s) y.Headers.Add("Retry-After", s.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return y;
    }

    public int Say(string metot, string yolParcasi)
    {
        lock (Kayit) return Kayit.Count(k => k.Metot == metot && k.Yol.Contains(yolParcasi, StringComparison.Ordinal));
    }

    public const string Yogun429 = """{"message":"Çok fazla istek gönderildi, lütfen biraz bekleyin","error":"TooManyRequests","statusCode":429}""";
}

// ─────────────────────────────────────────────────────────────────────────────
// (1)+(2) ApiIstemci düzeyi — önbellek ve 429 davranışı
// ─────────────────────────────────────────────────────────────────────────────
public sealed class Istek1OnbellekTestleri : BunitContext
{
    private DateTimeOffset saat = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    private readonly List<TimeSpan> beklemeler = [];

    private (ApiIstemci Api, Istek1SahteApi Sunucu) Kur(Func<HttpRequestMessage, (int, string, int?)> yanitla)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var sunucu = new Istek1SahteApi(yanitla);
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(sunucu) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().TokenKaydetAsync("access-1", "refresh-1").GetAwaiter().GetResult();
        var api = Services.GetRequiredService<ApiIstemci>();
        api.Simdi = () => saat;
        api.Onbellek.Simdi = () => saat;
        api.Jitter = _ => 0;
        api.Uyu = sure => { beklemeler.Add(sure); return Task.CompletedTask; };
        return (api, sunucu);
    }

    private static (int, string, int?) Tamam(string govde = """[{"id":"s1","ad":"Merkez"}]""") => (200, govde, null);

    public sealed record Oge(string Id, string Ad);

    /// <summary>MUTASYON HEDEFİ (MISTEK-1): önbellek devre dışı → aynı liste 5 kez çekilir, KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_01_ayni_liste_60_sn_icinde_TEK_istekle_verilir()
    {
        var (api, sunucu) = Kur(_ => Tamam());

        for (var i = 0; i < 5; i++)
        {
            var l = await api.GetAsync<List<Oge>>("/subeler");
            Assert.Equal("Merkez", Assert.Single(l).Ad);
        }

        Assert.Equal(1, sunucu.Say("GET", "/subeler"));
        Assert.Equal(4, api.Onbellek.Isabet);

        // 60 sn dolunca TAZE çekilir (bayat liste ömür boyu kalmaz).
        saat = saat.Add(ListeOnbellegi.Tazelik);
        await api.GetAsync<List<Oge>>("/subeler");
        Assert.Equal(2, sunucu.Say("GET", "/subeler"));
    }

    [Fact]
    public async Task ISTEK1_02_onbellek_KAPALIYKEN_istek_sayisi_artar_olcu_kalibrasyonu()
    {
        // Aynı senaryonun önbelleksiz hâli: önceki testin 1 isteği gerçekten
        // önbellekten geliyor, sahte sunucudan değil.
        var (api, sunucu) = Kur(_ => Tamam());
        api.Onbellek.Etkin = false;

        for (var i = 0; i < 5; i++) await api.GetAsync<List<Oge>>("/subeler");

        Assert.Equal(5, sunucu.Say("GET", "/subeler"));
    }

    [Fact]
    public async Task ISTEK1_03_ayni_anda_gelen_istekler_TEK_ucusa_iner_sonra_onbellekten_gelir()
    {
        var (api, sunucu) = Kur(_ => Tamam("""[{"id":"r1","ad":"Ön Vitrin"}]"""));
        sunucu.Gecikme = TimeSpan.FromMilliseconds(60);

        var gorevler = Enumerable.Range(0, 25).Select(_ => api.GetAsync<List<Oge>>("/reyonlar")).ToArray();
        var sonuc = await Task.WhenAll(gorevler);
        Assert.All(sonuc, l => Assert.Equal("r1", Assert.Single(l).Id));
        Assert.Equal(1, sunucu.Say("GET", "/reyonlar"));

        await api.GetAsync<List<Oge>>("/reyonlar");         // uçuş bitti → önbellek
        Assert.Equal(1, sunucu.Say("GET", "/reyonlar"));
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-2): yazma önbelleği düşürmezse KIRMIZI.</summary>
    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ISTEK1_04_yazma_sonrasi_liste_gecersiz_kilinir(string metot)
    {
        var reyonlar = """[{"id":"r1","ad":"Ön Vitrin"}]""";
        var (api, sunucu) = Kur(i => i.Method == HttpMethod.Get ? Tamam(reyonlar) : Tamam("""{"id":"r2"}"""));

        Assert.Single(await api.GetAsync<List<Oge>>("/reyonlar"));
        reyonlar = """[{"id":"r1","ad":"Ön Vitrin"},{"id":"r2","ad":"Arka Vitrin"}]""";
        switch (metot)
        {
            case "POST": await api.PostAsync("/reyonlar", new { ad = "Arka Vitrin" }); break;
            case "PATCH": await api.PatchAsync("/reyonlar/r2", new { ad = "Arka Vitrin" }); break;
            default: await api.DeleteAsync<System.Text.Json.JsonElement>("/reyonlar/r9"); break;
        }

        var sonra = await api.GetAsync<List<Oge>>("/reyonlar");
        Assert.Equal(2, sonra.Count);                              // yeni reyon GÖRÜNÜR
        Assert.Equal(2, sunucu.Say("GET", "/reyonlar"));
    }

    [Fact]
    public async Task ISTEK1_05_beyaz_liste_DISI_yol_onbellege_girmez_alt_kayit_da_girmez()
    {
        var (api, sunucu) = Kur(_ => Tamam());

        await api.GetAsync<List<Oge>>("/cari?aktif=true&limit=200");
        await api.GetAsync<List<Oge>>("/cari?aktif=true&limit=200");
        await api.GetAsync<List<Oge>>("/kasalar/k1/hareketler");
        await api.GetAsync<List<Oge>>("/kasalar/k1/hareketler");

        Assert.Equal(2, sunucu.Say("GET", "/cari"));
        Assert.Equal(2, sunucu.Say("GET", "/kasalar/k1/hareketler"));
    }

    [Fact]
    public async Task ISTEK1_06_oturum_olayi_ve_kimlik_degisimi_onbellegi_bosaltir()
    {
        var (api, sunucu) = Kur(_ => Tamam());
        var oturum = Services.GetRequiredService<OturumServisi>();
        await api.GetAsync<List<Oge>>("/subeler");

        // Açılıştaki İLK kullanıcı ataması (/auth/ben) aynı oturumdur → düşürmez.
        oturum.KullaniciAta(new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        await api.GetAsync<List<Oge>>("/subeler");
        Assert.Equal(1, sunucu.Say("GET", "/subeler"));

        // Sonraki atama (başka kullanıcı/şube, mod, profil) → boşalır.
        oturum.KullaniciAta(new OturumKullanici("u2", "Başka", "Kullanıcı", "b@ornek.com", "SAHIP", "s2"));
        await api.GetAsync<List<Oge>>("/subeler");
        Assert.Equal(2, sunucu.Say("GET", "/subeler"));

        // Çıkış (kullanıcı yok) → boşalır.
        oturum.KullaniciAta(null);
        await api.GetAsync<List<Oge>>("/subeler");
        Assert.Equal(3, sunucu.Say("GET", "/subeler"));

        // Jetondaki kiracı değişirse (olay olmasa da) başka kiracının listesi VERİLMEZ.
        Assert.NotEqual(ListeOnbellegi.KimlikCoz(Jeton("u1", "t1", "s1")), ListeOnbellegi.KimlikCoz(Jeton("u1", "t2", "s1")));
        Assert.NotEqual(ListeOnbellegi.KimlikCoz(Jeton("u1", "t1", "s1")), ListeOnbellegi.KimlikCoz(Jeton("u1", "t1", "s2")));
        Assert.Equal(ListeOnbellegi.KimlikCoz(Jeton("u1", "t1", "s1")), ListeOnbellegi.KimlikCoz(Jeton("u1", "t1", "s1", "x")));
    }

    private static string Jeton(string sub, string tenant, string sube, string ek = "")
    {
        static string B64(string s) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("""{"alg":"HS256"}""")}.{B64($$"""{"sub":"{{sub}}","tenantId":"{{tenant}}","subeId":"{{sube}}","rol":"SAHIP","jti":"{{ek}}"}""")}.imza";
    }

    [Fact]
    public async Task ISTEK1_07_canli_kur_YALNIZ_push_bagliyken_onbellekten_verilir_push_gelince_duser()
    {
        var (api, sunucu) = Kur(_ => Tamam("""{"id":"kur-1"}"""));

        // Push kopuk: her okuma sunucuya (kur fotoğrafına bağlanan yazma bayat kurla yapılamaz).
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        Assert.Equal(2, sunucu.Say("GET", "/kur/guncel"));

        // Push bağlı: ek çekim YOK.
        api.Onbellek.KurCanliBagli = true;
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        Assert.Equal(2, sunucu.Say("GET", "/kur/guncel"));

        // Yeni kur itildi → kayıt düşer, sonraki okuma tazedir.
        api.Onbellek.Dusur(ListeOnbellegi.KurYolu);
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        Assert.Equal(3, sunucu.Say("GET", "/kur/guncel"));

        // "Yenile" (TazeGetAsync) önbelleği HER ZAMAN atlar — Yenile yalancı değil.
        await api.TazeGetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        Assert.Equal(4, sunucu.Say("GET", "/kur/guncel"));
    }

    [Fact]
    public async Task ISTEK1_08_yazma_SURERKEN_baslayan_okuma_onbellege_yazilmaz()
    {
        var reyonlar = """[{"id":"r1","ad":"Eski"}]""";
        var (api, sunucu) = Kur(i => i.Method == HttpMethod.Get ? Tamam(reyonlar) : Tamam("{}"));
        sunucu.Gecikme = TimeSpan.FromMilliseconds(40);

        var okuma = api.GetAsync<List<Oge>>("/reyonlar");          // eski hâli okuyacak
        var yazma = api.PostAsync("/reyonlar", new { ad = "Yeni" });
        await Task.WhenAll(okuma, yazma);
        reyonlar = """[{"id":"r1","ad":"Eski"},{"id":"r2","ad":"Yeni"}]""";

        var sonra = await api.GetAsync<List<Oge>>("/reyonlar");
        Assert.Equal(2, sonra.Count);                              // eski yanıt SIZMADI
        Assert.Equal(2, sunucu.Say("GET", "/reyonlar"));           // bitmiş eski uçuşa katılmadı
    }

    /// <summary>
    /// MUTASYON HEDEFİ (MISTEK-13): tek uçuş kaydı sonuç yazıldıktan SONRA silinirse,
    /// hemen ardından gelen çağrı BİTMİŞ eski uçuşa katılıp eski yanıtı alır (yarış;
    /// testte yakalandı). Doğru kodda sıralı her çağrı sunucuya gider — deterministik.
    /// </summary>
    [Fact]
    public async Task ISTEK1_14_ardisik_GET_ler_bitmis_eski_ucusa_KATILMAZ()
    {
        var sayac = 0;
        var (api, sunucu) = Kur(_ => Tamam($$"""[{"id":"c{{Interlocked.Increment(ref sayac)}}","ad":"x"}]"""));
        sunucu.Gecikme = TimeSpan.FromMilliseconds(1);

        for (var i = 1; i <= 150; i++)
        {
            var l = await api.GetAsync<List<Oge>>("/cari?limit=5");
            Assert.Equal($"c{i}", Assert.Single(l).Id);
            if (i % 50 == 0) saat = saat.Add(ApiIstemci.IstekPenceresi);   // fırtına tavanı (60/dk) ölçüyü bozmasın
        }
        Assert.Equal(150, sunucu.Say("GET", "/cari"));
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-3): 429'da son iyi liste dönülmezse KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_09_israrci_429_da_son_iyi_liste_doner_ve_TEK_yogunluk_bandi_acilir()
    {
        var yogun = false;
        var (api, sunucu) = Kur(_ => yogun ? (429, Istek1SahteApi.Yogun429, 60) : Tamam("""[{"id":"c1","ad":"Ayşe"}]"""));
        var olay = 0;
        api.YogunlukDegisti += () => olay++;

        Assert.Single(await api.GetAsync<List<Oge>>("/cari?limit=100"));
        yogun = true;
        var yine = await api.GetAsync<List<Oge>>("/cari?limit=100");   // FIRLATMAZ

        Assert.Equal("Ayşe", Assert.Single(yine).Ad);              // liste BOŞALMADI
        Assert.True(api.Yogun);
        Assert.Equal(1, olay);

        yogun = false;
        await api.GetAsync<List<Oge>>("/cari?limit=100");
        Assert.False(api.Yogun);                                   // kendiliğinden kapandı
        Assert.Equal(2, olay);
    }

    [Fact]
    public async Task ISTEK1_10_hic_veri_yokken_israrci_429_SADE_mesajla_hata_verir_jargon_yok()
    {
        var (api, _) = Kur(_ => (429, Istek1SahteApi.Yogun429, 60));

        var hata = await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<List<Oge>>("/cari?limit=100"));

        Assert.StartsWith("Biraz yoğunluk var", hata.Message);
        Assert.DoesNotContain("istek sınırı", hata.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("429", hata.Message);
        Assert.Contains("Yeniden dene", hata.Message);
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-4): Retry-After'a göre bir kez bekleme kaldırılırsa KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_11_Retry_After_10_sn_ise_BIR_KEZ_o_kadar_bekleyip_yeniden_dener()
    {
        var sira = 0;
        var (api, sunucu) = Kur(_ => sira++ == 0 ? (429, Istek1SahteApi.Yogun429, 10) : Tamam());
        var gorulen = new List<bool>();
        api.YogunlukDegisti += () => gorulen.Add(api.Yogun);

        var l = await api.GetAsync<List<Oge>>("/raporlar/gunluk");

        Assert.Single(l);
        Assert.Equal(2, sunucu.Say("GET", "/raporlar/gunluk"));
        Assert.Equal(TimeSpan.FromSeconds(10), Assert.Single(beklemeler));
        Assert.Equal(new[] { true, false }, gorulen);               // bant açıldı ve kapandı
    }

    [Fact]
    public async Task ISTEK1_12_Retry_After_ikinci_kez_gelirse_BIR_kereden_fazla_beklemez()
    {
        var (api, sunucu) = Kur(_ => (429, Istek1SahteApi.Yogun429, 10));

        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<List<Oge>>("/raporlar/gunluk"));

        Assert.Equal(2, sunucu.Say("GET", "/raporlar/gunluk"));   // asıl + tek yeniden deneme
        Assert.Single(beklemeler);
    }

    /// <summary>
    /// MUTASYON HEDEFİ (MISTEK-16) — doğrulayıcı bulgusu: son-iyi yedeği YAŞ SINIRLI.
    /// 10:00'da çekilen gövde 10:20'de 429 alınca "şimdiki" gibi DÖNMEZ; hata çağırana
    /// ulaşır (sayfa kendi bandını gösterir). Sınır içindeki (Retry-After + 60 sn) yedek döner.
    /// </summary>
    [Fact]
    public async Task ISTEK1_15_son_iyi_yedegi_YAS_SINIRLI_eski_govde_simdiki_gibi_donmez()
    {
        var yogun = false;
        var (api, _) = Kur(_ => yogun ? (429, Istek1SahteApi.Yogun429, 10) : Tamam("""[{"id":"k1","ad":"Ana Kasa"}]"""));

        Assert.Single(await api.GetAsync<List<Oge>>("/raporlar/kasa"));
        yogun = true;

        // Sınır içinde (60 sn sonra; pay = 10 + 60 sn): yedek döner, bant açık.
        saat = saat.AddSeconds(60);
        Assert.Single(await api.GetAsync<List<Oge>>("/raporlar/kasa"));
        Assert.True(api.Yogun);

        // 20 dakika sonra: eski bakiye DÖNMEZ — hata.
        saat = saat.AddMinutes(20);
        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<List<Oge>>("/raporlar/kasa"));
    }

    /// <summary>
    /// MUTASYON HEDEFİ (MISTEK-17) — doğrulayıcı bulgusu: "Yenile" (TazeGetAsync) ve canlı
    /// kur 429'da son-iyi yedeğine DÜŞMEZ; taze sorgu eski gövdeyi yeni gibi alamaz.
    /// </summary>
    [Fact]
    public async Task ISTEK1_16_Yenile_ve_canli_kur_429_da_yedege_DUSMEZ_hata_cagirana_ulasir()
    {
        var yogun = false;
        var (api, _) = Kur(i => yogun
            ? (429, Istek1SahteApi.Yogun429, 60)
            : Tamam(i.RequestUri!.AbsolutePath.Contains("/kur/") ? """{"id":"kur-1"}""" : """[{"id":"c1","ad":"Ayşe"}]"""));

        await api.GetAsync<List<Oge>>("/cari?limit=100");
        await api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel");
        yogun = true;

        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.TazeGetAsync<List<Oge>>("/cari?limit=100"));
        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.TazeGetAsync<System.Text.Json.JsonElement>("/kur/guncel"));
        await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<System.Text.Json.JsonElement>("/kur/guncel"));
        // Aynı anın taze olmayan liste okuması yine yedeği alır (ekran boşalmaz).
        Assert.Single(await api.GetAsync<List<Oge>>("/cari?limit=100"));
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-18): /kasalar bakiye taşır → 10 sn tazelik (60 değil).</summary>
    [Fact]
    public async Task ISTEK1_17_kasalar_BAKIYE_tasir_10_sn_tazelik()
    {
        var (api, sunucu) = Kur(_ => Tamam("""[{"id":"k1","ad":"Ana Kasa"}]"""));

        await api.GetAsync<List<Oge>>("/kasalar");
        saat = saat.AddSeconds(5);
        await api.GetAsync<List<Oge>>("/kasalar");
        Assert.Equal(1, sunucu.Say("GET", "/kasalar"));

        saat = saat.AddSeconds(5);                                   // 10 sn doldu
        await api.GetAsync<List<Oge>>("/kasalar");
        Assert.Equal(2, sunucu.Say("GET", "/kasalar"));
        Assert.Equal(TimeSpan.FromSeconds(10), ListeOnbellegi.TazelikBul("/kasalar?aktif=true"));
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-19): yoğunluk metni sözlükten, etkin dilde (çıplak TR yok).</summary>
    [Fact]
    public async Task ISTEK1_18_yogunluk_hata_metni_ETKIN_DILDE_sozlukten()
    {
        var (api, _) = Kur(_ => (429, Istek1SahteApi.Yogun429, 60));
        api.DilKaynagi = () => "en";

        var hata = await Assert.ThrowsAsync<OranSinirHatasi>(() => api.GetAsync<List<Oge>>("/cari?limit=100"));

        Assert.Equal(UstBarSozlugu.OranSinir("en", 60), hata.Message);
        Assert.DoesNotContain("yoğunluk", hata.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("60", hata.Message);
        Assert.NotEqual(UstBarSozlugu.OranSinir("tr", 60), UstBarSozlugu.OranSinir("en", 60));
    }

    /// <summary>Canlı ölçümde görüldü: sebep noktayla bitince saat ipucunda ".." çıkıyordu.</summary>
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void ISTEK1_19_saat_ipucu_sebep_noktayla_bitse_de_CIFT_NOKTA_yok(string dil)
    {
        var m = UstBarSozlugu.SonSorguHatali(dil, "14:12", UstBarSozlugu.OranSinir(dil, 10));
        Assert.DoesNotContain("..", m);
        Assert.Contains(UstBarSozlugu.OranSinir(dil, 10).TrimEnd('.'), m);
    }

    [Fact]
    public void ISTEK1_13_yogunluk_metni_TR_EN_sade_tek_satir()
    {
        Assert.Equal("Biraz yoğunluk var, birazdan kendiliğinden yenilenecek.", UstBarSozlugu.Yogunluk("tr"));
        Assert.False(string.IsNullOrWhiteSpace(UstBarSozlugu.Yogunluk("en")));
        Assert.NotEqual(UstBarSozlugu.Yogunluk("tr"), UstBarSozlugu.Yogunluk("en"));
        Assert.DoesNotContain('\n', UstBarSozlugu.Yogunluk("tr"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// (2)+(4)+(5) Ekranlar — Ürünler / Cariler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class Istek1EkranTestleri : BunitContext
{
    private bool yogun;
    private readonly Istek1SahteApi sunucu;

    private const string Urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true,"birim":"ADET"},
         {"id":"u2","tip":"ZIYNET","kod":"YZK-07","ad":"Tektaş Yüzük","ayarId":"K18",
          "gramAgirlik":3.2,"tasAgirlik":0,"hasGrami":2.4,"barkod":"869002","aktif":true,"birim":"ADET"}]
        """;

    private const string Cariler = """
        [{"id":"c1","tur":"MUSTERI","ad":"Ayşe Yılmaz","aktif":true,"tlBakiye":0,"hasGramBakiye":0},
         {"id":"c2","tur":"TEDARIKCI","ad":"Toptancı AŞ","aktif":true,"tlBakiye":0,"hasGramBakiye":0}]
        """;

    public Istek1EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sunucu = new Istek1SahteApi(Yanitla);
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(sunucu) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        var api = Services.GetRequiredService<ApiIstemci>();
        api.Uyu = _ => Task.CompletedTask;       // 429 beklemeleri testte anlık
        api.Jitter = _ => 0;
    }

    private (int, string, int?) Yanitla(HttpRequestMessage i)
    {
        var yol = i.RequestUri!.PathAndQuery;
        // Yoğunlukta YENİ sorgular 429 alır (süzgeç değişti); sunucu ısrarcı (60 sn).
        if (yogun && (yol.Contains("/urunler?", StringComparison.Ordinal) || yol.Contains("/cari?", StringComparison.Ordinal)))
            return (429, Istek1SahteApi.Yogun429, 60);
        return yol switch
        {
            var y when y.Contains("/urunler/stok-adetleri") =>
                (200, """{"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":42.6},{"urunId":"u2","toplamAdet":1,"toplamHasGram":2.4}]}""", null),
            var y when y.Contains("/urunler/madenler") => (200, """{"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":""}}""", null),
            var y when y.Contains("/urun-kategorileri") => (200, "[]", null),
            var y when y.Contains("/reyonlar") => (200, """[{"id":"r1","subeId":"s1","ad":"Ön Vitrin","tip":"VITRIN","aktif":true,"urunSayisi":2}]""", null),
            var y when y.Contains("/kur/pariteler") => (200, """{"pariteler":[]}""", null),
            var y when y.Contains("/kur/guncel") => (200, """{"id":"kur-1","altinHasFiyati":3000}""", null),
            var y when y.Contains("/urunler") => (200, Urunler, null),
            var y when y.Contains("/cari") => (200, Cariler, null),
            _ => (200, "[]", null),
        };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> UrunlerCiz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.DoesNotContain("…", e.Find("#u-stok-adet-u1").TextContent));
        Assert.Equal("3 adet", e.Find("#u-stok-adet-u1").TextContent.Trim());
        return e;
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: Yıldız Müşteriler'de tür süzgeci (#cari-tur-filtre) yalnız BAĞLANTIDAN etkinken görünür
    /// (tuvalde süzgeç düğmesi yok — Yildiz4DTestleri). Kullanıcının o seçiciye ulaştığı gerçek yol: /cariler?tur=….
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> CarilerCiz(string? baglanti = null)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        if (baglanti is not null)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo(baglanti);
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForAssertion(() => Assert.Contains("Ayşe Yılmaz", e.Markup));
        return e;
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-5): Urunler catch'inde listeyi boşaltan eski davranış → KIRMIZI.</summary>
    [Fact]
    public void ISTEK1_20_Urunler_suzgec_degisip_429_gelince_LISTE_EKRANDA_KALIR_bant_ustte()
    {
        var e = UrunlerCiz();
        yogun = true;

        SeciciYardim.Sec(e, "urun-durum-filtre", "false");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.StartsWith("Biraz yoğunluk var", e.Find("#hata-bandi").TextContent.Trim());
        Assert.Contains("Burma Bilezik", e.Markup);                  // tablo yerinde
        Assert.Contains("Tektaş Yüzük", e.Markup);
        Assert.Equal("3 adet", e.Find("#u-stok-adet-u1").TextContent.Trim());   // stok da silinmedi
        Assert.Empty(e.FindAll(".bos-durum"));                       // hata ≠ "kayıt yok" (UX-S28)
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-6): Cariler catch'inde listeyi boşaltan eski davranış → KIRMIZI.</summary>
    [Fact]
    public void ISTEK1_21_Cariler_Tur_suzgeci_degisip_429_gelince_LISTE_EKRANDA_KALIR()
    {
        var e = CarilerCiz("/cariler?tur=MUSTERI");
        e.WaitForElement("#cari-tur-filtre");
        yogun = true;

        SeciciYardim.Sec(e, "cari-tur-filtre", "TEDARIKCI");

        e.WaitForAssertion(() => Assert.StartsWith("Biraz yoğunluk var", e.Find("#hata-bandi").TextContent.Trim()));
        // Eldeki liste SİLİNMEZ: Yıldız satırları yerinde; "henüz müşteri yok"/"aramaya uyan yok" denmez (UX-S28).
        Assert.Contains("Ayşe Yılmaz", e.Find("#yildiz-musteri-c1").TextContent);
        Assert.Contains("Toptancı AŞ", e.Find("#yildiz-musteri-c2").TextContent);
        Assert.Empty(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll("#yildiz-musteri-bos, #yildiz-musteri-arama-bos, #yildiz-musteri-suzgec-bos"));
    }

    [Theory]
    [InlineData("urun-durum-filtre")]
    [InlineData("urun-ayar-filtre")]
    [InlineData("urun-reyon-filtre")]
    public void ISTEK1_22_Urunler_filtre_ACMAK_istek_atmaz_yalniz_SECIM_yeniler(string filtre)
    {
        var e = UrunlerCiz();
        var once = sunucu.Kayit.Count;

        e.Find($"#{filtre}").Click();                                // aç
        Assert.Equal("true", e.Find($"#{filtre}").GetAttribute("aria-expanded"));
        e.Find($"#{filtre}").Click();                                // kapat
        Assert.Equal(once, sunucu.Kayit.Count);                      // TEK istek yok

        // Seçim değişince liste yenilenir (tek /urunler isteği).
        var secenek = SeciciYardim.Secenekler(e, filtre).FirstOrDefault(o => !string.IsNullOrEmpty(o.Deger()));
        if (secenek is null) return;                                 // seçeneksiz süzgeç (ör. boş ayar listesi)
        SeciciYardim.Sec(e, filtre, secenek.Deger());
        e.WaitForAssertion(() => Assert.True(sunucu.Kayit.Skip(once).Count(k => k.Yol.StartsWith("/api/v1/urunler?", StringComparison.Ordinal)) == 1));
    }

    [Fact]
    public void ISTEK1_23_Cariler_Tur_filtresi_ACMAK_istek_atmaz()
    {
        var e = CarilerCiz("/cariler?tur=MUSTERI");
        e.WaitForElement("#cari-tur-filtre");
        var once = sunucu.Kayit.Count;

        e.Find("#cari-tur-filtre").Click();
        Assert.Equal("true", e.Find("#cari-tur-filtre").GetAttribute("aria-expanded"));
        e.Find("#cari-tur-filtre").Click();

        Assert.Equal(once, sunucu.Kayit.Count);
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-7): stok sütunu satır başına istek atarsa KIRMIZI.</summary>
    [Fact]
    public void ISTEK1_24_Urunler_stok_sutunu_TEK_toplu_istekle_dolar_N_arti_1_yok()
    {
        var e = UrunlerCiz();

        Assert.Equal("1 adet", e.Find("#u-stok-adet-u2").TextContent.Trim());
        Assert.Equal(1, sunucu.Say("GET", "/urunler/stok-adetleri"));
        Assert.Equal(0, sunucu.Kayit.Count(k => k.Yol.EndsWith("/stok", StringComparison.Ordinal)));
        var istek = sunucu.Kayit.Single(k => k.Yol.Contains("/urunler/stok-adetleri", StringComparison.Ordinal)).Yol;
        Assert.Contains("u1", Uri.UnescapeDataString(istek));
        Assert.Contains("u2", Uri.UnescapeDataString(istek));
    }

    [Fact]
    public void ISTEK1_25_Urunler_ikinci_kez_acilinca_reyon_ve_kategori_listesi_ONBELLEKTEN_gelir()
    {
        UrunlerCiz();
        var reyon = sunucu.Say("GET", "/reyonlar");
        var kategori = sunucu.Say("GET", "/urun-kategorileri");

        var e2 = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e2.WaitForAssertion(() => Assert.Equal("3 adet", e2.Find("#u-stok-adet-u1").TextContent.Trim()));

        Assert.Equal(reyon, sunucu.Say("GET", "/reyonlar"));
        Assert.Equal(kategori, sunucu.Say("GET", "/urun-kategorileri"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// (3) Üst bar — zil "?" basmaz, tek yoğunluk bandı
// ─────────────────────────────────────────────────────────────────────────────
public sealed class Istek1UstBarTestleri : BunitContext
{
    private (int, string, int?) sayiYaniti = (200, """{"sayi":3,"enYuksekOnem":"UYARI"}""", null);
    private (int, string, int?) kurYaniti = (200, """{"id":"kur-1","altinHasFiyati":3000,"usdTry":42}""", null);

    public Istek1UstBarTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var sunucu = new Istek1SahteApi(i =>
        {
            var y = i.RequestUri!.PathAndQuery;
            if (y.Contains("/bildirimler/sayi")) return sayiYaniti;
            if (y.Contains("/kur/guncel")) return kurYaniti;
            return (200, "{}", null);
        });
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(sunucu) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        var api = Services.GetRequiredService<ApiIstemci>();
        api.Uyu = _ => Task.CompletedTask;
        api.Jitter = _ => 0;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    /// <summary>MUTASYON HEDEFİ (MISTEK-8): hata rozetine "?" geri konursa KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_30_zil_sayisi_alinamazsa_SORU_ISARETI_YOK_rozet_gorunmez_sebep_ekran_okuyucuda()
    {
        var e = Render<UstBar>();
        e.WaitForAssertion(() => Assert.Equal("3", e.Find("#ust-bildirim-rozet .bildirim-sayi").TextContent.Trim()));

        sayiYaniti = (500, """{"message":"Sunucu hatası","statusCode":500}""", null);
        await e.InvokeAsync(() => e.Instance.RozetiYenile());

        Assert.Empty(e.FindAll(".bildirim-sayi"));
        Assert.DoesNotContain(">?<", e.Markup);
        Assert.Empty(e.FindAll("#ust-bildirim-rozet"));
        var gizli = e.Find("#ust-bildirim-rozet-hata");                // UX-S28: hata yutulmadı
        Assert.Contains("gorsel-gizli", gizli.ClassName);
        Assert.Contains("Sunucu hatası", gizli.TextContent);
    }

    [Fact]
    public async Task ISTEK1_31_zil_yogunlukta_SON_BILINEN_sayiyi_korur()
    {
        var e = Render<UstBar>();
        e.WaitForAssertion(() => Assert.Equal("3", e.Find("#ust-bildirim-rozet .bildirim-sayi").TextContent.Trim()));

        // Son iyi yanıt yedeği YOK (bir yazma onu düşürdü) → istemci OranSinirHatasi fırlatır;
        // rozet yine de son bilinen sayıyı korur, "?" ya da hata çizmez.
        Services.GetRequiredService<ApiIstemci>().Onbellek.YazmaOldu();
        sayiYaniti = (429, Istek1SahteApi.Yogun429, 60);
        await e.InvokeAsync(() => e.Instance.RozetiYenile());

        Assert.Equal("3", e.Find("#ust-bildirim-rozet .bildirim-sayi").TextContent.Trim());
        Assert.Empty(e.FindAll("#ust-bildirim-rozet-hata"));
    }

    /// <summary>
    /// MUTASYON HEDEFİ (MISTEK-12): saat, TEK bir geçici hata (ör. yoğunluk) varken
    /// kur hâlâ tazeyse KIRMIZI çerçeveye dönmez (GOZ #19/#48/#54: sayfadan sayfaya
    /// "kırmızı hap" görünüyordu); sebep ipucunda yazar. Kur gerçekten yoksa kırmızı
    /// kalır (S1cUstBarTestleri.Kur_alinamazsa_serit_BOS_KALMAZ_hata_gorunur_UX_S28).
    /// </summary>
    [Fact]
    public async Task ISTEK1_33_saat_gecici_hatada_kur_tazeyken_KIRMIZI_olmaz_sebep_ipucunda()
    {
        var e = Render<UstBar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-kur-has")));
        Assert.DoesNotContain("hatali", e.Find("#ust-kur-saat").ClassName ?? "");

        kurYaniti = (500, """{"message":"Kur sağlayıcısı yanıt vermedi","statusCode":500}""", null);
        var canli = Services.GetRequiredService<CanliKurServisi>();
        await e.InvokeAsync(() => canli.TazeleAsync());

        Assert.NotNull(canli.SonHata);
        Assert.False(canli.Bayat);
        e.WaitForAssertion(() => Assert.StartsWith("Son sorgu başarısız", e.Find("#ust-kur-saat").GetAttribute("title") ?? ""));
        Assert.DoesNotContain("hatali", e.Find("#ust-kur-saat").ClassName ?? "");
        Assert.NotEmpty(e.FindAll("#ust-kur-has"));                    // son bilinen kur ekranda
    }

    [Fact]
    public async Task ISTEK1_32_yogunlukta_TEK_sade_bant_acilir_ve_kendiliginden_kapanir()
    {
        var e = Render<UstBar>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ust-bildirim-rozet")));
        Assert.Empty(e.FindAll("#ust-yogunluk-bandi"));

        // Az önce başarıyla gelen sayı yolu şimdi ısrarlı 429: son iyi yanıt döner, bant açılır.
        sayiYaniti = (429, Istek1SahteApi.Yogun429, 60);
        var api = Services.GetRequiredService<ApiIstemci>();
        await e.InvokeAsync(async () => await api.GetAsync("/bildirimler/sayi"));

        e.WaitForAssertion(() => Assert.Single(e.FindAll("#ust-yogunluk-bandi")));
        var bant = e.Find("#ust-yogunluk-bandi");
        Assert.Equal(UstBarSozlugu.Yogunluk("tr"), bant.TextContent.Trim());
        Assert.Equal("status", bant.GetAttribute("role"));

        sayiYaniti = (200, """{"sayi":3,"enYuksekOnem":"UYARI"}""", null);
        await e.InvokeAsync(() => e.Instance.RozetiYenile());
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#ust-yogunluk-bandi")));
    }
}

/// <summary>Elle ilerletilen saat (CanliKurServisi TimeProvider kancası).</summary>
internal sealed class Istek1Saat : TimeProvider
{
    public DateTimeOffset An { get; set; } = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => An;
}

/// <summary>
/// Doğrulayıcı bulgusu (ORTA, hakem çelişkisi): ısrarlı 429'da canlı kur servisi eski
/// kuru "yeni sorgulandı" gibi işaretliyordu (SonHata=null, Bayat=False). Artık 429
/// kurda HATA kalır: SonHata yazılır, son başarılı okuma İLERLEMEZ, eşik dolunca Bayat.
/// </summary>
public sealed class Istek1KurTestleri : BunitContext
{
    private (int, string, int?) kurYaniti = (200, """{"id":"kur-1","altinHasFiyati":3000,"usdTry":42}""", null);
    private readonly Istek1Saat saat = new();

    /// <summary>MUTASYON HEDEFİ (MISTEK-17): TazeGetAsync kurda son-iyi yedeğine düşerse KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_34_israrli_429_da_kur_SonHata_yazar_basarili_okuma_ilerlemez_esikte_BAYAT()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var sunucu = new Istek1SahteApi(_ => kurYaniti);
        Services.AddSingleton<TimeProvider>(saat);
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(sunucu) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        await Services.GetRequiredService<OturumServisi>().TokenKaydetAsync("access-1", "refresh-1");
        var api = Services.GetRequiredService<ApiIstemci>();
        api.Uyu = _ => Task.CompletedTask;
        api.Jitter = _ => 0;
        api.Simdi = () => saat.An;
        api.Onbellek.Simdi = () => saat.An;
        var canli = Services.GetRequiredService<CanliKurServisi>();

        await canli.TazeleAsync();                                   // başarılı kur
        Assert.Null(canli.SonHata);
        Assert.Equal("kur-1", canli.Son!.Id);

        kurYaniti = (429, Istek1SahteApi.Yogun429, 60);             // ısrarlı yoğunluk
        saat.An = saat.An.AddSeconds(5);
        await canli.TazeleAsync();
        Assert.NotNull(canli.SonHata);                               // eski kur "taze" işaretlenmedi
        Assert.False(canli.Bayat);                                   // henüz eşik dolmadı — telaş yok

        // Yoklama gibi tekrarlar: eşik (3 aralık) dolunca kur BAYAT — son başarılı
        // okuma yoğunluk boyunca İLERLEMEDİ.
        for (var i = 0; i < 4; i++)
        {
            saat.An = saat.An.Add(canli.Aralik);
            await canli.TazeleAsync();
        }
        Assert.NotNull(canli.SonHata);
        Assert.True(canli.Bayat);
        Assert.Equal("kur-1", canli.Son!.Id);                        // son bilinen kur ekranda kalır
        await Assert.ThrowsAsync<OranSinirHatasi>(() => canli.AlAsync());   // yazma yolu bayat kur ALAMAZ
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// (6) HTTP — sınırlar ve toplu stok ucu
// ─────────────────────────────────────────────────────────────────────────────
[Collection("http")]
public sealed class Istek1HttpTestleri(HttpFikstur f)
{
    private static int ipSayac;
    private static string YeniIp() => $"198.51.100.{Interlocked.Increment(ref ipSayac) % 250 + 1}";

    /// <summary>MUTASYON HEDEFİ (MISTEK-9): genel sınır 300'den farklıysa KIRMIZI.</summary>
    [Fact]
    public async Task ISTEK1_40_genel_sinir_dakikada_300_istek_gecer_301_inci_429()
    {
        Assert.Equal(300, OranSiniri.GenelDakikaSiniri);
        var istemci = f.Ortam.AnonimIstemci(YeniIp());

        var kodlar = new List<int>();
        for (var i = 0; i < OranSiniri.GenelDakikaSiniri; i++)
            kodlar.Add((int)(await istemci.GetAsync("/saglik")).StatusCode);

        Assert.DoesNotContain(429, kodlar);                          // 1..300 geçer (299 dahil)
        var fazla = await istemci.GetAsync("/saglik");                // 301.
        Assert.Equal(HttpStatusCode.TooManyRequests, fazla.StatusCode);
        // Kayan pencere reddinde de Retry-After gider (≤ bir dilim) — istemci bir kez bekleyebilsin.
        Assert.NotNull(fazla.Headers.RetryAfter?.Delta);
        Assert.InRange(fazla.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, OranSiniri.GenelDilimSaniye);
        var govde = await fazla.Json();
        Assert.Equal(429, govde.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task ISTEK1_41_giris_siniri_DEGISMEDI_dakikada_5_altinci_429()
    {
        var istemci = f.Ortam.AnonimIstemci(YeniIp());
        var eposta = $"istek1-{Guid.NewGuid():N}@test.local";

        var kodlar = new List<int>();
        for (var i = 0; i < 6; i++)
            kodlar.Add((int)(await istemci.PostJson("/api/v1/auth/giris", new { email = eposta, sifre = "gecersizsifre" })).StatusCode);

        Assert.DoesNotContain(429, kodlar.Take(5));
        Assert.Equal(429, kodlar[5]);
    }

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad, stokAdet: 7m);
    }

    [Fact]
    public async Task ISTEK1_42_toplu_stok_ucu_subeleri_toplar_stoksuz_urun_0_yabanci_kimlik_YOK()
    {
        var k = YeniKiraci("ISTEK1 toplu stok");
        var yabanci = YeniKiraci("ISTEK1 yabancı");
        string stoksuz;
        using (var db = f.Vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            var sube2 = Kuyumcu.Api.Common.Kimlik.Ulid();
            db.Subeler.Add(new Sube { Id = sube2, TenantId = k.TenantId, Ad = "Şube 2", Aktif = true, Olusturuldu = now, Guncellendi = now });
            db.Stoklar.Add(new Stok
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube2, UrunId = k.UrunId,
                Adet = 2.5m, ToplamHasGram = 2.5m * k.UrunHasGrami, Guncellendi = now,
            });
            stoksuz = Kuyumcu.Api.Common.Kimlik.Ulid();
            db.Urunler.Add(new Urun
            {
                Id = stoksuz, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"STOKSUZ-{stoksuz[^10..]}", Ad = "Stoksuz",
                AyarId = "K22", Milyem = 916, GramAgirlik = 1m, TasAgirlik = 0m, HasGrami = 0.916m,
                IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            await db.SaveChangesAsync();
        }

        var c = await f.Ortam.Istemci(k).GetAsync(
            $"/api/v1/urunler/stok-adetleri?ids={k.UrunId},{stoksuz},{yabanci.UrunId},YOK");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var urunler = (await c.Json()).GetProperty("urunler").EnumerateArray().ToList();

        Assert.Equal(2, urunler.Count);                              // yabancı + olmayan YOK
        var ana = urunler.Single(u => u.GetProperty("urunId").GetString() == k.UrunId);
        Assert.Equal(9.5m, ana.GetProperty("toplamAdet").GetDecimal());  // 7 + 2,5 (iki şube)
        Assert.Equal(0m, urunler.Single(u => u.GetProperty("urunId").GetString() == stoksuz).GetProperty("toplamAdet").GetDecimal());
        Assert.DoesNotContain(urunler, u => u.GetProperty("urunId").GetString() == yabanci.UrunId);

        // Tek tek uçla AYNI hesap.
        var tek = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/urunler/{k.UrunId}/stok")).Json();
        Assert.Equal(tek.GetProperty("toplamAdet").GetDecimal(), ana.GetProperty("toplamAdet").GetDecimal());
    }

    [Fact]
    public async Task ISTEK1_43_toplu_stok_ucu_bos_ve_sinir_ustu_400_kimliksiz_401()
    {
        var k = YeniKiraci("ISTEK1 toplu stok sınır");
        var istemci = f.Ortam.Istemci(k);

        Assert.Equal(HttpStatusCode.BadRequest, (await istemci.GetAsync("/api/v1/urunler/stok-adetleri")).StatusCode);
        var cok = string.Join(",", Enumerable.Range(0, 501).Select(i => $"U{i:D5}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await istemci.GetAsync($"/api/v1/urunler/stok-adetleri?ids={cok}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await f.Ortam.AnonimIstemci(YeniIp()).GetAsync($"/api/v1/urunler/stok-adetleri?ids={k.UrunId}")).StatusCode);
    }
}
