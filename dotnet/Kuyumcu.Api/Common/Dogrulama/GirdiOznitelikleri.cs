using System.ComponentModel.DataAnnotations;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Api.Common.Dogrulama;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — SUNUCU GİRDİ ÖZNİTELİKLERİ.
// Her öznitelik Kuyumcu.Core/Domain/Girdi'deki AYNI kuralı çağırır; DTO'da elle
// 'if' ya da [RegularExpression] ile ikinci bir kural yazılmaz (denetim.sh
// IBAN_REGEX_DTO). Mesaj GirdiSozlugu.Tr'den gelir → ekranın alan altı metniyle
// AYNI cümle (ekran kullanıcının dilinde aynı anahtarı basar).
// Boş/null değer GEÇER: zorunluluk [Required]'ın işidir.
// ⚠ DataAnnotations yalnız controller yolunda çalışır (DEĞİŞMEZ kuralı); servis
//   içinden çağrılan yolda aynı Core kuralı elle çağrılır.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Girdi özniteliklerinin ortak tabanı.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public abstract class GirdiOzniteligi : ValidationAttribute
{
    /// <summary>Kural: değer geçerliyse null.</summary>
    protected abstract GirdiHatasi? Denetle(object? deger);

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null) return ValidationResult.Success;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
        var h = Denetle(value);
        if (h is not { } hata) return ValidationResult.Success;
        var uye = ctx.MemberName is { Length: > 0 } m ? new[] { m } : null;
        return new ValidationResult(GirdiSozlugu.Mesaj(hata, Kuyumcu.Core.I18n.IstekDili.Gecerli), uye);
    }

    /// <summary>decimal / int / double değerini decimal'e çevirir; tanınmayan tür → null.</summary>
    protected static decimal? Ondalik(object? v) => v switch
    {
        decimal d => d,
        int i => i,
        long l => l,
        double db => (decimal)db,
        _ => null,
    };

    /// <summary>DateOnly / DateTime / DateTimeOffset değerini işletme gününe çevirir.</summary>
    protected static DateOnly? Gun(object? v) => v switch
    {
        DateOnly d => d,
        DateTime dt => dt.Kind == DateTimeKind.Utc ? Zaman.YerelBugun(dt) : DateOnly.FromDateTime(dt),
        DateTimeOffset o => Zaman.YerelBugun(o.UtcDateTime),
        _ => null,
    };
}

/// <summary>K1 — IBAN (TCMB 2008/6 md.4 + MOD 97-10).</summary>
public sealed class IbanAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => IbanDogrulayici.Denetle(d as string);
}

/// <summary>K2 — telefon (ITU E.164 TR bildirimi).</summary>
public sealed class TelefonAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => TelefonDogrulayici.Denetle(d as string);
}

/// <summary>K3 — e-posta (RFC 5321/5322 + alan adında nokta).</summary>
public sealed class EpostaAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => EpostaDogrulayici.Denetle(d as string);
}

/// <summary>K8–K8e — tarih penceresi (işletme günü Europe/Istanbul).</summary>
public sealed class TarihPenceresiAttribute(TarihTuru tur) : GirdiOzniteligi
{
    public TarihTuru Tur { get; } = tur;
    protected override GirdiHatasi? Denetle(object? d) => TarihPenceresi.Denetle(Gun(d), Tur, Zaman.YerelBugun());
}

/// <summary>K9–K9b — tutar.</summary>
public sealed class TutarAttribute(TutarTuru tur = TutarTuru.Pozitif) : GirdiOzniteligi
{
    public TutarTuru Tur { get; } = tur;
    protected override GirdiHatasi? Denetle(object? d) => TutarKurali.Denetle(Ondalik(d), Tur);
}

/// <summary>K10 — gram.</summary>
public sealed class GramAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AgirlikKurali.Gram(Ondalik(d));
}

/// <summary>K11 — has gram.</summary>
public sealed class HasAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AgirlikKurali.Has(Ondalik(d));
}

