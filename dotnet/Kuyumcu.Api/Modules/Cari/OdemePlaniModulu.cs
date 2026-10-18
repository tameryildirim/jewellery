using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Vade;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// D1-06 (18.10, rakip eşitleme: "kuyumcunun kendi taksitli/senetli satışı").
//   • Aday: müşterinin AÇIK veresiye satış borcu (VadeKapisi.AcikBorclar — tek tanım). Plan tutarı = açık tutar.
//   • Taksitler eşit bölünür (kuruş artığı son taksite); ilk vade bugün ya da sonrası; aralık 1–12 ay.
//   • Senetsiz plan: taksit durumu o borcun ödenen kısmından sırayla hesaplanır (tahsilat FIFO — defter aynen).
//   • Senetli plan: her taksit için ALINAN SENET (evrak modülü, aynı işlemde) — borç senede geçer; durum senetten.
//     Senetli plan iptal edilmez (senetler evrak ekranında yönetilir).
//   • Vade farkı EKLENMEZ. Nakit tahsilat sınırı (VUK 459) tahsilat anında zaten uygulanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class OdemePlaniDto
{
    [Required, MaxLength(36)] public string CariId { get; set; } = null!;
    [Required, MaxLength(36)] public string KaynakHareketId { get; set; } = null!;
    [Range(2, 36, ErrorMessage = "Taksit sayısı 2 ile 36 arasında olmalıdır")] public int TaksitSayisi { get; set; }
    [Required(ErrorMessage = "İlk taksit tarihi zorunludur")] public DateOnly? IlkVade { get; set; }
    [Range(1, 12, ErrorMessage = "Taksit aralığı 1 ile 12 ay arasında olmalıdır")] public int AralikAy { get; set; } = 1;
    public bool Senetli { get; set; }
}

public static class OdemePlaniMetni
{
    public static string Bulunamadi => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Ödeme planı bulunamadı");
    public static string AdayDegil => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu satışın taksitlendirilecek açık borcu yok");
    public static string ZatenVar => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu satışın etkin bir ödeme planı zaten var");
    public static string GecmisVade => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("İlk taksit tarihi bugünden önce olamaz");
    public static string SenetliIptal => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Senetli plan iptal edilmez — senetleri Çek ve senet ekranından yönetin");
    public static string KucukTaksit => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Taksit tutarı en az 1 TL olmalıdır");
}

