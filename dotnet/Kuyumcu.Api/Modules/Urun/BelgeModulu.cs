using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

/// <summary>
/// Belge yükleme gövdesi — base64 (JSON ile, multipart altyapısı yok).
///
/// ⚠ <see cref="SahipTuru"/>/<see cref="SahipId"/> genel uçta (POST /belgeler)
/// ZORUNLUDUR; eski ürün ucu (POST /urunler/{id}/belgeler) bunları YOLDAN
/// doldurur ve gövdede beklemez — A16'nın canlı kanıtı o yol üzerinden alındı.
/// </summary>
public sealed class BelgeYukleDto
{
    [MaxLength(20)] public string? SahipTuru { get; set; }
    [MaxLength(36)] public string? SahipId { get; set; }
    [MaxLength(20)] public string? Tur { get; set; }
    [Required, MaxLength(200)] public string Ad { get; set; } = null!;
    /// <summary>Base64 içerik. Ham tavan: PDF 5 MB, JPEG/PNG 2 MB (yapılandırmadan).</summary>
    [Required] public string IcerikBase64 { get; set; } = null!;
}

public sealed class BelgeServisi(KuyumcuDbContext db, BelgeAyarlari? ayarlar = null)
{
    // ⚠ Aşkın'ın koşulu: ctor parametresi VARSAYILAN DEĞERLİ — mevcut çağrılar
    // (PirlantaBelgeTestleri vb.) değişmeden derlenir; test küçük tavan verebilir.
    private readonly BelgeAyarlari ayar = ayarlar ?? new BelgeAyarlari();

    // ── Genel yol (paket 4) ──────────────────────────────────────────────────

