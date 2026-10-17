using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// 18.10 (kurucu: "Faturalog kadar sade… müşteri hemen bulunacak, kullanıcı ürünü seçip satışını yapacak, faturaya
// direkt çevrilecek") — kuyumcu fatura haddi üstünde alıcının TC/VKN'si zorunludur (sunucu 400 alan:cariId). Seçili
// müşterinin kartında numara YOKSA tezgâhtar ekrandan çıkmadan yazar: kart tamamlanır (POST /islemler/musteri-kimlik,
// yalnız boş alan) ve satış AYNI belge seçimiyle yeniden kaydedilir.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private bool musteriKimlikEksik;
    private string? musteriKimlikHata;

    private bool MusteriKimlikSor =>
        musteriKimlikEksik && !string.IsNullOrEmpty(cariId) && cariId != pesinCariId
        && string.IsNullOrEmpty(cariler.FirstOrDefault(c => c.Id == cariId)?.VergiNo);

    private void MusteriKimlikYakala(ApiHata ex)
    {
        musteriKimlikHata = null;
        musteriKimlikEksik = ex.StatusCode == 400 && ex.Alanlar.ContainsKey("cariId") && cariId != pesinCariId;
    }

    private async Task MusteriKimlikKaydet(string no)
    {
        musteriKimlikHata = null;
        try
        {
            await Api.PostAsync<System.Text.Json.JsonElement>("/islemler/musteri-kimlik", new { cariId, kimlikNo = no });
            var i = cariler.FindIndex(c => c.Id == cariId);
            if (i >= 0) cariler[i] = cariler[i] with { VergiNo = no };
            musteriKimlikEksik = false; hata = null;
            await Kaydet();
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { musteriKimlikHata = ex.Message; }
    }
}
