using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── SERVİS YÖNETİCİSİ ENTEGRASYONU (Paket T-1) ───────────────────────────────
// Konsoldan çalıştırıldığında ikisi de NO-OP'tur; yalnız gerçekten bir Windows
// servisi ya da systemd birimi olarak başlatıldığında devreye girer (Type=notify
// hazır bildirimi, servis durdurma sinyali, günlük hedefi).
builder.Host.UseWindowsService();
builder.Host.UseSystemd();

// ── Yapılandırma ─────────────────────────────────────────────────────────────
var jwtAyarlar = new JwtAyarlar
{
    Secret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret tanımlı değil (appsettings veya ortam değişkeni)"),
    AccessDakika = builder.Configuration.GetValue("Jwt:AccessDakika", 15),
    RefreshGun = builder.Configuration.GetValue("Jwt:RefreshGun", 7),
};
// ── ÜRETİM AÇILIŞ KAPISI (Paket T-1, fail-closed) ────────────────────────────
// Common/UretimAcilisDenetimi.cs. Build() ÖNCESİ, AlanSifreleme.Yukle ile AYNI
// bölgede ve TEMBEL DEĞİL: varsayılan/örnek sırla, parolayla ya da TLS beyanı
// olmadan üretim süreci HİÇ açılmaz. Bu çağrı Program.cs'te TEKTİR
// (denetim.sh sayacı); ikinci bir yapılandırma okuma yolu açılmaz.
UretimAcilisDenetimi.Dogrula(builder.Configuration, builder.Environment);

builder.Services.AddSingleton(jwtAyarlar);
builder.Services.AddSingleton<TokenServisi>();

var baglanti = builder.Configuration.GetConnectionString("Kuyumcu")
    ?? throw new InvalidOperationException("ConnectionStrings:Kuyumcu tanımlı değil");
// ── Kiracı izolasyonu (ADIM1-KARAR-2026-09-11.md) ────────────────────────────
// Okuma: global sorgu filtresi. Yazma: KiraciYazmaDenetcisi. İkisi BİRLİKTE şart —
// filtre tek başına Add()/kiracı taşıma yollarını hiç kapatmaz.
builder.Services.AddHttpContextAccessor();
// Alan şifreleme anahtarı — AÇILIŞTA, İSTEKTEN ÖNCE yüklenir.
// ⚠ Canlı testte yakalandı: `AddSingleton(_ => Yukle(...))` TEMBELDİ; ilk DbContext
// çözülene kadar çağrılmıyordu ve anahtarsız uygulama /saglik'a 200 döndü. Fail-closed
// için fabrika değil, burada HEMEN yüklenen örnek kaydedilir: anahtar yoksa Build()'e
// bile gelinmez.
var alanSifreleyici = AlanSifreleme.Yukle(builder.Configuration);
builder.Services.AddSingleton<IAlanSifreleyici>(alanSifreleyici);
builder.Services.AddScoped<KapsamBaglamAyari>();
builder.Services.AddScoped<IKiraciBaglami, IstekKiraciBaglami>();
builder.Services.AddSingleton<SistemKapsami>();
builder.Services.AddScoped<DenetimServisi>();
builder.Services.AddScoped<Kuyumcu.Api.Modules.Hurda.HurdaServisi>();
// PAKET P-20 (Y-10) — hurda stok / ramat çıkışı okuma servisi.
builder.Services.AddScoped<Kuyumcu.Api.Modules.Hurda.HurdaStokServisi>();
// PAKET P-20b (K-13) — tahlil farkı düzeltme ucu.
builder.Services.AddScoped<Kuyumcu.Api.Modules.Hurda.TahlilFarkiServisi>();
builder.Services.AddScoped<Kuyumcu.Api.Modules.Masak.SaklamaSuresiIsi>();
builder.Services.AddScoped<Kuyumcu.Api.Modules.Masak.SaklamaBekletmeServisi>();   // PAKET YASAL-3 (§ 4.3)

// Resmî tatil takvimi: sabit tatiller kodda, DİNÎ bayramlar yapılandırmada
// (hicri takvim kayar, Diyanet ilanıyla kesinleşir — tahmin edilmez).
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Takvim")
        .Get<Kuyumcu.Api.Common.TakvimAyarlari>()
    ?? new Kuyumcu.Api.Common.TakvimAyarlari());

// MASAK eşikleri yapılandırmadan okunur — mevzuat rakamı değişince KOD DEĞİŞMEZ.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Masak")
        .Get<Kuyumcu.Api.Modules.Masak.MasakEsikleri>()
    ?? new Kuyumcu.Api.Modules.Masak.MasakEsikleri());
builder.Services.AddSingleton<Kuyumcu.Api.Modules.Masak.MasakKuralMotoru>();

// PAKET YASAL-1 — TARİHLİ yasal parametreler (tevsik haddi, MASAK eşikleri…).
// Kurallar rakamı İŞLEM GÜNÜNE göre bu servisten okur; appsettings bölümleri
// yalnız tohum/eşitlik denetimi içindir (açılışta ayrışırsa sunucu açılmaz).
builder.Services.AddSingleton(sp => new Kuyumcu.Api.Common.YasalParametreServisi(
    sp.GetRequiredService<SistemKapsami>()));

// PAKET 40-A — risk skorlama ayarları. ⚠ Etkin VARSAYILAN FALSE: risk temelli
// yaklaşımın kuyumcuyu bağlayan biçimi DOĞRULANMADI (ADR-0005). Kapalıyken skor
// hesaplanır ve gösterilir, hiçbir kapı kurmaz. Puanlar MEVZUAT DEĞİL POLİTİKADIR.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("MasakRisk")
        .Get<Kuyumcu.Api.Modules.Masak.MasakRiskAyarlari>()
    ?? new Kuyumcu.Api.Modules.Masak.MasakRiskAyarlari());

// İki aşamalı giriş (paket 45): hane/drift/kilit/ömür rakamları yapılandırmadan
// okunur (ADR-0005) — sıkılaştırma kod değişikliği istemesin. SIR YOK.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Mfa")
        .Get<Kuyumcu.Api.Modules.Auth.MfaAyarlari>()
    ?? new Kuyumcu.Api.Modules.Auth.MfaAyarlari());

