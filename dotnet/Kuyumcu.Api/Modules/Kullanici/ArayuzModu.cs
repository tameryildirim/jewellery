namespace Kuyumcu.Api.Modules.Kullanici;

// ═════════════════════════════════════════════════════════════════════════════
// ARAYÜZ MODU ÇÖZÜMLEYİCİSİ — paket UX-3 Faz A
//
// TEK DOĞRULUK KAYNAĞI. "Etkin mod" yalnız burada hesaplanır ve yalnız
// /auth/ben yanıtıyla istemciye taşınır. İstemci bu mantığı TEKRARLAMAZ
// (Bertan KOŞUL 6): iki yerde iki farklı çözüm kuralı doğardı.
//
// ⚠ Bu bir TERCİHTİR, YETKİ DEĞİLDİR (K-UX-6 · Mehmet K1):
//   • Hiçbir yasal kapı (tevsik, MASAK, taş kökeni, pırlanta, gün kilidi)
//     bu değeri OKUMAZ. denetim.sh iki sayaçla bunu zorlar.
//   • Değer JWT claim'ine KONMAZ (Mehmet K6): claim'e girseydi (a) mod
//     değişimi yeniden girişe kadar yansımaz, (b) sunucuda "claim'den mod
//     oku" cazibesi doğardı.
//
// NULL = "bilinmiyor" değil, "kiracı varsayılanını devral" demektir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kullanıcı tercihi + kiracı varsayılanından ETKİN arayüz modunu çözer.</summary>
public static class ArayuzModu
{
    public const string Basit = "BASIT";
    public const string Profesyonel = "PROFESYONEL";

    /// <summary>Beyaz liste dışı hiçbir değer kabul edilmez (TAM eşleşme, büyük harf).</summary>
    public static bool Gecerli(string? mod) => mod is Basit or Profesyonel;

    /// <summary>
    /// Etkin mod: kullanıcının kendi tercihi varsa o; yoksa kiracı varsayılanı;
    /// o da yoksa (eski satır / bozuk veri) PROFESYONEL — yani BUGÜNKÜ davranış.
    /// Geri düşüş PROFESYONEL'dir: sade yüz bir kapı değil, bir tercihtir; bilinmeyen
    /// durumda kullanıcıdan alan GİZLEMEK, gizlenen alanın hiç doldurulmaması
    /// riskini doğurur (Anayasa md.1 — varsayım yapılmaz).
    /// </summary>
    public static string Coz(string? kullaniciModu, string? kiraciVarsayilani)
    {
        if (Gecerli(kullaniciModu)) return kullaniciModu!;
        return Gecerli(kiraciVarsayilani) ? kiraciVarsayilani! : Profesyonel;
    }
}
