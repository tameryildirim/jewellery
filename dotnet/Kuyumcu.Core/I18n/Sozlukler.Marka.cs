// ═══════════════════════════════════════════════════════════════════════════
// MARKA ADI — TEK KAYNAK (paket P-D2 · demo hazırlık dalgası, 23 Eylül 2026)
//
// Kurucu kararı (23 Eylül 2026, birebir): "ürünün adı mihenk olacak".
// MİHENK = mihenk taşı; kuyumcunun altının ayarını ölçtüğü taş.
//
// NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı anda
// başka şeritler tarafından kullanılır. Marka metni oraya değil buraya yazılır;
// aynı namespace olduğu için çağrı yeri değişmez (Dil/Marka yan yana durur).
//
// KURAL: yazılımın adı bundan sonra YALNIZCA buradan okunur. Razor/PageTitle/
// C# içine düz metin marka adı yazılmaz — dotnet/denetim.sh P02-1d sayacı
// (ESKI_URUN_ADI = 0) ve Kuyumcu.Tests/MarkaAdiTestleri.cs bunu kapatır.
//
// SINIR (bilinçli): YASAL BELGE ŞABLONLARINDA (gider pusulası, fatura, MASAK
// formları) geçen FİRMA UNVANI ve yasal metinler bu dosyanın konusu DEĞİLDİR
// ve DEĞİŞMEZ. Burada tanımlanan yalnızca YAZILIMIN ADIdır.
// Firma adı ByGatech A.Ş. olarak kalır.
// ═══════════════════════════════════════════════════════════════════════════
namespace Kuyumcu.Core.I18n;

/// <summary>
/// Ürünün (yazılımın) kullanıcıya görünen adı ve ondan türeyen başlıklar.
/// Tek kaynak: burada değişir, her yerde değişir.
/// </summary>
public static class Marka
{
    /// <summary>Ürün adı — Türkçe yazım (büyük İ). Ekranda görünen ad budur.</summary>
    public const string Ad = "MİHENK";

    /// <summary>
    /// ASCII yazım. Türkçe'ye özgü harf taşımayan yerler (otpauth URI, dosya adı,
    /// e-posta başlığı gibi aktarım yüzeyleri) için.
    /// </summary>
    public const string AdAscii = "MIHENK";

    /// <summary>Sayfa başlığı ayracı — em tire, iki yanında boşluk.</summary>
    public const string Ayrac = " — ";

    /// <summary>Kılavuz belgesinin başlığı.</summary>
    public const string KilavuzBasligi = Ad + Ayrac + "Kullanım Kılavuzu";

    /// <summary>Açılış (yükleniyor) ekranının metni.</summary>
    public const string YukleniyorMetni = Ad + " yükleniyor…";

    /// <summary>
    /// Tarayıcı sekmesi başlığı: "&lt;sayfa adı&gt; — MİHENK".
    /// Sayfa adı boşsa yalnız ürün adı döner (boş ayraç basılmaz).
    /// </summary>
    public static string SayfaBasligi(string? sayfaAdi) =>
        string.IsNullOrWhiteSpace(sayfaAdi) ? Ad : sayfaAdi + Ayrac + Ad;
}
