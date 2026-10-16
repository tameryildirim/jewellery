using System.Security.Cryptography;
using System.Text;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// TOTP — RFC 6238 (HOTP: RFC 4226, Base32: RFC 4648). Paket 45 Faz A.
//
// SAF SINIF: veritabanı/HTTP bilmez. Uyum iddiası RFC 6238 Ek B vektörleriyle
// testte KANITLANIR (TotpTestleri) — "uyumlu" cümlesi vektör geçmeden yazılmaz.
//
// Dış paket EKLENMEDİ: HMAC-SHA1 + dinamik kesme ~60 satır; bir NuGet bağımlılığı
// tedarik zinciri yüzeyini büyütürdü (Bertan oyu).
// ═════════════════════════════════════════════════════════════════════════════

public static class Base32
{
    private const string Alfabe = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>RFC 4648 Base32, DOLGU YOK, büyük harf.</summary>
    public static string Kodla(byte[] veri)
    {
        var sb = new StringBuilder((veri.Length * 8 + 4) / 5);
        int tampon = 0, bit = 0;
        foreach (var b in veri)
        {
            tampon = (tampon << 8) | b;
            bit += 8;
            while (bit >= 5)
            {
                sb.Append(Alfabe[(tampon >> (bit - 5)) & 31]);
                bit -= 5;
            }
        }
        if (bit > 0) sb.Append(Alfabe[(tampon << (5 - bit)) & 31]);
        return sb.ToString();
    }

    public static byte[] Coz(string metin)
    {
        var bayt = new List<byte>(metin.Length * 5 / 8 + 1);
        int tampon = 0, bit = 0;
        foreach (var c in metin)
        {
            if (c is '=' or ' ' or '-') continue;
            var d = Alfabe.IndexOf(char.ToUpperInvariant(c));
            if (d < 0) throw new FormatException("Base32 alfabesi dışında karakter.");
            tampon = (tampon << 5) | d;
            bit += 5;
            if (bit >= 8)
            {
                bayt.Add((byte)((tampon >> (bit - 8)) & 0xFF));
                bit -= 8;
            }
        }
        return [.. bayt];
    }
}

public static class Totp
{
    /// <summary>TOTP zaman kaynağı MUTLAK UTC saniyedir — yerel gün araçları (Zaman.cs) burada kullanılmaz.</summary>
    public static long Adim(DateTime utc, int adimSaniye) =>
        (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds / adimSaniye);

    /// <summary>Verilen adım için HOTP değeri; başında sıfır varsa korunur (sıfırla doldurulur).</summary>
    public static string Uret(byte[] giz, long adim, int hane = 6)
    {
        var sayac = BitConverter.GetBytes(adim);
        if (BitConverter.IsLittleEndian) Array.Reverse(sayac);

        var hmac = HMACSHA1.HashData(giz, sayac);
        var ofset = hmac[^1] & 0x0F;
        var ikili = ((hmac[ofset] & 0x7F) << 24)
                  | ((hmac[ofset + 1] & 0xFF) << 16)
                  | ((hmac[ofset + 2] & 0xFF) << 8)
                  | (hmac[ofset + 3] & 0xFF);

        var mod = (int)Math.Pow(10, hane);
        return (ikili % mod).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(hane, '0');
    }

    /// <summary>
    /// Kodu ±<paramref name="driftAdim"/> pencerede doğrular. Karşılaştırma SABİT ZAMANLI
    /// (<see cref="CryptographicOperations.FixedTimeEquals"/>) — zamanlama sızıntısı olmasın.
    /// </summary>
    public static bool Dogrula(
        byte[] giz, string kod, DateTime an, int adimSaniye, int driftAdim, out long kullanilanAdim)
    {
        kullanilanAdim = 0;
        if (string.IsNullOrWhiteSpace(kod)) return false;
        var temiz = kod.Trim();

        var merkez = Adim(an, adimSaniye);
        for (var d = -driftAdim; d <= driftAdim; d++)
        {
            var adim = merkez + d;
            var beklenen = Uret(giz, adim, temiz.Length);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(beklenen), Encoding.UTF8.GetBytes(temiz))) continue;
            kullanilanAdim = adim;
            return true;
        }
        return false;
    }

    /// <summary>otpauth URI (Key Uri Format) — doğrulayıcı uygulamaya elle/QR ile girilir.</summary>
    public static string OtpauthUri(string yayinci, string hesap, string gizBase32, int hane, int adimSaniye) =>
        $"otpauth://totp/{Uri.EscapeDataString(yayinci)}:{Uri.EscapeDataString(hesap)}"
        + $"?secret={gizBase32}&issuer={Uri.EscapeDataString(yayinci)}"
        + $"&algorithm=SHA1&digits={hane}&period={adimSaniye}";
}
