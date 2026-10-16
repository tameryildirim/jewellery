using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kurulum;

// ═════════════════════════════════════════════════════════════════════════════
// KURULUŞ SİHİRBAZI — paket T-2 Faz A
//
// Sihirbaz İNCE SÜRÜCÜDÜR: kendi şube/kullanıcı/kur yazma yolunu AÇMAZ.
//
// ⚠ TASARIMDAN SAPMA (raporda gerekçeli): tasarım metni POST /kurulum/sube,
//   POST /kurulum/kullanici ve bir MANUAL kur adımı için ayrı uçlar öneriyordu.
//   Bunlar YAZILMADI: üçünün de sunucu karşılığı zaten vardır
//   (POST /api/v1/subeler · POST /api/v1/kullanicilar · POST /api/v1/kur/manuel)
//   ve "ince sürücü" bile olsa ikinci bir uç yüzeyi açmak Bertan'ın "ikiz yol
//   doğmasın" koşuluna terstir. Sihirbaz ekranı MEVCUT uçları çağırır; bu
//   modül yalnız DURUM, MÜHÜR ve AÇILIŞ'ı yönetir.
//
// ⚠ MÜHÜR ZORUNLUDUR (Mehmet koşulu 2). İki kapı birliktedir:
//   (1) mühürlendikten sonra açılış uçları 409 verir;
//   (2) mühürlenmese bile AÇILIŞ PENCERESİ (varsayılan 30 gün, yapılandırmadan)
//       dolduğunda açılış uçları 409 verir. Böylece mühürsüz bırakılan kiracıda
//       açılış yüzeyi SÜRESİZ açık kalmaz.
//   İki 409 de T-1'in /saglik/hazir semantiğinden AYRI hata adıyla döner.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>409 hata adları — T-1'in sağlık/hazırlık 409'undan ayrışır.</summary>
public static class KurulumHataAdi
{
    public const string Muhurlu = "Acilis Muhurlu";
    public const string PencereKapandi = "Acilis Penceresi Kapandi";
}

/// <summary>Kurulum tavanları — ADR-0005: rakam koda GÖMÜLMEZ.</summary>
public sealed class KurulumAyarlari
{
    /// <summary>
    /// Mühür konmasa bile açılış uçlarının açık kalacağı gün sayısı
    /// (Mehmet koşulu 2). 0 ya da negatif = pencere kapalı değil demektir,
    /// bu yüzden alt sınır 1'dir.
    /// </summary>
    public int AcilisPenceresiGun { get; init; } = 30;

    /// <summary>Bir uygula çağrısında işlenebilecek en fazla açılış satırı.</summary>
    public int MaksSatir { get; init; } = 500;
}

public sealed class KurulumDurumGuncelleDto
{
    /// <summary>
    /// Kullanıcının ATLADIĞI adımlar. PATCH sözleşmesi (UX-S3): gövdede YOKSA
    /// korunur, <c>null</c> ise silinir, dizi geldiyse tümüyle değiştirilir.
    /// </summary>
    public List<string>? AtlananAdimlar { get; set; }
}

/// <summary>Kurulum ekranındaki sayı rozetleri — her biri bir kayıt kümesini gösterir (UX-S23).</summary>
public sealed record KurulumSayaclari(
    int Sube, int Kullanici, int Kur, int Kasa, int Banka,
    int AcilisCari, int AcilisKasa, int AcilisBanka);

public sealed class KurulumMuhurDto
{
    [Required, MinLength(20), MaxLength(500)] public string Gerekce { get; set; } = "";

    /// <summary>Geri dönülemez işlem — ekranın onay modalından gelen bayrak.</summary>
    public bool Onay { get; set; }