public sealed class OdemePlaniServisi(KuyumcuDbContext db, DenetimServisi denetim, Evrak.EvrakServisi? evrak = null)
{
    /// <summary>Müşterinin taksitlendirilebilir (açık, planı olmayan) veresiye satışları.</summary>
    public async Task<List<OdemePlaniAdayi>> Adaylar(string cariId, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        var (borclar, _) = await VadeKapisi.AcikBorclar(db, islem.TenantId, cariId, DateTime.UtcNow);
        var planli = await PlanliHareketler(islem.TenantId, cariId);
        var idler = borclar.Select(b => b.HareketId).ToList();
        var satislar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && idler.Contains(h.Id) && h.Tur == "SATIS")
            .Take(idler.Count + 1)
            .ToDictionaryAsync(h => h.Id, h => h.Aciklama);
        return [.. borclar.Where(b => b.AcikTutar > 0m && satislar.ContainsKey(b.HareketId) && !planli.Contains(b.HareketId))
            .Select(b => new OdemePlaniAdayi(b.HareketId, b.Tarih, b.AcikTutar, satislar[b.HareketId]))];
    }

    public async Task<List<OdemePlaniOzet>> Liste(string cariId, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        var planlar = await db.OdemePlanlari.AsNoTracking()
            .Where(p => p.TenantId == islem.TenantId && p.CariId == cariId)
            .OrderByDescending(p => p.Olusturuldu).ThenBy(p => p.Id).Take(50).ToListAsync();
        return await Ozetle(planlar, islem.TenantId);
    }

    public async Task<OdemePlaniOzet> Getir(string id, AktifKullanici islem)
    {
        var p = await db.OdemePlanlari.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(OdemePlaniMetni.Bulunamadi);
        return (await Ozetle([p], islem.TenantId))[0];
    }

    public async Task<OdemePlaniOzet> Olustur(OdemePlaniDto dto, AktifKullanici islem)
    {
        if (dto.TaksitSayisi is < 2 or > 36) throw new BadRequestException("Taksit sayısı 2 ile 36 arasında olmalıdır").AlanIle("taksitSayisi");
        if (dto.AralikAy is < 1 or > 12) throw new BadRequestException("Taksit aralığı 1 ile 12 ay arasında olmalıdır").AlanIle("aralikAy");
        if (dto.IlkVade is not { } ilk) throw new BadRequestException("İlk taksit tarihi zorunludur").AlanIle("ilkVade");
        if (ilk < Zaman.YerelBugun()) throw new BadRequestException(OdemePlaniMetni.GecmisVade).AlanIle("ilkVade");
        var aday = (await Adaylar(dto.CariId, islem)).FirstOrDefault(a => a.HareketId == dto.KaynakHareketId)
            ?? throw new BadRequestException(OdemePlaniMetni.AdayDegil).AlanIle("kaynakHareketId");
        var toplam = aday.AcikTutar;
        var taksit = Math.Floor(toplam / dto.TaksitSayisi * 100m) / 100m;
        if (taksit < 1m) throw new BadRequestException(OdemePlaniMetni.KucukTaksit).AlanIle("taksitSayisi");

        await using var tx = await db.Database.IsBaslatVeyaDevral();
        if (await db.OdemePlanlari.AnyAsync(p => p.TenantId == islem.TenantId && p.KaynakHareketId == dto.KaynakHareketId && p.Durum == OdemePlani.Aktif))
            throw new ConflictException(OdemePlaniMetni.ZatenVar);
        var plan = new OdemePlani
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, CariId = dto.CariId, KaynakHareketId = dto.KaynakHareketId,
            ToplamTl = toplam, Senetli = dto.Senetli, OlusturanId = islem.Id, Olusturuldu = DateTime.UtcNow,
        };
        db.OdemePlanlari.Add(plan);
        var taksitler = Enumerable.Range(1, dto.TaksitSayisi).Select(i => new OdemePlaniTaksiti
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, PlanId = plan.Id, Sira = i,
            Vade = ilk.AddMonths((i - 1) * dto.AralikAy),
            TutarTl = i == dto.TaksitSayisi ? toplam - taksit * (dto.TaksitSayisi - 1) : taksit,
        }).ToList();
        db.OdemePlaniTaksitleri.AddRange(taksitler);
        denetim.YazmaKaydet(islem, nameof(OdemePlani), plan.Id, $"ödeme planı · {dto.TaksitSayisi} taksit · {toplam:0.00} TL{(dto.Senetli ? " · senetli" : "")}", hassas: false);
        await db.SaveChangesAsync();

        if (dto.Senetli)
        {
            var cari = await db.Cariler.AsNoTracking().Where(c => c.Id == dto.CariId && c.TenantId == islem.TenantId)
                .Select(c => new { c.Ad, c.VergiNo }).FirstAsync();
            var kur = await db.KurSnapshots.AsNoTracking().Where(k => k.TenantId == islem.TenantId && !k.IptalEdildi)
                .OrderByDescending(k => k.Olusturuldu).Select(k => k.Id).FirstOrDefaultAsync()
                ?? throw new BadRequestException("Kur kaydı bulunamadı");
            var kok = plan.Id[^6..];
            foreach (var t in taksitler)
            {
                var sonuc = await (evrak ?? throw new InvalidOperationException("Evrak servisi yok")).Gir(new Evrak.EvrakGirisDto
                {
                    EvrakTuru = "SENET", Tutar = t.TutarTl, VadeGunu = t.Vade, EvrakNo = $"OP-{kok}-{t.Sira}",
                    BorcluCariId = dto.CariId, BorcluAd = cari.Ad, KaynakCariId = dto.CariId, KurSnapshotId = kur,
                    Notlar = $"Ödeme planı {t.Sira}/{dto.TaksitSayisi}",
                }, islem);
                t.EvrakId = sonuc.Evrak.Id;
            }
            await db.SaveChangesAsync();
        }
        if (tx is not null) await tx.CommitAsync();
        return (await Ozetle([plan], islem.TenantId))[0];
    }

    public async Task<OdemePlaniOzet> Iptal(string id, AktifKullanici islem)
    {
        var p = await db.OdemePlanlari.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(OdemePlaniMetni.Bulunamadi);
        if (p.Senetli) throw new ConflictException(OdemePlaniMetni.SenetliIptal);
        if (p.Durum == OdemePlani.Aktif)
        {
            p.Durum = OdemePlani.Iptal; p.IptalZamani = DateTime.UtcNow;
            denetim.YazmaKaydet(islem, nameof(OdemePlani), p.Id, "ödeme planı iptal (borç cari defterinde aynen)", hassas: false);
            await db.SaveChangesAsync();
        }
        return (await Ozetle([p], islem.TenantId))[0];
    }

    /// <summary>Etkin planı olan veresiye satışlar (vade hatırlatması bunları taksit takvimine bırakır).</summary>
    public async Task<HashSet<string>> PlanliHareketler(string tenantId, string? cariId = null)
    {
        var q = db.OdemePlanlari.AsNoTracking().Where(p => p.TenantId == tenantId && p.Durum == OdemePlani.Aktif);
        if (cariId is not null) q = q.Where(p => p.CariId == cariId);
        return [.. await q.Select(p => p.KaynakHareketId).Take(5000).ToListAsync()];
    }

    public async Task<List<OdemePlaniOzet>> Ozetle(List<OdemePlani> planlar, string tenantId)
    {
        if (planlar.Count == 0) return [];
        var idler = planlar.Select(p => p.Id).ToList();
        var taksitler = await db.OdemePlaniTaksitleri.AsNoTracking()
            .Where(t => t.TenantId == tenantId && idler.Contains(t.PlanId)).OrderBy(t => t.Sira)
            .Take(idler.Count * 36 + 1).ToListAsync();
        var evrakIdler = taksitler.Where(t => t.EvrakId != null).Select(t => t.EvrakId!).Distinct().ToList();
        var evraklar = await db.KiymetliEvraklar.AsNoTracking()
            .Where(e => e.TenantId == tenantId && evrakIdler.Contains(e.Id))
            .Take(evrakIdler.Count + 1).ToDictionaryAsync(e => e.Id, e => e.Durum);
        var hareketIdler = planlar.Select(p => p.KaynakHareketId).Distinct().ToList();
        var tarihler = await db.Hareketler.AsNoTracking().Where(h => h.TenantId == tenantId && hareketIdler.Contains(h.Id))
            .Take(hareketIdler.Count + 1).ToDictionaryAsync(h => h.Id, h => h.Tarih);
        var cariIdler = planlar.Select(p => p.CariId).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking().Where(c => c.TenantId == tenantId && cariIdler.Contains(c.Id))
            .Take(cariIdler.Count + 1).ToDictionaryAsync(c => c.Id, c => c.Ad);
        var acik = new Dictionary<string, decimal>();
        foreach (var cariId in planlar.Where(p => !p.Senetli).Select(p => p.CariId).Distinct())
        {
            var (borclar, _) = await VadeKapisi.AcikBorclar(db, tenantId, cariId, DateTime.UtcNow);
            foreach (var b in borclar) acik[b.HareketId] = b.AcikTutar;
        }
        var bugun = Zaman.YerelBugun();
        var sonuc = new List<OdemePlaniOzet>();
        foreach (var p in planlar)
        {
            var pt = taksitler.Where(t => t.PlanId == p.Id).ToList();
            var satirlar = new List<OdemePlaniTaksitOzet>();
            decimal odenen;
            if (p.Senetli)
            {
                foreach (var t in pt)
                {
                    var d = t.EvrakId is null ? null : evraklar.GetValueOrDefault(t.EvrakId);
                    var durum = d == EvrakDurumu.TahsilEdildi ? "ODENDI" : d == EvrakDurumu.Karsiliksiz ? "KARSILIKSIZ" : t.Vade < bugun ? "GECIKTI" : "BEKLIYOR";
                    satirlar.Add(new(t.Sira, t.Vade, t.TutarTl, durum == "ODENDI" ? t.TutarTl : 0m, durum, t.EvrakId));
                }
                odenen = satirlar.Sum(s => s.OdenenTl);
            }
            else
            {
                odenen = Math.Clamp(p.ToplamTl - acik.GetValueOrDefault(p.KaynakHareketId, 0m), 0m, p.ToplamTl);
                var kalan = odenen;
                foreach (var t in pt)
                {
                    var o = Math.Min(kalan, t.TutarTl); kalan -= o;
                    var durum = o >= t.TutarTl ? "ODENDI" : t.Vade < bugun ? "GECIKTI" : o > 0m ? "KISMI" : "BEKLIYOR";
                    satirlar.Add(new(t.Sira, t.Vade, t.TutarTl, o, durum, null));
                }
            }
            var planDurum = p.Durum == OdemePlani.Iptal ? "IPTAL" : satirlar.All(s => s.Durum == "ODENDI") ? "TAMAMLANDI" : "AKTIF";
            sonuc.Add(new OdemePlaniOzet(p.Id, p.CariId, cariler.GetValueOrDefault(p.CariId), p.KaynakHareketId,
                tarihler.GetValueOrDefault(p.KaynakHareketId), p.ToplamTl, odenen, p.Senetli, planDurum, p.Olusturuldu, satirlar));
        }
        return sonuc;
    }
}

