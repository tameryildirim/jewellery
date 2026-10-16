using System.Globalization;
using System.Text.RegularExpressions;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET TOPLU-1 (keşif P5 01.10, YÜKSEK): sunucunun nakit kasa kapısı mesajı
/// (<c>KasaBakiyeKapisi.Mesaj</c>: "'Ana Kasa' kasasının bakiyesi yetersiz: 188407.25 TRY var,
/// 219840.00 TRY isteniyor. …") hata bandına OLDUĞU GİBİ basılıyordu — makine sayısı + ISO kodu
/// (ölçer K5 'TRY' kalıbı). Ekran sayıları ayrıştırıp sözlükteki cümleyle tr-TR biçiminde yazar
/// (Gider'deki kuralın aynısı, tek yerde). Kalıp tutmazsa null — çağıran kendi yoluna döner (hata yutulmaz).
/// <para>
/// PARA-DEMO-3 (tasarım hakemi KRİTİK, 02.10): sunucu artık toplam kapısı cümlesini tr-TR + para
/// işaretiyle yazar ("312.547,91 ₺ var"). Bu sınıf İKİ biçimi de okur: yeni tr-TR biçimi (sayı tr-TR,
/// ardından bölünmez boşluk + işaret) ve geçiş için eski invariant biçimi ("312547.91 TRY"). Ayrıca
/// nakit payı kapısının cümlesini de ("… kasasında nakit yetersiz: çekmecede …") sözlükten (TR+EN)
/// yeniden yazar — iki kasa reddi aynı dil ve aynı biçimle görünür. Kasa adındaki kesme işareti
/// ("Merkez'deki Kasa") kalıbı bozmaz.
/// </para>
/// </summary>
public static class KasaBakiyeMesaji
{
    /// <summary>tr-TR tutar: isteğe bağlı eksi (U+2212 ya da '-'), binlik nokta, iki hane kuruş.</summary>
    private const string TrSayi = @"[\u2212-]?\d{1,3}(?:\.\d{3})*,\d+";

    // Eski (invariant) biçim — geçiş uyumu.
    private static readonly Regex EskiKalip = new(
        @"^'(?<ad>.+?)' kasasının bakiyesi yetersiz: (?<b>-?\d+(?:\.\d+)?) (?<pb>\S+) var, (?<i>-?\d+(?:\.\d+)?) \S+ isteniyor",
        RegexOptions.CultureInvariant);

    // Yeni (tr-TR) biçim: sayı + U+00A0 + işaret.
    private static readonly Regex YeniKalip = new(
        @"^'(?<ad>.+?)' kasasının bakiyesi yetersiz: (?<b>" + TrSayi + @")\u00a0(?<pb>\S+) var, (?<i>" + TrSayi + @")\u00a0\S+ isteniyor",
        RegexOptions.CultureInvariant);

    // Nakit payı kapısı (KasaBakiyeKapisi.NakitMesaji).
    private static readonly Regex NakitKalibi = new(
        @"^'(?<ad>.+?)' kasasında nakit yetersiz: çekmecede (?<n>" + TrSayi + @")\u00a0(?<pb>\S+) nakit var, (?<i>" + TrSayi
        + @")\u00a0\S+ nakit isteniyor\. Kasadaki kart \((?<k>" + TrSayi + @")\u00a0\S+\) ve havale \((?<h>" + TrSayi + @")\u00a0\S+\)",
        RegexOptions.CultureInvariant);

    // Araç payı kapısı (KasaBakiyeKapisi.AracMesaji — elle kart/havale çıkışı). PARA-DEMO-3 tur 2 (doğrulayıcı
    // DÜŞÜK): bu kalıp tanınmıyordu, İngilizce arayüzde Türkçe görünüyordu.
    private static readonly Regex AracKalibi = new(
        @"^'(?<ad>.+?)' kasasındaki (?<arac>kart|havale) tahsilatı (?<p>" + TrSayi + @")\u00a0(?<pb>\S+); (?<i>" + TrSayi
        + @")\u00a0\S+ (?:kartla|havaleyle) çıkarılamaz\.",
        RegexOptions.CultureInvariant);

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// PARA-DEMO-3 tur 2 (doğrulayıcı DÜŞÜK): NAKİT çıkışta toplam kapısı cümlesi ("kasada 221.726,43 ₺ var")
    /// kart/havale payını da "var" sayıyordu; kullanıcı o tutara güvenip ikinci bir nakit reddi alıyordu.
    /// Ekran kasanın nakit ayrımını (<c>/kasalar/nakit-ayrimi</c>) biliyorsa ve kasada kart/havale payı varsa,
    /// toplam reddi NAKİT cümlesiyle yazılır (çekmecedeki nakit + kart + havale ayrı ayrı). Nakit çıkışta
    /// tutar toplamı aşıyorsa nakdi de aşar (kart/havale payı ≥ 0) — söylenen sebep doğrudur. Ayrım yoksa
    /// ya da kasa yalnız nakit tutuyorsa <see cref="Bicimle(string?, DilServisi)"/> aynen.
    /// </summary>
    public static string? Bicimle(string? sunucuMesaji, DilServisi dil, Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti? nakitAyrimi)
    {
        if (nakitAyrimi is { } a && a.Kart + a.Havale > 0m && Ayristir(sunucuMesaji) is { } t)
            return NakitCumlesi(dil, t.Ad, a.Nakit, t.Istenen, a.Kart, a.Havale, t.ParaBirimi);
        return Bicimle(sunucuMesaji, dil);
    }

