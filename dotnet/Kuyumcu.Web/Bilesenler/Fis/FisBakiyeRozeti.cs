using System.Globalization;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-3 — MÜŞTERİ BAKİYE ROZETİ (PLAN § S-2 madde 2.6 · Pusula F4)
//
// Taslak (Tezgah.dc.html): müşteri kutusunun içinde "Borcu yok" hapı. Müşteri
// seçilince HAS + TL (ve sıfırdan farklıysa döviz) durumu tek rozette yazar.
//
//   • İşaret kuralı Cariler.razor ile AYNI (K4): artı = müşteri bize borçlu,
//     eksi = biz müşteriye borçluyuz (müşterinin alacağı).
//   • Veri YENİ İSTEK DEĞİLDİR: satış ekranının zaten çektiği GET /cari
//     listesindeki tlBakiye / hasGramBakiye / döviz alanlarıdır (plan: "veri
//     zaten yükleniyor"). Sunucu hesaplar; burada formül yok, yalnız yön + biçim.
//   • UX-S32: her tutar KENDİ birimiyle basılır (Bicim.Para; has gramı "g has").
//   • Saf sınıf: istek atmaz, ApiIstemci bilmez (PLAN § 3 md.5 — tek POST yolu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Seçili müşterinin bakiye rozetinin metnini ve rengini üretir.</summary>
public static class FisBakiyeRozeti
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Has gramı ekranda: "5,660 g has" (üç hane — gram hassasiyeti).</summary>
    public static string HasGram(decimal gram, FisMusteriMetinleri m) =>
        $"{Math.Abs(gram).ToString("N3", Tr)} g {m.Has}";

    /// <summary>
    /// Rozet metni ve CSS sınıfı. Müşteri yoksa <c>null</c> (rozet çizilmez).
    /// Hepsi sıfırsa "Borcu yok" (yeşil); yalnız borç → sarı; yalnız alacak →
    /// mavi; iki yön birden → sarı, iki yön ayrı ayrı yazılır.
    /// </summary>
    public static (string Metin, string Sinif)? Olustur(CariVeri? cari, FisMusteriMetinleri m)
    {
        if (cari is null) return null;

        // P-DIL-BORC-KASA tur 2 (doğrulayıcı DÜŞÜK): yön GÖSTERİM hassasiyetine yuvarlanmış değerden
        // seçilir (has 3 hane, para 2 hane) — "0,000 g has" yazıp "bize borçlu" denmez. Tek kural:
        // BorcDiliSozlugu.Yon.
        var parcalar = new List<(int Yon, string Metin)>
        {
            (BorcDiliSozlugu.Yon(cari.HasGramBakiye, 3), HasGram(cari.HasGramBakiye, m)),
            (BorcDiliSozlugu.Yon(cari.TlBakiye, 2), Bicim.Para(Math.Abs(cari.TlBakiye), "TRY")),
            (BorcDiliSozlugu.Yon(cari.UsdBakiye, 2), Bicim.Para(Math.Abs(cari.UsdBakiye), "USD")),
            (BorcDiliSozlugu.Yon(cari.EurBakiye, 2), Bicim.Para(Math.Abs(cari.EurBakiye), "EUR")),
            (BorcDiliSozlugu.Yon(cari.GbpBakiye, 2), Bicim.Para(Math.Abs(cari.GbpBakiye), "GBP")),
        };

        var borc = parcalar.Where(p => p.Yon > 0).Select(p => p.Metin).ToList();
        var alacak = parcalar.Where(p => p.Yon < 0).Select(p => p.Metin).ToList();

        if (borc.Count == 0 && alacak.Count == 0) return (m.BorcuYok, "rozet rozet-yesil");

        var satirlar = new List<string>();
        if (borc.Count > 0) satirlar.Add($"{m.Borcu}: {string.Join(" · ", borc)}");
        if (alacak.Count > 0) satirlar.Add($"{m.Alacagi}: {string.Join(" · ", alacak)}");
        var sinif = borc.Count > 0 ? "rozet rozet-sari" : "rozet rozet-mavi";
        return (string.Join(" — ", satirlar), sinif);
    }
}