    /// <summary>
    /// PAKET P-49 — İKİNCİ KAPI (type-to-confirm). Kullanıcı işletme adını
    /// BİREBİR yazar; sunucu kiracının kayıtlı adıyla karşılaştırır. Ekran
    /// atlanıp uç doğrudan çağrılsa bile kapı ayakta kalır. Boş/yanlış → 400.
    /// DataAnnotations yok: kural servis içinde elle denetlenir.
    /// </summary>
    public string? IsletmeAdiOnayi { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KurulumServisi(
    KuyumcuDbContext db,
    DenetimServisi denetim,
    ILogger<KurulumServisi> logger,
    IConfiguration? yapilandirma = null)
{
    public const int GerekceAsgari = 20;

    private KurulumAyarlari Ayar =>
        yapilandirma?.GetSection("Kurulum").Get<KurulumAyarlari>() ?? new KurulumAyarlari();

    /// <summary>
    /// Adımların GERÇEK durumu — elle yazılan bir "bitti" bayrağı YOKTUR
    /// (UX-S21). Bayrak tutulsaydı veriyle çelişebilirdi: kullanıcı kasayı
    /// silse bile adım "bitti" görünürdü.
    /// </summary>
    private async Task<(KurulumDurumu? Satir, Dictionary<string, bool> Bitti,
        string[] Atlanan, KurulumSayaclari Sayaclar)> AdimDurumlari(string tenantId)
    {
        var satir = await db.KurulumDurumlari.AsNoTracking()
            .FirstOrDefaultAsync(k => k.TenantId == tenantId);

        var subeAdet = await db.Subeler.CountAsync(s => s.Aktif);
        var kullaniciAdet = await db.Kullanicilar.CountAsync(k => k.Aktif);
        var kurAdet = await db.KurSnapshots.CountAsync();
        var kasaAdet = await db.Kasalar.CountAsync(k => k.Aktif);
        var bankaAdet = await db.BankaHesaplari.CountAsync(h => h.Aktif);
        var tenantAdi = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId).Select(t => t.Ad).FirstOrDefaultAsync();

        var kalemler = await db.AcilisKalemleri.AsNoTracking()
            .Where(k => k.Durum == AcilisKalemDurumu.Uygulandi)
            .GroupBy(k => k.Tur)
            .Select(g => new { Tur = g.Key, Adet = g.Count() })
            .ToListAsync();

        var atlanan = (satir?.AtlananAdimlar ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var bitti = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [KurulumAdimlari.Isletme] = !string.IsNullOrWhiteSpace(tenantAdi) && subeAdet > 0,
            [KurulumAdimlari.Kullanicilar] = kullaniciAdet > 0,
            [KurulumAdimlari.Kur] = kurAdet > 0,
            [KurulumAdimlari.KasaBanka] = kasaAdet > 0 || bankaAdet > 0,
            [KurulumAdimlari.CariAcilis] = kalemler.Any(k => k.Tur == AcilisTuru.Cari),
            // Faz B: stok açılışı bu sürümde yoktur; adım her zaman "eksik"
            // görünmesin diye ZORUNLU listesinde de değildir.
            [KurulumAdimlari.StokAcilis] = kalemler.Any(k => k.Tur == AcilisTuru.Stok),
        };

        var sayaclar = new KurulumSayaclari(
            subeAdet, kullaniciAdet, kurAdet, kasaAdet, bankaAdet,
            kalemler.FirstOrDefault(k => k.Tur == AcilisTuru.Cari)?.Adet ?? 0,
            kalemler.FirstOrDefault(k => k.Tur == AcilisTuru.Kasa)?.Adet ?? 0,
            kalemler.FirstOrDefault(k => k.Tur == AcilisTuru.Banka)?.Adet ?? 0);

        return (satir, bitti, atlanan, sayaclar);
    }

    /// <summary>Mühür için eksik kalan ZORUNLU adımlar (atlananlar sayılmaz).</summary>
    private async Task<string[]> EksikZorunluAdimlar(string tenantId)
    {
        var (_, bitti, atlanan, _) = await AdimDurumlari(tenantId);
        return KurulumAdimlari.Zorunlu
            .Where(a => !bitti[a] && !atlanan.Contains(a, StringComparer.Ordinal))
            .ToArray();
    }

    /// <summary>Sihirbazın TEK GERÇEĞİ.</summary>
    public async Task<object> Durum(AktifKullanici islem)
    {
        var (satir, bitti, atlanan, sayaclar) = await AdimDurumlari(islem.TenantId);

        var eksikZorunlu = KurulumAdimlari.Zorunlu
            .Where(a => !bitti[a] && !atlanan.Contains(a, StringComparer.Ordinal))
            .ToArray();

        var pencereBitis = (satir?.Olusturuldu ?? DateTime.UtcNow)
            .AddDays(Math.Max(1, Ayar.AcilisPenceresiGun));

        return new
        {
            muhurlendi = satir?.Muhurlendi ?? false,
            muhurTarihi = satir?.MuhurTarihi,
            muhurleyenKullaniciId = satir?.MuhurleyenKullaniciId,
            muhurGerekcesi = satir?.MuhurGerekcesi,
            acilisPenceresiBitis = pencereBitis,
            acilisPenceresiAcik = !(satir?.Muhurlendi ?? false) && DateTime.UtcNow <= pencereBitis,
            muhurlenebilir = eksikZorunlu.Length == 0 && !(satir?.Muhurlendi ?? false),
            eksikAdimlar = eksikZorunlu,
            atlananAdimlar = atlanan,
            adimlar = KurulumAdimlari.Tumu.Select(a => new
            {
                anahtar = a,
                bitti = bitti[a],
                zorunlu = KurulumAdimlari.Zorunlu.Contains(a),
                atlandi = atlanan.Contains(a, StringComparer.Ordinal),
            }),
            sayilar = new
            {
                sube = sayaclar.Sube, kullanici = sayaclar.Kullanici, kur = sayaclar.Kur,
                kasa = sayaclar.Kasa, banka = sayaclar.Banka,
                acilisCari = sayaclar.AcilisCari,
                acilisKasa = sayaclar.AcilisKasa,
                acilisBanka = sayaclar.AcilisBanka,
            },
            maksSatir = Ayar.MaksSatir,
            gerekceAsgari = GerekceAsgari,
            // P-49: mühür onayında yazdırılacak ad — sistem bilir, ekran sormaz (UX-S21).
            isletmeAdi = await IsletmeAdi(islem.TenantId),
            // P-68: açılışın kabul ettiği para birimleri — TEK KAYNAK AcilisServisi.
            // Ekran hedef seçicisini buna göre süzer (sabit liste yazmaz).
            acilisParaBirimleri = AcilisServisi.KabulEdilenParaBirimleri,
        };
    }

    /// <summary>Adım atlama/geri dönme tercihi. Mühürlüyse 409.</summary>
    public async Task<object> DurumGuncelle(KurulumDurumGuncelleDto dto, AktifKullanici islem)
    {
        await MuhurDogrula(islem.TenantId);

        var now = DateTime.UtcNow;
        var satir = await SatirGuvence(islem.TenantId, now);

        // PATCH sözleşmesi: gövdede YOKSA korunur, null ise silinir.
        if (dto.AtlananAdimlar is { } liste)
        {
            var gecersiz = liste.Where(a => !KurulumAdimlari.Tumu.Contains(a, StringComparer.Ordinal)).ToList();
            if (gecersiz.Count > 0)
                throw new BadRequestException($"Bilinmeyen kurulum adımı: {string.Join(", ", gecersiz)}");
            satir.AtlananAdimlar = liste.Count == 0 ? null : string.Join(',', liste.Distinct());
        }

        satir.Guncellendi = now;
        await db.SaveChangesAsync();
        return await Durum(islem);
    }

    /// <summary>
    /// Açılışı KAPATIR. Geri dönülemez: sonrasında açılış uçları 409 verir ve
    /// düzeltme yalnız normal defter yoluyla (cari düzeltme fişi / storno) yapılır.
    /// </summary>
    public async Task<object> Muhurle(KurulumMuhurDto dto, AktifKullanici islem)
    {
        await MuhurDogrula(islem.TenantId);

        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < GerekceAsgari)
            throw new BadRequestException($"Mühür gerekçesi en az {GerekceAsgari} karakter olmalıdır");
        if (gerekce.Length > 500)
            throw new BadRequestException("Mühür gerekçesi en çok 500 karakter olabilir");
        if (!dto.Onay)
            throw new BadRequestException("Mühürleme geri alınamaz — onay kutusu işaretlenmelidir");
        var isletmeAdi = await IsletmeAdi(islem.TenantId);
        if (!IsletmeAdiEslesir(dto.IsletmeAdiOnayi, isletmeAdi))
            throw new BadRequestException(
                "Mühürleme geri alınamaz — onaylamak için işletme adını birebir yazın");

        var eksik = await EksikZorunluAdimlar(islem.TenantId);
        if (eksik.Length > 0)
            throw new BadRequestException(
                $"Zorunlu kurulum adımları tamamlanmadan mühür konamaz: {string.Join(", ", eksik)}");

        var now = DateTime.UtcNow;
        var satir = await SatirGuvence(islem.TenantId, now);
        satir.Muhurlendi = true;
        satir.MuhurTarihi = now;
        satir.MuhurleyenKullaniciId = islem.Id;
        satir.MuhurGerekcesi = gerekce;
        satir.Guncellendi = now;

        denetim.YazmaKaydet(islem, "ACILIS", satir.Id, $"kurulum MÜHÜRLENDİ · {gerekce}");
        await db.SaveChangesAsync();

        logger.LogInformation("[{TenantId}] Kurulum mühürlendi: kullanici={KullaniciId}",
            islem.TenantId, islem.Id);

        return await Durum(islem);
    }

