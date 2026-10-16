using System.Security.Cryptography;
using System.Text;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// Alan bazlı şifreleme — SIRADAKI-ADIM-KARAR-2026-09-11.md §2 "Şifreleme kararı".
//
// Mehmet md.2: TCKN/belge no veritabanında DÜZ METİN olmaz. Bu, ilk gerçek veri
// yazılmadan ÖNCE kuruldu — sonradan eklemek düz metin yedek/binlog izi bırakır
// ve append-only tablolarda göç gerektirirdi (Bertan). Bugün 0 satır var: göç yok.
//
// Mühürlenen tasarım:
//   • AES-256-GCM, RASTGELE nonce → aynı TCKN her kayıtta FARKLI şifreli metin.
//     Deterministik değil; şifreli sütun ARANMAZ.
//   • Eşitlik araması için ayrı HMAC-SHA256(anahtar, normalize(değer)) sütunu.
//   • Çıktı biçimi: "v{sürüm}:" + base64(nonce ‖ şifreli ‖ etiket). Sürüm, anahtar
//     rotasyonunu (Faz B) göçsüz mümkün kılar.
//   • Anahtar appsettings'e KONMAZ: ortam değişkeni KUYUMCU_SIFRELEME_ANAHTARI
//     (base64, 32 bayt). Yoksa uygulama AÇILMAZ — fail-closed.
// ═════════════════════════════════════════════════════════════════════════════

public interface IAlanSifreleyici
{
    /// <summary>Aktif anahtar sürümü — kayda yazılır, rotasyonda okumayı yönlendirir.</summary>
    byte Surum { get; }

    string Sifrele(string duzMetin);
    string Coz(string sifreli);

    /// <summary>Eşitlik araması için deterministik parmak izi (HMAC). Şifreli değil, tersine çevrilemez.</summary>
    string Hashle(string duzMetin);
}

public sealed class AesGcmAlanSifreleyici : IAlanSifreleyici
{
    public const int AnahtarBayt = 32;
    private const int NonceBayt = 12;
    private const int EtiketBayt = 16;

    private readonly byte[] anahtar;
    private readonly byte[] hmacAnahtari;

    public byte Surum { get; }

    public AesGcmAlanSifreleyici(byte[] anahtar, byte surum = 1)
    {
        if (anahtar.Length != AnahtarBayt)
            throw new ArgumentException($"Şifreleme anahtarı {AnahtarBayt} bayt olmalı (verilen {anahtar.Length}).");
        this.anahtar = anahtar;
        // HMAC anahtarı, şifreleme anahtarından türetilir; iki amaç için aynı ham anahtar kullanılmaz.
        hmacAnahtari = HMACSHA256.HashData(anahtar, "kuyumcu-arama-hmac"u8.ToArray());
        Surum = surum;
    }

    public string Sifrele(string duzMetin)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceBayt);
        var duz = Encoding.UTF8.GetBytes(duzMetin);
        var sifreli = new byte[duz.Length];
        var etiket = new byte[EtiketBayt];

        using var aes = new AesGcm(anahtar, EtiketBayt);
        aes.Encrypt(nonce, duz, sifreli, etiket);

        var paket = new byte[NonceBayt + sifreli.Length + EtiketBayt];
        nonce.CopyTo(paket, 0);
        sifreli.CopyTo(paket, NonceBayt);
        etiket.CopyTo(paket, NonceBayt + sifreli.Length);

        return $"v{Surum}:{Convert.ToBase64String(paket)}";
    }

    public string Coz(string sifreli)
    {
        var ayrac = sifreli.IndexOf(':');
        if (ayrac < 2 || sifreli[0] != 'v')
            throw new CryptographicException("Şifreli alan biçimi tanınmadı (sürüm öneki yok).");

        var surum = byte.Parse(sifreli.AsSpan(1, ayrac - 1));
        if (surum != Surum)
            throw new CryptographicException($"Şifreli alan sürümü {surum}, aktif anahtar sürümü {Surum} — rotasyon gerekiyor.");

        var paket = Convert.FromBase64String(sifreli[(ayrac + 1)..]);
        if (paket.Length < NonceBayt + EtiketBayt)
            throw new CryptographicException("Şifreli alan kısa — bozuk veri.");

        var nonce = paket.AsSpan(0, NonceBayt);
        var govde = paket.AsSpan(NonceBayt, paket.Length - NonceBayt - EtiketBayt);
        var etiket = paket.AsSpan(paket.Length - EtiketBayt, EtiketBayt);
        var duz = new byte[govde.Length];

        using var aes = new AesGcm(anahtar, EtiketBayt);
        aes.Decrypt(nonce, govde, etiket, duz);   // etiket tutmazsa CryptographicException — oynanmış veri
        return Encoding.UTF8.GetString(duz);
    }

    public string Hashle(string duzMetin) =>
        Convert.ToHexString(HMACSHA256.HashData(hmacAnahtari, Encoding.UTF8.GetBytes(Normalize(duzMetin))));

    /// <summary>"123 456 78901" ile "12345678901" aynı kişidir; büyük/küçük ve boşluk farkı yok.</summary>
    public static string Normalize(string s) =>
        new(s.Where(c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray());
}

public static class AlanSifreleme
{
    public const string OrtamDegiskeni = "KUYUMCU_SIFRELEME_ANAHTARI";
    public const string YapilandirmaAnahtari = "Sifreleme:Anahtar";

    /// <summary>
    /// Anahtarı yükler. Sıra: ortam değişkeni → yapılandırma (yalnız geliştirme için
    /// düşünülmüş). İkisi de yoksa ya da 32 bayt değilse UYGULAMA AÇILMAZ.
    /// </summary>
    public static IAlanSifreleyici Yukle(IConfiguration yapilandirma)
    {
        var b64 = Environment.GetEnvironmentVariable(OrtamDegiskeni);
        var kaynak = OrtamDegiskeni;
        if (string.IsNullOrWhiteSpace(b64))
        {
            b64 = yapilandirma[YapilandirmaAnahtari];
            kaynak = YapilandirmaAnahtari;
        }

        if (string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException(
                $"Şifreleme anahtarı yok. {OrtamDegiskeni} ortam değişkenini (base64, 32 bayt) tanımlayın. " +
                "MASAK kimlik verisi şifresiz yazılamaz — uygulama açılmıyor (fail-closed).");

        byte[] anahtar;
        try { anahtar = Convert.FromBase64String(b64); }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{kaynak} geçerli base64 değil.");
        }

        if (anahtar.Length != AesGcmAlanSifreleyici.AnahtarBayt)
            throw new InvalidOperationException(
                $"{kaynak} {AesGcmAlanSifreleyici.AnahtarBayt} bayt olmalı, {anahtar.Length} bayt.");

        return new AesGcmAlanSifreleyici(anahtar);
    }

    /// <summary>Yeni rastgele anahtar (base64) — kurulum runbook'u için.</summary>
    public static string AnahtarUret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(AesGcmAlanSifreleyici.AnahtarBayt));
}
