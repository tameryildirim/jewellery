namespace Kuyumcu.Core.Domain;

/// <summary>
/// PAKET-06 Faz B (20.10) — WhatsApp derin bağlantısı (wa.me). Dış kanal YOK: program mesaj GÖNDERMEZ; yalnız
/// kuyumcunun telefonunda/bilgisayarında WhatsApp'ı doğru numara ve hazır metinle açar. Numara Türkiye biçimine
/// (90 + 10 hane) çevrilir; çevrilemeyen numara için bağlantı üretilmez (uydurma yok).
/// </summary>
public static class WhatsAppBaglantisi
{
    public static string? Adres(string? telefon, string? metin = null)
    {
        var rakam = new string((telefon ?? "").Where(char.IsDigit).ToArray());
        if (rakam.StartsWith("0090")) rakam = rakam[4..];
        else if (rakam.StartsWith("90") && rakam.Length == 12) rakam = rakam[2..];
        else if (rakam.StartsWith('0') && rakam.Length == 11) rakam = rakam[1..];
        if (rakam.Length != 10 || rakam[0] != '5') return null;   // yalnız cep numarası (5xx)
        var adres = "https://wa.me/90" + rakam;
        return string.IsNullOrWhiteSpace(metin) ? adres : adres + "?text=" + Uri.EscapeDataString(metin.Trim());
    }
}