// 459 s. VUK Genel Tebliği tevsik haddi (PLAN-2 Adım 2) — 30.000 TL, 575 s. Tebliğ
// (RG 30.11.2024/32738). Rakam yapılandırmadan okunur (ADR-0005).
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Tevsik")
        .Get<Kuyumcu.Api.Modules.Tevsik.TevsikEsikleri>()
    ?? new Kuyumcu.Api.Modules.Tevsik.TevsikEsikleri());
builder.Services.AddSingleton<Kuyumcu.Api.Modules.Tevsik.TevsikKurali>();

// Paket 20 — etiket ibare ayarları. Rodyum kaplama ibaresinin ZORUNLU olup
// olmadığı DOĞRULANMADI; ADR-0005 gereği koda gömülmez, varsayılan KAPALI.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Etiket")
        .Get<Kuyumcu.Api.Modules.Urun.EtiketAyarlari>()
    ?? new Kuyumcu.Api.Modules.Urun.EtiketAyarlari());
// Önizleme ucu (GET /tevsik/gunluk) kapıyı DI ile alır; kural ve eşik tek kaynaktır.
builder.Services.AddScoped<Kuyumcu.Api.Modules.Tevsik.TevsikKapisi>();

// ── PAKET F-2-min — kiracı daveti, deneme süresi, sistem anahtarı ───────────
// Sistem:Anahtari ÜRETİMDE yalnız ortam değişkeninden gelir (Sistem__Anahtari,
// T-1 sır hattı). appsettings.json'da DEGISTIR- yer tutucusu durur ve o değerle
// /api/v1/sistem/* uçları KAPALIDIR (fail-closed, SistemAnahtariKapisi).
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Sistem")
        .Get<Kuyumcu.Api.Modules.Sistem.SistemAyarlari>()
    ?? new Kuyumcu.Api.Modules.Sistem.SistemAyarlari());
// Kiracı durumu önbelleği SINGLETON'dır: 30 sn'lik ömrü süreç genelinde geçerli,
// süre uzatma onu AÇIKÇA boşaltır (SistemServisi.KiraciGuncelle).
builder.Services.AddSingleton<Kuyumcu.Api.Modules.Sistem.KiraciDurumOnbellegi>();
builder.Services.AddScoped<Kuyumcu.Api.Modules.Sistem.SistemServisi>();

// Veri dışa aktarımı (PLAN-2 Adım 4): jeton ömrü, gerekçe asgarisi ve tam
// kimlik onay metni yapılandırmadan okunur — Mehmet'in şartları sıkılaştırılabilsin.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("DisaAktarim")
        .Get<Kuyumcu.Api.Modules.Disaaktarim.DisaAktarimAyarlari>()
    ?? new Kuyumcu.Api.Modules.Disaaktarim.DisaAktarimAyarlari());

// Veri İÇE aktarımı (paket 47 Faz A): satır/dosya tavanı, gerekçe asgarisi ve
// onay metni yapılandırmadan okunur (ADR-0005 — eşikler koda gömülmez).
// ⚠ Tavanlar ÖLÇÜLMEDİ: "5.000 satır / 2 MB" Mehmet'in başlangıç değeridir,
//   gerçek bir dükkânın kart sayısı ölçülünce yapılandırmadan değiştirilir.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("IceAktarim")
        .Get<Kuyumcu.Api.Modules.Iceaktarim.IceAktarimAyarlari>()
    ?? new Kuyumcu.Api.Modules.Iceaktarim.IceAktarimAyarlari());

// Hurda alış / gider pusulası: stopaj oranları ve 7 günlük düzenleme süresi
// yapılandırmadan okunur — mevzuat değişince kod değişmez (Adım 6).
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Hurda")
        .Get<Kuyumcu.Api.Modules.Hurda.HurdaAyarlari>()
    ?? new Kuyumcu.Api.Modules.Hurda.HurdaAyarlari());
// POS (paket 31) — davranış anahtarları. ⚠ Komisyon oranı ve valör günü
// BURADA YOKTUR: onlar mevzuat değil BANKA ÜYE İŞYERİ SÖZLEŞMESİDİR ve cihaz
// tanımında kullanıcıdan alınır (ADR-0005 + ADR-0016).
builder.Services.Configure<Kuyumcu.Api.Modules.Pos.PosAyarlari>(
    builder.Configuration.GetSection("Pos"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Pos")
        .Get<Kuyumcu.Api.Modules.Pos.PosAyarlari>()
    ?? new Kuyumcu.Api.Modules.Pos.PosAyarlari());

// KDV (paket 38 + P-26/Y-40) — genel oran ve özel matrah neden kodu.
// ADR-0005: oran koda gömülmez. ⚠ OzelMatrahKodu VARSAYILANI BOŞ = KAPALI:
// Y-40 anahtarı K-10 (mali müşavir/kurucu teyidi) gelmeden AÇILMAZ (plan
// satır 578/778). Kapalıyken ürün "HARİÇ" beyanı gösterir; teyit gelince
// appsettings'te tek alan (Kdv:OzelMatrahKodu = 805) yazılarak açılır.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Kdv")
        .Get<Kuyumcu.Api.Modules.Fatura.KdvAyarlari>()
    ?? new Kuyumcu.Api.Modules.Fatura.KdvAyarlari());

// Belge deposu tavanları (paket 4) — ADR-0005: rakamlar yapılandırmadan.
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Belge")
        .Get<Kuyumcu.Api.Modules.Urun.BelgeAyarlari>()
    ?? new Kuyumcu.Api.Modules.Urun.BelgeAyarlari());

// Anonim uçlarda istek DbContext'i HİÇ kurulmasın diye Lazy: kiracısız bağlamda
// ctor fail-closed patlar, oysa Auth'un o yolları sistem kapsamı kullanır.
builder.Services.AddScoped(sp => new Lazy<KuyumcuDbContext>(sp.GetRequiredService<KuyumcuDbContext>));
builder.Services.AddDbContext<KuyumcuDbContext>((sp, o) => o
    .UseMySql(baglanti, ServerVersion.AutoDetect(baglanti))
    .ReplaceService<IModelCacheKeyFactory, KiraciModelOnbellegi>()
    .AddInterceptors(
        new KiraciYazmaDenetcisi(sp.GetRequiredService<ILogger<KiraciYazmaDenetcisi>>()),
        // Yazma denetim izi: KiraciYazmaDenetcisi'nden SONRA çalışır ki
        // reddedilen yazmalar için boşuna iz satırı üretilmesin.
        new DenetimYazmaDinleyicisi(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<ILogger<DenetimYazmaDinleyicisi>>()),
        new MasakHashDinleyicisi()));

