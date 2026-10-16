using System.Text.Json;

namespace Kuyumcu.Api.Common;

/// <summary>
/// NestJS hata sözleşmesini koruyan istisnalar:
/// { "message": "...", "error": "Not Found", "statusCode": 404 }
/// </summary>
public class ApiException(int statusCode, string mesaj, string? hataAdi = null)
    : Exception(mesaj)
{
    public int StatusCode { get; } = statusCode;
    public string HataAdi { get; } = hataAdi ?? VarsayilanAd(statusCode);

    private static string VarsayilanAd(int kod) => kod switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        413 => "Payload Too Large",
        _   => "Internal Server Error",
    };
}

public sealed class NotFoundException(string mesaj) : ApiException(404, mesaj);
public sealed class BadRequestException(string mesaj) : ApiException(400, mesaj);
public sealed class UnauthorizedException(string mesaj) : ApiException(401, mesaj);
public sealed class ForbiddenException(string mesaj) : ApiException(403, mesaj);
public sealed class ConflictException(string mesaj) : ApiException(409, mesaj);

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-05 (Y-2) — ALAN ALTI ÇAKIŞMA
//
// ConflictException yalnız bir CÜMLE taşır; kullanıcı hangi alanı düzelteceğini
// bilemez. Benzersizlik ihlalinde (veritabanı 1062 ya da servisin ön kontrolü)
// hata TEK bir alana aittir ve ekranda o alanın ALTINDA görünmelidir.
// Gövdeye eklenen tek yeni alan `alanlar`dır; message/error/statusCode alan
// kümesi ve SIRASI aynen korunur (Nest sözleşmesi, T-4a mührü).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Belirli bir form alanına ait 409 çakışması.</summary>
public sealed class AlanCakismasiException(string alan, string mesaj)
    : ApiException(409, mesaj)
{
    /// <summary>İstemcinin `alanlar` sözlüğündeki anahtar (camelCase).</summary>
    public string Alan { get; } = alan;
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-3 — ALAN ALTI KURAL REDDİ (400)
//
// Kural reddi (ör. "Peşin Müşteri ile bu tutarda işlem yapılamaz") bir form
// alanına aittir: ekran "gerçek müşteri seçin" bandını müşteri alanının altında
// gösterebilsin diye gövdeye `alanlar` eklenir. İstisnanın TİPİ DEĞİŞMEZ
// (BadRequestException kalır — mevcut testler ve çağıranlar tipe bakar);
// işaret Exception.Data üzerinden taşınır. message/error/statusCode sırası
// aynen korunur (T-4a mührü).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir ApiException'a "bu red şu form alanına ait" işaretini koyar.</summary>
public static class AlanIsareti
{
    private const string Anahtar = "kuyumcu.alan";

    /// <summary>İstisnayı <paramref name="alan"/> (camelCase) ile işaretler ve AYNI nesneyi döndürür.</summary>
    public static TEx AlanIle<TEx>(this TEx ex, string alan) where TEx : ApiException
    {
        ex.Data[Anahtar] = alan;
        return ex;
    }

    /// <summary>İşaretli alan; işaret yoksa null.</summary>
    public static string? Alan(ApiException ex) => ex.Data[Anahtar] as string;

    private const string KodAnahtari = "kuyumcu.kod";

    /// <summary>
    /// PAKET YASAL-3 (§ 4.2): reddin MAKİNE KODU (ör. PESIN_ART_ARDA_SORU) — ekran
    /// soru/karar penceresini metne bakmadan açsın. Gövdeye `kod` YALNIZ işaretli
    /// istisnada eklenir; işaretsiz gövdeler DEĞİŞMEZ (T-4a mührü).
    /// </summary>
    public static TEx KodIle<TEx>(this TEx ex, string kod) where TEx : ApiException
    {
        ex.Data[KodAnahtari] = kod;
        return ex;
    }

    /// <summary>İşaretli kod; işaret yoksa null.</summary>
    public static string? Kod(ApiException ex) => ex.Data[KodAnahtari] as string;
}

/// <summary>ApiException'ları Nest formatında JSON'a çeviren middleware.</summary>
public sealed class HataYakalayici(RequestDelegate next, ILogger<HataYakalayici> logger)
{
    // ⚠ PAKET T-4a: DÖRT YAZMA DALI DA AYNI YARDIMCIDAN GEÇER. Gövdeye
    //   eklenen tek yeni alan istekKimligi'dir; mevcut alan kümeleri (message /
    //   error / statusCode) AYNEN korunur — Nest sözleşmesi ve var olan testler
    //   buna dayanıyor. 500 dalında `error` alanı YOKTUR ve EKLENMEZ
    //   (Bertan koşulu): hataAdi null geçilince alan hiç basılmaz.
    private static async Task Yaz(
        HttpContext ctx, int kod, string mesaj, string? hataAdi,
        IReadOnlyDictionary<string, string[]>? alanlar = null, string? makineKodu = null)
    {
        ctx.Response.StatusCode = kod;
        ctx.Response.ContentType = "application/json; charset=utf-8";

        // ⚠ PAKET DIL-SUNUCU: TEK ÇIKIŞ NOKTASI — sunucunun hata cümlesi istek diline göre
        //   sözlükten seçilir (message VE alan altı mesajlar). TR istekte cümle AYNEN kalır;
        //   alan anahtarları ve gövde şekli DEĞİŞMEZ.
        mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(mesaj);
        if (alanlar is { Count: > 0 })
            alanlar = alanlar.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(m => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(m)).ToArray());

        // Alan SIRASI da korunur: message, alanlar?, error, statusCode, istekKimligi.
        // `alanlar` YALNIZ dolu olduğunda basılır — mevcut testlerin gördüğü
        // gövdeler DEĞİŞMEZ (P-05: yalnız alan altı çakışmada eklenir).
        var govde = new Dictionary<string, object?> { ["message"] = mesaj };
        if (alanlar is { Count: > 0 }) govde["alanlar"] = alanlar;
        if (makineKodu is not null) govde["kod"] = makineKodu;   // YASAL-3: yalnız işaretli redde
        if (hataAdi is not null) govde["error"] = hataAdi;
        govde["statusCode"] = kod;
        govde[IstekKimligiAlani.GovdeAlani] = ctx.IstekKimligi();

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(govde));
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (AlanCakismasiException ex)
        {
            // P-05: alan altı 409 — ekran mesajı ilgili alanın altında gösterir.
            await Yaz(ctx, ex.StatusCode, ex.Message, ex.HataAdi,
                new Dictionary<string, string[]> { [ex.Alan] = [ex.Message] });
        }
        catch (ApiException ex)
        {
            // ⚠ PAKET P-31 (R-5): 5xx ApiException da BEKLENMEYEN hatadır —
            //   kullanıcı destek kodu görür; aynı kod günlükte de bulunmalı.
            //   Önceden bu dal hiç günlüğe yazmıyordu (kod ekranda, günlükte yok).
            //   503 BİLİNÇLİ kapalı durumdur (kanal kapısı, sistem anahtarı yok) —
            //   beklenmeyen hata değildir, hata düzeyinde yazılmaz.
            if (ex.StatusCode >= 500 && ex.StatusCode != 503)
                logger.LogError(ex, "Sunucu hatası ({Kod}): {Yol} · istek: {IstekKimligi} · hata kodu: {HataKodu}",
                    ex.StatusCode, ctx.Request.Path, ctx.IstekKimligi(),
                    Kuyumcu.Core.Sozlesmeler.HataKodu.Kisa(ctx.IstekKimligi()));
            // PAKET S2-3: alan işaretli red (ör. Peşin Müşteri eşiği → cariId)
            // `alanlar` taşır; işaretsiz istisnaların gövdesi DEĞİŞMEZ.
            var alan = AlanIsareti.Alan(ex);
            await Yaz(ctx, ex.StatusCode, ex.Message, ex.HataAdi,
                alan is null ? null : new Dictionary<string, string[]> { [alan] = [ex.Message] },
                AlanIsareti.Kod(ex));
        }
        // ⚠ PAKET 4: uç düzeyindeki gövde sınırı ([RequestSizeLimit]) aşıldığında
        // Kestrel BadHttpRequestException(413) fırlatır. Yakalanmazsa kullanıcı
        // "Internal server error" (500) görürdü — oysa hata istemcinindir ve
        // sebebi söylenebilir. Canlı doğrulamada 6 MB PDF ile ölçüldü.
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
        {
            await Yaz(
                ctx,
                ex.StatusCode,
                ex.StatusCode == 413
                    ? "İstek gövdesi çok büyük — belge tavanını aşıyor"
                    : "İstek gövdesi okunamadı",
                ex.StatusCode == 413 ? "Payload Too Large" : "Bad Request");
        }
        // ⚠ PAKET 25 — İYİMSER KİLİT: stok_yerlesimleri.guncellendi
        // [ConcurrencyCheck] taşır. İki satış aynı yerleşim satırını aynı anda
        // düşürürse ikinci yazma 0 satır günceller ve EF bunu fırlatır.
        // 500 dönmek yanlış olurdu: hata istemcinin değil, YARIŞINDIR ve
        // isteğin tekrarlanması çözer.
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Eşzamanlı güncelleme çakışması: {Yol}", ctx.Request.Path);
            await Yaz(
                ctx,
                409,
                "Bu kayıt siz işlem yaparken başka bir kullanıcı tarafından değiştirildi — "
                + "ekranı yenileyip işlemi tekrarlayın.",
                "Conflict");
        }
        // ⚠ PAKET P-05 (Y-2) — BENZERSİZLİK İHLALİ (MySQL 1062).
        //   Doğal anahtar kısıtları artık VERİTABANINDA. Servislerin AnyAsync
        //   ön kontrolü ile INSERT arasındaki pencerede ikinci istek gelirse
        //   kısıt onu reddeder; yakalanmazsa kullanıcı 500 görürdü — oysa hata
        //   istemcinindir, sebebi söylenebilir ve TEK bir alana aittir.
        //   ⚠ SIRA ÖNEMLİ: DbUpdateConcurrencyException DbUpdateException'dan
        //     TÜRER, bu yüzden yukarıda (iyimser kilit dalında) kalır.
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (BenzersizlikIhlaliMi(ex))
        {
            var indeks = Kuyumcu.Core.I18n.BenzersizlikSozlugu.IndeksAdiniAyikla(
                ex.InnerException?.Message);
            var ihlal = Kuyumcu.Core.I18n.BenzersizlikSozlugu.Coz(indeks);
            // Günlüğe ham metin (indeks adı dâhil), kullanıcıya YALNIZ Türkçe cümle.
            logger.LogWarning(ex, "Benzersizlik ihlali: {Yol} · indeks: {Indeks}",
                ctx.Request.Path, indeks ?? "(çözülemedi)");
            await Yaz(ctx, 409, ihlal.Mesaj, "Conflict",
                new Dictionary<string, string[]> { [ihlal.Alan] = [ihlal.Mesaj] });
        }
        catch (Exception ex)
        {
            // ⚠ Kimlik ÖNCE günlüğe: kullanıcının göreceği kod ile sunucudaki
            //   yığın izi aynı satırda eşleşsin (IstekKimligiAraci kapsamı
            //   zaten basar; burada ayrıca alan olarak yazılır ki kapsam
            //   biçimlendiricisi olmayan sağlayıcıda da görünsün).
            // ⚠ PAKET P-31 (R-5): kullanıcının ekranda gördüğü KISA kod da
            //   satıra yazılır — destek kısa kodla günlükte doğrudan arar.
            //   Eşleşme YALNIZ sunucu tarafındadır; satır kişisel veri taşımaz
            //   (yol + kimlik + kod; gövde/sorgu dizgesi/kullanıcı YAZILMAZ).
            logger.LogError(ex, "Beklenmeyen hata: {Yol} · istek: {IstekKimligi} · hata kodu: {HataKodu}",
                ctx.Request.Path, ctx.IstekKimligi(),
                Kuyumcu.Core.Sozlesmeler.HataKodu.Kisa(ctx.IstekKimligi()));
            // 500 gövdesi: message + statusCode + istekKimligi. `error` YOK.
            await Yaz(ctx, 500, "Internal server error", hataAdi: null);
        }
    }

    /// <summary>
    /// PAKET P-05: yazma hatası bir BENZERSİZLİK ihlali mi? Sağlayıcı tipine
    /// derleme bağımlılığı kurmamak için hata NUMARASI yansımayla okunur
    /// (MySqlException.Number = 1062). Numara okunamazsa metne düşülür.
    /// </summary>
    private static bool BenzersizlikIhlaliMi(Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var ic = ex.InnerException;
        if (ic is null) return false;
        var numara = ic.GetType().GetProperty("Number")?.GetValue(ic);
        if (numara is int kod) return kod == 1062;
        return ic.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }
}
