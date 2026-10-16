namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// TÜRKÇE METİN NORMALLEŞTİRME — TEK KAYNAK (paket P-36)
//
// NEDEN: "ısparta" yazan kullanıcı "Isparta" kaydını bulamıyordu. Katlama
// kuralı bugüne dek MenuTanimi içinde private durdu; Secici bileşeni de aynı
// kurala muhtaç. İKİ NORMALLEŞTİRME YASAK (FRONTEND-KIMLIK § 6.5) — kural
// buraya taşındı, MenuTanimi ve Secici aynı metodu çağırır.
//
// ⚠ tr-TR kültürüyle küçültme YAPILMAZ: ToLower("I", tr-TR) = "ı" olur ve
//   "ısparta" ile "Isparta" yine ayrışırdı. Harf haritası İKİ YÖNÜ de aynı
//   harfe katlar: İ/I/ı/i → 'i'.
// ═════════════════════════════════════════════════════════════════════════════

public static class AramaMetni
{
    /// <summary>Türkçe harfleri ASCII'ye katlar ve küçültür (kültürden bağımsız).</summary>
    public static string Katla(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var h in s)
            sb.Append(h switch
            {
                'İ' or 'I' or 'ı' or 'i' => 'i',
                'Ş' or 'ş' => 's',
                'Ğ' or 'ğ' => 'g',
                'Ü' or 'ü' => 'u',
                'Ö' or 'ö' => 'o',
                'Ç' or 'ç' => 'c',
                'Â' or 'â' => 'a',
                'Î' or 'î' => 'i',
                'Û' or 'û' => 'u',
                _ => char.ToLowerInvariant(h),
            });
        return sb.ToString();
    }

    /// <summary>Aksan/büyük-küçük duyarsız "içinde geçiyor mu" (Ordinal).</summary>
    public static bool Iceriyor(string? metin, string? sorgu)
    {
        if (string.IsNullOrWhiteSpace(sorgu)) return true;
        return Katla(metin).Contains(Katla(sorgu), StringComparison.Ordinal);
    }

    /// <summary>Baştan eşleşme — sıralamada öne alınır.</summary>
    public static bool BastanEsliyor(string? metin, string? sorgu)
    {
        if (string.IsNullOrWhiteSpace(sorgu)) return false;
        return Katla(metin).StartsWith(Katla(sorgu), StringComparison.Ordinal);
    }
}
