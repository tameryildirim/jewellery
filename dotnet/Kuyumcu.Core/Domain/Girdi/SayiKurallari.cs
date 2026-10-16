namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K9–K17 — SAYISAL GİRDİ KURALLARI (İŞ KURALI; tavanlar kolon kapasitesinden —
// Kuyumcu.Api/Modules/KolonTavani.cs ile aynı sayılar). Hepsi null → null
// (zorunluluk çağıranındır); sessiz yuvarlama YOK: fazla ondalık HATADIR.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>K9 tutar türü.</summary>
public enum TutarTuru
{
    /// <summary>&gt; 0 (en az 0,01).</summary>
    Pozitif,
    /// <summary>≥ 0 (sayılan nakit, kredi limiti, iskonto tutarı).</summary>
    SifirOlabilir,
    /// <summary>Eksi olabilir, sıfır olamaz (düzeltme farkı, sayım farkı).</summary>
    Isaretli,
}

/// <summary>Ortak sayı yardımcıları (aralık + ondalık + tavan).</summary>
public static class SayiKurali
{
    /// <summary>
    /// Genel alt/üst sınır (SayisalAlan EnAz/EnCok'un uygulaması). Alt sınır ≥ 0 iken
    /// eksi değer ayrı ve açık mesajla söylenir ("eksi yazılamaz").
    /// </summary>
    public static GirdiHatasi? Aralik(decimal? d, decimal? enAz, decimal? enCok)
    {
        if (d is not { } x) return null;
        if (enAz is { } a && x < a)
            return a >= 0 && x < 0 ? new GirdiHatasi(GirdiKodu.SayiEksi) : new GirdiHatasi(GirdiKodu.SayiEnAz, a);
        if (enCok is { } c && x > c) return new GirdiHatasi(GirdiKodu.SayiEnCok, c);
        return null;
    }

    /// <summary>Fazla ondalık mı?</summary>
    public static GirdiHatasi? Ondalik(decimal d, int enCok) =>
        GirdiYardim.OndalikHane(d) > enCok ? new GirdiHatasi(GirdiKodu.SayiOndalik, enCok) : null;

    /// <summary>Mutlak değer tavanı.</summary>
    public static GirdiHatasi? Tavan(decimal d, decimal tavan) =>
        Math.Abs(d) > tavan ? new GirdiHatasi(GirdiKodu.SayiTavan, tavan) : null;
}

/// <summary>K9 / K9a / K9b — tutar (TL/döviz): 2 ondalık, decimal(15,2) tavanı.</summary>
public static class TutarKurali
{
    public const int OndalikHane = 2;
    public const decimal EnAz = 0.01m;
    /// <summary>decimal(15,2) — KolonTavani.D15_2 / PozitifTutar.</summary>
    public const decimal Tavan = 9_999_999_999_999.99m;

    public static GirdiHatasi? Denetle(decimal? d, TutarTuru tur = TutarTuru.Pozitif)
    {
        if (d is not { } x) return null;
        if (tur != TutarTuru.Isaretli && x < 0) return new(GirdiKodu.SayiEksi);
        if (x == 0 && tur != TutarTuru.SifirOlabilir) return new(GirdiKodu.SayiSifir);
        return SayiKurali.Ondalik(x, OndalikHane) ?? SayiKurali.Tavan(x, Tavan);
    }
}

/// <summary>K10/K11 ağırlık türü (ekran Gram bileşeninin Tur parametresi).</summary>
public enum AgirlikTuru { Gram, Tas, Has }

/// <summary>K10 / K11 — gram (brüt/net/taş) ve has gram.</summary>
public static class AgirlikKurali
{
    public const int GramOndalik = 3;
    public const int HasOndalik = 6;
    /// <summary>decimal(12,3) — KolonTavani.D12_3.</summary>
    public const decimal GramTavan = 999_999_999.999m;
    /// <summary>decimal(15,6) — KolonTavani.D15_6.</summary>
    public const decimal HasTavan = 999_999_999.999999m;

    /// <summary>Türüne göre tek giriş (ekran bileşeni Gram Tur=… bunu çağırır). null tür = kural yok.</summary>
    public static GirdiHatasi? Denetle(decimal? d, AgirlikTuru? tur, decimal? brut = null) => tur switch
    {
        AgirlikTuru.Gram => Gram(d),
        AgirlikTuru.Tas => Tas(d, brut),
        AgirlikTuru.Has => Has(d),
        _ => null,
    };

    /// <summary>Gram: &gt; 0, en çok 3 ondalık.</summary>
    public static GirdiHatasi? Gram(decimal? d)
    {
        if (d is not { } x) return null;
        if (x < 0) return new(GirdiKodu.SayiEksi);
        if (x == 0) return new(GirdiKodu.SayiSifir);
        return SayiKurali.Ondalik(x, GramOndalik) ?? SayiKurali.Tavan(x, GramTavan);
    }

    /// <summary>Taş ağırlığı: ≥ 0, 3 ondalık, brüt verildiyse brütten KÜÇÜK (has eksi olmasın).</summary>
    public static GirdiHatasi? Tas(decimal? tas, decimal? brut)
    {
        if (tas is not { } x) return null;
        if (x < 0) return new(GirdiKodu.SayiEksi);
        var h = SayiKurali.Ondalik(x, GramOndalik) ?? SayiKurali.Tavan(x, GramTavan);
        if (h is not null) return h;
        return x > 0 && brut is { } b && x >= b ? new GirdiHatasi(GirdiKodu.TasBrut) : null;
    }

    /// <summary>Has gram: &gt; 0 (yönlü işlemde eksi yok), en çok 6 ondalık.</summary>
    public static GirdiHatasi? Has(decimal? d)
    {
        if (d is not { } x) return null;
        if (x < 0) return new(GirdiKodu.SayiEksi);
        if (x == 0) return new(GirdiKodu.SayiSifir);
        return SayiKurali.Ondalik(x, HasOndalik) ?? SayiKurali.Tavan(x, HasTavan);
    }
}

/// <summary>K12 — milyem 1–1000, en çok 3 ondalık (milyem tanımı gereği 1000'i aşamaz).</summary>
public static class MilyemKurali
{
    public const decimal EnAz = 1m;
    public const decimal EnCok = 1000m;
    public const int OndalikHane = 3;
    /// <summary>Ayarın standart milyeminden makul sapma (öneri, UYARI — engel değil).</summary>
    public const decimal SapmaUyari = 50m;

    public static GirdiHatasi? Denetle(decimal? d)
    {
        if (d is not { } x) return null;
        if (x < EnAz || x > EnCok) return new(GirdiKodu.MilyemAralik, EnAz, EnCok);
        return SayiKurali.Ondalik(x, OndalikHane);
    }

    /// <summary>Ayar verildiyse milyem standarttan ±50'den fazla sapıyor mu (uyarı)?</summary>
    public static bool SapmaVar(decimal? milyem, string? ayarId) =>
        milyem is { } m && ayarId is { Length: > 0 } && Ayarlar.Bul(ayarId) is { } a
        && Math.Abs(m - a.Milyem) > SapmaUyari;
}

/// <summary>
/// M1 (müşteri 16.10: "bileziğin milyemi, çarpanı burada olacak, elimle müdahale edebileceğim;
/// 925 ise 925") — SATIŞ milyemi: kuyumcunun satışta has fiyatıyla çarptığı milyem
/// (ayar milyemi + işçilik/kâr payı). Ayar milyeminden farklı olarak 1000'i aşabilir
/// (24 ayarda kâr payı); kolon decimal(7,2) → 1–2000, en çok 2 ondalık.
/// </summary>
public static class SatisMilyemiKurali
{
    public const decimal EnAz = 1m;
    public const decimal EnCok = 2000m;
    public const int OndalikHane = 2;

    public static GirdiHatasi? Denetle(decimal? d)
    {
        if (d is not { } x) return null;
        if (x < EnAz || x > EnCok) return new(GirdiKodu.MilyemAralik, EnAz, EnCok);
        return SayiKurali.Ondalik(x, OndalikHane);
    }
}

/// <summary>
/// M2b (müşteri 16.10: "150 bin liralık fatura, binde 1'i KDV'ye giren kısım, geri kalanı %0";
/// "yaz binde birden") — ürünün KDV'ye giren payı, BİNDE: 0 &lt; x ≤ 1000, en çok 3 ondalık
/// (kolon decimal(7,3)).
/// </summary>
public static class KdvMatrahPayiKurali
{
    public const decimal EnAz = 0.001m;
    public const decimal EnCok = 1000m;
    public const int OndalikHane = 3;

    public static GirdiHatasi? Denetle(decimal? d)
    {
        if (d is not { } x) return null;
        if (x <= 0m) return new(GirdiKodu.SayiSifir);
        if (x > EnCok) return new(GirdiKodu.MilyemAralik, EnAz, EnCok);
        return SayiKurali.Ondalik(x, OndalikHane);
    }
}

/// <summary>K13 — ayar: serbest giriş yok, yalnız Ayarlar listesi.</summary>
public static class AyarKurali
{
    public static GirdiHatasi? Denetle(string? ayarId) =>
        string.IsNullOrWhiteSpace(ayarId) || Ayarlar.Bul(ayarId) is not null ? null : new GirdiHatasi(GirdiKodu.AyarBilinmiyor);
}

/// <summary>K14 — adet: tam sayı ≥ 1 (gramlı üründe miktar gramdır, adet değil).</summary>
public static class AdetKurali
{
    public const decimal EnAz = 1m;
    /// <summary>decimal(12,3) kolonunun tam sayı kısmı.</summary>
    public const decimal Tavan = 999_999_999m;

    public static GirdiHatasi? Denetle(decimal? d)
    {
        if (d is not { } x) return null;
        if (x != decimal.Truncate(x)) return new(GirdiKodu.SayiTam);
        if (x < EnAz) return new(GirdiKodu.SayiEnAz, EnAz);
        return SayiKurali.Tavan(x, Tavan);
    }
}

/// <summary>K15 — yüzde 0–100 (n ondalık); marj/kâr oranı ≥ 0, en çok 500.</summary>
public static class YuzdeKurali
{
    public const int OndalikHane = 2;
    public const decimal MarjEnCok = 500m;

    public static GirdiHatasi? Denetle(decimal? d, int ondalik = OndalikHane)
    {
        if (d is not { } x) return null;
        if (x < 0 || x > 100) return new(GirdiKodu.YuzdeAralik, 0, 100);
        return SayiKurali.Ondalik(x, ondalik);
    }

    public static GirdiHatasi? Marj(decimal? d, int ondalik = OndalikHane)
    {
        if (d is not { } x) return null;
        if (x < 0) return new(GirdiKodu.SayiEksi);
        if (x > MarjEnCok) return new(GirdiKodu.SayiEnCok, MarjEnCok);
        return SayiKurali.Ondalik(x, ondalik);
    }
}

/// <summary>K16 — kur (döviz/altın): &gt; 0, en çok 6 ondalık, decimal(15,6) tavanı.</summary>
public static class KurKurali
{
    public const int OndalikHane = 6;
    public const decimal Tavan = 999_999_999.999999m;

    public static GirdiHatasi? Denetle(decimal? d)
    {
        if (d is not { } x) return null;
        if (x < 0) return new(GirdiKodu.SayiEksi);
        if (x == 0) return new(GirdiKodu.SayiSifir);
        return SayiKurali.Ondalik(x, OndalikHane) ?? SayiKurali.Tavan(x, Tavan);
    }
}

/// <summary>K17 — gün sayısı (vade 0–3650, valör 0–90, taksit 2–12): tam sayı, aralık çağırandan.</summary>
public static class GunSayisiKurali
{
    public static GirdiHatasi? Denetle(decimal? d, int enAz, int enCok)
    {
        if (d is not { } x) return null;
        if (x != decimal.Truncate(x)) return new(GirdiKodu.SayiTam);
        if (x < enAz || x > enCok) return new(GirdiKodu.GunAralik, enAz, enCok);
        return null;
    }
}