// ── Kimlik doğrulama ─────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            // ⚠ PAKET 45 / Mehmet VETO: MFA meydan okuma jetonu access token ile
            // AYNI Secret'la imzalanır. Audience doğrulanmazsa JwtBearer onu geçerli
            // bearer sayar ve "parolası doğru ama MFA geçilmemiş" kullanıcı API'ye
            // girer — yani MFA fiilen atlanır. Access token audience'ı 'kuyumcu-api',
            // meydan okuma jetonunki 'kuyumcu-mfa'; ikincisi buradan GEÇEMEZ.
            ValidateAudience = true,
            ValidAudience = Kuyumcu.Api.Auth.Jetonlar.ApiAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAyarlar.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        // Nest JwtGuard sözleşmesi: 401 gövdesi { message, error, statusCode }
        //   • Token yoksa: "Token bulunamadı"
        //   • Token geçersiz/süresi dolmuşsa: "Geçersiz veya süresi dolmuş token"
        o.Events = new JwtBearerEvents
        {
            // SignalR WebSocket handshake'i Authorization başlığı gönderemez; token
            // access_token query'sinden okunur (yalnız /hubs yolları için).
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)
                    && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            },
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                var auth = ctx.Request.Headers.Authorization.ToString();
                var tokenVar = auth.StartsWith("Bearer ", StringComparison.Ordinal)
                    && auth.Length > "Bearer ".Length;
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(tokenVar ? "Geçersiz veya süresi dolmuş token" : "Token bulunamadı"),
                    error = "Unauthorized",
                    statusCode = 401,
                    // PAKET T-4a: 401 gövdesi de HataYakalayici'dan GEÇMEZ
                    // (JwtBearer doğrudan yazar); destek kodu burada da verilir.
                    istekKimligi = ctx.HttpContext.IstekKimligi(),
                }));
            },
        };
    });
builder.Services.AddAuthorization();

// ── MVC + doğrulama sözleşmesi ───────────────────────────────────────────────
builder.Services
    // ⚠ PAKET F-2-min: İKİNCİ BİR AddControllers AÇILMAZ (Bertan koşulu) —
    // kiracı durum kapısı MEVCUT zincire global filtre olarak eklenir.
    .AddControllers(o => o.Filters.Add<Kuyumcu.Api.Modules.Sistem.KiraciDurumKapisi>())
    // PAKET PATCH-2 — TIRNAKLI SAYI TEK NOKTADAN REDDEDİLİR. ASP.NET'in web varsayılanı
    // AllowReadingFromString'dir: "1.500" dizesi invariant okunup 1,5 olur (Türk
    // kullanıcının "bin beş yüz"ü sessizce 1,50 TL'ye düşüyordu — hasım ölçümü). Strict ile
    // dize sayı her yazma ucunda 400'dür. İstemci (Kuyumcu.Web/ApiIstemci) gövdeyi
    // JsonContent ile decimal SAYI olarak yazar (213 yazma çağrısı tarandı; rapor § 2),
    // yazma (serileştirme) davranışı değişmez. DTO üstündeki Strict öznitelikleri ikinci
    // savunma hattı olarak kalır.
    .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling =
        System.Text.Json.Serialization.JsonNumberHandling.Strict)
    .ConfigureApiBehaviorOptions(o =>
    {
        // Nest ValidationPipe formatı: { message: string[], error: "Bad Request", statusCode: 400 }
        //
        // ⚠ PAKET UX-4d madde 8: mesajlar buradan geçerken TÜRKÇELEŞTİRİLİR ve
        // ayrıca alan→mesaj sözlüğü (`alanlar`) eklenir; ekran hatayı ilgili
        // giriş alanının ALTINA yazabilsin (sessiz form yasağı, DoD EK md.3).
        // Türkçeleştirme tek noktada yapılır — gerekçe: Kuyumcu.Core
        // DogrulamaSozlugu başlığındaki not.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var alanlar = new Dictionary<string, string[]>();
            var mesajlar = new List<string>();

            // PAKET PATCH-2: JSON çözücü hatası anahtarı "$.alan" (JSON yolu) taşır ve
            // ardından gövde parametresi için yapay bir "dto zorunludur" eklenir.
            // Ekran alan altı mesajı "alan" anahtarıyla eşleştirir → yol temizlenir,
            // yapay zorunluluk hatası (gövde ZATEN gönderildi) düşürülür.
            var jsonHatasiVar = ctx.ModelState.Any(kv =>
                kv.Key.StartsWith('$') && kv.Value?.Errors.Count > 0);
            var govdeParametreleri = ctx.ActionDescriptor.Parameters
                .Where(p => p.BindingInfo?.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in ctx.ModelState.Where(kv => kv.Value?.Errors.Count > 0))
            {
                if (jsonHatasiVar && govdeParametreleri.Contains(kv.Key)) continue;

                string[] ceviriler;
                string anahtar;
                if (kv.Key.StartsWith('$'))
                {
                    var yol = JsonYoluAlani(kv.Key);
                    anahtar = yol.Length == 0 ? "govde" : Kuyumcu.Core.I18n.DogrulamaSozlugu.Anahtar(yol);
                    ceviriler = kv.Value!.Errors
                        .Select(e => JsonHataMesaji(yol, e.ErrorMessage, e.Exception?.Message))
                        .Distinct()
                        .ToArray();
                    // PAKET SON-KALANLAR: kullanıcı iletisi sade; teknik ayrıntı (çözücü metni) günlükte kalır.
                    foreach (var e in kv.Value!.Errors)
                        ctx.HttpContext.RequestServices.GetService<ILogger<Program>>()?.LogInformation(
                            "JSON_SAYI_COZULEMEDI {Yol} · {Ayrinti} · istek: {IstekKimligi}",
                            kv.Key, string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage,
                            ctx.HttpContext.IstekKimligi());
                }
                else
                {
                    anahtar = Kuyumcu.Core.I18n.DogrulamaSozlugu.Anahtar(kv.Key);
                    ceviriler = kv.Value!.Errors
                        .Select(e => Kuyumcu.Core.I18n.DogrulamaSozlugu.Cevir(kv.Key, e.ErrorMessage))
                        .ToArray();
                }
                mesajlar.AddRange(ceviriler);
                alanlar[anahtar] = alanlar.TryGetValue(anahtar, out var onceki)
                    ? [.. onceki, .. ceviriler]
                    : ceviriler;
            }

            // PAKET T-4a: doğrulama 400'ü HataYakalayici'dan GEÇMEZ (çerçeve
            // doğrudan yazar). Kullanıcı bu hatayı da destek koduyla
            // bildirebilsin diye kimlik burada da gövdeye konur.
            return new BadRequestObjectResult(new
            {
                message = mesajlar.ToArray(),
                alanlar,
                error = "Bad Request",
                statusCode = 400,
                istekKimligi = ctx.HttpContext.IstekKimligi(),
            });
        };
    })
    .AddMvcOptions(o =>
    {
        // Model bağlama hataları (tür uyuşmazlığı, eksik gövde) DataAnnotations
        // değildir; çerçevenin kendi İngilizce metinlerini kullanır. Resmî kanca
        // ModelBindingMessageProvider'dır — burada Türkçeye alınır.
        var m = o.ModelBindingMessageProvider;
        // PAKET DIL-SUNUCU: erişimciler İSTEK ANINDA çalışır → dil IstekDili'nden okunur (TR varsayılan).
        static bool En() => Kuyumcu.Core.I18n.IstekDili.Gecerli == "en";
        m.SetValueIsInvalidAccessor(v => En() ? $"'{v}' is not a valid value" : $"'{v}' geçerli bir değer değil");
        m.SetValueMustNotBeNullAccessor(_ => En() ? "This field cannot be left blank" : "Bu alan boş bırakılamaz");
        m.SetMissingRequestBodyRequiredValueAccessor(() => En() ? "The request body cannot be empty" : "İstek gövdesi boş olamaz");
        m.SetMissingBindRequiredValueAccessor(alan => En()
            ? $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)} is required"
            : $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)} zorunludur");
        m.SetMissingKeyOrValueAccessor(() => En() ? "A key or value is missing" : "Anahtar veya değer eksik");
        m.SetUnknownValueIsInvalidAccessor(_ => En() ? "The submitted value is not valid" : "Gönderilen değer geçerli değil");
        m.SetAttemptedValueIsInvalidAccessor((v, alan) => En()
            ? $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)}: '{v}' is not a valid value"
            : $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)}: '{v}' geçerli bir değer değil");
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(v => En() ? $"'{v}' is not a valid value" : $"'{v}' geçerli bir değer değil");
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => En() ? "The submitted value is not valid" : "Gönderilen değer geçerli değil");
        m.SetValueMustBeANumberAccessor(alan => En()
            ? $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)} must be a number"
            : $"{Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(alan)} sayı olmalıdır");
        m.SetNonPropertyValueMustBeANumberAccessor(() => En() ? "The value must be a number" : "Değer sayı olmalıdır");
    });

