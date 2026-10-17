using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kuyumcu.Contracts.Sayfalar.Faturalar;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// 18.10 — TOPLU FATURA (Faturalog eşitliği F6: "yüzlerce siparişi tek tuşla"; kuyumcu için anlamlı biçimi
// gün sonu: bugünün faturasız satış fişleri). Kurucu: "Faturalog kadar sade… faturaya direkt çevrilecek".
//   • Adaylar: faturasız SATIŞ hareketleri fişe göre gruplanır (FaturaHareketBagi.Bagsiz — tek tanım).
//   • Kesim: her fiş KENDİ işleminde FaturaServisi.FistenOlustur (KUYUM) → SAHIP/MUDUR/MUHASEBE ise KESILDI.
//     Biri kesilemezse diğerleri sürer; sebep satırına yazılır (sessiz atlama yok).
//   • Kurallar satıştakiyle AYNI: Peşin Müşteri'ye fatura kesilmez; kuyumcu fatura haddi üstünde geçerli TC/VKN şart.
// ═════════════════════════════════════════════════════════════════════════════


public sealed class TopluFaturaDto
{
    [Required, MinLength(1, ErrorMessage = "En az bir fiş seçin"), MaxLength(100, ErrorMessage = "Tek seferde en çok 100 fiş")]
    public List<string> Anahtarlar { get; set; } = [];
}

public sealed class FaturaTopluServisi(KuyumcuDbContext db, FaturaServisi fatura, YasalParametreServisi? yasal = null)
{
    public async Task<TopluAdayListesi> Adaylar(AktifKullanici islem, int gun = 1)
    {
        if (gun is < 1 or > 31) throw new BadRequestException("Gün 1 ile 31 arasında olmalıdır").AlanIle("gun");
        var (bas, _) = Zaman.YerelGunAraligiUtc(Zaman.YerelBugun().AddDays(-(gun - 1)));
        var aday = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == "SATIS" && h.Tarih >= bas && h.StornoKaynakId == null);
        var satirlar = await FaturaHareketBagi.Bagsiz(db, islem.TenantId, aday)
            .StornoCiftiniDisla(db, islem.TenantId)
            .OrderByDescending(h => h.Tarih).ThenBy(h => h.Id)
            .Take(1000)
            .Select(h => new { h.Id, h.FisId, h.Tarih, h.CariId, h.TlTutari })
            .ToListAsync();

