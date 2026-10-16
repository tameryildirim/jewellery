using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-YASLANDIRMA (madde 1) — CARİ YAŞLANDIRMA DİLİM KURALI (tek yer).
//
// GOC-47B § 0.7 (hasım ORTA): açılış devri (ACILIS) bakiyeye "şimdi" tarihiyle
// yazıldığı için cari yaşlandırma devralınan borcu 0-30 gün gösteriyordu —
// önceki programda 200 gündür bekleyen borç "taze" okunuyordu.
//
// Kural (tur 2 — hasım KRİTİK düzeltmesi): BELIRSIZ ancak İKİ koşul BİRLİKTE
// sağlanınca verilir: (a) açılış NET tutarı ≠ 0 (TL, has ya da bir döviz biriminin
// kendi neti — birimler birbirine toplanmaz) VE (b) son açılıştan sonra açılış
// OLMAYAN yazım yok. Öteki durumlarda yaşlanma tarihi = açılış olmayan son
// hareketin tarihi (ters açılış kaydı "son işlem" sayılmaz — açılış geri alınınca
// kalan yeni borç 0-30'da kalır). Açılıştan SONRA işlem gören ve açılışı hâlâ
// bakiyede olan cari "açılış bakiyesi de var" bayrağını taşır (rozet).
//
// Rapor (ozetTl), satır dilimi ve ekran AYNI kuraldan beslenir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Cari yaşlandırma dilim kuralı.</summary>
public static class CariYaslandirmaKurali
{
    /// <summary>Açılış devri hareket türü (CariServisi.AcilisYaz yazar).</summary>
    public const string AcilisTuru = "ACILIS";

    /// <summary>Vadesi bilinmeyen dilim anahtarı (JSON'a aynen geçer).</summary>
    public const string Belirsiz = "BELIRSIZ";

    /// <summary>BELIRSIZ nedeni: bakiyeye son dokunan yazım açılış devri.</summary>
    public const string NedenAcilis = "ACILIS";

    /// <summary>BELIRSIZ nedeni: bakiye satırında hiç işlem tarihi yok.</summary>
    public const string NedenTarihYok = "TARIH_YOK";

    /// <summary>SQL projeksiyonu: görünüm satırı + yaşlanma tarihi (null = bilinmiyor).</summary>
    public sealed class Satir
    {
        public CariBakiyeGorunumu G { get; init; } = null!;
        public DateTime? YasTarihi { get; init; }
        /// <summary>Açılış devri hâlâ bakiyede (birim bazında net ≠ 0).</summary>
        public bool AcilisDahil { get; init; }
    }

    /// <summary>
    /// BELIRSIZ nedeni: tarihli dilimde null; açılış neti bakiyedeyse ACILIS;
    /// aksi hâlde TARIH_YOK (bakiyede işlem tarihi yok). ACILIS yalnız
    /// <paramref name="acilisDahil"/> doğruyken verilir.
    /// </summary>
    public static string? BelirsizNedeni(int? gun, bool acilisDahil) =>
        gun is not null ? null : acilisDahil ? NedenAcilis : NedenTarihYok;

    /// <summary>Gün sayısı: floor(simdi − yaşlanma tarihi); tarih yoksa null.</summary>
    public static int? GunSayisi(DateTime? yasTarihi, DateTime simdi) =>
        yasTarihi is { } t ? (int)Math.Floor((simdi - t).TotalDays) : null;

    /// <summary>Gün sayısından dilim anahtarı.</summary>
    public static string Dilim(int? gun) =>
        gun is not int g ? Belirsiz
        : g <= 30        ? "0-30"
        : g <= 60        ? "31-60"
        : g <= 90        ? "61-90"
                         : "90+";
}
