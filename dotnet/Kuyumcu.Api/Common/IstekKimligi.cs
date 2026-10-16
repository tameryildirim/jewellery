namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// İSTEK KİMLİĞİ (korelasyon) — paket T-4 Faz A (T-4a).
//
// HEDEF: kullanıcı bir hata gördüğünde TEK bir kopyalanabilir kod söylesin;
// destek o kodla sunucu günlüğündeki isteği kesin olarak bulsun.
//
// ⚠ İSTEMCİYE GÜVENİLMEZ: gelen X-Istek-Kimligi başlığı OKUNMAZ. Okunsaydı
//   istemci kendi seçtiği kimliği gönderip başkasının kaydıyla aynı satıra
//   yazabilirdi (günlük zehirlenmesi). Kimliği HER ZAMAN sunucu üretir ve
//   giden başlığı EZER.
//
// ⚠ TEK ÜRETİCİ: kimlik yalnız burada üretilir. Okuma tek yardımcıyla yapılır
//   (HttpContext.IstekKimligi()); ikinci bir üretici/okuyucu açılmaz.
//
// ⚠ BİÇİM: ULID (26 karakter, Common/Ulid.cs ile aynı üretici). Tahmin
//   edilebilir sayaç kullanılmaz; kiracı/kullanıcı kimliği kimliğe GÖMÜLMEZ
//   (kimlik kişisel veri değildir, her rolde gösterilebilir).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İstek kimliğinin taşındığı sabitler — dizge tek yerde yazılıdır.</summary>
public static class IstekKimligiAlani
{
    /// <summary>Yanıt başlığı adı.</summary>
    public const string Baslik = "X-Istek-Kimligi";

    /// <summary>HttpContext.Items anahtarı.</summary>
    public const string OgeAnahtari = "istek_kimligi";

    /// <summary>Hata gövdesindeki alan adı (camelCase — API sözleşmesi).</summary>
    public const string GovdeAlani = "istekKimligi";

    /// <summary>ILogger kapsam anahtarı (sunucu günlüğünde aranan ad).</summary>
    public const string GunlukAlani = "IstekKimligi";
}

/// <summary>
/// Her isteğe bir ULID verir: <see cref="HttpContext.Items"/>, TraceIdentifier,
/// <c>X-Istek-Kimligi</c> yanıt başlığı ve ILogger kapsamı AYNI değeri taşır.
/// </summary>
public sealed class IstekKimligiAraci(RequestDelegate next, ILogger<IstekKimligiAraci> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var kimlik = Kimlik.Ulid();

        ctx.Items[IstekKimligiAlani.OgeAnahtari] = kimlik;
        // TraceIdentifier de aynı değere çekilir: denetim izi ve çerçevenin
        // kendi günlükleri zaten bunu basar (SistemModulu.cs "· istek:").
        ctx.TraceIdentifier = kimlik;
        // Başlık, yanıt AKMAYA BAŞLAMADAN yazılır. Hata yolunda gövdeyi
        // HataYakalayici yazar; başlık o an çoktan yerindedir.
        ctx.Response.Headers[IstekKimligiAlani.Baslik] = kimlik;

        // Kapsam: bu isteğin ALTINDAKİ her günlük satırı kimliği taşır —
        // yalnız gövdeye yazılsaydı destek kodu alır ama sunucuda ARAYAMAZDI.
        // ⚠ BİÇİMLİ KAPSAM (sözlük DEĞİL): konsol biçimlendiricisi kapsamı
        //   ToString() ile yazar; sözlük verilseydi günlükte kimlik yerine
        //   "System.Collections.Generic.Dictionary`2[...]" görünürdü (canlıda
        //   ölçüldü). Biçimli kapsam hem okunur metni hem yapısal alan adını
        //   (IstekKimligi) taşır — grep de, yapısal sorgu da çalışır.
        using (logger.BeginScope("IstekKimligi:{IstekKimligi}", kimlik))
        {
            await next(ctx);
        }
    }
}

/// <summary>İstek kimliğini okumanın TEK yolu.</summary>
public static class IstekKimligiUzantilari
{
    /// <summary>
    /// Bu isteğin kimliği. Ara katman çalışmadıysa (ör. birim testte elle
    /// kurulan bağlam) çerçevenin TraceIdentifier'ı döner — uydurulmaz.
    /// </summary>
    public static string IstekKimligi(this HttpContext ctx)
        => ctx.Items.TryGetValue(IstekKimligiAlani.OgeAnahtari, out var d) && d is string s && s.Length > 0
            ? s
            : ctx.TraceIdentifier;
}
