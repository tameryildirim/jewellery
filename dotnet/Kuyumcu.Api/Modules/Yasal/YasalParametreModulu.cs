using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kuyumcu.Contracts.Sayfalar.YasalSinirlar;

namespace Kuyumcu.Api.Modules.Yasal;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — YASAL SINIRLAR (salt okunur uçlar)
//
// Kiracı bu rakamları DEĞİŞTİREMEZ (ulusal rakam; yanlış giriş ceza doğurur):
// yazma yalnız migration + POST /api/v1/sistem/yasal-parametreler (sistem anahtarı,
// PAKET YASAL-PARAM-EKRAN: önizleme + onay kodu, bugünden ya da ileri tarihten geçerli).
// DoD sözleşmesi: varlık DEĞİŞMEZ — ekranda DegismezIpucu gerekçesi yazılıdır.
// Kişisel veri YOKTUR.
// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Authorize]
[Route("api/v1/yasal-parametreler")]
public sealed class YasalParametreController(
    YasalParametreServisi yasal, TakvimAyarlari takvim, Kuyumcu.Api.Modules.Sistem.SistemAyarlari sistem) : ControllerBase
{
    /// <summary>GET /api/v1/yasal-parametreler?tarih=YYYY-MM-DD — o gün geçerli tüm rakamlar.</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public YasalListeVeri Liste([FromQuery] DateOnly? tarih)
    {
        var gun = tarih ?? yasal.Bugun;
        return new YasalListeVeri(
            Gun(gun), Gun(yasal.Bugun),
            yasal.Gecerliler(gun).Select(Yansit).ToList(),
            yasal.Tatiller(gun.Year).Select(Gun).ToList(),
            yasal.BayramEksik(gun.Year, takvim),
            yasal.Sonrakiler(gun).Select(Yansit).ToList(),
            // Tur 3 (Mehmet notu): değişiklik formu yalnız ByGatech'in yönetim kiracısında görünür.
            sistem.YonetimKiracisiMi(User.FindFirst("tenantId")?.Value));
    }

    /// <summary>GET /api/v1/yasal-parametreler/gecmis?anahtar= — bir rakamın tarihçesi.</summary>
    [HttpGet("gecmis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public YasalGecmisVeri Gecmis([FromQuery] string? anahtar)
    {
        if (string.IsNullOrWhiteSpace(anahtar) || !YasalAnahtar.Birimler.ContainsKey(anahtar))
            throw new BadRequestException("Anahtar tanımlı yasal parametreler listesinde değil.").AlanIle("anahtar");
        return new YasalGecmisVeri(anahtar, yasal.Gecmis(anahtar).Select(Yansit).ToList());
    }

    /// <summary>
    /// GET /api/v1/yasal-parametreler/yaklasan — önümüzdeki 30 günde başlayacak
    /// değişiklikler + bu yılın bayram günleri girildi mi (Panel bandı).
    /// </summary>
    [HttpGet("yaklasan")]
    [HerkeseAcik("Yalnız ulusal yasal rakamların yaklaşan değişikliği — kişisel ve kiracı verisi yok; Panel bandı her rolde çağırır")]
    public YasalYaklasanVeri Yaklasan()
    {
        var bugun = yasal.Bugun;
        return new YasalYaklasanVeri(
            Gun(bugun),
            YasalParametreServisi.YaklasanUfkuGun,
            YasalParametreServisi.BantUfkuGun,
            yasal.Yaklasan(bugun, YasalParametreServisi.YaklasanUfkuGun).Select(y => new YasalDegisiklikVeri(
                y.Yeni.Anahtar, y.Yeni.AciklamaSade, y.Yeni.Birim, Gun(y.Yeni.Baslangic),
                y.Yeni.Baslangic.DayNumber - bugun.DayNumber,
                y.Yeni.DegerSayi, y.Yeni.DegerMetin, y.Bugunku?.DegerSayi, y.Bugunku?.DegerMetin,
                y.Yeni.Teyit,
                YasalAnahtar.EsikGosterimi(y.Yeni.Anahtar, y.Yeni.DegerSayi).Tur,
                YasalAnahtar.EsikGosterimi(y.Yeni.Anahtar, y.Yeni.DegerSayi).Tutar,
                YasalAnahtar.EsikGosterimi(y.Yeni.Anahtar, y.Bugunku?.DegerSayi).Tutar,
                YasalAnahtar.EsikGosterimi(y.Yeni.Anahtar, y.Bugunku?.DegerSayi).Tur)).ToList(),
            bugun.Year,
            yasal.BayramEksik(bugun.Year, takvim));
    }

    private static string Gun(DateOnly g) => g.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private YasalSatirVeri Yansit(YasalParametreSatiri s)
    {
        var esik = YasalAnahtar.EsikGosterimi(s.Anahtar, s.DegerSayi);
        return new(
        s.Id, s.Anahtar, s.DegerSayi, s.DegerMetin, s.Birim, Gun(s.Baslangic),
        s.Bitis is { } b ? Gun(b) : null, s.Dayanak, s.KaynakUrl, s.Teyit, s.AciklamaSade,
        // YASAL-1 tur 2: program bu rakamı bir kuralda kullanıyor mu (ekran grubu).
        YasalAnahtar.ProgramdaKullanilan.Contains(s.Anahtar) && s.Teyit != YasalTeyit.Kapali,
        esik.Tur, esik.Tutar,
        // PAKET YASAL-PARAM-EKRAN: kuyumcu kaynağı (RG tarih/sayı) salt okunur görür.
        s.ResmiGazeteTarihi is { } rg ? Gun(rg) : null, s.ResmiGazeteSayisi,
        // PAKET YASAL-PARAM-2: bugünden geçerli satırın yürürlük saati (yerel) + aynı gün düzeltilebilir mi.
        s.BaslangicAni is { } a
            ? Zaman.UtcdenYerele(DateTime.SpecifyKind(a, DateTimeKind.Utc)).ToString("HH:mm", CultureInfo.InvariantCulture)
            : null,
        yasal.AyniGunDuzeltilebilir(s));
    }
}
