using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// D1-05 (18.10) — satış onayı kuralları özeti; düzenleme SatisOnayAyarCekmecesi'nde. Okunamazsa sebep bantta (UX-S28).
public partial class Isletme
{
    private SatisOnayAyarlari? satisOnay;
    private string? satisOnayHata;
    private bool satisOnayCekmeceAcik;

    private async Task SatisOnayYukle()
    {
        try { satisOnay = await Api.GetAsync<SatisOnayAyarlari>("/satis-onayi/ayarlar"); satisOnayHata = null; }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { satisOnayHata = ex.Message; }
    }

    // D2-12 — sadakat puanı ayarı
    private Kuyumcu.Contracts.Sayfalar.Cariler.SadakatAyari? sadakat;
    private bool sadakatCekmeceAcik;
    private bool disApiAcik;   // D3-11

    // 20.10 — e-Fatura bağlantısı (yalnız sahip görür; ötekilerde sunucu 403 → satır zaten çizilmez)
    private Kuyumcu.Contracts.Sayfalar.EBelgeAyarOzet? ebelge;
    private bool ebelgeAcik;

    private async Task EBelgeYukle()
    {
        if (Oturum.Kullanici?.Rol != "SAHIP") return;
        try { ebelge = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.EBelgeAyarOzet>("/ebelge/ayar"); }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { satisOnayHata ??= ex.Message; }
    }

    private string EBelgeOzeti => ebelge is { Kayitli: true } e
        ? EBelgeSozlugu.Bagli(e.Saglayicilar.FirstOrDefault(s => s.Kod == e.Saglayici)?.Ad ?? e.Saglayici!, e.Ortam ?? "TEST", Dil.Dil)
          + (e.Etkin ? "" : " · " + EBelgeSozlugu.Kapali(Dil.Dil))
        : EBelgeSozlugu.BagliDegil(Dil.Dil);

    private async Task SadakatYukle()
    {
        try { sadakat = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Cariler.SadakatAyari>("/sadakat/ayar"); }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { satisOnayHata ??= ex.Message; }
    }

    private void SadakatKaydedildi(Kuyumcu.Contracts.Sayfalar.Cariler.SadakatAyari a) { sadakat = a; sadakatCekmeceAcik = false; }

    private string SadakatOzeti => sadakat is { TlBasina: { } t, Baslangic: { } b }
        ? string.Format(D1Sozlugu.Al(Dil.Dil).SadakatOzet, Bicim.Oran(t), Bicim.Gun(b))
        : D1Sozlugu.Al(Dil.Dil).Kapali;

    private void SatisOnayKaydedildi(SatisOnayAyarlari a)
    {
        satisOnay = a; satisOnayCekmeceAcik = false;
        BasariGoster(D1Sozlugu.Al(Dil.Dil).OnayKurallariKaydedildi);
    }

    private string SatisOnayOzeti
    {
        get
        {
            var m = D1Sozlugu.Al(Dil.Dil);
            if (satisOnay is not { } a) return "—";
            return string.Format(m.OnayKurallariOzet,
                a.IskontoYuzde is { } i ? $"%{Bicim.Oran(i)}" : m.Kapali,
                a.TutarTl is { } t ? Bicim.TlKurus(t) : m.Kapali,
                a.Zararina ? m.Acik : m.Kapali,
                a.MesaiBaslangic is { } b && a.MesaiBitis is { } e ? $"{b}–{e}" : m.Kapali);
        }
    }
}
