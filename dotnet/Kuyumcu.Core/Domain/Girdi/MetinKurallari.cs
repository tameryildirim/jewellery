namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K7, K18–K21, K30 — METİN GİRDİ KURALLARI. Hepsi boş → null (zorunluluk
// çağıranındır).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// K18 — barkod / kod. STANDART: ISO/IEC 15417 (Code 128-B basılabilir küme ASCII
/// 32–126), GS1 General Specifications (EAN-13 kontrol hanesi). Bizim barkodumuz
/// Code 128-B (Kuyumcu.Core/Domain/Barkod.cs); EAN-13 yalnız tedarikçi barkodunda
/// UYARI olarak denetlenir.
/// </summary>
public static class BarkodKurali
{
    public static GirdiHatasi? Denetle(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (s[0] == ' ') return new(GirdiKodu.BarkodBosluk);
        if (s.Length > AlanSinirlari.Barkod) return new(GirdiKodu.BarkodUzunluk, 1, AlanSinirlari.Barkod);
        foreach (var c in s)
            if (c is < ' ' or > '~') return new(GirdiKodu.BarkodKarakter);
        return null;
    }

    /// <summary>13 rakamlı barkodun EAN-13 kontrol hanesi tutmuyorsa UYARI (engel değil).</summary>
    public static GirdiHatasi? Ean13Uyari(string? s) =>
        s is { Length: 13 } && KimlikNoDogrulayici.YalnizRakam(s) && !Ean13Gecerli(s)
            ? new GirdiHatasi(GirdiKodu.Ean13Kontrol) : null;

    /// <summary>GS1: soldan tek konumlar ×1, çift konumlar ×3; kontrol = toplamı 10'a tamamlayan hane.</summary>
    public static bool Ean13Gecerli(string s)
    {
        if (s.Length != 13 || !KimlikNoDogrulayici.YalnizRakam(s)) return false;
        var t = 0;
        for (var i = 0; i < 12; i++) t += (s[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - t % 10) % 10 == s[12] - '0';
    }
}

/// <summary>K19 / K20 — ad-soyad ve ünvan: ≥ 2 karakter, en az bir harf (İŞ KURALI).</summary>
public static class AdKurali
{
    public const int EnAz = 2;

    /// <summary>Kırpar, çift boşluğu teke indirir (kayıt biçimi).</summary>
    public static string Normallestir(string? s) => GirdiYardim.BoslukSadelestir(s);

    public static GirdiHatasi? Denetle(string? s, int enCok = AlanSinirlari.Ad)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = Normallestir(s);
        if (n.Length < EnAz) return new(GirdiKodu.AdKisa, EnAz);
        if (!GirdiYardim.HarfVar(n)) return new(GirdiKodu.AdHarf);
        if (n.Length > enCok) return new(GirdiKodu.AdUzun, enCok);
        return null;
    }

    public static GirdiHatasi? Unvan(string? s) => Denetle(s, AlanSinirlari.Unvan);
}

/// <summary>
/// K21 — adres: ≥ 10 karakter, en az bir harf, ≤ 500. ⚠ 10 karakter eşiği İŞ KURALI
/// ve TEYİTSİZ (envanter K21). Bu paket hiçbir mevcut DTO'ya uygulamaz; göç paketi
/// ekip oyuyla uygular.
/// </summary>
public static class AdresKurali
{
    public const int EnAz = 10;

    public static GirdiHatasi? Denetle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = GirdiYardim.BoslukSadelestir(s);
        if (n.Length < EnAz) return new(GirdiKodu.AdresKisa, EnAz);
        if (!GirdiYardim.HarfVar(n)) return new(GirdiKodu.AdresHarf);
        if (n.Length > AlanSinirlari.Adres) return new(GirdiKodu.AdresUzun, AlanSinirlari.Adres);
        return null;
    }
}

