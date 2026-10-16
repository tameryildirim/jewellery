using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO tur 2 (hasım ORTA) — KASADAN VİRMANDA AKTARILAN PARANIN ARACI
//
// POS cihazı / banka hesabı tanımsız dükkânda kart ve havale tahsilatı kasaya
// yazılır. Bu para bankaya virmanla aktarılırken NAKİT damgası alırsa gün sonu
// nakit defteri (bakiye − Σkart − Σhavale) düşer, sayımda sahte FAZLA çıkar.
// Ekran: kaynak bir kasa ve kasada kart/havale payı VARSA "Ne aktarılıyor?"
// seçicisi açılır (yoksa gizli — zorunlu önce, UX-S22); varsayılan Nakit
// (bugünkü davranış). Pay sınırı seçeneğin yanında yazar; sunucu aynı sınırı
// ayrıca uygular (istemci kilidi yalnız ayna).
// ⚠ TEK DOKUNUŞ gerekçesi: Gider.razor virman formu başka dalgada açılmıştı;
//   bu dokunuş hasım bulgusunun (VirmanModulu NAKIT damgası) ekran ayağıdır.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Gider
{
    private ParaDemoMetinleri PdV => ParaDemoSozlugu.Al(Dil.Dil);

    private string? virmanArac = "NAKIT";
    private GunKapanisiOnizleme? virmanAyrim;
    private string? virmanAyrimHata;

    /// <summary>Kaynak kasada kart ya da havale payı varsa araç seçicisi görünür.</summary>
    private bool VirmanAracGorunur =>
        virmanAyrim is { } a && (a.KartToplam > 0m || a.HavaleToplam > 0m);

    private string VirmanParaBirimi =>
        Taraflar.FirstOrDefault(t => Anahtar(t) == virmanKaynak)?.ParaBirimi ?? "TRY";

    private List<SecenekOgesi> VirmanAracSecenekleri
    {
        get
        {
            var l = new List<SecenekOgesi> { new("NAKIT", PdV.OdemeNakit) };
            if (virmanAyrim is { } a)
            {
                if (a.KartToplam > 0m)
                    l.Add(new("KART", string.Format(PdV.VirmanKartPayi, Bicim.Para(a.KartToplam, VirmanParaBirimi))));
                if (a.HavaleToplam > 0m)
                    l.Add(new("HAVALE", string.Format(PdV.VirmanHavalePayi, Bicim.Para(a.HavaleToplam, VirmanParaBirimi))));
            }
            return l;
        }
    }

    /// <summary>Gövdeye giden araç: seçici görünmüyorsa null (sunucu NAKIT sayar).</summary>
    private string? VirmanAracGonder => VirmanAracGorunur && virmanArac != "NAKIT" ? virmanArac : null;

    /// <summary>Seçilen kart/havale payını aşan tutar Kaydet'i kilitler (sebep yazılı).</summary>
    private string? VirmanAracKilidi
    {
        get
        {
            if (VirmanAracGonder is not { } arac || virmanAyrim is not { } a || virmanTutar is not > 0m) return null;
            var pay = arac == "KART" ? a.KartToplam : a.HavaleToplam;
            return virmanTutar > pay
                ? string.Format(PdV.VirmanPayAsildi, Bicim.Para(pay, VirmanParaBirimi))
                : null;
        }
    }

    /// <summary>Kaynak değişince kasanın nakit/kart/havale ayrımı okunur.</summary>
    private async Task VirmanKaynakDegisti()
    {
        virmanArac = "NAKIT"; virmanAyrim = null; virmanAyrimHata = null;
        if (string.IsNullOrWhiteSpace(virmanKaynak)) return;
        var (tur, id) = Coz(virmanKaynak);
        if (tur != "KASA" || string.IsNullOrWhiteSpace(id)) return;
        try { virmanAyrim = await Api.GetAsync<GunKapanisiOnizleme>($"/kasalar/{id}/gun-kapanisi/onizleme"); }
        catch (ApiHata ex) { virmanAyrimHata = ex.Message; }
        catch (System.Text.Json.JsonException ex) { virmanAyrimHata = ex.Message; }
    }
}
