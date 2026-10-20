using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-03 (18.10, rakip eşitleme) — İŞLEM BEKLETME. Tezgâhtaki yarım satış fişi sunucuda bekletilir
// (POST /bekleyen-fisler), sıradaki müşteriye geçilir; "Bekleyen fişler" listesinden geri alınır
// (POST …/{id}/al — kayıt silinir, kalemler ve müşteri tezgâha yüklenir). Bekletme defter yazmaz;
// geri alınan fiş normal Kaydet'ten geçer (fiyat bandı, MASAK, nakit sınırı aynen).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private List<BekleyenFisOzet> bekleyenler = [];
    private bool bekleyenlerAcik;
    private bool bekletiliyor;
    private string? bekleyenNot;
    private bool bekleyenNotKotu;

    private D1Metinleri D1 => D1Sozlugu.Al(Dil.Dil);

    /// <summary>Bekleyenler yardımcı veridir: okunamazsa tezgâh kapanmaz, sebep not satırına yazılır (UX-S28).</summary>
    private async Task BekleyenleriYukle()
    {
        try { bekleyenler = await Api.GetAsync<List<BekleyenFisOzet>>("/bekleyen-fisler") ?? []; }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { bekleyenler = []; bekleyenNot = ex.Message; bekleyenNotKotu = true; }
    }

    private bool BekletGorunur => tur == "SATIS" && kalemler.Count > 0 && !kaydediliyor;

    private async Task Beklet()
    {
        if (!BekletGorunur || bekletiliyor) return;
        bekletiliyor = true;
        try
        {
            var icerik = new BekleyenFisIcerik(
                string.IsNullOrEmpty(cariId) || cariId == pesinCariId ? null : cariId,
                [.. kalemler.Select(k => new BekleyenFisKalem(k.UrunId, k.UrunAdi, k.Adet, k.BirimFiyat, k.Hediye, k.SatirIskontoYuzde))],
                fisIskontoYuzde, Bos(aciklama));
            var ozet = await Api.PostAsync<BekleyenFisOzet>("/bekleyen-fisler", new { icerik });
            await Vazgec();
            bekleyenNot = string.Format(D1.Bekletildi, ozet.Etiket); bekleyenNotKotu = false;
            await BekleyenleriYukle();
        }
        catch (ApiHata ex) { bekleyenNot = ex.Message; bekleyenNotKotu = true; }
        finally { bekletiliyor = false; }
    }

    private async Task BekleyenAl(string id)
    {
        if (FisDolu) { bekleyenNot = D1.OnceFisiBosalt; bekleyenNotKotu = true; return; }
        var etiket = bekleyenler.FirstOrDefault(b => b.Id == id)?.Etiket ?? "";
        try
        {
            var ic = await Api.PostAsync<BekleyenFisIcerik>($"/bekleyen-fisler/{Uri.EscapeDataString(id)}/al");
            kalemler.Clear();
            foreach (var k in ic.Kalemler)
                kalemler.Add(new FisKalemi
                {
                    UrunId = k.UrunId, UrunAdi = k.UrunAdi, Adet = k.Adet, BirimFiyat = k.BirimFiyat,
                    Hediye = k.Hediye, SatirIskontoYuzde = k.SatirIskontoYuzde,
                });
            fisIskontoYuzde = ic.FisIskontoYuzde;
            aciklama = ic.Aciklama ?? "";
            if (!string.IsNullOrEmpty(ic.CariId))
            {
                if (!cariler.Any(c => c.Id == ic.CariId)) await CarileriTazele();
                cariId = ic.CariId;
            }
            else { cariId = ""; PesinOnSec(); }
            // Y-11: teklif geri alınınca aynı kur kilidi kullanılır (fiyat sözü tutulur); kilit yoksa güncel kur + not.
            if (!string.IsNullOrEmpty(ic.KurKilidiId))
            {
                await KilitleriYukle();
                var k = kurKilitleri.FirstOrDefault(x => x.Id == ic.KurKilidiId && x.Bitis > DateTime.UtcNow);
                if (k is not null) { kurKilidi = k; kilitNot = null; KurYansit(); }
                else { kilitNot = TeklifSozlugu.KilitYok(Dil.Dil); kilitNotKotu = true; }
            }
            bekleyenlerAcik = false;
            bekleyenNot = string.Format(D1.BekleyenAlindi, etiket); bekleyenNotKotu = false;
            await TevsikOnizle();
        }
        catch (ApiHata ex) { bekleyenNot = ex.Message; bekleyenNotKotu = true; }
        await BekleyenleriYukle();
    }

    private async Task BekleyenSil(string id)
    {
        try { await Api.PostAsync<object>($"/bekleyen-fisler/{Uri.EscapeDataString(id)}/sil"); bekleyenNot = null; }
        catch (ApiHata ex) { bekleyenNot = ex.Message; bekleyenNotKotu = true; }
        await BekleyenleriYukle();
    }

    private IReadOnlyList<YildizBekleyenOgesi> YildizBekleyenler => [.. bekleyenler.Select(b => new YildizBekleyenOgesi(b.Id, b.Etiket,
        string.Join(" · ", new[] { string.Format(D1.KalemSayi, b.KalemSayisi), Bicim.TlKurus(b.ToplamTl), Bicim.TarihSaat(b.Olusturuldu), b.KullaniciAd,
                // Y-11: teklif satırında geçerlilik (dolduysa açıkça yazar)
                b.TeklifBitis is { } tb ? (tb > DateTime.UtcNow ? TeklifSozlugu.Gecerli(Bicim.TarihSaat(tb), Dil.Dil) : TeklifSozlugu.SuresiDoldu(Dil.Dil)) : null }
            .Where(p => !string.IsNullOrWhiteSpace(p))), b.TeklifNo is null ? null : b.Id))];

    // ── Y-11 (20.10) — fiyat teklif fişi: bekletme + kur kilidi → numaralı, süreli döküm (/cikti/TEKLIF/{id}) ──
    private async Task TeklifVer()
    {
        if (!BekletGorunur || bekletiliyor) return;
        if (KilitliKurId is null)
        {
            // Kilit yoksa teklif verilmez: süre seçici açılır, sebep not satırına yazılır (uydurma süre yok).
            kilitSecenekAcik = true; bekleyenNot = TeklifSozlugu.OnceKilit(Dil.Dil); bekleyenNotKotu = true;
            return;
        }
        bekletiliyor = true;
        try
        {
            var icerik = new BekleyenFisIcerik(
                string.IsNullOrEmpty(cariId) || cariId == pesinCariId ? null : cariId,
                [.. kalemler.Select(k => new BekleyenFisKalem(k.UrunId, k.UrunAdi, k.Adet, k.BirimFiyat, k.Hediye, k.SatirIskontoYuzde))],
                fisIskontoYuzde, Bos(aciklama));
            var ozet = await Api.PostAsync<BekleyenFisOzet>("/bekleyen-fisler", new { icerik, teklif = true, kurKilidiId = KilitliKurId });
            await Vazgec();
            bekleyenNot = TeklifSozlugu.Verildi(ozet.TeklifNo ?? ozet.Etiket, Dil.Dil); bekleyenNotKotu = false;
            await BekleyenleriYukle();
            Nav.NavigateTo($"/cikti/TEKLIF/{Uri.EscapeDataString(ozet.Id)}?donus=/islemler");
        }
        catch (ApiHata ex) { bekleyenNot = ex.Message; bekleyenNotKotu = true; }
        finally { bekletiliyor = false; }
    }
}
