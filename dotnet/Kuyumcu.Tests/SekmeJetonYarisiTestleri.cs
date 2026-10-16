using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Keşif B006 (EMANET-1 son doğrulama, orkestratörce KRİTİK) — İKİ SEKME JETON YARIŞI
//
// Aynı kullanıcı iki/üç sekmede: erişim süresi dolunca her sekme bellekteki AYNI
// eski yenileme jetonuyla /auth/yenile çağırıyordu. Sunucu (doğru olarak, RFC 9700
// §4.14.2) ikinci kullanımı çalıntı sayıp TÜM aileyi iptal ediyor, bütün sekmeler
// düşüyordu. Bu testlerde her "sekme" ayrı bir DI kapsamıdır (ayrı OturumServisi +
// ApiIstemci, ayrı bellek); PAYLAŞILAN tek şey tarayıcının localStorage'ı ve
// tarayıcı geneli kilittir (navigator.locks karşılığı). Sahte sunucu, gerçek
// sunucunun rotasyon + çalıntı tespiti kuralını BİREBİR uygular.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SekmeJetonYarisiTestleri
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task B006_Cok_sekme_ayni_anda_sure_dolumunda_hicbiri_dusmez(int sekmeSayisi)
    {
        var tarayici = new SahteTarayici();
        var sunucu = new KatiSunucu();
        var ilk = sunucu.IlkAccess(Kisi.Ali);
        tarayici.Depo["kuyumcu-access"] = ilk;
        tarayici.Depo["kuyumcu-refresh"] = "refresh-0";
        sunucu.AccessSuresiDoldu(ilk);

        var sekmeler = Enumerable.Range(0, sekmeSayisi).Select(_ => Sekme.Ac(tarayici, sunucu)).ToList();
        foreach (var s in sekmeler) await s.Oturum.YukleAsync();

        // Her sekme aynı anda korumalı bir istek atar; hepsi 401 görür.
        var sonuclar = await Task.WhenAll(sekmeler.Select(s => Task.Run(async () =>
        {
            try { await s.Api.GetAsync("/kur/guncel"); return "ok"; }
            catch (Exception e) { return e.GetType().Name + ": " + e.Message; }
        })));

        Assert.All(sonuclar, r => Assert.Equal("ok", r));
        Assert.False(sunucu.AileIptalEdildi, "Hiçbir sekme eski jetonu yeniden kullanmamalıydı");
        Assert.Equal(1, sunucu.YenilemeSayisi);
        Assert.Equal(0, sunucu.CikisSayisi);
        Assert.All(sekmeler, s => Assert.True(s.Oturum.GirisYapildi));
        Assert.All(sekmeler, s => Assert.DoesNotContain("/giris", s.Nav.Uri));
        // Bütün sekmeler depodaki TEK güncel çifti taşır.
        Assert.All(sekmeler, s => Assert.Equal(tarayici.Depo["kuyumcu-refresh"], s.Oturum.RefreshToken));
    }

    [Fact]
    public async Task B006_Baska_sekme_cikis_yaptiysa_bu_sekme_de_duser_ve_yeniden_kullanim_iptali_korunur()
    {
        var tarayici = new SahteTarayici();
        var sunucu = new KatiSunucu();
        var ilk = sunucu.IlkAccess(Kisi.Ali);
        tarayici.Depo["kuyumcu-access"] = ilk;
        tarayici.Depo["kuyumcu-refresh"] = "refresh-0";

        var a = Sekme.Ac(tarayici, sunucu);
        var b = Sekme.Ac(tarayici, sunucu);
        await a.Oturum.YukleAsync();
        await b.Oturum.YukleAsync();

        await a.Oturum.CikisAsync();          // A çıkış yapar: sunucuda refresh-0 iptal, depo boş
        sunucu.AccessSuresiDoldu(ilk);

        await Assert.ThrowsAsync<OturumDustuHatasi>(() => b.Api.GetAsync("/kur/guncel"));
        Assert.False(b.Oturum.GirisYapildi);
        Assert.EndsWith("/giris", b.Nav.Uri);
    }

    [Fact]
    public async Task B006_Sahte_sunucu_gercek_sunucu_kuralini_uygular_iptal_jetonu_aileyi_duser()
    {
        // Güvenlik kuralının istemci testlerinde de canlı olduğunun kanıtı:
        // kilit olmadan (tek sekme, iki ayrı çağrı) eski jeton ikinci kez gelirse aile düşer.
        var sunucu = new KatiSunucu();
        using var http = new HttpClient(sunucu) { BaseAddress = new Uri("http://test.local/") };
        var ilk = await http.PostAsync("api/v1/auth/yenile", Json(new { refreshToken = "refresh-0" }));
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
        var ikinci = await http.PostAsync("api/v1/auth/yenile", Json(new { refreshToken = "refresh-0" }));
        Assert.Equal(HttpStatusCode.Unauthorized, ikinci.StatusCode);
        Assert.True(sunucu.AileIptalEdildi);
    }

    [Fact]
    public void B006_index_kilit_betigini_Blazordan_ONCE_yukler_ve_betik_navigator_locks_kullanir()
    {
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html");
        var kilit = html.IndexOf("<script src=\"js/oturum-kilidi.js\"></script>", StringComparison.Ordinal);
        var blazor = html.IndexOf("_framework/blazor.webassembly.js", StringComparison.Ordinal);
        Assert.True(kilit > 0 && kilit < blazor, "oturum-kilidi.js Blazor'dan önce yüklenmeli");

        var js = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "oturum-kilidi.js"));
        Assert.Contains("window.kuyumcuOturumKilidi", js);
        Assert.Contains("navigator.locks.request", js);
        Assert.Contains("birak:", js);
        Assert.DoesNotContain("eval(", js);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GÜVENLİK (orkestratör 30.09 05:52, KRİTİK): depodaki çift YALNIZ aynı kullanıcı
    // + aynı kiracıya aitse benimsenir. Ortak bilgisayar: sekme 1'de Ali çıkar (ya da
    // çıkmadan) Veli girer; Ali'nin sekme 2'si Veli'nin çiftini BENİMSEMEZ, Veli
    // jetonuyla HİÇBİR istek göndermez, yerel oturumunu (yalnız bellekte) bırakır ve
    // nedeniyle /giris'e düşer. Veli'nin oturumu (depo) bozulmaz.
    // ═════════════════════════════════════════════════════════════════════════
    [Theory]
    [InlineData(true, false)]    // Ali çıktı, Veli girdi (aynı kiracı)
    [InlineData(false, false)]   // Ali çıkmadan Veli girdi (Ali'nin ailesi hâlâ geçerli)
    [InlineData(true, true)]     // aynı kullanıcı kimliği ama BAŞKA kiracı
    public async Task B006_Guvenlik_baska_kullanicinin_ciftini_benimsemez_onun_jetonuyla_istek_gondermez_nedeniyle_duser(
        bool aliCikti, bool baskaKiraci)
    {
        var tarayici = new SahteTarayici();
        var sunucu = new KatiSunucu();
        var aliAccess = sunucu.IlkAccess(Kisi.Ali);
        tarayici.Depo["kuyumcu-access"] = aliAccess;
        tarayici.Depo["kuyumcu-refresh"] = "refresh-0";

        var sekme1 = Sekme.Ac(tarayici, sunucu);
        var sekme2 = Sekme.Ac(tarayici, sunucu);
        await sekme1.Oturum.YukleAsync();
        await sekme2.Oturum.YukleAsync();
        sekme2.Oturum.KullaniciAta(new OturumKullanici("ali", "Ali", "Tezgah", "ali@ornek.com", "TEZGAHTAR", "sube-1"));

        if (aliCikti) await sekme1.Oturum.CikisAsync();
        var veli = baskaKiraci ? Kisi.Ali with { TenantId = "kiraci-2", Rol = "PATRON" } : Kisi.Veli;
        var (veliAccess, veliRefresh) = sunucu.Giris(veli);
        await sekme1.Oturum.TokenKaydetAsync(veliAccess, veliRefresh);   // sekme 1 artık Veli
        sunucu.AccessSuresiDoldu(aliAccess);                            // Ali'nin erişimi doldu

        var yenilemeOnce = sunucu.YenilemeSayisi;
        var hata = await Assert.ThrowsAsync<OturumDustuHatasi>(() => sekme2.Api.GetAsync("/emanet/ozet"));

        // Veli'nin jetonuyla HİÇBİR istek gitmedi; sekme 2 sunucuda yenileme de denemedi.
        Assert.DoesNotContain(veliAccess, sunucu.KorumaliIstekJetonlari);
        Assert.Equal(yenilemeOnce, sunucu.YenilemeSayisi);
        Assert.False(sunucu.AileIptalEdildi);
        // Sekme 2 düştü ve SESSİZ değil: giriş ekranına nedeni taşınır.
        Assert.False(sekme2.Oturum.GirisYapildi);
        Assert.Null(sekme2.Oturum.Kullanici);
        Assert.NotEqual(veliRefresh, sekme2.Oturum.RefreshToken);
        Assert.EndsWith("/giris", sekme2.Nav.Uri);
        Assert.Equal(OturumServisi.BildirimBaskaKullaniciKodu, sekme2.Oturum.GirisBildirimi);
        Assert.NotNull(hata);
        // Sonraki istek de gönderilmez (oturum düşürüldü).
        var sayac = sunucu.KorumaliIstekJetonlari.Count;
        await Assert.ThrowsAsync<OturumDustuHatasi>(() => sekme2.Api.GetAsync("/kasa"));
        Assert.Equal(sayac, sunucu.KorumaliIstekJetonlari.Count);
        // Veli'nin oturumu bozulmadı: depo aynı, çıkış yapılmadı, Veli çalışıyor.
        Assert.Equal(veliRefresh, tarayici.Depo["kuyumcu-refresh"]);
        Assert.Equal(veliAccess, tarayici.Depo["kuyumcu-access"]);
        Assert.Equal(aliCikti ? 1 : 0, sunucu.CikisSayisi);
        await sekme1.Api.GetAsync("/emanet/ozet");
    }

    [Fact]
    public async Task B006_Guvenlik_ayni_kullanici_benimseyince_rol_ve_sube_yeni_jetondan_yenilenir()
    {
        var tarayici = new SahteTarayici();
        var sunucu = new KatiSunucu();
        var ilk = sunucu.IlkAccess(Kisi.Ali);
        tarayici.Depo["kuyumcu-access"] = ilk;
        tarayici.Depo["kuyumcu-refresh"] = "refresh-0";

        var sekme1 = Sekme.Ac(tarayici, sunucu);
        var sekme2 = Sekme.Ac(tarayici, sunucu);
        await sekme1.Oturum.YukleAsync();
        await sekme2.Oturum.YukleAsync();
        sekme1.Oturum.KullaniciAta(new OturumKullanici("ali", "Ali", "Tezgah", "ali@ornek.com", "TEZGAHTAR", "sube-1"));
        sekme2.Oturum.KullaniciAta(new OturumKullanici("ali", "Ali", "Tezgah", "ali@ornek.com", "TEZGAHTAR", "sube-1"));
        sunucu.AccessSuresiDoldu(ilk);
        sunucu.SonrakiRol = "MUDUR";                 // patron Ali'yi müdür yaptı, şubesini değiştirdi

        await sekme1.Api.GetAsync("/emanet/ozet");   // sekme 1 sunucuda yeniler (yeni rol)
        await sekme2.Api.GetAsync("/emanet/ozet");   // sekme 2 depodakini benimser

        Assert.Equal(1, sunucu.YenilemeSayisi);
        Assert.Equal(tarayici.Depo["kuyumcu-refresh"], sekme2.Oturum.RefreshToken);
        // Sunucuda yenileyen sekme de (sekme 1) yeni rolü gösterir — iki sekme aynı rolde.
        Assert.Equal("MUDUR", sekme1.Oturum.Kullanici!.Rol);
        Assert.Equal("sube-2", sekme1.Oturum.Kullanici.SubeId);
        Assert.Equal("MUDUR", sekme2.Oturum.Kullanici!.Rol);
        Assert.Equal("sube-2", sekme2.Oturum.Kullanici.SubeId);
        Assert.Equal("Ali", sekme2.Oturum.Kullanici.Ad);
        Assert.Null(sekme2.Oturum.GirisBildirimi);
        Assert.DoesNotContain("/giris", sekme2.Nav.Uri);
    }

    [Fact]
    public async Task B006_Guvenlik_okunamayan_jeton_fail_closed_benimsenmez()
    {
        var tarayici = new SahteTarayici();
        var sunucu = new KatiSunucu();
        var ilk = sunucu.IlkAccess(Kisi.Ali);
        tarayici.Depo["kuyumcu-access"] = ilk;
        tarayici.Depo["kuyumcu-refresh"] = "refresh-0";
        var sekme = Sekme.Ac(tarayici, sunucu);
        await sekme.Oturum.YukleAsync();
        sunucu.AccessSuresiDoldu(ilk);
        tarayici.Depo["kuyumcu-access"] = "bozuk-jeton";   // kimliği okunamayan çift
        tarayici.Depo["kuyumcu-refresh"] = "refresh-x";

        await Assert.ThrowsAsync<OturumDustuHatasi>(() => sekme.Api.GetAsync("/emanet/ozet"));
        Assert.DoesNotContain("bozuk-jeton", sunucu.KorumaliIstekJetonlari);
        Assert.Equal(0, sunucu.YenilemeSayisi);
        Assert.EndsWith("/giris", sekme.Nav.Uri);
    }

    /// <summary>Test kişisi = erişim jetonundaki iddialar (TokenServisi.AccessUret ile aynı adlar).</summary>
    internal sealed record Kisi(string Sub, string TenantId, string Rol, string SubeId)
    {
        public static readonly Kisi Ali = new("ali", "kiraci-1", "TEZGAHTAR", "sube-1");
        public static readonly Kisi Veli = new("veli", "kiraci-1", "PATRON", "sube-1");
    }

    /// <summary>İmzasız JWT biçimi (başlık.yük.imza). İstemci yalnız yükten kimliği okur.</summary>
    internal static string Jwt(Kisi k, int n)
    {
        static string B64(string m) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(m))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var yuk = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["sub"] = k.Sub, ["tenantId"] = k.TenantId, ["subeId"] = k.SubeId, ["rol"] = k.Rol, ["n"] = n,
        });
        return B64("""{"alg":"HS256","typ":"JWT"}""") + "." + B64(yuk) + ".imza" + n;
    }

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), System.Text.Encoding.UTF8, "application/json");

    // ── Sekme = ayrı DI kapsamı ─────────────────────────────────────────────────
    private sealed record Sekme(OturumServisi Oturum, ApiIstemci Api, TestNav Nav)
    {
        public static Sekme Ac(SahteTarayici tarayici, KatiSunucu sunucu)
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<IJSRuntime>(tarayici);
            var nav = new TestNav();
            sc.AddSingleton<NavigationManager>(nav);
            sc.AddScoped(_ => new HttpClient(sunucu, disposeHandler: false) { BaseAddress = new Uri("http://test.local/") });
            sc.AddScoped<OturumServisi>();
            sc.AddScoped<ApiIstemci>();
            var sp = sc.BuildServiceProvider().CreateScope().ServiceProvider;
            return new Sekme(sp.GetRequiredService<OturumServisi>(), sp.GetRequiredService<ApiIstemci>(), nav);
        }
    }

    private sealed class TestNav : NavigationManager
    {
        public TestNav() => Initialize("http://test.local/", "http://test.local/");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }

    /// <summary>Paylaşılan localStorage + tarayıcı geneli kilit (navigator.locks karşılığı).</summary>
    private sealed class SahteTarayici : IJSRuntime
    {
        public readonly ConcurrentDictionary<string, string> Depo = new();
        private readonly SemaphoreSlim kilit = new(1, 1);
        private int sayac;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? sonuc = null;
            switch (identifier)
            {
                case "localStorage.getItem":
                    sonuc = Depo.TryGetValue((string)args![0]!, out var d) ? d : null;
                    break;
                case "localStorage.setItem":
                    Depo[(string)args![0]!] = (string)args[1]!;
                    break;
                case "localStorage.removeItem":
                    Depo.TryRemove((string)args![0]!, out _);
                    break;
                case "kuyumcuOturumKilidi.al":
                    await kilit.WaitAsync(cancellationToken);
                    sonuc = "k" + Interlocked.Increment(ref sayac);
                    break;
                case "kuyumcuOturumKilidi.birak":
                    kilit.Release();
                    break;
            }
            await Task.Yield();
            return sonuc is null ? default! : (TValue)sonuc;
        }
    }

    /// <summary>
    /// Gerçek AuthModulu.Yenile kuralı: geçerli jeton → yeni çift, eski iptal;
    /// iptal edilmiş jeton yeniden gelirse TÜM aile iptal (çalıntı tespiti).
    /// </summary>
    private sealed class KatiSunucu : HttpMessageHandler
    {
        private readonly object k = new();
        // refresh → sahibi (kişi). Gerçek sunucuda refresh_tokenler.kullanici_id.
        private readonly Dictionary<string, Kisi> gecerliRefresh = new() { ["refresh-0"] = Kisi.Ali };
        private readonly HashSet<string> iptalRefresh = [];
        private readonly HashSet<string> gecerliAccess = ["access-0"];
        private int uretim;
        public bool AileIptalEdildi { get; private set; }
        public int YenilemeSayisi { get; private set; }
        public int CikisSayisi { get; private set; }
        /// <summary>Korumalı uca gelen her isteğin erişim jetonu (API günlüğündeki istek kimliği).</summary>
        public ConcurrentQueue<string> KorumaliIstekJetonlari { get; } = new();
        /// <summary>Sonraki yenilemede verilecek rol (başka yerde rol değişti senaryosu).</summary>
        public string? SonrakiRol { get; set; }

        public void AccessSuresiDoldu(string access) { lock (k) gecerliAccess.Remove(access); }

        /// <summary>İlk çift (refresh-0) için JWT biçimli erişim jetonu (Ali).</summary>
        public string IlkAccess(Kisi kisi) { lock (k) { var a = Jwt(kisi, 0); gecerliAccess.Add(a); return a; } }

        /// <summary>Başka bir sekmede /auth/giris: bu kişi için yeni bir aile açılır.</summary>
        public (string Access, string Refresh) Giris(Kisi kisi)
        {
            lock (k)
            {
                uretim++;
                var a = Jwt(kisi, uretim);
                var r = "refresh-" + uretim;
                gecerliAccess.Add(a);
                gecerliRefresh[r] = kisi;
                return (a, r);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            string? govde = istek.Content is null ? null : await istek.Content.ReadAsStringAsync(iptal);
            // Gerçek eşzamanlılık: sekmelerin 401'i ve yenilemeyi AYNI ANDA görmesi için.
            await Task.Delay(40, iptal);

            if (yol.EndsWith("/auth/yenile"))
            {
                var jeton = JsonDocument.Parse(govde!).RootElement.GetProperty("refreshToken").GetString()!;
                lock (k)
                {
                    YenilemeSayisi++;
                    if (iptalRefresh.Contains(jeton))
                    {
                        AileIptalEdildi = true;
                        foreach (var g in gecerliRefresh.Keys) iptalRefresh.Add(g);
                        gecerliRefresh.Clear();
                        gecerliAccess.Clear();
                        return Yetkisiz();
                    }
                    if (!gecerliRefresh.Remove(jeton, out var sahibi)) return Yetkisiz();
                    iptalRefresh.Add(jeton);
                    uretim++;
                    if (SonrakiRol is not null) sahibi = sahibi with { Rol = SonrakiRol, SubeId = "sube-2" };
                    var yeniA = Jwt(sahibi, uretim);
                    var yeniR = "refresh-" + uretim;
                    gecerliAccess.Add(yeniA);
                    gecerliRefresh[yeniR] = sahibi;
                    return Tamam($$"""{"accessToken":"{{yeniA}}","refreshToken":"{{yeniR}}"}""");
                }
            }
            if (yol.EndsWith("/auth/cikis"))
            {
                var jeton = JsonDocument.Parse(govde!).RootElement.GetProperty("refreshToken").GetString()!;
                lock (k)
                {
                    CikisSayisi++;
                    if (gecerliRefresh.Remove(jeton, out _)) iptalRefresh.Add(jeton);
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            var access = istek.Headers.Authorization?.Parameter;
            if (access is not null) KorumaliIstekJetonlari.Enqueue(access);
            lock (k)
            {
                if (access is null || !gecerliAccess.Contains(access)) return Yetkisiz();
            }
            return Tamam("""{"ok":true}""");
        }

        private static HttpResponseMessage Yetkisiz() => new(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"message":"Yetkisiz","statusCode":401}""", System.Text.Encoding.UTF8, "application/json"),
        };

        private static HttpResponseMessage Tamam(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}

/// <summary>
/// Keşif B006 güvenliği — ekran tarafı: başka kullanıcı yüzünden düşen sekme giriş
/// ekranına SESSİZ gelmez (nedeni HataBandi'nda), ve aynı sekmeden yeniden giriş
/// yapılabilir (düşmüş sekme anonim giriş uçlarını engellemez).
/// </summary>
public sealed class SekmeJetonGuvenlikEkranTestleri : Bunit.BunitContext
{
    private readonly ConcurrentQueue<(string Yol, string? Jeton)> istekler = new();

    private sealed class Sunucu(ConcurrentQueue<(string, string?)> kayit, string veliAccess) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var jeton = istek.Headers.Authorization?.Parameter;
            kayit.Enqueue((yol, jeton));
            if (yol.EndsWith("/genel/kisitlar"))
                return Task.FromResult(Yanit(HttpStatusCode.OK, """{"kaynak":"test","adet":0,"kisitlar":[]}"""));
            if (yol.EndsWith("/auth/giris"))
                return Task.FromResult(Yanit(HttpStatusCode.OK,
                    $$$"""{"accessToken":"{{{veliAccess}}}","refreshToken":"r-veli-2","kullanici":{"id":"veli","ad":"Veli","soyad":"P","email":"v@ornek.com","rol":"PATRON","subeId":"sube-1"}}"""));
            if (jeton == veliAccess) return Task.FromResult(Yanit(HttpStatusCode.OK, """{"ok":true}"""));
            return Task.FromResult(Yanit(HttpStatusCode.Unauthorized, """{"message":"Yetkisiz","statusCode":401}"""));
        }

        private static HttpResponseMessage Yanit(HttpStatusCode k, string g) =>
            new(k) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
    }

    [Fact]
    public async Task B006_Guvenlik_baska_kullanici_yuzunden_dusen_sekme_giriste_nedeni_gorur_ve_yeniden_girebilir()
    {
        var ali = SekmeJetonYarisiTestleri.Jwt(SekmeJetonYarisiTestleri.Kisi.Ali, 0);
        var veli = SekmeJetonYarisiTestleri.Jwt(SekmeJetonYarisiTestleri.Kisi.Veli, 1);
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        // Depo (localStorage) artık Veli'nin: sekme 1'de Veli giriş yaptı.
        JSInterop.Setup<string?>("localStorage.getItem", "kuyumcu-access").SetResult(veli);
        JSInterop.Setup<string?>("localStorage.getItem", "kuyumcu-refresh").SetResult("r-veli");
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sunucu(istekler, veli)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();

        var oturum = Services.GetRequiredService<OturumServisi>();
        var api = Services.GetRequiredService<ApiIstemci>();
        await oturum.TokenKaydetAsync(ali, "r-ali");   // bu sekmenin belleği: Ali (erişimi dolmuş)

        await Assert.ThrowsAsync<OturumDustuHatasi>(() => api.GetAsync("/emanet/ozet"));
        Assert.DoesNotContain(istekler, i => i.Jeton == veli);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/auth/"));
        // localStorage'daki Veli çifti SİLİNMEDİ (onun oturumu düşmesin).
        Assert.Empty(JSInterop.Invocations["localStorage.removeItem"]);

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/giris", nav.Uri);
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        e.WaitForAssertion(() => Assert.Contains(
            Kuyumcu.Core.I18n.GirisSozlugu.BaskaKullaniciOturumActi("tr"), e.Markup));
        // Canlıda giriş ekranı kabuk + istemci yönlendirmesiyle İKİ kez çizilebiliyor (t41 canlı
        // bulgusu): bildirim ilk çizimde tüketilmemeli, ikinci çizim de nedeni göstermeli.
        var e2 = Render<Kuyumcu.Web.Pages.Giris>();
        e2.WaitForAssertion(() => Assert.Contains(
            Kuyumcu.Core.I18n.GirisSozlugu.BaskaKullaniciOturumActi("tr"), e2.Markup));

        // Aynı sekmeden yeniden giriş: istek gider, oturum Veli olarak açılır, sonraki istek geçer.
        await api.PostAsync<System.Text.Json.JsonElement>("/auth/giris", new { email = "v@ornek.com", sifre = "x" });
        Assert.Contains(istekler, i => i.Yol.EndsWith("/auth/giris"));
        await oturum.TokenKaydetAsync(veli, "r-veli-2");
        await api.GetAsync("/emanet/ozet");
        Assert.Contains(istekler, i => i.Yol.EndsWith("/emanet/ozet") && i.Jeton == veli);
        // Yeni oturum açılınca bildirim kalkar: sonraki giriş ekranı eski nedeni göstermez.
        Assert.Null(oturum.GirisBildirimi);
        var e3 = Render<Kuyumcu.Web.Pages.Giris>();
        Assert.DoesNotContain(Kuyumcu.Core.I18n.GirisSozlugu.BaskaKullaniciOturumActi("tr"), e3.Markup);
    }
}
