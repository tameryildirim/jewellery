using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// HTTP BORU HATTI ORTAMI — PLAN-2 Adım 0a (Aşkın'ın kalite kapısı K2).
//
// NEDEN VAR: 2026-09-13 ölçümü — test projesinde `Mvc.Testing` yoktu,
// `WebApplicationFactory`/`HttpClient` 0 eşleşme veriyordu. Yani 321 testin
// hiçbiri GERÇEK bir HTTP isteği yapmıyordu. Servis testi şunları göremez:
//   • middleware SIRASI          • kimlik doğrulama / yetkilendirme
//   • oran sınırı (429)          • JSON sözleşmesi (camelCase)
//   • model bağlama / 400 biçimi • DI kapsamı (scope) davranışı
//
// GEÇMİŞTEKİ İKİ HATA TAM BURADAN GEÇTİ:
//   1. SistemKapsami çocuk kapsamı HttpContext'e eziliyordu → giriş 500
//   2. MapControllers().RequireRateLimiting uç niteliklerini eziyordu → 429 hiç dönmedi
// İkisi de ~300 yeşil servis testinden geçmişti. Bu dosya o kör noktayı kapatır.
//
// VERİTABANI: TestVeritabani ile AYNI (kuyumcu_test). Şema oradaki Migrate()
// ile kurulur; burada ikinci kez kurulmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HttpOrtami : WebApplicationFactory<Program>
{
    public HttpOrtami() : this(new TestVeritabani()) { }

    public HttpOrtami(TestVeritabani vt) => AyarlariKur(vt.Baglanti);

    // ── NEDEN ORTAM DEĞİŞKENİ, NEDEN ConfigureAppConfiguration DEĞİL ─────────
    // Program.cs fail-closed kurulmuş: şifreleme anahtarı ve Jwt sırrı
    // `builder.Build()` ÇAĞRILMADAN ÖNCE okunuyor (anahtar yoksa uygulama hiç
    // ayağa kalkmasın diye). WebApplicationFactory'nin ConfigureAppConfiguration
    // geri çağrıları ise Build() sırasında uygulanır — yani ÇOK GEÇ.
    //
    // Bu bir kusur değil, o tasarımın doğal sonucu. Testin uyum sağlaması
    // gereken yer burası: ayarları sürecin ortam değişkenlerine koyuyoruz ki
    // üst düzey ifadeler onları ilk satırda görebilsin.
    //
    // ⚠ Ortam değişkenleri SÜREÇ GENELİNDEDİR. Hepsi test veritabanını
    // gösterir; TestVeritabani zaten geliştirme veritabanına bağlanmayı
    // reddeder (kuyumcu / kuyumcu_net yasak), yani kaza riski kapalı.
    private static void AyarlariKur(string baglanti)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Kuyumcu", baglanti);
        Environment.SetEnvironmentVariable("Jwt__Secret", TestSirri);
        Environment.SetEnvironmentVariable("Jwt__AccessDakika", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshGun", "7");
        // TestVeritabani.TestSifreleyici ile AYNI anahtar: HTTP yoluyla yazılan
        // şifreli alan, doğrudan DbContext ile okunabilsin.
        Environment.SetEnvironmentVariable(
            Kuyumcu.Api.Common.AlanSifreleme.OrtamDegiskeni, TestSifrelemeAnahtari);
        // Çapraz origin testleri için beyaz listede tek origin.
        Environment.SetEnvironmentVariable("Cors__Origins__0", "https://izinli.example");
        // PAKET F-2-min: sistem yönetimi uçları YAPILANDIRMA SIRRI ile korunur.
        // Testte GERÇEK (yer tutucu olmayan) bir anahtar verilir ki uçlar AÇILSIN;
        // "yer tutucu ile uç KAPALI" davranışı ayrı bir SAF testle sınanır
        // (ortam değişkeni süreç genelindedir, bir fikstürde iki değer olamaz).
        Environment.SetEnvironmentVariable("Sistem__Anahtari", TestSistemAnahtari);
        Environment.SetEnvironmentVariable("Sistem__VarsayilanDenemeGun", "14");
        Environment.SetEnvironmentVariable("Sistem__DavetGecerlilikSaat", "72");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(s =>
        {
            // Kur zamanlayıcısı testte ÇALIŞMAZ: dış ağa çıkar, testi hem
            // yavaşlatır hem dış servise bağımlı kılar (Anayasa md.1 — testin
            // kırmızısı bizim hatamızı göstermeli, sağlayıcının kesintisini değil).
            s.RemoveAll<IHostedService>();

            // Üretimdeki nginx'i taklit et: TestServer RemoteIpAddress ataması
            // yapmaz, bu yüzden UseForwardedHeaders devreye girmezdi. Loopback
            // atanınca vekil "güvenilir" sayılır ve X-Forwarded-For okunur —
            // yani üretim topolojisinin aynısı sınanır.
            s.AddSingleton<IStartupFilter, VekilTaklidi>();
        });
    }

    /// <summary>
    /// TestServer'da uzak IP'yi loopback'e sabitler (nginx aynı makinede).
    /// Boru hattının EN BAŞINA eklenir ki UseForwardedHeaders'tan önce çalışsın.
    /// </summary>
    private sealed class VekilTaklidi : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> sonraki) =>
            uygulama =>
            {
                uygulama.Use(async (baglam, ileri) =>
                {
                    baglam.Connection.RemoteIpAddress ??= System.Net.IPAddress.Loopback;
                    await ileri();
                });
                sonraki(uygulama);
            };
    }

    /// <summary>Üretim sırrıyla aynı uzunlukta, ama yalnız teste özgü.</summary>
    public const string TestSirri = "test-sirri-yalnizca-testlerde-kullanilir-0123456789abcdef";

    /// <summary>Testlerdeki sistem yönetimi anahtarı (paket F-2-min) — 32+ karakter, yer tutucu DEĞİL.</summary>
    public const string TestSistemAnahtari = "test-sistem-anahtari-0123456789abcdefgh";

    /// <summary>X-Sistem-Anahtari + X-Sistem-Operator taşıyan istemci.</summary>
    public HttpClient SistemIstemci(string? anahtar = null, string? operatorAdi = "asikin-qa")
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Add("X-Forwarded-For", BenzersizIp());
        if (anahtar is not null) c.DefaultRequestHeaders.Add("X-Sistem-Anahtari", anahtar);
        if (operatorAdi is not null) c.DefaultRequestHeaders.Add("X-Sistem-Operator", operatorAdi);
        return c;
    }

    /// <summary>TestVeritabani.TestSifreleyici'nin anahtarı (1..32) base64 olarak.</summary>
    public static readonly string TestSifrelemeAnahtari = Convert.ToBase64String(
        Enumerable.Range(1, AesGcmAlanSifreleyici.AnahtarBayt).Select(i => (byte)i).ToArray());

    // ── İstemci kurucuları ───────────────────────────────────────────────────

    /// <summary>
    /// Token'sız istemci — 401 testleri için.
    /// <para>
    /// ⚠ <b>Neden IP ayrımı:</b> oran sınırı anonim istekleri IP'ye göre bölütler
    /// (genel 300/dk — ISTEK-1, kayıt 3/saat). Tüm testler aynı IP'den gelirse birbirinin
    /// kotasını yer ve testler <i>ürün doğru çalışırken</i> kırmızıya döner —
    /// bu gerçekten yaşandı (2026-09-13, <c>TooManyRequests</c>).
    /// Her test kendi istemci IP'siyle gelir; üretimdeki ayrı kullanıcıların
    /// karşılığıdır. Oran sınırı testte GEVŞETİLMEZ: sınanan davranış odur.
    /// </para>
    /// </summary>
    public HttpClient AnonimIstemci(string? istemciIp = null)
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Add("X-Forwarded-For", istemciIp ?? BenzersizIp());
        return c;
    }

    /// <summary>Test başına benzersiz, yönlendirilemeyen (RFC 5737) istemci IP'si.</summary>
    public static string BenzersizIp()
    {
        var n = Interlocked.Increment(ref sayac);
        return $"203.0.113.{n % 254 + 1}";
    }

    private static int sayac;

    /// <summary>Belirtilen kiracı/rol için imzalı token taşıyan istemci.</summary>
    public HttpClient Istemci(Kiraci kiraci, string rol = "SAHIP")
        => Istemci(kiraci.KullaniciId, kiraci.TenantId, kiraci.SubeId, rol);

    public HttpClient Istemci(string kullaniciId, string tenantId, string? subeId, string rol)
    {
        var token = new TokenServisi(new JwtAyarlar
        {
            Secret = TestSirri,
            AccessDakika = 15,
            RefreshGun = 7,
        }).AccessUret(kullaniciId, tenantId, subeId, rol);

        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    /// <summary>Süresi dolmuş/bozuk token — "geçersiz token" yolunu sınamak için.</summary>
    public HttpClient BozukTokenIstemcisi()
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "bu.gecerli.degil");
        return c;
    }
}

