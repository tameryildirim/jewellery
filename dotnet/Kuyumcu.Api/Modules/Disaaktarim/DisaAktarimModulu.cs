using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Disaaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ DIŞA AKTARIMI — PLAN-2 Adım 4
//
// AMAÇ (Eray'ın pazar bulgusu): rakiplerin en zayıf noktası verinin programda
// REHİN kalması. Şikâyetlerin ortak derdi MASAK değil, "verimi alamıyorum".
// Bu modül o rehinliği kırar: müşteri kendi verisini standart CSV olarak alır.
//
// MEHMET'İN 6 ŞARTI (VETO-1 — hepsi karşılanmadan sevk edilemez):
//   1. Yalnız SAHIP; tek tıkla değil, GEREKÇE metni zorunlu     → [RolGerekli] + Gerekce
//   2. Varsayılan maskeli; tam kimlikli mod AYRI ONAY           → Maskeli=true + TamKimlikOnayi
//   3. Her aktarım denetim izine: kim/ne zaman/kaç satır/
//      hangi alanlar/hangi gerekçe                              → DenetimServisi, iki kayıt
//   4. Oran sınırlı                                             → [EnableRateLimiting(DisaAktarim)]
//   5. Dosya şifreli; parola dosyayla AYNI KANALDAN verilmez    → AES-256 ZIP + parola yanıtta
//   6. İndirme bağlantısı kısa ömürlü, TEK KULLANIMLIK          → jeton + SonKullanma + Kullanildi
//
// PAROLA AKIŞI — şart 5'in nasıl karşılandığı:
//   Talep yanıtı (kanal A) parolayı BİR KEZ döndürür; sunucu yalnız SHA-256
//   özetini saklar. İndirme (kanal B) parolayı geri ister. Böylece:
//     • Bağlantıyı ele geçiren dosyayı açamaz (parola yok).
//     • Parolayı gören bağlantı olmadan indiremez.
//     • Sunucu parolayı hiç saklamaz.
//
// SAKLANMAYAN: üretilen dosya diske yazılmaz, indirme anında akışa üretilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DisaAktarimAyarlari
{
    /// <summary>Şart 6: bağlantı ömrü. Dakika.</summary>
    public int JetonOmruDakika { get; set; } = 10;

    /// <summary>Şart 1: gerekçe bu uzunluğun altındaysa talep reddedilir.</summary>
    public int GerekceAsgari { get; set; } = 20;

    /// <summary>Şart 2: tam kimlikli aktarımda birebir yazılması gereken onay metni.</summary>
    public string TamKimlikOnayMetni { get; set; } =
        "TAM KİMLİK BİLGİSİ DIŞA AKTARILACAKTIR";
}

public sealed class DisaAktarimTalepDto
{
    /// <summary>Şart 1 — neden alınıyor? Boş geçilemez.</summary>
    [Required, MaxLength(500)] public string Gerekce { get; set; } = null!;

    /// <summary>Boş bırakılırsa tüm kaynaklar.</summary>
    public string[]? Kapsam { get; set; }

    /// <summary>
    /// Şart 2 — kişisel veri maskesiz istenirse, onay metni BİREBİR yazılmalıdır.
    /// Boşsa aktarım maskeli üretilir.
    /// </summary>
    [MaxLength(100)] public string? TamKimlikOnayi { get; set; }
}

public sealed class DisaAktarimIndirDto
{
    /// <summary>Talep yanıtında bir kez gösterilen parola (kanal A'dan gelir).</summary>
    [Required, StringLength(64, MinimumLength = 8)]
    public string Parola { get; set; } = null!;
}

