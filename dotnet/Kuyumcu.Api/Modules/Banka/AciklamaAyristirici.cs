using System.Text.RegularExpressions;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// M8 kalanı (20.10) — banka AÇIKLAMASINDAN müşteri ipucu. SAF SINIF.
// Bankaların çoğu gönderenin adını ve bazen TC/VKN'sini açıklamaya yazar
// ("Gönd: AYŞE DEMİR 0157-Enpara", "AYŞE DEMİR 12345678901 HAVALE", "GÖNDEREN: …").
//   • Kimlik: açıklamadaki 10/11 haneli sayılar KimlikNo kuralından geçerse kimliktir (ilk geçerli).
//     Geçersiz sayı (dekont no, IBAN parçası) kimlik SAYILMAZ.
//   • Ad: yalnız bir gönderen etiketinden ("GÖND:", "GÖNDEREN:", "ADINA:") sonraki ad; etiket yoksa ad çıkarılmaz
//     (açıklamadaki her kelime ad değildir — "HAVALE", "EFT", şube kodu…).
// K4-OK-5 korunur: buradan çıkan ad yalnız ÖNERİ üretir; kimlik ise mevcut kimlik yolunun aynısıdır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed record AciklamaIpucu(string? KimlikNo, string? Ad);

public static partial class AciklamaAyristirici
{
    [GeneratedRegex(@"(?<!\d)(\d{10,11})(?!\d)")]
    private static partial Regex Sayilar();

    // Etiket → ad: "GÖND: AYŞE DEMİR 0157-Enpara" → "AYŞE DEMİR". Ad harf/boşluk/nokta; sayı ya da ayraçta biter.
    [GeneratedRegex(@"(?:G[ÖO]ND(?:EREN)?|ADINA|G[ÖO]NDERICI|SENDER)\s*[:\-]?\s*(?<ad>[\p{L}][\p{L}\s\.']{1,80}?)(?=\s*(?:\d|[-–/|,;]|$))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GonderenAdi();

    public static AciklamaIpucu Ayristir(string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(aciklama)) return new(null, null);
        string? kimlik = null;
        foreach (Match m in Sayilar().Matches(aciklama))
        {
            var no = m.Groups[1].Value;
            if (KimlikNo.Dogrula(no, KimlikNo.TurTuret(no)) is null) { kimlik = no; break; }
        }
        string? ad = null;
        if (GonderenAdi().Match(aciklama) is { Success: true } g)
        {
            var aday = string.Join(' ', g.Groups["ad"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (aday.Length >= 3 && aday.Contains(' ') && Kuyumcu.Core.Domain.Girdi.AdKurali.Unvan(aday) is null) ad = aday;
        }
        return new(kimlik, ad);
    }
}
