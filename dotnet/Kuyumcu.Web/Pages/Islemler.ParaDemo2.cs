using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-2 — satış ekranı: BASİT tek ödeme satırı (madde 1), kayıt sonrası
// PRO ödeme katmanı (madde 11).
// ⚠ AYRI DOSYA NEDENİ: Islemler.razor 1.400 satır tavanındadır; razor'a yeni satır eklenmedi.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>BASİT tek ödeme satırında seçilebilen türler — sunucunun tek ödemeli satışta kabul ettikleri.</summary>
    private static readonly string[] BasitTekOdemeTurleri = ["NAKIT", "KART", "HAVALE"];

    /// <summary>
    /// PARA-DEMO-2 (madde 1): BASİT tezgâh fişinde, karma değilken ve yasal kapı (tevsik/MASAK) ödeme seçicisini
    /// kendisi AÇMAMIŞKEN tek ödeme satırı Nakit/Kart/Havale seçtirir. Kapı açıksa mevcut seçici (#islem-odeme)
    /// çizilir ve satır yalnız adı yazar — aynı soru iki yerde sorulmaz (UX-S22). null = satırda seçici yok.
    /// </summary>
    private IReadOnlyList<SecenekOgesi>? BasitOdemeSecenekleri =>
        // Nakit sınırı aşıldıysa (SATIS-4 a: ödeme kendiliğinden Kart'a döner, nedeni #islem-odeme altında yazar) mevcut seçici kalır.
        TezgahFisi && !karmaOdeme && !OdemeKapisiAcik && !NakitSiniriAsildi
            ? [new("NAKIT", Dil.T.Islem.OdemeNakit), new("KART", Dil.T.Islem.OdemeKart), new("HAVALE", Dil.T.Islem.OdemeHavale)]
            : null;

    /// <summary>Tek ödeme satırından tür seçildi: gövdenin odemeTuru'su (tek ödemeli /islemler/satis).</summary>
    private async Task BasitOdemeSec(string tur)
    {
        if (Array.IndexOf(BasitTekOdemeTurleri, tur) < 0) return;   // bilinmeyen değer uydurulmaz
        odemeTuru = tur;
        OdemeDegisti();
        await TevsikOnizle();
    }

    /// <summary>
    /// PARA-DEMO-2 (madde 1): "Tek ödemeye dön" seçili aracı KORUR — karmadaki ilk satır Kart idiyse tek ödeme
    /// Kart olur (eskiden hep Nakit'e dönüyordu). Tek ödemede seçilemeyen araç (veresiye, döviz, eski altın) → Nakit.
    /// </summary>
    private async Task TekOdemeyeDonAracla()
    {
        var ilk = bacaklar.FirstOrDefault()?.Arac;
        await TekOdemeyeDon();
        odemeTuru = ilk is not null && Array.IndexOf(BasitTekOdemeTurleri, ilk) >= 0 ? ilk : "NAKIT";
        await TevsikOnizle();
    }
}
