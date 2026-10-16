using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — tezgâhın iki yeni sunucu kararı (Islemler.razor ≤ 1.400 satır:
// mantık burada, sayfada yalnız tek satırlık çağrılar).
//  • § 4.2 Peşin Müşteri art arda: kod PESIN_ART_ARDA_SORU → tek soru
//    (PesinArtArdaSoru). "Hayır, başka biri" → aynı fiş masak.ayniKisiBeyani=false
//    ile yeniden gönderilir. Beyan tek kullanımlıktır (başarı/müşteri değişimi sıfırlar).
//  • § 4.4 Yaptırım: kod YAPTIRIM_ENGEL → kırmızı bant (sözlükten tam cümle) +
//    Kaydet kilitli ve sebebi yazılı (UX-S30); müşteri değişince kilit kalkar.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private bool pesinSoruAcik;
    private bool? ayniKisiBeyani;
    private bool yaptirimEngel;

    private Kuyumcu.Core.I18n.Yasal3Metinleri Y3 => Kuyumcu.Core.I18n.Yasal3Sozlugu.Al(Dil.Dil);

    /// <summary>
    /// POST gövdesinin masak bloğu: kimlik gerekiyorsa MASAK formu (değişmedi); Peşin
    /// sorusu cevaplandıysa YALNIZ beyan; ikisi de yoksa null (gövde değişmez).
    /// </summary>
    private object? MasakGovdesi =>
        MasakGerekli ? masak : ayniKisiBeyani is { } b ? new { ayniKisiBeyani = b } : null;

    /// <summary>Kaydet'in catch'inde: sunucu kodunu okur (metne bakmaz).</summary>
    private void Yasal3Yakala(ApiHata ex)
    {
        pesinSoruAcik = ex.Kod == "PESIN_ART_ARDA_SORU";
        yaptirimEngel = ex.Kod == "YAPTIRIM_ENGEL";
        if (pesinSoruAcik) { hata = null; ayniKisiBeyani = null; }   // soru bandı hata bandının yerine
        if (yaptirimEngel) hata = Y3.EngelMetin;                      // BASİT sadeleştirmesi "(yasal liste)"yi silmesin
    }

    /// <summary>Başarı ya da müşteri değişimi: beyan ve kilit fişe özgüdür.</summary>
    private void Yasal3Sifirla() { pesinSoruAcik = false; ayniKisiBeyani = null; yaptirimEngel = false; }

    private async Task PesinSoruHayir()
    {
        ayniKisiBeyani = false;
        pesinSoruAcik = false;
        await Kaydet();
    }

    private void PesinSoruEvet()
    {
        Yasal3Sifirla();
        GercekMusteriSec();
    }

    /// <summary>Kaydet kilidinin yaptırım sebebi (UX-S30) — yoksa null.</summary>
    private string? YaptirimKilidi => yaptirimEngel ? Y3.EngelKilit : null;
}
