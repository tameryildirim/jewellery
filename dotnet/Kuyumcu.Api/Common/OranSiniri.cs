using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// Oran sınırı — ADIM 2. Limitler Mehmet'in (06 Güvenlik) tespiti, ADIM1-KARAR §4.
//
// Neden: `giris` ve `yenile` uçları [AllowAnonymous] ve sınırsızdı (ölçüldü:
// AddRateLimiter = 0). BCrypt (round 12, ~250 ms) tek başına yeterli savunma
// değildir — IP başına paralel deneme ile aşılır.
// ═════════════════════════════════════════════════════════════════════════════

public static class OranSiniri
{
    public const string Giris = "giris";
    public const string Yenile = "yenile";
    public const string Kayit = "kayit";

    /// <summary>
    /// Sistem yönetimi davet ÜRETİMİ (paket F-2-min).
    /// <para>
    /// ⚠ CANLI DOĞRULAMA BULGUSU (2026-09-17): bu uç önce <see cref="Kayit"/>
    /// ilkesini kullanıyordu; ikisi AYNI bölümü (IP) paylaştığı için saatte 3
    /// istek HER İKİSİNE birden düşüyordu. Sonuç: (a) yöneticinin pilot daveti
    /// üretmesi, sıradan bir ziyaretçinin aynı IP'den yaptığı başarısız kayıt
    /// denemeleriyle kilitlenebiliyordu, (b) gerekçesiz/başlıksız 400 alan bir
    /// deneme bile kotayı yiyordu (sınırlayıcı, kapıdan ÖNCE koşar).
    /// </para>
    /// <para>
    /// Uç zaten sistem anahtarıyla korunur; buradaki sınır anahtar deneme
    /// hızını bastırmak içindir, meşru yöneticiyi kilitlemek için değil:
    /// 20/saat = pilot günü için bol, kaba kuvvet için hiç.
    /// </para>
    /// </summary>
    public const string SistemDavet = "sistem-davet";
    public const string Genel = "genel";

    /// <summary>
    /// Genel (GlobalLimiter) sınırı — kimliği doğrulanmış kullanıcı başına,
    /// anonimde IP başına, 1 dakikalık kayan pencerede. PAKET ISTEK-1: 100 → 300
    /// (gerekçe AddKuyumcuOranSiniri içindeki notta).
    /// </summary>
    public const int GenelDakikaSiniri = 300;

    /// <summary>Genel kayan pencerenin dilim sayısı (1 dk / 6 = 10 sn'lik dilim).</summary>
    public const int GenelDilimSayisi = 6;

    /// <summary>Genel sınır reddinde Retry-After: bir dilimin süresi (saniye).</summary>
    public const int GenelDilimSaniye = 60 / GenelDilimSayisi;

    /// <summary>
    /// Dışa aktarım (PLAN-2 Adım 4, Mehmet'in VETO-1 şartı 4). Toplu kişisel veri
    /// çıkışıdır; sızdırma denemesinin hızını sınırlar. Meşru kullanım seyrektir —
    /// bir işletme günde birkaç kez veri almak ister, dakikada değil.
    ///
    /// ⚠ TALEP ve İNDİRME AYNI KOTAYI PAYLAŞIR: bir tam aktarım iki istek eder,
    /// yani saatte 10 istek = 5 tam aktarım. Bu rakam canlı doğrulamada ölçüldü
    /// (2026-09-14): 5'lik kota yalnız 2 tam aktarıma yetiyordu ve sıradan bir
    /// deneme-yanılma bile kilide takılıyordu.
    /// </summary>
    public const string DisaAktarim = "disa-aktarim";

    /// <summary>
    /// Veri İÇE aktarımı (paket 47 Faz A, Mehmet'in koşulu: dışa aktarımın
    /// ALTINDA — 5/saat). Gerekçe: içe aktarımın riski sızdırma değil
    /// ZEHİRLEME'dir (uydurma cari, sahte stok); meşru kullanım ise ömürde
    /// birkaç kezdir (kuruluşta bir göç). Kuru prova ve uygulama AYNI kotayı
    /// paylaşır: bir tam göç 2 istek eder, yani saatte 5 istek ≈ 2 tam göç.
    /// Rakam koda gömülü DEĞİL, bu sabit yalnız politikanın ADIDIR.
    /// </summary>
    public const string IceAktarim = "ice-aktarim";

    /// <summary>
    /// Belge yükleme (paket 4, Mehmet K4-6). Kimliği doğrulanmış bir kullanıcı
    /// 5 MB'lık istekleri sınırsız gönderemez. Anahtar KULLANICI'dır
    /// (<see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> — 'sub' tuzağı).
    /// </summary>
    public const string Belge = "belge";

    /// <summary>
    /// Banka ekstresi yükleme (paket 32, Mehmet koşulu). Ayrıştırma CPU işidir
    /// ve 10.000 satırlık bir dosya ucuz değildir; meşru kullanım ise ayda
    /// birkaç kezdir (hesap başına aylık ekstre). Saatte 20 yükleme bunu
    /// sıkmaz, ayrıştırıcıyı döngüye sokan istismarı keser.
    /// Rakam koda gömülü DEĞİL; bu sabit yalnız politikanın ADIDIR.
    /// </summary>
    public const string Ekstre = "ekstre";

