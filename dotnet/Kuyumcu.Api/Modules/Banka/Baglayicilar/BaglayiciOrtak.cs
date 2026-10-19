using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Kuyumcu.Api.Common;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Api.Modules.Banka.Baglayicilar;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b — bağlayıcıların ortak araçları. Kaynak: ARASTIRMA-BANKA-API-TEKNIK-2026-10-19.md § 12:
//   • XML yerel ada göre, HARF DUYARSIZ okunur (bankalar arası yazım farkı; tek kayıtta dizi yerine tek eleman).
//   • Ondalık ayırıcı bankaya göre virgül ya da nokta → ikisi de okunur.
//   • Banka saatleri Türkiye saatidir → UTC'ye çevrilir.
//   • Uzun aralık bankanın sınırına göre dilimlenir (varsayılan 1 gün).
// ═════════════════════════════════════════════════════════════════════════════

internal static class Xml
{
    public static IEnumerable<XElement> Hepsi(XContainer k, string ad) =>
        k.Descendants().Where(e => string.Equals(e.Name.LocalName, ad, StringComparison.OrdinalIgnoreCase));

    public static XElement? Ilk(XContainer k, string ad) => Hepsi(k, ad).FirstOrDefault();

    /// <summary>Doğrudan çocuk alanın metni (yoksa / boşsa null).</summary>
    public static string? Al(XElement e, params string[] adlar)
    {
        foreach (var ad in adlar)
        {
            var c = e.Elements().FirstOrDefault(x => string.Equals(x.Name.LocalName, ad, StringComparison.OrdinalIgnoreCase));
            if (c is not null && !c.HasElements && !string.IsNullOrWhiteSpace(c.Value)) return c.Value.Trim();
        }
        return null;
    }

    public static XDocument Oku(string metin)
    {
        try { return XDocument.Parse(metin); }
        catch (System.Xml.XmlException ex) { throw new BankaBaglantiHatasi("Banka yanıtı okunamadı (XML değil).", ex); }
    }

    public static string Kac(string? d) => System.Security.SecurityElement.Escape(d ?? "") ?? "";
}

internal static class Sayi
{
    /// <summary>"1.250,67" · "1250.67" · "1,250.67" · "-8208,00" → decimal. Okunamazsa null.</summary>
    public static decimal? Oku(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().Replace(" ", "");
        int v = t.LastIndexOf(','), n = t.LastIndexOf('.');
        if (v >= 0 && n >= 0) t = v > n ? t.Replace(".", "").Replace(',', '.') : t.Replace(",", "");
        else if (v >= 0) t = t.Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}

public static class Tarih
{
    /// <summary>Türkiye yerel saati → UTC.</summary>
    public static DateTime Utc(DateTime yerel) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(yerel, DateTimeKind.Unspecified), Zaman.Isletme);

    public static DateTime Yerel(DateTime utc) => Zaman.UtcdenYerele(utc);

    /// <summary>Verilen biçimlerden biriyle yerel tarihi okur; ISO'yu da dener. Okunamazsa null.</summary>
    public static DateTime? Oku(string? s, params string[] bicimler)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (bicimler.Length > 0 && DateTime.TryParseExact(s, bicimler, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var o))
            return o.Offset == TimeSpan.Zero && s.EndsWith('Z') ? Yerel(o.UtcDateTime) : o.DateTime;
        return null;
    }

    /// <summary>[bas, bit] yerel aralığını en çok <paramref name="gun"/> günlük dilimlere böler.</summary>
    public static IEnumerable<(DateTime Bas, DateTime Bit)> Dilimle(DateTime bas, DateTime bit, int gun)
    {
        for (var b = bas; b < bit;)
        {
            var s = b.AddDays(gun) < bit ? b.AddDays(gun).AddSeconds(-1) : bit;
            yield return (b, s);
            b = s.AddSeconds(1);
        }
    }
}

internal static class Http
{
    public static async Task<string> Gonder(HttpClient http, HttpRequestMessage istek, string bankaAdi, CancellationToken iptal, Encoding? kodlama = null)
    {
        using var yanit = await http.SendAsync(istek, iptal);
        var bayt = await yanit.Content.ReadAsByteArrayAsync(iptal);
        var metin = (kodlama ?? Encoding.UTF8).GetString(bayt);
        // SOAP hatası 500 ile gelir: önce yanıttaki hata metnine bakılır (ör. "kullanıcı adı hatalı").
        if (!yanit.IsSuccessStatusCode)
        {
            if (metin.Contains("Fault", StringComparison.OrdinalIgnoreCase) && Soap.Hata(metin) is { } f)
                throw new BankaBaglantiHatasi($"{bankaAdi}: {f}");
            throw BankaBaglayicilari.HttpHatasi(yanit.StatusCode, bankaAdi);
        }
        return metin;
    }

