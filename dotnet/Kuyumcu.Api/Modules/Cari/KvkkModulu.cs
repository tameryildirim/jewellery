using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Kvkk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// D1-19 (18.10, rakip eşitleme: "KVKK aydınlatma ve rıza kaydı — metin hukukçudan").
//   • Metin işletmenindir: PUT yeni SÜRÜM yazar (aynı metin tekrar gönderilirse sürüm artmaz).
//   • Müşteri beyanı EKLENİR: aydınlatma yapıldı (geçerli metin sürümüyle) · pazarlama izni var/yok.
//     Metin hiç girilmediyse aydınlatma kaydı reddedilir (hangi metnin okunduğu bilinmeden "aydınlatıldı" denmez).
//   • Peşin Müşteri'ye beyan yazılmaz (kimliği olmayan sistem carisi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KvkkMetinDto
{
    [Required(ErrorMessage = "Aydınlatma metni zorunludur")]
    [StringLength(KvkkMetni.EnFazlaKarakter, ErrorMessage = "Aydınlatma metni en çok 20000 karakter olabilir")]
    public string Metin { get; set; } = null!;
}

public sealed class KvkkKayitDto
{
    [Required] public string Tur { get; set; } = null!;
    public bool Deger { get; set; } = true;
    public string? Kanal { get; set; }
}

public sealed class KvkkServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    private async Task<KvkkMetni?> Guncel(string tenantId) =>
        await db.KvkkMetinleri.AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Surum).FirstOrDefaultAsync();

    public async Task<KvkkMetinDurumu> MetinDurumu(AktifKullanici islem)
    {
        var m = await Guncel(islem.TenantId);
        var sonlar = await SonBeyanlar(islem.TenantId, null);
        return new KvkkMetinDurumu(m?.Surum, m?.Metin, m?.Olusturuldu,
            sonlar.Count(x => x.Tur == KvkkKayitTuru.Aydinlatma && x.Deger),
            sonlar.Count(x => x.Tur == KvkkKayitTuru.PazarlamaIzni && x.Deger));
    }

    public async Task<KvkkMetinDurumu> MetinKaydet(KvkkMetinDto dto, AktifKullanici islem)
    {
        var metin = (dto.Metin ?? "").Trim();
        if (metin.Length == 0) throw new BadRequestException("Aydınlatma metni zorunludur").AlanIle("metin");
        if (metin.Length > KvkkMetni.EnFazlaKarakter) throw new BadRequestException("Aydınlatma metni en çok 20000 karakter olabilir").AlanIle("metin");
        var m = await Guncel(islem.TenantId);
        if (m is null || m.Metin != metin)
        {
            var yeni = new KvkkMetni
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, Surum = (m?.Surum ?? 0) + 1, Metin = metin,
                KullaniciId = islem.Id, Olusturuldu = DateTime.UtcNow,
            };
            db.KvkkMetinleri.Add(yeni);
            denetim.YazmaKaydet(islem, nameof(KvkkMetni), yeni.Id, $"KVKK aydınlatma metni · sürüm {yeni.Surum}", hassas: false);
            await db.SaveChangesAsync();
        }
        return await MetinDurumu(islem);
    }

    /// <summary>Her (cari, tür) için en son beyan.</summary>
    private async Task<List<KvkkKaydi>> SonBeyanlar(string tenantId, string? cariId)
    {
        var q = db.KvkkKayitlari.AsNoTracking().Where(k => k.TenantId == tenantId);
        if (cariId is not null) q = q.Where(k => k.CariId == cariId);
        var hepsi = await q.OrderByDescending(k => k.Tarih).ThenByDescending(k => k.Id).Take(20_000).ToListAsync();
        return [.. hepsi.GroupBy(k => (k.CariId, k.Tur)).Select(g => g.First())];
    }

    private async Task CariDogrula(string cariId, AktifKullanici islem)
    {
        var c = await db.Cariler.AsNoTracking().Where(x => x.Id == cariId && x.TenantId == islem.TenantId)
            .Select(x => new { x.SistemCarisi }).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri için KVKK beyanı alınmaz");
    }

    public async Task<KvkkCariDurumu> CariDurumu(string cariId, AktifKullanici islem)
    {
        await CariDogrula(cariId, islem);
        var gecmis = await db.KvkkKayitlari.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.CariId == cariId)
            .OrderByDescending(k => k.Tarih).ThenByDescending(k => k.Id).Take(100).ToListAsync();
        var kullaniciIdler = gecmis.Select(k => k.KullaniciId).Distinct().ToList();
        var adlar = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && kullaniciIdler.Contains(k.Id))
            .Take(kullaniciIdler.Count + 1).ToDictionaryAsync(k => k.Id, k => k.Ad);
        var aydinlatma = gecmis.FirstOrDefault(k => k.Tur == KvkkKayitTuru.Aydinlatma && k.Deger);
        var pazarlama = gecmis.FirstOrDefault(k => k.Tur == KvkkKayitTuru.PazarlamaIzni);
        return new KvkkCariDurumu(aydinlatma?.Tarih, aydinlatma?.MetinSurumu, (await Guncel(islem.TenantId))?.Surum,
            pazarlama?.Deger ?? false, pazarlama?.Tarih,
            [.. gecmis.Select(k => new KvkkKayitSatiri(k.Tur, k.Deger, k.MetinSurumu, k.Kanal, adlar.GetValueOrDefault(k.KullaniciId), k.Tarih))]);
    }

    public async Task<KvkkCariDurumu> BeyanEkle(string cariId, KvkkKayitDto dto, AktifKullanici islem)
    {
        await CariDogrula(cariId, islem);
        if (!KvkkKayitTuru.Tumu.Contains(dto.Tur)) throw new BadRequestException("KVKK beyan türü AYDINLATMA ya da PAZARLAMA_IZNI olmalıdır").AlanIle("tur");
        var kanal = string.IsNullOrWhiteSpace(dto.Kanal) ? KvkkKanal.Tezgah : dto.Kanal;
        if (!KvkkKanal.Tumu.Contains(kanal)) throw new BadRequestException("Kanal TEZGAH ya da IMZALI_FORM olmalıdır").AlanIle("kanal");
        int? surum = null;
        if (dto.Tur == KvkkKayitTuru.Aydinlatma)
        {
            if (!dto.Deger) throw new BadRequestException("Aydınlatma geri alınamaz; yalnız yapıldı olarak kaydedilir").AlanIle("deger");
            surum = (await Guncel(islem.TenantId))?.Surum
                ?? throw new BadRequestException("Önce KVKK ekranından işletmenin aydınlatma metnini girin").AlanIle("tur");
        }
        var k = new KvkkKaydi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, CariId = cariId, Tur = dto.Tur, Deger = dto.Deger,
            MetinSurumu = surum, Kanal = kanal, KullaniciId = islem.Id, Tarih = DateTime.UtcNow,
        };
        db.KvkkKayitlari.Add(k);
        denetim.YazmaKaydet(islem, nameof(KvkkKaydi), k.Id, $"KVKK · {dto.Tur} · {(dto.Deger ? "evet" : "hayır")} · {kanal}", hassas: false);
        await db.SaveChangesAsync();
        return await CariDurumu(cariId, islem);
    }

    /// <summary>Basılacak metin (çıktı servisi kullanır); hiç girilmediyse null.</summary>
    public Task<KvkkMetni?> GuncelMetin(string tenantId) => Guncel(tenantId);
}