// ── CORS (Mehmet'in açık maddesi, ADIM1-2 raporu §8/3) ───────────────────────
// Eskiden AllowAnyOrigin idi. JWT başlıkta taşındığı için klasik CSRF yok, ama
// XSS ile token çalınırsa HERHANGİ bir origin'den kullanılabiliyordu.
// Artık beyaz liste: Cors:Origins yapılandırmasından okunur.
// Liste BOŞSA çapraz origin İZNİ VERİLMEZ — üretimde aynı origin'den servis
// edildiği için (Blazor + API tek süreç) bu doğru varsayılandır; fail-closed.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (corsOrigins.Length == 0)
    {
        p.WithOrigins().AllowAnyHeader().AllowAnyMethod();   // hiçbir çapraz origin
        return;
    }
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Canlı kur push kanalı (SignalR)
builder.Services.AddSignalR();

// Rol yetki matrisi önbelleği (tenant başına)
builder.Services.AddMemoryCache();

// Modül servisleri (tek kayıt noktası)
builder.Services.AddKuyumcuModulleri();

// Kur çekme zamanlayıcısı (Nest'teki @Cron karşılığı)
builder.Services.AddHostedService<Kuyumcu.Api.Modules.Kur.KurZamanlayici>();
// Paket 6 Faz A — zamanlanmış iş turu (iş MANTIĞI IsCalistirici'dedir).
builder.Services.AddHostedService<Kuyumcu.Api.Modules.Isler.IsZamanlayici>();
builder.Services.AddHostedService<Kuyumcu.Api.Modules.Banka.BankaCekici>();   // M7: banka sağlayıcı çekicisi
builder.Services.AddSingleton<Kuyumcu.Api.Modules.Masak.BmYaptirimGuncelleyici>();   // D1-18: BM yaptırım listesi (günlük)
builder.Services.AddHostedService(sp => sp.GetRequiredService<Kuyumcu.Api.Modules.Masak.BmYaptirimGuncelleyici>());
// PAKET P-28 (F-4 · R-2): bellekteki kiracı etiketli sayaçları periyodik yazar.
// ⚠ HttpOrtami tüm IHostedService'leri kaldırır → HTTP testinde KOŞMAZ; döngü
//   canlı doğrulamayla ölçülür, yazma MANTIĞI TelemetriYazici testleriyle kapanır.
builder.Services.AddHostedService<Kuyumcu.Api.Modules.Telemetri.TelemetriZamanlayici>();
builder.Services.AddHttpClient();

// ── ADIM 2: oran sınırı + sıkıştırma + sağlık ────────────────────────────────
builder.Services.AddKuyumcuOranSiniri();
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    // Blazor WASM ilk yükleme 9,26 MB ölçüldü; wasm sıkıştırılmıyordu.
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/wasm", "application/octet-stream", "application/json"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.AddHealthChecks();

