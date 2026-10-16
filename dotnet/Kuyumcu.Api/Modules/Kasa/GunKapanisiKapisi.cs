using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kasa;

/// <summary>
/// Gün kapanışı kilidi.
///
/// Kapanmış bir kasa-gününe yeni hareket yazılamaz. Yazılabilseydi sayım
/// tutanağının altı sonradan doldurulmuş olur, fark kaydı anlamını yitirirdi.
/// Kilit, kasanın kullanıldığı HER yolda aynı yerden uygulanır — kontrolün
/// tek tek çağrı noktalarına serpiştirilmesi "biri unutulur" demektir.
/// </summary>
public static class GunKapanisiKapisi
{
    /// <summary>
    /// Bu kasa, işlem anının YEREL gününde kapalı mı? Kapalıysa
    /// <see cref="BadRequestException"/> atar (fail-closed).
    /// </summary>
    public static async Task Dogrula(
        KuyumcuDbContext db, string tenantId, string kasaId, DateTime an)
    {
        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(an));

        var kapanis = await db.GunKapanislari.AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId
                                   && g.KasaId == kasaId
                                   && g.YerelGun == yerelGun);

        // Yeniden açılmış kapanış kilit uygulamaz (AcilisZamani dolu).
        if (kapanis is null || kapanis.AcilisZamani is not null) return;

        // PAKET SON-KALANLAR (hasım YÜKSEK, B1440__14): ileti sade Türkçe — büyük harfle bağırma yok,
        // rol kodu (SAHİP) yok, "TL" yerine kasanın para simgesi; kullanıcının yapacağı TEK şey yazılı.
        var paraBirimi = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.Id == kasaId)
            .Select(k => k.ParaBirimi).FirstOrDefaultAsync();
        throw new BadRequestException(
            $"Bu kasanın {yerelGun.ToString("dd.MM.yyyy", Tr)} günü kapandı "
            + $"(sayılan {Para(kapanis.SayilanNakit, paraBirimi)}). "
            + "Kapanmış güne kayıt yazılamaz; yeniden açmak için işletme sahibine başvurun.");
    }

    private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// PAKET SON-KALANLAR — sunucu iletilerinde tutar: tr-TR biçimi + kasanın para simgesi
    /// (UX-S32: "varsayılan TL" yok). Bilinmeyen kod simgesiz, kodla yazılır.
    /// </summary>
    public static string Para(decimal tutar, string? paraBirimi)
    {
        var t = tutar.ToString("N2", Tr);
        return (paraBirimi ?? "TRY") switch
        {
            "TRY" => $"{t} ₺",
            "USD" => $"{t} $",
            "EUR" => $"{t} €",
            "GBP" => $"{t} £",
            var kod => $"{t} {kod}",
        };
    }
}
