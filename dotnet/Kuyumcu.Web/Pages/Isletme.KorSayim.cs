using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// D1-11 (18.10, rakip eşitleme: "kör kasa sayımı") — işletme ayarı. Okunamazsa anahtar KİLİTLİ kalır, sebebi bantta (UX-S28).
public partial class Isletme
{
    private bool korSayim;
    private bool korSayimOkundu;
    private string? korSayimHata;

    private async Task KorSayimYukle()
    {
        korSayimHata = null;
        try
        {
            var a = await Api.GetAsync<JsonElement>("/kasalar/kor-sayim");
            korSayim = a.TryGetProperty("etkin", out var v) && v.ValueKind == JsonValueKind.True;
            korSayimOkundu = true;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { korSayimHata = ex.Message; }
    }

    private async Task KorSayimDegistir(bool etkin)
    {
        korSayimHata = null;
        try
        {
            var a = await Api.PutAsync<JsonElement>("/kasalar/kor-sayim", new { etkin });
            korSayim = a.TryGetProperty("etkin", out var v) && v.ValueKind == JsonValueKind.True;
            BasariGoster(korSayim ? D1Sozlugu.Al(Dil.Dil).KorSayimAcildi : D1Sozlugu.Al(Dil.Dil).KorSayimKapandi);
        }
        catch (ApiHata ex) { korSayimHata = ex.Message; }
    }
}
