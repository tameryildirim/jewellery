namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT LİSTESİ MAKUL ARALIKLARI — paket S-3c (kurul tur 3 engel 20)
//
// NEDEN BURADA: eşik hem SUNUCUDA (fail-closed kapı) hem EKRANDA (uyarı + onay
// kutusu) gerekir. İki yerde iki ayrı sayı yazılırsa kullanıcı ekranda uyarı
// almadan sunucudan 400 yer — UX-S30 ("kilitli düğme sebebini söyler") ve
// UX-S31 ("tek kaynak") ikisi birden kırılır. Tek kaynak: bu dosya.
//
// SERT sınır: dışına ASLA çıkılamaz (400).
// RAHAT aralık: dışına bilinçli onayla (riskOnayi=true) çıkılır.
// ═════════════════════════════════════════════════════════════════════════════
public static class FiyatAraliklari
{
    public const decimal MarjSertAlt = -100m, MarjSertUst = 500m;
    public const decimal MarjRahatAlt = 0m, MarjRahatUst = 100m;
    public const decimal CarpanSertAlt = 0m, CarpanSertUst = 500m;
    public const decimal CarpanRahatUst = 200m;

    /// <summary>Sert sınır dışı alan kodu (<c>MARJ</c>/<c>CARPAN</c>) ya da <c>null</c>.</summary>
    public static string? SertSinirDisi(decimal marj, decimal carpan) =>
        marj < MarjSertAlt || marj > MarjSertUst ? "MARJ"
        : carpan < CarpanSertAlt || carpan > CarpanSertUst ? "CARPAN"
        : null;

    /// <summary>
    /// Rahat aralık dışıysa risk kodu, içindeyse <c>null</c>.
    /// Kodlar: <c>MARJ_EKSI</c>, <c>MARJ_YUKSEK</c>, <c>CARPAN_YUKSEK</c>.
    /// Ham kod EKRANA BASILMAZ (UX-S31) — sözlükten çevrilir.
    /// </summary>
    public static string? RiskKodu(decimal marj, decimal carpan) =>
        marj < MarjRahatAlt ? "MARJ_EKSI"
        : marj > MarjRahatUst ? "MARJ_YUKSEK"
        : carpan > CarpanRahatUst ? "CARPAN_YUKSEK"
        : null;

    // ── KISMİ GÖVDE (paket S-3c ikinci dilim) ────────────────────────────────
    // Ürüne özel fiyat (PATCH) ve toplu uygulama gövdelerinde marj/çarpan
    // NULLABLE'dır: "gönderilmedi" demek "listeden devral" demektir, riskli değer
    // demek değildir. Gönderilmeyen alan bu yüzden NÖTR değerle değerlendirilir —
    // böylece tek kapı hem tam hem kısmi gövdeye uygulanır (UX-S31: tek kaynak).
    /// <summary>Gönderilmeyen alanın risk üretmeyen karşılığı.</summary>
    public const decimal MarjNotr = 0m, CarpanNotr = 100m;

    /// <summary>Kısmi gövde için sert sınır denetimi; verilmeyen alan nötr sayılır.</summary>
    public static string? SertSinirDisiKismi(decimal? marj, decimal? carpan) =>
        SertSinirDisi(marj ?? MarjNotr, carpan ?? CarpanNotr);

    /// <summary>Kısmi gövde için risk kodu; verilmeyen alan nötr sayılır.</summary>
    public static string? RiskKoduKismi(decimal? marj, decimal? carpan) =>
        RiskKodu(marj ?? MarjNotr, carpan ?? CarpanNotr);
}