/// <summary>
/// Aynı MySQL şemasını hem servis testleri hem HTTP testleri kullanır.
/// Ayrı koleksiyon: HTTP testleri sunucu ayağa kaldırdığı için servis
/// testleriyle paralel koşmasınlar (oran sınırı sayaçları paylaşılıyor).
/// </summary>
public sealed class HttpFikstur : IDisposable
{
    public TestVeritabani Vt { get; } = new();
    public HttpOrtami Ortam { get; }

    public HttpFikstur() => Ortam = new HttpOrtami(Vt);

    public void Dispose() => Ortam.Dispose();
}

[CollectionDefinition("http")]
public sealed class HttpKoleksiyonu : ICollectionFixture<HttpFikstur>;

/// <summary>Yanıt gövdesini JSON olarak okumak için kısayollar.</summary>
public static class HttpYardim
{
    public static async Task<JsonElement> Json(this HttpResponseMessage r)
        => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    public static async Task<string> Metin(this HttpResponseMessage r)
        => await r.Content.ReadAsStringAsync();

    public static Task<HttpResponseMessage> PostJson(this HttpClient c, string yol, object govde)
        => c.PostAsJsonAsync(yol, govde);

    /// <summary>PATCH kısayolu (paket 40-A) — PATCH uçları da HTTP'den sınanır.</summary>
    public static Task<HttpResponseMessage> PatchJson(this HttpClient c, string yol, object govde)
        => c.PatchAsJsonAsync(yol, govde);
}
