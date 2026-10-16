using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kuyumcu.Api.Modules.Sistem;

// ═════════════════════════════════════════════════════════════════════════════
// SİSTEM ANAHTARI KAPISI — paket F-2-min Faz A · MÜHÜR 4 / K1-K2.
//
// SİSTEM YÖNETİCİSİ KİMLİĞİ = YAPILANDIRMA SIRRI. 'SISTEM' diye bir ROL
// AÇILMADI: AktifKullanici.Seviye sözlüğüne, [RolGerekli] listelerine,
// BasitMenu'ye ve KenarCubugu'na DOKUNULMADI (K1). Bu dosya tek kimlik hattıdır.
//
// FAIL-CLOSED — uç şu üç durumda KAPALIDIR (503):
//   • Sistem:Anahtari boş                 → yapılandırılmamış
//   • 32 karakterden kısa                 → zayıf
//   • 'DEGISTIR' ile başlıyor             → appsettings.json yer tutucusu
// "Anahtar yok = serbest" davranışı YASAKTIR.
//
// Anahtar yalnız T-1 ortam-değişkeni sır hattından (Sistem__Anahtari) gelir;
// appsettings.json'da DEGISTIR- yer tutucusu durur. Karşılaştırma
// CryptographicOperations.FixedTimeEquals iledir ('==' yasak; dotnet/denetim.sh
// bunu ayrıca sayar). Anahtar DEĞERİ hiçbir log/denetim kaydına yazılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sistem uçlarının yapılandırması — rakam koda gömülmez (ADR-0005).</summary>
public sealed class SistemAyarlari
{
    /// <summary>
    /// Yer tutucu öneki — TEK KAYNAK: T-1 üretim açılış denetçisi. Dizge burada
    /// TEKRAR YAZILMAZ (dotnet/denetim.sh "DEGISTIR yalnız denetçi dosyasında"
    /// sayacı bunu zorlar).
    /// </summary>
    public static string YerTutucuOneki => Common.UretimAcilisDenetimi.DegistirOneki;
    public const int AsgariAnahtarUzunlugu = 32;

    public string Anahtari { get; init; } = "";
    public int VarsayilanDenemeGun { get; init; } = 14;
    public int DavetGecerlilikSaat { get; init; } = 72;

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN tur 3 (Mehmet notu): yasal sınır değişiklik formunun GÖRÜNDÜĞÜ kiracılar
    /// (ByGatech'in kendi yönetim kiracısı). Yeni ROL DEĞİL, yetki DEĞİL — yalnız ekran görünürlüğü;
    /// yazma yine sistem anahtarıyla. Boş (varsayılan) = form hiçbir kiracıda görünmez (fail-closed).
    /// </summary>
    public string[] YonetimKiracilari { get; init; } = [];

    /// <summary>Kiracı yönetim listesinde mi ve sistem uçları açık mı (anahtar kullanılabilir)?</summary>
    public bool YonetimKiracisiMi(string? kiraciId) =>
        AnahtarKullanilabilir && kiraciId is { Length: > 0 }
        && Array.Exists(YonetimKiracilari, k => string.Equals(k?.Trim(), kiraciId, StringComparison.Ordinal));

    /// <summary>Anahtar yapılandırılmış ve yer tutucu değil mi? Değilse uçlar KAPALI.</summary>
    public bool AnahtarKullanilabilir =>
        !string.IsNullOrWhiteSpace(Anahtari)
        && Anahtari.Length >= AsgariAnahtarUzunlugu
        && !Anahtari.StartsWith(YerTutucuOneki, StringComparison.Ordinal);
}

/// <summary>
/// <c>/api/v1/sistem/*</c> uçlarının TEK kapısı. JWT ile geçilemez: SAHIP
/// rolündeki geçerli bir access token bu uçları AÇMAZ.
/// </summary>
public sealed class SistemAnahtariKapisi(SistemAyarlari ayarlar) : IAsyncAuthorizationFilter
{
    /// <summary>Anahtarın taşındığı başlık.</summary>
    public const string Baslik = "X-Sistem-Anahtari";

