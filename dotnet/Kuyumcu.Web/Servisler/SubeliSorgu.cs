namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-B (§ 4.3.3) — TEK İSTEMCİ YARDIMCISI: görünüm şubesini
// sorguya ekler.
//
// NEDEN TEK YARDIMCI: Panel, Kasalar, Hurda, POS, Emanet, stok, kâr-zarar,
// işlem defteri ve kasa raporu aynı soruyu sorar ("kullanıcı şu an hangi şubeye
// bakıyor?"). Her sayfa kendi `subeId=` birleştirmesini yazsaydı biri unutulur,
// ekran "Merkez" derken o kart tüm şubeleri gösterirdi.
//
// ⚠ YETKİ DEĞİLDİR: sunucu her uçta GorunumSubesi.Coz ile doğrular. Şubeye
//   kilitli kullanıcıda değer hep null'dır (sunucu oturum şubesini uygular).
// ⚠ Kiracı geneli uçlar (cari yaşlandırma, vade, banka, fatura, MASAK, tevsik)
//   bu yardımcıdan GEÇMEZ — müşteri hesapları şubeye bölünmez (§ 4.2.3).
// ═════════════════════════════════════════════════════════════════════════════

public static class SubeliSorgu
{
    private static KabukServisi Kabuk => KabukServisi.Ortak;

    /// <summary>Bu oturumun görünüm şubesi (null = tüm şubeler / daraltma yok).</summary>
    public static string? Sube(OturumServisi oturum) => Kabuk.GorunumSubesiIcin(oturum);

    /// <summary>
    /// Görünüm şubesi seçiliyse yola <c>subeId=</c> ekler; seçili değilse yolu
    /// AYNEN döner. Yolda zaten <c>subeId=</c> varsa (sayfanın kendi süzgeci)
    /// dokunulmaz.
    /// </summary>
    public static string Ekle(string yol, OturumServisi oturum)
    {
        if (Sube(oturum) is not { Length: > 0 } s) return yol;
        if (yol.Contains("subeId=", StringComparison.Ordinal)) return yol;
        return yol + (yol.Contains('?') ? "&" : "?") + "subeId=" + Uri.EscapeDataString(s);
    }

    /// <summary>
    /// Tercih okuması uçuştaysa bekler (sayfa yenilenince ilk istek seçili şubeyle
    /// gitsin — "önce birleşik, sonra şube" titremesi olmasın). Okuma yoksa hemen döner.
    /// </summary>
    public static Task HazirAsync(OturumServisi oturum) => Kabuk.GorunumBekle(oturum);
}
