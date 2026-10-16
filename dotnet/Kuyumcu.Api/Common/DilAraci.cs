using Kuyumcu.Core.I18n;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — İSTEK DİLİ ARACI
//
// İstemci arayüz dilini `Accept-Language` başlığıyla söyler ("en" | "tr"); bu araç
// onu IstekDili'ne yazar. Sunucunun ekrana giden her cümlesi (hata gövdesi,
// uyarı, künye) o dile göre SunucuIletiSozlugu'ndan seçilir.
//
// ⚠ Başlık yok/tanınmıyorsa "tr" (eski davranış BİREBİR). Dil yalnız ileti
//   seçer: veri, tutar, yetki ve iş kuralı dilden ETKİLENMEZ. Yanıta `Content-
//   Language` yazılır ki önbellek/vekiller farkı görsün.
// ⚠ UseRateLimiter'dan, HataYakalayici'dan ve kimlik doğrulamadan ÖNCE durur
//   (429/401 gövdeleri de istek diliyle yazılsın).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Accept-Language başlığından istek dilini çözer.</summary>
public sealed class DilAraci(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext ctx)
    {
        var dil = IstekDili.BasliktanCoz(ctx.Request.Headers.AcceptLanguage.ToString());
        IstekDili.Ata(dil);
        // Content-Language yalnız API yanıtlarında (statik dosya yanıtlarına dil damgası basılmaz).
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.Headers.ContentLanguage = dil;
        return next(ctx);
    }
}
