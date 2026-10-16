using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// M6 (müşteri 16.10: "fişe hiç gerek yok… sisteme girdiğim anda fatura kessin") — Tezgâh'ın satış
// belgesi varsayılanı (FIS | FATURA). Okunamazsa anahtar KİLİTLİ kalır, sebebi bantta (UX-S28).
public partial class Isletme
{
    private string satisBelgeVarsayilan = "FIS";
    private bool satisBelgeOkundu;
    private string? satisBelgeHata;

    private async Task SatisBelgeYukle()
    {
        satisBelgeHata = null;
        try
        {
            var a = await Api.GetAsync<JsonElement>("/satis-belge");
            satisBelgeVarsayilan = a.TryGetProperty("varsayilan", out var v) && v.GetString() == "FATURA" ? "FATURA" : "FIS";
            satisBelgeOkundu = true;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { satisBelgeHata = ex.Message; }
    }

    private async Task SatisBelgeDegistir(bool fatura)
    {
        satisBelgeHata = null;
        try
        {
            var a = await Api.PutAsync<JsonElement>("/satis-belge", new { varsayilan = fatura ? "FATURA" : "FIS" });
            satisBelgeVarsayilan = a.TryGetProperty("varsayilan", out var v) && v.GetString() == "FATURA" ? "FATURA" : "FIS";
            BasariGoster(satisBelgeVarsayilan == "FATURA" ? SatisBelgeSozlugu.VarsayilanFatura(Dil.Dil) : SatisBelgeSozlugu.VarsayilanFis(Dil.Dil));
        }
        catch (ApiHata ex) { satisBelgeHata = ex.Message; }
    }
}
