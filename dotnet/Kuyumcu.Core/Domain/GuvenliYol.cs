namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN — GÜVENLİ DÖNÜŞ YOLU (açık yönlendirme kapısı, ORTA güvenlik).
//
// SORUN (doğrulayıcı EKSIK-B1-YAZDIR tur 3, gerçek Chrome): Cikti "?donus=" yalnız ilk iki
// karakteri denetliyordu. "/\t/kotu.example.com" ve "/\n/…" geçiyordu; tarayıcının URL
// ayrıştırıcısı TAB/LF/CR'yi SİLER → "//kotu.example.com" = başka köken (Geri ile dışarı).
//
// KURAL (tek yer — her "dönüş" parametresi BUNU kullanır):
//   · "/" ile başlar, ikinci karakter "/" ya da "\" DEĞİL;
//   · hiçbir yerde kontrol karakteri (\x00–\x1F, \x7F) ve "\" yok; YOL kısmında ":" yok (şema kaçağı;
//     sorgu kısmında serbest — tur 2);
//   · URL-çözülmüş hâli de (en çok 3 kat: %252F → %2F → /) AYNI koşulu sağlar;
//   · aksi → null (çağıran kendi uygulama içi varsayılanına düşer; varsayılanı yoksa "/").
// ═════════════════════════════════════════════════════════════════════════════

public static class GuvenliYol
{
    public const string Kok = "/";
    private const int AzamiCozme = 3;

    /// <summary>Güvenli uygulama içi yol ise kendisi, değilse <c>null</c>.</summary>
    public static string? UygulamaIci(string? yol)
    {
        if (!Uygun(yol)) return null;
        var cozulmus = yol!;
        for (var i = 0; i <= AzamiCozme; i++)
        {
            string sonraki;
            try { sonraki = Uri.UnescapeDataString(cozulmus); }
            catch (UriFormatException) { return null; }
            if (sonraki == cozulmus) return yol;     // sabitlendi — tüm katlar uygun
            if (!Uygun(sonraki)) return null;
            cozulmus = sonraki;
        }
        return null;                                 // 3 kattan derin kodlama → fail-closed
    }

    /// <summary>Güvenli değilse <paramref name="varsayilan"/> (o da güvenli değilse "/").</summary>
    public static string Donus(string? yol, string varsayilan = Kok) =>
        UygulamaIci(yol) ?? UygulamaIci(varsayilan) ?? Kok;

    private static bool Uygun(string? d)
    {
        // Yalnız "/" (kök) dönüş HEDEFİ sayılmaz (çağıranın bağlamlı varsayılanı tercih edilir).
        if (d is null || d.Length < 2 || d[0] != '/') return false;
        if (d[1] == '/' || d[1] == '\\') return false;
        // tur 2 (doğrulayıcı DÜŞÜK): ":" yalnız YOL kısmında yasak (şema kaçağı orada olur);
        // sorgu kısmında ("?saat=10:00") meşrudur — ilk "?"ten sonra serbest.
        var sorguda = false;
        foreach (var ch in d)
        {
            if (ch < 0x20 || ch == 0x7F || ch == '\\') return false;
            if (ch == '?') sorguda = true;
            else if (ch == ':' && !sorguda) return false;
        }
        return true;
    }
}
