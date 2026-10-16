using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 — Müşteriler ekranının para formları (Tahsilat, Veresiye, Döviz tahsilat,
// Döviz veresiye) sunucuda MASAK kapısına tabidir (CariServisi TahsilatGirHareket / VeresiyeGir /
// DovizTahsilatGir / DovizVeresiyeGir → MasakKapisi.DogrulaKumulatif). Ekranda kimlik alanı yoktu:
// 200.000 ₺ üstü nakit tahsilat çıkışsız kalıyordu (tasarım hakemi KRİTİK, 10.10).
// Dört form TEK ParaMasakKapisi örneğini paylaşır — aynı anda yalnız biri açıktır ve her açılış Ac ile
// sıfırlar (kişisel veri önceki formdan taşınmaz). Ölçü TL'dir: döviz formlarında kurdan türetilen TL
// karşılığı (ekrandaki salt okunur satırla AYNI hesap; son sözü sunucunun kuru söyler).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Cariler
{
    private ParaMasakKapisi? pmKapi;
    /// <summary>Para formlarının ortak MASAK kapısı (tek örnek).</summary>
    private ParaMasakKapisi PM => pmKapi ??= ParaMasakKapisi.Olustur(Api);

    /// <summary>MASAK alanı değişince sayfa yeniden çizilir (Kaydet kilidi burada hesaplanır).</summary>
    private void MasakAlaniDegisti() { }

    // ── Veresiye ──
    private async Task VrTutarDegisti(decimal? v)
    {
        vrTutar = v ?? 0m;
        await PM.Tazele(vrTutar, veresiyeCari?.Id);
    }

    /// <summary>UX-S30: veresiye Kaydet kilidinin sebebi (tutar yok ya da kimlik eksik).</summary>
    private string? VeresiyeKilidi =>
        sayiKapisi.OnekleVar("vr-") ? null   // sebebi SayiKapisiSebebi yazıyor (tek yer)
        : vrTutar <= 0m ? YasalKapi2Sozlugu.Al(Dil.Dil).VeresiyeTutarGirin
        : PM.KilitSebebi(Dil.Dil);

    // ── Döviz (ölçü: TL karşılığı) ──
    private decimal DovizTl(string paraBirimi, decimal tutar) =>
        DovizKuru(paraBirimi) is { } kur ? Math.Round(tutar * kur, 2, MidpointRounding.AwayFromZero) : 0m;

    private async Task DvTutarDegisti(decimal? v)
    {
        dvTutar = v ?? 0m; dvDokunuldu = true;
        await DvMasakTazele();
    }

    private Task DvMasakTazele() => PM.Tazele(DovizTl(dvParaBirimi, dvTutar), dovizVeresiyeCari?.Id);

    private async Task DtTutarDegisti(decimal? v)
    {
        dtTutar = v ?? 0m;
        await PM.Tazele(DovizTl(dtParaBirimi, dtTutar), dovizTahsilatCari?.Id);
    }

    /// <summary>Para birimi değişince kasa listesi (UX-S24) ve TL ölçüsü birlikte yenilenir.</summary>
    private async Task DtBirimDegisti()
    {
        DovizKasaSec();
        await PM.Tazele(DovizTl(dtParaBirimi, dtTutar), dovizTahsilatCari?.Id);
    }
}
