namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-M — TELEFON GÖSTERİM VE GİRDİ BİÇİMİ (tuval Musteriler.dc.html)
//
// Tuval: "0532 411 22 33" (TR: 0 + 3 + 3 + 2 + 2). Bu dosya YALNIZ biçimdir;
// doğrulama kuralı sunucudadır (Kuyumcu.Core Iletisim.TelefonGecerli).
//
// ÖLÇÜLDÜ (kuyumcu_net, 2026-09-25): kayıtlı 52 telefonun 51'i YALNIZ RAKAM,
// 11 hane, başında 0 ("05324112233"); 1'i boşluklu. Sunucu kullanıcının
// yazdığını AYNEN saklar; arama rakam çekirdeğiyle eşleştirir (CariArama).
// → Sunucuya giden değer ölçülen çoğunluk biçimidir: tanınan TR numarası
//   "0" + 10 rakam; tanınmayan (yabancı, kısa, harfli) AYNEN (kırpılmış) gider,
//   son sözü sunucu söyler. Mevcut kayıtlar GÖÇ GEREKTİRMEZ: gösterimde biçimlenir.
//
// Tanınan TR numarası (ulusal 10 hane, ilk hane 2-5 ya da 8: sabit hat 2xx-4xx,
// cep 5xx, 0850):
//   "05324112233" · "0532 411 22 33" · "(0532) 411-22-33" · "5324112233"
//   "+905324112233" · "+90 (532) 411-22-33" · "905324112233"
// "0090 532 411 22 33" (00 uluslararası önek) de tanınır (GIRDI-AILESI-2 tur 3).
// Tanınmayan her şey (7 hane, harf, "+44 …", "0044 …") OLDUĞU GİBİ gösterilir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>TR telefon numarasının gösterim/girdi biçimi ve sunucuya giden hali.</summary>
public static class TelefonBicimi
{
    /// <summary>Boş telefonun liste gösterimi.</summary>
    public const string Bos = "—";

    /// <summary>
    /// Girdiyi TR ulusal 10 haneye (başındaki 0 OLMADAN) indirger; tanınmazsa null.
    /// Serbest yazım işaretleri: boşluk, tire, parantez, nokta; baştaki '+' yalnız +90 için.
    /// </summary>
    public static string? UlusalHane(string? girdi)
    {
        if (string.IsNullOrWhiteSpace(girdi)) return null;
        var t = girdi.Trim();
        var arti = t[0] == '+';
        var rakam = new System.Text.StringBuilder(t.Length);
        for (var i = arti ? 1 : 0; i < t.Length; i++)
        {
            var ch = t[i];
            if (ch is >= '0' and <= '9') rakam.Append(ch);
            else if (ch is ' ' or '-' or '(' or ')' or '.') continue;
            else return null;   // harf ya da tanınmayan işaret → biçimlenmez
        }
        var r = rakam.ToString();
        string? h = arti
            ? (r.Length == 12 && r.StartsWith("90", StringComparison.Ordinal) ? r[2..] : null)
            : r.Length switch
            {
                11 when r[0] == '0' => r[1..],
                10 => r,
                12 when r.StartsWith("90", StringComparison.Ordinal) => r[2..],
                // GIRDI-AILESI-2 tur 3: "0090 532 …" (00 uluslararası önek) TR numarasıdır.
                14 when r.StartsWith("0090", StringComparison.Ordinal) => r[4..],
                _ => null,
            };
        return h is not null && h[0] is '2' or '3' or '4' or '5' or '8' ? h : null;
    }

    /// <summary>Liste/okuma gösterimi: "0532 411 22 33"; boş → "—"; tanınmayan AYNEN.</summary>
    public static string Goster(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return Bos;
        return UlusalHane(tel) is { } h ? Bicimle(h) : tel.Trim();
    }

    /// <summary>
    /// Girdi kutusunda görünen değer: tanınan numara "0532 411 22 33"; boş → "";
    /// tanınmayan kullanıcının yazdığı gibi (kırpılmış) kalır — bozulmaz.
    /// </summary>
    public static string Girdi(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return "";
        return UlusalHane(tel) is { } h ? Bicimle(h) : tel.Trim();
    }

    /// <summary>
    /// Sunucuya giden değer: boş → null; tanınan TR numarası "05324112233"
    /// (ölçülen saklama biçimi); tanınmayan AYNEN (kırpılmış) — sunucu doğrular.
    /// </summary>
    public static string? Sunucuya(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return null;
        return UlusalHane(tel) is { } h ? "0" + h : tel.Trim();
    }

    private static string Bicimle(string h) => $"0{h[..3]} {h[3..6]} {h[6..8]} {h[8..10]}";
}
