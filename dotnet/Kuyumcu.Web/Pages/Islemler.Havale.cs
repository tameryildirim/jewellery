using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// 17.10 HAVALEDEN SATIŞ — kurucu: "hesaba para gelince listeye düşer, tezgâhtar ne sattıysa kendisi seçer ve
// direkt fatura kesilir… arka planda satış fişine de dönüşür, kullanıcı görmez, kullanıcıyı yormamak lazım".
//   • Tezgâh /islemler?havale={bankaHareketId} ile açılır (Banka › karar kutusu › "Ürün seç ve sat").
//   • Müşteri havalenin müşterisi, belge FATURA, ödeme tek HAVALE bacağı (kasaya YAZILMAZ — para bankada).
//   • Fiş toplamı gelen havaleye EŞİT olmalı (Kaydet kilidi sebebini yazar); fiyat/indirimle eşitlenir.
//   • Satış + fatura + dekontun satışa bağlanması sunucuda TEK istekte (SatisModulu.HavaleDogrula/Bagla).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private HavaleSatiri? havale;

    /// <summary>Tezgâh bir gelen havaleyi satıyor mu?</summary>
    private bool HavaleModu => havale is not null && tur == "SATIS";

    /// <summary>Havale modunda fiş toplamı gelen paraya eşit değilse Kaydet kilidinin sebebi.</summary>
    private string? HavaleKilidi =>
        !HavaleModu || havale is not { } h ? null
        : karmaOdeme ? HavaleSozlugu.KarmaYok(Dil.Dil)
        : ToplamTl != h.Tutar ? HavaleSozlugu.ToplamEsitOlmali(Bicim.Tl(h.Tutar), Bicim.Tl(ToplamTl), Dil.Dil) : null;

    private string? HavaleNotu => HavaleModu && havale is { } h
        ? HavaleSozlugu.Not(h.CariAd ?? h.KarsiTarafAd ?? "—", Bicim.Tl(h.Tutar), Dil.Dil) : null;

    /// <summary>Tek bacaklı satış gövdesi: havale modunda banka hareketine bağlı HAVALE, değilse bugünkü bacak (gövde birebir eskisi).</summary>
    private object[] TekOdeme(string? kasa) => HavaleModu && havale is { } h
        ? [new { arac = "HAVALE", tutarTl = ToplamTl, kasaId = (string?)null, bankaHareketId = h.Id }]
        : [new { arac = odemeTuru, tutarTl = ToplamTl, kasaId = odemeTuru == "VERESIYE" ? null : kasa }];

    /// <summary>
    /// Açılışın TÜM adımları (bekleyen havaleler dahil) bitti mi — sayfa kabında data-acilis="bitti".
    /// Davranış değiştirmez; bUnit tezgâh yardımcıları (TemaTemizligiGDYardimcisi.TezgahBekle) bunu bekler:
    /// açılışın son adımı sürerken basılan düğme renderer kuyruğuna düşüp tıklamadan SONRA işleniyordu (17.10 dalgalı kırmızılar).
    /// </summary>
    private bool acilisBitti;

    private List<HavaleSatiri> bekleyenHavaleler = [];
    private bool havalelerAcik;

    /// <summary>Tezgâh açılışında bekleyen havaleleri okur; ?havale= varsa o havaleyi seçer.</summary>
    private async Task HavaleYukle()
    {
        try { bekleyenHavaleler = await Api.TazeGetAsync<List<HavaleSatiri>>("/islemler/havale-bekleyen"); }
        catch (Exception ex) when (ex is ApiHata or JsonException) { bekleyenHavaleler = []; }
        var sorgu = new Uri(Nav.Uri).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
        // Açılış yarışı: havale listesi gelirken tezgâhtar başka türe geçtiyse (Alış …) havale o fişe ZORLANMAZ —
        // HavaleSec türü SATIŞ yapıp kalemleri temizlemeden taşırdı; havale listede kalır, oradan seçilir.
        if (sorgu.TryGetValue("havale", out var id) && !string.IsNullOrWhiteSpace(id) && tur == "SATIS") HavaleSec(id);
    }

    /// <summary>Havaleyi seçer: müşteri havalenin müşterisi, ödeme HAVALE, belge FATURA (kurucu: "direkt fatura kesilir").</summary>
    private void HavaleSec(string id)
    {
        if (bekleyenHavaleler.FirstOrDefault(x => x.Id == id) is not { } h) return;
        havale = h;
        havalelerAcik = false;
        tur = "SATIS";
        odemeTuru = "HAVALE";
        belgeTuru = "FATURA";
        if (h.CariId is { Length: > 0 } c) cariId = c;
    }

    /// <summary>Tezgâhtar havaleden satmaktan vazgeçti: normal satışa döner (sepet korunur).</summary>
    private void HavaleBirak()
    {
        havale = null;
        odemeTuru = "NAKIT";
        belgeTuru = null;   // normal satışta fatura yeniden sorulur
        if (new Uri(Nav.Uri).Query.Contains("havale=")) Nav.NavigateTo("/islemler", replace: true);
    }

    /// <summary>Başarılı satıştan sonra havale modu kapanır, listeden düşer, adres temizlenir.</summary>
    private void HavaleKapat()
    {
        if (havale is null) return;
        bekleyenHavaleler.RemoveAll(x => x.Id == havale.Id);
        HavaleBirak();
    }

    private IReadOnlyList<Kuyumcu.Web.Bilesenler.Yildiz.YildizHavaleOgesi> HavaleOgeleri => [.. bekleyenHavaleler.Select(h =>
        new Kuyumcu.Web.Bilesenler.Yildiz.YildizHavaleOgesi(h.Id, h.CariAd ?? h.KarsiTarafAd ?? "—", Bicim.Tl(h.Tutar), Bicim.TarihTam(h.Tarih)))];
}