public sealed class DisaAktarimServisi(
    KuyumcuDbContext db, DenetimServisi denetim, DisaAktarimAyarlari? ayarlar = null)
{
    private readonly DisaAktarimAyarlari ayar = ayarlar ?? new DisaAktarimAyarlari();

    public const string DenetimVarligi = "DisaAktarim";

    // ── 1) Talep ─────────────────────────────────────────────────────────────

    public async Task<object> Talep(DisaAktarimTalepDto dto, AktifKullanici islem)
    {
        // Şart 1: gerekçe. "dışa aktarım" gibi bir kelime gerekçe değildir.
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < ayar.GerekceAsgari)
            throw new BadRequestException(
                $"Dışa aktarım gerekçesi en az {ayar.GerekceAsgari} karakter olmalıdır. " +
                "Bu metin denetim izine yazılır ve silinemez.");

        // Kapsam
        var istenen = dto.Kapsam is { Length: > 0 } ? dto.Kapsam : Kaynaklar.Adlar;
        var kaynaklar = new List<IDisaAktarilabilir>();
        foreach (var ad in istenen)
        {
            var k = Kaynaklar.Bul(ad)
                ?? throw new BadRequestException(
                    $"Bilinmeyen dışa aktarım kaynağı: '{ad}'. " +
                    $"Geçerli değerler: {string.Join(", ", Kaynaklar.Adlar)}");
            if (!kaynaklar.Contains(k)) kaynaklar.Add(k);
        }

        // Şart 2: maskesiz mod ayrı onay ister — ve yalnız kişisel veri varsa anlamlıdır.
        var maskeli = true;
        if (!string.IsNullOrWhiteSpace(dto.TamKimlikOnayi))
        {
            if (dto.TamKimlikOnayi.Trim() != ayar.TamKimlikOnayMetni)
                throw new BadRequestException(
                    $"Tam kimlikli aktarım için onay metni birebir yazılmalıdır: " +
                    $"\"{ayar.TamKimlikOnayMetni}\"");
            maskeli = false;
        }

        var jeton = Jeton();
        var parola = Parola();
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();

        db.Set<DisaAktarimTalebi>().Add(new DisaAktarimTalebi
        {
            Id = id,
            TenantId = islem.TenantId,
            KullaniciId = islem.Id,
            Gerekce = gerekce,
            Kapsam = string.Join(",", kaynaklar.Select(k => k.Ad)),
            Maskeli = maskeli,
            JetonHash = Ozet(jeton),
            ParolaHash = Ozet(parola),
            Olusturuldu = now,
            SonKullanma = now.AddMinutes(ayar.JetonOmruDakika),
        });
        await db.SaveChangesAsync();

        // Şart 3: talep de denetim izine girer — indirme hiç olmasa bile
        // "kim ne istedi" kaydı kalır.
        await denetim.OkumaKaydet(islem, DenetimVarligi, id,
            $"talep · kapsam: {string.Join("+", kaynaklar.Select(k => k.Ad))} · " +
            $"{(maskeli ? "MASKELİ" : "⚠ TAM KİMLİK")} · gerekçe: {gerekce}");

        return new
        {
            talepId = id,
            kapsam = kaynaklar.Select(k => k.Ad).ToArray(),
            maskeli,
            // Jeton ve parola YALNIZ BURADA görünür; sunucuda özetleri saklanır.
            jeton,
            parola,
            sonKullanma = now.AddMinutes(ayar.JetonOmruDakika),
            uyari = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(maskeli
                ? "Dosya AES-256 ile şifrelidir. Parolayı dosyayla aynı kanaldan göndermeyin."
                : "⚠ Bu dosya MASKESİZ kimlik bilgisi içerir. KVKK md.12 gereği " +
                  "aktarım denetim izine işlenmiştir. Parolayı dosyayla aynı kanaldan göndermeyin."),
        };
    }

    // ── 2) İndirme ───────────────────────────────────────────────────────────

    /// <summary>
    /// Jetonu doğrular ve talebi TÜKETİR (şart 6). Doğrulama ile tüketim arasında
    /// boşluk bırakılmaz: aynı jetonla iki eşzamanlı indirme olamaz.
    /// </summary>
    public async Task<DisaAktarimTalebi> JetonuTuket(string jeton, string parola, string? ip)
    {
        var hash = Ozet(jeton);
        var now = DateTime.UtcNow;

        var talep = await db.Set<DisaAktarimTalebi>()
            .FirstOrDefaultAsync(t => t.JetonHash == hash)
            ?? throw new NotFoundException("Dışa aktarım bağlantısı geçersiz");

        if (talep.Kullanildi is not null)
            throw new BadRequestException(
                "Bu dışa aktarım bağlantısı daha önce kullanıldı. Yeni bir talep oluşturun.");

        if (talep.SonKullanma <= now)
            throw new BadRequestException(
                "Dışa aktarım bağlantısının süresi doldu. Yeni bir talep oluşturun.");

        // Parola: sabit zamanlı karşılaştırma — zamanlama sızıntısı bırakmaz.
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Ozet(parola)),
                Encoding.UTF8.GetBytes(talep.ParolaHash)))
            throw new BadRequestException("Parola hatalı");

        talep.Kullanildi = now;
        talep.IpAdresi = ip;
        await db.SaveChangesAsync();

        return talep;
    }

    /// <summary>
    /// Şifreli ZIP'i doğrudan yanıt akışına yazar. Ara bellek ya da geçici dosya
    /// KULLANILMAZ — 8 yıllık veri belleğe alınmaz, diske düz metin düşmez.
    /// </summary>
    public async Task<int> Yaz(Stream hedef, DisaAktarimTalebi talep, string parola)
    {
        var toplam = 0;

        // SharpZipLib: AES-256 (WinZip AE-1). 7-Zip, WinRAR, Keka, Windows Explorer
        // (AES için harici araç) açar — biçim standarttır, müşteri bize bağımlı kalmaz.
        using var zip = new ZipOutputStream(hedef) { IsStreamOwner = false };
        zip.SetLevel(6);
        zip.Password = parola;

        foreach (var ad in talep.Kapsam.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kaynak = Kaynaklar.Bul(ad);
            if (kaynak is null) continue;   // kayıt yazıldıktan sonra kaynak kaldırılmışsa

            // 20.10: kendi dosyasını üreten kaynak (Luca CSV parçaları, Zirve xlsx) — CSV yolu değişmedi.
            if (kaynak is IDosyaUreten uretici)
            {
                await foreach (var (dosyaAdi, icerik, satirSayisi) in uretici.Dosyalar(db, talep.TenantId))
                {
                    zip.PutNextEntry(new ZipEntry(dosyaAdi) { DateTime = DateTime.UtcNow, AESKeySize = 256 });
                    await zip.WriteAsync(icerik);
                    zip.CloseEntry();
                    toplam += satirSayisi;
                }
                continue;
            }

            var giris = new ZipEntry($"{ad}.csv")
            {
                DateTime = DateTime.UtcNow,
                AESKeySize = 256,
            };
            zip.PutNextEntry(giris);

            await zip.WriteAsync(CsvYazici.Bom);
            await zip.WriteAsync(CsvYazici.Bayt(CsvYazici.Satir(kaynak.Basliklar)));

            await foreach (var satir in kaynak.Satirlar(db, talep.TenantId, talep.Maskeli))
            {
                await zip.WriteAsync(CsvYazici.Bayt(CsvYazici.Satir(satir)));
                toplam++;
            }

            zip.CloseEntry();
        }

        // Ne aldığını bilsin: kapsam, maskeleme ve gerekçe dosyanın içinde de dursun.
        zip.PutNextEntry(new ZipEntry("OKUBENI.txt") { AESKeySize = 256 });
        await zip.WriteAsync(CsvYazici.Bayt(Okubeni(talep, toplam)));
        zip.CloseEntry();

        await zip.FlushAsync();
        zip.Finish();
        return toplam;
    }

    /// <summary>Şart 3: indirme tamamlandıktan sonra satır sayısıyla birlikte iz.</summary>
    public async Task IndirmeKaydet(DisaAktarimTalebi talep, int satir, AktifKullanici islem)
    {
        talep.SatirSayisi = satir;
        db.DenetimKayitlari.Add(DenetimServisi.Olustur(
            talep.TenantId, talep.KullaniciId, DenetimEylemi.Okuma, DenetimVarligi, talep.Id,
            $"indirildi · {satir} satır · kapsam: {talep.Kapsam} · " +
            $"{(talep.Maskeli ? "MASKELİ" : "⚠ TAM KİMLİK")} · gerekçe: {talep.Gerekce}",
            null));
        await db.SaveChangesAsync();
    }

    public async Task<object> Gecmis(AktifKullanici islem, int limit)
    {
        var kayitlar = await db.Set<DisaAktarimTalebi>().AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId)
            .OrderByDescending(t => t.Olusturuldu)
            .Take(limit)
            .Select(t => new
            {
                id = t.Id,
                kullaniciId = t.KullaniciId,
                gerekce = t.Gerekce,
                kapsam = t.Kapsam,
                maskeli = t.Maskeli,
                olusturuldu = t.Olusturuldu,
                sonKullanma = t.SonKullanma,
                kullanildi = t.Kullanildi,
                satirSayisi = t.SatirSayisi,
                ipAdresi = t.IpAdresi,
            })
            .ToListAsync();

        return new { toplam = kayitlar.Count, kayitlar };
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static string Okubeni(DisaAktarimTalebi t, int satir) => $"""
        KUYUMCU ERP — VERİ DIŞA AKTARIMI
        ================================
        Talep no        : {t.Id}
        Oluşturulma     : {t.Olusturuldu:yyyy-MM-dd HH:mm:ss} UTC
        Kapsam          : {t.Kapsam}
        Kişisel veri    : {(t.Maskeli ? "MASKELİ (varsayılan)" : "TAM — maskesiz")}
        Toplam satır    : {satir}
        Gerekçe         : {t.Gerekce}

        Biçim
        -----
        • Dosyalar UTF-8 (BOM'lu) CSV, RFC 4180. Excel doğrudan açar.
        • Ondalık ayracı NOKTA, tarihler ISO-8601 (UTC).
        • Bu veri sizindir. Başka bir programa aktarmak için ek izin gerekmez.

        Güvenlik
        --------
        • Bu arşiv AES-256 ile şifrelidir.
        • Parolayı bu dosyayla aynı kanaldan paylaşmayın.
        • Maskesiz kimlik bilgisi içeriyorsa KVKK md.12 gereği korumak
          artık dosyayı indiren tarafın sorumluluğundadır.
        """;

    private static string Jeton() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>
    /// 24 bayt entropi, karışıklık yaratan karakterler ayıklanmış. Kullanıcı bunu
    /// elle bir başka kanala taşıyacak (şart 5) — okunabilirliği güvenliğin parçası.
    /// </summary>
    private static string Parola()
    {
        const string abece = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bayt = RandomNumberGenerator.GetBytes(24);
        return string.Concat(bayt.Select(b => abece[b % abece.Length]));
    }

    private static string Ozet(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
}

