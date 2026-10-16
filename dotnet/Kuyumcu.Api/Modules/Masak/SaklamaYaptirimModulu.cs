using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — Uyum ekranının SAKLAMA BEKLETMESİ ve YASAL LİSTE uçları +
// Panel bantlarının özeti. MasakModulu.cs'e dokunmamak için AYRI controller
// (aynı api/v1/masak öneki). Hepsi [ModulYetkisi("uyum")] — TEZGÂHTAR'a kapalı.
//
// DoD sözleşmesi:
//   • saklama_bekletmeleri: POST (oluştur) · PATCH (bitiş/gerekçe; gövdede yoksa
//     koru, null → 400 zorunlu alan) · POST {id}/kaldir (erken bitir = pasife al;
//     satır silinmez) · her yazma DenetimServisi.YazmaKaydet.
//   • yaptirim_eslesmeleri: PATCH (durum/MASAK bildirim tarihi/evrak no/not).
//     SİLİNMEZ ve kilit bu uçla AÇILMAZ (6415 md.10 — kilidi yalnız kararın
//     kaldırılması açar; ekranda DegismezIpucu gerekçesi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SaklamaBekletmeDto
{
    /// <summary>TUMU ya da TESPIT.</summary>
    [MaxLength(10)] public string? Kapsam { get; set; }
    [MaxLength(36)] public string? KimlikTespitiId { get; set; }
    /// <summary>Yerel gün (dahil) — en çok bugünden 1 yıl sonrası.</summary>
    public DateOnly? Bitis { get; set; }
    [MaxLength(500)] public string? Gerekce { get; set; }
}

public sealed class SaklamaBekletmePatchDto
{
    private DateOnly? _bitis; private string? _gerekce;
    public DateOnly? Bitis { get => _bitis; set { _bitis = value; BitisVerildi = true; } }
    [JsonIgnore] public bool BitisVerildi { get; private set; }
    [MaxLength(500)] public string? Gerekce { get => _gerekce; set { _gerekce = value; GerekceVerildi = true; } }
    [JsonIgnore] public bool GerekceVerildi { get; private set; }
}

public sealed class SaklamaBekletmeServisi(KuyumcuDbContext db, DenetimServisi denetim, IConfiguration yapilandirma)
{
    public const int AsgariGerekce = 20;
    private const string Varlik = nameof(SaklamaBekletme);