    public async Task<object> Ekle(BelgeYukleDto dto, AktifKullanici islem)
    {
        var sahipTuru = (dto.SahipTuru ?? "").Trim();
        var sahipId = (dto.SahipId ?? "").Trim();
        var tur = string.IsNullOrWhiteSpace(dto.Tur) ? BelgeTuru.Sertifika : dto.Tur.Trim();

        if (!BelgeSahipTuru.Tumu.Contains(sahipTuru))
            throw new BadRequestException(
                $"Geçersiz sahip türü: {(sahipTuru.Length == 0 ? "(boş)" : sahipTuru)} " +
                $"({string.Join(", ", BelgeSahipTuru.Tumu)})");
        if (sahipId.Length == 0) throw new BadRequestException("Sahip kimliği zorunludur");

        await SahipDogrula(sahipTuru, sahipId, islem);

        byte[] icerik;
        try { icerik = Convert.FromBase64String(dto.IcerikBase64); }
        catch (FormatException) { throw new BadRequestException("Belge içeriği geçerli base64 değil"); }

        BelgeKapisi.IcerikDogrula(icerik, tur, out var mime, ayar);
        await TavanDogrula(sahipTuru, sahipId, icerik.Length, islem);

        var b = new Belge
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId,
            SahipTuru = sahipTuru, SahipId = sahipId, Tur = tur,
            Ad = dto.Ad.Trim(), Mime = mime, Boyut = icerik.Length,
            Sha256 = Convert.ToHexString(SHA256.HashData(icerik)).ToLowerInvariant(),
            Icerik = icerik, YukleyenKullaniciId = islem.Id, Olusturuldu = DateTime.UtcNow,
        };
        db.Belgeler.Add(b);
        await db.SaveChangesAsync();
        return Ozet(b);
    }

    /// <summary>
    /// Sahip FAIL-CLOSED doğrulanır: tür kapalı listede olmalı VE o türün
    /// tablosunda AYNI KİRACIDA bir satır bulunmalı. Bulunamazsa 404 —
    /// "sahipsiz belge" (yetim kayıt) yazılamaz.
    /// </summary>
    private async Task SahipDogrula(string sahipTuru, string sahipId, AktifKullanici islem)
    {
        switch (sahipTuru)
        {
            case BelgeSahipTuru.Urun:
                if (!await db.Urunler.AnyAsync(u => u.Id == sahipId && u.TenantId == islem.TenantId))
                    throw new NotFoundException("Ürün bulunamadı");
                return;

            case BelgeSahipTuru.Isletme:
                // ⚠ Mehmet K4-2: Tenant'ta TenantId YOK (filtre Id üzerinden kurulur),
                // bu yüzden db.Tenants sorgusu değil KOŞULSUZ eşitlik zorlanır.
                // Başka kiracının kimliği verilirse 404 — varlık sızdırılmaz.
                if (sahipId != islem.TenantId) throw new NotFoundException("İşletme bulunamadı");
                return;

            case BelgeSahipTuru.Siparis:
                // ⚠ Bertan'ın koşulu: dal BOŞ BIRAKILMAZ. Sipariş modülü (paket 3)
                // gelene kadar açıkça reddedilir; varsayım yapılmaz.
                throw new BadRequestException("Sipariş modülü henüz yok — SIPARIS sahibine belge bağlanamaz");

            case BelgeSahipTuru.GiderPusulasi:
                // ⚠ Mehmet K4-3 (VETO koşulu): imzalı pusula fotoğrafı satıcının
                // TCKN'sini ve imzasını GÖRÜNTÜ olarak taşır — bu, paketin kapsam
                // dışı bıraktığı "cari kimlik belgesi" sınıfının ta kendisidir
                // (blob maskelenemez, ADR-0008 ile çelişir). Ayrı karara bağlandı.
                throw new BadRequestException(
                    "Gider pusulası eki ayrı karara bağlandı (ADR-0008 blob karşılığı) — henüz açık değil");

            case BelgeSahipTuru.KimlikTespit:
                // D1-13: kimlik tespit formuna ekranda imza — sahip AYNI KİRACIDAKİ tespit kaydı.
                if (!await db.MasakKimlikTespitleri.AnyAsync(m => m.Id == sahipId && m.TenantId == islem.TenantId))
                    throw new NotFoundException("Kimlik tespiti bulunamadı");
                return;

            case BelgeSahipTuru.Satis:
                // D1-13: garanti kartına ekranda imza — sahip AYNI KİRACIDAKİ SATIS satırı.
                if (!await db.Hareketler.AnyAsync(h => h.Id == sahipId && h.TenantId == islem.TenantId && h.Tur == "SATIS"))
                    throw new NotFoundException("Satış bulunamadı");
                return;

            case BelgeSahipTuru.HurdaAlis:
                // D1-12 (18.10, rakip eşitleme): hurda alımına fotoğraf. Sahip AYNI KİRACIDAKİ HURDA_ALIS satırıdır;
                // başka tür ya da başka kiracı 404 (varlık sızdırılmaz). Defter satırı DEĞİŞMEZ — fotoğraf yanındadır.
                if (!await db.Hareketler.AnyAsync(h => h.Id == sahipId && h.TenantId == islem.TenantId && h.Tur == "HURDA_ALIS"))
                    throw new NotFoundException("Hurda alımı bulunamadı");
                return;

            default:
                throw new BadRequestException($"Geçersiz sahip türü: {sahipTuru}");
        }
    }

    /// <summary>
    /// Tavanlar (ADR-0005, hepsi yapılandırmadan). Mesajda MEVCUT kullanım da
    /// yazılır — kullanıcı ne yapacağını bilsin.
    ///
    /// ⚠ Bertan'ın koşulu: toplam SUM'ı LONGBLOB'a DOKUNMAZ; yalnız `boyut`
    /// kolonu seçilir. Varlık materyalize edilseydi her yüklemede yüzlerce MB
    /// okunurdu.
    /// </summary>
    private async Task TavanDogrula(string sahipTuru, string sahipId, int yeniBoyut, AktifKullanici islem)
    {
        var sahipAktif = await db.Belgeler.AsNoTracking()
            .CountAsync(b => b.TenantId == islem.TenantId && b.SahipTuru == sahipTuru
                             && b.SahipId == sahipId && b.Aktif);
        if (sahipAktif >= ayar.SahipBasinaAzami)
            throw new BadRequestException(
                $"Bu kayda en fazla {ayar.SahipBasinaAzami} aktif belge bağlanabilir (mevcut: {sahipAktif})");

        var toplam = await db.Belgeler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId && b.Aktif)
            .SumAsync(b => (long?)b.Boyut) ?? 0L;
        if (toplam + yeniBoyut > ayar.KiraciToplamBayt)
            throw new BadRequestException(
                $"Belge deposu tavanı aşıldı: mevcut {toplam / 1024} KB, " +
                $"eklenmek istenen {yeniBoyut / 1024} KB, tavan {ayar.KiraciToplamMb} MB");
    }

    /// <summary>
    /// Sahibe bağlı aktif belgeler. ⚠ İÇERİK DÖNDÜRMEZ — 20 belgelik bir liste
    /// 40 MB JSON üretemez. En fazla <c>SahipBasinaAzami</c> satır.
    /// </summary>
    public async Task<List<object>> ListeGenel(string sahipTuru, string sahipId, AktifKullanici islem)
    {
        if (!BelgeSahipTuru.Tumu.Contains(sahipTuru))
            throw new BadRequestException(
                $"Geçersiz sahip türü: {sahipTuru} ({string.Join(", ", BelgeSahipTuru.Tumu)})");
        if (string.IsNullOrWhiteSpace(sahipId))
            throw new BadRequestException("Sahip kimliği zorunludur");

        return (await db.Belgeler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId && b.SahipTuru == sahipTuru
                        && b.SahipId == sahipId && b.Aktif)
            .OrderByDescending(b => b.Olusturuldu)
            .Take(ayar.SahipBasinaAzami)
            .Select(b => new { b.Id, b.Ad, b.Tur, b.Mime, b.Boyut, b.Sha256, b.Olusturuldu })
            .ToListAsync()).Cast<object>().ToList();
    }

    // ── Eski ürün yolu (KORUNUR, davranışı değişmez) ─────────────────────────

    public Task<object> Yukle(string urunId, BelgeYukleDto dto, AktifKullanici islem)
    {
        dto.SahipTuru = BelgeSahipTuru.Urun;
        dto.SahipId = urunId;
        dto.Tur ??= BelgeTuru.Sertifika;
        return Ekle(dto, islem);
    }

    public Task<List<object>> Liste(string urunId, AktifKullanici islem) =>
        ListeGenel(BelgeSahipTuru.Urun, urunId, islem);

    // ── Ortak ────────────────────────────────────────────────────────────────

    /// <summary>
    /// ⚠ Kiracı dışıysa 404 (403 DEĞİL): varlığın var olduğu bilgisi de sızmaz.
    /// TenantId şartı burada AÇIKÇA yazılır — global filtre ikinci katmandır,
    /// tek katman değildir (ADR-0010).
    /// </summary>
    public async Task<Belge> Icerik(string belgeId, AktifKullanici islem) =>
        await db.Belgeler.AsNoTracking().FirstOrDefaultAsync(b => b.Id == belgeId && b.TenantId == islem.TenantId && b.Aktif)
        ?? throw new NotFoundException("Belge bulunamadı");

    /// <summary>PASİFE ALIR, SİLMEZ — tüketici ispatı korunur.</summary>
    public async Task<object> PasifeAl(string belgeId, AktifKullanici islem)
    {
        var b = await db.Belgeler.FirstOrDefaultAsync(x => x.Id == belgeId && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Belge bulunamadı");
        b.Aktif = false;
        await db.SaveChangesAsync();
        return Ozet(b);
    }

    private static object Ozet(Belge b) => new
    {
        b.Id, b.SahipTuru, b.SahipId, b.Ad, b.Tur, b.Mime, b.Boyut, b.Sha256, b.Olusturuldu, b.Aktif,
    };
}

