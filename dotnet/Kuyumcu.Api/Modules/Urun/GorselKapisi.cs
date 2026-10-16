using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Urun;

/// <summary>
/// SAF görsel adresi kapısı — veritabanı GÖRMEZ (PAKET P-10, Y-3).
///
/// <para>
/// ⚠ NEDEN VAR: <c>gorselUrl</c> kolonu sunucuda başından beri kabul ediliyordu
/// ama hiçbir ekran göndermiyordu; P-10 alanı forma bağlayınca değer ilk kez
/// KULLANICIDAN geliyor ve ürün kartında <c>&lt;img src&gt;</c> olarak
/// çiziliyor. Serbest metin kabul edilirse <c>data:</c> / <c>javascript:</c> /
/// <c>file:</c> şemaları depoya yazılır ve her istemcide farklı davranır.
/// Mehmet'in K4-4 ilkesi (BelgeKapisi'ndeki "tanınmayan her imza reddedilir")
/// adres yüzeyinde de geçerlidir: <b>kapalı liste</b> — yalnız
/// <c>http</c> ve <c>https</c>.
/// </para>
///
/// <para>
/// BOŞ = SİL: PATCH sözleşmesi gereği alan gövdede YOKSA korunur; gövdede
/// boş dizge geldiyse kullanıcı temizlemek istemiştir → NULL yazılır.
/// Eskiden boş dizge olduğu gibi saklanıyordu ve kart "adres var ama boş"
/// durumuna düşüyordu (sessiz etkisizlik — UX-S27 yasağı).
/// </para>
/// </summary>
public static class GorselKapisi
{
    /// <summary>İzin verilen tek şema listesi. Yeni şema buraya yazılmadan kabul edilmez.</summary>
    public static readonly string[] IzinliSemalar = ["http", "https"];

    /// <summary>
    /// Metin alanını normalleştirir: kırpılır, boşsa <c>null</c> olur.
    /// Rfid · Aciklama · GorselUrl gibi "boş dizge = temizle" alanlarının
    /// TEK yolu budur; her çağrı yerinde ayrı bir üçlü ifade yazılmaz.
    /// </summary>
    public static string? Bosla(string? ham)
        => string.IsNullOrWhiteSpace(ham) ? null : ham.Trim();

    /// <summary>
    /// Görsel adresini doğrular ve normalleştirir.
    /// Boş → <c>null</c>. Dolu → mutlak <c>http</c>/<c>https</c> olmak ZORUNDA.
    /// </summary>
    /// <exception cref="BadRequestException">Şema kapalı listede değilse.</exception>
    public static string? Dogrula(string? ham)
    {
        var deger = Bosla(ham);
        if (deger is null) return null;

        if (!Uri.TryCreate(deger, UriKind.Absolute, out var uri)
            || !IzinliSemalar.Contains(uri.Scheme))
            throw new BadRequestException(
                "Görsel adresi yalnız http:// veya https:// ile başlayabilir " +
                $"(verilen: {Kisalt(deger)})");

        return deger;
    }

    /// <summary>Hata mesajına ham adresin tamamı basılmaz — kütük şişer, veri sızar.</summary>
    private static string Kisalt(string deger)
        => deger.Length <= 40 ? deger : deger[..40] + "…";
}