[ApiController]
[Authorize]
[Route("api/v1/odeme-planlari")]
[Yetki.ModulYetkisi("cari")]
public sealed class OdemePlaniController(OdemePlaniServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/odeme-planlari/adaylar?cariId= — taksitlendirilebilir veresiye satışlar.</summary>
    [HttpGet("adaylar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<List<OdemePlaniAdayi>> Adaylar([FromQuery] string cariId) => servis.Adaylar(cariId, Islem);

    /// <summary>GET /api/v1/odeme-planlari?cariId= — müşterinin planları (taksit durumlarıyla).</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<List<OdemePlaniOzet>> Liste([FromQuery] string cariId) => servis.Liste(cariId, Islem);

    /// <summary>GET /api/v1/odeme-planlari/{id}</summary>
    [HttpGet("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<OdemePlaniOzet> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>POST /api/v1/odeme-planlari — veresiye satışı taksitlere böl (isteğe bağlı senetli).</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<OdemePlaniOzet> Olustur([FromBody] OdemePlaniDto dto) => servis.Olustur(dto, Islem);

    /// <summary>POST /api/v1/odeme-planlari/{id}/iptal — senetsiz planı kaldır (borç defterde aynen kalır).</summary>
    [HttpPost("{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<OdemePlaniOzet> Iptal(string id) => servis.Iptal(id, Islem);
}
