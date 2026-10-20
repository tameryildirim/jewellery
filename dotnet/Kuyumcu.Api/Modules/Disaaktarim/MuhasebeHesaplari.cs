using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Disaaktarim;

// Y-15 (20.10, rakip taraması: Terminal Gold "bir kez eşleştirme") — mali müşavirin hesap planı kodları BİR KEZ yazılır;
// Luca fişi satış/KDV satırlarında bu kodlar gider. Cari (120.xxx) kodu cari kartındadır (eksikse dosyaya girmez, OKUBENI listeler).
public sealed class MuhasebeHesapDto
{
    [MaxLength(30)] public string? SatisHesabi { get; set; }
    [MaxLength(30)] public string? KdvHesabi { get; set; }
}


public static class MuhasebeHesapKapisi
{
    public const string VarsayilanSatis = "600", VarsayilanKdv = "391";
    private static readonly Regex Bicim = new(@"^[0-9]{3}(\.[0-9]{1,6}){0,5}$", RegexOptions.Compiled);

    /// <summary>Hesap kodu: 3 hane + isteğe bağlı nokta alt kırılımlar (600, 600.01, 391.001.02). Boş = varsayılan.</summary>
    public static string? Temizle(string? kod, string alan)
    {
        if (string.IsNullOrWhiteSpace(kod)) return null;
        var k = kod.Trim();
        if (!Bicim.IsMatch(k)) throw new BadRequestException("Hesap kodu 600 ya da 600.01 biçiminde olmalıdır (rakam ve nokta)").AlanIle(alan);
        return k;
    }

    public static async Task<(string Satis, string Kdv)> Oku(KuyumcuDbContext db, string tenantId)
    {
        string[] anahtarlar = [KiraciAyarAnahtari.MuhasebeSatisHesabi, KiraciAyarAnahtari.MuhasebeKdvHesabi];
        var d = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && anahtarlar.Contains(a.Anahtar)).Take(2)
            .ToDictionaryAsync(a => a.Anahtar, a => a.Deger);
        var satis = d.GetValueOrDefault(KiraciAyarAnahtari.MuhasebeSatisHesabi);
        var kdv = d.GetValueOrDefault(KiraciAyarAnahtari.MuhasebeKdvHesabi);
        return (string.IsNullOrWhiteSpace(satis) ? VarsayilanSatis : satis, string.IsNullOrWhiteSpace(kdv) ? VarsayilanKdv : kdv);
    }
}

[ApiController]
[Authorize]
[Route("api/v1/muhasebe-hesaplari")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Yetki.ModulYetkisi("fatura")]
public sealed class MuhasebeHesapController(KuyumcuDbContext db, DenetimServisi denetim) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/muhasebe-hesaplari — etkin kodlar (boşsa varsayılan).</summary>
    [HttpGet]
    public async Task<Kuyumcu.Contracts.Sayfalar.Islemler.MuhasebeHesapOzet> Getir()
    {
        var (s, k) = await MuhasebeHesapKapisi.Oku(db, Islem.TenantId);
        return new Kuyumcu.Contracts.Sayfalar.Islemler.MuhasebeHesapOzet(s, k, MuhasebeHesapKapisi.VarsayilanSatis, MuhasebeHesapKapisi.VarsayilanKdv);
    }

    /// <summary>PUT /api/v1/muhasebe-hesaplari — kodları yaz (boş = varsayılana dön); denetim izine düşer.</summary>
    [HttpPut]
    public async Task<Kuyumcu.Contracts.Sayfalar.Islemler.MuhasebeHesapOzet> Yaz([FromBody] MuhasebeHesapDto? dto)
    {
        var islem = Islem;
        var satis = MuhasebeHesapKapisi.Temizle(dto?.SatisHesabi, "satisHesabi");
        var kdv = MuhasebeHesapKapisi.Temizle(dto?.KdvHesabi, "kdvHesabi");
        var now = DateTime.UtcNow;
        foreach (var (anahtar, deger) in new[] { (KiraciAyarAnahtari.MuhasebeSatisHesabi, satis), (KiraciAyarAnahtari.MuhasebeKdvHesabi, kdv) })
        {
            var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(a => a.TenantId == islem.TenantId && a.Anahtar == anahtar);
            if (deger is null) { if (kayit is not null) db.KiraciAyarlari.Remove(kayit); continue; }
            if (kayit is null) db.KiraciAyarlari.Add(new KiraciAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = anahtar, Deger = deger, Guncellendi = now });
            else { kayit.Deger = deger; kayit.Guncellendi = now; }
        }
        denetim.YazmaKaydet(islem, "KiraciAyari", KiraciAyarAnahtari.MuhasebeSatisHesabi,
            $"muhasebe hesap kodları: satış {satis ?? MuhasebeHesapKapisi.VarsayilanSatis} · KDV {kdv ?? MuhasebeHesapKapisi.VarsayilanKdv}", hassas: false);
        await db.SaveChangesAsync();
        return await Getir();
    }
}
