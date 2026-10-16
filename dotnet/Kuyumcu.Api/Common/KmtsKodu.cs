using System.Text.RegularExpressions;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 / Y4-C — KMTS TAKİP KODU: TEK DOĞRULAMA YERİ.
//
// MEVZUAT: KMTS yükümlülüğü incelenen metinlerde (Tebliğ 2026/1, RG 29.07.2026/33324
// + 12.08.2026 Usul-Esaslar değişikliği) "rafineriler ve KMTS'ye tabi tüzel kişiler"
// için; perakende kuyumcuya yükümlülük BULUNAMADI (T-7). Resmî kod BİÇİMİ
// yayımlanmadı → YASAL-1 anahtarı KMTS_KOD_DESENI KAPALI (boş) doğdu.
//   • Desen BOŞ iken: yalnız uzunluk (≤100) ve yazdırılabilir karakter (bugünkü davranış).
//   • Desen yazılınca (sistem ucu, ileri tarihli satır): ürün ve satış yolu AYNI
//     yardımcıdan 400 döner. Mevcut kayıtlar SİLİNMEZ.
// ═════════════════════════════════════════════════════════════════════════════

public static class KmtsKodu
{
    public const int EnFazla = 100;

    /// <summary>
    /// Ham değeri doğrular ve kırpar. Boş → null. Geçersizse 400 (alan: <paramref name="alan"/>).
    /// Desen o günün YASAL-1 satırından (<see cref="YasalAnahtar.KmtsKodDeseni"/>) okunur.
    /// </summary>
    public static string? Dogrula(string? ham, YasalParametreServisi? yasal, string alan = "kmtsKodu")
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        var kod = ham.Trim();
        if (kod.Length > EnFazla)
            throw new BadRequestException($"KMTS kodu en çok {EnFazla} karakter olabilir.").AlanIle(alan);
        if (kod.Any(c => char.IsControl(c)))
            throw new BadRequestException("KMTS kodunda yazdırılamayan karakter var.").AlanIle(alan);

        var desen = Desen(yasal);
        if (desen is not null && !Uyar(kod, desen))
            throw new BadRequestException(
                "KMTS kodu resmî biçime uymuyor. Rafineriden aldığınız belgede yazan kodu aynen girin.").AlanIle(alan);
        return kod;
    }

    /// <summary>O günün deseni; satır yoksa ya da değer boşsa null (serbest).</summary>
    public static string? Desen(YasalParametreServisi? yasal)
    {
        if (yasal is null) return null;
        var satir = yasal.SatirBul(YasalAnahtar.KmtsKodDeseni, yasal.Bugun);
        return string.IsNullOrWhiteSpace(satir?.DegerMetin) ? null : satir!.DegerMetin!.Trim();
    }

    /// <summary>Tam eşleşme; bozuk desen FAIL-CLOSED (uymuyor sayılır, kod kabul edilmez).</summary>
    public static bool Uyar(string kod, string desen)
    {
        try
        {
            return Regex.IsMatch(kod, $"^(?:{desen})$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>UX-S29: tarama en çok bu kadar kodlu ürüne bakar (sınır yanıtta yazılır).</summary>
    public const int TaramaSiniri = 5000;

    /// <summary>
    /// § 4.4 (hasım bulgusu 01.10): desen yazılınca MEVCUT kayıtlar aynı yardımcıyla
    /// taranır; uymayanlar listelenir, SİLİNMEZ. Desen yoksa (bugün KAPALI) tarama yok.
    /// </summary>
    public static async Task<(bool BicimDenetimi, List<KmtsUymayan> Uymayanlar, bool SinirdaMi)> Tara(
        KuyumcuDbContext db, string tenantId, YasalParametreServisi? yasal)
    {
        var desen = Desen(yasal);
        if (desen is null) return (false, [], false);
        var kodlu = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.KmtsKodu != null && u.KmtsKodu != "")
            .OrderBy(u => u.Kod)
            .Select(u => new KmtsUymayan(u.Id, u.Kod, u.Ad, u.KmtsKodu!))
            .Take(TaramaSiniri)
            .ToListAsync();
        return (true, kodlu.Where(u => !Uyar(u.KmtsKodu, desen)).ToList(), kodlu.Count >= TaramaSiniri);
    }

    public static async Task<bool> KullanimAcik(KuyumcuDbContext db, string tenantId) =>
        (await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Anahtar == KiraciAyarAnahtari.KmtsKullanimi)
            .Select(a => a.Deger)
            .FirstOrDefaultAsync()) == "true";
}

public sealed record KmtsUymayan(string UrunId, string Kod, string Ad, string KmtsKodu);

public sealed class KmtsAyarDto
{
    public bool? Acik { get; set; }
}

/// <summary>
/// GET/PUT /api/v1/kmts/ayar — kiracı ayarı (kiraci_ayarlari). Okuma her oturumlu
/// kullanıcıya açıktır (ürün formu alanı çizip çizmeyeceğini bilmeli); yazma SAHİP.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/kmts")]
public sealed class KmtsController(KuyumcuDbContext db, DenetimServisi denetim, YasalParametreServisi? yasal = null) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpGet("ayar")]
    [HerkeseAcik("Yalnız kendi kiracısının tek bir aç/kapa ayarı — kişisel veri yok; ürün formu alanı çizip çizmeyeceğini bilmeli")]
    public async Task<object> AyarGetir()
    {
        var (bicim, uymayan, _) = await KmtsKodu.Tara(db, Islem.TenantId, yasal);
        return new
        {
            acik = await KmtsKodu.KullanimAcik(db, Islem.TenantId),
            // Resmî biçim tanımlıysa ekran yalnız "biçim denetleniyor" der; desenin kendisi basılmaz.
            bicimDenetimi = bicim,
            // Yalnız SAYI (kişisel veri yok); liste rol korumalı uçta.
            uymayanSayisi = uymayan.Count,
        };
    }

    /// <summary>GET /api/v1/kmts/uymayanlar — desene uymayan mevcut ürün kodları (silinmez, listelenir).</summary>
    [HttpGet("uymayanlar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<object> Uymayanlar()
    {
        var (bicim, uymayan, sinirda) = await KmtsKodu.Tara(db, Islem.TenantId, yasal);
        return new { bicimDenetimi = bicim, sayi = uymayan.Count, urunler = uymayan.Take(200), sinir = KmtsKodu.TaramaSiniri, sinirdaMi = sinirda };
    }

    [HttpPut("ayar")]
    [RolGerekli("SAHIP")]
    public async Task<object> AyarYaz([FromBody] KmtsAyarDto? dto)
    {
        if (dto?.Acik is not { } acik)
            throw new BadRequestException("Açık/kapalı değeri zorunludur.").AlanIle("acik");
        var islem = Islem;
        var now = DateTime.UtcNow;
        var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(
            a => a.TenantId == islem.TenantId && a.Anahtar == KiraciAyarAnahtari.KmtsKullanimi);
        if (kayit is null)
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = KiraciAyarAnahtari.KmtsKullanimi,
                Deger = acik ? "true" : "false", Guncellendi = now,
            });
        else
        {
            kayit.Deger = acik ? "true" : "false";
            kayit.Guncellendi = now;
        }
        denetim.YazmaKaydet(islem, "KiraciAyari", KiraciAyarAnahtari.KmtsKullanimi,
            $"KMTS kodu alanı {(acik ? "AÇILDI" : "KAPATILDI")}", hassas: false);
        await db.SaveChangesAsync();
        return new { acik, bicimDenetimi = KmtsKodu.Desen(yasal) is not null };
    }
}
