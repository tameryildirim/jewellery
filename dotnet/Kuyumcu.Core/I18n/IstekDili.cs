namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — İSTEĞİN DİLİ (TEK KAYNAK)
//
// KURUCU BULGUSU (08.10): "ingilizceye cevirdigimizde ... turkce kaliyor". Sunucunun
// ekrana giden cümleleri (hata, uyarı, durum) Türkçe sabit yazılıydı; sınıfı kapatan
// TEK yol: istemci dilini Accept-Language başlığıyla söyler, sunucu cümleyi
// SunucuIletiSozlugu'ndan o dile göre seçer. İKİNCİ YOL AÇILMAZ (kod+parametre
// dönüp istemcide çevirme ile karışmaz).
//
// AsyncLocal: istek boyunca (middleware → uç → servis) aşağı akar; arka plan
// işlerinde ve testlerde değer YOKTUR → varsayılan "tr" (davranış ESKİSİYLE aynı).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Geçerli isteğin arayüz dili ("tr" | "en"). Yoksa "tr".</summary>
public static class IstekDili
{
    private static readonly AsyncLocal<string?> Deger = new();

    /// <summary>Geçerli dil kodu; atanmamışsa "tr".</summary>
    public static string Gecerli => Deger.Value ?? "tr";

    /// <summary>Dili bu mantıksal akışa yazar (middleware çağırır).</summary>
    public static void Ata(string? dil) => Deger.Value = Normalize(dil);

    /// <summary>Testler için kapsamlı atama: Dispose önceki değeri geri koyar.</summary>
    public static IDisposable Kapsam(string dil)
    {
        var onceki = Deger.Value;
        Deger.Value = Normalize(dil);
        return new Geri(onceki);
    }

    /// <summary>"en", "en-US;q=0.9" → "en"; diğer her şey → "tr".</summary>
    public static string Normalize(string? dil) =>
        dil is not null && dil.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";

    /// <summary>
    /// Accept-Language başlığından dil: İLK etiket belirleyicidir (istemci tek etiket
    /// gönderir: "en" ya da "tr"). Başlık yoksa/boşsa "tr".
    /// </summary>
    public static string BasliktanCoz(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return "tr";
        var ilk = acceptLanguage.Split(',', 2)[0].Split(';', 2)[0].Trim();
        return Normalize(ilk);
    }

    private sealed class Geri(string? onceki) : IDisposable
    {
        public void Dispose() => Deger.Value = onceki;
    }
}
