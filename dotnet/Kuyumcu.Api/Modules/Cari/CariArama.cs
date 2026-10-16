namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-5 (QA turu 9, YÜKSEK) — CARİ ARAMASI TELEFONLA ÇALIŞIR.
//
// Ölçüm (kuyumcu_net, 2026-09-24): arama kutusu "Ad, telefon veya e-posta"
// diyordu ama sunucu yalnız `ad LIKE %q%` arıyordu; kayıtlı '05329998877' ile
// arama 0 satır döndü. Telefon, kayıtta kullanıcının yazdığı biçimle saklanır
// (Iletisim.TelefonDesen: rakam, boşluk, parantez, tire, baştaki +). Bu yüzden
// karşılaştırma İKİ TARAFTA da rakama indirgenmiş biçimle yapılır:
//   • kayıt tarafı: SQL'de REPLACE ile ' ', '-', '(', ')', '+' atılır;
//   • arama tarafı: TelefonCekirdegi — rakamlar, baştaki ülke kodu (90) ve
//     baştaki sıfırlar atılır. '0312 444 33 22', '03124443322',
//     '+90 312 444 33 22' ve '0090 312…' aynı çekirdeğe ('3124443322') iner;
//     çekirdek kayıt içinde PARÇA olarak aranır (baştaki 0/+90 fark etmez).
// ═════════════════════════════════════════════════════════════════════════════

public static class CariArama
{
    /// <summary>Çekirdek bundan kısaysa telefon araması yapılmaz (her kaydı eşleştirmesin).</summary>
    public const int EnAzRakam = 3;

    /// <summary>
    /// Arama metni bir telefon (ya da telefon parçası) gibi görünüyorsa rakam
    /// çekirdeğini döndürür; değilse null (ad/e-posta araması yine yapılır).
    /// Telefon gibi görünmek: yalnız rakam ve telefon yazım işaretleri
    /// (boşluk, +, -, (, ), .) içerir.
    /// </summary>
    public static string? TelefonCekirdegi(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return null;
        var rakamlar = new System.Text.StringBuilder(q.Length);
        foreach (var ch in q.Trim())
        {
            if (ch is >= '0' and <= '9') rakamlar.Append(ch);
            else if (ch is ' ' or '+' or '-' or '(' or ')' or '.') continue;
            else return null;
        }
        var r = rakamlar.ToString().TrimStart('0');
        // Ülke kodu: +90 5xx… → 12 rakam; 0090… → baştaki sıfırlar atılınca aynı.
        if (r.Length == 12 && r.StartsWith("90", StringComparison.Ordinal)) r = r[2..];
        return r.Length >= EnAzRakam ? r : null;
    }
}