    /// <summary>Mutasyonlarda zorunlu operatör kimliği (K3) — ≥3 karakter.</summary>
    public const string OperatorBasligi = "X-Sistem-Operator";

    public const int OperatorAsgariUzunluk = 3;

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN tur 2 — Türkçe harfli operatör adı. HTTP başlığı yalnız ASCII taşır
    /// (tarayıcı "Tasarım Hakemi" gibi bir değeri reddeder); istemci adı RFC 5987 biçiminde
    /// <c>UTF-8''&lt;yüzde-kodlu&gt;</c> gönderir. Önek yoksa değer olduğu gibi okunur (curl/betik).
    /// Bozuk yüzde dizisi olduğu gibi bırakılır (istek reddedilmez; denetim izine o metin yazılır).
    /// </summary>
    public const string OperatorKodOneki = "UTF-8''";

    public static string OperatorCoz(string? ham)
    {
        var d = (ham ?? "").Trim();
        if (!d.StartsWith(OperatorKodOneki, StringComparison.OrdinalIgnoreCase)) return d;
        try { return Uri.UnescapeDataString(d[OperatorKodOneki.Length..]).Trim(); }
        catch (UriFormatException) { return d; }
    }

    /// <summary>İstemci tarafı: adı başlığa güvenle konacak biçime çevirir.</summary>
    public static string OperatorKodla(string ad) => OperatorKodOneki + Uri.EscapeDataString(ad.Trim());

    public Task OnAuthorizationAsync(AuthorizationFilterContext baglam)
    {
        if (!ayarlar.AnahtarKullanilabilir)
        {
            // 503: "yanlış anahtar" değil, "bu kurulumda uç kapalı".
            baglam.Result = Cevap(baglam, StatusCodes.Status503ServiceUnavailable,
                "Sistem yönetimi uçları bu kurulumda kapalıdır (Sistem:Anahtari yapılandırılmamış).");
            return Task.CompletedTask;
        }

        var gelen = baglam.HttpContext.Request.Headers[Baslik].ToString();
        if (!Esit(gelen, ayarlar.Anahtari))
        {
            // Jeton var/yok ayrımı sızdırılmaz — tek tip mesaj.
            baglam.Result = Cevap(baglam, StatusCodes.Status403Forbidden,
                "Sistem yönetimi anahtarı geçersiz.");
            return Task.CompletedTask;
        }

        // K3 — "kim çağırdı" izi: mutasyon fiillerinde operatör başlığı ZORUNLU.
        if (MutasyonMu(baglam.HttpContext.Request.Method))
        {
            var op = OperatorCoz(baglam.HttpContext.Request.Headers[OperatorBasligi].ToString());
            if (op.Length < OperatorAsgariUzunluk)
                baglam.Result = Cevap(baglam, StatusCodes.Status400BadRequest,
                    $"{OperatorBasligi} başlığı zorunludur (en az {OperatorAsgariUzunluk} karakter): "
                    + "sistem yönetimi işlemi kim tarafından yapıldığı denetim izine yazılır.");
        }

        return Task.CompletedTask;
    }

    public static bool MutasyonMu(string fiil) =>
        fiil is "POST" or "PUT" or "PATCH" or "DELETE";

    /// <summary>
    /// SABİT ZAMANLI karşılaştırma. '==' zamanlama yan kanalı açar; uzunluk
    /// farkı zaten erken döner ama içerik karşılaştırması sabit zamanlıdır.
    /// </summary>
    private static bool Esit(string gelen, string beklenen)
    {
        var a = Encoding.UTF8.GetBytes(gelen);
        var b = Encoding.UTF8.GetBytes(beklenen);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static ObjectResult Cevap(AuthorizationFilterContext baglam, int kod, string mesaj) =>
        new(new
        {
            message = mesaj,
            error = kod switch
            {
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status400BadRequest => "Bad Request",
                _ => "Service Unavailable",
            },
            statusCode = kod,
        })
        {
            StatusCode = kod,
        };
}