    public static HttpRequestMessage Soap11(string url, string eylem, string zarf) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(zarf, Encoding.UTF8, "text/xml"),
        Headers = { { "SOAPAction", $"\"{eylem}\"" } },
    };

    public static void Basic(HttpRequestMessage istek, string kullanici, string sifre) =>
        istek.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kullanici}:{sifre}")));
}

internal static class Soap
{
    /// <summary>SOAP Fault metni (faultstring / Reason/Text); yoksa null. En çok 200 karakter.</summary>
    public static string? Hata(string metin)
    {
        try
        {
            var d = XDocument.Parse(metin);
            var f = Xml.Ilk(d, "faultstring")?.Value ?? Xml.Ilk(d, "Text")?.Value;
            return string.IsNullOrWhiteSpace(f) ? null : (f.Trim().Length > 200 ? f.Trim()[..200] : f.Trim());
        }
        catch (System.Xml.XmlException) { return null; }
    }
}

internal static class Giris
{
    public static string Zorunlu(IReadOnlyDictionary<string, string?> k, string ad) =>
        k.TryGetValue(ad, out var d) && !string.IsNullOrWhiteSpace(d) ? d.Trim()
            : throw new BankaBaglantiHatasi("Bağlantı bilgileri eksik; bilgileri yeniden girin.");

    public static string? Istege(IReadOnlyDictionary<string, string?> k, string ad) =>
        k.TryGetValue(ad, out var d) && !string.IsNullOrWhiteSpace(d) ? d.Trim() : null;

    /// <summary>"98886207-1" → ("98886207", "1"); tiresizse ek no null.</summary>
    public static (string No, string? Ek) Bol(string hesap)
    {
        var i = hesap.IndexOf('-');
        return i < 0 ? (hesap.Trim(), null) : (hesap[..i].Trim(), hesap[(i + 1)..].Trim());
    }
}

/// <summary>
/// Bankanın ham hareketi → Mihenk hareketi. Bankadan gelen geçersiz değer (IBAN, TC/VKN, ad) hareketi
/// KİLİTLEMEZ: o alan boş bırakılır (yoksa satır her turda reddedilip sonsuza dek beklerdi).
/// </summary>
public static class HareketTemizleyici
{
    public static SaglayiciHareketi? Temizle(string kod, string? referans, DateTime yerelTarih, bool gelen, decimal tutar,
        string? karsiAd, string? karsiIban, string? karsiKimlik, string? aciklama, string? yedekAnahtar = null)
    {
        tutar = Math.Round(Math.Abs(tutar), 2, MidpointRounding.AwayFromZero);
        if (tutar < 0.01m) return null;
        var iban = string.IsNullOrWhiteSpace(karsiIban) ? null : IbanDogrulayici.Normallestir(karsiIban);
        if (iban is not null && !IbanDogrulayici.Gecerli(iban)) iban = null;
        var kimlik = DogalAnahtar.Normalle(karsiKimlik);
        if (kimlik is not null && KimlikNo.Dogrula(kimlik, KimlikNo.TurTuret(kimlik)) is not null) kimlik = null;
        var ad = karsiAd?.Trim();
        if (string.IsNullOrEmpty(ad) || !ad.Any(char.IsLetter) || AdKurali.Unvan(ad) is not null) ad = null;
        else if (ad.Length > AlanSinirlari.HesapSahibi) ad = ad[..AlanSinirlari.HesapSahibi];
        var acik = aciklama?.Trim();
        if (string.IsNullOrEmpty(acik)) acik = null;
        else if (acik.Length > AlanSinirlari.DekontAciklama) acik = acik[..AlanSinirlari.DekontAciklama];
        var tarihUtc = Tarih.Utc(yerelTarih);
        // Referans: bankanın benzersiz no'su; yoksa hareketin kendisinden özet (aynı hareket aynı özeti verir).
        var r = !string.IsNullOrWhiteSpace(referans) ? referans.Trim()
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{yedekAnahtar}|{yerelTarih:O}|{gelen}|{tutar.ToString(CultureInfo.InvariantCulture)}|{aciklama}")))[..32];
        var tam = $"{kod}-{r}";
        if (tam.Length > 64) tam = $"{kod}-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r)))[..40];
        return new SaglayiciHareketi(tam, tarihUtc, gelen ? HareketYonu.BankaGelen : HareketYonu.BankaGiden,
            tutar, ad, iban, kimlik, acik);
    }
}