/// <summary>K12 — milyem.</summary>
public sealed class MilyemAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => MilyemKurali.Denetle(Ondalik(d));
}

/// <summary>K13 — ayar (yalnız Ayarlar listesi).</summary>
public sealed class AyarAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AyarKurali.Denetle(d as string);
}

/// <summary>K14 — adet (tam sayı ≥ 1).</summary>
public sealed class AdetAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AdetKurali.Denetle(Ondalik(d));
}

/// <summary>K15 — yüzde (0–100, n ondalık).</summary>
public sealed class YuzdeAttribute(int ondalik = YuzdeKurali.OndalikHane) : GirdiOzniteligi
{
    public int OndalikHane { get; } = ondalik;
    protected override GirdiHatasi? Denetle(object? d) => YuzdeKurali.Denetle(Ondalik(d), OndalikHane);
}

/// <summary>K16 — kur.</summary>
public sealed class KurAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => KurKurali.Denetle(Ondalik(d));
}

/// <summary>K17 — gün sayısı.</summary>
public sealed class GunSayisiAttribute(int enAz, int enCok) : GirdiOzniteligi
{
    public int EnAz { get; } = enAz;
    public int EnCok { get; } = enCok;
    protected override GirdiHatasi? Denetle(object? d) => GunSayisiKurali.Denetle(Ondalik(d), EnAz, EnCok);
}

/// <summary>K18 — barkod.</summary>
public sealed class BarkodAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => BarkodKurali.Denetle(d as string);
}

/// <summary>K19 — ad / soyad.</summary>
public sealed class AdAttribute(int enCok = AlanSinirlari.Ad) : GirdiOzniteligi
{
    public int EnCok { get; } = enCok;
    protected override GirdiHatasi? Denetle(object? d) => AdKurali.Denetle(d as string, EnCok);
}

/// <summary>K20 — ünvan / firma adı.</summary>
public sealed class UnvanAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AdKurali.Unvan(d as string);
}

/// <summary>K21 — adres (eşik TEYİTSİZ İŞ KURALI; göç paketi ekip oyuyla uygular).</summary>
public sealed class AdresAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => AdresKurali.Denetle(d as string);
}

/// <summary>K7 — pasaport no.</summary>
public sealed class PasaportAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => PasaportKurali.Denetle(d as string);
}

/// <summary>K30 — gerekçe (20–1000).</summary>
public sealed class GerekceAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => GerekceKurali.Denetle(d as string);
}

/// <summary>K8e — sorgu parametresi tarih aralığı için elle çağrılan yardımcı (öznitelik tek alana bağlıdır).</summary>
public static class TarihAraligi
{
    /// <summary>Aralık bozuksa sözlük metniyle 400 (BadRequestException) fırlatır.</summary>
    public static void Denetle(DateOnly? bas, DateOnly? bit, string alan = "bitis")
    {
        if (TarihPenceresi.AralikDenetle(bas, bit) is { } h)
            throw new BadRequestException(GirdiSozlugu.Mesaj(h, Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle(alan);
    }
}

/// <summary>K5 — TCKN (TCKN-DOGRULAMA KimlikNoDogrulayici).</summary>
public sealed class TcknAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => KimlikKurali.Tckn(d as string);
}

/// <summary>K4 — VKN (10 hane; kontrol hanesi TEYİTSİZ).</summary>
public sealed class VknAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => KimlikKurali.Vkn(d as string);
}

/// <summary>K4/K5 — TCKN ya da VKN (uzunluktan).</summary>
public sealed class TcknVeyaVknAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => KimlikKurali.TcknVeyaVkn(d as string);
}

/// <summary>K4/K5 ya da K7 — rakamsa TCKN/VKN, harf içeriyorsa pasaport no (PAKET GOC-B tur 2: yabancı evrak borçlusu).</summary>
public sealed class TcknVknVeyaPasaportAttribute : GirdiOzniteligi
{
    protected override GirdiHatasi? Denetle(object? d) => KimlikKurali.TcknVknVeyaPasaport(d as string);
}
