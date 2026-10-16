using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Api.Modules.Auth;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET AUTH-SIFIRLA — "Parolamı unuttum": kullanıcının KENDİ parolasını güvenli
// sıfırlaması (kurul 5 engeli: "tek kullanıcılı dükkânda patron parolasını
// unutursa destek çağrısı açılır").
//
// EKİP OYU (Mehmet güvenlik + Taşkın + Ertuğ) — kodda madde numarasıyla anılır:
//   (1) Yanıt HER ZAMAN aynı: hesap var/yok sızdırılmaz (gövde + süre tabanı).
//   (2) Jeton ≥ 32 bayt rastgele, 30 dk, tek kullanımlık; DB'de yalnız SHA-256;
//       yeni istek ve kullanım öncekileri iptal eder.
//   (3) Hız sınırı: e-posta + IP başına 15 dk'da 3 istek (4.'sü 429).
//   (4) Yeni parola mevcut parola kuralına tabi; başarılı sıfırlamada TÜM
//       refresh jetonları iptal + denetim kaydı (parola/jeton DEĞERİ yazılmaz).
//   (5) MFA atlanmaz: sıfırlama OTURUM AÇMAZ, jeton döndürmez; sonraki giriş
//       MFA ikinci adımından geçer (parola sürümü değiştiği için elde kalan
//       meydan okuma jetonu da ölür).
//   (6) E-posta kanalı yoksa FAIL-CLOSED: istek ucu 503, ekran "dükkân
//       sahibine/destek hattına başvurun" der; "gönderildi" DEMEZ.
//   (7) Geliştirme/test: e-posta yerel DOSYAYA ya da test kanalına yazılır;
//       GERÇEK E-POSTA GÖNDERİMİ YOK (ADR-0016 dış kanal tekeli — IBildirimKanali
//       DI kaydı 1'de kalır; bu kanal bildirim değil, ayrı ve dar bir arayüzdür).
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class ParolaSifirlamaIstekDto
{
    [Required(ErrorMessage = "E-posta zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [MaxLength(254)]
    public string Email { get; set; } = null!;
}

public sealed class ParolaSifirlamaTamamlaDto
{
    /// <summary>E-postadaki bağlantının jetonu (base64url, 43 karakter).</summary>
    [Required(ErrorMessage = "Bağlantı jetonu eksik")]
    [MaxLength(200)]
    public string Jeton { get; set; } = null!;

    [Required(ErrorMessage = "Yeni şifre zorunludur")]
    public string YeniSifre { get; set; } = null!;

    [Required(ErrorMessage = "Yeni şifrenin tekrarı zorunludur")]
    public string YeniSifreTekrar { get; set; } = null!;
}

// ── Ayarlar ──────────────────────────────────────────────────────────────────

/// <summary>
/// "ParolaSifirlama" yapılandırma bölümü. SIR YOK.
/// <para>
/// <see cref="Kanal"/>: boş = KAPALI (varsayılan, fail-closed). "DOSYA" = e-posta
/// yerel dizine .eml olarak yazılır — YALNIZ Development/Testing ortamında
/// kabul edilir (üretimde jeton diske düşmesin). Gerçek SMTP bu üründe YOK
/// (ADR-0016): üretimde özellik kapalı kalır, ekran doğru yolu yazar.
/// </para>
/// <para>
/// <see cref="BaglantiTabani"/>: e-postadaki bağlantının kökü (örn.
/// https://dukkan.ornek.com). İSTEĞİN Host başlığından TÜRETİLMEZ — Host
/// zehirlemesiyle jetonun saldırgan alan adına gitmesi engellenir. Boşsa
/// özellik KAPALI sayılır.
/// </para>
/// </summary>
public sealed class ParolaSifirlamaAyarlari
{
    public const string KanalDosya = "DOSYA";

    public string? Kanal { get; set; }
    public string? DosyaDizini { get; set; }
    public string? BaglantiTabani { get; set; }

    /// <summary>
    /// Yanıt süre tabanı (ms) — madde (1): var/yok e-postanın yanıtı aynı süre
    /// aralığında döner (iş bundan kısa sürerse kalan süre beklenir).
    /// </summary>
    public int YanitTabaniMs { get; set; } = 600;
}

// ── E-posta kanalı (dar arayüz; bildirim kanalı DEĞİL) ───────────────────────

/// <summary>
/// Parola sıfırlama postasının TEK çıkış yolu. Uygulaması yapılandırmadan seçilir
/// (<see cref="ParolaSifirlamaPostasiFabrikasi"/>). Gerçek SMTP uygulaması YOKTUR.
/// </summary>
public interface IParolaSifirlamaPostasi
{
    /// <summary>Kanal kullanılabilir mi? false → istek ucu 503 (madde 6).</summary>
    bool Yapilandirildi { get; }

    Task Gonder(string alici, string konu, string govde, CancellationToken iptal);
}

/// <summary>Varsayılan: KAPALI. Hiçbir şey göndermez; çağrılırsa patlar (sessiz "gönderildi" yok).</summary>
public sealed class KapaliParolaSifirlamaPostasi : IParolaSifirlamaPostasi
{
    public bool Yapilandirildi => false;

    public Task Gonder(string alici, string konu, string govde, CancellationToken iptal) =>
        throw new InvalidOperationException("Parola sıfırlama e-posta kanalı yapılandırılmamış.");
}

/// <summary>
/// Geliştirme/test kanalı (madde 7): postayı yerel dizine .eml dosyası olarak yazar.
/// Dosya yalnız sahibine okunur (Unix 600). Dışarıya istek GİTMEZ.
/// </summary>
public sealed class DosyaParolaSifirlamaPostasi(string dizin) : IParolaSifirlamaPostasi
{
    public bool Yapilandirildi => true;

    public string Dizin { get; } = dizin;

    public async Task Gonder(string alici, string konu, string govde, CancellationToken iptal)
    {
        Directory.CreateDirectory(Dizin);
        var ad = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml";
        var yol = Path.Combine(Dizin, ad);
        var icerik = new StringBuilder()
            .Append("To: ").Append(alici).Append("\r\n")
            .Append("Subject: ").Append(konu).Append("\r\n")
            .Append("Date: ").Append(DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("Content-Type: text/plain; charset=utf-8\r\n\r\n")
            .Append(govde).Append("\r\n")
            .ToString();
        await File.WriteAllTextAsync(yol, icerik, new UTF8Encoding(false), iptal);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(yol, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}

/// <summary>Kanalı yapılandırmadan seçer. Tanınmayan değer ya da üretimde DOSYA → KAPALI.</summary>
public static class ParolaSifirlamaPostasiFabrikasi
{
    public static IParolaSifirlamaPostasi Kur(
        ParolaSifirlamaAyarlari ayar, IHostEnvironment ortam, ILogger gunluk)
    {
        var kanal = (ayar.Kanal ?? "").Trim().ToUpperInvariant();
        if (kanal.Length == 0) return new KapaliParolaSifirlamaPostasi();

        if (kanal == ParolaSifirlamaAyarlari.KanalDosya)
        {
            if (!(ortam.IsDevelopment() || ortam.IsEnvironment("Testing")))
            {
                gunluk.LogWarning(
                    "ParolaSifirlama:Kanal=DOSYA yalnız Development/Testing ortamında kabul edilir; " +
                    "{Ortam} ortamında özellik KAPALI (fail-closed).", ortam.EnvironmentName);
                return new KapaliParolaSifirlamaPostasi();
            }
            var dizin = string.IsNullOrWhiteSpace(ayar.DosyaDizini)
                ? Path.Combine(Path.GetTempPath(), "kuyumcu-posta-kutusu")
                : ayar.DosyaDizini!;
            return new DosyaParolaSifirlamaPostasi(dizin);
        }

        gunluk.LogWarning("ParolaSifirlama:Kanal tanınmıyor; özellik KAPALI (fail-closed).");
        return new KapaliParolaSifirlamaPostasi();
    }
}

// ── Hız sınırı (madde 3) ─────────────────────────────────────────────────────

/// <summary>
/// E-posta + IP başına 15 dakikada 3 istek. Bölüt anahtarı GÖVDEDEKİ e-postadır
/// (başlık değil): istemci başlığı değiştirerek sınırı aşamaz. Hesap VAR/YOK
/// fark etmeksizin sayılır — 429'un kendisi varlık sızdırmaz.
/// </summary>
public sealed class ParolaSifirlamaHizSiniri : IDisposable
{
    public const int PencereIstek = 3;
    public static readonly TimeSpan Pencere = TimeSpan.FromMinutes(15);

    private readonly PartitionedRateLimiter<string> sinirlayici =
        PartitionedRateLimiter.Create<string, string>(anahtar =>
            RateLimitPartition.GetFixedWindowLimiter(anahtar, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PencereIstek,
                Window = Pencere,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    /// <summary>İzin varsa true; yoksa false + kaç saniye sonra deneneceği.</summary>
    public bool Al(string ip, string normalEposta, out int bekleSaniye)
    {
        using var kira = sinirlayici.AttemptAcquire($"{ip}|{normalEposta}");
        if (kira.IsAcquired) { bekleSaniye = 0; return true; }
        bekleSaniye = kira.TryGetMetadata(MetadataName.RetryAfter, out var sure)
            ? Math.Max(1, (int)Math.Ceiling(sure.TotalSeconds))
            : (int)Pencere.TotalSeconds;
        return false;
    }

    public void Dispose() => sinirlayici.Dispose();
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed record ParolaSifirlamaIstekSonucu(bool Reddedildi, int BekleSaniye);

public sealed class ParolaSifirlamaServisi(
    SistemKapsami kapsam,
    IParolaSifirlamaPostasi posta,
    ParolaSifirlamaAyarlari ayar,
    ParolaSifirlamaHizSiniri hizSiniri,
    ILogger<ParolaSifirlamaServisi> gunluk)
{
    /// <summary>Jeton ömrü — madde (2): 30 dakika.</summary>
    public const int JetonOmruDakika = 30;

    /// <summary>Jeton entropisi — madde (2): en az 32 bayt.</summary>
    public const int JetonBayt = 32;

    private const int BcryptRounds = 12;

    /// <summary>
    /// Mevcut parola kuralı (KullaniciModulu.SifreDegistirDto ile AYNI — drift
    /// AuthSifirlaTestleri'nde yansımayla bekçilenir): 8-72 karakter, en az bir
    /// harf ve bir rakam.
    /// </summary>
    public const string ParolaKalibi = "^(?=.*[0-9])(?=.*[a-zA-Z]).+$";
    public const int ParolaEnAz = 8;
    public const int ParolaEnFazla = 72;

    /// <summary>Madde (1): var/yok AYNI metin.</summary>
    public const string IstekYanitMesaji =
        "Bu e-posta adresi kayıtlıysa parola sıfırlama bağlantısı gönderildi. " +
        "Bağlantı 30 dakika geçerlidir; gelen kutunuzu ve istenmeyen klasörünü kontrol edin.";

    /// <summary>Madde (6): kanal yokken ekranın ve 503'ün söylediği doğru yol.</summary>
    public const string KapaliMesaji =
        "Parola sıfırlama e-postası bu kurulumda açık değil. Şifre sıfırlama için işletme " +
        "sahibine ya da destek hattına başvurun.";

    /// <summary>Jeton geçersiz/kullanılmış/süresi dolmuş — TEK TİP mesaj (hangisi olduğu sızdırılmaz).</summary>
    public const string JetonRedMesaji =
        "Bu bağlantı geçersiz, kullanılmış ya da süresi dolmuş. Giriş ekranından yeni bir bağlantı isteyin.";

    /// <summary>Özellik açık mı? Kanal yapılandırılmış VE bağlantı tabanı verilmiş olmalı.</summary>
    public bool Acik =>
        posta.Yapilandirildi && Uri.TryCreate(ayar.BaglantiTabani, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);

    public static string Normallestir(string? eposta) => (eposta ?? "").Trim().ToLowerInvariant();

    // ─── İstek: "bağlantı gönder" ─────────────────────────────────────────────

    public async Task<ParolaSifirlamaIstekSonucu> Iste(ParolaSifirlamaIstekDto dto, string ip, CancellationToken iptal)
    {
        // Servis içi elle kontrol (DTO DataAnnotations yalnız controller yolunda).
        var eposta = Normallestir(dto?.Email);
        if (eposta.Length == 0 || eposta.Length > 254 || !new EmailAddressAttribute().IsValid(eposta))
            throw new BadRequestException("Geçerli bir e-posta adresi giriniz").AlanIle("email");

        // Madde (6): FAIL-CLOSED — kanal yoksa "gönderildi" denmez.
        if (!Acik)
            throw new ApiException(StatusCodes.Status503ServiceUnavailable, KapaliMesaji, "Service Unavailable");

        // Madde (3): e-posta + IP başına 15 dk'da 3.
        if (!hizSiniri.Al(ip, eposta, out var bekle))
            return new ParolaSifirlamaIstekSonucu(true, bekle);

        // Madde (1): süre tabanı — var/yok yolu aynı aralıkta döner.
        var saat = Stopwatch.StartNew();
        try
        {
            await JetonUretVeGonder(eposta, ip, iptal);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Gönderim/DB hatası yanıtı DEĞİŞTİRMEZ (aksi hâlde "hesap var" sızar);
            // hata GİZLENMEZ: Error düzeyinde günlüğe düşer (Anayasa md.1).
            gunluk.LogError(ex, "Parola sıfırlama bağlantısı üretilemedi/gönderilemedi.");
        }
        var kalan = ayar.YanitTabaniMs - (int)saat.ElapsedMilliseconds;
        if (kalan > 0) await Task.Delay(kalan, iptal);
        return new ParolaSifirlamaIstekSonucu(false, 0);
    }

    private async Task JetonUretVeGonder(string eposta, string ip, CancellationToken iptal)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        // Aynı e-posta birden çok işletmede olabilir (bileşik tekil) — her hesap kendi jetonunu alır.
        var kullanicilar = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.Email == eposta && k.Aktif)
            .Select(k => new { k.Id, k.TenantId, k.Email, k.Ad })
            .Take(5).ToListAsync(iptal);
        if (kullanicilar.Count == 0) return;

        var simdi = DateTime.UtcNow;
        var postalar = new List<(string Alici, string Govde)>();
        foreach (var k in kullanicilar)
        {
            // Madde (2): yeni istek öncekileri geçersiz kılar (izlenen satır → SaveChanges;
            // ExecuteUpdate KULLANILMAZ — yazma denetçisi ve denetim izi görsün).
            var acik = await db.ParolaSifirlamaJetonlari
                .Where(j => j.KullaniciId == k.Id && j.TenantId == k.TenantId
                            && j.Kullanildi == null && !j.IptalEdildi)
                .ToListAsync(iptal);
            foreach (var j in acik) j.IptalEdildi = true;

            var (duz, hash) = JetonUret();
            db.ParolaSifirlamaJetonlari.Add(new ParolaSifirlamaJetonu
            {
                Id = Kimlik.Ulid(),
                TenantId = k.TenantId,
                KullaniciId = k.Id,
                JetonHash = hash,
                Gecerlilik = simdi.AddMinutes(JetonOmruDakika),
                Kullanildi = null,
                IptalEdildi = false,
                IpAdresi = ip.Length > 45 ? ip[..45] : ip,
                Olusturuldu = simdi,
            });
            postalar.Add((k.Email, PostaGovdesi(k.Ad, duz)));
        }
        await db.SaveChangesAsync(iptal);

        foreach (var (alici, govde) in postalar)
            await posta.Gonder(alici, "Mihenk — parola sıfırlama bağlantınız", govde, iptal);
    }

    /// <summary>Madde (2): ≥ 32 bayt kriptografik rastgele → base64url; DB'ye SHA-256 hex.</summary>
    public static (string Duz, string Hash) JetonUret()
    {
        var bayt = RandomNumberGenerator.GetBytes(JetonBayt);
        var duz = Convert.ToBase64String(bayt).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (duz, TokenServisi.Hashle(duz));
    }

    /// <summary>
    /// Bağlantı jetonu PARÇA (#) içinde taşır: tarayıcı parçayı sunucuya, vekil
    /// günlüğüne ya da Referer başlığına göndermez.
    /// </summary>
    public string Baglanti(string duzJeton) =>
        $"{ayar.BaglantiTabani!.TrimEnd('/')}/sifre-sifirla#jeton={duzJeton}";

    private string PostaGovdesi(string ad, string duzJeton) =>
        $"Merhaba {ad},\r\n\r\n" +
        "Mihenk hesabınız için parola sıfırlama isteği aldık. Yeni parola belirlemek için " +
        "aşağıdaki bağlantıyı açın:\r\n\r\n" +
        Baglanti(duzJeton) + "\r\n\r\n" +
        $"Bağlantı {JetonOmruDakika} dakika geçerlidir ve yalnız bir kez kullanılabilir.\r\n" +
        "Bu isteği siz yapmadıysanız bu e-postayı yok sayın; parolanız değişmez.\r\n";

    // ─── Tamamla: yeni parola ─────────────────────────────────────────────────

    public async Task<object> Tamamla(ParolaSifirlamaTamamlaDto dto, CancellationToken iptal)
    {
        // Madde (4): mevcut parola kuralı — servis içinde elle (controller dışı yol da korunur).
        var yeni = dto?.YeniSifre ?? "";
        if (yeni.Length < ParolaEnAz || yeni.Length > ParolaEnFazla)
            throw new BadRequestException($"Yeni şifre {ParolaEnAz}-{ParolaEnFazla} karakter olmalıdır").AlanIle("yeniSifre");
        if (!Regex.IsMatch(yeni, ParolaKalibi))
            throw new BadRequestException("Şifre en az bir harf ve bir rakam içermelidir").AlanIle("yeniSifre");
        if (!string.Equals(yeni, dto!.YeniSifreTekrar, StringComparison.Ordinal))
            throw new BadRequestException("Yeni şifre ve tekrarı eşleşmiyor").AlanIle("yeniSifreTekrar");
        if (string.IsNullOrWhiteSpace(dto.Jeton) || dto.Jeton.Length > 200)
            throw new BadRequestException(JetonRedMesaji);

        var hash = TokenServisi.Hashle(dto.Jeton.Trim());
        var simdi = DateTime.UtcNow;

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var jeton = await db.ParolaSifirlamaJetonlari.FirstOrDefaultAsync(j => j.JetonHash == hash, iptal);
        // Madde (2): tek kullanım + iptal + 30 dk. Hangisi olduğu SIZDIRILMAZ.
        if (jeton is null || jeton.Kullanildi is not null || jeton.IptalEdildi || jeton.Gecerlilik <= simdi)
            throw new BadRequestException(JetonRedMesaji);

        var kullanici = await db.Kullanicilar
            .FirstOrDefaultAsync(k => k.Id == jeton.KullaniciId && k.TenantId == jeton.TenantId, iptal);
        if (kullanici is null || !kullanici.Aktif)
            throw new BadRequestException(JetonRedMesaji);

        await using var tx = await db.Database.BeginTransactionAsync(iptal);

        jeton.Kullanildi = simdi;
        // Aynı kullanıcının diğer açık jetonları da ölür (madde 2: "kullanınca öncekiler geçersiz").
        var digerleri = await db.ParolaSifirlamaJetonlari
            .Where(j => j.KullaniciId == kullanici.Id && j.TenantId == kullanici.TenantId
                        && j.Id != jeton.Id && j.Kullanildi == null && !j.IptalEdildi)
            .ToListAsync(iptal);
        foreach (var j in digerleri) j.IptalEdildi = true;

        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(yeni, BcryptRounds);
        kullanici.Guncellendi = simdi;

        // Madde (4): denetim kaydı — DEĞER yazılmaz (ne parola ne jeton ne özet).
        sistem.ServiceProvider.GetRequiredService<DenetimServisi>().YazmaKaydet(
            new AktifKullanici(kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol),
            "Kullanici", kullanici.Id,
            "parola e-posta bağlantısıyla sıfırlandı (Parolamı unuttum) · tüm oturumlar kapatıldı",
            hassas: true, eylem: DenetimEylemi.Guncelleme);

        try
        {
            await db.SaveChangesAsync(iptal);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Eşzamanlı ikinci "tamamla": jeton bu arada kullanıldı → tek tip red.
            throw new BadRequestException(JetonRedMesaji);
        }

        // Madde (4): kullanıcının TÜM refresh jetonları iptal (JETON mantığıyla aynı yol).
        await AuthServisi.TumTokenlerIptalEt(db, kullanici.Id);
        await tx.CommitAsync(iptal);

        // Madde (5): OTURUM AÇILMAZ — jeton dönmez; giriş (ve MFA) yeniden yapılır.
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz.") };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Route("api/v1/auth/parola-sifirlama")]
public sealed class ParolaSifirlamaController(ParolaSifirlamaServisi servis) : ControllerBase
{
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

    /// <summary>GET /api/v1/auth/parola-sifirlama/durum — özellik açık mı (madde 6). Hesap bilgisi İÇERMEZ.</summary>
    [HttpGet("durum")]
    [AllowAnonymous]
    public object Durum() => new
    {
        acik = servis.Acik,
        mesaj = servis.Acik ? null : Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(ParolaSifirlamaServisi.KapaliMesaji),
        gecerlilikDakika = ParolaSifirlamaServisi.JetonOmruDakika,
    };

    /// <summary>
    /// POST /api/v1/auth/parola-sifirlama/istek — bağlantı iste. Yanıt HER ZAMAN
    /// aynı (madde 1); kanal yoksa 503 (madde 6); e-posta+IP başına 15 dk'da 3 (madde 3).
    /// </summary>
    [HttpPost("istek")]
    [AllowAnonymous]
    [EnableRateLimiting(OranSiniri.ParolaSifirlama)]
    public async Task<IActionResult> Iste([FromBody] ParolaSifirlamaIstekDto dto, CancellationToken iptal)
    {
        var sonuc = await servis.Iste(dto, Ip, iptal);
        if (sonuc.Reddedildi)
        {
            Response.Headers.RetryAfter = sonuc.BekleSaniye.ToString(CultureInfo.InvariantCulture);
            return new ContentResult
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
                ContentType = "application/json",
                Content = JsonSerializer.Serialize(new
                {
                    message = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Çok fazla sıfırlama isteği gönderildi. Lütfen 15 dakika sonra yeniden deneyin."),
                    error = "TooManyRequests",
                    statusCode = 429,
                }),
            };
        }
        return Ok(new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(ParolaSifirlamaServisi.IstekYanitMesaji) });
    }

    /// <summary>POST /api/v1/auth/parola-sifirlama/tamamla — jeton + yeni parola. Oturum AÇMAZ (madde 5).</summary>
    [HttpPost("tamamla")]
    [AllowAnonymous]
    [EnableRateLimiting(OranSiniri.ParolaSifirlama)]
    public Task<object> Tamamla([FromBody] ParolaSifirlamaTamamlaDto dto, CancellationToken iptal) =>
        servis.Tamamla(dto, iptal);
}
