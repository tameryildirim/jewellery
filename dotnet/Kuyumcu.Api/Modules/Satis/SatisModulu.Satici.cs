using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Contracts.Sayfalar.SaticiPersonel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5b (PLAN 2.8) — SATICI PERSONEL (hareketler.personel_id)
//
// ANLAM KİLİDİ: personel_id = SATIŞI YAPAN ("satan"), kaydı GİREN değil
// (kaydı giren kullanici_id'dir). SatisRaporlari personel kırılımı bunu okur.
//
// KURAL (fail-closed):
//   · personelId gelmezse (null) → OTURUM KULLANICISI yazılır. Mevcut
//     istemciler (alanı göndermeyenler) değişmeden çalışır.
//   · boş dizge → 400 (belirsiz niyet; "boş = oturum" diye yorumlanmaz).
//   · oturum kullanıcısının kendisi → her rol için serbest.
//   · BAŞKA personel → yalnız SAHIP / MUDUR. Diğer roller 403 (yetki matrisi
//     "islem" modülü dışında satıcı kapısı TANIMLAMIYOR; tezgâhtarın
//     başkası adına satış yazması prim/performans kaydını bozar).
//   · seçilen kullanıcı AYNI KİRACIDA ve AKTİF olmalı; değilse 400. Başka
//     kiracının kimliği "bulunamadı" ile 400 döner (kiracı sızıntısı yok:
//     var/yok bilgisi verilmez).
// Personel bilgisi yasal belge alanı DEĞİLDİR (fiş/fatura zorunlu unsuru
// değil); yalnız iç kayıttır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satıcı seçim kuralının saf çekirdeği — rol kapısı tek yerde.</summary>
public static class SaticiKapisi
{
    /// <summary>Başka personeli "satan" olarak yazabilen roller (TAM eşleşme).</summary>
    public static readonly string[] SecebilenRoller = ["SAHIP", "MUDUR"];

    public static bool BaskasiniSecebilir(string? rol) =>
        rol is not null && SecebilenRoller.Contains(rol, StringComparer.Ordinal);

    public const string BosMesaji = "Satıcı personel boş olamaz; satan siz iseniz alanı göndermeyin.";
    public const string YetkiMesaji = "Başka bir personeli satıcı olarak yalnız işletme sahibi ya da müdür seçebilir.";
    public const string BulunamadiMesaji = "Seçilen satıcı personel bu işletmede bulunamadı.";
    public const string PasifMesaji = "Seçilen satıcı personel pasif; pasif kullanıcı adına satış yazılamaz.";

    /// <summary>Seçici listesinin tavanı (B-8: sınırsız liste yok). Aşılırsa DahaFazlasiVar döner (UX-S29).</summary>
    public const int AzamiListe = 200;
}

public sealed partial class SatisServisi
{
    /// <summary>
    /// Satışı yapan personeli çözer. Transaction AÇILMADAN çağrılır (K1).
    /// </summary>
    internal async Task<string> SaticiBelirle(string? personelId, AktifKullanici islem)
    {
        if (personelId is null) return islem.Id;
        if (string.IsNullOrWhiteSpace(personelId))
            throw new BadRequestException(SaticiKapisi.BosMesaji);
        if (personelId == islem.Id) return islem.Id;

        if (!SaticiKapisi.BaskasiniSecebilir(islem.Rol))
            throw new ForbiddenException(SaticiKapisi.YetkiMesaji);

        var k = await db.Kullanicilar.AsNoTracking()
            .Where(x => x.Id == personelId && x.TenantId == islem.TenantId)
            .Select(x => new { x.Aktif })
            .SingleOrDefaultAsync();
        if (k is null) throw new BadRequestException(SaticiKapisi.BulunamadiMesaji);
        if (!k.Aktif) throw new BadRequestException(SaticiKapisi.PasifMesaji);
        return personelId;
    }

    /// <summary>
    /// Satıcı seçici verisi. Seçim serbest değilse liste yalnız oturum
    /// kullanıcısıdır (ekran tek seçenekte seçiciyi gizler — UX-S22).
    /// </summary>
    public async Task<SaticiPersonelListesi> SaticiPersoneller(AktifKullanici islem)
    {
        var serbest = SaticiKapisi.BaskasiniSecebilir(islem.Rol);
        var liste = await db.Kullanicilar.AsNoTracking()
            .Where(x => x.TenantId == islem.TenantId && x.Aktif && (serbest || x.Id == islem.Id))
            .OrderBy(x => x.Ad).ThenBy(x => x.Soyad)
            .Select(x => new SaticiPersonelSatiri(x.Id, x.Ad, x.Soyad))
            .Take(SaticiKapisi.AzamiListe + 1)
            .ToListAsync();
        var fazla = liste.Count > SaticiKapisi.AzamiListe;
        if (fazla) liste.RemoveAt(liste.Count - 1);
        return new SaticiPersonelListesi(liste, islem.Id, serbest, fazla);
    }
}

/// <summary>GET /api/v1/islemler/personeller — satış ekranının "Satan" seçicisi.</summary>
[ApiController]
[Authorize]
[Route("api/v1/islemler")]
[Yetki.ModulYetkisi("islem")]
public sealed class SaticiPersonelController(SatisServisi servis) : ControllerBase
{
    /// <summary>İşlem modülüne erişen her rol okur; başkasını görebilen yalnız SAHIP/MUDUR.</summary>
    [HttpGet("personeller")]
    public Task<SaticiPersonelListesi> Personeller() =>
        servis.SaticiPersoneller(AktifKullanici.FromClaims(User));
}