    /// <summary>Kiracının kayıtlı işletme adı (mühür onayında karşılaştırılır).</summary>
    private Task<string?> IsletmeAdi(string tenantId) =>
        db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.Ad).FirstOrDefaultAsync();

    /// <summary>
    /// P-49 type-to-confirm kuralı — TEK KAYNAK. Baştaki/sondaki boşluk dışında
    /// BİREBİR (büyük/küçük harf duyarlı, sıra/kültür bağımsız) eşleşme.
    /// Kayıtlı ad boşsa fail-closed: hiçbir girdi eşleşmez.
    /// </summary>
    public static bool IsletmeAdiEslesir(string? girilen, string? kayitli)
    {
        var k = kayitli?.Trim() ?? "";
        if (k.Length == 0) return false;
        return string.Equals((girilen ?? "").Trim(), k, StringComparison.Ordinal);
    }

    // ── Kapılar ─────────────────────────────────────────────────────────────

    /// <summary>Mühürlüyse 409 (ayrı hata adıyla).</summary>
    public async Task MuhurDogrula(string tenantId)
    {
        var muhurlu = await db.KurulumDurumlari.AsNoTracking()
            .AnyAsync(k => k.TenantId == tenantId && k.Muhurlendi);
        if (muhurlu)
            throw new ApiException(409,
                "Kurulum mühürlenmiştir; açılış bakiyesi artık değiştirilemez. " +
                "Düzeltme için cari düzeltme fişini ya da ters kaydı kullanın.",
                KurulumHataAdi.Muhurlu);
    }

    /// <summary>
    /// Satır tavanı — rakam KODA GÖMÜLÜ DEĞİL, yapılandırmadan okunur
    /// (Aşkın'ın 47 koşulu; testler de rakamı sabitlemez, buradan okur).
    /// </summary>
    public void SatirTavaniDogrula(int adet)
    {
        if (adet > Ayar.MaksSatir)
            throw new BadRequestException(
                $"Bir açılış işleminde en fazla {Ayar.MaksSatir} satır olabilir (gelen: {adet}). " +
                "Listeyi bölerek uygulayın.");
    }

    /// <summary>Açılış penceresi dolduysa 409 (Mehmet koşulu 2).</summary>
    public async Task AcilisPenceresiDogrula(string tenantId)
    {
        var satir = await db.KurulumDurumlari.AsNoTracking()
            .FirstOrDefaultAsync(k => k.TenantId == tenantId);
        if (satir is null) return;   // pencere henüz başlamadı

        var bitis = satir.Olusturuldu.AddDays(Math.Max(1, Ayar.AcilisPenceresiGun));
        if (DateTime.UtcNow > bitis)
            throw new ApiException(409,
                $"Açılış penceresi {bitis:yyyy-MM-dd} tarihinde kapandı. " +
                "Bakiye düzeltmesi artık normal defter yollarıyla yapılır.",
                KurulumHataAdi.PencereKapandi);
    }

    /// <summary>
    /// Kurulum satırını GÜVENCEYE alır (yoksa açar) ve pencereyi başlatır.
    /// <para>⚠ SaveChanges ÇAĞIRMAZ: çağıranın transaction'ı içinde yazılır.</para>
    /// </summary>
    public async Task<KurulumDurumu> SatirGuvence(string tenantId, DateTime now)
    {
        var satir = await db.KurulumDurumlari.FirstOrDefaultAsync(k => k.TenantId == tenantId);
        if (satir is not null) return satir;

        satir = new KurulumDurumu
        {
            Id = Kimlik.Ulid(),
            TenantId = tenantId,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.KurulumDurumlari.Add(satir);
        return satir;
    }

    /// <summary>Açılış uygulanınca kurulum satırının varlığını ve damgasını tazeler.</summary>
    public async Task AdimIsaretle(string tenantId, DateTime now)
    {
        var satir = await SatirGuvence(tenantId, now);
        satir.Guncellendi = now;
    }
}

