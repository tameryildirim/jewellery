using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// D1-03 (18.10, rakip eşitleme: "işlem bekletme") — tezgâhta yarım kalan satış fişi bekletilir,
// sıradaki müşteriye geçilir, sonra aynı ya da başka kasadan geri alınır.
//   • Bekletme DEFTER YAZMAZ (stok/kasa/cari aynen). Geri alma = kaydı silip içeriği döndürmek;
//     iki kasa aynı fişi alamaz (silme tek satırlık koşullu işlem — ikinci alan 404).
//   • Ürünler kiracıda doğrulanır; adet > 0, fiyat ≥ 0, iskonto 0–100.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BekleyenFisDto
{
    [Required] public BekleyenFisIcerik Icerik { get; set; } = null!;
}

public sealed class BekleyenFisServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<BekleyenFisOzet>> Liste(AktifKullanici islem)
    {
        var l = await db.BekleyenFisler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId)
            .OrderByDescending(b => b.Olusturuldu).ThenBy(b => b.Id)
            .Take(BekleyenFis.AzamiAdet)
            .ToListAsync();
        var cariIdler = l.Where(b => b.CariId != null).Select(b => b.CariId!).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id))
            .Take(cariIdler.Count + 1)
            .ToDictionaryAsync(c => c.Id, c => new { c.Ad, c.SistemCarisi });
        var kullaniciIdler = l.Select(b => b.KullaniciId).Distinct().ToList();
        var kullanicilar = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && kullaniciIdler.Contains(k.Id))
            .Take(kullaniciIdler.Count + 1)
            .ToDictionaryAsync(k => k.Id, k => k.Ad);
        return [.. l.Select(b =>
        {
            var c = b.CariId is null ? null : cariler.GetValueOrDefault(b.CariId);
            return new BekleyenFisOzet(b.Id, b.Etiket, c is null || c.SistemCarisi ? null : c.Ad, b.KalemSayisi, b.ToplamTl,
                kullanicilar.GetValueOrDefault(b.KullaniciId), b.Olusturuldu);
        })];
    }

    public async Task<BekleyenFisOzet> Beklet(BekleyenFisDto dto, AktifKullanici islem)
    {
        var ic = dto.Icerik ?? throw new BadRequestException("Bekletilecek fiş boş").AlanIle("icerik");
        var kalemler = ic.Kalemler ?? [];
        if (kalemler.Count == 0) throw new BadRequestException("Bekletilecek fişte kalem yok").AlanIle("kalemler");
        if (kalemler.Count > BekleyenFis.AzamiKalem) throw new BadRequestException("Bekletilen fişte en çok 200 kalem olabilir").AlanIle("kalemler");
        if (kalemler.Any(k => k.Adet <= 0m)) throw new BadRequestException("Adet sıfırdan büyük olmalıdır").AlanIle("kalemler");
        if (kalemler.Any(k => k.BirimFiyat < 0m)) throw new BadRequestException("Birim fiyat eksi olamaz").AlanIle("kalemler");
        if (kalemler.Any(k => k.SatirIskontoYuzde is < 0m or > 100m) || ic.FisIskontoYuzde is < 0m or > 100m)
            throw new BadRequestException("İskonto yüzde 0 ile 100 arasında olmalıdır").AlanIle("kalemler");
        if (ic.Aciklama is { Length: > 500 }) throw new BadRequestException("Açıklama en çok 500 karakter olabilir").AlanIle("aciklama");

        var urunIdler = kalemler.Select(k => k.UrunId).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Ad })
            .Take(urunIdler.Count + 1)
            .ToDictionaryAsync(u => u.Id, u => u.Ad);
        if (urunler.Count != urunIdler.Count) throw new BadRequestException("Fişteki bir ürün bulunamadı").AlanIle("kalemler");

        string? cariAd = null;
        if (!string.IsNullOrEmpty(ic.CariId))
        {
            var c = await db.Cariler.AsNoTracking()
                .Where(x => x.Id == ic.CariId && x.TenantId == islem.TenantId)
                .Select(x => new { x.Ad, x.SistemCarisi }).FirstOrDefaultAsync()
                ?? throw new BadRequestException("Müşteri bulunamadı").AlanIle("cariId");
            if (!c.SistemCarisi) cariAd = c.Ad;
        }

        if (await db.BekleyenFisler.CountAsync(b => b.TenantId == islem.TenantId) >= BekleyenFis.AzamiAdet)
            throw new ConflictException("En çok 50 fiş bekletilebilir — önce bekleyenlerden birini alın ya da silin");

        // Ürün adı SUNUCUDAN (istemcinin yazdığı ad saklanmaz); tutar liste içindir.
        var temiz = new BekleyenFisIcerik(string.IsNullOrEmpty(ic.CariId) ? null : ic.CariId,
            [.. kalemler.Select(k => k with { UrunAdi = urunler[k.UrunId] })], ic.FisIskontoYuzde,
            string.IsNullOrWhiteSpace(ic.Aciklama) ? null : ic.Aciklama.Trim());
        var ara = temiz.Kalemler.Sum(k => k.Hediye ? 0m : k.Adet * k.BirimFiyat * (1m - k.SatirIskontoYuzde / 100m));
        var toplam = Math.Round(ara * (1m - (temiz.FisIskontoYuzde ?? 0m) / 100m), 2, MidpointRounding.AwayFromZero);
        var ilk = temiz.Kalemler[0].UrunAdi;
        var etiket = cariAd ?? (temiz.Kalemler.Count == 1 ? ilk : $"{ilk} +{temiz.Kalemler.Count - 1}");

        var b = new BekleyenFis
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, KullaniciId = islem.Id, CariId = temiz.CariId,
            Etiket = etiket.Length > 120 ? etiket[..120] : etiket,
            Icerik = JsonSerializer.Serialize(temiz, Json), KalemSayisi = temiz.Kalemler.Count, ToplamTl = toplam,
            Olusturuldu = DateTime.UtcNow,
        };
        db.BekleyenFisler.Add(b);
        denetim.YazmaKaydet(islem, nameof(BekleyenFis), b.Id, $"fiş bekletildi · {b.KalemSayisi} kalem · {toplam:0.00} TL", hassas: false);
        await db.SaveChangesAsync();
        return new BekleyenFisOzet(b.Id, b.Etiket, cariAd, b.KalemSayisi, b.ToplamTl, null, b.Olusturuldu);
    }

    /// <summary>
    /// Bekleyen fişi geri alır: kayıt silinir, içerik döner. İki kasa aynı anda alırsa ikincinin silmesi
    /// 0 satır etkiler → EF eşzamanlılık hatası → 404 (aynı fiş iki tezgâhta açılmaz).
    /// </summary>
    public async Task<BekleyenFisIcerik> Al(string id, AktifKullanici islem)
    {
        var b = await Kaldir(id, islem, "bekleyen fiş geri alındı");
        return JsonSerializer.Deserialize<BekleyenFisIcerik>(b.Icerik, Json)
            ?? throw new BadRequestException("Bekleyen fiş okunamadı");
    }

    public Task Sil(string id, AktifKullanici islem) => Kaldir(id, islem, "bekleyen fiş silindi");

    private async Task<BekleyenFis> Kaldir(string id, AktifKullanici islem, string iz)
    {
        var b = await db.BekleyenFisler.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(BulunamadiMetni);
        db.BekleyenFisler.Remove(b);
        denetim.YazmaKaydet(islem, nameof(BekleyenFis), id, iz, hassas: false);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new NotFoundException(BulunamadiMetni); }
        return b;
    }

    private const string BulunamadiMetni = "Bekleyen fiş bulunamadı (başka kasa almış olabilir)";
}

