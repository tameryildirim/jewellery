using System.Globalization;
using System.Text;

namespace Kuyumcu.Core.Domain;

/// <summary>
/// D1-02 (18.10) — işletmenin etiket şablonu: boyut (hazır boylar) + alanların sırası ve görünürlüğü.
/// Saklama biçimi tek satır: "50x30|AD,IBARE,MADEN,SATIR,BARKOD,KOD" (kiraci_ayarlari.deger ≤ 200).
/// Taş kökeni ibaresi (md.11/e) şablondan ÇIKARILAMAZ — çözümleme onu her zaman ekler.
/// </summary>
public sealed record EtiketSablonu(int GenislikMm, int YukseklikMm, IReadOnlyList<string> Alanlar)
{
    public const string Ad = "AD", Ibare = "IBARE", Maden = "MADEN", Satir = "SATIR", Barkod = "BARKOD", Qr = "QR", Kod = "KOD", Fiyat = "FIYAT";
    public static readonly IReadOnlyList<string> TumAlanlar = [Ad, Ibare, Maden, Satir, Barkod, Qr, Kod, Fiyat];
    public static readonly IReadOnlyList<(int G, int Y)> Boylar = [(50, 30), (58, 40), (40, 25), (35, 22)];
    public static readonly EtiketSablonu Varsayilan = new(50, 30, [Ad, Ibare, Maden, Satir, Barkod, Kod]);

    public string Yaz() => FormattableString.Invariant($"{GenislikMm}x{YukseklikMm}|{string.Join(',', Alanlar)}");

    /// <summary>Kayıtlı metni çözer; bozuk/eksikse varsayılan. Bilinmeyen alan atılır, tekrar atılır, ibare eklenir.</summary>
    public static EtiketSablonu Coz(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin)) return Varsayilan;
        var parca = metin.Split('|');
        var boy = parca[0].Split('x');
        if (parca.Length != 2 || boy.Length != 2
            || !int.TryParse(boy[0], NumberStyles.None, CultureInfo.InvariantCulture, out var g)
            || !int.TryParse(boy[1], NumberStyles.None, CultureInfo.InvariantCulture, out var y)
            || !Boylar.Contains((g, y))) return Varsayilan;
        return Normalle(new EtiketSablonu(g, y, parca[1].Split(',', StringSplitOptions.RemoveEmptyEntries)));
    }

    public static EtiketSablonu Normalle(EtiketSablonu s)
    {
        var alanlar = s.Alanlar.Where(a => TumAlanlar.Contains(a)).Distinct().ToList();
        if (!alanlar.Contains(Ibare)) alanlar.Insert(Math.Min(1, alanlar.Count), Ibare);
        return s with { Alanlar = alanlar };
    }
}

/// <summary>D1-02 — bir etiketin basılacak değerleri (ekranla ZPL aynı veriden).</summary>
public sealed record EtiketVerisi(string Ad, string? Ibare, string Maden, string Satir, string? Barkod, string Kod, string? Fiyat);

/// <summary>
/// D1-02 — Zebra (ZPL II) çıktısı: 203 dpi (8 nokta/mm), UTF-8 (^CI28). Alanlar şablon sırasıyla alt alta; Code128
/// (^BC) ve QR (^BQ, M düzeyi). Dosya yazıcıya Zebra Setup Utilities ya da ağ yazıcısının 9100 kapısıyla gönderilir.
/// </summary>
public static class EtiketZpl
{
    private const int NoktaMm = 8;

    public static string Uret(EtiketSablonu s, IEnumerable<EtiketVerisi> etiketler)
    {
        var sb = new StringBuilder();
        foreach (var e in etiketler)
        {
            var w = s.GenislikMm * NoktaMm; var h = s.YukseklikMm * NoktaMm;
            sb.Append("^XA^CI28").Append(FormattableString.Invariant($"^PW{w}^LL{h}^LH0,0"));
            var y = 2 * NoktaMm; var x = 2 * NoktaMm;
            var satir = Math.Max(18, h / 12);
            foreach (var a in s.Alanlar)
            {
                switch (a)
                {
                    case EtiketSablonu.Ad: Metin(sb, x, ref y, e.Ad, satir + 4); break;
                    case EtiketSablonu.Ibare when !string.IsNullOrWhiteSpace(e.Ibare): Metin(sb, x, ref y, e.Ibare!, satir); break;
                    case EtiketSablonu.Maden: Metin(sb, x, ref y, e.Maden, satir); break;
                    case EtiketSablonu.Satir: Metin(sb, x, ref y, e.Satir, satir); break;
                    case EtiketSablonu.Kod: Metin(sb, x, ref y, e.Kod, satir); break;
                    case EtiketSablonu.Fiyat when !string.IsNullOrWhiteSpace(e.Fiyat): Metin(sb, x, ref y, e.Fiyat!, satir + 4); break;
                    case EtiketSablonu.Barkod when !string.IsNullOrWhiteSpace(e.Barkod):
                        var bh = Math.Max(30, h / 6);
                        sb.Append(FormattableString.Invariant($"^FO{x},{y}^BY1^BCN,{bh},Y,N,N^FD{Temiz(e.Barkod!)}^FS"));
                        y += bh + satir + 4; break;
                    case EtiketSablonu.Qr:
                        var veri = string.IsNullOrWhiteSpace(e.Barkod) ? e.Kod : e.Barkod!;
                        sb.Append(FormattableString.Invariant($"^FO{w - 14 * NoktaMm},{2 * NoktaMm}^BQN,2,3^FDMA,{Temiz(veri)}^FS"));
                        break;
                }
            }
            sb.Append("^XZ\n");
        }
        return sb.ToString();
    }

    private static void Metin(StringBuilder sb, int x, ref int y, string metin, int yukseklik)
    {
        sb.Append(FormattableString.Invariant($"^FO{x},{y}^A0N,{yukseklik},{yukseklik}^FD{Temiz(metin)}^FS"));
        y += yukseklik + 4;
    }

    /// <summary>ZPL denetim karakterleri (^ ~) alan verisinde komut sayılır — boşlukla değiştirilir.</summary>
    private static string Temiz(string s) => s.Replace('^', ' ').Replace('~', ' ').Replace('\n', ' ');
}