[ApiController]
[Authorize]
[Route("api/v1/kvkk")]
[Yetki.ModulYetkisi("cari")]
public sealed class KvkkController(KvkkServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kvkk/metin — geçerli aydınlatma metni + özet sayılar.</summary>
    [HttpGet("metin")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<KvkkMetinDurumu> Metin() => servis.MetinDurumu(Islem);

    /// <summary>PUT /api/v1/kvkk/metin — metni yeni sürüm olarak kaydet (aynı metin sürüm artırmaz).</summary>
    [HttpPut("metin")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<KvkkMetinDurumu> MetinKaydet([FromBody] KvkkMetinDto dto) => servis.MetinKaydet(dto, Islem);
}

[ApiController]
[Authorize]
[Route("api/v1/cari/{cariId}/kvkk")]
[Yetki.ModulYetkisi("cari")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
public sealed class CariKvkkController(KvkkServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/cari/{cariId}/kvkk — müşterinin KVKK durumu ve geçmişi.</summary>
    [HttpGet]
    public Task<KvkkCariDurumu> Durum(string cariId) => servis.CariDurumu(cariId, Islem);

    /// <summary>POST /api/v1/cari/{cariId}/kvkk — beyan ekle (aydınlatma yapıldı · pazarlama izni var/yok).</summary>
    [HttpPost]
    public Task<KvkkCariDurumu> Ekle(string cariId, [FromBody] KvkkKayitDto dto) => servis.BeyanEkle(cariId, dto, Islem);
}