        var cariIdler = satirlar.Where(s => s.CariId != null).Select(s => s.CariId!).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id))
            .Take(cariIdler.Count)
            .ToDictionaryAsync(c => c.Id, c => new { c.Ad, c.SistemCarisi, c.VergiNo });
        var had = yasal?.SatirBul(YasalAnahtar.KuyumcuFaturaHaddiTl, YasalParametreServisi.IslemGunu(DateTime.UtcNow))?.DegerSayi;

        var adaylar = satirlar
            .GroupBy(s => s.FisId ?? s.Id)
            .Select(g =>
            {
                var ilk = g.OrderBy(x => x.Tarih).First();
                var cari = ilk.CariId is null ? null : cariler.GetValueOrDefault(ilk.CariId);
                var tutar = g.Sum(x => Math.Abs(x.TlTutari));
                string? sebep = cari is null || cari.SistemCarisi
                    ? TopluMetin.PesinMusteri
                    : had is { } h && tutar > h && !GecerliKimlik(cari.VergiNo) ? TopluMetin.KimlikYok
                    : null;
                return new TopluAday(g.Key, ilk.FisId, ilk.FisId is null ? ilk.Id : null, ilk.Tarih, ilk.CariId, cari?.Ad,
                    tutar, g.Count(), sebep is null, sebep);
            })
            .OrderByDescending(a => a.Tarih)
            .Take(200)
            .ToList();
        return new TopluAdayListesi(gun, adaylar);
    }

    public async Task<TopluSonuc> Kes(TopluFaturaDto dto, AktifKullanici islem)
    {
        var anahtarlar = dto.Anahtarlar.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
        if (anahtarlar.Count == 0) throw new BadRequestException("En az bir fiş seçin").AlanIle("anahtarlar");
        if (anahtarlar.Count > 100) throw new BadRequestException("Tek seferde en çok 100 fiş").AlanIle("anahtarlar");

        // Aday kümesi sunucuda YENİDEN kurulur — istemcinin "kesilebilir" dediğine güvenilmez.
        var adaylar = (await Adaylar(islem, 31)).Adaylar.ToDictionary(a => a.Anahtar);
        var sonuc = new List<TopluSonucSatiri>();
        foreach (var anahtar in anahtarlar)
        {
            if (!adaylar.TryGetValue(anahtar, out var a))
            {
                sonuc.Add(new(anahtar, null, null, null, TopluMetin.AdayDegil));
                continue;
            }
            if (!a.Kesilebilir)
            {
                sonuc.Add(new(anahtar, null, null, null, a.Sebep));
                continue;
            }
            await using var tx = await db.Database.IsBaslatVeyaDevral();
            try
            {
                var taslak = JsonSerializer.SerializeToElement(await fatura.FistenOlustur(new FaturaFistenDto
                {
                    FisId = a.FisId, HareketId = a.HareketId, CariId = a.CariId, KdvTuru = "KUYUM",
                }, islem));
                var faturaId = taslak.GetProperty("Id").GetString()!;
                var son = islem.Rol is "SAHIP" or "MUDUR" or "MUHASEBE"
                    ? JsonSerializer.SerializeToElement(await fatura.DurumGuncelle(faturaId, new DurumGuncelleDto { Durum = "KESILDI" }, islem))
                    : taslak;
                if (tx is not null) await tx.CommitAsync();
                sonuc.Add(new(anahtar, faturaId, Oku(son, "FaturaNo", "faturaNo"), Oku(son, "Durum", "durum"), null));
            }
            catch (ApiException ex)
            {
                if (tx is not null) await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                sonuc.Add(new(anahtar, null, null, null, ex.Message));
            }
        }
        return new TopluSonuc(sonuc.Count(s => s.FaturaId != null), sonuc.Count(s => s.FaturaId == null), sonuc);
    }

    private static bool GecerliKimlik(string? no) =>
        !string.IsNullOrWhiteSpace(no) && Kuyumcu.Core.Domain.KimlikNo.Dogrula(no, Kuyumcu.Core.Domain.KimlikNo.TurTuret(no)) is null;

    private static string? Oku(JsonElement e, string a, string b) =>
        e.ValueKind == JsonValueKind.Object && (e.TryGetProperty(a, out var v) || e.TryGetProperty(b, out v)) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;
}

/// <summary>Toplu fatura sebep metinleri (sunucu iletisi — SunucuIletiSozlugu çiftleri).</summary>
public static class TopluMetin
{
    public static string PesinMusteri => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Peşin Müşteri'ye fatura kesilmez — fişe müşteri bağlayın");
    public static string KimlikYok => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Fatura sınırı üstünde müşterinin TC / vergi numarası yok");
    public static string AdayDegil => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu fiş faturalanabilir değil (faturası var ya da bulunamadı)");
}

[ApiController]
[Authorize]
[Route("api/v1/faturalar/toplu")]
[Yetki.ModulYetkisi("fatura")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class FaturaTopluController(FaturaTopluServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/faturalar/toplu?gun=1 — faturasız satış fişleri (bugün = 1).</summary>
    [HttpGet]
    public Task<TopluAdayListesi> Adaylar([FromQuery] int gun = 1) => servis.Adaylar(Islem, gun);

    /// <summary>POST /api/v1/faturalar/toplu — seçilen fişlere fatura keser (her fiş kendi işleminde).</summary>
    [HttpPost]
    public Task<TopluSonuc> Kes([FromBody] TopluFaturaDto dto) => servis.Kes(dto, Islem);
}