    private static DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);

    /// <summary>Bitiş günü kuralı: bugünden sonra, en çok 1 yıl.</summary>
    private static DateTime BitisDogrula(DateOnly? gun)
    {
        if (gun is not { } g)
            throw new BadRequestException("Bekletmenin bitiş tarihini seçin.").AlanIle("bitis");
        if (g <= Bugun)
            throw new BadRequestException("Bitiş tarihi bugünden sonra olmalı.").AlanIle("bitis");
        if (g > Bugun.AddYears(1))
            throw new BadRequestException("Bekletme en çok 1 yıl sürebilir; süre dolunca yenileyebilirsiniz.").AlanIle("bitis");
        return Zaman.YerelGunAraligiUtc(g).Item2;   // o günün SONUNDA biter
    }

    private static string GerekceDogrula(string? g)
    {
        var t = g?.Trim() ?? "";
        if (t.Length < AsgariGerekce)
            throw new BadRequestException($"Gerekçe en az {AsgariGerekce} karakter olmalı ({t.Length}/{AsgariGerekce}).").AlanIle("gerekce");
        if (t.Length > 500)
            throw new BadRequestException("Gerekçe en çok 500 karakter olabilir.").AlanIle("gerekce");
        return t;
    }

    public async Task<object> Liste(AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;
        var satirlar = await db.SaklamaBekletmeleri.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId)
            .OrderByDescending(b => b.Olusturuldu).Take(100).ToListAsync();
        // Geçmiş maskelemeler (elle + otomatik) — denetim izinden, kişisel veri yok.
        var maskelemeler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == islem.TenantId && d.VarlikTuru == nameof(MasakKimlikTespiti)
                     && d.Detay != null && d.Detay.StartsWith("saklama süresi maskelemesi"))
            .OrderByDescending(d => d.Tarih).Take(20)
            .Select(d => new { tarih = d.Tarih, otomatik = d.KullaniciId == null, ozet = d.Detay }).ToListAsync();
        return new
        {
            kuru = SaklamaUygulaKuru(),
            bekletmeler = satirlar.Select(b => Sekillendir(b, simdi)).ToList(),
            maskelemeler,
        };
    }

    private bool SaklamaUygulaKuru() => yapilandirma.GetValue("Isler:SaklamaUygula:Kuru", true)
        || !yapilandirma.GetValue("Isler:SaklamaUygula:Etkin", true);

    public async Task<object> Olustur(SaklamaBekletmeDto dto, AktifKullanici islem)
    {
        // DTO DataAnnotations yalnız controller yolunda — servis elle denetler.
        var kapsam = dto.Kapsam?.Trim().ToUpperInvariant() ?? SaklamaBekletmeKapsami.Tumu;
        if (!SaklamaBekletmeKapsami.Hepsi.Contains(kapsam))
            throw new BadRequestException("Kapsam geçersiz — tüm kayıtlar ya da tek kayıt seçin.").AlanIle("kapsam");
        string? tespitId = null;
        if (kapsam == SaklamaBekletmeKapsami.Tespit)
        {
            tespitId = dto.KimlikTespitiId?.Trim();
            if (string.IsNullOrEmpty(tespitId)
                || !await db.MasakKimlikTespitleri.AsNoTracking().AnyAsync(m => m.TenantId == islem.TenantId && m.Id == tespitId))
                throw new BadRequestException("Bekletilecek kimlik kaydını seçin.").AlanIle("kimlikTespitiId");
        }
        var bitis = BitisDogrula(dto.Bitis);
        var gerekce = GerekceDogrula(dto.Gerekce);
        var simdi = DateTime.UtcNow;
        var b = new SaklamaBekletme
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Kapsam = kapsam, KimlikTespitiId = tespitId,
            Baslangic = simdi, Bitis = bitis, Gerekce = gerekce, OlusturanKullaniciId = islem.Id,
            Olusturuldu = simdi, Guncellendi = simdi,
        };
        db.SaklamaBekletmeleri.Add(b);
        denetim.YazmaKaydet(islem, Varlik, b.Id,
            $"saklama bekletmesi eklendi · kapsam {kapsam} · bitiş {dto.Bitis:yyyy-MM-dd} · gerekçe: {gerekce}");
        await db.SaveChangesAsync();
        return Sekillendir(b, simdi);
    }

    public async Task<object> Guncelle(string id, SaklamaBekletmePatchDto dto, AktifKullanici islem)
    {
        var b = await Bul(id, islem);
        if (b.Kaldirildi is not null)
            throw new BadRequestException("Kaldırılmış bekletme düzenlenemez; yeni bekletme ekleyin.");
        var degisen = new List<string>();
        if (dto.BitisVerildi) { b.Bitis = BitisDogrula(dto.Bitis); degisen.Add($"bitiş {dto.Bitis:yyyy-MM-dd}"); }
        if (dto.GerekceVerildi) { b.Gerekce = GerekceDogrula(dto.Gerekce); degisen.Add("gerekçe"); }
        if (degisen.Count == 0) throw new BadRequestException("Değiştirilecek alan yok.");
        b.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, b.Id, $"saklama bekletmesi güncellendi · {string.Join(", ", degisen)} · gerekçe: {b.Gerekce}");
        await db.SaveChangesAsync();
        return Sekillendir(b, DateTime.UtcNow);
    }

    public async Task<object> Kaldir(string id, AktifKullanici islem)
    {
        var b = await Bul(id, islem);
        if (b.Kaldirildi is not null) throw new BadRequestException("Bu bekletme zaten kaldırılmış.");
        var simdi = DateTime.UtcNow;
        b.Kaldirildi = simdi; b.KaldiranKullaniciId = islem.Id; b.Guncellendi = simdi;
        denetim.YazmaKaydet(islem, Varlik, b.Id, "saklama bekletmesi erken kaldırıldı (kendiliğinden gizleme yeniden işler)");
        await db.SaveChangesAsync();
        return Sekillendir(b, simdi);
    }

    private async Task<SaklamaBekletme> Bul(string id, AktifKullanici islem) =>
        await db.SaklamaBekletmeleri.FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.Id == id)
        ?? throw new NotFoundException("Bekletme bulunamadı.");

    private static object Sekillendir(SaklamaBekletme b, DateTime simdi) => new
    {
        id = b.Id, kapsam = b.Kapsam, kimlikTespitiId = b.KimlikTespitiId,
        baslangic = b.Baslangic,
        // Ekran yerel günü gösterir: bitiş anı gün SONU olduğu için bir önceki yerel gün.
        bitisGunu = DateOnly.FromDateTime(Zaman.UtcdenYerele(b.Bitis).AddTicks(-1)),
        gerekce = b.Gerekce, olusturuldu = b.Olusturuldu, kaldirildi = b.Kaldirildi,
        aktif = b.Kaldirildi is null && b.Baslangic <= simdi && b.Bitis > simdi,
    };
}