// ═════════════════════════════════════════════════════════════════════════════
// UÇLAR
// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/disa-aktarim")]
[Authorize]
// Şart 1: yalnız SAHİP. Sınıf düzeyinde — yeni eklenen bir uç izinsiz açılamaz.
[RolGerekli("SAHIP")]
[Yetki.ModulYetkisi("veri-aktarim")]
public sealed class DisaAktarimController(DisaAktarimServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/disa-aktarim/kaynaklar — Aktarılabilir veri kümeleri</summary>
    [HttpGet("kaynaklar")]
    public object Kaynaklari() => Kaynaklar.Hepsi.Select(k => new
    {
        ad = k.Ad,
        kisiselVeri = k.KisiselVeri,
        alanlar = k.Basliklar,
    });

    /// <summary>POST /api/v1/disa-aktarim — Talep oluştur (gerekçe zorunlu)</summary>
    // Şart 4 — sınır TALEP ve İNDİRME üzerindedir. Kaynak listesi ve geçmiş
    // okumaları kotayı yemez: onlar veri çıkarmaz, denetim ekranını besler.
    [EnableRateLimiting(OranSiniri.DisaAktarim)]
    [HttpPost]
    public Task<object> Talep([FromBody] DisaAktarimTalepDto dto) => servis.Talep(dto, Islem);

    /// <summary>GET /api/v1/disa-aktarim/gecmis — Kim ne zaman ne aktardı</summary>
    [HttpGet("gecmis")]
    public Task<object> Gecmis([FromQuery, Range(1, 200)] int limit = 50) =>
        servis.Gecmis(Islem, limit);

    /// <summary>
    /// POST /api/v1/disa-aktarim/{jeton}/indir — Şifreli ZIP.
    /// Parola gövdede gelir: bağlantı tek başına dosyayı açmaya yetmez.
    /// </summary>
    [EnableRateLimiting(OranSiniri.DisaAktarim)]
    [HttpPost("{jeton}/indir")]
    public async Task Indir(string jeton, [FromBody] DisaAktarimIndirDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var talep = await servis.JetonuTuket(jeton, dto.Parola, ip);

        // Kiracı kapanı: jeton başka bir kiracınınsa buraya gelinmez.
        if (talep.TenantId != Islem.TenantId)
            throw new NotFoundException("Dışa aktarım bağlantısı geçersiz");

        // ⚠ SENKRON YAZMAYA İZİN — yalnız bu istek için.
        // SharpZipLib'in ZipOutputStream'i (CloseEntry/Finish) senkron yazar;
        // Kestrel bunu varsayılan olarak yasaklar. İki alternatif vardı:
        //   (a) arşivi belleğe/geçici dosyaya kurup sonra async kopyalamak —
        //       8 yıllık veriyi belleğe almak bu adımın tasarım kararına aykırı,
        //       geçici dosya ise sunucuda artefakt bırakır;
        //   (b) bu bayrağı yalnız bu istek için açmak.
        // (b) seçildi: uç SAHİP'e kapalı, saatte 5 istekle sınırlı ve akış
        // doğrudan istemciye gidiyor. Bayrak isteğe özeldir, sunucu geneline değil.
        var govdeDenetimi = HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (govdeDenetimi is not null) govdeDenetimi.AllowSynchronousIO = true;

        var dosyaAdi = $"kuyumcu-disa-aktarim-{talep.Olusturuldu:yyyyMMdd-HHmmss}.zip";
        Response.ContentType = "application/zip";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{dosyaAdi}\"";
        // Şifreli içerik tarayıcıda/vekilde önbelleğe alınmaz.
        Response.Headers.CacheControl = "no-store";

        var satir = await servis.Yaz(Response.Body, talep, dto.Parola);
        await servis.IndirmeKaydet(talep, satir, Islem);
    }
}