    /// <summary>
    /// MFA kurulum/kapatma uçları (paket 45). Kimliği doğrulanmış kullanıcı başına
    /// 10/dakika: meşru kullanımda bir kurulum birkaç istektir, kod deneme
    /// yüzeyini ise dar tutar.
    /// </summary>
    public const string Mfa = "mfa";

    /// <summary>
    /// Girişin İKİNCİ adımı (paket 45) — 5/dakika, girişle aynı sıkılık.
    /// <para>
    /// ⚠ Bölüt anahtarı meydan okuma jetonundan çözülen KULLANICI'dır; jeton
    /// <b>imza doğrulanarak</b> çözülür (Mehmet'in şartı: imzasız parse ile
    /// başkasının kotası tüketilemesin). Çözülemezse IP'ye düşer — daha sıkı taraf.
    /// </para>
    /// </summary>
    public const string MfaGiris = "mfa-giris";

    /// <summary>
    /// Zamanlanmış işin ELLE tetiklenmesi (paket 6). Kullanıcı başına 3/dakika:
    /// bir koşum kiracının TÜM cari ve uyum kayıtlarını tarar; art arda tetikleme
    /// veritabanını meşgul eden ucuz bir kaldıraçtır. Meşru kullanım seyrektir —
    /// iş zaten kendiliğinden koşar, elle tetikleme "şimdi bak" içindir.
    /// </summary>
    public const string IsCalistir = "is-calistir";

    /// <summary>
    /// PAKET AUTH-SIFIRLA — "Parolamı unuttum" uçları için IP başına KABA tavan
    /// (15 dk'da 20). Asıl kural (e-posta + IP başına 15 dk'da 3) servis içinde,
    /// GÖVDEDEKİ e-postayla uygulanır (ParolaSifirlamaHizSiniri); bu tavan tek IP'nin
    /// e-posta değiştirerek toplu posta yağdırmasını keser.
    /// </summary>
    public const string ParolaSifirlama = "parola-sifirlama";

    public static IServiceCollection AddKuyumcuOranSiniri(this IServiceCollection s) =>
        s.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Giriş: IP + e-posta birlikte. Yalnız IP olsaydı ortak NAT arkasındaki
            // bir dükkânın tüm personeli birbirini kilitlerdi.
            o.AddPolicy(Giris, k => RateLimitPartition.GetFixedWindowLimiter(
                Anahtar(k, epostaDahil: true),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

            o.AddPolicy(Yenile, k => RateLimitPartition.GetFixedWindowLimiter(
                Anahtar(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            // Kayıt: otomatik sahte kiracı üretimini engeller.
            o.AddPolicy(Kayit, k => RateLimitPartition.GetFixedWindowLimiter(
                Anahtar(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1) }));

            // Sistem davet üretimi: kayıt kotasından AYRI bölüm (yukarıdaki not).
            o.AddPolicy(SistemDavet, k => RateLimitPartition.GetFixedWindowLimiter(
                Anahtar(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromHours(1) }));

            o.AddPolicy(Mfa, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            o.AddPolicy(MfaGiris, k => RateLimitPartition.GetFixedWindowLimiter(
                MfaAnahtari(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

            o.AddPolicy(ParolaSifirlama, k => RateLimitPartition.GetFixedWindowLimiter(
                Anahtar(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(15) }));

            o.AddPolicy(IsCalistir, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(1) }));

            o.AddPolicy(IceAktarim, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));

            o.AddPolicy(DisaAktarim, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) }));

            // Belge yükleme: kullanıcı başına 30/dakika. Meşru kullanımda bir
            // ürüne birkaç belge eklenir; 30 bunu sıkmaz, toplu istismarı keser.
            o.AddPolicy(Belge, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

            // Banka ekstresi yükleme: kullanıcı başına 20/saat (paket 32).
            o.AddPolicy(Ekstre, k => RateLimitPartition.GetFixedWindowLimiter(
                Kullanici(k),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromHours(1) }));

            // Genel API: kullanıcı başına, kayan pencere (1 dk, 6 dilim).
            //
            // ⚠ PAKET ISTEK-1 — 100/dk → 300/dk (ekip kararı: Tamer + Mehmet/güvenlik,
            // 2026-09-25). ÖLÇÜM: orkestratörün tek kullanıcılı başsız Chrome
            // turunda 10 ekran 30 sn'de gezildi → 67 /api/v1 isteği (~134/dk).
            // Hızlı gezinen TEK meşru kullanıcı 100/dk'ya çarpıyor, ekranlar
            // kırmızı "çok yoğun" bandıyla boşalıyordu (GOZ-INCELEME-2026-09-25
            // #0 #34 #44–#46 #53 #57 #66–#69). İstemci tarafında liste önbelleği
            // (ListeOnbellegi) aynı ölçümü ayrıca düşürür; sınır meşru gezinmenin
            // ~2 katına çekildi.
            // GÜVENLİK GEREKÇESİ (Mehmet): bu sınır kimlik doğrulanmış kullanıcı
            // başınadır ve kaba kuvvet savunması DEĞİLDİR — kaba kuvvet yüzeyleri
            // (giriş 5/dk, yenile 10/dk, kayıt 3/saat, MFA 10/dk, MFA girişi 5/dk,
            // dışa/içe aktarım, belge, ekstre, iş tetikleme) kendi sıkı
            // politikalarıyla ZİNCİRLENİR ve DEĞİŞMEDİ. 300/dk tek kullanıcının
            // betikle toplu veri çekme hızını hâlâ saniyede 5 istekle sınırlar.
            //
            // ⚠ GlobalLimiter olarak kuruluyor, `RequireRateLimiting` ile DEĞİL.
            // Sebep (canlı testte yakalandı): MapControllers().RequireRateLimiting(Genel)
            // uca kural olarak eklendiğinde, action üzerindeki [EnableRateLimiting]
            // sıkı politikasını EZİYORDU — giriş ucu 5/dk yerine 100/dk ile
            // çalışıyor ve 8 ardışık hatalı denemede 429 DÖNMÜYORDU.
            // GlobalLimiter uç politikasıyla ZİNCİRLENİR, ezmez: her ikisi de uygulanır.
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(k =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    Kullanici(k),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = GenelDakikaSiniri, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = GenelDilimSayisi,
                    }));

