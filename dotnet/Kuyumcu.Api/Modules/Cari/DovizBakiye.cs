using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// DÖVİZ BAKİYE EŞLEMESİ — paket 27 Faz A.
//
// ⚠ NEDEN AYRI DOSYA (Bertan, K27): cari_bakiyeler'de üç döviz kolonu var
// (usd/eur/gbp) ve bu üç kolona DOĞRUDAN atama yapılan her yeni nokta, bir
// sonraki para biriminde UNUTULACAK bir yerdir. Atama TEK yerde toplanır;
// denetim.sh bu dosyanın DIŞINDA "UsdBakiye +=" benzeri atama saymaz.
//
// ⚠ TRY BURADA YOKTUR ve bu bilinçlidir: TRY zaten tl_bakiye'dir. TRY'yi
// "dördüncü döviz" saymak aynı borcu iki kolonda tutmak demekti.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Para birimi ↔ <see cref="CariBakiye"/> kolonu eşlemesinin TEK yeri.
/// </summary>
public static class DovizBakiye
{
    /// <summary>Kapalı küme — başka para birimi kabul EDİLMEZ (K27, fail-closed).</summary>
    public static readonly string[] ParaBirimleri = ["USD", "EUR", "GBP"];

    /// <summary>Döviz borcu doğuran hareket türü.</summary>
    public const string TurVeresiye = "DOVIZ_VERESIYE";

    /// <summary>Döviz borcu kapatan (efektif tahsilat) hareket türü.</summary>
    public const string TurTahsilat = "DOVIZ_TAHSILAT";

    /// <summary>
    /// Bu hareket türü döviz defterine mi yazıyor? <c>HareketFabrikasi</c>
    /// K27-E değişmezini bu yordamla zorlar.
    /// </summary>
    public static bool DovizTuru(string? tur) => tur is TurVeresiye or TurTahsilat;

    /// <summary>
    /// Geçerli bir döviz kodu mu? <b>TAM EŞLEŞME</b> — <c>ToUpperInvariant</c>
    /// KULLANILMAZ.
    /// <para>
    /// ⚠ Türkçe kültürde <c>"usd".ToUpper()</c> → <c>"USD"</c> olsa da
    /// <c>"ı"/"i"</c> çiftinde kültüre bağlı davranış vardır; para birimi
    /// kümesi zaten kapalı ve büyük harflidir, normalize etmek yerine
    /// REDDETMEK doğru olandır (girdinin yanlış olduğunu çağıran öğrenir).
    /// </para>
    /// </summary>
    public static bool Gecerli(string? paraBirimi) =>
        paraBirimi is not null && Array.IndexOf(ParaBirimleri, paraBirimi) >= 0;

    /// <summary>
    /// Geçerli değilse 400. TRY özel mesaj alır: kullanıcı "TL borcu yazacağım"
    /// diye buraya gelmiş olabilir; doğru yol veresiye/tahsilat ucudur.
    /// </summary>
    public static string Dogrula(string? paraBirimi)
    {
        if (paraBirimi == "TRY")
            throw new BadRequestException(
                "TRY bir döviz bakiyesi değildir — TL borcu için veresiye/tahsilat ucunu kullanın");
        if (!Gecerli(paraBirimi))
            throw new BadRequestException(
                $"Para birimi geçersiz — kabul edilen değerler: {string.Join(", ", ParaBirimleri)}");
        return paraBirimi!;
    }

    /// <summary>İlgili kolonu okur. Geçersiz para biriminde patlar (sessiz 0 dönmez).</summary>
    public static decimal Oku(CariBakiye b, string paraBirimi) => paraBirimi switch
    {
        "USD" => b.UsdBakiye,
        "EUR" => b.EurBakiye,
        "GBP" => b.GbpBakiye,
        _ => throw new BadRequestException($"Para birimi geçersiz: {paraBirimi}"),
    };

    /// <summary>
    /// İlgili kolona delta ekler — <b>yalnız hedef kolona</b>. Üç kolona
    /// doğrudan atama yapan TEK yer burasıdır (denetim.sh sayar).
    /// </summary>
    public static void Ekle(CariBakiye b, string paraBirimi, decimal delta)
    {
        switch (paraBirimi)
        {
            case "USD": b.UsdBakiye += delta; break;
            case "EUR": b.EurBakiye += delta; break;
            case "GBP": b.GbpBakiye += delta; break;
            default: throw new BadRequestException($"Para birimi geçersiz: {paraBirimi}");
        }
    }

    /// <summary>
    /// Kur fotoğrafından o para biriminin TL kurunu okur. <c>null</c> =
    /// "o para biriminin kuru bu kayıtta YOK" — çağıran fail-closed davranır.
    /// (<c>OdemeSepeti.DovizKurlari.Kur</c> ile aynı mantık; tek fark kaynağın
    /// snapshot olması.)
    /// </summary>
    public static decimal? KurAl(KurSnapshot s, string paraBirimi) => paraBirimi switch
    {
        "USD" => s.UsdTry,
        "EUR" => s.EurTry,
        "GBP" => s.GbpTry,
        _ => null,
    };

    /// <summary>
    /// Kuru fail-closed okur: kayıtta yoksa ya da ≤ 0 ise 400. "Yaklaşık kur"
    /// YOKTUR — kur bilinmeden döviz borcunun TL karşılığı yazılamaz, çünkü o
    /// değer MASAK eşiğine ve tevsik haddine giren sayıdır.
    /// </summary>
    public static decimal KurZorunlu(KurSnapshot s, string paraBirimi)
    {
        var kur = KurAl(s, paraBirimi);
        if (kur is not { } k || k <= 0m)
            throw new BadRequestException(
                $"{paraBirimi} kuru işlem anındaki kur kaydında yok — döviz işlemi yapılamaz");
        return k;
    }

    /// <summary>
    /// Efektif miktarın TL karşılığı — 2 hane, <c>AwayFromZero</c>
    /// (para yuvarlamasında proje genelindeki tek kural).
    /// </summary>
    public static decimal TlKarsiligi(decimal miktar, decimal kur) =>
        Math.Round(miktar * kur, 2, MidpointRounding.AwayFromZero);
}