// ═════════════════════════════════════════════════════════════════════════════
// GENEL BELGE UÇLARI (paket 4)
// ═════════════════════════════════════════════════════════════════════════════

[ApiController, Authorize]
[Route("api/v1/belgeler")]
[Yetki.ModulYetkisi("urun")]
public sealed class BelgeController(BelgeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpGet]
    public Task<List<object>> Liste([FromQuery] string? sahipTuru, [FromQuery] string? sahipId) =>
        servis.ListeGenel(sahipTuru ?? "", sahipId ?? "", Islem);

    /// <summary>
    /// ⚠ Mehmet K4-1: küresel gövde sınırı GEVŞETİLMEZ (Kestrel varsayılanı 30 MB,
    /// Program.cs'te ayar YOK — ölçüldü). Yalnız bu uç 8 MB ile SINIRLANIR
    /// (5 MB ham PDF base64'te ~6,7 MB gövde eder).
    /// ⚠ K4-5: rol kapısı ATTRIBUTE düzeyinde. K4-6: ayrı oran sınırı yalnız POST'ta.
    /// </summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    [EnableRateLimiting(OranSiniri.Belge)]
    [RequestSizeLimit(8_000_000)]
    public Task<object> Yukle([FromBody] BelgeYukleDto dto) => servis.Ekle(dto, Islem);

    [HttpGet("{belgeId}/icerik")]
    public async Task<IActionResult> Icerik(string belgeId)
    {
        // ⚠ Mehmet K4-7: kiracı/aktif şartı 304'ten ÖNCE doğrulanır. If-None-Match
        // ile yabancı kiracının belgesinin VARLIĞI bile sızmamalı → 404, 304 değil.
        var b = await servis.Icerik(belgeId, Islem);
        return BelgeYaniti(b);
    }

    [HttpDelete("{belgeId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> PasifeAl(string belgeId) => servis.PasifeAl(belgeId, Islem);

    /// <summary>
    /// ETag = sha256, Cache-Control: private, must-revalidate.
    /// ⚠ K4-4: akış <c>Content-Disposition: attachment</c> ile döner (inline DEĞİL).
    /// </summary>
    internal IActionResult BelgeYaniti(Belge b)
    {
        var etag = $"\"{b.Sha256}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, must-revalidate";
        if (Request.Headers.IfNoneMatch.Any(v => v == etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return File(b.Icerik, b.Mime, BelgeAdi(b));
    }

    internal static string BelgeAdi(Belge b)
    {
        var uzanti = b.Mime switch
        {
            BelgeKapisi.MimeJpeg => ".jpg",
            BelgeKapisi.MimePng => ".png",
            _ => ".pdf",
        };
        return b.Ad.EndsWith(uzanti, StringComparison.OrdinalIgnoreCase) ? b.Ad : b.Ad + uzanti;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ESKİ ÜRÜN UÇLARI — KORUNUR, DAVRANIŞI DEĞİŞMEZ (A16 canlı kanıtı bu yollardan
// alındı). Yeni servise sahipTuru='URUN' ile delege eder.
// ═════════════════════════════════════════════════════════════════════════════

[ApiController, Authorize]
[Route("api/v1/urunler")]
[Yetki.ModulYetkisi("urun")]
public sealed class BelgeModulu(BelgeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);
    [HttpGet("{id}/belgeler")] public Task<List<object>> Liste(string id) => servis.Liste(id, Islem);

    [HttpPost("{id}/belgeler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    [EnableRateLimiting(OranSiniri.Belge)]
    [RequestSizeLimit(8_000_000)]
    public Task<object> Yukle(string id, [FromBody] BelgeYukleDto dto) => servis.Yukle(id, dto, Islem);

    [HttpGet("belgeler/{belgeId}/icerik")]
    public async Task<IActionResult> Icerik(string belgeId)
    {
        var b = await servis.Icerik(belgeId, Islem);
        var etag = $"\"{b.Sha256}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, must-revalidate";
        if (Request.Headers.IfNoneMatch.Any(v => v == etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return File(b.Icerik, b.Mime, BelgeController.BelgeAdi(b));
    }

    /// <summary>⚠ Mehmet K4-5: eski DELETE ucuna da aynı rol kapısı konur.</summary>
    [HttpDelete("belgeler/{belgeId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> PasifeAl(string belgeId) => servis.PasifeAl(belgeId, Islem);
}