// ── Uçlar ────────────────────────────────────────────────────────────────────

/// <summary>
/// Kurulum uçları. Sınıf düzeyinde <c>[RolGerekli("SAHIP","MUDUR")]</c>: yeni
/// bir uç izinsiz açılamaz. YAZMA uçlarında ayrıca metot düzeyinde
/// <c>[RolGerekli("SAHIP")]</c> vardır — iki filtre de koşar, yani MÜDÜR
/// durumu GÖRÜR ama açılış YAZAMAZ (403).
/// </summary>
[ApiController]
[Route("api/v1/kurulum")]
[Authorize]
[RolGerekli("SAHIP", "MUDUR")]
[Yetki.ModulYetkisi("kurulum")]
public sealed class KurulumController(KurulumServisi servis, AcilisServisi acilis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kurulum/durum — adımlar, eksikler, mühür ve pencere bilgisi.</summary>
    [HttpGet("durum")]
    public Task<object> Durum() => servis.Durum(Islem);

    /// <summary>PATCH /api/v1/kurulum/durum — adım atlama tercihi (UX-S3).</summary>
    [HttpPatch("durum")]
    [RolGerekli("SAHIP")]
    public Task<object> DurumGuncelle([FromBody] KurulumDurumGuncelleDto dto)
        => servis.DurumGuncelle(dto, Islem);

    /// <summary>POST /api/v1/kurulum/muhurle — açılışı kapatır (geri alınamaz).</summary>
    [HttpPost("muhurle")]
    [RolGerekli("SAHIP")]
    public Task<object> Muhurle([FromBody] KurulumMuhurDto dto) => servis.Muhurle(dto, Islem);

    /// <summary>POST /api/v1/kurulum/acilis/prova — KURU PROVA, hiçbir yazma yok.</summary>
    [HttpPost("acilis/prova")]
    [RolGerekli("SAHIP")]
    public Task<object> Prova([FromBody] AcilisProvaDto dto) => acilis.Prova(dto, Islem);

    /// <summary>POST /api/v1/kurulum/acilis/uygula — tek transaction uygulama.</summary>
    [HttpPost("acilis/uygula")]
    [RolGerekli("SAHIP")]
    public Task<object> Uygula([FromBody] AcilisUygulaDto dto) => acilis.Uygula(dto, Islem);

    /// <summary>POST /api/v1/kurulum/acilis/geri-al — mühürsüz oturumu ters kayıtla geri alır.</summary>
    [HttpPost("acilis/geri-al")]
    [RolGerekli("SAHIP")]
    public Task<object> GeriAl([FromBody] AcilisGeriAlDto dto) => acilis.GeriAl(dto, Islem);
}