// ── TERS VEKİL: gerçek istemci IP'si ─────────────────────────────────────────
// ⚠ CANLI YAPILANDIRMA İNCELEMESİNDE YAKALANDI (2026-09-13):
// `deploy/nginx-kuyumcu.conf` ters vekil olarak kuruluyor ve X-Forwarded-For /
// X-Real-IP başlıklarını gönderiyor. Ama uygulama bu başlıkları OKUMUYORDU.
// Sonuç: Connection.RemoteIpAddress herkes için nginx'in kendisi (127.0.0.1).
//
// Bunun üç somut etkisi vardı:
//   1. Kayıt oran sınırı (3/saat, IP başına) TÜM SİSTEM için 3/saat oluyordu —
//      bir kuyumcu kayıt olunca diğerleri saatlerce kilitleniyordu.
//   2. Genel sınır (100/dk) tüm anonim trafiği tek kovaya koyuyordu.
//   3. Denetim izindeki `ip_adresi` herkes için 127.0.0.1 yazıyordu; KVKK md.12
//      izinin IP alanı anlamsızdı.
//
// GÜVENLİK: X-Forwarded-For körü körüne güvenilemez — istemci uydurabilir ve
// oran sınırını atlayabilir. Bu yüzden YALNIZ güvenilen vekillerden gelen
// başlık kabul edilir. Varsayılan: loopback (nginx aynı makinede). Farklı
// topoloji için `TersVekil:GuvenilirVekiller` yapılandırmasından eklenir.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;            // tek vekil katmanı; zincir uydurulamasın
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
    o.KnownProxies.Add(IPAddress.Loopback);
    o.KnownProxies.Add(IPAddress.IPv6Loopback);

    foreach (var adres in builder.Configuration
                 .GetSection("TersVekil:GuvenilirVekiller").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(adres, out var ip)) o.KnownProxies.Add(ip);
        else throw new InvalidOperationException(
            $"TersVekil:GuvenilirVekiller içindeki '{adres}' geçerli bir IP adresi değil.");
    }
});

var app = builder.Build();

// Boru hattının EN BAŞINDA: IP ve şemayı okuyan her şeyden (oran sınırı,
// denetim izi, HTTPS yönlendirme) ÖNCE düzeltilmiş değerler oturmalı.
app.UseForwardedHeaders();

// ── İSTEK KİMLİĞİ (paket T-4a) ───────────────────────────────────────────────
// UseForwardedHeaders'tan SONRA (kimlik üretilirken gerçek istemci IP'si
// oturmuş olsun), HataYakalayici'dan ÖNCE: hata yakalayıcı kimliği HAZIR
// bulur ve oran sınırı reddi de kimlikli çıkar. TEK KAYIT NOKTASI budur.
// PAKET DIL-SUNUCU: istek dili (Accept-Language) — hata/429/401 gövdelerinden ÖNCE.
app.UseMiddleware<DilAraci>();
app.UseMiddleware<IstekKimligiAraci>();

// ── GÜVENLİK BAŞLIKLARI (OWASP ASVS 5.0.0 · V3 Web Frontend Security) ────────
// 2026-09-13 ASVS öz-değerlendirmesinde açık olarak tespit edildi: hiçbir
// güvenlik başlığı gönderilmiyordu.
//
// CSP notu: Blazor WebAssembly çalışmak için 'wasm-unsafe-eval' ister
// (WebAssembly derlemesi). 'unsafe-inline' stil için gereklidir — bileşenler
// satır içi style kullanır. Bunlar Blazor'un bilinen kısıtlarıdır, gevşeklik
// değil; script tarafında 'unsafe-inline' VERİLMEZ.
// P-37b: hava kutucuğunun dış kaynağı — varsayılan BOŞ (hiçbir dış origin
// açılmaz). Yalnız yapılandırmada açıkça verilirse connect-src genişler.
var havaCspEki = Kuyumcu.Api.Common.HavaOrigin.CspEki(app.Configuration["Hava:KaynakOrigin"]);