[ApiController]
[Authorize]
[Route("api/v1/masak")]
[Yetki.ModulYetkisi("uyum")]
public sealed class SaklamaYaptirimController(
    SaklamaBekletmeServisi bekletme, SaklamaSuresiIsi saklama, KuyumcuDbContext db, IConfiguration yapilandirma) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/masak/saklama-bekletmeleri — bekletmeler + geçmiş maskelemeler.</summary>
    [HttpGet("saklama-bekletmeleri")]
    [RolGerekli("SAHIP")]
    public Task<object> Bekletmeler() => bekletme.Liste(Islem);

    /// <summary>POST /api/v1/masak/saklama-bekletme — gerekçeli bekletme (≤ 1 yıl).</summary>
    [HttpPost("saklama-bekletme")]
    [RolGerekli("SAHIP")]
    public async Task<IActionResult> BekletmeEkle([FromBody] SaklamaBekletmeDto dto) =>
        StatusCode(StatusCodes.Status201Created, await bekletme.Olustur(dto, Islem));

    /// <summary>PATCH /api/v1/masak/saklama-bekletme/{id} — bitiş/gerekçe.</summary>
    [HttpPatch("saklama-bekletme/{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> BekletmeGuncelle(string id, [FromBody] SaklamaBekletmePatchDto dto) =>
        bekletme.Guncelle(id, dto, Islem);

    /// <summary>POST /api/v1/masak/saklama-bekletme/{id}/kaldir — erken bitir (satır kalır).</summary>
    [HttpPost("saklama-bekletme/{id}/kaldir")]
    [RolGerekli("SAHIP")]
    public Task<object> BekletmeKaldir(string id) => bekletme.Kaldir(id, Islem);

    /// <summary>GET /api/v1/masak/yaptirim — liste tarihi/kaynağı + bu kiracının eşleşmeleri.</summary>
    [HttpGet("yaptirim")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Yaptirim() => YaptirimServisi.Ozet(db, Islem.TenantId, OzetGunu);

    /// <summary>Yalnız testlerin kaydettiği gün dikişi (üretimde null → sistem günü).</summary>
    private DateOnly? OzetGunu =>
        (HttpContext?.RequestServices.GetService(typeof(YaptirimServisi.YaptirimOzetGunu)) as YaptirimServisi.YaptirimOzetGunu)?.Bugun;

    /// <summary>PATCH /api/v1/masak/yaptirim-eslesmeleri/{id} — MASAK'a bildirim kapatması.</summary>
    [HttpPatch("yaptirim-eslesmeleri/{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> EslesmeGuncelle(string id, [FromBody] YaptirimEslesmePatchDto dto) =>
        YaptirimServisi.EslesmeGuncelle(db, id, dto, Islem);

    /// <summary>
    /// GET /api/v1/masak/uyum-ozet — Panel bantları (SAHİP): 30 gün içinde gizlenecek
    /// kayıt sayısı, liste eskiliği, açık eşleşmeler. Kişisel veri YOK.
    /// </summary>
    [HttpGet("uyum-ozet")]
    [RolGerekli("SAHIP")]
    public async Task<object> UyumOzet()
    {
        var simdi = DateTime.UtcNow;
        var yaklasan = await saklama.AdaylariBul(Islem.TenantId, simdi, simdi.AddDays(SaklamaSuresiIsi.OncedenHaberGun));
        var y = System.Text.Json.JsonSerializer.SerializeToElement(await YaptirimServisi.Ozet(db, Islem.TenantId, OzetGunu));
        return new
        {
            saklamaYaklasan = yaklasan.TespitIdleri.Count,
            saklamaGun = SaklamaSuresiIsi.OncedenHaberGun,
            saklamaKuru = yapilandirma.GetValue("Isler:SaklamaUygula:Kuru", true),
            listeTarihi = y.GetProperty("listeTarihi").ValueKind == System.Text.Json.JsonValueKind.Null
                ? (DateOnly?)null : DateOnly.Parse(y.GetProperty("listeTarihi").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            listeEski = y.GetProperty("listeEski").GetBoolean(),
            listeYasiGun = y.GetProperty("listeYasiGun").ValueKind == System.Text.Json.JsonValueKind.Null
                ? (int?)null : y.GetProperty("listeYasiGun").GetInt32(),
            acikTam = y.GetProperty("acikTam").GetInt32(),
            acikBenzerlik = y.GetProperty("acikBenzerlik").GetInt32(),
        };
    }
}
