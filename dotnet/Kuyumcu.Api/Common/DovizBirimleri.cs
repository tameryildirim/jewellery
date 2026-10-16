using System.Linq.Expressions;
using System.Reflection;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-YASLANDIRMA (madde 2) — DÖVİZ BAKİYE KOLONLARI TEK KAYNAK.
//
// YASAL-3 açık kalanı: CariModulu (pasife alma ön koşulu) ve RaporModulu
// (yaşlandırma süzgeci + dovizBakiyesiVar) döviz bakiyesini USD/EUR/GBP diye
// ELLE sayıyordu. Yeni bir birim kolonu eklendiğinde bu satırlar onu sessizce
// kaçırırdı (rapor "bakiyesi yok" der, pasife alma uyarısı çıkmazdı).
//
// Kalıp YASAL-3'ünkü: liste ELLE TUTULMAZ, CariBakiye'deki "…Bakiye" kolonlarından
// TÜRER (YaptirimServisi.BakiyeKolonlari). Döviz kolonu = TL ve has DIŞINDAKİ her
// bakiye kolonu. Para birimi kodu kolon adından gelir ("GbpBakiye" → "GBP").
// Tutarlılık testi (RaporYaslandirmaTestleri) bu kod kümesinin DovizBakiye.ParaBirimleri
// ile AYNI olduğunu doğrular: kolon eklenip yazıcıya eklenmezse (ya da tersi) KIRMIZI.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Cari bakiye satırındaki döviz kolonlarının tek kaynağı.</summary>
public static class DovizBirimleri
{
    /// <summary>
    /// Döviz bakiye kolonları — <see cref="Modules.Masak.YaptirimServisi.BakiyeKolonlari"/>
    /// içinden TL ve has gram DIŞINDA kalanlar. Elle yazılmış para birimi listesi YOKTUR.
    /// </summary>
    public static readonly PropertyInfo[] Kolonlar =
        Modules.Masak.YaptirimServisi.BakiyeKolonlari
            .Where(p => p.Name is not (nameof(CariBakiye.TlBakiye) or nameof(CariBakiye.HasGramBakiye)))
            .ToArray();

    /// <summary>Kolonun para birimi kodu: "UsdBakiye" → "USD".</summary>
    public static string Kod(PropertyInfo kolon) =>
        kolon.Name[..^"Bakiye".Length].ToUpperInvariant();

    /// <summary>Kolonlardan türeyen para birimi kodları (sıra: kolon sırası).</summary>
    public static readonly string[] Kodlar = Kolonlar.Select(Kod).ToArray();

    /// <summary>Satırda sıfırdan farklı herhangi bir döviz bakiyesi var mı.</summary>
    public static bool BakiyeVar(CariBakiye? b) =>
        b is not null && Kolonlar.Any(p => (decimal)p.GetValue(b)! != 0m);

    /// <summary>Sıfır OLMAYAN döviz bakiyeleri — kod → tutar (kolon sırasıyla).</summary>
    public static Dictionary<string, decimal> Bakiyeler(CariBakiye b)
    {
        var sonuc = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var p in Kolonlar)
        {
            var v = (decimal)p.GetValue(b)!;
            if (v != 0m) sonuc[Kod(p)] = v;
        }
        return sonuc;
    }

    /// <summary>
    /// SQL'e çevrilen süzgeç parçası: <paramref name="bakiye"/> ifadesinin HER döviz
    /// kolonu için <c>kolon &gt; esik || kolon &lt; -esik</c>, VEYA ile bağlı.
    /// Kolon yoksa <c>false</c> sabiti döner.
    /// </summary>
    public static Expression AcikIfadesi(Expression bakiye, decimal esik)
    {
        Expression? govde = null;
        var arti = Expression.Constant(esik);
        var eksi = Expression.Constant(-esik);
        foreach (var p in Kolonlar)
        {
            var kolon = Expression.Property(bakiye, p);
            var parca = Expression.OrElse(
                Expression.GreaterThan(kolon, arti),
                Expression.LessThan(kolon, eksi));
            govde = govde is null ? parca : Expression.OrElse(govde, parca);
        }
        return govde ?? Expression.Constant(false);
    }

    /// <summary>
    /// Bir kolonun SQL toplamı için seçici: <c>g =&gt; (decimal?)g.Bakiye.&lt;Kolon&gt;</c>.
    /// </summary>
    public static Expression<Func<T, decimal?>> ToplamSecici<T>(
        Expression<Func<T, CariBakiye>> bakiye, PropertyInfo kolon)
    {
        var govde = Expression.Convert(Expression.Property(bakiye.Body, kolon), typeof(decimal?));
        return Expression.Lambda<Func<T, decimal?>>(govde, bakiye.Parameters);
    }
}
