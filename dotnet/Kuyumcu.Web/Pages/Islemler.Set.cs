using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using SetVeri = Kuyumcu.Contracts.Sayfalar.Urunler.UrunSetiVeri;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-16 (18.10, rakip eşitleme: "set") — tezgâhta set barkodu: ürün barkodu bulunamazsa (404) set aranır;
// bulunursa her parça KENDİ fiyat teklifiyle ve adediyle fişe eklenir (stok parça başına düşer). Fiyatı
// hesaplanamayan parça varsa durulur ve sebep yazılır (sessiz eksik ekleme yok).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Set okutması — true: kod bir setti (eklendi ya da sebebi yazıldı); false: set de değil.</summary>
    private async Task<bool> SetOkut(string kod)
    {
        if (tur != "SATIS") return false;
        SetVeri set;
        try { set = await Api.GetAsync<SetVeri>($"/urun-setleri/barkod/{Uri.EscapeDataString(kod)}"); }
        catch (ApiHata ex) when (ex.StatusCode is 404 or 403) { return false; }
        if (set?.Kalemler is not { Count: > 0 }) return false;   // eksik/boş yanıt set sayılmaz — "bulunamadı" söylenir
        foreach (var k in set.Kalemler)
        {
            if (urunler.All(x => x.Id != k.UrunId))
            {
                try { urunler = [.. urunler, await Api.GetAsync<UrunVeri>($"/urunler/{Uri.EscapeDataString(k.UrunId)}")]; }
                catch (ApiHata ex) { giris.BarkodHata = ex.Message; return true; }
            }
            giris.UrunId = k.UrunId;
            giris.Adet = k.Adet;
            giris.BirimFiyat = await TeklifAl(k.UrunId);
            if (giris.BirimFiyat is null or <= 0)
            {
                giris.BarkodHata = string.Format(D1Sozlugu.Al(Dil.Dil).SetFiyatYok, k.UrunAdi, FiyatHesaplanamadiMetni(giris.SonTeklif));
                return true;
            }
            await KalemEkle();
        }
        giris.Barkod = "";
        return true;
    }
}