app.Use(async (baglam, ileri) =>
{
    var b = baglam.Response.Headers;
    // MIME tipi tahmini kapatılır — yüklenen içerik betik sanılamaz.
    b["X-Content-Type-Options"] = "nosniff";
    // Tıklama hırsızlığı (clickjacking): uygulama çerçeveye alınamaz.
    b["X-Frame-Options"] = "DENY";
    // Dış bağlantılara tam URL (ve olası kimlik) sızmasın.
    b["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Kullanılmayan tarayıcı yetenekleri kapatılır.
    b["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    b["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'wasm-unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'" + havaCspEki + "; " +   // API/SignalR + (varsa) hava kaynağı
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";
    await ileri();
});

// HSTS yalnız üretimde: geliştirmede localhost'u kalıcı HTTPS'e kilitlemek
// tarayıcıda zor temizlenen bir duruma yol açar.
if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseResponseCompression();
app.UseMiddleware<HataYakalayici>();

// ⚠ PAKET PATCH-3 — SAYI TAŞMASI SON SAVUNMA HATTI (PATCH-SOZLESME tur 3 açığı).
//   Kolon tavanı uç uç (KolonTavani.Denetle) konur; unutulan/hesaplanan bir değer
//   (ör. adet × fiyat toplamı) decimal(p,s) kolonuna sığmazsa MySQL katı kip 1264
//   "Out of range" verir, C# decimal çarpımı taşarsa OverflowException — ikisi de
//   500 idi. Hata istemcinin girdisidir, sebebi söylenebilir: 400 (+ çıkarılabiliyorsa
//   kolon adından alan). HataYakalayici'nın İÇİNDE durur: BadRequestException'a
//   çevrilen hata aynı gövde biçimiyle yazılır (T-4a). İşlem zaten geri alınmıştır
//   (SaveChanges/transaction istisnayla çıkar) — kısmi yazım yok.
app.Use(async (ctx, next) =>
{
    try { await next(ctx); }
    catch (Exception ex) when (!ctx.Response.HasStarted && Program.SayiTasmasiCevir(ex) is { } red)
    {
        // PATCH-3 tur 2 (doğrulayıcı DÜŞÜK): istisnanın KENDİSİ (yığın izi + iç halkalar)
        // günlüğe yazılır — girdiyle ilgisiz bir iç hesap taşması 400 görünse de günlükte
        // ayırt edilebilir kalır.
        app.Logger.LogWarning(ex, "Sayı taşması 400'e çevrildi: {Yol} · {Tur}", ctx.Request.Path, ex.GetType().Name);
        throw red;
    }
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapHealthChecks("/saglik");

// ── HAZIRLIK (readiness) YOKLAMASI — Paket T-1 ───────────────────────────────
// /saglik  = CANLILIK: süreç ayakta mı (yük dengeleyici bunu yoklar; gövdesi
//            Healthy/Unhealthy'dir ve sürüm/şema/sır SIZDIRMAZ).
// /saglik/hazir = HAZIRLIK: veritabanına bağlanılabiliyor mu ve şema güncel mi.
// Ayrım kasıtlıdır: şema eskiyken süreç AYAKTA ama HİZMETE HAZIR DEĞİLDİR.
// Gövde migration ADI vermez (şema bilgisi sızdırmaz), yalnız kısa bir kod.
// ⚠ DbContext DOĞRUDAN ENJEKTE EDİLMEZ. Uç ANONİMDİR; istek bağlamında kiracı
// yoktur ve IstekKiraciBaglami çözülürken patlar — o hata gövde bile yazılmadan
// 500 üretirdi (testte yakalandı: /saglik/hazir → InternalServerError).
// Doğru mekanizma zaten var: SistemKapsami, DbContext çözülmeden ÖNCE sistem
// bağlamını yerleştirir. Burada yalnız migration listesi okunur — kiracı verisi
// OKUNMAZ, YAZILMAZ.
app.MapGet("/saglik/hazir", async (SistemKapsami kapsamlar) =>
{
    try
    {
        await using var kapsam = kapsamlar.Sistem();
        var db = kapsam.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var bekleyen = await db.Database.GetPendingMigrationsAsync();
        return bekleyen.Any()
            ? Results.Json(new { hazir = false, kod = "SEMA_ESKI" }, statusCode: 503)
            : Results.Json(new { hazir = true, kod = "HAZIR" });
    }
    catch (Exception)
    {
        // Anayasa md.1: hata "hazır" diye yutulmaz. Ayrıntı (sunucu adı, parola,
        // sürücü mesajı) gövdeye YAZILMAZ; yalnız kısa bir kod döner.
        return Results.Json(new { hazir = false, kod = "VERITABANI_YOK" }, statusCode: 503);
    }
}).AllowAnonymous();

// Blazor WASM yayın çıktısını sun (üretim: tek süreçten hem web hem API)
app.UseBlazorFrameworkFiles();
// Sunum 14.10 (kurucu ekranında amblem tek yatay çizgi göründü): stil/betik/sayfa dosyaları sürüm adı taşımıyor
// (css/tema-yildiz.css); Cache-Control yokken tarayıcı tahmini önbellekle ESKİ CSS'i yeni WASM ile birleştirdi.
// no-cache = her açılışta ETag ile sor (değişmediyse 304, ek yük yok) → yeni yayın hemen görünür.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var uzanti = Path.GetExtension(ctx.File.Name);
        if (uzanti is ".css" or ".js" or ".html" or ".json")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Kuyumcu.Api.Auth.SaltOkunurKapisi>();   // D1-14: salt okunur (mali müşavir) hesap yazamaz — tek kapı

// ── PAKET P-28 (F-4 · R-2) — KİRACI ETİKETLİ TELEMETRİ ───────────────────────
// UseAuthentication'dan SONRA: kiracı etiketi (tenantId claim'i) çözülmüş
// olmalı. UseStaticFiles'tan SONRA: Blazor varlıkları kiracıya ait değildir.
// MapControllers'tan ÖNCE: uç süresi ve yanıt baytı bu sarmalın içinde ölçülür.
// ⚠ Ölçülen TEK şey sayaçtır (istek, hata, ms, bayt); yol/sorgu/başlık/gövde
//   OKUNMAZ — Mehmet koşulu, denetim.sh TELEMETRIDE_KISISEL_VERI ile kanıtlı.
app.UseMiddleware<Kuyumcu.Api.Modules.Telemetri.TelemetriAraci>();
// Oran sınırı kimlik doğrulamadan SONRA: genel limit kullanıcı başına bölütlenir,
// bunun için JWT'nin çözülmüş olması gerekir.
app.UseRateLimiter();

app.MapControllers();
app.MapHub<Kuyumcu.Api.Modules.Kur.KurHub>("/hubs/kur");   // canlı kur push
app.MapHub<Kuyumcu.Api.Modules.Banka.BankaHub>("/hubs/banka");   // M7: canlı banka hareketi
app.MapHub<Kuyumcu.Api.Modules.DisApi.DisaKurHub>("/hubs/disa-kur");   // D3-11: dış canlı kur (API anahtarı)
// ⚠ PAKET UX-4d madde 7 (Mehmet — API yüzeyi HTML sızdırmaz):
// /api/** altındaki BİLİNMEYEN bir yol SPA geri dönüşüne düşüp 200 + index.html
// döndürüyordu. Bir API istemcisi için bu sessiz bir yalandır: yanlış yazılmış
// uç "çalışıyor" görünür, tarayıcı konsolunda HTML ayrıştırma hatası çıkar ve
// uygulama kabuğunun tamamı API cevabı sanılarak sızar.
// Geri dönüş ARTIK /api önekinde KAPALI: 404 + JSON.
app.MapFallback("/api/{**kalan}", async ctx =>
{
    ctx.Response.StatusCode = 404;
    ctx.Response.ContentType = "application/json; charset=utf-8";
    await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
    {
        message = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Uç bulunamadı"),
        hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("uç bulunamadı"),
        error = "Not Found",
        statusCode = 404,
        yol = ctx.Request.Path.Value,
    }));
});

app.MapFallbackToFile("index.html", new StaticFileOptions   // SPA yönlendirmesi; sayfa da her açılışta sorulur (sunum 14.10)
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache"
});

// ── ŞEMA HAZIRLIK KAPISI (Paket T-1, FAIL-CLOSED 5) ──────────────────────────
// Otomatik `Database.Migrate()` KASITLI OLARAK AÇILMAZ: yedeksiz ve çok süreçli
// bir şema değişimi geri alınamaz. Şema yükseltmesi TEK YOLDAN yapılır —
// önce doğrulanmış yedek (dotnet/yedek-al-ve-dogrula.sh), sonra migration bundle
// (kuyumcu-migrate). Bkz. docs/RUNBOOK-SEMA-YUKSELTME.md.
//
// Bekleyen migration varsa üretim süreci HİZMET VERMEZ: çıkış kodu 3 ile durur.
// Yalnız Production'da; geliştirme/test koşusu etkilenmez.
if (app.Environment.IsProduction())
{
    // ⚠ Düz CreateScope() KULLANILMAZ: KuyumcuDbContext kiracı ya da sistem
    // bağlamı olmadan kurulamaz ve açılış "Kiracı bağlamı çözülemedi" ile
    // ÇÖKERDİ (gerçek üretim provasında yakalandı, 2026-09-17). SistemKapsami
    // bağlamı DbContext çözülmeden ÖNCE yerleştirir.
    using var acilisKapsami = app.Services.GetRequiredService<SistemKapsami>().Sistem();
    var acilisVt = acilisKapsami.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
    var bekleyenMigrationlar = acilisVt.Database.GetPendingMigrations().ToList();
    if (bekleyenMigrationlar.Count > 0)
    {
        app.Logger.LogCritical(
            "Şema güncel değil: {Sayi} bekleyen migration var. Önce yedek alın, sonra kuyumcu-migrate " +
            "çalıştırın (docs/RUNBOOK-SEMA-YUKSELTME.md). Uygulama açılmıyor.",
            bekleyenMigrationlar.Count);
        Environment.Exit(3);
    }
}

// ── PAKET YASAL-1 (K-7) — AÇILIŞ EŞİTLİK DENETİMİ (FAIL-CLOSED) ─────────────
// appsettings'teki mevzuat rakamı ile yasal_parametreler tablosunun BUGÜNKÜ
// değeri karşılaştırılır. YASAL-PARAM-EKRAN: tablodan okunan anahtarda tablo esas
// (uyarı); yapılandırmadan okunan anahtarda ayrışma → sunucu AÇILMAZ, günlükte
// tek satır sebep yazılır (UretimAcilisDenetimi deseni). Önbellek de burada dolar.
{
    var yasalParametreler = app.Services.GetRequiredService<Kuyumcu.Api.Common.YasalParametreServisi>();
    IReadOnlyList<string> yasalFarklar = [];
    try
    {
        yasalParametreler.Tazele();
        // PAKET MASAK-370: yasal değer değişikliği bir VERİ migration'ıyla gelir (ör.
        // MasakKimlikEsigi370: 185.000 → 370.000). Şema ESKİYSE (bekleyen migration var)
        // tablo henüz eski değeri taşır; yeni sürümün appsettings'iyle karşılaştırmak
        // "ayrışma" değil "şema eski" durumudur. O hâlde eşitlik denetimi ERTELENİR ve
        // günlüğe yazılır; hazır olmama /saglik/hazir 503 SEMA_ESKI ile, üretimde T-1
        // kapısının çıkış kodu 3 ile söylenir. Kapılar işlem anında tabloyu okur (eski,
        // daha sıkı değer) — gevşeme yok, fail-closed korunur.
        bool semaEski;
        using (var yasalKapsam = app.Services.GetRequiredService<SistemKapsami>().Sistem())
            semaEski = yasalKapsam.ServiceProvider.GetRequiredService<KuyumcuDbContext>()
                .Database.GetPendingMigrations().Any();
        // PAKET YASAL-PARAM-EKRAN — TEK KAYNAK: işlem anında TABLODAN okunan anahtarlarda
        // (MASAK eşikleri, tevsik haddi …) tablo ESASTIR; appsettings kopyası yalnız
        // veritabanısız kurulumun varsayılanıdır → fark UYARI, sunucu açılır (ekrandan
        // bugünden geçerli değişiklik sunucuyu "açılmaz" duruma düşürmez). Kodun HÂLÂ
        // yapılandırmadan okuduğu anahtarlarda fark gerçek ayrışmadır → ENGEL.
        // Şema eskiyse denetim TÜMÜYLE ATLANMAZ (hasım DÜŞÜK, MASAK-370): farklar yine
        // hesaplanır ve günlüğe yazılır; yalnız engel ertelenir (tablo bekleyen veri
        // migration'ından önceki değeri taşıyabilir).
        var (yasalEngel, yasalUyari) = yasalParametreler.AcilisDegerlendir(
            Kuyumcu.Api.Common.YasalAcilisDenetimi.YapilandirmaDegerleri(app.Services));
        foreach (var u in yasalUyari)
            app.Logger.LogWarning("YASAL_PARAM_YAPILANDIRMA_FARKLI — tablo esas alınır (işlem anında tablodan okunur): {Fark}", u);
        if (semaEski)
        {
            app.Logger.LogWarning("YASAL_PARAM_DENETIM_ERTELENDI — bekleyen migration var; yapılandırmadan okunan anahtarların engeli şema güncellenince uygulanır. Farklar: {Farklar}",
                yasalEngel.Count == 0 ? "yok" : string.Join(" · ", yasalEngel));
        }
        else
            yasalFarklar = yasalEngel;
    }
    catch (System.Data.Common.DbException ex)
    {
        // Veritabanına ulaşılamıyor ya da şema eski (tablo yok): bunun kapısı
        // /saglik/hazir (503 SEMA_ESKI / VERITABANI_YOK) ve üretimdeki bekleyen
        // migration çıkışıdır (kod 3). Eşitlik denetimi burada YAPILAMADI —
        // sessiz değil: günlüğe yazılır; yasal kapılar işlem anında tabloyu
        // okuyamazsa işlem yine yapılmaz (fail-closed).
        app.Logger.LogWarning("YASAL_PARAM_DENETIM_YAPILAMADI — yasal_parametreler okunamadı: {Hata}", ex.Message);
    }
    if (yasalFarklar.Count > 0)
    {
        var sebep = "YASAL_PARAM_AYRISMA — appsettings ile yasal_parametreler tablosu ayrışmış, uygulama açılmıyor: "
                    + string.Join(" · ", yasalFarklar);
        app.Logger.LogCritical("{Sebep}", sebep);
        throw new InvalidOperationException(sebep);
    }
}

app.Run();

public partial class Program
{
    /// <summary>
    /// PAKET PATCH-2 — JSON yolunu ekranın alan anahtarına çevirir:
    /// "$.satisFiyati" → "satisFiyati", "$.kalemler[0].adet" → "kalemler[0].adet", "$" → "".
    /// </summary>
    public static string JsonYoluAlani(string yol)
    {
        if (string.IsNullOrEmpty(yol) || yol == "$") return "";
        if (yol.StartsWith("$.", StringComparison.Ordinal)) return yol[2..];
        if (yol.StartsWith('$')) return yol[1..].TrimStart('.');
        return yol;
    }

    /// <summary>
    /// PAKET PATCH-2 — JSON çözücü hatasının Türkçe, alanı adlandıran iletisi. Sayı alanına
    /// dize ("1.500") ya da decimal aralığını aşan sayı (1e30) gelirse: "X geçerli bir sayı
    /// değil …". Hedef tür okunamıyorsa genel ileti; gövde hiç okunamıyorsa (bozuk JSON) gövde iletisi.
    /// </summary>
    public static string JsonHataMesaji(string alan, string? mesaj, string? istisnaMesaji)
    {
        var metin = string.IsNullOrWhiteSpace(mesaj) ? istisnaMesaji ?? "" : mesaj;
        var en = Kuyumcu.Core.I18n.IstekDili.Gecerli == "en";   // PAKET DIL-SUNUCU
        var ad = alan.Length == 0 ? (en ? "The request body" : "İstek gövdesi") : Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(PascalYol(alan));
        if (metin.Contains("could not be converted to", StringComparison.Ordinal))
        {
            var sayiTuru = metin.Contains("System.Decimal", StringComparison.Ordinal)
                || metin.Contains("System.Int", StringComparison.Ordinal)
                || metin.Contains("System.Double", StringComparison.Ordinal)
                || metin.Contains("System.Single", StringComparison.Ordinal)
                || metin.Contains("System.UInt", StringComparison.Ordinal);
            return sayiTuru
                // PAKET SON-KALANLAR (hasım DÜŞÜK): ileti kullanıcı dilinde — teknik ayrıntı (tırnak, ondalık
                // ayırıcı nokta) günlüğe gider (InvalidModelStateResponseFactory, JSON_SAYI_COZULEMEDI).
                ? (en ? $"{ad} is not a valid number — write the number in digits, e.g. 1500.50"
                      : $"{ad} geçerli bir sayı değil — sayıyı rakamla yazın, ör. 1500,50")
                : (en ? $"{ad} is not of the expected type" : $"{ad} beklenen türde değil");
        }
        return alan.Length == 0
            ? (en ? "The request body could not be read: send valid JSON" : "İstek gövdesi okunamadı: geçerli bir JSON gönderin")
            : (en ? $"{ad} could not be read: send a valid value" : $"{ad} okunamadı: geçerli bir değer gönderin");
    }

    /// <summary>
    /// PAKET PATCH-3 — sayı taşması mı? MySQL 1264 ("Out of range value for column 'x'")
    /// ya da OverflowException (istisna zincirinin herhangi bir halkasında) → 400 istisnası;
    /// değilse null. Sağlayıcı tipine derleme bağımlılığı yok: hata NUMARASI yansımayla
    /// okunur (HataYakalayici.BenzersizlikIhlaliMi ile aynı yöntem).
    /// </summary>
    public static Kuyumcu.Api.Common.BadRequestException? SayiTasmasiCevir(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            var numara = e.GetType().GetProperty("Number")?.GetValue(e);
            var aralikDisi = numara is int n
                ? n == 1264
                : e.Message.StartsWith("Out of range value for column", StringComparison.OrdinalIgnoreCase);
            if (aralikDisi)
            {
                var kolon = KolonAdi(e.Message);
                if (kolon is null)
                    return new Kuyumcu.Api.Common.BadRequestException(
                        "Girilen sayı çok büyük — kaydedilebilecek en büyük değeri aşıyor");
                var alan = KolonAlani(kolon);
                var ad = Kuyumcu.Core.I18n.DogrulamaSozlugu.AlanAdi(PascalYol(alan));
                return Kuyumcu.Api.Common.AlanIsareti.AlanIle(
                    new Kuyumcu.Api.Common.BadRequestException(
                        $"{ad} çok büyük — kaydedilebilecek en büyük değeri aşıyor"), alan);
            }
            if (e is OverflowException)
                return new Kuyumcu.Api.Common.BadRequestException(
                    "Girilen sayı çok büyük — hesaplanan tutar kaydedilebilecek en büyük değeri aşıyor");
        }
        return null;
    }

    /// <summary>MySQL iletisinden kolon adı: "… for column 'tl_tutari' at row 1" → tl_tutari.</summary>
    public static string? KolonAdi(string? ileti)
    {
        if (string.IsNullOrEmpty(ileti)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(ileti, @"column '(?:[^'.]+\.)?(?<k>[A-Za-z0-9_]+)'");
        return m.Success ? m.Groups["k"].Value : null;
    }

    /// <summary>snake_case kolon → camelCase alan: "tl_tutari" → "tlTutari".</summary>
    public static string KolonAlani(string kolon)
    {
        var parcalar = kolon.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length == 0) return kolon;
        return parcalar[0].ToLowerInvariant() + string.Concat(parcalar.Skip(1)
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    // "kalemler[0].birimFiyatTl" → "kalemler[0].BirimFiyatTl" (AlanAdi son parçayı sözlükte arar)
    private static string PascalYol(string yol)
    {
        var parcalar = yol.Split('.');
        var son = parcalar[^1];
        if (son.Length > 0 && char.IsLower(son[0])) parcalar[^1] = char.ToUpperInvariant(son[0]) + son[1..];
        return string.Join('.', parcalar);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PLAN-2 Adım 0a: WebApplicationFactory testten Program sınıfını görebilsin diye.
// Üst düzey ifadelerin ürettiği Program sınıfı internal olurdu; HTTP boru hattı
// (middleware sırası, auth, oran sınırı) başka türlü test edilemez.
// ═════════════════════════════════════════════════════════════════════════════
