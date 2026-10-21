namespace Kuyumcu.Core.I18n;

// Görsel denetim O-18 (21.10) — denetim izi özeti okunamazsa liste yine gelir; özet satırının yerinde tek satır uyarı. TR + EN.
public static class DenetimOzetSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Okunamadi(string dil) => En(dil) ? "Summary could not be read; the list below is complete." : "Özet okunamadı; aşağıdaki liste eksiksiz.";
}