/// <summary>
/// K7 — pasaport no: harf + rakam, boşluksuz, büyük harf, 5–20. STANDART: ICAO Doc
/// 9303 Part 4 (MRZ belge no alanı 9 karakter); TR biçimi ("1 harf + 8 rakam")
/// TEYİTSİZ → geniş desen (yabancı pasaportlar ülkeye göre değişir).
/// </summary>
public static class PasaportKurali
{
    private static readonly System.Text.RegularExpressions.Regex Desen =
        new(@"^[A-Z0-9]{5,20}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static string Normallestir(string? s) => (s ?? "").Trim().ToUpperInvariant();

    public static GirdiHatasi? Denetle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return Desen.IsMatch(Normallestir(s)) ? null : new GirdiHatasi(GirdiKodu.PasaportBicim, 5, AlanSinirlari.Pasaport);
    }
}

/// <summary>K30 — gerekçe (storno/düzeltme): kırpılmış 20–1000 karakter (proje kuralı, GerekceDto ile aynı).</summary>
public static class GerekceKurali
{
    public static GirdiHatasi? Denetle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = s.Trim().Length;
        if (n < AlanSinirlari.GerekceEnAz) return new(GirdiKodu.GerekceKisa, AlanSinirlari.GerekceEnAz, n);
        if (n > AlanSinirlari.GerekceEnCok) return new(GirdiKodu.GerekceUzun, AlanSinirlari.GerekceEnCok, n);
        return null;
    }
}

/// <summary>
/// K4–K6 — TCKN / VKN köprüsü. Kural TCKN-DOGRULAMA paketinin <see cref="KimlikNoDogrulayici"/>'sidir
/// (YENİDEN YAZILMAZ); burada yalnız sonucu girdi ailesinin kodlarına çevrilir. VKN kontrol hanesi
/// TEYİTSİZ → yalnız 10 hane (KimlikNoDogrulayici ile aynı).
/// </summary>
public static class KimlikKurali
{
    private static GirdiHatasi? Cevir(KimlikNoHatasi h, int hane) => h switch
    {
        KimlikNoHatasi.Yok => null,
        KimlikNoHatasi.RakamDisi => new GirdiHatasi(GirdiKodu.KimlikRakam),
        KimlikNoHatasi.HaneSayisi => new GirdiHatasi(GirdiKodu.KimlikHane, hane),
        _ => new GirdiHatasi(GirdiKodu.TcknGecersiz),
    };

    public static GirdiHatasi? Tckn(string? s) => Cevir(KimlikNoDogrulayici.TcknDenetle(s), 11);

    public static GirdiHatasi? Vkn(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = s.Trim();
        if (!KimlikNoDogrulayici.YalnizRakam(n)) return new(GirdiKodu.KimlikRakam);
        return KimlikNoDogrulayici.VknBicimGecerli(n) ? null : new GirdiHatasi(GirdiKodu.KimlikHane, 10);
    }

    /// <summary>TCKN ya da VKN: hane hatasında mesaj "10 ya da 11" der (tek sayı söylemez — doğrulayıcı tur 1).</summary>
    public static GirdiHatasi? TcknVeyaVkn(string? s)
    {
        var h = KimlikNoDogrulayici.TcknVeyaVknDenetle(s);
        return h == KimlikNoHatasi.HaneSayisi
            ? new GirdiHatasi(GirdiKodu.KimlikHaneIkili, (s ?? "").Trim().Length)
            : Cevir(h, 11);
    }

    /// <summary>
    /// PAKET GOC-B tur 2 (doğrulayıcı DÜŞÜK bulgu): evrak borçlusu YABANCI olabilir. Paket öncesinde
    /// 10-20 karakter serbestti; tur 1 yalnız 10/11 rakama daraltmıştı (sapma yazılmamıştı).
    /// Kural: yalnız rakamsa TCKN/VKN (K4/K5, KimlikNoDogrulayici — YENİDEN YAZILMAZ); harf içeriyorsa
    /// pasaport / yabancı kimlik no (K7 PasaportKurali, 5–20 harf/rakam). Tek kaynak: ekran ve sunucu.
    /// </summary>
    public static GirdiHatasi? TcknVknVeyaPasaport(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = s.Trim();
        return KimlikNoDogrulayici.YalnizRakam(n) ? TcknVeyaVkn(n) : PasaportKurali.Denetle(n);
    }
}