    /// <summary>Nakit payı cümlesi (sözlükten, TR+EN) — sunucu reddi ve istemci erken uyarısı AYNI cümleyi kullanır.</summary>
    public static string NakitCumlesi(DilServisi dil, string kasaAdi, decimal nakit, decimal istenen, decimal kart, decimal havale, string paraBirimi) =>
        string.Format(Kuyumcu.Core.I18n.ParaDemo3Sozlugu.Al(dil.Dil).NakitYetersiz, kasaAdi,
            Bicim.Para(Math.Max(0m, nakit), paraBirimi), Bicim.Para(istenen, paraBirimi),
            Bicim.Para(kart, paraBirimi), Bicim.Para(havale, paraBirimi));

    /// <summary>Kalıp tutarsa biçimli cümle; tutmazsa null.</summary>
    public static string? Bicimle(string? sunucuMesaji, DilServisi dil)
    {
        if (string.IsNullOrEmpty(sunucuMesaji)) return null;
        if (Ayristir(sunucuMesaji) is { } t)
            return string.Format(dil.T.Gider.YetersizBakiye, t.Ad, Bicim.Para(t.Bakiye, t.ParaBirimi), Bicim.Para(t.Istenen, t.ParaBirimi));
        var n = NakitKalibi.Match(sunucuMesaji);
        if (n.Success && TrOku(n.Groups["n"].Value) is { } nakit && TrOku(n.Groups["i"].Value) is { } ist
            && TrOku(n.Groups["k"].Value) is { } kart && TrOku(n.Groups["h"].Value) is { } havale)
        {
            var pb = KodaCevir(n.Groups["pb"].Value);
            return string.Format(Kuyumcu.Core.I18n.ParaDemo3Sozlugu.Al(dil.Dil).NakitYetersiz, n.Groups["ad"].Value,
                Bicim.Para(nakit, pb), Bicim.Para(ist, pb), Bicim.Para(kart, pb), Bicim.Para(havale, pb));
        }
        var r = AracKalibi.Match(sunucuMesaji);
        if (r.Success && TrOku(r.Groups["p"].Value) is { } pay && TrOku(r.Groups["i"].Value) is { } istenen)
        {
            var pb = KodaCevir(r.Groups["pb"].Value);
            var p = Kuyumcu.Core.I18n.ParaDemo3Sozlugu.Al(dil.Dil);
            return string.Format(p.AracPayiYetersiz, r.Groups["ad"].Value,
                r.Groups["arac"].Value == "kart" ? p.AracKart : p.AracHavale, Bicim.Para(pay, pb), Bicim.Para(istenen, pb));
        }
        return null;
    }

    /// <summary>Toplam kapısı reddinin parçaları (iki biçim); kalıp dışı → null.</summary>
    public static (string Ad, decimal Bakiye, decimal Istenen, string ParaBirimi)? Ayristir(string? sunucuMesaji)
    {
        if (string.IsNullOrEmpty(sunucuMesaji)) return null;
        var y = YeniKalip.Match(sunucuMesaji);
        if (y.Success && TrOku(y.Groups["b"].Value) is { } yb && TrOku(y.Groups["i"].Value) is { } yi)
            return (y.Groups["ad"].Value, yb, yi, KodaCevir(y.Groups["pb"].Value));
        var m = EskiKalip.Match(sunucuMesaji);
        var inv = CultureInfo.InvariantCulture;
        if (m.Success
            && decimal.TryParse(m.Groups["b"].Value, NumberStyles.Number, inv, out var b)
            && decimal.TryParse(m.Groups["i"].Value, NumberStyles.Number, inv, out var i))
            return (m.Groups["ad"].Value, b, i, m.Groups["pb"].Value);
        return null;
    }

    private static decimal? TrOku(string s) =>
        decimal.TryParse(s.Replace('−', '-'), NumberStyles.Number, Tr, out var d) ? d : null;

    /// <summary>Sunucunun yazdığı işaret → ISO kodu (Bicim.Para kod bekler); tanınmayan aynen.</summary>
    private static string KodaCevir(string isaret) => isaret switch
    {
        "₺" => "TRY",
        "$" => "USD",
        "€" => "EUR",
        _ => isaret,
    };
}
