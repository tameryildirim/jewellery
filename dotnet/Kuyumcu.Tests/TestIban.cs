using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — TEST IBAN ÜRETİCİSİ (envanter D2: 20 dosyada "TR" + rastgele
// 24 rakam vardı; mod-97 yalnız 1/97 olasılıkla tutuyordu → fikstür GEÇERSİZ veriyi
// "doğru" diye mühürlüyordu). Artık her test IBAN'ı kontrol haneleri DOĞRU hesaplanmış,
// TCMB 2008/6 md.4 yapısında (TR + 2 kontrol + 5 banka + 1 rezerv + 16 hesap) üretilir.
// Kural Core'daki IbanDogrulayici'dir (ikinci kopya yok).
// ═════════════════════════════════════════════════════════════════════════════

public static class TestIban
{
    /// <summary>Rastgele 22 haneli BBAN + doğru kontrol haneleri.</summary>
    public static string Uret()
    {
        var r = Random.Shared;
        var b = new char[22];
        for (var i = 0; i < 22; i++) b[i] = (char)('0' + r.Next(10));
        return IbanDogrulayici.TrTamamla(new string(b));
    }

    /// <summary>Tohumdan BELİRLENİMLİ IBAN (aynı tohum → aynı IBAN; kiracıya özgü fikstürler için).</summary>
    public static string Uret(string tohum)
    {
        var h = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tohum));
        var b = new char[22];
        for (var i = 0; i < 22; i++) b[i] = (char)('0' + h[i] % 10);
        return IbanDogrulayici.TrTamamla(new string(b));
    }

    /// <summary>Verilen 22 karakterlik BBAN'ı doğru kontrol haneleriyle tamamlar.</summary>
    public static string Tamamla(string bban22) => IbanDogrulayici.TrTamamla(bban22);
}
