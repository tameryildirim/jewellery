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
        string.Join(" · ", new[] { string.Format(D1.KalemSayi, b.KalemSayisi), Bicim.TlKurus(b.ToplamTl), Bicim.TarihSaat(b.Olusturuldu), b.KullaniciAd }
            .Where(p => !string.IsNullOrWhiteSpace(p)))))];
}
