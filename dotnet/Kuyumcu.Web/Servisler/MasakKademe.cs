using Kuyumcu.Contracts.Bilesenler.MasakFormu;

namespace Kuyumcu.Web.Servisler;

/// <summary>Kimliği hangi kural tetikledi (ekran metni için; kapı kararı SUNUCUDADIR).</summary>
public enum MasakTetik
{
    /// <summary>Eşik yok ya da sunucu kimlik istedi ama ölçüyü bildirmedi — sayı uydurulmaz.</summary>
    Bilinmiyor,
    /// <summary>Kimlik gerekmiyor.</summary>
    Yok,
    /// <summary>Bu müşterinin bugünkü bağlantılı işlem toplamı.</summary>
    Toplam,
    /// <summary>Amaç beyanı kademesi (md.5/1-b eşiğinin altında; mali müşavir teyidi bekliyor).</summary>
    Amac,
    /// <summary>MASAK md.5/1-b kimlik eşiği.</summary>
    Kimlik,
}

/// <summary>
/// PAKET YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK) — satış ekranında kimliği isteyen
/// KADEMENİN TEK seçicisi: kilit metni (Islemler), MASAK formu başlığı ve PRO "Yasal kapılar"
/// paneli aynı kuralla seçer, böylece aynı ekranda iki farklı eşik görünmez. Sayılar SUNUCUDAN
/// (/masak/esikler, /masak/gereksinim); istemciye eşik gömülmez.
/// </summary>
public static class MasakKademe
{
    /// <summary>Kararın ölçüsü: bağlantılı toplamdan geldiyse sunucunun toplamı, değilse işlem tutarı.</summary>
    public static decimal Olcu(MasakGereksinim? g, decimal tutar) =>
        g is { KimlikGerekli: true, ToplamdanMi: true } ? g.OlcuTutarTl : Math.Abs(tutar);

    public static MasakTetik Sec(Esikler? e, MasakGereksinim? g, decimal tutar)
    {
        if (e is null) return MasakTetik.Bilinmiyor;
        if (g is { KimlikGerekli: true, ToplamdanMi: true }) return MasakTetik.Toplam;
        var t = Math.Abs(tutar);
        if (t >= e.KimlikEsigi) return MasakTetik.Kimlik;
        if (AmacKademesiVar(e) && t >= e.AmacBeyaniEsigi) return MasakTetik.Amac;
        return g is { KimlikGerekli: true } ? MasakTetik.Bilinmiyor : MasakTetik.Yok;
    }

    /// <summary>
    /// Tur 3 (hasım KRİTİK): karar bağlantılı TOPLAMDAN geldiyse toplamın aştığı kademe —
    /// <see cref="Sec"/> ile AYNI kural, ölçü sunucunun toplamı: toplam ≥ kimlik eşiği → Kimlik (MASAK);
    /// amaç kademesindeyse → Amac; ikisi de değilse (eski sunucu / beklenmeyen ölçü) Bilinmiyor — sayı uydurulmaz.
    /// Toplamdan gelmeyen kararda Bilinmiyor.
    /// </summary>
    public static MasakTetik ToplamKademesi(Esikler? e, MasakGereksinim? g)
    {
        if (e is null || g is not { KimlikGerekli: true, ToplamdanMi: true }) return MasakTetik.Bilinmiyor;
        var t = Math.Abs(g.OlcuTutarTl);
        if (t >= e.KimlikEsigi) return MasakTetik.Kimlik;
        if (AmacKademesiVar(e) && t >= e.AmacBeyaniEsigi) return MasakTetik.Amac;
        return MasakTetik.Bilinmiyor;
    }

    /// <summary>Amaç beyanı kademesi kimlik eşiğinin ALTINDA ayrı bir kademe mi.</summary>
    public static bool AmacKademesiVar(Esikler e) => e.AmacBeyaniEsigi > 0m && e.AmacBeyaniEsigi < e.KimlikEsigi;
}