            // 429 gövdesi mevcut hata sözleşmesine uyar (message/error/statusCode).
            o.OnRejected = async (baglam, iptal) =>
            {
                baglam.HttpContext.Response.ContentType = "application/json";
                // PAKET ISTEK-1 (ölçüldü): kayan pencere (GlobalLimiter) reddinde kira
                // RetryAfter üst verisi TAŞIMAZ → başlık hiç gitmiyordu ve istemci ne
                // kadar bekleyeceğini bilemiyordu. Kayan pencerede izinler en geç bir
                // DİLİM sonunda geri döner; o süre (60 sn / 6 = 10 sn) doğru üst sınırdır.
                // Sabit pencereli uç politikaları kendi süresini zaten verir.
                var saniye = baglam.Lease.TryGetMetadata(MetadataName.RetryAfter, out var sure)
                    ? (int)Math.Ceiling(sure.TotalSeconds)
                    : GenelDilimSaniye;
                baglam.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(1, saniye).ToString(CultureInfo.InvariantCulture);

                await baglam.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Çok fazla istek gönderildi, lütfen biraz bekleyin"),
                    error = "TooManyRequests",
                    statusCode = 429,
                }), iptal);
            };
        });

    private static string Ip(HttpContext k) =>
        k.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen";

    /// <summary>
    /// Bölütleme anahtarı: giriş yapmışsa KULLANICI, değilse IP.
    ///
    /// ⚠ NEDEN <c>FindFirst("sub")</c> DEĞİL (2026-09-14'te yakalandı): JwtBearer
    /// varsayılan olarak <c>MapInboundClaims = true</c> ile çalışır ve gelen
    /// <c>sub</c> talebini <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/>
    /// adına ÇEVİRİR. Yani <c>FindFirst("sub")</c> kimlikli isteklerde bile
    /// HER ZAMAN null dönüyordu; "kullanıcı başına" yazan sınır fiilen IP başına
    /// çalışıyordu. Aynı dükkânda tek internet hattı arkasındaki bütün personel
    /// tek kotayı paylaşıyordu. <see cref="Auth.AktifKullanici"/> ile aynı
    /// çözümleme sırası kullanılır.
    /// </summary>
    private static string Kullanici(HttpContext k) =>
        k.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? k.User.FindFirst("sub")?.Value
        ?? Ip(k);

    /// <summary>
    /// MFA ikinci adımı için bölüt: Authorization başlığındaki meydan okuma jetonu
    /// İMZA DOĞRULANARAK çözülür; olmazsa IP. Gövde OKUNMAZ (akış tüketilmesin).
    /// </summary>
    private static string MfaAnahtari(HttpContext k)
    {
        var servis = k.RequestServices.GetService<Auth.TokenServisi>();
        var auth = k.Request.Headers.Authorization.ToString();
        var jeton = auth.StartsWith("Bearer ", StringComparison.Ordinal)
            ? auth["Bearer ".Length..]
            : null;
        var cozum = servis?.MfaMeydanOkumaCoz(jeton);
        return cozum is null ? Ip(k) : $"mfa|{cozum.Value.KullaniciId}";
    }

    private static string Anahtar(HttpContext k, bool epostaDahil = false)
    {
        if (!epostaDahil) return Ip(k);
        // Gövdeyi okumadan e-postayı almak için istemcinin gönderdiği başlığa bakılır;
        // yoksa yalnız IP'ye düşer (daha sıkı, güvenli taraf).
        var eposta = k.Request.Headers["X-Giris-Eposta"].ToString();
        return string.IsNullOrWhiteSpace(eposta) ? Ip(k) : $"{Ip(k)}|{eposta.ToLowerInvariant()}";
    }
}
