namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5 · T-4b) — DESTEK KANALI SÖZLEŞMELERİ + KISA HATA KODU.
//
// ⚠ NEDEN BURADA (Pages DEĞİL): Pages içinde yeni istemci record'u açılmaz
//   (denetim.sh tavanı dolu). Sunucu ve istemci AYNI tipi kullanır; alan adı
//   ve tipi tek yerde durur (ad VE tip sözleşmesi).
//
// ⚠ KISA HATA KODU — EŞLEŞME YALNIZ SUNUCU TARAFINDA (plan riski):
//   Kod, sunucunun ürettiği istek kimliğinin (ULID, 26 karakter) SON 8
//   karakteridir. Bu karakterler ULID'in RASTGELE kısmıdır (zaman damgası
//   değildir) ve kiracı/kullanıcı/tutar gibi hiçbir bilgi taşımaz.
//   Sunucu günlüğü hem tam kimliği (kapsam) hem kısa kodu (satır alanı)
//   yazar; destek kısa kodla günlükte arar. İstemci kod ÜRETMEZ, yalnız
//   sunucunun verdiği kimliği KISALTIR — uydurma kod gösterilmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kullanıcıya gösterilen kısa hata kodu — tek kaynak.</summary>
public static class HataKodu
{
    /// <summary>Kısa kodun uzunluğu (ULID'in rastgele kuyruğu).</summary>
    public const int Uzunluk = 8;

    /// <summary>
    /// Sunucu kimliğinden kısa kod. Kimlik boşsa <c>null</c> döner (kod
    /// UYDURULMAZ). Kimlik zaten kısaysa olduğu gibi (büyük harfle) döner.
    /// </summary>
    public static string? Kisa(string? kimlik)
    {
        if (string.IsNullOrWhiteSpace(kimlik)) return null;
        var k = kimlik.Trim().ToUpperInvariant();
        return k.Length <= Uzunluk ? k : k[^Uzunluk..];
    }

    /// <summary>
    /// Kullanıcının destek formuna yazdığı kod GEÇERLİ mi? Yalnız Crockford
    /// Base32 alfabesi (ULID) ve tam olarak 8 ya da 26 karakter kabul edilir —
    /// serbest metin (ad, telefon, not) bu alandan pakete SIZAMAZ.
    /// </summary>
    public static bool GecerliMi(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod)) return false;
        var k = kod.Trim().ToUpperInvariant();
        if (k.Length != Uzunluk && k.Length != 26) return false;
        foreach (var c in k)
            if (!("0123456789ABCDEFGHJKMNPQRSTVWXYZ".Contains(c))) return false;
        return true;
    }
}

/// <summary>
/// POST /api/v1/destek/paket gövdesi. <c>Gerekce</c> ≥ 20 karakter; yalnız
/// işletmenin KENDİ denetim izine yazılır, PAKETE KONMAZ (serbest metin
/// kişisel veri taşıyabilir). <c>HataKodu</c> isteğe bağlıdır.
/// </summary>
public sealed record DestekPaketiIstegi(string? Gerekce, string? Konu, string? HataKodu);

/// <summary>POST /api/v1/destek/istemci-hatasi gövdesi — YALNIZ hata türü adı.</summary>
public sealed record IstemciHatasiBildirimi(string? Tur);

/// <summary>İstemci hatası bildiriminin yanıtı: sunucunun ürettiği kod.</summary>
public sealed record IstemciHatasiYaniti(string IstekKimligi, string HataKodu);

/// <summary>Destek paketi geçmiş satırı (işletmenin kendi denetim izinden).</summary>
public sealed record DestekPaketiGecmisSatiri(DateTime Tarih, string PaketKodu, string Konu);

/// <summary>GET /api/v1/destek/paket/gecmis zarfı (UX-S29: sınır + daha fazlası var).</summary>
public sealed record DestekPaketiGecmisi(
    bool KanalAcik, int Sinir, bool DahaFazlasiVar, int AsgariGerekce,
    List<string> Konular, List<DestekPaketiGecmisSatiri> Kayitlar);
