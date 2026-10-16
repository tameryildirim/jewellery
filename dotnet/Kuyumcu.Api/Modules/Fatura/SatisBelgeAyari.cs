using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// M6 (müşteri 16.10: "fişe hiç gerek yok… 30'un altında kalsın fark etmez, TC'yi yazın,
// sisteme girdiğim anda fatura kessin"; "30 bin lira geçtiğinde isim konulmalı") —
// Tezgâh satış belgesi: işletme varsayılanı (FIS | FATURA) + bugünkü KUYUMCU fatura haddi
// (VUK GT 514 md.4: genel haddin 3 katı — yasal parametre tablosundan, işlem günü).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SatisBelgeAyarDto
{
    public string? Varsayilan { get; set; }
}

[ApiController]
[Authorize]
[Route("api/v1/satis-belge")]
[Yetki.ModulYetkisi("islem")]   // Tezgâh okur; yazma ayrıca SAHIP/MUDUR
public sealed class SatisBelgeController(KuyumcuDbContext db, DenetimServisi denetim, YasalParametreServisi? yasal = null) : ControllerBase
{
    public const string Fis = "FIS";
    public const string Fatura = "FATURA";
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/satis-belge — varsayılan belge türü + bugünkü kuyumcu fatura haddi (yoksa null).</summary>
    [HttpGet]
    public async Task<object> Getir()
    {
        var deger = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == Islem.TenantId && a.Anahtar == KiraciAyarAnahtari.SatisBelgeVarsayilan)
            .Select(a => a.Deger).FirstOrDefaultAsync();
        var satir = yasal?.SatirBul(YasalAnahtar.KuyumcuFaturaHaddiTl, yasal.Bugun);
        return new
        {
            varsayilan = deger == Fatura ? Fatura : Fis,
            kuyumcuFaturaHaddi = satir?.DegerSayi,
            haddiDayanak = satir?.Dayanak,
        };
    }

    /// <summary>PUT /api/v1/satis-belge — varsayılanı yazar (SAHIP/MUDUR), denetim izine düşer.</summary>
    [HttpPut]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<object> Yaz([FromBody] SatisBelgeAyarDto? dto)
    {
        var yeni = dto?.Varsayilan;
        if (yeni is not (Fis or Fatura))
            throw new BadRequestException("Belge türü FIS ya da FATURA olmalıdır").AlanIle("varsayilan");
        var islem = Islem;
        var now = DateTime.UtcNow;
        var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(
            a => a.TenantId == islem.TenantId && a.Anahtar == KiraciAyarAnahtari.SatisBelgeVarsayilan);
        if (kayit is null)
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = KiraciAyarAnahtari.SatisBelgeVarsayilan,
                Deger = yeni, Guncellendi = now,
            });
        else { kayit.Deger = yeni; kayit.Guncellendi = now; }
        denetim.YazmaKaydet(islem, "KiraciAyari", KiraciAyarAnahtari.SatisBelgeVarsayilan,
            $"Satış belgesi varsayılanı {yeni} yapıldı", hassas: false);
        await db.SaveChangesAsync();
        return await Getir();
    }
}