[ApiController]
[Authorize]
[Route("api/v1/bekleyen-fisler")]
[Yetki.ModulYetkisi("islem")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
public sealed class BekleyenFisController(BekleyenFisServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/bekleyen-fisler — işletmenin bekleyen fişleri (yeniden eskiye).</summary>
    [HttpGet]
    public Task<List<BekleyenFisOzet>> Liste() => servis.Liste(Islem);

    /// <summary>POST /api/v1/bekleyen-fisler — tezgâhtaki fişi beklet.</summary>
    [HttpPost]
    public Task<BekleyenFisOzet> Beklet([FromBody] BekleyenFisDto dto) => servis.Beklet(dto, Islem);

    /// <summary>POST /api/v1/bekleyen-fisler/{id}/al — fişi geri al (kayıt silinir, içerik döner).</summary>
    [HttpPost("{id}/al")]
    public Task<BekleyenFisIcerik> Al(string id) => servis.Al(id, Islem);

    /// <summary>POST /api/v1/bekleyen-fisler/{id}/sil — bekleyen fişi at (defter yazılmadığı için iz yalnız denetimde).</summary>
    [HttpPost("{id}/sil")]
    public async Task<IActionResult> Sil(string id) { await servis.Sil(id, Islem); return NoContent(); }
}
