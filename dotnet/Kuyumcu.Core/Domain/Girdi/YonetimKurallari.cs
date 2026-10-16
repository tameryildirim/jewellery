using System.Text.RegularExpressions;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-D — yönetim ve rapor ekranlarının "önce kapanacak açıkları" (envanter
// GIRDI-KURALLARI-ENVANTER-2026-10-08 § E · GOC-D satırı). Kural TEK YERDE; ekran
// (alan altı + yeniden yükleme kilidi) ve sunucu (400 + aynı metin) bu sınıfları çağırır.
//   • RaporDonemKurali  — K8e rapor/süzgeç aralığı: her uç Rapor penceresinde
//                         (2000..gelecek yıl sonu), bas ≤ bit, en çok 400 gün (sunucunun
//                         mevcut performans sınırı RaporAraligi.EnFazlaGun — İŞ KURALI).
//   • ParolaKurali      — K31 proje parola politikası: 8–72 karakter, en az bir harf ve
//                         bir rakam (ParolaSifirlama + Kullanici DTO'larıyla AYNI kural;
//                         kayıt ucu eskiden "aaaaaaaa"yı kabul ediyordu).
//   • RaporSuzgecKurali — K32 süzgeç beyaz listeleri (ödeme aracı, denetim eylemi,
//                         varlık türü biçimi): bilinmeyen değer "kayıt yok"a çevrilmez, 400.
// Mesajlar sözlükten (GirdiSozlugu / Sozlukler.Kabuk / GocDSozlugu) — çıplak metin yok.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Rapor/süzgeç dönemi kuralı (K8e) — ekran ve sunucu aynı sınıf.</summary>
public static class RaporDonemKurali
{
    /// <summary>Rapor döneminin en uzun hâli (gün) — sunucunun mevcut sınırı (RaporAraligi.EnFazlaGun).</summary>
    public const int EnCokGun = 400;

    /// <summary>
    /// Dönem denetimi. Boş uç denetlenmez (varsayılanı çağıran verir). Sıra: başlangıç penceresi →
    /// bitiş penceresi → ters aralık → uzunluk. <paramref name="enCokGun"/> ≤ 0 ise gün sınırı yerine
    /// K8e'nin 5 yıl kuralı uygulanır (denetim izi gibi sayfalı dökümler).
    /// </summary>
    /// <returns>null = geçerli; değilse (sunucu alan adı, kullanıcının dilinde metin).</returns>
    public static (string Alan, string Mesaj)? Denetle(DateOnly? bas, DateOnly? bit, DateOnly bugun, string dil,
                                                       int enCokGun = EnCokGun)
    {
        if (TarihPenceresi.Denetle(bas, TarihTuru.Rapor, bugun) is { } hb) return ("baslangic", GirdiSozlugu.Mesaj(hb, dil));
        if (TarihPenceresi.Denetle(bit, TarihTuru.Rapor, bugun) is { } hs) return ("bitis", GirdiSozlugu.Mesaj(hs, dil));
        if (bas is not { } b || bit is not { } s) return null;
        if (s < b) return ("bitis", Sozlukler.Getir(dil).Kabuk.TarihAraligi);
        if (enCokGun > 0)
        {
            if (s.DayNumber - b.DayNumber + 1 > enCokGun)
                return ("bitis", string.Format(GocDSozlugu.Al(dil).RaporDonemUzun, enCokGun));
        }
        else if (TarihPenceresi.AralikDenetle(b, s) is { } ha)
        {
            return ("bitis", GirdiSozlugu.Mesaj(ha, dil));
        }
        return null;
    }

    /// <summary>Tek gün (mutabakat günü, günlük özet) — Rapor penceresi.</summary>
    public static string? GunDenetle(DateOnly? gun, DateOnly bugun, string dil) =>
        TarihPenceresi.Denetle(gun, TarihTuru.Rapor, bugun) is { } h ? GirdiSozlugu.Mesaj(h, dil) : null;
}

/// <summary>K31 — parola politikası (tek kaynak). Metin: AuthSifirlaSozlugu.ParolaKurali.</summary>
public static class ParolaKurali
{
    public const int EnAz = 8;
    /// <summary>bcrypt 72 bayttan sonrasını yok sayar — üst sınır bu yüzden 72.</summary>
    public const int EnCok = 72;

    /// <summary>8–72 karakter, en az bir ASCII harf ve bir ASCII rakam.</summary>
    public static bool Gecerli(string? p) =>
        p is { Length: >= EnAz and <= EnCok } && p.Any(char.IsAsciiDigit) && p.Any(char.IsAsciiLetter);

    /// <summary>Geçersizse kullanıcının dilinde kural metni; geçerliyse null. Boş → null (zorunluluk çağıranın).</summary>
    public static string? Mesaj(string? p, string? dil) =>
        string.IsNullOrEmpty(p) || Gecerli(p) ? null : AuthSifirlaSozlugu.Al(dil).ParolaKurali;
}

/// <summary>K32 — rapor/denetim süzgeç beyaz listeleri (tek kaynak).</summary>
public static class RaporSuzgecKurali
{
    /// <summary>
    /// Hareketin ödeme aracı (hareketler.odeme_turu) — deftere fiilen yazılan değerler:
    /// Entities.OdemeTurleri (NAKIT, KART, HAVALE, ALTIN, DIGER) + çok bacaklı fişin KARMA etiketi +
    /// VERESIYE + DOVIZ (OdemeSepeti bacak aracı). İşlem defteri seçicisi bu listenin ALT kümesidir.
    /// </summary>
    public static readonly IReadOnlyList<string> OdemeAraclari =
        ["NAKIT", "KART", "HAVALE", "ALTIN", "DIGER", "VERESIYE", "KARMA", "DOVIZ"];

    /// <summary>Denetim izi eylemleri (Data/DenetimIzi.cs DenetimEylemi ile aynı beş değer).</summary>
    public static readonly IReadOnlyList<string> DenetimEylemleri = ["OKUMA", "EKLEME", "GUNCELLEME", "SILME", "REDDEDILDI"];

    /// <summary>Varlık türü bir C# tür adıdır (ör. "Cari", "GiderPusulasi") — biçim dışı değer süzgeç olamaz.</summary>
    private static readonly Regex VarlikTuruDeseni = new("^[A-Za-z][A-Za-z0-9_]{0,99}$", RegexOptions.CultureInvariant);

    public static bool VarlikTuruGecerli(string? s) => s is not null && VarlikTuruDeseni.IsMatch(s);

    /// <summary>Boş değer süzgeç değildir (geçer); dolu değer listede TAM eşleşmeli.</summary>
    public static bool ListedeMi(string? deger, IEnumerable<string> liste) =>
        string.IsNullOrEmpty(deger) || liste.Contains(deger, StringComparer.Ordinal);

    /// <summary>Kullanıcının dilinde "tanınmayan süzgeç" metni.</summary>
    public static string Mesaj(string? dil) => GocDSozlugu.Al(dil).SuzgecBilinmiyor;
}

/// <summary>
/// PAKET GOC-D — yönetim ekranlarının uzunluk sınırları (ekran EnFazla = DTO MaxLength TEK sabit;
/// envanter F2 "ekran maxlength = sunucu sınırı"). Ortak AlanSinirlari'nda olanlar (Ad, Adres) oradan okunur.
/// </summary>
public static class YonetimSinirlari
{
    /// <summary>Dükkân (işletme) adı — KayitDto.IsletmeAdi MaxLength(200).</summary>
    public const int IsletmeAdi = 200;
    /// <summary>Şube adı — SubeOlusturDto/SubeGuncelleDto MaxLength(200).</summary>
    public const int SubeAdi = 200;
    /// <summary>Davet jetonu — KayitDto.DavetJetonu MaxLength(200) (ekran 100'dü: uzun jeton kırpılıyordu).</summary>
    public const int DavetJetonu = 200;
    /// <summary>Vergi dairesi — TenantKimlikGuncelleDto MaxLength(100).</summary>
    public const int VergiDairesi = 100;
    /// <summary>
    /// Girişte / MFA kapatmada / kurtarma kodu yenilemede kod kutusu: 6 haneli TOTP ya da 16 karakterlik
    /// kurtarma kodu (ayraçlı yazım dahil). DTO StringLength(25). Orkestratör düzeltmesi (C4_62/C4_63) DEĞERİ KORUNDU.
    /// </summary>
    public const int MfaKodGirisi = 24;
    /// <summary>MFA kurulum onayı — yalnız TOTP (6 hane); kurtarma kodu burada geçmez.</summary>
    public const int MfaKurulumKodu = 10;
    /// <summary>Destek paketi hata kodu — HataKodu: 8 karakter kısa kod ya da 26 karakter tam kimlik.</summary>
    public const int DestekHataKodu = 26;
    /// <summary>Yedek tatbikatı / iptal gerekçesi — YedekModulu DTO MaxLength(500) (ekran 1000'di).</summary>
    public const int YedekGerekce = 500;
}
